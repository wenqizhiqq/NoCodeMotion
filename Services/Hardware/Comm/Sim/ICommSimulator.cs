// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁣志​◆‎编‌写⁠◇‍微⁠信‍﹕‎1⁣8​7‍◆‌1⁣9‍3‍6‏◇‌1‍3‌9⁣9⁣　‏※⁣保‍留⁠所‎有​权​利‎请⁣勿⁠删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Generic;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Hardware.Comm.Sim
{
    /// <summary>
    /// 一种通讯类型的「对端仿真器」。
    /// <para>两种做法，面板上都用同一套控件驱动：</para>
    /// <list type="number">
    ///   <item><description><b>真对端</b>（管道 / 共享内存 / 共享文件夹 / 本机文件 / 网口TCP / UDP / ModbusTCP / Web / SECS）：
    ///   在本机真的起一个「对方」（监听、从站、mock 服务、反向通道…），本端照常走真实通道连它 ——
    ///   收发、超时、断线全部是真的，只是另一端换成了软件自己。</description></item>
    ///   <item><description><b>回环接管</b>（串口 / ModbusRTU / GPIB / 西门子S7 / 三菱MC）：
    ///   这几类要么需要物理线 / 未内置协议，本机没有「另一端」可起。于是仿真器改成接管本端通道：
    ///   本端发送的内容记进日志并按应答规则产生「收到的数据」，不碰任何硬件。</description></item>
    /// </list>
    /// </summary>
    public interface ICommSimulator : IDisposable
    {
        /// <summary>面板标题里那一句：本端将连到哪里、对端在仿什么（如「对端 = 本机命名管道服务端」）。</summary>
        string Endpoint { get; }

        /// <summary>这个仿真器和真设备的差别 / 注意事项（面板底部灰字）。</summary>
        string Note { get; }

        /// <summary>是不是「回环接管本端通道」型（true 时本端不再碰硬件）。</summary>
        bool TakesOverLocalChannel { get; }

        bool IsRunning { get; }

        /// <summary>收发与状态日志（面板记录区、Lua 输出面板共用一个出口）。</summary>
        event Action<string> LineLogged;

        /// <summary>启动对端（失败抛中文 IOException，不静默）。</summary>
        void Start();

        /// <summary>停止对端。</summary>
        void Stop();

        /// <summary>手工把一段内容当成「对方发来的」注入给本端（返回 false = 当前没有可注入的链路）。</summary>
        bool Inject(string text);

        /// <summary>本端发送过来时按规则算应答（回环型由通道调用；对端型由自己的收发循环调用）。</summary>
        string ReplyFor(string received);
    }

    /// <summary>
    /// 仿真器公共部分：应答模式（回显 / 按规则 / 静默）、规则解析、日志出口。
    /// <para>规则写成「一行一条」存在 <see cref="CommItem.SimRules"/> 里（单个文本列，xlsx 反射直接落盘）：
    /// <code>
    /// # 井号开头是注释
    /// 触发条件=&gt;应答内容        # 精确匹配（忽略首尾空白）
    /// *触发条件=&gt;应答内容       # 前缀 * 表示「包含」
    /// =&gt;应答内容                # 条件留空 = 兜底（谁都没命中时回这个）
    /// </code>
    /// 应答里可以写 <c>{msg}</c> 引用收到的原文，<c>\r \n \t</c> 与通道里的转义一致。</para>
    /// </summary>
    public abstract class CommSimBase : ICommSimulator
    {
        private readonly List<(string Match, string Reply, bool Contains)> _rules =
            new List<(string, string, bool)>();
        private int _ruleStamp = -1;

        protected readonly CommItem Cfg;
        protected readonly object Gate = new object();
        private bool _running;

        protected CommSimBase(CommItem cfg) { Cfg = cfg; }

        public abstract string Endpoint { get; }
        public abstract string Note { get; }
        public virtual bool TakesOverLocalChannel => false;

        public event Action<string> LineLogged;

        public bool IsRunning { get { lock (Gate) return _running; } }

        protected void SetRunning(bool value) { lock (Gate) _running = value; }

        protected void Log(string line) => LineLogged?.Invoke(line);

        public abstract void Start();
        public abstract void Stop();
        public abstract bool Inject(string text);

        /// <summary>应答模式：回显 / 按规则 / 静默（读 <see cref="CommItem.SimMode"/>）。</summary>
        protected string Mode => (Cfg.SimMode ?? "回显").Trim();

        /// <summary>
        /// 按模式与规则算出该回什么：静默回空串；回显原样返回；按规则取第一条命中的应答（支持兜底）。
        /// </summary>
        public virtual string ReplyFor(string received)
        {
            string mode = Mode;
            if (mode.StartsWith("静默", StringComparison.Ordinal)) return string.Empty;
            if (mode.StartsWith("回显", StringComparison.Ordinal)) return received ?? string.Empty;

            var rules = ParsedRules();
            string got = (received ?? string.Empty).Trim();
            string fallback = null;
            foreach (var r in rules)
            {
                // 条件留空 = 兜底行。先记下来、等具体规则全不落再说，
                // 否则用户把兜底写在第一行时，后面所有规则都会被它抢答。
                if (r.Match.Length == 0) { if (fallback == null) fallback = r.Reply; continue; }

                bool hit = r.Contains ? got.IndexOf(r.Match, StringComparison.OrdinalIgnoreCase) >= 0
                                      : string.Equals(r.Match, got, StringComparison.OrdinalIgnoreCase);
                if (hit) return Expand(r.Reply, received);
            }
            return fallback == null ? string.Empty : Expand(fallback, received);
        }

        /// <summary>把 <c>{msg}</c> 换成收到的原文，并处理 \r \n \t 转义。</summary>
        protected static string Expand(string reply, string received)
        {
            if (string.IsNullOrEmpty(reply)) return string.Empty;
            string s = reply.Replace("{msg}", received ?? string.Empty, StringComparison.Ordinal);
            return PayloadCodec.Unescape(s);
        }

        /// <summary>解析规则文本（内容没变时复用上一次结果，面板每轮刷新不必重算）。</summary>
        protected List<(string Match, string Reply, bool Contains)> ParsedRules()
        {
            string raw = Cfg.SimRules ?? string.Empty;
            int stamp = raw.Length ^ (raw.Length > 0 ? raw[0] : 0) ^ (raw.Length > 1 ? raw[raw.Length - 1] : 0);
            lock (_rules)
            {
                if (stamp != _ruleStamp)
                {
                    _rules.Clear();
                    foreach (var line in raw.Split('\n'))
                    {
                        string t = line.Trim();
                        if (t.Length == 0 || t.StartsWith("#") || t.StartsWith("//")) continue;
                        int arrow = t.IndexOf("=>", StringComparison.Ordinal);
                        if (arrow < 0) arrow = t.IndexOf('→');
                        if (arrow < 0) continue;                      // 没写箭头的行忽略（避免半条规则误命中）
                        string m = t.Substring(0, arrow).Trim();
                        string r = t.Substring(arrow + 2).Trim();
                        bool contains = m.StartsWith("*");
                        if (contains) m = m.Substring(1).Trim();
                        _rules.Add((m, r, contains));
                    }
                    _ruleStamp = stamp;
                }
                return _rules;
            }
        }

        /// <summary>规则条数（面板显示「已配 N 条规则」）。</summary>
        protected int RuleCount
        {
            get { lock (_rules) return ParsedRules().Count; }
        }

        public virtual void Dispose()
        {
            try { Stop(); } catch { }
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
