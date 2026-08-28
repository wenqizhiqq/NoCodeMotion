// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace NoCodeMotion.Models
{
    /// <summary>流程中的一步。DataGrid 的每一行对应一个 FlowStep。</summary>
    public class FlowStep : EditorItemBase
    {
        private string _logic = "就";        // 如果 / 就 / 否则
        private string _function = "轴";      // 轴 / IO / 气缸 / modbus
        private string _property = "速度";    // 属性：随功能自动填充（速度/位置/编码器位置…）
        private string _operation = "等于";   // 修改 / 加 / 减 / 乘 / 除 / 等于 / 是否等于 / 大于 / 小于 / 大于等于 / 小于等于 / 取反 / 与 / 或 / 取模
        private string _setValue = string.Empty;
        private string _timeout = "空";       // 超时：等待3秒就统计 / 空 / 不停机
        private int _durationMs;
        private string _actualValue = string.Empty; // 实际值：属性列对应的实际测量值

        public string Logic { get => _logic; set => SetField(ref _logic, value); }
        public string Function { get => _function; set => SetField(ref _function, value); }
        public string Property { get => _property; set => SetField(ref _property, value); }
        public string Operation { get => _operation; set => SetField(ref _operation, value); }
        public string SetValue { get => _setValue; set => SetField(ref _setValue, value); }
        public string Timeout { get => _timeout; set => SetField(ref _timeout, value); }
        public int DurationMs { get => _durationMs; set => SetField(ref _durationMs, value); }



        private bool _isCurrent;

        /// <summary>运行态：当前是否正在执行此行（用于流程表格高亮，不落盘）。</summary>
        [JsonIgnore]
        public bool IsCurrent { get => _isCurrent; set => SetField(ref _isCurrent, value); }
        public string ActualValue { get => _actualValue; set => SetField(ref _actualValue, value); }
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
