// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;

namespace NoCodeMotion.Services.Hardware.Leadshine
{
    /// <summary>
    /// 雷赛控制卡的安全调用层：负责
    ///   1. 探测 / 加载 LTDMC.dll（缺库、位数不符、函数名不符都翻译成中文提示，不让程序崩）
    ///   2. 板卡初始化与关闭（进程内只初始化一次）
    ///   3. 把原生返回的错误码翻译成中文异常
    ///   4. 缓存每个轴已下发的参数（脉冲当量 / 速度曲线），避免每次运动都重复下发
    ///
    /// 上层 <see cref="LeadshineHardwareBridge"/> 只调用本类，不直接碰 P/Invoke。
    /// </summary>
    public sealed class LtdmcCard
    {
        private static readonly object _gate = new object();
        private static bool _initialized;
        private static short _cardCount;

        /// <summary>已下发过参数的轴（key = 卡号:轴号 + 参数指纹），避免重复下发。</summary>
        private readonly ConcurrentDictionary<string, string> _axisProfileCache = new ConcurrentDictionary<string, string>();

        /// <summary>对接日志回调（打到 Lua 输出面板）。</summary>
        public Action<string> Log { get; set; }

        /// <summary>已初始化成功的卡数量（0 表示没有可用的卡）。</summary>
        public static short CardCount => _cardCount;

        /// <summary>控制卡是否已就绪（初始化成功且至少有 1 张卡）。</summary>
        public static bool IsReady => _initialized && _cardCount > 0;

        // ===================== 库探测 / 初始化 =====================

        /// <summary>
        /// LTDMC.dll 是否能被找到。先看 exe 目录，再交给系统按 PATH 查找。
        /// </summary>
        public static bool DllExists()
        {
            try
            {
                string baseDir = AppContext.BaseDirectory ?? string.Empty;
                if (File.Exists(Path.Combine(baseDir, LtdmcNative.Dll))) return true;
                // 交给系统搜索路径（System32 / PATH）
                if (NativeLibrary.TryLoad(LtdmcNative.Dll, out IntPtr h))
                {
                    if (h != IntPtr.Zero) NativeLibrary.Free(h);
                    return true;
                }
            }
            catch { /* 探测失败按“没有库”处理 */ }
            return false;
        }

        /// <summary>
        /// 初始化控制卡。返回是否成功；失败时通过 <paramref name="message"/> 给出中文原因，
        /// 调用方可以据此回落到仿真模式而不抛异常。
        /// </summary>
        public bool TryInitialize(out string message)
        {
            lock (_gate)
            {
                if (_initialized)
                {
                    message = $"雷赛控制卡已初始化，卡数量={_cardCount}";
                    return _cardCount > 0;
                }

                if (!DllExists())
                {
                    message = $"未找到 {LtdmcNative.Dll}。请把雷赛驱动库（与程序位数一致的 32/64 位版本）复制到程序目录：{AppContext.BaseDirectory}";
                    return false;
                }

                try
                {
                    short n = LtdmcNative.dmc_board_init();
                    if (n <= 0)
                    {
                        _initialized = false;
                        message = "雷赛 dmc_board_init 返回 0：没有检测到控制卡。请检查卡是否插好、驱动是否安装、卡电源/网线是否连接。";
                        return false;
                    }

                    _cardCount = n;
                    _initialized = true;
                    message = $"雷赛控制卡初始化成功，卡数量={n}";
                    return true;
                }
                catch (DllNotFoundException)
                {
                    message = $"加载 {LtdmcNative.Dll} 失败：库文件或其依赖缺失。请安装雷赛驱动，并把 DLL 放到程序目录。";
                }
                catch (BadImageFormatException)
                {
                    message = $"{LtdmcNative.Dll} 位数与程序不一致：程序当前是 {(Environment.Is64BitProcess ? "64 位" : "32 位")}，请替换为对应位数的 LTDMC.dll。";
                }
                catch (EntryPointNotFoundException ex)
                {
                    message = $"{LtdmcNative.Dll} 里找不到函数（{ex.Message}）：库版本与声明不一致，请按你的《LTDMC 函数库说明书》修改 LtdmcNative.cs 中对应的一行声明。";
                }
                catch (Exception ex)
                {
                    message = "初始化雷赛控制卡时发生异常：" + ex.Message;
                }
                return false;
            }
        }

        /// <summary>关闭控制卡（程序退出时调用一次）。</summary>
        public static void Close()
        {
            lock (_gate)
            {
                if (!_initialized) return;
                try { LtdmcNative.dmc_board_close(); }
                catch { /* 关闭失败不影响退出 */ }
                _initialized = false;
                _cardCount = 0;
            }
        }

        // ===================== 轴参数 =====================

        /// <summary>
        /// 下发轴的脉冲当量与速度曲线。相同参数只下发一次（按指纹缓存）。
        /// </summary>
        public void ApplyAxisProfile(ushort card, ushort axis,
            double pulsePerUnit, double speed, double accel, double decel, double jerk)
        {
            double maxVel = speed > 0 ? speed : 10;
            double tacc = accel > 0 ? maxVel / accel : 0.1;   // 加速时间 = 速度 / 加速度
            double tdec = decel > 0 ? maxVel / decel : tacc;
            string key = $"{card}:{axis}";
            string finger = $"{pulsePerUnit}|{maxVel}|{tacc}|{tdec}|{jerk}";
            if (_axisProfileCache.TryGetValue(key, out string old) && old == finger) return;

            if (pulsePerUnit > 0)
                Call(() => LtdmcNative.dmc_set_equiv(card, axis, pulsePerUnit), "设置脉冲当量");

            Call(() => LtdmcNative.dmc_set_profile_unit(card, axis, 0, maxVel, tacc, tdec, 0), "设置速度曲线");

            if (jerk > 0)
                Call(() => LtdmcNative.dmc_set_s_profile(card, axis, 0, jerk), "设置 S 形平滑");

            _axisProfileCache[key] = finger;
        }

        /// <summary>只改速度（保留其它曲线参数），用于 SetAxisSpeed。</summary>
        public void SetSpeed(ushort card, ushort axis, double speed, double accel, double decel)
        {
            double maxVel = speed > 0 ? speed : 1;
            double tacc = accel > 0 ? maxVel / accel : 0.1;
            double tdec = decel > 0 ? maxVel / decel : tacc;
            Call(() => LtdmcNative.dmc_set_profile_unit(card, axis, 0, maxVel, tacc, tdec, 0), "设置轴速度");
            _axisProfileCache[$"{card}:{axis}"] = $"speed-only|{maxVel}|{tacc}|{tdec}";
        }

        // ===================== 运动 =====================

        /// <summary>相对定长运动。</summary>
        public void MoveRelative(ushort card, ushort axis, double distance) =>
            Call(() => LtdmcNative.dmc_pmove_unit(card, axis, distance, 0), "相对定长运动");

        /// <summary>绝对定位运动。</summary>
        public void MoveAbsolute(ushort card, ushort axis, double position) =>
            Call(() => LtdmcNative.dmc_pmove_unit(card, axis, position, 1), "绝对定位运动");

        /// <summary>停止轴。immediate=true 立即停止，false 减速停止。</summary>
        public void Stop(ushort card, ushort axis, bool immediate = false) =>
            Call(() => LtdmcNative.dmc_stop(card, axis, (ushort)(immediate ? 1 : 0)), "停止轴");

        /// <summary>轴是否已停止（到位）。</summary>
        public bool IsDone(ushort card, ushort axis) =>
            CallValue(() => LtdmcNative.dmc_check_done(card, axis), "查询轴状态") == 1;

        /// <summary>读取指令位置。</summary>
        public double GetPosition(ushort card, ushort axis)
        {
            try { return LtdmcNative.dmc_get_position_unit(card, axis); }
            catch (Exception ex) { throw Translate(ex, "读取轴位置"); }
        }

        /// <summary>设置 / 清零指令位置。</summary>
        public void SetPosition(ushort card, ushort axis, double pos) =>
            Call(() => LtdmcNative.dmc_set_position_unit(card, axis, pos), "设置轴位置");

        /// <summary>下发回零速度曲线并启动回零。</summary>
        public void Home(ushort card, ushort axis, ushort homeMode,
            double creepSpeed, double homeSpeed, double accel, double decel)
        {
            double high = homeSpeed > 0 ? homeSpeed : 10;
            double low = creepSpeed > 0 ? creepSpeed : Math.Max(high / 10, 0.1);
            double tacc = accel > 0 ? high / accel : 0.1;
            double tdec = decel > 0 ? high / decel : tacc;
            Call(() => LtdmcNative.dmc_set_home_profile_unit(card, axis, low, high, tacc, tdec), "设置回零速度");
            Call(() => LtdmcNative.dmc_home_move(card, axis, homeMode, 0, 0), "启动回零");
        }

        /// <summary>回零是否完成。</summary>
        public bool IsHomeDone(ushort card, ushort axis)
        {
            try { return LtdmcNative.dmc_check_home_done(card, axis) == 1; }
            catch (EntryPointNotFoundException)
            {
                // 部分库版本没有该函数，退化为“轴停止即回零结束”
                return IsDone(card, axis);
            }
            catch (Exception ex) { throw Translate(ex, "查询回零状态"); }
        }

        /// <summary>伺服使能。enable=true 使能（雷赛使能脚一般低电平有效，即写 0）。</summary>
        public void ServoOn(ushort card, ushort axis, bool enable, bool lowActive = true) =>
            Call(() => LtdmcNative.dmc_write_sevon_pin(card, axis,
                (ushort)(enable ? (lowActive ? 0 : 1) : (lowActive ? 1 : 0))), "伺服使能");

        /// <summary>整卡急停。</summary>
        public void EmergencyStop(ushort card) =>
            Call(() => LtdmcNative.dmc_emg_stop(card), "急停");

        // ===================== IO =====================

        /// <summary>写输出位。</summary>
        public void WriteOutBit(ushort card, ushort bitNo, int value) =>
            Call(() => LtdmcNative.dmc_write_outbit(card, bitNo, (ushort)(value != 0 ? 1 : 0)), "写输出位");

        /// <summary>读输出位。</summary>
        public int ReadOutBit(ushort card, ushort bitNo) =>
            CallValue(() => LtdmcNative.dmc_read_outbit(card, bitNo), "读输出位");

        /// <summary>读输入位。</summary>
        public int ReadInBit(ushort card, ushort bitNo) =>
            CallValue(() => LtdmcNative.dmc_read_inbit(card, bitNo), "读输入位");

        // ===================== 错误处理 =====================

        /// <summary>执行一个返回错误码的原生调用，非 0 抛中文异常。</summary>
        private void Call(Func<short> action, string op)
        {
            short rc;
            try { rc = action(); }
            catch (Exception ex) { throw Translate(ex, op); }
            if (rc != 0)
                throw new HardwareOperationException($"雷赛控制卡{op}失败，错误码 {rc}（{DescribeCode(rc)}）");
        }

        /// <summary>执行一个“返回值即数据”的原生调用（如读 IO / 查询状态），负值视为错误。</summary>
        private int CallValue(Func<short> action, string op)
        {
            short rc;
            try { rc = action(); }
            catch (Exception ex) { throw Translate(ex, op); }
            if (rc < 0)
                throw new HardwareOperationException($"雷赛控制卡{op}失败，错误码 {rc}（{DescribeCode(rc)}）");
            return rc;
        }

        private static Exception Translate(Exception ex, string op)
        {
            switch (ex)
            {
                case DllNotFoundException:
                    return new HardwareOperationException($"{op}失败：找不到 {LtdmcNative.Dll}，请把雷赛驱动库放到程序目录。");
                case BadImageFormatException:
                    return new HardwareOperationException($"{op}失败：{LtdmcNative.Dll} 位数与程序不一致（当前进程 {(Environment.Is64BitProcess ? "64 位" : "32 位")}）。");
                case EntryPointNotFoundException e2:
                    return new HardwareOperationException($"{op}失败：库中没有该函数（{e2.Message}），请按手册核对 LtdmcNative.cs 的声明。");
                case HardwareOperationException:
                    return ex;
                default:
                    return new HardwareOperationException($"{op}时发生异常：{ex.Message}");
            }
        }

        /// <summary>常见错误码的中文解释（具体码值以你手上的雷赛手册为准）。</summary>
        private static string DescribeCode(short code)
        {
            switch (code)
            {
                case 0: return "成功";
                case 1: return "卡号或轴号超出范围";
                case 2: return "参数超出允许范围";
                case 3: return "该功能当前不支持或未初始化";
                case 4: return "轴正在运动中，无法执行该指令";
                case 5: return "通讯 / 驱动异常，请检查连线与驱动";
                case -1: return "调用失败，通常是未初始化或句柄失效";
                default: return "请查阅雷赛《LTDMC 函数库说明书》错误码表";
            }
        }
    }

    /// <summary>硬件操作失败异常（消息为中文，会直接显示在 Lua 输出面板）。</summary>
    public sealed class HardwareOperationException : Exception
    {
        public HardwareOperationException(string message) : base(message) { }
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
