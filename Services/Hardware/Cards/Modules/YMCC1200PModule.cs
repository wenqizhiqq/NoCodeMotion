// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>研控 MCC1200P（脉冲卡，实现类命名空间为 YKMCC1200P）。</summary>
    public sealed class YMCC1200PModule : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "YMCC1200P",
            SortOrder = 4,
            DisplayName = "研控 MCC1200P（脉冲卡）",
            Vendor = "研控",
            BusKind = "脉冲",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.YMCC1200P",
            NativeDlls = new[] { "MCC.dll" },
            BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
            Aliases = new[] { "YMCC1200P", "YKMCC1200P", "MCC1200P", "1200P" },
            Note = "MCC.dll（参考工程未随附该库，需向厂商索取后放入程序目录）。支持扩展 IO 模块。",
        };
    }
}
