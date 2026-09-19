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
// 另提供 Probe()：给界面「实际值」列取实时读数（只读展示，不参与放行判断）。
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
        /// <summary>目标名在项目配置里找不到时，「实际值」列显示的文本。</summary>
        public const string NotFoundText = "找不到";

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
                if (!c.IsUsed) continue;                                 // 未勾选「使用」的行不参与判断
                if (string.IsNullOrWhiteSpace(c.TargetName)) continue;   // 空行不参与判断
                var fail = Check(c);
                if (fail != null) fails.Add(fail);
            }
            return fails;
        }

        /// <summary>
        /// 探测一条条件的实时实际值，供界面「实际值」列显示（只读）。
        /// Text：实际值文本，名称留空时为空串；
        /// Ok  ：true = 当前满足该条条件，false = 不满足或目标找不到，null = 该行不参与判断。
        /// </summary>
        public static (string Text, bool? Ok) Probe(PointMoveCondition c)
        {
            if (c == null || string.IsNullOrWhiteSpace(c.TargetName)) return (string.Empty, null);

            string kind = NormalizeKind(c.Kind);
            string target = c.TargetName.Trim();

            // 展示用读数：目标在项目里不存在时如实显示「找不到」，
            // 避免把 SimRuntime 的兜底 0 当成真实读数展示给操作者。
            if (!TryReadActual(kind, target, strictTarget: true, out _, out string text, out _, out _))
                return (NotFoundText, c.IsUsed ? false : (bool?)null);

            // 未勾选「使用」：照常显示实际值，但不参与判断（界面显示为灰色）
            if (!c.IsUsed) return (text, null);

            return (text, Check(c) == null);
        }

        /// <summary>把某条条件的实际状态转成人类可读文本（用于日志/界面提示）。</summary>
        public static string DescribeActual(string kind, string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName)) return string.Empty;
            string k = NormalizeKind(kind);
            return TryReadActual(k, targetName.Trim(), strictTarget: true, out _, out string text, out _, out _)
                ? text
                : NotFoundText;
        }

        // ===================== 内部实现 =====================

        /// <summary>空/未填的类型一律按 IO 处理（与旧实现一致）。</summary>
        private static string NormalizeKind(string? kind) =>
            string.IsNullOrWhiteSpace(kind) ? ConditionKinds.Io : kind;

        /// <summary>校验单条条件：满足返回 null，不满足/无法校验返回说明文本。</summary>
        private static string? Check(PointMoveCondition c)
        {
            string kind = NormalizeKind(c.Kind);
            string target = c.TargetName.Trim();

            // 1) 取实际值（数值统一成 double，便于和期望值比较）
            //    strictTarget: false —— 与改造前完全一致：气缸/变量名即使不在项目名称库里也照读，
            //    避免历史工程因名称笔误突然被拦死（放行判断的行为不做任何变更）。
            if (!TryReadActual(kind, target, strictTarget: false,
                               out double actual, out string actualText, out double tolerance, out string error))
                return error;

            // 2) 期望值文本 → 数值
            if (!TryParseExpected(kind, c.ExpectedState, out double expected))
                return $"{kind} [{target}] 的期望值「{c.ExpectedState}」不是可识别的{DescribeValueDomain(kind)}，无法校验";

            // 3) 比较
            bool pass = CompareWith(actual, expected, c.Comparison, tolerance);
            return pass ? null : $"{kind} [{target}] 期望「{c.ExpectedState}」实际「{actualText}」";
        }

        /// <summary>
        /// 读取一条条件的实际状态。
        /// <paramref name="strictTarget"/> = true 时，气缸 / 变量也必须能在项目名称库里找到；
        /// = false 时沿用旧行为（只对 相机 / 轴 / IO 做存在性校验）。
        /// 返回 false 表示读不到，<paramref name="error"/> 给出原因。
        /// </summary>
        private static bool TryReadActual(string kind, string target, bool strictTarget,
                                          out double value, out string text, out double tolerance, out string error)
        {
            value = 0;
            text = string.Empty;
            tolerance = 0;
            error = string.Empty;

            switch (kind)
            {
                case ConditionKinds.Cylinder:
                    if (strictTarget && !Catalog.CylinderNames.Contains(target))
                    {
                        error = $"气缸 [{target}] 在项目气缸列表中找不到，无法校验";
                        return false;
                    }
                    value = SimRuntime.GetCylinder(target);
                    text = value == 1 ? "伸出" : "缩回";
                    return true;

                case ConditionKinds.Variable:
                    if (strictTarget && !Catalog.VariableNames.Contains(target))
                    {
                        error = $"变量 [{target}] 在项目变量列表中找不到，无法校验";
                        return false;
                    }
                    value = SimRuntime.GetVariableResolved(target);
                    text = Format(value);
                    return true;

                case ConditionKinds.Camera:
                {
                    var cam = FindCamera(target);
                    if (cam == null) { error = $"相机 [{target}] 在项目相机列表中找不到，无法校验"; return false; }
                    value = cam.IsConnected ? 1.0 : 0.0;
                    text = cam.IsConnected ? "已连接" : "未连接";
                    return true;
                }

                case ConditionKinds.AxisPosition:
                case ConditionKinds.AxisEnabled:
                case ConditionKinds.AxisSpeed:
                case ConditionKinds.AxisLimitPlus:
                case ConditionKinds.AxisLimitMinus:
                {
                    var axis = FindAxis(target);
                    if (axis == null) { error = $"轴 [{target}] 在项目轴配置中找不到，无法校验"; return false; }
                    switch (kind)
                    {
                        case ConditionKinds.AxisPosition:
                            value = AxisRuntimeState.Get(target);
                            // 位置比较带容差：直接用轴的「到位误差 InPosError」，
                            // 否则 == 在浮点/机械误差下永远不成立。
                            tolerance = axis.InPosError;
                            text = Format(value);
                            break;
                        case ConditionKinds.AxisEnabled:
                            value = axis.Enabled ? 1.0 : 0.0;
                            text = axis.Enabled ? "已使能" : "未使能";
                            break;
                        case ConditionKinds.AxisSpeed:
                            value = axis.Speed;
                            text = Format(value);
                            break;
                        case ConditionKinds.AxisLimitPlus:
                            value = axis.PosLimitPlus;
                            text = Format(value);
                            break;
                        default:
                            value = axis.PosLimitMinus;
                            text = Format(value);
                            break;
                    }
                    return true;
                }

                default: // IO
                {
                    var io = ResolveIo(target);
                    if (!io.Found) { error = $"IO [{target}] 在项目 IO（输入/输出）中找不到，无法校验"; return false; }
                    value = io.Value;
                    text = io.Value.ToString(CultureInfo.InvariantCulture);
                    return true;
                }
            }
        }

        /// <summary>数值统一按 3 位小数显示（去掉多余的 0）。</summary>
        private static string Format(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

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
