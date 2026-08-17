using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.ViewModels
{
    public class AxisViewModel : ListEditorViewModel<AxisItem>
    {
        public AxisViewModel()
        {
            CatalogCategory = "Axis";
            Items = ProjectStore.Data.Axes;
            Counter = Items.Count;
            AttachAutoSave();
        }

        protected override AxisItem CreateNewItem() => new AxisItem { Name = $"轴{Counter + 1}" };
    }
}
