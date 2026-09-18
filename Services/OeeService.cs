// ◆◇※⁣
// ◆温启志◆编写◇微信﹕18719361399　※保留所有权利请勿删除⁣
using System;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// OEE（设备综合效率）统计服务（静态单例）：
    /// 可用率 = 运行时间 / (运行时间 + 停机时间)，由 StatusBarService 状态变化按秒累积；
    /// 性能 = (目标节拍 × 完成轮数) / 运行时间，目标节拍取 AlarmConfig.TargetCycleSec；
    /// 良率 = 良品 / 总量（未统计不良时按 100% 计，可调用 RecordDefect 扣减）；
    /// OEE = 可用率 × 性能 × 良率。
    /// 每完成一轮主流程 OnCycleDone 计入轮数/产量。
    /// </summary>
    public static class OeeService
    {
        private static readonly System.Timers.Timer _timer = new(1000) { AutoReset = true };
        private static DateTime _last = DateTime.Now;
        private static bool _running;
        private static readonly object _lock = new();

        public static long RunTicksMs;     // 运行累计 ms
        public static long DownTicksMs;    // 停机累计 ms
        public static int CycleCount;
        public static int TotalCount => CycleCount;
        public static int GoodCount = -1;  // -1 表示未单独统计良品（按 100% 计）

        static OeeService()
        {
            _timer.Elapsed += (_, _) => Accrue();
            _timer.Start();
            StatusBarService.StateChanged += OnState;
        }

        private static void OnState(object? sender, EventArgs e)
        {
            Accrue();
            _running = StatusBarService.IsRunning && !StatusBarService.EStopped;
            _last = DateTime.Now;
        }

        private static void Accrue()
        {
            var now = DateTime.Now;
            var dt = (long)(now - _last).TotalMilliseconds;
            if (dt <= 0) return;
            lock (_lock)
            {
                if (_running) RunTicksMs += dt; else DownTicksMs += dt;
            }
            _last = now;
        }

        public static void OnCycleDone(long ms)
        {
            CycleCount++;
            if (GoodCount >= 0) GoodCount++;
        }

        /// <summary>记录不良品（扣减良品数），用于良率统计。</summary>
        public static void RecordDefect(int n = 1)
        {
            if (GoodCount < 0) GoodCount = Math.Max(0, CycleCount - n);
            else GoodCount = Math.Max(0, GoodCount - n);
        }

        public static double Availability => (RunTicksMs + DownTicksMs) > 0
            ? RunTicksMs / (double)(RunTicksMs + DownTicksMs) : 0;

        public static double Performance
        {
            get
            {
                double target = AlarmConfig.Current.TargetCycleSec * 1000;
                if (target <= 0 || RunTicksMs <= 0) return 0;
                double ideal = target * CycleCount;
                double p = ideal / RunTicksMs;
                return Math.Min(1.0, p);
            }
        }

        public static double Quality
        {
            get
            {
                if (GoodCount < 0) return 1.0;          // 未统计不良 → 100%
                if (CycleCount <= 0) return 1.0;
                return GoodCount / (double)CycleCount;
            }
        }

        public static double Oee => Availability * Performance * Quality;

        public static void Reset()
        {
            lock (_lock) { RunTicksMs = 0; DownTicksMs = 0; }
            CycleCount = 0; GoodCount = -1; _last = DateTime.Now; _running = false;
        }
    }
}
// ◇作者保留所有权利　请勿删除※⁣
