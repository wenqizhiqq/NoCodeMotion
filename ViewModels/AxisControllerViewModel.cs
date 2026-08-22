using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>控制器页面：增删改控制器实例（控制卡 / 扩展IO），供轴页面选择归属。</summary>
    public class AxisControllerViewModel : ListEditorViewModel<AxisControllerItem>, IEnsureDefaultSelection
    {
        public AxisControllerViewModel()
        {
            CatalogCategory = "Controller";
            Items = ProjectStore.Data.Controllers;
            Counter = Items.Count;
            AttachAutoSave();
        }

        protected override AxisControllerItem CreateNewItem() => new AxisControllerItem { Kind = "控制卡", Name = $"控制卡{Counter + 1}" };

        /// <summary>添加一张控制卡。</summary>
        public ICommand AddCardCommand => new RelayCommand(_ => AddItem("控制卡", "控制卡"));

        /// <summary>添加一个扩展IO模块。</summary>
        public ICommand AddExpansionIoCommand => new RelayCommand(_ => AddItem("扩展IO", "扩展IO"));

        private void AddItem(string kind, string namePrefix)
        {
            var item = new AxisControllerItem { Kind = kind, Name = $"{namePrefix}{Counter + 1}" };
            Counter++;
            Items.Add(item); // 触发 OnItemsChanged -> 订阅 + 保存
            SelectedItem = item;
        }

        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0) SelectedItem = Items[0];
        }
    }
}
