// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‏启‏志‌◆‍编‎写‌◇‌微‎信⁠﹕‌1‍8‍7⁣◆⁣1‍9‍3⁠6⁣◇‌1‌3‎9​9‏　‍※‎保‌留‎所‍有‍权⁣利‎请‎勿⁠删‌除‍◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
        }

        protected override AxisControllerItem CreateNewItem() => new AxisControllerItem { Kind = "控制卡", Name = $"控制卡{Counter + 1}" };

        /// <summary>添加一张控制卡。</summary>
        public ICommand AddCardCommand => new RelayCommand(_ => AddCard());

        private void AddCard()
        {
            var item = CreateNewItem();
            Counter++;
            Items.Add(item); // 触发 OnItemsChanged -> 订阅 + 保存
            SelectedItem = item;
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
        }

        private void OnTrackedCardChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AxisControllerItem.InIoCount)
                || e.PropertyName == nameof(AxisControllerItem.OutIoCount)
                || e.PropertyName == nameof(AxisControllerItem.AxisCount))
                RaiseTotals();
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
            //    只探库、不初始化卡 —— 真正初始化推迟到首次轴 / IO 动作时懒加载（见 SamsunCardBridge），
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
