// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>雷赛 DMC1000S（脉冲卡）。</summary>
    public sealed class DMC1000SModule : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "DMC1000S",
            SortOrder = 3,
            DisplayName = "雷赛 DMC1000S（脉冲卡）",
            Vendor = "雷赛",
            BusKind = "脉冲",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.DMC1000S",
            NativeDlls = new[] { "Dmc1000.dll", "LTDMC.dll" },
            BusTypes = new[] { CardBusType.Pulse },
            Aliases = new[] { "DMC1000S", "DMC1000", "1000S", "0x2711", "10001" },
            Note = "主驱动 Dmc1000.dll，另有一个高速比较器配置调用 LTDMC.dll。（该目录下的 dmc1000SConfig.cs 仍在旧命名空间 NoCodeMotion.Services.Hardware.Cards.Families.DMC1000S，与实现类不同。）",
        };
    }
}
