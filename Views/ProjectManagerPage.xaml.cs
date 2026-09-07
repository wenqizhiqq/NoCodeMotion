// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‏志‌◆⁣编⁣写⁣◇​微⁣信⁠﹕‍1‌8‌7‏◆⁣1‍9⁣3⁠6⁠◇‎1‌3‎9‍9‌　‌※⁠保⁣留⁠所⁠有‎权‎利​请​勿‌删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Windows.Controls;
using System.Windows.Input;
using NoCodeMotion.ViewModels;
using NoCodeMotion.Models;

namespace NoCodeMotion.Views
{
    public partial class ProjectManagerPage : UserControl
    {
        public ProjectManagerPage()
        {
            InitializeComponent();
            DataContext = new ProjectManagerViewModel();
        }

        /// <summary>双击工程列表项 → 打开该工程。</summary>
        private void ProjectList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ProjectManagerViewModel vm && vm.OpenCommand.CanExecute(null))
                vm.OpenCommand.Execute(null);
        }

        /// <summary>备注文本框失焦时自动保存到工程文件。</summary>
        private void Remark_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ProjectManagerViewModel vm && vm.SaveRemarkCommand.CanExecute(null))
                vm.SaveRemarkCommand.Execute(null);
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
