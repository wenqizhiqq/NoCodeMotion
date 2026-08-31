// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 状态栏绑定用 VM：订阅 StatusBarService.StateChanged，把全局状态转发为 INPC 属性变更。
    /// 构造时把当前工程名（ProjectManager.CurrentName）同步到服务，保证打开即显示。
    /// </summary>
    public class StatusBarViewModel : ViewModelBase
    {
        public StatusBarViewModel()
        {
            StatusBarService.StateChanged += (_, _) => RaiseAll();
            // 构造即同步一次当前工程名（App 构造函数已打开上次工程）
            StatusBarService.SetProject(ProjectManager.CurrentName ?? "未打开工程");
        }

        private void RaiseAll()
        {
            OnPropertyChanged(nameof(ProjectName));
            OnPropertyChanged(nameof(UserName));
            OnPropertyChanged(nameof(UserRole));
            OnPropertyChanged(nameof(RunStatusText));
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(EStopped));
            OnPropertyChanged(nameof(RunColor));
            OnPropertyChanged(nameof(ExceptionText));
            OnPropertyChanged(nameof(HasException));
        }

        public string ProjectName => StatusBarService.ProjectName;
        public string UserName => StatusBarService.UserName;
        public string UserRole => StatusBarService.UserRole;
        public string RunStatusText => StatusBarService.RunStatusText;
        public bool IsRunning => StatusBarService.IsRunning;
        public bool EStopped => StatusBarService.EStopped;
        public string RunColor => StatusBarService.RunColor;
        public string ExceptionText => StatusBarService.ExceptionText;
        public bool HasException => StatusBarService.HasException;
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
