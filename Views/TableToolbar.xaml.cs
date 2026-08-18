using System.Windows;
using System.Windows.Controls;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 通用表格工具栏（所有表格页都用它）：Excel编辑 / 添加 / 删除 / 上移 / 下移 / 复制 / 粘贴 / 回撤 / 重做 + 条目计数。
    /// 按钮命令（ExcelEditCommand / AddCommand / ... / RedoCommand）通过继承的 DataContext 自动绑定，
    /// 复用到任意表格面板（IO输入/输出、变量、流程步骤……）：把 TableToolbar 放进 DataContext 是正确 ViewModel 的区域即可。
    /// </summary>
    public partial class TableToolbar : UserControl
    {
        public TableToolbar() => InitializeComponent();

        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register(nameof(Count), typeof(int), typeof(TableToolbar), new PropertyMetadata(0));
        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        public static readonly DependencyProperty CountLabelProperty =
            DependencyProperty.Register(nameof(CountLabel), typeof(string), typeof(TableToolbar), new PropertyMetadata("项"));
        /// <summary>条目计数单位（"项" / "步" / "行"），默认"项"。</summary>
        public string CountLabel
        {
            get => (string)GetValue(CountLabelProperty);
            set => SetValue(CountLabelProperty, value);
        }
    }
}
