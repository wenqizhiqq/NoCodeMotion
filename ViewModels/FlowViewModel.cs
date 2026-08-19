using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Threading;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 单个流程的步骤面板：复用通用表格面板，定制步骤行的创建与克隆逻辑。
    /// 步骤集合跟随“当前选中流程”的 Steps —— FlowViewModel 在选中流程变化时调用 SetItems 切换。
    /// </summary>
    public class FlowStepPanel : TablePanelViewModel<FlowStep>
    {
        public FlowStepPanel(ObservableCollection<FlowStep> steps) : base("步骤", steps) { }

        protected override FlowStep MakeNew(int index)
            => new FlowStep { Name = $"步骤{Items.Count + 1}" };

        protected override FlowStep Clone(FlowStep src)
        {
            var json = JsonSerializer.Serialize(src);
            var copy = JsonSerializer.Deserialize<FlowStep>(json)!;
            copy.Name = $"{copy.Name}_副本";
            return copy;
        }

        protected override void OnItemChanged(FlowStep item, string? propertyName)
            => ProjectStore.ScheduleSave();
    }

    /// <summary>
    /// 流程页 ViewModel：左侧列表管理“流程”项目（复用基类增删 + 自动保存），
    /// 右侧表格管理当前流程内的“步骤”（FlowStep）。步骤的增删/移动/复制/粘贴/回撤/重做
    /// 全部由 StepPanel（TablePanelViewModel&lt;FlowStep&gt;）统一提供，并使用通用 TableToolbar。
    /// 另含流程执行仿真控制（运行 / 单步 / 跳到指定行 / 暂停 / 停止），纯运行态，无真实运动硬件。
    /// 顶部提供两个具体添加命令：添加表格流程（Kind=Table）/ 添加脚本流程（Kind=Lua）。
    /// 对 Lua 流程额外提供：基于 MoonSharp 的调试（运行/单步/跳进/跳出/继续/暂停/停止）、
    /// 每秒语法实时检测、变量采集与点击查看、一键格式化。
    /// </summary>
    public class FlowViewModel : ListEditorViewModel<FlowItem>, IEnsureDefaultSelection
    {
        /// <summary>当前选中流程的步骤面板。FlowPage 通过它绑定工具栏与表格。</summary>
        public FlowStepPanel StepPanel { get; }

        // ---------- 流程执行仿真状态（运行态，不落盘）----------
        private readonly DispatcherTimer _runTimer;
        private bool _isRunning;
        private bool _isPaused;
        private int _currentStep = -1;
        private int _jumpRow = 1;

        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                if (!SetField(ref _currentStep, value)) return;
                HighlightCurrent();
                RaiseRunState();
            }
        }

        public string CurrentStepText => _currentStep < 0
            ? (IsRunning ? "运行中" : "未开始")
            : (_currentStep < StepPanel.Items.Count ? $"第 {_currentStep + 1} 步 / 共 {StepPanel.Items.Count} 步" : "已完成");

        public bool IsRunning
        {
            get => _isRunning;
            set { if (SetField(ref _isRunning, value)) RaiseRunState(); }
        }

        public bool IsPaused
        {
            get => _isPaused;
            set { if (SetField(ref _isPaused, value)) RaiseRunState(); }
        }

        public int JumpRow
        {
            get => _jumpRow;
            set => SetField(ref _jumpRow, value);
        }

        public bool CanRun => !IsRunning && StepPanel.Items.Count > 0;
        public bool CanStep => StepPanel.Items.Count > 0 && (_currentStep < 0 || _currentStep < StepPanel.Items.Count - 1);
        public bool CanPause => IsRunning && !IsPaused;
        public bool CanStop => IsRunning || IsPaused || _currentStep >= 0;

        // ---------- 新建流程的两个具体添加命令 ----------
        private FlowKind _nextAddKind = FlowKind.Table;

        public ICommand AddTableFlowCommand { get; }
        public ICommand AddScriptFlowCommand { get; }

        public bool IsKindTable => SelectedItem?.Kind == FlowKind.Table;
        public bool IsKindLua => SelectedItem?.Kind == FlowKind.Lua;

        public ICommand RunCommand { get; }
        public ICommand StepCommand { get; }
        public ICommand JumpCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }

        // ---------- Lua 调试状态 ----------
        private readonly LuaDebugger _luaDbg = new LuaDebugger();
        private readonly Dispatcher _uiDispatcher;
        private readonly DispatcherTimer _luaCheckTimer;
        private bool _luaIsDebugging;
        private int _luaCurrentLine;
        private int _luaErrorLine;
        private bool _luaHasError;
        private string _luaDiagnostics = "";
        private string _luaDebugStatus = "未调试";
        private string _luaInspectedVarName = "";
        private string _luaInspectedVarValue = "";
        private bool _luaHasInspected;
        private string _luaOutput = "";

        /// <summary>当前调试会话的局部变量（暂停时刷新）。</summary>
        public ObservableCollection<LuaVar> LuaVariables { get; } = new ObservableCollection<LuaVar>();

        public bool LuaIsDebugging
        {
            get => _luaIsDebugging;
            set => SetField(ref _luaIsDebugging, value);
        }
        public int LuaCurrentLine
        {
            get => _luaCurrentLine;
            set => SetField(ref _luaCurrentLine, value);
        }
        public int LuaErrorLine
        {
            get => _luaErrorLine;
            set => SetField(ref _luaErrorLine, value);
        }
        public bool LuaHasError
        {
            get => _luaHasError;
            set => SetField(ref _luaHasError, value);
        }
        public string LuaDiagnostics
        {
            get => _luaDiagnostics;
            set => SetField(ref _luaDiagnostics, value);
        }
        public string LuaDebugStatus
        {
            get => _luaDebugStatus;
            set => SetField(ref _luaDebugStatus, value);
        }
        public string LuaInspectedVarName
        {
            get => _luaInspectedVarName;
            set => SetField(ref _luaInspectedVarName, value);
        }
        public string LuaInspectedVarValue
        {
            get => _luaInspectedVarValue;
            set => SetField(ref _luaInspectedVarValue, value);
        }
        public bool LuaHasInspected
        {
            get => _luaHasInspected;
            set => SetField(ref _luaHasInspected, value);
        }
        public string LuaOutput
        {
            get => _luaOutput;
            set => SetField(ref _luaOutput, value);
        }

        // ---------- Lua 智能提示 ----------
        public ObservableCollection<string> LuaCompletions { get; } = new ObservableCollection<string>();
        private bool _luaShowCompletions;
        public bool LuaShowCompletions
        {
            get => _luaShowCompletions;
            private set { _luaShowCompletions = value; RaiseLuaCompletionProps(); }
        }
        private int _luaCompletionIndex;
        public int LuaCompletionIndex
        {
            get => _luaCompletionIndex;
            private set { _luaCompletionIndex = value; RaiseLuaCompletionProps(); }
        }
        public string LuaCompletionHeader => LuaShowCompletions
            ? $"智能提示：共 {LuaCompletions.Count} 项 · ↑↓ 选择 · Tab/Enter 补全 · Esc 关闭"
            : string.Empty;

        public bool CanLuaRun => IsKindLua && SelectedItem != null;
        public bool CanLuaStep => IsKindLua && SelectedItem != null;
        public bool CanLuaStepInto => IsKindLua && SelectedItem != null;
        public bool CanLuaStepOut => IsKindLua && SelectedItem != null;
        public bool CanLuaPause => LuaIsDebugging;
        public bool CanLuaStop => LuaIsDebugging;
        public bool CanLuaFormat => IsKindLua && SelectedItem != null;

        public ICommand LuaRunCommand { get; }
        public ICommand LuaStepCommand { get; }
        public ICommand LuaStepIntoCommand { get; }
        public ICommand LuaStepOutCommand { get; }
        public ICommand LuaPauseCommand { get; }
        public ICommand LuaStopCommand { get; }
        public ICommand LuaFormatCommand { get; }
        public ICommand LuaInspectCommand { get; }

        /// <summary>由“智能提示”按钮触发，请求代码后台弹出补全窗口。</summary>
        public event Action? RequestShowCompletion;

        public ICommand LuaHintCommand { get; }

        public FlowViewModel()
        {
            Items = ProjectStore.Data.Flows;
            Counter = Items.Count;
            AttachAutoSave();

            foreach (FlowItem item in Items) item.PropertyChanged += OnFlowItemPropertyChanged;
            Items.CollectionChanged += OnFlowsCollectionChanged;

            StepPanel = new FlowStepPanel(new ObservableCollection<FlowStep>());
            StepPanel.SetItems(SelectedItem?.Steps ?? new ObservableCollection<FlowStep>());

            RunCommand = new RelayCommand(_ => Run());
            StepCommand = new RelayCommand(_ => StepOnce());
            JumpCommand = new RelayCommand(_ => JumpToRow());
            PauseCommand = new RelayCommand(_ => Pause());
            StopCommand = new RelayCommand(_ => Stop());

            AddTableFlowCommand = new RelayCommand(_ => AddNewOfKind(FlowKind.Table));
            AddScriptFlowCommand = new RelayCommand(_ => AddNewOfKind(FlowKind.Lua));

            // Lua 调试命令
            LuaRunCommand = new RelayCommand(_ => LuaRun());
            LuaStepCommand = new RelayCommand(_ => LuaStepOver());
            LuaStepIntoCommand = new RelayCommand(_ => LuaStepInto());
            LuaStepOutCommand = new RelayCommand(_ => LuaStepOut());
            LuaPauseCommand = new RelayCommand(_ => LuaRequestBreak());
            LuaStopCommand = new RelayCommand(_ => LuaStop());
            LuaFormatCommand = new RelayCommand(_ => LuaFormat());
            LuaInspectCommand = new RelayCommand(p => LuaInspect(p as string));
            LuaHintCommand = new RelayCommand(_ => RequestShowCompletion?.Invoke(),
                _ => IsKindLua && SelectedItem != null);

            _uiDispatcher = Dispatcher.CurrentDispatcher;
            _luaDbg.Paused += OnLuaPaused;
            _luaDbg.Error += OnLuaError;
            _luaDbg.Finished += OnLuaFinished;
            _luaDbg.Output += OnLuaOutput;

            _runTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            _runTimer.Tick += (_, _) => StepOnce();

            // 每秒实时检测 Lua 语法
            _luaCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _luaCheckTimer.Tick += (_, _) => CheckLuaSyntax();
            _luaCheckTimer.Start();
        }

        private void OnFlowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (FlowItem item in e.NewItems) item.PropertyChanged += OnFlowItemPropertyChanged;
            if (e.OldItems != null)
                foreach (FlowItem item in e.OldItems) item.PropertyChanged -= OnFlowItemPropertyChanged;
        }

        private void OnFlowItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FlowItem.Kind))
            {
                OnPropertyChanged(nameof(IsKindTable));
                OnPropertyChanged(nameof(IsKindLua));
                Stop();
                ResetLuaState();
            }
        }

        private void AddNewOfKind(FlowKind kind)
        {
            _nextAddKind = kind;
            try { Add(); }
            finally { _nextAddKind = FlowKind.Table; }
        }

        private void RaiseRunState()
        {
            OnPropertyChanged(nameof(CanRun));
            OnPropertyChanged(nameof(CanStep));
            OnPropertyChanged(nameof(CanPause));
            OnPropertyChanged(nameof(CanStop));
            OnPropertyChanged(nameof(CurrentStepText));
        }

        private void RaiseLuaCan()
        {
            OnPropertyChanged(nameof(CanLuaRun));
            OnPropertyChanged(nameof(CanLuaStep));
            OnPropertyChanged(nameof(CanLuaStepInto));
            OnPropertyChanged(nameof(CanLuaStepOut));
            OnPropertyChanged(nameof(CanLuaPause));
            OnPropertyChanged(nameof(CanLuaStop));
            OnPropertyChanged(nameof(CanLuaFormat));
        }

        private void HighlightCurrent()
        {
            if (_currentStep >= 0 && _currentStep < StepPanel.Items.Count)
                StepPanel.SelectedItem = StepPanel.Items[_currentStep];
            else if (!IsRunning)
                StepPanel.SelectedItem = null;
        }

        private void Run()
        {
            if (!CanRun) return;
            if (_currentStep < 0 || _currentStep >= StepPanel.Items.Count) CurrentStep = 0;
            IsPaused = false;
            IsRunning = true;
            _runTimer.Start();
        }

        private void StepOnce()
        {
            if (StepPanel.Items.Count == 0) { Stop(); return; }
            int next = _currentStep < 0 ? 0 : _currentStep + 1;
            if (next >= StepPanel.Items.Count)
            {
                Stop();
                _currentStep = StepPanel.Items.Count;
                OnPropertyChanged(nameof(CurrentStepText));
                HighlightCurrent();
                return;
            }
            CurrentStep = next;
        }

        private void JumpToRow()
        {
            if (StepPanel.Items.Count == 0) return;
            int idx = _jumpRow - 1;
            if (idx < 0) idx = 0;
            if (idx >= StepPanel.Items.Count) idx = StepPanel.Items.Count - 1;
            CurrentStep = idx;
        }

        private void Pause()
        {
            if (!CanPause) return;
            _runTimer.Stop();
            IsPaused = true;
        }

        private void Stop()
        {
            _runTimer.Stop();
            IsRunning = false;
            IsPaused = false;
            _currentStep = -1;
            OnPropertyChanged(nameof(CurrentStepText));
            HighlightCurrent();
            RaiseRunState();
        }

        // ---------- Lua 调试控制 ----------

        private void LuaRun()
        {
            if (SelectedItem == null || SelectedItem.Kind != FlowKind.Lua) return;
            if (_luaDbg.IsDebugging) { _luaDbg.Continue(); return; }
            StartLua(false);
        }

        private void LuaStepOver()
        {
            if (SelectedItem == null || SelectedItem.Kind != FlowKind.Lua) return;
            if (_luaDbg.IsDebugging) { _luaDbg.StepOver(); return; }
            StartLua(true);
        }

        private void LuaStepInto()
        {
            if (SelectedItem == null || SelectedItem.Kind != FlowKind.Lua) return;
            if (_luaDbg.IsDebugging) { _luaDbg.StepInto(); return; }
            StartLua(true);
        }

        private void LuaStepOut()
        {
            if (SelectedItem == null || SelectedItem.Kind != FlowKind.Lua) return;
            if (_luaDbg.IsDebugging) { _luaDbg.StepOut(); return; }
            StartLua(true);
        }

        private void LuaRequestBreak()
        {
            if (_luaDbg.IsDebugging) _luaDbg.RequestBreak();
        }

        private void LuaStop()
        {
            _luaDbg.Stop();
            ResetLuaState();
        }

        private void StartLua(bool step)
        {
            if (SelectedItem == null || SelectedItem.Kind != FlowKind.Lua) return;
            LuaVariables.Clear();
            LuaInspectedVarName = "";
            LuaInspectedVarValue = "";
            LuaHasInspected = false;
            LuaOutput = "";
            LuaHasError = false;
            LuaErrorLine = 0;
            LuaIsDebugging = true;
            LuaDebugStatus = step ? "已暂停于第 1 行" : "运行中";
            _luaDbg.Start(SelectedItem.LuaSource, step);
            RaiseLuaCan();
        }

        private void LuaInspect(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            LuaInspectedVarName = name;
            LuaInspectedVarValue = _luaDbg.Inspect(name);
            LuaHasInspected = true;
        }

        private void LuaFormat()
        {
            if (SelectedItem == null || SelectedItem.Kind != FlowKind.Lua) return;
            string formatted = LuaDebugger.FormatLua(SelectedItem.LuaSource);
            if (formatted != SelectedItem.LuaSource)
            {
                SelectedItem.LuaSource = formatted;
                LuaDiagnostics = "已格式化（缩进已调整）";
                LuaHasError = false;
                LuaErrorLine = 0;
            }
        }

        /// <summary>根据编辑器当前文本与光标位置刷新智能提示候选项（由 FlowPage 的 TextBox 事件驱动）。</summary>
        public void UpdateLuaCompletions(string? source, int caret)
        {
            if (!IsKindLua || SelectedItem == null) { LuaCloseCompletions(); return; }
            if (LuaDebugger.TryGetCompletions(source ?? "", caret, out var items, out _))
            {
                LuaCompletions.Clear();
                foreach (var it in items) LuaCompletions.Add(it);
                _luaCompletionIndex = 0;
                _luaShowCompletions = true;
            }
            else
            {
                _luaShowCompletions = false;
            }
            RaiseLuaCompletionProps();
        }

        public void LuaCompletionMove(int delta)
        {
            if (!_luaShowCompletions || LuaCompletions.Count == 0) return;
            int n = LuaCompletions.Count;
            _luaCompletionIndex = (_luaCompletionIndex + delta) % n;
            if (_luaCompletionIndex < 0) _luaCompletionIndex += n;
            RaiseLuaCompletionProps();
        }

        public string? LuaCompletionCurrent =>
            (_luaShowCompletions && _luaCompletionIndex >= 0 && _luaCompletionIndex < LuaCompletions.Count)
                ? LuaCompletions[_luaCompletionIndex] : null;

        public void LuaCloseCompletions()
        {
            _luaShowCompletions = false;
            RaiseLuaCompletionProps();
        }

        private void RaiseLuaCompletionProps()
        {
            OnPropertyChanged(nameof(LuaShowCompletions));
            OnPropertyChanged(nameof(LuaCompletionIndex));
            OnPropertyChanged(nameof(LuaCompletionHeader));
        }

        /// <summary>每秒检测当前 Lua 脚本语法，错误则显示行号与信息。</summary>
        private void CheckLuaSyntax()
        {
            if (SelectedItem == null || SelectedItem.Kind != FlowKind.Lua)
            {
                LuaHasError = false; LuaErrorLine = 0; LuaDiagnostics = "";
                return;
            }
            var (ok, line, msg) = LuaDebugger.CheckSyntax(SelectedItem.LuaSource);
            if (ok)
            {
                LuaHasError = false;
                LuaErrorLine = 0;
                if (!LuaIsDebugging) LuaDiagnostics = "语法正确";
            }
            else
            {
                LuaHasError = true;
                LuaErrorLine = line;
                LuaDiagnostics = msg;
            }
        }

        private void OnLuaPaused(int line, IList<LuaVar> vars)
        {
            if (_uiDispatcher != null)
                _uiDispatcher.BeginInvoke(new Action(() =>
                {
                    LuaCurrentLine = line;
                    LuaVariables.Clear();
                    foreach (var v in vars) LuaVariables.Add(v);
                    LuaDebugStatus = $"已暂停于第 {line} 行";
                    LuaIsDebugging = true;
                    RaiseLuaCan();
                }));
        }

        private void OnLuaError(int line, string msg)
        {
            if (_uiDispatcher != null)
                _uiDispatcher.BeginInvoke(new Action(() =>
                {
                    LuaErrorLine = line;
                    LuaHasError = true;
                    LuaDiagnostics = msg;
                    LuaDebugStatus = line > 0 ? $"错误于第 {line} 行" : "运行错误";
                    LuaIsDebugging = false;
                    RaiseLuaCan();
                }));
        }

        private void OnLuaFinished()
        {
            if (_uiDispatcher != null)
                _uiDispatcher.BeginInvoke(new Action(() =>
                {
                    LuaDebugStatus = "已执行完成";
                    LuaIsDebugging = false;
                    LuaVariables.Clear();
                    LuaCurrentLine = 0;
                    RaiseLuaCan();
                }));
        }

        private void OnLuaOutput(string s)
        {
            if (_uiDispatcher != null)
                _uiDispatcher.BeginInvoke(new Action(() => { LuaOutput += s + "\n"; }));
        }

        private void ResetLuaState()
        {
            _luaDbg.Stop();
            LuaIsDebugging = false;
            LuaCurrentLine = 0;
            LuaVariables.Clear();
            LuaInspectedVarName = "";
            LuaInspectedVarValue = "";
            LuaHasInspected = false;
            LuaOutput = "";
            LuaHasError = false;
            LuaErrorLine = 0;
            LuaDiagnostics = "";
            LuaDebugStatus = "未调试";
            LuaCloseCompletions();
            RaiseLuaCan();
        }

        protected override FlowItem CreateNewItem()
        {
            var kind = _nextAddKind;
            int idx = Items.Count(i => i.Kind == kind) + 1;
            string prefix = kind == FlowKind.Table ? "表格流程" : "脚本流程";
            return new FlowItem
            {
                Name = $"{prefix}{idx}",
                Kind = kind,
            };
        }

        protected override void OnPropertyChanged(string? propertyName)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName == nameof(SelectedItem))
            {
                StepPanel.SetItems(SelectedItem?.Steps ?? new ObservableCollection<FlowStep>());
                Stop();
                ResetLuaState();
                OnPropertyChanged(nameof(IsKindTable));
                OnPropertyChanged(nameof(IsKindLua));
            }
        }

        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0)
                SelectedItem = Items[0];
            if (StepPanel.SelectedItem == null && StepPanel.Items.Count > 0)
                StepPanel.SelectedItem = StepPanel.Items[0];
        }
    }
}
