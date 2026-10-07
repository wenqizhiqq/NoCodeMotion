// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启‍志‍◆⁠编‎写​◇‍微‍信‏﹕‍1‎8‌7‏◆⁠1​9‏3‌6​◇‎1‎3‌9⁠9‍　‎※⁣保‏留‌所‌有‏权​利‍请‏勿⁠删‌除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using MoonSharp.Interpreter;
namespace NoCodeMotion.Services
{
    /// <summary>
    /// 硬件对接层解析器（单例入口）。
    ///
    /// 框架运行时从这里取当前生效的 <see cref="IHardwareBridge"/>。默认是
    /// <see cref="StubHardwareBridge"/>（无硬件也能跑）。接入真实设备时，在程序启动处
    /// 把 <see cref="Current"/> 换成你自己的实现即可：
    ///
    /// <code>
    /// HardwareBridge.Current = new MyMotionCardBridge();   // 实现 IHardwareBridge
    /// </code>
    ///
    /// 该属性线程安全（volatile 读），Lua 脚本执行线程会在每次运行时读取一次。
    /// </summary>
    public static class HardwareBridge
    {
        /// <summary>当前生效的硬件对接实现。默认无硬件桩。</summary>
        public static volatile IHardwareBridge Current = new StubHardwareBridge();

        /// <summary>
        /// ★ 全局「等待闸」：所有硬件阻塞等待（轴到位 / 气缸到位 / 等待输入 / 回零到位）在轮询时都会调它。
        /// <para>流程/循环运行时由 <see cref="BindWaitGuard"/> 绑定到该次运行的 FlowRunControl：
        /// 操作员按「暂停」→ 闸内 while 等「继续」；按「停止 / 急停」→ 闸内抛异常，等待立刻跳出。</para>
        /// <para>为 null = 不干预（手动 Jog、手动点位等非流程场景）。</para>
        /// </summary>
        public static volatile Action WaitGuard;

        /// <summary>
        /// 把等待闸绑定到一次流程运行的暂停/停止状态源。
        /// <para>参数用 object 而不是 FlowRunControl：FlowRunControl 定义在 NoCodeMotion.ViewModels，
        /// 而本文件在 Services —— 反射取字段可以避免 Services 反向依赖 ViewModels 造成循环依赖。</para>
        /// <para>传 null = 解绑（恢复「不干预」）。返回是否绑定成功（反射拿不到字段时为 false）。</para>
        /// </summary>
        public static bool BindWaitGuard(object ctrl)
        {
            var g = BuildWaitGuard(ctrl);
            WaitGuard = g;
            return g != null;
        }

        /// <summary>
        /// 由暂停/停止状态源构造等待闸：
        ///   - StopRequested / EStopRequested → 抛 ScriptRuntimeException（跳出等待）；
        ///   - PauseRequested → 置 Paused 状态（可选回调）→ ResumeEvent.Wait() 阻塞 → 恢复后重查停止。
        /// 反射字段：StopRequested / EStopRequested / PauseRequested / ResumeEvent / ResumeTick；
        /// 可选字段 OnWaitPaused / OnWaitResumed（Action，用于把「等待中被暂停」反映到界面状态）。
        /// </summary>
        private static Action BuildWaitGuard(object ctrl)
        {
            if (ctrl == null) return null;
            var t = ctrl.GetType();
            var fStop = t.GetField("StopRequested");
            var fEStop = t.GetField("EStopRequested");
            var fPause = t.GetField("PauseRequested");
            var fResume = t.GetField("ResumeEvent");
            if (fStop == null || fPause == null || fResume == null) return null;   // 认不出就不再拦（保守）
            var fOnPaused = t.GetField("OnWaitPaused");
            var fOnResumed = t.GetField("OnWaitResumed");

            return () =>
            {
                if (fEStop != null && fEStop.GetValue(ctrl) is bool es && es)
                    throw new OperationCanceledException("已急停：等待被中断。");
                if (fStop.GetValue(ctrl) is bool s && s)
                    throw new OperationCanceledException("已停止：等待被中断。");
                if (fPause.GetValue(ctrl) is bool pr && pr)
                {
                    // 等待中被暂停：先让界面能看到「暂停」（可选），再阻塞到「继续」
                    if (fOnPaused?.GetValue(ctrl) is Action onPaused) { try { onPaused(); } catch { } }
                    var ev = fResume.GetValue(ctrl) as System.Threading.WaitHandle;
                    if (ev != null) { try { ev.WaitOne(); } catch { } }
                    if (fOnResumed?.GetValue(ctrl) is Action onResumed) { try { onResumed(); } catch { } }
                    if (fEStop != null && fEStop.GetValue(ctrl) is bool es2 && es2)
                        throw new OperationCanceledException("已急停：等待被中断。");
                    if (fStop.GetValue(ctrl) is bool s2 && s2)
                        throw new OperationCanceledException("已停止：等待被中断。");
                }
            };
        }

        /// <summary>替换为自定义对接实现（如运动控制卡 / PLC 驱动）。</summary>
        public static void SetBridge(IHardwareBridge bridge) =>
            Current = bridge ?? new StubHardwareBridge();
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
