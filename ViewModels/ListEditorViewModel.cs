using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Views;

namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 列表编辑页的通用 ViewModel：维护 Items 集合、当前选中项，以及“添加/删除/重命名”命令。
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
        public ICommand RenameCommand => new RelayCommand(_ => Rename(), _ => SelectedItem != null);

        private void Rename()
        {
            if (SelectedItem is null) return;
            var dlg = new RenameDialog("重命名", SelectedItem.Name);
            if (dlg.ShowDialog() == true && !string.IsNullOrEmpty(dlg.ResultName))
                SelectedItem.Name = dlg.ResultName!;
        }

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

            // 配置页改值实时下发设备：仅当在线模式且已挂载真实硬件桥时生效。
            if (HardwarePush.ShouldPush && sender is T t)
                PushItem(t, e.PropertyName);
        }

        /// <summary>
        /// 配置页改值实时下发设备的钩子：HardwarePush.ShouldPush 为真时由 OnItemPropertyChanged 调用。
        /// 基类默认空实现（不触发任何运动）；子类（如 AxisViewModel）重写以把速度、使能等安全参数下发到硬件。
        /// 设计原则：仅下发非运动类参数，避免编辑坐标误触发轴运动。
        /// </summary>
        protected virtual void PushItem(T item, string? propertyName) { }

        /// <summary>把当前列表的名称汇入全局 Catalog。子类可重写以自定义汇总口径（例如点位表页要汇总所有工位下的点位名）。</summary>
        protected virtual void SyncCatalog()
        {
            if (CatalogCategory == null) return;
            var names = Items.Select(i => i.Name).ToList();
            switch (CatalogCategory)
            {
                case "Axis": Catalog.SetAxis(names); break;
                case "Io": Catalog.SetIo(names); break;
                case "Cylinder": Catalog.SetCylinder(names); break;
                case "Comm": Catalog.SetComm(names); break;
                case "Variable": Catalog.SetVariable(names); break;
                case "Point": Catalog.SetPoint(names); break;
            }
        }
    }
}
