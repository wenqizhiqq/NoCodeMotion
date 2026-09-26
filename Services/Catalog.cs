// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁣启‎志‌◆⁠编‍写‏◇‎微‍信‏﹕‍1‏8⁣7‍◆⁠1‌9​3‌6⁣◇‍1‏3‎9‍9⁣　⁣※‍保⁠留⁠所‏有⁠权‏利⁠请⁠勿​删​除‍◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NoCodeMotion.Models;
using NoCodeMotion.Services.Hardware;
using NoCodeMotion.Services.Hardware.Cards;

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
        /// <summary>输入 IO 名称（供气缸「伸出/缩回感应」等需要方向的页面选真实点位）。</summary>
        public static ObservableCollection<string> InIoNames { get; } = new();
        /// <summary>输出 IO 名称（供气缸「输出点」等需要方向的页面选真实点位）。</summary>
        public static ObservableCollection<string> OutIoNames { get; } = new();
        public static ObservableCollection<string> CylinderNames { get; } = new();
        public static ObservableCollection<string> CameraNames { get; } = new();
        public static ObservableCollection<string> CommNames { get; } = new();
        public static ObservableCollection<string> VariableNames { get; } = new();
        public static ObservableCollection<string> AllNames { get; } = new();
        public static ObservableCollection<string> PointNames { get; } = new();
        public static ObservableCollection<string> ControllerNames { get; } = new();
        public static ObservableCollection<string> VendorNames { get; } = new();
        public static ObservableCollection<string> BusTypeNames { get; } = new();

        public static void SetAxis(IEnumerable<string> names) => Set(AxisNames, names);
        public static void SetIo(IEnumerable<string> names) => Set(IoNames, names);
        public static void SetIoIn(IEnumerable<string> names) => Set(InIoNames, names);
        public static void SetIoOut(IEnumerable<string> names) => Set(OutIoNames, names);
        public static void SetCylinder(IEnumerable<string> names) => Set(CylinderNames, names);
        public static void SetCamera(IEnumerable<string> names) => Set(CameraNames, names);
        public static void SetComm(IEnumerable<string> names) => Set(CommNames, names);
        public static void SetVariable(IEnumerable<string> names) => Set(VariableNames, names);
        public static void SetPoint(IEnumerable<string> names) => Set(PointNames, names);
        public static void SetController(IEnumerable<string> names) => Set(ControllerNames, names);
        public static void SetVendor(IEnumerable<string> names) => Set(VendorNames, names);
        public static void SetBusType(IEnumerable<string> names) => Set(BusTypeNames, names);

        /// <summary>从主流运动控制卡厂商登记表刷新「品牌 / 总线类型」下拉（脉冲 + 总线全覆盖）。</summary>
        public static void RefreshControllerStandards()
        {
            SetVendor(CardVendorRegistry.Vendors.Select(v => v.Vendor));
            var buses = new List<string>();
            foreach (var v in CardVendorRegistry.Vendors)
                foreach (var b in v.BusTypes)
                {
                    string n = CardVendorRegistry.BusTypeName(b);
                    if (!buses.Contains(n)) buses.Add(n);
                }
            // 已移植卡族声明的总线类型也要并进来：自动识别登记控制器时是按卡族写 BusType 的，
            // 候选里没有这个值，单元格就会渲染成空白（下拉只认候选内的值）。
            foreach (var f in CardFamilyCatalog.Families)
                foreach (var b in f.BusTypes)
                {
                    string n = CardVendorRegistry.BusTypeName(b);
                    if (!buses.Contains(n)) buses.Add(n);
                }
            SetBusType(buses);
        }

        private static void Set(ObservableCollection<string> target, IEnumerable<string> names)
        {
            // ★ 可能由后台线程触发（流程 / 节点图写变量 → SimRuntime.WriteVarRow → VariableRow 变更 → 本方法）。
            // ObservableCollection 只允许在其所属（UI）线程改，否则 WPF 抛
            // 「该类型的 CollectionView 不支持从调度程序线程以外的线程对其 SourceCollection 进行的更改」。
            // 先物化候选，再在需要时封送到 UI 线程。
            var list = names.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            var app = System.Windows.Application.Current;
            if (app?.Dispatcher != null && !app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.BeginInvoke(new System.Action(() => Apply(target, list)));
                return;
            }
            Apply(target, list);
        }

        private static void Apply(ObservableCollection<string> target, List<string> names)
        {
            // 差量更新：候选内容没变化就不动集合。Clear() 会触发 CollectionChanged(Reset)，
            // 正被下拉引用的 ComboBox 会闪空/丢当前显示（流程页运行中曾因此「名称」列变空白）。
            bool same = target.Count == names.Count;
            if (same)
            {
                for (int i = 0; i < names.Count; i++)
                    if (!string.Equals(target[i], names[i], StringComparison.Ordinal)) { same = false; break; }
            }
            if (same) return;

            target.Clear();
            foreach (var n in names) target.Add(n);
            RebuildAll();
        }

        private static void RebuildAll()
        {
            AllNames.Clear();
            foreach (var n in AxisNames) if (!AllNames.Contains(n)) AllNames.Add(n);
            foreach (var n in IoNames) if (!AllNames.Contains(n)) AllNames.Add(n);
            foreach (var n in CylinderNames) if (!AllNames.Contains(n)) AllNames.Add(n);
            foreach (var n in CameraNames) if (!AllNames.Contains(n)) AllNames.Add(n);
            foreach (var n in CommNames) if (!AllNames.Contains(n)) AllNames.Add(n);
            foreach (var n in VariableNames) if (!AllNames.Contains(n)) AllNames.Add(n);
            foreach (var n in PointNames) if (!AllNames.Contains(n)) AllNames.Add(n);
            foreach (var n in ControllerNames) if (!AllNames.Contains(n)) AllNames.Add(n);
        }

        /// <summary>从已载入的工程中重建名称库（用于启动后填充下拉选项）。</summary>
        public static void SyncAllFromData(ProjectData data)
        {
            SetAxis(data.Axes.Select(a => a.Name));
            // 输入 / 输出分开登记（气缸等按方向选点），另合并一份给流程页通用下拉
            var inNames = data.Inputs.Select(i => i.Name).ToList();
            var outNames = data.Outputs.Select(i => i.Name).ToList();
            SetIoIn(inNames);
            SetIoOut(outNames);
            SetIo(inNames.Concat(outNames));
            SetCylinder(data.Cylinders.Select(c => c.Name));
            SetCamera(data.Cameras.Select(c => c.Name));
            SetComm(data.Comms.Select(c => c.Name));
            SetVariable(data.Variables.SelectMany(v => v.Names()));
            // 点位名称来自所有点位表（工位）下的全部点位行
            SetPoint(data.PointTables.SelectMany(t => t.Points).Select(p => p.Name));
            SetController(data.Controllers.Select(c => c.Name));
            // 刷新控制器页「品牌 / 总线类型」下拉（主流脉冲卡 + 总线主站厂商）
            RefreshControllerStandards();
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
