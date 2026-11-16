// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>SoftServo WMX3（软件 EtherCAT 主站）。</summary>
    public sealed class SoftServoModule : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "SoftServo",
            SortOrder = 2,
            DisplayName = "SoftServo WMX3（软件 EtherCAT 主站）",
            Vendor = "",
            BusKind = "总线",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.SoftServo",
            // WMX3 是托管程序集（x64），不是原生 dll；列在这里是为了让探测能识别出它已随程序输出。
            NativeDlls = new[] { "CoreMotionApi_CLRLib.dll", "EcApi_CLRLib.dll", "EventApi_CLRLib.dll", "IOApi_CLRLib.dll", "LTDMC.dll", "MCCE135.dll", "RtMotionApiNET.dll", "WMX3Api_CLRLib.dll" },
            BusTypes = new[] { CardBusType.EtherCAT },
            Aliases = new[] { "SoftServo", "WMX3", "软伺服" },
            Note = "主驱动为 WMX3Api_CLRLib.dll 等一组托管程序集（x64，需先装 WMX3 运行环境）；EtherCAT 主站复位 / 错误码读取沿用了研控 MCCE135.dll 与雷赛 LTDMC.dll 的调用，故一并列出。",
        };
    }
}
