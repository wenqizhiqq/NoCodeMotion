// ◆◇※⁣
// ◆温启志◆编写◇微信﹕18719361399　※保留所有权利请勿删除⁣
using System.IO;
using System.Text.Json;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 报警 / 信号塔 / OEE 目标 配置（独立于 xlsx，存于工程目录 side-JSON：
    /// &lt;RootDir&gt;/&lt;工程名&gt;.telemetry.json）。
    /// 红/黄/绿/蜂鸣器 四项填写「输出 IO 的名称」（与 IO 表一致），留空表示该灯/蜂鸣器不接。
    /// </summary>
    public class AlarmConfig
    {
        public string GreenOutput { get; set; } = "";   // 运行（绿）
        public string YellowOutput { get; set; } = "";  // 暂停（黄）
        public string RedOutput { get; set; } = "";     // 报警/急停（红）
        public string BuzzerOutput { get; set; } = "";  // 蜂鸣器

        public int BuzzerOnMs { get; set; } = 400;      // 蜂鸣单次响时长
        public int BuzzerOffMs { get; set; } = 300;     // 蜂鸣间隔
        public int BuzzerRepeat { get; set; } = 3;      // 蜂鸣次数（报警/急停时）

        public double TargetCycleSec { get; set; } = 5; // 目标节拍（性能计算）
        public double YieldTargetPct { get; set; } = 98; // 良率目标（展示参考）

        public bool AutoSafeOnEStop { get; set; } = true;   // 急停后自动回安全位
        public bool AutoSafeOnException { get; set; } = true; // 异常后自动回安全位

        private static AlarmConfig _current = new();
        public static AlarmConfig Current => _current;

        /// <summary>重新从工程 side-JSON 载入（工程打开后调用，确保配置与当前工程一致）。</summary>
        public static void Reload()
        {
            try
            {
                var p = PathFor();
                if (File.Exists(p))
                {
                    var txt = File.ReadAllText(p);
                    var cfg = JsonSerializer.Deserialize<AlarmConfig>(txt);
                    if (cfg != null) _current = cfg;
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                var p = PathFor();
                var dir = Path.GetDirectoryName(p);
                if (dir != null) Directory.CreateDirectory(dir);
                File.WriteAllText(p, JsonSerializer.Serialize(_current, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        private static string PathFor()
        {
            var root = ProjectManager.RootDir ?? "";
            var name = string.IsNullOrEmpty(ProjectManager.CurrentName) ? ProjectStore.DefaultProjectName : ProjectManager.CurrentName;
            return Path.Combine(root, name + ".telemetry.json");
        }
    }
}
// ◇作者保留所有权利　请勿删除※⁣
