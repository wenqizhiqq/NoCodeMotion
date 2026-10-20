// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁣志​◆‎编‌写⁠◇‍微⁠信‍﹕‎1⁣8​7‍◆‌1⁣9‍3‍6‏◇‌1‍3‌9⁣9⁣　‏※⁣保‍留⁠所‎有​权​利‎请⁣勿⁠删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Hardware.Comm.Sim
{
    /// <summary>
    /// 「反向通道」型对端：在本机按同一份配置再起一条通道当对方（管道 / 共享内存 / 共享文件夹 / 本机文件 / SECS）。
    /// 本端照常走真实通道，只是另一端换成了软件自己 —— 收发、超时、编码全是真的。
    /// </summary>
    public sealed class PeerChannelSimulator : CommSimBase
    {
        private readonly Func<CommItem, ICommChannel> _buildPeer;
        private readonly string _endpoint;
        private readonly string _note;
        private ICommChannel _peer;
        private Thread _loop;
        private volatile bool _on;

        public PeerChannelSimulator(CommItem cfg, Func<CommItem, ICommChannel> buildPeer, string endpoint, string note)
            : base(cfg)
        {
            _buildPeer = buildPeer;
            _endpoint = endpoint;
            _note = note;
        }

        public override string Endpoint => _endpoint;
        public override string Note => _note;

        public override void Start()
        {
            if (IsRunning) return;
            var peer = _buildPeer(Cfg);            // 建不出来会抛中文 IOException
            peer.Open();
            _peer = peer;
            SetRunning(true);
            _on = true;
            _loop = new Thread(Loop) { IsBackground = true, Name = "CommSim-" + Cfg.Name };
            _loop.Start();
            Log($"[仿真] 「{Cfg.Name}」对端已启动：{_endpoint}");
        }

        private void Loop()
        {
            while (_on)
            {
                string got;
                try { got = _peer.Recv(); }
                catch (Exception ex) { if (_on) Log($"[仿真] 「{Cfg.Name}」对端读取中断：{ex.Message}"); break; }
                if (string.IsNullOrEmpty(got)) continue;

                Log($"[仿真] 对端收到本端 → {got}");
                string reply = ReplyFor(got);
                if (reply.Length == 0) continue;
                try
                {
                    _peer.Send(reply);
                    Log($"[仿真] 对端回应本端 ← {reply}");
                }
                catch (Exception ex) { Log($"[仿真] 「{Cfg.Name}」对端回应失败：{ex.Message}"); break; }
            }
            _on = false;
            SetRunning(false);
        }

        public override void Stop()
        {
            _on = false;
            try { _peer?.Dispose(); } catch { }
            _peer = null;
            SetRunning(false);
        }

        public override bool Inject(string text)
        {
            var p = _peer;
            if (p == null) return false;
            p.Send(text);
            Log($"[仿真] 手工注入 → 本端下一次「接收」会拿到：{text}");
            return true;
        }
    }

    /// <summary>
    /// TCP 对端（网口TCP / 相机网口）：在本机监听本端要连的那个端口，收到什么按模式回什么。
    /// 一台设备（PLC / 相机 / 条码枪）在本机的替身 —— 本端连 127.0.0.1 就等于连上了「设备」。
    /// </summary>
    public sealed class TcpPeerSimulator : CommSimBase
    {
        private TcpListener _listener;
        private Thread _accept;
        private volatile bool _on;
        private readonly List<TcpClient> _clients = new List<TcpClient>();

        public TcpPeerSimulator(CommItem cfg) : base(cfg) { }

        private int Port => Cfg.BaudOrPort > 0 ? Cfg.BaudOrPort : 9000;

        public override string Endpoint => $"本机监听 0.0.0.0:{Port}（本端连 {Cfg.PortOrIp}:{Port}）";

        public override string Note => "这个「对方」只会收到什么就回什么，不懂设备自己的那套私有命令（比如相机命令集）；"
                                       + "想让它固定回某一句，把上面的模式改成「按规则」，再写好规则就行。";

        public override void Start()
        {
            if (IsRunning) return;
            try
            {
                _listener = new TcpListener(IPAddress.Any, Port);
                _listener.Start();
            }
            catch (SocketException ex)
            {
                throw new IOException($"仿真对端监听端口 {Port} 失败：{ex.Message}"
                                      + "（端口被占用就换一个，或先停掉占用该端口的程序）");
            }
            SetRunning(true);
            _on = true;
            _accept = new Thread(AcceptLoop) { IsBackground = true, Name = "CommSimTcp-" + Cfg.Name };
            _accept.Start();
            Log($"[仿真] 「{Cfg.Name}」TCP 对端已监听 0.0.0.0:{Port}");
        }

        private void AcceptLoop()
        {
            while (_on)
            {
                TcpClient cli;
                try { cli = _listener.AcceptTcpClient(); }
                catch { break; }
                lock (_clients) _clients.Add(cli);
                Log($"[仿真] 本端已连入：{(cli.Client.RemoteEndPoint ?? (object)"?")}");
                new Thread(() => Serve(cli)) { IsBackground = true }.Start();
            }
            SetRunning(false);
        }

        private void Serve(TcpClient cli)
        {
            var buf = new byte[8192];
            try
            {
                var ns = cli.GetStream();
                while (_on && cli.Connected)
                {
                    if (!ns.DataAvailable) { Thread.Sleep(5); continue; }
                    int n = ns.Read(buf, 0, buf.Length);
                    if (n <= 0) break;
                    string got = PayloadCodec.Decode(buf, n);
                    Log($"[仿真] TCP 对端收到本端 → {got}");
                    string reply = ReplyFor(got);
                    if (reply.Length == 0) continue;
                    var bytes = PayloadCodec.Encode(reply);
                    ns.Write(bytes, 0, bytes.Length);
                    ns.Flush();
                    Log($"[仿真] TCP 对端回应本端 ← {reply}");
                }
            }
            catch (Exception ex) { if (_on) Log($"[仿真] TCP 对端连接中断：{ex.Message}"); }
            finally { lock (_clients) _clients.Remove(cli); try { cli.Dispose(); } catch { } }
        }

        public override void Stop()
        {
            _on = false;
            lock (_clients)
            {
                foreach (var c in _clients) { try { c.Dispose(); } catch { } }
                _clients.Clear();
            }
            try { _listener?.Stop(); } catch { }
            _listener = null;
            SetRunning(false);
        }

        public override bool Inject(string text)
        {
            var bytes = PayloadCodec.Encode(text);
            int sent = 0;
            lock (_clients)
            {
                foreach (var c in _clients)
                {
                    try { var ns = c.GetStream(); ns.Write(bytes, 0, bytes.Length); ns.Flush(); sent++; }
                    catch { }
                }
            }
            if (sent == 0) return false;
            Log($"[仿真] 已把「{text}」推给 {sent} 个本端连接");
            return true;
        }
    }

    /// <summary>UDP 对端：监听本端要发的端口，谁发来就回给谁（UDP 无连接，注入需要先收到过一包）。</summary>
    public sealed class UdpPeerSimulator : CommSimBase
    {
        private UdpClient _udp;
        private Thread _loop;
        private volatile bool _on;
        private IPEndPoint _lastRemote;

        public UdpPeerSimulator(CommItem cfg) : base(cfg) { }

        private int Port => Cfg.BaudOrPort > 0 ? Cfg.BaudOrPort : 9000;

        public override string Endpoint => $"本机监听 UDP :{Port}（本端发到 {Cfg.PortOrIp}:{Port}）";

        public override string Note => "UDP 本来就不保证一定送到，这个「对方」只把最近收到的那一包回给你；"
                                       + "你这边一次都没发过的时候，它没法主动塞内容给你。";

        public override void Start()
        {
            if (IsRunning) return;
            try { _udp = new UdpClient(Port); }
            catch (SocketException ex)
            {
                throw new IOException($"仿真对端监听 UDP 端口 {Port} 失败：{ex.Message}");
            }
            SetRunning(true);
            _on = true;
            _loop = new Thread(Loop) { IsBackground = true, Name = "CommSimUdp-" + Cfg.Name };
            _loop.Start();
            Log($"[仿真] 「{Cfg.Name}」UDP 对端已监听 :{Port}");
        }

        private void Loop()
        {
            while (_on)
            {
                byte[] data;
                IPEndPoint remote = null;
                try { data = _udp.Receive(ref remote); }
                catch { break; }
                if (data == null || data.Length == 0) continue;
                _lastRemote = remote;
                string got = PayloadCodec.Decode(data, data.Length);
                Log($"[仿真] UDP 对端收到本端（{remote}）→ {got}");
                string reply = ReplyFor(got);
                if (reply.Length == 0) continue;
                var bytes = PayloadCodec.Encode(reply);
                try { _udp.Send(bytes, bytes.Length, remote); Log($"[仿真] UDP 对端回应 ← {reply}"); } catch { }
            }
            SetRunning(false);
        }

        public override void Stop()
        {
            _on = false;
            try { _udp?.Close(); } catch { }
            _udp = null;
            SetRunning(false);
        }

        public override bool Inject(string text)
        {
            var r = _lastRemote;
            if (r == null || _udp == null) return false;
            var bytes = PayloadCodec.Encode(text);
            _udp.Send(bytes, bytes.Length, r);
            Log($"[仿真] 已把「{text}」发给本端 {r}");
            return true;
        }
    }

    /// <summary>
    /// Web(HTTP) 对端：本机起一个极简 HTTP 服务，替身就是「MES / WebAPI」。
    /// <para>本端 URL 里的端口就是本对端监听的端口；响应正文按模式生成
    /// （回显 = 把请求体原样回；按规则 = 用「METHOD 路径」或请求体匹配）。</para>
    /// <para>★ 用原生 TcpListener 手写响应，不用 HttpListener：后者在 Windows 上非管理员要 urlacl 授权，
    /// 现场经常直接报「拒绝访问」。</para>
    /// </summary>
    public sealed class HttpMockSimulator : CommSimBase
    {
        private TcpListener _listener;
        private Thread _accept;
        private volatile bool _on;
        private readonly List<TcpClient> _clients = new List<TcpClient>();

        public HttpMockSimulator(CommItem cfg) : base(cfg) { }

        private int Port
        {
            get
            {
                if (Cfg.BaudOrPort > 0) return Cfg.BaudOrPort;
                try
                {
                    var u = new Uri(UrlWithScheme());
                    return u.Port;
                }
                catch { return 8080; }
            }
        }

        private string UrlWithScheme()
        {
            string u = (Cfg.PortOrIp ?? string.Empty).Trim();
            if (!u.StartsWith("http", StringComparison.OrdinalIgnoreCase)) u = "http://" + u;
            return u;
        }

        public override string Endpoint => $"本机 HTTP 服务 http://127.0.0.1:{Port}/（本端 URL 填它）";

        public override string Note => "这个「对方」只做最常用的那一种回应：回 200 加一段内容，不认网址路由、也不查账号密码；"
                                       + "想让不同的请求回不同的内容，就用「按规则」去匹配请求方式和网址。";

        public override void Start()
        {
            if (IsRunning) return;
            try
            {
                _listener = new TcpListener(IPAddress.Any, Port);
                _listener.Start();
            }
            catch (SocketException ex)
            {
                throw new IOException($"仿真 HTTP 服务监听端口 {Port} 失败：{ex.Message}");
            }
            SetRunning(true);
            _on = true;
            _accept = new Thread(AcceptLoop) { IsBackground = true, Name = "CommSimHttp-" + Cfg.Name };
            _accept.Start();
            Log($"[仿真] 「{Cfg.Name}」HTTP 对端已启动：http://127.0.0.1:{Port}/");
        }

        private void AcceptLoop()
        {
            while (_on)
            {
                TcpClient cli;
                try { cli = _listener.AcceptTcpClient(); } catch { break; }
                lock (_clients) _clients.Add(cli);
                new Thread(() => Serve(cli)) { IsBackground = true }.Start();
            }
            SetRunning(false);
        }

        private void Serve(TcpClient cli)
        {
            try
            {
                var ns = cli.GetStream();
                ns.ReadTimeout = 5000;
                string head = ReadHead(ns);
                if (head.Length == 0) return;
                string[] first = head.Split(' ');
                string method = first.Length > 0 ? first[0] : "?";
                string path = first.Length > 1 ? first[1] : "/";
                int len = 0;
                foreach (var l in head.Split('\n'))
                    if (l.StartsWith("Content-Length", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(l.Split(':')[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out len);
                string body = "";
                if (len > 0)
                {
                    var buf = new byte[len];
                    int got = 0;
                    while (got < len) { int n = ns.Read(buf, got, len - got); if (n <= 0) break; got += n; }
                    body = Encoding.UTF8.GetString(buf, 0, got);
                }

                string key = method + " " + path;
                string reply = ReplyFor(body.Length > 0 ? body : key);
                if (reply.Length == 0) reply = key;                       // 静默也至少回 200，别让本端当成连不上
                Log($"[仿真] HTTP 对端收到 {key} 正文「{body}」→ 回 {reply.Length} 字节");

                var payload = Encoding.UTF8.GetBytes(reply);
                var header = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain; charset=utf-8\r\n"
                           + $"Content-Length: {payload.Length}\r\nConnection: close\r\n\r\n";
                var hb = Encoding.ASCII.GetBytes(header);
                ns.Write(hb, 0, hb.Length);
                ns.Write(payload, 0, payload.Length);
                ns.Flush();
            }
            catch (Exception ex) { if (_on) Log($"[仿真] HTTP 对端处理失败：{ex.Message}"); }
            finally { lock (_clients) _clients.Remove(cli); try { cli.Dispose(); } catch { } }
        }

        /// <summary>
        /// 读到「头部结束」为止。HTTP 每行以 CRLF 结尾，结束空行是 \r\n\r\n ——
        /// 只判「连续两个 LF」永远等不到（第二个 LF 前面还夹着 CR），会一直堵到对方超时。
        /// </summary>
        private static string ReadHead(NetworkStream ns)
        {
            var sb = new StringBuilder();
            while (true)
            {
                int c;
                try { c = ns.ReadByte(); } catch { break; }
                if (c < 0) break;
                sb.Append((char)c);
                int n = sb.Length;
                if (n >= 4 && sb[n - 1] == '\n' && sb[n - 2] == '\r' && sb[n - 3] == '\n') break;   // …\n\r\n
                if (n >= 2 && sb[n - 1] == '\n' && sb[n - 2] == '\n') break;                        // 纯 LF 写法也认
            }
            return sb.ToString();
        }

        public override void Stop()
        {
            _on = false;
            lock (_clients)
            {
                foreach (var c in _clients) { try { c.Dispose(); } catch { } }
                _clients.Clear();
            }
            try { _listener?.Stop(); } catch { }
            _listener = null;
            SetRunning(false);
        }

        public override bool Inject(string text) => false;   // HTTP 是从请求到响应，没有「服务端主动推」
    }

    /// <summary>
    /// 回环型仿真（串口 / ModbusRTU / GPIB / 西门子S7 / 三菱MC）：本机起不了「另一端」（要物理线 / 未内置协议），
    /// 于是直接<b>接管本端通道</b>：本端发送不再碰硬件，只记日志并按规则产生「收到的数据」。
    /// </summary>
    public sealed class LoopbackSimulator : CommSimBase
    {
        private readonly Queue<string> _pending = new Queue<string>();
        private readonly List<string> _history = new List<string>();

        public LoopbackSimulator(CommItem cfg) : base(cfg) { }

        public override string Endpoint => $"回环接管本端（{Cfg.CommType}：{Cfg.PortOrIp}），不碰任何硬件";

        public override string Note => "开着仿真时，你发出去的东西只记进日志，不会真的打到串口或设备上；点【接收】拿到的是按规则生成的、或者你手工注入的内容。"
                                       + "把仿真关掉就换回连真设备。";

        public override bool TakesOverLocalChannel => true;

        public override void Start()
        {
            if (IsRunning) return;
            SetRunning(true);
            Log($"[仿真] 「{Cfg.Name}」已切到回环仿真：发送不碰硬件，接收按模式「{Cfg.SimMode}」产生");
        }

        public override void Stop()
        {
            SetRunning(false);
            lock (_pending) { _pending.Clear(); }
        }

        /// <summary>本端发送进来：记历史 + 按规则算应答入队（供本端「接收」取）。</summary>
        public void FeedFromLocal(string data)
        {
            lock (_history)
            {
                _history.Add(data ?? string.Empty);
                if (_history.Count > 200) _history.RemoveAt(0);
            }
            Log($"[仿真] 本端发送（已拦截，未下发硬件）→ {data}");
            string reply = ReplyFor(data);
            if (reply.Length == 0) return;
            lock (_pending) _pending.Enqueue(reply);
            Log($"[仿真] 按模式产生应答，本端下一次「接收」会拿到：{reply}");
        }

        /// <summary>本端接收：取队列（没有就等到超时）。</summary>
        public string TakeFromSim(int timeoutMs)
        {
            int waited = 0;
            while (true)
            {
                lock (_pending) if (_pending.Count > 0) return _pending.Dequeue();
                if (waited >= timeoutMs) return string.Empty;
                Thread.Sleep(10);
                waited += 10;
            }
        }

        /// <summary>最近 N 条本端发送（面板显示，便于核对脚本到底发了什么）。</summary>
        public IReadOnlyList<string> SentHistory
        {
            get { lock (_history) return _history.ToArray(); }
        }

        public override bool Inject(string text)
        {
            lock (_pending) _pending.Enqueue(text);
            Log($"[仿真] 手工注入 → 本端下一次「接收」会拿到：{text}");
            return true;
        }

        /// <summary>给 CommManager 用的通道壳：把本端收发接到本仿真器上。</summary>
        public ICommChannel CreateChannel() => new LoopbackCommChannel(this, Cfg);

        private sealed class LoopbackCommChannel : ICommChannel
        {
            private readonly LoopbackSimulator _sim;
            private readonly CommItem _cfg;

            public LoopbackCommChannel(LoopbackSimulator sim, CommItem cfg) { _sim = sim; _cfg = cfg; }

            public string Name => _cfg.Name;
            public bool IsOpen => _sim.IsRunning;
            public void Open() => _sim.Start();
            public void Send(string data) => _sim.FeedFromLocal(data);
            public string Recv() => _sim.TakeFromSim(_cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 1000);
            public void Dispose() { }
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
