using NoCodeMotion.Models;

namespace NoCodeMotion.ViewModels
{
    public class IoViewModel : ListEditorViewModel<IoItem>
    {
        protected override IoItem CreateNewItem() => new IoItem { Name = $"点位{Counter + 1}" };
    }
}
