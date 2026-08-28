// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.Generic;
using System.Linq;

namespace NoCodeMotion.Services;

/// <summary>
/// 表格化时序确定性编排引擎（专利核心）。
/// 把流程表中每行的「时序标记 + 同步组」编译为实时任务调度表（确定性调度序列），
/// 并在编译期做时序冲突静态检测。本类与具体 Model 解耦：只消费 TimingStepInput，
/// 因此既可驱动真实的 FlowStep（接入后），也可驱动独立的演示模型。
/// </summary>
public enum TimingConflictSeverity { Info, Warning, Error }

/// <summary>单步输入的轻量载体：步骤/动作/目标/时序标记/同步组。</summary>
public sealed record TimingStepInput(
    int Step,
    string Action,
    string Target,
    string TimingMark,
    string SyncGroup);

/// <summary>编译产物：调度表中的一条确定性事件（同一同步组在统一总线周期边界原子触发）。</summary>
public sealed record ScheduledEvent(
    int Step,
    string Action,
    string Target,
    string SyncGroup,
    double AbsoluteMs,
    int BusCycleIndex);

/// <summary>编译结果：确定性调度事件序列 + 时序标记解析告警。</summary>
public sealed class TimingCompileResult
{
    public List<ScheduledEvent> Events { get; } = new();
    public List<string> ParseWarnings { get; } = new();
    public bool Ok => ParseWarnings.Count == 0;
}

/// <summary>编译期时序冲突（同步组可行性 / 时延分辨率 / 资源争用死锁 / 累积闭合）。</summary>
public sealed record TimingConflict(TimingConflictSeverity Severity, string Message, IReadOnlyList<int> Steps);

public static class TimingSequenceEngine
{
    /// <summary>
    /// 把「时序标记 + 同步组」编译为实时任务调度表。
    /// 步骤：① 解析每步时序标记为相对工艺起点的绝对触发时刻（ms）；
    /// ② 同一同步组内动作合并为同一同步事件，对齐到总线周期网格（BusCycleIndex）；
    /// ③ 输出确定性调度序列。
    /// </summary>
    /// <param name="steps">流程步骤（按表格行序）。</param>
    /// <param name="busCycleMs">实时总线周期分辨率（默认 1 ms）。</param>
    public static TimingCompileResult Compile(IEnumerable<TimingStepInput> steps, double busCycleMs = 1.0)
    {
        var result = new TimingCompileResult();
        if (busCycleMs <= 0) busCycleMs = 1.0;

        double prevAbsolute = 0.0;
        var ordered = steps?.ToList() ?? new List<TimingStepInput>();
        foreach (var s in ordered)
        {
            string mark = (s.TimingMark ?? "").Trim();
            double abs;
            if (string.IsNullOrEmpty(mark) || mark is "auto" or "跟随" or "—" or "-")
            {
                // 缺省：跟随前序（专利：按序累加或与前序同组），即与前一步同一触发时刻
                abs = prevAbsolute;
            }
            else
            {
                if (!TryParseTimingMark(mark, out double rel))
                {
                    result.ParseWarnings.Add($"步骤 {s.Step}：时序标记「{mark}」无法解析，已按 0ms 处理");
                    abs = prevAbsolute;
                }
                else
                {
                    abs = prevAbsolute + rel;
                }
            }
            prevAbsolute = abs;
            int cycle = (int)Math.Round(abs / busCycleMs);
            result.Events.Add(new ScheduledEvent(s.Step, s.Action, s.Target, s.SyncGroup?.Trim() ?? "", abs, cycle));
        }
        return result;
    }

    /// <summary>
    /// 编译期时序冲突检测（专利权利要求 6 / 具体实施方式 5）。
    /// 覆盖：① 同步组内动作是否可在单一总线周期内完成；② 相对时延是否满足总线周期分辨率；
    /// ③ 同步组间资源争用与潜在死锁；④ 前后依赖同步组链的累积时序闭合。
    /// </summary>
    public static List<TimingConflict> CheckConflicts(IEnumerable<TimingStepInput> steps, TimingCompileResult schedule, double busCycleMs = 1.0)
    {
        var conflicts = new List<TimingConflict>();
        if (busCycleMs <= 0) busCycleMs = 1.0;
        var list = steps?.ToList() ?? new List<TimingStepInput>();
        if (list.Count == 0) return conflicts;

        // ① 同步组单周期可行性
        var byGroup = list.Where(x => !string.IsNullOrWhiteSpace(x.SyncGroup))
                          .GroupBy(x => x.SyncGroup!.Trim());
        foreach (var g in byGroup)
        {
            double tact = g.Sum(x => ActionDurationMs(x.Action));
            if (tact > busCycleMs)
                conflicts.Add(new TimingConflict(TimingConflictSeverity.Error,
                    $"同步组「{g.Key}」内动作估算耗时 T_action={tact:F2}ms 大于总线周期 T_cycle={busCycleMs:F2}ms，无法在单一周期内完成",
                    g.Select(x => x.Step).ToList()));
        }

        // ② 相对时延分辨率（仅校验显式写明的相对标记）
        double prev = 0.0;
        foreach (var s in list)
        {
            string mark = (s.TimingMark ?? "").Trim();
            if (string.IsNullOrEmpty(mark) || mark is "auto" or "跟随" or "—" or "-") { prev = double.NaN; continue; }
            if (TryParseTimingMark(mark, out double rel))
            {
                if (Math.Abs(rel) < busCycleMs - 1e-9)
                    conflicts.Add(new TimingConflict(TimingConflictSeverity.Warning,
                        $"步骤 {s.Step}：相对时延 {rel:F2}ms 小于总线周期分辨率 {busCycleMs:F2}ms",
                        new List<int> { s.Step }));
                prev = rel;
            }
        }

        // ③ 资源争用 / 死锁：同一目标（轴/IO 点位）被多个不同同步组争用
        var byTarget = list.Where(x => !string.IsNullOrWhiteSpace(x.Target))
                           .GroupBy(x => x.Target!.Trim());
        foreach (var t in byTarget)
        {
            var groups = t.Select(x => x.SyncGroup?.Trim() ?? "")
                          .Where(g => !string.IsNullOrEmpty(g))
                          .Distinct().ToList();
            if (groups.Count >= 2)
                conflicts.Add(new TimingConflict(TimingConflictSeverity.Warning,
                    $"目标「{t.Key}」被多个同步组争用（{string.Join("、", groups)}），存在资源争用/潜在死锁风险",
                    t.Select(x => x.Step).ToList()));
        }

        // ④ 累积时序闭合：按触发时刻排序的同步组链，估算最坏累积偏差 vs 工艺阈值
        const double thresholdMs = 5.0;
        var groupTimes = byGroup.Select(g => new
        {
            Name = g.Key,
            Min = schedule.Events.Where(e => e.SyncGroup == g.Key).Min(e => e.AbsoluteMs),
            Max = schedule.Events.Where(e => e.SyncGroup == g.Key).Max(e => e.AbsoluteMs)
        }).OrderBy(x => x.Min).ToList();
        double cum = 0.0;
        foreach (var gt in groupTimes)
        {
            cum += (gt.Max - gt.Min) + 0.5; // 每周期最坏 ±0.5ms 抖动近似
            if (cum > thresholdMs)
                conflicts.Add(new TimingConflict(TimingConflictSeverity.Info,
                    $"同步组链累积时序偏差估算 {cum:F2}ms 超过工艺阈值 {thresholdMs:F2}ms，建议调整同步组划分",
                    new List<int>()));
        }
        return conflicts;
    }

    /// <summary>解析时序标记：支持 T+5ms / T+5 / +5 / -2 / 5 / T-2ms 等形式，单位 ms。</summary>
    public static bool TryParseTimingMark(string raw, out double relativeMs)
    {
        relativeMs = 0;
        var s = (raw ?? "").Trim();
        if (s.Length == 0) return false;
        s = s.Replace("T", "", StringComparison.OrdinalIgnoreCase)
             .Replace("ms", "", StringComparison.OrdinalIgnoreCase)
             .Replace("m", "", StringComparison.OrdinalIgnoreCase)
             .Trim();
        if (s.Length == 0) return false;
        // 允许前导 +/-
        return double.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, out relativeMs);
    }

    /// <summary>单动作 PDO/执行器更新耗时估算（ms），用于同步组单周期可行性校验。</summary>
    private static double ActionDurationMs(string? action)
    {
        return action?.Trim() switch
        {
            "轴运动" or "轴" or "Axis" => 0.5,
            "IO" or "IO 输出" => 0.1,
            "气缸" => 0.3,
            "真空" => 0.2,
            "等待" or "延时" => 0.0,
            "点位" => 0.4,
            "modbus" => 0.6,
            "相机" => 0.8,
            _ => 0.3
        };
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
