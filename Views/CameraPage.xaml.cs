using System.Windows.Controls;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 相机页。直接以 CameraViewModel 充当 DataContext，EditorPage 通过 Items/SelectedItem/
    /// AddCommand/DeleteCommand/RenameCommand 五个契约自动接管左侧增删。
    /// </summary>
    public partial class CameraPage : UserControl
    {
        public CameraPage()
        {
            InitializeComponent();
            DataContext = new CameraViewModel();
        }
    }
}
