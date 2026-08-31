// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Views;

namespace NoCodeMotion.ViewModels
{
    /// <summary>项目管理页面：左列表 + 右详情（备注 / 需求 / 复制给AI / 粘贴生成）。</summary>
    public class ProjectManagerViewModel : ViewModelBase, IEnsureDefaultSelection
    {
        public ObservableCollection<ProjectEntry> Projects { get; } = new();

        private ProjectEntry? _selectedEntry;
        public ProjectEntry? SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (SetField(ref _selectedEntry, value))
                {
                    LoadDetailFor(value);
                    OnPropertyChanged(nameof(CanEditDetail));
                }
            }
        }

        public string? CurrentProject => ProjectManager.CurrentName;

        // ==================== 右侧详情：备注 + 需求 ====================

        /// <summary>有选中工程时才允许编辑备注/需求。</summary>
        public bool CanEditDetail => SelectedEntry != null;

        private string _detailRemark = "";
        /// <summary>右侧详情的备注（编辑后点【保存备注】写回工程文件）。</summary>
        public string DetailRemark
        {
            get => _detailRemark;
            set => SetField(ref _detailRemark, value);
        }

        /// <summary>右侧详情的需求（多行文本，每行一条需求或一段自然语言描述）。\n        /// 编辑后点【保存需求】写回工程文件。</summary>
        private string _requirementsText = "";
        public string RequirementsText
        {
            get => _requirementsText;
            set => SetField(ref _requirementsText, value);
        }

        private string _statusMessage = "";
        /// <summary>操作结果提示（复制成功 / 粘贴生成结果 / 错误信息）。</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        // ==================== 命令 ====================

        public ICommand NewCommand => new RelayCommand(_ => New());
        public ICommand OpenCommand => new RelayCommand(_ => Open(), _ => SelectedEntry != null);
        public ICommand SaveCommand => new RelayCommand(_ => Save());
        public ICommand DeleteCommand => new RelayCommand(_ => Delete(), _ => SelectedEntry != null);
        public ICommand RenameCommand => new RelayCommand(_ => Rename(), _ => SelectedEntry != null);
        public ICommand RefreshCommand => new RelayCommand(_ => Refresh());

        public ICommand SaveRemarkCommand => new RelayCommand(_ => SaveRemark(), _ => SelectedEntry != null);
        public ICommand SaveRequirementsCommand => new RelayCommand(_ => SaveRequirements(), _ => SelectedEntry != null);
        public ICommand CopyPromptCommand => new RelayCommand(_ => CopyPrompt(), _ => SelectedEntry != null);
        public ICommand PasteGenerateCommand => new RelayCommand(_ => PasteGenerate());

        public ProjectManagerViewModel()
        {
            Refresh();
        }

        // ==================== 详情加载 / 保存 ====================

        /// <summary>选中某个工程后，从工程文件读取备注与需求到右侧详情。</summary>
        private void LoadDetailFor(ProjectEntry? entry)
        {
            RequirementsText = "";

            if (entry == null) return;

            DetailRemark = entry.Remark ?? "";
            RequirementsText = ProjectManager.GetRequirementsText(entry.Name);
        }

        private void SaveRemark()
        {
            if (SelectedEntry == null) return;
            ProjectManager.SetRemark(SelectedEntry.Name, DetailRemark);
            SelectedEntry.Remark = DetailRemark;
            Refresh();
            StatusMessage = "备注已保存。";
        }

        /// <summary>把右侧需求文本写回工程文件（ProjectData.RequirementsText）。</summary>
        private void SaveRequirements()
        {
            if (SelectedEntry == null) return;
            ProjectManager.SetRequirementsText(SelectedEntry.Name, RequirementsText);
            var lineCount = RequirementsText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
            StatusMessage = string.IsNullOrWhiteSpace(RequirementsText)
                ? "需求已清空。"
                : $"需求已保存（共 {lineCount} 条）。";
        }

        // ==================== 复制 / 粘贴（AI 往返） ====================

        /// <summary>【复制需求】生成给 AI 的提示词（含 JSON 契约）到剪贴板。</summary>
        private void CopyPrompt()
        {
            if (SelectedEntry == null) return;
            try
            {
                var prompt = AiProjectExchange.BuildPrompt(
                    SelectedEntry.Name,
                    string.IsNullOrWhiteSpace(DetailRemark) ? SelectedEntry.Remark : DetailRemark,
                    RequirementsText);
                Clipboard.SetText(prompt);
                StatusMessage = $"提示词已复制（工程「{SelectedEntry.Name}」+ {RequirementsText.Length} 字需求）。粘贴到 AI 即可生成配置。";
            }
            catch (System.Exception ex)
            {
                StatusMessage = "复制失败：" + ex.Message;
            }
        }

        /// <summary>【粘贴生成】读取剪贴板里的 AI 返回 JSON 并应用到当前工程。</summary>
        private void PasteGenerate()
        {
            string text;
            try
            {
                if (!Clipboard.ContainsText())
                {
                    StatusMessage = "剪贴板没有文本内容。请先复制 AI 返回的 JSON。";
                    return;
                }
                text = Clipboard.GetText();
            }
            catch (System.Exception ex)
            {
                StatusMessage = "读取剪贴板失败：" + ex.Message;
                return;
            }

            // 没有打开工程时，拿选中项打开一个作为生成目标
            var target = ProjectManager.CurrentName;
            if (string.IsNullOrEmpty(target) && SelectedEntry != null)
            {
                ProjectManager.OpenProject(SelectedEntry.Name);
                target = SelectedEntry.Name;
            }
            if (string.IsNullOrEmpty(target))
            {
                StatusMessage = "请先双击列表打开一个工程，再粘贴生成。";
                return;
            }

            // 弹窗确认：粘贴生成会清空当前工程所有配置
            var confirm = new ConfirmDialog(
                "粘贴生成确认",
                "粘贴 AI 返回的 JSON 会【完全清空】工程「" + target + "」当前的\n" +
                "控制器 / 轴 / 输入 / 输出 / 气缸 / 通讯 / 相机 / 工位 / 流程 / 变量\n\n" +
                "然后写入新内容。是否继续？",
                "粘贴生成");
            if (confirm.ShowDialog() != true)
            {
                StatusMessage = "已取消粘贴生成。";
                return;
            }

            var result = AiProjectExchange.ApplyGenerated(ProjectStore.Data, text);
            if (result.StartsWith("未识别") || result.StartsWith("JSON 解析失败") || result.StartsWith("内容不是"))
            {
                StatusMessage = result;
                return;
            }

            ProjectManager.SaveCurrent(target);
            Refresh();
            StatusMessage = result + "。已保存到工程「" + target + "」。";
        }

        // ==================== 原有工程操作 ====================

        private void Refresh()
        {
            Projects.Clear();
            foreach (var e in ProjectManager.ListProjectEntries())
                Projects.Add(e);
            OnPropertyChanged(nameof(CurrentProject));
        }

        /// <summary>备注单元格编辑结束后调用：把备注写回对应工程文件。</summary>
        public void PersistRemark(ProjectEntry entry) => ProjectManager.SetRemark(entry.Name, entry.Remark);

        private void New()
        {
            // 弹新弹窗：选模板 + 输入工程名。取消 / 无效输入直接退出。
            var dlg = new NewProjectDialog("工程" + (Projects.Count + 1));
            if (dlg.ShowDialog() != true) return;
            var name = dlg.ResultName!;
            var template = dlg.SelectedTemplate!;
            if (ProjectManager.Exists(name))
            {
                // 同名已存在则直接打开，避免覆盖；用户想覆盖可手动删除后重建。
                ProjectManager.OpenProject(name);
            }
            else
            {
                ProjectManager.NewProject(name, template);
            }
            // 新建/打开会触发界面重建，列表由重建后的页面重新刷新，此处无需额外 Refresh
        }

        private void Open()
        {
            if (SelectedEntry == null) return;
            ProjectManager.OpenProject(SelectedEntry.Name);
        }

        private void Save()
        {
            string? name = SelectedEntry?.Name ?? ProjectManager.CurrentName;
            if (string.IsNullOrEmpty(name))
            {
                var dlg = new RenameDialog("保存工程", "工程1");
                if (dlg.ShowDialog() != true || string.IsNullOrEmpty(dlg.ResultName)) return;
                name = dlg.ResultName!;
            }
            ProjectManager.SaveCurrent(name);
            Refresh();
            SelectedEntry = Projects.FirstOrDefault(p => p.Name == name);
        }

        private void Delete()
        {
            if (SelectedEntry == null) return;
            var dlg = new ConfirmDialog("删除工程", $"确定删除工程「{SelectedEntry.Name}」？此操作不可撤销。");
            if (dlg.ShowDialog() != true) return;
            ProjectManager.DeleteProject(SelectedEntry.Name);
            Refresh();
            SelectedEntry = null;
        }

        private void Rename()
        {
            if (SelectedEntry == null) return;
            var dlg = new RenameDialog("重命名工程", SelectedEntry.Name);
            if (dlg.ShowDialog() != true || string.IsNullOrEmpty(dlg.ResultName)) return;
            var newName = dlg.ResultName!;
            if (newName != SelectedEntry.Name && ProjectManager.Exists(newName)) return;
            ProjectManager.RenameProject(SelectedEntry.Name, newName);
            Refresh();
            SelectedEntry = Projects.FirstOrDefault(p => p.Name == newName);
        }

        public void EnsureDefaultSelection()
        {
            Refresh();
            if (SelectedEntry == null && Projects.Count > 0)
                SelectedEntry = Projects[0];
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
