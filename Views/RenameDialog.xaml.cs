// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Windows;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// Apple 风格重命名弹窗：输入新名称，确定/取消。
    /// ShowDialog() 返回 true 且 ResultName 非空即表示已重命名。
    /// </summary>
    public partial class RenameDialog : Window
    {
        public string? ResultName { get; private set; }

        public RenameDialog(string title, string currentName, string confirmText = "确定")
        {
            InitializeComponent();
            TitleText.Text = title;
            NameBox.Text = currentName;
            ConfirmButton.Content = confirmText;
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
