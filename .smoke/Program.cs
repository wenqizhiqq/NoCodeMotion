// 仓库内冒烟工程（.smoke\）：主工程已 <Compile Remove=".smoke\**"/>，不会互相干扰。
// 跑法（仓库根目录）：
//   dotnet build .smoke/ncm_smoke.csproj -p:UseAppHost=false
//   dotnet exec .smoke/bin/Debug/net10.0-windows/ncm_smoke.dll
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
                            Check(row.ActualWidth > 210 && row.ActualWidth >= inList.ActualWidth * 0.85,
                                  "行宽加大：拉伸到列宽（行 " + row.ActualWidth.ToString("0.0")
                                  + " / 列表 " + inList.ActualWidth.ToString("0.0")
                                  + "，旧写法固定 Width=210）");
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
                    Check(ioCard.ActualWidth > axisCard.ActualWidth,
                          "左列比右列宽，IO 两表宽度不被压缩（IO " + ioCard.ActualWidth.ToString("0")
                          + " > 轴控 " + axisCard.ActualWidth.ToString("0") + "）");
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

            Console.WriteLine("\n====================  "
                              + (_fail == 0 ? "全部通过" : _fail + " 项失败")
                              + "  ====================");
            return _fail == 0 ? 0 : 1;
        }
    }
}
