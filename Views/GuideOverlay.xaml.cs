// 新手引导气泡（coach-mark）覆盖层：暗化 + 高亮框 + 引导卡片。
// 暗化层与高亮框仅作视觉、点击穿透，气泡卡片保持可交互。
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NoCodeMotion.Views
{
    public partial class GuideOverlay : UserControl
    {
        private Action? _onNext;
        private Action? _onSkip;

        public GuideOverlay()
        {
            InitializeComponent();
        }

        /// <summary>在 target 矩形附近显示引导气泡。</summary>
        /// <param name="target">目标控件在覆盖层坐标中的位置（由调用方用 TranslatePoint 计算）。</param>
        /// <param name="overlayW/overlayH">覆盖层（即主窗口客户区）尺寸，用于把气泡夹在可视范围内。</param>
        public void Show(Rect target, string title, string text, bool isLast,
                         double overlayW, double overlayH, Action onNext, Action onSkip)
        {
            TitleTb.Text = title;
            TextTb.Text = text;
            NextBtn.Content = isLast ? "完成" : "下一步";
            _onNext = onNext;
            _onSkip = onSkip;
            Bubble.Visibility = Visibility.Visible;
            Bubble.UpdateLayout();
            Position(target, overlayW, overlayH, Bubble.ActualWidth, Bubble.ActualHeight);
        }

        /// <summary>窗口尺寸变化时，按当前目标矩形重新摆放高亮框与气泡。</summary>
        public void Reposition(Rect target, double overlayW, double overlayH)
        {
            if (Bubble.Visibility != Visibility.Visible) return;
            Bubble.UpdateLayout();
            Position(target, overlayW, overlayH, Bubble.ActualWidth, Bubble.ActualHeight);
        }

        private void Position(Rect t, double overlayW, double overlayH, double bw, double bh)
        {
            double pad = 6;
            var r = new Rect(t.X - pad, t.Y - pad, t.Width + pad * 2, t.Height + pad * 2);
            Frame.Visibility = Visibility.Visible;
            Frame.Width = r.Width;
            Frame.Height = r.Height;
            Frame.Margin = new Thickness(r.X, r.Y, 0, 0);

            // 气泡优先放在目标下方，放不下则翻到上方；最后夹在窗口内。
            double x = r.X + r.Width / 2 - bw / 2;
            double y = r.Y + r.Height + 12;
            if (y + bh > overlayH && r.Y - 12 - bh > 0)
                y = r.Y - 12 - bh;
            x = Math.Max(10, Math.Min(x, overlayW - bw - 10));
            y = Math.Max(10, Math.Min(y, overlayH - bh - 10));
            Bubble.Margin = new Thickness(x, y, 0, 0);
        }

        private void Next_Click(object sender, RoutedEventArgs e) => _onNext?.Invoke();
        private void Skip_Click(object sender, RoutedEventArgs e) => _onSkip?.Invoke();
    }
}
