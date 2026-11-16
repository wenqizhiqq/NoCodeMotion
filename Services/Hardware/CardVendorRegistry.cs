// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace NoCodeMotion.Services.Hardware
{
    /// <summary>总线类型分类：脉冲型卡 与 各类实时总线主站。</summary>
    public enum CardBusType
    {
        /// <summary>脉冲型（步进 / 伺服脉冲输出）。</summary>
        Pulse,
        /// <summary>EtherCAT 实时以太网总线主站。</summary>
        EtherCAT,
        /// <summary>CANopen 现场总线。</summary>
        CANopen,
        /// <summary>Modbus/TCP（网口）。</summary>
        ModbusTcp,
        /// <summary>Modbus/RTU（串口）。</summary>
        ModbusRtu,
        /// <summary>Profinet 工业以太网。</summary>
        Profinet,
        /// <summary>其它 / 未分类。</summary>
        Other
    }

    /// <summary>
    /// 主流运动控制卡 / 总线主站厂商信息。用于「添加控制器」下拉与「自动识别硬件」扫描。
    /// <para>品牌登记只描述厂商本身；某品牌下具体有哪些卡族，由 <c>Cards/Modules</c> 下的
    /// 卡族模块各自声明，两边不需要手工同步。</para>
    /// </summary>
    public sealed class CardVendorInfo
    {
        /// <summary>品牌代号（与 AxisControllerItem.Vendor 对应，如 雷赛 / 固高）。</summary>
        public string Vendor { get; set; }
        /// <summary>中文全称（雷赛 Leadshine / 固高 Googoltech …）。</summary>
        public string DisplayName { get; set; }
        /// <summary>类别：脉冲卡 / 总线主站 / 综合型 / 仿真。</summary>
        public string Category { get; set; }
        /// <summary>该厂商支持的总线类型（可同时含 脉冲 与 总线）。</summary>
        public CardBusType[] BusTypes { get; set; }
        /// <summary>用于探测“是否装了驱动”的原生库文件名。</summary>
        public string[] DllNames { get; set; }
        /// <summary>是否已集成真实实时对接（雷赛 = 自有 LtdmcCard 封装，其余 = 移植进来的卡族层）。</summary>
        public bool HasLiveBridge { get; set; }
        /// <summary>备注 / 对接说明。</summary>
        public string Note { get; set; }
    }

    /// <summary>
    /// 主流运动控制卡厂商登记表（脉冲型 + 总线型全覆盖）。
    /// 这是“支持更多品牌”的中央数据源：添加控制器下拉、自动识别、总线分类都读这里。
    /// 各品牌下已移植的卡族不在这里维护 —— 见 <c>Cards/Modules</c> 下各卡族模块。
    ///
    /// ★ 品牌划分与卡族对应关系来自 WenQiZhiMotion / SMotorse 参考实现自己的枚举与分组规则：
    ///     ShareData/ShareDatastruct.cs 的 CardTypeEnum = { 模拟卡, 雷赛, 升立德, 恒昱, 研控 }
    ///   参考实现没覆盖的型号（MCC141C / PLTEI400H / EC600 / SoftServo）统一归到「未分类」，
    ///   不臆造品牌名。
    /// </summary>
    public static class CardVendorRegistry
    {
        public static readonly List<CardVendorInfo> Vendors = new List<CardVendorInfo>
        {
            // ==================== 已集成真实对接（有可用的实时驱动实现） ====================

            new CardVendorInfo {
                Vendor = "雷赛", DisplayName = "雷赛 Leadshine", Category = "综合型（脉冲+总线）",
                // Other（「其它」）也要声明：自动识别在「是总线卡但没扫到 EtherCAT 从站」时会如实填它，
                // 而 Catalog.BusTypeNames 是从这里生成的 —— 不声明就会写出下拉框候选之外的值、渲染成空白。
                BusTypes = new[] { CardBusType.Pulse, CardBusType.EtherCAT, CardBusType.CANopen, CardBusType.Other },
                DllNames = new[] { "LTDMC.dll", "LTDmcCom.dll", "Dmc1000.dll" }, HasLiveBridge = true,
                Note = "DMC 系列脉冲卡 + EtherCAT/CANopen 总线主站。默认走自有 LtdmcCard 封装（诊断更全）；"
                     + "把「卡型号」写成 Cards/Modules 下登记的卡族键即可改用移植进来的卡族实现。" },

            new CardVendorInfo {
                Vendor = "升立德", DisplayName = "升立德 SLD", Category = "综合型（脉冲+IO+总线）",
                BusTypes = new[] { CardBusType.Pulse, CardBusType.EtherCAT, CardBusType.CANopen, CardBusType.Other },
                DllNames = new[] { "sldmv.dll", "PCI1230.dll", "Pci9014.dll", "Pci9016.dll", "SLD9014PTP.dll", "ecat_motion.dll" }, HasLiveBridge = true,
                Note = "SLD1230/1232 PCIe 脉冲卡、PCI9014/9016/RT3316 脉冲卡、E64 系列 IO 卡、EC600 EtherCAT 总线主站；"
                     + "扩展 IO 走 CAN / EtherCAT 模块。底层库：sldmv.dll / PCI1230.dll / Pci9014.dll / Pci9016.dll / ecat_motion.dll。" },

            new CardVendorInfo {
                Vendor = "恒昱", DisplayName = "恒昱 HY", Category = "脉冲卡",
                BusTypes = new[] { CardBusType.Pulse, CardBusType.CANopen, CardBusType.Other },
                DllNames = new[] { "MCX08.dll", "PCI400.dll", "LTDMC.dll" }, HasLiveBridge = true,
                Note = "HY7X00 系列（7400/7600/7800/7C00）与 MC608 脉冲卡；HY7X00 主驱动走 PCI400.dll，"
                     + "MC608 走 MCX08.dll。支持 HY_CAN_In20Out20 扩展模块。" },

            new CardVendorInfo {
                Vendor = "研控", DisplayName = "研控 YK", Category = "综合型（脉冲+总线）",
                BusTypes = new[] { CardBusType.Pulse, CardBusType.EtherCAT, CardBusType.CANopen, CardBusType.Other },
                DllNames = new[] { "MCC800S.dll", "MCCE135.dll", "MCN420.dll", "MCC1C00.dll", "MCC.dll" }, HasLiveBridge = true,
                Note = "MCC 系列脉冲 / 总线卡（400P/400S/800P/800S/1600P/1200P/E3032）、MCN42 系列与 MCC141C。"
                     + "底层库：MCC800S.dll / MCCE135.dll / MCN420.dll / MCC1C00.dll；"
                     + "MCC.dll 参考实现未随附，400P/800P/1600P/1200P 需另取该库。" },

            new CardVendorInfo {
                Vendor = "模拟卡", DisplayName = "模拟卡（虚拟 / 数字孪生）", Category = "仿真",
                BusTypes = new[] { CardBusType.Pulse, CardBusType.EtherCAT, CardBusType.ModbusTcp, CardBusType.Other },
                DllNames = new string[0], HasLiveBridge = true,
                Note = "纯内存仿真，不驱动任何硬件，用于先写流程后接设备。"
                     + "数字孪生卡另带 Modbus/TCP，可接外部仿真模型。" },

            // 「虚拟」是早期版本写进工程文件的品牌名，保留这一条以免老工程的控制器下拉渲染成空白。
            // 与「模拟卡」指向同一组卡族，只是品牌代号不同。
            new CardVendorInfo {
                Vendor = "虚拟", DisplayName = "虚拟（仿真，旧品牌名）", Category = "仿真",
                BusTypes = new[] { CardBusType.Pulse, CardBusType.EtherCAT, CardBusType.ModbusTcp },
                DllNames = new string[0], HasLiveBridge = true,
                Note = "老工程沿用的品牌代号，等价于「模拟卡」。新工程建议直接用「模拟卡」。" },

            new CardVendorInfo {
                Vendor = "未分类", DisplayName = "未分类（参考实现未标注品牌）", Category = "综合型",
                BusTypes = new[] { CardBusType.Pulse, CardBusType.EtherCAT, CardBusType.CANopen, CardBusType.Other },
                DllNames = new[] { "PLT.dll", "WMX3Api_CLRLib.dll" }, HasLiveBridge = true,
                Note = "这 2 个卡族在参考实现里没有品牌归属（它的分组规则按前缀匹配，"
                     + "「PLT…」和「SoftServo」两个前缀都没覆盖到），因此按型号单独登记，不臆造厂商名。"
                     + "底层库：PLT.dll / WMX3Api_CLRLib.dll（SoftServo 为托管程序集，需另装 WMX3 运行环境）。" },

            // ==================== 仅厂商登记，实时对接尚未实现 ====================

            new CardVendorInfo {
                Vendor = "固高", DisplayName = "固高 Googoltech", Category = "综合型（脉冲+总线）",
                BusTypes = new[] { CardBusType.Pulse, CardBusType.EtherCAT },
                DllNames = new[] { "gts.dll", "GT.dll", "gtsdll.dll" }, HasLiveBridge = false,
                Note = "GT 系列脉冲卡 + EtherCAT 总线主站；待接入对接（需固高 SDK）。" },

            new CardVendorInfo {
                Vendor = "正运动", DisplayName = "正运动 ZMotion", Category = "综合型（脉冲+总线）",
                BusTypes = new[] { CardBusType.Pulse, CardBusType.EtherCAT, CardBusType.CANopen, CardBusType.ModbusTcp },
                DllNames = new[] { "zmotion.dll", "zauxdll.dll", "ZMotion.dll" }, HasLiveBridge = false,
                Note = "ZMC 系列控制器（脉冲+总线）；待接入对接（需正运动 SDK）。" },

            new CardVendorInfo {
                Vendor = "研华", DisplayName = "研华 Advantech", Category = "脉冲卡",
                BusTypes = new[] { CardBusType.Pulse },
                DllNames = new[] { "AdvMotion.dll", "Device.dll" }, HasLiveBridge = false,
                Note = "PCI 脉冲运动控制卡；待接入对接（需研华 DAQMotion SDK）。" },

            new CardVendorInfo {
                Vendor = "汇川", DisplayName = "汇川 Inovance", Category = "总线主站（EtherCAT）",
                BusTypes = new[] { CardBusType.EtherCAT },
                DllNames = new[] { "Inovance.dll", "EasySV.dll" }, HasLiveBridge = false,
                Note = "SV660N 等 EtherCAT 总线伺服；待接入对接（需汇川 SDK）。" },

            new CardVendorInfo {
                Vendor = "台达", DisplayName = "台达 Delta", Category = "总线主站（EtherCAT）",
                BusTypes = new[] { CardBusType.EtherCAT, CardBusType.ModbusTcp },
                DllNames = new[] { "ASDA.dll", "DeltaComm.dll" }, HasLiveBridge = false,
                Note = "ASDA 系列 EtherCAT 总线伺服；待接入对接（需台达 SDK）。" },

            new CardVendorInfo {
                Vendor = "倍福", DisplayName = "倍福 Beckhoff", Category = "总线主站（EtherCAT）",
                BusTypes = new[] { CardBusType.EtherCAT },
                DllNames = new[] { "TcAdsDll.dll", "Beckhoff.dll" }, HasLiveBridge = false,
                Note = "TwinCAT EtherCAT 主站；待接入对接（需倍福 TwinCAT ADS SDK）。" },
        };

        /// <summary>总线类型 → 中文/简称（用于 UI 下拉与登记）。</summary>
        public static string BusTypeName(CardBusType t) => t switch
        {
            CardBusType.Pulse => "脉冲",
            CardBusType.EtherCAT => "EtherCAT",
            CardBusType.CANopen => "CANopen",
            CardBusType.ModbusTcp => "Modbus/TCP",
            CardBusType.ModbusRtu => "Modbus/RTU",
            CardBusType.Profinet => "Profinet",
            _ => "其它"
        };

        /// <summary>该厂商的任意驱动库是否在程序目录 / 系统路径中可见。</summary>
        public static bool DllPresent(CardVendorInfo v)
        {
            if (v.DllNames == null || v.DllNames.Length == 0) return false;
            foreach (var dll in v.DllNames)
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
                catch { /* 探测失败按“没有库”处理 */ }
            }
            return false;
        }

        /// <summary>按品牌代号取厂商信息；找不到回退到第一条（雷赛）。</summary>
        public static CardVendorInfo ByVendor(string vendor) =>
            Vendors.FirstOrDefault(x => x.Vendor == (vendor ?? string.Empty)) ?? Vendors[0];
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
