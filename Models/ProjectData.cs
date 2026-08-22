using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace NoCodeMotion.Models
{
    /// <summary>整个工程的配置数据：所有页面的列表都汇总在这里，作为唯一数据源（单一真实来源）。</summary>
    public class ProjectData
    {
        public ObservableCollection<AxisItem> Axes { get; set; } = new();

        /// <summary>轴控制器列表：每块运动控制卡/控制器实例，供轴页面选择归属。</summary>
        public ObservableCollection<AxisControllerItem> Controllers { get; set; } = new();
        public ObservableCollection<CylinderItem> Cylinders { get; set; } = new();
        public ObservableCollection<CommItem> Comms { get; set; } = new();
        public ObservableCollection<TrayItem> Trays { get; set; } = new();
        public ObservableCollection<FlowItem> Flows { get; set; } = new();

        /// <summary>点位表列表：一个点位表 = 一个工位，含该工位的 4 个轴与全部点位行。</summary>
        public ObservableCollection<PointTable> PointTables { get; set; } = new();

        /// <summary>【旧字段，仅用于兼容早期工程】单一点位表的点位行，载入后会迁移到 PointTables。</summary>
        public ObservableCollection<PointItem> Points { get; set; } = new();

        /// <summary>【旧字段，仅用于兼容早期工程】单一点位表所选的 4 个轴，载入后会迁移到 PointTables。</summary>
        public ObservableCollection<string> PointAxes { get; set; } = new();

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

        /// <summary>
        /// 载入工程后调用：把旧的单一点位表（Points / PointAxes）迁移为「工位1」，
        /// 并保证工程中至少存在一个点位表，同时补齐每个点位表的 4 个轴槽。
        /// </summary>
        public void EnsurePointTables()
        {
            if (PointTables.Count == 0)
            {
                var table = new PointTable { Name = "工位1" };
                for (int i = 0; i < PointTable.SlotCount && i < PointAxes.Count; i++)
                    table.AxisNames[i] = PointAxes[i];
                foreach (var p in Points)
                    table.Points.Add(p);
                PointTables.Add(table);
            }

            foreach (var t in PointTables)
            {
                t.EnsureAxisSlots();
                foreach (var p in t.Points) p.EnsureSlots();
            }

            // 旧字段已迁移完毕，清空避免下次载入重复迁移
            Points.Clear();
            PointAxes.Clear();
        }
    }
}
