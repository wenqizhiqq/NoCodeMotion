#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace NoCodeMotion.Editing
{
    /// <summary>
    /// 每行耗时边栏：在编辑器最左侧绘制每一行的累计执行耗时（毫秒），如 "12.3ms"。
    /// 仅显示已测量过的行；耗时较高的行用红色高亮，便于定位慢语句。
    /// </summary>
    public sealed class LineTimeMargin : AbstractMargin
    {
        private static readonly Brush BackBrush = new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2));
        private static readonly Brush TimeBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x8A, 0x8E));
        private static readonly Brush HotBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B));
        private static readonly Typeface TimeTypeface =
            new Typeface(new FontFamily("Consolas, Cascadia Mono, Courier New"),
                FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        // 单行耗时超过该阈值（毫秒）用红色标出
        private const double HotMs = 30;

        private readonly Dictionary<int, double> _times = new Dictionary<int, double>();

        static LineTimeMargin()
        {
            BackBrush.Freeze();
            TimeBrush.Freeze();
            HotBrush.Freeze();
        }

        public LineTimeMargin()
        {
            Width = 56;
        }

        /// <summary>用快照刷新各行的耗时。传 null 清空。</summary>
        public void SetLineTimes(IReadOnlyDictionary<int, double> times)
        {
            _times.Clear();
            if (times != null)
            {
                foreach (var kv in times)
                    _times[kv.Key] = kv.Value;
            }
            InvalidateVisual();
        }

        public void Clear() => SetLineTimes(null);

        protected override Size MeasureOverride(Size availableSize) => new Size(Width, 0);

        protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
        {
            if (oldTextView != null) oldTextView.VisualLinesChanged -= OnVisualLinesChanged;
            base.OnTextViewChanged(oldTextView, newTextView);
            if (newTextView != null) newTextView.VisualLinesChanged += OnVisualLinesChanged;
            InvalidateVisual();
        }

        private void OnVisualLinesChanged(object sender, EventArgs e) => InvalidateVisual();

        protected override void OnRender(DrawingContext dc)
        {
            var textView = TextView;
            var size = RenderSize;

            dc.DrawRectangle(BackBrush, null, new Rect(0, 0, size.Width, size.Height));
            if (textView == null || !textView.VisualLinesValid) return;

            foreach (VisualLine line in textView.VisualLines)
            {
                int number = line.FirstDocumentLine.LineNumber;
                if (!_times.TryGetValue(number, out double ms)) continue;

                string text = ms >= 1 ? ms.ToString("F1", CultureInfo.InvariantCulture)
                                       : ms.ToString("F2", CultureInfo.InvariantCulture);
                text += "ms";

                var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    TimeTypeface, 10.5, ms >= HotMs ? HotBrush : TimeBrush, 1.0);

                double top = line.VisualTop - textView.VerticalOffset;
                double y = top + (line.Height - ft.Height) / 2; // 垂直居中
                double x = size.Width - ft.Width - 4;            // 右对齐，留 4px 边距
                dc.DrawText(ft, new Point(x, y));
            }
        }
    }
}
