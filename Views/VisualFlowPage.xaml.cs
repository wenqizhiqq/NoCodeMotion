using System.Windows.Controls;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 视觉流程详情页。DataContext 由 FlowPage 注入为 VisualFlowDetailViewModel（资源），
    /// 该 VM 的 Steps/Name 已通过 BindingProxy 绑到主选中 FlowItem 的 VisualSteps/Name，
    /// 所以本页的步骤增删直接作用于主流程项；不再自带独立 VM 与左侧列表。
    /// </summary>
    public partial class VisualFlowPage : UserControl
    {
        public VisualFlowPage()
        {
            InitializeComponent();
        }
    }
}
