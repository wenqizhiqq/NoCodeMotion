// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‌启‏志​◆⁣编⁣写‏◇‌微‌信‎﹕⁠1‏8⁠7⁣◆​1⁣9‏3‎6‏◇‎1‎3⁣9‌9‎　⁠※‌保⁠留⁠所‍有⁣权​利‎请⁠勿⁠删‎除⁣◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>气缸配置——涵盖基本信息、IO 点位、动作参数、安全逻辑与高级设置。</summary>
    public class CylinderItem : EditorItemBase
    {
        // ===================== 基本信息 =====================
        private string _deviceId = string.Empty;       // 设备编号
        private string _type = "双动气缸";               // 气缸类型：单动气缸（1 路输出）/ 双动气缸（2 路输出）
        private string _action = "伸出";               // 默认动作：伸出 / 缩回
        private string _initialState = "缩回";         // 初始状态：伸出 / 缩回
        private string _remark = string.Empty;         // 备注说明

        private string _currentState = "缩回";         // 运行时状态：伸出 / 缩回（列表按钮着色用）

        // ===================== IO 点位 =====================
        private string _outPoint = string.Empty;       // 输出点（关联 IO 名称）
        private string _sensorExtend = string.Empty;   // 伸出到位感应（关联 IO 名称）
        private string _sensorRetract = string.Empty;  // 缩回到位感应（关联 IO 名称）
        private string _sensorType = "NPN";            // 感应类型：NPN / PNP
        private string _backupSensor = string.Empty;   // 缩回输出（第 2 路输出，仅双动气缸用；旧名「备用感应」）

        // ===================== 动作参数 =====================
        private int _delayMs = 200;                    // 动作延时(ms)
        private int _extendMs = 300;                   // 伸出延时(ms)
        private int _retractMs = 300;                  // 缩回延时(ms)
        private int _extendSpeed = 100;                // 伸出速度(%)
        private int _retractSpeed = 100;               // 缩回速度(%)
        private int _toleranceMs = 50;                 // 到位容差(ms)

        // ===================== 安全与逻辑 =====================
        private bool _interlock = false;              // 互锁使能
        private bool _doubleCoil = false;              // 双线圈
        private bool _alarmEnable = true;              // 报警使能
        private bool _manualEnable = true;             // 手动使能
        private int _timeoutMs = 3000;                // 超时时间(ms)：等待到位感应的上限
        private string _timeoutAction = "报警并停止";  // 超时报警方式：报警并停止 / 仅报警 / 忽略

        // ===================== 高级 =====================
        private bool _pulseOutput = false;             // 脉冲输出
        private int _pulseWidthMs = 100;               // 脉冲宽度(ms)
        private string _linkedAxis = string.Empty;     // 关联轴

        // ===================== 基本信息 =====================
        public string DeviceId { get => _deviceId; set => SetField(ref _deviceId, value); }
        public string Type
        {
            get => _type;
            // ★ 在 setter 里归一化：xlsx 载入 / AI 导入 / 模板 / 界面写入四条路径都走属性 setter，
            //   一处归一即全覆盖（旧工程存的是「单作用 / 双作用」，不归一的话类型药丸渲染空白并写回 null）。
            set { if (SetField(ref _type, NormalizeType(value))) OnPropertyChanged(nameof(IsSingleActing)); }
        }

        /// <summary>
        /// 气缸类型归一化：统一成「单动气缸」/「双动气缸」两个受限值。
        /// 类型药丸（ApplePillList）是受限值域下拉 —— 候选外的值渲染空白，所以必须归一。
        /// </summary>
        public static string NormalizeType(string? raw)
        {
            string s = (raw ?? string.Empty).Trim();
            if (s.Length == 0) return "双动气缸";
            if (s.Contains("单动") || s.Contains("单作")) return "单动气缸";
            if (s.Contains("双动") || s.Contains("双作")) return "双动气缸";
            // 只写了「单 / 双」的（旧工程、AI 乱填）也能认；实在认不出的按双动 ——
            // 双动是功能超集，最多多写一路输出，不会漏动作。
            if (s.Contains("单")) return "单动气缸";
            return "双动气缸";
        }

        /// <summary>
        /// 是否单动气缸：只有 1 路输出（<see cref="OutPoint"/> 伸出输出），缩回靠弹簧复位。
        /// 单动气缸不写第 2 路输出（<see cref="BackupSensor"/>），气缸页也会收起「缩回输出」那一行。
        /// </summary>
        public bool IsSingleActing => NormalizeType(_type) == "单动气缸";
        public string Action { get => _action; set => SetField(ref _action, value); }
        public string InitialState { get => _initialState; set => SetField(ref _initialState, value); }
        public string Remark { get => _remark; set => SetField(ref _remark, value); }

        /// <summary>运行时状态（伸出 / 缩回）。列表内联按钮据此着色：当前状态实色高亮、另一状态淡化。
        /// 由 CylinderViewModel 的动作命令（伸出/缩回/复位）更新。</summary>
        public string CurrentState { get => _currentState; set => SetField(ref _currentState, value); }

        // ===================== IO 点位 =====================
        public string OutPoint { get => _outPoint; set => SetField(ref _outPoint, value); }
        public string SensorExtend { get => _sensorExtend; set => SetField(ref _sensorExtend, value); }
        public string SensorRetract { get => _sensorRetract; set => SetField(ref _sensorRetract, value); }
        public string SensorType { get => _sensorType; set => SetField(ref _sensorType, value); }
        public string BackupSensor { get => _backupSensor; set => SetField(ref _backupSensor, value); }

        // ===================== 动作参数 =====================
        public int DelayMs { get => _delayMs; set => SetField(ref _delayMs, value); }
        public int ExtendMs { get => _extendMs; set => SetField(ref _extendMs, value); }
        public int RetractMs { get => _retractMs; set => SetField(ref _retractMs, value); }
        public int ExtendSpeed { get => _extendSpeed; set => SetField(ref _extendSpeed, value); }
        public int RetractSpeed { get => _retractSpeed; set => SetField(ref _retractSpeed, value); }
        public int ToleranceMs { get => _toleranceMs; set => SetField(ref _toleranceMs, value); }

        // ===================== 安全与逻辑 =====================
        public bool Interlock { get => _interlock; set => SetField(ref _interlock, value); }
        public bool DoubleCoil { get => _doubleCoil; set => SetField(ref _doubleCoil, value); }
        public bool AlarmEnable { get => _alarmEnable; set => SetField(ref _alarmEnable, value); }
        public bool ManualEnable { get => _manualEnable; set => SetField(ref _manualEnable, value); }
        public int TimeoutMs { get => _timeoutMs; set => SetField(ref _timeoutMs, value); }
        /// <summary>超时报警方式：报警并停止（默认，抛异常终止流程）/ 仅报警（记报警后继续）/ 忽略（只记日志继续）。</summary>
        public string TimeoutAction { get => _timeoutAction; set => SetField(ref _timeoutAction, value); }

        // ===================== 高级 =====================
        public bool PulseOutput { get => _pulseOutput; set => SetField(ref _pulseOutput, value); }
        public int PulseWidthMs { get => _pulseWidthMs; set => SetField(ref _pulseWidthMs, value); }
        public string LinkedAxis { get => _linkedAxis; set => SetField(ref _linkedAxis, value); }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
