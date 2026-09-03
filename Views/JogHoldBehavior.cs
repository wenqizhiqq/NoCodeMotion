// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
namespace NoCodeMotion.Behaviors
{
    using System.Windows;
    using System.Windows.Input;

    /// <summary>
    /// JOG 按住行为：按下按钮触发 <see cref="StartCommand"/>（连续运动），
    /// 松开鼠标 / 失去捕获触发 <see cref="StopCommand"/>（发送停止命令）。
    /// 用于轴控 JOG：按住连续运动，鼠标抬起停止。
    /// </summary>
    public static class JogHoldBehavior
    {
        public static readonly DependencyProperty StartCommandProperty =
            DependencyProperty.RegisterAttached(
                "StartCommand", typeof(ICommand), typeof(JogHoldBehavior),
                new PropertyMetadata(null, Attach));

        public static readonly DependencyProperty StartParameterProperty =
            DependencyProperty.RegisterAttached(
                "StartParameter", typeof(object), typeof(JogHoldBehavior),
                new PropertyMetadata(null));

        public static readonly DependencyProperty StopCommandProperty =
            DependencyProperty.RegisterAttached(
                "StopCommand", typeof(ICommand), typeof(JogHoldBehavior),
                new PropertyMetadata(null));

        public static ICommand GetStartCommand(DependencyObject o) => (ICommand)o.GetValue(StartCommandProperty);
        public static void SetStartCommand(DependencyObject o, ICommand v) => o.SetValue(StartCommandProperty, v);

        public static object GetStartParameter(DependencyObject o) => o.GetValue(StartParameterProperty);
        public static void SetStartParameter(DependencyObject o, object v) => o.SetValue(StartParameterProperty, v);

        public static ICommand GetStopCommand(DependencyObject o) => (ICommand)o.GetValue(StopCommandProperty);
        public static void SetStopCommand(DependencyObject o, ICommand v) => o.SetValue(StopCommandProperty, v);

        private static void Attach(DependencyObject d, DependencyPropertyChangedEventArgs _)
        {
            if (d is UIElement el)
            {
                el.PreviewMouseDown += OnPreviewMouseDown;
                el.PreviewMouseUp += OnPreviewMouseUp;
                el.LostMouseCapture += OnLostCapture;
            }
        }

        private static void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            var el = (UIElement)sender;
            var cmd = GetStartCommand(el);
            var p = GetStartParameter(el);
            if (cmd?.CanExecute(p) == true) cmd.Execute(p);
            el.CaptureMouse();
        }

        private static void OnPreviewMouseUp(object sender, MouseButtonEventArgs _)
        {
            Stop((UIElement)sender);
        }

        private static void OnLostCapture(object sender, MouseEventArgs _)
        {
            Stop((UIElement)sender);
        }

        private static void Stop(UIElement el)
        {
            if (el.IsMouseCaptured) el.ReleaseMouseCapture();
            var stop = GetStopCommand(el);
            if (stop?.CanExecute(null) == true) stop.Execute(null);
        }
    }
}
// ◇作者保留所有权利　请勿删除※