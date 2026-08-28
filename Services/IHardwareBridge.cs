// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 硬件对接接口（对接点 / 驱动层）。
    ///
    /// 这是 Lua 运动控制函数与真实硬件之间的唯一桥梁。Lua 里写的
    /// <c>AxisMove("轴1")</c>、<c>SetIO("输出1", 1)</c> 等，经 HardwareApi 把名称解析成
    /// 项目里配置好的对象（AxisItem / IoItem / CylinderItem / CommItem / TrayItem）后，
    /// 最终调用本接口的对应方法。你只需要实现这个接口，把动作下发到真实设备
    /// （运动控制卡、IO 卡、PLC、串口、网口、视觉等），再把实现赋值给
    /// <see cref="HardwareBridge.Current"/>，Lua 脚本就能驱动真实硬件。
    ///
    /// 未实现时框架默认使用 <see cref="StubHardwareBridge"/>：只打日志、返回安全默认值，
    /// 让脚本在没有硬件的情况下也能完整跑通（便于先写流程、后接设备）。
    /// </summary>
    public interface IHardwareBridge
    {
        /// <summary>输出对接日志，会显示在 Lua 编辑器的输出面板，方便排查对接问题。</summary>
        void Log(string message);

        // ===================== 轴 Axis =====================

        /// <summary>按对象配置的“目标位置”驱动轴运动到目标位。</summary>
        void MoveAxis(AxisItem axis);

        /// <summary>设置轴的运行速度（单位取决于 AxisItem.Unit）。</summary>
        void SetAxisSpeed(AxisItem axis, double speed);

        /// <summary>执行回零（HomeMode / HomeSpeed 等参数取自 axis）。</summary>
        void HomeAxis(AxisItem axis);

        /// <summary>立即停止轴。</summary>
        void StopAxis(AxisItem axis);

        /// <summary>阻塞等待轴到位（到位误差 InPosError），超时按 axis 配置处理。</summary>
        void WaitAxisDone(AxisItem axis);

        /// <summary>使能 / 解除使能轴（EnableLevel 等参数取自 axis）。</summary>
        void EnableAxis(AxisItem axis);

        /// <summary>相对当前位置移动 distance（单位同 axis.Unit）。</summary>
        void MoveAxisRel(AxisItem axis, double distance);

        /// <summary>运动到绝对位置 position（单位同 axis.Unit）。</summary>
        void MoveAxisAbs(AxisItem axis, double position);

        // ===================== 输入 / 输出 IO =====================

        /// <summary>读取输入点当前值，返回 0 / 1（或其它电平值）。</summary>
        double ReadInput(IoItem io);

        /// <summary>阻塞等待输入点变为 value（0/1），超时由对接层按设备处理。</summary>
        void WaitInput(IoItem io, int value);

        /// <summary>设置输出点电平为 value（0/1）。</summary>
        void WriteOutput(IoItem io, int value);

        /// <summary>把输出点电平取反（1→0，0→1）。</summary>
        void ToggleOutput(IoItem io);

        // ===================== 气缸 Cylinder =====================

        /// <summary>驱动气缸动作：state=1 伸出，state=0 缩回。</summary>
        void CylinderMove(CylinderItem cyl, int state);

        /// <summary>阻塞等待气缸到位（按 OutPoint / SensorExtend / SensorRetract 配置轮询）。</summary>
        void WaitCylinder(CylinderItem cyl);

        /// <summary>气缸复位到初始状态（InitialState）。</summary>
        void CylinderReset(CylinderItem cyl);

        // ===================== 通讯 Comm =====================

        /// <summary>通过指定通讯通道发送数据（CommType / PortOrIp / BaudOrPort 等取自 comm）。</summary>
        void CommSend(CommItem comm, string data);

        /// <summary>从指定通讯通道接收一行 / 一段数据并返回。</summary>
        string CommRecv(CommItem comm);

        // ===================== 料盘 Tray =====================

        /// <summary>在料盘取料（按 StartX/Y、PitchX/Y 等布局换算坐标）。</summary>
        void TrayPick(TrayItem tray);

        /// <summary>在料盘放料（按 StartX/Y、PitchX/Y 等布局换算坐标）。</summary>
        void TrayPlace(TrayItem tray);
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
