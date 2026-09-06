// ◆◇※▣▤▥▦▧▨▩░💒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░💒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░💒▓✦
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 把「逻辑」列的类别（控制流 / 动作 / 注释）或具体值映射为浅色背景画刷，
    /// 用于下拉分组头与表格单元格着色，颜色编码与全局调色板一致：
    /// 控制流=浅蓝、动作=浅橙、注释=浅灰。
    /// </summary>
    public class LogicCategoryToBrushConverter : IValueConverter
    {
        // 浅色背景（与 §12/§14 调色板对应：primary #2563EB / accent #F59E0B / neutral #94A3B8）
        private static readonly SolidColorBrush ControlFlow = new(Color.FromRgb(0xDB, 0xEA, 0xFE)); // 浅蓝
        private static readonly SolidColorBrush Action = new(Color.FromRgb(0xFE, 0xF3, 0xC7));        // 浅橙
        private static readonly SolidColorBrush Comment = new(Color.FromRgb(0xF1, 0xF5, 0xF9));       // 浅灰

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value?.ToString();
            return s switch
            {
                "控制流" => ControlFlow,
                "动作" => Action,
                "注释" => Comment,
                // 直接传具体值（如果/就/否则…）也按值归类
                "如果" or "否则如果" or "否则" or "并且" or "或者"
                    or "循环开始" or "循环结束" or "结束" => ControlFlow,
                "就" or "等待" or "延时" => Action,
                _ => Comment
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
