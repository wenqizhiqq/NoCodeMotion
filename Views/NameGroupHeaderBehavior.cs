// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨
// ◆温​启⁣志⁣◆‌编⁣写‏◇‎微⁠信‍﹕‍1⁣8‏7⁣◆​1⁠9‌3‎6⁣◇⁠1‌3‍9‍9​　‍※​保‍留⁣所‍有‎权‏利‌请‏勿⁣删‏除‌◇
namespace NoCodeMotion.Behaviors
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Threading;

    /// <summary>
    /// 给下拉框挂上「二级菜单」的分组头样式（分类名 + 该类下的名称数量）。
    /// 分组数据由 <see cref="NoCodeMotion.Services.Catalog"/> 按名称前缀挂到名称库的默认视图上，
    /// 这里只负责「长什么样」——没有分组时不会渲染任何东西，挂上也无副作用。
    /// </summary>
    /// <remarks>
    /// 覆盖方式两层，缺一不可：
    /// ① **全局类处理器**（`[ModuleInitializer]` 里 RegisterClassHandler）：应用内每个 ComboBox 一加载就自动挂上，
    ///    与它用不用 Style、用显式 Style 还是内联 Style 无关 —— 这才是「所有页面都有」的保证。
    ///    页面里三种写法混用，靠逐个 Style / 逐个下拉去开必漏（气缸页、流程名称列都漏过）。
    /// ② **附加属性 Enable**：给需要显式表达意图的 Style 或单个下拉用（CellComboStyle 已开）。
    ///    两层都会先查 GroupStyle.Count，重复挂不会发生。
    /// 为什么不能直接在 Style 里写 &lt;Setter Property="GroupStyle"&gt;：
    /// ItemsControl.GroupStyle 是**只读 CLR 集合、不是依赖属性**，而 Style 的 Setter 只能设依赖属性
    /// —— 直接写 GroupStyle 编译不过。附加属性是依赖属性，可以写进 Setter；
    /// 回调里再往那个只读集合 Add 一项（只读指的是引用不能换，集合本身可增删）。
    /// </remarks>
    public static class NameGroupHeader
    {
        /// <summary>分组头模板在 AppStyles.xaml 里的资源键。</summary>
        public const string HeaderTemplateKey = "NameGroupHeaderTemplate";

        /// <summary>一级分类行（含右侧飞出的二级菜单）样式在 AppStyles.xaml 里的资源键。</summary>
        public const string ContainerStyleKey = "NameGroupCascadeContainerStyle";

        /// <summary>
        /// 模块加载时就把「所有 ComboBox 自动挂分组头」注册好。
        /// ★ 用 ModuleInitializer 而不是静态构造函数：静态构造只在**第一次访问本类**时才跑，
        ///   一旦哪天没人再引用附加属性（例如有人删掉 CellComboStyle 里的 Setter），
        ///   全局兜底会静默失效 —— 「少一行就悄悄不生效」正是本项目反复踩的坑。
        /// </summary>
        [System.Runtime.CompilerServices.ModuleInitializer]
        internal static void RegisterGlobalHook()
        {
            // 任何 ComboBox 一加载就挂上分组头模板，无需逐页 / 逐个 Style 去开。
            // 未分组的视图不会生成 GroupItem，所以对绝大多数下拉完全无副作用。
            EventManager.RegisterClassHandler(
                typeof(ComboBox),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnComboBoxLoaded));
        }

        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable", typeof(bool), typeof(NameGroupHeader),
                new PropertyMetadata(false, OnEnableChanged));

        public static bool GetEnable(DependencyObject o) => (bool)o.GetValue(EnableProperty);
        public static void SetEnable(DependencyObject o, bool v) => o.SetValue(EnableProperty, v);

        private static void OnComboBoxLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox combo) EnsureHeader(combo);
        }

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ComboBox combo || e.NewValue is not true) return;
            EnsureHeader(combo);
        }

        /// <summary>挂上分组头模板；已挂过、或模板资源尚未就绪时什么都不做。</summary>
        private static void EnsureHeader(ComboBox combo)
        {
            if (combo.GroupStyle.Count > 0) return;      // 已挂过就别重复（类处理器 + 附加属性都可能触发）
            var tpl = Application.Current?.TryFindResource(HeaderTemplateKey) as DataTemplate;
            if (tpl is null) return;
            var gs = new GroupStyle { HeaderTemplate = tpl };
            // 一级分类行 + 右侧飞出的二级菜单（取不到就退化成「标题 + 平铺」，功能不受影响）。
            // ★ ContainerStyle 只在**真的分了组**时才被用到（没有分组就不会生成 GroupItem），
            //   所以对未分组的下拉依旧零副作用。
            if (Application.Current?.TryFindResource(ContainerStyleKey) is Style container)
                gs.ContainerStyle = container;
            combo.GroupStyle.Add(gs);
        }
    }

    /// <summary>
    /// 让分组下拉的一级分类行支持「悬停即展开、点击切换开/关」——二级菜单在**右侧飞出**
    /// （就是右键菜单那种级联子菜单）。
    /// 挂在 <see cref="GroupItem"/> 上（由 <c>NameGroupCascadeContainerStyle</c> 里的附加属性开启），
    /// 运行时在它的模板里找两个部件：<c>row</c>（ToggleButton，一级行）与 <c>flyout</c>（Popup，二级菜单）。
    /// </summary>
    /// <remarks>
    /// 为什么必须写代码、不能纯 XAML 触发器：
    /// 二级菜单是**独立的 Popup**（自己的顶层窗口），鼠标一移进去，一级行的 <c>IsMouseOver</c> 立刻变 false ——
    /// 若用 <c>&lt;Trigger Property="IsMouseOver"&gt;</c> 去收起，鼠标刚够到二级菜单它就消失了。
    /// 所以只能轮询「一级行 + 二级菜单」两处是否都无鼠标，再决定收起。
    /// 另外「悬停展开」与「点击切换」会互相打架（点击收起后，悬停判定会立刻把它弹开），
    /// 所以点收起后要临时抑制悬停，等鼠标离开一级行再解除。
    /// </remarks>
    public static class NameGroupFlyout
    {
        /// <summary>一级分类行部件名（ToggleButton）。</summary>
        public const string RowPartName = "row";

        /// <summary>二级菜单部件名（Popup）。</summary>
        public const string FlyoutPartName = "flyout";

        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable", typeof(bool), typeof(NameGroupFlyout),
                new PropertyMetadata(false, OnEnableChanged));

        public static bool GetEnable(DependencyObject o) => (bool)o.GetValue(EnableProperty);
        public static void SetEnable(DependencyObject o, bool v) => o.SetValue(EnableProperty, v);

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GroupItem gi || e.NewValue is not true) return;
            gi.Loaded -= OnLoaded;
            gi.Loaded += OnLoaded;
            if (gi.IsLoaded) OnLoaded(gi, new RoutedEventArgs());
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            var gi = (GroupItem)sender;
            gi.Loaded -= OnLoaded;
            if (gi.Template?.FindName(RowPartName, gi) is not ToggleButton row) return;
            if (gi.Template?.FindName(FlyoutPartName, gi) is not Popup flyout) return;

            bool suppressHover = false;   // 刚被点着收起时，别让悬停判定立刻又弹开
            var timer = new DispatcherTimer(DispatcherPriority.Input)
            {
                Interval = TimeSpan.FromMilliseconds(200),
            };

            // 只在需要判定时才让定时器跑，判完就停，避免常驻唤醒。
            void Start() { if (!timer.IsEnabled) timer.Start(); }

            void Reconcile()
            {
                bool overRow = row.IsMouseOver;
                bool overFly = flyout.Child is UIElement c && c.IsMouseOver;
                if (overRow)
                {
                    if (!suppressHover) row.IsChecked = true;     // 悬停即展开
                }
                else if (!overFly)
                {
                    row.IsChecked = false;                       // 两处都没鼠标了才收起
                    timer.Stop();
                }
            }

            void CloseFlyout()
            {
                row.IsChecked = false;
                timer.Stop();
            }

            timer.Tick += (_, _) => Reconcile();
            row.MouseEnter += (_, _) => Start();
            row.MouseLeave += (_, _) => { suppressHover = false; Start(); };
            // ToggleButton 先更新 IsChecked 再触发 Click，所以这里读到的是「点击之后」的状态
            row.Click += (_, _) => { suppressHover = row.IsChecked != true; Start(); };

            flyout.Opened += (_, _) =>
            {
                if (flyout.Child is UIElement c)
                {
                    c.MouseLeave -= OnFlyChildLeave;
                    c.MouseLeave += OnFlyChildLeave;
                }
                Start();
            };
            flyout.Closed += (_, _) => { if (!row.IsMouseOver) timer.Stop(); };

            // 外层下拉一关，一级行会被卸载 —— 顺手把二级菜单也收掉，
            // 否则它会作为独立顶层窗口留在屏幕上（Popup 不会跟着父窗口一起消失）。
            gi.Unloaded += (_, _) => CloseFlyout();

            // 选中某个名称后，保证外层下拉会关掉（关不掉的话二级菜单也会悬在半空）
            if (ItemsControl.ItemsControlFromItemContainer(gi) is ComboBox combo)
            {
                combo.SelectionChanged += (_, _) =>
                {
                    CloseFlyout();
                    if (combo.IsDropDownOpen) combo.IsDropDownOpen = false;
                };
            }

            void OnFlyChildLeave(object s, MouseEventArgs a) => Start();
        }
    }
}
// ◇作者保留所有权利　请勿删除※
