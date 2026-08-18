using System.Windows.Controls;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    public partial class PointPage : UserControl
    {
        public PointPage()
        {
            InitializeComponent();
            DataContext = new PointViewModel();
        }
    }
}
