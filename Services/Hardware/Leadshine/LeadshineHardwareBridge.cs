// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁠志⁠◆​编⁠写​◇​微‌信‌﹕‍1‍8‌7⁣◆​1‎9​3‎6⁣◇‍1⁣3‎9‎9‍　‏※‏保​留‍所​有‌权⁣利⁠请‌勿⁣删‍除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using MoonSharp.Interpreter;
using NoCodeMotion.Models;
using NoCodeMotion.Services.Hardware.Comm;

namespace NoCodeMotion.Services.Hardware.Leadshine
{
    /// <summary>
    /// 真实硬件对接实现：轴 / IO / 气缸 / 料盘走雷赛（Leadshine）DMC 控制卡，
    /// 通讯走真实串口 / 网口 / Modbus（<see cref="CommManager"/>）。
    ///
    /// 启用方式（程序启动处，或“硬件设置”里切换）：
    /// <code>
    /// HardwareBridge.Current = new LeadshineHardwareBridge(msg => 输出日志(msg));
    /// </code>
    ///
    /// 设计原则：
    ///   - 控制卡不在（没插卡 / 没装驱动 / 库位数不符）时不崩：轴 IO 动作只记警告日志，
    ///     通讯部分照样真实可用，方便先接 PLC 调流程。
    ///   - 出错信息全中文，直接显示在 Lua 输出面板。
    ///
    /// ★ 脉冲卡与总线卡（EtherCAT，DMC-E 3000 / 5000 系列）的差异
    ///   —— 下面每条都对着官方例程核过，不是猜的：
    ///
    ///   1) 运动指令两卡通用，都走 dmc_ 族（官方例 1「定长运动」、例 7「回原点运动」实测）：
    ///      dmc_set_profile_unit → dmc_pmove_unit / dmc_vmove、dmc_get_position_unit(带 ref 出参)、
    ///      dmc_check_done、dmc_set_position_unit、dmc_change_speed_unit、dmc_stop。
    ///      也就是说总线卡上「轴号」仍然是一个整数索引，不需要换算成从站地址。
    ///
    ///   2) 伺服使能不一样（这是「指令返回 0 但轴不动」的头号原因）：
    ///      总线卡 → nmc_set_axis_enable / nmc_set_axis_disable（CiA402 状态机，总线伺服没有本地使能脚）；
    ///      脉冲卡 → dmc_write_sevon_pin（本地使能脚，雷赛默认低电平有效）。
    ///      总线轴只有在状态机 = 4（操作使能）时才会动，本类会在使能后与超时时把这个值打出来。
    ///
    ///   3) 回零参数下发不一样：
    ///      总线卡 → nmc_set_home_profile(卡号, 轴号, 回零模式, 低速, 高速, 加速时间, 减速时间, 零点偏移)
    ///               + nmc_home_move(卡号, 轴号)；
    ///      脉冲卡 → dmc_set_home_profile_unit + dmc_home_move。
    ///      回零是否完成统一读 dmc_get_home_result（state=1 为成功；LTDMC.dll 里没有 dmc_check_home_done）。
    ///      以上分派由 <see cref="LtdmcCard.Home"/> 自动完成。
    ///
    ///   4) IO 寻址：官方例 8 在总线卡上仍然是 dmc_read_inbit(卡号, 位号) / dmc_write_outbit(卡号, 位号)
    ///      按「整卡位号」逐位访问，点数从 nmc_get_total_ionum 读。
    ///      官方 19 个例程里没有任何一个用 nmc_read_inbit，所以本类默认沿用整卡位号，
    ///      不擅自改成「从站节点号 + 站内位号」，免得现有接线静默读错点。
    ///      若你的 EtherCAT IO 从站必须按节点号访问，把 <see cref="Options.BusIoAddressing"/> 设为 true。
    ///
    ///   5) 诊断：总线卡看 nmc_get_axis_state_machine + nmc_get_axis_errcode + nmc_get_errcode(卡号, 2)，
    ///      脉冲卡看 dmc_axis_io_status + dmc_get_stop_reason。超时异常里会带上这些读数。
    /// </summary>
    public sealed class LeadshineHardwareBridge : IHardwareBridge, IDisposable
    {
        /// <summary>可调参数（按现场接线习惯改这里即可）。</summary>
        public static class Options
        {
            /// <summary>默认卡号（单卡系统固定 0）。卡初始化成功后会自动改用探测到的实际卡号。</summary>
            public static ushort DefaultCardNo = 0;

            /// <summary>每个 IO 扩展模块占多少位（用于把「模块号 + 序号」换算成雷赛整卡位号）。</summary>
            public static ushort BitsPerModule = 16;

            /// <summary>等待轴到位的最长时间。</summary>
            public static int AxisWaitTimeoutMs = 60000;

            /// <summary>等待 IO / 气缸的默认最长时间。</summary>
            public static int IoWaitTimeoutMs = 30000;

            /// <summary>伺服使能脚是否低电平有效（雷赛脉冲卡默认写 0 使能）。总线卡不使用该脚。</summary>
            public static bool ServoLowActive = true;

            /// <summary>轮询间隔。</summary>
            public static int PollIntervalMs = 5;

            /// <summary>
            /// 总线 IO 寻址方式。
            ///   null / false（默认）= 整卡位号：位号 = 模块号 × <see cref="BitsPerModule"/> + 序号，
            ///                        调 dmc_read_inbit / dmc_write_outbit（与官方例 8 一致）；
            ///   true              = 从站节点号 + 站内位号：节点号取「模块」列、位号取「序号」列，
            ///                        调 nmc_read_inbit / nmc_write_outbit。
            /// </summary>
            public static bool? BusIoAddressing = null;
        }

        private readonly LtdmcCard _card = new LtdmcCard();
        private readonly CommManager _comm = new CommManager();
        private readonly Action<string> _log;
        private readonly ConcurrentDictionary<string, int> _trayIndex = new ConcurrentDictionary<string, int>();
        private bool _cardReady;
        private bool _warnedNoCard;
        private bool _warnedBusIo;

        public LeadshineHardwareBridge(Action<string> log = null)
        {
            _log = log;
            _card.Log = Log;   // 未传回调时统一走 HardwareLog（Lua 运行期会指向输出面板）
            _comm.Log = Log;

            _cardReady = _card.TryInitialize(out string message);
            Log(_cardReady ? "[雷赛] " + message : "[雷赛] " + message + "（轴 / IO 动作将只记录日志，通讯功能仍然可用）");

            if (_cardReady)
            {
                Log("[雷赛] 寻址方式：轴运动走 dmc_ 族（脉冲卡 / 总线卡通用）；伺服使能与回零走 "
                    + (LtdmcCard.IsBusCard ? "nmc_ 族（总线卡）" : "dmc_ 族（脉冲卡）"));
            }
        }

        /// <summary>控制卡是否可用（供界面显示对接状态）。</summary>
        public bool IsCardReady => _cardReady;

        /// <summary>探测到的第一张卡的信息（卡号 / 卡型 / 轴数 / 从站数 / 总线错误码），未初始化时为 null。</summary>
        public LtdmcCard.CardInfo Card => LtdmcCard.FirstCard;

        /// <summary>当前卡是否为总线卡（EtherCAT / CANopen 主站）。</summary>
        public bool IsBusCard => LtdmcCard.IsBusCard;

        /// <summary>重新初始化控制卡（插好卡 / 装好驱动后可在界面上点“重连”）。</summary>
        public bool Reconnect(out string message)
        {
            _cardReady = _card.TryInitialize(out message);
            _warnedNoCard = false;
            _warnedBusIo = false;
            Log("[雷赛] 重连结果：" + message);
            return _cardReady;
        }

        public void Log(string message)
        {
            if (_log != null) _log(message);
            else HardwareLog.Write(message);
        }

        // ===================== 轴 =====================

        public void MoveAxis(AxisItem axis)
        {
            // AxisItem 没有“目标位置”字段，单独的 AxisMove 无法确定终点，
            // 因此这里只提示改用带位置的函数，避免误动作撞机。
            Log($"[雷赛] 轴「{axis.Name}」调用了 AxisMove，但未指定目标位置。请改用 MoveAxisAbs(\"{axis.Name}\", 目标位置) 或 MoveAxisRel(\"{axis.Name}\", 位移)。");
        }

        public void SetAxisSpeed(AxisItem axis, double speed)
        {
            if (!Ready(axis.Name, $"设速 {speed}")) return;
            var (card, no) = Addr(axis);
            Guard(() => _card.SetSpeed(card, no, speed, axis.Accel, axis.Decel));
            Log($"[雷赛] 轴「{axis.Name}」速度已设为 {speed} {axis.Unit}/s（卡{card} 轴{no}）");
        }

        public void HomeAxis(AxisItem axis)
        {
            if (!Ready(axis.Name, "回零")) return;
            var (card, no) = Addr(axis);
            ushort mode = ParseHomeMode(axis.HomeMode);

            WarnIfAxisCardMismatch(axis, card);
            WarnIfBusAxisNotEnabled(axis, card, no);

            Guard(() =>
            {
                EnsureProfile(axis, card, no);
                // 总线卡 / 脉冲卡的回零参数下发函数不同，由 LtdmcCard.Home 按卡型自动分派：
                //   总线卡：nmc_set_home_profile(含回零模式与零点偏移) + nmc_home_move
                //   脉冲卡：dmc_set_home_profile_unit + dmc_home_move
                _card.Home(card, no, mode, axis.CreepSpeed, axis.HomeSpeed, axis.Accel, axis.Decel, axis.HomeOffset);
            });
            Log($"[雷赛] 轴「{axis.Name}」开始回零（模式={axis.HomeMode}→{mode} 高速={axis.HomeSpeed} 爬行={axis.CreepSpeed}，"
                + (LtdmcCard.IsBusCard ? "总线卡：nmc_set_home_profile + nmc_home_move）" : "脉冲卡：dmc_set_home_profile_unit + dmc_home_move）"));

            // 等回零完成，再按配置的零点偏移重定义坐标
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < Options.AxisWaitTimeoutMs)
            {
                bool done = false;
                Guard(() => done = _card.IsHomeDone(card, no));
                if (done)
                {
                    Guard(() => _card.SetPosition(card, no, axis.HomeOffset));
                    Log($"[雷赛] 轴「{axis.Name}」回零完成，坐标已置为 {axis.HomeOffset}");
                    return;
                }
                Thread.Sleep(Options.PollIntervalMs);
            }
            throw new ScriptRuntimeException(
                $"轴「{axis.Name}」回零超时（{Options.AxisWaitTimeoutMs}ms）。请检查原点 / 限位感应是否接好、回零模式与速度是否合理。"
                + DiagnoseAxis(axis, card, no));
        }

        public void StopAxis(AxisItem axis)
        {
            if (!Ready(axis.Name, "停止")) return;
            var (card, no) = Addr(axis);
            Guard(() => _card.Stop(card, no, immediate: false));
            Log($"[雷赛] 轴「{axis.Name}」已减速停止");
        }

        public void WaitAxisDone(AxisItem axis)
        {
            if (!Ready(axis.Name, "等待到位")) return;
            var (card, no) = Addr(axis);
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < Options.AxisWaitTimeoutMs)
            {
                bool done = false;
                Guard(() => done = _card.IsDone(card, no));
                if (done)
                {
                    double pos = 0;
                    Guard(() => pos = _card.GetPosition(card, no));
                    Log($"[雷赛] 轴「{axis.Name}」已到位，当前位置 {pos:F3} {axis.Unit}");
                    return;
                }
                Thread.Sleep(Options.PollIntervalMs);
            }
            throw new ScriptRuntimeException(
                $"等待轴「{axis.Name}」到位超时（{Options.AxisWaitTimeoutMs}ms）。"
                + DiagnoseAxis(axis, card, no));
        }

        public void EnableAxis(AxisItem axis)
        {
            if (!Ready(axis.Name, "使能")) return;
            var (card, no) = Addr(axis);
            WarnIfAxisCardMismatch(axis, card);
            ConfigureAxisForMachine(axis, card, no);   // 机上必做：上电自检 / 参数下发

            bool lowActive = IsLowActive(axis.EnableLevel);
            string path = null;
            Guard(() => path = _card.SetServoEnable(card, no, enable: true, lowActive: lowActive));
            string how = LtdmcCard.IsBusCard ? string.Empty : (lowActive ? "，低电平有效" : "，高电平有效");
            Log($"[雷赛] 轴「{axis.Name}」已使能（{path}{how}）");

            if (LtdmcCard.IsBusCard)
            {
                // CiA402 状态机 2→3→4 不是瞬间完成的，立刻读往往还停在 2，等一下再报。
                Thread.Sleep(200);
                ReportBusAxisState(axis.Name, card, no, "使能后");
            }
        }

        public void MoveAxisRel(AxisItem axis, double distance)
        {
            if (!Ready(axis.Name, $"相对移动 {distance}")) return;
            var (card, no) = Addr(axis);
            WarnIfAxisCardMismatch(axis, card);
            WarnIfBusAxisNotEnabled(axis, card, no);
            Guard(() =>
            {
                EnsureProfile(axis, card, no);
                _card.MoveRelative(card, no, distance);
            });
            Log($"[雷赛] 轴「{axis.Name}」相对移动 {distance} {axis.Unit}（卡{card} 轴{no}）");
        }

        public void MoveAxisAbs(AxisItem axis, double position)
        {
            if (!Ready(axis.Name, $"绝对移动到 {position}")) return;
            CheckSoftLimit(axis, position);
            var (card, no) = Addr(axis);
            WarnIfAxisCardMismatch(axis, card);
            WarnIfBusAxisNotEnabled(axis, card, no);
            Guard(() =>
            {
                EnsureProfile(axis, card, no);
                _card.MoveAbsolute(card, no, position);
            });
            Log($"[雷赛] 轴「{axis.Name}」定位到 {position} {axis.Unit}（卡{card} 轴{no}）");
        }

        /// <summary>
        /// 读取编码器反馈位置（单位同 axis.Unit）。
        /// 官方例 1 用「指令位置 vs 编码器位置」判断电机到底动没动：
        /// 指令位置在变、编码器不变 → 没使能 / 动力线没接 / 编码器线松。
        /// </summary>
        public double GetAxisEncoder(AxisItem axis)
        {
            if (!Ready(axis.Name, "读编码器")) return 0;
            var (card, no) = Addr(axis);
            double v = 0;
            Guard(() => v = _card.GetEncoder(card, no));
            return v;
        }

        /// <summary>
        /// 读取当前指令位置（单位同 axis.Unit）。
        /// 与 <see cref="GetAxisEncoder"/> 一起用可判断「轴是不是真的在走」。
        /// </summary>
        public double GetAxisPosition(AxisItem axis)
        {
            if (!Ready(axis.Name, "读位置")) return 0;
            var (card, no) = Addr(axis);
            double v = 0;
            Guard(() => v = _card.GetPosition(card, no));
            return v;
        }

        /// <summary>读取总线轴状态机（CiA402，4 = 操作使能）；脉冲卡返回 -1。</summary>
        public int GetAxisStateMachine(AxisItem axis)
        {
            var (card, no) = Addr(axis);
            return _card.GetAxisStateMachine(card, no);
        }

        /// <summary>把 <see cref="GetAxisStateMachine"/> 的读数翻译成中文。</summary>
        public static string DescribeAxisState(int state) => LtdmcCard.DescribeAxisState(state);

        // ===================== IO =====================

        public double ReadInput(IoItem io)
        {
            if (!_cardReady) { WarnNoCard($"读输入「{io.Name}」"); return io.Value; }
            HintBusIoOnce();
            ushort card = CardNoOf(io);
            int raw = 0;
            if (UseBusIo(io))
            {
                ushort node = (ushort)Math.Max(io.ModuleNo, 0);
                ushort bit = (ushort)Math.Max(io.Sequence, 0);
                Guard(() => raw = _card.ReadInBitBus(card, node, bit));
            }
            else
            {
                ushort bit = BitNo(io);
                Guard(() => raw = _card.ReadInBit(card, bit));
            }
            int value = ApplyLevel(raw, io.Level);
            io.Value = value;
            return value;
        }

        public void WaitInput(IoItem io, int value)
        {
            if (!_cardReady) { WarnNoCard($"等待输入「{io.Name}」= {value}"); return; }
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < Options.IoWaitTimeoutMs)
            {
                if ((int)ReadInput(io) == value)
                {
                    Log($"[雷赛] 输入「{io.Name}」已变为 {value}（耗时 {sw.ElapsedMilliseconds}ms）");
                    return;
                }
                Thread.Sleep(Options.PollIntervalMs);
            }
            string where = UseBusIo(io)
                ? $"从站节点 {io.ModuleNo} 的位 {io.Sequence}"
                : $"整卡位号 {BitNo(io)}（模块 {io.ModuleNo} × {Options.BitsPerModule} + 序号 {io.Sequence}）";
            throw new ScriptRuntimeException(
                $"等待输入「{io.Name}」= {value} 超时（{Options.IoWaitTimeoutMs}ms）。"
                + $"请检查传感器接线、电平设置（当前 {io.Level}）与卡号 / 寻址是否正确（当前按 {where} 读，读到的原始值 {io.Value}）。");
        }

        public void WriteOutput(IoItem io, int value)
        {
            if (!_cardReady) { WarnNoCard($"写输出「{io.Name}」= {value}"); return; }
            HintBusIoOnce();
            ushort card = CardNoOf(io);
            int raw = ApplyLevel(value, io.Level);
            if (UseBusIo(io))
            {
                ushort node = (ushort)Math.Max(io.ModuleNo, 0);
                ushort bit = (ushort)Math.Max(io.Sequence, 0);
                Guard(() => _card.WriteOutBitBus(card, node, bit, raw));
                Log($"[雷赛] 输出「{io.Name}」= {value}（卡{card} 从站{node} 位{bit}）");
            }
            else
            {
                ushort bit = BitNo(io);
                Guard(() => _card.WriteOutBit(card, bit, raw));
                Log($"[雷赛] 输出「{io.Name}」= {value}（卡{card} 位{bit}）");
            }
            io.Value = value;
        }

        public void ToggleOutput(IoItem io)
        {
            if (!_cardReady) { WarnNoCard($"取反输出「{io.Name}」"); return; }
            HintBusIoOnce();
            ushort card = CardNoOf(io);
            int next;
            if (UseBusIo(io))
            {
                ushort node = (ushort)Math.Max(io.ModuleNo, 0);
                ushort bit = (ushort)Math.Max(io.Sequence, 0);
                int cur = 0;
                Guard(() => cur = _card.ReadOutBitBus(card, node, bit));
                next = cur != 0 ? 0 : 1;
                Guard(() => _card.WriteOutBitBus(card, node, bit, next));
                Log($"[雷赛] 输出「{io.Name}」已取反 → {ApplyLevel(next, io.Level)}（卡{card} 从站{node} 位{bit}）");
            }
            else
            {
                ushort bit = BitNo(io);
                int cur = 0;
                Guard(() => cur = _card.ReadOutBit(card, bit));
                next = cur != 0 ? 0 : 1;
                Guard(() => _card.WriteOutBit(card, bit, next));
                Log($"[雷赛] 输出「{io.Name}」已取反 → {ApplyLevel(next, io.Level)}（卡{card} 位{bit}）");
            }
            io.Value = ApplyLevel(next, io.Level);
        }

        // ===================== 气缸（通过 IO 点驱动） =====================

        public void CylinderMove(CylinderItem cyl, int state)
        {
            var outIo = FindIo(cyl.OutPoint, isOutput: true);
            if (outIo == null)
            {
                Log($"[雷赛] 气缸「{cyl.Name}」没有配置有效的输出点（当前：{cyl.OutPoint}），动作已跳过。");
                return;
            }

            if (cyl.DelayMs > 0) Thread.Sleep(cyl.DelayMs);

            if (cyl.PulseOutput && cyl.PulseWidthMs > 0)
            {
                WriteOutput(outIo, state);
                Thread.Sleep(cyl.PulseWidthMs);
                WriteOutput(outIo, state != 0 ? 0 : 1);
                Log($"[雷赛] 气缸「{cyl.Name}」脉冲输出 {cyl.PulseWidthMs}ms（{(state == 1 ? "伸出" : "缩回")}）");
                return;
            }

            WriteOutput(outIo, state);

            // 双线圈：另一路取反
            if (cyl.DoubleCoil)
            {
                var backIo = FindIo(cyl.BackupSensor, isOutput: true);
                if (backIo != null) WriteOutput(backIo, state != 0 ? 0 : 1);
            }

            Log($"[雷赛] 气缸「{cyl.Name}」{(state == 1 ? "伸出" : "缩回")}（输出点 {cyl.OutPoint}）");
        }

        public void WaitCylinder(CylinderItem cyl)
        {
            int timeout = cyl.TimeoutMs > 0 ? cyl.TimeoutMs : Options.IoWaitTimeoutMs;
            var outIo = FindIo(cyl.OutPoint, isOutput: true);
            int expectExtend = outIo != null ? (outIo.Value != 0 ? 1 : 0) : 1;

            string sensorName = expectExtend == 1 ? cyl.SensorExtend : cyl.SensorRetract;
            var sensor = FindIo(sensorName, isOutput: false);
            if (sensor == null)
            {
                // 没接到位感应，退化为按配置的动作时间等待
                int wait = expectExtend == 1
                    ? (cyl.ExtendMs > 0 ? cyl.ExtendMs : 300)
                    : (cyl.RetractMs > 0 ? cyl.RetractMs : 300);
                Thread.Sleep(wait);
                Log($"[雷赛] 气缸「{cyl.Name}」无到位感应（{sensorName}），按动作时间等待 {wait}ms");
                return;
            }

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeout)
            {
                if ((int)ReadInput(sensor) == 1)
                {
                    Log($"[雷赛] 气缸「{cyl.Name}」{(expectExtend == 1 ? "伸出" : "缩回")}到位（感应 {sensorName}，耗时 {sw.ElapsedMilliseconds}ms）");
                    return;
                }
                Thread.Sleep(Options.PollIntervalMs);
            }
            throw new ScriptRuntimeException($"气缸「{cyl.Name}」等待到位超时（{timeout}ms）。请检查气压、电磁阀输出「{cyl.OutPoint}」、到位感应「{sensorName}」接线与电平。");
        }

        public void CylinderReset(CylinderItem cyl)
        {
            int state = (cyl.InitialState ?? string.Empty).Contains("伸") ? 1 : 0;
            CylinderMove(cyl, state);
            Log($"[雷赛] 气缸「{cyl.Name}」复位到初始状态：{cyl.InitialState}");
        }

        // ===================== 通讯（真实串口 / 网口 / Modbus） =====================

        public void CommSend(CommItem comm, string data)
        {
            try
            {
                _comm.Send(comm, data);
                Log($"[通讯] 「{comm.Name}」发送：{data}");
            }
            catch (Exception ex) when (!(ex is ScriptRuntimeException))
            {
                throw new ScriptRuntimeException($"通讯「{comm.Name}」发送失败：{ex.Message}");
            }
        }

        public string CommRecv(CommItem comm)
        {
            try
            {
                string s = _comm.Recv(comm);
                Log($"[通讯] 「{comm.Name}」接收：{(string.IsNullOrEmpty(s) ? "(无数据)" : s)}");
                return s;
            }
            catch (Exception ex) when (!(ex is ScriptRuntimeException))
            {
                throw new ScriptRuntimeException($"通讯「{comm.Name}」接收失败：{ex.Message}");
            }
        }

        // ===================== 料盘 =====================

        public void TrayPick(TrayItem tray) => TrayStep(tray, "取料");

        public void TrayPlace(TrayItem tray) => TrayStep(tray, "放料");

        /// <summary>
        /// 按行列布局算出当前格子的坐标并推进格号。料盘没有绑定 X / Y 轴字段，
        /// 因此这里只负责算坐标 + 记日志，脚本里再用 MoveAxisAbs 把 XY 轴移过去。
        /// </summary>
        private void TrayStep(TrayItem tray, string action)
        {
            int total = Math.Max(tray.Rows, 1) * Math.Max(tray.Cols, 1);
            int index = _trayIndex.AddOrUpdate(tray.Name, 0, (_, old) => (old + 1) % total);
            int row = index / Math.Max(tray.Cols, 1);
            int col = index % Math.Max(tray.Cols, 1);
            double x = tray.StartX + col * tray.PitchX;
            double y = tray.StartY + row * tray.PitchY;

            Log($"[雷赛] 料盘「{tray.Name}」{action}：第 {index + 1}/{total} 格（行{row + 1} 列{col + 1}）坐标 X={x:F3} Y={y:F3}");
        }

        // ===================== 寻址 / 卡型分派 =====================

        /// <summary>
        /// 轴的卡号：优先用「控制器」页面里给该轴配置的卡号，其次用实际探测到的第一张卡，
        /// 最后退回 <see cref="Options.DefaultCardNo"/>。
        /// </summary>
        private static ushort CardNoOf(AxisItem axis)
        {
            var ctl = FindController(axis?.Controller);
            if (ctl != null) return (ushort)Math.Max(ctl.CardNo, 0);
            return LtdmcCard.IsReady ? LtdmcCard.FirstCardNo : Options.DefaultCardNo;
        }

        /// <summary>IO 的卡号：优先用归属控制器，其次用该 IO 自己填的卡号，最后用实际探测到的卡号。</summary>
        private static ushort CardNoOf(IoItem io)
        {
            var ctl = FindController(io?.Controller);
            if (ctl != null) return (ushort)Math.Max(ctl.CardNo, 0);
            if (io != null && io.CardNo > 0) return (ushort)io.CardNo;
            return LtdmcCard.IsReady ? LtdmcCard.FirstCardNo : Options.DefaultCardNo;
        }

        /// <summary>在工程里按名称找控制器；找不到（或工程还没加载）返回 null，不抛异常。</summary>
        private static AxisControllerItem FindController(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            try
            {
                var list = ProjectStore.Data?.Controllers;
                return list?.FirstOrDefault(c => c != null && c.Name == name);
            }
            catch { return null; }
        }

        /// <summary>
        /// 按轴自己的配置判断是不是总线轴（AxisType：脉冲 / 总线 / EtherCAT / CANopen / 模拟量 / 虚拟轴）。
        /// 没标注（或标注为模拟量 / 虚拟轴）时跟随控制卡探测结果。
        /// </summary>
        private static bool IsBusAxis(AxisItem axis)
        {
            string t = axis?.AxisType ?? string.Empty;
            if (t.Contains("脉冲")) return false;
            if (t.Contains("总线") || Contains(t, "EtherCAT") || Contains(t, "CANopen")) return true;
            return LtdmcCard.IsBusCard;
        }

        /// <summary>
        /// IO 是否走「总线寻址」（从站节点号 + 站内位号）。
        /// 默认 false：沿用官方例 8 的整卡位号，不改变现有接线行为。
        /// </summary>
        private static bool UseBusIo(IoItem io) => Options.BusIoAddressing == true;

        private static bool Contains(string s, string what) =>
            s.IndexOf(what, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>把「模块号 + 序号」换算成雷赛的整卡位号。</summary>
        private static ushort BitNo(IoItem io)
        {
            int bit = Math.Max(io.ModuleNo, 0) * Options.BitsPerModule + Math.Max(io.Sequence, 0);
            return (ushort)bit;
        }

        private (ushort card, ushort axis) Addr(AxisItem axis) => (CardNoOf(axis), (ushort)Math.Max(axis.AxisNo, 0));

        private void EnsureProfile(AxisItem axis, ushort card, ushort no) =>
            _card.ApplyAxisProfile(card, no, axis.PulsePerUnit, axis.Speed, axis.Accel, axis.Decel, axis.Jerk);

        // ===================== 诊断 =====================

        /// <summary>
        /// 轴上电时一次性初始化（移植自官方例 1 / 例 7 的机上做法）：
        ///   - 脉冲卡：设置脉冲输出模式、减速停止时间，并读轴 IO 状态位做自检；
        ///   - 总线卡：脉冲输出模式 / 减速停止时间都是本地资源，对总线轴没有意义（下发会返回 0
        ///     但不起作用），所以只报 CiA402 状态机与错误码。
        /// 全部按“最佳努力”下发——任一函数不被当前卡型号支持都只记日志、不中断上电。
        /// </summary>
        private void ConfigureAxisForMachine(AxisItem axis, ushort card, ushort no)
        {
            if (LtdmcCard.IsBusCard)
            {
                ReportBusAxisState(axis.Name, card, no, "上电自检");
                return;
            }

            TryConfig(() => _card.SetPulseOutmode(card, no, 0), $"轴「{axis.Name}」脉冲输出模式=脉冲+方向(0)");
            TryConfig(() => _card.SetDecStopTime(card, no, 0.1), $"轴「{axis.Name}」减速停止时间=0.1s");
            TryConfig(() =>
            {
                uint st = _card.ReadAxisIoStatus(card, no);
                Log($"[雷赛] 轴「{axis.Name}」上电自检 IO 状态位 = 0x{st:X}（bit0负限位/bit1正限位/bit2原点/bit3 EZ/bit4伺服报警/bit5急停，详见雷赛手册）");
            }, $"轴「{axis.Name}」读取 IO 状态");
        }

        /// <summary>轴上电配置按最佳努力执行：失败只记日志，不阻断伺服使能与后续流程。</summary>
        private void TryConfig(Action action, string what)
        {
            try { action(); }
            catch (Exception ex)
            {
                Log($"[雷赛·配置未生效] {what}：{ex.Message}（不影响上电，必要时按手册调整）");
            }
        }

        /// <summary>
        /// 打印总线轴的 CiA402 状态机 + 错误码。
        /// 现场「指令返回 0 但轴不动」九成是状态机不在 4（操作使能），所以使能后必打这一行。
        /// </summary>
        private void ReportBusAxisState(string axisName, ushort card, ushort no, string when)
        {
            try
            {
                int st = _card.GetAxisStateMachine(card, no);
                int axisErr = _card.GetAxisErrCode(card, no);
                int busErr = _card.GetBusErrCode(card);

                var sb = new StringBuilder();
                sb.Append($"[雷赛] 轴「{axisName}」{when}：状态机={st}（{LtdmcCard.DescribeAxisState(st)}）");
                if (axisErr > 0) sb.Append($"，★伺服错误码 {axisErr}");
                if (busErr > 0) sb.Append($"，★总线错误码 {busErr}（EtherCAT 未正常通信，先查网线 / 从站上电）");
                if (st >= 0 && st != 4) sb.Append("；★只有状态机=4（操作使能）轴才会运动");
                Log(sb.ToString());
            }
            catch (Exception ex)
            {
                Log($"[雷赛] 轴「{axisName}」读取总线状态失败：{ex.Message}");
            }
        }

        /// <summary>总线轴运动前先看一眼状态机：不是 4 就提前警告，别让操作员对着「指令成功但轴不动」猜。</summary>
        private void WarnIfBusAxisNotEnabled(AxisItem axis, ushort card, ushort no)
        {
            if (!LtdmcCard.IsBusCard) return;
            try
            {
                int st = _card.GetAxisStateMachine(card, no);
                if (st >= 0 && st != 4)
                    Log($"[雷赛·警告] 轴「{axis.Name}」当前状态机={st}（{LtdmcCard.DescribeAxisState(st)}），"
                        + "不是「操作使能(4)」——运动指令会正常返回，但轴不会动。请先执行 轴使能。");
            }
            catch { /* 读不到就算了，不干扰运动 */ }
        }

        /// <summary>
        /// 轴配置与卡型对不上时给出提示：轴写成「总线」但卡是脉冲卡（或反之）。
        /// 这是现场最常见的配置错误，直接点出来省得排查半天。
        /// </summary>
        private void WarnIfAxisCardMismatch(AxisItem axis, ushort card)
        {
            if (!LtdmcCard.IsReady) return;
            bool axisBus = IsBusAxis(axis);
            bool cardBus = LtdmcCard.IsBusCard;
            if (axisBus == cardBus) return;
            Log($"[雷赛·警告] 轴「{axis.Name}」配置为{(axisBus ? "总线轴" : "脉冲轴")}，"
                + $"但探测到的是{(cardBus ? "总线卡" : "脉冲卡")}——轴类型与卡型不一致，"
                + "请到「轴」页面把「轴类型」改成与实物一致，否则使能与回零会走错分支。");
        }

        /// <summary>轴超时 / 不动时的现场诊断串，直接拼进异常消息，让操作员一眼看到该查什么。</summary>
        private string DiagnoseAxis(AxisItem axis, ushort card, ushort no)
        {
            var sb = new StringBuilder("请检查伺服是否使能、是否报警、目标位置是否超出行程。");
            try
            {
                if (LtdmcCard.IsBusCard)
                {
                    int st = _card.GetAxisStateMachine(card, no);
                    int axisErr = _card.GetAxisErrCode(card, no);
                    int busErr = _card.GetBusErrCode(card);
                    sb.Append($" 当前总线状态：状态机={st}（{LtdmcCard.DescribeAxisState(st)}）");
                    if (st != 4) sb.Append("——★不是「操作使能」，轴不会运动：请先调用 轴使能，或检查伺服上电 / 报警 / 急停。");
                    if (axisErr > 0) sb.Append($" 伺服错误码={axisErr}。");
                    if (busErr > 0) sb.Append($" 总线错误码={busErr}（先查 EtherCAT 网线 / 从站上电）。");
                }
                else
                {
                    int reason = _card.GetStopReason(card, no);
                    if (reason != 0) sb.Append($" 停止原因码={reason}（撞限位 / 急停 / 报警，详见雷赛手册）。");
                    uint io = _card.ReadAxisIoStatus(card, no);
                    sb.Append($" 轴 IO 状态位=0x{io:X}（bit0 负限位 / bit1 正限位 / bit2 原点 / bit4 伺服报警 / bit5 急停）。");
                }
            }
            catch { /* 诊断本身失败不影响原始异常 */ }
            return sb.ToString();
        }

        /// <summary>总线卡上第一次读写 IO 时提示一次寻址方式，现场要切换时知道去哪儿改。</summary>
        private void HintBusIoOnce()
        {
            if (_warnedBusIo || !LtdmcCard.IsBusCard) return;
            _warnedBusIo = true;
            if (Options.BusIoAddressing == true)
                Log("[雷赛] 总线 IO 寻址：从站节点号 + 站内位号（nmc_read_inbit / nmc_write_outbit）——节点号取「模块」列、位号取「序号」列。");
            else
                Log($"[雷赛] 总线 IO 寻址：整卡位号 = 模块 × {Options.BitsPerModule} + 序号"
                    + "（dmc_read_inbit / dmc_write_outbit，与官方例 8 一致）。"
                    + "若你的 EtherCAT IO 从站要按「节点号 + 站内位号」访问，把 LeadshineHardwareBridge.Options.BusIoAddressing 设为 true。");
        }

        // ===================== 其它辅助 =====================

        /// <summary>按电平配置决定是否取反（常闭 / 低电平有效 → 取反）。</summary>
        private static int ApplyLevel(int value, string level)
        {
            string s = level ?? string.Empty;
            bool invert = s.Contains("低") || s.Contains("常闭") ||
                          s.IndexOf("NC", StringComparison.OrdinalIgnoreCase) >= 0;
            return invert ? (value != 0 ? 0 : 1) : (value != 0 ? 1 : 0);
        }

        private static bool IsLowActive(string level)
        {
            string s = level ?? string.Empty;
            if (s.Contains("高") || s.IndexOf("High", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            return true;   // 默认低电平有效（雷赛常见接法）
        }

        private static ushort ParseHomeMode(string mode)
        {
            string s = (mode ?? string.Empty).Trim();
            if (ushort.TryParse(s, out ushort n)) return n;
            if (s.Contains("限位")) return 0;
            if (s.Contains("EZ") || s.Contains("Z 相") || s.Contains("Z相")) return 2;
            if (s.Contains("原点")) return 1;
            return 0;
        }

        /// <summary>按软限位检查目标位置，避免撞机。</summary>
        private static void CheckSoftLimit(AxisItem axis, double position)
        {
            if (axis.PosLimitPlus == 0 && axis.PosLimitMinus == 0) return;   // 未配置软限位
            if (position > axis.PosLimitPlus || position < axis.PosLimitMinus)
                throw new ScriptRuntimeException(
                    $"轴「{axis.Name}」目标位置 {position} 超出软限位范围 [{axis.PosLimitMinus}, {axis.PosLimitPlus}]，已阻止运动。");
        }

        /// <summary>按名称找 IO 点；名称直接写数字时按“序号”虚拟一个点。</summary>
        private static IoItem FindIo(string name, bool isOutput)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var list = isOutput ? ProjectStore.Data.Outputs : ProjectStore.Data.Inputs;
            var io = list.FirstOrDefault(x => x.Name == name);
            if (io != null) return io;

            if (int.TryParse(name.Trim(), out int seq))
                return new IoItem { Name = name, CardNo = Options.DefaultCardNo, ModuleNo = 0, Sequence = seq };

            return null;
        }

        private bool Ready(string what, string action)
        {
            if (_cardReady) return true;
            WarnNoCard($"{what} {action}");
            return false;
        }

        private void WarnNoCard(string action)
        {
            if (!_warnedNoCard)
            {
                Log("[雷赛] 控制卡未就绪，以下动作只记录不执行：请确认卡已插好、驱动已安装、LTDMC.dll 与程序位数一致。");
                _warnedNoCard = true;
            }
            Log($"[雷赛·未执行] {action}");
        }

        /// <summary>把底层硬件异常翻译成 Lua 能显示的中文错误。</summary>
        private static void Guard(Action action)
        {
            try { action(); }
            catch (HardwareOperationException ex) { throw new ScriptRuntimeException(ex.Message); }
        }

        public void Dispose()
        {
            _comm.CloseAll();
            LtdmcCard.Close();
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
