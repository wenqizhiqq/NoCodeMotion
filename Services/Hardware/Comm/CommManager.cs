#nullable disable
using System;
using System.Collections.Concurrent;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Hardware.Comm
{
    /// <summary>
    /// 通讯通道管理器：按项目里的「通讯」配置按需创建真实通道，并按名称缓存复用
    /// （同一个 COM 口 / IP 不会被重复打开）。<see cref="LeadshineHardwareBridge"/> 的
    /// CommSend / CommRecv 都委托到这里。
    ///
    /// 支持的 CommType（来自 CommItem 注释）：
    ///   串口              → SerialCommChannel
    ///   网口TCP / 相机网口 → TcpCommChannel
    ///   网口UDP           → UdpCommChannel
    ///   ModbusRTU         → ModbusCommChannel（串口）
    ///   ModbusTCP         → ModbusCommChannel（网口）
    ///   西门子S7 / 三菱MC → 暂未内置专用协议，给出中文提示（可后续扩展）
    /// </summary>
    public sealed class CommManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, ICommChannel> _channels =
            new ConcurrentDictionary<string, ICommChannel>(StringComparer.Ordinal);

        /// <summary>对接日志（打到 Lua 输出面板）。</summary>
        public Action<string> Log { get; set; }

        /// <summary>发送数据。</summary>
        public void Send(CommItem cfg, string data)
        {
            var ch = GetOrCreate(cfg);
            ch.Send(data);
        }

        /// <summary>接收数据。</summary>
        public string Recv(CommItem cfg)
        {
            var ch = GetOrCreate(cfg);
            return ch.Recv();
        }

        /// <summary>获取或创建通道（按名称 + 类型指纹缓存，配置变了会重建）。</summary>
        private ICommChannel GetOrCreate(CommItem cfg)
        {
            string key = ChannelKey(cfg);
            return _channels.GetOrAdd(key, _ =>
            {
                var ch = Create(cfg);
                Log?.Invoke($"[通讯] 打开通道 {cfg.Name}（{cfg.CommType} → {cfg.PortOrIp}:{cfg.BaudOrPort}）");
                return ch;
            });
        }

        private static string ChannelKey(CommItem cfg) =>
            $"{cfg.Name}|{cfg.CommType}|{cfg.PortOrIp}|{cfg.BaudOrPort}|{cfg.DataBits}|{cfg.Parity}|{cfg.StopBits}";

        private static ICommChannel Create(CommItem cfg)
        {
            string type = (cfg.CommType ?? string.Empty).Trim();

            if (Contains(type, "ModbusRTU", "Modbus RTU", "MODBUSRTU"))
                return new ModbusCommChannel(cfg, isTcp: false);
            if (Contains(type, "ModbusTCP", "Modbus TCP", "MODBUSTCP"))
                return new ModbusCommChannel(cfg, isTcp: true);
            if (Contains(type, "串口", "RS232", "RS485", "COM", "Serial"))
                return new SerialCommChannel(cfg);
            if (Contains(type, "UDP"))
                return new UdpCommChannel(cfg);
            if (Contains(type, "TCP", "网口", "相机", "以太网", "Ethernet", "Socket"))
                return new TcpCommChannel(cfg);
            if (Contains(type, "西门子", "S7"))
                throw new NotSupportedException($"通讯类型「{type}」（西门子 S7）暂未内置协议。可先用 ModbusTCP 对接，或在 CommManager 里扩展 S7 通道。");
            if (Contains(type, "三菱", "MC"))
                throw new NotSupportedException($"通讯类型「{type}」（三菱 MC）暂未内置协议。可先用 ModbusTCP 对接，或在 CommManager 里扩展 MC 通道。");

            // 默认按 TCP 处理（大多数以太网设备），保证不因类型拼写差异直接失败
            return new TcpCommChannel(cfg);
        }

        private static bool Contains(string src, params string[] keys)
        {
            foreach (var k in keys)
                if (src.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        /// <summary>关闭并清空所有已打开的通道。</summary>
        public void CloseAll()
        {
            foreach (var kv in _channels)
            {
                try { kv.Value.Dispose(); } catch { }
            }
            _channels.Clear();
        }

        public void Dispose() => CloseAll();
    }
}
