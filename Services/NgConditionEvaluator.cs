// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦
using System;
using System.Globalization;
using System.Linq;
using NoCodeMotion.Models.NodeGraph;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 节点图「条件分支」节点的条件求值。
    /// 属性面板的实时判定（是否满足）与 <see cref="NgRunner"/> 的运行路由共用同一套逻辑，
    /// 保证「面板上看到满足哪条」= 「运行时走哪条分支」。
    ///
    /// 每条分支 = 类型（轴位置 / 轴速度 / 输入IO / 输出IO / 变量）+ 名称 + 比较 + 值；
    /// 未配置结构化分支时回退历史「条件」表达式（兼容旧图与内置模板）。
    /// </summary>
    public static class NgConditionEvaluator
    {
        public const int MaxBranches = 8;

        public static string Prop(NgNode n, string name)
            => n.Props.FirstOrDefault(p => p.Name == name)?.Value ?? string.Empty;

        /// <summary>分支数（夹在 1..8）。</summary>
        public static int BranchCount(NgNode n)
        {
            if (!int.TryParse(Prop(n, "分支数"), NumberStyles.Any, CultureInfo.InvariantCulture, out int v)) v = 2;
            return Math.Clamp(v, 1, MaxBranches);
        }

        /// <summary>该分支是否已配置（名称非空）。</summary>
        public static bool IsConfigured(NgNode n, int branch)
            => !string.IsNullOrWhiteSpace(Prop(n, $"条件{branch}名称"));

        /// <summary>旧图 / 内置模板：没有任何结构化分支、但写了历史「条件」表达式。</summary>
        public static bool IsLegacy(NgNode n)
        {
            for (int i = 1; i <= MaxBranches; i++)
                if (IsConfigured(n, i)) return false;
            return !string.IsNullOrWhiteSpace(Prop(n, "条件"));
        }

        /// <summary>历史「条件」表达式求值（变量参与，兼容旧图）。</summary>
        public static bool EvaluateLegacy(NgNode n)
        {
            string expr = Prop(n, "条件");
            if (string.IsNullOrWhiteSpace(expr)) return false;
            try
            {
                return ExpressionEvaluator.Evaluate(expr, SimRuntime.GetVariableResolved, out double v) && v != 0;
            }
            catch { return false; }
        }

        /// <summary>读第 branch 条条件的左值（真实值：轴位置/速度、输入/输出电平、变量值）。读不到返回 false。</summary>
        public static bool TryReadLeft(NgNode n, int branch, out double value, out string shown)
        {
            value = 0;
            shown = "";
            string type = Prop(n, $"条件{branch}类型");
            string name = Prop(n, $"条件{branch}名称");
            if (string.IsNullOrWhiteSpace(name)) return false;
            var bridge = HardwareBridge.Current;
            try
            {
                switch (type)
                {
                    case "轴速度":
                    {
                        var ax = HardwareResolver.ResolveAxis(name);
                        if (ax == null) return false;
                        value = ax.Speed;
                        shown = $"{name} 速度={value:0.###}";
                        return true;
                    }
                    case "输入IO":
                    {
                        var io = HardwareResolver.ResolveInput(name);
                        if (io == null || bridge == null) return false;
                        value = bridge.ReadInput(io);
                        shown = $"{name} 输入={value:0.###}";
                        return true;
                    }
                    case "输出IO":
                        value = SimRuntime.GetOutput(name);
                        shown = $"{name} 输出={value:0}";
                        return true;
                    case "变量":
                        value = SimRuntime.GetVariableResolved(name);
                        shown = $"{name}={value:0.###}";
                        return true;
                    default:   // 轴位置
                    {
                        var ax = HardwareResolver.ResolveAxis(name);
                        if (ax == null) return false;
                        double pos = double.NaN;
                        if (bridge != null) { try { pos = bridge.ReadAxisPosition(ax); } catch { pos = double.NaN; } }
                        if (double.IsNaN(pos)) pos = AxisRuntimeState.Get(name);
                        value = pos;
                        shown = $"{name} 位置={pos:0.###}";
                        return true;
                    }
                }
            }
            catch { return false; }
        }

        public static bool Compare(string op, double left, double right)
        {
            switch ((op ?? string.Empty).Trim())
            {
                case "不等于": return left != right;
                case "大于": return left > right;
                case "大于等于": return left >= right;
                case "小于": return left < right;
                case "小于等于": return left <= right;
                default: return left == right;   // 等于
            }
        }

        /// <summary>第 branch 条条件是否成立。</summary>
        public static bool Evaluate(NgNode n, int branch)
        {
            if (!IsConfigured(n, branch)) return false;
            if (!TryReadLeft(n, branch, out double left, out _)) return false;
            double.TryParse(Prop(n, $"条件{branch}值"), NumberStyles.Any, CultureInfo.InvariantCulture, out double right);
            return Compare(Prop(n, $"条件{branch}比较"), left, right);
        }

        /// <summary>条件分支的路由端口（运行器 / 3D 预览共用）：按序取第一条满足的分支（条件1..条件N）；
        /// 都不满足 → 「否则」；旧图（无结构化分支但有「条件」表达式）→ True / False。</summary>
        public static string ResolvePort(NgNode n)
        {
            if (IsLegacy(n)) return EvaluateLegacy(n) ? "True" : "False";
            int count = BranchCount(n);
            for (int i = 1; i <= count; i++)
                if (Evaluate(n, i)) return $"条件{i}";
            return "否则";
        }

        /// <summary>路由端口的人读说明（节点卡摘要 / 3D 预览日志用）。</summary>
        public static string DescribePort(NgNode n, string port)
        {
            if (IsLegacy(n)) return Prop(n, "条件");
            if (port == "否则") return "无分支满足 → 否则";
            if (port.StartsWith("条件", StringComparison.Ordinal) && int.TryParse(port.Substring(2), out int i))
                return $"条件{i}：{Describe(n, i)}";
            return port;
        }

        /// <summary>条件的人读文本（属性面板 / 节点摘要用）。</summary>
        public static string Describe(NgNode n, int branch)
        {
            string name = Prop(n, $"条件{branch}名称");
            if (string.IsNullOrWhiteSpace(name)) return "未配置";
            return $"{Prop(n, $"条件{branch}类型")} {name} {Prop(n, $"条件{branch}比较")} {Prop(n, $"条件{branch}值")}";
        }
    }
}
// ◇作者保留所有权利　请勿删除※
