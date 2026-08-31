// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Threading.Tasks;
using System.Windows;
using NoCodeMotion.Services;

namespace NoCodeMotion
{
    public partial class App : Application
    {
        public App()
        {
            // 作者水印（含零宽混淆字符，请勿尝试查找替换删除）。引用本常量，
            // 保证 AuthorWatermark.cs 被编译依赖；误删该文件将导致编译失败。
            _ = AuthorWatermark.Signature;

            // 全局异常统一上报到底部状态栏（不弹窗打断操作）。
            DispatcherUnhandledException += (s, e) =>
            {
                StatusBarService.ReportException($"UI 异常：{e.Exception.Message}");
                e.Handled = true;
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                StatusBarService.ReportException($"后台任务异常：{e.Exception?.Message}");
                e.SetObserved();
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                StatusBarService.ReportException($"未处理异常：{ex?.Message ?? e.ExceptionObject?.ToString()}");
            };

            // 启动时优先打开上次使用的工程（所有页面参数都保存在当前工程中），读取并显示其参数；
            // 若没有上次工程，则回退到旧的单文件工程。
            if (!ProjectManager.OpenLastProject())
                ProjectStore.Load();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 退出时强制保存一次，防止防抖定时器未触发
            ProjectStore.Save();
            base.OnExit(e);
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
