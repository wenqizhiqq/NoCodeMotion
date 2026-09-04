// ◆◇※▣▤ۥ▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤ۥ▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣ۤۥ▦▧▨۩░▒▓✦
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// =====================================================================
// 3D 仿真流程播放器：把 FlowItem（表格流程 / 节点图流程）编译成带时序的动作序列，
// 在 DispatcherTimer 上逐帧驱动 AxisRuntimeState（轴）+ SimRuntime（IO/气缸/相机），
// 参数化机台每帧自动读这些状态刷新网格，从而"跑起来"。
// 与真实硬件解耦——绝不调用 HardwareBridge，纯虚拟仿真。
// =====================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Threading;
using NoCodeMotion.Models;
using NoCodeMotion.Models.NodeGraph;

namespace NoCodeMotion.Services
{
    /// <summary>单条仿真动作（一次可含轴移动 / IO / 气缸 / 相机闪光 / 变量写入其一，并带显示时长）。</summary>
    internal sealed class SimAction
    {
        public string? AxisName;
        public double AxisTarget;
        public string? IoName;
        public int IoValue;
        public string? CylName;
        public int CylState;
        public string? CamName;
        public string? VarName;
        public double VarValue;
        /// <summary>若设置，则在 Apply 时按当前变量值实时求值为 VarValue（支持 {变量名}/算术表达式）。</summary>
        public string? VarExpr;
        public int DurationMs;
        public string Label = "";
    }

    public sealed class SimFlowPlayer
    {
        public event Action<int, int>? Progress;     // (当前步序号 1-based, 总步数)
        public event Action? Completed;
        public event Action<string>? Log;
        public event Action? StateChanged;

        private DispatcherTimer? _timer;
        private List<SimAction> _actions = new();
        private int _idx = -1;
        private double _elapsedMs;
        private bool _started;
        private readonly Dictionary<string, double> _cur = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, double> _tgt = new(StringComparer.OrdinalIgnoreCase);

        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public string? CurrentLabel { get; private set; }

        /// <summary>已装载流程的步数（供"流程预览"等 UI 显示）。</summary>
        public int StepCount => _actions.Count;
        /// <summary>已装载流程每步的显示文案（供"流程预览"等 UI 显示）。</summary>
        public IReadOnlyList<string> StepLabels => _actions.Select(a => a.Label).ToList();

        /// <summary>在不启动定时器、不重置仿真状态的前提下，编译流程并返回步骤文案列表（供"流程预览"使用）。</summary>
        public static IReadOnlyList<string> PreviewSteps(FlowItem? flow)
            => Compile(flow).Select(a => a.Label).ToList();

        // ===================== 装载 =====================
        public void Load(FlowItem? flow)
        {
            Stop();
            _actions = Compile(flow);
            _idx = -1;
            _started = false;
            _cur.Clear();
            _tgt.Clear();
            Log?.Invoke($"已装载流程：{flow?.Name ?? "(空)"}，共 {_actions.Count} 步");
            Progress?.Invoke(0, _actions.Count);
            StateChanged?.Invoke();
        }

        // ===================== 控制 =====================
        public void Play()
        {
            if (_actions.Count == 0) { Log?.Invoke("无流程步骤可运行"); return; }
            _timer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _timer.Tick -= OnTick;
            _timer.Tick += OnTick;
            IsRunning = true;
            IsPaused = false;
            _timer.Start();
            StateChanged?.Invoke();
            Log?.Invoke("▶ 仿真开始");
        }

        public void Pause()
        {
            if (!IsRunning) return;
            IsPaused = true;
            _timer?.Stop();
            StateChanged?.Invoke();
            Log?.Invoke("⏸ 已暂停");
        }

        public void Stop()
        {
            _timer?.Stop();
            IsRunning = false;
            IsPaused = false;
            SimRuntime.Reset();          // 气缸缩回 / 相机闪光清除 / IO 清零
            StateChanged?.Invoke();
        }

        // ===================== 主循环 =====================
        private void OnTick(object? sender, EventArgs e)
        {
            double dur = _idx >= 0 ? _actions[_idx].DurationMs : 0;
            if (_idx < 0 || _elapsedMs >= dur)
            {
                if (_idx + 1 >= _actions.Count)
                {
                    _timer?.Stop();
                    IsRunning = false;
                    IsPaused = false;
                    Completed?.Invoke();
                    StateChanged?.Invoke();
                    Log?.Invoke("■ 仿真结束");
                    return;
                }
                _idx++;
                _elapsedMs = 0;
                _started = true;
                var a = _actions[_idx];
                Apply(a);
                CurrentLabel = a.Label;
                Progress?.Invoke(_idx + 1, _actions.Count);
                Log?.Invoke($"[{_idx + 1}/{_actions.Count}] {a.Label}");
            }
            else
            {
                _elapsedMs += 33;
            }

            // 轴位置低通插值，写入 AxisRuntimeState（机台每帧读取刷新）
            foreach (var kv in _tgt)
            {
                string ax = kv.Key;
                double tgt = kv.Value;
                double cur = _cur.TryGetValue(ax, out var c) ? c : tgt;
                double next = cur + (tgt - cur) * 0.18;
                if (Math.Abs(tgt - next) < 0.01) next = tgt;
                _cur[ax] = next;
                AxisRuntimeState.Set(ax, next);
            }
        }

        private void Apply(SimAction a)
        {
            if (!string.IsNullOrEmpty(a.AxisName))
            {
                if (!_cur.ContainsKey(a.AxisName))
                    _cur[a.AxisName] = AxisRuntimeState.Get(a.AxisName);
                _tgt[a.AxisName] = a.AxisTarget;
            }
            if (!string.IsNullOrEmpty(a.IoName)) SimRuntime.SetOutput(a.IoName, a.IoValue);
            if (!string.IsNullOrEmpty(a.CylName)) SimRuntime.SetCylinder(a.CylName, a.CylState);
            if (!string.IsNullOrEmpty(a.CamName)) SimRuntime.FlashCamera(a.CamName);
            if (!string.IsNullOrEmpty(a.VarExpr))
            {
                var ok = ExpressionEvaluator.Evaluate(a.VarExpr, n => SimRuntime.GetVariableResolved(n), out var ev);
                SimRuntime.SetVariable(a.VarName ?? "", ok ? ev : 0);
            }
            else if (!string.IsNullOrEmpty(a.VarName)) SimRuntime.SetVariable(a.VarName, a.VarValue);
        }

        // ===================== 编译：表格流程 =====================
        private static List<SimAction> Compile(FlowItem? flow)
        {
            var list = new List<SimAction>();
            if (flow == null) return list;
            if (flow.Kind == FlowKind.NodeGraph)
                return CompileNodeGraph(NgDoc.FromJson(flow.GraphJson));
            if (flow.Kind != FlowKind.Table) return list;

            foreach (var s in flow.Steps)
            {
                string func = (s.Function ?? "").Trim();
                if (string.IsNullOrEmpty(func)) continue;     // 纯逻辑行（如果/否则/结束）跳过
                string name = s.Name ?? "";
                string op = (s.Operation ?? "").Trim();
                string setv = (s.SetValue ?? "").Trim();

                switch (func)
                {
                    case "轴":
                        if (string.IsNullOrEmpty(name))
                        {
                            // 延时占位（Delay 助手用 轴+空名+SetValue=0 表示）
                            if (s.DurationMs > 0) list.Add(DelayAction(s.DurationMs, "延时"));
                        }
                        else list.Add(AxisAction(name, op, setv,
                                     double.TryParse(s.Property, out var sp) ? sp : 0));
                        break;
                    case "IO":
                    case "IO输出":
                        // 输出点→置位；输入点（等待）→仿真里当作短延时
                        var outIo = HardwareResolver.ResolveOutput(name);
                        if (outIo != null) list.Add(IoAction(name, ParseInt(setv, 0)));
                        else list.Add(DelayAction(300, $"等待输入 {name}"));
                        break;
                    case "气缸":
                        int st = (setv == "0" || setv == "缩回" || setv == "retract" || setv == "复位") ? 0 : 1;
                        list.Add(CylAction(name, st));
                        break;
                    case "点位":
                        list.AddRange(PointActions(name, setv));
                        break;
                    case "相机":
                        list.Add(CamAction(CameraNameOf(setv)));
                        break;
                    case "延时":
                        list.Add(DelayAction(ParseInt(setv, 300), "延时"));
                        break;
                    case "modbus":
                    case "Modbus":
                        list.Add(LogAction($"[Modbus] {name} {setv}"));
                        break;
                    case "变量":
                        // 运行时按当前变量值实时求值（支持 算术表达式 / 引用其它变量），写回仿真变量仓。
                        list.Add(new SimAction
                        {
                            VarName = name,
                            VarExpr = setv,
                            DurationMs = 250,
                            Label = $"变量 {name} = {setv}"
                        });
                        break;
                    case "系统":
                        list.Add(LogAction($"[系统] {setv}"));
                        break;
                }
            }
            return list;
        }

        // ===================== 编译：节点图流程 =====================
        // 真正的图执行器：从 Start 出发，遇到 条件分支 按状态求值选 True/False 分支，
        // 遇到 循环 把 Body 子图按次数展开（支持嵌套），从而把带逻辑/变量的节点图
        // 编译成一条确定时序的仿真动作序列。编译期用一份"虚拟状态"跟踪轴位置与变量，
        // 使 条件分支 能基于前面节点的执行结果正确选路。
        private static List<SimAction> CompileNodeGraph(NgDoc doc)
        {
            var list = new List<SimAction>();
            if (doc.Nodes.Count == 0) return list;
            var start = doc.Nodes.FirstOrDefault(n => n.Kind == NgKind.Start);
            if (start == null) return list;

            var state = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            SeedState(state);

            int guard = 0;
            ExecuteFrom(doc, start, state, list, null, ref guard);
            return list;
        }

        /// <summary>从 node 沿执行流递归生成动作，直到遇到 End / 回到 stopAt（循环回边）/ 图尾。</summary>
        private static void ExecuteFrom(NgDoc doc, NgNode? node, Dictionary<string, double> state,
            List<SimAction> list, NgNode? stopAt, ref int guard)
        {
            while (node != null && guard++ < 5000)
            {
                if (node == stopAt) return;                       // 循环回边：结束本轮
                if (node.Kind == NgKind.End) return;              // 普通结束
                if (node.Kind == NgKind.Start) { node = Next(doc, node, null); continue; }

                if (node.Kind == NgKind.Decision)
                {
                    bool sat = EvalCondition(Prop(node, "条件"), state);
                    list.Add(LogAction($"[分支] {(sat ? "True" : "False")} : {Prop(node, "条件")}"));
                    node = Next(doc, node, sat ? "True" : "False");
                    continue;
                }
                if (node.Kind == NgKind.Loop)
                {
                    int times = ParseInt(Prop(node, "次数"), 1);
                    var body = Next(doc, node, "Body");
                    var exit = Next(doc, node, "Exit");
                    for (int i = 0; i < Math.Max(0, times); i++)
                        ExecuteFrom(doc, body, state, list, node, ref guard);
                    node = exit;
                    continue;
                }

                list.AddRange(NodeToActions(doc, node, state));
                node = Next(doc, node, null);
            }
        }

        private static NgNode? Next(NgDoc doc, NgNode node, string? port)
        {
            var c = doc.Connections.FirstOrDefault(x => x.SourceId == node.Id && (port == null || x.SourcePort == port));
            if (c == null) return null;
            return doc.Nodes.FirstOrDefault(n => n.Id == c.TargetId);
        }

        /// <summary>把工程当前轴初始位置与变量初值灌入虚拟状态，供分支求值。</summary>
        private static void SeedState(Dictionary<string, double> state)
        {
            var data = ProjectStore.Data;
            if (data?.Axes != null)
                foreach (var ax in data.Axes)
                    state[ax.Name] = AxisRuntimeState.Get(ax.Name);
            if (data?.Variables != null)
                foreach (var r in data.Variables)
                    for (int c = 1; c <= 5; c++)
                    {
                        string nm = c switch { 1 => r.Name1, 2 => r.Name2, 3 => r.Name3, 4 => r.Name4, 5 => r.Name5, _ => "" };
                        string vl = c switch { 1 => r.Value1, 2 => r.Value2, 3 => r.Value3, 4 => r.Value4, 5 => r.Value5, _ => "" };
                        if (!string.IsNullOrWhiteSpace(nm) && double.TryParse(vl, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                            state[nm] = v;
                    }
        }

        private static List<SimAction> NodeToActions(NgDoc doc, NgNode n, Dictionary<string, double> state)
        {
            var list = new List<SimAction>();
            string P(string nm) => Prop(n, nm);
            switch (n.Kind)
            {
                case NgKind.MoveAxis:
                    {
                        string mode = P("模式");
                        double v = ParseDouble(P("目标位置"), 0);
                        double cur = AxisRuntimeState.Get(P("轴"));
                        double tgt = (mode == "相对" || mode == "rel") ? cur + v : v;
                        state[P("轴")] = tgt;                      // 更新虚拟状态，供后续分支判断
                        list.Add(AxisAction(P("轴"), mode == "相对" ? "rel" : "abs", P("目标位置"), ParseDouble(P("速度"), 0)));
                    }
                    break;
                case NgKind.Home:
                    list.Add(AxisAction(P("轴"), "home", "0", 0));
                    break;
                case NgKind.WaitAxis:
                    list.Add(DelayAction(400, $"等待轴 {P("轴")} 到位"));
                    break;
                case NgKind.Cylinder:
                    {
                        int st = (P("动作") == "缩回" || P("动作") == "0") ? 0 : 1;
                        list.Add(CylAction(P("气缸"), st));
                    }
                    break;
                case NgKind.PointGo:
                    list.AddRange(PointActions(P("点位表"), P("点位")));
                    break;
                case NgKind.IoWrite:
                    list.Add(IoAction(P("输出"), ParseInt(P("值"), 1)));
                    break;
                case NgKind.Delay:
                    list.Add(DelayAction(ParseInt(P("时间ms"), 300), "延时"));
                    break;
                case NgKind.WaitInput:
                    list.Add(DelayAction(300, $"等待输入 {P("信号")}"));
                    break;
                case NgKind.CamCapture:
                    list.Add(CamAction(CameraNameOf(P("相机"))));
                    break;
                case NgKind.VarSet:
                    {
                        double v = EvalExpr(P("值"), state);
                        state[P("变量")] = v;
                        list.Add(new SimAction { VarName = P("变量"), VarValue = v, DurationMs = 200, Label = $"变量 {P("变量")} = {v:0.###}" });
                    }
                    break;
                case NgKind.Compute:
                    {
                        double v = EvalExpr(P("表达式"), state);
                        state[P("变量")] = v;
                        list.Add(new SimAction { VarName = P("变量"), VarValue = v, DurationMs = 200, Label = $"运算 {P("变量")} = {v:0.###}" });
                    }
                    break;
                case NgKind.ModbusSend:
                case NgKind.ModbusRecv:
                case NgKind.TcpSend:
                case NgKind.McuWrite:
                    list.Add(LogAction($"[{n.Kind}] {P("指令")}{P("报文")}{P("数据")}"));
                    break;
                case NgKind.TemplateMatch:
                case NgKind.DefectDetect:
                case NgKind.Measure:
                case NgKind.Align:
                case NgKind.Calib:
                    list.Add(LogAction($"[视觉:{n.Kind}] {P("模板")}{P("测量项")}{P("标定板")}"));
                    break;
                default:
                    break;
            }
            return list;
        }

        private static string Prop(NgNode n, string nm)
            => n.Props.FirstOrDefault(p => p.Name == nm)?.Value ?? "";

        // ===================== 表达式求值（用于 设置变量 / 运算 / 条件分支）=====================
        // 支持数字、标识符（轴名/变量名，取自 state）、+ - * / %、一元负号、括号，
        // 以及比较/相等运算符（>, >=, <, <=, ==, !=）用于条件分支，返回 bool。
        private static double EvalExpr(string expr, Dictionary<string, double> state)
        {
            return ExpressionEvaluator.Evaluate(expr, n => state.TryGetValue(n, out var v) ? v : 0, out var r) ? r : 0;
        }

        private static bool EvalCondition(string expr, Dictionary<string, double> state)
        {
            if (string.IsNullOrWhiteSpace(expr)) return false;
            // 找首个比较/相等运算符
            int opLen = 0; string? op = null;
            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];
                if (c == '>' || c == '<' || c == '=' || c == '!')
                {
                    if (i + 1 < expr.Length && expr[i + 1] == '=') { op = expr.Substring(i, 2); opLen = 2; break; }
                    if (c == '=' || c == '!') continue;           // 单 '=' 视为赋值/忽略，'!' 需配 '='
                    op = expr.Substring(i, 1); opLen = 1; break;
                }
            }
            if (op == null)                                       // 无比较符：按数值真值
                return Math.Abs(EvalExpr(expr, state)) > 1e-9;
            string left = expr.Substring(0, expr.IndexOf(op, StringComparison.Ordinal)) .Trim();
            string right = expr.Substring(expr.IndexOf(op, StringComparison.Ordinal) + opLen).Trim();
            double lv = EvalExpr(left, state), rv = EvalExpr(right, state);
            return op switch
            {
                ">" => lv > rv, ">=" => lv >= rv, "<" => lv < rv, "<=" => lv <= rv,
                "==" => Math.Abs(lv - rv) < 1e-9, "!=" => Math.Abs(lv - rv) >= 1e-9,
                _ => false
            };
        }

        /// <summary>极简词法/语法分析（递归下降）已迁移至共享 <see cref="ExpressionEvaluator"/>。</summary>

        // ===================== 动作工厂 =====================
        private static SimAction AxisAction(string axis, string op, string setv, double speed)
        {
            double target;
            double curPos = AxisRuntimeState.Get(axis);
            if (op == "home" || op == "回零" || op == "归零")
            {
                var ax = HardwareResolver.ResolveAxis(axis);
                target = ax != null ? ax.PosLimitMinus : 0;
            }
            else if (op == "相对" || op == "rel" || op == "相对运动")
                target = curPos + ParseDouble(setv, 0);
            else
                target = ParseDouble(setv, curPos);

            double dist = Math.Abs(target - curPos);
            double sp = speed > 0 ? speed : 50;
            int dur = (int)Math.Clamp(dist / sp * 600, 350, 3000);
            if (op == "停止" || op == "stop") dur = 60;
            return new SimAction
            {
                AxisName = axis,
                AxisTarget = target,
                DurationMs = dur,
                Label = $"轴 {axis} → {target:0.##}{(op == "home" || op == "回零" || op == "归零" ? " (回零)" : "")}"
            };
        }

        private static SimAction IoAction(string name, int value)
            => new() { IoName = name, IoValue = value, DurationMs = 250, Label = $"IO {name} = {value}" };

        private static SimAction CylAction(string name, int state)
            => new() { CylName = name, CylState = state, DurationMs = 400, Label = $"气缸 {name} {(state == 1 ? "伸出" : "缩回")}" };

        private static SimAction CamAction(string name)
            => new() { CamName = name, DurationMs = 450, Label = $"相机 {name} 取帧" };

        private static SimAction DelayAction(int ms, string label)
            => new() { DurationMs = Math.Max(120, ms), Label = label };

        private static SimAction LogAction(string label)
            => new() { DurationMs = 250, Label = label };

        private static List<SimAction> PointActions(string tableName, string pointName)
        {
            var list = new List<SimAction>();
            var pt = ProjectStore.Data?.PointTables?.FirstOrDefault(t => string.Equals(t.Name, tableName, StringComparison.OrdinalIgnoreCase));
            if (pt == null) return list;
            var item = string.IsNullOrEmpty(pointName)
                ? pt.Points.FirstOrDefault()
                : pt.Points.FirstOrDefault(p => string.Equals(p.Name, pointName, StringComparison.OrdinalIgnoreCase));
            if (item == null) return list;
            for (int i = 0; i < Math.Min(pt.AxisNames.Count, item.Positions.Count); i++)
            {
                string ax = pt.AxisNames[i];
                if (string.IsNullOrWhiteSpace(ax)) continue;
                double pos = item.Positions[i]?.Position ?? 0;
                double sp = item.Positions[i]?.Speed ?? 0;
                double curPos = AxisRuntimeState.Get(ax);
                double dist = Math.Abs(pos - curPos);
                int dur = (int)Math.Clamp(dist / Math.Max(sp, 50) * 600, 350, 3000);
                list.Add(new SimAction { AxisName = ax, AxisTarget = pos, DurationMs = dur, Label = $"点位 {tableName}/{pointName} · {ax}→{pos:0.##}" });
            }
            return list;
        }

        // ===================== 工具 =====================
        private static string CameraNameOf(string s)
        {
            if (int.TryParse(s, out var idx))
            {
                var cams = ProjectStore.Data?.Cameras;
                if (cams != null && idx >= 0 && idx < cams.Count) return cams[idx].Name;
            }
            var cam = ProjectStore.Data?.Cameras?.FirstOrDefault(c => string.Equals(c.Name, s, StringComparison.OrdinalIgnoreCase));
            return cam?.Name ?? s;
        }

        private static int ParseInt(string s, int def)
            => int.TryParse(s, out var v) ? v : def;
        private static double ParseDouble(string s, double def)
            => double.TryParse(s, out var v) ? v : def;
    }
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣ۤۥ▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣ۤۥ▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣ۤ
