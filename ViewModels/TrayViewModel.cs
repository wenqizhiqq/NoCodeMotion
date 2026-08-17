using NoCodeMotion.Models;

namespace NoCodeMotion.ViewModels
{
    public class TrayViewModel : ListEditorViewModel<TrayItem>
    {
        protected override TrayItem CreateNewItem() => new TrayItem { Name = $"料盘{Counter + 1}" };
    }
}
