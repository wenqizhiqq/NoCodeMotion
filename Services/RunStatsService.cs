// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※⁣
// ◆温启志◆编写◇微信﹕18719361399　※保留所有权利请勿删除⁣
using System;
using System.Collections.Generic;
using System.Linq;

namespace NoCodeMotion.Services
{
    /// <summary>单步耗时统计快照（某流程的某一步，按 流程#步序#逻辑 去重累计）。</summary>
    public sealed class StepStat
    {
        public string Flow = "";
        public int Index;
        public string Logic = "";
        public long LastMs;
        public long MinMs = long.MaxValue;
        public long MaxMs;
        public double AvgMs;
        public int Count;
    }

    /// <summary>单流程一轮（CT）耗时统计快照。</summary>
    public sealed class CycleStat
    {
        public string Flow = "";
        public long LastMs;
        public long MinMs = long.MaxValue;
        public long MaxMs;
        public double AvgMs;
        public int Count;
        public DateTime LastTime;
    }

    /// <summary>
    /// 运行耗时自动统计服务（静态单例，线程安全）：
    /// 表格流程每步真实计时（FlowExecutor 写入）聚合为「各步平均/最大耗时」；
    /// 每轮（CT）真实计时聚合为「本轮/平均/最大/最小」；
    /// 供操作员页 RunStatsView 展示瓶颈分解与 CSV 导出。
    /// 节点图流程的逐节点耗时由 NgRunner 自身记录，本服务只补表格流程的 CT 视角。
    /// </summary>
    public static class RunStatsService
    {
        private static readonly Dictionary<string, CycleStat> _cycles = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, StepStat> _steps = new(StringComparer.Ordinal);
        private static readonly object _lock = new();

        public static void RecordStep(string flow, int index, string logic, long ms)
        {
            if (string.IsNullOrEmpty(flow)) flow = "(未命名流程)";
            var key = $"{flow}#{index}#{logic}";
            lock (_lock)
            {
                if (!_steps.TryGetValue(key, out var s))
                {
                    s = new StepStat { Flow = flow, Index = index, Logic = logic, MinMs = ms };
                    _steps[key] = s;
                }
                s.LastMs = ms;
                if (ms < s.MinMs) s.MinMs = ms;
                if (ms > s.MaxMs) s.MaxMs = ms;
                s.Count++;
                s.AvgMs += (ms - s.AvgMs) / s.Count;
            }
        }

        public static void RecordCycle(string flow, long ms)
        {
            if (string.IsNullOrEmpty(flow)) flow = "(未命名流程)";
            lock (_lock)
            {
                if (!_cycles.TryGetValue(flow, out var c))
                {
                    c = new CycleStat { Flow = flow, MinMs = ms };
                    _cycles[flow] = c;
                }
                c.LastMs = ms;
                if (ms < c.MinMs) c.MinMs = ms;
                if (ms > c.MaxMs) c.MaxMs = ms;
                c.Count++;
                c.AvgMs += (ms - c.AvgMs) / c.Count;
                c.LastTime = DateTime.Now;
            }
        }

        public static List<CycleStat> SnapshotCycles()
        {
            lock (_lock) return _cycles.Values.OrderBy(x => x.Flow).ToList();
        }

        /// <summary>返回平均耗时最长的 Top-N 步骤（全局，跨流程），用于瓶颈分析。</summary>
        public static List<StepStat> SnapshotSlowSteps(int topN)
        {
            lock (_lock)
            {
                var list = _steps.Values.Where(s => s.Count > 0).OrderByDescending(s => s.AvgMs).ToList();
                if (topN > 0 && list.Count > topN) list = list.Take(topN).ToList();
                return list;
            }
        }

        public static void Reset()
        {
            lock (_lock) { _cycles.Clear(); _steps.Clear(); }
        }
    }
}
// ◇作者保留所有权利　请勿删除※⁣
