// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‏启⁣志⁣◆‎编⁠写‎◇‏微⁠信‎﹕​1‎8⁠7‌◆⁣1⁠9⁣3⁠6‌◇⁠1‍3‌9‍9⁣　⁣※‍保‏留​所‍有‎权⁠利‏请‌勿‏删⁣除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Windows;
using System.Windows.Controls;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    public partial class OperatorPage : UserControl
    {
        public OperatorPage()
        {
            InitializeComponent();
            DataContext = new OperatorViewModel();
            // 仅当本页可见时才跑 33ms 仿真循环；导航到其它页/最小化时停止，省出 UI 线程给其它页面。
            if (DataContext is OperatorViewModel vm)
            {
                IsVisibleChanged += (_, __) => vm.SetSimVisible(IsVisible);
                vm.SetSimVisible(IsVisible);
            }
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
