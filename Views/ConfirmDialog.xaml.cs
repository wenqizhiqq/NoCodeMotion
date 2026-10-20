// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‍启​志⁠◆‌编⁠写‍◇​微‌信‏﹕‍1‏8⁣7⁣◆‎1⁣9‏3⁠6⁣◇‍1‌3‌9‏9‎　⁠※‏保‍留‌所‍有​权‍利‌请‍勿‌删‌除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Windows;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// Apple 风格自定义确认弹窗：透明窗口 + 圆角白卡 + 柔和阴影 + 取消/主操作按钮。
    /// ShowDialog() 返回 true=主操作，false=取消。
    /// </summary>
    public partial class ConfirmDialog : Window
    {
        /// <summary>点了第三颗「备选」按钮才为 true；没传 altText 时恒为 false。</summary>
        public bool AltChose { get; private set; }

        /// <param name="altText">第三颗按钮（橙色）的文字；留空 = 不显示，保持原来的「取消 / 主操作」两颗。</param>
        public ConfirmDialog(string title, string message, string confirmText = "是", string? altText = null)
        {
            InitializeComponent();
            TitleText.Text = title;
            MessageText.Text = message;
            ConfirmButton.Content = confirmText;
            if (!string.IsNullOrEmpty(altText))
            {
                AltButton.Content = altText;
                AltButton.Visibility = Visibility.Visible;
            }
            Owner = Application.Current?.MainWindow;
        }

        private void AltButton_Click(object sender, RoutedEventArgs e)
        {
            AltChose = true;
            // 备选不等于「确定」，所以 DialogResult 仍给 false，由调用方读 AltChose 分流。
            DialogResult = false;
            Close();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
