#nullable disable
using System.Threading;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 无真实硬件时的默认对接桩。
    ///
    /// 所有方法只把“将要执行什么动作 + 解析到的硬件地址”打到 Lua 输出面板，
    /// 并返回安全默认值，使脚本在没有设备时也能完整跑通。
    ///
    /// 这是 <see cref="IHardwareBridge"/> 的参考实现：你做真实对接时，照着每个方法的日志里
    /// 出现的字段（卡号 / 模块 / 通道 / 端口 / 坐标…）去下发对应的硬件指令即可。
    /// 例：把 <c>MoveAxis</c> 里的 <c>axis.AxisNo</c> 当作运动控制卡通道号，调用你自己的驱动。
    /// </summary>
    public sealed class StubHardwareBridge : IHardwareBridge
    {
        private readonly bool _simulateWait;

        public StubHardwareBridge(bool simulateWait = true)
        {
            _simulateWait = simulateWait;
        }

        public void Log(string message) => Write("[桩] " + message);

        // ---- 轴 ----
        public void MoveAxis(AxisItem axis) =>
            Write($"轴运动 → 名称={axis.Name} 轴号={axis.AxisNo} 单位={axis.Unit} 速度={axis.Speed}");
        public void SetAxisSpeed(AxisItem axis, double speed) =>
            Write($"轴设速 → 名称={axis.Name} 轴号={axis.AxisNo} 速度={speed}");
        public void HomeAxis(AxisItem axis) =>
            Write($"轴回零 → 名称={axis.Name} 轴号={axis.AxisNo} 模式={axis.HomeMode}");
        public void StopAxis(AxisItem axis) =>
            Write($"轴停止 → 名称={axis.Name} 轴号={axis.AxisNo}");
        public void WaitAxisDone(AxisItem axis)
        {
            Write($"等待轴到位 → 名称={axis.Name} 轴号={axis.AxisNo} 误差={axis.InPosError}");
            Simulate();
        }
        public void EnableAxis(AxisItem axis) =>
            Write($"轴使能 → 名称={axis.Name} 轴号={axis.AxisNo}");
        public void MoveAxisRel(AxisItem axis, double distance) =>
            Write($"轴相对移动 → 名称={axis.Name} 轴号={axis.AxisNo} 位移={distance}");
        public void MoveAxisAbs(AxisItem axis, double position) =>
            Write($"轴绝对移动 → 名称={axis.Name} 轴号={axis.AxisNo} 目标={position}");

        // ---- IO ----
        public double ReadInput(IoItem io)
        {
            Write($"读输入 → 名称={io.Name} 卡类={io.CardType} 卡号={io.CardNo} 模块={io.ModuleNo} 序号={io.Sequence}");
            return io.Value;
        }
        public void WaitInput(IoItem io, int value)
        {
            Write($"等待输入 → 名称={io.Name} 卡号={io.CardNo} 模块={io.ModuleNo} 序号={io.Sequence} 目标={value}");
            Simulate();
        }
        public void WriteOutput(IoItem io, int value) =>
            Write($"写输出 → 名称={io.Name} 卡类={io.CardType} 卡号={io.CardNo} 模块={io.ModuleNo} 序号={io.Sequence} 值={value}");
        public void ToggleOutput(IoItem io) =>
            Write($"输出取反 → 名称={io.Name} 卡号={io.CardNo} 模块={io.ModuleNo} 序号={io.Sequence}");

        // ---- 气缸 ----
        public void CylinderMove(CylinderItem cyl, int state)
        {
            string act = state == 1 ? cyl.Action : "缩回";
            Write($"气缸动作 → 名称={cyl.Name} 设备={cyl.DeviceId} 动作={act} 输出点={cyl.OutPoint} 伸感应={cyl.SensorExtend} 缩感应={cyl.SensorRetract}");
        }
        public void WaitCylinder(CylinderItem cyl)
        {
            Write($"等待气缸到位 → 名称={cyl.Name} 设备={cyl.DeviceId} 伸感应={cyl.SensorExtend} 缩感应={cyl.SensorRetract} 超时={cyl.TimeoutMs}ms");
            Simulate();
        }
        public void CylinderReset(CylinderItem cyl) =>
            Write($"气缸复位 → 名称={cyl.Name} 设备={cyl.DeviceId} 初始={cyl.InitialState}");

        // ---- 通讯 ----
        public void CommSend(CommItem comm, string data) =>
            Write($"通讯发送 → 名称={comm.Name} 类型={comm.CommType} 地址={comm.PortOrIp} 波特/端口={comm.BaudOrPort} 数据={data}");
        public string CommRecv(CommItem comm)
        {
            Write($"通讯接收 → 名称={comm.Name} 类型={comm.CommType} 地址={comm.PortOrIp} 超时={comm.TimeoutMs}ms");
            return string.Empty;
        }

        // ---- 料盘 ----
        public void TrayPick(TrayItem tray) =>
            Write($"料盘取料 → 名称={tray.Name} 起点=({tray.StartX},{tray.StartY}) 间距=({tray.PitchX},{tray.PitchY})");
        public void TrayPlace(TrayItem tray) =>
            Write($"料盘放料 → 名称={tray.Name} 起点=({tray.StartX},{tray.StartY}) 间距=({tray.PitchX},{tray.PitchY})");

        private static void Write(string s) =>
            System.Diagnostics.Debug.WriteLine(s);

        private void Simulate()
        {
            if (_simulateWait) Thread.Sleep(20);
        }
    }
}
