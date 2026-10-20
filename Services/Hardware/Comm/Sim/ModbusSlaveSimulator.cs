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
    /// Modbus/TCP 从站对端（仿真「PLC / 从站设备」）。
    /// <para>自己按 MBAP + PDU 报文实现常用功能码（1/2/3/4/5/6/15/16），数据区在内存里：
    /// 线圈、离散输入、保持寄存器、输入寄存器各一段。本端 <c>ModbusCommChannel</c> 写进去的值，
    /// 再从仿真从站读回来 —— 整条 Modbus 链路是真的，只是从站换成了软件自己。</para>
    /// <para>★ 不用 NModbus 的从站 API：它各版本入口不一样（CreateSlaveNetwork 的重载在 3.x 里反复变动），
    /// 而 MBAP 报文本身很简单，自己解析反而稳、并且能顺手把每一笔读写打进日志（现场最需要这个）。</para>
    /// </summary>
    public sealed class ModbusTcpSlaveSimulator : CommSimBase
    {
        private const int Coils = 2048;
        private const int Registers = 2048;

        private readonly bool[] _coils = new bool[Coils];
        private readonly bool[] _discrete = new bool[Coils];
        private readonly ushort[] _holding = new ushort[Registers];
        private readonly ushort[] _input = new ushort[Registers];

        private TcpListener _listener;
        private Thread _accept;
        private volatile bool _on;

        public ModbusTcpSlaveSimulator(CommItem cfg) : base(cfg) { }

        private int Port => Cfg.BaudOrPort > 0 ? Cfg.BaudOrPort : 502;

        public override string Endpoint => $"本机 Modbus 从站 0.0.0.0:{Port}（站号 {Station}），本端连 {Cfg.PortOrIp}:{Port}";

        public override string Note => "数据全放在内存里：线圈、离散输入、保持寄存器、输入寄存器各 2048 个位置，一开始都是 0。"
                                       + "本端写进去的值能立刻读回；「离散输入 / 输入寄存器」是只读区，要造初值就手工注入或改本端写入。";

        /// <summary>仿真的从站地址：借用「数据位」字段（Modbus 类型下它没有串口含义）。</summary>
        private byte Station => (byte)(Cfg.DataBits > 0 && Cfg.DataBits <= 247 ? Cfg.DataBits : 1);

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
                throw new IOException($"仿真 Modbus 从站监听端口 {Port} 失败：{ex.Message}"
                                      + "（502 等低位端口可能被占用，或需要管理员权限）");
            }
            SetRunning(true);
            _on = true;
            _accept = new Thread(AcceptLoop) { IsBackground = true, Name = "CommSimModbus-" + Cfg.Name };
            _accept.Start();
            Log($"[仿真] 「{Cfg.Name}」Modbus 从站已启动：0.0.0.0:{Port}，站号 {Station}");
        }

        private void AcceptLoop()
        {
            while (_on)
            {
                TcpClient cli;
                try { cli = _listener.AcceptTcpClient(); } catch { break; }
                Log($"[仿真] Modbus 主站已连入：{(cli.Client.RemoteEndPoint ?? (object)"?")}");
                new Thread(() => Serve(cli)) { IsBackground = true }.Start();
            }
            SetRunning(false);
        }

        private void Serve(TcpClient cli)
        {
            try
            {
                var ns = cli.GetStream();
                var head = new byte[7];
                while (_on && cli.Connected)
                {
                    if (!ReadExact(ns, head, 7)) break;
                    int tid = (head[0] << 8) | head[1];
                    int len = (head[4] << 8) | head[5];
                    byte unit = head[6];
                    if (len < 2 || len > 300) break;
                    var pdu = new byte[len - 1];
                    if (!ReadExact(ns, pdu, pdu.Length)) break;

                    byte fc = pdu[0];
                    byte[] resp = Handle(fc, pdu);
                    if (resp == null) continue;                       // 该功能码不支持：不回，让主站超时

                    // MBAP = 事务号(2) + 协议号(2) + 后续字节数(2) + 站号(1)，共 7 字节；
                    // ★ 站号只在头里出现一次，后面直接跟 PDU —— 再塞一遍主站读到的第一个字节就成了站号，整条应答错位。
                    var mbap = new byte[7];
                    mbap[0] = (byte)(tid >> 8); mbap[1] = (byte)tid;
                    mbap[4] = (byte)((resp.Length + 1) >> 8); mbap[5] = (byte)(resp.Length + 1);
                    mbap[6] = unit;
                    ns.Write(mbap, 0, mbap.Length);
                    ns.Write(resp, 0, resp.Length);
                    ns.Flush();
                }
            }
            catch (Exception ex) { if (_on) Log($"[仿真] Modbus 从站连接异常：{ex.Message}"); }
            finally { try { cli.Dispose(); } catch { } }
        }

        private static bool ReadExact(NetworkStream ns, byte[] buf, int count)
        {
            int got = 0;
            while (got < count)
            {
                int n;
                try { n = ns.Read(buf, got, count - got); } catch { return false; }
                if (n <= 0) return false;
                got += n;
            }
            return true;
        }

        /// <summary>处理一条 PDU，返回要回的 PDU（含功能码）；不支持的返回 null。</summary>
        private byte[] Handle(byte fc, byte[] p)
        {
            int a = (p[1] << 8) | p[2];
            int n = (p[3] << 8) | p[4];
            // 地址 / 数量越界：按协议回 0x02「非法数据地址」。
            // 绝不能抛出去 —— 抛出去整个连接就断了，主站看到的是「设备掉线」，现场极难查。
            int limit = (fc == 1 || fc == 2 || fc == 5 || fc == 15) ? Coils : Registers;
            // FC5（写单个线圈）/ FC6（写单个寄存器）里 p[3..4] 是要写的「值」，不是数量 —— 不能按数量判越界
            bool countCheck = fc != 5 && fc != 6;
            if (a < 0 || a > limit || (countCheck && (n <= 0 || n > 2000 || a + n > limit)))
            {
                Log($"[仿真] Modbus FC{fc} 地址越界（起始 {a}，数量 {n}，上限 {limit}）→ 回非法数据地址");
                return new byte[] { (byte)(fc | 0x80), 0x02 };
            }
            switch (fc)
            {
                case 1:     // 读线圈
                case 2:     // 读离散输入
                {
                    bool[] src = fc == 1 ? _coils : _discrete;
                    lock (src)
                    {
                        var bytes = new byte[(n + 7) / 8];
                        for (int i = 0; i < n; i++)
                            if (GetBit(src, a + i)) bytes[i / 8] |= (byte)(1 << (i % 8));
                        Log($"[仿真] Modbus FC{fc} 读 {a}..{a + n - 1} → {Hex(bytes)}");
                        return Pack(fc, bytes);
                    }
                }
                case 3:     // 读保持寄存器
                case 4:     // 读输入寄存器
                {
                    ushort[] src = fc == 3 ? _holding : _input;
                    lock (src)
                    {
                        var bytes = new byte[n * 2];
                        for (int i = 0; i < n; i++)
                        {
                            ushort v = a + i < src.Length ? src[a + i] : (ushort)0;
                            bytes[i * 2] = (byte)(v >> 8);
                            bytes[i * 2 + 1] = (byte)(v & 0xFF);
                        }
                        Log($"[仿真] Modbus FC{fc} 读 {a}..{a + n - 1} → {Hex(bytes)}");
                        return Pack(fc, bytes);
                    }
                }
                case 5:     // 写单个线圈
                {
                    bool on = ((p[3] << 8) | p[4]) == 0xFF00;
                    lock (_coils) _coils[a] = on;
                    Log($"[仿真] Modbus FC5 写线圈 {a} = {(on ? "ON" : "OFF")}");
                    return Slice(p, 0, 5);
                }
                case 6:     // 写单个保持寄存器
                {
                    ushort v = (ushort)((p[3] << 8) | p[4]);
                    lock (_holding) _holding[a] = v;
                    Log($"[仿真] Modbus FC6 写保持寄存器 {a} = {v}");
                    return Slice(p, 0, 5);
                }
                case 15:    // 写多个线圈
                {
                    lock (_coils)
                        for (int i = 0; i < n; i++) _coils[a + i] = (p[5 + i / 8] & (1 << (i % 8))) != 0;
                    Log($"[仿真] Modbus FC15 写线圈 {a}..{a + n - 1}");
                    return new byte[] { 15, (byte)(a >> 8), (byte)a, (byte)(n >> 8), (byte)n };
                }
                case 16:    // 写多个保持寄存器
                {
                    lock (_holding)
                        for (int i = 0; i < n; i++)
                            _holding[a + i] = (ushort)((p[6 + i * 2] << 8) | p[7 + i * 2]);
                    Log($"[仿真] Modbus FC16 写保持寄存器 {a}..{a + n - 1}");
                    return new byte[] { 16, (byte)(a >> 8), (byte)a, (byte)(n >> 8), (byte)n };
                }
                default:
                    Log($"[仿真] Modbus 从站不支持功能码 FC{fc}（已回错误码 0x01）");
                    return new byte[] { (byte)(fc | 0x80), 0x01 };
            }
        }

        private static bool GetBit(bool[] src, int index) => index >= 0 && index < src.Length && src[index];

        private static byte[] Pack(byte fc, byte[] data)
        {
            var resp = new byte[data.Length + 2];
            resp[0] = fc;
            resp[1] = (byte)data.Length;
            Array.Copy(data, 0, resp, 2, data.Length);
            return resp;
        }

        private static byte[] Slice(byte[] p, int from, int len)
        {
            var r = new byte[len];
            Array.Copy(p, from, r, 0, len);
            return r;
        }

        private static string Hex(byte[] b)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < b.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(b[i].ToString("X2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        public override void Stop()
        {
            _on = false;
            try { _listener?.Stop(); } catch { }
            _listener = null;
            SetRunning(false);
        }

        /// <summary>注入 = 改仿真从站的数据区（格式：<c>40001=123</c> 保持寄存器 / <c>00001=1</c> 线圈）。</summary>
        public override bool Inject(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            int eq = text.IndexOf('=');
            if (eq <= 0) return false;
            string addr = text.Substring(0, eq).Trim();
            string val = text.Substring(eq + 1).Trim();

            if (addr.StartsWith("4"))
            {
                if (!ushort.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort v)) return false;
                int i = ParseAddr(addr, 40001);
                lock (_holding) _holding[i] = v;
                Log($"[仿真] 注入保持寄存器 {addr} = {v}");
                return true;
            }
            if (addr.StartsWith("3"))
            {
                if (!ushort.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort v)) return false;
                int i = ParseAddr(addr, 30001);
                lock (_input) _input[i] = v;
                Log($"[仿真] 注入输入寄存器 {addr} = {v}");
                return true;
            }
            bool on = val == "1" || val.Equals("ON", StringComparison.OrdinalIgnoreCase)
                   || val.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (addr.StartsWith("1"))
            {
                int i = ParseAddr(addr, 1);
                lock (_coils) _coils[i] = on;
                Log($"[仿真] 注入线圈 {addr} = {(on ? "ON" : "OFF")}");
                return true;
            }
            if (addr.StartsWith("0"))
            {
                int i = ParseAddr(addr, 1);
                lock (_discrete) _discrete[i] = on;
                Log($"[仿真] 注入离散输入 {addr} = {(on ? "ON" : "OFF")}");
                return true;
            }
            return false;
        }

        private static int ParseAddr(string addr, int base1)
        {
            if (!int.TryParse(addr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) return 0;
            int i = v - base1;
            if (i < 0) i = 0;
            return i >= Registers ? 0 : i;
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
