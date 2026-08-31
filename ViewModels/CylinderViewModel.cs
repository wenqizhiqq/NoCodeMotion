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

        /// <summary>伸出当前选中气缸（state=1）。</summary>
        public ICommand ExtendCommand => new RelayCommand(_ => Move(1), _ => SelectedItem != null);

        /// <summary>缩回当前选中气缸（state=0）。</summary>
        public ICommand RetractCommand => new RelayCommand(_ => Move(0), _ => SelectedItem != null);

        /// <summary>复位到 InitialState（一般用于复位流程的入口）。</summary>
        public ICommand ResetCommand => new RelayCommand(_ => HardwareBridge.Current.CylinderReset(SelectedItem!), _ => SelectedItem != null);

        private void Move(int state)
        {
            if (SelectedItem is null) return;
            HardwareBridge.Current.CylinderMove(SelectedItem, state);
        }
    }
}

// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
