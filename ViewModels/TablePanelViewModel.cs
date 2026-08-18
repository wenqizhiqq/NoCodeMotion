using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 通用表格面板 ViewModel（输入IO / 输出IO / 变量 等都在用它）：
    /// 维护一份 ObservableCollection&lt;T&gt;，提供 添加/删除/上移/下移/复制/粘贴/回撤/重做，
    /// 并通过 JSON 快照栈支持回撤 / 重做。任何增删改都会自动保存。
    /// 具体行类型由子类通过 MakeNew / Clone 定制。
    /// </summary>
    public abstract class TablePanelViewModel<T> : ViewModelBase where T : INotifyPropertyChanged
    {
        public string Title { get; }
        public ObservableCollection<T> Items { get; }

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

        private readonly Stack<List<T>> _undo = new();
        private readonly Stack<List<T>> _redo = new();
        private T? _clipboard;
        private bool _applyingUndoRedo;

        protected TablePanelViewModel(string title, ObservableCollection<T> items)
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

        protected abstract T MakeNew(int index);
        protected abstract T Clone(T src);
        protected virtual void OnItemChanged(T item, string? propertyName) { }

        private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_applyingUndoRedo) Snapshot();
            if (e.NewItems != null) foreach (T it in e.NewItems) Subscribe(it);
            if (e.OldItems != null) foreach (T it in e.OldItems) Unsubscribe(it);
            ProjectStore.ScheduleSave();
            RaiseCommandsChanged();
        }

        private void Subscribe(T it) => it.PropertyChanged += OnItemPropertyChanged;
        private void Unsubscribe(T it) => it.PropertyChanged -= OnItemPropertyChanged;

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
            var json = JsonSerializer.Serialize(Items);
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
    }
}
