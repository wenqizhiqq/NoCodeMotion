// === NoCodeMotion 视觉流程引擎（OpenCV 实现） | 作者：温​启‎志‍ ‍|⁣ ⁣微‏信‎：‏1‏8‌7‍1‌9‎3⁠6‏1​3​9‌9‌ ⁠|​ ​保‎留‍所⁠有‍权⁣利‏，⁣请‌勿‍删‎除 ===
using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Cv = OpenCvSharp;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Vision
{
    /// <summary>
    /// 视觉算子执行器（目录见 <see cref="VisionOperators"/>）。
    ///
    /// 与老的「步骤类型」大 switch 并存：步骤填了 Op（算子键）就走这里，
    /// 没填就仍按原来的 StepType 跑 —— 老工程一字不动照样能用。
    ///
    /// 跨步骤传数据靠 <see cref="VisionOpContext"/>：上一步采到的边缘点存这里，
    /// 「把点连成线 / 把点拟成圆」直接吃它，不用用户复制坐标。
    /// </summary>
    public sealed class VisionOpContext
    {
        /// <summary>最近一次算子采到的边缘点（供拟合类算子复用）。</summary>
        public readonly List<Cv.Point2f> Points = new();
        public VisionCaliper.LineResult? LastLine;
        public VisionCaliper.CircleResult? LastCircle;

        /// <summary>历史线 / 历史圆（按找到的先后压进来）：两条线、两个圆这类算子要取最近两条。</summary>
        public readonly List<VisionCaliper.LineResult> Lines = new();
        public readonly List<VisionCaliper.CircleResult> Circles = new();

        /// <summary>角点（找矩形四角 / 找尖角 的结果），供「两点多远」「换算到机台」直接取用。</summary>
        public readonly List<Cv.Point2f> Corners = new();

        /// <summary>标定与换算：上一步定出来的比例（每毫米多少像素）与手动对点的仿射参数。</summary>
        public double ScalePxPerMm = double.NaN;
        public double CalibAngleDeg = double.NaN;
        public double CalibOriginX = double.NaN, CalibOriginY = double.NaN;
        public bool HasCalib => !double.IsNaN(ScalePxPerMm) && ScalePxPerMm > 0;

        /// <summary>最近一次量到的长度 / 面积（「定比例」在 pixels 留空时用它）。</summary>
        public double LastLength = double.NaN;
        public Cv.Point2f? LastPoint;

        // ---- 批次 5：对位与标定之间传的数据 ----

        /// <summary>
        /// 标定算子定出来的完整换算关系：X = A1·u + A2·v + A3，Y = B1·u + B2·v + B3
        /// （u/v = 图上像素，X/Y = 机台毫米），和「标定」页那张表用同一套系数。
        /// </summary>
        public double[]? Affine;
        public bool HasAffine => Affine != null && Affine.Length == 6;

        /// <summary>最近一次标定的像素当量（毫米/像素）、残差（毫米）、以及存进了哪台相机。</summary>
        public double MmPerPx = double.NaN;
        public double CalibRms = double.NaN;
        public string CalibCam = "";

        /// <summary>「找旋转中心」给的中心（图上像素 / 换算后的机台毫米）、半径、误差。</summary>
        public double RotCenterU = double.NaN, RotCenterV = double.NaN;
        public double RotCenterX = double.NaN, RotCenterY = double.NaN;
        public double RotRadiusPx = double.NaN, RotRmsPx = double.NaN;
        public bool HasRotCenter => !double.IsNaN(RotCenterU);

        /// <summary>上一条算子给的角度（找图 / 找矩形四角 / 特征点对齐），「照图形基准对位」没填时直接拿它。</summary>
        public double LastAngle = double.NaN;

        /// <summary>上一条「两个点对位」量到的偏差；「算对位移动量」没填时直接拿它。</summary>
        public double AlignDX = double.NaN, AlignDY = double.NaN, AlignDR = double.NaN;
        public bool HasAlign => !double.IsNaN(AlignDX);
    }

    public static class VisionOpRunner
    {
        static readonly Cv.Scalar Ink = new Cv.Scalar(0, 220, 0);        // BGR：找到的几何画绿
        static readonly Cv.Scalar InkWarn = new Cv.Scalar(0, 0, 230);    // 失败画红
        static readonly Cv.Scalar Dot = new Cv.Scalar(255, 60, 60);      // 边缘点画红点

        // ===================== 入口 =====================

        /// <summary>
        /// 跑一个算子。返回「之后的当前图像」：图像 / 颜色类算子会换一张图，
        /// 调用方（VisionEngine）拿返回值替换 cur 并重建注释画布；没换图时返回原对象。
        /// </summary>
        public static Cv.Mat Run(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat? display,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var dst = display ?? cur;
            var next = cur;                          // 被换出来的新图（默认不换）
            var gray = ToGray(cur);
            try
            {
                switch (spec.Key)
                {
                    // ---- 批次 1：卡尺找边 ----
                    case "line": DoLine(spec, s, gray, dst, report, ctx, progress, straightness: false); break;
                    case "straight": DoLine(spec, s, gray, dst, report, ctx, progress, straightness: true); break;
                    case "lines": DoLines(spec, s, gray, dst, report, ctx, progress); break;
                    case "circle": DoCircle(spec, s, gray, dst, report, ctx, progress, arc: false); break;
                    case "arc": DoCircle(spec, s, gray, dst, report, ctx, progress, arc: true); break;
                    case "edgepoints": DoEdgePoints(spec, s, gray, dst, report, ctx, progress); break;
                    case "quad": DoQuad(spec, s, cur, gray, dst, report, ctx, progress); break;
                    case "corner": DoCorners(spec, s, gray, dst, report, ctx, progress); break;
                    case "fitline": DoFitLineFromPoints(spec, s, dst, report, ctx, progress); break;
                    case "fitcircle": DoFitCircleFromPoints(spec, s, dst, report, ctx, progress); break;
                    case "edgept": DoEdgePoint(spec, s, gray, dst, report, ctx, progress); break;
                    // ---- 批次 7：Halcon 常驻算子 ----
                    case "thresh": DoMaskOp(spec, s, gray, dst, report, ctx, progress, "thresh"); break;
                    case "autothresh": DoMaskOp(spec, s, gray, dst, report, ctx, progress, "autothresh"); break;
                    case "dynthresh": DoMaskOp(spec, s, gray, dst, report, ctx, progress, "dynthresh"); break;
                    case "erode": DoMaskOp(spec, s, gray, dst, report, ctx, progress, "erode"); break;
                    case "dilate": DoMaskOp(spec, s, gray, dst, report, ctx, progress, "dilate"); break;
                    case "open": DoMaskOp(spec, s, gray, dst, report, ctx, progress, "open"); break;
                    case "close": DoMaskOp(spec, s, gray, dst, report, ctx, progress, "close"); break;
                    case "tophat": DoMaskOp(spec, s, gray, dst, report, ctx, progress, "tophat"); break;
                    case "bothat": DoMaskOp(spec, s, gray, dst, report, ctx, progress, "bothat"); break;
                    case "edges": DoEdgeMap(spec, s, gray, dst, report, progress); break;
                    case "grayproj": DoGrayProjection(spec, s, gray, dst, report, progress); break;
                    case "rigid": DoRigidPoint(spec, s, dst, report, progress); break;
                    case "affineimg": DoAffineImage(spec, s, gray, dst, report, progress); break;
                    // ---- 批次 8 ----
                    case "connect": DoComponents(spec, s, gray, dst, report, ctx, progress, "connect"); break;
                    case "selectshape": DoComponents(spec, s, gray, dst, report, ctx, progress, "select"); break;
                    case "shapefeat": DoComponents(spec, s, gray, dst, report, ctx, progress, "feat"); break;
                    case "convexhull": DoComponents(spec, s, gray, dst, report, ctx, progress, "hull"); break;
                    case "contours": DoContourLines(spec, s, gray, dst, report, progress); break;
                    case "edgepair": DoEdgePair(spec, s, gray, dst, report, progress); break;
                    case "minmaxgray": DoMinMaxGray(spec, s, gray, dst, report, progress); break;
                    case "hueselect": DoHueSelect(spec, s, cur, dst, report, ctx, progress); break;
                    case "invert": DoPixelMap(spec, s, gray, dst, report, "invert"); break;
                    case "equhisto": DoPixelMap(spec, s, gray, dst, report, "equhisto"); break;
                    case "gaussblur": DoPixelMap(spec, s, gray, dst, report, "gauss"); break;
                    case "ellipse": DoEllipse(spec, s, gray, dst, report, ctx, progress); break;
                    case "fitellipse": DoFitEllipseFromPoints(spec, s, dst, report, ctx, progress); break;

                    // ---- 批次 2：几何计算 ----
                    case "pp": DoPointPoint(spec, s, dst, report, ctx, progress); break;
                    case "pl": DoPointLine(spec, s, dst, report, ctx, progress); break;
                    case "pc": DoPointCircle(spec, s, dst, report, ctx, progress); break;
                    case "ll": DoLineLine(spec, s, dst, report, ctx, progress); break;
                    case "lc": DoLineCircle(spec, s, dst, report, ctx, progress); break;
                    case "cc": DoCircleCircle(spec, s, dst, report, ctx, progress); break;
                    case "gap": DoLineGap(spec, s, dst, report, ctx, progress); break;
                    case "pxstat": DoPixelStat(spec, s, gray, dst, report, progress); break;
                    case "histo": DoHistogram(spec, s, gray, dst, report, progress); break;

                    // ---- 批次 2：标定与换算 ----
                    case "mscale": DoMeasureScale(spec, s, report, ctx, progress); break;
                    case "mcaltwo": DoTwoPointCalib(spec, s, dst, report, ctx, progress); break;
                    case "px2mm": DoPixelToMachine(spec, s, dst, report, ctx, progress); break;
                    case "mm2px": DoMachineToPixel(spec, s, dst, report, ctx, progress); break;
                    case "scalar": DoScalarConvert(spec, s, report, ctx, progress); break;

                    // ---- 批次 5：标定（对位要用）----
                    case "caln": DoMultiCalib(spec, s, dst, report, ctx, progress); break;
                    case "calone": DoOneAxisCalib(spec, s, dst, report, ctx, progress); break;
                    case "rotcenter": DoRotateCenter(spec, s, dst, report, ctx, progress); break;
                    case "calboard": DoBoardScale(spec, s, gray, dst, report, ctx, progress); break;
                    case "lenscal": next = DoLensCalib(spec, s, cur, gray, dst, report, ctx, progress); break;
                    case "lensfix": next = DoLensFix(spec, s, cur, dst, report, ctx, progress); break;

                    // ---- 批次 5：对位 ----
                    case "alignpm": DoTwoMarkAlign(spec, s, dst, report, ctx, progress); break;
                    case "aligntpl": DoTemplateAlign(spec, s, dst, report, ctx, progress); break;
                    case "alignrel": DoPairAlign(spec, s, report, ctx, progress); break;
                    case "alignrigid": DoRigidMap(spec, s, dst, report, ctx, progress); break;
                    case "alignout": DoAlignOutput(spec, s, report, ctx, progress); break;

                    // ---- 批次 3：匹配与识别 ----
                    case "shape": DoShapeMatch(spec, s, cur, gray, dst, report, ctx, progress); break;
                    case "feat": DoFeatureAlign(spec, s, cur, gray, dst, report, ctx, progress); break;
                    case "blob": DoBlob(spec, s, gray, dst, report, ctx, progress); break;
                    case "regions": DoRegions(spec, s, gray, dst, report, ctx, progress); break;
                    case "bar1d": DoReadCode(spec, s, cur, dst, report, ctx, progress, oneD: true); break;
                    case "qr": DoReadCode(spec, s, cur, dst, report, ctx, progress, oneD: false); break;
                    case "codecheck": DoReadCode(spec, s, cur, dst, report, ctx, progress, oneD: null); break;

                    // ---- 批次 4：图像与颜色（这几个会换掉当前图像） ----
                    case "colorspace": next = DoColorSpace(spec, s, cur, dst, report, progress); break;
                    case "chan": next = DoChannel(spec, s, cur, dst, report, progress); break;
                    case "colorchk": DoColorCheck(spec, s, cur, dst, report, progress); break;
                    case "enhance": next = DoEnhance(spec, s, cur, gray, dst, report, progress); break;
                    case "denoise": next = DoDenoise(spec, s, cur, gray, dst, report, progress); break;
                    case "geom": next = DoGeometry(spec, s, cur, dst, report, progress); break;
                    case "save": DoSaveImage(spec, s, cur, dst, report, progress); break;
                    case "load": next = DoLoadImage(spec, s, report, progress); break;
                    case "twoimg": next = DoTwoImage(spec, s, cur, dst, report, progress); break;
                    case "crop": next = DoCrop(spec, s, cur, gray, dst, report, progress); break;

                    default: Add(report, s, spec, false, "这个算子还没有实现：" + spec.Key); break;
                }
            }
            catch (Exception ex)
            {
                // OpenCV 对坏参数（ROI 越界、核尺寸为 0 之类）是直接抛异常上来的。
                // 这里兜成「这一步失败 + 人话原因」：一个算子不该把整条视觉流程打断，
                // 更不该把异常甩到界面上（AI 填的坏 ROI 也走这条）。
                Add(report, s, spec, false, "这个算子跑不起来：" + ex.Message);
                progress?.Report(spec.Name + " 失败：" + ex.Message);
            }
            finally { gray.Dispose(); }
            return next;
        }

        // ===================== 找直线 / 直线度 =====================

        static void DoLine(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress, bool straightness)
        {
            var (cx, cy, len, wide, ang, pol, minGrad, samples, keep) = LineParams(spec, s, gray);
            var pts = ScanLineEdges(gray, cx, cy, len, wide, ang, pol, minGrad, samples);
            ctx.Points.Clear();
            ctx.Points.AddRange(pts);

            var fit = VisionCaliper.FitLine(pts, keep);
            ctx.LastLine = fit;
            if (fit.Ok) ctx.Lines.Add(fit);          // 攒着：「两条线的夹角」「平行线间距」要取最近两条
            if (!fit.Ok)
            {
                Add(report, s, spec, false, fit.Fail);
                progress?.Report((straightness ? "看这条边够不够直：" : "找直线：") + fit.Fail);
                return;
            }
            DrawLine(dst, fit);
            string tail = straightness
                ? $"最大弯了 {fit.MaxDeviation:F2} 像素"
                : $"角度 {fit.AngleDeg:F2}°";
            Add(report, s, spec, true,
                $"{tail}｜({fit.X1:F1},{fit.Y1:F1})→({fit.X2:F1},{fit.Y2:F1})｜偏差 {fit.Rms:F2}｜用了 {fit.UsedPoints}/{pts.Count} 个点",
                fit.Rms, fit.UsedPoints,
                ("起点X", fit.X1), ("起点Y", fit.Y1), ("终点X", fit.X2), ("终点Y", fit.Y2),
                ("角度", fit.AngleDeg), ("偏差", fit.Rms), ("最大偏离", fit.MaxDeviation), ("点数", fit.UsedPoints));
            progress?.Report($"{spec.Name}：{tail}，偏差 {fit.Rms:F2} 像素");
        }

        /// <summary>沿边方向均匀放探针，每条探针在垂直方向上找最强的那条边。</summary>
        static List<Cv.Point2f> ScanLineEdges(Cv.Mat gray, double cx, double cy, double len, double wide,
            double angleDeg, VisionCaliper.Polarity pol, double minGrad, int samples)
        {
            var pts = new List<Cv.Point2f>();
            double a = angleDeg * Math.PI / 180.0;
            double ux = Math.Cos(a), uy = Math.Sin(a);          // 沿边方向
            double nx = -uy, ny = ux;                            // 垂直（搜索）方向
            for (int i = 0; i < samples; i++)
            {
                double t = -len / 2 + len * i / Math.Max(1, samples - 1);
                double px = cx + ux * t, py = cy + uy * t;
                double sx = px - nx * wide / 2, sy = py - ny * wide / 2;
                double ex = px + nx * wide / 2, ey = py + ny * wide / 2;
                double step = Math.Sqrt((ex - sx) * (ex - sx) + (ey - sy) * (ey - sy)) / (samples > 0 ? 1 : 1);
                var vals = VisionCaliper.SampleProfile(gray, sx, sy, ex, ey, 41);
                var hits = VisionCaliper.FindEdges(vals, pol, minGrad,
                    Math.Sqrt((ex - sx) * (ex - sx) + (ey - sy) * (ey - sy)) / (vals.Length - 1));
                if (hits.Count == 0) continue;
                var best = hits[0];
                foreach (var h in hits) if (h.Strength > best.Strength) best = h;
                pts.Add(VisionCaliper.PointAlong(sx, sy, ex, ey, best.Position));
            }
            return pts;
        }

        // ===================== 找多条直线 =====================

        static void DoLines(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var (cx, cy, len, wide, ang, pol, minGrad, samples, keep) = LineParams(spec, s, gray);
            int maxHits = (int)Num(spec, s, "maxHits", 3);
            double minGap = Num(spec, s, "minGap", 12);

            // 每条探针上把所有边缘都收下来，按「在搜索方向上的位置」聚类成若干条边
            var buckets = new List<(double Offset, Cv.Point2f P)>();
            double a = ang * Math.PI / 180.0, ux = Math.Cos(a), uy = Math.Sin(a), nx = -uy, ny = ux;
            for (int i = 0; i < samples; i++)
            {
                double t = -len / 2 + len * i / Math.Max(1, samples - 1);
                double px = cx + ux * t, py = cy + uy * t;
                double sx = px - nx * wide / 2, sy = py - ny * wide / 2;
                double ex = px + nx * wide / 2, ey = py + ny * wide / 2;
                double seg = Math.Sqrt((ex - sx) * (ex - sx) + (ey - sy) * (ey - sy));
                var vals = VisionCaliper.SampleProfile(gray, sx, sy, ex, ey, 61);
                foreach (var h in VisionCaliper.FindEdges(vals, pol, minGrad, seg / (vals.Length - 1)))
                    buckets.Add((h.Position, VisionCaliper.PointAlong(sx, sy, ex, ey, h.Position)));
            }

            var groups = ClusterByOffset(buckets, minGap);
            var taken = groups.OrderByDescending(g => g.Count).Take(Math.Max(1, maxHits)).ToList();
            ctx.Points.Clear();
            ctx.Points.AddRange(groups.SelectMany(g => g));

            if (taken.Count == 0) { Add(report, s, spec, false, "一条边都没找到：把「边要有多明显」调小，或确认框里有边"); return; }

            double worst = 0; int total = 0;
            foreach (var g in taken)
            {
                var fit = VisionCaliper.FitLine(g, keep);
                if (!fit.Ok) continue;
                ctx.Lines.Add(fit);
                DrawLine(dst, fit);
                worst = Math.Max(worst, fit.Rms);
                total++;
            }
            ctx.LastLine = null;
            Add(report, s, spec, total > 0,
                $"找到 {total} 条边｜最大偏差 {worst:F2}｜边缘点共 {ctx.Points.Count} 个", worst, total,
                ("条数", total), ("最大偏差", worst), ("点数", ctx.Points.Count));
            progress?.Report($"{spec.Name}：{total} 条边");
        }

        /// <summary>按搜索方向上的位置把边缘点分成几组（相邻差 &gt; minGap 就断开了）。</summary>
        static List<List<Cv.Point2f>> ClusterByOffset(List<(double Offset, Cv.Point2f P)> buckets, double minGap)
        {
            var outGroups = new List<List<Cv.Point2f>>();
            if (buckets.Count == 0) return outGroups;
            var sorted = buckets.OrderBy(b => b.Offset).ToList();
            var cur = new List<Cv.Point2f> { sorted[0].P };
            double last = sorted[0].Offset;
            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i].Offset - last > minGap) { outGroups.Add(cur); cur = new List<Cv.Point2f>(); }
                cur.Add(sorted[i].P);
                last = sorted[i].Offset;
            }
            outGroups.Add(cur);
            return outGroups.Where(g => g.Count >= 3).ToList();
        }

        // ===================== 找圆 / 找圆弧 =====================

        static void DoCircle(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress, bool arc)
        {
            var roi = RoiOf(spec, s, gray);
            double cx = Num(spec, s, "cx", 0), cy = Num(spec, s, "cy", 0);
            if (cx <= 0 || cy <= 0) { cx = roi.Left + roi.Width / 2.0; cy = roi.Top + roi.Height / 2.0; }
            double r0 = Num(spec, s, "radius", 60), tol = Num(spec, s, "tol", 20);
            var pol = PolarityOf(spec, s);
            double minGrad = Num(spec, s, "minGrad", 8);
            int samples = (int)Num(spec, s, "samples", 120);
            double keep = Num(spec, s, "keep", 0.9);
            double a0 = arc ? Num(spec, s, "a0", 0) : 0;
            double a1 = arc ? Num(spec, s, "a1", 360) : 360;
            if (Math.Abs(a1 - a0) < 1) { a0 = 0; a1 = 360; }

            var pts = new List<Cv.Point2f>();
            for (int i = 0; i < samples; i++)
            {
                double deg = a0 + (a1 - a0) * i / Math.Max(1, samples);
                double rad = deg * Math.PI / 180.0;
                double dx = Math.Cos(rad), dy = Math.Sin(rad);
                double sx = cx + dx * Math.Max(1, r0 - tol), sy = cy + dy * Math.Max(1, r0 - tol);
                double ex = cx + dx * (r0 + tol), ey = cy + dy * (r0 + tol);
                double seg = Math.Sqrt((ex - sx) * (ex - sx) + (ey - sy) * (ey - sy));
                var vals = VisionCaliper.SampleProfile(gray, sx, sy, ex, ey, 41);
                var hits = VisionCaliper.FindEdges(vals, pol, minGrad, seg / (vals.Length - 1));
                if (hits.Count == 0) continue;
                var best = hits[0];
                foreach (var h in hits) if (h.Strength > best.Strength) best = h;
                pts.Add(VisionCaliper.PointAlong(sx, sy, ex, ey, best.Position));
            }
            ctx.Points.Clear();
            ctx.Points.AddRange(pts);

            var fit = VisionCaliper.FitCircle(pts, keep);
            ctx.LastCircle = fit;
            if (fit.Ok) { ctx.Circles.Add(fit); ctx.LastPoint = new Cv.Point2f((float)fit.Cx, (float)fit.Cy); }
            if (!fit.Ok) { Add(report, s, spec, false, fit.Fail); return; }

            dst.Circle((int)Math.Round(fit.Cx), (int)Math.Round(fit.Cy), Math.Max(1, (int)Math.Round(fit.R)), Ink, 2);
            dst.Line((int)Math.Round(fit.Cx) - 7, (int)Math.Round(fit.Cy), (int)Math.Round(fit.Cx) + 7, (int)Math.Round(fit.Cy), Ink, 1);
            dst.Line((int)Math.Round(fit.Cx), (int)Math.Round(fit.Cy) - 7, (int)Math.Round(fit.Cx), (int)Math.Round(fit.Cy) + 7, Ink, 1);
            foreach (var p in pts) dst.Circle((int)Math.Round(p.X), (int)Math.Round(p.Y), 2, Dot, 1);

            string span = arc ? $"｜角度 {fit.StartDeg:F0}°~{fit.EndDeg:F0}°" : "";
            Add(report, s, spec, true,
                $"圆心 ({fit.Cx:F1},{fit.Cy:F1})｜半径 {fit.R:F2}（直径 {fit.R * 2:F2}）｜偏差 {fit.Rms:F2}｜用了 {fit.UsedPoints}/{pts.Count} 个点{span}",
                fit.R, fit.UsedPoints,
                ("圆心X", fit.Cx), ("圆心Y", fit.Cy), ("半径", fit.R), ("直径", fit.R * 2),
                ("偏差", fit.Rms), ("点数", fit.UsedPoints), ("起始角", fit.StartDeg), ("结束角", fit.EndDeg));
            progress?.Report($"{spec.Name}：半径 {fit.R:F2}，偏差 {fit.Rms:F2} 像素");
        }

        // ===================== 找边缘点 =====================

        static void DoEdgePoints(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var roi = RoiOf(spec, s, gray);
            double x0 = Num(spec, s, "x0", 0), y0 = Num(spec, s, "y0", 0);
            double x1 = Num(spec, s, "x1", 0), y1 = Num(spec, s, "y1", 0);
            if (x0 == 0 && y0 == 0) { x0 = roi.Left; y0 = roi.Top + roi.Height / 2.0; }
            if (x1 == 0 && y1 == 0) { x1 = roi.Right; y1 = y0; }
            int samples = (int)Num(spec, s, "samples", 200);
            var pol = PolarityOf(spec, s);
            double minGrad = Num(spec, s, "minGrad", 8);

            double seg = Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
            if (seg < 2) { Add(report, s, spec, false, "这条线太短（起点和终点几乎重合）"); return; }
            var vals = VisionCaliper.SampleProfile(gray, x0, y0, x1, y1, Math.Max(16, samples));
            var hits = VisionCaliper.FindEdges(vals, pol, minGrad, seg / (vals.Length - 1));

            ctx.Points.Clear();
            foreach (var h in hits) ctx.Points.Add(VisionCaliper.PointAlong(x0, y0, x1, y1, h.Position));
            dst.Line((int)Math.Round(x0), (int)Math.Round(y0), (int)Math.Round(x1), (int)Math.Round(y1), Ink, 1);
            foreach (var p in ctx.Points) dst.Circle((int)Math.Round(p.X), (int)Math.Round(p.Y), 3, Dot, 1);

            if (ctx.Points.Count == 0) { Add(report, s, spec, false, "这条线上没找到台阶：把「边要有多明显」调小试试"); return; }
            double minSpace = double.MaxValue;
            for (int i = 1; i < ctx.Points.Count; i++)
            {
                double d = Math.Sqrt(Sq(ctx.Points[i].X - ctx.Points[i - 1].X) + Sq(ctx.Points[i].Y - ctx.Points[i - 1].Y));
                minSpace = Math.Min(minSpace, d);
            }
            if (minSpace != double.MaxValue) ctx.LastLength = minSpace;
            if (ctx.Points.Count > 0) ctx.LastPoint = ctx.Points[0];
            Add(report, s, spec, true,
                $"找到 {ctx.Points.Count} 个边缘点｜最近两个隔 {minSpace:F1} 像素",
                minSpace == double.MaxValue ? 0 : minSpace, ctx.Points.Count,
                ("点数", ctx.Points.Count), ("最小间距", minSpace == double.MaxValue ? 0 : minSpace));
            progress?.Report($"{spec.Name}：{ctx.Points.Count} 个边缘点");
        }

        // ===================== 找矩形四角 =====================

        static void DoQuad(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var roi = RoiOf(spec, s, gray);
            double minArea = Num(spec, s, "minArea", 2000);
            double canny = Num(spec, s, "canny", 80);
            double approx = Num(spec, s, "approx", 4);

            using var sub = new Cv.Mat(gray, roi);
            using var blur = new Cv.Mat();
            Cv.Cv2.GaussianBlur(sub, blur, new Cv.Size(3, 3), 0);
            using var edges = new Cv.Mat();
            Cv.Cv2.Canny(blur, edges, canny * 0.4, canny);
            Cv.Cv2.FindContours(edges, out Cv.Point[][] cs, out _, Cv.RetrievalModes.External,
                Cv.ContourApproximationModes.ApproxSimple);

            Cv.Point[] best = null; double bestArea = 0;
            foreach (var c in cs)
            {
                double area = Math.Abs(Cv.Cv2.ContourArea(c));
                if (area < minArea || area <= bestArea) continue;
                var ap = Cv.Cv2.ApproxPolyDP(c, Math.Max(0.5, approx), true);
                if (ap.Length != 4) continue;
                if (!IsRectangular(ap)) continue;
                best = ap; bestArea = area;
            }
            if (best == null)
            {
                Add(report, s, spec, false, "框里没找到四边形工件：把「最小面积」调小，或把框缩小到只套住工件");
                dst.Rectangle(roi, InkWarn, 1);
                return;
            }
            var corners = best.Select(p => new Cv.Point2f(p.X + roi.Left, p.Y + roi.Top)).ToArray();
            ctx.Corners.Clear();
            ctx.Corners.AddRange(corners);
            ctx.LastPoint = new Cv.Point2f((float)corners.Average(p => p.X), (float)corners.Average(p => p.Y));
            ctx.LastLength = Dist(corners[0], corners[2]);
            ctx.LastAngle = Math.Atan2(corners[1].Y - corners[0].Y, corners[1].X - corners[0].X) * 180 / Math.PI;
            dst.Polylines(new[] { System.Array.ConvertAll(corners, q => new Cv.Point((int)q.X, (int)q.Y)) }, true, Ink, 2);
            foreach (var p in corners) dst.Circle((int)Math.Round(p.X), (int)Math.Round(p.Y), 4, Dot, 2);

            double w = Dist(corners[0], corners[1]), h = Dist(corners[1], corners[2]);
            double cx = corners.Average(p => p.X), cy = corners.Average(p => p.Y);
            double ang = Math.Atan2(corners[1].Y - corners[0].Y, corners[1].X - corners[0].X) * 180 / Math.PI;
            Add(report, s, spec, true,
                $"中心 ({cx:F1},{cy:F1})｜{w:F1}×{h:F1}｜角度 {ang:F2}°｜面积 {bestArea:F0}", ang, 4,
                ("中心X", cx), ("中心Y", cy), ("宽", w), ("高", h), ("角度", ang), ("面积", bestArea),
                ("角1X", corners[0].X), ("角1Y", corners[0].Y), ("角2X", corners[1].X), ("角2Y", corners[1].Y),
                ("角3X", corners[2].X), ("角3Y", corners[2].Y), ("角4X", corners[3].X), ("角4Y", corners[3].Y));
            progress?.Report($"{spec.Name}：{w:F1}×{h:F1}，角度 {ang:F2}°");
        }

        /// <summary>四个角都接近直角才算矩形（避免把 L 形、三角形当方块）。</summary>
        static bool IsRectangular(Cv.Point[] p)
        {
            for (int i = 0; i < 4; i++)
            {
                var a = p[i]; var b = p[(i + 1) % 4]; var c = p[(i + 2) % 4];
                double v1x = b.X - a.X, v1y = b.Y - a.Y, v2x = c.X - b.X, v2y = c.Y - b.Y;
                double n1 = Math.Sqrt(v1x * v1x + v1y * v1y), n2 = Math.Sqrt(v2x * v2x + v2y * v2y);
                if (n1 < 4 || n2 < 4) return false;
                double cos = Math.Abs((v1x * v2x + v1y * v2y) / (n1 * n2));
                if (cos > 0.35) return false;         // 夹角偏离 90° 太多
            }
            return true;
        }

        // ===================== 找尖角 =====================

        static void DoCorners(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var roi = RoiOf(spec, s, gray);
            int maxCorners = (int)Num(spec, s, "maxCorners", 10);
            double quality = Num(spec, s, "quality", 0.05);
            double minDist = Num(spec, s, "minDist", 20);

            using var sub = new Cv.Mat(gray, roi);
            var found = Cv.Cv2.GoodFeaturesToTrack(sub, maxCorners, quality, minDist, null, 3, true, 0.04);
            ctx.Points.Clear();
            foreach (var p in found)
            {
                var g = new Cv.Point2f(p.X + roi.Left, p.Y + roi.Top);
                ctx.Points.Add(g);
                dst.Line((int)g.X - 6, (int)g.Y, (int)g.X + 6, (int)g.Y, Ink, 1);
                dst.Line((int)g.X, (int)g.Y - 6, (int)g.X, (int)g.Y + 6, Ink, 1);
            }
            ctx.Corners.Clear();
            ctx.Corners.AddRange(ctx.Points);
            if (ctx.Points.Count > 0) ctx.LastPoint = ctx.Points[0];
            if (ctx.Points.Count == 0)
            { Add(report, s, spec, false, "没找到尖角：把「角要有多突出」调小（例如 0.01）"); return; }
            Add(report, s, spec, true,
                $"找到 {ctx.Points.Count} 个尖角｜第一个 ({ctx.Points[0].X:F0},{ctx.Points[0].Y:F0})",
                0, ctx.Points.Count,
                ("个数", ctx.Points.Count), ("第一个X", ctx.Points[0].X), ("第一个Y", ctx.Points[0].Y));
            progress?.Report($"{spec.Name}：{ctx.Points.Count} 个尖角");
        }

        // ===================== 用上面攒下的点做拟合 =====================

        static void DoFitLineFromPoints(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            if (ctx.Points.Count < 2) { Add(report, s, spec, false, "手上还没有边缘点：先跑一步「找边缘点」或「找直线」"); return; }
            var fit = VisionCaliper.FitLine(ctx.Points, Num(spec, s, "keep", 0.9));
            ctx.LastLine = fit;
            if (fit.Ok) ctx.Lines.Add(fit);
            if (!fit.Ok) { Add(report, s, spec, false, fit.Fail); return; }
            DrawLine(dst, fit);
            Add(report, s, spec, true,
                $"角度 {fit.AngleDeg:F2}°｜偏差 {fit.Rms:F2}｜用了 {fit.UsedPoints}/{ctx.Points.Count} 个点", fit.AngleDeg, fit.UsedPoints,
                ("起点X", fit.X1), ("起点Y", fit.Y1), ("终点X", fit.X2), ("终点Y", fit.Y2),
                ("角度", fit.AngleDeg), ("偏差", fit.Rms), ("点数", fit.UsedPoints));
        }

        static void DoFitCircleFromPoints(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            if (ctx.Points.Count < 3) { Add(report, s, spec, false, "手上的点不够（拟圆至少 3 个）：先跑一步「找边缘点」"); return; }
            var fit = VisionCaliper.FitCircle(ctx.Points, Num(spec, s, "keep", 0.9));
            ctx.LastCircle = fit;
            if (fit.Ok) { ctx.Circles.Add(fit); ctx.LastPoint = new Cv.Point2f((float)fit.Cx, (float)fit.Cy); }
            if (!fit.Ok) { Add(report, s, spec, false, fit.Fail); return; }
            dst.Circle((int)Math.Round(fit.Cx), (int)Math.Round(fit.Cy), Math.Max(1, (int)Math.Round(fit.R)), Ink, 2);
            Add(report, s, spec, true,
                $"圆心 ({fit.Cx:F1},{fit.Cy:F1})｜半径 {fit.R:F2}｜偏差 {fit.Rms:F2}｜用了 {fit.UsedPoints}/{ctx.Points.Count} 个点",
                fit.R, fit.UsedPoints,
                ("圆心X", fit.Cx), ("圆心Y", fit.Cy), ("半径", fit.R), ("偏差", fit.Rms), ("点数", fit.UsedPoints));
        }

        /// <summary>连通域那一族（connection / select_shape / region_features / shape_trans）：统一阈值 → 拆块 → 按模式报数。</summary>
        static void DoComponents(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress, string mode)
        {
            var roi = RoiOf(spec, s, gray);
            using var sub = new Cv.Mat(gray, roi);
            using var bin = new Cv.Mat();
            Cv.Cv2.Threshold(sub, bin, Num(spec, s, "thr", 128), 255, Cv.ThresholdTypes.Binary);
            using var labels = new Cv.Mat();
            using var stats = new Cv.Mat();
            using var cents = new Cv.Mat();
            int n = Cv.Cv2.ConnectedComponentsWithStats(bin, labels, stats, cents);
            {
                int cnt = Math.Max(0, n - 1);
                int best = -1, bestArea = 0;
                int loA = (int)Num(spec, s, "loA", 50), hiA = (int)Num(spec, s, "hiA", 1000000);
                double maxAspect = Num(spec, s, "maxAspect", 3), minFill = Num(spec, s, "minFill", 0.4);
                int kept = 0;
                for (int i = 1; i < n; i++)
                {
                    int a = stats.At<int>(i, 4);
                    int bw = stats.At<int>(i, 2), bh = stats.At<int>(i, 3);
                    double aspect = Math.Max(bw, bh) / (double)Math.Max(1, Math.Min(bw, bh));
                    double fill = a / (double)Math.Max(1, bw * bh);
                    if (mode == "select" && (a < loA || (hiA > 0 && a > hiA) || aspect > maxAspect || fill < minFill)) continue;
                    if (mode == "select") kept++;
                    if (a > bestArea) { bestArea = a; best = i; }
                }
                if (best < 0)
                {
                    Add(report, s, spec, false, "一块目标都没找到：把「分界灰度」调松（或确认目标比背景亮）");
                    return;
                }
                double cx = roi.X + cents.At<double>(best, 0), cy = roi.Y + cents.At<double>(best, 1);
                ctx.LastPoint = new Cv.Point2f((float)cx, (float)cy);
                DrawPt(dst, ctx.LastPoint.Value);
                if (mode == "hull")
                {
                    using var one = new Cv.Mat();
                    Cv.Cv2.Compare(labels, best, one, Cv.CmpTypes.EQ);
                    Cv.Cv2.FindContours(one, out Cv.Point[][] cs, out _, Cv.RetrievalModes.External, Cv.ContourApproximationModes.ApproxSimple);
                    var biggest = cs.OrderByDescending(c => Cv.Cv2.ContourArea(c)).FirstOrDefault();
                    if (biggest == null) { Add(report, s, spec, false, "最大块的轮廓提不出来"); return; }
                    var hull = Cv.Cv2.ConvexHull(biggest);
                    double hullArea = Cv.Cv2.ContourArea(hull);
                    double realArea = Cv.Cv2.ContourArea(biggest);
                    double conv = realArea > 1e-9 ? hullArea / realArea : 0;
                    dst.Polylines(new[] { Array.ConvertAll(hull, p => new Cv.Point(p.X + roi.X, p.Y + roi.Y)) }, true, Ink, 2);
                    Add(report, s, spec, true, $"凸包 {hullArea:F0}｜实际 {realArea:F0}｜凸性 {conv:F3}", conv, 1,
                        ("凸包面积", hullArea), ("实际面积", realArea), ("凸性", conv));
                    return;
                }
                if (mode == "feat")
                {
                    using var one = new Cv.Mat();
                    Cv.Cv2.Compare(labels, best, one, Cv.CmpTypes.EQ);
                    Cv.Cv2.FindContours(one, out Cv.Point[][] cs, out _, Cv.RetrievalModes.External, Cv.ContourApproximationModes.ApproxSimple);
                    var biggest = cs.OrderByDescending(c => Cv.Cv2.ContourArea(c)).FirstOrDefault();
                    if (biggest == null) { Add(report, s, spec, false, "最大块的轮廓提不出来"); return; }
                    var rr = Cv.Cv2.MinAreaRect(biggest);
                    double fill = bestArea / (double)Math.Max(1, stats.At<int>(best, 2) * stats.At<int>(best, 3));
                    double major = Math.Max(rr.Size.Width, rr.Size.Height), minor = Math.Min(rr.Size.Width, rr.Size.Height);
                    double rot = ((rr.Angle + (rr.Size.Width < rr.Size.Height ? 90 : 0)) % 180 + 180) % 180;
                    Cv.Cv2.Circle(dst, (int)cx, (int)cy, Math.Max(2, (int)(major / 2)), InkWarn, 1);
                    Add(report, s, spec, true,
                        $"面积 {bestArea}｜中心 ({cx:F1},{cy:F1})｜外接 {stats.At<int>(best, 2)}×{stats.At<int>(best, 3)}｜填充 {fill:F2}｜最小外接矩形 {major:F1}×{minor:F1}@{rot:F1}°",
                        bestArea, 1,
                        ("面积", bestArea), ("中心X", cx), ("中心Y", cy),
                        ("宽", stats.At<int>(best, 2)), ("高", stats.At<int>(best, 3)), ("填充率", fill),
                        ("长", major), ("短", minor), ("角度", rot));
                    return;
                }
                string head = mode == "select" ? $"合格块 {kept}/{cnt}" : $"共 {cnt} 块";
                Add(report, s, spec, cnt > 0 && (mode != "select" || kept > 0),
                    $"{head}｜最大块 {bestArea} 像素，中心 ({cx:F1},{cy:F1})", bestArea, cnt,
                    ("块数", mode == "select" ? kept : cnt), ("最大块面积", bestArea),
                    ("最大块中心X", cx), ("最大块中心Y", cy));
            }
        }

        /// <summary>提取轮廓线（gen_contour_region_xld）：报条数、最长周长、最大面积。</summary>
        static void DoContourLines(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            var roi = RoiOf(spec, s, gray);
            using var sub = new Cv.Mat(gray, roi);
            using var bin = new Cv.Mat();
            Cv.Cv2.Threshold(sub, bin, Num(spec, s, "thr", 128), 255, Cv.ThresholdTypes.Binary);
            Cv.Cv2.FindContours(bin, out Cv.Point[][] cs, out _, Cv.RetrievalModes.External, Cv.ContourApproximationModes.ApproxSimple);
            if (cs.Length == 0) { Add(report, s, spec, false, "没有轮廓：把分界灰度调松一点"); return; }
            double bestLen = 0, bestArea = 0;
            foreach (var c in cs)
            {
                double len = Cv.Cv2.ArcLength(c, true);
                double area = Cv.Cv2.ContourArea(c);
                if (len > bestLen) { bestLen = len; bestArea = area; }
                dst.Polylines(new[] { Array.ConvertAll(c, p => new Cv.Point(p.X + roi.X, p.Y + roi.Y)) }, true, Ink, 1);
            }
            Add(report, s, spec, true, $"轮廓 {cs.Length} 条｜最长 {bestLen:F1}｜面积 {bestArea:F0}",
                bestLen, cs.Length, ("轮廓条数", cs.Length), ("最大周长", bestLen), ("最大面积", bestArea));
        }

        /// <summary>找一对边（measure_pairs）：一条剖面上取前两条边，报位置 + 宽度 + 中心。</summary>
        static void DoEdgePair(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            double cx = Num(spec, s, "cx", 0), cy = Num(spec, s, "cy", 0);
            if (cx <= 0 && cy <= 0) { cx = gray.Cols / 2.0; cy = gray.Rows / 2.0; }
            double width = Math.Max(8, Num(spec, s, "width", 80));
            double a = Num(spec, s, "angle", 0) * Math.PI / 180.0;
            double nx = -Math.Sin(a), ny = Math.Cos(a);
            var vals = VisionCaliper.SampleProfile(gray, cx - nx * width / 2, cy - ny * width / 2,
                                                  cx + nx * width / 2, cy + ny * width / 2, Math.Max(32, (int)width * 2));
            double step = width / (vals.Length - 1);
            var hits = VisionCaliper.FindEdges(vals, VisionCaliper.Polarity.Any, Num(spec, s, "minGrad", 8), step);
            if (hits.Count < 2)
            {
                Add(report, s, spec, false, $"只找到 {hits.Count} 条边（要两条）：调小「最小灰度差」，或把带子挪到两条边都过的地方");
                return;
            }
            var h1 = hits.OrderBy(h => h.Position).First();
            var h2 = hits.OrderBy(h => h.Position).Skip(1).First();
            var p1 = VisionCaliper.PointAlong(cx - nx * width / 2, cy - ny * width / 2, cx + nx * width / 2, cy + ny * width / 2, h1.Position);
            var p2 = VisionCaliper.PointAlong(cx - nx * width / 2, cy - ny * width / 2, cx + nx * width / 2, cy + ny * width / 2, h2.Position);
            double gap = Math.Abs(h2.Position - h1.Position);
            DrawPt(dst, p1); DrawPt(dst, p2);
            dst.Line((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, Ink, 1);
            Add(report, s, spec, true, $"两条边相距 {gap:F2} 像素", gap, 2,
                ("左边位置", h1.Position), ("右边位置", h2.Position), ("宽度", gap), ("中心位置", (h1.Position + h2.Position) / 2));
        }

        /// <summary>灰度极值（min_max_gray）。</summary>
        static void DoMinMaxGray(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            var roi = RoiOf(spec, s, gray);
            using var sub = new Cv.Mat(gray, roi);
            Cv.Cv2.MinMaxLoc(sub, out double mn, out double mx, out Cv.Point mnLoc, out Cv.Point mxLoc);
            double lo = Num(spec, s, "loL", 0), hi = Num(spec, s, "hiL", 128);
            using var mask = new Cv.Mat();
            Cv.Cv2.InRange(sub, new Cv.Scalar(Math.Min(lo, hi)), new Cv.Scalar(Math.Max(lo, hi)), mask);
            int inRange = Cv.Cv2.CountNonZero(mask);
            double ratio = 100.0 * inRange / Math.Max(1, roi.Width * roi.Height);
            DrawPt(dst, new Cv.Point2f(mxLoc.X + roi.X, mxLoc.Y + roi.Y));
            Add(report, s, spec, true, $"最亮 {mx:F0}｜最暗 {mn:F0}｜区间内 {ratio:F1}%", mx, 1,
                ("最亮灰度", mx), ("最暗灰度", mn), ("最亮位置X", mxLoc.X + roi.X), ("最亮位置Y", mxLoc.Y + roi.Y),
                ("区间占比", ratio));
        }

        /// <summary>按颜色取区域（decompose3 + 色相/饱和度阈值）。</summary>
        static void DoHueSelect(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var roi = RoiOf(spec, s, cur);
            using var sub = new Cv.Mat(cur, roi);
            using var hsv = new Cv.Mat();
            Cv.Cv2.CvtColor(sub, hsv, Cv.ColorConversionCodes.BGR2HSV);
            double hlo = Num(spec, s, "hlo", 0), hhi = Num(spec, s, "hhi", 10);
            using var mask = new Cv.Mat();
            Cv.Cv2.InRange(hsv, new Cv.Scalar(Math.Min(hlo, hhi), Num(spec, s, "slo", 80), Num(spec, s, "vlo", 40)),
                            new Cv.Scalar(Math.Max(hlo, hhi), 255, 255), mask);
            int count = Cv.Cv2.CountNonZero(mask);
            var mo = Cv.Cv2.Moments(mask, false);
            double cx = roi.X + (mo.M00 > 1e-9 ? mo.M10 / mo.M00 : roi.Width / 2.0);
            double cy = roi.Y + (mo.M00 > 1e-9 ? mo.M01 / mo.M00 : roi.Height / 2.0);
            double ratio = 100.0 * count / Math.Max(1, roi.Width * roi.Height);
            ctx.LastPoint = new Cv.Point2f((float)cx, (float)cy);
            if (count > 0) DrawPt(dst, ctx.LastPoint.Value);
            Add(report, s, spec, count > 0,
                count > 0 ? $"颜色块 {count} 像素（占框 {ratio:F1}%）｜中心 ({cx:F1},{cy:F1})"
                          : "这个色相/饱和度区间里一个像素都没取到：把色相范围放宽、饱和度门槛调低",
                count, count, ("面积", count), ("中心X", cx), ("中心Y", cy), ("占框多少", ratio));
        }

        /// <summary>整块像素变换：反色 / 直方图均衡 / 高斯模糊。</summary>
        static void DoPixelMap(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, string mode)
        {
            var roi = RoiOf(spec, s, gray);
            using var sub = new Cv.Mat(gray, roi);
            using var outp = new Cv.Mat(dst, roi);
            if (mode == "invert") Cv.Cv2.BitwiseNot(sub, outp);
            else if (mode == "equhisto") Cv.Cv2.EqualizeHist(sub, outp);
            else
            {
                int k = Math.Max(3, (int)Math.Round(Num(spec, s, "kernel", 5))) | 1;
                Cv.Cv2.GaussianBlur(sub, outp, new Cv.Size(k, k), 0);
            }
            Add(report, s, spec, true, mode == "invert" ? "已反色" : mode == "equhisto" ? "已均衡" : "已模糊",
                dst.Cols, 1, ("图宽", dst.Cols), ("图高", dst.Rows),
                ("核大小", mode == "gauss" ? Math.Max(3, (int)Math.Round(Num(spec, s, "kernel", 5))) | 1 : 0));
        }

        /// <summary>
        /// 阈值 / 形态学这一族（Halcon 的 threshold、auto_threshold、dyn_threshold、erosion、dilation、opening、closing、top_hat、bottom_hat）：
        /// 统一先得到一张「目标 = 白」的掩膜，再报面积 / 重心 / 占框比例 —— 这几个数正是下游筛选用得上的。
        /// </summary>
        static void DoMaskOp(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress, string mode)
        {
            var roi = RoiOf(spec, s, gray);
            using var sub = new Cv.Mat(gray, roi);
            using var bin = new Cv.Mat();
            using var tmp = new Cv.Mat();
            int k = Math.Max(1, (int)Math.Round(Num(spec, s, "kernel", 3)));
            using var kernel = Cv.Cv2.GetStructuringElement(Cv.MorphShapes.Rect, new Cv.Size(k, k));
            string how;
            try
            {
                if (mode == "thresh")
                {
                    double lo = Num(spec, s, "loL", 0), hi = Num(spec, s, "hiL", 128);
                    Cv.Cv2.InRange(sub, new Cv.Scalar(Math.Min(lo, hi)), new Cv.Scalar(Math.Max(lo, hi)), bin);
                    how = $"灰度 {Math.Min(lo, hi):F0}~{Math.Max(lo, hi):F0}";
                }
                else if (mode == "autothresh")
                {
                    Cv.Cv2.GaussianBlur(sub, tmp, new Cv.Size(3, 3), 0);
                    double cut = Cv.Cv2.Threshold(tmp, bin, 0, 255, Cv.ThresholdTypes.Binary | Cv.ThresholdTypes.Otsu);
                    how = $"自动分界 {cut:F0}";
                }
                else if (mode == "dynthresh")
                {
                    int w = Math.Max(3, (int)Math.Round(Num(spec, s, "window", 15))) | 1;
                    Cv.Cv2.GaussianBlur(sub, tmp, new Cv.Size(w, w), 0);
                    using var diff = new Cv.Mat();
                    Cv.Cv2.Absdiff(sub, tmp, diff);
                    Cv.Cv2.Threshold(diff, bin, Num(spec, s, "offset", 8), 255, Cv.ThresholdTypes.Binary);
                    string pick = Txt(spec, s, "pick");
                    if (pick == "只留亮的" || pick == "只留暗的")
                    {
                        using var side = new Cv.Mat();
                        Cv.Cv2.Compare(sub, tmp, side, pick == "只留亮的" ? Cv.CmpTypes.GT : Cv.CmpTypes.LT);
                        Cv.Cv2.BitwiseAnd(bin, side, bin);
                    }
                    how = $"局部背景差 > {Num(spec, s, "offset", 8):F0}（窗口 {w}）";
                }
                else if (mode is "erode" or "dilate" or "open" or "close")
                {
                    Cv.Cv2.Threshold(sub, bin, Num(spec, s, "thr", 128), 255, Cv.ThresholdTypes.Binary);
                    var mt = mode switch
                    {
                        "erode" => Cv.MorphTypes.Erode,
                        "dilate" => Cv.MorphTypes.Dilate,
                        "open" => Cv.MorphTypes.Open,
                        _ => Cv.MorphTypes.Close,
                    };
                    Cv.Cv2.MorphologyEx(bin, tmp, mt, kernel);
                    tmp.CopyTo(bin);
                    how = $"{mode} 核 {k}";
                }
                else
                {
                    Cv.Cv2.MorphologyEx(sub, tmp, mode == "tophat" ? Cv.MorphTypes.TopHat : Cv.MorphTypes.BlackHat, kernel);
                    Cv.Cv2.Threshold(tmp, bin, Num(spec, s, "thr", 20), 255, Cv.ThresholdTypes.Binary);
                    how = $"{(mode == "tophat" ? "亮细节" : "暗细节")} > {Num(spec, s, "thr", 20):F0}";
                }

                int count = Cv.Cv2.CountNonZero(bin);
                var mo = Cv.Cv2.Moments(bin, false);
                double area = mo.M00;
                double cx = roi.X + (area > 1e-9 ? mo.M10 / area : roi.Width / 2.0);
                double cy = roi.Y + (area > 1e-9 ? mo.M01 / area : roi.Height / 2.0);
                double ratio = 100.0 * count / Math.Max(1, roi.Width * roi.Height);
                ctx.LastPoint = new Cv.Point2f((float)cx, (float)cy);
                dst.Rectangle(roi, Ink, 1);
                if (count > 0) DrawPt(dst, ctx.LastPoint.Value);
                Add(report, s, spec, count > 0,
                    count > 0
                        ? $"{how}｜面积 {count} 像素（占框 {ratio:F1}%）｜中心 ({cx:F1}, {cy:F1})"
                        : $"{how}｜一个目标都没取到：调松参数（灰度区间 / 分界灰度 / 偏移量）",
                    count, count,
                    ("面积", count), ("中心X", cx), ("中心Y", cy), ("占框多少", ratio));
            }
            catch (Exception ex)
            {
                Add(report, s, spec, false, "这一步跑不动：" + ex.Message);
            }
        }

        /// <summary>边缘图（Halcon edges_image / Canny）：把整幅图的边缘画成细线，报边缘点占比。</summary>
        static void DoEdgeMap(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            var roi = RoiOf(spec, s, gray);
            using var sub = new Cv.Mat(gray, roi);
            using var edges = new Cv.Mat();
            double lo = Num(spec, s, "lo", 50), hi = Math.Max(lo + 1, Num(spec, s, "hi", 150));
            Cv.Cv2.Canny(sub, edges, lo, hi);
            int n = Cv.Cv2.CountNonZero(edges);
            double ratio = 100.0 * n / Math.Max(1, roi.Width * roi.Height);
            using var mask = new Cv.Mat(dst, roi);
            mask.SetTo(new Cv.Scalar(0, 0, 255), edges);
            Add(report, s, spec, n > 0,
                n > 0 ? $"边缘点 {n} 个（占框 {ratio:F2}%）" : "这条区间一个边缘点都没找到：把两个阈值都调小",
                n, n, ("边缘点占比", ratio), ("边缘点个数", n));
        }

        /// <summary>灰度投影（Halcon gray_projections）：按行/按列压成一条曲线，找最亮和最暗的位置。</summary>
        static void DoGrayProjection(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            var roi = RoiOf(spec, s, gray);
            using var sub = new Cv.Mat(gray, roi);
            bool byRow = Txt(spec, s, "dir") != "按列投影";
            int len = byRow ? sub.Rows : sub.Cols;
            int span = byRow ? sub.Cols : sub.Rows;
            var avg = new double[len];
            for (int i = 0; i < len; i++)
            {
                double sum = 0;
                for (int j = 0; j < span; j++)
                    sum += byRow ? sub.At<byte>(i, j) : sub.At<byte>(j, i);
                avg[i] = sum / Math.Max(1, span);
            }
            double best = double.MinValue, worst = double.MaxValue;
            int bi = 0, wi = 0;
            for (int i = 0; i < len; i++)
            {
                double v = avg[i];
                if (v > best) { best = v; bi = i; }
                if (v < worst) { worst = v; wi = i; }
            }
            double peak = (byRow ? roi.Y : roi.X) + bi;
            double valley = (byRow ? roi.Y : roi.X) + wi;
            Add(report, s, spec, true,
                $"{(byRow ? "行" : "列")}方向｜最亮在第 {peak:F0}｜最暗在第 {valley:F0}",
                best, len,
                ("峰值位置", peak), ("峰值灰度", best), ("谷值位置", valley), ("谷值灰度", worst));
        }

        /// <summary>刚体换算（Halcon vector_angle_to_rigid + affine_trans_point_2d）：绕基准点转 + 平移。</summary>
        static void DoRigidPoint(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            double bx = Num(spec, s, "bx", 0), by = Num(spec, s, "by", 0);
            double ang = Num(spec, s, "ang", 0) * Math.PI / 180.0;
            double px = Num(spec, s, "px", 0), py = Num(spec, s, "py", 0);
            double dx = px - bx, dy = py - by;
            double x = bx + dx * Math.Cos(ang) - dy * Math.Sin(ang) + Num(spec, s, "offx", 0);
            double y = by + dx * Math.Sin(ang) + dy * Math.Cos(ang) + Num(spec, s, "offy", 0);
            DrawPt(dst, new Cv.Point2f((float)bx, (float)by));
            DrawPt(dst, new Cv.Point2f((float)x, (float)y));
            dst.Line((int)bx, (int)by, (int)x, (int)y, Ink, 1);
            Add(report, s, spec, true,
                $"换算后 ({x:F2}, {y:F2})｜绕 ({bx:F0},{by:F0}) 转 {Num(spec, s, "ang", 0):F1}°",
                Math.Sqrt(dx * dx + dy * dy), 1,
                ("换算后X", x), ("换算后Y", y), ("相对角度", Num(spec, s, "ang", 0)));
        }

        /// <summary>按角度转图（Halcon affine_trans_image / rotate_image）：绕中心转 + 缩放 + 平移，尺寸不变。</summary>
        static void DoAffineImage(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            double ang = Num(spec, s, "ang", 0), scale = Math.Max(0.05, Num(spec, s, "scale", 1));
            var center = new Cv.Point2f(gray.Cols / 2f, gray.Rows / 2f);
            using var m = Cv.Cv2.GetRotationMatrix2D(center, ang, scale);
            m.Set(0, 2, m.At<double>(0, 2) + Num(spec, s, "dx", 0));
            m.Set(1, 2, m.At<double>(1, 2) + Num(spec, s, "dy", 0));
            Cv.Cv2.WarpAffine(gray, dst, m, new Cv.Size(gray.Cols, gray.Rows));
            Add(report, s, spec, true, $"已转 {ang:F1}°、缩放 {scale:F2}", scale, 1,
                ("图宽", dst.Cols), ("图高", dst.Rows), ("转角", ang));
        }

        /// <summary>
        /// 找一个边缘点（SGamma 的边缘搜索 · 点）：沿边方向均匀取 9 条剖面，每条上找「第一个」或「最强」的边，
        /// 最后取中位数当结果 —— 只扫一条线容易被一个噪点带跑。
        /// </summary>
        static void DoEdgePoint(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            double cx = Num(spec, s, "cx", 0), cy = Num(spec, s, "cy", 0);
            if (cx <= 0 && cy <= 0) { cx = gray.Cols / 2.0; cy = gray.Rows / 2.0; }
            double length = Math.Max(8, Num(spec, s, "length", 120));
            double width = Math.Max(4, Num(spec, s, "width", 60));
            double minGrad = Math.Max(0, Num(spec, s, "minGrad", 8));
            bool strongest = Txt(spec, s, "pick") == "最强的边";
            bool sub = Num(spec, s, "sub", 1) >= 1;

            double a = Num(spec, s, "angle", 0) * Math.PI / 180.0;
            double ex = Math.Cos(a), ey = Math.Sin(a);          // 沿边方向
            double nx = -ey, ny = ex;                            // 搜索方向（垂直边）

            const int profiles = 9;
            int count = Math.Max(16, (int)Math.Round(width * 2));
            double step = width / (count - 1);
            var pos = new List<double>();
            var str = new List<double>();
            for (int k = 0; k < profiles; k++)
            {
                double t = (k / (double)(profiles - 1) - 0.5) * length;
                double px = cx + ex * t, py = cy + ey * t;
                var vals = VisionCaliper.SampleProfile(gray,
                    px - nx * width / 2, py - ny * width / 2,
                    px + nx * width / 2, py + ny * width / 2, count);
                var hits = VisionCaliper.FindEdges(vals, PolarityOf(spec, s), minGrad, step);
                if (hits.Count == 0) continue;
                var h = strongest ? hits.OrderByDescending(q => q.Strength).First()
                                  : hits.OrderBy(q => q.Position).First();
                pos.Add(h.Position);
                str.Add(h.Strength);
            }
            if (pos.Count == 0)
            {
                Add(report, s, spec, false,
                    "这条带子里没找到边：把「最小灰度差」调小、换个搜索角度，或者把带子挪到真有边的地方");
                return;
            }
            pos.Sort(); str.Sort();
            double medPos = pos[pos.Count / 2];
            double medStr = str[str.Count / 2];
            var p = VisionCaliper.PointAlong(cx - nx * width / 2, cy - ny * width / 2,
                                             cx + nx * width / 2, cy + ny * width / 2, medPos);
            if (!sub) p = new Cv.Point2f((float)Math.Round(p.X), (float)Math.Round(p.Y));
            ctx.LastPoint = p;
            ctx.Points.Add(p);

            // 灰度差 = 边两侧各取 1.5 像素的真实灰度跳变（不是梯度幅值；梯度会随采样步长缩水）
            var duo = VisionCaliper.SampleProfile(gray, p.X - nx * 1.5, p.Y - ny * 1.5,
                                                   p.X + nx * 1.5, p.Y + ny * 1.5, 2);
            double grayJump = Math.Abs(duo[1] - duo[0]);

            dst.Line((int)(cx - nx * width / 2), (int)(cy - ny * width / 2),
                     (int)(cx + nx * width / 2), (int)(cy + ny * width / 2), Ink, 1);
            DrawPt(dst, p);
            Add(report, s, spec, true,
                $"边缘点 ({p.X:F2}, {p.Y:F2})｜灰度差 {grayJump:F1}｜{pos.Count}/{profiles} 条剖面找到",
                grayJump, pos.Count,
                ("X", p.X), ("Y", p.Y), ("灰度差", grayJump), ("边缘强度", medStr),
                ("在剖面上的位置", medPos), ("找到几条", pos.Count));
        }

        /// <summary>找椭圆：阈值 → 外轮廓 → 逐块拟合，按面积取前几个（SGamma 的拟合（椭）圆）。</summary>
        static void DoEllipse(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var roi = RoiOf(spec, s, gray);
            using var sub = new Cv.Mat(gray, roi);
            using var bin = new Cv.Mat();
            double thr = Num(spec, s, "thr", 128);
            if (Txt(spec, s, "which") == "暗块") Cv.Cv2.Threshold(sub, bin, thr, 255, Cv.ThresholdTypes.BinaryInv);
            else Cv.Cv2.Threshold(sub, bin, thr, 255, Cv.ThresholdTypes.Binary);
            Cv.Cv2.FindContours(bin, out Cv.Point[][] contours, out _,
                Cv.RetrievalModes.External, Cv.ContourApproximationModes.ApproxSimple);

            double minArea = Math.Max(1, Num(spec, s, "minArea", 500));
            int maxHits = Math.Max(1, (int)Math.Round(Num(spec, s, "maxHits", 1)));
            var found = new List<(Cv.RotatedRect E, double Area)>();
            foreach (var cc in contours)
            {
                if (cc.Length < 5) continue;
                double area = Cv.Cv2.ContourArea(cc);
                if (area < minArea) continue;
                var shifted = Array.ConvertAll(cc, q => new Cv.Point(q.X + roi.Left, q.Y + roi.Top));
                found.Add((Cv.Cv2.FitEllipse(shifted), area));
            }
            if (found.Count == 0)
            {
                Add(report, s, spec, false, $"框里没有面积超过 {minArea:F0} 的块：调小「太小不算」，或者把「分界灰度」调到能把目标分开");
                return;
            }
            found.Sort((x, y) => y.Area.CompareTo(x.Area));
            var best = found[0].E;
            foreach (var f in found.Take(maxHits)) Cv.Cv2.Ellipse(dst, f.E, Ink, 2);
            DrawPt(dst, best.Center);
            double major = Math.Max(best.Size.Width, best.Size.Height);
            double minor = Math.Min(best.Size.Width, best.Size.Height);
            double rot = best.Angle;
            if (best.Size.Width < best.Size.Height) rot += 90;   // 让「长轴角度」说得通
            rot = ((rot % 180) + 180) % 180;                     // 统一到 0~180，别出现 -90 / 210 这种
            ctx.LastPoint = best.Center;
            Add(report, s, spec, true,
                $"中心 ({best.Center.X:F1}, {best.Center.Y:F1})｜长轴 {major:F1}｜短轴 {minor:F1}｜转角 {rot:F1}°｜共 {found.Count} 个",
                major, found.Count,
                ("中心X", best.Center.X), ("中心Y", best.Center.Y),
                ("长轴", major), ("短轴", minor), ("角度", rot), ("个数", found.Count));
        }

        /// <summary>把上游边缘点拟成椭圆（先整体拟一次，把最离谱的踢掉再拟一次）。</summary>
        static void DoFitEllipseFromPoints(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            if (ctx.Points.Count < 5)
            {
                Add(report, s, spec, false, "手上的点不够（拟椭圆至少 5 个）：先跑一步「找一个边缘点」/「找直边上的点」攒点");
                return;
            }
            double keep = Math.Min(1, Math.Max(0.5, Num(spec, s, "keep", 0.9)));
            var pts = ctx.Points.ToList();
            var e = Cv.Cv2.FitEllipse(pts);
            int keepN = Math.Max(5, (int)Math.Round(pts.Count * keep));
            if (keepN < pts.Count)
            {
                var ranked = pts.OrderBy(q => EllipseResidualPx(e, q)).ToList();
                pts = ranked.Take(keepN).ToList();
                e = Cv.Cv2.FitEllipse(pts);
            }
            double rms = Math.Sqrt(pts.Average(q => Sq(EllipseResidualPx(e, q))));
            double major = Math.Max(e.Size.Width, e.Size.Height);
            double minor = Math.Min(e.Size.Width, e.Size.Height);
            double rot = e.Angle;
            if (e.Size.Width < e.Size.Height) rot += 90;
            rot = ((rot % 180) + 180) % 180;
            ctx.LastPoint = e.Center;
            Cv.Cv2.Ellipse(dst, e, Ink, 2);
            DrawPt(dst, e.Center);
            Add(report, s, spec, true,
                $"中心 ({e.Center.X:F1}, {e.Center.Y:F1})｜长轴 {major:F1}｜短轴 {minor:F1}｜转角 {rot:F1}°｜偏差 {rms:F2}｜用了 {pts.Count} 个点",
                major, pts.Count,
                ("中心X", e.Center.X), ("中心Y", e.Center.Y),
                ("长轴", major), ("短轴", minor), ("角度", rot), ("偏差", rms), ("点数", pts.Count));
        }

        /// <summary>点到椭圆边界的近似像素距离：把偏移按半轴归一化，再乘回短半轴。</summary>
        static double EllipseResidualPx(Cv.RotatedRect e, Cv.Point2f p)
        {
            double a = Math.Max(1e-6, e.Size.Width / 2), b = Math.Max(1e-6, e.Size.Height / 2);
            double rad = -e.Angle * Math.PI / 180.0;
            double dx = p.X - e.Center.X, dy = p.Y - e.Center.Y;
            double u = dx * Math.Cos(rad) - dy * Math.Sin(rad);
            double v = dx * Math.Sin(rad) + dy * Math.Cos(rad);
            double r = Math.Sqrt((u / a) * (u / a) + (v / b) * (v / b));
            return Math.Abs(r - 1) * Math.Min(a, b);
        }

        // ===================== 批次 2：几何计算 =====================

        /// <summary>取一个点：手填，或用上一步找到的圆心 / 角点 / 边缘点。</summary>
        static Cv.Point2f PickPoint(VisionOpSpec spec, VisualFlowStep s, VisionOpContext ctx, string xKey, string yKey)
        {
            string use = Txt(spec, s, "src");
            if (use.Length == 0) use = Txt(spec, s, "use");
            if (use.StartsWith("上一步"))
            {
                if (ctx.LastPoint != null) return ctx.LastPoint.Value;
                if (ctx.Corners.Count > 0) return ctx.Corners[0];
                if (ctx.LastCircle != null) return new Cv.Point2f((float)ctx.LastCircle.Cx, (float)ctx.LastCircle.Cy);
                if (ctx.Points.Count > 0) return ctx.Points[0];
            }
            return new Cv.Point2f((float)Num(spec, s, xKey, 0), (float)Num(spec, s, yKey, 0));
        }

        /// <summary>取一条线：上一步找到的，或手填两端点。</summary>
        static VisionCaliper.LineResult? PickLine(VisionOpSpec spec, VisualFlowStep s, VisionOpContext ctx,
            bool preferLast = true)
        {
            string use = Txt(spec, s, "use");
            if (use != "手填" && ctx.LastLine != null && ctx.LastLine.Ok) return ctx.LastLine;
            double x1 = Num(spec, s, "x1", 0), y1 = Num(spec, s, "y1", 0), x2 = Num(spec, s, "x2", 0), y2 = Num(spec, s, "y2", 0);
            if (x1 == 0 && y1 == 0 && x2 == 0 && y2 == 0) return preferLast ? ctx.LastLine : null;
            return FitLineFromTwo(x1, y1, x2, y2);
        }

        static VisionCaliper.LineResult FitLineFromTwo(double x1, double y1, double x2, double y2)
        {
            var pts = new List<Cv.Point2f> { new Cv.Point2f((float)x1, (float)y1), new Cv.Point2f((float)x2, (float)y2) };
            var r = VisionCaliper.FitLine(pts);
            r.X1 = x1; r.Y1 = y1; r.X2 = x2; r.Y2 = y2;
            r.AngleDeg = Math.Atan2(y2 - y1, x2 - x1) * 180 / Math.PI;
            while (r.AngleDeg > 90) r.AngleDeg -= 180;
            while (r.AngleDeg < -90) r.AngleDeg += 180;
            r.Ok = true;
            return r;
        }

        static void NeedTwo(out VisionCaliper.LineResult? a, out VisionCaliper.LineResult? b, VisionOpContext ctx,
            VisionReport report, VisualFlowStep s, VisionOpSpec spec)
        {
            a = b = null;
            if (ctx.Lines.Count < 2)
            { Add(report, s, spec, false, "手上不足两条线：先跑两步「找直线」或「找多条直线」"); return; }
            a = ctx.Lines[ctx.Lines.Count - 2];
            b = ctx.Lines[ctx.Lines.Count - 1];
        }

        static void DoPointPoint(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst, VisionReport report,
            VisionOpContext ctx, IProgress<string>? progress)
        {
            Cv.Point2f a, b;
            string use = Txt(spec, s, "use");
            if (use == "上一步的角点" && ctx.Corners.Count >= 2) { a = ctx.Corners[0]; b = ctx.Corners[1]; }
            else if (use == "上一步的边缘点" && ctx.Points.Count >= 2) { a = ctx.Points[0]; b = ctx.Points[ctx.Points.Count - 1]; }
            else
            {
                a = new Cv.Point2f((float)Num(spec, s, "ax", 0), (float)Num(spec, s, "ay", 0));
                b = new Cv.Point2f((float)Num(spec, s, "bx", 0), (float)Num(spec, s, "by", 0));
            }
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            double ang = Math.Atan2(dy, dx) * 180 / Math.PI;
            // 参考方向：SGamma 的「与参考线同向 / 垂直」——量的是投影长度，不是直线距离
            string refdir = Txt(spec, s, "refdir");
            double proj = dist;
            if (refdir == "沿参考线" || refdir == "垂直参考线")
            {
                double ra = Num(spec, s, "refang", 0) * Math.PI / 180.0;
                proj = refdir == "沿参考线"
                    ? Math.Abs(dx * Math.Cos(ra) + dy * Math.Sin(ra))
                    : Math.Abs(-dx * Math.Sin(ra) + dy * Math.Cos(ra));
            }
            ctx.LastLength = refdir == "无参考线" ? dist : proj;
            DrawPt(dst, a); DrawPt(dst, b);
            dst.Line((int)a.X, (int)a.Y, (int)b.X, (int)b.Y, Ink, 1);
            Add(report, s, spec, true,
                refdir == "无参考线"
                    ? $"距离 {dist:F2} 像素｜X差 {dx:F1}｜Y差 {dy:F1}｜角度 {ang:F2}°"
                    : $"距离 {dist:F2}｜{refdir}的投影 {proj:F2} 像素", dist, 2,
                ("距离", dist), ("投影距离", proj), ("X方向差", dx), ("Y方向差", dy), ("角度", ang));
        }

        static void DoPointLine(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst, VisionReport report,
            VisionOpContext ctx, IProgress<string>? progress)
        {
            var line = PickLine(spec, s, ctx);
            if (line == null || !line.Ok) { Add(report, s, spec, false, "没有可用的线：先跑一步「找直线」，或把「线从哪来」改成手填"); return; }
            var p = new Cv.Point2f((float)Num(spec, s, "px", 0), (float)Num(spec, s, "py", 0));
            if (Txt(spec, s, "px") == "0" && Txt(spec, s, "py") == "0" && ctx.LastPoint != null) p = ctx.LastPoint.Value;

            double vx = line.X2 - line.X1, vy = line.Y2 - line.Y1;
            double len2 = vx * vx + vy * vy;
            double t = len2 < 1e-9 ? 0 : ((p.X - line.X1) * vx + (p.Y - line.Y1) * vy) / len2;
            double fx = line.X1 + vx * t, fy = line.Y1 + vy * t;
            double d = Math.Sqrt(Sq(p.X - fx) + Sq(p.Y - fy));
            ctx.LastLength = d;
            dst.Line((int)p.X, (int)p.Y, (int)fx, (int)fy, InkWarn, 1);
            DrawPt(dst, p); DrawPt(dst, new Cv.Point2f((float)fx, (float)fy));
            Add(report, s, spec, true,
                $"垂直距离 {d:F2} 像素｜垂足 ({fx:F1},{fy:F1})｜在线上的位置 {t * 100:F0}%", d, 1,
                ("垂直距离", d), ("垂足X", fx), ("垂足Y", fy), ("在线上的位置", t));
        }

        static void DoPointCircle(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst, VisionReport report,
            VisionOpContext ctx, IProgress<string>? progress)
        {
            VisionCaliper.CircleResult? c = null;
            if (Txt(spec, s, "use") != "手填") c = ctx.LastCircle;
            c ??= new VisionCaliper.CircleResult
            {
                Ok = true, Cx = Num(spec, s, "cx", 0), Cy = Num(spec, s, "cy", 0), R = Num(spec, s, "radius", 0)
            };
            if (!c.Ok || c.R <= 0) { Add(report, s, spec, false, "没有可用的圆：先跑一步「找圆」，或把「圆从哪来」改成手填"); return; }
            var p = new Cv.Point2f((float)Num(spec, s, "px", 0), (float)Num(spec, s, "py", 0));
            if (p.X == 0 && p.Y == 0 && ctx.LastPoint != null) p = ctx.LastPoint.Value;
            double dc = Math.Sqrt(Sq(p.X - c.Cx) + Sq(p.Y - c.Cy));
            double edge = dc - c.R;
            ctx.LastLength = dc;
            dst.Line((int)c.Cx, (int)c.Cy, (int)p.X, (int)p.Y, Ink, 1);
            DrawPt(dst, p);
            Add(report, s, spec, true,
                $"到圆心 {dc:F2}｜到圆边 {edge:F2}（{(edge >= 0 ? "在圆外" : "在圆内")}）", edge, 1,
                ("到圆心距离", dc), ("到圆边距离", edge), ("在圆内还是圆外", edge >= 0 ? 1 : 0));
        }

        static void DoLineLine(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst, VisionReport report,
            VisionOpContext ctx, IProgress<string>? progress)
        {
            NeedTwo(out var a, out var b, ctx, report, s, spec);
            if (a == null || b == null) return;
            double ang = Math.Abs(a.AngleDeg - b.AngleDeg);
            if (ang > 90) ang = 180 - ang;
            double tol = Num(spec, s, "tol", 0.5);
            if (ang < tol)
            {
                Add(report, s, spec, true, $"两条线基本平行（夹角 {ang:F2}°）→ 改看「两条平行线间距」", ang, 0,
                    ("夹角", ang), ("是否平行", 1));
                return;
            }
            // 用两点式求交：a: p+tv, b: q+su
            double x1 = a.X1, y1 = a.Y1, x2 = a.X2, y2 = a.Y2, x3 = b.X1, y3 = b.Y1, x4 = b.X2, y4 = b.Y2;
            double den = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (Math.Abs(den) < 1e-9) { Add(report, s, spec, false, "两条线算不出交点（可能重合或太接近平行）"); return; }
            double ix = ((x1 * y2 - y1 * x2) * (x3 - x4) - (x1 - x2) * (x3 * y4 - y3 * x4)) / den;
            double iy = ((x1 * y2 - y1 * x2) * (y3 - y4) - (y1 - y2) * (x3 * y4 - y3 * x4)) / den;
            ctx.LastPoint = new Cv.Point2f((float)ix, (float)iy);
            DrawPt(dst, ctx.LastPoint.Value);
            Add(report, s, spec, true,
                $"夹角 {ang:F2}°｜交点 ({ix:F1},{iy:F1})", ang, 1,
                ("夹角", ang), ("交点X", ix), ("交点Y", iy), ("是否平行", 0));
        }

        static void DoLineCircle(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst, VisionReport report,
            VisionOpContext ctx, IProgress<string>? progress)
        {
            var l = ctx.LastLine; var c = ctx.LastCircle;
            if (l == null || !l.Ok || c == null || !c.Ok)
            { Add(report, s, spec, false, "需要先各有一条线和一个圆（跑过「找直线」和「找圆」）"); return; }
            double vx = l.X2 - l.X1, vy = l.Y2 - l.Y1;
            double n = Math.Sqrt(vx * vx + vy * vy);
            if (n < 1e-9) { Add(report, s, spec, false, "那条线两端重合，量不了"); return; }
            double d = Math.Abs((c.Cx - l.X1) * vy - (c.Cy - l.Y1) * vx) / n;
            double diff = d - c.R;
            string how = Math.Abs(diff) < 1 ? "正好相切" : diff < 0 ? "穿过圆（有 2 个交点）" : "没碰到圆";
            double ix = c.Cx, iy = c.Cy;
            if (d >= 1e-9)
            {
                double t = ((c.Cx - l.X1) * vx + (c.Cy - l.Y1) * vy) / (n * n);
                ix = l.X1 + vx * t; iy = l.Y1 + vy * t;      // 垂足 = 圆心在线上的投影
            }
            ctx.LastLength = d; ctx.LastPoint = new Cv.Point2f((float)ix, (float)iy);
            DrawPt(dst, ctx.LastPoint.Value);
            Add(report, s, spec, true,
                $"圆心到线 {d:F2} 像素｜比半径{'多' + diff:F2}｜{how}", d, 1,
                ("圆心到线距离", d), ("与半径差", diff), ("相交情况", how == "正好相切" ? 0 : diff < 0 ? -1 : 1),
                ("交点X", ix), ("交点Y", iy));
        }

        static void DoCircleCircle(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst, VisionReport report,
            VisionOpContext ctx, IProgress<string>? progress)
        {
            if (ctx.Circles.Count < 2) { Add(report, s, spec, false, "手上不足两个圆：先跑两步「找圆」"); return; }
            var a = ctx.Circles[ctx.Circles.Count - 2];
            var b = ctx.Circles[ctx.Circles.Count - 1];
            double dx = b.Cx - a.Cx, dy = b.Cy - a.Cy;
            double d = Math.Sqrt(dx * dx + dy * dy);
            double gap = d - a.R - b.R;                     // 负数 = 重叠
            double ang = Math.Atan2(dy, dx) * 180 / Math.PI;
            ctx.LastLength = d;
            dst.Line((int)a.Cx, (int)a.Cy, (int)b.Cx, (int)b.Cy, Ink, 1);
            string howFar = gap < 0 ? $"重叠 {-gap:F2}" : $"还差 {gap:F2}";
            Add(report, s, spec, true,
                $"圆心距 {d:F2}｜间隙 {gap:F2}（{howFar}）｜连线角度 {ang:F2}°",
                gap, 2, ("圆心距", d), ("间隙", gap), ("半径差", a.R - b.R), ("连线角度", ang));
        }

        static void DoLineGap(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst, VisionReport report,
            VisionOpContext ctx, IProgress<string>? progress)
        {
            NeedTwo(out var a, out var b, ctx, report, s, spec);
            if (a == null || b == null) return;
            double where = Num(spec, s, "where", 0);
            double mx = where > 0 ? where : (a.X1 + a.X2) / 2;
            double av = a.AngleDeg * Math.PI / 180, bv = b.AngleDeg * Math.PI / 180;
            // 在 x = mx 处分别取两条线上的点，再量它们之间的垂直距离（取法向，不按水平投影虚高）
            double pa = LineYAt(a, mx), pb = LineYAt(b, mx);
            double dy = Math.Abs(pa - pb);
            double cos = Math.Cos((av + bv) / 2);
            double gap = Math.Abs(cos) < 0.2 ? dy : dy * Math.Abs(cos);   // 线接近竖直时按 x 方向量
            if (Math.Abs(Math.Cos(av)) < 0.2 && Math.Abs(Math.Cos(bv)) < 0.2)
            {
                double qa = LineXAt(a, (a.Y1 + a.Y2) / 2), qb = LineXAt(b, (a.Y1 + a.Y2) / 2);
                gap = Math.Abs(qa - qb);
            }
            ctx.LastLength = gap;
            Add(report, s, spec, true,
                $"间距 {gap:F2} 像素｜两条线角度 {a.AngleDeg:F2}° / {b.AngleDeg:F2}°", gap, 2,
                ("间距", gap), ("第一条角度", a.AngleDeg), ("第二条角度", b.AngleDeg), ("角度差", Math.Abs(a.AngleDeg - b.AngleDeg)));
        }

        static double LineYAt(VisionCaliper.LineResult l, double x)
        {
            double dx = l.X2 - l.X1;
            if (Math.Abs(dx) < 1e-6) return (l.Y1 + l.Y2) / 2;
            return l.Y1 + (x - l.X1) * (l.Y2 - l.Y1) / dx;
        }

        static double LineXAt(VisionCaliper.LineResult l, double y)
        {
            double dy = l.Y2 - l.Y1;
            if (Math.Abs(dy) < 1e-6) return (l.X1 + l.X2) / 2;
            return l.X1 + (y - l.Y1) * (l.X2 - l.X1) / dy;
        }

        static void DoPixelStat(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            var roi = RoiOf(spec, s, gray);
            using var sub = new Cv.Mat(gray, roi);
            Cv.Cv2.MeanStdDev(sub, out Cv.Scalar mean, out Cv.Scalar sd);
            Cv.Cv2.MinMaxLoc(sub, out double minV, out double maxV, out Cv.Point minL, out Cv.Point maxL, null);
            double hi = Num(spec, s, "hi", 200), lo = Num(spec, s, "lo", 50);
            using var bright = new Cv.Mat(); using var dark = new Cv.Mat();
            Cv.Cv2.Threshold(sub, bright, hi, 255, Cv.ThresholdTypes.Binary);
            Cv.Cv2.Threshold(sub, dark, lo, 255, Cv.ThresholdTypes.BinaryInv);
            double total = Math.Max(1, sub.Rows * sub.Cols);
            double hiRatio = Cv.Cv2.CountNonZero(bright) / total, loRatio = Cv.Cv2.CountNonZero(dark) / total;
            dst.Rectangle(roi, Ink, 1);
            Add(report, s, spec, true,
                $"平均 {mean.Val0:F1}｜最亮 {maxV:F0}｜最暗 {minV:F0}｜起伏 {sd.Val0:F1}｜亮点 {hiRatio * 100:F1}%｜暗点 {loRatio * 100:F1}%",
                mean.Val0, sub.Rows * sub.Cols,
                ("平均灰度", mean.Val0), ("最亮", maxV), ("最暗", minV), ("起伏(标准差)", sd.Val0),
                ("亮点占比", hiRatio * 100), ("暗点占比", loRatio * 100));
        }

        static void DoHistogram(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            var roi = RoiOf(spec, s, gray);
            using var sub = new Cv.Mat(gray, roi);
            using var hist = new Cv.Mat();
            var h = new[] { sub };
            Cv.Cv2.CalcHist(h, new int[] { 0 }, null, hist, 1, new int[] { 256 }, new Cv.Rangef[] { new Cv.Rangef(0, 256) });
            hist.GetArray(out float[] bins);
            int peak = 0; float peakV = -1;
            for (int i = 0; i < bins.Length; i++) if (bins[i] > peakV) { peakV = bins[i]; peak = i; }
            // Otsu 直接让 OpenCV 算（把阈值当返回值用：maxThreshold 会给出分界）
            // 分界值自己按 Otsu 算：Cv2.Threshold 的返回值在部分 OpenCvSharp 版本上拿不到（实测返回 0），
            // 而这里只需要那个数本身，不需要真的二值化一张图。
            double otsu = OtsuFromHist(bins, (double)sub.Rows * sub.Cols);
            double total = sub.Rows * sub.Cols, acc = 0; double median = 0;
            for (int i = 0; i < 256; i++) { acc += bins[i]; if (acc >= total / 2) { median = i; break; } }
            double minOn = 0, maxOn = 255;
            for (int i = 0; i < 256; i++) if (bins[i] > 0) { minOn = i; break; }
            for (int i = 255; i >= 0; i--) if (bins[i] > 0) { maxOn = i; break; }
            dst.Rectangle(roi, Ink, 1);
            Add(report, s, spec, true,
                $"建议分界 {otsu:F0}｜最多的一档 {peak}｜中位数 {median:F0}｜明暗跨度 {minOn:F0}~{maxOn:F0}",
                otsu, 256, ("建议分界值", otsu), ("最亮档", maxOn), ("最暗档", minOn), ("中位数", median),
                ("对比度", maxOn - minOn));
        }

        // ===================== 批次 2：标定与换算 =====================

        static void DoMeasureScale(VisionOpSpec spec, VisualFlowStep s, VisionReport report,
            VisionOpContext ctx, IProgress<string>? progress)
        {
            double actual = Num(spec, s, "actual", 0);
            double pixels = Num(spec, s, "pixels", 0);
            if (pixels <= 0) pixels = ctx.LastLength;
            if (Txt(spec, s, "method") == "圆直径" && ctx.LastCircle != null) pixels = ctx.LastCircle.R * 2;
            if (actual <= 0 || pixels <= 0 || double.IsNaN(pixels))
            { Add(report, s, spec, false, "两个数都要有：标件实际多长（毫米）+ 图上量到多长（像素，或先跑一步量出长度）"); return; }
            double pxPerMm = pixels / actual;
            ctx.ScalePxPerMm = pxPerMm;
            Add(report, s, spec, true,
                $"1 毫米 = {pxPerMm:F3} 像素｜1 像素 = {1 / pxPerMm:F5} 毫米", pxPerMm, 0,
                ("每毫米多少像素", pxPerMm), ("每像素多少毫米", 1 / pxPerMm));
        }

        static void DoTwoPointCalib(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst, VisionReport report,
            VisionOpContext ctx, IProgress<string>? progress)
        {
            double p1x = Num(spec, s, "p1x", 0), p1y = Num(spec, s, "p1y", 0);
            double m1x = Num(spec, s, "m1x", 0), m1y = Num(spec, s, "m1y", 0);
            double p2x = Num(spec, s, "p2x", 0), p2y = Num(spec, s, "p2y", 0);
            double m2x = Num(spec, s, "m2x", 0), m2y = Num(spec, s, "m2y", 0);
            double pd = Math.Sqrt(Sq(p2x - p1x) + Sq(p2y - p1y));
            double md = Math.Sqrt(Sq(m2x - m1x) + Sq(m2y - m1y));
            if (pd < 2 || md < 1e-6)
            { Add(report, s, spec, false, "两个点要隔开一点：图上两点至少相距 2 像素，机台坐标也要有实际差值"); return; }
            double pxPerMm = pd / md;
            double angPx = Math.Atan2(p2y - p1y, p2x - p1x) * 180 / Math.PI;
            double angMm = Math.Atan2(m2y - m1y, m2x - m1x) * 180 / Math.PI;
            double rot = angMm - angPx;
            // 零点：机台 (0,0) 对应图上哪一点 = p1 - R(rot)^-1 · (m1 · scale)
            double rad = -rot * Math.PI / 180;
            double ox = p1x - (m1x * pxPerMm * Math.Cos(rad) - m1y * pxPerMm * Math.Sin(rad));
            double oy = p1y - (m1x * pxPerMm * Math.Sin(rad) + m1y * pxPerMm * Math.Cos(rad));
            ctx.ScalePxPerMm = pxPerMm; ctx.CalibAngleDeg = rot; ctx.CalibOriginX = ox; ctx.CalibOriginY = oy;
            DrawPt(dst, new Cv.Point2f((float)p1x, (float)p1y));
            DrawPt(dst, new Cv.Point2f((float)p2x, (float)p2y));
            Add(report, s, spec, true,
                $"1 毫米 = {pxPerMm:F3} 像素｜图要再转 {rot:F2}° 才是机台方向｜零点在图上 ({ox:F1},{oy:F1})", pxPerMm, 2,
                ("每毫米多少像素", pxPerMm), ("转角", rot), ("零点X", ox), ("零点Y", oy));
        }

        /// <summary>取换算用的 6 个系数：本流程标定算子给的优先，其次才是「标定」页里这台相机的。</summary>
        static double[]? ResolveAffine(VisionOpSpec spec, VisualFlowStep s, VisionOpContext ctx)
        {
            if (ctx.HasAffine) return ctx.Affine;
            var rec = Calibration.CalibrationStore.FindUsable(Txt(spec, s, "cam"));
            if (rec != null && rec.A1 != 0 && rec.B2 != 0)
            {
                ctx.Affine = new[] { rec.A1, rec.A2, rec.A3, rec.B1, rec.B2, rec.B3 };
                ctx.MmPerPx = rec.PixelEquivalent;
                ctx.CalibCam = rec.CameraName;
                return ctx.Affine;
            }
            return null;
        }

        /// <summary>图上像素推成机台毫米；没有可用换算关系返回 false。</summary>
        static bool TryPixelToMachine(double[]? a, double u, double v, out double x, out double y)
        {
            x = y = double.NaN;
            if (a == null) return false;
            x = a[0] * u + a[1] * v + a[2];
            y = a[3] * u + a[4] * v + a[5];
            return true;
        }

        /// <summary>反过来：机台毫米推回图上像素（系数退化返回 false）。</summary>
        static bool TryMachineToPixel(double[]? a, double x, double y, out double u, out double v)
        {
            u = v = double.NaN;
            if (a == null) return false;
            double det = a[0] * a[4] - a[1] * a[3];
            if (Math.Abs(det) < 1e-12) return false;
            double mx = x - a[2], my = y - a[5];
            u = (mx * a[4] - my * a[1]) / det;
            v = (a[0] * my - mx * a[3]) / det;
            return true;
        }

        /// <summary>毫米/像素（图上横竖两个方向平均）；没有换算关系返回 NaN。</summary>
        static double MmPerPxOf(double[]? a)
        {
            if (a == null) return double.NaN;
            double u = Math.Sqrt(Sq(a[0]) + Sq(a[3])), v = Math.Sqrt(Sq(a[1]) + Sq(a[4]));
            if (u < 1e-12 || v < 1e-12) return double.NaN;
            return 0.5 * (u + v);
        }

        /// <summary>按「比例 + 转角 + 零点」这套老约定推出 6 个系数（和「手动对两个点」同一套约定）。</summary>
        static double[] AffineFrom(double scalePxPerMm, double rotDeg, double ox, double oy)
        {
            double r = rotDeg * Math.PI / 180, cs = Math.Cos(r), sn = Math.Sin(r);
            return new[]
            {
                cs / scalePxPerMm, -sn / scalePxPerMm, -(cs * ox - sn * oy) / scalePxPerMm,
                sn / scalePxPerMm, cs / scalePxPerMm, -(sn * ox + cs * oy) / scalePxPerMm,
            };
        }

        /// <summary>标定结果写回「标定」页那张表（只在填了相机名时写）。</summary>
        static void SaveCalib(VisionOpContext ctx, string cam, string how)
        {
            var a = ctx.Affine;
            if (a == null || a.Length != 6 || cam.Length == 0) return;
            Calibration.CalibrationStore.Upsert(new Models.CameraCalibration
            {
                CameraName = cam,
                CalibratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                IsValid = true,
                Message = how,
                PixelEquivalent = MmPerPxOf(a),
                PixelEquivalentV = Math.Sqrt(Sq(a[1]) + Sq(a[4])),
                AngleX = Math.Atan2(a[3], a[0]) * 180 / Math.PI,
                AngleY = Math.Atan2(a[4], a[1]) * 180 / Math.PI,
                A1 = a[0], A2 = a[1], A3 = a[2], B1 = a[3], B2 = a[4], B3 = a[5],
                ResidualRms = ctx.CalibRms,
                RotCenterX = ctx.RotCenterX, RotCenterY = ctx.RotCenterY,
                RotCenterU = ctx.RotCenterU, RotCenterV = ctx.RotCenterV,
                RotRadiusPx = ctx.RotRadiusPx, RotResidualRms = ctx.RotRmsPx,
            });
            ctx.CalibCam = cam;
        }

        static (double scale, double rot, double ox, double oy, string from) ResolveCalib(
            VisionOpSpec spec, VisualFlowStep s, VisionOpContext ctx)
        {
            if (ctx.HasCalib) return (ctx.ScalePxPerMm, double.IsNaN(ctx.CalibAngleDeg) ? 0 : ctx.CalibAngleDeg,
                                      double.IsNaN(ctx.CalibOriginX) ? 0 : ctx.CalibOriginX,
                                      double.IsNaN(ctx.CalibOriginY) ? 0 : ctx.CalibOriginY, "本流程上一步定的标定");
            string cam = Txt(spec, s, "cam");
            if (cam.Length > 0)
            {
                var rec = Calibration.CalibrationStore.FindUsable(cam);
                // 「标定」表里存的是毫米/像素，这套老参数要的是像素/毫米，必须取倒数。
                // 以前直接把毫米/像素当像素/毫米用，标定越准反而偏得越离谱（2026-11-05 修）。
                if (rec != null && rec.PixelEquivalent > 1e-12)
                    return (1 / rec.PixelEquivalent, rec.AngleX, 0, 0, "「标定」表里 " + cam + " 的结果");
            }
            return (double.NaN, 0, 0, 0, "");
        }

        static void DoPixelToMachine(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst, VisionReport report,
            VisionOpContext ctx, IProgress<string>? progress)
        {
            var affIn = ResolveAffine(spec, s, ctx);
            if (affIn != null)
            {
                var pm = PickPoint(spec, s, ctx, "px", "py");
                if (TryPixelToMachine(affIn, pm.X, pm.Y, out double gx, out double gy))
                {
                    double mmpp = MmPerPxOf(affIn);
                    ctx.LastPoint = pm;
                    DrawPt(dst, pm);
                    string by = ctx.CalibCam.Length > 0 ? "「标定」表里 " + ctx.CalibCam + " 的换算关系" : "上一条标定算子给的换算关系";
                    Add(report, s, spec, true,
                        $"图上 ({pm.X:F1},{pm.Y:F1}) → 机台 ({gx:F3},{gy:F3}) 毫米｜用的是{by}", gx, 0,
                        ("机台X", gx), ("机台Y", gy), ("每像素多少毫米", mmpp),
                        ("每毫米多少像素", mmpp > 1e-12 ? 1 / mmpp : double.NaN));
                    return;
                }
            }
            var (scale, rot, ox, oy, from) = ResolveCalib(spec, s, ctx);
            if (double.IsNaN(scale) || scale <= 0)
            { Add(report, s, spec, false, "还没有可用的标定：先跑「多点标定」「标定板定比例」或「手动对两个点」，或者去「标定」页做一台相机的标定"); return; }
            var p = PickPoint(spec, s, ctx, "px", "py");
            double rad = rot * Math.PI / 180;
            double dx = (p.X - ox) / scale, dy = (p.Y - oy) / scale;
            double mx = dx * Math.Cos(rad) - dy * Math.Sin(rad);
            double my = dx * Math.Sin(rad) + dy * Math.Cos(rad);
            ctx.LastPoint = p;
            DrawPt(dst, p);
            Add(report, s, spec, true,
                $"图上 ({p.X:F1},{p.Y:F1}) → 机台 ({mx:F3},{my:F3}) mm｜用的是{from}", mx, 0,
                ("机台X", mx), ("机台Y", my), ("每毫米多少像素", scale), ("转角", rot));
        }

        static void DoMachineToPixel(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst, VisionReport report,
            VisionOpContext ctx, IProgress<string>? progress)
        {
            double mx = Num(spec, s, "mx", 0), my = Num(spec, s, "my", 0);
            var affOut = ResolveAffine(spec, s, ctx);
            if (affOut != null && TryMachineToPixel(affOut, mx, my, out double au, out double av))
            {
                DrawPt(dst, new Cv.Point2f((float)au, (float)av));
                Add(report, s, spec, true, $"机台 ({mx:F3},{my:F3}) 毫米 → 图上 ({au:F1},{av:F1})", au, 0,
                    ("图上X", au), ("图上Y", av));
                return;
            }
            var (scale, rot, ox, oy, from) = ResolveCalib(spec, s, ctx);
            if (double.IsNaN(scale) || scale <= 0)
            { Add(report, s, spec, false, "还没有可用的标定：先跑「多点标定」「标定板定比例」或「手动对两个点」"); return; }
            double rad = -rot * Math.PI / 180;
            double a = mx * Math.Cos(rad) - my * Math.Sin(rad);
            double b = mx * Math.Sin(rad) + my * Math.Cos(rad);
            double px = ox + a * scale, py = oy + b * scale;
            DrawPt(dst, new Cv.Point2f((float)px, (float)py));
            Add(report, s, spec, true, $"机台 ({mx:F3},{my:F3}) mm → 图上 ({px:F1},{py:F1})", px, 0,
                ("图上X", px), ("图上Y", py));
        }

        static void DoScalarConvert(VisionOpSpec spec, VisualFlowStep s, VisionReport report,
            VisionOpContext ctx, IProgress<string>? progress)
        {
            double raw = Num(spec, s, "raw", 0);
            if (raw == 0 && !double.IsNaN(ctx.LastLength)) raw = ctx.LastLength;
            double scale = Num(spec, s, "scale", 1), offset = Num(spec, s, "offset", 0);
            double v = raw * scale + offset;
            string unit = Txt(spec, s, "unit");
            Add(report, s, spec, true, $"{raw:0.###} × {scale:0.####} + {offset:0.####} = {v:0.###} {unit}", v, 0,
                ("换算后的值", v), ("比例", scale), ("偏移", offset));
        }

        // ===================== 批次 5：对位与标定的小工具 =====================

        /// <summary>把一张「每行 per 个数」的表读成数组。逗号、等号、空格、分号、换行都能当分隔符。</summary>
        static List<double[]> ParseNumbers(string raw, int per)
        {
            var list = new List<double[]>();
            if (string.IsNullOrWhiteSpace(raw)) return list;
            var norm = raw.Replace('，', ',').Replace('＝', '=').Replace('　', ' ')
                          .Replace('；', ';').Replace('、', ';').Replace('=', ',').Replace('\r', '\n');
            foreach (var seg in norm.Split(new[] { ';', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = seg.Trim().Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != per) continue;
                var v = new double[per];
                bool okAll = true;
                for (int i = 0; i < per; i++)
                {
                    if (!double.TryParse(parts[i], NumberStyles.Any, CultureInfo.InvariantCulture, out v[i]))
                    { okAll = false; break; }
                }
                if (okAll) list.Add(v);
            }
            return list;
        }

        /// <summary>
        /// 点配对表 → (图上u, 图上v, 机台X, 机台Y)。一行一对，两种写法都行：
        ///   「图上X,图上Y=机台X,机台Y」或「图上X,图上Y,机台X,机台Y」；多行用分号或换行隔开。
        /// </summary>
        static List<(double U, double V, double X, double Y)> ParsePairs(string raw)
            => ParseNumbers(raw, 4).Select(q => (q[0], q[1], q[2], q[3])).ToList();

        /// <summary>一行三个数的表（机台位置或角度 + 图上X + 图上Y）。</summary>
        static List<double[]> ParseTriples(string raw) => ParseNumbers(raw, 3);

        /// <summary>解析一对数：「X,Y」「X=Y」「X，Y」都认。</summary>
        static bool ParseXY(string raw, out double x, out double y)
        {
            x = y = double.NaN;
            var v = ParseNumbers(raw, 2);
            if (v.Count == 0) return false;
            x = v[0][0]; y = v[0][1];
            return true;
        }

        /// <summary>参数留空或填 0 都算「没填」，用来实现「0 = 用上一条算子给的数」。</summary>
        static double Num0(VisionOpSpec spec, VisualFlowStep s, string key)
        {
            string raw = s.GetOpParam(key, spec.Find(key)?.Default ?? "0");
            return double.TryParse((raw ?? "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double v) ? v : 0;
        }

        static double LenOf(double dx, double dy) => Math.Sqrt(Sq(dx) + Sq(dy));

        /// <summary>画一个点，再画一条从它到「该在的位置」的短连线（一眼看出还差多少没对上位）。</summary>
        static void DrawPair(Cv.Mat dst, double u, double v, double gx, double gy)
        {
            DrawPt(dst, new Cv.Point2f((float)u, (float)v));
            if (double.IsNaN(gx) || double.IsNaN(gy)) return;
            dst.Line((int)Math.Round(u), (int)Math.Round(v), (int)Math.Round(gx), (int)Math.Round(gy), InkWarn, 1);
            dst.Circle((int)Math.Round(gx), (int)Math.Round(gy), 3, InkWarn, 1);
        }

        /// <summary>OpenCV 的去畸变只吃 1 或 3 通道：BGRA 先转 BGR，出来再转回去。</summary>
        static Cv.Mat ToThree(Cv.Mat src, out bool wasFour)
        {
            wasFour = src.Channels() == 4;
            var o = new Cv.Mat();
            if (src.Channels() == 1 || src.Channels() == 3) src.CopyTo(o);
            else Cv.Cv2.CvtColor(src, o, Cv.ColorConversionCodes.BGRA2BGR);
            return o;
        }

        static Cv.Mat FromThree(Cv.Mat bgr, bool backToFour)
        {
            var o = new Cv.Mat();
            if (backToFour) Cv.Cv2.CvtColor(bgr, o, Cv.ColorConversionCodes.BGR2BGRA);
            else bgr.CopyTo(o);
            return o;
        }

        // ===================== 批次 5：标定 =====================

        /// <summary>多点标定（九点标定）：好几对「图上的点 = 机台的点」一起定出完整换算关系。</summary>
        static void DoMultiCalib(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst, VisionReport report,
                                 VisionOpContext ctx, IProgress<string>? progress)
        {
            var pairs = ParsePairs(Txt(spec, s, "pairs"));
            bool full = Txt(spec, s, "mode") != "只算平移和转动";
            int need = full ? 3 : 2;
            if (pairs.Count < need)
            {
                Add(report, s, spec, false,
                    $"点配对至少要 {need} 对才够算，现在只认出 {pairs.Count} 对。一对的写法：图上X,图上Y=机台X,机台Y，"
                    + "多对用分号或换行隔开（每行一对最好认）。");
                return;
            }
            double[] a;
            double rms;
            if (full)
            {
                var fit = Calibration.CalibSolver.FitAffine(pairs);
                if (!fit.Ok) { Add(report, s, spec, false, "算不出换算关系：" + fit.Message); return; }
                a = new[] { fit.A1, fit.A2, fit.A3, fit.B1, fit.B2, fit.B3 };
                rms = fit.RmsMm;
            }
            else
            {
                var img = pairs.Select(q => new Cv.Point2f((float)q.U, (float)q.V)).ToList();
                var mc = pairs.Select(q => new Cv.Point2f((float)q.X, (float)q.Y)).ToList();
                var all = Enumerable.Range(0, pairs.Count).ToList();
                FitLeastSquares(all, img, mc, out double tx, out double ty, out double cs, out double sn, out double sc);
                if (sc < 1e-9)
                { Add(report, s, spec, false, "这些点全挤在一块（或重复填了同一对），算不出比例和转角：横竖都要铺开。"); return; }
                a = new[] { sc * cs, -sc * sn, tx, sc * sn, sc * cs, ty };
                double se0 = 0;
                foreach (var q in pairs) se0 += Sq(LenOf(a[0] * q.U + a[1] * q.V + a[2] - q.X,
                                                         a[3] * q.U + a[4] * q.V + a[5] - q.Y));
                rms = Math.Sqrt(se0 / (2.0 * pairs.Count));
            }
            double mmpp = MmPerPxOf(a);
            if (double.IsNaN(mmpp) || mmpp <= 0)
            { Add(report, s, spec, false, "这些点定不出有意义的比例：别让所有点都排在一条直线上。"); return; }

            double tol = Num(spec, s, "tol", 0.05);
            ctx.Affine = a; ctx.MmPerPx = mmpp; ctx.CalibRms = rms;
            ctx.ScalePxPerMm = 1 / mmpp;
            ctx.CalibAngleDeg = Math.Atan2(a[3], a[0]) * 180 / Math.PI;
            ctx.CalibOriginX = 0; ctx.CalibOriginY = 0;
            foreach (var q in pairs)
            {
                TryPixelToMachine(a, q.U, q.V, out double gx, out double gy);
                DrawPair(dst, q.U, q.V, gx, gy);
            }
            string cam = Txt(spec, s, "cam");
            SaveCalib(ctx, cam, "多点标定：" + pairs.Count + " 对，" + (full ? "全算（含倾斜）" : "只算平移和转动"));
            Add(report, s, spec, rms <= tol,
                $"{pairs.Count} 对点算好了｜1 毫米 = {1 / mmpp:F3} 像素（1 像素 = {mmpp:F6} 毫米）｜图上要再转 {ctx.CalibAngleDeg:F2}° 才是机台方向"
                + $"｜每点平均差 {Calibration.CalibSolver.Fmt(rms)} 毫米"
                + (rms > tol ? "（比「误差上限」大，先别拿去对位：查机台是否走到位、点有没有配错）" : "")
                + (cam.Length > 0 ? "｜已存进「标定」表的 " + cam : "｜只在当前这条流程里生效"),
                mmpp, pairs.Count,
                ("每像素多少毫米", mmpp), ("每毫米多少像素", 1 / mmpp), ("转角", ctx.CalibAngleDeg),
                ("倾斜", Math.Atan2(a[4], a[1]) * 180 / Math.PI),
                ("标定误差多少毫米", rms), ("用了几对点", pairs.Count), ("零点X", a[2]), ("零点Y", a[5]));
            progress?.Report(spec.Name + $"：{pairs.Count} 对点，1 毫米 = {1 / mmpp:F3} 像素");
        }

        /// <summary>单轴标定：只走一根轴，看同一个标记在图上怎么挪，求比例 + 相机和这根轴的夹角。</summary>
        static void DoOneAxisCalib(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst, VisionReport report,
                                   VisionOpContext ctx, IProgress<string>? progress)
        {
            var rows = ParseTriples(Txt(spec, s, "trio"));
            if (rows.Count < 2)
            { Add(report, s, spec, false, "至少要 2 条（机台位置,图上X,图上Y），现在只认出 " + rows.Count + " 条。轴多走几趟更准。"); return; }
            if (rows.Max(r => r[0]) - rows.Min(r => r[0]) < 1e-6)
            { Add(report, s, spec, false, "这几条的机台位置全都一样（是不是都填 0 了？）：轴要真的挪开，每趟记的位置才不一样。"); return; }

            foreach (var r in rows) DrawPt(dst, new Cv.Point2f((float)r[1], (float)r[2]));

            // 图上位置对机台位置做线性回归：斜率向量 = 「机台走 1 毫米，图上挪多少像素、朝哪个方向」
            int n = rows.Count;
            double sm = 0, su = 0, sv = 0, smm = 0, smu = 0, smv = 0;
            for (int i = 0; i < n; i++)
            {
                double m = rows[i][0], u = rows[i][1], v = rows[i][2];
                sm += m; su += u; sv += v; smm += m * m; smu += m * u; smv += m * v;
            }
            double den = n * smm - sm * sm;
            if (Math.Abs(den) < 1e-12)
            { Add(report, s, spec, false, "机台位置太集中，算不出斜率：把两趟的间距拉大一点。"); return; }
            double du = (n * smu - sm * su) / den, dv = (n * smv - sm * sv) / den;
            double scale = LenOf(du, dv);                                      // 像素/毫米
            if (scale < 1e-9)
            { Add(report, s, spec, false, "机台挪了、图上标记一点没动：相机八成跟着轴一起走，那种情况要走「多点标定」。"); return; }
            double ox = su / n - du * (sm / n), oy = sv / n - dv * (sm / n);   // 这根轴读数为 0 时，标记在图上的位置

            double theta = Math.Atan2(dv, du) * 180 / Math.PI;                 // 机台这根轴的正方向在图上指向哪
            bool yAxis = Txt(spec, s, "axis") == "Y";
            double rot = yAxis ? 90 - theta : -theta;                          // 另一根轴按垂直补齐
            double se = 0;
            foreach (var r in rows) se += Sq(LenOf(du * r[0] + ox - r[1], dv * r[0] + oy - r[2]));
            double rmsMm = Math.Sqrt(se / n) / scale;

            double[] a = AffineFrom(scale, rot, ox, oy);
            double tol = Num(spec, s, "tol", 0.05);
            ctx.Affine = a; ctx.ScalePxPerMm = scale; ctx.CalibAngleDeg = rot;
            ctx.CalibOriginX = ox; ctx.CalibOriginY = oy;
            ctx.MmPerPx = 1 / scale; ctx.CalibRms = rmsMm;
            string cam = Txt(spec, s, "cam");
            SaveCalib(ctx, cam, "单轴标定：走 " + (yAxis ? "Y" : "X") + " 轴 " + n + " 趟，另一根按垂直补齐");
            Add(report, s, spec, rmsMm <= tol,
                $"1 毫米 = {scale:F3} 像素（1 像素 = {1 / scale:F6} 毫米）｜机台 {(yAxis ? "Y" : "X")} 轴在图上指向 {theta:F2}°"
                + $"，另一根按垂直补齐（转角 {rot:F2}°）｜每点平均差 {Calibration.CalibSolver.Fmt(rmsMm)} 毫米"
                + (rmsMm > tol ? "（超过「误差上限」，先别拿去对位）" : "")
                + (cam.Length > 0 ? "｜已存进「标定」表的 " + cam : ""),
                scale, n,
                ("每毫米多少像素", scale), ("每像素多少毫米", 1 / scale),
                ("图上方向角", theta), ("补齐后的转角", rot),
                ("标定误差多少毫米", rmsMm), ("用了几点", n), ("零点X", ox), ("零点Y", oy));
            progress?.Report(spec.Name + $"：1 毫米 = {scale:F3} 像素，夹角 {theta:F2}°");
        }

        /// <summary>旋转中心标定：转几个角度记下同一个标记的图上位置，拟出圆心（对位要绕它转）。</summary>
        static void DoRotateCenter(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst, VisionReport report,
                                   VisionOpContext ctx, IProgress<string>? progress)
        {
            var rows = ParseTriples(Txt(spec, s, "trio"));
            if (rows.Count < 3)
            { Add(report, s, spec, false, "至少 3 个角度（转过的角度,图上X,图上Y），现在只认出 " + rows.Count + " 条。常用 5 个。"); return; }
            if (rows.Max(r => r[0]) - rows.Min(r => r[0]) < 1)
            { Add(report, s, spec, false, "转过的角度全都差不多，拟不出圆：至少转开 1 度，最好 30~90 度。"); return; }

            var pts = rows.Select(r => (U: r[1], V: r[2])).ToList();
            var fit = Calibration.CalibSolver.FitCircle(pts);
            if (!fit.Ok) { Add(report, s, spec, false, "拟圆失败：" + fit.Message); return; }
            double minr = Num(spec, s, "minr", 5), tol = Num(spec, s, "tol", 3);
            foreach (var q in pts) DrawPt(dst, new Cv.Point2f((float)q.U, (float)q.V));
            dst.Circle((int)Math.Round(fit.CenterU), (int)Math.Round(fit.CenterV),
                       Math.Max(1, (int)Math.Round(fit.RadiusPx)), Ink, 1);
            DrawPt(dst, new Cv.Point2f((float)fit.CenterU, (float)fit.CenterV));

            ctx.RotCenterU = fit.CenterU; ctx.RotCenterV = fit.CenterV;
            ctx.RotRadiusPx = fit.RadiusPx; ctx.RotRmsPx = fit.RmsPx;
            bool thin = fit.RadiusPx < minr;
            bool mapped = TryPixelToMachine(ResolveAffine(spec, s, ctx), fit.CenterU, fit.CenterV,
                                            out double mx, out double my);
            if (mapped) { ctx.RotCenterX = mx; ctx.RotCenterY = my; }
            string cam = Txt(spec, s, "cam");
            if (cam.Length > 0 && ctx.HasAffine)
                SaveCalib(ctx, cam, "找旋转中心：" + rows.Count + " 个角度，半径 " + Calibration.CalibSolver.Fmt(fit.RadiusPx) + " 像素");
            Add(report, s, spec, fit.RmsPx <= tol && !thin,
                $"旋转中心在图上 ({Calibration.CalibSolver.Fmt(fit.CenterU)},{Calibration.CalibSolver.Fmt(fit.CenterV)})"
                + $"｜标记转的半径 {Calibration.CalibSolver.Fmt(fit.RadiusPx)} 像素｜每点平均差 {Calibration.CalibSolver.Fmt(fit.RmsPx)} 像素"
                + (thin ? $"（半径不到「转半径至少」{minr:0.##} 像素，这个中心不可信：把标记挪离转轴远一点）" : "")
                + (fit.RmsPx > tol ? "（误差比「误差上限」大：检查每趟拍到的是不是同一个标记）" : "")
                + (mapped ? $"｜换算到机台 ({mx:F3},{my:F3}) 毫米" : "｜还没定标定，只给图上位置（先跑「多点标定」就能同时给机台位置）"),
                fit.CenterU, rows.Count,
                ("旋转中心X", fit.CenterU), ("旋转中心Y", fit.CenterV), ("转半径", fit.RadiusPx),
                ("标定误差多少像素", fit.RmsPx), ("用了几点", rows.Count),
                ("机台旋转中心X", mx), ("机台旋转中心Y", my));
            progress?.Report(spec.Name + $"：中心 ({Calibration.CalibSolver.Fmt(fit.CenterU)},{Calibration.CalibSolver.Fmt(fit.CenterV)})");
        }

        /// <summary>找标定板上的特征点（框里有几块找几块，取找全的那块）。失败给人话原因。</summary>
        static Cv.Point2f[]? BoardPoints(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray,
                                         out int rows, out int cols, out string bad)
        {
            rows = cols = 0; bad = "";
            bool circles = Txt(spec, s, "board") == "圆点";
            rows = (int)Math.Round(Num(spec, s, "rows", 7));
            cols = (int)Math.Round(Num(spec, s, "cols", 7));
            if (rows < 2 || cols < 2) { bad = "行数、列数至少各填 2（填的是点数，不是格子面积）"; return null; }
            var roi = RoiOf(spec, s, gray);
            if (roi.Width < cols * 4 || roi.Height < rows * 4)
            { bad = $"框住的那块太小，装不下 {cols}×{rows} 个板点：把框画大一点，或者少填几行几列。"; return null; }
            using var sub = gray.SubMat(roi).Clone();
            Cv.Point2f[]? best = null;
            for (int att = 0; att < 4 && best == null; att++)
            {
                Cv.Point2f[] pts;
                bool ok;
                try
                {
                    ok = circles
                        ? Cv.Cv2.FindCirclesGrid(sub, new Cv.Size(cols, rows), out pts, Cv.FindCirclesGridFlags.SymmetricGrid)
                        : Cv.Cv2.FindChessboardCorners(sub, new Cv.Size(cols, rows), out pts,
                            Cv.ChessboardFlags.AdaptiveThresh | Cv.ChessboardFlags.NormalizeImage);
                }
                catch (Exception ex) { bad = "OpenCV 找板子时报错：" + ex.Message; return null; }
                if (ok && pts != null && pts.Length == rows * cols)
                {
                    for (int i = 0; i < pts.Length; i++)
                        pts[i] = new Cv.Point2f(pts[i].X + roi.X, pts[i].Y + roi.Y);
                    best = pts;
                    break;
                }
                // 第一遍没找全：糊的抹一下、反差差的拉一下，再试；还不成就报人话原因。
                if (att == 0) Cv.Cv2.GaussianBlur(sub, sub, new Cv.Size(5, 5), 0);
                else if (att == 1) Cv.Cv2.Normalize(sub, sub, 0, 255, Cv.NormTypes.MinMax, -1);
                else if (att == 2 && !circles)
                    Cv.Cv2.AdaptiveThreshold(sub, sub, 255, Cv.AdaptiveThresholdTypes.GaussianC, Cv.ThresholdTypes.Binary, 31, 5);
            }
            if (best == null)
            {
                bad = $"板子没找全（要 {(circles ? "圆点" : "棋盘格")} {cols}×{rows} = {cols * rows} 个点）。"
                      + "常见原因：只拍到半块、光太强反光、图糊、板子歪太多。"
                      + "行数列数填交叉点 / 圆点的个数：7×7 的板就填 7 和 7。";
                return null;
            }
            return best;
        }

        /// <summary>板点 → 换算关系。先全算（含倾斜），残差太大（透视严重）退回「比例 + 转角」。</summary>
        static (double[]? aff, double mmpp, double rms, double angle) BoardFit(
            Cv.Point2f[] pts, int rows, int cols, double cellMm)
        {
            int ix(int r, int c) => r * cols + c;
            double angle = Math.Atan2(pts[ix(0, cols - 1)].Y - pts[ix(0, 0)].Y,
                                      pts[ix(0, cols - 1)].X - pts[ix(0, 0)].X) * 180 / Math.PI;
            var list = new List<(double U, double V, double X, double Y)>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    list.Add((pts[ix(r, c)].X, pts[ix(r, c)].Y, c * cellMm, r * cellMm));

            var fit = Calibration.CalibSolver.FitAffine(list);
            if (fit.Ok && fit.RmsMm <= cellMm * 0.35)
                return (new[] { fit.A1, fit.A2, fit.A3, fit.B1, fit.B2, fit.B3 }, fit.PixelEquivalentU, fit.RmsMm, angle);

            // 退路：板点当相似变换拟（只有比例 + 转角 + 零点），再把 px = f(mm) 反解成 mm = f(px)
            var ideal = list.Select(q => new Cv.Point2f((float)q.X, (float)q.Y)).ToList();
            var seen = list.Select(q => new Cv.Point2f((float)q.U, (float)q.V)).ToList();
            var all = Enumerable.Range(0, list.Count).ToList();
            FitLeastSquares(all, ideal, seen, out double tx, out double ty, out double cs, out double sn, out double sc);
            if (sc < 1e-9) return (null, double.NaN, double.NaN, angle);
            double mmpp = 1 / sc, se = 0;
            foreach (var q in list)
                se += Sq(LenOf(sc * (q.X * cs - q.Y * sn) + tx - q.U, sc * (q.X * sn + q.Y * cs) + ty - q.V));
            double rms = Math.Sqrt(se / (2.0 * list.Count)) * mmpp;
            double angR = Math.Atan2(sn, cs), c2 = Math.Cos(angR), s2 = Math.Sin(angR), inv = 1 / sc;
            return (new[] { c2 * inv, s2 * inv, -(c2 * tx + s2 * ty) * inv,
                            -s2 * inv, c2 * inv, (s2 * tx - c2 * ty) * inv }, mmpp, rms, angR * 180 / Math.PI);
        }

        /// <summary>标定板定比例：拍一张板子自己找点，直接定出换算关系，不用手填任何坐标。</summary>
        static void DoBoardScale(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
                                 VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var found = BoardPoints(spec, s, gray, out int rows, out int cols, out string bad);
            if (found == null) { Add(report, s, spec, false, bad); return; }
            double cell = Num(spec, s, "cell", 10);
            if (cell <= 0) { Add(report, s, spec, false, "「一格多宽」要填正数（毫米）"); return; }
            var (a, mmpp, rms, angle) = BoardFit(found, rows, cols, cell);
            if (a == null)
            { Add(report, s, spec, false, "这些板点定不出比例：板子在图上几乎缩成一个点了，靠近一点重拍。"); return; }
            foreach (var q in found) DrawPt(dst, q);
            double tol = Num(spec, s, "tol", 0.1);
            ctx.Affine = a; ctx.MmPerPx = mmpp; ctx.CalibRms = rms;
            ctx.ScalePxPerMm = 1 / mmpp; ctx.CalibAngleDeg = Math.Atan2(a[3], a[0]) * 180 / Math.PI;
            ctx.CalibOriginX = 0; ctx.CalibOriginY = 0;
            string board = Txt(spec, s, "board") == "圆点" ? "圆点" : "棋盘格";
            string cam = Txt(spec, s, "cam");
            SaveCalib(ctx, cam, "标定板：" + board + " " + cols + "×" + rows + "，格距 " + cell + " 毫米");
            Add(report, s, spec, rms <= tol,
                $"{board} {cols}×{rows} 点全找到｜1 毫米 = {1 / mmpp:F3} 像素（1 像素 = {mmpp:F6} 毫米）"
                + $"｜板子在图上转了 {angle:F2}°｜每点平均差 {Calibration.CalibSolver.Fmt(rms)} 毫米"
                + (rms > tol ? "（误差比上限大：板子拍歪得多或有透视，正对着拍更准）" : "")
                + (cam.Length > 0 ? "｜已存进「标定」表的 " + cam : ""),
                mmpp, found.Length,
                ("每像素多少毫米", mmpp), ("每毫米多少像素", 1 / mmpp),
                ("找到几个点", found.Length), ("板子转了多少度", angle), ("标定误差多少毫米", rms));
            progress?.Report(spec.Name + $"：{found.Length} 个点，1 毫米 = {1 / mmpp:F3} 像素");
        }

        /// <summary>镜头畸变标定：用标定板算镜头把直线拍弯了多少，顺手可以把这张图掰直。</summary>
        static Cv.Mat DoLensCalib(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
                                  VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var found = BoardPoints(spec, s, gray, out int rows, out int cols, out string bad);
            if (found == null) { Add(report, s, spec, false, bad); return cur; }
            double cell = Num(spec, s, "cell", 10);
            if (cell <= 0) { Add(report, s, spec, false, "「一格多宽」要填正数（毫米）"); return cur; }

            var obj = new Cv.Point3f[rows * cols];
            int at = 0;
            for (int br = 0; br < rows; br++)
                for (int bc = 0; bc < cols; bc++) obj[at++] = new Cv.Point3f(bc * (float)cell, br * (float)cell, 0);
            var img = found.Select(q => new Cv.Point2f(q.X, q.Y)).ToArray();

            // 起点：画面正中当主点、长边当焦距；UseIntrinsicGuess 让 OpenCV 从这儿往下解
            double f0 = Math.Max(gray.Cols, gray.Rows);
            bool radialOnly = Txt(spec, s, "mode") != "径向+切向";
            var flags = Cv.CalibrationFlags.UseIntrinsicGuess
                        | (radialOnly ? Cv.CalibrationFlags.ZeroTangentDist : Cv.CalibrationFlags.None);
            using var objMat = Cv.Mat.FromArray(obj);
            using var imgMat = Cv.Mat.FromArray(img);
            using var km = KMat(new double[9] { f0, 0, gray.Cols / 2.0, 0, f0, gray.Rows / 2.0, 0, 0, 1 });
            using var dm = new Cv.Mat(1, 5, Cv.MatType.CV_64F);      // k1 k2 p1 p2 k3，从「没畸变」起步
            dm.SetArray(new double[5]);
            double reproj;
            try
            {
                reproj = Cv.Cv2.CalibrateCamera(new[] { objMat }, new[] { imgMat },
                    new Cv.Size(gray.Cols, gray.Rows), km, dm, out _, out _, flags, null);
            }
            catch (Exception ex)
            {
                Add(report, s, spec, false, "OpenCV 的相机标定没跑起来：" + ex.Message
                      + "（只拍一张板子时畸变本来就难算准：板子摆正、拍满整个画面再试；"
                      + "厂商已经给了畸变数的话，用「按参数把图掰直」更稳）");
                return cur;
            }
            double fx = km.At<double>(0, 0), fy = km.At<double>(1, 1), px = km.At<double>(0, 2), py = km.At<double>(1, 2);
            double dk1 = El(dm, 0), dk2 = El(dm, 1), dp1 = El(dm, 2), dp2 = El(dm, 3), dk3 = El(dm, 4);
            double mmpp = MmPerPxOf(new[] { fx, 0, px, 0, fy, py });
            if (double.IsNaN(mmpp)) mmpp = 1 / Math.Max(1e-9, fx);

            foreach (var q in found) DrawPt(dst, q);
            Cv.Mat next = cur;                        // 换图只交新图，cur 由引擎释放
            bool fix = Num(spec, s, "fix", 1) >= 1;
            if (fix)
            {
                var done = UndistortTo(cur, km, dm);
                if (done != null) next = done;
            }
            Add(report, s, spec, true,
                $"畸变算好了：k1={Calibration.CalibSolver.Fmt(dk1)} k2={Calibration.CalibSolver.Fmt(dk2)} k3={Calibration.CalibSolver.Fmt(dk3)}"
                + (radialOnly ? "（只算径向，切向按 0）" : $"｜切向 p1={Calibration.CalibSolver.Fmt(dp1)} p2={Calibration.CalibSolver.Fmt(dp2)}")
                + $"｜画面中心 ({px:F1},{py:F1})｜重投影误差 {reproj:F3} 像素（约 {Calibration.CalibSolver.Fmt(reproj * mmpp)} 毫米）"
                + (fix ? "｜这张图已掰直，后面的算子用的是掰直后的图" : "｜只给参数，图没动"),
                dk1, found.Length,
                ("畸变k1", dk1), ("畸变k2", dk2), ("畸变k3", dk3),
                ("切向p1", dp1), ("切向p2", dp2), ("重投影误差", reproj),
                ("中心点X", px), ("中心点Y", py), ("焦距", fx), ("每像素多少毫米", mmpp));
            progress?.Report(spec.Name + $"：k1={Calibration.CalibSolver.Fmt(dk1)}，误差 {reproj:F3} 像素");
            return next;
        }

        /// <summary>畸变数组（一行）里取第 i 个；OpenCV 可能只解 4 个（k1 k2 p1 p2），后面按 0 算。</summary>
        static double El(Cv.Mat m, int i) => i >= 0 && m.Cols > i ? m.At<double>(0, i) : 0;

        /// <summary>把 9 个数摊成 3×3 相机矩阵。尺寸必须自己定：Reshape 出来的头依赖临时 Mat，出了作用域就读到 0。</summary>
        static Cv.Mat KMat(double[] k)
        {
            var m = new Cv.Mat(3, 3, Cv.MatType.CV_64F);
            m.SetArray(k);
            return m;
        }

        /// <summary>按 K / D 掰直当前图（保持通道数）；失败返回 null。</summary>
        static Cv.Mat? UndistortTo(Cv.Mat src, Cv.Mat K, Cv.Mat D)
        {
            try
            {
                bool four = src.Channels() == 4;
                using var three = ToThree(src, out _);
                using var out3 = new Cv.Mat();
                Cv.Cv2.Undistort(three, out3, K, D);
                return FromThree(out3, four);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[VisionOpRunner] 掰直失败：" + ex.Message);
                return null;
            }
        }

        /// <summary>按参数把图掰直 / 加回畸变（不用板子，参数手填或抄上一条算子给的）。</summary>
        static Cv.Mat DoLensFix(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
                                VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            double k1 = Num(spec, s, "k1", 0), k2 = Num(spec, s, "k2", 0), k3 = Num(spec, s, "k3", 0);
            double p1 = Num(spec, s, "p1", 0), p2 = Num(spec, s, "p2", 0);
            if (Math.Abs(k1) + Math.Abs(k2) + Math.Abs(k3) + Math.Abs(p1) + Math.Abs(p2) < 1e-12)
            { Add(report, s, spec, false, "五个畸变数全是 0，那等于不处理：把「镜头畸变标定」给的 k1/k2/k3/p1/p2 填进来。"); return cur; }
            bool fix = Txt(spec, s, "mode") != "还原（加畸变）";
            int w = cur.Cols, h = cur.Rows;
            double f = Num(spec, s, "focal", 0);
            if (f <= 0) f = Math.Max(w, h);
            double cx = Num(spec, s, "cx", 0), cy = Num(spec, s, "cy", 0);
            if (cx == 0) cx = (w - 1) / 2.0;
            if (cy == 0) cy = (h - 1) / 2.0;
            var K = new double[9] { f, 0, cx, 0, f, cy, 0, 0, 1 };
            var D = new[] { k1, k2, p1, p2, k3 };

            var next = new Cv.Mat();
            try
            {
                using var km = KMat(K);
                using var dm = new Cv.Mat(1, D.Length, Cv.MatType.CV_64F);
                dm.SetArray(D);
                if (fix)
                {
                    using var m1 = new Cv.Mat();
                    using var m2 = new Cv.Mat();
                    Cv.Cv2.InitUndistortRectifyMap(km, dm, new Cv.Mat(), km, new Cv.Size(w, h),
                        Cv.MatType.CV_32FC1, m1, m2);
                    Cv.Cv2.Remap(cur, next, m1, m2, Cv.InterpolationFlags.Linear,
                        Cv.BorderTypes.Constant, new Cv.Scalar(0, 0, 0, 0));
                }
                else
                {
                    // 加畸变：把每个「输出像素」当成理想位置，按 OpenCV 同一套多项式模型
                    // （径向 k1/k2/k3 + 切向 p1/p2）问它来自输入图的哪儿，逐点自己算，不绕 OpenCV
                    var src2 = new Cv.Point2f[w * h];
                    int at = 0;
                    for (int y = 0; y < h; y++)
                    {
                        double ny = (y - cy) / f;
                        for (int x = 0; x < w; x++)
                        {
                            double nx = (x - cx) / f;
                            double r2 = nx * nx + ny * ny;
                            double radial = 1 + r2 * (k1 + r2 * (k2 + r2 * k3));
                            double dx = nx * radial + 2 * p1 * nx * ny + p2 * (r2 + 2 * nx * nx);
                            double dy = ny * radial + p1 * (r2 + 2 * ny * ny) + 2 * p2 * nx * ny;
                            src2[at++] = new Cv.Point2f((float)(f * dx + cx), (float)(f * dy + cy));
                        }
                    }
                    using var map = new Cv.Mat(h, w, Cv.MatType.CV_32FC2);   // 地图按画面尺寸铺，Reshape 会算错行列
                    map.SetArray(src2);
                    using var none = new Cv.Mat();
                    Cv.Cv2.Remap(cur, next, map, none, Cv.InterpolationFlags.Linear,
                        Cv.BorderTypes.Constant, new Cv.Scalar(0, 0, 0, 0));
                }
            }
            catch (Exception ex)
            {
                next.Dispose();
                Add(report, s, spec, false, "OpenCV 生成校正图时报错：" + ex.Message + "（k1 太负会把图掰得太狠，先减一半试试）");
                return cur;
            }
            Add(report, s, spec, true,
                $"{(fix ? "已掰直（去畸变）" : "已加回畸变（和原图对齐时用）")}｜焦距 {f:F2} 像素、中心 ({cx:F1},{cy:F1})"
                + $"｜k1={Calibration.CalibSolver.Fmt(k1)} k2={Calibration.CalibSolver.Fmt(k2)} k3={Calibration.CalibSolver.Fmt(k3)} p1={Calibration.CalibSolver.Fmt(p1)} p2={Calibration.CalibSolver.Fmt(p2)}",
                k1, 0,
                ("用了k1", k1), ("用了k2", k2), ("用了k3", k3), ("用了p1", p1), ("用了p2", p2),
                ("用了焦距", f), ("中心点X", cx), ("中心点Y", cy));
            progress?.Report(spec.Name + (fix ? "：掰直完成" : "：加回畸变完成"));
            return next;
        }

        // ===================== 批次 5：对位 =====================

        /// <summary>取实测的两个标记点：上一步的两个角点 / 两个圆心 / 手填一对。</summary>
        static bool TwoMarks(VisionOpSpec spec, VisualFlowStep s, VisionOpContext ctx,
                             out Cv.Point2f a, out Cv.Point2f b, out string bad)
        {
            a = b = default; bad = "";
            string use = Txt(spec, s, "src");
            if (use.StartsWith("上一步"))
            {
                if (use.Contains("角点"))
                {
                    if (ctx.Corners.Count < 2)
                    { bad = $"上一步只找到 {ctx.Corners.Count} 个角点，凑不出一对：先跑「找矩形四角」，或把这里改成手填"; return false; }
                    a = ctx.Corners[0]; b = ctx.Corners[1];
                    return true;
                }
                if (ctx.Circles.Count >= 2)
                {
                    int n2 = ctx.Circles.Count;
                    a = new Cv.Point2f((float)ctx.Circles[n2 - 2].Cx, (float)ctx.Circles[n2 - 2].Cy);
                    b = new Cv.Point2f((float)ctx.Circles[n2 - 1].Cx, (float)ctx.Circles[n2 - 1].Cy);
                    return true;
                }
                if (ctx.Points.Count >= 2)
                { a = ctx.Points[ctx.Points.Count - 2]; b = ctx.Points[ctx.Points.Count - 1]; return true; }
                bad = "上一步没找到两个点（要有两个圆、或一把边缘点）：先跑两次「找圆」，或把这里改成手填";
                return false;
            }
            if (!ParseXY(Txt(spec, s, "m1"), out double ax, out double ay)
                || !ParseXY(Txt(spec, s, "m2"), out double bx, out double by))
            { bad = "手填的两个点读不出来：每个点写成「X,Y」这样一对数"; return false; }
            a = new Cv.Point2f((float)ax, (float)ay); b = new Cv.Point2f((float)bx, (float)by);
            return true;
        }

        /// <summary>两个点对位：实测两点 vs 标准两点 → 偏了多少毫米、转了多少度、该绕哪儿转回来。</summary>
        static void DoTwoMarkAlign(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst, VisionReport report,
                                   VisionOpContext ctx, IProgress<string>? progress)
        {
            if (!TwoMarks(spec, s, ctx, out Cv.Point2f ma, out Cv.Point2f mb, out string bad))
            { Add(report, s, spec, false, bad); return; }
            if (!ParseXY(Txt(spec, s, "b1"), out double b1x, out double b1y)
                || !ParseXY(Txt(spec, s, "b2"), out double b2x, out double b2y))
            { Add(report, s, spec, false, "标准的两个点要各写一对「X,Y」（两个点用同一个坐标系：都用图上像素，或都用机台毫米）"); return; }
            double mLen = LenOf(mb.X - ma.X, mb.Y - ma.Y);
            double bLen = LenOf(b2x - b1x, b2y - b1y);
            if (mLen < 2 || bLen < 1e-6)
            { Add(report, s, spec, false, $"两个点要隔开一点：实测两点相距 {mLen:F1}，标准两点相距 {bLen:F3}。挨太近定不出角度。"); return; }

            double rotDeg = Math.Atan2(b2y - b1y, b2x - b1x) * 180 / Math.PI
                          - Math.Atan2(mb.Y - ma.Y, mb.X - ma.X) * 180 / Math.PI;
            while (rotDeg > 180) rotDeg -= 360;
            while (rotDeg < -180) rotDeg += 360;

            // 先绕枢轴把工件转正直，再看还差多少平移：枢轴用标定出的旋转中心，没标定就用两点中点
            string basis = ctx.HasRotCenter ? "标定出的旋转中心" : "两个点的中点";
            double px = ctx.HasRotCenter ? ctx.RotCenterU : (ma.X + mb.X) / 2.0;
            double py = ctx.HasRotCenter ? ctx.RotCenterV : (ma.Y + mb.Y) / 2.0;
            double mx = (b1x + b2x) / 2.0, my = (b1y + b2y) / 2.0;
            double midX = (ma.X + mb.X) / 2.0, midY = (ma.Y + mb.Y) / 2.0;
            double rad = rotDeg * Math.PI / 180, cs = Math.Cos(rad), sn = Math.Sin(rad);
            double vx = midX - px, vy = midY - py;
            double dx = mx - (px + cs * vx - sn * vy);
            double dy = my - (py + sn * vx + cs * vy);
            double ratio = bLen / mLen;

            double mmpp = Num(spec, s, "mmpp", 0);
            bool fromCalib = false;
            if (mmpp <= 0)
            {
                double m2 = MmPerPxOf(ResolveAffine(spec, s, ctx));
                if (!double.IsNaN(m2)) { mmpp = m2; fromCalib = true; }
                else mmpp = 1;
            }
            double ddx = dx * mmpp, ddy = dy * mmpp;
            double tolX = Num(spec, s, "tolX", 0.1), tolY = Num(spec, s, "tolY", 0.1), tolR = Num(spec, s, "tolR", 0.5);
            bool ok = Math.Abs(ddx) <= tolX && Math.Abs(ddy) <= tolY && Math.Abs(rotDeg) <= tolR;

            ctx.AlignDX = ddx; ctx.AlignDY = ddy; ctx.AlignDR = rotDeg;
            dst.Line((int)Math.Round(ma.X), (int)Math.Round(ma.Y), (int)Math.Round(mb.X), (int)Math.Round(mb.Y), InkWarn, 2);
            DrawPt(dst, ma); DrawPt(dst, mb); DrawPt(dst, new Cv.Point2f((float)px, (float)py));
            string unit = fromCalib ? "毫米" : (mmpp == 1 ? "像素（还没标定，只能当像素看）" : "毫米");
            Add(report, s, spec, ok,
                $"偏了 X {ddx:F3} / Y {ddy:F3} {unit}，要再转 {rotDeg:F3}°（合成 {LenOf(ddx, ddy):F3}）｜绕{basis} ({px:F1},{py:F1}) 转回来"
                + $"｜两点长度比 {ratio:F4}" + (Math.Abs(ratio - 1) > 0.01 ? "（长度差 1% 以上：多半是标定或透视问题，不是位置偏了）" : "")
                + (ok ? "｜在容差内" : $"｜超差（容差 X {tolX:0.###} / Y {tolY:0.###} / 角度 {tolR:0.###}°）"),
                ddx, 0,
                ("偏差X", ddx), ("偏差Y", ddy), ("合成偏差", LenOf(ddx, ddy)), ("角度偏差", rotDeg),
                ("旋转中心X", px), ("旋转中心Y", py), ("两点长度比", ratio),
                ("实测1X", ma.X), ("实测1Y", ma.Y), ("实测2X", mb.X), ("实测2Y", mb.Y),
                ("是否合格", ok ? 1 : 0), ("每像素多少毫米", mmpp));
            progress?.Report(spec.Name + $"：偏差 {ddx:F3}/{ddy:F3} {unit}，转角 {rotDeg:F3}°");
        }

        /// <summary>把角度差折到 -180~180（转 350° 和转 -10° 是一回事，报 -10° 更像人说的话）。</summary>
        static double Norm180(double a)
        {
            while (a > 180) a -= 360;
            while (a < -180) a += 360;
            return a;
        }

        /// <summary>毫米/像素：参数填了就用填的，没填就用标定算出的（标定都没有只能当像素看）。</summary>
        static double MmPerPxOrDefault(VisionOpSpec spec, VisualFlowStep s, VisionOpContext ctx, out bool fromCalib)
        {
            double mmpp = Num(spec, s, "mmpp", 0);
            fromCalib = false;
            if (mmpp <= 0)
            {
                double m2 = MmPerPxOf(ResolveAffine(spec, s, ctx));
                if (!double.IsNaN(m2)) { mmpp = m2; fromCalib = true; }
                else mmpp = 1;
            }
            return mmpp;
        }

        /// <summary>照基准比：刚找到的 Mark 位置 / 角度 vs 示教记下的基准 → ΔX、ΔY、Δθ。</summary>
        static void DoTemplateAlign(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst, VisionReport report,
                                    VisionOpContext ctx, IProgress<string>? progress)
        {
            var p = PickPoint(spec, s, ctx, "px", "py");
            double ang = Num0(spec, s, "ang");
            bool angFromLast = false;
            if (ang == 0 && !double.IsNaN(ctx.LastAngle)) { ang = ctx.LastAngle; angFromLast = true; }
            double bx = Num(spec, s, "bx", 0), by = Num(spec, s, "by", 0), bang = Num(spec, s, "bang", 0);

            double mmpp = MmPerPxOrDefault(spec, s, ctx, out bool fromCalib);
            double ddx = (bx - p.X) * mmpp, ddy = (by - p.Y) * mmpp;
            double dth = Norm180(bang - ang);
            double tolX = Num(spec, s, "tolX", 0.1), tolY = Num(spec, s, "tolY", 0.1), tolR = Num(spec, s, "tolR", 0.5);
            bool ok = Math.Abs(ddx) <= tolX && Math.Abs(ddy) <= tolY && Math.Abs(dth) <= tolR;

            ctx.AlignDX = ddx; ctx.AlignDY = ddy; ctx.AlignDR = dth;
            DrawPt(dst, p);
            string unit = fromCalib ? "毫米" : (mmpp == 1 ? "像素（还没标定，只能当像素看）" : "毫米");
            Add(report, s, spec, ok,
                $"偏了 X {ddx:F3} / Y {ddy:F3} {unit}，角度差 {dth:F3}°（合成 {LenOf(ddx, ddy):F3}）"
                + (angFromLast ? "｜角度用的是上一条算子给的" : "")
                + (ok ? "｜在容差内，可以贴" : $"｜超差（容差 X {tolX:0.###} / Y {tolY:0.###} / 角度 {tolR:0.###}°）先别贴"),
                ddx, 0,
                ("偏差X", ddx), ("偏差Y", ddy), ("角度偏差", dth), ("合成偏差", LenOf(ddx, ddy)),
                ("当前X", p.X), ("当前Y", p.Y), ("当前角度", ang),
                ("基准X", bx), ("基准Y", by), ("是否合格", ok ? 1 : 0), ("每像素多少毫米", mmpp));
            progress?.Report(spec.Name + $"：ΔX {ddx:F3} / ΔY {ddy:F3} {unit}，Δθ {dth:F3}°");
        }

        /// <summary>上下两片相对对位：两层位姿先绕回转中心把转角差吃掉，剩下的就是真正要补的平移差。</summary>
        static void DoPairAlign(VisionOpSpec spec, VisualFlowStep s, VisionReport report,
                                VisionOpContext ctx, IProgress<string>? progress)
        {
            double ax = Num(spec, s, "ax", 0), ay = Num(spec, s, "ay", 0), aang = Num(spec, s, "aang", 0);
            double bx = Num(spec, s, "bx", 0), by = Num(spec, s, "by", 0), bang = Num(spec, s, "bang", 0);
            double rcx = Num(spec, s, "rcx", 0), rcy = Num(spec, s, "rcy", 0);

            bool hasRc = rcx != 0 || rcy != 0;
            double px0 = hasRc ? rcx : (ax + bx) / 2.0, py0 = hasRc ? rcy : (ay + by) / 2.0;
            double dth = Norm180(aang - bang);
            double rad = -dth * Math.PI / 180, cs = Math.Cos(rad), sn = Math.Sin(rad);
            double vx = ax - px0, vy = ay - py0;
            double ddx = (px0 + cs * vx - sn * vy) - bx;
            double ddy = (py0 + sn * vx + cs * vy) - by;

            double tolx = Num(spec, s, "tolx", 0.05), toly = Num(spec, s, "toly", 0.05), tolr = Num(spec, s, "tolr", 0.1);
            bool ok = Math.Abs(ddx) <= tolx && Math.Abs(ddy) <= toly && Math.Abs(dth) <= tolr;

            ctx.AlignDX = ddx; ctx.AlignDY = ddy; ctx.AlignDR = dth;
            Add(report, s, spec, ok,
                $"上下相对偏 X {ddx:F3} / Y {ddy:F3} 毫米，角度差 {dth:F3}°（合成 {LenOf(ddx, ddy):F3}）"
                + $"｜绕{px0:F2},{py0:F2} 转（{(hasRc ? "用的是填的回转中心" : "没填回转中心，按两片中间算：精度要求高就把它填上")}）"
                + (ok ? "｜在容差内，可以贴" : $"｜超差（容差 X {tolx:0.###} / Y {toly:0.###} / 角度 {tolr:0.###}°）先别贴"),
                ddx, 0,
                ("相对偏差X", ddx), ("相对偏差Y", ddy), ("相对角度", dth), ("合成偏差", LenOf(ddx, ddy)),
                ("绕哪转X", px0), ("绕哪转Y", py0), ("是否合格", ok ? 1 : 0));
            progress?.Report(spec.Name + $"：相对 ΔX {ddx:F3} / ΔY {ddy:F3} 毫米，Δθ {dth:F3}°");
        }

        /// <summary>按标记点换算坐标：标记该在哪 A / 实测到哪 C / 要换算的位置 B → Q = A + R(转角)·(B − C)。</summary>
        static void DoRigidMap(VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst, VisionReport report,
                               VisionOpContext ctx, IProgress<string>? progress)
        {
            var mark = PickPoint(spec, s, ctx, "cx", "cy");                  // C：标记实测到的位置
            double ax = Num(spec, s, "ax", 0), ay = Num(spec, s, "ay", 0);   // A：标记该在的位置
            double bx = Num(spec, s, "bx", 0), by = Num(spec, s, "by", 0);   // B：要换算的位置
            double rot = Num(spec, s, "rot", 0);
            double mdx = ax - mark.X, mdy = ay - mark.Y;                     // 标记偏了多少
            bool still = LenOf(mdx, mdy) < 1e-9 && Math.Abs(rot) < 1e-9;
            double rad = rot * Math.PI / 180, cs = Math.Cos(rad), sn = Math.Sin(rad);
            double vx = bx - mark.X, vy = by - mark.Y;
            double ox = ax + cs * vx - sn * vy, oy = ay + sn * vx + cs * vy;
            if (mark.X >= 0 && mark.Y >= 0 && mark.X < dst.Cols && mark.Y < dst.Rows)
                DrawPt(dst, new Cv.Point2f((float)mark.X, (float)mark.Y));    // B/Q 可能是毫米，不硬画在图上
            Add(report, s, spec, true,
                (still ? "标记没偏、也没给转角：这套关系等于原样搬过去" : $"标记偏了 ({mdx:F2},{mdy:F2})")
                + $"｜B ({bx:F2},{by:F2}) → ({ox:F2},{oy:F2})｜要补的移动量 ({ox - bx:F3},{oy - by:F3})"
                + (Math.Abs(rot) > 1e-9 ? $"｜绕标记转了 {rot:F2}°" : "｜没绕标记转（转角 0 = 只平移）"),
                ox - bx, 0,
                ("换算后X", ox), ("换算后Y", oy), ("偏移X", ox - bx), ("偏移Y", oy - by),
                ("标记偏差X", mdx), ("标记偏差Y", mdy), ("转角", rot),
                ("合成偏移", LenOf(ox - bx, oy - by)));
            progress?.Report(spec.Name + $"：B ({bx:F2},{by:F2}) 落到 ({ox:F2},{oy:F2})");
        }

        /// <summary>算对位移动量：视觉偏差 + 现在在哪 + 要去哪 + 吸嘴离相机的距离 → X/Y/R 各走多少。</summary>
        static void DoAlignOutput(VisionOpSpec spec, VisualFlowStep s, VisionReport report,
                                  VisionOpContext ctx, IProgress<string>? progress)
        {
            double rx = Num(spec, s, "rx", 0), ry = Num(spec, s, "ry", 0), rr = Num(spec, s, "rr", 0);
            double sx = Num(spec, s, "sx", 0), sy = Num(spec, s, "sy", 0), sr = Num(spec, s, "sr", 0);
            double dx = Num0(spec, s, "dx"), dy = Num0(spec, s, "dy"), dr = Num0(spec, s, "dr");
            string used = "";
            if (dx == 0 && dy == 0 && ctx.HasAlign) { dx = ctx.AlignDX; dy = ctx.AlignDY; used = "（偏差用的是上一条「两个点对位」给的数）"; }
            if (dr == 0 && ctx.HasAlign) dr = ctx.AlignDR;
            double offx = Num(spec, s, "offx", 0), offy = Num(spec, s, "offy", 0);
            double limit = Num(spec, s, "limit", 0);

            double tx2 = sx + dx + offx, ty2 = sy + dy + offy, tr2 = sr + dr;
            double mvx = tx2 - rx, mvy = ty2 - ry, mvr = tr2 - rr;
            double dist = LenOf(mvx, mvy);
            bool inRange = limit <= 0 || dist <= limit;
            Add(report, s, spec, inRange,
                $"X 走 {mvx:F3} 毫米、Y 走 {mvy:F3} 毫米、角度转到 {tr2:F3}°（现在 {rr:F3}°，要转 {mvr:F3}°）"
                + used
                + (offx != 0 || offy != 0 ? $"｜已把吸嘴离相机的 ({offx:F2},{offy:F2}) 毫米算进去" : "")
                + (limit > 0 ? (inRange ? $"｜在允许移动的 {limit:0.###} 毫米以内"
                                       : $"｜超出允许移动的 {limit:0.###} 毫米，先别动：多半是标定不对或抓错了位置") : ""),
                mvx, 0,
                ("目标X", tx2), ("目标Y", ty2), ("目标角度", tr2),
                ("移动X", mvx), ("移动Y", mvy), ("移动角度", mvr), ("移动距离", dist),
                ("在允许范围", inRange ? 1 : 0));
            progress?.Report(spec.Name + $"：X {mvx:F3} / Y {mvy:F3} 毫米，转到 {tr2:F3}°");
        }

        // ===================== 批次 3：匹配与识别 =====================

        static void DoShapeMatch(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            string path = Txt(spec, s, "tpl");
            if (path.Length == 0 || !File.Exists(path))
            { Add(report, s, spec, false, "先给一张样例图（模板）：填文件路径，或在右边结果图上框一块再抠成样例图"); return; }
            using var tpl = Cv.Cv2.ImRead(path, Cv.ImreadModes.Grayscale);
            if (tpl.Empty()) { Add(report, s, spec, false, "样例图读不出来：" + path); return; }

            double minScore = Num(spec, s, "minScore", 0.75);
            double angleRange = Num(spec, s, "angle", 360);
            int maxHits = (int)Num(spec, s, "maxHits", 10);
            bool contourOnly = Txt(spec, s, "mode") == "只看轮廓";

            using var src = new Cv.Mat();
            if (contourOnly) Cv.Cv2.Canny(gray, src, 50, 150); else gray.CopyTo(src);
            using var tsrc = new Cv.Mat();
            if (contourOnly) Cv.Cv2.Canny(tpl, tsrc, 50, 150); else tpl.CopyTo(tsrc);

            var hits = new List<(double X, double Y, double A, double S)>();
            using (var matcher = new GrayMatch.RotatedTemplateMatcher())
            {
                matcher.SetSource(src);
                matcher.SetTemplate(tsrc);
                matcher.UseContour = false;                       // 上面已按需换成边缘图，这里不再重复处理
                double step = angleRange <= 30 ? 1 : 5;
                var rs = matcher.Match(pyramidLevels: 0, angleStart: 0, angleEnd: angleRange, angleStep: step,
                    nccThreshold: minScore, maxOverlap: 0.3, topN: maxHits, denseMode: 0);
                foreach (var r in rs) hits.Add((r.LeftTopX, r.LeftTopY, r.Angle, r.Score));
            }
            if (hits.Count == 0) { Add(report, s, spec, false, $"没找到（相似度低于 {minScore:F2}）：换个样例图，或把「至少有多像」调低"); return; }

            var best = hits[0];
            foreach (var h in hits) if (h.S > best.S) best = h;
            double tw = tpl.Cols, th = tpl.Rows;
            double cx = best.X + tw / 2.0, cy = best.Y + th / 2.0;
            foreach (var h in hits)
                dst.Rectangle(new Cv.Rect((int)h.X, (int)h.Y, (int)tw, (int)th), Ink, 1);
            ctx.LastPoint = new Cv.Point2f((float)cx, (float)cy);
            ctx.LastAngle = best.A;
            Add(report, s, spec, true,
                $"找到 {hits.Count} 处｜最像的一处 ({cx:F1},{cy:F1}) 角度 {best.A:F1}° 相似度 {best.S:F3}", best.S, hits.Count,
                ("位置X", cx), ("位置Y", cy), ("角度", best.A), ("相似度", best.S), ("找到几个", hits.Count));
        }

                static void DoFeatureAlign(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            string path = Txt(spec, s, "tpl");
            if (path.Length == 0 || !File.Exists(path)) { Add(report, s, spec, false, "先给一张样例图（模板）的完整路径"); return; }
            using var tpl = Cv.Cv2.ImRead(path, Cv.ImreadModes.Grayscale);
            if (tpl.Empty()) { Add(report, s, spec, false, "样例图读不出来：" + path); return; }

            int minPairs = (int)Num(spec, s, "minPairs", 6);
            int nfeat = Math.Max(50, (int)Num(spec, s, "maxFeats", 600));

            // 用 ORB 描述子匹配（不是光流）：光流要求两张图一样大，抠出来的小模板会直接报错。
            // 两侧必须用同一套参数，且只能取 OpenCV 默认档位（patch=31、金字塔 8 层）：
            // 之前把 patch 写成 400 像素，小模板里没有一个放得下的，两侧描述子驴唇不对马嘴（最近距离 87，一个都对不上）。
            (int fast, string Label)[] tries = { (20, "常规"), (8, "更敏感") };
            List<Cv.Point2f>? bestA = null;
            List<Cv.Point2f>? bestB = null;
            int pairs = 0, nearest = 9999, nA = 0, nB = 0;
            string used = "";

            foreach (var (fast, label) in tries)
            {
                using var orb = Cv.ORB.Create(nfeat, 1.2f, 8, 31, 0, 2, Cv.ORBScoreType.Harris, 31, fast);
                using var dA = new Cv.Mat();
                using var dB = new Cv.Mat();
                // DetectAndCompute 的描述子参数是 OutputArray（不是 out Mat），必须先把 Mat 建好传进去
                orb.DetectAndCompute(tpl, null, out Cv.KeyPoint[] kpA, dA);
                orb.DetectAndCompute(gray, null, out Cv.KeyPoint[] kpB, dB);
                int fA = kpA == null ? 0 : kpA.Length, fB = kpB == null ? 0 : kpB.Length;
                nA = dA.Rows; nB = dB.Rows;
                if (fA == 0 || dA.Empty())
                { Add(report, s, spec, false, "样例图上提不出特征点：换一块有棱角 / 有图案的区域当样例"); return; }
                if (fB == 0 || dB.Empty())
                { Add(report, s, spec, false, $"当前图上提不出特征点（样例上有 {fA} 个）：画面太干净或太糊"); return; }

                var a = new List<Cv.Point2f>();
                var b = new List<Cv.Point2f>();
                int near = MatchByHamming(dA, dB, kpA, kpB, a, b);
                used = label;
                if (a.Count > pairs) { pairs = a.Count; nearest = near; bestA = a; bestB = b; }
                if (pairs >= minPairs) break;
            }

            if (pairs < minPairs || bestA == null || bestB == null)
            {
                // 报出「最近距离」现场才能判断是差太多（距离大）还是图案重复导致的歧义（距离小但被比值检验丢掉）
                Add(report, s, spec, false,
                    $"只有 {pairs} 个点对上（要求至少 {minPairs} 个，已试过{used}）：样例 {nA} 个特征 / 当前图 {nB} 个，"
                    + $"最近距离 {nearest}。距离很大 = 两张图确实不像；距离很小 = 图里有很多重复图案，比值检验把它们都判成歧义了");
                return;
            }
            SolveSimilar(bestA, bestB, spec, s, dst, report, ctx);
        }

                /// <summary>
        /// 由「样例点 → 当前图点」的点对解平移 / 转角 / 缩放：先 RANSAC 挑出能互相解释的那一组，再用它最小二乘精拟。
        /// 直接拿全部点对做最小二乘会被少数配错的点对拉歪（实测纹理图算出转角 131°、缩放 0.66）。
        /// </summary>
        static void SolveSimilar(List<Cv.Point2f> a, List<Cv.Point2f> b,
            VisionOpSpec spec, VisualFlowStep s, Cv.Mat dst, VisionReport report, VisionOpContext ctx)
        {
            int minPairs = (int)Num(spec, s, "minPairs", 6);
            double tol = Num(spec, s, "tol", 3);
            int n = a.Count;
            var best = new List<int>();
            var cand = new List<int>();
            int hyp = 0, maxHyp = n <= 150 ? int.MaxValue : 4000;   // 点对不多就全枚举，结果可复现

            for (int i = 0; i < n && hyp < maxHyp; i++)
            {
                for (int j = i + 1; j < n && hyp < maxHyp; j++)
                {
                    double ux = a[j].X - a[i].X, uy = a[j].Y - a[i].Y;
                    if (Math.Sqrt(ux * ux + uy * uy) < 8) continue;                 // 两点太近定不准方向
                    if (!SimilarFrom2(a[i], a[j], b[i], b[j], out double tx, out double ty,
                            out double c, out double sn, out double sc)) continue;
                    cand.Clear();
                    for (int k = 0; k < n; k++)
                        if (Reproj(a[k], b[k], tx, ty, c, sn, sc) <= tol) cand.Add(k);
                    if (cand.Count > best.Count) best = new List<int>(cand);
                    hyp++;
                }
            }

            if (best.Count < 3)
            {
                Add(report, s, spec, false,
                    $"{n} 个配对里没有一组能互相解释（容差 {tol:F1} 像素）：图上有重复图案配错了，把样例选得更独特一些，或调大容差");
                return;
            }
            if (best.Count < minPairs)
            {
                Add(report, s, spec, false,
                    $"只有 {best.Count} 个点站得住（要求至少 {minPairs} 个，容差 {tol:F1} 像素）：换一块更大更有棱角的样例，或调大容差");
                return;
            }

            FitLeastSquares(best, a, b, out double fx, out double fy, out double fc, out double fsn, out double fsc);
            if (fsc <= 0) { Add(report, s, spec, false, "对上的点全挤在一处，解不出变换"); return; }
            double rot = Math.Atan2(fsn, fc) * 180 / Math.PI;
            double sum = 0, worst = 0;
            foreach (var i in best) { double e = Reproj(a[i], b[i], fx, fy, fc, fsn, fsc); sum += e; if (e > worst) worst = e; }
            double mean = sum / best.Count;

            ctx.LastPoint = new Cv.Point2f((float)fx, (float)fy);
            foreach (var i in best.Take(40)) DrawPt(dst, b[i]);
            Add(report, s, spec, true,
                $"平移 ({fx:F1},{fy:F1})，转 {rot:F2}°，缩放 {fsc:F3}，用上 {best.Count}/{n} 个点，平均偏差 {mean:F2} 像素",
                rot, best.Count,
                ("平移X", fx), ("平移Y", fy), ("转角", rot), ("缩放", fsc),
                ("对上的特征点数", n), ("站住的点数", best.Count), ("平均偏差", mean), ("最大偏差", worst));
        }

        /// <summary>两点定一个相似变换：两条连线向量的夹角差 = 转角，长度比 = 缩放，再对第一对点求平移。</summary>
        static bool SimilarFrom2(Cv.Point2f p0, Cv.Point2f p1, Cv.Point2f q0, Cv.Point2f q1,
            out double tx, out double ty, out double cos, out double sin, out double scale)
        {
            double ux = p1.X - p0.X, uy = p1.Y - p0.Y, vx = q1.X - q0.X, vy = q1.Y - q0.Y;
            double lu = Math.Sqrt(ux * ux + uy * uy), lv = Math.Sqrt(vx * vx + vy * vy);
            tx = ty = cos = sin = scale = 0;
            if (lu < 1e-6 || lv < 1e-6) return false;
            scale = lv / lu;
            if (scale < 0.05 || scale > 20) return false;                  // 荒谬缩放 = 配错了
            double ang = Math.Atan2(vy, vx) - Math.Atan2(uy, ux);
            cos = Math.Cos(ang); sin = Math.Sin(ang);
            tx = q0.X - scale * (p0.X * cos - p0.Y * sin);
            ty = q0.Y - scale * (p0.X * sin + p0.Y * cos);
            return true;
        }

        /// <summary>样例点按这个变换落到当前图之后，离配对点还有多远（像素）。</summary>
        static double Reproj(Cv.Point2f p, Cv.Point2f q, double tx, double ty, double cos, double sin, double scale)
        {
            double dx = scale * (p.X * cos - p.Y * sin) + tx - q.X;
            double dy = scale * (p.X * sin + p.Y * cos) + ty - q.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>闭式最小二乘相似变换（质心对齐 + 点积 / 叉积和求转角与缩放）；调用方保证点数 >= 3。</summary>
        static void FitLeastSquares(List<int> idx, List<Cv.Point2f> a, List<Cv.Point2f> b,
            out double tx, out double ty, out double cos, out double sin, out double scale)
        {
            double ppx = idx.Average(i => a[i].X), ppy = idx.Average(i => a[i].Y);
            double qpx = idx.Average(i => b[i].X), qpy = idx.Average(i => b[i].Y);
            double dot = 0, cross = 0, norm = 0;
            foreach (var i in idx)
            {
                double ux = a[i].X - ppx, uy = a[i].Y - ppy;
                double vx = b[i].X - qpx, vy = b[i].Y - qpy;
                dot += ux * vx + uy * vy; cross += ux * vy - uy * vx; norm += ux * ux + uy * uy;
            }
            tx = ty = cos = sin = scale = 0;
            if (norm < 1e-6 || Math.Abs(dot) + Math.Abs(cross) < 1e-9) return;
            scale = Math.Sqrt(dot * dot + cross * cross) / norm;
            double ang = Math.Atan2(cross, dot);
            cos = Math.Cos(ang); sin = Math.Sin(ang);
            tx = qpx - scale * (ppx * cos - ppy * sin);
            ty = qpy - scale * (ppx * sin + ppy * cos);
        }

        static void DoBlob(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var roi = RoiOf(spec, s, gray);
            double thr = Num(spec, s, "thr", 128);
            bool bright = Txt(spec, s, "which") != "暗斑";
            double minArea = Num(spec, s, "minArea", 30), maxArea = Num(spec, s, "maxArea", 0);
            double expect = Num(spec, s, "expect", 0);

            using var sub = new Cv.Mat(gray, roi);
            using var bin = new Cv.Mat();
            Cv.Cv2.Threshold(sub, bin, thr, 255,
                bright ? Cv.ThresholdTypes.Binary : Cv.ThresholdTypes.BinaryInv);
            Cv.Cv2.FindContours(bin, out Cv.Point[][] cs, out _, Cv.RetrievalModes.External,
                Cv.ContourApproximationModes.ApproxSimple);

            int n = 0; double total = 0, biggest = 0; Cv.Point2f cen = default; bool first = true;
            foreach (var c in cs)
            {
                double area = Cv.Cv2.ContourArea(c);
                if (area < minArea) continue;
                if (maxArea > 0 && area > maxArea) continue;
                n++; total += area;
                if (area > biggest)
                {
                    biggest = area;
                    var mo = Cv.Cv2.Moments(c);
                    if (mo.M00 > 1e-9) cen = new Cv.Point2f((float)(mo.M10 / mo.M00), (float)(mo.M01 / mo.M00));
                }
                var shifted = Array.ConvertAll(c, q => new Cv.Point(q.X + roi.Left, q.Y + roi.Top));
                dst.Polylines(new[] { shifted }, true, Ink, 1);
                first = false;
            }
            if (n > 0) { DrawPt(dst, cen); ctx.LastPoint = cen; }
            bool pass = expect <= 0 || Math.Abs(n - expect) < 0.5;
            Add(report, s, spec, n > 0 || expect <= 0,
                $"找到 {n} 个｜总面积 {total:F0}｜最大 {biggest:F0}｜重心 ({cen.X:F0},{cen.Y:F0})｜{(pass ? "数量符合" : $"应该是 {expect:F0} 个")}",
                biggest, n, ("斑点个数", n), ("总面积", total), ("最大面积", biggest),
                ("重心X", cen.X), ("重心Y", cen.Y), ("合格与否", pass ? 1 : 0));
        }

        static void DoRegions(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var roi = RoiOf(spec, s, gray);
            using var sub = new Cv.Mat(gray, roi);
            using var bin = new Cv.Mat();
            Cv.Cv2.Threshold(sub, bin, Num(spec, s, "thr", 128), 255, Cv.ThresholdTypes.Binary);
            using var labels = new Cv.Mat();
            int n = Cv.Cv2.ConnectedComponents(bin, labels, Cv.PixelConnectivity.Connectivity8);
            double minArea = Num(spec, s, "minArea", 20);
            int kept = 0; double biggest = 0, total = 0;
            if (labels.Type() == Cv.MatType.CV_32SC1)
            {
                var counts = new double[Math.Max(2, n)];
                for (int y = 0; y < labels.Rows; y++)
                    for (int x = 0; x < labels.Cols; x++) counts[labels.At<int>(y, x)]++;
                for (int i = 1; i < counts.Length; i++)
                    if (counts[i] >= minArea) { kept++; total += counts[i]; biggest = Math.Max(biggest, counts[i]); }
            }
            dst.Rectangle(roi, Ink, 1);
            Add(report, s, spec, true,
                $"{kept} 块｜最大 {biggest:F0}｜平均 {(kept > 0 ? total / kept : 0):F1}｜总 {total:F0}",
                biggest, kept, ("块数", kept), ("最大块面积", biggest),
                ("平均块面积", kept > 0 ? total / kept : 0), ("总面积", total));
        }

        static void DoReadCode(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress, bool? oneD)
        {
            var roi = RoiOf(spec, s, cur);
            using var sub = new Cv.Mat(cur, roi);
            var texts = new List<string>();
            var centers = new List<Cv.Point2f>();

            if (oneD != false)
            {
                try
                {
                    using var det = new Cv.BarcodeDetector();
                    det.DetectAndDecode(sub, out var pts, out var codes, out _);
                    if (codes != null)
                        foreach (var c in codes) if (!string.IsNullOrWhiteSpace(c)) texts.Add(c.Trim());
                    foreach (var g in Group4(pts)) centers.Add(CenterOf(g));
                }
                catch (Exception ex) { progress?.Report("一维码识别不可用：" + ex.Message); }
            }
            if (oneD != true && texts.Count == 0)
            {
                try
                {
                    using var qr = new Cv.QRCodeDetector();
                    string payload = qr.DetectAndDecode(sub, out var corners, null);
                    if (!string.IsNullOrWhiteSpace(payload))
                    { texts.Add(payload.Trim()); centers.Add(CenterOf(corners)); }
                }
                catch (Exception ex) { progress?.Report("二维码识别不可用：" + ex.Message); }
            }

            foreach (var c in centers) DrawPt(dst, c);
            string got = texts.Count > 0 ? texts[0] : "";
            string expect = Txt(spec, s, "expect");
            bool pass;
            if (expect.Length == 0) pass = texts.Count > 0;
            else pass = TxtMatch(Txt(spec, s, "how"), got, expect);
            if (texts.Count == 0 && oneD == null && Num(spec, s, "allowEmpty", 0) >= 1) pass = true;

            if (texts.Count == 0)
            { Add(report, s, spec, false, "没读到码：把框缩小到码上、调亮画面，或确认码没有破损", 0, 0,
                    ("内容", 0), ("码类型", 0), ("合格与否", pass ? 1 : 0)); return; }
            Add(report, s, spec, true,
                $"读到「{got}」｜共 {texts.Count} 个｜{(pass ? "和期望一致" : expect.Length > 0 ? "和期望不一致（期望「" + expect + "」）" : "没填期望，只记录")}",
                texts.Count, texts.Count, ("合格与否", pass ? 1 : 0));
            report.Results[report.Results.Count - 1].Text = got;
        }

        static bool TxtMatch(string how, string got, string expect) => how switch
        {
            "包含" => (got ?? "").Contains(expect ?? "", StringComparison.OrdinalIgnoreCase),
            "开头是" => (got ?? "").StartsWith(expect ?? "", StringComparison.OrdinalIgnoreCase),
            _ => string.Equals((got ?? "").Trim(), (expect ?? "").Trim(), StringComparison.OrdinalIgnoreCase),
        };

        // ===================== 批次 4：图像与颜色（返回新图） =====================

        static Cv.Mat DoColorSpace(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            string to = Txt(spec, s, "to");
            var outMat = new Cv.Mat();
            Cv.ColorConversionCodes code = to switch
            {
                "灰度" => cur.Channels() == 1 ? 0 : Cv.ColorConversionCodes.BGR2GRAY,
                "HSV" => cur.Channels() == 1 ? 0 : Cv.ColorConversionCodes.BGR2HSV,
                "Lab" => cur.Channels() == 1 ? 0 : Cv.ColorConversionCodes.BGR2Lab,
                _ => cur.Channels() == 1 ? Cv.ColorConversionCodes.GRAY2BGR : 0,
            };
            if (code == 0) { cur.CopyTo(outMat); Add(report, s, spec, true, "这张图已经是" + to + "，没换"); return outMat; }
            Cv.Cv2.CvtColor(cur, outMat, code);
            Add(report, s, spec, true, "已换成" + to + "（后面「只留一种颜色」选对应通道最好用）", 0, 0, ("换成了哪种", 1));
            return outMat;
        }

        static Cv.Mat DoChannel(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            string ch = Txt(spec, s, "ch");
            bool needHsv = ch.StartsWith("H(") || ch.StartsWith("S(") || ch.StartsWith("V(");
            using var work = new Cv.Mat();
            if (needHsv && cur.Channels() != 1) Cv.Cv2.CvtColor(cur, work, Cv.ColorConversionCodes.BGR2HSV);
            else cur.CopyTo(work);

            int idx = ch switch
            {
                "H(色调)" => 0, "S(饱和)" => 1, "V(明度)" => 2,
                "B" => 0, "G" => 1, "R" => 2,
                _ => 2,
            };
            var planes = Cv.Cv2.Split(work);
            int take = work.Channels() == 1 ? 0 : Math.Min(idx, planes.Length - 1);
            var one = planes[take];
            var outMat = new Cv.Mat();
            if (work.Channels() == 1) one.CopyTo(outMat);
            else Cv.Cv2.CvtColor(one, outMat, Cv.ColorConversionCodes.GRAY2BGR);
            foreach (var pl in planes) pl.Dispose();
            Add(report, s, spec, true, "只留了「" + ch + "」这一层（灰白图，越亮表示这个分量越大）", 0, 0,
                ("留了哪个通道", idx));
            return outMat;
        }

        static void DoColorCheck(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            // 目录里只有一个 roi 参数（"x,y,w,h"），统一走 RoiOf，别自己拆 roiX/roiW（那样永远取不到）
            var roi = RoiOf(spec, s, ToGray(cur));
            using var sub = new Cv.Mat(cur, roi);
            using var hsv = new Cv.Mat();
            if (sub.Channels() == 1) Cv.Cv2.CvtColor(sub, hsv, Cv.ColorConversionCodes.GRAY2BGR);
            else Cv.Cv2.CvtColor(sub, hsv, Cv.ColorConversionCodes.BGR2HSV);
            var m = Cv.Cv2.Mean(hsv);
            double eh = Num(spec, s, "eh", 0), es = Num(spec, s, "es", 0), ev = Num(spec, s, "ev", 200);
            double tol = Num(spec, s, "tol", 30);
            double dh = Math.Abs(m.Val0 - eh), ds = Math.Abs(m.Val1 - es), dv = Math.Abs(m.Val2 - ev);
            double worst = Math.Max(dh, Math.Max(ds, dv));
            bool pass = worst <= tol;
            dst.Rectangle(roi, pass ? Ink : InkWarn, 2);
            Add(report, s, spec, true,
                $"平均色 H {m.Val0:F0} / S {m.Val1:F0} / V {m.Val2:F0}｜最大偏差 {worst:F0}（允许 {tol:F0}）｜{(pass ? "合格" : "不合格")}",
                worst, 1, ("平均H", m.Val0), ("平均S", m.Val1), ("平均V", m.Val2), ("最大偏差", worst), ("合格与否", pass ? 1 : 0));
        }

        static Cv.Mat DoEnhance(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            string how = Txt(spec, s, "how");
            double k = Num(spec, s, "strength", 2);
            bool color = cur.Channels() > 1;
            using var g = new Cv.Mat();
            if (color) Cv.Cv2.CvtColor(cur, g, Cv.ColorConversionCodes.BGR2GRAY); else gray.CopyTo(g);
            Cv.Cv2.MeanStdDev(g, out Cv.Scalar bm, out Cv.Scalar bs);
            double before = bs.Val0;

            using var done = new Cv.Mat();
            switch (how)
            {
                case "整体拉伸":
                    Cv.Cv2.Normalize(g, done, 0, 255, Cv.NormTypes.MinMax);
                    break;
                case "提亮":
                case "压暗":
                {
                    double gamma = how == "提亮" ? 1.0 / Math.Max(0.2, k) : Math.Max(0.2, k);
                    var lut = new byte[256];
                    for (int i = 0; i < 256; i++)
                        lut[i] = (byte)Math.Max(0, Math.Min(255, Math.Round(255 * Math.Pow(i / 255.0, gamma))));
                    using var t = new Cv.Mat(1, 256, Cv.MatType.CV_8UC1);
                    for (int i = 0; i < 256; i++) t.Set<byte>(0, i, lut[i]);
                    Cv.Cv2.LUT(g, t, done);
                    break;
                }
                default:
                    using (var clahe = Cv.Cv2.CreateCLAHE(clipLimit: Math.Max(1, k), tileGridSize: new Cv.Size(8, 8)))
                        clahe.Apply(g, done);
                    break;
            }
            Cv.Cv2.MeanStdDev(done, out Cv.Scalar am, out Cv.Scalar as2);
            double after = as2.Val0;
            var outMat = new Cv.Mat();
            if (color) Cv.Cv2.CvtColor(done, outMat, Cv.ColorConversionCodes.GRAY2BGR); else done.CopyTo(outMat);
            Add(report, s, spec, true, $"{how}：起伏从 {before:F1} 变到 {after:F1}", after, 0,
                ("增强前对比度", before), ("增强后对比度", after));
            return outMat;
        }

        static Cv.Mat DoDenoise(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            string how = Txt(spec, s, "how");
            int size = (int)Num(spec, s, "size", 5);
            if (size < 3) size = 3;
            if (size % 2 == 0) size++;                       // 核尺寸必须是奇数
            var outMat = new Cv.Mat();
            switch (how)
            {
                case "中值": Cv.Cv2.MedianBlur(cur, outMat, size); break;
                case "高斯": Cv.Cv2.GaussianBlur(cur, outMat, new Cv.Size(size, size), 0); break;
                default: Cv.Cv2.BilateralFilter(cur, outMat, size, 60, 60); break;
            }
            Add(report, s, spec, true, $"用「{how}」洗过一遍，范围 {size}", size, 0, ("范围多大", size));
            return outMat;
        }

        static Cv.Mat DoGeometry(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            double dx = Num(spec, s, "dx", 0), dy = Num(spec, s, "dy", 0);
            double angle = Num(spec, s, "angle", 0), scale = Num(spec, s, "scale", 1);
            string flip = Txt(spec, s, "flip");
            scale = Math.Max(0.1, scale);
            var center = new Cv.Point2f(cur.Cols / 2f, cur.Rows / 2f);
            using var m = Cv.Cv2.GetRotationMatrix2D(center, angle, scale);
            m.At<double>(0, 2) += dx;
            m.At<double>(1, 2) += dy;
            var outMat = new Cv.Mat();
            Cv.Cv2.WarpAffine(cur, outMat, m, new Cv.Size(cur.Cols, cur.Rows),
                Cv.InterpolationFlags.Linear, Cv.BorderTypes.Constant, new Cv.Scalar(255, 255, 255));
            if (flip == "左右翻" || flip == "上下翻")
            {
                var t = new Cv.Mat();
                Cv.Cv2.Flip(outMat, t, flip == "左右翻" ? Cv.FlipMode.Y : Cv.FlipMode.X);
                outMat.Dispose(); outMat = t;
            }
            Add(report, s, spec, true,
                $"挪 ({dx:F0},{dy:F0})｜转 {angle:F1}°｜缩放 {scale:F2}｜{flip}", angle, 0,
                ("平移X", dx), ("平移Y", dy), ("转角", angle), ("缩放", scale));
            return outMat;
        }

        static void DoSaveImage(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            string path = Txt(spec, s, "path");
            if (path.Length == 0) path = "out\\视觉.jpg";
            if (path.Contains("%")) path = path.Replace("%", DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
            try
            {
                var dir = Path.GetDirectoryName(Path.GetFullPath(path));
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                bool with = Num(spec, s, "withMark", 1) >= 1;
                bool ok = Cv.Cv2.ImWrite(path, with ? dst : cur);
                Add(report, s, spec, ok, ok ? "已存到 " + Path.GetFullPath(path) : "写文件失败（看看路径是否合法、有没有被占用）",
                    0, 0, ("是否成功", ok ? 1 : 0));
            }
            catch (Exception ex) { Add(report, s, spec, false, "存图失败：" + ex.Message); }
        }

        static Cv.Mat DoLoadImage(VisionOpSpec spec, VisualFlowStep s, VisionReport report, IProgress<string>? progress)
        {
            string path = Txt(spec, s, "path");
            if (path.Length == 0 || !File.Exists(path))
            { Add(report, s, spec, false, "找不到这个图片文件：" + path); return null; }
            var m = Cv.Cv2.ImRead(path, Cv.ImreadModes.Color);
            if (m.Empty()) { Add(report, s, spec, false, "这张图打不开：" + path); return null; }
            Add(report, s, spec, true, $"已读入 {m.Cols}×{m.Rows}：{Path.GetFileName(path)}", m.Cols, m.Rows,
                ("宽", m.Cols), ("高", m.Rows));
            return m;
        }

        static Cv.Mat DoTwoImage(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            string path = Txt(spec, s, "second");
            if (path.Length == 0 || !File.Exists(path)) { Add(report, s, spec, false, "先选另一张图（填完整路径）"); return null; }
            using var b0 = Cv.Cv2.ImRead(path, Cv.ImreadModes.Color);
            if (b0.Empty()) { Add(report, s, spec, false, "另一张图打不开：" + path); return null; }
            using var b = new Cv.Mat();
            if (b0.Size() != cur.Size()) Cv.Cv2.Resize(b0, b, new Cv.Size(cur.Cols, cur.Rows));
            else b0.CopyTo(b);
            var outMat = new Cv.Mat();
            switch (Txt(spec, s, "op"))
            {
                case "相加": Cv.Cv2.Add(cur, b, outMat); break;
                case "都亮的留": Cv.Cv2.BitwiseAnd(cur, b, outMat); break;
                case "有一个亮就留": Cv.Cv2.BitwiseOr(cur, b, outMat); break;
                case "取绝对差": Cv.Cv2.Absdiff(cur, b, outMat); break;
                default: Cv.Cv2.Absdiff(cur, b, outMat); break;      // 相减找差异
            }
            Add(report, s, spec, true, "已和「" + Path.GetFileName(path) + "」做了「" + Txt(spec, s, "op") + "」", 0, 0);
            return outMat;
        }

        static Cv.Mat DoCrop(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            var roi = RoiOf(spec, s, gray);
            if (roi.Width >= cur.Cols && roi.Height >= cur.Rows)
            { Add(report, s, spec, false, "框几乎和整张图一样大，没得裁：先在右边拖一个小一点的框"); return null; }
            var outMat = new Cv.Mat(cur, roi).Clone();
            Add(report, s, spec, true, $"只留下 ({roi.X},{roi.Y}) 起 {roi.Width}×{roi.Height} 这一块", roi.Width, roi.Height,
                ("框X", roi.X), ("框Y", roi.Y), ("宽", roi.Width), ("高", roi.Height));
            return outMat;
        }

        // ===================== 小画图工具 =====================

        static void DrawPt(Cv.Mat dst, Cv.Point2f p)
        {
            dst.Line((int)p.X - 5, (int)p.Y, (int)p.X + 5, (int)p.Y, Dot, 1);
            dst.Line((int)p.X, (int)p.Y - 5, (int)p.X, (int)p.Y + 5, Dot, 1);
        }

        /// <summary>条形码检测器给的是摊平的所有角点，四个一组切成一个个码的框。</summary>
        static List<Cv.Point2f[]> Group4(Cv.Point2f[] flat)
        {
            var list = new List<Cv.Point2f[]>();
            if (flat == null) return list;
            for (int i = 0; i + 3 < flat.Length; i += 4)
                list.Add(new[] { flat[i], flat[i + 1], flat[i + 2], flat[i + 3] });
            return list;
        }

        /// <summary>类间方差最大的那个灰度就是 Otsu 分界（直方图已经算好了，直接扫一遍）。</summary>
        static double OtsuFromHist(float[] bins, double total)
        {
            if (bins == null || bins.Length == 0 || total <= 0) return 0;
            double sumAll = 0;
            for (int i = 0; i < bins.Length; i++) sumAll += i * bins[i];
            double sumB = 0, wB = 0, best = -1;
            var plate = new List<int>();
            for (int i = 0; i < bins.Length; i++)
            {
                wB += bins[i];
                if (wB <= 0) continue;
                double wF = total - wB;
                if (wF <= 0) break;
                sumB += i * bins[i];
                double mB = sumB / wB, mF = (sumAll - sumB) / wF;
                double between = wB * wF * (mB - mF) * (mB - mF);
                if (between > best * 1.0001 + 1e-9) { best = between; plate.Clear(); plate.Add(i); }
                else if (Math.Abs(between - best) <= best * 0.0001 + 1e-9) plate.Add(i);
            }
            // 纯双峰（只有 0 和 255）时，中间任何一档的类间方差都一样大：取平台中点，
            // 才和 OpenCV 给的结果一致（取第一个会得 0，看着像"没算出来"）。
            return plate.Count == 0 ? 0 : (plate[0] + plate[plate.Count - 1]) / 2.0;
        }

        /// <summary>暴力汉明匹配 + Lowe 比值检验：只留「最近明显比次近近」的点对。</summary>
        /// <summary>返回所有模板特征里「最近距离」的最小值，供失败信息判断原因。</summary>
        static int MatchByHamming(Cv.Mat dA, Cv.Mat dB, Cv.KeyPoint[] kpA, Cv.KeyPoint[] kpB,
            List<Cv.Point2f> a, List<Cv.Point2f> b)
        {
            int cols = dA.Cols;
            int nearestSeen = 9999;
            var ba = new byte[dA.Rows * cols];
            var bb = new byte[dB.Rows * cols];
            System.Runtime.InteropServices.Marshal.Copy(dA.Data, ba, 0, ba.Length);
            System.Runtime.InteropServices.Marshal.Copy(dB.Data, bb, 0, bb.Length);
            var pop = Bits();
            for (int i = 0; i < dA.Rows; i++)
            {
                int best = -1, bestD = 9999, second = 9999;
                for (int j = 0; j < dB.Rows; j++)
                {
                    int d = 0;
                    for (int k = 0; k < cols; k++)
                        d += pop[ba[i * cols + k] ^ bb[j * cols + k]];
                    if (d < bestD) { second = bestD; bestD = d; best = j; }
                    else if (d < second) second = d;
                }
                if (bestD < nearestSeen) nearestSeen = bestD;
                if (best < 0 || bestD > 60) continue;                 // 绝对太远：不是同一个点
                if (second <= bestD) continue;                        // 次近一样近：说明是巧合，丢掉
                a.Add(kpA[i].Pt); b.Add(kpB[best].Pt);
            }
            return nearestSeen;
        }

        /// <summary>0~255 的置位计数表（汉明距离按位算）。</summary>
        static byte[] Bits()
        {
            var t = new byte[256];
            for (int i = 0; i < 256; i++) { int x = i, n = 0; while (x != 0) { n += x & 1; x >>= 1; } t[i] = (byte)n; }
            return t;
        }

        static Cv.Point2f CenterOf(Cv.Point2f[] poly)
        {
            if (poly == null || poly.Length == 0) return default;
            double x = 0, y = 0; foreach (var p in poly) { x += p.X; y += p.Y; }
            return new Cv.Point2f((float)(x / poly.Length), (float)(y / poly.Length));
        }

        // ===================== 参数读取 =====================

        static (double cx, double cy, double len, double wide, double ang, VisionCaliper.Polarity pol,
                double minGrad, int samples, double keep)
            LineParams(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray)
        {
            var roi = RoiOf(spec, s, gray);
            double cx = Num(spec, s, "cx", 0), cy = Num(spec, s, "cy", 0);
            if (cx <= 0 || cy <= 0) { cx = roi.Left + roi.Width / 2.0; cy = roi.Top + roi.Height / 2.0; }
            return (cx, cy, Num(spec, s, "length", 200), Num(spec, s, "width", 40), Num(spec, s, "angle", 0),
                    PolarityOf(spec, s), Num(spec, s, "minGrad", 8), (int)Num(spec, s, "samples", 60), Num(spec, s, "keep", 0.9));
        }

        static VisionCaliper.Polarity PolarityOf(VisionOpSpec spec, VisualFlowStep s)
            => Txt(spec, s, "polarity") switch
            {
                "暗到亮" => VisionCaliper.Polarity.DarkToLight,
                "亮到暗" => VisionCaliper.Polarity.LightToDark,
                _ => VisionCaliper.Polarity.Any,
            };

        /// <summary>框选区域；没填或填成 0 就用整张图。</summary>
        static Cv.Rect RoiOf(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray)
        {
            var t = Txt(spec, s, "roi");
            var parts = t.Split(new[] { ',', '，', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 4
                && double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double x)
                && double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double y)
                && double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double w)
                && double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double h)
                && w > 2 && h > 2)
            {
                int x0 = (int)Math.Max(0, Math.Min(x, gray.Cols - 1));
                int y0 = (int)Math.Max(0, Math.Min(y, gray.Rows - 1));
                return new Cv.Rect(x0, y0,
                    (int)Math.Max(3, Math.Min(w, gray.Cols - x0)),
                    (int)Math.Max(3, Math.Min(h, gray.Rows - y0)));
            }
            return new Cv.Rect(0, 0, gray.Cols, gray.Rows);
        }

        static double Num(VisionOpSpec spec, VisualFlowStep s, string key, double def)
        {
            var p = spec.Find(key);
            string raw = s.GetOpParam(key, p?.Default ?? def.ToString(CultureInfo.InvariantCulture));
            return double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out double v) ? v : def;
        }

        static string Txt(VisionOpSpec spec, VisualFlowStep s, string key)
        {
            var p = spec.Find(key);
            return (s.GetOpParam(key, p?.Default ?? "") ?? "").Trim();
        }

        // ===================== 画图与记账 =====================

        static void DrawLine(Cv.Mat dst, VisionCaliper.LineResult f)
        {
            dst.Line((int)Math.Round(f.X1), (int)Math.Round(f.Y1), (int)Math.Round(f.X2), (int)Math.Round(f.Y2), Ink, 2);
            dst.Circle((int)Math.Round(f.X1), (int)Math.Round(f.Y1), 3, Dot, 2);
            dst.Circle((int)Math.Round(f.X2), (int)Math.Round(f.Y2), 3, Dot, 2);
        }

        static Cv.Mat ToGray(Cv.Mat src)
        {
            var g = new Cv.Mat();
            if (src.Channels() == 1) src.CopyTo(g);
            else Cv.Cv2.CvtColor(src, g, src.Channels() == 4 ? Cv.ColorConversionCodes.BGRA2GRAY : Cv.ColorConversionCodes.BGR2GRAY);
            return g;
        }

        static void Add(VisionReport report, VisualFlowStep s, VisionOpSpec spec, bool ok, string summary,
            double value = 0, int count = 0, params (string Name, double V)[] numbers)
        {
            var r = new VisionStepResult
            {
                StepName = s.Name,
                Type = spec.Name,
                Ok = ok,
                Summary = summary,
                Value = value,
                Count = count,
            };
            foreach (var (n, v) in numbers) r.Numbers.Add(new System.Collections.Generic.KeyValuePair<string, double>(n, v));
            report.Results.Add(r);
        }

        /// <summary>按名字取本次报告里最后一个算子的输出量（测试与「存进变量」用）。</summary>
        public static double Number(VisionReport report, string name)
        {
            for (int i = report.Results.Count - 1; i >= 0; i--)
                foreach (var kv in report.Results[i].Numbers)
                    if (kv.Key == name) return kv.Value;
            return double.NaN;
        }

        static double Sq(double v) => v * v;
        static double Dist(Cv.Point2f a, Cv.Point2f b) => Math.Sqrt(Sq(a.X - b.X) + Sq(a.Y - b.Y));
    }
}

// === NoCodeMotion 视觉流程引擎（OpenCV 实现） | 作者：温⁣启⁠志‌ ‌|​ ​微⁠信​：‎1‎8‎7‎1⁠9‎3​6‏1‎3‎9⁠9⁠ ​|⁣ ‌保⁣留​所⁠有​权‍利‏，⁠请‎勿‏删‍除 ===
