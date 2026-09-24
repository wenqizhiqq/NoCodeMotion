// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​
using System;
using System.Linq;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// IO「功能」目录：输入 IO 与输出 IO 各自一套可选功能，默认「无」。
    /// <para>这些功能是<b>真实有效</b>的，由 <see cref="ViewModels.OperatorViewModel"/> 的 150ms UI 定时器周期执行：</para>
    /// <list type="bullet">
    /// <item>安全类输入（急停按钮 / 安全门 / 光栅）：逻辑值有效（1，已按「电平」取反）→ 触发急停。</item>
    /// <item>指示类输出（运行 / 就绪 / 报警 / 三色灯 / 蜂鸣器）：按设备运行状态自动输出。</item>
    /// </list>
    /// </summary>
    public static class IoFunctionCatalog
    {
        /// <summary>默认功能：不做任何自动行为。</summary>
        public const string None = "无";

        /// <summary>输入 IO 可选功能。</summary>
        public static readonly string[] InputFunctions =
        {
            None, "急停按钮", "安全门", "光栅", "原点开关", "正限位", "负限位",
            "启动按钮", "停止按钮", "复位按钮", "暂停按钮", "手自动切换",
            "到位信号", "来料检测", "气压检测", "报警反馈",
        };

        /// <summary>输出 IO 可选功能。</summary>
        public static readonly string[] OutputFunctions =
        {
            None, "轴使能", "运行指示", "就绪指示", "报警指示",
            "三色灯-红", "三色灯-绿", "三色灯-黄", "蜂鸣器",
            "气缸伸出", "气缸缩回", "真空吸", "真空破", "夹爪夹紧", "夹爪松开", "电磁阀",
        };

        /// <summary>安全类输入：逻辑值为 1 时请求急停。</summary>
        public static readonly string[] SafetyInputFunctions = { "急停按钮", "安全门", "光栅" };

        /// <summary>指示类输出：随设备运行 / 报警状态自动输出。</summary>
        public static readonly string[] IndicatorOutputFunctions =
        { "运行指示", "就绪指示", "报警指示", "三色灯-红", "三色灯-绿", "三色灯-黄", "蜂鸣器" };

        /// <summary>该功能在对应方向下是否合法。</summary>
        public static bool IsValid(string? function, bool isInput)
        {
            if (string.IsNullOrWhiteSpace(function)) return false;
            return (isInput ? InputFunctions : OutputFunctions).Contains(function);
        }

        /// <summary>非法 / 空值一律归一到「无」（旧工程里的「动点」等会变成「无」）。</summary>
        public static string Normalize(string? function, bool isInput)
            => IsValid(function, isInput) ? function! : None;

        public static bool IsSafetyInput(string? function)
            => !string.IsNullOrEmpty(function) && Array.IndexOf(SafetyInputFunctions, function) >= 0;

        public static bool IsIndicatorOutput(string? function)
            => !string.IsNullOrEmpty(function) && Array.IndexOf(IndicatorOutputFunctions, function) >= 0;

        /// <summary>指示类输出的目标值（按设备状态推导）；返回 -1 表示该功能不自动驱动。</summary>
        public static int DesiredIndicator(string? function, bool running, bool estop) => function switch
        {
            "运行指示" => running && !estop ? 1 : 0,
            "三色灯-绿" => running && !estop ? 1 : 0,
            "就绪指示" => !running && !estop ? 1 : 0,
            "报警指示" => estop ? 1 : 0,
            "三色灯-红" => estop ? 1 : 0,
            "蜂鸣器" => estop ? 1 : 0,
            "三色灯-黄" => 0,
            _ => -1,
        };
    }
}
