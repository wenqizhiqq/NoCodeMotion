// === NoCodeMotion 视觉流程引擎（OpenCV 实现） | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Cv = OpenCvSharp;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.Services.Vision
{
    /// <summary>
    /// 单步执行结果（供页面结果列表展示）。
    /// </summary>
    public sealed class VisionStepResult
    {
        public string StepName { get; set; } = "";
        public string Type { get; set; } = "";
        public bool Ok { get; set; }
        public string Summary { get; set; } = "";
    }

    /// <summary>
    /// 视觉流程运行报告：包含最终标注后的图像像素缓冲（BGRA）与每步结果。
    /// 计算在后台线程完成，像素缓冲为纯 byte[]，由调用方（UI 线程）组装成 WriteableBitmap。
    /// </summary>
    public sealed class VisionReport
    {
        public bool HasImage;
        public int Width;
        public int Height;
        public byte[]? Bgra = Array.Empty<byte>();
        public List<VisionStepResult> Results { get; } = new();
    }

    /// <summary>
    /// 视觉流程引擎（基于 OpenCV / OpenCvSharp 的真实图像算法实现）。
    /// 真正执行：图像采集 / 图像预处理 / 模板匹配 / 缺陷检测 / 测量 / 通讯 六类算子。
    /// 无相机 SDK、无真实模板文件时也能用「测试图」完整跑通整条流程，便于现场验证。
    /// </summary>
    public static class VisionEngine
    {
        public static VisionReport Run(IEnumerable<VisualFlowStep> steps, IProgress<string>? progress = null)
        {
            var report = new VisionReport();
            Cv.Mat? cur = null;
            bool usedSynthetic = false;
            int tplX = 0, tplY = 0, tplW = 0, tplH = 0; // 合成测试图里的“目标”矩形（用于无模板文件时自动取模板）
            var featurePts = new List<(double X, double Y, string Tag)>();

            try
            {
                foreach (var s in steps)
                {
                    if (s == null || !s.Enabled) continue;
                    switch ((s.StepType ?? "").Trim())
                    {
                        case "图像采集":
                            {
                                var next = RunAcquire(s, ref usedSynthetic, ref tplX, ref tplY, ref tplW, ref tplH, report, progress);
                                cur?.Dispose();
                                cur = next;
                                featurePts.Clear();
                                break;
                            }
                        case "图像预处理":
                            if (cur == null) { AddFail(report, s, "请先执行图像采集"); break; }
                            cur = RunPreprocess(s, cur, featurePts, report, progress);
                            break;
                        case "模板匹配":
                            if (cur == null) { AddFail(report, s, "请先执行图像采集"); break; }
                            cur = RunMatch(s, cur, usedSynthetic, tplX, tplY, tplW, tplH, featurePts, report, progress);
                            break;
                        case "缺陷检测":
                            if (cur == null) { AddFail(report, s, "请先执行图像采集"); break; }
                            cur = RunDefect(s, cur, featurePts, report, progress);
                            break;
                        case "测量":
                            cur = RunMeasure(s, cur, featurePts, report, progress);
                            break;
                        case "通讯":
                            RunComm(s, report, progress);
                            break;
                        default:
                            report.Results.Add(new VisionStepResult { StepName = s.Name, Type = s.StepType, Ok = false, Summary = "未知步骤类型，已跳过" });
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                report.Results.Add(new VisionStepResult
                {
                    StepName = "引擎",
                    Type = "异常",
                    Ok = false,
                    Summary = $"OpenCV 执行异常：{ex.Message}"
                });
            }

            if (cur != null)
            {
                report.HasImage = true;
                report.Width = cur.Width;
                report.Height = cur.Height;
                report.Bgra = MatToBgra(cur);
            }
            cur?.Dispose();
            return report;
        }

        // ============ 图像采集 ============
        private static Cv.Mat RunAcquire(VisualFlowStep s, ref bool usedSynthetic,
            ref int tplX, ref int tplY, ref int tplW, ref int tplH,
            VisionReport report, IProgress<string>? progress)
        {
            string path = (s.SavePath ?? "").Trim();
            if (File.Exists(path))
            {
                var m = ImReadBgra(path);
                if (m != null)
                {
                    usedSynthetic = false;
                    report.Results.Add(new VisionStepResult
                    {
                        StepName = s.Name,
                        Type = "图像采集",
                        Ok = true,
                        Summary = $"已采集图像 {m.Width}x{m.Height}"
                    });
                    progress?.Report($"图像采集：{m.Width}x{m.Height}");
                    return m;
                }
            }

            // 无有效文件 -> 生成测试图（含一个明矩形目标，供模板匹配/测量演示）
            int w = (int)Clamp(s.Width, 64, 4096);
            int h = (int)Clamp(s.Height, 64, 4096);
            var bytes = MakeSynthetic(w, h, out tplX, out tplY, out tplW, out tplH);
            usedSynthetic = true;
            report.Results.Add(new VisionStepResult
            {
                StepName = s.Name,
                Type = "图像采集",
                Ok = true,
                Summary = $"已生成测试图 {w}x{h}（未提供有效图像路径，自动用测试图演示）"
            });
            progress?.Report("图像采集：生成测试图");
            return BgraToMat(bytes, w, h);
        }

        // ============ 模板匹配（OpenCV matchTemplate + 角度扫描） ============
        private static Cv.Mat RunMatch(VisualFlowStep s, Cv.Mat cur, bool usedSynthetic,
            int tplX, int tplY, int tplW, int tplH, List<(double X, double Y, string Tag)> featurePts,
            VisionReport report, IProgress<string>? progress)
        {
            Cv.Mat? tpl = null;
            string tpath = (s.TemplatePath ?? "").Trim();
            if (File.Exists(tpath))
            {
                tpl = ImReadBgra(tpath);
            }
            else if (usedSynthetic && tplW > 0)
            {
                tpl = new Cv.Mat(cur, new Cv.Rect(tplX, tplY, tplW, tplH)).Clone();
            }

            if (tpl == null) { AddFail(report, s, "请设置有效模板路径（或先采集测试图）"); return cur; }

            using var sGray = new Cv.Mat();
            Cv.Cv2.CvtColor(cur, sGray, Cv.ColorConversionCodes.BGRA2GRAY);
            using var tGray = new Cv.Mat();
            Cv.Cv2.CvtColor(tpl, tGray, Cv.ColorConversionCodes.BGRA2GRAY);

            double angleRange = Clamp(s.AngleRange, 0, 360);
            var angles = BuildAngles(angleRange);
            double best = -2; int bx = 0, by = 0; double bangle = 0;
            foreach (var a in angles)
            {
                Cv.Mat rt;
                if (Math.Abs(a) < 1e-6)
                {
                    rt = tGray;
                }
                else
                {
                    using var rm = Cv.Cv2.GetRotationMatrix2D(new Cv.Point2f(tGray.Width / 2f, tGray.Height / 2f), a, 1.0);
                    rt = new Cv.Mat();
                    Cv.Cv2.WarpAffine(tGray, rt, rm, tGray.Size());
                }

                using var res = new Cv.Mat();
                Cv.Cv2.MatchTemplate(sGray, rt, res, Cv.TemplateMatchModes.CCoeffNormed);
                Cv.Cv2.MinMaxLoc(res, out _, out double maxVal, out _, out Cv.Point maxLoc);
                if (maxVal > best) { best = maxVal; bx = maxLoc.X; by = maxLoc.Y; bangle = a; }

                if (rt != tGray) rt.Dispose();
                if (best >= 0.999) break;
            }

            int tw = tpl.Width, th = tpl.Height;
            bool pass = best >= Clamp(s.ScoreThreshold, 0, 1);

            Cv.Cv2.Rectangle(cur, new Cv.Rect(bx, by, tw, th), Rgb(0, 200, 80), 2);
            Cv.Cv2.DrawMarker(cur, new Cv.Point(bx + tw / 2, by + th / 2), Rgb(0, 200, 80), Cv.MarkerTypes.Cross, 12, 2);
            featurePts.Add((bx + tw / 2.0, by + th / 2.0, "匹配"));

            tpl.Dispose();

            report.Results.Add(new VisionStepResult
            {
                StepName = s.Name,
                Type = "模板匹配",
                Ok = pass,
                Summary = pass
                    ? $"匹配成功 分数 {best:F3} @ ({bx},{by}) 角度 {bangle:F0}°"
                    : $"未达阈值（{s.ScoreThreshold:F2}）分数 {best:F3} @ ({bx},{by})"
            });
            progress?.Report($"模板匹配：分数 {best:F3}");
            return cur;
        }

        // ============ 缺陷检测（OpenCV 阈值 + 轮廓连通域） ============
        private static Cv.Mat RunDefect(VisualFlowStep s, Cv.Mat cur,
            List<(double X, double Y, string Tag)> featurePts, VisionReport report, IProgress<string>? progress)
        {
            double thr = Clamp(s.Threshold, 0, 255);
            using var g = new Cv.Mat();
            Cv.Cv2.CvtColor(cur, g, Cv.ColorConversionCodes.BGRA2GRAY);
            using var bin = new Cv.Mat();
            // 判定亮/暗缺陷：Algorithm 含“亮/bright”视为亮斑，否则默认暗斑
            bool bright = (s.Algorithm ?? "").IndexOf("亮", StringComparison.Ordinal) >= 0
                       || (s.Algorithm ?? "").IndexOf("bright", StringComparison.OrdinalIgnoreCase) >= 0;
            Cv.Cv2.Threshold(g, bin, thr, 255, bright ? Cv.ThresholdTypes.Binary : Cv.ThresholdTypes.BinaryInv);

            Cv.Cv2.FindContours(bin, out Cv.Point[][] contours, out Cv.HierarchyIndex[] hier,
                Cv.RetrievalModes.External, Cv.ContourApproximationModes.ApproxSimple);

            double minA = Clamp(s.MinArea, 1, 1e9);
            double maxA = Clamp(s.MaxArea, 1, 1e9);
            int idx = 0;
            (double X, double Y, string Tag)? largest = null;
            double largestArea = -1;
            foreach (var c in contours)
            {
                double area = Cv.Cv2.ContourArea(c);
                if (area < minA || area > maxA) continue;
                idx++;
                var r = Cv.Cv2.BoundingRect(c);
                Cv.Cv2.Rectangle(cur, r, Rgb(220, 40, 40), 2);
                var m = Cv.Cv2.Moments(c);
                double cx = m.M00 != 0 ? m.M10 / m.M00 : r.X + r.Width / 2.0;
                double cy = m.M00 != 0 ? m.M01 / m.M00 : r.Y + r.Height / 2.0;
                if (area > largestArea) { largestArea = area; largest = (cx, cy, "缺陷" + idx); }
            }
            if (largest.HasValue) featurePts.Add(largest.Value);

            report.Results.Add(new VisionStepResult
            {
                StepName = s.Name,
                Type = "缺陷检测",
                Ok = true,
                Summary = $"检出 {idx} 处缺陷（面积阈值 {minA:0}~{maxA:0}）"
            });
            progress?.Report($"缺陷检测：{idx} 处");
            return cur;
        }

        // ============ 测量（两特征点像素距离 x 标定系数） ============
        private static Cv.Mat? RunMeasure(VisualFlowStep s, Cv.Mat? cur,
            List<(double X, double Y, string Tag)> featurePts, VisionReport report, IProgress<string>? progress)
        {
            if (cur == null) { AddFail(report, s, "请先执行图像采集"); return cur; }

            var pts = new List<(double X, double Y, string Tag)>(featurePts);
            if (pts.Count < 2)
            {
                double cx = cur.Width / 2.0, cy = cur.Height / 2.0;
                while (pts.Count < 2) pts.Add((cx, cy, "中心"));
            }
            var a = pts[pts.Count - 2];
            var b = pts[pts.Count - 1];
            double dx = a.X - b.X, dy = a.Y - b.Y;
            double px = Math.Sqrt(dx * dx + dy * dy);
            double cal = Clamp(s.Calibration, 1e-6, 1e9);
            double len = px * cal;

            Cv.Cv2.Line(cur, new Cv.Point((int)a.X, (int)a.Y), new Cv.Point((int)b.X, (int)b.Y), Rgb(40, 120, 240), 2);
            Cv.Cv2.DrawMarker(cur, new Cv.Point((int)a.X, (int)a.Y), Rgb(40, 120, 240), Cv.MarkerTypes.Cross, 10, 2);
            Cv.Cv2.DrawMarker(cur, new Cv.Point((int)b.X, (int)b.Y), Rgb(40, 120, 240), Cv.MarkerTypes.Cross, 10, 2);

            report.Results.Add(new VisionStepResult
            {
                StepName = s.Name,
                Type = "测量",
                Ok = true,
                Summary = $"{a.Tag}->{b.Tag} 距离 {len:F2} {s.Unit}（{px:F1}px x 标定 {cal}）"
            });
            progress?.Report($"测量：{len:F2} {s.Unit}");
            return cur;
        }

        // ============ 图像预处理（OpenCV 原语） ============
        private static Cv.Mat RunPreprocess(VisualFlowStep s, Cv.Mat cur,
            List<(double X, double Y, string Tag)> featurePts, VisionReport report, IProgress<string>? progress)
        {
            string op = (s.PreOp ?? "").Trim();
            if (op.Length == 0 || op == "无")
            {
                report.Results.Add(new VisionStepResult { StepName = s.Name, Type = "图像预处理", Ok = true, Summary = "无操作" });
                return cur;
            }

            int k1 = Math.Max(1, (int)Math.Round(s.PreParam2)); // 核大小（默认 3）
            if ((k1 & 1) == 0) k1++;                            // 强制奇数
            double thr = Clamp(s.PreParam1, 0, 255);

            try
            {
                switch (op)
                {
                    case "灰度化":
                        {
                            var g = new Cv.Mat(); Cv.Cv2.CvtColor(cur, g, Cv.ColorConversionCodes.BGRA2GRAY);
                            var o = new Cv.Mat(); Cv.Cv2.CvtColor(g, o, Cv.ColorConversionCodes.GRAY2BGRA);
                            g.Dispose(); cur.Dispose();
                            return FinalizePre(o, s, "灰度化", report, progress);
                        }
                    case "二值化":
                        {
                            var g = new Cv.Mat(); Cv.Cv2.CvtColor(cur, g, Cv.ColorConversionCodes.BGRA2GRAY);
                            var t = new Cv.Mat(); Cv.Cv2.Threshold(g, t, thr, 255, Cv.ThresholdTypes.Binary);
                            var o = new Cv.Mat(); Cv.Cv2.CvtColor(t, o, Cv.ColorConversionCodes.GRAY2BGRA);
                            g.Dispose(); t.Dispose(); cur.Dispose();
                            return FinalizePre(o, s, "二值化", report, progress);
                        }
                    case "高斯平滑":
                        {
                            var g = new Cv.Mat(); Cv.Cv2.CvtColor(cur, g, Cv.ColorConversionCodes.BGRA2GRAY);
                            var b = new Cv.Mat(); Cv.Cv2.GaussianBlur(g, b, new Cv.Size(k1, k1), 0);
                            var o = new Cv.Mat(); Cv.Cv2.CvtColor(b, o, Cv.ColorConversionCodes.GRAY2BGRA);
                            g.Dispose(); b.Dispose(); cur.Dispose();
                            return FinalizePre(o, s, "高斯平滑", report, progress);
                        }
                    case "中值滤波":
                        {
                            var g = new Cv.Mat(); Cv.Cv2.CvtColor(cur, g, Cv.ColorConversionCodes.BGRA2GRAY);
                            var b = new Cv.Mat(); Cv.Cv2.MedianBlur(g, b, k1);
                            var o = new Cv.Mat(); Cv.Cv2.CvtColor(b, o, Cv.ColorConversionCodes.GRAY2BGRA);
                            g.Dispose(); b.Dispose(); cur.Dispose();
                            return FinalizePre(o, s, "中值滤波", report, progress);
                        }
                    case "腐蚀":
                        {
                            var g = new Cv.Mat(); Cv.Cv2.CvtColor(cur, g, Cv.ColorConversionCodes.BGRA2GRAY);
                            var kernel = Cv.Cv2.GetStructuringElement(Cv.MorphShapes.Rect, new Cv.Size(k1, k1));
                            var b = new Cv.Mat(); Cv.Cv2.Erode(g, b, kernel);
                            var o = new Cv.Mat(); Cv.Cv2.CvtColor(b, o, Cv.ColorConversionCodes.GRAY2BGRA);
                            g.Dispose(); kernel.Dispose(); b.Dispose(); cur.Dispose();
                            return FinalizePre(o, s, "腐蚀", report, progress);
                        }
                    case "膨胀":
                        {
                            var g = new Cv.Mat(); Cv.Cv2.CvtColor(cur, g, Cv.ColorConversionCodes.BGRA2GRAY);
                            var kernel = Cv.Cv2.GetStructuringElement(Cv.MorphShapes.Rect, new Cv.Size(k1, k1));
                            var b = new Cv.Mat(); Cv.Cv2.Dilate(g, b, kernel);
                            var o = new Cv.Mat(); Cv.Cv2.CvtColor(b, o, Cv.ColorConversionCodes.GRAY2BGRA);
                            g.Dispose(); kernel.Dispose(); b.Dispose(); cur.Dispose();
                            return FinalizePre(o, s, "膨胀", report, progress);
                        }
                    case "开运算":
                        {
                            var g = new Cv.Mat(); Cv.Cv2.CvtColor(cur, g, Cv.ColorConversionCodes.BGRA2GRAY);
                            var kernel = Cv.Cv2.GetStructuringElement(Cv.MorphShapes.Rect, new Cv.Size(k1, k1));
                            var e = new Cv.Mat(); Cv.Cv2.Erode(g, e, kernel);
                            var d = new Cv.Mat(); Cv.Cv2.Dilate(e, d, kernel);
                            var o = new Cv.Mat(); Cv.Cv2.CvtColor(d, o, Cv.ColorConversionCodes.GRAY2BGRA);
                            g.Dispose(); kernel.Dispose(); e.Dispose(); d.Dispose(); cur.Dispose();
                            return FinalizePre(o, s, "开运算", report, progress);
                        }
                    case "闭运算":
                        {
                            var g = new Cv.Mat(); Cv.Cv2.CvtColor(cur, g, Cv.ColorConversionCodes.BGRA2GRAY);
                            var kernel = Cv.Cv2.GetStructuringElement(Cv.MorphShapes.Rect, new Cv.Size(k1, k1));
                            var d = new Cv.Mat(); Cv.Cv2.Dilate(g, d, kernel);
                            var e = new Cv.Mat(); Cv.Cv2.Erode(d, e, kernel);
                            var o = new Cv.Mat(); Cv.Cv2.CvtColor(e, o, Cv.ColorConversionCodes.GRAY2BGRA);
                            g.Dispose(); kernel.Dispose(); d.Dispose(); e.Dispose(); cur.Dispose();
                            return FinalizePre(o, s, "闭运算", report, progress);
                        }
                    case "Sobel边缘":
                        {
                            var g = new Cv.Mat(); Cv.Cv2.CvtColor(cur, g, Cv.ColorConversionCodes.BGRA2GRAY);
                            var sx = new Cv.Mat(); Cv.Cv2.Sobel(g, sx, Cv.MatType.CV_16S, 1, 1, 3);
                            var abs = new Cv.Mat(); Cv.Cv2.ConvertScaleAbs(sx, abs);
                            var o = new Cv.Mat(); Cv.Cv2.CvtColor(abs, o, Cv.ColorConversionCodes.GRAY2BGRA);
                            g.Dispose(); sx.Dispose(); abs.Dispose(); cur.Dispose();
                            return FinalizePre(o, s, "Sobel边缘", report, progress);
                        }
                    case "直方图均衡":
                        {
                            var g = new Cv.Mat(); Cv.Cv2.CvtColor(cur, g, Cv.ColorConversionCodes.BGRA2GRAY);
                            var eq = new Cv.Mat(); Cv.Cv2.EqualizeHist(g, eq);
                            var o = new Cv.Mat(); Cv.Cv2.CvtColor(eq, o, Cv.ColorConversionCodes.GRAY2BGRA);
                            g.Dispose(); eq.Dispose(); cur.Dispose();
                            return FinalizePre(o, s, "直方图均衡", report, progress);
                        }
                    case "ROI裁剪":
                        {
                            if (!TryParseRoi(s.PreRoi, cur.Width, cur.Height, out int rx, out int ry, out int rw, out int rh))
                            { AddFail(report, s, "ROI 格式错误（应为 x,y,w,h）"); return cur; }
                            var crop = new Cv.Mat(cur, new Cv.Rect(rx, ry, rw, rh)).Clone();
                            cur.Dispose();
                            featurePts.Clear(); // ROI 后原坐标失效
                            report.Results.Add(new VisionStepResult { StepName = s.Name, Type = "图像预处理", Ok = true, Summary = $"ROI 裁剪 {rw}x{rh} @ ({rx},{ry})" });
                            progress?.Report($"ROI 裁剪：{rw}x{rh}");
                            return crop;
                        }
                    case "图像加":
                    case "图像减":
                    case "图像与":
                    case "图像或":
                        {
                            Cv.Mat? b2 = null;
                            string p2 = (s.PreImage2Path ?? "").Trim();
                            if (!File.Exists(p2) || (b2 = ImReadBgra(p2)) == null)
                            { AddFail(report, s, "第二张图加载失败（检查路径）"); return cur; }
                            if (b2.Width != cur.Width || b2.Height != cur.Height)
                            { b2.Dispose(); AddFail(report, s, $"第二张图尺寸 {b2.Width}x{b2.Height} 与当前 {cur.Width}x{cur.Height} 不一致"); return cur; }
                            Cv.Mat r = new Cv.Mat(cur.Rows, cur.Cols, Cv.MatType.CV_8UC4);
                            if (op == "图像加") Cv.Cv2.Add(cur, b2, r);
                            else if (op == "图像减") Cv.Cv2.Subtract(cur, b2, r);
                            else if (op == "图像与") Cv.Cv2.BitwiseAnd(cur, b2, r);
                            else Cv.Cv2.BitwiseOr(cur, b2, r);
                            b2.Dispose(); cur.Dispose();
                            return FinalizePre(r, s, op, report, progress);
                        }
                    default:
                        AddFail(report, s, $"未知预处理操作：{op}");
                        return cur;
                }
            }
            catch (Exception ex)
            {
                AddFail(report, s, $"{op} 失败：{ex.Message}");
                return cur;
            }
        }

        // 预处理成功收尾：登记结果并返回新 Mat
        private static Cv.Mat FinalizePre(Cv.Mat o, VisualFlowStep s, string op, VisionReport report, IProgress<string>? progress)
        {
            report.Results.Add(new VisionStepResult { StepName = s.Name, Type = "图像预处理", Ok = true, Summary = $"已执行 {op}" });
            progress?.Report($"预处理：{op}");
            return o;
        }

        // ============ 通讯（经 HardwareBridge 真实发送） ============
        private static void RunComm(VisualFlowStep s, VisionReport report, IProgress<string>? progress)
        {
            string target = (s.Target ?? "").Trim();
            string content = s.Content ?? "";
            var comm = HardwareResolver.ResolveComm(target);
            if (comm == null)
            {
                AddFail(report, s, $"未找到通讯配置：{target}");
                return;
            }
            try
            {
                HardwareBridge.Current?.CommSend(comm, content);
                report.Results.Add(new VisionStepResult
                {
                    StepName = s.Name,
                    Type = "通讯",
                    Ok = true,
                    Summary = $"已发送 -> {target}：{content}"
                });
                progress?.Report($"通讯：已发送 -> {target}");
            }
            catch (Exception ex)
            {
                report.Results.Add(new VisionStepResult
                {
                    StepName = s.Name,
                    Type = "通讯",
                    Ok = false,
                    Summary = $"发送失败：{ex.Message}"
                });
            }
        }

        // ===================== 基础工具 =====================

        private static void AddFail(VisionReport report, VisualFlowStep s, string msg)
            => report.Results.Add(new VisionStepResult { StepName = s.Name, Type = s.StepType, Ok = false, Summary = msg });

        private static double Clamp(double v, double lo, double hi)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return lo;
            return v < lo ? lo : v > hi ? hi : v;
        }

        // OpenCV (B,G,R) 顺序的 Scalar（输入仍用直观的 R,G,B）
        private static Cv.Scalar Rgb(int r, int g, int b) => new Cv.Scalar(b, g, r);

        // BGRA 字节缓冲 <-> OpenCV Mat 互转
        private static Cv.Mat BgraToMat(byte[] bgra, int w, int h)
        {
            var m = new Cv.Mat(h, w, Cv.MatType.CV_8UC4);
            Marshal.Copy(bgra, 0, m.Data, bgra.Length);
            return m;
        }

        private static byte[] MatToBgra(Cv.Mat m)
        {
            Cv.Mat? tmp = (m.Channels() == 4) ? null : EnsureBgra(m);
            var src = tmp ?? m;
            var bytes = new byte[(int)(src.Total() * src.Channels())];
            Marshal.Copy(src.Data, bytes, 0, bytes.Length);
            tmp?.Dispose();
            return bytes;
        }

        private static Cv.Mat EnsureBgra(Cv.Mat m)
        {
            var o = new Cv.Mat();
            if (m.Channels() == 1) Cv.Cv2.CvtColor(m, o, Cv.ColorConversionCodes.GRAY2BGRA);
            else Cv.Cv2.CvtColor(m, o, Cv.ColorConversionCodes.BGR2BGRA);
            return o;
        }

        private static Cv.Mat? ImReadBgra(string path)
        {
            if (!File.Exists(path)) return null;
            var m = Cv.Cv2.ImRead(path, Cv.ImreadModes.Unchanged);
            if (m.Empty()) return null;
            var outp = EnsureBgra(m);
            m.Dispose();
            return outp;
        }

        private static List<double> BuildAngles(double angleRange)
        {
            var list = new List<double>();
            if (angleRange <= 0) { list.Add(0); return list; }
            int n = Math.Max(1, Math.Min(8, (int)Math.Round(angleRange / 45.0)));
            for (int i = 0; i <= n; i++) list.Add(angleRange * i / n);
            return list;
        }

        private static bool TryParseRoi(string s, int iw, int ih, out int x, out int y, out int w, out int h)
        {
            x = y = w = h = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            var parts = s.Split(new[] { ',', ' ', '，' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4) return false;
            if (!int.TryParse(parts[0], out x) || !int.TryParse(parts[1], out y)
                || !int.TryParse(parts[2], out w) || !int.TryParse(parts[3], out h)) return false;
            if (w <= 0 || h <= 0) return false;
            x = Math.Max(0, Math.Min(iw - 1, x));
            y = Math.Max(0, Math.Min(ih - 1, y));
            w = Math.Max(1, Math.Min(iw - x, w));
            h = Math.Max(1, Math.Min(ih - y, h));
            return true;
        }

        /// <summary>生成测试图：渐变背景 + 暗圆（缺陷候选） + 亮矩形（目标，记录其位置供自动取模板）。返回 BGRA 字节。</summary>
        private static byte[] MakeSynthetic(int w, int h, out int tplX, out int tplY, out int tplW, out int tplH)
        {
            byte[] buf = new byte[w * h * 4];
            var rnd = new Random(12345);
            int tx = w * 3 / 5, ty = h * 2 / 5, tw = Math.Max(24, w / 6), th = Math.Max(24, h / 6);
            tplX = tx; tplY = ty; tplW = tw; tplH = th;
            int cx = w / 3, cy = h * 2 / 3, rad = Math.Max(10, Math.Min(w, h) / 12);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = (y * w + x) * 4;
                    double g = 150 + 60 * Math.Sin(x * 0.02) * Math.Cos(y * 0.02);
                    double d = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d < rad) g -= 90;
                    if (x >= tx && x < tx + tw && y >= ty && y < ty + th) g += 70;
                    g = Math.Max(0, Math.Min(255, g + (rnd.NextDouble() - 0.5) * 12));
                    byte v = (byte)g;
                    buf[i] = v; buf[i + 1] = v; buf[i + 2] = v; buf[i + 3] = 255;
                }
            }
            return buf;
        }
    }
}
// === NoCodeMotion 视觉流程引擎（OpenCV 实现） | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
