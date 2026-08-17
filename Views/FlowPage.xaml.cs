using System.Windows.Controls;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    public partial class FlowPage : UserControl
    {
        public FlowPage()
        {
            InitializeComponent();
            DataContext = new FlowViewModel();
        }
    }
}
