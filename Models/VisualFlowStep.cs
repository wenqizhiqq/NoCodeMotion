// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‏启‍志‏◆‏编⁠写‏◇‍微⁠信⁠﹕⁠1‎8‏7⁠◆‏1‎9‍3‌6⁠◇​1‌3​9​9⁣　‏※‎保​留⁠所‌有⁠权⁣利‍请​勿⁠删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>
    /// 视觉流程步骤。StepType 决定右侧参数面板显示哪一组字段。
    /// 工具类型：模板匹配 / 图像采集 / 图像预处理 / 缺陷检测 / 测量 / 字符识别 / 通讯。
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
        private string _sourceType = "相机";   // 相机 / 文件夹 / 文件（默认相机；相机读不到才回退测试图）
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

        // 字符识别（OCR，Windows.Media.Ocr）
        private string _ocrLanguage = "自动";        // 自动 / 中文 / 英文 / 日文 / 韩文 / 繁体中文
        private string _ocrExpectedText = "";        // 期望文本（比对基准，空=仅识别不判定）
        private string _ocrMatchMode = "包含";       // 包含 / 等于 / 正则
        private bool _ocrIgnoreCase = false;
        private int _ocrRoiX = 0;                    // 识别区域（原图像素坐标），W/H<=0 表示整图
        private int _ocrRoiY = 0;
        private int _ocrRoiW = 0;
        private int _ocrRoiH = 0;

        // 标定（9 点 XY + 5 点旋转；程序自动走点，结果落工程「标定」表）
        private string _calibMode = "XY+旋转";        // XY+旋转 / 仅XY / 仅旋转
        private string _calibXAxis = "";              // 9 点标定的 X 轴（工程轴名）
        private string _calibYAxis = "";              // 9 点标定的 Y 轴
        private string _calibRotAxis = "";            // 旋转标定的旋转轴
        private double _calibPitchMm = 10.0;          // 9 点网格间距（mm）
        private double _calibRotationStepDeg = 20.0;  // 旋转步距（度）
        private int _calibRotationCount = 5;          // 旋转采样点数
        private double _calibSpeed = 0;               // 定位速度（0 = 沿用工程轴速度）
        private int _calibSettleMs = 300;             // 每点到位后的稳定延时（ms）
        private int _calibThreshold = 128;            // 标记亮度阈值
        private bool _calibDarkMarker = true;         // true = 亮底黑点（暗标记）
        private int _calibMinArea = 30;               // 标记最小面积
        private int _calibMaxArea = 400000;           // 标记最大面积
        private string _calibResultText = "";         // 上次标定结果回显（多行文本）

        // 运行结果（每次运行后由 VisionEngine 回写）
        private double _durationMs = 0;
        private bool _lastOk = false;
        private string _lastResult = "";   // 空=尚未运行

        public string Name { get => _name; set => Set(ref _name, value); }
        public string StepType { get => _stepType; set => Set(ref _stepType, value); }
        public bool Enabled { get => _enabled; set => Set(ref _enabled, value); }

        private bool _breakpoint;
        /// <summary>断点：运行到此步暂停（状态「触发断点」），点「继续/运行」恢复（随工程落盘）。</summary>
        public bool Breakpoint { get => _breakpoint; set => Set(ref _breakpoint, value); }

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

        // 字符识别
        public string OcrLanguage { get => _ocrLanguage; set => Set(ref _ocrLanguage, value); }
        public string OcrExpectedText { get => _ocrExpectedText; set => Set(ref _ocrExpectedText, value); }
        public string OcrMatchMode { get => _ocrMatchMode; set => Set(ref _ocrMatchMode, value); }
        public bool OcrIgnoreCase { get => _ocrIgnoreCase; set => Set(ref _ocrIgnoreCase, value); }
        public int OcrRoiX { get => _ocrRoiX; set { if (Set(ref _ocrRoiX, value)) OnChanged(nameof(OcrRoiText)); } }
        public int OcrRoiY { get => _ocrRoiY; set { if (Set(ref _ocrRoiY, value)) OnChanged(nameof(OcrRoiText)); } }
        public int OcrRoiW { get => _ocrRoiW; set { if (Set(ref _ocrRoiW, value)) OnChanged(nameof(OcrRoiText)); } }
        public int OcrRoiH { get => _ocrRoiH; set { if (Set(ref _ocrRoiH, value)) OnChanged(nameof(OcrRoiText)); } }

        /// <summary>识别区域描述文本："整图识别" 或 "x,y  WxH"，用于参数区回显。</summary>
        public string OcrRoiText => _ocrRoiW > 0 && _ocrRoiH > 0
            ? $"({_ocrRoiX},{_ocrRoiY})  {_ocrRoiW}×{_ocrRoiH}"
            : "整图识别（在右侧图上拖拽可框选区域）";

        // 标定
        /// <summary>标定方式：XY+旋转 / 仅XY / 仅旋转。</summary>
        public string CalibMode { get => _calibMode; set => Set(ref _calibMode, value); }
        /// <summary>9 点标定的 X 轴名称（工程轴名）。</summary>
        public string CalibXAxis { get => _calibXAxis; set => Set(ref _calibXAxis, value); }
        /// <summary>9 点标定的 Y 轴名称。</summary>
        public string CalibYAxis { get => _calibYAxis; set => Set(ref _calibYAxis, value); }
        /// <summary>旋转标定的旋转轴名称。</summary>
        public string CalibRotAxis { get => _calibRotAxis; set => Set(ref _calibRotAxis, value); }
        /// <summary>9 点网格间距（mm）。</summary>
        public double CalibPitchMm { get => _calibPitchMm; set => Set(ref _calibPitchMm, value); }
        /// <summary>旋转步距（度）。</summary>
        public double CalibRotationStepDeg { get => _calibRotationStepDeg; set => Set(ref _calibRotationStepDeg, value); }
        /// <summary>旋转采样点数（默认 5 点）。</summary>
        public int CalibRotationCount { get => _calibRotationCount; set => Set(ref _calibRotationCount, value); }
        /// <summary>标定定位速度（0 = 沿用工程轴速度）。</summary>
        public double CalibSpeed { get => _calibSpeed; set => Set(ref _calibSpeed, value); }
        /// <summary>每点到位后的稳定延时（ms）。</summary>
        public int CalibSettleMs { get => _calibSettleMs; set => Set(ref _calibSettleMs, value); }
        /// <summary>标记亮度阈值（0~255）。</summary>
        public int CalibThreshold { get => _calibThreshold; set => Set(ref _calibThreshold, value); }
        /// <summary>标记明暗：true = 亮底黑点（暗标记）。</summary>
        public bool CalibDarkMarker { get => _calibDarkMarker; set => Set(ref _calibDarkMarker, value); }
        /// <summary>标记最小面积（像素）。</summary>
        public int CalibMinArea { get => _calibMinArea; set => Set(ref _calibMinArea, value); }
        /// <summary>标记最大面积（像素）。</summary>
        public int CalibMaxArea { get => _calibMaxArea; set => Set(ref _calibMaxArea, value); }
        /// <summary>上次标定结果回显（多行：像素当量 / 方向角 / 旋转中心 / 残差）。</summary>
        public string CalibResultText { get => _calibResultText; set => Set(ref _calibResultText, value); }

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
