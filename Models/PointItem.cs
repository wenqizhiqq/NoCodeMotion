using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace NoCodeMotion.Models
{
    /// <summary>轴点位：一个命名位置，含 4 个轴槽的目标位置与速度，供流程「移动到点位」引用。</summary>
    public class PointItem : EditorItemBase
    {
        /// <summary>固定 4 个轴槽，与所属点位表的 4 个轴一一对应。</summary>
        public const int SlotCount = 4;

        private ObservableCollection<PointAxis> _positions = new();

        public PointItem()
        {
            Attach(_positions);
            EnsureSlots();
        }

        /// <summary>4 个轴槽的目标值（位置 + 速度）。JSON 反序列化会整体替换本集合，setter 负责重新挂钩并补齐槽位。</summary>
        public ObservableCollection<PointAxis> Positions
        {
            get => _positions;
            set
            {
                if (ReferenceEquals(_positions, value)) return;
                Detach(_positions);
                _positions = value ?? new ObservableCollection<PointAxis>();
                Attach(_positions);
                EnsureSlots();
                OnPropertyChanged();
            }
        }

        /// <summary>补齐到 4 个轴槽（兼容旧工程中 Positions 为空的行，避免单元格空白且无法编辑）。</summary>
        public void EnsureSlots()
        {
            while (_positions.Count < SlotCount) _positions.Add(new PointAxis());
        }

        private void Attach(ObservableCollection<PointAxis> list)
        {
            list.CollectionChanged += OnPositionsChanged;
            foreach (var a in list) a.PropertyChanged += OnChildChanged;
        }

        private void Detach(ObservableCollection<PointAxis> list)
        {
            list.CollectionChanged -= OnPositionsChanged;
            foreach (var a in list) a.PropertyChanged -= OnChildChanged;
        }

        private void OnPositionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (PointAxis a in e.NewItems)
                    a.PropertyChanged += OnChildChanged;
            if (e.OldItems != null)
                foreach (PointAxis a in e.OldItems)
                    a.PropertyChanged -= OnChildChanged;
            OnPropertyChanged(nameof(Positions));
        }

        // 子项（某轴位置/速度）变化 → 冒泡为 PointItem 的属性变更，使列表 VM 触发自动保存
        private void OnChildChanged(object? sender, PropertyChangedEventArgs e)
            => OnPropertyChanged(nameof(Positions));
    }
}
