// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‎启​志⁣◆‏编‍写⁣◇⁣微​信⁣﹕⁠1‍8⁠7⁠◆⁣1‎9‎3‌6⁣◇⁠1‎3‌9‏9‎　​※⁣保‍留⁣所‍有‎权‍利‏请⁠勿‏删⁣除⁣◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Linq;

namespace NoCodeMotion.Models
{
    /// <summary>一个扩展 IO 模块规格：型号名 + 默认输入 / 输出 / 轴数。</summary>
    public sealed class ExpansionModuleSpec
    {
        public string Name { get; }
        public int InIo { get; }
        public int OutIo { get; }
        public int AxisCount { get; }

        public ExpansionModuleSpec(string name, int inIo, int outIo, int axisCount = 0)
        {
            Name = name;
            InIo = inIo;
            OutIo = outIo;
            AxisCount = axisCount;
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// 常用扩展 IO 模块规格目录（CAN / EtherCAT 数字量模块）。
    /// <para>控制卡页里选择型号即自动带出输入 / 输出 / 轴数，用户再填数量即可；
    /// 带出的数值仍可手工微调，以适配实际接线。</para>
    /// </summary>
    public static class ExpansionModuleCatalog
    {
        public static readonly ExpansionModuleSpec[] Modules =
        {
            new ExpansionModuleSpec("通用 8入8出", 8, 8),
            new ExpansionModuleSpec("通用 16入16出", 16, 16),
            new ExpansionModuleSpec("通用 24入", 24, 0),
            new ExpansionModuleSpec("通用 24出", 0, 24),
            new ExpansionModuleSpec("通用 32入", 32, 0),
            new ExpansionModuleSpec("通用 32出", 0, 32),
            new ExpansionModuleSpec("雷赛 IO408（8入8出）", 8, 8),
            new ExpansionModuleSpec("雷赛 ECAT-16IN", 16, 0),
            new ExpansionModuleSpec("雷赛 ECAT-16OUT", 0, 16),
            new ExpansionModuleSpec("雷赛 ECAT-32IN32OUT", 32, 32),
            new ExpansionModuleSpec("恒昱 HY_CAN_In20Out20", 20, 20),
            new ExpansionModuleSpec("研控 MCC-CAN 16入16出", 16, 16),
        };

        /// <summary>按型号名取规格；找不到返回 null（例如用户手填的自定义型号）。</summary>
        public static ExpansionModuleSpec ByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            string n = name.Trim();
            return Modules.FirstOrDefault(m => string.Equals(m.Name, n, StringComparison.OrdinalIgnoreCase));
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
