// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‏启⁣志⁣◆‎编⁠写‎◇‏微⁠信‎﹕​1‎8⁠7‌◆⁣1⁠9⁣3⁠6‌◇⁠1‍3‌9‍9⁣　⁣※‍保‏留​所‍有‎权⁠利‏请‌勿‏删⁣除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Windows;
using System.Windows.Controls;
using NoCodeMotion.ViewModels;
using NoCodeMotion.Services;

namespace NoCodeMotion.Views
{
    public partial class OperatorPage : UserControl
    {
        public OperatorPage()
        {
            InitializeComponent();
            DataContext = new OperatorViewModel();
            // 仅当本页可见时才跑 33ms 仿真循环；导航到其它页/最小化时停止，省出 UI 线程给其它页面。
            if (DataContext is OperatorViewModel vm)
            {
                IsVisibleChanged += (_, __) => vm.SetSimVisible(IsVisible);
                vm.SetSimVisible(IsVisible);
            }
        }

        /// <summary>2D/3D 仿真切换：Checked 在 InitializeComponent 解析期触发时宿主元素尚未建好，判空跳过（初始可见性由 XAML 设定）。</summary>
        private void SimMode_Changed(object sender, RoutedEventArgs e)
        {
            if (Sim2DHost == null || Sim3DHost == null) return;
            bool is2D = RbSim2D?.IsChecked == true;
            Sim2DHost.Visibility = is2D ? Visibility.Visible : Visibility.Collapsed;
            Sim3DHost.Visibility = is2D ? Visibility.Collapsed : Visibility.Visible;
            // 轴IO标定 / 生成3D模型 仅对 3D 仿真有意义，2D 模式下隐藏
            var ioBtnVis = is2D ? Visibility.Collapsed : Visibility.Visible;
            if (BtnAxisIoCalib != null) BtnAxisIoCalib.Visibility = ioBtnVis;
            if (BtnBuildFromIo != null) BtnBuildFromIo.Visibility = ioBtnVis;
        }

        /// <summary>切换用户：列出真实用户库（UserStore）中的全部用户，确定后切换当前登录用户。</summary>
        private void SwitchUser_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SwitchUserDialog();
            // ★ Owner 只能是「已显示过且不是自己」的窗口：Application.MainWindow 在离屏 / 冒烟场景
            //   可能是自己或尚未显示，直接 ShowDialog 会抛「无法将 Owner 设置为之前未显示的 Window」。
            var owner = Window.GetWindow(this);
            if (owner is not null && !ReferenceEquals(owner, dlg) && owner.IsVisible) dlg.Owner = owner;

            if (dlg.ShowDialog() != true) return;
            string name = dlg.SelectedName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return;

            if (DataContext is OperatorViewModel vm) vm.SwitchToUser(name);
            StatusBarService.RefreshUser();
            StatusBarService.ReportInfo("已切换用户：" + name);
        }

        /// <summary>用户设置：新增 / 重命名 / 删除用户并设置角色。关闭后同步当前用户（可能被改名或删除）。</summary>
        private void UserSettings_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new UserManagementDialog();
            var owner = Window.GetWindow(this);
            if (owner is not null && !ReferenceEquals(owner, dlg) && owner.IsVisible) dlg.Owner = owner;

            dlg.ShowDialog();
            if (DataContext is OperatorViewModel vm) vm.RefreshUser();
            StatusBarService.RefreshUser();
        }

        /// <summary>轴IO标定：打开绑定对话框，保存后立即按新绑定重建 3D 机台。</summary>
        private void BtnAxisIoCalib_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AxisIoCalibDialog { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;
            ProjectStore.ScheduleSave();
            BuildFromAxisIo();
        }

        /// <summary>按当前轴配置与标定 IO 重建 3D 机台。</summary>
        private void BtnBuildFromIo_Click(object sender, RoutedEventArgs e) => BuildFromAxisIo();

        private void BuildFromAxisIo()
        {
            // 当前在 2D 时切到 3D，让生成结果立即可见
            if (RbSim2D?.IsChecked == true) RbSim3D.IsChecked = true;
            Sim3DHost.RebuildFromAxisIo();
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
