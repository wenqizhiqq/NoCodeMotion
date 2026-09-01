// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Windows.Input;
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
        }

        protected override CylinderItem CreateNewItem() => new CylinderItem { Name = $"气缸{Counter + 1}" };

        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0) SelectedItem = Items[0];
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
