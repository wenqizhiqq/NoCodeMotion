// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NoCodeMotion.Views.Controls
{
    /// <summary>
    /// 数字输入 + Slider 组合控件。
    /// TextBox 给精确值（任意类型，依赖 WPF 自动类型转换），Slider 给快速拖动调整。
    /// 两者共享同一个 Value（string，TwoWay 绑定到源属性），内部用 SliderValue（double）作为中介。
    /// 文本非数字（空、字母）时滑块保持上一有效位置；滑块范围由 Min/Max 设定，默认 0-100。
    /// </summary>
    public partial class NumSliderBox : UserControl
    {
        // 防止 Value <-> SliderValue 互相同步时无限递归
        private bool _syncing;

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value), typeof(string), typeof(NumSliderBox),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValueChanged));

        public static readonly DependencyProperty MinProperty =
            DependencyProperty.Register(
                nameof(Min), typeof(double), typeof(NumSliderBox),
                new PropertyMetadata(0.0, OnMinMaxChanged));

        public static readonly DependencyProperty MaxProperty =
            DependencyProperty.Register(
                nameof(Max), typeof(double), typeof(NumSliderBox),
                new PropertyMetadata(100.0, OnMinMaxChanged));

        public static readonly DependencyProperty SliderValueProperty =
            DependencyProperty.Register(
                nameof(SliderValue), typeof(double), typeof(NumSliderBox),
                new PropertyMetadata(0.0, OnSliderValueChanged));

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>Slider 下限，默认 0。</summary>
        public double Min
        {
            get => (double)GetValue(MinProperty);
            set => SetValue(MinProperty, value);
        }

        /// <summary>Slider 上限，默认 100。</summary>
        public double Max
        {
            get => (double)GetValue(MaxProperty);
            set => SetValue(MaxProperty, value);
        }

        /// <summary>内部 Slider 中介值（double）。外部不要直接绑定。</summary>
        public double SliderValue
        {
            get => (double)GetValue(SliderValueProperty);
            set => SetValue(SliderValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (NumSliderBox)d;
            if (ctrl._syncing) return;

            // 文本 -> 滑块同步：能解析为数字才同步
            if (double.TryParse(ctrl.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
            {
                v = Math.Max(ctrl.Min, Math.Min(ctrl.Max, v));
                if (Math.Abs(ctrl.SliderValue - v) > 0.0001)
                {
                    ctrl._syncing = true;
                    ctrl.SliderValue = v;
                    ctrl._syncing = false;
                }
            }
        }

        private static void OnSliderValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (NumSliderBox)d;
            if (ctrl._syncing) return;

            // 滑块 -> 文本同步：保留两位有效小数，去掉无意义的 ".0"
            string formatted = ((double)e.NewValue).ToString("0.##", CultureInfo.InvariantCulture);
            if (!string.Equals(ctrl.Value, formatted, StringComparison.Ordinal))
            {
                ctrl._syncing = true;
                ctrl.Value = formatted;
                ctrl._syncing = false;
            }
        }

        private static void OnMinMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (NumSliderBox)d;
            // 范围改了，重新钳制一次 SliderValue
            double clamped = Math.Max(ctrl.Min, Math.Min(ctrl.Max, ctrl.SliderValue));
            if (Math.Abs(clamped - ctrl.SliderValue) > 0.0001)
            {
                ctrl._syncing = true;
                ctrl.SliderValue = clamped;
                ctrl._syncing = false;
            }
        }

        /// <summary>输入框只允许数字 / 小数点 / 负号 / 退格（其它键直接拦截）。</summary>
        private void NumTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // '-' 只允许出现在开头
            var tb = (TextBox)sender;
            string next = tb.Text.Substring(0, tb.SelectionStart) + e.Text;
            // 简单合法性：必须能 parse 成 double；允许空串（中间状态）
            if (string.IsNullOrEmpty(next)) { e.Handled = false; return; }
            // 数字字符或负号开头
            bool ok = true;
            foreach (char c in next)
            {
                if (!(char.IsDigit(c) || c == '.' || c == '-')) { ok = false; break; }
            }
            if (!ok) { e.Handled = true; return; }
            // 小数点最多一个
            if (next.IndexOf('.') != next.LastIndexOf('.')) { e.Handled = true; return; }
            // '-' 只允许开头
            if (next.IndexOf('-') > 0) { e.Handled = true; return; }
            // 双解析测试（拦截 "-."、"1.2.3" 等残值）
            if (!double.TryParse(next, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                e.Handled = true;
        }

        /// <summary>失焦时若是中间状态（比如 "-")，自动补成 Min 兜底。</summary>
        private void NumTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(tb.Text))
            {
                _syncing = true;
                Value = Min.ToString("0.##", CultureInfo.InvariantCulture);
                _syncing = false;
            }
            else if (!double.TryParse(tb.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                _syncing = true;
                Value = Min.ToString("0.##", CultureInfo.InvariantCulture);
                _syncing = false;
            }
        }
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
