using System.Windows;
using NoCodeMotion.Services;

namespace NoCodeMotion
{
    public partial class App : Application
    {
        public App()
        {
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
