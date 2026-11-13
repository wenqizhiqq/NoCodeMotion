// === NoCodeMotion 视觉流程引擎（VisionPlus 算子移植 · OpenCV 真实实现） | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
using System;
using System.Collections.Generic;
using System.Globalization;
using Cv = OpenCvSharp;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Vision
{
    /// <summary>一条颜色模板区域（学习时存下来，比对时逐区打分）。</summary>
    internal sealed class VpColorRegion
    {
        public Cv.Rect Box;
        public double[] Hist = Array.Empty<double>();
        public double Weight = 1;
    }

    /// <summary>
    /// VisionPlus 移植算子的跨步状态。挂在 <see cref="VisionOpContext.Vp"/> 上，
    /// 不往 VisualFlowStep 里堆字段（工程落盘只认 ParamJson）。
    /// </summary>
    internal sealed class VpState
    {
        // ---- 位置跟随 ----
        public bool HasFixture;
        public double RefX, RefY, RefAngle;
        public double Dx, Dy, Dr;

        // ---- 参考图 / 颜色模板 / 训练模型 ----
        public Cv.Mat? RefImage;                       // BGR 8U，缺陷比对 / 相似度 / 训练式找线圆
        public readonly List<VpColorRegion> ColorRegions = new();
        public double EasyAngle, EasyRadius;

        // ---- 深度图（CV_32F，单位由 ZUnit 决定）----
        public Cv.Mat? ZMap;
        public double ZUnit = 1.0;
        public double[]? PlaneA, PlaneB;               // z = a*x + b*y + c

        // ---- EL 传统学习：样本特征 + 标签 ----
        public readonly List<(double[] Feature, string Label)> Samples = new();
        public string LastLabel = "";

        // ---- 读码 / 几何 ----
        public string LastCode = "";
        public readonly List<Cv.Point2f> Created = new();
    }

    /// <summary>
    /// 新算子共用的小工具：参数读取、ROI、颜色转换、结果记账。
    /// 与 VisionOpRunner 自带的私有 helper 语义一致，独立一份是为了不改动已有执行器。
    /// </summary>
    internal static class VpUtil
    {
        public static readonly Cv.Scalar Ink = new Cv.Scalar(0, 220, 0);    // BGR 绿
        public static readonly Cv.Scalar Warn = new Cv.Scalar(0, 0, 230);   // BGR 红
        public static readonly Cv.Scalar Dot = new Cv.Scalar(255, 60, 60);  // BGR 蓝

        const string StateKey = "visionplus";

        public static VpState State(VisionOpContext ctx)
        {
            if (ctx.Vp.TryGetValue(StateKey, out var o) && o is VpState st) return st;
            st = new VpState();
            ctx.Vp[StateKey] = st;
            return st;
        }

        // ---------------- 参数 ----------------

        public static string Txt(VisionOpSpec spec, VisualFlowStep s, string key)
        {
            var p = spec.Find(key);
            return (s.GetOpParam(key, p?.Default ?? "") ?? "").Trim();
        }

        public static double Num(VisionOpSpec spec, VisualFlowStep s, string key, double def)
        {
            var p = spec.Find(key);
            string raw = s.GetOpParam(key, p?.Default ?? def.ToString(CultureInfo.InvariantCulture));
            return double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out double v) ? v : def;
        }

        public static int Int(VisionOpSpec spec, VisualFlowStep s, string key, int def)
            => (int)Math.Round(Num(spec, s, key, def));

        public static bool Bool(VisionOpSpec spec, VisualFlowStep s, string key, bool def)
        {
            var t = Txt(spec, s, key);
            if (t.Length == 0) return def;
            return t == "1" || t.Equals("true", StringComparison.OrdinalIgnoreCase)
                || t == "是" || t == "开" || t == "勾" || t == "on";
        }

        /// <summary>框选区域；没填或填 0 用整张图。返回一定落在图内。</summary>
        public static Cv.Rect Roi(VisionOpSpec spec, VisualFlowStep s, Cv.Mat img)
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
                int x0 = (int)Math.Max(0, Math.Min(x, img.Cols - 1));
                int y0 = (int)Math.Max(0, Math.Min(y, img.Rows - 1));
                return new Cv.Rect(x0, y0,
                    (int)Math.Max(3, Math.Min(w, img.Cols - x0)),
                    (int)Math.Max(3, Math.Min(h, img.Rows - y0)));
            }
            return new Cv.Rect(0, 0, img.Cols, img.Rows);
        }

        // ---------------- 图像 ----------------

        public static Cv.Mat ToGray(Cv.Mat src)
        {
            var g = new Cv.Mat();
            if (src.Channels() == 1) src.CopyTo(g);
            else Cv.Cv2.CvtColor(src, g, src.Channels() == 4
                ? Cv.ColorConversionCodes.BGRA2GRAY : Cv.ColorConversionCodes.BGR2GRAY);
            return g;
        }

        public static Cv.Mat ToBgr(Cv.Mat src)
        {
            var b = new Cv.Mat();
            if (src.Channels() == 3) src.CopyTo(b);
            else if (src.Channels() == 4) Cv.Cv2.CvtColor(src, b, Cv.ColorConversionCodes.BGRA2BGR);
            else Cv.Cv2.CvtColor(src, b, Cv.ColorConversionCodes.GRAY2BGR);
            return b;
        }

        /// <summary>把 ROI 裁出来（返回新 Mat，用完要 Dispose）。</summary>
        public static Cv.Mat Crop(Cv.Mat src, Cv.Rect r) => new Cv.Mat(src, r);

        public static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);

        public static string F(double v, int digits = 2)
            => double.IsNaN(v) || double.IsInfinity(v) ? "--" : v.ToString("F" + digits, CultureInfo.InvariantCulture);

        // ---------------- 记账 ----------------

        public static void Add(VisionReport report, VisualFlowStep s, VisionOpSpec spec, bool ok, string summary,
            double value = 0, int count = 0, string text = "", params (string Name, double V)[] numbers)
        {
            var r = new VisionStepResult
            {
                StepName = s.Name,
                Type = spec.Name,
                Ok = ok,
                Summary = summary,
                Value = value,
                Count = count,
                Text = text ?? "",
            };
            if (numbers != null)
                foreach (var (n, v) in numbers)
                    r.Numbers.Add(new KeyValuePair<string, double>(n, v));
            report.Results.Add(r);
        }

        public static void Progress(IProgress<string>? p, string msg) => p?.Report(msg);
    }
}
