using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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
        private string _statusText = "请选择工位，点「启动」开始生产。";

        public ObservableCollection<PointTable> Tables { get; }

        public PointTable? SelectedTable
        {
            get => _selectedTable;
            set
            {
                if (!SetField(ref _selectedTable, value)) return;
                _runIndex = -1;
                SelectedPoint = null;
                TimingRows.Clear();
                OnPropertyChanged(nameof(CurrentPoints));
                OnPropertyChanged(nameof(CanRun));
                OnPropertyChanged(nameof(CurrentStation));
            }
        }

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
            }
        }

        /// <summary>是否允许启动：未运行、未急停、已选工位且工位至少有 1 个点位。</summary>
        public bool CanRun => !IsRunning && !EStopped && SelectedTable != null && (SelectedTable.Points.Count > 0);

        /// <summary>启动/继续按钮可用：未运行未急停且已选工位，或已暂停（点“继续”恢复）。</summary>
        public bool CanStart => (!IsRunning || IsPaused) && !EStopped && SelectedTable != null && (SelectedTable.Points.Count > 0);

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

        /// <summary>良率折线（0~100% 映射到绘图区）。</summary>
        public PointCollection YieldPoints { get; private set; } = new();

        /// <summary>节拍折线（0~10s 映射到绘图区）。</summary>
        public PointCollection CyclePoints { get; private set; } = new();

        private const double CW = 640;   // 绘图区宽
        private const double CH = 220;   // 绘图区高
        private const double Pad = 30;   // 边距

        // ---------- 异常日志 ----------
        public ObservableCollection<LogEntry> Log { get; } = new();

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

        public OperatorViewModel()
        {
            Tables = ProjectStore.Data.PointTables;
            RunCommand = new RelayCommand(_ => StartOrResume());
            StopCommand = new RelayCommand(_ => Stop());
            EStopCommand = new RelayCommand(_ => EStop());
            ResetCommand = new RelayCommand(_ => Reset());
            PauseCommand = new RelayCommand(_ => Pause());
            SwitchUserCommand = new RelayCommand(p => { if (p is string u && !string.IsNullOrEmpty(u)) CurrentUser = u; });
            ManualCommand = new RelayCommand(_ => Mode = OpMode.Manual);
            AutoCommand = new RelayCommand(_ => Mode = OpMode.Auto);


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
                Task.Run(() =>
                {
                    ExecutePoint(idx);
                    Ui(() =>
                    {
                        StepTo(idx);
                        RecordTiming(idx, table.Points[idx]);
                        AdvanceProduction();
                    });
                });
                StatusText = $"手动单步：「{table.Name}」{table.Points[idx].Name}（{idx + 1}/{table.Points.Count}）";
                AddLog(LogLevel.Info, $"手动单步 -> {table.Points[idx].Name}");
                return;
            }

            // 自动模式：后台线程顺序运行（真实驱动机台）
            _stopRequested = false;
            _eStopRequested = false;
            _pauseRequested = false;
            _resumeEvent.Set();
            IsRunning = true;
            IsPaused = false;
            _runStart = DateTime.Now;
            _runSw.Restart();
            _runIndex = 0;
            AddLog(LogLevel.Info, $"启动运行（自动）：工位「{SelectedTable!.Name}」。");
            StatusText = $"运行中：「{SelectedTable.Name}」";
            _runThread = new Thread(RunLoop) { IsBackground = true, Name = "OpRun" };
            _runThread.Start();
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
            IsRunning = false;
            IsPaused = false;
            StatusText = "已停止。";
            AddLog(LogLevel.Warn, "运行已手动停止。");
        }

        private void EStop()
        {
            bool wasRunning = IsRunning || IsPaused;
            _eStopRequested = true;
            _stopRequested = true;
            _resumeEvent.Set();
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
            _runIndex = -1;
            SelectedPoint = null;
            TimingRows.Clear();
            StatusText = "急停！所有运动已切断，请复位后重新启动。";
            AddLog(LogLevel.Error, wasRunning ? "急停触发！运行中工位已紧急切断。" : "急停触发！");
        }

        private void Reset()
        {
            _stopRequested = true;
            _resumeEvent.Set();
            _eStopRequested = false;
            _pauseRequested = false;
            IsRunning = false;
            IsPaused = false;
            EStopped = false;
            _runIndex = -1;
            SelectedPoint = null;
            TimingRows.Clear();
            TotalCount = 0;
            Yield = 99.0;
            CycleTime = 2.4;
            RunElapsedText = "00:00";
            Samples.Clear();
            RebuildChart();
            StatusText = "已复位，可重新选择工位并启动。";
            AddLog(LogLevel.Info, "系统已复位。");
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
            IsPaused = true;
            StatusText = "已暂停。";
            AddLog(LogLevel.Warn, "运行已暂停。");
        }

        private void Resume()
        {
            if (!IsPaused) return;
            _pauseRequested = false;
            _resumeEvent.Set();
            IsPaused = false;
            StatusText = "继续运行。";
            AddLog(LogLevel.Info, "继续运行。");
        }

        /// <summary>后台执行循环：依次把 4 个轴走到每个点位目标，真实驱动机台；点间响应暂停/停止/急停。</summary>
        private void RunLoop()
        {
            var table = SelectedTable;
            if (table == null) { Ui(() => IsRunning = false); return; }
            _runSw.Restart();
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
                ExecutePoint(idx);
                if (_eStopRequested || _stopRequested) break;
            }
            Ui(() =>
            {
                IsRunning = false;
                IsPaused = false;
                if (_eStopRequested)
                    StatusText = "急停！所有运动已切断，请复位后重新启动。";
                else if (_stopRequested)
                    StatusText = "已停止。";
                else
                    StatusText = $"运行完成：「{table.Name}」共 {n} 个点位，产量 {TotalCount} 件。";
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

        /// <summary>把动作封送回 UI 线程执行（后台运行循环调用）。</summary>
        private void Ui(Action a)
        {
            var app = Application.Current;
            if (app?.Dispatcher != null && !app.Dispatcher.CheckAccess())
                app.Dispatcher.Invoke(a);
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

        // ---------- 日志 ----------
        private void AddLog(LogLevel level, string message)
        {
            Log.Insert(0, new LogEntry { Time = DateTime.Now, Level = level, Message = message });
            while (Log.Count > 200) Log.RemoveAt(Log.Count - 1);
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
