using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using NoCodeMotion.Models;

namespace NoCodeMotion.Models
{
    /// <summary>轴点位：一个命名位置，含 4 个轴槽的目标位置与速度，供流程「移动到点位」引用。</summary>
    public class PointItem : EditorItemBase
    {
        private readonly ObservableCollection<PointAxis> _positions = new();

        /// <summary>4 个轴槽的目标值（位置 + 速度）。JSON 反序列化会填充此已有集合（保持子项订阅）。</summary>
        public ObservableCollection<PointAxis> Positions => _positions;

        public PointItem()
        {
            _positions.CollectionChanged += OnPositionsChanged;
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
