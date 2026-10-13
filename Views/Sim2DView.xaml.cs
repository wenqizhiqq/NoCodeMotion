// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// =====================================================================
// 2D 运控仿真视图（俯视）。配套内核见 Services/Sim2DMotion.cs。
//
// 数据源与 3D 仿真完全一致：AxisRuntimeState（轴位置）+ SimRuntime（IO/气缸/相机）。
// 流程仿真播放器 / 脚本桩 / 点位流程写共享态 → 本视图梯形规划追踪渲染；
// 本页点动按住连动 → 内核持续前推目标并回写共享态（3D 视图/自定义页面读回一致）。
//
// 渲染：DispatcherTimer 16ms 推进内核 + 直接改 Canvas 子元素几何（无绑定开销）。
// 仅在 IsVisible 时计时；切到 3D 或翻页自动停止，省出 UI 线程。
// =====================================================================
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using NoCodeMotion.Models;
using NoCodeMotion.Services.Hardware;
using System.Linq;
using System.Text.RegularExpressions;
using NoCodeMotion.Services;

namespace NoCodeMotion.Views
{
    public partial class Sim2DView : UserControl
    {
        private readonly Sim2DMotion _motion = new();
        private readonly DispatcherTimer _timer;
        private DateTime _last = DateTime.UtcNow;
        private int _tick;

        // 轴映射：X/Y 画平面，Z 表现刀头内芯大小
        private Sim2DAxis _ax = new() { Name = "X" };
        private Sim2DAxis _ay = new() { Name = "Y" };
        private Sim2DAxis _az = new() { Name = "Z" };

        // ===== 生成布局（按当前工程配置自动生成 2D 仿真，与实际轴IO联动）=====
        private bool _ioLink;                                  // 生成布局后开启：轮询实际轴位置/IO
        private readonly List<(Shape El, double Wx, double Wy, double Ww, double Wh)> _layoutRects = new();
        private readonly List<(Shape El, double Wx, double Wy)> _layoutDots = new();
        private readonly List<(TextBlock El, double Wx, double Wy)> _layoutTexts = new();
        private readonly List<(Shape El, double Wx, double Wy, Func<int?> Val)> _ioMarks = new();
        private readonly List<(Shape El, string Cyl, int Row, int Col)> _nozzleDots = new();
        private readonly List<(TextBlock El, int Row)> _nozzleLabels = new();
        private readonly Dictionary<string, AxisStatusSnapshot> _lastSnap = new(StringComparer.OrdinalIgnoreCase);
        private Rect _layoutBounds = Rect.Empty;

        private static readonly SolidColorBrush IoOnBr = Br("#22C55E");
        private static readonly SolidColorBrush IoOffBr = Br("#94A3B8");
        private static readonly SolidColorBrush IoNoneBr = Br("#CBD5E1");
        private static readonly SolidColorBrush NozzleBr = Br("#3B82F6");
        private static readonly SolidColorBrush NozzleOnBr = Br("#10B981");

        // 世界（mm）→ 画布（px）映射
        private Rect _world = new(-110, -110, 220, 220);
        private double _scale = 1, _offX = 0, _offY = 0;
        private bool _worldDirty = true;
        private bool _transformDirty = true;
        private double _lastW = -1, _lastH = -1;
        private int _lastTraceCount = -1;
        private bool _showTrace = true;

        private string _axesSignature = "";
        private int _chipSignature = -1;
        private readonly List<(Ellipse Dot, string Kind, object Item)> _chipDots = new();

        public ICommand JogStartCmd { get; }
        public ICommand JogStopCmd { get; }

        public Sim2DView()
        {
            InitializeComponent();
            JogStartCmd = new MiniRelay(p => StartJog(p as string ?? ""));
            JogStopCmd = new MiniRelay(_ => StopJog());

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _timer.Tick += OnTimerTick;

            Loaded += (_, __) => { RebuildAxes(); UpdateTimerGate(); };
            Unloaded += (_, __) => _timer.Stop();
            IsVisibleChanged += (_, __) => UpdateTimerGate();
            SizeChanged += (_, __) => { _worldDirty = true; };
        }

        // ===================== 计时门控 =====================
        private void UpdateTimerGate()
        {
            bool run = IsVisible && IsLoaded;
            if (run && !_timer.IsEnabled) { _last = DateTime.UtcNow; RebuildAxes(); _timer.Start(); }
            else if (!run && _timer.IsEnabled) _timer.Stop();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            var now = DateTime.UtcNow;
            double dt = (now - _last).TotalSeconds;
            _last = now;
            if (dt <= 0 || dt > 0.5) dt = 0.016;

            _motion.Tick(dt);

            _tick++;
            if (_tick % 60 == 0) CheckAxesSignature();     // 约 1s 检查轴配置变化（工程重载/编辑）
            Render();
            if (_tick % 16 == 0)
            {
                RefreshChips();                             // 约 250ms 刷 IO/气缸/相机
                PollRealAxes();                             // 实际轴位置/状态字联动（接卡时）
                RefreshLayoutIo();                          // 布局传感器/吸嘴颜色
            }
        }

        // ===================== 轴映射 =====================
        private void RebuildAxes()
        {
            var axes = new List<Sim2DAxis>();
            var src = ProjectStore.Data?.Axes;
            if (src != null)
            {
                foreach (var it in src)
                {
                    string name = string.IsNullOrWhiteSpace(it.Name) ? $"轴{it.AxisNo}" : it.Name;
                    double act = AxisRuntimeState.Get(name);
                    var a = new Sim2DAxis
                    {
                        Name = name,
                        Unit = string.IsNullOrWhiteSpace(it.Unit) ? "mm" : it.Unit,
                        Speed = it.Speed > 0 ? it.Speed : 100,
                        AccelTime = it.Accel > 0 ? it.Accel : 0.2,
                        DecelTime = it.Decel > 0 ? it.Decel : 0.2,
                        LimitPlus = it.PosLimitPlus,
                        LimitMinus = it.PosLimitMinus,
                        SoftLimitEnable = it.SoftLimitEnable,
                        InPos = it.InPosError > 0 ? it.InPosError : 0.01,
                        Act = act,
                        Target = act,
                    };
                    // 限位无效（未配置 / 反向 / 全 0）→ 以当前位置为中心给默认行程窗
                    if (a.LimitPlus - a.LimitMinus < 1e-6)
                    {
                        double c = Math.Abs(a.Act) > 1e-6 ? a.Act : 0;
                        a.LimitPlus = Math.Max(100, Math.Abs(c) + 50);
                        a.LimitMinus = -a.LimitPlus;
                        a.SoftLimitEnable = false;
                    }
                    axes.Add(a);
                }
            }
            if (axes.Count == 0)
            {
                axes.Add(new Sim2DAxis { Name = "X", Speed = 100 });
                axes.Add(new Sim2DAxis { Name = "Y", Speed = 100 });
                axes.Add(new Sim2DAxis { Name = "Z", Speed = 50 });
            }

            _motion.Axes.Clear();
            _motion.Axes.AddRange(axes);
            _motion.ClearTrace();
            _lastTraceCount = -1;

            var used = new HashSet<Sim2DAxis>();
            _ax = ResolveRole(axes, 'X', 0, used, out _);
            _ay = ResolveRole(axes, 'Y', 1, used, out _);
            _az = ResolveRole(axes, 'Z', 2, used, out bool zVirt);
            RowZ.Visibility = zVirt ? Visibility.Collapsed : Visibility.Visible;

            _axesSignature = BuildAxesSignature();
            _worldDirty = true;
        }

        /// <summary>角色轴解析：优先名称含角色字母的轴，其次按顺序补位，最后给虚拟轴兜底（virt=true 表示新建了虚拟轴）。</summary>
        private static Sim2DAxis ResolveRole(List<Sim2DAxis> axes, char role, int fallbackIdx, HashSet<Sim2DAxis> used, out bool virt)
        {
            foreach (var a in axes)
            {
                if (used.Contains(a)) continue;
                if (a.Name.Trim().ToUpperInvariant().Contains(role)) { used.Add(a); virt = false; return a; }
            }
            if (fallbackIdx < axes.Count && !used.Contains(axes[fallbackIdx])) { used.Add(axes[fallbackIdx]); virt = false; return axes[fallbackIdx]; }
            var v = new Sim2DAxis { Name = role.ToString(), Speed = 100 };
            axes.Add(v);
            used.Add(v);
            virt = true;
            return v;
        }

        private string BuildAxesSignature()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var a in _motion.Axes)
                sb.Append(a.Name).Append('|').Append(a.LimitMinus).Append('|').Append(a.LimitPlus)
                  .Append('|').Append(a.Speed).Append('|').Append(a.Unit).Append(';');
            return sb.ToString();
        }

        private void CheckAxesSignature()
        {
            if (BuildAxesSignature() != _axesSignature) RebuildAxes();
        }

        private Sim2DAxis AxisByRole(char role) => role switch
        {
            'X' or 'x' => _ax,
            'Y' or 'y' => _ay,
            'Z' or 'z' => _az,
            _ => _ax,
        };

        // ===================== 点动 / 回零 / 停止 =====================
        private void StartJog(string key)
        {
            if (key.Length < 2) return;
            var a = AxisByRole(key[0]);
            double dir = key[1] == '+' ? 1 : -1;
            a.JogVel = dir * Math.Max(1e-3, a.Speed) * _motion.SpeedScale;
        }

        private void StopJog()
        {
            foreach (var a in _motion.Axes) a.JogVel = 0;
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            foreach (var a in _motion.Axes) AxisRuntimeState.Set(a.Name, 0);
        }

        private void Stop_Click(object sender, RoutedEventArgs e) => _motion.StopAll();

        private void SlScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _motion.SpeedScale = SlScale.Value / 100.0;
            if (TbScale != null) TbScale.Text = $"{SlScale.Value:0}%";
        }

        private void CbTrace_Changed(object sender, RoutedEventArgs e)
        {
            _showTrace = CbTrace.IsChecked == true;
            PlTrace.Visibility = _showTrace ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ClearTrace_Click(object sender, RoutedEventArgs e)
        {
            _motion.ClearTrace();
            _lastTraceCount = 0;
            PlTrace.Points = new PointCollection();
        }

        // ===================== 渲染 =====================
        private double MapX(double wx) => _offX + (wx - _world.Left) * _scale;
        private double MapY(double wy) => Cv.ActualHeight - _offY - (wy - _world.Top) * _scale;

        private void Render()
        {
            double w = Cv.ActualWidth, h = Cv.ActualHeight;
            if (w < 40 || h < 40) return;

            // 头部贴近世界窗边缘 → 扩窗重算
            if (_ax.Act < _world.Left + 8 || _ax.Act > _world.Right - 8 ||
                _ay.Act < _world.Top + 8 || _ay.Act > _world.Bottom - 8) _worldDirty = true;

            if (_worldDirty || Math.Abs(w - _lastW) > 0.5 || Math.Abs(h - _lastH) > 0.5)
            {
                ComputeWorld(w, h);
                BuildGrid();
                _transformDirty = true;
                _lastW = w; _lastH = h;
            }

            // 行程框 / 导轨 / 横梁
            double lx = MapX(_world.Left), ly = MapY(_world.Top);
            double lw = _world.Width * _scale, lh = _world.Height * _scale;
            SetRect(RcLimit, lx, ly, lw, lh);
            SetRect(RcRailX, lx - 6, ly + 8, lw + 12, 8);
            SetRect(RcBeamY, MapX(_ax.Act) - 7, ly, 14, lh);

            // 刀头：位置=XY，内芯大小=Z 行程百分比
            double hx = MapX(_ax.Act), hy = MapY(_ay.Act);
            SetRect(ElHead, hx - 9, hy - 9, 18, 18);
            double zNorm = 0.5;
            double zSpan = _az.LimitPlus - _az.LimitMinus;
            if (zSpan > 1e-6) zNorm = Math.Clamp((_az.Act - _az.LimitMinus) / zSpan, 0, 1);
            double core = 4 + 8 * zNorm;
            SetRect(ElHeadCore, hx - core / 2, hy - core / 2, core, core);

            RenderLayout();

            // 轨迹
            if (_showTrace && (_transformDirty || _motion.Trace.Count != _lastTraceCount))
            {
                var pc = new PointCollection(_motion.Trace.Count);
                foreach (var (tx, ty) in _motion.Trace) pc.Add(new Point(MapX(tx), MapY(ty)));
                PlTrace.Points = pc;
                _lastTraceCount = _motion.Trace.Count;
            }
            _transformDirty = false;

            // 读数
            string xu = _ax.Unit, yu = _ay.Unit, zu = _az.Unit;
            TbPosX.Text = $"{_ax.Act:F3} {xu}　目标 {_ax.Target:F2}";
            TbPosY.Text = $"{_ay.Act:F3} {yu}　目标 {_ay.Target:F2}";
            TbPosZ.Text = $"{_az.Act:F3} {zu}　目标 {_az.Target:F2}";
            TbVelX.Text = $"{Math.Abs(_ax.Vel):F1} {xu}/s";
            TbVelY.Text = $"{Math.Abs(_ay.Vel):F1} {yu}/s";
            TbVelZ.Text = $"{Math.Abs(_az.Vel):F1} {zu}/s";
        }

        private static void SetRect(FrameworkElement el, double x, double y, double w, double h)
        {
            Canvas.SetLeft(el, x);
            Canvas.SetTop(el, y);
            el.Width = w;
            el.Height = h;
        }

        // ===================== 生成布局（按当前工程配置自动生成 2D 仿真） =====================

        /// <summary>「生成布局」按钮：按当前工程配置生成 2D 俯视布局——料盘阵列、真空吸嘴组（随刀头）、
        /// 轴IO限位/原点传感器；同时开启与实际轴IO的联动（优先取轴IO标定绑定的 IO 点位，
        /// 未绑定时取控制卡轴状态字 bit1正限位/bit2负限位/bit4原点）。</summary>
        private void BtnGenLayout_Click(object sender, RoutedEventArgs e)
        {
            BuildLayoutFromConfig();
            _ioLink = true;
            _worldDirty = true;
        }

        private void BuildLayoutFromConfig()
        {
            CvLayout.Children.Clear();
            _layoutRects.Clear(); _layoutDots.Clear(); _layoutTexts.Clear();
            _ioMarks.Clear(); _nozzleDots.Clear(); _nozzleLabels.Clear();
            _layoutBounds = Rect.Empty;
            void Include(Rect r) => _layoutBounds = _layoutBounds.IsEmpty ? r : Rect.Union(_layoutBounds, r);

            var data = ProjectStore.Data;

            // 1) 料盘阵列：StartX/StartY 为首格中心，PitchX/PitchY 为格距（与料盘点位一致）
            if (data?.Trays != null)
                foreach (var t in data.Trays)
                {
                    if (t == null || t.Rows <= 0 || t.Cols <= 0) continue;
                    double w = (t.Cols - 1) * t.PitchX + t.PitchX;
                    double h = (t.Rows - 1) * t.PitchY + t.PitchY;
                    double x0 = t.StartX - t.PitchX / 2, y0 = t.StartY - t.PitchY / 2;
                    var rect = new Rectangle { RadiusX = 3, RadiusY = 3, Fill = Br("#2264748B"), Stroke = Br("#64748B"), StrokeThickness = 1 };
                    CvLayout.Children.Add(rect);
                    _layoutRects.Add((rect, x0, y0, w, h));
                    Include(new Rect(x0, y0, w, h));
                    for (int r = 0; r < t.Rows; r++)
                        for (int c = 0; c < t.Cols; c++)
                        {
                            var dot = new Ellipse { Width = 6, Height = 6, Fill = Br("#F59E0B") };
                            CvLayout.Children.Add(dot);
                            _layoutDots.Add((dot, t.StartX + c * t.PitchX, t.StartY + r * t.PitchY));
                        }
                    var lbl = new TextBlock { Text = t.Name, FontSize = 10, Foreground = Br("#475569") };
                    CvLayout.Children.Add(lbl);
                    _layoutTexts.Add((lbl, x0, y0 + h + 2));
                }

            // 2) 真空吸嘴组：名称含 吸嘴/吸盘 的气缸按前缀分组，方阵挂在刀头上随动（绿=伸出/真空）
            var nozzles = (data?.Cylinders ?? new System.Collections.ObjectModel.ObservableCollection<CylinderItem>())
                .Where(c => c != null && (c.Name.Contains("吸嘴") || c.Name.Contains("吸盘"))).ToList();
            int rowIdx = 0;
            foreach (var g in nozzles.GroupBy(c => Regex.Replace(c.Name, @"\d+$", "")))
            {
                int col = 0;
                foreach (var c in g.Take(8))
                {
                    var sq = new Rectangle { RadiusX = 1, RadiusY = 1, Fill = NozzleBr, Stroke = Br("#FFFFFF"), StrokeThickness = 1, ToolTip = c.Name };
                    CvLayout.Children.Add(sq);
                    _nozzleDots.Add((sq, c.Name, rowIdx, col));
                    col++;
                }
                var lbl = new TextBlock { Text = $"{g.Key}×{g.Count()}", FontSize = 10, Foreground = Br("#475569") };
                CvLayout.Children.Add(lbl);
                _nozzleLabels.Add((lbl, rowIdx));
                rowIdx++;
            }

            // 3) 轴IO传感器：行程端点方块=正/负限位，负端内侧圆点=原点（X 在行程框下边、Y 在左边）
            AddAxisIoMarks(_ax, _ay.LimitMinus - 16, isX: true);
            AddAxisIoMarks(_ay, _ax.LimitMinus - 16, isX: false);
        }

        private void AddAxisIoMarks(Sim2DAxis a, double cross, bool isX)
        {
            var item = FindAxisItem(a.Name);
            double span = a.LimitPlus - a.LimitMinus;
            double originW = span > 1e-6 ? a.LimitMinus + span * 0.1 : a.LimitMinus;

            void Add(Shape el, double wAlong, string kind, string? bound, Func<bool?> snapBit)
            {
                (double wx, double wy) = isX ? (wAlong, cross) : (cross, wAlong);
                el.ToolTip = $"{a.Name}轴 {kind} · {(string.IsNullOrWhiteSpace(bound) ? "未绑定（控制卡状态字）" : $"IO「{bound}」")}";
                CvLayout.Children.Add(el);
                _ioMarks.Add((el, wx, wy, () => ReadAxisIo(item, bound, snapBit)));
            }

            Add(new Rectangle { Width = 9, Height = 9, RadiusX = 1, RadiusY = 1, Fill = IoNoneBr, Stroke = Br("#64748B"), StrokeThickness = 1 },
                a.LimitPlus, "正限位", item?.IoLimitPlus, () => _lastSnap.TryGetValue(a.Name, out var s) ? s.Pel : null);
            Add(new Rectangle { Width = 9, Height = 9, RadiusX = 1, RadiusY = 1, Fill = IoNoneBr, Stroke = Br("#64748B"), StrokeThickness = 1 },
                a.LimitMinus, "负限位", item?.IoLimitMinus, () => _lastSnap.TryGetValue(a.Name, out var s) ? s.Mel : null);
            Add(new Ellipse { Width = 9, Height = 9, Fill = IoNoneBr, Stroke = Br("#64748B"), StrokeThickness = 1 },
                originW, "原点", item?.IoOrigin, () => _lastSnap.TryGetValue(a.Name, out var s) ? s.Org : null);
        }

        private static int? ReadAxisIo(AxisItem? item, string? bound, Func<bool?> snapBit)
        {
            if (item != null && !string.IsNullOrWhiteSpace(bound))
            {
                var data = ProjectStore.Data;
                if (data?.Inputs != null)
                    foreach (var io in data.Inputs)
                        if (io != null && string.Equals(io.Name, bound, StringComparison.OrdinalIgnoreCase))
                            return io.Value;
                if (data?.Outputs != null)
                    foreach (var io in data.Outputs)
                        if (io != null && string.Equals(io.Name, bound, StringComparison.OrdinalIgnoreCase))
                            return io.Value;
            }
            var sb = snapBit();
            return sb == null ? null : sb == true ? 1 : 0;
        }

        private static AxisItem? FindAxisItem(string name) =>
            ProjectStore.Data?.Axes?.FirstOrDefault(x => x != null && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

        /// <summary>每帧重定位布局元素（世界坐标 → 屏幕坐标，跟随取景与缩放）。</summary>
        private void RenderLayout()
        {
            foreach (var (el, wx, wy, ww, wh) in _layoutRects)
                SetRect(el, MapX(wx), MapY(wy + wh), Math.Max(1, ww * _scale), Math.Max(1, wh * _scale));
            foreach (var (el, wx, wy) in _layoutDots)
                SetRect(el, MapX(wx) - 3, MapY(wy) - 3, 6, 6);
            foreach (var (el, wx, wy) in _layoutTexts)
            { Canvas.SetLeft(el, MapX(wx)); Canvas.SetTop(el, MapY(wy)); }
            foreach (var (el, wx, wy, _) in _ioMarks)
                SetRect(el, MapX(wx) - 4.5, MapY(wy) - 4.5, 9, 9);

            // 吸嘴方阵跟随刀头（屏幕像素偏移，4 列网格，多组纵向堆叠）
            double hx = MapX(_ax.Act), hy = MapY(_ay.Act);
            foreach (var (el, _, row, col) in _nozzleDots)
                SetRect(el, hx - 20 + (col % 4) * 10, hy - 34 - row * 26 + (col / 4) * 10, 8, 8);
            foreach (var (el, row) in _nozzleLabels)
            { Canvas.SetLeft(el, hx - 20); Canvas.SetTop(el, hy - 46 - row * 26); }
        }

        /// <summary>约 250ms 刷新布局 IO 颜色：传感器 绿=有效 / 灰=无效；吸嘴 绿=伸出(真空) / 蓝=缩回。</summary>
        private void RefreshLayoutIo()
        {
            foreach (var (el, _, _, val) in _ioMarks)
            {
                Brush t = val() switch { 1 => IoOnBr, 0 => IoOffBr, _ => IoNoneBr };
                if (!ReferenceEquals(el.Fill, t)) el.Fill = t;
            }
            foreach (var (el, cyl, _, _) in _nozzleDots)
            {
                Brush t = SimRuntime.GetCylinder(cyl) == 1 ? NozzleOnBr : NozzleBr;
                if (!ReferenceEquals(el.Fill, t)) el.Fill = t;
            }
        }

        /// <summary>实际轴位置联动：接卡时轮询 X/Y/Z 轴反馈位置写入 AxisRuntimeState（2D/3D 同步跟随真实轴），
        /// 并缓存轴状态字供布局传感器显示。点动中的轴跳过（由 2D 内核持有目标）。</summary>
        private void PollRealAxes()
        {
            if (!_ioLink || !HardwareSetup.IsCardReady) return;
            foreach (var a in new[] { _ax, _ay, _az })
            {
                if (a == null || a.JogVel != 0) continue;
                var item = FindAxisItem(a.Name);
                if (item == null) continue;
                AxisStatusSnapshot snap;
                try { snap = AxisMonitorService.Read(item); } catch { continue; }
                _lastSnap[a.Name] = snap;
                if (snap.Connected) AxisRuntimeState.Set(a.Name, snap.Position);
            }
        }

        private void ComputeWorld(double w, double h)
        {
            double x0 = _ax.LimitMinus, x1 = _ax.LimitPlus;
            double y0 = _ay.LimitMinus, y1 = _ay.LimitPlus;
            if (!_ax.SoftLimitEnable || x1 - x0 < 1e-6) { x0 = _ax.Act - 60; x1 = _ax.Act + 60; }
            if (!_ay.SoftLimitEnable || y1 - y0 < 1e-6) { y0 = _ay.Act - 60; y1 = _ay.Act + 60; }
            x0 = Math.Min(x0, _ax.Act - 10); x1 = Math.Max(x1, _ax.Act + 10);
            y0 = Math.Min(y0, _ay.Act - 10); y1 = Math.Max(y1, _ay.Act + 10);
            if (!_layoutBounds.IsEmpty)
            {
                x0 = Math.Min(x0, _layoutBounds.Left - 10); x1 = Math.Max(x1, _layoutBounds.Right + 10);
                y0 = Math.Min(y0, _layoutBounds.Top - 10); y1 = Math.Max(y1, _layoutBounds.Bottom + 10);
            }
            _world = new Rect(x0, y0, x1 - x0, y1 - y0);

            const double margin = 30;
            _scale = Math.Min((w - 2 * margin) / _world.Width, (h - 2 * margin) / _world.Height);
            if (_scale <= 0 || double.IsInfinity(_scale) || double.IsNaN(_scale)) _scale = 1;
            _offX = (w - _world.Width * _scale) / 2;
            _offY = (h - _world.Height * _scale) / 2;
            _worldDirty = false;
        }

        private void BuildGrid()
        {
            CvGrid.Children.Clear();
            double step = NiceStep(_world.Width / 8);
            var gridBrush = Br("#E2E8F0");
            var zeroBrush = Br("#CBD5E1");

            for (double gx = Math.Ceiling(_world.Left / step) * step; gx <= _world.Right + 1e-9; gx += step)
            {
                bool zero = Math.Abs(gx) < step / 2;
                var l = new Line
                {
                    X1 = MapX(gx), Y1 = MapY(_world.Top),
                    X2 = MapX(gx), Y2 = MapY(_world.Bottom),
                    Stroke = zero ? zeroBrush : gridBrush,
                    StrokeThickness = zero ? 1.2 : 1,
                };
                CvGrid.Children.Add(l);
            }
            for (double gy = Math.Ceiling(_world.Top / step) * step; gy <= _world.Bottom + 1e-9; gy += step)
            {
                bool zero = Math.Abs(gy) < step / 2;
                var l = new Line
                {
                    X1 = MapX(_world.Left), Y1 = MapY(gy),
                    X2 = MapX(_world.Right), Y2 = MapY(gy),
                    Stroke = zero ? zeroBrush : gridBrush,
                    StrokeThickness = zero ? 1.2 : 1,
                };
                CvGrid.Children.Add(l);
            }

            // 原点十字
            var o = Br("#64748B");
            CvGrid.Children.Add(new Line { X1 = MapX(-step / 2), Y1 = MapY(0), X2 = MapX(step / 2), Y2 = MapY(0), Stroke = o, StrokeThickness = 1.4 });
            CvGrid.Children.Add(new Line { X1 = MapX(0), Y1 = MapY(-step / 2), X2 = MapX(0), Y2 = MapY(step / 2), Stroke = o, StrokeThickness = 1.4 });

            TbGridStep.Text = $"格 {step:0.###} {_ax.Unit}";
        }

        private static double NiceStep(double raw)
        {
            if (raw <= 0) return 1;
            double pow = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            double r = raw / pow;
            double m = r >= 5 ? 5 : r >= 2 ? 2 : 1;
            return m * pow;
        }

        // ===================== IO / 气缸 / 相机状态 =====================
        private void RefreshChips()
        {
            var data = ProjectStore.Data;
            int sig = (data?.Outputs.Count ?? 0) * 1000000 + (data?.Cylinders.Count ?? 0) * 1000 + (data?.Cameras.Count ?? 0);
            if (sig != _chipSignature) { RebuildChips(data); _chipSignature = sig; }
            if (_chipSignature == 0) return;

            var on = Br("#10B981");
            var off = Br("#CBD5E1");
            var flash = Br("#F59E0B");
            var now = DateTime.Now;
            foreach (var (dot, kind, item) in _chipDots)
            {
                switch (kind)
                {
                    case "io":
                        dot.Fill = SimRuntime.GetOutput(((IoItem)item).Name) == 1 ? on : off;
                        break;
                    case "cyl":
                        dot.Fill = SimRuntime.GetCylinder(((CylinderItem)item).Name) == 1 ? on : off;
                        break;
                    case "cam":
                        var t = SimRuntime.GetCamFlash(((CameraItem)item).Name);
                        dot.Fill = (now - t).TotalMilliseconds < 400 ? flash : off;
                        break;
                }
            }
        }

        private void RebuildChips(ProjectData? data)
        {
            PnChips.Children.Clear();
            _chipDots.Clear();

            int nOut = data?.Outputs.Count ?? 0;
            int nCyl = data?.Cylinders.Count ?? 0;
            int nCam = data?.Cameras.Count ?? 0;
            if (nOut + nCyl + nCam == 0)
            {
                PnChips.Children.Add(new TextBlock
                {
                    Text = "未配置 输出 / 气缸 / 相机",
                    FontSize = 11,
                    Foreground = Br("#94A3B8"),
                    Margin = new Thickness(2, 2, 0, 0),
                });
                return;
            }

            void AddLabel(string t) => PnChips.Children.Add(new TextBlock
            {
                Text = t,
                FontSize = 11,
                Foreground = Br("#64748B"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 2, 6, 0),
            });

            void AddDotChip(string kind, object item, string name)
            {
                var dot = new Ellipse { Width = 7, Height = 7, Fill = Br("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
                var tb = new TextBlock
                {
                    Text = name,
                    FontSize = 10.5,
                    Foreground = Br("#334155"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 0, 0),
                    MaxWidth = 90,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                };
                var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                sp.Children.Add(dot);
                sp.Children.Add(tb);
                var bd = new Border
                {
                    Background = Br("#F1F5F9"),
                    CornerRadius = new CornerRadius(9),
                    Padding = new Thickness(7, 2, 7, 2),
                    Margin = new Thickness(0, 2, 6, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = sp,
                    ToolTip = name,
                };
                PnChips.Children.Add(bd);
                _chipDots.Add((dot, kind, item));
            }

            if (nOut > 0)
            {
                AddLabel("输出");
                int shown = 0;
                foreach (var io in data!.Outputs)
                {
                    if (shown++ >= 12) break;
                    AddDotChip("io", io, io.Name);
                }
            }
            if (nCyl > 0)
            {
                AddLabel("气缸");
                int shown = 0;
                foreach (var c in data!.Cylinders)
                {
                    if (shown++ >= 8) break;
                    AddDotChip("cyl", c, c.Name);
                }
            }
            if (nCam > 0)
            {
                AddLabel("相机");
                int shown = 0;
                foreach (var c in data!.Cameras)
                {
                    if (shown++ >= 6) break;
                    AddDotChip("cam", c, c.Name);
                }
            }
        }

        private static SolidColorBrush Br(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }

        /// <summary>极简命令（视图内部用，避免引入 ViewModel 依赖）。</summary>
        private sealed class MiniRelay : ICommand
        {
            private readonly Action<object?> _exec;
            public MiniRelay(Action<object?> exec) => _exec = exec;
            public bool CanExecute(object? parameter) => true;
            public void Execute(object? parameter) => _exec(parameter);
            public event EventHandler? CanExecuteChanged { add { } remove { } }
        }
    }
}
// ◇作者保留所有权利　请勿删除※
