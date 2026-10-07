using System;
using System.Windows;

namespace NoCodeMotion.Views
{
    /// <summary>退出进度窗口：后台释放资源时通过 Report 更新进度条与状态文字。</summary>
    public partial class ClosingProgressWindow : Window
    {
        public ClosingProgressWindow()
        {
            InitializeComponent();
        }

        /// <summary>更新进度（pct: 0~100）。从后台线程调用，内部自行封送回 UI 线程。</summary>
        public void Report(int pct, string text)
        {
            Dispatcher.Invoke(() =>
            {
                if (pct < 0) pct = 0;
                if (pct > _bar.Maximum) pct = (int)_bar.Maximum;
                _bar.Value = pct;
                _status.Text = text;
            });
        }
    }
}
