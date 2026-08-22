using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 多工程管理：在 %LocalAppData%\NoCodeMotion\Projects 下以「工程名.json」文件管理多个工程，
    /// 提供 列出 / 新建 / 打开(读取) / 保存 / 删除 / 重命名 / 改备注。
    /// 打开/新建采用「原地载入」：只替换 ProjectStore.Data 各集合的内容，不替换 Data 实例，
    /// 因此各页面 ViewModel 持有的集合引用始终有效，无需重建即可看到新工程数据。
    /// </summary>
    public static class ProjectManager
    {
        public static string RootDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "NoCodeMotion", "Projects");

        /// <summary>当前已打开/保存的工程名（未指定为 null）。</summary>
        public static string? CurrentName { get; private set; }

        /// <summary>工程数据被原地替换后触发，供主窗口清空页面缓存并重建当前页。</summary>
        public static event System.Action? DataReloaded;

        /// <summary>列出全部工程条目（含创建/修改时间、备注）。</summary>
        public static List<ProjectEntry> ListProjectEntries()
        {
            var list = new List<ProjectEntry>();
            try
            {
                if (!Directory.Exists(RootDir)) return list;
                foreach (var file in Directory.EnumerateFiles(RootDir, "*.json")
                             .OrderBy(f => Path.GetFileNameWithoutExtension(f)))
                {
                    var name = Path.GetFileNameWithoutExtension(file) ?? "";
                    DateTime? created = null, updated = null;
                    string? remark = "";
                    try
                    {
                        var data = JsonSerializer.Deserialize<ProjectData>(File.ReadAllText(file));
                        if (data != null)
                        {
                            created = data.CreatedAt;
                            updated = data.UpdatedAt;
                            remark = data.Remark;
                        }
                    }
                    catch { }
                    if (created == null) { try { created = File.GetCreationTime(file); } catch { } }
                    if (updated == null) { try { updated = File.GetLastWriteTime(file); } catch { } }
                    updated = updated ?? created;
                    list.Add(new ProjectEntry { Name = name, CreatedAt = created, UpdatedAt = updated, Remark = remark });
                }
            }
            catch { }
            return list;
        }

        private static string FileFor(string name) => Path.Combine(RootDir, name + ".json");

        public static bool Exists(string name) => File.Exists(FileFor(name));

        /// <summary>新建工程：写入空工程文件并原地载入为当前数据。</summary>
        public static void NewProject(string name)
        {
            var fresh = new ProjectData();
            fresh.EnsurePointTables();
            fresh.CreatedAt = DateTime.Now;
            CurrentName = name;
            WriteFile(name, fresh);
            LoadInto(fresh);
        }

        /// <summary>打开(读取)工程：从 name.json 载入为当前数据（原地，不替换 Data 实例）。</summary>
        public static void OpenProject(string name)
        {
            var path = FileFor(name);
            ProjectData data;
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    data = JsonSerializer.Deserialize<ProjectData>(json) ?? new ProjectData();
                }
                catch
                {
                    data = new ProjectData();
                }
            }
            else
            {
                data = new ProjectData();
            }
            data.EnsurePointTables();
            CurrentName = name;
            LoadInto(data);
        }

        /// <summary>保存当前工程（写入 name；默认保存到当前工程名）。</summary>
        public static void SaveCurrent(string? name = null)
        {
            name = name ?? CurrentName;
            if (string.IsNullOrEmpty(name)) return;
            CurrentName = name;
            WriteFile(name, ProjectStore.Data);
        }

        /// <summary>修改指定工程的备注（改写其 JSON 文件，并更新修改时间）。</summary>
        public static void SetRemark(string name, string? remark)
        {
            var path = FileFor(name);
            if (!File.Exists(path)) return;
            try
            {
                var data = JsonSerializer.Deserialize<ProjectData>(File.ReadAllText(path)) ?? new ProjectData();
                data.Remark = remark ?? "";
                WriteFile(name, data);
            }
            catch { }
        }

        public static void DeleteProject(string name)
        {
            try { File.Delete(FileFor(name)); } catch { }
            if (CurrentName == name) CurrentName = null;
        }

        public static void RenameProject(string oldName, string newName)
        {
            try
            {
                if (oldName != newName && File.Exists(FileFor(oldName)))
                    File.Move(FileFor(oldName), FileFor(newName), true);
            }
            catch { }
            if (CurrentName == oldName) CurrentName = newName;
        }

        private static void WriteFile(string name, ProjectData data)
        {
            try
            {
                Directory.CreateDirectory(RootDir);
                data.UpdatedAt = DateTime.Now;
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FileFor(name), json);
            }
            catch { }
        }

        /// <summary>把 src 内容原地复制到 ProjectStore.Data（保留集合实例），同步名称库后通知界面重建。</summary>
        private static void LoadInto(ProjectData src)
        {
            ProjectStore.SuppressSave(true);
            try
            {
                ProjectStore.Data.CopyFrom(src);
                ProjectStore.Data.EnsurePointTables();
                Catalog.SyncAllFromData(ProjectStore.Data);
            }
            finally
            {
                ProjectStore.SuppressSave(false);
            }
            DataReloaded?.Invoke();
        }
    }
}