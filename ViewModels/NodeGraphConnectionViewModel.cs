// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
// ◆温​启⁣志⁣◆​编⁣写‍◇‎微​信⁣﹕‌1⁣8‏7‏◆⁠1⁣9‎3‏6​◇⁠1⁣3‏9‏9​　‎※⁠保⁠留‏所⁠有⁠权⁣利​请⁠勿‌删⁣除‍◇
// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using NoCodeMotion.Models.NodeGraph;

namespace NoCodeMotion.ViewModels;

/// <summary>节点连线 ViewModel（INPC）：根据源/目标节点坐标计算贝塞尔路径与箭头三角。
/// 节点移动时（X/Y 变更）自动重算几何。</summary>
public sealed class NodeGraphConnectionViewModel : INotifyPropertyChanged
{
    private readonly NgConnection _model;
    private readonly NodeGraphNodeViewModel _src;
    private readonly NodeGraphNodeViewModel _tgt;
    private readonly string _port;

    public NgConnection Model => _model;
    public string SourceId => _model.SourceId;
    public string TargetId => _model.TargetId;
    public string SourcePort => _model.SourcePort;

    private Geometry _pathGeometry = null!;
    public Geometry PathGeometry => _pathGeometry;

    private PointCollection _arrowPoints = null!;
    public PointCollection ArrowPoints => _arrowPoints;

    private readonly Brush _brush;
    /// <summary>线条/箭头颜色：选中时变红（提示「删除所选」作用于它），否则按端口语义着色。</summary>
    public Brush Brush => IsSelected ? SelectedBrush : _brush;
    /// <summary>线条粗细：选中时加粗，便于确认选中的是哪一条。</summary>
    public double Thickness => IsSelected ? 3.4 : 2.2;

    private static readonly Brush SelectedBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));

    private bool _isSelected;
    /// <summary>是否被选中（点击连线即选中，选中后「删除所选 / Delete 键」可删除）。</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnChanged(nameof(IsSelected));
            OnChanged(nameof(Brush));
            OnChanged(nameof(Thickness));
        }
    }

    /// <summary>线的可读描述（源端口 → 目标），供提示/日志用。</summary>
    public string Describe => $"{_src.Title} · {_port} → {_tgt.Title}";

    public NodeGraphConnectionViewModel(NgConnection model, NodeGraphNodeViewModel src, NodeGraphNodeViewModel tgt)
    {
        _model = model;
        _src = src;
        _tgt = tgt;
        _port = model.SourcePort;

        _brush = _port switch
        {
            "True" => new SolidColorBrush(Color.FromRgb(22, 163, 74)),
            "False" => new SolidColorBrush(Color.FromRgb(229, 87, 63)),
            _ => new SolidColorBrush(Color.FromRgb(74, 137, 220))
        };

        _src.PropertyChanged += OnNodeChanged;
        _tgt.PropertyChanged += OnNodeChanged;
        Recompute();
    }

    private void OnNodeChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(NodeGraphNodeViewModel.X) or nameof(NodeGraphNodeViewModel.Y))
            Recompute();
    }

    private void Recompute()
    {
        int idx = System.Math.Max(0, _src.OutputPortIndex(_port));
        var p0 = _src.OutputPoint(idx);
        var p1 = _tgt.InputPoint;
        _pathGeometry = NgGeometry.MakeBezier(p0, p1);
        _arrowPoints = NgGeometry.MakeArrow(p1, p0);
        OnChanged(nameof(PathGeometry));
        OnChanged(nameof(ArrowPoints));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
