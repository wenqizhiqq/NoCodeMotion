using System.Windows.Controls;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    public partial class CylinderPage : UserControl
    {
        public CylinderPage()
        {
            InitializeComponent();
            DataContext = new CylinderViewModel();
        }
    }
}
