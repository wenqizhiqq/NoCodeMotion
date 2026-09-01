// === NoCodeMotion 视觉流程页 | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NoCodeMotion.Models;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 视觉流程详情页。自身持有 VisualFlowDetailViewModel 作为 DataContext。
    /// 注意：不能在 _vm（非树 DependencyObject）上设 RelativeSource Binding——
    /// RelativeSource 祖先解析要求 target 在可视树中，VM 不在树里，绑定会静默失败，
    /// _vm.Steps 永远为 null，AddStepCommand 因 null 检查直接 return → "添加步骤点了没反应"。
    /// 正确做法：Loaded 后用 VisualTreeHelper 找到 FlowPage 祖先，订阅其 DataContext（FlowViewModel）
    /// 的 SelectedItem PropertyChanged，手动同步 _vm.Steps/Name 到当前选中的 FlowItem。
    /// </summary>
    public partial class VisualFlowPage : UserControl
    {
        private readonly VisualFlowDetailViewModel _vm = new VisualFlowDetailViewModel();
        private FlowPage? _flowPage;
        private PropertyChangedEventHandler? _fvmHandler;
        private NotifyCollectionChangedEventHandler? _matchResultsHandler;
        private int _projRetry;   // 布局时序重试计数，避免无限重排

        public VisualFlowPage()
        {
            InitializeComponent();
            DataContext = _vm;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            // 布局尺寸变化时同步重算匹配框的屏幕坐标
            ImageHost.SizeChanged += (_, _) => ProjectOverlayBoxes();
            // VM 的 ResultImage / MatchResults 变化也要重新投影
            _vm.PropertyChanged += OnVmPropertyChanged;
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // ResultImage 变化意味着源图宽高变了，所有 OverlayBox 都得按新图重算
            if (e.PropertyName == nameof(VisualFlowDetailViewModel.ResultImage))
            {
                ProjectOverlayBoxes();
            }
            else if (e.PropertyName == nameof(VisualFlowDetailViewModel.MatchResults))
            {
                // 订阅新集合的 CollectionChanged（增删/重置），以便后续引擎追加结果时同步
                HookMatchResultsCollection(_vm.MatchResults);
                ProjectOverlayBoxes();
            }
        }

        private void HookMatchResultsCollection(ObservableCollection<MatchBox>? col)
        {
            if (_matchResultsHandler != null && _vm.MatchResults != null)
            {
                _vm.MatchResults.CollectionChanged -= _matchResultsHandler;
            }
            _matchResultsHandler = (_, _) => ProjectOverlayBoxes();
            if (col != null) col.CollectionChanged += _matchResultsHandler;
        }

        /// <summary>
        /// 按当前 ResultImage 的像素尺寸 + ImageHost 的实际显示尺寸，把 MatchBox 投影为屏幕坐标 OverlayBox。
        /// 公式与 Image.Stretch=Uniform 完全一致：scale = min(hostW/srcW, hostH/srcH)，
        /// 偏移 = (hostW - srcW*scale)/2, (hostH - srcH*scale)/2。
        /// OverlayBoxes 喂给叠加层 ItemsControl 直接使用 Canvas.Left/Top/Width/Height（屏幕像素），
        /// 不再依赖外层 RenderTransform，避免 PropertyChanged / 布局时序 race。
        /// </summary>
        private void ProjectOverlayBoxes()
        {
            // 直接读 VM 上的 ResultImage（DP，同步可取），不依赖 ResultImageView.Source 的绑定传播时机，
            // 避免「MatchResults 已就绪但 Image 控件 Source 还没刷新」导致拿不到像素尺寸。
            var src = _vm.ResultImage as BitmapSource;
            var matches = _vm.MatchResults;
            if (matches == null || matches.Count == 0)
            {
                _vm.OverlayBoxes = null;
                return;
            }
            // 图像尚未解码完成（PixelWidth=0）或宿主尚未完成布局（ActualWidth=0）时，
            // 立刻投影会拿到错误尺寸而放弃绘制。用一个 Render 优先级的延迟重试兜底，
            // 避免「首次匹配绿框不出现」的布局时序 race。
            if (src == null || src.PixelWidth <= 0 || src.PixelHeight <= 0
                || ImageHost.ActualWidth <= 0 || ImageHost.ActualHeight <= 0)
            {
                // 最多重试 30 次（约数帧内必完成布局），超出则放弃，避免极端情况下空转
                if (_projRetry++ < 30)
                {
                    ImageHost.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Render,
                        (Action)ProjectOverlayBoxes);
                }
                return;
            }
            _projRetry = 0;
            double hostW = ImageHost.ActualWidth, hostH = ImageHost.ActualHeight;

            double scale = System.Math.Min(hostW / src.PixelWidth, hostH / src.PixelHeight);
            double dispW = src.PixelWidth * scale, dispH = src.PixelHeight * scale;
            double offsetX = (hostW - dispW) / 2.0, offsetY = (hostH - dispH) / 2.0;

            _vm.OverlayBoxes = new ObservableCollection<OverlayBox>(
                matches.Select(m => OverlayBox.Project(m, scale, offsetX, offsetY)));
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 先解绑旧订阅（防止 Loaded 二次触发或同实例复用时重复）
            DetachFlowViewModel();

            // 沿可视树向上找 FlowPage 祖先（视觉页嵌在 FlowPage 的 ContentControl.Detail 链中）
            DependencyObject? cur = this;
            while (cur != null)
            {
                cur = VisualTreeHelper.GetParent(cur);
                if (cur is FlowPage fp) { _flowPage = fp; break; }
            }
            if (_flowPage == null) return;

            // 监听 FlowPage.DataContext（FlowViewModel）的 SelectedItem 变化，
            // 把选中 FlowItem 的 VisualSteps/Name 同步到 VM；用户切换流程或新建步骤会随之刷新。
            _fvmHandler = (s, args) =>
            {
                if (args.PropertyName == nameof(FlowViewModel.SelectedItem)) ApplySelection();
            };
            if (_flowPage.DataContext is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged += _fvmHandler;
            }
            ApplySelection();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => DetachFlowViewModel();

        private void DetachFlowViewModel()
        {
            if (_flowPage?.DataContext is INotifyPropertyChanged inpc && _fvmHandler != null)
            {
                inpc.PropertyChanged -= _fvmHandler;
            }
            if (_vm.MatchResults != null && _matchResultsHandler != null)
            {
                _vm.MatchResults.CollectionChanged -= _matchResultsHandler;
            }
            _fvmHandler = null;
        }

        private void ApplySelection()
        {
            if (_flowPage?.DataContext is FlowViewModel fvm)
            {
                var sel = fvm.SelectedItem;
                _vm.Steps = sel?.VisualSteps;
                _vm.Name = sel?.Name;
                // 关键：进入视觉流程时若还没选中任何步骤，自动选第一个；
                // 否则 SelectedStep==null 永远不变 → IsImageAcquisition 等标志全是 false → 所有参数卡都隐藏
                if (_vm.Steps != null && _vm.Steps.Count > 0 && _vm.SelectedStep == null)
                    _vm.SelectedStep = _vm.Steps[0];
            }
        }

        // ===================== 结果图上拖拽画框，确定模板区域 =====================

        private bool _dragging;
        private Point _dragStart;

        private void ImageHost_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ResultImageView.Source == null) return;
            _dragStart = e.GetPosition(ImageHost);
            _dragging = true;
            UpdateRoiRect(_dragStart, _dragStart);
            RoiRect.Visibility = Visibility.Visible;
            ImageHost.CaptureMouse();
            e.Handled = true;
        }

        private void ImageHost_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            UpdateRoiRect(_dragStart, e.GetPosition(ImageHost));
            e.Handled = true;
        }

        private void ImageHost_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_dragging) return;
            _dragging = false;
            ImageHost.ReleaseMouseCapture();
            var end = e.GetPosition(ImageHost);
            ApplyTemplateRoi(_dragStart, end);
            e.Handled = true;
        }

        /// <summary>更新拖拽中矩形的屏幕位置/尺寸。</summary>
        private void UpdateRoiRect(Point a, Point b)
        {
            double x = System.Math.Min(a.X, b.X), y = System.Math.Min(a.Y, b.Y);
            Canvas.SetLeft(RoiRect, x);
            Canvas.SetTop(RoiRect, y);
            RoiRect.Width = System.Math.Abs(a.X - b.X);
            RoiRect.Height = System.Math.Abs(a.Y - b.Y);
        }

        /// <summary>
        /// 把屏幕框选矩形换算成原图像素坐标并写入当前步骤。
        /// Image 用 Stretch=Uniform（等比缩放 + 居中），需按缩放比和居中偏移反算。
        /// </summary>
        private void ApplyTemplateRoi(Point a, Point b)
        {
            if (DataContext is not VisualFlowDetailViewModel vm) return;
            var step = vm.SelectedStep;
            if (step == null) return;

            if (ResultImageView.Source is not BitmapSource src || src.PixelWidth <= 0 || src.PixelHeight <= 0)
            {
                RoiRect.Visibility = Visibility.Collapsed;
                return;
            }

            double hostW = ImageHost.ActualWidth, hostH = ImageHost.ActualHeight;
            if (hostW <= 0 || hostH <= 0) { RoiRect.Visibility = Visibility.Collapsed; return; }

            double scale = System.Math.Min(hostW / src.PixelWidth, hostH / src.PixelHeight);
            double dispW = src.PixelWidth * scale, dispH = src.PixelHeight * scale;
            double offsetX = (hostW - dispW) / 2.0, offsetY = (hostH - dispH) / 2.0;

            double x1 = System.Math.Max(0, (System.Math.Min(a.X, b.X) - offsetX) / scale);
            double y1 = System.Math.Max(0, (System.Math.Min(a.Y, b.Y) - offsetY) / scale);
            double x2 = System.Math.Min(src.PixelWidth, (System.Math.Max(a.X, b.X) - offsetX) / scale);
            double y2 = System.Math.Min(src.PixelHeight, (System.Math.Max(a.Y, b.Y) - offsetY) / scale);

            int w = (int)System.Math.Round(x2 - x1), h = (int)System.Math.Round(y2 - y1);
            if (w < 8 || h < 8)   // 太小的框视为误操作
            {
                RoiRect.Visibility = Visibility.Collapsed;
                vm.RunStatus = "框选区域太小（至少 8×8 像素），已忽略";
                return;
            }

            step.TemplateRoiX = (int)System.Math.Round(x1);
            step.TemplateRoiY = (int)System.Math.Round(y1);
            step.TemplateRoiW = w;
            step.TemplateRoiH = h;
            vm.RunStatus = $"已框选模板区域：({step.TemplateRoiX},{step.TemplateRoiY}) {w}×{h}　点「开启匹配」执行";
            // 自动保存：用户松手即把 ROI 区域裁剪成模板图保存到 Templates/，并回写到 step.TemplatePath。
            // 这样「确定模板」按钮就退化成"用户已经看到对了，再点一下二次确认"的作用，无需手动。
            if (vm.ConfirmTemplateCommand.CanExecute(null))
            {
                vm.ConfirmTemplateCommand.Execute(null);
            }
        }
    }
}
// === NoCodeMotion 视觉流程页 | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===