using System;
using System.Globalization;
using System.Windows.Data;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 把"轴名 + 后缀（位置/速度）"拼成列头文本。
    /// values[0]=轴名（可能为空），values[1]=后缀（"位置"/"速度"）。
    /// 轴名为空时回退为"未选轴"，保证列头永远有可读中文。
    /// </summary>
    public class AxisHeaderMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var name = values != null && values.Length > 0 ? values[0] as string : null;
            var suffix = values != null && values.Length > 1 ? values[1] as string : "";
            if (string.IsNullOrWhiteSpace(name))
                name = "未选轴";
            return string.IsNullOrEmpty(suffix) ? name : $"{name} {suffix}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>布尔值 → 中文"是/否"，用于轴状态展示。</summary>
    public class BoolToZhConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? "是" : "否";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// 通用 StringFormat 转换器：把 value 按 parameter 格式化为字符串。
    /// 用于向 object 类型的 CommandParameter 传字符串（如 "0,+"）——
    /// WPF 的 Binding.StringFormat 只在目标为 string 时生效；CommandParameter 是 object，
    /// 直接写 StringFormat 会被静默忽略，导致点击按钮后 Move 收到 int 而抛 InvalidCastException。
    /// </summary>
    public class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.Format(culture, parameter as string ?? "{0}", value);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
