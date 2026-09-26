// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
// ◆温‎启‏志‎◆​编‌写⁠◇‌微​信​﹕⁣1​8‎7⁣◆‌1​9​3‍6‎◇‎1⁠3⁠9⁠9‍　‏※⁣保​留​所‎有‌权⁣利‌请​勿​删⁠除​◇
// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using NoCodeMotion.Models.NodeGraph;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels;

/// <summary>节点属性项 ViewModel（INPC）：双向绑定到属性面板；值变更回调触发自动保存。</summary>
public sealed class NgPropViewModel : INotifyPropertyChanged
{
    private readonly NgProp _model;
    private readonly NgNode? _owner;
    private readonly System.Action? _onChanged;
    private static readonly System.Text.RegularExpressions.Regex CondProp =
        new(@"^条件(\d)(类型|名称|比较|值)$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public string Name => _model.Name;
    public bool HasOptions => !string.IsNullOrEmpty(_model.Options);

    private bool TryBranchIndex(out int index)
    {
        var m = CondProp.Match(Name);
        index = m.Success ? int.Parse(m.Groups[1].Value) : 0;
        return m.Success;
    }

    private string OwnerProp(string name)
        => _owner?.Props.FirstOrDefault(p => p.Name == name)?.Value ?? string.Empty;

    /// <summary>「条件N名称」的候选随「条件N类型」变：轴位置 / 轴速度 → 轴名，输入IO → 输入点，输出IO → 输出点，变量 → 变量名。</summary>
    private System.Collections.IEnumerable? BranchNameOptions(int branch) => OwnerProp($"条件{branch}类型") switch
    {
        "轴位置" or "轴速度" => Catalog.AxisNames,
        "输入IO" => Catalog.InIoNames,
        "输出IO" => Catalog.OutIoNames,
        "变量" => Catalog.VariableNames,
        _ => null
    };

    /// <summary>属性名 → 名称库下拉候选（轴 / 变量 / 输出 / 信号 / 气缸 / 通讯 / 点位 / 条件N名称）；无则 null（用自由文本）。</summary>
    public System.Collections.IEnumerable? CatalogOptions
    {
        get
        {
            if (TryBranchIndex(out int bi) && Name.EndsWith("名称", System.StringComparison.Ordinal))
                return BranchNameOptions(bi);
            return Name switch
            {
                "轴" => Catalog.AxisNames,
                "变量" => Catalog.VariableNames,
                "输出" => Catalog.OutIoNames,
                "信号" => Catalog.InIoNames,
                "气缸" => Catalog.CylinderNames,
                "通讯" => Catalog.CommNames,
                "点位" => Catalog.PointNames,
                _ => null
            };
        }
    }
    /// <summary>该属性是否用「名称库」下拉（候选来自工程里已配置的对象）。</summary>
    public bool UsesCatalog => CatalogOptions != null;
    /// <summary>既非固定候选、也非名称库下拉 → 用自由文本输入框。</summary>
    public bool IsPlainText => !HasOptions && !UsesCatalog;
    public System.Collections.Generic.List<string> OptionsList
        => _model.Options?.Split('|')?.ToList() ?? new System.Collections.Generic.List<string>();

    /// <summary>节点卡上是否显示该属性行（条件分支的「条件N*」组只在右侧面板分组显示，卡片上不铺开）。</summary>
    public bool ShowOnCard => !CondProp.IsMatch(Name);

    /// <summary>条件分支里，超出「分支数」的组隐藏（属性面板只显示当前启用的几条分支）。</summary>
    public bool IsVisible
    {
        get
        {
            if (!TryBranchIndex(out int bi)) return true;
            int count = 2;
            if (int.TryParse(OwnerProp("分支数"), out int c))
                count = System.Math.Clamp(c, 1, NgConditionEvaluator.MaxBranches);
            return bi <= count;
        }
    }

    public string Value
    {
        get => _model.Value;
        set { if (_model.Value != value) { _model.Value = value; OnChanged(); _onChanged?.Invoke(); } }
    }

    public NgPropViewModel(NgProp model, System.Action? onChanged = null, NgNode? owner = null)
    {
        _model = model;
        _onChanged = onChanged;
        _owner = owner;
    }

    /// <summary>兄弟属性变化后刷新依赖项：「条件N类型」变 → 「条件N名称」候选变；「分支数」变 → 各行显隐变。</summary>
    public void NotifyDependentChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CatalogOptions)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
}

/// <summary>节点卡上的一个输出端口（条件分支端口实时显示「符合 / 不符合」并变色）。</summary>
public sealed class NgPortStateViewModel : INotifyPropertyChanged
{
    public string Label { get; }
    private string _colorHex = "#64748B";
    public NgPortStateViewModel(string label) { Label = label; }

    /// <summary>该端口是否参与判定（条件分支的端口才有状态文字）。</summary>
    private bool _hasState;
    public bool HasState
    {
        get => _hasState;
        set { if (_hasState != value) { _hasState = value; Notify(nameof(HasState)); Notify(nameof(StateText)); } }
    }

    private bool _satisfied;
    public bool Satisfied
    {
        get => _satisfied;
        set
        {
            if (_satisfied == value) return;
            _satisfied = value;
            Notify(nameof(Satisfied));
            Notify(nameof(StateText));
            Notify(nameof(StateColor));
            Notify(nameof(ColorHex));
        }
    }

    /// <summary>「符合 / 不符合」（非判定端口为空串）。</summary>
    public string StateText => !_hasState ? string.Empty : (_satisfied ? "符合" : "不符合");
    /// <summary>状态文字颜色：符合=绿、不符合=灰。</summary>
    public string StateColor => _satisfied ? "#16A34A" : "#94A3B8";

    /// <summary>端口圆点颜色：判定端口按符合状态变色；普通端口用节点颜色。</summary>
    public string ColorHex
    {
        get => _hasState ? StateColor : _colorHex;
        set { if (_colorHex != value) { _colorHex = value; Notify(nameof(ColorHex)); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}

/// <summary>属性面板的一组属性（条件分支按「条件1 / 条件2…」分组，便于分辨；组标题可显示实时符合状态）。</summary>
public sealed class NgPropGroupViewModel : INotifyPropertyChanged
{
    public string Title { get; }
    public bool HasTitle => !string.IsNullOrEmpty(Title);
    public System.Collections.ObjectModel.ObservableCollection<NgPropViewModel> Items { get; } = new();

    public NgPropGroupViewModel(string title) { Title = title; }

    /// <summary>组标题是否显示「符合 / 不符合」（条件分支的每个条件组）。</summary>
    private bool _showState;
    public bool ShowState
    {
        get => _showState;
        set { if (_showState != value) { _showState = value; Notify(nameof(ShowState)); } }
    }

    private bool _satisfied;
    public bool Satisfied
    {
        get => _satisfied;
        set
        {
            if (_satisfied == value) return;
            _satisfied = value;
            Notify(nameof(Satisfied)); Notify(nameof(StateText)); Notify(nameof(StateColor));
        }
    }
    public string StateText => _satisfied ? "符合" : "不符合";
    public string StateColor => _satisfied ? "#16A34A" : "#94A3B8";

    /// <summary>是否显示该组（条件分支里超出「分支数」的组隐藏）。</summary>
    private bool _visible = true;
    public bool IsVisible
    {
        get => _visible;
        set { if (_visible != value) { _visible = value; Notify(nameof(IsVisible)); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}

/// <summary>节点图节点 ViewModel（INPC）：包裹 POCO 的 NgNode，暴露可绑定的坐标 / 选中态 / 属性。
/// X/Y 变更会触发 PropertyChanged（"X"/"Y"），连线 VM 据此重算贝塞尔几何。</summary>
public sealed class NodeGraphNodeViewModel : INotifyPropertyChanged
{
    private readonly NgNode _model;

    public NgNode Model => _model;
    public string Id => _model.Id;
    public NgKind Kind => _model.Kind;
    public NgDomain Domain => NgNodeDefinitions.All[_model.Kind].Domain;
    /// <summary>领域中文名（视觉 / 运控 / 通讯），供属性面板显示。</summary>
    public string DomainText => NgNodeDefinitions.DomainTitle.TryGetValue(Domain, out var t) ? t : Domain.ToString();
    public string Title => NgNodeDefinitions.All[_model.Kind].Title;
    public string Color => NgNodeDefinitions.All[_model.Kind].Color;
    public bool HasInput => NgNodeDefinitions.All[_model.Kind].HasInput;
    public IReadOnlyList<string> Outputs => NgNodeDefinitions.All[_model.Kind].Outputs;

    public double X
    {
        get => _model.X;
        set { if (_model.X != value) { _model.X = value; OnChanged(nameof(X)); } }
    }
    public double Y
    {
        get => _model.Y;
        set { if (_model.Y != value) { _model.Y = value; OnChanged(nameof(Y)); } }
    }

    public ObservableCollection<NgPropViewModel> Props { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected != value) { _isSelected = value; OnChanged(nameof(IsSelected)); } }
    }

    // ===================== 调试器相关属性（由 NgRunner 报告驱动，XAML 用 DataTrigger 渲染色边框/耗时/异常/断点） =====================

    private NgStepResult? _stepResult;
    public NgStepResult? StepResult
    {
        get => _stepResult;
        set
        {
            if (_stepResult == value) return;
            _stepResult = value;
            OnChanged(nameof(StepResult));
            OnChanged(nameof(StepStatus));
            OnChanged(nameof(StatusText));
            OnChanged(nameof(DurationText));
            OnChanged(nameof(ErrorText));
            OnChanged(nameof(HasError));
            OnChanged(nameof(Summary));
            OnChanged(nameof(HasSummary));
        }
    }
    /// <summary>节点执行状态（Idle/Running/Done/Error/Paused/Skipped）。XAML 用它做 DataTrigger 切换 BorderBrush 与浮标颜色。</summary>
    public NgStepStatus StepStatus => _stepResult?.Status ?? NgStepStatus.Idle;
    public string StatusText => _stepResult?.StatusText ?? string.Empty;
    public string DurationText => _stepResult?.DurationText ?? string.Empty;
    public string ErrorText => _stepResult?.ErrorText ?? string.Empty;
    public bool HasError => _stepResult?.Status == NgStepStatus.Error;

    /// <summary>结果摘要（视觉节点：匹配分数 / 缺陷数 / 测量值 / 偏差 / 像素当量）。</summary>
    public string Summary => _stepResult?.Summary ?? string.Empty;
    /// <summary>有摘要且非异常时才在卡片上显示摘要行（异常已有红条）。</summary>
    public bool HasSummary => !string.IsNullOrWhiteSpace(_stepResult?.Summary) && !HasError;

    private bool _hasBreakpoint;
    /// <summary>是否有断点（标题栏右上小红点）。</summary>
    public bool HasBreakpoint
    {
        get => _hasBreakpoint;
        set { if (_hasBreakpoint != value) { _hasBreakpoint = value; OnChanged(nameof(HasBreakpoint)); } }
    }

    private bool _isCurrent;
    /// <summary>是否正在执行（脉冲外发光）。</summary>
    public bool IsCurrent
    {
        get => _isCurrent;
        set { if (_isCurrent != value) { _isCurrent = value; OnChanged(nameof(IsCurrent)); } }
    }

    public NodeGraphNodeViewModel(NgNode model, System.Action? onPropChanged = null)
    {
        _model = model;
        // 属性变更时：先刷新「条件N名称候选 / 分支显隐 / 端口判定」，再回调外部（自动保存 / 重绘连线）
        Props = new ObservableCollection<NgPropViewModel>(
            model.Props.Select(p => new NgPropViewModel(p, () =>
            {
                RefreshPropDependencies();
                RefreshDecisionState();
                onPropChanged?.Invoke();
            }, model)));
        BuildPanelGroups();
        BuildOutputPorts();
    }

    /// <summary>某个属性变了 → 刷新所有行的「候选 / 显隐」。
    /// 例：改「条件1类型」→「条件1名称」候选跟着换；改「分支数」→ 多余的条件组隐藏。</summary>
    public void RefreshPropDependencies()
    {
        foreach (var p in Props) p.NotifyDependentChanged();
    }

    /// <summary>输入端口锚点（画布坐标）。</summary>
    public Point InputPoint => NgGeometry.InputPoint(X, Y);
    /// <summary>输出端口锚点（画布坐标）。</summary>
    public Point OutputPoint(int portIndex) => NgGeometry.OutputPoint(X, Y, portIndex);
    /// <summary>返回输出端口名对应的索引（用于几何计算）；找不到返回 0。</summary>
    public int OutputPortIndex(string? port)
    {
        if (string.IsNullOrEmpty(port)) return 0;
        var outs = Outputs;
        for (int i = 0; i < outs.Count; i++)
            if (outs[i] == port) return i;
        return 0;
    }

    // ===================== 属性面板「实时值」（1 秒刷新） =====================

    /// <summary>是否显示实时值行：轴类节点显示「实际位置」，设置变量 / 运算 显示「当前值」。</summary>
    public bool ShowsLiveRow =>
        Kind is NgKind.MoveAxis or NgKind.Home or NgKind.WaitAxis or NgKind.VarSet or NgKind.Compute;

    /// <summary>实时值行的标签。</summary>
    public string LiveLabel =>
        Kind is NgKind.MoveAxis or NgKind.Home or NgKind.WaitAxis ? "实际位置" : "当前值";

    private string _liveValueText = "—";
    /// <summary>实时值文本（轴当前位置 / 变量当前值；读不到显示「—」）。</summary>
    public string LiveValueText
    {
        get => _liveValueText;
        set { if (_liveValueText != value) { _liveValueText = value; OnChanged(nameof(LiveValueText)); } }
    }

    /// <summary>刷新实时值：轴类节点读当前位置（真实桥优先、回退运行态缓存）；
    /// 设置变量 / 运算 读「变量」属性所指变量的当前值。
    /// 由属性面板的 1 秒定时器调用，运行时随轴运动 / 变量赋值实时变化。</summary>
    public void RefreshLiveValue()
    {
        if (!ShowsLiveRow) return;

        if (Kind is NgKind.VarSet or NgKind.Compute)
        {
            string varName = Props.FirstOrDefault(p => p.Name == "变量")?.Value ?? string.Empty;
            LiveValueText = string.IsNullOrWhiteSpace(varName)
                ? "—"
                : SimRuntime.GetVariableResolved(varName).ToString("0.###", CultureInfo.InvariantCulture);
            return;
        }

        string axisName = Props.FirstOrDefault(p => p.Name == "轴")?.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(axisName)) { LiveValueText = "—"; return; }
        double pos = double.NaN;
        try
        {
            var ax = HardwareResolver.ResolveAxis(axisName);
            var br = HardwareBridge.Current;
            if (ax != null && br != null) pos = br.ReadAxisPosition(ax);
        }
        catch { pos = double.NaN; }
        if (double.IsNaN(pos)) pos = AxisRuntimeState.Get(axisName);
        LiveValueText = pos.ToString("0.###", CultureInfo.InvariantCulture);
    }

    // ===================== 属性面板分组 / 端口实时判定 =====================

    /// <summary>属性面板的分组（条件分支按「条件1 / 条件2…」分组，便于分辨；其它节点只有一组、无标题）。</summary>
    public ObservableCollection<NgPropGroupViewModel> PanelGroups { get; } = new();

    /// <summary>节点卡上的输出端口（条件分支端口实时显示「符合 / 不符合」并变色）。</summary>
    public ObservableCollection<NgPortStateViewModel> OutputPorts { get; } = new();

    private void BuildPanelGroups()
    {
        if (Kind != NgKind.Decision)
        {
            var g = new NgPropGroupViewModel("");
            foreach (var p in Props) g.Items.Add(p);
            PanelGroups.Add(g);
            return;
        }
        // 条件分支：分支设置 → 每个条件一组 → 高级表达式
        var head = new NgPropGroupViewModel("分支设置");
        foreach (var p in Props.Where(x => x.Name == "分支数")) head.Items.Add(p);
        PanelGroups.Add(head);

        for (int i = 1; i <= NgConditionEvaluator.MaxBranches; i++)
        {
            var g = new NgPropGroupViewModel($"条件{i}") { ShowState = true };
            foreach (var p in Props.Where(x => x.Name.StartsWith($"条件{i}", System.StringComparison.Ordinal) && x.Name != "条件"))
                g.Items.Add(p);
            PanelGroups.Add(g);
        }
        var adv = new NgPropGroupViewModel("高级（表达式条件）");
        foreach (var p in Props.Where(x => x.Name == "条件")) adv.Items.Add(p);
        PanelGroups.Add(adv);
    }

    private void BuildOutputPorts()
    {
        OutputPorts.Clear();
        bool decision = Kind == NgKind.Decision;
        foreach (var label in Outputs)
        {
            var pm = new NgPortStateViewModel(label) { ColorHex = Color };
            if (decision) pm.HasState = true;      // 条件分支端口显示 符合 / 不符合
            OutputPorts.Add(pm);
        }
    }

    /// <summary>刷新条件分支的端口与分组判定（1 秒定时器对所有条件分支节点调用 → 卡片端口实时变色）。</summary>
    public void RefreshDecisionState()
    {
        if (Kind != NgKind.Decision) return;
        int n = NgConditionEvaluator.BranchCount(Model);

        // 端口：每条分支按自身条件判定（符合=绿 / 不符合=灰）；「否则」= 没有任何分支满足时符合
        bool anyHit = false;
        for (int i = 1; i <= n; i++) if (NgConditionEvaluator.Evaluate(Model, i)) { anyHit = true; break; }
        foreach (var pm in OutputPorts)
        {
            if (pm.Label == "否则") pm.Satisfied = !anyHit;
            else if (pm.Label.StartsWith("条件", System.StringComparison.Ordinal)
                     && int.TryParse(pm.Label.Substring(2), out int i))
                pm.Satisfied = i <= n && NgConditionEvaluator.Evaluate(Model, i);
        }

        // 分组：组标题显示 符合/不符合；超出「分支数」的组隐藏
        foreach (var g in PanelGroups)
        {
            if (!g.ShowState) continue;
            if (!int.TryParse(g.Title.Substring(2), out int bi)) continue;
            g.IsVisible = bi <= n;
            g.Satisfied = NgConditionEvaluator.Evaluate(Model, bi);
        }
    }

    /// <summary>属性面板 1 秒定时器调用：刷新实时值（轴位置 / 变量当前值）+ 条件分支端口/分组判定。</summary>
    public void RefreshPanelState()
    {
        RefreshLiveValue();
        RefreshDecisionState();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
