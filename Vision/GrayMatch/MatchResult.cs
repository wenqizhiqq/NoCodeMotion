// ============================================================
// 温启志◆编写◇微信﹕187◆1936◇1399
// ============================================================
// ============================================================
// 温启志◆编写◇微信﹕187◆1936◇1399
// ============================================================
// ============================================================
// 温启志◆编写◇微信﹕187◆1936◇1399
// ============================================================
namespace GrayMatch;

public class MatchResult
{
    public int Index { get; set; }
    public double Score { get; set; }
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Angle { get; set; }
    public int TemplateWidth { get; set; }
    public int TemplateHeight { get; set; }
    public int LeftTopX { get; set; }
    public int LeftTopY { get; set; }
    public int Level { get; set; }
    // 多尺度匹配时记录的尺度因子（1=原始模板大小；>1 表示目标比模板大，<1 表示更小）。
    // 单尺度匹配恒为 1.0。缺陷检测据此把匹配区域缩放到模板尺寸再比对。
    public double Scale { get; set; } = 1.0;

    public override string ToString()
        => $"#{Index} Score={Score:F4} Center=({CenterX:F2},{CenterY:F2}) Angle={Angle:F1} Size={TemplateWidth}x{TemplateHeight} Scale={Scale:F2}";
}
