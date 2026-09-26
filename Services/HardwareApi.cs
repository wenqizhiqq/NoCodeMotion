// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‎启​志‍◆⁠编‏写‌◇⁠微⁠信‌﹕‏1‍8‌7‎◆‌1​9⁠3⁣6‏◇‏1​3‏9‍9‌　‎※​保‏留‏所‌有‎权‍利‏请‌勿‏删⁠除⁣◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using MoonSharp.Interpreter;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// Lua 侧硬件函数的绑定层（对接桥的“前半段”）。
    ///
    /// 职责：
    /// 1. 把 Lua 里调用的 <c>AxisMove("轴1")</c> / <c>SetIO("输出1", 1)</c> 等函数，
    ///    注册成 MoonSharp 全局函数（见 <see cref="Register"/>）。
    /// 2. 按名称 Name 从 <see cref="ProjectStore.Data"/> 解析出配置对象
    ///    （AxisItem / IoItem / CylinderItem / CommItem / TrayItem），找不到时抛
    ///    <see cref="ScriptRuntimeException"/>，错误信息直接显示在 Lua 输出面板。
    /// 3. 把解析好的对象交给 <see cref="IHardwareBridge"/>（真实硬件或桩）执行。
    ///
    /// 真正的“对接”只发生在 IHardwareBridge 里：本类不碰任何设备，只做名称解析与 Lua 绑定。
    /// </summary>
    public sealed class HardwareApi
    {
        private readonly IHardwareBridge _bridge;
        private readonly Action<string> _log;

        public HardwareApi(IHardwareBridge bridge, Action<string> log)
        {
            _bridge = bridge;
            _log = log;
        }

        // ===================== 名称解析 =====================

        private AxisItem FindAxis(string name)
        {
            var ax = ProjectStore.Data.Axes.FirstOrDefault(a => a.Name == name);
            if (ax == null) throw new ScriptRuntimeException($"找不到轴：{name}");
            return ax;
        }

        private IoItem FindInput(string name)
        {
            var io = ProjectStore.Data.Inputs.FirstOrDefault(i => i.Name == name);
            if (io == null) throw new ScriptRuntimeException($"找不到输入 IO：{name}");
            return io;
        }

        private IoItem FindOutput(string name)
        {
            var io = ProjectStore.Data.Outputs.FirstOrDefault(i => i.Name == name);
            if (io == null) throw new ScriptRuntimeException($"找不到输出 IO：{name}");
            return io;
        }

        private CylinderItem FindCylinder(string name)
        {
            var c = ProjectStore.Data.Cylinders.FirstOrDefault(x => x.Name == name);
            if (c == null) throw new ScriptRuntimeException($"找不到气缸：{name}");
            return c;
        }

        private CommItem FindComm(string name)
        {
            var c = ProjectStore.Data.Comms.FirstOrDefault(x => x.Name == name);
            if (c == null) throw new ScriptRuntimeException($"找不到通讯：{name}");
            return c;
        }

        private TrayItem FindTray(string name)
        {
            var t = ProjectStore.Data.Trays.FirstOrDefault(x => x.Name == name);
            if (t == null) throw new ScriptRuntimeException($"找不到料盘：{name}");
            return t;
        }

        // ===================== 轴 =====================

        public void AxisMove(string name) => _bridge.MoveAxis(FindAxis(name));
        public void SetAxisSpeed(string name, double speed) => _bridge.SetAxisSpeed(FindAxis(name), speed);
        public void AxisHome(string name) => _bridge.HomeAxis(FindAxis(name));
        public void StopAxis(string name) => _bridge.StopAxis(FindAxis(name));
        public void WaitAxisDone(string name) => _bridge.WaitAxisDone(FindAxis(name));
        public void EnableAxis(string name) => _bridge.EnableAxis(FindAxis(name));
        public void MoveAxisRel(string name, double distance) => _bridge.MoveAxisRel(FindAxis(name), distance);
        public void MoveAxisAbs(string name, double position) => _bridge.MoveAxisAbs(FindAxis(name), position);

        // ===================== 输入 / 输出 IO =====================

        public double ReadIO(string name) => _bridge.ReadInput(FindInput(name));
        public void WaitIO(string name, int value) => _bridge.WaitInput(FindInput(name), value);
        public void SetIO(string name, int value) => _bridge.WriteOutput(FindOutput(name), value);
        public void ToggleIO(string name) => _bridge.ToggleOutput(FindOutput(name));

        // ===================== 气缸 =====================

        public void CylinderMove(string name, int state) => _bridge.CylinderMove(FindCylinder(name), state);
        public void WaitCylinder(string name) => _bridge.WaitCylinder(FindCylinder(name));
        public void CylinderReset(string name) => _bridge.CylinderReset(FindCylinder(name));

        // ===================== 通讯 =====================

        public void CommSend(string name, string data) => _bridge.CommSend(FindComm(name), data ?? string.Empty);
        public string CommRecv(string name) => _bridge.CommRecv(FindComm(name));

        // ===================== 料盘 =====================

        public void TrayPick(string name) => _bridge.TrayPick(FindTray(name));
        public void TrayPlace(string name) => _bridge.TrayPlace(FindTray(name));

        // ===================== 点位（点位表 / 点位） =====================
        // 写法：PointMove("工位1.取料点")（“点位表名.点位名”）；只写点位名时会跨表查唯一匹配。

        private PointTable FindPointTable(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ScriptRuntimeException("点位表名不能为空");
            var t = ProjectStore.Data.PointTables.FirstOrDefault(x => x.Name == name);
            if (t == null) throw new ScriptRuntimeException($"找不到点位表：{name}");
            return t;
        }

        /// <summary>解析 "点位表名.点位名"（也接受只写点位名）。</summary>
        private (PointTable table, PointItem point) ResolvePoint(string spec)
        {
            spec = (spec ?? string.Empty).Trim();
            if (spec.Length == 0) throw new ScriptRuntimeException("点位不能为空，写法：\"点位表名.点位名\"");

            int dot = spec.IndexOfAny(new[] { '.', '．', '/', '\\' });
            if (dot > 0)
            {
                var t = FindPointTable(spec.Substring(0, dot));
                string pn = spec.Substring(dot + 1).Trim();
                var p = t.Points.FirstOrDefault(x => x.Name == pn);
                if (p == null) throw new ScriptRuntimeException($"点位表「{t.Name}」里没有点位：{pn}");
                return (t, p);
            }

            foreach (var t in ProjectStore.Data.PointTables)
            {
                var p = t.Points.FirstOrDefault(x => x.Name == spec);
                if (p != null) return (t, p);
            }
            throw new ScriptRuntimeException($"找不到点位：{spec}（建议写全 \"点位表名.点位名\"）");
        }

        /// <summary>点位移动：按点位表各轴槽把轴真正开到该点位（未填目标位置的槽跳过、不改动该轴）。
        /// 阻塞到各轴到位；返回实际驱动的轴数。</summary>
        public double PointMove(string spec)
        {
            var (table, point) = ResolvePoint(spec);
            int moved = 0;
            for (int i = 0; i < PointTable.SlotCount; i++)
            {
                string axisName = table.AxisNames.Count > i ? table.AxisNames[i] : string.Empty;
                if (string.IsNullOrWhiteSpace(axisName)) continue;
                var slot = point.Positions.Count > i ? point.Positions[i] : null;
                if (slot?.Position == null) continue;

                var ax = FindAxis(axisName);
                if (slot.Speed > 0) _bridge.SetAxisSpeed(ax, slot.Speed);
                _bridge.MoveAxisAbs(ax, slot.Position.Value);
                _bridge.WaitAxisDone(ax);
                moved++;
            }
            _log?.Invoke($"[点位] 移动到「{table.Name}.{point.Name}」：已驱动 {moved} 个轴到位");
            return moved;
        }

        /// <summary>点位修改：把点位的第 slot 个轴槽（1~4）目标位置改成 position，真实写回工程并保存。
        /// speed &gt; 0 时同时改该槽速度，传 0 表示不动速度。返回改动的轴槽数（0 或 1）。
        /// （speed 不做可选参数：C# 方法组无法转成带可选参数的委托，MoonSharp 注册会 CS0123。）</summary>
        public double PointModify(string spec, double slot, double position, double speed)
        {
            var (table, point) = ResolvePoint(spec);
            int idx = (int)slot - 1;
            if (idx < 0 || idx >= PointTable.SlotCount)
                throw new ScriptRuntimeException($"轴槽号只能是 1~{PointTable.SlotCount}，收到：{slot}");

            string axisName = table.AxisNames.Count > idx ? table.AxisNames[idx] : string.Empty;
            if (string.IsNullOrWhiteSpace(axisName))
                throw new ScriptRuntimeException($"点位表「{table.Name}」第 {slot} 个轴槽没有配轴，请先到「点位」页选轴");

            RunOnUiThread(() =>
            {
                point.EnsureSlots();
                point.Positions[idx].Position = position;
                if (speed > 0) point.Positions[idx].Speed = speed;
            });
            _log?.Invoke($"[点位] 修改「{table.Name}.{point.Name}」第 {slot} 轴槽（{axisName}）目标位置 = {position:0.###}");
            return 1;
        }

        /// <summary>点位示教：把点位表各轴槽的当前位置写成该点位的目标位置（现场“开到位置后示教”），
        /// 真实写回工程并保存；返回写入的轴槽数。</summary>
        public double PointTeach(string spec)
        {
            var (table, point) = ResolvePoint(spec);
            int written = 0;
            for (int i = 0; i < PointTable.SlotCount; i++)
            {
                string axisName = table.AxisNames.Count > i ? table.AxisNames[i] : string.Empty;
                if (string.IsNullOrWhiteSpace(axisName)) continue;
                var ax = FindAxis(axisName);
                double pos = _bridge.ReadAxisPosition(ax);
                if (double.IsNaN(pos)) continue;
                int idx = i;
                RunOnUiThread(() =>
                {
                    point.EnsureSlots();
                    point.Positions[idx].Position = pos;
                });
                written++;
            }
            _log?.Invoke($"[点位] 示教「{table.Name}.{point.Name}」：已用当前位置写入 {written} 个轴槽");
            return written;
        }

        /// <summary>改工程数据（点位坐标等）必须在 UI 线程做，否则点位页/流程页的绑定集合会抛跨线程异常；
        /// 改完统一落盘（xlsx），与点位页手工改坐标等价。</summary>
        private static void RunOnUiThread(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
                dispatcher.Invoke(action);      // 脚本在后台线程跑 → 封送到 UI 线程执行
            else
                action();
            ProjectStore.ScheduleSave();
        }

        // ===================== 硬件状态 / 模式 =====================

        /// <summary>返回当前对接状态（中文），例：雷赛控制卡已连接（卡数量 1）…</summary>
        public string HardwareStatus() => Hardware.HardwareSetup.StatusMessage;

        /// <summary>控制卡是否已就绪（1 就绪 / 0 未就绪）。</summary>
        public double HardwareReady() => Hardware.HardwareSetup.IsCardReady ? 1 : 0;

        /// <summary>重新连接控制卡（现场插好卡 / 装好驱动后调用），返回中文结果。</summary>
        public string HardwareReconnect()
        {
            string msg = Hardware.HardwareSetup.Reconnect();
            _log?.Invoke("[硬件] " + msg);
            return msg;
        }

        /// <summary>切换到真实硬件（雷赛控制卡 + 真实串口 / 网口 / Modbus），返回中文结果。</summary>
        public string UseRealHardware()
        {
            string msg = Hardware.HardwareSetup.UseLeadshine();
            _log?.Invoke("[硬件] " + msg);
            return msg;
        }

        /// <summary>
        /// 切换到「已移植运动控制卡族」（升立德 / 恒昱 / 研控 / 未分类 / 模拟卡，
        /// 也可显式选雷赛的卡族实现），返回中文结果。
        /// </summary>
        public string UseCardFamilies()
        {
            string msg = Hardware.HardwareSetup.UseCardFamilies();
            _log?.Invoke("[硬件] " + msg);
            return msg;
        }

        /// <summary>切换到仿真（不碰任何设备），返回中文结果。</summary>
        public string UseSimulation()
        {
            string msg = Hardware.HardwareSetup.UseSimulation();
            _log?.Invoke("[硬件] " + msg);
            return msg;
        }

        // ===================== 运行控制 / 辅助（全局函数） =====================

        /// <summary>当前是否处于急停锁定（供脚本里 <c>if EStop() then return end</c> 使用）。</summary>
        public bool IsEStop() => StatusBarService.EStopped;

        /// <summary>脚本内延时（毫秒）。在后台线程休眠，便于让出运行节奏。</summary>
        public void Delay(double ms)
        {
            int msInt = (int)Math.Max(0, Math.Min(60000, ms));
            Thread.Sleep(msInt);
        }

        /// <summary>大写 <c>Print</c> 别名（与模板里 <c>Print(...)</c> 对应；标准 <c>print</c> 已由 Options.DebugPrint 接管）。</summary>
        public void LuaPrint(object value) => _log?.Invoke(value?.ToString() ?? string.Empty);

        // Log.Info / Log.Warn / Log.Error / Log.Debug：日志面板输出的别名。
        // 提示词与「视觉流程」的文档一直让 AI 写 Log.Info(...)，但早期版本从没注册过 Log，
        // 脚本一运行就报 "attempt to call a nil value (field 'Log')"。这里补齐，
        // 让 Log.* 与 print / Print 等价（print 由 Options.DebugPrint 接管）。
        /// <summary>Log.Info(...) / Log.Output(...)：普通输出。</summary>
        public void LuaLogInfo(object value) => _log?.Invoke(value?.ToString() ?? string.Empty);

        /// <summary>Log.Warn(...)：警告输出，带 [警告] 前缀便于在日志里定位。</summary>
        public void LuaLogWarn(object value) => _log?.Invoke("[警告] " + (value?.ToString() ?? string.Empty));

        /// <summary>Log.Error(...)：错误输出，带 [错误] 前缀。</summary>
        public void LuaLogError(object value) => _log?.Invoke("[错误] " + (value?.ToString() ?? string.Empty));

        /// <summary>Log.Debug(...)：调试输出，带 [调试] 前缀。</summary>
        public void LuaLogDebug(object value) => _log?.Invoke("[调试] " + (value?.ToString() ?? string.Empty));

        // ===================== 命名空间式 API（与“脚本流程示例”模板一致） =====================

        /// <summary>Variable.Get / Variable.Set：对接工程变量表（VariableRow）。</summary>
        private sealed class VariableApi
        {
            private readonly HardwareApi _owner;
            public VariableApi(HardwareApi owner) => _owner = owner;

            /// <summary>返回变量值：可解析为数字时返回 number，否则返回字符串；未定义返回 nil。</summary>
            public DynValue Get(string name)
            {
                var (row, col) = FindVar(name);
                if (row == null) return DynValue.Nil;
                string raw = GetVal(row, col);
                if (string.IsNullOrWhiteSpace(raw)) return DynValue.Nil;
                if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                    return DynValue.NewNumber(d);
                return DynValue.NewString(raw);
            }

            /// <summary>写入变量值（数字或字符串都会以字符串形式存回变量表，并实时打印）。
            /// 整个查找+新建+写入在 UI 线程同步执行：<c>ProjectStore.Data.Variables</c> 绑定到「变量页面」DataGrid 的
            /// CollectionView，从 LuaScriptThread 后台线程 <c>Add</c> / 修改行属性会触发
            /// "该类型的 CollectionView 不支持从调度程序线程以外的线程对其 SourceCollection" 异常，
            /// 被 LuaDebugSession 包成「宿主异常」（现象：脚本报错（行 0））。
            /// 用 <c>Dispatcher.Invoke</c> 短暂阻塞，保证紧跟其后的 Variable.Get 能读到新值。</summary>
            public void Set(string name, object value)
            {
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    dispatcher.Invoke(new Action(() => SetCore(name, value)));
                    return;
                }
                SetCore(name, value);
            }

            private void SetCore(string name, object value)
            {
                var (row, col) = FindOrAddVar(name);
                string s = value switch
                {
                    string str => str,
                    double d => d.ToString(CultureInfo.InvariantCulture),
                    _ => value?.ToString() ?? string.Empty
                };
                SetVal(row, col, s);
                _owner._log?.Invoke($"[变量] {name} = {s}");
            }

            private static string GetName(VariableRow r, int c) => c switch { 1 => r.Name1, 2 => r.Name2, 3 => r.Name3, 4 => r.Name4, 5 => r.Name5, _ => string.Empty };
            private static string GetVal(VariableRow r, int c) => c switch { 1 => r.Value1, 2 => r.Value2, 3 => r.Value3, 4 => r.Value4, 5 => r.Value5, _ => string.Empty };
            private static void SetName(VariableRow r, int c, string v) { switch (c) { case 1: r.Name1 = v; break; case 2: r.Name2 = v; break; case 3: r.Name3 = v; break; case 4: r.Name4 = v; break; case 5: r.Name5 = v; break; } }
            private static void SetVal(VariableRow r, int c, string v) { switch (c) { case 1: r.Value1 = v; break; case 2: r.Value2 = v; break; case 3: r.Value3 = v; break; case 4: r.Value4 = v; break; case 5: r.Value5 = v; break; } }

            private static (VariableRow row, int col) FindVar(string name)
            {
                foreach (var r in ProjectStore.Data.Variables)
                    for (int c = 1; c <= 5; c++)
                        if (GetName(r, c) == name) return (r, c);
                return (null, 0);
            }

            private static (VariableRow row, int col) FindOrAddVar(string name)
            {
                foreach (var r in ProjectStore.Data.Variables)
                    for (int c = 1; c <= 5; c++)
                        if (GetName(r, c) == name) return (r, c);
                // 找第一个有空位的行
                foreach (var r in ProjectStore.Data.Variables)
                    for (int c = 1; c <= 5; c++)
                        if (string.IsNullOrWhiteSpace(GetName(r, c)))
                        { SetName(r, c, name); return (r, c); }
                // 新建一行
                var nr = new VariableRow();
                nr.Name1 = name;
                ProjectStore.Data.Variables.Add(nr);
                return (nr, 1);
            }
        }

        /// <summary>IO.Get / IO.Set：对接输入/输出 IO。IO.Get 返回字符串 "0"/"1"。</summary>
        private sealed class IoApi
        {
            private readonly HardwareApi _owner;
            public IoApi(HardwareApi owner) => _owner = owner;

            /// <summary>读取输入 IO 当前值（字符串 "0"/"1"）。
            /// 仿真且工程未配置该输入（如没有物理“启动”按钮）时返回 "1"，让纯仿真脚本可直接跑通等待信号的逻辑。</summary>
            public string Get(string name)
            {
                var io = ProjectStore.Data.Inputs.FirstOrDefault(i => i.Name == name);
                if (io == null) return "1";
                double v = _owner._bridge.ReadInput(io);
                return v == 0 ? "0" : "1";
            }

            /// <summary>写入输出 IO（值会被解析为整数）。找不到该输出时给出警告而非崩溃。</summary>
            public void Set(string name, object value)
            {
                int v = ParseInt(value);
                var io = ProjectStore.Data.Outputs.FirstOrDefault(i => i.Name == name);
                if (io == null)
                {
                    _owner._log?.Invoke($"[警告] 未找到输出 IO：{name}，已忽略 IO.Set");
                    return;
                }
                _owner._bridge.WriteOutput(io, v);
            }

            private static int ParseInt(object value) => value switch
            {
                int i => i,
                double d => (int)d,
                string s => int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out int r) ? r : 0,
                _ => 0
            };
        }

        /// <summary>Axis.MoveAbs / MoveRel / Home / Stop / WaitDone / SetSpeed / Enable：对接轴。</summary>
        private sealed class AxisApi
        {
            private readonly HardwareApi _owner;
            public AxisApi(HardwareApi owner) => _owner = owner;

            public void MoveAbs(string name, double position, double speed)
            {
                _owner.SetAxisSpeed(name, speed);
                _owner.MoveAxisAbs(name, position);
            }
            public void MoveRel(string name, double distance) => _owner.MoveAxisRel(name, distance);
            public void Home(string name) => _owner.AxisHome(name);
            public void Stop(string name) => _owner.StopAxis(name);
            public void WaitDone(string name) => _owner.WaitAxisDone(name);
            public void SetSpeed(string name, double speed) => _owner.SetAxisSpeed(name, speed);
            public void Enable(string name) => _owner.EnableAxis(name);
        }

        /// <summary>Cylinder.Out / Back / Reset：对接气缸。</summary>
        private sealed class CylinderApi
        {
            private readonly HardwareApi _owner;
            public CylinderApi(HardwareApi owner) => _owner = owner;

            public void Out(string name) => _owner.CylinderMove(name, 1);
            public void Back(string name) => _owner.CylinderMove(name, 0);
            public void Reset(string name) => _owner.CylinderReset(name);
        }

        // ===================== 注册到 MoonSharp =====================

        /// <summary>
        /// 把全部硬件函数注册为 Lua 全局函数。在脚本编译前调用一次即可。
        /// 这些名字与“智能工具”面板插入的模板一一对应。
        /// </summary>
        public static void Register(Script script, HardwareApi api)
        {
            script.Globals["AxisMove"] = (Action<string>)api.AxisMove;
            script.Globals["SetAxisSpeed"] = (Action<string, double>)api.SetAxisSpeed;
            script.Globals["AxisHome"] = (Action<string>)api.AxisHome;
            script.Globals["StopAxis"] = (Action<string>)api.StopAxis;
            script.Globals["WaitAxisDone"] = (Action<string>)api.WaitAxisDone;
            script.Globals["EnableAxis"] = (Action<string>)api.EnableAxis;
            script.Globals["MoveAxisRel"] = (Action<string, double>)api.MoveAxisRel;
            script.Globals["MoveAxisAbs"] = (Action<string, double>)api.MoveAxisAbs;

            script.Globals["ReadIO"] = (Func<string, double>)api.ReadIO;
            script.Globals["WaitIO"] = (Action<string, int>)api.WaitIO;
            script.Globals["SetIO"] = (Action<string, int>)api.SetIO;
            script.Globals["ToggleIO"] = (Action<string>)api.ToggleIO;

            script.Globals["CylinderMove"] = (Action<string, int>)api.CylinderMove;
            script.Globals["WaitCylinder"] = (Action<string>)api.WaitCylinder;
            script.Globals["CylinderReset"] = (Action<string>)api.CylinderReset;

            script.Globals["CommSend"] = (Action<string, string>)api.CommSend;
            script.Globals["CommRecv"] = (Func<string, string>)api.CommRecv;

            script.Globals["TrayPick"] = (Action<string>)api.TrayPick;
            script.Globals["TrayPlace"] = (Action<string>)api.TrayPlace;

            // 点位（点位表 / 点位）：会真开控制卡走到点位、真改点位坐标并落盘
            // 注意 Func<...> 的最后一个类型参数是**返回值**：PointModify 有 4 个入参 → Func<string,double,double,double,double>
            script.Globals["PointMove"] = (Func<string, double>)api.PointMove;
            script.Globals["PointModify"] = (Func<string, double, double, double, double>)api.PointModify;
            script.Globals["PointTeach"] = (Func<string, double>)api.PointTeach;

            // 命名空间式 API（与“脚本流程示例”模板一一对应）。
            // 用 Table + CallbackFunction.FromDelegate 暴露，避免把 CLR 实例直接赋给全局
            // （MoonSharp 要求先 UserData.RegisterType，否则会抛“cannot convert clr type”）。
            var vapi = new VariableApi(api);
            var variableTable = new Table(script);
            variableTable["Get"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Func<string, DynValue>)vapi.Get));
            variableTable["Set"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<string, object>)vapi.Set));
            script.Globals["Variable"] = DynValue.NewTable(variableTable);

            var iapi = new IoApi(api);
            var ioTable = new Table(script);
            ioTable["Get"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Func<string, string>)iapi.Get));
            ioTable["Set"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<string, object>)iapi.Set));
            script.Globals["IO"] = DynValue.NewTable(ioTable);

            var aapi = new AxisApi(api);
            var axisTable = new Table(script);
            axisTable["MoveAbs"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<string, double, double>)aapi.MoveAbs));
            axisTable["MoveRel"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<string, double>)aapi.MoveRel));
            axisTable["Home"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<string>)aapi.Home));
            axisTable["Stop"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<string>)aapi.Stop));
            axisTable["WaitDone"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<string>)aapi.WaitDone));
            axisTable["SetSpeed"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<string, double>)aapi.SetSpeed));
            axisTable["Enable"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<string>)aapi.Enable));
            script.Globals["Axis"] = DynValue.NewTable(axisTable);

            var capi = new CylinderApi(api);
            var cylTable = new Table(script);
            cylTable["Out"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<string>)capi.Out));
            cylTable["Back"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<string>)capi.Back));
            cylTable["Reset"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<string>)capi.Reset));
            script.Globals["Cylinder"] = DynValue.NewTable(cylTable);

            // Log 表：提示词/文档里一直用 Log.Info 的写法，但沙箱从来没注册过它，
            // AI 照着写出来的 Lua 一运行就是 attempt to call a nil value。补上别名表。
            var logTable = new Table(script);
            logTable["Info"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<object>)api.LuaLogInfo));
            logTable["Output"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<object>)api.LuaLogInfo));
            logTable["Warn"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<object>)api.LuaLogWarn));
            logTable["Error"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<object>)api.LuaLogError));
            logTable["Debug"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<object>)api.LuaLogDebug));
            script.Globals["Log"] = DynValue.NewTable(logTable);

            script.Globals["EStop"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Func<bool>)api.IsEStop));
            script.Globals["Delay"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<double>)api.Delay));
            // WaitStep：与 Delay 等价的别名。命名上对齐表格流程里 FlowStep.WaitStep(ms) 的 C# 助手与内置「脚本流程」Lua 模板，
            // 让用户写 Lua 时既能写 Delay(ms)（一般延时）也能写 WaitStep(ms)（每步停顿）而不会触发「attempt to call a nil value」。
            script.Globals["WaitStep"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<double>)api.Delay));
            script.Globals["Print"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<object>)api.LuaPrint));

            script.Globals["HardwareStatus"] = (Func<string>)api.HardwareStatus;
            script.Globals["HardwareReady"] = (Func<double>)api.HardwareReady;
            script.Globals["HardwareReconnect"] = (Func<string>)api.HardwareReconnect;
            script.Globals["UseRealHardware"] = (Func<string>)api.UseRealHardware;
            script.Globals["UseCardFamilies"] = (Func<string>)api.UseCardFamilies;
            script.Globals["UseSimulation"] = (Func<string>)api.UseSimulation;
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
