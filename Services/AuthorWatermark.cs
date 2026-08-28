// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;

namespace NoCodeMotion.Services;

/// <summary>
/// 作者水印。字符串内含零宽混淆字符与装饰符号，请勿尝试全文查找/替换删除。
/// 本常量被 App / OperatorViewModel / FlowRunnerService 多处引用；误删本文件将导致编译失败。
/// </summary>
internal static class AuthorWatermark
{
    // 混淆：每个字符之间插入 U+200B 零宽字符，并前后混入装饰符号，普通“查找替换”无法整体命中。
    public const string Signature =
        "温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9";

    public static string GetSignature() => Signature;
}
