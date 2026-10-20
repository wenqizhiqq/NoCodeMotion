// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁣志​◆‎编‌写⁠◇‍微⁠信‍﹕‎1⁣8​7‍◆‌1⁣9‍3‍6‏◇‌1‍3‌9⁣9⁣　‏※⁣保‍留⁠所‎有​权​利‎请⁣勿⁠删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Generic;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Hardware.Comm.Sim
{
    /// <summary>
    /// 通讯仿真器注册表：一条「通讯」配置对应一个仿真器，按配置指纹缓存（改了参数自动重建）。
    /// <para>对外三件事：① 面板拿仿真器（<see cref="For"/>）启停 / 看日志 / 注入；
    /// ② <see cref="TryTakeover"/> 给 <c>CommManager</c> 用 —— 回环型（串口 / ModbusRTU / GPIB / S7 / MC）
    /// 开了仿真之后，本端通道直接换成仿真器的通道壳，一次硬件都不碰；
    /// ③ 全局日志出口，通讯页与 Lua 输出面板都能看到。</para>
    /// </summary>
    public static class CommSimManager
    {
        private static readonly Dictionary<string, ICommSimulator> _sims =
            new Dictionary<string, ICommSimulator>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> _stamps =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>所有仿真器的日志汇流到这里（通讯页 / 状态栏接这里）。</summary>
        public static event Action<string> LineLogged;

        /// <summary>这几类本机起不了「另一端」（要物理线 / 未内置协议），只能用回环接管式仿真。</summary>
        public static bool IsLoopbackType(string type)
        {
            type = (type ?? string.Empty).Trim();
            return Contains(type, "串口", "ModbusRTU", "GPIB", "西门子", "S7", "三菱", "MC");
        }

        /// <summary>取（或按当前配置重建）某条通讯项的仿真器。</summary>
        public static ICommSimulator For(CommItem cfg)
        {
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            string name = cfg.Name ?? string.Empty;
            string stamp = Stamp(cfg);
            lock (_sims)
            {
                if (_sims.TryGetValue(name, out var old) && _stamps[name] == stamp) return old;
                if (old != null) { try { old.Dispose(); } catch { } }
                var sim = Build(cfg);
                sim.LineLogged += line => LineLogged?.Invoke(line);
                _sims[name] = sim;
                _stamps[name] = stamp;
                return sim;
            }
        }

        /// <summary>已建过的仿真器（不重建、不启动）。面板刷新状态用。</summary>
        public static ICommSimulator Peek(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            lock (_sims) { _sims.TryGetValue(name, out var s); return s; }
        }

        /// <summary>
        /// CommManager 建通道前问一句：这条通讯项是不是已经被「回环仿真」接管了。
        /// 是的话给出仿真通道壳（收发都不碰硬件），返回 true。
        /// </summary>
        public static bool TryTakeover(CommItem cfg, out ICommChannel channel)
        {
            channel = null;
            if (cfg == null || !cfg.SimEnabled || !IsLoopbackType(cfg.CommType)) return false;
            if (!(For(cfg) is LoopbackSimulator lb)) return false;
            channel = lb.CreateChannel();
            return true;
        }

        /// <summary>停掉某条通讯项的仿真（面板点「停止仿真」）。</summary>
        public static void Stop(CommItem cfg)
        {
            if (cfg == null) return;
            ICommSimulator sim;
            lock (_sims) { _sims.TryGetValue(cfg.Name ?? string.Empty, out sim); }
            if (sim == null) return;
            try { sim.Stop(); } catch { }
        }

        /// <summary>全部停掉（退出软件 / 关闭工程时调用）。</summary>
        public static void StopAll()
        {
            lock (_sims)
            {
                foreach (var kv in _sims) { try { kv.Value.Dispose(); } catch { } }
                _sims.Clear();
                _stamps.Clear();
            }
        }

        // ---------- 各类型的仿真器怎么来 ----------

        private static ICommSimulator Build(CommItem cfg)
        {
            string type = (cfg.CommType ?? string.Empty).Trim();

            // 反向通道型：本机再起一条同样的通道当「对方」，收发全是真的
            if (Contains(type, "管道"))
                return new PeerChannelSimulator(cfg, c => new PipeCommChannel(OppositePipe(c)),
                    PipeEndpoint(cfg),
                    "对端 = 反向角色的命名管道。本端是「服务端」时对端就是「客户端」，反之亦然；两端都在本机，数据真的过一遍管道。");
            if (Contains(type, "共享内存", "内存"))
                return new PeerChannelSimulator(cfg, c => new SharedMemoryCommChannel(PeerName(c)),
                    $"共享区「{cfg.PortOrIp}」的另一端",
                    "对端 = 同一块共享内存的另一条队列（仿真器通道名自动加「·对端」，避免和本端抢同一个占位）。");
            if (Contains(type, "文件夹", "目录", "投递"))
                return new PeerChannelSimulator(cfg, c => new DropFolderCommChannel(PeerName(c)),
                    $"投递箱「{cfg.PortOrIp}\\send」的另一端",
                    "对端 = 同一根目录下另一个通道名的投递箱；本端发的文件它读走，它发的文件本端读。");
            if (Contains(type, "本机文件", "本地文件"))
                return new PeerChannelSimulator(cfg, c => new LocalFileCommChannel(PeerName(c)),
                    $"同一个文件「{cfg.PortOrIp}」的另一端",
                    "对端 = 读写同一个文件的另一个实例；「覆盖」模式下后写的一方会盖掉前一方，注意顺序。");
            if (Contains(type, "SECS", "HSMS"))
                return new PeerChannelSimulator(cfg, c => new SecsCommChannel(OppositeSecs(c)),
                    SecsEndpoint(cfg),
                    "对端 = 反向角色的 HSMS 会话（自动应答 S1F1 / S1F13 / S1F15 / S1F17）。数据消息按规则回，"
                    + "不含具体设备的 S6/S2 语义。");

            // 监听型：本机起一个真服务，本端照常连
            if (Contains(type, "ModbusTCP", "Modbus TCP"))
                return new ModbusTcpSlaveSimulator(cfg);
            if (Contains(type, "UDP"))
                return new UdpPeerSimulator(cfg);
            if (Contains(type, "Web", "HTTP", "URL"))
                return new HttpMockSimulator(cfg);
            if (Contains(type, "TCP", "网口", "相机", "以太网"))
                return new TcpPeerSimulator(cfg);

            // 兜底：回环接管（串口 / ModbusRTU / GPIB / S7 / MC 都走这条）
            return new LoopbackSimulator(cfg);
        }

        private static string PipeEndpoint(CommItem cfg)
        {
            string own = string.Equals((cfg.PipeRole ?? "").Trim(), "客户端", StringComparison.OrdinalIgnoreCase)
                ? "客户端" : "服务端";
            return $"本机管道「{cfg.PortOrIp}」的另一端（对端角色 = {(own == "服务端" ? "客户端" : "服务端")}）";
        }

        private static string SecsEndpoint(CommItem cfg)
        {
            bool passive = !string.Equals((cfg.SecsRole ?? "").Trim(), "主动", StringComparison.OrdinalIgnoreCase);
            return passive
                ? $"对端主动连本机 :{cfg.BaudOrPort}（本端被动监听，DeviceID {cfg.SecsDeviceId}）"
                : $"对端在本机 :{cfg.BaudOrPort} 监听，等本端连入（DeviceID {cfg.SecsDeviceId}）";
        }

        /// <summary>复制一份配置，只把「管道角色」反过来。</summary>
        private static CommItem OppositePipe(CommItem c)
        {
            var p = Clone(c);
            p.PipeRole = string.Equals((c.PipeRole ?? "").Trim(), "客户端", StringComparison.OrdinalIgnoreCase)
                ? "服务端" : "客户端";
            return p;
        }

        /// <summary>复制一份配置，只把 HSMS 角色反过来（主动 ↔ 被动）。</summary>
        private static CommItem OppositeSecs(CommItem c)
        {
            var p = Clone(c);
            bool passive = !string.Equals((c.SecsRole ?? "").Trim(), "主动", StringComparison.OrdinalIgnoreCase);
            p.SecsRole = passive ? "主动" : "被动";
            if (passive) p.PortOrIp = "127.0.0.1";            // 本端监听 → 对端连本机
            return p;
        }

        /// <summary>复制一份配置，只把通道名改成「原名·对端」（共享内存 / 投递目录靠通道名区分两端）。</summary>
        private static CommItem PeerName(CommItem c)
        {
            var p = Clone(c);
            p.Name = (c.Name ?? "comm") + "·对端";
            return p;
        }

        private static CommItem Clone(CommItem c) => new CommItem
        {
            Name = c.Name,
            CommType = c.CommType,
            PortOrIp = c.PortOrIp,
            BaudOrPort = c.BaudOrPort,
            DataBits = c.DataBits,
            Parity = c.Parity,
            StopBits = c.StopBits,
            TimeoutMs = c.TimeoutMs,
            SecsRole = c.SecsRole,
            SecsDeviceId = c.SecsDeviceId,
            SecsT3Ms = c.SecsT3Ms,
            SecsT5Ms = c.SecsT5Ms,
            SecsT6Ms = c.SecsT6Ms,
            SecsT7Ms = c.SecsT7Ms,
            SecsT8Ms = c.SecsT8Ms,
            SecsAutoReply = c.SecsAutoReply,
            SecsMdln = c.SecsMdln,
            SecsSoftRev = c.SecsSoftRev,
            PipeRole = c.PipeRole,
            ShmSizeKb = c.ShmSizeKb,
            FileWriteMode = c.FileWriteMode,
            WebMethod = c.WebMethod,
            WebContentType = c.WebContentType,
            SimEnabled = c.SimEnabled,
            SimMode = c.SimMode,
            SimRules = c.SimRules,
        };

        private static string Stamp(CommItem c) =>
            $"{c.CommType}|{c.PortOrIp}|{c.BaudOrPort}|{c.DataBits}|{c.PipeRole}|{c.SecsRole}|{c.SecsDeviceId}"
            + $"|{c.ShmSizeKb}|{c.FileWriteMode}|{c.WebMethod}|{c.SimEnabled}";

        private static bool Contains(string src, params string[] keys)
        {
            foreach (var k in keys)
                if (src.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
