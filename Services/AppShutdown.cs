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
        /// <summary>停止运行中的流程并断开所有控制器（关闭运动卡，回到离线）。</summary>
        public static void StopAndRelease()
        {
            // 1) 停止所有循环运行的流程（单步/单次流程很快自行结束，这里主要清掉循环）
            FlowLoopManager.StopAll();   // FlowLoopManager 是 NoCodeMotion.ViewModels 下的顶级静态类（与 OperatorViewModel 同写法）

            // 2) 断开所有控制器（关闭卡并清除连接状态）——与界面「断开」按钮同一路径
            var ctrls = ProjectStore.Data?.Controllers?.ToList();
            if (ctrls != null)
            {
                foreach (var c in ctrls)
                {
                    try { HardwareSetup.CardFamilies?.Disconnect(c); }
                    catch { /* 断开失败也不影响继续退出 */ }
                }
            }
        }
    }
}
