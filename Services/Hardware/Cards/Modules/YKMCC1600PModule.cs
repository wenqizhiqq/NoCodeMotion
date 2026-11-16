// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>研控 MCC1600P（脉冲卡）。</summary>
    public sealed class YKMCC1600PModule : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "YKMCC1600P",
            SortOrder = 3,
            DisplayName = "研控 MCC1600P（脉冲卡）",
            Vendor = "研控",
            BusKind = "脉冲",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.YKMCC1600P",
            NativeDlls = new[] { "MCC.dll" },
            BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
            Aliases = new[] { "YKMCC1600P", "MCC1600P", "1600P", "0xF00", "3840" },
            Note = "MCC.dll（参考工程未随附该库，需向厂商索取后放入程序目录）。支持扩展 IO 模块。",
        };
    }
}
