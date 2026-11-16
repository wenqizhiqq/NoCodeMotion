// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>升立德 SLD1232（PCIe 脉冲卡）。</summary>
    public sealed class SLD1232Module : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "SLD1232",
            SortOrder = 2,
            DisplayName = "升立德 SLD1232（PCIe 脉冲卡）",
            Vendor = "升立德",
            BusKind = "脉冲",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.SLD1232",
            NativeDlls = new[] { "LTDMC.dll", "PCI1230.dll" },
            BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
            Aliases = new[] { "SLD1232", "1232", "0x1232", "4658" },
            Note = "PCI1230.dll。支持扩展 IO 模块。",
        };
    }
}
