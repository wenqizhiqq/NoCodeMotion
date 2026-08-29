// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NoCodeMotion.Models;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 新建流程弹窗：输入流程名称 + **单选**一个模板（选中后下方实时预览该模板将生成的步骤）。
    /// 构造参数：
    ///   kind        —— 流程类型（决定标题）
    ///   templates   —— 可选模板名列表（单选）
    ///   stepsProvider —— 模板名 → 该模板的步骤名列表（用于预览）
    /// 返回值：DialogResult=true 时通过 FlowName / SelectedTemplate 读取用户输入。
    /// </summary>
    public partial class FlowCreateDialog : Window
    {
        public string FlowName { get; private set; } = "";
        public string SelectedTemplate { get; private set; } = "";

        private readonly List<RadioButton> _templateButtons = new();
        private readonly Func<string, IEnumerable<string>>? _stepsProvider;

        public FlowCreateDialog(FlowKind kind, IEnumerable<string> templates,
            Func<string, IEnumerable<string>>? stepsProvider = null,
            string defaultName = "新流程")
        {
            InitializeComponent();
            _stepsProvider = stepsProvider;

            Title = kind switch
            {
                FlowKind.Table => "新建运控流程",   // 表格流程已更名为"运控"（运动控制）
                FlowKind.Lua => "新建脚本流程",
                FlowKind.Vision => "新建视觉流程",
                _ => "新建流程"
            };
            HeaderText.Text = Title;

            // 默认名称（带同类 kind 内的序号，由调用方算好后传入）
            NameBox.Text = defaultName;
            NameBox.SelectAll();
            NameBox.Focus();

            // 模板单选按钮（RadioButton 互斥，默认选中第一项）
            foreach (var t in templates)
            {
                var rb = new RadioButton
                {
                    Content = t,
                    GroupName = "TemplateGroup",
                    IsChecked = _templateButtons.Count == 0,
                    Margin = new Thickness(0, 4, 0, 4),
                    FontSize = 13
                };
                rb.Checked += (_, _) => UpdatePreview();
                TemplatePanel.Children.Add(rb);
                _templateButtons.Add(rb);
            }
            UpdatePreview();
        }

        /// <summary>选中项变化时刷新「将生成的步骤」预览。</summary>
        private void UpdatePreview()
        {
            var cur = _templateButtons.FirstOrDefault(r => r.IsChecked == true);
            string tpl = cur?.Content as string ?? "";
            if (_stepsProvider == null || string.IsNullOrEmpty(tpl))
            {
                PreviewText.Text = "无预览";
                return;
            }
            var steps = _stepsProvider(tpl)?.ToList() ?? new List<string>();
            PreviewText.Text = steps.Count == 0
                ? "该模板暂无步骤"
                : string.Join(" → ", steps);
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            FlowName = (NameBox.Text ?? "").Trim();
            if (string.IsNullOrEmpty(FlowName))
            {
                MessageBox.Show("请输入流程名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameBox.Focus();
                return;
            }
            var cur = _templateButtons.FirstOrDefault(r => r.IsChecked == true);
            SelectedTemplate = cur?.Content as string ?? "";
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
// ◇作者保留所有权利　请勿删除※⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓⁣