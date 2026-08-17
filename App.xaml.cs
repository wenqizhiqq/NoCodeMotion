using System.Windows;
using NoCodeMotion.Services;

namespace NoCodeMotion
{
    public partial class App : Application
    {
        public App()
        {
            // 启动时先载入已保存的工程配置，确保各页面 ViewModel 引用的是同一份数据
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
