// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>
    /// 卡族模块基类：一个模块 = 一个卡族的全部登记信息。
    /// <para><b>新增一种控制卡</b>：在 <c>Cards/Families</c> 下放入该卡族的实现类（或复用已有的），
    /// 然后在 <c>Cards/Modules</c> 下新增一个本类的子类，填好 <see cref="Describe"/> 的元数据即可 ——
    /// 目录、UI 下拉、自动识别、匹配逻辑都会自动带上它，不需要改任何其它文件。</para>
    /// <para><b>删除一种控制卡</b>：删掉对应的模块文件（以及 <c>Cards/Families</c> 下不再需要的目录）。</para>
    /// <para>实现类按约定命名时，<see cref="CardFamilyDescriptor.Create"/> 无需赋值，
    /// 由 <see cref="CardFamilyRegistry"/> 调 <see cref="CardFamilyFactory"/> 自动生成。</para>
    /// </summary>
    public abstract class CardFamilyModule
    {
        /// <summary>产出该卡族的静态描述。</summary>
        public abstract CardFamilyDescriptor Describe();
    }
}
