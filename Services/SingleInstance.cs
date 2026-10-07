using System.Diagnostics;
using System.Threading;
using System.Windows;
using NoCodeMotion.Views;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 单实例保护：用命名 Mutex 检测是否已有本软件在运行。
    /// 若已多开，弹窗让操作员选择「关闭其它实例并打开本窗口」或「取消打开」。
    /// </summary>
    public static class SingleInstance
    {
        // Local\ 作用域 = 同一会话；带用户名避免不同 Windows 用户互相误挡。
        private static readonly string MutexName =
            @"Local\NoCodeMotion_SingleInstance_" + (System.Environment.UserName ?? "default");

        private static Mutex? _mutex;

        /// <summary>
        /// 确保本进程是唯一的运行实例。
        /// 返回 true = 可以启动主窗口；false = 已取消（调用方应 Shutdown）。
        /// </summary>
        public static bool EnsureSingleInstance()
        {
            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);
            if (createdNew) return true;   // 我们拿到互斥量，独占运行

            // 已有其它实例在运行：弹窗让操作员决策
            var dlg = new InstanceConflictWindow();
            bool? cont = dlg.ShowDialog();
            if (cont != true)
            {
                // 取消打开：释放本进程对互斥量的引用并退出（不重新获取，避免再次独占）
                try { _mutex.Close(); } catch { }
                _mutex = null;
                return false;
            }

            // 关闭其它实例（先优雅关闭，超时再强杀），等待其退出后重新独占
            CloseOtherInstances();
            WaitOtherExit();
            try { _mutex.WaitOne(); }
            catch (AbandonedMutexException) { /* 被遗弃的互斥量 → 现已归本进程所有 */ }
            return true;
        }

        private static void CloseOtherInstances()
        {
            foreach (var p in Process.GetProcessesByName("NoCodeMotion"))
            {
                if (p.Id == System.Environment.ProcessId) continue;
                try
                {
                    // 优先优雅关闭：触发对方 MainWindow.Closing → 进度条释放资源 + 关闭控制器
                    if (!p.CloseMainWindow()) p.Kill();
                }
                catch { try { p.Kill(); } catch { } }
            }
        }

        private static void WaitOtherExit()
        {
            for (int i = 0; i < 80; i++)   // 最多约 8 秒
            {
                bool any = false;
                foreach (var p in Process.GetProcessesByName("NoCodeMotion"))
                {
                    if (p.Id != System.Environment.ProcessId) { any = true; break; }
                }
                if (!any) return;
                Thread.Sleep(100);
            }
            // 超时仍有残留：强制结束
            foreach (var p in Process.GetProcessesByName("NoCodeMotion"))
            {
                if (p.Id == System.Environment.ProcessId) continue;
                try { p.Kill(); } catch { }
            }
        }
    }
}
