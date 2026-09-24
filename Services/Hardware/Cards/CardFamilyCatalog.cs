// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 运动控制卡「家族目录」—— 把移植进来的 WenQiZhi.Domain.MotionCard.Common 各卡族
// 与 NoCodeMotion 的控制器 / 轴 / IO 配置对接起来。
//
// 背景：SMotorse 参考实现里，每个卡族（26 个）各自有
//   CardRealization : ICard        —— 卡级操作（初始化 / 打开 / 关闭 / 总线状态）
//   AxisRealization : IAxis        —— 轴级操作（点位 / 回零 / 使能 / 停止 / 读位置）
//   InioRealization : IIOInPut     —— 主板输入
//   OutioRealization: IIOOutPut    —— 主板输出
//   Expand*Realizetion : IEIOInPut / IEIOOutPut —— 扩展 IO（CAN / EtherCAT 模块，部分卡族才有）
// 参考实现靠 WinForms 界面 + SystemSupportMotionCardEnum 直接 new 出来用。
// NoCodeMotion 没有那套界面，所以这里把「用户配置的控制器 → 具体卡族实现」这一步
// 显式登记成一张表，由 CardFamilyCatalog.Resolve 完成匹配，再由 WenQiZhiCardBridge 驱动。
//
// ★ 品牌归属来自参考实现自己的枚举与分组规则，不是猜的：
//     ShareData/ShareDatastruct.cs  的 CardTypeEnum = { 模拟卡, 雷赛, 升立德, 恒昱, 研控 }
//     SMotorseWpf/HeaderRegion/ProjectManager/SelectControllerDialog.xaml.cs 的 BuildCardTree()
//   该规则为：卡名含 DMC 且含 E → 雷赛总线；含 DMC 不含 E → 雷赛脉冲；
//             以 PCI/E/F 开头 → 升立德；以 HY 开头 → 恒昱；
//             以 MCC 开头且含 E → 研控总线；以 MCC 开头不含 E → 研控脉冲；
//             含「虚拟」或「仿真」→ 模拟卡。
//   参考实现自己的规则漏掉了 SLD1230/1232 系列与 PLTEI400H（首字母 S / P 都没覆盖），
//   本表按同一套判据补齐：SLD* 属升立德（sldmv.dll / PCI1230.dll 同族）。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using WenQiZhi.Domain.MotionCard.Common;

namespace NoCodeMotion.Services.Hardware.Cards
{
    /// <summary>
    /// 一个卡族的运行期实例集合。
    /// <para>注意 <see cref="NewAxis"/> 是工厂而不是单个实例：</para>
    /// <para>各卡族的 AxisRealization 把 <c>AxisID</c> / <c>AxisWhichCardNo</c> 存在实例字段里，
    /// 而且 GetCardAxisCurrentPosition / CardAxisHomeMove / GetCardAxisCurrentState /
    /// CardAxisLineUnitSpeed 这些方法<b>不带 CardNo / axis 参数</b>，直接读这两个字段。
    /// 所以每个配置的轴必须持有自己的实例，共用一份会把轴号串到一起。</para>
    /// </summary>
    public sealed class CardFamilyRuntime
    {
        /// <summary>所属卡族。</summary>
        public CardFamilyDescriptor Family { get; set; }

        /// <summary>卡级操作（已 new，未初始化）。</summary>
        public ICard Card { get; set; }

        /// <summary>轴级操作工厂：每个轴取一个新实例。</summary>
        public Func<IAxis> NewAxis { get; set; }

        /// <summary>主板输入。</summary>
        public IIOInPut InIo { get; set; }

        /// <summary>主板输出。</summary>
        public IIOOutPut OutIo { get; set; }

        /// <summary>扩展输入（该卡族不支持时为 null）。</summary>
        public IEIOInPut ExtIn { get; set; }

        /// <summary>扩展输出（该卡族不支持时为 null）。</summary>
        public IEIOOutPut ExtOut { get; set; }

        /// <summary>该卡族是否支持扩展 IO 模块。</summary>
        public bool HasExpansionIo => ExtIn != null && ExtOut != null;
    }

    /// <summary>
    /// 一个卡族的静态描述：怎么匹配、依赖哪些底层 dll、怎么 new 出实现类。
    /// </summary>
    public sealed class CardFamilyDescriptor
    {
        /// <summary>家族键（与移植目录名一致，如 DMC_E3032_EtherCAT）。写进控制器「卡型号」即可精确命中。</summary>
        public string Key { get; set; }

        /// <summary>中文显示名。</summary>
        public string DisplayName { get; set; }

        /// <summary>品牌（雷赛 / 升立德 / 恒昱 / 研控 / 模拟卡；无法确证时为空）。</summary>
        public string Vendor { get; set; }

        /// <summary>总线 / 脉冲（雷赛、研控下再分一级）。</summary>
        public string BusKind { get; set; }

        /// <summary>移植后的命名空间（实现类都在这个命名空间下）。</summary>
        public string Namespace { get; set; }

        /// <summary>该卡族底层真正调用的原生库文件名（探测与说明用）。</summary>
        public string[] NativeDlls { get; set; }

        /// <summary>该卡族支持的总线类型。</summary>
        public CardBusType[] BusTypes { get; set; }

        /// <summary>
        /// 匹配「卡型号」的关键字（不区分大小写，取最长命中）。
        /// <para>两类值都要放进来：① 真实型号名（DMC3400 / MCC800S / HY7C00）；
        /// ② <b>固件卡型码</b> —— 「自动识别」对雷赛卡写进「卡型号」的是
        /// <c>$"0x{固件上报的卡型码:X}"</c>（见 <c>AxisControllerViewModel.AutoDetect</c>），
        /// 例如 DMC1000S 会写成 <c>0x2711</c>。少了固件码，混装工程里这张卡会掉到
        /// 「品牌 + 总线」兜底分支，被配上一张**不相干的同品牌卡</b>。
        /// 固件码取自参考实现 <c>SystemHardwareData</c> 里「卡型码 → 卡族」的 switch。</para>
        /// </summary>
        public string[] Aliases { get; set; }

        /// <summary>备注 / 对接说明。</summary>
        public string Note { get; set; }

        /// <summary>
        /// 该卡族的标准规格（设计容量），在底层硬件 API 不回报容量时作为可信兜底。
        /// 0 = 未知（不兜底，沿用硬件返回值或用户配置）。当前仅个别卡族人工核对此值，
        /// 其余卡族保持 0，继续走底层 API 的真实读数。例如研控 MCN42 系列固定为 8 轴 / 16 入 / 16 出，
        /// 但其移植实现 <c>GetCardTotalAxisNum</c> 离线时返回 0、IO 实体默认填 24，故需目录规格兜底。
        /// </summary>
        public int AxisCount { get; set; }

        /// <summary>该卡族标准规格：主板输入 IO 数（兜底用，0 = 未知）。</summary>
        public int InIoCount { get; set; }

        /// <summary>该卡族标准规格：主板输出 IO 数（兜底用，0 = 未知）。</summary>
        public int OutIoCount { get; set; }

        /// <summary>工厂：new 出一整套实现类。</summary>
        public Func<CardFamilyRuntime> Create { get; set; }

        /// <summary>该卡族依赖的任意一个原生库在程序目录 / 系统路径中是否可见。</summary>
        public bool DllPresent => CardFamilyCatalog.AnyDllPresent(NativeDlls);

        /// <summary>
        /// 该卡族是否为「模拟卡」—— 实现是进程内仿真，不依赖任何真实硬件 / 原生 dll。
        /// <para>判据用登记时的品牌与总线（见文件头规则「含『虚拟』或『仿真』→ 模拟卡」）：
        /// <c>VirtualMotionCard</c>（虚拟运动卡）与 <c>DigitalTwinCard</c>（数字孪生卡）
        /// 都登记为 <c>Vendor = 模拟卡</c> / <c>BusKind = 虚拟</c>。</para>
        /// <para>★ 模拟卡与真实卡在几处语义不同，桥里必须分开处理，否则会出现
        /// 「指令返回 0 但轴不动」「状态全是 —」这类看起来像 bug 的现象，详见
        /// <c>WenQiZhiCardBridge.EnsureSimReady / BuildSimParam</c>。</para>
        /// </summary>
        public bool IsSimulation =>
            Vendor == "模拟卡" || BusKind == "虚拟" ||
            (Key != null && (Key.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0
                          || Key.IndexOf("DigitalTwin", StringComparison.OrdinalIgnoreCase) >= 0));

        public override string ToString() => Key;
    }

    /// <summary>
    /// 已移植运动控制卡族的中央登记表。
    /// 「控制器 → 卡族实现」的匹配、自动识别扫描、Lua 侧的能力查询都读这里。
    /// </summary>
    public static class CardFamilyCatalog
    {
        // 卡族实现类的构造辅助：把 6 个 new 收成一行，省得 26 个卡族各写 6 遍。
        private static Func<CardFamilyRuntime> F(
            Func<ICard> card,
            Func<IAxis> axis,
            Func<IIOInPut> inIo,
            Func<IIOOutPut> outIo,
            Func<IEIOInPut> extIn = null,
            Func<IEIOOutPut> extOut = null)
            => () => new CardFamilyRuntime
            {
                Card = card(),
                NewAxis = axis,
                InIo = inIo(),
                OutIo = outIo(),
                ExtIn = extIn == null ? null : extIn(),
                ExtOut = extOut == null ? null : extOut(),
            };

        /// <summary>全部已移植卡族。</summary>
        public static readonly CardFamilyDescriptor[] Families =
        {
            // ==================== 雷赛（LTDMC.dll / Dmc1000.dll） ====================

            new CardFamilyDescriptor
            {
                Key = "DMC_E3032_EtherCAT", DisplayName = "雷赛 DMC-E3000 / 5000（EtherCAT 总线主站）",
                Vendor = "雷赛", BusKind = "总线",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.DMC_E3032_EtherCAT",
                NativeDlls = new[] { "LTDMC.dll" },
                BusTypes = new[] { CardBusType.EtherCAT, CardBusType.CANopen, CardBusType.Pulse },
                Aliases = new[] { "DMC_E3032", "DMC-E3032", "E3032", "DMC-E3000", "DMC-E5000", "DMC5000", "E3000", "0x80013032", "2147561522" },
                Note = "LTDMC.dll。轴运动走 dmc_ 族；伺服使能 / 回零 / 总线 IO 走 nmc_ 族（CiA402 状态机）。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.DMC_E3032_EtherCAT.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DMC_E3032_EtherCAT.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DMC_E3032_EtherCAT.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DMC_E3032_EtherCAT.OutioRealization()),
            },

            new CardFamilyDescriptor
            {
                Key = "DMC3400A", DisplayName = "雷赛 DMC3400A（脉冲卡）",
                Vendor = "雷赛", BusKind = "脉冲",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.DMC3400A",
                NativeDlls = new[] { "LTDMC.dll" },
                BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
                // 13312 = 0x3400 是 dmc_get_CardInfList 上报的固件卡型，自动识别会写成 "0x3400"
                Aliases = new[] { "DMC3400", "DMC3800", "DMC3000", "0x3400", "0x3800", "3400", "3800", "3000系列", "0x3C00", "13312", "14336", "15360" },
                Note = "LTDMC.dll。支持 CAN 扩展 IO 模块（DMC_CAN_In*Out*）。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.DMC3400A.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DMC3400A.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DMC3400A.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DMC3400A.OutioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DMC3400A.ExpandInioRealizetion(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DMC3400A.ExpandOutioRealizetion()),
            },

            new CardFamilyDescriptor
            {
                Key = "DMC1000S", DisplayName = "雷赛 DMC1000S（脉冲卡）",
                Vendor = "雷赛", BusKind = "脉冲",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.DMC1000S",
                NativeDlls = new[] { "Dmc1000.dll", "LTDMC.dll" },
                BusTypes = new[] { CardBusType.Pulse },
                Aliases = new[] { "DMC1000S", "DMC1000", "1000S", "0x2711", "10001" },
                Note = "主驱动 Dmc1000.dll，另有一个高速比较器配置调用 LTDMC.dll。（该目录下的 dmc1000SConfig.cs 仍在旧命名空间 MotionCardRes.DMC1000S，与实现类不同。）",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.DMC1000S.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DMC1000S.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DMC1000S.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DMC1000S.OutioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DMC1000S.ExpandInioRealizetion(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DMC1000S.ExpandOutioRealizetion()),
            },

            new CardFamilyDescriptor
            {
                Key = "DMC2210", DisplayName = "雷赛 DMC2210（脉冲卡）",
                Vendor = "雷赛", BusKind = "脉冲",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.DMC2210",
                NativeDlls = new[] { "Dmc2210.dll" },
                BusTypes = new[] { CardBusType.Pulse },
                Aliases = new[] { "DMC2210", "2210" },
                // ★ 参考工程 bin2 里的 Dmc2210.dll 是 32 位（x86），64 位宿主加载会失败。
                Note = "Dmc2210.dll（参考工程自带的这份是 32 位，64 位程序加载不了，需换成 x64 版）。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.DMC2210.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DMC2210.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DMC2210.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DMC2210.OutioRealization()),
            },

            // ==================== 升立德（sldmv.dll / PCI 系列 / E64 系列） ====================

            new CardFamilyDescriptor
            {
                Key = "SLD1230", DisplayName = "升立德 SLD1230（PCIe 脉冲卡）",
                Vendor = "升立德", BusKind = "脉冲",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.SLD1230",
                NativeDlls = new[] { "LTDMC.dll", "PCI1230.dll" },
                BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
                Aliases = new[] { "SLD1230", "1230", "0x1230", "4656" },
                Note = "PCI1230.dll。支持扩展 IO 模块。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.SLD1230.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SLD1230.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SLD1230.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SLD1230.OutioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SLD1230.ExpandInioRealizetion(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SLD1230.ExpandOutioRealizetion()),
            },

            new CardFamilyDescriptor
            {
                Key = "SLD1232", DisplayName = "升立德 SLD1232（PCIe 脉冲卡）",
                Vendor = "升立德", BusKind = "脉冲",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.SLD1232",
                NativeDlls = new[] { "LTDMC.dll", "PCI1230.dll" },
                BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
                Aliases = new[] { "SLD1232", "1232", "0x1232", "4658" },
                Note = "PCI1230.dll。支持扩展 IO 模块。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.SLD1232.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SLD1232.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SLD1232.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SLD1232.OutioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SLD1232.ExpandInioRealizetion(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SLD1232.ExpandOutioRealizetion()),
            },

            new CardFamilyDescriptor
            {
                Key = "E64IOSeries", DisplayName = "升立德 E64 系列 IO 卡（E6432 / E6416 / E6364 / E6316 / E6564 / E6516）",
                Vendor = "升立德", BusKind = "IO 卡",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.SLDIOE64",
                NativeDlls = new[] { "LTDMC.dll", "sldmv.dll" },
                BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
                Aliases = new[] { "E6432", "E6416", "E6364", "E6316", "E6564", "E6516", "SLDIOE64", "E64", "0xE6416", "0xE6432", "943126", "943154" },
                Note = "sldmv.dll。纯 IO 扩展卡（轴能力有限），扩展 IO 走 CAN / EtherCAT 模块。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.SLDIOE64.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SLDIOE64.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SLDIOE64.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SLDIOE64.OutioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SLDIOE64.ExpandInioRealizetion(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SLDIOE64.ExpandOutioRealizetion()),
            },

            new CardFamilyDescriptor
            {
                Key = "PCI9014", DisplayName = "升立德 PCI9014（4 轴脉冲卡）",
                Vendor = "升立德", BusKind = "脉冲",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.PCI9014",
                NativeDlls = new[] { "Pci9014.dll", "SLD9014PTP.dll" },
                BusTypes = new[] { CardBusType.Pulse },
                Aliases = new[] { "PCI9014", "9014", "0x2336" },
                // 该卡族源码里还 DllImport 了 SLD9014PTP.dll（参考工程未提供），只声明不调用时才不影响。
                Note = "Pci9014.dll（源码另引 SLD9014PTP.dll，参考工程未随附，未走到就不影响）。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.PCI9014.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.PCI9014.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.PCI9014.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.PCI9014.OutioRealization()),
            },

            new CardFamilyDescriptor
            {
                Key = "PCI9016", DisplayName = "升立德 PCI9016（脉冲卡）",
                Vendor = "升立德", BusKind = "脉冲",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.PCI9016",
                NativeDlls = new[] { "Pci9016.dll" },
                BusTypes = new[] { CardBusType.Pulse },
                Aliases = new[] { "PCI9016", "9016", "0x2338" },
                Note = "Pci9016.dll。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.PCI9016.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.PCI9016.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.PCI9016.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.PCI9016.OutioRealization()),
            },

            new CardFamilyDescriptor
            {
                Key = "RT3316", DisplayName = "升立德 RT3316（脉冲卡）",
                Vendor = "升立德", BusKind = "脉冲",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.RT3316",
                NativeDlls = new[] { "Pci9014.dll" },
                BusTypes = new[] { CardBusType.Pulse },
                Aliases = new[] { "RT3316", "3316" },
                Note = "复用 Pci9014.dll（与 PCI9014 同族）。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.RT3316.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.RT3316.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.RT3316.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.RT3316.OutioRealization()),
            },

            // ==================== 恒昱（MCX08.dll / HY7X00 / HYMC608） ====================

            new CardFamilyDescriptor
            {
                Key = "HY7X00", DisplayName = "恒昱 HY7X00 系列（HY7400 / HY7600 / HY7800 / HY7C00）",
                Vendor = "恒昱", BusKind = "脉冲",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.HY7X00",
                NativeDlls = new[] { "LTDMC.dll", "PCI400.dll" },
                BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
                Aliases = new[] { "HY7X00", "HY7400", "HY7600", "HY7800", "HY7C00", "0x7400", "0x7600", "0x7800", "0x7C00", "29696", "30208", "30720", "31744" },
                Note = "主驱动 PCI400.dll（恒昱 7X00 与 PCI400 同一套 API），另有 1 个高速比较器配置调用 LTDMC.dll。支持 HY_CAN_In20Out20 扩展模块。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.HY7X00.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.HY7X00.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.HY7X00.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.HY7X00.OutioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.HY7X00.ExpandInioRealizetion(),
                           () => new WenQiZhi.Domain.MotionCard.Common.HY7X00.ExpandOutioRealizetion()),
            },

            new CardFamilyDescriptor
            {
                Key = "HYMC608", DisplayName = "恒昱 MC608（脉冲卡）",
                Vendor = "恒昱", BusKind = "脉冲",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.HYMC608",
                NativeDlls = new[] { "LTDMC.dll", "MCX08.dll" },
                BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
                Aliases = new[] { "HYMC608", "MC608", "MCX08", "0x608", "1544" },
                Note = "MCX08.dll。支持扩展 IO 模块。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.HYMC608.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.HYMC608.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.HYMC608.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.HYMC608.OutioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.HYMC608.ExpandInioRealizetion(),
                           () => new WenQiZhi.Domain.MotionCard.Common.HYMC608.ExpandOutioRealizetion()),
            },

            // ==================== 研控（MCC.dll / MCC800S.dll / MCCE135.dll / MCN420.dll） ====================

            new CardFamilyDescriptor
            {
                Key = "YKMCC400P", DisplayName = "研控 MCC400P（脉冲卡）",
                Vendor = "研控", BusKind = "脉冲",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.YKMCC400P",
                NativeDlls = new[] { "MCC.dll" },
                BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
                Aliases = new[] { "YKMCC400P", "MCC400P", "400P", "0x4040", "16448" },
                // ★ MCC.dll 参考工程 bin2 里没有，属缺失库（与 MCC1600P / MCC800P 共用）。
                Note = "MCC.dll（参考工程未随附该库，需向厂商索取后放入程序目录）。支持扩展 IO 模块。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.YKMCC400P.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC400P.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC400P.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC400P.OutioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC400P.ExpandInioRealizetion(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC400P.ExpandOutioRealizetion()),
            },

            new CardFamilyDescriptor
            {
                Key = "YKMCC800P", DisplayName = "研控 MCC800P（脉冲卡）",
                Vendor = "研控", BusKind = "脉冲",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.YKMCC800P",
                NativeDlls = new[] { "MCC.dll" },
                BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
                Aliases = new[] { "YKMCC800P", "MCC800P", "800P", "0x8040", "32832" },
                Note = "MCC.dll（参考工程未随附该库，需向厂商索取后放入程序目录）。支持扩展 IO 模块。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.YKMCC800P.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC800P.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC800P.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC800P.OutioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC800P.ExpandInioRealizetion(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC800P.ExpandOutioRealizetion()),
            },

            new CardFamilyDescriptor
            {
                Key = "YKMCC1600P", DisplayName = "研控 MCC1600P（脉冲卡）",
                Vendor = "研控", BusKind = "脉冲",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.YKMCC1600P",
                NativeDlls = new[] { "MCC.dll" },
                BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
                Aliases = new[] { "YKMCC1600P", "MCC1600P", "1600P", "0xF00", "3840" },
                Note = "MCC.dll（参考工程未随附该库，需向厂商索取后放入程序目录）。支持扩展 IO 模块。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.YKMCC1600P.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC1600P.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC1600P.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC1600P.OutioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC1600P.ExpandInioRealizetion(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC1600P.ExpandOutioRealizetion()),
            },

            new CardFamilyDescriptor
            {
                Key = "YMCC1200P", DisplayName = "研控 MCC1200P（脉冲卡）",
                Vendor = "研控", BusKind = "脉冲",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.YKMCC1200P",
                NativeDlls = new[] { "MCC.dll" },
                BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
                Aliases = new[] { "YMCC1200P", "YKMCC1200P", "MCC1200P", "1200P" },
                Note = "MCC.dll（参考工程未随附该库，需向厂商索取后放入程序目录）。支持扩展 IO 模块。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.YKMCC1200P.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC1200P.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC1200P.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC1200P.OutioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC1200P.ExpandInioRealizetion(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC1200P.ExpandOutioRealizetion()),
            },

            new CardFamilyDescriptor
            {
                Key = "YKMCC400S", DisplayName = "研控 MCC400S（总线卡）",
                Vendor = "研控", BusKind = "总线",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.YKMCC400S",
                NativeDlls = new[] { "MCC800S.dll" },
                BusTypes = new[] { CardBusType.EtherCAT, CardBusType.CANopen, CardBusType.Pulse },
                Aliases = new[] { "YKMCC400S", "MCC400S", "400S" },
                // 参考实现把 MCC400S 与 MCC800S 都上报成固件码 0x8076（它自己的复制粘贴 bug），
                // 无法据此区分；按 0x4040=400P / 0x8040=800P 的规律，0x80xx 属 800 系列，
                // 所以 0x8076 归给了 MCC800S，400S 必须按型号名填写。
                Note = "MCC800S.dll。支持扩展 IO 模块。固件码 0x8076 无法与 MCC800S 区分，本族不登记该码。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.YKMCC400S.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC400S.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC400S.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC400S.OutioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC400S.ExpandInioRealizetion(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC400S.ExpandOutioRealizetion()),
            },

            new CardFamilyDescriptor
            {
                Key = "YKMCC800S", DisplayName = "研控 MCC800S（总线卡）",
                Vendor = "研控", BusKind = "总线",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.YKMCC800S",
                NativeDlls = new[] { "MCC800S.dll" },
                BusTypes = new[] { CardBusType.EtherCAT, CardBusType.CANopen, CardBusType.Pulse },
                Aliases = new[] { "YKMCC800S", "MCC800S", "800S", "0x8076", "32886" },
                Note = "MCC800S.dll。支持扩展 IO 模块。固件码 0x8076（参考实现里 400S 也写成这个码，按 0x80xx=800 系列归本族）。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.YKMCC800S.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC800S.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC800S.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC800S.OutioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC800S.ExpandInioRealizetion(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC800S.ExpandOutioRealizetion()),
            },

            new CardFamilyDescriptor
            {
                Key = "YKMCCE3032", DisplayName = "研控 MCCE3032 / MCCE332（EtherCAT 总线主站）",
                Vendor = "研控", BusKind = "总线",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.YKMCC_E3032_EtherCAT",
                NativeDlls = new[] { "MCCE135.dll" },
                BusTypes = new[] { CardBusType.EtherCAT, CardBusType.CANopen },
                Aliases = new[] { "YKMCCE3032", "YKMCC_E3032", "MCCE3032", "MCCE332", "0x7E2", "2018" },
                Note = "MCCE135.dll。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.YKMCC_E3032_EtherCAT.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC_E3032_EtherCAT.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC_E3032_EtherCAT.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCC_E3032_EtherCAT.OutioRealization()),
            },

            new CardFamilyDescriptor
            {
                Key = "MCN42Series", DisplayName = "研控 MCN42 系列（MCC42Series）",
                Vendor = "研控", BusKind = "脉冲",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.YKMCN42Series",
                NativeDlls = new[] { "MCN420.dll" },
                BusTypes = new[] { CardBusType.Pulse },
                Aliases = new[] { "MCN42", "MCC42", "MCN420", "42系列", "0x1A4", "0x1A5" },
                Note = "MCN420.dll。固定规格：8 轴 / 16 入 / 16 出（底层实现离线返回 0、IO 默认填 24，需目录规格兜底）。",
                AxisCount = 8, InIoCount = 16, OutIoCount = 16,
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.YKMCN42Series.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCN42Series.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCN42Series.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.YKMCN42Series.OutioRealization()),
            },

            // ==================== 模拟卡（不驱动真实硬件） ====================

            new CardFamilyDescriptor
            {
                Key = "VirtualMotionCard", DisplayName = "虚拟运动卡（脱机调试）",
                Vendor = "模拟卡", BusKind = "虚拟",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.VirtualMotionCard",
                NativeDlls = new[] { "LTDMC.dll" },
                BusTypes = new[] { CardBusType.Pulse, CardBusType.EtherCAT, CardBusType.Other },
                Aliases = new[] { "虚拟运动卡", "虚拟控制卡", "虚拟", "VirtualMotionCard", "0x1B198", "111000" },
                Note = "主路径纯内存仿真，不碰硬件；仅「多轴停止 / 急停」两个接口转发 LTDMC.dll。用于先写流程后接设备。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.VirtualMotionCard.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.VirtualMotionCard.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.VirtualMotionCard.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.VirtualMotionCard.OutioRealization()),
            },

            new CardFamilyDescriptor
            {
                Key = "DigitalTwinCard", DisplayName = "数字孪生卡（仿真运动卡）",
                Vendor = "模拟卡", BusKind = "虚拟",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.DigitalTwinCard",
                NativeDlls = new string[0],
                BusTypes = new[] { CardBusType.Pulse, CardBusType.ModbusTcp, CardBusType.Other },
                Aliases = new[] { "数字孪生", "仿真运动卡", "仿真", "DigitalTwin", "0x877F8", "555000" },
                Note = "轴 / IO 走内存仿真；另带 ModubsComm（Modbus/TCP）可接外部仿真模型。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.DigitalTwinCard.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DigitalTwinCard.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DigitalTwinCard.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.DigitalTwinCard.OutioRealization()),
            },

            // ==================== 品牌待考（参考实现自己的分组规则也没归类的） ====================

            new CardFamilyDescriptor
            {
                Key = "MCC141C", DisplayName = "研控 MCC141C（脉冲卡）",
                // 品牌依据：① 参考实现的分组规则「StartsWith("MCC") 且不含 E → 研控·脉冲」命中；
                //          ② 它的底层包装类命名空间是 csMCC1C00，与研控其余卡（csMCC / csMCC800S /
                //             csMCC1600 / csMCCE135）同一命名族。
                //          保留说明：它没有出现在参考实现的两个卡型枚举里（SystemSupportMotionCardEnum /
                //           AllMotionCardEnum），所以参考实现的卡树里本来也看不到它 —— 归属是推断出来的。
                Vendor = "研控", BusKind = "脉冲",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.MCC141C",
                NativeDlls = new[] { "MCC1C00.dll" },
                BusTypes = new[] { CardBusType.Pulse },
                Aliases = new[] { "MCC141C", "141C", "MCC1C00" },
                Note = "MCC1C00.dll。品牌按参考实现规则推断为研控（该型号未出现在参考实现的卡型枚举里）。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.MCC141C.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.MCC141C.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.MCC141C.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.MCC141C.OutioRealization()),
            },

            new CardFamilyDescriptor
            {
                Key = "PLTEI400H", DisplayName = "PLTEI400H（脉冲卡）",
                Vendor = "", BusKind = "脉冲",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.PLTEI400H",
                NativeDlls = new[] { "LTDMC.dll", "PLT.dll" },
                BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen },
                Aliases = new[] { "PLTEI400H", "PLTEI400", "400H", "PLT", "0xD430", "54320" },
                Note = "PLT.dll。支持扩展 IO 模块；参考实现的品牌分组规则未收录此型号，品牌待确认。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.PLTEI400H.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.PLTEI400H.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.PLTEI400H.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.PLTEI400H.OutioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.PLTEI400H.ExpandInioRealizetion(),
                           () => new WenQiZhi.Domain.MotionCard.Common.PLTEI400H.ExpandOutioRealizetion()),
            },

            new CardFamilyDescriptor
            {
                Key = "EC600", DisplayName = "升立德 EC600（EtherCAT 总线主站）",
                // 品牌依据：参考实现的分组规则「StartsWith("E") → 升立德」命中，且它在
                // SystemSupportMotionCardEnum 里紧挨着升立德的 E64xx IO 卡区块
                // （… MCCE332, EC600, E6432, E6416, E6364 …），参考实现的卡树就是这么归的。
                Vendor = "升立德", BusKind = "总线",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.EC600_EtherCAT",
                NativeDlls = new[] { "LTDMC.dll", "ecat_motion.dll" },
                BusTypes = new[] { CardBusType.EtherCAT, CardBusType.Pulse },
                Aliases = new[] { "EC600", "0xEC600", "968192" },
                Note = "ecat_motion.dll。品牌按参考实现规则归为升立德（它同时支持脉冲，属总线/脉冲双模）。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.EC600_EtherCAT.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.EC600_EtherCAT.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.EC600_EtherCAT.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.EC600_EtherCAT.OutioRealization()),
            },

            new CardFamilyDescriptor
            {
                Key = "SoftServo", DisplayName = "SoftServo WMX3（软件 EtherCAT 主站）",
                Vendor = "", BusKind = "总线",
                Namespace = "WenQiZhi.Domain.MotionCard.Common.SoftServo_EtherCAT",
                // WMX3 是托管程序集（x64），不是原生 dll；列在这里是为了让探测能识别出它已随程序输出。
                NativeDlls = new[] { "CoreMotionApi_CLRLib.dll", "EcApi_CLRLib.dll", "EventApi_CLRLib.dll", "IOApi_CLRLib.dll", "LTDMC.dll", "MCCE135.dll", "RtMotionApiNET.dll", "WMX3Api_CLRLib.dll" },
                BusTypes = new[] { CardBusType.EtherCAT },
                Aliases = new[] { "SoftServo", "WMX3", "软伺服" },
                Note = "主驱动为 WMX3Api_CLRLib.dll 等一组托管程序集（x64，需先装 WMX3 运行环境）；EtherCAT 主站复位 / 错误码读取沿用了研控 MCCE135.dll 与雷赛 LTDMC.dll 的调用，故一并列出。",
                Create = F(() => new WenQiZhi.Domain.MotionCard.Common.SoftServo_EtherCAT.CardRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SoftServo_EtherCAT.AxisRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SoftServo_EtherCAT.InioRealization(),
                           () => new WenQiZhi.Domain.MotionCard.Common.SoftServo_EtherCAT.OutioRealization()),
            },
        };

        /// <summary>参考实现里已有的品牌枚举（来自 ShareDatastruct.cs 的 CardTypeEnum），按此顺序展示。</summary>
        public static readonly string[] Vendors = { "雷赛", "升立德", "恒昱", "研控", "模拟卡" };

        /// <summary>某个卡族的原生库是否在程序目录 / 系统路径中可见。</summary>
        public static bool AnyDllPresent(string[] dllNames)
        {
            if (dllNames == null || dllNames.Length == 0) return false;
            foreach (var dll in dllNames)
            {
                try
                {
                    if (File.Exists(Path.Combine(AppContext.BaseDirectory ?? string.Empty, dll))) return true;
                    if (NativeLibrary.TryLoad(dll, out IntPtr h))
                    {
                        if (h != IntPtr.Zero) NativeLibrary.Free(h);
                        return true;
                    }
                }
                catch { /* 探测失败按「没有库」处理 */ }
            }
            return false;
        }

        /// <summary>按家族键精确取卡族；找不到返回 null。</summary>
        public static CardFamilyDescriptor ByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            string k = key.Trim();
            return Families.FirstOrDefault(x => string.Equals(x.Key, k, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 把「控制器配置」匹配到具体卡族。
        /// <para>顺序：① 卡型号关键字（最长命中优先）→ ② 家族键字面量 → ③ 品牌 + 总线类型。</para>
        /// <para>匹配不到返回 null，调用方应回退到别的对接实现（例如雷赛 Leadshine 桥）。</para>
        /// </summary>
        public static CardFamilyDescriptor Resolve(string vendor, string cardType, string busType)
        {
            string t = (cardType ?? string.Empty).Trim();

            // ① 卡型号关键字：取最长命中，避免 "1000" 抢走本该给 "1000S" 的匹配
            if (t.Length > 0)
            {
                CardFamilyDescriptor best = null;
                int bestLen = 0;
                foreach (var f in Families)
                {
                    if (f.Aliases == null) continue;
                    foreach (var a in f.Aliases)
                    {
                        if (string.IsNullOrEmpty(a)) continue;
                        if (a.Length <= bestLen) continue;
                        if (t.IndexOf(a, StringComparison.OrdinalIgnoreCase) >= 0) { best = f; bestLen = a.Length; }
                    }
                }
                if (best != null) return best;

                // ② 用户直接把家族键写进了「卡型号」
                var byKey = ByKey(t);
                if (byKey != null) return byKey;
            }

            // ③ 只给了品牌：按品牌 + 总线类型挑
            if (!string.IsNullOrWhiteSpace(vendor))
            {
                string v = NormalizeVendor(vendor);
                var cands = Families.Where(x => x.Vendor == v).ToList();
                if (cands.Count > 0)
                {
                    bool wantBus = IsBusType(busType);
                    if (wantBus)
                    {
                        // 只给了「品牌 + 总线」时，同一品牌往往有好几张支持 EtherCAT 的卡
                        // （研控 MCC400S / MCC800S 兼脉冲，MCCE3032 是纯总线主站）。
                        // 先挑「纯总线」的（不兼脉冲），也就是专职总线主站；没有再退到任何支持总线的。
                        return cands.FirstOrDefault(x => x.BusTypes.Contains(CardBusType.EtherCAT)
                                                        && !x.BusTypes.Contains(CardBusType.Pulse))
                            ?? cands.FirstOrDefault(x => x.BusTypes.Contains(CardBusType.EtherCAT))
                            ?? cands[0];
                    }
                    return cands.FirstOrDefault(x => x.BusTypes.Contains(CardBusType.Pulse))
                        ?? cands[0];
                }
            }

            return null;
        }

        /// <summary>
        /// 兼容历史工程的品牌写法：早期版本把仿真卡登记为「虚拟」，现统一叫「模拟卡」。
        /// </summary>
        public static string NormalizeVendor(string vendor)
        {
            string v = (vendor ?? string.Empty).Trim();
            return v == "虚拟" ? "模拟卡" : v;
        }

        /// <summary>总线类型字符串是否表示「总线卡」（否则按脉冲卡处理）。</summary>
        public static bool IsBusType(string busType)
        {
            string s = busType ?? string.Empty;
            if (s.Contains("脉冲")) return false;
            return s.Contains("总线")
                || s.IndexOf("EtherCAT", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("CANopen", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Profinet", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>扫一遍所有卡族，返回底层库可见（即驱动已装 / dll 已随程序输出）的那些。</summary>
        public static List<CardFamilyDescriptor> DetectPresent()
        {
            var list = new List<CardFamilyDescriptor>();
            foreach (var f in Families)
            {
                try { if (f.DllPresent) list.Add(f); }
                catch { /* 单个卡族探测失败不影响其它 */ }
            }
            return list;
        }
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
