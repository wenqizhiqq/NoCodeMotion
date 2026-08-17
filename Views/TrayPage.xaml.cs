using System.Windows.Controls;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    public partial class TrayPage : UserControl
    {
        public TrayPage()
        {
            InitializeComponent();
            DataContext = new TrayViewModel();
        }
    }
}
