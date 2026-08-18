using System.Windows;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// Apple 风格自定义确认弹窗：透明窗口 + 圆角白卡 + 柔和阴影 + 取消/主操作按钮。
    /// ShowDialog() 返回 true=主操作，false=取消。
    /// </summary>
    public partial class ConfirmDialog : Window
    {
        public ConfirmDialog(string title, string message, string confirmText = "是")
        {
            InitializeComponent();
            TitleText.Text = title;
            MessageText.Text = message;
            ConfirmButton.Content = confirmText;
            Owner = Application.Current?.MainWindow;
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
