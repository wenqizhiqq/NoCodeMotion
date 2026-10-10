// === NoCodeMotion 视觉标定数学求解器（9点仿射 + 5点圆拟合） | 作者：温​启‎志‍ ‍|⁣ ⁣微‏信‎：‏1‏8‌7‍1‌9‎3⁠6‏1​3​9‌9‌ ⁠|​ ​保‎留‍所⁠有‍权⁣利‏，⁣请‌勿‍删‎除 ===
using System.Globalization;

namespace NoCodeMotion.Services.Vision.Calibration
{
    /// <summary>
    /// 9 点仿射拟合结果：图像像素 (u,v) → 机台 mm (X,Y)。
    ///   X = A1·u + A2·v + A3
    ///   Y = B1·u + B2·v + B3
    /// </summary>
    public sealed class AffineFit
    {
        /// <summary>仿射系数 A1。</summary>
        public double A1 { get; set; }
        /// <summary>仿射系数 A2。</summary>
        public double A2 { get; set; }
        /// <summary>仿射系数 A3（平移）。</summary>
        public double A3 { get; set; }
        /// <summary>仿射系数 B1。</summary>
        public double B1 { get; set; }
        /// <summary>仿射系数 B2。</summary>
        public double B2 { get; set; }
        /// <summary>仿射系数 B3（平移）。</summary>
        public double B3 { get; set; }

        /// <summary>拟合残差 RMS（mm）。</summary>
        public double RmsMm { get; set; }

        /// <summary>像素当量 mm/px（沿图像 +u 方向 = √(A1²+B1²)）。</summary>
        public double PixelEquivalentU { get; set; }

        /// <summary>像素当量 mm/px（沿图像 +v 方向 = √(A2²+B2²)）。</summary>
        public double PixelEquivalentV { get; set; }

        /// <summary>图像 +u 方向在机台坐标下的方向角（度）。</summary>
        public double AngleXDeg { get; set; }

        /// <summary>图像 +v 方向在机台坐标下的方向角（度）。</summary>
        public double AngleYDeg { get; set; }

        /// <summary>求解是否成功（点数足够且不奇异）。</summary>
        public bool Ok { get; set; }

        /// <summary>失败原因 / 成功摘要。</summary>
        public string Message { get; set; } = "";

        /// <summary>按仿射把像素坐标换算成机台 mm。</summary>
        public void Map(double u, double v, out double x, out double y)
        {
            x = A1 * u + A2 * v + A3;
            y = B1 * u + B2 * v + B3;
        }
    }

    /// <summary>5 点旋转标定的圆拟合结果（图像像素坐标系）。</summary>
    public sealed class CircleFit
    {
        /// <summary>圆心 u（像素）——即旋转中心。</summary>
        public double CenterU { get; set; }
        /// <summary>圆心 v（像素）——即旋转中心。</summary>
        public double CenterV { get; set; }
        /// <summary>拟合半径（像素）。</summary>
        public double RadiusPx { get; set; }
        /// <summary>拟合残差 RMS（像素）。</summary>
        public double RmsPx { get; set; }
        /// <summary>求解是否成功。</summary>
        public bool Ok { get; set; }
        /// <summary>失败原因 / 成功摘要。</summary>
        public string Message { get; set; } = "";
    }

    /// <summary>
    /// 标定数学求解器（纯 C#，不依赖 OpenCV / WPF，便于冒烟直接断言）。
    /// 9 点 → 最小二乘仿射；5 点 → Kåsa 圆拟合（线性化最小二乘）。
    /// </summary>
    public static class CalibSolver
    {
        /// <summary>最小二乘拟合仿射。pts 为 (U, V, X, Y)：像素 → 机台 mm。</summary>
        public static AffineFit FitAffine(IReadOnlyList<(double U, double V, double X, double Y)> pts)
        {
            var r = new AffineFit();
            int n = pts?.Count ?? 0;
            if (n < 3)
            {
                r.Message = "点数不足（仿射至少需要 3 个非共线点），当前 " + n + " 点";
                return r;
            }

            double suu = 0, suv = 0, svv = 0, su = 0, sv = 0;
            double sux = 0, svx = 0, sx = 0;
            double suy = 0, svy = 0, sy = 0;
            for (int i = 0; i < n; i++)
            {
                var p = pts![i];
                suu += p.U * p.U; suv += p.U * p.V; svv += p.V * p.V;
                su += p.U; sv += p.V;
                sux += p.U * p.X; svx += p.V * p.X; sx += p.X;
                suy += p.U * p.Y; svy += p.V * p.Y; sy += p.Y;
            }

            var m = new double[3, 3];
            m[0, 0] = suu; m[0, 1] = suv; m[0, 2] = su;
            m[1, 0] = suv; m[1, 1] = svv; m[1, 2] = sv;
            m[2, 0] = su;  m[2, 1] = sv;  m[2, 2] = n;

            var cx = new double[3];
            if (!Solve3(m, new[] { sux, svx, sx }, cx))
            {
                r.Message = "9 点共线或退化，仿射不可解（请确认 9 点铺成 3×3 网格）";
                return r;
            }
            var cy = new double[3];
            if (!Solve3(m, new[] { suy, svy, sy }, cy))
            {
                r.Message = "9 点共线或退化，仿射不可解（请确认 9 点铺成 3×3 网格）";
                return r;
            }

            r.A1 = cx[0]; r.A2 = cx[1]; r.A3 = cx[2];
            r.B1 = cy[0]; r.B2 = cy[1]; r.B3 = cy[2];

            double se = 0;
            for (int i = 0; i < n; i++)
            {
                var p = pts![i];
                double ex = r.A1 * p.U + r.A2 * p.V + r.A3 - p.X;
                double ey = r.B1 * p.U + r.B2 * p.V + r.B3 - p.Y;
                se += ex * ex + ey * ey;
            }
            r.RmsMm = Math.Sqrt(se / (2.0 * n));

            r.PixelEquivalentU = Math.Sqrt(r.A1 * r.A1 + r.B1 * r.B1);
            r.PixelEquivalentV = Math.Sqrt(r.A2 * r.A2 + r.B2 * r.B2);
            r.AngleXDeg = Math.Atan2(r.B1, r.A1) * 180.0 / Math.PI;
            r.AngleYDeg = Math.Atan2(r.B2, r.A2) * 180.0 / Math.PI;
            r.Ok = true;
            r.Message = "仿射拟合成功：像素当量 " + Fmt(r.PixelEquivalentU) + " mm/px，残差 RMS " + Fmt(r.RmsMm) + " mm";
            return r;
        }

        /// <summary>Kåsa 圆拟合：pts 为 (U, V)，返回圆心与半径（最小二乘）。</summary>
        public static CircleFit FitCircle(IReadOnlyList<(double U, double V)> pts)
        {
            var r = new CircleFit();
            int n = pts?.Count ?? 0;
            if (n < 3)
            {
                r.Message = "点数不足（圆拟合至少需要 3 点），当前 " + n + " 点";
                return r;
            }

            double suu = 0, suv = 0, svv = 0, su = 0, sv = 0;
            double suz = 0, svz = 0, sz = 0;
            for (int i = 0; i < n; i++)
            {
                var p = pts![i];
                double z = p.U * p.U + p.V * p.V;
                suu += p.U * p.U; suv += p.U * p.V; svv += p.V * p.V;
                su += p.U; sv += p.V;
                suz += p.U * z; svz += p.V * z; sz += z;
            }

            var m = new double[3, 3];
            m[0, 0] = suu; m[0, 1] = suv; m[0, 2] = su;
            m[1, 0] = suv; m[1, 1] = svv; m[1, 2] = sv;
            m[2, 0] = su;  m[2, 1] = sv;  m[2, 2] = n;

            var sol = new double[3];
            if (!Solve3(m, new[] { -suz, -svz, -sz }, sol))
            {
                r.Message = "5 点退化（重合或共线），圆拟合不可解";
                return r;
            }

            double d = sol[0], e = sol[1], f = sol[2];
            double cu = -d / 2.0, cv = -e / 2.0;
            double rad2 = cu * cu + cv * cv - f;
            if (rad2 <= 0)
            {
                r.Message = "5 点拟合出的半径非正，请检查旋转角度与图像点是否一一对应";
                return r;
            }
            r.CenterU = cu;
            r.CenterV = cv;
            r.RadiusPx = Math.Sqrt(rad2);

            double se = 0;
            for (int i = 0; i < n; i++)
            {
                var p = pts![i];
                double du = p.U - cu, dv = p.V - cv;
                double dd = Math.Sqrt(du * du + dv * dv) - r.RadiusPx;
                se += dd * dd;
            }
            r.RmsPx = Math.Sqrt(se / n);
            r.Ok = true;
            r.Message = "圆拟合成功：旋转中心 (" + Fmt(cu) + ", " + Fmt(cv) + ") px，半径 " + Fmt(r.RadiusPx) + " px";
            return r;
        }

        /// <summary>把 (A,B) 点集序列化成 "a,b;a,b;…"（不变文化）。</summary>
        public static string SerializePoints2(IEnumerable<(double A, double B)> pts)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var p in pts)
            {
                if (sb.Length > 0) sb.Append(';');
                sb.Append(Fmt(p.A)).Append(',').Append(Fmt(p.B));
            }
            return sb.ToString();
        }

        /// <summary>解析 "a,b;a,b;…"（忽略空段与残缺段）。</summary>
        public static List<(double A, double B)> ParsePoints2(string? s)
        {
            var list = new List<(double A, double B)>();
            if (string.IsNullOrWhiteSpace(s)) return list;
            foreach (var seg in s!.Split(';'))
            {
                var t = seg.Trim();
                if (t.Length == 0) continue;
                var parts = t.Split(',');
                if (parts.Length < 2) continue;
                if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double a)
                    && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double b))
                    list.Add((a, b));
            }
            return list;
        }

        /// <summary>把 (A,B,C) 点集序列化成 "a,b,c;…"。</summary>
        public static string SerializePoints3(IEnumerable<(double A, double B, double C)> pts)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var p in pts)
            {
                if (sb.Length > 0) sb.Append(';');
                sb.Append(Fmt(p.A)).Append(',').Append(Fmt(p.B)).Append(',').Append(Fmt(p.C));
            }
            return sb.ToString();
        }

        /// <summary>解析 "a,b,c;…"（忽略空段与残缺段）。</summary>
        public static List<(double A, double B, double C)> ParsePoints3(string? s)
        {
            var list = new List<(double A, double B, double C)>();
            if (string.IsNullOrWhiteSpace(s)) return list;
            foreach (var seg in s!.Split(';'))
            {
                var t = seg.Trim();
                if (t.Length == 0) continue;
                var parts = t.Split(',');
                if (parts.Length < 3) continue;
                if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double a)
                    && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double b)
                    && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double c))
                    list.Add((a, b, c));
            }
            return list;
        }

        /// <summary>不变文化短格式（避免小数点被本地化，落 xlsx / 存变量都安全）。</summary>
        public static string Fmt(double v) => v.ToString("0.##########", CultureInfo.InvariantCulture);

        /// <summary>3×3 线性方程组高斯消元（列主元）。奇异返回 false。</summary>
        private static bool Solve3(double[,] m, double[] rhs, double[] sol)
        {
            double scale = 0;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    scale = Math.Max(scale, Math.Abs(m[i, j]));
            double eps = Math.Max(1e-12, scale * 1e-12);

            var a = new double[3, 4];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++) a[i, j] = m[i, j];
                a[i, 3] = rhs[i];
            }
            for (int c = 0; c < 3; c++)
            {
                int piv = c;
                for (int i = c + 1; i < 3; i++)
                    if (Math.Abs(a[i, c]) > Math.Abs(a[piv, c])) piv = i;
                if (Math.Abs(a[piv, c]) < eps) return false;
                if (piv != c)
                    for (int j = c; j < 4; j++) { double t = a[c, j]; a[c, j] = a[piv, j]; a[piv, j] = t; }
                for (int i = c + 1; i < 3; i++)
                {
                    double f = a[i, c] / a[c, c];
                    for (int j = c; j < 4; j++) a[i, j] -= f * a[c, j];
                }
            }
            for (int i = 2; i >= 0; i--)
            {
                double s = a[i, 3];
                for (int j = i + 1; j < 3; j++) s -= a[i, j] * sol[j];
                sol[i] = s / a[i, i];
            }
            return true;
        }
    }
}
// === NoCodeMotion 视觉标定数学求解器（9点仿射 + 5点圆拟合） | 作者：温⁣启⁠志‌ ‌|​ ​微⁠信​：‎1‎8‎7‎1⁠9‎3​6‏1‎3‎9⁠9⁠ ​|⁣ ‌保⁣留​所⁠有​权‍利‏，⁠请‎勿‏删‍除 ===
