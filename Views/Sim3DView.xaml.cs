// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
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
    /// 运行轨迹 3D 仿真控件（纯 WPF Media3D，无第三方 3D 库）。
    /// 真实机台由下载的 WaveFront .obj 零件装配而成（龙门侧板 / 滑座 / 主轴 / 限位 / 控制柜等），
    /// 通过自写 <see cref="ObjLoader"/> 解析并在共享 CAD 坐标系下整体归一化。
    /// 运行时：滑座（X）/ 主轴组（Z、Y）跟随轨迹头实时运动，红色头标出当前位置。
    /// - Points：点位序列（轨迹路径，随机台工作空间映射）
    /// - Head：当前位置（运行时由 VM 插值驱动）
    /// - HeadVisible：是否显示当前位置头
    /// - CurrentIndex：高亮当前目标点位
    /// 支持鼠标拖拽旋转、滚轮缩放。
    /// </summary>
    public partial class Sim3DView : UserControl
    {
        // ===== 机台几何常数（场景单位） =====
        private const double BedTop = -60;   // 基台台面高度（保留兼容）
        private const double FrameX = 40;    // 龙门立柱 X 位置 ±
        private const double SpanZ = 50;     // 横梁沿 Z 跨度 ±
        private const double NormHalf = 40;  // 点位归一化半幅
        private const double WorldHalfX = 45;// 滑座 X 向运动半幅（场景单位）
        private const double WorldHalfZ = 35;// 滑座 Z 向运动半幅
        private const double WorldHalfY = 22;// 刀具 Y 向运动半幅

        // ===== 部件配色 =====
        private static readonly Color C_Bed = Color.FromRgb(0x47, 0x55, 0x69);     // 基台 深石
        private static readonly Color C_Frame = Color.FromRgb(0x64, 0x74, 0x8B);   // 龙门架/侧板 石蓝
        private static readonly Color C_FrameDark = Color.FromRgb(0x47, 0x55, 0x69);// 深石
        private static readonly Color C_Carriage = Color.FromRgb(0x94, 0xA3, 0xB8);// 滑座 浅石
        private static readonly Color C_Cabinet = Color.FromRgb(0x33, 0x3D, 0x4D); // 控制柜 深蓝灰
        private static readonly Color C_XMotor = Color.FromRgb(0xEA, 0xB3, 0x08);  // X 电机 黄
        private static readonly Color C_YMotor = Color.FromRgb(0xF9, 0x73, 0x16);  // Y 电机 橙
        private static readonly Color C_ZMotor = Color.FromRgb(0x14, 0xB8, 0xA6);  // Z 电机 青
        private static readonly Color C_Tool = Color.FromRgb(0x8B, 0x5C, 0xF6);    // 工具头 紫
        private static readonly Color C_Sensor = Color.FromRgb(0xEC, 0x48, 0x99);  // 感应器 粉
        private static readonly Color C_Traj = Color.FromRgb(0x3B, 0x82, 0xF6);    // 轨迹 蓝
        private static readonly Color C_Point = Color.FromRgb(0x60, 0xA5, 0xFA);   // 点位 浅蓝
        private static readonly Color C_Head = Color.FromRgb(0xEF, 0x44, 0x44);    // 当前位置 红
        private static readonly Color C_Workpiece = Color.FromRgb(0x3B, 0x82, 0xF6);// 工件 蓝

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
        private double _radius = 220;
        private bool _dragging;
        private Point _last;

        // ===== 场景变换（归一化） =====
        private double _scale = 1;
        private Point3D _center = new(0, 0, 0);

        // ===== 共享几何（冻结，多个模型复用） =====
        private readonly MeshGeometry3D _sphere = BuildSphere();
        private readonly MeshGeometry3D _cyl = BuildCylinder();
        private readonly MeshGeometry3D _cone = BuildCone();
        private readonly MeshGeometry3D _box = BuildBox();

        // ===== 运行时引用 =====
        private readonly List<GeometryModel3D> _pointModels = new();
        private GeometryModel3D? _headModel;
        private ModelVisual3D? _machineGroup;   // 整台机台（全局归一化）
        private ModelVisual3D? _carriageGroup;  // 滑座（X 向平移）
        private ModelVisual3D? _routerGroup;    // 主轴/刀具组（Z、Y 向平移）
        private Rect3D _machineBounds = Rect3D.Empty;
        private double _globalScale = 1;
        private bool _zUp;

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
                _radius = Math.Max(90, Math.Min(700, _radius));
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
            _routerGroup = null;
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
            double groundY = -82;
            double span = 100;
            var plane = new MeshGeometry3D();
            plane.Positions.Add(new Point3D(-span, groundY, -span));
            plane.Positions.Add(new Point3D(span, groundY, -span));
            plane.Positions.Add(new Point3D(span, groundY, span));
            plane.Positions.Add(new Point3D(-span, groundY, span));
            plane.TriangleIndices.Add(0); plane.TriangleIndices.Add(1); plane.TriangleIndices.Add(2);
            plane.TriangleIndices.Add(0); plane.TriangleIndices.Add(2); plane.TriangleIndices.Add(3);
            AddModel(Root, plane, 1, 1, 1, new Point3D(0, 0, 0), Color.FromArgb(38, 226, 232, 240), null);

            // ===== 真实机台模型装配 =====
            BuildMachine();

            // 工件（简化蓝块，置于机台中部）
            AddModel(Root, _box, 20, 8, 16, new Point3D(0, -50, 0), C_Workpiece);

            // ===== 轨迹 + 点位（映射到机台工作空间） =====
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

            // 初始姿态
            UpdatePose(HeadVisible ? ToSceneSafe(Head) : new Point3D(0, 0, 0));
            UpdateCurrent();
        }

        // 装配真实机台：静态结构件 + 滑座组 + 主轴组，整体归一化
        private void BuildMachine()
        {
            _machineGroup = new ModelVisual3D();
            Root.Children.Add(_machineGroup);
            _carriageGroup = new ModelVisual3D();
            _routerGroup = new ModelVisual3D();
            _carriageGroup.Children.Add(_routerGroup);

            var baseDir = Path.Combine(AppContext.BaseDirectory, "Models");

            // —— 静态结构件（直接挂 _machineGroup）——
            AddObj(_machineGroup, Path.Combine(baseDir, @"side_plates\left\LEFT_PLATE.obj"), C_Frame);
            AddObj(_machineGroup, Path.Combine(baseDir, @"side_plates\right\RIGHT_PLATE.obj"), C_Frame);
            AddObj(_machineGroup, Path.Combine(baseDir, @"electronic_box_small\front_panel_electronic_box_small.obj"), C_Cabinet);
            AddObj(_machineGroup, Path.Combine(baseDir, @"other\X_AXIS_ENDSTOP_LIMIT_SWITCH_THICK.obj"), C_Sensor);
            AddObj(_machineGroup, Path.Combine(baseDir, @"other\Y_AXIS_ENDSTOP_LIMIT_SWITCH_LONG.obj"), C_Sensor);
            AddObj(_machineGroup, Path.Combine(baseDir, @"other\RAILS_SUPPORT.obj"), C_Frame);
            AddObj(_machineGroup, Path.Combine(baseDir, @"other\IDLER_BLOCK.obj"), C_Frame);
            AddObj(_machineGroup, Path.Combine(baseDir, @"router\CABLE_CHAIN_MOUNT.obj"), C_FrameDark);

            // —— 滑座组（随龙门 X 向移动）——
            AddObj(_carriageGroup, Path.Combine(baseDir, @"router\CARRIAGE.obj"), C_Carriage);
            AddObj(_carriageGroup, Path.Combine(baseDir, @"router\ROUTER_BRACKET.obj"), C_Frame);
            AddObj(_carriageGroup, Path.Combine(baseDir, @"router\Z_MOTOR_MOUNT.obj"), C_ZMotor);

            // —— 主轴/刀具组（随滑座 Z、工具 Y 移动）——
            AddObj(_routerGroup, Path.Combine(baseDir, @"router\VERTICAL_SLIDER.obj"), C_YMotor);
            AddObj(_routerGroup, Path.Combine(baseDir, @"router\VACUUM_FUNNEL.obj"), C_Tool);
            AddObj(_routerGroup, Path.Combine(baseDir, @"router\VACUUM_HOSE_RING.obj"), C_Tool);

            _machineGroup.Children.Add(_carriageGroup);

            // 计算全局包围盒 → 居中 + 归一化 + Z-up 校正
            if (_machineBounds.IsEmpty) return;
            var b = _machineBounds;
            var ctr = new Point3D(b.X + b.SizeX / 2, b.Y + b.SizeY / 2, b.Z + b.SizeZ / 2);
            double maxDim = Math.Max(b.SizeX, Math.Max(b.SizeY, b.SizeZ));
            if (maxDim < 1e-6) maxDim = 1;
            _globalScale = 150.0 / maxDim;
            _zUp = b.SizeZ > b.SizeY * 1.15; // CAD 大概率 Z-up，旋转到 WPF 的 Y-up

            var tg = new Transform3DGroup();
            tg.Children.Add(new TranslateTransform3D(-ctr.X, -ctr.Y, -ctr.Z)); // 先居中
            tg.Children.Add(new ScaleTransform3D(_globalScale, _globalScale, _globalScale));
            if (_zUp)
                tg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), -90)));
            _machineGroup.Transform = tg;
        }

        private void AddObj(ModelVisual3D parent, string path, Color color)
        {
            MeshGeometry3D? mesh = null;
            try { mesh = ObjLoader.LoadFile(path); }
            catch { return; }
            if (mesh == null || mesh.Positions.Count == 0) return;
            var mat = new DiffuseMaterial(new SolidColorBrush(color));
            var gm = new GeometryModel3D(mesh, mat) { BackMaterial = mat };
            parent.Children.Add(new ModelVisual3D { Content = gm });
            var mb = mesh.Bounds;
            _machineBounds = _machineBounds.IsEmpty ? mb : Rect3D.Union(_machineBounds, mb);
        }

        // 归一化坐标 → 机台工作空间坐标
        private Point3D ToMachine(Point3D n) => new(
            n.X * (FrameX - 5) / NormHalf,
            -30 + (n.Y / NormHalf) * 25,
            n.Z * (SpanZ - 5) / NormHalf);

        private void UpdateHead()
        {
            if (_machineGroup == null) return;
            UpdatePose(ToSceneSafe(Head));
        }

        // 根据归一化头坐标驱动滑座/主轴组 + 红头
        private void UpdatePose(Point3D sceneSafe)
        {
            if (_carriageGroup == null || _routerGroup == null) return;
            // 归一化 n ≈ [-1,1]
            double nX = Clamp(sceneSafe.X / NormHalf, -1.2, 1.2);
            double nY = Clamp(sceneSafe.Y / NormHalf, -1.2, 1.2);
            double nZ = Clamp(sceneSafe.Z / NormHalf, -1.2, 1.2);
            double wx = nX * WorldHalfX;
            double wz = nZ * WorldHalfZ;
            double wy = nY * WorldHalfY;

            Vector3D cNative, rNative;
            if (_zUp)
            {
                cNative = new Vector3D(wx / _globalScale, -wz / _globalScale, 0);
                rNative = new Vector3D(0, 0, wy / _globalScale);
            }
            else
            {
                cNative = new Vector3D(wx / _globalScale, 0, wz / _globalScale);
                rNative = new Vector3D(0, wy / _globalScale, 0);
            }
            _carriageGroup.Transform = new TranslateTransform3D(cNative);
            _routerGroup.Transform = new TranslateTransform3D(rNative);

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

        private static MeshGeometry3D BuildCone(int slices = 18)
        {
            var m = new MeshGeometry3D();
            int apex = m.Positions.Count; m.Positions.Add(new Point3D(0, 0.5, 0));
            for (int j = 0; j <= slices; j++)
            {
                double theta = 2 * Math.PI * j / slices;
                m.Positions.Add(new Point3D(Math.Cos(theta), -0.5, Math.Sin(theta)));
            }
            for (int j = 0; j < slices; j++)
            {
                m.TriangleIndices.Add(apex); m.TriangleIndices.Add(1 + j); m.TriangleIndices.Add(1 + j + 1);
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
