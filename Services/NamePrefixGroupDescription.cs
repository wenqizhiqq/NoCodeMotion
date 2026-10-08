// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁣启‎志‌◆⁠编‍写‏◇‎微‍信‏﹕‍1‏8⁣7‍◆⁠1‌9​3‌6⁣◇‍1‏3‎9‍9⁣　⁣※‍保⁠留⁠所‏有⁠权‏利⁠请⁠勿​删​除‍◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.ComponentModel;
using System.Globalization;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 名称分组依据：按名称里第一个连字符之前的部分分类（「下料-x」「下料-y」→ 分类「下料」）。
    /// 名称里没有连字符（或连字符在首位、前缀为空）时归入 <see cref="FallbackGroupName"/>。
    /// </summary>
    /// <remarks>
    /// 为什么不用 PropertyGroupDescription：条目本身就是 string，没有可反射的属性可写；
    /// 而且分组后条目类型必须**仍然**是 string —— 一旦包成新对象，
    /// 所有 SelectedItem 到 string 属性的双向绑定会全部失配（下拉显示空白、写回 null）。
    /// </remarks>
    public sealed class NamePrefixGroupDescription : GroupDescription
    {
        /// <summary>截不出前缀时的兜底分类名。</summary>
        public const string FallbackGroupName = "其他";

        // 半角 / 全角连字符都认：中文输入法下很容易打出全角「－」，
        // 只认半角会静默地一个分类都截不出来（下拉看起来完全没变化）。
        private static readonly char[] Separators = { '-', '\uff0d' };

        /// <summary>取名称的分类前缀；取不到返回 null。</summary>
        public static string? PrefixOf(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            int i = name.IndexOfAny(Separators);
            if (i <= 0) return null;                 // 没有连字符，或连字符在首位（前缀为空）
            string p = name.Substring(0, i).Trim();
            return p.Length == 0 ? null : p;
        }

        /// <inheritdoc/>
        public override object GroupNameFromItem(object item, int level, CultureInfo culture)
            => PrefixOf(item as string) ?? FallbackGroupName;
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
