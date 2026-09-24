// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启‏志‌◆​编‌写‏◇​微​信‎﹕‍1​8‍7‏◆‏1​9‍3⁣6​◇⁠1‌3‏9​9​　⁠※⁠保‍留‌所‎有​权‌利‏请‏勿‍删​除​◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Hardware;
using NoCodeMotion.Services.Hardware.Cards;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.ViewModels
{
    public class AxisViewModel : ListEditorViewModel<AxisItem>, IEnsureDefaultSelection
    {
        public AxisViewModel()
        {
            CatalogCategory = "Axis";
            Items = ProjectStore.Data.Axes;
            Counter = Items.Count;
            AttachAutoSave();
            AxisControllerViewModel.DetectedCountsChanged += OnDetectedCountsChanged;

            // 「轴状态与控制」只针对当前选中的轴（见 Monitor / OnPropertyChanged 钩子）

            // 实时状态刷新（300ms）：只在轴页可见时跑，且硬件读取全部在后台线程
            _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _statusTimer.Tick += (_, __) => RefreshStatuses();
        }

        // ===================== 轴状态与控制（只针对「左侧当前选中的轴」）=====================

        private AxisRowViewModel? _monitor;

        /// <summary>当前选中轴的「状态与控制」视图模型；没选中轴时为 null。</summary>
        public AxisRowViewModel? Monitor
        {
            get => _monitor;
            private set
            {
                if (ReferenceEquals(_monitor, value)) return;
                _monitor = value;
                OnPropertyChanged(nameof(Monitor));
                OnPropertyChanged(nameof(HasMonitor));
            }
        }

        /// <summary>是否已选中轴（控制卡片内容 / 空提示的显隐）。</summary>
        public bool HasMonitor => _monitor != null;

        /// <summary>轴状态字位定义提示（显示在卡片上，方便现场对着卡手册核对）。</summary>
        public string IoWordHint => AxisMonitorService.IoWordHint;

        private readonly DispatcherTimer _statusTimer;
        private int _reading;

        /// <summary>
        /// 选中轴 / 增删轴都会走到这里（基类 SelectedItem 的 SetField 会发 SelectedItem 通知），
        /// 据此重建监控对象 —— 保证「状态与控制」永远对准当前选中的那一个轴。
        /// </summary>
        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName == nameof(SelectedItem))
                Monitor = SelectedItem == null ? null : new AxisRowViewModel(SelectedItem);
        }

        /// <summary>轴页显示 / 隐藏时调用：隐藏时停掉 300ms 硬件轮询，避免在别的页面上空转读卡。</summary>
        public void SetStatusRefreshEnabled(bool enabled)
        {
            if (enabled)
            {
                if (!_statusTimer.IsEnabled) _statusTimer.Start();
                RefreshStatuses();
            }
            else
            {
                _statusTimer.Stop();
            }
        }

        /// <summary>
        /// 300ms 轮询：后台读「当前选中轴」的底层实时状态，回 UI 线程赋值。
        /// 上一次还没读完就跳过这一拍；没选中轴 / 没连卡时不去读硬件（不编造状态）。
        /// </summary>
        private void RefreshStatuses()
        {
            var m = Monitor;
            if (m == null) return;
            if (Interlocked.CompareExchange(ref _reading, 1, 0) != 0) return;

            // 一个控制器都没连上时不必去读卡，直接置「未连接」。
            if (!HardwareSetup.IsCardReady)
            {
                m.Snapshot = new AxisStatusSnapshot { Message = "未连接" };
                Interlocked.Exchange(ref _reading, 0);
                return;
            }

            Task.Run(() =>
            {
                AxisStatusSnapshot snap;
                try { snap = AxisMonitorService.Read(m.Item); }
                catch (Exception ex) { snap = new AxisStatusSnapshot { Message = "读取失败：" + ex.Message }; }

                var app = System.Windows.Application.Current;
                if (app == null) { Interlocked.Exchange(ref _reading, 0); return; }

                app.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (ReferenceEquals(Monitor, m)) m.Snapshot = snap;   // 期间换轴了就别回填
                    Interlocked.Exchange(ref _reading, 0);
                }), DispatcherPriority.Background);
            });
        }

        /// <summary>已连接控制卡从底层硬件真实检测到的轴数量之和（连接后自动获取，显示在轴页面顶部）。</summary>
        public int DetectedAxisCount
        {
            get
            {
                int sum = 0;
                var ctls = ProjectStore.Data?.Controllers;
                if (ctls == null) return 0;
                foreach (var c in ctls)
                    if (IsControllerReadyNow(c.Name)) sum += c.DetectedAxisCount;
                return sum;
            }
        }

        private static bool IsControllerReadyNow(string name)
        {
            if (HardwareSetup.Mode == HardwareMode.Leadshine) return HardwareSetup.IsCardReady;
            var b = HardwareBridge.Current as WenQiZhiCardBridge;
            return b != null && b.IsControllerReady(name);
        }

        private void OnDetectedCountsChanged(object? sender, EventArgs e)
            => OnPropertyChanged(nameof(DetectedAxisCount));

        protected override AxisItem CreateNewItem() => new AxisItem { Name = $"轴{Counter + 1}" };

        /// <summary>配置页改值实时下发设备：速度变化下发到卡，使能/电平变化重新使能轴。</summary>
        protected override void PushItem(AxisItem item, string? propertyName)
        {
            var bridge = HardwareBridge.Current;
            if (propertyName == nameof(AxisItem.Speed))
                bridge.SetAxisSpeed(item, item.Speed);
            else if (propertyName == nameof(AxisItem.Enabled) || propertyName == nameof(AxisItem.EnableLevel))
                bridge.EnableAxis(item);
        }

        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0) SelectedItem = Items[0];
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
