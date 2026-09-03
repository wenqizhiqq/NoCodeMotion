// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// ◆◇※▣▤▥ۦ▧▨۩░ے▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦۧ
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NoCodeMotion.Models;
using NoCodeMotion.Models.NodeGraph;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views;

/// <summary>
/// 节点图编辑器页。DataContext = NodeGraphViewModel；所有渲染由 XAML 的 ItemsControl + DataTemplate 完成。
/// 本文件仅处理交互（节点拖拽移动、输出→输入拖拽连线、选中）与 FlowPage 的选中流程同步。
/// 同步方式同 VisualFlowPage：Loaded 后沿可视树找 FlowPage 祖先，订阅 FlowViewModel.SelectedItem 变化。
/// </summary>
public partial class NodeGraphPage : UserControl
{
    private readonly NodeGraphViewModel _vm = new NodeGraphViewModel();
    private FlowPage? _flowPage;
    private PropertyChangedEventHandler? _fvmHandler;

    // 拖拽状态
    private NodeGraphNodeViewModel? _dragNode;
    private Point _dragOffset;
    private NodeGraphNodeViewModel? _linkSrc;
    private string? _linkPort;

    // 缩放（滚轮）与工具箱拖拽状态
    private double _zoom = 1.0;
    private readonly ScaleTransform _scale = new ScaleTransform(1, 1);
    private NgKind? _dragKind;
    private Point _dragStartPoint;

    public NodeGraphPage()
    {
        InitializeComponent();
        DataContext = _vm;
        DesignerCanvas.LayoutTransform = _scale;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DetachFlowViewModel();
        DependencyObject? cur = this;
        while (cur != null)
        {
            cur = VisualTreeHelper.GetParent(cur);
            if (cur is FlowPage fp) { _flowPage = fp; break; }
        }
        if (_flowPage?.DataContext is INotifyPropertyChanged inpc)
        {
            _fvmHandler = (_, args) =>
            {
                if (args.PropertyName == nameof(FlowViewModel.SelectedItem)) ApplySelection();
            };
            inpc.PropertyChanged += _fvmHandler;
        }
        ApplySelection();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => DetachFlowViewModel();

    private void DetachFlowViewModel()
    {
        if (_flowPage?.DataContext is INotifyPropertyChanged inpc && _fvmHandler != null)
            inpc.PropertyChanged -= _fvmHandler;
        _fvmHandler = null;
    }

    private void ApplySelection()
    {
        if (_flowPage?.DataContext is FlowViewModel fvm)
        {
            var sel = fvm.SelectedItem;
            if (sel != null && sel.Kind == FlowKind.NodeGraph)
                _vm.LoadFrom(sel);
        }
    }

    // ===================== 画布交互 =====================

    private void DesignerCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var dep = e.OriginalSource as DependencyObject;

        // 1) 连线命中（透明粗线）
        if (FindWithTag(dep, "CONN") is FrameworkElement connEl && connEl.DataContext is NodeGraphConnectionViewModel conn)
        {
            _vm.SelectedConnection = conn;
            e.Handled = true;
            return;
        }
        // 2) 输出端口 → 开始连线
        if (FindWithTag(dep, "OUT") is FrameworkElement outEl)
        {
            var port = outEl.DataContext as string;
            var src = FindNodeVm(outEl);
            if (src != null && port != null) { BeginLink(src, port, e); return; }
        }
        // 3) 输入端口 → 等待 mouseup 完成连线（此处不动作）
        if (FindWithTag(dep, "IN") != null) { e.Handled = true; return; }
        // 4) 标题栏 → 拖拽移动
        if (FindWithTag(dep, "NODE_HEADER") is FrameworkElement hdr)
        {
            var node = FindNodeVm(hdr);
            if (node != null) { BeginNodeDrag(node, e); return; }
        }
        // 5) 节点其它区域 → 选中
        var anyNode = FindNodeVm(dep);
        if (anyNode != null) { _vm.SelectedNode = anyNode; return; }
        // 6) 空白 → 取消选中
        _vm.SelectedNode = null;
    }

    private void DesignerCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragNode != null)
        {
            var p = e.GetPosition(DesignerCanvas);
            _dragNode.X = Math.Max(0, p.X - _dragOffset.X);
            _dragNode.Y = Math.Max(0, p.Y - _dragOffset.Y);
            e.Handled = true;
        }
        else if (_linkSrc != null)
        {
            UpdateTempLine(e.GetPosition(DesignerCanvas));
            e.Handled = true;
        }
    }

    private void DesignerCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragNode != null)
        {
            _dragNode = null;
            DesignerCanvas.ReleaseMouseCapture();
            _vm.Save();
            e.Handled = true;
        }
        else if (_linkSrc != null)
        {
            var pt = e.GetPosition(DesignerCanvas);
            var hit = VisualTreeHelper.HitTest(DesignerCanvas, pt)?.VisualHit;
            var inEl = FindWithTag(hit, "IN");
            if (inEl != null)
            {
                var tgt = FindNodeVm(inEl);
                if (tgt != null && tgt != _linkSrc)
                    _vm.Connect(_linkSrc.Id, _linkPort!, tgt.Id);
            }
            EndLink();
            e.Handled = true;
        }
    }

    private void BeginNodeDrag(NodeGraphNodeViewModel vm, MouseButtonEventArgs e)
    {
        _dragNode = vm;
        var p = e.GetPosition(DesignerCanvas);
        _dragOffset = new Point(p.X - vm.X, p.Y - vm.Y);
        _vm.SelectedNode = vm;
        DesignerCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void BeginLink(NodeGraphNodeViewModel src, string port, MouseButtonEventArgs e)
    {
        _linkSrc = src;
        _linkPort = port;
        TempLine.Visibility = Visibility.Visible;
        UpdateTempLine(e.GetPosition(DesignerCanvas));
        DesignerCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void EndLink()
    {
        _linkSrc = null;
        _linkPort = null;
        TempLine.Visibility = Visibility.Collapsed;
        TempLine.Data = null;
        if (DesignerCanvas.IsMouseCaptured) DesignerCanvas.ReleaseMouseCapture();
    }

    // ===================== 工具箱拖拽 / 缩放 / 滚动 =====================

    // 从工具箱拖出节点：按下记录类型并捕获鼠标，移动超过阈值即发起 DragDrop。
    private void ToolItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is NgNodeDef def && fe is UIElement ui)
        {
            _dragKind = def.Kind;
            _dragStartPoint = e.GetPosition(this);
            ui.CaptureMouse();
        }
    }

    private void ToolItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragKind == null) return;
        var ui = sender as UIElement;
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            ui?.ReleaseMouseCapture();
            _dragKind = null;
            return;
        }
        var cur = e.GetPosition(this);
        if (Math.Abs(cur.X - _dragStartPoint.X) < 4 && Math.Abs(cur.Y - _dragStartPoint.Y) < 4) return;
        var kind = _dragKind.Value;
        _dragKind = null;
        ui?.ReleaseMouseCapture();
        DragDrop.DoDragDrop(ui ?? this, new DataObject("NgKind", kind), DragDropEffects.Copy);
    }

    private void ToolItem_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is UIElement ui) ui.ReleaseMouseCapture();
        _dragKind = null;
    }

    // 在画布上放下工具箱拖来的节点：落点即为节点中心（逻辑坐标，随缩放自动一致）。
    private void DesignerCanvas_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("NgKind")) return;
        var kind = (NgKind)e.Data.GetData("NgKind");
        var p = e.GetPosition(DesignerCanvas);
        _vm.AddNode(kind, p.X - NgGeometry.NodeWidth / 2, Math.Max(0, p.Y - NgGeometry.HeaderHeight / 2));
        e.Handled = true;
    }

    // 滚轮缩放：限制在 0.3~2.5 倍；缩放施加在画布 LayoutTransform 上，节点/连线坐标保持逻辑一致。
    private void DesignerCanvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        double factor = e.Delta > 0 ? 1.1 : 1.0 / 1.1;
        double nz = Math.Max(0.3, Math.Min(2.5, _zoom * factor));
        if (nz != _zoom)
        {
            _zoom = nz;
            _scale.ScaleX = _scale.ScaleY = _zoom;
        }
        e.Handled = true;
    }

    private void UpdateTempLine(Point cursor)
    {
        if (_linkSrc == null || _linkPort == null) return;
        int idx = Math.Max(0, _linkSrc.OutputPortIndex(_linkPort));
        var p0 = _linkSrc.OutputPoint(idx);
        TempLine.Data = NgGeometry.MakeBezier(p0, cursor);
    }

    // ===================== 命中辅助 =====================

    private static DependencyObject? FindWithTag(DependencyObject? dep, string tag)
    {
        while (dep != null)
        {
            if (dep is FrameworkElement fe && fe.Tag as string == tag) return fe;
            dep = VisualTreeHelper.GetParent(dep);
        }
        return null;
    }

    private static NodeGraphNodeViewModel? FindNodeVm(DependencyObject? dep)
    {
        while (dep != null)
        {
            if (dep is FrameworkElement fe && fe.DataContext is NodeGraphNodeViewModel vm) return vm;
            dep = VisualTreeHelper.GetParent(dep);
        }
        return null;
    }
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦۧ
