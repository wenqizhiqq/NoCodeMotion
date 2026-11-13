// === NoCodeMotion 视觉流程引擎（VisionPlus 算子移植 · OpenCV 真实实现） | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Cv = OpenCvSharp;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Vision
{
    /// <summary>
    /// 深度学习 / 传统学习算子：ONNX 分类、检测、分割，文字检测+识别，样本学习分类。
    /// 全部走 OpenCV DNN / WinRT OCR 的真实推理，无桩、无假结果；模型缺失或形状不符都如实报失败。
    /// </summary>
    internal static class VpOpsAI
    {
        public static Cv.Mat Run(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            switch (spec.Key)
            {
                case "dlclassify": return DlClassify(spec, s, cur, dst, report, progress);
                case "dldetect": return DlDetect(spec, s, cur, dst, report, progress);
                case "dlsegment": return DlSegment(spec, s, cur, dst, report, progress);
                case "dlocr": return DlOcr(spec, s, cur, dst, report, progress);
                case "elclassify": return ElClassify(spec, s, cur, dst, report, ctx, progress);
            }
            return cur;
        }

        // ---------------- 1. ONNX 图像分类 ----------------
        private static Cv.Mat DlClassify(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            if (!HaveModel(spec, s, report, progress, out string model)) return cur;
            string labelsPath = VpUtil.Txt(spec, s, "labels");
            var roi = VpUtil.Roi(spec, s, cur);
            int size = Math.Max(16, VpUtil.Int(spec, s, "size", 224));
            int topk = Math.Max(1, VpUtil.Int(spec, s, "topk", 3));
            double minScore = VpUtil.Num(spec, s, "minScore", 0);
            try
            {
                string[]? labels = LoadLabels(labelsPath);
                using var crop = VpUtil.Crop(cur, roi);
                using var bgr = VpUtil.ToBgr(crop);
                using var blob = Cv.Dnn.CvDnn.BlobFromImage(bgr, 1.0, new Cv.Size(size, size),
                    new Cv.Scalar(0, 0, 0), swapRB: true, crop: false);
                using var net = Cv.Dnn.CvDnn.ReadNetFromOnnx(model);
                net.SetInput(blob, "");
                using var prob = net.Forward("");

                float[] data = FlattenF32(prob);
                if (data.Length == 0)
                {
                    Fail(report, s, spec, "分类模型没有输出分数（输出为空）", progress);
                    return cur;
                }
                // softmax：减最大值防溢出，再归一化
                double maxv = data[0];
                for (int i = 1; i < data.Length; i++) if (data[i] > maxv) maxv = data[i];
                double sum = 0;
                var exps = new double[data.Length];
                for (int i = 0; i < data.Length; i++) { exps[i] = Math.Exp(data[i] - maxv); sum += exps[i]; }
                var probs = new double[data.Length];
                for (int i = 0; i < data.Length; i++) probs[i] = sum > 0 ? exps[i] / sum : 0;

                int[] order = Enumerable.Range(0, data.Length).ToArray();
                Array.Sort(order, (a, b) => probs[b].CompareTo(probs[a]));
                int k = Math.Min(topk, data.Length);
                int best = order[0];
                double bestP = probs[best];
                string bestName = LabelOf(labels, best);
                bool pass = minScore <= 0 || bestP >= minScore;

                // 把最佳类别画到注释画布
                string draw = bestName + " " + VpUtil.F(bestP * 100, 1) + "%";
                Cv.Cv2.PutText(dst, draw, new Cv.Point(roi.X + 4, Math.Max(16, roi.Y + 22)),
                    Cv.HersheyFonts.HersheySimplex, 0.6, pass ? VpUtil.Ink : VpUtil.Warn, 2);

                var nums = new List<(string, double)>();
                nums.Add(("最佳类别序号", best));
                nums.Add(("置信度", bestP));
                nums.Add(("是否合格", pass ? 1 : 0));
                for (int i = 0; i < k; i++) nums.Add(("第" + (i + 1) + "名分数", probs[order[i]]));

                string summary = "最佳「" + bestName + "」置信度 " + VpUtil.F(bestP * 100, 1) + "%"
                    + (minScore > 0 ? (pass ? "，达到及格线" : "，低于及格线 " + VpUtil.F(minScore, 2)) : "");
                VpUtil.Add(report, s, spec, pass, summary, bestP, data.Length, bestName, nums.ToArray());
                VpUtil.Progress(progress, "ONNX分类：" + summary);
            }
            catch (Exception ex)
            {
                Fail(report, s, spec, "ONNX分类推理失败：" + ex.Message, progress);
            }
            return cur;
        }

        // ---------------- 2. ONNX 目标检测 ----------------
        private static Cv.Mat DlDetect(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            if (!HaveModel(spec, s, report, progress, out string model)) return cur;
            string[]? labels = LoadLabels(VpUtil.Txt(spec, s, "labels"));
            var roi = VpUtil.Roi(spec, s, cur);
            int size = Math.Max(32, VpUtil.Int(spec, s, "size", 640));
            double conf = VpUtil.Num(spec, s, "conf", 0.35);
            double nms = VpUtil.Num(spec, s, "nms", 0.45);
            int maxHits = Math.Max(1, VpUtil.Int(spec, s, "maxHits", 20));
            try
            {
                using var crop = VpUtil.Crop(cur, roi);
                using var bgr = VpUtil.ToBgr(crop);
                using var blob = Cv.Dnn.CvDnn.BlobFromImage(bgr, 1.0, new Cv.Size(size, size),
                    new Cv.Scalar(0, 0, 0), swapRB: true, crop: false);
                using var net = Cv.Dnn.CvDnn.ReadNetFromOnnx(model);
                net.SetInput(blob, "");
                using var prob = net.Forward("");

                if (!ParseDetections(prob, out var dets, out string err))
                {
                    Fail(report, s, spec, err, progress);
                    return cur;
                }
                // 归一化坐标 → 整图像素坐标，并过滤低分框
                var boxes = new List<Box>();
                foreach (var d in dets)
                {
                    if (d.Conf < conf) continue;
                    int x1 = ClampInt(roi.X + d.X1 * roi.Width, 0, cur.Cols);
                    int y1 = ClampInt(roi.Y + d.Y1 * roi.Height, 0, cur.Rows);
                    int x2 = ClampInt(roi.X + d.X2 * roi.Width, 0, cur.Cols);
                    int y2 = ClampInt(roi.Y + d.Y2 * roi.Height, 0, cur.Rows);
                    if (x2 <= x1 || y2 <= y1) continue;
                    boxes.Add(new Box { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Conf = d.Conf, Class = d.Class });
                }
                var keep = Nms(boxes, nms);
                if (keep.Count > maxHits) keep = keep.Take(maxHits).ToList();

                // 画框 + 标签
                foreach (var b in keep)
                {
                    Cv.Cv2.Rectangle(dst, new Cv.Rect(b.X1, b.Y1, b.X2 - b.X1, b.Y2 - b.Y1), VpUtil.Ink, 2);
                    string txt = LabelOf(labels, b.Class) + " " + VpUtil.F(b.Conf, 2);
                    Cv.Cv2.PutText(dst, txt, new Cv.Point(b.X1, Math.Max(12, b.Y1 - 4)),
                        Cv.HersheyFonts.HersheySimplex, 0.5, VpUtil.Ink, 1);
                }

                double bestConf = keep.Count > 0 ? keep[0].Conf : 0;
                int bx = keep.Count > 0 ? keep[0].X1 : 0;
                int by = keep.Count > 0 ? keep[0].Y1 : 0;
                int bw = keep.Count > 0 ? keep[0].X2 - keep[0].X1 : 0;
                int bh = keep.Count > 0 ? keep[0].Y2 - keep[0].Y1 : 0;
                var nums = new List<(string, double)>();
                nums.Add(("目标个数", keep.Count));
                nums.Add(("最佳置信度", bestConf));
                nums.Add(("最佳框X", bx));
                nums.Add(("最佳框Y", by));
                nums.Add(("最佳框宽", bw));
                nums.Add(("最佳框高", bh));
                string summary = "检测到 " + keep.Count + " 个目标"
                    + (keep.Count > 0 ? "，最佳置信度 " + VpUtil.F(bestConf, 2) + "（" + LabelOf(labels, keep[0].Class) + "）" : "（无过阈值的框）");
                VpUtil.Add(report, s, spec, true, summary, bestConf, keep.Count, "", nums.ToArray());
                VpUtil.Progress(progress, "ONNX检测：" + summary);
            }
            catch (Exception ex)
            {
                Fail(report, s, spec, "ONNX检测推理失败：" + ex.Message, progress);
            }
            return cur;
        }

        // ---------------- 3. ONNX 图像分割（换图类） ----------------
        private static Cv.Mat DlSegment(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            if (!HaveModel(spec, s, report, progress, out string model)) return cur;
            var roi = VpUtil.Roi(spec, s, cur);
            int size = Math.Max(32, VpUtil.Int(spec, s, "size", 512));
            int classes = Math.Max(2, VpUtil.Int(spec, s, "classes", 2));
            double conf = VpUtil.Num(spec, s, "conf", 0);
            try
            {
                using var crop = VpUtil.Crop(cur, roi);
                using var bgr = VpUtil.ToBgr(crop);
                using var blob = Cv.Dnn.CvDnn.BlobFromImage(bgr, 1.0, new Cv.Size(size, size),
                    new Cv.Scalar(0, 0, 0), swapRB: true, crop: false);
                using var net = Cv.Dnn.CvDnn.ReadNetFromOnnx(model);
                net.SetInput(blob, "");
                using var prob = net.Forward("");

                int dims = prob.Dims;
                int[] shape = new int[dims];
                for (int i = 0; i < dims; i++) shape[i] = prob.Size(i);
                float[] data = FlattenF32(prob);

                // N×C×H×W 分数图（这里 N=1）→ 逐像素 argmax；否则当作 H×W（或 1×H×W）标签图
                bool scoreMode = dims == 4 && shape[0] == 1;
                int H, W, C = 0, off = 0;
                if (scoreMode) { C = shape[1]; H = shape[2]; W = shape[3]; }
                else
                {
                    H = shape[Math.Max(0, dims - 2)];
                    W = shape[dims - 1];
                    off = Math.Max(0, (int)(data.Length - (long)H * W));   // 取最后 H*W 个数当标签图
                }
                int useC = scoreMode ? Math.Min(classes, C) : 0;
                var labelBytes = new byte[Math.Max(0, H * W)];
                var counts = new long[classes];
                long valid = 0;
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        int p = y * W + x;
                        int cls = 0;
                        if (scoreMode)
                        {
                            float best = -1e30f;
                            for (int c = 0; c < useC; c++)
                            {
                                int di = c * H * W + p;
                                float v = di < data.Length ? data[di] : -1e30f;
                                if (v > best) { best = v; cls = c; }
                            }
                            if (conf > 0 && best < conf) cls = 0;   // 低于阈值归为背景
                            else valid++;
                        }
                        else
                        {
                            int di = off + p;
                            cls = di >= 0 && di < data.Length ? (int)data[di] : 0;
                            if (cls < 0) cls = 0;
                            if (cls >= classes) cls = classes - 1;
                            valid++;                                  // 标签图无分数，全部算有效
                        }
                        labelBytes[p] = (byte)cls;
                        if (cls < classes) counts[cls]++;
                    }

                // 固定确定性 BGR 调色板（手工 LUT，不依赖 ApplyColorMap）
                byte[] lut = BuildPalette(classes);
                var colorBytes = new byte[H * W * 3];
                for (int p = 0; p < H * W; p++)
                {
                    int c = labelBytes[p];
                    colorBytes[p * 3] = lut[c * 3];
                    colorBytes[p * 3 + 1] = lut[c * 3 + 1];
                    colorBytes[p * 3 + 2] = lut[c * 3 + 2];
                }
                using var colorSmall = new Cv.Mat(H, W, Cv.MatType.CV_8UC3);
                Marshal.Copy(colorBytes, 0, colorSmall.Data, colorBytes.Length);
                using var colorRoi = new Cv.Mat();
                Cv.Cv2.Resize(colorSmall, colorRoi, new Cv.Size(roi.Width, roi.Height), 0, 0, Cv.InterpolationFlags.Nearest);

                // 返回新建的 BGR 8U 图：把 ROI 区域替换成彩色掩膜
                var outImg = VpUtil.ToBgr(cur);
                using (var roiDest = new Cv.Mat(outImg, roi)) colorRoi.CopyTo(roiDest);

                long maxPixels = 0; int distinct = 0;
                for (int c = 0; c < classes; c++)
                {
                    if (counts[c] > 0) distinct++;
                    if (counts[c] > maxPixels) maxPixels = counts[c];
                }
                double ratio = (H * W) > 0 ? (double)maxPixels / (H * W) : 0;
                var nums = new List<(string, double)>();
                nums.Add(("类别数", distinct));
                nums.Add(("最大类像素数", maxPixels));
                nums.Add(("最大类占比", ratio * 100));
                nums.Add(("有效像素数", valid));
                string summary = "分割出 " + distinct + " 类，最大类占 " + VpUtil.F(ratio * 100, 1)
                    + "%，有效像素 " + valid + " 个";
                VpUtil.Add(report, s, spec, true, summary, ratio, distinct, "", nums.ToArray());
                VpUtil.Progress(progress, "ONNX分割：" + summary);
                return outImg;   // 换图：交回调用方替换 cur
            }
            catch (Exception ex)
            {
                Fail(report, s, spec, "ONNX分割推理失败：" + ex.Message, progress);
                return cur;
            }
        }

        // ---------------- 4. 文字检测 + Windows OCR 识别 ----------------
        private static Cv.Mat DlOcr(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, IProgress<string>? progress)
        {
            string model = VpUtil.Txt(spec, s, "model");
            var roi = VpUtil.Roi(spec, s, cur);
            string lang = VpUtil.Txt(spec, s, "lang");
            string expected = VpUtil.Txt(spec, s, "expected");
            string match = VpUtil.Txt(spec, s, "match");
            bool ignoreCase = VpUtil.Bool(spec, s, "ignoreCase", true);
            try
            {
                // 收集待识别的文字框（整图坐标）
                var boxes = new List<Cv.Rect>();
                if (model.Length > 0)
                {
                    if (!File.Exists(model))
                    {
                        Fail(report, s, spec, "文本检测模型文件不存在：" + model, progress);
                        return cur;
                    }
                    // 无 size 参数：按 ROI 较大边派生输入边长，避免写死不相关常数
                    int detSize = Math.Max(64, Math.Min(1280, Math.Max(roi.Width, roi.Height)));
                    using var crop = VpUtil.Crop(cur, roi);
                    using var bgr = VpUtil.ToBgr(crop);
                    using var blob = Cv.Dnn.CvDnn.BlobFromImage(bgr, 1.0, new Cv.Size(detSize, detSize),
                        new Cv.Scalar(0, 0, 0), swapRB: true, crop: false);
                    using var net = Cv.Dnn.CvDnn.ReadNetFromOnnx(model);
                    net.SetInput(blob, "");
                    using var prob = net.Forward("");
                    if (!ParseDetections(prob, out var dets, out string err))
                    {
                        Fail(report, s, spec, "文本检测：" + err, progress);
                        return cur;
                    }
                    // 只保留较高分的框、按分排序、限量，避免对成百上千个候选框逐个 OCR
                    foreach (var d in dets.Where(d => d.Conf >= 0.3)
                                           .OrderByDescending(d => d.Conf).Take(20))
                    {
                        int x1 = ClampInt(roi.X + d.X1 * roi.Width, 0, cur.Cols);
                        int y1 = ClampInt(roi.Y + d.Y1 * roi.Height, 0, cur.Rows);
                        int x2 = ClampInt(roi.X + d.X2 * roi.Width, 0, cur.Cols);
                        int y2 = ClampInt(roi.Y + d.Y2 * roi.Height, 0, cur.Rows);
                        var r = ClampRect(new Cv.Rect(x1, y1, x2 - x1, y2 - y1), cur.Cols, cur.Rows);
                        if (r.Width >= 2 && r.Height >= 2) boxes.Add(r);
                    }
                }
                else
                {
                    boxes.Add(ClampRect(roi, cur.Cols, cur.Rows));   // 整块 ROI 当一个框
                }

                var allText = new StringBuilder();
                int lineCount = 0;
                foreach (var box in boxes)
                {
                    if (box.Width < 2 || box.Height < 2) continue;
                    using var boxMat = new Cv.Mat(cur, box);
                    if (!TryOcr(boxMat, lang, out string txt, out var lineRects, out int nLines, out string oerr))
                    {
                        Fail(report, s, spec, oerr, progress);   // WinRT OCR 不可用 → 如实失败
                        return cur;
                    }
                    if (txt.Length > 0) { if (allText.Length > 0) allText.Append('\n'); allText.Append(txt); }
                    lineCount += nLines;
                    // 逐行词框画到注释画布（绿），外框画蓝
                    Cv.Cv2.Rectangle(dst, box, VpUtil.Dot, 1);
                    foreach (var lr in lineRects)
                        Cv.Cv2.Rectangle(dst, new Cv.Rect(box.X + lr.X, box.Y + lr.Y, lr.Width, lr.Height), VpUtil.Ink, 1);
                }

                string recognized = allText.ToString();
                bool pass = EvalMatch(recognized, expected, match, ignoreCase, out double score);
                int charCount = Compact(recognized).Length;
                var nums = new List<(string, double)>();
                nums.Add(("识别到的字数", charCount));
                nums.Add(("文本行数", lineCount));
                nums.Add(("是否合格", pass ? 1 : 0));
                nums.Add(("匹配分数", score));
                string summary = "识别到 " + charCount + " 字 / " + lineCount + " 行，"
                    + (expected.Length > 0 ? (pass ? "符合期望" : "不符合期望") : "（未设期望）")
                    + "，匹配分数 " + VpUtil.F(score, 2);
                VpUtil.Add(report, s, spec, pass, summary, charCount, lineCount, recognized, nums.ToArray());
                VpUtil.Progress(progress, "文字识别：" + (recognized.Length > 0 ? recognized : "（空）"));
            }
            catch (Exception ex)
            {
                Fail(report, s, spec, "文字检测+识别失败：" + ex.Message, progress);
            }
            return cur;
        }

        // ---------------- 5. 样本学习分类（无需外部模型） ----------------
        private static Cv.Mat ElClassify(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            string mode = VpUtil.Txt(spec, s, "mode");
            string folder = VpUtil.Txt(spec, s, "folder");
            string label = VpUtil.Txt(spec, s, "label");
            var roi = VpUtil.Roi(spec, s, cur);
            int bins = Math.Max(4, VpUtil.Int(spec, s, "bins", 16));
            int k = Math.Max(1, VpUtil.Int(spec, s, "k", 3));
            double maxDist = VpUtil.Num(spec, s, "maxDist", 0);
            var st = VpUtil.State(ctx);

            if (mode == "清空")
            {
                int n = st.Samples.Count;
                st.Samples.Clear();
                st.LastLabel = "";
                VpUtil.Add(report, s, spec, true, "已清空样本库，共清除 " + n + " 个样本", 0, n);
                VpUtil.Progress(progress, "样本学习：已清空 " + n + " 个样本");
                return cur;
            }

            if (mode == "分类")
            {
                if (st.Samples.Count == 0)
                {
                    Fail(report, s, spec, "还没学样本，请先跑一次「加样本」", progress);
                    return cur;
                }
                try
                {
                    using var crop = VpUtil.Crop(cur, roi);
                    double[] feat = ComputeFeature(crop, bins);
                    var ds = new List<(double D, string L)>();
                    foreach (var smp in st.Samples) ds.Add((Dist(feat, smp.Feature), smp.Label));
                    var sorted = ds.OrderBy(x => x.D).ToList();
                    int kk = Math.Min(k, sorted.Count);
                    var votes = new Dictionary<string, int>(StringComparer.Ordinal);
                    for (int i = 0; i < kk; i++)
                    {
                        if (!votes.ContainsKey(sorted[i].L)) votes[sorted[i].L] = 0;
                        votes[sorted[i].L]++;
                    }
                    string winner = ""; int winVotes = -1;
                    foreach (var kv in votes) if (kv.Value > winVotes) { winVotes = kv.Value; winner = kv.Key; }
                    double conf = kk > 0 ? (double)winVotes / kk : 0;
                    double nearest = sorted.Count > 0 ? sorted[0].D : 0;
                    string result = (maxDist > 0 && nearest > maxDist) ? "未知" : winner;
                    st.LastLabel = result;

                    Cv.Cv2.PutText(dst, "判定：" + result + " (" + VpUtil.F(conf * 100, 0) + "%)",
                        new Cv.Point(roi.X + 4, Math.Max(16, roi.Y + 22)),
                        Cv.HersheyFonts.HersheySimplex, 0.6, VpUtil.Ink, 2);
                    var nums = new List<(string, double)>();
                    nums.Add(("置信度", conf));
                    nums.Add(("已学样本数", st.Samples.Count));
                    nums.Add(("最近距离", nearest));
                    string summary = "判定「" + result + "」，置信度 " + VpUtil.F(conf * 100, 0)
                        + "%，最近距离 " + VpUtil.F(nearest, 3);
                    VpUtil.Add(report, s, spec, true, summary, conf, st.Samples.Count, result, nums.ToArray());
                    VpUtil.Progress(progress, "样本分类：" + summary);
                }
                catch (Exception ex) { Fail(report, s, spec, "样本分类失败：" + ex.Message, progress); }
                return cur;
            }

            // 加样本（默认）
            try
            {
                int added = 0;
                if (folder.Length > 0)
                {
                    if (!Directory.Exists(folder))
                    {
                        Fail(report, s, spec, "样本目录不存在或不是文件夹：" + folder, progress);
                        return cur;
                    }
                    string[] exts = { ".png", ".jpg", ".jpeg", ".bmp" };
                    foreach (var sub in Directory.GetDirectories(folder))
                    {
                        string cls = Path.GetFileName(sub);
                        if (cls.Length == 0) continue;
                        foreach (var file in Directory.GetFiles(sub))
                        {
                            string e = Path.GetExtension(file).ToLowerInvariant();
                            if (Array.IndexOf(exts, e) < 0) continue;
                            using var im = Cv.Cv2.ImRead(file, Cv.ImreadModes.Color);
                            if (im.Empty()) continue;
                            double[] f = ComputeFeature(im, bins);
                            st.Samples.Add((f, cls));
                            added++;
                        }
                    }
                }
                else
                {
                    string cls = label.Length > 0 ? label : "未命名";
                    using var crop = VpUtil.Crop(cur, roi);
                    double[] f = ComputeFeature(crop, bins);
                    st.Samples.Add((f, cls));
                    added = 1;
                }
                var nums = new List<(string, double)>();
                nums.Add(("已学样本数", st.Samples.Count));
                string msg = "本次加样本 " + added + " 个，样本库共 " + st.Samples.Count + " 个";
                VpUtil.Add(report, s, spec, true, msg, 0, added, "", nums.ToArray());
                VpUtil.Progress(progress, "样本学习：" + msg);
            }
            catch (Exception ex) { Fail(report, s, spec, "加样本失败：" + ex.Message, progress); }
            return cur;
        }

        // ===================== 共用工具 =====================

        private sealed class Det   // 归一化坐标的检测结果
        {
            public double X1, Y1, X2, Y2, Conf;
            public int Class;
        }

        private sealed class Box   // 整图像素坐标的检测框
        {
            public int X1, Y1, X2, Y2;
            public double Conf;
            public int Class;
        }

        private static void Fail(VisionReport report, VisualFlowStep s, VisionOpSpec spec,
            string msg, IProgress<string>? progress)
        {
            VpUtil.Add(report, s, spec, false, msg);
            VpUtil.Progress(progress, msg);
        }

        private static bool HaveModel(VisionOpSpec spec, VisualFlowStep s, VisionReport report,
            IProgress<string>? progress, out string path)
        {
            path = VpUtil.Txt(spec, s, "model");
            if (path.Length == 0 || !File.Exists(path))
            {
                Fail(report, s, spec, "还没设置模型文件（.onnx），请先填模型路径", progress);
                return false;
            }
            return true;
        }

        private static string[]? LoadLabels(string path)
        {
            if (path.Length > 0 && File.Exists(path))
            {
                try { return File.ReadAllLines(path); } catch { return null; }
            }
            return null;
        }

        private static string LabelOf(string[]? labels, int cls)
        {
            if (labels != null && cls >= 0 && cls < labels.Length && labels[cls].Trim().Length > 0)
                return labels[cls].Trim();
            return "类别#" + cls;
        }

        /// <summary>把任意维 CV_32F 输出展平成 1 行，再用 GetGenericIndexer&lt;float&gt; 摊成 float[]。</summary>
        private static float[] FlattenF32(Cv.Mat m)
        {
            Cv.Mat src = m;
            Cv.Mat? owned = null;
            if (!m.IsContinuous() || m.Type() != Cv.MatType.CV_32F)
            {
                owned = new Cv.Mat();
                m.ConvertTo(owned, Cv.MatType.CV_32F, 1.0, 0.0);   // 统一成连续 CV_32F 深度
                src = owned;
            }
            try
            {
                // Reshape(1,1) 把 [1,1,N,7] / [1,C,H,W] / [1,N] 等任意维展平成 1 行 N 列
                using var row = src.Reshape(1, 1);
                int n = row.Cols;
                var arr = new float[n];
                var idx = row.GetGenericIndexer<float>();
                for (int i = 0; i < n; i++) arr[i] = idx[0, i];
                return arr;
            }
            catch
            {
                // 个别运行时对 4D blob 的 Reshape 不支持 → 直接按连续内存摊平，结果一致
                int n = (int)src.Total() * src.Channels();
                var arr = new float[n];
                if (n > 0) Marshal.Copy(src.Data, arr, 0, n);
                return arr;
            }
            finally { owned?.Dispose(); }
        }

        /// <summary>通用检测输出解析：SSD [1,1,N,7] 或 YOLO [1,N,5+C]/[1,N,6]。</summary>
        private static bool ParseDetections(Cv.Mat prob, out List<Det> dets, out string err)
        {
            dets = new List<Det>();
            err = "";
            int dims = prob.Dims;
            int[] shape = new int[dims];
            for (int i = 0; i < dims; i++) shape[i] = prob.Size(i);
            float[] data = FlattenF32(prob);
            int stride = dims > 0 ? shape[dims - 1] : 0;

            // SSD：[1,1,N,7] = (batch,class,conf,x1,y1,x2,y2)，坐标归一化
            if (dims == 4 && shape[0] == 1 && shape[1] == 1 && stride == 7)
            {
                int n = shape[2];
                for (int i = 0; i < n; i++)
                {
                    int b = i * 7;
                    if (b + 6 >= data.Length) break;
                    dets.Add(new Det
                    {
                        Class = (int)data[b + 1], Conf = data[b + 2],
                        X1 = data[b + 3], Y1 = data[b + 4], X2 = data[b + 5], Y2 = data[b + 6]
                    });
                }
                return true;
            }
            // YOLO：[1,N,5+C] / [1,N,6] = (cx,cy,w,h,obj,类别分数/单类)，坐标归一化
            if (dims == 3 && shape[0] == 1 && stride >= 6)
            {
                int n = shape[1];
                for (int i = 0; i < n; i++)
                {
                    int b = i * stride;
                    if (b + stride - 1 >= data.Length) break;
                    double cx = data[b], cy = data[b + 1], w = data[b + 2], h = data[b + 3], obj = data[b + 4];
                    int cls;
                    if (stride == 6) cls = (int)Math.Round(data[b + 5]);   // 单类别导出常把类别放第 6 位
                    else
                    {
                        float best = -1e30f; cls = 0;
                        for (int j = 5; j < stride; j++)
                        {
                            float v = data[b + j];
                            if (v > best) { best = v; cls = j - 5; }
                        }
                    }
                    dets.Add(new Det
                    {
                        Class = cls, Conf = obj,
                        X1 = cx - w / 2, Y1 = cy - h / 2, X2 = cx + w / 2, Y2 = cy + h / 2
                    });
                }
                return true;
            }
            err = "模型输出形状无法识别（Dims=" + dims + "，各维 " + string.Join("x", shape) + "）。"
                + "本算子支持 [1,1,N,7]（SSD：类别/置信度/归一化 x1,y1,x2,y2）"
                + "或 [1,N,5+C] / [1,N,6]（YOLO：cx,cy,w,h,obj,类别分数）。请换用这两种输出格式的检测模型。";
            return false;
        }

        private static double IoU(Box a, Box b)
        {
            double ix1 = Math.Max(a.X1, b.X1), iy1 = Math.Max(a.Y1, b.Y1);
            double ix2 = Math.Min(a.X2, b.X2), iy2 = Math.Min(a.Y2, b.Y2);
            double inter = Math.Max(0, ix2 - ix1) * Math.Max(0, iy2 - iy1);
            double ua = Math.Max(0, (double)a.X2 - a.X1) * Math.Max(0, (double)a.Y2 - a.Y1);
            double ub = Math.Max(0, (double)b.X2 - b.X1) * Math.Max(0, (double)b.Y2 - b.Y1);
            double uni = ua + ub - inter;
            return uni <= 0 ? 0 : inter / uni;
        }

        /// <summary>基于 IoU 的贪心 NMS（类无关），按置信度从高到低抑制重叠框。</summary>
        private static List<Box> Nms(List<Box> dets, double thr)
        {
            int[] order = Enumerable.Range(0, dets.Count).ToArray();
            Array.Sort(order, (a, b) => dets[b].Conf.CompareTo(dets[a].Conf));
            var keep = new List<Box>();
            var suppressed = new bool[dets.Count];
            foreach (int idx in order)
            {
                if (suppressed[idx]) continue;
                keep.Add(dets[idx]);
                for (int j = 0; j < dets.Count; j++)
                {
                    if (j == idx || suppressed[j]) continue;
                    if (IoU(dets[idx], dets[j]) > thr) suppressed[j] = true;
                }
            }
            return keep;
        }

        private static int ClampInt(double v, int lo, int hi) => (int)Math.Max(lo, Math.Min(hi, v));

        private static Cv.Rect ClampRect(Cv.Rect r, int W, int H)
        {
            int x = Math.Max(0, Math.Min(r.X, W - 1));
            int y = Math.Max(0, Math.Min(r.Y, H - 1));
            int x2 = Math.Max(0, Math.Min(r.X + r.Width, W));
            int y2 = Math.Max(0, Math.Min(r.Y + r.Height, H));
            return new Cv.Rect(x, y, Math.Max(0, x2 - x), Math.Max(0, y2 - y));
        }

        /// <summary>固定确定性 BGR 调色板：背景黑，其余按类别号稳定生成。</summary>
        private static byte[] BuildPalette(int n)
        {
            var lut = new byte[n * 3];
            for (int c = 0; c < n; c++)
            {
                int o = c * 3;
                if (c == 0) { lut[o] = 0; lut[o + 1] = 0; lut[o + 2] = 0; continue; }
                lut[o] = (byte)(((c * 199) % 156) + 100);     // B
                lut[o + 1] = (byte)(((c * 151) % 156) + 100); // G
                lut[o + 2] = (byte)(((c * 73) % 156) + 100);  // R
            }
            return lut;
        }

        // ---- 样本特征：归一化 B/G/R 直方图 + 灰度均值/方差 + Canny 边缘密度 ----
        private static double[] ComputeFeature(Cv.Mat img, int bins)
        {
            using var bgr0 = VpUtil.ToBgr(img);
            using var bgr = new Cv.Mat();
            if (bgr0.Type() == Cv.MatType.CV_8UC3) bgr0.CopyTo(bgr);
            else bgr0.ConvertTo(bgr, Cv.MatType.CV_8U, 1.0, 0.0);   // 兜底：统一成 8U 三通道

            // 展平成 1 行，用 GetGenericIndexer<byte> 顺序读每个像素的 B,G,R
            using var flat = bgr.Reshape(1, 1);
            int n = flat.Cols / 3;                       // 像素数
            var idx = flat.GetGenericIndexer<byte>();
            var hb = new double[bins]; var hg = new double[bins]; var hr = new double[bins];
            double sum = 0, sq = 0;
            for (int p = 0; p < n; p++)
            {
                int o = p * 3;
                byte b = idx[0, o], g = idx[0, o + 1], r = idx[0, o + 2];
                hb[Math.Min(bins - 1, b * bins / 256)]++;
                hg[Math.Min(bins - 1, g * bins / 256)]++;
                hr[Math.Min(bins - 1, r * bins / 256)]++;
                double y = 0.114 * b + 0.587 * g + 0.299 * r;
                sum += y; sq += y * y;
            }
            double mean = n > 0 ? sum / n : 0;
            double std = Math.Sqrt(n > 0 ? Math.Max(0, sq / n - mean * mean) : 0);

            using var gray = VpUtil.ToGray(bgr);
            using var edges = new Cv.Mat();
            Cv.Cv2.Canny(gray, edges, 60, 180);
            double edgeD = n > 0 ? (double)Cv.Cv2.CountNonZero(edges) / n : 0;

            var f = new double[3 * bins + 3];
            int q = 0;
            for (int i = 0; i < bins; i++) f[q++] = n > 0 ? hb[i] / n : 0;
            for (int i = 0; i < bins; i++) f[q++] = n > 0 ? hg[i] / n : 0;
            for (int i = 0; i < bins; i++) f[q++] = n > 0 ? hr[i] / n : 0;
            f[q++] = mean / 255.0;
            f[q++] = std / 255.0;
            f[q] = edgeD;
            return f;
        }

        private static double Dist(double[] a, double[] b)
        {
            int n = Math.Min(a.Length, b.Length);
            double s = 0;
            for (int i = 0; i < n; i++) { double d = a[i] - b[i]; s += d * d; }
            return Math.Sqrt(s);
        }

        private static string Compact(string t)
        {
            var sb = new StringBuilder(t.Length);
            foreach (var ch in t) if (!char.IsWhiteSpace(ch)) sb.Append(ch);
            return sb.ToString();
        }

        /// <summary>最长公共子序列长度（真实计算，用于匹配分数）。</summary>
        private static int Lcs(string a, string b)
        {
            if (a.Length == 0 || b.Length == 0) return 0;
            var prev = new int[b.Length + 1];
            var cur = new int[b.Length + 1];
            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                    cur[j] = a[i - 1] == b[j - 1] ? prev[j - 1] + 1 : Math.Max(prev[j], cur[j - 1]);
                var tmp = prev; prev = cur; cur = tmp;
            }
            return prev[b.Length];
        }

        private static bool EvalMatch(string recognized, string expected, string mode, bool ignoreCase, out double score)
        {
            score = 0;
            if (expected.Length == 0)
            {
                score = recognized.Length > 0 ? 1.0 : 0.0;
                return recognized.Length > 0;
            }
            string a = Compact(ignoreCase ? recognized.ToLowerInvariant() : recognized);
            string b = Compact(ignoreCase ? expected.ToLowerInvariant() : expected);
            bool ok;
            switch (mode)
            {
                case "正则":
                    try { ok = Regex.IsMatch(recognized, expected, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None); }
                    catch { ok = false; }
                    break;
                case "完全一样": ok = a == b; break;
                case "开头是": ok = a.StartsWith(b, StringComparison.Ordinal); break;
                default: ok = a.Contains(b); break;   // 包含
            }
            score = b.Length > 0 ? (double)Lcs(a, b) / b.Length : 0;
            return ok;
        }

        /// <summary>把任意 Mat 转成连续 BGRA 字节（供 SoftwareBitmap 使用）。</summary>
        private static byte[] ToBgraBytes(Cv.Mat src)
        {
            using var bgr = VpUtil.ToBgr(src);   // 连续 CV_8UC3
            int n = (int)bgr.Total();
            var bgrBytes = new byte[(int)bgr.Total() * bgr.Channels()];
            Marshal.Copy(bgr.Data, bgrBytes, 0, bgrBytes.Length);
            var bgra = new byte[n * 4];
            for (int i = 0; i < n; i++)
            {
                bgra[i * 4] = bgrBytes[i * 3];
                bgra[i * 4 + 1] = bgrBytes[i * 3 + 1];
                bgra[i * 4 + 2] = bgrBytes[i * 3 + 2];
                bgra[i * 4 + 3] = 255;
            }
            return bgra;
        }

        private static Windows.Media.Ocr.OcrEngine? ResolveOcr(string lang)
        {
            try
            {
                if (lang.Length == 0 || lang == "自动")
                    return Windows.Media.Ocr.OcrEngine.TryCreateFromUserProfileLanguages();
                string tag = lang switch
                {
                    "中文" or "简体中文" => "zh-Hans-CN",
                    "英文" => "en-US",
                    _ => lang,   // 也允许直接写 BCP-47 标签
                };
                return Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language(tag))
                       ?? Windows.Media.Ocr.OcrEngine.TryCreateFromUserProfileLanguages();
            }
            catch { return null; }
        }

        /// <summary>对一块裁剪图跑 Windows.Media.Ocr（同步阻塞），返回文本、逐行框、行数。</summary>
        private static bool TryOcr(Cv.Mat crop, string lang, out string text, out List<Cv.Rect> lineRects,
            out int nLines, out string err)
        {
            text = ""; lineRects = new List<Cv.Rect>(); nLines = 0; err = "";
            try
            {
                byte[] bytes = ToBgraBytes(crop);
                var buf = Windows.Security.Cryptography.CryptographicBuffer.CreateFromByteArray(bytes);
                using var sb = Windows.Graphics.Imaging.SoftwareBitmap.CreateCopyFromBuffer(
                    buf, Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, crop.Width, crop.Height,
                    Windows.Graphics.Imaging.BitmapAlphaMode.Ignore);
                var engine = ResolveOcr(lang);
                if (engine == null)
                {
                    err = "系统未安装所需 OCR 语言包，请在「设置 → 时间和语言 → 语言和区域」里添加可选功能「光学字符识别」";
                    return false;
                }
                var ocr = engine.RecognizeAsync(sb).AsTask().GetAwaiter().GetResult();
                var txt = new StringBuilder();
                if (ocr?.Lines != null)
                {
                    foreach (var line in ocr.Lines)
                    {
                        string lt = (line.Text ?? "").Trim();
                        nLines++;
                        if (lt.Length > 0) { if (txt.Length > 0) txt.Append('\n'); txt.Append(lt); }
                        int bx = int.MaxValue, by = int.MaxValue, ex = int.MinValue, ey = int.MinValue;
                        if (line.Words != null)
                            foreach (var w in line.Words)
                            {
                                var br = w.BoundingRect;
                                if (br.Width <= 0 || br.Height <= 0) continue;
                                bx = Math.Min(bx, (int)Math.Floor(br.X));
                                by = Math.Min(by, (int)Math.Floor(br.Y));
                                ex = Math.Max(ex, (int)Math.Ceiling(br.X + br.Width));
                                ey = Math.Max(ey, (int)Math.Ceiling(br.Y + br.Height));
                            }
                        if (bx != int.MaxValue)
                            lineRects.Add(new Cv.Rect(bx, by, Math.Max(1, ex - bx), Math.Max(1, ey - by)));
                    }
                }
                text = txt.ToString();
                return true;
            }
            catch (Exception ex)
            {
                err = "Windows OCR 调用失败：" + ex.Message;
                return false;
            }
        }
    }
}
