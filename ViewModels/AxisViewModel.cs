// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启‏志‌◆​编‌写‏◇​微​信‎﹕‍1​8‍7‏◆‏1​9‍3⁣6​◇⁠1‌3‏9​9​　⁠※⁠保‍留‌所‎有​权‌利‏请‏勿‍删​除​◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Hardware;
using NoCodeMotion.Services.Hardware.Cards;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.ViewModels
{
    public class AxisViewModel : ListEditorViewModel<AxisItem>, IEnsureDefaultSelection
    {
        public AxisViewModel()
        {
            CatalogCategory = "Axis";
            Items = ProjectStore.Data.Axes;
            Counter = Items.Count;
            AttachAutoSave();
            AxisControllerViewModel.DetectedCountsChanged += OnDetectedCountsChanged;
        }

        /// <summary>已连接控制卡从底层硬件真实检测到的轴数量之和（连接后自动获取，显示在轴页面顶部）。</summary>
        public int DetectedAxisCount
        {
            get
            {
                int sum = 0;
                var ctls = ProjectStore.Data?.Controllers;
                if (ctls == null) return 0;
                foreach (var c in ctls)
                    if (IsControllerReadyNow(c.Name)) sum += c.DetectedAxisCount;
                return sum;
            }
        }

        private static bool IsControllerReadyNow(string name)
        {
            if (HardwareSetup.Mode == HardwareMode.Leadshine) return HardwareSetup.IsCardReady;
            var b = HardwareBridge.Current as SamsunCardBridge;
            return b != null && b.IsControllerReady(name);
        }

        private void OnDetectedCountsChanged(object? sender, EventArgs e)
            => OnPropertyChanged(nameof(DetectedAxisCount));

        protected override AxisItem CreateNewItem() => new AxisItem { Name = $"轴{Counter + 1}" };

        /// <summary>配置页改值实时下发设备：速度变化下发到卡，使能/电平变化重新使能轴。</summary>
        protected override void PushItem(AxisItem item, string? propertyName)
        {
            var bridge = HardwareBridge.Current;
            if (propertyName == nameof(AxisItem.Speed))
                bridge.SetAxisSpeed(item, item.Speed);
            else if (propertyName == nameof(AxisItem.Enabled) || propertyName == nameof(AxisItem.EnableLevel))
                bridge.EnableAxis(item);
        }

        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0) SelectedItem = Items[0];
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
