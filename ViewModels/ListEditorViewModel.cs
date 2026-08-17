using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 列表编辑页的通用 ViewModel：维护 Items 集合、当前选中项，以及“添加/删除”命令。
    /// Items 直接引用 ProjectStore.Data 中的共享集合（单一真实来源），任何增删改都会自动保存。
    /// 同时把配置项的名称同步到全局 Catalog，供流程页“名称”下拉框引用。
    /// </summary>
    public abstract class ListEditorViewModel<T> : ViewModelBase where T : EditorItemBase
    {
        private T? _selectedItem;
        protected int Counter { get; set; }

        /// <summary>子类在构造函数中设置为 "Axis"/"Io"/"Cylinder"/"Comm"，即可把名称汇入 Catalog；流程页等留空。</summary>
        protected string? CatalogCategory { get; set; }

        public ObservableCollection<T> Items { get; protected set; } = new();

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

        /// <summary>
        /// 子类构造函数设置好 Items（共享集合）后调用：订阅增删改以自动保存，并同步名称库。
        /// </summary>
        protected void AttachAutoSave()
        {
            Items.CollectionChanged += OnItemsChanged;
            foreach (var item in Items) SubscribeItem(item);
            SyncCatalog();
            ProjectStore.ScheduleSave();
        }

        protected virtual void Add()
        {
            var item = CreateNewItem();
            Counter++;
            Items.Add(item); // 触发 OnItemsChanged -> 订阅 + 保存
            SelectedItem = item;
        }

        protected virtual void Delete()
        {
            if (SelectedItem is null) return;
            Items.Remove(SelectedItem); // 触发 OnItemsChanged -> 取消订阅 + 保存
            SelectedItem = null;
        }

        /// <summary>由子类实现：创建一个带默认名称的新项目。</summary>
        protected abstract T CreateNewItem();

        private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (EditorItemBase item in e.NewItems) SubscribeItem(item);
            if (e.OldItems != null)
                foreach (EditorItemBase item in e.OldItems) UnsubscribeItem(item);

            SyncCatalog();
            ProjectStore.ScheduleSave();
        }

        private void SubscribeItem(EditorItemBase item)
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }

        private void UnsubscribeItem(EditorItemBase item)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            ProjectStore.ScheduleSave();
            if (e.PropertyName == nameof(EditorItemBase.Name))
                SyncCatalog();
        }

        private void SyncCatalog()
        {
            if (CatalogCategory == null) return;
            var names = Items.Select(i => i.Name).ToList();
            switch (CatalogCategory)
            {
                case "Axis": Catalog.SetAxis(names); break;
                case "Io": Catalog.SetIo(names); break;
                case "Cylinder": Catalog.SetCylinder(names); break;
                case "Comm": Catalog.SetComm(names); break;
            }
        }
    }
}
