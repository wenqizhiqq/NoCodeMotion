using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>点位表页 ViewModel：选择 4 个轴，维护点位列表（点位名称 + 4 轴位置/速度），并提供轴的使能/回原/寸动/JOG 控制。</summary>
    public class PointViewModel : ListEditorViewModel<PointItem>, IEnsureDefaultSelection
    {
        /// <summary>4 个轴槽的运行/配置状态。轴名持久化到工程；使能/回原/当前位置为运行态（不落盘）。</summary>
        public ObservableCollection<AxisState> AxisStates { get; } = new();

        private double _inchStep = 1.0;
        private double _jogStep = 10.0;

        /// <summary>寸动步长（每次寸动移动的单位）。</summary>
        public double InchStep
        {
            get => _inchStep;
            set => SetField(ref _inchStep, value);
        }

        /// <summary>JOG 步长（每次 JOG 移动的单位，通常大于寸动）。</summary>
        public double JogStep
        {
            get => _jogStep;
            set => SetField(ref _jogStep, value);
        }

        public PointViewModel()
        {
            CatalogCategory = "Point";
            Items = ProjectStore.Data.Points;
            Counter = Items.Count;

            // 初始化 4 个轴槽（轴名从工程载入）
            var saved = ProjectStore.Data.PointAxes;
            for (int i = 0; i < 4; i++)
            {
                var st = new AxisState(i) { AxisName = i < saved.Count ? saved[i] : string.Empty };
                st.PropertyChanged += OnAxisStateChanged;
                AxisStates.Add(st);
            }

            EnableCommand = new RelayCommand(ToggleEnable);
            HomeCommand = new RelayCommand(Home);
            InchCommand = new RelayCommand(p => Move(p, false));
            JogCommand = new RelayCommand(p => Move(p, true));

            AttachAutoSave();
        }

        // 轴名变更 → 写回工程并保存
        private void OnAxisStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AxisState.AxisName) && sender is AxisState st)
            {
                while (ProjectStore.Data.PointAxes.Count <= st.Index)
                    ProjectStore.Data.PointAxes.Add(string.Empty);
                ProjectStore.Data.PointAxes[st.Index] = st.AxisName;
                ProjectStore.ScheduleSave();
            }
        }

        protected override PointItem CreateNewItem()
        {
            var point = new PointItem { Name = $"点位{Counter + 1}" };
            for (int i = 0; i < 4; i++)
                point.Positions.Add(new PointAxis());
            return point;
        }

        // ===== 轴控制命令（运行态仿真：当前无运动硬件层，命令在此更新运行态，后续接运动引擎）=====
        public ICommand EnableCommand { get; }
        public ICommand HomeCommand { get; }
        public ICommand InchCommand { get; }
        public ICommand JogCommand { get; }

        private void ToggleEnable(object? p)
        {
            int i = (int)p!;
            AxisStates[i].Enabled = !AxisStates[i].Enabled;
        }

        private void Home(object? p)
        {
            int i = (int)p!;
            AxisStates[i].CurrentPosition = 0;
            AxisStates[i].Homed = true;
        }

        private void Move(object? p, bool jog)
        {
            var parts = ((string)p!).Split(',');
            int i = int.Parse(parts[0]);
            int dir = int.Parse(parts[1]);
            double step = jog ? JogStep : InchStep;
            AxisStates[i].CurrentPosition += dir * step;
        }

        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0)
                SelectedItem = Items[0];
        }
    }

    /// <summary>单个轴槽的运行/配置状态。</summary>
    public class AxisState : ViewModelBase
    {
        public int Index { get; }

        private string _axisName = string.Empty;
        private bool _enabled;
        private bool _homed;
        private double _current;

        public AxisState(int index) => Index = index;

        /// <summary>该槽位选择的轴名（来自 Catalog.AxisNames）。</summary>
        public string AxisName
        {
            get => _axisName;
            set => SetField(ref _axisName, value);
        }

        public bool Enabled
        {
            get => _enabled;
            set => SetField(ref _enabled, value);
        }

        public bool Homed
        {
            get => _homed;
            set => SetField(ref _homed, value);
        }

        /// <summary>轴的当前位置（运行态显示，仿真用）。</summary>
        public double CurrentPosition
        {
            get => _current;
            set => SetField(ref _current, value);
        }
    }
}
