using NoCodeMotion.ViewModels;
using System.Windows.Controls;

namespace NoCodeMotion.Views
{
    public partial class VariablePage : UserControl
    {
        public VariablePage()
        {
            InitializeComponent();
            DataContext = new VariableViewModel();
        }
    }
}