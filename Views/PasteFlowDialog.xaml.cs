// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁣启​志‌◆​编​写‎◇‏微⁠信​﹕‌1‏8​7‎◆⁠1‌9​3‌6‌◇​1⁠3​9‏9‌　‎※⁣保⁣留⁠所​有‍权​利​请⁣勿‏删‎除‍◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NoCodeMotion.Services;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// Apple 风格「粘贴生成流程」弹窗。
    /// 弹窗内直接写清四步操作说明，中间是 AI 返回 JSON 的输入框（自动填入剪贴板内容，可编辑、可重新读取），
    /// 下方实时预览「识别到几个流程 / 各是什么类型 / 多少步骤」，点【确定生成】才真正写入流程。
    /// ShowDialog() 返回 true 时 <see cref="Json"/> 为待导入的 JSON，<see cref="FlowCount"/> 为识别到的流程个数。
    /// </summary>
    public partial class PasteFlowDialog : Window
    {
        /// <summary>确定后待导入的流程 JSON（取消时为 null）。</summary>
        public string? Json { get; private set; }

        /// <summary>当前输入框里识别到的流程个数（0 = 还没粘对内容，确定按钮会被禁用）。</summary>
        public int FlowCount { get; private set; }

        /// <summary>当前选中的流程名（null = 未选中，此时只做新增）。</summary>
        private readonly string? _targetName;

        public PasteFlowDialog(string? clipboardText, string? targetName)
        {
            InitializeComponent();
            _targetName = string.IsNullOrWhiteSpace(targetName) ? null : targetName;

            StepText.Text =
                "① 在流程页点【复制JSON】，提示词自动进剪贴板\r\n" +
                "② 粘到豆包 / WorkBuddy 等 AI 对话，补一句你的需求（例：改成 3 工位循环），让它返回流程 JSON\r\n" +
                "③ 复制 AI 返回的 JSON，粘到下面输入框（剪贴板有内容时会自动填入）\r\n" +
                "④ 点【确定生成】写入流程" + TargetHint();

            JsonBox.Text = clipboardText ?? string.Empty;
            Owner = Application.Current?.MainWindow;
            Loaded += (_, __) =>
            {
                JsonBox.Focus();
                JsonBox.CaretIndex = JsonBox.Text.Length;
            };
            UpdatePreview();
        }

        /// <summary>第四步后面补一句「这次会覆盖还是新增」，避免用户不知道会动到哪个流程。</summary>
        private string TargetHint() => _targetName == null
            ? "\r\n（当前未选中流程：AI 返回的内容会作为新流程加进工程）"
            : $"\r\n（当前选中「{_targetName}」：AI 只返回 1 个流程时覆盖它的内容、名称保留；返回多个则全部新增）";

        private void JsonBox_TextChanged(object sender, TextChangedEventArgs e) => UpdatePreview();

        /// <summary>实时预览：解析失败/没识别到流程时给出原因，并禁用【确定生成】。</summary>
        private void UpdatePreview()
        {
            var text = JsonBox.Text ?? string.Empty;
            var summary = AiProjectExchange.PreviewFlowJson(text, out var count);
            FlowCount = count;

            bool ok = count > 0;
            ConfirmButton.IsEnabled = ok;
            PreviewText.Text = ok
                ? summary + (count == 1 && _targetName != null
                    ? $"　→　将【覆盖】当前流程「{_targetName}」（名称保留）"
                    : "　→　将【新增】到工程")
                : summary;
            PreviewText.Foreground = Brush(ok ? "TextSecondaryBrush" : "DangerBrush");
        }

        private Brush Brush(string key) =>
            (Brush)(TryFindResource(key) ?? Brushes.Gray);

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!System.Windows.Clipboard.ContainsText()) return;
                JsonBox.Text = System.Windows.Clipboard.GetText();
            }
            catch
            {
                // 剪贴板被别的程序占用时忽略即可，用户还能手动 Ctrl+V
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (FlowCount <= 0) return;   // 没识别到流程就不让确定，避免生成空流程
            Json = JsonBox.Text;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
