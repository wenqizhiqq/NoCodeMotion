// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Views;

namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 工程师页 ViewModel：把现场调试常用的四类控制集中到一个页面，方便工程师在设备上直接操作。
    /// ① 轴控制（4 个轴槽的使能/回原/寸动/JOG）；② IO 控制（输入只读 + 输出开关）；
    /// ③ 气缸控制（每个气缸伸出/缩回）；④ 点位移动和设置（选工位 → 逐行移动/保存点位）。
    /// 「轴控制」的 4 个轴槽仅是调试面板的瞬时控制对象（不落盘、不联动工位）；
    /// 「点位移动和设置」的列头由 <see cref="PointAxisStates"/> 单独承载，从当前工位的
    /// <see cref="PointTable.AxisNames"/> 加载，与顶部下拉框完全解耦，保证点位表轴列固定。
    /// </summary>
    public class EngineerViewModel : ViewModelBase, IEnsureDefaultSelection
    {
        // ===== ① 轴控制 =====
        /// <summary>共 4 个轴槽的运行态（轴名/使能/回原/当前位置）。仅调试用，不与 PointTable.AxisNames 联动。</summary>
        public ObservableCollection<EngineerAxisState> AxisStates { get; } = new();

        /// <summary>点位表 4 个轴槽的列头（与 AxisStates 解耦），按当前工位的 PointTable.AxisNames 加载。</summary>
        public ObservableCollection<EngineerAxisState> PointAxisStates { get; } = new();

        public ICommand EnableCommand { get; }
        public ICommand HomeCommand { get; }
        public ICommand InchCommand { get; }
        public ICommand JogCommand { get; }

        // ===== ② IO 控制 =====
        /// <summary>输入 IO（只读，展示当前状态值）。</summary>
        public ObservableCollection<IoItem> Inputs { get; }
        /// <summary>输出 IO（可开关，切换 Value 0/1）。</summary>
        public ObservableCollection<IoItem> Outputs { get; }
        public ICommand ToggleOutputCommand { get; }

        // ===== ③ 气缸控制 =====
        /// <summary>气缸运行态集合（每个气缸一个，记录伸出/缩回）。</summary>
        public ObservableCollection<CylinderRuntime> Cylinders { get; }
        public ICommand ToggleCylinderCommand { get; }

        // ===== ④ 点位移动和设置 =====
        /// <summary>工位（点位表）列表，供下拉选择。</summary>
        public ObservableCollection<PointTable> Tables { get; }

        private PointTable? _selectedTable;
        /// <summary>当前选中的工位；选中后下方表格展示该工位的点位行。</summary>
        public PointTable? SelectedTable
        {
            get => _selectedTable;
            set
            {
                if (SetField(ref _selectedTable, value))
                    OnPropertyChanged(nameof(CurrentPoints));
            }
        }

        /// <summary>当前工位的点位行集合，供表格绑定；未选工位时为 null。</summary>
        public ObservableCollection<PointItem>? CurrentPoints => SelectedTable?.Points;

        public ICommand MoveToPointCommand { get; }
        public ICommand SaveCurrentCommand { get; }

        public EngineerViewModel()
        {
            // ① 轴控制：4 个轴槽（瞬时调试对象，不与点位表列头联动），默认无选中轴
            for (int i = 0; i < 4; i++)
                AxisStates.Add(new EngineerAxisState(i));
            EnableCommand = new RelayCommand(ToggleEnable);
            HomeCommand = new RelayCommand(Home);
            InchCommand = new RelayCommand(p => Move(p, false));
            JogCommand = new RelayCommand(p => Move(p, true));

            // ② IO 控制：直接引用全局数据，输入只读、输出可切
            Inputs = ProjectStore.Data.Inputs;
            Outputs = ProjectStore.Data.Outputs;
            ToggleOutputCommand = new RelayCommand(ToggleOutput);

            // ③ 气缸控制：为每个气缸建一个运行态（初始全部缩回）
            Cylinders = new ObservableCollection<CylinderRuntime>();
            foreach (var c in ProjectStore.Data.Cylinders)
                Cylinders.Add(new CylinderRuntime(c, c.Action == "缩回"));
            ProjectStore.Data.Cylinders.CollectionChanged += OnCylindersChanged;
            ToggleCylinderCommand = new RelayCommand(ToggleCylinder);

            // ④ 点位移动和设置（列头由 PointAxisStates 单独承载，从 SelectedTable.AxisNames 加载）
            Tables = ProjectStore.Data.PointTables;
            for (int i = 0; i < PointTable.SlotCount; i++)
                PointAxisStates.Add(new EngineerAxisState(i));
            PropertyChanged += OnSelfPropertyChanged;
            MoveToPointCommand = new RelayCommand(MoveToPoint);
            SaveCurrentCommand = new RelayCommand(SaveCurrent);

            EnsureDefaultSelection();
        }

        private void OnCylindersChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (CylinderItem old in e.OldItems)
                {
                    var rt = Cylinders.FirstOrDefault(r => ReferenceEquals(r.Item, old));
                    if (rt != null) Cylinders.Remove(rt);
                }
            if (e.NewItems != null)
                foreach (CylinderItem neo in e.NewItems)
                    Cylinders.Add(new CylinderRuntime(neo, neo.Action == "缩回"));
        }

        // ===== ① 轴控制命令（运行态仿真：当前无运动硬件层，命令在此更新运行态，后续接运动引擎）=====

        private void ToggleEnable(object? p)
        {
            int i = (int)p!;
            AxisStates[i].Enabled = !AxisStates[i].Enabled;
        }

        private void Home(object? p)
        {
            int i = (int)p!;
            AxisStates[i].CurrentPosition = 0;
            AxisStates[i].Homed = true;
        }

        private void Move(object? p, bool jog)
        {
            var parts = ((string)p!).Split(',');
            int i = int.Parse(parts[0]);
            int dir = int.Parse(parts[1]);
            double step = jog ? AxisStates[i].JogStep : AxisStates[i].InchStep;
            AxisStates[i].CurrentPosition += dir * step;
        }

        // ===== ② IO 输出切换 =====

        private void ToggleOutput(object? p)
        {
            if (p is not IoItem item) return;
            item.Value = item.Value == 0 ? 1 : 0;
        }

        // ===== ③ 气缸伸出/缩回 =====

        private void ToggleCylinder(object? p)
        {
            if (p is CylinderRuntime rt) rt.Extended = !rt.Extended;
        }

        // ===== ④ 点位行移动/保存（运行态仿真，弹窗确认后执行）=====

        /// <summary>移动：确认后把 4 个轴移动到该行点位记录的目标位置（仿真：直接写入轴当前位置）。</summary>
        private void MoveToPoint(object? p)
        {
            if (p is not PointItem item) return;
            var dlg = new ConfirmDialog(
                "移动确认",
                $"是否将 4 个轴移动到点位「{item.Name}」记录的目标位置？",
                "移动");
            if (dlg.ShowDialog() != true) return;
            for (int i = 0; i < AxisStates.Count && i < item.Positions.Count; i++)
                AxisStates[i].CurrentPosition = item.Positions[i].Position;
        }

        /// <summary>保存：确认后把 4 个轴的当前位置写回该行点位的单元（触发自动保存落盘）。</summary>
        private void SaveCurrent(object? p)
        {
            if (p is not PointItem item) return;
            var dlg = new ConfirmDialog(
                "保存确认",
                $"是否将 4 个轴当前位置保存到点位「{item.Name}」？",
                "保存");
            if (dlg.ShowDialog() != true) return;
            for (int i = 0; i < AxisStates.Count && i < item.Positions.Count; i++)
                item.Positions[i].Position = AxisStates[i].CurrentPosition;
        }

        public void EnsureDefaultSelection()
        {
            if (SelectedTable == null && Tables.Count > 0)
                SelectedTable = Tables[0];
        }

        // ===== ④ 联动：SelectedTable 变化 → PointAxisStates 从工位的 AxisNames 加载 =====

        private PointTable? _observedTable;

        private void OnSelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(SelectedTable)) return;
            if (_observedTable != null) _observedTable.PropertyChanged -= OnObservedTableChanged;
            _observedTable = SelectedTable;
            if (_observedTable != null) _observedTable.PropertyChanged += OnObservedTableChanged;
            LoadPointAxisNames();
        }

        private void OnObservedTableChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 工位的 AxisNames 被其它入口（如 PointPage）改动时，也即时刷新列头
            if (e.PropertyName == nameof(PointTable.AxisNames))
                LoadPointAxisNames();
        }

        private void LoadPointAxisNames()
        {
            var table = _observedTable;
            for (int i = 0; i < PointAxisStates.Count; i++)
                PointAxisStates[i].AxisName =
                    table != null && i < table.AxisNames.Count ? table.AxisNames[i] : string.Empty;
        }
    }

    /// <summary>工程师页中单个轴槽的运行态（与 PointViewModel.AxisState 字段一致，供 AxisCardTemplate 绑定）。</summary>
    public class EngineerAxisState : ViewModelBase
    {
        public int Index { get; }

        private string _axisName = string.Empty;
        private bool _enabled;
        private bool _homed;
        private double _current;
        private double _inchStep = 1.0;
        private double _jogStep = 10.0;

        public EngineerAxisState(int index) => Index = index;

        /// <summary>该槽位选择的轴名（来自 Catalog.AxisNames）。</summary>
        public string AxisName
        {
            get => _axisName;
            set => SetField(ref _axisName, value);
        }

        public bool Enabled
        {
            get => _enabled;
            set => SetField(ref _enabled, value);
        }

        public bool Homed
        {
            get => _homed;
            set => SetField(ref _homed, value);
        }

        /// <summary>轴的当前位置（运行态显示，仿真用）。</summary>
        public double CurrentPosition
        {
            get => _current;
            set => SetField(ref _current, value);
        }

        /// <summary>该轴的寸动距离（每次寸动移动的单位）。</summary>
        public double InchStep
        {
            get => _inchStep;
            set => SetField(ref _inchStep, value);
        }

        /// <summary>该轴的 JOG 速度（每次 JOG 移动的单位，通常大于寸动）。</summary>
        public double JogStep
        {
            get => _jogStep;
            set => SetField(ref _jogStep, value);
        }
    }

    /// <summary>气缸运行态：包裹一个 CylinderItem 并记录其伸出/缩回（运行态，不落盘）。</summary>
    public class CylinderRuntime : ViewModelBase
    {
        public CylinderItem Item { get; }

        private bool _extended;

        /// <summary>是否已伸出；true=伸出，false=缩回。</summary>
        public bool Extended
        {
            get => _extended;
            set => SetField(ref _extended, value);
        }

        public CylinderRuntime(CylinderItem item, bool extended)
        {
            Item = item;
            _extended = extended;
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
