using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Debugging;

namespace NoCodeMotion.Services
{
    /// <summary>调试时可展示的一个 Lua 变量（名称 + 值文本）。</summary>
    public class LuaVar
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public bool IsInspected { get; set; }
    }

    /// <summary>用于中止正在运行的 Lua 脚本（在 GetAction 里抛出，被宿主捕获为“已停止”而非错误）。</summary>
    internal class LuaAbortException : Exception { }

    internal enum LuaDebugState { Idle, Paused, Running }

    /// <summary>
    /// 基于 MoonSharp（纯 C# Lua 5.2 解释器，无原生 DLL）的 Lua 调试宿主，实现 IDebugger：
    /// ①语法实时检测（CheckSyntax，由 VM 每秒调用一次）；
    /// ②运行 / 单步(StepOver) / 跳进(StepIn) / 跳出(StepOut) / 继续 / 暂停 / 停止（后台线程执行，行级暂停）；
    /// ③变量采集（当前作用域局部变量）+ 点击变量名求值查看名称与值；
    /// ④一键格式化（LuaFormatter）。
    /// 步进模型：引擎在“应当暂停”的源码行调用 GetAction，本类在此阻塞等待用户下发下一步指令。
    /// </summary>
    public class LuaDebugger : IDebugger
    {
        private Script? _script;
        private int _currentLine;
        private volatile LuaDebugState _state = LuaDebugState.Idle;
        private volatile DebuggerAction.ActionType _requestedAction = DebuggerAction.ActionType.Run;
        private volatile bool _breakRequested;   // 置 true 时强制引擎立即暂停并进入 GetAction
        private volatile bool _abort;
        private readonly ManualResetEventSlim _gate = new ManualResetEventSlim(false);
        private List<LuaVar> _locals = new List<LuaVar>();
        private readonly object _localsLock = new object();
        private CancellationTokenSource? _cts;

        public event Action<int, IList<LuaVar>>? Paused;   // 行号 + 局部变量快照
        public event Action<int, string>? Error;           // 行号 + 信息（含位置）
        public event Action? Finished;
        public event Action<string>? Output;

        public int CurrentLine => _currentLine;
        public bool IsDebugging => _state != LuaDebugState.Idle;

        // ---------- IDebugger 接口实现 ----------

        public DebuggerCaps GetDebuggerCaps() =>
            DebuggerCaps.CanDebugSourceCode | DebuggerCaps.HasLineBasedBreakpoints;

        public void SetDebugService(DebugService debugService) { }
        public void SetSourceCode(SourceCode sourceCode) { }
        public void SetByteCode(string[] byteCode) { }
        public bool IsPauseRequested() => _breakRequested;
        public bool SignalRuntimeException(ScriptRuntimeException ex) => false; // 让错误原样抛出，由宿主捕获
        public void SignalExecutionEnded() { }
        public List<DynamicExpression> GetWatchItems() => new List<DynamicExpression>();
        public void RefreshBreakpoints(IEnumerable<SourceRef> refs) { }

        public void Update(WatchType watchType, IEnumerable<WatchItem> items)
        {
            if (watchType == WatchType.Locals && items != null)
            {
                var list = new List<LuaVar>();
                foreach (var w in items)
                {
                    if (w == null) continue;
                    list.Add(new LuaVar { Name = w.Name ?? "", Value = FormatDyn(w.Value) });
                }
                lock (_localsLock) { _locals = list; }
            }
        }

        public DebuggerAction GetAction(int ip, SourceRef sourceref)
        {
            _currentLine = sourceref?.FromLine ?? 0;

            // 自由运行：直接放行，不阻塞。
            if (_requestedAction == DebuggerAction.ActionType.Run && !_abort)
                return new DebuggerAction { Action = DebuggerAction.ActionType.Run };

            // 用户已请求停止：直接抛出异常终止，不再展示暂停界面（避免停止后 UI 残留“已暂停”）。
            if (_abort) throw new LuaAbortException();

            // 入口前导指令（无源码行，FromLine=0）：跳过，不展示“第 0 行”，直接按当前动作继续到首行。
            if (_currentLine == 0)
            {
                _breakRequested = false;
                return new DebuggerAction { Action = _requestedAction };
            }

            // 步进：暂停并展示，然后阻塞等待用户下发下一步指令。
            _state = LuaDebugState.Paused;
            _breakRequested = false; // 消费初始暂停，避免每个指令都强制暂停
            IList<LuaVar> snap;
            lock (_localsLock) { snap = _locals.ToList(); }
            Paused?.Invoke(_currentLine, snap);

            _gate.Wait();
            _gate.Reset();

            if (_abort) throw new LuaAbortException();

            var act = _requestedAction;
            _state = LuaDebugState.Running;
            return new DebuggerAction { Action = act };
        }

        // ---------- 控制 API ----------

        /// <summary>启动执行。step=true 时在第一行即暂停（单步/跳进的起始）。</summary>
        public void Start(string code, bool step)
        {
            Stop();
            _abort = false;
            _currentLine = 0;
            _breakRequested = step; // 首行暂停
            _requestedAction = step ? DebuggerAction.ActionType.StepIn : DebuggerAction.ActionType.Run;
            _cts = new CancellationTokenSource();
            _script = new Script();
            _script.DebuggerEnabled = true;
            _script.Options.DebugPrint = s => Output?.Invoke(s);
            _script.AttachDebugger(this);
            _state = LuaDebugState.Running;
            Task.Run(() =>
            {
                try
                {
                    _script.DoString(code, null, "flow");
                    if (!_abort) Finished?.Invoke();
                }
                catch (LuaAbortException) { }
                catch (Exception ex) when (ex is InterpreterException && ex.InnerException is LuaAbortException) { }
                catch (SyntaxErrorException ex) { Error?.Invoke(ParseLine(ex.DecoratedMessage), ex.DecoratedMessage); }
                catch (InterpreterException ex) { Error?.Invoke(ParseLine(ex.DecoratedMessage), ex.DecoratedMessage); }
                catch (Exception ex) { Error?.Invoke(0, ex.Message); }
                finally { _state = LuaDebugState.Idle; }
            }, _cts.Token);
        }

        public void StepInto() { _requestedAction = DebuggerAction.ActionType.StepIn; _gate.Set(); }
        public void StepOver() { _requestedAction = DebuggerAction.ActionType.StepOver; _gate.Set(); }
        public void StepOut()  { _requestedAction = DebuggerAction.ActionType.StepOut; _gate.Set(); }
        public void Continue() { _requestedAction = DebuggerAction.ActionType.Run; _gate.Set(); }
        public void RequestBreak() { _breakRequested = true; _gate.Set(); } // 立即暂停（运行态）

        public void Stop()
        {
            _abort = true;
            _breakRequested = true; // 强制暂停以进入 GetAction，随后抛出哨兵异常终止
            try { _cts?.Cancel(); } catch { }
            _gate.Set();
            _state = LuaDebugState.Idle;
        }

        /// <summary>在暂停处对变量名求值（点击变量查看名称与值），返回“name = value”文本。</summary>
        public string Inspect(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return "";
            lock (_localsLock)
            {
                var hit = _locals.FirstOrDefault(v => v.Name == expression);
                if (hit != null) return hit.Name + " = " + hit.Value;
            }
            return expression + " = (当前作用域未找到)";
        }

        public IList<LuaVar> GetCurrentLocals()
        {
            lock (_localsLock) { return _locals.ToList(); }
        }

        // ---------- 静态工具 ----------

        /// <summary>仅编译（不执行）检测语法错误，返回行号与信息。</summary>
        public static (bool ok, int line, string msg) CheckSyntax(string code)
        {
            try { new Script().LoadString(code, null, "flow"); return (true, 0, ""); }
            catch (SyntaxErrorException ex) { return (false, ParseLine(ex.DecoratedMessage), ex.DecoratedMessage); }
            catch (InterpreterException ex) { return (false, ParseLine(ex.DecoratedMessage), ex.DecoratedMessage); }
            catch (Exception ex) { return (false, 0, ex.Message); }
        }

        public static string FormatLua(string code) => new LuaFormatter().Format(code);

        private static int ParseLine(string decorated)
        {
            if (string.IsNullOrEmpty(decorated)) return 0;
            var m = Regex.Match(decorated, @"\((\d+)\s*,\s*\d+\)");
            if (m.Success) return int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            m = Regex.Match(decorated, @":(\d+)");
            if (m.Success) return int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            return 0;
        }

        private static string FormatDyn(DynValue v)
        {
            if (v == null || v.IsNil()) return "nil";
            switch (v.Type)
            {
                case DataType.Table: return "{table}";
                case DataType.Function: return "function";
                case DataType.Boolean: return v.Boolean ? "true" : "false";
                case DataType.Number: return v.Number.ToString(CultureInfo.InvariantCulture);
                case DataType.String: return "\"" + v.String + "\"";
                default: return v.ToPrintString();
            }
        }

        // ---------- Lua 智能提示词库与补全 ----------
        private static readonly string[] LuaKeywords =
        {
            "and", "break", "do", "else", "elseif", "end", "false", "for", "function", "goto",
            "if", "in", "local", "nil", "not", "or", "repeat", "return", "then", "true", "until", "while"
        };
        private static readonly string[] LuaBuiltins =
        {
            "print", "pairs", "ipairs", "next", "type", "tostring", "tonumber", "select", "assert",
            "error", "pcall", "xpcall", "require", "load", "loadstring", "rawequal", "rawget", "rawset",
            "setmetatable", "getmetatable", "unpack", "collectgarbage", "coroutine", "utf8"
        };
        private static readonly string[] LuaMathFns =
        {
            "math.abs", "math.ceil", "math.floor", "math.max", "math.min", "math.sqrt", "math.sin",
            "math.cos", "math.tan", "math.pi", "math.random", "math.randomseed", "math.modf",
            "math.fmod", "math.huge", "math.log", "math.exp"
        };
        private static readonly string[] LuaStringFns =
        {
            "string.len", "string.sub", "string.find", "string.format", "string.gsub", "string.upper",
            "string.lower", "string.rep", "string.reverse", "string.byte", "string.char",
            "string.match", "string.gmatch"
        };
        private static readonly string[] LuaTableFns =
        {
            "table.insert", "table.remove", "table.sort", "table.concat", "table.pack",
            "table.unpack", "table.maxn"
        };

        /// <summary>
        /// 计算 Lua 编辑器在 caret 处的智能提示候选项。
        /// 命中时返回 true 且 items 非空，tokenStart 为当前待补全 token 的起始索引（用于整体替换）。
        /// </summary>
        public static bool TryGetCompletions(string source, int caret, out List<string> items, out int tokenStart)
        {
            items = new List<string>();
            tokenStart = caret;
            if (string.IsNullOrEmpty(source) || caret < 0 || caret > source.Length) return false;
            string before = source.Substring(0, caret);
            var m = Regex.Match(before, @"([A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*\.?)$");
            if (!m.Success) return false;
            string token = m.Value;
            tokenStart = caret - token.Length;
            bool hasDot = token.IndexOf('.') >= 0;

            var pool = new List<string>();
            pool.AddRange(LuaKeywords);
            pool.AddRange(LuaBuiltins);
            pool.AddRange(LuaMathFns);
            pool.AddRange(LuaStringFns);
            pool.AddRange(LuaTableFns);
            pool.AddRange(ParseLocals(source));

            var seen = new HashSet<string>();
            foreach (var c in pool)
            {
                bool match = hasDot
                    ? c.StartsWith(token, StringComparison.Ordinal)
                    : (c.StartsWith(token, StringComparison.Ordinal)
                       && (c.IndexOf('.') < 0 || c.StartsWith(token + ".", StringComparison.Ordinal)));
                if (match && seen.Add(c)) items.Add(c);
            }
            return items.Count > 0;
        }

        private static List<string> ParseLocals(string source)
        {
            var res = new List<string>();
            foreach (Match mm in Regex.Matches(source, @"\blocal\s+\w+"))
            {
                var name = mm.Value.Substring(mm.Value.IndexOf(' ') + 1);
                res.Add(name);
            }
            foreach (Match mm in Regex.Matches(source, @"\bfunction\s+([A-Za-z_]\w*)"))
                res.Add(mm.Groups[1].Value);
            foreach (Match mm in Regex.Matches(source, @"\b([A-Za-z_]\w*)\s*=\s*function"))
                res.Add(mm.Groups[1].Value);
            return res;
        }
    }

    /// <summary>Lua 缩进格式化（轻量）：按 if/for/while/do/function/repeat 增缩进，end/until 减缩进，else/elseif 同级重开。</summary>
    internal class LuaFormatter
    {
        private static readonly HashSet<string> Openers = new() { "do", "function", "repeat", "if", "for", "while" };

        public string Format(string code)
        {
            if (string.IsNullOrEmpty(code)) return code;
            var lines = code.Replace("\r\n", "\n").Split('\n');
            var outLines = new List<string>();
            int indent = 0;
            const int Unit = 4;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0) { outLines.Add(""); continue; }
                string head = line;
                int comment = line.IndexOf("--");
                if (comment >= 0 && !line.Substring(0, comment).Contains('"'))
                    head = line.Substring(0, comment).Trim();
                bool isElse = head.StartsWith("else") || head.StartsWith("elseif");
                bool closes = head.StartsWith("end") || head.StartsWith("until") || isElse;
                bool opens = Openers.Contains(FirstWord(head));
                if (closes) indent = Math.Max(0, indent - 1);
                if (isElse)
                {
                    outLines.Add(new string(' ', indent * Unit) + line);
                    indent++;
                    continue;
                }
                outLines.Add(new string(' ', indent * Unit) + line);
                if (opens) indent++;
            }
            return string.Join("\n", outLines);
        }

        private static string FirstWord(string s)
        {
            int i = 0;
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t')) i++;
            int j = i;
            while (j < s.Length && s[j] != ' ' && s[j] != '\t' && s[j] != '(' && s[j] != ')') j++;
            return s.Substring(i, j - i);
        }
    }
}
