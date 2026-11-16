// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>虚拟运动卡（脱机调试）。</summary>
    public sealed class VirtualMotionCardModule : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "VirtualMotionCard",
            SortOrder = 1,
            DisplayName = "虚拟运动卡（脱机调试）",
            Vendor = "模拟卡",
            BusKind = "虚拟",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.VirtualMotionCard",
            NativeDlls = new[] { "LTDMC.dll" },
            BusTypes = new[] { CardBusType.Pulse, CardBusType.EtherCAT, CardBusType.Other },
            Aliases = new[] { "虚拟运动卡", "虚拟控制卡", "虚拟", "VirtualMotionCard", "0x1B198", "111000" },
            Note = "主路径纯内存仿真，不碰硬件；仅「多轴停止 / 急停」两个接口转发 LTDMC.dll。用于先写流程后接设备。",
        };
    }
}
