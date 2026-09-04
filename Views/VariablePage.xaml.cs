// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using NoCodeMotion.Services;
using NoCodeMotion.ViewModels;
using System.Windows.Controls;

namespace NoCodeMotion.Views
{
    public partial class VariablePage : UserControl
    {
        public VariablePage()
        {
            InitializeComponent();
            DataContext = new VariableViewModel();
            // 仿真运行时变量变化 → 刷新"解析值"列（表达式随依赖变量实时更新）
            SimRuntime.Changed += OnSimChanged;
            Unloaded += (_, _) => SimRuntime.Changed -= OnSimChanged;
        }

        private void OnSimChanged()
        {
            if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke((System.Action)OnSimChanged); return; }
            VarGrid?.Items.Refresh();
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
