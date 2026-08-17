using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 流程页 ViewModel：左侧列表管理“流程”项目（复用基类增删 + 自动保存），
    /// 右侧表格管理当前流程内的“步骤”（FlowStep），步骤的增删改同样自动保存。
    /// </summary>
    public class FlowViewModel : ListEditorViewModel<FlowItem>
    {
        private FlowStep? _selectedStep;

        public FlowStep? SelectedStep
        {
            get => _selectedStep;
            set
            {
                if (SetField(ref _selectedStep, value))
                    OnPropertyChanged(nameof(CanDeleteStep));
            }
        }

        public bool CanDeleteStep => SelectedStep != null && SelectedItem != null;

        public ICommand AddStepCommand => new RelayCommand(_ => AddStep(), _ => SelectedItem != null);
        public ICommand DeleteStepCommand => new RelayCommand(_ => DeleteStep(), _ => CanDeleteStep);

        public FlowViewModel()
        {
            Items = ProjectStore.Data.Flows;
            Counter = Items.Count;
            AttachAutoSave();

            foreach (var flow in Items) AttachSteps(flow);
            Items.CollectionChanged += OnFlowsChanged;
        }

        protected override FlowItem CreateNewItem() => new FlowItem { Name = $"流程{Counter + 1}" };

        private void AddStep()
        {
            if (SelectedItem is null) return;
            var step = new FlowStep { Name = $"步骤{SelectedItem.Steps.Count + 1}" };
            SelectedItem.Steps.Add(step);
            SelectedStep = step;
        }

        private void DeleteStep()
        {
            if (SelectedItem is null || SelectedStep is null) return;
            SelectedItem.Steps.Remove(SelectedStep);
            SelectedStep = null;
        }

        private void OnFlowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null) foreach (FlowItem f in e.NewItems) AttachSteps(f);
            if (e.OldItems != null) foreach (FlowItem f in e.OldItems) DetachSteps(f);
        }

        private void AttachSteps(FlowItem flow)
        {
            flow.Steps.CollectionChanged += OnStepsChanged;
            foreach (var step in flow.Steps) step.PropertyChanged += OnStepPropertyChanged;
        }

        private void DetachSteps(FlowItem flow)
        {
            flow.Steps.CollectionChanged -= OnStepsChanged;
        }

        private void OnStepsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (FlowStep st in e.NewItems) st.PropertyChanged += OnStepPropertyChanged;
            if (e.OldItems != null)
                foreach (FlowStep st in e.OldItems) st.PropertyChanged -= OnStepPropertyChanged;

            ProjectStore.ScheduleSave();
        }

        private void OnStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
            => ProjectStore.ScheduleSave();
    }
}
