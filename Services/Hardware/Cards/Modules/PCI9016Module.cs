// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>升立德 PCI9016（脉冲卡）。</summary>
    public sealed class PCI9016Module : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "PCI9016",
            SortOrder = 5,
            DisplayName = "升立德 PCI9016（脉冲卡）",
            Vendor = "升立德",
            BusKind = "脉冲",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.PCI9016",
            NativeDlls = new[] { "Pci9016.dll" },
            BusTypes = new[] { CardBusType.Pulse },
            Aliases = new[] { "PCI9016", "9016", "0x2338" },
            Note = "Pci9016.dll。",
        };
    }
}
