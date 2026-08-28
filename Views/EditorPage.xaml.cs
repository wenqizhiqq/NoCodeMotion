// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 通用编辑页框架：顶部“添加/删除/重命名”工具栏 + 左侧项目列表 + 右侧详情区。
    /// 每个业务页（轴/IO/气缸…）只需把具体表单放进 Detail 属性即可复用整套布局与增删逻辑。
    /// 通过 LeftToolbarContent + ShowDefaultAddButton 允许宿主页（如流程页）注入
    /// 多个具体添加按钮并隐藏默认“添加”（如“添加表格 / 添加脚本”）。
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

        /// <summary>是否显示默认的“添加”按钮。宿主页在提供自定义添加按钮时应设为 false。</summary>
        public static readonly DependencyProperty ShowDefaultAddButtonProperty =
            DependencyProperty.Register(nameof(ShowDefaultAddButton), typeof(bool), typeof(EditorPage),
                new PropertyMetadata(true));

        public bool ShowDefaultAddButton
        {
            get => (bool)GetValue(ShowDefaultAddButtonProperty);
            set => SetValue(ShowDefaultAddButtonProperty, value);
        }

        /// <summary>注入到顶部工具栏“添加”按钮左侧的自定义内容（如“添加表格 / 添加脚本”）。</summary>
        public static readonly DependencyProperty LeftToolbarContentProperty =
            DependencyProperty.Register(nameof(LeftToolbarContent), typeof(UIElement), typeof(EditorPage),
                new PropertyMetadata(null));

        public UIElement? LeftToolbarContent
        {
            get => (UIElement?)GetValue(LeftToolbarContentProperty);
            set => SetValue(LeftToolbarContentProperty, value);
        }

        public EditorPage()
        {
            InitializeComponent();
        }
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
