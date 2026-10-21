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

        /// <summary>
        /// 在一条剖面上找满足极性与强度的边缘（亚像素）。
        /// maxEdges = 1 只要最强那条（单边卡尺）；&lt; 0 全要（一条线上有多个台阶时用）。
        /// </summary>
        public static List<ProfileHit> FindEdges(double[] vals, Polarity pol, double minStrength, double stepPx)
        {
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
                double off = Math.Abs(denom) > 1e-9 ? Math.Min(Math.Max(0.5 * (g[i - 1] - g[i + 1]) / denom, -1), 1) : 0;
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
