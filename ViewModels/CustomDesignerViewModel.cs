// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
// ◆温​启‎志‌◆‌编‌写‍◇‍微‍信⁠﹕⁠1‌8​7⁠◆‌1‌9‌3‌6‌◇⁣1‌3‌9⁠9⁠　‌※‍保‍留‍所⁣有⁣权⁠利⁠请⁣勿‍删‌除‍◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>设计器中一个控件的包装：Model 是落盘数据；LiveText 只是显示框的运行时文本（不落盘）。</summary>
    public class DesignerWidgetVM : ViewModelBase
    {
        public DesignerWidget Model { get; }

        private bool _isSelected;
        /// <summary>是否被选中（画布上的高亮框、右侧属性面板跟着它）。</summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value);
        }

        private string _liveText = string.Empty;
        /// <summary>显示框的实时文本（250ms 定时刷新；不落盘）。</summary>
        public string LiveText
        {
            get => _liveText;
            set => SetField(ref _liveText, value);
        }

        public DesignerWidgetVM(DesignerWidget model) => Model = model;
    }

    /// <summary>
    /// 自定义页面（可视化设计器）ViewModel：左侧工具箱拖控件 → 中间画布摆位置/大小 →
    /// 右侧属性面板配动作（写 IO / 气缸 / 轴移动 / 变量…）。控件随工程存进 xlsx 的「自定义」表。
    /// <para>运行时动作全部经 <see cref="HardwareBridge.Current"/> 真实下发，显示框由页面定时器读回真实值。</para>
    /// </summary>
    public class CustomDesignerViewModel : ViewModelBase
    {
        /// <summary>画布上的控件（与 <see cref="ProjectStore.Data"/>.DesignerWidgets 同步维护）。</summary>
        public ObservableCollection<DesignerWidgetVM> Widgets { get; } = new();

        /// <summary>左侧工具箱可拖出的控件类型。</summary>
        public ObservableCollection<string> ToolboxTypes { get; } = new() { "按钮", "输入框", "显示框", "标签" };

        /// <summary>按钮可用的动作。</summary>
        public static readonly string[] ButtonActions =
            { "无", "写输出IO", "气缸", "轴移动(绝对)", "轴移动(相对)", "轴回原", "轴停止", "写变量" };

        /// <summary>输入框可用的动作。</summary>
        public static readonly string[] InputActions = { "无", "写变量" };

        /// <summary>显示框可显示的内容。</summary>
        public static readonly string[] DisplayActions =
            { "显示IO", "显示气缸", "显示变量", "显示轴位置", "显示轴参数", "无" };

        /// <summary>「显示轴参数」的参数取值。</summary>
        public static readonly string[] AxisParamNames =
            { "速度", "加速时间", "减速时间", "点动距离", "手动速度" };

        private DesignerWidgetVM? _selected;
        /// <summary>当前选中的控件（画布高亮 + 右侧属性面板）。</summary>
        public DesignerWidgetVM? SelectedWidget
        {
            get => _selected;
            set
            {
                var old = _selected;
                if (!SetField(ref _selected, value)) return;
                if (old?.Model != null) old.Model.PropertyChanged -= OnSelectedModelChanged;
                if (value?.Model != null) value.Model.PropertyChanged += OnSelectedModelChanged;
                foreach (var w in Widgets)
                    w.IsSelected = ReferenceEquals(w, value);
                OnPropertyChanged(nameof(ActionOptions));
                OnPropertyChanged(nameof(TargetOptions));
                OnPropertyChanged(nameof(ParamOptions));
            }
        }

        /// <summary>选中控件的「动作」变化 → 联动刷新参数候选 / 目标候选，不合候选的旧参数收敛成默认值。</summary>
        private void OnSelectedModelChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DesignerWidget.Action)) return;
            var m = SelectedWidget?.Model;
            var opts = ParamOptions;
            if (m != null && opts != null && !opts.Contains(m.Param))
                m.Param = opts[0];
            OnPropertyChanged(nameof(ParamOptions));
            OnPropertyChanged(nameof(TargetOptions));
        }

        private bool _isDesign = true;
        /// <summary>true = 设计模式（拖拽摆布局，控件本身不可点）；false = 运行模式（按钮可点、输入框可输）。</summary>
        public bool IsDesign
        {
            get => _isDesign;
            set { if (SetField(ref _isDesign, value)) OnPropertyChanged(nameof(IsRunMode)); }
        }

        /// <summary>运行模式（给 XAML 绑 IsHitTestVisible 用）。</summary>
        public bool IsRunMode => !_isDesign;

        /// <summary>属性面板「动作」下拉的候选项（按选中控件的类型切换）。</summary>
        public IReadOnlyList<string> ActionOptions
        {
            get
            {
                var t = SelectedWidget?.Model.WidgetType;
                if (t == "输入框") return InputActions;
                if (t == "显示框") return DisplayActions;
                if (t == "标签") return new[] { "无" };   // 标签纯显示，无动作
                return ButtonActions;
            }
        }

        /// <summary>属性面板「目标」下拉的候选名（按动作切换：IO 名 / 气缸名 / 变量名 / 轴名）。</summary>
        public IReadOnlyList<string> TargetOptions
        {
            get
            {
                var data = ProjectStore.Data;
                var m = SelectedWidget?.Model;
                if (data == null || m == null) return Array.Empty<string>();
                switch (m.Action)
                {
                    case "写输出IO":
                        return data.Outputs.Select(i => i.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();
                    case "显示IO":
                        return data.Outputs.Concat(data.Inputs).Select(i => i.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();
                    case "气缸":
                    case "显示气缸":
                        return data.Cylinders.Select(i => i.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();
                    case "写变量":
                    case "显示变量":
                        return data.Variables.SelectMany(r => r.Names()).Distinct().ToList();
                    case "轴移动(绝对)":
                    case "轴移动(相对)":
                    case "轴回原":
                    case "轴停止":
                    case "显示轴位置":
                    case "显示轴参数":
                        return data.Axes.Select(i => i.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();
                    default:
                        return Array.Empty<string>();
                }
            }
        }

        /// <summary>
        /// 属性面板「参数」的固定候选（按动作）；<c>null</c> = 该动作参数自由输入（显示输入框）。
        /// 写输出IO → 开/关/切换；气缸 → 伸出/缩回/切换；显示轴参数 → 速度/加速时间/…。
        /// </summary>
        public IReadOnlyList<string>? ParamOptions
        {
            get
            {
                return SelectedWidget?.Model.Action switch
                {
                    "写输出IO" => new[] { "开", "关", "切换" },
                    "气缸" => new[] { "伸出", "缩回", "切换" },
                    "显示轴参数" => AxisParamNames,
                    _ => null,
                };
            }
        }

        public ICommand DeleteSelectedCommand { get; }
        public ICommand ClearAllCommand { get; }

        public CustomDesignerViewModel()
        {
            ReloadFromStore();
            DeleteSelectedCommand = new RelayCommand(_ => DeleteSelected());
            ClearAllCommand = new RelayCommand(_ => ClearAll());
        }

        /// <summary>从工程数据重建包装列表（页面构造 / 清空后重载时调用）。</summary>
        private void ReloadFromStore()
        {
            Widgets.Clear();
            foreach (var m in ProjectStore.Data.DesignerWidgets)
                if (m != null) Widgets.Add(new DesignerWidgetVM(m));
        }

        // ===== 增删 =====

        /// <summary>在指定位置添加一个控件（工具箱拖入画布时调用），返回新控件。</summary>
        public DesignerWidgetVM AddWidget(string type, double x, double y)
        {
            var data = ProjectStore.Data;
            int n = data.DesignerWidgets.Count(w => w != null && w.WidgetType == type) + 1;
            var m = new DesignerWidget
            {
                Name = $"{type}{n}",
                WidgetType = type,
                X = Math.Max(0, x),
                Y = Math.Max(0, y),
                Text = $"{type}{n}",
            };
            switch (type)
            {
                case "输入框":
                    m.Action = "写变量"; m.Width = 150; m.Height = 34; break;
                case "显示框":
                    m.Action = "显示变量"; m.Width = 180; m.Height = 40; break;
                case "标签":
                    m.Action = "无"; m.Text = $"标签{n}"; m.Width = 120; m.Height = 30; break;
                default:
                    m.Action = "写输出IO"; m.Width = 130; m.Height = 40; break;
            }
            data.DesignerWidgets.Add(m);
            var vm = new DesignerWidgetVM(m);
            Widgets.Add(vm);
            SelectedWidget = vm;
            return vm;
        }

        /// <summary>删除当前选中控件（画布 Delete 键 / 属性面板「删除」按钮）。</summary>
        public void DeleteSelected()
        {
            var vm = SelectedWidget;
            if (vm == null) return;
            ProjectStore.Data.DesignerWidgets.Remove(vm.Model);
            Widgets.Remove(vm);
            SelectedWidget = null;
        }

        /// <summary>清空画布。</summary>
        public void ClearAll()
        {
            ProjectStore.Data.DesignerWidgets.Clear();
            Widgets.Clear();
            SelectedWidget = null;
        }

        // ===== 运行时：按钮 / 输入框动作（真实下发硬件） =====

        /// <summary>执行按钮动作（按钮 Click 时调用）。</summary>
        public void Execute(DesignerWidget? m)
        {
            if (m == null || m.Action == "无") return;
            var bridge = HardwareBridge.Current;
            try
            {
                switch (m.Action)
                {
                    case "写输出IO":
                    {
                        var io = FindIo(m.Target);
                        if (io == null) { Warn($"输出「{m.Target}」不存在，请检查属性面板的目标名。"); return; }
                        int value = m.Param switch
                        {
                            "关" => 0,
                            "切换" => ((int)bridge.ReadOutput(io) != 0) ? 0 : 1,
                            _ => 1,
                        };
                        bridge.WriteOutput(io, value);
                        SimRuntime.SetOutput(io.Name, value);
                        StatusBarService.ReportInfo($"[自定义] 输出「{io.Name}」→ {(value != 0 ? "开" : "关")}");
                        break;
                    }
                    case "气缸":
                    {
                        var cyl = ProjectStore.Data.Cylinders.FirstOrDefault(c => c.Name == m.Target);
                        if (cyl == null) { Warn($"气缸「{m.Target}」不存在，请检查属性面板的目标名。"); return; }
                        int state = m.Param == "缩回" ? 0
                                  : m.Param == "切换" ? (SimRuntime.GetCylinder(cyl.Name) != 0 ? 0 : 1)
                                  : 1;
                        bridge.CylinderMove(cyl, state);
                        SimRuntime.SetCylinder(cyl.Name, state);
                        cyl.CurrentState = state != 0 ? "伸出" : "缩回";
                        StatusBarService.ReportInfo($"[自定义] 气缸「{cyl.Name}」→ {(state != 0 ? "伸出" : "缩回")}");
                        break;
                    }
                    case "轴移动(绝对)":
                    {
                        var axis = FindAxis(m.Target);
                        if (axis == null) { Warn($"轴「{m.Target}」不存在。"); return; }
                        if (!TryNum(m.Param, out double pos)) { Warn($"轴「{axis.Name}」目标位置「{m.Param}」不是数字。"); return; }
                        bridge.MoveAxisAbs(axis, pos);
                        StatusBarService.ReportInfo($"[自定义] 轴「{axis.Name}」绝对移动 → {pos:0.###}");
                        break;
                    }
                    case "轴移动(相对)":
                    {
                        var axis = FindAxis(m.Target);
                        if (axis == null) { Warn($"轴「{m.Target}」不存在。"); return; }
                        if (!TryNum(m.Param, out double dist)) { Warn($"轴「{axis.Name}」移动距离「{m.Param}」不是数字。"); return; }
                        bridge.MoveAxisRel(axis, dist);
                        StatusBarService.ReportInfo($"[自定义] 轴「{axis.Name}」相对移动 {dist:0.###}");
                        break;
                    }
                    case "轴回原":
                    {
                        var axis = FindAxis(m.Target);
                        if (axis == null) { Warn($"轴「{m.Target}」不存在。"); return; }
                        bridge.HomeAxis(axis);
                        StatusBarService.ReportInfo($"[自定义] 轴「{axis.Name}」回原已下发");
                        break;
                    }
                    case "轴停止":
                    {
                        var axis = FindAxis(m.Target);
                        if (axis == null) { Warn($"轴「{m.Target}」不存在。"); return; }
                        bridge.StopAxis(axis);
                        StatusBarService.ReportInfo($"[自定义] 轴「{axis.Name}」已停止");
                        break;
                    }
                    case "写变量":
                    {
                        if (string.IsNullOrWhiteSpace(m.Target)) { Warn("请先在属性面板选择要写的变量。"); return; }
                        if (!TryNum(m.Param, out double v)) { Warn($"变量「{m.Target}」的写入值「{m.Param}」不是数字。"); return; }
                        SimRuntime.SetVariable(m.Target, v);
                        StatusBarService.ReportInfo($"[自定义] 变量「{m.Target}」= {v:0.###}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                StatusBarService.ReportException($"[自定义] 「{m.Name}」动作失败：{ex.Message}");
            }
        }

        /// <summary>输入框提交（回车 / 失焦）：把输入值写进绑定的变量。</summary>
        public void CommitInput(DesignerWidget? m, string text)
        {
            if (m == null || m.Action != "写变量") return;
            if (string.IsNullOrWhiteSpace(m.Target)) { Warn("请先在属性面板选择要写的变量。"); return; }
            if (!TryNum(text, out double v)) { Warn($"变量「{m.Target}」的输入值「{text}」不是数字。"); return; }
            try
            {
                SimRuntime.SetVariable(m.Target, v);
                StatusBarService.ReportInfo($"[自定义] 变量「{m.Target}」= {v:0.###}");
            }
            catch (Exception ex)
            {
                StatusBarService.ReportException($"[自定义] 写变量失败：{ex.Message}");
            }
        }

        // ===== 运行时：显示框读回（页面 250ms 定时器调用） =====

        /// <summary>刷新所有显示框的实时文本（只动 LiveText，不碰布局数据）。</summary>
        public void RefreshLive()
        {
            foreach (var vm in Widgets)
            {
                if (vm.Model.WidgetType != "显示框") continue;
                vm.LiveText = ComputeLive(vm.Model);
            }
        }

        private string ComputeLive(DesignerWidget m)
        {
            if (string.IsNullOrWhiteSpace(m.Target)) return m.Action == "无" ? (m.Text ?? "") : "未选目标";
            try
            {
                switch (m.Action)
                {
                    case "显示IO":
                    {
                        var io = FindIo(m.Target);
                        if (io == null) return "未找到";
                        double v = ProjectStore.Data.Outputs.Contains(io)
                            ? HardwareBridge.Current.ReadOutput(io)
                            : HardwareBridge.Current.ReadInput(io);
                        return v != 0 ? "高" : "低";
                    }
                    case "显示气缸":
                    {
                        var cyl = ProjectStore.Data.Cylinders.FirstOrDefault(c => c.Name == m.Target);
                        if (cyl == null) return "未找到";
                        // 优先读真实传感器（配置了伸/缩感应），没有就回放仿真状态
                        var ext = FindIo(cyl.SensorExtend);
                        if (ext != null)
                        {
                            if ((int)HardwareBridge.Current.ReadInput(ext) == 1) return "伸出";
                            var ret = FindIo(cyl.SensorRetract);
                            if (ret != null && (int)HardwareBridge.Current.ReadInput(ret) == 1) return "缩回";
                            return "运动中";
                        }
                        return SimRuntime.GetCylinder(cyl.Name) != 0 ? "伸出" : "缩回";
                    }
                    case "显示变量":
                        return SimRuntime.GetVariableResolved(m.Target).ToString("0.###", CultureInfo.InvariantCulture);
                    case "显示轴位置":
                    {
                        var axis = FindAxis(m.Target);
                        if (axis == null) return "未找到";
                        return HardwareBridge.Current.ReadAxisPosition(axis).ToString("0.###");
                    }
                    case "显示轴参数":
                    {
                        var axis = FindAxis(m.Target);
                        if (axis == null) return "未找到";
                        return m.Param switch
                        {
                            "加速时间" => $"{axis.Accel:0.###} s",
                            "减速时间" => $"{axis.Decel:0.###} s",
                            "点动距离" => $"{axis.JogStep:0.###}",
                            "手动速度" => $"{axis.ManualSpeed:0.###}",
                            _ => $"{axis.Speed:0.###}",   // 默认显示运行速度
                        };
                    }
                    default:
                        return m.Text ?? "";
                }
            }
            catch
            {
                return "读取失败";
            }
        }

        // ===== 查找辅助 =====

        private static IoItem? FindIo(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var data = ProjectStore.Data;
            return data.Outputs.FirstOrDefault(i => i.Name == name)
                ?? data.Inputs.FirstOrDefault(i => i.Name == name);
        }

        private static AxisItem? FindAxis(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return ProjectStore.Data.Axes.FirstOrDefault(a => a.Name == name);
        }

        private static bool TryNum(string? s, out double v)
            => double.TryParse(s?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out v);

        private static void Warn(string msg) => StatusBarService.ReportException("[自定义] " + msg);
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
