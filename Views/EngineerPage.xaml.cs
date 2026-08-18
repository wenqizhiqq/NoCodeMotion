using System.Windows.Controls;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    public partial class EngineerPage : UserControl
    {
        public EngineerPage()
        {
            InitializeComponent();
            DataContext = new EngineerViewModel();
        }
    }
}
