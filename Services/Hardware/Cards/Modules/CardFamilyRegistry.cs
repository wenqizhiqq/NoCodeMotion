// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace NoCodeMotion.Services.Hardware.Cards.Modules
{
    /// <summary>
    /// 卡族模块注册表：启动时反射扫描本程序集内全部 <see cref="CardFamilyModule"/> 子类，
    /// 实例化并缓存它们产出的 <see cref="CardFamilyDescriptor"/>。
    /// <para>没有任何手写清单 —— 本目录下的模块文件就是唯一事实来源。</para>
    /// </summary>
    public static class CardFamilyRegistry
    {
        private static readonly IReadOnlyList<CardFamilyDescriptor> _all = Load();

        /// <summary>全部已登记卡族（按品牌顺序、同品牌按家族键排序）。</summary>
        public static IReadOnlyList<CardFamilyDescriptor> All => _all;

        private static IReadOnlyList<CardFamilyDescriptor> Load()
        {
            var list = new List<CardFamilyDescriptor>();

            foreach (var type in CardFamilyFactory.EnumerateAssemblyTypes(typeof(CardFamilyRegistry).Assembly)
                         .Where(t => t != null && !t.IsAbstract && typeof(CardFamilyModule).IsAssignableFrom(t))
                         .OrderBy(t => t.FullName, StringComparer.Ordinal))
            {
                var module = (CardFamilyModule)Activator.CreateInstance(type);
                var descriptor = module.Describe();
                if (descriptor == null)
                    throw new InvalidOperationException($"卡族模块 {type.FullName} 的 Describe() 返回了 null。");
                if (string.IsNullOrWhiteSpace(descriptor.Key))
                    throw new InvalidOperationException($"卡族模块 {type.FullName} 没有登记 Key。");

                // 实现类按约定命名时，模块只写元数据；工厂在这里统一补齐。
                if (descriptor.Create == null)
                    descriptor.Create = CardFamilyFactory.ByConvention(descriptor.Namespace);

                list.Add(descriptor);
            }

            var duplicate = list.GroupBy(d => d.Key, StringComparer.OrdinalIgnoreCase)
                                .FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null)
                throw new InvalidOperationException(
                    $"卡族键重复：「{duplicate.Key}」被 {duplicate.Count()} 个模块登记，请只保留一个。");

            var vendorOrder = CardFamilyCatalog.Vendors;
            return list.OrderBy(d => VendorRank(d.Vendor, vendorOrder))
                       .ThenBy(d => d.SortOrder <= 0 ? int.MaxValue : d.SortOrder)
                       .ThenBy(d => d.Key, StringComparer.Ordinal)
                       .ToList();
        }

        private static int VendorRank(string vendor, string[] order)
        {
            int index = Array.IndexOf(order, vendor);
            return index < 0 ? order.Length : index;
        }
    }
}
