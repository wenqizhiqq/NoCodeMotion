// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// 运行轨迹 3D 仿真控件（参数化机台）。
// 机台几何根据 ProjectStore.Data.Axes + 流程内容（相机/气缸等设备使用）自动生成，
// 三轴联动由 AxisRuntimeState（流程/单步运行时实时写入的轴位置）驱动；
// 流程「相机」步骤真实取帧后，抓拍图显示在卡片内的 CaptureImage 预览。
// 支持鼠标拖拽旋转（自然跟踪球手感）、滚轮缩放；机台各部件使用程序化真实材质
// （拉丝金属 / 烤漆 / 阳极氧化 / 深色塑料 / 镜头玻璃 / 床面网格等），基元网格带 UV 以正确贴图。
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Cad;

namespace NoCodeMotion.Views
{
    /// <summary>运行轨迹 3D 仿真控件（参数化机台，由轴配置 + 流程内容自动生成，真实材质）。</summary>
    public partial class Sim3DView : UserControl
    {
        // 部件材质种类（对应程序化真实材质）
        private enum MatKind
        {
            Bed, Frame, Carriage, Spindle, ToolTip,
            RotaryBase, Rotary, CameraBody, CameraLens, Workpiece,
            CylBody, CylRod,
            Point, Traj, Head
        }

        // ===== 场景参数 =====
        private const double NormHalf = 40;   // 点位归一化半幅
        private const double WorkX = 50;      // 机台 X 工作空间半幅（场景 X 轴）
        private const double WorkZ = 35;      // 机台 depth 工作空间半幅（场景 Z 轴）
        private const double WorkY = 25;      // 机台 vertical 工作空间半幅（场景 Y 轴）
        private const double BaseY = -10;     // 床面以上基准高度

        // 数据叠加层颜色（轨迹/点位/当前位置保持纯色以便辨识）
        private static readonly Color C_Traj = Color.FromRgb(0x3B, 0x82, 0xF6);
        private static readonly Color C_Point = Color.FromRgb(0x60, 0xA5, 0xFA);
        private static readonly Color C_Head = Color.FromRgb(0xEF, 0x44, 0x44);
        private static readonly Color C_LabelFill = Color.FromArgb(235, 255, 255, 255);
        private static readonly Color C_LabelBorder = Color.FromRgb(0x47, 0x55, 0x69);
        private static readonly Color C_LabelText = Color.FromRgb(0x1E, 0x29, 0x3B);

        // ===== 依赖属性 =====
        /// <summary>智能跟随开关：哪个轴在动相机就平滑对准该轴（工具条 CheckBox / 外部可设）。</summary>
        public static readonly DependencyProperty FollowEnabledProperty =
            DependencyProperty.Register(nameof(FollowEnabled), typeof(bool), typeof(Sim3DView),
                new PropertyMetadata(true, (o, e) => ((Sim3DView)o)._followEnabled = (bool)e.NewValue));
        public bool FollowEnabled
        {
            get => (bool)GetValue(FollowEnabledProperty);
            set => SetValue(FollowEnabledProperty, value);
        }

        /// <summary>异常焦点轴：运行异常时由 OperatorViewModel 解析轴名后设置，相机锁定并飞到该轴红高亮；设为空即恢复跟随。</summary>
        public static readonly DependencyProperty FocusAxisNameProperty =
            DependencyProperty.Register(nameof(FocusAxisName), typeof(string), typeof(Sim3DView),
                new PropertyMetadata(null, (o, e) =>
                {
                    var v = (Sim3DView)o;
                    var n = e.NewValue as string;
                    if (string.IsNullOrEmpty(n)) v.ClearFocus();
                    else v.FocusAxis(n, true);
                }));
        public string? FocusAxisName
        {
            get => (string?)GetValue(FocusAxisNameProperty);
            set => SetValue(FocusAxisNameProperty, value);
        }

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

        // ===== 相机轨道参数（自然跟踪球：拖拽方向与场景旋转一致） =====
        private double _theta = 0.7;
        private double _phi = 0.5;
        private double _radius = 240;
        private bool _dragging;
        private Point _last;

        // ===== 场景变换（点位归一化） =====
        private double _scale = 1;
        private Point3D _center = new(0, 0, 0);

        // ===== 共享几何（冻结，带 UV 以贴图） =====
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

        // 轴分类结果 + 流程设备使用
        private readonly List<AxisInfo> _linearInfos = new();
        private readonly List<AxisInfo> _rotaryInfos = new();
        private AxisInfo? _axialX, _axialYdepth, _axialZup;
        private bool _usesCamera, _usesCylinder;

        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(33) };

        // ===== 智能跟随 / 运动高亮 / 平滑插值 =====
        private Point3D _orbitCenter = new(0, 6, 0);        // 相机轨道中心（Look 目标），自动跟随时平滑移动
        private Point3D _orbitCenterTarget = new(0, 6, 0);
        private bool _followEnabled = true;                 // 智能跟随开关（手动拖拽时临时关闭）
        private DispatcherTimer? _followResumeTimer;
        private readonly Dictionary<string, double> _prevAxis = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, double> _axisSpeed = new(StringComparer.OrdinalIgnoreCase);
        private double _dispX, _dispY, _dispZ;              // 直线轴平滑显示位移（避免跳变更顺滑）
        private double _dispRot;                            // 首个旋转轴平滑角度
        private readonly Dictionary<char, ModelVisual3D> _highlightHosts = new();  // 轴角色 -> 发光外壳宿主
        private readonly Dictionary<char, GeometryModel3D> _highlightModels = new();
        private readonly SolidColorBrush _hlBrushActive = new(Color.FromRgb(0xFB, 0x92, 0x3C)); // 运动中：橙
        private readonly SolidColorBrush _hlBrushError = new(Color.FromRgb(0xEF, 0x44, 0x44));  // 异常：红
        private string? _focusAxisName;                     // 异常锁定焦点轴名（优先于自动跟随）
        private bool _focusIsError;
        private double _pulse;                              // 高亮脉冲相位

        // ===== STP / STEP CAD 模型（由“打开STP”按钮导入，独立于参数化机台） =====
        private bool _cadMode;                              // 是否处于 CAD 显示模式（隐藏参数化机台）
        private ModelVisual3D? _stpModel;                  // 缓存的 CAD 可视节点
        private Model3DGroup? _stpContent;                  // 冻结后的 CAD 模型（跨线程安全）
        private Point3D _stpCenter;                        // CAD 包围盒中心（WPF 空间，已 Z-up→Y-up）
        private double _stpRadius;                         // CAD 包围球半径

        // ===== DWG / DXF 二维布局（由"导入DWG/DXF"按钮导入，独立于参数化机台与 STP） =====
        private ModelVisual3D? _dwgContent;                // DWG 可视节点（含线段网格 + 文字标签）
        private Point3D _dwgCenter;                        // 取景中心
        private double _dwgRadius;                         // 取景半径

        public Sim3DView()
        {
            InitializeComponent();
            UpdateCamera();

            Vp.MouseDown += (s, e) =>
            {
                Vp.CaptureMouse();
                _dragging = true;
                _last = e.GetPosition(Vp);
                // 用户手动接管视角：暂停智能跟随，松开 2.5s 后自动恢复（异常锁定时不恢复）
                _followEnabled = false;
                _followResumeTimer?.Stop();
            };
            Vp.MouseMove += (s, e) =>
            {
                if (!_dragging) return;
                var p = e.GetPosition(Vp);
                double dx = p.X - _last.X;
                double dy = p.Y - _last.Y;
                _last = p;
                // 自然跟踪球：拖动方向与场景旋转方向一致（抓取跟随手感）
                _theta += dx * 0.01;
                _phi += dy * 0.01;
                _phi = Math.Max(-1.45, Math.Min(1.45, _phi));
                UpdateCamera();
            };
            Vp.MouseUp += (s, e) =>
            {
                _dragging = false;
                Vp.ReleaseMouseCapture();
                // 2.5s 后恢复智能跟随（除非正处于异常锁定视角）
                if (_focusAxisName == null)
                {
                    _followResumeTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2500) };
                    _followResumeTimer.Tick += (s2, e2) => { _followResumeTimer!.Stop(); if (_focusAxisName == null) _followEnabled = true; };
                    _followResumeTimer.Stop();
                    _followResumeTimer.Start();
                }
            };
            Vp.MouseWheel += (s, e) =>
            {
                // 滚轮上滚(Delta>0)放大(相机靠近, radius 减小)；下滚缩小——与常见查看器一致
                _radius *= (1 - e.Delta * 0.0008);
                _radius = Math.Max(100, Math.Min(800, _radius));
                UpdateCamera();
            };

            _timer.Tick += (s, e) => UpdatePoseFromRuntime();

            if (ProjectStore.Data?.Axes is INotifyCollectionChanged ncA)
                ncA.CollectionChanged += (s, e) => Dispatcher.Invoke(BuildScene);
            if (ProjectStore.Data?.Cameras is INotifyCollectionChanged ncC)
                ncC.CollectionChanged += (s, e) => Dispatcher.Invoke(BuildScene);
            if (ProjectStore.Data?.Cylinders is INotifyCollectionChanged ncY)
                ncY.CollectionChanged += (s, e) => Dispatcher.Invoke(BuildScene);
            if (ProjectStore.Data?.Flows is INotifyCollectionChanged ncF)
                ncF.CollectionChanged += (s, e) => Dispatcher.Invoke(BuildScene);

            BuildScene();
            _timer.Start();

            // 自动载入内置示例机器人 STP（位于输出目录 Models\CAD\ 下），让操作员仿真页默认展示真实 3D 模型；
            // 用 Loaded 触发一次即可，避免在构造时控件尚未就绪。用户也可用「打开STP」加载其它模型。
            Loaded += (s, e) => TryAutoLoadDefaultCad();
        }

        private bool _autoCadTried;
        private void TryAutoLoadDefaultCad()
        {
            if (_autoCadTried) return;
            _autoCadTried = true;
            var def = System.IO.Path.Combine(AppContext.BaseDirectory, "Models", "CAD", "IR-R10-140S-INT-3D-3D.stp");
            if (System.IO.File.Exists(def))
                LoadStepFile(def);
        }

        // ===================== 场景构建 =====================
        private void BuildScene()
        {
            Root.Children.Clear();
            _pointModels.Clear();
            _headModel = null;
            _machineGroup = null;
            _stpModel = null;
            _dwgContent = null;
            _linearGroups.Clear();
            _rotaryTops.Clear();
            _highlightHosts.Clear();
            _highlightModels.Clear();
            _workpieceParent = null;
            _deepestLinear = null;
            _axialX = _axialYdepth = _axialZup = null;
            _linearInfos.Clear();
            _rotaryInfos.Clear();
            _usesCamera = _usesCylinder = false;
            _scale = 1;
            _center = new Point3D(0, 0, 0);
            _prevAxis.Clear();
            _axisSpeed.Clear();
            _dispX = _dispY = _dispZ = 0;
            _dispRot = 0;

            var raw = Points;

            if (_cadMode && (_dwgContent != null || _stpContent != null))
            {
                // CAD/DWG 显示模式：隐藏参数化机台、轨迹与点位，只显示导入模型
                EmptyHint.Visibility = Visibility.Collapsed;
                _machineGroup = null;
                if (_dwgContent != null)
                    Root.Children.Add(_dwgContent);
                else if (_stpContent != null)
                    Root.Children.Add(new ModelVisual3D { Content = _stpContent });
            }
            else
            {
                EmptyHint.Visibility = (raw == null || raw.Count == 0) ? Visibility.Visible : Visibility.Collapsed;

                // 参数化机台：始终根据轴配置 + 流程内容生成（即使暂无点位也展示机台）
                BuildMachine();

                // 轨迹 + 点位（来自点位表 / 流程点位步骤）—— raw 可能为 null，必须空保护
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

                    var plane = new MeshGeometry3D();
                    double groundY = -52, span = 110;
                    plane.Positions.Add(new Point3D(-span, groundY, -span));
                    plane.Positions.Add(new Point3D(span, groundY, -span));
                    plane.Positions.Add(new Point3D(span, groundY, span));
                    plane.Positions.Add(new Point3D(-span, groundY, span));
                    plane.TriangleIndices.Add(0); plane.TriangleIndices.Add(1); plane.TriangleIndices.Add(2);
                    plane.TriangleIndices.Add(0); plane.TriangleIndices.Add(2); plane.TriangleIndices.Add(3);
                    AddModel(Root, plane, 1, 1, 1, new Point3D(0, 0, 0), MatKind.Bed);

                    var pts = new Point3D[raw.Count];
                    for (int i = 0; i < raw.Count; i++) pts[i] = ToMachine(ToSceneSafe(raw[i]));
                    for (int i = 0; i < pts.Length - 1; i++)
                        AddSegment(pts[i], pts[i + 1], 1.2, MatKind.Traj);
                    for (int i = 0; i < pts.Length; i++)
                        _pointModels.Add(AddModel(Root, _sphere, 3.2, 3.2, 3.2, pts[i], MatKind.Point));
                }

                if (HeadVisible)
                    _headModel = AddModel(Root, _sphere, 4.6, 4.6, 4.6, new Point3D(0, 0, 0), MatKind.Head);
            }

            UpdatePoseFromRuntime();
            UpdateCurrent();
        }

        // 机台：轴（龙门/导轨/转台）+ 流程内容驱动的外设（相机/气缸）
        private void BuildMachine()
        {
            AnalyzeProject();

            _machineGroup = new ModelVisual3D();
            Root.Children.Add(_machineGroup);

            // 床身 + 4 底脚
            AddModel(_machineGroup, _box, 130, 4, 90, new Point3D(0, -12, 0), MatKind.Bed);
            foreach (var cx in new[] { -55.0, 55.0 })
                foreach (var cz in new[] { -35.0, 35.0 })
                    AddModel(_machineGroup, _cyl, 5, 10, 5, new Point3D(cx, -18, cz), MatKind.RotaryBase);

            // 直线轴：嵌套组 X ⊃ D(depth) ⊃ U(up)
            ModelVisual3D parent = _machineGroup;
            if (_axialX != null) { var g = new ModelVisual3D(); parent.Children.Add(g); _linearGroups['X'] = g; AddGantryStructure(g); AddHighlight(g, 'X', 11, 58, 76, new Point3D(0, 16, 0)); parent = g; }
            if (_axialYdepth != null) { var g = new ModelVisual3D(); parent.Children.Add(g); _linearGroups['D'] = g; AddBridgeStructure(g); AddHighlight(g, 'D', 116, 16, 18, new Point3D(0, 38, 0)); parent = g; }
            if (_axialZup != null) { var g = new ModelVisual3D(); parent.Children.Add(g); _linearGroups['U'] = g; AddSpindleStructure(g); AddHighlight(g, 'U', 18, 32, 18, new Point3D(0, 8, 0)); parent = g; _deepestLinear = parent; }

            // 旋转轴：床面转台
            foreach (var r in _rotaryInfos)
            {
                var baseC = new ModelVisual3D();
                _machineGroup.Children.Add(baseC);
                AddModel(baseC, _cyl, 18, 3, 18, new Point3D(0, -9, 0), MatKind.RotaryBase);
                var top = new ModelVisual3D();
                baseC.Children.Add(top);
                AddModel(top, _cyl, 16, 4, 16, new Point3D(0, -6, 0), MatKind.Rotary);
                _rotaryTops[r.Role] = top;
                if (_workpieceParent == null) _workpieceParent = top; // 工件随第一个旋转轴转
                AddHighlight(baseC, r.Role, 22, 8, 22, new Point3D(0, -6, 0));
            }

            // 工件（蓝金属块）：放在旋转台上（若有），否则床面
            if (_workpieceParent == null) _workpieceParent = _machineGroup;
            AddModel(_workpieceParent, _box, 22, 6, 18, new Point3D(0, -2, 0), MatKind.Workpiece);

            // 相机：由 ProjectStore.Data.Cameras 数量驱动（流程用到相机则至少 1 个）
            int camN = ProjectStore.Data?.Cameras?.Count ?? 0;
            if (camN == 0 && _usesCamera) camN = 1;
            for (int i = 0; i < camN; i++)
            {
                double t = camN == 1 ? 0.5 : (double)i / (camN - 1);
                double ang = (0.25 + 0.5 * t) * Math.PI;     // 45°..135°，前上方俯视床面
                double cx = Math.Cos(ang) * 62;
                double cz = -Math.Sin(ang) * 62;
                double cy = 56 - i * 3;
                AddModel(_machineGroup, _box, 14, 10, 18, new Point3D(cx, cy, cz), MatKind.CameraBody);
                AddModel(_machineGroup, _cyl, 8, 8, 8, new Point3D(cx, cy - 9, cz), MatKind.CameraLens);
                AddModel(_machineGroup, _cyl, 3, 40, 3, new Point3D(cx, cy - 21, cz), MatKind.CameraBody);
                _machineGroup.Children.Add(MakeLabel("相机" + (camN > 1 ? (i + 1).ToString() : ""), new Point3D(cx, cy + 13, cz)));
            }

            // 气缸：由 ProjectStore.Data.Cylinders 数量驱动（流程用到气缸则至少 1 个）
            int cylN = ProjectStore.Data?.Cylinders?.Count ?? 0;
            if (cylN == 0 && _usesCylinder) cylN = 1;
            for (int i = 0; i < cylN; i++)
            {
                double cx = cylN == 1 ? 0 : -40 + 80 * (double)i / (cylN - 1);
                double cz = 32;
                AddModel(_machineGroup, _cyl, 9, 16, 9, new Point3D(cx, -4, cz), MatKind.CylBody);   // 缸体
                AddModel(_machineGroup, _cyl, 3.4, 16, 3.4, new Point3D(cx, 12, cz), MatKind.CylRod); // 活塞杆（伸出示意）
                _machineGroup.Children.Add(MakeLabel("气缸" + (cylN > 1 ? (i + 1).ToString() : ""), new Point3D(cx, 26, cz)));
            }

            BuildLabels();
        }

        // 龙门结构（随 X 平移）
        private void AddGantryStructure(ModelVisual3D g)
        {
            AddModel(g, _box, 8, 55, 8, new Point3D(0, 15, -32), MatKind.Frame);
            AddModel(g, _box, 8, 55, 8, new Point3D(0, 15, 32), MatKind.Frame);
            AddModel(g, _box, 8, 8, 72, new Point3D(0, 42, 0), MatKind.Frame); // 顶部横梁（沿 depth）
        }
        // 桥式滑座（随 depth 平移）
        private void AddBridgeStructure(ModelVisual3D g)
        {
            AddModel(g, _box, 110, 10, 12, new Point3D(0, 38, 0), MatKind.Carriage);
        }
        // 主轴/刀具（随 up 平移）
        private void AddSpindleStructure(ModelVisual3D g)
        {
            AddModel(g, _cyl, 10, 20, 10, new Point3D(0, 12, 0), MatKind.Spindle);
            AddModel(g, _box, 4, 8, 4, new Point3D(0, -2, 0), MatKind.ToolTip);
        }

        private void BuildLabels()
        {
            _machineGroup!.Children.Add(MakeLabel("工件", new Point3D(0, 4, 24)));
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

        // 分析项目：轴分类 + 流程步骤设备使用（相机/气缸）
        private void AnalyzeProject()
        {
            _axialX = _axialYdepth = _axialZup = null;
            _linearInfos.Clear();
            _rotaryInfos.Clear();
            _usesCamera = _usesCylinder = false;

            var axes = ProjectStore.Data?.Axes;
            if (axes != null)
            {
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
                for (int i = 0; i < linear.Count; i++)
                {
                    var li = linear[i];
                    if (i == 0) { li.Scene = 'X'; _axialX = li; }
                    else if (i == 1) { li.Scene = 'D'; _axialYdepth = li; }
                    else if (i == 2) { li.Scene = 'U'; _axialZup = li; }
                    _linearInfos.Add(li);
                }
            }

            var flows = ProjectStore.Data?.Flows;
            if (flows != null)
                foreach (var f in flows)
                    if (f.Steps != null)
                        foreach (var s in f.Steps)
                        {
                            if (s.Function == "相机") _usesCamera = true;
                            else if (s.Function == "气缸") _usesCylinder = true;
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

        // 每帧：用 AxisRuntimeState 驱动机台各轴组 + 当前位置头（含平滑、运动检测、智能跟随、高亮）
        private void UpdatePoseFromRuntime()
        {
            if (_machineGroup == null) return;

            // 目标轴位置（归一化 → 工作空间）
            double tx = _axialX != null ? NormPos(_axialX) * WorkX : 0;
            double tz = _axialYdepth != null ? NormPos(_axialYdepth) * WorkZ : 0;
            double tyUp = (_axialZup != null ? NormPos(_axialZup) * WorkY : 0) + BaseY;
            double trot = _rotaryInfos.Count > 0 ? AxisRuntimeState.Get(_rotaryInfos[0].Name) : 0;

            // 平滑插值（低通），动画更顺滑、避免跳变
            const double k = 0.2;
            _dispX += (tx - _dispX) * k;
            _dispZ += (tz - _dispZ) * k;
            _dispY += (tyUp - _dispY) * k;
            _dispRot += (trot - _dispRot) * k;

            if (_linearGroups.TryGetValue('X', out var gx)) gx.Transform = new TranslateTransform3D(_dispX, 0, 0);
            if (_linearGroups.TryGetValue('D', out var gd)) gd.Transform = new TranslateTransform3D(0, 0, _dispZ);
            if (_linearGroups.TryGetValue('U', out var gu)) gu.Transform = new TranslateTransform3D(0, _dispY, 0);

            foreach (var r in _rotaryInfos)
            {
                if (_rotaryTops.TryGetValue(r.Role, out var top))
                {
                    var axisVec = r.Role == 'A' ? new Vector3D(0, 1, 0)
                                : r.Role == 'B' ? new Vector3D(1, 0, 0)
                                : new Vector3D(0, 0, 1);
                    top.Transform = new RotateTransform3D(new AxisAngleRotation3D(axisVec, _dispRot));
                }
            }

            PlaceHead(_dispX, _dispY, _dispZ);

            UpdateSmartFocus();
        }

        // 智能跟随：检测运动最快的轴 → 相机平滑对准它；异常锁定轴优先并红高亮
        private void UpdateSmartFocus()
        {
            _pulse += 0.18;
            double s = 0.5 + 0.5 * Math.Sin(_pulse);

            // 各轴归一化值 + 每帧速度
            var axes = new List<(char role, double norm)>();
            if (_axialX != null) axes.Add(('X', NormPos(_axialX)));
            if (_axialYdepth != null) axes.Add(('D', NormPos(_axialYdepth)));
            if (_axialZup != null) axes.Add(('U', NormPos(_axialZup)));
            foreach (var r in _rotaryInfos) axes.Add((r.Role, AxisRuntimeState.Get(r.Name) / 180.0));

            char activeRole = '\0';
            double maxSpeed = 0;
            foreach (var (role, norm) in axes)
            {
                string key = role.ToString();
                double prev = _prevAxis.TryGetValue(key, out var p) ? p : norm;
                double speed = Math.Abs(norm - prev);
                _prevAxis[key] = norm;
                _axisSpeed[key] = speed;
                if (speed > maxSpeed) { maxSpeed = speed; activeRole = role; }
            }

            if (_focusAxisName != null)
            {
                char fr = RoleOfAxis(_focusAxisName);
                _orbitCenterTarget = FocusPointFor(fr);
                SetHighlight(fr, true, s);
                HideOtherHighlights(fr);
                SmoothOrbit();
                return;
            }

            if (_followEnabled && !_dragging && maxSpeed > 0.0009 && activeRole != '\0')
            {
                _orbitCenterTarget = FocusPointFor(activeRole);
                SetHighlight(activeRole, false, s);
                HideOtherHighlights(activeRole);
            }
            else
            {
                FadeAllHighlights();
            }
            SmoothOrbit();
        }

        private Point3D FocusPointFor(char role)
        {
            return role switch
            {
                'X' => new Point3D(_dispX, 30, 0),
                'D' => new Point3D(_dispX, 30, _dispZ),
                'U' => new Point3D(_dispX, 30 + _dispY, _dispZ),
                _ => new Point3D(0, 2, 0) // 旋转轴转台中心
            };
        }

        private char RoleOfAxis(string name)
        {
            foreach (var a in _linearInfos)
                if (string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)) return a.Scene;
            foreach (var r in _rotaryInfos)
                if (string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)) return r.Role;
            char c = PickLinearRole(name);
            return c != 'L' ? c : PickRotaryRole(name);
        }

        private void SetHighlight(char role, bool isError, double s)
        {
            if (!_highlightModels.TryGetValue(role, out var gm)) return;
            if (gm.Material is not DiffuseMaterial mat) return;
            mat.Brush = isError ? _hlBrushError : _hlBrushActive;
            // DiffuseMaterial 无 Opacity，靠 SolidColorBrush.Opacity 做呼吸脉冲
            (isError ? _hlBrushError : _hlBrushActive).Opacity = 0.25 + 0.55 * s;
            if (_highlightHosts.TryGetValue(role, out var host)) host.Content = gm;
        }

        private void HideOtherHighlights(char role)
        {
            foreach (var kv in _highlightHosts)
                if (kv.Key != role) kv.Value.Content = null;
        }

        private void FadeAllHighlights()
        {
            foreach (var kv in _highlightHosts) kv.Value.Content = null;
        }

        private void SmoothOrbit()
        {
            _orbitCenter.X += (_orbitCenterTarget.X - _orbitCenter.X) * 0.08;
            _orbitCenter.Y += (_orbitCenterTarget.Y - _orbitCenter.Y) * 0.08;
            _orbitCenter.Z += (_orbitCenterTarget.Z - _orbitCenter.Z) * 0.08;
        }

        /// <summary>外部（运行异常）调用：把相机锁定并飞到指定轴，红高亮脉冲。</summary>
        public void FocusAxis(string name, bool isError = true)
        {
            _focusAxisName = name;
            _focusIsError = isError;
            _followEnabled = false;
            _followResumeTimer?.Stop();
        }

        /// <summary>清除异常视角锁定，恢复正常智能跟随。</summary>
        public void ClearFocus()
        {
            _focusAxisName = null;
            _focusIsError = false;
            if (!_dragging) _followEnabled = true;
        }

        // 工具条：智能跟随开关
        private void ChkFollow_Changed(object sender, RoutedEventArgs e)
        {
            if (ChkFollow == null) return;
            _followEnabled = ChkFollow.IsChecked == true;
            if (_followEnabled && _focusAxisName != null) ClearFocus();
            _followResumeTimer?.Stop();
        }

        // 工具条：复位视角到默认轨道
        private void BtnResetView_Click(object sender, RoutedEventArgs e)
        {
            if (_cadMode && (_dwgContent != null || _stpContent != null))
            {
                // CAD/DWG 模式：复位到模型最佳取景
                if (_dwgContent != null)
                {
                    _theta = 0.7; _phi = 0.5; _radius = Math.Max(_dwgRadius * 2.6, 140);
                    _orbitCenter = _orbitCenterTarget = _dwgCenter;
                }
                else
                {
                    _theta = 0.7; _phi = 0.5; _radius = Math.Max(_stpRadius * 2.6, 120);
                    _orbitCenter = _orbitCenterTarget = _stpCenter;
                }
            }
            else
            {
                _theta = 0.7; _phi = 0.5; _radius = 240;
                _orbitCenter = _orbitCenterTarget = new Point3D(0, 6, 0);
            }
            UpdateCamera();
        }

        // ===================== STP / STEP 导入 =====================
        // 通过 OcctNet.Wrapper（封装 OpenCASCADE 7.9 原生库）把 STEP/IGES BREP 三角化为 WPF 网格。
        // 这是能正确处理 Creo/UG/SolidWorks 等真实 BREP 的可靠路径（Assimp 的 BREP 三角化器对该类文件产出 0 面）。

        /// <summary>Z-up（多数 CAD）→ Y-up（WPF 视口）的旋转，仅旋转不平移。</summary>
        private static readonly Matrix3D _zUpToYUp =
            new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), -90)).Value;

        /// <summary>打开 STP/STEP 文件对话框并异步加载模型。</summary>
        private void BtnOpenStp_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "STEP 模型 (*.stp;*.step)|*.stp;*.step|所有文件 (*.*)|*.*",
                Title = "打开 STP / STEP 模型"
            };
            bool? ok = null;
            try { ok = dlg.ShowDialog(); }
            catch (Exception ex)
            {
                SetStpStatus("无法打开文件对话框（可能被安全软件拦截）：" + ex.Message);
                return;
            }
            if (ok == true) LoadStepFile(dlg.FileName);
        }

        /// <summary>打开 DWG/DXF 文件对话框并异步加载为 2D 布局（Aspose.CAD 读取，矢量迭代无评估水印）。</summary>
        private void BtnImportDwg_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CAD 图纸 (*.dwg;*.dxf)|*.dwg;*.dxf|所有文件 (*.*)|*.*",
                Title = "导入 DWG / DXF 图纸"
            };
            bool? ok = null;
            try { ok = dlg.ShowDialog(); }
            catch (Exception ex)
            {
                SetStpStatus("无法打开文件对话框（可能被安全软件拦截）：" + ex.Message);
                return;
            }
            if (ok == true) LoadDwgFile(dlg.FileName);
        }

        /// <summary>异步解析 DWG/DXF 并切换到 2D 布局显示模式（后台线程做几何提取，UI 线程只负责装配）。</summary>
        public void LoadDwgFile(string path)
        {
            SetStpStatus("正在解析图纸…");
            Task.Run(() =>
            {
                try
                {
                    var d = DwgReader.Read(path);   // 同步读取 + 几何提取（Aspose.CAD，矢量迭代无评估水印）
                    if (!d.HasData || !d.HasFit)
                    {
                        Dispatcher.Invoke(() => SetStpStatus("图纸无可渲染几何（可能是纯 3D 实体/栅格）：" + System.IO.Path.GetFileName(path)));
                        return;
                    }
                    BuildDwgModel(d, out var root, out var center, out var radius);

                    Dispatcher.Invoke(() =>
                    {
                        _stpContent = null; _stpModel = null;   // 导入 DWG 时清空可能已显示的 STP
                        _dwgContent = root;
                        _dwgCenter = center;
                        _dwgRadius = radius;
                        _cadMode = true;
                        _orbitCenter = _orbitCenterTarget = center;
                        _radius = Math.Max(radius * 2.6, 140);
                        _followEnabled = false;
                        if (ChkFollow != null) ChkFollow.IsChecked = false;
                        BuildScene();   // 进入 DWG 模式：隐藏参数化机台，显示导入图纸
                        UpdateCamera();
                        SetStpStatus($"已加载 {System.IO.Path.GetFileName(path)} · 线段 {d.Segments.Count:N0} · 标注 {d.Labels.Count}");
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => SetStpStatus("图纸解析失败：" + ex.Message));
                }
            });
        }

        /// <summary>清除已加载的 CAD 模型，恢复参数化机台。</summary>
        private void BtnClearStp_Click(object sender, RoutedEventArgs e)
        {
            _cadMode = false;
            _stpContent = null;
            _stpModel = null;
            _dwgContent = null;
            _followEnabled = true;
            if (ChkFollow != null) ChkFollow.IsChecked = true;
            BuildScene();
            UpdateCamera();
            SetStpStatus("已清除模型，恢复参数化机台");
        }

        /// <summary>异步解析 STP 文件并切换到 CAD 显示模式（后台线程做重三角化，UI 线程只负责装配冻结模型）。</summary>
        public void LoadStepFile(string path)
        {
            SetStpStatus("正在解析 STP…");
            Task.Run(() =>
            {
                try
                {
                    BuildStepModel(path, out var group, out var center, out var radius, out long tris);
                    group.Freeze(); // 跨线程安全，便于在 UI 线程使用

                    Dispatcher.Invoke(() =>
                    {
                        _dwgContent = null;   // 载入 STP 时清空可能已显示的 DWG
                        _stpContent = group;
                        _stpCenter = center;
                        _stpRadius = radius;
                        _cadMode = true;
                        _orbitCenter = _orbitCenterTarget = center;
                        _radius = Math.Max(radius * 2.6, 120);
                        _followEnabled = false;
                        if (ChkFollow != null) ChkFollow.IsChecked = false;
                        BuildScene();   // 进入 CAD 模式：隐藏参数化机台，显示导入模型
                        UpdateCamera();
                        SetStpStatus($"已加载 {System.IO.Path.GetFileName(path)} · {tris:N0} 三角面");
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => SetStpStatus("STP 解析失败：" + ex.Message));
                }
            });
        }

        // 用 OpenCASCADE 把 STEP/IGES 读入并三角化，烘培为世界坐标（含 Z-up→Y-up）后转为单一 WPF 网格，
        // 同时计算平滑顶点法线与包围盒。单次调用在后台线程完成，返回可冻结的 Model3DGroup。
        private static void BuildStepModel(string path, out Model3DGroup group, out Point3D center, out double radius, out long totalTris)
        {
            group = new Model3DGroup();
            using var shape = OcctNet.Wrapper.OcctShape.ImportStep(path);
            var mesh = shape.Triangulate(linearDeflection: 1.0);
                int vc = mesh.Vertices.Count;
                int ic = mesh.TriangleIndices.Count;
                totalTris = ic / 3;

                var min = new Point3D(double.MaxValue, double.MaxValue, double.MaxValue);
                var max = new Point3D(double.MinValue, double.MinValue, double.MinValue);
                var positions = new Point3D[vc];
                for (int i = 0; i < vc; i++)
                {
                    var v = mesh.Vertices[i];
                    var p = _zUpToYUp.Transform(new Point3D(v.X, v.Y, v.Z)); // 烘培 Z-up→Y-up
                    positions[i] = p;
                    if (p.X < min.X) min.X = p.X; if (p.X > max.X) max.X = p.X;
                    if (p.Y < min.Y) min.Y = p.Y; if (p.Y > max.Y) max.Y = p.Y;
                    if (p.Z < min.Z) min.Z = p.Z; if (p.Z > max.Z) max.Z = p.Z;
                }

                var mg = new MeshGeometry3D();
                for (int i = 0; i < vc; i++) mg.Positions.Add(positions[i]);
                for (int i = 0; i < ic; i++) mg.TriangleIndices.Add(mesh.TriangleIndices[i]);

                // 由三角形累积平滑顶点法线（OCCT 三角化不直接给法线）
                var norms = new Vector3D[vc];
                for (int t = 0; t < ic; t += 3)
                {
                    int a = mesh.TriangleIndices[t], b = mesh.TriangleIndices[t + 1], c = mesh.TriangleIndices[t + 2];
                    var n = Vector3D.CrossProduct(positions[b] - positions[a], positions[c] - positions[a]);
                    if (n.Length > 1e-9)
                    {
                        n.Normalize();
                        norms[a] += n; norms[b] += n; norms[c] += n;
                    }
                }
                for (int i = 0; i < vc; i++)
                {
                    var nv = norms[i];
                    if (nv.Length > 1e-9) nv.Normalize(); else nv = new Vector3D(0, 1, 0);
                    mg.Normals.Add(nv);
                }

                var mat = MakeStepMaterial();
                var gm = new GeometryModel3D(mg, mat) { BackMaterial = mat };
                group.Children.Add(gm);

                var size = new Vector3D(max.X - min.X, max.Y - min.Y, max.Z - min.Z);
                center = new Point3D(min.X + size.X / 2, min.Y + size.Y / 2, min.Z + size.Z / 2);
                radius = 0.5 * Math.Sqrt(size.X * size.X + size.Y * size.Y + size.Z * size.Z);
                if (!double.IsFinite(radius) || radius < 1e-3) radius = 100;
        }

        // CAD 材质：浅钢蓝金属感；双面可见避免黑面（BREP 三角化法线方向偶发不一致）。
        private static Material MakeStepMaterial()
        {
            var brush = new SolidColorBrush(Color.FromRgb(0xB8, 0xC2, 0xCE));
            var grp = new MaterialGroup();
            grp.Children.Add(new DiffuseMaterial(brush));
            grp.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)), 18));
            return grp;
        }

        private void SetStpStatus(string text)
        {
            if (TxtStpStatus != null) TxtStpStatus.Text = text;
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

        // ===================== DWG / DXF 二维布局渲染 =====================
        // 把 DwgReader 提取的线段 + 文字标注，烘焙成躺在地面（XZ 平面）的 3D 布局：
        //  - 每条线段 → 一条细矩形（两个三角形），沿 XZ 平面铺开；
        //  - 文字标注 → Viewport2DVisual3D（复用 IsVisualHostMaterial 技术）；
        //  - 背板 → 浅色半透明卡片，提升可读性。
        // DWG(X,Y) 直接映射到场景 X / Z（Y-up→场景 Z），并按取景包围盒居中、缩放到固定主尺寸。

        /// <summary>把图纸烘焙为可视节点（含线段网格 + 文字标签），并计算取景中心与半径。</summary>
        private static void BuildDwgModel(DwgDrawing d, out ModelVisual3D root, out Point3D center, out double radius)
        {
            const double floorY = 0.2;     // 略高于机台床面（BaseY=-10）
            const double target = 180.0;    // 取景主尺寸（场景单位）：图纸较大一维映射到该长度
            double fx0 = d.FitMinX, fy0 = d.FitMinY, fx1 = d.FitMaxX, fy1 = d.FitMaxY;
            double fcx = (fx0 + fx1) / 2, fcy = (fy0 + fy1) / 2;
            double fw = Math.Max(fx1 - fx0, 1e-3), fh = Math.Max(fy1 - fy0, 1e-3);
            double scale = target / Math.Max(fw, fh);   // DWG 单位 → 场景单位
            double lineW = target / 650.0;              // 线宽（场景单位）
            double hw = lineW / 2;
            double margin = 0.02 * Math.Max(fw, fh);    // 容差：剔除离群图块（如远处的电缆表）

            double MapX(double x) => (x - fcx) * scale;
            double MapZ(double y) => (y - fcy) * scale;

            // ---- 背板 ----
            var back = new MeshGeometry3D();
            double bw = fw * scale * 0.5 + lineW * 3, bh = fh * scale * 0.5 + lineW * 3;
            back.Positions.Add(new Point3D(-bw, floorY - 0.05, -bh));
            back.Positions.Add(new Point3D(bw, floorY - 0.05, -bh));
            back.Positions.Add(new Point3D(bw, floorY - 0.05, bh));
            back.Positions.Add(new Point3D(-bw, floorY - 0.05, bh));
            back.TriangleIndices.Add(0); back.TriangleIndices.Add(1); back.TriangleIndices.Add(2);
            back.TriangleIndices.Add(0); back.TriangleIndices.Add(2); back.TriangleIndices.Add(3);
            var backMat = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0xF8, 0xF9, 0xFB)) { Opacity = 0.30 });
            var backModel = new GeometryModel3D(back, backMat);

            // ---- 线段（细矩形，XZ 平面）----
            var mg = new MeshGeometry3D();
            foreach (var s in d.Segments)
            {
                // 跳过离群图块（与取景主图块不相连，例如被拖到远处 Y≈-2.5M 的电缆表）
                double mx = (s.X1 + s.X2) / 2, my = (s.Y1 + s.Y2) / 2;
                if (mx < fx0 - margin || mx > fx1 + margin || my < fy0 - margin || my > fy1 + margin) continue;

                double ax = MapX(s.X1), az = MapZ(s.Y1);
                double bx = MapX(s.X2), bz = MapZ(s.Y2);
                double dx = bx - ax, dz = bz - az;
                double len = Math.Sqrt(dx * dx + dz * dz);
                if (len < 1e-9) continue;
                double px = -dz / len * hw, pz = dx / len * hw;   // 垂直方向（XZ 平面）
                int b = mg.Positions.Count;
                mg.Positions.Add(new Point3D(ax + px, floorY, az + pz));
                mg.Positions.Add(new Point3D(bx + px, floorY, bz + pz));
                mg.Positions.Add(new Point3D(bx - px, floorY, bz - pz));
                mg.Positions.Add(new Point3D(ax - px, floorY, az - pz));
                mg.TriangleIndices.Add(b); mg.TriangleIndices.Add(b + 1); mg.TriangleIndices.Add(b + 2);
                mg.TriangleIndices.Add(b); mg.TriangleIndices.Add(b + 2); mg.TriangleIndices.Add(b + 3);
            }
            var lineMat = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0x1F, 0x2A, 0x37)));
            var lineModel = new GeometryModel3D(mg, lineMat);

            var linesGroup = new Model3DGroup();
            linesGroup.Children.Add(backModel);
            linesGroup.Children.Add(lineModel);
            linesGroup.Freeze();

            root = new ModelVisual3D();
            root.Children.Add(new ModelVisual3D { Content = linesGroup });

            // ---- 文字标注（仅主图块内）----
            double lh = target / 22.0;   // 标签高度（场景单位）
            foreach (var lb in d.Labels)
            {
                if (lb.X < fx0 - margin || lb.X > fx1 + margin || lb.Y < fy0 - margin || lb.Y > fy1 + margin) continue;
                var t = lb.Text?.Trim();
                if (string.IsNullOrWhiteSpace(t)) continue;
                double lx = MapX(lb.X), lz = MapZ(lb.Y);
                root.Children.Add(MakeDwgLabel(t, new Point3D(lx, floorY + lh * 0.6, lz), lh));
            }

            center = new Point3D(0, 0, 0);   // 已按取景中心居中
            radius = target * 1.55;
        }

        /// <summary>生成一个贴地的文字标签（Viewport2DVisual3D）。</summary>
        private static Viewport2DVisual3D MakeDwgLabel(string text, Point3D pos, double worldH)
        {
            double w = worldH * Math.Max(2.4, text.Length * 0.62);
            double h = worldH;
            var tb = new TextBlock
            {
                Text = text,
                FontSize = worldH * 0.66,
                Foreground = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27)),
                FontWeight = FontWeights.Medium,
                TextWrapping = TextWrapping.NoWrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(228, 0xFF, 0xFF, 0xFF)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
                BorderThickness = new Thickness(worldH * 0.05),
                CornerRadius = new CornerRadius(worldH * 0.14),
                Padding = new Thickness(worldH * 0.12),
                Width = w,
                Height = h,
                Child = tb
            };
            var hostMat = new DiffuseMaterial(new SolidColorBrush(Colors.White));
            Viewport2DVisual3D.SetIsVisualHostMaterial(hostMat, true);
            var mesh = new MeshGeometry3D();
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
                Transform = new TranslateTransform3D(pos.X, pos.Y, pos.Z)
            };
        }

        // ===================== 几何辅助 =====================
        private GeometryModel3D AddModel(ModelVisual3D parent, MeshGeometry3D mesh, double sx, double sy, double sz,
            Point3D pos, MatKind kind, Transform3D? rot = null)
        {
            var mat = GetMaterial(kind);
            var g = new Transform3DGroup();
            if (rot != null) g.Children.Add(rot);
            g.Children.Add(new ScaleTransform3D(sx, sy, sz));
            g.Children.Add(new TranslateTransform3D(pos.X, pos.Y, pos.Z));
            var gm = new GeometryModel3D(mesh, mat) { Transform = g, BackMaterial = mat };
            parent.Children.Add(new ModelVisual3D { Content = gm });
            return gm;
        }

        // 发光高亮外壳：包住对应轴部件，默认隐藏。运动时显橙并脉冲，异常时显红并强脉冲。
        // 返回宿主 ModelVisual3D 以便切换可见性；同时存 GeometryModel3D 以便换材质。
        private ModelVisual3D AddHighlight(ModelVisual3D parent, char role, double sx, double sy, double sz, Point3D pos)
        {
            var mat = new DiffuseMaterial(_hlBrushActive);
            var gm = new GeometryModel3D(_box, mat) { BackMaterial = mat };
            var host = new ModelVisual3D
            {
                Content = gm,
                Transform = new Transform3DGroup
                {
                    Children = { new ScaleTransform3D(sx, sy, sz), new TranslateTransform3D(pos.X, pos.Y, pos.Z) }
                }
            };
            parent.Children.Add(host);
            _highlightHosts[role] = host;
            _highlightModels[role] = gm;
            return host;
        }

        private void AddSegment(Point3D a, Point3D b, double radius, MatKind kind)
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
            AddModel(Root, _cyl, radius, len, radius, mid, kind, rot);
        }

        private void UpdateCamera()
        {
            double r = _radius;
            var pos = new Point3D(
                _orbitCenter.X + r * Math.Cos(_phi) * Math.Cos(_theta),
                _orbitCenter.Y + r * Math.Sin(_phi),
                _orbitCenter.Z + r * Math.Cos(_phi) * Math.Sin(_theta));
            Cam.Position = pos;
            Cam.LookDirection = new Vector3D(_orbitCenter.X - pos.X, _orbitCenter.Y - pos.Y, _orbitCenter.Z - pos.Z);
            Cam.UpDirection = new Vector3D(0, 1, 0);
        }

        // ===================== 程序化真实材质（缓存） =====================
        private static readonly Dictionary<MatKind, Material> _matCache = new();
        private static Material GetMaterial(MatKind kind)
        {
            if (_matCache.TryGetValue(kind, out var m)) return m;
            m = BuildMaterial(kind);
            m.Freeze();
            _matCache[kind] = m;
            return m;
        }

        private static Material BuildMaterial(MatKind kind)
        {
            if (kind == MatKind.Point || kind == MatKind.Traj || kind == MatKind.Head)
                return new DiffuseMaterial(new SolidColorBrush(kind == MatKind.Traj ? C_Traj : kind == MatKind.Head ? C_Head : C_Point));

            var brush = new ImageBrush(Texture(kind))
            {
                TileMode = TileMode.Tile,
                ViewportUnits = BrushMappingMode.RelativeToBoundingBox,
                // 床面网格纹理需要重复平铺才能看出 T 型槽阵列；其余每面一张
                Viewport = kind == MatKind.Bed ? new Rect(0, 0, 0.18, 0.18) : new Rect(0, 0, 1, 1)
            };
            var diff = new DiffuseMaterial(brush);
            if (IsMetal(kind))
            {
                var grp = new MaterialGroup();
                grp.Children.Add(diff);
                grp.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)), 24));
                return grp;
            }
            return diff;
        }

        private static bool IsMetal(MatKind k) =>
            k == MatKind.Bed || k == MatKind.Frame || k == MatKind.Carriage ||
            k == MatKind.RotaryBase || k == MatKind.Rotary || k == MatKind.CylRod;

        private static readonly Dictionary<MatKind, BitmapSource> _texCache = new();
        private static BitmapSource Texture(MatKind kind)
        {
            if (_texCache.TryGetValue(kind, out var t)) return t;
            int s = 128;
            BitmapSource bmp = kind switch
            {
                MatKind.Bed => BedGrid(s),
                MatKind.Frame => Brushed(s, Color.FromRgb(0x6B, 0x74, 0x82), Color.FromRgb(0x4A, 0x53, 0x63), true),
                MatKind.Carriage => Brushed(s, Color.FromRgb(0xB6, 0xBE, 0xCA), Color.FromRgb(0x8F, 0x99, 0xA8), true),
                MatKind.Spindle => Painted(s, Color.FromRgb(0xA7, 0x7D, 0xF6), Color.FromRgb(0x6D, 0x4A, 0xD6)),
                MatKind.ToolTip => Painted(s, Color.FromRgb(0xFB, 0x92, 0x3C), Color.FromRgb(0xEA, 0x58, 0x0C)),
                MatKind.RotaryBase => Brushed(s, Color.FromRgb(0x3A, 0x44, 0x52), Color.FromRgb(0x26, 0x2E, 0x3A), true),
                MatKind.Rotary => Anodized(s, Color.FromRgb(0x22, 0xB8, 0xF3), Color.FromRgb(0x0E, 0xA5, 0xE9)),
                MatKind.CameraBody => DarkPlastic(s),
                MatKind.CameraLens => Lens(s),
                MatKind.Workpiece => Brushed(s, Color.FromRgb(0x6A, 0xA8, 0xFA), Color.FromRgb(0x3B, 0x82, 0xF6), false),
                MatKind.CylBody => Painted(s, Color.FromRgb(0x9C, 0xA3, 0xB0), Color.FromRgb(0x6B, 0x72, 0x80)),
                MatKind.CylRod => Brushed(s, Color.FromRgb(0xDD, 0xE2, 0xE8), Color.FromRgb(0xB4, 0xBB, 0xC5), true),
                _ => Brushed(s, Colors.Gray, Colors.DarkGray, true)
            };
            _texCache[kind] = bmp;
            return bmp;
        }

        private static BitmapSource MakeTex(int s, Action<DrawingContext> draw)
        {
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen()) draw(dc);
            var bmp = new RenderTargetBitmap(s, s, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(dv);
            bmp.Freeze();
            return bmp;
        }

        // 拉丝金属：基色渐变 + 细密条纹
        private static BitmapSource Brushed(int s, Color a, Color b, bool vertical)
        {
            return MakeTex(s, dc =>
            {
                dc.DrawRectangle(new LinearGradientBrush(a, b, vertical ? 90 : 0), null, new Rect(0, 0, s, s));
                var rnd = new Random(0x9E37);
                for (int i = 0; i < s * 4; i++)
                {
                    double p = rnd.NextDouble() * s;
                    byte al = (byte)(10 + rnd.NextDouble() * 22);
                    var pen = new Pen(new SolidColorBrush(Color.FromArgb(al, 255, 255, 255)), 1);
                    if (vertical) dc.DrawLine(pen, new Point(p, 0), new Point(p, s));
                    else dc.DrawLine(pen, new Point(0, p), new Point(s, p));
                }
            });
        }

        // 烤漆：径向渐变 + 细微颗粒
        private static BitmapSource Painted(int s, Color a, Color b)
        {
            return MakeTex(s, dc =>
            {
                dc.DrawRectangle(new RadialGradientBrush(a, b), null, new Rect(0, 0, s, s));
                var rnd = new Random(0x51ED);
                for (int i = 0; i < s * s / 30; i++)
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(14, 0, 0, 0)), null, new Rect(rnd.Next(s), rnd.Next(s), 1, 1));
            });
        }

        // 阳极氧化：径向渐变 + 细密同心圆
        private static BitmapSource Anodized(int s, Color a, Color b)
        {
            return MakeTex(s, dc =>
            {
                dc.DrawRectangle(new RadialGradientBrush(a, b), null, new Rect(0, 0, s, s));
                var rnd = new Random(0x70D);
                for (int i = 0; i < 60; i++)
                {
                    double r = rnd.NextDouble() * s * 0.5;
                    dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(10, 255, 255, 255)), 1), new Point(s / 2.0, s / 2.0), r, r);
                }
            });
        }

        // 床面：深色机加工面 + T 型槽网格
        private static BitmapSource BedGrid(int s)
        {
            return MakeTex(s, dc =>
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x39, 0x41, 0x4F)), null, new Rect(0, 0, s, s));
                var pen = new Pen(new SolidColorBrush(Color.FromRgb(0x22, 0x29, 0x34)), 3);
                int cells = 4;
                for (int i = 0; i <= cells; i++) { double p = (double)i / cells * s; dc.DrawLine(pen, new Point(p, 0), new Point(p, s)); dc.DrawLine(pen, new Point(0, p), new Point(s, p)); }
                dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(0x55, 0x60, 0x70)), 2), new Point(0, s / 2.0), new Point(s, s / 2.0));
            });
        }

        // 深色工程塑料/橡胶
        private static BitmapSource DarkPlastic(int s)
        {
            return MakeTex(s, dc =>
            {
                dc.DrawRectangle(new LinearGradientBrush(Color.FromRgb(0x33, 0x3A, 0x47), Color.FromRgb(0x10, 0x14, 0x1C), 90), null, new Rect(0, 0, s, s));
                var rnd = new Random(0x42);
                for (int i = 0; i < s * s / 40; i++)
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(12, 255, 255, 255)), null, new Rect(rnd.Next(s), rnd.Next(s), 1, 1));
            });
        }

        // 镜头：玻璃质感径向高光
        private static BitmapSource Lens(int s)
        {
            return MakeTex(s, dc =>
            {
                dc.DrawRectangle(new RadialGradientBrush(Color.FromRgb(0x66, 0x77, 0x90), Color.FromRgb(0x0B, 0x10, 0x18)), null, new Rect(0, 0, s, s));
                dc.DrawEllipse(new RadialGradientBrush(Color.FromArgb(180, 255, 255, 255), Color.FromArgb(0, 255, 255, 255)), null, new Point(s * 0.38, s * 0.34), s * 0.16, s * 0.12);
            });
        }

        // ===================== 基础网格生成（带 UV） =====================
        private static MeshGeometry3D BuildSphere(int stacks = 14, int slices = 18)
        {
            var m = new MeshGeometry3D();
            for (int i = 0; i <= stacks; i++)
            {
                double phi = Math.PI * i / stacks;
                for (int j = 0; j <= slices; j++)
                {
                    double theta = 2 * Math.PI * j / slices;
                    m.Positions.Add(new Point3D(Math.Sin(phi) * Math.Cos(theta), Math.Cos(phi), Math.Sin(phi) * Math.Sin(theta)));
                    m.TextureCoordinates.Add(new Point((double)j / slices, (double)i / stacks));
                }
            }
            for (int i = 0; i < stacks; i++)
                for (int j = 0; j < slices; j++)
                {
                    int a = i * (slices + 1) + j;
                    int b = a + slices + 1;
                    m.TriangleIndices.Add(a); m.TriangleIndices.Add(b); m.TriangleIndices.Add(a + 1);
                    m.TriangleIndices.Add(b); m.TriangleIndices.Add(b + 1); m.TriangleIndices.Add(a + 1);
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
                double x = Math.Cos(theta), z = Math.Sin(theta), u = (double)j / slices;
                m.Positions.Add(new Point3D(x, 0.5, z)); m.TextureCoordinates.Add(new Point(u, 1));   // 顶环
                m.Positions.Add(new Point3D(x, -0.5, z)); m.TextureCoordinates.Add(new Point(u, 0));  // 底环
            }
            for (int j = 0; j < slices; j++)
            {
                int a = j * 2, b = a + 1, c = a + 2, d = a + 3;
                m.TriangleIndices.Add(a); m.TriangleIndices.Add(b); m.TriangleIndices.Add(c);
                m.TriangleIndices.Add(b); m.TriangleIndices.Add(d); m.TriangleIndices.Add(c);
            }
            int top = m.Positions.Count; m.Positions.Add(new Point3D(0, 0.5, 0)); m.TextureCoordinates.Add(new Point(0.5, 0.5));
            int bot = m.Positions.Count; m.Positions.Add(new Point3D(0, -0.5, 0)); m.TextureCoordinates.Add(new Point(0.5, 0.5));
            for (int j = 0; j < slices; j++)
            {
                int a = j * 2;
                double x = Math.Cos(2 * Math.PI * j / slices), z = Math.Sin(2 * Math.PI * j / slices);
                m.TriangleIndices.Add(top); m.TriangleIndices.Add(a); m.TriangleIndices.Add(a + 2);
                m.TriangleIndices.Add(bot); m.TriangleIndices.Add(a + 3); m.TriangleIndices.Add(a + 1);
            }
            m.Freeze();
            return m;
        }

        // 规范立方体（6 面，每面 4 顶点 + UV，外法线朝外）
        private static MeshGeometry3D BuildBox()
        {
            var m = new MeshGeometry3D();
            double h = 0.5;
            var faces = new[]
            {
                new[] { new Point3D(-h,-h, h), new Point3D( h,-h, h), new Point3D( h, h, h), new Point3D(-h, h, h) }, // +Z
                new[] { new Point3D( h,-h,-h), new Point3D(-h,-h,-h), new Point3D(-h, h,-h), new Point3D( h, h,-h) }, // -Z
                new[] { new Point3D( h,-h, h), new Point3D( h,-h,-h), new Point3D( h, h,-h), new Point3D( h, h, h) }, // +X
                new[] { new Point3D(-h,-h,-h), new Point3D(-h,-h, h), new Point3D(-h, h, h), new Point3D(-h, h,-h) }, // -X
                new[] { new Point3D(-h, h,-h), new Point3D( h, h,-h), new Point3D( h, h, h), new Point3D(-h, h, h) }, // +Y
                new[] { new Point3D(-h,-h,-h), new Point3D( h,-h,-h), new Point3D( h,-h, h), new Point3D(-h,-h, h) }, // -Y
            };
            foreach (var f in faces)
            {
                int baseIdx = m.Positions.Count;
                for (int i = 0; i < 4; i++)
                {
                    m.Positions.Add(f[i]);
                    m.TextureCoordinates.Add(new Point(i == 0 || i == 3 ? 0 : 1, i >= 2 ? 1 : 0));
                }
                m.TriangleIndices.Add(baseIdx); m.TriangleIndices.Add(baseIdx + 1); m.TriangleIndices.Add(baseIdx + 2);
                m.TriangleIndices.Add(baseIdx); m.TriangleIndices.Add(baseIdx + 2); m.TriangleIndices.Add(baseIdx + 3);
            }
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
