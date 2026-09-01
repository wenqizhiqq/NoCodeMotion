// ◆◇※▣▤▥▦▧▨▩░💡💡
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NoCodeMotion.Services;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 主窗口底部状态栏：显示工程名、当前用户（含切换/管理入口）、运行状态、异常情况。
    /// DataContext 为 StatusBarViewModel，绑定 StatusBarService 全局状态。
    /// 异常文本可点击 → 弹窗显示完整内容（状态栏空间有限会截断）。
    /// </summary>
    public partial class StatusBarView : UserControl
    {
        public StatusBarView()
        {
            InitializeComponent();
            DataContext = new StatusBarViewModel();
        }

        private void SwitchUser_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dlg = new SwitchUserDialog();
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.SelectedName))
            {
                UserStore.SetCurrent(dlg.SelectedName);
                StatusBarService.RefreshUser();
            }
        }

        private void UserManagement_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dlg = new UserManagementDialog();
            dlg.ShowDialog();
            // 用户在管理弹窗中可能切换/删除，关闭后刷新用户名
            StatusBarService.RefreshUser();
        }

        private void ClearException_Click(object sender, System.Windows.RoutedEventArgs e)
            => StatusBarService.ClearException();

        /// <summary>
        /// 点击错误胶囊 → 弹窗显示完整异常文本。
        /// 用 MouseLeftButtonUp 而非 Down，避免拖选文本（万一以后允许选）时被误触打开。
        /// 当前文本是只读展示，不会拖选，但保留 Down→Up 的区分更稳。
        /// </summary>
        private void ShowException_Click(object sender, MouseButtonEventArgs e)
        {
            var full = StatusBarService.ExceptionText;
            if (string.IsNullOrEmpty(full)) return;
            var dlg = new ExceptionDetailDialog
            {
                Owner = Window.GetWindow(this),
                Message = full
            };
            dlg.ShowDialog();
        }
    }
}
// ◇作者保留所有权利　请勿删除※
