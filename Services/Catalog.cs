using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 全局对象目录：各页面（轴/IO/气缸/通讯）把已配置的名称汇总到这里，
    /// 供流程页"名称"下拉框选择引用。所有集合均为 ObservableCollection，变化时自动刷新界面。
    /// </summary>
    public static class Catalog
    {
        public static ObservableCollection<string> AxisNames { get; } = new();
        public static ObservableCollection<string> IoNames { get; } = new();
        public static ObservableCollection<string> CylinderNames { get; } = new();
        public static ObservableCollection<string> CommNames { get; } = new();
        public static ObservableCollection<string> VariableNames { get; } = new();
        public static ObservableCollection<string> AllNames { get; } = new();

        public static void SetAxis(IEnumerable<string> names) => Set(AxisNames, names);
        public static void SetIo(IEnumerable<string> names) => Set(IoNames, names);
        public static void SetCylinder(IEnumerable<string> names) => Set(CylinderNames, names);
        public static void SetComm(IEnumerable<string> names) => Set(CommNames, names);
        public static void SetVariable(IEnumerable<string> names) => Set(VariableNames, names);

        private static void Set(ObservableCollection<string> target, IEnumerable<string> names)
        {
            target.Clear();
            foreach (var n in names.Where(x => !string.IsNullOrWhiteSpace(x)))
                target.Add(n);

            RebuildAll();
        }

        private static void RebuildAll()
        {
            AllNames.Clear();
            foreach (var n in AxisNames) if (!AllNames.Contains(n)) AllNames.Add(n);
            foreach (var n in IoNames) if (!AllNames.Contains(n)) AllNames.Add(n);
            foreach (var n in CylinderNames) if (!AllNames.Contains(n)) AllNames.Add(n);
            foreach (var n in CommNames) if (!AllNames.Contains(n)) AllNames.Add(n);
            foreach (var n in VariableNames) if (!AllNames.Contains(n)) AllNames.Add(n);
        }

        /// <summary>从已载入的工程中重建名称库（用于启动后填充下拉选项）。</summary>
        public static void SyncAllFromData(ProjectData data)
        {
            SetAxis(data.Axes.Select(a => a.Name));
            // 输入 + 输出合并到 IO 名称库
            var ioNames = data.Inputs.Select(i => i.Name).Concat(data.Outputs.Select(i => i.Name));
            SetIo(ioNames);
            SetCylinder(data.Cylinders.Select(c => c.Name));
            SetComm(data.Comms.Select(c => c.Name));
            SetVariable(data.Variables.SelectMany(v => v.Names()));
        }
    }
}
