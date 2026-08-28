// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using ClosedXML.Excel;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 通用 Excel 批量编辑：把面板里的行导出为 .xlsx，让用户在 Excel/WPS 里改，
    /// 关闭后由调用方回读并替换面板的 Items。
    /// 列由 T 的公共可写属性决定，表头优先用本类维护的“属性名→中文名”映射。
    /// </summary>
    public static class ExcelBatchEdit
    {
        // 属性名 → 中文表头（按面板类型维护，插入顺序即为列顺序）。
        // 未列出的属性回退到属性名本身。
        private static readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<string, string>> Headers
            = new Dictionary<Type, IReadOnlyDictionary<string, string>>
            {
                [typeof(IoItem)] = new Dictionary<string, string>
                {
                    ["CardType"] = "卡类",
                    ["CardNo"] = "卡号",
                    ["ModuleNo"] = "模块",
                    ["Sequence"] = "序号",
                    ["Name"] = "IO名称",
                    ["SuitCode"] = "套码",
                    ["Level"] = "电平",
                    ["Function"] = "功能",
                    ["Value"] = "值",
                },
                [typeof(VariableRow)] = new Dictionary<string, string>
                {
                    ["Name1"] = "名称1", ["Value1"] = "字符串值1",
                    ["Name2"] = "名称2", ["Value2"] = "字符串值2",
                    ["Name3"] = "名称3", ["Value3"] = "字符串值3",
                    ["Name4"] = "名称4", ["Value4"] = "字符串值4",
                    ["Name5"] = "名称5", ["Value5"] = "字符串值5",
                },
                [typeof(FlowStep)] = new Dictionary<string, string>
                {
                    ["Logic"] = "逻辑",
                    ["Function"] = "功能",
                    ["Name"] = "名称",
                    ["Property"] = "属性",
                    ["Operation"] = "运算",
                    ["SetValue"] = "设置值",
                    ["Timeout"] = "超时",
                    ["DurationMs"] = "耗时(ms)",
                    ["ActualValue"] = "实际值",
                },
            };

        /// <summary>把 items 导出到 %TEMP%/NoCodeMotion/{fileNameHint}_{时间戳}.xlsx，返回文件路径。</summary>
        public static string Export<T>(IEnumerable<T> items, string? fileNameHint = null)
        {
            var dir = Path.Combine(Path.GetTempPath(), "NoCodeMotion");
            Directory.CreateDirectory(dir);
            var safe = string.IsNullOrWhiteSpace(fileNameHint) ? "导出" : fileNameHint!;
            // 过滤文件名中的非法字符
            foreach (var c in Path.GetInvalidFileNameChars()) safe = safe.Replace(c, '_');
            var path = Path.Combine(dir, $"{safe}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            var props = GetExportProps(typeof(T));

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Sheet1");
            for (int c = 0; c < props.Count; c++)
                ws.Cell(1, c + 1).Value = GetHeader(typeof(T), props[c].Name);

            int r = 2;
            foreach (var item in items)
            {
                if (item == null) { r++; continue; }
                for (int c = 0; c < props.Count; c++)
                {
                    var v = props[c].GetValue(item);
                    ws.Cell(r, c + 1).Value = v?.ToString() ?? string.Empty;
                }
                r++;
            }
            if (props.Count > 0) ws.Columns().AdjustToContents();
            wb.SaveAs(path);
            return path;
        }

        /// <summary>从 .xlsx 读回 T 的列表。表头按"中文表头 / 属性名"匹配列（不区分大小写）。</summary>
        public static List<T> Import<T>(string path) where T : new()
        {
            var result = new List<T>();
            var props = GetExportProps(typeof(T));
            var propByHeader = BuildHeaderToPropMap(typeof(T), props);

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheet(1);
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            if (lastRow < 2 || lastCol < 1) return result;

            for (int r = 2; r <= lastRow; r++)
            {
                var item = new T();
                bool any = false;
                for (int c = 1; c <= lastCol; c++)
                {
                    var header = ws.Cell(1, c).GetString().Trim();
                    if (!propByHeader.TryGetValue(header, out var prop) || prop == null) continue;
                    any = true;
                    var cell = ws.Cell(r, c);
                    if (cell.IsEmpty()) continue;
                    var raw = cell.GetString();
                    if (string.IsNullOrEmpty(raw)) continue;
                    try
                    {
                        var converted = Convert.ChangeType(raw, prop.PropertyType, CultureInfo.InvariantCulture);
                        prop.SetValue(item, converted);
                    }
                    catch
                    {
                        // 类型不兼容时跳过该单元格
                    }
                }
                if (any) result.Add(item);
            }
            return result;
        }

        /// <summary>导出列：先按 Headers 字典顺序，再按属性名补齐未列出属性。</summary>
        private static List<PropertyInfo> GetExportProps(Type t)
        {
            var all = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                .ToList();
            if (!Headers.TryGetValue(t, out var headerMap))
                return all.OrderBy(p => p.Name, StringComparer.Ordinal).ToList();

            var ordered = new List<PropertyInfo>();
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (var propName in headerMap.Keys)
            {
                var p = all.FirstOrDefault(x => x.Name == propName);
                if (p != null) { ordered.Add(p); used.Add(propName); }
            }
            foreach (var p in all.Where(x => !used.Contains(x.Name))
                                   .OrderBy(x => x.Name, StringComparer.Ordinal))
                ordered.Add(p);
            return ordered;
        }

        private static string GetHeader(Type t, string propName)
        {
            if (Headers.TryGetValue(t, out var map) && map.TryGetValue(propName, out var h)) return h;
            return propName;
        }

        private static Dictionary<string, PropertyInfo> BuildHeaderToPropMap(Type t, List<PropertyInfo> props)
        {
            var map = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in props)
            {
                var header = GetHeader(t, p.Name);
                if (!map.ContainsKey(header))
                    map[header] = p;
                // 兼容：用户把表头改成英文属性名也能匹配
                map[p.Name] = p;
            }
            return map;
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
