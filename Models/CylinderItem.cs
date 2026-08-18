using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>气缸配置</summary>
    public class CylinderItem : EditorItemBase
    {
        private string _outPoint = string.Empty;     // 输出点（关联 IO 名称，来自 IO 库）
        private string _sensorExtend = string.Empty;  // 伸出到位感应（关联 IO 名称）
        private string _sensorRetract = string.Empty; // 缩回到位感应（关联 IO 名称）
        private string _action = "伸出";              // 默认动作：伸出 / 缩回
        private int _delayMs = 200;

        public string OutPoint { get => _outPoint; set => SetField(ref _outPoint, value); }
        public string SensorExtend { get => _sensorExtend; set => SetField(ref _sensorExtend, value); }
        public string SensorRetract { get => _sensorRetract; set => SetField(ref _sensorRetract, value); }
        public string Action { get => _action; set => SetField(ref _action, value); }
        public int DelayMs { get => _delayMs; set => SetField(ref _delayMs, value); }
    }
}
