using System.Windows;
using System.Windows.Controls;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 通用「字段」容器：在任意控件下方自动渲染一行灰色小字说明（Hint）。
    /// 用法：&lt;local:Field Hint="说明文字"&gt; &lt;TextBox .../&gt; &lt;/local:Field&gt;
    /// 可选 Label 作为控件上方的简短标题。
    /// </summary>
    public class Field : ContentControl
    {
        static Field()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Field),
                new FrameworkPropertyMetadata(typeof(Field)));
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(Field),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty HintProperty =
            DependencyProperty.Register(nameof(Hint), typeof(string), typeof(Field),
                new PropertyMetadata(string.Empty));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string Hint
        {
            get => (string)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }
    }
}
