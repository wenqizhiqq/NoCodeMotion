// ◆◇※▣▤▥▦▧▨▩░💡💡
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
using System.Windows.Controls;
using NoCodeMotion.Services;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 主窗口底部状态栏：显示工程名、当前用户（含切换/管理入口）、运行状态、异常情况。
    /// DataContext 为 StatusBarViewModel，绑定 StatusBarService 全局状态。
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
    }
}
// ◇作者保留所有权利　请勿删除※
