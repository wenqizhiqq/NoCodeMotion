using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>IO 点位（输入或输出），对应工业 IO 配置表格中的一行。</summary>
    public class IoItem : EditorItemBase
    {
        private string _cardType = "雷赛";      // 卡类（厂家/卡类型）
        private int _cardNo;                    // 卡号
        private int _moduleNo;                  // 模块
        private int _sequence;                  // 序号
        private string _suitCode = string.Empty; // 套码（自定义分组标签）
        private string _level = "取反";          // 电平（取反 / 原点 等）
        private string _cylinder = string.Empty;// 气缸（关联气缸名称）
        private string _function = "动点";       // 功能（动点 / 原点 / 光栅 / 安全门 / 启动按钮 / 复位按钮 / 停止按钮）
        private int _value;                     // 当前状态值

        public string CardType { get => _cardType; set => SetField(ref _cardType, value); }
        public int CardNo { get => _cardNo; set => SetField(ref _cardNo, value); }
        public int ModuleNo { get => _moduleNo; set => SetField(ref _moduleNo, value); }
        public int Sequence { get => _sequence; set => SetField(ref _sequence, value); }
        public string SuitCode { get => _suitCode; set => SetField(ref _suitCode, value); }
        public string Level { get => _level; set => SetField(ref _level, value); }
        public string Cylinder { get => _cylinder; set => SetField(ref _cylinder, value); }
        public string Function { get => _function; set => SetField(ref _function, value); }
        public int Value { get => _value; set => SetField(ref _value, value); }
    }
}
