using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>
    /// 相机条目。属性编辑页 + DataGrid 双向绑定需要 INPC 通知。
    /// （Models 层纯 POCO 原则主要约束工艺/变量等纯数据；编辑器 ItemBase 类允许 INPC。）
    /// </summary>
    public class CameraItem : INotifyPropertyChanged
    {
        private string _name = "";
        private string _vendor = "海康威视";
        private string _ipAddress = "192.168.1.100";
        private int _port = 8000;
        private int _width = 1920;
        private int _height = 1080;
        private double _exposureMs = 10.0;
        private double _gain = 1.0;
        private bool _isConnected;
        private string _description = "";

        /// <summary>相机显示名（左侧列表也用它）</summary>
        public string Name { get => _name; set => Set(ref _name, value); }

        /// <summary>厂商/品牌（海康/大华/巴斯勒/AVT 等）</summary>
        public string Vendor { get => _vendor; set => Set(ref _vendor, value); }

        /// <summary>IP 地址（GigE Vision / RTSP）</summary>
        public string IpAddress { get => _ipAddress; set => Set(ref _ipAddress, value); }

        /// <summary>端口（默认 8000）</summary>
        public int Port { get => _port; set => Set(ref _port, value); }

        /// <summary>分辨率宽</summary>
        public int Width { get => _width; set => Set(ref _width, value); }

        /// <summary>分辨率高</summary>
        public int Height { get => _height; set => Set(ref _height, value); }

        /// <summary>曝光（毫秒）</summary>
        public double ExposureMs { get => _exposureMs; set => Set(ref _exposureMs, value); }

        /// <summary>增益</summary>
        public double Gain { get => _gain; set => Set(ref _gain, value); }

        /// <summary>当前连接状态（运行时回填）</summary>
        public bool IsConnected { get => _isConnected; set => Set(ref _isConnected, value); }

        /// <summary>备注</summary>
        public string Description { get => _description; set => Set(ref _description, value); }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
