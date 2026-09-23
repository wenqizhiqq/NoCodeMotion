// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‎启​志⁣◆‏编‍写⁣◇⁣微​信⁣﹕⁠1‍8⁠7⁠◆⁣1‎9‎3‌6⁣◇⁠1‎3‌9‏9‎　​※⁣保‍留⁣所‍有‎权‍利‏请⁠勿‏删⁣除⁣◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
namespace NoCodeMotion.Models
{
    /// <summary>
    /// 控制卡内挂接的一个扩展 IO 模块（CAN / EtherCAT 模块）。
    /// <para>挂在 <see cref="AxisControllerItem.ExpansionModules"/> 下，用户只需要「选模块型号 + 填数量」，
    /// 每模块的输入 / 输出 / 轴数由 <see cref="ExpansionModuleCatalog"/> 自动带出，再由控制卡汇总。</para>
    /// </summary>
    public class ExpansionModuleItem : EditorItemBase
    {
        private string _moduleType = "通用 16入16出";  // 模块型号（来自 ExpansionModuleCatalog）
        private int _count = 1;                         // 该型号并联的模块数量
        private int _inIo = 16;                         // 单模块输入 IO 数
        private int _outIo = 16;                        // 单模块输出 IO 数
        private int _axisCount;                         // 单模块额外提供的轴数（多数 IO 模块为 0）

        /// <summary>模块型号 / 名称。</summary>
        public string ModuleType { get => _moduleType; set => SetField(ref _moduleType, value); }

        /// <summary>该型号模块的并联数量。</summary>
        public int Count { get => _count; set => SetField(ref _count, value); }

        /// <summary>单模块输入 IO 数（选择型号后自动带出，可改）。</summary>
        public int InIo { get => _inIo; set => SetField(ref _inIo, value); }

        /// <summary>单模块输出 IO 数（选择型号后自动带出，可改）。</summary>
        public int OutIo { get => _outIo; set => SetField(ref _outIo, value); }

        /// <summary>单模块额外提供的轴数（选择型号后自动带出，可改；多数 IO 模块为 0）。</summary>
        public int AxisCount { get => _axisCount; set => SetField(ref _axisCount, value); }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
