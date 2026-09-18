// ◆◇※⁣
// ◆温启志◆编写◇微信﹕18719361399　※保留所有权利请勿删除⁣
using System;
using System.Linq;
using NoCodeMotion.Models;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 安全待机服务（静态单例）：
    /// 急停 / 流程异常时，自动用独立 FlowRunControl（estop/stop 均为 false，安全位流程会真正执行）
    /// 跑工程中 Role=Reset 的「复位/安全位流程」，把设备带回安全位置。
    /// 行为受 AlarmConfig.AutoSafeOnEStop / AutoSafeOnException 开关控制。
    /// 复用 FlowRunnerService.RunAllAsync（filter=复位流程），不新增运行器，避免与现有并发模型冲突。
    /// </summary>
    public static class SafetyService
    {
        static SafetyService()
        {
            //StatusBarService.EStopTriggered += () => TriggerSafePosition("急停");
        }

        /// <summary>流程异常时由 FlowRunnerService 调用。</summary>
        public static void OnException(string flow, string message)
        {
            if (!AlarmConfig.Current.AutoSafeOnException) return;
            TriggerSafePosition("异常");
        }

        private static bool _busy;
        private static readonly object _gate = new();

        private static void TriggerSafePosition(string reason)
        {
            if (!AlarmConfig.Current.AutoSafeOnEStop && reason == "急停") return;
            lock (_gate)
            {
                if (_busy) return;
                _busy = true;
            }
            try
            {
                var resetFlows = ProjectStore.Data?.Flows?
                    .Where(f => f.Role == FlowRole.Reset).ToList();
                if (resetFlows == null || resetFlows.Count == 0)
                {
                    StatusBarService.ReportInfo($"[{reason}] 未配置复位/安全位流程，未自动回安全位（请在流程页新增「复位流程」）。");
                    return;
                }
                var ctrl = new FlowRunControl();   // estop/stop=false → 安全位流程会真正执行
                StatusBarService.ReportInfo($"[{reason}] 自动执行安全待机流程：{string.Join("、", resetFlows.Select(f => f.Name ?? ""))}");
                FlowRunnerService.RunAllAsync(
                    ctrl,
                    (m, k) => { try { StatusBarService.ReportInfo(m); } catch { } },
                    (idx, name, step) => { },
                    (idx, name) => { },
                    () => { StatusBarService.ReportInfo("安全待机流程执行完成。"); lock (_gate) { _busy = false; } },
                    default,
                    f => f.Role == FlowRole.Reset);
            }
            catch (Exception ex)
            {
                try { StatusBarService.ReportException($"安全待机流程启动失败：{ex.Message}"); } catch { }
                lock (_gate) { _busy = false; }
            }
        }
    }
}
// ◇作者保留所有权利　请勿删除※⁣
