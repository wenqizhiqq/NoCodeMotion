// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‎启⁠志‌◆‏编‍写⁠◇‌微⁠信⁣﹕‍1​8​7⁠◆⁠1⁠9‌3‍6‌◇‍1‎3‏9‍9⁠　⁠※⁣保​留⁠所⁣有⁠权‎利‍请⁣勿​删‌除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Windows;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// Apple 风格操作员使用说明书弹窗：透明窗口 + 圆角白卡 + 滚动正文。
    /// </summary>
    public partial class OperatorManualDialog : Window
    {
        public OperatorManualDialog()
        {
            InitializeComponent();
            Owner = Application.Current?.MainWindow;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
