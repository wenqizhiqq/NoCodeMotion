// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‏启⁣志‏◆⁠编‍写⁠◇‏微‎信‍﹕​1‍8‎7‍◆‎1‌9⁠3⁠6‏◇⁠1​3‌9‏9‏　⁣※‏保‎留‏所‍有⁣权‌利​请⁣勿‌删‌除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>轴配置（运动控制参数较全）</summary>
    public class AxisItem : EditorItemBase
    {
        // 基本信息
        private string _axisType = "脉冲";           // 脉冲 / 总线 / EtherCAT / CANopen / 模拟量 / 虚拟轴
        private int _axisNo;
        private bool _enabled = true;
        private string _unit = "mm";                 // mm / ° / 脉冲 / um / 自定义

        // 运动参数
        private double _pulsePerUnit;                // 脉冲当量（每单位脉冲数）
        private double _speed = 100;                 // 运行速度
        private double _accel = 50;                  // 加速度
        private double _decel = 50;                  // 减速度
        private double _jerk;                        // 加加速度

        // 回零参数
        private string _homeMode = "原点开关+限位";  // 原点开关+限位 / 仅正限位 / 仅负限位 / 编码器Z相 / 当前位置设零 / 负向限位回零 / 正向限位回零 / 索引回零
        private double _homeSpeed = 50;              // 回零速度
        private double _creepSpeed = 10;             // 爬行速度
        private double _homeOffset;                  // 原点偏移

        // 限位与保护
        private double _posLimitPlus;                // 软正限位
        private double _posLimitMinus;               // 软负限位
        private double _inPosError = 0.01;           // 到位误差

        // 电平与编码器
        private string _enableLevel = "高电平";      // 高电平 / 低电平
        private string _dirLevel = "正向";           // 正向 / 负向
        private string _alarmLevel = "高电平";       // 高电平 / 低电平 / 无
        private string _encoderType = "无";          // 无 / 增量式 / 绝对式
        private double _encoderRes;                  // 编码器分辨率
        private double _eStopDecel = 200;            // 急停减速

        // ── 以下为「与底层控制卡参数对齐」补齐的字段（对应卡层 CAxisParameter / MotionParamModel / HomeParameModel）──
        // 运动参数
        private double _startVel;                    // 起始速度（卡层 StartVel / MinVel）
        private double _stopVel;                     // 停止速度（卡层 StopVel）
        private string _speedCurve = "梯形";         // 速度类型：梯形 / S型（卡层 JK）
        private double _sPara;                       // S 段（0~1，卡层 SPara，仅 S 型有效）

        // 回零
        private string _homeDir = "负向";            // 回零方向：负向 / 正向（卡层 HomeDir）
        private double _homeTimeoutMs;               // 回零超时 ms，0 = 不判断超时（卡层 HomeOvertime）

        // 限位与保护
        private bool _softLimitEnable;               // 软限位使能（卡层 SoftPELEnable / SoftNELEnable）
        private string _limitLevel = "低电平";        // 限位电平：低电平 / 高电平（卡层 ELLogic）

        // 电平与编码器
        private bool _alarmEnable;                   // 报警使能（卡层 ALMEnable）
        private string _originLevel = "低电平";       // 原点电平：低电平 / 高电平（卡层 OriginLogic）
        private string _originStopMode = "减速停止";  // 原点停止模式：减速停止 / 急停（卡层 ORGMode）
        private bool _encoderEnable = true;          // 编码器使能（卡层 EncodeEnable）
        private double _encoderDeviation = 20;       // 编码器误差（卡层 Deviation，超差应报警）

        // 轴权限（门控「轴状态与控制」表里的手动按钮；卡层 EncodeEnableClearBtn 等为反向语义，这里用正向）
        private bool _allowEnable = true;            // 允许使能
        private bool _allowManual = true;            // 允许 点动 / Jog / 停止
        private bool _allowHome = true;              // 允许回零
        private bool _allowSetZero = true;           // 允许设零点

        // 手动调试参数（「轴状态与控制」表用）
        private double _jogStep = 1;                 // 点动距离（正负决定方向）
        private double _manualSpeed = 20;            // 手动速度（点动 / Jog 前下发到卡）

        public string AxisType { get => _axisType; set => SetField(ref _axisType, value); }
        public int AxisNo { get => _axisNo; set => SetField(ref _axisNo, value); }
        public bool Enabled { get => _enabled; set => SetField(ref _enabled, value); }
        public string Unit { get => _unit; set => SetField(ref _unit, value); }

        // 归属的控制器（在「控制器」页面配置），为空表示未指定
        private string _controller = string.Empty;
        public string Controller { get => _controller; set => SetField(ref _controller, value); }

        public double PulsePerUnit { get => _pulsePerUnit; set => SetField(ref _pulsePerUnit, value); }
        public double Speed { get => _speed; set => SetField(ref _speed, value); }
        public double Accel { get => _accel; set => SetField(ref _accel, value); }
        public double Decel { get => _decel; set => SetField(ref _decel, value); }
        public double Jerk { get => _jerk; set => SetField(ref _jerk, value); }

        public string HomeMode { get => _homeMode; set => SetField(ref _homeMode, value); }
        public double HomeSpeed { get => _homeSpeed; set => SetField(ref _homeSpeed, value); }
        public double CreepSpeed { get => _creepSpeed; set => SetField(ref _creepSpeed, value); }
        public double HomeOffset { get => _homeOffset; set => SetField(ref _homeOffset, value); }

        public double PosLimitPlus { get => _posLimitPlus; set => SetField(ref _posLimitPlus, value); }
        public double PosLimitMinus { get => _posLimitMinus; set => SetField(ref _posLimitMinus, value); }
        public double InPosError { get => _inPosError; set => SetField(ref _inPosError, value); }

        public string EnableLevel { get => _enableLevel; set => SetField(ref _enableLevel, value); }
        public string DirLevel { get => _dirLevel; set => SetField(ref _dirLevel, value); }
        public string AlarmLevel { get => _alarmLevel; set => SetField(ref _alarmLevel, value); }
        public string EncoderType { get => _encoderType; set => SetField(ref _encoderType, value); }
        public double EncoderRes { get => _encoderRes; set => SetField(ref _encoderRes, value); }
        public double EStopDecel { get => _eStopDecel; set => SetField(ref _eStopDecel, value); }

        // ── 补齐的底层参数 ──
        public double StartVel { get => _startVel; set => SetField(ref _startVel, value); }
        public double StopVel { get => _stopVel; set => SetField(ref _stopVel, value); }
        public string SpeedCurve { get => _speedCurve; set => SetField(ref _speedCurve, value); }
        public double SPara { get => _sPara; set => SetField(ref _sPara, value); }

        public string HomeDir { get => _homeDir; set => SetField(ref _homeDir, value); }
        public double HomeTimeoutMs { get => _homeTimeoutMs; set => SetField(ref _homeTimeoutMs, value); }

        public bool SoftLimitEnable { get => _softLimitEnable; set => SetField(ref _softLimitEnable, value); }
        public string LimitLevel { get => _limitLevel; set => SetField(ref _limitLevel, value); }

        public bool AlarmEnable { get => _alarmEnable; set => SetField(ref _alarmEnable, value); }
        public string OriginLevel { get => _originLevel; set => SetField(ref _originLevel, value); }
        public string OriginStopMode { get => _originStopMode; set => SetField(ref _originStopMode, value); }
        public bool EncoderEnable { get => _encoderEnable; set => SetField(ref _encoderEnable, value); }
        public double EncoderDeviation { get => _encoderDeviation; set => SetField(ref _encoderDeviation, value); }

        public bool AllowEnable { get => _allowEnable; set => SetField(ref _allowEnable, value); }
        public bool AllowManual { get => _allowManual; set => SetField(ref _allowManual, value); }
        public bool AllowHome { get => _allowHome; set => SetField(ref _allowHome, value); }
        public bool AllowSetZero { get => _allowSetZero; set => SetField(ref _allowSetZero, value); }

        public double JogStep { get => _jogStep; set => SetField(ref _jogStep, value); }
        public double ManualSpeed { get => _manualSpeed; set => SetField(ref _manualSpeed, value); }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
