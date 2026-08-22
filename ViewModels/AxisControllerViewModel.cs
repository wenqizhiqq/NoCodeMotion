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
            // 先释放旧句柄，再强制重新扫描（插好卡 / 装好驱动后可点此重连）
            LtdmcCard.Close();
            var status = HardwareSetup.Reconnect();
            var count = LtdmcCard.CardCount;

            if (count > 0)
            {
                int added = 0;
                for (int i = 0; i < count; i++)
                {
                    var name = $"控制卡{Items.Count + 1}";
                    Items.Add(new AxisControllerItem
                    {
                        Kind = "控制卡",
                        Vendor = "雷赛",
                        CardNo = i,
                        Name = name
                    });
                    added++;
                }
                Counter = Items.Count;
                if (Items.Count > 0) SelectedItem = Items[Items.Count - 1];
                DetectMessage = $"自动识别完成：检测到 {count} 张控制卡，已添加 {added} 个控制器。";
            }
            else
            {
                DetectMessage = "未检测到控制卡。" + status;
            }
            HardwareLog.Write("[硬件识别] " + DetectMessage);
        }

        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0) SelectedItem = Items[0];
        }
    }
}
