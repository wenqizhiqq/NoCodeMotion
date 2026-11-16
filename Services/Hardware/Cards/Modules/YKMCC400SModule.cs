// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>研控 MCC400S（总线卡）。</summary>
    public sealed class YKMCC400SModule : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "YKMCC400S",
            SortOrder = 5,
            DisplayName = "研控 MCC400S（总线卡）",
            Vendor = "研控",
            BusKind = "总线",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.YKMCC400S",
            NativeDlls = new[] { "MCC800S.dll" },
            BusTypes = new[] { CardBusType.EtherCAT, CardBusType.CANopen, CardBusType.Pulse },
            Aliases = new[] { "YKMCC400S", "MCC400S", "400S" },
            // 参考实现把 MCC400S 与 MCC800S 都上报成固件码 0x8076（它自己的复制粘贴 bug），
            // 无法据此区分；按 0x4040=400P / 0x8040=800P 的规律，0x80xx 属 800 系列，
            // 所以 0x8076 归给了 MCC800S，400S 必须按型号名填写。
            Note = "MCC800S.dll。支持扩展 IO 模块。固件码 0x8076 无法与 MCC800S 区分，本族不登记该码。",
        };
    }
}
