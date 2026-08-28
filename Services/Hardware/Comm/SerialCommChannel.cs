// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Hardware.Comm
{
    /// <summary>
    /// 真实串口通道（System.IO.Ports.SerialPort，RS232 / RS485 / USB 转串口都走这里）。
    ///
    /// 对应配置：CommType = 串口，PortOrIp = COM3，BaudOrPort = 波特率，
    /// DataBits / Parity / StopBits / TimeoutMs 按配置下发。
    /// </summary>
    public sealed class SerialCommChannel : ICommChannel
    {
        private readonly CommItem _cfg;
        private readonly object _gate = new object();
        private SerialPort _port;

        public SerialCommChannel(CommItem cfg)
        {
            _cfg = cfg;
        }

        public string Name => _cfg.Name;

        public bool IsOpen => _port != null && _port.IsOpen;

        public void Open()
        {
            lock (_gate)
            {
                if (IsOpen) return;

                string portName = SerialSettings.NormalizePortName(_cfg.PortOrIp);
                int timeout = _cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 500;

                _port = new SerialPort(portName,
                    _cfg.BaudOrPort > 0 ? _cfg.BaudOrPort : 9600,
                    SerialSettings.ParseParity(_cfg.Parity),
                    _cfg.DataBits > 0 ? _cfg.DataBits : 8,
                    SerialSettings.ParseStopBits(_cfg.StopBits))
                {
                    ReadTimeout = timeout,
                    WriteTimeout = timeout,
                    Handshake = Handshake.None,
                    DtrEnable = false,
                    RtsEnable = false
                };

                try
                {
                    _port.Open();
                    _port.DiscardInBuffer();
                    _port.DiscardOutBuffer();
                }
                catch (UnauthorizedAccessException)
                {
                    _port = null;
                    throw new IOException($"串口 {portName} 打开失败：已被其它程序占用（请关闭调试助手 / 另一个上位机）。");
                }
                catch (IOException ex)
                {
                    _port = null;
                    throw new IOException($"串口 {portName} 打开失败：{ex.Message}（请确认串口号是否存在、USB 转串口驱动是否安装）。");
                }
                catch (ArgumentException ex)
                {
                    _port = null;
                    throw new IOException($"串口参数不正确：{ex.Message}（检查串口号 / 波特率 / 数据位 / 校验 / 停止位配置）。");
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
                try { _port.Write(payload, 0, payload.Length); }
                catch (TimeoutException)
                {
                    throw new IOException($"串口 {_cfg.PortOrIp} 发送超时（{_cfg.TimeoutMs}ms）：对方可能没有接收或 RS485 方向控制异常。");
                }
            }
        }

        public string Recv()
        {
            Open();
            int timeout = _cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 500;
            var buffer = new byte[4096];
            int total = 0;
            int idleMs = 0;
            int waited = 0;

            while (waited < timeout && total < buffer.Length)
            {
                int available;
                lock (_gate) { available = _port.BytesToRead; }

                if (available > 0)
                {
                    lock (_gate)
                    {
                        int read = _port.Read(buffer, total, Math.Min(available, buffer.Length - total));
                        total += read;
                    }
                    idleMs = 0;
                    // 收到数据后再等一小段，凑齐一帧（工业设备常分包到达）
                    Thread.Sleep(5);
                    waited += 5;
                    continue;
                }

                if (total > 0)
                {
                    idleMs += 5;
                    if (idleMs >= 20) break;   // 静默 20ms 视为一帧结束
                }

                Thread.Sleep(5);
                waited += 5;
            }

            return PayloadCodec.Decode(buffer, total);
        }

        public void Dispose()
        {
            lock (_gate)
            {
                try
                {
                    if (_port != null)
                    {
                        if (_port.IsOpen) _port.Close();
                        _port.Dispose();
                    }
                }
                catch { /* 释放异常忽略 */ }
                _port = null;
            }
        }
    }

    /// <summary>串口参数解析（串口通道与 Modbus RTU 通道共用）。</summary>
    internal static class SerialSettings
    {
        /// <summary>把 "com3" / "COM3" / " 3 " 统一成 "COM3"。</summary>
        public static string NormalizePortName(string raw)
        {
            string s = (raw ?? string.Empty).Trim();
            if (s.Length == 0) throw new IOException("串口号为空：请在通讯配置里填写 COM 口（如 COM3）。");
            if (int.TryParse(s, out int n)) return "COM" + n;
            return s.ToUpperInvariant();
        }

        /// <summary>"无/None" "奇/Odd" "偶/Even" "Mark" "Space" → Parity。</summary>
        public static Parity ParseParity(string raw)
        {
            string s = (raw ?? string.Empty).Trim();
            if (s.Length == 0) return Parity.None;
            if (s.Contains("奇") || s.StartsWith("O", StringComparison.OrdinalIgnoreCase)) return Parity.Odd;
            if (s.Contains("偶") || s.StartsWith("E", StringComparison.OrdinalIgnoreCase)) return Parity.Even;
            if (s.Contains("标记") || s.StartsWith("M", StringComparison.OrdinalIgnoreCase)) return Parity.Mark;
            if (s.Contains("空格") || s.StartsWith("Sp", StringComparison.OrdinalIgnoreCase)) return Parity.Space;
            return Parity.None;
        }

        /// <summary>1 / 1.5 / 2 → StopBits。</summary>
        public static StopBits ParseStopBits(double raw)
        {
            if (Math.Abs(raw - 1.5) < 0.01) return StopBits.OnePointFive;
            if (raw >= 2) return StopBits.Two;
            return StopBits.One;
        }

        /// <summary>按配置构造一个未打开的 SerialPort（Modbus RTU 复用）。</summary>
        public static SerialPort Create(CommItem cfg)
        {
            int timeout = cfg.TimeoutMs > 0 ? cfg.TimeoutMs : 500;
            return new SerialPort(NormalizePortName(cfg.PortOrIp),
                cfg.BaudOrPort > 0 ? cfg.BaudOrPort : 9600,
                ParseParity(cfg.Parity),
                cfg.DataBits > 0 ? cfg.DataBits : 8,
                ParseStopBits(cfg.StopBits))
            {
                ReadTimeout = timeout,
                WriteTimeout = timeout
            };
        }
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
