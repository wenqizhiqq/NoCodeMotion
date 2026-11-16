// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>升立德 RT3316（脉冲卡）。</summary>
    public sealed class RT3316Module : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "RT3316",
            SortOrder = 6,
            DisplayName = "升立德 RT3316（脉冲卡）",
            Vendor = "升立德",
            BusKind = "脉冲",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.RT3316",
            NativeDlls = new[] { "Pci9014.dll" },
            BusTypes = new[] { CardBusType.Pulse },
            Aliases = new[] { "RT3316", "3316" },
            Note = "复用 Pci9014.dll（与 PCI9014 同族）。",
        };
    }
}
