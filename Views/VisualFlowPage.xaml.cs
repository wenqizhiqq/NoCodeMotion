// === NoCodeMotion 视觉流程页 | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 视觉流程详情页。自身持有 VisualFlowDetailViewModel 作为 DataContext；
    /// 在 Loaded 时通过 RelativeSource 找到父级 FlowPage，把 VM 的 Steps / Name
    /// 绑到选中流程项的 VisualSteps / Name，使步骤增删与运行结果直接落进主流程项。
    /// </summary>
    public partial class VisualFlowPage : UserControl
    {
        private readonly VisualFlowDetailViewModel _vm = new VisualFlowDetailViewModel();

        public VisualFlowPage()
        {
            InitializeComponent();
            DataContext = _vm;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var ancestor = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(FlowPage), 1);
            BindingOperations.SetBinding(_vm, VisualFlowDetailViewModel.StepsProperty,
                new Binding("SelectedItem.VisualSteps") { RelativeSource = ancestor, Mode = BindingMode.OneWay });
            BindingOperations.SetBinding(_vm, VisualFlowDetailViewModel.NameProperty,
                new Binding("SelectedItem.Name") { RelativeSource = ancestor, Mode = BindingMode.OneWay });
        }
    }
}
// === NoCodeMotion 视觉流程页 | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
