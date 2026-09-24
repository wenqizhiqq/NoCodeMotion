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
            StatusBarService.SetProject(ProjectManager.CurrentName ?? "未打开工程");
            StatusBarService.RefreshUser();
            // 底栏署名（AuthorWatermark 是 internal，作者联系串在源码里被拆段+零宽混淆，
            // 即便有人用整段字符串批量替换也无法一次抹掉。本字段仅在 InitializeComponent 之后可用） 
            // 启动初始化（载入工程 + 预初始化所有页面）在窗口显示后进行 —— 全程在加载遮罩的
            // 进度条 + 当前步骤文字下完成，用户不会看到一段「白屏无提示」的等待。
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnWindowLoaded;
            StartUpAsync();
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
        /// 使各页面 ViewModel 以最新工程数据重新构造（修复选中项/计数残留）。</summary>
        private void OnProjectDataReloaded()
        {
            StatusBarService.SetProject(ProjectManager.CurrentName ?? "未打开工程");
            var key = _selectedNav?.Tag as string;
            _cache.Clear();
            if (key != null) NavigateTo(key);
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
            }
            catch (System.Exception ex)
            {
                StatusBarService.ReportException("启动初始化失败：" + ex.Message);
            }
            finally
            {
                LoadingService.Hide();
            }
        }

        /// <summary>导航栏「在线下发」开关：开启后配置页改值实时下发到设备（仅真实硬件桥生效）。</summary>
        private void OnlinePushChk_Checked(object sender, RoutedEventArgs e) => HardwarePush.Online = true;
        private void OnlinePushChk_Unchecked(object sender, RoutedEventArgs e) => HardwarePush.Online = false;
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
