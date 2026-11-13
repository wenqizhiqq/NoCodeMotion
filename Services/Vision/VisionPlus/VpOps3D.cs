// === NoCodeMotion 视觉流程引擎（VisionPlus 算子移植 · OpenCV 真实实现） | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Cv = OpenCvSharp;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Vision
{
    /// <summary>
    /// 深度图 / 3D 量测算子。共享数据只有一处：<see cref="VpState.ZMap"/>
    /// （CV_32FC1，单位 mm，float.NaN = 无效点）与 <see cref="VpState.PlaneA"/> / PlaneB
    /// （double[3] = (a,b,c)，z = a·x + b·y + c）。所有数值都是真实从深度图算出来的。
    /// </summary>
    internal static class VpOps3D
    {
        const string NoZMap = "还没载入深度图，请先跑「载入深度图(ZMap)」";

        public static Cv.Mat Run(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            switch (spec.Key)
            {
                case "zmapload":   return ZMapLoad(spec, s, cur, dst, report, ctx, progress);
                case "zmap2img":   return ZMap2Img(spec, s, cur, dst, report, ctx, progress);
                case "planefit":   return PlaneFit(spec, s, cur, dst, report, ctx, progress);
                case "heightcalc": return HeightCalc(spec, s, cur, dst, report, ctx, progress);
                case "volumecalc": return VolumeCalc(spec, s, cur, dst, report, ctx, progress);
                case "anglepp":    return AnglePP(spec, s, cur, dst, report, ctx, progress);
                case "thickness":  return Thickness(spec, s, cur, dst, report, ctx, progress);
                default:           return cur;
            }
        }

        // ---------------- 公共小工具 ----------------

        /// <summary>取深度图；没载入时记一条诚实的失败并让调用方直接返回 cur。</summary>
        static Cv.Mat? ZMap(VisionOpSpec spec, VisualFlowStep s, VisionReport report, VisionOpContext ctx)
        {
            var z = VpUtil.State(ctx).ZMap;
            if (z == null || z.Empty())
            {
                VpUtil.Add(report, s, spec, false, NoZMap);
                return null;
            }
            return z;
        }

        static bool Same(double a, double b)
            => Math.Abs(a - b) <= 1e-6 * (1.0 + Math.Abs(a) + Math.Abs(b));

        /// <summary>扫一遍 ZMap 求有效点的真实范围与个数（mm）。</summary>
        static void ZRange(Cv.Mat z, out double lo, out double hi, out int valid, out int invalidCnt)
        {
            lo = double.PositiveInfinity; hi = double.NegativeInfinity;
            valid = 0; invalidCnt = 0;
            for (int y = 0; y < z.Rows; y++)
                for (int x = 0; x < z.Cols; x++)
                {
                    float v = z.At<float>(y, x);
                    if (float.IsNaN(v)) { invalidCnt++; continue; }
                    valid++;
                    if (v < lo) lo = v;
                    if (v > hi) hi = v;
                }
            if (valid == 0) { lo = 0; hi = 1; }
        }

        /// <summary>3×3 线性方程组高斯消元（列主元）；奇异/共线返回 false。</summary>
        static bool Solve3(double[,] M, double[] rhs, out double[] x)
        {
            x = new double[3];
            double scale = 0;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++) scale = Math.Max(scale, Math.Abs(M[i, j]));
            if (scale <= 0) return false;
            double eps = scale * 1e-12;              // 相对阈值：坐标很大时仍能识别共线

            var a = new double[3, 4];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++) a[i, j] = M[i, j];
                a[i, 3] = rhs[i];
            }
            for (int col = 0; col < 3; col++)
            {
                int piv = col;
                for (int r = col + 1; r < 3; r++)
                    if (Math.Abs(a[r, col]) > Math.Abs(a[piv, col])) piv = r;
                if (Math.Abs(a[piv, col]) < eps) return false;
                if (piv != col)
                    for (int j = col; j < 4; j++)
                    { double t = a[col, j]; a[col, j] = a[piv, j]; a[piv, j] = t; }
                double d = a[col, col];
                for (int j = col; j < 4; j++) a[col, j] /= d;
                for (int r = 0; r < 3; r++)
                {
                    if (r == col) continue;
                    double f = a[r, col];
                    if (f == 0) continue;
                    for (int j = col; j < 4; j++) a[r, j] -= f * a[col, j];
                }
            }
            x[0] = a[0, 3]; x[1] = a[1, 3]; x[2] = a[2, 3];
            return true;
        }

        /// <summary>平面法向量 (-a,-b,1) 归一化；返回 null 表示长度太小。</summary>
        static double[]? NormalFromPlane(double[] p)
        {
            double nx = -p[0], ny = -p[1], nz = 1.0;
            double len = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (len < 1e-12) return null;
            return new[] { nx / len, ny / len, nz / len };
        }

        /// <summary>法向量对水平面(0,0,1)的倾角（度）。</summary>
        static double Incline(double[] n)
            => Math.Acos(VpUtil.Clamp(Math.Abs(n[2]), 0, 1)) * 180.0 / Math.PI;

        /// <summary>在结果图上叠几行短文字（Hershey 字体只画拉丁/数字，中文放 Summary 里）。</summary>
        static void Stamp(Cv.Mat dst, bool ok, params string[] lines)
        {
            if (dst == null || dst.Empty()) return;
            var col = ok ? VpUtil.Ink : VpUtil.Warn;
            int y = 22;
            foreach (var ln in lines)
            {
                if (string.IsNullOrEmpty(ln)) continue;
                Cv.Cv2.PutText(dst, ln, new Cv.Point(8, y), Cv.HersheyFonts.HersheySimplex, 0.5, col, 1);
                y += 17;
                if (y > dst.Rows - 5) break;
            }
        }

        // =====================================================================
        // ① 载入深度图
        // =====================================================================
        static Cv.Mat ZMapLoad(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            string path = VpUtil.Txt(spec, s, "path");
            double unit = VpUtil.Num(spec, s, "unit", 0.001);
            double invalid = VpUtil.Num(spec, s, "invalid", 0);
            int cols = VpUtil.Int(spec, s, "cols", 0);
            if (unit == 0) unit = 1.0;                       // 0 系数没意义，兜底成不缩放
            VpUtil.Progress(progress, "载入深度图…");

            if (!File.Exists(path))
            {
                VpUtil.Add(report, s, spec, false,
                    "深度图文件不存在：" + (string.IsNullOrWhiteSpace(path) ? "（没填文件路径）" : path),
                    0, 0, path);
                return cur;
            }

            // counts = 原始计数（未乘 unit）的 CV_32FC1 单通道 Mat
            // 读文件异常交给 VisionOpRunner 外层 try 统一兜底，这里只处理能明确说清的失败
            Cv.Mat counts;
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".csv" || ext == ".txt")
            {
                counts = ParseCsv(path, cols);
            }
            else
            {
                using var raw = Cv.Cv2.ImRead(path, Cv.ImreadModes.Unchanged);
                if (raw == null || raw.Empty())
                {
                    VpUtil.Add(report, s, spec, false,
                        "深度图文件读不出来（不是有效的 16 位 PNG/TIFF）：" + path, 0, 0, path);
                    return cur;
                }
                counts = ToCounts(raw);                     // 统一 16U / 8U / 32F → CV_32F
            }

            if (counts.Empty())
            {
                counts.Dispose();
                VpUtil.Add(report, s, spec, false,
                    "深度图里没解析出任何数字（CSV 为空或格式不对）：" + path, 0, 0, path);
                return cur;
            }

            // 乘 unit 得 mm；等于 invalid 的点置 float.NaN
            var z = new Cv.Mat(counts.Rows, counts.Cols, Cv.MatType.CV_32FC1);
            float uf = (float)unit, iv = (float)invalid;
            int valid = 0;
            double zmin = double.PositiveInfinity, zmax = double.NegativeInfinity, sum = 0;
            for (int y = 0; y < counts.Rows; y++)
                for (int x = 0; x < counts.Cols; x++)
                {
                    float r = counts.At<float>(y, x);
                    float scaled = r * uf;
                    // invalid 既可能是原始计数值，也可能是 mm 值，两种口径都认
                    bool bad = Same(r, iv) || Same(scaled, iv);
                    z.Set<float>(y, x, bad ? float.NaN : scaled);
                    if (!bad)
                    {
                        valid++;
                        if (scaled < zmin) zmin = scaled;
                        if (scaled > zmax) zmax = scaled;
                        sum += scaled;
                    }
                }
            counts.Dispose();
            if (valid == 0) { zmin = 0; zmax = 0; }
            double mean = valid > 0 ? sum / valid : 0;

            var st = VpUtil.State(ctx);
            st.ZMap?.Dispose();                          // 换图先释放旧的
            st.ZMap = z;                                // 交给 VpState 长期持有，此处不 Dispose
            st.ZUnit = unit;

            bool ok = valid > 0;
            string summary = ok
                ? $"载入 {z.Cols}×{z.Rows} 深度图，有效点 {valid} 个，Z {VpUtil.F(zmin)}~{VpUtil.F(zmax)} mm，均值 {VpUtil.F(mean)} mm"
                : "深度图读到了但一个有效点都没有（可能「无效值」把整张图都屏蔽了），请检查无效值设置";
            VpUtil.Add(report, s, spec, ok, summary, mean, valid, path,
                ("宽", z.Cols), ("高", z.Rows), ("有效点数", valid),
                ("Z最小", zmin), ("Z最大", zmax), ("Z均值", mean));
            VpUtil.Progress(progress, ok ? $"深度图 {valid} 个有效点" : "深度图没有有效点");
            return cur;
        }

        /// <summary>读空白 / 逗号 / 中文逗号 / 分号分隔的数字，按 cols 列拼成 CV_32FC1。</summary>
        static Cv.Mat ParseCsv(string path, int colsHint)
        {
            var rows = new List<float[]>();
            int cols = colsHint;
            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var toks = line.Split(new[] { ' ', '\t', ',', '，', ';', '；' },
                    StringSplitOptions.RemoveEmptyEntries);
                var nums = new List<float>();
                foreach (var t in toks)
                    if (float.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out float f))
                        nums.Add(f);
                if (nums.Count == 0) continue;
                if (cols <= 0) cols = nums.Count;         // 0 = 按第一行推断列数
                var row = new float[cols];
                for (int i = 0; i < cols; i++) row[i] = i < nums.Count ? nums[i] : 0f;
                rows.Add(row);
            }
            if (cols <= 0 || rows.Count == 0) return new Cv.Mat();
            var m = new Cv.Mat(rows.Count, cols, Cv.MatType.CV_32FC1);
            for (int y = 0; y < rows.Count; y++)
                for (int x = 0; x < cols; x++)
                    m.Set<float>(y, x, rows[y][x]);
            return m;
        }

        /// <summary>ImRead 出来的 16U / 8U / 32F 统一转成单通道 CV_32FC1 原始计数。</summary>
        static Cv.Mat ToCounts(Cv.Mat raw)
        {
            Cv.Mat? tmpGray = null;
            Cv.Mat one = raw;
            if (raw.Channels() > 1) { tmpGray = VpUtil.ToGray(raw); one = tmpGray; }
            var f = new Cv.Mat();
            one.ConvertTo(f, Cv.MatType.CV_32FC1);       // 不缩放，保留原始计数值
            tmpGray?.Dispose();
            return f;
        }

        // =====================================================================
        // ② 深度图转可视图（换图类，返回新建的 BGR 8U Mat）
        // =====================================================================
        static Cv.Mat ZMap2Img(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var z = ZMap(spec, s, report, ctx);
            if (z == null) return cur;

            string mode = VpUtil.Txt(spec, s, "mode");
            double pmin = VpUtil.Num(spec, s, "zmin", 0);
            double pmax = VpUtil.Num(spec, s, "zmax", 0);
            int steps = Math.Max(2, VpUtil.Int(spec, s, "steps", 8));

            ZRange(z, out double alo, out double ahi, out int valid, out int invalidCnt);
            double lo = pmin != 0 ? pmin : alo;         // 0 = 自动取有效点最小/最大
            double hi = pmax != 0 ? pmax : ahi;
            if (hi <= lo) hi = lo + 1;
            double span = hi - lo;

            // 线性映射 zmin..zmax → 0..255；NaN → 0
            using var gray8 = new Cv.Mat(z.Rows, z.Cols, Cv.MatType.CV_8UC1);
            for (int y = 0; y < z.Rows; y++)
                for (int x = 0; x < z.Cols; x++)
                {
                    float v = z.At<float>(y, x);
                    byte b = 0;
                    if (!float.IsNaN(v))
                        b = (byte)VpUtil.Clamp(Math.Round((v - lo) / span * 255.0), 0, 255);
                    gray8.Set<byte>(y, x, b);
                }

            var bgr = new Cv.Mat();                     // 本算子新建的返回图，不 Dispose
            if (mode == "等高线")
            {
                // 量化成 steps 条带，再在条带边界上画轮廓线
                using var bands = new Cv.Mat(z.Rows, z.Cols, Cv.MatType.CV_8UC1);
                for (int y = 0; y < z.Rows; y++)
                    for (int x = 0; x < z.Cols; x++)
                    {
                        int g = gray8.At<byte>(y, x);
                        int band = (int)VpUtil.Clamp(Math.Round(g / 255.0 * (steps - 1)), 0, steps - 1);
                        bands.Set<byte>(y, x, (byte)Math.Round(band * 255.0 / (steps - 1)));
                    }
                Cv.Cv2.CvtColor(bands, bgr, Cv.ColorConversionCodes.GRAY2BGR);
                using var edges = new Cv.Mat();
                Cv.Cv2.Canny(bands, edges, 30, 90);
                bgr.SetTo(VpUtil.Dot, edges);           // 蓝色等高线叠在灰度底图上
            }
            else if (mode == "灰度")
            {
                Cv.Cv2.CvtColor(gray8, bgr, Cv.ColorConversionCodes.GRAY2BGR);
            }
            else // 伪彩
            {
                Cv.Cv2.ApplyColorMap(gray8, bgr, Cv.ColormapTypes.Jet);
            }

            string cn = mode == "等高线" ? "等高线" : (mode == "灰度" ? "灰度" : "伪彩");
            string summary = $"深度图转{cn}：Z {VpUtil.F(lo)}~{VpUtil.F(hi)} mm，有效 {valid} 点，无效 {invalidCnt} 点";
            VpUtil.Add(report, s, spec, true, summary, valid, valid, "",
                ("Z最小", lo), ("Z最大", hi), ("有效点数", valid), ("无效点数", invalidCnt));
            VpUtil.Progress(progress, summary);
            return bgr;
        }

        // =====================================================================
        // ③ 平面拟合：最小二乘 z = a·x + b·y + c
        // =====================================================================
        static Cv.Mat PlaneFit(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var z = ZMap(spec, s, report, ctx);
            if (z == null) return cur;

            var roi = VpUtil.Roi(spec, s, z);
            int step = Math.Max(1, VpUtil.Int(spec, s, "step", 2));
            double maxRms = VpUtil.Num(spec, s, "maxRms", 0);
            string slot = VpUtil.Txt(spec, s, "slot");
            bool toB = slot.Contains("平面B") || slot.Contains("B");

            // 正规方程累加：M·[a,b,c]^T = [Szx,Szy,Sz]^T
            double Sxx = 0, Sxy = 0, Sx = 0, Syy = 0, Sy = 0, S1 = 0, Szx = 0, Szy = 0, Sz = 0;
            int n = 0;
            int x0 = roi.X, y0 = roi.Y, x1 = roi.X + roi.Width, y1 = roi.Y + roi.Height;
            for (int y = y0; y < y1; y += step)
                for (int x = x0; x < x1; x += step)
                {
                    float v = z.At<float>(y, x);
                    if (float.IsNaN(v)) continue;
                    double xx = x, yy = y, zz = v;
                    Sxx += xx * xx; Sxy += xx * yy; Sx += xx;
                    Syy += yy * yy; Sy += yy; S1 += 1;
                    Szx += xx * zz; Szy += yy * zz; Sz += zz;
                    n++;
                }
            if (n < 3)
            {
                VpUtil.Add(report, s, spec, false, "框里有效深度点不足 3 个，拟合不了平面");
                return cur;
            }
            double[,] M = { { Sxx, Sxy, Sx }, { Sxy, Syy, Sy }, { Sx, Sy, S1 } };
            double[] rhs = { Szx, Szy, Sz };
            if (!Solve3(M, rhs, out var coef))
            {
                VpUtil.Add(report, s, spec, false,
                    "这些深度点几乎共线（法方程奇异），拟合不出唯一平面，请把框选放宽一点");
                return cur;
            }
            double a = coef[0], b = coef[1], c = coef[2];

            // 二次遍历算真实残差 RMS
            double ss = 0;
            int nn = 0;
            for (int y = y0; y < y1; y += step)
                for (int x = x0; x < x1; x += step)
                {
                    float v = z.At<float>(y, x);
                    if (float.IsNaN(v)) continue;
                    double r = v - (a * x + b * y + c);
                    ss += r * r; nn++;
                }
            double rms = nn > 0 ? Math.Sqrt(ss / nn) : 0;
            double incl = Math.Acos(VpUtil.Clamp(1.0 / Math.Sqrt(a * a + b * b + 1.0), -1, 1))
                          * 180.0 / Math.PI;

            var st = VpUtil.State(ctx);
            var plane = new double[] { a, b, c };
            if (toB) st.PlaneB = plane; else st.PlaneA = plane;

            bool ok = !(maxRms > 0 && rms > maxRms);
            string eq = $"z={VpUtil.F(a, 4)}·x+{VpUtil.F(b, 4)}·y+{VpUtil.F(c, 4)}";
            string summary = ok
                ? $"最小二乘拟合平面（{n} 点）：{eq}，倾角 {VpUtil.F(incl)}°，残差RMS {VpUtil.F(rms, 4)} mm → 存到{(toB ? "平面B" : "平面A")}"
                : $"拟合完成但残差RMS {VpUtil.F(rms, 4)} mm 超过上限 {VpUtil.F(maxRms)} mm，判不合格（框里可能有台阶或异物）";
            VpUtil.Add(report, s, spec, ok, summary, rms, n, eq,
                ("系数a", a), ("系数b", b), ("系数c", c),
                ("倾角", incl), ("残差RMS", rms), ("参与点数", n));
            Cv.Cv2.Rectangle(dst, roi, VpUtil.Ink, 1);
            Stamp(dst, ok,
                $"{(toB ? "B" : "A")}: a={VpUtil.F(a, 3)} b={VpUtil.F(b, 3)} c={VpUtil.F(c, 2)}",
                $"RMS={VpUtil.F(rms, 3)} inc={VpUtil.F(incl, 1)}deg n={n}");
            VpUtil.Progress(progress, $"平面拟合 倾角{VpUtil.F(incl)}° RMS{VpUtil.F(rms, 3)}");
            return cur;
        }

        // =====================================================================
        // ④ 高度测量
        // =====================================================================
        static Cv.Mat HeightCalc(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var z = ZMap(spec, s, report, ctx);
            if (z == null) return cur;

            var roi = VpUtil.Roi(spec, s, z);
            string refMode = VpUtil.Txt(spec, s, "ref");
            bool usePlane = !refMode.Contains("固定");   // 默认「拟合平面A」
            double baseZ = VpUtil.Num(spec, s, "baseZ", 0);
            int step = Math.Max(1, VpUtil.Int(spec, s, "step", 1));
            double maxH = VpUtil.Num(spec, s, "maxH", 0);
            double minH = VpUtil.Num(spec, s, "minH", 0);

            double[]? plane = null;
            if (usePlane)
            {
                plane = VpUtil.State(ctx).PlaneA;
                if (plane == null)
                {
                    VpUtil.Add(report, s, spec, false,
                        "选了「拟合平面A」当零面，但还没拟合平面A，请先跑一次「平面拟合」并存到平面A（或改用「固定基准」）");
                    return cur;
                }
            }

            double hmax = double.NegativeInfinity, hmin = double.PositiveInfinity, sum = 0;
            int n = 0;
            int x0 = roi.X, y0 = roi.Y, x1 = roi.X + roi.Width, y1 = roi.Y + roi.Height;
            for (int y = y0; y < y1; y += step)
                for (int x = x0; x < x1; x += step)
                {
                    float v = z.At<float>(y, x);
                    if (float.IsNaN(v)) continue;
                    double zero = plane != null ? plane[0] * x + plane[1] * y + plane[2] : baseZ;
                    double h = v - zero;
                    if (h > hmax) hmax = h;
                    if (h < hmin) hmin = h;
                    sum += h; n++;
                }
            if (n == 0)
            {
                VpUtil.Add(report, s, spec, false, "框里没有有效深度点，量不出高度");
                return cur;
            }
            double avg = sum / n, diff = hmax - hmin;
            bool ok = !(maxH > 0 && hmax > maxH) && !(minH != 0 && hmin < minH);
            string refTxt = usePlane ? "拟合平面A" : $"固定基准Z={VpUtil.F(baseZ)}";
            string summary = ok
                ? $"相对{refTxt}：最大 {VpUtil.F(hmax)} / 最小 {VpUtil.F(hmin)} / 平均 {VpUtil.F(avg)} mm，高度差 {VpUtil.F(diff)} mm，{n} 点"
                : $"高度超出判定范围（maxH={VpUtil.F(maxH)}, minH={VpUtil.F(minH)}）：最大 {VpUtil.F(hmax)} 最小 {VpUtil.F(hmin)} mm，判不合格";
            VpUtil.Add(report, s, spec, ok, summary, hmax, n, refTxt,
                ("最大高度", hmax), ("最小高度", hmin), ("平均高度", avg),
                ("高度差", diff), ("参与点数", n));
            Cv.Cv2.Rectangle(dst, roi, ok ? VpUtil.Ink : VpUtil.Warn, 1);
            Stamp(dst, ok,
                $"H max={VpUtil.F(hmax)} min={VpUtil.F(hmin)}",
                $"avg={VpUtil.F(avg)} diff={VpUtil.F(diff)} n={n}");
            VpUtil.Progress(progress, $"高度 平均{VpUtil.F(avg)} 差{VpUtil.F(diff)} mm");
            return cur;
        }

        // =====================================================================
        // ⑤ 体积计算：Σ max(0, z-baseZ) × 像素实际面积
        // =====================================================================
        static Cv.Mat VolumeCalc(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var z = ZMap(spec, s, report, ctx);
            if (z == null) return cur;

            var roi = VpUtil.Roi(spec, s, z);
            double baseZ = VpUtil.Num(spec, s, "baseZ", 0);
            double pixelMm = VpUtil.Num(spec, s, "pixelMm", 0.05);
            int step = Math.Max(1, VpUtil.Int(spec, s, "step", 1));
            if (pixelMm <= 0) pixelMm = 0.05;
            // 隔 step 取一点，每个样点代表 step×step 个像素，(pixelMm*step)² 就是它的实际面积 mm²
            double cell = pixelMm * step;
            double cellArea = cell * cell;

            double vol = 0, hsum = 0, hmax = double.NegativeInfinity;
            int n = 0;
            int x0 = roi.X, y0 = roi.Y, x1 = roi.X + roi.Width, y1 = roi.Y + roi.Height;
            for (int y = y0; y < y1; y += step)
                for (int x = x0; x < x1; x += step)
                {
                    float v = z.At<float>(y, x);
                    if (float.IsNaN(v)) continue;
                    double h = v - baseZ;
                    if (h > 0) vol += h * cellArea;      // 低于基准面的点不计体积
                    hsum += h;
                    if (h > hmax) hmax = h;
                    n++;
                }
            if (n == 0)
            {
                VpUtil.Add(report, s, spec, false, "框里没有有效深度点，算不出体积");
                return cur;
            }
            double avg = hsum / n;
            if (double.IsNegativeInfinity(hmax)) hmax = 0;
            string summary = $"基准Z={VpUtil.F(baseZ)}：体积 {VpUtil.F(vol)} mm³，平均高度 {VpUtil.F(avg)} mm，最大高度 {VpUtil.F(hmax)} mm，有效像素 {n} 个（像素 {VpUtil.F(pixelMm)} mm，步长 {step}）";
            VpUtil.Add(report, s, spec, true, summary, vol, n, "",
                ("体积(立方毫米)", vol), ("平均高度", avg), ("最大高度", hmax), ("有效像素数", n));
            Cv.Cv2.Rectangle(dst, roi, VpUtil.Ink, 1);
            Stamp(dst, true,
                $"Vol={VpUtil.F(vol)} mm3",
                $"hAvg={VpUtil.F(avg)} hMax={VpUtil.F(hmax)} n={n}");
            VpUtil.Progress(progress, $"体积 {VpUtil.F(vol)} mm³");
            return cur;
        }

        // =====================================================================
        // ⑥ 面面夹角：两法向量真实夹角
        // =====================================================================
        static Cv.Mat AnglePP(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            bool useManual = VpUtil.Bool(spec, s, "useManual", false);
            double maxAngle = VpUtil.Num(spec, s, "maxAngle", 0);
            double[]? nA, nB;
            string how;

            if (useManual)
            {
                // 手填法向量是纯数值运算，不依赖深度图；不伪造任何数据，直接算
                double ax = VpUtil.Num(spec, s, "nax", 0), ay = VpUtil.Num(spec, s, "nay", 0),
                       az = VpUtil.Num(spec, s, "naz", 1);
                double bx = VpUtil.Num(spec, s, "nbx", 0), by = VpUtil.Num(spec, s, "nby", 0),
                       bz = VpUtil.Num(spec, s, "nbz", 1);
                nA = Normalize3(ax, ay, az);
                nB = Normalize3(bx, by, bz);
                how = "手填法向量";
                if (nA == null || nB == null)
                {
                    VpUtil.Add(report, s, spec, false, "手填的法向量长度接近 0，算不了夹角，请填非零向量");
                    return cur;
                }
            }
            else
            {
                // 读拟合结果要先有深度图（平面本就是拿 ZMap 拟合出来的）
                var z = ZMap(spec, s, report, ctx);
                if (z == null) return cur;
                var st = VpUtil.State(ctx);
                if (st.PlaneA == null || st.PlaneB == null)
                {
                    VpUtil.Add(report, s, spec, false,
                        "请先跑两次「平面拟合」，分别存到平面A和平面B（或勾上「用手填的法向量」）");
                    return cur;
                }
                nA = NormalFromPlane(st.PlaneA);
                nB = NormalFromPlane(st.PlaneB);
                how = "拟合平面A / 平面B";
            }

            if (nA == null || nB == null)
            {
                VpUtil.Add(report, s, spec, false, "平面法向量退化（长度接近 0），算不了夹角");
                return cur;
            }
            double dot = VpUtil.Clamp(nA[0] * nB[0] + nA[1] * nB[1] + nA[2] * nB[2], -1, 1);
            double angle = Math.Acos(dot) * 180.0 / Math.PI;
            double inclA = Incline(nA), inclB = Incline(nB);
            bool ok = !(maxAngle > 0 && angle > maxAngle);
            string summary = ok
                ? $"{how}：面面夹角 {VpUtil.F(angle)}°，法向量点积 {VpUtil.F(dot, 4)}（A倾角 {VpUtil.F(inclA)}°，B倾角 {VpUtil.F(inclB)}°）"
                : $"面面夹角 {VpUtil.F(angle)}° 超过上限 {VpUtil.F(maxAngle)}°，判不合格";
            string nTxt = $"nA=({VpUtil.F(nA[0], 3)},{VpUtil.F(nA[1], 3)},{VpUtil.F(nA[2], 3)}) nB=({VpUtil.F(nB[0], 3)},{VpUtil.F(nB[1], 3)},{VpUtil.F(nB[2], 3)})";
            VpUtil.Add(report, s, spec, ok, summary, angle, 0, nTxt,
                ("夹角(度)", angle), ("法向量点积", dot), ("A倾角", inclA), ("B倾角", inclB));
            Stamp(dst, ok,
                $"Angle={VpUtil.F(angle)}deg dot={VpUtil.F(dot, 3)}",
                $"incA={VpUtil.F(inclA, 1)} incB={VpUtil.F(inclB, 1)}");
            VpUtil.Progress(progress, $"面面夹角 {VpUtil.F(angle)}°");
            return cur;
        }

        static double[]? Normalize3(double x, double y, double z)
        {
            double len = Math.Sqrt(x * x + y * y + z * z);
            if (len < 1e-12) return null;
            return new[] { x / len, y / len, z / len };
        }

        // =====================================================================
        // ⑦ 厚度测量（两拟合平面 / 单平面回退）
        // =====================================================================
        static Cv.Mat Thickness(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            var z = ZMap(spec, s, report, ctx);
            if (z == null) return cur;

            var roi = VpUtil.Roi(spec, s, z);
            double pixelMm = VpUtil.Num(spec, s, "pixelMm", 0.05);
            double target = VpUtil.Num(spec, s, "target", 0);
            double tol = VpUtil.Num(spec, s, "tol", 0.05);
            int step = Math.Max(1, VpUtil.Int(spec, s, "step", 1));
            if (pixelMm <= 0) pixelMm = 0.05;

            var st = VpUtil.State(ctx);
            double[]? A = st.PlaneA, B = st.PlaneB;
            int x0 = roi.X, y0 = roi.Y, x1 = roi.X + roi.Width, y1 = roi.Y + roi.Height;

            double tmax = double.NegativeInfinity, tmin = double.PositiveInfinity, sum = 0;
            int n = 0;
            string how;

            if (A != null && B != null)
            {
                // 平面方程的 z 本就是 mm，|zA-zB| 直接是 mm 厚度，不需要 pixelMm。
                for (int y = y0; y < y1; y += step)
                    for (int x = x0; x < x1; x += step)
                    {
                        double zA = A[0] * x + A[1] * y + A[2];
                        double zB = B[0] * x + B[1] * y + B[2];
                        double t = Math.Abs(zA - zB);
                        if (t > tmax) tmax = t;
                        if (t < tmin) tmin = t;
                        sum += t; n++;
                    }
                how = "两拟合平面 zA-zB";
            }
            else if (A != null || B != null)
            {
                // 只有一张平面（通常是底面）：用深度图相对该平面的高度当厚度。
                // z 与平面值都是 mm，差值天然是 mm。
                double[] p = A != null ? A : B!;
                for (int y = y0; y < y1; y += step)
                    for (int x = x0; x < x1; x += step)
                    {
                        float v = z.At<float>(y, x);
                        if (float.IsNaN(v)) continue;
                        double t = Math.Abs(v - (p[0] * x + p[1] * y + p[2]));
                        if (t > tmax) tmax = t;
                        if (t < tmin) tmin = t;
                        sum += t; n++;
                    }
                how = (A != null ? "平面A" : "平面B") + " + 深度图（单平面回退估算）";
            }
            else
            {
                // 一张平面都没有：只能拿 ROI 里的原始深度范围粗估厚度。
                // 深度若仍是未标定的「像素级差」，乘 pixelMm（mm/像素）把范围换算成 mm，
                // 使这个回退值与两平面分支（本来就是 mm）单位口径一致。
                double lo = double.PositiveInfinity, hi = double.NegativeInfinity;
                for (int y = y0; y < y1; y += step)
                    for (int x = x0; x < x1; x += step)
                    {
                        float v = z.At<float>(y, x);
                        if (float.IsNaN(v)) continue;
                        if (v < lo) lo = v;
                        if (v > hi) hi = v;
                        n++;
                    }
                if (n == 0)
                {
                    VpUtil.Add(report, s, spec, false, "框里没有有效深度点，量不出厚度");
                    return cur;
                }
                double range = (hi - lo) * pixelMm;      // 原始深度范围 → mm
                tmax = tmin = sum = range;
                n = 1;
                how = $"无拟合平面，原始深度范围×pixelMm={VpUtil.F(pixelMm)} 粗估（建议先拟合平面）";
            }

            if (n == 0)
            {
                VpUtil.Add(report, s, spec, false, "框里没有有效深度点，量不出厚度");
                return cur;
            }
            double avg = sum / n, diff = tmax - tmin;
            bool ok = !(target > 0 && Math.Abs(avg - target) > tol);
            string summary = ok
                ? $"{how}：平均厚度 {VpUtil.F(avg)} mm，最大 {VpUtil.F(tmax)}，最小 {VpUtil.F(tmin)}，厚度差 {VpUtil.F(diff)} mm，{(ok ? "合格" : "不合格")}"
                : $"{how}：平均厚度 {VpUtil.F(avg)} mm 偏离标准 {VpUtil.F(target)} mm 超过公差 {VpUtil.F(tol)} mm，判不合格";
            VpUtil.Add(report, s, spec, ok, summary, avg, n, how,
                ("平均厚度", avg), ("最大厚度", tmax), ("最小厚度", tmin),
                ("厚度差", diff), ("是否合格", ok ? 1 : 0));
            Cv.Cv2.Rectangle(dst, roi, ok ? VpUtil.Ink : VpUtil.Warn, 1);
            Stamp(dst, ok,
                $"T avg={VpUtil.F(avg)} max={VpUtil.F(tmax)}",
                $"min={VpUtil.F(tmin)} diff={VpUtil.F(diff)} {(ok ? "PASS" : "FAIL")}");
            VpUtil.Progress(progress, $"厚度 平均{VpUtil.F(avg)} mm {(ok ? "合格" : "不合格")}");
            return cur;
        }
    }
}
