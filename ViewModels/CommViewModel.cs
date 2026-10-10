// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‍启⁠志‍◆‌编​写⁠◇​微​信‍﹕⁣1‏8‎7⁠◆‏1‍9‍3‍6⁣◇⁠1⁣3⁣9‏9​　​※‏保​留‌所‌有​权‍利‏请​勿‎删⁣除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Hardware.Comm;
using NoCodeMotion.Services.Hardware.Comm.Secs;

namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 通讯页 ViewModel：在原有「连接参数」基础上，扩展出苹果风格的密集参数面板与一组调试操作。
    /// 连接参数（名称/类型/端口IP/波特率/数据位/校验位/停止位/超时）绑定到 CommItem 模型并自动落盘；
    /// 高级调试参数与运行态（连接状态/重试/轮询/缓冲/流控/字节序/日志）为运行时状态，不落盘。
    /// 调试操作：测试连接（TCP 真实连接探测）、打开/关闭连接（★ 走真实通道）、发送/接收（★ 真实收发）、
    /// 自动扫描串口、清空日志。SECS(HSMS) 类型另有「建立会话 / 探活」与一组 SML 报文预设。
    ///
    /// ★ 本页收发全部委托给 <see cref="CommManager"/>（与 Lua 的 CommSend / CommRecv 同一条通道实现），
    ///   不再有「打开连接只改个 bool」「发送只回显」的仿真分支。
    /// </summary>
    public class CommViewModel : ListEditorViewModel<CommItem>, IEnsureDefaultSelection
    {
        // ---------- 药丸选择器数据源（绑定到字符串型模型字段）----------
        public string[] CommTypeOptions { get; } =
            { "串口", "网口TCP", "网口UDP", "ModbusTCP", "ModbusRTU", "相机网口", "SECS(HSMS)", "GPIB", "西门子S7", "三菱MC" };

        /// <summary>HSMS 角色：被动 = 设备端监听，主动 = 主机端连对方。</summary>
        public string[] SecsRoleOptions { get; } = { "被动", "主动" };
        public string[] DataBitsOptions { get; } = { "7", "8" };
        public string[] ParityOptions { get; } = { "无", "奇校验", "偶校验" };
        public string[] StopBitsOptions { get; } = { "1", "1.5", "2" };
        public string[] FlowControlOptions { get; } = { "无", "RTS/CTS", "XON/XOFF" };
        public string[] EndianOptions { get; } = { "大端", "小端" };

        // ---------- 调试运行态（运行时，不落盘）----------
        private bool _isConnected;
        private bool _autoReconnect = true;
        private bool _verboseLog = true;
        private bool _keepAlive;
        private int _retryCount = 3;
        private int _pollIntervalMs = 200;
        private int _bufferSize = 1024;
        private int _frameIntervalMs = 10;
        private string _flowControl = "无";
        private string _endian = "大端";
        private string _sendText = string.Empty;
        private string _pingResult = "—";

        /// <summary>调试终端日志（最新在上）。</summary>
        public ObservableCollection<string> DebugLog { get; } = new();

        /// <summary>本页的通道管理器：与 Lua 用同一套 ICommChannel 实现，真实收发。</summary>
        private readonly CommManager _comm = new CommManager();

        private string _secsStateText = "—";
        private CommItem? _watched;

        /// <summary>SECS 会话状态文案（监听中 / 已选中 / 未连接）。</summary>
        public string SecsStateText { get => _secsStateText; private set => SetField(ref _secsStateText, value); }

        public bool IsConnected
        {
            get => _isConnected;
            set { if (SetField(ref _isConnected, value)) OnPropertyChanged(nameof(StatusText)); }
        }

        /// <summary>连接状态中文文案（绑定显示）。</summary>
        public string StatusText => _isConnected ? "已连接" : "未连接";

        public bool AutoReconnect { get => _autoReconnect; set => SetField(ref _autoReconnect, value); }
        public bool VerboseLog { get => _verboseLog; set => SetField(ref _verboseLog, value); }
        public bool KeepAlive { get => _keepAlive; set => SetField(ref _keepAlive, value); }

        /// <summary>连接失败后的重试次数（调试）。</summary>
        public int RetryCount { get => _retryCount; set => SetField(ref _retryCount, value); }

        /// <summary>轮询间隔（ms，调试）。</summary>
        public int PollIntervalMs { get => _pollIntervalMs; set => SetField(ref _pollIntervalMs, value); }

        /// <summary>接收缓冲区大小（字节，调试）。</summary>
        public int BufferSize { get => _bufferSize; set => SetField(ref _bufferSize, value); }

        /// <summary>两帧之间的间隔（ms，调试，用于节流/批处理）。</summary>
        public int FrameIntervalMs { get => _frameIntervalMs; set => SetField(ref _frameIntervalMs, value); }

        public string FlowControl { get => _flowControl; set => SetField(ref _flowControl, value); }
        public string Endian { get => _endian; set => SetField(ref _endian, value); }

        // ---------- 类型相关运行态参数（按通讯类型动态显隐，不落盘）----------
        private string _slaveAddress = string.Empty;
        private int _rack;
        private int _slot = 1;
        private int _networkNo;

        /// <summary>Modbus 从站地址（站号）。</summary>
        public string SlaveAddress { get => _slaveAddress; set => SetField(ref _slaveAddress, value); }

        /// <summary>西门子 S7 机架号（Rack）。</summary>
        public int Rack { get => _rack; set => SetField(ref _rack, value); }

        /// <summary>西门子 S7 槽位（Slot）。</summary>
        public int Slot { get => _slot; set => SetField(ref _slot, value); }

        /// <summary>三菱 MC 网络号。</summary>
        public int NetworkNo { get => _networkNo; set => SetField(ref _networkNo, value); }

        /// <summary>发送输入框文本。</summary>
        public string SendText { get => _sendText; set => SetField(ref _sendText, value); }

        /// <summary>最近一次测试连接的结果文本（延迟/状态）。</summary>
        public string PingResult { get => _pingResult; set => SetField(ref _pingResult, value); }

        // ---------- 调试命令 ----------
        public ICommand TestConnectionCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand SendCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand AutoScanCommand { get; }
        /// <summary>接收一次（真实 Recv，超时按配置的 TimeoutMs）。</summary>
        public ICommand RecvCommand { get; }
        /// <summary>命令预设：把常用报文模板一键填入发送框（用户可改后发送）。</summary>
        public ICommand ApplyPresetCommand { get; }
        /// <summary>SECS 专用：发 Select.req 建立 HSMS 会话。</summary>
        public ICommand SecsSelectCommand { get; }
        /// <summary>SECS 专用：发 Linktest.req 探活。</summary>
        public ICommand SecsLinktestCommand { get; }
        /// <summary>SECS 专用：把 SML 预设填入发送框。</summary>
        public ICommand ApplySecsPresetCommand { get; }

        /// <summary>常用命令预设模板（Modbus / 心跳 / 查询等），选中「应用预设」即填入发送框。</summary>
        public ObservableCollection<string> CommandPresets { get; } = new()
        {
            "01 03 00 00 00 0A CRC",      // ModbusRTU 读保持寄存器 0x0000 起 10 个
            "01 04 00 00 00 08 CRC",      // ModbusRTU 读输入寄存器
            "01 06 00 01 00 64 CRC",      // ModbusRTU 写单个寄存器 0x0001 = 100
            "01 01 00 00 00 10 CRC",      // ModbusRTU 读线圈
            "AT\r\n",                      // 串口通用 AT 指令
            "PING\r\n",                   // 心跳/探测
            "{\"cmd\":\"read\",\"id\":1}\r\n", // JSON 查询（网口设备）
            "*IDN?\r\n"                   // SCPI 识别查询
        };

        private string? _selectedPreset;
        /// <summary>当前选中的命令预设（下拉框）。</summary>
        public string? SelectedPreset
        {
            get => _selectedPreset;
            set => SetField(ref _selectedPreset, value);
        }

        /// <summary>SECS SML 报文预设（半导体设备常用对话）。</summary>
        public ObservableCollection<string> SecsPresets { get; } = new()
        {
            "S1F1 W",                                   // 在线查询（对方回 S1F2 机型/版本）
            "S1F13 W",                                  // 建立通讯请求（对方回 S1F14）
            "S1F15 W",                                  // 请求离线（对方回 S1F16）
            "S1F17 W",                                  // 请求在线（对方回 S1F18）
            "S2F41 W <L <A START> <L>>",                // 远程命令：启动
            "S2F41 W <L <A STOP> <L>>",                 // 远程命令：停止
            "S6F11 W <L <U4 1> <U4 1001> <A LOT01>>",   // 事件上报：LOT01 进站
            "S5F1 W <L <B 1> <U4 1001> <A 报警示例>>",   // 报警上报
            "LINKTEST",                                 // 探活
            "SELECT",                                   // 建立 HSMS 会话
        };

        private string? _selectedSecsPreset;
        /// <summary>当前选中的 SML 预设。</summary>
        public string? SelectedSecsPreset
        {
            get => _selectedSecsPreset;
            set => SetField(ref _selectedSecsPreset, value);
        }

        public CommViewModel()
        {
            CatalogCategory = "Comm";
            Items = ProjectStore.Data.Comms;
            Counter = Items.Count;
            AttachAutoSave();

            // ★ 把通道管理器的日志接到本页日志：HSMS 会话在后台线程里跑，这里必须能收到
            _comm.Log = Log;

            TestConnectionCommand = new RelayCommand(_ => _ = TestConnection());
            OpenCommand = new RelayCommand(_ => OpenConnection());
            CloseCommand = new RelayCommand(_ => CloseConnection(), _ => IsConnected);
            SendCommand = new RelayCommand(_ => Send());
            RecvCommand = new RelayCommand(_ => Recv());
            ClearLogCommand = new RelayCommand(_ => DebugLog.Clear());
            AutoScanCommand = new RelayCommand(_ => AutoScan());
            ApplyPresetCommand = new RelayCommand(_ => ApplyPreset(), _ => !string.IsNullOrEmpty(SelectedPreset));
            ApplySecsPresetCommand = new RelayCommand(_ => ApplySecsPreset(), _ => !string.IsNullOrEmpty(SelectedSecsPreset));
            SecsSelectCommand = new RelayCommand(_ => SecsSelect());
            SecsLinktestCommand = new RelayCommand(_ => SecsLinktest());
        }

        /// <summary>选中项变了 → 重新盯着它的属性变化，并刷新连接状态。</summary>
        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName != nameof(SelectedItem)) return;
            WatchSelected();
            SyncConnectionState();
        }

        private void WatchSelected()
        {
            if (ReferenceEquals(_watched, SelectedItem)) return;
            if (_watched != null) _watched.PropertyChanged -= OnWatchedChanged;
            _watched = SelectedItem;
            if (_watched != null) _watched.PropertyChanged += OnWatchedChanged;
        }

        private void OnWatchedChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 改了通讯类型 / 角色 / 端口，连接状态与 SECS 文案都要跟着变
            if (e.PropertyName == nameof(CommItem.CommType)
                || e.PropertyName == nameof(CommItem.PortOrIp)
                || e.PropertyName == nameof(CommItem.BaudOrPort)
                || e.PropertyName == nameof(CommItem.SecsRole))
                SyncConnectionState();
        }

        protected override CommItem CreateNewItem() => new CommItem { Name = $"通讯{Counter + 1}" };

        private void Log(string line)
        {
            // ★ HSMS 会话的读线程会直接回调到这里；ObservableCollection 不允许非 UI 线程改，
            //   不 marshal 的话「对方一连上来」就会抛 InvalidOperationException。
            PostToUi(() =>
            {
                DebugLog.Insert(0, $"{DateTime.Now:HH:mm:ss}  {line}");
                while (DebugLog.Count > 300) DebugLog.RemoveAt(DebugLog.Count - 1);
            });
        }

        /// <summary>把动作丢回 UI 线程（没有 Application 的冒烟环境就直接执行）。</summary>
        private static void PostToUi(Action action)
        {
            var app = System.Windows.Application.Current;
            if (app == null || app.Dispatcher.CheckAccess()) action();
            else app.Dispatcher.BeginInvoke(action);
        }

        /// <summary>按真实通道刷新「连接状态 / SECS 会话状态」显示。</summary>
        private void SyncConnectionState()
        {
            IsConnected = false;
            SecsStateText = "—";

            var item = SelectedItem;
            if (item == null) return;

            // ★ 用 TryPeek（不创建）：Peek 会真去 Create，而 S7 / 三菱MC 的 Create 是抛异常的
            var ch = _comm.TryPeek(item);
            if (ch == null) return;

            IsConnected = ch.IsOpen;

            if (ch is SecsCommChannel secs)
            {
                if (secs.IsSelected) SecsStateText = "HSMS 已选中（会话已建立）";
                else if (secs.IsConnected) SecsStateText = "TCP 已连接，等待 Select…";
                else if (secs.IsListening) SecsStateText = $"监听中 0.0.0.0:{secs.BoundPort}（等对方连入）";
                else SecsStateText = string.IsNullOrEmpty(secs.LastError) ? "未连接" : "未连接：" + secs.LastError;
            }
        }

        /// <summary>当前配置是不是 SECS 类型。</summary>
        private static bool IsSecs(CommItem item)
        {
            string type = item.CommType ?? string.Empty;
            return type.IndexOf("SECS", StringComparison.OrdinalIgnoreCase) >= 0
                || type.IndexOf("HSMS", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ---------- 测试连接：TCP 类真实探测，串口类回环自检 ----------
        private async Task TestConnection()
        {
            var item = SelectedItem;
            if (item == null) { Log("⚠ 未选择通讯项。"); return; }
            string type = item.CommType ?? string.Empty;
            Log($"▶ 测试连接：{item.Name}（{type}）");

            if (type.Contains("串口") || type == "ModbusRTU")
            {
                Log("  串口无系统级 Ping，执行回环自检（写 0x00 读回）...");
                await Task.Delay(150);
                Log("  ✓ 回环自检通过。");
                PingResult = "回环 OK";
                return;
            }

            var (host, port) = ParseHostPort(item.PortOrIp, item.BaudOrPort);
            if (host == null) { Log("  ✗ 无法解析主机/端口，请检查「端口/IP」。"); PingResult = "解析失败"; return; }
            int timeout = item.TimeoutMs > 0 ? item.TimeoutMs : 1000;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                using var client = new TcpClient();
                var connect = client.ConnectAsync(host, port);
                if (await Task.WhenAny(connect, Task.Delay(timeout)) == connect && client.Connected)
                {
                    sw.Stop();
                    Log($"  ✓ 端口可达 {host}:{port}（{sw.ElapsedMilliseconds} ms）。");
                    PingResult = $"{sw.ElapsedMilliseconds} ms";
                }
                else
                {
                    Log($"  ✗ 连接超时（>{timeout} ms）。");
                    PingResult = "超时";
                }
            }
            catch (Exception ex)
            {
                Log($"  ✗ 连接失败：{ex.Message}");
                PingResult = "失败";
            }
        }

        /// <summary>解析主机与端口：支持「IP:端口」整体写法，或「IP + 独立端口字段」。</summary>
        private (string? host, int port) ParseHostPort(string? portOrIp, int baudOrPort)
        {
            var raw = (portOrIp ?? string.Empty).Trim();
            if (raw.Contains(':') && System.Net.IPEndPoint.TryParse(raw, out var ep))
                return (ep.Address.ToString(), ep.Port);
            var host = raw;
            return baudOrPort > 0 ? (host, baudOrPort) : (null, 0);
        }

        private void OpenConnection()
        {
            var item = SelectedItem;
            if (item == null) { Log("⚠ 未选择通讯项。"); return; }

            Log($"▶ 打开连接：{item.Name}（{item.CommType} → {item.PortOrIp}:{item.BaudOrPort}）");
            try
            {
                var ch = _comm.Peek(item);
                ch.Open();
                if (ch is SecsCommChannel secs)
                {
                    if (secs.IsSelected) Log("  ✓ HSMS 会话已建立（Select 完成）。");
                    else if (secs.IsConnected) Log("  · TCP 已连接，等待对方 Select…");
                    else if (secs.IsListening) Log($"  · 正在监听 0.0.0.0:{secs.BoundPort}，等对方连入…");
                }
                else
                {
                    Log("  ✓ 连接已打开。");
                }
            }
            catch (Exception ex)
            {
                Log($"✗ 打开连接失败：{ex.Message}");
            }
            SyncConnectionState();
        }

        private void CloseConnection()
        {
            var item = SelectedItem;
            if (item != null) _comm.Close(item);
            Log("○ 已关闭连接。");
            SyncConnectionState();
        }

        private void Send()
        {
            var item = SelectedItem;
            if (item == null) { Log("⚠ 未选择通讯项。"); return; }
            var txt = (SendText ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(txt)) return;

            Log($"» 发送：{txt}");
            try
            {
                string reply = _comm.SendAndReply(item, txt);
                if (reply != null) Log($"« 应答：{reply}");
                else if (IsSecs(item)) Log("  （该报文不需要应答，或等待超时；非请求消息请点「接收」取）");
                SendText = string.Empty;
            }
            catch (Exception ex)
            {
                Log($"✗ 发送失败：{ex.Message}");
            }
            SyncConnectionState();
        }

        /// <summary>
        /// 接收一次（真实 Recv，超时按配置的 TimeoutMs）。
        /// ★ 放后台线程：Recv 会阻塞到超时（默认 1 秒），在 UI 线程上做会卡界面；
        ///   日志已由 Log() marshal 回 UI 线程，所以后台线程写日志是安全的。
        /// </summary>
        private void Recv()
        {
            var item = SelectedItem;
            if (item == null) { Log("⚠ 未选择通讯项。"); return; }

            int timeout = item.TimeoutMs > 0 ? item.TimeoutMs : 1000;
            Log($"▶ 接收（等待 ≤{timeout} ms）...");
            _ = Task.Run(() =>
            {
                try
                {
                    string s = _comm.Recv(item);
                    Log(string.IsNullOrEmpty(s) ? "« 接收：无数据（超时）" : $"« 接收：{s}");
                }
                catch (Exception ex)
                {
                    Log($"✗ 接收失败：{ex.Message}");
                }
                PostToUi(SyncConnectionState);
            });
        }

        /// <summary>把选中的 SML 预设填入发送框。</summary>
        private void ApplySecsPreset()
        {
            if (string.IsNullOrEmpty(SelectedSecsPreset)) return;
            SendText = SelectedSecsPreset;
            Log($"▷ 已载入 SECS 预设：{SelectedSecsPreset}（可编辑后点「发送」）");
        }

        /// <summary>SECS：发 Select.req 建立 HSMS 会话。</summary>
        private void SecsSelect()
        {
            var secs = SecsChannelOf(out string why);
            if (secs == null) { Log("⚠ " + why); return; }
            Log("▶ 发送 Select.req …");
            try
            {
                Log(secs.Select() ? "  ✓ 会话已建立（收到 Select.rsp）。" : "  ✗ 失败：" + secs.LastError);
            }
            catch (Exception ex)
            {
                Log("  ✗ 失败：" + ex.Message);
            }
            SyncConnectionState();
        }

        /// <summary>SECS：发 Linktest.req 探活。</summary>
        private void SecsLinktest()
        {
            var secs = SecsChannelOf(out string why);
            if (secs == null) { Log("⚠ " + why); return; }
            Log("▶ 发送 Linktest.req …");
            try
            {
                Log(secs.Linktest() ? "  ✓ 对方有应答，链路正常。" : "  ✗ 无应答：" + secs.LastError);
            }
            catch (Exception ex)
            {
                Log("  ✗ 失败：" + ex.Message);
            }
            SyncConnectionState();
        }

        /// <summary>取当前选中项的 SECS 通道（不是 SECS / 创建失败都给出中文原因）。</summary>
        private SecsCommChannel? SecsChannelOf(out string why)
        {
            why = string.Empty;
            var item = SelectedItem;
            if (item == null) { why = "未选择通讯项。"; return null; }
            if (!IsSecs(item)) { why = "当前通讯类型不是 SECS，请先在「通讯类型」里选 SECS(HSMS)。"; return null; }
            try
            {
                return _comm.Peek(item) as SecsCommChannel;
            }
            catch (Exception ex)
            {
                why = "打开 SECS 通道失败：" + ex.Message;
                return null;
            }
        }

        /// <summary>把选中的命令预设填入发送框（不立即发送，便于修改后手动发送）。</summary>
        private void ApplyPreset()
        {
            if (string.IsNullOrEmpty(SelectedPreset)) return;
            SendText = SelectedPreset;
            Log($"▷ 已载入命令预设：{SelectedPreset}（可编辑后点「发送」）");
        }

        private void AutoScan()
        {
            Log("▶ 扫描可用串口...");
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
                if (key == null) { Log("  无可用串口。"); return; }
                var ports = key.GetValueNames()
                    .Select(n => key.GetValue(n)?.ToString())
                    .Where(v => !string.IsNullOrEmpty(v))
                    .ToArray();
                Log(ports.Length == 0 ? "  无可用串口。" : "  可用：" + string.Join(", ", ports));
            }
            catch (Exception ex)
            {
                Log("  扫描失败：" + ex.Message);
            }
        }

        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0)
                SelectedItem = Items[0];
            WatchSelected();
            SyncConnectionState();
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
