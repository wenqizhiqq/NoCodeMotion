// === NoCodeMotion 视觉标定标记质心检测（纯托管 4 连通域） | 作者：温​启‎志‍ ‍|⁣ ⁣微‏信‎：‏1‏8‌7‍1‌9‎3⁠6‏1​3​9‌9‌ ⁠|​ ​保‎留‍所⁠有‍权⁣利‏，⁣请‌勿‍删‎除 ===
namespace NoCodeMotion.Services.Vision.Calibration
{
    /// <summary>标记（圆点）检测参数。</summary>
    public sealed class MarkerDetectOptions
    {
        /// <summary>亮度阈值 0~255。</summary>
        public int Threshold { get; set; } = 128;

        /// <summary>true：找暗标记（亮底黑点）；false：找亮标记（暗底白点）。</summary>
        public bool DarkMarker { get; set; } = true;

        /// <summary>最小面积（像素）。</summary>
        public int MinArea { get; set; } = 30;

        /// <summary>最大面积（像素）。</summary>
        public int MaxArea { get; set; } = 400000;

        /// <summary>填充率下限（面积 / 外接矩形面积；实心圆约 0.78）。</summary>
        public double MinFillRatio { get; set; } = 0.55;

        /// <summary>填充率上限（用来排除方框 / 整块背景）。</summary>
        public double MaxFillRatio { get; set; } = 0.95;

        /// <summary>外接矩形长宽比上限。</summary>
        public double MaxAspect { get; set; } = 1.8;

        /// <summary>圆度下限 4πA/P²（P 为裂纹周长；实心圆约 0.6~0.9）。</summary>
        public double MinCircularity { get; set; } = 0.35;

        /// <summary>最多返回多少个候选（按面积降序）。</summary>
        public int MaxBlobs { get; set; } = 32;
    }

    /// <summary>检测到的标记连通域：质心 + 形状指标。</summary>
    public sealed class MarkerBlob
    {
        /// <summary>质心 u（像素）。</summary>
        public double CenterU { get; set; }
        /// <summary>质心 v（像素）。</summary>
        public double CenterV { get; set; }
        /// <summary>像素面积。</summary>
        public int Area { get; set; }
        /// <summary>外接矩形最小 u。</summary>
        public int MinU { get; set; }
        /// <summary>外接矩形最小 v。</summary>
        public int MinV { get; set; }
        /// <summary>外接矩形最大 u。</summary>
        public int MaxU { get; set; }
        /// <summary>外接矩形最大 v。</summary>
        public int MaxV { get; set; }
        /// <summary>外接矩形宽（像素）。</summary>
        public int BoxWidth => MaxU - MinU + 1;
        /// <summary>外接矩形高（像素）。</summary>
        public int BoxHeight => MaxV - MinV + 1;
        /// <summary>填充率 = 面积 / 外接矩形面积。</summary>
        public double FillRatio { get; set; }
        /// <summary>圆度 = 4πA/P²。</summary>
        public double Circularity { get; set; }

        /// <summary>外接矩形长宽比（≥1）。</summary>
        public double Aspect
        {
            get
            {
                int a = BoxWidth, b = BoxHeight;
                int hi = a > b ? a : b, lo = a < b ? a : b;
                return lo <= 0 ? 0 : (double)hi / lo;
            }
        }

        /// <summary>一行摘要（写日志 / 结果说明用）。</summary>
        public string Describe()
        {
            return "(" + CalibSolver.Fmt(CenterU) + ", " + CalibSolver.Fmt(CenterV) + ") px，面积 "
                + Area.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "，填充率 " + CalibSolver.Fmt(FillRatio)
                + "，圆度 " + CalibSolver.Fmt(Circularity);
        }
    }

    /// <summary>
    /// 标记质心检测（纯托管实现：亮度阈值 + 4 连通域 + 质心/面积/填充率/圆度）。
    /// 刻意不依赖 OpenCV —— 标定是最需要「可离线断言」的一环，
    /// opencv_world480.dll 不在 DLL 搜索路径时冒烟里也要能跑通。
    /// </summary>
    public static class MarkerDetector
    {
        /// <summary>
        /// 在 BGRA 缓冲里找标记连通域，按面积降序返回。
        /// 输入约定与 VisionEngine 的 report.Bgra 一致：字节序 B,G,R,A。
        /// </summary>
        public static List<MarkerBlob> Detect(byte[]? bgra, int width, int height, MarkerDetectOptions? opt = null)
        {
            var list = new List<MarkerBlob>();
            if (bgra == null || width <= 0 || height <= 0) return list;
            if (bgra.LongLength < (long)width * height * 4) return list;
            var o = opt ?? new MarkerDetectOptions();

            int n = width * height;
            int thr = Math.Max(0, Math.Min(255, o.Threshold));
            var fg = new bool[n];
            for (int i = 0; i < n; i++)
            {
                int b = bgra[i * 4], g = bgra[i * 4 + 1], r = bgra[i * 4 + 2];
                int lum = (r * 299 + g * 587 + b * 114) / 1000;
                fg[i] = o.DarkMarker ? lum < thr : lum >= thr;
            }

            var visited = new bool[n];
            var stack = new Stack<int>();
            for (int start = 0; start < n; start++)
            {
                if (!fg[start] || visited[start]) continue;
                visited[start] = true;
                stack.Clear();
                stack.Push(start);

                int area = 0;
                long su = 0, sv = 0;
                int minU = int.MaxValue, minV = int.MaxValue, maxU = int.MinValue, maxV = int.MinValue;
                int perim = 0;

                while (stack.Count > 0)
                {
                    int idx = stack.Pop();
                    int u = idx % width, v = idx / width;
                    area++; su += u; sv += v;
                    if (u < minU) minU = u;
                    if (u > maxU) maxU = u;
                    if (v < minV) minV = v;
                    if (v > maxV) maxV = v;

                    // 裂纹周长：四邻越界或非前景各记一条单位边
                    if (u == 0 || !fg[idx - 1]) perim++;
                    if (u == width - 1 || !fg[idx + 1]) perim++;
                    if (v == 0 || !fg[idx - width]) perim++;
                    if (v == height - 1 || !fg[idx + width]) perim++;

                    if (u > 0 && fg[idx - 1] && !visited[idx - 1]) { visited[idx - 1] = true; stack.Push(idx - 1); }
                    if (u < width - 1 && fg[idx + 1] && !visited[idx + 1]) { visited[idx + 1] = true; stack.Push(idx + 1); }
                    if (v > 0 && fg[idx - width] && !visited[idx - width]) { visited[idx - width] = true; stack.Push(idx - width); }
                    if (v < height - 1 && fg[idx + width] && !visited[idx + width]) { visited[idx + width] = true; stack.Push(idx + width); }
                }

                if (area < o.MinArea || area > o.MaxArea) continue;
                int bw = maxU - minU + 1, bh = maxV - minV + 1;
                int hi = bw > bh ? bw : bh, lo = bw < bh ? bw : bh;
                double aspect = lo <= 0 ? double.MaxValue : (double)hi / lo;
                if (aspect > o.MaxAspect) continue;
                double fill = (double)area / Math.Max(1, bw * bh);
                if (fill < o.MinFillRatio || fill > o.MaxFillRatio) continue;
                double circ = perim <= 0 ? 0.0 : 4.0 * Math.PI * area / ((double)perim * perim);
                if (circ < o.MinCircularity) continue;

                list.Add(new MarkerBlob
                {
                    CenterU = (double)su / area,
                    CenterV = (double)sv / area,
                    Area = area,
                    MinU = minU,
                    MinV = minV,
                    MaxU = maxU,
                    MaxV = maxV,
                    FillRatio = fill,
                    Circularity = circ
                });
                if (list.Count >= o.MaxBlobs) break;
            }

            list.Sort((a, b) => b.Area.CompareTo(a.Area));
            return list;
        }

        /// <summary>取面积最大的标记；没有则返回 null 并给出原因。</summary>
        public static MarkerBlob? DetectLargest(byte[]? bgra, int width, int height,
            MarkerDetectOptions? opt, out string error)
        {
            var all = Detect(bgra, width, height, opt);
            if (all.Count == 0)
            {
                error = "未检测到标记（请检查亮度阈值、明暗标记、面积区间，以及视野内是否真的有标记）";
                return null;
            }
            error = "";
            return all[0];   // Detect 已按面积降序
        }
    }
}
// === NoCodeMotion 视觉标定标记质心检测（纯托管 4 连通域） | 作者：温⁣启⁠志‌ ‌|​ ​微⁠信​：‎1‎8‎7‎1⁠9‎3​6‏1‎3‎9⁠9⁠ ​|⁣ ‌保⁣留​所⁠有​权‍利‏，⁠请‎勿‏删‍除 ===
