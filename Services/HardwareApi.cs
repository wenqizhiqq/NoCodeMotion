// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
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

            /// <summary>写入变量值（数字或字符串都会以字符串形式存回变量表，并实时打印）。</summary>
            public void Set(string name, object value)
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

            script.Globals["EStop"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Func<bool>)api.IsEStop));
            script.Globals["Delay"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<double>)api.Delay));
            script.Globals["Print"] = DynValue.NewCallback(CallbackFunction.FromDelegate(script, (Action<object>)api.LuaPrint));

            script.Globals["HardwareStatus"] = (Func<string>)api.HardwareStatus;
            script.Globals["HardwareReady"] = (Func<double>)api.HardwareReady;
            script.Globals["HardwareReconnect"] = (Func<string>)api.HardwareReconnect;
            script.Globals["UseRealHardware"] = (Func<string>)api.UseRealHardware;
            script.Globals["UseSimulation"] = (Func<string>)api.UseSimulation;
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
