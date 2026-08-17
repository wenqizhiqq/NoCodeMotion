using NoCodeMotion.Models;

namespace NoCodeMotion.ViewModels
{
    public class CommViewModel : ListEditorViewModel<CommItem>
    {
        protected override CommItem CreateNewItem() => new CommItem { Name = $"通讯{Counter + 1}" };
    }
}
