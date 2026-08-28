// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 变量快照节点：名称 / 值 / 类型，表类型可展开子节点。
    /// 快照在 Lua 线程挂起时构建，之后可安全地在 UI 线程使用。
    /// </summary>
    public sealed class VarInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = "nil";
        public string TypeName { get; set; } = "nil";
        public string Scope { get; set; } = string.Empty;   // 局部 / 全局 / 上值
        public bool IsExpanded { get; set; }
        public ObservableCollection<VarInfo> Children { get; } = new ObservableCollection<VarInfo>();

        public bool HasChildren => Children.Count > 0;

        /// <summary>用于提示框和变量面板显示的一行摘要。</summary>
        public string Display => $"{Name} = {Value}";

        public override string ToString() => Display;
    }

    /// <summary>脚本挂起（命中断点 / 单步停下 / 运行时错误）时的完整现场。</summary>
    public sealed class PauseInfo
    {
        public int Line { get; set; }
        public bool IsError { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<VarInfo> Locals { get; set; } = new List<VarInfo>();
        public List<VarInfo> Globals { get; set; } = new List<VarInfo>();
        public List<string> CallStack { get; set; } = new List<string>();
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
