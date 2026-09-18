// ◆◇※⁣
// ◆温启志◆编写◇微信﹕18719361399　※保留所有权利请勿删除⁣
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 报警 / 信号塔 / OEE 配置对话框：直接绑定 AlarmConfig.Current（side-JSON 单例）；
    /// 三色灯/蜂鸣器输出从工程 IO 表的输出项名称选取；保存即写盘。
    /// </summary>
    public partial class AlarmConfigDialog : Window
    {
        public AlarmConfigDialog()
        {
            InitializeComponent();
            try { AlarmConfig.Reload(); } catch { }
            DataContext = AlarmConfig.Current;

            var names = new List<string> { "" };
            if (ProjectStore.Data?.Outputs != null)
                names.AddRange(ProjectStore.Data.Outputs.Select(o => o.Name).Where(n => !string.IsNullOrEmpty(n)));
            GreenCombo.ItemsSource = names;
            YellowCombo.ItemsSource = names;
            RedCombo.ItemsSource = names;
            BuzzerCombo.ItemsSource = names;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try { AlarmConfig.Save(); StatusBarService.ReportInfo("报警/信号塔/OEE 配置已保存。"); }
            catch (System.Exception ex) { StatusBarService.ReportException($"保存配置失败：{ex.Message}"); }
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
// ◇作者保留所有权利　请勿删除※⁣
