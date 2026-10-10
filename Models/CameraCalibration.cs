// === NoCodeMotion 视觉工具标定结果模型 | 作者：温⁠启​志‍ ‌|‏ ‌微​信⁠：‎1‌8‎7‎1‍9‎3​6‍1‌3​9⁣9​ ‌|‏ ⁠保⁣留‎所‎有​权‏利‏，​请⁣勿⁠删‌除 ===
namespace NoCodeMotion.Models
{
    /// <summary>
    /// 相机标定结果（工程级持久化记录，一台相机一条）。
    ///
    /// 全部字段都是标量（string / double / bool），这样 XlsxProjectStore 的反射导出能直接
    /// 把它落成「标定」工作表；点集用分号/逗号分隔的字符串保存，避免嵌套集合。
    ///
    /// 数学约定（图像像素坐标 (u,v) 与机台坐标 (X,Y) mm）：
    ///   9 点标定 → 仿射拟合：
    ///       X = A1·u + A2·v + A3
    ///       Y = B1·u + B2·v + B3
    ///   像素当量 = √(A1²+B1²) mm/px（沿图像 +u 方向）；+v 方向当量 = √(A2²+B2²)。
    ///   X 方向角 = atan2(B1, A1)、Y 方向角 = atan2(B2, A2)（度，机台坐标系下的方向）。
    ///   旋转中心由 5 点旋转标定的 (θ, u, v) 做 Kåsa 圆拟合得到（图像像素），
    ///   再经上面的仿射换算成机台 mm 坐标。
    /// </summary>
    public sealed class CameraCalibration
    {
        /// <summary>相机名（对应 CameraItem.Name，也是查表键）。</summary>
        public string CameraName { get; set; } = "";

        /// <summary>标定时间（yyyy-MM-dd HH:mm:ss）。</summary>
        public string CalibratedAt { get; set; } = "";

        /// <summary>本次标定是否成功（失败时下面的数值不可信）。</summary>
        public bool IsValid { get; set; }

        /// <summary>结果说明（成功摘要 / 失败原因）。</summary>
        public string Message { get; set; } = "";

        /// <summary>像素当量 mm/px（沿图像 +u 方向，= √(A1²+B1²)）。</summary>
        public double PixelEquivalent { get; set; }

        /// <summary>像素当量 mm/px（沿图像 +v 方向，= √(A2²+B2²)）。</summary>
        public double PixelEquivalentV { get; set; }

        /// <summary>图像 +u 方向在机台坐标下的方向角（度）。</summary>
        public double AngleX { get; set; }

        /// <summary>图像 +v 方向在机台坐标下的方向角（度）。</summary>
        public double AngleY { get; set; }

        /// <summary>仿射系数：X = A1·u + A2·v + A3。</summary>
        public double A1 { get; set; }
        /// <summary>仿射系数 A2。</summary>
        public double A2 { get; set; }
        /// <summary>仿射系数 A3（平移）。</summary>
        public double A3 { get; set; }

        /// <summary>仿射系数：Y = B1·u + B2·v + B3。</summary>
        public double B1 { get; set; }
        /// <summary>仿射系数 B2。</summary>
        public double B2 { get; set; }
        /// <summary>仿射系数 B3（平移）。</summary>
        public double B3 { get; set; }

        /// <summary>旋转中心（机台 mm 坐标 X）。</summary>
        public double RotCenterX { get; set; }

        /// <summary>旋转中心（机台 mm 坐标 Y）。</summary>
        public double RotCenterY { get; set; }

        /// <summary>旋转中心（图像像素坐标 u）。</summary>
        public double RotCenterU { get; set; }

        /// <summary>旋转中心（图像像素坐标 v）。</summary>
        public double RotCenterV { get; set; }

        /// <summary>旋转拟合半径（图像像素）。</summary>
        public double RotRadiusPx { get; set; }

        /// <summary>9 点仿射拟合残差 RMS（mm）。</summary>
        public double ResidualRms { get; set; }

        /// <summary>5 点圆拟合残差 RMS（px）。</summary>
        public double RotResidualRms { get; set; }

        /// <summary>9 点图像像素点集，序列化 "u,v;u,v;…"。</summary>
        public string Points9 { get; set; } = "";

        /// <summary>9 点机台坐标点集，序列化 "X,Y;X,Y;…"（mm）。</summary>
        public string Machine9 { get; set; } = "";

        /// <summary>5 点旋转点集，序列化 "θ,u,v;θ,u,v;…"（θ 度，u/v 像素）。</summary>
        public string Points5 { get; set; } = "";

        /// <summary>是否已算出可用结果（成功且像素当量 &gt; 0）。</summary>
        public bool IsUsable => IsValid && PixelEquivalent > 0;

        /// <summary>把图像像素坐标 (u,v) 按仿射换算成机台 mm 坐标。</summary>
        public void ImageToMachine(double u, double v, out double x, out double y)
        {
            x = A1 * u + A2 * v + A3;
            y = B1 * u + B2 * v + B3;
        }

        /// <summary>
        /// 把模板匹配得到的图像像素点按「绕旋转中心旋转 angleDeg 度」后再换算成机台 mm。
        /// 匹配出的模板自带角度，直接走仿射会丢掉旋转量；这里先在图像空间绕标定出的旋转中心
        /// 旋转，再走仿射。当仿射的线性部分是相似变换（等比例 + 旋转，正常标定都满足）时，
        /// 这与「先换算到 mm 再绕机台旋转中心旋转」完全等价。
        /// </summary>
        public void ImageToMachineRotated(double u, double v, double angleDeg, out double x, out double y)
        {
            double rad = angleDeg * Math.PI / 180.0;
            double c = Math.Cos(rad), s = Math.Sin(rad);
            double du = u - RotCenterU, dv = v - RotCenterV;
            double ru = RotCenterU + du * c - dv * s;
            double rv = RotCenterV + du * s + dv * c;
            ImageToMachine(ru, rv, out x, out y);
        }
    }
}
// === NoCodeMotion 视觉工具标定结果模型 | 作者：温‍启⁠志‎ ​|‎ ‎微⁣信‌：‎1⁣8⁣7⁣1‏9​3‌6‏1‍3​9‍9⁣ ‎|​ ⁠保​留⁣所‏有⁣权‏利‌，⁣请⁣勿‏删‎除 ===
