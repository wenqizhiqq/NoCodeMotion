// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
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
    /// 新增一种卡：在此加一条记录，并在 HardwareSetup / 对应 Bridge 里实现对接即可，
    /// UI 与自动识别会自动识别该品牌与总线类型。
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
        /// <summary>是否已集成真实实时对接（当前仅雷赛）。</summary>
        public bool HasLiveBridge { get; set; }
        /// <summary>备注 / 对接说明。</summary>
        public string Note { get; set; }
    }

    /// <summary>
    /// 主流运动控制卡厂商登记表（脉冲型 + 总线型全覆盖）。
    /// 这是“支持更多类型控制卡”的中央数据源：添加控制器下拉、自动识别、总线分类都读这里。
    /// </summary>
    public static class CardVendorRegistry
    {
        public static readonly List<CardVendorInfo> Vendors = new List<CardVendorInfo>
        {
            new CardVendorInfo {
                Vendor = "雷赛", DisplayName = "雷赛 Leadshine", Category = "综合型（脉冲+总线）",
                BusTypes = new[] { CardBusType.Pulse, CardBusType.EtherCAT, CardBusType.CANopen },
                DllNames = new[] { "LTDMC.dll", "LTDmcCom.dll" }, HasLiveBridge = true,
                Note = "DMC 系列脉冲卡 + EtherCAT/CANopen 总线主站，已集成真实对接（LTDMC.dll）。" },

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

            new CardVendorInfo {
                Vendor = "虚拟", DisplayName = "虚拟（仿真）", Category = "仿真",
                BusTypes = new[] { CardBusType.Pulse, CardBusType.EtherCAT, CardBusType.ModbusTcp },
                DllNames = new string[0], HasLiveBridge = true,
                Note = "纯仿真，不驱动任何硬件，用于先写流程后接设备。" },
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
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
