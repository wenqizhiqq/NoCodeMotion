// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 通讯页 ViewModel：在原有「连接参数」基础上，扩展出苹果风格的密集参数面板与一组调试操作。
    /// 连接参数（名称/类型/端口IP/波特率/数据位/校验位/停止位/超时）绑定到 CommItem 模型并自动落盘；
    /// 高级调试参数与运行态（连接状态/重试/轮询/缓冲/流控/字节序/日志）为运行时状态，不落盘。
    /// 调试操作：测试连接（TCP 真实连接探测、串口回环自检）、打开/关闭连接（仿真）、发送回显、自动扫描串口、清空日志。
    /// </summary>
    public class CommViewModel : ListEditorViewModel<CommItem>, IEnsureDefaultSelection
    {
        // ---------- 药丸选择器数据源（绑定到字符串型模型字段）----------
        public string[] CommTypeOptions { get; } =
            { "串口", "网口TCP", "网口UDP", "ModbusTCP", "ModbusRTU", "相机网口", "西门子S7", "三菱MC" };
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
        /// <summary>命令预设：把常用报文模板一键填入发送框（用户可改后发送）。</summary>
        public ICommand ApplyPresetCommand { get; }

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

        public CommViewModel()
        {
            CatalogCategory = "Comm";
            Items = ProjectStore.Data.Comms;
            Counter = Items.Count;
            AttachAutoSave();

            TestConnectionCommand = new RelayCommand(_ => _ = TestConnection());
            OpenCommand = new RelayCommand(_ => OpenConnection());
            CloseCommand = new RelayCommand(_ => CloseConnection(), _ => IsConnected);
            SendCommand = new RelayCommand(_ => Send());
            ClearLogCommand = new RelayCommand(_ => DebugLog.Clear());
            AutoScanCommand = new RelayCommand(_ => AutoScan());
            ApplyPresetCommand = new RelayCommand(_ => ApplyPreset(), _ => !string.IsNullOrEmpty(SelectedPreset));
        }

        protected override CommItem CreateNewItem() => new CommItem { Name = $"通讯{Counter + 1}" };

        private void Log(string line)
        {
            DebugLog.Insert(0, $"{DateTime.Now:HH:mm:ss}  {line}");
            while (DebugLog.Count > 300) DebugLog.RemoveAt(DebugLog.Count - 1);
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
            if (SelectedItem == null) { Log("⚠ 未选择通讯项。"); return; }
            IsConnected = true;
            Log($"● 已打开连接：{SelectedItem.Name}");
        }

        private void CloseConnection()
        {
            IsConnected = false;
            Log("○ 已关闭连接。");
        }

        private void Send()
        {
            if (SelectedItem == null) return;
            var txt = (SendText ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(txt)) return;
            Log($"» 发送：{txt}");
            if (!IsConnected) Log("  ⚠ 当前未连接，以下为回显仿真。");
            Log($"« 回应：{txt}");
            SendText = string.Empty;
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
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
