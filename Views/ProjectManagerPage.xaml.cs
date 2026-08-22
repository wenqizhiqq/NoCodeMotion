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