// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
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

        private void ProjectGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ProjectManagerViewModel vm && vm.OpenCommand.CanExecute(null))
                vm.OpenCommand.Execute(null);
        }

        private void ProjectGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header?.ToString() == "备注" &&
                e.Row.Item is ProjectEntry entry &&
                DataContext is ProjectManagerViewModel vm)
            {
                vm.PersistRemark(entry);
            }
        }
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
