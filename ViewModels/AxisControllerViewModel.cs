// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‏启‏志‌◆‍编‎写‌◇‌微‎信⁠﹕‌1‍8‍7⁣◆⁣1‍9‍3⁠6⁣◇‌1‌3‎9​9‏　‍※‎保‌留‎所‍有‍权⁣利‎请‎勿⁠删‌除‍◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Hardware;
using NoCodeMotion.Services.Hardware.Cards;
using NoCodeMotion.Services.Hardware.Leadshine;

namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 控制器页面：增删改控制卡实例，供轴页面选择归属。
    /// <para>扩展 IO 现在挂在控制卡内部（<see cref="AxisControllerItem.ExpansionModules"/>）：
    /// 选模块型号 + 填数量即可，页面自动汇总「输入 IO 总数 / 输出 IO 总数 / 轴总数」。</para>
    /// </summary>
    public class AxisControllerViewModel : ListEditorViewModel<AxisControllerItem>, IEnsureDefaultSelection
    {
        public AxisControllerViewModel()
        {
            CatalogCategory = "Controller";
            Items = ProjectStore.Data.Controllers;
            Counter = Items.Count;
            AttachAutoSave();

            // 选中项切换时重挂总汇订阅（基类 SelectedItem 不是虚属性，靠自身 PropertyChanged 感知）。
            PropertyChanged += OnSelfPropertyChanged;
            WireSelected(SelectedItem);

            // 打开软件 / 切换工程后自动后台连接控制卡，并刷新状态栏在线指示
            ProjectManager.DataReloaded += () => AutoConnectAll();
            if (Items.Count > 0) AutoConnectAll();
            else UpdateStatusBar();
        }

        protected override AxisControllerItem CreateNewItem() => new AxisControllerItem { Kind = "控制卡", Name = $"控制卡{Counter + 1}" };

        /// <summary>添加一张控制卡（品牌 / 总线类型 / 卡型号都在详情里用下拉框选择）。</summary>
        public ICommand AddCardCommand => new RelayCommand(_ => AddCard());

        private void AddCard()
        {
            var item = CreateNewItem();
            Counter++;
            Items.Add(item); // 触发 OnItemsChanged -> 订阅 + 保存
            SelectedItem = item;
        }

        /// <summary>
        /// 卡型号候选：按当前「品牌 + 总线类型」过滤 CardFamilyCatalog（供卡型号下拉框）。
        /// <para>品牌优先；品牌下没有匹配卡型时忽略品牌过滤，避免下拉为空。总线类型同理。</para>
        /// </summary>
        public IReadOnlyList<string> CardTypeOptions => BuildCardTypeOptions();

        private IReadOnlyList<string> BuildCardTypeOptions()
        {
            var result = new List<string>();
            var ctl = SelectedItem;
            if (ctl == null) return result;

            IEnumerable<CardFamilyDescriptor> list = CardFamilyCatalog.Families;

            // ① 品牌
            string v = CardFamilyCatalog.NormalizeVendor(ctl.Vendor);
            if (!string.IsNullOrWhiteSpace(v) && v != "自定义")
            {
                var byVendor = list.Where(f => f.Vendor == v).ToList();
                if (byVendor.Count > 0) list = byVendor;
            }

            // ② 总线类型（脉冲 / EtherCAT / CANopen …）
            if (!string.IsNullOrWhiteSpace(ctl.BusType))
            {
                var byBus = list.Where(f => f.BusTypes != null
                                            && f.BusTypes.Any(b => CardVendorRegistry.BusTypeName(b) == ctl.BusType)).ToList();
                if (byBus.Count > 0) list = byBus;
            }

            foreach (var f in list) result.Add(f.Key);
            return result;
        }

        // ============ 扩展 IO 模块（挂在控制卡内部） ============

        private static readonly string[] _moduleOptions = ExpansionModuleCatalog.Modules.Select(m => m.Name).ToArray();

        /// <summary>可选扩展 IO 模块型号（供下拉框绑定）。</summary>
        public string[] ExpansionModuleOptions => _moduleOptions;

        private string _selectedModuleType = ExpansionModuleCatalog.Modules[0].Name;
        /// <summary>「添加扩展模块」时选用的型号。</summary>
        public string SelectedModuleType { get => _selectedModuleType; set => SetField(ref _selectedModuleType, value); }

        /// <summary>向当前控制卡添加一个扩展 IO 模块（型号取 <see cref="SelectedModuleType"/>，数量默认 1）。</summary>
        public ICommand AddExpansionModuleCommand => new RelayCommand(_ => AddExpansionModule());

        private void AddExpansionModule()
        {
            if (SelectedItem == null) return;
            var module = new ExpansionModuleItem { ModuleType = SelectedModuleType, Count = 1 };
            ApplySpec(module);                       // 先按型号带出默认 IO / 轴数
            SelectedItem.ExpansionModules.Add(module); // 触发 CollectionChanged -> 订阅 + 汇总 + 保存
        }

        /// <summary>删除一个扩展 IO 模块（命令参数为模块对象）。</summary>
        public ICommand RemoveExpansionModuleCommand => new RelayCommand(m => RemoveExpansionModule(m));

        private void RemoveExpansionModule(object? parameter)
        {
            if (SelectedItem == null || parameter is not ExpansionModuleItem module) return;
            SelectedItem.ExpansionModules.Remove(module);
        }

        // ============ 连接状态：真实来自底层 WenQiZhiCardBridge / HardwareSetup ============
        // ★ 不维护 UI 自己的影子字典：在线状态直接读底层 slot 的 Ready（卡族层按控制器名、
        //   雷赛层按整卡就绪），连接 / 断开 / 获取后刷新通知即可。

        /// <summary>当前选中控制卡的在线状态（真实来自底层）。</summary>
        public bool IsOnline
        {
            get
            {
                var ctl = SelectedItem;
                if (ctl == null) return false;
                if (HardwareSetup.Mode == HardwareMode.CardFamilies && HardwareSetup.CardFamilies != null)
                    return HardwareSetup.CardFamilies.IsControllerReady(ctl.Name);
                if (HardwareSetup.Mode == HardwareMode.Leadshine)
                    return HardwareSetup.IsCardReady;
                return false;
            }
        }

        /// <summary>在线 / 离线文字（给状态药丸用）。</summary>
        public string ConnectionText => IsOnline ? "在线" : "离线";

        /// <summary>所有已初始化控制器的底层真实状态（来自 WenQiZhiCardBridge.ControllerStatus）。</summary>
        public IReadOnlyList<string> ConnectionStatusLines
            => HardwareSetup.CardFamilies?.ControllerStatus() ?? Array.Empty<string>();

        /// <summary>当前选中控制卡连接后从底层真实检测到的轴数量（供控制器页显示）。</summary>
        public int DetectedAxisCount => SelectedItem?.DetectedAxisCount ?? 0;

        /// <summary>当前选中控制卡连接后从底层真实检测到的输入 IO 数量。</summary>
        public int DetectedInIo => SelectedItem?.DetectedInIo ?? 0;

        /// <summary>当前选中控制卡连接后从底层真实检测到的输出 IO 数量。</summary>
        public int DetectedOutIo => SelectedItem?.DetectedOutIo ?? 0;

        /// <summary>连接后真实检测到轴 / IO 数量变化时触发，供轴页 / IO 页刷新显示。</summary>
        public static event EventHandler DetectedCountsChanged;

        /// <summary>通知轴页 / IO 页刷新真实数量显示。</summary>
        private static void RaiseDetected() => DetectedCountsChanged?.Invoke(null, EventArgs.Empty);

        private string _connectMessage = string.Empty;
        /// <summary>连接 / 获取 的结果说明（显示在扩展IO卡片下）。</summary>
        public string ConnectMessage { get => _connectMessage; set => SetField(ref _connectMessage, value); }

        /// <summary>是否正在连接控制卡（后台线程执行，UI 不冻结）。</summary>
        private bool _isConnecting;
        public bool IsConnecting { get => _isConnecting; set => SetField(ref _isConnecting, value); }

        /// <summary>连接过程中的步骤文本，供进度条旁显示「正在干什么」。</summary>
        private string _connectStatusText = "就绪";
        public string ConnectStatusText { get => _connectStatusText; set => SetField(ref _connectStatusText, value); }

        /// <summary>获取：探测该控制卡匹配的卡族与扩展IO能力，刷新可添加的型号候选。</summary>
        public ICommand FetchModulesCommand => new RelayCommand(_ => FetchModules());

        private void FetchModules()
        {
            var ctl = SelectedItem;
            if (ctl == null) return;

            var fam = WenQiZhiCardBridge.ResolveFamily(ctl);
            bool exp = WenQiZhiCardBridge.SupportsExpansionIo(ctl);
            int n = ExpansionModuleCatalog.Modules.Length;

            ConnectMessage = IsOnline
                ? $"已获取：{(fam == null ? "未匹配到已移植卡族" : "卡族 " + fam.Key)}；"
                  + $"{(exp ? "支持扩展 IO 模块" : "未声明支持扩展 IO 模块")}；可选型号 {n} 种。"
                : $"未连接控制卡：{(fam == null ? "卡型号未匹配到已移植卡族" : "卡族 " + fam.Key)}；"
                  + $"{(exp ? "支持扩展 IO 模块" : "未声明支持扩展 IO 模块")}。已载入内置型号 {n} 种，请先点「连接」。";
            HardwareLog.Write("[控制器] " + ConnectMessage);
            RaiseStatusLines();   // 刷新底层连接总览
        }

        /// <summary>连接：初始化 / 打开该控制卡（含其扩展 IO 模块），成功后在线。</summary>
        public ICommand ConnectCommand => new RelayCommand(_ => Connect());

        private void Connect()
        {
            var ctl = SelectedItem;
            if (ctl == null) return;
            if (IsConnecting) return;   // 防止重复点击

            var dispatcher = System.Windows.Application.Current.Dispatcher;
            IsConnecting = true;
            ConnectStatusText = "正在初始化硬件层...";

            System.Threading.Tasks.Task.Run(() =>
            {
                bool ok = false;
                string msg = string.Empty;
                System.Exception runError = null;
                try
                {
                    dispatcher.Invoke(() => ConnectStatusText = "正在初始化硬件层...");
                    HardwareSetup.EnsureInitialized();   // 按工程自动装配：卡族层 / 雷赛封装
                    dispatcher.Invoke(() => ConnectStatusText = "正在连接控制卡...");
                    if (HardwareSetup.Mode == HardwareMode.CardFamilies && HardwareSetup.CardFamilies != null)
                    {
                        ok = HardwareSetup.CardFamilies.TryConnect(ctl, out msg);
                    }
                    else
                    {
                        // 雷赛自有封装：重连并看卡是否就绪
                        msg = HardwareSetup.Reconnect();
                        ok = HardwareSetup.IsCardReady;
                    }
                }
                catch (System.Exception ex) { runError = ex; }

                // 数据生成 / 状态刷新会触碰 UI 绑定的集合与属性，统一回到 UI 线程执行
                dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (runError != null)
                        {
                            ConnectStatusText = "连接失败";
                            ConnectMessage = "● 连接异常：" + runError.Message;
                            HardwareLog.Write("[控制器] " + ConnectMessage);
                            return;
                        }

                        var fam = WenQiZhiCardBridge.ResolveFamily(ctl);

                        // ★ 连接后从底层硬件真实读取轴数 / IO 数，回退到配置值；真实值 > 0 时回写配置让汇总 / IO 生成一致
                        ConnectStatusText = "正在读取真实轴 / IO 数量...";
                        FetchDetectedCounts(ctl);

                        // 本 VM 自己的「真实检测」代理属性也要刷新（静态事件只通知轴页 / IO 页）
                        OnPropertyChanged(nameof(DetectedAxisCount));
                        OnPropertyChanged(nameof(DetectedInIo));
                        OnPropertyChanged(nameof(DetectedOutIo));

                        RaiseConnection();
                        RaiseStatusLines();
                        RaiseTotals();
                        RaiseDetected();   // 通知轴页 / IO 页刷新真实数量
                        ConnectStatusText = "正在生成轴与 IO 点...";
                        string axisMsg = GenerateAxisPoints(ctl);   // 连接后按底层真实轴数自动生成轴（先清空本卡轴再添加）
                        string ioMsg = GenerateIoPoints(ctl);   // 连接后按「主板 + 扩展模块」的 IO 数自动生成 IO 点
                        ConnectStatusText = "连接完成";
                        ConnectMessage = (ok ? "● 已连接：" : "○ 未连接：") + ctl.Name + " —— " + msg
                            + (fam == null ? "。★卡型号未匹配到已移植卡族，轴 / IO 不会真实下发。" : string.Empty)
                            + (ctl.DetectedAxisCount > 0
                                ? $" 真实检测：轴 {ctl.DetectedAxisCount} / 输入 {ctl.DetectedInIo} / 输出 {ctl.DetectedOutIo}。"
                                : string.Empty)
                            + " " + axisMsg + " " + ioMsg;
                        HardwareLog.Write("[控制器] " + ConnectMessage);
                        UpdateStatusBar();
                    }
                    catch (System.Exception ex)
                    {
                        ConnectStatusText = "连接失败";
                        ConnectMessage = "● 连接异常：" + ex.Message;
                        HardwareLog.Write("[控制器] " + ConnectMessage);
                    }
                    finally { IsConnecting = false; }
                });
            });
        }

        /// <summary>
        /// 按控制卡的 IO 配置自动生成 IO 点：主板（模块=0）+ 各扩展模块（模块号=第几个模块，从 1 起）。
        /// <para>连接时先<b>全局清空</b>工程里所有 IO 点，再只重建本控制卡的（避免旧 / 手工 / 其它卡的点残留叠加）。</para>
        /// </summary>
        private static string GenerateIoPoints(AxisControllerItem ctl)
        {
            var data = ProjectStore.Data;
            if (data == null) return "（工程未加载，未生成 IO 点）";

            string tag = ctl.Name;

            // 全局清空：连接时把工程里所有 IO 点先清空，再只重建本控制卡的（避免旧 / 手工 / 其它卡的点残留叠加）
            data.Inputs.Clear();
            data.Outputs.Clear();

            int nIn = 0, nOut = 0;

            // 主板 IO：模块=0，序号 1..N
            for (int s = 1; s <= System.Math.Max(ctl.InIoCount, 0); s++)
                data.Inputs.Add(MakeIo(tag, "输入", ++nIn, 0, s));
            for (int s = 1; s <= System.Math.Max(ctl.OutIoCount, 0); s++)
                data.Outputs.Add(MakeIo(tag, "输出", ++nOut, 0, s));

            // 扩展模块 IO：模块号从 1 起，按「数量」逐个模块实例展开（与卡族扩展IO寻址一致：模块=从站号、序号=位号）
            int moduleOrdinal = 0;
            foreach (var m in ctl.ExpansionModules)
            {
                int units = System.Math.Max(m.Count, 1);
                for (int u = 0; u < units; u++)
                {
                    moduleOrdinal++;
                    for (int s = 1; s <= System.Math.Max(m.InIo, 0); s++)
                        data.Inputs.Add(MakeIo(tag, "输入", ++nIn, moduleOrdinal, s));
                    for (int s = 1; s <= System.Math.Max(m.OutIo, 0); s++)
                        data.Outputs.Add(MakeIo(tag, "输出", ++nOut, moduleOrdinal, s));
                }
            }

            Catalog.SetIo(data.Inputs.Select(x => x.Name).Concat(data.Outputs.Select(x => x.Name)));
            ProjectStore.ScheduleSave();
            return $"已按配置自动生成 IO 点：输入 {nIn} / 输出 {nOut}（控制器「{tag}」）。";
        }

        private static IoItem MakeIo(string controller, string prefix, int index, int moduleNo, int seq) => new IoItem
        {
            Name = $"{prefix}{index}",
            Controller = controller,   // 自动绑定到本控制卡（IO 表不再显示该列，但桥接寻址要用）
            ModuleNo = moduleNo,       // 0=主板，>0=扩展模块从站号
            Sequence = seq,            // 位号
            Level = "取反",
            Function = "动点",
        };

        /// <summary>
        /// 按控制卡的真实 / 配置轴数自动生成轴：连接时先<b>全局清空</b>工程里所有轴，再只重建本控制卡的。
        /// <para>数量优先取底层真实检测到的轴数（连接后从卡读到），为 0 时回退到配置轴数（AxisCount）。</para>
        /// </summary>
        private static string GenerateAxisPoints(AxisControllerItem ctl)
        {
            var data = ProjectStore.Data;
            if (data == null) return "（工程未加载，未生成轴）";

            string tag = ctl.Name;
            // 全局清空：连接时把工程里所有轴先清空，再只重建本控制卡的
            data.Axes.Clear();

            int count = ctl.DetectedAxisCount > 0 ? ctl.DetectedAxisCount : System.Math.Max(ctl.AxisCount, 0);
            int n = 0;
            for (int s = 1; s <= count; s++)
                data.Axes.Add(MakeAxis(tag, ++n, s));

            Catalog.SetAxis(data.Axes.Select(x => x.Name));
            ProjectStore.ScheduleSave();
            return $"已按底层自动生成轴：{n}（控制器「{tag}」）。";
        }

        private static AxisItem MakeAxis(string controller, int index, int axisNo) => new AxisItem
        {
            Name = $"轴{index}",
            Controller = controller,   // 自动绑定到本控制卡
            AxisNo = axisNo,
            Enabled = true,
        };

        /// <summary>断开：关闭该控制卡的硬件连接，回到离线。</summary>
        public ICommand DisconnectCommand => new RelayCommand(_ => Disconnect());

        private void Disconnect()
        {
            var ctl = SelectedItem;
            if (ctl == null) return;

            try { HardwareSetup.CardFamilies?.Disconnect(ctl); } catch { /* 断开失败也置离线 */ }
            RaiseConnection();
            RaiseStatusLines();
            UpdateStatusBar();
            ConnectMessage = $"○ 已断开「{ctl.Name}」。";
            HardwareLog.Write("[控制器] " + ConnectMessage);
        }

        /// <summary>后台自动连接工程里的所有控制卡（打开软件 / 切换工程时调用），刷新状态栏在线指示；不阻塞 UI。</summary>
        public void AutoConnectAll()
        {
            if (IsConnecting) return;
            var controllers = Items.ToList();
            if (controllers.Count == 0) { UpdateStatusBar(); return; }

            var dispatcher = System.Windows.Application.Current.Dispatcher;
            IsConnecting = true;
            ConnectStatusText = "正在自动连接控制卡...";
            StatusBarService.ReportInfo("正在后台自动连接控制卡…");
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    HardwareSetup.EnsureInitialized();
                    if (HardwareSetup.Mode == HardwareMode.CardFamilies && HardwareSetup.CardFamilies != null)
                    {
                        foreach (var c in controllers)
                            HardwareSetup.CardFamilies.TryConnect(c, out _);
                    }
                    else if (HardwareSetup.Mode == HardwareMode.Leadshine)
                    {
                        HardwareSetup.Reconnect();
                    }
                    dispatcher.Invoke(() =>
                    {
                        foreach (var c in controllers) FetchDetectedCounts(c);
                        RaiseConnection();
                        RaiseStatusLines();
                        RaiseTotals();
                        RaiseDetected();
                        UpdateStatusBar();
                        ConnectStatusText = "连接完成";
                        int online = 0;
                        foreach (var c in controllers) if (IsControllerReady(c)) online++;
                        ConnectMessage = $"已自动连接控制卡：{online}/{controllers.Count} 在线。";
                        StatusBarService.ReportInfo($"已自动连接控制卡：{online}/{controllers.Count} 在线。");
                        HardwareLog.Write("[控制器] " + ConnectMessage);
                    });
                }
                catch (System.Exception ex)
                {
                    dispatcher.Invoke(() =>
                    {
                        ConnectStatusText = "连接失败";
                        StatusBarService.ReportException("自动连接控制卡失败：" + ex.Message);
                        HardwareLog.Write("[控制器] 自动连接异常：" + ex.Message);
                    });
                }
                finally
                {
                    dispatcher.Invoke(() => IsConnecting = false);
                }
            });
        }

        private static bool IsControllerReady(AxisControllerItem ctl)
            => HardwareSetup.Mode == HardwareMode.Leadshine
                ? HardwareSetup.IsCardReady
                : (HardwareSetup.CardFamilies != null && HardwareSetup.CardFamilies.IsControllerReady(ctl.Name));

        private void UpdateStatusBar()
        {
            int total = Items.Count;
            int online = 0;
            foreach (var c in Items) if (IsControllerReady(c)) online++;
            StatusBarService.SetControllerStatus(online, total);
        }

        private void RaiseConnection()
        {
            OnPropertyChanged(nameof(IsOnline));
            OnPropertyChanged(nameof(ConnectionText));
        }

        /// <summary>通知界面刷新底层连接总览（来自 WenQiZhiCardBridge.ControllerStatus）。</summary>
        private void RaiseStatusLines() => OnPropertyChanged(nameof(ConnectionStatusLines));

        /// <summary>
        /// 连接成功后从底层真实读取轴 / IO 数量：卡族层走 <see cref="WenQiZhiCardBridge.TryGetRealCounts"/>，
        /// 雷赛层走 <see cref="LtdmcCard"/> 上报的轴数；硬件未返回有效值时保留用户配置。
        /// 真实值 &gt; 0 时回写配置（AxisCount / InIoCount / OutIoCount），使「数量汇总」与自动生成的 IO 点与硬件一致。
        /// </summary>
        private static void FetchDetectedCounts(AxisControllerItem ctl)
        {
            int axis = 0, inIo = 0, outIo = 0;
            if (HardwareSetup.Mode == HardwareMode.CardFamilies && HardwareSetup.CardFamilies != null)
                HardwareSetup.CardFamilies.TryGetRealCounts(ctl.Name, out axis, out inIo, out outIo);
            else if (HardwareSetup.Mode == HardwareMode.Leadshine)
            {
                var info = LtdmcCard.FirstCard;
                axis = info == null ? 0 : (int)(info.IsBusCard ? info.BusAxes : info.LocalAxes);
            }
            ctl.DetectedAxisCount = axis;
            ctl.DetectedInIo = inIo;
            ctl.DetectedOutIo = outIo;
            if (axis > 0) ctl.AxisCount = axis;
            if (inIo > 0) ctl.InIoCount = inIo;
            if (outIo > 0) ctl.OutIoCount = outIo;
        }

        // ============ 汇总：输入 / 输出 IO 总数、轴总数 ============

        /// <summary>输入 IO 总数 = 主板输入 + Σ(扩展模块数量 × 单模块输入)。</summary>
        public int TotalInIo => SelectedItem == null ? 0 : SelectedItem.InIoCount + SelectedItem.ExpansionModules.Sum(m => m.Count * m.InIo);

        /// <summary>输出 IO 总数 = 主板输出 + Σ(扩展模块数量 × 单模块输出)。</summary>
        public int TotalOutIo => SelectedItem == null ? 0 : SelectedItem.OutIoCount + SelectedItem.ExpansionModules.Sum(m => m.Count * m.OutIo);

        /// <summary>轴总数 = 卡自身轴数 + Σ(扩展模块数量 × 单模块附加轴数)。</summary>
        public int TotalAxis => SelectedItem == null ? 0 : SelectedItem.AxisCount + SelectedItem.ExpansionModules.Sum(m => m.Count * m.AxisCount);

        private AxisControllerItem? _tracked;

        private void OnSelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedItem))
                WireSelected(SelectedItem);
        }

        /// <summary>把总汇订阅挂到当前选中卡上，并解除旧卡的订阅。</summary>
        private void WireSelected(AxisControllerItem? item)
        {
            if (ReferenceEquals(_tracked, item)) return;

            if (_tracked != null)
            {
                _tracked.ExpansionModules.CollectionChanged -= OnModulesChanged;
                foreach (var m in _tracked.ExpansionModules) m.PropertyChanged -= OnModulePropertyChanged;
                _tracked.PropertyChanged -= OnTrackedCardChanged;
            }

            _tracked = item;

            if (_tracked != null)
            {
                _tracked.ExpansionModules.CollectionChanged += OnModulesChanged;
                foreach (var m in _tracked.ExpansionModules) m.PropertyChanged += OnModulePropertyChanged;
                _tracked.PropertyChanged += OnTrackedCardChanged;
            }

            RaiseTotals();
            RaiseConnection();   // 切换选中卡时同步在线/离线显示
            OnPropertyChanged(nameof(CardTypeOptions));   // 切换选中卡时刷新卡型号候选
            OnPropertyChanged(nameof(DetectedAxisCount));   // 切换选中卡时刷新真实检测数量显示
            OnPropertyChanged(nameof(DetectedInIo));
            OnPropertyChanged(nameof(DetectedOutIo));
        }

        private void OnTrackedCardChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AxisControllerItem.InIoCount)
                || e.PropertyName == nameof(AxisControllerItem.OutIoCount)
                || e.PropertyName == nameof(AxisControllerItem.AxisCount))
                RaiseTotals();

            // 品牌 / 总线类型变了 → 卡型号候选跟着变；型号若不在新候选里则清空，避免与实际不符。
            if (e.PropertyName == nameof(AxisControllerItem.Vendor)
                || e.PropertyName == nameof(AxisControllerItem.BusType))
            {
                OnPropertyChanged(nameof(CardTypeOptions));
                if (SelectedItem != null && !string.IsNullOrEmpty(SelectedItem.CardType)
                    && !CardTypeOptions.Contains(SelectedItem.CardType))
                    SelectedItem.CardType = string.Empty;
            }
        }

        private void OnModulesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (ExpansionModuleItem m in e.NewItems)
                {
                    m.PropertyChanged += OnModulePropertyChanged;
                    ApplySpec(m);
                }
            if (e.OldItems != null)
                foreach (ExpansionModuleItem m in e.OldItems)
                    m.PropertyChanged -= OnModulePropertyChanged;

            RaiseTotals();
            ProjectStore.ScheduleSave();
        }

        private bool _applyingSpec;

        private void OnModulePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 换型号时自动带出该型号的输入 / 输出 / 轴数（带出的值仍可手工微调）。
            if (!_applyingSpec && sender is ExpansionModuleItem m && e.PropertyName == nameof(ExpansionModuleItem.ModuleType))
                ApplySpec(m);

            RaiseTotals();
            ProjectStore.ScheduleSave();
        }

        private void ApplySpec(ExpansionModuleItem module)
        {
            var spec = ExpansionModuleCatalog.ByName(module.ModuleType);
            if (spec == null) return;
            _applyingSpec = true;
            module.InIo = spec.InIo;
            module.OutIo = spec.OutIo;
            module.AxisCount = spec.AxisCount;
            _applyingSpec = false;
        }

        private void RaiseTotals()
        {
            OnPropertyChanged(nameof(TotalInIo));
            OnPropertyChanged(nameof(TotalOutIo));
            OnPropertyChanged(nameof(TotalAxis));
        }

        /// <summary>自动识别硬件：重新连接控制卡，把检测到的每张卡登记为一个控制器。</summary>
        public ICommand AutoDetectCommand => new RelayCommand(_ => AutoDetect());

        /// <summary>自动识别结果提示（显示在页面状态条）。</summary>
        public string DetectMessage { get => _detectMessage; set => SetField(ref _detectMessage, value); }
        private string _detectMessage = string.Empty;

        private void AutoDetect()
        {
            var sb = new StringBuilder();
            int added = 0;

            // 1) 雷赛（已集成真实对接）：重连并扫描真实卡数量
            LtdmcCard.Close();
            var leadStatus = HardwareSetup.Reconnect();
            int leadCards = LtdmcCard.CardCount;
            if (leadCards > 0)
            {
                // ★ 卡型不能写死「脉冲」：DMC-E 3000/5000 是总线卡，轴与 IO 走 nmc_ 族，
                //   写死脉冲会让用户照着错的分支接线。脉冲 / 总线由 LtdmcCard 探测得出
                //   （卡型取自 dmc_get_CardInfList，轴数与从站数取自 nmc_get_total_axes /
                //   nmc_get_total_slaves，总线状态取自 nmc_get_errcode），这里照实登记。
                var info = LtdmcCard.FirstCard;
                bool isBus = info != null && info.IsBusCard;
                int axisCount = info == null ? 0 : (int)(isBus ? info.BusAxes : info.LocalAxes);

                for (int i = 0; i < leadCards; i++)
                {
                    Items.Add(new AxisControllerItem
                    {
                        Kind = "控制卡",
                        Vendor = "雷赛",
                        CardType = info == null || info.CardType == 0 ? "DMC" : $"0x{info.CardType:X}",
                        BusType = LtdmcCard.DescribeBusType(info),
                        Connection = isBus ? "EtherCAT" : "PCI",
                        AxisCount = axisCount > 0 ? axisCount : 4,
                        CardNo = i,
                        Name = $"控制卡{Items.Count + 1}",
                        Description = info == null ? string.Empty : info.Summary
                    });
                    added++;
                }
                sb.AppendLine($"雷赛：检测到 {leadCards} 张控制卡，已登记。");
                if (info != null)
                {
                    sb.AppendLine("　" + info.Summary);
                    if (info.IsBusCard && info.BusErrCode != 0)
                        sb.AppendLine("　★总线错误码非 0：EtherCAT 没在正常通信，请先查网线 / 从站上电，再试轴。");
                }
            }
            else
            {
                sb.AppendLine("雷赛：未检测到控制卡。" + leadStatus);
            }

            // 2) 已移植的运动控制卡族：按底层库（DLL）是否存在识别，登记为该卡族的控制器。
            //    只探库、不初始化卡 —— 真正初始化推迟到首次轴 / IO 动作时懒加载（见 WenQiZhiCardBridge），
            //    免得自动识别阶段去碰一个没插卡的驱动把界面卡住。
            foreach (var fam in CardFamilyCatalog.DetectPresent())
            {
                if (fam.Vendor == "雷赛") continue;          // 雷赛已在上面按实物卡真实扫描
                string busName = fam.BusTypes.Length > 0
                    ? CardVendorRegistry.BusTypeName(fam.BusTypes[0])
                    : "其它";
                Items.Add(new AxisControllerItem
                {
                    Kind = "控制卡",
                    Vendor = string.IsNullOrEmpty(fam.Vendor) ? "未分类" : fam.Vendor,
                    CardType = fam.Key,
                    BusType = busName,
                    Connection = fam.BusTypes.Contains(CardBusType.EtherCAT) ? "EtherCAT" : "PCI",
                    Name = $"控制卡{Items.Count + 1}",
                    Description = fam.DisplayName + "（已移植卡族，底层库：" + string.Join(" / ", fam.NativeDlls) + "）"
                });
                added++;
                sb.AppendLine($"{fam.DisplayName}：底层库已就位（{string.Join(" / ", fam.NativeDlls)}），"
                            + $"已登记为卡族 {fam.Key}，首次动作时初始化控制卡。");
            }

            // 3) 其它主流厂商：按驱动库（DLL）是否存在识别，驱动在即登记控制器并提示待接入对接
            foreach (var v in CardVendorRegistry.Vendors)
            {
                if (v.Vendor == "雷赛") continue;            // 雷赛已在上面真实扫描
                if (v.HasLiveBridge) continue;               // 已移植卡族已在上面登记
                if (!CardVendorRegistry.DllPresent(v)) continue;
                string bus = v.BusTypes.Length > 0 ? CardVendorRegistry.BusTypeName(v.BusTypes[0]) : "其它";
                Items.Add(new AxisControllerItem
                {
                    Kind = "控制卡",
                    Vendor = v.Vendor,
                    BusType = bus,
                    Name = $"{v.Vendor}控制器{Items.Count + 1}",
                    Description = "驱动已安装，待接入实时对接"
                });
                added++;
                sb.AppendLine($"{v.DisplayName}：驱动已安装（{string.Join(" / ", v.DllNames)}），已登记；实时对接待接入。");
            }

            Counter = Items.Count;
            if (Items.Count > 0) SelectedItem = Items[Items.Count - 1];

            DetectMessage = added > 0
                ? $"自动识别完成：共登记 {added} 个控制器。\n" + sb.ToString().TrimEnd()
                : "未检测到任何控制卡或驱动。请确认控制卡已插好、驱动已安装，并把对应 DLL 放到程序目录。";
            HardwareLog.Write("[硬件识别] " + DetectMessage);
        }

        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0) SelectedItem = Items[0];
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
