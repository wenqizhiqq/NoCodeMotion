// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‌启‎志⁠◆‍编⁣写​◇⁠微⁠信‎﹕​1​8​7‏◆‏1‌9​3‍6‌◇⁣1‍3‎9‍9‏　⁣※‌保‍留⁣所​有‍权⁠利⁠请‍勿‌删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Windows.Controls;
using System.Windows.Threading;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Hardware;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    public partial class IoPage : UserControl
    {
        private readonly DispatcherTimer _ioPoll = new() { Interval = TimeSpan.FromMilliseconds(250) };

        public IoPage()
        {
            InitializeComponent();
            DataContext = new IoViewModel();
            // 仿真运行时 IO 状态变化 → 刷新"运行时"列高亮
            SimRuntime.Changed += OnSimChanged;

            // 真实硬件在线时周期读取输入电平，让「电平状态」反映实际输入
            _ioPoll.Tick += (_, _) => RefreshInputLevels();
            _ioPoll.Start();

            Unloaded += (_, _) =>
            {
                SimRuntime.Changed -= OnSimChanged;
                _ioPoll.Stop();
            };
        }

        private void OnSimChanged()
        {
            if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke((System.Action)OnSimChanged); return; }
            InputGrid?.Items.Refresh();
            OutputGrid?.Items.Refresh();
        }

        /// <summary>硬件就绪时周期读取输入点电平（未接卡 / 仿真不轮询，避免无谓开销）。</summary>
        private void RefreshInputLevels()
        {
            if (!HardwareSetup.IsCardReady) return;
            var inputs = ProjectStore.Data?.Inputs;
            if (inputs == null) return;

            foreach (var io in inputs)
            {
                try { io.Value = (int)HardwareBridge.Current.ReadInput(io); }
                catch { /* 单点读失败不影响其它 */ }
            }
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
