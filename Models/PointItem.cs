using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using NoCodeMotion.Models;

namespace NoCodeMotion.Models
{
    /// <summary>轴点位：一个命名位置，含每个轴的目标坐标，供流程「移动到点位」引用。</summary>
    public class PointItem : EditorItemBase
    {
        private readonly ObservableCollection<PointAxis> _positions = new();

        /// <summary>各轴在点位中的目标坐标。JSON 反序列化时会填充此已有集合（保持子项订阅）。</summary>
        public ObservableCollection<PointAxis> Positions => _positions;

        public PointItem()
        {
            _positions.CollectionChanged += OnPositionsChanged;
        }

        private void OnPositionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (PointAxis pa in e.NewItems)
                    pa.PropertyChanged += OnChildChanged;
            if (e.OldItems != null)
                foreach (PointAxis pa in e.OldItems)
                    pa.PropertyChanged -= OnChildChanged;
            OnPropertyChanged(nameof(Positions));
        }

        // 子项（某轴坐标）变化 → 冒泡为 PointItem 的属性变更，使列表 VM 触发自动保存
        private void OnChildChanged(object? sender, PropertyChangedEventArgs e)
            => OnPropertyChanged(nameof(Positions));
    }
}
