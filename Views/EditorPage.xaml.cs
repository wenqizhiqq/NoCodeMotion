// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‏启‎志⁣◆⁣编‍写‏◇‎微‎信⁣﹕‍1​8‏7⁣◆‏1​9​3​6‎◇‎1⁣3‍9​9⁠　‎※⁠保‎留⁠所​有⁠权⁣利‎请​勿​删‍除‍◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 字符串非空（非 null / 非空白）转 bool。用于 EditorPage 的空列表提示：
    /// 仅当宿主页设置了非空 EmptyHint 时才启用「空列表隐藏右侧详情 + 显示添加提示」。
    /// </summary>
    public class StringNotEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => !string.IsNullOrWhiteSpace(value as string);

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// 通用编辑页框架：顶部"添加/删除/重命名"工具栏 + 左侧项目列表 + 右侧详情区。
    /// 每个业务页（轴/IO/气缸…）只需把具体表单放进 Detail 属性即可复用整套布局与增删逻辑。
    /// 通过 LeftToolbarContent + ShowDefaultAddButton 允许宿主页（如流程页）注入
    /// 多个具体添加按钮并隐藏默认"添加"（如"添加运控 / 添加脚本"）。
    /// 通过 LeftListItemTemplate 允许宿主页（如气缸页）覆盖列表项模板，给每行加内联按钮。
    /// 重命名通过弹窗（RenameDialog）完成，列表项名称仅作只读展示。
    /// </summary>
    [ContentProperty(nameof(Detail))]
    public partial class EditorPage : UserControl
    {
        public static readonly DependencyProperty DetailProperty =
            DependencyProperty.Register(nameof(Detail), typeof(UIElement), typeof(EditorPage), new PropertyMetadata(null));

        public UIElement? Detail
        {
            get => (UIElement?)GetValue(DetailProperty);
            set => SetValue(DetailProperty, value);
        }

        /// <summary>是否显示默认的"添加"按钮。宿主页在提供自定义添加按钮时应设为 false。</summary>
        public static readonly DependencyProperty ShowDefaultAddButtonProperty =
            DependencyProperty.Register(nameof(ShowDefaultAddButton), typeof(bool), typeof(EditorPage),
                new PropertyMetadata(true));

        public bool ShowDefaultAddButton
        {
            get => (bool)GetValue(ShowDefaultAddButtonProperty);
            set => SetValue(ShowDefaultAddButtonProperty, value);
        }

        /// <summary>注入到顶部工具栏"添加"按钮左侧的自定义内容（如"添加运控 / 添加脚本"）。</summary>
        public static readonly DependencyProperty LeftToolbarContentProperty =
            DependencyProperty.Register(nameof(LeftToolbarContent), typeof(UIElement), typeof(EditorPage),
                new PropertyMetadata(null));

        public UIElement? LeftToolbarContent
        {
            get => (UIElement?)GetValue(LeftToolbarContentProperty);
            set => SetValue(LeftToolbarContentProperty, value);
        }

        /// <summary>
        /// 空列表提示文案。设了非空文案后，当左侧 Items 为空（Count==0）时隐藏右侧 Detail、
        /// 改为显示这条「请添加」提示；不设置（默认 null/空）则保持旧行为（始终显示 Detail）。
        /// 这样只有显式需要的页（控制器 / 轴 / 气缸…）才启用，其它页不受影响。
        /// </summary>
        public static readonly DependencyProperty EmptyHintProperty =
            DependencyProperty.Register(nameof(EmptyHint), typeof(string), typeof(EditorPage), new PropertyMetadata(null));

        public string? EmptyHint
        {
            get => (string?)GetValue(EmptyHintProperty);
            set => SetValue(EmptyHintProperty, value);
        }

        /// <summary>自定义左侧列表项模板。宿主页（如气缸页）想给每行加内联按钮时设置此属性；
        /// 不设置则用 EditorListItemTemplate（默认模板，流程页/通用页用）。
        /// **关键**：XAML 里 ListBox.ItemTemplate 必须是直接引用（StaticResource），不能用 Binding +
        /// FallbackValue——后者在 WPF 中 FallbackValue 的 {StaticResource} 解析存在边角问题，
        /// 会导致部分页面的 ItemTemplate 为 null，ListBox 回退显示 data context 的 ToString（类名）。
        /// 这里用 DP 变更回调在代码后端直接给 ListBox.ItemTemplate 赋值，最稳。</summary>
        public static readonly DependencyProperty LeftListItemTemplateProperty =
            DependencyProperty.Register(nameof(LeftListItemTemplate), typeof(DataTemplate), typeof(EditorPage),
                new PropertyMetadata(null, OnLeftListItemTemplateChanged));

        public DataTemplate? LeftListItemTemplate
        {
            get => (DataTemplate?)GetValue(LeftListItemTemplateProperty);
            set => SetValue(LeftListItemTemplateProperty, value);
        }

        private static void OnLeftListItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not EditorPage ep) return;
            // 用 Loaded 时机保证 ListBox 模板已实例化（XAML 已 InitializeComponent 完成）
            if (ep.IsLoaded)
                ep.ApplyLeftListItemTemplate();
            else
                ep.Loaded += (_, _) => ep.ApplyLeftListItemTemplate();
        }

        /// <summary>把当前 LeftListItemTemplate 写到 ListBox.ItemTemplate（null 时回退默认模板）。</summary>
        private void ApplyLeftListItemTemplate()
        {
            var lb = this.FindName("ListBox") as System.Windows.Controls.ListBox
                  ?? (this.Content as System.Windows.FrameworkElement)?.FindName("ListBox") as System.Windows.Controls.ListBox;
            if (lb == null) return;
            lb.ItemTemplate = LeftListItemTemplate
                ?? (this.Resources["EditorListItemTemplate"] as DataTemplate);
        }

        public EditorPage()
        {
            InitializeComponent();
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
