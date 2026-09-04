// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
using System;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 状态栏全局状态服务（静态单例）：主窗口底部状态栏与全局运行/异常/用户状态的中枢。
    /// 各模块（ProjectManager、OperatorViewModel、App 全局异常）把状态推到这里，
    /// StatusBarView 通过 StatusBarViewModel 订阅 StateChanged 刷新显示。
    /// </summary>
    public static class StatusBarService
    {
        /// <summary>状态变化通知（StatusBarViewModel 订阅后转发属性变更）。</summary>
        public static event EventHandler? StateChanged;

        /// <summary>当前工程名（未打开时为「未打开工程」）。</summary>
        public static string ProjectName { get; private set; } = "未打开工程";

        /// <summary>当前登录用户（取 UserStore.Current）。</summary>
        public static string UserName => UserStore.Current?.Name ?? "未登录";

        /// <summary>当前用户角色（展示用）。</summary>
        public static string UserRole => UserStore.Current?.Role ?? "";

        /// <summary>运行状态文本：空闲 / 运行中 / 急停锁定。</summary>
        public static string RunStatusText { get; private set; } = "空闲";

        public static bool IsRunning { get; private set; }
        public static bool EStopped { get; private set; }

        /// <summary>异常/告警文本（空表示无）。</summary>
        public static string ExceptionText { get; private set; } = "";

        public static bool HasException => !string.IsNullOrWhiteSpace(ExceptionText);

        /// <summary>普通信息文本（如「已生成 N 个点位」），区别于异常，以中性/蓝色展示。</summary>
        public static string InfoText { get; private set; } = "";

        public static bool HasInfo => !string.IsNullOrWhiteSpace(InfoText);

        /// <summary>运行状态圆点/文本颜色。</summary>
        public static string RunColor => EStopped ? "#DC2626" : (IsRunning ? "#16A34A" : "#64748B");

        public static void SetProject(string name)
        {
            ProjectName = string.IsNullOrWhiteSpace(name) ? "未打开工程" : name;
            Raise();
        }

        /// <summary>由 OperatorViewModel 在运行/急停状态变化时调用。</summary>
        public static void SetRunState(bool running, bool estop)
        {
            IsRunning = running;
            EStopped = estop;
            RunStatusText = estop ? "急停锁定" : (running ? "运行中" : "空闲");
            Raise();
        }

        /// <summary>报告一条异常/告警到状态栏（自动带时间）。</summary>
        public static void ReportException(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            ExceptionText = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Raise();
        }

        /// <summary>清除异常提示。</summary>
        public static void ClearException()
        {
            ExceptionText = "";
            Raise();
        }

        /// <summary>报告一条普通信息到状态栏（自动带时间，区别于异常告警）。</summary>
        public static void ReportInfo(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            InfoText = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Raise();
        }

        /// <summary>清除信息提示。</summary>
        public static void ClearInfo()
        {
            InfoText = "";
            Raise();
        }

        /// <summary>用户切换/变更后刷新用户名显示。</summary>
        public static void RefreshUser() => Raise();

        private static void Raise() => StateChanged?.Invoke(null, EventArgs.Empty);
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
