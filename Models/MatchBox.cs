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
}
// === NoCodeMotion 视觉流程匹配框模型 | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
