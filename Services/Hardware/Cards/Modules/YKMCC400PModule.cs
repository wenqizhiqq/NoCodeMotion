// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>研控 MCC400P（脉冲卡）。</summary>
    public sealed class YKMCC400PModule : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "YKMCC400P",
            SortOrder = 1,
            DisplayName = "研控 MCC400P（脉冲卡）",
            Vendor = "研控",
            BusKind = "脉冲",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.YKMCC400P",
            NativeDlls = new[] { "MCC.dll" },
            BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
            Aliases = new[] { "YKMCC400P", "MCC400P", "400P", "0x4040", "16448" },
            // ★ MCC.dll 参考工程 bin2 里没有，属缺失库（与 MCC1600P / MCC800P 共用）。
            Note = "MCC.dll（参考工程未随附该库，需向厂商索取后放入程序目录）。支持扩展 IO 模块。",
        };
    }
}
