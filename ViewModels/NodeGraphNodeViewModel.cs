// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
// ◆温‎启‏志‎◆​编‌写⁠◇‌微​信​﹕⁣1​8‎7⁣◆‌1​9​3‍6‎◇‎1⁠3⁠9⁠9‍　‏※⁣保​留​所‎有‌权⁣利‌请​勿​删⁠除​◇
// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using NoCodeMotion.Models.NodeGraph;

namespace NoCodeMotion.ViewModels;

/// <summary>节点属性项 ViewModel（INPC）：双向绑定到属性面板；值变更回调触发自动保存。</summary>
public sealed class NgPropViewModel : INotifyPropertyChanged
{
    private readonly NgProp _model;
    private readonly System.Action? _onChanged;

    public string Name => _model.Name;
    public bool HasOptions => !string.IsNullOrEmpty(_model.Options);
    public System.Collections.Generic.List<string> OptionsList
        => _model.Options?.Split('|')?.ToList() ?? new System.Collections.Generic.List<string>();

    public string Value
    {
        get => _model.Value;
        set { if (_model.Value != value) { _model.Value = value; OnChanged(); _onChanged?.Invoke(); } }
    }

    public NgPropViewModel(NgProp model, System.Action? onChanged = null)
    {
        _model = model;
        _onChanged = onChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
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
        }
    }
    /// <summary>节点执行状态（Idle/Running/Done/Error/Paused/Skipped）。XAML 用它做 DataTrigger 切换 BorderBrush 与浮标颜色。</summary>
    public NgStepStatus StepStatus => _stepResult?.Status ?? NgStepStatus.Idle;
    public string StatusText => _stepResult?.StatusText ?? string.Empty;
    public string DurationText => _stepResult?.DurationText ?? string.Empty;
    public string ErrorText => _stepResult?.ErrorText ?? string.Empty;
    public bool HasError => _stepResult?.Status == NgStepStatus.Error;

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
        Props = new ObservableCollection<NgPropViewModel>(
            model.Props.Select(p => new NgPropViewModel(p, onPropChanged)));
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

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
