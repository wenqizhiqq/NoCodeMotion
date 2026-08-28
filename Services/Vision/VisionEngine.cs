// === NoCodeMotion 视觉流程引擎 | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
    /// 纯 C# 视觉流程引擎（不依赖任何第三方成像库，仅用 WPF 自带的 WriteableBitmap/BitmapImage）。
    /// 真正执行：图像采集 / 模板匹配 / 缺陷检测 / 测量 / 通讯 五类算子。
    /// 无相机 SDK、无真实模板文件时也能用「测试图」完整跑通整条流程，便于现场验证。
    /// </summary>
    public static class VisionEngine
    {
        public static VisionReport Run(IEnumerable<VisualFlowStep> steps, IProgress<string>? progress = null)
        {
            var report = new VisionReport();
            byte[]? cur = null;
            int curW = 0, curH = 0;
            bool usedSynthetic = false;
            int tplX = 0, tplY = 0, tplW = 0, tplH = 0; // 合成测试图里的“目标”矩形（用于无模板文件时自动取模板）
            var featurePts = new List<(double X, double Y, string Tag)>();

            foreach (var s in steps)
            {
                if (s == null || !s.Enabled) continue;
                switch ((s.StepType ?? "").Trim())
                {
                    case "图像采集":
                        RunAcquire(s, ref cur, ref curW, ref curH, ref usedSynthetic, ref tplX, ref tplY, ref tplW, ref tplH, report, progress);
                        featurePts.Clear();
                        break;
                    case "图像预处理":
                        RunPreprocess(s, cur, curW, curH, ref cur, ref curW, ref curH, featurePts, report, progress);
                        break;
                    case "模板匹配":
                        RunMatch(s, cur, curW, curH, usedSynthetic, tplX, tplY, tplW, tplH, ref cur, ref curW, ref curH, featurePts, report, progress);
                        break;
                    case "缺陷检测":
                        RunDefect(s, cur, curW, curH, ref cur, ref curW, ref curH, featurePts, report, progress);
                        break;
                    case "测量":
                        RunMeasure(s, curW, curH, featurePts, ref cur, ref curW, ref curH, report, progress);
                        break;
                    case "通讯":
                        RunComm(s, report, progress);
                        break;
                    default:
                        report.Results.Add(new VisionStepResult { StepName = s.Name, Type = s.StepType, Ok = false, Summary = "未知步骤类型，已跳过" });
                        break;
                }
            }

            if (cur != null)
            {
                report.HasImage = true;
                report.Width = curW;
                report.Height = curH;
                report.Bgra = cur;
            }
            return report;
        }

        // ============ 图像采集 ============
        private static void RunAcquire(VisualFlowStep s, ref byte[]? cur, ref int curW, ref int curH,
            ref bool usedSynthetic, ref int tplX, ref int tplY, ref int tplW, ref int tplH,
            VisionReport report, IProgress<string>? progress)
        {
            string path = (s.SavePath ?? "").Trim();
            bool ok = false;
            if (File.Exists(path))
            {
                ok = LoadImageFile(path, out cur, out curW, out curH);
            }
            if (!ok)
            {
                // 无有效文件 -> 生成测试图（含一个明矩形目标，供模板匹配/测量演示）
                int w = (int)Clamp(s.Width, 64, 4096);
                int h = (int)Clamp(s.Height, 64, 4096);
                cur = MakeSynthetic(w, h, out tplX, out tplY, out tplW, out tplH);
                curW = w; curH = h;
                usedSynthetic = true;
                report.Results.Add(new VisionStepResult
                {
                    StepName = s.Name,
                    Type = "图像采集",
                    Ok = true,
                    Summary = $"已生成测试图 {w}x{h}（未提供有效图像路径，自动用测试图演示）"
                });
                progress?.Report("图像采集：生成测试图");
            }
            else
            {
                usedSynthetic = false;
                report.Results.Add(new VisionStepResult
                {
                    StepName = s.Name,
                    Type = "图像采集",
                    Ok = true,
                    Summary = $"已采集图像 {curW}x{curH}"
                });
                progress?.Report($"图像采集：{curW}x{curH}");
            }
        }

        // ============ 模板匹配（由粗到细 NCC，支持角度范围） ============
        private static void RunMatch(VisualFlowStep s, byte[]? cur, int curW, int curH, bool usedSynthetic,
            int tplX, int tplY, int tplW, int tplH, ref byte[]? outCur, ref int outW, ref int outH,
            List<(double X, double Y, string Tag)> featurePts, VisionReport report, IProgress<string>? progress)
        {
            if (cur == null) { AddFail(report, s, "请先执行图像采集"); return; }

            byte[]? tpl = null; int tw = 0, th = 0;
            string tpath = (s.TemplatePath ?? "").Trim();
            if (File.Exists(tpath))
            {
                if (!LoadImageFile(tpath, out tpl, out tw, out th))
                {
                    if (usedSynthetic && tplW > 0) Carve(cur, curW, curH, tplX, tplY, tplW, tplH, out tpl, out tw, out th);
                }
            }
            else if (usedSynthetic && tplW > 0)
            {
                Carve(cur, curW, curH, tplX, tplY, tplW, tplH, out tpl, out tw, out th);
            }

            if (tpl == null) { AddFail(report, s, "请设置有效模板路径（或先采集测试图）"); return; }

            double angleRange = Clamp(s.AngleRange, 0, 360);
            var (score, bx, by, angle) = MatchTemplate(cur, curW, curH, tpl, tw, th, angleRange);
            bool pass = score >= Clamp(s.ScoreThreshold, 0, 1);

            // 在图上画绿色匹配框
            DrawRect(cur, curW, curH, bx, by, tw, th, 0, 200, 80);
            DrawCross(cur, curW, curH, bx + tw / 2, by + th / 2, 6, 0, 200, 80);
            featurePts.Add((bx + tw / 2.0, by + th / 2.0, "匹配"));

            report.Results.Add(new VisionStepResult
            {
                StepName = s.Name,
                Type = "模板匹配",
                Ok = pass,
                Summary = pass
                    ? $"匹配成功 分数 {score:F3} @ ({bx},{by}) 角度 {angle:F0}°"
                    : $"未达阈值（{s.ScoreThreshold:F2}）分数 {score:F3} @ ({bx},{by})"
            });
            progress?.Report($"模板匹配：分数 {score:F3}");
            outCur = cur; outW = curW; outH = curH;
        }

        // ============ 缺陷检测（阈值 + 连通域） ============
        private static void RunDefect(VisualFlowStep s, byte[]? cur, int curW, int curH,
            ref byte[]? outCur, ref int outW, ref int outH,
            List<(double X, double Y, string Tag)> featurePts, VisionReport report, IProgress<string>? progress)
        {
            if (cur == null) { AddFail(report, s, "请先执行图像采集"); return; }

            double thr = Clamp(s.Threshold, 0, 255);
            float[] gray = ToGray(cur, curW * curH);
            // 判定亮/暗缺陷：Algorithm 含“亮/bright”视为亮斑，否则默认暗斑
            bool bright = (s.Algorithm ?? "").IndexOf("亮", StringComparison.Ordinal) >= 0
                       || (s.Algorithm ?? "").IndexOf("bright", StringComparison.OrdinalIgnoreCase) >= 0;
            bool[] mask = new bool[curW * curH];
            for (int i = 0; i < mask.Length; i++)
                mask[i] = bright ? gray[i] >= (float)thr : gray[i] <= (float)thr;

            double minA = Clamp(s.MinArea, 1, 1e9);
            double maxA = Clamp(s.MaxArea, 1, 1e9);
            var comps = ConnectedComponents(mask, curW, curH, minA, maxA);

            int idx = 0;
            (double X, double Y, string Tag)? largest = null;
            double largestArea = -1;
            foreach (var c in comps)
            {
                idx++;
                DrawRect(cur, curW, curH, c.X, c.Y, c.W, c.H, 220, 40, 40);
                var cx = c.X + c.W / 2.0;
                var cy = c.Y + c.H / 2.0;
                if (c.Area > largestArea) { largestArea = c.Area; largest = (cx, cy, "缺陷" + idx); }
            }
            if (largest.HasValue) featurePts.Add(largest.Value);

            report.Results.Add(new VisionStepResult
            {
                StepName = s.Name,
                Type = "缺陷检测",
                Ok = true,
                Summary = $"检出 {comps.Count} 处缺陷（面积阈值 {minA:0}~{maxA:0}）"
            });
            progress?.Report($"缺陷检测：{comps.Count} 处");
            outCur = cur; outW = curW; outH = curH;
        }

        // ============ 测量（两特征点像素距离 x 标定系数） ============
        private static void RunMeasure(VisualFlowStep s, int curW, int curH,
            List<(double X, double Y, string Tag)> featurePts, ref byte[]? outCur, ref int outW, ref int outH,
            VisionReport report, IProgress<string>? progress)
        {
            var pts = new List<(double X, double Y, string Tag)>(featurePts);
            // 不足两个点 -> 用图像中心补足，保证有可测距离
            if (pts.Count < 2)
            {
                double cx = curW / 2.0, cy = curH / 2.0;
                while (pts.Count < 2) pts.Add((cx, cy, "中心"));
            }
            var a = pts[pts.Count - 2];
            var b = pts[pts.Count - 1];
            double dx = a.X - b.X, dy = a.Y - b.Y;
            double px = Math.Sqrt(dx * dx + dy * dy);
            double cal = Clamp(s.Calibration, 1e-6, 1e9);
            double len = px * cal;

            if (outCur != null)
            {
                DrawLine(outCur, outW, outH, (int)a.X, (int)a.Y, (int)b.X, (int)b.Y, 40, 120, 240);
                DrawCross(outCur, outW, outH, (int)a.X, (int)a.Y, 5, 40, 120, 240);
                DrawCross(outCur, outW, outH, (int)b.X, (int)b.Y, 5, 40, 120, 240);
            }

            report.Results.Add(new VisionStepResult
            {
                StepName = s.Name,
                Type = "测量",
                Ok = true,
                Summary = $"{a.Tag}->{b.Tag} 距离 {len:F2} {s.Unit}（{px:F1}px x 标定 {cal}）"
            });
            progress?.Report($"测量：{len:F2} {s.Unit}");
        }

        // ============ 图像预处理 ============
        // 在 cur（BGRA）上做 16 种预处理操作：灰度/二值/平滑/形态学/Sobel/直方图均衡/ROI/算术。
        // 大部分基于灰度 float[]，再 WriteGray 写回 BGRA；ROI 改变 curW/curH；算术需读第二张图。
        private static void RunPreprocess(VisualFlowStep s, byte[]? cur, int curW, int curH,
            ref byte[]? outCur, ref int outW, ref int outH,
            List<(double X, double Y, string Tag)> featurePts, VisionReport report, IProgress<string>? progress)
        {
            if (cur == null) { AddFail(report, s, "请先执行图像采集"); return; }

            string op = (s.PreOp ?? "").Trim();
            if (op.Length == 0 || op == "无")
            {
                report.Results.Add(new VisionStepResult { StepName = s.Name, Type = "图像预处理", Ok = true, Summary = "无操作" });
                outCur = cur; outW = curW; outH = curH;
                return;
            }

            // 通用参数
            int k1 = Math.Max(1, (int)Math.Round(s.PreParam2)); // 核大小（默认 3）
            if ((k1 & 1) == 0) k1++; // 强制奇数
            double thr = Clamp(s.PreParam1, 0, 255);

            try
            {
                switch (op)
                {
                    case "灰度化":
                        {
                            float[] g = ToGray(cur, curW * curH);
                            WriteGray(cur, curW, curH, g);
                            break;
                        }
                    case "二值化":
                        {
                            float[] g = ToGray(cur, curW * curH);
                            for (int i = 0; i < g.Length; i++) g[i] = g[i] >= thr ? 255f : 0f;
                            WriteGray(cur, curW, curH, g);
                            break;
                        }
                    case "高斯平滑":
                        {
                            float[] g = ToGray(cur, curW * curH);
                            float[] dst = GaussianBlur(g, curW, curH, k1);
                            WriteGray(cur, curW, curH, dst);
                            break;
                        }
                    case "中值滤波":
                        {
                            float[] g = ToGray(cur, curW * curH);
                            float[] dst = MedianFilter(g, curW, curH, k1);
                            WriteGray(cur, curW, curH, dst);
                            break;
                        }
                    case "腐蚀":
                        {
                            float[] g = ToGray(cur, curW * curH);
                            float[] dst = Erode(g, curW, curH, k1);
                            WriteGray(cur, curW, curH, dst);
                            break;
                        }
                    case "膨胀":
                        {
                            float[] g = ToGray(cur, curW * curH);
                            float[] dst = Dilate(g, curW, curH, k1);
                            WriteGray(cur, curW, curH, dst);
                            break;
                        }
                    case "开运算":
                        {
                            float[] g = ToGray(cur, curW * curH);
                            float[] dst = Dilate(Erode(g, curW, curH, k1), curW, curH, k1);
                            WriteGray(cur, curW, curH, dst);
                            break;
                        }
                    case "闭运算":
                        {
                            float[] g = ToGray(cur, curW * curH);
                            float[] dst = Erode(Dilate(g, curW, curH, k1), curW, curH, k1);
                            WriteGray(cur, curW, curH, dst);
                            break;
                        }
                    case "Sobel边缘":
                        {
                            float[] g = ToGray(cur, curW * curH);
                            float[] mag = Sobel(g, curW, curH);
                            WriteGray(cur, curW, curH, mag);
                            break;
                        }
                    case "直方图均衡":
                        {
                            float[] g = ToGray(cur, curW * curH);
                            HistEqual(g);
                            WriteGray(cur, curW, curH, g);
                            break;
                        }
                    case "ROI裁剪":
                        {
                            if (!TryParseRoi(s.PreRoi, curW, curH, out int rx, out int ry, out int rw, out int rh))
                            { AddFail(report, s, "ROI 格式错误（应为 x,y,w,h）"); return; }
                            byte[] crop = RoiCrop(cur, curW, curH, rx, ry, rw, rh);
                            outCur = crop; outW = rw; outH = rh;
                            featurePts.Clear(); // ROI 后原坐标失效
                            report.Results.Add(new VisionStepResult { StepName = s.Name, Type = "图像预处理", Ok = true, Summary = $"ROI 裁剪 {rw}x{rh} @ ({rx},{ry})" });
                            progress?.Report($"ROI 裁剪：{rw}x{rh}");
                            return;
                        }
                    case "图像加":
                    case "图像减":
                    case "图像与":
                    case "图像或":
                        {
                            byte[]? b2 = null; int bw2 = 0, bh2 = 0;
                            string p2 = (s.PreImage2Path ?? "").Trim();
                            if (!File.Exists(p2) || !LoadImageFile(p2, out b2, out bw2, out bh2) || b2 == null)
                            { AddFail(report, s, "第二张图加载失败（检查路径与尺寸一致）"); return; }
                            if (bw2 != curW || bh2 != curH)
                            { AddFail(report, s, $"第二张图尺寸 {bw2}x{bh2} 与当前 {curW}x{curH} 不一致"); return; }
                            if (op == "图像加")      Arith(cur, b2, curW * curH * 4, (a, b) => Clamp(a + b, 0, 255));
                            else if (op == "图像减") Arith(cur, b2, curW * curH * 4, (a, b) => Clamp(a - b, 0, 255));
                            else if (op == "图像与") Arith(cur, b2, curW * curH * 4, (a, b) => (double)((int)a & (int)b));
                            else                     Arith(cur, b2, curW * curH * 4, (a, b) => (double)((int)a | (int)b));
                            break;
                        }
                    default:
                        AddFail(report, s, $"未知预处理操作：{op}");
                        return;
                }

                report.Results.Add(new VisionStepResult { StepName = s.Name, Type = "图像预处理", Ok = true, Summary = $"已执行 {op}" });
                progress?.Report($"预处理：{op}");
                outCur = cur; outW = curW; outH = curH;
            }
            catch (Exception ex)
            {
                AddFail(report, s, $"{op} 失败：{ex.Message}");
            }
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

        private static bool LoadImageFile(string path, out byte[]? buf, out int w, out int h)
        {
            buf = Array.Empty<byte>(); w = 0; h = 0;
            try
            {
                var bi = new BitmapImage();
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = fs;
                    bi.EndInit();
                    bi.Freeze();
                }
                var conv = new FormatConvertedBitmap(bi, PixelFormats.Bgra32, null, 0);
                conv.Freeze();
                w = conv.PixelWidth; h = conv.PixelHeight;
                int stride = w * 4;
                buf = new byte[h * stride];
                conv.CopyPixels(buf, stride, 0);
                return true;
            }
            catch { return false; }
        }

        /// <summary>生成测试图：渐变背景 + 暗圆（缺陷候选） + 亮矩形（目标，记录其位置供自动取模板）。</summary>
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

        private static void Carve(byte[] src, int sw, int sh, int x, int y, int w, int h, out byte[]? tpl, out int tw, out int th)
        {
            tw = Math.Min(w, sw - x); th = Math.Min(h, sh - y);
            if (tw <= 0 || th <= 0) { tpl = Array.Empty<byte>(); tw = th = 0; return; }
            tpl = new byte[tw * th * 4];
            for (int j = 0; j < th; j++)
                for (int i = 0; i < tw; i++)
                {
                    int si = ((y + j) * sw + (x + i)) * 4;
                    int di = (j * tw + i) * 4;
                    tpl[di] = src[si]; tpl[di + 1] = src[si + 1]; tpl[di + 2] = src[si + 2]; tpl[di + 3] = 255;
                }
        }

        // -------------------- 模板匹配核心 --------------------

        private static (double score, int bx, int by, double angle) MatchTemplate(
            byte[] src, int sw, int sh, byte[] tpl, int tw, int th, double angleRange)
        {
            float[] sFull = ToGray(src, sw * sh);
            float[] tFull = ToGray(tpl, tw * th);

            // 粗尺度：源长边<=160，模板长边<=40
            int sFactor = Math.Max(1, (int)Math.Ceiling(Math.Max(sw, sh) / 160.0));
            int tFactor = Math.Max(sFactor, (int)Math.Ceiling(Math.Max(tw, th) / 40.0));
            int csw = Math.Max(1, sw / sFactor), csh = Math.Max(1, sh / sFactor);
            int ctw = Math.Max(1, tw / tFactor), cth = Math.Max(1, th / tFactor);
            float[] cS = Downsample(sFull, sw, sh, csw, csh);
            float[] cT = Downsample(tFull, tw, th, ctw, cth);

            var angles = BuildAngles(angleRange);
            double best = -2; int bbx = 0, bby = 0; double bangle = 0;
            foreach (var a in angles)
            {
                float[] rt; int rtw, rth;
                if (Math.Abs(a) < 1e-6) { rt = cT; rtw = ctw; rth = cth; }
                else { rt = Rotate(cT, ctw, cth, a); rtw = ctw; rth = cth; }
                (float m, float sd) = MeanStd(rt);
                if (sd < 1e-3) continue;
                for (int y = 0; y + rth <= csh; y++)
                    for (int x = 0; x + rtw <= csw; x++)
                    {
                        double sc = NccAt(cS, csw, csh, rt, rtw, rth, m, sd, x, y);
                        if (sc > best) { best = sc; bbx = x; bby = y; bangle = a; }
                    }
                if (best >= 0.99) break;
            }

            // 精修：全分辨率在粗匹配点附近小窗搜索
            int fx = bbx * sFactor, fy = bby * sFactor;
            float[] rtFull; int rfw, rfh;
            if (Math.Abs(bangle) < 1e-6) { rtFull = tFull; rfw = tw; rfh = th; }
            else { rtFull = Rotate(tFull, tw, th, bangle); rfw = tw; rfh = th; }
            (float fm, float fsd) = MeanStd(rtFull);
            int win = (ctw * tFactor) + 16;
            int x0 = Math.Max(0, fx - win / 2), y0 = Math.Max(0, fy - win / 2);
            int x1 = Math.Min(sw - rfw, fx + win / 2), y1 = Math.Min(sh - rfh, fy + win / 2);
            double bestF = best; int bfx = fx, bfy = fy;
            if (fsd >= 1e-3)
            {
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        double sc = NccAt(sFull, sw, sh, rtFull, rfw, rfh, fm, fsd, x, y);
                        if (sc > bestF) { bestF = sc; bfx = x; bfy = y; }
                    }
            }
            return (bestF, bfx, bfy, bangle);
        }

        private static List<double> BuildAngles(double angleRange)
        {
            var list = new List<double>();
            if (angleRange <= 0) { list.Add(0); return list; }
            int n = Math.Max(1, Math.Min(8, (int)Math.Round(angleRange / 45.0)));
            for (int i = 0; i <= n; i++) list.Add(angleRange * i / n);
            return list;
        }

        /// <summary>归一化互相关（NCC）：返回 [-1,1]，1 表示完全一致。</summary>
        private static double NccAt(float[] src, int sw, int sh, float[] tpl, int tw, int th,
            float tMean, float tStd, int x0, int y0)
        {
            if (y0 < 0 || x0 < 0 || y0 + th > sh || x0 + tw > sw) return -2;
            double sumS = 0;
            for (int j = 0; j < th; j++)
                for (int i = 0; i < tw; i++)
                    sumS += src[(y0 + j) * sw + (x0 + i)];
            double meanS = sumS / (tw * th);
            double ss = 0, st = 0;
            for (int j = 0; j < th; j++)
            {
                int row = (y0 + j) * sw;
                int trow = j * tw;
                for (int i = 0; i < tw; i++)
                {
                    double ds = src[row + x0 + i] - meanS;
                    double dt = tpl[trow + i] - tMean;
                    ss += ds * ds;
                    st += ds * dt;
                }
            }
            double denom = Math.Sqrt(ss) * tStd;
            return denom < 1e-6 ? 0 : st / denom;
        }

        // -------------------- 图像处理基元 --------------------

        private static float[] ToGray(byte[] bgra, int n)
        {
            var g = new float[n];
            for (int i = 0; i < n; i++)
                g[i] = 0.299f * bgra[i * 4 + 2] + 0.587f * bgra[i * 4 + 1] + 0.114f * bgra[i * 4];
            return g;
        }

        private static float[] Downsample(float[] src, int sw, int sh, int dw, int dh)
        {
            var outp = new float[dw * dh];
            for (int y = 0; y < dh; y++)
            {
                int sy = Math.Min(sh - 1, y * sh / dh);
                for (int x = 0; x < dw; x++)
                {
                    int sx = Math.Min(sw - 1, x * sw / dw);
                    outp[y * dw + x] = src[sy * sw + sx];
                }
            }
            return outp;
        }

        private static float[] Rotate(float[] src, int w, int h, double angleDeg)
        {
            double a = angleDeg * Math.PI / 180.0;
            double ca = Math.Cos(a), sa = Math.Sin(a);
            var outp = new float[w * h];
            int cx = w / 2, cy = h / 2;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double dx = x - cx, dy = y - cy;
                    double sx = cx + dx * ca + dy * sa;
                    double sy = cy - dx * sa + dy * ca;
                    int ix = (int)Math.Round(sx), iy = (int)Math.Round(sy);
                    if (ix >= 0 && ix < w && iy >= 0 && iy < h) outp[y * w + x] = src[iy * w + ix];
                    else outp[y * w + x] = 0;
                }
            return outp;
        }

        private static (float mean, float std) MeanStd(float[] arr)
        {
            double sum = 0;
            for (int i = 0; i < arr.Length; i++) sum += arr[i];
            float mean = (float)(sum / arr.Length);
            double v = 0;
            for (int i = 0; i < arr.Length; i++) { double d = arr[i] - mean; v += d * d; }
            return (mean, (float)Math.Sqrt(v / arr.Length));
        }

        // -------------------- 图像预处理原语（纯灰度 float[]） --------------------

        /// <summary>WriteGray：把灰度浮点缓冲写回 BGRA（B=G=R=val, A=255）。原地修改 cur。</summary>
        private static void WriteGray(byte[] cur, int w, int h, float[] g)
        {
            int n = w * h;
            for (int i = 0; i < n; i++)
            {
                byte v = (byte)Clamp(g[i], 0, 255);
                int k = i * 4;
                cur[k] = v; cur[k + 1] = v; cur[k + 2] = v; cur[k + 3] = 255;
            }
        }

        /// <summary>GrayThreshold：按灰度阈值二值化。</summary>
        private static void GrayThreshold(float[] g, double thr)
        {
            for (int i = 0; i < g.Length; i++) g[i] = g[i] >= thr ? 255f : 0f;
        }

        /// <summary>GaussianBlur：k x k 可分离高斯（k 强制奇数）。边界用 clamp。</summary>
        private static float[] GaussianBlur(float[] src, int w, int h, int k)
        {
            if (k <= 1) return (float[])src.Clone();
            double sigma = Math.Max(0.5, k / 3.0);
            int half = k / 2;
            double[] k1d = new double[k];
            double sum = 0;
            for (int i = 0; i < k; i++)
            {
                double x = i - half;
                k1d[i] = Math.Exp(-x * x / (2 * sigma * sigma));
                sum += k1d[i];
            }
            for (int i = 0; i < k; i++) k1d[i] /= sum;

            float[] tmp = new float[w * h];
            float[] dst = new float[w * h];
            // 水平
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double s = 0;
                    for (int i = 0; i < k; i++)
                    {
                        int xi = Math.Min(w - 1, Math.Max(0, x + i - half));
                        s += src[y * w + xi] * k1d[i];
                    }
                    tmp[y * w + x] = (float)s;
                }
            // 垂直
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double s = 0;
                    for (int i = 0; i < k; i++)
                    {
                        int yi = Math.Min(h - 1, Math.Max(0, y + i - half));
                        s += tmp[yi * w + x] * k1d[i];
                    }
                    dst[y * w + x] = (float)s;
                }
            return dst;
        }

        /// <summary>MedianFilter：k x k 中值（k 强制奇数，k=3 性能可控）。</summary>
        private static float[] MedianFilter(float[] src, int w, int h, int k)
        {
            int half = k / 2;
            float[] dst = new float[w * h];
            var win = new float[k * k];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int n = 0;
                    for (int j = -half; j <= half; j++)
                        for (int i = -half; i <= half; i++)
                        {
                            int xi = Math.Min(w - 1, Math.Max(0, x + i));
                            int yi = Math.Min(h - 1, Math.Max(0, y + j));
                            win[n++] = src[yi * w + xi];
                        }
                    Array.Sort(win, 0, n);
                    dst[y * w + x] = win[n / 2];
                }
            return dst;
        }

        /// <summary>Erode：k x k 灰度腐蚀（输出邻域最小值）。</summary>
        private static float[] Erode(float[] src, int w, int h, int k)
        {
            int half = k / 2;
            float[] dst = new float[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float m = float.MaxValue;
                    for (int j = -half; j <= half; j++)
                        for (int i = -half; i <= half; i++)
                        {
                            int xi = Math.Min(w - 1, Math.Max(0, x + i));
                            int yi = Math.Min(h - 1, Math.Max(0, y + j));
                            float v = src[yi * w + xi];
                            if (v < m) m = v;
                        }
                    dst[y * w + x] = m;
                }
            return dst;
        }

        /// <summary>Dilate：k x k 灰度膨胀（输出邻域最大值）。</summary>
        private static float[] Dilate(float[] src, int w, int h, int k)
        {
            int half = k / 2;
            float[] dst = new float[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float m = float.MinValue;
                    for (int j = -half; j <= half; j++)
                        for (int i = -half; i <= half; i++)
                        {
                            int xi = Math.Min(w - 1, Math.Max(0, x + i));
                            int yi = Math.Min(h - 1, Math.Max(0, y + j));
                            float v = src[yi * w + xi];
                            if (v > m) m = v;
                        }
                    dst[y * w + x] = m;
                }
            return dst;
        }

        /// <summary>Sobel：3x3 梯度幅值，归一化到 0..255。</summary>
        private static float[] Sobel(float[] src, int w, int h)
        {
            float[] dst = new float[w * h];
            float max = 0;
            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++)
                {
                    float gx = -src[(y - 1) * w + (x - 1)] - 2 * src[y * w + (x - 1)] - src[(y + 1) * w + (x - 1)]
                               + src[(y - 1) * w + (x + 1)] + 2 * src[y * w + (x + 1)] + src[(y + 1) * w + (x + 1)];
                    float gy = -src[(y - 1) * w + (x - 1)] - 2 * src[(y - 1) * w + x] - src[(y - 1) * w + (x + 1)]
                               + src[(y + 1) * w + (x - 1)] + 2 * src[(y + 1) * w + x] + src[(y + 1) * w + (x + 1)];
                    float m = Math.Abs(gx) + Math.Abs(gy);
                    dst[y * w + x] = m;
                    if (m > max) max = m;
                }
            if (max > 0)
                for (int i = 0; i < dst.Length; i++) dst[i] = dst[i] * 255f / max;
            return dst;
        }

        /// <summary>HistEqual：直方图均衡（256 桶，0..255）。</summary>
        private static void HistEqual(float[] g)
        {
            int n = g.Length;
            int[] hist = new int[256];
            for (int i = 0; i < n; i++)
            {
                int v = (int)Clamp(g[i], 0, 255);
                hist[v]++;
            }
            int[] cdf = new int[256];
            cdf[0] = hist[0];
            for (int i = 1; i < 256; i++) cdf[i] = cdf[i - 1] + hist[i];
            int cdfMin = 0;
            for (int i = 0; i < 256; i++) if (cdf[i] != 0) { cdfMin = cdf[i]; break; }
            int denom = n - cdfMin;
            if (denom <= 0) return;
            for (int i = 0; i < n; i++)
            {
                int v = (int)Clamp(g[i], 0, 255);
                g[i] = (float)Math.Round((cdf[v] - cdfMin) * 255.0 / denom);
            }
        }

        /// <summary>TryParseRoi：解析 "x,y,w,h"；返回是否成功并 clamp 到图像范围。</summary>
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

        /// <summary>RoiCrop：按 ROI 裁剪；返回新 BGRA 缓冲。</summary>
        private static byte[] RoiCrop(byte[] src, int sw, int sh, int x, int y, int w, int h)
        {
            byte[] dst = new byte[w * h * 4];
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    int si = ((y + j) * sw + (x + i)) * 4;
                    int di = (j * w + i) * 4;
                    dst[di] = src[si]; dst[di + 1] = src[si + 1]; dst[di + 2] = src[si + 2]; dst[di + 3] = 255;
                }
            return dst;
        }

        /// <summary>Arith：对两个同尺寸 BGRA 缓冲逐字节按 func 合并，写回 a。</summary>
        private static void Arith(byte[] a, byte[] b, int n, Func<double, double, double> func)
        {
            for (int i = 0; i < n; i++) a[i] = (byte)Clamp(func(a[i], b[i]), 0, 255);
        }

        private sealed class Comp { public int X, Y, W, H; public double Area; }

        private static List<Comp> ConnectedComponents(bool[] mask, int w, int h, double minA, double maxA)
        {
            var comps = new List<Comp>();
            bool[] visited = new bool[w * h];
            var stack = new List<int>(1024);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int start = y * w + x;
                    if (!mask[start] || visited[start]) continue;
                    stack.Clear();
                    stack.Add(start);
                    visited[start] = true;
                    int minX = x, minY = y, maxX = x, maxY = y; int count = 0;
                    while (stack.Count > 0)
                    {
                        int p = stack[stack.Count - 1]; stack.RemoveAt(stack.Count - 1);
                        int px = p % w, py = p / w;
                        count++;
                        if (px < minX) minX = px; if (px > maxX) maxX = px;
                        if (py < minY) minY = py; if (py > maxY) maxY = py;
                        TryPush(mask, visited, stack, w, h, px - 1, py);
                        TryPush(mask, visited, stack, w, h, px + 1, py);
                        TryPush(mask, visited, stack, w, h, px, py - 1);
                        TryPush(mask, visited, stack, w, h, px, py + 1);
                    }
                    if (count >= minA && count <= maxA)
                        comps.Add(new Comp { X = minX, Y = minY, W = maxX - minX + 1, H = maxY - minY + 1, Area = count });
                }
            return comps;
        }

        private static void TryPush(bool[] mask, bool[] visited, List<int> stack, int w, int h, int x, int y)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            int idx = y * w + x;
            if (mask[idx] && !visited[idx]) { visited[idx] = true; stack.Add(idx); }
        }

        // -------------------- 像素绘制（BGRA） --------------------

        private static void DrawRect(byte[] buf, int w, int h, int x, int y, int bw, int bh, int r, int g, int b)
        {
            int x0 = Math.Max(0, x), y0 = Math.Max(0, y);
            int x1 = Math.Min(w - 1, x + bw - 1), y1 = Math.Min(h - 1, y + bh - 1);
            for (int i = x0; i <= x1; i++) { SetPx(buf, w, i, y0, r, g, b); SetPx(buf, w, i, y1, r, g, b); }
            for (int j = y0; j <= y1; j++) { SetPx(buf, w, x0, j, r, g, b); SetPx(buf, w, x1, j, r, g, b); }
        }

        private static void DrawLine(byte[] buf, int w, int h, int x0, int y0, int x1, int y1, int r, int g, int b)
        {
            int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy, x = x0, y = y0;
            while (true)
            {
                SetPx(buf, w, x, y, r, g, b);
                if (x == x1 && y == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }
        }

        private static void DrawCross(byte[] buf, int w, int h, int cx, int cy, int size, int r, int g, int b)
        {
            for (int i = -size; i <= size; i++)
            {
                SetPx(buf, w, cx + i, cy, r, g, b);
                SetPx(buf, w, cx, cy + i, r, g, b);
            }
        }

        private static void SetPx(byte[] buf, int w, int x, int y, int r, int g, int b)
        {
            if (x < 0 || y < 0 || x >= w || y >= buf.Length / (w * 4)) return;
            int i = (y * w + x) * 4;
            buf[i] = (byte)b; buf[i + 1] = (byte)g; buf[i + 2] = (byte)r; buf[i + 3] = 255;
        }
    }
}
// === NoCodeMotion 视觉流程引擎 | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
