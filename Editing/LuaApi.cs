#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace NoCodeMotion.Editing
{
    public enum SymbolKind
    {
        Keyword,
        Function,
        Module,
        Field,
        Variable,
        Snippet
    }

    public sealed class LuaSymbol
    {
        public LuaSymbol(string name, SymbolKind kind, string signature, string description, string insertText = null)
        {
            Name = name;
            Kind = kind;
            Signature = signature;
            Description = description;
            InsertText = insertText;
        }

        public string Name { get; }
        public SymbolKind Kind { get; }
        public string Signature { get; }
        public string Description { get; }
        public string InsertText { get; }
    }

    /// <summary>Lua 5.2 / MoonSharp 标准库的智能提示数据。</summary>
    public static class LuaApi
    {
        public const char CaretMarker = '\u0001';

        public static readonly string[] Keywords =
        {
            "and", "break", "do", "else", "elseif", "end", "false", "for", "function", "goto",
            "if", "in", "local", "nil", "not", "or", "repeat", "return", "then", "true", "until", "while"
        };

        private static readonly List<LuaSymbol> GlobalsList = new List<LuaSymbol>
        {
            F("print", "print(...)", "把参数输出到控制台（输出面板）"),
            F("type", "type(v)", "返回值的类型名：nil/boolean/number/string/table/function"),
            F("tostring", "tostring(v)", "把任意值转换成字符串"),
            F("tonumber", "tonumber(v [, base])", "把字符串转成数字，失败返回 nil"),
            F("pairs", "pairs(t)", "遍历表的所有键值对：for k, v in pairs(t) do"),
            F("ipairs", "ipairs(t)", "按 1..n 顺序遍历数组部分：for i, v in ipairs(t) do"),
            F("next", "next(t [, key])", "返回表中的下一个键值对"),
            F("select", "select(index, ...)", "取变长参数的第 index 个；select('#', ...) 取个数"),
            F("assert", "assert(v [, message])", "断言：v 为假时抛出错误"),
            F("error", "error(message [, level])", "抛出一个错误"),
            F("pcall", "pcall(f, ...)", "保护调用，返回 ok, result 或 ok, errmsg"),
            F("xpcall", "xpcall(f, handler, ...)", "带错误处理函数的保护调用"),
            F("setmetatable", "setmetatable(t, meta)", "为表设置元表"),
            F("getmetatable", "getmetatable(t)", "获取表的元表"),
            F("rawget", "rawget(t, k)", "绕过元表直接读取字段"),
            F("rawset", "rawset(t, k, v)", "绕过元表直接写入字段"),
            F("rawequal", "rawequal(a, b)", "绕过元表比较是否相等"),
            F("rawlen", "rawlen(t)", "绕过元表取长度"),
            F("unpack", "unpack(t [, i [, j]])", "把数组展开成多个返回值"),
            F("require", "require(module)", "加载模块"),
            F("load", "load(chunk [, chunkname])", "把字符串 / 函数编译成 Lua 代码块"),
            F("collectgarbage", "collectgarbage([opt])", "控制垃圾回收器"),
            M("string", "字符串库：format / find / gsub / sub …"),
            M("table", "表操作库：insert / remove / concat / sort …"),
            M("math", "数学库：floor / random / max / pi …"),
            M("os", "系统库：time / date / clock …"),
            M("io", "输入输出库：write / read / open …"),
            M("coroutine", "协程库：create / resume / yield …"),
            M("json", "MoonSharp 提供的 JSON 库：parse / serialize"),
            new LuaSymbol("_G", SymbolKind.Variable, "_G", "全局环境表"),
            new LuaSymbol("_VERSION", SymbolKind.Variable, "_VERSION", "Lua 版本字符串")
        };

        private static readonly Dictionary<string, List<LuaSymbol>> Members =
            new Dictionary<string, List<LuaSymbol>>(StringComparer.Ordinal)
            {
                ["string"] = new List<LuaSymbol>
                {
                    F("format", "string.format(fmt, ...)", "格式化字符串，如 string.format('%d 分', 90)"),
                    F("len", "string.len(s)", "字符串长度，等价于 #s"),
                    F("sub", "string.sub(s, i [, j])", "取子串，索引从 1 开始，支持负数"),
                    F("upper", "string.upper(s)", "转大写"),
                    F("lower", "string.lower(s)", "转小写"),
                    F("rep", "string.rep(s, n [, sep])", "重复字符串 n 次"),
                    F("reverse", "string.reverse(s)", "反转字符串"),
                    F("find", "string.find(s, pattern [, init [, plain]])", "查找模式，返回起止位置"),
                    F("match", "string.match(s, pattern [, init])", "按模式匹配并返回捕获"),
                    F("gmatch", "string.gmatch(s, pattern)", "迭代所有匹配：for w in string.gmatch(s, '%a+') do"),
                    F("gsub", "string.gsub(s, pattern, repl [, n])", "替换匹配内容，返回新串和替换次数"),
                    F("byte", "string.byte(s [, i [, j]])", "取字符的数值编码"),
                    F("char", "string.char(...)", "由数值编码构造字符串"),
                    F("split", "string.split(s, sep)", "MoonSharp 扩展：按分隔符切分"),
                    F("contains", "string.contains(s, sub)", "MoonSharp 扩展：是否包含子串"),
                    F("startsWith", "string.startsWith(s, prefix)", "MoonSharp 扩展：前缀判断"),
                    F("endsWith", "string.endsWith(s, suffix)", "MoonSharp 扩展：后缀判断")
                },
                ["table"] = new List<LuaSymbol>
                {
                    F("insert", "table.insert(t, [pos,] value)", "插入元素，默认追加到末尾"),
                    F("remove", "table.remove(t [, pos])", "移除并返回元素，默认移除最后一个"),
                    F("concat", "table.concat(t [, sep [, i [, j]]])", "把数组元素连接成字符串"),
                    F("sort", "table.sort(t [, comp])", "排序，comp(a, b) 返回 a 是否应排在前面"),
                    F("unpack", "table.unpack(t [, i [, j]])", "展开数组为多返回值"),
                    F("pack", "table.pack(...)", "把变长参数打包成表，字段 n 为个数")
                },
                ["math"] = new List<LuaSymbol>
                {
                    F("floor", "math.floor(x)", "向下取整"),
                    F("ceil", "math.ceil(x)", "向上取整"),
                    F("abs", "math.abs(x)", "绝对值"),
                    F("max", "math.max(...)", "最大值"),
                    F("min", "math.min(...)", "最小值"),
                    F("sqrt", "math.sqrt(x)", "平方根"),
                    F("pow", "math.pow(x, y)", "x 的 y 次方"),
                    F("random", "math.random([m [, n]])", "随机数：无参 [0,1)，一参 [1,m]，两参 [m,n]"),
                    F("randomseed", "math.randomseed(x)", "设置随机种子"),
                    F("fmod", "math.fmod(x, y)", "取余"),
                    F("modf", "math.modf(x)", "拆分整数与小数部分"),
                    F("exp", "math.exp(x)", "自然指数"),
                    F("log", "math.log(x [, base])", "对数"),
                    F("sin", "math.sin(x)", "正弦（弧度）"),
                    F("cos", "math.cos(x)", "余弦（弧度）"),
                    F("tan", "math.tan(x)", "正切（弧度）"),
                    F("deg", "math.deg(x)", "弧度转角度"),
                    F("rad", "math.rad(x)", "角度转弧度"),
                    new LuaSymbol("pi", SymbolKind.Field, "math.pi", "圆周率 3.1415926…"),
                    new LuaSymbol("huge", SymbolKind.Field, "math.huge", "正无穷大")
                },
                ["os"] = new List<LuaSymbol>
                {
                    F("time", "os.time([table])", "取时间戳"),
                    F("date", "os.date([format [, time]])", "格式化日期，如 os.date('%Y-%m-%d')"),
                    F("clock", "os.clock()", "程序占用的 CPU 时间，常用于计时"),
                    F("difftime", "os.difftime(t2, t1)", "两个时间戳之差（秒）"),
                    F("getenv", "os.getenv(name)", "读取环境变量"),
                    F("exit", "os.exit([code])", "退出脚本")
                },
                ["io"] = new List<LuaSymbol>
                {
                    F("write", "io.write(...)", "写出内容（不换行）"),
                    F("read", "io.read([format])", "读取输入"),
                    F("open", "io.open(filename [, mode])", "打开文件，返回文件句柄"),
                    F("lines", "io.lines([filename])", "按行迭代文件"),
                    F("close", "io.close([file])", "关闭文件")
                },
                ["coroutine"] = new List<LuaSymbol>
                {
                    F("create", "coroutine.create(f)", "创建协程"),
                    F("resume", "coroutine.resume(co, ...)", "启动 / 继续协程"),
                    F("yield", "coroutine.yield(...)", "挂起当前协程"),
                    F("status", "coroutine.status(co)", "协程状态：running/suspended/dead"),
                    F("wrap", "coroutine.wrap(f)", "把协程包装成函数"),
                    F("running", "coroutine.running()", "返回当前协程")
                },
                ["json"] = new List<LuaSymbol>
                {
                    F("parse", "json.parse(str)", "把 JSON 字符串解析成表"),
                    F("serialize", "json.serialize(t)", "把表序列化成 JSON 字符串"),
                    F("isNull", "json.isNull(v)", "判断是否为 JSON null")
                }
            };

        public static readonly List<LuaSymbol> Snippets = new List<LuaSymbol>
        {
            S("if", "if 条件语句", "if " + CaretMarker + " then\n\nend"),
            S("ifelse", "if-else 语句", "if " + CaretMarker + " then\n\nelse\n\nend"),
            S("for", "数值 for 循环", "for i = 1, " + CaretMarker + " do\n\nend"),
            S("forp", "pairs 遍历", "for k, v in pairs(" + CaretMarker + ") do\n\nend"),
            S("fori", "ipairs 遍历", "for i, v in ipairs(" + CaretMarker + ") do\n\nend"),
            S("while", "while 循环", "while " + CaretMarker + " do\n\nend"),
            S("function", "函数定义", "function " + CaretMarker + "()\n\nend"),
            S("local function", "局部函数定义", "local function " + CaretMarker + "()\n\nend"),
            S("repeat", "repeat-until 循环", "repeat\n\t" + CaretMarker + "\nuntil ")
        };

        public static IReadOnlyList<LuaSymbol> Globals => GlobalsList;

        public static bool TryGetMembers(string module, out List<LuaSymbol> members) =>
            Members.TryGetValue(module, out members);

        public static IEnumerable<string> ModuleNames => Members.Keys;

        /// <summary>按名字查找（先全局、再各模块成员），用于悬停提示。</summary>
        public static LuaSymbol Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var hit = GlobalsList.FirstOrDefault(s => s.Name == name);
            if (hit != null) return hit;

            foreach (var kv in Members)
            {
                hit = kv.Value.FirstOrDefault(s => s.Name == name);
                if (hit != null) return hit;
            }

            if (Keywords.Contains(name))
                return new LuaSymbol(name, SymbolKind.Keyword, name, "Lua 关键字");

            return null;
        }

        /// <summary>查找 module.member 形式的符号。</summary>
        public static LuaSymbol FindMember(string module, string member)
        {
            if (module != null && Members.TryGetValue(module, out var list))
                return list.FirstOrDefault(s => s.Name == member);
            return null;
        }

        private static LuaSymbol F(string name, string signature, string desc) =>
            new LuaSymbol(name, SymbolKind.Function, signature, desc);

        private static LuaSymbol M(string name, string desc) =>
            new LuaSymbol(name, SymbolKind.Module, name, desc);

        private static LuaSymbol S(string name, string desc, string body) =>
            new LuaSymbol(name, SymbolKind.Snippet, name + " …", desc, body);
    }
}
