// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
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
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
