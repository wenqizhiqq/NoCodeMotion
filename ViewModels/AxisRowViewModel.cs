// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Hardware;

namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 「轴状态与控制」表里的一行 = 一个轴。
    ///
    ///   左侧（只读）：正极限 / 负极限 / 原点 / 急停 / 报警 / 使能 / 运动中 / 位置 / 编码器 —— 全部来自底层真实读数；
    ///   中间（可设置）：点动距离 / 手动速度 —— 落盘到工程；
    ///   右侧（操作）：使能 / 点动 −+ / Jog −+ / 回零 / 停止 / 设零点 —— 真实下发到控制卡。
    ///
    /// ★ 所有硬件调用都在后台线程执行，UI 线程只做赋值与状态栏提示（否则会卡界面）。
    /// </summary>
    public sealed class AxisRowViewModel : ViewModelBase
    {
        private readonly AxisItem _item;
        private AxisStatusSnapshot _snap = new AxisStatusSnapshot();

        public AxisRowViewModel(AxisItem item)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));

            // 「使能」是唯一的使能动作（「上电使能」那项是工程配置，不是按钮权限），所以不设 CanExecute。
            EnableCommand = new RelayCommand(_ => Invoke("使能", () => AxisMonitorService.Enable(_item)));
            StopCommand = new RelayCommand(_ => Invoke("停止", () => AxisMonitorService.Stop(_item)), _ => _item.AllowManual);
            HomeCommand = new RelayCommand(_ => Invoke("回零", () => AxisMonitorService.Home(_item)), _ => _item.AllowHome);
            SetZeroCommand = new RelayCommand(_ => SetZero(), _ => _item.AllowSetZero);
            InchMinusCommand = new RelayCommand(_ => Inch(-1), _ => _item.AllowManual);
            InchPlusCommand = new RelayCommand(_ => Inch(+1), _ => _item.AllowManual);
            JogMinusStartCommand = new RelayCommand(_ => StartJog(false), _ => _item.AllowManual);
            JogPlusStartCommand = new RelayCommand(_ => StartJog(true), _ => _item.AllowManual);
            JogStopCommand = new RelayCommand(_ => Invoke("停止", () => AxisMonitorService.Stop(_item)), _ => _item.AllowManual);

            _item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AxisItem.Name)) OnPropertyChanged(nameof(Name));
                else if (e.PropertyName != null && e.PropertyName.StartsWith("Allow", StringComparison.Ordinal))
                {
                    OnPropertyChanged(nameof(PermissionText));
                    CommandManager.InvalidateRequerySuggested();
                }
            };
        }

        /// <summary>对应的轴配置对象（命令下发时用它解析轴号 / 控制器）。</summary>
        public AxisItem Item => _item;

        public string Name => string.IsNullOrWhiteSpace(_item.Name) ? "（未命名轴）" : _item.Name;
        public string ControllerText => string.IsNullOrWhiteSpace(_item.Controller) ? "未指定控制器" : _item.Controller;

        /// <summary>轴权限摘要（哪个按钮被允许），显示在控制容器里，避免操作员对着灰按钮猜。</summary>
        public string PermissionText
        {
            get
            {
                var parts = new List<string>();
                if (_item.AllowManual) parts.Add("点动/Jog/停止");
                if (_item.AllowHome) parts.Add("回零");
                if (_item.AllowSetZero) parts.Add("设零点");
                return parts.Count == 0 ? "全部禁止（在「轴权限」里开启）" : "允许：" + string.Join("、", parts);
            }
        }

        // ===================== 可设置：点动距离 / 手动速度 =====================

        public double JogStep
        {
            get => _item.JogStep;
            set { if (_item.JogStep != value) { _item.JogStep = value; OnPropertyChanged(); } }
        }

        public double ManualSpeed
        {
            get => _item.ManualSpeed;
            set { if (_item.ManualSpeed != value) { _item.ManualSpeed = value; OnPropertyChanged(); } }
        }

        // ===================== 只读实时状态 =====================

        /// <summary>由 <see cref="AxisViewModel"/> 在后台读完状态后回 UI 线程赋值。</summary>
        public AxisStatusSnapshot Snapshot
        {
            get => _snap;
            set { _snap = value ?? new AxisStatusSnapshot(); RaiseStatus(); }
        }

        public bool Connected => _snap.Connected;
        public string StatusText => _snap.Message;
        public string PosText => _snap.PosText;
        public string EncText => _snap.EncText;
        public string MovingText => _snap.MovingText;
        public string PelText => _snap.PelText;
        public string MelText => _snap.MelText;
        public string OrgText => _snap.OrgText;
        public string EmgText => _snap.EmgText;
        public string AlarmText => _snap.AlarmText;
        public string EnabledText => _snap.EnabledText;
        public string AlarmCodeText => _snap.AlarmCodeText;
        public string IoWordText => _snap.HasIoWord ? $"0x{_snap.IoWord:X}" : "—";
        public string IoWordTip => AxisMonitorService.IoWordHint;

        // 供 XAML DataTrigger 上色（有信号 = 亮，无 = 暗）
        public bool PelOn => _snap.Pel == true;
        public bool MelOn => _snap.Mel == true;
        public bool OrgOn => _snap.Org == true;
        public bool EmgOn => _snap.Emg == true;
        public bool AlarmOn => _snap.Alarm == true;
        public bool EnabledOn => _snap.Enabled == true;
        public bool Moving => _snap.Moving;

        // 颜色（十六进制字符串；WPF 绑定到 Brush 时会自动转换，与状态栏 RunColor/ControllerColor 同一套做法）
        private const string ColOff = "#94A3B8";
        private const string ColRed = "#DC2626";
        private const string ColAmber = "#D97706";
        private const string ColBlue = "#1D4ED8";
        private const string ColGreen = "#16A34A";

        public string PelColor => SigColor(_snap.Pel, ColAmber);
        public string MelColor => SigColor(_snap.Mel, ColAmber);
        public string OrgColor => SigColor(_snap.Org, ColBlue);
        public string EmgColor => SigColor(_snap.Emg, ColRed);
        public string AlarmColor => SigColor(_snap.Alarm, ColRed);
        public string EnabledColor => SigColor(_snap.Enabled, ColGreen);
        public string MovingColor => !_snap.Connected ? ColOff : (_snap.Moving ? ColBlue : ColOff);
        public string PosColor => _snap.Connected ? "#0F172A" : ColOff;

        private static string SigColor(bool? v, string on) => v == null ? ColOff : (v.Value ? on : ColOff);

        private void RaiseStatus()
        {
            foreach (var p in new[]
            {
                nameof(Connected), nameof(StatusText), nameof(PosText), nameof(EncText), nameof(MovingText),
                nameof(PelText), nameof(MelText), nameof(OrgText), nameof(EmgText), nameof(AlarmText),
                nameof(EnabledText), nameof(IoWordText), nameof(IoWordTip), nameof(AlarmCodeText),
                nameof(PelOn), nameof(MelOn), nameof(OrgOn), nameof(EmgOn), nameof(AlarmOn),
                nameof(EnabledOn), nameof(Moving),
                nameof(PelColor), nameof(MelColor), nameof(OrgColor), nameof(EmgColor),
                nameof(AlarmColor), nameof(EnabledColor), nameof(MovingColor), nameof(PosColor),
            }) OnPropertyChanged(p);
        }

        // ===================== 命令 =====================

        public ICommand EnableCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand HomeCommand { get; }
        public ICommand SetZeroCommand { get; }
        public ICommand InchMinusCommand { get; }
        public ICommand InchPlusCommand { get; }
        public ICommand JogMinusStartCommand { get; }
        public ICommand JogPlusStartCommand { get; }
        public ICommand JogStopCommand { get; }

        // ===================== 实现（全部后台线程） =====================

        /// <summary>后台执行一个「下发类」动作，完成 / 失败都回报状态栏。</summary>
        private async void Invoke(string action, Action work)
        {
            if (NotConnected(action)) return;
            string name = Name;
            await Task.Run(() =>
            {
                try
                {
                    work();
                    Report($"轴「{name}」{action} 已下发。", false);
                }
                catch (Exception ex)
                {
                    Report($"轴「{name}」{action} 失败：{ex.Message}", true);
                }
            });
        }

        /// <summary>
        /// 手动按钮的前置检查：没连控制卡就直接说清楚，**别对着桩/log 谎报「已下发」**。
        /// </summary>
        private bool NotConnected(string action)
        {
            if (HardwareSetup.Mode != HardwareMode.Simulation && HardwareSetup.IsCardReady) return false;
            Report($"轴「{Name}」{action} 未执行：控制器未连接（先到「控制器」页点「连接控制器」）。", true);
            return true;
        }

        /// <summary>
        /// 点动 / Jog 动作后追加的「为什么可能不动」提醒（**只提醒、不拦截**）。
        /// 现场「指令下发成功但轴不动」九成是这两条：① 有报警未清除 ② 轴未使能。
        /// 只有明确读到才提醒；读不到（null）不打扰。
        /// </summary>
        private string MoveHint()
        {
            if (!_snap.Connected) return string.Empty;
            var parts = new List<string>();
            if (_snap.HasAlarm)
                parts.Add(_snap.AlarmCode != 0
                    ? $"该轴有报警（码 {_snap.AlarmCode}），报警未清除前轴通常不会动，请先排除并清报警"
                    : "该轴报警信号有效，未清除前轴通常不会动");
            if (_snap.Enabled == false) parts.Add("状态显示该轴未使能，若不动请先点「使能」");
            return parts.Count == 0 ? string.Empty : "（注意：" + string.Join("；", parts) + "）";
        }

        /// <summary>点动一段距离（正负决定方向）。「手动速度」作为本次点动速度下发。</summary>
        private async void Inch(int dir)
        {
            double step = Math.Abs(JogStep);
            if (step <= 0)
            {
                Report($"轴「{Name}」点动距离为 0，请先填写「点动距离」。", true);
                return;
            }
            if (NotConnected("点动")) return;
            string name = Name;
            double speed = ManualSpeed;
            double delta = step * dir;
            string hint = MoveHint();
            await Task.Run(() =>
            {
                string err = AxisMonitorService.Inch(_item, delta, speed);
                if (string.IsNullOrEmpty(err))
                    Report($"轴「{name}」点动 {(dir > 0 ? "+" : "−")}{step}{hint}", false);
                else
                    Report($"轴「{name}」点动失败：{err}", true);
            });
        }

        /// <summary>启动连续 Jog（按住走、松开停）。「手动速度」作为本次 Jog 速度下发。</summary>
        private async void StartJog(bool positive)
        {
            if (NotConnected("Jog")) return;
            string name = Name;
            double speed = ManualSpeed;
            string hint = MoveHint();
            await Task.Run(() =>
            {
                string err = AxisMonitorService.StartJog(_item, positive, speed);
                if (string.IsNullOrEmpty(err))
                    Report($"轴「{name}」Jog {(positive ? "正向" : "反向")} 已启动 —— Jog 是「按住才走」，松开即停；只点一下就松开只会走一瞬（要按「点动距离」走一步请用「点动 −/+」）。{hint}", false);
                else
                    Report($"轴「{name}」Jog 失败：{err}", true);
            });
        }

        /// <summary>把当前指令位置置零。</summary>
        private async void SetZero()
        {
            if (NotConnected("设零点")) return;
            string name = Name;
            await Task.Run(() =>
            {
                string err = AxisMonitorService.SetZero(_item);
                if (string.IsNullOrEmpty(err))
                    Report($"轴「{name}」当前位置已置零。", false);
                else
                    Report($"轴「{name}」设零点失败：{err}", true);
            });
        }

        /// <summary>状态栏提示：从后台线程投回 UI 线程再报，避免跨线程刷 UI。</summary>
        private static void Report(string message, bool isError)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;
            app.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (isError) StatusBarService.ReportException(message);
                else StatusBarService.ReportInfo(message);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
