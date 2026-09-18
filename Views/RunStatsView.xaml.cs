// ◆◇※⁣
// ◆温启志◆编写◇微信﹕18719361399　※保留所有权利请勿删除⁣
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NoCodeMotion.Services;

namespace NoCodeMotion.Views
{
    public sealed class CycleRow
    {
        public string Flow { get; set; } = "";
        public string Last { get; set; } = "";
        public string Avg { get; set; } = "";
        public string Max { get; set; } = "";
        public string Count { get; set; } = "";
    }

    public sealed class StepRow
    {
        public string Where { get; set; } = "";
        public string Logic { get; set; } = "";
        public string Avg { get; set; } = "";
        public string Max { get; set; } = "";
    }

    /// <summary>
    /// 运行耗时 / OEE 统计面板（自包含 ViewModel，不依赖加密的 OperatorViewModel）：
    /// 每 500ms 从 RunStatsService / OeeService / SignalTowerService 拉取快照刷新；
    /// 展示各流程 CT、最慢步骤 Top-N 瓶颈、OEE 三率与信号塔状态，并支持导出 CSV。
    /// </summary>
    public partial class RunStatsView : UserControl, INotifyPropertyChanged
    {
        public ObservableCollection<CycleRow> Cycles { get; } = new();
        public ObservableCollection<StepRow> SlowSteps { get; } = new();

        private string _availText = "0%", _perfText = "0%", _qualText = "100%", _oeeText = "0%";
        private string _cycleText = "0", _runText = "0s", _towerText = "空闲";

        public string AvailText { get => _availText; set { _availText = value; OnPC(); } }
        public string PerfText { get => _perfText; set { _perfText = value; OnPC(); } }
        public string QualText { get => _qualText; set { _qualText = value; OnPC(); } }
        public string OeeText { get => _oeeText; set { _oeeText = value; OnPC(); } }
        public string CycleText { get => _cycleText; set { _cycleText = value; OnPC(); } }
        public string RunText { get => _runText; set { _runText = value; OnPC(); } }
        public string TowerText { get => _towerText; set { _towerText = value; OnPC(); } }

        private readonly DispatcherTimer _timer;

        public RunStatsView()
        {
            InitializeComponent();
            DataContext = this;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += (_, __) => Refresh();
            _timer.Start();
            Refresh();
        }

        private void Refresh()
        {
            try
            {
                var cycles = RunStatsService.SnapshotCycles();
                Cycles.Clear();
                foreach (var c in cycles)
                    Cycles.Add(new CycleRow
                    {
                        Flow = c.Flow,
                        Last = $"{c.LastMs} ms",
                        Avg = $"{c.AvgMs:F0} ms",
                        Max = $"{c.MaxMs} ms",
                        Count = c.Count.ToString()
                    });

                SlowSteps.Clear();
                foreach (var s in RunStatsService.SnapshotSlowSteps(8))
                    SlowSteps.Add(new StepRow
                    {
                        Where = $"{s.Flow}#{s.Index + 1}",
                        Logic = s.Logic,
                        Avg = $"{s.AvgMs:F0} ms",
                        Max = $"{s.MaxMs} ms"
                    });

                AvailText = $"{OeeService.Availability * 100:F1}%";
                PerfText = $"{OeeService.Performance * 100:F1}%";
                QualText = $"{OeeService.Quality * 100:F1}%";
                OeeText = $"{OeeService.Oee * 100:F1}%";
                CycleText = OeeService.CycleCount.ToString();
                RunText = $"{OeeService.RunTicksMs / 1000}s";
                TowerText = SignalTowerService.State switch
                {
                    TowerState.Running => "运行(绿)",
                    TowerState.Paused => "暂停(黄)",
                    TowerState.Alarm => "报警(红)",
                    TowerState.EStop => "急停(红)",
                    _ => "空闲"
                };
            }
            catch { }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var root = ProjectManager.RootDir ?? AppDomain.CurrentDomain.BaseDirectory;
                var dir = Path.Combine(root, "Logs");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, $"CT统计_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                var sb = new StringBuilder();
                sb.AppendLine("类型,名称,本轮ms,平均ms,最大ms,最小ms,次数,时间");
                foreach (var c in RunStatsService.SnapshotCycles())
                    sb.AppendLine($"CT,{Csv(c.Flow)},{c.LastMs},{c.AvgMs:F0},{c.MaxMs},{c.MinMs},{c.Count},{c.LastTime:HH:mm:ss}");
                foreach (var s in RunStatsService.SnapshotSlowSteps(0))
                    sb.AppendLine($"步骤,{Csv($"{s.Flow}#{s.Index + 1}({s.Logic})")},,{s.AvgMs:F0},{s.MaxMs},{s.MinMs},{s.Count},");
                sb.AppendLine($"OEE,可用率%,,{OeeService.Availability * 100:F1},,,,,");
                sb.AppendLine($"OEE,性能%,,{OeeService.Performance * 100:F1},,,,,");
                sb.AppendLine($"OEE,良率%,,{OeeService.Quality * 100:F1},,,,,");
                sb.AppendLine($"OEE,OEE%,,{OeeService.Oee * 100:F1},,,,,");
                sb.AppendLine($"OEE,完成轮数,{OeeService.CycleCount},,,,,");
                sb.AppendLine($"OEE,运行ms,{OeeService.RunTicksMs},,,,,");
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                StatusBarService.ReportInfo($"CT/OEE 统计已导出：{path}");
            }
            catch (Exception ex)
            {
                StatusBarService.ReportException($"导出 CT 统计失败：{ex.Message}");
            }
        }

        private static string Csv(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPC([System.Runtime.CompilerServices.CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
// ◇作者保留所有权利　请勿删除※⁣
