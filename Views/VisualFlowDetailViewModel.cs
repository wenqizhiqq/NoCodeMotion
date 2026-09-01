// === NoCodeMotion 视觉流程详情 VM | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Services.Vision;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 视觉流程详情 VM（DependencyObject，以便作为资源并对其 DP 绑定）。
    /// Steps / Name 由 VisualFlowPage 的代码隐藏通过 RelativeSource 绑到主选中 FlowItem 的
    /// VisualSteps / Name；因此本 VM 操作的 Steps 就是主 FlowItem.VisualSteps（同一引用），
    /// 步骤的增删直接落进主流程项。RunCommand 负责把流程真正跑起来并把结果回显。
    /// </summary>
    public class VisualFlowDetailViewModel : DependencyObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // ---- 依赖属性（与 FlowPage 的绑定对应） ----
        public static readonly DependencyProperty StepsProperty =
            DependencyProperty.Register(nameof(Steps), typeof(ObservableCollection<VisualFlowStep>),
                typeof(VisualFlowDetailViewModel));

        public ObservableCollection<VisualFlowStep>? Steps
        {
            get => (ObservableCollection<VisualFlowStep>?)GetValue(StepsProperty);
            set => SetValue(StepsProperty, value);
        }

        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register(nameof(Name), typeof(string), typeof(VisualFlowDetailViewModel));

        public string? Name
        {
            get => (string?)GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }

        public static readonly DependencyProperty SelectedStepProperty =
            DependencyProperty.Register(nameof(SelectedStep), typeof(VisualFlowStep),
                typeof(VisualFlowDetailViewModel),
                new PropertyMetadata(null, OnSelectedStepChanged));

        public VisualFlowStep? SelectedStep
        {
            get => (VisualFlowStep?)GetValue(SelectedStepProperty);
            set => SetValue(SelectedStepProperty, value);
        }

        public static readonly DependencyProperty HasStepProperty =
            DependencyProperty.Register(nameof(HasStep), typeof(bool), typeof(VisualFlowDetailViewModel));

        public bool HasStep
        {
            get => (bool)GetValue(HasStepProperty);
            private set => SetValue(HasStepProperty, value);
        }

        // ---- 当前选中步骤的工具类型显隐标志（用于右侧参数卡按类型切换） ----
        public bool IsImageAcquisition => SelectedStep?.StepType == "图像采集";
        public bool IsPreprocess => SelectedStep?.StepType == "图像预处理";
        public bool IsTemplateMatch => SelectedStep?.StepType == "模板匹配";
        public bool IsDefect => SelectedStep?.StepType == "缺陷检测";
        public bool IsMeasure => SelectedStep?.StepType == "测量";
        public bool IsComm => SelectedStep?.StepType == "通讯";

        // ---- 图像采集来源类型显隐标志（相机 / 文件夹 / 文件） ----
        public bool IsCameraSource => SelectedStep?.SourceType == "相机";
        public bool IsFolderSource => SelectedStep?.SourceType == "文件夹";
        public bool IsFileSource => SelectedStep?.SourceType == "文件";

        // ---- 运行结果相关 ----
        public static readonly DependencyProperty ResultImageProperty =
            DependencyProperty.Register(nameof(ResultImage), typeof(ImageSource), typeof(VisualFlowDetailViewModel));

        public ImageSource? ResultImage
        {
            get => (ImageSource?)GetValue(ResultImageProperty);
            set => SetValue(ResultImageProperty, value);
        }

        public static readonly DependencyProperty HasResultProperty =
            DependencyProperty.Register(nameof(HasResult), typeof(bool), typeof(VisualFlowDetailViewModel));

        public bool HasResult
        {
            get => (bool)GetValue(HasResultProperty);
            private set => SetValue(HasResultProperty, value);
        }

        public static readonly DependencyProperty IsRunningProperty =
            DependencyProperty.Register(nameof(IsRunning), typeof(bool), typeof(VisualFlowDetailViewModel));

        public bool IsRunning
        {
            get => (bool)GetValue(IsRunningProperty);
            private set => SetValue(IsRunningProperty, value);
        }

        public static readonly DependencyProperty CanRunProperty =
            DependencyProperty.Register(nameof(CanRun), typeof(bool), typeof(VisualFlowDetailViewModel), new PropertyMetadata(true));

        public bool CanRun
        {
            get => (bool)GetValue(CanRunProperty);
            private set => SetValue(CanRunProperty, value);
        }

        public static readonly DependencyProperty RunStatusProperty =
            DependencyProperty.Register(nameof(RunStatus), typeof(string), typeof(VisualFlowDetailViewModel));

        /// <summary>运行状态/提示文本。setter 为 public，便于页面 code-behind（如框选模板）回写提示。</summary>
        public string RunStatus
        {
            get => (string?)GetValue(RunStatusProperty) ?? "";
            set => SetValue(RunStatusProperty, value ?? "");
        }

        // ---- 模板预览（点「确定模板」后裁剪 ROI 区域显示在按钮下方） ----
        public static readonly DependencyProperty TemplatePreviewImageProperty =
            DependencyProperty.Register(nameof(TemplatePreviewImage), typeof(ImageSource), typeof(VisualFlowDetailViewModel));

        public ImageSource? TemplatePreviewImage
        {
            get => (ImageSource?)GetValue(TemplatePreviewImageProperty);
            set => SetValue(TemplatePreviewImageProperty, value);
        }

        public static readonly DependencyProperty HasTemplatePreviewProperty =
            DependencyProperty.Register(nameof(HasTemplatePreview), typeof(bool), typeof(VisualFlowDetailViewModel));

        public bool HasTemplatePreview
        {
            get => (bool)GetValue(HasTemplatePreviewProperty);
            private set => SetValue(HasTemplatePreviewProperty, value);
        }

        // ---- 模板匹配结果（点「开启匹配」后叠加在右侧图像上：相似度 / 精度 / 位置 / 角度） ----
        public static readonly DependencyProperty MatchResultProperty =
            DependencyProperty.Register(nameof(MatchResult), typeof(MatchOutcome), typeof(VisualFlowDetailViewModel),
                new PropertyMetadata(null, (d, e) => ((VisualFlowDetailViewModel)d).OnPropertyChanged(nameof(HasMatchResult))));

        public MatchOutcome? MatchResult
        {
            get => (MatchOutcome?)GetValue(MatchResultProperty);
            set => SetValue(MatchResultProperty, value);
        }

        public bool HasMatchResult => MatchResult != null;

        // ---- 全部匹配结果框集合（供结果图 WPF 叠加层按 angle 绘制旋转矩形/文本）。
        // 与 MatchResult（单条 best）并存：MatchResult 喂顶部信息胶囊，MatchResults 喂叠加层。
        public static readonly DependencyProperty MatchResultsProperty =
            DependencyProperty.Register(nameof(MatchResults), typeof(ObservableCollection<MatchBox>),
                typeof(VisualFlowDetailViewModel),
                new PropertyMetadata(null, (d, e) => ((VisualFlowDetailViewModel)d).OnPropertyChanged(nameof(HasMatchResults))));

        public ObservableCollection<MatchBox>? MatchResults
        {
            get => (ObservableCollection<MatchBox>?)GetValue(MatchResultsProperty);
            set => SetValue(MatchResultsProperty, value);
        }

        public bool HasMatchResults => MatchResults != null && MatchResults.Count > 0;

        /// <summary>每步执行结果（绑定到结果列表）。同一实例，增删由集合自身通知。</summary>
        public ObservableCollection<VisionStepResult> Results { get; } = new();

        // ---- 命令 ----
        public ICommand AddStepCommand { get; }
        public ICommand DeleteStepCommand { get; }
        public ICommand RunCommand { get; }
        public ICommand RunStepCommand { get; }
        /// <summary>把本地图片文件载入右侧预览（不跑引擎），用于「浏览后直接看图」。</summary>
        public void LoadPreviewImage(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;   // 立即加载并释放文件句柄，避免占用
                bmp.EndInit();
                bmp.Freeze();
                ResultImage = bmp;
                HasResult = true;
                RunStatus = $"已载入图像：{Path.GetFileName(path)}（{bmp.PixelWidth}×{bmp.PixelHeight}）";
            }
            catch (Exception ex)
            {
                RunStatus = $"载入图像失败：{ex.Message}";
            }
        }

        /// <summary>
        /// 把当前选中步骤的 ROI 区域从 ResultImage 裁剪出来，保存为 PNG 写到 Templates/ 目录，
        /// 回写 step.TemplatePath，并把裁剪图设到 TemplatePreviewImage（在参数卡「确定模板」按钮下方预览）。
        /// </summary>
        private void CaptureTemplate()
        {
            var step = SelectedStep;
            if (step == null) { RunStatus = "请先选中一个模板匹配步骤"; return; }
            if (ResultImage == null) { RunStatus = "请先在「图像采集」步骤浏览一张图像（作为模板源图）"; return; }
            if (step.TemplateRoiW <= 0 || step.TemplateRoiH <= 0)
            {
                RunStatus = "请先在右侧图像上拖拽画框确定模板区域，再点「确定模板」";
                return;
            }

            try
            {
                var src = (BitmapSource)ResultImage;
                int x = Math.Max(0, step.TemplateRoiX);
                int y = Math.Max(0, step.TemplateRoiY);
                int w = Math.Min(step.TemplateRoiW, src.PixelWidth - x);
                int h = Math.Min(step.TemplateRoiH, src.PixelHeight - y);
                if (w <= 0 || h <= 0) { RunStatus = "框选区域超出图像范围"; return; }

                var cropped = new CroppedBitmap(src, new Int32Rect(x, y, w, h));
                cropped.Freeze();

                // 保存到 工作目录/Templates/，文件名 = 步骤名_时间戳.png
                string dir = Path.Combine(Directory.GetCurrentDirectory(), "Templates");
                Directory.CreateDirectory(dir);
                string safe = SanitizeFileName(string.IsNullOrWhiteSpace(step.Name) ? "template" : step.Name!);
                string fileName = $"{safe}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string fullPath = Path.Combine(dir, fileName);

                using (var fs = File.Create(fullPath))
                {
                    var enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(cropped));
                    enc.Save(fs);
                }

                step.TemplatePath = fullPath;
                TemplatePreviewImage = cropped;
                HasTemplatePreview = true;
                RunStatus = $"模板已保存：{fileName}（{w}×{h}）";
            }
            catch (Exception ex)
            {
                RunStatus = $"确定模板失败：{ex.Message}";
            }
        }

        private static string SanitizeFileName(string raw)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder(raw.Length);
            foreach (var c in raw) sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            return sb.ToString();
        }

        /// <summary>
        /// 「开启匹配」：取流程里「图像采集」步骤的图片作为源图，用当前步骤的框选区域（或模板文件）
        /// 执行模板匹配，把匹配结果框画到右侧图像上。
        /// </summary>
        private async Task RunMatchAsync()
        {
            var step = SelectedStep;
            if (step == null) { RunStatus = "请先选中一个模板匹配步骤"; return; }

            var acquire = Steps?.FirstOrDefault(x => x.StepType == "图像采集" && x.Enabled);
            string srcPath = acquire?.SavePath ?? "";
            if (!File.Exists(srcPath))
            {
                RunStatus = "请先在「图像采集」步骤点浏览选择一张图片（作为源图）";
                return;
            }
            if (step.TemplateRoiW <= 0 || step.TemplateRoiH <= 0)
            {
                if (!File.Exists(step.TemplatePath))
                {
                    RunStatus = "请先在右侧图像上拖拽画框确定模板区域";
                    return;
                }
            }

            IsRunning = true;
            CanRun = false;
            RunStatus = "匹配中…";
            Results.Clear();

            // 构造两步流程：采集（源图文件）→ 匹配（当前步骤参数，含框选 ROI）
            var acq = new VisualFlowStep
            {
                Name = "采集", StepType = "图像采集", Enabled = true,
                SourceType = "文件", SavePath = srcPath
            };
            var mt = new VisualFlowStep
            {
                Name = step.Name, StepType = "模板匹配", Enabled = true,
                TemplateRoiX = step.TemplateRoiX, TemplateRoiY = step.TemplateRoiY,
                TemplateRoiW = step.TemplateRoiW, TemplateRoiH = step.TemplateRoiH,
                TemplatePath = step.TemplatePath, MatchMode = step.MatchMode,
                ScoreThreshold = step.ScoreThreshold, AngleRange = step.AngleRange
            };

            try
            {
                var report = await Task.Run(() =>
                    VisionEngine.Run(new ObservableCollection<VisualFlowStep> { acq, mt }, _progress));

                // 回到 UI 线程组装结果
                Results.Clear();
                foreach (var r in report.Results) Results.Add(r);

                if (report.HasImage && report.Bgra != null && report.Bgra.Length == report.Width * report.Height * 4)
                {
                    var wb = new WriteableBitmap(report.Width, report.Height, 96, 96, PixelFormats.Bgra32, null);
                    wb.WritePixels(new Int32Rect(0, 0, report.Width, report.Height), report.Bgra, report.Width * 4, 0);
                    ResultImage = wb;
                    HasResult = true;
                }

                // 结构化匹配结果供右侧图像叠加（相似度 / 精度 / 位置 / 角度）
                MatchResult = report.Match;

                // 全部匹配框 → 叠加层画旋转矩形（避免 Angle≠0 时轴对齐框方向错）
                MatchResults = report.Matches.Count > 0
                    ? new ObservableCollection<MatchBox>(report.Matches)
                    : null;

                int ok = 0;
                foreach (var r in Results) if (r.Ok) ok++;
                RunStatus = report.Match != null
                    ? $"匹配完成：{Results.Count} 步，{ok} 步成功　相似度 {report.Match.Score:F3} / 阈值 {report.Match.Threshold:F2}　共找到 {report.Matches.Count} 个目标"
                    : $"匹配完成：{Results.Count} 步，{ok} 步成功";
            }
            catch (Exception ex)
            {
                // 异常不再逃逸到 UnobservedTaskException；同时显式提示到状态栏
                RunStatus = $"匹配失败：{ex.Message}";
                StatusBarService.ReportException($"模板匹配失败：{ex.Message}");
            }
            finally
            {
                // 异常路径也要恢复「运行中」标志，否则按钮永久变灰
                IsRunning = false;
                CanRun = true;
            }
        }

        /// <summary>路径浏览命令：参数为要写入的属性名（SavePath/FolderPath/TemplatePath/PreImage2Path）。</summary>
        public ICommand BrowsePathCommand { get; }
        /// <summary>「开启匹配」：用当前图 + 框选模板执行匹配，把结果框画到右侧图像上。</summary>
        public ICommand RunMatchCommand { get; }
        /// <summary>清除已框选的模板区域。</summary>
        public ICommand ClearTemplateRoiCommand { get; }
        /// <summary>
        /// 「确定模板」：把右侧已框选的 ROI 区域从当前 ResultImage 裁剪出来，保存到 Templates/ 目录，
        /// 同时把裁剪图显示到参数卡按钮下方的预览区，并把路径回写到 step.TemplatePath。
        /// </summary>
        public ICommand ConfirmTemplateCommand { get; }

        private readonly Progress<string> _progress;

        public VisualFlowDetailViewModel()
        {
            AddStepCommand = new SimpleRelayCommand(_ =>
            {
                var s = Steps;
                if (s == null) return;
                var step = new VisualFlowStep { Name = $"步骤{s.Count + 1}", StepType = "图像采集" };
                s.Add(step);
                SelectedStep = step;
            });

            DeleteStepCommand = new SimpleRelayCommand(_ =>
            {
                var s = Steps;
                if (s == null || SelectedStep == null) return;
                s.Remove(SelectedStep);
                SelectedStep = null;
            });

            RunCommand = new SimpleRelayCommand(_ => _ = RunAsync());
            RunStepCommand = new SimpleRelayCommand(p => _ = RunStepAsync(p as VisualFlowStep));
            BrowsePathCommand = new SimpleRelayCommand(p => BrowsePath(p as string));
            RunMatchCommand = new SimpleRelayCommand(_ => _ = RunMatchAsync());
            ClearTemplateRoiCommand = new SimpleRelayCommand(_ =>
            {
                if (SelectedStep == null) return;
                SelectedStep.TemplateRoiW = 0;
                SelectedStep.TemplateRoiH = 0;
                SelectedStep.TemplateRoiX = 0;
                SelectedStep.TemplateRoiY = 0;
                TemplatePreviewImage = null;
                HasTemplatePreview = false;
                RunStatus = "已清除模板框选";
            });
            ConfirmTemplateCommand = new SimpleRelayCommand(_ => CaptureTemplate());
            _progress = new Progress<string>(msg => RunStatus = msg);
        }

        /// <summary>
        /// 打开文件/文件夹选择对话框，把选中路径写回当前选中步骤的对应属性。
        /// 参数 kind：SavePath / FolderPath / TemplatePath / PreImage2Path。
        /// </summary>
        private void BrowsePath(string? kind)
        {
            var step = SelectedStep;
            if (step == null || string.IsNullOrEmpty(kind)) return;

            try
            {
                if (kind == nameof(VisualFlowStep.FolderPath))
                {
                    var fdlg = new OpenFolderDialog { Title = "选择图像文件夹" };
                    if (fdlg.ShowDialog() == true) step.FolderPath = fdlg.FolderName;
                    return;
                }

                var ofd = new OpenFileDialog
                {
                    Title = kind == nameof(VisualFlowStep.TemplatePath) ? "选择模板图像" : "选择图像文件",
                    Filter = "图像文件|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|所有文件|*.*",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (ofd.ShowDialog() != true) return;

                switch (kind)
                {
                    case nameof(VisualFlowStep.SavePath):
                        step.SavePath = ofd.FileName;
                        // 图像采集选完图立即载入右侧预览（用户要求：浏览后直接看图）
                        LoadPreviewImage(ofd.FileName);
                        break;
                    case nameof(VisualFlowStep.TemplatePath): step.TemplatePath = ofd.FileName; break;
                    case nameof(VisualFlowStep.PreImage2Path): step.PreImage2Path = ofd.FileName; break;
                }
            }
            catch (Exception ex)
            {
                // 对话框若被环境（如安全软件）阻断，给出明确提示，避免"点了没反应"
                RunStatus = $"路径选择失败：{ex.Message}";
            }
        }

        private static void OnSelectedStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var vm = (VisualFlowDetailViewModel)d;
            // 解旧步骤、订新步骤的 INPC，便于 StepType 变化时刷新右侧参数卡显隐
            if (e.OldValue is INotifyPropertyChanged oldInpc) oldInpc.PropertyChanged -= vm.OnSelectedStepTypeChanged;
            if (e.NewValue is INotifyPropertyChanged newInpc) newInpc.PropertyChanged += vm.OnSelectedStepTypeChanged;
            vm.HasStep = e.NewValue != null;
            // 切换步骤时清空模板预览 / 匹配结果（不同步骤数据互不串）
            vm.TemplatePreviewImage = null;
            vm.HasTemplatePreview = false;
            vm.MatchResult = null;
            vm.MatchResults = null;
            vm.RaiseTypeFlags();
            vm.RaiseSourceFlags();
        }

        private void OnSelectedStepTypeChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VisualFlowStep.StepType)) RaiseTypeFlags();
            else if (e.PropertyName == nameof(VisualFlowStep.SourceType)) RaiseSourceFlags();
        }

        private void RaiseTypeFlags()
        {
            OnPropertyChanged(nameof(IsImageAcquisition));
            OnPropertyChanged(nameof(IsPreprocess));
            OnPropertyChanged(nameof(IsTemplateMatch));
            OnPropertyChanged(nameof(IsDefect));
            OnPropertyChanged(nameof(IsMeasure));
            OnPropertyChanged(nameof(IsComm));
        }

        private void RaiseSourceFlags()
        {
            OnPropertyChanged(nameof(IsCameraSource));
            OnPropertyChanged(nameof(IsFolderSource));
            OnPropertyChanged(nameof(IsFileSource));
        }

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private async Task RunAsync()
        {
            var steps = Steps;
            if (steps == null || steps.Count == 0)
            {
                RunStatus = "请先选中视觉流程并添加步骤";
                return;
            }
            var enabled = new ObservableCollection<VisualFlowStep>(steps);
            if (enabled.Count == 0) { RunStatus = "没有可执行的步骤"; return; }

            IsRunning = true;
            CanRun = false;
            RunStatus = "视觉流程运行中…";
            Results.Clear();

            var report = await Task.Run(() => VisionEngine.Run(enabled, _progress));

            // 回到 UI 线程组装结果（WriteableBitmap 必须在 UI 线程创建）
            Results.Clear();
            foreach (var r in report.Results) Results.Add(r);

            if (report.HasImage && report.Bgra != null && report.Bgra.Length == report.Width * report.Height * 4)
            {
                var wb = new WriteableBitmap(report.Width, report.Height, 96, 96, PixelFormats.Bgra32, null);
                wb.WritePixels(new Int32Rect(0, 0, report.Width, report.Height), report.Bgra, report.Width * 4, 0);
                ResultImage = wb;
                HasResult = true;
            }
            else
            {
                HasResult = false;
            }

            // 单条最佳摘要（喂顶部信息胶囊）
            MatchResult = report.Match;
            // 全部匹配框 → 叠加层旋转矩形
            MatchResults = report.Matches.Count > 0
                ? new ObservableCollection<MatchBox>(report.Matches)
                : null;

            int ok = 0;
            foreach (var r in Results) if (r.Ok) ok++;
            IsRunning = false;
            CanRun = true;
            RunStatus = report.Matches.Count > 0
                ? $"完成：共 {Results.Count} 步，{ok} 步成功　匹配 {report.Matches.Count} 个目标"
                : $"完成：共 {Results.Count} 步，{ok} 步成功";
        }

        /// <summary>从首个启用步骤运行到 target（含），用于单步/分段验证，并回填该段每步的耗时与结果。</summary>
        private async Task RunStepAsync(VisualFlowStep? target)
        {
            var steps = Steps;
            if (steps == null || steps.Count == 0 || target == null)
            {
                RunStatus = "请先选中视觉流程并添加步骤";
                return;
            }
            int idx = steps.IndexOf(target);
            if (idx < 0) return;

            // 先清空所有步骤的上次结果，避免未执行步骤显示旧数据
            foreach (var s in steps) { s.DurationMs = 0; s.LastOk = false; s.LastResult = ""; }

            var runList = new ObservableCollection<VisualFlowStep>(steps.Take(idx + 1));

            IsRunning = true;
            CanRun = false;
            RunStatus = $"运行到「{target.Name}」…";
            Results.Clear();

            var report = await Task.Run(() => VisionEngine.Run(runList, _progress));

            Results.Clear();
            foreach (var r in report.Results) Results.Add(r);

            if (report.HasImage && report.Bgra != null && report.Bgra.Length == report.Width * report.Height * 4)
            {
                var wb = new WriteableBitmap(report.Width, report.Height, 96, 96, PixelFormats.Bgra32, null);
                wb.WritePixels(new Int32Rect(0, 0, report.Width, report.Height), report.Bgra, report.Width * 4, 0);
                ResultImage = wb;
                HasResult = true;
            }
            else
            {
                HasResult = false;
            }

            // 单条最佳摘要 + 全部匹配框（叠加层旋转矩形）
            MatchResult = report.Match;
            MatchResults = report.Matches.Count > 0
                ? new ObservableCollection<MatchBox>(report.Matches)
                : null;

            int ok = 0;
            foreach (var r in Results) if (r.Ok) ok++;
            IsRunning = false;
            CanRun = true;
            RunStatus = report.Matches.Count > 0
                ? $"运行到「{target.Name}」完成：{Results.Count} 步，{ok} 步成功　匹配 {report.Matches.Count} 个目标"
                : $"运行到「{target.Name}」完成：{Results.Count} 步，{ok} 步成功";
        }

        private sealed class SimpleRelayCommand : ICommand
        {
            private readonly Action<object?> _execute;
            public SimpleRelayCommand(Action<object?> execute) { _execute = execute; }
            public event EventHandler? CanExecuteChanged { add { } remove { } }
            public bool CanExecute(object? parameter) => true;
            public void Execute(object? parameter) => _execute(parameter);
        }
    }
}
// === NoCodeMotion 视觉流程详情 VM | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
