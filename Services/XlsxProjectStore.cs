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
    ///       按菜单页分页，每个菜单页写入一个 worksheet（分页保存），xlsx 为唯一存储。
    ///
    /// 为规避「无法读取加密的 ProjectData 字段结构」这一限制，本类采用反射式通用读写：
    ///   - 导出：遍历根对象 public 属性，集合属性 -> 一张表（列=元素类型的标量属性），
    ///           标量/枚举属性 -> 汇聚到「项目管理」信息表；
    ///   - 导入：按表名(=属性名)反射清空并重建集合，按列名匹配属性名回填（含类型转换）。
    /// 调用方（ProjectStore）只需调用 <see cref="SaveProject"/> / <see cref="OpenProject"/> 即可，
    /// 无需关心具体字段。中文 sheet 名通过 <see cref="SheetNameOverrides"/> 对齐菜单（见下方说明）。
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

        /// <summary>
        /// 属性名 -> 中文 sheet 名的覆盖表。当前为占位（用属性名作 sheet）。
        /// 待用户提供 ProjectData 的属性名后，把 "Axes"->"轴" 这类映射填进来即可全部中文化。
        /// </summary>
        public static readonly System.Collections.Generic.Dictionary<string, string> SheetNameOverrides
            = new System.Collections.Generic.Dictionary<string, string>();

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
        public static System.Collections.Generic.IEnumerable<string> ListProjects()
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

        /// <summary>把根对象（ProjectData）导出为 多 sheet 字典。集合属性各成一张表，标量属性汇入「项目管理」信息表。</summary>
        public static IDictionary<string, DataTable> ExportToDataTables(object root)
        {
            var dict = new Dictionary<string, DataTable>();
            var meta = new DataTable("项目管理");
            meta.Columns.Add("属性", typeof(string));
            meta.Columns.Add("值", typeof(string));

            foreach (var p in root.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!p.CanRead) continue;
                if (IsCollection(p.PropertyType))
                {
                    var itemType = CollectionItemType(p.PropertyType);
                    if (itemType != null && !IsScalar(itemType))
                    {
                        var dt = CollectionToTable(p.GetValue(root), itemType);
                        dict[SheetKey(p.Name)] = dt;
                    }
                }
                else if (IsScalar(p.PropertyType) || p.PropertyType.IsEnum)
                {
                    meta.Rows.Add(p.Name, p.GetValue(root)?.ToString() ?? "");
                }
            }
            if (meta.Rows.Count > 0) dict["项目管理"] = meta;
            return dict;
        }

        /// <summary>从多 sheet 字典反射回填到根对象（ProjectData）。集合按 sheet 名匹配属性名后清空重建；标量回填「项目管理」信息表。</summary>
        public static void ImportFromDataTables(object root, IDictionary<string, DataTable> tables)
        {
            var propByName = root.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                  .Where(p => p.CanRead && p.CanWrite).ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var p in root.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!p.CanRead || !p.CanWrite) continue;
                if (!IsCollection(p.PropertyType)) continue;
                var itemType = CollectionItemType(p.PropertyType);
                if (itemType == null || IsScalar(itemType)) continue;
                if (!tables.TryGetValue(SheetKey(p.Name), out var dt)) continue;

                var coll = p.GetValue(root);
                p.PropertyType.GetMethod("Clear")?.Invoke(coll, null);
                var add = p.PropertyType.GetMethod("Add");
                if (add == null) continue;

                var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                    .Where(x => x.CanRead && x.CanWrite && (IsScalar(x.PropertyType) || x.PropertyType.IsEnum))
                                    .ToArray();
                foreach (DataRow dr in dt.Rows)
                {
                    var item = Activator.CreateInstance(itemType)!;
                    foreach (var pp in props)
                    {
                        if (!dt.Columns.Contains(pp.Name)) continue;
                        try { pp.SetValue(item, ConvertTo(pp.PropertyType, dr[pp.Name]?.ToString())); }
                        catch { /* 跳过无法转换的字段，保持默认值 */ }
                    }
                    add.Invoke(coll, new object[] { item });
                }
            }

            if (tables.TryGetValue("项目管理", out var meta))
                foreach (DataRow dr in meta.Rows)
                {
                    var name = dr["属性"]?.ToString();
                    var val = dr["值"]?.ToString();
                    if (name != null && propByName.TryGetValue(name, out var sp)
                        && (IsScalar(sp.PropertyType) || sp.PropertyType.IsEnum))
                    {
                        try { sp.SetValue(root, ConvertTo(sp.PropertyType, val)); } catch { }
                    }
                }
        }

        /// <summary>便捷：直接把根对象保存为工程 xlsx（导出+写文件）。</summary>
        public static void SaveProject(object root, string projectName)
            => Save(projectName, ExportToDataTables(root));

        /// <summary>便捷：从工程 xlsx 反射回填根对象（读文件+导入）。</summary>
        public static void OpenProject(object root, string projectName)
            => ImportFromDataTables(root, Load(projectName));

        // ===================== 内部工具 =====================

        private static bool IsScalar(Type t)
        {
            var u = Nullable.GetUnderlyingType(t);
            if (u != null) t = u;
            return ScalarTypes.Contains(t) || t.IsEnum;
        }

        private static bool IsCollection(Type t)
        {
            if (t == typeof(string)) return false;
            return typeof(System.Collections.IEnumerable).IsAssignableFrom(t)
                && (t.IsArray || (t.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(t)));
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
            if (collection is System.Collections.IEnumerable en)
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

        /// <summary>属性名 -> sheet 名（应用 SheetNameOverrides 覆盖表）。</summary>
        private static string SheetKey(string propertyName)
            => SheetNameOverrides.TryGetValue(propertyName, out var v) ? v : propertyName;

        private static string SafeSheetName(string name)
        {
            char[] invalid = { '\\', '/', '?', '*', '[', ']', ':' };
            var s = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            if (s.Length > 31) s = s.Substring(0, 31);
            return string.IsNullOrWhiteSpace(s) ? "Sheet" : s;
        }
    }
}
