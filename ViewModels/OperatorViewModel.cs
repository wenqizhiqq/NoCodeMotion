using System;
using System.Collections.ObjectModel;
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
    /// 运行逻辑在选中工位（点位表）上顺序经过各点位（纯运行状态仿真，无真实运动硬件）。</summary>
    /// <summary>运行模式：手动（单步） / 自动（连续循环）。</summary>
    public enum OpMode
    {
        Manual,
        Auto
    }

    public class OperatorViewModel : ViewModelBase, IEnsureDefaultSelection
    {
        private readonly Random _rand = new();

        // ---------- 工位（点位表）选择 + 运行进度 ----------
        private PointTable? _selectedTable;
        private PointItem? _selectedPoint;
        private bool _isRunning;
        private bool _eStopped;
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

        /// <summary>启动按钮可用：同 CanRun（急停后需先复位）。</summary>
        public bool CanStart => CanRun;

        /// <summary>停止按钮可用：仅在运行时。</summary>
        public bool CanStop => IsRunning;

        /// <summary>急停按钮可用：运行中或存在急停锁定（随时可切断）。</summary>
        public bool CanEStop => IsRunning || EStopped;

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

        private readonly DispatcherTimer _timer;

        public OperatorViewModel()
        {
            Tables = ProjectStore.Data.PointTables;
            RunCommand = new RelayCommand(_ => Start());
            StopCommand = new RelayCommand(_ => Stop());
            EStopCommand = new RelayCommand(_ => EStop());
            ResetCommand = new RelayCommand(_ => Reset());
            SwitchUserCommand = new RelayCommand(p => { if (p is string u && !string.IsNullOrEmpty(u)) CurrentUser = u; });
            ManualCommand = new RelayCommand(_ => Mode = OpMode.Manual);
            AutoCommand = new RelayCommand(_ => Mode = OpMode.Auto);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
            _timer.Tick += OnTick;

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

            // 手动模式：每按一次「启动」前进一个点位（单步，不自动循环）
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
                StepTo(_runIndex);
                AdvanceProduction();
                StatusText = $"手动单步：「{SelectedTable.Name}」{SelectedTable.Points[_runIndex].Name}（{_runIndex + 1}/{SelectedTable.Points.Count}）";
                AddLog(LogLevel.Info, $"手动单步 -> {SelectedTable.Points[_runIndex].Name}");
                return;
            }

            // 自动模式：定时器顺序连续运行
            IsRunning = true;
            _runStart = DateTime.Now;
            _runIndex = 0;
            StepTo(_runIndex);
            _timer.Start();
            AddLog(LogLevel.Info, $"启动运行（自动）：工位「{SelectedTable!.Name}」。");
            StatusText = $"运行中：「{SelectedTable.Name}」";
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
            if (!IsRunning) return;
            _timer.Stop();
            IsRunning = false;
            StatusText = "已停止。";
            AddLog(LogLevel.Warn, "运行已手动停止。");
        }

        private void EStop()
        {
            bool wasRunning = IsRunning;
            _timer.Stop();
            IsRunning = false;
            EStopped = true;
            _runIndex = -1;
            SelectedPoint = null;
            StatusText = "急停！所有运动已切断，请复位后重新启动。";
            AddLog(LogLevel.Error, wasRunning ? "急停触发！运行中工位已紧急切断。" : "急停触发！");
        }

        private void Reset()
        {
            _timer.Stop();
            IsRunning = false;
            EStopped = false;
            _runIndex = -1;
            SelectedPoint = null;
            TotalCount = 0;
            Yield = 99.0;
            CycleTime = 2.4;
            RunElapsedText = "00:00";
            Samples.Clear();
            RebuildChart();
            StatusText = "已复位，可重新选择工位并启动。";
            AddLog(LogLevel.Info, "系统已复位。");
        }

        private void OnTick(object? sender, EventArgs e)
        {
            if (SelectedTable == null) { Stop(); return; }

            // 生产数据推进 + 采样
            AdvanceProduction();

            // 运行时长
            var elapsed = DateTime.Now - _runStart;
            RunElapsedText = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";

            // 偶发异常（仿真），约 12% 节拍出现
            if (_rand.NextDouble() < 0.12)
            {
                var alarms = new[]
                {
                    "轴3 跟随误差偏大。",
                    "工位2 气缸到位超时。",
                    "IO 输入信号抖动。",
                    "视觉定位置信度偏低。",
                    "传送带负载异常。",
                };
                var msg = alarms[_rand.Next(alarms.Length)];
                AddLog(_rand.NextDouble() < 0.4 ? LogLevel.Error : LogLevel.Warn, msg);
            }

            // 工位步进
            _runIndex++;
            if (_runIndex >= SelectedTable.Points.Count)
            {
                _timer.Stop();
                IsRunning = false;
                _runIndex = SelectedTable.Points.Count - 1;
                SelectedPoint = _runIndex >= 0 ? SelectedTable.Points[_runIndex] : null;
                StatusText = $"运行完成：「{SelectedTable.Name}」共 {SelectedTable.Points.Count} 个点位，产量 {TotalCount} 件。";
                AddLog(LogLevel.Info, $"运行完成：「{SelectedTable.Name}」，累计产量 {TotalCount} 件，良率 {Yield:F1}%。");
                return;
            }
            StepTo(_runIndex);
        }

        private void StepTo(int idx)
        {
            if (SelectedTable == null) return;
            if (idx < 0 || idx >= SelectedTable.Points.Count) return;
            var p = SelectedTable.Points[idx];
            SelectedPoint = p;
            StatusText = $"运行中：「{SelectedTable.Name}」{p.Name}（{idx + 1}/{SelectedTable.Points.Count}）";
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
}
