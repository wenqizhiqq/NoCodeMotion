using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.ViewModels
{
    public class CylinderViewModel : ListEditorViewModel<CylinderItem>
    {
        public CylinderViewModel()
        {
            CatalogCategory = "Cylinder";
            Items = ProjectStore.Data.Cylinders;
            Counter = Items.Count;
            AttachAutoSave();
        }

        protected override CylinderItem CreateNewItem() => new CylinderItem { Name = $"气缸{Counter + 1}" };
    }
}
