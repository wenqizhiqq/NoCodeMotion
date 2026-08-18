using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.ViewModels
{
    public class TrayViewModel : ListEditorViewModel<TrayItem>, IEnsureDefaultSelection
    {
        public TrayViewModel()
        {
            Items = ProjectStore.Data.Trays;
            Counter = Items.Count;
            AttachAutoSave();
        }

        protected override TrayItem CreateNewItem() => new TrayItem { Name = $"料盘{Counter + 1}" };
    
        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0)
                SelectedItem = Items[0];
        }
}
}
