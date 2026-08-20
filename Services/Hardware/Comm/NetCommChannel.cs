#nullable disable
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Hardware.Comm
{
    /// <summary>
    /// 真实网口 TCP 通道（相机、PLC、条码枪、激光打标机等以太网设备）。
    ///
    /// 对应配置：CommType = 网口TCP / 相机网口，PortOrIp = 对方 IP，BaudOrPort = 端口号。
    /// 断线后下次收发会自动重连。
    /// </summary>
    public sealed class TcpCommChannel : ICommChannel
    {
        private readonly CommItem _cfg;
        private readonly object _gate = new object();
        private TcpClient _client;
        private NetworkStream _stream;

        public TcpCommChannel(CommItem cfg) => _cfg = cfg;

        public string Name => _cfg.Name;

        public bool IsOpen => _client != null && _client.Connected;

        public void Open()
        {
            lock (_gate)
            {
                if (IsOpen) return;
                Cleanup();

                string ip = (_cfg.PortOrIp ?? string.Empty).Trim();
                if (ip.Length == 0)
                    throw new IOException("网口地址为空：请在通讯配置里填写对方 IP（如 192.168.1.10）。");
                int port = _cfg.BaudOrPort > 0 ? _cfg.BaudOrPort : 502;
                int timeout = _cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 1000;

                var client = new TcpClient { NoDelay = true, ReceiveTimeout = timeout, SendTimeout = timeout };
                try
                {
                    if (!client.ConnectAsync(ip, port).Wait(timeout))
                    {
                        client.Dispose();
                        throw new IOException($"连接 {ip}:{port} 超时（{timeout}ms）：请检查网线、IP 是否同网段、对方端口是否开启。");
                    }
                    _client = client;
                    _stream = client.GetStream();
                }
                catch (AggregateException ex)
                {
                    client.Dispose();
                    throw new IOException($"连接 {ip}:{port} 失败：{ex.InnerException?.Message ?? ex.Message}");
                }
                catch (SocketException ex)
                {
                    client.Dispose();
                    throw new IOException($"连接 {ip}:{port} 失败：{ex.Message}");
                }
            }
        }

        public void Send(string data)
        {
            Open();
            byte[] payload = PayloadCodec.Encode(data);
            if (payload.Length == 0) return;
            lock (_gate)
            {
                try { _stream.Write(payload, 0, payload.Length); }
                catch (IOException)
                {
                    Cleanup();
                    throw new IOException($"向 {_cfg.PortOrIp}:{_cfg.BaudOrPort} 发送失败：连接已断开，请检查设备与网络。");
                }
            }
        }

        public string Recv()
        {
            Open();
            int timeout = _cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 1000;
            var buffer = new byte[8192];
            int total = 0;
            int waited = 0;

            while (waited < timeout && total < buffer.Length)
            {
                bool hasData;
                lock (_gate) { hasData = _stream != null && _stream.DataAvailable; }

                if (hasData)
                {
                    lock (_gate)
                    {
                        int read = _stream.Read(buffer, total, buffer.Length - total);
                        if (read <= 0) break;
                        total += read;
                    }
                    Thread.Sleep(5);
                    waited += 5;
                    // 数据读完就返回（TCP 粘包时按静默判定）
                    lock (_gate) { if (_stream != null && !_stream.DataAvailable) break; }
                    continue;
                }

                if (total > 0) break;
                Thread.Sleep(5);
                waited += 5;
            }

            return PayloadCodec.Decode(buffer, total);
        }

        public void Dispose()
        {
            lock (_gate) Cleanup();
        }

        private void Cleanup()
        {
            try { _stream?.Dispose(); } catch { }
            try { _client?.Dispose(); } catch { }
            _stream = null;
            _client = null;
        }
    }

    /// <summary>
    /// 真实网口 UDP 通道。对应配置：CommType = 网口UDP，PortOrIp = 对方 IP，BaudOrPort = 端口号。
    /// </summary>
    public sealed class UdpCommChannel : ICommChannel
    {
        private readonly CommItem _cfg;
        private readonly object _gate = new object();
        private UdpClient _client;
        private IPEndPoint _remote;

        public UdpCommChannel(CommItem cfg) => _cfg = cfg;

        public string Name => _cfg.Name;

        public bool IsOpen => _client != null;

        public void Open()
        {
            lock (_gate)
            {
                if (IsOpen) return;

                string ip = (_cfg.PortOrIp ?? string.Empty).Trim();
                if (!IPAddress.TryParse(ip, out IPAddress addr))
                    throw new IOException($"UDP 地址不合法：{ip}（请填写 IP，如 192.168.1.10）。");
                int port = _cfg.BaudOrPort > 0 ? _cfg.BaudOrPort : 9000;

                _remote = new IPEndPoint(addr, port);
                _client = new UdpClient { Client = { ReceiveTimeout = _cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 1000 } };
            }
        }

        public void Send(string data)
        {
            Open();
            byte[] payload = PayloadCodec.Encode(data);
            if (payload.Length == 0) return;
            lock (_gate) _client.Send(payload, payload.Length, _remote);
        }

        public string Recv()
        {
            Open();
            try
            {
                IPEndPoint from = null;
                byte[] buf;
                lock (_gate) buf = _client.Receive(ref from);
                return PayloadCodec.Decode(buf, buf.Length);
            }
            catch (SocketException)
            {
                return string.Empty;   // 超时没收到，返回空串由脚本判断
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                try { _client?.Dispose(); } catch { }
                _client = null;
            }
        }
    }
}
