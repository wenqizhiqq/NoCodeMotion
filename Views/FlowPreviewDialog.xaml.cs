// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
using System.Collections.Generic;
using System.Windows;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 流程步骤预览对话框：把仿真编译后的步骤动作序列（SimFlowPlayer.StepLabels）逐条列出，
    /// 让用户在不实际运行的情况下，确认流程将按预期执行（含分支/循环展开后的真实步序）。
    /// </summary>
    public partial class FlowPreviewDialog : Window
    {
        public FlowPreviewDialog(string flowName, int stepCount, IEnumerable<string> steps)
        {
            InitializeComponent();
            Owner = Application.Current?.MainWindow;
            TitleText.Text = $"流程步骤预览 · {flowName}";
            SummaryText.Text = stepCount > 0
                ? $"该流程经仿真编译后共 {stepCount} 步（分支/循环已展开），以下为实际执行顺序。"
                : "该流程没有可编译的步骤（空流程或未包含可执行节点）。";

            int i = 1;
            foreach (var s in steps)
                StepList.Items.Add(new { Index = i++, Text = string.IsNullOrWhiteSpace(s) ? "(空步骤)" : s });
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
