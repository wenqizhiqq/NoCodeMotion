using NoCodeMotion.Models;

namespace NoCodeMotion.ViewModels
{
    public class FlowViewModel : ListEditorViewModel<FlowItem>
    {
        protected override FlowItem CreateNewItem() => new FlowItem { Name = $"步骤{Counter + 1}" };
    }
}
