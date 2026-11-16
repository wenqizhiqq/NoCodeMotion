// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>雷赛 DMC-E3000 / 5000（EtherCAT 总线主站）。</summary>
    public sealed class DMC_E3032_EtherCATModule : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "DMC_E3032_EtherCAT",
            SortOrder = 1,
            DisplayName = "雷赛 DMC-E3000 / 5000（EtherCAT 总线主站）",
            Vendor = "雷赛",
            BusKind = "总线",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.DMC_E3032_EtherCAT",
            NativeDlls = new[] { "LTDMC.dll" },
            BusTypes = new[] { CardBusType.EtherCAT, CardBusType.CANopen, CardBusType.Pulse },
            Aliases = new[] { "DMC_E3032", "DMC-E3032", "E3032", "DMC-E3000", "DMC-E5000", "DMC5000", "E3000", "0x80013032", "2147561522" },
            Note = "LTDMC.dll。轴运动走 dmc_ 族；伺服使能 / 回零 / 总线 IO 走 nmc_ 族（CiA402 状态机）。",
        };
    }
}
