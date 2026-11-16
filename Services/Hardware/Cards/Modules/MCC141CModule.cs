// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>研控 MCC141C（脉冲卡）。</summary>
    public sealed class MCC141CModule : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "MCC141C",
            SortOrder = 9,
            DisplayName = "研控 MCC141C（脉冲卡）",
            // 品牌依据：① 参考实现的分组规则「StartsWith("MCC") 且不含 E → 研控·脉冲」命中；
            //          ② 它的底层包装类命名空间是 csMCC1C00，与研控其余卡（csMCC / csMCC800S /
            //             csMCC1600 / csMCCE135）同一命名族。
            //          保留说明：它没有出现在参考实现的两个卡型枚举里（SystemSupportMotionCardEnum /
            //           AllMotionCardEnum），所以参考实现的卡树里本来也看不到它 —— 归属是推断出来的。
            Vendor = "研控",
            BusKind = "脉冲",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.MCC141C",
            NativeDlls = new[] { "MCC1C00.dll" },
            BusTypes = new[] { CardBusType.Pulse },
            Aliases = new[] { "MCC141C", "141C", "MCC1C00" },
            Note = "MCC1C00.dll。品牌按参考实现规则推断为研控（该型号未出现在参考实现的卡型枚举里）。",
        };
    }
}
