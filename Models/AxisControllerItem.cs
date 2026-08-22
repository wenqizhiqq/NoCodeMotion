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
        public string Description { get => _description; set => SetField(ref _description, value); }

        /// <summary>类型：控制卡（运动控制卡）或 扩展IO（IO 扩展模块）。</summary>
        public string Kind { get => _kind; set => SetField(ref _kind, value); }
    }
}
