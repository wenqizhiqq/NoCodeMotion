using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.ViewModels
{
    public class CommViewModel : ListEditorViewModel<CommItem>, IEnsureDefaultSelection
    {
        public CommViewModel()
        {
            CatalogCategory = "Comm";
            Items = ProjectStore.Data.Comms;
            Counter = Items.Count;
            AttachAutoSave();
        }

        protected override CommItem CreateNewItem() => new CommItem { Name = $"通讯{Counter + 1}" };
    
        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0)
                SelectedItem = Items[0];
        }
}
}
