// =====================================================================
// 节点图执行结果与调试器状态。
// NodeGraph 调试器：每个节点一次执行的 Status/DurationMs/ErrorText，
// 与整体 RunState（Idle/Running/Paused/Stepping/Completed/Error/Stopped）。
// UI 用 DataTrigger 绑定到 NgStepStatus 渲染色边框 / 浮标；调试工具栏绑 NgRunState。
// =====================================================================
using System;
using System.Collections.Generic;

namespace NoCodeMotion.Models.NodeGraph;

public enum NgStepStatus
{
    Idle,       // 未运行
    Running,    // 正在执行
    Done,       // 正常完成
    Error,      // 异常
    Paused,     // 暂停（断点命中或单步后）
    Skipped,    // 跳过（条件分支未选中 / 循环退出）
}

/// <summary>整体调试器状态。控制 6 按钮的可用性：Run/Step/Resume/Pause/Stop/断点。</summary>
public enum NgRunState
{
    Idle,       // 未运行
    Running,    // 运行中
    Paused,     // 已暂停（断点或单步后）
    Stepping,   // 单步执行中（一次只跑一个节点，跑完即停）
    Completed,  // 全部完成
    Error,      // 异常停止
    Stopped,    // 手动停止
}

/// <summary>单个节点一次执行的结果（POCO，由 NgRunner 写入）。</summary>
public sealed class NgStepResult
{
    public string NodeId { get; set; } = "";
    public NgStepStatus Status { get; set; } = NgStepStatus.Idle;
    public long DurationMs { get; set; }
    public string ErrorText { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }

    /// <summary>显示在节点卡片标题下方的状态图标（✓ ✗ ‖ 等）。XAML 优先用 DataTrigger 绑图标，这里是 fallback。</summary>
    public string StatusText => Status switch
    {
        NgStepStatus.Idle => string.Empty,
        NgStepStatus.Running => "运行中…",
        NgStepStatus.Done => "✓",
        NgStepStatus.Error => "✗",
        NgStepStatus.Paused => "‖",
        NgStepStatus.Skipped => "跳过",
        _ => string.Empty,
    };

    /// <summary>显示在节点卡片标题下方的耗时文字（"123 ms"）。Idle 状态显示空。</summary>
    public string DurationText => Status == NgStepStatus.Idle ? string.Empty : $"{DurationMs} ms";
}

/// <summary>整个流程一次运行的结果汇总（节点 id → 结果；当前节点 id；整体状态；最近一次异常）。</summary>
public sealed class NgRunReport
{
    public Dictionary<string, NgStepResult> Results { get; } = new(StringComparer.Ordinal);
    public NgRunState State { get; set; } = NgRunState.Idle;
    public string? CurrentNodeId { get; set; }
    public string LastError { get; set; } = string.Empty;

    public void Reset()
    {
        Results.Clear();
        State = NgRunState.Idle;
        CurrentNodeId = null;
        LastError = string.Empty;
    }
}
