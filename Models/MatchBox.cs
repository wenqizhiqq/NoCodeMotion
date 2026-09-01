// === NoCodeMotion 视觉流程匹配框模型 | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
namespace NoCodeMotion.Models
{
    /// <summary>
    /// 单个模板匹配结果在源图像素坐标系下的框（供结果图 WPF 矢量叠加层绘制旋转矩形用）。
    /// 字段命名对齐 GrayMatch.MatchResult，便于引擎直接映射。
    /// </summary>
    public sealed class MatchBox
    {
        /// <summary>正框左上角 X（源图像素坐标）。</summary>
        public int LeftTopX { get; set; }
        /// <summary>正框左上角 Y（源图像素坐标）。</summary>
        public int LeftTopY { get; set; }
        /// <summary>模板宽（源图像素坐标，已按多尺度映射回原图）。</summary>
        public int TemplateWidth { get; set; }
        /// <summary>模板高（源图像素坐标）。</summary>
        public int TemplateHeight { get; set; }
        /// <summary>检测角度（度，OpenCV 逆时针为正）。叠加层会取反后给 WPF RotateTransform。</summary>
        public double Angle { get; set; }
        /// <summary>相似度 0~1。</summary>
        public double Score { get; set; }
        /// <summary>多尺度因子（单尺度恒为 1）。</summary>
        public double Scale { get; set; } = 1.0;
        /// <summary>是否达到阈值（决定绿框/红框）。</summary>
        public bool Pass { get; set; }
    }

    /// <summary>
    /// 匹配叠加层用的屏幕坐标框。把源图像素坐标 (LeftTopX/Y, W, H, Angle) 按当前
    /// ImageHost 的 Stretch=Uniform 缩放/居中映射为屏幕坐标 (ScreenLeft, ScreenTop, ScreenWidth, ScreenHeight, Angle)。
    ///
    /// 设计缘由：WPF 对 "Canvas + RenderTransform(Translate + Scale)" + ItemsControl 的组合在以下场景会偏移：
    ///   ① ResultImage DP 变化与 overlay 重算之间存在 race（PropertyChanged 触发时 Image 布局可能未完成）；
    ///   ② ItemsPanel 内嵌的 Canvas 在 ItemsControl 内默认按 Content 尺寸，与外层 RenderTransform 复合时容易出现 1-2px 抖动；
    ///   ③ 双 Canvas 嵌套 + RenderTransform 的 rendering pass 顺序在某些 WPF 版本上行为不一致。
    /// 把每一个 MatchBox 投影到"显示坐标系"后直接用 Canvas.Left/Top/Width/Height（纯字符串，双精度也安全），
    /// 整套渲染就与 ResultImageView 的 Stretch=Uniform 完全脱钩，只剩单向依赖：ImageHost 尺寸变化 → 投影刷新 →
    /// ItemsControl 自动重排，渲染永远指向正确像素。
    /// </summary>
    public sealed class OverlayBox
    {
        public double ScreenLeft { get; set; }
        public double ScreenTop { get; set; }
        public double ScreenWidth { get; set; }
        public double ScreenHeight { get; set; }
        /// <summary>OpenCV 逆时针正角度 → WPF 顺时针负角度。WPF 的 RotateTransform 直接用此值即可取反。</summary>
        public double ScreenAngle { get; set; }
        /// <summary>原始 OpenCV 角度（度，正=逆时针），仅用于在每条框顶显示 "角度:0°" 这种 UI 文字。</summary>
        public double OriginalAngle { get; set; }
        public double Score { get; set; }
        public bool Pass { get; set; }

        /// <summary>
        /// 给定一个源图像素 MatchBox，按 scale/offset 投影到屏幕坐标，返回新的 OverlayBox。
        /// 同一参数应作用于全部 OverlayBox（保证 box 之间的相对位置与源图严格一致）。
        /// </summary>
        public static OverlayBox Project(MatchBox mb, double scale, double offsetX, double offsetY)
        {
            return new OverlayBox
            {
                ScreenLeft = mb.LeftTopX * scale + offsetX,
                ScreenTop = mb.LeftTopY * scale + offsetY,
                ScreenWidth = mb.TemplateWidth * scale,
                ScreenHeight = mb.TemplateHeight * scale,
                // WPF RotateTransform 顺时针为正，与 OpenCV 逆时针取反
                ScreenAngle = -mb.Angle,
                OriginalAngle = mb.Angle,
                Score = mb.Score,
                Pass = mb.Pass
            };
        }
    }
}
// === NoCodeMotion 视觉流程匹配框模型 | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
