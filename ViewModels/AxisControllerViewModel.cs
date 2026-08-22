using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>轴控制器页面：增删改控制器实例，供轴页面选择归属。</summary>
    public class AxisControllerViewModel : ListEditorViewModel<AxisControllerItem>, IEnsureDefaultSelection
    {
        public AxisControllerViewModel()
        {
            CatalogCategory = "Controller";
            Items = ProjectStore.Data.Controllers;
            Counter = Items.Count;
            AttachAutoSave();
        }

        protected override AxisControllerItem CreateNewItem() => new AxisControllerItem { Name = $"控制器{Counter + 1}" };

        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0) SelectedItem = Items[0];
        }
    }
}
