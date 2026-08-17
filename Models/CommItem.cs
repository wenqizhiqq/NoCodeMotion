using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>通讯配置</summary>
    public class CommItem : EditorItemBase
    {
        private string _commType = "串口";          // 串口 / 网口TCP
        private string _portOrIp = "COM1";
        private int _baudOrPort = 9600;
        private int _timeoutMs = 1000;

        public string CommType { get => _commType; set => SetField(ref _commType, value); }
        public string PortOrIp { get => _portOrIp; set => SetField(ref _portOrIp, value); }
        public int BaudOrPort { get => _baudOrPort; set => SetField(ref _baudOrPort, value); }
        public int TimeoutMs { get => _timeoutMs; set => SetField(ref _timeoutMs, value); }
    }
}
