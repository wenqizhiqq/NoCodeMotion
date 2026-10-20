// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁣志​◆‎编‌写⁠◇‍微⁠信‍﹕‎1⁣8​7‍◆‌1⁣9‍3‍6‏◇‌1‍3‌9⁣9⁣　‏※⁣保‍留⁠所‎有​权​利‎请⁣勿⁠删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 「工程文件被外部改了哪些内容」摘要：把刚从磁盘读出来的 <see cref="ProjectData"/>
    /// 与内存里正在用的逐表比对，产出「哪个表 新增 / 删除 / 修改 了哪几条」的清单，
    /// 给 <see cref="ProjectFileWatcher"/> 的确认弹窗显示 —— 用户点「载入文件」之前得先看见改了啥。
    /// </summary>
    /// <remarks>
    /// 两个必须注意的点：
    /// ① **运行态字段一律跳过**（IO 电平、流程状态、相机连接与上次结果、各表「实际值」读数）。
    ///    这些是软件自己每几百毫秒刷的，不参与工艺定义，拿来比会把「软件在跑」误报成「文件被改了」。
    /// ② **按名称配对，不按行号**。中间插一行会让行号整体错位，逐行比会把后面所有行都判成修改；
    ///    名称才是这些表的主键（软件里的下拉、跨表引用也都按名称）。变量表没有名称列，只能按行序比。
    /// </remarks>
    public static class ProjectChangeSummary
    {
        /// <summary>每类最多列几个名字，再多就收成「…等 N 个」，免得弹窗被几百个名字撑爆。</summary>
        private const int NameCap = 6;

        private static readonly HashSet<string> SkipProps = new(StringComparer.Ordinal)
        {
            "Value", "Status", "ActualValue", "ActualOk", "IsConnected", "LastResult", "LastScore",
        };

        /// <summary>返回一段可直接塞进弹窗的多行文本（无实质改动时返回一句说明）。</summary>
        public static string Build(ProjectData disk, ProjectData mem)
        {
            var lines = new List<string>();

            Sec(lines, "控制器", disk.Controllers, mem.Controllers);
            Sec(lines, "轴", disk.Axes, mem.Axes);
            Sec(lines, "输入IO", disk.Inputs, mem.Inputs);
            Sec(lines, "输出IO", disk.Outputs, mem.Outputs);
            Sec(lines, "气缸", disk.Cylinders, mem.Cylinders);
            Sec(lines, "点位表", disk.PointTables, mem.PointTables);
            Sec(lines, "料盘", disk.Trays, mem.Trays);
            Sec(lines, "相机", disk.Cameras, mem.Cameras);
            Sec(lines, "通讯", disk.Comms, mem.Comms);
            Sec(lines, "流程", disk.Flows, mem.Flows);
            Sec(lines, "自定义页面", disk.DesignerWidgets, mem.DesignerWidgets);
            Sec(lines, "相机标定", disk.Calibrations, mem.Calibrations,
                (x, _) => string.IsNullOrWhiteSpace(x?.CameraName) ? "（空相机）" : x.CameraName);
            // 变量表没有「名称」这一列（一行五组 名称/值），只能按行序配对
            Sec(lines, "变量", disk.Variables, mem.Variables, (_, i) => "第" + (i + 1) + "行");

            if (!string.Equals(disk.Remark ?? "", mem.Remark ?? "", System.StringComparison.Ordinal))
                lines.Add("项目管理：修改 备注");
            if (!string.Equals(disk.RequirementsText ?? "", mem.RequirementsText ?? "", System.StringComparison.Ordinal))
                lines.Add("项目管理：修改 需求文本");

            return lines.Count == 0
                ? "各表内容与软件里一致 —— 文件可能只是被重新保存了一次，没有实质改动。"
                : string.Join("\n", lines);
        }

        private static void Sec<T>(List<string> lines, string title, IEnumerable<T> diskItems, IEnumerable<T> memItems,
                                   Func<T?, int, string>? keyOf = null)
        {
            var d = Pair(diskItems, keyOf);
            var m = Pair(memItems, keyOf);

            var added = d.Keys.Where(k => !m.ContainsKey(k)).ToList();
            var removed = m.Keys.Where(k => !d.ContainsKey(k)).ToList();
            var changed = d.Keys.Where(k => m.ContainsKey(k) && m[k] != d[k]).ToList();

            if (added.Count == 0 && removed.Count == 0 && changed.Count == 0) return;

            var sb = new StringBuilder(title).Append('：');
            var parts = new List<string>();
            if (added.Count > 0) parts.Add("新增 " + added.Count + "（" + Names(added) + "）");
            if (removed.Count > 0) parts.Add("删除 " + removed.Count + "（" + Names(removed) + "）");
            if (changed.Count > 0) parts.Add("修改 " + changed.Count + "（" + Names(changed) + "）");
            sb.Append(string.Join("｜", parts));
            lines.Add(sb.ToString());
        }

        /// <summary>名称 → 内容指纹。同名多行（重名本来就是软件不允许的）合并成一个指纹，保证照样能比出来。</summary>
        private static Dictionary<string, string> Pair<T>(IEnumerable<T> items, Func<T?, int, string>? keyOf)
        {
            var map = new Dictionary<string, string>(System.StringComparer.Ordinal);
            int i = 0;
            foreach (var it in items)
            {
                string key = keyOf != null ? keyOf(it, i) : KeyOf(it);
                string fp = Fingerprint(it);
                map[key] = map.TryGetValue(key, out string? old) ? old + "\n" + fp : fp;
                i++;
            }
            return map;
        }

        private static string KeyOf(object? item)
        {
            switch (item)
            {
                case null: return "（空行）";
                case EditorItemBase e when !string.IsNullOrWhiteSpace(e.Name): return e.Name;
                case EditorItemBase e2: return "（空名）";
                default:
                    var p = item.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
                    var v = p?.GetValue(item) as string;
                    return string.IsNullOrWhiteSpace(v) ? "（无名）" : v!;
            }
        }

        private static string Names(List<string> keys)
        {
            var shown = keys.Take(NameCap).ToList();
            string text = string.Join("、", shown);
            return keys.Count > NameCap ? text + " …等 " + keys.Count + " 个" : text;
        }

        /// <summary>
        /// 把一个对象的可比内容拼成指纹字符串。只用于「两份读数是否相同」，不给人看，格式稳定即可。
        /// <para>★ 规则与 <c>XlsxProjectStore</c> 的导入侧严格对齐：<b>只有「可读 + 可写 + 标量或枚举」的属性
        /// 会被从 xlsx 回填</b>。计算属性（只读，如 <c>ConnectionText</c> / <c>RealText</c>）磁盘上虽然写了列、
        /// 读回来却是默认值，拿它们比会让每个 item 都「被修改」，摘要就成了噪音。
        /// 集合按子项递归（子行 / 点位 / 格子 / 步骤都靠它），子项是标量就直接取值 ——
        /// 否则 <c>double[]</c> 这种会被递归成空指纹，只剩长度可比、内容可比不出来。</para>
        /// </summary>
        private static string Fingerprint(object? item, int depth = 0)
        {
            if (item == null) return "null";
            var sb = new StringBuilder();
            foreach (var p in item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (SkipProps.Contains(p.Name)) continue;
                if (!p.CanRead || p.GetIndexParameters().Length > 0) continue;

                object? v;
                try { v = p.GetValue(item); } catch { continue; }

                if (IsScalar(p.PropertyType))
                {
                    if (!p.CanWrite) continue;                       // 只读计算属性：读盘不回填，不参与比对
                    sb.Append(p.Name).Append('=')
                      .Append(v is string str ? str
                            : System.Convert.ToString(v, CultureInfo.InvariantCulture)).Append(';');
                    continue;
                }

                if (depth >= 2 || !IsCollection(p.PropertyType) || v is not IEnumerable list) continue;

                sb.Append(p.Name).Append('[');
                foreach (var child in list)
                {
                    sb.Append(child != null && IsScalar(child.GetType())
                        ? System.Convert.ToString(child, CultureInfo.InvariantCulture)
                        : Fingerprint(child, depth + 1)).Append('|');
                }
                sb.Append("];");
            }
            return sb.ToString();
        }

        // 与 XlsxProjectStore 的 IsScalar / IsCollection 同口径（那份是 internal 的私有实现，这里只比对用）
        private static readonly HashSet<Type> ScalarTypes = new()
        {
            typeof(string), typeof(bool), typeof(byte), typeof(sbyte), typeof(char),
            typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal), typeof(DateTime), typeof(TimeSpan), typeof(Guid),
        };

        private static bool IsScalar(Type t)
        {
            var u = Nullable.GetUnderlyingType(t);
            if (u != null) t = u;
            return ScalarTypes.Contains(t) || t.IsEnum;
        }

        private static bool IsCollection(Type t)
        {
            if (t == typeof(string)) return false;
            return typeof(IEnumerable).IsAssignableFrom(t)
                   && (t.IsArray || (t.IsGenericType && typeof(IEnumerable).IsAssignableFrom(t)));
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
