using System.Windows.Controls;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    public partial class IoPage : UserControl
    {
        public IoPage()
        {
            InitializeComponent();
            DataContext = new IoViewModel();
        }
    }
}
