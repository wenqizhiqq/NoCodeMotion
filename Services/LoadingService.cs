// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
using System;
using System.Threading.Tasks;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 全局加载进度服务：在「切换/加载页面」与「打开/新建工程」这类可能耗时的操作期间，
    /// 驱动主窗口的半透明加载遮罩（不确定进度条 + 提示文本）。
    /// 采用引用计数（depth），支持嵌套：例如打开工程会触发页面缓存清空并重建，
    /// 二者各包一层 Show，只有最外层结束（depth 归零）才隐藏遮罩。
    /// </summary>
    public static class LoadingService
    {
        private static int _depth = 0;

        /// <summary>是否处于加载中（depth &gt; 0）。</summary>
        public static bool IsLoading => _depth > 0;

        /// <summary>当前加载提示文本（最外层或最近一次 Show 传入的内容）。</summary>
        public static string Message { get; private set; } = "";

        /// <summary>加载状态变化（Show/Hide）时触发；主窗口订阅以切换遮罩可见性与文本。</summary>
        public static event Action? StateChanged;

        /// <summary>开始一轮加载（可嵌套）。message 为本次提示文本。</summary>
        public static void Show(string message)
        {
            Message = message ?? "";
            _depth++;
            StateChanged?.Invoke();
        }

        /// <summary>结束一轮加载，与 Show 配对；全部结束后清空提示文本。</summary>
        public static void Hide()
        {
            if (_depth > 0) _depth--;
            if (_depth == 0) Message = "";
            StateChanged?.Invoke();
        }

        /// <summary>在加载遮罩下运行一段异步工作；自动 Show/Hide 配对，异常也会正确收尾。</summary>
        public static async Task RunAsync(string message, Func<Task> work)
        {
            Show(message);
            try { await work(); }
            finally { Hide(); }
        }
    }
}
