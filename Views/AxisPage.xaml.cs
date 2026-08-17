using System.Windows.Controls;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    public partial class AxisPage : UserControl
    {
        public AxisPage()
        {
            InitializeComponent();
            DataContext = new AxisViewModel();
        }
    }
}
