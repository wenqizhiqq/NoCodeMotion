// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>恒昱 MC608（脉冲卡）。</summary>
    public sealed class HYMC608Module : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "HYMC608",
            SortOrder = 2,
            DisplayName = "恒昱 MC608（脉冲卡）",
            Vendor = "恒昱",
            BusKind = "脉冲",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.HYMC608",
            NativeDlls = new[] { "LTDMC.dll", "MCX08.dll" },
            BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
            Aliases = new[] { "HYMC608", "MC608", "MCX08", "0x608", "1544" },
            Note = "MCX08.dll。支持扩展 IO 模块。",
        };
    }
}
