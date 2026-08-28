// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 通用表格面板 ViewModel（输入IO / 输出IO / 变量 / 流程步骤 等都在用它）：
    /// 维护一份 ObservableCollection&lt;T&gt;，提供 添加/删除/上移/下移/复制/粘贴/回撤/重做/Excel批编辑，
    /// 并通过 JSON 快照栈支持回撤 / 重做。任何增删改都会自动保存。
    /// 具体行类型由子类通过 MakeNew / Clone 定制。
    /// SetItems 可把面板切换到另一份集合（如切换选中流程时切换其步骤集合），
    /// 会安全地清理旧集合订阅、建立新订阅并打快照。
    /// </summary>
    public abstract class TablePanelViewModel<T> : ViewModelBase where T : INotifyPropertyChanged, new()
    {
        public string Title { get; }

        private ObservableCollection<T> _items = new();
        public ObservableCollection<T> Items
        {
            get => _items;
            private set => SetItems(value);
        }

        private T? _selectedItem;
        public T? SelectedItem
        {
            get => _selectedItem;
            set { if (SetField(ref _selectedItem, value)) RaiseCommandsChanged(); }
        }

        public ICommand AddCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }

        /// <summary>Excel 批编辑：导出当前行到 .xlsx → 打开 Excel/WPS → 关闭后自动回读并替换 Items。</summary>
        public virtual ICommand ExcelEditCommand => new RelayCommand(_ => OpenExcelForBatchEdit());

        private readonly Stack<List<T>> _undo = new();
        private readonly Stack<List<T>> _redo = new();
        private T? _clipboard;
        private bool _applyingUndoRedo;

        protected TablePanelViewModel(string title, ObservableCollection<T> items)
        {
            Title = title;
            SetItems(items);

            AddCommand = new RelayCommand(_ => Add());
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedItem != null);
            MoveUpCommand = new RelayCommand(_ => Move(-1), _ => SelectedItem != null && Items.IndexOf(SelectedItem) > 0);
            MoveDownCommand = new RelayCommand(_ => Move(+1), _ => SelectedItem != null && Items.IndexOf(SelectedItem) < Items.Count - 1);
            CopyCommand = new RelayCommand(_ => Copy(), _ => SelectedItem != null);
            PasteCommand = new RelayCommand(_ => Paste(), _ => _clipboard != null);
            UndoCommand = new RelayCommand(_ => Undo(), _ => _undo.Count > 0);
            RedoCommand = new RelayCommand(_ => Redo(), _ => _redo.Count > 0);
        }

        /// <summary>
        /// 把面板绑定到另一份集合（如切换到某个流程时，切换为其步骤集合）。
        /// 会清理旧集合及其中各项的订阅、建立新订阅、打快照，并通知 UI。
        /// </summary>
        public void SetItems(ObservableCollection<T>? items)
        {
            if (ReferenceEquals(_items, items)) return;
            DetachItems(_items);
            _items = items ?? new ObservableCollection<T>();
            _items.CollectionChanged += OnItemsChanged;
            AttachItems(_items);
            OnPropertyChanged(nameof(Items));
            RaiseCommandsChanged();
            Snapshot();
        }

        protected abstract T MakeNew(int index);
        protected abstract T Clone(T src);
        protected virtual void OnItemChanged(T item, string? propertyName) { }

        private void AttachItems(IEnumerable<T> items)
        {
            foreach (var it in items) it.PropertyChanged += OnItemPropertyChanged;
        }

        private void DetachItems(IEnumerable<T>? items)
        {
            if (items == null) return;
            foreach (var it in items) it.PropertyChanged -= OnItemPropertyChanged;
        }

        private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_applyingUndoRedo) Snapshot();
            if (e.NewItems != null) AttachItems(e.NewItems.Cast<T>());
            if (e.OldItems != null) DetachItems(e.OldItems.Cast<T>());
            ProjectStore.ScheduleSave();
            RaiseCommandsChanged();
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            ProjectStore.ScheduleSave();
            if (sender is T t) OnItemChanged(t, e.PropertyName);
        }

        private void RaiseCommandsChanged()
        {
            OnPropertyChanged(nameof(MoveUpCommand));
            OnPropertyChanged(nameof(MoveDownCommand));
            OnPropertyChanged(nameof(DeleteCommand));
            OnPropertyChanged(nameof(CopyCommand));
            OnPropertyChanged(nameof(PasteCommand));
            OnPropertyChanged(nameof(UndoCommand));
            OnPropertyChanged(nameof(RedoCommand));
        }

        public void Add()
        {
            var item = MakeNew(Items.Count);
            Items.Add(item);
            SelectedItem = item;
        }

        public void Delete()
        {
            if (SelectedItem == null) return;
            var idx = Items.IndexOf(SelectedItem);
            Items.Remove(SelectedItem);
            SelectedItem = idx < Items.Count ? Items[idx] : (Items.Count > 0 ? Items[^1] : default(T));
        }

        public void Move(int delta)
        {
            if (SelectedItem == null) return;
            var idx = Items.IndexOf(SelectedItem);
            var newIdx = idx + delta;
            if (newIdx < 0 || newIdx >= Items.Count) return;
            Items.Move(idx, newIdx);
            SelectedItem = Items[newIdx];
        }

        public void Copy()
        {
            if (SelectedItem == null) return;
            _clipboard = Clone(SelectedItem);
        }

        public void Paste()
        {
            if (_clipboard == null) return;
            Items.Add(Clone(_clipboard));
            SelectedItem = Items[^1];
        }

        public void Snapshot()
        {
            _undo.Push(DeepCopy());
            _redo.Clear();
            while (_undo.Count > 100) _undo.Pop();
        }

        private List<T> DeepCopy()
        {
            var json = JsonSerializer.Serialize(Items.ToList());
            return JsonSerializer.Deserialize<List<T>>(json) ?? new();
        }

        private void Apply(List<T> snap)
        {
            _applyingUndoRedo = true;
            try
            {
                Items.Clear();
                foreach (var it in snap) Items.Add(it);
            }
            finally { _applyingUndoRedo = false; }
        }

        public void Undo()
        {
            if (_undo.Count == 0) return;
            _redo.Push(DeepCopy());
            Apply(_undo.Pop());
            SelectedItem = Items.Count > 0 ? Items[0] : default(T);
        }

        public void Redo()
        {
            if (_redo.Count == 0) return;
            _undo.Push(DeepCopy());
            Apply(_redo.Pop());
            SelectedItem = Items.Count > 0 ? Items[0] : default(T);
        }

        // ==================== Excel 批量编辑 ====================

        /// <summary>把当前 Items 导出到 .xlsx，用系统默认程序（Excel/WPS）打开；关闭后自动回读替换 Items。</summary>
        protected void OpenExcelForBatchEdit()
        {
            string path;
            try
            {
                path = ExcelBatchEdit.Export(Items, Title);
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出 Excel 失败：" + ex.Message, "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null) return;

            Process? p = null;
            try
            {
                var psi = new ProcessStartInfo(path) { UseShellExecute = true };
                p = Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开 Excel 失败：" + ex.Message +
                    "\n请确认已安装 Excel 或 WPS 等可处理 .xlsx 的程序。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (p == null)
            {
                MessageBox.Show("无法启动 Excel（无关联程序）。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            p.EnableRaisingEvents = true;
            p.Exited += (_, _) =>
            {
                try
                {
                    var imported = ExcelBatchEdit.Import<T>(path);
                    dispatcher.Invoke(() => ReplaceItemsFromExcel(imported));
                }
                catch (Exception ex)
                {
                    dispatcher.Invoke(() => MessageBox.Show("读取 Excel 失败：" + ex.Message, "提示",
                        MessageBoxButton.OK, MessageBoxImage.Warning));
                }
                finally
                {
                    try { File.Delete(path); } catch { /* 忽略 */ }
                }
            };
        }

        /// <summary>用 Excel 回读的结果替换 Items。若内容未变则跳过（避免无意义 undo）。</summary>
        protected void ReplaceItemsFromExcel(IList<T> imported)
        {
            var oldJson = JsonSerializer.Serialize(Items.ToList());
            var newJson = JsonSerializer.Serialize(imported.ToList());
            if (oldJson == newJson) return;

            ReplaceItemsInternal(imported);
            OnAfterExcelReplace(imported);
        }

        /// <summary>批量替换 Items（一次 undo 步）。</summary>
        private void ReplaceItemsInternal(IEnumerable<T> newItems)
        {
            _applyingUndoRedo = true;
            try
            {
                Items.Clear();
                foreach (var it in newItems) Items.Add(it);
            }
            finally { _applyingUndoRedo = false; }
            Snapshot();
            ProjectStore.ScheduleSave();
        }

        /// <summary>Excel 回读替换完成后给子类一个补救机会（重新同步目录等）。</summary>
        protected virtual void OnAfterExcelReplace(IList<T> imported) { }
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
