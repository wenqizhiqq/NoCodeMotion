// 仓库内冒烟工程（.smoke\）：主工程已 <Compile Remove=".smoke\**"/>，不会互相干扰。
// 跑法（仓库根目录）：
//   dotnet build .smoke/ncm_smoke.csproj -p:UseAppHost=false
//   dotnet exec .smoke/bin/Debug/net10.0-windows10.0.19041.0/ncm_smoke.dll
// ★ 输出目录随目标框架走（主工程已升到 net10.0-windows10.0.19041.0，别再跑老的 net10.0-windows 副本）。
// ★ 要让 OpenCvSharp 原生库可加载（其依赖 opencv_world480.dll 在 dotnet exec 下不在搜索路径），
//   把输出目录加进 PATH 再跑：PATH="<输出目录>:$PATH" dotnet exec <dll>；
//   否则段 H 诊断项、段 M 的 OCR 端到端、段 N 的「相机回退」会跳过（不影响其余断言）。
// 段 N：图像采集来源 —— 默认「相机」/ 相机名解析 / 文件或文件夹路径无效时明确报错（不静默出测试图）。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Camera;
using NoCodeMotion.Services.Vision;
using NoCodeMotion.ViewModels;

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

            Console.WriteLine("\n====================  "
                              + (_fail == 0 ? "全部通过" : _fail + " 项失败")
                              + "  ====================");
            return _fail == 0 ? 0 : 1;
        }
    }
}
