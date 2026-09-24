// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‏启​志⁠◆⁠编⁣写⁣◇‎微⁠信⁠﹕⁠1‍8⁠7⁠◆⁣1‌9⁣3‎6‍◇‎1‍3‍9‏9⁣　‏※‎保‍留‎所⁠有⁠权‎利‍请‏勿‎删⁣除⁣◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Hardware;
using NoCodeMotion.Services.Hardware.Cards;

namespace NoCodeMotion.ViewModels
{
    /// <summary>单个 IO 面板（输入或输出）：复用通用表格面板，定制 IO 行的创建与克隆逻辑。</summary>
    public class IoPanelViewModel : TablePanelViewModel<IoItem>
    {
        /// <summary>本面板是否为输入（否则为输出）——决定「功能」候选目录与合法性校验。</summary>
        public bool IsInput => Title != "输出";

        public IoPanelViewModel(string title, System.Collections.ObjectModel.ObservableCollection<IoItem> items)
            : base(title, items)
        {
            NormalizeFunctions();
        }

        /// <summary>把不在本方向功能目录里的旧值（如旧工程里的「动点」）统一归一到「无」。</summary>
        private void NormalizeFunctions()
        {
            foreach (var io in Items)
            {
                if (io == null) continue;
                var want = IoFunctionCatalog.Normalize(io.Function, IsInput);
                if (!string.Equals(io.Function, want, StringComparison.Ordinal)) io.Function = want;
            }
        }

        protected override IoItem MakeNew(int index)
        {
            int nextSeq = Items.Count == 0 ? 1 : Items.Max(i => i.Sequence) + 1;
            return new IoItem
            {
                Name = $"{Title}{nextSeq}",
                Sequence = nextSeq,
                Level = "取反",
                Function = IoFunctionCatalog.None
            };
        }

        protected override IoItem Clone(IoItem src)
        {
            var json = JsonSerializer.Serialize(src);
            var item = JsonSerializer.Deserialize<IoItem>(json)!;
            item.Name = $"{item.Name}_副本";
            return item;
        }

        protected override void OnItemChanged(IoItem item, string? propertyName)
        {
            if (propertyName == nameof(EditorItemBase.Name))
                SyncIoCatalog();

            // 配置页改值实时下发设备：输出点电平变化 → 写输出到设备。
            if (HardwarePush.ShouldPush && Title == "输出" && propertyName == nameof(IoItem.Value))
                HardwareBridge.Current.WriteOutput(item, item.Value);
        }

        private void SyncIoCatalog()
        {
            var all = ProjectStore.Data.Inputs.Select(i => i.Name)
                .Concat(ProjectStore.Data.Outputs.Select(i => i.Name));
            Catalog.SetIo(all);
        }

        /// <summary>Excel 回读替换后，名称变化发生在订阅之前，OnItemChanged 收不到 → 主动全量同步一次目录。</summary>
        protected override void OnAfterExcelReplace(IList<IoItem> imported)
        {
            SyncIoCatalog();
            NormalizeFunctions();
        }

        // ===== 输出 IO 行内手动开关：真正下发到控制卡（点哪行驱动哪行，无需先选中） =====

        /// <summary>置位指定输出点（参数为当前行 IoItem）。</summary>
        public ICommand OutputHighCommand => new RelayCommand(p => { if (p is IoItem item) ApplyOutput(item, 1); });

        /// <summary>复位指定输出点（参数为当前行 IoItem）。</summary>
        public ICommand OutputLowCommand => new RelayCommand(p => { if (p is IoItem item) ApplyOutput(item, 0); });

        /// <summary>单个开关：在当前值上取反并下发（参数为当前行 IoItem）。</summary>
        public ICommand ToggleOutputCommand => new RelayCommand(p =>
        {
            if (p is IoItem item) ApplyOutput(item, item.Value != 0 ? 0 : 1);
        });

        /// <summary>
        /// 真正把输出电平下发到控制卡：先按工程装配硬件层（卡族 / 雷赛），再写输出，并同步仿真状态。
        /// <para>写失败时把界面值回退，避免「看着已开、其实没下发」。</para>
        /// </summary>
        private void ApplyOutput(IoItem item, int value)
        {
            if (item == null || Title != "输出") return;
            value = value != 0 ? 1 : 0;

            int previous = item.Value;
            item.Value = value;                       // 界面先行（开关 / 电平状态立即高亮）
            SimRuntime.SetOutput(item.Name, value);   // 同步仿真（3D / 运行时高亮）

            try
            {
                EnsureHardwareLayer();
                HardwareBridge.Current.WriteOutput(item, value);
                HardwareLog.Write($"[IO] 输出「{item.Name}」={value}（{(value != 0 ? "高" : "低")}电平）已下发"
                                  + $"（{HardwareSetup.Mode} / {HardwareBridge.Current.GetType().Name}）");
                StatusBarService.ReportInfo($"输出「{item.Name}」→ {(value != 0 ? "高电平(开)" : "低电平(关)")}");
            }
            catch (System.Exception ex)
            {
                item.Value = previous;                // 下发失败 → 回退，界面与实际一致
                SimRuntime.SetOutput(item.Name, previous);
                HardwareLog.Write($"[IO] 输出「{item.Name}」下发失败：{ex.Message}");
                StatusBarService.ReportException($"输出「{item.Name}」下发失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 确保硬件层与当前工程一致：<see cref="HardwareSetup.EnsureInitialized"/> 只装配一次，
        /// 若此前已按旧工程（或无工程）装成雷赛 / 仿真层，而当前工程里有非雷赛卡族（含虚拟卡），
        /// 这里补一次切换到卡族层，否则虚拟卡 / 已移植卡的点会发不出去。
        /// </summary>
        private static void EnsureHardwareLayer()
        {
            HardwareSetup.EnsureInitialized();
            if (HardwareSetup.Mode != HardwareMode.CardFamilies && WenQiZhiCardBridge.CanServeProject(out _))
                HardwareSetup.UseCardFamilies();
        }
    }

    /// <summary>IO 页面顶层 ViewModel：包含两个面板（输入/输出）。</summary>
    public class IoViewModel : ViewModelBase, IEnsureDefaultSelection
    {
        public IoPanelViewModel InputPanel { get; }
        public IoPanelViewModel OutputPanel { get; }

        public IoViewModel()
        {
            InputPanel = new IoPanelViewModel("输入", ProjectStore.Data.Inputs);
            OutputPanel = new IoPanelViewModel("输出", ProjectStore.Data.Outputs);

            // 启动时把当前数据快照一下，让"回撤"可以撤销到首次加载
            InputPanel.Snapshot();
            OutputPanel.Snapshot();

            AxisControllerViewModel.DetectedCountsChanged += OnDetectedCountsChanged;
        }

        /// <summary>已连接控制卡从底层硬件真实检测到的输入 IO 数量之和（连接后自动获取，显示在 IO 页面顶部）。</summary>
        public int DetectedInIo
        {
            get
            {
                int sum = 0;
                var ctls = ProjectStore.Data?.Controllers;
                if (ctls == null) return 0;
                foreach (var c in ctls)
                    if (IsControllerReadyNow(c.Name)) sum += c.DetectedInIo;
                return sum;
            }
        }

        /// <summary>已连接控制卡从底层硬件真实检测到的输出 IO 数量之和（连接后自动获取，显示在 IO 页面顶部）。</summary>
        public int DetectedOutIo
        {
            get
            {
                int sum = 0;
                var ctls = ProjectStore.Data?.Controllers;
                if (ctls == null) return 0;
                foreach (var c in ctls)
                    if (IsControllerReadyNow(c.Name)) sum += c.DetectedOutIo;
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
        {
            OnPropertyChanged(nameof(DetectedInIo));
            OnPropertyChanged(nameof(DetectedOutIo));
        }

        public void EnsureDefaultSelection()
        {
            if (InputPanel.SelectedItem == null && InputPanel.Items.Count > 0)
                InputPanel.SelectedItem = InputPanel.Items[0];
            if (OutputPanel.SelectedItem == null && OutputPanel.Items.Count > 0)
                OutputPanel.SelectedItem = OutputPanel.Items[0];
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
