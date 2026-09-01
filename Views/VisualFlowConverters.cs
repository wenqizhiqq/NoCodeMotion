// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇⁣
using System;
using System.Globalization;
using System.Windows.Data;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 角度取反转换器（OpenCV 逆时针 → WPF 顺时针）。
    /// 模板匹配返回的角度是 OpenCV 约定（正角度 = 屏幕上逆时针），但 WPF 的
    /// RotateTransform 用相反约定（正角度 = 顺时针）。要让叠加层的绿框视觉上
    /// 与检测目标对齐，必须给 RotateTransform 喂「取反」后的角度。
    /// 显示给用户的文字标签则保留原值（用 StringFormat 直接绑 Angle）。
    /// 参考 GrayMatch.Wpf.AngleSignConverter。
    /// </summary>
    public sealed class AngleNegateConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d) return -d;
            if (value is int i) return (double)-i;
            return 0.0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d) return -d;
            return 0.0;
        }
    }
}
// ◇作者保留所有权利　请勿删除※⁣