// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.ComponentModel;
using System.Windows;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// Apple 风格「正在连接控制器」进度弹窗：圆角白卡 + 不确定进度条 + 步骤文字。
    /// <para>打开后立即在后台启动连接动作（<paramref name="start"/>），并监听 VM 的
    /// <see cref="AxisControllerViewModel.IsConnecting"/>；连接结束（false）时自动关闭。
    /// 连接全程在后台线程执行，弹窗只负责显示进度，界面不会冻结。</para>
    /// </summary>
    public partial class ConnectProgressDialog : Window
    {
        private readonly Action _start;
        private readonly AxisControllerViewModel _vm;

        /// <param name="start">启动连接的动作（通常是 VM 的 Connect）。</param>
        /// <param name="vm">作为 DataContext 提供进度文字，并用于监听连接结束。</param>
        public ConnectProgressDialog(Action start, AxisControllerViewModel vm)
        {
            InitializeComponent();
            _start = start;
            _vm = vm;
            DataContext = vm;
            Owner = Application.Current?.MainWindow;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            _vm.PropertyChanged += OnVmPropertyChanged;
            _start();
            // 安全兜底：若连接动作未真正开始（IsConnecting 仍为 false），直接关闭避免弹窗卡住。
            if (!_vm.IsConnecting) Close();
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AxisControllerViewModel.IsConnecting) && !_vm.IsConnecting)
                Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
            base.OnClosed(e);
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
