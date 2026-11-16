// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 运动控制卡「家族目录」—— 对外提供卡族查询与匹配，是 UI / 自动识别 / WenQiZhiCardBridge
// 访问卡族的唯一入口。
//
// ★ 卡族清单不在本文件里：每个卡族是 Cards/Modules 下的一个 CardFamilyModule 子类，
//   由 CardFamilyRegistry 反射扫描汇总。因此：
//     新增一种控制卡 = 在 Cards/Families 放实现类 + 在 Cards/Modules 加一个模块文件；
//     删除一种控制卡 = 删除对应模块文件（及不再需要的 Families 目录）。
//   本文件只保留「匹配 / 品牌归一化 / dll 探测」这些与具体卡族无关的通用逻辑。
//
// 每个卡族（Cards/Families/<Key>/）的约定结构：
//   CardRealization : MotionCardBase        —— 卡级操作（初始化 / 打开 / 关闭 / 总线状态）
//   AxisRealization : MotionAxisBase        —— 轴级操作（点位 / 回零 / 使能 / 停止 / 读位置）
//   InioRealization : DigitalInputBase      —— 主板输入
//   OutioRealization: DigitalOutputBase     —— 主板输出
//   Expand*Realizetion : ExpansionInputBase / ExpansionOutputBase —— 扩展 IO（部分卡族才有）
// 实现类由 CardFamilyFactory 按上述类名反射创建，模块里不需要写 new。
//
// ★ 品牌归属来自参考实现自己的枚举与分组规则，不是猜的：
//     ShareData/ShareDatastruct.cs  的 CardTypeEnum = { 模拟卡, 雷赛, 升立德, 恒昱, 研控 }
//     SMotorseWpf/HeaderRegion/ProjectManager/SelectControllerDialog.xaml.cs 的 BuildCardTree()
//   该规则为：卡名含 DMC 且含 E → 雷赛总线；含 DMC 不含 E → 雷赛脉冲；
//             以 PCI/E/F 开头 → 升立德；以 HY 开头 → 恒昱；
//             以 MCC 开头且含 E → 研控总线；以 MCC 开头不含 E → 研控脉冲；
//             含「虚拟」或「仿真」→ 模拟卡。
//   参考实现自己的规则漏掉了 SLD1230/1232 系列与 PLTEI400H（首字母 S / P 都没覆盖），
//   各卡族模块按同一套判据补齐：SLD* 属升立德（sldmv.dll / PCI1230.dll 同族）。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using NoCodeMotion.Services.Hardware.Cards.Modules;

namespace NoCodeMotion.Services.Hardware.Cards
{
    /// <summary>
    /// 已移植运动控制卡族的中央查询入口。
    /// 「控制器 → 卡族实现」的匹配、自动识别扫描、Lua 侧的能力查询都读这里。
    /// 卡族清单本身由 <see cref="CardFamilyRegistry"/> 从 Modules 目录自动汇总。
    /// </summary>
    public static class CardFamilyCatalog
    {
        /// <summary>全部已移植卡族（只读，来自各卡族模块）。</summary>
        public static IReadOnlyList<CardFamilyDescriptor> Families => CardFamilyRegistry.All;

        /// <summary>参考实现里已有的品牌枚举（来自 ShareDatastruct.cs 的 CardTypeEnum），按此顺序展示。</summary>
        public static readonly string[] Vendors = { "雷赛", "升立德", "恒昱", "研控", "模拟卡" };

        /// <summary>某个卡族的原生库是否在程序目录 / 系统路径中可见。</summary>
        public static bool AnyDllPresent(string[] dllNames)
        {
            if (dllNames == null || dllNames.Length == 0) return false;
            foreach (var dll in dllNames)
            {
                try
                {
                    if (File.Exists(Path.Combine(AppContext.BaseDirectory ?? string.Empty, dll))) return true;
                    if (NativeLibrary.TryLoad(dll, out IntPtr h))
                    {
                        if (h != IntPtr.Zero) NativeLibrary.Free(h);
                        return true;
                    }
                }
                catch { /* 探测失败按「没有库」处理 */ }
            }
            return false;
        }

        /// <summary>按家族键精确取卡族；找不到返回 null。</summary>
        public static CardFamilyDescriptor ByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            string k = key.Trim();
            return Families.FirstOrDefault(x => string.Equals(x.Key, k, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 把「控制器配置」匹配到具体卡族。
        /// <para>顺序：① 卡型号关键字（最长命中优先）→ ② 家族键字面量 → ③ 品牌 + 总线类型。</para>
        /// <para>匹配不到返回 null，调用方应回退到别的对接实现（例如雷赛 Leadshine 桥）。</para>
        /// </summary>
        public static CardFamilyDescriptor Resolve(string vendor, string cardType, string busType)
        {
            string t = (cardType ?? string.Empty).Trim();

            // ① 卡型号关键字：取最长命中，避免 "1000" 抢走本该给 "1000S" 的匹配
            if (t.Length > 0)
            {
                CardFamilyDescriptor best = null;
                int bestLen = 0;
                foreach (var f in Families)
                {
                    if (f.Aliases == null) continue;
                    foreach (var a in f.Aliases)
                    {
                        if (string.IsNullOrEmpty(a)) continue;
                        if (a.Length <= bestLen) continue;
                        if (t.IndexOf(a, StringComparison.OrdinalIgnoreCase) >= 0) { best = f; bestLen = a.Length; }
                    }
                }
                if (best != null) return best;

                // ② 用户直接把家族键写进了「卡型号」
                var byKey = ByKey(t);
                if (byKey != null) return byKey;
            }

            // ③ 只给了品牌：按品牌 + 总线类型挑
            if (!string.IsNullOrWhiteSpace(vendor))
            {
                string v = NormalizeVendor(vendor);
                var cands = Families.Where(x => x.Vendor == v).ToList();
                if (cands.Count > 0)
                {
                    bool wantBus = IsBusType(busType);
                    if (wantBus)
                    {
                        // 只给了「品牌 + 总线」时，同一品牌往往有好几张支持 EtherCAT 的卡
                        // （研控 MCC400S / MCC800S 兼脉冲，MCCE3032 是纯总线主站）。
                        // 先挑「纯总线」的（不兼脉冲），也就是专职总线主站；没有再退到任何支持总线的。
                        return cands.FirstOrDefault(x => x.BusTypes.Contains(CardBusType.EtherCAT)
                                                        && !x.BusTypes.Contains(CardBusType.Pulse))
                            ?? cands.FirstOrDefault(x => x.BusTypes.Contains(CardBusType.EtherCAT))
                            ?? cands[0];
                    }
                    // 只给了「品牌 + 脉冲」时，优先挑「纯脉冲」卡族（声明支持脉冲、但不含 EtherCAT 主站），
                    // 否则会把脉冲卡匹配到总线主站实现上 —— 例如雷赛 DMC5400（PCI 脉冲）会落到
                    // DMC_E3032_EtherCAT 家族，于是「设零点」走了总线用的 dmc_set_position_unit 而报错。
                    return cands.FirstOrDefault(x => x.BusTypes.Contains(CardBusType.Pulse)
                                                    && !x.BusTypes.Contains(CardBusType.EtherCAT))
                        ?? cands.FirstOrDefault(x => x.BusTypes.Contains(CardBusType.Pulse))
                        ?? cands[0];
                }
            }

            return null;
        }

        /// <summary>
        /// 兼容历史工程的品牌写法：早期版本把仿真卡登记为「虚拟」，现统一叫「模拟卡」。
        /// </summary>
        public static string NormalizeVendor(string vendor)
        {
            string v = (vendor ?? string.Empty).Trim();
            return v == "虚拟" ? "模拟卡" : v;
        }

        /// <summary>总线类型字符串是否表示「总线卡」（否则按脉冲卡处理）。</summary>
        public static bool IsBusType(string busType)
        {
            string s = busType ?? string.Empty;
            if (s.Contains("脉冲")) return false;
            return s.Contains("总线")
                || s.IndexOf("EtherCAT", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("CANopen", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Profinet", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>扫一遍所有卡族，返回底层库可见（即驱动已装 / dll 已随程序输出）的那些。</summary>
        public static List<CardFamilyDescriptor> DetectPresent()
        {
            var list = new List<CardFamilyDescriptor>();
            foreach (var f in Families)
            {
                try { if (f.DllPresent) list.Add(f); }
                catch { /* 单个卡族探测失败不影响其它 */ }
            }
            return list;
        }
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
