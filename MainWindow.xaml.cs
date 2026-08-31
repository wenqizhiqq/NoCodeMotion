// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
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
            ProjectManager.DataReloaded += OnProjectDataReloaded;
            StatusBarService.SetProject(ProjectManager.CurrentName ?? "未打开工程");
            StatusBarService.RefreshUser();
            // 默认打开「流程」页面
            NavigateTo("Flow");
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
            if (!_cache.TryGetValue(key, out var page))
            {
                page = _pages[key]();
                _cache[key] = page;
            }
            PageHost.Content = page;

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

        /// <summary>导航栏「在线下发」开关：开启后配置页改值实时下发到设备（仅真实硬件桥生效）。</summary>
        private void OnlinePushChk_Checked(object sender, RoutedEventArgs e) => HardwarePush.Online = true;
        private void OnlinePushChk_Unchecked(object sender, RoutedEventArgs e) => HardwarePush.Online = false;
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
