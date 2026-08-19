#nullable disable
namespace NoCodeMotion.Services
{
    /// <summary>
    /// 硬件对接层解析器（单例入口）。
    ///
    /// 框架运行时从这里取当前生效的 <see cref="IHardwareBridge"/>。默认是
    /// <see cref="StubHardwareBridge"/>（无硬件也能跑）。接入真实设备时，在程序启动处
    /// 把 <see cref="Current"/> 换成你自己的实现即可：
    ///
    /// <code>
    /// HardwareBridge.Current = new MyMotionCardBridge();   // 实现 IHardwareBridge
    /// </code>
    ///
    /// 该属性线程安全（volatile 读），Lua 脚本执行线程会在每次运行时读取一次。
    /// </summary>
    public static class HardwareBridge
    {
        /// <summary>当前生效的硬件对接实现。默认无硬件桩。</summary>
        public static volatile IHardwareBridge Current = new StubHardwareBridge();

        /// <summary>替换为自定义对接实现（如运动控制卡 / PLC 驱动）。</summary>
        public static void SetBridge(IHardwareBridge bridge) =>
            Current = bridge ?? new StubHardwareBridge();
    }
}
