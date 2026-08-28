// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System.Linq;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 非 Lua 路径（操作员页 / 流程页 / 配置实时下发）共用的设备名称解析。
    /// 与 HardwareApi 的区别：本类不抛异常，找不到返回 null，由调用方决定如何提示，
    /// 避免操作员/流程运行因某个名称缺失而整体崩溃。
    /// </summary>
    public static class HardwareResolver
    {
        public static AxisItem ResolveAxis(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return ProjectStore.Data.Axes.FirstOrDefault(a => a.Name == name);
        }

        public static IoItem ResolveInput(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return ProjectStore.Data.Inputs.FirstOrDefault(i => i.Name == name);
        }

        public static IoItem ResolveOutput(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return ProjectStore.Data.Outputs.FirstOrDefault(i => i.Name == name);
        }

        public static CylinderItem ResolveCylinder(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return ProjectStore.Data.Cylinders.FirstOrDefault(c => c.Name == name);
        }

        public static CommItem ResolveComm(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return ProjectStore.Data.Comms.FirstOrDefault(c => c.Name == name);
        }

        public static TrayItem ResolveTray(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return ProjectStore.Data.Trays.FirstOrDefault(t => t.Name == name);
        }

        public static PointTable ResolvePointTable(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return ProjectStore.Data.PointTables.FirstOrDefault(t => t.Name == name);
        }
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
