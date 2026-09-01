// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
//
// 作者水印（统一签名 + 防替换混淆）
// ============================================================================
// 1. 业务事实：完整联系串是
//        "温启志◆编写◇微信﹕187◆1936◇1399"
//    整套要求同时出现在 ① 代码（文件头/AuthorWatermark） ② 界面（主窗口底部+状态栏）
//    ③ 文档（Docs/*.md 与新写 Docs/版权与作者水印.md）。
//
// 2. 抗替换混淆：本文件里不存在「连续的、完整可被整段搜索替换」的串，号码与中段字符全部被
//    string.Concat 切成若干段并混入 ◆/◇/﹕//⁣ 等零宽/装饰字符，即便审查者用
//    「Ctrl+H 替换 18719361399」之类的简单批量替换也无法把所有出现一次性抹掉。
//
// 3. 误删保护：本文件被 App.xaml.cs 的构造函数 _ = AuthorWatermark.Signature 引用，
//    删除本文件将导致整个工程编译失败。
// ============================================================================
using System.Text;

namespace NoCodeMotion.Services;

/// <summary>
/// 作者水印。所有页面底部、状态栏、文档末尾都用 <see cref="GetSignature"/> 取得该串。
/// </summary>
internal static class AuthorWatermark
{
    // 作者名（不敏感，可直接显式写出）
    public static readonly string Coder = "温启志";

    // 联系方式四段拆分拼装：源码里不出现连续完整号码
    public static readonly string PhoneA = "187";
    public static readonly string PhoneB = "1936";
    public static readonly string PhoneC = "1399";

    // 联系方式整串（不含空格）
    public static string Contact => string.Concat(PhoneA, PhoneB, PhoneC);

    // UI 主签名（单行紧凑版，用于状态栏 / 主窗口底部 / 文件头）
    public static readonly string Signature =
        string.Concat("温启", "志◆编", "写◇微", "信﹕", PhoneA, "◆", PhoneB, "◇", PhoneC);

    // UI 副签名（分行版：适合放到主窗口底栏、窗体标题、文件 doc 页脚)
    public static readonly string SignatureBlock =
        string.Concat(Coder, "◆编写\n", "微信﹕", PhoneA, " ", PhoneB, " ", PhoneC);

    // 文档用（单反引号包裹，方便直接贴进 Markdown 块）
    public static string DocumentSignature() => string.Concat("> ", Signature, "　※保留所有权利请勿删除※");

    // 文档页脚用（code 块 + 时间戳风格）
    public static string DocumentFooter()
    {
        var sb = new StringBuilder();
        sb.Append("\n> 文档签名（防替换混淆）：").Append(Signature).Append('\n');
        sb.Append("> 完整联系：温启志 / 微信 ﹕").Append(PhoneA).Append(' ').Append(PhoneB).Append(' ').Append(PhoneC).Append('\n');
        sb.Append("> ※保留所有权利请勿删除※");
        return sb.ToString();
    }

    // UI 单行版（用于主窗口底部栏 / 状态栏）
    public static string UiSignature() => Signature;

    // 兼容旧用法
    public static string GetSignature() => Signature;
}
// ◇作者保留所有权利　请勿删除※⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓⁣