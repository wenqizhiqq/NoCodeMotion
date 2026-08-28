// === NoCodeMotion 视觉流程详情 VM | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NoCodeMotion.Models;
using NoCodeMotion.Services.Vision;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 视觉流程详情 VM（DependencyObject，以便作为资源并对其 DP 绑定）。
    /// Steps / Name 由 VisualFlowPage 的代码隐藏通过 RelativeSource 绑到主选中 FlowItem 的
    /// VisualSteps / Name；因此本 VM 操作的 Steps 就是主 FlowItem.VisualSteps（同一引用），
    /// 步骤的增删直接落进主流程项。RunCommand 负责把流程真正跑起来并把结果回显。
    /// </summary>
    public class VisualFlowDetailViewModel : DependencyObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // ---- 依赖属性（与 FlowPage 的绑定对应） ----
        public static readonly DependencyProperty StepsProperty =
            DependencyProperty.Register(nameof(Steps), typeof(ObservableCollection<VisualFlowStep>),
                typeof(VisualFlowDetailViewModel));

        public ObservableCollection<VisualFlowStep>? Steps
        {
            get => (ObservableCollection<VisualFlowStep>?)GetValue(StepsProperty);
            set => SetValue(StepsProperty, value);
        }

        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register(nameof(Name), typeof(string), typeof(VisualFlowDetailViewModel));

        public string? Name
        {
            get => (string?)GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }

        public static readonly DependencyProperty SelectedStepProperty =
            DependencyProperty.Register(nameof(SelectedStep), typeof(VisualFlowStep),
                typeof(VisualFlowDetailViewModel),
                new PropertyMetadata(null, OnSelectedStepChanged));

        public VisualFlowStep? SelectedStep
        {
            get => (VisualFlowStep?)GetValue(SelectedStepProperty);
            set => SetValue(SelectedStepProperty, value);
        }

        public static readonly DependencyProperty HasStepProperty =
            DependencyProperty.Register(nameof(HasStep), typeof(bool), typeof(VisualFlowDetailViewModel));

        public bool HasStep
        {
            get => (bool)GetValue(HasStepProperty);
            private set => SetValue(HasStepProperty, value);
        }

        // ---- 当前选中步骤的工具类型显隐标志（用于右侧参数卡按类型切换） ----
        public bool IsImageAcquisition => SelectedStep?.StepType == "图像采集";
        public bool IsPreprocess => SelectedStep?.StepType == "图像预处理";
        public bool IsTemplateMatch => SelectedStep?.StepType == "模板匹配";
        public bool IsDefect => SelectedStep?.StepType == "缺陷检测";
        public bool IsMeasure => SelectedStep?.StepType == "测量";
        public bool IsComm => SelectedStep?.StepType == "通讯";

        // ---- 运行结果相关 ----
        public static readonly DependencyProperty ResultImageProperty =
            DependencyProperty.Register(nameof(ResultImage), typeof(ImageSource), typeof(VisualFlowDetailViewModel));

        public ImageSource? ResultImage
        {
            get => (ImageSource?)GetValue(ResultImageProperty);
            set => SetValue(ResultImageProperty, value);
        }

        public static readonly DependencyProperty HasResultProperty =
            DependencyProperty.Register(nameof(HasResult), typeof(bool), typeof(VisualFlowDetailViewModel));

        public bool HasResult
        {
            get => (bool)GetValue(HasResultProperty);
            private set => SetValue(HasResultProperty, value);
        }

        public static readonly DependencyProperty IsRunningProperty =
            DependencyProperty.Register(nameof(IsRunning), typeof(bool), typeof(VisualFlowDetailViewModel));

        public bool IsRunning
        {
            get => (bool)GetValue(IsRunningProperty);
            private set => SetValue(IsRunningProperty, value);
        }

        public static readonly DependencyProperty CanRunProperty =
            DependencyProperty.Register(nameof(CanRun), typeof(bool), typeof(VisualFlowDetailViewModel), new PropertyMetadata(true));

        public bool CanRun
        {
            get => (bool)GetValue(CanRunProperty);
            private set => SetValue(CanRunProperty, value);
        }

        public static readonly DependencyProperty RunStatusProperty =
            DependencyProperty.Register(nameof(RunStatus), typeof(string), typeof(VisualFlowDetailViewModel));

        public string RunStatus
        {
            get => (string?)GetValue(RunStatusProperty) ?? "";
            private set => SetValue(RunStatusProperty, value ?? "");
        }

        /// <summary>每步执行结果（绑定到结果列表）。同一实例，增删由集合自身通知。</summary>
        public ObservableCollection<VisionStepResult> Results { get; } = new();

        // ---- 命令 ----
        public ICommand AddStepCommand { get; }
        public ICommand DeleteStepCommand { get; }
        public ICommand RunCommand { get; }
        public ICommand RunStepCommand { get; }

        private readonly Progress<string> _progress;

        public VisualFlowDetailViewModel()
        {
            AddStepCommand = new SimpleRelayCommand(_ =>
            {
                var s = Steps;
                if (s == null) return;
                var step = new VisualFlowStep { Name = $"步骤{s.Count + 1}", StepType = "图像采集" };
                s.Add(step);
                SelectedStep = step;
            });

            DeleteStepCommand = new SimpleRelayCommand(_ =>
            {
                var s = Steps;
                if (s == null || SelectedStep == null) return;
                s.Remove(SelectedStep);
                SelectedStep = null;
            });

            RunCommand = new SimpleRelayCommand(_ => _ = RunAsync());
            RunStepCommand = new SimpleRelayCommand(p => _ = RunStepAsync(p as VisualFlowStep));
            _progress = new Progress<string>(msg => RunStatus = msg);
        }

        private static void OnSelectedStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var vm = (VisualFlowDetailViewModel)d;
            // 解旧步骤、订新步骤的 INPC，便于 StepType 变化时刷新右侧参数卡显隐
            if (e.OldValue is INotifyPropertyChanged oldInpc) oldInpc.PropertyChanged -= vm.OnSelectedStepTypeChanged;
            if (e.NewValue is INotifyPropertyChanged newInpc) newInpc.PropertyChanged += vm.OnSelectedStepTypeChanged;
            vm.HasStep = e.NewValue != null;
            vm.RaiseTypeFlags();
        }

        private void OnSelectedStepTypeChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VisualFlowStep.StepType)) RaiseTypeFlags();
        }

        private void RaiseTypeFlags()
        {
            OnPropertyChanged(nameof(IsImageAcquisition));
            OnPropertyChanged(nameof(IsPreprocess));
            OnPropertyChanged(nameof(IsTemplateMatch));
            OnPropertyChanged(nameof(IsDefect));
            OnPropertyChanged(nameof(IsMeasure));
            OnPropertyChanged(nameof(IsComm));
        }

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private async Task RunAsync()
        {
            var steps = Steps;
            if (steps == null || steps.Count == 0)
            {
                RunStatus = "请先选中视觉流程并添加步骤";
                return;
            }
            var enabled = new ObservableCollection<VisualFlowStep>(steps);
            if (enabled.Count == 0) { RunStatus = "没有可执行的步骤"; return; }

            IsRunning = true;
            CanRun = false;
            RunStatus = "视觉流程运行中…";
            Results.Clear();

            var report = await Task.Run(() => VisionEngine.Run(enabled, _progress));

            // 回到 UI 线程组装结果（WriteableBitmap 必须在 UI 线程创建）
            Results.Clear();
            foreach (var r in report.Results) Results.Add(r);

            if (report.HasImage && report.Bgra != null && report.Bgra.Length == report.Width * report.Height * 4)
            {
                var wb = new WriteableBitmap(report.Width, report.Height, 96, 96, PixelFormats.Bgra32, null);
                wb.WritePixels(new Int32Rect(0, 0, report.Width, report.Height), report.Bgra, report.Width * 4, 0);
                ResultImage = wb;
                HasResult = true;
            }
            else
            {
                HasResult = false;
            }

            int ok = 0;
            foreach (var r in Results) if (r.Ok) ok++;
            IsRunning = false;
            CanRun = true;
            RunStatus = $"完成：共 {Results.Count} 步，{ok} 步成功";
        }

        /// <summary>从首个启用步骤运行到 target（含），用于单步/分段验证，并回填该段每步的耗时与结果。</summary>
        private async Task RunStepAsync(VisualFlowStep? target)
        {
            var steps = Steps;
            if (steps == null || steps.Count == 0 || target == null)
            {
                RunStatus = "请先选中视觉流程并添加步骤";
                return;
            }
            int idx = steps.IndexOf(target);
            if (idx < 0) return;

            // 先清空所有步骤的上次结果，避免未执行步骤显示旧数据
            foreach (var s in steps) { s.DurationMs = 0; s.LastOk = false; s.LastResult = ""; }

            var runList = new ObservableCollection<VisualFlowStep>(steps.Take(idx + 1));

            IsRunning = true;
            CanRun = false;
            RunStatus = $"运行到「{target.Name}」…";
            Results.Clear();

            var report = await Task.Run(() => VisionEngine.Run(runList, _progress));

            Results.Clear();
            foreach (var r in report.Results) Results.Add(r);

            if (report.HasImage && report.Bgra != null && report.Bgra.Length == report.Width * report.Height * 4)
            {
                var wb = new WriteableBitmap(report.Width, report.Height, 96, 96, PixelFormats.Bgra32, null);
                wb.WritePixels(new Int32Rect(0, 0, report.Width, report.Height), report.Bgra, report.Width * 4, 0);
                ResultImage = wb;
                HasResult = true;
            }
            else
            {
                HasResult = false;
            }

            int ok = 0;
            foreach (var r in Results) if (r.Ok) ok++;
            IsRunning = false;
            CanRun = true;
            RunStatus = $"运行到「{target.Name}」完成：{Results.Count} 步，{ok} 步成功";
        }

        private sealed class SimpleRelayCommand : ICommand
        {
            private readonly Action<object?> _execute;
            public SimpleRelayCommand(Action<object?> execute) { _execute = execute; }
            public event EventHandler? CanExecuteChanged { add { } remove { } }
            public bool CanExecute(object? parameter) => true;
            public void Execute(object? parameter) => _execute(parameter);
        }
    }
}
// === NoCodeMotion 视觉流程详情 VM | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
