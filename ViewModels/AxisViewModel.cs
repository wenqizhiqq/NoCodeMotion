using NoCodeMotion.Models;

namespace NoCodeMotion.ViewModels
{
    public class AxisViewModel : ListEditorViewModel<AxisItem>
    {
        protected override AxisItem CreateNewItem() => new AxisItem { Name = $"轴{Counter + 1}" };
    }
}
