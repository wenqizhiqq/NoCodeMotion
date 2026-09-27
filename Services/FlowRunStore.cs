// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦樘▧▨▩░▒▓✦​⁣​
// ◆温⁣启‍志‏◆‍编‎写‌◇‏微⁣信⁣﹕‎1‎8‏7‏◆‎1​9​3‌6​◇‍1‎3‌9‏9‏　‏※‍保‎留‏所‍有​权‎利‌请‏勿‍删‌除‍◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦樘▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦樘▧▨▩░▒▓✦​⁣​
using System.Collections.Concurrent;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 流程运行时的线程安全共享态：后台流程线程（每条 Flow 一条 Thread，内部 while 循环）只往这里写，
    /// 绝不调用 Dispatcher / 触碰 UI；UI 线程上的 DispatcherTimer 周期性读取本 store 并把状态推到
    /// FlowItem.Status 等绑定属性。这样运行期的高频状态刷新与界面渲染彻底解耦，流程再快也不卡 UI。
    /// </summary>
    public static class FlowRunStore
    {
        private sealed class Entry
        {
            public FlowStatus Status = FlowStatus.Idle;
            public string Step = string.Empty;
            public int Cycle;
            // —— 进度（供各流程页面用定时器轮询刷新"行序号 / 当前节点"）——
            public int StepIndex = -1;          // 表格/视觉：当前步骤下标（-1 = 无）
            public int Line = 0;                // Lua：当前执行行（0 = 无）
            public string NodeId = string.Empty;// 节点图：当前节点 Id（空 = 无）
        }

        private static readonly ConcurrentDictionary<FlowItem, Entry> _map = new();

        public static void SetStatus(FlowItem flow, FlowStatus status)
        {
            if (flow == null) return;
            _map.GetOrAdd(flow, _ => new Entry()).Status = status;
        }

        public static void SetStep(FlowItem flow, string step)
        {
            if (flow == null) return;
            _map.GetOrAdd(flow, _ => new Entry()).Step = step ?? string.Empty;
        }

        public static void SetCycle(FlowItem flow, int cycle)
        {
            if (flow == null) return;
            _map.GetOrAdd(flow, _ => new Entry()).Cycle = cycle;
        }

        /// <summary>写进度（供各流程页面定时器轮询）：<paramref name="stepIndex"/> 表格/视觉当前步骤下标、
        /// <paramref name="line"/> Lua 当前行、<paramref name="nodeId"/> 节点图当前节点 Id。传 null / 负数即不改那一项。</summary>
        public static void SetProgress(FlowItem flow, int? stepIndex = null, int? line = null, string? nodeId = null)
        {
            if (flow == null) return;
            var e = _map.GetOrAdd(flow, _ => new Entry());
            if (stepIndex.HasValue) e.StepIndex = stepIndex.Value;
            if (line.HasValue) e.Line = line.Value;
            if (nodeId != null) e.NodeId = nodeId;
        }

        /// <summary>读进度快照（UI 定时器调用）：步骤下标 / Lua 行 / 节点图当前节点 Id。</summary>
        public static (int StepIndex, int Line, string NodeId) GetProgress(FlowItem flow)
        {
            if (flow != null && _map.TryGetValue(flow, out var e)) return (e.StepIndex, e.Line, e.NodeId);
            return (-1, 0, string.Empty);
        }

        // —— 节点图：节点 Id → 步骤结果（只读给页面定时器刷新节点卡片用）——
        private static readonly ConcurrentDictionary<FlowItem, System.Collections.Generic.Dictionary<string, Models.NodeGraph.NgStepResult>> _nodeResults = new();

        /// <summary>运行器把节点图的"每节点结果"快照放进共享态；节点图页定时器读它给节点卡片上色/显示耗时摘要。
        /// 传 null 表示本条流程本轮已结束（页面据此不再高亮"当前节点"，但保留最后一次结果便于查看）。</summary>
        public static void SetNodeResults(FlowItem flow, System.Collections.Generic.Dictionary<string, Models.NodeGraph.NgStepResult>? results)
        {
            if (flow == null) return;
            if (results == null) _nodeResults.TryRemove(flow, out _);
            else _nodeResults[flow] = results;
        }

        /// <summary>读节点图的每节点结果（无则 null）。UI 定时器调用，读到的对象是运行器持有的引用，
        /// 仅用于展示（耗时/摘要/状态色），不修改。</summary>
        public static System.Collections.Generic.Dictionary<string, Models.NodeGraph.NgStepResult>? GetNodeResults(FlowItem flow)
            => flow != null && _nodeResults.TryGetValue(flow, out var r) ? r : null;

        /// <summary>读取某流程当前快照；若不存在则返回默认的 就绪/空/0。</summary>
        public static (FlowStatus Status, string Step, int Cycle) Get(FlowItem flow)
        {
            if (flow == null) return (FlowStatus.Idle, string.Empty, 0);
            if (_map.TryGetValue(flow, out var e)) return (e.Status, e.Step, e.Cycle);
            return (FlowStatus.Idle, string.Empty, 0);
        }

        /// <summary>仅当该流程有运行记录时返回 true（用于定时器判断是否需要推送状态）。</summary>
        public static bool Contains(FlowItem flow) => flow != null && _map.ContainsKey(flow);

        /// <summary>
        /// 把共享运行态推送到 FlowItem.Status（必须在 UI 线程调用；同值不触发 INPC，无抖动）。
        /// 操作员页 150ms 定时器、流程页/节点图页的 1s 定时器都会调用它 —— 这样**无论当前停在哪个页面**，
        /// 左侧流程列表的状态芯片都能实时反映「循环 / 运行 / 暂停 / 异常 / 停止」。
        /// 返回（正在运行含循环的流程数，其中循环运行的流程数）。
        /// </summary>
        public static (int Running, int Looping) PushStatuses()
        {
            var flows = ProjectStore.Data?.Flows;
            if (flows == null) return (0, 0);
            int running = 0, looping = 0;
            foreach (var f in flows)
            {
                if (!_map.TryGetValue(f, out var e)) continue;
                if (f.Status != e.Status) f.Status = e.Status;
                if (e.Status == FlowStatus.Running || e.Status == FlowStatus.Looping)
                {
                    running++;
                    if (e.Status == FlowStatus.Looping) looping++;
                }
            }
            return (running, looping);
        }

        public static void Clear(FlowItem flow)
        {
            if (flow != null) _map.TryRemove(flow, out _);
        }

        public static void ClearAll() => _map.Clear();
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦樘▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥樦樘▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥樦樘▧▨▩░▒▓✦​⁣​
