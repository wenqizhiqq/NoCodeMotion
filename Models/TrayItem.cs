using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>料盘（阵列托盘）配置，含二维单元格。</summary>
    public class TrayItem : EditorItemBase
    {
        private int _rows = 5;
        private int _cols = 5;
        private double _startX;
        private double _startY;
        private double _pitchX = 10;
        private double _pitchY = 10;
        private ObservableCollection<TrayCell> _cells = new();

        public int Rows
        {
            get => _rows;
            set { if (SetField(ref _rows, value)) RegenerateCells(); }
        }

        public int Cols
        {
            get => _cols;
            set { if (SetField(ref _cols, value)) RegenerateCells(); }
        }

        public double StartX { get => _startX; set => SetField(ref _startX, value); }
        public double StartY { get => _startY; set => SetField(ref _startY, value); }
        public double PitchX { get => _pitchX; set => SetField(ref _pitchX, value); }
        public double PitchY { get => _pitchY; set => SetField(ref _pitchY, value); }

        /// <summary>二维阵列单元格（行优先）。随 Rows/Cols 自动重生成，并尽量保留原有占用状态。</summary>
        public ObservableCollection<TrayCell> Cells
        {
            get => _cells;
            set
            {
                _cells = value ?? new ObservableCollection<TrayCell>();
                AttachCells();
                OnPropertyChanged();
            }
        }

        public TrayItem()
        {
            RegenerateCells();
        }

        private void AttachCells()
        {
            _cells.CollectionChanged -= Cells_CollectionChanged;
            _cells.CollectionChanged += Cells_CollectionChanged;
            foreach (var c in _cells)
                c.PropertyChanged -= Cell_PropertyChanged;
            foreach (var c in _cells)
                c.PropertyChanged += Cell_PropertyChanged;
        }

        private void Cells_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (TrayCell c in e.NewItems) c.PropertyChanged += Cell_PropertyChanged;
            if (e.OldItems != null)
                foreach (TrayCell c in e.OldItems) c.PropertyChanged -= Cell_PropertyChanged;
            OnPropertyChanged(nameof(Cells));
        }

        private void Cell_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 任意单元格变化都触发整体保存（供自动保存感知）
            OnPropertyChanged(nameof(Cells));
        }

        private void RegenerateCells()
        {
            var old = _cells.ToList();
            var next = new ObservableCollection<TrayCell>();
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _cols; c++)
                {
                    var prev = old.FirstOrDefault(x => x.Row == r && x.Col == c);
                    next.Add(new TrayCell { Row = r, Col = c, Occupied = prev?.Occupied ?? false });
                }
            Cells = next;
        }
    }
}
