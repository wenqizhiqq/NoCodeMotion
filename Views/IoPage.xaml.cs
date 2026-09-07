// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‌启‎志⁠◆‍编⁣写​◇⁠微⁠信‎﹕​1​8​7‏◆‏1‌9​3‍6‌◇⁣1‍3‎9‍9‏　⁣※‌保‍留⁣所​有‍权⁠利⁠请‍勿‌删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using NoCodeMotion.Services;
using NoCodeMotion.ViewModels;
using System.Windows.Controls;

namespace NoCodeMotion.Views
{
    public partial class IoPage : UserControl
    {
        public IoPage()
        {
            InitializeComponent();
            DataContext = new IoViewModel();
            // 仿真运行时 IO 状态变化 → 刷新"运行时"列高亮
            SimRuntime.Changed += OnSimChanged;
            Unloaded += (_, _) => SimRuntime.Changed -= OnSimChanged;
        }

        private void OnSimChanged()
        {
            if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke((System.Action)OnSimChanged); return; }
            InputGrid?.Items.Refresh();
            OutputGrid?.Items.Refresh();
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
