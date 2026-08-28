// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using ClosedXML.Excel;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 工程 xlsx 存储基础件（明文实现，供加密的 ProjectStore / ProjectManager 调用）。
    /// 约定：每个工程 = &lt;see cref="ProjectsRoot"/&gt;\&lt;工程名&gt;.xlsx；
    ///       按菜单页分页，每个菜单页写入一个 worksheet（分页保存），xlsx 为唯一存储，不使用 JSON。
    ///
    /// 为规避「无法读取加密的 ProjectData 字段结构」这一限制，本类采用反射式通用读写：
    ///   - 导出：遍历根对象 public 属性，集合属性 -> 一张表（列=元素类型的标量属性），
    ///           标量/枚举属性 -> 汇聚到「项目管理」信息表；
    ///   - 导入：按表名(=属性名)反射清空并重建集合，按列名匹配属性名回填（含类型转换）。
    /// 调用方（ProjectStore）只需调用 <see cref="SaveProject"/> / <see cref="OpenProject"/> 即可，
    /// 无需关心具体字段。中文 sheet 名通过 <see cref="SheetNameOverrides"/> / <see cref="ChildSheetNameOverrides"/> 对齐菜单。
    ///
    /// 菜单页 -> sheet 映射（与顶部菜单一致）：
    ///   项目管理 / 控制器 / 轴 / IO / 气缸 / 点位表 / 通讯 / 料盘 / 相机 / 变量 / 流程 / 工程师 / 操作员
    /// 其中 IO 由 Inputs+Outputs 合并（带「类型」列）；点位表/料盘/流程 含嵌套子表（父项名称 列关联）。
    /// </summary>
    public static class XlsxProjectStore
    {
        /// <summary>
        /// 固定工程目录：程序输出 bin\projects\（从 exe 所在目录上溯到名为 bin 的文件夹，再拼接 projects）。
        /// 工程全部以 xlsx 单文件存储，不使用 JSON。
        /// </summary>
        public static string ProjectsRoot => ResolveProjectsRoot();

        private static string ResolveProjectsRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !string.Equals(dir.Name, "bin", StringComparison.OrdinalIgnoreCase))
                dir = dir.Parent;
            var bin = dir?.FullName ?? AppContext.BaseDirectory;
            return Path.Combine(bin, "projects");
        }

        /// <summary>属性名 -> 中文 sheet 名的覆盖表（对齐菜单页）。</summary>
        public static readonly Dictionary<string, string> SheetNameOverrides = new()
        {
            ["Axes"] = "轴",
            ["Controllers"] = "控制器",
            ["Cylinders"] = "气缸",
            ["Comms"] = "通讯",
            ["Trays"] = "料盘",
            ["Flows"] = "流程",
            ["PointTables"] = "点位表",
            ["Points"] = "点位",
            ["PointAxes"] = "点位轴",
            ["Inputs"] = "输入",
            ["Outputs"] = "输出",
            ["Variables"] = "变量",
            ["Io"] = "IO",
        };

        /// <summary>父表属性名 -> 嵌套子表的中文名后缀（最终 sheet = 父sheet + "." + 此后缀）。</summary>
        public static readonly Dictionary<string, string> ChildSheetNameOverrides = new()
        {
            ["Steps"] = "步骤",
            ["Cells"] = "格子",
            ["Points"] = "点位",
            ["AxisNames"] = "轴名",
        };

        /// <summary>顶部菜单页顺序（用于文档/占位对齐，不强制建空表）。</summary>
        public static readonly string[] MenuSheetNames =
        {
            "项目管理", "控制器", "轴", "IO", "气缸", "点位表", "通讯",
            "料盘", "相机", "变量", "流程", "工程师", "操作员",
        };

        /// <summary>导出/导入时跳过的属性（Io 为 Inputs+Outputs 的计算合并属性，避免重复存储）。</summary>
        private static readonly HashSet<string> ExcludedProperties = new() { "Io" };

        /// <summary>合并进「IO」表的属性（Inputs / Outputs）。</summary>
        private static readonly HashSet<string> MergedIoProperties = new() { "Inputs", "Outputs" };

        private static readonly HashSet<Type> ScalarTypes = new()
        {
            typeof(string), typeof(bool), typeof(byte), typeof(sbyte), typeof(char),
            typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal), typeof(DateTime), typeof(TimeSpan), typeof(Guid)
        };

        public static void EnsureProjectsFolder()
        {
            if (!Directory.Exists(ProjectsRoot))
                Directory.CreateDirectory(ProjectsRoot);
        }

        public static string FilePathFor(string projectName)
        {
            EnsureProjectsFolder();
            var safe = string.Join("_", projectName.Split(Path.GetInvalidFileNameChars()));
            if (string.IsNullOrWhiteSpace(safe)) safe = "未命名工程";
            return Path.Combine(ProjectsRoot, safe + ".xlsx");
        }

        public static bool Exists(string projectName) => File.Exists(FilePathFor(projectName));

        /// <summary>将若干 sheet 写入工程的 xlsx（覆盖式保存）。</summary>
        public static void Save(string projectName, IDictionary<string, DataTable> sheets)
        {
            var path = FilePathFor(projectName);
            EnsureProjectsFolder();
            using var wb = new XLWorkbook();
            foreach (var kv in sheets)
            {
                var dt = kv.Value;
                var ws = wb.Worksheets.Add(SafeSheetName(kv.Key));
                for (int c = 0; c < dt.Columns.Count; c++)
                    ws.Cell(1, c + 1).Value = dt.Columns[c].ColumnName;
                for (int r = 0; r < dt.Rows.Count; r++)
                    for (int c = 0; c < dt.Columns.Count; c++)
                        ws.Cell(r + 2, c + 1).Value = dt.Rows[r][c]?.ToString() ?? string.Empty;
                ws.Columns().AdjustToContents();
            }
            wb.SaveAs(path);
        }

        /// <summary>从工程的 xlsx 读取全部 sheet（sheet 名 -> DataTable）。文件不存在返回空字典。</summary>
        public static IDictionary<string, DataTable> Load(string projectName)
        {
            var path = FilePathFor(projectName);
            var result = new Dictionary<string, DataTable>();
            if (!File.Exists(path)) return result;
            using var wb = new XLWorkbook(path);
            foreach (var ws in wb.Worksheets)
            {
                var dt = new DataTable(ws.Name);
                var firstRow = ws.FirstRowUsed();
                if (firstRow == null) continue;
                int colCount = ws.LastCellUsed()?.Address.ColumnNumber ?? 0;
                for (int c = 1; c <= colCount; c++)
                    dt.Columns.Add(firstRow.Cell(c).GetString());
                foreach (var row in ws.RowsUsed().Skip(1))
                {
                    var dr = dt.NewRow();
                    for (int c = 1; c <= colCount; c++)
                        dr[c - 1] = row.Cell(c).GetString();
                    dt.Rows.Add(dr);
                }
                result[ws.Name] = dt;
            }
            return result;
        }

        /// <summary>列出 projects 目录下所有工程名（不含扩展名）。</summary>
        public static IEnumerable<string> ListProjects()
        {
            EnsureProjectsFolder();
            return Directory.EnumerateFiles(ProjectsRoot, "*.xlsx")
                            .Select(p => Path.GetFileNameWithoutExtension(p))
                            .OrderBy(x => x);
        }

        public static void Delete(string projectName)
        {
            var path = FilePathFor(projectName);
            if (File.Exists(path)) File.Delete(path);
        }

        // ===================== 反射式通用导出 / 导入 =====================

        /// <summary>把根对象（ProjectData）导出为多 sheet 字典：集合各成表，标量汇入「项目管理」，IO 合并，嵌套子表单独建表。</summary>
        public static IDictionary<string, DataTable> ExportToDataTables(object root, string? projectName = null)
        {
            var dict = new Dictionary<string, DataTable>();
            var meta = new DataTable("项目管理");
            meta.Columns.Add("属性", typeof(string));
            meta.Columns.Add("值", typeof(string));
            if (!string.IsNullOrEmpty(projectName))
                meta.Rows.Add("工程名称", projectName);

            foreach (var p in root.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!p.CanRead) continue;
                if (ExcludedProperties.Contains(p.Name)) continue;

                if (IsCollection(p.PropertyType))
                {
                    if (MergedIoProperties.Contains(p.Name)) continue; // IO 统一在 BuildIoSheet 处理
                    var itemType = CollectionItemType(p.PropertyType);
                    if (itemType == null) continue;
                    var sheetName = SheetKey(p.Name);
                    if (IsScalar(itemType))
                    {
                        var sdt = new DataTable(sheetName);
                        sdt.Columns.Add("值", typeof(string));
                        if (p.GetValue(root) is IEnumerable sEnum)
                            foreach (var s in sEnum) sdt.Rows.Add(s?.ToString() ?? "");
                        dict[sheetName] = sdt;
                    }
                    else
                    {
                        dict[sheetName] = CollectionToTable(p.GetValue(root), itemType);
                    }
                }
                else if (IsScalar(p.PropertyType) || p.PropertyType.IsEnum)
                {
                    meta.Rows.Add(p.Name, p.GetValue(root)?.ToString() ?? "");
                }
            }

            // 嵌套子表（点位表.点位 / 点位表.轴名 / 料盘.格子 / 流程.步骤 ...）
            foreach (var kv in CollectNestedSheets(root))
                dict[kv.Key] = kv.Value;

            // IO 合并表
            dict["IO"] = BuildIoSheet(root);

            if (meta.Rows.Count > 0) dict["项目管理"] = meta;
            return dict;
        }

        /// <summary>从多 sheet 字典反射回填到根对象（ProjectData）。集合按 sheet 名匹配属性名后清空重建；标量回填「项目管理」信息表；嵌套子表按父项名称关联。</summary>
        public static void ImportFromDataTables(object root, IDictionary<string, DataTable> tables)
        {
            var props = root.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.CanRead && p.CanWrite).ToArray();
            var byName = props.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            // 1) 顶层集合（含字符串列表）与标量
            foreach (var p in props)
            {
                if (!IsCollection(p.PropertyType)) continue;
                if (ExcludedProperties.Contains(p.Name) || MergedIoProperties.Contains(p.Name)) continue;
                var itemType = CollectionItemType(p.PropertyType);
                if (itemType == null) continue;
                if (!tables.TryGetValue(SheetKey(p.Name), out var dt)) continue;

                var coll = p.GetValue(root);
                var clear = p.PropertyType.GetMethod("Clear");
                var add = p.PropertyType.GetMethod("Add");
                if (clear != null) clear.Invoke(coll, null);
                if (add == null) continue;

                if (IsScalar(itemType))
                {
                    var col = dt.Columns[0].ColumnName;
                    foreach (DataRow dr in dt.Rows)
                        add.Invoke(coll, new object[] { ConvertTo(itemType, dr[col]?.ToString())! });
                }
                else
                {
                    var itemProps = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                            .Where(x => x.CanRead && x.CanWrite && (IsScalar(x.PropertyType) || x.PropertyType.IsEnum))
                                            .ToArray();
                    foreach (DataRow dr in dt.Rows)
                    {
                        var item = Activator.CreateInstance(itemType)!;
                        foreach (var pp in itemProps)
                        {
                            if (!dt.Columns.Contains(pp.Name)) continue;
                            try { pp.SetValue(item, ConvertTo(pp.PropertyType, dr[pp.Name]?.ToString())); }
                            catch { /* 跳过无法转换的字段，保持默认值 */ }
                        }
                        add.Invoke(coll, new object[] { item });
                    }
                }
            }

            // 2) IO 拆分
            if (tables.TryGetValue("IO", out var ioDt))
                SplitIoToRoot(root, ioDt);

            // 3) 嵌套子表还原
            RestoreNestedCollections(root, tables);

            // 4) 标量（项目管理信息表）
            if (tables.TryGetValue("项目管理", out var meta))
                foreach (DataRow dr in meta.Rows)
                {
                    var name = dr["属性"]?.ToString();
                    var val = dr["值"]?.ToString();
                    if (name != null && byName.TryGetValue(name, out var sp)
                        && (IsScalar(sp.PropertyType) || sp.PropertyType.IsEnum))
                    {
                        try { sp.SetValue(root, ConvertTo(sp.PropertyType, val)); } catch { }
                    }
                }
        }

        /// <summary>便捷：直接把根对象保存为工程 xlsx（导出+写文件）。</summary>
        public static void SaveProject(object root, string projectName)
            => Save(projectName, ExportToDataTables(root, projectName));

        /// <summary>便捷：从工程 xlsx 反射回填根对象（读文件+导入）。</summary>
        public static void OpenProject(object root, string projectName)
            => ImportFromDataTables(root, Load(projectName));

        // ===================== 嵌套子表 =====================

        private static IDictionary<string, DataTable> CollectNestedSheets(object root)
        {
            var result = new Dictionary<string, DataTable>();
            foreach (var p in root.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!p.CanRead || ExcludedProperties.Contains(p.Name) || MergedIoProperties.Contains(p.Name)) continue;
                if (!IsCollection(p.PropertyType)) continue;
                var itemType = CollectionItemType(p.PropertyType);
                if (itemType == null || IsScalar(itemType)) continue;

                var parentColl = p.GetValue(root) as IEnumerable;
                if (parentColl == null) continue;
                var parentSheet = SheetKey(p.Name);

                foreach (var cp in itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!cp.CanRead || !IsCollection(cp.PropertyType)) continue;
                    var childItemType = CollectionItemType(cp.PropertyType);
                    if (childItemType == null) continue;
                    var childSheet = parentSheet + "." + ChildSheetKey(cp.Name);

                    var dt = new DataTable(childSheet);
                    dt.Columns.Add("父项名称", typeof(string));
                    if (IsScalar(childItemType))
                        dt.Columns.Add(ChildSheetKey(cp.Name), typeof(string));
                    else
                        foreach (var cpp in childItemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                    .Where(x => x.CanRead && (IsScalar(x.PropertyType) || x.PropertyType.IsEnum)))
                            dt.Columns.Add(cpp.Name, typeof(string));

                    foreach (var parent in parentColl)
                    {
                        if (parent == null) continue;
                        var pname = GetName(parent);
                        if (cp.GetValue(parent) is not IEnumerable childColl) continue;
                        if (IsScalar(childItemType))
                        {
                            foreach (var s in childColl)
                                dt.Rows.Add(pname, s?.ToString() ?? "");
                        }
                        else
                        {
                            var cprops = childItemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .Where(x => x.CanRead && (IsScalar(x.PropertyType) || x.PropertyType.IsEnum)).ToArray();
                            foreach (var child in childColl)
                            {
                                if (child == null) continue;
                                var row = dt.NewRow();
                                row["父项名称"] = pname;
                                foreach (var cpp in cprops)
                                    row[cpp.Name] = cpp.GetValue(child)?.ToString() ?? "";
                                dt.Rows.Add(row);
                            }
                        }
                    }
                    if (!result.ContainsKey(childSheet)) result[childSheet] = dt;
                }
            }
            return result;
        }

        private static void RestoreNestedCollections(object root, IDictionary<string, DataTable> tables)
        {
            foreach (var p in root.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!p.CanRead || !p.CanWrite || ExcludedProperties.Contains(p.Name) || MergedIoProperties.Contains(p.Name)) continue;
                if (!IsCollection(p.PropertyType)) continue;
                var itemType = CollectionItemType(p.PropertyType);
                if (itemType == null || IsScalar(itemType)) continue;

                var parentColl = p.GetValue(root) as IEnumerable;
                if (parentColl == null) continue;
                var parentSheet = SheetKey(p.Name);

                // 父项按名称分组（同名多个也可正确还原）
                var parentsByName = new Dictionary<string, List<object>>();
                foreach (var parent in parentColl)
                {
                    if (parent == null) continue;
                    var pn = GetName(parent);
                    if (!parentsByName.TryGetValue(pn, out var list))
                        parentsByName[pn] = list = new List<object>();
                    list.Add(parent);
                }

                foreach (var cp in itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!cp.CanRead || !cp.CanWrite || !IsCollection(cp.PropertyType)) continue;
                    var childItemType = CollectionItemType(cp.PropertyType);
                    if (childItemType == null) continue;
                    var childSheet = parentSheet + "." + ChildSheetKey(cp.Name);
                    if (!tables.TryGetValue(childSheet, out var dt)) continue;

                    var groups = dt.Rows.Cast<DataRow>()
                        .GroupBy(r => r["父项名称"]?.ToString() ?? "");

                    foreach (var g in groups)
                    {
                        if (!parentsByName.TryGetValue(g.Key, out var parents)) continue;
                        foreach (var parent in parents)
                        {
                            var childColl = cp.GetValue(parent);
                            var clear = cp.PropertyType.GetMethod("Clear");
                            var add = cp.PropertyType.GetMethod("Add");
                            if (childColl == null || add == null) continue;
                            if (clear != null) clear.Invoke(childColl, null);

                            if (IsScalar(childItemType))
                            {
                                var col = dt.Columns.Cast<DataColumn>().Last().ColumnName;
                                foreach (var row in g)
                                    add.Invoke(childColl, new object[] { ConvertTo(childItemType, row[col]?.ToString())! });
                            }
                            else
                            {
                                var cprops = childItemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                    .Where(x => x.CanRead && x.CanWrite && (IsScalar(x.PropertyType) || x.PropertyType.IsEnum)).ToArray();
                                foreach (var row in g)
                                {
                                    var item = Activator.CreateInstance(childItemType)!;
                                    foreach (var cpp in cprops)
                                    {
                                        if (!dt.Columns.Contains(cpp.Name)) continue;
                                        try { cpp.SetValue(item, ConvertTo(cpp.PropertyType, row[cpp.Name]?.ToString())); }
                                        catch { }
                                    }
                                    add.Invoke(childColl, new object[] { item });
                                }
                            }
                        }
                    }
                }
            }
        }

        // ===================== IO 合并 =====================

        private static DataTable BuildIoSheet(object root)
        {
            var dt = new DataTable("IO");
            dt.Columns.Add("类型", typeof(string));

            var itemType = CollectionItemTypeFor(root, "Inputs") ?? CollectionItemTypeFor(root, "Outputs");
            if (itemType == null) return dt;

            foreach (var cp in itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(x => x.CanRead && (IsScalar(x.PropertyType) || x.PropertyType.IsEnum)))
                dt.Columns.Add(cp.Name, typeof(string));

            AppendIoRows(root, "Inputs", "输入", dt, itemType);
            AppendIoRows(root, "Outputs", "输出", dt, itemType);
            return dt;
        }

        private static void AppendIoRows(object root, string propName, string tag, DataTable dt, Type itemType)
        {
            var p = root.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (p == null || p.GetValue(root) is not IEnumerable en) return;
            var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanRead && (IsScalar(x.PropertyType) || x.PropertyType.IsEnum)).ToArray();
            foreach (var item in en)
            {
                if (item == null) continue;
                var row = dt.NewRow();
                row["类型"] = tag;
                foreach (var cp in props)
                    row[cp.Name] = cp.GetValue(item)?.ToString() ?? "";
                dt.Rows.Add(row);
            }
        }

        private static void SplitIoToRoot(object root, DataTable dt)
        {
            var inP = root.GetType().GetProperty("Inputs", BindingFlags.Public | BindingFlags.Instance);
            var outP = root.GetType().GetProperty("Outputs", BindingFlags.Public | BindingFlags.Instance);
            if (inP == null || outP == null) return;

            var inColl = inP.GetValue(root);
            var outColl = outP.GetValue(root);
            var inClear = inP.PropertyType.GetMethod("Clear");
            var outClear = outP.PropertyType.GetMethod("Clear");
            var inAdd = inP.PropertyType.GetMethod("Add");
            var outAdd = outP.PropertyType.GetMethod("Add");
            if (inClear != null) inClear.Invoke(inColl, null);
            if (outClear != null) outClear.Invoke(outColl, null);
            if (inAdd == null || outAdd == null) return;

            var itemType = CollectionItemType(inP.PropertyType) ?? CollectionItemType(outP.PropertyType);
            if (itemType == null) return;
            var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanRead && x.CanWrite && (IsScalar(x.PropertyType) || x.PropertyType.IsEnum)).ToArray();

            foreach (DataRow dr in dt.Rows)
            {
                var tag = dr["类型"]?.ToString();
                var item = Activator.CreateInstance(itemType)!;
                foreach (var cp in props)
                {
                    if (!dt.Columns.Contains(cp.Name)) continue;
                    try { cp.SetValue(item, ConvertTo(cp.PropertyType, dr[cp.Name]?.ToString())); }
                    catch { }
                }
                if (tag == "输入") inAdd.Invoke(inColl, new object[] { item });
                else outAdd.Invoke(outColl, new object[] { item });
            }
        }

        // ===================== 内部工具 =====================

        private static Type? CollectionItemTypeFor(object root, string propName)
        {
            var p = root.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            return p == null ? null : CollectionItemType(p.PropertyType);
        }

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

        private static Type? CollectionItemType(Type t)
        {
            if (t.IsArray) return t.GetElementType();
            if (t.IsGenericType) return t.GetGenericArguments().FirstOrDefault();
            return null;
        }

        private static DataTable CollectionToTable(object? collection, Type itemType)
        {
            var dt = new DataTable();
            var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                               .Where(p => p.CanRead && (IsScalar(p.PropertyType) || p.PropertyType.IsEnum))
                               .ToArray();
            foreach (var p in props) dt.Columns.Add(p.Name, typeof(string));
            if (collection is IEnumerable en)
                foreach (var item in en)
                {
                    if (item == null) continue;
                    var row = dt.NewRow();
                    foreach (var p in props)
                        row[p.Name] = p.GetValue(item)?.ToString() ?? "";
                    dt.Rows.Add(row);
                }
            return dt;
        }

        private static object? ConvertTo(Type t, string? s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var u = Nullable.GetUnderlyingType(t);
            if (u != null) t = u;
            if (t == typeof(string)) return s;
            if (t.IsEnum) return Enum.TryParse(t, s, true, out var ev) ? ev : null;
            if (t == typeof(DateTime) && DateTime.TryParse(s, out var dt)) return dt;
            if (t == typeof(bool)) return s == "True" || s == "1" || s == "是" || s == "true";
            try { return Convert.ChangeType(s, t); }
            catch { return null; }
        }

        private static string GetName(object obj)
        {
            var p = obj.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
            return p?.GetValue(obj)?.ToString() ?? "";
        }

        /// <summary>属性名 -> sheet 名（应用 SheetNameOverrides 覆盖表）。</summary>
        private static string SheetKey(string propertyName)
            => SheetNameOverrides.TryGetValue(propertyName, out var v) ? v : propertyName;

        /// <summary>嵌套子属性名 -> 子表后缀（应用 ChildSheetNameOverrides 覆盖表）。</summary>
        private static string ChildSheetKey(string propertyName)
            => ChildSheetNameOverrides.TryGetValue(propertyName, out var v) ? v : propertyName;

        private static string SafeSheetName(string name)
        {
            char[] invalid = { '\\', '/', '?', '*', '[', ']', ':' };
            var s = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            if (s.Length > 31) s = s.Substring(0, 31);
            return string.IsNullOrWhiteSpace(s) ? "Sheet" : s;
        }
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
