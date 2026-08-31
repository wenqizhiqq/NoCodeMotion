// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
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
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
