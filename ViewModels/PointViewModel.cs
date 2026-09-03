// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Cad;
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
            JogStartCommand = new RelayCommand(JogStart);
            JogStopCommand = new RelayCommand(_ => JogStop());
            MoveToPointCommand = new RelayCommand(MoveToPoint);
            SaveCurrentCommand = new RelayCommand(SaveCurrent);
            AddPointCommand = new RelayCommand(_ => AddPoint(), _ => SelectedItem != null);
            DeletePointCommand = new RelayCommand(_ => DeletePoint(), _ => CanDeletePoint);
            CompileCommand = new RelayCommand(_ => Compile(), _ => SelectedItem != null);
            ImportDxfCommand = new RelayCommand(_ => ImportDxf(), _ => SelectedItem != null);

            // 轨迹仿真命令
            SimPlayCommand = new RelayCommand(_ => SimPlay(), _ => CanSimulate(null) && !IsSimulating);
            SimStopCommand = new RelayCommand(_ => SimStop(), _ => IsSimulating);
            SimResetCommand = new RelayCommand(_ => SimReset(), _ => HasTable);
            SimLoopCommand = new RelayCommand(_ => SimLoop = !SimLoop, _ => HasTable);

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
            BindSimTable(SelectedItem);
            SelectedPoint = null;
            OnPropertyChanged(nameof(CurrentPoints));
            OnPropertyChanged(nameof(HasTable));
        }

        // 切换工位时把点位集合的增删订阅到仿真重算；旧工位退订，避免悬空引用
        private PointTable? _simBoundTable;
        private void BindSimTable(PointTable? table)
        {
            if (ReferenceEquals(_simBoundTable, table)) return;
            if (_simBoundTable != null)
                _simBoundTable.Points.CollectionChanged -= OnSimPointsChanged;
            _simBoundTable = table;
            if (_simBoundTable != null)
                _simBoundTable.Points.CollectionChanged += OnSimPointsChanged;
            RebuildSim();
        }

        private void OnSimPointsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildSim();

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
        public ICommand ImportDxfCommand { get; }

        private void AddPoint()
        {
            if (SelectedItem is not PointTable table) return;
            var point = new PointItem { Name = UniqueName("点位", table.Points.Select(p => p.Name)) };
            table.Points.Add(point);
            SelectedPoint = point;
        }

        /// <summary>从 DXF 导入几何，生成点位表行（轴1/轴2 ← DXF X/Y，步长 5mm）。
        /// 追加到当前工位末尾，不覆盖已有行；用户可手动删除冗余后再用。</summary>
        private void ImportDxf()
        {
            if (SelectedItem is not PointTable table) return;
            try
            {
                var dlg = new OpenFileDialog
                {
                    Title = "选择 DXF 图形文件",
                    Filter = "DXF 图形 (*.dxf)|*.dxf|所有文件 (*.*)|*.*",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dlg.ShowDialog() != true) return;

                // stepMm=5：5mm 采样兼顾密度与点表规模；用户后续可在表格里改位置/速度。
                var pts = DxfImporter.ImportToPoints(dlg.FileName, stepMm: 5.0);
                if (pts.Count == 0)
                {
                    StatusBarService.ReportException("DXF 解析完成但未提取到几何（支持 LINE / LWPOLYLINE / POLYLINE / ARC / CIRCLE）。");
                    return;
                }

                int baseIndex = table.Points.Count;
                for (int i = 0; i < pts.Count; i++)
                {
                    var p = new PointItem
                    {
                        Name = UniqueName($"DXF{baseIndex + i + 1}", table.Points.Select(x => x.Name))
                    };
                    p.Positions[0].Position = Math.Round(pts[i].X, 4);
                    p.Positions[1].Position = Math.Round(pts[i].Y, 4);
                    // Positions[2]/[3]（轴3/轴4，如 Z/A）留 0，用户按需编辑
                    p.Positions[0].Speed = 100;
                    p.Positions[1].Speed = 100;
                    table.Points.Add(p);
                }
                SelectedPoint = table.Points[table.Points.Count - 1];
            }
            catch (Exception ex)
            {
                // 对话框若被环境（如安全软件）阻断，给出明确提示，避免"点了没反应"
                StatusBarService.ReportException($"导入 DXF 失败：{ex.Message}");
            }
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
        public ICommand JogStartCommand { get; }
        public ICommand JogStopCommand { get; }
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
            var name = string.IsNullOrWhiteSpace(AxisStates[i].AxisName) ? $"轴{i + 1}" : AxisStates[i].AxisName;
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

        // ===== 轨迹仿真（右上角面板）：按点位顺序在 2D 画布上演示运动动画 =====
        // 用轴槽 0/1 作为 X/Y 坐标（轴名来自 AxisStates），自动拟合缩放；同时把插值结果
        // 写回 4 个轴的 CurrentPosition，使「轴控制」卡片随动画实时联动。

        private readonly ObservableCollection<SimPointVm> _simPoints = new();
        private readonly List<double> _segDur = new();
        private DispatcherTimer? _simTimer;
        private int _simSeg;
        private double _simT;

        /// <summary>仿真点位集合（逻辑坐标 0..100，画布内 Viewbox 自适应缩放）。</summary>
        public ObservableCollection<SimPointVm> SimPoints => _simPoints;

        private Geometry? _simTrajectory;
        /// <summary>连接各点位的轨迹折线（逻辑坐标）。</summary>
        public Geometry? SimTrajectory
        {
            get => _simTrajectory;
            private set => SetField(ref _simTrajectory, value);
        }

        private double _simMarkerX = 50;
        private double _simMarkerY = 50;
        /// <summary>动画标记当前位置（逻辑坐标 0..100）。</summary>
        public double SimMarkerX
        {
            get => _simMarkerX;
            private set => SetField(ref _simMarkerX, value);
        }
        public double SimMarkerY
        {
            get => _simMarkerY;
            private set => SetField(ref _simMarkerY, value);
        }

        private double _simLiveX;
        private double _simLiveY;
        /// <summary>动画标记当前位置（轴 1/2 实际单位），用于下方数值读数。</summary>
        public double SimLiveX
        {
            get => _simLiveX;
            private set => SetField(ref _simLiveX, value);
        }
        public double SimLiveY
        {
            get => _simLiveY;
            private set => SetField(ref _simLiveY, value);
        }

        private int _simActiveIndex = -1;
        /// <summary>当前正在前往的目标点位序号（0 基），用于高亮与读数。</summary>
        public int SimActiveIndex
        {
            get => _simActiveIndex;
            private set => SetField(ref _simActiveIndex, value);
        }

        private bool _isSimulating;
        public bool IsSimulating
        {
            get => _isSimulating;
            private set
            {
                if (SetField(ref _isSimulating, value))
                    RaiseSimCanExec();
            }
        }

        private bool _simLoop;
        /// <summary>是否循环播放（到达末点后回到起点继续）。</summary>
        public bool SimLoop
        {
            get => _simLoop;
            set => SetField(ref _simLoop, value);
        }

        private double _simSpeed = 1.0;
        /// <summary>仿真速度倍率（0.25..3，越大越快）。</summary>
        public double SimSpeed
        {
            get => _simSpeed;
            set => SetField(ref _simSpeed, value);
        }

        public ICommand SimPlayCommand { get; }
        public ICommand SimStopCommand { get; }
        public ICommand SimResetCommand { get; }
        public ICommand SimLoopCommand { get; }

        private bool CanSimulate(object? _) => HasTable && CurrentPoints is { Count: >= 2 };

        private void RaiseSimCanExec() => CommandManager.InvalidateRequerySuggested();

        /// <summary>重算仿真点位与轨迹：从当前工位点位取轴 1/2 坐标，自动拟合到 0..100 逻辑空间。</summary>
        private void RebuildSim()
        {
            StopTimer();
            IsSimulating = false;
            _simPoints.Clear();
            _segDur.Clear();

            var pts = CurrentPoints;
            if (pts == null || pts.Count == 0)
            {
                SimTrajectory = null;
                SimMarkerX = 50; SimMarkerY = 50;
                SimActiveIndex = -1;
                RaiseSimCanExec();
                return;
            }

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;
            var coords = new List<double[]>();
            foreach (var p in pts)
            {
                var a = new double[4];
                for (int i = 0; i < 4; i++)
                    a[i] = (i < p.Positions.Count) ? p.Positions[i].Position : 0;
                coords.Add(a);
                if (a[0] < minX) minX = a[0]; if (a[0] > maxX) maxX = a[0];
                if (a[1] < minY) minY = a[1]; if (a[1] > maxY) maxY = a[1];
            }

            const double pad = 10;
            double denomX = (maxX - minX) == 0 ? 1 : (maxX - minX);
            double denomY = (maxY - minY) == 0 ? 1 : (maxY - minY);
            for (int i = 0; i < pts.Count; i++)
            {
                double lx = pad + (coords[i][0] - minX) / denomX * (100 - 2 * pad);
                double ly = pad + (1 - (coords[i][1] - minY) / denomY) * (100 - 2 * pad); // 反转 Y：向上为更大
                _simPoints.Add(new SimPointVm { Index = i + 1, X = lx, Y = ly, A = coords[i] });
            }

            if (_simPoints.Count >= 2)
            {
                var fig = new PathFigure { StartPoint = new Point(_simPoints[0].X, _simPoints[0].Y) };
                var poly = new PolyLineSegment();
                for ( int i = 1; i < _simPoints.Count; i++)
                    poly.Points.Add(new Point(_simPoints[i].X, _simPoints[i].Y));
                fig.Segments.Add(poly);
                SimTrajectory = new PathGeometry { Figures = { fig } };

                double totalLen = 0;
                var lens = new List<double>();
                for (int i = 0; i < _simPoints.Count - 1; i++)
                {
                    double dx = coords[i + 1][0] - coords[i][0];
                    double dy = coords[i + 1][1] - coords[i][1];
                    double dz = coords[i + 1][2] - coords[i][2];
                    double dw = coords[i + 1][3] - coords[i][3];
                    double d = Math.Sqrt(dx * dx + dy * dy + dz * dz + dw * dw);
                    lens.Add(d);
                    totalLen += d;
                }
                const double baseTotal = 6.0; // 基础总时长（秒），由 SimSpeed 实时缩放
                for (int i = 0; i < lens.Count; i++)
                {
                    double dur = totalLen > 0 ? lens[i] / totalLen * baseTotal : baseTotal / lens.Count;
                    _segDur.Add(Math.Max(0.3, dur));
                }
            }
            else
            {
                SimTrajectory = null;
            }

            // 标记回到起点，并让 4 轴读数同步到起点
            if (_simPoints.Count > 0)
            {
                var first = _simPoints[0];
                SimMarkerX = first.X; SimMarkerY = first.Y;
                SimLiveX = first.A[0]; SimLiveY = first.A[1];
                for (int i = 0; i < AxisStates.Count && i < 4; i++)
                    AxisStates[i].CurrentPosition = first.A[i];
                SetActiveIndex(0);
            }
            else
            {
                SimActiveIndex = -1;
            }
            RaiseSimCanExec();
        }

        /// <summary>开始仿真：重算几何后启动定时器，逐段插值推进标记。</summary>
        private void SimPlay()
        {
            if (!CanSimulate(null)) return;
            StopTimer();
            RebuildSim();
            if (_simPoints.Count < 2)
            {
                StatusBarService.ReportException("当前工位点位不足 2 个，无法演示轨迹。");
                return;
            }
            IsSimulating = true;
            _simSeg = 0;
            _simT = 0;
            ApplySegmentToMarker(0, 0);
            SetActiveIndex(0);
            _simTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _simTimer.Tick += SimTick;
            _simTimer.Start();
        }

        private void SimStop()
        {
            StopTimer();
            IsSimulating = false;
        }

        private void SimReset()
        {
            StopTimer();
            IsSimulating = false;
            RebuildSim();
        }

        private void SimTick(object? sender, EventArgs e)
        {
            if (_simPoints.Count < 2)
            {
                StopTimer();
                IsSimulating = false;
                return;
            }
            double dt = _simTimer?.Interval.TotalSeconds ?? 0.033;
            _simT += dt * SimSpeed;
            int n = _simPoints.Count;

            while (_simSeg < n - 1 && _simT >= _segDur[_simSeg])
            {
                _simT -= _segDur[_simSeg];
                _simSeg++;
                if (_simSeg >= n - 1)
                {
                    if (SimLoop)
                    {
                        _simSeg = 0;
                        _simT = 0;
                    }
                    else
                    {
                        ApplySegmentToMarker(n - 2, 1);
                        SetActiveIndex(n - 1);
                        StopTimer();
                        IsSimulating = false;
                        return;
                    }
                }
            }

            if (_simSeg >= n - 1) _simSeg = n - 2;
            double dur = _segDur[_simSeg];
            double p = dur > 0 ? Math.Min(1, _simT / dur) : 1;
            ApplySegmentToMarker(_simSeg, p);
            SetActiveIndex(_simSeg + (p >= 1 ? 1 : 0));
        }

        /// <summary>把第 seg 段、进度 p(0..1) 的插值位置写回标记与 4 轴当前位置。</summary>
        private void ApplySegmentToMarker(int seg, double p)
        {
            if (seg < 0 || seg >= _simPoints.Count - 1) return;
            var a = _simPoints[seg];
            var b = _simPoints[seg + 1];
            SimMarkerX = a.X + (b.X - a.X) * p;
            SimMarkerY = a.Y + (b.Y - a.Y) * p;
            SimLiveX = a.A[0] + (b.A[0] - a.A[0]) * p;
            SimLiveY = a.A[1] + (b.A[1] - a.A[1]) * p;
            for (int i = 0; i < AxisStates.Count && i < 4; i++)
                AxisStates[i].CurrentPosition = a.A[i] + (b.A[i] - a.A[i]) * p;
        }

        private void SetActiveIndex(int idx)
        {
            for (int i = 0; i < _simPoints.Count; i++)
                _simPoints[i].IsActive = i == idx;
            SimActiveIndex = idx;
        }

        private void StopTimer()
        {
            if (_simTimer != null)
            {
                _simTimer.Stop();
                _simTimer.Tick -= SimTick;
                _simTimer = null;
            }
        }

        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0)
                SelectedItem = Items[0];
            if (SelectedPoint == null && CurrentPoints is { Count: > 0 } points)
                SelectedPoint = points[0];
        }
    }

    /// <summary>仿真面板中的单个点位：逻辑坐标 (X,Y∈0..100) + 4 轴实际坐标 A，供轨迹画布与插值使用。</summary>
    public class SimPointVm : INotifyPropertyChanged
    {
        public int Index { get; set; }

        private double _x;
        public double X
        {
            get => _x;
            set { if (_x != value) { _x = value; OnChanged(); } }
        }

        private double _y;
        public double Y
        {
            get => _y;
            set { if (_y != value) { _y = value; OnChanged(); } }
        }

        /// <summary>轴 1..4 的实际坐标（按槽位顺序），插值演示时写回轴当前位置。</summary>
        public double[] A { get; set; } = new double[4];

        public string Name { get; set; } = string.Empty;

        private bool _active;
        public bool IsActive
        {
            get => _active;
            set { if (_active != value) { _active = value; OnChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnChanged([CallerMemberName] string? p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    /// <summary>单个轴槽的运行/配置状态。</summary>
    public class AxisState : ViewModelBase
    {
        public int Index { get; }

        private string _axisName = string.Empty;
        private bool _enabled;
        private bool _homed;
        private double _current;
        private double _inchStep = 1.0;
        private double _jogStep = 10.0;

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
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
