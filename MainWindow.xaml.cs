// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启‌志‍◆‍编​写⁣◇‌微‎信‍﹕‌1‍8​7⁣◆⁠1‍9‏3⁠6‎◇⁠1‍3‍9‍9⁠　​※⁠保‏留⁠所‏有‌权‏利‌请​勿‎删‏除​◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NoCodeMotion.Views;
using NoCodeMotion.ViewModels;
using NoCodeMotion.Services;
using NoCodeMotion.Models;

namespace NoCodeMotion
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<string, System.Func<UserControl>> _pages = new()
        {
            ["ProjectManager"] = () => new ProjectManagerPage(),
            ["AxisController"] = () => new AxisControllerPage(),
            ["Axis"] = () => new AxisPage(),
            ["Io"] = () => new IoPage(),
            ["Cylinder"] = () => new CylinderPage(),
            ["Point"] = () => new PointPage(),
            ["Comm"] = () => new CommPage(),
            ["Tray"] = () => new TrayPage(),
            ["Variable"] = () => new VariablePage(),
            ["Flow"] = () => new FlowPage(),
            ["Camera"] = () => new CameraPage(), 
            ["Engineer"] = () => new EngineerPage(),
            ["Custom"] = () => new CustomDesignerPage(),
            ["Operator"] = () => new OperatorPage(),
            ["Manual"] = () => new OperatorManualPage(),
        };

        /// <summary>当前主窗口实例，供页面内（如工程工作台卡片）发起跨页导航。</summary>
        public static MainWindow? Instance { get; private set; }

        // 缓存已创建的页面，切换标签时保留各自的数据（已添加的轴/IO 等不丢失）
        private readonly Dictionary<string, UserControl> _cache = new();

        private Button? _selectedNav;

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            LoadingService.StateChanged += OnLoadingStateChanged;
            ProjectManager.DataReloaded += OnProjectDataReloaded;
            // 打开工程 / 新建模板时，在加载遮罩里把所有页面重建一遍（详见 RebuildPagesForReloadAsync）
            ProjectManager.ReloadPagesAsync = RebuildPagesForReloadAsync;
            StatusBarService.SetProject(ProjectManager.CurrentName ?? "未打开工程");
            StatusBarService.RefreshUser();
            // 底栏署名（AuthorWatermark 是 internal，作者联系串在源码里被拆段+零宽混淆，
            // 即便有人用整段字符串批量替换也无法一次抹掉。本字段仅在 InitializeComponent 之后可用） 
            // 启动初始化（载入工程 + 预初始化所有页面）在窗口显示后进行 —— 全程在加载遮罩的
            // 进度条 + 当前步骤文字下完成，用户不会看到一段「白屏无提示」的等待。
            Loaded += OnWindowLoaded;
            Closing += MainWindow_Closing;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnWindowLoaded;
            StartUpAsync();
        }

        private bool _shuttingDown;

        /// <summary>启动预初始化进行中：此时载入工程不额外逐页重建（StartUpAsync 自己会建一遍）。</summary>
        private bool _startupRunning;

        // ★ 关闭软件：先拦下关闭，弹进度条一步步释放资源（停止流程 + 关闭控制器 + 保存工程），
        // 完成后再真正退出；避免直接关窗口导致运动卡 / 流程残留、工程未保存。
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_shuttingDown) return;            // 第二次进入（Shutdown 触发）→ 放行
            e.Cancel = true;                       // 先拦下，走进度条释放流程
            _shuttingDown = true;

            var dlg = new ClosingProgressWindow { Owner = this };
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    dlg.Report(5, "停止运行中的流程…");
                    AppShutdown.StopFlows();        // 只停流程，先不动硬件（退出前流程还要驱动轴 / IO）
                    System.Threading.Thread.Sleep(200);  // 让停止信号传播到硬件等待
                    RunBeforeExitFlows(dlg);        // 单次执行「退出前执行」流程（最多 30 秒）
                    dlg.Report(70, "保存工程配置…");
                    ProjectStore.Save();
                    dlg.Report(85, "断开并关闭控制器…");
                    AppShutdown.ReleaseControllers();
                    System.Threading.Thread.Sleep(120);
                    dlg.Report(100, "完成，正在退出…");
                    System.Threading.Thread.Sleep(150);
                }
                catch (System.Exception ex)
                {
                    dlg.Report(100, "退出时发生错误：" + ex.Message);
                }
                finally
                {
                    dlg.Dispatcher.Invoke(() => dlg.Close());
                }
            });
            dlg.ShowDialog();
            System.Windows.Application.Current.Shutdown();
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string key)
                Navigate(key, btn);
        }

        /// <summary>供页面内（如工程工作台卡片）按模块键跳转并高亮对应导航按钮。</summary>
        public void NavigateTo(string key)
        {
            if (!_pages.ContainsKey(key)) return;
            Navigate(key, FindNavButton(key));
        }

        private Button? FindNavButton(string key)
            => NavPanel.Children.OfType<Button>().FirstOrDefault(b => b.Tag as string == key);

        /// <summary>工程数据被「打开/新建」原地替换后，清空页面缓存并重建当前页，
        /// 使各页面 ViewModel 以最新工程数据重新构造（修复选中项/计数残留）。
        /// <para>打开工程 / 新建模板时加载遮罩还在，逐页重建交给 <see cref="RebuildPagesForReloadAsync"/>，
        /// 这样轴页这类大页面的构造时间落在进度条里，而不是用户第一次切过去时。</para></summary>
        private void OnProjectDataReloaded()
        {
            StatusBarService.SetProject(ProjectManager.CurrentName ?? "未打开工程");
            if (LoadingService.IsLoading) return;
            var key = _selectedNav?.Tag as string;
            _cache.Clear();
            if (key != null) NavigateTo(key);
        }

        /// <summary>打开工程 / 新建模板后（加载遮罩仍可见）逐页重建页面。
        /// 每建一页就让出一次 UI 线程，让进度条和步骤文字真正走起来。</summary>
        private async System.Threading.Tasks.Task RebuildPagesForReloadAsync()
        {
            // 启动时的预初始化由 StartUpAsync 自己逐页完成（它有一套 n 页 + 2 步的进度）
            if (_startupRunning) return;

            var keys = _pages.Keys.ToList();
            int total = keys.Count;
            int done = 0;
            var currentKey = _selectedNav?.Tag as string;

            _cache.Clear();
            LoadingService.ProgressMax = total + 1;
            foreach (var key in keys)
            {
                LoadingService.Report(++done, $"正在刷新页面 ({done}/{total})：{TitleFor(key)}…");
                await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
                try
                {
                    _cache[key] = _pages[key]();
                }
                catch (System.Exception ex)
                {
                    // 单页构造失败不能拖垮整次载入（例如某页 XAML 运行时异常）
                    StatusBarService.ReportException($"页面「{TitleFor(key)}」初始化失败：{ex.Message}");
                }
            }

            LoadingService.Report(Math.Min(++done, LoadingService.ProgressMax), "正在进入主界面…");
            await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
            NavigateTo(currentKey ?? "Flow");
        }

        private void Navigate(string key, Button? btn)
        {
            // 页面切换即时完成，不显示加载遮罩（进度条仅用于打开/新建工程）。
            if (!_cache.TryGetValue(key, out var page))
            {
                page = _pages[key]();
                _cache[key] = page;
            }
            PageHost.Content = page;
            FinishNavigate(page, key, btn);
        }

        /// <summary>页面装好后的公共收尾：默认选中项 + 导航高亮。</summary>
        private void FinishNavigate(UserControl page, string key, Button? btn)
        {
            // 切换页面后，若页面内尚无选中项，则默认选中第一项
            if (page.DataContext is IEnsureDefaultSelection eds)
                eds.EnsureDefaultSelection();

            if (_selectedNav != null)
            {
                _selectedNav.Background = System.Windows.Media.Brushes.Transparent;
                _selectedNav.Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush");
            }

            _selectedNav = btn ?? FindNavButton(key);
            ShowPageGuide(key);

            if (_selectedNav != null)
            {
                _selectedNav.Background = (System.Windows.Media.Brush)FindResource("NavActiveBrush");
                _selectedNav.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
            }
        }

        /// <summary>加载遮罩可见性 / 文本 / 进度随 LoadingService 状态切换（打开/新建工程、启动预初始化时显示）。</summary>
        private void OnLoadingStateChanged()
        {
            LoadingOverlay.Visibility = LoadingService.IsLoading ? Visibility.Visible : Visibility.Collapsed;
            LoadingText.Text = LoadingService.Message;
            if (LoadingService.Progress < 0)
            {
                LoadingBar.IsIndeterminate = true;
            }
            else
            {
                LoadingBar.IsIndeterminate = false;
                LoadingBar.Maximum = LoadingService.ProgressMax;
                LoadingBar.Value = Math.Min(LoadingService.Progress, LoadingService.ProgressMax);
            }
        }

        /// <summary>
        /// 底部「本页怎么用」说明栏：按页签取白话文案。没配文案的页签直接隐藏，不留空白条。
        /// </summary>
        private void ShowPageGuide(string key)
        {
            var g = PageGuideCatalog.For(key);
            if (g == null)
            {
                PageGuide.Visibility = System.Windows.Visibility.Collapsed;
                return;
            }
            PageGuide.OperationText = g.Operation;
            PageGuide.PrecautionText = g.Precaution;
            PageGuide.Visibility = System.Windows.Visibility.Visible;
        }

        /// <summary>导航键对应的中文页签名，用于预初始化提示文案。</summary>
        private static string TitleFor(string key) => key switch
        {
            "ProjectManager" => "项目管理",
            "AxisController" => "控制器",
            "Axis" => "轴",
            "Io" => "IO",
            "Cylinder" => "气缸",
            "Point" => "点位表",
            "Comm" => "通讯",
            "Tray" => "料盘",
            "Variable" => "变量",
            "Flow" => "流程",
            "Camera" => "相机",
            "Engineer" => "工程师",
            "Custom" => "自定义",
            "Operator" => "操作员",
            "Manual" => "说明书",
            _ => key
        };

        /// <summary>
        /// 启动初始化：全程在加载遮罩下、用**确定式进度条 + 当前步骤文字**显示初始化内容。
        ///   ① 载入上次工程（xlsx 读取在后台线程，遮罩可见）
        ///   ② 逐页预初始化（第 i / 共 n 页：页签名）
        ///   ③ 进入默认「流程」页
        /// 页面全部构造进缓存后，后续切换瞬时完成、不再需要进度条。
        /// </summary>
        private async void StartUpAsync()
        {
            _startupRunning = true;
            var keys = _pages.Keys.ToList();
            int total = keys.Count;
            int steps = total + 2;          // 载入工程 + n 页 + 收尾
            LoadingService.ProgressMax = steps;
            LoadingService.Show("正在启动…");
            // 先让遮罩渲染出来，再开始干活
            await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);

            int done = 0;
            try
            {
                // ① 载入上次工程（所有页面参数都保存在工程里）。
                LoadingService.Report(++done, "正在载入上次工程…（读取轴 / IO / 气缸 / 点位 / 流程 / 控制器参数）");
                await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);

                var last = ProjectManager.LoadLastProject();
                if (last != null && ProjectManager.Exists(last))
                {
                    // OpenProjectAsync：xlsx 读取放后台线程，LoadInto 回 UI 线程（遮罩全程可见）
                    await ProjectManager.OpenProjectAsync(last);
                }
                else
                {
                    LoadingService.Report(++done, "未找到工程，载入默认参数…");
                    await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
                    ProjectStore.Load();
                }
                StatusBarService.SetProject(ProjectManager.CurrentName ?? "未打开工程");

                // ② 逐页预初始化
                foreach (var key in keys)
                {
                    LoadingService.Report(Math.Min(++done, LoadingService.ProgressMax),
                        $"正在初始化页面 ({done - 2}/{total})：{TitleFor(key)}…");
                    await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
                    if (!_cache.ContainsKey(key))
                        _cache[key] = _pages[key]();
                }

                // ③ 默认进入「流程」页（已在缓存中，瞬时切换）
                LoadingService.Report(Math.Min(++done, LoadingService.ProgressMax), "正在进入主界面…");
                await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
                NavigateTo("Flow");

                // ④ 工程文件「外部改动自动载入」：放在启动流程最后开，
                //    免得启动期间的读盘 / 首次保存被当成外部改动（watcher 自己也会屏蔽软件写盘）。
                ProjectFileWatcher.Start();
            }
            catch (System.Exception ex)
            {
                StatusBarService.ReportException("启动初始化失败：" + ex.Message);
            }
            finally
            {
                _startupRunning = false;
                LoadingService.Hide();
                // 遮罩隐藏后：等「载入工程 → 自动连接控制卡」收尾，再启动「开机循环」流程，
                // 避免流程在卡还没连上时就去驱动轴 / IO。
                StartStartupFlows();
            }
        }

        /// <summary>开机循环：工程载入完成后，自动把 Role=StartupLoop 的流程按「循环运行」启动。
        /// 只在启动时执行一次；用户之后在操作员页点【停止】即结束，不会自动重启。
        /// 异步等待自动连接控制卡收尾（最多 10 秒），避免流程先于硬件连接启动。</summary>
        private async void StartStartupFlows()
        {
            try
            {
                var flows = ProjectStore.Data?.Flows?
                    .Where(f => f.Role == FlowRole.StartupLoop)
                    .ToList();
                if (flows == null || flows.Count == 0) return;

                // 先让出一次，等遮罩隐藏触发的「自动连接」真正开始（否则这里读到的 Connecting 还是 false）
                await System.Threading.Tasks.Task.Delay(300);
                for (int i = 0; i < 100 && StatusBarService.ControllerConnecting; i++)
                    await System.Threading.Tasks.Task.Delay(100);
                if (_shuttingDown) return;   // 等待期间用户已关闭软件

                foreach (var f in flows)
                {
                    try { FlowLoopManager.StartLoop(f); }
                    catch (System.Exception ex) { StatusBarService.ReportException($"开机循环「{f.Name}」启动失败：{ex.Message}"); }
                }
                StatusBarService.ReportInfo($"开机循环：已启动 {flows.Count} 条流程（{string.Join("、", flows.Select(f => f.Name))}）。");
            }
            catch (System.Exception ex)
            {
                StatusBarService.ReportException("开机循环启动失败：" + ex.Message);
            }
        }

        /// <summary>退出前执行：Role=BeforeExit 的流程单次跑完（最多等 30 秒），完成后才保存工程 / 断开控制器。
        /// 在关闭进度条的后台线程里执行，不阻塞 UI。</summary>
        private static void RunBeforeExitFlows(ClosingProgressWindow dlg)
        {
            var flows = ProjectStore.Data?.Flows?
                .Where(f => f.Role == FlowRole.BeforeExit)
                .ToList();
            if (flows == null || flows.Count == 0) return;

            const int timeoutSec = 30;
            dlg.Report(10, $"执行退出前流程（{flows.Count} 条）：{string.Join("、", flows.Select(f => f.Name))}…");

            var ctrl = new FlowRunControl();
            ctrl.InitVars();
            using var done = new System.Threading.ManualResetEventSlim(false);
            FlowRunnerService.RunAllAsync(
                ctrl,
                (m, k) => StatusBarService.ReportInfo(m),
                (idx, name, step) => { },
                (idx, name) => { },
                () => done.Set(),
                filter: f => f.Role == FlowRole.BeforeExit,
                forceLoop: false);

            int waited = 0;
            while (!done.Wait(1000) && waited < timeoutSec)
            {
                waited++;
                dlg.Report(10 + waited, $"执行退出前流程…（已等待 {waited}s / 最多 {timeoutSec}s）");
            }
            if (!done.IsSet)
            {
                ctrl.StopRequested = true;
                ctrl.ResumeEvent.Set();
                done.Wait(1500);   // 留一点时间让流程线程从等待里退出，再断开硬件
                StatusBarService.ReportException($"退出前流程超过 {timeoutSec}s 未结束，已强制中止。");
            }
            // 退出前流程写过的变量落回工程表，随后的 ProjectStore.Save() 才会保存它们
            try { ctrl.WriteBackVars(); } catch { }
        }

        /// <summary>导航栏「在线下发」开关：开启后配置页改值实时下发到设备（仅真实硬件桥生效）。</summary>
        private void OnlinePushChk_Checked(object sender, RoutedEventArgs e) => HardwarePush.Online = true;
        private void OnlinePushChk_Unchecked(object sender, RoutedEventArgs e) => HardwarePush.Online = false;
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
