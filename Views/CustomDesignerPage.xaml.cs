// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
// ◆温​启‍志​◆⁠编‍写‍◇‌微‌信‍﹕‌1⁠8‌7​◆‌1‍9​3‌6‌◇‌1‌3⁠9‌9‌　⁠※‌保‌留‍所‍有‌权‍利⁠请‍勿‌删‌除‍◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 自定义页面（可视化设计器）：
    /// 左侧工具箱（拖控件）→ 中间画布（拖位置 / 右下角手柄拖大小）→ 右侧属性面板（配动作）。
    /// 设计模式只摆布局；运行模式按钮 / 输入框生效，显示框由 250ms 定时器读回真实值。
    /// </summary>
    public partial class CustomDesignerPage : UserControl
    {
        private readonly DispatcherTimer _live = new() { Interval = TimeSpan.FromMilliseconds(250) };

        // 拖动 / 缩放状态（同一时刻只有一个控件在被拖）
        private DesignerWidgetVM? _drag;
        private bool _dragMove, _dragSize;
        private Point _start;
        private double _startX, _startY, _startW, _startH;

        private CustomDesignerViewModel? VM => DataContext as CustomDesignerViewModel;

        public CustomDesignerPage()
        {
            InitializeComponent();
            DataContext = new CustomDesignerViewModel();

            // 显示框实时读回（页面可见时才跑，Unloaded 停止——页面被 MainWindow 缓存）
            _live.Tick += (_, _) => VM?.RefreshLive();
            Loaded += (_, _) => _live.Start();
            Unloaded += (_, _) => _live.Stop();

            // Delete 删除选中控件（正在输入框里编辑时不拦截）
            PreviewKeyDown += (_, e) =>
            {
                if (e.Key != Key.Delete) return;
                if (Keyboard.FocusedElement is TextBox) return;
                if (VM != null && VM.IsDesign && VM.SelectedWidget != null)
                {
                    VM.DeleteSelected();
                    e.Handled = true;
                }
            };
        }

        // ===== 工具箱拖拽 =====

        private void ToolboxItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (sender is FrameworkElement { Tag: string type } fe && !string.IsNullOrEmpty(type))
                DragDrop.DoDragDrop(fe, new DataObject("designer-widget-type", type), DragDropEffects.Copy);
        }

        private void Canvas_Drop(object sender, DragEventArgs e)
        {
            var vm = VM;
            if (vm == null || !vm.IsDesign) return;
            if (!e.Data.GetDataPresent("designer-widget-type")) return;
            var type = e.Data.GetData("designer-widget-type") as string;
            if (string.IsNullOrEmpty(type)) return;

            var p = e.GetPosition(DesignerCanvas);
            // 让控件以鼠标点为中心放置，位置取整避免拖出 0.5px 的模糊边界
            double x = System.Math.Max(0, System.Math.Round(p.X - 65));
            double y = System.Math.Max(0, System.Math.Round(p.Y - 18));
            vm.AddWidget(type, x, y);
        }

        // ===== 控件拖动 / 缩放 =====

        private void Widget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var vm = VM;
            if (vm == null || sender is not FrameworkElement fe) return;
            if (fe.DataContext is not DesignerWidgetVM wvm) return;

            vm.SelectedWidget = wvm;              // 点击即选中（运行模式也允许选中看属性）
            if (!vm.IsDesign) return;             // 运行模式不拖拽，按钮 / 输入框自己接管

            _drag = wvm; _dragMove = true; _dragSize = false;
            _start = e.GetPosition(DesignerCanvas);
            _startX = wvm.Model.X; _startY = wvm.Model.Y;
            fe.CaptureMouse();
            e.Handled = true;
        }

        private void Grip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var vm = VM;
            if (vm == null || !vm.IsDesign) return;
            if (sender is not FrameworkElement fe) return;
            if (fe.DataContext is not DesignerWidgetVM wvm) return;

            vm.SelectedWidget = wvm;
            _drag = wvm; _dragSize = true; _dragMove = false;
            _start = e.GetPosition(DesignerCanvas);
            _startW = wvm.Model.Width; _startH = wvm.Model.Height;
            fe.CaptureMouse();
            e.Handled = true;
        }

        private void Widget_MouseMove(object sender, MouseEventArgs e)
        {
            if (_drag == null) return;
            var vm = VM;
            if (vm == null || !vm.IsDesign) return;
            var p = e.GetPosition(DesignerCanvas);

            if (_dragMove)
            {
                _drag.Model.X = System.Math.Max(0, System.Math.Round(_startX + p.X - _start.X));
                _drag.Model.Y = System.Math.Max(0, System.Math.Round(_startY + p.Y - _start.Y));
            }
            else if (_dragSize)
            {
                _drag.Model.Width = System.Math.Max(40, System.Math.Round(_startW + p.X - _start.X));
                _drag.Model.Height = System.Math.Max(24, System.Math.Round(_startH + p.Y - _start.Y));
            }
        }

        private void Widget_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe) fe.ReleaseMouseCapture();
            _drag = null;
            _dragMove = _dragSize = false;
        }

        // ===== 运行时：按钮 / 输入框 =====

        private void WidgetButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is DesignerWidgetVM wvm)
                VM?.Execute(wvm.Model);
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            CommitInput(sender);
            e.Handled = true;
        }

        private void InputBox_LostFocus(object sender, RoutedEventArgs e) => CommitInput(sender);

        private void CommitInput(object sender)
        {
            if (sender is TextBox tb && tb.DataContext is DesignerWidgetVM wvm)
                VM?.CommitInput(wvm.Model, tb.Text);
        }
    }

    /// <summary>字符串相等 → Visible（否则 Collapsed）。用于按控件类型切换模板内容。</summary>
    public class StringEqualsToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => string.Equals(value as string, parameter as string, StringComparison.Ordinal)
                ? Visibility.Visible
                : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>true（设计模式）→ 参数指定的像素列宽；false（运行模式）→ 0。用于收起工具箱 / 属性列。</summary>
    public class BoolToColWidthConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var design = value is bool b && b;
            if (!design) return new GridLength(0);
            var w = 170d;
            if (parameter != null && double.TryParse(parameter.ToString(), out var p) && p > 0) w = p;
            return new GridLength(w);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>null → Collapsed（否则 Visible）；ConverterParameter="inv" 反转：null → Visible（否则 Collapsed）。</summary>
    public class NullToVisConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var isNull = value == null;
            var inv = string.Equals(parameter as string, "inv", StringComparison.OrdinalIgnoreCase);
            return (isNull ^ inv) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
