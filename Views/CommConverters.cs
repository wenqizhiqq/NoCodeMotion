// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 通讯类型 → Visibility：当 CommType 命中 parameter 中以 '|' 分隔的任一类型时返回 Visible，否则 Collapsed。
    /// 用于在通讯页按所选「通讯类型」动态显隐对应的参数区块。
    /// </summary>
    public class CommTypeVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var type = value as string ?? string.Empty;
            var list = (parameter as string ?? string.Empty).Split('|');
            foreach (var t in list)
            {
                if (string.Equals(t.Trim(), type, StringComparison.OrdinalIgnoreCase))
                    return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
