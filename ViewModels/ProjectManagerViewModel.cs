using System.Collections.ObjectModel;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Views;

namespace NoCodeMotion.ViewModels
{
    /// <summary>项目管理页面：列出全部工程，并提供 新建 / 打开(读取) / 保存 / 删除 / 重命名 / 刷新。</summary>
    public class ProjectManagerViewModel : ViewModelBase, IEnsureDefaultSelection
    {
        public ObservableCollection<string> Projects { get; } = new();

        private string? _selectedProject;
        public string? SelectedProject
        {
            get => _selectedProject;
            set => SetField(ref _selectedProject, value);
        }

        public string? CurrentProject => ProjectManager.CurrentName;

        public ICommand NewCommand => new RelayCommand(_ => New());
        public ICommand OpenCommand => new RelayCommand(_ => Open(), _ => SelectedProject != null);
        public ICommand SaveCommand => new RelayCommand(_ => Save());
        public ICommand DeleteCommand => new RelayCommand(_ => Delete(), _ => SelectedProject != null);
        public ICommand RenameCommand => new RelayCommand(_ => Rename(), _ => SelectedProject != null);
        public ICommand RefreshCommand => new RelayCommand(_ => Refresh());

        public ProjectManagerViewModel()
        {
            Refresh();
        }

        private void Refresh()
        {
            Projects.Clear();
            foreach (var n in ProjectManager.ListProjects())
                Projects.Add(n);
            OnPropertyChanged(nameof(CurrentProject));
        }

        private void New()
        {
            var dlg = new RenameDialog("新建工程", "工程" + (Projects.Count + 1));
            if (dlg.ShowDialog() != true || string.IsNullOrEmpty(dlg.ResultName)) return;
            var name = dlg.ResultName!;
            if (ProjectManager.Exists(name))
                ProjectManager.OpenProject(name);
            else
                ProjectManager.NewProject(name);
            // 新建/打开会触发界面重建，列表由重建后的页面重新刷新，此处无需额外 Refresh
        }

        private void Open()
        {
            if (SelectedProject == null) return;
            ProjectManager.OpenProject(SelectedProject);
        }

        private void Save()
        {
            string? name = SelectedProject ?? ProjectManager.CurrentName;
            if (string.IsNullOrEmpty(name))
            {
                var dlg = new RenameDialog("保存工程", "工程1");
                if (dlg.ShowDialog() != true || string.IsNullOrEmpty(dlg.ResultName)) return;
                name = dlg.ResultName!;
            }
            ProjectManager.SaveCurrent(name);
            Refresh();
            SelectedProject = name;
        }

        private void Delete()
        {
            if (SelectedProject == null) return;
            var dlg = new ConfirmDialog("删除工程", $"确定删除工程「{SelectedProject}」？此操作不可撤销。");
            if (dlg.ShowDialog() != true) return;
            ProjectManager.DeleteProject(SelectedProject);
            Refresh();
            SelectedProject = null;
        }

        private void Rename()
        {
            if (SelectedProject == null) return;
            var dlg = new RenameDialog("重命名工程", SelectedProject);
            if (dlg.ShowDialog() != true || string.IsNullOrEmpty(dlg.ResultName)) return;
            var newName = dlg.ResultName!;
            if (newName != SelectedProject && ProjectManager.Exists(newName)) return;
            ProjectManager.RenameProject(SelectedProject, newName);
            Refresh();
            SelectedProject = newName;
        }

        public void EnsureDefaultSelection()
        {
            Refresh();
            if (SelectedProject == null && Projects.Count > 0)
                SelectedProject = Projects[0];
        }
    }
}