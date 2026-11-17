// === NoCodeMotion 视觉流程引擎（OpenCV 实现） | 作者：温​启‎志‍ ‍|⁣ ⁣微‏信‎：‏1‏8‌7‍1‌9‎3⁠6‏1​3​9‌9‌ ⁠|​ ​保‎留‍所⁠有‍权⁣利‏，⁣请‌勿‍删‎除 ===
using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using Cv = OpenCvSharp;

namespace NoCodeMotion.Services.Vision
{
    /// <summary>
    /// 卡尺族几何内核：在「一条搜索剖面」上找边缘点，再把点拟成直线 / 圆，并给出残差。
    ///
    /// 思路与 LV Studio 的卡尺工具、海康 VisionMaster 的找线找圆一致，实现全部自己写在灰度图上：
    ///   ① 剖面：沿搜索方向等间距取点（双线性插值，支持亚像素），得到一条 1D 灰度序列；
    ///   ② 梯度：中心差分 + 一次 1-2-1 平滑，避免直接拿灰度找极值被噪声骗；
    ///   ③ 找边：按「暗→亮 / 亮→暗 / 任意」挑极性，取 |梯度| ≥ 阈值的局部极大，
    ///      再用抛物线插值把峰位精修到亚像素；
    ///   ④ 拟合：直线 = 总体最小二乘（Cv2.FitLine）+ 按残差百分位剔群；
    ///      圆 = Kasa 代数解起值 + Gauss-Newton 对几何半径残差迭代 + 同样剔群。
    ///
    /// 内部角度一律用弧度算，只有出结果给人看时才换成度。
    /// </summary>
    public static class VisionCaliper
    {
        /// <summary>边缘方向（极性）。</summary>
        public enum Polarity { Any = 0, DarkToLight = 1, LightToDark = 2 }

        /// <summary>每条卡尺上取哪个候选边（对应传统找圆找直线 demo 的「选择模式」）。</summary>
        public enum EdgePick { Strongest = 0, AllAboveThreshold = 1, NearestExpected = 2 }

        /// <summary>稳健拟合策略（对应 demo 的三个 LoRansac 选项）。</summary>
        public enum RobustFit { Ransac = 0, RansacIrls = 1, RansacGeometricLm = 2 }

        /// <summary>一条剖面上找到的一个边缘点（亚像素）。</summary>
        public sealed class ProfileHit
        {
            public Cv.Point2f Point;
            public double Strength;   // 梯度幅值（越大边越明显）
            public double Position;   // 在剖面上的位置（像素，0 = 剖面起点）
        }

        public sealed class LineResult
        {
            public bool Ok;
            public double X1, Y1, X2, Y2;     // 端点（点投影到拟合方向后取首末，才有物理意义）
            public double AngleDeg;           // 与 +X 轴夹角，-90~90
            public double Rms;                // 垂直残差均方根
            public double MaxDeviation;       // 最大偏离：「这条线够不够直」就看它
            public int UsedPoints;
            public string Fail;
            /// <summary>稳健拟合留下的内点（画绿）/ 被剔除的外点（画红）。普通拟合为 null。</summary>
            public List<Cv.Point2f>? Inliers;
            public List<Cv.Point2f>? Outliers;
        }

        public sealed class CircleResult
        {
            public bool Ok;
            public double Cx, Cy, R;
            public double Rms;
            public int UsedPoints;
            public double StartDeg = double.NaN;   // 实际张到的起止角（整圆时 0 / 360）
            public double EndDeg = double.NaN;
            public string Fail;
            /// <summary>稳健拟合留下的内点（画绿）/ 被剔除的外点（画红）。普通拟合为 null。</summary>
            public List<Cv.Point2f>? Inliers;
            public List<Cv.Point2f>? Outliers;
        }

        // ===================== ① 剖面采样 =====================

        /// <summary>沿 (x0,y0)→(x1,y1) 取 count 个等间距灰度值（双线性插值；越界按边缘钳住）。</summary>
        public static double[] SampleProfile(Cv.Mat gray, double x0, double y0, double x1, double y1, int count)
        {
            count = Math.Max(2, count);
            var vals = new double[count];
            double dx = x1 - x0, dy = y1 - y0;
            int w = gray.Cols, h = gray.Rows;
            for (int i = 0; i < count; i++)
            {
                double t = (double)i / (count - 1);
                vals[i] = SampleAt(gray, x0 + dx * t, y0 + dy * t, w, h);
            }
            return vals;
        }

        static double SampleAt(Cv.Mat gray, double x, double y, int w, int h)
        {
            double fx = Math.Min(Math.Max(x, 0), w - 1.001);
            double fy = Math.Min(Math.Max(y, 0), h - 1.001);
            int x0 = (int)Math.Floor(fx), y0 = (int)Math.Floor(fy);
            int x1 = Math.Min(x0 + 1, w - 1), y1 = Math.Min(y0 + 1, h - 1);
            double ax = fx - x0, ay = fy - y0;
            double v00 = At(gray, x0, y0), v10 = At(gray, x1, y0), v01 = At(gray, x0, y1), v11 = At(gray, x1, y1);
            return (v00 * (1 - ax) + v10 * ax) * (1 - ay) + (v01 * (1 - ax) + v11 * ax) * ay;
        }

        static double At(Cv.Mat gray, int x, int y)
        {
            // MatType 的成员不是编译期常量，不能用 switch 表达式匹配，只能逐个比
            if (gray.Type() == Cv.MatType.CV_32FC1) return gray.Get<float>(y, x);
            if (gray.Type() == Cv.MatType.CV_64FC1) return gray.Get<double>(y, x);
            return gray.Get<byte>(y, x);
        }

        // ===================== ② 梯度 =====================

        /// <summary>中心差分梯度（首尾单侧），再做一次 1-2-1 平滑压噪。</summary>
        public static double[] Gradient(double[] vals)
        {
            int n = vals.Length;
            var g = new double[n];
            if (n < 2) return g;
            g[0] = vals[1] - vals[0];
            g[n - 1] = vals[n - 1] - vals[n - 2];
            for (int i = 1; i < n - 1; i++) g[i] = (vals[i + 1] - vals[i - 1]) * 0.5;
            var s = new double[n];
            for (int i = 0; i < n; i++)
            {
                double a = i > 0 ? g[i - 1] : g[i];
                double c = i < n - 1 ? g[i + 1] : g[i];
                s[i] = (a + 2 * g[i] + c) * 0.25;
            }
            return s;
        }

        // ===================== ③ 找边 =====================

        /// <summary>对剖面做对称盒式平滑（半径 = 卷积核半径）。对称核不挪位置，峰还是原来的位置。</summary>
        public static double[] Smooth(double[] vals, int radius)
        {
            if (vals == null || radius <= 0 || vals.Length < 3) return vals;
            int n = vals.Length;
            radius = Math.Min(radius, (n - 1) / 2);
            if (radius <= 0) return vals;
            var pre = new double[n + 1];
            for (int i = 0; i < n; i++) pre[i + 1] = pre[i] + vals[i];
            var dst = new double[n];
            for (int i = 0; i < n; i++)
            {
                int a = Math.Max(0, i - radius), b = Math.Min(n - 1, i + radius);
                dst[i] = (pre[b + 1] - pre[a]) / (b - a + 1);
            }
            return dst;
        }

        /// <summary>
        /// 在一条剖面上找满足极性与强度的边缘（亚像素）。
        /// maxEdges = 1 只要最强那条（单边卡尺）；&lt; 0 全要（一条线上有多个台阶时用）。
        /// </summary>
        public static List<ProfileHit> FindEdges(double[] vals, Polarity pol, double minStrength, double stepPx)
            => FindEdges(vals, pol, minStrength, stepPx, 0, true);

        /// <summary>
        /// 同上，另带「卷积核半径」（求导前平滑次数）与「亚像素」开关：
        /// kernelRadius = 0 不平滑；subpixel = false 时只报到整像素。
        /// </summary>
        public static List<ProfileHit> FindEdges(double[] vals, Polarity pol, double minStrength, double stepPx,
            int kernelRadius, bool subpixel)
        {
            if (kernelRadius > 0) vals = Smooth(vals, kernelRadius);
            var hits = new List<ProfileHit>();
            var g = Gradient(vals);
            int n = g.Length;
            for (int i = 1; i < n - 1; i++)
            {
                double v = g[i];
                if (Math.Abs(v) < minStrength) continue;
                if (!(Math.Abs(v) >= Math.Abs(g[i - 1]) && Math.Abs(v) > Math.Abs(g[i + 1]))) continue;
                if (pol == Polarity.DarkToLight && v <= 0) continue;
                if (pol == Polarity.LightToDark && v >= 0) continue;

                double denom = g[i - 1] - 2 * v + g[i + 1];
                double off = subpixel && Math.Abs(denom) > 1e-9 ? Math.Min(Math.Max(0.5 * (g[i - 1] - g[i + 1]) / denom, -1), 1) : 0;
                hits.Add(new ProfileHit { Position = (i + off) * stepPx, Strength = Math.Abs(v) });
            }
            return hits;
        }

        /// <summary>把剖面上的位置换算回图上坐标（沿剖面方向前进 Position 像素）。</summary>
        public static Cv.Point2f PointAlong(double x0, double y0, double x1, double y1, double position)
        {
            double len = Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
            if (len < 1e-9) return new Cv.Point2f((float)x0, (float)y0);
            double t = position / len;
            return new Cv.Point2f((float)(x0 + (x1 - x0) * t), (float)(y0 + (y1 - y0) * t));
        }

        // ===================== ④ 直线拟合 =====================

        /// <summary>总体最小二乘拟线 + 三轮按残差百分位剔群。点不够或几乎重合时明确报失败。</summary>
        public static LineResult FitLine(IList<Cv.Point2f> pts, double keepRatio = 0.9)
        {
            var r = new LineResult();
            if (pts == null || pts.Count < 2) { r.Fail = "边缘点太少（直线至少 2 个）"; return r; }

            var work = new List<Cv.Point2f>(pts);
            Cv.Line2D l = default;
            for (int round = 0; round < 3; round++)
            {
                if (work.Count < 2) { r.Fail = "剔除离群点后剩下的点不够"; return r; }
                l = Cv.Cv2.FitLine(work.ToArray(), Cv.DistanceTypes.L2, 0, 0.01, 0.01);
                var res = new double[work.Count];
                for (int i = 0; i < work.Count; i++) res[i] = PerpDist(work[i], l);
                double lim = Percentile(res, round == 0 ? 0.95 : keepRatio);
                var next = new List<Cv.Point2f>();
                for (int i = 0; i < work.Count; i++) if (res[i] <= Math.Max(lim, 0.3)) next.Add(work[i]);
                if (next.Count < 2 || next.Count == work.Count) break;
                work = next;
            }

            double vx = l.Vx, vy = l.Vy;
            double norm = Math.Sqrt(vx * vx + vy * vy);
            if (norm < 1e-9) { r.Fail = "这些点几乎重合，拟不出直线"; return r; }
            vx /= norm; vy /= norm;

            double tmin = double.MaxValue, tmax = double.MinValue;
            foreach (var p in work)
            {
                double t = (p.X - l.X1) * vx + (p.Y - l.Y1) * vy;
                if (t < tmin) tmin = t;
                if (t > tmax) tmax = t;
            }
            r.Ok = true;
            r.X1 = l.X1 + vx * tmin; r.Y1 = l.Y1 + vy * tmin;
            r.X2 = l.X1 + vx * tmax; r.Y2 = l.Y1 + vy * tmax;
            double ang = Math.Atan2(vy, vx) * 180.0 / Math.PI;
            while (ang > 90) ang -= 180;
            while (ang < -90) ang += 180;
            r.AngleDeg = ang;

            double sum = 0, mx = 0;
            foreach (var p in work)
            {
                double d = PerpDist(p, l);
                sum += d * d; if (d > mx) mx = d;
            }
            r.Rms = work.Count > 0 ? Math.Sqrt(sum / work.Count) : 0;
            r.MaxDeviation = mx;
            r.UsedPoints = work.Count;
            return r;
        }

        /// <summary>
        /// 稳健找直线：RANSAC 定出内点最多的那条，再按策略精修。
        ///   LoRansac            = RANSAC + 内点总体最小二乘（干净、快）
        ///   LoRansacIrls        = 再加 Tukey 权重的迭代重加权（杂点多时更稳）
        ///   LoRansacGeometricLm = 再加 Cauchy 权重的几何迭代（残差大的点只降权不扔）
        /// inlierTol = 内点最大偏差（像素）；minInliers = 低于这个内点数直接判失败。
        /// </summary>
        public static LineResult FitLineRobust(IList<Cv.Point2f> pts, RobustFit mode, double inlierTol,
            int minInliers, int maxIter = 300)
        {
            var r = new LineResult();
            int n = pts?.Count ?? 0;
            if (n < 2) { r.Fail = "边缘点太少（直线至少 2 个）"; return r; }
            inlierTol = Math.Max(0.05, inlierTol);
            var src = new List<Cv.Point2f>(pts);
            int need = Math.Max(2, Math.Min(minInliers, n));

            // ① RANSAC：随机取两点定线，按垂直距离数内点，留内点最多的那条
            double bx = src[0].X, by = src[0].Y, bvx = 1, bvy = 0;
            int best = -1;
            var rnd = new Random(20261117);
            int iter = Math.Min(maxIter, 40 + n * 4);
            for (int k = 0; k < iter; k++)
            {
                int i = rnd.Next(n), j = rnd.Next(n);
                if (i == j) continue;
                double vx = src[j].X - src[i].X, vy = src[j].Y - src[i].Y;
                double len = Math.Sqrt(vx * vx + vy * vy);
                if (len < 1e-6) continue;
                vx /= len; vy /= len;
                int cnt = 0;
                foreach (var p in src) if (Perp(p.X, p.Y, src[i].X, src[i].Y, vx, vy) <= inlierTol) cnt++;
                if (cnt > best) { best = cnt; bx = src[i].X; by = src[i].Y; bvx = vx; bvy = vy; }
            }
            if (best < need)
            {
                r.Fail = $"稳健拟合没找到足够的内点（{Math.Max(best, 0)}/{n}，至少要 {need} 个）：把「内点最大偏差」调大，或把「边缘响应阈值」调小";
                return r;
            }

            var inl = new List<Cv.Point2f>();
            var outl = new List<Cv.Point2f>();
            foreach (var p in src)
                (Perp(p.X, p.Y, bx, by, bvx, bvy) <= inlierTol ? inl : outl).Add(p);

            // ② 先做一次总体最小二乘（加权版也走这里，权重全 1）
            TlsLine(inl, null, out double cx, out double cy, out double ux, out double uy);

            // ③ 按策略迭代重加权
            int rounds = mode == RobustFit.Ransac ? 0 : (mode == RobustFit.RansacIrls ? 8 : 12);
            if (rounds > 0 && inl.Count >= 2)
            {
                var w = new double[inl.Count];
                for (int it = 0; it < rounds; it++)
                {
                    double sigma = 1.4826 * Mad(inl, cx, cy, ux, uy);
                    double c = (mode == RobustFit.RansacIrls ? 4.685 : 3.0) * Math.Max(sigma, inlierTol);
                    if (c < 1e-6) break;
                    for (int i = 0; i < inl.Count; i++)
                    {
                        double d = Perp(inl[i].X, inl[i].Y, cx, cy, ux, uy) / c;
                        // Tukey：超过 c 的直接权重 0；Cauchy：只降权，不归零
                        w[i] = mode == RobustFit.RansacIrls
                            ? (Math.Abs(d) >= 1 ? 0 : (1 - d * d) * (1 - d * d))
                            : 1.0 / (1.0 + d * d);
                    }
                    TlsLine(inl, w, out double nx, out double ny, out double nux, out double nuy);
                    bool done = Math.Abs(nx - cx) + Math.Abs(ny - cy) < 1e-4
                                && Math.Abs(nux - ux) + Math.Abs(nuy - uy) < 1e-6;
                    cx = nx; cy = ny; ux = nux; uy = nuy;
                    if (done) break;
                }
            }

            // ④ 以外点/内点的最终结果出数（分类仍按「离最终直线多远」）
            inl.Clear(); outl.Clear();
            foreach (var p in src)
                (Perp(p.X, p.Y, cx, cy, ux, uy) <= inlierTol ? inl : outl).Add(p);
            if (inl.Count < 2) { r.Fail = "稳健拟合后剩下的内点不够（少于 2 个）"; return r; }

            double tmin = double.MaxValue, tmax = double.MinValue, sum = 0, mx = 0;
            foreach (var p in inl)
            {
                double t = (p.X - cx) * ux + (p.Y - cy) * uy;
                if (t < tmin) tmin = t;
                if (t > tmax) tmax = t;
                double d = Perp(p.X, p.Y, cx, cy, ux, uy);
                sum += d * d; if (d > mx) mx = d;
            }

            r.Ok = true;
            r.X1 = cx + ux * tmin; r.Y1 = cy + uy * tmin;
            r.X2 = cx + ux * tmax; r.Y2 = cy + uy * tmax;
            double ang = Math.Atan2(uy, ux) * 180.0 / Math.PI;
            while (ang > 90) ang -= 180;
            while (ang < -90) ang += 180;
            r.AngleDeg = ang;
            r.Rms = Math.Sqrt(sum / inl.Count);
            r.MaxDeviation = mx;
            r.UsedPoints = inl.Count;
            r.Inliers = inl;
            r.Outliers = outl;
            return r;
        }

        /// <summary>加权总体最小二乘直线：主方向 = 加权协方差矩阵的主轴。</summary>
        static void TlsLine(IList<Cv.Point2f> pts, double[]? w,
            out double cx, out double cy, out double vx, out double vy)
        {
            double sw = 0, sx = 0, sy = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                double wi = w == null ? 1 : w[i];
                sw += wi; sx += wi * pts[i].X; sy += wi * pts[i].Y;
            }
            if (sw <= 1e-9)
            {
                sw = pts.Count; sx = 0; sy = 0;
                foreach (var p in pts) { sx += p.X; sy += p.Y; }
            }
            cx = sx / sw; cy = sy / sw;
            double sxx = 0, syy = 0, sxy = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                double wi = w == null ? 1 : w[i];
                double dx = pts[i].X - cx, dy = pts[i].Y - cy;
                sxx += wi * dx * dx; syy += wi * dy * dy; sxy += wi * dx * dy;
            }
            if (sxx + syy < 1e-12) { vx = 1; vy = 0; return; }
            double th = 0.5 * Math.Atan2(2 * sxy, sxx - syy);
            vx = Math.Cos(th); vy = Math.Sin(th);
        }

        static double Perp(double px, double py, double x0, double y0, double vx, double vy)
        {
            double len = Math.Sqrt(vx * vx + vy * vy);
            if (len < 1e-9) return double.MaxValue;
            return Math.Abs((px - x0) * vy - (py - y0) * vx) / len;
        }

        /// <summary>残差的中位绝对偏差（MAD），用来估稳健尺度。</summary>
        static double Mad(IList<Cv.Point2f> pts, double cx, double cy, double vx, double vy)
        {
            if (pts.Count == 0) return 0;
            var d = new double[pts.Count];
            for (int i = 0; i < pts.Count; i++) d[i] = Perp(pts[i].X, pts[i].Y, cx, cy, vx, vy);
            Array.Sort(d);
            return d[d.Length / 2];
        }

        static double PerpDist(Cv.Point2f p, Cv.Line2D l)
        {
            double n = Math.Sqrt(l.Vx * l.Vx + l.Vy * l.Vy);
            if (n < 1e-9) return double.MaxValue;
            return Math.Abs((p.X - l.X1) * l.Vy - (p.Y - l.Y1) * l.Vx) / n;
        }

        // ===================== ⑤ 圆拟合 =====================

        /// <summary>Kasa 代数解起值 → Gauss-Newton 对几何半径残差迭代 → 按残差剔群再来一轮。</summary>
        public static CircleResult FitCircle(IList<Cv.Point2f> pts, double keepRatio = 0.9)
        {
            var r = new CircleResult();
            if (pts == null || pts.Count < 3) { r.Fail = "边缘点太少（圆至少 3 个）"; return r; }
            var work = new List<Cv.Point2f>(pts);

            for (int round = 0; round < 3; round++)
            {
                if (!Kasa(work, out double cx, out double cy, out double rad))
                { r.Fail = "这些点拟不出圆（太接近一条直线）"; return r; }

                for (int it = 0; it < 15; it++)
                {
                    // 残差模型：f_i = |p_i - c| - R；∂f/∂c = -(ux, uy)，∂f/∂R = -1
                    double a00 = 0, a01 = 0, a02 = 0, a11 = 0, a12 = 0, a22 = 0, b0 = 0, b1 = 0, b2 = 0;
                    foreach (var p in work)
                    {
                        double dx = p.X - cx, dy = p.Y - cy;
                        double d = Math.Sqrt(dx * dx + dy * dy);
                        if (d < 1e-9) continue;
                        double ux = dx / d, uy = dy / d;
                        double j0 = -ux, j1 = -uy, j2 = -1.0;
                        double f = d - rad;
                        a00 += j0 * j0; a01 += j0 * j1; a02 += j0 * j2;
                        a11 += j1 * j1; a12 += j1 * j2; a22 += j2 * j2;
                        b0 += -j0 * f; b1 += -j1 * f; b2 += -j2 * f;
                    }
                    if (!Solve3(a00, a01, a02, a01, a11, a12, a02, a12, a22, ref b0, ref b1, ref b2)) break;
                    cx += b0; cy += b1; rad += b2;
                    if (Math.Abs(b0) + Math.Abs(b1) + Math.Abs(b2) < 1e-5) break;
                }

                var res = new double[work.Count];
                for (int i = 0; i < work.Count; i++)
                {
                    double dx = work[i].X - cx, dy = work[i].Y - cy;
                    res[i] = Math.Abs(Math.Sqrt(dx * dx + dy * dy) - rad);
                }
                double lim = Percentile(res, round == 0 ? 0.95 : keepRatio);
                var next = new List<Cv.Point2f>();
                for (int i = 0; i < work.Count; i++) if (res[i] <= Math.Max(lim, 0.4)) next.Add(work[i]);
                bool changed = next.Count >= 3 && next.Count != work.Count;
                if (changed) work = next;

                double sum = 0, mx = 0;
                foreach (var p in work)
                {
                    double d = Math.Abs(Math.Sqrt((p.X - cx) * (p.X - cx) + (p.Y - cy) * (p.Y - cy)) - rad);
                    sum += d * d; if (d > mx) mx = d;
                }
                r.Cx = cx; r.Cy = cy; r.R = rad;
                r.Rms = work.Count > 0 ? Math.Sqrt(sum / work.Count) : 0;
                r.UsedPoints = work.Count;
                ArcSpan(work, cx, cy, out r.StartDeg, out r.EndDeg);
                if (!changed) break;
            }

            if (r.R <= 0.5 || double.IsNaN(r.R) || double.IsInfinity(r.R))
            { r.Fail = "拟出来的圆半径不合理（太小或发散）"; return r; }
            r.Ok = true;
            return r;
        }

        /// <summary>
        /// 稳健找圆：RANSAC 定出内点最多的圆，再按策略精修。
        /// fixedRadius &gt; 0 时半径固定（三点定圆退化成两点 + 给定半径）；
        /// radiusMin / radiusMax &gt; 0 时只接受落在这个范围内的半径（先验半径上下界）。
        /// 其余参数语义与 <see cref="FitLineRobust"/> 一致。
        /// </summary>
        public static CircleResult FitCircleRobust(IList<Cv.Point2f> pts, RobustFit mode, double inlierTol,
            int minInliers, double fixedRadius, double radiusMin, double radiusMax, int maxIter = 400)
        {
            var r = new CircleResult();
            int n = pts?.Count ?? 0;
            if (n < 3) { r.Fail = "边缘点太少（圆至少 3 个）"; return r; }
            inlierTol = Math.Max(0.05, inlierTol);
            var src = new List<Cv.Point2f>(pts);
            int need = Math.Max(3, Math.Min(minInliers, n));
            bool fixR = fixedRadius > 0.5;

            // ① RANSAC：随机取 3 点定圆；固定半径时取 2 点 + 半径定圆心（两个候选都试）
            double bcx = 0, bcy = 0, bR = 0;
            int best = -1;
            var rnd = new Random(20261117);
            int iter = Math.Min(maxIter, 60 + n * 4);
            for (int k = 0; k < iter; k++)
            {
                if (fixR)
                {
                    int i = rnd.Next(n), j = rnd.Next(n);
                    if (i == j) continue;
                    double midX = (src[i].X + src[j].X) / 2, midY = (src[i].Y + src[j].Y) / 2;
                    double dx = src[j].X - src[i].X, dy = src[j].Y - src[i].Y;
                    double d = Math.Sqrt(dx * dx + dy * dy);
                    if (d < 1e-6 || d > 2 * fixedRadius) continue;
                    double h = Math.Sqrt(Math.Max(0, fixedRadius * fixedRadius - d * d / 4));
                    double nx = -dy / d, ny = dx / d;
                    for (int sg = -1; sg <= 1; sg += 2)
                    {
                        double cx = midX + nx * h * sg, cy = midY + ny * h * sg;
                        int cnt = CountCircleInliers(src, cx, cy, fixedRadius, inlierTol);
                        if (cnt > best) { best = cnt; bcx = cx; bcy = cy; bR = fixedRadius; }
                    }
                    continue;
                }

                int a = rnd.Next(n), b = rnd.Next(n), c = rnd.Next(n);
                if (a == b || b == c || a == c) continue;
                if (!Circle3(src[a], src[b], src[c], out double ccx, out double ccy, out double cr)) continue;
                if (radiusMin > 0 && cr < radiusMin) continue;
                if (radiusMax > 0 && cr > radiusMax) continue;
                int cnt2 = CountCircleInliers(src, ccx, ccy, cr, inlierTol);
                if (cnt2 > best) { best = cnt2; bcx = ccx; bcy = ccy; bR = cr; }
            }
            if (best < need)
            {
                r.Fail = $"稳健拟合没找到足够的内点（{Math.Max(best, 0)}/{n}，至少要 {need} 个）：把「内点最大偏差」调大，或把「边缘响应阈值」调小";
                return r;
            }

            var inl = new List<Cv.Point2f>();
            var outl = new List<Cv.Point2f>();
            foreach (var p in src)
                (Math.Abs(Dist2(p, bcx, bcy) - bR) <= inlierTol ? inl : outl).Add(p);

            // ② 按策略精修
            if (mode != RobustFit.Ransac)
            {
                int rounds = mode == RobustFit.RansacIrls ? 8 : 12;
                var w = new double[inl.Count];
                for (int it = 0; it < rounds; it++)
                {
                    double sigma = MadCircle(inl, bcx, bcy, bR);
                    double c = (mode == RobustFit.RansacIrls ? 4.685 : 3.0) * Math.Max(sigma, inlierTol);
                    if (c < 1e-6) break;
                    for (int i = 0; i < inl.Count; i++)
                    {
                        double d = Math.Abs(Dist2(inl[i], bcx, bcy) - bR) / c;
                        w[i] = mode == RobustFit.RansacIrls
                            ? (Math.Abs(d) >= 1 ? 0 : (1 - d * d) * (1 - d * d))
                            : 1.0 / (1.0 + d * d);
                    }
                    double ox = bcx, oy = bcy, or0 = bR;
                    GnCircle(inl, ref bcx, ref bcy, ref bR, fixR, w, 6);
                    if (Math.Abs(bcx - ox) + Math.Abs(bcy - oy) + Math.Abs(bR - or0) < 1e-5) break;
                }
            }
            else
            {
                GnCircle(inl, ref bcx, ref bcy, ref bR, fixR, null, 12);
            }

            // ③ 最终内外点按「离最终圆多远」重分
            inl.Clear(); outl.Clear();
            foreach (var p in src)
                (Math.Abs(Dist2(p, bcx, bcy) - bR) <= inlierTol ? inl : outl).Add(p);
            if (inl.Count < 3) { r.Fail = "稳健拟合后剩下的内点不够（少于 3 个）"; return r; }
            if (bR <= 0.5 || double.IsNaN(bR) || double.IsInfinity(bR))
            { r.Fail = "拟出来的圆半径不合理（太小或发散）"; return r; }

            double sum = 0, mx = 0;
            foreach (var p in inl)
            {
                double d = Math.Abs(Dist2(p, bcx, bcy) - bR);
                sum += d * d; if (d > mx) mx = d;
            }
            r.Ok = true;
            r.Cx = bcx; r.Cy = bcy; r.R = bR;
            r.Rms = Math.Sqrt(sum / inl.Count);
            r.UsedPoints = inl.Count;
            r.Inliers = inl;
            r.Outliers = outl;
            ArcSpan(inl, bcx, bcy, out r.StartDeg, out r.EndDeg);
            return r;
        }

        /// <summary>圆心 + 半径的 Gauss-Newton 精修（可加权、可固定半径）。</summary>
        static void GnCircle(IList<Cv.Point2f> pts, ref double cx, ref double cy, ref double rad,
            bool fixR, double[]? w, int iters)
        {
            if (pts.Count < 3) return;
            for (int it = 0; it < iters; it++)
            {
                double a00 = 0, a01 = 0, a02 = 0, a11 = 0, a12 = 0, a22 = 0, b0 = 0, b1 = 0, b2 = 0;
                for (int i = 0; i < pts.Count; i++)
                {
                    double wi = w == null ? 1 : w[i];
                    if (wi <= 0) continue;
                    double dx = pts[i].X - cx, dy = pts[i].Y - cy;
                    double d = Math.Sqrt(dx * dx + dy * dy);
                    if (d < 1e-9) continue;
                    double ux = dx / d, uy = dy / d;
                    double f = d - rad;
                    a00 += wi * ux * ux; a01 += wi * ux * uy; a02 += wi * ux;
                    a11 += wi * uy * uy; a12 += wi * uy; a22 += wi;
                    b0 += wi * ux * f; b1 += wi * uy * f; b2 += wi * f;
                }
                if (a00 < 1e-12 && a11 < 1e-12) return;
                if (fixR)
                {
                    double det = a00 * a11 - a01 * a01;
                    if (Math.Abs(det) < 1e-12) return;
                    double dcx = (b0 * a11 - b1 * a01) / det;
                    double dcy = (a00 * b1 - a01 * b0) / det;
                    cx += dcx; cy += dcy;
                    if (Math.Abs(dcx) + Math.Abs(dcy) < 1e-5) return;
                }
                else
                {
                    if (!Solve3(a00, a01, a02, a01, a11, a12, a02, a12, a22, ref b0, ref b1, ref b2)) return;
                    cx += b0; cy += b1; rad += b2;
                    if (Math.Abs(b0) + Math.Abs(b1) + Math.Abs(b2) < 1e-5) return;
                }
            }
        }

        /// <summary>三点定圆（外接圆），三点几乎共线时返回 false。</summary>
        static bool Circle3(Cv.Point2f p1, Cv.Point2f p2, Cv.Point2f p3,
            out double cx, out double cy, out double rad)
        {
            cx = cy = rad = 0;
            double ax = p1.X, ay = p1.Y, bx = p2.X, by = p2.Y, qx = p3.X, qy = p3.Y;
            double d = 2 * (ax * (by - qy) + bx * (qy - ay) + qx * (ay - by));
            if (Math.Abs(d) < 1e-9) return false;
            double a2 = ax * ax + ay * ay, b2 = bx * bx + by * by, q2 = qx * qx + qy * qy;
            cx = (a2 * (by - qy) + b2 * (qy - ay) + q2 * (ay - by)) / d;
            cy = (a2 * (qx - bx) + b2 * (ax - qx) + q2 * (bx - ax)) / d;
            rad = Math.Sqrt((ax - cx) * (ax - cx) + (ay - cy) * (ay - cy));
            return rad > 1e-6 && !double.IsNaN(rad) && !double.IsInfinity(rad);
        }

        static int CountCircleInliers(IList<Cv.Point2f> pts, double cx, double cy, double rad, double tol)
        {
            int cnt = 0;
            foreach (var p in pts) if (Math.Abs(Dist2(p, cx, cy) - rad) <= tol) cnt++;
            return cnt;
        }

        static double Dist2(Cv.Point2f p, double cx, double cy)
            => Math.Sqrt((p.X - cx) * (p.X - cx) + (p.Y - cy) * (p.Y - cy));

        static double MadCircle(IList<Cv.Point2f> pts, double cx, double cy, double rad)
        {
            if (pts.Count == 0) return 0;
            var d = new double[pts.Count];
            for (int i = 0; i < pts.Count; i++) d[i] = Math.Abs(Dist2(pts[i], cx, cy) - rad);
            Array.Sort(d);
            return d[d.Length / 2];
        }

        /// <summary>Kasa 代数圆解：x²+y² + F + D·x + E·y = 0 的最小二乘。</summary>
        static bool Kasa(List<Cv.Point2f> pts, out double cx, out double cy, out double rad)
        {
            cx = cy = rad = 0;
            if (pts.Count < 3) return false;
            double n = pts.Count, sx = 0, sy = 0, sz = 0, sxx = 0, syy = 0, sxy = 0, sxz = 0, syz = 0;
            foreach (var p in pts)
            {
                double x = p.X, y = p.Y, z = x * x + y * y;
                sx += x; sy += y; sz += z;
                sxx += x * x; syy += y * y; sxy += x * y;
                sxz += x * z; syz += y * z;
            }
            double b0 = -sz, b1 = -sxz, b2 = -syz;
            if (!Solve3(n, sx, sy, sx, sxx, sxy, sy, sxy, syy, ref b0, ref b1, ref b2)) return false;
            cx = -b1 / 2; cy = -b2 / 2;
            double rr = cx * cx + cy * cy - b0;
            if (rr <= 0) return false;
            rad = Math.Sqrt(rr);
            return rad > 1e-6;
        }

        /// <summary>只用来判断点是不是「铺在一段小弧上」，供失败原因用。</summary>
        static void ArcSpan(List<Cv.Point2f> pts, double cx, double cy, out double start, out double end)
        {
            start = 0; end = 360;
            if (pts.Count < 2) return;
            var angs = new List<double>();
            foreach (var p in pts) angs.Add(((Math.Atan2(p.Y - cy, p.X - cx) * 180 / Math.PI) + 360) % 360);
            angs.Sort();
            double gap = -1; int at = 0;
            for (int i = 0; i < angs.Count; i++)
            {
                double a = angs[i];
                double b = i + 1 < angs.Count ? angs[i + 1] : angs[0] + 360;
                if (b - a > gap) { gap = b - a; at = i; }
            }
            start = at + 1 < angs.Count ? angs[at + 1] : angs[0];
            end = angs[at];
            if (360 - gap >= 359) { start = 0; end = 360; }
        }

        /// <summary>3×3 对称正定方程组，列主元高斯消元。</summary>
        static bool Solve3(double a00, double a01, double a02, double a10, double a11, double a12,
                           double a20, double a21, double a22, ref double b0, ref double b1, ref double b2)
        {
            double[,] m = { { a00, a01, a02, b0 }, { a10, a11, a12, b1 }, { a20, a21, a22, b2 } };
            for (int c = 0; c < 3; c++)
            {
                int piv = c;
                for (int r0 = c + 1; r0 < 3; r0++) if (Math.Abs(m[r0, c]) > Math.Abs(m[piv, c])) piv = r0;
                if (Math.Abs(m[piv, c]) < 1e-10) return false;
                if (piv != c) for (int k = c; k < 4; k++) { double t = m[c, k]; m[c, k] = m[piv, k]; m[piv, k] = t; }
                for (int r0 = 0; r0 < 3; r0++)
                {
                    if (r0 == c) continue;
                    double f = m[r0, c] / m[c, c];
                    for (int k = c; k < 4; k++) m[r0, k] -= f * m[c, k];
                }
            }
            b0 = m[0, 3] / m[0, 0]; b1 = m[1, 3] / m[1, 1]; b2 = m[2, 3] / m[2, 2];
            return true;
        }

        /// <summary>百分位（线性插值）。</summary>
        public static double Percentile(double[] values, double p)
        {
            if (values == null || values.Length == 0) return 0;
            var a = (double[])values.Clone();
            Array.Sort(a);
            double idx = (a.Length - 1) * Math.Min(Math.Max(p, 0), 1);
            int lo = (int)Math.Floor(idx), hi = (int)Math.Ceiling(idx);
            return lo == hi ? a[lo] : a[lo] + (a[hi] - a[lo]) * (idx - lo);
        }
    }
}

// === NoCodeMotion 视觉流程引擎（OpenCV 实现） | 作者：温⁣启⁠志‌ ‌|​ ​微⁠信​：‎1‎8‎7‎1⁠9‎3​6‏1‎3‎9⁠9⁠ ​|⁣ ‌保⁣留​所⁠有​权‍利‏，⁠请‎勿‏删‍除 ===
