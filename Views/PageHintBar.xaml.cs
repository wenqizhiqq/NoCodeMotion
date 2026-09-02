// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
using System.Windows;
using System.Windows.Controls;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 页面底部「操作说明 + 注意事项」提示栏。每个页面底部放一个，
    /// 通过 OperationText / PrecautionText 设置本页的操作方法与注意事项文字。
    /// 设计为贴底、浅色、可换行，不随页面内容滚动。
    /// </summary>
    public partial class PageHintBar : UserControl
    {
        public static readonly DependencyProperty OperationTextProperty =
            DependencyProperty.Register(nameof(OperationText), typeof(string), typeof(PageHintBar),
                new PropertyMetadata(""));

        public string OperationText
        {
            get => (string)GetValue(OperationTextProperty);
            set => SetValue(OperationTextProperty, value);
        }

        public static readonly DependencyProperty PrecautionTextProperty =
            DependencyProperty.Register(nameof(PrecautionText), typeof(string), typeof(PageHintBar),
                new PropertyMetadata(""));

        public string PrecautionText
        {
            get => (string)GetValue(PrecautionTextProperty);
            set => SetValue(PrecautionTextProperty, value);
        }

        public PageHintBar()
        {
            InitializeComponent();
        }
    }
}
// ◇作者保留所有权利　请勿删除※
