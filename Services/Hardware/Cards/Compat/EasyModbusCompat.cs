﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 本工程自行实现的 EasyModbus 兼容层（非厂商源码）。
//   用途: DigitalTwinCard（数字孪生卡）的 ModubsComm.cs 依赖 EasyModbus.ModbusClient，
//         但 NuGet 上 EasyModbus 4.4.0 包结构损坏（DLL 在包根目录，无法解析为引用），
//         且许可证为 CC BY-NC-ND 4.0（禁商用 / 禁演绎），故按实际用到的 API 重写。
//   位置: 命名空间刻意保持 EasyModbus，使移植过来的 ModubsComm.cs 可以零改动。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.IO;
using System.Net.Sockets;

namespace EasyModbus
{
    /// <summary>
    /// 与 EasyModbus.ModbusClient 用法兼容的 Modbus/TCP 客户端（本工程自行实现，非厂商源码）。
    /// <para>
    /// 移植 DigitalTwinCard（数字孪生卡）时，其 ModubsComm.cs 依赖 EasyModbus 的 ModbusClient。
    /// 而 NuGet 上的 EasyModbus 4.4.0 包结构损坏（DLL 位于包根目录而非 lib/&lt;tfm&gt;/，无法被解析为引用），
    /// 且其许可证为 CC BY-NC-ND 4.0（禁止商用、禁止演绎），不适合本产品使用。
    /// 故此处按 ModubsComm.cs 实际用到的那部分 API 重新实现一个等价客户端。
    /// </para>
    /// <para>
    /// 覆盖的成员：IPAddress / Port / UnitIdentifier / ConnectionTimeout、Connect / Disconnect、
    /// ReadCoils / ReadDiscreteInputs / ReadHoldingRegisters / ReadInputRegisters、
    /// WriteSingleCoil / WriteMultipleCoils / WriteMultipleRegisters、
    /// ConvertFloatToRegisters / ConvertRegistersToFloat。
    /// </para>
    /// <para>
    /// 浮点寄存器字序与 EasyModbus 保持一致：ConvertFloatToRegisters 返回 [高字, 低字]，
    /// 因此线上字节序为「低字在前」；ConvertRegistersToFloat 为其严格逆运算。
    /// </para>
    /// </summary>
    public class ModbusClient : IDisposable
    {
        // ── 连接参数（与 EasyModbus 同名同类型）──
        public string IPAddress { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 502;
        public int ConnectionTimeout { get; set; } = 2000;
        public byte UnitIdentifier { get; set; } = 1;

        /// <summary>是否已连接。</summary>
        public bool Connected => _client != null && _client.Connected;

        private TcpClient _client;
        private NetworkStream _stream;
        private readonly object _sync = new object();
        private ushort _transactionId;

        // ── 连接管理 ──

        public ModbusClient() { }

        public ModbusClient(string ipAddress, int port = 502)
        {
            IPAddress = ipAddress;
            Port = port;
        }

        /// <summary>按 IPAddress / Port 建立 TCP 连接。</summary>
        public void Connect()
        {
            lock (_sync)
            {
                CloseInternal();
                var client = new TcpClient
                {
                    NoDelay = true,
                    ReceiveTimeout = ConnectionTimeout,
                    SendTimeout = ConnectionTimeout
                };
                // TcpClient.Connect 没有超时参数，用异步等待实现可控超时。
                var ar = client.BeginConnect(IPAddress, Port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(ConnectionTimeout <= 0 ? 2000 : ConnectionTimeout))
                {
                    try { client.Close(); } catch { /* 关闭失败不影响超时结论 */ }
                    throw new TimeoutException($"连接 Modbus 服务器 {IPAddress}:{Port} 超时（{ConnectionTimeout} ms）");
                }
                client.EndConnect(ar);
                _client = client;
                _stream = client.GetStream();
            }
        }

        /// <summary>连接指定服务器（EasyModbus 同名重载）。</summary>
        public void Connect(string ipAddress, int port)
        {
            IPAddress = ipAddress;
            Port = port;
            Connect();
        }

        /// <summary>断开连接。</summary>
        public void Disconnect()
        {
            lock (_sync) CloseInternal();
        }

        private void CloseInternal()
        {
            try { _stream?.Dispose(); } catch { /* 忽略关闭异常 */ }
            try { _client?.Close(); } catch { /* 忽略关闭异常 */ }
            _stream = null;
            _client = null;
        }

        public void Dispose() => Disconnect();

        // ── 读操作 ──

        /// <summary>读线圈（功能码 0x01）。</summary>
        public bool[] ReadCoils(int startAddress, int quantity)
            => ReadBits(0x01, startAddress, quantity);

        /// <summary>读离散输入（功能码 0x02）。</summary>
        public bool[] ReadDiscreteInputs(int startAddress, int quantity)
            => ReadBits(0x02, startAddress, quantity);

        /// <summary>读保持寄存器（功能码 0x03）。</summary>
        public int[] ReadHoldingRegisters(int startAddress, int quantity)
            => ReadRegisters(0x03, startAddress, quantity);

        /// <summary>读输入寄存器（功能码 0x04）。</summary>
        public int[] ReadInputRegisters(int startAddress, int quantity)
            => ReadRegisters(0x04, startAddress, quantity);

        private bool[] ReadBits(byte functionCode, int startAddress, int quantity)
        {
            if (quantity <= 0 || quantity > 2000)
                throw new ArgumentOutOfRangeException(nameof(quantity), "位读取数量必须在 1~2000 之间");
            var pdu = new byte[5];
            pdu[0] = functionCode;
            WriteUInt16(pdu, 1, (ushort)startAddress);
            WriteUInt16(pdu, 3, (ushort)quantity);
            byte[] resp = Transact(pdu);
            // 响应: [功能码][字节数][数据...]
            int byteCount = resp[1];
            var result = new bool[quantity];
            for (int i = 0; i < quantity; i++)
                result[i] = (resp[2 + (i / 8)] & (1 << (i % 8))) != 0;
            return result;
        }

        private int[] ReadRegisters(byte functionCode, int startAddress, int quantity)
        {
            if (quantity <= 0 || quantity > 125)
                throw new ArgumentOutOfRangeException(nameof(quantity), "寄存器读取数量必须在 1~125 之间");
            var pdu = new byte[5];
            pdu[0] = functionCode;
            WriteUInt16(pdu, 1, (ushort)startAddress);
            WriteUInt16(pdu, 3, (ushort)quantity);
            byte[] resp = Transact(pdu);
            var result = new int[quantity];
            for (int i = 0; i < quantity; i++)
                result[i] = ReadUInt16(resp, 2 + i * 2);
            return result;
        }

        // ── 写操作 ──

        /// <summary>写单个线圈（功能码 0x05）。</summary>
        public void WriteSingleCoil(int address, bool value)
        {
            var pdu = new byte[5];
            pdu[0] = 0x05;
            WriteUInt16(pdu, 1, (ushort)address);
            WriteUInt16(pdu, 3, (ushort)(value ? 0xFF00 : 0x0000));
            Transact(pdu);
        }

        /// <summary>写多个线圈（功能码 0x0F）。</summary>
        public void WriteMultipleCoils(int startAddress, bool[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("写入数据不能为空", nameof(data));
            int byteCount = (data.Length + 7) / 8;
            var pdu = new byte[6 + byteCount];
            pdu[0] = 0x0F;
            WriteUInt16(pdu, 1, (ushort)startAddress);
            WriteUInt16(pdu, 3, (ushort)data.Length);
            pdu[5] = (byte)byteCount;
            for (int i = 0; i < data.Length; i++)
                if (data[i]) pdu[6 + (i / 8)] |= (byte)(1 << (i % 8));
            Transact(pdu);
        }

        /// <summary>写多个寄存器（功能码 0x10）。</summary>
        public void WriteMultipleRegisters(int startAddress, int[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("写入数据不能为空", nameof(data));
            if (data.Length > 123)
                throw new ArgumentOutOfRangeException(nameof(data), "寄存器写入数量不能超过 123");
            var pdu = new byte[6 + data.Length * 2];
            pdu[0] = 0x10;
            WriteUInt16(pdu, 1, (ushort)startAddress);
            WriteUInt16(pdu, 3, (ushort)data.Length);
            pdu[5] = (byte)(data.Length * 2);
            for (int i = 0; i < data.Length; i++)
                WriteUInt16(pdu, 6 + i * 2, (ushort)(data[i] & 0xFFFF));
            Transact(pdu);
        }

        // ── 浮点 ↔ 寄存器（与 EasyModbus 语义一致）──

        /// <summary>把 float 拆成两个寄存器值，返回 [高字, 低字]。</summary>
        public static int[] ConvertFloatToRegisters(float value)
        {
            byte[] b = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(b);   // 转成大端字节序
            return new[] { (b[0] << 8) | b[1], (b[2] << 8) | b[3] };
        }

        /// <summary>把 [高字, 低字] 还原为 float（ConvertFloatToRegisters 的逆运算）。</summary>
        public static float ConvertRegistersToFloat(int[] registers)
        {
            if (registers == null || registers.Length < 2)
                throw new ArgumentException("需要至少两个寄存器", nameof(registers));
            int high = registers[0] & 0xFFFF;
            int low = registers[1] & 0xFFFF;
            byte[] b = { (byte)(high >> 8), (byte)high, (byte)(low >> 8), (byte)low };
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            return BitConverter.ToSingle(b, 0);
        }

        /// <summary>从指定下标开始还原 float。</summary>
        public static float ConvertRegistersToFloat(int[] registers, int index)
        {
            if (registers == null || registers.Length < index + 2)
                throw new ArgumentException("寄存器数组长度不足", nameof(registers));
            return ConvertRegistersToFloat(new[] { registers[index], registers[index + 1] });
        }

        /// <summary>把多个 float 拆成寄存器数组。</summary>
        public static int[] ConvertFloatToRegisters(float[] values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var result = new int[values.Length * 2];
            for (int i = 0; i < values.Length; i++)
            {
                var pair = ConvertFloatToRegisters(values[i]);
                result[i * 2] = pair[0];
                result[i * 2 + 1] = pair[1];
            }
            return result;
        }

        // ── 报文收发 ──

        /// <summary>发送 PDU 并返回响应 PDU（不含 MBAP 头）。</summary>
        private byte[] Transact(byte[] pdu)
        {
            lock (_sync)
            {
                if (_client == null || _stream == null || !_client.Connected)
                    throw new InvalidOperationException("Modbus 未连接，请先调用 Connect()");

                ushort txId = ++_transactionId;
                // MBAP: 事务号(2) 协议号(2)=0 长度(2)=单元号+PDU长度 单元号(1)
                var frame = new byte[7 + pdu.Length];
                WriteUInt16(frame, 0, txId);
                WriteUInt16(frame, 2, 0);
                WriteUInt16(frame, 4, (ushort)(pdu.Length + 1));
                frame[6] = UnitIdentifier;
                Buffer.BlockCopy(pdu, 0, frame, 7, pdu.Length);

                _stream.Write(frame, 0, frame.Length);
                _stream.Flush();

                // MBAP 头 7 字节：事务号(2) 协议号(2) 长度(2) 单元号(1)
                // 长度字段 = 单元号(1) + PDU 长度，故 PDU 长度 = 长度 - 1
                byte[] header = ReadExactly(7);
                int length = ReadUInt16(header, 4);
                if (length < 2 || length > 260)
                    throw new IOException($"Modbus 响应长度非法：{length}");

                byte[] body = ReadExactly(length - 1);   // body 即响应 PDU

                byte function = body[0];
                if ((function & 0x80) != 0)
                {
                    byte code = body.Length > 1 ? body[1] : (byte)0;
                    throw new IOException($"Modbus 异常响应：功能码 0x{pdu[0]:X2}，异常码 {code}（{DescribeException(code)}）");
                }
                return body;
            }
        }

        private static string DescribeException(byte code) => code switch
        {
            1 => "非法功能码",
            2 => "非法数据地址",
            3 => "非法数据值",
            4 => "从站设备故障",
            5 => "确认（从站正在处理）",
            6 => "从站设备忙",
            8 => "存储奇偶校验错",
            10 => "网关路径不可用",
            11 => "网关目标设备响应失败",
            _ => "未知异常"
        };

        /// <summary>从流中精确读取 count 个字节。</summary>
        private byte[] ReadExactly(int count)
        {
            var buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int n = _stream.Read(buffer, offset, count - offset);
                if (n <= 0) throw new IOException("Modbus 连接已被对端关闭");
                offset += n;
            }
            return buffer;
        }

        private static void WriteUInt16(byte[] buffer, int offset, ushort value)
        {
            buffer[offset] = (byte)(value >> 8);        // Modbus 大端
            buffer[offset + 1] = (byte)(value & 0xFF);
        }

        private static int ReadUInt16(byte[] buffer, int offset)
            => (buffer[offset] << 8) | buffer[offset + 1];
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
