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
            if (_tick % 16 == 0) RefreshChips();            // 约 250ms 刷 IO/气缸/相机
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

        private void ComputeWorld(double w, double h)
        {
            double x0 = _ax.LimitMinus, x1 = _ax.LimitPlus;
            double y0 = _ay.LimitMinus, y1 = _ay.LimitPlus;
            if (!_ax.SoftLimitEnable || x1 - x0 < 1e-6) { x0 = _ax.Act - 60; x1 = _ax.Act + 60; }
            if (!_ay.SoftLimitEnable || y1 - y0 < 1e-6) { y0 = _ay.Act - 60; y1 = _ay.Act + 60; }
            x0 = Math.Min(x0, _ax.Act - 10); x1 = Math.Max(x1, _ax.Act + 10);
            y0 = Math.Min(y0, _ay.Act - 10); y1 = Math.Max(y1, _ay.Act + 10);
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
