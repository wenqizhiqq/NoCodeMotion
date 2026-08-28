// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace NoCodeMotion.Editing
{
    /// <summary>整行背景高亮（用于当前执行行 / 出错行）。</summary>
    public sealed class LineHighlightRenderer : IBackgroundRenderer
    {
        private readonly Brush _background;
        private readonly Pen _border;

        public LineHighlightRenderer(Color background, Color border)
        {
            _background = new SolidColorBrush(background);
            _background.Freeze();
            var borderBrush = new SolidColorBrush(border);
            borderBrush.Freeze();
            _border = new Pen(borderBrush, 1);
            _border.Freeze();
        }

        public int Line { get; set; }

        public KnownLayer Layer => KnownLayer.Background;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (Line <= 0 || textView?.Document == null) return;
            if (Line > textView.Document.LineCount) return;

            textView.EnsureVisualLines();
            DocumentLine docLine = textView.Document.GetLineByNumber(Line);

            var builder = new BackgroundGeometryBuilder { AlignToWholePixels = true, CornerRadius = 2 };
            builder.AddSegment(textView, new TextSegment { StartOffset = docLine.Offset, EndOffset = docLine.EndOffset });

            Geometry geometry = builder.CreateGeometry();
            double width = textView.ActualWidth;

            if (geometry != null)
            {
                Rect b = geometry.Bounds;
                drawingContext.DrawRectangle(_background, _border,
                    new Rect(0, b.Top, width, b.Height));
            }
            else
            {
                // 空行：手动定位
                VisualLine vl = textView.GetVisualLine(Line);
                if (vl == null) return;
                double top = vl.VisualTop - textView.VerticalOffset;
                drawingContext.DrawRectangle(_background, _border, new Rect(0, top, width, vl.Height));
            }
        }
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
