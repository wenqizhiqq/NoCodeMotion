// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁣志​◆‎编‌写⁠◇‍微⁠信‍﹕‎1⁣8​7‍◆‌1⁣9‍3‍6‏◇‌1‍3‌9⁣9⁣　‏※⁣保‍留⁠所‎有​权​利‎请⁣勿⁠删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 进程计时精度。
    /// <para>Windows 默认时间片约 15.6ms：<c>Thread.Sleep(1)</c> 实际睡 ~15ms（本机实测 50 次 = 771ms），
    /// 等待循环（等轴到位 / 等 IO / 延时）会被整体拖慢，「到位延时 50ms」也会变成 62ms。
    /// 启动时把进程计时器提到 1ms（同样 50 次实测 97ms），退出时还原。</para>
    /// <para>只影响本进程，不改系统全局设置；代价是空闲时唤醒次数略多，工控机可以接受。</para>
    /// </summary>
    public static class Timing
    {
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint milliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint milliseconds);

        static bool _raised;

        /// <summary>把进程计时精度提到 milliseconds（默认 1）。重复调用安全。</summary>
        public static void Raise(int milliseconds = 1)
        {
            if (_raised) return;
            if (TimeBeginPeriod((uint)Math.Max(1, milliseconds)) == 0) _raised = true;
        }

        /// <summary>退出时还原（不还原会让整机的省电策略跟着受影响）。</summary>
        public static void Release()
        {
            if (!_raised) return;
            TimeEndPeriod(1);
            _raised = false;
        }

        /// <summary>
        /// 比 <c>Thread.Sleep</c> 准的等待：先粗睡到只剩约 1ms，再用自旋收尾（只烧最后 ≤1ms，别空转整段）。
        /// 适合等待循环里的 ≤50ms 小片；很长的等待仍应直接用 Thread.Sleep。
        /// </summary>
        public static void Sleep(int milliseconds)
        {
            if (milliseconds <= 0) return;
            var sw = Stopwatch.StartNew();
            if (milliseconds > 2) Thread.Sleep(milliseconds - 1);
            while (sw.ElapsedMilliseconds < milliseconds) Thread.SpinWait(200);
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
