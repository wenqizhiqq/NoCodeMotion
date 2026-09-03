// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using ClosedXML.Excel;
using NoCodeMotion.Models;

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
        /// 「项目管理」页末尾另有一段说明（属性=主题 / 值=说明），供人和 AI 直接阅读/修改 xlsx，不参与数据回填。
        /// </summary>
    public static class XlsxProjectStore
    {
        /// <summary>
        /// 固定工程目录：程序输出 bin\projects\（从 exe 所在目录上溯到名为 bin 的文件夹，再拼接 projects）。
        /// 工程全部以 xlsx 单文件存储，不使用 JSON。
        /// 可通过 <see cref="ConfigureRoot"/> 覆盖（ProjectManager/ProjectStore 会指向自己的 RootDir）。
        /// </summary>
        public static string ProjectsRoot => _overrideRoot ?? ResolveProjectsRoot();
        private static string? _overrideRoot;

        /// <summary>由调用方覆盖工程目录（典型用法：ProjectManager.RootDir）。</summary>
        public static void ConfigureRoot(string rootDir)
        {
            if (string.IsNullOrWhiteSpace(rootDir)) { _overrideRoot = null; return; }
            _overrideRoot = rootDir;
        }

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
            ["Cameras"] = "相机",
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

        /// <summary>导出/导入时跳过的属性：Io 为 Inputs+Outputs 的计算合并属性，避免重复存储；Points/PointAxes 是旧版单工位字段，EnsurePointTables 后已清空，不再落盘。</summary>
        private static readonly HashSet<string> ExcludedProperties = new() { "Io", "Points", "PointAxes" };

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
            // 按主界面顶部菜单顺序排页：父页排在 MenuSheetNames 对应位置，嵌套子页(父.子)紧随父页之后，未知页置末尾。
            var orderedKeys = sheets.Keys
                .OrderBy(k =>
                {
                    var dot = k.IndexOf('.');
                    var parent = dot >= 0 ? k.Substring(0, dot) : k;
                    int rank = Array.IndexOf(MenuSheetNames, parent);
                    if (rank < 0) rank = MenuSheetNames.Length; // 不在菜单里的页置末尾
                    int childFlag = dot >= 0 ? 1 : 0;           // 子页紧随父页之后
                    return (rank, childFlag, k);
                })
                .ToArray();
            foreach (var key in orderedKeys)
            {
                var dt = sheets[key];
                var ws = wb.Worksheets.Add(SafeSheetName(key));
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

        /// <summary>
        /// 只读取「项目管理」信息表中的元数据（CreatedAt/UpdatedAt/Remark），不全量反序列化。
        /// 给 ListProjectEntries 用：列工程清单时不需要加载整个 ProjectData。
        /// 文件不存在或读不到则三项都返回 null/空。
        /// </summary>
        public static (DateTime? CreatedAt, DateTime? UpdatedAt, string? Remark) LoadMeta(string projectName)
        {
            var path = FilePathFor(projectName);
            if (!File.Exists(path)) return (null, null, null);
            try
            {
                using var wb = new XLWorkbook(path);
                var ws = wb.Worksheets.FirstOrDefault(s => s.Name == "项目管理");
                if (ws == null) return (null, null, null);
                DateTime? created = null, updated = null;
                string? remark = null;
                foreach (var row in ws.RowsUsed())
                {
                    var name = row.Cell(1).GetString();
                    var val = row.Cell(2).GetString();
                    if (string.IsNullOrEmpty(name)) continue;
                    if (name == "CreatedAt") { if (DateTime.TryParse(val, out var d)) created = d; }
                    else if (name == "UpdatedAt") { if (DateTime.TryParse(val, out var d)) updated = d; }
                    else if (name == "Remark") { remark = val; }
                }
                return (created, updated, remark);
            }
            catch { return (null, null, null); }
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

            // 合并的嵌套子表（点位表/料盘/流程 → 各自一页，含父行 + 子行 + 轴名行 + 空行分隔）
            AddMergedBlockSheets(root, dict);

            // IO 合并表
            dict["IO"] = BuildIoSheet(root);

            // 文档说明：合并进「项目管理」页末尾（属性=主题 / 值=说明），供人和 AI 阅读/修改；不参与数据回填
            AppendDocRows(meta);

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

            // 3) 合并嵌套块还原（点位表/料盘/流程 → 按 类型 列拆回父/子集合）
            RestoreMergedBlockSheets(root, tables);

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

                            // 数组类型（如 PointTable.AxisNames = string[4]）没有 Clear/Add，
                            // 走专门路径：按行数重新分配数组并按位填充。
                            if (cp.PropertyType.IsArray)
                            {
                                if (!IsScalar(childItemType!)) continue;
                                var arr = Array.CreateInstance(childItemType!, g.Count());
                                var col = dt.Columns.Cast<DataColumn>().Last().ColumnName;
                                int idx = 0;
                                foreach (var row in g)
                                {
                                    arr.SetValue(ConvertTo(childItemType!, row[col]?.ToString()), idx++);
                                }
                                cp.SetValue(parent, arr);
                                continue;
                            }

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

        // ===================== 合并嵌套块（一个父表 = 一页 sheet） =====================

        /// <summary>把点位表/料盘/流程三类有子集合的父表，写成"一页含父行 + 子行 + 轴名行 + 空行"的合并格式（替换旧的 父.子 分页）。</summary>
        private static void AddMergedBlockSheets(object root, IDictionary<string, DataTable> dict)
        {
            var pt = GetCollection(root, "PointTables");
            if (pt != null) dict["点位表"] = BuildPointTableSheet(pt);

            var tr = GetCollection(root, "Trays");
            if (tr != null) dict["料盘"] = BuildTraySheet(tr);

            var fl = GetCollection(root, "Flows");
            if (fl != null) dict["流程"] = BuildFlowSheet(fl);
        }

        /// <summary>合并 sheet 的回填入口：根据 sheet 名调用对应的分块解析器。</summary>
        private static void RestoreMergedBlockSheets(object root, IDictionary<string, DataTable> tables)
        {
            if (tables.TryGetValue("点位表", out var ptDt)) RestorePointTableSheet(root, ptDt);
            if (tables.TryGetValue("料盘", out var trDt)) RestoreTraySheet(root, trDt);
            if (tables.TryGetValue("流程", out var flDt)) RestoreFlowSheet(root, flDt);
        }

        /// <summary>
        /// 把 xlsx 结构 / 编辑说明追加到「项目管理」信息表末尾（用其已有列：属性=主题、值=说明）。
        /// 这些说明行不参与数据回填：
        ///   - ImportFromDataTables 对「项目管理」只回填 属性 与根对象标量属性同名的行，说明行的中文标题不会命中任何属性名，故被安全忽略；
        ///   - LoadMeta 仅读取 CreatedAt/UpdatedAt/Remark 三行，同样忽略说明行。
        /// 重新保存工程时 ExportToDataTables 会重建 meta 并重新追加，故手动删除说明内容也无妨（会被重建）。
        /// </summary>
        private static void AppendDocRows(DataTable meta)
        {
            void Add(string topic, string desc)
            {
                var r = meta.NewRow();
                r["属性"] = topic;
                r["值"] = desc;
                meta.Rows.Add(r);
            }

            meta.Rows.Add(meta.NewRow()); // 空行分隔：上方是标量元数据，下方是说明
            Add("===== 说明（仅供人和 AI 阅读，不参与数据回填） =====", "");
            Add("== 文件结构总览 ==", "本 xlsx 是工程的唯一存储（不再使用 JSON）。每个菜单页写入一个 worksheet；本说明已合并在「项目管理」页末尾。");
            Add("sheet 顺序", "与顶部菜单一致：项目管理→控制器→轴→IO→气缸→点位表→通讯→料盘→相机→变量→流程（工程师/操作员无数据则不生成 sheet）。");
            Add("合并页", "点位表 / 料盘 / 流程 为有子集合的父表，已合并为「一页含父行+子行+空行分隔」格式（不再分页）。");
            Add("说明位置", "本段说明位于「项目管理」页（属性=主题 / 值=说明），导入时自动忽略：只回填与根对象属性同名的行，说明行的中文标题不会命中，故被跳过；请勿担心误改数据，保存工程会自动重建。");

            Add("== 行类型约定（类型 列） ==", "合并页用「类型」列区分父行/子行，用「父项名称」列把子行挂回父行。");
            Add("点位表.类型", "工位 = 点位表父行（名称=工位名）；点位 = 子行（父项名称 指向工位）；轴名 = 该工位的轴槽名称行（父项名称 指向工位）。");
            Add("料盘.类型", "料盘 = 父行；格子 = 子行（父项名称 指向料盘）。");
            Add("流程.类型", "流程 = 父行；步骤 = 普通步骤子行；视步 = 视觉步骤子行（父项名称 均指向流程名）。");
            Add("IO.类型", "输入 / 输出 —— 由该列区分，输入来自 Inputs 集合，输出来自 Outputs 集合。");
            Add("项目管理", "两列：属性 / 值，上方存标量元数据（工程名称、CreatedAt、UpdatedAt、Remark、RequirementsText 等），下方为本说明段。");

            Add("== 编辑提示 ==", "改完 cell 用软件「保存工程」重新生成即可；直接改 xlsx 后由软件打开也会被读取回填（按列名匹配）。");
            Add("勿改定位列", "不要改动「类型」「父项名称」的取值，否则导入会错位或丢失父子关联。");
            Add("保留空行", "父块之间用全空行分隔，保留即可（导入会跳过全空行）。");
            Add("布尔写法", "已占用 / 使能 / 上次成功 等布尔字段写「是/否」或 True/False 均可识别。");
            Add("列名即字段", "列名即字段显示名（中文），新增/删除行请保持列对齐，不要增删列。");

            Add("== 枚举取值参考 ==", "写入枚举列时，以下 ASCII 值或中文均可被识别。");
            Add("流程.类型标记 (Kind)", "Table=表格 / Lua=脚本 / Vision=视觉。");
            Add("流程.角色 (Role)", "Main=主流程 / Reset=复位流程。");
            Add("流程.状态 (Status)", "Idle=空闲 / Running=运行中 / Done=完成 / Error=错误。");
            Add("IO.类型", "输入 / 输出（仅展示用，不参与枚举解析）。");

            Add("== 合并页列说明 ==", "列数固定，勿增删。");
            Add("点位表（17列）", "类型/名称/父项名称/时序/同步组/轴1位置/轴1速度/轴2位置/轴2速度/轴3位置/轴3速度/轴4位置/轴4速度/轴1名/轴2名/轴3名/轴4名。");
            Add("料盘（12列）", "类型/名称/父项名称/行数/列数/起点X/起点Y/间距X/间距Y/行号/列号/已占用。");
            Add("流程（51列）", "类型/名称/父项名称/类型标记/角色/Lua源码/状态 + 步骤列(逻辑/功能/属性/操作/设值/超时/时长ms/实际值) + 视步列(视步类型/使能/相机ID/保存路径/曝光ms/宽度/高度/源类型/文件夹路径/模板路径/分数阈值/角度范围/匹配模式/模板框X/模板框Y/模板框W/模板框H/算法/最小面积/最大面积/阈值/检测模式/测量模式/标定/单位/协议/目标/内容/预处理操作/预处理参数1/预处理参数2/预处理ROI/第二图路径/运行时长ms/上次成功/上次结果)。");
        }

        // ----- 点位表 -----

        private static DataTable BuildPointTableSheet(IEnumerable parentColl)
        {
            var dt = new DataTable("点位表");
            // 列结构：类型 | 名称 | 父项名称 | 时序 | 同步组 | 轴1-4 位置/速度 | 轴1-4 名
            dt.Columns.Add("类型", typeof(string));
            dt.Columns.Add("名称", typeof(string));
            dt.Columns.Add("父项名称", typeof(string));
            dt.Columns.Add("时序", typeof(string));
            dt.Columns.Add("同步组", typeof(string));
            for (int i = 1; i <= 4; i++) { dt.Columns.Add($"轴{i}位置", typeof(string)); dt.Columns.Add($"轴{i}速度", typeof(string)); }
            for (int i = 1; i <= 4; i++) dt.Columns.Add($"轴{i}名", typeof(string));

            foreach (var ptObj in parentColl)
            {
                if (ptObj == null) continue;
                var ptName = GetName(ptObj);
                // 父行
                var parentRow = dt.NewRow();
                parentRow["类型"] = "工位";
                parentRow["名称"] = ptName;
                dt.Rows.Add(parentRow);
                // 点位行（按 PointTable.Points）
                var pointsProp = ptObj.GetType().GetProperty("Points");
                if (pointsProp?.GetValue(ptObj) is IEnumerable points)
                    foreach (var pObj in points)
                    {
                        if (pObj == null) continue;
                        var r = dt.NewRow();
                        r["类型"] = "点位";
                        r["名称"] = GetName(pObj);
                        r["父项名称"] = ptName;
                        // 时序/同步组
                        SetStr(r, "时序", pObj, "TimingMark");
                        SetStr(r, "同步组", pObj, "SyncGroup");
                        // 4 轴位置/速度
                        var posProp = pObj.GetType().GetProperty("Positions");
                        if (posProp?.GetValue(pObj) is IEnumerable positions)
                        {
                            int idx = 0;
                            foreach (var aObj in positions)
                            {
                                if (idx >= 4) break;
                                if (aObj == null) { idx++; continue; }
                                var pVal = aObj.GetType().GetProperty("Position")?.GetValue(aObj);
                                var sVal = aObj.GetType().GetProperty("Speed")?.GetValue(aObj);
                                r[$"轴{idx + 1}位置"] = pVal?.ToString() ?? "";
                                r[$"轴{idx + 1}速度"] = sVal?.ToString() ?? "";
                                idx++;
                            }
                        }
                        dt.Rows.Add(r);
                    }
                // 轴名行
                var axesRow = dt.NewRow();
                axesRow["类型"] = "轴名";
                axesRow["名称"] = "轴槽";
                axesRow["父项名称"] = ptName;
                var axisProp = ptObj.GetType().GetProperty("AxisNames");
                if (axisProp?.GetValue(ptObj) is IEnumerable axes)
                {
                    int idx = 0;
                    foreach (var s in axes)
                    {
                        if (idx >= 4) break;
                        axesRow[$"轴{idx + 1}名"] = s?.ToString() ?? "";
                        idx++;
                    }
                }
                dt.Rows.Add(axesRow);
                // 空行分隔
                dt.Rows.Add(dt.NewRow());
            }
            return dt;
        }

        private static void RestorePointTableSheet(object root, DataTable dt)
        {
            var pointTablesProp = root.GetType().GetProperty("PointTables");
            if (pointTablesProp == null) return;
            if (pointTablesProp.GetValue(root) is not IEnumerable ptColl) return;
            var ptClear = pointTablesProp.PropertyType.GetMethod("Clear");
            var ptAdd = pointTablesProp.PropertyType.GetMethod("Add");
            if (ptClear != null) ptClear.Invoke(ptColl, null);
            if (ptAdd == null) return;

            // 把现有 PointTable 按名称分组（与导出顺序对齐）
            var byName = new Dictionary<string, object>();
            foreach (var p in dt.Rows.Cast<DataRow>().Where(r => r["类型"]?.ToString() == "工位"))
            {
                var n = p["名称"]?.ToString() ?? "";
                if (string.IsNullOrEmpty(n) || byName.ContainsKey(n)) continue;
                var newPt = new PointTable { Name = n };
                ptAdd.Invoke(ptColl, new object[] { newPt });
                byName[n] = newPt;
            }

            // 把点位和轴名按"父项名称"挂到对应 PointTable
            foreach (var row in dt.Rows.Cast<DataRow>())
            {
                var t = row["类型"]?.ToString();
                var pname = row["父项名称"]?.ToString() ?? "";
                if (!byName.TryGetValue(pname, out var ptObj)) continue;
                if (t == "点位")
                {
                    var ptType = ptObj.GetType();
                    var pointsProp = ptType.GetProperty("Points");
                    if (pointsProp?.GetValue(ptObj) is IEnumerable ptPoints && pointsProp.PropertyType.GetMethod("Add") is var add && add != null)
                    {
                        var newP = new PointItem
                        {
                            Name = row["名称"]?.ToString() ?? "",
                            TimingMark = row["时序"]?.ToString() ?? "",
                            SyncGroup = row["同步组"]?.ToString() ?? "",
                        };
                        // 4 轴位置/速度
                        var posProp = newP.GetType().GetProperty("Positions");
                        if (posProp?.GetValue(newP) is IEnumerable positions)
                        {
                            var posClear = posProp.PropertyType.GetMethod("Clear");
                            var posAdd = posProp.PropertyType.GetMethod("Add");
                            posClear?.Invoke(positions, null);
                            for (int i = 1; i <= 4; i++)
                            {
                                var pVal = row[$"轴{i}位置"]?.ToString() ?? "";
                                var sVal = row[$"轴{i}速度"]?.ToString() ?? "";
                                var pa = new PointAxis
                                {
                                    Position = double.TryParse(pVal, out var dp) ? dp : 0,
                                    Speed = double.TryParse(sVal, out var ds) ? ds : 0,
                                };
                                posAdd?.Invoke(positions, new object[] { pa });
                            }
                        }
                        add.Invoke(ptPoints, new object[] { newP });
                    }
                }
                else if (t == "轴名")
                {
                    var ptType = ptObj.GetType();
                    var axesProp = ptType.GetProperty("AxisNames");
                    if (axesProp?.GetValue(ptObj) is IEnumerable axes)
                    {
                        var axClear = axesProp.PropertyType.GetMethod("Clear");
                        var axAdd = axesProp.PropertyType.GetMethod("Add");
                        axClear?.Invoke(axes, null);
                        for (int i = 1; i <= 4; i++)
                        {
                            var n = row[$"轴{i}名"]?.ToString() ?? "";
                            axAdd?.Invoke(axes, new object[] { n });
                        }
                        // 补齐 4 槽
                        ptType.GetMethod("EnsureAxisSlots")?.Invoke(ptObj, null);
                    }
                }
            }
        }

        // ----- 料盘 -----

        private static DataTable BuildTraySheet(IEnumerable parentColl)
        {
            var dt = new DataTable("料盘");
            dt.Columns.Add("类型", typeof(string));
            dt.Columns.Add("名称", typeof(string));
            dt.Columns.Add("父项名称", typeof(string));
            dt.Columns.Add("行数", typeof(string));
            dt.Columns.Add("列数", typeof(string));
            dt.Columns.Add("起点X", typeof(string));
            dt.Columns.Add("起点Y", typeof(string));
            dt.Columns.Add("间距X", typeof(string));
            dt.Columns.Add("间距Y", typeof(string));
            dt.Columns.Add("行号", typeof(string));
            dt.Columns.Add("列号", typeof(string));
            dt.Columns.Add("已占用", typeof(string));

            foreach (var trObj in parentColl)
            {
                if (trObj == null) continue;
                var trName = GetName(trObj);
                var r1 = dt.NewRow();
                r1["类型"] = "料盘";
                r1["名称"] = trName;
                SetStr(r1, "行数", trObj, "Rows");
                SetStr(r1, "列数", trObj, "Cols");
                SetStr(r1, "起点X", trObj, "StartX");
                SetStr(r1, "起点Y", trObj, "StartY");
                SetStr(r1, "间距X", trObj, "PitchX");
                SetStr(r1, "间距Y", trObj, "PitchY");
                dt.Rows.Add(r1);
                var cellsProp = trObj.GetType().GetProperty("Cells");
                if (cellsProp?.GetValue(trObj) is IEnumerable cells)
                    foreach (var cObj in cells)
                    {
                        if (cObj == null) continue;
                        var r = dt.NewRow();
                        r["类型"] = "格子";
                        var row = GetInt(cObj, "Row");
                        var col = GetInt(cObj, "Col");
                        r["名称"] = $"{row},{col}";
                        r["父项名称"] = trName;
                        SetStr(r, "行号", cObj, "Row");
                        SetStr(r, "列号", cObj, "Col");
                        r["已占用"] = GetBool(cObj, "Occupied") ? "是" : "否";
                        dt.Rows.Add(r);
                    }
                dt.Rows.Add(dt.NewRow());
            }
            return dt;
        }

        private static void RestoreTraySheet(object root, DataTable dt)
        {
            var traysProp = root.GetType().GetProperty("Trays");
            if (traysProp == null) return;
            if (traysProp.GetValue(root) is not IEnumerable trColl) return;
            var clear = traysProp.PropertyType.GetMethod("Clear");
            var add = traysProp.PropertyType.GetMethod("Add");
            if (clear != null) clear.Invoke(trColl, null);
            if (add == null) return;

            var byName = new Dictionary<string, object>();
            foreach (var r in dt.Rows.Cast<DataRow>().Where(r => r["类型"]?.ToString() == "料盘"))
            {
                var n = r["名称"]?.ToString() ?? "";
                if (string.IsNullOrEmpty(n) || byName.ContainsKey(n)) continue;
                var rows = GetInt(r["行数"]?.ToString() ?? "0");
                var cols = GetInt(r["列数"]?.ToString() ?? "0");
                var tr = new TrayItem
                {
                    Name = n,
                    Rows = rows > 0 ? rows : 1,
                    Cols = cols > 0 ? cols : 1,
                    StartX = GetDouble(r["起点X"]?.ToString()),
                    StartY = GetDouble(r["起点Y"]?.ToString()),
                    PitchX = GetDouble(r["间距X"]?.ToString()),
                    PitchY = GetDouble(r["间距Y"]?.ToString()),
                };
                add.Invoke(trColl, new object[] { tr });
                byName[n] = tr;
            }
            // 格子按父项挂回
            foreach (var r in dt.Rows.Cast<DataRow>().Where(r => r["类型"]?.ToString() == "格子"))
            {
                var pname = r["父项名称"]?.ToString() ?? "";
                if (!byName.TryGetValue(pname, out var trObj)) continue;
                var trType = trObj.GetType();
                var cellsProp = trType.GetProperty("Cells");
                if (cellsProp?.GetValue(trObj) is not IEnumerable cells) continue;
                var addCell = cellsProp.PropertyType.GetMethod("Add");
                if (addCell == null) continue;
                var rr = GetInt(r["行号"]?.ToString());
                var cc = GetInt(r["列号"]?.ToString());
                var occ = r["已占用"]?.ToString() == "是" || r["已占用"]?.ToString() == "True";
                addCell.Invoke(cells, new object[] { new TrayCell { Row = rr, Col = cc, Occupied = occ } });
            }
        }

        // ----- 流程 -----

        private static DataTable BuildFlowSheet(IEnumerable parentColl)
        {
            // 列结构 = FlowItem 标量 + FlowStep 标量 + VisualFlowStep 标量
            var dt = new DataTable("流程");
            dt.Columns.Add("类型", typeof(string));
            dt.Columns.Add("名称", typeof(string));
            dt.Columns.Add("父项名称", typeof(string));
            // FlowItem 标量
            dt.Columns.Add("类型标记", typeof(string));
            dt.Columns.Add("角色", typeof(string));
            dt.Columns.Add("Lua源码", typeof(string));
            dt.Columns.Add("节点图JSON", typeof(string));
            dt.Columns.Add("状态", typeof(string));
            // FlowStep 标量
            dt.Columns.Add("逻辑", typeof(string));
            dt.Columns.Add("功能", typeof(string));
            dt.Columns.Add("属性", typeof(string));
            dt.Columns.Add("操作", typeof(string));
            dt.Columns.Add("设值", typeof(string));
            dt.Columns.Add("超时", typeof(string));
            dt.Columns.Add("时长ms", typeof(string));
            dt.Columns.Add("实际值", typeof(string));
            // VisualFlowStep 标量
            dt.Columns.Add("视步类型", typeof(string));
            dt.Columns.Add("使能", typeof(string));
            dt.Columns.Add("相机ID", typeof(string));
            dt.Columns.Add("保存路径", typeof(string));
            dt.Columns.Add("曝光ms", typeof(string));
            dt.Columns.Add("宽度", typeof(string));
            dt.Columns.Add("高度", typeof(string));
            dt.Columns.Add("源类型", typeof(string));
            dt.Columns.Add("文件夹路径", typeof(string));
            dt.Columns.Add("模板路径", typeof(string));
            dt.Columns.Add("分数阈值", typeof(string));
            dt.Columns.Add("角度范围", typeof(string));
            dt.Columns.Add("匹配模式", typeof(string));
            dt.Columns.Add("模板框X", typeof(string));
            dt.Columns.Add("模板框Y", typeof(string));
            dt.Columns.Add("模板框W", typeof(string));
            dt.Columns.Add("模板框H", typeof(string));
            dt.Columns.Add("算法", typeof(string));
            dt.Columns.Add("最小面积", typeof(string));
            dt.Columns.Add("最大面积", typeof(string));
            dt.Columns.Add("阈值", typeof(string));
            dt.Columns.Add("检测模式", typeof(string));
            dt.Columns.Add("测量模式", typeof(string));
            dt.Columns.Add("标定", typeof(string));
            dt.Columns.Add("单位", typeof(string));
            dt.Columns.Add("协议", typeof(string));
            dt.Columns.Add("目标", typeof(string));
            dt.Columns.Add("内容", typeof(string));
            dt.Columns.Add("预处理操作", typeof(string));
            dt.Columns.Add("预处理参数1", typeof(string));
            dt.Columns.Add("预处理参数2", typeof(string));
            dt.Columns.Add("预处理ROI", typeof(string));
            dt.Columns.Add("第二图路径", typeof(string));
            dt.Columns.Add("运行时长ms", typeof(string));
            dt.Columns.Add("上次成功", typeof(string));
            dt.Columns.Add("上次结果", typeof(string));

            foreach (var flObj in parentColl)
            {
                if (flObj == null) continue;
                var flName = GetName(flObj);
                // 父行
                var r1 = dt.NewRow();
                r1["类型"] = "流程";
                r1["名称"] = flName;
                SetStr(r1, "类型标记", flObj, "Kind");
                SetStr(r1, "角色", flObj, "Role");
                SetStr(r1, "Lua源码", flObj, "LuaSource");
                SetStr(r1, "节点图JSON", flObj, "GraphJson");
                SetStr(r1, "状态", flObj, "Status");
                dt.Rows.Add(r1);

                // 步骤
                var stepsProp = flObj.GetType().GetProperty("Steps");
                if (stepsProp?.GetValue(flObj) is IEnumerable steps)
                    foreach (var sObj in steps)
                    {
                        if (sObj == null) continue;
                        var r = dt.NewRow();
                        r["类型"] = "步骤";
                        r["名称"] = GetName(sObj);
                        r["父项名称"] = flName;
                        SetStr(r, "逻辑", sObj, "Logic");
                        SetStr(r, "功能", sObj, "Function");
                        SetStr(r, "属性", sObj, "Property");
                        SetStr(r, "操作", sObj, "Operation");
                        SetStr(r, "设值", sObj, "SetValue");
                        SetStr(r, "超时", sObj, "Timeout");
                        SetStr(r, "时长ms", sObj, "DurationMs");
                        SetStr(r, "实际值", sObj, "ActualValue");
                        dt.Rows.Add(r);
                    }
                // 视步
                var vstepsProp = flObj.GetType().GetProperty("VisualSteps");
                if (vstepsProp?.GetValue(flObj) is IEnumerable vsteps)
                    foreach (var vObj in vsteps)
                    {
                        if (vObj == null) continue;
                        var r = dt.NewRow();
                        r["类型"] = "视步";
                        r["名称"] = GetName(vObj);
                        r["父项名称"] = flName;
                        SetStr(r, "视步类型", vObj, "StepType");
                        SetStr(r, "使能", vObj, "Enabled");
                        SetStr(r, "相机ID", vObj, "CameraId");
                        SetStr(r, "保存路径", vObj, "SavePath");
                        SetStr(r, "曝光ms", vObj, "ExposureMs");
                        SetStr(r, "宽度", vObj, "Width");
                        SetStr(r, "高度", vObj, "Height");
                        SetStr(r, "源类型", vObj, "SourceType");
                        SetStr(r, "文件夹路径", vObj, "FolderPath");
                        SetStr(r, "模板路径", vObj, "TemplatePath");
                        SetStr(r, "分数阈值", vObj, "ScoreThreshold");
                        SetStr(r, "角度范围", vObj, "AngleRange");
                        SetStr(r, "匹配模式", vObj, "MatchMode");
                        SetStr(r, "模板框X", vObj, "TemplateRoiX");
                        SetStr(r, "模板框Y", vObj, "TemplateRoiY");
                        SetStr(r, "模板框W", vObj, "TemplateRoiW");
                        SetStr(r, "模板框H", vObj, "TemplateRoiH");
                        SetStr(r, "算法", vObj, "Algorithm");
                        SetStr(r, "最小面积", vObj, "MinArea");
                        SetStr(r, "最大面积", vObj, "MaxArea");
                        SetStr(r, "阈值", vObj, "Threshold");
                        SetStr(r, "检测模式", vObj, "DetectMode");
                        SetStr(r, "测量模式", vObj, "MeasureMode");
                        SetStr(r, "标定", vObj, "Calibration");
                        SetStr(r, "单位", vObj, "Unit");
                        SetStr(r, "协议", vObj, "Protocol");
                        SetStr(r, "目标", vObj, "Target");
                        SetStr(r, "内容", vObj, "Content");
                        SetStr(r, "预处理操作", vObj, "PreOp");
                        SetStr(r, "预处理参数1", vObj, "PreParam1");
                        SetStr(r, "预处理参数2", vObj, "PreParam2");
                        SetStr(r, "预处理ROI", vObj, "PreRoi");
                        SetStr(r, "第二图路径", vObj, "PreImage2Path");
                        SetStr(r, "运行时长ms", vObj, "DurationMs");
                        SetStr(r, "上次成功", vObj, "LastOk");
                        SetStr(r, "上次结果", vObj, "LastResult");
                        dt.Rows.Add(r);
                    }
                dt.Rows.Add(dt.NewRow());
            }
            return dt;
        }

        private static void RestoreFlowSheet(object root, DataTable dt)
        {
            var flowsProp = root.GetType().GetProperty("Flows");
            if (flowsProp == null) return;
            if (flowsProp.GetValue(root) is not IEnumerable flColl) return;
            var clear = flowsProp.PropertyType.GetMethod("Clear");
            var add = flowsProp.PropertyType.GetMethod("Add");
            if (clear != null) clear.Invoke(flColl, null);
            if (add == null) return;

            var byName = new Dictionary<string, object>();
            foreach (var r in dt.Rows.Cast<DataRow>().Where(r => r["类型"]?.ToString() == "流程"))
            {
                var n = r["名称"]?.ToString() ?? "";
                if (string.IsNullOrEmpty(n) || byName.ContainsKey(n)) continue;
                var fl = new FlowItem { Name = n };
                TrySet(fl, "Kind", r["类型标记"]?.ToString(), typeof(FlowKind));
                TrySet(fl, "Role", r["角色"]?.ToString(), typeof(FlowRole));
                TrySet(fl, "LuaSource", r["Lua源码"]?.ToString());
                TrySet(fl, "GraphJson", r["节点图JSON"]?.ToString());
                TrySet(fl, "Status", r["状态"]?.ToString(), typeof(FlowStatus));
                add.Invoke(flColl, new object[] { fl });
                byName[n] = fl;
            }

            foreach (var r in dt.Rows.Cast<DataRow>())
            {
                var t = r["类型"]?.ToString();
                var pname = r["父项名称"]?.ToString() ?? "";
                if (!byName.TryGetValue(pname, out var flObj)) continue;
                var flType = flObj.GetType();
                if (t == "步骤")
                {
                    var stepsProp = flType.GetProperty("Steps");
                    if (stepsProp?.GetValue(flObj) is IEnumerable steps && stepsProp.PropertyType.GetMethod("Add") is var a && a != null)
                    {
                        var st = new FlowStep
                        {
                            Name = r["名称"]?.ToString() ?? "",
                            Logic = r["逻辑"]?.ToString() ?? "",
                            Function = r["功能"]?.ToString() ?? "",
                            Property = r["属性"]?.ToString() ?? "",
                            Operation = r["操作"]?.ToString() ?? "",
                            SetValue = r["设值"]?.ToString() ?? "",
                            Timeout = r["超时"]?.ToString() ?? "",
                            DurationMs = GetInt(r["时长ms"]?.ToString()),
                            ActualValue = r["实际值"]?.ToString() ?? "",
                        };
                        a.Invoke(steps, new object[] { st });
                    }
                }
                else if (t == "视步")
                {
                    var vstepsProp = flType.GetProperty("VisualSteps");
                    if (vstepsProp?.GetValue(flObj) is IEnumerable vsteps && vstepsProp.PropertyType.GetMethod("Add") is var a && a != null)
                    {
                        var v = new VisualFlowStep
                        {
                            Name = r["名称"]?.ToString() ?? "",
                            StepType = r["视步类型"]?.ToString() ?? "",
                            Enabled = r["使能"]?.ToString() == "True" || r["使能"]?.ToString() == "是" || r["使能"]?.ToString() == "true",
                            CameraId = r["相机ID"]?.ToString() ?? "",
                            SavePath = r["保存路径"]?.ToString() ?? "",
                            ExposureMs = GetDouble(r["曝光ms"]?.ToString()),
                            Width = GetInt(r["宽度"]?.ToString()),
                            Height = GetInt(r["高度"]?.ToString()),
                            SourceType = r["源类型"]?.ToString() ?? "",
                            FolderPath = r["文件夹路径"]?.ToString() ?? "",
                            TemplatePath = r["模板路径"]?.ToString() ?? "",
                            ScoreThreshold = GetDouble(r["分数阈值"]?.ToString()),
                            AngleRange = GetDouble(r["角度范围"]?.ToString()),
                            MatchMode = r["匹配模式"]?.ToString() ?? "",
                            TemplateRoiX = GetInt(r["模板框X"]?.ToString()),
                            TemplateRoiY = GetInt(r["模板框Y"]?.ToString()),
                            TemplateRoiW = GetInt(r["模板框W"]?.ToString()),
                            TemplateRoiH = GetInt(r["模板框H"]?.ToString()),
                            Algorithm = r["算法"]?.ToString() ?? "",
                            MinArea = GetDouble(r["最小面积"]?.ToString()),
                            MaxArea = GetDouble(r["最大面积"]?.ToString()),
                            Threshold = GetDouble(r["阈值"]?.ToString()),
                            DetectMode = r["检测模式"]?.ToString() ?? "",
                            MeasureMode = r["测量模式"]?.ToString() ?? "",
                            Calibration = GetDouble(r["标定"]?.ToString()),
                            Unit = r["单位"]?.ToString() ?? "",
                            Protocol = r["协议"]?.ToString() ?? "",
                            Target = r["目标"]?.ToString() ?? "",
                            Content = r["内容"]?.ToString() ?? "",
                            PreOp = r["预处理操作"]?.ToString() ?? "",
                            PreParam1 = GetDouble(r["预处理参数1"]?.ToString()),
                            PreParam2 = GetDouble(r["预处理参数2"]?.ToString()),
                            PreRoi = r["预处理ROI"]?.ToString() ?? "",
                            PreImage2Path = r["第二图路径"]?.ToString() ?? "",
                            DurationMs = GetDouble(r["运行时长ms"]?.ToString()),
                            LastOk = r["上次成功"]?.ToString() == "True" || r["上次成功"]?.ToString() == "是" || r["上次成功"]?.ToString() == "true",
                            LastResult = r["上次结果"]?.ToString() ?? "",
                        };
                        a.Invoke(vsteps, new object[] { v });
                    }
                }
            }
        }

        // ----- 反射小工具 -----

        private static IEnumerable? GetCollection(object root, string propName)
        {
            var p = root.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            return p?.GetValue(root) as IEnumerable;
        }

        private static void SetStr(DataRow row, string col, object src, string propName)
        {
            var v = src.GetType().GetProperty(propName)?.GetValue(src);
            row[col] = v?.ToString() ?? "";
        }

        private static int GetInt(object? src, string propName)
        {
            var v = src?.GetType().GetProperty(propName)?.GetValue(src);
            return v switch { int i => i, long l => (int)l, double d => (int)d, string s => int.TryParse(s, out var n) ? n : 0, _ => 0 };
        }

        private static int GetInt(string? s) => int.TryParse(s, out var n) ? n : 0;

        private static double GetDouble(string? s) => double.TryParse(s, out var n) ? n : 0;

        private static bool GetBool(object src, string propName)
        {
            var v = src.GetType().GetProperty(propName)?.GetValue(src);
            return v is bool b && b;
        }

        private static void TrySet(object target, string propName, string? value, Type? enumType = null)
        {
            var p = target.GetType().GetProperty(propName);
            if (p == null || !p.CanWrite) return;
            try
            {
                if (enumType != null && p.PropertyType == enumType)
                {
                    if (Enum.TryParse(enumType, value ?? "", true, out var ev)) p.SetValue(target, ev);
                    return;
                }
                p.SetValue(target, ConvertTo(p.PropertyType, value));
            }
            catch { }
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
            // 字符串：空串保持空串（不要返回 null —— 否则 AxisNames 等集合里的空槽
            // 会变成 null，UI 绑定时会触发「值不能为 null」异常）
            if (t == typeof(string)) return s ?? "";
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
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
