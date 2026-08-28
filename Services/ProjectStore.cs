// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.IO;
using System.Text.Json;
using System.Timers;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 工程配置存取：从 JSON 文件自动载入、自动保存（防抖）。
    /// 文件位置：%LocalAppData%\NoCodeMotion\project.json
    /// </summary>
    public static class ProjectStore
    {
        public static ProjectData Data { get; private set; } = new();

        /// <summary>原地载入工程时临时屏蔽自动保存，避免替换集合时触发大量写盘。</summary>
        private static bool _suppressSave;

        /// <summary>设置是否屏蔽 ScheduleSave（原地载入工程期间应为 true）。</summary>
        public static void SuppressSave(bool suppress) => _suppressSave = suppress;

        private static readonly string FilePath =  "project.json";

        private static System.Timers.Timer? _saveTimer;

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    var data = JsonSerializer.Deserialize<ProjectData>(json);
                    if (data != null)
                        Data = data;
                }
            }
            catch
            {
                // 载入失败则保持空数据，避免崩溃
            }

            // 迁移旧的单一点位表 → 多工位结构，并补齐 4 个轴槽；然后重建全局名称库
            Data.EnsurePointTables();
            Catalog.SyncAllFromData(Data);
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

                var dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
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
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
