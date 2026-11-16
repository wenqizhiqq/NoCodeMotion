// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>升立德 EC600（EtherCAT 总线主站）。</summary>
    public sealed class EC600Module : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "EC600",
            SortOrder = 7,
            DisplayName = "升立德 EC600（EtherCAT 总线主站）",
            // 品牌依据：参考实现的分组规则「StartsWith("E") → 升立德」命中，且它在
            // SystemSupportMotionCardEnum 里紧挨着升立德的 E64xx IO 卡区块
            // （… MCCE332, EC600, E6432, E6416, E6364 …），参考实现的卡树就是这么归的。
            Vendor = "升立德",
            BusKind = "总线",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.EC600",
            NativeDlls = new[] { "LTDMC.dll", "ecat_motion.dll" },
            BusTypes = new[] { CardBusType.EtherCAT, CardBusType.Pulse },
            Aliases = new[] { "EC600", "0xEC600", "968192" },
            Note = "ecat_motion.dll。品牌按参考实现规则归为升立德（它同时支持脉冲，属总线/脉冲双模）。",
        };
    }
}
