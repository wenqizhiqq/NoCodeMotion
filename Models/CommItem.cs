// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁠志⁣◆‌编​写⁣◇​微‌信‎﹕⁣1⁠8‎7⁠◆‍1‎9‍3‍6⁠◇⁠1‌3⁠9‎9⁣　‏※⁣保⁣留⁠所⁣有‎权⁣利‎请‍勿‏删‍除‏◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>通讯配置</summary>
    public class CommItem : EditorItemBase
    {
        private string _commType = "串口";          // 串口 / 网口TCP / 网口UDP / ModbusTCP / ModbusRTU / 相机网口 / SECS(HSMS) / 西门子S7 / 三菱MC
        private string _portOrIp = "COM1";
        private int _baudOrPort = 9600;
        private int _dataBits = 8;
        private string _parity = "无";              // 无 / 奇校验 / 偶校验
        private double _stopBits = 1;               // 1 / 1.5 / 2
        private int _timeoutMs = 1000;

        // ---- SECS/HSMS（SEMI E37 会话 + E5 报文）专用参数：只有 CommType = SECS 时才用，其余类型忽略 ----
        private string _secsRole = "被动";           // 被动 = 设备端（监听本机端口等对方连入）/ 主动 = 主机端（连对方 IP）
        private int _secsDeviceId;                   // 本机 DeviceID（数据消息的会话号），一般 0
        private int _secsT3Ms = 45000;               // T3：等数据应答超时
        private int _secsT5Ms = 10000;               // T5：连接分离后重连 / 重新监听的间隔
        private int _secsT6Ms = 5000;                // T6：等控制应答（Select / Deselect / Linktest）超时
        private int _secsT7Ms = 10000;               // T7：连上后一直不 Select 的容忍时间
        private int _secsT8Ms = 5000;                // T8：网络字符间超时（作为 socket 读写超时）
        private bool _secsAutoReply = true;          // 自动应答 S1F1 / S1F13 / S1F15 / S1F17
        private string _secsMdln = "NoCodeMotion";   // S1F2 / S1F14 里回的机型 MDLN
        private string _secsSoftRev = "1.0.0";       // S1F2 / S1F14 里回的软件版本 SOFTREV

        public string CommType { get => _commType; set => SetField(ref _commType, value); }
        public string PortOrIp { get => _portOrIp; set => SetField(ref _portOrIp, value); }
        public int BaudOrPort { get => _baudOrPort; set => SetField(ref _baudOrPort, value); }
        public int DataBits { get => _dataBits; set => SetField(ref _dataBits, value); }
        public string Parity { get => _parity; set => SetField(ref _parity, value); }
        public double StopBits { get => _stopBits; set => SetField(ref _stopBits, value); }
        public int TimeoutMs { get => _timeoutMs; set => SetField(ref _timeoutMs, value); }

        /// <summary>HSMS 角色：「被动」= 设备端监听，「主动」= 主机端连对方。</summary>
        public string SecsRole { get => _secsRole; set => SetField(ref _secsRole, value); }

        /// <summary>本机 DeviceID（数据消息会话号）。</summary>
        public int SecsDeviceId { get => _secsDeviceId; set => SetField(ref _secsDeviceId, value); }

        /// <summary>T3：等数据应答超时（ms）。</summary>
        public int SecsT3Ms { get => _secsT3Ms; set => SetField(ref _secsT3Ms, value); }

        /// <summary>T5：连接分离后重连 / 重新监听的间隔（ms）。</summary>
        public int SecsT5Ms { get => _secsT5Ms; set => SetField(ref _secsT5Ms, value); }

        /// <summary>T6：等控制应答超时（ms）。</summary>
        public int SecsT6Ms { get => _secsT6Ms; set => SetField(ref _secsT6Ms, value); }

        /// <summary>T7：连上后未 Select 的容忍时间（ms）。</summary>
        public int SecsT7Ms { get => _secsT7Ms; set => SetField(ref _secsT7Ms, value); }

        /// <summary>T8：网络字符间超时（ms，socket 读写超时）。</summary>
        public int SecsT8Ms { get => _secsT8Ms; set => SetField(ref _secsT8Ms, value); }

        /// <summary>是否自动应答 S1F1 / S1F13 / S1F15 / S1F17。</summary>
        public bool SecsAutoReply { get => _secsAutoReply; set => SetField(ref _secsAutoReply, value); }

        /// <summary>S1F2 / S1F14 里回的机型 MDLN。</summary>
        public string SecsMdln { get => _secsMdln; set => SetField(ref _secsMdln, value); }

        /// <summary>S1F2 / S1F14 里回的软件版本 SOFTREV。</summary>
        public string SecsSoftRev { get => _secsSoftRev; set => SetField(ref _secsSoftRev, value); }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
