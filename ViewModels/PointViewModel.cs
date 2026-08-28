// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
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
    /// 点位表页 ViewModel：左侧维护多个点位表（一个点位表 = 一个工位，可新增/删除/切换）；
    /// 右侧针对当前选中的工位，选择 4 个轴、维护该工位的点位行（名称 + 4 轴位置/速度），
    /// 并提供轴的使能/回原/寸动/JOG 控制。
    /// </summary>
    public class PointViewModel : ListEditorViewModel<PointTable>, IEnsureDefaultSelection
    {
        /// <summary>4 个轴槽的运行/配置状态。轴名属于当前工位并持久化；使能/回原/当前位置为运行态（不落盘）。</summary>
        public ObservableCollection<AxisState> AxisStates { get; } = new();

        private bool _loadingAxisNames;
        private PointItem? _selectedPoint;
        private double _inchStep = 1.0;
        private double _jogStep = 10.0;

        /// <summary>寸动步长（每次寸动移动的单位）。</summary>
        public double InchStep
        {
            get => _inchStep;
            set => SetField(ref _inchStep, value);
        }

        /// <summary>JOG 步长（每次 JOG 移动的单位，通常大于寸动）。</summary>
        public double JogStep
        {
            get => _jogStep;
            set => SetField(ref _jogStep, value);
        }

        /// <summary>当前工位下的点位行集合，供右侧表格绑定；未选工位时为 null。</summary>
        public ObservableCollection<PointItem>? CurrentPoints => SelectedItem?.Points;

        // ---------- 时序编译（对应专利「方向二」：同步组 / 时序标记 编译期冲突检测）----------
        /// <summary>最近一次「编译时序」的问题列表（空表示通过）。</summary>
        public ObservableCollection<TimingIssue> CompileIssues { get; } = new();

        private bool _hasCompiled;
        private bool _hasCompileIssues;
        private int _errorCount;
        private int _warningCount;
        private string _compileSummary = string.Empty;

        /// <summary>是否已执行过编译（控制结果面板可见性）。</summary>
        public bool HasCompiled
        {
            get => _hasCompiled;
            private set => SetField(ref _hasCompiled, value);
        }

        /// <summary>本次编译是否存在错误/警告。</summary>
        public bool HasCompileIssues
        {
            get => _hasCompileIssues;
            private set => SetField(ref _hasCompileIssues, value);
        }

        public int ErrorCount
        {
            get => _errorCount;
            private set => SetField(ref _errorCount, value);
        }

        public int WarningCount
        {
            get => _warningCount;
            private set => SetField(ref _warningCount, value);
        }

        /// <summary>编译结果摘要文本（通过 / N 错误 M 警告）。</summary>
        public string CompileSummary
        {
            get => _compileSummary;
            private set => SetField(ref _compileSummary, value);
        }

        /// <summary>右侧表格当前选中的点位行（删除点位时使用）。</summary>
        public PointItem? SelectedPoint
        {
            get => _selectedPoint;
            set
            {
                if (SetField(ref _selectedPoint, value))
                    OnPropertyChanged(nameof(CanDeletePoint));
            }
        }

        public bool CanDeletePoint => SelectedPoint != null;

        /// <summary>是否已选中一个工位（未选中时右侧显示空状态提示）。</summary>
        public bool HasTable => SelectedItem != null;

        public PointViewModel()
        {
            Items = ProjectStore.Data.PointTables;

            // 4 个轴槽（轴名随当前工位切换而重新载入）
            for (int i = 0; i < PointTable.SlotCount; i++)
            {
                var st = new AxisState(i);
                st.PropertyChanged += OnAxisStateChanged;
                AxisStates.Add(st);
            }

            EnableCommand = new RelayCommand(ToggleEnable);
            HomeCommand = new RelayCommand(Home);
            InchCommand = new RelayCommand(p => Move(p, false));
            JogCommand = new RelayCommand(p => Move(p, true));
            MoveToPointCommand = new RelayCommand(MoveToPoint);
            SaveCurrentCommand = new RelayCommand(SaveCurrent);
            AddPointCommand = new RelayCommand(_ => AddPoint(), _ => SelectedItem != null);
            DeletePointCommand = new RelayCommand(_ => DeletePoint(), _ => CanDeletePoint);
            CompileCommand = new RelayCommand(_ => Compile(), _ => SelectedItem != null);

            // 工位切换 → 重新载入 4 个轴名、刷新右侧表格
            PropertyChanged += OnSelfPropertyChanged;

            // 工位增删 / 工位内点位名变化 → 重新汇总点位名称库
            Items.CollectionChanged += OnTablesCollectionChanged;
            foreach (var t in Items) t.PropertyChanged += OnTableChanged;

            AttachAutoSave();
            EnsureDefaultSelection();
        }

        // ===== 工位（点位表）级操作 =====

        protected override PointTable CreateNewItem()
        {
            var table = new PointTable { Name = UniqueName("工位", Items.Select(t => t.Name)) };
            // 新工位默认带一行点位，便于立即录入
            table.Points.Add(new PointItem { Name = "点位1" });
            return table;
        }

        /// <summary>删除工位前弹窗确认，避免误删整张点位表。</summary>
        protected override void Delete()
        {
            if (SelectedItem is not PointTable table) return;
            var dlg = new ConfirmDialog(
                "删除确认",
                $"是否删除点位表「{table.Name}」？该工位下的 {table.Points.Count} 个点位会一并删除。",
                "删除");
            if (dlg.ShowDialog() != true) return;
            base.Delete();
        }

        /// <summary>点位名称库汇总所有工位下的点位行（而不是工位名）。</summary>
        protected override void SyncCatalog()
            => Catalog.SetPoint(Items.SelectMany(t => t.Points).Select(p => p.Name));

        private void OnTablesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (PointTable t in e.OldItems) t.PropertyChanged -= OnTableChanged;
            if (e.NewItems != null)
                foreach (PointTable t in e.NewItems) t.PropertyChanged += OnTableChanged;
        }

        private void OnTableChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PointTable.PointNamesSignature))
                SyncCatalog();
        }

        private void OnSelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(SelectedItem)) return;
            LoadAxisNamesFromTable();
            SelectedPoint = null;
            OnPropertyChanged(nameof(CurrentPoints));
            OnPropertyChanged(nameof(HasTable));
        }

        // 切换工位时把该工位保存的 4 个轴名填进轴槽（此期间不回写，避免污染其它工位）
        private void LoadAxisNamesFromTable()
        {
            var table = SelectedItem;
            _loadingAxisNames = true;
            try
            {
                for (int i = 0; i < AxisStates.Count; i++)
                    AxisStates[i].AxisName =
                        table != null && i < table.AxisNames.Count ? table.AxisNames[i] : string.Empty;
            }
            finally
            {
                _loadingAxisNames = false;
            }
        }

        // 用户改选轴 → 写回当前工位并保存
        private void OnAxisStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_loadingAxisNames) return;
            if (e.PropertyName != nameof(AxisState.AxisName)) return;
            if (sender is not AxisState st || SelectedItem is not PointTable table) return;

            table.EnsureAxisSlots();
            if (st.Index < table.AxisNames.Count)
                table.AxisNames[st.Index] = st.AxisName;
        }

        // ===== 当前工位内的点位行增删 =====

        public ICommand AddPointCommand { get; }
        public ICommand DeletePointCommand { get; }
        public ICommand CompileCommand { get; }

        private void AddPoint()
        {
            if (SelectedItem is not PointTable table) return;
            var point = new PointItem { Name = UniqueName("点位", table.Points.Select(p => p.Name)) };
            table.Points.Add(point);
            SelectedPoint = point;
        }

        private void DeletePoint()
        {
            if (SelectedItem is not PointTable table || SelectedPoint is not PointItem point) return;
            table.Points.Remove(point);
            SelectedPoint = null;
        }

        /// <summary>编译当前工位的「时序标记 / 同步组」列，做编译期冲突检测，结果填入 CompileIssues。</summary>
        private void Compile()
        {
            CompileIssues.Clear();
            if (SelectedItem == null)
            {
                HasCompiled = true;
                HasCompileIssues = false;
                ErrorCount = 0;
                WarningCount = 0;
                CompileSummary = "未选择工位，无可编译内容。";
                return;
            }

            var issues = TimingCompiler.Compile(SelectedItem.Points);
            foreach (var i in issues) CompileIssues.Add(i);

            ErrorCount = issues.Count(i => i.Severity == TimingSeverity.Error);
            WarningCount = issues.Count(i => i.Severity == TimingSeverity.Warning);
            HasCompileIssues = ErrorCount + WarningCount > 0;

            CompileSummary = HasCompileIssues
                ? $"发现 {ErrorCount} 个错误、{WarningCount} 个警告，请修正后再下发。"
                : "时序编译通过，无冲突，可下发到实时调度。";
            HasCompiled = true;
        }

        /// <summary>生成「前缀 + 最小可用序号」的唯一名称，避免删除后重名。</summary>
        private static string UniqueName(string prefix, System.Collections.Generic.IEnumerable<string> existing)
        {
            var used = new System.Collections.Generic.HashSet<string>(existing);
            for (int n = 1; ; n++)
            {
                var name = prefix + n;
                if (!used.Contains(name)) return name;
            }
        }

        // ===== 轴控制命令（运行态仿真：当前无运动硬件层，命令在此更新运行态，后续接运动引擎）=====
        public ICommand EnableCommand { get; }
        public ICommand HomeCommand { get; }
        public ICommand InchCommand { get; }
        public ICommand JogCommand { get; }
        public ICommand MoveToPointCommand { get; }
        public ICommand SaveCurrentCommand { get; }

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
            double step = jog ? JogStep : InchStep;
            AxisStates[i].CurrentPosition += dir * step;
        }

        // ===== 点位行操作命令（逐行按钮，弹窗确认后执行；运行态仿真）=====

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
            if (SelectedItem == null && Items.Count > 0)
                SelectedItem = Items[0];
            if (SelectedPoint == null && CurrentPoints is { Count: > 0 } points)
                SelectedPoint = points[0];
        }
    }

    /// <summary>单个轴槽的运行/配置状态。</summary>
    public class AxisState : ViewModelBase
    {
        public int Index { get; }

        private string _axisName = string.Empty;
        private bool _enabled;
        private bool _homed;
        private double _current;

        public AxisState(int index) => Index = index;

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
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
