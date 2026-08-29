// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Threading;
using System.Globalization;
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
    /// 顶部提供两个具体添加命令：添加运控流程（Kind=Table）/ 添加脚本流程（Kind=Lua）。
    /// Lua 脚本的编辑、调试与智能提示由 <see cref="Views.LuaEditorView"/> 承载（直接复用 LuaStudio 代码）。
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

        // 「实际值」列 1 秒定时刷新：不管是否在运行，每秒把当前选中流程里
        // Function=="变量" 的步骤 ActualValue 回填为变量当前值，方便用户实时看到变量读数变化。
        private readonly DispatcherTimer _actualValueRefreshTimer;

        // 控制流运行态（与 _currentStep 解耦，避免高亮滞后一行）
        private const int TickIntervalMs = 1000; // 与 _runTimer.Interval 对齐，单步/运行每步默认耗时（1 秒刷新）
        private int _pendingNext = -1;          // 下一拍要执行的行索引
        private readonly Stack<bool> _ifStack = new();     // 每个「如果」块是否已有分支命中
        private readonly Stack<LoopFrame> _loopStack = new();
        private sealed class LoopFrame
        {
            public int Start;
            public int End;
            public int Remaining;
        }

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

        public bool CanRun => !IsRunning && StepPanel.Items.Count > 0;
        public bool CanStep => !IsRunning && StepPanel.Items.Count > 0;
        public bool CanJump => !IsRunning && StepPanel.SelectedItem != null;
        public bool CanPause => IsRunning && !IsPaused;
        public bool CanStop => IsRunning || IsPaused || _currentStep >= 0;

        // ---------- 新建流程的三个具体添加命令 ----------
        private FlowKind _nextAddKind = FlowKind.Table;

        public ICommand AddTableFlowCommand { get; }
        public ICommand AddScriptFlowCommand { get; }
        /// <summary>添加视觉流程（Kind=Vision）：相机器视觉 / 模板匹配 等图形节点编辑流（编辑区暂为占位）。</summary>
        public ICommand AddVisionFlowCommand { get; }

        public bool IsKindTable => SelectedItem?.Kind == FlowKind.Table;
        public bool IsKindLua => SelectedItem?.Kind == FlowKind.Lua;
        public bool IsKindVision => SelectedItem?.Kind == FlowKind.Vision;

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

            foreach (FlowItem item in Items) item.PropertyChanged += OnFlowItemPropertyChanged;
            Items.CollectionChanged += OnFlowsCollectionChanged;

            StepPanel = new FlowStepPanel(new ObservableCollection<FlowStep>());
            StepPanel.SetItems(SelectedItem?.Steps ?? new ObservableCollection<FlowStep>());

            RunCommand = new RelayCommand(_ => Run());
            StepCommand = new RelayCommand(_ => StepOnce());
            JumpCommand = new RelayCommand(_ => JumpToRow(), _ => CanJump);
            PauseCommand = new RelayCommand(_ => Pause());
            StopCommand = new RelayCommand(_ => Stop());

            AddTableFlowCommand = new RelayCommand(_ => OpenCreateDialog(FlowKind.Table));
            AddScriptFlowCommand = new RelayCommand(_ => OpenCreateDialog(FlowKind.Lua));
            AddVisionFlowCommand = new RelayCommand(_ => OpenCreateDialog(FlowKind.Vision));

            _runTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            _runTimer.Tick += (_, _) => StepOnce();

            // 「实际值」列 1 秒刷新：遍历当前选中流程的步骤，对 Function=="变量" 的步骤
            // 重新调用 GetVariableValue 写回 ActualValue；其他步骤 ActualValue 不动。
            // Timer 与流程选择/运行状态解耦，构造后即开始，迭代的 StepPanel.Items
            // 会在 SelectedItem 变化时随之切换（SetItems 时已经把 Items 换成新流程的 Steps）。
            _actualValueRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            _actualValueRefreshTimer.Tick += (_, _) => RefreshActualValues();
            _actualValueRefreshTimer.Start();
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
                OnPropertyChanged(nameof(IsKindVision));
                Stop();
            }
        }

        private void AddNewOfKind(FlowKind kind)
        {
            _nextAddKind = kind;
            try { Add(); }
            finally { _nextAddKind = FlowKind.Table; }
        }

        /// <summary>
        /// 弹窗式新建流程：输入名称 + 勾选默认步骤 → 创建 FlowItem 并填充步骤。
        /// 三个「添加运控/脚本/视觉」按钮共用。
        /// </summary>
        private void OpenCreateDialog(FlowKind kind)
        {
            var templates = GetTemplates(kind);
            // 按同类 Kind 编号：弹窗默认名带序号，避开与已有流程重名
            int idx = Items.Count(i => i.Kind == kind) + 1;
            string defaultName = $"新流程{idx}";
            var dlg = new Views.FlowCreateDialog(kind, templates,
                t => GetTemplateSteps(kind, t).Select(d => d.Label),   // 预览显示步骤描述（Label），不是对象名
                defaultName)
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };
            if (dlg.ShowDialog() != true) return;

            _nextAddKind = kind;
            try
            {
                Add();   // 创建 FlowItem（CreateNewItem 用 _nextAddKind 决定 Kind）
                if (SelectedItem != null)
                {
                    SelectedItem.Name = dlg.FlowName;
                    AddTemplateSteps(SelectedItem, kind, dlg.SelectedTemplate);
                    // 脚本流程：Lua 源码随所选模板变化（通讯/分拣/MES/文件处理 各有示例脚本）
                    if (kind == FlowKind.Lua)
                        SelectedItem.LuaSource = Services.LuaTemplates.Get(dlg.SelectedTemplate);
                }
            }
            finally { _nextAddKind = FlowKind.Table; }
        }

        /// <summary>每个流程类型可选用的模板名（弹窗里单选）。</summary>
        private static List<string> GetTemplates(FlowKind kind) => kind switch
        {
            // 第一个固定"空项目"——只建空流程，不加任何默认步骤
            FlowKind.Table => new() { "空项目", "点胶机", "XYZ", "探针台", "平移机" },
            FlowKind.Lua   => new() { "空项目", "通讯", "分拣", "MES", "文件处理" },
            FlowKind.Vision => new() { "空项目", "缺陷检测", "测量", "对位", "标定" },
            _ => new() { "空项目" }
        };

        /// <summary>模板预设的一步（运控步骤会带可执行参数；视觉步骤只用 Name 当 StepType）。</summary>
        private sealed class StepDef
        {
            public string Name = "";           // 对象名：轴名 / IO点 / 气缸名 / 通讯名（表格「名称」列）
            public string Label = "";          // 步骤描述：仅用于弹窗预览（Human readable）
            public string Function = "轴";     // 轴 / IO / 气缸 / modbus
            public string Property = "";       // 属性：位置 / 输出状态 / 电磁阀 / 寄存器值（随 Function 枚举）
            public string Operation = "";      // HomeAxis / MoveAxisAbs / WriteOutput / CylinderMove ...
            public string SetValue = "";
            public string Timeout = "3000";
            public StepDef(string name, string function, string property, string operation,
                string setValue, string timeout = "3000", string label = "")
            {
                Name = name; Function = function; Property = property;
                Operation = operation; SetValue = setValue; Timeout = timeout;
                Label = string.IsNullOrEmpty(label) ? $"{function}·{name}·{operation}" : label;
            }
        }

        /// <summary>
        /// 每个模板对应的预设步骤序列——选中不同模板，生成到流程里的步骤内容随之变化。
        /// 运控（Table）步骤带完整可执行参数（Function/Property/Operation/SetValue/Timeout），
        /// 直接就是能跑的示例步骤；视觉步骤用 Name 作为 StepType。
        /// </summary>
        private static List<StepDef> GetTemplateSteps(FlowKind kind, string template)
        {
            if (template == "空项目") return new List<StepDef>();   // 不生成任何步骤
            return kind switch
            {
            FlowKind.Table => template switch
            {
                "点胶机" => new()
                {
                    new StepDef("X", "轴", "已回零", "HomeAxis", "", "10000"),
                    new StepDef("X", "轴", "位置", "MoveAxisAbs", "100"),
                    new StepDef("Z", "轴", "位置", "MoveAxisAbs", "50"),
                    new StepDef("Y0", "IO", "输出状态", "WriteOutput", "1"),
                    new StepDef("Y0", "IO", "脉冲状态", "Wait", "500"),
                    new StepDef("Y0", "IO", "输出状态", "WriteOutput", "0"),
                    new StepDef("Z", "轴", "位置", "MoveAxisAbs", "0"),
                },
                "XYZ" => new()
                {
                    new StepDef("X", "轴", "已回零", "HomeAxis", "", "10000"),
                    new StepDef("X", "轴", "位置", "MoveAxisAbs", "100"),
                    new StepDef("Y", "轴", "位置", "MoveAxisAbs", "100"),
                    new StepDef("Z", "轴", "位置", "MoveAxisAbs", "50"),
                    new StepDef("X", "轴", "已回零", "WaitAxisStop", ""),
                },
                "探针台" => new()
                {
                    new StepDef("Z", "轴", "已回零", "HomeAxis", "", "10000"),
                    new StepDef("Z", "轴", "位置", "MoveAxisAbs", "30"),
                    new StepDef("X0", "IO", "输入状态", "ReadInput", ""),
                    new StepDef("通讯1", "modbus", "寄存器值", "CommSend", "read"),
                    new StepDef("Z", "轴", "位置", "MoveAxisAbs", "0"),
                },
                "平移机" => new()
                {
                    new StepDef("X", "轴", "已回零", "HomeAxis", "", "10000"),
                    new StepDef("X", "轴", "位置", "MoveAxisAbs", "0"),
                    new StepDef("吸盘", "气缸", "电磁阀", "CylinderMove", ""),
                    new StepDef("X", "轴", "位置", "MoveAxisAbs", "200"),
                    new StepDef("吸盘", "气缸", "电磁阀", "CylinderReset", ""),
                },
                _ => new()
            },
            FlowKind.Lua => template switch
            {
                "通讯" => new()
                {
                    new StepDef("通讯1", "modbus", "寄存器值", "CommSend", "init"),
                    new StepDef("通讯1", "modbus", "寄存器值", "CommSend", "send"),
                    new StepDef("通讯1", "modbus", "寄存器值", "CommSend", "recv"),
                    new StepDef("通讯1", "modbus", "寄存器值", "CommSend", "close"),
                },
                "分拣" => new()
                {
                    new StepDef("X0", "IO", "输入状态", "ReadInput", ""),
                    new StepDef("Y1", "IO", "输出状态", "WriteOutput", "1"),
                    new StepDef("分拣缸", "气缸", "电磁阀", "CylinderMove", ""),
                    new StepDef("Y2", "IO", "输出状态", "WriteOutput", "1"),
                },
                "MES" => new()
                {
                    new StepDef("通讯1", "modbus", "寄存器值", "CommSend", "connect"),
                    new StepDef("通讯1", "modbus", "寄存器值", "CommSend", "upload"),
                    new StepDef("通讯1", "modbus", "寄存器值", "CommSend", "recv"),
                    new StepDef("通讯1", "modbus", "寄存器值", "CommSend", "ack"),
                },
                "文件处理" => new()
                {
                    new StepDef("通讯1", "modbus", "寄存器值", "CommSend", "read"),
                    new StepDef("通讯1", "modbus", "寄存器值", "CommSend", "parse"),
                    new StepDef("通讯1", "modbus", "寄存器值", "CommSend", "write"),
                    new StepDef("通讯1", "modbus", "寄存器值", "CommSend", "log"),
                },
                _ => new()
            },
            FlowKind.Vision => template switch
            {
                "缺陷检测" => new()
                {
                    new StepDef("图像采集", "", "", "", ""),
                    new StepDef("图像预处理", "", "", "", ""),
                    new StepDef("缺陷检测", "", "", "", ""),
                    new StepDef("通讯", "", "", "", ""),
                },
                "测量" => new()
                {
                    new StepDef("图像采集", "", "", "", ""),
                    new StepDef("图像预处理", "", "", "", ""),
                    new StepDef("模板匹配", "", "", "", ""),
                    new StepDef("测量", "", "", "", ""),
                    new StepDef("通讯", "", "", "", ""),
                },
                "对位" => new()
                {
                    new StepDef("图像采集", "", "", "", ""),
                    new StepDef("图像预处理", "", "", "", ""),
                    new StepDef("模板匹配", "", "", "", ""),
                    new StepDef("对位", "", "", "", ""),
                    new StepDef("通讯", "", "", "", ""),
                },
                "标定" => new()
                {
                    new StepDef("图像采集", "", "", "", ""),
                    new StepDef("标定", "", "", "", ""),
                    new StepDef("通讯", "", "", "", ""),
                },
                _ => new()
            },
            _ => new()
        };
    }

        /// <summary>把模板预设的步骤序列加到新流程（视觉走 VisualSteps，运控/脚本走 Steps 并填参数）。</summary>
        private static void AddTemplateSteps(FlowItem item, FlowKind kind, string template)
        {
            if (item == null) return;
            var defs = GetTemplateSteps(kind, template);
            if (kind == FlowKind.Vision)
            {
                // 视觉步骤以 Name 作为 StepType，右侧参数卡据此切换
                foreach (var d in defs)
                    item.VisualSteps.Add(new VisualFlowStep { Name = d.Name, StepType = d.Name, Enabled = true });
            }
            else
            {
                // 运控/脚本步骤：带上模板预设的可执行参数，落盘后即可直接运行
                foreach (var d in defs)
                    item.Steps.Add(new FlowStep
                    {
                        Name = d.Name,
                        Function = d.Function,
                        Property = d.Property,
                        Operation = d.Operation,
                        SetValue = d.SetValue,
                        Timeout = d.Timeout
                    });
            }
        }

        private void RaiseRunState()
        {
            OnPropertyChanged(nameof(CanRun));
            OnPropertyChanged(nameof(CanStep));
            OnPropertyChanged(nameof(CanJump));
            OnPropertyChanged(nameof(CanPause));
            OnPropertyChanged(nameof(CanStop));
            OnPropertyChanged(nameof(CurrentStepText));
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
            bool fresh = (_currentStep < 0 || _currentStep >= StepPanel.Items.Count);
            if (fresh)
            {
                _pendingNext = -1;
                _ifStack.Clear();
                _loopStack.Clear();
                ClearCurrentFlags();
                CurrentStep = 0;
            }
            IsPaused = false;
            IsRunning = true;
            _runTimer.Start();
        }

        private void StepOnce()
        {
            var items = StepPanel.Items;
            if (items.Count == 0) { Stop(); return; }
            // 未开始或已走完时从头开始：保证「单步运行」走完流程后按钮不卡死，可重新点击从头走
            if (!IsRunning && (_currentStep < 0 || _currentStep >= items.Count))
            {
                _pendingNext = -1;
                _ifStack.Clear();
                _loopStack.Clear();
                ClearCurrentFlags();
            }
            int i = _pendingNext >= 0 ? _pendingNext : (_currentStep < 0 || _currentStep >= items.Count ? 0 : _currentStep);
            if (i >= items.Count) { FinishRun(); return; }
            var step = items[i];
            // Trim 防御：流程数据若从文件加载带有不可见字符/空格（如 "如果 "），switch 会全部落 default → 线性逐行不跳转。
            // 进 switch 前统一去空白，确保 "如果"/"就"/"否则" 等能精确命中分支。
            string logic = (step.Logic ?? string.Empty).Trim();
            int dur = (logic == "延时" || logic == "等待")
                ? (step.DurationMs > 0 ? step.DurationMs : TickIntervalMs)
                : TickIntervalMs;
            step.DurationMs = dur;

            // 实际值回填：功能=变量时，把变量当前值写回 ActualValue（让「实际值」列显示真实测量值，而不是停留在手动输入的占位）
            // 当 Operation="修改" 时，先执行赋值：把变量值改为「设置值」列里的值，再回填 ActualValue 显示新值。
            if (step.Function == "变量" && !string.IsNullOrWhiteSpace(step.Name))
            {
                if (step.Operation == "修改")
                    SetVariableValue(step.Name, step.SetValue);
                step.ActualValue = GetVariableValue(step.Name);
            }

            // 真实硬件联动：功能为设备类且本行不是纯控制行时，把动作下发到机台（未挂真实桥走桩日志）
            if ((step.Function == "轴" || step.Function == "IO" || step.Function == "气缸" || step.Function == "modbus" || step.Function == "点位")
                && logic != "如果" && logic != "否则如果" && logic != "否则" && logic != "结束" && logic != "循环开始" && logic != "循环结束")
            {
                ExecuteHardwareStep(step);
            }

            int next;
            switch (logic)
            {
                case "如果":
                {
                    bool r = EvalCondition(step);
                    int j = MergeCompound(items, i, ref r);
                    _ifStack.Push(r);
                    if (r)
                    {
                        next = j;
                        if (next < items.Count && ((items[next].Logic ?? string.Empty).Trim() == "否则" || (items[next].Logic ?? string.Empty).Trim() == "否则如果"))
                            next = FindEnd(items, next);
                    }
                    else
                    {
                        next = FindElse(items, i);
                    }
                    break;
                }
                case "否则如果":
                {
                    if (_ifStack.Count > 0 && _ifStack.Peek())
                    {
                        next = FindEnd(items, i);
                    }
                    else
                    {
                        bool r = EvalCondition(step);
                        int j = MergeCompound(items, i, ref r);
                        if (r && _ifStack.Count > 0) { _ifStack.Pop(); _ifStack.Push(true); }
                        if (r)
                        {
                            next = j;
                            if (next < items.Count && ((items[next].Logic ?? string.Empty).Trim() == "否则" || (items[next].Logic ?? string.Empty).Trim() == "否则如果"))
                                next = FindEnd(items, next);
                        }
                        else
                        {
                            next = FindElse(items, i);
                        }
                    }
                    break;
                }
                case "否则":
                {
                    next = (_ifStack.Count > 0 && _ifStack.Peek()) ? FindEnd(items, i) : i + 1;
                    break;
                }
                case "结束":
                {
                    if (_ifStack.Count > 0) { _ifStack.Pop(); next = i + 1; }
                    else { FinishRun(); return; }
                    break;
                }
                case "循环开始":
                {
                    var ex = (_loopStack.Count > 0 && _loopStack.Peek().Start == i) ? _loopStack.Peek() : null;
                    if (ex == null)
                    {
                        int cnt = ParseLoopCount(step.SetValue);
                        int end = FindLoopEnd(items, i);
                        if (cnt <= 0 || end >= items.Count) next = end + 1;
                        else { _loopStack.Push(new LoopFrame { Start = i, End = end, Remaining = cnt }); next = i + 1; }
                    }
                    else
                    {
                        next = i + 1;
                    }
                    break;
                }
                case "循环结束":
                {
                    if (_loopStack.Count > 0)
                    {
                        var f = _loopStack.Peek();
                        f.Remaining--;
                        if (f.Remaining > 0) next = f.Start;
                        else { next = i + 1; _loopStack.Pop(); }
                    }
                    else
                    {
                        next = i + 1;
                    }
                    break;
                }
                default:
                {
                    next = i + 1;
                    if (next < items.Count && ((items[next].Logic ?? string.Empty).Trim() == "否则" || (items[next].Logic ?? string.Empty).Trim() == "否则如果"))
                        next = FindEnd(items, next);
                    break;
                }
            }

            if (next >= items.Count) { FinishRun(); return; }
            ClearCurrentFlags();
            step.IsCurrent = true;
            _pendingNext = next;
            CurrentStep = i;
        }

        private void ClearCurrentFlags()
        {
            foreach (var s in StepPanel.Items)
                if (s.IsCurrent) s.IsCurrent = false;
        }


        private static double ParseNum(string s, double def)
        {
            if (string.IsNullOrWhiteSpace(s)) return def;
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : def;
        }

        /// <summary>把流程里“设备类”步骤真实下发到机台：轴 / IO / 气缸 / modbus / 点位。</summary>
        private void ExecuteHardwareStep(FlowStep step)
        {
            var bridge = HardwareBridge.Current;
            try
            {
                switch (step.Function)
                {
                    case "轴":
                    {
                        var axis = HardwareResolver.ResolveAxis(step.Name);
                        if (axis == null) { bridge.Log($"找不到轴：{step.Name}"); break; }
                        string prop = (step.Property ?? string.Empty).Trim();
                        double val = ParseNum(step.SetValue, 0);
                        if (prop == "速度" || prop == "Speed") bridge.SetAxisSpeed(axis, val);
                        else if (prop == "回零" || prop == "Home" || prop == "原点") bridge.HomeAxis(axis);
                        else if (prop == "停止") bridge.StopAxis(axis);
                        else if (prop == "使能") bridge.EnableAxis(axis);
                        else if (!string.IsNullOrWhiteSpace(step.SetValue)) bridge.MoveAxisAbs(axis, val);
                        else bridge.MoveAxis(axis);
                        break;
                    }
                    case "IO":
                    {
                        var io = HardwareResolver.ResolveOutput(step.Name) ?? HardwareResolver.ResolveInput(step.Name);
                        if (io == null) { bridge.Log($"找不到 IO：{step.Name}"); break; }
                        if (HardwareResolver.ResolveOutput(step.Name) != null)
                            bridge.WriteOutput(io, ParseNum(step.SetValue, 0) >= 0.5 ? 1 : 0);
                        else
                            bridge.ReadInput(io);
                        break;
                    }
                    case "气缸":
                    {
                        var cyl = HardwareResolver.ResolveCylinder(step.Name);
                        if (cyl == null) { bridge.Log($"找不到气缸：{step.Name}"); break; }
                        string prop = (step.Property ?? string.Empty).Trim();
                        if (prop == "复位") { bridge.CylinderReset(cyl); break; }
                        int state = ParseNum(step.SetValue, 1) >= 0.5 ? 1 : 0;
                        if (prop == "缩回" || step.SetValue == "0") state = 0;
                        bridge.CylinderMove(cyl, state);
                        break;
                    }
                    case "modbus":
                    {
                        var comm = HardwareResolver.ResolveComm(step.Name);
                        if (comm == null) { bridge.Log($"找不到通讯：{step.Name}"); break; }
                        bridge.CommSend(comm, step.SetValue ?? string.Empty);
                        break;
                    }
                    case "点位":
                    {
                        var table = HardwareResolver.ResolvePointTable(step.Name);
                        if (table == null) { bridge.Log($"找不到点位表：{step.Name}"); break; }
                        foreach (var p in table.Points)
                        {
                            for (int i = 0; i < PointTable.SlotCount; i++)
                            {
                                var an = table.AxisNames.Count > i ? table.AxisNames[i] : string.Empty;
                                if (string.IsNullOrWhiteSpace(an)) continue;
                                var axis = HardwareResolver.ResolveAxis(an);
                                if (axis == null) continue;
                                var slot = p.Positions.Count > i ? p.Positions[i] : null;
                                if (slot == null) continue;
                                if (slot.Speed > 0) bridge.SetAxisSpeed(axis, slot.Speed);
                                bridge.MoveAxisAbs(axis, slot.Position);
                            }
                        }
                        break;
                    }
                }
            }
            catch (Exception ex) { bridge.Log($"硬件下发异常（{step.Name}）：{ex.Message}"); }
        }

        private void FinishRun()
        {
            _runTimer.Stop();
            IsRunning = false;
            IsPaused = false;
            _pendingNext = -1;
            ClearCurrentFlags();
            _currentStep = StepPanel.Items.Count; // 标记已完成（CurrentStepText 显示“已完成”）
            OnPropertyChanged(nameof(CurrentStepText));
            HighlightCurrent();
            RaiseRunState();
        }

        private int MergeCompound(IReadOnlyList<FlowStep> items, int i, ref bool r)
        {
            int k = i + 1;
            while (k < items.Count)
            {
                var lg = (items[k].Logic ?? string.Empty).Trim();
                if (lg == "并且") { r = r && EvalCondition(items[k]); k++; }
                else if (lg == "或者") { r = r || EvalCondition(items[k]); k++; }
                else break;
            }
            return k;
        }

        private int FindElse(IReadOnlyList<FlowStep> items, int i)
        {
            int depth = 0;
            for (int k = i + 1; k < items.Count; k++)
            {
                var lg = (items[k].Logic ?? string.Empty).Trim();
                if (lg == "如果") depth++;
                else if (lg == "结束") { if (depth > 0) depth--; else return k; }
                else if (depth == 0 && (lg == "否则" || lg == "否则如果")) return k;
            }
            return items.Count;
        }

        private int FindEnd(IReadOnlyList<FlowStep> items, int i)
        {
            int depth = 0;
            for (int k = i + 1; k < items.Count; k++)
            {
                var lg = (items[k].Logic ?? string.Empty).Trim();
                if (lg == "如果") depth++;
                else if (lg == "结束") { if (depth > 0) depth--; else return k; }
            }
            return items.Count;
        }

        private int FindLoopEnd(IReadOnlyList<FlowStep> items, int i)
        {
            int depth = 0;
            for (int k = i + 1; k < items.Count; k++)
            {
                var lg = (items[k].Logic ?? string.Empty).Trim();
                if (lg == "循环开始") depth++;
                else if (lg == "循环结束") { if (depth == 0) return k; else depth--; }
            }
            return items.Count;
        }

        private bool EvalCondition(FlowStep s)
        {
            string left = (s.Function == "变量") ? GetVariableValue(s.Name) : s.ActualValue;
            string right = s.SetValue;
            if (string.IsNullOrWhiteSpace(left)) return false;
            bool okL = double.TryParse(left, out double lnum);
            bool okR = double.TryParse(right, out double rnum);
            switch (s.Operation)
            {
                case "大于": return okL && okR && lnum > rnum;
                case "小于": return okL && okR && lnum < rnum;
                case "大于等于": return okL && okR && lnum >= rnum;
                case "小于等于": return okL && okR && lnum <= rnum;
                case "不等于": return !string.Equals((left ?? "").Trim(), (right ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
                default: return string.Equals((left ?? "").Trim(), (right ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
            }
        }

        private int ParseLoopCount(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 1;
            var m = System.Text.RegularExpressions.Regex.Match(s ?? string.Empty, @"-?\d+");
            return m.Success ? int.Parse(m.Value) : 1;
        }

        private string GetVariableValue(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            var vars = ProjectStore.Data.Variables;
            foreach (var row in vars)
            {
                if (string.Equals((row.Name1 ?? "").Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)) return row.Value1 ?? string.Empty;
                if (string.Equals((row.Name2 ?? "").Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)) return row.Value2 ?? string.Empty;
                if (string.Equals((row.Name3 ?? "").Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)) return row.Value3 ?? string.Empty;
                if (string.Equals((row.Name4 ?? "").Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)) return row.Value4 ?? string.Empty;
                if (string.Equals((row.Name5 ?? "").Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)) return row.Value5 ?? string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// 按变量名在 Variables 表里写入新值。匹配规则与 GetVariableValue 一致（按 Name1..Name5
        /// 顺序扫描整个表，首个命中即更新对应 ValueN；找不到则静默不抛）。
        /// 用于流程行 Operation="修改" 时的赋值。
        /// </summary>
        private void SetVariableValue(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var vars = ProjectStore.Data.Variables;
            foreach (var row in vars)
            {
                if (string.Equals((row.Name1 ?? "").Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)) { row.Value1 = value ?? string.Empty; return; }
                if (string.Equals((row.Name2 ?? "").Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)) { row.Value2 = value ?? string.Empty; return; }
                if (string.Equals((row.Name3 ?? "").Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)) { row.Value3 = value ?? string.Empty; return; }
                if (string.Equals((row.Name4 ?? "").Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)) { row.Value4 = value ?? string.Empty; return; }
                if (string.Equals((row.Name5 ?? "").Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)) { row.Value5 = value ?? string.Empty; return; }
            }
        }

        /// <summary>
        /// 「实际值」列 1 秒定时刷新：遍历当前选中流程的步骤，对 Function=="变量" 的步骤
        /// 调用 GetVariableValue 把变量当前值写回 ActualValue。
        /// 其他功能的步骤 ActualValue 由执行器在 StepOnce 中按需回填，这里不动。
        /// 空集合（未选中流程）直接返回。
        /// </summary>
        private void RefreshActualValues()
        {
            var items = StepPanel?.Items;
            if (items == null || items.Count == 0) return;
            foreach (var step in items)
            {
                if (step == null) continue;
                if (step.Function != "变量") continue;
                if (string.IsNullOrWhiteSpace(step.Name)) continue;
                step.ActualValue = GetVariableValue(step.Name);
            }
        }

        private void JumpToRow()
        {
            var target = StepPanel.SelectedItem;
            if (target == null || StepPanel.Items.Count == 0) return;
            int idx = StepPanel.Items.IndexOf(target);
            if (idx < 0) idx = 0;
            if (idx >= StepPanel.Items.Count) idx = StepPanel.Items.Count - 1;
            _pendingNext = -1;
            _ifStack.Clear();
            _loopStack.Clear();
            ClearCurrentFlags();
            CurrentStep = idx;
        }

        private void Pause()
        {
            if (!CanPause) return;
            _runTimer.Stop();
            IsPaused = true;
            IsRunning = false; // 暂停后允许用“运行”继续（_currentStep 未越界时 Run 不会重置）
        }

        private void Stop()
        {
            _runTimer.Stop();
            IsRunning = false;
            IsPaused = false;
            _pendingNext = -1;
            _ifStack.Clear();
            _loopStack.Clear();
            ClearCurrentFlags();
            _currentStep = -1;
            OnPropertyChanged(nameof(CurrentStepText));
            HighlightCurrent();
            RaiseRunState();
        }

        protected override FlowItem CreateNewItem()
        {
            var kind = _nextAddKind;
            int idx = Items.Count(i => i.Kind == kind) + 1;
            // 流程新建命名：运控流程 N / 脚本流程 N / 视觉流程 N，与三类 Kind 一一对应。
            // 注：FlowKind.Table 对外显示名已由"表格"改为"运控"（运动控制）
            string prefix = kind switch
            {
                FlowKind.Table => "运控流程",
                FlowKind.Lua => "脚本流程",
                FlowKind.Vision => "视觉流程",
                _ => "流程"
            };
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
                OnPropertyChanged(nameof(IsKindTable));
                OnPropertyChanged(nameof(IsKindLua));
                OnPropertyChanged(nameof(IsKindVision));
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
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
