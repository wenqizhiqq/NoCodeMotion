// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁣启‌志⁣◆‍编⁣写⁠◇⁠微‌信‌﹕​1​8‍7⁠◆‍1‎9⁠3‏6​◇‌1‌3​9​9‎　​※‍保​留‎所⁣有‎权‎利‎请‎勿‍删‏除‌◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Concurrent;
using NoCodeMotion.Models;
using NoCodeMotion.Services.Hardware.Comm.Sim;

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
    ///   SECS(HSMS)        → SecsCommChannel（半导体 SECS/GEM：SEMI E37 会话 + E5 报文）
    ///   管道              → PipeCommChannel（本机命名管道，两端一进一出）
    ///   共享内存          → SharedMemoryCommChannel（本机命名内存映射区，两条环形队列）
    ///   共享文件夹        → DropFolderCommChannel（目录投递：out\ 发、in\ 收，可指网络共享）
    ///   本机文件          → LocalFileCommChannel（一个磁盘文件当收发口）
    ///   Web               → HttpCommChannel（HTTP/HTTPS：POST/PUT 发、GET 收）
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

        /// <summary>
        /// 发送并取回「立即应答」的文本：
        ///   SECS 通道 → W-bit 报文配到的应答（如 S1F1 的 S1F2），不需要应答 / 超时返回 null；
        ///   其它通道 → 按普通发送处理，返回 null。
        /// 页面与 Lua 都用它把应答显示出来（普通 Send 是 void，拿不到应答）。
        /// </summary>
        public string SendAndReply(CommItem cfg, string data)
        {
            var ch = GetOrCreate(cfg);
            if (ch is SecsCommChannel secs) return secs.SendAndReply(data);
            if (ch is HttpCommChannel http) return http.SendAndReply(data);   // Web：把响应正文回给页面/Lua
            ch.Send(data);
            return null;
        }

        /// <summary>取通道（不存在则按配置创建，但不会 Open）。页面用它读 IsOpen / 监听端口 / 最近错误。</summary>
        public ICommChannel Peek(CommItem cfg) => cfg == null ? null : GetOrCreate(cfg);

        /// <summary>
        /// 取「已存在」的通道，不创建。★ 页面刷新状态必须用它：
        ///   Peek 会真的去 Create，而「西门子S7 / 三菱MC」的 Create 是直接抛 NotSupportedException 的，
        ///   拿 Peek 刷状态会让「选中一条 S7 配置」当场抛异常。
        /// </summary>
        public ICommChannel TryPeek(CommItem cfg)
        {
            if (cfg == null) return null;
            _channels.TryGetValue(ChannelKey(cfg), out var ch);
            return ch;
        }

        /// <summary>关闭并移除某条配置对应的通道（页面点「关闭连接」，或配置改了要重建）。</summary>
        public void Close(CommItem cfg)
        {
            if (cfg == null) return;
            if (_channels.TryRemove(ChannelKey(cfg), out var ch))
            {
                try { ch.Dispose(); } catch { }
                Log?.Invoke($"[通讯] 关闭通道 {cfg.Name}");
            }
        }

        /// <summary>获取或创建通道（按名称 + 类型指纹缓存，配置变了会重建）。</summary>
        private ICommChannel GetOrCreate(CommItem cfg)
        {
            string key = ChannelKey(cfg);
            return _channels.GetOrAdd(key, _ =>
            {
                var ch = Create(cfg);
                // ★ SECS 会话在后台线程里跑，它的日志必须接到同一个出口，否则页面/Lua 看不到收发
                if (ch is SecsCommChannel secs) secs.Log = Log;
                Log?.Invoke($"[通讯] 打开通道 {cfg.Name}（{cfg.CommType} → {cfg.PortOrIp}:{cfg.BaudOrPort}）");
                return ch;
            });
        }

        private static string ChannelKey(CommItem cfg) =>
            $"{cfg.Name}|{cfg.CommType}|{cfg.PortOrIp}|{cfg.BaudOrPort}|{cfg.DataBits}|{cfg.Parity}|{cfg.StopBits}"
            + $"|{cfg.SecsRole}|{cfg.SecsDeviceId}"
            // 新参数的改动必须换通道实例，否则改了「管道角色 / 共享区大小 / 写入方式 / 请求方式」还沿用旧通道
            + $"|{cfg.PipeRole}|{cfg.ShmSizeKb}|{cfg.FileWriteMode}|{cfg.WebMethod}|{cfg.WebContentType}"
            + $"|{(cfg.SimEnabled ? "仿真" : "实机")}";

        private static ICommChannel Create(CommItem cfg)
        {
            string type = (cfg.CommType ?? string.Empty).Trim();

            // ★ 仿真接管：串口 / ModbusRTU / GPIB / 西门子S7 / 三菱MC 这几类本机起不了「另一端」，
            //   开了仿真后本端通道直接换成仿真器的通道壳 —— 一次硬件都不碰，发送只进日志、接收按规则产生。
            //   （其余类型是「真对端」型：本端仍走真实通道，仿真器在本机另起一个对方。）
            if (Sim.CommSimManager.TryTakeover(cfg, out var takeover)) return takeover;

            // ★ SECS/HSMS 必须最先判断：它的类型串里没有 TCP 字样，
            //   落到最后会被默认分支当成普通 TcpCommChannel（不报错，但完全没有 SECS 语义）。
            if (Contains(type, "SECS", "HSMS", "E37", "E5"))
                return new SecsCommChannel(cfg);

            // ★ 本机互联 / 接口类通道：必须在「串口 / TCP 默认分支」之前判断。
            //   放到后面会被默认分支当成 TcpCommChannel —— 不报错，但完全没有管道 / 共享内存 / HTTP 语义。
            //   「共享文件夹」也要排在「本机文件」之前：两个词都含「文件」，先匹配「文件夹」才不会串台。
            if (Contains(type, "管道", "Pipe", "NamedPipe"))
                return new PipeCommChannel(cfg);
            if (Contains(type, "共享内存", "SharedMemory", "MMF", "内存"))
                return new SharedMemoryCommChannel(cfg);
            if (Contains(type, "文件夹", "目录", "投递", "Drop", "SharedFolder"))
                return new DropFolderCommChannel(cfg);
            if (Contains(type, "本机文件", "本地文件", "磁盘", "LocalFile", "File"))
                return new LocalFileCommChannel(cfg);
            if (Contains(type, "Web", "HTTP", "URL", "REST", "接口"))
                return new HttpCommChannel(cfg);

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
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
