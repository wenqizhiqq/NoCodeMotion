// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
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
    /// </summary>
    public sealed class LeadshineHardwareBridge : IHardwareBridge, IDisposable
    {
        /// <summary>可调参数（按现场接线习惯改这里即可）。</summary>
        public static class Options
        {
            /// <summary>默认卡号（单卡系统固定 0）。</summary>
            public static ushort DefaultCardNo = 0;

            /// <summary>每个 IO 扩展模块占多少位（用于把「模块号 + 序号」换算成雷赛位号）。</summary>
            public static ushort BitsPerModule = 16;

            /// <summary>等待轴到位的最长时间。</summary>
            public static int AxisWaitTimeoutMs = 60000;

            /// <summary>等待 IO / 气缸的默认最长时间。</summary>
            public static int IoWaitTimeoutMs = 30000;

            /// <summary>伺服使能脚是否低电平有效（雷赛默认写 0 使能）。</summary>
            public static bool ServoLowActive = true;

            /// <summary>轮询间隔。</summary>
            public static int PollIntervalMs = 5;
        }

        private readonly LtdmcCard _card = new LtdmcCard();
        private readonly CommManager _comm = new CommManager();
        private readonly Action<string> _log;
        private readonly ConcurrentDictionary<string, int> _trayIndex = new ConcurrentDictionary<string, int>();
        private bool _cardReady;
        private bool _warnedNoCard;

        public LeadshineHardwareBridge(Action<string> log = null)
        {
            _log = log;
            _card.Log = Log;   // 未传回调时统一走 HardwareLog（Lua 运行期会指向输出面板）
            _comm.Log = Log;

            _cardReady = _card.TryInitialize(out string message);
            Log(_cardReady ? "[雷赛] " + message : "[雷赛] " + message + "（轴 / IO 动作将只记录日志，通讯功能仍然可用）");
        }

        /// <summary>控制卡是否可用（供界面显示对接状态）。</summary>
        public bool IsCardReady => _cardReady;

        /// <summary>重新初始化控制卡（插好卡 / 装好驱动后可在界面上点“重连”）。</summary>
        public bool Reconnect(out string message)
        {
            _cardReady = _card.TryInitialize(out message);
            _warnedNoCard = false;
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

            Guard(() =>
            {
                EnsureProfile(axis, card, no);
                _card.Home(card, no, mode, axis.CreepSpeed, axis.HomeSpeed, axis.Accel, axis.Decel);
            });
            Log($"[雷赛] 轴「{axis.Name}」开始回零（模式={axis.HomeMode}→{mode} 高速={axis.HomeSpeed} 爬行={axis.CreepSpeed}）");

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
            throw new ScriptRuntimeException($"轴「{axis.Name}」回零超时（{Options.AxisWaitTimeoutMs}ms）。请检查原点 / 限位感应是否接好、回零模式与速度是否合理。");
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
            throw new ScriptRuntimeException($"等待轴「{axis.Name}」到位超时（{Options.AxisWaitTimeoutMs}ms）。请检查伺服是否使能、是否报警、目标位置是否超出行程。");
        }

        public void EnableAxis(AxisItem axis)
        {
            if (!Ready(axis.Name, "使能")) return;
            var (card, no) = Addr(axis);
            bool lowActive = IsLowActive(axis.EnableLevel);
            Guard(() => _card.ServoOn(card, no, enable: true, lowActive: lowActive));
            Log($"[雷赛] 轴「{axis.Name}」已使能（{(lowActive ? "低电平有效" : "高电平有效")}）");
        }

        public void MoveAxisRel(AxisItem axis, double distance)
        {
            if (!Ready(axis.Name, $"相对移动 {distance}")) return;
            var (card, no) = Addr(axis);
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
            Guard(() =>
            {
                EnsureProfile(axis, card, no);
                _card.MoveAbsolute(card, no, position);
            });
            Log($"[雷赛] 轴「{axis.Name}」定位到 {position} {axis.Unit}（卡{card} 轴{no}）");
        }

        // ===================== IO =====================

        public double ReadInput(IoItem io)
        {
            if (!_cardReady) { WarnNoCard($"读输入「{io.Name}」"); return io.Value; }
            ushort card = (ushort)Math.Max(io.CardNo, 0);
            ushort bit = BitNo(io);
            int raw = 0;
            Guard(() => raw = _card.ReadInBit(card, bit));
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
            throw new ScriptRuntimeException($"等待输入「{io.Name}」= {value} 超时（{Options.IoWaitTimeoutMs}ms）。请检查传感器接线、电平设置（当前 {io.Level}）与卡号 / 模块 / 序号是否正确。");
        }

        public void WriteOutput(IoItem io, int value)
        {
            if (!_cardReady) { WarnNoCard($"写输出「{io.Name}」= {value}"); return; }
            ushort card = (ushort)Math.Max(io.CardNo, 0);
            ushort bit = BitNo(io);
            int raw = ApplyLevel(value, io.Level);
            Guard(() => _card.WriteOutBit(card, bit, raw));
            io.Value = value;
            Log($"[雷赛] 输出「{io.Name}」= {value}（卡{card} 位{bit}）");
        }

        public void ToggleOutput(IoItem io)
        {
            if (!_cardReady) { WarnNoCard($"取反输出「{io.Name}」"); return; }
            ushort card = (ushort)Math.Max(io.CardNo, 0);
            ushort bit = BitNo(io);
            int raw = 0;
            Guard(() => raw = _card.ReadOutBit(card, bit));
            int next = raw != 0 ? 0 : 1;
            Guard(() => _card.WriteOutBit(card, bit, next));
            io.Value = ApplyLevel(next, io.Level);
            Log($"[雷赛] 输出「{io.Name}」已取反 → {io.Value}（卡{card} 位{bit}）");
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

        // ===================== 内部辅助 =====================

        private (ushort card, ushort axis) Addr(AxisItem axis) =>
            (Options.DefaultCardNo, (ushort)Math.Max(axis.AxisNo, 0));

        private void EnsureProfile(AxisItem axis, ushort card, ushort no) =>
            _card.ApplyAxisProfile(card, no, axis.PulsePerUnit, axis.Speed, axis.Accel, axis.Decel, axis.Jerk);

        /// <summary>把「模块号 + 序号」换算成雷赛的位号。</summary>
        private static ushort BitNo(IoItem io)
        {
            int bit = Math.Max(io.ModuleNo, 0) * Options.BitsPerModule + Math.Max(io.Sequence, 0);
            return (ushort)bit;
        }

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
