// === NoCodeMotion 视觉流程引擎（VisionPlus 算子移植 · OpenCV 真实实现） | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using Cv = OpenCvSharp;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Vision
{
    /// <summary>
    /// VisionPlus 移植：匹配 / 颜色 / 训练式工具 / 标定 9 个算子的真实实现。
    /// cur / gray / dst 都归调用方，绝不 Dispose；自己新建的 Mat 一律 using 释放。
    /// 换图算子（colorlut / pixmap16）返回新建的 Mat，其余一律返回 cur。
    /// </summary>
    internal static class VpOpsMatch
    {
        public static Cv.Mat Run(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            switch (spec.Key)
            {
                case "colormatch":  ColorMatch(spec, s, cur, gray, dst, report, ctx, progress); break;
                case "pminspect":   PmInspect(spec, s, cur, gray, dst, report, ctx, progress); break;
                case "pmalign":     PmAlign(spec, s, cur, gray, dst, report, ctx, progress); break;
                case "easyline":    EasyLine(spec, s, cur, gray, dst, report, ctx, progress); break;
                case "easycircle":  EasyCircle(spec, s, cur, gray, dst, report, ctx, progress); break;
                case "handeye":     HandEye(spec, s, cur, gray, dst, report, ctx, progress); break;
                case "npointcal":   NPointCal(spec, s, cur, gray, dst, report, ctx, progress); break;
                case "colorlut":    return ColorLut(spec, s, cur, gray, dst, report, ctx, progress);
                case "pixmap16":    return PixMap16(spec, s, cur, gray, dst, report, ctx, progress);
            }
            return cur; // 非换图算子：之后仍用原来的当前图
        }

        // ===================== 跨步私有状态（ConditionalWeakTable，不改 VpState / VisualFlowStep） =====================
        // 手眼 / N点标定：点对直接挂在各自的 ConditionalWeakTable 上，按 ctx 隔离，运行完即可回收；
        // 找线 / 找圆：学到的模型参数放在私有 sealed 状态类里。
        private sealed class LineTrain
        {
            public double Cx, Cy, Angle, Length, Width, MinGrad, MinScore;
            public string Polarity = "任意";
            public int Samples;
            public bool Trained;
        }
        private sealed class CircleTrain
        {
            public double Cx, Cy, Radius, Tol, MinGrad, MinScore;
            public string Polarity = "任意";
            public int Samples;
            public bool Trained;
        }
        static readonly ConditionalWeakTable<VisionOpContext, List<(double Ux, double Uy, double Mx, double My)>> HeTable = new();
        static readonly ConditionalWeakTable<VisionOpContext, List<(double Ux, double Uy, double Mx, double My)>> NpTable = new();
        static readonly ConditionalWeakTable<VisionOpContext, LineTrain> LineTable = new();
        static readonly ConditionalWeakTable<VisionOpContext, CircleTrain> CircleTable = new();

        private struct Hit
        {
            public double Cx, Cy, Score, Angle, Scale, W, H;
        }

        // ===================== 公共小工具 =====================
        static double Deg(double rad) => rad * 180.0 / Math.PI;
        static double Rad(double deg) => deg * Math.PI / 180.0;

        static Cv.Scalar ScoreColor(double score)
            => new Cv.Scalar(0, 220 * VpUtil.Clamp(score, 0, 1), 230 * (1 - VpUtil.Clamp(score, 0, 1)));

        /// <summary>把矩形夹到图内，保证可安全用于 new Mat(src, rect)。</summary>
        static Cv.Rect ClampRect(Cv.Rect r, Cv.Mat img)
        {
            int x = (int)Math.Max(0, Math.Min(r.X, img.Cols - 1));
            int y = (int)Math.Max(0, Math.Min(r.Y, img.Rows - 1));
            int w = Math.Max(1, Math.Min(r.Width, img.Cols - x));
            int h = Math.Max(1, Math.Min(r.Height, img.Rows - y));
            return new Cv.Rect(x, y, w, h);
        }

        /// <summary>解析 "x,y,w,h;x,y,w,h"；留空时回落到一个框。</summary>
        static List<Cv.Rect> ParseRegions(string text, Cv.Rect fallback, Cv.Mat img)
        {
            var list = new List<Cv.Rect>();
            foreach (var seg in (text ?? "").Split(new[] { ';', '；', '|', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var p = seg.Split(new[] { ',', '，', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length >= 4
                    && double.TryParse(p[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double x)
                    && double.TryParse(p[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double y)
                    && double.TryParse(p[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double w)
                    && double.TryParse(p[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double h)
                    && w > 2 && h > 2)
                {
                    list.Add(ClampRect(new Cv.Rect((int)Math.Round(x), (int)Math.Round(y),
                        (int)Math.Round(w), (int)Math.Round(h)), img));
                }
            }
            if (list.Count == 0) list.Add(fallback);
            return list;
        }

        /// <summary>把单通道任意深度灰度图规整成 0..255 的 double[]，同时给出原图统计。</summary>
        static double[] NormGray(Cv.Mat gray, out double gmin, out double gmax, out double gmean)
        {
            Cv.Cv2.MinMaxLoc(gray, out gmin, out gmax);
            gmean = Cv.Cv2.Mean(gray).Val0;
            using var n8 = new Cv.Mat();
            Cv.Cv2.Normalize(gray, n8, 0, 255, Cv.NormTypes.MinMax, Cv.MatType.CV_8U);
            int w = n8.Cols, h = n8.Rows;
            var a = new double[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    a[y * w + x] = n8.Get<byte>(y, x);
            return a;
        }

        /// <summary>双线性取灰度（值域 0..255）。</summary>
        static double Sample(double[] a, int w, int h, double x, double y)
        {
            x = VpUtil.Clamp(x, 0, w - 1.001);
            y = VpUtil.Clamp(y, 0, h - 1.001);
            int x0 = (int)x, y0 = (int)y;
            int x1 = Math.Min(x0 + 1, w - 1), y1 = Math.Min(y0 + 1, h - 1);
            double ax = x - x0, ay = y - y0;
            double v00 = a[y0 * w + x0], v10 = a[y0 * w + x1];
            double v01 = a[y1 * w + x0], v11 = a[y1 * w + x1];
            return (v00 * (1 - ax) + v10 * ax) * (1 - ay) + (v01 * (1 - ax) + v11 * ax) * ay;
        }

        // ---- 线性代数：最小二乘正规方程 + 高斯消元（纯 C#，不依赖不确定的 OpenCV 求解 API） ----
        static double[]? GaussSolve(double[,] A, double[] b, int n)
        {
            var M = new double[n, n + 1];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++) M[i, j] = A[i, j];
                M[i, n] = b[i];
            }
            for (int col = 0; col < n; col++)
            {
                int piv = col;
                for (int r = col + 1; r < n; r++)
                    if (Math.Abs(M[r, col]) > Math.Abs(M[piv, col])) piv = r;
                if (Math.Abs(M[piv, col]) < 1e-12) return null; // 退化/奇异
                if (piv != col)
                    for (int c = col; c <= n; c++)
                    { double t = M[piv, c]; M[piv, c] = M[col, c]; M[col, c] = t; }
                for (int r = 0; r < n; r++)
                {
                    if (r == col) continue;
                    double f = M[r, col] / M[col, col];
                    if (f == 0) continue;
                    for (int c = col; c <= n; c++) M[r, c] -= f * M[col, c];
                }
            }
            var x = new double[n];
            for (int i = 0; i < n; i++) x[i] = M[i, n] / M[i, i];
            return x;
        }

        /// <summary>最小二乘：rows 每行 cols 个自变量 → target，解正规方程。</summary>
        static double[]? LeastSquares(double[][] rows, double[] target, int cols)
        {
            int n = rows.Length;
            if (n < cols) return null;
            var A = new double[cols, cols];
            var b = new double[cols];
            for (int i = 0; i < n; i++)
                for (int r = 0; r < cols; r++)
                {
                    b[r] += rows[i][r] * target[i];
                    for (int c = 0; c < cols; c++) A[r, c] += rows[i][r] * rows[i][c];
                }
            return GaussSolve(A, b, cols);
        }

        /// <summary>Kasa 线性化最小二乘拟合圆：x²+y² = a·x + b·y + c。</summary>
        static bool FitCircle(IReadOnlyList<(double X, double Y)> pts, out double cx, out double cy, out double r)
        {
            cx = cy = r = 0;
            if (pts.Count < 3) return false;
            var rows = new double[pts.Count][];
            var tgt = new double[pts.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                rows[i] = new[] { pts[i].X, pts[i].Y, 1.0 };
                tgt[i] = pts[i].X * pts[i].X + pts[i].Y * pts[i].Y;
            }
            var sol = LeastSquares(rows, tgt, 3);
            if (sol == null) return false;
            cx = sol[0] / 2; cy = sol[1] / 2;
            double q = sol[2] + cx * cx + cy * cy;
            if (q <= 0) return false;
            r = Math.Sqrt(q);
            return true;
        }

        static double[,] Mul3(double[,] A, double[,] B)
        {
            var C = new double[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                { double s = 0; for (int k = 0; k < 3; k++) s += A[i, k] * B[k, j]; C[i, j] = s; }
            return C;
        }

        /// <summary>把匹配结果画成旋转矩形（四角用三角算，再用 Cv2.Line 连边）。</summary>
        static void DrawRotRect(Cv.Mat dst, double cx, double cy, double w, double h, double angleDeg, Cv.Scalar col)
        {
            double c = Math.Cos(Rad(angleDeg)), s = Math.Sin(Rad(angleDeg));
            double hw = w / 2, hh = h / 2;
            double[] lx = { -hw, hw, hw, -hw };
            double[] ly = { -hh, -hh, hh, hh };
            var p = new Cv.Point[4];
            for (int i = 0; i < 4; i++)
                p[i] = new Cv.Point((int)Math.Round(cx + lx[i] * c - ly[i] * s),
                                    (int)Math.Round(cy + lx[i] * s + ly[i] * c));
            for (int i = 0; i < 4; i++) Cv.Cv2.Line(dst, p[i], p[(i + 1) % 4], col, 2);
        }

        // ===================== ① 多区域颜色模板匹配 =====================
        /// <summary>BGR→HSV，纯循环统计归一化 H/S 直方图（各 bins 档，拼成 double[2*bins]）。</summary>
        static double[] ColorHist(Cv.Mat bgrCrop, int bins)
        {
            using var hsv = new Cv.Mat();
            Cv.Cv2.CvtColor(bgrCrop, hsv, Cv.ColorConversionCodes.BGR2HSV);
            var planes = Cv.Cv2.Split(hsv); // [0]=H(0..179) [1]=S(0..255) [2]=V，均为 CV_8UC1
            var hist = new double[2 * bins];
            try
            {
                // 用纯循环逐像素读 H/S，避开 CalcHist / Rangef 之类枚举与重载的不确定性
                var hi = planes[0].GetGenericIndexer<byte>();
                var si = planes[1].GetGenericIndexer<byte>();
                int w = hsv.Cols, h = hsv.Rows;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int hb = (int)(hi[y, x] * bins / 180.0);
                        int sb = (int)(si[y, x] * bins / 256.0);
                        if (hb < 0) hb = 0; else if (hb >= bins) hb = bins - 1;
                        if (sb < 0) sb = 0; else if (sb >= bins) sb = bins - 1;
                        hist[hb] += 1.0;
                        hist[bins + sb] += 1.0;
                    }
            }
            finally { foreach (var p in planes) p.Dispose(); }
            double sum = 0;
            for (int i = 0; i < hist.Length; i++) sum += hist[i];
            if (sum > 0) for (int i = 0; i < hist.Length; i++) hist[i] /= sum; // 归一化到总和 1
            return hist;
        }

        /// <summary>两个归一化直方图的 L1 距离（Σ|a-b|，取值 0~2）。</summary>
        static double HistDist(double[] a, double[] b)
        {
            int n = Math.Min(a.Length, b.Length);
            double d = 0;
            for (int i = 0; i < n; i++) d += Math.Abs(a[i] - b[i]);
            return d;
        }

        static void ColorMatch(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            string mode = VpUtil.Txt(spec, s, "mode");
            int bins = Math.Max(4, Math.Min(64, VpUtil.Int(spec, s, "bins", 16)));
            double tol = VpUtil.Num(spec, s, "tol", 0.25);
            if (tol <= 0) tol = 0.25;
            double minScore = VpUtil.Num(spec, s, "minScore", 0.8);
            var roi = VpUtil.Roi(spec, s, cur);
            var boxes = ParseRegions(VpUtil.Txt(spec, s, "regions"), roi, cur);
            var st = VpUtil.State(ctx);

            using var bgr = VpUtil.ToBgr(cur); // 统一成 3 通道 BGR 再做 HSV 直方图

            if (mode != "比对当前图")
            {
                // 学习各区域：每块存一份归一化 H-S 直方图
                st.ColorRegions.Clear();
                foreach (var box in boxes)
                {
                    using var crop = new Cv.Mat(bgr, box);
                    var hist = ColorHist(crop, bins);
                    st.ColorRegions.Add(new VpColorRegion { Box = box, Hist = hist, Weight = 1 });
                    Cv.Cv2.Rectangle(dst, box, VpUtil.Ink, 1);
                }
                VpUtil.Progress(progress, "颜色模板：已学习 " + boxes.Count + " 个区域");
                VpUtil.Add(report, s, spec, true,
                    $"已学习 {boxes.Count} 个区域的颜色模板（每区 H/S 各 {bins} 档）",
                    boxes.Count, boxes.Count, "",
                    ("总分", 1), ("区域数", boxes.Count), ("最低的区分数", 1), ("是否合格", 1));
                return;
            }

            // 比对当前图
            if (st.ColorRegions.Count == 0)
            {
                VpUtil.Add(report, s, spec, false,
                    "还没学习颜色模板：请先在标准件上跑一次「学习各区域」", 0, 0, "",
                    ("总分", 0), ("区域数", 0), ("最低的区分数", 0), ("是否合格", 0));
                return;
            }
            int learnedBins = st.ColorRegions[0].Hist.Length / 2;
            if (learnedBins < 2) learnedBins = bins;
            double sumScore = 0, minRegion = 1;
            for (int k = 0; k < st.ColorRegions.Count; k++)
            {
                var reg = st.ColorRegions[k];
                var box = ClampRect(reg.Box, bgr);
                using var crop = new Cv.Mat(bgr, box);
                var now = ColorHist(crop, learnedBins);
                double dist = HistDist(reg.Hist, now);
                double score = VpUtil.Clamp(1 - dist / tol, 0, 1) * reg.Weight;
                score = VpUtil.Clamp(score, 0, 1);
                sumScore += score;
                if (score < minRegion) minRegion = score;
                Cv.Cv2.Rectangle(dst, box, ScoreColor(score), 2);
                Cv.Cv2.PutText(dst, VpUtil.F(score, 2), new Cv.Point(box.X + 2, box.Y + 14),
                    Cv.HersheyFonts.HersheySimplex, 0.5, score >= minScore ? VpUtil.Ink : VpUtil.Warn, 1);
            }
            double overall = sumScore / st.ColorRegions.Count;
            bool ok = overall >= minScore;
            VpUtil.Add(report, s, spec, ok,
                $"共 {st.ColorRegions.Count} 区，总分 {VpUtil.F(overall)}｜最差一区 {VpUtil.F(minRegion)}｜{(ok ? "合格" : "不合格，颜色有偏差")}",
                overall, st.ColorRegions.Count, "",
                ("总分", overall), ("区域数", st.ColorRegions.Count),
                ("最低的区分数", minRegion), ("是否合格", ok ? 1 : 0));
        }

        // ===================== ② 标准图缺陷比对 =====================
        /// <summary>用中心块 MatchTemplate 估计 cur 相对 ref 的平移量。</summary>
        static (double Dx, double Dy) EstimateShift(Cv.Mat gRef, Cv.Mat gCur)
        {
            int pw = Math.Min(Math.Max(8, gRef.Cols / 2), gCur.Cols);
            int ph = Math.Min(Math.Max(8, gRef.Rows / 2), gCur.Rows);
            pw = Math.Min(pw, gRef.Cols); ph = Math.Min(ph, gRef.Rows);
            if (pw < 4 || ph < 4) return (0, 0);
            int ox = (gRef.Cols - pw) / 2, oy = (gRef.Rows - ph) / 2;
            using var patch = new Cv.Mat(gRef, new Cv.Rect(ox, oy, pw, ph));
            using var res = new Cv.Mat();
            Cv.Cv2.MatchTemplate(gCur, patch, res, Cv.TemplateMatchModes.CCoeffNormed, null);
            Cv.Cv2.MinMaxLoc(res, out _, out _, out _, out Cv.Point peak);
            return (peak.X - ox, peak.Y - oy);
        }

        static void PmInspect(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            string mode = VpUtil.Txt(spec, s, "mode");
            var st = VpUtil.State(ctx);

            if (mode == "设为标准图")
            {
                st.RefImage?.Dispose();
                st.RefImage = VpUtil.ToBgr(cur); // BGR 8U 克隆
                var r0 = VpUtil.Roi(spec, s, cur);
                Cv.Cv2.Rectangle(dst, r0, VpUtil.Ink, 1);
                VpUtil.Add(report, s, spec, true,
                    $"已把当前图存为标准图（{cur.Cols}×{cur.Rows}）", 1, 1, "",
                    ("缺陷处数", 0), ("最大缺陷面积", 0), ("缺陷总面积", 0), ("是否合格", 1));
                return;
            }

            if (st.RefImage == null || st.RefImage.Empty())
            {
                VpUtil.Add(report, s, spec, false,
                    "还没设标准图：请先跑一次「设为标准图」存一张好件", 0, 0, "",
                    ("缺陷处数", 0), ("最大缺陷面积", 0), ("缺陷总面积", 0), ("是否合格", 0));
                return;
            }

            var refImg = st.RefImage;
            double thresh = VpUtil.Num(spec, s, "thresh", 28);
            int k = VpUtil.Int(spec, s, "blur", 3);
            if (k < 0) k = 0; if (k > 31) k = 31; if (k % 2 == 0) k++; if (k < 1) k = 1;
            double minArea = VpUtil.Num(spec, s, "minArea", 12);
            bool align = VpUtil.Bool(spec, s, "align", true);

            using var curB = VpUtil.ToBgr(cur);
            using var work = new Cv.Mat();
            if (curB.Cols != refImg.Cols || curB.Rows != refImg.Rows)
                Cv.Cv2.Resize(curB, work, new Cv.Size(refImg.Cols, refImg.Rows)); // 尺寸不一致：缩放对齐
            else curB.CopyTo(work);

            var roi = ClampRect(VpUtil.Roi(spec, s, refImg), refImg);
            using var refCrop = new Cv.Mat(refImg, roi);
            using var workCrop = new Cv.Mat(work, roi);

            // 可选对齐：估计平移后把当前图搬回标准图坐标系
            using var aligned = new Cv.Mat();
            if (align)
            {
                using var gRef = VpUtil.ToGray(refCrop);
                using var gWork = VpUtil.ToGray(workCrop);
                var sh = EstimateShift(gRef, gWork);
                if (Math.Abs(sh.Dx) > 0.01 || Math.Abs(sh.Dy) > 0.01)
                {
                    using var M = Cv.Cv2.GetRotationMatrix2D(new Cv.Point2f(0, 0), 0, 1.0);
                    M.Set<double>(0, 2, -sh.Dx);
                    M.Set<double>(1, 2, -sh.Dy);
                    Cv.Cv2.WarpAffine(workCrop, aligned, M, workCrop.Size(),
                        Cv.InterpolationFlags.Linear, Cv.BorderTypes.Replicate);
                }
                else workCrop.CopyTo(aligned);
            }
            else workCrop.CopyTo(aligned);

            // blur → AbsDiff → gray → Threshold → ConnectedComponentsWithStats
            using var bRef = new Cv.Mat();
            using var bAligned = new Cv.Mat();
            if (k >= 3)
            {
                Cv.Cv2.GaussianBlur(refCrop, bRef, new Cv.Size(k, k), 0);
                Cv.Cv2.GaussianBlur(aligned, bAligned, new Cv.Size(k, k), 0);
            }
            else { refCrop.CopyTo(bRef); aligned.CopyTo(bAligned); }

            using var diff = new Cv.Mat();
            Cv.Cv2.Absdiff(bRef, bAligned, diff);
            using var gdiff = VpUtil.ToGray(diff);
            using var bin = new Cv.Mat();
            Cv.Cv2.Threshold(gdiff, bin, thresh, 255, Cv.ThresholdTypes.Binary);
            using var labels = new Cv.Mat();
            using var stats = new Cv.Mat();
            using var cents = new Cv.Mat();
            int n = Cv.Cv2.ConnectedComponentsWithStats(bin, labels, stats, cents);

            double sx = dst.Cols / (double)refImg.Cols;
            double sy = dst.Rows / (double)refImg.Rows;
            int defectCount = 0;
            double maxArea = 0, totalArea = 0;
            for (int i = 1; i < n; i++)
            {
                int a = stats.At<int>(i, 4);
                if (a < minArea) continue;
                int x = stats.At<int>(i, 0), y = stats.At<int>(i, 1);
                int w = stats.At<int>(i, 2), h = stats.At<int>(i, 3);
                defectCount++;
                maxArea = Math.Max(maxArea, a);
                totalArea += a;
                var r = new Cv.Rect((int)((x + roi.X) * sx), (int)((y + roi.Y) * sy),
                    Math.Max(1, (int)(w * sx)), Math.Max(1, (int)(h * sy)));
                Cv.Cv2.Rectangle(dst, r, VpUtil.Warn, 2);
                Cv.Cv2.PutText(dst, a.ToString(CultureInfo.InvariantCulture), new Cv.Point(r.X, r.Y - 3),
                    Cv.HersheyFonts.HersheySimplex, 0.4, VpUtil.Warn, 1);
            }
            bool ok = defectCount == 0;
            VpUtil.Add(report, s, spec, ok,
                $"{(ok ? "合格" : "不合格")}：缺陷 {defectCount} 处，最大 {VpUtil.F(maxArea, 0)} 像素，总面积 {VpUtil.F(totalArea, 0)} 像素",
                maxArea, defectCount, "",
                ("缺陷处数", defectCount), ("最大缺陷面积", maxArea),
                ("缺陷总面积", totalArea), ("是否合格", ok ? 1 : 0));
        }

        // ===================== ③ 金字塔轮廓/灰度匹配 =====================
        /// <summary>模板先缩放、再旋转到扩展画布（不裁角），返回新 Mat 由调用方释放。</summary>
        static Cv.Mat RotateScale(Cv.Mat tpl, double angleDeg, double scale)
        {
            int sw = Math.Max(2, (int)Math.Round(tpl.Cols * scale));
            int sh = Math.Max(2, (int)Math.Round(tpl.Rows * scale));
            using var scaled = new Cv.Mat();
            Cv.Cv2.Resize(tpl, scaled, new Cv.Size(sw, sh));
            double c = Math.Abs(Math.Cos(Rad(angleDeg))), s = Math.Abs(Math.Sin(Rad(angleDeg)));
            int nw = Math.Max(2, (int)(sw * c + sh * s));
            int nh = Math.Max(2, (int)(sw * s + sh * c));
            using var M = Cv.Cv2.GetRotationMatrix2D(new Cv.Point2f(sw / 2f, sh / 2f), angleDeg, 1.0);
            M.Set<double>(0, 2, M.At<double>(0, 2) + (nw / 2.0 - sw / 2.0));
            M.Set<double>(1, 2, M.At<double>(1, 2) + (nh / 2.0 - sh / 2.0));
            var rot = new Cv.Mat(nh, nw, Cv.MatType.CV_8UC1);
            Cv.Cv2.WarpAffine(scaled, rot, M, new Cv.Size(nw, nh),
                Cv.InterpolationFlags.Linear, Cv.BorderTypes.Constant, new Cv.Scalar(0));
            return rot;
        }

        static void PmAlign(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            string path = VpUtil.Txt(spec, s, "template");
            Cv.Mat? tpl = null;
            if (path.Length > 0 && File.Exists(path))
                tpl = Cv.Cv2.ImRead(path, Cv.ImreadModes.Grayscale);
            else
            {
                var st0 = VpUtil.State(ctx);
                if (st0.RefImage != null && !st0.RefImage.Empty()) tpl = VpUtil.ToGray(st0.RefImage);
            }
            if (tpl == null || tpl.Empty())
            {
                tpl?.Dispose();
                VpUtil.Add(report, s, spec, false,
                    "没给模板：填模板图片路径，或先跑一次「设为标准图」存一张模板", 0, 0, "",
                    ("命中数", 0), ("最佳分数", 0), ("中心X", 0), ("中心Y", 0), ("角度", 0), ("缩放", 0));
                return;
            }

            using (tpl)
            {
                double a0 = VpUtil.Num(spec, s, "a0", -30), a1 = VpUtil.Num(spec, s, "a1", 30);
                if (a1 < a0) { double t = a0; a0 = a1; a1 = t; }
                double sMin = VpUtil.Num(spec, s, "scaleMin", 0.9), sMax = VpUtil.Num(spec, s, "scaleMax", 1.1);
                if (sMax < sMin) { double t = sMin; sMin = sMax; sMax = t; }
                if (sMin < 0.05) sMin = 0.05;
                int levels = Math.Max(0, Math.Min(4, VpUtil.Int(spec, s, "levels", 2)));
                double minScore = VpUtil.Num(spec, s, "minScore", 0.7);
                int maxHits = Math.Max(1, Math.Min(20, VpUtil.Int(spec, s, "maxHits", 1)));
                var roi = VpUtil.Roi(spec, s, cur);

                var angles = new List<double>();
                if (a1 - a0 < 1e-9) angles.Add(a0);
                else
                {
                    double step = 5.0;
                    if ((a1 - a0) / step > 40) step = (a1 - a0) / 40.0;
                    for (double a = a0; a <= a1 + 1e-9; a += step) angles.Add(a);
                    if (angles.Count == 0 || angles[angles.Count - 1] < a1 - 1e-9) angles.Add(a1);
                }
                var scales = new List<double>();
                if (sMax - sMin < 1e-9) scales.Add(sMin);
                else for (int i = 0; i < 5; i++) scales.Add(sMin + (sMax - sMin) * i / 4.0);

                var srcPyr = new List<Cv.Mat>();
                var tplPyr = new List<Cv.Mat>();
                var hits = new List<Hit>();
                double bestAny = 0;
                try
                {
                    using (var roiMat = new Cv.Mat(gray, roi))
                    { var l0 = new Cv.Mat(); roiMat.CopyTo(l0); srcPyr.Add(l0); }
                    var t0 = new Cv.Mat(); tpl.CopyTo(t0); tplPyr.Add(t0);
                    for (int L = 1; L <= levels; L++)
                    {
                        var si = new Cv.Mat(); Cv.Cv2.PyrDown(srcPyr[L - 1], si, null, Cv.BorderTypes.Default); srcPyr.Add(si);
                        var ti = new Cv.Mat(); Cv.Cv2.PyrDown(tplPyr[L - 1], ti, null, Cv.BorderTypes.Default); tplPyr.Add(ti);
                    }

                    for (int L = 0; L < srcPyr.Count; L++)
                    {
                        double factor = Math.Pow(2, L);
                        var src = srcPyr[L];
                        var baseTpl = tplPyr[L];
                        foreach (double ang in angles)
                            foreach (double sc in scales)
                            {
                                using var rt = RotateScale(baseTpl, ang, sc);
                                if (rt.Empty() || rt.Cols < 2 || rt.Rows < 2) continue;
                                if (rt.Cols > src.Cols || rt.Rows > src.Rows) continue; // 模板不能比图大
                                using var res = new Cv.Mat();
                                Cv.Cv2.MatchTemplate(src, rt, res, Cv.TemplateMatchModes.CCoeffNormed, null);
                                Cv.Cv2.MinMaxLoc(res, out _, out double peakVal, out _, out Cv.Point peak);
                                if (double.IsNaN(peakVal) || double.IsInfinity(peakVal)) continue;
                                if (peakVal > bestAny) bestAny = peakVal;
                                if (peakVal < minScore) continue;
                                hits.Add(new Hit
                                {
                                    Cx = roi.X + (peak.X + rt.Cols / 2.0) * factor,
                                    Cy = roi.Y + (peak.Y + rt.Rows / 2.0) * factor,
                                    Score = peakVal, Angle = ang, Scale = sc,
                                    W = rt.Cols * factor, H = rt.Rows * factor
                                });
                            }
                    }
                }
                finally
                {
                    foreach (var m in srcPyr) m.Dispose();
                    foreach (var m in tplPyr) m.Dispose();
                }

                // NMS：按分数降序，中心过近的合并，留前 maxHits 个
                hits.Sort((p, q) => q.Score.CompareTo(p.Score));
                double minDist = Math.Max(6, Math.Min(tpl.Cols, tpl.Rows) * 0.5);
                var kept = new List<Hit>();
                foreach (var h in hits)
                {
                    bool dup = false;
                    foreach (var q in kept)
                        if (Math.Sqrt((h.Cx - q.Cx) * (h.Cx - q.Cx) + (h.Cy - q.Cy) * (h.Cy - q.Cy)) < minDist)
                        { dup = true; break; }
                    if (!dup) kept.Add(h);
                    if (kept.Count >= maxHits) break;
                }

                if (kept.Count == 0)
                {
                    VpUtil.Add(report, s, spec, false,
                        $"没找到（最佳相似度 {VpUtil.F(bestAny)} 低于及格线 {VpUtil.F(minScore)}，或模板比画面大）",
                        bestAny, 0, "",
                        ("命中数", 0), ("最佳分数", bestAny), ("中心X", 0), ("中心Y", 0), ("角度", 0), ("缩放", 0));
                    return;
                }

                var best = kept[0];
                foreach (var h in kept)
                {
                    DrawRotRect(dst, h.Cx, h.Cy, h.W, h.H, h.Angle, ScoreColor(h.Score));
                    Cv.Cv2.PutText(dst, VpUtil.F(h.Score, 2),
                        new Cv.Point((int)h.Cx, (int)h.Cy), Cv.HersheyFonts.HersheySimplex, 0.5, VpUtil.Ink, 1);
                }

                // 最佳命中写进 report.Matches（MatchBox 字段已核对）
                report.Matches.Add(new MatchBox
                {
                    LeftTopX = (int)Math.Round(best.Cx - best.W / 2),
                    LeftTopY = (int)Math.Round(best.Cy - best.H / 2),
                    TemplateWidth = Math.Max(1, (int)Math.Round(best.W)),
                    TemplateHeight = Math.Max(1, (int)Math.Round(best.H)),
                    Angle = best.Angle, Score = best.Score, Scale = best.Scale,
                    Pass = best.Score >= minScore
                });
                ctx.LastPoint = new Cv.Point2f((float)best.Cx, (float)best.Cy);
                ctx.LastAngle = best.Angle;

                VpUtil.Add(report, s, spec, true,
                    $"命中 {kept.Count} 处｜最佳 ({VpUtil.F(best.Cx)},{VpUtil.F(best.Cy)}) 分数 {VpUtil.F(best.Score)} 角度 {VpUtil.F(best.Angle)}° 缩放 {VpUtil.F(best.Scale)}",
                    best.Score, kept.Count, "",
                    ("命中数", kept.Count), ("最佳分数", best.Score), ("中心X", best.Cx),
                    ("中心Y", best.Cy), ("角度", best.Angle), ("缩放", best.Scale));
            }
        }

        // ===================== ④ 训练式找线 =====================
        static void EasyLine(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            string mode = VpUtil.Txt(spec, s, "mode");
            var lt = LineTable.GetOrCreateValue(ctx);
            var st = VpUtil.State(ctx);
            var roi = VpUtil.Roi(spec, s, cur);

            if (mode != "运行")
            {
                double cx = VpUtil.Num(spec, s, "cx", 0), cy = VpUtil.Num(spec, s, "cy", 0);
                if (cx == 0) cx = roi.X + roi.Width / 2.0;
                if (cy == 0) cy = roi.Y + roi.Height / 2.0;
                lt.Cx = cx; lt.Cy = cy;
                lt.Angle = VpUtil.Num(spec, s, "angle", 0);
                lt.Length = Math.Max(8, VpUtil.Num(spec, s, "length", 200));
                lt.Width = Math.Max(4, VpUtil.Num(spec, s, "width", 40));
                lt.Polarity = VpUtil.Txt(spec, s, "polarity");
                if (lt.Polarity.Length == 0) lt.Polarity = "任意";
                lt.MinGrad = VpUtil.Num(spec, s, "minGrad", 8);
                lt.Samples = Math.Max(5, Math.Min(600, VpUtil.Int(spec, s, "samples", 60)));
                lt.MinScore = VpUtil.Num(spec, s, "minScore", 0.6);
                lt.Trained = true;

                st.EasyAngle = lt.Angle;
                st.RefImage?.Dispose();
                using (var gc = new Cv.Mat(gray, roi)) st.RefImage = gc.Clone();

                // 训练时把学到的搜索带画出来
                double d = Rad(lt.Angle), dx = Math.Cos(d), dy = Math.Sin(d);
                Cv.Cv2.Line(dst,
                    new Cv.Point((int)(cx - lt.Length / 2 * dx), (int)(cy - lt.Length / 2 * dy)),
                    new Cv.Point((int)(cx + lt.Length / 2 * dx), (int)(cy + lt.Length / 2 * dy)), VpUtil.Ink, 1);
                VpUtil.Add(report, s, spec, true,
                    $"已训练找线：中心 ({VpUtil.F(cx)},{VpUtil.F(cy)}) 角度 {VpUtil.F(lt.Angle)}° 长 {VpUtil.F(lt.Length, 0)} 探针 {lt.Samples} 条",
                    lt.Samples, lt.Samples, "",
                    ("起点X", cx - lt.Length / 2 * dx), ("起点Y", cy - lt.Length / 2 * dy),
                    ("终点X", cx + lt.Length / 2 * dx), ("终点Y", cy + lt.Length / 2 * dy),
                    ("角度", lt.Angle), ("匹配分数", 1), ("是否找到", 1));
                return;
            }

            if (!lt.Trained)
            {
                VpUtil.Add(report, s, spec, false,
                    "还没训练找线模型：请先在标准件上跑一次「训练」", 0, 0, "",
                    ("起点X", 0), ("起点Y", 0), ("终点X", 0), ("终点Y", 0),
                    ("角度", 0), ("匹配分数", 0), ("是否找到", 0));
                return;
            }

            using var blurred = new Cv.Mat();
            Cv.Cv2.GaussianBlur(gray, blurred, new Cv.Size(3, 3), 0); // 先抹平噪点，再做双线性采样取梯度
            var vals = NormGray(blurred, out _, out _, out _);
            int w = gray.Cols, h = gray.Rows;
            double rad = Rad(lt.Angle), ldx = Math.Cos(rad), ldy = Math.Sin(rad);
            double nx = -ldy, ny = ldx; // 垂直探针方向
            int samples = lt.Samples;
            int steps = Math.Max(8, (int)lt.Width);
            var hits = new List<(double X, double Y)>();
            double confSum = 0, bestGradSum = 0;
            for (int i = 0; i < samples; i++)
            {
                double t = samples > 1 ? (-lt.Length / 2 + lt.Length * i / (samples - 1)) : 0;
                double bx = lt.Cx + t * ldx, by = lt.Cy + t * ldy;
                double bestG = 0, bestPos = 0; bool found = false;
                for (int j = 1; j < steps; j++)
                {
                    double p = -lt.Width / 2 + lt.Width * j / steps;
                    double gA = Sample(vals, w, h, bx + (p - 1) * nx, by + (p - 1) * ny);
                    double gB = Sample(vals, w, h, bx + (p + 1) * nx, by + (p + 1) * ny);
                    double grad = (gB - gA) * 0.5;
                    bool polOk = lt.Polarity == "任意"
                        || (lt.Polarity == "暗到亮" && grad > 0)
                        || (lt.Polarity == "亮到暗" && grad < 0);
                    if (polOk && Math.Abs(grad) >= lt.MinGrad && Math.Abs(grad) > bestG)
                    { bestG = Math.Abs(grad); bestPos = p; found = true; }
                }
                if (found)
                {
                    hits.Add((bx + bestPos * nx, by + bestPos * ny));
                    confSum += VpUtil.Clamp(bestG / (lt.MinGrad * 4.0), 0, 1);
                    bestGradSum += bestG;
                }
            }

            double ex1, ey1, ex2, ey2, angDeg;
            bool fitOk = hits.Count >= 3;
            if (fitOk)
            {
                double mx = 0, my = 0;
                foreach (var p in hits) { mx += p.X; my += p.Y; }
                mx /= hits.Count; my /= hits.Count;
                double sxx = 0, syy = 0, sxy = 0;
                foreach (var p in hits)
                {
                    double a = p.X - mx, b = p.Y - my;
                    sxx += a * a; syy += b * b; sxy += a * b;
                }
                double theta = 0.5 * Math.Atan2(2 * sxy, sxx - syy); // 主方向（总体最小二乘）
                double fx = Math.Cos(theta), fy = Math.Sin(theta);
                angDeg = Deg(theta);
                ex1 = mx - lt.Length / 2 * fx; ey1 = my - lt.Length / 2 * fy;
                ex2 = mx + lt.Length / 2 * fx; ey2 = my + lt.Length / 2 * fy;
            }
            else
            {
                angDeg = lt.Angle;
                ex1 = lt.Cx - lt.Length / 2 * ldx; ey1 = lt.Cy - lt.Length / 2 * ldy;
                ex2 = lt.Cx + lt.Length / 2 * ldx; ey2 = lt.Cy + lt.Length / 2 * ldy;
            }

            double hitFrac = hits.Count / (double)Math.Max(1, samples);
            double meanConf = hits.Count > 0 ? confSum / hits.Count : 0;
            double score = hitFrac * meanConf;
            bool ok = fitOk && score >= lt.MinScore;
            var col = ok ? VpUtil.Ink : VpUtil.Warn;
            Cv.Cv2.Line(dst, new Cv.Point((int)ex1, (int)ey1), new Cv.Point((int)ex2, (int)ey2), col, 2);
            foreach (var p in hits) Cv.Cv2.Circle(dst, (int)p.X, (int)p.Y, 1, col, 1);

            string why = ok ? "找到" : (hits.Count < 3 ? "有效边缘点太少（调低 minGrad / 检查明暗方向）" : "分数不够");
            VpUtil.Add(report, s, spec, ok,
                $"{(ok ? "找到线" : "没找到线")}：{why}｜有效探针 {hits.Count}/{samples} 分数 {VpUtil.F(score)} 角度 {VpUtil.F(angDeg)}°",
                score, hits.Count, "",
                ("起点X", ex1), ("起点Y", ey1), ("终点X", ex2), ("终点Y", ey2),
                ("角度", angDeg), ("匹配分数", score), ("是否找到", ok ? 1 : 0));
        }

        // ===================== ⑤ 训练式找圆 =====================
        static void EasyCircle(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            string mode = VpUtil.Txt(spec, s, "mode");
            var ct = CircleTable.GetOrCreateValue(ctx);
            var st = VpUtil.State(ctx);
            var roi = VpUtil.Roi(spec, s, cur);

            if (mode != "运行")
            {
                double cx = VpUtil.Num(spec, s, "cx", 0), cy = VpUtil.Num(spec, s, "cy", 0);
                if (cx == 0) cx = roi.X + roi.Width / 2.0;
                if (cy == 0) cy = roi.Y + roi.Height / 2.0;
                ct.Cx = cx; ct.Cy = cy;
                ct.Radius = Math.Max(2, VpUtil.Num(spec, s, "radius", 60));
                ct.Tol = Math.Max(1, VpUtil.Num(spec, s, "tol", 20));
                ct.Polarity = VpUtil.Txt(spec, s, "polarity");
                if (ct.Polarity.Length == 0) ct.Polarity = "任意";
                ct.MinGrad = VpUtil.Num(spec, s, "minGrad", 8);
                ct.Samples = Math.Max(8, Math.Min(720, VpUtil.Int(spec, s, "samples", 120)));
                ct.MinScore = VpUtil.Num(spec, s, "minScore", 0.6);
                ct.Trained = true;

                st.EasyRadius = ct.Radius;
                st.RefImage?.Dispose();
                using (var gc = new Cv.Mat(gray, roi)) st.RefImage = gc.Clone();

                Cv.Cv2.Circle(dst, (int)cx, (int)cy, (int)ct.Radius, VpUtil.Ink, 1);
                VpUtil.Add(report, s, spec, true,
                    $"已训练找圆：圆心 ({VpUtil.F(cx)},{VpUtil.F(cy)}) 半径 {VpUtil.F(ct.Radius)} 探针 {ct.Samples} 条",
                    ct.Samples, ct.Samples, "",
                    ("圆心X", cx), ("圆心Y", cy), ("半径", ct.Radius), ("直径", ct.Radius * 2),
                    ("匹配分数", 1), ("是否找到", 1));
                return;
            }

            if (!ct.Trained)
            {
                VpUtil.Add(report, s, spec, false,
                    "还没训练找圆模型：请先在标准件上跑一次「训练」", 0, 0, "",
                    ("圆心X", 0), ("圆心Y", 0), ("半径", 0), ("直径", 0),
                    ("匹配分数", 0), ("是否找到", 0));
                return;
            }

            using var blurred = new Cv.Mat();
            Cv.Cv2.GaussianBlur(gray, blurred, new Cv.Size(3, 3), 0); // 先抹平噪点，再做双线性采样取梯度
            var vals = NormGray(blurred, out _, out _, out _);
            int w = gray.Cols, h = gray.Rows;
            int samples = ct.Samples;
            int steps = Math.Max(6, (int)(ct.Tol * 2));
            var hits = new List<(double X, double Y)>();
            double confSum = 0;
            for (int i = 0; i < samples; i++)
            {
                double ang = 2 * Math.PI * i / samples;
                double ux = Math.Cos(ang), uy = Math.Sin(ang); // 由圆内指向圆外
                double bestG = 0, bestR = ct.Radius; bool found = false;
                for (int j = 1; j < steps; j++)
                {
                    double rr = ct.Radius - ct.Tol + 2 * ct.Tol * j / steps;
                    double gA = Sample(vals, w, h, ct.Cx + (rr - 1) * ux, ct.Cy + (rr - 1) * uy);
                    double gB = Sample(vals, w, h, ct.Cx + (rr + 1) * ux, ct.Cy + (rr + 1) * uy);
                    double grad = (gB - gA) * 0.5;
                    bool polOk = ct.Polarity == "任意"
                        || (ct.Polarity == "暗到亮" && grad > 0)
                        || (ct.Polarity == "亮到暗" && grad < 0);
                    if (polOk && Math.Abs(grad) >= ct.MinGrad && Math.Abs(grad) > bestG)
                    { bestG = Math.Abs(grad); bestR = rr; found = true; }
                }
                if (found)
                {
                    hits.Add((ct.Cx + bestR * ux, ct.Cy + bestR * uy));
                    confSum += VpUtil.Clamp(bestG / (ct.MinGrad * 4.0), 0, 1);
                }
            }

            double fcx = ct.Cx, fcy = ct.Cy, fr = ct.Radius;
            bool fitOk = hits.Count >= 3 && FitCircle(hits, out fcx, out fcy, out fr);
            double hitFrac = hits.Count / (double)Math.Max(1, samples);
            double meanConf = hits.Count > 0 ? confSum / hits.Count : 0;
            double score = hitFrac * meanConf;
            bool ok = fitOk && score >= ct.MinScore;
            var col = ok ? VpUtil.Ink : VpUtil.Warn;
            Cv.Cv2.Circle(dst, (int)fcx, (int)fcy, Math.Max(1, (int)fr), col, 2);
            foreach (var p in hits) Cv.Cv2.Circle(dst, (int)p.X, (int)p.Y, 1, col, 1);

            string why = ok ? "找到" : (hits.Count < 3 ? "有效边缘点太少（调低 minGrad / 检查明暗方向）" : "拟合或分数不够");
            VpUtil.Add(report, s, spec, ok,
                $"{(ok ? "找到圆" : "没找到圆")}：{why}｜有效探针 {hits.Count}/{samples} 分数 {VpUtil.F(score)} 半径 {VpUtil.F(fr)}",
                score, hits.Count, "",
                ("圆心X", fcx), ("圆心Y", fcy), ("半径", fr), ("直径", fr * 2),
                ("匹配分数", score), ("是否找到", ok ? 1 : 0));
        }

        // ===================== ⑥ 颜色查找表变换（换图） =====================
        static Cv.Mat ColorLut(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            string mode = VpUtil.Txt(spec, s, "mode");
            double gain = VpUtil.Num(spec, s, "gain", 1);
            double bias = VpUtil.Num(spec, s, "bias", 0);
            double gamma = VpUtil.Num(spec, s, "gamma", 1);
            double lo = VpUtil.Num(spec, s, "lo", 0), hi = VpUtil.Num(spec, s, "hi", 255);
            if (hi <= lo) hi = lo + 1;
            if (gamma <= 0) gamma = 1;

            using var bgr = VpUtil.ToBgr(cur);
            // 256 项 LUT，1×256 CV_8U
            using var lut = new Cv.Mat(1, 256, Cv.MatType.CV_8UC1);
            for (int i = 0; i < 256; i++)
            {
                double v;
                switch (mode)
                {
                    case "伽马": v = 255 * Math.Pow(i / 255.0, gamma); break;
                    case "反色": v = 255 - i; break;
                    case "分段拉伸": v = (i - lo) / (hi - lo) * 255; break;
                    case "通道交换": v = i; break;          // 通道交换不走 LUT，下面单独处理
                    case "亮度对比度":
                    default: v = (i - 128) * gain + 128 + bias; break;
                }
                lut.Set<byte>(0, i, (byte)VpUtil.Clamp(Math.Round(v), 0, 255));
            }

            var outp = new Cv.Mat(); // 新建的 BGR 8U，作为返回值
            if (mode == "通道交换")
            {
                Cv.Mat[] ch = Cv.Cv2.Split(bgr); // [B,G,R] 拆成三张单通道图
                try { Cv.Cv2.Merge(new Cv.Mat[] { ch[2], ch[1], ch[0] }, outp); } // 按 [R,G,B] 合回 = 交换 B↔R
                finally { if (ch != null) foreach (var m in ch) m?.Dispose(); }
            }
            else
            {
                Cv.Cv2.LUT(bgr, lut, outp);
            }

            var bm = Cv.Cv2.Mean(bgr);
            var am = Cv.Cv2.Mean(outp);
            double before = (bm.Val0 + bm.Val1 + bm.Val2) / 3;
            double after = (am.Val0 + am.Val1 + am.Val2) / 3;
            using var outGray = VpUtil.ToGray(outp);
            Cv.Cv2.MinMaxLoc(outGray, out double mn, out double mx);

            VpUtil.Add(report, s, spec, true,
                $"{mode}：均值 {VpUtil.F(before)} → {VpUtil.F(after)}，灰度范围 {VpUtil.F(mn, 0)}~{VpUtil.F(mx, 0)}",
                after, 0, "",
                ("变换前均值", before), ("变换后均值", after), ("最小值", mn), ("最大值", mx));
            return outp; // 新建的 3 通道 BGR 8U，作为之后的当前图
        }

        // ===================== ⑦ 16位图/像素映射（换图） =====================
        static Cv.Mat PixMap16(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            string mode = VpUtil.Txt(spec, s, "mode");
            double lo = VpUtil.Num(spec, s, "lo", 0), hi = VpUtil.Num(spec, s, "hi", 0);
            int levels = Math.Max(2, Math.Min(256, VpUtil.Int(spec, s, "levels", 16)));
            double gamma = VpUtil.Num(spec, s, "gamma", 1);
            if (gamma <= 0) gamma = 1;

            using var g = VpUtil.ToGray(cur); // 单通道，保留原始位深
            double[] vals = NormGray(g, out double gmin, out double gmax, out double gmean);
            int w = g.Cols, h = g.Rows;
            double span = gmax - gmin;

            // 手填区间是原图单位，换算到 0..255 规整域
            double loN = 0, hiN = 255;
            if (mode == "手填区间")
            {
                if (lo > 0 && span > 1e-9) loN = VpUtil.Clamp((lo - gmin) / span * 255, 0, 255);
                if (hi > 0 && span > 1e-9) hiN = VpUtil.Clamp((hi - gmin) / span * 255, 0, 255);
                if (hiN <= loN) hiN = Math.Min(255, loN + 1);
            }
            double denom = hiN - loN; if (denom < 1e-6) denom = 1;

            using var mapped = new Cv.Mat(h, w, Cv.MatType.CV_8UC1);
            var mi = mapped.GetGenericIndexer<byte>();
            var seen = new bool[256];
            int used = 0;
            double sumOut = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double t = VpUtil.Clamp((vals[y * w + x] - loN) / denom, 0, 1);
                    double o;
                    switch (mode)
                    {
                        case "量化": { int q = (int)Math.Round(t * (levels - 1)); o = q / (double)(levels - 1) * 255; break; }
                        case "伽马压缩": o = 255 * Math.Pow(t, gamma); break;
                        default: o = t * 255; break; // 自动拉伸 / 手填区间
                    }
                    byte b = (byte)VpUtil.Clamp(Math.Round(o), 0, 255);
                    mi[y, x] = b;
                    sumOut += b;
                    if (!seen[b]) { seen[b] = true; used++; }
                }
            double mappedMean = sumOut / Math.Max(1, w * h);

            var outp = new Cv.Mat();
            Cv.Cv2.CvtColor(mapped, outp, Cv.ColorConversionCodes.GRAY2BGR); // 返回新的 BGR 8U

            VpUtil.Add(report, s, spec, true,
                $"{mode}：原图 {VpUtil.F(gmin, 0)}~{VpUtil.F(gmax, 0)} 均值 {VpUtil.F(gmean)} → 映射均值 {VpUtil.F(mappedMean)}，用了 {used} 档",
                mappedMean, used, "",
                ("原图最小", gmin), ("原图最大", gmax), ("原图均值", gmean),
                ("映射后均值", mappedMean), ("用到几档", used));
            return outp;
        }

        // ===================== ⑧ 手眼标定 =====================
        /// <summary>仿射最小二乘：X=A1u+A2v+A3，Y=B1u+B2v+B3，返回 (ax,by,rms)。</summary>
        static bool SolveAffine(List<(double Ux, double Uy, double Mx, double My)> he, out double[] ax, out double[] by, out double rms)
        {
            ax = Array.Empty<double>(); by = Array.Empty<double>(); rms = double.NaN;
            int n = he.Count;
            if (n < 3) return false;
            var rows = new double[n][];
            var tx = new double[n]; var ty = new double[n];
            for (int i = 0; i < n; i++)
            {
                rows[i] = new[] { he[i].Ux, he[i].Uy, 1.0 };
                tx[i] = he[i].Mx; ty[i] = he[i].My;
            }
            var a = LeastSquares(rows, tx, 3);
            var b = LeastSquares(rows, ty, 3);
            if (a == null || b == null) return false;
            ax = a; by = b;
            double ss = 0;
            for (int i = 0; i < n; i++)
            {
                double px = a[0] * he[i].Ux + a[1] * he[i].Uy + a[2];
                double py = b[0] * he[i].Ux + b[1] * he[i].Uy + b[2];
                double ex = px - he[i].Mx, ey = py - he[i].My;
                ss += ex * ex + ey * ey;
            }
            rms = Math.Sqrt(ss / n);
            return true;
        }

        static void HandEye(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var he = HeTable.GetOrCreateValue(ctx);
            string mode = VpUtil.Txt(spec, s, "mode");

            if (mode == "清除")
            {
                int had = he.Count;
                he.Clear();
                VpUtil.Add(report, s, spec, true, $"已清除 {had} 个标定点", 0, 0, "",
                    ("已记点数", 0), ("像素当量(毫米每像素)", 0), ("残差(毫米)", 0),
                    ("旋转中心X", 0), ("旋转中心Y", 0), ("是否可用", 0));
                return;
            }

            if (mode != "求解")
            {
                double ux = VpUtil.Num(spec, s, "ux", 0), uy = VpUtil.Num(spec, s, "uy", 0);
                double mx = VpUtil.Num(spec, s, "mx", 0), my = VpUtil.Num(spec, s, "my", 0);
                he.Add((ux, uy, mx, my));
                Cv.Cv2.Circle(dst, (int)ux, (int)uy, 4, VpUtil.Ink, 2);
                VpUtil.Add(report, s, spec, true,
                    $"已记录第 {he.Count} 点：图上 ({VpUtil.F(ux)},{VpUtil.F(uy)}) ↔ 机台 ({VpUtil.F(mx)},{VpUtil.F(my)})",
                    he.Count, he.Count, "",
                    ("已记点数", he.Count), ("像素当量(毫米每像素)", 0), ("残差(毫米)", 0),
                    ("旋转中心X", 0), ("旋转中心Y", 0), ("是否可用", 0));
                return;
            }

            int minPoints = Math.Max(2, VpUtil.Int(spec, s, "minPoints", 3));
            if (he.Count < minPoints)
            {
                VpUtil.Add(report, s, spec, false,
                    $"点数不够：已记 {he.Count} 点，至少要 {minPoints} 点才能求解", 0, he.Count, "",
                    ("已记点数", he.Count), ("像素当量(毫米每像素)", 0), ("残差(毫米)", 0),
                    ("旋转中心X", 0), ("旋转中心Y", 0), ("是否可用", 0));
                return;
            }

            if (!SolveAffine(he, out double[] ax, out double[] by, out double rms))
            {
                VpUtil.Add(report, s, spec, false,
                    "点退化（共线或重复），正规方程奇异，解不出仿射参数", 0, he.Count, "",
                    ("已记点数", he.Count), ("像素当量(毫米每像素)", 0), ("残差(毫米)", 0),
                    ("旋转中心X", 0), ("旋转中心Y", 0), ("是否可用", 0));
                return;
            }

            // 像素当量：两行尺度平均（毫米/像素）
            double su = Math.Sqrt(ax[0] * ax[0] + ax[1] * ax[1]);
            double sv = Math.Sqrt(by[0] * by[0] + by[1] * by[1]);
            double mmPerPx = (su + sv) / 2;
            if (mmPerPx <= 0 || double.IsNaN(mmPerPx)) mmPerPx = double.NaN;

            ctx.Affine = new[] { ax[0], ax[1], ax[2], by[0], by[1], by[2] };
            ctx.MmPerPx = mmPerPx;
            ctx.ScalePxPerMm = mmPerPx > 0 ? 1.0 / mmPerPx : double.NaN;
            ctx.CalibRms = rms;

            // 旋转中心：装轴上且转了 ≥3 次，对记录的机台点拟合圆
            double rcx = 0, rcy = 0; bool hasRot = false;
            int rotNums = VpUtil.Int(spec, s, "rotNums", 0);
            string type = VpUtil.Txt(spec, s, "type");
            if (rotNums >= 3 && type == "相机装在轴上")
            {
                var pts = new List<(double X, double Y)>();
                foreach (var p in he) pts.Add((p.Mx, p.My));
                if (FitCircle(pts, out rcx, out rcy, out _)) hasRot = true;
            }
            if (!hasRot) { rcx = 0; rcy = 0; }

            bool usable = !double.IsNaN(mmPerPx) && mmPerPx > 0
                && !double.IsNaN(rms) && rms < mmPerPx * 50 + 5;
            string rotNote = hasRot ? $"旋转中心 ({VpUtil.F(rcx)},{VpUtil.F(rcy)})"
                : "未求旋转中心（需转≥3次且相机装在轴上，此处记 0）";
            VpUtil.Add(report, s, spec, usable,
                $"{(usable ? "标定可用" : "标定结果存疑")}：{he.Count} 点，像素当量 {VpUtil.F(mmPerPx, 4)} mm/px，残差 {VpUtil.F(rms, 3)} mm，{rotNote}",
                double.IsNaN(mmPerPx) ? 0 : mmPerPx, he.Count, "",
                ("已记点数", he.Count), ("像素当量(毫米每像素)", double.IsNaN(mmPerPx) ? 0 : mmPerPx),
                ("残差(毫米)", rms), ("旋转中心X", rcx), ("旋转中心Y", rcy), ("是否可用", usable ? 1 : 0));
        }

        // ===================== ⑨ N点对N点标定 =====================
        static void NPointCal(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var np = NpTable.GetOrCreateValue(ctx);
            string mode = VpUtil.Txt(spec, s, "mode");
            string kind = VpUtil.Txt(spec, s, "kind");
            if (kind.Length == 0) kind = "仿射";

            if (mode == "清除")
            {
                int had = np.Count;
                np.Clear();
                VpUtil.Add(report, s, spec, true, $"已清除 {had} 个标定点", 0, 0, "",
                    ("已记点数", 0), ("残差(毫米)", 0), ("a1", 0), ("a2", 0), ("a3", 0),
                    ("b1", 0), ("b2", 0), ("b3", 0), ("是否可用", 0));
                return;
            }

            if (mode != "求解")
            {
                double ux = VpUtil.Num(spec, s, "ux", 0), uy = VpUtil.Num(spec, s, "uy", 0);
                double mx = VpUtil.Num(spec, s, "mx", 0), my = VpUtil.Num(spec, s, "my", 0);
                np.Add((ux, uy, mx, my));
                Cv.Cv2.Circle(dst, (int)ux, (int)uy, 4, VpUtil.Ink, 2);
                VpUtil.Add(report, s, spec, true,
                    $"已记录第 {np.Count} 点：图上 ({VpUtil.F(ux)},{VpUtil.F(uy)}) ↔ 机台 ({VpUtil.F(mx)},{VpUtil.F(my)})",
                    np.Count, np.Count, "",
                    ("已记点数", np.Count), ("残差(毫米)", 0), ("a1", 0), ("a2", 0), ("a3", 0),
                    ("b1", 0), ("b2", 0), ("b3", 0), ("是否可用", 0));
                return;
            }

            int minPoints = Math.Max(2, VpUtil.Int(spec, s, "minPoints", 3));
            int need = Math.Max(minPoints, kind == "透视" ? 4 : 3);
            if (np.Count < need)
            {
                VpUtil.Add(report, s, spec, false,
                    $"点数不够：已记 {np.Count} 点，{kind} 至少要 {need} 点", 0, np.Count, "",
                    ("已记点数", np.Count), ("残差(毫米)", 0), ("a1", 0), ("a2", 0), ("a3", 0),
                    ("b1", 0), ("b2", 0), ("b3", 0), ("是否可用", 0));
                return;
            }

            int n = np.Count;
            double[] c = new double[6]; // a1,a2,a3,b1,b2,b3
            double rms = double.NaN;
            bool solved = false;

            if (kind == "透视")
            {
                // 8 系数单应：Hartley 归一化 + DLT 线性最小二乘，解完反归一化。
                // 与 FindHomography 的非鲁棒解同源；用纯 C# 正规方程避开其 mask/重载不确定性。
                double cu = 0, cv = 0, cX = 0, cY = 0;
                foreach (var p in np) { cu += p.Ux; cv += p.Uy; cX += p.Mx; cY += p.My; }
                cu /= n; cv /= n; cX /= n; cY /= n;
                double du = 0, dm = 0;
                foreach (var p in np)
                {
                    du += Math.Sqrt((p.Ux - cu) * (p.Ux - cu) + (p.Uy - cv) * (p.Uy - cv));
                    dm += Math.Sqrt((p.Mx - cX) * (p.Mx - cX) + (p.My - cY) * (p.My - cY));
                }
                du /= n; dm /= n;
                double su = du > 1e-9 ? Math.Sqrt(2) / du : 1;
                double sm = dm > 1e-9 ? Math.Sqrt(2) / dm : 1;

                var rows = new double[2 * n][];
                var tgt = new double[2 * n];
                for (int i = 0; i < n; i++)
                {
                    double u = (np[i].Ux - cu) * su, v = (np[i].Uy - cv) * su;
                    double X = (np[i].Mx - cX) * sm, Y = (np[i].My - cY) * sm;
                    rows[2 * i] = new[] { u, v, 1, 0, 0, 0, -u * X, -v * X }; tgt[2 * i] = X;
                    rows[2 * i + 1] = new[] { 0, 0, 0, u, v, 1, -u * Y, -v * Y }; tgt[2 * i + 1] = Y;
                }
                var hn = LeastSquares(rows, tgt, 8);
                if (hn != null)
                {
                    var Hn = new double[3, 3] { { hn[0], hn[1], hn[2] }, { hn[3], hn[4], hn[5] }, { hn[6], hn[7], 1 } };
                    var Tu = new double[3, 3] { { su, 0, -su * cu }, { 0, su, -su * cv }, { 0, 0, 1 } };
                    var Tmi = new double[3, 3] { { 1 / sm, 0, cX }, { 0, 1 / sm, cY }, { 0, 0, 1 } };
                    var H = Mul3(Mul3(Tmi, Hn), Tu); // 原图 → 机台
                    if (Math.Abs(H[2, 2]) > 1e-12)
                    {
                        for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) H[i, j] /= H[2, 2];
                        c = new[] { H[0, 0], H[0, 1], H[0, 2], H[1, 0], H[1, 1], H[1, 2] };
                        double ss = 0;
                        foreach (var p in np)
                        {
                            double d = H[2, 0] * p.Ux + H[2, 1] * p.Uy + H[2, 2];
                            if (Math.Abs(d) < 1e-12) continue;
                            double px = (H[0, 0] * p.Ux + H[0, 1] * p.Uy + H[0, 2]) / d;
                            double py = (H[1, 0] * p.Ux + H[1, 1] * p.Uy + H[1, 2]) / d;
                            double ex = px - p.Mx, ey = py - p.My;
                            ss += ex * ex + ey * ey;
                        }
                        rms = Math.Sqrt(ss / n);
                        solved = true;
                    }
                }
            }
            else if (kind == "相似")
            {
                // X = p·u - q·v + tx，Y = q·u + p·v + ty；线性解 (p,q,tx,ty)
                var rows = new double[2 * n][];
                var tgt = new double[2 * n];
                for (int i = 0; i < n; i++)
                {
                    double u = np[i].Ux, v = np[i].Uy;
                    rows[2 * i] = new[] { u, -v, 1, 0 }; tgt[2 * i] = np[i].Mx;
                    rows[2 * i + 1] = new[] { v, u, 0, 1 }; tgt[2 * i + 1] = np[i].My;
                }
                var sol = LeastSquares(rows, tgt, 4);
                if (sol != null)
                {
                    double p = sol[0], q = sol[1], tx = sol[2], ty = sol[3];
                    c = new[] { p, -q, tx, q, p, ty };
                    ctx.Affine = c;
                    double ss = 0;
                    foreach (var pt in np)
                    {
                        double px = p * pt.Ux - q * pt.Uy + tx;
                        double py = q * pt.Ux + p * pt.Uy + ty;
                        double ex = px - pt.Mx, ey = py - pt.My;
                        ss += ex * ex + ey * ey;
                    }
                    rms = Math.Sqrt(ss / n);
                    solved = true;
                }
            }
            else // 仿射
            {
                var rows = new double[n][];
                var tx = new double[n]; var ty = new double[n];
                for (int i = 0; i < n; i++)
                {
                    rows[i] = new[] { np[i].Ux, np[i].Uy, 1.0 };
                    tx[i] = np[i].Mx; ty[i] = np[i].My;
                }
                var a = LeastSquares(rows, tx, 3);
                var b = LeastSquares(rows, ty, 3);
                if (a != null && b != null)
                {
                    c = new[] { a[0], a[1], a[2], b[0], b[1], b[2] };
                    ctx.Affine = c;
                    double ss = 0;
                    foreach (var pt in np)
                    {
                        double px = a[0] * pt.Ux + a[1] * pt.Uy + a[2];
                        double py = b[0] * pt.Ux + b[1] * pt.Uy + b[2];
                        double ex = px - pt.Mx, ey = py - pt.My;
                        ss += ex * ex + ey * ey;
                    }
                    rms = Math.Sqrt(ss / n);
                    solved = true;
                }
            }

            if (!solved)
            {
                VpUtil.Add(report, s, spec, false,
                    "正规方程奇异（点共线 / 重复 / 退化），解不出换算系数", 0, n, "",
                    ("已记点数", n), ("残差(毫米)", 0), ("a1", 0), ("a2", 0), ("a3", 0),
                    ("b1", 0), ("b2", 0), ("b3", 0), ("是否可用", 0));
                return;
            }

            bool usable = !double.IsNaN(rms) && !double.IsInfinity(rms) && rms < 50;
            string note = kind == "透视" ? "（透视单应，a1~b3 为其线性部分）" : "";
            VpUtil.Add(report, s, spec, usable,
                $"{(usable ? "标定可用" : "标定结果存疑")}：{kind}{note}，{n} 点，残差 {VpUtil.F(rms, 3)} mm",
                double.IsNaN(rms) ? 0 : rms, n, "",
                ("已记点数", n), ("残差(毫米)", rms),
                ("a1", c[0]), ("a2", c[1]), ("a3", c[2]),
                ("b1", c[3]), ("b2", c[4]), ("b3", c[5]), ("是否可用", usable ? 1 : 0));
        }
    }
}
