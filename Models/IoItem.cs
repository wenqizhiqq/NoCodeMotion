using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>IO 点位</summary>
    public class IoItem : EditorItemBase
    {
        private string _ioType = "输入";            // 输入 / 输出
        private int _cardNo;
        private int _portNo;
        private bool _state;

        public string IoType { get => _ioType; set => SetField(ref _ioType, value); }
        public int CardNo { get => _cardNo; set => SetField(ref _cardNo, value); }
        public int PortNo { get => _portNo; set => SetField(ref _portNo, value); }
        public bool State { get => _state; set => SetField(ref _state, value); }
    }
}
