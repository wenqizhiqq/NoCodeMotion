using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>流程步骤</summary>
    public class FlowItem : EditorItemBase
    {
        private string _stepType = "轴运动";        // 轴运动 / IO动作 / 气缸动作 / 等待 / 通讯
        private string _target = string.Empty;       // 关联对象（轴/IO/气缸名称）
        private string _description = string.Empty;

        public string StepType { get => _stepType; set => SetField(ref _stepType, value); }
        public string Target { get => _target; set => SetField(ref _target, value); }
        public string Description { get => _description; set => SetField(ref _description, value); }
    }
}
