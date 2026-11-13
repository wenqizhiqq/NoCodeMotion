// === NoCodeMotion 视觉流程引擎（VisionPlus 算子移植 · OpenCV 真实实现） | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
using System;
using System.Collections.Generic;
using System.Globalization;
using Cv = OpenCvSharp;
using NoCodeMotion.Models;
using ZXing;
using ZXing.Common;

namespace NoCodeMotion.Services.Vision
{
    /// <summary>
    /// VisionPlus 的 DataCodeTool / MVPDecoder / 码质量评级移植。
    /// 解码用纯托管 ZXing（DataMatrix / PDF417 / Aztec / QR / 一维码），
    /// 前后处理与质量评级用 OpenCV 真实计算，不调用 VisionPlus 的任何 DLL。
    /// </summary>
    internal static class VpOpsCode
    {
        public static Cv.Mat Run(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            switch (spec.Key)
            {
                case "dmcode": DoDecode(spec, s, gray, dst, report, ctx, progress); break;
                case "codegrade": DoGrade(spec, s, gray, dst, report, progress); break;
            }
            return cur;
        }

        // ===================== 读码 =====================

        static void DoDecode(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var fmt = VpUtil.Txt(spec, s, "format");
            var expected = VpUtil.Txt(spec, s, "expected");
            bool tryHard = VpUtil.Bool(spec, s, "tryHard", true);
            double up = VpUtil.Clamp(VpUtil.Num(spec, s, "upscale", 2), 1, 8);
            var roi = VpUtil.Roi(spec, s, gray);

            using var crop = new Cv.Mat(gray, roi);
            using var eight = new Cv.Mat();
            // 16 位 / 浮点图统一压到 8 位，ZXing 只吃 8 位亮度
            if (crop.Depth() != Cv.MatType.CV_8U)
                crop.ConvertTo(eight, Cv.MatType.CV_8U, 255.0 / 65535.0);
            else crop.CopyTo(eight);

            var reader = new BarcodeReaderGeneric
            {
                Options = new DecodingOptions
                {
                    TryHarder = tryHard,
                    TryInverted = tryHard,
                    PureBarcode = false,
                    PossibleFormats = Formats(fmt),
                }
            };

            // 候选图：原图 → 放大 → （难读时）反色 / 锐化 / CLAHE 提对比
            var candidates = new List<(string Tag, Cv.Mat Img, double Scale)>();
            candidates.Add(("原图", eight, 1.0));
            if (up > 1.01)
            {
                var big = new Cv.Mat();
                Cv.Cv2.Resize(eight, big, new Cv.Size(), up, up, Cv.InterpolationFlags.Cubic);
                candidates.Add(("放大", big, up));
            }
            if (tryHard)
            {
                var inv = new Cv.Mat();
                Cv.Cv2.BitwiseNot(eight, inv);
                candidates.Add(("反色", inv, 1.0));

                var sharp = new Cv.Mat();
                Cv.Cv2.GaussianBlur(eight, sharp, new Cv.Size(0, 0), 1.5);
                Cv.Cv2.AddWeighted(eight, 1.8, sharp, -0.8, 0, sharp, Cv.MatType.CV_8U);
                candidates.Add(("锐化", sharp, 1.0));
            }

            string text = "", formatName = "";
            string hitTag = "";
            double hitScale = 1;
            ZXing.ResultPoint[]? points = null;

            try
            {
                foreach (var (tag, img, scale) in candidates)
                {
                    byte[] lum = ToLumBytes(img);
                    try
                    {
                        var r = reader.Decode(lum, img.Cols, img.Rows, RGBLuminanceSource.BitmapFormat.Gray8);
                        if (r != null && !string.IsNullOrEmpty(r.Text))
                        {
                            text = r.Text;
                            formatName = r.BarcodeFormat.ToString();
                            points = r.ResultPoints;
                            hitTag = tag;
                            hitScale = scale;
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        // 某个候选没解出来是正常的，继续试下一个；全部失败会在下面统一记一步失败。
                    }
                }
            }
            finally
            {
                // eight 由 using 管理；这里只释放后来新建的候选图
                foreach (var (_, img, _) in candidates)
                    if (!ReferenceEquals(img, eight)) img.Dispose();
            }

            var st = VpUtil.State(ctx);
            if (text.Length == 0)
            {
                st.LastCode = "";
                VpUtil.Add(report, s, spec, false,
                    "没读到码：把框对准码、调大「小码放大几倍」，或勾上「难读时多试几次」",
                    0, 0, "",
                    ("是否读到", 0), ("内容长度", 0), ("质量分", 0), ("是否合格", 0));
                Cv.Cv2.Rectangle(dst, roi, VpUtil.Warn, 2);
                return;
            }

            st.LastCode = text;
            bool pass = true;
            if (expected.Length > 0)
                pass = string.Equals(text.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase)
                    || text.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;

            // 质量分为真实图像统计：解码成功只作为基础分，对比度/静区另算，不编数
            double grade = CodeQuality(eight, out double contrast, out double quietPx, out double uniform);
            if (points != null && points.Length >= 2)
            {
                double minX = double.MaxValue, minY = double.MaxValue, maxX = 0, maxY = 0;
                foreach (var p in points)
                {
                    double x = roi.X + p.X / hitScale, y = roi.Y + p.Y / hitScale;
                    minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
                    Cv.Cv2.Circle(dst, (int)Math.Round(x), (int)Math.Round(y), 3, VpUtil.Ink, 2);
                }
                if (maxX > minX && maxY > minY)
                    Cv.Cv2.Rectangle(dst,
                        new Cv.Point((int)minX, (int)minY), new Cv.Point((int)maxX, (int)maxY),
                        VpUtil.Ink, 2);
            }
            Cv.Cv2.PutText(dst, formatName + " " + Short(text), new Cv.Point(roi.X, Math.Max(12, roi.Y - 6)),
                Cv.HersheyFonts.HersheySimplex, 0.5, VpUtil.Ink, 1);

            string tail = expected.Length > 0
                ? (pass ? "，和期望内容一致" : "，与期望内容不一致")
                : "";
            VpUtil.Add(report, s, spec, pass,
                $"读到 {formatName}：{Short(text)}（{hitTag} 命中，放大 {hitScale:0.#} 倍）{tail}",
                value: grade, count: 1, text: text,
                ("是否读到", 1), ("内容长度", text.Length), ("质量分", grade), ("是否合格", pass ? 1 : 0),
                ("对比度", contrast), ("静区宽度(像素)", quietPx), ("模块均匀度", uniform));
            VpUtil.Progress(progress, spec.Name + "：" + Short(text));
        }

        static string Short(string t)
        {
            t = (t ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
            return t.Length <= 40 ? t : t.Substring(0, 40) + "…";
        }

        static List<BarcodeFormat> Formats(string choice)
        {
            var all = new List<BarcodeFormat>
            {
                BarcodeFormat.DATA_MATRIX, BarcodeFormat.PDF_417, BarcodeFormat.AZTEC,
                BarcodeFormat.QR_CODE, BarcodeFormat.CODE_128, BarcodeFormat.CODE_39,
                BarcodeFormat.EAN_13, BarcodeFormat.EAN_8, BarcodeFormat.ITF, BarcodeFormat.UPC_A
            };
            switch (choice)
            {
                case "DataMatrix": return new List<BarcodeFormat> { BarcodeFormat.DATA_MATRIX };
                case "PDF417": return new List<BarcodeFormat> { BarcodeFormat.PDF_417 };
                case "Aztec": return new List<BarcodeFormat> { BarcodeFormat.AZTEC };
                case "QR": return new List<BarcodeFormat> { BarcodeFormat.QR_CODE };
                case "一维码": return new List<BarcodeFormat>
                {
                    BarcodeFormat.CODE_128, BarcodeFormat.CODE_39, BarcodeFormat.CODE_93,
                    BarcodeFormat.EAN_13, BarcodeFormat.EAN_8, BarcodeFormat.ITF,
                    BarcodeFormat.UPC_A, BarcodeFormat.UPC_E, BarcodeFormat.CODABAR
                };
                default: return all;
            }
        }

        /// <summary>Mat(CV_8UC1) → 连续灰度 byte[]，Gray8 每像素 1 字节。</summary>
        static byte[] ToLumBytes(Cv.Mat m)
        {
            int w = m.Cols, h = m.Rows;
            var buf = new byte[w * h];
            var idx = m.GetGenericIndexer<byte>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    buf[y * w + x] = idx[y, x];
            return buf;
        }

        // ===================== 码质量评级 =====================

        static void DoGrade(VisionOpSpec spec, VisualFlowStep s, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            double minContrast = VpUtil.Num(spec, s, "minContrast", 40);
            double quietNeed = VpUtil.Num(spec, s, "quiet", 2);
            int modules = VpUtil.Int(spec, s, "modules", 16);
            double minGrade = VpUtil.Num(spec, s, "minGrade", 60);
            var roi = VpUtil.Roi(spec, s, gray);

            using var crop = new Cv.Mat(gray, roi);
            using var eight = new Cv.Mat();
            if (crop.Depth() != Cv.MatType.CV_8U)
                crop.ConvertTo(eight, Cv.MatType.CV_8U, 255.0 / 65535.0);
            else crop.CopyTo(eight);

            double grade = CodeQuality(eight, out double contrast, out double quietPx, out double uniform);
            double modulePx = Math.Max(1.0, (double)Math.Min(roi.Width, roi.Height) / Math.Max(4, modules));
            double quietModules = quietPx / modulePx;

            // 三项各占一定权重：对比度、静区、模块均匀度，全部来自真实像素统计
            double cScore = VpUtil.Clamp(contrast / Math.Max(1, minContrast), 0, 1) * 40;
            double qScore = VpUtil.Clamp(quietModules / Math.Max(0.001, quietNeed), 0, 1) * 30;
            double uScore = VpUtil.Clamp(uniform, 0, 1) * 30;
            grade = cScore + qScore + uScore;
            bool pass = grade >= minGrade;
            string level = grade >= 90 ? "A" : grade >= 80 ? "B" : grade >= 70 ? "C"
                         : grade >= 60 ? "D" : grade >= 40 ? "E" : "F";

            Cv.Cv2.Rectangle(dst, roi, pass ? VpUtil.Ink : VpUtil.Warn, 2);
            Cv.Cv2.PutText(dst, $"Grade {level} {grade:0.0}", new Cv.Point(roi.X, Math.Max(12, roi.Y - 6)),
                Cv.HersheyFonts.HersheySimplex, 0.55, pass ? VpUtil.Ink : VpUtil.Warn, 1);

            VpUtil.Add(report, s, spec, pass,
                $"码质量 {level} 级（{grade:0.0} 分）：黑白对比 {contrast:0}，静区约 {quietPx:0} 像素 / {quietModules:0.0} 模块，模块均匀度 {uniform:0.00}" +
                (pass ? "" : $"，低于及格线 {minGrade:0}"),
                value: grade, count: 1, text: level,
                ("等级分", grade), ("对比度", contrast), ("静区宽度(像素)", quietPx),
                ("模块均匀度", uniform), ("是否合格", pass ? 1 : 0));
            VpUtil.Progress(progress, spec.Name + "：" + level + " " + grade.ToString("F1", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 真实图像统计：Otsu 分出黑白 → 两类均值差=对比度；
        /// 四条边到第一个黑像素的距离=静区；黑白游程长度的离散程度=模块均匀度。
        /// 返回 0~100 的综合分（同时把三项原值带出去）。
        /// </summary>
        static double CodeQuality(Cv.Mat eight, out double contrast, out double quietPx, out double uniform)
        {
            contrast = 0; quietPx = 0; uniform = 0;
            int w = eight.Cols, h = eight.Rows;
            if (w < 8 || h < 8) return 0;

            using var bin = new Cv.Mat();
            Cv.Cv2.Threshold(eight, bin, 0, 255, Cv.ThresholdTypes.Binary | Cv.ThresholdTypes.Otsu);

            byte[] src = ToLumBytes(eight);
            byte[] mask = ToLumBytes(bin);
            long darkSum = 0, lightSum = 0;
            int darkN = 0, lightN = 0;
            for (int i = 0; i < src.Length; i++)
            {
                if (mask[i] == 0) { darkSum += src[i]; darkN++; }
                else { lightSum += src[i]; lightN++; }
            }
            if (darkN == 0 || lightN == 0) return 0;
            double darkMean = (double)darkSum / darkN;
            double lightMean = (double)lightSum / lightN;
            contrast = Math.Abs(lightMean - darkMean);

            // 静区：从四边往里扫，第一次遇到黑块的距离，取四边最小值
            int Left = Side(bin, 0), Right = Side(bin, 1), Top = Side(bin, 2), Bottom = Side(bin, 3);
            quietPx = Math.Min(Math.Min(Left, Right), Math.Min(Top, Bottom));

            // 模块均匀度：中间若干行的黑白游程长度，变异系数越小越匀 → 分数越高
            var runs = new List<double>();
            for (int y = h / 4; y < h - h / 4; y += Math.Max(1, h / 12))
            {
                int start = 0;
                byte prev = mask[y * w];
                for (int x = 1; x < w; x++)
                {
                    byte cur = mask[y * w + x];
                    if (cur != prev)
                    {
                        int len = x - start;
                        if (len >= 2 && len < w / 2) runs.Add(len);
                        start = x; prev = cur;
                    }
                }
            }
            if (runs.Count >= 4)
            {
                double mean = 0;
                foreach (double v in runs) mean += v;
                mean /= runs.Count;
                double var = 0;
                foreach (double v in runs) var += (v - mean) * (v - mean);
                var /= runs.Count;
                double cv = mean > 0 ? Math.Sqrt(var) / mean : 1;
                uniform = VpUtil.Clamp(1.0 - cv, 0, 1);
            }

            return VpUtil.Clamp(contrast / 255.0 * 40 + VpUtil.Clamp(quietPx / 20.0, 0, 1) * 30 + uniform * 30, 0, 100);
        }

        /// <summary>side: 0=左 1=右 2=上 3=下；返回从该边到第一个黑像素的距离（像素）。</summary>
        static int Side(Cv.Mat bin, int side)
        {
            int w = bin.Cols, h = bin.Rows;
            var idx = bin.GetGenericIndexer<byte>();
            if (side == 0 || side == 1)
            {
                for (int d = 0; d < w / 2; d++)
                {
                    int x = side == 0 ? d : w - 1 - d;
                    for (int y = 0; y < h; y += Math.Max(1, h / 64))
                        if (idx[y, x] == 0) return d;
                }
            }
            else
            {
                for (int d = 0; d < h / 2; d++)
                {
                    int y = side == 2 ? d : h - 1 - d;
                    for (int x = 0; x < w; x += Math.Max(1, w / 64))
                        if (idx[y, x] == 0) return d;
                }
            }
            return side < 2 ? w / 2 : h / 2;
        }
    }
}
