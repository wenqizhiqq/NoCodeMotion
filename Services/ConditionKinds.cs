// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启​志⁣◆​编⁣写⁣◇⁣微​信⁣﹕‎1‍8⁣7⁠◆‍1⁣9⁠3⁠6‏◇‍1‍3⁣9⁠9‌　‎※⁠保​留‎所​有⁠权​利‏请‎勿‍删‌除⁣◇​⁣​
// =====================================================================
// 移动条件「类型」元数据表：把每种条件类型的
//   名称来源 / 期望值候选 / 可用比较运算符 / 是否数值型
// 集中在一处声明。界面（PointPage.xaml）与模型（PointMoveCondition）都从这里取，
// 以后要加新类型只需在本文件加一行 + 在 PointConditionService 加一个分支。
// =====================================================================
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 移动条件（防撞机）的「类型」目录。
    /// 每种类型规定：名称从哪里选、期望值怎么填、能用哪些比较运算符。
    /// </summary>
    public static class ConditionKinds
    {
        // —— 类型名（同时作为落盘编码里的第一段，改名会导致旧工程读不出，慎改）——
        public const string Io = "IO";
        public const string Cylinder = "气缸";
        public const string Variable = "变量";
        public const string AxisPosition = "轴位置";
        public const string AxisEnabled = "轴使能";
        public const string AxisSpeed = "轴速度";
        public const string AxisLimitPlus = "轴正限位";
        public const string AxisLimitMinus = "轴负限位";
        public const string Camera = "相机";

        /// <summary>下拉里可选的类型，数组顺序即界面显示顺序。</summary>
        public static readonly string[] All =
        {
            Io, Cylinder, Variable, AxisPosition, AxisEnabled, AxisSpeed, AxisLimitPlus, AxisLimitMinus, Camera
        };

        // 枚举型（状态二选一）只允许 == / !=；数值型给全 6 种。
        private static readonly string[] EnumOperators = { "==", "!=" };
        private static readonly string[] NumOperators = { "==", "!=", ">", ">=", "<", "<=" };

        private static readonly string[] IoValues = { "0", "1" };
        private static readonly string[] CylinderValues = { "伸出", "缩回" };
        private static readonly string[] AxisEnabledValues = { "已使能", "未使能" };
        private static readonly string[] CameraValues = { "已连接", "未连接" };
        private static readonly string[] NoValues = System.Array.Empty<string>();

        /// <summary>
        /// 名称候选。直接返回 Catalog 里的实时集合（引用同一个 ObservableCollection），
        /// 这样轴/IO/气缸/变量/相机的配置一变，下拉框立刻跟着刷新。
        /// </summary>
        public static ObservableCollection<string> TargetsFor(string? kind) => kind switch
        {
            Cylinder => Catalog.CylinderNames,
            Variable => Catalog.VariableNames,
            AxisPosition or AxisEnabled or AxisSpeed or AxisLimitPlus or AxisLimitMinus => Catalog.AxisNames,
            Camera => Catalog.CameraNames,
            _ => Catalog.IoNames,
        };

        /// <summary>期望值候选；返回空集合表示该类型由用户自由输入（数值型）。</summary>
        public static IReadOnlyList<string> ValuesFor(string? kind) => kind switch
        {
            Io => IoValues,
            Cylinder => CylinderValues,
            AxisEnabled => AxisEnabledValues,
            Camera => CameraValues,
            _ => NoValues,
        };

        /// <summary>是否数值型：期望值要自己填数字，且支持 &gt; / &gt;= / &lt; / &lt;= 比较。</summary>
        public static bool IsNumeric(string? kind) =>
            kind is Variable or AxisPosition or AxisSpeed or AxisLimitPlus or AxisLimitMinus;

        /// <summary>该类型可用的比较运算符。</summary>
        public static IReadOnlyList<string> OperatorsFor(string? kind) =>
            IsNumeric(kind) ? NumOperators : EnumOperators;

        /// <summary>期望值输入框的提示（数值型才显示）。</summary>
        public static string ValueHintFor(string? kind) => kind switch
        {
            Variable => "数值",
            AxisPosition => "位置",
            AxisSpeed => "速度",
            AxisLimitPlus or AxisLimitMinus => "限位",
            _ => "期望值",
        };

        /// <summary>比较运算：数值统一按 double 比，运算符非法时按「相等」处理。</summary>
        public static bool Compare(double actual, double expected, string? comparison) => comparison switch
        {
            "!=" => actual != expected,
            ">" => actual > expected,
            ">=" => actual >= expected,
            "<" => actual < expected,
            "<=" => actual <= expected,
            _ => actual == expected,
        };
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
