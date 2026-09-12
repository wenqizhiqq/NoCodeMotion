// =====================================================================
// 节点图（NodeGraph）解释器：按 NgDoc 拓扑遍历执行节点，支持 6 按钮调试器。
//
// 状态机 NgRunState: Idle → Running → (Paused|Stepping) → Running → Completed/Stopped/Error
//   - Run    : 从头跑到结束；遇断点/暂停即停；可被 Resume/Stop 唤醒/终止
//   - Step   : 从 Idle 启动时跑一个节点后停；从 Paused 启动时跑一个节点后停
//   - Resume : 从 Paused 继续跑到结束
//   - Pause  : 把 Running 切成 Paused（30ms 轮询门检测）
//   - Stop   : 立即取消运行循环
//   - ToggleBreakpoint(nodeId) : 增删断点；Run 时遇断点自动 Paused
//
// 与 SimFlowPlayer（跑 Table 流程）解耦：NgRunner 直接走 NgDoc 邻接表，
// 不需要"先编译 NgDoc → SimAction"。代价是节点执行 switch 写在 NgRunner 内。
//
// 节点执行：运控/通讯/逻辑/变量 15 个 Kind 调 IHardwareBridge 真逻辑；
// 视觉 6 个（采集/匹配/缺陷/测量/对位/标定）交给 NgVisionExecutor，
// 内部走 VisionEngine（OpenCvSharp + GrayMatch 旋转不变 NCC）真算子，
// 结果摘要写回 NgStepResult.Summary，数值结果写入 SimRuntime 变量。
// =====================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NoCodeMotion.Models;
using NoCodeMotion.Models.NodeGraph;
using NoCodeMotion.Services.Vision;

namespace NoCodeMotion.Services;

public sealed class NgRunner
{
    private readonly IHardwareBridge _bridge;
    private readonly Func<string, AxisItem?> _findAxis;
    private readonly Func<string, IoItem?> _findInput;
    private readonly Func<string, IoItem?> _findOutput;
    private readonly Func<string, CylinderItem?> _findCyl;
    private readonly Func<string, CommItem?> _findComm;
    private readonly Action<string, double> _setVar;
    private readonly Func<string, double> _getVar;

    private readonly Dictionary<string, NgNode> _nodeMap = new();
    private readonly Dictionary<string, List<NgConnection>> _outMap = new();
    private readonly Dictionary<string, int> _loopCounters = new();
    /// <summary>并行汇聚状态：Join 节点 Id → 已到达分支 Id 集合。</summary>
    private readonly Dictionary<string, HashSet<string>> _joinArrivals = new();
    private NgDoc? _doc;

    /// <summary>视觉节点执行器（会话式：跨节点保持当帧与算子链）。</summary>
    private readonly NgVisionExecutor _vision;
    /// <summary>上一个节点的结果摘要（视觉节点回填，写入 NgStepResult.Summary 供卡片显示）。</summary>
    private string _lastNodeSummary = "";
    /// <summary>上一个节点的错误信息（非视觉节点 throw / 视觉节点 Error 字段都落到这里）。</summary>
    private string _lastNodeError = "";

    private CancellationTokenSource? _cts;
    private volatile NgRunState _state = NgRunState.Idle;
    private bool _stepMode;

    public NgRunReport Report { get; } = new();
    public HashSet<string> Breakpoints { get; } = new(StringComparer.Ordinal);
    public NgRunState State => _state;
    public string? CurrentNodeId => Report.CurrentNodeId;
    public string LastError => Report.LastError;

    /// <summary>状态变更（State/Breakpoints 变化时触发，UI 刷新按钮可用性）。</summary>
    public event Action? StateChanged;
    /// <summary>节点结果变更（每步执行完触发，UI 刷新节点卡片状态色/耗时/异常）。</summary>
    public event Action? ReportChanged;

    public NgRunner(
        IHardwareBridge bridge,
        Func<string, AxisItem?> findAxis,
        Func<string, IoItem?> findInput,
        Func<string, IoItem?> findOutput,
        Func<string, CylinderItem?> findCyl,
        Func<string, CommItem?> findComm,
        Action<string, double> setVar,
        Func<string, double> getVar)
    {
        _bridge = bridge;
        _findAxis = findAxis;
        _findInput = findInput;
        _findOutput = findOutput;
        _findCyl = findCyl;
        _findComm = findComm;
        _setVar = setVar;
        _getVar = getVar;
        _vision = new NgVisionExecutor(setVar, msg => { try { bridge?.Log(msg); } catch { } });
    }

    /// <summary>加载/切换 NgDoc 时调用：重建邻接表、清空报告与循环计数、断点保留。</summary>
    public void Load(NgDoc doc)
    {
        _doc = doc;
        _nodeMap.Clear();
        _outMap.Clear();
        _loopCounters.Clear();
        Report.Reset();
        foreach (var n in doc.Nodes) _nodeMap[n.Id] = n;
        foreach (var c in doc.Connections)
        {
            if (!_outMap.TryGetValue(c.SourceId, out var list))
                _outMap[c.SourceId] = list = new List<NgConnection>();
            list.Add(c);
        }
        StateChanged?.Invoke();
        ReportChanged?.Invoke();
    }

    // ===================== 调试器命令 =====================

    public void Run()
    {
        if (_state == NgRunState.Running || _state == NgRunState.Stepping) return;
        if (_doc == null) return;
        _loopCounters.Clear();
        _joinArrivals.Clear();
        Report.Reset();
        _vision.Reset();
        _cts = new CancellationTokenSource();
        _stepMode = false;
        _state = NgRunState.Running;
        StateChanged?.Invoke();
        _ = Task.Run(() => RunAsync(_cts.Token));
    }

    public void Step()
    {
        if (_state == NgRunState.Paused)
        {
            _stepMode = true;
            _state = NgRunState.Running;
            StateChanged?.Invoke();
            return;
        }
        if (_state != NgRunState.Idle && _state != NgRunState.Completed
            && _state != NgRunState.Stopped && _state != NgRunState.Error) return;
        if (_doc == null) return;
        _loopCounters.Clear();
        _joinArrivals.Clear();
        Report.Reset();
        _vision.Reset();
        _cts = new CancellationTokenSource();
        _stepMode = true;
        _state = NgRunState.Running;
        StateChanged?.Invoke();
        _ = Task.Run(() => RunAsync(_cts.Token));
    }

    public void Resume()
    {
        if (_state != NgRunState.Paused) return;
        _stepMode = false;
        _state = NgRunState.Running;
        StateChanged?.Invoke();
    }

    public void Pause()
    {
        if (_state != NgRunState.Running && _state != NgRunState.Stepping) return;
        _state = NgRunState.Paused;
        StateChanged?.Invoke();
    }

    public void Stop()
    {
        if (_state == NgRunState.Idle) return;
        _state = NgRunState.Stopped;
        _cts?.Cancel();
        StateChanged?.Invoke();
    }

    public void ToggleBreakpoint(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return;
        if (!Breakpoints.Add(nodeId)) Breakpoints.Remove(nodeId);
        StateChanged?.Invoke();
    }

    public bool HasBreakpoint(string nodeId) => Breakpoints.Contains(nodeId);

    // ===================== 运行循环 =====================

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            var start = _nodeMap.Values.FirstOrDefault(n => n.Kind == NgKind.Start);
            if (start == null)
            {
                _state = NgRunState.Error;
                Report.LastError = "未找到开始节点";
                StateChanged?.Invoke();
                return;
            }

            // 多游标调度器：每个分支一条游标，逻辑并行、执行单线程交错（便于调试）。
            var queue = new Queue<BranchCursor>();
            queue.Enqueue(new BranchCursor { Current = start });

            while (queue.Count > 0)
            {
                if (ct.IsCancellationRequested) return;

                var branch = queue.Dequeue();
                var current = branch.Current;
                if (current == null) continue;

                // 断点：进入节点前先判断
                if (Breakpoints.Contains(current.Id))
                {
                    _state = NgRunState.Paused;
                    StateChanged?.Invoke();
                    await WaitResumeAsync(ct);
                    if (ct.IsCancellationRequested) return;
                }

                // 执行：记开始时间、写 Status=Running，UI 立即可见"运行中"
                var res = new NgStepResult
                {
                    NodeId = current.Id,
                    Status = NgStepStatus.Running,
                    StartedAt = DateTime.Now,
                };
                Report.Results[current.Id] = res;
                Report.CurrentNodeId = current.Id;
                ReportChanged?.Invoke();

                var sw = Stopwatch.StartNew();
                try
                {
                    await Task.Run(() => ExecuteNodeSync(current), ct);
                    res.Summary = _lastNodeSummary;
                    // 视觉节点的 Error 字段不算 throw —— 只标记节点卡 ErrorText、推日志，
                    // 流程继续走下一个节点，由 Decision/Compute 节点按变量决定走向。
                    if (!string.IsNullOrEmpty(_lastNodeError))
                    {
                        res.Status = NgStepStatus.Error;
                        res.ErrorText = _lastNodeError;
                        Report.LastError = _lastNodeError;
                        ReportChanged?.Invoke();
                        try { _bridge.Log($"[节点 {current.Kind}] {_lastNodeError}"); } catch { }
                    }
                    else if (res.Status == NgStepStatus.Running)
                    {
                        res.Status = NgStepStatus.Done;
                    }
                }
                catch (Exception ex)
                {
                    // 真致命：节点执行器本身崩了（非视觉节点 / 调度异常）
                    res.Status = NgStepStatus.Error;
                    res.ErrorText = ex.Message;
                    Report.LastError = ex.Message;
                    try { _bridge.Log($"[节点 {current.Kind} 异常] {ex.Message}"); } catch { }
                    ReportChanged?.Invoke();
                    // 这里不切 _state=Error，让 Debug UI 红条可见但仍可继续；
                    // 若想「真致命即停」可加一个属性开关，默认走「节点卡显示、不中断」。
                }
                finally
                {
                    sw.Stop();
                    res.DurationMs = sw.ElapsedMilliseconds;
                    res.FinishedAt = DateTime.Now;
                    ReportChanged?.Invoke();
                }

                if (_state == NgRunState.Error) return;

                if (current.Kind == NgKind.End)
                {
                    // 该分支到达结束；继续处理其他分支，等全部完成再 Completed。
                    continue;
                }

                // 单步 或 手动暂停：跑完一个节点后在边界停下，等用户继续 / 恢复
                bool needPause = _stepMode || _state == NgRunState.Paused;
                if (needPause)
                {
                    _state = NgRunState.Paused;
                    StateChanged?.Invoke();
                    await WaitResumeAsync(ct);
                    if (ct.IsCancellationRequested) return;
                }

                // 选下一节点 / 分支
                EnqueueNextNodes(queue, branch, current);
            }

            _state = NgRunState.Completed;
            StateChanged?.Invoke();
        }
        catch (OperationCanceledException)
        {
            _state = NgRunState.Stopped;
            StateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _state = NgRunState.Error;
            Report.LastError = ex.Message;
            StateChanged?.Invoke();
        }
    }

    /// <summary>暂停门：轮询 _state，Resume/Step 把 _state 切回 Running 后自然跳出；
    /// Stop 会取消 ct 以异常形式退出。30ms 粒度对调试无感。</summary>
    private async Task WaitResumeAsync(CancellationToken ct)
    {
        while (_state == NgRunState.Paused && !ct.IsCancellationRequested)
            await Task.Delay(30, ct);
    }

    private bool EvaluateDecision(NgNode node)
    {
        string expr = GetProp(node, "条件", "true");
        try
        {
            if (ExpressionEvaluator.Evaluate(expr, _getVar, out double v)) return v != 0;
            return false;
        }
        catch { return false; }
    }

    /// <summary>循环节点端口选择：首次进入返 Body + 计数-1；计数到 0 返 Exit。</summary>
    private string GetLoopPort(NgNode loop)
    {
        int total = GetIntProp(loop, "次数", 1);
        if (!_loopCounters.TryGetValue(loop.Id, out int rem))
        {
            rem = total - 1;
            _loopCounters[loop.Id] = rem;
            return "Body";
        }
        if (rem > 0)
        {
            _loopCounters[loop.Id] = rem - 1;
            return "Body";
        }
        return "Exit";
    }

    private NgNode? NextNode(string currentId, string? port)
    {
        if (port == null) return null;
        if (_outMap.TryGetValue(currentId, out var list))
        {
            var c = list.FirstOrDefault(x => x.SourcePort == port);
            if (c != null && _nodeMap.TryGetValue(c.TargetId, out var n)) return n;
        }
        return null;
    }

    /// <summary>根据当前节点类型，把后续节点/分支加入调度队列。
    /// ParallelFork 同时扇出 N 条分支；ParallelJoin 等待全部分支到达后再继续。</summary>
    private void EnqueueNextNodes(Queue<BranchCursor> queue, BranchCursor branch, NgNode current)
    {
        if (current.Kind == NgKind.ParallelFork)
        {
            int count = GetIntProp(current, "分支数", 4);
            int emitted = 0;
            if (_outMap.TryGetValue(current.Id, out var list))
            {
                // 按 Branch1..Branch8 顺序取前 count 个已连线的端口
                foreach (var c in list.OrderBy(x => x.SourcePort))
                {
                    if (emitted >= count) break;
                    if (!c.SourcePort.StartsWith("Branch", StringComparison.Ordinal)) continue;
                    if (_nodeMap.TryGetValue(c.TargetId, out var target))
                    {
                        queue.Enqueue(new BranchCursor { Current = target });
                        emitted++;
                    }
                }
            }
            // 若该 Fork 一条线都没连，直接走默认 Out（兼容旧图 / 空 Fork）
            if (emitted == 0)
            {
                var next = NextNode(current.Id, "Out");
                if (next != null) queue.Enqueue(new BranchCursor { Current = next });
            }
        }
        else if (current.Kind == NgKind.ParallelJoin)
        {
            if (!_joinArrivals.TryGetValue(current.Id, out var set))
                _joinArrivals[current.Id] = set = new HashSet<string>(StringComparer.Ordinal);
            set.Add(branch.Id);

            int expected = GetIntProp(current, "分支数", 4);
            if (set.Count >= expected)
            {
                _joinArrivals.Remove(current.Id);
                var next = NextNode(current.Id, "Out");
                if (next != null) queue.Enqueue(new BranchCursor { Current = next });
            }
        }
        else
        {
            string? port = current.Kind switch
            {
                NgKind.Decision => EvaluateDecision(current) ? "True" : "False",
                NgKind.Loop => GetLoopPort(current),
                _ => "Out",
            };
            var next = NextNode(current.Id, port);
            if (next != null) queue.Enqueue(new BranchCursor { Current = next });
        }
    }

    /// <summary>并行执行上下文：每个分支一条游标，调度器按队列顺序逐个执行节点。
    /// 逻辑上多条分支并发，执行层面单线程交错（便于调试、断点、暂停）。</summary>
    private sealed class BranchCursor
    {
        public string Id { get; } = System.Guid.NewGuid().ToString();
        public NgNode? Current { get; set; }
    }

    // ===================== 节点执行（按 Kind switch） =====================

    private void ExecuteNodeSync(NgNode node)
    {
        _lastNodeSummary = "";
        _lastNodeError = "";
        switch (node.Kind)
        {
            case NgKind.Start:
            case NgKind.End:
            case NgKind.ParallelFork:
            case NgKind.ParallelJoin:
                // 路由逻辑在 EnqueueNextNodes 中处理；节点本身无业务动作。
                break;

            case NgKind.Delay: {
                int ms = GetIntProp(node, "时间ms", 500);
                // Delay 阻塞即可（Task.Run 包裹，不卡 UI 线程）
                Thread.Sleep(ms);
                break;
            }

            case NgKind.MoveAxis: {
                var ax = _findAxis(GetProp(node, "轴", "X"));
                if (ax == null) throw new InvalidOperationException($"未找到轴：{GetProp(node, "轴", "")}");
                string mode = GetProp(node, "模式", "绝对");
                double pos = GetDoubleProp(node, "目标位置", 0);
                double spd = GetDoubleProp(node, "速度", 10);
                _bridge.SetAxisSpeed(ax, spd);
                if (mode == "相对") _bridge.MoveAxisRel(ax, pos);
                else _bridge.MoveAxisAbs(ax, pos);
                break;
            }

            case NgKind.Home: {
                var ax = _findAxis(GetProp(node, "轴", "X"));
                if (ax == null) throw new InvalidOperationException($"未找到轴：{GetProp(node, "轴", "")}");
                _bridge.HomeAxis(ax);
                break;
            }

            case NgKind.WaitAxis: {
                var ax = _findAxis(GetProp(node, "轴", "X"));
                if (ax == null) throw new InvalidOperationException($"未找到轴：{GetProp(node, "轴", "")}");
                _bridge.WaitAxisDone(ax);
                break;
            }

            case NgKind.Cylinder: {
                var cyl = _findCyl(GetProp(node, "气缸", ""));
                if (cyl == null) throw new InvalidOperationException($"未找到气缸：{GetProp(node, "气缸", "")}");
                string act = GetProp(node, "动作", "伸出");
                _bridge.CylinderMove(cyl, act == "伸出" ? 1 : 0);
                break;
            }

            case NgKind.PointGo: {
                var pt = ProjectStore.Data?.Points?.FirstOrDefault(p => p.Name == GetProp(node, "点位", ""));
                if (pt == null) throw new InvalidOperationException($"未找到点位：{GetProp(node, "点位", "")}");
                // 简化：把点位第 1 轴槽位置走 X 轴（点位表 4 槽位，多轴完整联动留待扩展）
                var ax = _findAxis("X");
                if (ax != null && pt.Positions.Count > 0) { _bridge.MoveAxisAbs(ax, pt.Positions[0].Position); _bridge.WaitAxisDone(ax); }
                break;
            }

            case NgKind.IoWrite: {
                var io = _findOutput(GetProp(node, "输出", ""));
                if (io == null) throw new InvalidOperationException($"未找到输出：{GetProp(node, "输出", "")}");
                int v = GetIntProp(node, "值", 1);
                _bridge.WriteOutput(io, v);
                break;
            }

            case NgKind.WaitInput: {
                var io = _findInput(GetProp(node, "信号", ""));
                if (io == null) throw new InvalidOperationException($"未找到输入：{GetProp(node, "信号", "")}");
                string st = GetProp(node, "状态", "高电平");
                _bridge.WaitInput(io, st == "高电平" ? 1 : 0);
                break;
            }

            case NgKind.VarSet: {
                string name = GetProp(node, "变量", "");
                string v = GetProp(node, "值", "0");
                if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                    _setVar(name, d);
                else _setVar(name, 0);
                break;
            }

            case NgKind.Compute: {
                string name = GetProp(node, "变量", "");
                string expr = GetProp(node, "表达式", "0");
                if (ExpressionEvaluator.Evaluate(expr, _getVar, out double r))
                    _setVar(name, r);
                else
                    throw new InvalidOperationException($"表达式无法求值：{expr}");
                break;
            }

            case NgKind.ModbusSend: {
                var c = _findComm(GetProp(node, "通讯", ""));
                if (c == null) throw new InvalidOperationException($"未找到通讯：{GetProp(node, "通讯", "")}");
                _bridge.CommSend(c, GetProp(node, "指令", ""));
                break;
            }

            case NgKind.ModbusRecv: {
                var c = _findComm(GetProp(node, "通讯", ""));
                if (c == null) throw new InvalidOperationException($"未找到通讯：{GetProp(node, "通讯", "")}");
                _bridge.CommRecv(c);
                break;
            }

            case NgKind.TcpSend: {
                var c = _findComm(GetProp(node, "端点", ""));
                if (c == null) throw new InvalidOperationException($"未找到通讯：{GetProp(node, "端点", "")}");
                _bridge.CommSend(c, GetProp(node, "报文", ""));
                break;
            }

            case NgKind.McuWrite: {
                _bridge.Log($"[下位机写] 设备={GetProp(node, "设备", "")} 数据={GetProp(node, "数据", "")}");
                break;
            }

            // —— 视觉节点：交给会话式视觉执行器（VisionEngine + GrayMatch 真算子） ——
            // 采集节点把当帧落盘，后续 匹配/缺陷/测量/对位/标定 复用同一帧；
            // 结果摘要回填卡片，数值结果写入变量供 条件分支 / 运算 节点引用。
            // 异常一律降级到 _lastNodeError，不抛 —— 节点卡 + 日志显示 + 流程继续。
            case NgKind.CamCapture:
            case NgKind.TemplateMatch:
            case NgKind.DefectDetect:
            case NgKind.Measure:
            case NgKind.Align:
            case NgKind.Calib:
                {
                    var oc = _vision.Execute(node);
                    _lastNodeSummary = oc.Summary;
                    _lastNodeError = oc.Error ?? "";
                }
                break;
        }
    }

    // ===================== 节点属性辅助 =====================

    private static string GetProp(NgNode n, string name, string def)
    {
        var p = n.Props.FirstOrDefault(x => x.Name == name);
        return p == null || string.IsNullOrEmpty(p.Value) ? def : p.Value;
    }

    private static int GetIntProp(NgNode n, string name, int def)
    {
        var s = GetProp(n, name, def.ToString(CultureInfo.InvariantCulture));
        return int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : def;
    }

    private static double GetDoubleProp(NgNode n, string name, double def)
    {
        var s = GetProp(n, name, def.ToString(CultureInfo.InvariantCulture));
        return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : def;
    }
}
