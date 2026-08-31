// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░💡💡
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
using System.Windows;
using System.Windows.Controls;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 切换用户弹窗：列出全部用户，选中后确定即切换当前登录用户。
    /// ShowDialog() 返回 true 且 SelectedName 非空表示已切换。
    /// </summary>
    public partial class SwitchUserDialog : Window
    {
        public string? SelectedName { get; private set; }

        public SwitchUserDialog()
        {
            InitializeComponent();
            Owner = Application.Current?.MainWindow;
            UserList.ItemsSource = UserStore.Users;
            // 默认选中当前登录用户
            if (UserStore.Current != null)
                UserList.SelectedItem = UserStore.Current;
        }

        private void UserList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UserList.SelectedItem is AppUser u)
                SelectedName = u.Name;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserList.SelectedItem is AppUser u && !string.IsNullOrWhiteSpace(u.Name))
            {
                SelectedName = u.Name;
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
// ◇作者保留所有权利　请勿删除※
