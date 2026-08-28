// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using NoCodeMotion.Models;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 视觉流程详情 VM（DependencyObject，以便作为资源并对其 DP 绑定）。
    /// 由 FlowPage 在 Resources 里通过 BindingProxy 把 Steps/Name 绑到主选中项的 VisualSteps/Name；
    /// 因此该 VM 操作的 Steps 就是主 FlowItem.VisualSteps（同一引用），步骤的增删直接落进主流程项。
    /// </summary>
    public class VisualFlowDetailViewModel : DependencyObject
    {
        // ---- 依赖属性（与 FlowPage 资源里的绑定对应） ----
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

        private static void OnSelectedStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((VisualFlowDetailViewModel)d).HasStep = e.NewValue != null;

        // ---- 命令（操作 Steps，即主项的 VisualSteps） ----
        public ICommand AddStepCommand { get; }
        public ICommand DeleteStepCommand { get; }

        public VisualFlowDetailViewModel()
        {
            AddStepCommand = new SimpleRelayCommand(_ =>
            {
                var s = Steps;
                if (s == null) return;
                s.Add(new VisualFlowStep
                {
                    Name = $"步骤{s.Count + 1}",
                    StepType = "图像采集"
                });
            });

            // CanExecute 留为恒真以避开 RelayCommand 无 RaiseCanExecuteChanged 的限制；执行内自检
            DeleteStepCommand = new SimpleRelayCommand(_ =>
            {
                var s = Steps;
                if (s == null || SelectedStep == null) return;
                s.Remove(SelectedStep);
                SelectedStep = null;
            });
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
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
