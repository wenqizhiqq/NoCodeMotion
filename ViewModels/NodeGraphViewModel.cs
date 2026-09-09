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
    private readonly NgDoc _doc = new();
    private readonly NgRunner _runner;

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
        }
    }

    private NodeGraphConnectionViewModel? _selectedConn;
    public NodeGraphConnectionViewModel? SelectedConnection
    {
        get => _selectedConn;
        set
        {
            if (_selectedConn == value) return;
            _selectedConn = value;
            if (value != null) SelectedNode = null;
            OnChanged(nameof(SelectedConnection));
            OnChanged(nameof(HasSelection));
        }
    }

    public bool HasSelection => SelectedNode != null || SelectedConnection != null;

    /// <summary>工具箱：按 视觉 / 运控 / 通讯 分组的节点类型列表。</summary>
    public System.Collections.Generic.List<NgToolGroup> ToolboxGroups { get; }

    public ICommand AddNodeCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ClearCommand { get; }

    // ===================== 调试器：状态、6 命令、按钮可用性 =====================

    public NgRunState RunState => _runner.State;
    public string? CurrentNodeId => _runner.CurrentNodeId;
    public string LastError => _runner.LastError;
    public string RunStateText => RunState switch
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
    public bool CanRun => _runner.State is NgRunState.Idle or NgRunState.Completed or NgRunState.Stopped or NgRunState.Error;
    public bool CanStep => _runner.State is NgRunState.Idle or NgRunState.Paused or NgRunState.Completed or NgRunState.Stopped or NgRunState.Error;
    public bool CanResume => _runner.State == NgRunState.Paused;
    public bool CanPause => _runner.State is NgRunState.Running or NgRunState.Stepping;
    public bool CanStop => _runner.State != NgRunState.Idle;

    public ICommand RunCommand { get; }
    public ICommand StepCommand { get; }
    public ICommand ResumeCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ToggleBreakpointCommand { get; }

    public NodeGraphViewModel()
    {
        // 构造 NgRunner：把 name→对象 的解析 + 变量读写桥接给 runner
        _runner = new NgRunner(
            HardwareBridge.Current,
            HardwareResolver.ResolveAxis,
            HardwareResolver.ResolveInput,
            HardwareResolver.ResolveOutput,
            HardwareResolver.ResolveCylinder,
            HardwareResolver.ResolveComm,
            SimRuntime.SetVariable,
            SimRuntime.GetVariable);
        _runner.StateChanged += OnRunnerStateChanged;
        _runner.ReportChanged += OnRunnerReportChanged;

        AddNodeCommand = new RelayCommand(p => AddNode(ParseKind(p), DefaultX(), DefaultY()));
        DeleteCommand = new RelayCommand(_ => DeleteSelected(), _ => HasSelection);
        ClearCommand = new RelayCommand(_ => ClearAll());

        RunCommand = new RelayCommand(_ => _runner.Run(), _ => CanRun);
        StepCommand = new RelayCommand(_ => _runner.Step(), _ => CanStep);
        ResumeCommand = new RelayCommand(_ => _runner.Resume(), _ => CanResume);
        PauseCommand = new RelayCommand(_ => _runner.Pause(), _ => CanPause);
        StopCommand = new RelayCommand(_ => _runner.Stop(), _ => CanStop);
        ToggleBreakpointCommand = new RelayCommand(p => _runner.ToggleBreakpoint(p as string ?? string.Empty));

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
        _runner.Load(doc);
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
        }
        else if (SelectedConnection != null)
        {
            Connections.Remove(SelectedConnection);
            SelectedConnection = null;
            Save();
        }
    }

    public void ClearAll()
    {
        Connections.Clear();
        Nodes.Clear();
        SelectedNode = null;
        SelectedConnection = null;
        Save();
    }

    private static NgKind ParseKind(object? p) =>
        p is NgKind k ? k : NgKind.Start;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ===================== NgRunner 事件回调 =====================

    private void OnRunnerStateChanged()
    {
        // 同步所有节点的断点标记（断点是 runner 全局态）
        foreach (var n in Nodes) n.HasBreakpoint = _runner.HasBreakpoint(n.Id);
        OnChanged(nameof(RunState));
        OnChanged(nameof(RunStateText));
        OnChanged(nameof(CurrentNodeId));
        OnChanged(nameof(LastError));
        OnChanged(nameof(CanRun));
        OnChanged(nameof(CanStep));
        OnChanged(nameof(CanResume));
        OnChanged(nameof(CanPause));
        OnChanged(nameof(CanStop));
    }

    private void OnRunnerReportChanged()
    {
        // 每步执行完同步到节点 VM（StepResult + IsCurrent）
        foreach (var n in Nodes)
        {
            n.StepResult = _runner.Report.Results.TryGetValue(n.Id, out var r) ? r : null;
            n.IsCurrent = n.Id == _runner.CurrentNodeId;
        }
    }
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
