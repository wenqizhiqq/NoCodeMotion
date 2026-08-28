// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Text;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Hardware;
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
                for (int i = 0; i < leadCards; i++)
                {
                    Items.Add(new AxisControllerItem
                    {
                        Kind = "控制卡",
                        Vendor = "雷赛",
                        BusType = "脉冲",
                        CardNo = i,
                        Name = $"控制卡{Items.Count + 1}"
                    });
                    added++;
                }
                sb.AppendLine($"雷赛：检测到 {leadCards} 张控制卡，已登记。");
            }
            else
            {
                sb.AppendLine("雷赛：未检测到控制卡。" + leadStatus);
            }

            // 2) 其它主流厂商：按驱动库（DLL）是否存在识别，驱动在即登记控制器并提示待接入对接
            foreach (var v in CardVendorRegistry.Vendors)
            {
                if (v.Vendor == "雷赛") continue;            // 雷赛已在上面真实扫描
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
