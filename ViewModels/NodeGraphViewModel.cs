// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
// ◆温​启‌志‍◆⁠编‍写‏◇‌微‎信‍﹕⁠1‍8‍7​◆‏1‎9⁣3‏6‎◇⁣1‍3‌9‎9⁣　‌※‌保⁣留⁠所‏有​权‏利​请⁠勿​删‍除‎◇
// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Models.NodeGraph;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels;

/// <summary>工具箱分组（供 XAML 分组渲染）。</summary>
public sealed class NgToolGroup
{
    public NgDomain Domain { get; set; }
    public string Title { get; set; } = "";
    public System.Collections.Generic.List<NgNodeDef> Items { get; set; } = new();
}

/// <summary>节点图编辑器主 ViewModel（INPC）。
/// 持有当前流程的节点 / 连线集合，负责增删节点、连线、选中、属性编辑与自动保存。
/// 所有渲染由 XAML 的 ItemsControl + DataTemplate 完成，VM 不直接操作任何视觉元素。</summary>
public sealed class NodeGraphViewModel : INotifyPropertyChanged
{
    private FlowItem? _flowItem;
    /// <summary>上一拍的运行状态文字：外部（操作员）驱动的循环运行要让本页状态跟着变，仅在变化时触发 INPC。</summary>
    private string _lastRunStateText = "";
    /// <summary>本页 runner 是否正在跑（运行一次/单步/继续）：结束时据此安全回落共享态状态，不误伤外部循环的状态。</summary>
    private bool _ownRunActive;
    private readonly NgDoc _doc = new();
    private readonly NgRunner _runner;
    private readonly System.Windows.Threading.DispatcherTimer _actualPosTimer;

    // 「循环运行」已托管到 FlowLoopManager（静态、页面无关）：本页只负责入口调用 + 200ms 轮询显示。
    public ObservableCollection<NodeGraphNodeViewModel> Nodes { get; } = new();
    public ObservableCollection<NodeGraphConnectionViewModel> Connections { get; } = new();

    private NodeGraphNodeViewModel? _selectedNode;
    public NodeGraphNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (_selectedNode == value) return;
            if (_selectedNode != null) _selectedNode.IsSelected = false;
            _selectedNode = value;
            if (_selectedNode != null) _selectedNode.IsSelected = true;
            if (value != null) SelectedConnection = null;
            OnChanged(nameof(SelectedNode));
            OnChanged(nameof(HasSelection));
            OnChanged(nameof(ShowLiveRow));
            OnChanged(nameof(SelectedHint));
            value?.RefreshPanelState();          // 选中即刷新一次「实时值 / 条件判定」
        }
    }

    private NodeGraphConnectionViewModel? _selectedConn;
    public NodeGraphConnectionViewModel? SelectedConnection
    {
        get => _selectedConn;
        set
        {
            if (_selectedConn == value) return;
            if (_selectedConn != null) _selectedConn.IsSelected = false;   // 取消上一条连线的选中高亮
            _selectedConn = value;
            if (_selectedConn != null) _selectedConn.IsSelected = true;    // 选中的连线变红加粗
            if (value != null) SelectedNode = null;
            OnChanged(nameof(SelectedConnection));
            OnChanged(nameof(HasSelection));
            OnChanged(nameof(SelectedHint));
        }
    }

    public bool HasSelection => SelectedNode != null || SelectedConnection != null;

    /// <summary>选中项提示（面板顶部显示选中了哪条连线 / 哪个节点，并说明删除方式）。</summary>
    public string SelectedHint => SelectedConnection != null
        ? $"已选中连线：{SelectedConnection.Describe}　（点「删除所选」或按 Delete 键删除）"
        : SelectedNode != null
            ? $"已选中节点：{SelectedNode.Title}　（点「删除所选」或按 Delete 键删除）"
            : string.Empty;

    /// <summary>属性面板是否显示「实时值」行（选中 轴类 / 设置变量 / 运算 节点时）。null 安全：未选中时为 false。</summary>
    public bool ShowLiveRow => SelectedNode?.ShowsLiveRow == true;

    /// <summary>工具箱：按 视觉 / 运控 / 通讯 分组的节点类型列表。</summary>
    public System.Collections.Generic.List<NgToolGroup> ToolboxGroups { get; }

    public ICommand AddNodeCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand DeleteAllConnectionsCommand { get; }
    public ICommand ClearCommand { get; }

    // ===================== 调试器：状态、6 命令、按钮可用性 =====================

    public NgRunState RunState => _runner.State;
    public string? CurrentNodeId => _runner.CurrentNodeId;
    public string LastError => _runner.LastError;
    public string RunStateText
    {
        get
        {
            // ★ 外部运行（操作员页「启动」/ 流程页「循环运行」在后台跑本条流程）时，本页自己的 runner 是闲的。
            //   读共享态 FlowRunStore 把真实状态显示出来——否则页面会一直显示「未运行」与实际不符。
            if (_flowItem != null
                && _runner.State is NgRunState.Idle or NgRunState.Completed or NgRunState.Stopped or NgRunState.Error
                && FlowRunStore.Contains(_flowItem))
            {
                var (est, _, cycle) = FlowRunStore.Get(_flowItem);
                switch (est)
                {
                    case FlowStatus.Looping: return $"循环运行中（操作员/页面启动）　·　已循环 {cycle} 轮";
                    case FlowStatus.Running: return "运行中（外部启动）";
                    case FlowStatus.Paused: return "已暂停（操作员）";
                }
            }
            string baseText = RunState switch
            {
                NgRunState.Idle => "未运行",
                NgRunState.Running => "运行中…",
                NgRunState.Paused => "已暂停（断点或单步）",
                NgRunState.Stepping => "单步中…",
                NgRunState.Completed => "已完成",
                NgRunState.Error => "异常停止",
                NgRunState.Stopped => "已停止",
                _ => string.Empty,
            };
            return baseText;
        }
    }

    /// <summary>当前是否处于「循环运行」模式（该流程正被 FlowLoopManager 循环运行）。</summary>
    public bool IsLoopRunning => FlowLoopManager.IsLooping(_flowItem);
    public bool CanRun => _runner.State is NgRunState.Idle or NgRunState.Completed or NgRunState.Stopped or NgRunState.Error
        && !FlowLoopManager.IsLooping(_flowItem);
    public bool CanStep => _runner.State is NgRunState.Idle or NgRunState.Paused or NgRunState.Completed or NgRunState.Stopped or NgRunState.Error
        && !FlowLoopManager.IsLooping(_flowItem);

    /// <summary>「运行一次 / 循环运行」按钮可用：runner 可启动且当前不在循环运行中（循环中用「停止」退出）。</summary>
    public bool CanStartRun => CanRun;
    /// <summary>「单步」按钮可用：允许单步的状态。</summary>
    public bool CanStartStep => CanStep;
    public bool CanResume => _runner.State == NgRunState.Paused;
    public bool CanPause => _runner.State is NgRunState.Running or NgRunState.Stepping;
    /// <summary>「停止」可用：本页 runner 在跑，或该流程正被静态管理器循环运行（操作员启动/页面启动）。</summary>
    public bool CanStop => _runner.State != NgRunState.Idle || FlowLoopManager.IsLooping(_flowItem);

    /// <summary>「运行一次」：从开始节点连续执行整个流程一遍。</summary>
    public ICommand RunCommand { get; }
    /// <summary>「循环运行」：反复执行整个流程，直到点「停止」。</summary>
    public ICommand LoopRunCommand { get; }
    public ICommand StepCommand { get; }
    public ICommand ResumeCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ToggleBreakpointCommand { get; }

    public NodeGraphViewModel()
    {
        // 构造 NgRunner：把 name→对象 的解析 + 变量读写桥接给 runner。
        // ★ 读必须走 GetVariableResolved（工程变量表优先，与变量页/流程页同一数据源）：
        //   原来读 SimRuntime 内存仓（不认识工程表里的当前值），导致「计算+1」第一次
        //   就以 0 为基数把工程表覆盖成 1——"真实加减乘除"全是错的。
        //   写走 SetVariable（内存仓 + 工程表双写，变量页 INPC 即时刷新）。
        _runner = new NgRunner(
            HardwareResolver.ResolveAxis,
            HardwareResolver.ResolveInput,
            HardwareResolver.ResolveOutput,
            HardwareResolver.ResolveCylinder,
            HardwareResolver.ResolveComm,
            SimRuntime.SetVariable,
            SimRuntime.GetVariableResolved);
        _runner.StateChanged += OnOwnRunnerProgress;
        _runner.ReportChanged += OnOwnRunnerProgress;

        // 属性面板「实际位置」等回显：200ms 定时器轮询（运行时随轴运动实时变化）；只刷新当前选中节点，代价极小。
        _actualPosTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = System.TimeSpan.FromMilliseconds(200)
        };
        _actualPosTimer.Tick += (_, _) =>
        {
            // 条件分支：所有节点的端口 / 分组实时变色（卡片上直接看到 符合 / 不符合）
            foreach (var n in Nodes) n.RefreshDecisionState();
            SelectedNode?.RefreshPanelState();
            // ★ 回显全部走定时器轮询共享态（运行器只写共享态、不往页面推）：
            //   刷"当前节点高亮 + 各节点结果"，无论本页启动还是操作员启动，节点都正常依次跳转/变色。
            RefreshRunFromStore();
            FlowRunStore.PushStatuses();   // 共享态 → 左侧流程列表状态芯片（循环 / 运行）
            // 状态文字（循环运行中（操作员启动）/ 运行中（外部启动））跟着刷新
            string text = RunStateText;
            if (text != _lastRunStateText) { _lastRunStateText = text; OnChanged(nameof(RunStateText)); }
            // 轮询会改变 CanRun/CanStop 等（外部循环运行时：运行/循环禁用、停止可用）→ 主动刷新命令状态
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        };
        _actualPosTimer.Start();

        AddNodeCommand = new RelayCommand(p => AddNode(ParseKind(p), DefaultX(), DefaultY()));
        DeleteCommand = new RelayCommand(_ => DeleteSelected(), _ => HasSelection);
        DeleteAllConnectionsCommand = new RelayCommand(_ => DeleteAllConnections(), _ => Connections.Count > 0);
        ClearCommand = new RelayCommand(_ => ClearAll());

        RunCommand = new RelayCommand(_ => { _runner.Run(); }, _ => CanRun);
        // ★ 循环运行 → 托管到 FlowLoopManager（静态、页面无关）：切页/卸载不影响运行；
        //   节点高亮由 200ms 定时器轮询共享态完成。
        LoopRunCommand = new RelayCommand(_ => FlowLoopManager.StartLoop(_flowItem), _ => CanStartRun && _flowItem != null);
        StepCommand = new RelayCommand(_ => _runner.Step(), _ => CanStep);
        ResumeCommand = new RelayCommand(_ => _runner.Resume(), _ => CanResume);
        PauseCommand = new RelayCommand(_ => _runner.Pause(), _ => CanPause);
        // 停止：两项都做、任何状态都兜底 —— ① 该流程正被静态管理器循环运行（操作员启动/页面启动）→ 停那条循环；
        // ② 本页自己的 runner 在跑 → 停本页运行（Idle 时 Stop 内部直接返回，幂等安全）。
        StopCommand = new RelayCommand(_ =>
        {
            FlowLoopManager.StopLoop(_flowItem);
            StopRun();
            OnChanged(nameof(RunStateText));
        }, _ => CanStop);

        // 「循环运行」已托管到 FlowLoopManager（静态）——本页不再有自己的循环重启定时器
        // 断点切换：p = 节点 Id（画布点击角标 / 选中后点「断点」按钮两种入口）。
        // 切换后立即写入共享态 FlowRunStore —— 后台循环运行的新建 NgRunner 从共享态取断点，
        // 否则断点只在页面自己的「运行一次」里生效（循环运行感知不到）。
        ToggleBreakpointCommand = new RelayCommand(p =>
        {
            if (p is string id && !string.IsNullOrEmpty(id)) ToggleBreakpointAt(id);
        });

        ToolboxGroups = NgNodeDefinitions.DomainOrder.Select(dom => new NgToolGroup
        {
            Domain = dom,
            Title = NgNodeDefinitions.DomainTitle[dom],
            Items = NgNodeDefinitions.All.Values.Where(d => d.Domain == dom).ToList()
        }).ToList();
    }

    // ============ 加载 / 保存 ============

    /// <summary>从选中的流程项加载节点图（解析 GraphJson）。切换流程时调用。</summary>
    public void LoadFrom(FlowItem item)
    {
        _flowItem = item;
        var doc = NgDoc.FromJson(item.GraphJson);
        BuildViewModels(doc);
        // 断点恢复：共享态里记过断点（页面重建/重进后）→ 应用到本页 runner；再回写共享态统一来源
        var stored = FlowRunStore.GetBreakpoints(item);
        if (stored != null) _runner.SetBreakpoints(stored);
        FlowRunStore.SetBreakpoints(item, _runner.Breakpoints);
        _runner.Load(doc);
        // 切到正在跑的流程：立即按共享态刷一次"当前节点/结果/状态文字"（此后 200ms 定时器接管）
        RefreshRunFromStore();
        OnChanged(nameof(RunStateText));
    }

    /// <summary>按节点 Id 切换断点（画布点击断点角标 / 工具栏「断点」按钮共用）。
    /// 同步：本页 runner 断点集合 → 共享态 FlowRunStore → 立即刷该节点角标（不等 200ms 定时器）。</summary>
    public void ToggleBreakpointAt(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return;
        _runner.ToggleBreakpoint(nodeId);
        FlowRunStore.SetBreakpoints(_flowItem, _runner.Breakpoints);
        var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node != null) node.HasBreakpoint = _runner.HasBreakpoint(nodeId);
    }

    private void BuildViewModels(NgDoc doc)
    {
        Nodes.Clear();
        Connections.Clear();
        var map = new System.Collections.Generic.Dictionary<string, NodeGraphNodeViewModel>();
        foreach (var n in doc.Nodes)
        {
            var vm = new NodeGraphNodeViewModel(n, Save);
            Nodes.Add(vm);
            map[n.Id] = vm;
        }
        foreach (var c in doc.Connections)
        {
            if (map.TryGetValue(c.SourceId, out var s) && map.TryGetValue(c.TargetId, out var t))
                Connections.Add(new NodeGraphConnectionViewModel(c, s, t));
        }
    }

    /// <summary>把当前视图状态序列化回 GraphJson 并触发工程自动保存。</summary>
    public void Save()
    {
        if (_flowItem == null) return;
        _doc.Nodes = Nodes.Select(n => n.Model).ToList();
        _doc.Connections = Connections.Select(c => c.Model).ToList();
        _flowItem.GraphJson = _doc.ToJson();
        ProjectStore.ScheduleSave();
    }

    // ============ 节点 / 连线编辑 ============

    private double DefaultX() => 80 + (Nodes.Count * 26) % 360;
    private double DefaultY() => 80 + (Nodes.Count * 26) % 240;

    public void AddNode(NgKind kind, double x, double y)
    {
        var def = NgNodeDefinitions.All[kind];
        var node = new NgNode { Kind = kind, X = x, Y = y };
        foreach (var pd in def.Props)
            node.Props.Add(new NgProp { Name = pd.Name, Value = pd.Default, Options = pd.Options });
        var vm = new NodeGraphNodeViewModel(node, Save);
        Nodes.Add(vm);
        SelectedNode = vm;
        Save();
        SyncRunnerTopology();
    }

    /// <summary>在 src 节点的 port 输出端口与 tgt 节点输入端口之间建立连线。</summary>
    public void Connect(string srcId, string port, string tgtId)
    {
        if (srcId == tgtId) return;
        var s = Nodes.FirstOrDefault(n => n.Id == srcId);
        var t = Nodes.FirstOrDefault(n => n.Id == tgtId);
        if (s == null || t == null || !t.HasInput) return;
        if (Connections.Any(c => c.SourceId == srcId && c.SourcePort == port && c.TargetId == tgtId)) return;
        var conn = new NgConnection { SourceId = srcId, SourcePort = port, TargetId = tgtId };
        Connections.Add(new NodeGraphConnectionViewModel(conn, s, t));
        Save();
        SyncRunnerTopology();
    }

    public void DeleteSelected()
    {
        if (SelectedNode != null)
        {
            var id = SelectedNode.Id;
            foreach (var c in Connections.Where(c => c.SourceId == id || c.TargetId == id).ToList())
                Connections.Remove(c);
            Nodes.Remove(SelectedNode);
            SelectedNode = null;
            Save();
            SyncRunnerTopology();
        }
        else if (SelectedConnection != null)
        {
            var conn = SelectedConnection;
            SelectedConnection = null;      // 先清选中（同时清掉它的红色高亮）
            Connections.Remove(conn);
            Save();
            SyncRunnerTopology();
        }
    }

    /// <summary>删除指定连线（供画布右键等直接调用）。</summary>
    public void DeleteConnection(NodeGraphConnectionViewModel conn)
    {
        if (conn == null || !Connections.Contains(conn)) return;
        if (ReferenceEquals(SelectedConnection, conn)) SelectedConnection = null;
        Connections.Remove(conn);
        Save();
        SyncRunnerTopology();
    }

    /// <summary>删除画布上所有连线（保留节点）。</summary>
    public void DeleteAllConnections()
    {
        if (Connections.Count == 0) return;
        SelectedConnection = null;
        Connections.Clear();
        Save();
        SyncRunnerTopology();
    }

    /// <summary>拓扑变更（增删节点 / 增删连线 / 清空）后把最新图同步给运行器。
    /// 否则运行器仍按 Load 时的旧连线路由 —— 删掉的连线在运行时还会继续走、新增的走不到。</summary>
    private void SyncRunnerTopology()
    {
        _doc.Nodes = Nodes.Select(n => n.Model).ToList();
        _doc.Connections = Connections.Select(c => c.Model).ToList();
        _runner.Load(_doc);
    }

    public void ClearAll()
    {
        Connections.Clear();
        Nodes.Clear();
        SelectedNode = null;
        SelectedConnection = null;
        Save();
        SyncRunnerTopology();
    }

    private static NgKind ParseKind(object? p) =>
        p is NgKind k ? k : NgKind.Start;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ===================== 本页运行：只写共享态（回显由 200ms 定时器轮询完成） =====================

    /// <summary>本页 runner 的状态/报告回调：**只把进度写进共享态 FlowRunStore**（线程安全），
    /// 绝不直接改 Nodes 集合 —— 节点卡片的高亮/变色由 200ms 定时器轮询共享态完成。
    /// 这样"运行中切流程/切页面"（LoadFrom 重建 Nodes 集合）也不会与运行器并发冲突。</summary>
    private void OnOwnRunnerProgress()
    {
        if (_flowItem == null) return;
        try
        {
            FlowRunStore.SetProgress(_flowItem, nodeId: _runner.CurrentNodeId ?? "");
            FlowRunStore.SetNodeResults(_flowItem, _runner.Report.Results);
            FlowRunStore.SetTriggeredBreakpoint(_flowItem, _runner.Report.TriggeredBreakpointId ?? "");
            // ★ 本页 runner 的状态也要写进共享态：单步/运行一次的"当前节点高亮"
            //   依赖 RefreshRunFromStore 里的状态门槛（Running/Looping/Paused 才点亮 IsCurrent），
            //   只写 nodeId 不写 Status → 共享态默认 Idle → 单步永远不显示"运行到此节点"。
            //   结束时安全回落：外部（操作员/FlowLoopManager）循环在跑时绝不覆盖它的状态。
            var st = _runner.State;
            if (st is NgRunState.Running or NgRunState.Stepping)
            {
                _ownRunActive = true;
                FlowRunStore.SetStatus(_flowItem, FlowStatus.Running);
            }
            else if (st == NgRunState.Paused)
            {
                if (_ownRunActive) FlowRunStore.SetStatus(_flowItem, FlowStatus.Paused);
            }
            else if (_ownRunActive)
            {
                // Completed / Stopped / Error：只有确实是"本页在跑"才回落状态
                _ownRunActive = false;
                if (!FlowLoopManager.IsLooping(_flowItem))
                    FlowRunStore.SetStatus(_flowItem, st == NgRunState.Completed ? FlowStatus.Idle : FlowStatus.Stopped);
            }
        }
        catch { /* 展示用途，失败不影响运行 */ }
        OnChanged(nameof(RunState));
        OnChanged(nameof(RunStateText));
        OnChanged(nameof(CurrentNodeId));
        OnChanged(nameof(LastError));
        OnChanged(nameof(CanRun));
        OnChanged(nameof(CanStep));
        OnChanged(nameof(CanStartRun));
        OnChanged(nameof(CanStartStep));
        OnChanged(nameof(CanResume));
        OnChanged(nameof(CanPause));
        OnChanged(nameof(CanStop));
    }

    /// <summary>「停止」：中止本页 runner（循环运行由 FlowLoopManager 负责，见 StopCommand 路由）。</summary>
    private void StopRun()
    {
        _runner.Stop();
        OnChanged(nameof(CanStartRun));
        OnChanged(nameof(CanStartStep));
        OnChanged(nameof(RunStateText));
        OnChanged(nameof(IsLoopRunning));
    }

    // ===================== 回显：定时器轮询共享态（本页运行 / 操作员运行统一） =====================

    /// <summary>由 200ms 定时器调用：读 <see cref="FlowRunStore"/> 把「当前节点 → IsCurrent 高亮」
    /// 「每节点结果 → 卡片状态色/耗时/摘要」「断点标记」套到节点 VM 上，并清掉不再运行的状态。
    /// ★ 全程 UI 线程、只读共享态 —— 运行器不参与，切页面/切流程都不会打断运行。</summary>
    private void RefreshRunFromStore()
    {
        if (_flowItem == null || !FlowRunStore.Contains(_flowItem)) return;
        var (st, _, _) = FlowRunStore.Get(_flowItem);
        bool running = st is FlowStatus.Running or FlowStatus.Looping or FlowStatus.Paused;
        var (_, _, nodeId) = FlowRunStore.GetProgress(_flowItem);
        var results = FlowRunStore.GetNodeResults(_flowItem);
        string trigBp = FlowRunStore.GetTriggeredBreakpoint(_flowItem);
        foreach (var n in Nodes)
        {
            n.HasBreakpoint = _runner.HasBreakpoint(n.Id);
            // 触发断点：运行中且共享态记录的触发节点 = 本节点 → 右上角「触发断点」
            bool wantTrig = running && !string.IsNullOrEmpty(trigBp) && n.Id == trigBp;
            if (n.BreakpointTriggered != wantTrig) n.BreakpointTriggered = wantTrig;
            bool want = running && !string.IsNullOrEmpty(nodeId) && n.Id == nodeId;
            if (n.IsCurrent != want) n.IsCurrent = want;
            if (results != null && results.TryGetValue(n.Id, out var r)) n.StepResult = r;
        }
    }
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
