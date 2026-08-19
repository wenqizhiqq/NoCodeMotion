#nullable disable
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace NoCodeMotion.Editing
{
    /// <summary>
    /// 断点边栏：显示断点圆点与当前执行行的黄色箭头，单击可切换断点。
    /// </summary>
    public sealed class BreakpointMargin : AbstractMargin
    {
        private static readonly Brush MarginBrush = new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2));
        private static readonly Brush BreakBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0x1C, 0x23));
        private static readonly Brush BreakEdge = new SolidColorBrush(Color.FromRgb(0xB0, 0x12, 0x18));
        private static readonly Brush ArrowBrush = new SolidColorBrush(Color.FromRgb(0xF2, 0xB0, 0x1E));
        private static readonly Brush ArrowEdge = new SolidColorBrush(Color.FromRgb(0xB5, 0x7F, 0x06));
        private static readonly Brush HoverBrush = new SolidColorBrush(Color.FromArgb(0x60, 0xE5, 0x1C, 0x23));

        private int _hoverLine;

        static BreakpointMargin()
        {
            MarginBrush.Freeze();
            BreakBrush.Freeze();
            BreakEdge.Freeze();
            ArrowBrush.Freeze();
            ArrowEdge.Freeze();
            HoverBrush.Freeze();
        }

        public BreakpointMargin()
        {
            Cursor = Cursors.Hand;
            ToolTip = "单击设置 / 取消断点";
        }

        /// <summary>当前断点行号集合（1 起）。</summary>
        public HashSet<int> Breakpoints { get; } = new HashSet<int>();

        /// <summary>当前执行到的行（0 表示无）。</summary>
        public int CurrentLine { get; private set; }

        public event EventHandler BreakpointsChanged;

        public void SetCurrentLine(int line)
        {
            if (CurrentLine == line) return;
            CurrentLine = line;
            InvalidateVisual();
        }

        public bool Toggle(int line)
        {
            if (line <= 0) return false;
            bool added;
            if (Breakpoints.Contains(line))
            {
                Breakpoints.Remove(line);
                added = false;
            }
            else
            {
                Breakpoints.Add(line);
                added = true;
            }

            InvalidateVisual();
            BreakpointsChanged?.Invoke(this, EventArgs.Empty);
            return added;
        }

        public void ClearAll()
        {
            if (Breakpoints.Count == 0) return;
            Breakpoints.Clear();
            InvalidateVisual();
            BreakpointsChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override Size MeasureOverride(Size availableSize) => new Size(20, 0);

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

            dc.DrawRectangle(MarginBrush, null, new Rect(0, 0, size.Width, size.Height));

            if (textView == null || !textView.VisualLinesValid) return;

            foreach (VisualLine line in textView.VisualLines)
            {
                int number = line.FirstDocumentLine.LineNumber;
                double top = line.VisualTop - textView.VerticalOffset;
                double centerY = top + line.Height / 2;
                double cx = size.Width / 2;

                if (Breakpoints.Contains(number))
                    dc.DrawEllipse(BreakBrush, new Pen(BreakEdge, 1), new Point(cx, centerY), 6, 6);
                else if (number == _hoverLine)
                    dc.DrawEllipse(HoverBrush, null, new Point(cx, centerY), 5.5, 5.5);

                if (number == CurrentLine)
                {
                    var geo = new StreamGeometry();
                    using (var ctx = geo.Open())
                    {
                        ctx.BeginFigure(new Point(cx - 6, centerY - 5), true, true);
                        ctx.LineTo(new Point(cx + 1, centerY - 5), true, false);
                        ctx.LineTo(new Point(cx + 7, centerY), true, false);
                        ctx.LineTo(new Point(cx + 1, centerY + 5), true, false);
                        ctx.LineTo(new Point(cx - 6, centerY + 5), true, false);
                    }
                    geo.Freeze();
                    dc.DrawGeometry(ArrowBrush, new Pen(ArrowEdge, 1), geo);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int line = GetLineAt(e.GetPosition(this).Y);
            if (line != _hoverLine)
            {
                _hoverLine = line;
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverLine != 0)
            {
                _hoverLine = 0;
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            int line = GetLineAt(e.GetPosition(this).Y);
            if (line > 0)
            {
                Toggle(line);
                e.Handled = true;
            }
        }

        private int GetLineAt(double y)
        {
            var textView = TextView;
            if (textView == null || !textView.VisualLinesValid) return 0;

            VisualLine vl = textView.GetVisualLineFromVisualTop(y + textView.VerticalOffset);
            return vl?.FirstDocumentLine.LineNumber ?? 0;
        }
    }
}
