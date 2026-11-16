// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>雷赛 DMC2210（脉冲卡）。</summary>
    public sealed class DMC2210Module : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "DMC2210",
            SortOrder = 4,
            DisplayName = "雷赛 DMC2210（脉冲卡）",
            Vendor = "雷赛",
            BusKind = "脉冲",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.DMC2210",
            NativeDlls = new[] { "Dmc2210.dll" },
            BusTypes = new[] { CardBusType.Pulse },
            Aliases = new[] { "DMC2210", "2210" },
            // ★ 参考工程 bin2 里的 Dmc2210.dll 是 32 位（x86），64 位宿主加载会失败。
            Note = "Dmc2210.dll（参考工程自带的这份是 32 位，64 位程序加载不了，需换成 x64 版）。",
        };
    }
}
