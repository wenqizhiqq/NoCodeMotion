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
        /// <summary>返回未满足的条件说明；空列表表示全部满足（或无条件）。</summary>
      

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
