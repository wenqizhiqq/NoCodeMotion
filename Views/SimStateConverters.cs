// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// 仿真运行时状态 → UI 绑定的值/画刷转换器（IO / 气缸 / 变量表达式）。
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using NoCodeMotion.Services;

namespace NoCodeMotion.Views
{
    /// <summary>IO 输出运行时电平（0/1）。</summary>
    public sealed class IoRuntimeStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => SimRuntime.GetOutput(value as string) == 1 ? "1" : "0";
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }

    /// <summary>IO 输出运行时电平画刷：置位=绿，复位=灰。</summary>
    public sealed class IoRuntimeBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => SimRuntime.GetOutput(value as string) == 1
                ? (Brush)new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E))
                : (Brush)new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF));
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }

    /// <summary>气缸运行时状态文字（伸出/缩回）。</summary>
    public sealed class CylinderRuntimeStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => SimRuntime.GetCylinder(value as string) == 1 ? "伸出" : "缩回";
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }

    /// <summary>气缸运行时状态画刷：伸出=蓝，缩回=灰蓝。</summary>
    public sealed class CylinderRuntimeBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => SimRuntime.GetCylinder(value as string) == 1
                ? (Brush)new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB))
                : (Brush)new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69));
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }

    /// <summary>变量行解析值摘要：对值为表达式的变量，列出 "名称=解析值"。</summary>
    public sealed class VarResolveSummaryConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 10) return "";
            var parts = new System.Collections.Generic.List<string>();
            for (int i = 0; i < 5; i++)
            {
                string name = values[i * 2] as string ?? "";
                string raw = values[i * 2 + 1] as string ?? "";
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(raw)) continue;
                if (ExpressionEvaluator.IsExpression(raw))
                {
                    double v = SimRuntime.GetVariableResolved(name);
                    parts.Add($"{name}={v:0.###}");
                }
            }
            return string.Join("   ", parts);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => System.Array.Empty<object>();
    }

    /// <summary>相机闪光指示画刷：取像瞬间(1.2s 内)绿色亮起，否则灰色。</summary>
    public sealed class CamFlashBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var t = SimRuntime.GetCamFlash(value as string);
            bool lit = t != DateTime.MinValue && (DateTime.Now - t).TotalSeconds < 1.2;
            return lit
                ? (Brush)new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E))
                : (Brush)new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣ۤ
