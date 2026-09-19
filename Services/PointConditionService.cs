// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁣启‎志‌◆⁠编‍写‏◇‎微‍信‏﹕‍1‏8⁣7‍◆⁠1‌9​3‌6⁣◇‍1‏3‎9‍9⁣　⁣※‍保⁠留⁠所‏有⁠权‏利⁠请⁠勿​删​除‍◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// =====================================================================
// 点位移动前防撞条件校验：把每个 PointMoveCondition 与当前实时状态比对，
// 全部满足才放行移动。各类型的实际值来源见 ConditionKinds：
//   IO         → HardwareBridge.Current.ReadInput（输入）/ SimRuntime.GetOutput（输出）
//   气缸       → SimRuntime.GetCylinder（仿真状态仓，0=缩回 / 1=伸出）
//   变量       → SimRuntime.GetVariableResolved（支持表达式，递归求值）
//   轴位置     → AxisRuntimeState.Get（仿真/实机每帧写入的当前位置）
//   轴使能     → AxisItem.Enabled
//   轴速度     → AxisItem.Speed
//   轴正/负限位→ AxisItem.PosLimitPlus / PosLimitMinus
//   相机       → CameraItem.IsConnected
// 返回未满足的条件说明列表；空列表表示全部满足（或无条件）。
// =====================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 点位防撞条件校验服务：移动到某点位前，逐一核对它配置的条件是否与当前设备状态一致。
    /// 任意一条不满足即视为不安全，移动应被阻止（防撞机）。
    /// </summary>
    public static class PointConditionService
    {
        /// <summary>
        /// 返回未满足的条件说明；空列表表示全部满足（或无条件）。
        /// 名称留空的行不参与判断；已填名称的行必须全部满足才允许移动。
        /// </summary>
        public static IReadOnlyList<string> Evaluate(PointItem? point)
        {
            var fails = new List<string>();
            if (point?.Conditions == null) return fails;

            foreach (var c in point.Conditions)
            {
                if (string.IsNullOrWhiteSpace(c.TargetName)) continue;   // 空行不参与判断
                var fail = Check(c);
                if (fail != null) fails.Add(fail);
            }
            return fails;
        }

        /// <summary>把某条条件的实际状态转成人类可读文本（用于日志/界面提示）。</summary>
        public static string DescribeActual(string kind, string targetName)
        {
            switch (kind)
            {
                case ConditionKinds.Cylinder:
                    return SimRuntime.GetCylinder(targetName) == 1 ? "伸出" : "缩回";
                case ConditionKinds.Variable:
                    return SimRuntime.GetVariableResolved(targetName).ToString("0.###", CultureInfo.InvariantCulture);
                case ConditionKinds.Camera:
                {
                    var cam = FindCamera(targetName);
                    return cam == null ? "找不到" : (cam.IsConnected ? "已连接" : "未连接");
                }
                case ConditionKinds.AxisPosition:
                    return AxisRuntimeState.Get(targetName).ToString("0.###", CultureInfo.InvariantCulture);
                case ConditionKinds.AxisEnabled:
                case ConditionKinds.AxisSpeed:
                case ConditionKinds.AxisLimitPlus:
                case ConditionKinds.AxisLimitMinus:
                {
                    var axis = FindAxis(targetName);
                    if (axis == null) return "找不到";
                    double v = kind switch
                    {
                        ConditionKinds.AxisEnabled => axis.Enabled ? 1.0 : 0.0,
                        ConditionKinds.AxisSpeed => axis.Speed,
                        ConditionKinds.AxisLimitPlus => axis.PosLimitPlus,
                        _ => axis.PosLimitMinus,
                    };
                    return kind == ConditionKinds.AxisEnabled ? (v == 1 ? "已使能" : "未使能")
                                                              : v.ToString("0.###", CultureInfo.InvariantCulture);
                }
                default:
                {
                    var io = ResolveIo(targetName);
                    return io.Found ? io.Value.ToString(CultureInfo.InvariantCulture) : "找不到";
                }
            }
        }

        // ===================== 内部实现 =====================

        /// <summary>校验单条条件：满足返回 null，不满足/无法校验返回说明文本。</summary>
        private static string? Check(PointMoveCondition c)
        {
            string kind = string.IsNullOrWhiteSpace(c.Kind) ? ConditionKinds.Io : c.Kind;
            string target = c.TargetName.Trim();

            // 1) 取实际值（数值统一成 double，便于和期望值比较）
            double actual;
            string actualText;
            double tolerance = 0;

            switch (kind)
            {
                case ConditionKinds.Cylinder:
                    actual = SimRuntime.GetCylinder(target);
                    actualText = actual == 1 ? "伸出" : "缩回";
                    break;

                case ConditionKinds.Variable:
                    actual = SimRuntime.GetVariableResolved(target);
                    actualText = actual.ToString("0.###", CultureInfo.InvariantCulture);
                    break;

                case ConditionKinds.Camera:
                {
                    var cam = FindCamera(target);
                    if (cam == null) return $"相机 [{target}] 在项目相机列表中找不到，无法校验";
                    actual = cam.IsConnected ? 1 : 0;
                    actualText = cam.IsConnected ? "已连接" : "未连接";
                    break;
                }

                case ConditionKinds.AxisPosition:
                case ConditionKinds.AxisEnabled:
                case ConditionKinds.AxisSpeed:
                case ConditionKinds.AxisLimitPlus:
                case ConditionKinds.AxisLimitMinus:
                {
                    var axis = FindAxis(target);
                    if (axis == null) return $"轴 [{target}] 在项目轴配置中找不到，无法校验";
                    switch (kind)
                    {
                        case ConditionKinds.AxisPosition:
                            actual = AxisRuntimeState.Get(target);
                            // 位置比较带容差：直接用轴的「到位误差 InPosError」，
                            // 否则 == 在浮点/机械误差下永远不成立。
                            tolerance = axis.InPosError;
                            actualText = actual.ToString("0.###", CultureInfo.InvariantCulture);
                            break;
                        case ConditionKinds.AxisEnabled:
                            actual = axis.Enabled ? 1 : 0;
                            actualText = axis.Enabled ? "已使能" : "未使能";
                            break;
                        case ConditionKinds.AxisSpeed:
                            actual = axis.Speed;
                            actualText = actual.ToString("0.###", CultureInfo.InvariantCulture);
                            break;
                        case ConditionKinds.AxisLimitPlus:
                            actual = axis.PosLimitPlus;
                            actualText = actual.ToString("0.###", CultureInfo.InvariantCulture);
                            break;
                        default:
                            actual = axis.PosLimitMinus;
                            actualText = actual.ToString("0.###", CultureInfo.InvariantCulture);
                            break;
                    }
                    break;
                }

                default: // IO
                {
                    var io = ResolveIo(target);
                    if (!io.Found) return $"IO [{target}] 在项目 IO（输入/输出）中找不到，无法校验";
                    actual = io.Value;
                    actualText = io.Value.ToString(CultureInfo.InvariantCulture);
                    break;
                }
            }

            // 2) 期望值文本 → 数值
            if (!TryParseExpected(kind, c.ExpectedState, out double expected))
                return $"{kind} [{target}] 的期望值「{c.ExpectedState}」不是可识别的{DescribeValueDomain(kind)}，无法校验";

            // 3) 比较
            bool pass = CompareWith(actual, expected, c.Comparison, tolerance);
            return pass ? null : $"{kind} [{target}] 期望「{c.ExpectedState}」实际「{actualText}」";
        }

        /// <summary>期望值文本解析：枚举型按状态词映射成 1/0，数值型按 double 解析。</summary>
        private static bool TryParseExpected(string kind, string? raw, out double value)
        {
            value = 0;
            string s = (raw ?? string.Empty).Trim();
            if (s.Length == 0) return false;

            switch (kind)
            {
                case ConditionKinds.Cylinder:
                    if (s == "伸出") { value = 1; return true; }
                    if (s == "缩回") { value = 0; return true; }
                    return false;
                case ConditionKinds.AxisEnabled:
                    if (s == "已使能") { value = 1; return true; }
                    if (s == "未使能") { value = 0; return true; }
                    return false;
                case ConditionKinds.Camera:
                    if (s == "已连接") { value = 1; return true; }
                    if (s == "未连接") { value = 0; return true; }
                    return false;
                case ConditionKinds.Io:
                    if (s == "1") { value = 1; return true; }
                    if (s == "0") { value = 0; return true; }
                    return false;
                default:
                    return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
            }
        }

        /// <summary>比较：容差大于 0 时，== / != 按「差值在容差内即视为相等」处理（用于轴位置）。</summary>
        private static bool CompareWith(double actual, double expected, string? comparison, double tolerance)
        {
            string op = string.IsNullOrWhiteSpace(comparison) ? "==" : comparison;
            if (tolerance > 0 && (op == "==" || op == "!="))
            {
                bool same = Math.Abs(actual - expected) <= tolerance;
                return op == "!=" ? !same : same;
            }
            return ConditionKinds.Compare(actual, expected, op);
        }

        private static string DescribeValueDomain(string kind) => kind switch
        {
            ConditionKinds.Io => "IO 状态（0 / 1）",
            ConditionKinds.Cylinder => "气缸状态（伸出 / 缩回）",
            ConditionKinds.AxisEnabled => "轴使能状态（已使能 / 未使能）",
            ConditionKinds.Camera => "相机连接状态（已连接 / 未连接）",
            _ => "数字",
        };

        private static AxisItem? FindAxis(string name) =>
            ProjectStore.Data?.Axes.FirstOrDefault(a => a.Name == name);

        private static CameraItem? FindCamera(string name) =>
            ProjectStore.Data?.Cameras.FirstOrDefault(c => c.Name == name);

        private static (int Value, bool Found) ResolveIo(string name)
        {
            var data = ProjectStore.Data;
            if (data != null)
            {
                var input = data.Inputs.FirstOrDefault(i => i.Name == name);
                if (input != null)
                {
                    var bridge = HardwareBridge.Current;
                    if (bridge != null) return ((int)bridge.ReadInput(input), true);
                }
                var output = data.Outputs.FirstOrDefault(i => i.Name == name);
                if (output != null) return (SimRuntime.GetOutput(name), true);
            }
            return (0, false);
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
