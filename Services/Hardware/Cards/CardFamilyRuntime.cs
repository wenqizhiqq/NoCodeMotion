// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using NoCodeMotion.Hardware.Motion;

namespace NoCodeMotion.Services.Hardware.Cards
{
    /// <summary>
    /// 一个卡族的运行期实例集合。
    /// <para>注意 <see cref="NewAxis"/> 是工厂而不是单个实例：</para>
    /// <para>各卡族的 AxisRealization 把 <c>AxisID</c> / <c>AxisWhichCardNo</c> 存在实例字段里，
    /// 而且 GetCardAxisCurrentPosition / CardAxisHomeMove / GetCardAxisCurrentState /
    /// CardAxisLineUnitSpeed 这些方法<b>不带 CardNo / axis 参数</b>，直接读这两个字段。
    /// 所以每个配置的轴必须持有自己的实例，共用一份会把轴号串到一起。</para>
    /// </summary>
    public sealed class CardFamilyRuntime
    {
        /// <summary>所属卡族。</summary>
        public CardFamilyDescriptor Family { get; set; }

        /// <summary>卡级操作（已 new，未初始化）。</summary>
        public MotionCardBase Card { get; set; }

        /// <summary>轴级操作工厂：每个轴取一个新实例。</summary>
        public Func<MotionAxisBase> NewAxis { get; set; }

        /// <summary>主板输入。</summary>
        public DigitalInputBase InIo { get; set; }

        /// <summary>主板输出。</summary>
        public DigitalOutputBase OutIo { get; set; }

        /// <summary>扩展输入（该卡族不支持时为 null）。</summary>
        public ExpansionInputBase ExtIn { get; set; }

        /// <summary>扩展输出（该卡族不支持时为 null）。</summary>
        public ExpansionOutputBase ExtOut { get; set; }

        /// <summary>该卡族是否支持扩展 IO 模块。</summary>
        public bool HasExpansionIo => ExtIn != null && ExtOut != null;
    }
}
