// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁣志​◆‎编‌写⁠◇‍微⁠信‍﹕‎1⁣8​7‍◆‌1⁣9‍3‍6‏◇‌1‍3‌9⁣9⁣　‏※⁣保‍留⁠所‎有​权​利‎请⁣勿⁠删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.IO;
using System.Text;
using System.Threading;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Hardware.Comm
{
    /// <summary>
    /// 本机文件通道（CommType = 本机文件）：把「一个磁盘文件」当成收发口。
    /// <para>发送 = 按「写入方式」覆盖或追加写这个文件；接收 = 读文件内容，
    /// <b>只在文件被外部改过之后才返回一次</b>（否则返回空串）—— 否则脚本循环 Recv 会把同一条内容读无数遍。</para>
    /// <para>用途：和同机跑的其它程序（PLC 仿真、MES 客户端、脚本）用文件交接数据，
    /// 以及把流程算出来的结果落一份给人看。路径写在「文件路径」里，相对路径按程序运行目录解析。</para>
    /// </summary>
    public sealed class LocalFileCommChannel : ICommChannel
    {
        private readonly CommItem _cfg;
        private readonly object _gate = new object();
        private DateTime _lastSeen;         // 上次读到的文件修改时间：用来判断「有没有新内容」

        public LocalFileCommChannel(CommItem cfg) => _cfg = cfg;

        public string Name => _cfg.Name;

        /// <summary>文件能正常读写就算已连接（不需要事先打开）。</summary>
        public bool IsOpen => true;

        /// <summary>文件完整路径（相对路径按当前目录解析）。</summary>
        public string FullPath => Resolve(_cfg.PortOrIp);

        private static string Resolve(string path)
        {
            string p = (path ?? string.Empty).Trim().Trim('"');
            if (p.Length == 0)
                throw new IOException("文件路径为空：请在通讯配置里填写完整路径（如 D:\\ncm\\io.txt）。");
            return Path.IsPathRooted(p) ? p : Path.GetFullPath(p);
        }

        public void Open()
        {
            string full = FullPath;
            string dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); }
                catch (Exception ex) { throw new IOException($"创建目录失败：{dir}（{ex.Message}）"); }
            }
        }

        public void Send(string data)
        {
            byte[] payload = PayloadCodec.Encode(data);
            string full = FullPath;
            Open();
            lock (_gate)
            {
                try
                {
                    bool append = string.Equals((_cfg.FileWriteMode ?? string.Empty).Trim(), "追加", StringComparison.OrdinalIgnoreCase);
                    using (var fs = new FileStream(full, append ? FileMode.Append : FileMode.Create, FileAccess.Write))
                        fs.Write(payload, 0, payload.Length);

                    // 自己刚写的，不该被下一次 Recv 当成「外部新内容」读回来
                    _lastSeen = File.GetLastWriteTimeUtc(full);
                }
                catch (Exception ex)
                {
                    throw new IOException($"写文件失败：{full}（{ex.Message}）");
                }
            }
        }

        public string Recv()
        {
            string full = FullPath;
            int timeout = _cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 1000;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (true)
            {
                lock (_gate)
                {
                    if (File.Exists(full))
                    {
                        DateTime w = File.GetLastWriteTimeUtc(full);
                        if (w != _lastSeen)
                        {
                            try
                            {
                                byte[] all = File.ReadAllBytes(full);
                                _lastSeen = w;
                                return PayloadCodec.Decode(all, all.Length);
                            }
                            catch (IOException)
                            {
                                // 对方正在写：本轮读不了，下一轮再读
                            }
                        }
                    }
                    else if (sw.ElapsedMilliseconds > 0)
                    {
                        _lastSeen = default;      // 文件被删了：允许它重新出现时再算一次新内容
                    }
                }

                if (sw.ElapsedMilliseconds >= timeout) return string.Empty;
                Thread.Sleep(10);
            }
        }

        public void Dispose() { }
    }

    /// <summary>
    /// 共享文件夹通道（CommType = 共享文件夹）：一个目录当投递箱，两端各写自己的子目录、读别人写的子目录。
    /// <para>约定：根目录填两端同一个；本端发送写到「根目录 + send + 本端通道名」子目录下， <c>out\</c> 是我发出去的（每条消息一个文件），
    /// 接收则扫描「send」下除自己以外的子目录，按文件名升序取最旧一条、读完删除。
    /// 所以两端只要「通道名不同 + 根目录相同」就能对上。这是 MES / 上位机 / AGV 调度最常见的「文件投递」接法，
    /// 跨机器时把根目录填成网络共享路径即可（走 SMB，不需要额外协议）。</para>
    /// <para>文件名是「时间戳(毫秒)-序号」，先写 .tmp 再改名 —— 对方轮询时不会读到半截文件。</para>
    /// </summary>
    public sealed class DropFolderCommChannel : ICommChannel
    {
        private readonly CommItem _cfg;
        private int _seq;

        public DropFolderCommChannel(CommItem cfg) => _cfg = cfg;

        public string Name => _cfg.Name;

        public bool IsOpen => true;

        private string Root
        {
            get
            {
                string p = (_cfg.PortOrIp ?? string.Empty).Trim().Trim('"');
                if (p.Length == 0)
                    throw new IOException("共享目录为空：请在通讯配置里填写目录（如 D:\\ncm\\drop 或 \\\\server\\share\\drop）。");
                return Path.IsPathRooted(p) ? p : Path.GetFullPath(p);
            }
        }

        /// <summary>投递总目录（两端共用）：本端写它下面的 send\&lt;通道名&gt;\，读它下面别人的子目录。</summary>
        public string SendRoot => Path.Combine(Root, "send");

        /// <summary>本端发送目录。</summary>
        public string OutDir => Path.Combine(SendRoot, Own);

        /// <summary>接收扫描根目录（= send\，实际只取别人名下的子目录）。</summary>
        public string InDir => SendRoot;

        /// <summary>本端身份（通道名转成安全目录名）。</summary>
        private string Own => Sanitize(_cfg.Name);

        public void Open()
        {
            try { Directory.CreateDirectory(OutDir); }
            catch (Exception ex)
            {
                throw new IOException($"创建投递目录失败：{OutDir}（{ex.Message}）—— 网络共享请确认已连接、且有写权限。");
            }
        }

        public void Send(string data)
        {
            byte[] payload = PayloadCodec.Encode(data);
            Open();
            // 时间戳（含毫秒）+ 自增序号 + 通道名：排序稳定、同名不覆盖
            string file = Path.Combine(OutDir,
                $"{DateTime.Now:yyyyMMdd-HHmmss-fff}-{Interlocked.Increment(ref _seq):D3}.msg");
            try
            {
                // 先写临时名再改名：对方轮询时不会读到半截文件
                string tmp = file + ".tmp";
                File.WriteAllBytes(tmp, payload);
                File.Move(tmp, file, overwrite: true);
            }
            catch (Exception ex)
            {
                throw new IOException($"投递文件写入失败：{file}（{ex.Message}）");
            }
        }

        public string Recv()
        {
            int timeout = _cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 1000;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (true)
            {
                string picked = null;
                try
                {
                    if (Directory.Exists(InDir))
                    {
                        // 只收别人投来的：send\ 下除本端通道名以外的子目录
                        foreach (var peer in Directory.GetDirectories(InDir))
                        {
                            if (string.Equals(Path.GetFileName(peer), Own, StringComparison.OrdinalIgnoreCase)) continue;
                            foreach (var f in Directory.GetFiles(peer))
                            {
                                string name = Path.GetFileName(f);
                                if (name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) continue;   // 还没写完
                                // 跨目录按「时间戳-序号」文件名比较即为全局先后顺序
                                if (picked == null || string.CompareOrdinal(CompareKey(f), CompareKey(picked)) < 0)
                                    picked = f;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new IOException($"读取投递目录失败：{InDir}（{ex.Message}）");
                }

                if (picked != null)
                {
                    try
                    {
                        byte[] all = File.ReadAllBytes(picked);
                        try { File.Delete(picked); } catch { /* 删不掉也先交给脚本，下轮可能重复读到，比丢数据好 */ }
                        return PayloadCodec.Decode(all, all.Length);
                    }
                    catch (IOException)
                    {
                        // 对方还在写这个文件：本轮跳过，下一轮再取
                    }
                }

                if (sw.ElapsedMilliseconds >= timeout) return string.Empty;
                Thread.Sleep(10);
            }
        }

        public void Dispose() { }

        /// <summary>跨子目录的先后顺序键：相对 send\ 的路径（子目录名 + 文件名，文件名本身以时间戳开头）。</summary>
        private string CompareKey(string fullPath)
        {
            string rel = fullPath.Substring(InDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return rel.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        private static string Sanitize(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in (s ?? "comm"))
                sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            return sb.Length == 0 ? "comm" : sb.ToString();
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
