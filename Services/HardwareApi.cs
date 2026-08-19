#nullable disable
using System;
using System.Linq;
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
        }
    }
}
