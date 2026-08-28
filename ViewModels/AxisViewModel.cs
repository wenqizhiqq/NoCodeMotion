// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.ViewModels
{
    public class AxisViewModel : ListEditorViewModel<AxisItem>, IEnsureDefaultSelection
    {
        public AxisViewModel()
        {
            CatalogCategory = "Axis";
            Items = ProjectStore.Data.Axes;
            Counter = Items.Count;
            AttachAutoSave();
        }

        protected override AxisItem CreateNewItem() => new AxisItem { Name = $"轴{Counter + 1}" };

        /// <summary>配置页改值实时下发设备：速度变化下发到卡，使能/电平变化重新使能轴。</summary>
        protected override void PushItem(AxisItem item, string? propertyName)
        {
            var bridge = HardwareBridge.Current;
            if (propertyName == nameof(AxisItem.Speed))
                bridge.SetAxisSpeed(item, item.Speed);
            else if (propertyName == nameof(AxisItem.Enabled) || propertyName == nameof(AxisItem.EnableLevel))
                bridge.EnableAxis(item);
        }

        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0) SelectedItem = Items[0];
        }
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
