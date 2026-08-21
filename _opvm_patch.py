# -*- coding: utf-8 -*-
p = "ViewModels/OperatorViewModel.cs"
t = open(p, "r", encoding="utf-8").read()

reps = []

reps.append((
"using System.Collections.ObjectModel;\nusing System.Windows;\nusing System.Windows.Input;\nusing System.Windows.Media;\nusing System.Windows.Threading;",
"using System.Collections.ObjectModel;\nusing System.Diagnostics;\nusing System.Threading;\nusing System.Threading.Tasks;\nusing System.Windows;\nusing System.Windows.Input;\nusing System.Windows.Media;\nusing System.Windows.Threading;"))

reps.append((
"运行逻辑在选中工位（点位表）上顺序经过各点位（纯运行状态仿真，无真实运动硬件）。",
"运行逻辑在选中工位（点位表）上顺序经过各点位，按 4 轴槽把每个轴走到目标位置+速度，经 HardwareBridge.Current 真正下发到机台（未挂载真实桥时走桩日志）。"))

reps.append((
"        private bool _isRunning;\n        private bool _eStopped;\n        private int _runIndex = -1;",
"        private bool _isRunning;\n        private bool _eStopped;\n        private bool _isPaused;\n        private int _runIndex = -1;"))

reps.append((
"        /// <summary>急停按钮可用：运行中或存在急停锁定（随时可切断）。</summary>\n        public bool CanEStop => IsRunning || EStopped;",
"        /// <summary>急停按钮可用：运行中或存在急停锁定（随时可切断）。</summary>\n        public bool CanEStop => IsRunning || EStopped;\n\n        /// <summary>是否处于暂停态（可恢复）。</summary>\n        public bool IsPaused\n        {\n            get => _isPaused;\n            set\n            {\n                if (!SetField(ref _isPaused, value)) return;\n                OnPropertyChanged(nameof(CanStart));\n                OnPropertyChanged(nameof(CanPause));\n                OnPropertyChanged(nameof(RunButtonText));\n            }\n        }\n\n        /// <summary>暂停按钮可用：仅在运行中且未暂停。</summary>\n        public bool CanPause => IsRunning && !IsPaused;\n\n        /// <summary>启动/继续按钮文字（暂停后为“继续”）。</summary>\n        public string RunButtonText => IsPaused ? \"继续\" : \"启动\";"))

reps.append((
"        /// <summary>启动按钮可用：同 CanRun（急停后需先复位）。</summary>\n        public bool CanStart => CanRun;",
"        /// <summary>启动/继续按钮可用：未运行未急停且已选工位，或已暂停（点“继续”恢复）。</summary>\n        public bool CanStart => (!IsRunning || IsPaused) && !EStopped && SelectedTable != null && (SelectedTable.Points.Count > 0);"))

reps.append((
"        public ICommand RunCommand { get; }\n        public ICommand StopCommand { get; }\n        public ICommand EStopCommand { get; }\n        public ICommand ResetCommand { get; }\n        public ICommand SwitchUserCommand { get; }\n        public ICommand ManualCommand { get; }\n        public ICommand AutoCommand { get; }",
"        public ICommand RunCommand { get; }\n        public ICommand StopCommand { get; }\n        public ICommand EStopCommand { get; }\n        public ICommand ResetCommand { get; }\n        public ICommand PauseCommand { get; }\n        public ICommand SwitchUserCommand { get; }\n        public ICommand ManualCommand { get; }\n        public ICommand AutoCommand { get; }"))

reps.append((
"        private readonly DispatcherTimer _timer;",
"        // ---------- 真实运行控制（后台线程执行，避免 WaitAxisDone 阻塞 UI）----------\n        private Thread? _runThread;\n        private volatile bool _stopRequested;\n        private volatile bool _eStopRequested;\n        private volatile bool _pauseRequested;\n        private readonly ManualResetEventSlim _resumeEvent = new(true);\n        private Stopwatch _runSw = new();"))

reps.append((
"            RunCommand = new RelayCommand(_ => Start());\n            StopCommand = new RelayCommand(_ => Stop());\n            EStopCommand = new RelayCommand(_ => EStop());\n            ResetCommand = new RelayCommand(_ => Reset());",
"            RunCommand = new RelayCommand(_ => StartOrResume());\n            StopCommand = new RelayCommand(_ => Stop());\n            EStopCommand = new RelayCommand(_ => EStop());\n            ResetCommand = new RelayCommand(_ => Reset());\n            PauseCommand = new RelayCommand(_ => Pause());"))
reps.append((
"            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };\n            _timer.Tick += OnTick;\n",
""))

old_start = '''        private void Start()
        {
            if (!CanRun) return;
            EStopped = false;
            BuildTimingRows();

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
        }'''
new_start = '''        private void Start()
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
        }'''
reps.append((old_start, new_start))

old_stop = '''        private void Stop()
        {
            if (!IsRunning) return;
            _timer.Stop();
            IsRunning = false;
            StatusText = "已停止。";
            AddLog(LogLevel.Warn, "运行已手动停止。");
        }'''
new_stop = '''        private void Stop()
        {
            if (!IsRunning && !IsPaused) return;
            _stopRequested = true;
            _resumeEvent.Set();
            IsRunning = false;
            IsPaused = false;
            StatusText = "已停止。";
            AddLog(LogLevel.Warn, "运行已手动停止。");
        }'''
reps.append((old_stop, new_stop))

old_estop = '''        private void EStop()
        {
            bool wasRunning = IsRunning;
            _timer.Stop();
            IsRunning = false;
            EStopped = true;
            _runIndex = -1;
            SelectedPoint = null;
            TimingRows.Clear();
            StatusText = "急停！所有运动已切断，请复位后重新启动。";
            AddLog(LogLevel.Error, wasRunning ? "急停触发！运行中工位已紧急切断。" : "急停触发！");
        }'''
new_estop = '''        private void EStop()
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
        }'''
reps.append((old_estop, new_estop))

old_reset = '''        private void Reset()
        {
            _timer.Stop();
            IsRunning = false;
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
        }'''
new_reset = '''        private void Reset()
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
        }'''
reps.append((old_reset, new_reset))

old_tick = '''        private void OnTick(object? sender, EventArgs e)
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
        }'''
new_tick = '''        // ---------- 启动/继续/暂停/复位 调度 ----------
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
        }'''
reps.append((old_tick, new_tick))

reps.append((
"        private void StepTo(int idx)\n        {\n            if (SelectedTable == null) return;\n            if (idx < 0 || idx >= SelectedTable.Points.Count) return;\n            var p = SelectedTable.Points[idx];\n            SelectedPoint = p;\n            StatusText = $\"运行中：「{SelectedTable.Name}」{p.Name}（{idx + 1}/{SelectedTable.Points.Count}）\";\n            RecordTiming(idx, p);\n        }",
"        private void StepTo(int idx)\n        {\n            Ui(() =>\n            {\n                if (SelectedTable == null) return;\n                if (idx < 0 || idx >= SelectedTable.Points.Count) return;\n                var p = SelectedTable.Points[idx];\n                SelectedPoint = p;\n                StatusText = $\"运行中：「{SelectedTable.Name}」{p.Name}（{idx + 1}/{SelectedTable.Points.Count}）\";\n            });\n        }"))

old_rec = '''        private void RecordTiming(int idx, PointItem p)
        {
            if (idx < 0 || idx >= TimingRows.Count) return;
            var row = TimingRows[idx];
            var ms = TimingCompiler.ParseTimingMark(p.TimingMark);
            if (!ms.HasValue)
            {
                row.Status = TimingStatus.Unconfigured;
                return;
            }

            // 实际触发时刻 = 累计仿真时钟 + ±3ms 抖动（仿真，无真实运动硬件）
            double actual = _timingClockMs + (_timingRand.NextDouble() * 6 - 3);
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

            _timingClockMs += 700.0; // 每步基准 700ms（与自动节拍一致），推进仿真时钟
        }'''
new_rec = '''        private void RecordTiming(int idx, PointItem p)
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
        }'''
reps.append((old_rec, new_rec))

ok = True
for k, (o, n) in enumerate(reps):
    c = t.count(o)
    if c != 1:
        ok = False
        print(f"[FAIL] replacement #{k} count={c}")
        print("  old starts:", repr(o[:80]))
        idx = t.find(o[:40])
        print("  first occ at", idx)
        if idx >= 0:
            print("  context:", repr(t[idx - 40:idx + 120]))
        break
    t = t.replace(o, n, 1)

if ok:
    open(p, "w", encoding="utf-8").write(t)
    print("ALL OK, written. new length", len(t))
else:
    print("ABORTED, no changes written")
