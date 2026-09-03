// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
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
    public Brush Brush => _brush;

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
