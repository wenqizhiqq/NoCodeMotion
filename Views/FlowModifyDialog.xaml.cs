// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Windows;
using NoCodeMotion.Models;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// Apple 风格「修改流程」弹窗：同时编辑流程名称与主流程/复位流程角色。
    /// ShowDialog() 返回 true 且 ResultName 非空即表示已应用修改。
    /// </summary>
    public partial class FlowModifyDialog : Window
    {
        public string? ResultName { get; private set; }
        public FlowRole ResultRole { get; private set; } = FlowRole.Main;

        public FlowModifyDialog(string title, string currentName, FlowRole currentRole)
        {
            InitializeComponent();
            TitleText.Text = title;
            NameBox.Text = currentName;
            MainRadio.IsChecked = currentRole == FlowRole.Main;
            ResetRadio.IsChecked = currentRole == FlowRole.Reset;
            Owner = Application.Current?.MainWindow;
            Loaded += (_, __) =>
            {
                NameBox.Focus();
                NameBox.SelectAll();
            };
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            var name = NameBox.Text?.Trim();
            if (string.IsNullOrEmpty(name)) return; // 空名不允许，等同于取消
            ResultName = name;
            ResultRole = ResetRadio.IsChecked == true ? FlowRole.Reset : FlowRole.Main;
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
