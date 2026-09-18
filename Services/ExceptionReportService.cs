// ◆◇※⁣
// ◆温启志◆编写◇微信﹕18719361399　※保留所有权利请勿删除⁣
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NoCodeMotion.Services
{
    /// <summary>单条结构化异常记录（设备异常状态上报的基础单元）。</summary>
    public sealed class ExceptionRecord
    {
        public DateTime Time;
        public string Source = "";   // 来源：流程 / 状态栏 / UI
        public string Flow = "";     // 关联流程名（无则空）
        public string Message = "";
    }

    /// <summary>
    /// 设备异常状态上报服务（静态单例）：
    /// 内存环形缓冲（最多 1000 条）+ 自动追加落盘 Logs/exceptions_日期.csv；
    /// 启动时载入当天已落盘记录；支持导出 CSV 归档。
    /// 流程运行异常、状态栏异常统一汇入此处，作为 OEE/报警分析的数据源。
    /// </summary>
    public static class ExceptionReportService
    {
        private static readonly List<ExceptionRecord> _ring = new();
        private const int CAP = 1000;
        private static readonly object _lock = new();

        static ExceptionReportService()
        {
            try
            {
                var f = CsvPath();
                if (File.Exists(f))
                {
                    var data = File.ReadAllLines(f, Encoding.UTF8).Skip(1).ToList();
                    if (data.Count > CAP) data = data.Skip(data.Count - CAP).ToList();
                    foreach (var ln in data)
                    {
                        var parts = SplitCsv(ln);
                        if (parts.Length >= 4 && DateTime.TryParse(parts[0], out var t))
                            _ring.Add(new ExceptionRecord { Time = t, Source = parts[1], Flow = parts[2], Message = parts[3] });
                    }
                }
            }
            catch { }
        }

        public static void Record(string source, string flow, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            var rec = new ExceptionRecord { Time = DateTime.Now, Source = source ?? "", Flow = flow ?? "", Message = message };
            lock (_lock)
            {
                _ring.Add(rec);
                if (_ring.Count > CAP) _ring.RemoveAt(0);
            }
            AppendCsv(rec);
        }

        public static List<ExceptionRecord> Snapshot() { lock (_lock) return _ring.ToList(); }

        public static void Clear() { lock (_lock) _ring.Clear(); }

        private static string CsvPath()
        {
            var root = ProjectManager.RootDir ?? AppDomain.CurrentDomain.BaseDirectory;
            var dir = Path.Combine(root, "Logs");
            return Path.Combine(dir, $"exceptions_{DateTime.Now:yyyyMMdd}.csv");
        }

        private static void AppendCsv(ExceptionRecord r)
        {
            try
            {
                var dir = Path.GetDirectoryName(CsvPath());
                if (dir != null) Directory.CreateDirectory(dir);
                bool header = !File.Exists(CsvPath());
                using var sw = new StreamWriter(CsvPath(), true, Encoding.UTF8);
                if (header) sw.WriteLine("时间,来源,流程,消息");
                sw.WriteLine($"{r.Time:yyyy-MM-dd HH:mm:ss},{CsvEsc(r.Source)},{CsvEsc(r.Flow)},{CsvEsc(r.Message)}");
            }
            catch { }
        }

        public static void ExportCsv(string path)
        {
            var list = Snapshot();
            var sb = new StringBuilder();
            sb.AppendLine("时间,来源,流程,消息");
            foreach (var r in list) sb.AppendLine($"{r.Time:yyyy-MM-dd HH:mm:ss},{CsvEsc(r.Source)},{CsvEsc(r.Flow)},{CsvEsc(r.Message)}");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static string CsvEsc(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

        private static string[] SplitCsv(string line)
        {
            var res = new List<string>();
            bool q = false; var cur = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (q)
                {
                    if (c == '"') { if (i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; } else q = false; }
                    else cur.Append(c);
                }
                else
                {
                    if (c == '"') q = true;
                    else if (c == ',') { res.Add(cur.ToString()); cur.Clear(); }
                    else cur.Append(c);
                }
            }
            res.Add(cur.ToString());
            return res.ToArray();
        }
    }
}
// ◇作者保留所有权利　请勿删除※⁣
