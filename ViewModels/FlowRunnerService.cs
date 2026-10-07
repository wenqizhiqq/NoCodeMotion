// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁣志‌◆‌编‍写⁠◇‎微‌信‎﹕‌1⁣8‍7‏◆⁣1​9​3‌6‎◇⁠1‎3‎9⁣9‍　‍※‎保​留‌所‍有⁠权‏利​请⁣勿‌删‌除‍◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NoCodeMotion.Models;
using NoCodeMotion.Models.NodeGraph;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Vision;
using NoCodeMotion.Views;
using MoonSharp.Interpreter.Debugging;
using MoonSharp.Interpreter;

namespace NoCodeMotion.ViewModels
{
    /// <summary> 
    /// 并发流程运行控制：跨流程共享的停止 / 急停 / 暂停标志与变量表。
    /// 由 OperatorViewModel 在每个运行周期创建并传入 FlowRunnerService。
    /// </summary>
    public class FlowRunControl : IDisposable
    {
        public volatile bool StopRequested;
        public volatile bool EStopRequested;
        public volatile bool PauseRequested;
        public ManualResetEventSlim ResumeEvent = new(true);

        /// <summary>恢复代数：每次「继续」（ResumeLoop/ResumeAll/页面恢复运行）自增。
        /// 断点挂起的等待方（表格执行器断点闸 / Lua watcher / 节点图 WaitRound）发现代数变化即恢复执行。
        /// （ResumeEvent 初始为有信号，不能直接用来等"下一次继续"，必须用代数。）</summary>
        public int ResumeTick;

        /// <summary>「相机」步骤真实取帧后回调（byte[]=BGRA, w, h），供 3D 仿真抓拍预览订阅。</summary>
        public Action<byte[], int, int>? OnCameraCapture;

        /// <summary>
        /// ★ 硬件阻塞等待（轴到位 / 气缸到位 / 等待输入 / 回零到位）里被「暂停」时的回调。
        /// 由 FlowRunnerService 绑定成「把本条流程状态置 Paused」，让操作员按暂停后
        /// 界面立刻显示「已暂停」，而不是只看到轴在走、状态还是「运行中」。
        /// 为 null 时等待闸只阻塞、不改状态（仍然正确，只是界面反映晚一点）。
        /// </summary>
        public Action? OnWaitPaused;

        /// <summary>★ 硬件阻塞等待从「暂停」恢复时的回调（把状态改回 运行中 / 循环）。</summary>
        public Action? OnWaitResumed;

        /// <summary>
        /// 每步执行后的可视化停顿（毫秒）：让流程页的橙色当前行肉眼可见地逐行移动。
        /// 0 = 不停顿（操作员页生产运行用默认 0，不拖慢节奏）；流程页「运行一次 / 循环运行」传 250。
        /// </summary>
        public int StepPaceMs;

        /// <summary>变量表：名称 -> 值（字符串，数值运算时再解析）。与 ProjectStore.Data.Variables 双向同步。</summary>
        public Dictionary<string, string> Vars = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>运行期被显式写入过的变量名（脏键）。WriteBackVars 只回写这些键——
        /// 否则 InitVars 的旧快照会在收尾时把 节点图/Lua 等直写工程表的值（如 运算+1）冲回旧值，
        /// 表现为「复位后变量没+1」；快速连点时上一次收尾被 gen 作废才侥幸存活。</summary>
        public readonly HashSet<string> Touched = new(StringComparer.OrdinalIgnoreCase);

        public void InitVars()
        {
            Vars.Clear();
            Touched.Clear();
            if (ProjectStore.Data?.Variables == null) return;
            foreach (var row in ProjectStore.Data.Variables)
            {
                Add(row.Name1, row.Value1);
                Add(row.Name2, row.Value2);
                Add(row.Name3, row.Value3);
                Add(row.Name4, row.Value4);
                Add(row.Name5, row.Value5);
            }
        }

        private void Add(string n, string v)
        {
            if (!string.IsNullOrEmpty(n)) Vars[n] = v ?? "";
        }

        /// <summary>
        /// 运行期写变量：同时更新运行仓与工程变量表（VariableRow INPC 即时推送）。
        /// 这样变量页 / 流程页「实际值」列在运行期间就能看到最新值，而不是等运行结束。
        /// 标量属性变更 WPF 会自动封送到 UI 线程，后台线程调用安全（不涉及集合结构变更）。
        /// </summary>
        public void WriteVarLive(string name, string value)
        {
            if (string.IsNullOrEmpty(name)) return;
            Vars[name] = value ?? "";
            Touched.Add(name);
            var vars = ProjectStore.Data?.Variables;
            if (vars == null) return;
            foreach (var row in vars)
            {
                for (int c = 1; c <= 5; c++)
                {
                    string n = c switch
                    {
                        1 => row.Name1, 2 => row.Name2, 3 => row.Name3, 4 => row.Name4, 5 => row.Name5,
                        _ => null
                    };
                    if (string.Equals((n ?? "").Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        switch (c)
                        {
                            case 1: row.Value1 = value ?? ""; break;
                            case 2: row.Value2 = value ?? ""; break;
                            case 3: row.Value3 = value ?? ""; break;
                            case 4: row.Value4 = value ?? ""; break;
                            case 5: row.Value5 = value ?? ""; break;
                        }
                        return;
                    }
                }
            }
        }

        /// <summary>把运行期变量写回工程（VariableRow），方便变量页查看。
        /// 只回写 Touched（运行期真正被写过的键）：视觉结果等只进运行仓的键靠这里落表；
        /// 未触碰的键不回写，避免旧快照覆盖 节点图/Lua 直写工程表的最新值。</summary>
        public void WriteBackVars()
        {
            if (ProjectStore.Data?.Variables == null || Touched.Count == 0) return;
            foreach (var row in ProjectStore.Data.Variables)
            {
                Set(row.Name1, row, 1); Set(row.Name2, row, 2);
                Set(row.Name3, row, 3); Set(row.Name4, row, 4); Set(row.Name5, row, 5);
            }
        }

        private void Set(string n, VariableRow row, int k)
        {
            if (string.IsNullOrEmpty(n)) return;
            if (Touched.Contains(n) && Vars.TryGetValue(n, out var v))
            {
                switch (k)
                {
                    case 1: row.Value1 = v; break;
                    case 2: row.Value2 = v; break;
                    case 3: row.Value3 = v; break;
                    case 4: row.Value4 = v; break;
                    case 5: row.Value5 = v; break;
                }
            }
        }

        public void Dispose() { try { ResumeEvent?.Dispose(); } catch { } }
    }

    /// <summary>跨组件广播 Lua 流程的当前执行行，供流程页的 LuaEditorView 实时跳行高亮。
    /// 运行器用独立 LuaDebugSession 跑，编辑器只订阅本监控，按 FlowItem 匹配后高亮，解耦会话实例。</summary>
    /// <summary>Lua「外部运行」行号广播：独立会话的 LineStepped 在**脚本线程**上触发，
    /// 订阅者（LuaEditorView.OnMonitorLine 等）自己负责封送 UI 线程；这里逐个订阅者 try/catch，
    /// 界面处理异常绝不冒回脚本线程（否则 MoonSharp 记成脚本错误 → 整条循环被 break → "点进去就停"）。</summary>
    public static class LuaRunMonitor
    {
        public static event Action<FlowItem, int> LineChanged;
        public static event Action<FlowItem> RunEnded;

        public static void Report(FlowItem flow, int line)
        {
            var h = LineChanged;
            if (h == null) return;
            foreach (Action<FlowItem, int> d in h.GetInvocationList())
            {
                try { d(flow, line); } catch { /* 界面异常不进脚本线程 */ }
            }
        }

        public static void ReportEnded(FlowItem flow)
        {
            var h = RunEnded;
            if (h == null) return;
            foreach (Action<FlowItem> d in h.GetInvocationList())
            {
                try { d(flow); } catch { }
            }
        }
    }

    /// <summary>
    /// 并发流程执行服务：把 ProjectStore.Data.Flows 里每个 Flow 的「循环开始 / 循环结束」等逻辑区域
    /// 并发执行（每个 Flow 一条后台 Task）。
    /// 这是独立于 FlowViewModel 的运行器——FlowViewModel.cs 为加密文件无法改写——但复用同一套
    /// HardwareBridge / HardwareResolver 硬件接口，语义与流程页单步执行保持一致的最佳实现。
    /// </summary>
    public static class FlowRunnerService
    {
        static FlowRunnerService() { _ = AuthorWatermark.Signature; }   // 作者水印引用（误删 AuthorWatermark.cs 将编译失败）

        /// <summary>并发启动所有流程（每个 Flow 一条后台 Thread，内部 while 循环）。不使用 Task——
        /// 全部走 Thread，结束由看门狗线程在全部流程 Signal 后触发 onComplete。
        /// <paramref name="forceLoop"/>：null=按流程角色（主流程循环 / 复位流程单次，操作员页语义）；
        /// true=强制循环（流程页「循环运行」）；false=强制单次（流程页「运行一次」）。</summary>
        public static void RunAllAsync(
            FlowRunControl ctrl,
            Action<string, LogLevel> log,
            Action<int, string, string> onStep,
            Action<int, string> onFlowDone,
            Action onComplete,
            CancellationToken ct = default,
            Func<FlowItem, bool>? filter = null,
            bool? forceLoop = null)
        {
            var flows = ProjectStore.Data?.Flows?.Where(filter ?? (_ => true)).ToList() ?? new List<FlowItem>();
            if (flows.Count == 0) { onComplete?.Invoke(); return; }

            // ★ 把「等待闸」挂到本次运行的暂停/停止状态上：
            //   硬件阻塞等待（轴到位 / 气缸到位 / 等待输入 / 回零到位）都在线程池/流程线程里，
            //   拿不到这里的 ctrl。挂在 HardwareBridge（静态）上后，卡族桥接层的轮询循环
            //   就能按 ≤50ms 的切片查「暂停 → 等继续」「停止 → 抛出跳过等待」。
            //   ★ 必须在挂钩成功后再起流程线程（否则前几毫秒的等待查不到闸）。
            HookWaitGuard(ctrl);

            var done = new CountdownEvent(flows.Count);
            for (int i = 0; i < flows.Count; i++)
            {
                int idx = i;
                var flow = flows[i];
                bool loop = forceLoop ?? (flow.Role != FlowRole.Reset);
                var th = new Thread(() =>
                {
                    try { RunOneFlow(flow, idx, ctrl, log, onStep, onFlowDone, ct, loop); }
                    catch (OperationCanceledException) { /* 正常中止 */ }
                    catch (ScriptRuntimeException srex)
                    {
                        // 硬件等待现在统一抛 OperationCanceledException（见上分支）；本分支保留为兜底，
                        // 处理任何仍以 ScriptRuntimeException 上抛的等待中断（例如 Lua 延时被中断），
                        // 统一记成「停止」而非「异常」（否则日志一片红字，操作员看不出停止已生效）。
                        bool halted = ctrl.StopRequested || ctrl.EStopRequested;
                        SetStatus(flow, halted ? FlowStatus.Stopped : FlowStatus.Exception);
                        log?.Invoke(halted
                            ? $"流程「{flow?.Name}」已{(ctrl.EStopRequested ? "急停" : "停止")}（等待被中断）。"
                            : $"流程「{flow?.Name}」等待被中断：{srex.Message}", halted ? LogLevel.Warn : LogLevel.Error);
                    }
                    catch (Exception ex) { log?.Invoke($"流程「{flow?.Name}」运行异常：{ex.Message}", LogLevel.Error); }
                    finally
                    {
                        done.Signal();
                        // ★ 退出指纹：一条流程线程结束就记一行（含停止标志）——用来定位"是谁把所有流程停掉的"
                        log?.Invoke($"流程「{flow?.Name}」执行结束（{(loop ? "循环" : "单次")}·Stop={ctrl.StopRequested}·EStop={ctrl.EStopRequested}）。", LogLevel.Info);
                    }
                })
                { IsBackground = true, Name = $"Flow-{idx}" };
                th.Start();
            }
            // 看门狗线程：等所有流程结束 → 触发完成回调（回调内部自行入队到 UI 线程执行）。
            var watch = new Thread(() =>
            {
                try { done.Wait(); } catch { }
                UnhookWaitGuard(ctrl);
                onComplete?.Invoke();
            })
            { IsBackground = true, Name = "FlowWatchdog" };
            watch.Start();
        }

        /// <summary>
        /// ★ 把「等待闸」挂到本次运行的控制对象上（硬件阻塞等待靠它响应操作员暂停/停止）。
        /// <para>同时挂两个可选回调，让「等待中被暂停」也反映到流程状态芯片上
        /// （否则操作员按暂停后，界面还显示「运行中」，看起来像暂停没生效）。</para>
        /// <para>注意：这里是**进程级共享**的闸（HardwareBridge.WaitGuard 是静态的）。
        /// 同批次并发运行的所有流程共用同一个 FlowRunControl，所以互不冲突；
        /// 但若后续要支持「多条流程各自独立暂停」，需要改成按线程 [ThreadStatic] 绑定。</para>
        /// </summary>
        private static void HookWaitGuard(FlowRunControl ctrl)
        {
            if (ctrl == null) return;
            try
            {
                // 回调里用 _guardFlow.Value：每条流程线程在 RunOneFlow 入口写入自己的 FlowItem，
                // 于是「等待中被暂停」能把状态准确写到**正在等的那条流程**上，而不是最后启动的那条。
                ctrl.OnWaitPaused = () => { var f = _guardFlow.Value; if (f != null) FlowRunStore.SetStatus(f, FlowStatus.Paused); };
                ctrl.OnWaitResumed = () => { var f = _guardFlow.Value; if (f != null) FlowRunStore.SetStatus(f, FlowStatus.Running); };
                HardwareBridge.BindWaitGuard(ctrl);
            }
            catch { /* 挂不上闸不影响运行，只是等待不再响应暂停/停止 */ }
        }

        /// <summary>解绑等待闸（流程全部结束后调用，避免手动 Jog 误响应上一次运行的暂停标志）。</summary>
        private static void UnhookWaitGuard(FlowRunControl ctrl)
        {
            try { HardwareBridge.BindWaitGuard(null); } catch { }
            try { ctrl.OnWaitPaused = null; ctrl.OnWaitResumed = null; } catch { }
        }

        /// <summary>正在等待的流程（等闸回调里用来把状态置 Paused）——按流程线程独立。</summary>
        private static readonly ThreadLocal<FlowItem> _guardFlow = new();

        private static void RunOneFlow(FlowItem flow, int index, FlowRunControl ctrl,
            Action<string, LogLevel> log, Action<int, string, string> onStep, Action<int, string> onFlowDone,
            CancellationToken ct, bool loop)
        {
            if (flow == null) return;
            // ★ 在**本流程线程**上登记流程身份：硬件等待被暂停时，等闸回调据此把状态写到正确的流程上。
            _guardFlow.Value = flow;
            var name = flow.Name ?? "(未命名流程)";
            if (flow.Kind == FlowKind.Lua)
            {
                RunOneFlowLua(flow, index, ctrl, log, onStep, onFlowDone, loop);
                return;
            }
            if (flow.Kind == FlowKind.Vision)
            {
                RunOneFlowVision(flow, index, ctrl, log, onStep, onFlowDone, loop);
                return;
            }
            if (flow.Kind == FlowKind.NodeGraph)
            {
                RunOneFlowNodeGraph(flow, index, ctrl, log, onStep, onFlowDone, loop);
                return;
            }
            var steps = flow.Steps?.ToList();
            if (steps == null || steps.Count == 0)
            {
                log?.Invoke($"流程「{name}」没有步骤，已跳过。", LogLevel.Info);
                SetStatus(flow, FlowStatus.Idle);
                onFlowDone?.Invoke(index, name);
                return;
            }

            // loop=true：循环执行——每轮跑完所有步骤后从头再来，直到停止/急停。
            // loop=false：单次执行（复位流程 / 流程页「运行一次」）。
            if (loop)
            {
                log?.Invoke($"流程「{name}」开始循环运行（{steps.Count} 步/轮，直到停止/急停）。", LogLevel.Info);
                int cycle = 0;
                try
                {
                    while (!ctrl.StopRequested && !ctrl.EStopRequested)
                    {
                        cycle++;
                        FlowRunStore.SetCycle(flow, cycle);
                        SetStatus(flow, FlowStatus.Looping);   // 循环运行状态（列表芯片显示「循环」）
                        var exec = new FlowExecutor(flow, index, steps, ctrl, log, onStep, loop);
                        try
                        {
                            exec.Run(ct);
                            log?.Invoke($"流程「{name}」第 {cycle} 轮运行结束。", LogLevel.Info);
                        }
                        catch (OperationCanceledException)
                        {
                            SetStatus(flow, FlowStatus.Stopped);
                            log?.Invoke(ctrl.EStopRequested
                                ? $"流程「{name}」在第 {cycle} 轮急停。"
                                : $"流程「{name}」在第 {cycle} 轮停止。", LogLevel.Warn);
                            return;
                        }
                        catch (FlowAbortException abx)
                        {
                            // 本流程被主动中止（如点位移动条件未满足）：只结束这一条，同批次其它流程继续跑
                            SetStatus(flow, FlowStatus.Exception);
                            log?.Invoke($"流程「{name}」已中止：{abx.Message}", LogLevel.Error);
                            return;
                        }
                        catch (Exception ex)
                        {
                            SetStatus(flow, FlowStatus.Exception);
                            log?.Invoke($"流程「{name}」第 {cycle} 轮运行异常：{ex.Message}", LogLevel.Error);
                            Thread.Sleep(500);   // 避免持续异常时紧密重试刷屏
                        }
                        finally
                        {
                            exec.ClearCurrent();   // 每轮结束清掉上一轮的高亮行
                        }
                        Thread.Sleep(1);   // 让出 CPU，避免 while 紧密循环抢占 UI 线程导致卡顿
                    }
                    SetStatus(flow, FlowStatus.Stopped);
                    log?.Invoke(ctrl.EStopRequested
                        ? $"流程「{name}」已急停（{cycle} 轮）。"
                        : $"流程「{name}」已停止（{cycle} 轮）。", LogLevel.Warn);
                }
                finally
                {
                    onFlowDone?.Invoke(index, name);
                }
                return;
            }

            // 单次执行（复位流程 / 流程页「运行一次」）
            SetStatus(flow, FlowStatus.Running);
            var exec2 = new FlowExecutor(flow, index, steps, ctrl, log, onStep);
            log?.Invoke($"流程「{name}」开始运行（{steps.Count} 步，单次执行）。", LogLevel.Info);
            try
            {
                exec2.Run(ct);
                SetStatus(flow, FlowStatus.Idle);
                log?.Invoke($"流程「{name}」运行结束。", LogLevel.Info);
            }
            catch (OperationCanceledException)
            {
                SetStatus(flow, FlowStatus.Stopped);
                log?.Invoke($"流程「{name}」已停止。", LogLevel.Warn);
            }
            catch (Exception ex)
            {
                SetStatus(flow, FlowStatus.Exception);
                log?.Invoke($"流程「{name}」运行异常：{ex.Message}", LogLevel.Error);
            }
            finally
            {
                exec2.ClearCurrent();
                onFlowDone?.Invoke(index, name);
            }
        }

        /// <summary>把流程状态写入线程安全共享态 FlowRunStore。不直接碰 UI——UI 由 OperatorViewModel 的
/// DispatcherTimer 周期拉取并推到 FlowItem.Status，从而运行期高频刷新与界面渲染解耦，流程再快也不卡界面。</summary>
        private static void SetStatus(FlowItem flow, FlowStatus st)
        {
            FlowRunStore.SetStatus(flow, st);
        }

        /// <summary>Lua 脚本流程：**唯一路径 = 独立后台 LuaDebugSession 连续运行**（thread + while + 1ms），
        /// 与编辑器页面彻底解耦——进入/离开脚本页面、页面 Loaded/卸载、编辑器忙闲都绝不影响运行。
        /// 当前行广播给 LuaRunMonitor + 写 FlowRunStore.SetProgress，脚本页用 100ms 轮询定时器刷行号与状态。
        /// 暂停/停止映射到会话的 RequestPause/Resume/Stop。</summary>
        private static void RunOneFlowLua(FlowItem flow, int index, FlowRunControl ctrl,
            Action<string, LogLevel> log, Action<int, string, string> onStep, Action<int, string> onFlowDone, bool loop)
        {
            var name = flow.Name ?? "(未命名流程)";
            // 循环运行 → Looping（列表芯片显示「循环」）；单次（运行一次 / 复位流程）→ Running
            SetStatus(flow, loop ? FlowStatus.Looping : FlowStatus.Running);
            // Lua Main 循环：每轮跑完脚本后判断 Ended.IsError，一旦脚本报错就停止循环并把状态置 Exception，
            // 避免早前实现的「错误也立即重启」造成的 LuaScriptThread/LuaWatch 死循环刷屏与 UI 卡顿。
            try
            {
            int luaRound = 0;   // 单次只跑一轮：首轮 luaRound==0 进入，之后置 1 → 退出；循环模式无限循环
            // ★ 唯一路径：独立后台会话（thread + while + 1ms），与编辑器页面彻底解耦——
            //   进入/离开脚本页面、页面 Loaded/卸载、编辑器忙闲，都绝不影响运行；
            //   页面用 100ms 轮询定时器（PollExternalRun）+ LuaRunMonitor 广播刷行号与状态。
            // loop=true 持续循环直到停止/急停；loop=false（复位流程 / 运行一次）只跑一轮。
            // 用户在脚本页点「停止」→ 编辑器置 EditorStopRequested（本页无会话但共享态显示在跑）→ 这里消费为停止。
            while ((loop || luaRound == 0) && !ctrl.StopRequested && !LuaEditorView.EditorStopRequested && !ctrl.EStopRequested)
            {
                if (LuaEditorView.EditorStopRequested) { ctrl.StopRequested = true; LuaEditorView.EditorStopRequested = false; }
                luaRound++;
                FlowRunStore.SetCycle(flow, luaRound);
                Thread.Sleep(1);   // 让出 CPU，避免紧密循环抢占 UI 线程
                var ended = new ManualResetEventSlim(false);
                ExecutionEndedInfo lastEnded = null;
                try
                {
                    var session = new LuaDebugSession();
                    session.FlowName = name;   // 点位防撞报警里标出是哪条流程
                    session.RunControl = ctrl; // ★ 透传暂停/停止源：Lua 的 Delay/WaitStep 才被打断
                    // 断点：注入编辑器里为该脚本设置的断点行（命中即暂停，watcher 里等「继续」）
                    var luaBps = LuaEditorView.GetBreakpoints(flow);
                    if (luaBps.Count > 0) session.SetBreakpoints(luaBps);
                    session.Log += (m, k) => log?.Invoke($"[Lua:{name}] {m}", LogLevel.Info);
                    session.LineStepped += line =>
                    {
                        LuaRunMonitor.Report(flow, line);
                        onStep?.Invoke(index, name, $"Lua 行 {line}");
                        FlowRunStore.SetStep(flow, $"Lua 行 {line}");
                        FlowRunStore.SetProgress(flow, line: line);   // 供脚本页定时器轮询刷新行号高亮
                    };
                    session.Ended += info =>
                    {
                        lastEnded = info;
                        if (info.IsError) log?.Invoke($"[Lua:{name}] 运行错误（行 {info.ErrorLine}）：{info.Message}", LogLevel.Error);
                        ended.Set();
                    };
                    // 循环运行不逐轮刷日志（原来每轮一条，循环时淹没输出面板）：第 1 轮 + 每 100 轮一条
                    if (!loop || luaRound == 1 || luaRound % 100 == 0)
                        log?.Invoke($"流程「{name}」开始连续运行（Lua{(loop ? $"，已 {luaRound} 轮" : "，单次")}）。", LogLevel.Info);
                    session.Start(flow.LuaSource ?? "", false);
                    var watcher = new Thread(() =>
                    {
                        while (!ended.Wait(10))
                        {
                            if (ctrl.EStopRequested || ctrl.StopRequested) { session?.Stop(); break; }
                            // ★ 断点命中：会话被 MoonSharp 暂停 → 状态置 Breakpoint，挂起等「继续」
                            //   （流程页「运行一次/循环运行」、脚本页/节点图页「继续」、操作员「继续」都会自增 ResumeTick）
                            if (session.State == SessionState.Paused)
                            {
                                SetStatus(flow, FlowStatus.Breakpoint);
                                int tick0 = ctrl.ResumeTick;
                                while (session.State == SessionState.Paused && !ended.Wait(10))
                                {
                                    if (ctrl.StopRequested || ctrl.EStopRequested || LuaEditorView.EditorStopRequested)
                                    { session?.Stop(); break; }
                                    if (ctrl.ResumeTick != tick0) session?.Resume(DebuggerAction.ActionType.Run);
                                }
                                if (ctrl.StopRequested || ctrl.EStopRequested || LuaEditorView.EditorStopRequested) break;
                                SetStatus(flow, loop ? FlowStatus.Looping : FlowStatus.Running);
                            }
                            if (ctrl.PauseRequested)
                            {
                                SetStatus(flow, FlowStatus.Paused);
                                session?.RequestPause();
                                while (ctrl.PauseRequested && !ended.Wait(10)) { }
                                if (!ended.IsSet && !ctrl.PauseRequested)
                                {
                                    session?.Resume(DebuggerAction.ActionType.Run);
                                    SetStatus(flow, loop ? FlowStatus.Looping : FlowStatus.Running);
                                }
                            }
                        }
                    }) { IsBackground = true, Name = $"LuaWatch-{index}" };
                    watcher.Start();
                    ended.Wait();
                }
                catch (Exception ex)
                {
                    // ★ 停止/急停触发的等待中断（Lua 侧以 ScriptRuntimeException 上抛）是**预期**的「跳出等待」，
                    // 记成停止而不是异常——否则日志一片红字，操作员看不出「我按的停止确实起作用了」。
                    if (ctrl.StopRequested || ctrl.EStopRequested)
                        log?.Invoke($"流程「{name}」已{(ctrl.EStopRequested ? "急停" : "停止")}（等待被中断）。", LogLevel.Warn);
                    else
                        log?.Invoke($"流程「{name}」Lua 运行异常：{ex.Message}", LogLevel.Error);
                }
                finally { LuaRunMonitor.ReportEnded(flow); }
                if (ctrl.EStopRequested) { log?.Invoke($"流程「{name}」已急停。", LogLevel.Warn); break; }
                if (ctrl.StopRequested) { log?.Invoke($"流程「{name}」已停止。", LogLevel.Warn); break; }
                // 错误结束 → 跳出循环，状态置 Exception，不再重试（早前实现刷屏根因）
                if (lastEnded != null && lastEnded.IsError)
                {
                    SetStatus(flow, FlowStatus.Exception);
                    log?.Invoke($"流程「{name}」脚本报错（行 {lastEnded.ErrorLine}）：{lastEnded.Message} — 已停止重试，请修正脚本后重新启动。", LogLevel.Error);
                    break;
                }
                // 正常结束一轮 → 轮间隔 1ms 再起下一轮（while 循环只需 1ms 让出 CPU）
                Thread.Sleep(1);
            }
            onFlowDone?.Invoke(index, name);
            }
            finally
            {
                LuaEditorView.EditorStopRequested = false;   // 本条 Lua 流程已结束，清掉编辑器停止标志（避免残留影响后续运行）
                // 运行不再驱动编辑器页面，无需 ClearOperatorDriven（编辑器状态由页面自己的轮询定时器维护）
                // 收尾状态：被停止 / 急停 → Stopped；脚本错误 → 保持已置 Exception；其它正常完成 → Idle
                // 注意：必须检查 FlowRunStore 中的实时状态，而不是 flow.Status——
                // flow.Status 由 OperatorViewModel 的 DispatcherTimer 异步写入，有最多 150ms 滞后，
                // 错误分支刚 SetStatus(Exception) 立刻 break 时 flow.Status 还是旧 Running，
                // 会错误地把 Exception 覆盖成 Idle（早前 Lua 流程一直「就绪」的根因）。
                if (ctrl.EStopRequested || ctrl.StopRequested)
                    SetStatus(flow, FlowStatus.Stopped);
                else if (FlowRunStore.Get(flow).Status != FlowStatus.Exception)
                    SetStatus(flow, FlowStatus.Idle);
            }
        }

        /// <summary>视觉流程：复用 VisionEngine 真实执行 图像采集 / 预处理 / 模板匹配 / 缺陷检测 / 测量 / 通讯
        /// 六类算子，每轮跑完整条视觉流程后从头再来（Role=Main 循环；Role=Reset 单次）。运行期高频写回只走
        /// FlowRunStore 与进度回调，界面由 OperatorViewModel 的 DispatcherTimer 周期拉取，不卡界面。</summary>
        private static void RunOneFlowVision(FlowItem flow, int index, FlowRunControl ctrl,
            Action<string, LogLevel> log, Action<int, string, string> onStep, Action<int, string> onFlowDone, bool loop)
        {
            var name = flow.Name ?? "(未命名流程)";
            var steps = flow.VisualSteps?.ToList();
            if (steps == null || steps.Count == 0)
            {
                log?.Invoke($"视觉流程「{name}」没有步骤，已跳过。", LogLevel.Info);
                SetStatus(flow, FlowStatus.Idle);
                onFlowDone?.Invoke(index, name);
                return;
            }

            // 进度回调：只入队日志（线程安全 ConcurrentQueue）并写共享态，不碰 UI。
            IProgress<string> progress = new DirectProgress(msg =>
            {
                log?.Invoke($"[视觉:{name}] {msg}", LogLevel.Info);
                FlowRunStore.SetStep(flow, msg);
            });

            // loop=true：循环运行——每轮跑完整条视觉流程后从头再来，直到停止 / 急停。
            if (loop)
            {
                log?.Invoke($"视觉流程「{name}」开始循环运行（{steps.Count} 步/轮，直到停止/急停）。", LogLevel.Info);
                int cycle = 0;
                try
                {
                    while (!ctrl.StopRequested && !ctrl.EStopRequested)
                    {
                        // 暂停：以整轮为粒度响应——暂停期间阻塞在 ResumeEvent，恢复后继续下一轮。
                        if (ctrl.PauseRequested)
                        {
                            SetStatus(flow, FlowStatus.Paused);
                            try { ctrl.ResumeEvent?.Wait(); } catch { }
                            if (ctrl.StopRequested || ctrl.EStopRequested) break;
                            SetStatus(flow, FlowStatus.Looping);   // 循环运行状态
                        }
                        cycle++;
                        FlowRunStore.SetCycle(flow, cycle);
                        SetStatus(flow, FlowStatus.Looping);   // 循环运行状态（列表芯片显示「循环」）
                        // 断点闸：视觉步骤设了断点 → 状态 Breakpoint 挂起，等「继续」（ResumeTick 变化）或停止/急停
                        void VisionBreakpointGate(Models.VisualFlowStep s)
                        {
                            FlowRunStore.SetStatus(flow, FlowStatus.Breakpoint);
                            int tick0 = ctrl.ResumeTick;
                            while (ctrl.ResumeTick == tick0)
                            {
                                if (ctrl.StopRequested || ctrl.EStopRequested)
                                    throw new OperationCanceledException();
                                Thread.Sleep(15);
                            }
                            FlowRunStore.SetStatus(flow, FlowStatus.Looping);
                        }
                        try
                        {
                            var report = VisionEngine.Run(steps, progress, VisionBreakpointGate);
                            bool anyFail = report.Results.Any(r => !r.Ok);
                            log?.Invoke($"视觉流程「{name}」第 {cycle} 轮完成（{(anyFail ? "有失败步骤" : "全部通过")}，{report.Results.Count} 步）。", LogLevel.Info);
                        }
                        catch (OperationCanceledException)
                        {
                            // 断点挂起期间点停止/急停：按停止收尾（不是异常）
                            SetStatus(flow, FlowStatus.Stopped);
                            log?.Invoke($"视觉流程「{name}」在断点处被停止。", LogLevel.Warn);
                            break;
                        }
                        catch (Exception ex)
                        {
                            SetStatus(flow, FlowStatus.Exception);
                            log?.Invoke($"视觉流程「{name}」第 {cycle} 轮运行异常：{ex.Message}", LogLevel.Error);
                            Thread.Sleep(500);   // 避免持续异常时紧密重试刷屏
                        }
                        Thread.Sleep(1);   // 让出 CPU，避免 while 紧密循环抢占 UI 线程导致卡顿
                    }
                    SetStatus(flow, FlowStatus.Stopped);
                    log?.Invoke(ctrl.EStopRequested
                        ? $"视觉流程「{name}」已急停（{cycle} 轮）。"
                        : $"视觉流程「{name}」已停止（{cycle} 轮）。", LogLevel.Warn);
                }
                finally
                {
                    onFlowDone?.Invoke(index, name);
                }
                return;
            }

            // 复位流程：单次执行（跑完即结束，不循环）。断点闸：视觉步骤设了断点 → 挂起等「继续」。
            SetStatus(flow, FlowStatus.Running);
            log?.Invoke($"视觉流程「{name}」开始运行（{steps.Count} 步，复位流程单次执行）。", LogLevel.Info);
            void VisionBreakpointGateSingle(Models.VisualFlowStep s)
            {
                FlowRunStore.SetStatus(flow, FlowStatus.Breakpoint);
                int tick0 = ctrl.ResumeTick;
                while (ctrl.ResumeTick == tick0)
                {
                    if (ctrl.StopRequested || ctrl.EStopRequested) throw new OperationCanceledException();
                    Thread.Sleep(15);
                }
                FlowRunStore.SetStatus(flow, FlowStatus.Running);
            }
            try
            {
                var report = VisionEngine.Run(steps, progress, VisionBreakpointGateSingle);
                SetStatus(flow, FlowStatus.Idle);
                log?.Invoke($"视觉流程「{name}」运行结束。", LogLevel.Info);
            }
            catch (OperationCanceledException)
            {
                SetStatus(flow, FlowStatus.Stopped);
                log?.Invoke($"视觉流程「{name}」在断点处被停止。", LogLevel.Warn);
            }
            catch (Exception ex)
            {
                SetStatus(flow, FlowStatus.Exception);
                log?.Invoke($"视觉流程「{name}」运行异常：{ex.Message}", LogLevel.Error);
            }
            finally
            {
                onFlowDone?.Invoke(index, name);
            }
        }

        /// <summary>节点图流程：与节点图页面同一套 NgRunner 执行器（真硬件 + 变量桥接，读走工程变量表
        /// GetVariableResolved、写走 SetVariable 双写），后台整图执行。Role=Main 循环（轮间 200ms）；
        /// Role=Reset / 流程页「运行一次」单次。每次运行新建独立 runner，不与节点图页共用实例——
        /// 页面开着时两边互不打架（与 Lua 脚本独立会话同一思路）。</summary>
        private static void RunOneFlowNodeGraph(FlowItem flow, int index, FlowRunControl ctrl,
            Action<string, LogLevel> log, Action<int, string, string> onStep, Action<int, string> onFlowDone, bool loop)
        {
            var name = flow.Name ?? "(未命名流程)";
            NgDoc doc;
            try { doc = NgDoc.FromJson(flow.GraphJson ?? ""); }
            catch (Exception ex)
            {
                SetStatus(flow, FlowStatus.Exception);
                log?.Invoke($"节点图流程「{name}」解析失败：{ex.Message}", LogLevel.Error);
                onFlowDone?.Invoke(index, name);
                return;
            }
            if (doc.Nodes.Count == 0)
            {
                SetStatus(flow, FlowStatus.Idle);
                log?.Invoke($"节点图流程「{name}」是空图，已跳过。请在流程页选中它并编辑节点。", LogLevel.Info);
                onFlowDone?.Invoke(index, name);
                return;
            }

            var runner = new NgRunner(
                HardwareResolver.ResolveAxis,
                HardwareResolver.ResolveInput,
                HardwareResolver.ResolveOutput,
                HardwareResolver.ResolveCylinder,
                HardwareResolver.ResolveComm,
                SimRuntime.SetVariable,
                SimRuntime.GetVariableResolved);
            runner.FlowName = name;   // 点位防撞报警里标出是哪条流程
            runner.Load(doc);
            FlowRunStore.SetLoopRunner(flow, runner);   // 注册后台 runner：页面「单步」在暂停/断点挂起时推进它
            // 断点：从共享态取页面 VM 设置的断点（页面切换断点时写入 FlowRunStore）。
            var bps = FlowRunStore.GetBreakpoints(flow);
            if (bps != null) runner.SetBreakpoints(bps);
            // 断点语义：循环运行 → 到断点真暂停（挂起等「继续」，ResumeTick 唤醒，见 WaitRound）；
            // 后台单次（复位流程/流程页运行一次）→ 只标记「触发断点」不暂停（单次没有统一恢复入口，暂停会卡死流程）。
            runner.BreakOnHit = loop;
            // 进度写共享态（节点图页用定时器轮询刷新"当前节点高亮 + 各节点结果"，运行器不往页面推）。
            void PublishNodeProgress()
            {
                try
                {
                    FlowRunStore.SetProgress(flow, nodeId: runner.CurrentNodeId ?? "");
                    FlowRunStore.SetNodeResults(flow, runner.Report.Results);
                    FlowRunStore.SetTriggeredBreakpoint(flow, runner.Report.TriggeredBreakpointId ?? "");
                }
                catch { /* 展示用途，失败不影响运行 */ }
            }
            runner.ReportChanged += PublishNodeProgress;
            runner.StateChanged += PublishNodeProgress;

            // 一轮完成信号：State 离开 Running/Stepping（Completed/Stopped/Error）即视为本轮结束
            var roundDone = new ManualResetEventSlim(false);
            runner.StateChanged += () =>
            {
                var st = runner.State;
                if (st != NgRunState.Running && st != NgRunState.Stepping) roundDone.Set();
            };

            // 等本轮跑完；等待期间响应 停止/急停 → runner.Stop()（幂等），StateChanged 置位后退出。
            // 15ms 轮询：单圈毫秒级的图不因检测延迟拖慢循环节奏。
            void WaitRound()
            {
                // 卡轮诊断：正常循环时线程也"住"在这里（每轮之间都在等），不是卡死；
                // 但若某个节点一直不结束（等待类节点在等硬件/信号满足，或图里有回接连线让本轮走不完），
                // 就会永远停在 WaitRound。同一节点停留 ≥5 秒 → 每 5 秒记一条日志点出卡在哪个节点。
                var sw = System.Diagnostics.Stopwatch.StartNew();
                string lastNode = "";
                int logged = 0;
                while (true)
                {
                    if (!roundDone.Wait(1))
                    {
                        if (ctrl.StopRequested || ctrl.EStopRequested) { runner.Stop(); return; }
                        var node = runner.CurrentNodeId ?? "";
                        if (node != lastNode) { lastNode = node; sw.Restart(); logged = 0; continue; }
                        int sec = (int)sw.Elapsed.TotalSeconds;
                        if (sec >= 5 && sec / 5 > logged)
                        {
                            logged = sec / 5;
                            log?.Invoke($"节点图流程「{name}」在节点「{runner.DescribeNode(lastNode)}」上已停留 {sec} 秒" +
                                $"（等待类节点在等硬件/信号满足，或图里有回接连线导致本轮不结束；点「停止」可立即终止）。", LogLevel.Warn);
                        }
                        continue;
                    }
                    // roundDone 置位：Completed/Stopped/Error = 本轮结束；Paused = 断点/单步挂起。
                    // 挂起期间：继续（ResumeTick 变化）→ 跑到本轮结束；单步（页面直接推进 runner）→ 走一个节点再挂起。
                    if (runner.State == NgRunState.Paused)
                    {
                        SetStatus(flow, FlowStatus.Breakpoint);
                        int tick0 = ctrl.ResumeTick;
                        while (runner.State == NgRunState.Paused)
                        {
                            if (ctrl.StopRequested || ctrl.EStopRequested) { runner.Stop(); return; }
                            if (ctrl.ResumeTick != tick0) { runner.Resume(); break; }
                            Thread.Sleep(15);
                        }
                        if (!ctrl.StopRequested && !ctrl.EStopRequested) SetStatus(flow, FlowStatus.Looping);
                        roundDone.Reset();   // 恢复/单步后：继续等本轮真正结束（单步会再次 Paused）
                        continue;
                    }
                    return;   // 本轮真正结束
                }
            }

            try
            {
                if (loop)
                {
                    int cycle = 0;
                    string lastErr = "";
                    while (!ctrl.StopRequested && !ctrl.EStopRequested)
                    {
                        // 暂停：整轮粒度——暂停期间阻塞在 ResumeEvent，恢复后继续下一轮
                        if (ctrl.PauseRequested)
                        {
                            SetStatus(flow, FlowStatus.Paused);
                            try { ctrl.ResumeEvent?.Wait(); } catch { }
                            if (ctrl.StopRequested || ctrl.EStopRequested) break;
                            SetStatus(flow, FlowStatus.Looping);   // 循环运行状态
                        }
                        cycle++;
                        FlowRunStore.SetCycle(flow, cycle);
                        SetStatus(flow, FlowStatus.Looping);   // 循环运行状态（列表芯片显示「循环」）
                        onStep?.Invoke(index, name, $"节点图 第 {cycle} 轮");
                        FlowRunStore.SetStep(flow, $"节点图 第 {cycle} 轮");
                        // 高频循环不逐轮刷日志（淹没日志页还拖慢节奏）：第 1 轮 + 之后每 100 轮记一条
                        if (cycle == 1 || cycle % 100 == 0)
                            log?.Invoke($"节点图流程「{name}」已循环 {cycle} 轮。", LogLevel.Info);
                        roundDone.Reset();
                        // 「单步」可能在暂停期间启动了 step-mode run（state=Paused）→ 不要 Run() 把它重置，
                        // 预置 roundDone 让 WaitRound 直接进入挂起分支（等 继续/再单步）
                        if (runner.State != NgRunState.Paused) runner.Run();
                        else roundDone.Set();
                        WaitRound();
                        if (!string.IsNullOrEmpty(runner.LastError) && runner.LastError != lastErr)
                        {
                            lastErr = runner.LastError;
                            log?.Invoke($"节点图流程「{name}」第 {cycle} 轮提示：{lastErr}", LogLevel.Warn);
                        }
                        // 安全阻断（如点位移动条件未满足）：报警已写，结束循环不再逐轮重试刷屏
                        if (runner.State == NgRunState.Error)
                        {
                            log?.Invoke($"节点图流程「{name}」已中止：{runner.LastError}", LogLevel.Error);
                            break;
                        }
                        if (ctrl.EStopRequested || ctrl.StopRequested) break;
                        Thread.Sleep(1);   // 轮间 1ms：单圈毫秒级的图 ≈ 每秒 1000+ 轮；空图也不会占满 CPU
                    }
                    SetStatus(flow, FlowStatus.Stopped);
                    log?.Invoke(ctrl.EStopRequested
                        ? $"节点图流程「{name}」已急停（{cycle} 轮）。"
                        : $"节点图流程「{name}」已停止（{cycle} 轮）。", LogLevel.Warn);
                }
                else
                {
                    SetStatus(flow, FlowStatus.Running);
                    log?.Invoke($"节点图流程「{name}」开始运行（单次）。", LogLevel.Info);
                    onStep?.Invoke(index, name, "节点图 单次运行");
                    FlowRunStore.SetStep(flow, "节点图 单次运行");
                    roundDone.Reset();
                    runner.Run();
                    WaitRound();
                    bool halted = ctrl.EStopRequested || ctrl.StopRequested;
                    if (!string.IsNullOrEmpty(runner.LastError))
                        log?.Invoke($"节点图流程「{name}」运行提示：{runner.LastError}", halted ? LogLevel.Warn : LogLevel.Error);
                    SetStatus(flow, halted ? FlowStatus.Stopped : FlowStatus.Idle);
                    log?.Invoke(halted ? $"节点图流程「{name}」已停止。" : $"节点图流程「{name}」运行结束。",
                        halted ? LogLevel.Warn : LogLevel.Info);
                }
            }
            finally
            {
                FlowRunStore.SetProgress(flow, nodeId: "");   // 运行结束：页面定时器不再高亮"当前节点"（保留最后一次结果）
                FlowRunStore.SetTriggeredBreakpoint(flow, ""); // 运行结束：清掉"触发断点"角标
                FlowRunStore.SetLoopRunner(flow, null);        // 运行结束：注销后台 runner
                onFlowDone?.Invoke(index, name);
            }
        }

        /// <summary>无 SynchronizationContext 依赖的进度回调：直接在调用线程（后台流程线程）执行，
        /// 仅做线程安全操作（入队日志 + 写共享态），不触发任何 UI 刷新。</summary>
        private sealed class DirectProgress : IProgress<string>
        {
            private readonly Action<string> _cb;
            public DirectProgress(Action<string> cb) => _cb = cb;
            public void Report(string value) => _cb?.Invoke(value);
        }
    }

    /// <summary>单条流程的执行器：递归解释 循环开始/循环结束、如果/否则如果/否则/结束 等逻辑，并执行各功能步骤。</summary>
    /// <summary>单条流程中止（**只结束这一条**，同批次其它流程继续跑）。
    /// 用于防撞类场景，如「点位移动条件未满足」：旧实现把共享的 `_ctrl.StopRequested` 置位，
    /// 结果操作员一次启动里的**所有**流程全被拖停（用户报"一运行就都停了"）。</summary>
    internal sealed class FlowAbortException : Exception
    {
        public FlowAbortException(string message) : base(message) { }
    }

    /// <summary>循环运行管理器（**静态、与页面生命周期无关**）：
    /// 每条流程的「循环运行」都在这里托管 —— 独立后台 Thread（FlowRunnerService）+ 独立 FlowRunControl
    /// （每条流程可单独 停止 / 暂停 / 继续）。
    /// ★ 页面（流程页 / 脚本页 / 节点图页 / 操作员页）只是"调用入口 + 定时器轮询显示"：
    ///   切页、页面卸载、重新绑定都**不会影响正在运行的循环**（thread + while 循环在静态类里持续跑）。</summary>
    public static class FlowLoopManager
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<FlowItem, FlowRunControl> _loops = new();

        /// <summary>该流程当前是否正在循环运行。</summary>
        public static bool IsLooping(FlowItem? flow)
            => flow != null && _loops.TryGetValue(flow, out var c) && !c.StopRequested && !c.EStopRequested;

        /// <summary>启动某条流程的循环运行（已在跑则忽略）。每条流程独立 ctrl → 可单独 停止/暂停/继续。
        /// <paramref name="configure"/>：调用方对 ctrl 的附加配置（如操作员页的 3D 取像回调）。</summary>
        public static void StartLoop(FlowItem flow, Action<FlowRunControl>? configure = null)
        {
            if (flow == null || IsLooping(flow)) return;
            _loops.TryGetValue(flow, out var old);
            var ctrl = new FlowRunControl();
            ctrl.InitVars();
            ctrl.StepPaceMs = 0;
            configure?.Invoke(ctrl);
            _loops[flow] = ctrl;
            FlowRunStore.Clear(flow);
            FlowRunStore.SetStatus(flow, FlowStatus.Looping);
            FlowRunStore.SetCycle(flow, 0);
            FlowRunnerService.RunAllAsync(
                ctrl,
                log: null,
                onStep: null,
                onFlowDone: null,
                onComplete: () =>
                {
                    // 只清理仍然指向本 ctrl 的条目（避免误删新一轮）
                    if (_loops.TryGetValue(flow, out var c) && ReferenceEquals(c, ctrl))
                        _loops.TryRemove(flow, out _);
                    FlowRunStore.SetStatus(flow, FlowStatus.Idle);
                },
                filter: f => ReferenceEquals(f, flow),
                forceLoop: true);
        }

        /// <summary>停止某条流程的循环运行（未在跑则忽略）。</summary>
        public static void StopLoop(FlowItem? flow)
        {
            if (flow == null) return;
            if (_loops.TryRemove(flow, out var ctrl))
            {
                ctrl.StopRequested = true;
                ctrl.ResumeEvent.Set();
            }
        }

        /// <summary>暂停某条流程的循环运行（整轮/步骤边界粒度）。
        /// ★ 必须 Reset ResumeEvent：引擎的暂停门是 `ResumeEvent.Wait()`，事件初始为有信号，
        ///   不 Reset 的话 Wait 立即返回 → 流程继续跑（暂停形同虚设）。恢复时 ResumeLoop 再 Set。</summary>
        public static void PauseLoop(FlowItem? flow)
        {
            if (flow != null && _loops.TryGetValue(flow, out var c))
            {
                c.PauseRequested = true;
                c.ResumeEvent.Reset();
                FlowRunStore.SetStatus(flow, FlowStatus.Paused);   // 立即反映到列表芯片/状态文字（线程到边界时会再写一次）
            }
        }

        /// <summary>继续某条流程的循环运行。</summary>
        public static void ResumeLoop(FlowItem? flow)
        {
            if (flow != null && _loops.TryGetValue(flow, out var c))
            {
                c.PauseRequested = false;
                c.ResumeEvent.Set();
                System.Threading.Interlocked.Increment(ref c.ResumeTick);   // 唤醒断点挂起的等待方
            }
        }

        /// <summary>停止全部循环运行（操作员页「停止 / 急停 / 复位」用）。</summary>
        public static void StopAll()
        {
            foreach (var kv in _loops)
            {
                kv.Value.StopRequested = true;
                kv.Value.ResumeEvent.Set();
            }
            _loops.Clear();
        }

        /// <summary>暂停全部循环运行（操作员页「暂停」= 暂停所有流程）。</summary>
        public static void PauseAll()
        {
            foreach (var kv in _loops)
            {
                kv.Value.PauseRequested = true;
                kv.Value.ResumeEvent.Reset();
                FlowRunStore.SetStatus(kv.Key, FlowStatus.Paused);
            }
        }

        /// <summary>继续全部循环运行。</summary>
        public static void ResumeAll()
        {
            foreach (var kv in _loops)
            {
                kv.Value.PauseRequested = false;
                kv.Value.ResumeEvent.Set();
                System.Threading.Interlocked.Increment(ref kv.Value.ResumeTick);
            }
        }
    }

    internal class FlowExecutor
    {
        private readonly FlowItem _flow;
        private readonly int _index;
        private readonly List<FlowStep> _steps;
        private readonly FlowRunControl _ctrl;
        private readonly Action<string, LogLevel> _log;
        private readonly Action<int, string, string> _onStep;
        private IHardwareBridge _bridge => HardwareBridge.Current;
        private long _guard;
        private bool _pauseActive;     // 当前流程是否处于暂停状态（控制列表右侧"暂"芯片切换）
        private readonly bool _loop;   // 是否循环运行（暂停恢复后状态回 Looping 而非 Running）

        public FlowExecutor(FlowItem flow, int index, List<FlowStep> steps, FlowRunControl ctrl,
            Action<string, LogLevel> log, Action<int, string, string> onStep, bool loop = false)
        {
            _flow = flow; _index = index; _steps = steps; _ctrl = ctrl; _log = log; _onStep = onStep; _loop = loop;
            // 桥接实例不再在构造时缓存，改为每次从 HardwareBridge.Current 读取，
            // 防止「先打开流程页、后连接控制器」时执行器一直用 Stub 而实际值读真实硬件。
        }

        public void Run(CancellationToken ct) => ExecBlock(0, _steps.Count, ct);

        /// <summary>运行结束/中止后清除当前行高亮（写共享态，页面定时器轮询刷新）。</summary>
        public void ClearCurrent() => FlowRunStore.SetProgress(_flow, stepIndex: -1);

        /// <summary>断点闸：当前行设置了断点 → 状态置 Breakpoint（页面显示「触发断点」）并挂起本线程，
        /// 直到「继续」（ResumeTick 变化：流程页「运行/循环运行」、操作员「继续」都会自增）或 停止/急停。
        /// 恢复后状态回 Looping/Running，继续往下执行；下一轮再经过该行仍会暂停（真断点语义）。</summary>
        private void BreakpointGate(CancellationToken ct)
        {
            FlowRunStore.SetStatus(_flow, FlowStatus.Breakpoint);
            int tick = _ctrl.ResumeTick;
            while (_ctrl.ResumeTick == tick)
            {
                if (_ctrl.EStopRequested || _ctrl.StopRequested) throw new OperationCanceledException();
                ct.ThrowIfCancellationRequested();
                Thread.Sleep(15);
            }
            FlowRunStore.SetStatus(_flow, _loop ? FlowStatus.Looping : FlowStatus.Running);
        }

        private void AbortCheck(CancellationToken ct)
        {
            if (_ctrl.EStopRequested || _ctrl.StopRequested) throw new OperationCanceledException();
            if (ct.IsCancellationRequested) throw new OperationCanceledException();
            if (_ctrl.PauseRequested)
            {
                if (!_pauseActive)
                {
                    _pauseActive = true;
                    FlowRunStore.SetStatus(_flow, FlowStatus.Paused);
                }
                _ctrl.ResumeEvent?.Wait(ct);
                if (_pauseActive)
                {
                    _pauseActive = false;
                    FlowRunStore.SetStatus(_flow, _loop ? FlowStatus.Looping : FlowStatus.Running);
                }
                if (_ctrl.EStopRequested || _ctrl.StopRequested) throw new OperationCanceledException();
            }
            if (++_guard > 20_000_000)
                throw new OperationCanceledException("步骤执行数超限，已中止以防死循环。");
        }

        /// <summary>执行 [start, endExclusive) 区间内的步骤；返回跳出后的下一索引。</summary>
        private int ExecBlock(int start, int endExclusive, CancellationToken ct)
        {
            int i = start;
            while (i < endExclusive && i < _steps.Count)
            {
                Thread.Sleep(1);   // 每步让出 CPU，避免密集步骤循环抢占 UI 线程
                AbortCheck(ct);
                var s = _steps[i];
                if (s.Breakpoint) BreakpointGate(ct);   // ★ 断点行：挂起等「继续」（有实际效果的断点）
                var logic = (s.Logic ?? "").Trim();
                _onStep?.Invoke(_index, _flow.Name ?? "", $"第 {i + 1}/{_steps.Count} 步 · {logic}");
                // 行序号写共享态：流程页 300ms 定时器轮询刷新"当前行高亮"（运行器不碰 UI 集合）
                FlowRunStore.SetStep(_flow, $"第 {i + 1}/{_steps.Count} 步 · {logic}");
                FlowRunStore.SetProgress(_flow, stepIndex: i);

                switch (logic)
                {
                    case "循环开始":
                        {
                            int cnt = ParseCount(Sub(s.SetValue));
                            int close = FindMatch(i, "循环开始", "循环结束");
                            int bodyEnd = (close > i) ? close : _steps.Count;
                            for (int r = 0; r < cnt; r++)
                            {
                                AbortCheck(ct);
                                ExecBlock(i + 1, bodyEnd, ct);
                            }
                            i = (close > i) ? close + 1 : _steps.Count;
                            break;
                        }
                    case "循环结束":
                        return i + 1;
                    case "如果":
                    case "否则如果":
                        {
                            // 逻辑行自身的功能也执行（与单步引擎一致）：用户常把变量赋值直接写在
                            // 「如果 / 否则」行上，只当跳转闸口会让该行功能被静默跳过。
                            ExecuteLeaf(s);
                            bool cond = EvalCondition(s);
                            int endIf = FindEnd(i);
                            if (cond)
                            {
                                int els = FindFirstElse(i + 1, endIf);
                                int bodyEnd = (els >= 0) ? els : endIf;
                                ExecBlock(i + 1, bodyEnd, ct);
                                i = endIf + 1;
                            }
                            else
                            {
                                int els = FindFirstElse(i + 1, endIf);
                                i = (els >= 0) ? els : endIf + 1;
                            }
                            break;
                        }
                    case "否则":
                        {
                            // 同上：命中「否则」时，否则行自身的功能（如 变量修改为 7）要先执行
                            ExecuteLeaf(s);
                            int endIf = FindEnd(i);
                            ExecBlock(i + 1, endIf, ct);
                            i = endIf + 1;
                            break;
                        }
                    case "结束":
                        return i + 1;
                    case "注释":
                        i++;
                        break;
                    // 「就 / 并且 / 或者」不再跳过：它们与流程页单步引擎行为保持一致——
                    // 带功能的行（变量/轴/IO…）要真实执行；不带功能的条件连接词走 ExecuteLeaf 空操作。
                    // （旧实现把「就」当纯控制行跳过，导致流程页「运行一次/循环运行」对
                    //   逻辑=就 的变量/设备步骤毫无效果，而单步有效——两套引擎行为分歧。）
                    case "等待":
                    case "延时":
                        {
                            int ms = ParseInt(Sub(s.SetValue), 0);
                            if (ms > 0) SafeSleep(ms, ct);
                            i++;
                            break;
                        }
                    default:
                        ExecuteLeaf(s);
                        i++;
                        break;
                }

                // 可视化节奏：流程页「运行一次 / 循环运行」每步停顿一小段（StepPaceMs=250），
                // 让橙色当前行肉眼可见地逐行移动（否则 5 步几毫秒跑完，高亮一闪而过像"没有"）。
                // 操作员页生产运行 StepPaceMs=0，不拖慢节奏。
                if (_ctrl.StepPaceMs > 0) SafeSleep(_ctrl.StepPaceMs, ct);
            }
            return i;
        }

        private int FindMatch(int openIdx, string open, string close)
        {
            int depth = 0;
            for (int j = openIdx; j < _steps.Count; j++)
            {
                var l = (_steps[j].Logic ?? "").Trim();
                if (l == open) depth++;
                else if (l == close)
                {
                    depth--;
                    if (depth == 0) return j;
                }
            }
            return -1;
        }

        private int FindEnd(int ifIdx)
        {
            int depth = 0;
            for (int j = ifIdx; j < _steps.Count; j++)
            {
                var l = (_steps[j].Logic ?? "").Trim();
                if (l == "如果") depth++;
                else if (l == "结束")
                {
                    depth--;
                    if (depth == 0) return j;
                }
            }
            return _steps.Count;
        }

        private int FindFirstElse(int from, int toExclusive)
        {
            for (int j = from; j < toExclusive && j < _steps.Count; j++)
            {
                var l = (_steps[j].Logic ?? "").Trim();
                if (l == "否则如果" || l == "否则") return j;
            }
            return -1;
        }

        private void ExecuteLeaf(FlowStep s)
        {
            string func = (s.Function ?? "").Trim();
            string name = s.Name ?? "";
            string setv = Sub(s.SetValue);
            try
            {
                switch (func)
                {
                    case "轴":
                        ExecAxis(s, name, setv);
                        break;
                    case "IO":
                    case "IO输出":
                    case "输入IO":
                    case "输出IO":
                        ExecIo(name, setv, s.Operation ?? "");
                        break;
                    case "气缸":
                        ExecCylinder(s, name, setv);
                        break;
                    case "点位":
                        ExecPoint(name, setv);
                        break;
                    case "modbus":
                    case "Modbus":
                        ExecModbus(name, setv);
                        break;
                    case "变量":
                        {
                            // 「实际值」列立即显示运算后的新值（而不是把设置值写进去）
                            string nv = ExecVar(s, name, setv);
                            UiSet(() => { s.ActualValue = nv; s.DurationMs = 1; });
                            break;
                        }
                    case "系统":
                        _bridge?.Log(setv);
                        _log?.Invoke($"[系统] {setv}", LogLevel.Info);
                        break;
                    case "相机":
                        try
                        {
                            int camIdx = 0;
                            if (int.TryParse(name, out var c)) camIdx = c;
                            var bgra = VisionEngine.CaptureFrame(camIdx, out int w, out int h);
                            bool sim = false;
                            if (bgra == null || w <= 0 || h <= 0)
                            {
                                bgra = VisionSimCapture.Capture(camIdx, out w, out h);
                                sim = true;
                            }
                            var det = VisionSimCapture.Detect(camIdx);
                            var camName = name;
                            var cams = ProjectStore.Data?.Cameras;
                            if (cams != null && camIdx >= 0 && camIdx < cams.Count) camName = cams[camIdx].Name;
                            var resKey = $"CamResult{camIdx}";
                            _ctrl.Vars[resKey] = $"{det.X:0.0},{det.Y:0.0}";
                            _ctrl.Touched.Add(resKey);   // 视觉结果只进运行仓，标记脏键后由 WriteBackVars 落表
                            SimRuntime.SetVariable(resKey, det.Score);   // 分数写入数值仓，便于 {CamResult0} 引用
                            SimRuntime.FlashCamera(camName);
                            // 同步相机页「最近结果 / 匹配分数」展示
                            if (cams != null && camIdx >= 0 && camIdx < cams.Count)
                            {
                                cams[camIdx].LastResult = $"中心 ({det.X:0.0},{det.Y:0.0})";
                                cams[camIdx].LastScore = det.Score;
                            }
                            _log?.Invoke($"[相机] {(sim ? "仿真" : "真实")}取帧 {w}x{h}（{camName}），检测中心 ({det.X:0.0},{det.Y:0.0}) 分数 {det.Score:0.00}", LogLevel.Info);
                            _ctrl.OnCameraCapture?.Invoke(bgra, w, h);
                        }
                        catch (Exception ex)
                        {
                            _log?.Invoke($"[相机] 取帧异常：{ex.Message}", LogLevel.Error);
                        }
                        break;
                    case "延时":
                        int ms = ParseInt(setv, 0);
                        if (ms > 0) SafeSleep(ms, CancellationToken.None);
                        break;
                    default:
                        _log?.Invoke($"未识别的功能「{func}」，步骤已跳过。", LogLevel.Warn);
                        break;
                }
                // 非「变量」功能：把设置值先写到「实际值」列作为已下发反馈（1 秒定时器随后用真实读数覆盖）；
                // 「变量」已在 case 内写入运算后的新值，这里不能再覆盖成设置值（曾导致实际值先闪成设置值）。
                if (func != "变量")
                    UiSet(() => { s.ActualValue = setv; s.DurationMs = 1; });
            }
            catch (Exception ex)
            {
                // ★ 暂停/停止抛出的 OperationCanceledException（以及点位防撞的 FlowAbortException）
                // 必须原样上抛：否则「停止」会被这里吞掉、流程继续跑下一步（操作员以为停了其实没停）。
                if (ex is OperationCanceledException || ex is FlowAbortException) throw;
                _log?.Invoke($"步骤执行异常（{func} {name}）：{ex.Message}", LogLevel.Error);
            }
        }

        private void ExecAxis(FlowStep s, string name, string setv)
        {
            var ax = HardwareResolver.ResolveAxis(name);
            if (ax == null) { _log?.Invoke($"找不到轴：{name}", LogLevel.Error); return; }
            string op = (s.Operation ?? "").Trim();
            string prop = (s.Property ?? "").Trim();

            // 属性 = 速度：这是「设速度」而非移动，设置值即速度值（单位/秒）。
            if (prop == "速度")
            {
                if (double.TryParse(setv, out var spd) && spd > 0) _bridge?.SetAxisSpeed(ax, spd);
                else _log?.Invoke($"轴「{name}」速度无法解析：{setv}", LogLevel.Warn);
                return;
            }

            // 回零 / 停止不需要设置值（目标位置）。
            bool needTarget = !(op == "回零" || op == "home" || op == "归零" || op == "停止" || op == "stop");
            double target = 0;
            if (needTarget && !double.TryParse(setv, out target))
            {
                _log?.Invoke($"轴「{name}」目标位置无法解析：{setv}", LogLevel.Error);
                return;
            }

            switch (op)
            {
                case "回零": case "home": case "归零": case "原点":
                    _bridge?.HomeAxis(ax); break;
                case "停止": case "stop":
                    _bridge?.StopAxis(ax); break;
                case "相对移动": case "相对": case "rel": case "相对运动":
                    _bridge?.MoveAxisRel(ax, target); break;
                case "绝对移动": case "修改为": case "绝对定位":
                default:
                    _bridge?.MoveAxisAbs(ax, target); break;
            }
            _bridge?.WaitAxisDone(ax);
        }

        private void ExecIo(string name, string setv, string op)
        {
            var io = HardwareResolver.ResolveOutput(name) ?? HardwareResolver.ResolveInput(name);
            if (io == null) { _log?.Invoke($"找不到 IO：{name}", LogLevel.Error); return; }
            int v;
            if (op == "置位" || op == "set") v = 1;              // 置位：写 1
            else if (op == "复位" || op == "reset") v = 0;       // 复位：写 0
            else v = ParseInt(setv, 0);                          // 其余按设置值写入
            _bridge?.WriteOutput(io, v);
        }

        private void ExecCylinder(FlowStep s, string name, string setv)
        {
            var cy = HardwareResolver.ResolveCylinder(name);
            if (cy == null) { _log?.Invoke($"找不到气缸：{name}", LogLevel.Error); return; }
            string op = (s.Operation ?? "").Trim();
            var sv = (setv ?? "").Trim();
            if (op == "复位" || op == "reset" || op == "归位" || sv == "复位" || sv == "reset" || sv == "归位")
                _bridge?.CylinderReset(cy);
            else if (op == "缩回" || op == "retract" || sv == "0" || sv == "缩回" || sv == "retract")
                _bridge?.CylinderMove(cy, 0);        // 缩回
            else
                _bridge?.CylinderMove(cy, 1);        // 伸出（含旧步骤未指定运算的情况）
            _bridge?.WaitCylinder(cy);
        }

        private void ExecPoint(string name, string setv)
        {
            var pt = HardwareResolver.ResolvePointTable(name);
            if (pt == null) { _log?.Invoke($"找不到点位表：{name}", LogLevel.Error); return; }
            PointItem item = null;
            if (!string.IsNullOrEmpty(setv)) item = pt.Points.FirstOrDefault(p => p.Name == setv);
            if (item == null) item = pt.Points.FirstOrDefault();
            if (item == null) return;

            // 防撞：流程属无人值守场景，条件不满足只写报警列表（绝不弹窗），
            // 并中止整个流程——被拦下的移动说明机台状态与预期不符，继续跑后续步骤有撞机风险
            // （与操作员页自动运行失败即 _stopRequested = true 的处理保持一致）。
            if (!PointConditionGate.EnsureSilent(item, "流程", _flow?.Name))
            {
                // 只中止**本条**流程：不再置共享 _ctrl.StopRequested（那会把同批次其它流程一起停掉）
                throw new FlowAbortException($"点位「{item.Name}」移动条件未满足，已中止本流程。");
            }
            for (int i = 0; i < PointTable.SlotCount; i++)
            {
                var axisName = pt.AxisNames.Count > i ? pt.AxisNames[i] : "";
                if (string.IsNullOrWhiteSpace(axisName)) continue;
                var ax = HardwareResolver.ResolveAxis(axisName);
                if (ax == null) { _log?.Invoke($"找不到轴：{axisName}", LogLevel.Error); continue; }
                var slot = item.Positions.Count > i ? item.Positions[i] : null;
                if (slot == null) continue;
                if (slot.Position == null)
                {
                    _log?.Invoke($"点位「{item.Name}」轴 {pt.AxisNames[i]} 未填位置，已跳过（不移动）。", LogLevel.Info);
                    continue;
                }
                if (slot.Speed > 0) _bridge?.SetAxisSpeed(ax, slot.Speed);
                _bridge?.MoveAxisAbs(ax, slot.Position.Value);
                _bridge?.WaitAxisDone(ax);
            }
        }

        private void ExecModbus(string name, string setv)
        {
            var comm = HardwareResolver.ResolveComm(name);
            if (comm == null) { _log?.Invoke($"找不到通信：{name}", LogLevel.Error); return; }
            _bridge?.CommSend(comm, setv);
        }

        /// <summary>执行变量步骤；返回运算后的新值（供「实际值」列立即显示正确结果，而不是设置值）。</summary>
        private string ExecVar(FlowStep s, string name, string setv)
        {
            if (string.IsNullOrEmpty(name)) return GetVarNum(name).ToString("0.###");
            // 表达式模式：setv 含运算符/变量名 → 按当前变量值实时求值（如 "计数+1"、"A*2"）。
            if (ExpressionEvaluator.IsExpression(setv))
            {
                var ok = ExpressionEvaluator.Evaluate(setv, n => GetVarNum(n), out var r);
                var ev = (ok ? r : 0).ToString("0.###");
                _ctrl.WriteVarLive(name, ev);
                return ev;
            }
            double cur = GetVarNum(name);
            double val = double.TryParse(setv, out var v) ? v : 0;
            string op = (s.Operation ?? "").Trim();
            double res = cur;
            switch (op)
            {
                case "修改为": case "等于": res = val; break;
                case "加": res = cur + val; break;
                case "减": res = cur - val; break;
                case "乘": res = cur * val; break;
                case "除": res = val != 0 ? cur / val : 0; break;
                case "取模": res = val != 0 ? cur % val : 0; break;
                case "取反": res = cur == 0 ? 1 : 0; break;
                default: res = val; break;
            }
            var nv = res.ToString("0.###");
            _ctrl.WriteVarLive(name, nv);
            return nv;
        }

        private double GetVarNum(string name)
        {
            if (_ctrl.Vars.TryGetValue(name, out var v) && double.TryParse(v, out var d)) return d;
            return 0;
        }

        // ---- 辅助 ----

        private string Sub(string t)
        {
            if (string.IsNullOrEmpty(t)) return t;
            return Regex.Replace(t, @"\{([^}]+)\}", m =>
            {
                var key = m.Groups[1].Value.Trim();
                return _ctrl.Vars.TryGetValue(key, out var v) ? v : m.Value;
            });
        }

        private int ParseCount(string t)
        {
            var s = Sub(t);
            var m = Regex.Match(s, @"-?\d+");
            if (!m.Success) return 1;
            if (!int.TryParse(m.Value, out var n) || n <= 0) return 1;
            return Math.Min(n, 100000);
        }

        private int ParseInt(string t, int def) => int.TryParse(Sub(t), out var v) ? v : def;

        private bool EvalCondition(FlowStep s)
        {
            string left = Sub(s.Property), right = Sub(s.SetValue), op = (s.Operation ?? "").Trim();
            bool ln = double.TryParse(left, out var l), rn = double.TryParse(right, out var r);
            switch (op)
            {
                case "等于": case "==": case "是否等于":
                    return ln && rn ? l == r : left == right;
                case "大于": return ln && rn && l > r;
                case "小于": return ln && rn && l < r;
                case "大于等于": return ln && rn && l >= r;
                case "小于等于": return ln && rn && l <= r;
                case "取反": return !Truth(left);
                default: return false;
            }
        }

        private bool Truth(string s) => !string.IsNullOrEmpty(s) && s != "0" && s != "false" && s != "False";

        private void SafeSleep(int ms, CancellationToken ct)
        {
            int remain = ms;
            while (remain > 0)
            {
                int slice = Math.Min(50, remain);
                Thread.Sleep(slice);
                remain -= slice;
                AbortCheck(ct);
            }
        }

        private static void UiSet(Action a)
        {
            // 异步封送到 UI 线程：高亮当前行/变量等不需要同步等待，避免后台流程线程在 Invoke 上被 UI 阻塞（卡顿根因之一）。
            var app = Application.Current;
            if (app?.Dispatcher != null && !app.Dispatcher.CheckAccess())
                app.Dispatcher.BeginInvoke(a);
            else
                a();
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
