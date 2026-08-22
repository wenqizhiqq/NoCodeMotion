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

        public string AxisType { get => _axisType; set => SetField(ref _axisType, value); }
        public int AxisNo { get => _axisNo; set => SetField(ref _axisNo, value); }
        public bool Enabled { get => _enabled; set => SetField(ref _enabled, value); }
        public string Unit { get => _unit; set => SetField(ref _unit, value); }

        // 归属的轴控制器（在「轴控制器」页面配置），为空表示未指定
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
    }
}
