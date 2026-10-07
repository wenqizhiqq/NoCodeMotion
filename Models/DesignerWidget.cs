// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
// ◆温​启‍志​◆⁠编⁠写​◇‎微‎信​﹕‌1‍8​7‍◆‍1‌9⁠3‍6‍◇​1‌3‌9⁠9‌　​※​保‍留‍所‍有‌权‎利⁣请⁠勿‌删‍除‌◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
namespace NoCodeMotion.Models
{
    /// <summary>
    /// 自定义页面（可视化设计器）里的一个控件：用户从左侧工具箱拖到工作区，
    /// 拖动位置 / 大小，右侧属性面板设置参数。随工程一起存进 xlsx 的「自定义」表。
    /// <para>三种控件：<c>按钮</c>（点击执行动作：写 IO / 气缸 / 轴移动 / 写变量…）、
    /// <c>输入框</c>（把输入值写进变量）、<c>显示框</c>（定时读回并显示 IO / 气缸 / 变量 / 轴的值）。</para>
    /// </summary>
    public class DesignerWidget : EditorItemBase
    {
        private string _widgetType = "按钮";   // 按钮 / 输入框 / 显示框
        private double _x;
        private double _y;
        private double _width = 130;
        private double _height = 40;
        private string _text = string.Empty;   // 按钮文字 / 输入框标签 / 显示框标题
        private string _action = "无";          // 动作（按钮/输入框）或显示内容（显示框）
        private string _target = string.Empty;  // 目标名：IO 名 / 气缸名 / 变量名 / 轴名
        private string _param = string.Empty;   // 参数：开/关/切换、伸出/缩回、目标位置、变量值…

        /// <summary>控件类型：按钮 / 输入框 / 显示框。</summary>
        public string WidgetType
        {
            get => _widgetType;
            set => SetField(ref _widgetType, value);
        }

        /// <summary>工作区内 X 坐标（px）。</summary>
        public double X
        {
            get => _x;
            set => SetField(ref _x, value < 0 ? 0 : value);
        }

        /// <summary>工作区内 Y 坐标（px）。</summary>
        public double Y
        {
            get => _y;
            set => SetField(ref _y, value < 0 ? 0 : value);
        }

        /// <summary>宽度（px）。</summary>
        public double Width
        {
            get => _width;
            set => SetField(ref _width, value < 40 ? 40 : value);
        }

        /// <summary>高度（px）。</summary>
        public double Height
        {
            get => _height;
            set => SetField(ref _height, value < 24 ? 24 : value);
        }

        /// <summary>按钮文字 / 输入框上方标签 / 显示框标题（空则用控件名）。</summary>
        public string Text
        {
            get => _text;
            set => SetField(ref _text, value);
        }

        /// <summary>
        /// 按钮 / 输入框：要执行的动作（如 写输出IO / 气缸 / 轴移动(绝对) / 写变量…）；
        /// 显示框：要显示的内容（显示IO / 显示气缸 / 显示变量 / 显示轴位置 / 显示轴参数）。
        /// </summary>
        public string Action
        {
            get => _action;
            set => SetField(ref _action, value);
        }

        /// <summary>动作 / 显示的目标对象名（输出 IO 名、气缸名、变量名或轴名）。</summary>
        public string Target
        {
            get => _target;
            set => SetField(ref _target, value);
        }

        /// <summary>
        /// 动作参数：写输出IO → 开/关/切换；气缸 → 伸出/缩回/切换；
        /// 轴移动(绝对) → 目标位置；轴移动(相对) → 距离（正负即方向）；
        /// 写变量 → 写入值；显示轴参数 → 速度/加速时间/减速时间/点动距离/手动速度。
        /// </summary>
        public string Param
        {
            get => _param;
            set => SetField(ref _param, value);
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
