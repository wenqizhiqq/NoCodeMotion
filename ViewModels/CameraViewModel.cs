// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启‏志‌◆‍编⁠写⁠◇‎微‏信⁠﹕⁣1⁣8‏7‌◆⁣1⁣9‍3‌6‎◇‍1​3‍9⁣9‌　​※‏保‏留⁠所​有‏权‌利‎请‍勿​删‍除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Camera;
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

        private ImageSource? _preview;
        /// <summary>最近一帧预览图（真实取像；无相机时为仿真合成图）。</summary>
        public ImageSource? Preview
        {
            get => _preview;
            private set
            {
                if (Set(ref _preview, value)) OnPropertyChanged(nameof(HasPreview));
            }
        }

        /// <summary>是否已有可显示的预览帧（控制占位提示的显隐）。</summary>
        public bool HasPreview => _preview != null;

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

        /// <summary>枚举在线相机并更新左侧列表（顺序与 MVS 枚举一致）。</summary>
        public ICommand SearchDevicesCommand { get; }

        public CameraViewModel()
        {
            AddCommand = new RelayCommand(_ => Add());
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedItem is not null);
            RenameCommand = new RelayCommand(_ => Rename(), _ => SelectedItem is not null);
            ConnectCommand = new RelayCommand(_ => Connect(), _ => SelectedItem is not null);
            CaptureCommand = new RelayCommand(_ => Capture(), _ => SelectedItem is not null);
            ApplyCommonParamsCommand = new RelayCommand(_ => ApplyCommonParams(), _ => SelectedItem is not null);
            SearchDevicesCommand = new RelayCommand(_ => SearchDevices());
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

        /// <summary>连接 / 断开真实相机：连接 = 打开设备 + 抓第一帧做预览。</summary>
        private void Connect()
        {
            if (SelectedItem is null) return;
            int idx = Items.IndexOf(SelectedItem);

            var dev = ResolveDevice(idx);
            if (dev is null)
            {
                SelectedItem.IsConnected = false;
                StatusMessage = MvsCameraService.LastError is { Length: > 0 } err ? $"无法连接：{err}" : "无法连接：未发现相机。";
                return;
            }

            if (MvsCameraService.IsOpen(dev.Key))
            {
                MvsCameraService.Close(dev.Key);
                SelectedItem.IsConnected = false;
                StatusMessage = $"{SelectedItem.Name} 已断开（{dev.Display}）";
                return;
            }

            if (MvsCameraService.TryGrabBgra(dev, SettingsOf(SelectedItem),
                                             out var bgra, out int w, out int h, out var grabErr))
            {
                Preview = ToBitmap(bgra, w, h);
                SelectedItem.IsConnected = true;
                SelectedItem.LastResult = $"{w}×{h} 真实取像";
                StatusMessage = $"{SelectedItem.Name} 已连接（{dev.Display}）";
            }
            else
            {
                SelectedItem.IsConnected = false;
                StatusMessage = $"连接失败：{grabErr}";
            }
        }

        /// <summary>把工程里的相机索引解析成真实设备（无 MVS 运行库时返回 null）。</summary>
        private static MvsCameraDevice? ResolveDevice(int index)
        {
            if (!MvsCameraService.IsRuntimeAvailable) return null;
            var devices = MvsCameraService.Enumerate();
            return devices.Count == 0 ? null : MvsCameraService.Match(devices, null, index);
        }

        private static MvsCameraSettings SettingsOf(CameraItem it) => new MvsCameraSettings
        {
            ExposureMs = it.ExposureMs > 0 ? it.ExposureMs : 10.0,
            Gain = it.Gain > 0 ? it.Gain : 1.0,
            TriggerMode = string.IsNullOrWhiteSpace(it.TriggerMode) ? "连续" : it.TriggerMode
        };

        /// <summary>BGRA 字节 → WPF 位图（Freeze 后可安全跨线程绑定）。</summary>
        private static ImageSource? ToBitmap(byte[]? bgra, int w, int h)
        {
            if (bgra is null || w <= 0 || h <= 0 || bgra.Length < w * h * 4) return null;
            var bmp = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, bgra, w * 4);
            bmp.Freeze();
            return bmp;
        }

        /// <summary>枚举在线相机并更新列表；保留用户改过的名称，只刷新技术字段。</summary>
        private void SearchDevices()
        {
            if (!MvsCameraService.IsRuntimeAvailable)
            {
                StatusMessage = MvsCameraService.LastError is { Length: > 0 } e ? e : "MVS 运行库不可用。";
                return;
            }

            var devices = MvsCameraService.Enumerate();
            if (devices.Count == 0)
            {
                StatusMessage = MvsCameraService.LastError is { Length: > 0 } e ? e : "未发现相机。";
                return;
            }

            for (int i = 0; i < devices.Count; i++)
            {
                var d = devices[i];
                var desc = d.Model + (d.Serial.Length > 0 ? " / " + d.Serial : string.Empty);
                if (i < Items.Count)
                {
                    var it = Items[i];
                    if (d.Ip.Length > 0) it.IpAddress = d.Ip;
                    it.Vendor = "海康威视";
                    if (desc.Length > 0) it.Description = desc;
                }
                else
                {
                    Items.Add(new CameraItem
                    {
                        Name = d.UserName.Length > 0 ? d.UserName
                             : d.Model.Length > 0 ? d.Model : $"相机{Items.Count + 1}",
                        Vendor = "海康威视",
                        IpAddress = d.Ip,
                        Description = desc
                    });
                }
            }

            if (SelectedItem is null && Items.Count > 0) SelectedItem = Items[0];
            StatusMessage = $"发现 {devices.Count} 台相机（列表顺序与枚举一致）。";
        }

        /// <summary>拍照：优先真实取像，失败/无相机则回退仿真（页面与流程行为一致）。</summary>
        private void Capture()
        {
            if (SelectedItem is null) return;
            int idx = Items.IndexOf(SelectedItem);

            // 先声明再传 out：&& 短路时 out var 不保证赋值，会触发 CS0165。
            string grabErr = string.Empty;
            var dev = ResolveDevice(idx);
            if (dev is not null
                && MvsCameraService.TryGrabBgra(dev, SettingsOf(SelectedItem),
                                                out var bgra, out int w, out int h, out grabErr))
            {
                Preview = ToBitmap(bgra, w, h);
                SelectedItem.IsConnected = true;
                SelectedItem.LastResult = $"{w}×{h} 真实取像";
                // 检测算法尚未接入：分数仍沿用仿真值，避免下游 {CamResultN} 变量语义变化。
                var d0 = VisionSimCapture.Detect(idx);
                SelectedItem.LastScore = d0.Score;
                SimRuntime.FlashCamera(SelectedItem.Name);
                SimRuntime.SetVariable($"CamResult{idx}", d0.Score);
                StatusMessage = $"{SelectedItem.Name} 真实取像 {w}×{h}（{dev.Display}）";
                return;
            }

            // 回退：无真实相机 / 取像失败 → 合成图
            var det = VisionSimCapture.Detect(idx);
            var sim = VisionSimCapture.Capture(idx, out int sw, out int sh);
            Preview = ToBitmap(sim, sw, sh);
            SelectedItem.LastResult = $"仿真 {sw}×{sh}";
            SelectedItem.LastScore = det.Score;
            SimRuntime.FlashCamera(SelectedItem.Name);
            SimRuntime.SetVariable($"CamResult{idx}", det.Score);
            StatusMessage = dev is null
                ? $"{SelectedItem.Name} 仿真取像 {sw}×{sh}（未发现真实相机）"
                : $"{SelectedItem.Name} 真实取像失败：{grabErr}，已回退仿真";
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
