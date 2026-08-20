#nullable disable
using System;
using NoCodeMotion.Services.Hardware.Leadshine;

namespace NoCodeMotion.Services.Hardware
{
    /// <summary>硬件运行模式。</summary>
    public enum HardwareMode
    {
        /// <summary>仿真：轴 / IO / 通讯全部只打日志，不碰任何设备。</summary>
        Simulation,

        /// <summary>雷赛控制卡 + 真实通讯（串口 / 网口 / Modbus）。控制卡不在时轴 IO 只记日志，通讯照样真实。</summary>
        Leadshine
    }

    /// <summary>
    /// 硬件装配入口：决定当前用哪套实现（仿真 / 雷赛 + 真实通讯），
    /// 并把结果挂到 <see cref="HardwareBridge.Current"/>。
    ///
    /// 程序启动时调用 <see cref="AutoDetect"/>：
    ///   - 程序目录有 LTDMC.dll → 用雷赛（能初始化就是真控卡，初始化不了也能用真实通讯）
    ///   - 没有 LTDMC.dll     → 用仿真桩，保证没有硬件也能跑流程
    /// 界面上可以随时用 <see cref="UseLeadshine"/> / <see cref="UseSimulation"/> 切换。
    /// </summary>
    public static class HardwareSetup
    {
        private static LeadshineHardwareBridge _leadshine;
        private static bool _initialized;
        private static readonly object _gate = new object();

        /// <summary>当前模式。</summary>
        public static HardwareMode Mode { get; private set; } = HardwareMode.Simulation;

        /// <summary>最近一次装配的中文状态说明（可直接显示在状态栏）。</summary>
        public static string StatusMessage { get; private set; } = "仿真模式：未接硬件";

        /// <summary>控制卡是否已就绪。</summary>
        public static bool IsCardReady => Mode == HardwareMode.Leadshine && _leadshine != null && _leadshine.IsCardReady;

        /// <summary>
        /// 启动时自动装配。默认进入「雷赛 + 真实通讯」模式：
        ///   - 有 LTDMC.dll 且能初始化 → 轴 / IO 走真实控制卡
        ///   - 没有卡 / 没有库          → 轴 / IO 只记日志（不会崩），
        ///     但**串口 / 网口 / Modbus 通讯依然是真实的**，方便先接 PLC 调流程。
        /// 需要完全不碰设备时，显式调用 <see cref="UseSimulation"/>。
        /// </summary>
        public static string AutoDetect() => UseLeadshine();

        /// <summary>
        /// 第一次需要硬件时自动装配一次（Lua 会话在运行脚本前调用），
        /// 之后再调用不会重复初始化，也不会覆盖用户在界面上手动切换的模式。
        /// </summary>
        public static string EnsureInitialized()
        {
            lock (_gate)
            {
                if (_initialized) return StatusMessage;
                _initialized = true;
                return AutoDetect();
            }
        }

        /// <summary>切换到仿真（无硬件）模式。</summary>
        public static string UseSimulation()
        {
            ReleaseLeadshine();
            HardwareBridge.SetBridge(new StubHardwareBridge());
            Mode = HardwareMode.Simulation;
            _initialized = true;
            StatusMessage = "仿真模式：轴 / IO / 通讯只打印日志，不驱动设备";
            HardwareLog.Write("[硬件] " + StatusMessage);
            return StatusMessage;
        }

        /// <summary>切换到雷赛控制卡 + 真实通讯模式。</summary>
        public static string UseLeadshine()
        {
            ReleaseLeadshine();
            _leadshine = new LeadshineHardwareBridge();   // 日志走 HardwareLog
            HardwareBridge.SetBridge(_leadshine);
            Mode = HardwareMode.Leadshine;
            _initialized = true;
            StatusMessage = _leadshine.IsCardReady
                ? $"雷赛控制卡已连接（卡数量 {LtdmcCard.CardCount}），通讯：串口 / 网口 / Modbus 已就绪"
                : "雷赛控制卡未检测到：轴 / IO 只记录日志；通讯（串口 / 网口 / Modbus）可正常使用";
            HardwareLog.Write("[硬件] " + StatusMessage);
            return StatusMessage;
        }

        /// <summary>重新连接控制卡（现场插好卡 / 装好驱动后调用）。</summary>
        public static string Reconnect()
        {
            if (Mode != HardwareMode.Leadshine || _leadshine == null) return UseLeadshine();
            _leadshine.Reconnect(out string message);
            StatusMessage = message;
            return message;
        }

        /// <summary>程序退出时释放（关闭串口 / 网口连接与控制卡）。</summary>
        public static void Shutdown()
        {
            ReleaseLeadshine();
            HardwareLog.Sink = null;
        }

        private static void ReleaseLeadshine()
        {
            if (_leadshine == null) return;
            try { _leadshine.Dispose(); } catch { }
            _leadshine = null;
        }
    }
}
