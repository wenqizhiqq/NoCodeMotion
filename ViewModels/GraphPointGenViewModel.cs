// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁠志‎◆‌编‍写​◇⁠微⁠信⁣﹕⁣1⁠8‎7‎◆‍1​9‎3​6⁠◇‍1‌3⁠9⁣9⁣　‌※⁣保⁣留‌所​有⁣权⁠利​请⁠勿​删‏除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 「图形生成点位」——在点位表页内嵌的一块画布（框）里工作：
    ///   · 可导入一张底图（图片）作背景，在图上描点；
    ///   · 在框内单击添加点位，折线模式下连续单击连成折线；
    ///   · 按「框尺寸 + 起点偏移」把画布坐标线性映射为机器坐标（画布左下角 = 起点X/起点Y，Y 轴向上）；
    ///   · 点「生成点位」把结果按顺序追加到当前工位的点位行（轴1 ← X、轴2 ← Y）。
    /// 画布坐标固定为 <see cref="CanvasW"/> × <see cref="CanvasH"/>（像素）；框 = 整块画布。
    /// </summary>
    public class GraphPointGenViewModel : INotifyPropertyChanged
    {
        /// <summary>画布宽度（像素）。画布坐标即以此为准，框 = 整块画布。</summary>
        public const double CanvasW = 340;

        /// <summary>画布高度（像素）。</summary>
        public const double CanvasH = 260;

        public GraphPointGenViewModel()
        {
            ImportImageCommand = new RelayCommand(_ => ImportImage());
            NewStrokeCommand = new RelayCommand(_ => NewStroke(), _ => Points.Count > 0);
            UndoCommand = new RelayCommand(_ => Undo(), _ => Points.Count > 0);
            ClearCommand = new RelayCommand(_ => Clear(), _ => Points.Count > 0);
            GenerateCommand = new RelayCommand(_ => Generate(), _ => Points.Count > 0);
            ToggleExpandCommand = new RelayCommand(_ => Expanded = !Expanded);
        }

        // ===================== 参数 =====================

        private double _frameW = 100;
        /// <summary>框宽（机器单位，如 mm）：画布整幅宽度对应的机器行程。</summary>
        public double FrameW { get => _frameW; set => Set(ref _frameW, value); }

        private double _frameH = 100;
        /// <summary>框高（机器单位）：画布整幅高度对应的机器行程。</summary>
        public double FrameH { get => _frameH; set => Set(ref _frameH, value); }

        private double _originX;
        /// <summary>起点X：框左下角对应的机器 X（轴1）坐标。</summary>
        public double OriginX { get => _originX; set => Set(ref _originX, value); }

        private double _originY;
        /// <summary>起点Y：框左下角对应的机器 Y（轴2）坐标。</summary>
        public double OriginY { get => _originY; set => Set(ref _originY, value); }

        private double _speed = 100;
        /// <summary>生成点位的目标速度（写入轴1/轴2）。</summary>
        public double Speed { get => _speed; set => Set(ref _speed, value); }

        private string _prefix = "G";
        /// <summary>生成点位的命名前缀（前缀 + 序号）。</summary>
        public string Prefix { get => _prefix; set => Set(ref _prefix, value); }

        private double _step;
        /// <summary>折线采样步长（机器单位）：&gt;0 时把折线按该步长采样成密集点；0 = 只取折线顶点。</summary>
        public double Step { get => _step; set => Set(ref _step, value); }

        private bool _polylineMode = true;
        /// <summary>折线模式：勾选时连续单击连成折线；取消时每次单击是独立点位。</summary>
        public bool PolylineMode { get => _polylineMode; set => Set(ref _polylineMode, value); }

        private bool _expanded = true;
        /// <summary>面板是否展开（收起后只留标题，少占界面）。</summary>
        public bool Expanded { get => _expanded; set => Set(ref _expanded, value); }

        // ===================== 画布数据（画布坐标） =====================

        // 每条 = 一条折线；单击模式下每个点自成一条（长度 1）。
        private readonly List<List<Point>> _strokes = new();
        private List<Point>? _current;

        /// <summary>画布上的点位标记（用于绘制圆点 + 序号）。</summary>
        public ObservableCollection<GraphPtVm> Points { get; } = new();

        private Geometry _path = Geometry.Empty;
        /// <summary>所有折线拼成的几何（用于绘制轨迹）。</summary>
        public Geometry Path { get => _path; private set => Set(ref _path, value); }

        private ImageSource? _background;
        /// <summary>导入的底图（可选）。</summary>
        public ImageSource? Background { get => _background; private set => Set(ref _background, value); }

        public bool HasPoints => Points.Count > 0;

        /// <summary>生成时回传：机器坐标点位序列（轴1 = X、轴2 = Y）。</summary>
        public event Action<List<Point>>? Generated;

        // ===================== 命令 =====================

        public ICommand ImportImageCommand { get; }
        public ICommand NewStrokeCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand GenerateCommand { get; }
        public ICommand ToggleExpandCommand { get; }

        // ===================== 画布交互 =====================

        /// <summary>在画布坐标 (x, y) 处添加一个点（由页面把鼠标位置换算成画布坐标后调用）。</summary>
        public void AddPoint(double x, double y)
        {
            x = Math.Max(0, Math.Min(CanvasW, x));
            y = Math.Max(0, Math.Min(CanvasH, y));
            if (!PolylineMode || _current == null)
            {
                _current = new List<Point>();
                _strokes.Add(_current);
            }
            _current.Add(new Point(x, y));
            Rebuild();
        }

        /// <summary>结束当前折线：下一次单击起一条新折线。</summary>
        public void NewStroke() => _current = null;

        private void Undo()
        {
            if (_strokes.Count == 0) return;
            var last = _strokes[^1];
            last.RemoveAt(last.Count - 1);
            if (last.Count == 0) _strokes.RemoveAt(_strokes.Count - 1);
            _current = _strokes.Count > 0 ? _strokes[^1] : null;
            Rebuild();
        }

        private void Clear()
        {
            _strokes.Clear();
            _current = null;
            Rebuild();
        }

        private void Rebuild()
        {
            Points.Clear();
            var geo = new PathGeometry();
            int idx = 1;
            foreach (var s in _strokes)
            {
                foreach (var p in s)
                    Points.Add(new GraphPtVm { X = p.X, Y = p.Y, Index = idx++ });

                if (s.Count >= 2)
                {
                    var fig = new PathFigure { StartPoint = s[0], IsClosed = false, IsFilled = false };
                    var seg = new PolyLineSegment();
                    for (int i = 1; i < s.Count; i++) seg.Points.Add(s[i]);
                    fig.Segments.Add(seg);
                    geo.Figures.Add(fig);
                }
            }
            Path = geo;
            OnPropertyChanged(nameof(HasPoints));
        }

        private void ImportImage()
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Title = "选择底图（用于描点）",
                    Filter = "图片 (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|所有文件 (*.*)|*.*",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dlg.ShowDialog() != true) return;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;   // 立即读入，避免文件被占用
                bmp.UriSource = new Uri(dlg.FileName);
                bmp.EndInit();
                bmp.Freeze();
                Background = bmp;
            }
            catch (Exception ex)
            {
                StatusBarService.ReportException($"导入底图失败：{ex.Message}");
            }
        }

        // ===================== 生成 =====================

        private void Generate()
        {
            var pts = new List<Point>();
            foreach (var s in _strokes)
            {
                var machine = s.Select(ToMachine).ToList();
                if (Step > 0 && machine.Count >= 2)
                    machine = Resample(machine, Step);
                pts.AddRange(machine);
            }
            if (pts.Count == 0)
            {
                StatusBarService.ReportException("画布中还没有点位，请先在框内单击添加。");
                return;
            }
            Generated?.Invoke(pts);
        }

        /// <summary>画布坐标 → 机器坐标：左下角为起点(X,Y)，画布 Y 向下、机器 Y 向上。</summary>
        private Point ToMachine(Point c)
        {
            double mx = OriginX + c.X / CanvasW * FrameW;
            double my = OriginY + (CanvasH - c.Y) / CanvasH * FrameH;
            return new Point(mx, my);
        }

        /// <summary>把一条机器坐标折线按步长采样成密集点（含各段起点、含末点）。</summary>
        private static List<Point> Resample(List<Point> poly, double step)
        {
            var outp = new List<Point>();
            for (int i = 0; i < poly.Count - 1; i++)
            {
                var a = poly[i];
                var b = poly[i + 1];
                double dx = b.X - a.X, dy = b.Y - a.Y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                int n = (int)Math.Floor(len / step);
                if (n < 1) n = 1;
                for (int k = 0; k < n; k++)
                {
                    double t = (double)k / n;
                    outp.Add(new Point(a.X + dx * t, a.Y + dy * t));
                }
            }
            outp.Add(poly[^1]);
            return outp;
        }

        // ===================== INotifyPropertyChanged =====================

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }

    /// <summary>画布上的一个点位标记（画布坐标 + 序号）。</summary>
    public class GraphPtVm
    {
        public double X { get; set; }
        public double Y { get; set; }
        public int Index { get; set; }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
