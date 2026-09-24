// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‍启⁠志‍◆‏编⁠写⁣◇​微​信‍﹕‏1​8​7‍◆​1‍9⁠3⁠6‍◇⁠1‍3‏9⁣9‌　‌※‎保‌留​所⁣有‍权‎利​请‏勿‍删‏除⁣◇​⁣​
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
        /// <summary>超时报警方式可选项（与 CylinderItem.TimeoutAction 对应）。</summary>
        public string[] TimeoutActionOptions { get; } = { "报警并停止", "仅报警", "忽略" };

        public CylinderViewModel()
        {
            CatalogCategory = "Cylinder";
            Items = ProjectStore.Data.Cylinders;
            Counter = Items.Count;
            AttachAutoSave();
            // 选中气缸变化 / 周期刷新「状态显示」面板（右侧只读区，实时读 IO 表关联点位）
            PropertyChanged += (_, e) => { if (e.PropertyName == nameof(SelectedItem)) RaiseStatus(); };
            _statusTimer.Tick += (_, _) => RaiseStatus();
            _statusTimer.Start();
        }

        protected override CylinderItem CreateNewItem() => new CylinderItem { Name = $"气缸{Counter + 1}" };

        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0) SelectedItem = Items[0];
        }

        // ===== 状态显示（右侧只读区）：直接读 IO 表里关联点位的实时值 =====
        // 关联关系：输出点 -> 输出 IO（Catalog.OutIoNames）；伸出/缩回/备用感应 -> 输入 IO（Catalog.InIoNames）。

        private readonly DispatcherTimer _statusTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };

        /// <summary>当前运行状态（伸出 / 缩回）。</summary>
        public string StatusStateText
            => SelectedItem == null || string.IsNullOrEmpty(SelectedItem.CurrentState) ? "—" : SelectedItem.CurrentState;

        public string StatusOutText => Describe(FindOut(SelectedItem?.OutPoint));
        public string StatusExtendText => Describe(FindIn(SelectedItem?.SensorExtend));
        public string StatusRetractText => Describe(FindIn(SelectedItem?.SensorRetract));
        // 备用感应（BackupSensor）在运行时是「双线圈」的第二路输出（isOutput:true），故按输出 IO 查。
        public string StatusBackupText => Describe(FindOut(SelectedItem?.BackupSensor));

        private static IoItem? FindOut(string? name)
            => string.IsNullOrWhiteSpace(name) ? null : ProjectStore.Data?.Outputs?.FirstOrDefault(o => o.Name == name);
        private static IoItem? FindIn(string? name)
            => string.IsNullOrWhiteSpace(name) ? null : ProjectStore.Data?.Inputs?.FirstOrDefault(o => o.Name == name);

        private static string Describe(IoItem? io)
            => io == null ? "未关联" : $"{(io.Value != 0 ? "高电平 1" : "低电平 0")}　（{io.Function}）";

        private void RaiseStatus()
        {
            OnPropertyChanged(nameof(StatusStateText));
            OnPropertyChanged(nameof(StatusOutText));
            OnPropertyChanged(nameof(StatusExtendText));
            OnPropertyChanged(nameof(StatusRetractText));
            OnPropertyChanged(nameof(StatusBackupText));
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
            if (ReferenceEquals(item, SelectedItem)) RaiseStatus();
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
