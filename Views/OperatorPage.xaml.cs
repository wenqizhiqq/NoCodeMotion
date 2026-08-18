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
        }
    }
}
