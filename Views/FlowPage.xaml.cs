// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 流程页：左侧列表管理“流程”项目，右侧按选中流程的 Kind 切换表格步骤 / Lua 脚本 / 视觉流程。
    /// Lua 脚本的完整编辑器（AvalonEdit + 断点边栏 + 智能提示 + 单步/步入/步出/运行/暂停/停止 + 变量树 + 调用栈 + 输出 + 诊断）
    /// 由 <see cref="LuaEditorView"/> 承载（直接复用 LuaStudio 的代码）。
    /// 视觉流程编辑器（<see cref="VisualFlowPage"/>）改为延迟实例化：仅当 IsKindVision 为真时才创建，
    /// 且用 try/catch 兜住其加载异常，确保即便视觉页有问题，表格 / Lua 页也始终正常显示、互不影响。
    /// </summary>
    public partial class FlowPage : UserControl
    {
        /// <summary>运行时承载视觉流程页（或异常提示文本）的 UI 元素。由 EnsureVision 写入，
        /// XAML 中 Border.Child 通过 RelativeSource 绑定到它，避免给 Border 命名触发 MC3093
        /// （Border 位于 EditorPage.Detail 内容内，命名会双重注册到两个 NameScope）。</summary>
        public static readonly DependencyProperty VisionContentProperty =
            DependencyProperty.Register(
                nameof(VisionContent),
                typeof(UIElement),
                typeof(FlowPage),
                new PropertyMetadata(null));

        public UIElement? VisionContent
        {
            get => (UIElement?)GetValue(VisionContentProperty);
            set => SetValue(VisionContentProperty, value);
        }

        /// <summary>运行时承载节点图流程页（或异常提示文本）。镜像 VisionContent 的延迟实例化模式。</summary>
        public static readonly DependencyProperty NodeGraphContentProperty =
            DependencyProperty.Register(
                nameof(NodeGraphContent),
                typeof(UIElement),
                typeof(FlowPage),
                new PropertyMetadata(null));

        public UIElement? NodeGraphContent
        {
            get => (UIElement?)GetValue(NodeGraphContentProperty);
            set => SetValue(NodeGraphContentProperty, value);
        }

        public FlowPage()
        {
            InitializeComponent();
            var vm = new FlowViewModel();
            DataContext = vm;
            vm.PropertyChanged += OnVmPropertyChanged;
            // 入场时若当前选中项已是视觉/节点图流程，立即建好对应页。
            if (vm.IsKindVision) EnsureVision();
            if (vm.IsKindNodeGraph) EnsureNodeGraph();
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FlowViewModel.IsKindVision)
                && ((FlowViewModel)DataContext).IsKindVision)
            {
                EnsureVision();
            }
            else if (e.PropertyName == nameof(FlowViewModel.IsKindNodeGraph)
                && ((FlowViewModel)DataContext).IsKindNodeGraph)
            {
                EnsureNodeGraph();
            }
        }

        /// <summary>
        /// 延迟创建视觉流程页。已创建则直接返回。
        /// 若其加载抛异常，显示一个提示文本而不是崩溃整页——表格 / Lua 页不受影响。
        /// 视觉页通过 VisionContent 依赖属性承载（绑定到 XAML 中 Border.Child），不依赖任何 x:Name。
        /// </summary>
        private void EnsureVision()
        {
            if (VisionContent != null) return;
            try
            {
                VisionContent = new VisualFlowPage();
            }
            catch (Exception ex)
            {
                VisionContent = new TextBlock
                {
                    Text = "视觉流程页加载失败：" + ex.Message,
                    Foreground = System.Windows.Media.Brushes.OrangeRed,
                    Margin = new Thickness(16),
                    TextWrapping = TextWrapping.Wrap
                };
            }
        }

        /// <summary>
        /// 延迟创建节点图流程页。已创建则直接返回。若其加载抛异常，显示提示文本而非崩溃整页。
        /// 节点图页通过 NodeGraphContent 依赖属性承载（绑定到 XAML 中 Border.Child），不依赖任何 x:Name，避免 MC3093。
        /// </summary>
        private void EnsureNodeGraph()
        {
            if (NodeGraphContent != null) return;
            try
            {
                NodeGraphContent = new NodeGraphPage();
            }
            catch (Exception ex)
            {
                NodeGraphContent = new TextBlock
                {
                    Text = "节点图流程页加载失败：" + ex.Message,
                    Foreground = System.Windows.Media.Brushes.OrangeRed,
                    Margin = new Thickness(16),
                    TextWrapping = TextWrapping.Wrap
                };
            }
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
