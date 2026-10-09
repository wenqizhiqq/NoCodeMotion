// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨
// ◆温​启⁣志⁣◆‌编⁣写‏◇‎微⁠信‍﹕‍1⁣8‏7⁣◆​1⁠9‌3‎6⁣◇⁠1‌3‍9‍9​　‍※​保‍留⁣所‍有‎权‏利‌请‏勿⁣删‏除‌◇
using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NoCodeMotion.Behaviors
{
    /// <summary>
    /// 预览图的「滚轮缩放 + 中键拖拽平移 + 双击复位」附加行为。
    ///
    /// ★ 挂到**容器**上（而不是容器里的 Image）时，缩放/平移作用于整个容器 ——
    ///   容器内的叠加层（ROI 框、匹配结果框、Canvas 标注）会跟着图像一起变换，
    ///   永远保持对齐。视觉流程页的匹配框是按 Image.Stretch=Uniform 的映射预先算成
    ///   屏幕坐标的，只变换 Image 会让这些框与图错位。
    /// ★ 容器的本地坐标空间不变（RenderTransform 不影响布局，也不影响 GetPosition）——
    ///   所以业务代码里的 e.GetPosition(容器) 仍拿到「原图映射后的本地坐标」，
    ///   框选反算像素的公式一行都不用改。
    /// </summary>
    public static class ZoomPanBehavior
    {
        private const double WheelStep = 1.15;
        private const double Eps = 1e-9;

        private sealed class DragState
        {
            public Point LastMouse;
            public Cursor? OldCursor;
        }

        // 拖拽态挂在元素上（ConditionalWeakTable）→ 不给元素加字段，元素回收时自动清掉
        private static readonly ConditionalWeakTable<FrameworkElement, DragState> Drag = new();

        // 缓存委托实例：Detach 时 RemoveHandler 必须拿到同一个委托
        private static readonly MouseButtonEventHandler DownHandler = OnDown;
        private static readonly MouseEventHandler MoveHandler = OnMove;
        private static readonly MouseButtonEventHandler UpHandler = OnUp;
        private static readonly MouseWheelEventHandler WheelHandler = OnWheel;

        // ===================== 附加属性 =====================

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(ZoomPanBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject d) => (bool)d.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject d, bool value) => d.SetValue(IsEnabledProperty, value);

        /// <summary>最小缩放（1 = 容器适配时的原始比例）。</summary>
        public static readonly DependencyProperty MinScaleProperty =
            DependencyProperty.RegisterAttached("MinScale", typeof(double), typeof(ZoomPanBehavior),
                new PropertyMetadata(0.25));

        public static double GetMinScale(DependencyObject d) => (double)d.GetValue(MinScaleProperty);
        public static void SetMinScale(DependencyObject d, double value) => d.SetValue(MinScaleProperty, value);

        /// <summary>最大缩放。</summary>
        public static readonly DependencyProperty MaxScaleProperty =
            DependencyProperty.RegisterAttached("MaxScale", typeof(double), typeof(ZoomPanBehavior),
                new PropertyMetadata(16.0));

        public static double GetMaxScale(DependencyObject d) => (double)d.GetValue(MaxScaleProperty);
        public static void SetMaxScale(DependencyObject d, double value) => d.SetValue(MaxScaleProperty, value);

        /// <summary>当前缩放（供绑定/测试读取）。</summary>
        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.RegisterAttached("Scale", typeof(double), typeof(ZoomPanBehavior),
                new PropertyMetadata(1.0));

        public static double GetScale(DependencyObject d) => (double)d.GetValue(ScaleProperty);

        /// <summary>当前横向平移量（父空间像素）。</summary>
        public static readonly DependencyProperty OffsetXProperty =
            DependencyProperty.RegisterAttached("OffsetX", typeof(double), typeof(ZoomPanBehavior),
                new PropertyMetadata(0.0));

        public static double GetOffsetX(DependencyObject d) => (double)d.GetValue(OffsetXProperty);

        /// <summary>当前纵向平移量（父空间像素）。</summary>
        public static readonly DependencyProperty OffsetYProperty =
            DependencyProperty.RegisterAttached("OffsetY", typeof(double), typeof(ZoomPanBehavior),
                new PropertyMetadata(0.0));

        public static double GetOffsetY(DependencyObject d) => (double)d.GetValue(OffsetYProperty);

        private static void SetScale(DependencyObject d, double v) => d.SetValue(ScaleProperty, v);
        private static void SetOffsetX(DependencyObject d, double v) => d.SetValue(OffsetXProperty, v);
        private static void SetOffsetY(DependencyObject d, double v) => d.SetValue(OffsetYProperty, v);

        // ===================== 纯数学（可单测） =====================

        /// <summary>
        /// 光标锚定缩放：让 (mouseX,mouseY) 底下的那个内容点在缩放前后停在**同一屏幕位置**。
        /// 屏幕映射 screen = scale * p + offset（p 为容器本地坐标，offset 在父空间），
        /// 反解得 offset' = mouse * (1 - s) + offset * s，其中 s = newScale / oldScale。
        /// ★ mouse 与 offset 必须同一坐标系（都用父空间）。
        /// </summary>
        public static (double X, double Y) ZoomOffset(
            double mouseX, double mouseY, double oldScale, double newScale, double offsetX, double offsetY)
        {
            if (oldScale <= Eps) oldScale = 1.0;
            double s = newScale / oldScale;
            return (mouseX * (1 - s) + offsetX * s, mouseY * (1 - s) + offsetY * s);
        }

        /// <summary>平移 1:1 跟手：新偏移 = 起始偏移 + 鼠标位移（都在父空间量）。</summary>
        public static (double X, double Y) PanOffset(
            double startOffsetX, double startOffsetY, double deltaX, double deltaY)
            => (startOffsetX + deltaX, startOffsetY + deltaY);

        // ===================== 编程接口（冒烟测试也走这里） =====================

        /// <summary>复位：缩放回 1、平移回 0。</summary>
        public static void Reset(FrameworkElement el)
        {
            EnsureTransform(el);
            SetScale(el, 1.0);
            SetOffsetX(el, 0.0);
            SetOffsetY(el, 0.0);
            ApplyTransform(el);
        }

        /// <summary>以父空间坐标 <paramref name="mouse"/> 为锚点缩放一次（factor &gt; 1 放大）。</summary>
        public static void ZoomAtPoint(FrameworkElement el, Point mouse, double factor)
        {
            EnsureTransform(el);
            double old = GetScale(el);
            double next = Math.Clamp(old * factor, GetMinScale(el), GetMaxScale(el));
            if (Math.Abs(next - old) < Eps) return;
            var (ox, oy) = ZoomOffset(mouse.X, mouse.Y, old, next, GetOffsetX(el), GetOffsetY(el));
            SetScale(el, next);
            SetOffsetX(el, ox);
            SetOffsetY(el, oy);
            ApplyTransform(el);
        }

        /// <summary>按父空间像素平移。</summary>
        public static void PanBy(FrameworkElement el, double dx, double dy)
        {
            EnsureTransform(el);
            var (ox, oy) = PanOffset(GetOffsetX(el), GetOffsetY(el), dx, dy);
            SetOffsetX(el, ox);
            SetOffsetY(el, oy);
            ApplyTransform(el);
        }

        // ===================== 事件 =====================

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement el) return;
            if (e.NewValue is true) Attach(el);
            else Detach(el);
        }

        private static void Attach(FrameworkElement el)
        {
            EnsureTransform(el);
            ApplyTransform(el);
            el.MouseWheel += WheelHandler;
            el.MouseMove += MoveHandler;
            el.MouseUp += UpHandler;
            // handledEventsToo: true —— 页面自己的 MouseDown 处理器可能先跑并吃掉事件，
            // 这里必须仍然收到（左键双击复位就依赖这一点）。
            el.AddHandler(UIElement.MouseDownEvent, DownHandler, handledEventsToo: true);
        }

        private static void Detach(FrameworkElement el)
        {
            el.MouseWheel -= WheelHandler;
            el.MouseMove -= MoveHandler;
            el.MouseUp -= UpHandler;
            el.RemoveHandler(UIElement.MouseDownEvent, DownHandler);
        }

        /// <summary>
        /// 屏幕映射 = ScaleTransform(scale) 之后接 TranslateTransform(offset)，原点取左上角。
        /// 顺序不能反：先缩放再平移，offset 才是「父空间像素」的直观语义。
        /// </summary>
        private static void EnsureTransform(FrameworkElement el)
        {
            if (el.RenderTransform is TransformGroup g && g.Children.Count >= 2
                && g.Children[0] is ScaleTransform && g.Children[1] is TranslateTransform)
                return;
            el.RenderTransformOrigin = new Point(0, 0);
            var tg = new TransformGroup();
            tg.Children.Add(new ScaleTransform(1, 1));
            tg.Children.Add(new TranslateTransform(0, 0));
            el.RenderTransform = tg;
        }

        private static void ApplyTransform(FrameworkElement el)
        {
            if (el.RenderTransform is not TransformGroup tg || tg.Children.Count < 2) return;
            if (tg.Children[0] is ScaleTransform sc)
            {
                sc.ScaleX = GetScale(el);
                sc.ScaleY = GetScale(el);
            }
            if (tg.Children[1] is TranslateTransform tr)
            {
                tr.X = GetOffsetX(el);
                tr.Y = GetOffsetY(el);
            }
        }

        /// <summary>
        /// ★ 必须用「不受 el 自身 RenderTransform 影响」的坐标系来量鼠标。
        /// e.GetPosition(el) 返回的是 el **自身变换前**的本地坐标；而平移偏移量表达在父空间里，
        /// 把本地位移喂给 offset 会让图像以 1/scale 的速度跟手（放大后拖不动、缩小后拖飞）。
        /// </summary>
        private static Point GetStablePos(FrameworkElement el, MouseEventArgs e)
        {
            var relative = el.Parent as IInputElement ?? el;
            return e.GetPosition(relative);
        }

        private static void OnWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not FrameworkElement el) return;
            ZoomAtPoint(el, GetStablePos(el, e), e.Delta > 0 ? WheelStep : 1.0 / WheelStep);
            e.Handled = true;
        }

        private static void OnDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement el) return;

            // 左键双击 = 复位（Image / FrameworkElement 没有 MouseDoubleClick，只能在按下时看 ClickCount）
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                Reset(el);
                e.Handled = true;
                return;
            }

            // 中键拖拽 = 平移（左键留给业务方的框选）
            if (e.ChangedButton != MouseButton.Middle) return;

            var st = Drag.GetOrCreateValue(el);
            st.LastMouse = GetStablePos(el, e);
            st.OldCursor = el.Cursor;
            el.Cursor = Cursors.SizeAll;
            el.CaptureMouse();
            e.Handled = true;
        }

        private static void OnMove(object sender, MouseEventArgs e)
        {
            if (sender is not FrameworkElement el) return;
            if (!Drag.TryGetValue(el, out var st)) return;
            if (e.MiddleButton != MouseButtonState.Pressed) { EndDrag(el); return; }

            var cur = GetStablePos(el, e);
            PanBy(el, cur.X - st.LastMouse.X, cur.Y - st.LastMouse.Y);
            st.LastMouse = cur;
            e.Handled = true;
        }

        private static void OnUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement el) return;
            if (e.ChangedButton != MouseButton.Middle) return;
            if (!Drag.TryGetValue(el, out _)) return;
            EndDrag(el);
            e.Handled = true;
        }

        private static void EndDrag(FrameworkElement el)
        {
            if (Drag.TryGetValue(el, out var st))
            {
                // 还原拖拽前的光标；原来是「未设置」就清掉本地值，避免留下 SizeAll
                if (st.OldCursor != null) el.Cursor = st.OldCursor;
                else el.ClearValue(FrameworkElement.CursorProperty);
                Drag.Remove(el);
            }
            if (el.IsMouseCaptured) el.ReleaseMouseCapture();
        }
    }
}
// ◇作者保留所有权利　请勿删除※
