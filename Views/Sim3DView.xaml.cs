// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// 运行轨迹 3D 仿真控件（参数化机台）。
// 机台几何根据 ProjectStore.Data.Axes 自动生成（直线轴→导轨/滑座/主轴，旋转轴→转台），
// 三轴联动由 AxisRuntimeState（流程/单步运行时实时写入的轴位置）驱动；
// 流程「相机」步骤真实取帧后，抓拍图显示在卡片内的 CaptureImage 预览。
// 支持鼠标拖拽旋转、滚轮缩放。
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.Views
{
    /// <summary>运行轨迹 3D 仿真控件（参数化机台，由轴配置自动生成）。</summary>
    public partial class Sim3DView : UserControl
    {
        // ===== 场景参数 =====
        private const double NormHalf = 40;   // 点位归一化半幅
        private const double WorkX = 50;      // 机台 X 工作空间半幅（场景 X 轴）
        private const double WorkZ = 35;      // 机台 depth 工作空间半幅（场景 Z 轴）
        private const double WorkY = 25;      // 机台 vertical 工作空间半幅（场景 Y 轴）
        private const double BaseY = -10;     // 床面以上基准高度

        // ===== 部件配色 =====
        private static readonly Color C_Bed = Color.FromRgb(0x47, 0x55, 0x69);
        private static readonly Color C_Frame = Color.FromRgb(0x64, 0x74, 0x8B);
        private static readonly Color C_Carriage = Color.FromRgb(0x94, 0xA3, 0xB8);
        private static readonly Color C_Spindle = Color.FromRgb(0x8B, 0x5C, 0xF6);
        private static readonly Color C_ToolTip = Color.FromRgb(0xF9, 0x73, 0x16);
        private static readonly Color C_RotaryBase = Color.FromRgb(0x33, 0x3D, 0x4D);
        private static readonly Color C_Rotary = Color.FromRgb(0x0E, 0xA5, 0xE9);
        private static readonly Color C_CameraBody = Color.FromRgb(0x1E, 0x29, 0x3B);
        private static readonly Color C_CameraLens = Color.FromRgb(0x94, 0xA3, 0xB8);
        private static readonly Color C_Workpiece = Color.FromRgb(0x3B, 0x82, 0xF6);
        private static readonly Color C_Traj = Color.FromRgb(0x3B, 0x82, 0xF6);
        private static readonly Color C_Point = Color.FromRgb(0x60, 0xA5, 0xFA);
        private static readonly Color C_Head = Color.FromRgb(0xEF, 0x44, 0x44);
        private static readonly Color C_LabelFill = Color.FromArgb(235, 255, 255, 255);
        private static readonly Color C_LabelBorder = Color.FromRgb(0x47, 0x55, 0x69);
        private static readonly Color C_LabelText = Color.FromRgb(0x1E, 0x29, 0x3B);

        // ===== 依赖属性 =====
        public static readonly DependencyProperty PointsProperty =
            DependencyProperty.Register(nameof(Points), typeof(Point3DCollection), typeof(Sim3DView),
                new PropertyMetadata(null, (o, e) => ((Sim3DView)o).BuildScene()));

        public Point3DCollection? Points
        {
            get => (Point3DCollection?)GetValue(PointsProperty);
            set => SetValue(PointsProperty, value);
        }

        public static readonly DependencyProperty HeadProperty =
            DependencyProperty.Register(nameof(Head), typeof(Point3D), typeof(Sim3DView),
                new PropertyMetadata(new Point3D(0, 0, 0), (o, e) => ((Sim3DView)o).UpdateHead()));

        public Point3D Head
        {
            get => (Point3D)GetValue(HeadProperty);
            set => SetValue(HeadProperty, value);
        }

        public static readonly DependencyProperty HeadVisibleProperty =
            DependencyProperty.Register(nameof(HeadVisible), typeof(bool), typeof(Sim3DView),
                new PropertyMetadata(false, (o, e) => ((Sim3DView)o).BuildScene()));

        public bool HeadVisible
        {
            get => (bool)GetValue(HeadVisibleProperty);
            set => SetValue(HeadVisibleProperty, value);
        }

        public static readonly DependencyProperty CurrentIndexProperty =
            DependencyProperty.Register(nameof(CurrentIndex), typeof(int), typeof(Sim3DView),
                new PropertyMetadata(-1, (o, e) => ((Sim3DView)o).UpdateCurrent()));

        public int CurrentIndex
        {
            get => (int)GetValue(CurrentIndexProperty);
            set => SetValue(CurrentIndexProperty, value);
        }

        /// <summary>流程「相机」步骤抓拍到的帧（BGRA BitmapSource），由 OperatorViewModel 设置，卡片内预览显示。</summary>
        public static readonly DependencyProperty CaptureImageProperty =
            DependencyProperty.Register(nameof(CaptureImage), typeof(ImageSource), typeof(Sim3DView),
                new PropertyMetadata(null));

        public ImageSource? CaptureImage
        {
            get => (ImageSource?)GetValue(CaptureImageProperty);
            set => SetValue(CaptureImageProperty, value);
        }

        // ===== 相机轨道参数 =====
        private double _theta = 0.7;
        private double _phi = 0.5;
        private double _radius = 240;
        private bool _dragging;
        private Point _last;

        // ===== 场景变换（点位归一化） =====
        private double _scale = 1;
        private Point3D _center = new(0, 0, 0);

        // ===== 共享几何（冻结） =====
        private readonly MeshGeometry3D _sphere = BuildSphere();
        private readonly MeshGeometry3D _cyl = BuildCylinder();
        private readonly MeshGeometry3D _box = BuildBox();

        // ===== 运行时引用 =====
        private readonly List<GeometryModel3D> _pointModels = new();
        private GeometryModel3D? _headModel;
        private ModelVisual3D? _machineGroup;
        private readonly Dictionary<char, ModelVisual3D> _linearGroups = new(); // 场景轴 X/D/U -> 组
        private ModelVisual3D? _deepestLinear;
        private readonly Dictionary<char, ModelVisual3D> _rotaryTops = new();   // 旋转轴角色 -> 旋转顶面
        private ModelVisual3D? _workpieceParent;
        private Point3D _vmHead = new(0, 0, 0);

        // 轴分类结果
        private readonly List<AxisInfo> _linearInfos = new();
        private readonly List<AxisInfo> _rotaryInfos = new();
        private AxisInfo? _axialX, _axialYdepth, _axialZup;

        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(33) };

        public Sim3DView()
        {
            InitializeComponent();
            UpdateCamera();

            Vp.MouseDown += (s, e) =>
            {
                Vp.CaptureMouse();
                _dragging = true;
                _last = e.GetPosition(Vp);
            };
            Vp.MouseMove += (s, e) =>
            {
                if (!_dragging) return;
                var p = e.GetPosition(Vp);
                double dx = p.X - _last.X;
                double dy = p.Y - _last.Y;
                _last = p;
                _theta -= dx * 0.01;
                _phi -= dy * 0.01;
                _phi = Math.Max(-1.45, Math.Min(1.45, _phi));
                UpdateCamera();
            };
            Vp.MouseUp += (s, e) =>
            {
                _dragging = false;
                Vp.ReleaseMouseCapture();
            };
            Vp.MouseWheel += (s, e) =>
            {
                _radius *= (1 + e.Delta * 0.0008);
                _radius = Math.Max(100, Math.Min(800, _radius));
                UpdateCamera();
            };

            _timer.Tick += (s, e) => UpdatePoseFromRuntime();

            if (ProjectStore.Data?.Axes is INotifyCollectionChanged nc)
                nc.CollectionChanged += (s, e) => Dispatcher.Invoke(BuildScene);

            BuildScene();
            _timer.Start();
        }

        // ===================== 场景构建 =====================
        private void BuildScene()
        {
            Root.Children.Clear();
            _pointModels.Clear();
            _headModel = null;
            _machineGroup = null;
            _linearGroups.Clear();
            _rotaryTops.Clear();
            _workpieceParent = null;
            _deepestLinear = null;
            _axialX = _axialYdepth = _axialZup = null;
            _linearInfos.Clear();
            _rotaryInfos.Clear();
            _scale = 1;
            _center = new Point3D(0, 0, 0);

            var raw = Points;
            EmptyHint.Visibility = (raw == null || raw.Count == 0) ? Visibility.Visible : Visibility.Collapsed;

            // 参数化机台：始终根据轴配置生成（即使暂无点位也展示机台）
            BuildMachine();

            if (raw != null && raw.Count > 0)
            {
                // 计算包围盒 → 归一化到 80 尺度并居中
                double minX = double.MaxValue, maxX = double.MinValue;
                double minY = double.MaxValue, maxY = double.MinValue;
                double minZ = double.MaxValue, maxZ = double.MinValue;
                foreach (var p in raw)
                {
                    if (p.X < minX) minX = p.X; if (p.X > maxX) maxX = p.X;
                    if (p.Y < minY) minY = p.Y; if (p.Y > maxY) maxY = p.Y;
                    if (p.Z < minZ) minZ = p.Z; if (p.Z > maxZ) maxZ = p.Z;
                }
                var ctr = new Point3D((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);
                double maxDim = Math.Max(maxX - minX, Math.Max(maxY - minY, maxZ - minZ));
                if (maxDim < 1e-6) maxDim = 1;
                _center = ctr;
                _scale = 80.0 / maxDim;

                double groundY = -52;
                double span = 110;
                var plane = new MeshGeometry3D();
                plane.Positions.Add(new Point3D(-span, groundY, -span));
                plane.Positions.Add(new Point3D(span, groundY, -span));
                plane.Positions.Add(new Point3D(span, groundY, span));
                plane.Positions.Add(new Point3D(-span, groundY, span));
                plane.TriangleIndices.Add(0); plane.TriangleIndices.Add(1); plane.TriangleIndices.Add(2);
                plane.TriangleIndices.Add(0); plane.TriangleIndices.Add(2); plane.TriangleIndices.Add(3);
                AddModel(Root, plane, 1, 1, 1, new Point3D(0, 0, 0), Color.FromArgb(40, 226, 232, 240), null);
            }

            // ===== 轨迹 + 点位（来自点位表 / 流程点位步骤） =====
            if (raw != null && raw.Count > 0)
            {
                var pts = new Point3D[raw.Count];
                for (int i = 0; i < raw.Count; i++) pts[i] = ToMachine(ToSceneSafe(raw[i]));
                for (int i = 0; i < pts.Length - 1; i++)
                    AddSegment(pts[i], pts[i + 1], 1.2, C_Traj);
                for (int i = 0; i < pts.Length; i++)
                {
                    var col = i == 0 ? C_Traj : C_Point;
                    _pointModels.Add(AddModel(Root, _sphere, 3.2, 3.2, 3.2, pts[i], col));
                }
            }

            if (HeadVisible)
                _headModel = AddModel(Root, _sphere, 4.6, 4.6, 4.6, new Point3D(0, 0, 0), C_Head);

            UpdatePoseFromRuntime();
            UpdateCurrent();
        }

        private void BuildMachine()
        {
            ClassifyAxes();

            _machineGroup = new ModelVisual3D();
            Root.Children.Add(_machineGroup);

            // 床身 + 4 底脚
            AddModel(_machineGroup, _box, 130, 4, 90, new Point3D(0, -12, 0), C_Bed);
            foreach (var cx in new[] { -55.0, 55.0 })
                foreach (var cz in new[] { -35.0, 35.0 })
                    AddModel(_machineGroup, _cyl, 5, 10, 5, new Point3D(cx, -18, cz), C_Bed);

            // 直线轴：嵌套组 X ⊃ D(depth) ⊃ U(up)，每个组平移对应场景轴
            ModelVisual3D parent = _machineGroup;
            if (_axialX != null) { var g = new ModelVisual3D(); parent.Children.Add(g); _linearGroups['X'] = g; AddGantryStructure(g); parent = g; }
            if (_axialYdepth != null) { var g = new ModelVisual3D(); parent.Children.Add(g); _linearGroups['D'] = g; AddBridgeStructure(g); parent = g; }
            if (_axialZup != null) { var g = new ModelVisual3D(); parent.Children.Add(g); _linearGroups['U'] = g; AddSpindleStructure(g); parent = g; _deepestLinear = parent; }

            // 旋转轴：床面转台（绕场景 Y/X/Z 轴）
            foreach (var r in _rotaryInfos)
            {
                var baseC = new ModelVisual3D();
                _machineGroup.Children.Add(baseC);
                AddModel(baseC, _cyl, 18, 3, 18, new Point3D(0, -9, 0), C_RotaryBase);
                var top = new ModelVisual3D();
                baseC.Children.Add(top);
                AddModel(top, _cyl, 16, 4, 16, new Point3D(0, -6, 0), C_Rotary);
                _rotaryTops[r.Role] = top;
                if (_workpieceParent == null) _workpieceParent = top; // 工件随第一个旋转轴转
            }

            // 工件（蓝块）：放在旋转台上（若有），否则床面
            if (_workpieceParent == null) _workpieceParent = _machineGroup;
            AddModel(_workpieceParent, _box, 22, 6, 18, new Point3D(0, -2, 0), C_Workpiece);

            // 相机模型（静止，前上方俯视床面）
            AddModel(_machineGroup, _box, 14, 10, 18, new Point3D(0, 55, -58), C_CameraBody);
            AddModel(_machineGroup, _cyl, 8, 8, 8, new Point3D(0, 46, -58), C_CameraLens);
            AddModel(_machineGroup, _cyl, 3, 40, 3, new Point3D(0, 34, -58), C_CameraBody);

            BuildLabels();
        }

        // 龙门结构（随 X 平移）
        private void AddGantryStructure(ModelVisual3D g)
        {
            AddModel(g, _box, 8, 55, 8, new Point3D(0, 15, -32), C_Frame);
            AddModel(g, _box, 8, 55, 8, new Point3D(0, 15, 32), C_Frame);
            AddModel(g, _box, 8, 8, 72, new Point3D(0, 42, 0), C_Frame); // 顶部横梁（沿 depth）
        }
        // 桥式滑座（随 depth 平移）
        private void AddBridgeStructure(ModelVisual3D g)
        {
            AddModel(g, _box, 110, 10, 12, new Point3D(0, 38, 0), C_Carriage);
        }
        // 主轴/刀具（随 up 平移）
        private void AddSpindleStructure(ModelVisual3D g)
        {
            AddModel(g, _cyl, 10, 20, 10, new Point3D(0, 12, 0), C_Spindle);
            AddModel(g, _box, 4, 8, 4, new Point3D(0, -2, 0), C_ToolTip);
        }

        private void BuildLabels()
        {
            _machineGroup!.Children.Add(MakeLabel("相机", new Point3D(0, 64, -58)));
            _machineGroup.Children.Add(MakeLabel("工件", new Point3D(0, 4, 24)));
            foreach (var a in _linearInfos)
                _machineGroup.Children.Add(MakeLabel(a.Name, LabelPos(a)));
            foreach (var r in _rotaryInfos)
                _machineGroup.Children.Add(MakeLabel(r.Name, new Point3D(24, 2, 0)));
        }

        private Point3D LabelPos(AxisInfo a)
        {
            if (a == _axialX) return new Point3D(-58, 48, 0);
            if (a == _axialYdepth) return new Point3D(0, 48, 44);
            if (a == _axialZup) return new Point3D(42, 30, 0);
            return new Point3D(0, 48, 0);
        }

        // 根据轴配置分类（直线/旋转 + 场景角色 + 行程范围）
        private void ClassifyAxes()
        {
            _axialX = _axialYdepth = _axialZup = null;
            _linearInfos.Clear();
            _rotaryInfos.Clear();
            var axes = ProjectStore.Data?.Axes;
            if (axes == null) return;
            var linear = new List<AxisInfo>();
            foreach (var a in axes)
            {
                bool isRot = string.Equals(a.Unit, "°", StringComparison.OrdinalIgnoreCase);
                char role = isRot ? PickRotaryRole(a.Name) : PickLinearRole(a.Name);
                double mn = a.PosLimitMinus, mx = a.PosLimitPlus;
                if (Math.Abs(mx - mn) < 1e-6) { mn = -100; mx = 100; }
                var info = new AxisInfo { Name = a.Name, Role = role, IsRotary = isRot, Min = mn, Max = mx };
                if (isRot) _rotaryInfos.Add(info); else linear.Add(info);
            }
            // 前 3 个直线轴依次映射到 场景 X / depth / up
            for (int i = 0; i < linear.Count; i++)
            {
                var li = linear[i];
                if (i == 0) { li.Scene = 'X'; _axialX = li; }
                else if (i == 1) { li.Scene = 'D'; _axialYdepth = li; }
                else if (i == 2) { li.Scene = 'U'; _axialZup = li; }
                _linearInfos.Add(li);
            }
        }

        private static char PickLinearRole(string name)
        {
            if (name.Contains('X') || name.Contains('x')) return 'X';
            if (name.Contains('Y') || name.Contains('y')) return 'Y';
            if (name.Contains('Z') || name.Contains('z')) return 'Z';
            if (name.Contains('U') || name.Contains('u')) return 'U';
            return 'L';
        }
        private static char PickRotaryRole(string name)
        {
            if (name.Contains('A') || name.Contains('a') || name.Contains('R') || name.Contains('r')) return 'A';
            if (name.Contains('B') || name.Contains('b')) return 'B';
            if (name.Contains('C') || name.Contains('c')) return 'C';
            return 'A';
        }

        private double NormPos(AxisInfo a)
        {
            double mid = (a.Min + a.Max) / 2;
            double half = Math.Max(1e-6, (a.Max - a.Min) / 2);
            double t = (AxisRuntimeState.Get(a.Name) - mid) / half;
            return Math.Max(-1, Math.Min(1, t));
        }

        // 每帧：用 AxisRuntimeState 驱动机台各轴组 + 当前位置头
        private void UpdatePoseFromRuntime()
        {
            if (_machineGroup == null) return;

            double x = _axialX != null ? NormPos(_axialX) * WorkX : 0;
            double z = _axialYdepth != null ? NormPos(_axialYdepth) * WorkZ : 0;
            double yUp = (_axialZup != null ? NormPos(_axialZup) * WorkY : 0) + BaseY;

            if (_linearGroups.TryGetValue('X', out var gx)) gx.Transform = new TranslateTransform3D(x, 0, 0);
            if (_linearGroups.TryGetValue('D', out var gd)) gd.Transform = new TranslateTransform3D(0, 0, z);
            if (_linearGroups.TryGetValue('U', out var gu)) gu.Transform = new TranslateTransform3D(0, yUp, 0);

            foreach (var r in _rotaryInfos)
            {
                if (_rotaryTops.TryGetValue(r.Role, out var top))
                {
                    var axisVec = r.Role == 'A' ? new Vector3D(0, 1, 0)
                                : r.Role == 'B' ? new Vector3D(1, 0, 0)
                                : new Vector3D(0, 0, 1);
                    double deg = AxisRuntimeState.Get(r.Name); // 旋转轴单位 °，直接作角度
                    top.Transform = new RotateTransform3D(new AxisAngleRotation3D(axisVec, deg));
                }
            }

            PlaceHead(x, yUp, z);
        }

        private void PlaceHead(double x, double yUp, double z)
        {
            if (_headModel == null) return;
            Point3D eff;
            bool live = AxisRuntimeState.HasAny && (_axialX != null || _axialYdepth != null || _axialZup != null);
            if (live)
                eff = new Point3D(x, yUp - 8, z);     // 跟随实时轴位置（刀尖）
            else
                eff = ToMachine(_vmHead);              // 跟随点位表路径（OpSimHead）
            var g = new Transform3DGroup();
            g.Children.Add(new ScaleTransform3D(4.6, 4.6, 4.6));
            g.Children.Add(new TranslateTransform3D(eff.X, eff.Y, eff.Z));
            _headModel.Transform = g;
        }

        private void UpdateHead()
        {
            _vmHead = Head;
            UpdatePoseFromRuntime();
        }

        private void UpdateCurrent()
        {
            for (int i = 0; i < _pointModels.Count; i++)
            {
                var col = i == 0 ? C_Traj : C_Point;
                if (i == CurrentIndex) col = Colors.Orange;
                _pointModels[i].Material = new DiffuseMaterial(new SolidColorBrush(col));
                _pointModels[i].BackMaterial = new DiffuseMaterial(new SolidColorBrush(col));
            }
        }

        // 归一化坐标 → 机台工作空间坐标
        private Point3D ToMachine(Point3D n) => new(
            n.X * WorkX / NormHalf,
            -10 + (n.Y / NormHalf) * WorkY,
            n.Z * WorkZ / NormHalf);

        private Point3D ToSceneSafe(Point3D r) => new((r.X - _center.X) * _scale, (r.Y - _center.Y) * _scale, (r.Z - _center.Z) * _scale);

        // ===================== 文字标签（Viewport2DVisual3D） =====================
        // 关键：必须给 Material 设置 IsVisualHostMaterial=true，否则 Visual（文字）不渲染；Brush 用 White（Transparent 会让宿主整体不可见）。
        private Viewport2DVisual3D MakeLabel(string text, Point3D position)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(C_LabelFill),
                BorderBrush = new SolidColorBrush(C_LabelBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(5, 1, 5, 1),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 10.5,
                    Foreground = new SolidColorBrush(C_LabelText),
                    FontWeight = FontWeights.Medium
                }
            };

            var hostMat = new DiffuseMaterial(new SolidColorBrush(Colors.White));
            Viewport2DVisual3D.SetIsVisualHostMaterial(hostMat, true);

            var mesh = new MeshGeometry3D();
            double w = 64, h = 16;
            mesh.Positions.Add(new Point3D(-w / 2, -h / 2, 0));
            mesh.Positions.Add(new Point3D(w / 2, -h / 2, 0));
            mesh.Positions.Add(new Point3D(w / 2, h / 2, 0));
            mesh.Positions.Add(new Point3D(-w / 2, h / 2, 0));
            mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(1); mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(2); mesh.TriangleIndices.Add(3);

            return new Viewport2DVisual3D
            {
                Geometry = mesh,
                Material = hostMat,
                Visual = border,
                Transform = new TranslateTransform3D(position.X, position.Y, position.Z)
            };
        }

        // ===================== 几何辅助 =====================
        private GeometryModel3D AddModel(ModelVisual3D parent, MeshGeometry3D mesh, double sx, double sy, double sz,
            Point3D pos, Color color, Transform3D? rot = null)
        {
            var mat = new DiffuseMaterial(new SolidColorBrush(color));
            var g = new Transform3DGroup();
            if (rot != null) g.Children.Add(rot);
            g.Children.Add(new ScaleTransform3D(sx, sy, sz));
            g.Children.Add(new TranslateTransform3D(pos.X, pos.Y, pos.Z));
            var gm = new GeometryModel3D(mesh, mat) { Transform = g, BackMaterial = mat };
            parent.Children.Add(new ModelVisual3D { Content = gm });
            return gm;
        }

        private void AddSegment(Point3D a, Point3D b, double radius, Color color)
        {
            Vector3D dir = b - a;
            double len = dir.Length;
            if (len < 1e-6) return;
            Vector3D up = new(0, 1, 0);
            Transform3D? rot = null;
            Vector3D axis = Vector3D.CrossProduct(up, dir);
            if (axis.Length > 1e-6)
            {
                axis.Normalize();
                double ang = Vector3D.AngleBetween(up, dir);
                rot = new RotateTransform3D(new AxisAngleRotation3D(axis, ang));
            }
            var mid = new Point3D(a.X + dir.X * 0.5, a.Y + dir.Y * 0.5, a.Z + dir.Z * 0.5);
            AddModel(Root, _cyl, radius, len, radius, mid, color, rot);
        }

        private void UpdateCamera()
        {
            double r = _radius;
            var pos = new Point3D(
                r * Math.Cos(_phi) * Math.Cos(_theta),
                r * Math.Sin(_phi),
                r * Math.Cos(_phi) * Math.Sin(_theta));
            Cam.Position = pos;
            Cam.LookDirection = new Vector3D(-pos.X, -pos.Y, -pos.Z);
            Cam.UpDirection = new Vector3D(0, 1, 0);
        }

        // ===================== 基础网格生成 =====================
        private static MeshGeometry3D BuildSphere(int stacks = 14, int slices = 18)
        {
            var m = new MeshGeometry3D();
            for (int i = 0; i <= stacks; i++)
            {
                double phi = Math.PI * i / stacks;
                for (int j = 0; j <= slices; j++)
                {
                    double theta = 2 * Math.PI * j / slices;
                    m.Positions.Add(new Point3D(
                        Math.Sin(phi) * Math.Cos(theta),
                        Math.Cos(phi),
                        Math.Sin(phi) * Math.Sin(theta)));
                }
            }
            for (int i = 0; i < stacks; i++)
            {
                for (int j = 0; j < slices; j++)
                {
                    int a = i * (slices + 1) + j;
                    int b = a + slices + 1;
                    m.TriangleIndices.Add(a); m.TriangleIndices.Add(b); m.TriangleIndices.Add(a + 1);
                    m.TriangleIndices.Add(b); m.TriangleIndices.Add(b + 1); m.TriangleIndices.Add(a + 1);
                }
            }
            m.Freeze();
            return m;
        }

        private static MeshGeometry3D BuildCylinder(int slices = 18)
        {
            var m = new MeshGeometry3D();
            for (int j = 0; j <= slices; j++)
            {
                double theta = 2 * Math.PI * j / slices;
                double x = Math.Cos(theta), z = Math.Sin(theta);
                m.Positions.Add(new Point3D(x, 0.5, z));
                m.Positions.Add(new Point3D(x, -0.5, z));
            }
            for (int j = 0; j < slices; j++)
            {
                int a = j * 2, b = a + 1, c = a + 2, d = a + 3;
                m.TriangleIndices.Add(a); m.TriangleIndices.Add(b); m.TriangleIndices.Add(c);
                m.TriangleIndices.Add(b); m.TriangleIndices.Add(d); m.TriangleIndices.Add(c);
            }
            int top = m.Positions.Count; m.Positions.Add(new Point3D(0, 0.5, 0));
            int bot = m.Positions.Count; m.Positions.Add(new Point3D(0, -0.5, 0));
            for (int j = 0; j < slices; j++)
            {
                int a = j * 2;
                m.TriangleIndices.Add(top); m.TriangleIndices.Add(a); m.TriangleIndices.Add(a + 2);
                m.TriangleIndices.Add(bot); m.TriangleIndices.Add(a + 3); m.TriangleIndices.Add(a + 1);
            }
            m.Freeze();
            return m;
        }

        private static MeshGeometry3D BuildBox()
        {
            var m = new MeshGeometry3D();
            double h = 0.5;
            var c = new[]
            {
                new Point3D(-h,-h,-h), new Point3D(h,-h,-h), new Point3D(h,h,-h), new Point3D(-h,h,-h),
                new Point3D(-h,-h, h), new Point3D(h,-h, h), new Point3D(h,h, h), new Point3D(-h,h, h)
            };
            foreach (var p in c) m.Positions.Add(p);
            int[] idx =
            {
                0,1,2, 0,2,3,
                4,6,5, 4,7,6,
                0,4,5, 0,5,1,
                3,2,6, 3,6,7,
                0,3,7, 0,7,4,
                1,5,6, 1,6,2
            };
            foreach (var i in idx) m.TriangleIndices.Add(i);
            m.Freeze();
            return m;
        }

        /// <summary>单个轴的分类信息（用于参数化生成机台几何）。</summary>
        private sealed class AxisInfo
        {
            public string Name = "";
            public char Role;       // 旋转轴角色 A/B/C
            public char Scene;      // 直线轴场景角色 X/D/U
            public bool IsRotary;
            public double Min, Max;
        }
    }
}
