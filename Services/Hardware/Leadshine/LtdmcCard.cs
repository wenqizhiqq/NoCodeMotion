// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‌启‏志‌◆‍编‎写⁣◇‏微⁣信​﹕‍1⁠8‏7⁠◆‍1‌9‎3⁣6⁣◇‍1‌3⁠9​9⁣　‏※⁣保⁣留‌所⁣有‏权‎利‍请‏勿‎删⁠除​◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace NoCodeMotion.Services.Hardware.Leadshine
{
    /// <summary>
    /// 雷赛控制卡的安全调用层：负责
    ///   1. 探测 / 加载 LTDMC.dll（缺库、位数不符、函数名不符都翻译成中文提示，不让程序崩）
    ///   2. 板卡初始化与关闭（进程内只初始化一次），并读出卡信息、判定脉冲卡 / 总线卡
    ///   3. 把原生返回的错误码翻译成中文异常
    ///   4. 缓存每个轴已下发的参数（脉冲当量 / 速度曲线），避免每次运动都重复下发
    ///   5. 屏蔽「脉冲卡」与「总线卡」两套寻址方式的差异（IO 位号 vs 从站节点号 + 站内位号）
    ///
    /// 上层 <see cref="LeadshineHardwareBridge"/> 只调用本类，不直接碰 P/Invoke。
    /// </summary>
    public sealed class LtdmcCard
    {
        private static readonly object _gate = new object();
        private static bool _initialized;
        private static short _cardCount;
        private static ushort _firstCardNo;
        private static CardInfo _firstCard;

        /// <summary>已下发过参数的轴（key = 卡号:轴号），避免重复下发。</summary>
        private readonly ConcurrentDictionary<string, string> _axisProfileCache = new ConcurrentDictionary<string, string>();

        /// <summary>对接日志回调（打到 Lua 输出面板）。</summary>
        public Action<string> Log { get; set; }

        /// <summary>已初始化成功的卡数量（0 表示没有可用的卡）。</summary>
        public static short CardCount => _cardCount;

        /// <summary>控制卡是否已就绪（初始化成功且至少有 1 张卡）。</summary>
        public static bool IsReady => _initialized && _cardCount > 0;

        /// <summary>第一张卡的卡号（单卡系统即唯一卡号）。</summary>
        public static ushort FirstCardNo => _firstCardNo;

        /// <summary>第一张卡的详细信息（未初始化时为 null）。</summary>
        public static CardInfo FirstCard => _firstCard;

        /// <summary>当前卡是否为总线卡（EtherCAT / CANopen 主站）。轴与 IO 要走 nmc_ 族。</summary>
        public static bool IsBusCard => _firstCard != null && _firstCard.IsBusCard;

        /// <summary>
        /// 卡型 → 控制器页面「总线类型」下拉里的取值。
        /// ★ 返回值必须落在 Catalog.BusTypeNames 里（下拉框只认这些），否则当前值会渲染成空白。
        /// 「是总线卡、但没在 EtherCAT 端口上扫到从站」时如实返回「其它」，不硬说是 EtherCAT。
        /// </summary>
        public static string DescribeBusType(CardInfo info)
        {
            if (info == null || !info.IsBusCard) return "脉冲";
            return info.BusSlaves > 0 ? "EtherCAT" : "其它";
        }

        // ===================== 卡信息 =====================

        /// <summary>一张控制卡的探测结果。</summary>
        public sealed class CardInfo
        {
            public ushort CardNo;
            public uint CardType;

            /// <summary>总线轴数（nmc_get_total_axes）。脉冲卡为 0。</summary>
            public uint BusAxes;

            /// <summary>本地轴数（dmc_get_total_axes）。总线卡一般也有本地轴槽位。</summary>
            public uint LocalAxes;

            /// <summary>EtherCAT 端口上的从站数量。</summary>
            public ushort BusSlaves;

            /// <summary>总线错误码（0 = 正常）。</summary>
            public ushort BusErrCode;

            /// <summary>本地输入 / 输出点数。</summary>
            public ushort LocalIn;
            public ushort LocalOut;

            /// <summary>总线输入 / 输出点数。</summary>
            public ushort BusIn;
            public ushort BusOut;

            /// <summary>AD / DA 通道数。</summary>
            public ushort AdIn;
            public ushort DaOut;

            /// <summary>判定为总线卡：总线轴数或从站数大于 0。</summary>
            public bool IsBusCard => BusAxes > 0 || BusSlaves > 0;

            /// <summary>一行中文诊断，直接写日志 / 状态栏。</summary>
            public string Summary
            {
                get
                {
                    var sb = new StringBuilder();
                    sb.Append($"卡号 {CardNo}（卡型 0x{CardType:X}）：");
                    if (IsBusCard)
                    {
                        sb.Append($"总线卡 —— EtherCAT 从站 {BusSlaves} 个、总线轴 {BusAxes} 根");
                        if (BusIn > 0 || BusOut > 0) sb.Append($"，总线 IO 入 {BusIn} / 出 {BusOut}");
                        sb.Append(BusErrCode == 0 ? "，总线状态正常" : $"，★总线错误码 {BusErrCode}（EtherCAT 未正常通信，先查网线 / 从站上电）");
                        sb.Append("；轴与 IO 走 nmc_ 族");
                    }
                    else
                    {
                        sb.Append($"脉冲卡 —— 本地轴 {LocalAxes} 根");
                        if (LocalIn > 0 || LocalOut > 0) sb.Append($"，本地 IO 入 {LocalIn} / 出 {LocalOut}");
                        sb.Append("；轴与 IO 走 dmc_ 族");
                    }
                    if (AdIn > 0 || DaOut > 0) sb.Append($"，AD {AdIn} / DA {DaOut}");
                    return sb.ToString();
                }
            }
        }

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
        ///
        /// 成功时 <paramref name="message"/> 里带完整诊断（卡号 / 脉冲卡还是总线卡 / 轴数 / IO 点数 /
        /// 总线错误码），现场"轴不动"时先看这一行。
        /// </summary>
        public bool TryInitialize(out string message)
        {
            lock (_gate)
            {
                if (_initialized)
                {
                    message = $"雷赛控制卡已初始化，卡数量={_cardCount}" + DescribeFirstCard();
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
                        message = "雷赛 dmc_board_init 返回 0：没有检测到控制卡。请检查卡是否插好、驱动是否安装、卡电源 / 网线是否连接。";
                        return false;
                    }

                    _cardCount = n;
                    _firstCardNo = ResolveFirstCardNo();
                    _firstCard = Probe(_firstCardNo);
                    _initialized = true;

                    message = $"雷赛控制卡初始化成功，卡数量={n}。" + (_firstCard?.Summary ?? $"卡号 {_firstCardNo}（未能读取卡信息）");
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

        /// <summary>
        /// 读卡号列表，取第一张卡的卡号。
        /// 注意第 3 个参数必须是 <c>ushort[]</c>：官方例程就是
        /// <c>LTDMC.dmc_get_CardInfList(ref num, cardTypes /*uint[]*/, cardIds /*ushort[]*/)</c>。
        /// </summary>
        private static ushort ResolveFirstCardNo()
        {
            try
            {
                ushort num = 0;
                uint[] types = new uint[8];
                ushort[] ids = new ushort[8];
                short rc = LtdmcNative.dmc_get_CardInfList(ref num, types, ids);
                if (rc == 0 && num > 0 && ids[0] != 0xFFFF) return ids[0];
            }
            catch { /* 取不到就退回 0，单卡系统卡号通常就是 0 */ }
            return 0;
        }

        /// <summary>
        /// 探测一张卡的资源：轴数、IO 点数、AD/DA、EtherCAT 从站数与总线错误码。
        /// 全部按「最佳努力」——任一函数不被当前卡型号支持都只留空，不影响初始化。
        /// </summary>
        public CardInfo Probe(ushort cardNo)
        {
            var info = new CardInfo { CardNo = cardNo, CardType = ReadCardType(cardNo) };

            Try(() => { uint v = 0; if (LtdmcNative.dmc_get_total_axes(cardNo, ref v) == 0) info.LocalAxes = v; });
            Try(() => { ushort a = 0, b = 0; if (LtdmcNative.dmc_get_total_ionum(cardNo, ref a, ref b) == 0) { info.LocalIn = a; info.LocalOut = b; } });
            Try(() => { ushort a = 0, b = 0; if (LtdmcNative.dmc_get_total_adcnum(cardNo, ref a, ref b) == 0) { info.AdIn = a; info.DaOut = b; } });

            // 总线部分：脉冲卡上这些调用会失败（返回非 0），失败就当作"不是总线卡"
            Try(() => { uint v = 0; if (LtdmcNative.nmc_get_total_axes(cardNo, ref v) == 0) info.BusAxes = v; });
            Try(() => { ushort v = 0; if (LtdmcNative.nmc_get_total_slaves(cardNo, LtdmcNative.EtherCatPort, ref v) == 0) info.BusSlaves = v; });
            Try(() => { ushort v = 0; if (LtdmcNative.nmc_get_total_ionum(cardNo, ref v, ref info.BusOut) == 0) info.BusIn = v; });
            Try(() => { ushort v = 0; if (LtdmcNative.nmc_get_errcode(cardNo, LtdmcNative.EtherCatPort, ref v) == 0) info.BusErrCode = v; });

            return info;
        }

        private static uint ReadCardType(ushort cardNo)
        {
            try
            {
                ushort num = 0;
                uint[] types = new uint[8];
                ushort[] ids = new ushort[8];
                if (LtdmcNative.dmc_get_CardInfList(ref num, types, ids) != 0) return 0;
                for (int i = 0; i < num && i < ids.Length; i++)
                    if (ids[i] == cardNo) return types[i];
            }
            catch { }
            return 0;
        }

        private static string DescribeFirstCard() =>
            _firstCard == null ? string.Empty : "。" + _firstCard.Summary;

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
                _firstCard = null;
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

        /// <summary>在线变速（运动过程中改速度，不用停轴）。</summary>
        public void ChangeSpeed(ushort card, ushort axis, double newSpeed, double taccDec) =>
            Call(() => LtdmcNative.dmc_change_speed_unit(card, axis, newSpeed, taccDec), "在线变速");

        /// <summary>在线变位（运动过程中改目标位置）。</summary>
        public void ResetTargetPosition(ushort card, ushort axis, double newPos) =>
            Call(() => LtdmcNative.dmc_reset_target_position_unit(card, axis, newPos), "在线变位");

        /// <summary>读取当前速度。</summary>
        public double GetSpeed(ushort card, ushort axis)
        {
            double v = 0;
            try { LtdmcNative.dmc_read_current_speed_unit(card, axis, ref v); }
            catch (Exception ex) { throw Translate(ex, "读取轴速度"); }
            return v;
        }

        // ===================== 运动 =====================

        /// <summary>相对定长运动。</summary>
        public void MoveRelative(ushort card, ushort axis, double distance) =>
            Call(() => LtdmcNative.dmc_pmove_unit(card, axis, distance, 0), "相对定长运动");

        /// <summary>绝对定位运动。</summary>
        public void MoveAbsolute(ushort card, ushort axis, double position) =>
            Call(() => LtdmcNative.dmc_pmove_unit(card, axis, position, 1), "绝对定位运动");

        /// <summary>连续（Jog）运动。dir：0 负向，1 正向。</summary>
        public void Jog(ushort card, ushort axis, bool positive) =>
            Call(() => LtdmcNative.dmc_vmove(card, axis, (ushort)(positive ? 1 : 0)), "连续运动");

        /// <summary>停止轴。immediate=true 立即停止，false 减速停止。</summary>
        public void Stop(ushort card, ushort axis, bool immediate = false) =>
            Call(() => LtdmcNative.dmc_stop(card, axis, (ushort)(immediate ? 1 : 0)), "停止轴");

        /// <summary>轴是否已停止（到位）。</summary>
        public bool IsDone(ushort card, ushort axis) =>
            CallValue(() => LtdmcNative.dmc_check_done(card, axis), "查询轴状态") == 1;

        /// <summary>读取指令位置（单位模式）。</summary>
        public double GetPosition(ushort card, ushort axis)
        {
            double pos = 0;
            try
            {
                // ★ 必须传 ref 出参。原来的 double 返回值声明会让原生函数往野指针写内存。
                short rc = LtdmcNative.dmc_get_position_unit(card, axis, ref pos);
                if (rc != 0) throw new HardwareOperationException($"雷赛控制卡读取轴位置失败，错误码 {rc}（{DescribeCode(rc)}）");
                return pos;
            }
            catch (Exception ex) { throw Translate(ex, "读取轴位置"); }
        }

        /// <summary>设置 / 清零指令位置。</summary>
        public void SetPosition(ushort card, ushort axis, double pos) =>
            Call(() => LtdmcNative.dmc_set_position_unit(card, axis, pos), "设置轴位置");

        /// <summary>读取轴 IO 状态位（bit0 负限位 / bit1 正限位 / bit2 原点 / bit3 EZ / bit4 伺服报警 / bit5 急停）。</summary>
        /// <summary>
        /// 读取编码器反馈位置（单位模式）。
        /// 官方例 1 用它和指令位置对比：指令位置在走、编码器不动 → 没使能 / 动力线没接 / 编码器线松。
        /// </summary>
        public double GetEncoder(ushort card, ushort axis)
        {
            double pos = 0;
            try
            {
                short rc = LtdmcNative.dmc_get_encoder_unit(card, axis, ref pos);
                if (rc != 0) throw new HardwareOperationException($"雷赛控制卡读取编码器位置失败，错误码 {rc}（{DescribeCode(rc)}）");
                return pos;
            }
            catch (Exception ex) { throw Translate(ex, "读取编码器位置"); }
        }

        public uint ReadAxisIoStatus(ushort card, ushort axis)
        {
            try { return LtdmcNative.dmc_axis_io_status(card, axis); }
            catch (Exception ex) { throw Translate(ex, "读取轴 IO 状态"); }
        }

        /// <summary>读取轴停止原因（撞限位 / 急停 / 报警时用来看是被什么停下的）。</summary>
        public int GetStopReason(ushort card, ushort axis)
        {
            int reason = 0;
            try { LtdmcNative.dmc_get_stop_reason(card, axis, ref reason); }
            catch (Exception ex) { throw Translate(ex, "读取停止原因"); }
            return reason;
        }

        /// <summary>清除轴停止原因。</summary>
        public void ClearStopReason(ushort card, ushort axis) =>
            Call(() => LtdmcNative.dmc_clear_stop_reason(card, axis), "清除停止原因");

        /// <summary>整卡急停。</summary>
        public void EmergencyStop(ushort card) =>
            Call(() => LtdmcNative.dmc_emg_stop(card), "急停");

        // ===================== 回零 =====================

        /// <summary>
        /// 下发回零速度曲线并启动回零。
        /// 脉冲卡与总线卡的回零参数下发函数不同，这里按卡型自动分派：
        ///   - 脉冲卡：dmc_set_home_profile_unit（无回零模式参数）+ dmc_home_move(卡号, 轴号)
        ///   - 总线卡：nmc_set_home_profile（带回零模式与零点偏移）+ nmc_home_move(卡号, 轴号)
        /// </summary>
        public void Home(ushort card, ushort axis, ushort homeMode,
            double creepSpeed, double homeSpeed, double accel, double decel, double offset = 0)
        {
            double high = homeSpeed > 0 ? homeSpeed : 10;
            double low = creepSpeed > 0 ? creepSpeed : Math.Max(high / 10, 0.1);
            double tacc = accel > 0 ? high / accel : 0.1;
            double tdec = decel > 0 ? high / decel : tacc;

            if (IsBusCardFor(card))
            {
                Call(() => LtdmcNative.nmc_set_home_profile(card, axis, homeMode, low, high, tacc, tdec, offset), "设置总线回零参数");
                Call(() => LtdmcNative.nmc_home_move(card, axis), "启动总线回零");
            }
            else
            {
                Call(() => LtdmcNative.dmc_set_home_profile_unit(card, axis, low, high, tacc, tdec), "设置回零速度");
                Call(() => LtdmcNative.dmc_home_move(card, axis), "启动回零");
            }
        }

        /// <summary>
        /// 回零是否完成。
        /// 用 dmc_get_home_result（state==1 表示成功）—— LTDMC.dll 里并没有 dmc_check_home_done。
        /// 部分型号该函数返回失败，此时退化为「轴已停止即视为结束」。
        /// </summary>
        public bool IsHomeDone(ushort card, ushort axis)
        {
            try
            {
                ushort state = 0;
                short rc = LtdmcNative.dmc_get_home_result(card, axis, ref state);
                if (rc == 0) return state == 1;
                return IsDone(card, axis);   // 该型号不支持，退化判断
            }
            catch (EntryPointNotFoundException)
            {
                return IsDone(card, axis);
            }
            catch (Exception ex) { throw Translate(ex, "查询回零状态"); }
        }

        // ===================== 伺服使能 =====================

        /// <summary>
        /// 伺服使能，按卡型自动分派，返回实际走了哪条路（用于日志）。
        ///   - 总线卡：nmc_set_axis_enable / nmc_set_axis_disable（CiA402 状态机，总线伺服没有本地使能脚）
        ///   - 脉冲卡：dmc_write_sevon_pin（使能脚，雷赛默认低电平有效）
        /// ★ 总线卡上错用 dmc_write_sevon_pin 的典型现象：函数返回 0 但轴不动（状态机停在 1/2）。
        /// </summary>
        public string SetServoEnable(ushort card, ushort axis, bool enable, bool lowActive = true)
        {
            if (IsBusCardFor(card))
            {
                if (enable) Call(() => LtdmcNative.nmc_set_axis_enable(card, axis), "总线轴使能");
                else Call(() => LtdmcNative.nmc_set_axis_disable(card, axis), "总线轴失能");
                return enable ? "nmc_set_axis_enable（总线伺服使能）" : "nmc_set_axis_disable（总线伺服失能）";
            }

            Call(() => LtdmcNative.dmc_write_sevon_pin(card, axis,
                (ushort)(enable ? (lowActive ? 0 : 1) : (lowActive ? 1 : 0))), "伺服使能");
            return enable ? "dmc_write_sevon_pin（脉冲卡使能脚）" : "dmc_write_sevon_pin（脉冲卡断开使能脚）";
        }

        /// <summary>兼容旧调用：伺服使能（自动按卡型分派）。</summary>
        public void ServoOn(ushort card, ushort axis, bool enable, bool lowActive = true) =>
            SetServoEnable(card, axis, enable, lowActive);

        /// <summary>
        /// 读取总线轴状态机（CiA402）：4 = 操作使能（真正能动的状态），其它值都动不了。
        /// 脉冲卡不支持，返回 -1。
        /// </summary>
        public int GetAxisStateMachine(ushort card, ushort axis)
        {
            try
            {
                ushort st = 0;
                short rc = LtdmcNative.nmc_get_axis_state_machine(card, axis, ref st);
                return rc == 0 ? st : -1;
            }
            catch { return -1; }
        }

        /// <summary>总线轴状态机数值 → 中文（轴不动时先看这个）。</summary>
        public static string DescribeAxisState(int state) => state switch
        {
            0 => "未启动",
            1 => "启动禁止",
            2 => "准备启动",
            3 => "启动",
            4 => "操作使能（可运动）",
            5 => "停止",
            6 => "错误触发",
            7 => "错误",
            _ => "未知（该卡型不支持读取状态机）"
        };

        /// <summary>读取总线轴错误码（伺服自身报警码）。</summary>
        public int GetAxisErrCode(ushort card, ushort axis)
        {
            ushort code = 0;
            try { LtdmcNative.nmc_get_axis_errcode(card, axis, ref code); }
            catch { return -1; }
            return code;
        }

        /// <summary>清除总线轴错误码。</summary>
        public void ClearAxisErrCode(ushort card, ushort axis) =>
            Call(() => LtdmcNative.nmc_clear_axis_errcode(card, axis), "清除总线轴错误码");

        /// <summary>读取总线错误码（0 = 正常）。</summary>
        public int GetBusErrCode(ushort card)
        {
            ushort code = 0;
            try
            {
                if (LtdmcNative.nmc_get_errcode(card, LtdmcNative.EtherCatPort, ref code) != 0) return -1;
            }
            catch { return -1; }
            return code;
        }

        // ===================== 脉冲卡初始化（机上最佳努力） =====================

        /// <summary>
        /// 设置脉冲输出模式。0=脉冲+方向（最常见）、1=双脉冲、2=CW/CCW 等。
        /// 仅脉冲卡有意义；总线卡的轴运动由总线协议决定，不需要这个。
        /// </summary>
        public void SetPulseOutmode(ushort card, ushort axis, ushort mode) =>
            Call(() => LtdmcNative.dmc_set_pulse_outmode(card, axis, mode), "设置脉冲输出模式");

        /// <summary>设置减速停止时间（秒）。影响限位触发、急停、减速停止指令的减速过程，合理设置可防止撞机。</summary>
        public void SetDecStopTime(ushort card, ushort axis, double stopTime) =>
            Call(() => LtdmcNative.dmc_set_dec_stop_time(card, axis, stopTime), "设置减速停止时间");

        /// <summary>设置总线轴运行模式（1 位置 / 3 速度 / 4 力矩 / 6 回零）。</summary>
        public void SetAxisRunMode(ushort card, ushort axis, ushort runMode) =>
            Call(() => LtdmcNative.nmc_set_axis_run_mode(card, axis, runMode), "设置总线轴运行模式");

        // ===================== IO =====================

        /// <summary>写输出位（脉冲卡：按整卡位号）。</summary>
        public void WriteOutBit(ushort card, ushort bitNo, int value) =>
            Call(() => LtdmcNative.dmc_write_outbit(card, bitNo, (ushort)(value != 0 ? 1 : 0)), "写输出位");

        /// <summary>读输出位（脉冲卡）。</summary>
        public int ReadOutBit(ushort card, ushort bitNo) =>
            CallValue(() => LtdmcNative.dmc_read_outbit(card, bitNo), "读输出位");

        /// <summary>读输入位（脉冲卡）。</summary>
        public int ReadInBit(ushort card, ushort bitNo) =>
            CallValue(() => LtdmcNative.dmc_read_inbit(card, bitNo), "读输入位");

        /// <summary>读输入位（总线卡：从站节点号 + 站内位号）。</summary>
        public int ReadInBitBus(ushort card, ushort nodeId, ushort ioBit)
        {
            ushort v = 0;
            try
            {
                short rc = LtdmcNative.nmc_read_inbit(card, nodeId, ioBit, ref v);
                if (rc != 0) throw new HardwareOperationException($"雷赛控制卡读总线输入失败，错误码 {rc}（{DescribeCode(rc)}）");
            }
            catch (Exception ex) { throw Translate(ex, "读总线输入位"); }
            return v != 0 ? 1 : 0;
        }

        /// <summary>读输出位（总线卡）。</summary>
        public int ReadOutBitBus(ushort card, ushort nodeId, ushort ioBit)
        {
            ushort v = 0;
            try
            {
                short rc = LtdmcNative.nmc_read_outbit(card, nodeId, ioBit, ref v);
                if (rc != 0) throw new HardwareOperationException($"雷赛控制卡读总线输出失败，错误码 {rc}（{DescribeCode(rc)}）");
            }
            catch (Exception ex) { throw Translate(ex, "读总线输出位"); }
            return v != 0 ? 1 : 0;
        }

        /// <summary>写输出位（总线卡）。</summary>
        public void WriteOutBitBus(ushort card, ushort nodeId, ushort ioBit, int value) =>
            Call(() => LtdmcNative.nmc_write_outbit(card, nodeId, ioBit, (ushort)(value != 0 ? 1 : 0)), "写总线输出位");

        // ===================== 内部 =====================

        /// <summary>按卡号判断是不是总线卡；只有第一张卡有完整探测结果时按全局判定。</summary>
        private static bool IsBusCardFor(ushort card)
        {
            if (_firstCard != null && _firstCard.CardNo == card) return _firstCard.IsBusCard;
            return IsBusCard;
        }

        /// <summary>执行一个"探测型"调用：任何异常都吞掉（用于初始化阶段的最佳努力探测）。</summary>
        private static void Try(Action action)
        {
            try { action(); } catch { }
        }

        /// <summary>执行一个返回错误码的原生调用，非 0 抛中文异常。</summary>
        private void Call(Func<short> action, string op)
        {
            short rc;
            try { rc = action(); }
            catch (Exception ex) { throw Translate(ex, op); }
            if (rc != 0)
                throw new HardwareOperationException($"雷赛控制卡{op}失败，错误码 {rc}（{DescribeCode(rc)}）");
        }

        /// <summary>执行一个"返回值即数据"的原生调用（如读 IO / 查询状态），负值视为错误。</summary>
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

// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
