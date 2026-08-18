using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>变量页 ViewModel：维护变量列表、选中项，以及添加/删除（自动保存到工程 JSON）。</summary>
    public class VariableViewModel : ListEditorViewModel<VariableItem>
    {
        public VariableViewModel()
        {
            CatalogCategory = "Variable";
            Items = ProjectStore.Data.Variables;
            Counter = Items.Count;
            AttachAutoSave();
        }

        protected override VariableItem CreateNewItem() => new VariableItem { Name = $"变量{Counter + 1}" };
    }
}
