using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>工位运行视图 ViewModel：操作员选择某个工位（点位表），查看该工位的全部点位，
    /// 并一键「运行此工位」——按顺序经过各点位（纯运行状态仿真，无真实运动硬件）。</summary>
    public class OperatorViewModel : ViewModelBase, IEnsureDefaultSelection
    {
        private PointTable? _selectedTable;
        private PointItem? _selectedPoint;
        private bool _isRunning;
        private string _statusText = "请选择一个工位，然后点「运行此工位」。";
        private int _runIndex = -1;

        /// <summary>所有工位（点位表）列表，供顶部下拉选择。</summary>
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
            }
        }

        /// <summary>当前选中工位的点位行集合（供表格绑定）。</summary>
        public ObservableCollection<PointItem>? CurrentPoints => SelectedTable?.Points;

        /// <summary>表格当前选中的点位（运行时会随进度高亮当前点位）。</summary>
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
            }
        }

        /// <summary>是否允许开始运行：未运行、已选工位且工位至少有 1 个点位。</summary>
        public bool CanRun => !IsRunning && SelectedTable != null && (SelectedTable.Points.Count > 0);

        public string StatusText
        {
            get => _statusText;
            set => SetField(ref _statusText, value);
        }

        public ICommand RunCommand { get; }
        public ICommand StopCommand { get; }

        private readonly DispatcherTimer _timer;

        public OperatorViewModel()
        {
            Tables = ProjectStore.Data.PointTables;
            RunCommand = new RelayCommand(_ => Run());
            StopCommand = new RelayCommand(_ => Stop());
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
            _timer.Tick += OnTick;
        }

        private void Run()
        {
            if (!CanRun) return;
            IsRunning = true;
            _runIndex = 0;
            StepTo(_runIndex);
            _timer.Start();
        }

        private void Stop()
        {
            if (!IsRunning) return;
            _timer.Stop();
            IsRunning = false;
            StatusText = "已停止。";
        }

        private void OnTick(object? sender, EventArgs e)
        {
            if (SelectedTable == null) { Stop(); return; }
            _runIndex++;
            if (_runIndex >= SelectedTable.Points.Count)
            {
                _timer.Stop();
                IsRunning = false;
                _runIndex = SelectedTable.Points.Count - 1;
                SelectedPoint = _runIndex >= 0 ? SelectedTable.Points[_runIndex] : null;
                StatusText = $"运行完成：「{SelectedTable.Name}」共 {SelectedTable.Points.Count} 个点位。";
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
            StatusText = $"正在运行「{SelectedTable.Name}」：{p.Name}（{idx + 1}/{SelectedTable.Points.Count}）";
        }

        public void EnsureDefaultSelection()
        {
            if (SelectedTable == null && Tables.Count > 0)
                SelectedTable = Tables[0];
        }
    }
}
