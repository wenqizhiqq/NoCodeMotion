using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>点位中单个轴的目标坐标。</summary>
    public class PointAxis : INotifyPropertyChanged
    {
        private string _axisName = string.Empty;
        private double _position;

        public string AxisName
        {
            get => _axisName;
            set => SetField(ref _axisName, value);
        }

        public double Position
        {
            get => _position;
            set => SetField(ref _position, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
