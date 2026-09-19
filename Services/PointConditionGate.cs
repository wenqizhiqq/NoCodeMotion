// ◆◇※⁣
// ◆温启志◆编写◇微信﹕18719361399　※保留所有权利请勿删除⁣
using System.Collections.Generic;
using System.Text;
using System.Windows;
using NoCodeMotion.Models;
using NoCodeMotion.Views;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 点位移动前的防撞闸门：把「条件不满足」按使用场景分流——
    ///   人工操作（点位页「移动」按钮 / 操作员页手动单步）→ 弹窗，让操作者看到原因并自行处理；
    ///   流程 / 自动运行（无人值守）→ 写报警列表，绝不弹窗（弹窗会把流程永久卡死）。
    /// 两个入口都返回 bool：true = 允许移动，false = 已阻止。
    /// 判断口径统一走 <see cref="PointConditionService.Evaluate"/>，与界面「实际值」列同源。
    /// </summary>
    public static class PointConditionGate
    {
        /// <summary>人工移动：条件不满足时弹窗列出全部未满足项，返回 false 阻止移动。</summary>
        public static bool EnsureInteractive(PointItem? point)
        {
            var fails = PointConditionService.Evaluate(point);
            if (fails.Count == 0) return true;
            ShowBlockedDialog(point, fails);
            return false;
        }

        /// <summary>
        /// 弹出「移动条件未满足」提示（供已自行 Evaluate 过的调用方复用，避免重复判断）。
        /// 可在后台线程调用：会自动切到 UI 线程并**同步等待**弹窗关闭，
        /// 这样调用方拿到的「已阻止」结论与操作者看到提示是同一时刻。
        /// </summary>
        public static void ShowBlockedDialog(PointItem? point, IReadOnlyList<string> fails)
        {
            if (fails.Count == 0) return;

            string name = point?.Name ?? "";
            string text = BuildText(name, fails);

            void Show()
            {
                try
                {
                    var dlg = new ExceptionDetailDialog
                    {
                        Header = "移动条件未满足",
                        Message = text,
                    };
                    dlg.ShowDialog();
                }
                catch
                {
                    // 弹窗起不来（无 UI 宿主等）也必须留痕，退化为状态栏提示
                    try { StatusBarService.ReportException($"[防撞] 点位「{name}」{fails.Count} 条移动条件未满足，已阻止移动。"); } catch { }
                }
            }

            var app = Application.Current;
            if (app?.Dispatcher != null && !app.Dispatcher.CheckAccess())
            {
                // 后台线程（如操作员页手动单步跑在 OpStep 线程）→ 切 UI 线程同步执行
                try { app.Dispatcher.Invoke(Show); return; }
                catch { /* 调度器已关闭 → 落到下面直接执行 */ }
            }
            Show();
        }

        /// <summary>
        /// 流程 / 自动运行：条件不满足时逐条写入报警列表（不弹窗），返回 false 阻止移动。
        /// 每条未满足项单独一条报警，便于操作者按条排查。
        /// </summary>
        public static bool EnsureSilent(PointItem? point, string source, string? flow)
        {
            var fails = PointConditionService.Evaluate(point);
            if (fails.Count == 0) return true;
            RaiseAlarms(point, fails, source, flow);
            return false;
        }

        /// <summary>
        /// 把未满足项逐条写入报警列表（供已自行 Evaluate 过的调用方复用，避免重复判断）。
        /// 每条未满足项单独一条报警，便于操作者按条排查。
        /// </summary>
        public static void RaiseAlarms(PointItem? point, IReadOnlyList<string> fails, string source, string? flow)
        {
            if (fails.Count == 0) return;
            string name = point?.Name ?? "";
            foreach (var f in fails)
                AlarmService.Raise(LogLevel.Error, source, flow ?? "", $"点位「{name}」移动条件未满足：{f}");
        }

        /// <summary>把未满足项拼成弹窗正文。</summary>
        private static string BuildText(string pointName, IReadOnlyList<string> fails)
        {
            var sb = new StringBuilder();
            sb.Append("点位「").Append(pointName).Append("」有 ").Append(fails.Count)
              .Append(" 条移动条件未满足，已阻止移动：\r\n\r\n");
            for (int i = 0; i < fails.Count; i++)
                sb.Append(i + 1).Append(". ").Append(fails[i]).Append("\r\n");
            sb.Append("\r\n请先排除上述原因（或取消对应条件行的「使用」勾选）后重试。");
            return sb.ToString();
        }
    }
}
// ◇作者保留所有权利　请勿删除※⁣
