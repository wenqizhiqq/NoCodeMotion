using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>流程中的一步。DataGrid 的每一行对应一个 FlowStep。</summary>
    public class FlowStep : EditorItemBase
    {
        private string _logic = "就";        // 如果 / 就 / 否则
        private string _function = "轴";      // 轴 / IO / 气缸 / modbus
        private string _operation = "等于";   // 加 / 减 / 乘 / 除 / 等于 / 是否等于
        private string _setValue = string.Empty;
        private int _durationMs;

        public string Logic { get => _logic; set => SetField(ref _logic, value); }
        public string Function { get => _function; set => SetField(ref _function, value); }
        public string Operation { get => _operation; set => SetField(ref _operation, value); }
        public string SetValue { get => _setValue; set => SetField(ref _setValue, value); }
        public int DurationMs { get => _durationMs; set => SetField(ref _durationMs, value); }
    }
}
