// ◆◇※⁣
// ◆温启志◆编写◇微信﹕18719361399　※保留所有权利请勿删除⁣
using System;
using System.Collections.ObjectModel;
using System.Windows;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>单条报警记录（供界面「报警列表」面板绑定）。</summary>
    public sealed class AlarmEntry
    {
        /// <summary>发生时间。</summary>
        public DateTime Time { get; set; }

        /// <summary>等级：Error=报警、Warn=警告。</summary>
        public LogLevel Level { get; set; }

        /// <summary>来源：流程 / 自动运行 / 操作员 / 点位。</summary>
        public string Source { get; set; } = "";

        /// <summary>关联流程名（无则空）。</summary>
        public string Flow { get; set; } = "";

        /// <summary>报警内容。</summary>
        public string Message { get; set; } = "";

        /// <summary>时间显示（列表用）。</summary>
        public string TimeText => Time.ToString("HH:mm:ss");

        /// <summary>等级显示文本。</summary>
        public string LevelText => Level == LogLevel.Error ? "报警" : "警告";

        /// <summary>来源显示：带流程名时拼上（如「流程·上料」），便于定位是哪个流程报的。</summary>
        public string SourceText => string.IsNullOrWhiteSpace(Flow) ? Source : $"{Source}·{Flow}";
    }

    /// <summary>
    /// 报警列表服务（静态单例，线程安全）：
    /// 流程 / 自动运行中出现「点位移动条件未满足」等异常时写入这里，供界面「报警列表」面板显示。
    /// 与人工操作路径区分——无人值守时弹窗会把流程卡死，所以流程一律走报警列表。
    /// 同时转发 ExceptionReportService 落 CSV，并推一条到状态栏，保证不打开流程页也能看见。
    /// </summary>
    public static class AlarmService
    {
        /// <summary>列表封顶条数，超出丢弃最旧的。</summary>
        private const int Cap = 500;

        private static readonly ObservableCollection<AlarmEntry> _entries = new();
        private static readonly object _lock = new();

        /// <summary>
        /// 报警列表（新的在最前），界面直接绑定。
        /// ObservableCollection 自身对 Count 发 INotifyPropertyChanged，
        /// 所以「绑定 Count 做角标 / 空列表折叠」无需额外 VM。
        /// </summary>
        public static ObservableCollection<AlarmEntry> Entries => _entries;

        /// <summary>
        /// 记录一条报警。
        /// <paramref name="source"/> 为来源（如「流程」「自动运行」），<paramref name="flow"/> 为流程名（无则传空）。
        /// 可在任意线程调用：集合改动与状态栏提示会自动切到 UI 线程。
        /// </summary>
        public static void Raise(LogLevel level, string source, string flow, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            var rec = new AlarmEntry
            {
                Time = DateTime.Now,
                Level = level,
                Source = source ?? "",
                Flow = flow ?? "",
                Message = message,
            };

            // 落 CSV 在调用线程直接做（ExceptionReportService 内部有锁），
            // 不把文件 IO 丢给 UI 线程。
            try { ExceptionReportService.Record(rec.Source, rec.Flow, rec.Message); } catch { }

            void Apply()
            {
                Insert(rec);
                try
                {
                    if (level == LogLevel.Error)
                        StatusBarService.ReportException($"[{rec.Source}] {rec.Message}");
                    else
                        StatusBarService.ReportInfo($"[{rec.Source}] {rec.Message}");
                }
                catch { }
            }

            var app = Application.Current;
            if (app?.Dispatcher != null && !app.Dispatcher.CheckAccess())
            {
                try { app.Dispatcher.BeginInvoke(new Action(Apply)); return; }
                catch { /* 调度器不可用（如无 UI 的宿主）→ 退化为直接执行 */ }
            }
            Apply();
        }

        private static void Insert(AlarmEntry rec)
        {
            lock (_lock)
            {
                _entries.Insert(0, rec);
                while (_entries.Count > Cap) _entries.RemoveAt(_entries.Count - 1);
            }
        }

        /// <summary>清空报警列表（面板「清空」按钮）。</summary>
        public static void Clear()
        {
            var app = Application.Current;
            if (app?.Dispatcher != null && !app.Dispatcher.CheckAccess())
            {
                try { app.Dispatcher.BeginInvoke(new Action(Clear)); return; }
                catch { }
            }
            lock (_lock) { _entries.Clear(); }
        }
    }
}
// ◇作者保留所有权利　请勿删除※⁣
