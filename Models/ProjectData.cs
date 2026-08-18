using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace NoCodeMotion.Models
{
    /// <summary>整个工程的配置数据：所有页面的列表都汇总在这里，作为唯一数据源（单一真实来源）。</summary>
    public class ProjectData
    {
        public ObservableCollection<AxisItem> Axes { get; } = new();
        public ObservableCollection<CylinderItem> Cylinders { get; } = new();
        public ObservableCollection<CommItem> Comms { get; } = new();
        public ObservableCollection<TrayItem> Trays { get; } = new();
        public ObservableCollection<FlowItem> Flows { get; } = new();

        /// <summary>输入 IO 点位（左侧输入IO面板）</summary>
        public ObservableCollection<IoItem> Inputs { get; } = new();

        /// <summary>输出 IO 点位（右侧输出IO面板）</summary>
        public ObservableCollection<IoItem> Outputs { get; } = new();

        /// <summary>变量列表（流程/逻辑中可引用的计算与状态变量）</summary>
        public ObservableCollection<VariableItem> Variables { get; } = new();

        // === 兼容旧的 JSON 文件（保留一个旧字段 "Io"，反序列化时把内容迁移到 Inputs/Outputs） ===
        [JsonIgnore]
        public ObservableCollection<IoItem> Io
        {
            get => Inputs;
        }
    }
}
