// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>数字孪生卡（仿真运动卡，另带 Modbus/TCP）。</summary>
    public sealed class DigitalTwinCardModule : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "DigitalTwinCard",
            SortOrder = 2,
            DisplayName = "数字孪生卡（仿真运动卡）",
            Vendor = "模拟卡",
            BusKind = "虚拟",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.DigitalTwinCard",
            NativeDlls = new string[0],
            BusTypes = new[] { CardBusType.Pulse, CardBusType.ModbusTcp, CardBusType.Other },
            Aliases = new[] { "数字孪生", "仿真运动卡", "仿真", "DigitalTwin", "0x877F8", "555000" },
            Note = "轴 / IO 走内存仿真；另带 ModubsComm（Modbus/TCP）可接外部仿真模型。",
        };
    }
}
