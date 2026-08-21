using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

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
