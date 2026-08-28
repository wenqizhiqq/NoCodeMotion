// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
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
        private string _type = "双作用";               // 气缸类型：单作用 / 双作用
        private string _action = "伸出";               // 默认动作：伸出 / 缩回
        private string _initialState = "缩回";         // 初始状态：伸出 / 缩回
        private string _remark = string.Empty;         // 备注说明

        // ===================== IO 点位 =====================
        private string _outPoint = string.Empty;       // 输出点（关联 IO 名称）
        private string _sensorExtend = string.Empty;   // 伸出到位感应（关联 IO 名称）
        private string _sensorRetract = string.Empty;  // 缩回到位感应（关联 IO 名称）
        private string _sensorType = "NPN";            // 感应类型：NPN / PNP
        private string _backupSensor = string.Empty;   // 备用感应点

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
        private int _timeoutMs = 3000;                // 动作超时(ms)

        // ===================== 高级 =====================
        private bool _pulseOutput = false;             // 脉冲输出
        private int _pulseWidthMs = 100;               // 脉冲宽度(ms)
        private string _linkedAxis = string.Empty;     // 关联轴

        // ===================== 基本信息 =====================
        public string DeviceId { get => _deviceId; set => SetField(ref _deviceId, value); }
        public string Type { get => _type; set => SetField(ref _type, value); }
        public string Action { get => _action; set => SetField(ref _action, value); }
        public string InitialState { get => _initialState; set => SetField(ref _initialState, value); }
        public string Remark { get => _remark; set => SetField(ref _remark, value); }

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

        // ===================== 高级 =====================
        public bool PulseOutput { get => _pulseOutput; set => SetField(ref _pulseOutput, value); }
        public int PulseWidthMs { get => _pulseWidthMs; set => SetField(ref _pulseWidthMs, value); }
        public string LinkedAxis { get => _linkedAxis; set => SetField(ref _linkedAxis, value); }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
