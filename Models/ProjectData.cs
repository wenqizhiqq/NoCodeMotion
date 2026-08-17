using System.Collections.ObjectModel;

namespace NoCodeMotion.Models
{
    /// <summary>整个工程的配置数据：所有页面的列表都汇总在这里，作为唯一数据源（单一真实来源）。</summary>
    public class ProjectData
    {
        public ObservableCollection<AxisItem> Axes { get; } = new();
        public ObservableCollection<IoItem> Io { get; } = new();
        public ObservableCollection<CylinderItem> Cylinders { get; } = new();
        public ObservableCollection<CommItem> Comms { get; } = new();
        public ObservableCollection<TrayItem> Trays { get; } = new();
        public ObservableCollection<FlowItem> Flows { get; } = new();
    }
}
