// ◆◇※▣▤▥▦⁣
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇⁣
// ◆◇※⁣
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.ViewModels
{
    public class TrayViewModel : ListEditorViewModel<TrayItem>, IEnsureDefaultSelection
    {
        public TrayViewModel()
        {
            Items = ProjectStore.Data.Trays;
            Counter = Items.Count;
            AttachAutoSave();
        }

        protected override TrayItem CreateNewItem() => new TrayItem { Name = $"料盘{Counter + 1}" };

        /// <summary>应用参数区修改到生效字段（Rows/Cols 触发 RegenerateCells 重生成料盘图）。</summary>
        public ICommand ApplyCommand => new RelayCommand(_ => SelectedItem?.ApplyPending());

        /// <summary>放弃未提交的参数修改（恢复 Pending* 为当前生效值）。</summary>
        public ICommand ResetPendingCommand => new RelayCommand(_ => SelectedItem?.ResetPending());

        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0)
                SelectedItem = Items[0];
        }
}
}
// ◇作者保留所有权利　请勿删除※⁣
// ◆◇※⁣