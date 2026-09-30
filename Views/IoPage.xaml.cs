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

            // 周期读取输入 / 输出电平，让两张表的「电平状态」反映真实硬件
            _ioPoll.Tick += (_, _) => RefreshLevels();
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

        /// <summary>周期刷新输入 / 输出两张表的电平状态。</summary>
        private void RefreshLevels()
        {
            RefreshInputLevels();
            RefreshOutputLevels();
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

        /// <summary>
        /// 周期读回输出点**真实**电平（卡上的实际输出，不是界面期望值）。
        /// <para>仿真模式没有卡可读，改为回放 <see cref="SimRuntime"/> 里记录的输出状态，
        /// 这样流程 / 3D 视图驱动过的输出也能在表里实时亮起来。</para>
        /// </summary>
        private void RefreshOutputLevels()
        {
            var outputs = ProjectStore.Data?.Outputs;
            if (outputs == null) return;

            bool sim = HardwareSetup.Mode == HardwareMode.Simulation || !HardwareSetup.IsCardReady;

            foreach (var io in outputs)
            {
                if (io == null) continue;
                // 用户正在这一行编辑（如改名称 / 套码）时不打断
                if (IsRowEditing(OutputGrid, io)) continue;

                try
                {
                    if (sim) io.Value = SimRuntime.GetOutput(io.Name);
                    else io.Value = (int)HardwareBridge.Current.ReadOutput(io);
                }
                catch { /* 单点读失败不影响其它 */ }
            }
        }

        /// <summary>该行是否正处于编辑态（编辑框有焦点）——编辑时不要用采样值覆盖输入框所在行。</summary>
        private static bool IsRowEditing(DataGrid grid, object item)
        {
            if (grid == null || !grid.IsKeyboardFocusWithin) return false;
            var cell = grid.CurrentCell;
            return cell.IsValid && ReferenceEquals(cell.Item, item);
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
