// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>升立德 PCI9014（4 轴脉冲卡）。</summary>
    public sealed class PCI9014Module : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "PCI9014",
            SortOrder = 4,
            DisplayName = "升立德 PCI9014（4 轴脉冲卡）",
            Vendor = "升立德",
            BusKind = "脉冲",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.PCI9014",
            NativeDlls = new[] { "Pci9014.dll", "SLD9014PTP.dll" },
            BusTypes = new[] { CardBusType.Pulse },
            Aliases = new[] { "PCI9014", "9014", "0x2336" },
            // 该卡族源码里还 DllImport 了 SLD9014PTP.dll（参考工程未提供），只声明不调用时才不影响。
            Note = "Pci9014.dll（源码另引 SLD9014PTP.dll，参考工程未随附，未走到就不影响）。",
        };
    }
}
