// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨⁣
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇⁣
// ◆◇※▣▤▥⁣
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

        // === 待提交的参数：用户在 TextBox 里改的值不会立刻 RegenerateCells，
        //   点「应用参数生成点位」按钮才生效（避免每次按键就 1650 个 INPC 对象 + 整图重绘）。
        private int _pendingRows;
        private int _pendingCols;
        private double _pendingStartX;
        private double _pendingStartY;
        private double _pendingPitchX;
        private double _pendingPitchY;

        public int Rows
        {
            get => _rows;
            // 重要：必须先 RegenerateCells 再发 OnPropertyChanged。
            // SetField 内部会立刻发 PropertyChanged → TrayPage.OnItemChanged → RenderTray，
            // 若先发 PropertyChanged 后 RegenerateCells，RenderTray 会拿到新 Rows/Cols + 旧 Cells
            // （Cells.Count = 旧 Rows*Cols），Cells[r*cols+c] 直接 IndexOutOfRange。
            set
            {
                if (_rows == value) return;
                _rows = value;
                RegenerateCells();
                OnPropertyChanged();
            }
        }

        public int Cols
        {
            get => _cols;
            set
            {
                if (_cols == value) return;
                _cols = value;
                RegenerateCells();
                OnPropertyChanged();
            }
        }

        public double StartX { get => _startX; set => SetField(ref _startX, value); }
        public double StartY { get => _startY; set => SetField(ref _startY, value); }
        public double PitchX { get => _pitchX; set => SetField(ref _pitchX, value); }
        public double PitchY { get => _pitchY; set => SetField(ref _pitchY, value); }

        public int PendingRows
        {
            get => _pendingRows;
            set { if (SetField(ref _pendingRows, value)) OnPropertyChanged(nameof(HasPendingChanges)); }
        }
        public int PendingCols
        {
            get => _pendingCols;
            set { if (SetField(ref _pendingCols, value)) OnPropertyChanged(nameof(HasPendingChanges)); }
        }
        public double PendingStartX
        {
            get => _pendingStartX;
            set { if (SetField(ref _pendingStartX, value)) OnPropertyChanged(nameof(HasPendingChanges)); }
        }
        public double PendingStartY
        {
            get => _pendingStartY;
            set { if (SetField(ref _pendingStartY, value)) OnPropertyChanged(nameof(HasPendingChanges)); }
        }
        public double PendingPitchX
        {
            get => _pendingPitchX;
            set { if (SetField(ref _pendingPitchX, value)) OnPropertyChanged(nameof(HasPendingChanges)); }
        }
        public double PendingPitchY
        {
            get => _pendingPitchY;
            set { if (SetField(ref _pendingPitchY, value)) OnPropertyChanged(nameof(HasPendingChanges)); }
        }

        /// <summary>是否有待提交的参数修改（Pending* 与生效值不一致）。</summary>
        public bool HasPendingChanges =>
            _pendingRows != _rows || _pendingCols != _cols
            || _pendingStartX != _startX || _pendingStartY != _startY
            || _pendingPitchX != _pitchX || _pendingPitchY != _pitchY;

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
            // 初始时 Pending* 跟随生效值
            _pendingRows = _rows;
            _pendingCols = _cols;
            _pendingStartX = _startX;
            _pendingStartY = _startY;
            _pendingPitchX = _pitchX;
            _pendingPitchY = _pitchY;
            RegenerateCells();
        }

        /// <summary>应用待提交的参数到生效字段（Rows/Cols 触发 RegenerateCells）。</summary>
        public void ApplyPending()
        {
            if (_pendingRows != _rows) Rows = _pendingRows;
            if (_pendingCols != _cols) Cols = _pendingCols;
            if (_pendingStartX != _startX) StartX = _pendingStartX;
            if (_pendingStartY != _startY) StartY = _pendingStartY;
            if (_pendingPitchX != _pitchX) PitchX = _pendingPitchX;
            if (_pendingPitchY != _pitchY) PitchY = _pendingPitchY;
            OnPropertyChanged(nameof(HasPendingChanges));
        }

        /// <summary>把待提交参数恢复成当前生效值（放弃未提交的修改）。</summary>
        public void ResetPending()
        {
            _pendingRows = _rows;
            _pendingCols = _cols;
            _pendingStartX = _startX;
            _pendingStartY = _startY;
            _pendingPitchX = _pitchX;
            _pendingPitchY = _pitchY;
            OnPropertyChanged(nameof(PendingRows));
            OnPropertyChanged(nameof(PendingCols));
            OnPropertyChanged(nameof(PendingStartX));
            OnPropertyChanged(nameof(PendingStartY));
            OnPropertyChanged(nameof(PendingPitchX));
            OnPropertyChanged(nameof(PendingPitchY));
            OnPropertyChanged(nameof(HasPendingChanges));
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
// ◇作者保留所有权利　请勿删除※⁣
// ◆◇※⁣