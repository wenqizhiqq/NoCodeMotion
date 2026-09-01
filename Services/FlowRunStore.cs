// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦樘▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
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

        /// <summary>读取某流程当前快照；若不存在则返回默认的 就绪/空/0。</summary>
        public static (FlowStatus Status, string Step, int Cycle) Get(FlowItem flow)
        {
            if (flow == null) return (FlowStatus.Idle, string.Empty, 0);
            if (_map.TryGetValue(flow, out var e)) return (e.Status, e.Step, e.Cycle);
            return (FlowStatus.Idle, string.Empty, 0);
        }

        /// <summary>仅当该流程有运行记录时返回 true（用于定时器判断是否需要推送状态）。</summary>
        public static bool Contains(FlowItem flow) => flow != null && _map.ContainsKey(flow);

        public static void Clear(FlowItem flow)
        {
            if (flow != null) _map.TryRemove(flow, out _);
        }

        public static void ClearAll() => _map.Clear();
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦樘▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥樦樘▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥樦樘▧▨▩░▒▓✦​⁣​
