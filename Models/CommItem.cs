// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>通讯配置</summary>
    public class CommItem : EditorItemBase
    {
        private string _commType = "串口";          // 串口 / 网口TCP / 网口UDP / ModbusTCP / ModbusRTU / 相机网口 / 西门子S7 / 三菱MC
        private string _portOrIp = "COM1";
        private int _baudOrPort = 9600;
        private int _dataBits = 8;
        private string _parity = "无";              // 无 / 奇校验 / 偶校验
        private double _stopBits = 1;               // 1 / 1.5 / 2
        private int _timeoutMs = 1000;

        public string CommType { get => _commType; set => SetField(ref _commType, value); }
        public string PortOrIp { get => _portOrIp; set => SetField(ref _portOrIp, value); }
        public int BaudOrPort { get => _baudOrPort; set => SetField(ref _baudOrPort, value); }
        public int DataBits { get => _dataBits; set => SetField(ref _dataBits, value); }
        public string Parity { get => _parity; set => SetField(ref _parity, value); }
        public double StopBits { get => _stopBits; set => SetField(ref _stopBits, value); }
        public int TimeoutMs { get => _timeoutMs; set => SetField(ref _timeoutMs, value); }
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
