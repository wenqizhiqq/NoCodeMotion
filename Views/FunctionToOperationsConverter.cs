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
    /// 流程「运算」列下拉项随「功能 + 属性」联动（比旧版只看功能更精确）：
    ///   轴 + 位置/编码器位置 → 绝对移动 / 相对移动 / 回零 / 停止（运动指令）
    ///   轴 + 速度/扭矩/电流/加速度 → 修改为 / 加 / 减 / 乘 / 除 / 取模（设参）
    ///   轴 + 已回零         → 等于 / 大于 / 小于 / 大于等于 / 小于等于 / 取反（状态判读）
    ///   IO + 输出状态       → 修改为 / 置位 / 复位（写输出）
    ///   IO + 输入状态等     → 比较运算（判读）
    ///   气缸 + 电磁阀       → 伸出 / 缩回 / 复位（写阀）
    ///   气缸 + 状态类属性   → 比较运算（判读）
    ///   modbus             → 修改为 / 置位 / 复位
    ///   变量 + 数值         → 修改为 / 加 / 减 / 乘 / 除 / 取模 / 取反 / 比较
    ///   变量 + 字符串/布尔  → 修改为 / 取反 / 等于 …
    ///   点位 / 系统         → 修改为
    /// 已删除旧的「修改」选项，统一用「修改为」表达赋值，避免与「移动」混淆。
    /// </summary>
    public class FunctionToOperationsConverter : IMultiValueConverter
    {
        // ---- 动作类 ----
        private static readonly string[] MoveOps = { "绝对移动", "相对移动", "回零", "停止" };
        private static readonly string[] SetOps = { "修改为", "加", "减", "乘", "除", "取模" };
        private static readonly string[] IoWriteOps = { "修改为", "置位", "复位" };
        private static readonly string[] CylOps = { "伸出", "缩回", "复位" };

        // ---- 比较类（用于如果/否则如果 分支判定）----
        private static readonly string[] CondOps =
            { "等于", "大于", "小于", "大于等于", "小于等于", "取反" };
        private static readonly string[] VarNumOps =
            { "修改为", "加", "减", "乘", "除", "取模", "取反",
              "等于", "大于", "小于", "大于等于", "小于等于" };
        private static readonly string[] VarStrOps = { "修改为", "等于", "大于", "小于" };
        private static readonly string[] VarBoolOps = { "修改为", "取反", "等于" };

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string func = values.Length > 0 ? (values[0]?.ToString() ?? "") : "";
            string prop = values.Length > 1 ? (values[1]?.ToString() ?? "") : "";
            return OpsFor(func, prop);
        }

        private static IList<string> OpsFor(string func, string prop)
        {
            switch (func)
            {
                case "轴":
                    switch (prop)
                    {
                        case "位置":
                        case "编码器位置":
                            return new List<string>(MoveOps);
                        case "速度":
                        case "扭矩":
                        case "电流":
                        case "加速度":
                            return new List<string>(SetOps);
                        case "已回零":
                            return new List<string>(CondOps);
                        default:
                            return new List<string>(MoveOps);
                    }
                case "IO":
                case "IO输出":
                    return prop == "输出状态" ? new List<string>(IoWriteOps) : new List<string>(CondOps);
                case "气缸":
                    return prop == "电磁阀" ? new List<string>(CylOps) : new List<string>(CondOps);
                case "modbus":
                case "Modbus":
                    return new List<string>(IoWriteOps);
                case "变量":
                    return prop switch
                    {
                        "字符串" => new List<string>(VarStrOps),
                        "布尔" => new List<string>(VarBoolOps),
                        _ => new List<string>(VarNumOps)
                    };
                case "点位":
                case "系统":
                    return new List<string> { "修改为", "等于" };
                default:
                    return new List<string> { "修改为" };
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
// ◇作者保留所有权利　请勿删除※
