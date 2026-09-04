// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 气缸页 ViewModel：左侧列表 + 右侧详情（基本信息 / 动作参数 / IO / 安全 / 高级）。
    /// 另含「伸出 / 缩回 / 复位」三个手动动作命令，直接调 HardwareBridge.Current 驱动气缸。
    /// </summary>
    public class CylinderViewModel : ListEditorViewModel<CylinderItem>, IEnsureDefaultSelection
    {
        // 药丸选择的可选项（多选一）
        public string[] TypeOptions { get; } = { "单作用", "双作用" };
        public string[] SensorTypeOptions { get; } = { "NPN", "PNP" };
        public string[] InitialStateOptions { get; } = { "伸出", "缩回" };
        public string[] ActionOptions { get; } = { "伸出", "缩回" };

        public CylinderViewModel()
        {
            CatalogCategory = "Cylinder";
            Items = ProjectStore.Data.Cylinders;
            Counter = Items.Count;
            AttachAutoSave();
            // 时序表增删后自动重排步序，保证「步」列显示正确序号
            Sequence.CollectionChanged += (_, _) => RenumberSeq();
        }

        /// <summary>时序动作表"动作"列可选项（伸出 / 缩回）。</summary>
        public string[] SeqActionOptions { get; } = { "伸出", "缩回" };

        private void RenumberSeq()
        {
            for (int i = 0; i < Sequence.Count; i++) Sequence[i].StepIndex = i + 1;
        }

        protected override CylinderItem CreateNewItem() => new CylinderItem { Name = $"气缸{Counter + 1}" };

        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0) SelectedItem = Items[0];
        }

        // ===== 气缸时序动作表 + 仿真播放 =====
        public ObservableCollection<CylinderSequenceStep> Sequence { get; } = new();

        private CylinderSequenceStep? _selectedSeqStep;
        public CylinderSequenceStep? SelectedSeqStep
        {
            get => _selectedSeqStep;
            set => SetField(ref _selectedSeqStep, value);
        }

        private bool _isSeqPlaying;
        public bool IsSeqPlaying
        {
            get => _isSeqPlaying;
            set => SetField(ref _isSeqPlaying, value);
        }

        private int _seqCurrentIndex = -1;
        public int SeqCurrentIndex
        {
            get => _seqCurrentIndex;
            set => SetField(ref _seqCurrentIndex, value);
        }

        private string _seqProgressText = "就绪";
        public string SeqProgressText
        {
            get => _seqProgressText;
            set => SetField(ref _seqProgressText, value);
        }

        public ICommand AddSeqStepCommand => new RelayCommand(_ =>
        {
            var name = Sequence.Count == 0 && Items.Count > 0 ? Items[0].Name : "";
            var step = new CylinderSequenceStep { Cylinder = name, Action = "伸出", DelayMs = 300 };
            Sequence.Add(step);
            SelectedSeqStep = step;
        });

        public ICommand RemoveSeqStepCommand => new RelayCommand(_ =>
        {
            if (SelectedSeqStep != null) Sequence.Remove(SelectedSeqStep);
        }, _ => SelectedSeqStep != null);

        public ICommand PlaySequenceCommand => new RelayCommand(_ => PlaySequence(), _ => !IsSeqPlaying && Sequence.Count > 0);
        public ICommand StopSequenceCommand => new RelayCommand(_ => StopSequence(), _ => IsSeqPlaying);

        private DispatcherTimer? _seqTimer;
        private int _seqIdx;
        private double _seqElapsed;

        private void PlaySequence()
        {
            if (Sequence.Count == 0) return;
            StopSequence();
            _seqIdx = 0;
            _seqElapsed = 0;
            IsSeqPlaying = true;
            SeqCurrentIndex = 0;
            ApplySeqStep(0);
            _seqTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _seqTimer.Tick += OnSeqTick;
            _seqTimer.Start();
        }

        private void OnSeqTick(object? sender, EventArgs e)
        {
            if (_seqIdx >= Sequence.Count) { StopSequence(); return; }
            var step = Sequence[_seqIdx];
            _seqElapsed += 33;
            if (_seqElapsed >= Math.Max(0, step.DelayMs))
            {
                _seqIdx++;
                _seqElapsed = 0;
                if (_seqIdx >= Sequence.Count) { StopSequence(); return; }
                SeqCurrentIndex = _seqIdx;
                ApplySeqStep(_seqIdx);
            }
        }

        private void ApplySeqStep(int idx)
        {
            if (idx < 0 || idx >= Sequence.Count) return;
            var step = Sequence[idx];
            int state = step.Action == "缩回" ? 0 : 1;
            SimRuntime.SetCylinder(step.Cylinder, state);
            // 同步列表内联按钮着色（按名称找到对应气缸条目）
            var item = ProjectStore.Data?.Cylinders?
                .FirstOrDefault(c => string.Equals(c.Name, step.Cylinder, StringComparison.OrdinalIgnoreCase));
            if (item != null) item.CurrentState = state == 1 ? "伸出" : "缩回";
            SeqProgressText = $"第 {idx + 1}/{Sequence.Count} 步：{step.Cylinder} {step.Action}";
        }

        private void StopSequence()
        {
            if (_seqTimer != null) { _seqTimer.Stop(); _seqTimer.Tick -= OnSeqTick; _seqTimer = null; }
            IsSeqPlaying = false;
            SeqCurrentIndex = -1;
            SeqProgressText = "已停止";
        }

        // ===== 手动动作命令：伸出 / 缩回 / 复位（依赖 HasSelection） =====

        /// <summary>伸出当前选中气缸（state=1）。仅当详情页/脚本入口调用——列表行内按钮已改用 <see cref="RowExtendCommand"/>。</summary>
        public ICommand ExtendCommand => new RelayCommand(_ => Move(1), _ => SelectedItem != null);

        /// <summary>缩回当前选中气缸（state=0）。仅当详情页/脚本入口调用——列表行内按钮已改用 <see cref="RowRetractCommand"/>。</summary>
        public ICommand RetractCommand => new RelayCommand(_ => Move(0), _ => SelectedItem != null);

        /// <summary>复位到 InitialState（一般用于复位流程的入口）。</summary>
        public ICommand ResetCommand => new RelayCommand(_ => Reset(), _ => SelectedItem != null);

        // ===== 行内手动动作命令：不依赖 SelectedItem，按 CommandParameter 传入的行 item 直接驱动 =====
        // 用于列表行内的「伸出 / 缩回」按钮——点哪行驱动哪行，无需先选中行。

        /// <summary>伸出指定气缸（参数为绑定的当前行 CylinderItem）。</summary>
        public ICommand RowExtendCommand => new RelayCommand(p =>
        {
            if (p is CylinderItem item) MoveItem(item, 1);
        });

        /// <summary>缩回指定气缸（参数为绑定的当前行 CylinderItem）。</summary>
        public ICommand RowRetractCommand => new RelayCommand(p =>
        {
            if (p is CylinderItem item) MoveItem(item, 0);
        });

        private void Move(int state)
        {
            if (SelectedItem is null) return;
            MoveItem(SelectedItem, state);
        }

        private void MoveItem(CylinderItem item, int state)
        {
            HardwareBridge.Current.CylinderMove(item, state);
            // 同步运行时状态，列表内联按钮据此着色（state: 1=伸出 / 0=缩回）
            item.CurrentState = state == 1 ? "伸出" : "缩回";
            // 同步仿真仓，使 3D 视图的活塞伸缩与页面列表一致
            SimRuntime.SetCylinder(item.Name, state);
        }

        private void Reset()
        {
            if (SelectedItem is null) return;
            HardwareBridge.Current.CylinderReset(SelectedItem);
            // 复位回到配置里的初始状态
            SelectedItem.CurrentState = SelectedItem.InitialState;
        }
    }
}

// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
