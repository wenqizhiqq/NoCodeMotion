// === NoCodeMotion 视觉流程引擎（VisionPlus 算子移植 · OpenCV 真实实现） | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cv = OpenCvSharp;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Vision
{
    /// <summary>
    /// VisionPlus 移植：2D 基础 / 几何 / 图像统计 9 个算子的真实实现。
    /// 图像只读（gray / cur / dst 都归调用方），自己新建的 Mat 一律 using/finally 释放。
    /// </summary>
    internal static class VpOps2D
    {
        public static Cv.Mat Run(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            switch (spec.Key)
            {
                case "fixture":       Fixture(spec, s, cur, gray, dst, report, ctx, progress); break;
                case "caliper":       Caliper(spec, s, cur, gray, dst, report, ctx, progress); break;
                case "sharpness":     Sharpness(spec, s, cur, gray, dst, report, ctx, progress); break;
                case "similarity":    Similarity(spec, s, cur, gray, dst, report, ctx, progress); break;
                case "geomcreate":    GeomCreate(spec, s, cur, gray, dst, report, ctx, progress); break;
                case "geomintersect": GeomIntersect(spec, s, cur, gray, dst, report, ctx, progress); break;
                case "geomguide":     GeomGuide(spec, s, cur, gray, dst, report, ctx, progress); break;
                case "hommat":        HomMat(spec, s, cur, gray, dst, report, ctx, progress); break;
                case "morphchain":    MorphChain(spec, s, cur, gray, dst, report, ctx, progress); break;
            }
            return cur; // 这些都是非换图算子，之后仍用原来的当前图
        }

        // ===================== 公共小工具 =====================

        static double Deg(double rad) => rad * 180.0 / Math.PI;
        static double Rad(double deg) => deg * Math.PI / 180.0;

        static void DrawCross(Cv.Mat dst, double x, double y, Cv.Scalar color, int size = 7)
        {
            int ix = (int)Math.Round(x), iy = (int)Math.Round(y);
            dst.Line(ix - size, iy, ix + size, iy, color, 1);
            dst.Line(ix, iy - size, ix, iy + size, color, 1);
        }

        static void Label(Cv.Mat dst, string text, double x, double y, Cv.Scalar color)
        {
            Cv.Cv2.PutText(dst, text, new Cv.Point((int)Math.Round(x) + 6, (int)Math.Round(y) - 6),
                Cv.HersheyFonts.HersheySimplex, 0.5, color, 1);
        }

        /// <summary>取当前标记位：优先手填 / 上一步点 / 圆 / 模板匹配框 / 画面中心。</summary>
        static void ResolveFixturePos(Cv.Mat gray, VisionOpContext ctx, VisionReport report,
            double rx, double ry, double ra, out double x, out double y, out double a)
        {
            x = 0; y = 0; a = 0;
            if (rx != 0 || ry != 0) { x = rx; y = ry; }
            else if (ctx.LastPoint != null) { x = ctx.LastPoint.Value.X; y = ctx.LastPoint.Value.Y; }
            else if (ctx.LastCircle != null) { x = ctx.LastCircle.Cx; y = ctx.LastCircle.Cy; }
            else if (report.Matches.Count > 0)
            {
                var m = report.Matches[0];
                x = m.LeftTopX + m.TemplateWidth / 2.0;
                y = m.LeftTopY + m.TemplateHeight / 2.0;
            }
            else { x = gray.Cols / 2.0; y = gray.Rows / 2.0; }

            if (ra != 0) a = ra;
            else if (!double.IsNaN(ctx.LastAngle)) a = ctx.LastAngle;
            else if (report.Matches.Count > 0) a = report.Matches[0].Angle;
            else a = 0;
        }

        // ===================== ① 位置跟随 =====================

        static void Fixture(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var st = VpUtil.State(ctx);
            string mode = VpUtil.Txt(spec, s, "mode"); if (mode.Length == 0) mode = "设为基准";
            double rx = VpUtil.Num(spec, s, "rx", 0), ry = VpUtil.Num(spec, s, "ry", 0);
            double ra = VpUtil.Num(spec, s, "ra", 0);
            double maxShift = VpUtil.Num(spec, s, "maxShift", 0);

            if (mode == "清除")
            {
                st.HasFixture = false;
                st.RefX = st.RefY = st.RefAngle = 0;
                st.Dx = st.Dy = st.Dr = 0;
                VpUtil.Add(report, s, spec, true, "已清除位置跟随基准，后面的框不再跟着走",
                    0, 0, "基准已清空",
                    ("偏移X", 0), ("偏移Y", 0), ("偏移角度", 0), ("偏移距离", 0), ("跟随状态", 0));
                VpUtil.Progress(progress, spec.Name + "：已清除基准");
                return;
            }

            double cx, cy, ca;
            ResolveFixturePos(gray, ctx, report, rx, ry, ra, out cx, out cy, out ca);

            if (mode == "设为基准")
            {
                st.HasFixture = true;
                st.RefX = cx; st.RefY = cy; st.RefAngle = ca;
                st.Dx = st.Dy = st.Dr = 0;
                dst.Circle((int)Math.Round(cx), (int)Math.Round(cy), 8, VpUtil.Ink, 2);
                DrawCross(dst, cx, cy, VpUtil.Ink);
                Label(dst, "REF", cx, cy, VpUtil.Ink);
                VpUtil.Add(report, s, spec, true,
                    $"已把当前位置 ({VpUtil.F(cx, 1)},{VpUtil.F(cy, 1)})、角度 {VpUtil.F(ca, 1)}° 存为基准，下一步选「跟随当前」",
                    cx, 1, "基准已记录",
                    ("偏移X", 0), ("偏移Y", 0), ("偏移角度", 0), ("偏移距离", 0), ("跟随状态", 1));
                VpUtil.Progress(progress, spec.Name + "：已记下基准位");
                return;
            }

            // 跟随当前
            if (!st.HasFixture)
            {
                VpUtil.Add(report, s, spec, false, "还没设基准，请先跑一次「设为基准」再跟随",
                    0, 0, "缺少基准",
                    ("偏移X", 0), ("偏移Y", 0), ("偏移角度", 0), ("偏移距离", 0), ("跟随状态", 0));
                return;
            }

            double dx = cx - st.RefX, dy = cy - st.RefY;
            double dr = ca - st.RefAngle;
            while (dr > 180) dr -= 360;
            while (dr < -180) dr += 360;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            st.Dx = dx; st.Dy = dy; st.Dr = dr;

            int ax = (int)Math.Round(st.RefX), ay = (int)Math.Round(st.RefY);
            int bx = (int)Math.Round(cx), by = (int)Math.Round(cy);
            dst.Circle(ax, ay, 8, VpUtil.Ink, 2);
            dst.Line(ax, ay, bx, by, VpUtil.Dot, 2);
            dst.Circle(bx, by, 5, VpUtil.Warn, 2);
            DrawCross(dst, cx, cy, VpUtil.Warn);
            double ang = Math.Atan2(by - ay, bx - ax);
            dst.Line(bx, by, bx - (int)(9 * Math.Cos(ang - 0.4)), by - (int)(9 * Math.Sin(ang - 0.4)), VpUtil.Warn, 2);
            dst.Line(bx, by, bx - (int)(9 * Math.Cos(ang + 0.4)), by - (int)(9 * Math.Sin(ang + 0.4)), VpUtil.Warn, 2);
            Label(dst, $"dx{VpUtil.F(dx, 1)},dy{VpUtil.F(dy, 1)}", cx, cy, VpUtil.Warn);

            bool ok = !(maxShift > 0 && dist > maxShift);
            string tail = ok ? "在允许范围内"
                : $"偏了 {VpUtil.F(dist, 1)} 像素，超过上限 {VpUtil.F(maxShift, 1)}，先别动";
            VpUtil.Add(report, s, spec, ok,
                $"相对基准偏了 X {VpUtil.F(dx, 1)} / Y {VpUtil.F(dy, 1)} 像素，转了 {VpUtil.F(dr, 1)}°，距离 {VpUtil.F(dist, 1)}｜{tail}",
                dist, 1, tail,
                ("偏移X", dx), ("偏移Y", dy), ("偏移角度", dr), ("偏移距离", dist), ("跟随状态", ok ? 1 : 0));
            VpUtil.Progress(progress, spec.Name + "：" + tail);
        }

        // ===================== ② 参数化卡尺 =====================

        static void Caliper(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var roi = VpUtil.Roi(spec, s, gray);
            double cx = VpUtil.Num(spec, s, "cx", 0), cy = VpUtil.Num(spec, s, "cy", 0);
            if (cx == 0 && cy == 0) { cx = roi.X + roi.Width / 2.0; cy = roi.Y + roi.Height / 2.0; }
            double angle = VpUtil.Num(spec, s, "angle", 0);
            double length = Math.Max(8, VpUtil.Num(spec, s, "length", 200));
            double width = Math.Max(4, VpUtil.Num(spec, s, "width", 40));
            string polarity = VpUtil.Txt(spec, s, "polarity"); if (polarity.Length == 0) polarity = "任意";
            double minGrad = VpUtil.Num(spec, s, "minGrad", 8);
            int calipers = Math.Max(3, VpUtil.Int(spec, s, "calipers", 60));
            double sigma = VpUtil.Num(spec, s, "sigma", 1);
            string edgeMode = VpUtil.Txt(spec, s, "edgeMode"); if (edgeMode.Length == 0) edgeMode = "单个边";
            string select = VpUtil.Txt(spec, s, "select"); if (select.Length == 0) select = "最强";

            Cv.Mat work = gray.Channels() == 1 ? gray : VpUtil.ToGray(gray);
            var blur = new Cv.Mat();
            try
            {
                if (sigma > 0) Cv.Cv2.GaussianBlur(work, blur, new Cv.Size(0, 0), sigma);
                else work.CopyTo(blur);
                int H = blur.Rows, W = blur.Cols;
                var idx = blur.GetGenericIndexer<byte>();

                // 双线性采样：越界按边缘钳住
                double Sample(double x, double y)
                {
                    x = VpUtil.Clamp(x, 0, W - 1); y = VpUtil.Clamp(y, 0, H - 1);
                    int x0 = (int)x, y0 = (int)y;
                    int x1 = Math.Min(x0 + 1, W - 1), y1 = Math.Min(y0 + 1, H - 1);
                    double fx = x - x0, fy = y - y0;
                    double a = idx[y0, x0], b = idx[y0, x1], c = idx[y1, x0], d = idx[y1, x1];
                    return a * (1 - fx) * (1 - fy) + b * fx * (1 - fy) + c * (1 - fx) * fy + d * fx * fy;
                }

                double rad = Rad(angle);
                double ux = Math.Cos(rad), uy = Math.Sin(rad);   // 沿边方向
                double px = -uy, py = ux;                         // 垂直搜索方向
                int n = (int)Math.Max(8, Math.Round(width));      // 每条探针的采样点数

                var fit = new List<Cv.Point2f>();                  // 用来拟合最佳边的点
                var hitPts = new List<Cv.Point2f>();               // 所有命中的边点
                double sumAmp = 0; int ampCount = 0;
                double pairWidthSum = 0; int pairCount = 0;

                for (int i = 0; i < calipers; i++)
                {
                    double t = calipers > 1 ? (-0.5 + (double)i / (calipers - 1)) : 0.0;
                    double ccx = cx + t * length * ux, ccy = cy + t * length * uy;
                    double sx = ccx - (width / 2) * px, sy = ccy - (width / 2) * py;
                    double ex = ccx + (width / 2) * px, ey = ccy + (width / 2) * py;

                    dst.Line((int)Math.Round(sx), (int)Math.Round(sy),
                             (int)Math.Round(ex), (int)Math.Round(ey), VpUtil.Ink, 1);

                    var prof = new double[n];
                    for (int k = 0; k < n; k++)
                    {
                        double f = n > 1 ? (double)k / (n - 1) : 0.0;
                        prof[k] = Sample(sx + (ex - sx) * f, sy + (ey - sy) * f);
                    }

                    // 中心差分梯度 + 极性 / 幅度筛选
                    var hits = new List<(double f, double amp, double x, double y)>();
                    for (int k = 1; k < n - 1; k++)
                    {
                        double g = prof[k + 1] - prof[k - 1];       // 带方向
                        double amp = Math.Abs(g);
                        if (amp < minGrad) continue;
                        if (polarity == "暗到亮" && g <= 0) continue; // 灰度升高
                        if (polarity == "亮到暗" && g >= 0) continue; // 灰度降低
                        double f = (double)k / (n - 1);
                        hits.Add((f, amp, sx + (ex - sx) * f, sy + (ey - sy) * f));
                    }
                    if (hits.Count == 0) continue;

                    if (edgeMode == "一对边")
                    {
                        // 取最外侧两条边（跨进 / 跨出），量厚度
                        if (hits.Count >= 2)
                        {
                            var first = hits[0];
                            var last = hits[hits.Count - 1];
                            double wpx = Math.Sqrt((last.x - first.x) * (last.x - first.x)
                                                 + (last.y - first.y) * (last.y - first.y));
                            pairWidthSum += wpx; pairCount++;
                            fit.Add(new Cv.Point2f((float)first.x, (float)first.y));
                            hitPts.Add(new Cv.Point2f((float)first.x, (float)first.y));
                            hitPts.Add(new Cv.Point2f((float)last.x, (float)last.y));
                            sumAmp += first.amp + last.amp; ampCount += 2;
                            dst.Circle((int)Math.Round(first.x), (int)Math.Round(first.y), 2, VpUtil.Warn, 1);
                            dst.Circle((int)Math.Round(last.x), (int)Math.Round(last.y), 2, VpUtil.Warn, 1);
                        }
                    }
                    else if (select == "全部")
                    {
                        foreach (var h in hits)
                        {
                            fit.Add(new Cv.Point2f((float)h.x, (float)h.y));
                            hitPts.Add(new Cv.Point2f((float)h.x, (float)h.y));
                            sumAmp += h.amp; ampCount++;
                            dst.Circle((int)Math.Round(h.x), (int)Math.Round(h.y), 2, VpUtil.Warn, 1);
                        }
                    }
                    else
                    {
                        var best = hits[0];
                        if (select == "第一个") best = hits[0];
                        else if (select == "最后一个") best = hits[hits.Count - 1];
                        else
                        {
                            foreach (var h in hits) if (h.amp > best.amp) best = h; // 最强
                        }
                        fit.Add(new Cv.Point2f((float)best.x, (float)best.y));
                        hitPts.Add(new Cv.Point2f((float)best.x, (float)best.y));
                        sumAmp += best.amp; ampCount++;
                        dst.Circle((int)Math.Round(best.x), (int)Math.Round(best.y), 2, VpUtil.Warn, 1);
                    }
                }

                if (fit.Count < 2)
                {
                    VpUtil.Add(report, s, spec, false,
                        "没找到满足条件的边：调小「边要有多明显」或调大「垂直找多宽」，再检查「边的明暗」方向",
                        0, hitPts.Count, "未找到边",
                        ("边的位置", 0), ("两条边的宽度", 0), ("边缘幅度", 0),
                        ("找到的边数", hitPts.Count), ("评分", 0), ("用到的点数", fit.Count));
                    VpUtil.Progress(progress, spec.Name + "：没找到边");
                    return;
                }

                // 正交回归（总体最小二乘）拟合直线，任意朝向都稳
                double mx = 0, my = 0;
                foreach (var p in fit) { mx += p.X; my += p.Y; }
                mx /= fit.Count; my /= fit.Count;
                double sxx = 0, sxy = 0, syy = 0;
                foreach (var p in fit) { double a = p.X - mx, b = p.Y - my; sxx += a * a; sxy += a * b; syy += b * b; }
                double theta = 0.5 * Math.Atan2(2 * sxy, sxx - syy);
                double ldx = Math.Cos(theta), ldy = Math.Sin(theta);
                double ss = 0;
                foreach (var p in fit) { double d = (p.X - mx) * (-ldy) + (p.Y - my) * ldx; ss += d * d; }
                double rms = Math.Sqrt(ss / fit.Count);

                // 边的位置 = 相对搜索带中心、沿垂直方向的平均带符号偏移
                double meanOffset = 0;
                foreach (var p in fit) meanOffset += (p.X - cx) * px + (p.Y - cy) * py;
                meanOffset /= fit.Count;
                double meanAmp = ampCount > 0 ? sumAmp / ampCount : 0;
                double avgWidth = pairCount > 0 ? pairWidthSum / pairCount : 0;
                double score = VpUtil.Clamp(
                    (meanAmp / (meanAmp + minGrad)) * (1.0 / (1.0 + rms / Math.Max(4, width))), 0, 1);

                double hL = length / 2;
                int x1 = (int)Math.Round(mx - ldx * hL), y1 = (int)Math.Round(my - ldy * hL);
                int x2 = (int)Math.Round(mx + ldx * hL), y2 = (int)Math.Round(my + ldy * hL);
                dst.Line(x1, y1, x2, y2, VpUtil.Ink, 2);
                Label(dst, edgeMode == "一对边" ? $"W={VpUtil.F(avgWidth, 1)}" : "edge", x1, y1, VpUtil.Ink);

                string summary = edgeMode == "一对边"
                    ? $"一对边：平均宽 {VpUtil.F(avgWidth, 1)} 像素｜幅度 {VpUtil.F(meanAmp, 1)}｜{hitPts.Count} 个边点｜直线偏差 {VpUtil.F(rms, 2)}"
                    : $"单边：位置偏 {VpUtil.F(meanOffset, 1)} 像素｜幅度 {VpUtil.F(meanAmp, 1)}｜{hitPts.Count} 个边点｜直线偏差 {VpUtil.F(rms, 2)}";
                VpUtil.Add(report, s, spec, true, summary,
                    edgeMode == "一对边" ? avgWidth : meanOffset, hitPts.Count, edgeMode,
                    ("边的位置", meanOffset), ("两条边的宽度", avgWidth), ("边缘幅度", meanAmp),
                    ("找到的边数", hitPts.Count), ("评分", score), ("用到的点数", fit.Count));
                VpUtil.Progress(progress, spec.Name + "：" + summary);

                // 边点留给后面的拟合 / 对位算子复用
                ctx.Points.Clear();
                ctx.Points.AddRange(hitPts);
                if (ctx.Points.Count > 0) ctx.LastPoint = ctx.Points[0];
            }
            finally
            {
                blur.Dispose();
                if (!ReferenceEquals(work, gray)) work.Dispose();
            }
        }

        // ===================== ③ 清晰度评分 =====================

        static void Sharpness(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var roi = VpUtil.Roi(spec, s, gray);
            using var sub = new Cv.Mat(gray, roi);
            string method = VpUtil.Txt(spec, s, "method"); if (method.Length == 0) method = "拉普拉斯方差";
            double minScore = VpUtil.Num(spec, s, "minScore", 100);

            Cv.Mat g = sub.Channels() == 1 ? sub : VpUtil.ToGray(sub);
            double score = 0;
            try
            {
                if (method == "Tenengrad" || method == "梯度均值")
                {
                    using var gx = new Cv.Mat();
                    using var gy = new Cv.Mat();
                    Cv.Cv2.Sobel(g, gx, Cv.MatType.CV_32F, 1, 0, 3);
                    Cv.Cv2.Sobel(g, gy, Cv.MatType.CV_32F, 0, 1, 3);
                    double acc = 0; long cnt = 0;
                    for (int y = 0; y < gx.Rows; y++)
                        for (int x = 0; x < gx.Cols; x++)
                        {
                            double a = gx.At<float>(y, x), b = gy.At<float>(y, x);
                            acc += method == "Tenengrad" ? (a * a + b * b) : Math.Sqrt(a * a + b * b);
                            cnt++;
                        }
                    score = cnt > 0 ? acc / cnt : 0;
                }
                else // 拉普拉斯方差
                {
                    using var lap = new Cv.Mat();
                    Cv.Cv2.Laplacian(g, lap, Cv.MatType.CV_64F);
                    Cv.Cv2.MeanStdDev(lap, out _, out Cv.Scalar sd);
                    score = sd.Val0 * sd.Val0;
                }
            }
            finally { if (!ReferenceEquals(g, sub)) g.Dispose(); }

            bool ok = minScore <= 0 || score >= minScore;
            dst.Rectangle(roi, ok ? VpUtil.Ink : VpUtil.Warn, 1);
            Cv.Cv2.PutText(dst, $"{method}={VpUtil.F(score, 1)}",
                new Cv.Point(roi.X + 4, Math.Max(14, roi.Y + 18)),
                Cv.HersheyFonts.HersheySimplex, 0.5, ok ? VpUtil.Ink : VpUtil.Warn, 1);
            string tail = ok ? "合格" : $"不合格（低于及格线 {VpUtil.F(minScore, 1)}）";
            VpUtil.Add(report, s, spec, ok, $"{method} 清晰度 {VpUtil.F(score, 2)}｜{tail}",
                score, 0, method,
                ("清晰度评分", score), ("是否合格", ok ? 1 : 0), ("阈值", minScore));
            VpUtil.Progress(progress, spec.Name + "：" + tail);
        }

        // ===================== ④ 两图相似度 =====================

        /// <summary>把单通道灰度图读成一维 double 数组（GetGenericIndexer：行,列）。</summary>
        static double[] GrayArray(Cv.Mat g)
        {
            Cv.Mat one = g.Channels() == 1 ? g : VpUtil.ToGray(g);
            try
            {
                int h = one.Rows, w = one.Cols;
                var a = new double[h * w];
                var idx = one.GetGenericIndexer<byte>();
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        a[y * w + x] = idx[y, x];
                return a;
            }
            finally { if (!ReferenceEquals(one, g)) one.Dispose(); }
        }

        static double Ncc(Cv.Mat a, Cv.Mat b)
        {
            double[] A = GrayArray(a), B = GrayArray(b);
            int n = Math.Min(A.Length, B.Length);
            if (n == 0) return 0;
            double ma = 0, mb = 0;
            for (int i = 0; i < n; i++) { ma += A[i]; mb += B[i]; }
            ma /= n; mb /= n;
            double num = 0, da = 0, db = 0;
            for (int i = 0; i < n; i++)
            {
                double x = A[i] - ma, y = B[i] - mb;
                num += x * y; da += x * x; db += y * y;
            }
            double den = Math.Sqrt(da * db);
            if (den < 1e-9) return (da < 1e-9 && db < 1e-9) ? 1.0 : 0.0; // 两张纯色图
            return VpUtil.Clamp(num / den, -1, 1);
        }

        static double HistCorrelation(Cv.Mat a, Cv.Mat b)
        {
            double[] A = GrayArray(a), B = GrayArray(b);
            var ha = new double[256]; var hb = new double[256];
            for (int i = 0; i < A.Length; i++) ha[(int)VpUtil.Clamp(A[i], 0, 255)]++;
            for (int i = 0; i < B.Length; i++) hb[(int)VpUtil.Clamp(B[i], 0, 255)]++;
            double ma = 0, mb = 0;
            for (int k = 0; k < 256; k++) { ma += ha[k]; mb += hb[k]; }
            ma /= 256; mb /= 256;
            double num = 0, da = 0, db = 0;
            for (int k = 0; k < 256; k++)
            {
                double x = ha[k] - ma, y = hb[k] - mb;
                num += x * y; da += x * x; db += y * y;
            }
            double den = Math.Sqrt(da * db);
            return den < 1e-9 ? 1.0 : VpUtil.Clamp(num / den, -1, 1);
        }

        /// <summary>整块 ROI 当成一个窗口的全局 SSIM（均值 / 方差 / 协方差解析式）。</summary>
        static double Ssim(Cv.Mat a, Cv.Mat b)
        {
            double[] A = GrayArray(a), B = GrayArray(b);
            int n = Math.Min(A.Length, B.Length);
            if (n == 0) return 0;
            const double c1 = 6.5025, c2 = 58.5225; // (0.01*255)^2, (0.03*255)^2
            double sa = 0, sb = 0, saa = 0, sbb = 0, sab = 0;
            for (int i = 0; i < n; i++)
            {
                double x = A[i], y = B[i];
                sa += x; sb += y; saa += x * x; sbb += y * y; sab += x * y;
            }
            double ma = sa / n, mb = sb / n;
            double va = saa / n - ma * ma, vb = sbb / n - mb * mb, cov = sab / n - ma * mb;
            double num = (2 * ma * mb + c1) * (2 * cov + c2);
            double den = (ma * ma + mb * mb + c1) * (va + vb + c2);
            return den < 1e-9 ? 1.0 : VpUtil.Clamp(num / den, -1, 1);
        }

        static void Similarity(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var st = VpUtil.State(ctx);
            string mode = VpUtil.Txt(spec, s, "mode"); if (mode.Length == 0) mode = "设为参考图";
            var roi = VpUtil.Roi(spec, s, cur);
            string method = VpUtil.Txt(spec, s, "method"); if (method.Length == 0) method = "灰度NCC";
            double minScore = VpUtil.Num(spec, s, "minScore", 0.85);

            using var bgr = VpUtil.ToBgr(cur);
            using var crop = new Cv.Mat(bgr, roi);

            if (mode == "设为参考图")
            {
                st.RefImage?.Dispose();
                st.RefImage = crop.Clone(); // 独立拷贝：cur 之后被换掉也不失效
                dst.Rectangle(roi, VpUtil.Ink, 2);
                Cv.Cv2.PutText(dst, "REF", new Cv.Point(roi.X + 4, Math.Max(14, roi.Y + 18)),
                    Cv.HersheyFonts.HersheySimplex, 0.6, VpUtil.Ink, 2);
                VpUtil.Add(report, s, spec, true,
                    $"已把当前 ROI（{roi.Width}×{roi.Height}）存成参考图，下一步选「比对当前图」",
                    1, 1, "参考图已存",
                    ("相似度", 1), ("是否合格", 1), ("阈值", minScore));
                VpUtil.Progress(progress, spec.Name + "：已存参考图");
                return;
            }

            if (st.RefImage == null || st.RefImage.Empty())
            {
                VpUtil.Add(report, s, spec, false, "还没存参考图，请先跑一次「设为参考图」",
                    0, 0, "缺少参考图",
                    ("相似度", 0), ("是否合格", 0), ("阈值", minScore));
                return;
            }

            Cv.Mat curAligned = crop;
            Cv.Mat? resized = null;
            if (crop.Size() != st.RefImage.Size())
            {
                resized = new Cv.Mat();
                Cv.Cv2.Resize(crop, resized, st.RefImage.Size());
                curAligned = resized;
            }
            double sim;
            try
            {
                using var refGray = VpUtil.ToGray(st.RefImage);
                using var curGray = VpUtil.ToGray(curAligned);
                if (method == "直方图") sim = HistCorrelation(refGray, curGray);
                else if (method == "结构相似度") sim = Ssim(refGray, curGray);
                else sim = Ncc(refGray, curGray);
            }
            finally { resized?.Dispose(); }

            sim = VpUtil.Clamp(sim, 0, 1);
            bool ok = sim >= minScore;
            dst.Rectangle(roi, ok ? VpUtil.Ink : VpUtil.Warn, 2);
            Cv.Cv2.PutText(dst, $"{method}={VpUtil.F(sim, 3)}",
                new Cv.Point(roi.X + 4, Math.Max(14, roi.Y + 18)),
                Cv.HersheyFonts.HersheySimplex, 0.5, ok ? VpUtil.Ink : VpUtil.Warn, 1);
            string tail = ok ? "合格" : $"不合格（低于及格线 {VpUtil.F(minScore, 2)}）";
            VpUtil.Add(report, s, spec, ok, $"{method} 相似度 {VpUtil.F(sim, 4)}｜{tail}",
                sim, 0, method,
                ("相似度", sim), ("是否合格", ok ? 1 : 0), ("阈值", minScore));
            VpUtil.Progress(progress, spec.Name + "：" + tail);
        }

        // ===================== ⑤ 按数值造点 / 线 / 圆 =====================

        static void GeomCreate(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            string kind = VpUtil.Txt(spec, s, "kind"); if (kind.Length == 0) kind = "点";
            double x1 = VpUtil.Num(spec, s, "x1", 0), y1 = VpUtil.Num(spec, s, "y1", 0);
            double x2 = VpUtil.Num(spec, s, "x2", 100), y2 = VpUtil.Num(spec, s, "y2", 0);
            double cx = VpUtil.Num(spec, s, "cx", 0), cy = VpUtil.Num(spec, s, "cy", 0);
            double radius = VpUtil.Num(spec, s, "radius", 50);
            var st = VpUtil.State(ctx);

            double px = 0, py = 0, lineLen = 0, angleDeg = 0, r = 0;
            int created = 0;

            if (kind == "直线")
            {
                lineLen = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                angleDeg = Deg(Math.Atan2(y2 - y1, x2 - x1));
                var p1 = new Cv.Point2f((float)x1, (float)y1);
                var p2 = new Cv.Point2f((float)x2, (float)y2);
                ctx.Points.Add(p1); ctx.Points.Add(p2);
                st.Created.Add(p1); st.Created.Add(p2);
                ctx.LastPoint = p1; ctx.LastAngle = angleDeg;
                dst.Line((int)Math.Round(x1), (int)Math.Round(y1),
                         (int)Math.Round(x2), (int)Math.Round(y2), VpUtil.Ink, 2);
                dst.Circle((int)Math.Round(x1), (int)Math.Round(y1), 3, VpUtil.Warn, 1);
                dst.Circle((int)Math.Round(x2), (int)Math.Round(y2), 3, VpUtil.Warn, 1);
                px = (x1 + x2) / 2; py = (y1 + y2) / 2; created = 2;
            }
            else if (kind == "圆")
            {
                r = Math.Max(1, radius);
                var c = new Cv.Point2f((float)cx, (float)cy);
                ctx.Points.Add(c); st.Created.Add(c);
                ctx.LastPoint = c;
                dst.Circle((int)Math.Round(cx), (int)Math.Round(cy), (int)Math.Round(r), VpUtil.Ink, 2);
                DrawCross(dst, cx, cy, VpUtil.Ink);
                px = cx; py = cy; created = 1;
            }
            else
            {
                kind = "点";
                var p = new Cv.Point2f((float)x1, (float)y1);
                ctx.Points.Add(p); st.Created.Add(p);
                ctx.LastPoint = p;
                dst.Circle((int)Math.Round(x1), (int)Math.Round(y1), 4, VpUtil.Warn, 2);
                DrawCross(dst, x1, y1, VpUtil.Ink);
                px = x1; py = y1; created = 1;
            }
            Label(dst, kind, px, py, VpUtil.Ink);

            string summary = kind == "直线"
                ? $"造了一条线 ({VpUtil.F(x1, 1)},{VpUtil.F(y1, 1)})→({VpUtil.F(x2, 1)},{VpUtil.F(y2, 1)})，长 {VpUtil.F(lineLen, 1)}，角 {VpUtil.F(angleDeg, 1)}°"
                : kind == "圆"
                ? $"造了一个圆，圆心 ({VpUtil.F(cx, 1)},{VpUtil.F(cy, 1)})，半径 {VpUtil.F(r, 1)}"
                : $"造了一个点 ({VpUtil.F(x1, 1)},{VpUtil.F(y1, 1)})";
            VpUtil.Add(report, s, spec, true, summary,
                kind == "直线" ? lineLen : (kind == "圆" ? r : 1), created, kind,
                ("点X", px), ("点Y", py), ("线长", lineLen), ("角度", angleDeg), ("半径", r));
            VpUtil.Progress(progress, spec.Name + "：" + summary);
        }

        // ===================== ⑥ 求交点（线线 / 线圆 / 圆圆） =====================

        static void GeomIntersect(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            string kind = VpUtil.Txt(spec, s, "kind"); if (kind.Length == 0) kind = "线线";
            bool fallback = VpUtil.Bool(spec, s, "fallback", false);
            double x1 = VpUtil.Num(spec, s, "x1", 0), y1 = VpUtil.Num(spec, s, "y1", 0);
            double x2 = VpUtil.Num(spec, s, "x2", 100), y2 = VpUtil.Num(spec, s, "y2", 0);
            double x3 = VpUtil.Num(spec, s, "x3", 0), y3 = VpUtil.Num(spec, s, "y3", 0);
            double x4 = VpUtil.Num(spec, s, "x4", 100), y4 = VpUtil.Num(spec, s, "y4", 100);

            // 归一表示：线 = (a1x,a1y)-(a2x,a2y)；圆 = 圆心 (a1x,a1y) + 半径 a2x。B 同理。
            double a1x, a1y, a2x, a2y, b1x, b1y, b2x, b2y;
            string source = "手填坐标";
            a1x = x1; a1y = y1; a2x = x2; a2y = y2;
            b1x = x3; b1y = y3; b2x = x4; b2y = y4;

            if (!fallback)
            {
                if (kind == "线线" && ctx.Lines.Count >= 2)
                {
                    var A = ctx.Lines[ctx.Lines.Count - 2]; var B = ctx.Lines[ctx.Lines.Count - 1];
                    a1x = A.X1; a1y = A.Y1; a2x = A.X2; a2y = A.Y2;
                    b1x = B.X1; b1y = B.Y1; b2x = B.X2; b2y = B.Y2;
                    source = "上下文最近两条线";
                }
                else if (kind == "线线" && ctx.Points.Count >= 4)
                {
                    a1x = ctx.Points[0].X; a1y = ctx.Points[0].Y;
                    a2x = ctx.Points[1].X; a2y = ctx.Points[1].Y;
                    b1x = ctx.Points[2].X; b1y = ctx.Points[2].Y;
                    b2x = ctx.Points[3].X; b2y = ctx.Points[3].Y;
                    source = "上下文点列前四点";
                }
                else if (kind == "线圆" && ctx.Lines.Count >= 1 && ctx.Circles.Count >= 1)
                {
                    var L = ctx.Lines[ctx.Lines.Count - 1]; var C = ctx.Circles[ctx.Circles.Count - 1];
                    a1x = L.X1; a1y = L.Y1; a2x = L.X2; a2y = L.Y2;
                    b1x = C.Cx; b1y = C.Cy; b2x = C.R; b2y = 0;
                    source = "上下文最近线+圆";
                }
                else if (kind == "圆圆" && ctx.Circles.Count >= 2)
                {
                    var A = ctx.Circles[ctx.Circles.Count - 2]; var B = ctx.Circles[ctx.Circles.Count - 1];
                    a1x = A.Cx; a1y = A.Cy; a2x = A.R; a2y = 0;
                    b1x = B.Cx; b1y = B.Cy; b2x = B.R; b2y = 0;
                    source = "上下文最近两个圆";
                }
            }

            var pts = new List<Cv.Point2f>();
            double angle = 0, gap = 0;

            if (kind == "线线")
            {
                double rdx = a2x - a1x, rdy = a2y - a1y, sdx = b2x - b1x, sdy = b2y - b1y;
                double denom = rdx * sdy - rdy * sdx;
                double lr = Math.Sqrt(rdx * rdx + rdy * rdy), ls = Math.Sqrt(sdx * sdx + sdy * sdy);
                if (lr > 1e-9 && ls > 1e-9)
                {
                    double cosang = (rdx * sdx + rdy * sdy) / (lr * ls);
                    angle = Deg(Math.Acos(VpUtil.Clamp(cosang, -1, 1)));
                    if (angle > 90) angle = 180 - angle; // 取锐角
                }
                if (Math.Abs(denom) < 1e-9)
                {
                    if (lr > 1e-9) gap = Math.Abs((b1x - a1x) * rdy - (b1y - a1y) * rdx) / lr;
                }
                else
                {
                    double qpx = b1x - a1x, qpy = b1y - a1y;
                    double t = (qpx * sdy - qpy * sdx) / denom;
                    pts.Add(new Cv.Point2f((float)(a1x + t * rdx), (float)(a1y + t * rdy)));
                    gap = 0;
                }
                dst.Line((int)a1x, (int)a1y, (int)a2x, (int)a2y, VpUtil.Dot, 1);
                dst.Line((int)b1x, (int)b1y, (int)b2x, (int)b2y, VpUtil.Dot, 1);
            }
            else if (kind == "线圆")
            {
                double rx = a2x - a1x, ry = a2y - a1y;
                double ccx = b1x, ccy = b1y, R = Math.Abs(b2x);
                double fx = a1x - ccx, fy = a1y - ccy;
                double A = rx * rx + ry * ry;
                double B = 2 * (fx * rx + fy * ry);
                double Cc = fx * fx + fy * fy - R * R;
                double disc = B * B - 4 * A * Cc;
                double lr = Math.Sqrt(A);
                if (lr > 1e-9) gap = Math.Abs((ccx - a1x) * ry - (ccy - a1y) * rx) / lr - R;
                if (A > 1e-9 && disc >= -1e-9)
                {
                    double sq = Math.Sqrt(Math.Max(0, disc));
                    double t1 = (-B - sq) / (2 * A), t2 = (-B + sq) / (2 * A);
                    double ix = a1x + t1 * rx, iy = a1y + t1 * ry;
                    pts.Add(new Cv.Point2f((float)ix, (float)iy));
                    if (Math.Abs(t2 - t1) > 1e-9)
                        pts.Add(new Cv.Point2f((float)(a1x + t2 * rx), (float)(a1y + t2 * ry)));
                    double vx = ix - ccx, vy = iy - ccy, vl = Math.Sqrt(vx * vx + vy * vy);
                    if (vl > 1e-9 && lr > 1e-9)
                        angle = Deg(Math.Acos(VpUtil.Clamp((rx * vx + ry * vy) / (lr * vl), -1, 1)));
                }
                dst.Line((int)a1x, (int)a1y, (int)a2x, (int)a2y, VpUtil.Dot, 1);
                dst.Circle((int)ccx, (int)ccy, Math.Max(1, (int)Math.Round(R)), VpUtil.Dot, 1);
            }
            else if (kind == "圆圆")
            {
                double c1x = a1x, c1y = a1y, R1 = Math.Abs(a2x);
                double c2x = b1x, c2y = b1y, R2 = Math.Abs(b2x);
                double dx = c2x - c1x, dy = c2y - c1y;
                double d = Math.Sqrt(dx * dx + dy * dy);
                gap = d - (R1 + R2);
                if (d > 1e-9 && d <= R1 + R2 + 1e-9 && d >= Math.Abs(R1 - R2) - 1e-9)
                {
                    double a = (R1 * R1 - R2 * R2 + d * d) / (2 * d);
                    double h2 = R1 * R1 - a * a;
                    double h = h2 > 0 ? Math.Sqrt(h2) : 0;
                    double xm = c1x + a * dx / d, ym = c1y + a * dy / d;
                    double ox = -dy / d * h, oy = dx / d * h;
                    double ix = xm + ox, iy = ym + oy;
                    pts.Add(new Cv.Point2f((float)ix, (float)iy));
                    if (h > 1e-9) pts.Add(new Cv.Point2f((float)(xm - ox), (float)(ym - oy)));
                    double v1x = ix - c1x, v1y = iy - c1y, v2x = ix - c2x, v2y = iy - c2y;
                    double l1 = Math.Sqrt(v1x * v1x + v1y * v1y), l2 = Math.Sqrt(v2x * v2x + v2y * v2y);
                    if (l1 > 1e-9 && l2 > 1e-9)
                        angle = Deg(Math.Acos(VpUtil.Clamp((v1x * v2x + v1y * v2y) / (l1 * l2), -1, 1)));
                }
                dst.Circle((int)c1x, (int)c1y, Math.Max(1, (int)Math.Round(R1)), VpUtil.Dot, 1);
                dst.Circle((int)c2x, (int)c2y, Math.Max(1, (int)Math.Round(R2)), VpUtil.Dot, 1);
            }
            else
            {
                VpUtil.Add(report, s, spec, false, "不支持的交点类型：" + kind, 0, 0, kind,
                    ("交点X", 0), ("交点Y", 0), ("交点个数", 0), ("夹角", 0), ("两对象距离", 0));
                return;
            }

            foreach (var p in pts)
            {
                int ix = (int)Math.Round(p.X), iy = (int)Math.Round(p.Y);
                dst.Circle(ix, iy, 5, VpUtil.Warn, 2);
                DrawCross(dst, p.X, p.Y, VpUtil.Ink);
            }
            if (pts.Count > 0)
            {
                ctx.Points.AddRange(pts);
                ctx.LastPoint = pts[0];
            }

            double ix0 = pts.Count > 0 ? pts[0].X : 0, iy0 = pts.Count > 0 ? pts[0].Y : 0;
            bool ok = pts.Count > 0;
            string tail = ok ? $"{pts.Count} 个交点，夹角 {VpUtil.F(angle, 1)}°" : "两对象没有交点（相离或平行）";
            VpUtil.Add(report, s, spec, ok,
                $"{kind}（{source}）：{tail}，两对象距离 {VpUtil.F(gap, 1)}",
                ix0, pts.Count, source,
                ("交点X", ix0), ("交点Y", iy0), ("交点个数", pts.Count), ("夹角", angle), ("两对象距离", gap));
            VpUtil.Progress(progress, spec.Name + "：" + tail);
        }

        // ===================== ⑦ 几何引导线 =====================

        static void GeomGuide(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            double cx = VpUtil.Num(spec, s, "cx", 0), cy = VpUtil.Num(spec, s, "cy", 0);
            if (cx == 0 && cy == 0) { cx = gray.Cols / 2.0; cy = gray.Rows / 2.0; }
            double angle = VpUtil.Num(spec, s, "angle", 0);
            double length = Math.Max(4, VpUtil.Num(spec, s, "length", 200));
            double offset = VpUtil.Num(spec, s, "offset", 0);

            double rad = Rad(angle);
            double ux = Math.Cos(rad), uy = Math.Sin(rad);
            double px = -uy, py = ux;
            double mx = cx + px * offset, my = cy + py * offset;
            double sx = mx - ux * length / 2, sy = my - uy * length / 2;
            double ex = mx + ux * length / 2, ey = my + uy * length / 2;

            dst.Line((int)Math.Round(sx), (int)Math.Round(sy),
                     (int)Math.Round(ex), (int)Math.Round(ey), VpUtil.Ink, 2);
            dst.Circle((int)Math.Round(mx), (int)Math.Round(my), 4, VpUtil.Warn, 2);
            DrawCross(dst, sx, sy, VpUtil.Dot);
            DrawCross(dst, ex, ey, VpUtil.Dot);
            Label(dst, $"{VpUtil.F(angle, 1)}deg L{VpUtil.F(length, 0)}", mx, my, VpUtil.Ink);

            var p1 = new Cv.Point2f((float)sx, (float)sy);
            var p2 = new Cv.Point2f((float)ex, (float)ey);
            ctx.Points.Add(p1); ctx.Points.Add(p2);
            var st = VpUtil.State(ctx);
            st.Created.Add(p1); st.Created.Add(p2);

            VpUtil.Add(report, s, spec, true,
                $"引导线 ({VpUtil.F(sx, 1)},{VpUtil.F(sy, 1)})→({VpUtil.F(ex, 1)},{VpUtil.F(ey, 1)})，角 {VpUtil.F(angle, 1)}°，长 {VpUtil.F(length, 1)}，平移 {VpUtil.F(offset, 1)}",
                length, 2, "引导线",
                ("起点X", sx), ("起点Y", sy), ("终点X", ex), ("终点Y", ey), ("角度", angle), ("长度", length));
            VpUtil.Progress(progress, spec.Name + "：已画引导线");
        }

        // ===================== ⑧ 仿射矩阵运算 =====================
        // 3x3 行主序 double[9]，用 ConditionalWeakTable 按 ctx 挂住，不往 VpState 加字段。
        static readonly ConditionalWeakTable<VisionOpContext, double[]> HomTable = new();
        static readonly object HomLock = new();

        static double[] HomIdentity() => new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
        static double[] HomTranslate(double dx, double dy) => new double[] { 1, 0, dx, 0, 1, dy, 0, 0, 1 };
        static double[] HomRotate(double deg)
        {
            double c = Math.Cos(Rad(deg)), s = Math.Sin(Rad(deg));
            return new double[] { c, -s, 0, s, c, 0, 0, 0, 1 };
        }
        static double[] HomScale(double k) => new double[] { k, 0, 0, 0, k, 0, 0, 0, 1 };

        static double[] HomMul(double[] a, double[] b) // a * b
        {
            var r = new double[9];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < 3; k++) sum += a[i * 3 + k] * b[k * 3 + j];
                    r[i * 3 + j] = sum;
                }
            return r;
        }

        static double[]? HomInvert(double[] m)
        {
            double a = m[0], b = m[1], c = m[3], d = m[4];
            double det = a * d - b * c;
            if (Math.Abs(det) < 1e-12) return null; // 奇异，不可逆
            double ia = d / det, ib = -b / det, ic = -c / det, id = a / det;
            double tx = m[2], ty = m[5];
            return new double[]
            {
                ia, ib, -(ia * tx + ib * ty),
                ic, id, -(ic * tx + id * ty),
                0, 0, 1
            };
        }

        static void HomMat(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            string op = VpUtil.Txt(spec, s, "op"); if (op.Length == 0) op = "平移";
            double dx = VpUtil.Num(spec, s, "dx", 10), dy = VpUtil.Num(spec, s, "dy", 0);
            double angle = VpUtil.Num(spec, s, "angle", 0), scale = VpUtil.Num(spec, s, "scale", 1);
            if (scale <= 0) scale = 1;
            double px = VpUtil.Num(spec, s, "px", 0), py = VpUtil.Num(spec, s, "py", 0);

            double[] snap;
            double tx, ty, det;
            bool ok = true;
            string note = "";
            lock (HomLock)
            {
                if (!HomTable.TryGetValue(ctx, out var stored))
                {
                    stored = HomIdentity();
                    HomTable.Add(ctx, stored);
                }
                double[] M = (double[])stored.Clone();

                switch (op)
                {
                    case "平移": M = HomTranslate(dx, dy); note = "平移，矩阵重置"; break;
                    case "旋转": M = HomRotate(angle); note = "绕原点旋转，矩阵重置"; break;
                    case "缩放": M = HomScale(scale); note = "等比缩放，矩阵重置"; break;
                    case "求逆":
                        {
                            var inv = HomInvert(M);
                            if (inv == null) { ok = false; note = "矩阵不可逆（行列式≈0）"; }
                            else { M = inv; note = "求逆"; }
                            break;
                        }
                    case "合成":
                        {
                            var D = HomMul(HomMul(HomTranslate(dx, dy), HomRotate(angle)), HomScale(scale));
                            M = HomMul(D, M); note = "平移+旋转+缩放，在上一步基础上合成";
                            break;
                        }
                    default: ok = false; note = "未知变换：" + op; break;
                }
                Array.Copy(M, stored, 9); // 写回挂住的那份数组
                snap = (double[])M.Clone();
                tx = snap[0] * px + snap[1] * py + snap[2];
                ty = snap[3] * px + snap[4] * py + snap[5];
                det = snap[0] * snap[4] - snap[1] * snap[3];
            }

            if (!ok)
            {
                VpUtil.Add(report, s, spec, false, note, 0, 0, note,
                    ("a11", snap[0]), ("a12", snap[1]), ("a13", snap[2]),
                    ("a21", snap[3]), ("a22", snap[4]), ("a23", snap[5]),
                    ("变换后X", tx), ("变换后Y", ty), ("行列式", det));
                return;
            }

            VpUtil.Add(report, s, spec, true,
                $"{note}：点 ({VpUtil.F(px, 1)},{VpUtil.F(py, 1)}) → ({VpUtil.F(tx, 1)},{VpUtil.F(ty, 1)})，行列式 {VpUtil.F(det, 3)}",
                tx, 0, note,
                ("a11", snap[0]), ("a12", snap[1]), ("a13", snap[2]),
                ("a21", snap[3]), ("a22", snap[4]), ("a23", snap[5]),
                ("变换后X", tx), ("变换后Y", ty), ("行列式", det));
            VpUtil.Progress(progress, spec.Name + "：" + note);
        }

        // ===================== ⑨ 形态算子链 =====================

        static void MorphChain(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var roi = VpUtil.Roi(spec, s, gray);
            using var sub = new Cv.Mat(gray, roi);
            double thr = VpUtil.Num(spec, s, "thresh", 128);
            string chain = VpUtil.Txt(spec, s, "chain");
            double minArea = VpUtil.Num(spec, s, "minArea", 10);

            var work = new Cv.Mat();
            var bin = new Cv.Mat();
            try
            {
                if (thr <= 0)
                    Cv.Cv2.Threshold(sub, bin, 0, 255, Cv.ThresholdTypes.Binary | Cv.ThresholdTypes.Otsu);
                else
                    Cv.Cv2.Threshold(sub, bin, thr, 255, Cv.ThresholdTypes.Binary);
                bin.CopyTo(work);

                var tokens = chain.Split(new[] { ',', '，', ';', '；', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                int applied = 0;
                foreach (var tok in tokens)
                {
                    var parts = tok.Split(new[] { ':', '：', '=' }, StringSplitOptions.RemoveEmptyEntries);
                    string name = parts[0].Trim().ToLowerInvariant();
                    int k = 3;
                    if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int kk) && kk > 0) k = kk;
                    if (k % 2 == 0) k++;
                    k = Math.Max(1, Math.Min(k, 101));
                    using var kernel = Cv.Cv2.GetStructuringElement(Cv.MorphShapes.Rect, new Cv.Size(k, k));
                    using var tmp = new Cv.Mat();
                    switch (name)
                    {
                        case "erode": case "腐蚀": Cv.Cv2.Erode(work, tmp, kernel); break;
                        case "dilate": case "膨胀": Cv.Cv2.Dilate(work, tmp, kernel); break;
                        case "open": case "开": Cv.Cv2.MorphologyEx(work, tmp, Cv.MorphTypes.Open, kernel); break;
                        case "close": case "闭": Cv.Cv2.MorphologyEx(work, tmp, Cv.MorphTypes.Close, kernel); break;
                        case "gradient": case "grad": Cv.Cv2.MorphologyEx(work, tmp, Cv.MorphTypes.Gradient, kernel); break;
                        case "tophat": Cv.Cv2.MorphologyEx(work, tmp, Cv.MorphTypes.TopHat, kernel); break;
                        case "blackhat": case "bothat": Cv.Cv2.MorphologyEx(work, tmp, Cv.MorphTypes.BlackHat, kernel); break;
                        default: continue; // 不认的操作跳过
                    }
                    tmp.CopyTo(work);
                    applied++;
                }

                double total = Math.Max(1, (double)work.Rows * work.Cols);
                int white = Cv.Cv2.CountNonZero(work);
                double ratio = white / total;

                using var labels = new Cv.Mat();
                using var stats = new Cv.Mat();
                using var cents = new Cv.Mat();
                int n = Cv.Cv2.ConnectedComponentsWithStats(work, labels, stats, cents);
                int blobs = 0, maxArea = 0;
                for (int i = 1; i < n; i++)
                {
                    int a = stats.At<int>(i, 4);
                    if (a < minArea) continue;
                    blobs++;
                    if (a > maxArea) maxArea = a;
                    int x = stats.At<int>(i, 0) + roi.X, y = stats.At<int>(i, 1) + roi.Y;
                    int w = stats.At<int>(i, 2), h = stats.At<int>(i, 3);
                    dst.Rectangle(new Cv.Rect(x, y, w, h), VpUtil.Ink, 1);
                }
                dst.Rectangle(roi, VpUtil.Dot, 1);
                Cv.Cv2.PutText(dst, $"blobs={blobs} white={VpUtil.F(ratio * 100, 1)}%",
                    new Cv.Point(roi.X + 4, Math.Max(14, roi.Y + 18)),
                    Cv.HersheyFonts.HersheySimplex, 0.5, VpUtil.Ink, 1);

                VpUtil.Add(report, s, spec, true,
                    $"跑了 {applied} 步形态链：白区 {white} 像素（占 {VpUtil.F(ratio * 100, 2)}%），合格连通块 {blobs} 个，最大块 {maxArea} 像素",
                    white, blobs, chain,
                    ("白区面积", white), ("白区占比", ratio * 100), ("连通块数", blobs), ("最大块面积", maxArea));
                VpUtil.Progress(progress, spec.Name + $"：{blobs} 个块");
            }
            finally { work.Dispose(); bin.Dispose(); }
        }
    }
}
