// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>升立德 E64 系列 IO 卡（E6432 / E6416 / E6364 / E6316 / E6564 / E6516）。</summary>
    public sealed class E64IOSeriesModule : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "E64IOSeries",
            SortOrder = 3,
            DisplayName = "升立德 E64 系列 IO 卡（E6432 / E6416 / E6364 / E6316 / E6564 / E6516）",
            Vendor = "升立德",
            BusKind = "IO 卡",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.E64IOSeries",
            NativeDlls = new[] { "LTDMC.dll", "sldmv.dll" },
            BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
            Aliases = new[] { "E6432", "E6416", "E6364", "E6316", "E6564", "E6516", "SLDIOE64", "E64", "0xE6416", "0xE6432", "943126", "943154" },
            Note = "sldmv.dll。纯 IO 扩展卡（轴能力有限），扩展 IO 走 CAN / EtherCAT 模块。",
        };
    }
}
