using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>变量（用于流程/逻辑中引用与计算）。</summary>
    public class VariableItem : EditorItemBase
    {
        private string _varType = "int";      // 类型：int / float / double / bool / string
        private string _initialValue = "0";    // 初始值
        private string _currentValue = "0";    // 当前值（运行时）
        private string _scope = "全局";         // 作用域：全局 / 局部
        private string _description = string.Empty; // 备注

        public string VarType { get => _varType; set => SetField(ref _varType, value); }
        public string InitialValue { get => _initialValue; set => SetField(ref _initialValue, value); }
        public string CurrentValue { get => _currentValue; set => SetField(ref _currentValue, value); }
        public string Scope { get => _scope; set => SetField(ref _scope, value); }
        public string Description { get => _description; set => SetField(ref _description, value); }
    }
}
