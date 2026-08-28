// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>时序编译严重度（绑定到结果面板着色）。</summary>
    public enum TimingSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>编译期时序问题：绑定到「编译时序」结果面板，按严重度着色。</summary>
    public class TimingIssue
    {
        public TimingSeverity Severity { get; set; } = TimingSeverity.Info;
        public string PointName { get; set; } = string.Empty;
        public string SyncGroup { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 时序编译引擎（对应专利「方向二：表格化时序确定性编排」）：
    /// 将点位表的「时序标记 / 同步组」列编译为实时调度约束，并在编译期做冲突检测
    /// —— 同周期一致性、总线周期分辨率、同步组规模/单周期可行性、同周期资源争用。
    /// 纯算法，无真实 EtherCAT 硬件依赖（仿真用）。
    /// </summary>
    public static class TimingCompiler
    {
        /// <summary>总线 DC 同步周期（毫秒），用于时延分辨率校验。</summary>
        public const double BusCycleMs = 1.0;

        /// <summary>单 DC 周期可服务的同步动作上限，超出则该同步组可能无法在单一周期内完成。</summary>
        public const int MaxActionsPerCycle = 8;

        /// <summary>
        /// 解析时序标记文本为相对工艺起点的触发时刻（毫秒）。
        /// 支持 "T+5ms" / "T+0ms" / "5ms" / "T+5" / "5" / "0"；无法识别返回 null。
        /// </summary>
        public static double? ParseTimingMark(string? mark)
        {
            if (string.IsNullOrWhiteSpace(mark)) return null;
            var s = mark!.Trim();

            // 去掉 "T+" 前缀（大小写不敏感）或单独的 "T" 前缀
            int plus = s.IndexOf('+');
            if (plus >= 0)
                s = s.Substring(plus + 1);
            else if (s.StartsWith("T", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(1);

            // 去掉单位（ms / mS）
            if (s.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(0, s.Length - 2);
            s = s.Trim();

            if (s.Length == 0) return null;
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
        }

        /// <summary>编译一组点位，返回按出现顺序的时序问题列表（空表示通过）。</summary>
        public static IReadOnlyList<TimingIssue> Compile(IEnumerable<PointItem> points)
        {
            var issues = new List<TimingIssue>();
            var list = points as IList<PointItem> ?? points.ToList();

            // 1) 解析错误 + 2) 总线周期分辨率
            foreach (var p in list)
            {
                var raw = (p.TimingMark ?? string.Empty).Trim();
                if (raw.Length == 0) continue; // 未配置时序标记不报错，运行时标记为「未配置」

                var ms = ParseTimingMark(raw);
                if (ms == null)
                {
                    issues.Add(new TimingIssue
                    {
                        Severity = TimingSeverity.Error,
                        PointName = p.Name,
                        Message = $"时序标记「{raw}」无法识别，应为 T+5ms / 5ms / 0 等形式。"
                    });
                    continue;
                }

                var rem = ms.Value % BusCycleMs;
                if (rem > 1e-6 && rem < BusCycleMs - 1e-6)
                {
                    issues.Add(new TimingIssue
                    {
                        Severity = TimingSeverity.Warning,
                        PointName = p.Name,
                        SyncGroup = p.SyncGroup,
                        Message = $"时序标记 T+{ms.Value}ms 低于总线周期分辨率 {BusCycleMs}ms，将按 {BusCycleMs}ms 取整，建议对齐到整毫秒。"
                    });
                }
            }

            // 3) 同步组一致性（同组应同一时刻原子执行）+ 4) 同步组规模 / 单周期可行性
            var groups = list
                .Where(p => !string.IsNullOrWhiteSpace(p.SyncGroup))
                .GroupBy(p => p.SyncGroup!.Trim())
                .ToList();
            foreach (var g in groups)
            {
                var marks = g
                    .Select(p => ParseTimingMark(p.TimingMark))
                    .Where(m => m.HasValue)
                    .Select(m => m!.Value)
                    .Distinct()
                    .ToList();

                if (marks.Count > 1)
                {
                    foreach (var p in g)
                    {
                        var pm = ParseTimingMark(p.TimingMark);
                        if (pm == null) continue;
                        issues.Add(new TimingIssue
                        {
                            Severity = TimingSeverity.Error,
                            PointName = p.Name,
                            SyncGroup = g.Key,
                            Message = $"同步组 {g.Key} 内点位「{p.Name}」时序标记（T+{pm.Value}ms）与组内其它点位不一致，同组应同一时刻原子执行。"
                        });
                    }
                }

                var count = g.Count();
                if (count > MaxActionsPerCycle)
                {
                    issues.Add(new TimingIssue
                    {
                        Severity = TimingSeverity.Warning,
                        SyncGroup = g.Key,
                        Message = $"同步组 {g.Key} 含 {count} 个动作，超过单 DC 周期可服务上限 {MaxActionsPerCycle}，可能无法在单一周期内完成。"
                    });
                }
            }

            // 5) 同周期资源争用：同一时序标记 T 同时被某同步组与独立（无组）点位占用
            var byMark = list
                .Where(p => !string.IsNullOrWhiteSpace(p.TimingMark))
                .Select(p => new { Point = p, Ms = ParseTimingMark(p.TimingMark) })
                .Where(x => x.Ms.HasValue)
                .GroupBy(x => x.Ms!.Value)
                .ToList();
            foreach (var bucket in byMark)
            {
                var grouped = bucket.Where(x => !string.IsNullOrWhiteSpace(x.Point.SyncGroup)).ToList();
                var solo = bucket.Where(x => string.IsNullOrWhiteSpace(x.Point.SyncGroup)).ToList();
                if (grouped.Count == 0 || solo.Count == 0) continue;

                var gName = grouped.First().Point.SyncGroup!.Trim();
                foreach (var s in solo)
                {
                    issues.Add(new TimingIssue
                    {
                        Severity = TimingSeverity.Warning,
                        PointName = s.Point.Name,
                        SyncGroup = gName,
                        Message = $"时序标记 T+{bucket.Key}ms 同时被同步组 {gName} 与独立点位「{s.Point.Name}」占用，存在同周期资源争用。"
                    });
                }
            }

            return issues;
        }
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
