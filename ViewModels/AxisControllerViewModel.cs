// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‏启‏志‌◆‍编‎写‌◇‌微‎信⁠﹕‌1‍8‍7⁣◆⁣1‍9‍3⁠6⁣◇‌1‌3‎9​9‏　‍※‎保‌留‎所‍有‍权⁣利‎请‎勿⁠删‌除‍◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Text;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Hardware;
using NoCodeMotion.Services.Hardware.Cards;
using NoCodeMotion.Services.Hardware.Leadshine;

namespace NoCodeMotion.ViewModels
{
    /// <summary>控制器页面：增删改控制器实例（控制卡 / 扩展IO），供轴页面选择归属。</summary>
    public class AxisControllerViewModel : ListEditorViewModel<AxisControllerItem>, IEnsureDefaultSelection
    {
        public AxisControllerViewModel()
        {
            CatalogCategory = "Controller";
            Items = ProjectStore.Data.Controllers;
            Counter = Items.Count;
            AttachAutoSave();
        }

        protected override AxisControllerItem CreateNewItem() => new AxisControllerItem { Kind = "控制卡", Name = $"控制卡{Counter + 1}" };

        /// <summary>添加一张控制卡。</summary>
        public ICommand AddCardCommand => new RelayCommand(_ => AddItem("控制卡", "控制卡"));

        /// <summary>添加一个扩展IO模块。</summary>
        public ICommand AddExpansionIoCommand => new RelayCommand(_ => AddItem("扩展IO", "扩展IO"));

        private void AddItem(string kind, string namePrefix)
        {
            var item = new AxisControllerItem { Kind = kind, Name = $"{namePrefix}{Counter + 1}" };
            Counter++;
            Items.Add(item); // 触发 OnItemsChanged -> 订阅 + 保存
            SelectedItem = item;
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
