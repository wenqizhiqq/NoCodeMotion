﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 本工程自行实现的 SynchronizedCollection<T> 兼容层。
//   背景: .NET Framework 的 System.Collections.Generic.SynchronizedCollection<T>
//         由 System.ServiceModel 提供，.NET 10 基础类库里并不存在
//         （已核对 Microsoft.NETCore.App.Ref/10.0.2/ref/net10.0，无任何程序集导出它）。
//   用途: 移植进来的 SoftServo 卡家族（SoftServoSDK.cs）用它做通道池。
//   取舍: 为一个集合类型引入整个 System.ServiceModel.Primitives 包不划算，
//         故按实际用到的成员实现等价的最小线程安全集合。
//   位置: 命名空间刻意保持 System.Collections.Generic，使移植过来的
//         SoftServoSDK.cs 可以零改动。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections;

namespace System.Collections.Generic
{
    /// <summary>
    /// 线程安全的集合（与 .NET Framework 同名类型用法兼容）。
    /// 所有读写都在同一把锁下进行；枚举时返回快照，避免“枚举过程中被修改”异常。
    /// </summary>
    public class SynchronizedCollection<T> : IList<T>
    {
        private readonly List<T> _items = new List<T>();
        private readonly object _sync = new object();

        public SynchronizedCollection() { }

        public SynchronizedCollection(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            _items.AddRange(items);
        }

        /// <summary>元素个数。</summary>
        public int Count
        {
            get { lock (_sync) return _items.Count; }
        }

        public bool IsReadOnly => false;

        /// <summary>按索引读写元素。</summary>
        public T this[int index]
        {
            get { lock (_sync) return _items[index]; }
            set { lock (_sync) _items[index] = value; }
        }

        public void Add(T item)
        {
            lock (_sync) _items.Add(item);
        }

        public void Insert(int index, T item)
        {
            lock (_sync) _items.Insert(index, item);
        }

        public bool Remove(T item)
        {
            lock (_sync) return _items.Remove(item);
        }

        public void RemoveAt(int index)
        {
            lock (_sync) _items.RemoveAt(index);
        }

        public void Clear()
        {
            lock (_sync) _items.Clear();
        }

        public bool Contains(T item)
        {
            lock (_sync) return _items.Contains(item);
        }

        public int IndexOf(T item)
        {
            lock (_sync) return _items.IndexOf(item);
        }

        public void CopyTo(T[] array, int index)
        {
            lock (_sync) _items.CopyTo(array, index);
        }

        /// <summary>枚举时对当前内容取快照，允许并发修改。</summary>
        public IEnumerator<T> GetEnumerator()
        {
            List<T> snapshot;
            lock (_sync) snapshot = new List<T>(_items);
            return snapshot.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
