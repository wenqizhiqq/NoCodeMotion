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
        private string _sourceType = "文件";   // 相机 / 文件夹 / 文件
        private double _exposureMs = 10.0;
        private int _width = 1920;
        private int _height = 1080;
        private string _folderPath = "";

        // 模板匹配
        private string _templatePath = "";
        private double _scoreThreshold = 0.8;
        private double _angleRange = 360.0;
        private string _matchMode = "灰度匹配";   // 灰度匹配 / 轮廓匹配
        // 模板框选：用户在结果图上拖拽画框确定模板区域（原图像素坐标）。W/H<=0 表示尚未框选
        private int _templateRoiX = 0;
        private int _templateRoiY = 0;
        private int _templateRoiW = 0;
        private int _templateRoiH = 0;

        // 缺陷检测
        private string _algorithm = "NCC";
        private double _minArea = 100.0;
        private double _maxArea = 100000.0;
        private double _threshold = 128.0;
        private string _detectMode = "阈值面积";   // 阈值面积 / 边缘轮廓

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

        // 运行结果（每次运行后由 VisionEngine 回写）
        private double _durationMs = 0;
        private bool _lastOk = false;
        private string _lastResult = "";   // 空=尚未运行

        public string Name { get => _name; set => Set(ref _name, value); }
        public string StepType { get => _stepType; set => Set(ref _stepType, value); }
        public bool Enabled { get => _enabled; set => Set(ref _enabled, value); }

        public string CameraId { get => _cameraId; set => Set(ref _cameraId, value); }
        public string SavePath { get => _savePath; set => Set(ref _savePath, value); }
        public double ExposureMs { get => _exposureMs; set => Set(ref _exposureMs, value); }
        public int Width { get => _width; set => Set(ref _width, value); }
        public int Height { get => _height; set => Set(ref _height, value); }
        public string SourceType { get => _sourceType; set => Set(ref _sourceType, value); }
        public string FolderPath { get => _folderPath; set => Set(ref _folderPath, value); }

        public string TemplatePath { get => _templatePath; set => Set(ref _templatePath, value); }
        public double ScoreThreshold { get => _scoreThreshold; set => Set(ref _scoreThreshold, value); }
        public double AngleRange { get => _angleRange; set => Set(ref _angleRange, value); }
        public string MatchMode { get => _matchMode; set => Set(ref _matchMode, value); }
        public int TemplateRoiX { get => _templateRoiX; set => Set(ref _templateRoiX, value); }
        public int TemplateRoiY { get => _templateRoiY; set => Set(ref _templateRoiY, value); }
        public int TemplateRoiW { get => _templateRoiW; set => Set(ref _templateRoiW, value); }
        public int TemplateRoiH { get => _templateRoiH; set => Set(ref _templateRoiH, value); }

        /// <summary>模板框描述文本："未框选" 或 "x,y  WxH"，用于参数区回显。</summary>
        public string TemplateRoiText => _templateRoiW > 0 && _templateRoiH > 0
            ? $"({_templateRoiX},{_templateRoiY})  {_templateRoiW}×{_templateRoiH}"
            : "未框选（在右侧图上拖拽画框）";

        public string Algorithm { get => _algorithm; set => Set(ref _algorithm, value); }
        public double MinArea { get => _minArea; set => Set(ref _minArea, value); }
        public double MaxArea { get => _maxArea; set => Set(ref _maxArea, value); }
        public double Threshold { get => _threshold; set => Set(ref _threshold, value); }
        public string DetectMode { get => _detectMode; set => Set(ref _detectMode, value); }

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

        // 运行结果
        public double DurationMs { get => _durationMs; set { if (Set(ref _durationMs, value)) OnChanged(nameof(DurationText)); } }
        public bool LastOk { get => _lastOk; set { if (Set(ref _lastOk, value)) OnChanged(nameof(ResultText)); } }
        public string LastResult { get => _lastResult; set { if (Set(ref _lastResult, value)) { OnChanged(nameof(ResultText)); OnChanged(nameof(DurationText)); } } }

        /// <summary>耗时显示文本：未运行显示 –，否则“x.x ms”。</summary>
        public string DurationText => string.IsNullOrEmpty(_lastResult) ? "–" : $"{_durationMs:F1} ms";

        /// <summary>结果标记：未运行 –，成功 ✓，失败 ✗。</summary>
        public string ResultText => string.IsNullOrEmpty(_lastResult) ? "–" : (_lastOk ? "✓" : "✗");

        public event PropertyChangedEventHandler? PropertyChanged;
        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            return true;
        }
        private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
