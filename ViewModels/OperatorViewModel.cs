// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>操作员控制台 ViewModel：面向产线操作员，展示生产数据（产量/良率/节拍/运行时长）、
    /// 良率与节拍折线图、启动/停止/急停/复位控制、用户切换，以及异常日志。
    /// 运行逻辑在选中工位（点位表）上顺序经过各点位，按 4 轴槽把每个轴走到目标位置+速度，经 HardwareBridge.Current 真正下发到机台（未挂载真实桥时走桩日志）。</summary>
    /// <summary>运行模式：手动（单步） / 自动（连续循环）。</summary>
    public enum OpMode
    {
        Manual,
        Auto
    }

    public class OperatorViewModel : ViewModelBase, IEnsureDefaultSelection
    {
        private readonly Random _rand = new();
        private readonly Random _timingRand = new();

        // ---------- 时序偏差监控（对应专利「方向二」运行时监控与可视化层）----------
        /// <summary>运行时时序监控行：按点位顺序比对实际触发时刻与预期时序标记。</summary>
        public ObservableCollection<TimingRow> TimingRows { get; } = new();

        /// <summary>时序偏差阈值（毫秒）：|偏差| 超过此值记为「偏差」，超过 2.5 倍记为「超阈」并报警。</summary>
        private const double TimingThresholdMs = 2.0;

        /// <summary>累计仿真触发时钟（ms），自运行起始累加，用于测算每步实际触发时刻。</summary>
        private double _timingClockMs;

        // ---------- 工位（点位表）选择 + 运行进度 ----------
        private PointTable? _selectedTable;
        private PointItem? _selectedPoint;
        private bool _isRunning;
        private bool _eStopped;
        private bool _isPaused;
        private int _runIndex = -1;
        private DateTime _runStart;
        private string _statusText = "点「启动」开始运行（按全部流程并发执行）。";

        public ObservableCollection<PointTable> Tables { get; }

        public PointTable? SelectedTable
        {
            get => _selectedTable;
            set
            {
                if (!SetField(ref _selectedTable, value)) return;
                if (_simTable != null) _simTable.Points.CollectionChanged -= OnSimPointsChanged;
                _simTable = value;
                if (_simTable != null) _simTable.Points.CollectionChanged += OnSimPointsChanged;
                _runIndex = -1;
                SelectedPoint = null;
                TimingRows.Clear();
                RebuildSim3D();
                OnPropertyChanged(nameof(CurrentPoints));
                OnPropertyChanged(nameof(CanRun));
                OnPropertyChanged(nameof(CurrentStation));
            }
        }

        /// <summary>工位点位增减/编辑后重建 3D 场景。</summary>
        private void OnSimPointsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => RebuildSim3D();

        public ObservableCollection<PointItem>? CurrentPoints => SelectedTable?.Points;

        public PointItem? SelectedPoint
        {
            get => _selectedPoint;
            set => SetField(ref _selectedPoint, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (!SetField(ref _isRunning, value)) return;
                OnPropertyChanged(nameof(CanRun));
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanStop));
                OnPropertyChanged(nameof(CanPause));
                OnPropertyChanged(nameof(CanEStop));
                StatusBarService.SetRunState(IsRunning, EStopped);
            }
        }

        /// <summary>是否处于急停锁定状态（必须复位才能再次启动）。</summary>
        public bool EStopped
        {
            get => _eStopped;
            set
            {
                if (!SetField(ref _eStopped, value)) return;
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanRun));
                OnPropertyChanged(nameof(CanEStop));
                StatusBarService.SetRunState(IsRunning, EStopped);
            }
        }

        /// <summary>是否允许启动：未运行、未急停、已选工位且工位至少有 1 个点位。</summary>
        public bool CanRun => !IsRunning && !EStopped && (ProjectStore.Data.Flows.Count > 0 || HasAnyRunnableTable());

        private bool HasAnyRunnableTable() => Tables != null && Tables.Any(t => t.Points != null && t.Points.Count > 0);

        /// <summary>启动/继续按钮可用：未运行未急停且已选工位，或已暂停（点“继续”恢复）。</summary>
        public bool CanStart => (!IsRunning || IsPaused) && !EStopped && (ProjectStore.Data.Flows.Count > 0 || HasAnyRunnableTable());

        /// <summary>停止按钮可用：仅在运行时。</summary>
        public bool CanStop => IsRunning;

        /// <summary>急停按钮可用：运行中或存在急停锁定（随时可切断）。</summary>
        public bool CanEStop => IsRunning || EStopped;

        /// <summary>是否处于暂停态（可恢复）。</summary>
        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                if (!SetField(ref _isPaused, value)) return;
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanPause));
                OnPropertyChanged(nameof(RunButtonText));
            }
        }

        /// <summary>暂停按钮可用：仅在运行中且未暂停。</summary>
        public bool CanPause => IsRunning && !IsPaused;

        /// <summary>启动/继续按钮文字（暂停后为“继续”）。</summary>
        public string RunButtonText => IsPaused ? "继续" : "启动";

        public string CurrentStation => SelectedTable?.Name ?? "未选择";

        public string StatusText
        {
            get => _statusText;
            set => SetField(ref _statusText, value);
        }

        // ---------- 生产数据 KPI ----------
        private int _totalCount;
        private double _yield = 99.0;     // 良率 %
        private double _cycleTime = 2.4;  // 节拍 s
        private string _runElapsedText = "00:00";

        /// <summary>累计产量（件）。</summary>
        public int TotalCount
        {
            get => _totalCount;
            set => SetField(ref _totalCount, value);
        }

        /// <summary>良率（%）。</summary>
        public double Yield
        {
            get => _yield;
            set => SetField(ref _yield, value);
        }

        /// <summary>节拍（秒 / 件）。</summary>
        public double CycleTime
        {
            get => _cycleTime;
            set => SetField(ref _cycleTime, value);
        }

        /// <summary>运行时长文本（mm:ss）。</summary>
        public string RunElapsedText
        {
            get => _runElapsedText;
            set => SetField(ref _runElapsedText, value);
        }

        // ---------- 折线图数据 ----------
        /// <summary>采样点：每运行一个工位节拍采集一次（良率% / 节拍s / 累计产量）。</summary>
        public ObservableCollection<ChartSample> Samples { get; } = new();

        // ---------- 运行轨迹 3D 仿真 ----------
        /// <summary>3D 场景点位（原始机械坐标：X / Z(向上) / Y 已映射为 3D 的 X/Y/Z）。控件内部自动归一化缩放。</summary>
        private Point3DCollection _opSimPoints = new();
        public Point3DCollection OpSimPoints
        {
            get => _opSimPoints;
            private set { _opSimPoints = value; OnPropertyChanged(nameof(OpSimPoints)); }
        }

        /// <summary>当前位置（红色头），由 33ms 仿真定时器插值驱动；手动单步时跳到该点。</summary>
        private Point3D _opSimHead;
        public Point3D OpSimHead
        {
            get => _opSimHead;
            private set { _opSimHead = value; OnPropertyChanged(nameof(OpSimHead)); }
        }

        /// <summary>是否显示当前位置头。</summary>
        private bool _opSimHeadVisible;
        public bool OpSimHeadVisible
        {
            get => _opSimHeadVisible;
            private set { _opSimHeadVisible = value; OnPropertyChanged(nameof(OpSimHeadVisible)); }
        }

        /// <summary>当前目标点位索引（橙色高亮）。</summary>
        private int _opSimIndex = -1;
        public int OpSimIndex
        {
            get => _opSimIndex;
            private set { _opSimIndex = value; OnPropertyChanged(nameof(OpSimIndex)); }
        }

        /// <summary>相机抓拍预览图（流程「相机」步骤真实取帧后写入，绑定到 Sim3DView.CaptureImage）。</summary>
        private ImageSource? _opSimCapture;
        public ImageSource? OpSimCapture
        {
            get => _opSimCapture;
            private set { _opSimCapture = value; OnPropertyChanged(nameof(OpSimCapture)); }
        }

        /// <summary>仿真沿路径插值相位（单位：点位）。</summary>
        private double _simPhase;
        private DateTime _simLast = DateTime.Now;
        private PointTable? _simTable;
        private readonly DispatcherTimer _sim3DTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };

        /// <summary>良率折线（0~100% 映射到绘图区）。</summary>
        public PointCollection YieldPoints { get; private set; } = new();

        /// <summary>节拍折线（0~10s 映射到绘图区）。</summary>
        public PointCollection CyclePoints { get; private set; } = new();

        private const double CW = 640;   // 绘图区宽
        private const double CH = 220;   // 绘图区高
        private const double Pad = 30;   // 边距

        // ---------- 异常日志 ----------
        public ObservableCollection<LogEntry> Log { get; } = new();

        /// <summary>异常日志按等级分类计数（面板标题展示用，Log 变更时自动重算并触发 INPC）。</summary>
        public int WarnCount { get; private set; }
        public int ErrorCount { get; private set; }
        public bool IsLogEmpty => Log.Count == 0;
        public ICommand ClearLogCommand { get; }
        /// <summary>导出异常日志到文件（按钮：导出）。仅当面板有内容（Warn/Error）时可用。</summary>
        public ICommand ExportLogCommand { get; }

        // ---------- 用户切换 ----------
        public ObservableCollection<string> Users { get; } = new() { "操作员 A", "操作员 B", "班组长", "管理员" };
        private string _currentUser = "操作员 A";

        public string CurrentUser
        {
            get => _currentUser;
            set
            {
                if (!SetField(ref _currentUser, value)) return;
                AddLog(LogLevel.Info, $"用户切换为：{value}");
            }
        }

        public ICommand RunCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand EStopCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand SwitchUserCommand { get; }
        public ICommand ManualCommand { get; }
        public ICommand AutoCommand { get; }

        private OpMode _mode = OpMode.Auto;

        /// <summary>当前运行模式：手动（单步）/ 自动（连续）。</summary>
        public OpMode Mode
        {
            get => _mode;
            set
            {
                if (!SetField(ref _mode, value)) return;
                AddLog(LogLevel.Info, value == OpMode.Auto ? "切换为自动模式。" : "切换为手动模式。");
            }
        }

        // ---------- 真实运行控制（后台线程执行，避免 WaitAxisDone 阻塞 UI）----------
        private Thread? _runThread;
        private volatile bool _stopRequested;
        private volatile bool _eStopRequested;
        private volatile bool _pauseRequested;
        private readonly ManualResetEventSlim _resumeEvent = new(true);
        private Stopwatch _runSw = new();
        private FlowRunControl? _flowCtrl;

        // ---------- 定时器刷新（运行流程只写共享态，UI 由本定时器周期拉取，解耦防卡顿）----------
        /// <summary>150ms 定时器：把后台流程写进 FlowRunStore 的状态推到 FlowItem.Status / 状态文本 / 运行时长，
        /// 并排空日志与 UI 动作队列。运行线程本身绝不调用 Dispatcher，界面刷新完全由本定时器在 UI 线程完成。</summary>
        private readonly DispatcherTimer _uiTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };
        /// <summary>运行线程 → UI 的日志队列（线程安全，定时器在 UI 线程排空）。</summary>
        private readonly ConcurrentQueue<(string Msg, LogLevel Level)> _logQueue = new();
        /// <summary>运行线程 → UI 的动作队列（如 AdvanceProduction），定时器在 UI 线程执行。</summary>
        private readonly ConcurrentQueue<Action> _uiQueue = new();
        /// <summary>是否有运行在进行中（流程并发 / 工位顺序）。为 true 时定时器才把共享态推到 FlowItem.Status，
        /// 避免运行结束后定时器把手动（流程页单步）运行时写的状态覆盖成 就绪。</summary>
        private volatile bool _runActive;

        /// <summary>运行代号：每次 启动/复位 自增；旧的 FinalizeRun/FinalizeReset 凭代号判断是否已被新运行取代，
        /// 避免旧运行收尾在 UI 线程上把新运行的 _runActive 等状态错误覆盖（点复位时旧运行可能仍在跑）。</summary>
        private int _runGen;

        public OperatorViewModel()
        {
            _ = AuthorWatermark.Signature;   // 作者水印引用（误删 AuthorWatermark.cs 将编译失败）

            Tables = ProjectStore.Data.PointTables;
            RunCommand = new RelayCommand(_ => StartOrResume());
            StopCommand = new RelayCommand(_ => Stop());
            EStopCommand = new RelayCommand(_ => EStop());
            ResetCommand = new RelayCommand(_ => Reset());
            PauseCommand = new RelayCommand(_ => Pause());
            SwitchUserCommand = new RelayCommand(p => { if (p is string u && !string.IsNullOrEmpty(u)) CurrentUser = u; });
            ManualCommand = new RelayCommand(_ => Mode = OpMode.Manual);
            AutoCommand = new RelayCommand(_ => Mode = OpMode.Auto);
            ClearLogCommand = new RelayCommand(_ => ClearLog());
            ExportLogCommand = new RelayCommand(_ => ExportLog(), _ => !IsLogEmpty);

            // 日志集合变更时刷新 WarnCount/ErrorCount/IsLogEmpty（AddLog 插入与 ClearLog 清空都走这里）。
            Log.CollectionChanged += (_, __) => RefreshLogCounts();


            // 预置一段历史采样，使折线图打开即有内容
            var baseT = DateTime.Now.AddMinutes(-10);
            for (int i = 0; i < 12; i++)
            {
                Samples.Add(new ChartSample
                {
                    Time = baseT.AddMinutes(i),
                    Yield = 98.5 + _rand.NextDouble() * 1.2,
                    Cycle = 2.3 + _rand.NextDouble() * 0.4,
                    Count = (i + 1) * 40
                });
            }
            RebuildChart();
            AddLog(LogLevel.Info, "操作员控制台已启动。");

            // 定时器刷新：运行流程只写共享态（FlowRunStore），UI 完全由此定时器在 UI 线程周期拉取，
            // 流程线程不与界面交互，杜绝高频 Invoke 造成的卡顿。
            _uiTimer.Tick += UiTimerTick;
            _uiTimer.Start();

            // 3D 仿真定时器：运行时沿点位路径循环插值移动当前位置头（仅 UI 线程，不触碰运行线程）。
            _sim3DTimer.Tick += Sim3DTick;
            _sim3DTimer.Start();
        }

        /// <summary>定时器回调（UI 线程）：推状态、刷新文本、排空队列。运行线程不在此做任何 UI 操作。</summary>
        private void UiTimerTick(object? sender, EventArgs e)
        {
            // 1) 排空日志队列（只把 警告/异常 交给 AddLog，普通信息不进面板）
            while (_logQueue.TryDequeue(out var item))
                AddLog(item.Level, item.Msg);

            // 2) 排空 UI 动作队列（onFlowDone 等）
            while (_uiQueue.TryDequeue(out var act))
            {
                try { act(); } catch { }
            }

            // 3) 运行期：把共享态推到 FlowItem.Status（UI 线程安全；SetField 同值不触发 INPC，无抖动）
            if (_runActive)
            {
                var flows = ProjectStore.Data.Flows;
                if (flows != null)
                {
                    string? runningStep = null;
                    int runningCount = 0;
                    foreach (var f in flows)
                    {
                        if (!FlowRunStore.Contains(f)) continue;
                        var (st, step, _) = FlowRunStore.Get(f);
                        if (f.Status != st) f.Status = st;
                        if (st == FlowStatus.Running)
                        {
                            runningCount++;
                            if (runningStep == null) runningStep = step;
                        }
                    }
                    if (runningCount > 0 && runningStep != null)
                        StatusText = $"运行中：{runningCount} 个流程执行中 · {runningStep}";
                }
                var el = _runSw.Elapsed;
                RunElapsedText = $"{(int)el.TotalMinutes:D2}:{el.Seconds:D2}";
            }
        }

        // ---------- 运行控制 ----------
        private void Start()
        {
            if (!CanRun) return;
            EStopped = false;
            BuildTimingRows();

            // 手动模式：每按一次「启动」前进一个点位（单步，后台执行该点动作避免 WaitAxisDone 卡 UI）
            if (Mode == OpMode.Manual)
            {
                _runIndex = _runIndex < 0 ? 0 : _runIndex + 1;
                if (_runIndex >= SelectedTable!.Points.Count)
                {
                    _runIndex = SelectedTable.Points.Count - 1;
                    SelectedPoint = _runIndex >= 0 ? SelectedTable.Points[_runIndex] : null;
                    StatusText = $"已到末点位：「{SelectedTable.Name}」共 {SelectedTable.Points.Count} 个点位。";
                    AddLog(LogLevel.Info, $"手动模式已走到末点位：「{SelectedTable.Name}」。");
                    return;
                }
                int idx = _runIndex;
                var table = SelectedTable;
                // 用 Thread 而非 Task：后台执行该点动作（WaitAxisDone 可能阻塞），完成后经 Ui 异步回写。
                var t = new Thread(() =>
                {
                    ExecutePoint(idx);
                    Ui(() =>
                    {
                        StepTo(idx);
                        RecordTiming(idx, table.Points[idx]);
                        AdvanceProduction();
                        SetSimHeadToPoint(idx);
                    });
                }) { IsBackground = true, Name = "OpStep" };
                t.Start();
                StatusText = $"手动单步：「{table.Name}」{table.Points[idx].Name}（{idx + 1}/{table.Points.Count}）";
                AddLog(LogLevel.Info, $"手动单步 -> {table.Points[idx].Name}");
                return;
            }

            // 自动模式
            _stopRequested = false;
            _eStopRequested = false;
            _pauseRequested = false;
            _resumeEvent.Set();
            IsRunning = true;
            IsPaused = false;
            _runStart = DateTime.Now;
            _runSw.Restart();
            _runIndex = 0;
            _simPhase = 0;
            _simLast = DateTime.Now;
            // 优先并发运行全部流程；没有流程时回退到工位顺序运行（兼容旧工程）
            if (ProjectStore.Data.Flows.Count > 0)
            {
                StartFlows();
                return;
            }
            // 自动模式：依次运行全部有点位的工作站（按 Tables 列表顺序）
            int runnable = Tables.Count(t => t.Points != null && t.Points.Count > 0);
            AddLog(LogLevel.Info, $"启动运行（自动）：全部 {runnable} 个工位，按顺序连续运行。");
            StatusText = runnable > 0 ? "运行中：按全部工位顺序执行…" : "运行中：无工位可执行。";
            _runActive = true;
            _runThread = new Thread(RunLoop) { IsBackground = true, Name = "OpRun" };
            _runThread.Start();
        }

        /// <summary>启动 = 并发跑 ProjectStore.Data.Flows 里每个 Flow 的「循环开始/循环结束」等逻辑区域（次数取 SetValue）。
        /// 通过 FlowRunnerService 为每条流程起一条后台 Thread（内部 while 循环），真实驱动机台；支持暂停 / 停止 / 急停。</summary>
        private void StartFlows()
        {
            var flows = ProjectStore.Data.Flows;
            if (flows == null || flows.Count == 0) { StatusText = "没有可执行的流程。"; return; }
            _stopRequested = false;
            _eStopRequested = false;
            _pauseRequested = false;
            _resumeEvent.Set();
            IsRunning = true;
            IsPaused = false;
            _runStart = DateTime.Now;
            _runSw.Restart();

            var ctrl = new FlowRunControl();
            ctrl.InitVars();
            ctrl.OnCameraCapture = (b, w, h) => _uiQueue.Enqueue(() => SetCapture(b, w, h));
            _flowCtrl = ctrl;

            // 运行线程只写共享态 FlowRunStore，UI 由定时器拉取；日志/动作入队，定时器在 UI 线程排空。
            // 全部流程结束后由看门狗线程触发 onComplete（入队到 UI 线程执行），全程不依赖 Task。
            FlowRunStore.ClearAll();
            _runActive = true;

            AddLog(LogLevel.Info, $"启动运行（自动）：并发执行 {flows.Count} 个流程。");
            StatusText = $"运行中：并发执行 {flows.Count} 个流程…";

            int gen = ++_runGen;
            FlowRunnerService.RunAllAsync(
                ctrl,
                log: (msg, lvl) => _logQueue.Enqueue((msg, lvl)),
                onStep: (idx, name, cur) => { },
                onFlowDone: (idx, name) => _uiQueue.Enqueue(() => AdvanceProduction()),
                onComplete: () => _uiQueue.Enqueue(() => FinalizeRun(gen)),
                ct: CancellationToken.None
            );
        }

        /// <summary>全部流程运行结束后的收尾（由 UI 定时器队列在 UI 线程执行）：复位运行态、写回变量、刷新状态文本。</summary>
        private void FinalizeRun(int gen)
        {
            if (gen != _runGen) return;   // 已被新的 启动/复位 取代，丢弃旧收尾，避免覆盖新运行状态
            IsRunning = false;
            IsPaused = false;
            _runActive = false;
            _flowCtrl?.WriteBackVars();
            if (_eStopRequested)
                StatusText = "急停！请复位后重新启动。";
            else if (_stopRequested)
                StatusText = "已停止。";
            else
                StatusText = $"全部流程运行完成：{ProjectStore.Data.Flows.Count} 个流程 / 产量 {TotalCount} 件。";
            AddLog(LogLevel.Info, "全部流程运行结束。");
        }

        /// <summary>推进一次生产数据采样（手动单步与自动 tick 共用）。</summary>
        private void AdvanceProduction()
        {
            TotalCount += 30 + _rand.Next(0, 21);
            Yield = Math.Max(95.0, Math.Min(100.0, Yield + (_rand.NextDouble() - 0.45) * 0.6));
            CycleTime = Math.Max(1.8, Math.Min(3.2, CycleTime + (_rand.NextDouble() - 0.5) * 0.15));
            Samples.Add(new ChartSample { Time = DateTime.Now, Yield = Yield, Cycle = CycleTime, Count = TotalCount });
            if (Samples.Count > 60) Samples.RemoveAt(0);
            RebuildChart();
        }

        private void Stop()
        {
            if (!IsRunning && !IsPaused) return;
            _stopRequested = true;
            _resumeEvent.Set();
            _flowCtrl?.StopRequested = true; _flowCtrl?.ResumeEvent.Set();
            IsRunning = false;
            IsPaused = false;
            _runActive = false;
            FlowRunStore.ClearAll();
            ResetFlowStatuses();
            _simPhase = 0;
            if (SelectedTable != null && SelectedTable.Points.Count > 0) SetSimHeadToPoint(0);
            StatusText = "已停止。";
            AddLog(LogLevel.Warn, "运行已手动停止。");
        }

        private void EStop()
        {
            bool wasRunning = IsRunning || IsPaused;
            _eStopRequested = true;
            _stopRequested = true;
            _resumeEvent.Set();
            _flowCtrl?.EStopRequested = true; _flowCtrl?.StopRequested = true; _flowCtrl?.ResumeEvent.Set();
            // 立即切断所有运动：停轴 + 复位气缸 + 清输出
            var bridge = HardwareBridge.Current;
            try
            {
                foreach (var ax in ProjectStore.Data.Axes) bridge.StopAxis(ax);
                foreach (var cy in ProjectStore.Data.Cylinders) bridge.CylinderReset(cy);
                foreach (var o in ProjectStore.Data.Outputs) bridge.WriteOutput(o, 0);
            }
            catch (Exception ex) { AddLog(LogLevel.Error, $"急停下发异常：{ex.Message}"); }
            IsRunning = false;
            IsPaused = false;
            EStopped = true;
            _runActive = false;
            FlowRunStore.ClearAll();
            ResetFlowStatuses();
            _runIndex = -1;
            SelectedPoint = null;
            TimingRows.Clear();
            _simPhase = 0;
            if (SelectedTable != null && SelectedTable.Points.Count > 0) SetSimHeadToPoint(0);
            StatusText = "急停！所有运动已切断，请复位后重新启动。";
            AddLog(LogLevel.Error, wasRunning ? "急停触发！运行中工位已紧急切断。" : "急停触发！");
        }

        /// <summary>复位按钮：先清掉运行态 / KPI / 时序（与旧行为一致），再把所有「复位流程」（Role=Reset）在后台
        /// Thread 单次跑一遍（RunOneFlow 对 Role=Reset 本就只跑一轮不循环）。状态只写 FlowRunStore，UI 由 150ms
        /// 定时器拉取——与启动运行同一套铁律（禁 Task / 只 Thread / 定时器刷新）。无复位流程时仅做状态复位。</summary>
        private void Reset()
        {
            _stopRequested = true;
            _resumeEvent.Set();
            _eStopRequested = false;
            _pauseRequested = false;
            IsRunning = false;
            IsPaused = false;
            EStopped = false;
            _runActive = false;
            FlowRunStore.ClearAll();
            ResetFlowStatuses();
            _runIndex = -1;
            SelectedPoint = null;
            TimingRows.Clear();
            _simPhase = 0;
            if (SelectedTable != null && SelectedTable.Points.Count > 0) SetSimHeadToPoint(0);
            TotalCount = 0;
            Yield = 99.0;
            CycleTime = 2.4;
            RunElapsedText = "00:00";
            Samples.Clear();
            RebuildChart();

            // 若正在运行其它流程，先让其停止，避免两套运行重叠（旧收尾凭 _runGen 代号自动作废）
            _flowCtrl?.StopRequested = true; _flowCtrl?.ResumeEvent.Set();

            var resetFlows = ProjectStore.Data.Flows?.Where(f => f.Role == FlowRole.Reset).ToList();
            if (resetFlows == null || resetFlows.Count == 0)
            {
                StatusText = "已复位，点「启动」运行（按全部流程并发）。";
                AddLog(LogLevel.Info, "系统已复位（无复位流程）。");
                return;
            }

            // 复位流程：每条后台 Thread 单次运行（不循环），状态经 FlowRunStore 由定时器刷新。
            var ctrl = new FlowRunControl();
            ctrl.InitVars();
            ctrl.OnCameraCapture = (b, w, h) => _uiQueue.Enqueue(() => SetCapture(b, w, h));
            _flowCtrl = ctrl;
            FlowRunStore.ClearAll();
            _runActive = true;
            IsRunning = true;
            IsPaused = false;
            _runSw.Restart();
            int gen = ++_runGen;
            AddLog(LogLevel.Info, $"复位：并发执行 {resetFlows.Count} 个复位流程（单次，不循环）。");
            StatusText = $"复位中：执行 {resetFlows.Count} 个复位流程…";

            FlowRunnerService.RunAllAsync(
                ctrl,
                log: (msg, lvl) => _logQueue.Enqueue((msg, lvl)),
                onStep: (idx, name, cur) => { },
                onFlowDone: (idx, name) => { },
                onComplete: () => _uiQueue.Enqueue(() => FinalizeReset(gen)),
                ct: CancellationToken.None,
                filter: f => f.Role == FlowRole.Reset
            );
        }

        /// <summary>复位流程全部执行完的收尾（由 UI 定时器队列在 UI 线程执行）：复位运行态、写回变量、刷新状态文本。</summary>
        private void FinalizeReset(int gen)
        {
            if (gen != _runGen) return;   // 已被新的 启动/复位 取代，丢弃旧收尾
            _runActive = false;
            IsRunning = false;
            IsPaused = false;
            _flowCtrl?.WriteBackVars();
            StatusText = "复位完成，点「启动」运行（按全部流程并发）。";
            AddLog(LogLevel.Info, "复位流程执行完成。");
        }

        /// <summary>把各流程状态芯片重置为 就绪（UI 线程调用）。</summary>
        private void ResetFlowStatuses()
        {
            var flows = ProjectStore.Data.Flows;
            if (flows == null) return;
            foreach (var f in flows) f.Status = FlowStatus.Idle;
        }

        // ---------- 启动/继续/暂停/复位 调度 ----------
        private void StartOrResume()
        {
            if (IsPaused) { Resume(); return; }
            Start();
        }

        private void Pause()
        {
            if (!CanPause) return;
            _pauseRequested = true;
            _resumeEvent.Reset();
            _flowCtrl.PauseRequested = true; _flowCtrl.ResumeEvent.Reset();
            IsPaused = true;
            StatusText = "已暂停。";
            AddLog(LogLevel.Warn, "运行已暂停。");
        }

        private void Resume()
        {
            if (!IsPaused) return;
            _pauseRequested = false;
            _resumeEvent.Set();
            _flowCtrl.PauseRequested = false; _flowCtrl.ResumeEvent.Set();
            IsPaused = false;
            StatusText = "继续运行。";
            AddLog(LogLevel.Info, "继续运行。");
        }

        /// <summary>后台执行循环：依次把所有工位的 4 轴走到每个点位目标，真实驱动机台；点间响应暂停/停止/急停。</summary>
        private void RunLoop()
        {
            _runSw.Restart();
            int totalTables = 0;
            int totalPoints = 0;
            // 依次运行每一个工位（按 Tables 列出的顺序；空工位自动跳过）
            foreach (var table in Tables)
            {
                if (_eStopRequested || _stopRequested) break;
                if (table == null || table.Points == null || table.Points.Count == 0) continue;
                totalTables++;
                // 切到当前工位并重建时序监控行
                Ui(() =>
                {
                    SelectedTable = table;
                    _runIndex = 0;
                    BuildTimingRows();
                    StatusText = $"运行中：工位「{table.Name}」共 {table.Points.Count} 个点位…";
                });
                int n = table.Points.Count;
                for (int i = 0; i < n; i++)
                {
                    if (_eStopRequested || _stopRequested) break;
                    if (_pauseRequested) { _resumeEvent.Wait(); if (_eStopRequested || _stopRequested) break; }
                    int idx = i;
                    Ui(() =>
                    {
                        _runIndex = idx;
                        StepTo(idx);
                        RecordTiming(idx, table.Points[idx]);
                        AdvanceProduction();
                        var el = _runSw.Elapsed;
                        RunElapsedText = $"{(int)el.TotalMinutes:D2}:{el.Seconds:D2}";
                    });
                    ExecutePointForTable(table, idx);
                    if (_eStopRequested || _stopRequested) break;
                }
                if (_eStopRequested || _stopRequested) break;
                totalPoints += n;
            }
            int finalCount = totalPoints;
            Ui(() =>
            {
                IsRunning = false;
                IsPaused = false;
                _runActive = false;
                if (_eStopRequested)
                    StatusText = "急停！所有运动已切断，请复位后重新启动。";
                else if (_stopRequested)
                    StatusText = "已停止。";
                else if (totalTables == 0)
                    StatusText = "运行完成：没有可执行的工位。";
                else
                    StatusText = $"全部工位运行完成：{totalTables} 个工位 / {finalCount} 个点位 / 产量 {TotalCount} 件。";
            });
        }

        /// <summary>把单个点位（4 轴槽）的真实目标位置+速度下发到机台。</summary>
        private void ExecutePoint(int idx)
        {
            var table = SelectedTable;
            if (table == null || idx < 0 || idx >= table.Points.Count) return;
            var p = table.Points[idx];
            var bridge = HardwareBridge.Current;
            for (int i = 0; i < PointTable.SlotCount; i++)
            {
                var axisName = table.AxisNames.Count > i ? table.AxisNames[i] : string.Empty;
                if (string.IsNullOrWhiteSpace(axisName)) continue;
                var axis = HardwareResolver.ResolveAxis(axisName);
                if (axis == null) { Ui(() => AddLog(LogLevel.Warn, $"找不到轴：{axisName}")); continue; }
                var slot = p.Positions.Count > i ? p.Positions[i] : null;
                if (slot == null) continue;
                try
                {
                    if (slot.Speed > 0) bridge.SetAxisSpeed(axis, slot.Speed);
                    bridge.MoveAxisAbs(axis, slot.Position);
                    bridge.WaitAxisDone(axis);
                }
                catch (Exception ex) { Ui(() => AddLog(LogLevel.Error, $"轴 {axisName} 运动异常：{ex.Message}")); }
            }
        }

        /// <summary>把单个点位（4 轴槽）的真实目标位置+速度下发到机台（显式传入工位；后台线程直接调用）。</summary>
        private void ExecutePointForTable(PointTable table, int idx)
        {
            if (table == null || idx < 0 || idx >= table.Points.Count) return;
            var p = table.Points[idx];
            var bridge = HardwareBridge.Current;
            for (int i = 0; i < PointTable.SlotCount; i++)
            {
                var axisName = table.AxisNames.Count > i ? table.AxisNames[i] : string.Empty;
                if (string.IsNullOrWhiteSpace(axisName)) continue;
                var axis = HardwareResolver.ResolveAxis(axisName);
                if (axis == null) { Ui(() => AddLog(LogLevel.Warn, $"找不到轴：{axisName}")); continue; }
                var slot = p.Positions.Count > i ? p.Positions[i] : null;
                if (slot == null) continue;
                try
                {
                    if (slot.Speed > 0) bridge.SetAxisSpeed(axis, slot.Speed);
                    bridge.MoveAxisAbs(axis, slot.Position);
                    bridge.WaitAxisDone(axis);
                }
                catch (Exception ex) { Ui(() => AddLog(LogLevel.Error, $"轴 {axisName} 运动异常：{ex.Message}")); }
            }
        }

        /// <summary>把动作异步封送回 UI 线程执行（后台运行循环调用）。用 BeginInvoke 而非 Invoke，
        /// 避免后台线程在 UI 线程上被同步阻塞——这是运行期界面卡顿的根因之一。高频刷新改由定时器统一处理。</summary>
        private void Ui(Action a)
        {
            var app = Application.Current;
            if (app?.Dispatcher != null && !app.Dispatcher.CheckAccess())
                app.Dispatcher.BeginInvoke(a);
            else
                a();
        }

        private void StepTo(int idx)
        {
            Ui(() =>
            {
                if (SelectedTable == null) return;
                if (idx < 0 || idx >= SelectedTable.Points.Count) return;
                var p = SelectedTable.Points[idx];
                SelectedPoint = p;
                StatusText = $"运行中：「{SelectedTable.Name}」{p.Name}（{idx + 1}/{SelectedTable.Points.Count}）";
            });
        }

        /// <summary>构建时序监控行（运行启动时调用）：按点位顺序解析预期触发时刻，未配置时序标记的点位标记为「未配置」。</summary>
        private void BuildTimingRows()
        {
            TimingRows.Clear();
            _timingClockMs = 0;
            if (SelectedTable == null) return;
            int n = 0;
            foreach (var p in SelectedTable.Points)
            {
                var ms = TimingCompiler.ParseTimingMark(p.TimingMark);
                var row = new TimingRow { Step = n + 1, PointName = p.Name };
                if (ms.HasValue)
                    row.ExpectedText = $"T+{ms.Value}ms";
                else
                    row.Status = TimingStatus.Unconfigured;
                TimingRows.Add(row);
                n++;
            }
        }

        /// <summary>记录某步实际触发时刻并比对预期：偏差超阈值时写入异常日志报警（仿真时序）。</summary>
        private void RecordTiming(int idx, PointItem p)
        {
            if (idx < 0 || idx >= TimingRows.Count) return;
            var row = TimingRows[idx];
            var ms = TimingCompiler.ParseTimingMark(p.TimingMark);
            if (!ms.HasValue)
            {
                row.Status = TimingStatus.Unconfigured;
                return;
            }

            // 实际触发时刻 = 真实运行秒表（启动/继续即计时），相对工艺起点
            double actual = _runSw.ElapsedMilliseconds;
            double dev = actual - ms.Value;
            row.ActualText = $"{actual:F1}ms";
            row.DeviationText = $"{(dev >= 0 ? "+" : "")}{dev:F1}ms";

            if (Math.Abs(dev) <= TimingThresholdMs)
                row.Status = TimingStatus.Normal;
            else if (Math.Abs(dev) <= TimingThresholdMs * 2.5)
                row.Status = TimingStatus.Deviation;
            else
            {
                row.Status = TimingStatus.OverThreshold;
                AddLog(LogLevel.Error,
                    $"时序偏差超阈：点位「{p.Name}」预期 T+{ms.Value}ms，实际 {actual:F1}ms，偏差 {dev:F1}ms（阈值 ±{TimingThresholdMs}ms）。");
            }
        }

        // ---------- 折线图重算 ----------
        private void RebuildChart()
        {
            var yieldPts = new PointCollection();
            var cyclePts = new PointCollection();
            int n = Samples.Count;
            double x0 = Pad, x1 = CW - Pad;
            double yBottom = CH - Pad, yTop = Pad;
            if (n == 0) { YieldPoints = yieldPts; CyclePoints = cyclePts; RaiseChart(); return; }

            for (int i = 0; i < n; i++)
            {
                double x = n == 1 ? (x0 + x1) / 2 : x0 + (x1 - x0) * i / (n - 1);
                double yYield = yBottom + (yTop - yBottom) * (Samples[i].Yield / 100.0);
                yieldPts.Add(new Point(x, yYield));
                double cyc = Math.Max(0, Math.Min(10, Samples[i].Cycle));
                double yCycle = yBottom + (yTop - yBottom) * (cyc / 10.0);
                cyclePts.Add(new Point(x, yCycle));
            }
            YieldPoints = yieldPts;
            CyclePoints = cyclePts;
            RaiseChart();
        }

        private void RaiseChart()
        {
            OnPropertyChanged(nameof(YieldPoints));
            OnPropertyChanged(nameof(CyclePoints));
        }

        // ---------- 运行轨迹 3D 仿真 ----------
        /// <summary>根据选中工位的点位重建 3D 场景数据（原始机械坐标 → X / Z(向上) / Y 映射为 3D 的 X/Y/Z）。</summary>
        private void RebuildSim3D()
        {
            var col = new Point3DCollection();
            var tbl = SelectedTable;
            if (tbl?.Points != null)
            {
                foreach (var p in tbl.Points)
                {
                    double x = p.Positions.Count > 0 ? p.Positions[0].Position : 0;
                    double yUp = p.Positions.Count > 2 ? p.Positions[2].Position : 0;
                    double z = p.Positions.Count > 1 ? p.Positions[1].Position : 0;
                    col.Add(new Point3D(x, yUp, z));
                }
            }
            OpSimPoints = col;
            _simPhase = 0;
            if (col.Count > 0)
            {
                OpSimHead = col[0];
                OpSimHeadVisible = true;
                OpSimIndex = -1;
            }
            else
            {
                OpSimHeadVisible = false;
                OpSimIndex = -1;
            }
        }

        /// <summary>把当前位置头移动到指定点位索引（手动单步 / 自动逐点到达时调用）。</summary>
        private void SetSimHeadToPoint(int idx)
        {
            var tbl = SelectedTable;
            if (tbl == null || idx < 0 || idx >= tbl.Points.Count) return;
            var p = tbl.Points[idx];
            double x = p.Positions.Count > 0 ? p.Positions[0].Position : 0;
            double yUp = p.Positions.Count > 2 ? p.Positions[2].Position : 0;
            double z = p.Positions.Count > 1 ? p.Positions[1].Position : 0;
            OpSimHead = new Point3D(x, yUp, z);
            OpSimHeadVisible = true;
            OpSimIndex = idx;
        }

        /// <summary>把相机抓拍到的 BGRA 帧字节组装为 WPF BitmapSource 并推到预览（在 UI 队列线程调用）。</summary>
        private void SetCapture(byte[] bgra, int w, int h)
        {
            try
            {
                if (bgra == null || w <= 0 || h <= 0) return;
                int stride = w * 4;
                var bmp = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, bgra, stride);
                bmp.Freeze();
                OpSimCapture = bmp;
            }
            catch { }
        }

        /// <summary>3D 仿真定时器（UI 线程，33ms）：运行时沿点位路径循环插值移动当前位置头，方便直观查看运行轨迹。</summary>
        private void Sim3DTick(object? sender, EventArgs e)
        {
            if (OpSimPoints == null || OpSimPoints.Count < 2) return;
            if (!IsRunning) return;
            var now = DateTime.Now;
            double dt = (now - _simLast).TotalSeconds;
            _simLast = now;
            if (dt > 0.5) dt = 0.033; // 标签页挂起等情况下的跳变保护
            double speed = 0.5;        // 点位 / 秒（循环演示速度）
            _simPhase += dt * speed;
            if (_simPhase >= OpSimPoints.Count) _simPhase -= OpSimPoints.Count;

            int i0 = (int)Math.Floor(_simPhase) % OpSimPoints.Count;
            int i1 = (i0 + 1) % OpSimPoints.Count;
            double f = _simPhase - Math.Floor(_simPhase);
            var a = OpSimPoints[i0];
            var b = OpSimPoints[i1];
            OpSimHead = new Point3D(
                a.X + (b.X - a.X) * f,
                a.Y + (b.Y - a.Y) * f,
                a.Z + (b.Z - a.Z) * f);
            OpSimIndex = i0;
        }

        // ---------- 日志 ----------
        private void AddLog(LogLevel level, string message)
        {
            // 异常日志面板只展示 警告(Warn) 与 异常(Error)；普通信息(Info)不进入面板，避免刷屏。
            if (level == LogLevel.Info) return;
            Log.Insert(0, new LogEntry { Time = DateTime.Now, Level = level, Message = message });
            while (Log.Count > 200) Log.RemoveAt(Log.Count - 1);
        }

        /// <summary>清空异常日志（按钮：清空）。清空后 CollectionChanged 触发 RefreshLogCounts 同步重置计数与空态。</summary>
        private void ClearLog()
        {
            if (Log.Count == 0) return;
            Log.Clear();
        }

        /// <summary>
        /// 导出异常日志到文件（按钮：导出）。把当前面板内的 警告/异常 写入
        /// 「&lt;程序目录&gt;/Logs/异常日志_YYYYMMDD_HHMMSS.txt」，供操作员归档/上报。
        /// 面板只含 Warn/Error（AddLog 已过滤 Info），故导出即异常报告。
        /// </summary>
        private void ExportLog()
        {
            if (Log.Count == 0) return;
            try
            {
                var dir = System.IO.Path.Combine(AppContext.BaseDirectory, "Logs");
                System.IO.Directory.CreateDirectory(dir);
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var path = System.IO.Path.Combine(dir, $"异常日志_{stamp}.txt");
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("NoCodeMotion 异常日志导出");
                sb.AppendLine($"导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"异常(Error)：{ErrorCount}　警告(Warn)：{WarnCount}");
                sb.AppendLine(new string('-', 60));
                foreach (var e in Log)
                    sb.AppendLine($"[{e.Time:HH:mm:ss}] [{LevelText(e.Level)}] {e.Message}");
                System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                StatusBarService.ReportException($"导出异常日志失败：{ex.Message}");
            }
        }

        private static string LevelText(LogLevel level) => level switch
        {
            LogLevel.Warn => "警告",
            LogLevel.Error => "异常",
            _ => "信息"
        };

        /// <summary>重算 WarnCount/ErrorCount/IsLogEmpty 并触发 INPC。Log 容量上限 200，全量重算成本可忽略。</summary>
        private void RefreshLogCounts()
        {
            int w = 0, e = 0;
            for (int i = 0; i < Log.Count; i++)
            {
                var lvl = Log[i].Level;
                if (lvl == LogLevel.Warn) w++;
                else if (lvl == LogLevel.Error) e++;
            }
            WarnCount = w;
            ErrorCount = e;
            OnPropertyChanged(nameof(WarnCount));
            OnPropertyChanged(nameof(ErrorCount));
            OnPropertyChanged(nameof(IsLogEmpty));
        }

        public void EnsureDefaultSelection()
        {
            if (SelectedTable == null && Tables.Count > 0)
                SelectedTable = Tables[0];
        }
    }

    /// <summary>折线图单点采样：时间 + 良率% + 节拍s + 累计产量。</summary>
    public class ChartSample
    {
        public DateTime Time { get; set; }
        public double Yield { get; set; }
        public double Cycle { get; set; }
        public double Count { get; set; }
    }

    /// <summary>时序偏差监控状态。</summary>
    public enum TimingStatus
    {
        Normal,        // 正常：偏差在阈值内
        Deviation,     // 偏差：超出阈值但未超 2.5 倍
        OverThreshold, // 超阈：偏差超过 2.5 倍，触发报警
        Unconfigured   // 未配置：该点位未填时序标记
    }

    /// <summary>运行时时序监控单行：步骤 / 点位 / 预期 / 实际 / 偏差 / 状态（实现 INPC 以便逐行刷新）。</summary>
    public class TimingRow : ViewModelBase
    {
        private int _step;
        private string _pointName = string.Empty;
        private string _expectedText = "—";
        private string _actualText = "—";
        private string _deviationText = "—";
        private TimingStatus _status = TimingStatus.Unconfigured;

        public int Step
        {
            get => _step;
            set => SetField(ref _step, value);
        }

        public string PointName
        {
            get => _pointName;
            set => SetField(ref _pointName, value);
        }

        /// <summary>预期触发时刻文本（如 "T+5ms"），未配置时序标记时为 "—"。</summary>
        public string ExpectedText
        {
            get => _expectedText;
            set => SetField(ref _expectedText, value);
        }

        /// <summary>实际触发时刻文本（仿真测算，如 "708.3ms"）。</summary>
        public string ActualText
        {
            get => _actualText;
            set => SetField(ref _actualText, value);
        }

        /// <summary>偏差文本（实际 − 预期，如 "+3.1ms"）。</summary>
        public string DeviationText
        {
            get => _deviationText;
            set => SetField(ref _deviationText, value);
        }

        public TimingStatus Status
        {
            get => _status;
            set
            {
                if (SetField(ref _status, value))
                    OnPropertyChanged(nameof(StatusText));
            }
        }

        /// <summary>状态中文文案（绑定显示）。</summary>
        public string StatusText => Status switch
        {
            TimingStatus.Normal => "正常",
            TimingStatus.Deviation => "偏差",
            TimingStatus.OverThreshold => "超阈",
            _ => "未配置"
        };
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
