// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 运动控制卡对接实现：把移植进来的 WenQiZhi.Domain.MotionCard.Common 卡族层
// （CardRealization : ICard / AxisRealization : IAxis / InioRealization : IIOInPut /
//   OutioRealization : IIOOutPut / Expand* : IEIOInPut / IEIOOutPut）
// 接到 NoCodeMotion 的 IHardwareBridge 上，让 Lua 的 AxisMove / SetIO 等真正下发到卡。
//
// 与 LeadshineHardwareBridge 的分工：
//   - LeadshineHardwareBridge 直接 P/Invoke LTDMC.dll（自有 LtdmcCard 封装，只覆盖雷赛）。
//   - 本类走「卡族目录 → 具体实现类」这一层，因此 26 个卡族（雷赛 / 升立德 / 恒昱 / 研控 /
//     模拟卡）全部可用，扩展 IO 也一并接通。底层 dll 调用全部落在移植进来的卡族代码里。
//
// 匹配规则（控制器 → 卡族）见 CardFamilyCatalog.Resolve：
//   控制器「卡型号」关键字最长命中 → 家族键字面量 → 品牌 + 总线类型。
//   匹配不到就回退：本类会明确报「未匹配到已移植卡族」，不静默乱发指令。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using MoonSharp.Interpreter;
using NoCodeMotion.Models;
using NoCodeMotion.Services.Hardware.Comm;
using NoCodeMotion.Services.Hardware.Leadshine;   // HardwareOperationException
using WenQiZhi.Domain.MotionCard.Common;

namespace NoCodeMotion.Services.Hardware.Cards
{
    /// <summary>
    /// 真实硬件对接：轴 / IO 走移植进来的 26 个运动控制卡族，通讯走真实串口 / 网口 / Modbus
    /// （<see cref="CommManager"/>）。
    ///
    /// 启用方式（程序启动处，或「硬件设置」里切换）：
    /// <code>
    /// HardwareBridge.Current = new WenQiZhiCardBridge(msg => 输出日志(msg));
    /// </code>
    ///
    /// 设计原则：
    ///   - 卡族匹配不上 / 卡没插 / 底层库缺失时都不崩：轴 IO 动作只记警告日志，
    ///     通讯部分照样真实可用，方便先接 PLC 调流程。
    ///   - 出错信息全中文，直接显示在 Lua 输出面板。
    ///   - 一个控制器只初始化一次（懒加载），失败也记住状态，避免每次动作都重试一遍插卡。
    /// </summary>
    public sealed class WenQiZhiCardBridge : IHardwareBridge, IDisposable
    {
        /// <summary>可调参数（按现场接线习惯改这里即可）。</summary>
        public static class Options
        {
            /// <summary>等待轴到位 / 回零的最长时间。</summary>
            public static int AxisWaitTimeoutMs = 60000;

            /// <summary>等待 IO / 气缸的默认最长时间。</summary>
            public static int IoWaitTimeoutMs = 30000;

            /// <summary>轮询间隔。</summary>
            public static int PollIntervalMs = 5;

            /// <summary>主板上一个 IO 模块占多少位（按「整卡位号」访问扩展 IO 时的换算基数）。</summary>
            public static ushort BitsPerModule = 16;

            /// <summary>
            /// 是否把「模块」列当扩展 IO 从站号。
            ///   null（默认）/ true = 自动：IO 行的「模块」> 0 且该卡族支持扩展 IO 时，走扩展模块访问；
            ///   false             = 一律按主板整卡位号访问（位号 = 模块 × <see cref="BitsPerModule"/> + 序号）。
            /// 之所以给这个开关：IO 表里的「模块」列在有些现场是从 1 开始编号主板模块的，
            /// 自动判断会把它误当成扩展从站。接线与表格不一致时显式设为 false。
            /// </summary>
            public static bool? UseExpansionIo = null;

            /// <summary>
            /// 扩展 IO 的节点号基准。
            /// <para>0（默认）= 直接用 IO 行上填的「模块」列当节点号。</para>
            /// <para>雷赛 EtherCAT 扩展模块的节点号按手册「从 1001 开始」；若表里填的是 1、2、3…
            /// 这种从站序号，把它设成 1001 会自动换算。</para>
            /// </summary>
            public static int ExpansionNodeBase = 0;
        }

        /// <summary>一个控制器对应的卡族运行期状态。</summary>
        private sealed class CardSlot
        {
            public string ControllerName;
            public AxisControllerItem Config;
            public CardFamilyDescriptor Family;
            public CardFamilyRuntime Runtime;

            /// <summary>是否已成功初始化（InitCard + OpenCard）。</summary>
            public bool Ready;

            /// <summary>初始化结果说明（成功 / 失败原因），用于界面与日志。</summary>
            public string Status = "尚未初始化";

            /// <summary>实际卡号。</summary>
            public int CardNo;

            /// <summary>轴号 → 轴实现实例。每个轴一个实例（AxisID / AxisWhichCardNo 是实例字段）。</summary>
            public readonly Dictionary<int, IAxis> Axes = new Dictionary<int, IAxis>();

            /// <summary>该槽位的初始化串行锁：让慢初始化（InitCard / OpenCard）在 <c>_gate</c> 之外执行，
            /// 避免 UI 线程读 IsControllerReady / ControllerStatus 时被一起卡住（连接时界面卡死的根因）。</summary>
            public readonly object InitLock = new object();
        }

        private readonly Action<string> _log;
        private readonly CommManager _comm = new CommManager();
        private readonly ConcurrentDictionary<string, int> _trayIndex = new ConcurrentDictionary<string, int>();
        private readonly Dictionary<string, CardSlot> _slots = new Dictionary<string, CardSlot>(StringComparer.Ordinal);
        private readonly object _gate = new object();

        private bool _warnedNoController;
        private bool _warnedUnmatched;
        private bool _warnedExpansion;

        public WenQiZhiCardBridge(Action<string> log = null)
        {
            _log = log;
            _comm.Log = Log;   // 未传回调时统一走 HardwareLog（Lua 运行期会指向输出面板）
            Log("[卡族] 已加载 " + CardFamilyCatalog.Families.Length + " 个已移植运动控制卡族："
                + string.Join("、", CardFamilyCatalog.Families.Select(f => f.Key)));
        }

        public void Log(string message)
        {
            if (_log != null) _log(message);
            else HardwareLog.Write(message);
        }

        // ===================== 控制器 → 卡族 解析与初始化 =====================

        /// <summary>在工程里按名称找控制器；找不到（或工程还没加载）返回 null，不抛异常。</summary>
        private static AxisControllerItem FindController(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            try
            {
                var list = ProjectStore.Data?.Controllers;
                return list?.FirstOrDefault(c => c != null && c.Name == name);
            }
            catch { return null; }
        }

        /// <summary>
        /// 取某个对象（轴 / IO）归属的控制器。
        /// 没填「归属控制器」时，若工程里正好只有一个控制器，就用它；否则返回 null。
        /// </summary>
        private AxisControllerItem ControllerOf(string controllerName, string what)
        {
            var ctl = FindController(controllerName);
            if (ctl != null) return ctl;

            List<AxisControllerItem> all = null;
            try { all = ProjectStore.Data?.Controllers?.Where(c => c != null).ToList(); }
            catch { /* 工程未加载 */ }

            if (all != null && all.Count == 1) return all[0];

            if (!_warnedNoController)
            {
                _warnedNoController = true;
                Log("[卡族·未执行] 对象没有指定「归属控制器」，工程里也不是只有一个控制器，无法确定用哪张卡。"
                    + "请到「控制器」页面添加控制器，并在轴 / IO 行的「控制器」列填上它的名称。");
            }
            Log($"[卡族·未执行] {what}：未找到归属控制器（当前填的是「{controllerName}」）。");
            return null;
        }

        /// <summary>取（必要时初始化）某个控制器的卡族运行期状态；不可用时返回 null。</summary>
        private CardSlot SlotOf(AxisControllerItem ctl, string what)
        {
            if (ctl == null) return null;

            CardSlot slot;
            lock (_gate)
            {
                if (!_slots.TryGetValue(ctl.Name, out slot))
                {
                    slot = new CardSlot { ControllerName = ctl.Name, Config = ctl };
                    _slots[ctl.Name] = slot;
                }

                // 已尝试过就复用（成功失败都算），避免每次动作都重试一遍插卡。
                if (slot.Runtime != null) return slot;
            }

            // ★ 慢初始化放 _gate 之外，避免持锁卡住 UI 线程的状态查询。
            lock (slot.InitLock)
            {
                if (slot.Runtime == null) Initialize(slot, what);
            }
            return slot;
        }

        /// <summary>初始化一个控制器：匹配卡族 → new 实现类 → InitCard → OpenCard。</summary>
        private void Initialize(CardSlot slot, string what)
        {
            var ctl = slot.Config;
            var family = CardFamilyCatalog.Resolve(ctl.Vendor, ctl.CardType, ctl.BusType);
            slot.Family = family;

            if (family == null)
            {
                slot.Ready = false;
                slot.Status = "未匹配到已移植卡族";
                if (!_warnedUnmatched)
                {
                    _warnedUnmatched = true;
                    Log("[卡族·未执行] 有控制器的「品牌 + 卡型号」匹配不到任何已移植卡族，轴 / IO 动作将只记录日志。"
                        + "可选的卡族键：" + string.Join("、", CardFamilyCatalog.Families.Select(f => f.Key))
                        + "。把控制器「卡型号」改成上面任一个（或它支持的关键字，如 DMC3400 / MCC800S / HY7C00）即可对上。");
                }
                Log($"[卡族·未执行] 控制器「{ctl.Name}」（品牌={ctl.Vendor} 卡型号={ctl.CardType} 总线={ctl.BusType}）"
                    + $"未匹配到卡族，{what} 只记录不执行。");
                return;
            }

            // 底层库缺失先明确报出来：卡族代码编译得过，但插卡后一定驱动不了。
            if (family.NativeDlls != null && family.NativeDlls.Length > 0 && !family.DllPresent)
            {
                Log($"[卡族·警告] 控制器「{ctl.Name}」匹配到卡族 {family.Key}（{family.DisplayName}），"
                    + $"但它依赖的底层库 {string.Join(" / ", family.NativeDlls)} 在程序目录里找不到，"
                    + "初始化会失败。请把对应 dll 放到程序目录（本工程 Native\\ 下的会随输出自动复制）。");
            }

            try
            {
                slot.Runtime = family.Create();
                slot.Runtime.Family = family;
            }
            catch (Exception ex)
            {
                slot.Ready = false;
                slot.Status = "创建卡族实现失败：" + ex.Message;
                Log($"[卡族] 控制器「{ctl.Name}」创建 {family.Key} 实现失败：{ex.Message}，{what} 只记录不执行。");
                return;
            }

            slot.CardNo = Math.Max(ctl.CardNo, 0);

            // 模拟卡族的 CardRealization 在 IsVitualCard = true 时会跳过真实的初始化 / 打开卡，
            // 直接置位并返回成功 —— 脱机调试时正是我们要的。
            bool isSimulation = family.Key == "VirtualMotionCard" || family.Key == "DigitalTwinCard";
            try { slot.Runtime.Card.IsVitualCard = isSimulation; } catch { /* 少数卡族该属性只读 */ }

            // ★ 预置卡参数表：部分卡族（如 DMC3400A 的 DMC3000 系列分支）在 OpenCard 里会按下标
            //   访问 ListCardParam[i]，表是空的话直接 IndexOutOfRange，卡根本打不开。
            //   参考实现是在它自己的硬件配置界面里填这张表的，NoCodeMotion 没有那个界面，
            //   所以这里按控制器上配的「轴数量」补一份最小可用表（卡号 + 轴数）。
            //   配置文件 / 总线周期等更细的卡参数仍需按厂商手册在卡侧设定，本表不负责下发。
            SeedCardParams(slot);

            int initRes;
            try
            {
                initRes = slot.Runtime.Card.InitCard();
            }
            catch (Exception ex)
            {
                slot.Ready = false;
                slot.Status = "InitCard 异常：" + ex.Message;
                Log($"[卡族] 控制器「{ctl.Name}」（{family.Key}）初始化失败：{ex.Message}，{what} 只记录不执行。");
                return;
            }

            // 打开卡并读回实际卡数与卡号。数组按 16 张预留，SDK 会回填真实数量。
            ushort cardNum = 16;
            uint[] cardTypes = new uint[16];
            ushort[] cardIds = new ushort[16];
            int openRes = -1;
            try
            {
                openRes = slot.Runtime.Card.OpenCard(ref cardNum, ref cardTypes, ref cardIds);
            }
            catch (Exception ex)
            {
                // 部分卡族在没插卡时 OpenCard 会抛（数组越界 / 句柄无效），按「打不开」处理。
                Log($"[卡族] 控制器「{ctl.Name}」（{family.Key}）打开卡时异常：{ex.Message}");
            }

            // 实际卡号：优先用回读到的第一张卡，其次用配置里的卡号。
            if (cardIds != null && cardIds.Length > 0 && cardIds[0] != 0) slot.CardNo = cardIds[0];

            slot.Ready = initRes >= 0 && openRes == 0;
            slot.Status = slot.Ready
                ? $"已连接（卡族 {family.Key}，卡号 {slot.CardNo}，共 {cardNum} 张卡）"
                : $"未就绪（卡族 {family.Key}，InitCard={initRes}，OpenCard={openRes}）";

            Log($"[卡族] 控制器「{ctl.Name}」{slot.Status}。{family.Note}");
            if (slot.Ready && cardTypes != null && cardTypes.Length > 0 && cardTypes[0] != 0)
                Log($"[卡族] 控制器「{ctl.Name}」固件卡型 = 0x{cardTypes[0]:X}（可用于确认卡型号是否填对）。");
        }

        /// <summary>
        /// 按控制器配置补一份最小的卡参数表（见 <see cref="Initialize"/> 里的说明）。
        /// 表非空时不动，避免覆盖厂商 SDK 或后续流程填进去的值。
        /// </summary>
        private void SeedCardParams(CardSlot slot)
        {
            try
            {
                var list = slot.Runtime.Card.ListCardParam;
                if (list == null)
                {
                    slot.Runtime.Card.ListCardParam = list = new List<CardParamModel>();
                }
                if (list.Count > 0) return;

                int axisCount = Math.Max(slot.Config?.AxisCount ?? 0, 1);
                for (int i = 0; i < Math.Max(axisCount, 4); i++)
                {
                    list.Add(new CardParamModel
                    {
                        CardName = slot.Family.Key,
                        CardNo = slot.CardNo + i,
                        CardRemarks = "NoCodeMotion 自动补的最小卡参数表（卡号 + 轴数）",
                        CardSuportAxisNum = (ushort)axisCount,
                        InportNum = 4,
                        OutportNum = 4,
                        ControllerMode = 1,
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"[卡族] 控制器「{slot.ControllerName}」预置卡参数表失败（不影响后续初始化）：{ex.Message}");
            }
        }

        /// <summary>取某个轴对应的 (卡槽, 轴实现)；不可用时返回 (卡槽, null)。</summary>
        private (CardSlot slot, IAxis axis) AxisOf(AxisItem axis)
        {
            if (axis == null) return (null, null);

            var ctl = ControllerOf(axis.Controller, $"轴「{axis.Name}」");
            var slot = SlotOf(ctl, $"轴「{axis.Name}」");
            if (slot == null || !slot.Ready) return (slot, null);

            int no = Math.Max(axis.AxisNo, 0);
            lock (_gate)
            {
                if (slot.Axes.TryGetValue(no, out var existing)) return (slot, existing);

                IAxis a;
                try { a = slot.Runtime.NewAxis(); }
                catch (Exception ex)
                {
                    Log($"[卡族] 轴「{axis.Name}」创建轴对象失败：{ex.Message}");
                    return (slot, null);
                }

                // ★ 这几个字段必须逐轴设置：卡族里 GetCardAxisCurrentPosition / CardAxisHomeMove /
                //   GetCardAxisCurrentState 都不带 CardNo / axis 参数，直接读 AxisWhichCardNo 与 AxisID。
                //   共用实例会把轴号串到一起。
                a.AxisName = axis.Name;
                a.AxisWhichCardNo = slot.CardNo;
                a.AxisWhichCardName = slot.Family.Key;
                a.AxisWhichcardType = slot.Config?.CardType ?? string.Empty;
                a.AxisID = no;
                a.Online = true;
                a.IsVitualCard = slot.Family.Key == "VirtualMotionCard" || slot.Family.Key == "DigitalTwinCard";
                slot.Axes[no] = a;
                return (slot, a);
            }
        }

        /// <summary>取某个 IO 点归属的 (卡槽, 是否走扩展模块)；不可用时返回 (卡槽, false)。</summary>
        private (CardSlot slot, bool expansion) IoOf(IoItem io)
        {
            if (io == null) return (null, false);

            var ctl = ControllerOf(io.Controller, $"IO「{io.Name}」");
            var slot = SlotOf(ctl, $"IO「{io.Name}」");
            if (slot == null || !slot.Ready) return (slot, false);

            bool expansion = Options.UseExpansionIo != false && io.ModuleNo > 0 && slot.Runtime.HasExpansionIo;
            return (slot, expansion);
        }

        // ===================== 轴 =====================

        public void MoveAxis(AxisItem axis)
        {
            // AxisItem 没有「目标位置」字段，单独的 AxisMove 无法确定终点，
            // 因此这里只提示改用带位置的函数，避免误动作撞机。
            Log($"[卡族] 轴「{axis.Name}」调用了 AxisMove，但未指定目标位置。请改用 MoveAxisAbs(\"{axis.Name}\", 目标位置) 或 MoveAxisRel(\"{axis.Name}\", 位移)。");
        }

        public void SetAxisSpeed(AxisItem axis, double speed)
        {
            var (slot, a) = AxisOf(axis);
            if (a == null) { WarnNoAxis(axis, $"设速 {speed}"); return; }

            int res = 0;
            Guard(() => res = a.SetCardAxisMotionalVel(BuildMotionParam(axis, slot.CardNo, 0, 1, speed)));
            Report("轴设速", res, $"[卡族] 轴「{axis.Name}」速度已设为 {speed} {axis.Unit}/s（卡{slot.CardNo} 轴{axis.AxisNo}）");
        }

        public void EnableAxis(AxisItem axis)
        {
            var (slot, a) = AxisOf(axis);
            if (a == null) { WarnNoAxis(axis, "使能"); return; }

            if (IsSimulation(slot))
            {
                // 模拟卡的 OpenCardAxisEnable 未实现（恒返回 -1），使能要走「伺服使能端口」——
                // 它有状态（CardAxisWriteSevonPin / GetCardAxisSevonPin），状态卡的「使能」行读的也是它。★ 模拟卡 SDK 约定 active-low SON：写 0=使能、读回 0=使能（见 VirtualCardSDK「返回 1（失能）」）；写 1 反被当失能、状态恒显已使能。
                int sevon = 0;
                Guard(() => sevon = a.CardAxisWriteSevonPin(0));
                Report("轴使能", sevon, $"[卡族·模拟卡] 轴「{axis.Name}」已使能（伺服使能端口置 0＝低电平 active-low SON）");
                return;
            }

            int res = 0;
            Guard(() => res = a.OpenCardAxisEnable(slot.CardNo, Math.Max(axis.AxisNo, 0)));
            Report("轴使能", res, $"[卡族] 轴「{axis.Name}」已使能（卡{slot.CardNo} 轴{axis.AxisNo}，卡族 {slot.Family.Key}）");

            // 用「控制器上配的总线类型」判断，而不是卡族声明支持哪些总线 ——
            // 卡族通常同时声明脉冲与总线（同一套实现两种卡都能带），按声明判断会把脉冲轴也当总线轴。
            if (CardFamilyCatalog.IsBusType(slot.Config?.BusType))
            {
                // 总线伺服的 CiA402 状态机 2→3→4 不是瞬间完成的，立刻读往往还停在 2，等一下再报。
                Thread.Sleep(200);
                ReportBusAxisState(axis.Name, slot, a);
            }
        }

        public void MoveAxisRel(AxisItem axis, double distance)
        {
            var (slot, a) = AxisOf(axis);
            if (a == null) { WarnNoAxis(axis, $"相对移动 {distance}"); return; }
            Guard(() => SendPointMove(slot, a, axis, distance, 0, axis.Speed));   // 0 = 相对
            Log($"[卡族] 轴「{axis.Name}」相对移动 {distance} {axis.Unit}（卡{slot.CardNo} 轴{axis.AxisNo}）");
        }

        public void MoveAxisAbs(AxisItem axis, double position)
        {
            CheckSoftLimit(axis, position);
            var (slot, a) = AxisOf(axis);
            if (a == null) { WarnNoAxis(axis, $"绝对移动到 {position}"); return; }
            Guard(() => SendPointMove(slot, a, axis, position, 1, axis.Speed));   // 1 = 绝对
            Log($"[卡族] 轴「{axis.Name}」定位到 {position} {axis.Unit}（卡{slot.CardNo} 轴{axis.AxisNo}）");
        }

        public void StopAxis(AxisItem axis)
        {
            var (slot, a) = AxisOf(axis);
            if (a == null) { WarnNoAxis(axis, "停止"); return; }
            int res = 0;
            Guard(() => res = a.StopCardAxisMovement(slot.CardNo, Math.Max(axis.AxisNo, 0), 0));   // 0 = 减速停止
            Report("轴停止", res, $"[卡族] 轴「{axis.Name}」已减速停止");
        }

        public void WaitAxisDone(AxisItem axis)
        {
            var (slot, a) = AxisOf(axis);
            if (a == null) { WarnNoAxis(axis, "等待到位"); return; }

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < Options.AxisWaitTimeoutMs)
            {
                int state = 1;
                Guard(() => state = a.GetCardAxisCurrentState());   // 0 = 停止 / 1 = 运行中
                if (state == 0)
                {
                    double pos = 0;
                    Guard(() => pos = a.GetCardAxisCurrentPosition(0));   // 0 = 指令位置
                    Log($"[卡族] 轴「{axis.Name}」已到位，当前位置 {pos:F3} {axis.Unit}");
                    return;
                }
                Thread.Sleep(Options.PollIntervalMs);
            }
            throw new ScriptRuntimeException(
                $"等待轴「{axis.Name}」到位超时（{Options.AxisWaitTimeoutMs}ms）。"
                + DiagnoseAxis(axis, slot, a));
        }

        public void HomeAxis(AxisItem axis)
        {
            var (slot, a) = AxisOf(axis);
            if (a == null) { WarnNoAxis(axis, "回零"); return; }

            var hpm = BuildHomeParam(axis, slot.CardNo);
            int res = -1;

            if (IsSimulation(slot))
            {
                // 模拟卡的回零是它自己的后台循环：从当前位置按「速度缓冲 ÷1000 每毫秒」递减到原点
                // （spacing[原点] = 0）。速度缓冲没被写过就会「一步都不减」，于是 isRun 永久为真、
                // 状态一直显示「运动中」—— 所以回零前也要把曲线写进仿真器（用回零速度）。
                EnsureSimReady(slot, a, axis, BuildSimParam(axis, slot.CardNo, 0, 1, axis.HomeSpeed));
            }

            // 总线卡与脉冲卡的回零参数下发函数不同：
            //   总线卡（EtherCAT）：SetCardAxisHomeProfile → nmc_set_home_profile
            //   脉冲卡：            SetCardAxisHomeMode    → dmc_set_home_profile_unit
            // 两者都接受 HomeParameModel，这里按卡族的总线能力优先走总线分支，返回非 0 再退回另一支。
            bool bus = slot.Family.BusTypes.Contains(CardBusType.EtherCAT);
            Guard(() =>
            {
                res = bus ? a.SetCardAxisHomeProfile(hpm) : a.SetCardAxisHomeMode(hpm);
                if (res != 0) res = bus ? a.SetCardAxisHomeMode(hpm) : a.SetCardAxisHomeProfile(hpm);
            });
            Log($"[卡族] 轴「{axis.Name}」回零参数已下发（模式={axis.HomeMode}→{hpm.HomeMode} "
                + $"高速={hpm.HighVel} 低速={hpm.LowVel} 偏移={hpm.EtherCATHomeOffset}，返回值={res}）");

            Guard(() => a.CardAxisHomeMove());

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < Options.AxisWaitTimeoutMs)
            {
                ushort state = 0;
                int r = -1;
                Guard(() => r = a.CardAxisGetHomeResult(slot.CardNo, Math.Max(axis.AxisNo, 0), ref state));
                if (r == 0 && state == 1)
                {
                    Guard(() => a.SetCardAxisCurrentPosition(slot.CardNo, Math.Max(axis.AxisNo, 0), (int)axis.HomeOffset));
                    Log($"[卡族] 轴「{axis.Name}」回零完成，坐标已置为 {axis.HomeOffset}");
                    return;
                }
                Thread.Sleep(Options.PollIntervalMs);
            }
            throw new ScriptRuntimeException(
                $"轴「{axis.Name}」回零超时（{Options.AxisWaitTimeoutMs}ms）。请检查原点 / 限位感应是否接好、回零模式与速度是否合理。"
                + DiagnoseAxis(axis, slot, a));
        }

        /// <summary>读取指令位置（单位同 axis.Unit）。</summary>
        public double GetAxisPosition(AxisItem axis)
        {
            var (slot, a) = AxisOf(axis);
            if (a == null) { WarnNoAxis(axis, "读位置"); return 0; }
            double v = 0;
            Guard(() => v = a.GetCardAxisCurrentPosition(0));   // 0 = 输出脉冲计数器（指令位置）
            return v;
        }

        /// <summary>
        /// 读取编码器反馈位置（单位同 axis.Unit）。
        /// 与 <see cref="GetAxisPosition"/> 一起用可判断「轴是不是真的在走」：
        /// 指令位置在变、编码器不变 → 没使能 / 动力线没接 / 编码器线松。
        /// </summary>
        public double GetAxisEncoder(AxisItem axis)
        {
            var (slot, a) = AxisOf(axis);
            if (a == null) { WarnNoAxis(axis, "读编码器"); return 0; }
            double v = 0;
            Guard(() => v = a.GetCardAxisCurrentPosition(1));   // 1 = 编码器反馈计数器
            return v;
        }

        /// <summary>是否至少有一个控制器已经初始化成功（供状态栏 / Lua 的 HardwareReady 用）。</summary>
        public bool AnyReady
        {
            get { lock (_gate) return _slots.Values.Any(s => s.Ready); }
        }

        // ===================== 轴实时状态（供「轴状态与控制」表用；均为硬件调用，请在后台线程调）=====================

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, System.Reflection.MethodInfo> _ioStateMethods
            = new System.Collections.Concurrent.ConcurrentDictionary<Type, System.Reflection.MethodInfo>();

        /// <summary>
        /// 读取一个轴的实时状态。任一项读不到就保持默认值（界面对应列显示「—」），不编造状态。
        /// ★ 这是硬件调用，务必在后台线程执行。
        /// </summary>
        public bool TryReadAxisRaw(AxisItem axis, out AxisRawRead raw)
        {
            raw = default(AxisRawRead);
            raw.StateMachine = -1;
            if (axis == null) { raw.Message = "无轴"; return false; }

            CardSlot slot = null;
            IAxis a = null;
            try { (slot, a) = AxisOf(axis); }
            catch (Exception ex) { raw.Message = "寻址失败：" + ex.Message; return false; }
            if (a == null) { raw.Message = "控制器未连接或轴号越界"; return false; }
            if (slot != null && !slot.Ready) { raw.Message = $"「{slot.ControllerName}」尚未连接"; return false; }

            int axisNo = Math.Max(axis.AxisNo, 0);
            int cardNo = slot != null ? slot.CardNo : 0;
            bool sim = IsSimulation(slot);
            double equiv = EquivOf(axis);

            // ★ 不能把 out 参数写进 lambda（CS1628），先读到本地变量，最后再组装。
            double pos = 0, enc = 0;
            bool moving = false, hasWord = false;
            int alarm = 0;
            uint word = 0;
            bool? servoOn = null;

            // 单项读失败不影响其它项（卡族里不少接口在参考实现里是未完成的）。
            void Try(Action act) { try { act(); } catch { /* 该项读不到就留默认值 */ } }

            Try(() => pos = a.GetCardAxisCurrentPosition(0));   // 0 = 指令位置
            Try(() => enc = a.GetCardAxisCurrentPosition(1));   // 1 = 编码器反馈
            Try(() =>
            {
                int st = 1;
                st = a.GetCardAxisCurrentState();               // 0 = 停止 / 1 = 运行中
                moving = st != 0;
            });

            if (sim)
            {
                // 模拟卡：位置计数器是**脉冲**，界面按轴配置的「单位」显示 → 除回脉冲当量。
                pos /= equiv;
                enc /= equiv;

                // 模拟卡的 GetCardAxisAlarmState 返回的**就是轴状态字**（内部 GetCardAxisIOStatus），
                // 而它没有无参 GetAxisCurrentState()，反射取不到 → 直接用这个字当状态字。
                Try(() => { alarm = a.GetCardAxisAlarmState(cardNo, axisNo); word = unchecked((uint)alarm); hasWord = true; });

                // 使能：模拟卡用「伺服使能端口」（有状态、可读），它的状态字里没有使能位。SDK 约定读回 0=使能、1=失能（active-low SON），故用 == 0 判定（与 EnableAxis 写 0 对应）。
                Try(() => servoOn = a.GetCardAxisSevonPin() == 0);
            }
            else
            {
                Try(() => alarm = a.GetCardAxisAlarmState(cardNo, axisNo));
                try { hasWord = TryReadIoWord(a, out uint w); word = w; } catch { }
            }

            raw.Position = pos;
            raw.Encoder = enc;
            raw.Moving = moving;
            // 有「轴状态字」时，bit0 才是报警的权威判据；这个字段只留给「语义不定的返回码」。
            raw.AlarmCode = hasWord ? 0 : alarm;
            raw.IoWord = word;
            raw.HasIoWord = hasWord;
            raw.ServoOn = servoOn;
            raw.Ok = true;
            // 模拟卡要标明，免得把「软件仿真出来的状态」当成真机状态。
            raw.Message = sim ? "已连接（模拟卡·仿真）" : "已连接";
            return true;
        }

        /// <summary>
        /// 取「轴状态字」（dmc_axis_io_status 位布局）。
        /// 卡族的 AxisRealization 里有一个无参的 <c>GetAxisCurrentState()</c> 返回该字，但它不在
        /// <see cref="IAxis"/> 接口上，所以这里用反射取（结果按类型缓存）；取不到返回 false —— 界面显示「—」，不猜。
        /// </summary>
        private static bool TryReadIoWord(IAxis a, out uint word)
        {
            word = 0;
            if (a == null) return false;

            var mi = _ioStateMethods.GetOrAdd(a.GetType(), t =>
            {
                var m = t.GetMethod("GetAxisCurrentState",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                    null, Type.EmptyTypes, null);
                return (m != null && m.ReturnType == typeof(int)) ? m : null;
            });
            if (mi == null) return false;

            try
            {
                object r = mi.Invoke(a, null);
                if (r is int v) { word = unchecked((uint)v); return true; }
            }
            catch { /* 反射调用失败当作读不到 */ }
            return false;
        }

        // ===================== 模拟卡（虚拟运动卡 / 数字孪生卡）适配 =====================
        //
        // 模拟卡的实现是进程内仿真（VirtualMotionCardSDK / DigitalTwinCardSDK），与真实卡有三处语义差异。
        // 不处理就会出现「指令返回 0 但轴不动」「状态全是 —」这类看起来像 bug、其实是仿真器模型约定的现象：
        //
        //   ① 行程范围（spacing）默认全 0：仿真器的点位运动 / 连续运动都会做限位判断，
        //      把**任何**非 0 目标夹回 0 并置「正 / 负限位」位。现场表现：点动返回成功、位置却不变，
        //      而且限位位亮、状态字变成一个看不懂的大数字。
        //      → 每根轴首次运动前 SetSpacing 给一个宽行程。
        //   ② 速度曲线只认 SetCardAxisProfile（= 接口 SetCardAxisTProfile）；
        //      模拟卡里 SetCardAxisMotionalVel / SetCardVectorProfileMulticoor 是**空实现**（返回 0 但什么都不做）。
        //   ③ 连续运动按「每 1ms 前进 (int)(pps / 1000) 个脉冲」整数步进：pps < 1000 会被截断成 0，
        //      于是仿真器的 isRun 一直为真 —— 状态显示「运动中」而位置永远不变（用户实测到的现象）。
        //      → 速度换算成 pps（× 脉冲当量）并给 1000pps 下限。
        //
        // 另外：模拟卡没有无参 GetAxisCurrentState()（状态字），但它的 GetCardAxisAlarmState 返回的**就是**
        // 轴状态字（内部 GetCardAxisIOStatus）；使能也没实现 OpenCardAxisEnable（恒 -1），要走伺服使能端口。

        /// <summary>模拟卡虚拟行程范围（脉冲）。取值在 DMC 位置计数器量程（±1.34e8）以内。</summary>
        private const int SimTravelRange = 100_000_000;

        /// <summary>已做过「行程范围」初始化的轴（键 = 控制器名#轴号）。每轴只做一次，之后每次只下发速度。</summary>
        private readonly HashSet<string> _simReady = new HashSet<string>();

        private static bool IsSimulation(CardSlot slot) => slot != null && slot.Family != null && slot.Family.IsSimulation;

        /// <summary>脉冲当量（每单位脉冲数）；未填（&lt;=0）时按 1:1 处理，避免乘 0 / 除 0。</summary>
        private static double EquivOf(AxisItem axis) => axis != null && axis.PulsePerUnit > 0 ? axis.PulsePerUnit : 1;

        /// <summary>
        /// 拼一个「模拟卡」用的运动参数：距离与速度都换算成脉冲 / pps。
        /// 真实卡是卡内按脉冲当量换算的，模拟卡没有这一层，得由我们换算；读回位置时再除回来（见 TryReadAxisRaw）。
        /// </summary>
        private static MotionParamModel BuildSimParam(AxisItem axis, int cardNo, double dist, int posiMode, double speed)
        {
            double equiv = EquivOf(axis);
            double v = (speed > 0 ? speed : (axis.Speed > 0 ? axis.Speed : 1)) * equiv;
            if (v < 1000) v = 1000;          // 仿真器步进下限：< 1000pps 会被整数截断成「一点都不走」
            double d = dist * equiv;

            var mpm = BuildMotionParam(axis, cardNo, d, posiMode, v);
            mpm.Equiv = (int)Math.Round(equiv);
            mpm.Pos = (int)Math.Round(d);
            mpm.Dist = d;
            mpm.MinVel = v;                  // 恒速：MinVel = MaxVel ⇒ 仿真器走「无加减速段」分支，行为最可预期
            mpm.MaxVel = v;
            mpm.TaccVel = 0.001;
            mpm.TdecVel = 0.001;
            mpm.StopVel = 0;
            return mpm;
        }

        /// <summary>
        /// 模拟卡每次运动前的准备：首次给该轴设宽行程，每次都把速度曲线写进仿真器的参数缓冲
        /// （模拟卡里 <c>SetCardAxisTProfile</c> 才是真正生效的设速接口）。
        /// </summary>
        private void EnsureSimReady(CardSlot slot, IAxis a, AxisItem axis, MotionParamModel mpm)
        {
            int axisNo = Math.Max(axis.AxisNo, 0);
            string key = (slot != null ? slot.ControllerName : "?") + "#" + axisNo;

            bool first;
            lock (_simReady) first = _simReady.Add(key);

            if (first)
            {
                int res = a.SetSpacing(axisNo, 0, SimTravelRange, -SimTravelRange);
                Log($"[卡族·模拟卡] 轴「{axis.Name}」行程范围已设为 ±{SimTravelRange} 脉冲（SetSpacing 返回 {res}）——否则仿真器会把任何移动夹到 0。");
            }

            a.SetCardAxisTProfile(mpm);
        }

        /// <summary>
        /// 下发一次点位（相对 / 绝对）运动。模拟卡与真实卡的距离 / 速度换算不同，这里统一收口。
        /// posiMode：0 = 相对坐标，1 = 绝对坐标。返回底层返回码（0 = 成功，模拟卡恒为 0）。
        /// </summary>
        private int SendPointMove(CardSlot slot, IAxis a, AxisItem axis, double target, int posiMode, double speed)
        {
            if (IsSimulation(slot))
            {
                // 1 = 绝对：模拟卡只认绝对目标（见 SimAbsoluteTarget 的说明）。
                var mpm = BuildSimParam(axis, slot.CardNo, SimAbsoluteTarget(a, axis, target, posiMode), 1, speed);
                EnsureSimReady(slot, a, axis, mpm);
                return a.CardAxisPointMovement(mpm);
            }

            a.SetCardAxisMotionalVel(BuildMotionParam(axis, slot.CardNo, 0, 1, speed));
            return a.CardAxisPointMovement(BuildMotionParam(axis, slot.CardNo, target, posiMode, speed));
        }

        /// <summary>
        /// 把运动目标换算成模拟卡能用的「绝对目标」。
        /// ★ 模拟卡**没有相对坐标的概念**：它的 <c>CardAxisPMove</c> 完全忽略 posi_mode，
        ///   直接把入参**赋值**给位置计数器（等价于永远按绝对坐标处理）。
        ///   所以相对移动（posiMode = 0）必须自己读回当前位置再算绝对目标 ——
        ///   否则「点动 +1」只是把轴送到 1，再点几次还是停在 1（用户实测：点动看起来像绝对定位）。
        /// 真实卡族不受影响：<c>CardAxisPointMovement</c> 会把 posi_mode 原样传给 SDK
        /// （如 MCN420 的 <c>DmcCardAxisPMoveUnit(card, axis, Dist, posi_mode)</c>），相对/绝对都正确。
        /// </summary>  
        private static double SimAbsoluteTarget(IAxis a, AxisItem axis, double target, int posiMode)
        {
            if (posiMode != 0) return target;                       // 本来就是绝对目标

            double curCounts = 0;
            try { curCounts = a.GetCardAxisCurrentPosition(0); }    // 0 = 指令位置（脉冲）
            catch { /* 读不到就按 0 起算（退化成绝对，至少不报错） */ }

            return curCounts / EquivOf(axis) + target;              // 换回「单位」空间，再由 BuildSimParam 统一 ×当量
        }

        /// <summary>
        /// 点动一段距离（可指定速度，用于「手动速度」）。返回 null 表示成功，否则是失败原因。
        /// 与桥接口的 MoveAxisRel 等价，但那条路会把速度写死成 axis.Speed，手动速度不生效。
        /// </summary>
        public string InchAxis(AxisItem axis, double distance, double speed)
        {
            if (axis == null) return "无轴";
            CardSlot slot = null; IAxis a = null;
            try { (slot, a) = AxisOf(axis); }
            catch (Exception ex) { return "寻址失败：" + ex.Message; }
            if (a == null) { WarnNoAxis(axis, "点动"); return "控制器未连接或轴号越界"; }

            int cardNo = slot != null ? slot.CardNo : 0;
            double v = speed > 0 ? speed : (axis.Speed > 0 ? axis.Speed : 1);
            bool sim = IsSimulation(slot);

            try
            {
                if (sim)
                {
                    // 模拟卡：走 SendPointMove —— 它会把「相对距离」按当前位置换算成绝对目标
                    // （模拟卡忽略 posi_mode）；宽行程 + 速度曲线由 EnsureSimReady 负责。
                    int rs = SendPointMove(slot, a, axis, distance, 0, v);
                    if (rs != 0) return $"点动下发失败（卡返回 {rs}）";
                }
                else
                {
                    int r1 = a.SetCardAxisMotionalVel(BuildMotionParam(axis, cardNo, 0, 1, v));
                    if (r1 != 0) return $"点动失败：下发速度曲线返回 {r1}（检查脉冲当量 / 加减速是否合理）";

                    int res = a.CardAxisPointMovement(BuildMotionParam(axis, cardNo, distance, 0, v));   // 0 = 相对
                    if (res != 0) return $"点动下发失败（卡返回 {res}）";
                }

                Log($"[卡族{(sim ? "·模拟卡" : "")}] 轴「{axis.Name}」点动 {distance} {axis.Unit}（速度 {v} {axis.Unit}/s）");
                return null;
            }
            catch (Exception ex) { return "点动异常：" + ex.Message; }
        }

        /// <summary>
        /// 启动连续点动（Jog）。返回 null 表示成功，否则是失败原因。
        /// ★ **必须先下发速度曲线再发连续运动指令**：正常定位（<see cref="MoveAxisRel"/>）就是这么做的；
        /// 少了这一步，卡会「收下指令但按无效 profile 跑」—— 现场表现就是「点了 Jog 轴不动」。
        /// </summary>
        public string StartAxisJog(AxisItem axis, bool positive, double speed)
        {
            if (axis == null) return "无轴";
            CardSlot slot = null; IAxis a = null;
            try { (slot, a) = AxisOf(axis); }
            catch (Exception ex) { return "寻址失败：" + ex.Message; }
            if (a == null) { WarnNoAxis(axis, "Jog"); return "控制器未连接或轴号越界"; }

            int cardNo = slot != null ? slot.CardNo : 0;
            double v = speed > 0 ? speed : (axis.Speed > 0 ? axis.Speed : 1);
            bool sim = IsSimulation(slot);

            try
            {
                MotionParamModel mpm;
                if (sim)
                {
                    // 模拟卡的连续运动读的是它自己的「速度参数缓冲」，只有 SetCardAxisTProfile 会写进去；
                    // 少了这一步（或 pps < 1000）就会出现「状态显示运动中、位置一动不动」。
                    mpm = BuildSimParam(axis, cardNo, 0, 0, v);
                    EnsureSimReady(slot, a, axis, mpm);
                }
                else
                {
                    int r1 = a.SetCardAxisMotionalVel(BuildMotionParam(axis, cardNo, 0, 1, v));
                    if (r1 != 0) return $"Jog 失败：下发速度曲线返回 {r1}（检查脉冲当量 / 加减速是否合理）";
                    mpm = BuildMotionParam(axis, cardNo, 0, 0, v);
                }

                mpm.Dir = positive ? 1 : 0;                 // 0 = 负方向，1 = 正方向
                int res = a.CardAxisSerialMovement(mpm);
                if (res != 0) return $"Jog 下发失败（卡返回 {res}）";

                Log($"[卡族{(sim ? "·模拟卡" : "")}] 轴「{axis.Name}」Jog {(positive ? "正向" : "反向")} 已启动（速度 {v} {axis.Unit}/s），松开按钮停止");
                return null;
            }
            catch (Exception ex) { return "Jog 异常：" + ex.Message; }
        }

        /// <summary>把当前指令位置置零（设零点）。返回 null 表示成功，否则是失败原因。</summary>
        public string SetAxisZero(AxisItem axis)
        {
            if (axis == null) return "无轴";
            CardSlot slot = null; IAxis a = null;
            try { (slot, a) = AxisOf(axis); }
            catch (Exception ex) { return "寻址失败：" + ex.Message; }
            if (a == null) { WarnNoAxis(axis, "设零点"); return "控制器未连接或轴号越界"; }

            try
            {
                int res = a.SetCardAxisCurrentPosition(slot != null ? slot.CardNo : 0, Math.Max(axis.AxisNo, 0), 0);
                if (res != 0) return $"设零点失败（卡返回 {res}）";
                Log($"[卡族] 轴「{axis.Name}」当前位置已置零");
                return null;
            }
            catch (Exception ex) { return "设零点异常：" + ex.Message; }
        }

        /// <summary>
        /// 判断当前工程是否「需要」走卡族层：只要有一个控制器的品牌 + 卡型号匹配到已移植卡族，
        /// 且该卡族不是雷赛（雷赛默认仍走诊断更全的自有 LtdmcCard 封装），就返回 true。
        /// 工程还没加载 / 没有控制器 / 匹配不到时返回 false —— 调用方据此回退到雷赛封装，不改变既有行为。
        /// </summary>
        public static bool CanServeProject(out string why)
        {
            why = null;
            List<AxisControllerItem> ctls;
            try { ctls = ProjectStore.Data?.Controllers?.Where(c => c != null).ToList(); }
            catch { return false; }
            if (ctls == null || ctls.Count == 0) return false;

            var hits = new List<string>();
            foreach (var c in ctls)
            {
                var fam = CardFamilyCatalog.Resolve(c.Vendor, c.CardType, c.BusType);
                if (fam == null) continue;
                if (fam.Vendor == "雷赛") continue;      // 雷赛留给 LeadshineHardwareBridge
                hits.Add($"{c.Name}→{fam.Key}");
            }
            if (hits.Count == 0) return false;

            why = string.Join("、", hits);
            return true;
        }

        /// <summary>当前控制器是否已就绪（供界面显示对接状态）。</summary>
        public bool IsControllerReady(string controllerName)
        {
            lock (_gate)
                return _slots.TryGetValue(controllerName ?? string.Empty, out var s) && s.Ready;
        }

        /// <summary>所有已初始化控制器的状态说明（供界面显示）。</summary>
        public IReadOnlyList<string> ControllerStatus()
        {
            lock (_gate) return _slots.Values.Select(s => $"{s.ControllerName}：{s.Status}").ToList();
        }

        /// <summary>
        /// 连接后从底层硬件真实读取该控制器的轴数量与主板 IO 数量。
        /// <para>轴数走 <see cref="ICard.GetCardTotalAxisNum"/>；IO 数取自卡族实现维护的
        /// <see cref="IIOInPut.InIONums"/> / <see cref="IIOOutPut.OutIONums"/>（硬件卡实际拥有的 IO 数）。</para>
        /// <para>该控制器未就绪 / 底层未返回有效值时，三个 out 参数均为 0，调用方应回退到配置值。</para>
        /// </summary>
        public bool TryGetRealCounts(string controllerName, out int axisCount, out int inIo, out int outIo)
        {
            axisCount = 0; inIo = 0; outIo = 0;
            CardSlot slot;
            lock (_gate)
            {
                if (!_slots.TryGetValue(controllerName ?? string.Empty, out slot) || slot == null) return false;
            }
            if (!slot.Ready || slot.Runtime == null) return false;

            try
            {
                uint total = 0;
                int r = -1;
                if (slot.Runtime.Card != null) r = slot.Runtime.Card.GetCardTotalAxisNum(slot.CardNo, ref total);
                if (r == 0 && total > 0) axisCount = (int)total;
            }
            catch (Exception ex) { Log($"[卡族] 读取轴总数失败：{ex.Message}"); }

            try { if (slot.Runtime.InIo != null) inIo = slot.Runtime.InIo.InIONums; } catch { /* 部分卡族未维护该字段 */ }
            try { if (slot.Runtime.OutIo != null) outIo = slot.Runtime.OutIo.OutIONums; } catch { /* 部分卡族未维护该字段 */ }

            // 卡族标准规格优先：移植卡族的底层 API 经常不回报容量
            //（轴返回 0、IO 返回占位默认值 24），目录里人工核对过的「设计容量」才是可信来源。
            // 仅当该字段在目录中已人工设定（> 0）时覆盖硬件读数；未设定的卡族继续走硬件真实值。
            if (slot.Family != null)
            {
                if (slot.Family.AxisCount > 0) axisCount = slot.Family.AxisCount;
                if (slot.Family.InIoCount > 0) inIo = slot.Family.InIoCount;
                if (slot.Family.OutIoCount > 0) outIo = slot.Family.OutIoCount;
            }
            return true;
        }

        /// <summary>
        /// 立即「连接」某个控制器（供「控制器」页的连接按钮调用）：匹配卡族 → 创建实现 →
        /// InitCard / OpenCard，并把结果写入该控制器的状态。<para>返回是否就绪；<paramref name="status"/> 为中文状态说明。</para>
        /// <para>与 <see cref="SlotOf"/> 的懒加载共用同一份 slot 状态，连接后轴 / IO 动作会直接复用，不会重复初始化。</para>
        /// </summary>
        public bool TryConnect(AxisControllerItem ctl, out string status)
        {
            status = "未指定控制器";
            if (ctl == null) return false;

            CardSlot slot;
            lock (_gate)
            {
                if (!_slots.TryGetValue(ctl.Name, out slot))
                {
                    slot = new CardSlot { ControllerName = ctl.Name, Config = ctl };
                    _slots[ctl.Name] = slot;
                }
            }

            // ★ 初始化（InitCard / OpenCard）是慢硬件操作：放到 _gate 之外、按槽位串行执行。
            //   否则连接期间 UI 线程读 IsControllerReady / ControllerStatus 会一起等 _gate → 界面整个卡住。
            lock (slot.InitLock)
            {
                // 已经初始化成功过就不重复碰硬件；失败过也允许再试（现场插好卡后点连接）。
                if (slot.Runtime == null || !slot.Ready) Initialize(slot, "连接");
            }

            bool exp = slot.Runtime?.HasExpansionIo == true;
            status = slot.Status + (slot.Family != null ? $"；扩展IO模块：{(exp ? "支持" : "不支持")}" : string.Empty);
            return slot.Ready;
        }

        /// <summary>断开某个控制器（关闭卡并清除其连接状态），下次连接 / 动作会重新初始化。</summary>
        public void Disconnect(AxisControllerItem ctl)
        {
            if (ctl == null) return;
            CardSlot slot = null;
            lock (_gate)
            {
                if (_slots.TryGetValue(ctl.Name, out slot)) _slots.Remove(ctl.Name);
            }
            // ★ 关卡是硬件操作：放到 _gate 之外，避免持锁卡住 UI 线程的状态查询。
            try { slot?.Runtime?.Card?.CloseCard(); } catch { /* 关闭失败不影响断开 */ }
            Log($"[卡族] 控制器「{ctl.Name}」已断开连接。");
        }

        /// <summary>
        /// 该控制器匹配到的卡族是否支持扩展 IO 模块（供界面「获取 / 是否可加模块」判断）。
        /// <para>只创建卡族实现对象判断 <see cref="CardFamilyRuntime.HasExpansionIo"/>，不碰硬件。</para>
        /// </summary>
        public static bool SupportsExpansionIo(AxisControllerItem ctl)
        {
            var fam = CardFamilyCatalog.Resolve(ctl?.Vendor, ctl?.CardType, ctl?.BusType);
            if (fam == null) return false;
            try { return fam.Create()?.HasExpansionIo == true; }
            catch { return false; }
        }

        /// <summary>控制器匹配到的卡族（供界面显示；匹配不到返回 null）。</summary>
        public static CardFamilyDescriptor ResolveFamily(AxisControllerItem ctl)
            => CardFamilyCatalog.Resolve(ctl?.Vendor, ctl?.CardType, ctl?.BusType);

        /// <summary>清掉已初始化的卡族实例（改完控制器配置后可重新探测）。</summary>
        public void Reset()
        {
            List<CardSlot> old;
            lock (_gate)
            {
                old = _slots.Values.ToList();
                _slots.Clear();
                _warnedNoController = false;
                _warnedUnmatched = false;
                _warnedExpansion = false;
            }
            // ★ 关卡是硬件操作：放到 _gate 之外，避免持锁卡住 UI 线程的状态查询。
            foreach (var s in old)
            {
                try { s.Runtime?.Card?.CloseCard(); } catch { /* 关闭失败不影响重置 */ }
            }
            Log("[卡族] 已重置全部控制器连接状态，下次动作会重新初始化。");
        }

        // ===================== IO =====================

        public double ReadInput(IoItem io)
        {
            var (slot, expansion) = IoOf(io);
            if (slot == null || !slot.Ready) { WarnNoIo(io, "读输入"); return io.Value; }

            int raw = 0;
            if (expansion)
            {
                ushort node = ExpansionNode(io);
                ushort bit = (ushort)Math.Max(io.Sequence, 0);
                ushort val = 0;
                Guard(() => slot.Runtime.ExtIn.EGetExpandIOInBit(slot.CardNo, node, bit, ref val));
                raw = val;
            }
            else
            {
                HintExpansionOnce(io, slot);
                ushort bit = (ushort)MainBoardBit(io);
                Guard(() => raw = slot.Runtime.InIo.GetCardInPortNoValue(slot.CardNo, bit));
            }

            int value = ApplyLevel(raw, io.Level);
            io.Value = value;
            return value;
        }

        public void WaitInput(IoItem io, int value)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < Options.IoWaitTimeoutMs)
            {
                if ((int)ReadInput(io) == value)
                {
                    Log($"[卡族] 输入「{io.Name}」已变为 {value}（耗时 {sw.ElapsedMilliseconds}ms）");
                    return;
                }
                Thread.Sleep(Options.PollIntervalMs);
            }
            throw new ScriptRuntimeException(
                $"等待输入「{io.Name}」= {value} 超时（{Options.IoWaitTimeoutMs}ms）。"
                + $"请检查传感器接线、电平设置（当前 {io.Level}）与卡号 / 寻址是否正确"
                + $"（当前按 {DescribeIoAddress(io)} 读，读到的原始值 {io.Value}）。");
        }

        public void WriteOutput(IoItem io, int value)
        {
            var (slot, expansion) = IoOf(io);
            if (slot == null || !slot.Ready) { WarnNoIo(io, "写输出"); return; }

            int raw = ApplyLevel(value, io.Level);
            if (expansion)
            {
                ushort node = ExpansionNode(io);
                ushort bit = (ushort)Math.Max(io.Sequence, 0);
                int res = 0;
                Guard(() => res = slot.Runtime.ExtOut.ESetExpandIOOutBit(slot.CardNo, node, bit, raw));
                Report("写扩展输出", res, $"[卡族] 输出「{io.Name}」= {value}（卡{slot.CardNo} 从站{node} 位{bit}，扩展 IO）");
            }
            else
            {
                HintExpansionOnce(io, slot);
                ushort bit = (ushort)MainBoardBit(io);
                int res = 0;
                Guard(() => res = slot.Runtime.OutIo.SetCardBitNoInBit(slot.CardNo, bit, raw));
                Report("写输出", res, $"[卡族] 输出「{io.Name}」= {value}（卡{slot.CardNo} 位{bit}）");
            }
            io.Value = value;
        }

        public void ToggleOutput(IoItem io)
        {
            var (slot, expansion) = IoOf(io);
            if (slot == null || !slot.Ready) { WarnNoIo(io, "取反输出"); return; }

            int cur = 0;
            if (expansion)
            {
                ushort node = ExpansionNode(io);
                ushort bit = (ushort)Math.Max(io.Sequence, 0);
                ushort val = 0;
                Guard(() => slot.Runtime.ExtOut.EGetExpandIOOutBit(slot.CardNo, node, bit, ref val));
                cur = val;
            }
            else
            {
                ushort bit = (ushort)MainBoardBit(io);
                Guard(() => cur = slot.Runtime.OutIo.GetCardPortNoOutState(slot.CardNo, bit));
            }

            int next = cur != 0 ? 0 : 1;
            int logical = ApplyLevel(next, io.Level);
            WriteOutput(io, logical);
            Log($"[卡族] 输出「{io.Name}」已取反 → {logical}（{DescribeIoAddress(io)}）");
        }

        // ===================== 气缸（通过 IO 点驱动） =====================

        public void CylinderMove(CylinderItem cyl, int state)
        {
            var outIo = FindIo(cyl.OutPoint, isOutput: true);
            if (outIo == null)
            {
                Log($"[卡族] 气缸「{cyl.Name}」没有配置有效的输出点（当前：{cyl.OutPoint}），动作已跳过。");
                return;
            }

            if (cyl.DelayMs > 0) Thread.Sleep(cyl.DelayMs);

            if (cyl.PulseOutput && cyl.PulseWidthMs > 0)
            {
                WriteOutput(outIo, state);
                Thread.Sleep(cyl.PulseWidthMs);
                WriteOutput(outIo, state != 0 ? 0 : 1);
                Log($"[卡族] 气缸「{cyl.Name}」脉冲输出 {cyl.PulseWidthMs}ms（{(state == 1 ? "伸出" : "缩回")}）");
                return;
            }

            WriteOutput(outIo, state);   // 伸出输出（第 1 路）：1=伸出 / 0=缩回

            // 缩回输出（第 2 路，配置了才写）：与第 1 路互斥（1=缩回）。
            var retIo = FindIo(cyl.BackupSensor, isOutput: true);
            if (retIo != null) WriteOutput(retIo, state != 0 ? 0 : 1);

            Log($"[卡族] 气缸「{cyl.Name}」{(state == 1 ? "伸出" : "缩回")}（输出点 {cyl.OutPoint}）");
        }

        public void WaitCylinder(CylinderItem cyl)
        {
            int timeout = cyl.TimeoutMs > 0 ? cyl.TimeoutMs : Options.IoWaitTimeoutMs;
            var outIo = FindIo(cyl.OutPoint, isOutput: true);
            int expectExtend = outIo != null ? (outIo.Value != 0 ? 1 : 0) : 1;

            string sensorName = expectExtend == 1 ? cyl.SensorExtend : cyl.SensorRetract;
            var sensor = FindIo(sensorName, isOutput: false);
            if (sensor == null)
            {
                int wait = expectExtend == 1
                    ? (cyl.ExtendMs > 0 ? cyl.ExtendMs : 300)
                    : (cyl.RetractMs > 0 ? cyl.RetractMs : 300);
                Thread.Sleep(wait);
                Log($"[卡族] 气缸「{cyl.Name}」无到位感应（{sensorName}），按动作时间等待 {wait}ms");
                return;
            }

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeout)
            {
                if ((int)ReadInput(sensor) == 1)
                {
                    Log($"[卡族] 气缸「{cyl.Name}」{(expectExtend == 1 ? "伸出" : "缩回")}到位（感应 {sensorName}，耗时 {sw.ElapsedMilliseconds}ms）");
                    return;
                }
                Thread.Sleep(Options.PollIntervalMs);
            }
            // 超时：按气缸「超时报警方式」处理（报警并停止 = 默认，抛异常终止流程 / 运行）
            string timeoutMsg = $"气缸「{cyl.Name}」等待到位超时（{timeout}ms）。请检查气压、电磁阀输出「{cyl.OutPoint}」、到位感应「{sensorName}」接线与电平。";
            switch (cyl.TimeoutAction)
            {
                case "忽略":
                    Log("(超时已按「忽略」继续) " + timeoutMsg);
                    return;
                case "仅报警":
                    AlarmService.Raise(LogLevel.Error, "气缸超时", string.Empty, timeoutMsg);
                    return;
                default:   // 报警并停止
                    AlarmService.Raise(LogLevel.Error, "气缸超时", string.Empty, timeoutMsg);
                    throw new ScriptRuntimeException(timeoutMsg);
            }
        }

        public void CylinderReset(CylinderItem cyl)
        {
            int state = (cyl.InitialState ?? string.Empty).Contains("伸") ? 1 : 0;
            CylinderMove(cyl, state);
            Log($"[卡族] 气缸「{cyl.Name}」复位到初始状态：{cyl.InitialState}");
        }

        // ===================== 通讯（真实串口 / 网口 / Modbus） =====================

        public void CommSend(CommItem comm, string data)
        {
            try
            {
                _comm.Send(comm, data);
                Log($"[通讯] 「{comm.Name}」发送：{data}");
            }
            catch (Exception ex) when (!(ex is ScriptRuntimeException))
            {
                throw new ScriptRuntimeException($"通讯「{comm.Name}」发送失败：{ex.Message}");
            }
        }

        public string CommRecv(CommItem comm)
        {
            try
            {
                string s = _comm.Recv(comm);
                Log($"[通讯] 「{comm.Name}」接收：{(string.IsNullOrEmpty(s) ? "(无数据)" : s)}");
                return s;
            }
            catch (Exception ex) when (!(ex is ScriptRuntimeException))
            {
                throw new ScriptRuntimeException($"通讯「{comm.Name}」接收失败：{ex.Message}");
            }
        }

        // ===================== 料盘 =====================

        public void TrayPick(TrayItem tray) => TrayStep(tray, "取料");

        public void TrayPlace(TrayItem tray) => TrayStep(tray, "放料");

        /// <summary>
        /// 按行列布局算出当前格子的坐标并推进格号。料盘没有绑定 X / Y 轴字段，
        /// 因此这里只负责算坐标 + 记日志，脚本里再用 MoveAxisAbs 把 XY 轴移过去。
        /// </summary>
        private void TrayStep(TrayItem tray, string action)
        {
            int total = Math.Max(tray.Rows, 1) * Math.Max(tray.Cols, 1);
            int index = _trayIndex.AddOrUpdate(tray.Name, 0, (_, old) => (old + 1) % total);
            int row = index / Math.Max(tray.Cols, 1);
            int col = index % Math.Max(tray.Cols, 1);
            double x = tray.StartX + col * tray.PitchX;
            double y = tray.StartY + row * tray.PitchY;

            Log($"[卡族] 料盘「{tray.Name}」{action}：第 {index + 1}/{total} 格（行{row + 1} 列{col + 1}）坐标 X={x:F3} Y={y:F3}");
        }

        // ===================== 参数换算 =====================

        /// <summary>按轴配置拼一个卡族用的运动参数模型。</summary>
        private static MotionParamModel BuildMotionParam(AxisItem axis, int cardNo, double dist, int posiMode, double speed)
        {
            // AxisItem 的加减速填的是「加速度值」，卡族这边要的是「加速时间(s)」。
            // 换算：t = v / a（a 为 0 时退化为 0.1s，避免除零让卡收到非法参数）。
            double v = speed > 0 ? speed : (axis.Speed > 0 ? axis.Speed : 1);
            double tacc = axis.Accel > 0 ? v / axis.Accel : 0.1;
            double tdec = axis.Decel > 0 ? v / axis.Decel : 0.1;

            return new MotionParamModel
            {
                CardNo = cardNo,
                Axis = Math.Max(axis.AxisNo, 0),
                RunMode = 0,
                Equiv = (int)Math.Round(axis.PulsePerUnit),
                Pos = (int)Math.Round(dist),
                MinVel = axis.CreepSpeed > 0 ? axis.CreepSpeed : 0,
                MaxVel = v,
                TaccVel = tacc,
                TdecVel = tdec,
                StopVel = 0,
                SMode = 0,
                SPara = 0,
                Dist = dist,
                PosiMode = posiMode,          // 0 = 相对坐标，1 = 绝对坐标
                Dir = 1,
            };
        }

        /// <summary>按轴配置拼一个卡族用的回零参数模型。</summary>
        private static HomeParameModel BuildHomeParam(AxisItem axis, int cardNo)
        {
            double high = axis.HomeSpeed > 0 ? axis.HomeSpeed : 1;
            double low = axis.CreepSpeed > 0 ? axis.CreepSpeed : high / 5;

            return new HomeParameModel
            {
                CardNo = cardNo,
                Axis = Math.Max(axis.AxisNo, 0),
                HomeMode = ParseHomeMode(axis.HomeMode),
                HomeSpeed = 1,                     // 1 = 高速回零
                Vel = high,
                StartVel = low,
                LowVel = low,
                HighVel = high,
                TaccTime = axis.Accel > 0 ? high / axis.Accel : 0.1,
                TdecTime = axis.Decel > 0 ? high / axis.Decel : 0.1,
                OffSetPos = axis.HomeOffset,
                EtherCATHomeOffset = axis.HomeOffset,
                HomeDir = (axis.DirLevel ?? string.Empty).Contains("负") ? 0 : 1,
                OrgLevel = (axis.EnableLevel ?? string.Empty).Contains("低") ? 0 : 1,
                EZlevel = 0,
                ELEnable = true,
            };
        }

        /// <summary>把「回零模式」中文描述转成卡族的模式号（与雷赛手册的 0/1/2 对齐）。</summary>
        private static int ParseHomeMode(string mode)
        {
            string s = (mode ?? string.Empty).Trim();
            if (int.TryParse(s, out int n)) return n;
            if (s.Contains("EZ") || s.Contains("Z 相") || s.Contains("Z相") || s.Contains("索引")) return 2;
            if (s.Contains("当前位置")) return 35;
            if (s.Contains("原点")) return 1;
            return 0;   // 仅限位回零
        }

        // ===================== 寻址 =====================

        /// <summary>
        /// 主板输入 / 输出的位号。
        /// 「模块」= 0 时就是「序号」；模块 &gt; 0 但该卡族没有扩展 IO 实现时，
        /// 按整卡位号 模块 × <see cref="Options.BitsPerModule"/> + 序号 访问（与雷赛接线习惯一致）。
        /// </summary>
        private static int MainBoardBit(IoItem io) =>
            Math.Max(io.ModuleNo, 0) * Options.BitsPerModule + Math.Max(io.Sequence, 0);

        /// <summary>扩展 IO 的节点号：按「模块」列，必要时加基准（见 Options.ExpansionNodeBase）。</summary>
        private static ushort ExpansionNode(IoItem io)
        {
            int node = Math.Max(io.ModuleNo, 0);
            if (Options.ExpansionNodeBase > 0 && node < Options.ExpansionNodeBase) node += Options.ExpansionNodeBase;
            return (ushort)node;
        }

        /// <summary>给现场看的寻址说明，直接拼进超时异常里。</summary>
        private string DescribeIoAddress(IoItem io)
        {
            var (slot, expansion) = IoOf(io);
            if (slot == null) return "未找到可用控制器";
            if (expansion)
                return $"扩展 IO（卡{slot.CardNo} 从站{ExpansionNode(io)} 位{io.Sequence}）";
            return $"主板 IO（卡{slot.CardNo} 位{MainBoardBit(io)}）";
        }

        /// <summary>IO 行填了「模块」但该卡族不支持扩展 IO 时提示一次。</summary>
        private void HintExpansionOnce(IoItem io, CardSlot slot)
        {
            if (io.ModuleNo <= 0 || _warnedExpansion) return;
            _warnedExpansion = true;
            Log($"[卡族] IO「{io.Name}」填了「模块」={io.ModuleNo}，但卡族 {slot.Family.Key} 不支持扩展 IO 模块"
                + "（该卡族没有 Expand* 实现），已退回按主板整卡位号 "
                + $"= 模块 × {Options.BitsPerModule} + 序号 访问。若确实接了扩展模块，请换支持扩展 IO 的卡族。");
        }

        // ===================== 诊断 =====================

        /// <summary>
        /// 打印轴使能后的诊断读数。
        /// <para>注意各卡族 GetCardAxisAlarmState 的语义并不统一：有的返回伺服报警码，
        /// 有的返回整字轴 IO 状态位。所以文案里把「读到的值」和「怎么解读」分开说，不硬说成报警码。</para>
        /// </summary>
        private void ReportBusAxisState(string axisName, CardSlot slot, IAxis a)
        {
            try
            {
                int err = a.GetCardAxisAlarmState(slot.CardNo, Math.Max(a.AxisID, 0));
                Log(err != 0
                    ? $"[卡族] 轴「{axisName}」使能后轴状态读数 = {err}（0x{err:X}），★非 0 通常表示有报警 / 限位 / 急停信号，"
                      + "轴不会运动；具体位含义请对照该卡手册的轴 IO 状态位表。"
                    : $"[卡族] 轴「{axisName}」使能后轴状态读数 = 0（无报警）。若轴仍不动，"
                      + "请确认 CiA402 状态机已到「操作使能(4)」。");
            }
            catch (Exception ex)
            {
                Log($"[卡族] 轴「{axisName}」读取伺服报警码失败：{ex.Message}");
            }
        }

        /// <summary>轴超时 / 不动时的现场诊断串，直接拼进异常消息，让操作员一眼看到该查什么。</summary>
        private string DiagnoseAxis(AxisItem axis, CardSlot slot, IAxis a)
        {
            var sb = new StringBuilder("请检查伺服是否使能、是否报警、目标位置是否超出行程。");
            int no = Math.Max(axis.AxisNo, 0);
            try
            {
                int state = 1;
                Guard(() => state = a.GetCardAxisCurrentState());
                sb.Append($" 当前轴状态={(state == 0 ? "停止" : "运行中")}。");

                try
                {
                    int reason = 0;
                    int r = a.GetCardAxisStopReason(slot.CardNo, no, ref reason);
                    if (r == 0 && reason != 0)
                        sb.Append($" 停止原因码={reason}（撞限位 / 急停 / 报警，详见卡手册）。");
                }
                catch { /* 该卡族没实现停止原因读取 */ }

                double cmd = 0, enc = 0;
                Guard(() => cmd = a.GetCardAxisCurrentPosition(0));
                Guard(() => enc = a.GetCardAxisCurrentPosition(1));
                sb.Append($" 指令位置={cmd:F3}，编码器位置={enc:F3}。");
                if (Math.Abs(cmd) > 0.0001 && Math.Abs(enc) < 0.0001)
                    sb.Append("★指令在变而编码器不动：多半是没使能 / 动力线没接 / 编码器线松。");
            }
            catch { /* 诊断本身失败不影响原始异常 */ }
            return sb.ToString();
        }

        // ===================== 其它辅助 =====================

        /// <summary>底层返回码非 0 时给出警告；为 0 时打印成功说明。</summary>
        private void Report(string action, int res, string okMessage)
        {
            if (res == 0) { Log(okMessage); return; }
            Log($"[卡族·警告] {action}返回非 0（{res}），指令可能未生效——请对照卡手册的错误码表确认。");
        }

        /// <summary>按电平配置决定是否取反（常闭 / 低电平有效 → 取反）。</summary>
        private static int ApplyLevel(int value, string level)
        {
            string s = level ?? string.Empty;
            bool invert = s.Contains("低") || s.Contains("常闭") ||
                          s.IndexOf("NC", StringComparison.OrdinalIgnoreCase) >= 0;
            return invert ? (value != 0 ? 0 : 1) : (value != 0 ? 1 : 0);
        }

        /// <summary>按软限位检查目标位置，避免撞机。</summary>
        private static void CheckSoftLimit(AxisItem axis, double position)
        {
            if (axis.PosLimitPlus == 0 && axis.PosLimitMinus == 0) return;   // 未配置软限位
            if (position > axis.PosLimitPlus || position < axis.PosLimitMinus)
                throw new ScriptRuntimeException(
                    $"轴「{axis.Name}」目标位置 {position} 超出软限位范围 [{axis.PosLimitMinus}, {axis.PosLimitPlus}]，已阻止运动。");
        }

        /// <summary>按名称找 IO 点；名称直接写数字时按「序号」虚拟一个点。</summary>
        private static IoItem FindIo(string name, bool isOutput)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var list = isOutput ? ProjectStore.Data.Outputs : ProjectStore.Data.Inputs;
            var io = list.FirstOrDefault(x => x.Name == name);
            if (io != null) return io;

            if (int.TryParse(name.Trim(), out int seq))
                return new IoItem { Name = name, CardNo = 0, ModuleNo = 0, Sequence = seq };

            return null;
        }

        private void WarnNoAxis(AxisItem axis, string action)
        {
            Log($"[卡族·未执行] 轴「{axis.Name}」{action}：控制器未就绪或未匹配到卡族。");
        }

        private void WarnNoIo(IoItem io, string action)
        {
            Log($"[卡族·未执行] IO「{io.Name}」{action}：控制器未就绪或未匹配到卡族。");
        }

        /// <summary>
        /// 把底层硬件异常翻译成 Lua 能显示的中文错误。
        /// <para>必须兜住<b>所有</b>异常，不能只兜 HardwareOperationException：移植过来的卡族里
        /// 有相当一部分接口是参考实现里未完成的（直接 throw new NotImplementedException），
        /// 也有按下标访问卡参数表的地方（KeyNotFound / IndexOutOfRange）。漏出去的话 Lua 侧只会看到
        /// 一个类型名，操作员完全不知道发生了什么。</para>
        /// </summary>
        private static void Guard(Action action)
        {
            try { action(); }
            catch (ScriptRuntimeException) { throw; }
            catch (Exception ex)
            {
                throw new ScriptRuntimeException(
                    $"控制卡调用失败：{ex.Message}（{ex.GetType().Name}）"
                    + "。该卡族的这个接口在参考实现里可能尚未实现，请核对卡型号与手册。");
            }
        }

        public void Dispose()
        {
            _comm.CloseAll();
            lock (_gate)
            {
                foreach (var s in _slots.Values)
                {
                    try { s.Runtime?.Card?.CloseCard(); } catch { /* 释放失败不抛 */ }
                }
                _slots.Clear();
            }
        }
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
