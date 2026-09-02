// ◆◇※▣⁣
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇⁣
// ◆◇※⁣
// 右侧二维料盘：WriteableBitmap + unsafe 指针直接绘制。
//   · 一次性分配 WriteableBitmap（大小 = cols/rows × CellSize），所有更新写入同一 buffer，
//     不再每次 new RenderTargetBitmap（避免 GC 压力 + 多次大对象堆累积）。
//   · 像素直接内存写入，无 DrawingContext 抽象开销，80×80=6400 格重绘 <5ms。
//   · 点击按 e.GetPosition 算 col=x/CellSize, row=y/CellSize，切换 Occupied → 重绘。
//   · 数量多时 ScrollViewer 自动出滚动条；只画背景 + 边框，不画 R/C 文字（节省 ~1MB 文字位图缓存）。
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NoCodeMotion.Models;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    public partial class TrayPage : UserControl
    {
        // 单元格固定像素尺寸（不做拖拽缩放；数量多时 ScrollViewer 出滚动条）
        private const int CellSize = 40;

        private Image? _trayImage;
        private TrayViewModel? _vm;
        private TrayItem? _subscribedItem;
        private WriteableBitmap? _bitmap;        // 复用；尺寸变化才重新分配
        private int _bmpW, _bmpH;

        public TrayPage()
        {
            InitializeComponent();
            DataContext = new TrayViewModel();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // EditorPage.Detail 内部 NameScope 被 ContentControl 隔离，x:Name 字段为 null；
            // 运行时从可视化树找 Image（详见 MEMORY.md「宿主页 DP Content 子树 x:Name 隔离」）。
            _trayImage = FindVisualChild<Image>(this);
            if (DataContext is TrayViewModel vm)
            {
                _vm = vm;
                _vm.PropertyChanged += OnVmChanged;
                SubscribeItem(vm.SelectedItem);
            }
            RenderTray();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_vm != null) _vm.PropertyChanged -= OnVmChanged;
            UnsubscribeItem();
        }

        private void OnVmChanged(object? s, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TrayViewModel.SelectedItem))
                SubscribeItem(_vm?.SelectedItem);
        }

        private void SubscribeItem(TrayItem? item)
        {
            if (_subscribedItem == item) return;
            UnsubscribeItem();
            _subscribedItem = item;
            if (_subscribedItem != null)
                _subscribedItem.PropertyChanged += OnItemChanged;
            // 切换 SelectedItem 时强制重建位图（行/列数可能变化）
            _bitmap = null;
            RenderTray();
        }

        private void UnsubscribeItem()
        {
            if (_subscribedItem != null)
                _subscribedItem.PropertyChanged -= OnItemChanged;
            _subscribedItem = null;
        }

        private void OnItemChanged(object? s, PropertyChangedEventArgs e)
        {
            // Rows/Cols 变化（来自 ApplyPending）→ Cells 集合变化 → 任一 cell Occupied 变化都重绘
            if (e.PropertyName == nameof(TrayItem.Rows) ||
                e.PropertyName == nameof(TrayItem.Cols) ||
                e.PropertyName == nameof(TrayItem.Cells))
                RenderTray();
        }

        private unsafe void RenderTray()
        {
            if (_trayImage == null) return;
            var item = _vm?.SelectedItem;
            if (item == null || item.Rows <= 0 || item.Cols <= 0)
            {
                if (_bitmap != null) { _bitmap = null; _trayImage.Source = null; }
                _trayImage.Width = 0; _trayImage.Height = 0;
                return;
            }

            int rows = item.Rows, cols = item.Cols;
            int w = cols * CellSize;
            int h = rows * CellSize;

            // 尺寸变化才重建位图（避免每次 new RenderTargetBitmap 大对象堆分配）
            if (_bitmap == null || _bmpW != w || _bmpH != h)
            {
                _bitmap = new WriteableBitmap(w, h, 96, 96, PixelFormats.Pbgra32, null);
                _bmpW = w; _bmpH = h;
                _trayImage.Source = _bitmap;
            }

            uint bgEmpty = GetBgra("HoverBrush");
            uint bgOcc = GetBgra("SuccessSoftBrush");
            uint lineCol = GetBgra("LineBrush");
            uint white = 0xFFFFFFFF;

            _bitmap.Lock();
            try
            {
                byte* ptr = (byte*)_bitmap.BackBuffer;
                int stride = _bitmap.BackBufferStride;

                // 1. 清空整图为白色（透明会导致边缘有阴影）
                for (int y = 0; y < h; y++)
                {
                    uint* row = (uint*)(ptr + y * stride);
                    for (int x = 0; x < w; x++) row[x] = white;
                }

                // 2. 逐格画背景 + 4 边框（1px）；只画顶/底/左/右 4 边，邻格共享相邻边
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        var cell = item.Cells[r * cols + c];
                        int x0 = c * CellSize;
                        int y0 = r * CellSize;
                        uint bg = cell.Occupied ? bgOcc : bgEmpty;

                        // 背景填充：整格 CellSize×CellSize
                        FillSolid(ptr, stride, x0, y0, CellSize, CellSize, bg);

                        // 边框：顶 / 底 / 左 / 右 各 1px 行/列（顶/底与左右共享角点，多写 1 次无害）
                        HLine(ptr, stride, x0, y0, CellSize, lineCol);
                        HLine(ptr, stride, x0, y0 + CellSize - 1, CellSize, lineCol);
                        VLine(ptr, stride, x0, y0, CellSize, lineCol);
                        VLine(ptr, stride, x0 + CellSize - 1, y0, CellSize, lineCol);
                    }
                }
                _bitmap.AddDirtyRect(new Int32Rect(0, 0, w, h));
            }
            finally
            {
                _bitmap.Unlock();
            }
            _trayImage.Width = w;
            _trayImage.Height = h;
        }

        // 像素级 unsafe 绘制：直接把 BGRA32 颜色写入 BackBuffer
        private static unsafe void FillSolid(byte* ptr, int stride, int x, int y, int w, int h, uint bgra)
        {
            for (int yy = 0; yy < h; yy++)
            {
                uint* row = (uint*)(ptr + (y + yy) * stride + x * 4);
                for (int xx = 0; xx < w; xx++) row[xx] = bgra;
            }
        }

        private static unsafe void HLine(byte* ptr, int stride, int x, int y, int w, uint bgra)
        {
            uint* row = (uint*)(ptr + y * stride + x * 4);
            for (int xx = 0; xx < w; xx++) row[xx] = bgra;
        }

        private static unsafe void VLine(byte* ptr, int stride, int x, int y, int h, uint bgra)
        {
            for (int yy = 0; yy < h; yy++)
                *(uint*)(ptr + (y + yy) * stride + x * 4) = bgra;
        }

        private uint GetBgra(string key)
        {
            var brush = (SolidColorBrush)FindResource(key);
            var c = brush.Color;
            return (uint)((c.A << 24) | (c.R << 16) | (c.G << 8) | c.B);
        }

        private void TrayImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _vm?.SelectedItem;
            if (item == null || _trayImage == null) return;
            var pos = e.GetPosition(_trayImage);
            int c = (int)(pos.X / CellSize);
            int r = (int)(pos.Y / CellSize);
            if (r < 0 || r >= item.Rows || c < 0 || c >= item.Cols) return;
            var cell = item.Cells[r * item.Cols + c];
            cell.Occupied = !cell.Occupied;
            e.Handled = true;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var found = FindVisualChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }
    }
}
// ◇作者保留所有权利　请勿删除※⁣
// ◆◇※⁣