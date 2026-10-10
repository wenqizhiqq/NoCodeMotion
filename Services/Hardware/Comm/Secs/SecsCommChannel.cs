// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁣志​◆‎编‌写⁠◇‍微⁠信‍﹕‎1⁣8​7‍◆‌1⁣9‍3‍6‏◇‌1‍3‌9⁣9⁣　‏※⁣保‍留⁠所‎有​权​利‎请⁣勿⁠删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using NoCodeMotion.Models;
using NoCodeMotion.Services.Hardware.Comm.Secs;

namespace NoCodeMotion.Services.Hardware.Comm
{
    /// <summary>
    /// 真实 SECS/HSMS 通道（SEMI E37 会话 + SEMI E5 报文内容）。
    ///
    /// 配置映射（<see cref="CommItem"/>，CommType = <c>SECS(HSMS)</c> 时走这里）：
    ///   SecsRole   = 被动（设备端：监听本机端口，等 Host 连入）／ 主动（主机端：连对方 IP）
    ///   PortOrIp   = 主动模式：对方 IP；被动模式：忽略
    ///   BaudOrPort = 端口（HSMS 标准 5000；被动模式填 0 表示由系统分配一个空闲端口）
    ///   SecsT3/T5/T6/T7/T8 / SecsDeviceId / SecsAutoReply / SecsMdln / SecsSoftRev 见 CommItem
    ///
    /// 文本侧统一用 SML（SECS Message Language），与 Lua 的 CommSend / CommRecv 共用：
    /// <code>
    /// -- 问一句「在线吗」，打印对方回的机型
    /// CommSend("SECS1", "S1F1 W")
    /// local s = CommRecv("SECS1")     -- 例如 "S1F2 <L <A NoCodeMotion> <A 1.0.0>>"
    ///
    /// -- 主动上报一条事件 S6F11（L 数据项）
    /// CommSend("SECS1", "S6F11 W <L <U4 1> <U4 1001> <A LOT01>>")
    ///
    /// -- 控制消息（一般不用手发：Select 由 Open/会话层自动处理）
    /// CommSend("SECS1", "LINKTEST")
    /// </code>
    ///
    /// ★ W-bit 报文的应答会被会话层按 SystemBytes 直接配对吃掉（不会进收件队列），
    ///   所以本通道额外提供 <see cref="SendAndReply"/>，把应答文本交回调用方（页面日志 / Lua）。
    /// </summary>
    public sealed class SecsCommChannel : ICommChannel
    {
        private readonly CommItem _cfg;
        private readonly HsmsSession _session;
        private readonly object _gate = new object();

        public SecsCommChannel(CommItem cfg)
        {
            _cfg = cfg;
            _session = new HsmsSession
            {
                Active = IsActive(cfg),
                Host = (cfg.PortOrIp ?? string.Empty).Trim(),
                Port = cfg.BaudOrPort > 0 ? cfg.BaudOrPort : 5000,
                DeviceId = (ushort)Clamp(cfg.SecsDeviceId),
                T3Ms = Positive(cfg.SecsT3Ms, 45000),
                T5Ms = Positive(cfg.SecsT5Ms, 10000),
                T6Ms = Positive(cfg.SecsT6Ms, 5000),
                T7Ms = Positive(cfg.SecsT7Ms, 10000),
                T8Ms = Positive(cfg.SecsT8Ms, 5000),
                AutoReply = cfg.SecsAutoReply,
                Mdln = string.IsNullOrWhiteSpace(cfg.SecsMdln) ? "NoCodeMotion" : cfg.SecsMdln.Trim(),
                SoftRev = string.IsNullOrWhiteSpace(cfg.SecsSoftRev) ? "1.0.0" : cfg.SecsSoftRev.Trim(),
            };
        }

        public string Name => _cfg.Name;

        /// <summary>是否可用：主动模式 = TCP 已连；被动模式 = 正在监听（端口已绑定）。</summary>
        public bool IsOpen => _session.IsConnected || _session.IsListening;

        /// <summary>HSMS 会话是否已 Select（页面显示「已选中」）。</summary>
        public bool IsSelected => _session.IsSelected;

        /// <summary>是否有 TCP 连接（被动模式监听期间为 false）。</summary>
        public bool IsConnected => _session.IsConnected;

        /// <summary>被动模式是否正在监听。</summary>
        public bool IsListening => _session.IsListening;

        /// <summary>最近一次错误 / 断开原因。</summary>
        public string LastError => _session.LastError;

        /// <summary>被动模式实际监听的端口（BaudOrPort 填 0 时由系统分配，页面显示它）。</summary>
        public int BoundPort => _session.BoundPort;

        /// <summary>底层 HSMS 会话（页面探活 / 统计用）。</summary>
        public HsmsSession Session => _session;

        /// <summary>日志回调（CommManager 会接到页面日志 / Lua 输出面板）。</summary>
        public Action<string> Log
        {
            get => _session.Log;
            set => _session.Log = value;
        }

        public void Open()
        {
            lock (_gate)
            {
                if (_session.IsConnected) return;
                if (_session.IsListening) return;
                _session.Open();
            }
        }

        public void Send(string data)
        {
            SendAndReply(data);
        }

        /// <summary>
        /// 发送一条 SML 报文，返回「立即应答」的单行 SML；
        /// 不需要应答（非 W-bit）或等待超时返回 null。解析/发送失败抛中文异常。
        /// </summary>
        public string SendAndReply(string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return null;
            Open();

            var msg = SecsMessage.ParseSml(data.Trim(), (ushort)Clamp(_cfg.SecsDeviceId), out string perr);
            if (msg == null)
                throw new InvalidOperationException(
                    "SECS 报文解析失败：" + perr
                    + "。可用写法：S1F1 W ／ S6F11 W <L <U4 1> <A LOT01>> ／ SELECT ／ DESELECT ／ LINKTEST ／ SEPARATE");

            var reply = _session.Send(msg, out string serr);
            if (serr != null) throw new InvalidOperationException("SECS 发送失败：" + serr);
            return reply == null ? null : reply.ToSmlLine();
        }

        public string Recv()
        {
            Open();
            var m = _session.Recv(Positive(_cfg.TimeoutMs, 1000));
            return m == null ? string.Empty : m.ToSmlLine();
        }

        /// <summary>主动发 Select.req 建立会话（页面「建立会话」按钮）。</summary>
        public bool Select()
        {
            Open();
            return _session.Select();
        }

        /// <summary>发 Linktest.req 探活。</summary>
        public bool Linktest()
        {
            Open();
            return _session.Linktest();
        }

        public void Dispose()
        {
            lock (_gate) _session.Dispose();
        }

        private static bool IsActive(CommItem cfg) =>
            (cfg.SecsRole ?? string.Empty).IndexOf("主动", StringComparison.Ordinal) >= 0;

        private static int Clamp(int deviceId) =>
            deviceId < 0 ? 0 : (deviceId > 0x7FFF ? 0x7FFF : deviceId);

        private static int Positive(int v, int fallback) => v > 0 ? v : fallback;
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
