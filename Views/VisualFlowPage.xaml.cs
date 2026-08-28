// === NoCodeMotion 视觉流程页 | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        public VisualFlowPage()
        {
            InitializeComponent();
            DataContext = _vm;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
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
            _fvmHandler = null;
        }

        private void ApplySelection()
        {
            if (_flowPage?.DataContext is FlowViewModel fvm)
            {
                var sel = fvm.SelectedItem;
                _vm.Steps = sel?.VisualSteps;
                _vm.Name = sel?.Name;
            }
        }
    }
}
// === NoCodeMotion 视觉流程页 | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===