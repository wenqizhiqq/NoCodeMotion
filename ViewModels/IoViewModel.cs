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
        }

        private void SyncIoCatalog()
        {
            var all = ProjectStore.Data.Inputs.Select(i => i.Name)
                .Concat(ProjectStore.Data.Outputs.Select(i => i.Name));
            Catalog.SetIo(all);
        }
    }

    /// <summary>IO 页面顶层 ViewModel：包含两个面板（输入/输出）。</summary>
    public class IoViewModel : ViewModelBase
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

        /// <summary>Excel编辑 按钮占位：未来可以打开 Excel 导入/导出对话框。</summary>
        public ICommand ExcelEditCommand => new RelayCommand(_ =>
            System.Windows.MessageBox.Show("Excel 批量编辑功能尚未实现", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information));
    }
}
