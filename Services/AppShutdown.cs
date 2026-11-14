using System.Linq;
using NoCodeMotion.Models;
using NoCodeMotion.Services.Hardware;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 退出时的资源释放：停止运行中的流程 + 断开（关闭）所有运动控制器。
    /// 设计为可在后台线程调用，避免界面卡顿。
    /// </summary>
    public static class AppShutdown
    {
        /// <summary>停止所有循环运行的流程（不动硬件连接，退出前流程还要用轴 / IO）。</summary>
        public static void StopFlows()
        {
            FlowLoopManager.StopAll();
        }

        /// <summary>断开所有控制器（关闭卡并清除连接状态）——与界面「断开」按钮同一路径。</summary>
        public static void ReleaseControllers()
        {
            var ctrls = ProjectStore.Data?.Controllers?.ToList();
            if (ctrls == null) return;
            foreach (var c in ctrls)
            {
                try { HardwareSetup.CardFamilies?.Disconnect(c); }
                catch { /* 断开失败也不影响继续退出 */ }
            }
        }

        /// <summary>停止运行中的流程并断开所有控制器（关闭运动卡，回到离线）。</summary>
        public static void StopAndRelease()
        {
            StopFlows();
            ReleaseControllers();
        }
    }
}
