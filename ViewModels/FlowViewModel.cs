using System;
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
    /// 顶部提供两个具体添加命令：添加表格流程（Kind=Table）/ 添加脚本流程（Kind=Lua），
    /// 由 EditorPage 的 LeftToolbarContent 注入并隐藏默认「添加」按钮。
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

        /// <summary>当前执行到的步骤索引（0 基；-1 表示未开始，等于步骤数表示已完成）。</summary>
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

        /// <summary>当前步骤文案（用于底部状态显示）。</summary>
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

        /// <summary>“跳到指定行”的目标行号（1 基，绑定到底部输入框）。</summary>
        public int JumpRow
        {
            get => _jumpRow;
            set => SetField(ref _jumpRow, value);
        }

        public bool CanRun => !IsRunning && StepPanel.Items.Count > 0;
        public bool CanStep => StepPanel.Items.Count > 0 && (_currentStep < 0 || _currentStep < StepPanel.Items.Count - 1);
        public bool CanPause => IsRunning && !IsPaused;
        public bool CanStop => IsRunning || IsPaused || _currentStep >= 0;

        // ---------- 新建流程的两个具体添加命令（EditorPage 注入用）----------
        /// <summary>下次「添加」时要用的流程类型（AddTable/AddScript 命令临时改写它）。</summary>
        private FlowKind _nextAddKind = FlowKind.Table;

        /// <summary>添加表格流程（Kind=Table）。</summary>
        public ICommand AddTableFlowCommand { get; }

        /// <summary>添加 Lua 脚本流程（Kind=Lua，自动填默认脚本模板）。</summary>
        public ICommand AddScriptFlowCommand { get; }

        /// <summary>当前选中流程是表格流程（驱动右侧显示表格编辑器）。</summary>
        public bool IsKindTable => SelectedItem?.Kind == FlowKind.Table;

        /// <summary>当前选中流程是 Lua 脚本流程（驱动右侧显示 Lua 编辑器）。</summary>
        public bool IsKindLua => SelectedItem?.Kind == FlowKind.Lua;

        public ICommand RunCommand { get; }
        public ICommand StepCommand { get; }
        public ICommand JumpCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }

        public FlowViewModel()
        {
            Items = ProjectStore.Data.Flows;
            Counter = Items.Count;
            AttachAutoSave();

            // 监听每个 FlowItem.Kind 变化（用于驱动 IsKindTable/IsKindLua + 类型切换时复位执行态）
            foreach (FlowItem item in Items) item.PropertyChanged += OnFlowItemPropertyChanged;
            Items.CollectionChanged += OnFlowsCollectionChanged;

            StepPanel = new FlowStepPanel(new ObservableCollection<FlowStep>());
            StepPanel.SetItems(SelectedItem?.Steps ?? new ObservableCollection<FlowStep>());

            // 命令本身不带 can-exec 谓词（避免与下方 IsEnabled 绑定冲突）；
            // 按钮可用态完全由 CanRun/CanStep/CanPause/CanStop 属性驱动（RunState 变更时 RaiseRunState 通知），
            // 各执行方法内部仍有等价守卫，重复点击/非法点击不会出错。
            RunCommand = new RelayCommand(_ => Run());
            StepCommand = new RelayCommand(_ => StepOnce());
            JumpCommand = new RelayCommand(_ => JumpToRow());
            PauseCommand = new RelayCommand(_ => Pause());
            StopCommand = new RelayCommand(_ => Stop());

            // 顶部两个具体「添加」：先设置 Kind 再调基类 Add()，执行后复位 _nextAddKind
            AddTableFlowCommand = new RelayCommand(_ => AddNewOfKind(FlowKind.Table));
            AddScriptFlowCommand = new RelayCommand(_ => AddNewOfKind(FlowKind.Lua));

            _runTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            _runTimer.Tick += (_, _) => StepOnce();
        }

        private void OnFlowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (FlowItem item in e.NewItems) item.PropertyChanged += OnFlowItemPropertyChanged;
            if (e.OldItems != null)
                foreach (FlowItem item in e.OldItems) item.PropertyChanged -= OnFlowItemPropertyChanged;
        }

        /// <summary>监听每个 FlowItem 的属性变化：Kind 变更时刷新右侧编辑器的可见性属性 + 复位执行态。</summary>
        private void OnFlowItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FlowItem.Kind))
            {
                OnPropertyChanged(nameof(IsKindTable));
                OnPropertyChanged(nameof(IsKindLua));
                Stop(); // Lua 流程没有步骤表，运行态不再有效
            }
        }

        /// <summary>用指定 Kind 新建一个流程（由顶部两个「添加」按钮调用）。</summary>
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

        /// <summary>把当前步骤高亮到表格选中行（SelectedItem），执行完/停止时清除。</summary>
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

        /// <summary>前进一个步骤（单步按钮与运行定时器共用）。到达末尾则自动停止并标记「已完成」。</summary>
        private void StepOnce()
        {
            if (StepPanel.Items.Count == 0) { Stop(); return; }
            int next = _currentStep < 0 ? 0 : _currentStep + 1;
            if (next >= StepPanel.Items.Count)
            {
                Stop();
                _currentStep = StepPanel.Items.Count; // 标记已完成
                OnPropertyChanged(nameof(CurrentStepText));
                HighlightCurrent();
                return;
            }
            CurrentStep = next;
        }

        /// <summary>跳到指定行：按 JumpRow（1 基）设置执行位置并高亮；运行中跳转则从此处继续。</summary>
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
            // 切换流程时，步骤面板切到该流程的 Steps，并复位执行状态
            if (propertyName == nameof(SelectedItem))
            {
                StepPanel.SetItems(SelectedItem?.Steps ?? new ObservableCollection<FlowStep>());
                Stop();
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
