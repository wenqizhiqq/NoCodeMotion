// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‏启‌志‏◆⁣编⁠写​◇⁣微⁣信‎﹕​1⁣8‏7⁣◆‌1‍9​3‍6‎◇⁠1⁣3​9‎9‎　⁠※⁠保‏留‏所‎有​权‍利‍请⁣勿⁣删​除‌◇​⁣​
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
            OnPropertyChanged(nameof(InfoText));
            OnPropertyChanged(nameof(HasInfo));
            OnPropertyChanged(nameof(ControllerStatusText));
            OnPropertyChanged(nameof(ControllerColor));
            OnPropertyChanged(nameof(ControllerConnecting));
            OnPropertyChanged(nameof(ControllerConnectingText));
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
        public string InfoText => StatusBarService.InfoText;
        public bool HasInfo => StatusBarService.HasInfo;

        /// <summary>控制器连接状态文本（无控制器 / N/M 在线）。</summary>
        public string ControllerStatusText => StatusBarService.ControllerStatusText;
        /// <summary>控制器状态圆点颜色。</summary>
        public string ControllerColor => StatusBarService.ControllerColor;
        /// <summary>控制器是否正在后台连接（状态栏进度条显隐）。</summary>
        public bool ControllerConnecting => StatusBarService.ControllerConnecting;
        /// <summary>控制器连接中提示文本。</summary>
        public string ControllerConnectingText => StatusBarService.ControllerConnectingText;
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
