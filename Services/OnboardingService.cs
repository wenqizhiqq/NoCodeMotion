// 新手引导（coach-mark / 分步高亮）服务。
// 触发点全部放在可改文件，完全绕开加密的 MainWindow.xaml.cs：
//   - NewProjectDialog.Confirm 在选「空白工程」模板时调用 MarkNewBlank()；
//   - 本服务通过 MainWindow.Instance.FindName("PageHost") 订阅 ContentControl.ContentChanged，
//     在新建空白工程后的首个页面呈现时启动引导；
//   - 顶部「引导」按钮（MainWindow.xaml 中 x:Name=BtnGuide）也在 Attach 内订阅 Click 支持重看。
// 引导以「一次高亮一个控件 + 气泡（下一步 / 跳过）」的方式，依次带用户走完落地页核心操作与顶部导航。
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using NoCodeMotion.Views;

namespace NoCodeMotion.Services
{
    public static class OnboardingService
    {
        private static MainWindow? _window;
        private static ContentControl? _host;
        private static bool _attached;
        private static bool _pendingNewBlank;
        private static Grid? _overlayRoot;
        private static GuideOverlay? _overlay;
        private static List<GuideStep>? _steps;
        private static int _stepIndex;
        private static bool _active;

        /// <summary>预挂接：订阅页面切换与「引导」按钮。MainWindow 可用后即可调用（App 启动 / 新建工程时）。</summary>
        public static void EnsureAttached()
        {
            if (_attached) return;
            var win = MainWindow.Instance;
            if (win == null) return;
            _window = win;
            _host = win.FindName("PageHost") as ContentControl;
            // ContentControl 没有 ContentChanged 事件，改用 DependencyPropertyDescriptor 观察 Content 属性变化
            if (_host != null)
            {
                var dpd = DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(ContentControl));
                dpd?.AddValueChanged(_host, OnContentChanged);
            }
            // 顶部「引导」按钮手动重看（其 Click 在加密 MainWindow.xaml.cs 之外订阅，不改动该文件）
            if (win.FindName("BtnGuide") is Button gb)
                gb.Click += (_, _) => StartTour();
            _attached = true;
        }

        /// <summary>由 NewProjectDialog.Confirm 在选中「空白工程」模板时调用：标记待引导并挂接。</summary>
        public static void MarkNewBlank()
        {
            _pendingNewBlank = true;
            EnsureAttached();
            // 兜底：若创建后并未发生页面切换（停留在当前页），延迟启动一次。
            _window?.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(FallbackStart));
        }

        private static void FallbackStart()
        {
            if (_pendingNewBlank && _host?.Content is FrameworkElement fe)
            {
                _pendingNewBlank = false;
                StartTour(fe);
            }
        }

        private static void OnContentChanged(object? sender, EventArgs e)
        {
            if (_pendingNewBlank)
            {
                if (_host?.Content is FrameworkElement fe)
                {
                    _pendingNewBlank = false;
                    StartTour(fe);
                }
                return;
            }
            // 引导进行中若用户自行切换页面，上下文已变，结束引导避免目标失效。
            if (_active) EndTour();
        }

        /// <summary>手动重看引导（顶部「引导」按钮调用）。</summary>
        public static void StartTour()
        {
            EnsureAttached();
            if (_host?.Content is FrameworkElement fe) StartTour(fe);
        }

        private static void StartTour(FrameworkElement page)
        {
            if (_active) return;
            _active = true;
            EnsureOverlayRoot();
            // 等页面 Loaded 再构建步骤，确保视觉树已成型、目标控件可被命中测试找到。
            _window?.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                _steps = BuildSteps(page);
                _stepIndex = 0;
                if (_steps == null || _steps.Count == 0) { EndTour(); return; }
                ShowCurrentStep();
            }));
        }

        private static List<GuideStep>? BuildSteps(FrameworkElement page)
        {
            var steps = new List<GuideStep>();
            var addBtn = FindByName(page, "AddBtn") as FrameworkElement;
            var listBox = FindByName(page, "ListBox") as FrameworkElement;
            var detail = FindByName(page, "DetailHost") as FrameworkElement;
            var navPanel = _window?.FindName("NavPanel") as FrameworkElement;

            // 落地页为 EditorPage 体系（含 添加/列表/详情）：分步引导核心操作
            if (addBtn != null || listBox != null)
            {
                if (addBtn != null)
                    steps.Add(new GuideStep(addBtn, "① 添加第一个配置项",
                        "点击「添加」，新建你的第一个配置项（例如一根轴、一个 IO 点）。新建后它会出现在左侧列表里。"));
                if (listBox != null)
                    steps.Add(new GuideStep(listBox, "② 在列表中选择",
                        "新项目会出现在左侧列表，点它选中，右侧即可编辑它的参数。"));
                if (detail != null)
                    steps.Add(new GuideStep(detail, "③ 填写参数",
                        "选中后在这里填写参数（名称、数值等）。底部有「操作说明 / 注意事项」提示，按提示填写即可。"));
            }

            // 顶部导航：引导切换到其它功能分类，并在最后提示到「操作员」运行
            if (navPanel != null)
                steps.Add(new GuideStep(navPanel, "④ 切换功能分类",
                    "顶部这些按钮切换不同功能页：轴 / IO / 气缸 / 点位 / 通讯 / 料盘 / 相机 / 变量 / 流程 / 操作员。配置完一类，点这里进入下一类继续添加。全部配置完成后，到「操作员」页启动运行。"));

            // 兜底：若既不是编辑页也找不到导航（极少见），至少用导航引导起步
            if (steps.Count == 0 && navPanel != null)
                steps.Add(new GuideStep(navPanel, "开始配置",
                    "点击顶部分类按钮（轴 / IO / 气缸 / 点位 / 通讯 / 料盘 / 相机 / 变量 / 流程）开始添加你的第一个配置项。"));

            return steps.Count > 0 ? steps : null;
        }

        private static void EnsureOverlayRoot()
        {
            if (_overlayRoot != null) return;
            if (_window?.Content is not Grid root) return;
            _overlayRoot = new Grid
            {
                Name = "GuideOverlayRoot",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = true
            };
            Grid.SetRowSpan(_overlayRoot, 4);
            Grid.SetColumnSpan(_overlayRoot, 1);
            Panel.SetZIndex(_overlayRoot, 1000);
            root.Children.Add(_overlayRoot);
            _overlayRoot.SizeChanged += (_, _) => Reposition();
        }

        private static void Reposition()
        {
            if (!_active || _overlay == null || _steps == null || _stepIndex >= _steps.Count) return;
            var t = _steps[_stepIndex].Target;
            if (t == null || _overlayRoot == null) return;
            var pt = t.TranslatePoint(new Point(0, 0), _overlayRoot);
            _overlay.Reposition(new Rect(pt.X, pt.Y, t.ActualWidth, t.ActualHeight),
                _overlayRoot.ActualWidth, _overlayRoot.ActualHeight);
        }

        private static void ShowCurrentStep()
        {
            if (!_active || _steps == null || _stepIndex >= _steps.Count) { EndTour(); return; }
            var step = _steps[_stepIndex];
            var target = step.Target;
            if (target == null || !target.IsVisible || target.ActualWidth == 0 || target.ActualHeight == 0)
            {
                // 目标当前不可用（隐藏/未布局），跳过该步
                _stepIndex++;
                if (_stepIndex < _steps.Count)
                    _window?.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(ShowCurrentStep));
                else EndTour();
                return;
            }
            _overlay ??= new GuideOverlay();
            if (_overlayRoot == null) { EndTour(); return; }
            _overlayRoot.Children.Clear();
            _overlayRoot.Children.Add(_overlay);
            _overlayRoot.Visibility = Visibility.Visible;
            var pt = target.TranslatePoint(new Point(0, 0), _overlayRoot);
            bool isLast = _stepIndex == _steps.Count - 1;
            _overlay.Show(new Rect(pt.X, pt.Y, target.ActualWidth, target.ActualHeight),
                step.Title, step.Text, isLast,
                _overlayRoot.ActualWidth, _overlayRoot.ActualHeight,
                onNext: () => { _stepIndex++; ShowCurrentStep(); },
                onSkip: EndTour);
        }

        private static void EndTour()
        {
            _active = false;
            _steps = null;
            if (_overlayRoot != null)
            {
                _overlayRoot.Visibility = Visibility.Collapsed;
                _overlayRoot.Children.Clear();
            }
        }

        /// <summary>递归在视觉树中按 Name 查找元素（跨 UserControl 名称域，例如 AxisPage 内含的 EditorPage）。</summary>
        private static DependencyObject? FindByName(DependencyObject root, string name)
        {
            if (root == null) return null;
            if (root is FrameworkElement fe && fe.Name == name) return root;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var found = FindByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private sealed class GuideStep
        {
            public FrameworkElement Target { get; }
            public string Title { get; }
            public string Text { get; }
            public GuideStep(FrameworkElement target, string title, string text)
            {
                Target = target;
                Title = title;
                Text = text;
            }
        }
    }
}
