// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁣志​◆‎编‌写⁠◇‍微⁠信‍﹕‎1⁣8​7‍◆‌1⁣9‍3‍6‏◇‌1‍3‌9⁣9⁣　‏※⁣保‍留⁠所‎有​权​利‎请⁣勿⁠删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Hardware.Comm
{
    /// <summary>
    /// 共享内存通道（CommType = 共享内存）：同一台电脑、同一登录会话里的两个进程，
    /// 通过一块命名内存映射区直接传字节 —— 不经网络、不落磁盘，是本机之间最快的接法
    /// （两份本软件互发信号、或和 C#/Python 写的算法程序交换数据）。
    ///
    /// <para>内存布局（一块映射区里放两条单向环形队列）：</para>
    /// <code>
    ///  [0 .. 63]        两条队列的占用者名字（各 32 字节 ASCII，全 0 = 空位）
    ///  [64 .. ]         队列 X：long written | long read | 环形数据区(capacity)
    ///  [64+16+capacity ..]  队列 Y：long written | long read | 环形数据区(capacity)
    /// </code>
    ///
    /// <para>★ 两端必须用<b>不同的通道名</b>（如 shm-a / shm-b）：谁先 Open 谁占 X 队列、
    /// 另一端占 Y 队列 —— 各写自己那条、读对方那条。同名重启时会认回自己原来那条队列。</para>
    /// <para>读写用一把同名 Mutex 互斥；计数器单调递增，所以不必区分「满」和「空」。
    /// 队列写满时会等对方取走，超过「超时」就报中文错误 —— 绝不覆盖对方没读的数据，也不挂死脚本线程。</para>
    /// </summary>
    public sealed class SharedMemoryCommChannel : ICommChannel
    {
        private const int OwnerSlotBytes = 32;       // 每条队列占用者名字的长度
        private const int HeaderBytes = OwnerSlotBytes * 2;
        private const int RingHeadBytes = 16;        // long written + long read

        private readonly CommItem _cfg;
        private readonly object _gate = new object();
        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _view;
        private Mutex _mutex;
        private bool _owned;                          // 当前线程是否持有互斥锁
        private int _myRing;                          // 0 = X 队列，1 = Y 队列（我方发送方向）
        private int _capacity;                        // 单条队列数据区字节数

        public SharedMemoryCommChannel(CommItem cfg) => _cfg = cfg;

        public string Name => _cfg.Name;

        public bool IsOpen { get { lock (_gate) return _view != null; } }

        /// <summary>本端占的是哪条队列（0 = X，1 = Y）：发送写自己这条、接收读对方那条。</summary>
        public int MyRing => _myRing;

        /// <summary>共享区名字（两端填同一个；留空则用通道名）。</summary>
        public string MapName
        {
            get
            {
                string p = (_cfg.PortOrIp ?? string.Empty).Trim().Trim('"');
                if (p.Length == 0)
                    p = string.IsNullOrWhiteSpace(_cfg.Name) ? "ncm_shm" : _cfg.Name.Trim();
                return p.Replace('\\', '_').Replace('/', '_');
            }
        }

        /// <summary>单条队列容量（字节），由「共享内存大小(KB)」换算。</summary>
        public int Capacity
        {
            get
            {
                int kb = _cfg.ShmSizeKb > 0 ? _cfg.ShmSizeKb : 64;
                if (kb > 8192) kb = 8192;             // 上限 8MB，防手滑把内存吃光
                return kb * 1024;
            }
        }

        public void Open()
        {
            lock (_gate)
            {
                if (_view != null) return;
                string map = MapName;
                _capacity = Capacity;
                long size = HeaderBytes + 2L * (RingHeadBytes + _capacity);

                try
                {
                    _mmf = MemoryMappedFile.CreateOrOpen(map, size);
                    _view = _mmf.CreateViewAccessor(0, size, MemoryMappedFileAccess.ReadWrite);
                    _mutex = new Mutex(false, "ncm_mtx_" + map);
                }
                catch (Exception ex)
                {
                    Cleanup();
                    throw new IOException($"打开共享内存「{map}」失败：{ex.Message}"
                                          + "（两端若设置了不同的「共享内存大小(KB)」，请改成一致）");
                }

                if (!Hold(2000)) { Cleanup(); throw new IOException($"共享内存「{map}」忙：等锁 2000ms 超时，稍后重试。"); }
                try
                {
                    string self = OwnerOf(_cfg.Name);
                    string a = ReadOwner(0), b = ReadOwner(1);

                    if (a == self) _myRing = 0;
                    else if (b == self) _myRing = 1;
                    else if (string.IsNullOrEmpty(a)) { WriteOwner(0, self); _myRing = 0; }
                    else if (string.IsNullOrEmpty(b)) { WriteOwner(1, self); _myRing = 1; }
                    else
                        throw new IOException($"共享内存「{map}」的两条队列已被「{a}」「{b}」占用："
                                              + "一块共享区最多接两端，且两端的「通道名」必须不同。");
                }
                catch { Cleanup(); throw; }
                finally { Release(); }
            }
        }

        public void Send(string data)
        {
            Open();
            byte[] payload = PayloadCodec.Encode(data);
            if (payload.Length == 0) return;
            if (payload.Length > _capacity)
                throw new IOException($"这条消息 {payload.Length} 字节，超过共享区单条队列容量 {_capacity} 字节："
                                      + "请调大「共享内存大小(KB)」，或拆成多条发送。");

            int timeout = _cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 1000;
            var swSend = System.Diagnostics.Stopwatch.StartNew();
            if (!Hold(timeout))
                throw new IOException($"共享内存写入等锁超时（{timeout}ms）：对方可能正在大量读写。");
            try
            {
                long ring = RingBase(_myRing);
                long written = _view.ReadInt64(ring);
                long read = _view.ReadInt64(ring + 8);

                // 队列快满了（对方还没取走）就等；等不到就报错，绝不覆盖对方未读的数据
                while (written - read + payload.Length > _capacity)
                {
                    Release();
                    Thread.Sleep(5);
                    if (swSend.ElapsedMilliseconds >= timeout)
                        throw new IOException($"共享内存队列已满（{timeout}ms 内对方没取数据）："
                                              + "请确认另一端在跑、且「共享区名」两端一致。");
                    if (!Hold(timeout - (int)swSend.ElapsedMilliseconds))
                        throw new IOException($"共享内存写入等锁超时（{timeout}ms）。");
                    read = _view.ReadInt64(ring + 8);
                }

                for (int i = 0; i < payload.Length; i++)
                    _view.Write(ring + RingHeadBytes + ((written + i) % _capacity), payload[i]);
                _view.Write(ring, written + payload.Length);
                _view.Flush();
            }
            finally { Release(); }
        }

        public string Recv()
        {
            Open();
            int timeout = _cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 1000;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (true)
            {
                int left = Math.Max(50, timeout - (int)sw.ElapsedMilliseconds);
                if (Hold(left))
                {
                    try
                    {
                        long ring = RingBase(1 - _myRing);          // 读对方写的那条队列
                        long written = _view.ReadInt64(ring);
                        long read = _view.ReadInt64(ring + 8);
                        int avail = (int)(written - read);
                        if (avail > _capacity) avail = _capacity;   // 对方重启过等异常情况：按容量截断，别越界

                        if (avail > 0)
                        {
                            var buf = new byte[avail];
                            for (int i = 0; i < avail; i++)
                                _view.Read(ring + RingHeadBytes + ((read + i) % _capacity), out buf[i]);
                            _view.Write(ring + 8, read + avail);
                            _view.Flush();
                            return PayloadCodec.Decode(buf, buf.Length);
                        }
                    }
                    finally { Release(); }
                }

                if (sw.ElapsedMilliseconds >= timeout) return string.Empty;
                Thread.Sleep(5);
            }
        }

        private long RingBase(int ring) => HeaderBytes + ring * (RingHeadBytes + _capacity);

        private string OwnerOf(string name)
        {
            string n = string.IsNullOrWhiteSpace(name) ? "comm" : name.Trim();
            return n.Length > OwnerSlotBytes - 1 ? n.Substring(0, OwnerSlotBytes - 1) : n;
        }

        private string ReadOwner(int ring)
        {
            var bytes = new byte[OwnerSlotBytes];
            _view.ReadArray(OwnerSlotBytes * ring, bytes, 0, bytes.Length);
            int len = Array.IndexOf(bytes, (byte)0);
            if (len < 0) len = bytes.Length;
            return Encoding.ASCII.GetString(bytes, 0, len);
        }

        private void WriteOwner(int ring, string name)
        {
            var bytes = new byte[OwnerSlotBytes];
            if (!string.IsNullOrEmpty(name))
                Encoding.ASCII.GetBytes(name, 0, name.Length, bytes, 0);
            _view.WriteArray(OwnerSlotBytes * ring, bytes, 0, bytes.Length);
            _view.Flush();
        }

        private bool Hold(int ms)
        {
            if (_owned) return true;
            try { _owned = _mutex.WaitOne(ms <= 0 ? 1 : ms); return _owned; }
            catch (AbandonedMutexException) { _owned = true; return true; }   // 对方进程被强杀：锁归我，继续干活
            catch { return false; }
        }

        private void Release()
        {
            if (!_owned) return;
            _owned = false;
            try { _mutex.ReleaseMutex(); } catch { }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                // 让出自己的队列位，方便对方重启后仍能按名字认回原队列
                if (_view != null && Hold(200))
                {
                    try { WriteOwner(_myRing, string.Empty); } catch { }
                    Release();
                }
                Cleanup();
            }
        }

        private void Cleanup()
        {
            try { _view?.Dispose(); } catch { }
            try { _mmf?.Dispose(); } catch { }
            try { _mutex?.Dispose(); } catch { }
            _view = null; _mmf = null; _mutex = null;
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
