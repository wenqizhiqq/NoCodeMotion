// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>
    /// 视觉流程步骤。StepType 决定右侧参数面板显示哪一组字段。
    /// 工具类型：模板匹配 / 图像采集 / 图像预处理 / 缺陷检测 / 测量 / 通讯。
    /// </summary>
    public class VisualFlowStep : INotifyPropertyChanged
    {
        private string _name = "";
        private string _stepType = "图像采集";
        private bool _enabled = true;

        // 通用
        private string _cameraId = "0";
        private string _savePath = "";

        // 图像采集
        private double _exposureMs = 10.0;
        private int _width = 1920;
        private int _height = 1080;

        // 模板匹配
        private string _templatePath = "";
        private double _scoreThreshold = 0.8;
        private double _angleRange = 360.0;

        // 缺陷检测
        private string _algorithm = "NCC";
        private double _minArea = 100.0;
        private double _maxArea = 100000.0;
        private double _threshold = 128.0;

        // 测量
        private string _measureMode = "距离";
        private double _calibration = 1.0;
        private string _unit = "mm";

        // 通讯
        private string _protocol = "Modbus";
        private string _target = "";
        private string _content = "";

        // 图像预处理：操作名 + 两个通用参数 + ROI + 第二张图路径（算术）
        private string _preOp = "无";
        private double _preParam1 = 128.0;
        private double _preParam2 = 3.0;
        private string _preRoi = "";
        private string _preImage2Path = "";

        public string Name { get => _name; set => Set(ref _name, value); }
        public string StepType { get => _stepType; set => Set(ref _stepType, value); }
        public bool Enabled { get => _enabled; set => Set(ref _enabled, value); }

        public string CameraId { get => _cameraId; set => Set(ref _cameraId, value); }
        public string SavePath { get => _savePath; set => Set(ref _savePath, value); }
        public double ExposureMs { get => _exposureMs; set => Set(ref _exposureMs, value); }
        public int Width { get => _width; set => Set(ref _width, value); }
        public int Height { get => _height; set => Set(ref _height, value); }

        public string TemplatePath { get => _templatePath; set => Set(ref _templatePath, value); }
        public double ScoreThreshold { get => _scoreThreshold; set => Set(ref _scoreThreshold, value); }
        public double AngleRange { get => _angleRange; set => Set(ref _angleRange, value); }

        public string Algorithm { get => _algorithm; set => Set(ref _algorithm, value); }
        public double MinArea { get => _minArea; set => Set(ref _minArea, value); }
        public double MaxArea { get => _maxArea; set => Set(ref _maxArea, value); }
        public double Threshold { get => _threshold; set => Set(ref _threshold, value); }

        public string MeasureMode { get => _measureMode; set => Set(ref _measureMode, value); }
        public double Calibration { get => _calibration; set => Set(ref _calibration, value); }
        public string Unit { get => _unit; set => Set(ref _unit, value); }

        public string Protocol { get => _protocol; set => Set(ref _protocol, value); }
        public string Target { get => _target; set => Set(ref _target, value); }
        public string Content { get => _content; set => Set(ref _content, value); }

        // 图像预处理
        public string PreOp { get => _preOp; set => Set(ref _preOp, value); }
        public double PreParam1 { get => _preParam1; set => Set(ref _preParam1, value); }
        public double PreParam2 { get => _preParam2; set => Set(ref _preParam2, value); }
        public string PreRoi { get => _preRoi; set => Set(ref _preRoi, value); }
        public string PreImage2Path { get => _preImage2Path; set => Set(ref _preImage2Path, value); }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
