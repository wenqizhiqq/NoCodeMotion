// === NoCodeMotion 视觉流程引擎（OpenCV 实现） | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Cv = OpenCvSharp;
using GrayMatch;
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
/// 模板匹配结果（页面在结果图上叠加显示「相似度/精度/位置/角度」并画框用）。
/// </summary>
    public sealed class MatchOutcome
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public double Score { get; set; }
        public double Angle { get; set; }
        public double Threshold { get; set; }
        public bool Pass { get; set; }
        public string Mode { get; set; } = "";
        public string Template { get; set; } = "";

        /// <summary>相似度 0~1。</summary>
        public double Similarity => Score;

        /// <summary>精度 0~100%（= 相似度 × 100）。</summary>
        public double PrecisionPercent => Score * 100.0;
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

        /// <summary>最近的「模板匹配」步骤结果（用于 UI 叠加绿框/红框 + 文本）。无匹配步则为 null。</summary>
        public MatchOutcome? Match { get; set; }

        /// <summary>
        /// 模板匹配返回的全部 top-N 框（像素坐标，含角度/相似度/通过标志）。
        /// 结果图上由 WPF 矢量叠加层画旋转矩形（参考 GrayMatch.Wpf），
        /// 不在引擎里烧轴对齐框，避免 angle≠0 时框方向错。
        /// </summary>
        public List<MatchBox> Matches { get; } = new();
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
            Cv.Mat? display = null;   // 注释画布：累积各步的框/线/十字，与数据图 cur 分离，避免标注被后续算子当作像素内容（否则匹配绿框灰度≈126 会触发误检）
            bool usedSynthetic = false;
            int tplX = 0, tplY = 0, tplW = 0, tplH = 0; // 合成测试图里的“目标”矩形（用于无模板文件时自动取模板）
            var featurePts = new List<(double X, double Y, string Tag)>();

            // 运行前清空每步上次结果（steps 与主流程 Steps 是同批引用，直接回写对象）
            foreach (var s in steps)
            {
                if (s == null) continue;
                s.DurationMs = 0;
                s.LastOk = false;
                s.LastResult = "";
            }

            try
            {
                foreach (var s in steps)
                {
                    if (s == null) continue;
                    if (!s.Enabled)
                    {
                        // 未运行：保持重置后的空结果，列表显示中性“–”
                        progress?.Report($"跳过（已禁用）：{s.Name}");
                        continue;
                    }
                    var sw = Stopwatch.StartNew();
                    int before = report.Results.Count;
                    try
                    {
                        switch ((s.StepType ?? "").Trim())
                        {
                            case "图像采集":
                                {
                                    var next = RunAcquire(s, ref usedSynthetic, ref tplX, ref tplY, ref tplW, ref tplH, report, progress);
                                    cur?.Dispose();
                                    cur = next;
                                    display?.Dispose();
                                    display = cur.Clone();   // 采集后建立与 cur 同步的注释画布
                                    featurePts.Clear();
                                    break;
                                }
                            case "图像预处理":
                                if (cur == null) { AddFail(report, s, "请先执行图像采集"); break; }
                                cur = RunPreprocess(s, cur, featurePts, report, progress);
                                display?.Dispose();
                                display = cur.Clone();   // 数据图更换后，注释画布也同步换底（保留已画的标注）
                                break;
                            case "模板匹配":
                                if (cur == null) { AddFail(report, s, "请先执行图像采集"); break; }
                                cur = RunMatch(s, cur, usedSynthetic, tplX, tplY, tplW, tplH, featurePts, report, progress, display);
                                break;
                            case "缺陷检测":
                                if (cur == null) { AddFail(report, s, "请先执行图像采集"); break; }
                                cur = RunDefect(s, cur, featurePts, report, progress, display);
                                break;
                            case "测量":
                                cur = RunMeasure(s, cur, featurePts, report, progress, display);
                                break;
                            case "通讯":
                                RunComm(s, report, progress);
                                break;
                            default:
                                report.Results.Add(new VisionStepResult { StepName = s.Name, Type = s.StepType, Ok = false, Summary = "未知步骤类型，已跳过" });
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        AddFail(report, s, $"执行异常：{ex.Message}");
                    }
                    sw.Stop();
                    s.DurationMs = sw.Elapsed.TotalMilliseconds;
                    if (report.Results.Count > before)
                    {
                        var r = report.Results[report.Results.Count - 1];
                        s.LastOk = r.Ok;
                        s.LastResult = r.Summary;
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

            var img = display ?? cur;
            if (img != null)
            {
                report.HasImage = true;
                report.Width = img.Width;
                report.Height = img.Height;
                report.Bgra = MatToBgra(img);
            }
            display?.Dispose();
            cur?.Dispose();
            return report;
        }

        // ============ 图像采集 ============
        private static Cv.Mat RunAcquire(VisualFlowStep s, ref bool usedSynthetic,
            ref int tplX, ref int tplY, ref int tplW, ref int tplH,
            VisionReport report, IProgress<string>? progress)
        {
            string src = (s.SourceType ?? "文件").Trim();
            if (src == "相机")
            {
                if (int.TryParse((s.CameraId ?? "0").Trim(), out int camIdx))
                {
                    try
                    {
                        using var cap = new Cv.VideoCapture(camIdx);
                        // 注意：OpenCvSharp4 的 VideoCapture.IsOpened 是静态方法，不能经实例调用。
                        // 这里直接尝试 Read，失败或空帧由 catch / 后续回退处理。
                        var frame = new Cv.Mat();
                        if (cap.Read(frame) && !frame.Empty())
                        {
                            usedSynthetic = false;
                            report.Results.Add(new VisionStepResult
                            {
                                StepName = s.Name,
                                Type = "图像采集",
                                Ok = true,
                                Summary = $"相机 {camIdx} 采集 {frame.Width}x{frame.Height}"
                            });
                            progress?.Report($"图像采集：相机 {camIdx}");
                            return EnsureBgra(frame);
                        }
                        frame.Dispose();
                    }
                    catch (Exception ex)
                    {
                        report.Results.Add(new VisionStepResult
                        {
                            StepName = s.Name, Type = "图像采集", Ok = false,
                            Summary = $"相机 {camIdx} 采集失败：{ex.Message}（回退测试图）"
                        });
                    }
                }
                else
                {
                    report.Results.Add(new VisionStepResult
                    {
                        StepName = s.Name, Type = "图像采集", Ok = false,
                        Summary = $"相机编号无效：{s.CameraId}（回退测试图）"
                    });
                }
                return SyntheticFallback(s, ref usedSynthetic, ref tplX, ref tplY, ref tplW, ref tplH, report, progress, "相机不可用");
            }

            if (src == "文件夹")
            {
                string folder = (s.FolderPath ?? "").Trim();
                if (Directory.Exists(folder))
                {
                    foreach (var ext in new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.tif", "*.tiff" })
                    {
                        var files = Directory.GetFiles(folder, ext, SearchOption.TopDirectoryOnly);
                        if (files.Length > 0)
                        {
                            var m = ImReadBgra(files[0]);
                            if (m != null)
                            {
                                usedSynthetic = false;
                                report.Results.Add(new VisionStepResult
                                {
                                    StepName = s.Name, Type = "图像采集", Ok = true,
                                    Summary = $"文件夹首图 {Path.GetFileName(files[0])} {m.Width}x{m.Height}"
                                });
                                progress?.Report("图像采集：文件夹首图");
                                return m;
                            }
                        }
                    }
                }
                return SyntheticFallback(s, ref usedSynthetic, ref tplX, ref tplY, ref tplW, ref tplH, report, progress, "文件夹无可用图像");
            }

            // 文件（默认）
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
            return SyntheticFallback(s, ref usedSynthetic, ref tplX, ref tplY, ref tplW, ref tplH, report, progress, "未提供有效图像路径");
        }

        // 无可用来源时生成测试图（含明矩形目标，供模板匹配/测量演示），并回写 usedSynthetic / 目标矩形
        private static Cv.Mat SyntheticFallback(VisualFlowStep s, ref bool usedSynthetic,
            ref int tplX, ref int tplY, ref int tplW, ref int tplH, VisionReport report, IProgress<string>? progress, string note)
        {
            int w = (int)Clamp(s.Width, 64, 4096);
            int h = (int)Clamp(s.Height, 64, 4096);
            var bytes = MakeSynthetic(w, h, out tplX, out tplY, out tplW, out tplH);
            usedSynthetic = true;
            report.Results.Add(new VisionStepResult
            {
                StepName = s.Name, Type = "图像采集", Ok = true,
                Summary = $"已生成测试图 {w}x{h}（{note}）"
            });
            progress?.Report("图像采集：生成测试图");
            return BgraToMat(bytes, w, h);
        }

        // ============ 模板匹配（OpenCV matchTemplate + 角度扫描） ============
        private static Cv.Mat RunMatch(VisualFlowStep s, Cv.Mat cur, bool usedSynthetic,
            int tplX, int tplY, int tplW, int tplH, List<(double X, double Y, string Tag)> featurePts,
            VisionReport report, IProgress<string>? progress, Cv.Mat? display = null)
        {
            Cv.Mat? tpl = null;
            string tsrc = "";
            // ① 优先用"画框确定的模板区域"（用户在结果图上拖拽的 ROI，原图像素坐标）
            if (s.TemplateRoiW > 0 && s.TemplateRoiH > 0)
            {
                int rx = (int)Clamp(s.TemplateRoiX, 0, Math.Max(0, cur.Width - 1));
                int ry = (int)Clamp(s.TemplateRoiY, 0, Math.Max(0, cur.Height - 1));
                int rw = (int)Clamp(s.TemplateRoiW, 1, Math.Max(1, cur.Width - rx));
                int rh = (int)Clamp(s.TemplateRoiH, 1, Math.Max(1, cur.Height - ry));
                tpl = new Cv.Mat(cur, new Cv.Rect(rx, ry, rw, rh)).Clone();
                tsrc = $"框选区 ({rx},{ry}) {rw}×{rh}";
            }
            // ② 其次用模板文件
            if (tpl == null)
            {
                string tpath = (s.TemplatePath ?? "").Trim();
                if (File.Exists(tpath))
                {
                    tpl = ImReadBgra(tpath);
                    tsrc = "模板文件";
                }
            }
            // ③ 最后：合成测试图时自动从图中取已知目标矩形
            if (tpl == null && usedSynthetic && tplW > 0)
            {
                tpl = new Cv.Mat(cur, new Cv.Rect(tplX, tplY, tplW, tplH)).Clone();
                tsrc = "测试图自动取模板";
            }
            if (tpl == null) { AddFail(report, s, "请先框选模板区域（或设置有效模板路径）"); return cur; }

            // ===== 改用 GrayMatch.Wpf 的旋转不变 NCC 匹配核心（RotatedTemplateMatcher） =====
            // 源图与模板都转单通道灰度喂给 native NCC；轮廓匹配模式复用 UseContour 开关。
            string mode = (s.MatchMode ?? "灰度匹配").Trim();
            using var sGray = new Cv.Mat();
            Cv.Cv2.CvtColor(cur, sGray, Cv.ColorConversionCodes.BGRA2GRAY);
            using var tGray = new Cv.Mat();
            Cv.Cv2.CvtColor(tpl, tGray, Cv.ColorConversionCodes.BGRA2GRAY);

            double best = -2; int bx = 0, by = 0; double bangle = 0;
            int tw = tpl.Width, th = tpl.Height;
            {
                using var matcher = new RotatedTemplateMatcher();
                matcher.SetSource(sGray);
                matcher.SetTemplate(tGray);
                // 轮廓匹配模式：用边缘梯度图代替灰度，对光照/前景背景灰度接近更鲁棒
                matcher.UseContour = (mode == "轮廓匹配");

                double angleRange = Clamp(s.AngleRange, 0, 360);
                // 角度步长智能选：范围≤30°（短旋转 / 水平对齐）时 1° 精扫；
                // 范围 >30° 时用 5° 粗扫提速 5 倍（GrayMatch 的 0.35× 精修步 3° 会兜住）。
                // 范围≤0 时退化为 0°（仅原角度）。
                double angleStep = angleRange <= 0 ? 0.0
                                 : angleRange <= 30 ? 1.0
                                 : 5.0;
                // topN = 12：图里通常有十几个目标，全部显示；搜索时间与 topN 几乎无关
                // （GrayMatch 走传统两遍 / 全图扫描，NMS 后只保留前 N 个）
                var results = matcher.Match(
                    pyramidLevels: 0,            // 0 = 传统两遍全分辨率（稳健，不踩金字塔分支漏检 bug）
                    angleStart: 0,
                    angleEnd: angleRange,
                    angleStep: angleStep,
                    nccThreshold: Clamp(s.ScoreThreshold, 0, 1),
                    maxOverlap: 0.3,
                    topN: 12,                    // 视觉流程默认 12：让多目标场景全部框出
                    denseMode: 0);

                double thr = Clamp(s.ScoreThreshold, 0, 1);
                // 收全部 top-N 结果到 report.Matches（供 WPF 叠加层画旋转框；不再只取 [0]）
                foreach (var r in results)
                {
                    report.Matches.Add(new MatchBox
                    {
                        LeftTopX = r.LeftTopX,
                        LeftTopY = r.LeftTopY,
                        TemplateWidth = r.TemplateWidth,
                        TemplateHeight = r.TemplateHeight,
                        Angle = r.Angle,
                        Score = r.Score,
                        Scale = r.Scale,
                        Pass = r.Score >= thr
                    });
                }
                if (results.Count > 0)
                {
                    var r0 = results[0];         // 已按 Score 降序
                    best = r0.Score;
                    bx = r0.LeftTopX;
                    by = r0.LeftTopY;
                    bangle = r0.Angle;
                    tw = r0.TemplateWidth;
                    th = r0.TemplateHeight;
                }
            }

            bool pass = best >= Clamp(s.ScoreThreshold, 0, 1);

            // 不再在 Mat 上烧轴对齐框 — 结果图上的旋转绿/红框由 WPF 叠加层
            // （ItemsControl + Canvas + Rectangle + RotateTransform）按 angle 精确绘制，
            // 参考 GrayMatch.Wpf 的渲染方案，angle≠0 时方向正确。只把最佳中心加入
            // featurePts 给后续「测量」步使用。
            if (tw > 0 && th > 0)
            {
                featurePts.Add((bx + tw / 2.0, by + th / 2.0, "匹配"));
            }

            // 结构化结果供页面叠加相似度/精度/位置/角度
            report.Match = new MatchOutcome
            {
                X = bx, Y = by, W = tw, H = th,
                Score = best, Angle = bangle,
                Pass = pass,
                Threshold = Clamp(s.ScoreThreshold, 0, 1),
                Mode = mode,
                Template = tsrc
            };

            tpl.Dispose();

            report.Results.Add(new VisionStepResult
            {
                StepName = s.Name,
                Type = "模板匹配",
                Ok = pass,
                Summary = pass
                    ? $"[{mode}] 匹配成功 分数 {best:F3} @ ({bx},{by}) 角度 {bangle:F0}°　模板={tsrc}"
                    : $"[{mode}] 未达阈值（{s.ScoreThreshold:F2}）分数 {best:F3} @ ({bx},{by})　模板={tsrc}"
            });
            progress?.Report($"模板匹配：分数 {best:F3}");
            return cur;
        }

        // ============ 缺陷检测（OpenCV 阈值 + 轮廓连通域） ============
        private static Cv.Mat RunDefect(VisualFlowStep s, Cv.Mat cur,
            List<(double X, double Y, string Tag)> featurePts, VisionReport report, IProgress<string>? progress, Cv.Mat? display = null)
        {
            double thr = Clamp(s.Threshold, 0, 255);
            using var g = new Cv.Mat();
            Cv.Cv2.CvtColor(cur, g, Cv.ColorConversionCodes.BGRA2GRAY);
            using var bin = new Cv.Mat();
            string dmode = (s.DetectMode ?? "阈值面积").Trim();
            if (dmode == "边缘轮廓")
            {
                Cv.Cv2.Canny(g, bin, thr, Math.Min(255.0, thr + 60.0));
            }
            else
            {
                // 判定亮/暗缺陷：Algorithm 含“亮/bright”视为亮斑，否则默认暗斑
                bool bright = (s.Algorithm ?? "").IndexOf("亮", StringComparison.Ordinal) >= 0
                           || (s.Algorithm ?? "").IndexOf("bright", StringComparison.OrdinalIgnoreCase) >= 0;
                Cv.Cv2.Threshold(g, bin, thr, 255, bright ? Cv.ThresholdTypes.Binary : Cv.ThresholdTypes.BinaryInv);
            }

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
                var dst = display ?? cur;
                Cv.Cv2.Rectangle(dst, r, Rgb(220, 40, 40), 2);
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
                Summary = $"[{dmode}] 检出 {idx} 处缺陷（面积阈值 {minA:0}~{maxA:0}）"
            });
            progress?.Report($"缺陷检测：{idx} 处");
            return cur;
        }

        // ============ 测量（两特征点像素距离 x 标定系数） ============
        private static Cv.Mat? RunMeasure(VisualFlowStep s, Cv.Mat? cur,
            List<(double X, double Y, string Tag)> featurePts, VisionReport report, IProgress<string>? progress, Cv.Mat? display = null)
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

            var dst = display ?? cur;
            Cv.Cv2.Line(dst, new Cv.Point((int)a.X, (int)a.Y), new Cv.Point((int)b.X, (int)b.Y), Rgb(40, 120, 240), 2);
            Cv.Cv2.DrawMarker(dst, new Cv.Point((int)a.X, (int)a.Y), Rgb(40, 120, 240), Cv.MarkerTypes.Cross, 10, 2);
            Cv.Cv2.DrawMarker(dst, new Cv.Point((int)b.X, (int)b.Y), Rgb(40, 120, 240), Cv.MarkerTypes.Cross, 10, 2);

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

        /// <summary>
        /// 生成测试图：平缓渐变背景 + 暗圆（暗斑缺陷） + 亮矩形（模板匹配目标，记录其位置供自动取模板）
        /// + 小亮斑（供「亮」缺陷模式演示）。返回 BGRA 字节。
        /// 注意：背景渐变幅度必须保持在 ±15，使背景灰度落在约 135~165，不会跨越默认阈值 128。
        /// 实测若用 ±60，背景暗区（占比 24%）会被缺陷检测整片误判，默认一跑就报「检出 51 处缺陷」。
        /// </summary>
        private static byte[] MakeSynthetic(int w, int h, out int tplX, out int tplY, out int tplW, out int tplH)
        {
            byte[] buf = new byte[w * h * 4];
            var rnd = new Random(12345);
            int tx = w * 3 / 5, ty = h * 2 / 5, tw = Math.Max(24, w / 6), th = Math.Max(24, h / 6);
            tplX = tx; tplY = ty; tplW = tw; tplH = th;
            int cx = w / 3, cy = h * 2 / 3, rad = Math.Max(10, Math.Min(w, h) / 12);
            // 小亮斑：供「亮」缺陷模式演示（半径取暗斑一半）
            int sx = w * 4 / 5, sy = h * 3 / 4, srad = Math.Max(6, rad / 2);

            // sin 只依赖 x、cos 只依赖 y：提到循环外预计算，避免逐像素重复三角函数调用
            var sinX = new double[w];
            for (int x = 0; x < w; x++) sinX[x] = Math.Sin(x * 0.02);
            var cosY = new double[h];
            for (int y = 0; y < h; y++) cosY[y] = Math.Cos(y * 0.02);

            for (int y = 0; y < h; y++)
            {
                double dy = y - cy, dy2 = (y - sy) * (y - sy);
                bool inRowTpl = y >= ty && y < ty + th;
                for (int x = 0; x < w; x++)
                {
                    int i = (y * w + x) * 4;
                    double g = 150 + 15 * sinX[x] * cosY[y];
                    double dx = x - cx;
                    if (dx * dx + dy * dy < (double)rad * rad) g -= 90;              // 暗斑（默认阈值下的示范缺陷）
                    if (inRowTpl && x >= tx && x < tx + tw) g += 70;                 // 亮矩形（模板匹配目标）
                    double dx2 = x - sx;
                    if (dx2 * dx2 + dy2 < (double)srad * srad) g += 90;              // 亮斑（供「亮」模式演示）
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
