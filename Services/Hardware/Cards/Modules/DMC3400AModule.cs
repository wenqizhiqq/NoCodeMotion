// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>雷赛 DMC3400A（脉冲卡，支持 CAN 扩展 IO）。</summary>
    public sealed class DMC3400AModule : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "DMC3400A",
            SortOrder = 2,
            DisplayName = "雷赛 DMC3400A（脉冲卡）",
            Vendor = "雷赛",
            BusKind = "脉冲",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.DMC3400A",
            NativeDlls = new[] { "LTDMC.dll" },
            BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
            // 13312 = 0x3400 是 dmc_get_CardInfList 上报的固件卡型，自动识别会写成 "0x3400"
            Aliases = new[] { "DMC3400", "DMC3800", "DMC3000", "0x3400", "0x3800", "3400", "3800", "3000系列", "0x3C00", "13312", "14336", "15360" },
            Note = "LTDMC.dll。支持 CAN 扩展 IO 模块（DMC_CAN_In*Out*）。",
        };
    }
}
