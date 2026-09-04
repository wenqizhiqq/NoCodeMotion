// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
using System;
using System.Threading.Tasks;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 全局加载进度服务：在「打开/新建工程」与「启动时预初始化页面」这类可能耗时的操作期间，
    /// 驱动主窗口的半透明加载遮罩（进度条 + 提示文本）。
    /// 采用引用计数（depth），支持嵌套；提供可选的确定式进度（Progress/ProgressMax，&lt;0 表示不确定）。
    /// </summary>
    public static class LoadingService
    {
        private static int _depth = 0;
        private static int _progress = -1; // -1 = 不确定（IsIndeterminate）

        /// <summary>是否处于加载中（depth &gt; 0）。</summary>
        public static bool IsLoading => _depth > 0;

        /// <summary>当前加载提示文本。</summary>
        public static string Message { get; private set; } = "";

        /// <summary>当前进度（0..ProgressMax）；小于 0 表示不确定进度（无限滚动条）。</summary>
        public static int Progress { get => _progress; private set => _progress = value; }

        /// <summary>进度上限（默认 100）。</summary>
        public static int ProgressMax { get; set; } = 100;

        /// <summary>加载状态变化（Show/Hide/Report）时触发；主窗口订阅以切换遮罩。</summary>
        public static event Action? StateChanged;

        /// <summary>开始一轮加载（可嵌套）。重置为「不确定」进度。</summary>
        public static void Show(string message)
        {
            Message = message ?? "";
            _progress = -1;
            _depth++;
            StateChanged?.Invoke();
        }

        /// <summary>更新确定式进度；message 可选。进度按 0..ProgressMax 在遮罩进度条上显示。</summary>
        public static void Report(int value, string? message = null)
        {
            if (message != null) Message = message;
            Progress = value;
            StateChanged?.Invoke();
        }

        /// <summary>结束一轮加载，与 Show 配对；全部结束后清空提示文本与进度。</summary>
        public static void Hide()
        {
            if (_depth > 0) _depth--;
            if (_depth == 0) { Message = ""; _progress = -1; }
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
