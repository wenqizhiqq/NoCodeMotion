using System.Windows.Controls;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    public partial class AxisControllerPage : UserControl
    {
        public AxisControllerPage()
        {
            InitializeComponent();
            DataContext = new AxisControllerViewModel();
        }
    }
}
