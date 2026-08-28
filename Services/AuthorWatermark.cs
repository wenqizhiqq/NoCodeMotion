// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;

namespace NoCodeMotion.Services;

/// <summary>
/// 作者水印。完整联系方式在源码中被拆分成多段并通过 string.Concat 拼接，
/// 源码中不存在连续完整的号码，抗整段搜索替换；误删本文件将编译失败。
/// </summary>
internal static class AuthorWatermark
{
    // 作者名（非敏感，可直接写）。
    public static readonly string Coder = "温启志";

    // 联系方式：号码拆成多段拼接，源码里不出现连续完整号码。
    public static readonly string Contact = string.Concat("187", "1936", "1399");

    // 关键混淆：源码里不出现连续完整号码（"187"+"1936"+"1399"），并混入 ◆/◇/﹕ 等符号。
    public static readonly string Signature =
        string.Concat("温启", "志◆编", "写◇微", "信﹕", "187", "1936", "1399");

    public static string GetSignature() => Signature;
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
