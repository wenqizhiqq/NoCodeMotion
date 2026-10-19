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
            ProjectManager.DataReloaded += () => RequestAutoConnect();
            if (Items.Count > 0) RequestAutoConnect();
            else UpdateStatusBar();
        }

        /// <summary>新增控制卡的默认配置：模拟卡 + VirtualMotionCard（32 轴 / 脉冲 / PCI）。
        /// 新建工程还没有实物卡时，按这个默认值就能直接连上仿真卡写流程、生成轴，不必手填每一项。</summary>
        protected override AxisControllerItem CreateNewItem() => new AxisControllerItem
        {
            Kind = "控制卡",
            Name = $"控制卡{Counter + 1}",
            Vendor = "模拟卡",
            BusType = "脉冲",
            CardType = "VirtualMotionCard",
            CardNo = 0,
            AxisCount = 32,
            Connection = "PCI",
        };

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

        // ===== 流程门控：选卡型号 → 选扩展 IO → 再连接 =====
        /// <summary>是否已选择卡型号。未选型号时不允许配置扩展 IO / 连接。</summary>
        public bool HasCardType => !string.IsNullOrWhiteSpace(SelectedItem?.CardType);

        /// <summary>只有选了卡型号后才允许配置「扩展 IO 模块」（主板 IO 数 / 挂接模块）。</summary>
        public bool CanConfigureExpansion => HasCardType;

        /// <summary>只有选了卡型号后才允许点「连接控制器」。</summary>
        public bool CanConnect => HasCardType;

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

        /// <summary>是否已挂「等待加载遮罩结束」的一次性回调，避免重复订阅。</summary>
        private bool _autoConnectPending;

        /// <summary>
        /// 请求自动连接：**必须等「启动预初始化 / 打开工程」的加载遮罩结束后**再在后台连接。
        /// <para>连接会做硬件层装配（<c>HardwareSetup.EnsureInitialized</c> → 枚举已移植卡族库、加载底层 DLL）。
        /// 若与载入阶段同跑，会与页面预初始化争用原生加载器锁，拖住载入进度条，让用户误以为
        /// 「连上控制器才进主界面」。这里显式等到 <see cref="LoadingService.IsLoading"/> 为 false，
        /// 保证**先进主界面**，再由状态栏显示「正在连接…」→ 连接结果。</para>
        /// </summary>
        private void RequestAutoConnect()
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null) { AutoConnectAll(); return; }

            if (!LoadingService.IsLoading)
            {
                dispatcher.BeginInvoke(new Action(AutoConnectAll), System.Windows.Threading.DispatcherPriority.Background);
                return;
            }

            // 载入遮罩还在 → 等它隐藏后再后台连接（一次性订阅，避免重复）；绝不在载入阶段争用硬件初始化
            if (_autoConnectPending) return;
            _autoConnectPending = true;

            void OnLoadingStateChanged()
            {
                if (LoadingService.IsLoading) return;
                LoadingService.StateChanged -= OnLoadingStateChanged;
                _autoConnectPending = false;
                dispatcher.BeginInvoke(new Action(AutoConnectAll), System.Windows.Threading.DispatcherPriority.Background);
            }
            LoadingService.StateChanged += OnLoadingStateChanged;
        }

        /// <summary>连接：初始化 / 打开该控制卡（含其扩展 IO 模块），成功后在线。</summary>
        public ICommand ConnectCommand => new RelayCommand(_ => Connect());

        private void Connect()
        {
            var ctl = SelectedItem;
            if (ctl == null) return;
            if (IsConnecting) return;   // 防止重复点击
            var ctrls = Items.ToList();   // UI 线程快照，供后台统计在线数（避免在 UI 线程读硬件）

            var dispatcher = System.Windows.Application.Current.Dispatcher;
            IsConnecting = true;
            ConnectStatusText = "正在初始化硬件层...";
            StatusBarService.SetControllerConnecting(true);

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

                // ★ 真实轴 / IO 数量、在线数都在**后台线程**读取（TryGetRealCounts / ControllerReady 是硬件查询）。
                //   放到 UI 线程会让「连接」期间页面卡住、点不动——这里全部留在后台，UI 线程只做赋值与通知。
                int da = 0, di = 0, doo = 0, onlineNow = 0;
                if (runError == null)
                {
                    dispatcher.Invoke(() => ConnectStatusText = "正在读取真实轴 / IO 数量...");
                    try
                    {
                        ReadRealCounts(ctl, out da, out di, out doo);
                        foreach (var c in ctrls) if (IsControllerReady(c)) onlineNow++;
                    }
                    catch { /* 读不到就回退到配置值 */ }
                }

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

                        // 把后台读到的真实数量写回模型（真实值 > 0 时回写配置让汇总 / IO 生成一致）
                        ApplyRealCounts(ctl, da, di, doo);

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
                                : $" 卡未上报轴数（未插卡或未就绪，且卡族目录未登记该型号的设计容量），本次按工程配置的 {ctl.AxisCount} 根轴生成。")
                            + " " + axisMsg + " " + ioMsg;
                        HardwareLog.Write("[控制器] " + ConnectMessage);
                        StatusBarService.SetControllerStatus(onlineNow, ctrls.Count);   // 用后台统计的在线数，不再在 UI 线程读硬件
                    }
                    catch (System.Exception ex)
                    {
                        ConnectStatusText = "连接失败";
                        ConnectMessage = "● 连接异常：" + ex.Message;
                        HardwareLog.Write("[控制器] " + ConnectMessage);
                    }
                    finally { IsConnecting = false; StatusBarService.SetControllerConnecting(false); }
                });
            });
        }

        /// <summary>
        /// 连接控制器（合并原「获取卡参数」+「连接」两个按钮）：先「获取」探测该卡的卡族与扩展 IO 能力，
        /// 再弹出进度弹窗，在后台完成初始化 / 连接 / 读取真实轴IO / 生成点位，连接结束后弹窗自动关闭。
        /// </summary>
        public ICommand ConnectControllerCommand => new RelayCommand(_ => ConnectController());

        private void ConnectController()
        {
            var ctl = SelectedItem;
            if (ctl == null) { StatusBarService.ReportInfo("请先在左侧选择要连接的控制卡。"); return; }
            if (IsConnecting) return;   // 已在连接中，不重复弹窗

            FetchModules();   // ① 获取卡参数（探测卡族 / 扩展IO能力）

            // ② 弹窗显示进度条：弹窗 OnContentRendered 里启动 Connect()，连接完成后自动关闭
            var dlg = new NoCodeMotion.Views.ConnectProgressDialog(() => Connect(), this);
            dlg.ShowDialog();
        }

        /// <summary>
        /// 按控制卡的 IO 配置调整<b>本卡</b>的 IO 点（主板 + 各扩展模块）：只改数量，不改名称与参数。
        /// <para>按「模块号 + 位号」逐槽位比对：槽位仍在的原样保留（名称、功能、电平都不动），
        /// 槽位消失（数量变小 / 扩展模块被删）才删除，缺的槽位补默认名「输入N / 输出N」；
        /// 其它控制器的 IO 点不参与。</para>
        /// </summary>
        private static string GenerateIoPoints(AxisControllerItem ctl)
        {
            var data = ProjectStore.Data;
            if (data == null) return "（工程未加载，未生成 IO 点）";

            string tag = ctl.Name;

            // 目标槽位：主板（模块=0）位号 0..N-1，再按扩展模块实例逐个展开（模块号=从站号、位号从 0 起）。
            // ★ 位号必须从 0 开始：底层取位号是「模块 × 每模块位数 + 位号」（见 WenQiZhiCardBridge.MainBoardBit），
            //   从 1 开始会让每个点错位一位、最后一个点还超到卡上不存在的位号。
            var inSlots = new List<(int module, int seq)>();
            var outSlots = new List<(int module, int seq)>();
            for (int s = 0; s < System.Math.Max(ctl.InIoCount, 0); s++) inSlots.Add((0, s));
            for (int s = 0; s < System.Math.Max(ctl.OutIoCount, 0); s++) outSlots.Add((0, s));

            int moduleOrdinal = 0;
            foreach (var m in ctl.ExpansionModules)
            {
                int units = System.Math.Max(m.Count, 1);
                for (int u = 0; u < units; u++)
                {
                    moduleOrdinal++;
                    for (int s = 0; s < System.Math.Max(m.InIo, 0); s++) inSlots.Add((moduleOrdinal, s));
                    for (int s = 0; s < System.Math.Max(m.OutIo, 0); s++) outSlots.Add((moduleOrdinal, s));
                }
            }

            int addedIn = AdjustIoSlots(data.Inputs, tag, inSlots, "输入");
            int addedOut = AdjustIoSlots(data.Outputs, tag, outSlots, "输出");

            // ★ 三个名称库必须一起刷新：气缸页 4 个下拉绑的是按方向拆分的
            //   OutIoNames（伸出输出 / 缩回输出）与 InIoNames（伸出感应 / 缩回感应），
            //   节点图属性、流程页「输入IO / 输出IO」也读这两个；只刷新合并库 IoNames 的话，
            //   连接控制器后这两个库还停在旧值（新工程直接是空）→ 气缸页选不了任何 IO 名称。
            Catalog.SetIoIn(data.Inputs.Select(x => x.Name));
            Catalog.SetIoOut(data.Outputs.Select(x => x.Name));
            Catalog.SetIo(data.Inputs.Select(x => x.Name).Concat(data.Outputs.Select(x => x.Name)));
            ProjectStore.ScheduleSave();
            return $"本卡 IO 已按 输入 {inSlots.Count} / 输出 {outSlots.Count} 调整"
                + $"（新增 {addedIn + addedOut}、保留 {inSlots.Count + outSlots.Count - addedIn - addedOut}），"
                + "原有 IO 的名称与功能未改动。";
        }

        /// <summary>
        /// 把一个方向的 IO 点调到目标槽位集合（按「模块号 + 位号」比对，只动本控制器的点）：
        /// 槽位仍存在的保留不动、同槽位重复的只留第一个，槽位消失的删除，缺的补默认名。返回新增个数。
        /// </summary>
        private static int AdjustIoSlots(System.Collections.ObjectModel.ObservableCollection<IoItem> list, string controller,
                                         List<(int module, int seq)> slots, string prefix)
        {
            var want = new HashSet<(int, int)>(slots);
            var seen = new HashSet<(int, int)>();
            foreach (var io in list.Where(x => x != null && x.Controller == controller).ToList())
                if (!want.Contains((io.ModuleNo, io.Sequence)) || !seen.Add((io.ModuleNo, io.Sequence)))
                    list.Remove(io);

            int nextIndex = list.Count(x => x != null && x.Controller == controller);
            int added = 0;
            foreach (var (module, seq) in slots)
            {
                if (list.Any(x => x != null && x.Controller == controller && x.ModuleNo == module && x.Sequence == seq)) continue;
                string name = $"{prefix}{nextIndex}";
                for (int k = 2; list.Any(x => x != null && x.Name == name); k++) name = $"{prefix}{nextIndex}_{k}";
                var item = MakeIo(controller, prefix, nextIndex, module, seq);
                item.Name = name;
                list.Add(item);
                nextIndex++;
                added++;
            }
            return added;
        }

        private static IoItem MakeIo(string controller, string prefix, int index, int moduleNo, int seq) => new IoItem
        {
            Name = $"{prefix}{index}",
            Controller = controller,   // 自动绑定到本控制卡（IO 表不再显示该列，但桥接寻址要用）
            ModuleNo = moduleNo,       // 0=主板，>0=扩展模块从站号
            Sequence = seq,            // 位号
            Level = "取反",
            Function = IoFunctionCatalog.None,   // 默认「无」，按输入/输出方向在 IO 页选择真实功能
        };

        /// <summary>
        /// 按控制卡的真实 / 配置轴数调整<b>本卡</b>的轴：只改数量，不改名称与参数。
        /// <para>数量优先取底层真实检测到的轴数（连接后从卡读到），为 0 时回退到配置轴数（AxisCount）。</para>
        /// <para>多了就从轴号最大的尾部删，少了就补当前未占用的最小轴号（默认名「轴N」，与轴号同号）；
        /// 已存在的轴（含用户改过的名称、脉冲当量、速度、限位等）原样保留，其它控制器的轴也不动 ——
        /// 早先的「全局清空重建」会把改名和别的卡一起抹掉。</para>
        /// </summary>
        private static string GenerateAxisPoints(AxisControllerItem ctl)
        {
            var data = ProjectStore.Data;
            if (data == null) return "（工程未加载，未生成轴）";

            string tag = ctl.Name;
            int count = ctl.DetectedAxisCount > 0 ? ctl.DetectedAxisCount : System.Math.Max(ctl.AxisCount, 0);
            var mine = data.Axes.Where(a => a != null && a.Controller == tag).ToList();

            // 多了：从轴号最大的尾部删（轴号相同则删表里靠后的）
            int removed = 0;
            var drop = mine.OrderByDescending(a => a.AxisNo).ThenByDescending(a => data.Axes.IndexOf(a)).ToList();
            for (int i = 0; i < drop.Count && mine.Count - removed > count; i++)
            {
                data.Axes.Remove(drop[i]);
                removed++;
            }

            // 少了：补未占用的最小轴号；名字已被占用时加序号后缀，避免与其它卡的轴重名
            int added = 0;
            for (int i = mine.Count - removed; i < count; i++)
            {
                int no = 0;
                while (data.Axes.Any(a => a != null && a.Controller == tag && a.AxisNo == no)) no++;
                string name = $"轴{no}";
                for (int k = 2; data.Axes.Any(a => a != null && a.Name == name); k++) name = $"轴{no}_{k}";
                data.Axes.Add(MakeAxis(ctl, name, no));
                added++;
            }

            Catalog.SetAxis(data.Axes.Select(x => x.Name));
            ProjectStore.ScheduleSave();
            return $"本卡轴数已按 {count} 调整（新增 {added} / 删除 {removed}），原有轴的名称与参数未改动。";
        }

        /// <summary>造一根轴：名称与轴号由调用方给定（轴号 = 卡上的轴号，从 0 起）；
        /// 轴类型取本控制器的「总线类型」（轴页的轴类型只读，这里是写入点之一）。</summary>
        private static AxisItem MakeAxis(AxisControllerItem ctl, string name, int axisNo) => new AxisItem
        {
            Name = name,
            Controller = ctl.Name,     // 自动绑定到本控制卡
            AxisNo = axisNo,
            AxisType = AxisItem.AxisTypeOf(ctl.BusType),
            Enabled = true,
        };

        /// <summary>把某控制器名下所有轴的「轴类型」同步成该控制器的总线类型（轴页只读，故由此处代写）。</summary>
        private static void SyncAxisTypes(AxisControllerItem ctl)
        {
            var data = ProjectStore.Data;
            if (data == null || ctl == null) return;
            string want = AxisItem.AxisTypeOf(ctl.BusType);
            bool touched = false;
            foreach (var a in data.Axes)
            {
                if (a == null || a.Controller != ctl.Name) continue;
                if (a.AxisType == want) continue;
                a.AxisType = want;
                touched = true;
            }
            if (touched) ProjectStore.ScheduleSave();
        }

        /// <summary>断开：关闭该控制卡的硬件连接，回到离线。</summary>
        public ICommand DisconnectCommand => new RelayCommand(_ => Disconnect());

        private void Disconnect()
        {
            var ctl = SelectedItem;
            if (ctl == null) return;
            if (IsConnecting) return;

            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null) return;
            var ctrls = Items.ToList();

            // ★ 断开（CloseCard）也是硬件操作：放后台线程，UI 线程只做状态刷新与通知，避免点「断开」时界面卡住。
            System.Threading.Tasks.Task.Run(() =>
            {
                try { HardwareSetup.CardFamilies?.Disconnect(ctl); } catch { /* 断开失败也置离线 */ }

                int online = 0;
                foreach (var c in ctrls) if (IsControllerReady(c)) online++;
                int onlineCount = online, totalCount = ctrls.Count;

                dispatcher.Invoke(() =>
                {
                    RaiseConnection();
                    RaiseStatusLines();
                    StatusBarService.SetControllerStatus(onlineCount, totalCount);
                    ConnectMessage = $"○ 已断开「{ctl.Name}」。";
                    HardwareLog.Write("[控制器] " + ConnectMessage);
                });
            });
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
            StatusBarService.SetControllerConnecting(true);
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
                    // ★ 真实数量 / 在线数一律在**后台线程**读取：TryGetRealCounts 是硬件查询，
                    //   放到 UI 线程会让「载入工程 → 自动连接」期间页面卡住、点不动。
                    var snap = new List<(AxisControllerItem ctl, int axis, int inIo, int outIo)>();
                    foreach (var c in controllers)
                    {
                        ReadRealCounts(c, out int axis, out int inIo, out int outIo);
                        snap.Add((c, axis, inIo, outIo));
                    }
                    int online = 0;
                    foreach (var c in controllers) if (IsControllerReady(c)) online++;
                    int onlineCount = online, totalCount = controllers.Count;

                    dispatcher.Invoke(() =>
                    {
                        foreach (var s in snap) ApplyRealCounts(s.ctl, s.axis, s.inIo, s.outIo);
                        RaiseConnection();
                        RaiseStatusLines();
                        RaiseTotals();
                        RaiseDetected();
                        StatusBarService.SetControllerStatus(onlineCount, totalCount);   // 纯 UI 更新，不再读硬件
                        ConnectStatusText = "连接完成";
                        ConnectMessage = $"已自动连接控制卡：{onlineCount}/{totalCount} 在线。";
                        StatusBarService.ReportInfo($"已自动连接控制卡：{onlineCount}/{totalCount} 在线。");
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
                    dispatcher.Invoke(() => { IsConnecting = false; StatusBarService.SetControllerConnecting(false); });
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

        // 真实轴 / IO 数量读取拆成两半：ReadRealCounts（纯硬件读，后台线程可调）+
        // ApplyRealCounts（写回绑定属性，UI 线程）。连接成功后的回写口径见 ApplyRealCounts。

        /// <summary>只读硬件、不写任何绑定属性——可安全地在**后台线程**调用（连接时把硬件读数挪出 UI 线程，避免页面卡住）。</summary>
        private static void ReadRealCounts(AxisControllerItem ctl, out int axis, out int inIo, out int outIo)
        {
            axis = 0; inIo = 0; outIo = 0;
            if (HardwareSetup.Mode == HardwareMode.CardFamilies && HardwareSetup.CardFamilies != null)
                HardwareSetup.CardFamilies.TryGetRealCounts(ctl.Name, out axis, out inIo, out outIo);
            else if (HardwareSetup.Mode == HardwareMode.Leadshine)
            {
                var info = LtdmcCard.FirstCard;
                axis = info == null ? 0 : (int)(info.IsBusCard ? info.BusAxes : info.LocalAxes);
            }

            // 空工程 / 只有雷赛控制器的工程默认装配雷赛封装（HardwareSetup 的既有口径），这条路上
            // 压根不会去问卡族层；没插卡时 FirstCard 又是 null → 轴数恒为 0。
            // 所以这里补一次与模式无关的目录查询：该控制器型号命中已移植卡族、且目录里填了设计容量就用它。
            if (axis == 0)
            {
                var fam = WenQiZhiCardBridge.ResolveFamily(ctl);
                if (fam != null)
                {
                    if (fam.AxisCount > 0) axis = fam.AxisCount;
                    if (inIo == 0) inIo = fam.InIoCount;
                    if (outIo == 0) outIo = fam.OutIoCount;
                }
            }
        }

        /// <summary>把后台读到的真实数量写回模型（会触发绑定）——必须在 **UI 线程**调用。</summary>
        private static void ApplyRealCounts(AxisControllerItem ctl, int axis, int inIo, int outIo)
        {
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
            OnPropertyChanged(nameof(HasCardType));        // 切换选中卡时刷新「卡型号 → 扩展IO → 连接」门控
            OnPropertyChanged(nameof(CanConfigureExpansion));
            OnPropertyChanged(nameof(CanConnect));
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

            // ★ 总线类型变了 → 本卡的轴「轴类型」一起跟着改。
            //   轴页的「轴类型」是只读的（它由卡决定），这里就是它唯一的联动入口，否则换过总线类型后没法再纠正。
            if (e.PropertyName == nameof(AxisControllerItem.BusType) && sender is AxisControllerItem busCtl)
                SyncAxisTypes(busCtl);

            // 卡型号变了 → 刷新「扩展 IO / 连接」门控（未选型号时禁用扩展IO与连接）。
            if (e.PropertyName == nameof(AxisControllerItem.CardType))
            {
                OnPropertyChanged(nameof(HasCardType));
                OnPropertyChanged(nameof(CanConfigureExpansion));
                OnPropertyChanged(nameof(CanConnect));
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
