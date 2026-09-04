// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// 气缸页代码后端：订阅 SimRuntime.Changed 刷新时序表实时状态列。
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NoCodeMotion.Services;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 气缸页。直接以 CylinderViewModel 充当 DataContext，EditorPage 通过 Items/SelectedItem/
    /// AddCommand/DeleteCommand/RenameCommand 五个契约自动接管左侧增删。
    /// 订阅 SimRuntime.Changed 以刷新「气缸时序动作表」的实时状态列。
    /// </summary>
    public partial class CylinderPage : UserControl
    {
        private DataGrid? _seqGrid;

        public CylinderPage()
        {
            InitializeComponent();
            DataContext = new CylinderViewModel();
            SimRuntime.Changed += OnSimChanged;
            Loaded += (_, _) => _seqGrid = FindVisualChildByTag<DataGrid>(this, "SeqGrid");
            Unloaded += (_, _) => SimRuntime.Changed -= OnSimChanged;
        }

        private void OnSimChanged()
        {
            // 气缸运行时状态变化 → 重算时序表里每个气缸的实时状态点/文字。
            Dispatcher.Invoke(() => _seqGrid?.Items.Refresh());
        }

        private static T? FindVisualChildByTag<T>(DependencyObject root, string tag) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T t && child is FrameworkElement fe && fe.Tag as string == tag)
                    return t;
                var inner = FindVisualChildByTag<T>(child, tag);
                if (inner != null) return inner;
            }
            return null;
        }
    }
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣ۤ▦▧▨▩ᑒ▓✦✧⚝☢☣➤◈❖◆◇※▣ۤ
