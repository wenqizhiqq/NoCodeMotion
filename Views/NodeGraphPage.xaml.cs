// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
// ◆温⁠启⁣志‍◆‍编‌写‎◇⁣微‍信⁣﹕‌1‎8​7‌◆‎1⁣9‎3⁣6⁠◇‍1​3​9⁣9‍　⁣※⁣保​留⁣所‍有‍权‏利‍请‍勿‎删⁠除⁣◇
// ◆◇※▣▤▥ۦ▧▨۩░ے▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦۧ
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    // 整组拖动：框选出多个节点后，拖其中任意一个 → 记录「每个节点相对鼠标的偏移」，一起平移
    private readonly System.Collections.Generic.List<(NodeGraphNodeViewModel Node, double Dx, double Dy)> _groupDrag = new();
    // 左键框选
    private bool _marquee;
    private Point _marqueeStart;
    private NodeGraphNodeViewModel? _linkSrc;
    private string? _linkPort;

    // 缩放（滚轮）与工具箱拖拽状态
    private double _zoom = 1.0;
    private readonly ScaleTransform _scale = new ScaleTransform(1, 1);
    private NgNodeDef? _dragTool;
    private Point _dragStartPoint;

    // 空白画布拖拽平移
    private bool _panning;
    private Point _panStart;
    private double _scrollStartX;
    private double _scrollStartY;

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

    /// <summary>
    /// 外部替换了当前流程的 GraphJson（流程页「粘贴生成」）后调用，按新的图重建画布。
    /// GraphJson 是普通字符串属性，没有「集合变了」这种信号可订阅，只能由宿主显式通知。
    /// </summary>
    public void Reload() => ApplySelection();

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

        // 让画布拿到键盘焦点，「Delete 键删除所选」才生效
        if (!DesignerCanvas.IsKeyboardFocused) DesignerCanvas.Focus();

        // 1) 连线命中（透明粗线）
        if (FindWithTag(dep, "CONN") is FrameworkElement connEl && connEl.DataContext is NodeGraphConnectionViewModel conn)
        {
            _vm.SelectedConnection = conn;
            e.Handled = true;
            return;
        }
        // 1.5) 断点角标命中 → 鼠标点击设置/取消断点（不需要先选中节点；Tag=NODE_BP，在节点卡右上角）
        if (FindWithTag(dep, "NODE_BP") is FrameworkElement bpEl)
        {
            var bpNode = FindNodeVm(bpEl);
            if (bpNode != null) { _vm.ToggleBreakpointAt(bpNode.Id); e.Handled = true; return; }
        }
        // 2) 输出端口 → 开始连线
        // 端口现在是 ItemsControl 绑定 OutputPorts（项是 NgPortStateViewModel，不再是裸字符串），
        // 端口名取 VM 的 Label；拿不到再回退旧形态的 string DataContext。
        if (FindWithTag(dep, "OUT") is FrameworkElement outEl)
        {
            string? port = (outEl.DataContext as NgPortStateViewModel)?.Label
                           ?? outEl.DataContext as string;
            var src = FindNodeVm(outEl);
            if (src != null && !string.IsNullOrEmpty(port)) { BeginLink(src, port, e); return; }
        }
        // 3) 输入端口 → 等待 mouseup 完成连线（此处不动作）
        if (FindWithTag(dep, "IN") != null) { e.Handled = true; return; }
        // 4) 整张节点卡（除 OUT/IN 端口外，标题栏/主体/属性区/浮标区均可）→ 拖拽移动，单击即选中
        //    属于框选组时不先清高亮，否则整组会散架（清组的事交给 BeginNodeDrag 判断）
        var anyNode = FindNodeVm(dep);
        if (anyNode != null && !(_vm.SelectedNodes.Count > 1 && _vm.SelectedNodes.Contains(anyNode)))
            _vm.ClearSelectedNodes();
        if (anyNode != null) { BeginNodeDrag(anyNode, e); return; }
        // 6) 空白 → 取消选中并开始框选（平移让给中键）
        _vm.ClearSelectedNodes();
        _vm.SelectedNode = null;
        _vm.SelectedConnection = null;
        _marquee = true;
        _marqueeStart = e.GetPosition(DesignerCanvas);
        PlaceMarquee(_marqueeStart, _marqueeStart);
        MarqueeBox.Visibility = Visibility.Visible;
        DesignerCanvas.CaptureMouse();
        e.Handled = true;
    }

    /// <summary>中键按住 = 平移画布（左键要留给选中 / 拖动 / 框选）。</summary>
    private void DesignerCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle) return;     // 左 / 右键走各自的处理器
        _marquee = false;
        MarqueeBox.Visibility = Visibility.Collapsed;
        _panning = true;
        // ★ 位移必须在父空间量：画布自身带 LayoutTransform（缩放），在自己的空间里量会跟着缩放变形
        _panStart = e.GetPosition(CanvasScroller);
        _scrollStartX = CanvasScroller.ContentHorizontalOffset;
        _scrollStartY = CanvasScroller.ContentVerticalOffset;
        DesignerCanvas.CaptureMouse();
        Mouse.OverrideCursor = Cursors.ScrollAll;
        e.Handled = true;
    }

    private void DesignerCanvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle || !_panning) return;
        _panning = false;
        if (DesignerCanvas.IsMouseCaptured) DesignerCanvas.ReleaseMouseCapture();
        Mouse.OverrideCursor = null;
        e.Handled = true;
    }

    /// <summary>右键点在某条连线上 → 直接断开该连线（等价于选中后「删除所选」）。</summary>
    private void DesignerCanvas_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var dep = e.OriginalSource as DependencyObject;
        if (FindWithTag(dep, "CONN") is FrameworkElement connEl
            && connEl.DataContext is NodeGraphConnectionViewModel conn)
        {
            _vm.DeleteConnection(conn);
            e.Handled = true;
        }
    }

    /// <summary>Delete / Back 键删除当前选中的节点或连线（画布获得焦点时生效）。</summary>
    private void DesignerCanvas_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete && e.Key != Key.Back) return;
        if (e.OriginalSource is TextBoxBase) return;        // 正在输入框里编辑文本 → 不拦截
        if (!_vm.HasSelection) return;
        _vm.DeleteCommand.Execute(null);
        e.Handled = true;
    }

    private void DesignerCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_groupDrag.Count > 0)
        {
            var gp = e.GetPosition(DesignerCanvas);
            foreach (var (node, dx, dy) in _groupDrag)
            {
                node.X = Math.Max(0, gp.X - dx);
                node.Y = Math.Max(0, gp.Y - dy);
            }
            e.Handled = true;
        }
        else if (_dragNode != null)
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
        else if (_marquee)
        {
            PlaceMarquee(_marqueeStart, e.GetPosition(DesignerCanvas));
            e.Handled = true;
        }
        else if (_panning)
        {
            var cur = e.GetPosition(CanvasScroller);
            CanvasScroller.ScrollToHorizontalOffset(_scrollStartX - (cur.X - _panStart.X));
            CanvasScroller.ScrollToVerticalOffset(_scrollStartY - (cur.Y - _panStart.Y));
            e.Handled = true;
        }
    }

    private void DesignerCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_groupDrag.Count > 0)
        {
            _groupDrag.Clear();
            DesignerCanvas.ReleaseMouseCapture();
            _vm.Save();
            e.Handled = true;
        }
        else if (_dragNode != null)
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
        else if (_marquee)
        {
            _marquee = false;
            MarqueeBox.Visibility = Visibility.Collapsed;
            if (DesignerCanvas.IsMouseCaptured) DesignerCanvas.ReleaseMouseCapture();
            var end = e.GetPosition(DesignerCanvas);
            // 没拖动 = 只是点了一下空白：取消选中就完事，不要框出一个零宽选择
            if (Math.Abs(end.X - _marqueeStart.X) > 4 || Math.Abs(end.Y - _marqueeStart.Y) > 4)
            {
                var box = new Rect(_marqueeStart, end);
                var hits = _vm.Nodes.Where(n => NodeInBox(n, box)).ToList();
                if (hits.Count > 0) _vm.SelectNodes(hits);
            }
            e.Handled = true;
        }
        else if (_panning)
        {
            _panning = false;
            if (DesignerCanvas.IsMouseCaptured) DesignerCanvas.ReleaseMouseCapture();
            Mouse.OverrideCursor = null;
            e.Handled = true;
        }
    }

    /// <summary>把两个角点画成选择框（Rect 自带归一化，反向拖动也正确）。</summary>
    private void PlaceMarquee(Point a, Point b)
    {
        var r = new Rect(a, b);
        Canvas.SetLeft(MarqueeBox, r.Left);
        Canvas.SetTop(MarqueeBox, r.Top);
        MarqueeBox.Width = r.Width;
        MarqueeBox.Height = r.Height;
    }

    /// <summary>节点是否被框到：按容器实际渲染范围判断（含属性区，且自动跟着缩放走）。</summary>
    private bool NodeInBox(NodeGraphNodeViewModel node, Rect box)
    {
        if (NodesHost.ItemContainerGenerator.ContainerFromItem(node) is not FrameworkElement el) return false;
        if (el.ActualWidth <= 0 || el.ActualHeight <= 0) return false;
        var origin = el.TransformToVisual(DesignerCanvas).Transform(new Point(0, 0));
        return box.IntersectsWith(new Rect(origin, new Size(el.ActualWidth, el.ActualHeight)));
    }

    private void BeginNodeDrag(NodeGraphNodeViewModel vm, MouseButtonEventArgs e)
    {
        var p = e.GetPosition(DesignerCanvas);
        _groupDrag.Clear();
        if (_vm.SelectedNodes.Count > 1 && _vm.SelectedNodes.Contains(vm))
        {
            // 拖的是刚框选出的一组里的任意一个 → 整组按同一偏移一起走
            foreach (var n in _vm.SelectedNodes) _groupDrag.Add((n, p.X - n.X, p.Y - n.Y));
        }
        else
        {
            _vm.ClearSelectedNodes();           // 只拖这一个，框选组作废
            _dragNode = vm;
            _dragOffset = new Point(p.X - vm.X, p.Y - vm.Y);
        }
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
            _dragTool = def;
            _dragStartPoint = e.GetPosition(this);
            ui.CaptureMouse();
        }
    }

    private void ToolItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragTool == null) return;
        var ui = sender as UIElement;
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            ui?.ReleaseMouseCapture();
            _dragTool = null;
            return;
        }
        var cur = e.GetPosition(this);
        if (Math.Abs(cur.X - _dragStartPoint.X) < 4 && Math.Abs(cur.Y - _dragStartPoint.Y) < 4) return;
        var tool = _dragTool;
        _dragTool = null;
        ui?.ReleaseMouseCapture();
        DragDrop.DoDragDrop(ui ?? this, new DataObject("NgTool", tool), DragDropEffects.Copy);
    }

    private void ToolItem_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is UIElement ui) ui.ReleaseMouseCapture();
        _dragTool = null;
    }

    // 在画布上放下工具箱拖来的节点：落点即为节点中心（逻辑坐标，随缩放自动一致）。
    private void DesignerCanvas_Drop(object sender, DragEventArgs e)
    {
        if (!(e.Data.GetData("NgTool") is NgNodeDef tool)) return;
        var p = e.GetPosition(DesignerCanvas);
        _vm.AddTool(tool, p.X - NgGeometry.NodeWidth / 2, Math.Max(0, p.Y - NgGeometry.HeaderHeight / 2));
        e.Handled = true;
    }

    // 滚轮缩放：限制在 0.3~2.5 倍；以光标位置为锚点缩放（缩放后光标下的逻辑点保持不动）。
    private void DesignerCanvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        double old = _zoom;
        double factor = e.Delta > 0 ? 1.1 : 1.0 / 1.1;
        double nz = Math.Max(0.3, Math.Min(2.5, old * factor));
        if (nz != old)
        {
            var m = e.GetPosition(CanvasScroller);
            _zoom = nz;
            _scale.ScaleX = _scale.ScaleY = nz;
            double ratio = nz / old;
            CanvasScroller.ScrollToHorizontalOffset((CanvasScroller.ContentHorizontalOffset + m.X) * ratio - m.X);
            CanvasScroller.ScrollToVerticalOffset((CanvasScroller.ContentVerticalOffset + m.Y) * ratio - m.Y);
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
