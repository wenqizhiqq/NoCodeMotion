// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 流程「运算」列下拉项随「功能」联动：
    ///   轴        → 运动指令：绝对移动 / 相对移动 / 回零 / 停止
    ///   其它功能   → 通用运算（修改 / 修改为 / 加 / 减 …），用于变量赋值与逻辑判断。
    /// 解决旧版「轴的位置只能选『修改 / 修改为』，看着像赋值而非移动」的困惑：
    /// 现在轴步骤的运算列直接呈现明确的移动命令。
    /// </summary>
    public class FunctionToOperationsConverter : IValueConverter
    {
        private static readonly string[] MoveOps = { "绝对移动", "相对移动", "回零", "停止" };
        private static readonly string[] GeneralOps =
        {
            "修改", "修改为", "加", "减", "乘", "除",
            "等于", "大于", "小于", "大于等于", "小于等于", "取模", "取反", "与", "或"
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value?.ToString() ?? "") == "轴" ? MoveOps : GeneralOps;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
// ◇作者保留所有权利　请勿删除※
