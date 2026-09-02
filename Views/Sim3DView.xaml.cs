// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇⁣
// 真实机台 3D 装配说明：
// 各 .obj 来自同一 CNC 设计但导出时坐标原点不一致（每个零件单独导出，非共享装配），
// 故不能简单合并包围盒后做整体归一化。这里改为"逐零件归一化到单位立方体并修正 Z-up→Y-up，
// 再由代码手动指定每个零件在场景中的位置与尺寸"。
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 运行轨迹 3D 仿真控件。
    /// 真实机台零件（下载自 GitHub DIY-CNC-machine，14 个 .obj）逐零件归一化后由代码手动装配为龙门 CNC。
    /// 运行时：滑座（X/Z）跟随轨迹头实时运动，刀具（Y）跟随轨迹头实时下刀，红色头标出当前位置。
    /// 支持鼠标拖拽旋转、滚轮缩放。
    /// </summary>
    public partial class Sim3DView : UserControl
    {
        // ===== 场景参数 =====
        private const double NormHalf = 40;   // 点位归一化半幅
        private const double WorkX = 50;      // 机台 X 工作空间半幅（±50 映射到 ±1）
        private const double WorkZ = 35;      // 机台 Z 工作空间半幅
        private const double WorkY = 25;      // 刀具 Y 向运动半幅（±25 映射到 ±1）

        // ===== 部件配色 =====
        private static readonly Color C_Frame = Color.FromRgb(0x64, 0x74, 0x8B);   // 龙门/侧板 石蓝
        private static readonly Color C_FrameDark = Color.FromRgb(0x47, 0x55, 0x69);// 深石
        private static readonly Color C_Carriage = Color.FromRgb(0x94, 0xA3, 0xB8);// 滑座 浅石
        private static readonly Color C_Cabinet = Color.FromRgb(0x33, 0x3D, 0x4D); // 控制柜 深蓝灰
        private static readonly Color C_YMotor = Color.FromRgb(0xF9, 0x73, 0x16);  // 橙
        private static readonly Color C_ZMotor = Color.FromRgb(0x14, 0xB8, 0xA6);  // 青
        private static readonly Color C_Tool = Color.FromRgb(0x8B, 0x5C, 0xF6);    // 紫
        private static readonly Color C_Sensor = Color.FromRgb(0xEC, 0x48, 0x99);  // 粉
        private static readonly Color C_Traj = Color.FromRgb(0x3B, 0x82, 0xF6);    // 蓝
        private static readonly Color C_Point = Color.FromRgb(0x60, 0xA5, 0xFA);   // 浅蓝
        private static readonly Color C_Head = Color.FromRgb(0xEF, 0x44, 0x44);    // 红
        private static readonly Color C_Workpiece = Color.FromRgb(0x3B, 0x82, 0xF6);// 工件 蓝
        private static readonly Color C_LedGreen = Color.FromRgb(0x22, 0xC5, 0x5E); // 指示灯 绿
        private static readonly Color C_WarnG = Color.FromRgb(0x22, 0xC5, 0x5E);    // 警示灯塔 绿
        private static readonly Color C_WarnY = Color.FromRgb(0xFB, 0xBF, 0x24);    // 警示灯塔 黄
        private static readonly Color C_WarnR = Color.FromRgb(0xEF, 0x44, 0x44);    // 警示灯塔 红
        private static readonly Color C_Black = Color.FromRgb(0x1E, 0x29, 0x3B);    // 灯塔底座
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

        // ===== 相机轨道参数 =====
        private double _theta = 0.7;
        private double _phi = 0.5;
        private double _radius = 240;
        private bool _dragging;
        private Point _last;

        // ===== 场景变换（归一化） =====
        private double _scale = 1;
        private Point3D _center = new(0, 0, 0);

        // ===== 共享几何（冻结） =====
        private readonly MeshGeometry3D _sphere = BuildSphere();
        private readonly MeshGeometry3D _cyl = BuildCylinder();
        private readonly MeshGeometry3D _box = BuildBox();

        // ===== 运行时引用 =====
        private readonly List<GeometryModel3D> _pointModels = new();
        private GeometryModel3D? _headModel;
        private ModelVisual3D? _machineGroup;   // 静态结构 + 装饰 + 标签
        private ModelVisual3D? _carriageGroup;  // 滑座（X/Z 向平移）
        private ModelVisual3D? _toolGroup;      // 刀具（Y 向平移，嵌套在滑座下）

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
        }

        // ===================== 场景构建 =====================
        private void BuildScene()
        {
            Root.Children.Clear();
            _pointModels.Clear();
            _headModel = null;
            _machineGroup = null;
            _carriageGroup = null;
            _toolGroup = null;
            _scale = 1;
            _center = new Point3D(0, 0, 0);

            var raw = Points;
            if (raw == null || raw.Count == 0)
            {
                EmptyHint.Visibility = Visibility.Visible;
                return;
            }
            EmptyHint.Visibility = Visibility.Collapsed;

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

            // 地面网格（参考用）
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

            // ===== 真实机台装配（静态 + 动态组） =====
            BuildMachine();

            // 工件（蓝块，位于床面中央）
            AddModel(Root, _box, 22, 6, 18, new Point3D(0, -12, 0), C_Workpiece);

            // ===== 轨迹 + 点位 =====
            var pts = new Point3D[raw.Count];
            for (int i = 0; i < raw.Count; i++) pts[i] = ToMachine(ToSceneSafe(raw[i]));
            for (int i = 0; i < pts.Length - 1; i++)
                AddSegment(pts[i], pts[i + 1], 1.2, C_Traj);
            for (int i = 0; i < pts.Length; i++)
            {
                var col = i == 0 ? C_Traj : C_Point;
                _pointModels.Add(AddModel(Root, _sphere, 3.2, 3.2, 3.2, pts[i], col));
            }

            // 当前位置头（红色）
            if (HeadVisible)
                _headModel = AddModel(Root, _sphere, 4.6, 4.6, 4.6, ToMachine(ToSceneSafe(Head)), C_Head);

            UpdatePose(HeadVisible ? ToSceneSafe(Head) : new Point3D(0, 0, 0));
            UpdateCurrent();
        }

        // 装配真实机台：所有零件逐个归一化后手动定位
        private void BuildMachine()
        {
            _machineGroup = new ModelVisual3D();
            Root.Children.Add(_machineGroup);
            _carriageGroup = new ModelVisual3D();
            _machineGroup.Children.Add(_carriageGroup);
            _toolGroup = new ModelVisual3D();
            _carriageGroup.Children.Add(_toolGroup);

            var baseDir = Path.Combine(AppContext.BaseDirectory, "Models");

            // —— 静态结构件 ——（直接挂 _machineGroup）
            // 左右侧板（龙门立柱）
            TryAddPart(_machineGroup, baseDir, @"side_plates\left\LEFT_PLATE.obj",  new Point3D(-58, 5, 0),  new Vector3D(5, 95, 6),  C_Frame, null);
            TryAddPart(_machineGroup, baseDir, @"side_plates\right\RIGHT_PLATE.obj", new Point3D( 58, 5, 0),  new Vector3D(5, 95, 6),  C_Frame, null);
            // 横梁（基元）
            AddModel(_machineGroup, _box, 125, 6, 10, new Point3D(0, 52, 0), C_Frame);
            // 床面导轨（基元）
            AddModel(_machineGroup, _box, 130, 3, 35, new Point3D(0, -48, 0), C_FrameDark);
            // 控制柜面板（需绕 Y 转 90° 让薄面朝前）
            TryAddPart(_machineGroup, baseDir, @"electronic_box_small\front_panel_electronic_box_small.obj",
                new Point3D(82, -5, 0), new Vector3D(4, 55, 32), C_Cabinet,
                new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
            // 限位感应器
            TryAddPart(_machineGroup, baseDir, @"other\X_AXIS_ENDSTOP_LIMIT_SWITCH_THICK.obj", new Point3D(-60, 5, 0),  new Vector3D(3, 3, 3), C_Sensor, null);
            TryAddPart(_machineGroup, baseDir, @"other\Y_AXIS_ENDSTOP_LIMIT_SWITCH_LONG.obj",  new Point3D(0, 5, 50),   new Vector3D(3, 6, 3), C_Sensor, null);
            // 导轨支撑 / 惰轮
            TryAddPart(_machineGroup, baseDir, @"other\RAILS_SUPPORT.obj",  new Point3D(0, -44, 0),   new Vector3D(22, 3, 8),  C_FrameDark, null);
            TryAddPart(_machineGroup, baseDir, @"other\IDLER_BLOCK.obj",    new Point3D(35, -42, 25), new Vector3D(6, 3, 4),   C_FrameDark, null);
            // 拖链座（横梁上）
            TryAddPart(_machineGroup, baseDir, @"router\CABLE_CHAIN_MOUNT.obj", new Point3D(35, 55, 16), new Vector3D(8, 3, 14), C_FrameDark, null);

            // —— 滑座组（随 X/Z 移动） ——
            TryAddPart(_carriageGroup, baseDir, @"router\CARRIAGE.obj",        new Point3D(0, 47, 0), new Vector3D(22, 8, 22),  C_Carriage, null);
            TryAddPart(_carriageGroup, baseDir, @"router\ROUTER_BRACKET.obj",  new Point3D(0, 41, 0), new Vector3D(16, 8, 14),  C_Frame,    null);
            TryAddPart(_carriageGroup, baseDir, @"router\Z_MOTOR_MOUNT.obj",   new Point3D(0, 35, 0), new Vector3D(14, 6, 14),  C_ZMotor,   null);

            // —— 刀具组（随 Y 移动，嵌套在滑座下） ——
            TryAddPart(_toolGroup, baseDir, @"router\VERTICAL_SLIDER.obj",     new Point3D(0, 25, 0), new Vector3D(10, 22, 10), C_YMotor,   null);
            TryAddPart(_toolGroup, baseDir, @"router\VACUUM_FUNNEL.obj",       new Point3D(0, 8, 0),  new Vector3D(8, 6, 8),    C_Tool,     null);
            TryAddPart(_toolGroup, baseDir, @"router\VACUUM_HOSE_RING.obj",    new Point3D(0, 12, 0), new Vector3D(5, 3, 5),    C_Tool,     null);

            // —— 装饰细节（基元） ——
            // 三色警示灯塔（机台左后角）
            AddModel(_machineGroup, _cyl, 5, 4, 5, new Point3D(-72, -38, -38), C_Black);
            AddModel(_machineGroup, _cyl, 4, 4, 4, new Point3D(-72, -32, -38), C_WarnG);
            AddModel(_machineGroup, _cyl, 4, 4, 4, new Point3D(-72, -26, -38), C_WarnY);
            AddModel(_machineGroup, _cyl, 4, 4, 4, new Point3D(-72, -20, -38), C_WarnR);
            AddModel(_machineGroup, _sphere, 3, 3, 3, new Point3D(-72, -16, -38), C_WarnR);
            // 横梁上的 LED 指示灯（5 个绿点）
            for (int i = 0; i < 5; i++)
                AddModel(_machineGroup, _sphere, 2, 2, 2, new Point3D(-45 + i * 22, 56, 6), C_LedGreen);
            // X 电机端盖指示灯
            AddModel(_machineGroup, _sphere, 1.8, 1.8, 1.8, new Point3D(-12, 47, 12), C_LedGreen);

            // —— 3D 文字标签（Viewport2DVisual3D） ——
            _machineGroup.Children.Add(MakeLabel("机台侧板/龙门",  new Point3D(-58, 65, 0)));
            _machineGroup.Children.Add(MakeLabel("控制柜",        new Point3D( 82, 30, 0)));
            _machineGroup.Children.Add(MakeLabel("限位感应器 X",  new Point3D(-60, 12, 0)));
            _machineGroup.Children.Add(MakeLabel("限位感应器 Y",  new Point3D(  0, 12, 54)));
            _machineGroup.Children.Add(MakeLabel("警示灯塔",      new Point3D(-72, -10, -38)));
            _machineGroup.Children.Add(MakeLabel("拖链",          new Point3D( 35, 62, 16)));
            _carriageGroup.Children.Add(MakeLabel("滑座",         new Point3D( 18, 55, 0)));
            _carriageGroup.Children.Add(MakeLabel("主轴/刀具",    new Point3D( 15, 30, 0)));
        }

        // 加载单个 .obj，归一化到单位立方体（中心原点），修正 Z-up → Y-up
        // 返回归一化后的 MeshGeometry3D（已冻结）；失败返回 null
        private MeshGeometry3D? LoadNormalized(string path)
        {
            MeshGeometry3D? mesh;
            try { mesh = ObjLoader.LoadFile(path); }
            catch { return null; }
            if (mesh == null || mesh.Positions.Count == 0) return null;

            var b = mesh.Bounds;
            double sx = b.SizeX, sy = b.SizeY, sz = b.SizeZ;
            if (sx < 1e-6) sx = 1;
            if (sy < 1e-6) sy = 1;
            if (sz < 1e-6) sz = 1;
            var ctr = new Point3D(b.X + sx / 2, b.Y + sy / 2, b.Z + sz / 2);
            double maxDim = Math.Max(sx, Math.Max(sy, sz));
            double scale = 1.0 / maxDim;

            // 若 Z 跨度 > Y 跨度且明显（>1.1x），视为 Z-up 模型，旋转使 Z→Y
            bool zUp = sz > sy * 1.1;

            var n = new MeshGeometry3D();
            int pc = mesh.Positions.Count;
            for (int i = 0; i < pc; i++)
            {
                var p = mesh.Positions[i];
                double x = (p.X - ctr.X) * scale;
                double y, z;
                if (zUp)
                {
                    y = (p.Z - ctr.Z) * scale;   // Z → Y
                    z = -(p.Y - ctr.Y) * scale;  // Y → -Z（取负保持正面朝外）
                }
                else
                {
                    y = (p.Y - ctr.Y) * scale;
                    z = (p.Z - ctr.Z) * scale;
                }
                n.Positions.Add(new Point3D(x, y, z));
            }
            int nc = mesh.Normals.Count;
            for (int i = 0; i < nc; i++)
            {
                var v = mesh.Normals[i];
                if (zUp) n.Normals.Add(new Vector3D(v.X, v.Z, -v.Y));
                else n.Normals.Add(v);
            }
            n.Freeze();
            return n;
        }

        // 把归一化零件挂到指定 parent：以 size 缩放，以 position 平移（可选额外旋转）
        private void AddPart(ModelVisual3D parent, MeshGeometry3D norm, Point3D position, Vector3D size, Color color, Transform3D? extraRot)
        {
            var mat = new DiffuseMaterial(new SolidColorBrush(color));
            var g = new Transform3DGroup();
            if (extraRot != null) g.Children.Add(extraRot);
            g.Children.Add(new ScaleTransform3D(size.X, size.Y, size.Z));
            g.Children.Add(new TranslateTransform3D(position.X, position.Y, position.Z));
            var gm = new GeometryModel3D(norm, mat) { Transform = g, BackMaterial = mat };
            parent.Children.Add(new ModelVisual3D { Content = gm });
        }

        private void TryAddPart(ModelVisual3D parent, string baseDir, string relPath,
            Point3D position, Vector3D size, Color color, Transform3D? extraRot)
        {
            var mesh = LoadNormalized(Path.Combine(baseDir, relPath));
            if (mesh == null) return;
            AddPart(parent, mesh, position, size, color, extraRot);
        }

        // 生成一个面向 +Z 的浮空文字标签（Viewport2DVisual3D）
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

            // 关键：Viewport2DVisual3D 必须给 Material 设置 IsVisualHostMaterial=True，
            // 否则 Visual（文字）不会被渲染；Brush 用 White（透明会让宿主整体不可见）。
            var hostMat = new DiffuseMaterial(new SolidColorBrush(Colors.White));
            Viewport2DVisual3D.SetIsVisualHostMaterial(hostMat, true);

            var mesh = new MeshGeometry3D();
            double w = 70, h = 18;
            mesh.Positions.Add(new Point3D(-w / 2, -h / 2, 0));
            mesh.Positions.Add(new Point3D( w / 2, -h / 2, 0));
            mesh.Positions.Add(new Point3D( w / 2,  h / 2, 0));
            mesh.Positions.Add(new Point3D(-w / 2,  h / 2, 0));
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

        // 归一化坐标 → 机台工作空间坐标
        private Point3D ToMachine(Point3D n) => new(
            n.X * WorkX / NormHalf,
            -10 + (n.Y / NormHalf) * WorkY,
            n.Z * WorkZ / NormHalf);

        private void UpdateHead()
        {
            if (_machineGroup == null) return;
            UpdatePose(ToSceneSafe(Head));
        }

        // 根据归一化头坐标驱动滑座（X/Z）与刀具（Y），以及红头位置
        private void UpdatePose(Point3D sceneSafe)
        {
            if (_carriageGroup == null || _toolGroup == null) return;
            double nX = Clamp(sceneSafe.X / NormHalf, -1.2, 1.2);
            double nY = Clamp(sceneSafe.Y / NormHalf, -1.2, 1.2);
            double nZ = Clamp(sceneSafe.Z / NormHalf, -1.2, 1.2);

            _carriageGroup.Transform = new TranslateTransform3D(nX * WorkX, 0, nZ * WorkZ);
            _toolGroup.Transform = new TranslateTransform3D(0, nY * WorkY, 0);

            if (_headModel != null)
            {
                var hp = ToMachine(sceneSafe);
                var hg = new Transform3DGroup();
                hg.Children.Add(new ScaleTransform3D(4.6, 4.6, 4.6));
                hg.Children.Add(new TranslateTransform3D(hp.X, hp.Y, hp.Z));
                _headModel.Transform = hg;
            }
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

        // ===================== 几何辅助 =====================
        private static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);

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

        private Point3D ToSceneSafe(Point3D r) => new((r.X - _center.X) * _scale, (r.Y - _center.Y) * _scale, (r.Z - _center.Z) * _scale);

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
    }
}