// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁣启‍志⁠◆‍编⁠写‍◇‏微​信​﹕‌1⁠8​7‍◆‏1‌9⁠3‏6‏◇​1⁠3‏9‎9​　‌※⁠保‏留‎所‎有‏权‍利‏请⁣勿⁣删‎除⁠◇​⁣​
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

            // ★ 工程载入已移到 MainWindow 的启动流程（StartUpAsync）里做：
            //   在 App 构造函数里载入时主窗口还不存在，读 xlsx 的这段时间用户只能看到白屏、没有任何提示。
            //   放到主窗口 Loaded 之后，就能在「加载遮罩（进度条 + 当前初始化内容）」下完成整个启动初始化。
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
