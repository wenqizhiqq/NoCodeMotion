using NoCodeMotion.Models;

namespace NoCodeMotion.ViewModels
{
    public class CylinderViewModel : ListEditorViewModel<CylinderItem>
    {
        protected override CylinderItem CreateNewItem() => new CylinderItem { Name = $"气缸{Counter + 1}" };
    }
}
