// ◆◇※▣▤▥▦✧⚝☢☣➤◈❖◆◇※⁣
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇⁣
// ◆◇※▣▤✧⚝☢☣➤◈❖◆◇※⁣
using System.IO;
using System.Linq;
using System.Timers;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 工程配置存取：从 xlsx 文件自动载入、自动保存（防抖）。
    /// 工程文件存储由 <see cref="ProjectManager"/> 管理（每个工程 = <c>Projects\工程名.xlsx</c>）；
    /// 本类负责「未命名工程的兜底」与「数据变更后防抖自动保存」两个职责。
    /// </summary>
    public static class ProjectStore
    {
        public static ProjectData Data { get; private set; } = new();

        /// <summary>原地载入工程时临时屏蔽自动保存，避免替换集合时触发大量写盘。</summary>
        private static bool _suppressSave;

        /// <summary>设置是否屏蔽 ScheduleSave（原地载入工程期间应为 true）。</summary>
        public static void SuppressSave(bool suppress) => _suppressSave = suppress;

        /// <summary>未指定 CurrentName 时的兜底工程名。</summary>
        public const string DefaultProjectName = "_DefaultProject";

        private static System.Timers.Timer? _saveTimer;

        public static void Load()
        {
            try
            {
                XlsxProjectStore.ConfigureRoot(ProjectManager.RootDir);
                // 一次性迁移：把旧版 .json 工程转成 .xlsx（保留原数据，避免用户工程丢失）
                MigrateJsonToXlsx();

                var data = new ProjectData();
                XlsxProjectStore.OpenProject(data, DefaultProjectName);
                // 即便兜底工程不存在，OpenProject 也是 no-op，data 仍是空 ProjectData（OK）。
                Data = data;
            }
            catch
            {
                // 载入失败则保持空数据，避免崩溃
            }

            // 迁移旧的单一点位表 → 多工位结构，并补齐 4 个轴槽；然后重建全局名称库
            Data.EnsurePointTables();
            Catalog.SyncAllFromData(Data);
        }

        /// <summary>
        /// 一次性迁移：把 Projects/*.json 工程文件转成 .xlsx。
        /// 严格按「先 xlsx 落地成功，再删 .json」的顺序，避免任何迁移异常导致数据丢失。
        /// 迁移过的工程名记到 lastproject.txt 后保留不动；同名 .xlsx 已存在则跳过迁移（保留新格式）。
        /// </summary>
        private static void MigrateJsonToXlsx()
        {
            var root = ProjectManager.RootDir;
            if (!Directory.Exists(root)) return;
            var jsonFiles = Directory.EnumerateFiles(root, "*.json").ToList();
            if (jsonFiles.Count == 0) return;

            foreach (var jf in jsonFiles)
            {
                var name = Path.GetFileNameWithoutExtension(jf) ?? "";
                if (string.IsNullOrEmpty(name)) continue;

                // 跳过迁移标记文件 lastproject.txt 之类（其实不是 .json，仅作防御）
                if (name.StartsWith(".")) continue;

                // 已存在同名 .xlsx 就不覆盖，让新格式优先
                var xlsxPath = Path.Combine(root, name + ".xlsx");
                if (File.Exists(xlsxPath)) continue;

                ProjectData? data = null;
                try
                {
                    var text = File.ReadAllText(jf);
                    data = ProjectJsonAnnotator.Deserialize(text);
                }
                catch { /* 读不动就保留 .json 原样 */ }

                if (data == null) continue;

                try
                {
                    XlsxProjectStore.SaveProject(data, name);
                }
                catch
                {
                    // xlsx 写失败：保留 .json 原样不删
                    continue;
                }

                // xlsx 落地成功后再删 .json；如果删除失败也不影响功能（下次启动会跳过同名迁移）
                try { File.Delete(jf); } catch { }
            }
        }

        public static void Save()
        {
            try
            {
                // 若当前已打开某个工程，则把所有页面参数保存到该工程文件（单一真实来源）
                if (!string.IsNullOrEmpty(ProjectManager.CurrentName))
                {
                    ProjectManager.SaveCurrent();
                    return;
                }

                // 无当前工程时写入兜底工程文件，避免数据丢失
                XlsxProjectStore.ConfigureRoot(ProjectManager.RootDir);
                Data.UpdatedAt = System.DateTime.Now;
                XlsxProjectStore.SaveProject(Data, DefaultProjectName);
            }
            catch
            {
                // 忽略保存失败（如文件被占用）
            }
        }

        /// <summary>安排一次延迟保存（防抖 400ms），避免每次改动都立即写盘。</summary>
        public static void ScheduleSave()
        {
            if (_suppressSave) return;
            if (_saveTimer == null)
            {
                _saveTimer = new System.Timers.Timer(400) { AutoReset = false };
                _saveTimer.Elapsed += (_, _) => Save();
            }
            _saveTimer.Stop();
            _saveTimer.Start();
        }
    }
}
// ◇作者保留所有权利　请勿删除※⁣
// ◆◇※⁣