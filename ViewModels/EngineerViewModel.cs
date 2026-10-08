// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‌启​志​◆​编‏写⁠◇​微⁠信‏﹕‍1⁠8⁠7⁠◆⁣1‌9‌3‍6‎◇‌1‏3⁣9‏9⁠　‌※‎保‌留‌所⁠有‏权⁣利‏请‏勿⁣删‌除‌◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
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

        // ===== JOG 按住连续运动状态 =====
        private int _jogAxis = -1;
        private int _jogDir;
        private DispatcherTimer? _jogTimer;
        private string _jogHint = string.Empty;

        /// <summary>JOG 连续运动提示（空表示空闲）。</summary>
        public string JogHint
        {
            get => _jogHint;
            private set => SetField(ref _jogHint, value);
        }

        public ICommand EnableCommand { get; }
        public ICommand HomeCommand { get; }
        public ICommand InchCommand { get; }
        public ICommand JogStartCommand { get; }
        public ICommand JogStopCommand { get; }

        // ===== ② IO 控制 =====
        /// <summary>输入 IO（只读，展示当前状态值）。</summary>
        public ObservableCollection<IoItem> Inputs { get; }
        /// <summary>输出 IO（可开关，切换 Value 0/1）。</summary>
        public ObservableCollection<IoItem> Outputs { get; }
        public ICommand ToggleOutputCommand { get; }

        // 搜索过滤用**独立**视图：IO 页（IoViewModel）绑的是同一批 ProjectStore.Data.Inputs/Outputs，
        // 若在默认视图上加 Filter，会把 IO 页的表格一起过滤掉。
        private readonly CollectionViewSource _inputsCvs = new();
        private readonly CollectionViewSource _outputsCvs = new();
        private readonly CollectionViewSource _cylindersCvs = new();

        /// <summary>输入 IO 的过滤视图（供列表绑定；关键字为空时等于全量）。</summary>
        public ICollectionView InputsView => _inputsCvs.View;
        /// <summary>输出 IO 的过滤视图（供列表绑定）。</summary>
        public ICollectionView OutputsView => _outputsCvs.View;
        /// <summary>气缸列表的过滤视图（供列表绑定；关键字为空时等于全量）。</summary>
        public ICollectionView CylindersView => _cylindersCvs.View;

        private string _inputSearch = string.Empty;
        /// <summary>输入 IO 搜索关键字：匹配 名称 / 功能 / 卡类 / 套码 / 控制器 / 卡号 / 模块。</summary>
        public string InputSearch
        {
            get => _inputSearch;
            set
            {
                if (SetField(ref _inputSearch, value))
                {
                    InputsView.Refresh();
                    OnPropertyChanged(nameof(InputMatchInfo));
                }
            }
        }

        private string _outputSearch = string.Empty;
        /// <summary>输出 IO 搜索关键字：匹配 名称 / 功能 / 卡类 / 套码 / 控制器 / 卡号 / 模块。</summary>
        public string OutputSearch
        {
            get => _outputSearch;
            set
            {
                if (SetField(ref _outputSearch, value))
                {
                    OutputsView.Refresh();
                    OnPropertyChanged(nameof(OutputMatchInfo));
                }
            }
        }

        /// <summary>输入 IO 命中情况（命中数 / 总数），显示在搜索框右侧。</summary>
        public string InputMatchInfo => $"{InputsView.Cast<object>().Count()} / {Inputs.Count}";
        /// <summary>输出 IO 命中情况（命中数 / 总数）。</summary>
        public string OutputMatchInfo => $"{OutputsView.Cast<object>().Count()} / {Outputs.Count}";

        private string _cylinderSearch = string.Empty;
        /// <summary>气缸搜索关键字：匹配 名称 / 设备编号 / 类型 / 动作 / 输出点 / 伸出感应 / 缩回感应 / 备注。</summary>
        public string CylinderSearch
        {
            get => _cylinderSearch;
            set
            {
                if (SetField(ref _cylinderSearch, value))
                {
                    CylindersView.Refresh();
                    OnPropertyChanged(nameof(CylinderMatchInfo));
                }
            }
        }

        /// <summary>气缸命中情况（命中数 / 总数），显示在搜索框右侧。</summary>
        public string CylinderMatchInfo => $"{CylindersView.Cast<object>().Count()} / {Cylinders.Count}";

        // ===== 搜索框右侧「取消」按钮：一键清空关键字、恢复全量 =====
        /// <summary>清空输入 IO 搜索关键字。</summary>
        public ICommand ClearInputSearchCommand { get; }
        /// <summary>清空输出 IO 搜索关键字。</summary>
        public ICommand ClearOutputSearchCommand { get; }
        /// <summary>清空气缸搜索关键字。</summary>
        public ICommand ClearCylinderSearchCommand { get; }

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
            JogStartCommand = new RelayCommand(JogStart);
            JogStopCommand = new RelayCommand(_ => JogStop());

            // ② IO 控制：直接引用全局数据，输入只读、输出可切
            Inputs = ProjectStore.Data.Inputs;
            Outputs = ProjectStore.Data.Outputs;
            ToggleOutputCommand = new RelayCommand(ToggleOutput);

            // 搜索过滤：独立视图 + 过滤谓词；集合增删时同步刷新命中数
            _inputsCvs.Source = Inputs;
            _inputsCvs.Filter += (_, e) => e.Accepted = MatchIo(e.Item as IoItem, InputSearch);
            _outputsCvs.Source = Outputs;
            _outputsCvs.Filter += (_, e) => e.Accepted = MatchIo(e.Item as IoItem, OutputSearch);
            Inputs.CollectionChanged += (_, _) => OnPropertyChanged(nameof(InputMatchInfo));
            Outputs.CollectionChanged += (_, _) => OnPropertyChanged(nameof(OutputMatchInfo));

            ClearInputSearchCommand = new RelayCommand(_ => InputSearch = string.Empty);
            ClearOutputSearchCommand = new RelayCommand(_ => OutputSearch = string.Empty);
            ClearCylinderSearchCommand = new RelayCommand(_ => CylinderSearch = string.Empty);

            // ③ 气缸控制：为每个气缸建一个运行态（初始全部缩回）
            Cylinders = new ObservableCollection<CylinderRuntime>();
            foreach (var c in ProjectStore.Data.Cylinders)
                Cylinders.Add(new CylinderRuntime(c, c.Action == "缩回"));
            ProjectStore.Data.Cylinders.CollectionChanged += OnCylindersChanged;
            ToggleCylinderCommand = new RelayCommand(ToggleCylinder);

            // 气缸搜索：与 IO 一样用独立视图（三个列表互不干扰），集合增删时同步命中数
            _cylindersCvs.Source = Cylinders;
            _cylindersCvs.Filter += (_, e) => e.Accepted = MatchCylinder(e.Item as CylinderRuntime, CylinderSearch);
            Cylinders.CollectionChanged += (_, _) => OnPropertyChanged(nameof(CylinderMatchInfo));

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

        // ===== JOG 按住连续运动（仿真：定时器按 JogStep 持续累加当前位置；松开发停止命令）=====
        private void JogStart(object? p)
        {
            if (p is not string s) return;
            var parts = s.Split(',');
            if (parts.Length < 2) return;
            if (!int.TryParse(parts[0], out int i) || !int.TryParse(parts[1], out int dir)) return;
            if (i < 0 || i >= AxisStates.Count) return;
            _jogAxis = i;
            _jogDir = dir;
            StartJogTimer();
            var name = string.IsNullOrWhiteSpace(AxisStates[i].AxisName) ? $"轴{i}" : AxisStates[i].AxisName;
            JogHint = $"● JOG {name} 连续运动中（{(dir > 0 ? "正向 +" : "反向 −")}），松开鼠标停止";
        }

        private void JogStop()
        {
            if (_jogAxis < 0) return;
            StopJogTimer();
            JogHint = "JOG 已停止";
        }

        private void StartJogTimer()
        {
            StopJogTimer();
            _jogTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
            _jogTimer.Tick += JogTick;
            _jogTimer.Start();
        }

        private void JogTick(object? _, EventArgs __)
        {
            if (_jogAxis < 0 || _jogAxis >= AxisStates.Count) return;
            var st = AxisStates[_jogAxis];
            st.CurrentPosition += _jogDir * st.JogStep;
        }

        private void StopJogTimer()
        {
            if (_jogTimer != null)
            {
                _jogTimer.Stop();
                _jogTimer.Tick -= JogTick;
                _jogTimer = null;
            }
            _jogAxis = -1;
            _jogDir = 0;
        }

        /// <summary>IO 搜索匹配：把名称 / 功能 / 卡类 / 套码 / 控制器 / 卡号 / 模块 拼成一条串做子串匹配（忽略大小写）。</summary>
        private static bool MatchIo(IoItem? item, string? keyword)
        {
            if (item is null) return false;
            if (string.IsNullOrWhiteSpace(keyword)) return true;
            string k = keyword.Trim();
            string hay = $"{item.Name} {item.Function} {item.CardType} {item.SuitCode} {item.Controller} 卡{item.CardNo} 模{item.ModuleNo}";
            return hay.Contains(k, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>气缸搜索匹配：名称 / 设备编号 / 类型 / 动作 / 输出点 / 伸出感应 / 缩回感应 / 备注（忽略大小写）。</summary>
        private static bool MatchCylinder(CylinderRuntime? rt, string? keyword)
        {
            if (rt is null) return false;
            if (string.IsNullOrWhiteSpace(keyword)) return true;
            string k = keyword.Trim();
            var it = rt.Item;
            string hay = $"{it.Name} {it.DeviceId} {it.Type} {it.Action} {it.OutPoint} {it.SensorExtend} {it.SensorRetract} {it.Remark}";
            return hay.Contains(k, StringComparison.OrdinalIgnoreCase);
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
            {
                var slot = item.Positions[i];
                if (slot.Position == null)
                {
                    StatusBarService.ReportInfo($"点位「{item.Name}」轴 {i + 1} 未填位置，已跳过（不移动）。");
                    continue;
                }
                AxisStates[i].CurrentPosition = slot.Position.Value;
            }
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
