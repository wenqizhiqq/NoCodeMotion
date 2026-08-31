// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░💡💡
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
using System.Windows;
using System.Windows.Controls;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 用户管理弹窗：新增 / 重命名 / 删除用户，并设置角色（管理员 / 操作员）。
    /// </summary>
    public partial class UserManagementDialog : Window
    {
        private bool _suppressRoleSync;

        public UserManagementDialog()
        {
            InitializeComponent();
            Owner = Application.Current?.MainWindow;
            UserList.ItemsSource = UserStore.Users;
            if (UserStore.Current != null)
                UserList.SelectedItem = UserStore.Current;
            RefreshState();
        }

        private void UserList_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshState();

        private void RefreshState()
        {
            _suppressRoleSync = true;
            if (UserList.SelectedItem is AppUser u)
            {
                RoleCombo.SelectedIndex = u.Role == "管理员" ? 0 : 1;
                DeleteButton.IsEnabled = UserStore.Users.Count > 1 && (UserStore.Current == null || UserStore.Current.Name != u.Name);
                RenameButton.IsEnabled = true;
            }
            else
            {
                RoleCombo.SelectedIndex = 1;
                DeleteButton.IsEnabled = false;
                RenameButton.IsEnabled = false;
            }
            _suppressRoleSync = false;
        }

        private void RoleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressRoleSync) return;
            if (UserList.SelectedItem is AppUser u)
                UserStore.SetRole(u.Name, RoleCombo.Text ?? "操作员");
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new RenameDialog("新增用户", "", "新增");
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.ResultName))
            {
                if (!UserStore.Add(dlg.ResultName, "操作员"))
                    Hint.Text = $"新增失败：用户「{dlg.ResultName}」已存在或名称无效。";
                else
                    Hint.Text = $"已新增用户「{dlg.ResultName}」。";
                UserList.Items.Refresh();
                RefreshState();
            }
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserList.SelectedItem is not AppUser u) return;
            var dlg = new RenameDialog("重命名用户", u.Name, "重命名");
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.ResultName))
            {
                if (!UserStore.Rename(u.Name, dlg.ResultName))
                    Hint.Text = $"重命名失败：名称已存在或无效。";
                else
                    Hint.Text = $"已重命名为「{dlg.ResultName}」。";
                UserList.Items.Refresh();
                RefreshState();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserList.SelectedItem is not AppUser u) return;
            if (!UserStore.Remove(u.Name))
            {
                Hint.Text = "删除失败：至少保留 1 个用户，且不能删除当前登录用户。";
                return;
            }
            Hint.Text = $"已删除用户「{u.Name}」。";
            UserList.Items.Refresh();
            RefreshState();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
// ◇作者保留所有权利　请勿删除※
