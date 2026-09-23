// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‎启​志⁣◆‏编‍写⁣◇⁣微​信⁣﹕⁠1‍8⁠7⁠◆⁣1‎9‎3‌6⁣◇⁠1‎3‌9‏9‎　​※⁣保‍留⁣所‍有‎权‍利‏请⁠勿‏删⁣除⁣◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>
    /// 控制器（运动控制卡）。轴页面可选择一个控制器作为归属。
    /// <para>扩展 IO 不是独立的顶级控制器，而是挂在控制卡内的 <see cref="ExpansionModules"/> 子模块集合；
    /// 主板 IO 数（<see cref="InIoCount"/> / <see cref="OutIoCount"/>）加上各扩展模块带出的 IO，
    /// 由 ViewModel 汇总为输入 / 输出 IO 总数，轴总数取卡自身轴数加扩展模块附加轴数。</para>
    /// </summary>
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

        /// <summary>类型：控制卡（运动控制卡）或 扩展IO（历史工程遗留的独立 IO 项）。</summary>
        public string Kind { get => _kind; set => SetField(ref _kind, value); }

        // ============ IO 数量（主板 + 扩展模块） ============

        /// <summary>主板自带输入 IO 数（卡本体，不含扩展模块）。</summary>
        private int _inIoCount;
        public int InIoCount { get => _inIoCount; set => SetField(ref _inIoCount, value); }

        /// <summary>主板自带输出 IO 数（卡本体，不含扩展模块）。</summary>
        private int _outIoCount;
        public int OutIoCount { get => _outIoCount; set => SetField(ref _outIoCount, value); }

        /// <summary>
        /// 挂在控制卡下的扩展 IO 模块集合（CAN / EtherCAT 模块）。
        /// <para>作为子集合随控制卡一起落盘（xlsx 子表「控制器.扩展模块」），无需手工序列化。</para>
        /// <para>带 set 是为了让反射式导入能识别为可写集合（导入实际是 Clear + Add，不替换实例）。</para>
        /// </summary>
        public ObservableCollection<ExpansionModuleItem> ExpansionModules { get; set; } = new();
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
