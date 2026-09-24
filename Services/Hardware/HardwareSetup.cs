// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using NoCodeMotion.Services.Hardware.Cards;
using NoCodeMotion.Services.Hardware.Leadshine;

namespace NoCodeMotion.Services.Hardware
{
    /// <summary>硬件运行模式。</summary>
    public enum HardwareMode
    {
        /// <summary>仿真：轴 / IO / 通讯全部只打日志，不碰任何设备。</summary>
        Simulation,

        /// <summary>雷赛控制卡 + 真实通讯（串口 / 网口 / Modbus）。控制卡不在时轴 IO 只记日志，通讯照样真实。</summary>
        Leadshine,

        /// <summary>
        /// 移植进来的运动控制卡族层（26 个卡族：雷赛 / 升立德 / 恒昱 / 研控 / 未分类 / 模拟卡）
        /// + 真实通讯。卡族匹配不上 / 卡不在时轴 IO 只记日志，通讯照样真实。
        /// </summary>
        CardFamilies
    }

    /// <summary>
    /// 硬件装配入口：决定当前用哪套实现（仿真 / 雷赛自有封装 / 移植卡族层），
    /// 并把结果挂到 <see cref="HardwareBridge.Current"/>。
    ///
    /// 程序启动或首次运行脚本时调用 <see cref="AutoDetectFromProject"/>：
    ///   - 工程里的控制器匹配到「非雷赛」的已移植卡族 → 用卡族层（<see cref="UseCardFamilies"/>）
    ///   - 否则（含只有雷赛控制器的工程、以及还没有控制器的空工程）→ 用雷赛自有封装
    ///     （<see cref="UseLeadshine"/>，诊断信息更全，是既有行为，不变）
    /// 需要完全不碰设备时，显式调用 <see cref="UseSimulation"/>。
    /// 界面上 / Lua 里可以随时用 UseCardFamilies / UseLeadshine / UseSimulation 切换。
    /// </summary>
    public static class HardwareSetup
    {
        private static LeadshineHardwareBridge _leadshine;
        private static WenQiZhiCardBridge _cards;
        private static bool _initialized;
        private static readonly object _gate = new object();

        /// <summary>当前模式。</summary>
        public static HardwareMode Mode { get; private set; } = HardwareMode.Simulation;

        /// <summary>最近一次装配的中文状态说明（可直接显示在状态栏）。</summary>
        public static string StatusMessage { get; private set; } = "仿真模式：未接硬件";

        /// <summary>控制卡是否已就绪。</summary>
        public static bool IsCardReady => Mode switch
        {
            HardwareMode.Leadshine => _leadshine != null && _leadshine.IsCardReady,
            HardwareMode.CardFamilies => _cards != null && _cards.AnyReady,
            _ => false
        };

        /// <summary>当前卡族层对接实例（未启用时为 null），供界面读取各控制器状态。</summary>
        public static WenQiZhiCardBridge CardFamilies => _cards;

        /// <summary>
        /// 启动时自动装配。默认进入「雷赛自有封装 + 真实通讯」模式：
        ///   - 有 LTDMC.dll 且能初始化 → 轴 / IO 走真实控制卡
        ///   - 没有卡 / 没有库          → 轴 / IO 只记日志（不会崩），
        ///     但**串口 / 网口 / Modbus 通讯依然是真实的**，方便先接 PLC 调流程。
        /// 需要完全不碰设备时，显式调用 <see cref="UseSimulation"/>。
        /// </summary>
        public static string AutoDetect() => UseLeadshine();

        /// <summary>
        /// 按当前工程里的控制器配置自动选对接实现：
        /// 只要有控制器匹配到「非雷赛」的已移植卡族，就走卡族层；否则保持雷赛自有封装。
        /// 工程还没加载 / 没有控制器时不会误判，一律回退到雷赛自有封装（既有行为）。
        /// </summary>
        public static string AutoDetectFromProject()
        {
            string why;
            if (WenQiZhiCardBridge.CanServeProject(out why))
            {
                HardwareLog.Write("[硬件] 检测到工程里的控制器需要已移植卡族：" + why);
                return UseCardFamilies();
            }
            return UseLeadshine();
        }

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
                return AutoDetectFromProject();
            }
        }

        /// <summary>切换到仿真（无硬件）模式。</summary>
        public static string UseSimulation()
        {
            ReleaseBridges();
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
            ReleaseBridges();
            _leadshine = new LeadshineHardwareBridge();   // 日志走 HardwareLog
            HardwareBridge.SetBridge(_leadshine);
            Mode = HardwareMode.Leadshine;
            _initialized = true;
            // 这个串同时被 Lua 的 HardwareStatus() 读走，所以把卡型 / 总线状态一起说清楚：
            // 现场「轴不动」第一个要确认的就是「探测到的是脉冲卡还是总线卡、总线通不通」。
            string cardInfo = LtdmcCard.FirstCard?.Summary ?? "未能读取卡信息";
            StatusMessage = _leadshine.IsCardReady
                ? $"雷赛控制卡已连接（卡数量 {LtdmcCard.CardCount}）：{cardInfo}；通讯：串口 / 网口 / Modbus 已就绪"
                : "雷赛控制卡未检测到：轴 / IO 只记录日志；通讯（串口 / 网口 / Modbus）可正常使用";
            HardwareLog.Write("[硬件] " + StatusMessage);
            return StatusMessage;
        }

        /// <summary>
        /// 切换到「已移植运动控制卡族」模式：轴 / IO 走 Services\Hardware\Cards 下移植进来的
        /// 26 个卡族（雷赛 / 升立德 / 恒昱 / 研控 / 未分类 / 模拟卡），通讯照旧走真实串口 / 网口 / Modbus。
        /// 控制器按「卡型号」匹配卡族（见 <see cref="CardFamilyCatalog.Resolve"/>），
        /// 匹配不到或卡没插时轴 IO 只记日志，不会崩。
        /// </summary>
        public static string UseCardFamilies()
        {
            ReleaseBridges();
            _cards = new WenQiZhiCardBridge();   // 日志走 HardwareLog
            HardwareBridge.SetBridge(_cards);
            Mode = HardwareMode.CardFamilies;
            _initialized = true;

            var present = CardFamilyCatalog.DetectPresent();
            StatusMessage = present.Count > 0
                ? $"已移植卡族已就绪：底层库可见的卡族 {present.Count}/{CardFamilyCatalog.Families.Length} 个"
                  + $"（{string.Join("、", present.Select(f => f.Key))}）；控制器首次动作时初始化；"
                  + "通讯（串口 / 网口 / Modbus）可正常使用"
                : "已移植卡族已装配，但没扫到任何卡族的底层库：轴 / IO 只记录日志；"
                  + "通讯（串口 / 网口 / Modbus）可正常使用";
            HardwareLog.Write("[硬件] " + StatusMessage);
            return StatusMessage;
        }

        /// <summary>重新连接控制卡（现场插好卡 / 装好驱动后调用）。</summary>
        public static string Reconnect()
        {
            if (Mode == HardwareMode.CardFamilies)
            {
                if (_cards == null) return UseCardFamilies();
                _cards.Reset();
                StatusMessage = "已移植卡族：已重置连接状态，下次轴 / IO 动作会重新初始化控制卡。"
                    + (_cards.ControllerStatus().Count > 0
                        ? " " + string.Join("；", _cards.ControllerStatus())
                        : string.Empty);
                HardwareLog.Write("[硬件] " + StatusMessage);
                return StatusMessage;
            }

            if (Mode != HardwareMode.Leadshine || _leadshine == null) return UseLeadshine();
            _leadshine.Reconnect(out string message);
            StatusMessage = message;
            return message;
        }

        /// <summary>程序退出时释放（关闭串口 / 网口连接与控制卡）。</summary>
        public static void Shutdown()
        {
            ReleaseBridges();
            HardwareLog.Sink = null;
        }

        private static void ReleaseBridges()
        {
            if (_leadshine != null)
            {
                try { _leadshine.Dispose(); } catch { }
                _leadshine = null;
            }
            if (_cards != null)
            {
                try { _cards.Dispose(); } catch { }
                _cards = null;
            }
        }
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
