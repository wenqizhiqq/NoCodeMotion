// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace NoCodeMotion.Views
{
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

        /// <summary>自定义左侧列表项模板。宿主页（如气缸页）想给每行加内联按钮时设置此属性；
        /// 不设置则回退到 EditorListItemTemplate（默认模板，流程页/通用页用）。</summary>
        public static readonly DependencyProperty LeftListItemTemplateProperty =
            DependencyProperty.Register(nameof(LeftListItemTemplate), typeof(DataTemplate), typeof(EditorPage),
                new PropertyMetadata(null));

        public DataTemplate? LeftListItemTemplate
        {
            get => (DataTemplate?)GetValue(LeftListItemTemplateProperty);
            set => SetValue(LeftListItemTemplateProperty, value);
        }

        public EditorPage()
        {
            InitializeComponent();
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
