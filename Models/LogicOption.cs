// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░💒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░💒▓✦
// 流程「逻辑」列的下拉项：Value=真正写入 FlowStep.Logic 的字符串（运行器按此解析），
// Category=视觉分组（控制流 / 动作 / 注释），用于下拉分组与单元格着色，避免新建时填错。
namespace NoCodeMotion.Models;

/// <summary>逻辑列的可选项（带分组类别），用于 ComboBox 分组下拉 + 单元格着色。</summary>
public class LogicOption
{
    /// <summary>真正写入 FlowStep.Logic 的值（如果 / 就 / 否则 / 循环开始 …）。</summary>
    public string Value { get; set; } = "";

    /// <summary>视觉分组：控制流 / 动作 / 注释。</summary>
    public string Category { get; set; } = "动作";

    public override string ToString() => Value;
}
