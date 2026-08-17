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

        private static readonly string FilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "NoCodeMotion", "project.json");

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
                    {
                        Data = data;
                        Catalog.SyncAllFromData(Data);
                    }
                }
            }
            catch
            {
                // 载入失败则保持空数据，避免崩溃
            }
        }

        public static void Save()
        {
            try
            {
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
