// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‏启​志⁠◆⁠编⁣写⁣◇‎微⁠信⁠﹕⁠1‍8⁠7⁠◆⁣1‌9⁣3‎6‍◇‎1‍3‍9‏9⁣　‏※‎保‍留‎所⁠有⁠权‎利‍请‏勿‎删⁣除⁣◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.ViewModels
{
    /// <summary>单个 IO 面板（输入或输出）：复用通用表格面板，定制 IO 行的创建与克隆逻辑。</summary>
    public class IoPanelViewModel : TablePanelViewModel<IoItem>
    {
        public IoPanelViewModel(string title, System.Collections.ObjectModel.ObservableCollection<IoItem> items)
            : base(title, items) { }

        protected override IoItem MakeNew(int index)
        {
            int nextSeq = Items.Count == 0 ? 1 : Items.Max(i => i.Sequence) + 1;
            return new IoItem
            {
                Name = $"{Title}{nextSeq}",
                Sequence = nextSeq,
                Level = "取反"
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
            => SyncIoCatalog();

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
                HardwareSetup.EnsureInitialized();    // 按工程选真实硬件层（卡族 / 雷赛），未初始化时只装配一次
                HardwareBridge.Current.WriteOutput(item, value);
                HardwareLog.Write($"[IO] 输出「{item.Name}」={value}（{(value != 0 ? "高" : "低")}电平）已下发"
                                  + $"（{HardwareSetup.Mode}）");
            }
            catch (System.Exception ex)
            {
                item.Value = previous;                // 下发失败 → 回退，界面与实际一致
                SimRuntime.SetOutput(item.Name, previous);
                HardwareLog.Write($"[IO] 输出「{item.Name}」下发失败：{ex.Message}");
            }
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
