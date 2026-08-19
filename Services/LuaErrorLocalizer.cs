#nullable disable
using System.Text.RegularExpressions;
using MoonSharp.Interpreter;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 把 MoonSharp 的英文语法/运行时异常翻成中文调试信息：第几行 + 错误原因 + 解决思路。
    ///
    /// 用法：
    /// <code>
    /// var r = LuaErrorLocalizer.Localize(ex);   // ex 为 InterpreterException
    /// // r.Line  行号（取不到为 0）
    /// // r.Message 组合好的中文多行文案（不含 ✖ 前缀）
    /// </code>
    /// </summary>
    public sealed class LuaErrorLocalizer
    {
        /// <summary>本地化后的错误信息。</summary>
        public sealed class Report
        {
            public int Line;
            public string Type = "运行时错误";   // 语法错误 / 运行时错误
            public string Reason = string.Empty; // 中文原因
            public string Hint = string.Empty;   // 解决思路
            public string Raw = string.Empty;    // 原始英文
            public string Message = string.Empty; // 组合好的中文多行文案
        }

        private static readonly Regex Quoted = new Regex("'([^']*)'", RegexOptions.Compiled);
        private static readonly Regex LineInDecorated = new Regex(@":(\d+):", RegexOptions.Compiled);

        /// <summary>把 MoonSharp 异常翻译为中文报告。</summary>
        public static Report Localize(InterpreterException ex)
        {
            var raw = (ex?.Message ?? string.Empty).Trim();
            int line = ExtractLine(ex?.DecoratedMessage ?? raw);
            bool isSyntax = ex is SyntaxErrorException;

            string reason;
            string hint;

            if (ContainsCjk(raw))
            {
                // 自定义中文错误（如「找不到轴：轴1」），原样保留并给通用思路
                reason = raw;
                hint = "检查参数名称是否与项目里配置的对象（轴 / 输入 IO / 输出 IO / 气缸 / 通讯 / 料盘）一致；" +
                       "并确认相关的硬件接口（IHardwareBridge）已正确对接。";
            }
            else if (isSyntax)
            {
                (reason, hint) = LocalizeSyntax(raw);
            }
            else
            {
                (reason, hint) = LocalizeRuntime(raw);
            }

            var report = new Report
            {
                Line = line,
                Type = isSyntax ? "语法错误" : "运行时错误",
                Reason = reason,
                Hint = hint,
                Raw = raw
            };

            string where = line > 0 ? $"第 {line} 行" : "（行号未知）";
            report.Message = $"{where} {report.Type}\n" +
                             $"原因：{reason}\n" +
                             $"解决思路：{hint}\n" +
                             $"原始信息：{raw}";

            return report;
        }

        // ===================== 运行时错误 =====================

        private static (string reason, string hint) LocalizeRuntime(string raw)
        {
            if (raw.Contains("attempt to call a nil value"))
                return ("尝试调用一个为 nil 的函数 / 值（名字未定义或尚未赋值）。",
                    "检查该行被调用的名字是否拼写正确；它应是脚本里已定义的函数，或本编辑器提供的接口（轴 / IO / 气缸 / 通讯 / 料盘函数，且名称需与项目配置一致，例如 AxisMove(\"轴1\") 里的 \"轴1\" 必须在「轴」列表中存在）。");

            if (raw.Contains("attempt to call a '"))
                return ($"尝试把一个“{Quote(raw)}”类型的值当函数调用。",
                    "该变量不是函数。确认是否误把表 / 数字 / 字符串当函数调用，或少了运算符、多了括号。");

            if (raw.Contains("attempt to index a nil value"))
                return ("用 . 或 [] 访问了一个为 nil 的表的字段。",
                    "该表为 nil，先创建 / 赋值再访问；访问前用 if t then 判断是否存在。");

            if (raw.Contains("attempt to index a '"))
                return ($"对非表类型（“{Quote(raw)}”）做了字段访问（. 或 []）。",
                    "该值不是表，用 type(x) 查看实际类型；确认是否把变量用成了错误类型。");

            if (raw.Contains("attempt to perform arithmetic on a nil value"))
                return ("对 nil 做了算术运算（+ - * / 等）。",
                    "参与运算的变量为 nil，先给它赋值，或用 tonumber() 转换。");

            if (raw.Contains("attempt to perform arithmetic on a '"))
                return ($"对非数字类型（“{Quote(raw)}”）做了算术运算。",
                    "确认参与运算的变量是数字；字符串可用 tonumber() 转换。");

            if (raw.Contains("attempt to concatenate a nil value"))
                return ("用 .. 拼接了一个 nil 值。",
                    "拼接前确保变量非 nil，或改用 string.format / tostring() 包裹。");

            if (raw.Contains("attempt to compare a nil value"))
                return ("比较（> < == 等）时一侧为 nil。",
                    "比较前确保两侧都非 nil 且类型可比较。");

            if (raw.Contains("attempt to compare a '"))
                return ($"比较了不兼容的类型（“{Quote(raw)}”）。",
                    "确认两侧类型一致（通常都是数字或都是字符串）。");

            if (raw.Contains("attempt to get length of a nil value"))
                return ("对 nil 取长度（#）。",
                    "确保取长度的是字符串或表。");

            if (raw.Contains("attempt to get length of a '"))
                return ($"对“{Quote(raw)}”类型取长度（#），不支持。",
                    "只有字符串和表可以取长度（#）。");

            if (raw.Contains("bad argument"))
                return ($"函数参数类型 / 个数不对（{raw}）。",
                    "核对函数实参：数量、顺序、类型是否与定义一致；需要数字的先用 tonumber() 转换。");

            if (raw.Contains("expected, got"))
                return ($"参数类型不匹配（{raw}）。",
                    "检查调用处传入的参数类型，与函数定义对照。");

            if (raw.Contains("divide by zero") || raw.Contains("by zero"))
                return ("除以零。",
                    "做除法前先判断除数不为 0。");

            if (raw.Contains("too many"))
                return ($"参数过多（{raw}）。",
                    "减少实参数量，与函数定义保持一致。");

            if (raw.Contains("is not a"))
                return ($"类型不匹配（{raw}）。",
                    "检查变量类型，用 type() 辅助定位。");

            return (raw,
                "检查该行语法，或变量是否为 nil；可用 type(x) 打印变量类型辅助定位；必要时在该行前加 print(x) 观察变量实际值。");
        }

        // ===================== 语法错误 =====================

        private static (string reason, string hint) LocalizeSyntax(string raw)
        {
            if (raw.Contains("')' expected"))
                return ("括号不匹配，缺少右括号 ')'。",
                    "补齐缺失的 )，可对照报错里提示的左括号所在行。");

            if (raw.Contains("'end' expected") || raw.Contains("<eof>") || raw.Contains("expecting"))
                return ("代码块未闭合（缺少 end / ) / ]）。",
                    "for / if / function / while / repeat 等块需用 end（或 until）和对应的括号闭合；常见为 if / for / function 少写了 end。");

            if (raw.Contains("';' expected"))
                return ("缺少分号 ';'（通常可省略，多见于块结束处）。",
                    "检查上一行是否完整、是否有多余的逗号或语法残留。");

            if (raw.Contains("'=' expected"))
                return ("缺少赋值符号 '='。",
                    "赋值语句需写成 a = 表达式；或检查是否为不完整的语句。");

            if (raw.Contains("'then' expected"))
                return ("if 后面缺少 'then'。",
                    "写成 if 条件 then … end。");

            if (raw.Contains("'do' expected"))
                return ("for / while 后面缺少 'do'。",
                    "写成 for … do … end 或 while 条件 do … end。");

            if (raw.Contains("unexpected symbol"))
                return ($"出现了不该出现的符号（{Near(raw)}）。",
                    "检查该符号前后的语法：是否多了符号、漏写了 then / do / end，或括号未闭合。");

            if (raw.Contains("unfinished string"))
                return ("字符串没有用引号闭合。",
                    "检查引号 ' 或 \" 是否成对；多行文本用 [[ … ]] 长字符串。");

            if (raw.Contains("invalid escape sequence"))
                return ("字符串里的转义写法不合法（如 \\d）。",
                    "Lua 只支持 \\n \\t \\\\ \\\" 等有限转义；路径用 [[C:\\…]] 长字符串或双反斜杠 \\\\。");

            if (raw.Contains("function arguments expected"))
                return ("函数调用的参数列表写法不对。",
                    "检查函数名后是否紧跟 ( 参数 )，或是否漏写括号。");

            if (raw.Contains("function name expected"))
                return ("function 后面缺少函数名。",
                    "匿名函数用 function()；具名用 function 名字()。");

            if (raw.Contains("expected near") || raw.Contains("expected"))
                return ($"语法错误（{raw}）。",
                    "检查报错位置附近的括号、关键字（if / for / while / function / then / do / end）是否配对完整。");

            return (raw,
                "检查报错位置附近的括号与关键字（if / for / while / function / then / do / end）是否配对完整。");
        }

        // ===================== 工具 =====================

        private static string Quote(string raw)
        {
            var m = Quoted.Match(raw);
            return m.Success ? m.Groups[1].Value : "?";
        }

        private static string Near(string raw)
        {
            var m = Regex.Match(raw, "near '([^']*)'");
            return m.Success ? m.Groups[1].Value : "?";
        }

        /// <summary>从 MoonSharp 的 DecoratedMessage（如 [string "main"]:12: …）里提取行号。</summary>
        public static int ExtractLine(string decorated)
        {
            if (string.IsNullOrEmpty(decorated)) return 0;

            // [string "main"]:12: 或 chunk_1:12:
            var m = LineInDecorated.Match(decorated);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int line) && line > 0)
                return line;

            // (12, 列) 形式
            m = Regex.Match(decorated, @":\((\d+),");
            if (m.Success && int.TryParse(m.Groups[1].Value, out line) && line > 0)
                return line;

            // chunk_N:12 形式
            m = Regex.Match(decorated, @"chunk_\d+:(\d+)");
            if (m.Success && int.TryParse(m.Groups[1].Value, out line) && line > 0)
                return line;

            return 0;
        }

        private static bool ContainsCjk(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s)
            {
                if (c >= 0x4E00 && c <= 0x9FFF) return true;
            }
            return false;
        }
    }
}
