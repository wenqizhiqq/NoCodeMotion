// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NoCodeMotion.Hardware.Motion;

namespace NoCodeMotion.Services.Hardware.Cards
{
    /// <summary>
    /// 按命名约定创建卡族实现类，替代原先每个卡族手写的 4~6 个 lambda。
    /// <para>约定（与移植进来的卡族目录一一对应）：</para>
    /// <list type="bullet">
    /// <item><c>CardRealization</c> : MotionCardBase —— 必有</item>
    /// <item><c>AxisRealization</c> : MotionAxisBase —— 必有</item>
    /// <item><c>InioRealization</c> : DigitalInputBase —— 必有</item>
    /// <item><c>OutioRealization</c> : DigitalOutputBase —— 必有</item>
    /// <item><c>ExpandInioRealizetion</c> : ExpansionInputBase —— 可选</item>
    /// <item><c>ExpandOutioRealizetion</c> : ExpansionOutputBase —— 可选</item>
    /// </list>
    /// <para>缺扩展 IO 类时不报错，运行期 <see cref="CardFamilyRuntime.HasExpansionIo"/> 为 false。</para>
    /// </summary>
    internal static class CardFamilyFactory
    {
        /// <summary>程序集内全部类型（FullName → Type）。只建立一次，避免每次 new 卡都反射扫描。</summary>
        private static readonly Lazy<Dictionary<string, Type>> TypesByFullName =
            new Lazy<Dictionary<string, Type>>(BuildTypeIndex);

        private static Dictionary<string, Type> BuildTypeIndex()
        {
            var map = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var type in EnumerateAssemblyTypes(typeof(CardFamilyFactory).Assembly))
            {
                if (type == null || type.FullName == null) continue;
                map[type.FullName] = type;
            }
            return map;
        }

        /// <summary>安全枚举程序集类型：个别类型加载失败（缺驱动程序集）时返回已加载的那部分。</summary>
        internal static IEnumerable<Type> EnumerateAssemblyTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }

        /// <summary>按命名空间生成「new 出一整套实现类」的工厂。</summary>
        public static Func<CardFamilyRuntime> ByConvention(string ns) => () => new CardFamilyRuntime
        {
            Card = Create<MotionCardBase>(ns, "CardRealization"),
            NewAxis = () => Create<MotionAxisBase>(ns, "AxisRealization"),
            InIo = Create<DigitalInputBase>(ns, "InioRealization"),
            OutIo = Create<DigitalOutputBase>(ns, "OutioRealization"),
            ExtIn = TryCreate<ExpansionInputBase>(ns, "ExpandInioRealizetion"),
            ExtOut = TryCreate<ExpansionOutputBase>(ns, "ExpandOutioRealizetion"),
        };

        private static T Create<T>(string ns, string className) where T : class
        {
            Type type = ResolveType(ns, className);
            if (type == null)
                throw new InvalidOperationException(
                    $"卡族实现缺失：命名空间「{ns}」下找不到 {className}（应继承 {typeof(T).Name}）。"
                    + "请检查卡族模块登记的 Namespace 是否正确。");
            return (T)Activator.CreateInstance(type);
        }

        private static T TryCreate<T>(string ns, string className) where T : class
        {
            Type type = ResolveType(ns, className);
            return type == null ? null : (T)Activator.CreateInstance(type);
        }

        private static Type ResolveType(string ns, string className)
        {
            if (string.IsNullOrWhiteSpace(ns)) return null;
            string fullName = ns.Trim() + "." + className;
            TypesByFullName.Value.TryGetValue(fullName, out Type type);
            return type;
        }
    }
}
