// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Windows.Controls;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 流程页：左侧列表管理“流程”项目，右侧按选中流程的 Kind 切换表格步骤 / Lua 脚本。
    /// Lua 脚本的完整编辑器（AvalonEdit + 断点边栏 + 智能提示 + 单步/步入/步出/运行/暂停/停止 + 变量树 + 调用栈 + 输出 + 诊断）
    /// 由 <see cref="LuaEditorView"/> 承载（直接复用 LuaStudio 的代码），本文件只负责把页面挂到 ViewModel。
    /// </summary>
    public partial class FlowPage : UserControl
    {
        public FlowPage()
        {
            InitializeComponent();
            DataContext = new FlowViewModel();
        }
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
