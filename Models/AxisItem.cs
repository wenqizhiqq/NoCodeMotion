using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>轴配置</summary>
    public class AxisItem : EditorItemBase
    {
        private string _axisType = "脉冲";          // 脉冲 / 总线 / EtherCAT
        private int _axisNo;
        private bool _enabled = true;
        private string _homeMode = "原点回零";        // 原点回零 / 限位回零 / 当前位置设零
        private double _speed = 100;
        private double _accel = 50;
        private double _currentPos;

        public string AxisType { get => _axisType; set => SetField(ref _axisType, value); }
        public int AxisNo { get => _axisNo; set => SetField(ref _axisNo, value); }
        public bool Enabled { get => _enabled; set => SetField(ref _enabled, value); }
        public string HomeMode { get => _homeMode; set => SetField(ref _homeMode, value); }
        public double Speed { get => _speed; set => SetField(ref _speed, value); }
        public double Accel { get => _accel; set => SetField(ref _accel, value); }
        public double CurrentPos { get => _currentPos; set => SetField(ref _currentPos, value); }
    }
}
