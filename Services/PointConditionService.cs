// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‍志​◆‌编⁠写​◇⁠微‍信⁠﹕‍1‍8‍7‍◆‍1‌9‍3‏6‍◇⁠1⁣3‍9‍9‌　​※⁠保‌留‍所‌有⁠权⁠利‍请‏勿⁠删​‍除‍◇​⁣​
// =====================================================================
// 点位移动前防撞条件校验：把每个 PointMoveCondition 与当前实时状态比对，
// 全部满足才放行移动。IO 实时状态经 HardwareBridge.Current.ReadInput（真实硬件或桩）；
// 气缸实时状态经 SimRuntime.GetCylinder（仿真状态仓，0=缩回 / 1=伸出）。
// 返回未满足的条件说明列表；空列表表示全部满足（或无条件）。
// =====================================================================
using System.Collections.Generic;
using System.Linq;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 点位防撞条件校验服务：移动到某点位前，逐一核对它配置的 IO / 气缸条件是否与当前
    /// 设备实时状态一致。任意一条不满足即视为不安全，移动应被阻止（防撞机）。
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

                if (c.Kind == "气缸")
                {
                    int actual = SimRuntime.GetCylinder(c.TargetName);
                    int expected = c.ExpectedState == "伸出" ? 1 : 0;
                    bool pass = c.Comparison == "!=" ? actual != expected : actual == expected;
                    if (!pass)
                        fails.Add($"气缸 [{c.TargetName}] 期望「{c.ExpectedState}」实际「{(actual == 1 ? "伸出" : "缩回")}」");
                }
                else // IO
                {
                    var io = ResolveIo(c.TargetName);
                    if (!io.Found)
                    {
                        fails.Add($"IO [{c.TargetName}] 在项目 IO（输入/输出）中找不到，无法校验");
                        continue;
                    }
                    int expected = c.ExpectedState == "1" ? 1 : 0;
                    bool pass = c.Comparison == "!=" ? io.Value != expected : io.Value == expected;
                    if (!pass)
                        fails.Add($"IO [{c.TargetName}] 期望「{c.ExpectedState}」实际「{io.Value}」");
                }
            }
            return fails;
        }

        /// <summary>把实时状态转化为人类可读文本（用于日志/界面提示）。</summary>
        public static string DescribeActual(string kind, string targetName)
        {
            if (kind == "气缸")
            {
                int v = SimRuntime.GetCylinder(targetName);
                return v == 1 ? "伸出" : "缩回";
            }
            var io = ResolveIo(targetName);
            return io.Found ? io.Value.ToString() : "找不到";
        }

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
// ◆◇※▣▤ۦ▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣ۤۦ▧▨▩░ے▓✦✧⚝☢☣➤◈❖◆◇※▣ۤ
