// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‏启‌志​◆​编‍写⁠◇‌微‏信‌﹕⁣1‍8​7⁣◆‍1‏9‎3‌6‎◇‌1‎3⁠9⁠9‎　‎※‍保‍留⁣所‌有⁠权‍利‍请‏勿⁣删⁠除‍◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Windows;
using System.Windows.Controls;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    public partial class PointPage : UserControl
    {
        private readonly PointViewModel _vm;

        public PointPage()
        {
            InitializeComponent();
            _vm = new PointViewModel();
            DataContext = _vm;

            // 「移动条件」的「实际值」列要显示设备实时状态，靠 ViewModel 里的定时器周期刷新。
            // 只在页面真正显示时开，切走就停，避免不可见的页面一直空转。
            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e) => _vm.StartLiveRefresh();

        private void OnPageUnloaded(object sender, RoutedEventArgs e) => _vm.StopLiveRefresh();
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
