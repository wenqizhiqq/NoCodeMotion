using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 单个 IO 面板（输入或输出）的视图模型：
    /// 提供 添加/删除/上移/下移/复制/粘贴/回撤/重做 等命令，并通过简单的快照栈支持回撤 / 重做。
    /// </summary>
    public class IoPanelViewModel : ViewModelBase
    {
        public string Title { get; }
        public ObservableCollection<IoItem> Items { get; }

        private IoItem? _selectedItem;
        public IoItem? SelectedItem
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

        private readonly Stack<List<IoItem>> _undo = new();
        private readonly Stack<List<IoItem>> _redo = new();
        private IoItem? _clipboard;

        public IoPanelViewModel(string title, ObservableCollection<IoItem> items)
        {
            Title = title;
            Items = items;

            AddCommand = new RelayCommand(_ => Add());
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedItem != null);
            MoveUpCommand = new RelayCommand(_ => Move(-1), _ => SelectedItem != null && Items.IndexOf(SelectedItem) > 0);
            MoveDownCommand = new RelayCommand(_ => Move(+1), _ => SelectedItem != null && Items.IndexOf(SelectedItem) < Items.Count - 1);
            CopyCommand = new RelayCommand(_ => Copy(), _ => SelectedItem != null);
            PasteCommand = new RelayCommand(_ => Paste(), _ => _clipboard != null);
            UndoCommand = new RelayCommand(_ => Undo(), _ => _undo.Count > 0);
            RedoCommand = new RelayCommand(_ => Redo(), _ => _redo.Count > 0);

            Items.CollectionChanged += OnItemsChanged;
        }

        private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // 任何非"回撤/重做"导致的改动都会记录一次快照
            if (!_applyingUndoRedo)
                Snapshot();

            // 重新订阅各行的属性变化以触发自动保存
            if (e.NewItems != null)
                foreach (IoItem it in e.NewItems)
                    Subscribe(it);
            if (e.OldItems != null)
                foreach (IoItem it in e.OldItems)
                    Unsubscribe(it);

            SyncCatalog();
            ProjectStore.ScheduleSave();
            RaiseCommandsChanged();
        }

        private void Subscribe(IoItem it) => it.PropertyChanged += OnItemPropertyChanged;
        private void Unsubscribe(IoItem it) => it.PropertyChanged -= OnItemPropertyChanged;

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            ProjectStore.ScheduleSave();
            if (e.PropertyName == nameof(EditorItemBase.Name))
                SyncCatalog();
        }

        private void SyncCatalog()
        {
            // 合并输入 + 输出两个面板的名称到 Catalog.IoNames
            var allIoNames = ProjectStore.Data.Inputs.Select(i => i.Name)
                .Concat(ProjectStore.Data.Outputs.Select(i => i.Name));
            Catalog.SetIo(allIoNames);
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

        // === 操作 ===

        public void Add()
        {
            // 找到当前所有 IO 中序号最大的值再 +1（输入/输出分开）
            int nextSeq = Items.Count == 0 ? 1 : Items.Max(i => i.Sequence) + 1;
            var item = new IoItem
            {
                Name = $"{Title}{nextSeq}",
                Sequence = nextSeq,
                Level = Title == "输入" ? "取反" : "取反",
            };
            Items.Add(item);
            SelectedItem = item;
        }

        public void Delete()
        {
            if (SelectedItem == null) return;
            var idx = Items.IndexOf(SelectedItem);
            Items.Remove(SelectedItem);
            SelectedItem = idx < Items.Count ? Items[idx] : (Items.Count > 0 ? Items[^1] : null);
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
            // 用 JSON 深拷贝以保证粘贴的项与原项完全独立
            var json = JsonSerializer.Serialize(SelectedItem);
            _clipboard = JsonSerializer.Deserialize<IoItem>(json);
        }

        public void Paste()
        {
            if (_clipboard == null) return;
            var json = JsonSerializer.Serialize(_clipboard);
            var item = JsonSerializer.Deserialize<IoItem>(json)!;
            item.Name = $"{item.Name}_副本";
            int nextSeq = Items.Count == 0 ? 1 : Items.Max(i => i.Sequence) + 1;
            item.Sequence = nextSeq;
            Items.Add(item);
            SelectedItem = item;
        }

        // === 回撤 / 重做（用 JSON 快照实现） ===

        private bool _applyingUndoRedo;

        public void Snapshot()
        {
            var snap = DeepCopyItems();
            _undo.Push(snap);
            _redo.Clear();
            while (_undo.Count > 100) _undo.Pop(); // 限制栈大小
        }

        private List<IoItem> DeepCopyItems()
        {
            // 把当前 Items 内容深拷贝到一张列表里
            var json = JsonSerializer.Serialize(Items);
            return JsonSerializer.Deserialize<List<IoItem>>(json) ?? new();
        }

        private void ApplySnapshot(List<IoItem> snap)
        {
            _applyingUndoRedo = true;
            try
            {
                Items.Clear();
                foreach (var it in snap) Items.Add(it);
            }
            finally
            {
                _applyingUndoRedo = false;
            }
        }

        public void Undo()
        {
            if (_undo.Count == 0) return;
            var current = DeepCopyItems();
            _redo.Push(current);
            var prev = _undo.Pop();
            ApplySnapshot(prev);
            SelectedItem = Items.FirstOrDefault();
        }

        public void Redo()
        {
            if (_redo.Count == 0) return;
            var current = DeepCopyItems();
            _undo.Push(current);
            var next = _redo.Pop();
            ApplySnapshot(next);
            SelectedItem = Items.FirstOrDefault();
        }
    }

    /// <summary>IO 页面顶层 ViewModel：包含两个面板（输入/输出）。</summary>
    public class IoViewModel : ViewModelBase
    {
        public IoPanelViewModel InputPanel { get; }
        public IoPanelViewModel OutputPanel { get; }

        public IoViewModel()
        {
            InputPanel = new IoPanelViewModel("输入", ProjectStore.Data.Inputs);
            OutputPanel = new IoPanelViewModel("输出", ProjectStore.Data.Outputs);

            // 启动时把当前数据快照一下，让"回撤"可以撤销到首次加载
            InputPanel.Snapshot();
            OutputPanel.Snapshot();
        }

        /// <summary>Excel编辑 按钮占位：未来可以打开 Excel 导入/导出对话框。</summary>
        public ICommand ExcelEditCommand => new RelayCommand(_ =>
            System.Windows.MessageBox.Show("Excel 批量编辑功能尚未实现", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information));
    }
}
