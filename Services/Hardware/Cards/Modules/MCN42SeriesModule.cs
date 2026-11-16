// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>研控 MCN42 系列（MCC42Series），固定 8 轴 / 16 入 / 16 出。</summary>
    public sealed class MCN42SeriesModule : CardFamilyModule
    {
        public override CardFamilyDescriptor Describe() => new CardFamilyDescriptor
        {
            Key = "MCN42Series",
            SortOrder = 8,
            DisplayName = "研控 MCN42 系列（MCC42Series）",
            Vendor = "研控",
            BusKind = "脉冲",
            Namespace = "NoCodeMotion.Services.Hardware.Cards.Families.MCN42Series",
            NativeDlls = new[] { "MCN420.dll" },
            BusTypes = new[] { CardBusType.Pulse },
            Aliases = new[] { "MCN42", "MCC42", "MCN420", "42系列", "0x1A4", "0x1A5" },
            Note = "MCN420.dll。固定规格：8 轴 / 16 入 / 16 出（底层实现离线返回 0、IO 默认填 24，需目录规格兜底）。"
                 + "轴号 0~7、IO 位号 0~15，均从 0 开始。★ 伺服使能（Sevon，占用通用输出 OUT32~OUT35）"
                 + "只对 0~3 轴有效，4~7 轴没有该功能，这几根轴请在驱动器侧处理使能信号。"
                 + "★ Sevon 的第三参是**端口电平**：使能脚低电平有效 → 写 0 = 使能、写 1 = 不使能。"
                 + "方向与雷赛 / 模拟卡的 dmc_write_sevon_pin（同样低电平有效，0 = 使能）一致；"
                 + "若真机实测方向相反，以实测为准并同步改 NmcSetCardAxisEnable / NmcSetCardAxisDisable。"
                 + "★ 本卡族指令全部是 unit 版，卡内靠「指令位置比率」YK_set_command_ratio（pulses/unit，即脉冲当量）"
                 + "做 unit↔脉冲 换算，所以每次连接与运动前都会把「轴」页的「脉冲当量」下发到卡；"
                 + "当量为 0 或填错只会让 unit 换算出错（走错距离 / 速度），不会返回 8194。"
                 + "卡返回码请对照 Mcn420ErrorCode.xml：8194 = 普通缓冲满、20480 = 0 通道轴未运动完成、26 = 使能出错。",
            AxisCount = 8,
            InIoCount = 16,
            OutIoCount = 16,
        };
    }
}
