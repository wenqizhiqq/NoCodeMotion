using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>料盘（阵列托盘）配置</summary>
    public class TrayItem : EditorItemBase
    {
        private int _rows = 5;
        private int _cols = 5;
        private double _startX;
        private double _startY;
        private double _pitchX = 10;
        private double _pitchY = 10;

        public int Rows { get => _rows; set => SetField(ref _rows, value); }
        public int Cols { get => _cols; set => SetField(ref _cols, value); }
        public double StartX { get => _startX; set => SetField(ref _startX, value); }
        public double StartY { get => _startY; set => SetField(ref _startY, value); }
        public double PitchX { get => _pitchX; set => SetField(ref _pitchX, value); }
        public double PitchY { get => _pitchY; set => SetField(ref _pitchY, value); }
    }
}
