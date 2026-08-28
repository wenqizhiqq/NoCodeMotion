// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
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
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
