// ◆◇※⁣
// ◆温启志◆编写◇微信﹕18719361399　※保留所有权利请勿删除⁣
using System;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>信号塔（三色灯）目标状态。</summary>
    public enum TowerState { Idle, Running, Paused, Alarm, EStop }

    /// <summary>
    /// 信号塔（三色灯 + 蜂鸣器）自动驱动服务（静态单例）：
    /// 订阅 StatusBarService.StateChanged，并结合 FlowRunStore 各流程状态综合判定：
    ///   急停 → 红灯 + 蜂鸣；某流程异常 → 红灯 + 蜂鸣；有流程运行中 → 绿灯；有流程暂停 → 黄灯；否则全灭。
    /// 输出通过 AlarmConfig 中配置的输出 IO 名称经 HardwareResolver + HardwareBridge 真正写卡。
    /// 无实物卡（输出未解析到）时静默跳过，不报错。
    /// </summary>
    public static class SignalTowerService
    {
        private static TowerState _state = TowerState.Idle;
        private static System.Timers.Timer? _buzzerTimer;
        private static int _buzzerLeft;

        static SignalTowerService()
        {
            StatusBarService.StateChanged += Update;
        }

        public static TowerState State => _state;

        private static void Update(object? sender, EventArgs e)
        {
            TowerState s = TowerState.Idle;
            if (StatusBarService.EStopped)
            {
                s = TowerState.EStop;
            }
            else
            {
                bool anyRun = false, anyPause = false, anyErr = false;
                var flows = ProjectStore.Data?.Flows;
                if (flows != null)
                {
                    foreach (var f in flows)
                    {
                        var st = FlowRunStore.Get(f).Status;
                        if (st == FlowStatus.Exception) anyErr = true;
                        else if (st == FlowStatus.Running) anyRun = true;
                        else if (st == FlowStatus.Paused || st == FlowStatus.Breakpoint) anyPause = true;
                    }
                }
                if (anyErr || StatusBarService.HasException) s = TowerState.Alarm;
                else if (anyRun) s = TowerState.Running;
                else if (anyPause) s = TowerState.Paused;
                else s = TowerState.Idle;
            }
            Apply(s);
        }

        private static void Apply(TowerState s)
        {
            _state = s;
            Write(AlarmConfig.Current.GreenOutput, 0);
            Write(AlarmConfig.Current.YellowOutput, 0);
            Write(AlarmConfig.Current.RedOutput, 0);
            switch (s)
            {
                case TowerState.Running:
                    Write(AlarmConfig.Current.GreenOutput, 1);
                    StopBuzzer();
                    break;
                case TowerState.Paused:
                    Write(AlarmConfig.Current.YellowOutput, 1);
                    StopBuzzer();
                    break;
                case TowerState.Alarm:
                case TowerState.EStop:
                    Write(AlarmConfig.Current.RedOutput, 1);
                    Beep();
                    break;
                default:
                    StopBuzzer();
                    break;
            }
        }

        private static void Write(string name, int v)
        {
            if (string.IsNullOrEmpty(name)) return;
            try
            {
                var io = HardwareResolver.ResolveOutput(name);
                if (io == null) return;
                HardwareBridge.Current?.WriteOutput(io, v);
            }
            catch { }
        }

        private static void Beep()
        {
            if (string.IsNullOrEmpty(AlarmConfig.Current.BuzzerOutput)) return;
            _buzzerLeft = Math.Max(1, AlarmConfig.Current.BuzzerRepeat);
            if (_buzzerTimer == null)
            {
                _buzzerTimer = new System.Timers.Timer(AlarmConfig.Current.BuzzerOnMs + AlarmConfig.Current.BuzzerOffMs);
                _buzzerTimer.AutoReset = true;
                _buzzerTimer.Elapsed += (_, _) => BuzzerTick();
            }
            _buzzerTimer.Interval = Math.Max(50, AlarmConfig.Current.BuzzerOnMs + AlarmConfig.Current.BuzzerOffMs);
            _buzzerTimer.Start();
            Write(AlarmConfig.Current.BuzzerOutput, 1);
        }

        private static void BuzzerTick()
        {
            if (_buzzerLeft <= 0) { StopBuzzer(); return; }
            _buzzerLeft--;
            Write(AlarmConfig.Current.BuzzerOutput, 1);
            if (_buzzerLeft <= 0)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try { System.Threading.Thread.Sleep(AlarmConfig.Current.BuzzerOnMs); } catch { }
                    Write(AlarmConfig.Current.BuzzerOutput, 0);
                    try { _buzzerTimer?.Stop(); } catch { }
                });
            }
        }

        private static void StopBuzzer()
        {
            _buzzerLeft = 0;
            try { _buzzerTimer?.Stop(); } catch { }
            Write(AlarmConfig.Current.BuzzerOutput, 0);
        }
    }
}
// ◇作者保留所有权利　请勿删除※⁣
