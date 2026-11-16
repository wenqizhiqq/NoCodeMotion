// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;

namespace NoCodeMotion.Services.Hardware.Cards
{
    /// <summary>
    /// 一个卡族的静态描述：怎么匹配、依赖哪些底层 dll、怎么 new 出实现类。
    /// <para>每个卡族由 <c>Modules</c> 目录下一个 <see cref="Modules.CardFamilyModule"/> 子类产出，
    /// 新增 / 删除卡族只需增删对应的模块文件，不再改本目录下的任何代码。</para>
    /// </summary>
    public sealed class CardFamilyDescriptor
    {
        /// <summary>家族键（与移植目录名一致，如 DMC_E3032_EtherCAT）。写进控制器「卡型号」即可精确命中。</summary>
        public string Key { get; set; }

        /// <summary>
        /// 同品牌内的展示 / 兜底匹配优先级，越小越靠前；0 = 未指定（排在已指定的之后，再按 <see cref="Key"/> 排序）。
        /// <para>只在「没填卡型号、只按品牌 + 总线兜底」时影响选中哪一张卡，以及卡型号下拉的顺序。
        /// 新增卡族留 0 即可，不会抢在既有卡族前面。</para>
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>中文显示名。</summary>
        public string DisplayName { get; set; }

        /// <summary>品牌（雷赛 / 升立德 / 恒昱 / 研控 / 模拟卡；无法确证时为空）。</summary>
        public string Vendor { get; set; }

        /// <summary>总线 / 脉冲（雷赛、研控下再分一级）。</summary>
        public string BusKind { get; set; }

        /// <summary>移植后的命名空间（实现类都在这个命名空间下）。</summary>
        public string Namespace { get; set; }

        /// <summary>该卡族底层真正调用的原生库文件名（探测与说明用）。</summary>
        public string[] NativeDlls { get; set; }

        /// <summary>该卡族支持的总线类型。</summary>
        public CardBusType[] BusTypes { get; set; }

        /// <summary>
        /// 匹配「卡型号」的关键字（不区分大小写，取最长命中）。
        /// <para>两类值都要放进来：① 真实型号名（DMC3400 / MCC800S / HY7C00）；
        /// ② <b>固件卡型码</b> —— 「自动识别」对雷赛卡写进「卡型号」的是
        /// <c>$"0x{固件上报的卡型码:X}"</c>（见 <c>AxisControllerViewModel.AutoDetect</c>），
        /// 例如 DMC1000S 会写成 <c>0x2711</c>。少了固件码，混装工程里这张卡会掉到
        /// 「品牌 + 总线」兜底分支，被配上一张**不相干的同品牌卡</b>。
        /// 固件码取自参考实现 <c>SystemHardwareData</c> 里「卡型码 → 卡族」的 switch。</para>
        /// </summary>
        public string[] Aliases { get; set; }

        /// <summary>备注 / 对接说明。</summary>
        public string Note { get; set; }

        /// <summary>
        /// 该卡族的标准规格（设计容量），在底层硬件 API 不回报容量时作为可信兜底。
        /// 0 = 未知（不兜底，沿用硬件返回值或用户配置）。当前仅个别卡族人工核对此值，
        /// 其余卡族保持 0，继续走底层 API 的真实读数。例如研控 MCN42 系列固定为 8 轴 / 16 入 / 16 出，
        /// 但其移植实现 <c>GetAxisCount</c> 离线时返回 0、IO 实体默认填 24，故需目录规格兜底。
        /// </summary>
        public int AxisCount { get; set; }

        /// <summary>该卡族标准规格：主板输入 IO 数（兜底用，0 = 未知）。</summary>
        public int InIoCount { get; set; }

        /// <summary>该卡族标准规格：主板输出 IO 数（兜底用，0 = 未知）。</summary>
        public int OutIoCount { get; set; }

        /// <summary>
        /// 工厂：new 出一整套实现类。
        /// <para>模块不需要自己赋值：<see cref="Modules.CardFamilyRegistry"/> 加载时会按
        /// <see cref="Namespace"/> 用命名约定自动生成（见 <see cref="CardFamilyFactory"/>）。
        /// 只有实现类命名不符合约定的特殊卡族才需要显式指定。</para>
        /// </summary>
        public Func<CardFamilyRuntime> Create { get; set; }

        /// <summary>该卡族依赖的任意一个原生库在程序目录 / 系统路径中是否可见。</summary>
        public bool DllPresent => CardFamilyCatalog.AnyDllPresent(NativeDlls);

        /// <summary>
        /// 该卡族是否为「模拟卡」—— 实现是进程内仿真，不依赖任何真实硬件 / 原生 dll。
        /// <para>判据用登记时的品牌与总线（见模块登记规则「含『虚拟』或『仿真』→ 模拟卡」）：
        /// <c>VirtualMotionCard</c>（虚拟运动卡）与 <c>DigitalTwinCard</c>（数字孪生卡）
        /// 都登记为 <c>Vendor = 模拟卡</c> / <c>BusKind = 虚拟</c>。</para>
        /// <para>★ 模拟卡与真实卡在几处语义不同，桥里必须分开处理，否则会出现
        /// 「指令返回 0 但轴不动」「状态全是 —」这类看起来像 bug 的现象，详见
        /// <c>WenQiZhiCardBridge.EnsureSimReady / BuildSimParam</c>。</para>
        /// </summary>
        public bool IsSimulation =>
            Vendor == "模拟卡" || BusKind == "虚拟" ||
            (Key != null && (Key.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0
                          || Key.IndexOf("DigitalTwin", StringComparison.OrdinalIgnoreCase) >= 0));

        public override string ToString() => Key;
    }
}
