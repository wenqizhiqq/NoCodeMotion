// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Collections.ObjectModel;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Views;

namespace NoCodeMotion.ViewModels
{
    /// <summary>项目管理页面：以表格列出全部工程（名称/创建时间/修改时间/备注），并提供 新建 / 打开(读取) / 保存 / 删除 / 重命名 / 刷新。</summary>
    public class ProjectManagerViewModel : ViewModelBase, IEnsureDefaultSelection
    {
        public ObservableCollection<ProjectEntry> Projects { get; } = new();

        private ProjectEntry? _selectedEntry;
        public ProjectEntry? SelectedEntry
        {
            get => _selectedEntry;
            set => SetField(ref _selectedEntry, value);
        }

        public string? CurrentProject => ProjectManager.CurrentName;

        public ICommand NewCommand => new RelayCommand(_ => New());
        public ICommand OpenCommand => new RelayCommand(_ => Open(), _ => SelectedEntry != null);
        public ICommand SaveCommand => new RelayCommand(_ => Save());
        public ICommand DeleteCommand => new RelayCommand(_ => Delete(), _ => SelectedEntry != null);
        public ICommand RenameCommand => new RelayCommand(_ => Rename(), _ => SelectedEntry != null);
        public ICommand RefreshCommand => new RelayCommand(_ => Refresh());

        public ProjectManagerViewModel()
        {
            Refresh();
        }

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
