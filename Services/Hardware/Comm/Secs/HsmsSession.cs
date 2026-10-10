// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁣志​◆‎编‌写⁠◇‍微⁠信‍﹕‎1⁣8​7‍◆‌1⁣9‍3‍6‏◇‌1‍3‌9⁣9⁣　‏※⁣保‍留⁠所‎有​权​利‎请⁣勿⁠删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NoCodeMotion.Services.Hardware.Comm.Secs
{
    /// <summary>
    /// HSMS 会话（SEMI E37）：4 字节大端长度前缀分帧 + 10 字节头。
    ///
    /// 两种角色（<see cref="Active"/>）：
    ///   被动（false）= 设备端：监听本地端口，等 Host 连进来，收到 Select.req 回 Select.rsp；
    ///   主动（true） = 主机端：主动连 Host，连上后主动发 Select.req 等 Select.rsp。
    ///
    /// 状态机：NOT CONNECTED → CONNECTED（未 Select）→ SELECTED。
    ///
    /// 超时（E37 里的 T3/T5/T6/T7/T8）：
    ///   T3 = 等数据应答；T5 = 连接分离后再接/重连的间隔；
    ///   T6 = 等控制应答（Select/Deselect/Linktest）；T7 = 连上但一直不 Select 的容忍时间；
    ///   T8 = 网络字符间超时（当作 socket 读超时）。
    ///
    /// 线程模型：
    ///   - 被动模式有一条 accept 线程（后台），连上后交给读线程；断开后等 T5 再接下一条。
    ///   - 读线程（后台）负责分帧、解码、分发：控制消息就地处理并自动应答；
    ///     数据消息若是「某条 W-bit 请求的应答」就交回等待中的发送方，否则自动应答（S1F1/S1F13 等）
    ///     并放进收件队列供 <see cref="Recv"/> 取。
    ///   - 发送用独立的写锁，等待应答靠 SystemBytes 配对的信号量，超时不会永久阻塞。
    ///
    /// ★ 刻意不依赖 HardwareBridge 的 WaitGuard：通讯通道与运动控制互不阻塞，
    ///   这条会话线程始终在后台跑，页面/脚本的收发都只是「入队/出队」。
    /// </summary>
    public sealed class HsmsSession : IDisposable
    {
        /// <summary>单帧上限（防止畸形长度字段把内存吃光）。</summary>
        public const int MaxFrameLength = 16 * 1024 * 1024;

        // ---------------- 配置 ----------------

        /// <summary>true = 主动连 Host；false = 被动监听（设备端）。</summary>
        public bool Active { get; set; }

        /// <summary>主动模式：对方 IP。</summary>
        public string Host { get; set; } = "127.0.0.1";

        /// <summary>主动模式：对方端口；被动模式：本地监听端口（0 = 由系统分配，便于测试）。</summary>
        public int Port { get; set; } = 5000;

        /// <summary>本机 DeviceID（数据消息的会话号）。</summary>
        public ushort DeviceId { get; set; }

        /// <summary>T3：等数据应答超时（ms）。</summary>
        public int T3Ms { get; set; } = 45000;

        /// <summary>T5：连接分离后再次连接/监听的间隔（ms）。</summary>
        public int T5Ms { get; set; } = 10000;

        /// <summary>T6：等控制应答超时（ms）。</summary>
        public int T6Ms { get; set; } = 5000;

        /// <summary>T7：连上但未 Select 的容忍时间（ms）。</summary>
        public int T7Ms { get; set; } = 10000;

        /// <summary>T8：网络字符间超时（ms，作为 socket 读超时）。</summary>
        public int T8Ms { get; set; } = 5000;

        /// <summary>是否自动应答 S1F1 / S1F13 / S1F15 / S1F17。</summary>
        public bool AutoReply { get; set; } = true;

        /// <summary>S1F2 / S1F14 里回的机型（MDLN）。</summary>
        public string Mdln { get; set; } = "NoCodeMotion";

        /// <summary>S1F2 / S1F14 里回的软件版本（SOFTREV）。</summary>
        public string SoftRev { get; set; } = "1.0.0";

        /// <summary>日志回调（可为 null）。</summary>
        public Action<string> Log { get; set; }

        // ---------------- 运行态 ----------------

        private readonly object _gate = new object();
        private readonly object _writeGate = new object();
        private readonly object _pendingGate = new object();
        private readonly ConcurrentQueue<SecsMessage> _inbox = new ConcurrentQueue<SecsMessage>();
        private readonly Dictionary<uint, Pending> _pending = new Dictionary<uint, Pending>();

        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _acceptThread;
        private Thread _readerThread;
        private ManualResetEventSlim _readerExited = new ManualResetEventSlim(true);
        private volatile bool _running;
        private volatile bool _connected;
        private volatile bool _selected;
        private DateTime _connectedAtUtc = DateTime.MinValue;

        /// <summary>被动模式实际监听的端口（Port 填 0 时由系统分配，测试靠它拿端口）。</summary>
        public int BoundPort { get; private set; }

        /// <summary>是否有 TCP 连接。</summary>
        public bool IsConnected => _connected;

        /// <summary>HSMS 会话是否已 Select。</summary>
        public bool IsSelected => _selected;

        /// <summary>被动模式是否正在监听（端口已绑定，等对方连入）。主动模式恒为 false。</summary>
        public bool IsListening => _running && !Active && _listener != null;

        /// <summary>最近一次错误 / 断开原因。</summary>
        public string LastError { get; private set; } = string.Empty;

        /// <summary>已发送 / 已接收的消息数（含控制消息）。</summary>
        public long SentCount { get; private set; }
        public long ReceivedCount { get; private set; }
        public long TimeoutCount { get; private set; }

        private sealed class Pending
        {
            public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
            public HsmsSType? Expect;
            public SecsMessage Reply;
        }

        private void Say(string line) => Log?.Invoke("[HSMS] " + line);

        // ---------------------------------------------------------------- 打开 / 关闭

        /// <summary>打开会话。被动模式立即返回（后台等连接）；主动模式连上并完成 Select 才返回。</summary>
        public void Open()
        {
            lock (_gate)
            {
                if (_running) return;
                LastError = string.Empty;
                _running = true;
                _readerExited = new ManualResetEventSlim(true);
            }

            if (Active) OpenActive();
            else OpenPassive();
        }

        private void OpenPassive()
        {
            var listener = new TcpListener(IPAddress.Any, Port < 0 ? 0 : Port);
            listener.Start();
            _listener = listener;
            BoundPort = ((IPEndPoint)listener.LocalEndpoint).Port;
            Say($"被动监听 0.0.0.0:{BoundPort}（DeviceID={DeviceId}）");

            _acceptThread = new Thread(AcceptLoop)
            {
                IsBackground = true,
                Name = "HSMS-Accept",
            };
            _acceptThread.Start();
        }

        private void OpenActive()
        {
            string host = (Host ?? string.Empty).Trim();
            if (host.Length == 0) throw new IOException("HSMS 主动模式的对方 IP 为空");
            if (Port <= 0) throw new IOException("HSMS 主动模式的对方端口无效：" + Port.ToString());

            var client = new TcpClient { NoDelay = true };
            try
            {
                if (!client.ConnectAsync(host, Port).Wait(T5Ms))
                {
                    client.Dispose();
                    throw new IOException($"连接 {host}:{Port} 超时（{T5Ms} ms）：请检查网线 / IP 是否同网段 / 对方是否在监听。");
                }
            }
            catch (AggregateException ex)
            {
                client.Dispose();
                throw new IOException($"连接 {host}:{Port} 失败：{ex.InnerException?.Message ?? ex.Message}");
            }
            catch (SocketException ex)
            {
                client.Dispose();
                throw new IOException($"连接 {host}:{Port} 失败：{ex.Message}");
            }

            Say($"主动连接 {host}:{Port} 成功");
            AttachClient(client);

            if (!Select())
                throw new IOException("已连上 " + host + ":" + Port + "，但 HSMS Select 失败：" + LastError);
        }

        private void AcceptLoop()
        {
            while (_running)
            {
                TcpClient client;
                try
                {
                    client = _listener.AcceptTcpClient();
                }
                catch
                {
                    return;   // 监听器被 Close() 停掉
                }

                if (!_running)
                {
                    try { client.Close(); } catch { }
                    return;
                }

                Say("对方已接入：" + RemoteOf(client));
                AttachClient(client);

                // 等这条连接结束，然后按 T5 等一会儿再收下一条（E37 的连接分离超时）
                _readerExited.Wait();
                if (!_running) return;
                if (T5Ms > 0) SleepInterruptible(T5Ms);
            }
        }

        /// <summary>关闭会话（幂等）。</summary>
        public void Close()
        {
            _running = false;

            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            try { _listener?.Stop(); } catch { }

            lock (_gate)
            {
                _stream = null;
                _client = null;
                _connected = false;
                _selected = false;
            }

            ReleaseAllPending();
        }

        public void Dispose() => Close();

        private static string RemoteOf(TcpClient c)
        {
            try { return (c.Client.RemoteEndPoint as IPEndPoint)?.ToString() ?? "?"; }
            catch { return "?"; }
        }

        private void AttachClient(TcpClient client)
        {
            var stream = client.GetStream();
            stream.ReadTimeout = T8Ms > 0 ? T8Ms : 5000;
            stream.WriteTimeout = T8Ms > 0 ? T8Ms : 5000;

            lock (_gate)
            {
                _client = client;
                _stream = stream;
                _connected = true;
                _selected = false;
                _connectedAtUtc = DateTime.UtcNow;
                _readerExited = new ManualResetEventSlim(false);
            }

            _readerThread = new Thread(ReadLoop)
            {
                IsBackground = true,
                Name = "HSMS-Reader",
            };
            _readerThread.Start();
        }

        // ---------------------------------------------------------------- 读循环

        private void ReadLoop()
        {
            var lenBuf = new byte[4];
            try
            {
                while (_running && _connected)
                {
                    bool idle;
                    if (!ReadExact(lenBuf, 4, out idle))
                    {
                        if (idle)   // 只是这段时间没数据，继续等
                        {
                            if (NotSelectedTooLong()) break;
                            continue;
                        }
                        break;      // 对端关了 / 帧头读了一半就断了
                    }

                    uint len = ((uint)lenBuf[0] << 24) | ((uint)lenBuf[1] << 16) | ((uint)lenBuf[2] << 8) | lenBuf[3];
                    if (len < SecsMessage.HeaderLength || len > MaxFrameLength)
                    {
                        LastError = "非法帧长度 " + len.ToString() + "（允许 " + SecsMessage.HeaderLength + "~" + MaxFrameLength + "）";
                        Say("协议错误：" + LastError + "，断开连接");
                        break;
                    }

                    var frame = new byte[len];
                    if (!ReadExact(frame, (int)len, out idle))
                    {
                        LastError = "报文读到一半就断了（期望 " + len.ToString() + " 字节）";
                        Say(LastError);
                        break;
                    }

                    var msg = SecsMessage.Decode(frame, 0, (int)len, out string err);
                    if (msg == null)
                    {
                        LastError = "报文解码失败：" + err;
                        Say(LastError);
                        break;
                    }

                    ReceivedCount++;
                    Dispatch(msg);
                    if (NotSelectedTooLong()) break;
                }
            }
            catch (Exception ex)
            {
                if (_running) { LastError = ex.Message; Say("读线程异常退出：" + ex.Message); }
            }
            finally
            {
                lock (_gate)
                {
                    _connected = false;
                    _selected = false;
                    try { _stream?.Close(); } catch { }
                    try { _client?.Close(); } catch { }
                    _stream = null;
                    _client = null;
                    _readerExited.Set();
                }
                ReleaseAllPending();
                if (_running && !Active) Say("连接已断开，等 T5=" + T5Ms.ToString() + " ms 后再收下一条");
            }
        }

        private bool NotSelectedTooLong()
        {
            if (T7Ms <= 0 || _selected) return false;
            if ((DateTime.UtcNow - _connectedAtUtc).TotalMilliseconds <= T7Ms) return false;
            LastError = "T7 超时：连上后 " + T7Ms.ToString() + " ms 内没有收到 Select.req";
            Say(LastError + "，断开连接");
            return true;
        }

        /// <summary>读满 n 字节。<paramref name="idle"/> = 一个字节都没读到（只是暂时没数据）。</summary>
        private bool ReadExact(byte[] buf, int n, out bool idle)
        {
            idle = false;
            int got = 0;
            while (got < n)
            {
                NetworkStream s;
                lock (_gate) s = _stream;
                if (s == null) return false;

                int read;
                try
                {
                    read = s.Read(buf, got, n - got);
                }
                catch (IOException)
                {
                    // 读超时（T8）：刚开帧头就是「没数据」，读到一半就是断帧
                    idle = got == 0;
                    return false;
                }
                catch (ObjectDisposedException) { return false; }
                catch (InvalidOperationException) { return false; }

                if (read <= 0) return false;
                got += read;
            }
            return true;
        }

        private void SleepInterruptible(int ms)
        {
            var sw = Stopwatch.StartNew();
            while (_running && sw.ElapsedMilliseconds < ms)
                Thread.Sleep(20);
        }

        // ---------------------------------------------------------------- 分发

        private void Dispatch(SecsMessage msg)
        {
            if (msg.IsControl)
            {
                HandleControl(msg);
                return;
            }

            // 1) 是某条 W-bit 请求的应答？交回等待中的发送方，不再当「非请求消息」
            if (TryCompletePending(msg)) return;

            // 2) 自动应答
            if (msg.WBit && AutoReply)
            {
                var reply = BuildAutoReply(msg);
                if (reply != null)
                {
                    string err;
                    Send(reply, out err);
                    Say("« " + msg.ToSmlLine() + "　→　自动应答 » " + reply.ToSmlLine()
                        + (err == null ? string.Empty : "（应答发送失败：" + err + "）"));
                }
                else
                {
                    Say("« " + msg.ToSmlLine() + "　⚠ W-bit 但未自动应答（需要流程/脚本处理）");
                }
            }
            else
            {
                Say("« " + msg.ToSmlLine());
            }

            // 3) 进收件队列
            _inbox.Enqueue(msg);
        }

        private void HandleControl(SecsMessage msg)
        {
            Say("« " + msg.HeaderText());

            if (TryCompletePending(msg)) return;

            switch (msg.SType)
            {
                case HsmsSType.SelectReq:
                    {
                        bool was = _selected;
                        _selected = true;
                        string err;
                        Send(SecsMessage.Control(HsmsSType.SelectRsp, 0, msg.SystemBytes), out err);
                        Say("回 Select.rsp(status=0)" + (was ? "（重复 Select）" : "，HSMS 会话已建立")
                            + (err == null ? string.Empty : "（发送失败：" + err + "）"));
                        break;
                    }
                case HsmsSType.DeselectReq:
                    {
                        _selected = false;
                        string err;
                        Send(SecsMessage.Control(HsmsSType.DeselectRsp, 0, msg.SystemBytes), out err);
                        Say("回 Deselect.rsp(status=0)，会话回到未选中");
                        break;
                    }
                case HsmsSType.LinktestReq:
                    {
                        string err;
                        Send(SecsMessage.Control(HsmsSType.LinktestRsp, 0, msg.SystemBytes), out err);
                        break;
                    }
                case HsmsSType.SeparateReq:
                    Say("对方要求 Separate，主动断开连接");
                    try { _stream?.Close(); } catch { }
                    try { _client?.Close(); } catch { }
                    break;
                default:
                    Say("收到无人等待的 " + msg.HeaderText() + "（已忽略）");
                    break;
            }
        }

        private bool TryCompletePending(SecsMessage msg)
        {
            Pending pending;
            lock (_pendingGate)
            {
                if (!_pending.TryGetValue(msg.SystemBytes, out pending)) return false;
                if (pending.Expect.HasValue && pending.Expect.Value != msg.SType) return false;
                if (!pending.Expect.HasValue && !msg.IsData) return false;
                _pending.Remove(msg.SystemBytes);
            }
            pending.Reply = msg;
            pending.Done.Set();
            return true;
        }

        private void ReleaseAllPending()
        {
            List<Pending> list;
            lock (_pendingGate)
            {
                list = new List<Pending>(_pending.Values);
                _pending.Clear();
            }
            foreach (var p in list) p.Done.Set();
        }

        /// <summary>
        /// 标准「在线/通讯」类请求的自动应答（其余一律不猜，留给流程/脚本）：
        ///   S1F1  → S1F2  &lt;L &lt;A MDLN&gt; &lt;A SOFTREV&gt;&gt;
        ///   S1F13 → S1F14 &lt;L &lt;B 0&gt; &lt;L &lt;A MDLN&gt; &lt;A SOFTREV&gt;&gt;&gt;
        ///   S1F15 → S1F16 &lt;B 0&gt;（请求离线，OFFLACK=0 已接受）
        ///   S1F17 → S1F18 &lt;B 0&gt;（请求在线，ONLACK=0 已接受）
        /// </summary>
        private SecsMessage BuildAutoReply(SecsMessage msg)
        {
            var body = SecsItem.L(SecsItem.A(Mdln ?? string.Empty), SecsItem.A(SoftRev ?? string.Empty));
            switch (msg.Stream)
            {
                case 1:
                    if (msg.Function == 1) return SecsMessage.Data(DeviceId, 1, 2, false, msg.SystemBytes, body);
                    if (msg.Function == 13)
                        return SecsMessage.Data(DeviceId, 1, 14, false, msg.SystemBytes,
                            SecsItem.L(SecsItem.B(0), body));
                    if (msg.Function == 15) return SecsMessage.Data(DeviceId, 1, 16, false, msg.SystemBytes, SecsItem.B(0));
                    if (msg.Function == 17) return SecsMessage.Data(DeviceId, 1, 18, false, msg.SystemBytes, SecsItem.B(0));
                    break;
            }
            return null;
        }

        // ---------------------------------------------------------------- 收发

        /// <summary>
        /// 发送一条消息。W-bit 数据消息等数据应答（T3），控制消息等控制应答（T6）；
        /// 不需要应答的消息发完即返回 null。<paramref name="error"/> 非空表示失败/超时。
        /// </summary>
        public SecsMessage Send(SecsMessage msg, out string error)
        {
            error = null;
            if (msg == null) { error = "消息为空"; return null; }
            if (!_connected)
            {
                error = "HSMS 未连接" + (LastError.Length > 0 ? "（" + LastError + "）" : string.Empty);
                return null;
            }

            // ★ Separate.req / Reject.req 没有应答，不能等 —— 用 ReplyType() 判断而不是 IsControl
            bool waitReply = msg.IsData ? msg.WBit : msg.ReplyType().HasValue;
            Pending pending = null;
            if (waitReply)
            {
                pending = new Pending { Expect = msg.IsControl ? msg.ReplyType() : null };
                lock (_pendingGate) _pending[msg.SystemBytes] = pending;
            }

            try
            {
                WriteFrame(msg.Encode());
                SentCount++;
                Say("» " + msg.ToSmlLine());
                if (pending == null) return null;

                int timeout = msg.IsControl ? T6Ms : T3Ms;
                if (!pending.Done.Wait(timeout))
                {
                    TimeoutCount++;
                    error = "等待应答超时（" + timeout.ToString() + " ms，SystemBytes=0x"
                            + msg.SystemBytes.ToString("X8") + "）";
                    Say(error);
                    return null;
                }
                if (pending.Reply == null)
                {
                    error = "连接已断开，未收到应答";
                    return null;
                }
                return pending.Reply;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Say("发送失败：" + ex.Message);
                return null;
            }
            finally
            {
                if (pending != null)
                {
                    lock (_pendingGate) _pending.Remove(msg.SystemBytes);
                }
            }
        }

        private void WriteFrame(byte[] frame)
        {
            lock (_writeGate)
            {
                var s = _stream;
                if (s == null) throw new IOException("HSMS 连接已断开");
                var len = new byte[4];
                len[0] = (byte)((frame.Length >> 24) & 0xFF);
                len[1] = (byte)((frame.Length >> 16) & 0xFF);
                len[2] = (byte)((frame.Length >> 8) & 0xFF);
                len[3] = (byte)(frame.Length & 0xFF);
                s.Write(len, 0, 4);
                s.Write(frame, 0, frame.Length);
                s.Flush();
            }
        }

        /// <summary>取一条「非请求」的入站数据消息；超时返回 null。</summary>
        public SecsMessage Recv(int timeoutMs)
        {
            var sw = Stopwatch.StartNew();
            while (true)
            {
                if (_inbox.TryDequeue(out var m)) return m;
                if (timeoutMs <= 0 || sw.ElapsedMilliseconds >= timeoutMs) return null;
                Thread.Sleep(10);
            }
        }

        /// <summary>取一条已入队的数据消息（不等待）。</summary>
        public bool TryDequeue(out SecsMessage msg) => _inbox.TryDequeue(out msg);

        /// <summary>丢弃所有未取的入站消息。</summary>
        public void ClearInbox()
        {
            while (_inbox.TryDequeue(out _)) { }
        }

        /// <summary>发送 Select.req 并等 Select.rsp（T6）。</summary>
        public bool Select()
        {
            string err;
            var rsp = Send(SecsMessage.Control(HsmsSType.SelectReq, 0, SecsMessage.NewSystemBytes()), out err);
            if (rsp == null) { LastError = "Select 失败：" + err; Say(LastError); return false; }
            if (rsp.SType != HsmsSType.SelectRsp) { LastError = "Select 收到意外应答 " + rsp.HeaderText(); Say(LastError); return false; }
            if (rsp.Status != 0) { LastError = "Select 被拒绝，status=" + rsp.Status.ToString(); Say(LastError); return false; }
            _selected = true;
            Say("Select 完成，HSMS 会话已建立");
            return true;
        }

        /// <summary>发送 Deselect.req 并等应答（T6）。</summary>
        public bool Deselect()
        {
            string err;
            var rsp = Send(SecsMessage.Control(HsmsSType.DeselectReq, 0, SecsMessage.NewSystemBytes()), out err);
            if (rsp == null) { LastError = "Deselect 失败：" + err; return false; }
            _selected = false;
            return true;
        }

        /// <summary>发送 Linktest.req 并等应答（T6）；用于探活。</summary>
        public bool Linktest()
        {
            string err;
            var rsp = Send(SecsMessage.Control(HsmsSType.LinktestReq, 0, SecsMessage.NewSystemBytes()), out err);
            if (rsp == null) { LastError = "Linktest 失败：" + err; return false; }
            return rsp.SType == HsmsSType.LinktestRsp;
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
