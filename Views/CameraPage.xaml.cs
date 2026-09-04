// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// 相机页代码后端：订阅 SimRuntime.Changed + 周期刷新闪光指示。
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NoCodeMotion.Services;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 相机页。直接以 CameraViewModel 充当 DataContext，EditorPage 通过 Items/SelectedItem/
    /// AddCommand/DeleteCommand/RenameCommand 五个契约自动接管左侧增删。
    /// 订阅 SimRuntime.Changed 并周期刷新「闪光指示」，使取像瞬间亮绿后自动转灰。
    /// </summary>
    public partial class CameraPage : UserControl
    {
        private Border? _camFlashDot;
        private readonly System.Windows.Threading.DispatcherTimer _flashTimer =
            new() { Interval = System.TimeSpan.FromMilliseconds(250) };

        public CameraPage()
        {
            InitializeComponent();
            DataContext = new CameraViewModel();
            SimRuntime.Changed += OnSimChanged;
            Loaded += (_, _) =>
            {
                _camFlashDot = FindVisualChildByTag<Border>(this, "CamFlashDot");
                _flashTimer.Start();
            };
            Unloaded += (_, _) =>
            {
                _flashTimer.Stop();
                SimRuntime.Changed -= OnSimChanged;
            };
            _flashTimer.Tick += (_, _) => RefreshFlash();
        }

        private void OnSimChanged() => Dispatcher.Invoke(RefreshFlash);

        private void RefreshFlash()
        {
            // 重算闪光画刷：取像后 1.2s 内亮绿，否则灰色（CamFlashBrushConverter 按闪光时刻判定）。
            _camFlashDot?.GetBindingExpression(Border.BackgroundProperty)?.UpdateTarget();
        }

        private static T? FindVisualChildByTag<T>(DependencyObject root, string tag) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T t && child is FrameworkElement fe && fe.Tag as string == tag)
                    return t;
                var inner = FindVisualChildByTag<T>(child, tag);
                if (inner != null) return inner;
            }
            return null;
        }
    }
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣ۤ▦▧▨▩ᑒ▓✦✧⚝☢☣➤◈❖◆◇※▣ۤ
