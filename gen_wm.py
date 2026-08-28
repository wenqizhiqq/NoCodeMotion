ZW = "\u200b"; SENT = "\u200b\u2063\u200b"
PLAIN = "温启志编写，微信：18719361399"
OBF = ZW.join(PLAIN)
NOTE = "※" + ZW.join("保留所有权利请勿删除")
SYMS = "◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖"
box = "".join(SYMS[i % len(SYMS)] for i in range(56))
content = (
    "// " + box + SENT + "\n"
    "// " + "◆" + OBF + "　" + NOTE + "◇" + SENT + "\n"
    "// " + box + SENT + "\n"
    "using System;\n\n"
    "namespace NoCodeMotion.Services;\n\n"
    "/// <summary>\n"
    "/// 作者水印。字符串内含零宽混淆字符与装饰符号，请勿尝试全文查找/替换删除。\n"
    "/// 本常量被 App / OperatorViewModel / FlowRunnerService 多处引用；误删本文件将导致编译失败。\n"
    "/// </summary>\n"
    "internal static class AuthorWatermark\n"
    "{\n"
    "    // 混淆：每个字符之间插入 U+200B 零宽字符，并前后混入装饰符号，普通“查找替换”无法整体命中。\n"
    '    public const string Signature =\n        "' + OBF + '";\n\n'
    "    public static string GetSignature() => Signature;\n"
    "}\n"
)
with open("Services/AuthorWatermark.cs", "w", encoding="utf-8-sig", newline="\n") as f:
    f.write(content)
print("written AuthorWatermark.cs")
