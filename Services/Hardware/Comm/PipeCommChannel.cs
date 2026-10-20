// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁣志​◆‎编‌写⁠◇‍微⁠信‍﹕‎1⁣8​7‍◆‌1⁣9‍3‍6‏◇‌1‍3‌9⁣9⁣　‏※⁣保‍留⁠所‎有​权​利‎请⁣勿⁠删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Hardware.Comm
{
    /// <summary>
    /// 命名管道通道（CommType = 管道）：同一台电脑上两个进程之间的高速字节流，
    /// 用法和网口 TCP 基本一致，但不用占端口、不用管防火墙，是本机对接最常用的方式
    /// （例如同时开着两份本软件、或和别的上位机 / 仿真程序互相送信号）。
    /// <para>「管道名」两端必须填同一个（可以只写名字如 <c>ncm1</c>，也可以写 <c>\\.\pipe\ncm1</c>，
    /// 两种写法等价）；一端选「服务端」另一端选「客户端」，服务端先启动等对方连。</para>
    /// <para>★ 收数据靠一个后台读线程：命名管道没有 <c>TcpClient</c> 那种 DataAvailable 可问，
    /// 只能一直读、把读到的字节攒进缓冲，Recv 从缓冲里取 —— 否则要么阻塞死、要么丢数据。</para>
    /// </summary>
    public sealed class PipeCommChannel : ICommChannel
    {
        private readonly CommItem _cfg;
        private readonly object _gate = new object();
        private readonly List<byte> _rx = new List<byte>();
        private Thread _reader;
        private volatile bool _reading;
        private PipeStream _pipe;

        public PipeCommChannel(CommItem cfg) => _cfg = cfg;

        public string Name => _cfg.Name;

        /// <summary>是否「服务端」（等对方连）；填「客户端」则主动去连对方。</summary>
        private bool IsServer => !string.Equals((_cfg.PipeRole ?? string.Empty).Trim(), "客户端",
                                                StringComparison.OrdinalIgnoreCase);

        public bool IsOpen
        {
            get { lock (_gate) return _pipe != null && _pipe.IsConnected; }
        }

        /// <summary>管道名（去掉 \\.\pipe\ 前缀；空则用通道名兜底）。</summary>
        private string PipeName
        {
            get
            {
                string p = (_cfg.PortOrIp ?? string.Empty).Trim().Trim('"');
                if (p.StartsWith(@"\\.\pipe\", StringComparison.OrdinalIgnoreCase))
                    p = p.Substring(@"\\.\pipe\".Length);
                else if (p.StartsWith(@"\\localhost\pipe\", StringComparison.OrdinalIgnoreCase))
                    p = p.Substring(@"\\localhost\pipe\".Length);
                if (p.Length == 0)
                    p = string.IsNullOrWhiteSpace(_cfg.Name) ? "ncm_pipe" : _cfg.Name.Trim();
                return p;
            }
        }

        public void Open()
        {
            lock (_gate)
            {
                if (_pipe != null && _pipe.IsConnected) return;
                Cleanup();

                int timeout = _cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 1000;
                string pipeName = PipeName;

                if (IsServer)
                {
                    // ★ 服务端只负责在本机建监听，**不在这里等对方连入**：
                    //   「打开连接」跑在 UI 线程上，等对方会把界面挂死到超时。
                    //   连入由后台线程完成，连上后自动起读线程；发送 / 接收前用 RequireConnected 等。
                    NamedPipeServerStream srv;
                    try
                    {
                        srv = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1,
                            PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    }
                    catch (Exception ex)
                    {
                        // 最常见的现场原因就是两端都选了「服务端」：第二个服务端会撞到这句系统报错
                        throw new IOException($"管道「{pipeName}」无法在本机建立监听：{ex.Message}"
                                              + "（多半是两端都把「管道角色」选成了「服务端」—— 请把其中一端改成「客户端」，或换一个管道名）");
                    }
                    _pipe = srv;
                    new Thread(() =>
                    {
                        try
                        {
                            srv.WaitForConnection();
                            if (srv.IsConnected) StartReader(srv);
                        }
                        catch { /* 被 Dispose 打断，属正常退出 */ }
                    }) { IsBackground = true, Name = "PipeAccept-" + _cfg.Name }.Start();
                    return;
                }

                var cli = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                _pipe = cli;
                try
                {
                    cli.Connect(timeout);
                }
                catch (TimeoutException)
                {
                    Cleanup();
                    throw new IOException($"连接管道「{pipeName}」超时（{timeout}ms）："
                                          + "请确认另一端已把「管道角色」选成「服务端」并已点过「打开连接」/收发。");
                }
                catch (Exception ex)
                {
                    Cleanup();
                    throw new IOException($"连接管道「{pipeName}」失败：{ex.Message}");
                }
                StartReader(cli);
            }
        }

        /// <summary>等对方连入（服务端建立监听后是异步接受的，收发前得确认真的连上了）。</summary>
        private bool WaitConnected(int ms)
        {
            int waited = 0;
            while (waited < ms)
            {
                lock (_gate) { if (_pipe != null && _pipe.IsConnected) return true; }
                Thread.Sleep(10);
                waited += 10;
            }
            lock (_gate) return _pipe != null && _pipe.IsConnected;
        }

        /// <summary>没收发通道时给一句能照着做的中文提示，而不是丢个异常栈。</summary>
        private void RequireConnected(string doing)
        {
            int timeout = _cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 1000;
            if (WaitConnected(timeout)) return;
            throw new IOException($"管道「{PipeName}」{doing}前没等到对方连入（{timeout}ms）："
                                  + (IsServer
                                        ? "本端是「服务端」，请确认另一端把「管道角色」选成「客户端」、且管道名与本端一致。"
                                        : "本端是「客户端」，请确认另一端把「管道角色」选成「服务端」并已建立监听。"));
        }

        public void Send(string data)
        {
            Open();
            RequireConnected("发送");
            byte[] payload = PayloadCodec.Encode(data);
            if (payload.Length == 0) return;
            lock (_gate)
            {
                try
                {
                    _pipe.Write(payload, 0, payload.Length);
                    _pipe.Flush();
                }
                catch (Exception ex)
                {
                    Cleanup();
                    throw new IOException($"管道「{PipeName}」发送失败：{ex.Message}（对方可能已关闭，重新收发会自动重连）");
                }
            }
        }

        public string Recv()
        {
            Open();
            RequireConnected("接收");
            int timeout = _cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 1000;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                byte[] got = null;
                lock (_gate)
                {
                    if (_rx.Count > 0)
                    {
                        got = _rx.ToArray();
                        _rx.Clear();
                    }
                }
                if (got != null) return PayloadCodec.Decode(got, got.Length);

                if (sw.ElapsedMilliseconds >= timeout) return string.Empty;
                Thread.Sleep(5);
            }
        }

        /// <summary>后台读线程：把对方送来的字节持续塞进 <see cref="_rx"/>。
        /// 命名管道没有 DataAvailable 可问，只能一直读 —— 不这么做就会丢数据或挂死。</summary>
        private void StartReader(PipeStream pipe)
        {
            if (_reading || pipe == null) return;
            _reading = true;
            _reader = new Thread(() =>
            {
                var buf = new byte[8192];
                while (_reading)
                {
                    int n;
                    try
                    {
                        if (!pipe.IsConnected) break;
                        n = pipe.Read(buf, 0, buf.Length);
                    }
                    catch { break; }          // 对方关闭 / 被 Dispose：结束本线程
                    if (n <= 0) break;
                    lock (_gate)
                    {
                        for (int i = 0; i < n; i++) _rx.Add(buf[i]);
                    }
                }
                _reading = false;
            }) { IsBackground = true, Name = "PipeReader-" + _cfg.Name };
            _reader.Start();
        }

        public void Dispose()
        {
            lock (_gate) Cleanup();
        }

        private void Cleanup()
        {
            _reading = false;
            try { _pipe?.Dispose(); } catch { }
            _pipe = null;
            _rx.Clear();
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
