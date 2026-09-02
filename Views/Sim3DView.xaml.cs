// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 运行轨迹 3D 仿真控件（纯 WPF Media3D，无第三方 3D 库）。
    /// 渲染一台贴近实际的龙门式运动机台：基台 / 龙门架 / X·Y·Z 轴电机 / 工具头 / 感应器，
    /// 运行时龙门架(X)、滑座(Z)、工具头(Y) 跟随轨迹实时运动，红色头标出当前位置。
    /// - Points：点位序列（轨迹路径，随机台工作空间映射）
    /// - Head：当前位置（运行时由 VM 插值驱动）
    /// - HeadVisible：是否显示当前位置头
    /// - CurrentIndex：高亮当前目标点位
    /// 支持鼠标拖拽旋转、滚轮缩放。
    /// </summary>
    public partial class Sim3DView : UserControl
    {
        // ===== 机台几何常数（场景单位） =====
        private const double BedTop = -60;   // 基台台面高度
        private const double BeamY = 50;     // 龙门横梁高度
        private const double FrameX = 40;    // 龙门立柱 X 位置 ±
        private const double SpanZ = 50;     // 横梁沿 Z 跨度 ±
        private const double NormHalf = 40;  // 点位归一化半幅

        // ===== 部件配色 =====
        private static readonly Color C_Bed = Color.FromRgb(0x47, 0x55, 0x69);     // 基台 深石
        private static readonly Color C_Frame = Color.FromRgb(0x64, 0x74, 0x8B);   // 龙门架 石蓝
        private static readonly Color C_XMotor = Color.FromRgb(0xEA, 0xB3, 0x08);  // X 电机 黄
        private static readonly Color C_YMotor = Color.FromRgb(0xF9, 0x73, 0x16);  // Y 电机 橙
        private static readonly Color C_ZMotor = Color.FromRgb(0x14, 0xB8, 0xA6);  // Z 电机 青
        private static readonly Color C_Tool = Color.FromRgb(0x8B, 0x5C, 0xF6);    // 工具头 紫
        private static readonly Color C_Sensor = Color.FromRgb(0xEC, 0x48, 0x99);  // 感应器 粉
        private static readonly Color C_Traj = Color.FromRgb(0x3B, 0x82, 0xF6);    // 轨迹 蓝
        private static readonly Color C_Point = Color.FromRgb(0x60, 0xA5, 0xFA);  // 点位 浅蓝
        private static readonly Color C_Head = Color.FromRgb(0xEF, 0x44, 0x44);    // 当前位置 红

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
        private double _radius = 200;
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
        private readonly Transform3D _toolRot =
            new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180)); // 工具头锥尖朝下

        // ===== 运行时引用 =====
        private readonly List<GeometryModel3D> _pointModels = new();
        private GeometryModel3D? _headModel;
        private ModelVisual3D? _gantryVis;   // 龙门架（X 向平移）
        private ModelVisual3D? _carriageVis; // 滑座（Z 向平移）
        private GeometryModel3D? _toolModel; // 工具头（Y 向平移）
        private GeometryModel3D? _zColModel; // Z 立柱（随工具头伸缩）

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
                _radius = Math.Max(70, Math.Min(600, _radius));
                UpdateCamera();
            };
        }

        // ===================== 场景构建 =====================
        private void BuildScene()
        {
            Root.Children.Clear();
            _pointModels.Clear();
            _headModel = null;
            _gantryVis = null;
            _carriageVis = null;
            _toolModel = null;
            _zColModel = null;
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

            // 地面网格（参考用，置于基台下方）
            double groundY = BedTop - 8;
            double span = 90;
            var plane = new MeshGeometry3D();
            plane.Positions.Add(new Point3D(-span, groundY, -span));
            plane.Positions.Add(new Point3D(span, groundY, -span));
            plane.Positions.Add(new Point3D(span, groundY, span));
            plane.Positions.Add(new Point3D(-span, groundY, span));
            plane.TriangleIndices.Add(0); plane.TriangleIndices.Add(1); plane.TriangleIndices.Add(2);
            plane.TriangleIndices.Add(0); plane.TriangleIndices.Add(2); plane.TriangleIndices.Add(3);
            AddModel(Root, plane, 1, 1, 1, new Point3D(0, 0, 0), Color.FromArgb(38, 226, 232, 240), null);

            // ===== 机台：基台 + 感应器（固定） =====
            AddModel(Root, _box, 150, 8, 120, new Point3D(0, BedTop - 4, 0), C_Bed);          // 基台
            AddModel(Root, _box, 6, 6, 6, new Point3D(FrameX + 6, BedTop + 5, 0), C_Sensor);   // X 感应器(右)
            AddModel(Root, _box, 6, 6, 6, new Point3D(-(FrameX + 6), BedTop + 5, 0), C_Sensor);// X 感应器(左)

            // ===== 龙门架（X 向平移） =====
            _gantryVis = new ModelVisual3D();
            Root.Children.Add(_gantryVis);
            AddModel(_gantryVis, _box, 10, 110, 14, new Point3D(FrameX, -5, 0), C_Frame);   // 右立柱
            AddModel(_gantryVis, _box, 10, 110, 14, new Point3D(-FrameX, -5, 0), C_Frame);  // 左立柱
            AddModel(_gantryVis, _box, 12, 12, 2 * SpanZ + 16, new Point3D(0, BeamY, 0), C_Frame); // 横梁
            AddModel(_gantryVis, _box, 16, 16, 16, new Point3D(FrameX, -5, SpanZ), C_XMotor);    // X 轴电机
            AddModel(_gantryVis, _box, 6, 6, 6, new Point3D(0, BeamY, SpanZ + 10), C_Sensor);    // Y 感应器(横梁端)

            // ===== 滑座（Z 向平移，挂在龙门架下） =====
            _carriageVis = new ModelVisual3D();
            _gantryVis.Children.Add(_carriageVis);
            AddModel(_carriageVis, _box, 22, 10, 22, new Point3D(0, BeamY, 0), C_Frame);   // 滑座块
            AddModel(_carriageVis, _box, 12, 12, 16, new Point3D(0, BeamY, 0), C_YMotor);   // Y 轴电机
            AddModel(_carriageVis, _box, 12, 12, 12, new Point3D(0, BeamY - 12, 0), C_ZMotor); // Z 轴电机
            AddModel(_carriageVis, _box, 6, 6, 6, new Point3D(0, BeamY - 26, 0), C_Sensor); // Z 感应器

            // Z 立柱（随工具头伸缩）与工具头（动态）
            _zColModel = AddModel(_carriageVis, _cyl, 4, 1, 4, new Point3D(0, 0, 0), C_Tool);
            _toolModel = AddModel(_carriageVis, _cone, 7, 14, 7, new Point3D(0, 0, 0), C_Tool, _toolRot);

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

        // 归一化坐标 → 机台工作空间坐标
        private Point3D ToMachine(Point3D n) => new(
            n.X * (FrameX - 5) / NormHalf,
            -30 + (n.Y / NormHalf) * 25,
            n.Z * (SpanZ - 5) / NormHalf);

        private void UpdateHead()
        {
            if (_gantryVis == null) return;
            UpdatePose(ToSceneSafe(Head));
        }

        // 根据归一化头坐标驱动龙门架/滑座/工具头/Z立柱 + 红头
        private void UpdatePose(Point3D n)
        {
            if (_gantryVis == null || _carriageVis == null || _toolModel == null || _zColModel == null) return;
            double gx = n.X * (FrameX - 5) / NormHalf;
            double gz = n.Z * (SpanZ - 5) / NormHalf;
            double gy = -30 + (n.Y / NormHalf) * 25;

            _gantryVis.Transform = new TranslateTransform3D(gx, 0, 0);
            _carriageVis.Transform = new TranslateTransform3D(0, 0, gz);

            var tg = new Transform3DGroup();
            tg.Children.Add(_toolRot);
            tg.Children.Add(new ScaleTransform3D(7, 14, 7));
            tg.Children.Add(new TranslateTransform3D(0, gy, 0));
            _toolModel.Transform = tg;

            double len = BeamY - gy;
            double midY = (BeamY + gy) / 2;
            var zg = new Transform3DGroup();
            zg.Children.Add(new ScaleTransform3D(4, len, 4));
            zg.Children.Add(new TranslateTransform3D(0, midY, 0));
            _zColModel.Transform = zg;

            if (_headModel != null)
            {
                var hg = new Transform3DGroup();
                hg.Children.Add(new ScaleTransform3D(4.6, 4.6, 4.6));
                hg.Children.Add(new TranslateTransform3D(gx, gy, gz));
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
        // 在指定 parent 下添加一个模型，返回 GeometryModel3D 以便后续更新变换
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
