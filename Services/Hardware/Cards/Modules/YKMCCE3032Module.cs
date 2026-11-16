// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>研控 MCCE3032 / MCCE332（EtherCAT 总线主站）。</summary>
    public sealed class YKMCCE3032Module : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "YKMCCE3032",
            SortOrder = 7,
            DisplayName = "研控 MCCE3032 / MCCE332（EtherCAT 总线主站）",
            Vendor = "研控",
            BusKind = "总线",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.YKMCCE3032",
            NativeDlls = new[] { "MCCE135.dll" },
            BusTypes = new[] { CardBusType.EtherCAT, CardBusType.CANopen },
            Aliases = new[] { "YKMCCE3032", "YKMCC_E3032", "MCCE3032", "MCCE332", "0x7E2", "2018" },
            Note = "MCCE135.dll。",
        };
    }
}
