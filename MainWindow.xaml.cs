using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NoCodeMotion.Views;

namespace NoCodeMotion
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<string, System.Func<UserControl>> _pages = new()
        {
            ["Axis"] = () => new AxisPage(),
            ["Io"] = () => new IoPage(),
            ["Cylinder"] = () => new CylinderPage(),
            ["Comm"] = () => new CommPage(),
            ["Tray"] = () => new TrayPage(),
            ["Flow"] = () => new FlowPage(),
        };

        // 缓存已创建的页面，切换标签时保留各自的数据（已添加的轴/IO 等不丢失）
        private readonly Dictionary<string, UserControl> _cache = new();

        private Button? _selectedNav;

        public MainWindow()
        {
            InitializeComponent();
            var first = NavPanel.Children.OfType<Button>().FirstOrDefault();
            if (first != null) Navigate("Axis", first);
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string key)
                Navigate(key, btn);
        }

        private void Navigate(string key, Button btn)
        {
            if (!_cache.TryGetValue(key, out var page))
            {
                page = _pages[key]();
                _cache[key] = page;
            }
            PageHost.Content = page;

            if (_selectedNav != null)
                _selectedNav.Background = System.Windows.Media.Brushes.Transparent;

            _selectedNav = btn;
            _selectedNav.Background = (System.Windows.Media.Brush)FindResource("NavActiveBrush");
        }
    }
}
