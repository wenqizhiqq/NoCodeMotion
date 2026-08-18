using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace NoCodeMotion.Models
{
    /// <summary>整个工程的配置数据：所有页面的列表都汇总在这里，作为唯一数据源（单一真实来源）。</summary>
    public class ProjectData
    {
        public ObservableCollection<AxisItem> Axes { get; set; } = new();
        public ObservableCollection<CylinderItem> Cylinders { get; set; } = new();
        public ObservableCollection<CommItem> Comms { get; set; } = new();
        public ObservableCollection<TrayItem> Trays { get; set; } = new();
        public ObservableCollection<FlowItem> Flows { get; set; } = new();

        /// <summary>轴点位表（流程可引用的命名位置，含各轴目标坐标）。</summary>
        public ObservableCollection<PointItem> Points { get; set; } = new();

        /// <summary>点位表页所选的 4 个轴（按槽位 0..3），持久化以便表头显示轴名。</summary>
        public ObservableCollection<string> PointAxes { get; set; } = new() { "", "", "", "" };

        /// <summary>输入 IO 点位（左侧输入IO面板）</summary>
        public ObservableCollection<IoItem> Inputs { get; set; } = new();

        /// <summary>输出 IO 点位（右侧输出IO面板）</summary>
        public ObservableCollection<IoItem> Outputs { get; set; } = new();

        /// <summary>变量表（流程/逻辑中可引用的计算与状态变量），每行含 5 个 (名称/字符串值)。</summary>
        public ObservableCollection<VariableRow> Variables { get; set; } = new();

        // === 兼容旧的 JSON 文件（保留一个旧字段 "Io"，反序列化时把内容迁移到 Inputs/Outputs） ===
        [JsonIgnore]
        public ObservableCollection<IoItem> Io
        {
            get => Inputs;
        }
    }
}
