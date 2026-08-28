// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 把“功能”列映射到对应的「属性」可选项：
    ///   轴     → 速度 / 位置 / 编码器位置 / 扭矩 / 电流 / 加速度 / 已回零
    ///   IO     → 输入状态 / 输出状态 / 脉冲状态 / 报警状态
    ///   气缸   → 伸出到位 / 缩回到位 / 电磁阀 / 压力 / 动作中
    ///   modbus → 寄存器值 / 线圈状态 / 保持寄存器 / 输入寄存器
    ///   变量   → 数值 / 字符串 / 布尔
    ///   系统   → 运行时间 / 节拍 / 报警数 / 急停状态
    /// 用于流程「属性」下拉框随功能自动填充。
    /// </summary>
    public class FunctionToPropertiesConverter : IValueConverter
    {
        private static readonly Dictionary<string, List<string>> Map = new()
        {
            ["轴"] = new() { "速度", "位置", "编码器位置", "扭矩", "电流", "加速度", "已回零" },
            ["IO"] = new() { "输入状态", "输出状态", "脉冲状态", "报警状态" },
            ["气缸"] = new() { "伸出到位", "缩回到位", "电磁阀", "压力", "动作中" },
            ["modbus"] = new() { "寄存器值", "线圈状态", "保持寄存器", "输入寄存器" },
            ["变量"] = new() { "数值", "字符串", "布尔" },
            ["点位"] = new() { "速度", "加速度", "到位误差", "是否等待到位" },
            ["系统"] = new() { "运行时间", "节拍", "报警数", "急停状态" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => Map.TryGetValue(value?.ToString() ?? string.Empty, out var list)
                ? list
                : new List<string> { "速度" };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
