using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        // 进入编辑模式时自动展开下拉框（一次点击即可选择）。
        // 用 Dispatcher 延迟到本次点击完成后打开，避免被同一次鼠标抬起误判为“外部点击”而关闭。
        // 模板列的根可能是 hc:ComboBox，也可能是包含 ComboBox 的容器。
        private void StepsGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (FindChild<ComboBox>(e.EditingElement) is { } comboBox)
            {
                comboBox.Dispatcher.BeginInvoke(new Action(() => comboBox.IsDropDownOpen = true),
                    System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        // 单击单元格即进入编辑模式。关键：先把点击的单元格设为当前单元格，
        // 否则 BeginEdit 会编辑“旧当前单元格”，导致需要点两次才展开下拉框。
        private void StepsGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var cell = FindParent<DataGridCell>((DependencyObject)e.OriginalSource);
            if (cell != null && !cell.IsEditing && sender is DataGrid grid)
            {
                grid.CurrentCell = new DataGridCellInfo(cell);
                grid.BeginEdit();
            }
        }

        private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T t) return t;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private static T? FindChild<T>(DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null) return null;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;

                var found = FindChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }
    }
}
