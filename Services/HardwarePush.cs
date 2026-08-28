// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
namespace NoCodeMotion.Services
{
    /// <summary>
    /// 配置页「改值实时下发设备」的总开关与判定。
    /// 默认关闭；挂载真实硬件桥（非桩）且用户开启在线模式后，配置变更会推送到设备。
    /// 仅下发非运动类参数（速度 / 输出电平 / 使能等），避免编辑坐标误触发轴运动。
    /// </summary>
    public static class HardwarePush
    {
        /// <summary>是否处于在线下发模式。开启后，配置页改值在提交时会下发到设备。</summary>
        public static bool Online { get; set; }

        /// <summary>当前是否应下发：在线模式 且 已挂载真实桥（非桩）。</summary>
        public static bool ShouldPush => Online && !(HardwareBridge.Current is StubHardwareBridge);
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
