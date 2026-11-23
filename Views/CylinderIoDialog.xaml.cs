// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‏启​志⁠◆⁠编⁣写⁣◇‎微⁠信⁠﹕⁠1‍8⁠7⁠◆⁣1‌9⁣3‎6‍◇‎1‍3‍9‏9⁣　‏※‎保‍留‎所⁠有⁠权‎利‍请‏勿‎删⁣除⁣◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Windows;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 自动生成气缸 IO 的弹窗：输入气缸名称和生成数量。
    /// ShowDialog() 返回 true 且 ResultName 非空时，按 <see cref="ResultName"/> / <see cref="ResultCount"/> 生成。
    /// </summary>
    public partial class CylinderIoDialog : Window
    {
        /// <summary>气缸名称（可带编号，如 气缸1）。</summary>
        public string ResultName { get; private set; } = "";

        /// <summary>要生成几个气缸（每个气缸 2 个输入 + 2 个输出）。</summary>
        public int ResultCount { get; private set; } = 1;

        public CylinderIoDialog(string suggestedName)
        {
            InitializeComponent();
            NameBox.Text = string.IsNullOrWhiteSpace(suggestedName) ? "气缸1" : suggestedName;
            Owner = Application.Current?.MainWindow;
            Loaded += (_, __) =>
            {
                NameBox.Focus();
                NameBox.SelectAll();
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var name = NameBox.Text?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                NameBox.Focus();
                return;
            }

            int count = 1;
            if (int.TryParse(CountBox.Text?.Trim(), out int n) && n > 0) count = n;
            if (count > 50) count = 50;            // 一次别生成太多，误点也不会灌满表格

            ResultName = name;
            ResultCount = count;
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
