// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
// ◆温‏启‍志‎◆‎编‌写‍◇‌微‏信‏﹕‎1⁠8‍7​◆‌1‌9‏3⁣6‌◇​1‏3‎9‏9‏　‎※‍保‎留⁣所‌有⁠权‏利‎请‌勿​删‍除‎◇
// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NoCodeMotion.Views;

/// <summary>bool → Visibility：true 显示、false 折叠。</summary>
public sealed class NgBoolToVis : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => v is Visibility vis && vis == Visibility.Visible;
}

/// <summary>bool → Visibility：true 折叠、false 显示（与 NgBoolToVis 反相）。</summary>
public sealed class NgInverseBoolToVis : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => !(v is Visibility vis && vis == Visibility.Visible);
}

/// <summary>十六进制颜色字符串 → SolidColorBrush。</summary>
public sealed class NgStringToBrush : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        try
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(s));
        }
        catch { }
        return new SolidColorBrush(Colors.Gray);
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => null!;
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨۩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧
