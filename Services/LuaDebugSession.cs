#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Debugging;

namespace NoCodeMotion.Services
{
    public enum SessionState
    {
        Idle,
        Running,
        Paused
    }

    public enum LogKind
    {
        Info,
        Output,
        Error,
        Success
    }

    /// <summary>用户主动停止脚本时，从调试回调里抛出以展开 Lua 虚拟机调用栈。</summary>
    public sealed class ScriptTerminatedException : Exception
    {
        public ScriptTerminatedException() : base("脚本被用户终止") { }
    }

    public sealed class ExecutionEndedInfo
    {
        public bool IsError { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ErrorLine { get; set; }
        public bool Terminated { get; set; }
        public long ElapsedMs { get; set; }
        public List<VarInfo> Globals { get; set; } = new List<VarInfo>();
    }

    /// <summary>
    /// 基于 MoonSharp 的 Lua 调试会话。
    /// 脚本在后台线程执行，命中断点或单步停止时阻塞该线程，
    /// 并把变量 / 调用栈快照通过事件抛给 UI 线程。
    /// </summary>
    public sealed class LuaDebugSession : IDebugger
    {
        private const int MaxTableDepth = 3;
        private const int MaxTableItems = 100;

        private readonly object _sync = new object();
        private readonly AutoResetEvent _resumeEvent = new AutoResetEvent(false);
        private readonly HashSet<int> _breakpoints = new HashSet<int>();

        private Script _script;
        private SourceCode _sourceCode;
        private DebugService _debugService;
        private Thread _worker;
        private HashSet<string> _baselineGlobals = new HashSet<string>();

        private volatile bool _pauseRequested;
        private volatile bool _stopRequested;
        private volatile bool _running;

        private DebuggerAction.ActionType _nextAction = DebuggerAction.ActionType.Run;
        private List<WatchItem> _locals = new List<WatchItem>();
        private List<WatchItem> _callStack = new List<WatchItem>();
        private ScriptRuntimeException _pendingError;
        private int _lastPauseLine = -1;
        private int _lastPauseDepth = -1;
        private int _lastPauseIp = -1;

        // 每行执行耗时（毫秒，含该行所有指令累计）。line→累计毫秒。
        private readonly Dictionary<int, double> _lineTimes = new Dictionary<int, double>();
        private readonly object _lineTimeSync = new object();
        private int _measureLine = -1;
        private Stopwatch _measureStart;

        /// <summary>输出（print / 运行信息 / 错误）。在后台线程触发。</summary>
        public event Action<string, LogKind> Log;

        /// <summary>脚本挂起。在后台线程触发，UI 需自行 Dispatcher 切换。</summary>
        public event Action<PauseInfo> Paused;

        /// <summary>脚本结束。在后台线程触发。</summary>
        public event Action<ExecutionEndedInfo> Ended;

        public SessionState State { get; private set; } = SessionState.Idle;

        public bool IsBusy => State != SessionState.Idle;

        #region 生命周期控制

        public void Start(string code, bool breakAtEntry)
        {
            if (IsBusy) return;

            _stopRequested = false;
            _pauseRequested = false;
            _pendingError = null;
            _locals = new List<WatchItem>();
            _callStack = new List<WatchItem>();
            _nextAction = breakAtEntry ? DebuggerAction.ActionType.StepIn : DebuggerAction.ActionType.Run;
            State = SessionState.Running;
            _running = true;

            _worker = new Thread(() => Execute(code, breakAtEntry))
            {
                IsBackground = true,
                Name = "LuaScriptThread"
            };
            _worker.Start();
        }

        private void Execute(string code, bool breakAtEntry)
        {
            var sw = Stopwatch.StartNew();
            var info = new ExecutionEndedInfo();

            // 每行耗时统计：清空上一次运行的数据，并启动总测量计时
            _lineTimes.Clear();
            _measureLine = -1;
            _measureStart = Stopwatch.StartNew();

            try
            {
                _script = new Script(CoreModules.Preset_Complete);
                _script.Options.DebugPrint = s => Log?.Invoke(s, LogKind.Output);

                // 硬件装配：首次运行脚本时按环境自动选择（有 LTDMC.dll → 雷赛控制卡 + 真实通讯，
                // 否则仿真桩），并把硬件层日志接到输出面板。
                Hardware.HardwareLog.Sink = s => Log?.Invoke(s, LogKind.Output);
                Hardware.HardwareSetup.EnsureInitialized();

                // 预留硬件接口：把轴/IO/气缸/通讯/料盘的运动控制函数注册成 Lua 全局函数。
                // 名称解析与 Lua 绑定在 HardwareApi 里完成，真正的设备对接在 IHardwareBridge。
                HardwareApi.Register(_script, new HardwareApi(HardwareBridge.Current, s => Log?.Invoke(s, LogKind.Output)));

                _baselineGlobals = new HashSet<string>(
                    _script.Globals.Pairs.Select(p => p.Key.CastToString() ?? string.Empty));

                // 先挂调试器，LoadString 时才能拿到 SourceCode 并布置断点
                _script.AttachDebugger(this);

                DynValue chunk = _script.LoadString(code, null, "main");
                ApplyBreakpointsCore();

                _script.Call(chunk);

                info.Message = "脚本执行完毕";
            }
            catch (ScriptTerminatedException)
            {
                info.Terminated = true;
                info.Message = "脚本已被停止";
            }
            catch (InterpreterException ex)
            {
                var r = LuaErrorLocalizer.Localize(ex);
                info.IsError = true;
                info.Message = r.Message;
                info.ErrorLine = r.Line;
            }
            catch (Exception ex) when (IsTermination(ex))
            {
                info.Terminated = true;
                info.Message = "脚本已被停止";
            }
            catch (Exception ex)
            {
                info.IsError = true;
                info.Message = "宿主异常：" + ex.Message;
            }
            finally
            {
                sw.Stop();
                info.ElapsedMs = sw.ElapsedMilliseconds;

                // 结算最后一行的累计耗时
                if (_measureLine > 0 && _measureStart != null)
                {
                    double dt = _measureStart.Elapsed.TotalMilliseconds;
                    lock (_lineTimeSync)
                    {
                        _lineTimes[_measureLine] =
                            _lineTimes.TryGetValue(_measureLine, out double prev) ? prev + dt : dt;
                    }
                }
                _measureLine = -1;

                try
                {
                    info.Globals = SnapshotGlobals();
                }
                catch
                {
                    // 忽略快照失败
                }

                _running = false;
                State = SessionState.Idle;
                _pauseRequested = false;
                Ended?.Invoke(info);
            }
        }

        private static bool IsTermination(Exception ex)
        {
            while (ex != null)
            {
                if (ex is ScriptTerminatedException) return true;
                ex = ex.InnerException;
            }
            return false;
        }

        /// <summary>继续执行 / 单步。仅在 Paused 状态有效。</summary>
        public void Resume(DebuggerAction.ActionType action)
        {
            if (State != SessionState.Paused) return;
            _nextAction = action;
            // 复位计时起点，排除暂停（用户思考 / 单步）期间的时间
            if (_measureLine > 0 && _measureStart != null) _measureStart.Restart();
            State = SessionState.Running;
            _resumeEvent.Set();
        }

        /// <summary>返回每行累计耗时（毫秒）的只读快照，供 UI 边栏显示。</summary>
        public IReadOnlyDictionary<int, double> GetLineTimesSnapshot()
        {
            lock (_lineTimeSync)
                return new Dictionary<int, double>(_lineTimes);
        }

        /// <summary>请求中断正在运行的脚本（下一条指令处挂起）。</summary>
        public void RequestPause()
        {
            if (State == SessionState.Running) _pauseRequested = true;
        }

        /// <summary>停止脚本。运行中或挂起时都可用。</summary>
        public void Stop()
        {
            if (!_running) return;
            _stopRequested = true;
            _pauseRequested = true;
            _resumeEvent.Set();
        }

        #endregion

        #region 断点

        public void SetBreakpoints(IEnumerable<int> lines)
        {
            lock (_sync)
            {
                _breakpoints.Clear();
                foreach (int l in lines) _breakpoints.Add(l);
            }
            ApplyBreakpointsCore();
        }

        private void ApplyBreakpointsCore()
        {
            if (_debugService == null || _sourceCode == null) return;

            HashSet<int> lines;
            lock (_sync) lines = new HashSet<int>(_breakpoints);

            try
            {
                _debugService.ResetBreakPoints(_sourceCode, lines);
            }
            catch
            {
                // 脚本尚未编译完成时忽略
            }
        }

        #endregion

        #region IDebugger 实现

        public DebuggerCaps GetDebuggerCaps()
        {
            return DebuggerCaps.CanDebugSourceCode | DebuggerCaps.HasLineBasedBreakpoints;
        }

        public void SetDebugService(DebugService debugService)
        {
            _debugService = debugService;
            ApplyBreakpointsCore();
        }

        public void SetSourceCode(SourceCode sourceCode)
        {
            _sourceCode = sourceCode;
            ApplyBreakpointsCore();
        }

        public void SetByteCode(string[] byteCode)
        {
            // 不做字节码级调试
        }

        public bool IsPauseRequested() => _pauseRequested || _stopRequested;

        public bool SignalRuntimeException(ScriptRuntimeException ex)
        {
            _pendingError = ex;
            return true;    // 让虚拟机在出错位置停下来，方便查看变量
        }

        public DebuggerAction GetAction(int ip, SourceRef sourceref)
        {
            if (_stopRequested) throw new ScriptTerminatedException();

            // —— 每行执行耗时统计 ——
            // 仅在遇到真实源行（FromLine>0）时，结算上一行的累计耗时并开始计时当前行。
            // 同一行被多次访问（循环 / 函数内多语句）时保持累计，不重复结算。
            // 暂停期间（用户思考 / 单步）不计入：Resume 时复位计时起点。
            if (sourceref != null && sourceref.FromLine > 0 && _measureStart != null)
            {
                int line = sourceref.FromLine;
                if (_measureLine != line)
                {
                    if (_measureLine > 0)
                    {
                        double dt = _measureStart.Elapsed.TotalMilliseconds;
                        lock (_lineTimeSync)
                        {
                            _lineTimes[_measureLine] =
                                _lineTimes.TryGetValue(_measureLine, out double prev) ? prev + dt : dt;
                        }
                    }
                    _measureLine = line;
                    _measureStart.Restart();
                }
            }

            bool isBreakpoint = sourceref != null && sourceref.Breakpoint;
            bool userPause = _pauseRequested;
            bool hasError = _pendingError != null;

            // 处于“继续运行”模式且不是断点 / 暂停请求 / 错误：放行。
            // 注意：返回 StepOver 而非 Run —— MoonSharp 在 Run 模式下会在 ListenDebugger
            // 直接 return，整段运行都不再回调 GetAction，导致每行耗时无法累计；
            // 返回 StepOver 后引擎会在每一行真实源行重新回调 GetAction（从而能逐行计时），
            // 但本分支在暂停逻辑之前返回，不会真正卡住 UI，脚本照常跑完。
            if (!userPause && !hasError &&
                _nextAction == DebuggerAction.ActionType.Run &&
                !isBreakpoint)
            {
                return new DebuggerAction { Action = DebuggerAction.ActionType.StepOver };
            }

            // 代码块入口等没有真实行号的位置不值得停，按原动作继续
            if (!hasError && (sourceref == null || sourceref.FromLine <= 0))
            {
                return new DebuggerAction { Action = _nextAction };
            }

            // 同一条语句可能被拆成多个 SourceRef，单步时不重复停靠。
            // 判据：行号与栈深都没变，且指令指针在前进（循环回跳时 ip 变小，仍会正常停）。
            if (!hasError && !userPause && !isBreakpoint &&
                sourceref.FromLine == _lastPauseLine &&
                _callStack.Count == _lastPauseDepth &&
                ip > _lastPauseIp)
            {
                _lastPauseIp = ip;
                return new DebuggerAction { Action = _nextAction };
            }

            _lastPauseLine = sourceref.FromLine;
            _lastPauseDepth = _callStack.Count;
            _lastPauseIp = ip;

            _pauseRequested = false;
            State = SessionState.Paused;

            var info = new PauseInfo
            {
                Line = sourceref != null ? sourceref.FromLine : 0,
                Locals = BuildLocals(),
                Globals = SnapshotGlobals(),
                CallStack = BuildCallStack()
            };

            if (hasError)
            {
                var r = LuaErrorLocalizer.Localize(_pendingError);
                info.IsError = true;
                info.Message = r.Message;
                info.Line = r.Line;
                _pendingError = null;
            }

            Paused?.Invoke(info);

            _resumeEvent.WaitOne();

            if (_stopRequested) throw new ScriptTerminatedException();

            return new DebuggerAction { Action = _nextAction };
        }

        public void SignalExecutionEnded()
        {
            // 结束处理统一放在 Execute 的 finally
        }

        public void Update(WatchType watchType, IEnumerable<WatchItem> items)
        {
            if (items == null) return;

            switch (watchType)
            {
                case WatchType.Locals:
                    _locals = items.ToList();
                    break;
                case WatchType.CallStack:
                    _callStack = items.ToList();
                    break;
            }
        }

        public List<DynamicExpression> GetWatchItems() => new List<DynamicExpression>();

        public void RefreshBreakpoints(IEnumerable<SourceRef> refs)
        {
            // 断点集合由 UI 维护，这里无需回写
        }

        #endregion

        #region 变量快照

        /// <summary>虚拟机内部符号，不展示给用户。</summary>
        private static readonly HashSet<string> HiddenLocals = new HashSet<string>(StringComparer.Ordinal)
        {
            "_ENV", "...", "(temporary)"
        };

        private List<VarInfo> BuildLocals()
        {
            var list = new List<VarInfo>();
            var seen = new HashSet<string>();

            foreach (var w in _locals)
            {
                if (w == null || string.IsNullOrEmpty(w.Name)) continue;
                if (HiddenLocals.Contains(w.Name)) continue;
                if (!seen.Add(w.Name)) continue;
                var v = Describe(w.Name, w.Value, 0);
                v.Scope = "局部";
                list.Add(v);
            }

            return list;
        }

        private List<string> BuildCallStack()
        {
            var list = new List<string>();
            foreach (var w in _callStack)
            {
                string name = string.IsNullOrEmpty(w.Name) ? "(匿名函数)" : w.Name;
                string loc = w.Location != null ? $"  行 {w.Location.FromLine}" : string.Empty;
                list.Add(name + loc);
            }
            if (list.Count == 0) list.Add("(主程序)");
            return list;
        }

        /// <summary>只快照用户定义的全局变量，过滤掉标准库。</summary>
        public List<VarInfo> SnapshotGlobals()
        {
            var list = new List<VarInfo>();
            if (_script == null) return list;

            foreach (var pair in _script.Globals.Pairs)
            {
                string key = pair.Key.CastToString();
                if (string.IsNullOrEmpty(key)) continue;
                if (_baselineGlobals.Contains(key)) continue;

                var v = Describe(key, pair.Value, 0);
                v.Scope = "全局";
                list.Add(v);
            }

            return list.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private VarInfo Describe(string name, DynValue value, int depth)
        {
            var info = new VarInfo
            {
                Name = name,
                TypeName = value == null ? "nil" : value.Type.ToLuaTypeString(),
                Value = FormatValue(value)
            };

            if (value != null && value.Type == DataType.Table && depth < MaxTableDepth)
            {
                int count = 0;
                foreach (var pair in value.Table.Pairs)
                {
                    if (count++ >= MaxTableItems)
                    {
                        info.Children.Add(new VarInfo { Name = "…", Value = "(更多项已省略)", TypeName = string.Empty });
                        break;
                    }

                    string childName = pair.Key.Type == DataType.String
                        ? pair.Key.String
                        : "[" + FormatValue(pair.Key) + "]";

                    info.Children.Add(Describe(childName, pair.Value, depth + 1));
                }
            }

            return info;
        }

        public static string FormatValue(DynValue value)
        {
            if (value == null) return "nil";

            switch (value.Type)
            {
                case DataType.Nil:
                case DataType.Void:
                    return "nil";
                case DataType.Boolean:
                    return value.Boolean ? "true" : "false";
                case DataType.Number:
                    double d = value.Number;
                    return Math.Abs(d % 1) < double.Epsilon && Math.Abs(d) < 1e15
                        ? ((long)d).ToString()
                        : d.ToString("R");
                case DataType.String:
                    return "\"" + Escape(value.String) + "\"";
                case DataType.Table:
                    return DescribeTable(value.Table);
                case DataType.Function:
                case DataType.ClrFunction:
                    return "function";
                default:
                    try { return value.ToDebugPrintString(); }
                    catch { return value.Type.ToLuaTypeString(); }
            }
        }

        private static string DescribeTable(Table table)
        {
            if (table == null) return "table";

            int arrayLen = table.Length;
            int total = 0;
            var sb = new StringBuilder();
            sb.Append('{');

            foreach (var pair in table.Pairs)
            {
                total++;
                if (total > 4) continue;
                if (total > 1) sb.Append(", ");

                if (pair.Key.Type == DataType.String)
                    sb.Append(pair.Key.String).Append('=');

                var v = pair.Value;
                sb.Append(v.Type == DataType.Table ? "{…}" : FormatValue(v));
            }

            if (total > 4) sb.Append(", …");
            sb.Append('}');

            string prefix = arrayLen > 0 ? $"table[{arrayLen}] " : $"table({total}) ";
            return prefix + sb;
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            if (s.Length > 120) s = s.Substring(0, 120) + "…";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", string.Empty);
        }

        private static int ExtractLine(string decorated)
        {
            if (string.IsNullOrEmpty(decorated)) return 0;
            var m = Regex.Match(decorated, @":\((\d+),");
            if (m.Success && int.TryParse(m.Groups[1].Value, out int line)) return line;
            m = Regex.Match(decorated, @"chunk_\d+:(\d+)");
            if (m.Success && int.TryParse(m.Groups[1].Value, out line)) return line;
            return 0;
        }

        #endregion

        /// <summary>脚本结束后仍可用于查看全局变量的最后一份运行环境。</summary>
        public Script CurrentScript => _script;

        /// <summary>仅编译检测语法错误，返回行号与信息（供每秒实时检测使用）。</summary>
        public static (bool ok, int line, string msg) CheckSyntax(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return (true, 0, string.Empty);
            try
            {
                new Script(CoreModules.Preset_Complete).LoadString(code, null, "main");
                return (true, 0, string.Empty);
            }
            catch (SyntaxErrorException ex)
            {
                var r = LuaErrorLocalizer.Localize(ex);
                return (false, r.Line, r.Message);
            }
            catch (InterpreterException ex)
            {
                var r = LuaErrorLocalizer.Localize(ex);
                return (false, r.Line, r.Message);
            }
            catch (Exception ex) { return (false, 0, ex.Message); }
        }
    }
}
