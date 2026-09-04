// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Vision;

namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 相机页面 ViewModel。
    ///
    /// 左侧：EditorPage 容器接管 AddCommand / DeleteCommand / RenameCommand / Items / SelectedItem
    ///       五个契约属性/命令（与 ListEditorViewModel 同形），所以本 VM 自身就充当数据源。
    /// 右侧（Detail）：属性编辑 + 拍照预览占位。
    ///
    /// 注：相机 SDK（海康 MVS / 大华 / Basler Pylon / OpenCV）暂未接入，所有 IsConnected / Capture
    ///     仅做 UI 状态切换；接入 SDK 后只替换 Connect / Capture 内部实现。
    /// </summary>
    public class CameraViewModel : INotifyPropertyChanged
    {
        /// <summary>相机项集合直接指向 ProjectStore.Data.Cameras，让模板填充与跨页面共享保持一致。</summary>
        public ObservableCollection<CameraItem> Items => NoCodeMotion.Services.ProjectStore.Data.Cameras;

        private CameraItem? _selectedItem;
        public CameraItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (Set(ref _selectedItem, value))
                {
                    RaiseCanExecutes();
                    OnPropertyChanged(nameof(HasSelection));
                }
            }
        }

        public bool HasSelection => SelectedItem is not null;

        private string _statusMessage = "未连接";
        public string StatusMessage
        {
            get => _statusMessage;
            set => Set(ref _statusMessage, value);
        }

        public ICommand AddCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand CaptureCommand { get; }

        /// <summary>触发模式可选项（连续 / 软触发 / 硬触发），绑定药丸选择器。</summary>
        public string[] TriggerModeOptions { get; } = { "连续", "软触发", "硬触发" };

        /// <summary>一键应用常用拍摄参数（曝光 10ms / 增益 1.0 / 连续触发），便于快速标定。</summary>
        public ICommand ApplyCommonParamsCommand { get; }

        public CameraViewModel()
        {
            AddCommand = new RelayCommand(_ => Add());
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedItem is not null);
            RenameCommand = new RelayCommand(_ => Rename(), _ => SelectedItem is not null);
            ConnectCommand = new RelayCommand(_ => Connect(), _ => SelectedItem is not null);
            CaptureCommand = new RelayCommand(_ => Capture(), _ => SelectedItem is not null);
            ApplyCommonParamsCommand = new RelayCommand(_ => ApplyCommonParams(), _ => SelectedItem is not null);
        }

        private void Add()
        {
            var item = new CameraItem { Name = $"相机{Items.Count + 1}" };
            Items.Add(item);
            SelectedItem = item;
            StatusMessage = $"已添加 {item.Name}";
        }

        private void Delete()
        {
            if (SelectedItem is null) return;
            var name = SelectedItem.Name;
            var idx = Items.IndexOf(SelectedItem);
            Items.Remove(SelectedItem);
            SelectedItem = Items.Count == 0
                ? null
                : Items[Math.Min(idx, Items.Count - 1)];
            StatusMessage = $"已删除 {name}";
        }

        private void Rename()
        {
            // 真实重命名弹窗由 EditorPage 内置接管；此处只给状态提示，避免误用。
            StatusMessage = "请使用顶部「重命名」按钮";
        }

        private void Connect()
        {
            if (SelectedItem is null) return;
            SelectedItem.IsConnected = !SelectedItem.IsConnected;
            StatusMessage = SelectedItem.IsConnected
                ? $"{SelectedItem.Name} 已连接（占位，待接 SDK）"
                : $"{SelectedItem.Name} 已断开";
        }

        private void Capture()
        {
            if (SelectedItem is null) return;
            int idx = Items.IndexOf(SelectedItem);
            // 仿真取像：真实 SDK 未接入时由 VisionSimCapture 产出伪检测结果，
            // 与 FlowRunnerService 的「相机」步骤走同一仿真回退路径，保证页面与流程一致。
            var det = VisionSimCapture.Detect(idx);
            SelectedItem.LastResult = $"中心 ({det.X:0.0},{det.Y:0.0})";
            SelectedItem.LastScore = det.Score;
            SimRuntime.FlashCamera(SelectedItem.Name);
            SimRuntime.SetVariable($"CamResult{idx}", det.Score);
            StatusMessage = $"{SelectedItem.Name} 取像完成，匹配分数 {det.Score:0.00}";
        }

        private void ApplyCommonParams()
        {
            if (SelectedItem is null) return;
            SelectedItem.ExposureMs = 10.0;
            SelectedItem.Gain = 1.0;
            SelectedItem.TriggerMode = "连续";
            StatusMessage = $"{SelectedItem.Name} 已应用常用参数：曝光 10ms / 增益 1.0 / 连续触发";
        }

        private void RaiseCanExecutes()
        {
            // 项目自带 RelayCommand 未暴露 RaiseCanExecuteChanged；用 WPF 全局 InvalidateRequerySuggested
            // 通知所有命令重新评估 CanExecute，副作用是项目里其他 VM 也会被一起刷新，本场景可接受。
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
