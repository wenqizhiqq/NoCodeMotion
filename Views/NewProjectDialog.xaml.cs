// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧✦⁣
// =====================================================================
// Apple 风格「新建工程」弹窗：左侧按分类折叠的模板列表 + 右侧详情预览 + 名称输入。
// ShowDialog() 返回 true 且 ResultName 非空 + SelectedTemplate 非 null 时表示确认创建。
// 模板库定义见 Services/ProjectTemplateCatalog.cs。
// =====================================================================
using System.Windows;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.Views
{
    public partial class NewProjectDialog : Window
    {
        /// <summary>用户输入的工程名（确认时回写）。</summary>
        public string? ResultName { get; private set; }

        /// <summary>用户选中的模板（确认时回写）。非空。</summary>
        public ProjectTemplate? SelectedTemplate { get; private set; }

        public NewProjectDialog(string defaultName)
        {
            InitializeComponent();
            DataContext = ProjectTemplateCatalog.All;
            NameBox.Text = defaultName;
            Owner = Application.Current?.MainWindow;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 默认选中「空白工程」模板
            foreach (var t in ProjectTemplateCatalog.All)
            {
                if (t.Id == "empty")
                {
                    TemplateList.SelectedItem = t;
                    break;
                }
            }
            if (TemplateList.SelectedItem == null && ProjectTemplateCatalog.All.Count > 0)
                TemplateList.SelectedItem = ProjectTemplateCatalog.All[0];
            TemplateList.Focus();
            // 名称框后获得焦点（让用户立即可以改名）
            NameBox.Focus();
            NameBox.SelectAll();
        }

        private void TemplateList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 双击模板行 = 直接创建
            if (TemplateList.SelectedItem is ProjectTemplate)
                Confirm();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e) => Confirm();

        private void Confirm()
        {
            var name = NameBox.Text?.Trim();
            if (string.IsNullOrEmpty(name)) return;
            if (TemplateList.SelectedItem is not ProjectTemplate tpl) return;
            ResultName = name;
            SelectedTemplate = tpl;
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
// ◇作者保留所有权利　请勿删除※⁣
// ◆◇※▣▤▥▦▧✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧⁣