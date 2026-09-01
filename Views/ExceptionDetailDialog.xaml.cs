// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇⁣
using System;
using System.Windows;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 错误详情弹窗：显示 StatusBarService 完整异常文本（状态栏空间有限会截断，
    /// 这里展示原文，并支持选中/复制）。通过 Message 传入文本后 ShowDialog。
    /// </summary>
    public partial class ExceptionDetailDialog : Window
    {
        /// <summary>完整异常文本（含状态栏加的时间戳前缀）。</summary>
        public string Message
        {
            get => MessageView.Text;
            set => MessageView.Text = value ?? string.Empty;
        }

        public ExceptionDetailDialog()
        {
            InitializeComponent();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e) => SafeCopy();

        private void SafeCopy()
        {
            try
            {
                if (string.IsNullOrEmpty(MessageView.Text)) return;
                // 用 SetDataObject(text, true) 让剪贴板内容在程序退出后仍可用
                Clipboard.SetDataObject(MessageView.Text, true);
                CopyButton.Content = "已复制";
                CopyButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                // 全限定 System.Windows.MessageBox：避免与 XAML 里 x:Name="MessageBox" 的控件同名冲突
                System.Windows.MessageBox.Show(this, $"复制失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
// ◇作者保留所有权利　请勿删除※⁣