// === NoCodeMotion 视觉标定自动执行器（程序自动走 9 点 + 5 点旋转） | 作者：温​启‎志‍ ‍|⁣ ⁣微‏信‎：‏1‏8‌7‍1‌9‎3⁠6‏1​3​9‌9‌ ⁠|​ ​保‎留‍所⁠有‍权⁣利‏，⁣请‌勿‍删‎除 ===
using System.Diagnostics;
using NoCodeMotion.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NoCodeMotion.Services.Vision.Calibration
{
    /// <summary>
    /// 「示例标定图」：不接相机、不接轴，也能把 **9 张定 XY + 5 张定旋转中心** 这条线整段走一遍。
    ///
    /// 不是随便画几张图糊弄过去：这里**先约定一套真值**（像素 ↔ 毫米的仿射 + 转盘中心在图上的位置），
    /// 再按真值反算「轴走到这个位置时，标记应该出现在图上哪几个像素」，画成图交给标定执行器。
    /// 执行器拿到的就是普通 BGRA，走的是同一套找标记 → 与轴位置配对 → 最小二乘拟合的代码，
    /// 所以跑完应当能把 A1..B3 和旋转中心**还原回真值**（容差之内才算这条链真的通）。
    ///
    /// 两条铁律：
    ///   1. 只有人在参数卡上明确勾了「用示例图」才走这里，真相机在的时候绝不自动替代；
    ///   2. 示例标出的结果**不写进真实相机名的标定记录**（名字前挂「示例·」），
    ///      拿示例数字当机台数字去对位，比标歪了危险得多。
    /// </summary>
    public static class DemoCalib
    {
        /// <summary>示例图宽（像素）。</summary>
        public const int Width = 640;
        /// <summary>示例图高（像素）。</summary>
        public const int Height = 480;

        /// <summary>真值：一个像素代表多少毫米。</summary>
        public const double MmPerPx = 0.1;
        /// <summary>真值：机台 X=0 时标记在图上的列。</summary>
        public const double OriginU = 320;
        /// <summary>真值：机台 Y=0 时标记在图上的行。</summary>
        public const double OriginV = 240;
        /// <summary>真值：转盘中心在图上的列（旋转标定要还原的就是它）。</summary>
        public const double RotCenterU = 300;
        /// <summary>真值：转盘中心在图上的行。</summary>
        public const double RotCenterV = 260;
        /// <summary>标记圆盘半径（像素）。面积 ≈ 452，落在默认面积区间里，填充率 0.78 也过检测。</summary>
        public const double MarkerRadiusPx = 12;

        /// <summary>示例用的 X 轴名（不要求工程里真有这几根轴）。</summary>
        public const string AxisX = "示例X";
        /// <summary>示例用的 Y 轴名。</summary>
        public const string AxisY = "示例Y";
        /// <summary>示例用的旋转轴名。</summary>
        public const string AxisR = "示例R";

        /// <summary>机台毫米 → 图上像素（真值仿射的正解：X = MmPerPx·(u − OriginU)）。</summary>
        public static double PxU(double mmX) => OriginU + mmX / MmPerPx;

        /// <summary>机台毫米 → 图上像素。</summary>
        public static double PxV(double mmY) => OriginV + mmY / MmPerPx;

        /// <summary>
        /// 仿真轴：MoveAxisAbs 把目标位置记下来，ReadAxisPosition 原样读回（永远到位、永远不撞限位）。
        /// 标定执行器只管「走 → 读回 → 拍照」，它分不清这是真轴还是这几行字典。
        /// </summary>
        public sealed class Sim
        {
            readonly Dictionary<string, double> _pos = new(StringComparer.Ordinal);

            /// <summary>读当前位置（没走过的轴按 0）。</summary>
            public double Read(string? axis) => _pos.TryGetValue(axis ?? "", out double v) ? v : 0;

            /// <summary>走到目标位置（立即到位）。</summary>
            public void Move(string? axis, double value) => _pos[axis ?? ""] = value;
        }

        /// <summary>
        /// 按当前仿真轴位置画一帧，返回 BGRA（和标定执行器的取图委托同一种字节序）。
        /// 标记先落在「XY 换算出来的像素」上，转盘再带着它绕旋转中心转 R 轴当前角度 ——
        /// 所以 XY 那 9 张给出仿射，R 那 5 张的点必然落在以真值圆心为心的同一个圆上。
        /// </summary>
        public static byte[] Render(Sim sim, bool darkMarker)
        {
            double u0 = PxU(sim.Read(AxisX));
            double v0 = PxV(sim.Read(AxisY));
            double rad = sim.Read(AxisR) * Math.PI / 180.0;
            double du = u0 - RotCenterU, dv = v0 - RotCenterV;
            double mu = RotCenterU + du * Math.Cos(rad) - dv * Math.Sin(rad);
            double mv = RotCenterV + du * Math.Sin(rad) + dv * Math.Cos(rad);

            // 背景与标记的明暗按「找亮标记 / 找暗标记」反过来，阈值 128 两边都能分得开
            byte bg = (byte)(darkMarker ? 205 : 45);
            byte fg = (byte)(darkMarker ? 25 : 225);
            int w = Width, h = Height;
            var buf = new byte[w * h * 4];

            for (int y = 0; y < h; y++)
            {
                // 背景带一点缓变（不是纯色），免得看起来像假图；幅度 ±6，离阈值 128 很远
                int g = bg + (y % 7 - 3);
                int rowBase = y * w * 4;
                for (int x = 0; x < w; x++)
                {
                    int i = rowBase + x * 4;
                    buf[i] = (byte)g; buf[i + 1] = (byte)g; buf[i + 2] = (byte)g; buf[i + 3] = 255;
                }
            }

            int r2 = (int)(MarkerRadiusPx * MarkerRadiusPx);
            int u1 = (int)Math.Round(mu), v1 = (int)Math.Round(mv);
            for (int y = v1 - (int)MarkerRadiusPx - 1; y <= v1 + (int)MarkerRadiusPx + 1; y++)
            {
                if (y < 0 || y >= h) continue;
                for (int x = u1 - (int)MarkerRadiusPx - 1; x <= u1 + (int)MarkerRadiusPx + 1; x++)
                {
                    if (x < 0 || x >= w) continue;
                    double dx = x - mu, dy = y - mv;
                    if (dx * dx + dy * dy > r2) continue;
                    int i = (y * w + x) * 4;
                    buf[i] = fg; buf[i + 1] = fg; buf[i + 2] = fg; buf[i + 3] = 255;
                }
            }
            return buf;
        }

        /// <summary>
        /// 真值说明（跟着图片一起落盘）：跑完标定该还原出哪几个数，差多少算对。
        /// 现场的人不用猜「这串数字对不对」，把结果和这张纸一比就知道。
        /// </summary>
        public static string TruthText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("示例标定图的真值（标定跑完应当还原出下面这几个数，单位见每行）");
            sb.AppendLine("图像尺寸：" + Width + " × " + Height + " 像素");
            sb.AppendLine("像素当量：X " + F(MmPerPx) + " mm/px，Y " + F(MmPerPx) + " mm/px（横竖一样，没做倾斜）");
            sb.AppendLine("机台原点在图上的位置：(" + F(OriginU) + ", " + F(OriginV) + ") px —— 即 X=" + F(0) + " mm、Y=" + F(0) + " mm 时标记在这");
            sb.AppendLine("仿射（图上→机台）：X = " + F(MmPerPx) + "·u " + F(-OriginU * MmPerPx)
                          + "，Y = " + F(MmPerPx) + "·v " + F(-OriginV * MmPerPx));
            sb.AppendLine("旋转中心（图上）：(" + F(RotCenterU) + ", " + F(RotCenterV) + ") px");
            sb.AppendLine("旋转中心（机台）：(" + F(-OriginU * MmPerPx + RotCenterU * MmPerPx) + ", "
                          + F(-OriginV * MmPerPx + RotCenterV * MmPerPx) + ") mm");
            sb.AppendLine("标记：半径 " + F(MarkerRadiusPx) + " px 的实心圆，面积 ≈ "
                          + F(Math.PI * MarkerRadiusPx * MarkerRadiusPx) + " px");
            sb.AppendLine("9 张 XY：以起点为中心，按「间距」3×3 铺开（默认间距 10 mm → 图上每步 " + F(10 / MmPerPx) + " px）");
            sb.AppendLine("5 张旋转：以当前角度为中心，按「步距」各转一张（默认 ±40°、±20°、0°），标记绕旋转中心走圆");
            sb.AppendLine("★ 这套数字是软件画出来的，只用来验证 9+5 这条链通不通；真机标定请关掉「用示例图」重跑。");
            return sb.ToString();
        }

        static string F(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);
    }
}
// === NoCodeMotion 视觉标定自动执行器（程序自动走 9 点 + 5 点旋转） | 作者：温⁣启⁠志‌ ‌|​ ​微⁠信​：‎1‎8‎7‎1⁠9‎3​6‏1‎3‎9⁠9⁠ ​|⁣ ‌保⁣留​所⁠有​权‍利‏，⁠请‎勿‏删‍除 ===
