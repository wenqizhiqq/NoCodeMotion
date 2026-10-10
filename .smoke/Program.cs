// 仓库内冒烟工程（.smoke\）：主工程已 <Compile Remove=".smoke\**"/>，不会互相干扰。
// 跑法（仓库根目录）：
//   dotnet build .smoke/ncm_smoke.csproj -p:UseAppHost=false
//   dotnet exec .smoke/bin/Debug/net10.0-windows10.0.19041.0/ncm_smoke.dll
// ★ 输出目录随目标框架走（主工程已升到 net10.0-windows10.0.19041.0，别再跑老的 net10.0-windows 副本）。
// ★ 要让 OpenCvSharp 原生库可加载（其依赖 opencv_world480.dll 在 dotnet exec 下不在搜索路径），
//   把输出目录加进 PATH 再跑：PATH="<输出目录>:$PATH" dotnet exec <dll>；
//   否则段 H 诊断项、段 M 的 OCR 端到端、段 N 的「相机回退」会跳过（不影响其余断言）。
// 段 N：图像采集来源 —— 默认「相机」/ 相机名解析 / 文件或文件夹路径无效时明确报错（不静默出测试图）。
// 段 O：视觉流程页结果图 —— 滚轮缩放 / 中键拖拽平移 / 双击复位（ZoomPanBehavior 的光标锚定与 1:1 跟手不变式）。
// 段 Q：字符识别结果 —— 每行文字画绿框 + 框上方绿字标签（TextOverlayBox 投影 + XAML 叠加层）。
// 段 R：视觉标定 —— 9 点 XY 仿射 + 5 点旋转圆拟合（合成数据端到端：假轴 + 合成圆斑 → 还原像素当量/方向/旋转中心）。
// 段 S：真实 SECS/HSMS 通讯 —— E5 数据项编解码 + E37 会话 + 127.0.0.1 回环端到端（Select / S1F1→S1F2 / S6F11 / Separate）。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Camera;
using NoCodeMotion.Services.Vision;
using NoCodeMotion.Services.Vision.Calibration;
using NoCodeMotion.Services.Hardware.Comm;
using NoCodeMotion.Services.Hardware.Comm.Secs;
using NoCodeMotion.ViewModels;
using NoCodeMotion.Behaviors;

namespace NcmSmoke
{
    internal static class Program
    {
        private static int _fail;

        private static void Check(bool ok, string msg)
        {
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + msg);
            if (!ok) _fail++;
        }

        private static void Section(string s) => Console.WriteLine("\n=== " + s + " ===");

        /// <summary>从 exe 目录向上找仓库根（含 NoCodeMotion.csproj），返回 &lt;repo&gt;/.smoke/out。</summary>
        private static string? RepoSmokeOut()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                if (File.Exists(Path.Combine(d.FullName, "NoCodeMotion.csproj")))
                    return Path.Combine(d.FullName, ".smoke", "out");
            }
            return null;
        }

        /// <summary>从任意可视化子元素向上找最近的卡片 Border（Style == CardBorderStyle）。</summary>
        private static System.Windows.Controls.Border? CardOf(DependencyObject? d)
        {
            var style = Application.Current?.TryFindResource("CardBorderStyle") as Style;
            for (var cur = d; cur != null; cur = VisualTreeHelper.GetParent(cur))
                if (cur is System.Windows.Controls.Border b && style != null && ReferenceEquals(b.Style, style))
                    return b;
            return null;
        }

        private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
        {
            int n = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < n; i++)
            {
                var c = VisualTreeHelper.GetChild(root, i);
                yield return c;
                foreach (var d in Descendants(c)) yield return d;
            }
        }

        /// <summary>可视化树 + 逻辑树的直接子级（离屏、没进窗口时可视化树可能还没展开）。</summary>
        private static List<DependencyObject> Children(DependencyObject d)
        {
            var list = new List<DependencyObject>();
            try
            {
                int n = VisualTreeHelper.GetChildrenCount(d);
                for (int i = 0; i < n; i++) list.Add(VisualTreeHelper.GetChild(d, i));
            }
            catch { }
            try
            {
                foreach (var o in LogicalTreeHelper.GetChildren(d))
                    if (o is DependencyObject x && !list.Contains(x)) list.Add(x);
            }
            catch { }
            return list;
        }

        /// <summary>可视化树 + 逻辑树的全部后代（按引用去重）。</summary>
        private static IEnumerable<DependencyObject> AllDescendants(DependencyObject root)
        {
            var seen = new HashSet<DependencyObject> { root };
            var stack = new Stack<DependencyObject>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                foreach (var c in Children(stack.Pop()))
                    if (seen.Add(c)) { yield return c; stack.Push(c); }
            }
        }

        /// <summary>造一张有明显纹理的合成图（渐变 + 棋盘格），用来肉眼判断「缩放/平移是否真的生效」。</summary>
        private static BitmapSource MakeTestBitmap(int w, int h)
        {
            int stride = w * 4;
            var px = new byte[h * stride];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * stride + x * 4;
                    px[i + 0] = (byte)(x * 255 / Math.Max(1, w - 1));                 // B
                    px[i + 1] = (byte)(y * 255 / Math.Max(1, h - 1));                 // G
                    px[i + 2] = (byte)(((x / 40) + (y / 40)) % 2 == 0 ? 40 : 215);    // R（棋盘格）
                    px[i + 3] = 255;                                                  // A
                }
            var bmp = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, px, stride);
            bmp.Freeze();
            return bmp;
        }

        [STAThread]
        private static int Main()
        {
            ProjectStore.SuppressSave(true);   // 冒烟测试绝不写盘

            // ================= A. 画布坐标 → 机器坐标 =================
            Section("A. 画布坐标 → 机器坐标（框尺寸 + 起点偏移；画布 Y 向下 → 机器 Y 向上）");
            var g = new GraphPointGenViewModel();
            g.FrameW = 100; g.FrameH = 200; g.OriginX = 10; g.OriginY = -5;

            var got = new List<Point>();
            g.Generated += pts => { got.Clear(); got.AddRange(pts); };

            g.AddPoint(0, 260);     // 画布左下角
            g.AddPoint(340, 0);     // 画布右上角
            g.AddPoint(170, 130);   // 画布正中
            Check(g.Points.Count == 3, "画布记录了 3 个点位（实际 " + g.Points.Count + "）");

            g.GenerateCommand.Execute(null);
            Check(got.Count == 3, "生成回传 3 个机器坐标点位（实际 " + got.Count + "）");
            if (got.Count == 3)
            {
                Check(Math.Abs(got[0].X - 10) < 1e-6 && Math.Abs(got[0].Y - (-5)) < 1e-6,
                      "左下角 → 起点 (10, -5)  实际 (" + got[0].X + ", " + got[0].Y + ")");
                Check(Math.Abs(got[1].X - 110) < 1e-6 && Math.Abs(got[1].Y - 195) < 1e-6,
                      "右上角 → (起点X+框宽, 起点Y+框高) = (110, 195)  实际 (" + got[1].X + ", " + got[1].Y + ")");
                Check(Math.Abs(got[2].X - 60) < 1e-6 && Math.Abs(got[2].Y - 95) < 1e-6,
                      "正中 → (60, 95)  实际 (" + got[2].X + ", " + got[2].Y + ")");
            }

            // ================= B. 越界夹取 =================
            Section("B. 越界夹取（点必须在画布内）");
            g.ClearCommand.Execute(null);
            g.AddPoint(-50, 999);
            Check(g.Points.Count == 1
                  && Math.Abs(g.Points[0].X - 0) < 1e-6
                  && Math.Abs(g.Points[0].Y - 260) < 1e-6,
                  "(-50, 999) 夹到 (0, 260)  实际 (" + g.Points[0].X + ", " + g.Points[0].Y + ")");

            // ================= C. 折线采样 =================
            Section("C. 折线采样（步长 >0 时按机器距离密集采样）");
            g.ClearCommand.Execute(null);
            g.FrameW = 100; g.FrameH = 100; g.OriginX = 0; g.OriginY = 0;
            g.PolylineMode = true;
            g.Step = 0;
            g.AddPoint(0, 260);      // 机器 (0, 0)
            g.AddPoint(340, 260);    // 机器 (100, 0) —— 一条长 100 的水平线
            g.GenerateCommand.Execute(null);
            Check(got.Count == 2, "步长 0 → 只取折线顶点（2 个），实际 " + got.Count);

            g.Step = 10;             // 100 / 10 = 10 段 → 10 个起点 + 末点 = 11
            g.GenerateCommand.Execute(null);
            Check(got.Count == 11, "步长 10 → 采样 11 个点，实际 " + got.Count);
            if (got.Count == 11)
            {
                Check(Math.Abs(got[0].X) < 1e-6 && Math.Abs(got[10].X - 100) < 1e-6,
                      "采样首末 X = 0 / 100  实际 " + got[0].X + " / " + got[10].X);
                Check(got.All(p => Math.Abs(p.Y) < 1e-6), "整条采样线 Y 恒为 0");
            }

            // ================= D. 撤销 / 新折线 / 清空 =================
            Section("D. 撤销 / 新折线 / 清空 / 单击模式");
            g.Step = 0;
            g.ClearCommand.Execute(null);
            g.AddPoint(0, 0); g.AddPoint(10, 10);
            Check(g.Points.Count == 2, "折线模式连续单击 → 1 条折线 2 个点");
            g.NewStrokeCommand.Execute(null);
            g.AddPoint(20, 20);
            Check(g.Points.Count == 3, "新折线后 → 3 个点");
            g.UndoCommand.Execute(null);
            Check(g.Points.Count == 2, "撤销 → 2 个点");
            g.ClearCommand.Execute(null);
            Check(g.Points.Count == 0, "清空 → 0 个点");

            g.PolylineMode = false;
            g.AddPoint(1, 1); g.AddPoint(2, 2);
            Check(g.Points.Count == 2, "单击模式 → 2 个独立点（各自成条）");

            // ================= E. PointViewModel 接线 =================
            Section("E. PointViewModel.Graph 接线 → 追加到当前工位（轴1←X、轴2←Y）");
            try
            {
                var vm = new PointViewModel();
                Check(vm.Graph != null, "PointViewModel 暴露了 Graph 属性");

                var table = new PointTable { Name = "冒烟工位" };
                vm.Items.Add(table);
                vm.SelectedItem = table;

                vm.Graph.FrameW = 100; vm.Graph.FrameH = 100;
                vm.Graph.OriginX = 0; vm.Graph.OriginY = 0;
                vm.Graph.Speed = 777; vm.Graph.Prefix = "G";
                vm.Graph.Step = 0; vm.Graph.PolylineMode = false;
                vm.Graph.AddPoint(0, 260);     // 机器 (0, 0)
                vm.Graph.AddPoint(340, 0);     // 机器 (100, 100)
                vm.Graph.GenerateCommand.Execute(null);

                Check(table.Points.Count == 2, "工位里追加了 2 个点位（实际 " + table.Points.Count + "）");
                if (table.Points.Count >= 2)
                {
                    var a = table.Points[0];
                    var b = table.Points[1];
                    Check(a.Name == "G1" && b.Name == "G2", "命名 = 前缀 + 序号（" + a.Name + ", " + b.Name + "）");
                    Check(Math.Abs(a.Positions[0].Position!.Value - 0) < 1e-6
                          && Math.Abs(a.Positions[1].Position!.Value - 0) < 1e-6,
                          "轴1←X、轴2←Y（第 1 点 0, 0）");
                    Check(Math.Abs(b.Positions[0].Position!.Value - 100) < 1e-6
                          && Math.Abs(b.Positions[1].Position!.Value - 100) < 1e-6,
                          "轴1←X、轴2←Y（第 2 点 100, 100）");
                    Check(a.Positions[0].Speed == 777 && a.Positions[1].Speed == 777,
                          "轴1 / 轴2 速度 = 画布速度 777");
                    Check(a.Positions[2].Position is null || a.Positions[2].Position == 0,
                          "轴3 未写入（留空 / 0）");
                }

                // 重名保护：同参数再来一次 → 名字必须仍唯一
                vm.Graph.GenerateCommand.Execute(null);
                Check(table.Points.Count == 4, "第二次生成 → 共 4 个点位（实际 " + table.Points.Count + "）");
                var names = table.Points.Select(p => p.Name).ToList();
                Check(names.Distinct().Count() == names.Count,
                      "点位名无重复：" + string.Join(",", names));
            }
            catch (Exception ex)
            {
                Console.WriteLine("  SKIP  PointViewModel 无法在无 UI 线程下构造："
                                  + ex.GetType().Name + " " + ex.Message);
            }

            // ================= F. XAML 资源可达性（真机崩溃回归） =================
            // 构建通不过这一关：StaticResource 找不到是**运行期** XamlParseException。
            // 必须真的 new 一次页面，才会走 InitializeComponent → 解析 XAML。
            Section("F. 真实实例化页面 / 弹窗：XAML 里的 StaticResource 必须全部解析得到");
            try
            {
                var app = new NoCodeMotion.App();
                app.InitializeComponent();     // 载入 Application.Resources（Resources/AppStyles.xaml）
                Check(Application.Current != null, "Application 已建立，应用级资源链可用");

                try
                {
                    var pp = new NoCodeMotion.Views.PointPage();
                    Check(pp != null, "PointPage 构造成功（InitializeComponent 未抛 XamlParseException）");
                }
                catch (Exception ex)
                {
                    Check(false, "PointPage 构造抛异常：" + ex.GetType().Name + " / "
                                 + (ex.InnerException?.Message ?? ex.Message));
                }

                try
                {
                    var ep = new NoCodeMotion.Views.EngineerPage();
                    Check(ep != null, "EngineerPage 构造成功");
                }
                catch (Exception ex)
                {
                    Check(false, "EngineerPage 构造抛异常：" + ex.GetType().Name + " / "
                                 + (ex.InnerException?.Message ?? ex.Message));
                }

                try
                {
                    var cp = new NoCodeMotion.Views.CameraPage();
                    Check(cp != null, "CameraPage 构造成功（预览 Image / 搜索设备按钮 的 XAML 解析通过）");
                }
                catch (Exception ex)
                {
                    Check(false, "CameraPage 构造抛异常：" + ex.GetType().Name + " / "
                                 + (ex.InnerException?.Message ?? ex.Message));
                }

                try
                {
                    // 视觉流程页：新加的「相机」可编辑下拉 + CameraNames 绑定都在这里走一遍 XAML 解析
                    var vfp = new NoCodeMotion.Views.VisualFlowPage();
                    Check(vfp != null, "VisualFlowPage 构造成功（图像采集参数卡的 XAML 解析通过）");
                }
                catch (Exception ex)
                {
                    Check(false, "VisualFlowPage 构造抛异常：" + ex.GetType().Name + " / "
                                 + (ex.InnerException?.Message ?? ex.Message));
                }

                try
                {
                    var gg = new NoCodeMotion.Views.GraphGenDialog();
                    gg.DataContext = new GraphPointGenViewModel();
                    Check(gg != null, "GraphGenDialog 构造成功（图像生成点位弹窗的 XAML 解析通过）");
                }
                catch (Exception ex)
                {
                    Check(false, "GraphGenDialog 构造抛异常：" + ex.GetType().Name + " / "
                                 + (ex.InnerException?.Message ?? ex.Message));
                }

                try
                {
                    // 通讯页：新加的 SECS/HSMS 参数卡 + 「接收 / 建立会话 / 探活」按钮 + SML 预设下拉
                    // 都在这里真走一遍 InitializeComponent（build 抓不到 StaticResource 键写错 / 绑定写错）
                    var cmp = new NoCodeMotion.Views.CommPage();
                    Check(cmp != null, "CommPage 构造成功（SECS 参数卡 / 端口芯片 / 接收按钮 的 XAML 解析通过）");
                }
                catch (Exception ex)
                {
                    Check(false, "CommPage 构造抛异常：" + ex.GetType().Name + " / "
                                 + (ex.InnerException?.Message ?? ex.Message));
                }
            }
            catch (Exception ex)
            {
                Check(false, "建立 Application 失败：" + ex.Message);
            }

            // ================= G. 相机层：无 MVS 运行库时必须优雅降级 =================
            // 本机只装了海康托管封装（MvCameraControl.Net.dll），缺原生 MvCameraControl.dll，
            // 所以这里断言「不抛异常 + 给出可读原因 + 回退仿真」，而不是真取到图。
            Section("G. 相机层：真实取像不可用时优雅降级（不崩、给原因、回退仿真）");
            try
            {
                bool runtime = MvsCameraService.IsRuntimeAvailable;   // 首次会真枚举一次
                Console.WriteLine("  INFO  IsRuntimeAvailable = " + runtime
                                  + "  LastError = " + MvsCameraService.LastError);

                var devs = MvsCameraService.Enumerate();
                Check(devs != null, "Enumerate() 不抛异常（返回 " + (devs?.Count ?? 0) + " 台）");
                Check(MvsCameraService.Match(devs, null, 0) is null || devs.Count > 0,
                      "Match() 在空列表上返回 null 而不是抛异常");
                if (!runtime)
                    Check(MvsCameraService.LastError.Length > 0,
                          "运行库不可用时 LastError 给出可读原因");

                var cvm = new CameraViewModel();
                Check(cvm.SearchDevicesCommand != null, "CameraViewModel 暴露 SearchDevicesCommand");
                Check(cvm.Preview == null && !cvm.HasPreview, "初始无预览帧（HasPreview=false）");

                var cam = new CameraItem { Name = "冒烟相机" };
                cvm.Items.Add(cam);
                cvm.SelectedItem = cam;

                cvm.SearchDevicesCommand.Execute(null);
                Check(cvm.StatusMessage.Length > 0, "搜索设备后状态栏有提示：" + cvm.StatusMessage);

                cvm.ConnectCommand.Execute(null);
                Check(cam.IsConnected == false, "无真实相机时 Connect 不会谎报已连接");
                Check(cvm.StatusMessage.Length > 0, "连接失败原因已写进状态栏：" + cvm.StatusMessage);

                cvm.CaptureCommand.Execute(null);
                Check(cvm.Preview != null && cvm.HasPreview,
                      "无真实相机时 Capture 回退仿真并产出可显示预览帧");
                Check(cam.LastResult.Length > 0, "LastResult 已写入：" + cam.LastResult);
            }
            catch (Exception ex)
            {
                Check(false, "相机层降级路径抛异常：" + ex.GetType().Name + " / " + ex.Message);
            }

            // ================= H. 流程视觉：CaptureFrame 回退不崩 =================
            Section("H. 流程视觉 CaptureFrame：无真实相机时回退合成帧（流程不崩）");
            try
            {
                var frame = VisionEngine.CaptureFrame(0, out int fw, out int fh);
                Check(frame != null && frame.Length > 0,
                      "CaptureFrame 返回回退帧（" + fw + "×" + fh + "，"
                      + (frame?.Length ?? 0) + " 字节）");
            }
            catch (Exception ex)
            {
                Console.WriteLine("  DIAG  CaptureFrame 异常链：");
                for (var e = ex; e != null; e = e.InnerException)
                    Console.WriteLine("        " + e.GetType().FullName + " : " + e.Message);
                Check(false, "CaptureFrame 抛异常：" + ex.GetType().Name + " / " + ex.Message);
            }

            // 诊断：OpenCvSharp 原生库在本环境是否可加载（与相机无关，属环境能力）
            try
            {
                var m = new OpenCvSharp.Mat(4, 4, OpenCvSharp.MatType.CV_8UC3);
                Check(m.Width == 4, "OpenCvSharp 原生库可加载（诊断项）");
            }
            catch (Exception ex)
            {
                Console.WriteLine("  DIAG  OpenCvSharp 原生库不可用：");
                for (var e = ex; e != null; e = e.InnerException)
                    Console.WriteLine("        " + e.GetType().FullName + " : " + e.Message);
            }

            // ================= I. 工程师页 IO 搜索过滤 =================
            Section("I. 工程师页 IO 搜索：过滤视图只影响本页（不污染 IO 页共用的默认视图）");
            try
            {
                var evm = new EngineerViewModel();

                // 冒烟没有加载工程 → 自己塞几条，顺便验证「集合增删后命中数同步」
                if (evm.Inputs.Count == 0)
                {
                    evm.Inputs.Add(new IoItem { Name = "输入0", CardNo = 0, ModuleNo = 0 });
                    evm.Inputs.Add(new IoItem { Name = "输入1", CardNo = 0, ModuleNo = 0, Value = 1 });
                    evm.Inputs.Add(new IoItem { Name = "急停按钮", CardNo = 1, ModuleNo = 2, Function = "急停" });
                }
                if (evm.Outputs.Count == 0)
                {
                    evm.Outputs.Add(new IoItem { Name = "输出0", CardNo = 0, ModuleNo = 0 });
                    evm.Outputs.Add(new IoItem { Name = "报警灯", CardNo = 0, ModuleNo = 1, Value = 1 });
                }

                int totalIn = evm.Inputs.Count;
                int totalOut = evm.Outputs.Count;
                Console.WriteLine("  INFO  输入 " + totalIn + " 项 / 输出 " + totalOut + " 项");

                Check(evm.InputsView != null && evm.OutputsView != null,
                      "暴露了过滤视图 InputsView / OutputsView");
                Check(evm.InputsView.Cast<object>().Count() == totalIn,
                      "关键字为空 → 输入视图等于全量（" + totalIn + "）");

                string probe = evm.Inputs[0].Name;
                evm.InputSearch = probe;
                int hit = evm.InputsView.Cast<object>().Count();
                Check(hit >= 1 && hit <= totalIn,
                      "搜索「" + probe + "」→ 命中 " + hit + " / " + totalIn);
                Check(evm.InputMatchInfo == hit + " / " + totalIn,
                      "命中信息文案 = 命中 / 总数（" + evm.InputMatchInfo + "）");

                // 关键：IO 页（IoViewModel）用的是同一批集合的**默认视图**，必须不受本页过滤影响
                var defView = System.Windows.Data.CollectionViewSource.GetDefaultView(evm.Inputs);
                int defCount = defView.Cast<object>().Count();
                Check(defCount == totalIn,
                      "IO 页共用的默认视图未被本页过滤污染（仍 " + defCount + " 项）");

                evm.InputSearch = "绝不可能匹配的关键字xyzzy";
                Check(evm.InputsView.Cast<object>().Count() == 0, "搜不到 → 0 项");
                Check(defView.Cast<object>().Count() == totalIn, "此时默认视图仍未被污染");

                evm.InputSearch = "";
                Check(evm.InputsView.Cast<object>().Count() == totalIn, "清空关键字 → 恢复全量");

                // 输出侧同样可用
                evm.OutputSearch = "报警";
                int outHit = evm.OutputsView.Cast<object>().Count();
                Check(outHit >= 1 && outHit <= totalOut,
                      "输出侧搜索「报警」→ 命中 " + outHit + " / " + totalOut);

                // ---- 气缸列表搜索 ----
                evm.Cylinders.Add(new CylinderRuntime(
                    new CylinderItem { Name = "夹爪气缸", Type = "双作用", OutPoint = "输出3" }, false));
                evm.Cylinders.Add(new CylinderRuntime(
                    new CylinderItem { Name = "顶升气缸", Type = "单作用", OutPoint = "输出4" }, true));
                int totalCyl = evm.Cylinders.Count;

                Check(evm.CylindersView != null, "暴露了气缸过滤视图 CylindersView");
                Check(evm.CylindersView.Cast<object>().Count() == totalCyl,
                      "关键字为空 → 气缸视图等于全量（" + totalCyl + "）");

                evm.CylinderSearch = "夹爪";
                Check(evm.CylindersView.Cast<object>().Count() == 1,
                      "气缸搜索「夹爪」→ 命中 1 / " + totalCyl);
                Check(evm.CylinderMatchInfo == "1 / " + totalCyl,
                      "气缸命中信息文案 = 命中 / 总数（" + evm.CylinderMatchInfo + "）");

                evm.CylinderSearch = "输出4";
                Check(evm.CylindersView.Cast<object>().Count() == 1,
                      "气缸按输出点也能搜（「输出4」→ 1）");
                evm.CylinderSearch = "单作用";
                Check(evm.CylindersView.Cast<object>().Count() == 1,
                      "气缸按类型也能搜（「单作用」→ 1）");
                evm.CylinderSearch = "绝不可能匹配xyzzy";
                Check(evm.CylindersView.Cast<object>().Count() == 0, "气缸搜不到 → 0 项");

                // ---- 三个「取消搜索」按钮 ----
                evm.InputSearch = probe; evm.OutputSearch = "报警"; evm.CylinderSearch = "夹爪";
                evm.ClearInputSearchCommand.Execute(null);
                evm.ClearOutputSearchCommand.Execute(null);
                evm.ClearCylinderSearchCommand.Execute(null);
                Check(evm.InputSearch.Length == 0 && evm.OutputSearch.Length == 0 && evm.CylinderSearch.Length == 0,
                      "三个「取消」命令都把关键字清空了");
                Check(evm.InputsView.Cast<object>().Count() == totalIn
                      && evm.OutputsView.Cast<object>().Count() == totalOut
                      && evm.CylindersView.Cast<object>().Count() == totalCyl,
                      "取消后三个视图都恢复全量（" + totalIn + " / " + totalOut + " / " + totalCyl + "）");
            }
            catch (Exception ex)
            {
                Check(false, "IO / 气缸 搜索过滤抛异常：" + ex.GetType().Name + " / " + ex.Message);
            }

            // ================= J. 工程师页 IO 行「真实几何」+ 离屏渲染留证 =================
            // 需求是「行高缩小、宽度加大」——这是视觉指标，所以不看 XAML 文本，
            // 直接离屏 Measure/Arrange 后读控件 ActualWidth/ActualHeight。
            Section("J. 工程师页真实几何（离屏布局）：行高缩小 / 行宽拉伸 / 搜索+取消 / 轴控制在点位表正上方");
            try
            {
                var ep = new NoCodeMotion.Views.EngineerPage();
                var evm2 = (EngineerViewModel)ep.DataContext!;
                for (int i = 0; i < 14; i++)
                    evm2.Inputs.Add(new IoItem { Name = "输入" + i, CardNo = i / 8, ModuleNo = i % 8, Value = i % 2 });
                for (int i = 0; i < 14; i++)
                    evm2.Outputs.Add(new IoItem { Name = "输出" + i, CardNo = i / 8, ModuleNo = i % 8, Value = i % 2 });
                for (int i = 0; i < 8; i++)
                    evm2.Cylinders.Add(new CylinderRuntime(
                        new CylinderItem { Name = "气缸" + i, Type = i % 2 == 0 ? "双作用" : "单作用", OutPoint = "输出" + i },
                        i % 2 == 0));

                ep.Width = 1240; ep.Height = 780;
                ep.Measure(new Size(1240, 780));
                ep.Arrange(new Rect(0, 0, 1240, 780));
                ep.UpdateLayout();

                var lists = Descendants(ep).OfType<System.Windows.Controls.ListBox>().ToList();
                Check(lists.Count >= 3, "页面上有 3 个列表（输入 / 输出 / 气缸）（实际 " + lists.Count + "）");

                var inList = lists.FirstOrDefault(l => ReferenceEquals(l.ItemsSource, evm2.InputsView));
                Check(inList != null, "输入表的 ItemsSource 就是 InputsView（过滤视图已接上 UI）");

                if (inList != null)
                {
                    var item = Descendants(inList).OfType<System.Windows.Controls.ListBoxItem>().FirstOrDefault();
                    if (item is null)
                    {
                        Console.WriteLine("  INFO  虚拟化容器未在离屏布局中生成，跳过行几何断言");
                    }
                    else
                    {
                        var row = Descendants(item).OfType<System.Windows.Controls.Border>().FirstOrDefault();
                        if (row != null)
                        {
                            Console.WriteLine("  INFO  输入行实际尺寸 " + row.ActualWidth.ToString("0.0")
                                              + " × " + row.ActualHeight.ToString("0.0")
                                              + "（列表宽 " + inList.ActualWidth.ToString("0.0") + "）");
                            Check(row.ActualHeight > 0 && row.ActualHeight < 40,
                                  "行高缩小：单行 < 40px（实际 " + row.ActualHeight.ToString("0.0") + "px）");
                            Check(row.ActualWidth >= inList.ActualWidth * 0.85,
                                  "行宽拉伸到列宽（行 " + row.ActualWidth.ToString("0.0")
                                  + " / 列表 " + inList.ActualWidth.ToString("0.0") + "）");

                            // 「宽度加大」的真意 = 行宽跟着列宽走（旧写法固定 210，换窗口宽度也不会变）。
                            // 不再拿 210 当阈值：列宽比例是布局口味（当前 * / 2*，左列本来就窄），
                            // 用「换个更宽的窗口，行也跟着变宽」来证明它确实在拉伸。
                            // ★ 必须在 resize 之前把宽度抄下来：row 是活对象，重新布局后
                            //   row.ActualWidth 会被就地更新，事后再读两次拿到的是同一个值。
                            double rowW1 = row.ActualWidth;
                            ep.Width = 1600;
                            ep.Measure(new Size(1600, 780));
                            ep.Arrange(new Rect(0, 0, 1600, 780));
                            ep.UpdateLayout();
                            var it2 = Descendants(inList).OfType<System.Windows.Controls.ListBoxItem>().FirstOrDefault();
                            var row2 = it2 is null
                                ? null
                                : Descendants(it2).OfType<System.Windows.Controls.Border>().FirstOrDefault();
                            Check(row2 != null && row2.ActualWidth > rowW1 + 20,
                                  "行宽随列宽拉伸：窗口 1240 -> " + rowW1.ToString("0.0")
                                  + "px，1600 -> " + (row2?.ActualWidth ?? 0).ToString("0.0")
                                  + "px（旧写法固定 210 不会随列宽变）");
                            // 还原窗口宽度，后面还要按 1240 做卡片位置断言
                            ep.Width = 1240;
                            ep.Measure(new Size(1240, 780));
                            ep.Arrange(new Rect(0, 0, 1240, 780));
                            ep.UpdateLayout();
                        }
                        else
                        {
                            Console.WriteLine("  INFO  未找到行 Border，跳过行几何断言");
                        }
                    }
                }

                // 气缸列表：同一套判据（行高缩小 / 行宽加大 / 绑定过滤视图）
                var cylList = lists.FirstOrDefault(l => ReferenceEquals(l.ItemsSource, evm2.CylindersView));
                Check(cylList != null, "气缸表的 ItemsSource 就是 CylindersView（过滤视图已接上 UI）");
                if (cylList != null)
                {
                    var citem = Descendants(cylList).OfType<System.Windows.Controls.ListBoxItem>().FirstOrDefault();
                    var crow = citem is null
                        ? null
                        : Descendants(citem).OfType<System.Windows.Controls.Border>().FirstOrDefault();
                    if (crow is null)
                    {
                        Console.WriteLine("  INFO  气缸虚拟化容器未生成，跳过气缸行几何断言");
                    }
                    else
                    {
                        Console.WriteLine("  INFO  气缸行实际尺寸 " + crow.ActualWidth.ToString("0.0")
                                          + " × " + crow.ActualHeight.ToString("0.0")
                                          + "（列表宽 " + cylList.ActualWidth.ToString("0.0") + "）");
                        Check(crow.ActualHeight > 0 && crow.ActualHeight < 40,
                              "气缸行高缩小：单行 < 40px（实际 " + crow.ActualHeight.ToString("0.0") + "px）");
                        Check(crow.ActualWidth > 224 && crow.ActualWidth >= cylList.ActualWidth * 0.85,
                              "气缸行宽加大：拉伸到列宽（行 " + crow.ActualWidth.ToString("0.0")
                              + " / 列表 " + cylList.ActualWidth.ToString("0.0")
                              + "，旧写法固定 Width=224）");
                    }
                }

                int boxes = Descendants(ep).OfType<System.Windows.Controls.TextBox>().Count();
                Check(boxes >= 3, "三个列表各有搜索框（页面上 TextBox 数 = " + boxes + "）");

                int clears = Descendants(ep).OfType<System.Windows.Controls.Button>()
                                .Count(b => (b.Content as string) == "取消");
                Check(clears == 3, "三个搜索框右侧各有一个「取消」按钮（实际 " + clears + " 个）");

                // ---- 四张卡片的位置关系：轴控制必须在点位表正上方（同列相邻） ----
                var outList = lists.FirstOrDefault(l => ReferenceEquals(l.ItemsSource, evm2.OutputsView));
                Check(outList != null, "输出表的 ItemsSource 就是 OutputsView");
                var ptGrid = Descendants(ep).OfType<System.Windows.Controls.DataGrid>().FirstOrDefault();
                var axisItems = Descendants(ep).OfType<System.Windows.Controls.ItemsControl>()
                                    .FirstOrDefault(ic => ReferenceEquals(ic.ItemsSource, evm2.AxisStates));

                var ioCard = CardOf(inList);
                var cylCard = CardOf(cylList);
                var axisCard = CardOf(axisItems);
                var ptCard = CardOf(ptGrid);

                Check(ioCard != null && cylCard != null && axisCard != null && ptCard != null,
                      "四张卡片都能定位到（IO / 气缸 / 轴控制 / 点位表）");

                if (ioCard != null && cylCard != null && axisCard != null && ptCard != null)
                {
                    Point P(System.Windows.FrameworkElement el)
                        => el.TransformToAncestor(ep).Transform(new Point(0, 0));

                    var pIo = P(ioCard); var pCyl = P(cylCard); var pAxis = P(axisCard); var pPt = P(ptCard);
                    Console.WriteLine("  INFO  IO 卡   x=" + pIo.X.ToString("0") + " y=" + pIo.Y.ToString("0")
                                      + " w=" + ioCard.ActualWidth.ToString("0"));
                    Console.WriteLine("  INFO  气缸卡 x=" + pCyl.X.ToString("0") + " y=" + pCyl.Y.ToString("0")
                                      + " w=" + cylCard.ActualWidth.ToString("0"));
                    Console.WriteLine("  INFO  轴控卡 x=" + pAxis.X.ToString("0") + " y=" + pAxis.Y.ToString("0")
                                      + " w=" + axisCard.ActualWidth.ToString("0"));
                    Console.WriteLine("  INFO  点位卡 x=" + pPt.X.ToString("0") + " y=" + pPt.Y.ToString("0")
                                      + " w=" + ptCard.ActualWidth.ToString("0"));

                    Check(Math.Abs(pAxis.X - pPt.X) < 1.0 && pAxis.Y < pPt.Y,
                          "轴控制与点位表同列且在其正上方（Δx=" + Math.Abs(pAxis.X - pPt.X).ToString("0.0")
                          + "px，轴控 y=" + pAxis.Y.ToString("0") + " < 点位 y=" + pPt.Y.ToString("0") + "）");
                    Check(pIo.X < pAxis.X && pCyl.X < pAxis.X,
                          "IO 控制与气缸控制在左列（x=" + pIo.X.ToString("0") + " / " + pCyl.X.ToString("0")
                          + " < 轴控 x=" + pAxis.X.ToString("0") + "）");
                    // 两列都是 star 宽度，比例由 XAML 定（当前 * / 2*：右列更宽给点位表）。
                    // 这是布局口味、会随需求变，所以只断言「两列都占到位、合计铺满可用宽度」，
                    // 不锁死谁更宽 —— 锁死会让每次调比例都误报。
                    Check(ioCard.ActualWidth > 0 && axisCard.ActualWidth > 0
                          && Math.Abs(ioCard.ActualWidth + axisCard.ActualWidth + 16 - 1208) < 24,
                          "两列都占到位且合计铺满可用宽度（IO " + ioCard.ActualWidth.ToString("0")
                          + " + 轴控 " + axisCard.ActualWidth.ToString("0") + " ≈ 1208）");
                    Check(Math.Abs(pIo.Y - pAxis.Y) < 1.0 && pCyl.Y > pIo.Y,
                          "左右两列首行对齐，气缸卡在 IO 卡下方");
                }

                // 离屏渲染留证（RenderTargetBitmap 真渲染可视化树，不依赖窗口/桌面）
                var rtb = new RenderTargetBitmap(1240, 780, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(ep);
                // 输出到仓库内稳定路径 <repo>/.smoke/out/（.smoke/bin 会被清理，out 被 .gitignore 覆盖）
                string dir = RepoSmokeOut() ?? Path.Combine(AppContext.BaseDirectory, "out");
                Directory.CreateDirectory(dir);
                string png = Path.Combine(dir, "engineer_io.png");
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtb));
                using (var fs = File.Create(png)) enc.Save(fs);
                var fi = new FileInfo(png);
                Check(fi.Exists && fi.Length > 2000,
                      "已离屏渲染工程师页为 PNG（" + fi.Length + " 字节）：" + png);
            }
            catch (Exception ex)
            {
                Check(false, "离屏布局/渲染抛异常：" + ex.GetType().Name + " / " + ex.Message);
            }

            // ================= K. 名称库分组（下拉框二级菜单） =================
            // 需求：轴 / IO / 气缸 / 变量 的名称超过 20 个时，下拉框按名称里 '-' 前缀分类。
            // 分组挂在名称库的**默认视图**上（Catalog.*Names 是静态实例，所有下拉共用同一实例），
            // 所以这里直接读默认视图的 GroupDescriptions 验证，再用真实 ComboBox 验证分组头挂上了。
            // ★ 覆盖是**全局**的：NameGroupHeader 注册了 ComboBox 的类处理器，
            //   页面里「显式 Style / 内联 Style / 完全不写 Style」三种下拉都会自动带上分组头。
            Section("K. 名称库分组：超过 20 个按 '-' 前缀分类（下拉框二级菜单）");
            try
            {
                var axisView = System.Windows.Data.CollectionViewSource.GetDefaultView(Catalog.AxisNames);
                var saved = Catalog.AxisNames.ToList();

                var many = new List<string>();
                for (int i = 0; i < 15; i++) many.Add("下料-x" + i);
                for (int i = 0; i < 6; i++) many.Add("上料-y" + i);

                // (1) 21 个 -> 超过阈值，应分组
                Catalog.SetAxis(many);
                Check(Catalog.AxisNames.Count == 21, "名称库灌入 21 个名称（实际 " + Catalog.AxisNames.Count + "）");
                Check(axisView.GroupDescriptions.Count == 1,
                      "超过 20 个 -> 默认视图挂上分组（GroupDescription 数 = " + axisView.GroupDescriptions.Count + "）");

                var g1 = axisView.Groups?.Cast<System.Windows.Data.CollectionViewGroup>().ToList()
                         ?? new List<System.Windows.Data.CollectionViewGroup>();
                Check(g1.Count == 2, "分成 2 个二级分类（实际 " + g1.Count + "）");
                Check(g1.Count == 2 && (string?)g1[0].Name == "下料" && (string?)g1[1].Name == "上料",
                      "分类名 = '-' 前的部分，按首次出现排序（实际 " + string.Join(" / ", g1.Select(g => g.Name)) + "）");
                Check(g1.Count == 2 && g1[0].ItemCount == 15 && g1[1].ItemCount == 6,
                      "每个分类下的数量正确（15 / 6）");

                // 条目类型必须仍是 string：否则 SelectedItem 到 string 属性的双向绑定会全部失配
                var flat = axisView.Cast<object>().ToList();
                Check(flat.Count == 21 && flat.All(o => o is string),
                      "分组后条目仍然是 string（SelectedItem 双向绑定安全）");

                // AllNames = 轴/IO/气缸/变量的并集（流程页「名称」列归不了类时的兜底候选），同样要分组
                var allView = System.Windows.Data.CollectionViewSource.GetDefaultView(Catalog.AllNames);
                Check(Catalog.AllNames.Count > 20 && allView.GroupDescriptions.Count == 1,
                      "并集 AllNames 也分组（" + Catalog.AllNames.Count + " 个名称 -> 分组描述 "
                      + allView.GroupDescriptions.Count + " 个）");

                // 真实 ComboBox：CellComboStyle 必须经附加属性把分组头样式挂上去
                var combo = new System.Windows.Controls.ComboBox
                {
                    Style = Application.Current?.TryFindResource("CellComboStyle") as Style,
                    ItemsSource = Catalog.AxisNames,
                };
                combo.Measure(new Size(220, 24));
                combo.Arrange(new Rect(0, 0, 220, 24));
                Check(combo.GroupStyle.Count == 1,
                      "CellComboStyle 把分组头挂到了 ComboBox（GroupStyle.Count = " + combo.GroupStyle.Count + "）");
                Check(combo.GroupStyle.Count == 1 && combo.GroupStyle[0].HeaderTemplate != null,
                      "分组头模板已从 AppStyles.xaml 解析到（NameGroupHeaderTemplate）");
                combo.SelectedItem = "下料-x3";
                Check(combo.SelectedItem is string cs && cs == "下料-x3",
                      "分组后 SelectedItem 仍是 string，可正常选中（" + combo.SelectedItem + "）");

                // ★ 全局兜底：**完全不设 Style** 的下拉也必须自动带上分组头。
                //   页面里三种写法混用（显式 Style / 内联 Style / 不写 Style），逐个 Style 去开必漏，
                //   所以 NameGroupHeader 静态构造里注册了 ComboBox 的类处理器。
                var bare = new System.Windows.Controls.ComboBox { ItemsSource = Catalog.AxisNames };
                Check(bare.GroupStyle.Count == 0, "未加载时裸 ComboBox 还没有分组头（Count = 0）");
                bare.RaiseEvent(new RoutedEventArgs(System.Windows.FrameworkElement.LoadedEvent));
                Check(bare.GroupStyle.Count == 1 && bare.GroupStyle[0].HeaderTemplate != null,
                      "加载即自动挂上分组头（裸 ComboBox，不依赖任何 Style）");
                bare.RaiseEvent(new RoutedEventArgs(System.Windows.FrameworkElement.LoadedEvent));
                Check(bare.GroupStyle.Count == 1, "重复加载不会重复挂（仍为 1）");

                // 未分组的下拉：模板照样挂上，但视图里没有分组描述 -> 不可能生成 GroupItem，所以毫无副作用
                var plainItems = new System.Collections.ObjectModel.ObservableCollection<string> { "甲", "乙" };
                var plain = new System.Windows.Controls.ComboBox { ItemsSource = plainItems };
                plain.RaiseEvent(new RoutedEventArgs(System.Windows.FrameworkElement.LoadedEvent));
                Check(plain.GroupStyle.Count == 1
                      && System.Windows.Data.CollectionViewSource.GetDefaultView(plainItems)
                             .GroupDescriptions.Count == 0,
                      "未分组的下拉也挂了模板，但视图无分组描述 -> 不会渲染分组头（无副作用）");

                // (2) 恰好 20 个 -> 未超过阈值，不分组
                Catalog.SetAxis(many.Take(20).ToList());
                Check(axisView.GroupDescriptions.Count == 0,
                      "恰好 20 个（未超过）-> 不分组（实际 " + axisView.GroupDescriptions.Count + "）");

                // (3) 21 个但没有 '-' -> 截不出分类，也不分组（否则退化成一个「其他」大组，没意义）
                Catalog.SetAxis(Enumerable.Range(0, 21).Select(i => "轴" + i).ToList());
                Check(axisView.GroupDescriptions.Count == 0,
                      "21 个但名称里都没有 '-' -> 不分组（避免只有一个「其他」组）");

                // (4) 混合：20 个无 '-' + 1 个有 '-' -> 分组，截不出的归入「其他」
                var mixed = Enumerable.Range(0, 20).Select(i => "轴" + i).ToList();
                mixed.Add("下料-x");
                Catalog.SetAxis(mixed);
                var g4 = axisView.Groups?.Cast<System.Windows.Data.CollectionViewGroup>().ToList()
                         ?? new List<System.Windows.Data.CollectionViewGroup>();
                Check(axisView.GroupDescriptions.Count == 1 && g4.Any(g => (string?)g.Name == "其他"),
                      "混合时截不出前缀的归入「其他」（实际分类："
                      + string.Join(" / ", g4.Select(g => g.Name)) + "）");

                // 离屏渲染留证。下拉的 Popup 不在可视化树里，所以分两步证明「二级菜单真长出来了」：
                // (a) 直接实例化分组头模板，验证里面的 {Binding Name}/{Binding ItemCount} 生效；
                // (b) 真实 ListBox 里必须出现 GroupItem（= 分组头 + 组内条目）。
                Catalog.SetAxis(many);
                var tpl = Application.Current?.TryFindResource("NameGroupHeaderTemplate") as DataTemplate;
                var gAll = axisView.Groups?.Cast<System.Windows.Data.CollectionViewGroup>().ToList()
                           ?? new List<System.Windows.Data.CollectionViewGroup>();

                var panel = new System.Windows.Controls.StackPanel { Width = 220 };
                foreach (var grp in gAll)
                {
                    panel.Children.Add(new System.Windows.Controls.ContentPresenter
                    {
                        Content = grp,
                        ContentTemplate = tpl,
                    });
                    // CollectionViewGroup 不实现 IEnumerable，条目要走 .Items
                    foreach (var it in grp.Items.Take(3))
                        panel.Children.Add(new System.Windows.Controls.TextBlock
                        {
                            Text = "      " + it,
                            FontSize = 12,
                            Margin = new Thickness(10, 3, 0, 3),
                        });
                }
                panel.Measure(new Size(220, 600));
                panel.Arrange(new Rect(0, 0, 220, Math.Max(1, panel.DesiredSize.Height)));
                panel.UpdateLayout();
                var texts = Descendants(panel).OfType<System.Windows.Controls.TextBlock>()
                                 .Select(x => x.Text).ToList();
                Check(texts.Contains("下料") && texts.Contains("(15)")
                      && texts.Contains("上料") && texts.Contains("(6)"),
                      "分组头模板渲染出「分类名 + 数量」：" + string.Join(" | ", texts));

                var lb = new System.Windows.Controls.ListBox
                {
                    ItemsSource = Catalog.AxisNames,
                    Width = 240,
                    Height = 420,
                };
                System.Windows.Controls.VirtualizingPanel.SetIsVirtualizing(lb, false);   // 全量实体化，断言才确定
                lb.GroupStyle.Add(new System.Windows.Controls.GroupStyle { HeaderTemplate = tpl });
                lb.ApplyTemplate();
                lb.Measure(new Size(240, 420));
                lb.Arrange(new Rect(0, 0, 240, 420));
                lb.UpdateLayout();
                if (VisualTreeHelper.GetChildrenCount(lb) == 0)
                {
                    // 不在窗口里的裸 ListBox 取不到主题模板 —— 自己给个最小模板，
                    // 有 ItemsPresenter 才会按分组生成 GroupItem。
                    var f = new FrameworkElementFactory(typeof(System.Windows.Controls.ScrollViewer));
                    f.AppendChild(new FrameworkElementFactory(typeof(System.Windows.Controls.ItemsPresenter)));
                    lb.Template = new System.Windows.Controls.ControlTemplate(
                        typeof(System.Windows.Controls.ListBox)) { VisualTree = f };
                    lb.Measure(new Size(240, 420));
                    lb.Arrange(new Rect(0, 0, 240, 420));
                    lb.UpdateLayout();
                }
                var gis = Descendants(lb).OfType<System.Windows.Controls.GroupItem>().ToList();
                Check(gis.Count == 2, "ListBox 里真的渲染出 2 个分组头（实际 " + gis.Count + "）");

                // ★ 复现并钉死「分组头出来了、组内条目却没渲染」这个坑（用户截图即此现象）：
                //   CellComboStyle 的下拉宿主原本是 <ScrollViewer><StackPanel IsItemsHost="True"/></ScrollViewer>。
                //   裸面板宿主只实体化最外层的 GroupItem（= 分组标题），组内条目一个都不生成 ——
                //   下拉里就只剩「轴 (2)」「其他 (30)」两行标题，真实名称点不到。
                //   换成 ItemsPresenter 宿主后组内条目才正常生成。两种宿主各测一次，把结论钉住。
                int ProbeHost(bool useItemsPresenter, string tag)
                {
                    var itemF = new FrameworkElementFactory(typeof(System.Windows.Controls.Border));
                    itemF.SetValue(System.Windows.FrameworkElement.TagProperty, "probeItem");
                    var itemTpl = new DataTemplate { VisualTree = itemF };
                    var ic = new System.Windows.Controls.ItemsControl
                    { ItemsSource = Catalog.AxisNames, ItemTemplate = itemTpl };
                    ic.GroupStyle.Add(new System.Windows.Controls.GroupStyle { HeaderTemplate = tpl });

                    var sv = new FrameworkElementFactory(typeof(System.Windows.Controls.ScrollViewer));
                    if (useItemsPresenter)
                    {
                        sv.AppendChild(new FrameworkElementFactory(typeof(System.Windows.Controls.ItemsPresenter)));
                    }
                    else
                    {
                        var host = new FrameworkElementFactory(typeof(System.Windows.Controls.StackPanel));
                        host.SetValue(System.Windows.Controls.StackPanel.IsItemsHostProperty, true);
                        sv.AppendChild(host);
                    }
                    ic.Template = new System.Windows.Controls.ControlTemplate(typeof(System.Windows.Controls.ItemsControl))
                    { VisualTree = sv };

                    ic.Width = 240; ic.Height = 400;
                    ic.Measure(new Size(240, 400));
                    ic.Arrange(new Rect(0, 0, 240, 400));
                    ic.UpdateLayout();

                    int groups = Descendants(ic).OfType<System.Windows.Controls.GroupItem>().Count();
                    int items = Descendants(ic).OfType<System.Windows.Controls.Border>()
                                        .Count(b => "probeItem".Equals(b.Tag));
                    Console.WriteLine("      宿主[" + tag + "] 分组头=" + groups + " 组内条目=" + items);
                    Check(groups == 2, "两种宿主都渲染出 2 个分组头（" + tag + "）");
                    return items;
                }
                int bareItems = ProbeHost(false, "裸 StackPanel IsItemsHost（旧写法）");
                int presenterItems = ProbeHost(true, "ItemsPresenter（现在的写法）");
                Check(bareItems < Catalog.AxisNames.Count / 2,
                      "旧写法：裸面板宿主基本不实体化组内条目（" + bareItems + " / " + Catalog.AxisNames.Count
                      + "）—— 这就是「只有分类标题、点不到真实名称」的原因");
                Check(presenterItems == Catalog.AxisNames.Count,
                      "新写法：ItemsPresenter 宿主实体化全部 " + Catalog.AxisNames.Count
                      + " 个组内条目（实际 " + presenterItems + "）");

                // 上面是「机制」证明；这条证明**编进程序集里的真实 CellComboStyle** 宿主也已经是 ItemsPresenter
                //（源码改了但没重编 / 跑了旧 dll，都会在这里被抓到）。
                var realCombo = new System.Windows.Controls.ComboBox
                {
                    Style = Application.Current?.TryFindResource("CellComboStyle") as Style,
                    ItemsSource = Catalog.AxisNames,
                };
                realCombo.ApplyTemplate();
                var pop = realCombo.Template.FindName("PART_Popup", realCombo)
                          as System.Windows.Controls.Primitives.Popup;
                var popChild = pop?.Child as System.Windows.Controls.Border;
                Check(popChild is not null, "真实 CellComboStyle 模板里取得到 PART_Popup 的 Child");
                // 注意：Popup 没打开时它内部的可视化树不会实例化，Descendants 走不到 ——
                // 所以按**逻辑属性**查（Border.Child -> ScrollViewer.Content），离屏也可靠。
                var dropScroll = popChild?.Child as System.Windows.Controls.ScrollViewer;
                Check(dropScroll?.Content is System.Windows.Controls.ItemsPresenter,
                      "真实 CellComboStyle 下拉宿主已是 ItemsPresenter（程序集里的 XAML 确实更新了）");

                // ================= 级联二级菜单：一级分类行 + 右侧飞出 =================
                // 需求：下拉一级只列分类（「下料 (15) ›」），悬停/点击 → **右侧飞出**该分类下的真实名称。
                var container = Application.Current?.TryFindResource("NameGroupCascadeContainerStyle") as Style;
                Check(container is not null, "AppStyles 里找得到一级分类行样式 NameGroupCascadeContainerStyle");
                Check(container is not null && container.TargetType == typeof(System.Windows.Controls.GroupItem),
                      "一级分类行样式作用于 GroupItem");
                Check(combo.GroupStyle.Count == 1 && combo.GroupStyle[0].ContainerStyle == container,
                      "CellComboStyle 的分组样式同时挂上了一级分类行样式");

                // 用**真实的一级分类行样式**搭一个与「外层下拉」同构的列表：ScrollViewer > ItemsPresenter
                var catHost = new FrameworkElementFactory(typeof(System.Windows.Controls.ScrollViewer));
                catHost.AppendChild(new FrameworkElementFactory(typeof(System.Windows.Controls.ItemsPresenter)));
                var catIc = new System.Windows.Controls.ItemsControl
                {
                    ItemsSource = Catalog.AxisNames,
                    Background = System.Windows.Media.Brushes.White,
                    Width = 200,
                };
                catIc.Template = new System.Windows.Controls.ControlTemplate(typeof(System.Windows.Controls.ItemsControl))
                { VisualTree = catHost };
                var gsCat = new System.Windows.Controls.GroupStyle { HeaderTemplate = tpl };
                if (container is not null) gsCat.ContainerStyle = container;
                catIc.GroupStyle.Add(gsCat);
                catIc.Measure(new Size(200, 600));
                catIc.Arrange(new Rect(0, 0, 200, Math.Max(1, catIc.DesiredSize.Height)));
                catIc.UpdateLayout();

                var cats = Descendants(catIc).OfType<System.Windows.Controls.GroupItem>().ToList();
                Check(cats.Count == 2, "一级面板里渲染出 2 个分类行（实际 " + cats.Count + "）");
                var rowToggles = Descendants(catIc)
                    .OfType<System.Windows.Controls.Primitives.ToggleButton>().ToList();
                Check(rowToggles.Count == 2,
                      "一级行是 ToggleButton（悬停/点击的载体，实际 " + rowToggles.Count + "）");
                var rowTexts = Descendants(catIc).OfType<System.Windows.Controls.TextBlock>()
                                      .Select(x => x.Text).ToList();
                Check(rowTexts.Contains("下料") && rowTexts.Contains("(15)")
                      && rowTexts.Contains("上料") && rowTexts.Contains("(6)"),
                      "一级行显示「分类名 + 数量」：" + string.Join(" | ", rowTexts));
                // ★ 只在分类行内部数箭头：整棵树里数 Path 会被 ScrollViewer 自带滚动条的箭头污染
                int arrowCount = rowToggles
                    .Sum(t => Descendants(t).OfType<System.Windows.Shapes.Path>().Count());
                Check(arrowCount == 2,
                      "每个分类行右侧都有「>」箭头（提示二级菜单在右边，实际 " + arrowCount + "）");
                Check(!rowTexts.Any(x => x is not null && x.StartsWith("下料-")),
                      "一级面板只列分类，不再平铺真实名称");

                // 二级菜单：每个一级行挂一个 Popup，里面才是该分类的真实名称
                var flyouts = cats
                    .Select(c => c.Template?.FindName("flyout", c) as System.Windows.Controls.Primitives.Popup)
                    .Where(p => p is not null).Select(p => p!).ToList();
                Check(flyouts.Count == 2, "每个分类行都挂着二级菜单 Popup（实际 " + flyouts.Count + "）");
                Check(flyouts.Count == 2 && flyouts.All(p =>
                          p.Placement == System.Windows.Controls.Primitives.PlacementMode.Right),
                      "二级菜单从**右侧**飞出（Placement=Right）");
                // Popup 关着时内部可视化树不实例化 → 走逻辑属性查
                var flyScrolls = flyouts
                    .Select(p => (p.Child as System.Windows.Controls.Border)?.Child
                                  as System.Windows.Controls.ScrollViewer).ToList();
                Check(flyScrolls.Count == 2 && flyScrolls.All(s => s is not null),
                      "二级菜单内容是一张卡片（Border > ScrollViewer）");
                Check(flyScrolls.All(s => s?.Content is System.Windows.Controls.ItemsPresenter),
                      "二级菜单里是 ItemsPresenter（渲染的正是该分类下的名称）");
                Check(flyouts.All(p => p.StaysOpen),
                      "二级菜单用 StaysOpen 自行管理开关（悬停进出时不会闪）");

                // 二级菜单里到底有没有名称？Popup 关着，就把它的 Child 单独量一遍再渲染。
                string dir = RepoSmokeOut() ?? Path.Combine(AppContext.BaseDirectory, "out");
                Directory.CreateDirectory(dir);
                string Shot(System.Windows.FrameworkElement fe, string file)
                {
                    int w = Math.Max(1, (int)Math.Ceiling(fe.ActualWidth));
                    int hh = Math.Max(1, (int)Math.Ceiling(fe.ActualHeight));
                    var rtb = new RenderTargetBitmap(w, hh, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(fe);
                    var enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(rtb));
                    string p = Path.Combine(dir, file);
                    using (var fs = File.Create(p)) enc.Save(fs);
                    return p;
                }

                var fly0 = flyouts.Count > 0 ? flyouts[0].Child as System.Windows.FrameworkElement : null;
                int flyNames = 0;
                if (fly0 is not null)
                {
                    fly0.Measure(new Size(240, 320));
                    fly0.Arrange(new Rect(0, 0, Math.Max(1, fly0.DesiredSize.Width),
                                          Math.Max(1, fly0.DesiredSize.Height)));
                    fly0.UpdateLayout();
                    flyNames = Descendants(fly0).OfType<System.Windows.Controls.TextBlock>()
                                     .Count(x => x.Text is not null && x.Text.StartsWith("下料-"));
                }
                Console.WriteLine("      二级菜单里渲染出的名称数 = " + flyNames);
                Check(flyNames == 15,
                      "二级菜单里是该分类的全部真实名称（下料 15 个，实际 " + flyNames + "）");

                string pngCat = Shot(catIc, "name_cascade.png");
                var fiCat = new FileInfo(pngCat);
                Check(fiCat.Exists && fiCat.Length > 800,
                      "已离屏渲染「一级分类行」PNG（" + fiCat.Length + " 字节）：" + pngCat);
                if (fly0 is not null)
                {
                    string pngFly = Shot(fly0, "name_cascade_flyout.png");
                    var fiFly = new FileInfo(pngFly);
                    Check(fiFly.Exists && fiFly.Length > 800,
                          "已离屏渲染「右侧二级菜单」PNG（" + fiFly.Length + " 字节）：" + pngFly);
                }

                Catalog.SetAxis(saved);   // 复原，避免影响其它断言
            }
            catch (Exception ex)
            {
                Check(false, "名称分组验证抛异常：" + ex.GetType().Name + " / " + ex.Message);
            }

            // ================= L. 新建工程模板：示例名称「工位-对象」扩展 =================
            // 需求：新建工程弹窗里的示例模板要「轴 / IO / 气缸 / 变量尽量多」且都用「-」命名，
            // 好让名称下拉框的级联二级菜单一建工程就能看到。
            // 这里真跑一遍 ProjectTemplate.Build()，再喂给 Catalog.SyncAllFromData，
            // 然后读各名称库**默认视图**的 GroupDescriptions —— 与真实下拉框看到的是同一份视图。
            Section("L. 新建工程模板：示例名称「工位-对象」扩展（建完即可见级联下拉）");
            try
            {
                var tplAll = ProjectTemplateCatalog.All;
                Check(tplAll.Count >= 20, "模板目录共 " + tplAll.Count + " 个模板");

                var emptyTpl = tplAll.FirstOrDefault(t => t.Id == "empty");
                Check(emptyTpl != null, "存在「空白工程」模板");
                if (emptyTpl != null)
                {
                    var de = emptyTpl.Build();
                    Check(de.Axes.Count == 0 && de.Inputs.Count == 0 && de.Outputs.Count == 0
                          && de.Cylinders.Count == 0 && de.Variables.Count == 0
                          && de.Controllers.Count == 0,
                          "空白工程仍是 0 控制器 / 0 轴 / 0 IO / 0 气缸 / 0 变量（不追加示例名称）");
                }

                // 挑一个「语义上对象很少」的模板来验证扩展确实起了作用
                var one = tplAll.First(t => t.Id == "single-axis");
                var d1 = one.Build();
                int varCount = d1.Variables.SelectMany(v => v.Names()).Count();
                Console.WriteLine("  单轴点动模板 Build() 后：控制器 " + d1.Controllers.Count
                                  + " / 轴 " + d1.Axes.Count
                                  + " / 入 " + d1.Inputs.Count + " / 出 " + d1.Outputs.Count
                                  + " / 气缸 " + d1.Cylinders.Count + " / 变量 " + varCount);
                Check(d1.Axes.Count > Catalog.GroupThreshold, "轴数量超过分组阈值 20：" + d1.Axes.Count);
                Check(d1.Inputs.Count > Catalog.GroupThreshold, "输入 IO 数量超过分组阈值 20：" + d1.Inputs.Count);
                Check(d1.Outputs.Count > Catalog.GroupThreshold, "输出 IO 数量超过分组阈值 20：" + d1.Outputs.Count);
                Check(d1.Cylinders.Count > Catalog.GroupThreshold, "气缸数量超过分组阈值 20：" + d1.Cylinders.Count);
                Check(varCount > Catalog.GroupThreshold, "变量数量超过分组阈值 20：" + varCount);

                // 新增的对象必须都是「工位-对象」写法，且能截出前缀（否则分组退化成「其他」一桶）
                var demoAxes = d1.Axes.Skip(1).Select(a => a.Name).ToList();   // 第 1 根是模板自带的 X
                Check(demoAxes.Count > 20 && demoAxes.All(n => n.Contains('-')),
                      "扩展出来的轴名都带「-」：" + string.Join("、", demoAxes.Take(3)) + " …");
                var pref = demoAxes.Select(n => NamePrefixGroupDescription.PrefixOf(n))
                                   .Where(p => p != null).Distinct().ToList();
                Check(pref.Count >= 3, "轴名能截出多个分组前缀：" + string.Join("、", pref));

                // 扩展轴卡必须是「雷赛」：非雷赛控制器会让 CanServeProject() 切到卡族层，
                // 把所有模板的默认硬件通道整体改掉（不报错，只是行为变了）。
                var ext = d1.Controllers.FirstOrDefault(c => c.Name == "扩展轴卡");
                Check(ext != null && ext.Vendor == "雷赛", "扩展轴卡存在且厂商是「雷赛」");
                if (ext != null)
                {
                    var fam = NoCodeMotion.Services.Hardware.Cards.CardFamilyCatalog
                                  .Resolve(ext.Vendor, ext.CardType, ext.BusType);
                    Check(fam == null || fam.Vendor == "雷赛",
                          "扩展轴卡解析到的卡族仍是雷赛（CanServeProject 会跳过 → 默认通道不变）");
                    var nos = d1.Axes.Where(a => a.Controller == ext.Name).Select(a => a.AxisNo).ToList();
                    Check(nos.Count == nos.Distinct().Count(),
                          "扩展卡内轴号唯一（" + nos.Count + " 根，不共用物理通道）");
                }

                // 喂给名称库，验证「默认视图」真的挂上了分组（与真实下拉框同一份视图）
                Catalog.SyncAllFromData(d1);
                foreach (var pair in new (string label, System.Collections.ObjectModel.ObservableCollection<string> coll)[]
                         {
                             ("轴", Catalog.AxisNames), ("输入IO", Catalog.InIoNames),
                             ("输出IO", Catalog.OutIoNames), ("气缸", Catalog.CylinderNames),
                             ("变量", Catalog.VariableNames),
                         })
                {
                    var v = System.Windows.Data.CollectionViewSource.GetDefaultView(pair.coll);
                    Check(pair.coll.Count > Catalog.GroupThreshold && v.GroupDescriptions.Count > 0,
                          pair.label + " 名称库（" + pair.coll.Count + " 个）默认视图已分组 → 下拉框呈级联二级菜单");
                }
                Check(Catalog.AllNames.Count > Catalog.GroupThreshold
                      && System.Windows.Data.CollectionViewSource.GetDefaultView(Catalog.AllNames).GroupDescriptions.Count > 0,
                      "并集名称库（" + Catalog.AllNames.Count + " 个）也已分组");

                // ---- 四个名称录入页真的把提示栏放进了可视化树（不是只写在 XAML 源码里） ----
                string outDir = RepoSmokeOut() ?? Path.Combine(AppContext.BaseDirectory, "out");
                Directory.CreateDirectory(outDir);
                string Shot2(System.Windows.FrameworkElement fe, string file)
                {
                    int w = Math.Max(1, (int)Math.Ceiling(fe.ActualWidth));
                    int hh = Math.Max(1, (int)Math.Ceiling(fe.ActualHeight));
                    var rtb = new RenderTargetBitmap(w, hh, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(fe);
                    var enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(rtb));
                    string pp = Path.Combine(outDir, file);
                    using (var fs = File.Create(pp)) enc.Save(fs);
                    return pp;
                }

                // ★ 先把扩展后的数据灌进 ProjectStore，四个页面的列表才非空。
                //   EditorPage 在 Items.Count==0 且宿主页设了 EmptyHint 时会把右侧详情整块 Collapsed，
                //   提示栏自然量不到版面 —— 那是离屏空态的假象，不是布局问题。
                //   （各页 VM 的 Items 就是 ProjectStore.Data 里的那个集合实例，CopyFrom 原地改内容即可。）
                ProjectStore.Data.CopyFrom(d1);

                foreach (var pair in new (string label, System.Windows.FrameworkElement page)[]
                         {
                             ("轴", new NoCodeMotion.Views.AxisPage()),
                             ("IO", new NoCodeMotion.Views.IoPage()),
                             ("气缸", new NoCodeMotion.Views.CylinderPage()),
                             ("变量", new NoCodeMotion.Views.VariablePage()),
                         })
                {
                    var page = pair.page;
                    page.Measure(new Size(1200, 800));
                    page.Arrange(new Rect(0, 0, 1200, 800));
                    page.UpdateLayout();
                    var bar = AllDescendants(page).OfType<NoCodeMotion.Views.PageHintBar>().FirstOrDefault();
                    Check(bar != null
                          && !string.IsNullOrWhiteSpace(bar.OperationText)
                          && !string.IsNullOrWhiteSpace(bar.PrecautionText),
                          pair.label + " 页的可视化树里找得到命名提示栏（操作 / 注意两行都有文字）");
                    if (bar != null)
                    {
                        // 提示栏里两行文字的 Text 是 {Binding …} 绑到 PageHintBar 的依赖属性上的，
                        // 绑没绑上、有没有真的渲染出来，只有把渲染后的 TextBlock 文字读回来才知道。
                        var shown = AllDescendants(bar).OfType<System.Windows.Controls.TextBlock>()
                                        .Select(x => x.Text ?? string.Empty)
                                        .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                        Check(shown.Any(x => x.Contains("工位-对象")) && shown.Any(x => x.Contains("分组前缀")),
                              pair.label + " 页提示栏渲染出的文字里有「工位-对象 / 分组前缀」");
                    }
                    if (bar != null)
                    {
                        // 光「在树里」还不够，得真的占到了版面才算「用户看得见」。
                        Console.WriteLine("      " + pair.label + " 页提示栏在页面里实测："
                                          + Math.Round(bar.ActualWidth) + " x " + Math.Round(bar.ActualHeight));
                        Check(bar.ActualWidth > 200 && bar.ActualHeight > 20,
                              pair.label + " 页提示栏在页面里真的占了版面（"
                              + Math.Round(bar.ActualWidth) + " x " + Math.Round(bar.ActualHeight) + "）");
                    }
                    if (pair.label == "轴")
                    {
                        // ★ 渲染**还挂在父级里**的子元素会画出空白（元素在父级里的偏移把它推出
                        //   位图范围）→ 渲染整页（整页是脱离窗口的独立控件）才拿得到真实画面。
                        string pb = Shot2(page, "page_hint_axis.png");
                        var fb = new FileInfo(pb);
                        Check(fb.Exists && fb.Length > 5000,
                              "已离屏渲染「轴页（含底部命名提示栏）」PNG（" + fb.Length + " 字节）：" + pb);
                    }
                }

                // ---- 新建工程弹窗：命名约定提示真的在弹窗内容里 ----
                try
                {
                    var npd = new NoCodeMotion.Views.NewProjectDialog("示例工程");
                    // 把内容从窗口上摘下来再渲染：挂在窗口里的元素渲染出来会是空白（见上）。
                    var rootFe = npd.Content as System.Windows.FrameworkElement;
                    npd.Content = null;
                    if (rootFe is null)
                    {
                        Check(false, "新建工程弹窗的 Content 不是 FrameworkElement，无法验证提示");
                    }
                    else
                    {
                        rootFe.Measure(new Size(760, 560));
                        rootFe.Arrange(new Rect(0, 0, 760, 560));
                        rootFe.UpdateLayout();
                        var hint = AllDescendants(rootFe).OfType<System.Windows.Controls.TextBlock>()
                                         .FirstOrDefault(tb => tb.Text is not null
                                                               && tb.Text.Contains("工位-对象"));
                        Check(hint != null && hint.Text.Contains("分组前缀"),
                              "新建工程弹窗里渲染出「工位-对象」命名约定提示");
                        string pb = Shot2(rootFe, "new_project_naming_hint.png");
                        var fb = new FileInfo(pb);
                        Check(fb.Exists && fb.Length > 2000,
                              "已离屏渲染「新建工程弹窗」PNG（" + fb.Length + " 字节）：" + pb);
                    }
                }
                catch (Exception ex)
                {
                    Check(false, "新建工程弹窗构造 / 渲染抛异常：" + ex.GetType().Name + " / "
                                 + (ex.InnerException?.Message ?? ex.Message));
                }
            }
            catch (Exception ex)
            {
                Check(false, "模板示例名称扩展验证抛异常：" + ex.GetType().Name + " / " + ex.Message);
            }

            // ===================== 段 M：视觉流程「字符识别」(OCR) =====================
            Section("M  流程视觉：字符识别（Windows.Media.Ocr）");
            try
            {
                // 1) 模型默认值 + 识别区域回显
                var st = new VisualFlowStep { StepType = "字符识别" };
                Check(st.OcrLanguage == "自动" && st.OcrMatchMode == "包含" && !st.OcrIgnoreCase
                      && st.OcrRoiW == 0 && st.OcrRoiH == 0,
                      "字符识别步骤默认参数（语言=自动 / 匹配=包含 / 未框选=整图）");
                Check(st.OcrRoiText.Contains("整图"), "未框选时识别区域回显「整图识别」");

                // 2) 端到端（需要 OpenCvSharp 原生库；dotnet exec 下把输出目录加入 PATH 才可加载）
                bool cvOk;
                try { using var probe = new OpenCvSharp.Mat(4, 4, OpenCvSharp.MatType.CV_8UC3); cvOk = probe.Width == 4; }
                catch { cvOk = false; }

                if (!cvOk)
                {
                    Console.WriteLine("  SKIP  OpenCvSharp 原生库不可用，跳过字符识别端到端"
                                      + "（模型/接线已校验；把输出目录加入 PATH 后可跑完整 OCR）");
                }
                else
                {
                    // 造一张白底黑字图，走「图像采集(文件) → 字符识别(包含 ABC)」
                    string outDir = RepoSmokeOut() ?? Path.GetTempPath();
                    Directory.CreateDirectory(outDir);
                    string imgPath = Path.Combine(outDir, "ocr_sample.png");
                    using (var img = new OpenCvSharp.Mat(120, 460, OpenCvSharp.MatType.CV_8UC4,
                                                         new OpenCvSharp.Scalar(255, 255, 255, 255)))
                    {
                        OpenCvSharp.Cv2.PutText(img, "ABC123", new OpenCvSharp.Point(20, 85),
                            OpenCvSharp.HersheyFonts.HersheySimplex, 2.4, new OpenCvSharp.Scalar(0, 0, 0, 255), 4);
                        OpenCvSharp.Cv2.ImWrite(imgPath, img);
                    }

                    var acq = new VisualFlowStep
                    { Name = "采集", StepType = "图像采集", SourceType = "文件", SavePath = imgPath, Enabled = true };
                    var ocrOk = new VisualFlowStep
                    {
                        Name = "识别", StepType = "字符识别", Enabled = true,
                        OcrLanguage = "英文", OcrMatchMode = "包含", OcrExpectedText = "ABC"
                    };
                    var rep = VisionEngine.Run(new[] { acq, ocrOk });
                    var r = rep.Results.LastOrDefault(x => x.Type == "字符识别");
                    Check(r != null, "字符识别步骤执行并产出结果（不崩）：" + (r?.Summary ?? "无结果"));
                    if (r != null)
                    {
                        Check(!string.IsNullOrEmpty(r.Summary), "字符识别结果有摘要文本");
                        string norm = (r.Text ?? "").Replace(" ", "");
                        if (norm.Length > 0)
                            Check(r.Ok == norm.Contains("ABC"),
                                  "「包含 ABC」判定与识别文本一致（识别到：" + r.Text + "）");
                        else
                            Console.WriteLine("  SKIP  本机 OCR 未识别出文字（可能缺语言包），仅校验步骤跑通");
                    }

                    // 期望文本不可能出现 → 必须判 NG
                    var ocrNg = new VisualFlowStep
                    {
                        Name = "识别2", StepType = "字符识别", Enabled = true,
                        OcrLanguage = "英文", OcrMatchMode = "包含", OcrExpectedText = "ZZZZZZ"
                    };
                    var rep2 = VisionEngine.Run(new[] { acq, ocrNg });
                    var r2 = rep2.Results.LastOrDefault(x => x.Type == "字符识别");
                    Check(r2 != null && !r2.Ok, "期望文本不符时判定为 NG（" + (r2?.Summary ?? "无结果") + "）");
                }
            }
            catch (Exception ex)
            {
                Check(false, "字符识别验证抛异常：" + ex.GetType().Name + " / " + ex.Message);
            }

            // ===================== 段 N：图像采集「来源」 =====================
            Section("N  流程视觉：图像采集来源（默认相机 / 相机名解析 / 路径无效明确报错）");
            try
            {
                // 1) 默认来源=相机（旧默认「文件」：模板没配路径 → 每次都静默出测试图）
                var def = new VisualFlowStep { StepType = "图像采集" };
                Check(def.SourceType == "相机", "图像采集默认来源=相机（不再默认「文件」）");

                // 2) 相机名 → 下标解析（模板里存的是相机名，如「下视相机」；旧实现只 int.TryParse）
                var cams = ProjectStore.Data.Cameras;
                var backup = cams.ToList();
                cams.Clear();
                cams.Add(new CameraItem { Name = "上视相机" });
                cams.Add(new CameraItem { Name = "下视相机" });
                Check(VisionEngine.ResolveCameraIndex("下视相机") == 1, "相机名「下视相机」解析为下标 1");
                Check(VisionEngine.ResolveCameraIndex("上视相机") == 0, "相机名「上视相机」解析为下标 0");
                Check(VisionEngine.ResolveCameraIndex("0") == 0, "纯数字「0」按 0 基索引解析");
                Check(VisionEngine.ResolveCameraIndex("相机2") == 1, "「相机2」按 1 基解析为下标 1");
                Check(VisionEngine.ResolveCameraIndex("") == 0 && VisionEngine.ResolveCameraIndex("查无此机") == 0,
                      "空 / 未知相机名回退下标 0");
                cams.Clear(); foreach (var c in backup) cams.Add(c);

                // 3) 来源=文件 但没给路径 → 明确失败，不再静默回退测试图
                var noPath = VisionEngine.Run(new[]
                {
                    new VisualFlowStep { Name = "采集", StepType = "图像采集", SourceType = "文件", SavePath = "", Enabled = true }
                });
                var rNoPath = noPath.Results.LastOrDefault(x => x.Type == "图像采集");
                Check(rNoPath is { Ok: false } && !noPath.HasImage,
                      "来源=文件 未给路径 → 步骤失败且不出图（" + (rNoPath?.Summary ?? "无结果") + "）");

                // 4) 来源=文件 路径不存在 → 明确失败
                var badPath = VisionEngine.Run(new[]
                {
                    new VisualFlowStep { Name = "采集", StepType = "图像采集", SourceType = "文件",
                                         SavePath = @"Z:\查无此目录\nope.png", Enabled = true }
                });
                var rBad = badPath.Results.LastOrDefault(x => x.Type == "图像采集");
                Check(rBad is { Ok: false } && !badPath.HasImage,
                      "来源=文件 路径不存在 → 步骤失败（" + (rBad?.Summary ?? "无结果") + "）");

                // 5) 来源=文件夹 没给路径 → 明确失败
                var noFolder = VisionEngine.Run(new[]
                {
                    new VisualFlowStep { Name = "采集", StepType = "图像采集", SourceType = "文件夹", FolderPath = "", Enabled = true }
                });
                var rNoFolder = noFolder.Results.LastOrDefault(x => x.Type == "图像采集");
                Check(rNoFolder is { Ok: false } && !noFolder.HasImage, "来源=文件夹 未给路径 → 步骤失败");

                // 6) 采集失败后下游步骤必须给明确提示（旧实现 cur==null 会 NRE / 报难懂的异常）
                var chain = VisionEngine.Run(new[]
                {
                    new VisualFlowStep { Name = "采集", StepType = "图像采集", SourceType = "文件", SavePath = "", Enabled = true },
                    new VisualFlowStep { Name = "匹配", StepType = "模板匹配", Enabled = true }
                });
                var rMatch = chain.Results.LastOrDefault(x => x.Type == "模板匹配");
                Check(rMatch is { Ok: false } && rMatch.Summary.Contains("请先执行图像采集"),
                      "采集失败后下游步骤给出明确提示（" + (rMatch?.Summary ?? "无结果") + "）");

                // 7) 详情 VM：来源=相机 时显示相机行，且相机下拉列出工程相机名
                var cams2 = ProjectStore.Data.Cameras;
                var backup2 = cams2.ToList();
                cams2.Clear();
                cams2.Add(new CameraItem { Name = "上视相机" });
                cams2.Add(new CameraItem { Name = "下视相机" });
                var vm = new NoCodeMotion.Views.VisualFlowDetailViewModel();
                vm.SelectedStep = new VisualFlowStep { StepType = "图像采集", SourceType = "相机" };
                Check(vm.IsCameraSource && !vm.IsFileSource, "VM：来源=相机 时 IsCameraSource=true");
                Check(vm.CameraNames.Contains("上视相机") && vm.CameraNames.Contains("下视相机"),
                      "VM：CameraNames 列出工程相机名（" + string.Join("/", vm.CameraNames) + "）");
                vm.SelectedStep.SourceType = "文件";
                Check(vm.IsFileSource && !vm.IsCameraSource, "VM：切到来源=文件 后标志跟着切（下拉显隐正确）");
                cams2.Clear(); foreach (var c in backup2) cams2.Add(c);

                // 8) 来源=相机 且读不到相机 → 回退测试图（这条要 OpenCV 造图）
                bool cvOk;
                try { using var probe = new OpenCvSharp.Mat(4, 4, OpenCvSharp.MatType.CV_8UC3); cvOk = probe.Width == 4; }
                catch { cvOk = false; }
                if (!cvOk)
                {
                    Console.WriteLine("  SKIP  OpenCvSharp 原生库不可用，跳过「来源=相机 回退测试图」端到端");
                }
                else
                {
                    // 索引 99 保证本机不存在 → 走回退分支
                    var camRep = VisionEngine.Run(new[]
                    {
                        new VisualFlowStep { Name = "采集", StepType = "图像采集", SourceType = "相机",
                                             CameraId = "99", Width = 320, Height = 240, Enabled = true }
                    });
                    var rCam = camRep.Results.LastOrDefault(x => x.Type == "图像采集");
                    Check(rCam != null && camRep.HasImage && rCam.Summary.Contains("测试图"),
                          "来源=相机 读不到相机时回退测试图（" + (rCam?.Summary ?? "无结果") + "）");
                }
            }
            catch (Exception ex)
            {
                Check(false, "图像采集来源验证抛异常：" + ex.GetType().Name + " / " + ex.Message);
            }

            // ===================== 段 O：结果图缩放 / 平移行为 =====================
            Section("O  视觉流程页结果图：滚轮缩放 + 中键拖拽平移 + 双击复位（ZoomPanBehavior）");
            try
            {
                // 1) 光标锚定缩放的不变式：缩放前后，光标底下那个内容点必须停在同一屏幕位置
                bool anchorOk = true;
                var cases = new[] { (100.0, 1.0, 2.0), (37.5, 1.0, 1.15), (640.0, 3.0, 1.5), (12.0, 8.0, 1.0 / 1.15) };
                foreach (var (mx, oldS, newS) in cases)
                {
                    double my = mx / 2;
                    var (nox, noy) = ZoomPanBehavior.ZoomOffset(mx, my, oldS, newS, 0, 0);
                    double px = mx / oldS, py = my / oldS;          // 光标下的内容点（本地坐标）
                    if (Math.Abs(newS * px + nox - mx) > 1e-6 || Math.Abs(newS * py + noy - my) > 1e-6)
                        anchorOk = false;
                }
                Check(anchorOk, "光标锚定缩放：光标下的内容点缩放后仍停在原屏幕位置（4 组参数）");

                // 2) 平移 1:1 跟手
                var (px2, py2) = ZoomPanBehavior.PanOffset(10, -20, 33.5, -7.25);
                Check(Math.Abs(px2 - 43.5) < 1e-9 && Math.Abs(py2 + 27.25) < 1e-9,
                      "平移 1:1：新偏移 = 起始偏移 + 鼠标位移");

                // 3) 挂到元素上：变换组顺序必须是 Scale → Translate，原点左上
                var zoomBorder = new System.Windows.Controls.Border();
                var zoomHost = new System.Windows.Controls.Grid();
                zoomBorder.Child = zoomHost;
                ZoomPanBehavior.SetIsEnabled(zoomHost, true);
                Check(zoomHost.RenderTransform is TransformGroup tg0 && tg0.Children.Count >= 2
                      && tg0.Children[0] is ScaleTransform && tg0.Children[1] is TranslateTransform,
                      "启用后建立 TransformGroup(Scale → Translate)（顺序反了 offset 语义就错）");
                Check(zoomHost.RenderTransformOrigin == new Point(0, 0), "变换原点取左上角 (0,0)");

                // 4) 缩放 → 变换值同步
                ZoomPanBehavior.ZoomAtPoint(zoomHost, new Point(200, 120), 2.0);
                Check(Math.Abs(ZoomPanBehavior.GetScale(zoomHost) - 2.0) < 1e-9
                      && Math.Abs(ZoomPanBehavior.GetOffsetX(zoomHost) + 200) < 1e-9
                      && Math.Abs(ZoomPanBehavior.GetOffsetY(zoomHost) + 120) < 1e-9,
                      "ZoomAtPoint(2x @200,120) → scale=2 且 offset=(-200,-120)");
                var tg1 = (TransformGroup)zoomHost.RenderTransform;
                var sc1 = (ScaleTransform)tg1.Children[0];
                var tr1 = (TranslateTransform)tg1.Children[1];
                Check(Math.Abs(sc1.ScaleX - 2.0) < 1e-9 && Math.Abs(sc1.ScaleY - 2.0) < 1e-9
                      && Math.Abs(tr1.X + 200) < 1e-9 && Math.Abs(tr1.Y + 120) < 1e-9,
                      "TransformGroup 内的 Scale/Translate 已同步（不是只有附加属性变了）");

                // 5) 平移 → 偏移累加
                ZoomPanBehavior.PanBy(zoomHost, 30, -15);
                Check(Math.Abs(ZoomPanBehavior.GetOffsetX(zoomHost) + 170) < 1e-9
                      && Math.Abs(ZoomPanBehavior.GetOffsetY(zoomHost) + 135) < 1e-9,
                      "PanBy(30,-15) → offset 累加为 (-170,-135)");

                // 6) 上限钳制
                ZoomPanBehavior.ZoomAtPoint(zoomHost, new Point(0, 0), 1000.0);
                Check(Math.Abs(ZoomPanBehavior.GetScale(zoomHost) - ZoomPanBehavior.GetMaxScale(zoomHost)) < 1e-9,
                      "缩放钳制到 MaxScale（" + ZoomPanBehavior.GetMaxScale(zoomHost) + "）");

                // 7) 双击复位
                ZoomPanBehavior.Reset(zoomHost);
                Check(Math.Abs(ZoomPanBehavior.GetScale(zoomHost) - 1.0) < 1e-9
                      && ZoomPanBehavior.GetOffsetX(zoomHost) == 0 && ZoomPanBehavior.GetOffsetY(zoomHost) == 0,
                      "Reset() → scale=1 / offset=(0,0)");

                // 8) 视觉流程页确实挂上了（XAML 里的附加属性真的生效，不只是写了个属性名）
                var vfpZoom = new NoCodeMotion.Views.VisualFlowPage();
                if (vfpZoom.FindName("ImageHost") is FrameworkElement imgHost)
                    Check(ZoomPanBehavior.GetIsEnabled(imgHost),
                          "视觉流程页 ImageHost 已启用 ZoomPanBehavior（XAML 生效）");
                else
                    Check(false, "视觉流程页里找不到 ImageHost");

                // 9) 事件接线：合成一次「中键按下」→ 行为应接管（光标切 SizeAll / 事件被标记已处理）
                try
                {
                    ZoomPanBehavior.Reset(zoomHost);
                    var downArgs = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Middle)
                    { RoutedEvent = UIElement.MouseDownEvent };
                    zoomHost.RaiseEvent(downArgs);
                    Check(downArgs.Handled && zoomHost.Cursor == Cursors.SizeAll,
                          "中键按下被行为接管（事件接线生效：Handled=true 且光标切 SizeAll）");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("  SKIP  合成鼠标事件在本环境不可用（" + ex.GetType().Name
                                      + "），中键接线以源码守卫 G25 为准");
                }
            }
            catch (Exception ex)
            {
                Check(false, "结果图缩放/平移验证抛异常：" + ex.GetType().Name + " / " + ex.Message);
            }

            // ===================== 段 P：缩放/平移的「肉眼可见」证据（离屏渲染截图） =====================
            Section("P  结果图缩放 / 平移：离屏渲染截图（1x vs 2x）+ 操作说明真的在可视化树上");
            try
            {
                string outDirP = RepoSmokeOut() ?? Path.Combine(AppContext.BaseDirectory, "out");
                Directory.CreateDirectory(outDirP);

                var tplP = ProjectTemplateCatalog.All.First(t => t.Id == "vision-guided");
                var pdP = tplP.Build();
                ProjectStore.Data.CopyFrom(pdP);          // 让页面不是空态
                var visFlowP = pdP.Flows.FirstOrDefault(f => f.Kind == FlowKind.Vision);
                Check(visFlowP != null && visFlowP.VisualSteps.Count > 0,
                      "视觉引导模板里存在视觉流程步骤（" + (visFlowP?.VisualSteps.Count ?? 0) + " 步）");

                var pageP = new NoCodeMotion.Views.VisualFlowPage();
                var vmP = (NoCodeMotion.Views.VisualFlowDetailViewModel)pageP.DataContext;
                vmP.Steps = visFlowP!.VisualSteps;
                vmP.SelectedStep = vmP.Steps.FirstOrDefault(s => s.StepType == "图像采集") ?? vmP.Steps[0];
                vmP.ResultImage = MakeTestBitmap(640, 480);   // 造一张有纹理的「运行结果」图
                vmP.RunStatus = "就绪";

                pageP.Measure(new Size(1240, 780));
                pageP.Arrange(new Rect(0, 0, 1240, 780));
                pageP.UpdateLayout();

                var hostP = pageP.FindName("ImageHost") as System.Windows.Controls.Grid;
                Check(hostP != null && hostP.ActualWidth > 50 && hostP.ActualHeight > 50,
                      "结果图容器有真实版面：" + (int)(hostP?.ActualWidth ?? 0) + "×"
                      + (int)(hostP?.ActualHeight ?? 0));

                // 操作说明必须在可视化树里（不是只写在 XAML 源码里、被样式藏了 / 被裁了）
                var hintP = AllDescendants(pageP).OfType<System.Windows.Controls.TextBlock>()
                                .FirstOrDefault(t => t.Text != null && t.Text.Contains("中键拖拽平移"));
                Check(hintP != null && hintP.ActualWidth > 0,
                      "操作说明已渲染到树上：「" + (hintP?.Text ?? "(缺失)") + "」");

                byte[] RenderPixels(FrameworkElement fe)
                {
                    int w = Math.Max(1, (int)Math.Ceiling(fe.ActualWidth));
                    int hh = Math.Max(1, (int)Math.Ceiling(fe.ActualHeight));
                    var rtb = new RenderTargetBitmap(w, hh, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(fe);
                    var px = new byte[w * hh * 4];
                    rtb.CopyPixels(px, w * 4, 0);
                    return px;
                }
                string SavePng(FrameworkElement fe, string file)
                {
                    int w = Math.Max(1, (int)Math.Ceiling(fe.ActualWidth));
                    int hh = Math.Max(1, (int)Math.Ceiling(fe.ActualHeight));
                    var rtb = new RenderTargetBitmap(w, hh, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(fe);
                    var enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(rtb));
                    string pp = Path.Combine(outDirP, file);
                    using (var fs = File.Create(pp)) enc.Save(fs);
                    return pp;
                }

                var px1 = RenderPixels(pageP);
                string f1 = SavePng(pageP, "zoompan_1x.png");

                // 真的放大 2 倍（模拟滚轮在容器中心滚到 2x）
                if (hostP != null)
                    ZoomPanBehavior.ZoomAtPoint(hostP,
                        new Point(hostP.ActualWidth / 2, hostP.ActualHeight / 2), 2.0);
                pageP.UpdateLayout();
                var px2 = RenderPixels(pageP);
                string f2 = SavePng(pageP, "zoompan_2x.png");

                Check(Math.Abs(ZoomPanBehavior.GetScale(hostP!) - 2.0) < 1e-9,
                      "截图时容器 scale = 2.0（放大真的落到了元素上）");
                Check(!px1.SequenceEqual(px2),
                      "2x 后的渲染像素与 1x 不同（缩放作用到渲染结果，而不只是改了附加属性）");
                Console.WriteLine("  截图：" + f1);
                Console.WriteLine("        " + f2);
            }
            catch (Exception ex)
            {
                Check(false, "结果图缩放/平移截图验证抛异常：" + ex.GetType().Name + " / " + ex.Message);
            }

            // ===================== 段 Q：字符识别结果 —— 每行文字画绿框 + 框上方绿字标签 =====================
            Section("Q  字符识别结果：每行文字框（绿框 + 绿字标签）叠加到结果图");
            try
            {
                // 1) 投影数学：屏幕坐标 = 像素 * scale + offset，文字 / 通过标志原样带过
                var qItem = new TextBoxItem { Left = 100, Top = 50, Width = 200, Height = 40, Text = "ABC-123", Pass = true };
                var qBox = TextOverlayBox.Project(qItem, 0.5, 10, 20);
                Check(Math.Abs(qBox.ScreenLeft - 60) < 1e-9 && Math.Abs(qBox.ScreenTop - 45) < 1e-9
                      && Math.Abs(qBox.ScreenWidth - 100) < 1e-9 && Math.Abs(qBox.ScreenHeight - 20) < 1e-9
                      && qBox.Text == "ABC-123" && qBox.Pass,
                      "TextOverlayBox.Project：屏幕坐标 = 像素*scale + offset，文字/通过标志原样带过");

                // 2) Pass=False 原样传递（渲染层据此切红框红字）
                var qBox2 = TextOverlayBox.Project(
                    new TextBoxItem { Left = 0, Top = 0, Width = 10, Height = 10, Text = "NG", Pass = false }, 2.0, 0, 0);
                Check(!qBox2.Pass && Math.Abs(qBox2.ScreenWidth - 20) < 1e-9,
                      "Pass=False 原样传递（渲染层据此切红框红字）");

                // 3) 真页面：XAML 里的字符识别叠加层确实存在
                var qTpl = ProjectTemplateCatalog.All.First(x => x.Id == "vision-guided");
                var qPd = qTpl.Build();
                ProjectStore.Data.CopyFrom(qPd);
                var qFlow = qPd.Flows.First(f => f.Kind == FlowKind.Vision);
                var qPage = new NoCodeMotion.Views.VisualFlowPage();
                var qVm = (NoCodeMotion.Views.VisualFlowDetailViewModel)qPage.DataContext;
                qVm.Steps = qFlow.VisualSteps;
                qVm.SelectedStep = qVm.Steps.FirstOrDefault(s => s.StepType == "字符识别") ?? qVm.Steps[0];
                qVm.ResultImage = MakeTestBitmap(640, 480);
                qPage.Measure(new Size(1240, 780));
                qPage.Arrange(new Rect(0, 0, 1240, 780));
                qPage.UpdateLayout();

                var qCanvas = qPage.FindName("TextOverlay") as System.Windows.Controls.Canvas;
                Check(qCanvas != null, "视觉流程页存在字符识别叠加层 TextOverlay（XAML 生效）");

                // 4) 布局完成后再塞文字框（此时 ImageHost 已有真实尺寸，投影会立即执行）
                qVm.OcrTextBoxes = new System.Collections.ObjectModel.ObservableCollection<TextBoxItem>
                {
                    new TextBoxItem { Left = 40, Top = 40,  Width = 180, Height = 36, Text = "SN:2026", Pass = true },
                    new TextBoxItem { Left = 40, Top = 120, Width = 150, Height = 36, Text = "NG-CODE", Pass = false }
                };
                qPage.UpdateLayout();
                Check(qVm.TextOverlayBoxes != null && qVm.TextOverlayBoxes.Count == 2,
                      "VM 把 2 个文字框投影为 TextOverlayBoxes（与匹配框共用同一 scale/offset）");

                // 5) 匹配框走同一条投影路径（两类叠加层共用 ProjectOverlayBoxes）
                qVm.MatchResults = new System.Collections.ObjectModel.ObservableCollection<MatchBox>
                {
                    new MatchBox { LeftTopX = 10, LeftTopY = 10, TemplateWidth = 50, TemplateHeight = 50, Score = 0.99, Pass = true }
                };
                qPage.UpdateLayout();
                Check(qVm.OverlayBoxes != null && qVm.OverlayBoxes.Count == 1,
                      "匹配框也走同一投影路径（OverlayBoxes 随 MatchResults 刷新）");

                if (qCanvas != null)
                {
                    qCanvas.UpdateLayout();
                    int qRects = Descendants(qCanvas).OfType<System.Windows.Shapes.Rectangle>().Count();
                    int qLabels = Descendants(qCanvas).OfType<System.Windows.Controls.TextBlock>()
                                      .Count(x => x.Text == "SN:2026" || x.Text == "NG-CODE");
                    Check(qRects >= 2, "叠加层渲染出 " + qRects + " 个文字框");
                    Check(qLabels == 2, "框上方渲染出识别文字标签：" + qLabels + " 条");
                }

                // 6) 离屏渲染一张图作为肉眼证据（字符识别绿框 + 框上方绿字标签）
                string outDirQ = RepoSmokeOut() ?? Path.Combine(AppContext.BaseDirectory, "out");
                Directory.CreateDirectory(outDirQ);
                int qw2 = Math.Max(1, (int)Math.Ceiling(qPage.ActualWidth));
                int qh2 = Math.Max(1, (int)Math.Ceiling(qPage.ActualHeight));
                var rtbQ = new RenderTargetBitmap(qw2, qh2, 96, 96, PixelFormats.Pbgra32);
                rtbQ.Render(qPage);
                var encQ = new PngBitmapEncoder();
                encQ.Frames.Add(BitmapFrame.Create(rtbQ));
                string fq = Path.Combine(outDirQ, "ocr_overlay.png");
                using (var fs = File.Create(fq)) encQ.Save(fs);
                Console.WriteLine("  截图：" + fq);
            }
            catch (Exception ex)
            {
                Check(false, "字符识别叠加层验证抛异常：" + ex.GetType().Name + " / " + ex.Message);
            }

            // ===================== 段 R：视觉标定 —— 9 点 XY 仿射 + 5 点旋转圆拟合 =====================
            Section("R  视觉标定：9 点 XY 仿射 + 5 点旋转圆拟合（纯合成数据端到端）");
            try
            {
                // 合成圆斑（BGRA，字节序 B,G,R,A）：白底黑点，与 MarkerDetector / VisionEngine 的约定一致
                byte[] Disc(int w, int h, double cu, double cv, double r)
                {
                    var buf = new byte[w * h * 4];
                    for (int i = 0; i < buf.Length; i += 4)
                    { buf[i] = 255; buf[i + 1] = 255; buf[i + 2] = 255; buf[i + 3] = 255; }
                    double r2 = r * r;
                    int v0 = Math.Max(0, (int)Math.Floor(cv - r - 1)), v1 = Math.Min(h - 1, (int)Math.Ceiling(cv + r + 1));
                    int u0 = Math.Max(0, (int)Math.Floor(cu - r - 1)), u1 = Math.Min(w - 1, (int)Math.Ceiling(cu + r + 1));
                    for (int v = v0; v <= v1; v++)
                        for (int u = u0; u <= u1; u++)
                        {
                            double du = u - cu, dv = v - cv;
                            if (du * du + dv * dv > r2) continue;
                            int o = (v * w + u) * 4;
                            buf[o] = 0; buf[o + 1] = 0; buf[o + 2] = 0; buf[o + 3] = 255;
                        }
                    return buf;
                }

                // 可视化树 + 逻辑树一起走（离屏、没进窗口时可视化树可能还没完全展开）
                List<DependencyObject> Walk(DependencyObject root)
                {
                    var all = new List<DependencyObject>();
                    var stack = new Stack<DependencyObject>();
                    stack.Push(root);
                    int guard = 0;
                    while (stack.Count > 0 && guard++ < 40000)
                    {
                        var cur = stack.Pop();
                        all.Add(cur);
                        foreach (var c in Children(cur)) stack.Push(c);
                    }
                    return all;
                }

                System.Windows.Controls.Border? NearestBorder(DependencyObject? d)
                {
                    for (var cur = d; cur != null; cur = VisualTreeHelper.GetParent(cur))
                        if (cur is System.Windows.Controls.Border b) return b;
                    return null;
                }

                const double S = 0.5;   // 真值像素当量 mm/px

                // ---------- R1 求解器：9 点仿射（解析点集，精确恢复） ----------
                var rAna = new List<(double U, double V, double X, double Y)>();
                for (int iy = -1; iy <= 1; iy++)
                    for (int ix = -1; ix <= 1; ix++)
                    {
                        double mx = ix * 10.0, my = iy * 10.0;
                        rAna.Add((320.0 + mx / S, 240.0 + my / S, mx, my));
                    }
                var rFa = CalibSolver.FitAffine(rAna);
                Check(rFa.Ok && rFa.RmsMm < 1e-9,
                      "9 点仿射无噪声精确恢复（RMS " + CalibSolver.Fmt(rFa.RmsMm) + " mm ≈ 0）");
                Check(Math.Abs(rFa.PixelEquivalentU - S) < 1e-6
                      && Math.Abs(rFa.PixelEquivalentV - S) < 1e-6
                      && Math.Abs(rFa.AngleXDeg) < 1e-6
                      && Math.Abs(rFa.AngleYDeg - 90.0) < 1e-6,
                      "像素当量 = √(A1²+B1²) = " + CalibSolver.Fmt(rFa.PixelEquivalentU)
                      + " mm/px，X 向 " + CalibSolver.Fmt(rFa.AngleXDeg)
                      + "°、Y 向 " + CalibSolver.Fmt(rFa.AngleYDeg) + "°");
                rFa.Map(320.0, 240.0, out double rFaX, out double rFaY);
                Check(Math.Abs(rFaX) < 1e-6 && Math.Abs(rFaY) < 1e-6,
                      "Map(图像中心) 回到机台原点（" + CalibSolver.Fmt(rFaX) + ", " + CalibSolver.Fmt(rFaY) + "）mm");

                // 机台系相对图像系旋转 30°：u = 320 + (X·cosθ - Y·sinθ)/s，v = 240 + (X·sinθ + Y·cosθ)/s
                double th = 30.0 * Math.PI / 180.0, cth = Math.Cos(th), sth = Math.Sin(th);
                var rAna2 = new List<(double U, double V, double X, double Y)>();
                for (int iy = -1; iy <= 1; iy++)
                    for (int ix = -1; ix <= 1; ix++)
                    {
                        double mx = ix * 10.0, my = iy * 10.0;
                        rAna2.Add((320.0 + (mx * cth - my * sth) / S,
                                   240.0 + (mx * sth + my * cth) / S, mx, my));
                    }
                var rFa2 = CalibSolver.FitAffine(rAna2);
                Check(rFa2.Ok && Math.Abs(rFa2.PixelEquivalentU - S) < 1e-6
                      && Math.Abs(rFa2.AngleXDeg + 30.0) < 1e-6
                      && Math.Abs(rFa2.AngleYDeg - 60.0) < 1e-6,
                      "带 30° 旋转的 9 点同样精确恢复（X 向 " + CalibSolver.Fmt(rFa2.AngleXDeg)
                      + "°、Y 向 " + CalibSolver.Fmt(rFa2.AngleYDeg) + "°）");

                // 反向：点不够 / 共线必须明确失败，不能返回一个「看着像成功」的仿射
                var rFew = CalibSolver.FitAffine(new List<(double U, double V, double X, double Y)>
                    { (0, 0, 0, 0), (1, 1, 1, 1) });
                Check(!rFew.Ok && rFew.Message.Length > 0, "点数不足 → 明确失败：" + rFew.Message);
                var rCol = CalibSolver.FitAffine(new List<(double U, double V, double X, double Y)>
                    { (0, 0, 0, 0), (10, 10, 1, 1), (20, 20, 2, 2), (30, 30, 3, 3), (40, 40, 4, 4) });
                Check(!rCol.Ok, "点共线（退化）→ 明确失败，不返回假结果：" + rCol.Message);

                // ---------- R2 求解器：5 点圆拟合（解析点集，精确恢复） ----------
                var rAnaC = new List<(double U, double V)>();
                for (int k = 0; k < 5; k++)
                {
                    double a = (-40.0 + k * 20.0) * Math.PI / 180.0;
                    rAnaC.Add((400.0 + 40.0 * Math.Cos(a), 300.0 + 40.0 * Math.Sin(a)));
                }
                var rFc = CalibSolver.FitCircle(rAnaC);
                Check(rFc.Ok && Math.Abs(rFc.CenterU - 400.0) < 1e-6 && Math.Abs(rFc.CenterV - 300.0) < 1e-6
                      && Math.Abs(rFc.RadiusPx - 40.0) < 1e-6 && rFc.RmsPx < 1e-8,
                      "5 点圆拟合精确恢复旋转中心 (" + CalibSolver.Fmt(rFc.CenterU) + ", "
                      + CalibSolver.Fmt(rFc.CenterV) + ") px、半径 " + CalibSolver.Fmt(rFc.RadiusPx) + " px");

                // ---------- R3 点集序列化往返（落盘用字符串存，读回必须一模一样） ----------
                var rSer = CalibSolver.SerializePoints2(new[] { (1.5, -2.25), (3.0, 4.0) });
                var rPar = CalibSolver.ParsePoints2(rSer);
                Check(rSer == "1.5,-2.25;3,4" && rPar.Count == 2
                      && Math.Abs(rPar[1].A - 3.0) < 1e-12 && Math.Abs(rPar[0].B + 2.25) < 1e-12,
                      "点集序列化用不变文化（小数点是 '.'，不受系统区域影响）：" + rSer);

                // ---------- R4 标记检测：合成圆斑质心 ----------
                var rBlob = MarkerDetector.DetectLargest(Disc(640, 480, 123.0, 234.0, 8.0), 640, 480,
                                                         new MarkerDetectOptions(), out string rBlobErr);
                Check(rBlob != null && Math.Abs(rBlob.CenterU - 123.0) < 0.05 && Math.Abs(rBlob.CenterV - 234.0) < 0.05,
                      "合成圆斑质心 ≈ (123, 234)：" + (rBlob == null ? rBlobErr : rBlob.Describe()));
                Check(rBlob != null && rBlob.Area > 150 && rBlob.FillRatio > 0.6 && rBlob.Circularity > 0.45,
                      "圆斑面积/填充率/圆度都在合理区间（实心圆填充率 ≈ 0.68、圆度 ≈ 0.54）");
                var rBlank = MarkerDetector.DetectLargest(Disc(640, 480, -500, -500, 8.0), 640, 480,
                                                          new MarkerDetectOptions(), out string rBlankErr);
                Check(rBlank == null && rBlankErr.Length > 0,
                      "全白图里找不到标记 → 返回 null 并给出原因：" + rBlankErr);

                // ---------- R5 端到端：假轴 + 合成图，程序自动走 9 点 ----------
                double fX = 0, fY = 0, fA = 0;
                int guardCount = 0;
                bool rotPhase = false;

                var runner9 = new CalibrationRunner
                {
                    ReadAxisPosition = n => n == "X" ? fX : fY,
                    MoveAxisAbs = (n, p) => { if (n == "X") fX = p; else fY = p; },
                    IsAxisDone = n => true,
                    GrabFrame = () => (Disc(640, 480, 320.0 + fX / S, 240.0 + fY / S, 8.0), 640, 480),
                    Guard = () => guardCount++
                };
                var opt9 = new CalibrationRunOptions
                {
                    XAxisName = "X", YAxisName = "Y", RotationAxisName = "R",
                    PitchMm = 10.0, SettleMs = 2, DoNinePoint = true, DoRotation = false
                };
                var res9 = runner9.Run(opt9);
                Check(res9.Ok && res9.NinePoints.Count == 9,
                      "程序自动走满 9 点并取到 9 个样本：" + res9.Message);
                Check(res9.Affine != null && res9.Affine.Ok && res9.Affine.RmsMm < 1e-6
                      && Math.Abs(res9.Affine.PixelEquivalentU - S) < 1e-6
                      && Math.Abs(res9.Affine.AngleXDeg) < 1e-6,
                      "9 点端到端还原像素当量 " + CalibSolver.Fmt(res9.Affine?.PixelEquivalentU ?? -1)
                      + " mm/px（真值 " + CalibSolver.Fmt(S) + "）");
                Check(Math.Abs((res9.Affine?.A3 ?? 0) + 160.0) < 1e-6 && Math.Abs((res9.Affine?.B3 ?? 0) + 120.0) < 1e-6,
                      "仿射平移项也还原（图像中心 (320,240) 对应机台原点）");
                Check(guardCount > 0,
                      "标定过程中每点都轮询暂停/停止守卫（Guard 被调用 " + guardCount + " 次）");

                // ---------- R6 端到端：5 点旋转（标记绕旋转中心走 5 个角度） ----------
                fA = 0;
                var runnerRot = new CalibrationRunner
                {
                    ReadAxisPosition = n => fA,
                    MoveAxisAbs = (n, p) => fA = p,
                    IsAxisDone = n => true,
                    GrabFrame = () =>
                    {
                        double a = fA * Math.PI / 180.0;
                        return (Disc(640, 480, 400.0 + 40.0 * Math.Cos(a), 300.0 + 40.0 * Math.Sin(a), 8.0), 640, 480);
                    },
                    Guard = () => guardCount++
                };
                var optR = new CalibrationRunOptions
                {
                    RotationAxisName = "R", RotationStepDeg = 20.0, RotationCount = 5,
                    SettleMs = 2, DoNinePoint = false, DoRotation = true
                };
                var resR = runnerRot.Run(optR);
                Check(resR.Ok && resR.RotationPoints.Count == 5,
                      "程序自动走满 5 点旋转并取到 5 个样本：" + resR.Message);
                Check(resR.Circle != null && resR.Circle.Ok
                      && Math.Abs(resR.Circle.CenterU - 400.0) < 0.5 && Math.Abs(resR.Circle.CenterV - 300.0) < 0.5
                      && Math.Abs(resR.Circle.RadiusPx - 40.0) < 0.5,
                      "5 点端到端还原旋转中心 (" + CalibSolver.Fmt(resR.Circle?.CenterU ?? -1) + ", "
                      + CalibSolver.Fmt(resR.Circle?.CenterV ?? -1) + ") px、半径 "
                      + CalibSolver.Fmt(resR.Circle?.RadiusPx ?? -1) + " px");
                Check(Math.Abs((resR.RotationPoints[0].Angle) + 40.0) < 1e-9
                      && Math.Abs(resR.RotationPoints[4].Angle - 40.0) < 1e-9,
                      "5 个角度以基准角为中心对称铺开（-40° … +40°，基准 0°）");

                // ---------- R7 端到端：一次跑完 9 点 + 5 点 → 工程记录 → 真实位置换算 ----------
                fX = 0; fY = 0; fA = 0; rotPhase = false;
                var runnerAll = new CalibrationRunner
                {
                    ReadAxisPosition = n => n == "R" ? fA : (n == "X" ? fX : fY),
                    // 第一次动旋转轴 = 进入旋转阶段（9 点在先、5 点在后）
                    MoveAxisAbs = (n, p) => { if (n == "R") { rotPhase = true; fA = p; } else if (n == "X") fX = p; else fY = p; },
                    IsAxisDone = n => true,
                    GrabFrame = () =>
                    {
                        if (rotPhase)
                        {
                            double a = fA * Math.PI / 180.0;
                            return (Disc(640, 480, 400.0 + 40.0 * Math.Cos(a), 300.0 + 40.0 * Math.Sin(a), 8.0), 640, 480);
                        }
                        return (Disc(640, 480, 320.0 + fX / S, 240.0 + fY / S, 8.0), 640, 480);
                    },
                    Guard = () => guardCount++
                };
                var optAll = new CalibrationRunOptions
                {
                    XAxisName = "X", YAxisName = "Y", RotationAxisName = "R",
                    PitchMm = 10.0, RotationStepDeg = 20.0, RotationCount = 5, SettleMs = 2
                };
                var resAll = runnerAll.Run(optAll);
                Check(resAll.Ok && resAll.Affine != null && resAll.Circle != null,
                      "一次跑完 9 点 + 5 点：" + resAll.Message);
                var recAll = resAll.ToRecord("CAM-E2E");
                Check(recAll.IsUsable && recAll.RotRadiusPx > 0 && recAll.RotCenterU > 0 && recAll.Points9.Length > 0,
                      "结果转成工程记录（像素当量 / 旋转中心 / 半径 / 点集字符串都齐）");
                Check(CalibSolver.ParsePoints2(recAll.Points9).Count == 9
                      && CalibSolver.ParsePoints2(recAll.Machine9).Count == 9
                      && CalibSolver.ParsePoints3(recAll.Points5).Count == 5,
                      "落盘用的点集字符串往返正确（图像 9 点 / 机台 9 点 / 旋转 5 点）");

                recAll.ImageToMachine(320.0, 240.0, out double rMx, out double rMy);
                Check(Math.Abs(rMx) < 0.05 && Math.Abs(rMy) < 0.05,
                      "ImageToMachine(图像中心) ≈ 机台原点（" + CalibSolver.Fmt(rMx) + ", " + CalibSolver.Fmt(rMy) + "）mm");
                recAll.ImageToMachineRotated(recAll.RotCenterU, recAll.RotCenterV, 90.0, out double rCx, out double rCy);
                recAll.ImageToMachine(recAll.RotCenterU, recAll.RotCenterV, out double rEx, out double rEy);
                Check(Math.Abs(rCx - rEx) < 1e-9 && Math.Abs(rCy - rEy) < 1e-9,
                      "旋转中心自身是旋转不动点（绕它转 90° 结果不变）");
                recAll.ImageToMachineRotated(recAll.RotCenterU + 40.0, recAll.RotCenterV, 180.0, out double r180x, out double r180y);
                recAll.ImageToMachine(recAll.RotCenterU - 40.0, recAll.RotCenterV, out double r180ex, out double r180ey);
                Check(Math.Abs(r180x - r180ex) < 1e-9 && Math.Abs(r180y - r180ey) < 1e-9,
                      "绕旋转中心转 180° 落在中心的另一侧（与直接换算镜像点一致）");
                recAll.ImageToMachineRotated(320.0, 240.0, 0.0, out double r0x, out double r0y);
                Check(Math.Abs(r0x - rMx) < 1e-9 && Math.Abs(r0y - rMy) < 1e-9,
                      "角度 0 退化为直接仿射（模板无旋转时不改变结果）");

                // ---------- R8 失败路径：不瞎动轴 / 不回退合成图 / 停止必须冒泡 ----------
                var runnerBadAxis = new CalibrationRunner
                {
                    ReadAxisPosition = n => 0,
                    MoveAxisAbs = (n, p) => { },
                    IsAxisDone = n => true,
                    GrabFrame = () => (Disc(640, 480, 320, 240, 8), 640, 480)
                };
                var resBadAxis = runnerBadAxis.Run(new CalibrationRunOptions { SettleMs = 0 });
                Check(!resBadAxis.Ok && resBadAxis.Message.Contains("X 轴与 Y 轴"),
                      "没指定轴名 → 明确失败，不会拿默认轴乱动：" + resBadAxis.Message);

                var runnerNoImg = new CalibrationRunner
                {
                    ReadAxisPosition = n => 0,
                    MoveAxisAbs = (n, p) => { },
                    IsAxisDone = n => true,
                    GrabFrame = () => ((byte[]?)null, 0, 0)
                };
                var resNoImg = runnerNoImg.Run(new CalibrationRunOptions
                {
                    XAxisName = "X", YAxisName = "Y", RotationAxisName = "R", SettleMs = 0
                });
                Check(!resNoImg.Ok && resNoImg.Affine == null && resNoImg.Message.Contains("采集图像失败"),
                      "取不到图 → 明确失败且不产出仿射（不回退合成图，合成图会悄悄给出垃圾标定）：" + resNoImg.Message);

                var runnerNoCb = new CalibrationRunner();
                var resNoCb = runnerNoCb.Run(new CalibrationRunOptions { XAxisName = "X", YAxisName = "Y" });
                Check(!resNoCb.Ok && resNoCb.Message.Contains("缺少硬件回调"),
                      "缺硬件回调 → 明确失败：" + resNoCb.Message);

                int stopAfter = 4, guardHits = 0;
                var runnerStop = new CalibrationRunner
                {
                    ReadAxisPosition = n => 0,
                    MoveAxisAbs = (n, p) => { },
                    IsAxisDone = n => true,
                    GrabFrame = () => (Disc(640, 480, 320, 240, 8), 640, 480),
                    Guard = () => { if (++guardHits > stopAfter) throw new OperationCanceledException(); }
                };
                bool stopThrew = false;
                try
                {
                    runnerStop.Run(new CalibrationRunOptions
                    {
                        XAxisName = "X", YAxisName = "Y", RotationAxisName = "R", SettleMs = 2
                    });
                }
                catch (OperationCanceledException) { stopThrew = true; }
                Check(stopThrew,
                      "标定中途按「停止」：OperationCanceledException 冒泡给上层（没被吞成「标定失败」，否则轴会继续走）");

                // ---------- R9 标定仓库 + 变量导出（随工程落盘、流程里可直接引用） ----------
                ProjectStore.Data.Calibrations.Clear();
                CalibrationStore.Upsert(recAll);
                Check(CalibrationStore.HasUsable("CAM-E2E") && CalibrationStore.Find("cam-e2e") != null,
                      "标定按相机名存进工程，查表忽略大小写");
                CalibrationStore.Upsert(new CameraCalibration
                {
                    CameraName = "CAM-E2E", IsValid = true, PixelEquivalent = 0.25,
                    AngleX = 1.0, AngleY = 91.0, RotCenterX = 5.0, RotCenterY = 6.0,
                    RotCenterU = 400.0, RotCenterV = 300.0
                });
                Check(ProjectStore.Data.Calibrations.Count == 1
                      && Math.Abs(CalibrationStore.Find("CAM-E2E")!.PixelEquivalent - 0.25) < 1e-12,
                      "同名 Upsert 是覆盖而不是新增（一台相机只有一条标定）");
                int nVar = CalibVarExport.Export(CalibrationStore.Find("CAM-E2E"));
                Check(nVar == 7, "导出 7 个标定变量（像素当量 / X·Y 方向角 / 旋转中心 mm 与 px），实得 " + nVar);
                Check(Math.Abs(SimRuntime.GetVariableResolved("标定_像素当量") - 0.25) < 1e-9
                      && Math.Abs(SimRuntime.GetVariableResolved("标定_旋转中心U") - 400.0) < 1e-9,
                      "变量读得回来（GetVariableResolved 命中变量表单元格）");
                bool exprOk = ExpressionEvaluator.Evaluate("标定_像素当量 * 2",
                                                           n => SimRuntime.GetVariableResolved(n), out double exprVal);
                Check(exprOk && Math.Abs(exprVal - 0.5) < 1e-9,
                      "变量名可在表达式里直接引用（标定_像素当量 * 2 = " + CalibSolver.Fmt(exprVal)
                      + "；名字带 '-' 的话会被当成减号算错）");
                CalibrationStore.Remove("CAM-E2E");
                Check(!CalibrationStore.HasUsable("CAM-E2E"), "删除标定后查不到（清干净，别影响后面的用例）");

                // ---------- R10 真页面：标定参数卡确实渲染出来 ----------
                var rTpl = ProjectTemplateCatalog.All.First(x => x.Id == "vision-guided");
                var rPd = rTpl.Build();
                ProjectStore.Data.CopyFrom(rPd);
                var rFlow = rPd.Flows.First(f => f.Kind == FlowKind.Vision);
                var rStep = new VisualFlowStep
                {
                    Name = "标定1", StepType = "标定",
                    CalibMode = "XY+旋转", CalibXAxis = "X", CalibYAxis = "Y", CalibRotAxis = "R"
                };
                rFlow.VisualSteps.Add(rStep);
                var rPage = new NoCodeMotion.Views.VisualFlowPage();
                var rVm = (NoCodeMotion.Views.VisualFlowDetailViewModel)rPage.DataContext;
                rVm.Steps = rFlow.VisualSteps;
                rVm.SelectedStep = rStep;
                rPage.Measure(new Size(1240, 780));
                rPage.Arrange(new Rect(0, 0, 1240, 780));
                rPage.UpdateLayout();

                Check(rVm.IsCalibration, "选中「标定」步骤 → VM.IsCalibration = true");
                Check(rVm.CalibAxisNames.Count > 0,
                      "轴名候选来自工程（CalibAxisNames 共 " + rVm.CalibAxisNames.Count + " 个）");

                var rNodes = Walk(rPage);
                var rHeader = rNodes.OfType<System.Windows.Controls.TextBlock>().FirstOrDefault(x => x.Text == "标定参数");
                Check(rHeader != null, "页面里存在「标定参数」标题（XAML 生效，没被 StaticResource 键写错搞崩）");
                Check(rHeader != null && NearestBorder(rHeader)?.Visibility == Visibility.Visible,
                      "选中标定步骤时，标定参数卡是可见的");
                var rTexts = rNodes.OfType<System.Windows.Controls.TextBlock>().Select(x => x.Text).ToList();
                Check(rTexts.Contains("标定方式") && rTexts.Contains("旋转轴") && rTexts.Contains("开始标定"),
                      "卡片含 标定方式 / 旋转轴 / 开始标定 三项");
                var rSliders = rNodes.OfType<NoCodeMotion.Views.NumericSliderRow>().ToList();
                Check(rSliders.Any(x => x.Label == "9 点间距") && rSliders.Any(x => x.Label == "旋转步距")
                      && rSliders.Any(x => x.Label == "标记阈值"),
                      "卡片含 9 点间距 / 旋转步距 / 标记阈值 数值行（共 " + rSliders.Count + " 行）");
                Check(rVm.CalibCurrentText.Length > 0, "当前标定回显有内容：" + rVm.CalibCurrentText);

                // 切到非标定步骤 → 卡片收起（关系断言，不锁死具体可见性值）
                rVm.SelectedStep = rFlow.VisualSteps.First(s => s.StepType != "标定");
                rPage.UpdateLayout();
                Check(!rVm.IsCalibration, "切到非标定步骤 → IsCalibration = false（卡片收起）");
                var rNodes2 = Walk(rPage);
                var rHeader2 = rNodes2.OfType<System.Windows.Controls.TextBlock>().FirstOrDefault(x => x.Text == "标定参数");
                Check(rHeader2 != null && NearestBorder(rHeader2)?.Visibility != Visibility.Visible,
                      "切走后标定参数卡不再可见（与选中时形成对照）");

                // ---------- R11 离屏渲染一张图作为肉眼证据 ----------
                rVm.SelectedStep = rStep;
                rPage.UpdateLayout();
                string outDirR = RepoSmokeOut() ?? Path.Combine(AppContext.BaseDirectory, "out");
                Directory.CreateDirectory(outDirR);
                int rw = Math.Max(1, (int)Math.Ceiling(rPage.ActualWidth));
                int rh = Math.Max(1, (int)Math.Ceiling(rPage.ActualHeight));
                var rtbR = new RenderTargetBitmap(rw, rh, 96, 96, PixelFormats.Pbgra32);
                rtbR.Render(rPage);
                var encR = new PngBitmapEncoder();
                encR.Frames.Add(BitmapFrame.Create(rtbR));
                string fr = Path.Combine(outDirR, "calibration_card.png");
                using (var fs = File.Create(fr)) encR.Save(fs);
                Console.WriteLine("  截图：" + fr);
            }
            catch (Exception ex)
            {
                Check(false, "视觉标定验证抛异常：" + ex.GetType().Name + " / " + ex.Message);
            }

            // ==================== 段 S：真实 SECS/HSMS 通讯 ====================
            // 数据项编解码（全 15 种格式二进制往返 + SML 往返 + 长度字段 1/2/3 字节边界）、
            // 报文头精确字节、控制消息 vs 数据消息判定、CommManager 分派，
            // 以及 127.0.0.1 上「被动设备端 ↔ 主动主机端」的完整端到端会话
            // （Select / S1F1→S1F2 自动应答 / S1F13→S1F14 / 未请求 S6F11 / Linktest / Separate / 非法帧长度断开）。
            Section("段 S：真实 SECS/HSMS 通讯（E5 数据项 + E37 会话 + 127.0.0.1 回环端到端）");
            try
            {
                // ---------- S1 格式码就是 SEMI E5 的 6 位格式码 ----------
                Check((int)SecsFormat.List == 0 && (int)SecsFormat.Binary == 8 && (int)SecsFormat.Boolean == 9
                      && (int)SecsFormat.Ascii == 16 && (int)SecsFormat.Jis8 == 17
                      && (int)SecsFormat.I8 == 24 && (int)SecsFormat.I1 == 25 && (int)SecsFormat.I2 == 26
                      && (int)SecsFormat.I4 == 28 && (int)SecsFormat.F8 == 32 && (int)SecsFormat.F4 == 36
                      && (int)SecsFormat.U8 == 40 && (int)SecsFormat.U1 == 41 && (int)SecsFormat.U2 == 42
                      && (int)SecsFormat.U4 == 44,
                      "S1  SecsFormat 枚举值就是 SEMI E5 的 6 位格式码（L=0 / A=16 / U4=44）");

                // ---------- S2 首字节 = (格式码 << 2) | 长度字节数 ----------
                var sA = SecsItem.A("ABC").ToBytes();
                var sU1 = SecsItem.U1(0x5A).ToBytes();
                var sL = SecsItem.L(SecsItem.U4(1)).ToBytes();
                Check(sA.Length == 5 && sA[0] == 0x41 && sA[1] == 3 && sA[2] == (byte)'A' && sA[4] == (byte)'C',
                      "S2  A「ABC」→ 首字节 0x41（16<<2|1）、长度 3、随后 ASCII 字节");
                Check(sU1.Length == 3 && sU1[0] == 0xA5 && sU1[1] == 1 && sU1[2] == 0x5A,
                      "S3  U1 0x5A → 首字节 0xA5（41<<2|1）、长度 1（元素数）");
                Check(sL.Length == 8 && sL[0] == 0x01 && sL[1] == 1
                      && sL[2] == 0xB1 && sL[3] == 1 && sL[7] == 1,
                      "S4  ★ L 的长度字段写「子项个数」1（不是字节数）；内层 U4 首字节 0xB1");

                // ---------- S5 长度字段 1 / 2 / 3 字节边界 ----------
                var s255 = SecsItem.U1(Enumerable.Range(0, 255).Select(i => (long)i).ToArray()).ToBytes();
                var s256 = SecsItem.U1(Enumerable.Range(0, 256).Select(i => (long)i).ToArray()).ToBytes();
                var s65535 = SecsItem.U1(Enumerable.Range(0, 65535).Select(i => (long)i).ToArray()).ToBytes();
                var s65536 = SecsItem.U1(Enumerable.Range(0, 65536).Select(i => (long)(i & 0xFF)).ToArray()).ToBytes();
                Check(s255.Length == 2 + 255 && s255[0] == 0xA5 && s255[1] == 0xFF,
                      "S5  255 个元素 → 1 字节长度字段（0xA5 0xFF）");
                Check(s256.Length == 3 + 256 && s256[0] == 0xA6 && s256[1] == 0x01 && s256[2] == 0x00,
                      "S6  256 个元素 → 长度字段涨到 2 字节（0xA6 0x01 0x00）");
                Check(s65535.Length == 3 + 65535 && s65535[0] == 0xA6 && s65535[1] == 0xFF && s65535[2] == 0xFF,
                      "S7  65535 个元素 → 仍是 2 字节长度字段（0xA6 0xFF 0xFF）");
                Check(s65536.Length == 4 + 65536 && s65536[0] == 0xA7 && s65536[1] == 0x01
                      && s65536[2] == 0x00 && s65536[3] == 0x00,
                      "S8  65536 个元素 → 长度字段涨到 3 字节（0xA7 0x01 0x00 0x00）");
                int sPos = 0;
                var sBig = SecsItem.Decode(s65536, ref sPos, out string sErr);
                Check(sBig != null && sPos == s65536.Length && sBig.Ints.Count == 65536
                      && sBig.Ints[255] == 255 && sBig.Ints[256] == 0,
                      "S9  65536 元素大项二进制往返完整（3 字节长度字段解码正确）");
                var sBigA = SecsItem.A(new string('x', 300)).ToBytes();
                Check(sBigA.Length == 3 + 300 && sBigA[0] == 0x42 && sBigA[1] == 0x01 && sBigA[2] == 0x2C,
                      "S10 A 的长度字段写「字节数」（300 字节 → 0x012C，首字节 0x42）");

                // ---------- S11 全 15 种格式的二进制往返 ----------
                var sAll = SecsItem.L(
                    SecsItem.A("START"), SecsItem.Jis("JIS"),
                    SecsItem.B(0x01, 0xFF), SecsItem.Bool(true, false),
                    SecsItem.U1(1, 200), SecsItem.U2(300, 40000), SecsItem.U4(70000), SecsItem.U8(5000000000L),
                    SecsItem.I1(-1, 100), SecsItem.I2(-300), SecsItem.I4(-70000), SecsItem.I8(-5000000000L),
                    SecsItem.F4(1.5), SecsItem.F8(-2.25),
                    SecsItem.L());
                var sAllBytes = sAll.ToBytes();
                int sPos2 = 0;
                var sAllBack = SecsItem.Decode(sAllBytes, ref sPos2, out string sErr2);
                Check(sAllBack != null && sPos2 == sAllBytes.Length,
                      "S11 全 15 种格式（L/A/JIS8/B/BOOLEAN/I1..I8/U1..U8/F4/F8）编码后能完整解码"
                      + (sAllBack == null ? "（解码失败：" + sErr2 + "）" : string.Empty));
                Check(sAllBack != null && sAllBack.ToSmlLine() == sAll.ToSmlLine(),
                      "S12 ★ 二进制往返后 SML 文本逐字相同（" + sAll.ToSmlLine().Substring(0, Math.Min(70, sAll.ToSmlLine().Length)) + "…）");
                Check(sAllBack != null && sAllBack.Children.Count == 15 && sAllBack.Children[14].Children.Count == 0,
                      "S13 列表子项数正确，空列表 <L> 往返不丢");

                // ---------- S14 SML 文本解析与往返 ----------
                string sSml = "S6F11 W <L <U4 1> <U4 1001> <A LOT01> <L <B 0x01 0x02> <BOOLEAN TRUE FALSE> <F8 1.5>>>";
                var sMsg = SecsMessage.ParseSml(sSml, 7, out string sErr3);
                Check(sMsg != null, "S14 SML 解析成功" + (sMsg == null ? "（失败：" + sErr3 + "）" : string.Empty));
                Check(sMsg != null && sMsg.Stream == 6 && sMsg.Function == 11 && sMsg.WBit && sMsg.DeviceId == 7,
                      "S15 SML「S6F11 W」→ Stream=6 / Function=11 / W-bit / DeviceId=7");
                if (sMsg != null)
                {
                    var sAgain = SecsMessage.ParseSml(sMsg.ToSmlLine(), 7, out string sErr4);
                    Check(sAgain != null && sAgain.ToSmlLine() == sMsg.ToSmlLine(),
                          "S16 ★ SML 文本往返逐字相同（" + sMsg.ToSmlLine() + "）");
                }
                Check(SecsMessage.ParseSml("LINKTEST", 0, out _)?.SType == HsmsSType.LinktestReq
                      && SecsMessage.ParseSml("SELECT", 0, out _)?.SType == HsmsSType.SelectReq
                      && SecsMessage.ParseSml("SEPARATE", 0, out _)?.SType == HsmsSType.SeparateReq,
                      "S17 SML 控制关键字 SELECT / LINKTEST / SEPARATE 都能解析");
                Check(SecsMessage.ParseSml("S1F1 后面不该有东西", 0, out string sErr5) == null && sErr5 != null,
                      "S18 非法 SML 明确失败并给中文原因（" + sErr5 + "）");

                // ---------- S19 报文头精确字节 ----------
                var s1f1 = SecsMessage.Data(3, 1, 1, true, 0x11223344);
                var s1f1F = s1f1.Encode();
                Check(s1f1F.Length == 10 && s1f1F[0] == 0 && s1f1F[1] == 3
                      && s1f1F[2] == 0x80 && s1f1F[3] == 1 && s1f1F[4] == 0 && s1f1F[5] == 1
                      && s1f1F[6] == 0x11 && s1f1F[9] == 0x44,
                      "S19 S1F1 W 头 10 字节：DeviceID=3 / byte2=0x80(W) / byte3=1(Stream) / byte4=0(PType) / byte5=1(Func) / SystemBytes 大端");
                var sBack1 = SecsMessage.Decode(s1f1F, 0, s1f1F.Length, out string sErr6);
                Check(sBack1 != null && sBack1.IsData && sBack1.Stream == 1 && sBack1.Function == 1
                      && sBack1.WBit && sBack1.SystemBytes == 0x11223344,
                      "S20 ★ S1F1 W 解码回来仍是「数据消息」（byte5=1 与 Select.req 同值，判定必须看 byte3==0）");
                var sSel = SecsMessage.Control(HsmsSType.SelectReq, 0, 0x11223344);
                var sSelF = sSel.Encode();
                Check(sSelF.Length == 10 && sSelF[0] == 0xFF && sSelF[1] == 0xFF && sSelF[2] == 0
                      && sSelF[3] == 0 && sSelF[4] == 0 && sSelF[5] == 1,
                      "S21 Select.req 头：SessionID=0xFFFF / byte2=0 / byte3=0 / byte5=1");
                var sBack2 = SecsMessage.Decode(sSelF, 0, sSelF.Length, out string sErr7);
                Check(sBack2 != null && sBack2.IsControl && sBack2.SType == HsmsSType.SelectReq
                      && sBack2.SessionId == 0xFFFF,
                      "S22 ★ Select.req 解码回来是「控制消息」（byte3==0 且 byte5 在控制码集合里）");
                var sBadPtype = (byte[])s1f1F.Clone();
                sBadPtype[4] = 1;
                Check(SecsMessage.Decode(sBadPtype, 0, sBadPtype.Length, out string sErr8) == null,
                      "S23 PType != 0 的帧明确解码失败（" + sErr8 + "）");
                Check(SecsMessage.Control(HsmsSType.SeparateReq, 0, 1).ReplyType() == null
                      && SecsMessage.Control(HsmsSType.SelectReq, 0, 1).ReplyType() == HsmsSType.SelectRsp
                      && SecsMessage.Control(HsmsSType.LinktestReq, 0, 1).ReplyType() == HsmsSType.LinktestRsp,
                      "S24 ★ Separate.req 没有应答（ReplyType()==null）→ 发送方不能等，否则白等 T6");

                // ---------- S25 CommManager 分派 + 模型默认值 ----------
                var sCfg = new CommItem { Name = "SECS1", CommType = "SECS(HSMS)", PortOrIp = "127.0.0.1", BaudOrPort = 5000 };
                using (var sCm = new CommManager())
                {
                    var sCh = sCm.Peek(sCfg);
                    Check(sCh is SecsCommChannel,
                          "S25 CommType=SECS(HSMS) → SecsCommChannel（★ 不能落到默认 TCP 分支，否则静默失去 SECS 语义）");
                    Check(sCm.TryPeek(sCfg) != null
                          && sCm.TryPeek(new CommItem { Name = "没建过", CommType = "SECS(HSMS)" }) == null,
                          "S26 TryPeek 只取已存在的通道，绝不创建（页面刷状态靠它，避免 S7 的 Create 抛异常）");
                    bool sThrew = false;
                    try { sCm.Peek(new CommItem { Name = "S7", CommType = "西门子S7" }); }
                    catch (NotSupportedException) { sThrew = true; }
                    Check(sThrew, "S27 西门子S7 仍是明确的「暂未内置协议」，不会被静默当成 TCP");
                    sCm.Close(sCfg);
                    Check(sCm.TryPeek(sCfg) == null, "S28 Close() 之后通道从缓存里移除");
                }
                var sDef = new CommItem();
                Check(sDef.SecsRole == "被动" && sDef.SecsDeviceId == 0 && sDef.SecsAutoReply
                      && sDef.SecsT3Ms == 45000 && sDef.SecsT5Ms == 10000 && sDef.SecsT6Ms == 5000
                      && sDef.SecsT7Ms == 10000 && sDef.SecsT8Ms == 5000
                      && sDef.SecsMdln == "NoCodeMotion" && sDef.SecsSoftRev == "1.0.0",
                      "S29 CommItem 的 10 个 SECS 默认值（T3=45s / T5=10s / T6=5s / T7=10s / T8=5s、自动应答开）");
                var sRole = new CommItem { SecsRole = "主动" };
                Check(sRole.SecsRole == "主动", "S30 SecsRole 可读写（「主动」= 主机端）");
            }
            catch (Exception ex)
            {
                Check(false, "SECS 编解码验证抛异常：" + ex.GetType().Name + " / " + ex.Message);
            }

            // ---------- 回环端到端（本环境不允许监听时只跳过这一段）----------
            bool sCanListen = false;
            try
            {
                var sProbe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
                sProbe.Start();
                sProbe.Stop();
                sCanListen = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("  SKIP  本环境不允许本机监听 TCP，跳过 SECS 回环端到端（" + ex.GetType().Name + "）");
            }

            if (sCanListen)
            {
                HsmsSession? sDev = null;
                HsmsSession? sHost = null;
                HsmsSession? sDev2 = null;
                try
                {
                    // ---------- 被动（设备端）监听 0 号端口 → 系统分配 ----------
                    sDev = new HsmsSession
                    {
                        Active = false, Port = 0, DeviceId = 0, AutoReply = true,
                        Mdln = "NCM-TEST", SoftRev = "9.9.9",
                        T3Ms = 4000, T5Ms = 300, T6Ms = 4000, T7Ms = 8000, T8Ms = 1000,
                    };
                    sDev.Open();
                    int sPort = sDev.BoundPort;
                    Check(sPort > 0 && sDev.IsListening, "S31 被动端监听成功，BoundPort = " + sPort);

                    // ---------- 主动（主机端）连上并 Select ----------
                    sHost = new HsmsSession
                    {
                        Active = true, Host = "127.0.0.1", Port = sPort, DeviceId = 1,
                        T3Ms = 4000, T5Ms = 1000, T6Ms = 4000, T7Ms = 8000, T8Ms = 1000,
                    };
                    sHost.Open();
                    Check(sHost.IsConnected && sHost.IsSelected,
                          "S32 主动端连上 127.0.0.1:" + sPort + " 并完成 Select（IsSelected = true）");
                    for (int i = 0; i < 200 && !sDev.IsSelected; i++) System.Threading.Thread.Sleep(10);
                    Check(sDev.IsSelected, "S33 被动端收到 Select.req 并回 Select.rsp（IsSelected = true）");

                    // ---------- S1F1 → 自动应答 S1F2 ----------
                    var sRsp2 = sHost.Send(SecsMessage.Data(1, 1, 1, true, SecsMessage.NewSystemBytes()), out string sE2);
                    Check(sRsp2 != null, "S34 S1F1 W 收到应答" + (sRsp2 == null ? "（失败：" + sE2 + "）" : string.Empty));
                    Check(sRsp2 != null && sRsp2.Stream == 1 && sRsp2.Function == 2 && !sRsp2.WBit,
                          "S35 应答是 S1F2（Stream=1 / Function=2 / 无 W-bit）");
                    Func<SecsItem, string> sTxt = it => (it == null || it.Raw == null) ? string.Empty : System.Text.Encoding.UTF8.GetString(it.Raw);
                    Check(sRsp2 != null && sRsp2.Root != null && sRsp2.Root.Children.Count == 2
                          && sRsp2.Root.Children[0].Format == SecsFormat.Ascii
                          && sRsp2.Root.Children[1].Format == SecsFormat.Ascii,
                          "S36 S1F2 数据体 = <L <A MDLN> <A SOFTREV>>");
                    Check(sRsp2 != null && sRsp2.Root != null && sRsp2.Root.Children.Count == 2
                          && sTxt(sRsp2.Root.Children[0]) == "NCM-TEST"
                          && sTxt(sRsp2.Root.Children[1]) == "9.9.9",
                          "S37 MDLN / SOFTREV 就是配置里填的值（NCM-TEST / 9.9.9）");

                    // ---------- S1F13 → S1F14 / S1F15 → S1F16 / S1F17 → S1F18 ----------
                    var sRsp13 = sHost.Send(SecsMessage.Data(1, 1, 13, true, SecsMessage.NewSystemBytes()), out string sE13);
                    Check(sRsp13 != null && sRsp13.Stream == 1 && sRsp13.Function == 14
                          && sRsp13.Root != null && sRsp13.Root.Children.Count == 2
                          && sRsp13.Root.Children[0].Format == SecsFormat.Binary
                          && sRsp13.Root.Children[0].Ints.Count == 1 && sRsp13.Root.Children[0].Ints[0] == 0,
                          "S38 S1F13 → S1F14 <L <B 0(COMMACK)> <L <A MDLN> <A SOFTREV>>>"
                          + (sRsp13 == null ? "（失败：" + sE13 + "）" : string.Empty));
                    var sRsp15 = sHost.Send(SecsMessage.Data(1, 1, 15, true, SecsMessage.NewSystemBytes()), out string sE15);
                    Check(sRsp15 != null && sRsp15.Function == 16 && sRsp15.Root != null
                          && sRsp15.Root.Format == SecsFormat.Binary,
                          "S39 S1F15（请求离线）→ S1F16 <B 0(OFFLACK)>" + (sRsp15 == null ? "（失败：" + sE15 + "）" : string.Empty));
                    var sRsp17 = sHost.Send(SecsMessage.Data(1, 1, 17, true, SecsMessage.NewSystemBytes()), out string sE17);
                    Check(sRsp17 != null && sRsp17.Function == 18 && sRsp17.Root != null
                          && sRsp17.Root.Format == SecsFormat.Binary,
                          "S40 S1F17（请求在线）→ S1F18 <B 0(ONLACK)>" + (sRsp17 == null ? "（失败：" + sE17 + "）" : string.Empty));

                    // ---------- 未请求消息：被动端上报 S6F11，主动端从收件队列取 ----------
                    var sEv = SecsMessage.Data(0, 6, 11, false, SecsMessage.NewSystemBytes(),
                                               SecsItem.L(SecsItem.U4(1), SecsItem.U4(1001), SecsItem.A("LOT01")));
                    var sEvRet = sDev.Send(sEv, out string sE18);
                    Check(sEvRet == null && sE18 == null, "S41 非 W-bit 消息发完即返回（不等应答、不报错）");
                    var sGot = sHost.Recv(3000);
                    Check(sGot != null && sGot.Stream == 6 && sGot.Function == 11,
                          "S42 主动端从收件队列收到被动端上报的 S6F11（未请求消息）");
                    Check(sGot != null && sGot.ToSmlLine().Contains("LOT01"),
                          "S43 S6F11 数据体完整：" + (sGot == null ? "(null)" : sGot.ToSmlLine()));
                    Check(sHost.Recv(200) == null, "S44 队列取空后再 Recv 超时返回 null（不阻塞、不重复）");

                    // ---------- Linktest 探活 ----------
                    Check(sHost.Linktest(), "S45 Linktest.req → Linktest.rsp（探活通过）");

                    // ---------- 收发计数 ----------
                    Check(sHost.SentCount >= 5 && sHost.ReceivedCount >= 2
                          && sDev.SentCount >= 2 && sDev.ReceivedCount >= 5,
                          "S46 收发计数非零（主动 sent=" + sHost.SentCount + " recv=" + sHost.ReceivedCount
                          + "，被动 sent=" + sDev.SentCount + " recv=" + sDev.ReceivedCount + "）");

                    // ---------- Separate.req 无应答：必须立即返回，不能死等 T6 ----------
                    var sSwSep = System.Diagnostics.Stopwatch.StartNew();
                    var sSepRet = sHost.Send(SecsMessage.Control(HsmsSType.SeparateReq, 0, SecsMessage.NewSystemBytes()), out string sE19);
                    sSwSep.Stop();
                    Check(sSepRet == null && sE19 == null && sSwSep.ElapsedMilliseconds < 1500,
                          "S47 ★ Separate.req 没有应答，发完立即返回（" + sSwSep.ElapsedMilliseconds
                          + " ms，不会白等 T6=" + sHost.T6Ms + " ms）");
                    for (int i = 0; i < 200 && sDev.IsConnected; i++) System.Threading.Thread.Sleep(10);
                    Check(!sDev.IsConnected, "S48 被动端收到 Separate.req 后主动断开连接");

                    // ---------- 非法帧长度必须断开 ----------
                    sDev2 = new HsmsSession { Active = false, Port = 0, DeviceId = 0, T7Ms = 8000, T8Ms = 800 };
                    sDev2.Open();
                    using (var sRaw = new System.Net.Sockets.TcpClient())
                    {
                        sRaw.Connect("127.0.0.1", sDev2.BoundPort);
                        var sNs = sRaw.GetStream();
                        for (int i = 0; i < 300 && !sDev2.IsConnected; i++) System.Threading.Thread.Sleep(10);
                        bool sWasConn = sDev2.IsConnected;
                        // 声明长度 4（< 10 字节头）→ 非法，必须断开
                        sNs.Write(new byte[] { 0, 0, 0, 4, 0, 0, 0, 0 }, 0, 8);
                        sNs.Flush();
                        for (int i = 0; i < 300 && sDev2.IsConnected; i++) System.Threading.Thread.Sleep(10);
                        Check(sWasConn && !sDev2.IsConnected,
                              "S49 ★ 非法帧长度（声明 4 < 头长 10）→ 会话立即断开，绝不把垃圾当报文");
                        Check(sDev2.LastError.Contains("非法帧长度"),
                              "S50 断开原因写明「非法帧长度」：" + sDev2.LastError);
                    }

                    // ---------- Close 幂等 ----------
                    sHost.Close();
                    sHost.Close();
                    Check(!sHost.IsConnected && !sHost.IsSelected, "S51 Close() 幂等，且状态复位（IsConnected / IsSelected 都 false）");
                    sDev.Close();
                    Check(!sDev.IsListening && !sDev.IsConnected, "S52 被动端 Close() 后停止监听");
                }
                catch (Exception ex)
                {
                    Check(false, "SECS 回环端到端抛异常：" + ex.GetType().Name + " / " + ex.Message);
                }
                finally
                {
                    try { sHost?.Close(); } catch { }
                    try { sDev?.Close(); } catch { }
                    try { sDev2?.Close(); } catch { }
                }
            }

            // ---------- 通讯页 SECS 参数卡：离屏实例化 + 可见性关系 + 截图 ----------
            try
            {
                var sPage = new NoCodeMotion.Views.CommPage();
                var sVm = sPage.DataContext as CommViewModel;
                Check(sVm != null, "S53 CommPage 的 DataContext 是 CommViewModel（XAML 解析通过）");
                if (sVm != null)
                {
                    Check(sVm.CommTypeOptions.Contains("SECS(HSMS)"), "S54 通讯类型下拉含 SECS(HSMS)");
                    Check(sVm.SecsPresets.Any(p => p == "S1F1 W") && sVm.SecsPresets.Count >= 8,
                          "S55 SML 预设齐备（" + sVm.SecsPresets.Count + " 条，含 S1F1 W）");

                    // ★ 不往共享集合里加（避免触发自动落盘）；XAML 只绑 SelectedItem.*，独立对象足够
                    var sItem = new CommItem
                    {
                        Name = "SECS1", CommType = "SECS(HSMS)", PortOrIp = "127.0.0.1", BaudOrPort = 5000
                    };
                    sVm.SelectedItem = sItem;

                    List<DependencyObject> sWalk(DependencyObject root)
                    {
                        var list = new List<DependencyObject>();
                        var stack = new Stack<DependencyObject>();
                        stack.Push(root);
                        int guard = 0;
                        while (stack.Count > 0 && guard++ < 60000)
                        {
                            var cur = stack.Pop();
                            list.Add(cur);
                            foreach (var c in Children(cur)) stack.Push(c);
                        }
                        return list;
                    }
                    System.Windows.Controls.Border? sCardOf(string head)
                    {
                        foreach (var tb in sWalk(sPage).OfType<System.Windows.Controls.TextBlock>())
                        {
                            if (tb.Text == null || !tb.Text.StartsWith(head, StringComparison.Ordinal)) continue;
                            for (var cur = VisualTreeHelper.GetParent(tb); cur != null; cur = VisualTreeHelper.GetParent(cur))
                                if (cur is System.Windows.Controls.Border bb) return bb;
                        }
                        return null;
                    }

                    sPage.Measure(new Size(1240, 900));
                    sPage.Arrange(new Rect(0, 0, 1240, 900));
                    sPage.UpdateLayout();

                    var sCard = sCardOf("SECS / HSMS 参数");
                    Check(sCard != null && sCard.Visibility == Visibility.Visible,
                          "S56 ★ 选中 SECS(HSMS) 时 SECS 参数卡真的可见（XAML 绑定 + 精确匹配转换器都对）");

                    sItem.CommType = "串口";
                    sPage.UpdateLayout();
                    var sCard2 = sCardOf("SECS / HSMS 参数");
                    Check(sCard2 != null && sCard2.Visibility != Visibility.Visible,
                          "S57 切回「串口」后 SECS 参数卡收起（与选中时形成对照）");

                    // 截图（切回 SECS 让卡片可见，留一张肉眼证据）
                    sItem.CommType = "SECS(HSMS)";
                    sPage.UpdateLayout();
                    string sOutDir = RepoSmokeOut() ?? Path.Combine(AppContext.BaseDirectory, "out");
                    Directory.CreateDirectory(sOutDir);
                    int sw3 = Math.Max(1, (int)Math.Ceiling(sPage.ActualWidth));
                    int sh3 = Math.Max(1, (int)Math.Ceiling(sPage.ActualHeight));
                    var sRtb2 = new RenderTargetBitmap(sw3, sh3, 96, 96, PixelFormats.Pbgra32);
                    sRtb2.Render(sPage);
                    var sEnc2 = new PngBitmapEncoder();
                    sEnc2.Frames.Add(BitmapFrame.Create(sRtb2));
                    string sPng = Path.Combine(sOutDir, "comm_secs_card.png");
                    using (var sFs2 = File.Create(sPng)) sEnc2.Save(sFs2);
                    Console.WriteLine("  截图：" + sPng);
                }
            }
            catch (Exception ex)
            {
                Check(false, "通讯页 SECS 卡片验证抛异常：" + ex.GetType().Name + " / " + ex.Message);
            }

            Console.WriteLine("\n====================  "
                              + (_fail == 0 ? "全部通过" : _fail + " 项失败")
                              + "  ====================");
            return _fail == 0 ? 0 : 1;
        }
    }
}
