// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Windows;
using System.Windows.Controls;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 轴页：左侧选轴 + 右侧参数卡 + 底部「轴状态与控制」表。
    /// 状态表靠 300ms 轮询底层真实读数，所以只在页面可见时开启轮询，离开页面就停。
    /// </summary>
    public partial class AxisPage : UserControl
    {
        public AxisPage()
        {
            InitializeComponent();
            DataContext = _vm = new AxisViewModel();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private readonly AxisViewModel _vm;

        private void OnLoaded(object sender, RoutedEventArgs e) => _vm.SetStatusRefreshEnabled(true);

        private void OnUnloaded(object sender, RoutedEventArgs e) => _vm.SetStatusRefreshEnabled(false);
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
