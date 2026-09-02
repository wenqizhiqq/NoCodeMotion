// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Vision;
using NoCodeMotion.Views;
using MoonSharp.Interpreter.Debugging;

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

        /// <summary>「相机」步骤真实取帧后回调（byte[]=BGRA, w, h），供 3D 仿真抓拍预览订阅。</summary>
        public Action<byte[], int, int>? OnCameraCapture;

        /// <summary>变量表：名称 -> 值（字符串，数值运算时再解析）。与 ProjectStore.Data.Variables 双向同步。</summary>
        public Dictionary<string, string> Vars = new(StringComparer.OrdinalIgnoreCase);

        public void InitVars()
        {
            Vars.Clear();
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

        /// <summary>把运行期变量写回工程（VariableRow），方便变量页查看。</summary>
        public void WriteBackVars()
        {
            if (ProjectStore.Data?.Variables == null) return;
            foreach (var row in ProjectStore.Data.Variables)
            {
                Set(row.Name1, row, 1); Set(row.Name2, row, 2);
                Set(row.Name3, row, 3); Set(row.Name4, row, 4); Set(row.Name5, row, 5);
            }
        }

        private void Set(string n, VariableRow row, int k)
        {
            if (string.IsNullOrEmpty(n)) return;
            if (Vars.TryGetValue(n, out var v))
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
    public static class LuaRunMonitor
    {
        public static event Action<FlowItem, int> LineChanged;
        public static event Action<FlowItem> RunEnded;
        public static void Report(FlowItem flow, int line) => LineChanged?.Invoke(flow, line);
        public static void ReportEnded(FlowItem flow) => RunEnded?.Invoke(flow);
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
        /// 全部走 Thread，结束由看门狗线程在全部流程 Signal 后触发 onComplete。</summary>
        public static void RunAllAsync(
            FlowRunControl ctrl,
            Action<string, LogLevel> log,
            Action<int, string, string> onStep,
            Action<int, string> onFlowDone,
            Action onComplete,
            CancellationToken ct = default,
            Func<FlowItem, bool>? filter = null)
        {
            var flows = ProjectStore.Data?.Flows?.Where(filter ?? (_ => true)).ToList() ?? new List<FlowItem>();
            if (flows.Count == 0) { onComplete?.Invoke(); return; }
            var done = new CountdownEvent(flows.Count);
            for (int i = 0; i < flows.Count; i++)
            {
                int idx = i;
                var flow = flows[i];
                var th = new Thread(() =>
                {
                    try { RunOneFlow(flow, idx, ctrl, log, onStep, onFlowDone, ct); }
                    catch (OperationCanceledException) { /* 正常中止 */ }
                    catch (Exception ex) { log?.Invoke($"流程「{flow?.Name}」运行异常：{ex.Message}", LogLevel.Error); }
                    finally { done.Signal(); }
                })
                { IsBackground = true, Name = $"Flow-{idx}" };
                th.Start();
            }
            // 看门狗线程：等所有流程结束 → 触发完成回调（回调内部自行入队到 UI 线程执行）。
            var watch = new Thread(() =>
            {
                try { done.Wait(); } catch { }
                onComplete?.Invoke();
            })
            { IsBackground = true, Name = "FlowWatchdog" };
            watch.Start();
        }

        private static void RunOneFlow(FlowItem flow, int index, FlowRunControl ctrl,
            Action<string, LogLevel> log, Action<int, string, string> onStep, Action<int, string> onFlowDone,
            CancellationToken ct)
        {
            if (flow == null) return;
            var name = flow.Name ?? "(未命名流程)";
            if (flow.Kind == FlowKind.Lua)
            {
                RunOneFlowLua(flow, index, ctrl, log, onStep, onFlowDone);
                return;
            }
            if (flow.Kind == FlowKind.Vision)
            {
                RunOneFlowVision(flow, index, ctrl, log, onStep, onFlowDone);
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

            // 主流程（Role=Main）：循环执行——每轮跑完所有步骤后从头再来，直到停止/急停。
            // 复位流程（Role=Reset）：单次执行——归位/复位不应循环，跑完即结束。
            if (flow.Role == FlowRole.Main)
            {
                log?.Invoke($"流程「{name}」开始循环运行（{steps.Count} 步/轮，直到停止/急停）。", LogLevel.Info);
                int cycle = 0;
                try
                {
                    while (!ctrl.StopRequested && !ctrl.EStopRequested)
                    {
                        cycle++;
                        SetStatus(flow, FlowStatus.Running);
                        var exec = new FlowExecutor(flow, index, steps, ctrl, log, onStep);
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

            // 复位流程：单次执行（保留旧行为）
            SetStatus(flow, FlowStatus.Running);
            var exec2 = new FlowExecutor(flow, index, steps, ctrl, log, onStep);
            log?.Invoke($"流程「{name}」开始运行（{steps.Count} 步，复位流程单次执行）。", LogLevel.Info);
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

        /// <summary>Lua 脚本流程：优先复用 Lua 编辑器页面自身的运行（RunFlow，用户要求“lua 直接走编辑器页面运行”，
        /// 运行行/断点/单步与页面完全一致）；编辑器未就绪或被占用时退化为独立 LuaDebugSession 连续运行，
        /// 并把当前执行行广播给 LuaRunMonitor 供编辑器实时跳行高亮。暂停/停止映射到会话的 RequestPause/Resume/Stop。</summary>
        private static void RunOneFlowLua(FlowItem flow, int index, FlowRunControl ctrl,
            Action<string, LogLevel> log, Action<int, string, string> onStep, Action<int, string> onFlowDone)
        {
            var name = flow.Name ?? "(未命名流程)";
            SetStatus(flow, FlowStatus.Running);
            // Lua Main 循环：每轮跑完脚本后判断 Ended.IsError，一旦脚本报错就停止循环并把状态置 Exception，
            // 避免早前实现的「错误也立即重启」造成的 LuaScriptThread/LuaWatch 死循环刷屏与 UI 卡顿。
            try
            {
            var editor = LuaEditorView.Active;
            if (editor != null)
            {
                while (!ctrl.StopRequested && !ctrl.EStopRequested)
                {
                    Thread.Sleep(1);   // 让出 CPU，避免紧密循环抢占 UI 线程
                    LuaDebugSession session = null;
                    ExecutionEndedInfo lastEnded = null;
                    var editorReady = new ManualResetEventSlim(false);
                    try
                    {
                        // 用 BeginInvoke 异步把 RunFlow 派发到 UI 线程，避免后台线程被 Dispatcher.Invoke 同步阻塞；
                        // 派发完成后置 editorReady，后台线程再继续（不碰 UI）。
                        editor.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                session = editor.RunFlow(flow, false);
                                if (session != null)
                                {
                                    session.Log += (m, k) => log?.Invoke($"[Lua:{name}] {m}", LogLevel.Info);
                                    session.LineStepped += line =>
                                    {
                                        onStep?.Invoke(index, name, $"Lua 行 {line}");
                                        FlowRunStore.SetStep(flow, $"Lua 行 {line}");
                                    };
                                    Action<ExecutionEndedInfo> onEnded = null;
                                    onEnded = info => { lastEnded = info; };
                                    session.Ended += onEnded;
                                }
                            }
                            finally { editorReady.Set(); }
                        }));
                        if (!editorReady.Wait(2000)) { Thread.Sleep(200); continue; }   // 等待 UI 线程完成启动（超时则重试）
                    }
                    catch (Exception ex) { editorReady.Set(); log?.Invoke($"流程「{name}」Lua 启动异常：{ex.Message}", LogLevel.Error); Thread.Sleep(200); continue; }
                    if (session == null) { Thread.Sleep(200); continue; } // 编辑器正忙（用户手动调试），稍后重试
                    log?.Invoke($"流程「{name}」开始连续运行（复用 Lua 编辑器页面运行，直到停止）。", LogLevel.Info);
                    while (session.IsBusy && !ctrl.EStopRequested && !ctrl.StopRequested)
                    {
                        Thread.Sleep(40);
                        if (ctrl.EStopRequested || ctrl.StopRequested) { session.Stop(); break; }
                        if (ctrl.PauseRequested)
                        {
                            SetStatus(flow, FlowStatus.Paused);
                            session.RequestPause();
                            while (ctrl.PauseRequested && session.IsBusy) Thread.Sleep(40);
                            if (session.IsBusy && !ctrl.PauseRequested)
                            {
                                session.Resume(DebuggerAction.ActionType.Run);
                                SetStatus(flow, FlowStatus.Running);
                            }
                        }
                    }
                    LuaRunMonitor.ReportEnded(flow);
                    if (ctrl.EStopRequested) { log?.Invoke($"流程「{name}」已急停。", LogLevel.Warn); break; }
                    if (ctrl.StopRequested) { log?.Invoke($"流程「{name}」已停止。", LogLevel.Warn); break; }
                    // 脚本以错误结束 → 跳出循环，状态置 Exception，不再重试（早前实现刷屏根因）
                    if (lastEnded != null && lastEnded.IsError)
                    {
                        SetStatus(flow, FlowStatus.Exception);
                        log?.Invoke($"流程「{name}」脚本报错（行 {lastEnded.ErrorLine}）：{lastEnded.Message} — 已停止重试，请修正脚本后重新启动。", LogLevel.Error);
                        break;
                    }
                    // 正常结束一轮 → 节流后再起下一轮，避免脚本短到几毫秒时紧贴循环独占 CPU。
                    Thread.Sleep(200);
                }
                onFlowDone?.Invoke(index, name);
                return;
            }

            // 退化路径：Lua 编辑器页面未加载，用独立会话连续运行并广播当前行。
            while (!ctrl.StopRequested && !ctrl.EStopRequested)
            {
                Thread.Sleep(1);   // 让出 CPU，避免紧密循环抢占 UI 线程
                var ended = new ManualResetEventSlim(false);
                ExecutionEndedInfo lastEnded = null;
                try
                {
                    var session = new LuaDebugSession();
                    session.Log += (m, k) => log?.Invoke($"[Lua:{name}] {m}", LogLevel.Info);
                    session.LineStepped += line =>
                    {
                        LuaRunMonitor.Report(flow, line);
                        onStep?.Invoke(index, name, $"Lua 行 {line}");
                        FlowRunStore.SetStep(flow, $"Lua 行 {line}");
                    };
                    session.Ended += info =>
                    {
                        lastEnded = info;
                        if (info.IsError) log?.Invoke($"[Lua:{name}] 运行错误（行 {info.ErrorLine}）：{info.Message}", LogLevel.Error);
                        ended.Set();
                    };
                    log?.Invoke($"流程「{name}」开始连续运行（Lua，直到停止）。", LogLevel.Info);
                    session.Start(flow.LuaSource ?? "", false);
                    var watcher = new Thread(() =>
                    {
                        while (!ended.Wait(40))
                        {
                            if (ctrl.EStopRequested || ctrl.StopRequested) { session?.Stop(); break; }
                            if (ctrl.PauseRequested)
                            {
                                SetStatus(flow, FlowStatus.Paused);
                                session?.RequestPause();
                                while (ctrl.PauseRequested && !ended.Wait(40)) { }
                                if (!ended.IsSet && !ctrl.PauseRequested)
                                {
                                    session?.Resume(DebuggerAction.ActionType.Run);
                                    SetStatus(flow, FlowStatus.Running);
                                }
                            }
                        }
                    }) { IsBackground = true, Name = $"LuaWatch-{index}" };
                    watcher.Start();
                    ended.Wait();
                }
                catch (Exception ex) { log?.Invoke($"流程「{name}」Lua 运行异常：{ex.Message}", LogLevel.Error); }
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
                Thread.Sleep(200);
            }
            onFlowDone?.Invoke(index, name);
            }
            finally
            {
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
            Action<string, LogLevel> log, Action<int, string, string> onStep, Action<int, string> onFlowDone)
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

            // 主流程（Role=Main）：循环运行——每轮跑完整条视觉流程后从头再来，直到停止 / 急停。
            if (flow.Role == FlowRole.Main)
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
                            SetStatus(flow, FlowStatus.Running);
                        }
                        cycle++;
                        SetStatus(flow, FlowStatus.Running);
                        try
                        {
                            var report = VisionEngine.Run(steps, progress);
                            bool anyFail = report.Results.Any(r => !r.Ok);
                            log?.Invoke($"视觉流程「{name}」第 {cycle} 轮完成（{(anyFail ? "有失败步骤" : "全部通过")}，{report.Results.Count} 步）。", LogLevel.Info);
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

            // 复位流程：单次执行（跑完即结束，不循环）
            SetStatus(flow, FlowStatus.Running);
            log?.Invoke($"视觉流程「{name}」开始运行（{steps.Count} 步，复位流程单次执行）。", LogLevel.Info);
            try
            {
                var report = VisionEngine.Run(steps, progress);
                SetStatus(flow, FlowStatus.Idle);
                log?.Invoke($"视觉流程「{name}」运行结束。", LogLevel.Info);
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
    internal class FlowExecutor
    {
        private readonly FlowItem _flow;
        private readonly int _index;
        private readonly List<FlowStep> _steps;
        private readonly FlowRunControl _ctrl;
        private readonly Action<string, LogLevel> _log;
        private readonly Action<int, string, string> _onStep;
        private readonly IHardwareBridge _bridge;
        private long _guard;
        private FlowStep _lastCurrent;
        private bool _pauseActive;     // 当前流程是否处于暂停状态（控制列表右侧"暂"芯片切换）

        public FlowExecutor(FlowItem flow, int index, List<FlowStep> steps, FlowRunControl ctrl,
            Action<string, LogLevel> log, Action<int, string, string> onStep)
        {
            _flow = flow; _index = index; _steps = steps; _ctrl = ctrl; _log = log; _onStep = onStep;
            _bridge = HardwareBridge.Current;
        }

        public void Run(CancellationToken ct) => ExecBlock(0, _steps.Count, ct);

        /// <summary>运行结束/中止后清除当前行高亮。</summary>
        public void ClearCurrent()
        {
            UiSet(() =>
            {
                if (_lastCurrent != null) _lastCurrent.IsCurrent = false;
                _lastCurrent = null;
            });
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
                    // FlowExecutor 在自身线程内调自己的 UiSet（FlowExecutor.UiSet，跨类不可见）
                    UiSet(() => _flow.Status = FlowStatus.Paused);
                }
                _ctrl.ResumeEvent?.Wait(ct);
                if (_pauseActive)
                {
                    _pauseActive = false;
                    UiSet(() => _flow.Status = FlowStatus.Running);
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
                var logic = (s.Logic ?? "").Trim();
                _onStep?.Invoke(_index, _flow.Name ?? "", $"第 {i + 1}/{_steps.Count} 步 · {logic}");
                FlowRunStore.SetStep(_flow, $"第 {i + 1}/{_steps.Count} 步 · {logic}");
                UiSet(() =>
                {
                    if (_lastCurrent != null && !ReferenceEquals(_lastCurrent, s)) _lastCurrent.IsCurrent = false;
                    s.IsCurrent = true;
                    _lastCurrent = s;
                });

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
                            int endIf = FindEnd(i);
                            ExecBlock(i + 1, endIf, ct);
                            i = endIf + 1;
                            break;
                        }
                    case "结束":
                        return i + 1;
                    case "注释":
                    case "就":
                    case "并且":
                    case "或者":
                        i++;
                        break;
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
                        ExecIo(name, setv);
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
                        ExecVar(s, name, setv);
                        break;
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
                            if (bgra != null && w > 0 && h > 0)
                            {
                                _log?.Invoke($"[相机] 已取帧 {w}x{h}（相机步骤「{name}」）。", LogLevel.Info);
                                _ctrl.OnCameraCapture?.Invoke(bgra, w, h);
                            }
                            else
                                _log?.Invoke($"[相机] 取帧失败：{name}", LogLevel.Warn);
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
                UiSet(() => { s.ActualValue = setv; s.DurationMs = 1; });
            }
            catch (Exception ex)
            {
                _log?.Invoke($"步骤执行异常（{func} {name}）：{ex.Message}", LogLevel.Error);
            }
        }

        private void ExecAxis(FlowStep s, string name, string setv)
        {
            var ax = HardwareResolver.ResolveAxis(name);
            if (ax == null) { _log?.Invoke($"找不到轴：{name}", LogLevel.Error); return; }
            if (!double.TryParse(setv, out var target)) { _log?.Invoke($"轴「{name}」目标位置无法解析：{setv}", LogLevel.Error); return; }
            string op = (s.Operation ?? "").Trim();
            if (double.TryParse(Sub(s.Property), out var sp) && sp > 0) _bridge?.SetAxisSpeed(ax, sp);
            if (op == "回零" || op == "home" || op == "归零") _bridge?.HomeAxis(ax);
            else if (op == "停止" || op == "stop") _bridge?.StopAxis(ax);
            else if (op == "相对" || op == "rel" || op == "相对运动") _bridge?.MoveAxisRel(ax, target);
            else _bridge?.MoveAxisAbs(ax, target);
            _bridge?.WaitAxisDone(ax);
        }

        private void ExecIo(string name, string setv)
        {
            var io = HardwareResolver.ResolveOutput(name) ?? HardwareResolver.ResolveInput(name);
            if (io == null) { _log?.Invoke($"找不到 IO：{name}", LogLevel.Error); return; }
            int v = ParseInt(setv, 0);
            _bridge?.WriteOutput(io, v);
        }

        private void ExecCylinder(FlowStep s, string name, string setv)
        {
            var cy = HardwareResolver.ResolveCylinder(name);
            if (cy == null) { _log?.Invoke($"找不到气缸：{name}", LogLevel.Error); return; }
            var sv = (setv ?? "").Trim();
            if (sv == "复位" || sv == "reset" || sv == "归位")
                _bridge?.CylinderReset(cy);
            else
            {
                int state = (sv == "0" || sv == "缩回" || sv == "retract") ? 0 : 1;
                _bridge?.CylinderMove(cy, state);
            }
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
            for (int i = 0; i < PointTable.SlotCount; i++)
            {
                var axisName = pt.AxisNames.Count > i ? pt.AxisNames[i] : "";
                if (string.IsNullOrWhiteSpace(axisName)) continue;
                var ax = HardwareResolver.ResolveAxis(axisName);
                if (ax == null) { _log?.Invoke($"找不到轴：{axisName}", LogLevel.Error); continue; }
                var slot = item.Positions.Count > i ? item.Positions[i] : null;
                if (slot == null) continue;
                if (slot.Speed > 0) _bridge?.SetAxisSpeed(ax, slot.Speed);
                _bridge?.MoveAxisAbs(ax, slot.Position);
                _bridge?.WaitAxisDone(ax);
            }
        }

        private void ExecModbus(string name, string setv)
        {
            var comm = HardwareResolver.ResolveComm(name);
            if (comm == null) { _log?.Invoke($"找不到通信：{name}", LogLevel.Error); return; }
            _bridge?.CommSend(comm, setv);
        }

        private void ExecVar(FlowStep s, string name, string setv)
        {
            if (string.IsNullOrEmpty(name)) return;
            double cur = GetVarNum(name);
            double val = double.TryParse(setv, out var v) ? v : 0;
            string op = (s.Operation ?? "").Trim();
            double res = cur;
            switch (op)
            {
                case "修改": case "等于": res = val; break;
                case "加": res = cur + val; break;
                case "减": res = cur - val; break;
                case "乘": res = cur * val; break;
                case "除": res = val != 0 ? cur / val : 0; break;
                case "取模": res = val != 0 ? cur % val : 0; break;
                case "取反": res = cur == 0 ? 1 : 0; break;
                default: res = val; break;
            }
            _ctrl.Vars[name] = res.ToString();
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
