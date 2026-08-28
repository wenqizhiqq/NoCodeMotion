// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using NModbus;
using NModbus.Serial;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Hardware.Comm
{
    /// <summary>
    /// 真实 Modbus 通道（基于 NModbus 库）：
    ///   - CommType = ModbusRTU → 走串口（PortOrIp = COM 口，BaudOrPort = 波特率）
    ///   - CommType = ModbusTCP → 走网口（PortOrIp = IP，BaudOrPort = 端口，PLC 一般是 502）
    ///
    /// Lua 里用「命令字符串」操作寄存器，先 CommSend 下命令，读操作再用 CommRecv 取结果：
    /// <code>
    /// -- 读保持寄存器：站号 1，地址 0，读 2 个
    /// CommSend("PLC1", "RH,1,0,2")
    /// local v = CommRecv("PLC1")        -- 例如 "100,200"
    ///
    /// -- 写单个保持寄存器：站号 1，地址 10，值 1234
    /// CommSend("PLC1", "WH,1,10,1234")
    ///
    /// -- 写线圈：站号 1，地址 5，置 1
    /// CommSend("PLC1", "WC,1,5,1")
    ///
    /// -- 读离散输入 4 个：站号 1，地址 0
    /// CommSend("PLC1", "RD,1,0,4")
    /// local s = CommRecv("PLC1")        -- 例如 "1,0,0,1"
    /// </code>
    ///
    /// 命令一览（大小写不敏感）：
    ///   RH = 读保持寄存器(03)  RI = 读输入寄存器(04)  RC = 读线圈(01)  RD = 读离散输入(02)
    ///   WH = 写保持寄存器(06/16)  WC = 写线圈(05/15)
    /// </summary>
    public sealed class ModbusCommChannel : ICommChannel
    {
        private readonly CommItem _cfg;
        private readonly bool _isTcp;
        private readonly object _gate = new object();
        private readonly ConcurrentQueue<string> _readResults = new ConcurrentQueue<string>();

        private IModbusMaster _master;
        private SerialPort _port;
        private TcpClient _tcp;

        public ModbusCommChannel(CommItem cfg, bool isTcp)
        {
            _cfg = cfg;
            _isTcp = isTcp;
        }

        public string Name => _cfg.Name;

        public bool IsOpen => _master != null;

        public void Open()
        {
            lock (_gate)
            {
                if (IsOpen) return;
                var factory = new ModbusFactory();
                int timeout = _cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 1000;

                if (_isTcp)
                {
                    string ip = (_cfg.PortOrIp ?? string.Empty).Trim();
                    if (ip.Length == 0)
                        throw new IOException("ModbusTCP 地址为空：请填写 PLC 的 IP（如 192.168.1.10）。");
                    int port = _cfg.BaudOrPort > 0 ? _cfg.BaudOrPort : 502;

                    var tcp = new TcpClient { NoDelay = true, ReceiveTimeout = timeout, SendTimeout = timeout };
                    try
                    {
                        if (!tcp.ConnectAsync(ip, port).Wait(timeout))
                        {
                            tcp.Dispose();
                            throw new IOException($"连接 ModbusTCP {ip}:{port} 超时（{timeout}ms）：检查 IP 是否同网段、PLC 是否开启 Modbus 服务。");
                        }
                    }
                    catch (AggregateException ex)
                    {
                        tcp.Dispose();
                        throw new IOException($"连接 ModbusTCP {ip}:{port} 失败：{ex.InnerException?.Message ?? ex.Message}");
                    }

                    _tcp = tcp;
                    _master = factory.CreateMaster(tcp);
                }
                else
                {
                    var port = SerialSettings.Create(_cfg);
                    try { port.Open(); }
                    catch (UnauthorizedAccessException)
                    {
                        port.Dispose();
                        throw new IOException($"ModbusRTU 串口 {_cfg.PortOrIp} 已被其它程序占用。");
                    }
                    catch (Exception ex)
                    {
                        port.Dispose();
                        throw new IOException($"ModbusRTU 串口 {_cfg.PortOrIp} 打开失败：{ex.Message}");
                    }

                    _port = port;
                    _master = factory.CreateRtuMaster(new SerialPortAdapter(port));
                }

                _master.Transport.ReadTimeout = timeout;
                _master.Transport.WriteTimeout = timeout;
                _master.Transport.Retries = 1;
            }
        }

        public void Send(string command)
        {
            Open();
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Modbus 命令为空。格式示例：RH,1,0,2（读保持寄存器）或 WH,1,10,1234（写保持寄存器）。");

            var parts = command.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4)
                throw new ArgumentException($"Modbus 命令格式不正确：{command}。正确格式：功能,站号,地址,数量/值（如 RH,1,0,2 或 WH,1,10,1234）。");

            string func = parts[0].ToUpperInvariant();
            byte slave = ParseByte(parts[1], "站号");
            ushort addr = ParseUShort(parts[2], "地址");

            lock (_gate)
            {
                try
                {
                    switch (func)
                    {
                        case "RH":   // 读保持寄存器 03
                            Enqueue(_master.ReadHoldingRegisters(slave, addr, ParseUShort(parts[3], "数量")));
                            break;
                        case "RI":   // 读输入寄存器 04
                            Enqueue(_master.ReadInputRegisters(slave, addr, ParseUShort(parts[3], "数量")));
                            break;
                        case "RC":   // 读线圈 01
                            Enqueue(_master.ReadCoils(slave, addr, ParseUShort(parts[3], "数量")));
                            break;
                        case "RD":   // 读离散输入 02
                            Enqueue(_master.ReadInputs(slave, addr, ParseUShort(parts[3], "数量")));
                            break;

                        case "WH":   // 写保持寄存器 06 / 16
                        {
                            ushort[] values = parts.Skip(3).Select(p => ParseUShort(p, "写入值")).ToArray();
                            if (values.Length == 1) _master.WriteSingleRegister(slave, addr, values[0]);
                            else _master.WriteMultipleRegisters(slave, addr, values);
                            break;
                        }
                        case "WC":   // 写线圈 05 / 15
                        {
                            bool[] states = parts.Skip(3).Select(p => ParseBool(p)).ToArray();
                            if (states.Length == 1) _master.WriteSingleCoil(slave, addr, states[0]);
                            else _master.WriteMultipleCoils(slave, addr, states);
                            break;
                        }
                        default:
                            throw new ArgumentException($"不认识的 Modbus 命令「{func}」。可用：RH 读保持寄存器、RI 读输入寄存器、RC 读线圈、RD 读离散输入、WH 写保持寄存器、WC 写线圈。");
                    }
                }
                catch (SlaveException ex)
                {
                    throw new IOException($"Modbus 从站返回异常码 {ex.SlaveExceptionCode}：{DescribeSlaveError(ex.SlaveExceptionCode)}（命令 {command}）");
                }
                catch (TimeoutException)
                {
                    Dispose();   // 断开后下次自动重连
                    throw new IOException($"Modbus 通讯超时（{_cfg.TimeoutMs}ms）：检查站号、接线（A/B 是否反）、波特率与校验位是否与从站一致。命令 {command}");
                }
                catch (InvalidOperationException ex)
                {
                    Dispose();
                    throw new IOException($"Modbus 连接已失效：{ex.Message}（下次调用会自动重连）");
                }
            }
        }

        public string Recv()
        {
            return _readResults.TryDequeue(out string result) ? result : string.Empty;
        }

        public void Dispose()
        {
            lock (_gate)
            {
                try { _master?.Dispose(); } catch { }
                try { _port?.Dispose(); } catch { }
                try { _tcp?.Dispose(); } catch { }
                _master = null;
                _port = null;
                _tcp = null;
            }
        }

        // ===================== 辅助 =====================

        private void Enqueue(ushort[] values) =>
            _readResults.Enqueue(string.Join(",", values.Select(v => v.ToString(CultureInfo.InvariantCulture))));

        private void Enqueue(bool[] values) =>
            _readResults.Enqueue(string.Join(",", values.Select(v => v ? "1" : "0")));

        private static byte ParseByte(string s, string what)
        {
            if (!byte.TryParse(s, out byte v))
                throw new ArgumentException($"Modbus {what}「{s}」不是有效数字（0-255）。");
            return v;
        }

        private static ushort ParseUShort(string s, string what)
        {
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
                ushort.TryParse(s.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort hv))
                return hv;
            if (!ushort.TryParse(s, out ushort v))
                throw new ArgumentException($"Modbus {what}「{s}」不是有效数字（0-65535）。");
            return v;
        }

        private static bool ParseBool(string s)
        {
            string t = s.Trim();
            if (t == "1" || t.Equals("true", StringComparison.OrdinalIgnoreCase) || t == "ON" || t == "on") return true;
            if (t == "0" || t.Equals("false", StringComparison.OrdinalIgnoreCase) || t == "OFF" || t == "off") return false;
            throw new ArgumentException($"线圈状态「{s}」不合法，请填 1 / 0。");
        }

        private static string DescribeSlaveError(byte code)
        {
            switch (code)
            {
                case 1: return "从站不支持该功能码";
                case 2: return "寄存器 / 线圈地址超出从站范围";
                case 3: return "数据值不在允许范围";
                case 4: return "从站执行时发生故障";
                case 5: return "从站已收到请求，正在处理（需稍后重读）";
                case 6: return "从站忙，请稍后重试";
                default: return "请查阅从站设备手册";
            }
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
