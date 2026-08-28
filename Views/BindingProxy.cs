// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Windows;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 标准 WPF BindingProxy（Freezable）：让 ResourceDictionary 里的资源能拿到宿主 DataContext，
    /// 进而把宿主 FlowViewModel 的 SelectedItem 传给同在 Resources 里的 VisualFlowDetailViewModel。
    /// 使用：&lt;local:BindingProxy x:Key="FlowProxy" Data="{Binding}"/&gt;  ——  解析为宿主页的 DataContext（FlowViewModel）。
    /// </summary>
    public class BindingProxy : Freezable
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy));

        public object? Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        protected override Freezable CreateInstanceCore() => new BindingProxy();
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
