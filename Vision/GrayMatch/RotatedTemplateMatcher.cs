// ============================================================
// 温启志◆编写◇微信﹕187◆1936◇1399
// ============================================================
// ============================================================
// 温启志◆编写◇微信﹕187◆1936◇1399
// ============================================================
// ============================================================
// 温启志◆编写◇微信﹕187◆1936◇1399
// ============================================================
using OpenCvSharp;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace GrayMatch;

public class RotatedTemplateMatcher : IDisposable
{
    private Mat? _source;
    private Mat? _template;
    private Mat? _sourceGray;
    private Mat? _sourceContour;   // 轮廓/梯度图（用于轮廓匹配模式）
    private Mat? _templateContour;
    private byte[]? _templateContourMask;   // 模板边缘二值掩码（用于 UI 画绿色轮廓线）
    private int _templateContourMaskW, _templateContourMaskH;

    // 保护 _sourceGray/_template 等共享 Mat：加载/设置与匹配互斥，防止匹配过程中 Mat 被释放。
    private readonly SemaphoreSlim _dataLock = new(1, 1);

    public Mat Source => _source ?? throw new InvalidOperationException("Source image not loaded.");
    public Mat? Template => _template;
    public bool HasSource => _sourceGray != null;

    /// <summary>
    /// 轮廓匹配开关。开启后，匹配改用「边缘梯度图」（Sobel 梯度幅度，归一化到 0-255
    /// 单通道）代替灰度图，对光照变化、前景/背景灰度接近、纯形状轮廓（与纹理无关）
    /// 的图案更鲁棒。缺陷检测仍基于灰度差异，不受影响。
    /// setter 会按需生成/释放轮廓图，避免重复计算。
    /// </summary>
    private bool _useContour;
    public bool UseContour
    {
        get => _useContour;
        set
        {
            if (_useContour == value) return;
            _dataLock.Wait();
            try
            {
                _useContour = value;
                if (value)
                {
                    if (_sourceGray != null) { _sourceContour?.Dispose(); _sourceContour = MakeContour(_sourceGray); }
                    if (_template != null) { _templateContour?.Dispose(); _templateContour = MakeContour(_template); }
                }
                else
                {
                    _sourceContour?.Dispose(); _sourceContour = null;
                    _templateContour?.Dispose(); _templateContour = null;
                }
            }
            finally
            {
                _dataLock.Release();
            }
        }
    }

    /// <summary>
    /// 轮廓匹配参数：Canny 低阈值（1-254）。值越大保留的边越少（越严格）。
    /// 影响显示的绿色轮廓，以及（开启轮廓匹配时）边缘掩码的生成。默认 30。
    /// </summary>
    private int _contourThreshold = 30;
    public int ContourThreshold
    {
        get => _contourThreshold;
        set
        {
            if (value < 1) value = 1;
            if (value > 254) value = 254;
            _contourThreshold = value;
        }
    }

    /// <summary>
    /// 轮廓匹配参数：高斯预平滑 sigma（0 = 不模糊，上限 12）。会同时作用于
    /// 匹配用的梯度图与边缘掩码，抑制噪声带来的伪边。默认 1。
    /// </summary>
    private double _contourBlur = 1.0;
    public double ContourBlur
    {
        get => _contourBlur;
        set
        {
            if (value < 0) value = 0;
            if (value > 12) value = 12;
            _contourBlur = value;
        }
    }

    /// <summary>
    /// 多尺度匹配范围（模板大小的倍数）：0 = 关闭（仅原尺寸匹配）。
    /// 取值 0~0.6，表示在 [1-ScaleRange, 1+ScaleRange] 区间内按若干尺度搜索。
    /// 例如 0.3 ? 模板在 0.7×~1.3× 大小范围内都能被找到。默认 0（关闭）。
    /// </summary>
    private double _scaleRange = 0.0;
    public double ScaleRange
    {
        get => _scaleRange;
        set
        {
            if (value < 0) value = 0;
            if (value > 0.6) value = 0.6;
            _scaleRange = value;
        }
    }

    /// <summary>Pure matching time (ms) of the last Match call, excluding template
    /// cache construction — reported by the native layer so template creation and
    /// UI drawing are never counted.</summary>
    public double LastMatchMs { get; private set; }

    public void LoadSource(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("图片路径为空或空白。", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException($"找不到图片文件：{path}", path);

        _dataLock.Wait();
        try
        {
            DisposeSource();
            _source = Cv2.ImRead(path, ImreadModes.Color);
            if (_source == null || _source.Empty())
                throw new InvalidOperationException($"无法读取图片：{path}。文件可能已损坏、被占用，或格式不受 OpenCV 支持。");

            _sourceGray = new Mat();
            Cv2.CvtColor(_source, _sourceGray, ColorConversionCodes.BGR2GRAY);
            if (UseContour) _sourceContour = MakeContour(_sourceGray);
        }
        finally
        {
            _dataLock.Release();
        }
    }

    public void SetSource(Mat image)
    {
        _dataLock.Wait();
        try
        {
            DisposeSource();
            _source = image.Clone();
            _sourceGray = new Mat();
            if (_source.Channels() == 1)
                _source.CopyTo(_sourceGray);
            else
                Cv2.CvtColor(_source, _sourceGray, ColorConversionCodes.BGR2GRAY);
        }
        finally
        {
            _dataLock.Release();
        }
    }

    public void SetTemplateFromRoi(Rect roi)
    {
        _dataLock.Wait();
        try
        {
            if (_sourceGray == null) throw new InvalidOperationException("Source image not loaded.");
            _template?.Dispose();
            _template = new Mat(_sourceGray, roi);
            _template = _template.Clone();
            if (UseContour) _templateContour = MakeContour(_template);
            ComputeTemplateContourMask();
        }
        finally
        {
            _dataLock.Release();
        }
    }

    public void SetTemplate(Mat templateGray)
    {
        _dataLock.Wait();
        try
        {
            _template?.Dispose();
            _template = templateGray.Clone();
            if (UseContour) _templateContour = MakeContour(_template);
            ComputeTemplateContourMask();
        }
        finally
        {
            _dataLock.Release();
        }
    }

    /// <summary>
    /// Rotation-invariant NCC matching. The heavy lifting runs in the native
    /// GrayModelNative DLL (built from C++); this method only marshals image data.
    /// </summary>
    public List<MatchResult> Match(
        int pyramidLevels,
        double angleStart,
        double angleEnd,
        double angleStep,
        double nccThreshold,
        double maxOverlap,
        int topN,
        int denseMode = 0)
    {
        _dataLock.Wait();
        try
        {
            if (_sourceGray == null || _template == null)
                throw new InvalidOperationException("Source and template must be set before matching.");

            // Contour/edge images lose thin edges after Gaussian pyramid downsampling,
            // so coarse seeds become unreliable and rotated targets are dropped.
            // Force the legacy full-resolution two-pass path for contour mode.
            if (_useContour) pyramidLevels = 0;

            if (_scaleRange <= 0)
                return MatchSingleScale(_sourceGray, _sourceContour, pyramidLevels, angleStart, angleEnd,
                    angleStep, nccThreshold, maxOverlap, topN, denseMode, 1.0);

        // 多尺度：在 [1-ScaleRange, 1+ScaleRange] 内取若干尺度，把原图按比例缩小/放大后
        // 用原尺寸模板匹配（目标在该尺度下恰好与模板等大），再把结果映射回原图坐标。
        int steps = Math.Max(3, (int)Math.Round(_scaleRange / 0.1) + 1); // 0.1 一步，至少 3 步
        var all = new List<MatchResult>();
        double totalMs = 0;
        for (int k = 0; k < steps; k++)
        {
            double sf = steps == 1 ? 1.0 : (1.0 - _scaleRange + 2.0 * _scaleRange * k / (steps - 1));
            double srcFactor = 1.0 / sf;   // 原图缩放倍数：目标大(sf>1)?原图缩小
            using var srcScaled = new Mat();
            Cv2.Resize(_sourceGray, srcScaled, new OpenCvSharp.Size(0, 0), srcFactor, srcFactor, InterpolationFlags.Linear);
            Mat? contourScaled = null;
            if (UseContour && _sourceContour != null)
            {
                contourScaled = new Mat();
                Cv2.Resize(_sourceContour, contourScaled, new OpenCvSharp.Size(0, 0), srcFactor, srcFactor, InterpolationFlags.Linear);
            }
            var res = MatchSingleScale(srcScaled, contourScaled, pyramidLevels, angleStart, angleEnd,
                angleStep, nccThreshold, maxOverlap, topN, denseMode, sf);
            totalMs += LastMatchMs;   // 每次单尺度调用都会刷新 LastMatchMs
            if (contourScaled != null) contourScaled.Dispose();
            all.AddRange(res);
        }

        LastMatchMs = totalMs;
        // 跨尺度去重：同一目标在相邻尺度会被重复检出，按中心邻近+角度接近合并，保留最高分。
        var deduped = NmsAcrossScales(all, maxOverlap, angleStep);
        if (deduped.Count > topN) deduped = deduped.GetRange(0, topN);
        for (int i = 0; i < deduped.Count; i++) deduped[i].Index = i + 1;
        return deduped;
        }
        finally
        {
            _dataLock.Release();
        }
    }

    // 单尺度匹配：在给定（已缩放的）原图上用原尺寸模板跑 native NCC。mapFactor 用于把
    // 检测结果从缩放坐标映射回原图坐标（中心/尺寸 × mapFactor，角度不变），并记录 Scale=mapFactor。
    private List<MatchResult> MatchSingleScale(
        Mat srcGray, Mat? srcContour,
        int pyramidLevels, double angleStart, double angleEnd, double angleStep,
        double nccThreshold, double maxOverlap, int topN, int denseMode, double mapFactor)
    {
        var srcMatch = (UseContour && srcContour != null) ? srcContour : srcGray;
        var tplMatch = (UseContour && _templateContour != null) ? _templateContour! : _template!;

        IntPtr handle;
        try
        {
            handle = gm_create();
        }
        catch (DllNotFoundException ex)
        {
            throw new InvalidOperationException(
                "无法加载 GrayModelNative.dll。请确保以下文件与程序可执行文件(GrayMatch.Wpf.exe)位于同一目录：\n" +
                "  GrayModelNative.dll\n  opencv_world480.dll\n" +
                "  vcruntime140.dll、vcruntime140_1.dll、msvcp140.dll、concrt140.dll、vcomp140.dll\n" +
                "（后几个是 VC++ 2022 运行库；若目标电脑没装 Visual Studio 或 VC++ 可再发行包就会缺失，\n" +
                " 会导致 GrayModelNative.dll 加载失败。请从开发机的输出目录把整个文件夹一起拷贝。）\n" +
                "原始错误：" + ex.Message, ex);
        }
        catch (BadImageFormatException ex)
        {
            throw new InvalidOperationException(
                "GrayModelNative.dll 加载失败：很可能是 32 位 / 64 位不匹配。本程序需要 64 位 Windows，" +
                "且 GrayModelNative.dll 为 x64 版本，请将程序以 x64 运行。\n原始错误：" + ex.Message, ex);
        }
        if (handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create native matcher.");

        try
        {
            int s = gm_set_source(handle, srcMatch.Data, srcMatch.Width, srcMatch.Height, (int)srcMatch.Step(), 1);
            int t = gm_set_template(handle, tplMatch.Data, tplMatch.Width, tplMatch.Height, (int)tplMatch.Step(), 1);
            if (s != 0 || t != 0)
                throw new InvalidOperationException("Failed to set source/template in native matcher.");

            // Dense mode can return far more than topN distinct matches on a regular array,
            // so grow the native result buffer when enabled to avoid silent truncation.
            int bufSize = denseMode != 0 ? Math.Max(topN, 4096) : topN;
            var buffer = new GmMatchResult[bufSize];
            int written = gm_match(
                handle, pyramidLevels, angleStart, angleEnd, angleStep,
                nccThreshold, maxOverlap, topN, denseMode, buffer, buffer.Length);

            if (written < 0)
                return new List<MatchResult>();

            LastMatchMs = gm_get_last_match_ms(handle);

            var results = new List<MatchResult>(written);
            for (int i = 0; i < written; i++)
            {
                var r = buffer[i];
                results.Add(new MatchResult
                {
                    Index = i + 1,
                    Score = r.score,
                    CenterX = r.centerX * mapFactor,
                    CenterY = r.centerY * mapFactor,
                    Angle = r.angle,
                    TemplateWidth = (int)Math.Round(r.templateWidth * mapFactor),
                    TemplateHeight = (int)Math.Round(r.templateHeight * mapFactor),
                    LeftTopX = (int)Math.Round(r.leftTopX * mapFactor),
                    LeftTopY = (int)Math.Round(r.leftTopY * mapFactor),
                    Level = r.level,
                    Scale = mapFactor
                });
            }
            return results;
        }
        finally
        {
            gm_destroy(handle);
        }
    }

    // 跨尺度去重：删除同一目标在不同尺度下的重复检出。判定为重复的条件：
    // 中心距离 < 0.4 × 较小框短边，且角度差在 2×角度步长内。保留分数更高者。
    private static List<MatchResult> NmsAcrossScales(List<MatchResult> all, double maxOverlap, double angleStep)
    {
        var sorted = all.OrderByDescending(r => r.Score).ToList();
        var kept = new List<MatchResult>();
        foreach (var cand in sorted)
        {
            bool dup = false;
            int minSide = Math.Min(cand.TemplateWidth, cand.TemplateHeight);
            double tol = Math.Max(2.0, 0.4 * minSide);
            double angTol = Math.Max(2.0, 2.0 * angleStep);
            foreach (var k in kept)
            {
                double dx = cand.CenterX - k.CenterX;
                double dy = cand.CenterY - k.CenterY;
                if (Math.Sqrt(dx * dx + dy * dy) < tol && Math.Abs(cand.Angle - k.Angle) < angTol)
                {
                    dup = true;
                    break;
                }
            }
            if (!dup) kept.Add(cand);
        }
        return kept;
    }


    /// <summary>
    /// Tunables for <see cref="DetectDefects(List{MatchResult})"/>. Kept as a mutable property so
    /// the UI can push slider/textbox edits straight in without threading the values through
    /// every call site.
    /// </summary>
    public DefectOptions DefectOptions { get; set; } = new();

    /// <summary>
    /// Runs defect detection using the current <see cref="DefectOptions"/>.
    /// Overload resolution prefers this exact-arity method over the fully-parameterised one,
    /// so existing <c>DetectDefects(results)</c> call sites automatically pick up UI settings.
    /// </summary>
    public List<DefectResult> DetectDefects(List<MatchResult> results)
    {
        var o = DefectOptions ?? new DefectOptions();
        return DetectDefects(results, o.DiffThreshold, o.MinAreaFrac, o.GlobalBrightnessThresh,
                             o.EdgeTolerance, o.EdgeGradThresh, o.ErodeSize, o.DilateSize);
    }

    /// <summary>Wall-clock milliseconds spent inside the last <c>DetectDefects</c> call.</summary>
    public double LastDefectMs { get; private set; }

    /// <summary>
    /// Per-match defect detection. For each matched instance we inverse-rotate the source
    /// region by -angle so it becomes upright and aligned with the template, compute the
    /// absolute grayscale difference, threshold it to an anomaly mask, cluster connected
    /// components, and heuristically classify each into one of four defect types:
    ///   污渍/异物 (dark blob), 划痕 (elongated bright/dark line),
    ///   缺料/破损 (large missing/darker region), 亮度/对比度异常 (global brightness/contrast shift).
    /// Defects are returned in upright template-local coords plus a precomputed image-space
    /// center so the UI can draw the red overlay without re-deriving the rotation transform.
    /// </summary>
    public List<DefectResult> DetectDefects(
        List<MatchResult> results,
        double diffThreshold = 45,
        double minAreaFrac = 0.004,
        double globalBrightnessThresh = 28,
        int edgeTolerance = 0,
        double edgeGradThresh = 30,
        int erodeSize = 2,
        int dilateSize = 3)
    {
        _dataLock.Wait();
        try
        {
        var swDefect = System.Diagnostics.Stopwatch.StartNew();
        var outList = new List<DefectResult>();
        if (_sourceGray == null || _template == null || results == null || results.Count == 0)
        {
            LastDefectMs = 0;
            return outList;
        }

        var srcGray = _sourceGray;      // cached by LoadSource — no per-call full-image conversion
        var tmpl = _template;
        int tw = tmpl.Width, th = tmpl.Height;
        int minDim = Math.Min(tw, th);

        double minArea = minAreaFrac * tw * th;
        const double maxAreaFrac = 0.60;    // above this it is a whole-instance shift, not a local defect
        // sub-pixel / sub-degree pose error always lights up the instance outline: ignore a border band
        int margin = Math.Max(2, (int)Math.Round(0.04 * minDim));

        // -----------------------------------------------------------------------------------
        // Edge-tolerance band.
        // Sub-pixel / sub-degree pose error makes EVERY strong grey-level transition of the part
        // light up in the difference image, so contours kept appearing on the outline and on
        // internal contour junctions instead of on real defects. A morphological gradient
        // (dilate - erode) marks exactly those transitions; dilating it by `edgeTolerance` px
        // turns them into a "don't care" band that is subtracted from the defect mask.
        // Computed once per call (template-sized, O(tw*th)) and shared read-only by all threads.
        // -----------------------------------------------------------------------------------
        Mat? edgeBand = null;
        if (edgeTolerance > 0)
        {
            edgeBand = new Mat();
            using (var gk = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)))
                Cv2.MorphologyEx(tmpl, edgeBand, MorphTypes.Gradient, gk);
            Cv2.Threshold(edgeBand, edgeBand, edgeGradThresh, 255, ThresholdTypes.Binary);
            int ks = edgeTolerance * 2 + 1;
            using (var ek = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(ks, ks)))
                Cv2.Dilate(edgeBand, edgeBand, ek);
        }

        var buckets = new List<DefectResult>[results.Count];

        try
        {

        System.Threading.Tasks.Parallel.For(0, results.Count, i =>
        {
            var local = new List<DefectResult>();
            buckets[i] = local;

            var r = results[i];
            double ang = r.Angle;
            var center = new Point2f((float)r.CenterX, (float)r.CenterY);

            // 多尺度匹配时目标实际大小为 tw*Scale × th*Scale（Scale 默认 1）。
            // 先把该大小的旋转区域抠出来，再缩放到模板尺寸 tw×th 再与模板比对。
            int sw = Math.Max(4, (int)Math.Round(tw * r.Scale));
            int sh = Math.Max(4, (int)Math.Round(th * r.Scale));

            // Fold "upright the whole image" + "crop the template window" into ONE affine warp.
            using var m = Cv2.GetRotationMatrix2D(center, -ang, 1.0);
            double ox = r.CenterX - (sw - 1) * 0.5;
            double oy = r.CenterY - (sh - 1) * 0.5;
            m.Set<double>(0, 2, m.Get<double>(0, 2) - ox);
            m.Set<double>(1, 2, m.Get<double>(1, 2) - oy);

            using var patch = new Mat();
            if (sw == tw && sh == th)
            {
                Cv2.WarpAffine(srcGray, patch, m, new OpenCvSharp.Size(tw, th), InterpolationFlags.Linear, BorderTypes.Replicate);
            }
            else
            {
                using var patchBig = new Mat();
                Cv2.WarpAffine(srcGray, patchBig, m, new OpenCvSharp.Size(sw, sh), InterpolationFlags.Linear, BorderTypes.Replicate);
                Cv2.Resize(patchBig, patch, new OpenCvSharp.Size(tw, th), 0, 0, InterpolationFlags.Linear);
            }

            using var diff = new Mat();
            Cv2.Absdiff(tmpl, patch, diff);
            double meanDiff = Cv2.Mean(diff).Val0;

            using var mask = new Mat();
            Cv2.Threshold(diff, mask, diffThreshold, 255, ThresholdTypes.Binary);

            // (a) drop everything sitting on a template grey-level transition — that is pose
            //     error on the part outline / internal contour junctions, never a defect.
            if (edgeBand != null) Cv2.Subtract(mask, edgeBand, mask);

            // (b) erode: thin residual edge slivers collapse to nothing.
            if (erodeSize > 1)
            {
                using var ek = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(erodeSize, erodeSize));
                Cv2.Erode(mask, mask, ek);
            }

            // (c) dilate: restore the true footprint of what survived and re-bridge a scratch
            //     that the erosion broke into dashes. Keep dilate >= erode.
            if (dilateSize > 1)
            {
                using var dk = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(dilateSize, dilateSize));
                Cv2.Dilate(mask, mask, dk);
            }

            // (d) erase the outer border band (whole-instance outline).
            Cv2.Rectangle(mask, new Rect(0, 0, tw, th), Scalar.All(0), margin * 2);

            Cv2.FindContours(mask, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            bool foundLocal = false;
            for (int ci = 0; ci < contours.Length; ci++)
            {
                var c = contours[ci];
                double area = Cv2.ContourArea(c);
                if (area < minArea) continue;
                double areaFrac = area / (double)(tw * th);
                if (areaFrac > maxAreaFrac) continue;

                // tight, rotation-invariant footprint — an axis-aligned bbox of a diagonal scratch
                // is enormous, which is exactly why the old overlay looked oversized.
                var minRect = Cv2.MinAreaRect(c);
                double mw = minRect.Size.Width, mh = minRect.Size.Height;
                double longSide = Math.Max(mw, mh), shortSide = Math.Min(mw, mh);
                double ar = longSide / Math.Max(1.0, shortSide);

                using var cmask = new Mat(th, tw, MatType.CV_8UC1, Scalar.All(0));
                Cv2.DrawContours(cmask, new List<OpenCvSharp.Point[]> { c }, 0, Scalar.All(255), -1);
                double mT = Cv2.Mean(tmpl, cmask).Val0;
                double mP = Cv2.Mean(patch, cmask).Val0;
                double sev = Cv2.Mean(diff, cmask).Val0;
                double delta = mP - mT; // >0 instance brighter than template, <0 darker

                bool dark = delta < -18;
                bool bright = delta > 18;
                bool big = areaFrac > 0.18;
                // a scratch must be genuinely thin AND long AND small in area
                bool elongated = ar >= 4.0
                                 && shortSide <= Math.Max(3.0, 0.22 * minDim)
                                 && longSide >= 0.15 * minDim
                                 && areaFrac <= 0.25;

                string type;
                if (elongated) type = "划痕";
                else if (dark && big) type = "缺料/破损";
                else if (dark) type = "污渍/异物";
                else if (bright) type = "亮度异常";
                else type = "亮度/对比度异常";

                var bbox = Cv2.BoundingRect(c);

                // tight-box center: template-local -> image space, using the SAME -angle transform
                // the UI applies to the green match box.
                double ux = minRect.Center.X - tw / 2.0;
                double uy = minRect.Center.Y - th / 2.0;
                double phi = -ang * Math.PI / 180.0;
                double cosv = Math.Cos(phi), sinv = Math.Sin(phi);
                double imgCx = r.CenterX + (ux * cosv - uy * sinv);
                double imgCy = r.CenterY + (ux * sinv + uy * cosv);

                local.Add(new DefectResult
                {
                    CenterX = r.CenterX,
                    CenterY = r.CenterY,
                    Angle = r.Angle,
                    Tw = tw,
                    Th = th,
                    LeftTopX = r.LeftTopX,
                    LeftTopY = r.LeftTopY,
                    X = bbox.X,
                    Y = bbox.Y,
                    W = mw,
                    H = mh,
                    RectAngle = -ang + minRect.Angle,
                    ImgCx = imgCx,
                    ImgCy = imgCy,
                    Type = type,
                    Score = sev,
                    // carry the actual defective pixels (template-local) so the UI can paint them red
                    Pixels = CopyMask(cmask, tw, th),
                    Pw = tw,
                    Ph = th
                });
                foundLocal = true;
            }

            // No localized defect, but the instance as a whole is off in brightness/contrast.
            // Draw a small centred badge — NOT a full-template red block.
            if (!foundLocal && meanDiff > globalBrightnessThresh)
            {
                double badge = Math.Min(40.0, Math.Max(12.0, 0.22 * minDim));
                local.Add(new DefectResult
                {
                    CenterX = r.CenterX,
                    CenterY = r.CenterY,
                    Angle = r.Angle,
                    Tw = tw,
                    Th = th,
                    LeftTopX = r.LeftTopX,
                    LeftTopY = r.LeftTopY,
                    X = (int)Math.Round((tw - badge) / 2.0),
                    Y = (int)Math.Round((th - badge) / 2.0),
                    W = badge,
                    H = badge,
                    RectAngle = -ang,
                    ImgCx = r.CenterX,
                    ImgCy = r.CenterY,
                    Type = "亮度/对比度异常",
                    Score = meanDiff
                });
            }
        });

        for (int i = 0; i < buckets.Length; i++)
            if (buckets[i] != null) outList.AddRange(buckets[i]);

        }
        finally
        {
            edgeBand?.Dispose();
            swDefect.Stop();
            LastDefectMs = swDefect.Elapsed.TotalMilliseconds;
        }

        return outList;
        }
        finally
        {
            _dataLock.Release();
        }
    }

    /// <summary>Copies a continuous CV_8UC1 mask (th rows x tw cols) into a managed byte[].</summary>
    private static byte[] CopyMask(Mat mask, int tw, int th)
    {
        var buf = new byte[tw * th];
        System.Runtime.InteropServices.Marshal.Copy(mask.Data, buf, 0, buf.Length);
        return buf;
    }


    #region Native interop

    [StructLayout(LayoutKind.Sequential)]
    private struct GmMatchResult
    {
        public double score;
        public double centerX;
        public double centerY;
        public double angle;
        public int templateWidth;
        public int templateHeight;
        public int leftTopX;
        public int leftTopY;
        public int level;
    }

    private const string NativeLib = "GrayModelNative";

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr gm_create();

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void gm_destroy(IntPtr handle);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int gm_set_source(IntPtr handle, IntPtr data, int w, int h, int step, int channels);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int gm_set_template(IntPtr handle, IntPtr data, int w, int h, int step, int channels);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int gm_match(
        IntPtr handle,
        int pyramidLevels,
        double angleStart,
        double angleEnd,
        double angleStep,
        double nccThreshold,
        double maxOverlap,
        int topN,
        int denseMode,
        [In, Out] GmMatchResult[] outResults,
        int maxResults);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern double gm_get_last_match_ms(IntPtr handle);

    #endregion

    /// <summary>
    /// 由灰度图生成「轮廓/边缘」图：Sobel 梯度幅度（CV_32F）取模后归一化到 0-255 的
    /// 单通道 8 位图。归一化保证了与灰度图完全相同的内存布局（单通道、行连续），
    /// 因此 native 的 NCC 路径无需任何改动即可直接使用，且对光照变化更鲁棒。
    /// </summary>
    public byte[]? TemplateContourMask => _templateContourMask;
    public int TemplateContourW => _templateContourMaskW;
    public int TemplateContourH => _templateContourMaskH;

    /// <summary>
    /// 由灰度模板生成「边缘二值掩码」：Canny 边缘检测，输出 1/0 的掩码（1 = 边缘像素）。
    /// 供 UI 在轮廓匹配模式下把模板形状用绿色线条画到每个匹配位置上。
    /// 模板尺寸很小（仅模板大小），开销可忽略。
    /// </summary>
    private void ComputeTemplateContourMask()
    {
        _templateContourMask = null;
        _templateContourMaskW = 0;
        _templateContourMaskH = 0;
        if (_template == null) return;
        int w = _template.Width, h = _template.Height;
        var mask = new byte[w * h];

        // 可选预模糊（轮廓匹配参数之一）：抑制噪声伪边
        Mat? blurred = null;
        Mat work = _template;
        if (_contourBlur > 0)
        {
            blurred = new Mat();
            Cv2.GaussianBlur(_template, blurred, new OpenCvSharp.Size(0, 0), _contourBlur, _contourBlur);
            work = blurred;
        }

        // 第一选择：Canny 边缘（更细的线条）。低阈值由轮廓参数控制（越大边越少）。
        using (var edges = new Mat())
        {
            Cv2.Canny(work, edges, _contourThreshold, Math.Max(_contourThreshold * 2.0, _contourThreshold + 5));
            int count = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (edges.At<byte>(y, x) != 0) { mask[y * w + x] = 1; count++; }

            // 边缘太少（如低对比模板 / 阈值过高）时回退：用 Sobel 梯度幅度的阈值作为边缘，
            // 保证轮廓线至少有可见的模板形状。
            if (count < 5)
            {
                using var gx = new Mat();
                using var gy = new Mat();
                using var mag = new Mat();
                Cv2.Sobel(work, gx, MatType.CV_32F, 1, 0, 3);
                Cv2.Sobel(work, gy, MatType.CV_32F, 0, 1, 3);
                Cv2.Magnitude(gx, gy, mag);
                double minv = 0, maxv = 0;
                Cv2.MinMaxLoc(mag, out minv, out maxv);
                double thr = maxv * 0.25;
                if (thr > 1)
                {
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            float v = mag.At<float>(y, x);
                            if (v >= thr) mask[y * w + x] = 1;
                        }
                }
            }
        }
        blurred?.Dispose();

        _templateContourMask = mask;
        _templateContourMaskW = w;
        _templateContourMaskH = h;
    }

    /// <summary>
    /// 在「平滑 / 阈值」参数变化后调用：刷新模板边缘掩码（绿色轮廓显示用），
    /// 以及（开启轮廓匹配时）重新生成喂给 native 的梯度图。模板/原图尺寸很小，开销可忽略。
    /// </summary>
    public void RecomputeContours()
    {
        _dataLock.Wait();
        try
        {
            if (_template != null)
            {
                ComputeTemplateContourMask();
                if (UseContour) { _templateContour?.Dispose(); _templateContour = MakeContour(_template); }
            }
            if (UseContour && _sourceGray != null)
            {
                _sourceContour?.Dispose();
                _sourceContour = MakeContour(_sourceGray);
            }
        }
        finally
        {
            _dataLock.Release();
        }
    }

    private Mat MakeContour(Mat gray)
    {
        // 可选预模糊（轮廓匹配参数之一）
        Mat? blurred = null;
        Mat work = gray;
        if (_contourBlur > 0)
        {
            blurred = new Mat();
            Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(0, 0), _contourBlur, _contourBlur);
            work = blurred;
        }
        using var gx = new Mat();
        using var gy = new Mat();
        Cv2.Sobel(work, gx, MatType.CV_32F, 1, 0, 3);
        Cv2.Sobel(work, gy, MatType.CV_32F, 0, 1, 3);
        using var mag = new Mat();
        Cv2.Magnitude(gx, gy, mag);
        var outp = new Mat();
        Cv2.Normalize(mag, outp, 0, 255, NormTypes.MinMax, MatType.CV_8U);
        blurred?.Dispose();
        return outp;
    }

    private void DisposeSource()
    {
        _source?.Dispose();
        _sourceGray?.Dispose();
        _sourceContour?.Dispose();
        _source = null;
        _sourceGray = null;
        _sourceContour = null;
    }

    public void Dispose()
    {
        DisposeSource();
        _template?.Dispose();
        _templateContour?.Dispose();
        _template = null;
        _templateContour = null;
        // 不 Dispose _dataLock：如果匹配/缺陷检测线程正持有它，Dispose 会抛异常。
        // 程序退出时由终结器回收即可。
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Tunables for template-comparison defect detection. Defaults match the historical
/// hard-coded values, so behaviour is unchanged until the user edits the panel.
/// </summary>
public class DefectOptions
{
    /// <summary>Grey-level difference above which a pixel is anomalous (1..254).</summary>
    public double DiffThreshold { get; set; } = 45;

    /// <summary>Connected regions smaller than this fraction of the template area are noise.</summary>
    public double MinAreaFrac { get; set; } = 0.004;

    /// <summary>Mean difference above which an instance gets a global brightness badge.</summary>
    public double GlobalBrightnessThresh { get; set; } = 28;

    /// <summary>
    /// Half-width (px) of the "don't care" band grown around every template grey-level
    /// transition — an extra safety net for parts whose outline still leaks through.
    ///
    /// Off by default: measured on a contour-heavy part across 0.2°–3.0° of pose error, the
    /// erode/dilate pass alone already removed 100% of the outline false positives, while a
    /// 3-px band additionally swallowed a genuine 20-px scratch (it shrank to 8 px, and to
    /// nothing without the dilate). Turn it on (2–4) only if outline artefacts survive, and
    /// expect defects that touch a contour to be attenuated.
    /// </summary>
    public int EdgeTolerance { get; set; } = 0;

    /// <summary>Morphological-gradient strength that counts as a template edge (1..254).</summary>
    public double EdgeGradThresh { get; set; } = 30;

    /// <summary>Erosion kernel size; &lt;= 1 skips erosion. Removes thin edge slivers.</summary>
    public int ErodeSize { get; set; } = 2;

    /// <summary>Dilation kernel size; &lt;= 1 skips dilation. Restores the true defect footprint.</summary>
    public int DilateSize { get; set; } = 3;
}
