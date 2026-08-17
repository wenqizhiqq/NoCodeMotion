using System.Collections.ObjectModel;
using System.Windows.Input;
using NoCodeMotion.Models;

namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 列表编辑页的通用 ViewModel：维护 Items 集合、当前选中项，以及“添加/删除”命令。
    /// 每个具体页面（轴/IO/气缸…）只需继承并实现 CreateNewItem() 即可复用整套增删逻辑与界面。
    /// </summary>
    public abstract class ListEditorViewModel<T> : ViewModelBase where T : EditorItemBase
    {
        private T? _selectedItem;
        protected int Counter { get; set; }

        public ObservableCollection<T> Items { get; } = new();

        public T? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetField(ref _selectedItem, value))
                    OnPropertyChanged(nameof(CanDelete));
            }
        }

        public bool CanDelete => SelectedItem != null;

        public ICommand AddCommand => new RelayCommand(_ => Add());
        public ICommand DeleteCommand => new RelayCommand(_ => Delete(), _ => CanDelete);

        protected virtual void Add()
        {
            var item = CreateNewItem();
            Counter++;
            Items.Add(item);
            SelectedItem = item;
        }

        protected virtual void Delete()
        {
            if (SelectedItem is null) return;
            Items.Remove(SelectedItem);
            SelectedItem = null;
        }

        /// <summary>
        /// 由子类实现：创建一个带默认名称的新项目。
        /// </summary>
        protected abstract T CreateNewItem();
    }
}
