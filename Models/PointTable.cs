using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace NoCodeMotion.Models
{
    /// <summary>
    /// 点位表（一个工位）：包含该工位选择的 4 个轴，以及该工位下的全部点位行。
    /// 一个工程可以有多个工位，左侧列表可新增/删除/切换。
    /// </summary>
    public class PointTable : EditorItemBase
    {
        /// <summary>每个工位固定 4 个轴槽。</summary>
        public const int SlotCount = 4;

        private ObservableCollection<PointItem> _points = new();
        private ObservableCollection<string> _axisNames = new();

        public PointTable()
        {
            EnsureAxisSlots();
            Attach(_points);
            Hook(_axisNames);
        }

        /// <summary>该工位下的点位行（每行含名称 + 4 轴的位置/速度）。</summary>
        public ObservableCollection<PointItem> Points
        {
            get => _points;
            set
            {
                if (ReferenceEquals(_points, value)) return;
                Detach(_points);
                _points = value ?? new ObservableCollection<PointItem>();
                Attach(_points);
                OnPropertyChanged();
                OnPropertyChanged(nameof(PointNamesSignature));
            }
        }

        /// <summary>该工位所选的 4 个轴名（按槽位 0..3），决定表格 8 个轴列的列头。</summary>
        public ObservableCollection<string> AxisNames
        {
            get => _axisNames;
            set
            {
                if (ReferenceEquals(_axisNames, value)) return;
                Unhook(_axisNames);
                _axisNames = value ?? new ObservableCollection<string>();
                EnsureAxisSlots();
                Hook(_axisNames);
                OnPropertyChanged();
            }
        }

        /// <summary>所有点位名称拼成的签名，仅用于通知“点位名称集合已变化”，不参与持久化。</summary>
        [JsonIgnore]
        public string PointNamesSignature => string.Join("|", _points.Select(p => p.Name));

        /// <summary>补齐到 4 个轴槽（兼容旧工程或 JSON 缺失字段）。</summary>
        public void EnsureAxisSlots()
        {
            while (_axisNames.Count < SlotCount) _axisNames.Add(string.Empty);
        }

        // ===== 订阅管理：任何子项变化都冒泡为本对象的属性变更，驱动列表 VM 自动保存 =====

        private void Attach(ObservableCollection<PointItem> list)
        {
            list.CollectionChanged += OnPointsChanged;
            foreach (var p in list)
            {
                p.EnsureSlots();
                p.PropertyChanged += OnPointChanged;
            }
        }

        private void Detach(ObservableCollection<PointItem> list)
        {
            list.CollectionChanged -= OnPointsChanged;
            foreach (var p in list) p.PropertyChanged -= OnPointChanged;
        }

        private void Hook(ObservableCollection<string> list) => list.CollectionChanged += OnAxisNamesChanged;

        private void Unhook(ObservableCollection<string> list) => list.CollectionChanged -= OnAxisNamesChanged;

        private void OnPointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (PointItem p in e.NewItems)
                {
                    p.EnsureSlots();
                    p.PropertyChanged += OnPointChanged;
                }
            if (e.OldItems != null)
                foreach (PointItem p in e.OldItems)
                    p.PropertyChanged -= OnPointChanged;

            OnPropertyChanged(nameof(Points));
            OnPropertyChanged(nameof(PointNamesSignature));
        }

        private void OnAxisNamesChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => OnPropertyChanged(nameof(AxisNames));

        private void OnPointChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Points));
            if (e.PropertyName == nameof(Name))
                OnPropertyChanged(nameof(PointNamesSignature));
        }
    }
}
