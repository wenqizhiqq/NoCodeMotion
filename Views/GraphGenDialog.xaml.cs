// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‎启‍志⁣◆‍编‎写‍◇⁠微‏信⁣﹕‎1‏8‍7⁠◆⁣1​9​3⁠6‍◇⁣1‎3⁠9⁣9‎　​※​保‏留​所⁠有‏权⁣利‏请‍勿‍删‌除‌◇​⁣​
using System.Windows;
using System.Windows.Input;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 图像生成点位对话框：可导入一张底图，在图上单击描点（折线模式下连成折线），
    /// 按「框尺寸 + 起点偏移」映射为机器坐标点位（轴1←X、轴2←Y），确认后追加到当前工位。
    /// DataContext = <see cref="GraphPointGenViewModel"/>。
    /// </summary>
    public partial class GraphGenDialog : Window
    {
        public GraphGenDialog()
        {
            InitializeComponent();

            // 取宿主主窗口做 Owner；但若本窗口恰好是应用里第一个窗口，
            // Application.MainWindow 会返回它自己 —— 直接赋值会抛「不能把 Owner 设为自己」。
            var owner = Application.Current?.MainWindow;
            if (owner is not null && !ReferenceEquals(owner, this)) Owner = owner;
        }

        /// <summary>画布单击：把鼠标位置（画布坐标）交给 ViewModel 添加一个点位。</summary>
        private void GraphCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not GraphPointGenViewModel vm) return;
            var pos = e.GetPosition((IInputElement)sender);
            vm.AddPoint(pos.X, pos.Y);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // 只有画布上有点位时按钮才可点（IsEnabled 绑 HasPoints）；
            // 这里显式调一次生成，再由 PointViewModel 的 Generated 订阅把点位追加到当前工位。
            if (DataContext is GraphPointGenViewModel vm)
                vm.GenerateCommand.Execute(null);

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
