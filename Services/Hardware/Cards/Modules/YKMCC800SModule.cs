// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>研控 MCC800S（总线卡）。</summary>
    public sealed class YKMCC800SModule : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "YKMCC800S",
            SortOrder = 6,
            DisplayName = "研控 MCC800S（总线卡）",
            Vendor = "研控",
            BusKind = "总线",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.YKMCC800S",
            NativeDlls = new[] { "MCC800S.dll" },
            BusTypes = new[] { CardBusType.EtherCAT, CardBusType.CANopen, CardBusType.Pulse },
            Aliases = new[] { "YKMCC800S", "MCC800S", "800S", "0x8076", "32886" },
            Note = "MCC800S.dll。支持扩展 IO 模块。固件码 0x8076（参考实现里 400S 也写成这个码，按 0x80xx=800 系列归本族）。",
        };
    }
}
