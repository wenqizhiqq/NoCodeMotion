using System.Windows.Controls;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    public partial class CommPage : UserControl
    {
        public CommPage()
        {
            InitializeComponent();
            DataContext = new CommViewModel();
        }
    }
}
