// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>控制器（控制卡 / 扩展IO）。轴页面可选择一个控制器作为归属。</summary>
    public class AxisControllerItem : EditorItemBase
    {
        // 基本信息
        private string _kind = "控制卡";          // 控制卡 / 扩展IO
        private string _vendor = "雷赛";          // 雷赛 / 固高 / 虚拟 / 自定义
        private string _cardType = "DMC";         // 卡型号，如 DMC5410 / EtherCAT 主站
        private int _cardNo;                      // 卡号 / 索引
        private int _axisCount = 4;               // 该控制器可管理的轴数量
        private string _connection = "PCI";       // 连接方式：PCI / 网口 / COM / EtherCAT
        private string _description = "";         // 备注

        public string Vendor { get => _vendor; set => SetField(ref _vendor, value); }
        public string CardType { get => _cardType; set => SetField(ref _cardType, value); }
        public int CardNo { get => _cardNo; set => SetField(ref _cardNo, value); }
        public int AxisCount { get => _axisCount; set => SetField(ref _axisCount, value); }
        public string Connection { get => _connection; set => SetField(ref _connection, value); }

        /// <summary>总线类型：脉冲 / EtherCAT / CANopen / Modbus / Profinet / 其它。脉冲卡按脉冲输出驱动，总线卡走实时以太网 / 现场总线。</summary>
        private string _busType = "脉冲";
        public string BusType { get => _busType; set => SetField(ref _busType, value); }

        public string Description { get => _description; set => SetField(ref _description, value); }

        /// <summary>类型：控制卡（运动控制卡）或 扩展IO（IO 扩展模块）。</summary>
        public string Kind { get => _kind; set => SetField(ref _kind, value); }
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
