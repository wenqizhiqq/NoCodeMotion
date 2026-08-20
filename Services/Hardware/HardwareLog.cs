#nullable disable
using System;
using System.Diagnostics;

namespace NoCodeMotion.Services.Hardware
{
    /// <summary>
    /// 硬件层日志出口。对接实现（雷赛卡 / 通讯通道）不知道界面在哪，统一往这里写；
    /// Lua 执行时由 <see cref="Services.LuaDebugSession"/> 把 <see cref="Sink"/> 指向
    /// 编辑器的输出面板，脚本没有运行时则退回 VS 输出窗口。
    /// </summary>
    public static class HardwareLog
    {
        /// <summary>当前日志接收者（由 Lua 会话在运行期间设置）。</summary>
        public static volatile Action<string> Sink;

        public static void Write(string message)
        {
            var sink = Sink;
            if (sink != null)
            {
                try { sink(message); return; }
                catch { /* 界面已关闭时忽略 */ }
            }
            Debug.WriteLine(message);
        }
    }
}
