using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 通用编辑页框架：顶部“添加/删除”工具栏 + 左侧项目列表 + 右侧详情区。
    /// 每个业务页（轴/IO/气缸…）只需把具体表单放进 Detail 属性即可复用整套布局与增删逻辑。
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

        public EditorPage()
        {
            InitializeComponent();
        }
    }
}
