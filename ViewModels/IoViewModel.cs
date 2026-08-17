using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.ViewModels
{
    public class IoViewModel : ListEditorViewModel<IoItem>
    {
        public IoViewModel()
        {
            CatalogCategory = "Io";
            Items = ProjectStore.Data.Io;
            Counter = Items.Count;
            AttachAutoSave();
        }

        protected override IoItem CreateNewItem() => new IoItem { Name = $"点位{Counter + 1}" };
    }
}
