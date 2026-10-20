// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁣志​◆‎编‌写⁠◇‍微⁠信‍﹕‎1⁣8​7‍◆‌1⁣9‍3‍6‏◇‌1‍3‌9⁣9⁣　‏※⁣保‍留⁠所‎有​权​利‎请⁣勿⁠删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.Generic;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 流程词汇表：界面上的白话名 ⇄ 引擎内部认的名字。
    ///
    /// 为什么要两张名：
    ///   引擎里（FlowRunnerService / SimFlowPlayer / VisionEngine / NgRunner）到处是按名字
    ///   switch 的分支，改一个词要翻十几个文件、漏一处就是静默走 default 分支（不报错，只是不动）。
    ///   所以**内部名一个不动**，只在「读用户存进来的名字」这个边界上翻译一次；
    ///   界面下拉、工程 xlsx、AI 契约里全部用白话显示名。
    ///
    /// 三个方向都要幂等：
    ///   ToInternal*(存的什么名)  —— 存的是内部名 → 原样返回；是显示名 → 换内部名；认不出 → 原样返回（老行为）。
    ///   ToDisplay*(存的什么名)   —— 迁移用：内部名 → 显示名；已经是显示名 → 原样。
    ///   Migrate(ProjectData)     —— 打开老工程时把存值统一刷成显示名，保存后 xlsx 里就是白话。
    ///
    /// ★ 「复位」一个词在两个上下文里意思不同：输出点上的复位 = 断电（显示名「关闭」），
    ///   气缸上的复位 = 缩回归位（显示名仍叫「复位」）。所以运算名必须带功能一起判。
    /// </summary>
    public static class FlowVocab
    {
        // ===================== 词表 =====================

        private static readonly (string Display, string Internal)[] FunctionTable =
        {
            ("输入信号", "输入IO"),
            ("输出信号", "输出IO"),
            ("外部通讯", "modbus"),
            ("外部通讯", "Modbus"),
            ("系统状态", "系统"),
            ("等待", "延时"),
        };

        private static readonly (string Display, string Internal)[] OperationTable =
        {
            ("走到位置", "绝对移动"),
            ("挪多少", "相对移动"),
            ("回原点", "回零"),
            ("停下", "停止"),
            ("设为", "修改为"),
            ("设为", "修改"),
            ("打开", "置位"),
            ("关闭", "复位"),
            ("加上", "加"),
            ("减去", "减"),
            ("乘以", "乘"),
            ("除以", "除"),
            ("求余数", "取模"),
            ("真假互换", "取反"),
        };

        /// <summary>气缸专属：这几个显示名只属于气缸，不参与「复位 → 关闭」那条上下文判断。</summary>
        private static readonly string[] CylinderOnlyFunctions = { "气缸" };

        private static readonly (string Display, string Internal)[] PreOpTable =
        {
            ("变黑白图", "灰度化"),
            ("分成黑白两块", "二值化"),
            ("抹掉细碎噪点", "高斯平滑"),
            ("去掉麻点噪点", "中值滤波"),
            ("白色变瘦", "腐蚀"),
            ("白色变胖", "膨胀"),
            ("去掉小白点", "开运算"),
            ("补上小黑洞", "闭运算"),
            ("只留边缘线", "Sobel边缘"),
            ("暗处拉亮", "直方图均衡"),
            ("只留框住的那块", "ROI裁剪"),
            ("两张图相加", "图像加"),
            ("两张图相减", "图像减"),
            ("两张图都有的留", "图像与"),
            ("两张图合起来", "图像或"),
        };

        // ===================== 下拉候选（界面直接用这里，避免两处各写一份） =====================

        /// <summary>「功能」列候选（显示名，顺序即下拉顺序）。</summary>
        public static readonly string[] FunctionChoices =
            { "轴", "输入信号", "输出信号", "气缸", "点位", "外部通讯", "变量", "系统状态", "相机", "等待" };

        /// <summary>运动指令（轴 + 位置）。</summary>
        public static readonly string[] MoveOps = { "走到位置", "挪多少", "回原点", "停下" };
        /// <summary>设参数（轴 + 速度/扭矩…）。</summary>
        public static readonly string[] SetOps = { "设为", "加上", "减去", "乘以", "除以", "求余数" };
        /// <summary>写输出（输出信号 / 外部通讯）。</summary>
        public static readonly string[] IoWriteOps = { "设为", "打开", "关闭" };
        /// <summary>气缸动作。</summary>
        public static readonly string[] CylOps = { "伸出", "缩回", "复位" };
        /// <summary>状态判读（如果 / 否则如果 的分支条件）。</summary>
        public static readonly string[] CondOps = { "等于", "大于", "小于", "大于等于", "小于等于", "真假互换" };
        /// <summary>变量（数值）能做的事。</summary>
        public static readonly string[] VarNumOps =
            { "设为", "加上", "减去", "乘以", "除以", "求余数", "真假互换", "等于", "大于", "小于", "大于等于", "小于等于" };
        /// <summary>变量（字符串）。</summary>
        public static readonly string[] VarStrOps = { "设为", "等于", "大于", "小于" };
        /// <summary>变量（布尔）。</summary>
        public static readonly string[] VarBoolOps = { "设为", "真假互换", "等于" };

        /// <summary>图像预处理算子（视觉流程 / 节点图视觉节点共用，顺序即下拉顺序）。</summary>
        public static readonly string[] PreOpChoices =
        {
            "无", "变黑白图", "分成黑白两块", "抹掉细碎噪点", "去掉麻点噪点",
            "白色变瘦", "白色变胖", "去掉小白点", "补上小黑洞",
            "只留边缘线", "暗处拉亮", "只留框住的那块",
            "两张图相加", "两张图相减", "两张图都有的留", "两张图合起来",
        };

        // ===================== 翻译 =====================

        public static string ToInternalFunction(string? stored) => Map(stored, DisplayOf(FunctionTable));
        public static string ToDisplayFunction(string? stored) => Map(stored, InternalOf(FunctionTable));

        /// <summary>运算名 → 引擎内部名。<paramref name="function"/> 用来区分气缸的「复位」。</summary>
        public static string ToInternalOperation(string? stored, string? function)
        {
            string f = ToInternalFunction(function);
            if (IsCylinder(f) && string.Equals((stored ?? "").Trim(), "复位", StringComparison.Ordinal)) return "复位";
            return Map(stored, DisplayOf(OperationTable));
        }

        /// <summary>存值 → 显示名（迁移用）。输出点上的「复位」显示成「关闭」，气缸上的仍显示「复位」。</summary>
        public static string ToDisplayOperation(string? stored, string? function)
        {
            string s = (stored ?? "").Trim();
            if (s.Length == 0) return s;
            string f = ToInternalFunction(function);
            // 「复位」一个词两种意思：气缸上是缩回归位（保持原词），输出点上是断电（显示成「关闭」）。
            // 早返回，别让它再落到下面那张表里被统一翻成「关闭」。
            if (string.Equals(s, "复位", StringComparison.Ordinal)) return IsCylinder(f) ? "复位" : "关闭";
            var map = InternalOf(OperationTable);
            return map.TryGetValue(s, out var d) ? d : s;
        }

        public static string ToInternalPreOp(string? stored) => Map(stored, DisplayOf(PreOpTable));
        public static string ToDisplayPreOp(string? stored) => Map(stored, InternalOf(PreOpTable));

        private static bool IsCylinder(string internalFunction) => Array.IndexOf(CylinderOnlyFunctions, internalFunction) >= 0;

        /// <summary>
        /// 按表翻译。表里没有的名字原样返回：老工程里的自定义写法、
        /// 引擎本来就认的英文写法（home / set / rel…）都靠这条兜底。
        /// </summary>
        private static string Map(string? stored, Dictionary<string, string> table)
        {
            string s = (stored ?? "").Trim();
            if (s.Length == 0) return s;
            return table.TryGetValue(s, out var v) ? v : s;
        }

        /// <summary>显示名 → 内部名。同一个显示名对应多个内部写法时（「设为」= 修改为 / 修改），
        /// 只认表里第一个：后写的会把先写的悄悄盖掉，「设为」就会翻成「修改」而落空。</summary>
        private static Dictionary<string, string> DisplayOf((string Display, string Internal)[] t)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (display, internalName) in t)
                if (!d.ContainsKey(display)) d[display] = internalName;
            return d;
        }

        private static Dictionary<string, string> InternalOf((string Display, string Internal)[] t)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (display, internalName) in t) d[internalName] = display;    // 内部名 → 显示名（后写的胜出）
            return d;
        }

        // ===================== 老工程迁移 =====================

        /// <summary>
        /// 把工程里存的流程词汇统一刷成白话显示名（打开工程时调用；幂等，重复调用无副作用）。
        /// 只动「功能 / 运算 / 视觉预处理算子」三类字符串，不碰对象名和数值。
        /// </summary>
        public static int Migrate(ProjectData? data)
        {
            if (data?.Flows == null) return 0;
            int touched = 0;
            foreach (var flow in data.Flows) touched += MigrateFlow(flow);
            return touched;
        }

        /// <summary>
        /// 刷单个流程（<see cref="Migrate"/> 与 AI「粘贴生成」共用这一份：
        /// 两条路径各写一遍的话，漏掉的那条会让下拉框出现候选里没有的选中值，界面显示成空白）。
        /// </summary>
        public static int MigrateFlow(FlowItem? flow)
        {
            int touched = 0;
            if (flow?.Steps != null)
                foreach (var s in flow.Steps)
                {
                    if (s == null) continue;
                    string func = ToDisplayFunction(s.Function);
                    string op = ToDisplayOperation(s.Operation, s.Function);
                    if (func != s.Function) { s.Function = func; touched++; }
                    if (op != s.Operation) { s.Operation = op; touched++; }
                }
            if (flow?.VisualSteps != null)
                foreach (var v in flow.VisualSteps)
                {
                    if (v == null) continue;
                    string pre = ToDisplayPreOp(v.PreOp);
                    if (pre != v.PreOp) { v.PreOp = pre; touched++; }
                }
            return touched;
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
