// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>恒昱 HY7X00 系列（HY7400 / HY7600 / HY7800 / HY7C00）。</summary>
    public sealed class HY7X00Module : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "HY7X00",
            SortOrder = 1,
            DisplayName = "恒昱 HY7X00 系列（HY7400 / HY7600 / HY7800 / HY7C00）",
            Vendor = "恒昱",
            BusKind = "脉冲",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.HY7X00",
            NativeDlls = new[] { "LTDMC.dll", "PCI400.dll" },
            BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
            Aliases = new[] { "HY7X00", "HY7400", "HY7600", "HY7800", "HY7C00", "0x7400", "0x7600", "0x7800", "0x7C00", "29696", "30208", "30720", "31744" },
            Note = "主驱动 PCI400.dll（恒昱 7X00 与 PCI400 同一套 API），另有 1 个高速比较器配置调用 LTDMC.dll。支持 HY_CAN_In20Out20 扩展模块。",
        };
    }
}
