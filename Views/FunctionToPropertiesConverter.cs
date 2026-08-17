using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 把“功能”列映射到对应的「属性」可选项：
    ///   轴     → 速度 / 位置 / 编码器位置
    ///   IO     → 输入状态 / 输出状态
    ///   气缸   → 伸出到位 / 缩回到位 / 电磁阀
    ///   modbus → 寄存器值 / 线圈状态
    /// 用于流程「属性」下拉框随功能自动填充。
    /// </summary>
    public class FunctionToPropertiesConverter : IValueConverter
    {
        private static readonly Dictionary<string, List<string>> Map = new()
        {
            ["轴"] = new() { "速度", "位置", "编码器位置" },
            ["IO"] = new() { "输入状态", "输出状态" },
            ["气缸"] = new() { "伸出到位", "缩回到位", "电磁阀" },
            ["modbus"] = new() { "寄存器值", "线圈状态" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => Map.TryGetValue(value?.ToString() ?? string.Empty, out var list)
                ? list
                : new List<string> { "速度" };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
