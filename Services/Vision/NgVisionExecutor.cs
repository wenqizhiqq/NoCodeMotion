// =====================================================================
// 节点图视觉节点执行器（NgRunner ←→ VisionEngine 的会话式桥接层）
//
// 两边的模型是错位的：
//   · VisionEngine.Run 吃「一整条 VisualFlowStep 链」，链内前序「图像采集」为后续算子供图；
//   · NgRunner 是「逐节点、可暂停/单步/断点」的解释器，一次只执行一个节点。
// 本类负责把后者翻译成前者：
//   ① 采集节点执行后把当帧落盘为 PNG（会话帧），后续节点以「文件」源复用同一帧，
//      不会每个节点都去重新触发相机；
//   ② 「测量」需要上游算子产生的特征点（匹配中心 / 缺陷重心），因此为它重放本帧已执行的算子链；
//   ③ 循环体内同一节点会被反复执行 —— 算子链按节点 Id 占位复用并截断其后，链不会无限膨胀；
//   ④ 对位 / 标定 VisionEngine 里没有对应算子类型，在本类内实现
//      （对位 = 匹配中心与基准点的偏差换算；标定 = 棋盘格/圆点求 mm/px）；
//   ⑤ 所有结果按「变量前缀」写入 SimRuntime 变量，供下游 条件分支 / 运算 节点直接引用。
//
// 无相机、无模板文件时，引擎会回退合成测试图并自动取模板，整条视觉链依然能跑通，
// 便于现场没接相机时先验证流程逻辑。
// =====================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NoCodeMotion.Models;
using NoCodeMotion.Models.NodeGraph;

namespace NoCodeMotion.Services.Vision;

public sealed class NgVisionExecutor
{
    private readonly Action<string, double> _setVar;
    private readonly Action<string> _log;
    private readonly string _sessionDir;

    /// <summary>当帧落盘路径（空 = 本次运行尚未采集）。</summary>
    private string _framePath = "";
    /// <summary>当帧来自合成测试图：后续节点重建同一张测试图，保持「自动取模板」能力。</summary>
    private bool _frameSynthetic;
    private int _frameW, _frameH;
    private int _synthW = 1280, _synthH = 960;

    /// <summary>标定节点求得的像素当量 mm/px（0 = 未标定）。</summary>
    private double _mmPerPx;
    /// <summary>最近一次模板匹配结果（对位节点消费）。</summary>
    private MatchOutcome? _lastMatch;

    /// <summary>本帧已执行的算子（按节点 Id 占位），供「测量」重放取特征点。</summary>
    private readonly List<(string Id, VisualFlowStep Step)> _ops = new();

    public NgVisionExecutor(Action<string, double>? setVar, Action<string>? log)
    {
        _setVar = setVar ?? ((_, __) => { });
        _log = log ?? (_ => { });
        _sessionDir = Path.Combine(Path.GetTempPath(), "NoCodeMotion", "NgVision");
    }

    /// <summary>每次 Run/Step 启动时清空会话（丢弃上一轮的帧与算子链）。</summary>
    public void Reset()
    {
        _framePath = "";
        _frameSynthetic = false;
        _frameW = _frameH = 0;
        _mmPerPx = 0;
        _lastMatch = null;
        _ops.Clear();
    }

    /// <summary>是否为本类负责的视觉节点。</summary>
    public static bool IsVisionKind(NgKind k) =>
        k == NgKind.CamCapture || k == NgKind.TemplateMatch || k == NgKind.DefectDetect
        || k == NgKind.Measure || k == NgKind.Align || k == NgKind.Calib;

    /// <summary>单节点执行结果：summary 写卡片摘要，error 非空时由 NgRunner 写 ErrorText（不进异常流）。</summary>
    public readonly struct VisionExecOutcome
    {
        public readonly string Summary;
        public readonly string? Error;
        public VisionExecOutcome(string summary, string? error) { Summary = summary; Error = error; }
    }

    /// <summary>执行一个视觉节点。**任何内部异常一律降级到 Error 字段**，绝不抛给 NgRunner，
    /// 由 NgRunner 写入节点卡 ErrorText + Report.LastError + 日志面板；
    /// 这样调试器不会因为单个视觉节点配置缺失/算子失败而中断整条流程。</summary>
    public VisionExecOutcome Execute(NgNode node)
    {
        try
        {
            return node.Kind switch
            {
                NgKind.CamCapture => Capture(node),
                NgKind.TemplateMatch => Match(node),
                NgKind.DefectDetect => Defect(node),
                NgKind.Measure => MeasureStep(node),
                NgKind.Align => Align(node),
                NgKind.Calib => Calib(node),
                _ => new VisionExecOutcome($"非视觉节点：{node.Kind}", null)
            };
        }
        catch (Exception ex)
        {
            // 真致命（采集完全失败、标定无帧且落盘失败等）→ 返回错误，不抛
            return new VisionExecOutcome($"{node.Kind} 异常：{ex.Message}", ex.Message);
        }
    }

    // ===================== 图像采集 =====================

    private VisionExecOutcome Capture(NgNode node)
    {
        string src = Prop(node, "图像源", "相机").Trim();
        if (src != "相机" && src != "文件" && src != "文件夹") src = "相机";
        string path = Prop(node, "图像路径", "").Trim();
        string camName = Prop(node, "相机", "");
        int camIdx = ResolveCameraIndex(camName);

        var step = new VisualFlowStep
        {
            Name = "图像采集",
            StepType = "图像采集",
            Enabled = true,
            SourceType = src,
            CameraId = camIdx.ToString(CultureInfo.InvariantCulture),
            SavePath = src == "文件" ? path : "",
            FolderPath = src == "文件夹" ? path : "",
            ExposureMs = Num(node, "曝光ms", 10),
            Width = (int)Num(node, "宽度", 1280),
            Height = (int)Num(node, "高度", 960)
        };
        _synthW = step.Width <= 0 ? 1280 : step.Width;
        _synthH = step.Height <= 0 ? 960 : step.Height;

        VisionReport rep;
        string? err = null;
        try { rep = VisionEngine.Run(new[] { step }); }
        catch (Exception ex) { rep = new VisionReport(); err = ex.Message; }

        // 新的一帧：算子链与匹配结果全部作废
        _ops.Clear();
        _lastMatch = null;
        _framePath = "";
        _frameSynthetic = rep.UsedSynthetic;
        _frameW = rep.Width;
        _frameH = rep.Height;

        var r = LastResult(rep, "图像采集");
        if (err != null)
            return new VisionExecOutcome($"图像采集失败：{err}", err);

        // 当帧落盘（文件名按节点 Id，循环里覆盖同一文件不堆垃圾）
        string file = Path.Combine(_sessionDir, $"frame_{Safe(node.Id)}.png");
        if (VisionEngine.SaveBgra(rep.Bgra, rep.Width, rep.Height, file)) _framePath = file;

        SimRuntime.FlashCamera(string.IsNullOrWhiteSpace(camName) ? "相机1" : camName.Trim());
        _setVar("图像宽", _frameW);
        _setVar("图像高", _frameH);
        string summary = r?.Summary ?? $"已采集 {_frameW}×{_frameH}";
        _log($"[视觉] 图像采集：{summary}");
        return new VisionExecOutcome(summary, null);
    }

    // ===================== 模板匹配 =====================

    private VisionExecOutcome Match(NgNode node)
    {
        var op = new VisualFlowStep
        {
            Name = "模板匹配",
            StepType = "模板匹配",
            Enabled = true,
            TemplatePath = Prop(node, "模板路径", "").Trim(),
            MatchMode = Prop(node, "匹配模式", "灰度匹配"),
            ScoreThreshold = Num(node, "分数阈值", 0.8),
            AngleRange = Num(node, "角度范围", 360)
        };
        ApplyRoi(op, Prop(node, "模板框", ""));

        // 用 try/RunChain 的方式捕获引擎的"请先框选模板区域"等配置类异常，
        // 这类问题不该让整条流程崩 —— 写变量「通过=0」+ 摘要给原因即可。
        VisionReport rep;
        string err = "";
        try { rep = RunChain(op, replayOps: false); }
        catch (Exception ex) { err = ex.Message; rep = new VisionReport(); }
        var r = LastResult(rep, "模板匹配");
        var m = rep.Match;
        string prefix = Prop(node, "变量前缀", "匹配");

        if (m != null)
        {
            _lastMatch = m;
            _setVar(prefix + "分数", m.Score);
            _setVar(prefix + "X", m.X + m.W / 2.0);
            _setVar(prefix + "Y", m.Y + m.H / 2.0);
            _setVar(prefix + "角度", m.Angle);
            _setVar(prefix + "数量", rep.Matches.Count);
            _setVar(prefix + "通过", m.Pass ? 1 : 0);
        }
        else
        {
            _setVar(prefix + "分数", 0);
            _setVar(prefix + "数量", 0);
            _setVar(prefix + "通过", 0);
        }

        AddOp(node.Id, op);
        bool pass = m?.Pass ?? false;
        string summary = !string.IsNullOrEmpty(err)
            ? $"模板匹配未配置（{err}）"
            : (r?.Summary ?? "模板匹配无结果");
        _log($"[视觉] 模板匹配：{summary}");

        // 「失败即停」开关完全去掉 —— 节点卡 + Report.LastError 才是异常显示的地方。
        // 是否中断由上游 Decision 节点按 `前缀通过` 变量决定。
        return new VisionExecOutcome(summary, pass ? null : (err.Length > 0 ? err : "模板匹配未通过"));
    }

    // ===================== 缺陷检测 =====================

    private VisionExecOutcome Defect(NgNode node)
    {
        var op = new VisualFlowStep
        {
            Name = "缺陷检测",
            StepType = "缺陷检测",
            Enabled = true,
            DetectMode = Prop(node, "检测模式", "阈值面积"),
            Algorithm = Prop(node, "缺陷类型", "暗斑"),
            Threshold = Num(node, "阈值", 128),
            MinArea = Num(node, "最小面积", 50),
            MaxArea = Num(node, "最大面积", 100000)
        };
        if (op.MaxArea <= op.MinArea) op.MaxArea = 1e9;

        VisionReport rep;
        string err = "";
        try { rep = RunChain(op, replayOps: false); }
        catch (Exception ex) { err = ex.Message; rep = new VisionReport(); }
        var r = LastResult(rep, "缺陷检测");
        int cnt = r?.Count ?? 0;
        int allow = (int)Num(node, "允许缺陷数", 0);
        bool pass = cnt <= allow;

        string prefix = Prop(node, "变量前缀", "缺陷");
        _setVar(prefix + "数量", cnt);
        _setVar(prefix + "面积", r?.Value ?? 0);
        _setVar(prefix + "通过", pass ? 1 : 0);

        AddOp(node.Id, op);
        string summary = $"{r?.Summary ?? $"检出 {cnt} 处"} → {(pass ? "合格" : "超限")}（允许 {allow}）";
        _log($"[视觉] 缺陷检测：{summary}");
        // 「超限即停」开关去掉 —— 超限仅做变量标记，不抛异常中断流程。
        string? errOut = null;
        if (err.Length > 0) errOut = err;
        else if (!pass) errOut = $"缺陷 {cnt} 个，超出允许 {allow}";
        return new VisionExecOutcome(summary, errOut);
    }

    // ===================== 测量 =====================

    private VisionExecOutcome MeasureStep(NgNode node)
    {
        double cal = Num(node, "标定系数", 1);
        if (cal <= 0) cal = _mmPerPx > 0 ? _mmPerPx : 1;

        var op = new VisualFlowStep
        {
            Name = "测量",
            StepType = "测量",
            Enabled = true,
            MeasureMode = Prop(node, "测量项", "距离"),
            Calibration = cal,
            Unit = Prop(node, "单位", "mm")
        };

        VisionReport rep;
        string err = "";
        try { rep = RunChain(op, replayOps: true); }
        catch (Exception ex) { err = ex.Message; rep = new VisionReport(); }
        var r = LastResult(rep, "测量");
        double val = r?.Value ?? 0;
        double lo = Num(node, "下限", 0), hi = Num(node, "上限", 0);
        bool judge = hi > lo;
        bool pass = !judge || (val >= lo && val <= hi);

        string prefix = Prop(node, "变量前缀", "测量");
        _setVar(prefix + "值", val);
        _setVar(prefix + "通过", pass ? 1 : 0);

        AddOp(node.Id, op);
        string summary = judge
            ? $"{r?.Summary ?? $"测量 {val:F2}"} → {(pass ? "合格" : "超差")}（{lo:F2}~{hi:F2}）"
            : (r?.Summary ?? $"测量 {val:F2} {op.Unit}");
        _log($"[视觉] 测量：{summary}");
        string? errOut = null;
        if (err.Length > 0) errOut = err;
        else if (judge && !pass) errOut = $"测量 {val:F2} 超出范围 {lo:F2}~{hi:F2}";
        return new VisionExecOutcome(summary, errOut);
    }

    // ===================== 对位（匹配中心 vs 基准点） =====================

    private VisionExecOutcome Align(NgNode node)
    {
        if (_lastMatch == null)
            return new VisionExecOutcome("对位跳过：尚未执行模板匹配", "对位需要上游「模板匹配」结果");

        double mmpp = Num(node, "像素当量", 0);
        if (mmpp <= 0) mmpp = _mmPerPx > 0 ? _mmPerPx : 1;

        double cx = _lastMatch.X + _lastMatch.W / 2.0;
        double cy = _lastMatch.Y + _lastMatch.H / 2.0;
        double rx = Num(node, "基准X", 0), ry = Num(node, "基准Y", 0);
        bool useCenter = rx <= 0 && ry <= 0;
        if (useCenter)
        {
            rx = (_frameW > 0 ? _frameW : _synthW) / 2.0;
            ry = (_frameH > 0 ? _frameH : _synthH) / 2.0;
        }

        double dx = (cx - rx) * mmpp, dy = (cy - ry) * mmpp;
        double dist = Math.Sqrt(dx * dx + dy * dy);
        double tol = Num(node, "容差", 0.5);
        bool pass = dist <= tol;

        string prefix = Prop(node, "变量前缀", "偏差");
        _setVar(prefix + "X", dx);
        _setVar(prefix + "Y", dy);
        _setVar(prefix + "距离", dist);
        _setVar(prefix + "通过", pass ? 1 : 0);

        string basis = useCenter ? "基准=图像中心" : $"基准=({rx:F0},{ry:F0})";
        string summary = $"偏差 X {dx:F3} / Y {dy:F3}　距离 {dist:F3}（{basis}，当量 {mmpp:G4} mm/px）→ {(pass ? "合格" : "超差")}";
        _log($"[视觉] 对位：{summary}");
        // 「超差即停」开关去掉 —— 超差仅做变量标记，不抛异常。
        return new VisionExecOutcome(summary, pass ? null : $"对位偏差 {dist:F3} 超出容差 {tol:F3}");
    }

    // ===================== 标定（求像素当量 mm/px） =====================

    private VisionExecOutcome Calib(NgNode node)
    {
        try { EnsureFrameFile(); }
        catch (Exception ex) { return new VisionExecOutcome($"标定跳过：{ex.Message}", ex.Message); }

        bool circles = Prop(node, "标定板", "棋盘格").IndexOf("圆", StringComparison.Ordinal) >= 0;
        int rows = (int)Num(node, "行数", 9);
        int cols = (int)Num(node, "列数", 9);
        double cell = Num(node, "格子尺寸mm", 10);

        if (!VisionEngine.Calibrate(_framePath, circles, rows, cols, cell,
                out double mmpp, out int found, out string msg))
            return new VisionExecOutcome($"标定失败：{msg}", msg);

        _mmPerPx = mmpp;
        _setVar("像素当量", mmpp);
        string summary = $"像素当量 {mmpp:G6} mm/px（{msg}）";
        _log($"[视觉] 标定：{summary}　命中点 {found}");
        return new VisionExecOutcome(summary, null);
    }

    // ===================== 会话帧与算子链 =====================

    /// <summary>以当帧为源构造「图像采集」步骤（供每个算子节点拼一条最短可执行链）。</summary>
    private VisualFlowStep CurrentFrameStep()
    {
        var s = new VisualFlowStep { Name = "当帧", StepType = "图像采集", Enabled = true, SourceType = "文件" };
        if (_frameSynthetic || string.IsNullOrEmpty(_framePath) || !File.Exists(_framePath))
        {
            // 合成测试图路线：留空路径让引擎重建（MakeSynthetic 用固定随机种子，同尺寸同图），
            // 这样 usedSynthetic=true，模板匹配可自动取图中已知目标当模板。
            s.SavePath = "";
            s.Width = _synthW;
            s.Height = _synthH;
        }
        else
        {
            s.SavePath = _framePath;
        }
        return s;
    }

    private VisionReport RunChain(VisualFlowStep op, bool replayOps)
    {
        var chain = new List<VisualFlowStep> { CurrentFrameStep() };
        if (replayOps && _ops.Count > 0) chain.AddRange(_ops.Select(x => x.Step));
        chain.Add(op);
        return VisionEngine.Run(chain);
    }

    /// <summary>算子入链：同一节点重复执行（循环体）时占位复用并截断其后的旧算子。</summary>
    private void AddOp(string id, VisualFlowStep op)
    {
        int i = _ops.FindIndex(x => x.Id == id);
        if (i >= 0) _ops.RemoveRange(i, _ops.Count - i);
        _ops.Add((id, op));
        if (_ops.Count > 32) _ops.RemoveRange(0, _ops.Count - 32);   // 兜底护栏
    }

    /// <summary>标定需要真实文件：没有会话帧时先生成一帧并落盘。</summary>
    private void EnsureFrameFile()
    {
        if (!string.IsNullOrEmpty(_framePath) && File.Exists(_framePath)) return;
        VisionReport rep;
        try { rep = VisionEngine.Run(new[] { CurrentFrameStep() }); }
        catch (Exception ex) { throw new InvalidOperationException("无法生成测试图：" + ex.Message, ex); }
        if (!rep.HasImage) throw new InvalidOperationException("尚未采集图像，且无法生成测试图");
        _frameW = rep.Width;
        _frameH = rep.Height;
        _frameSynthetic = rep.UsedSynthetic;
        string file = Path.Combine(_sessionDir, "frame_calib.png");
        if (!VisionEngine.SaveBgra(rep.Bgra, rep.Width, rep.Height, file))
            throw new InvalidOperationException("标定图像落盘失败（临时目录不可写）");
        _framePath = file;
    }

    // ===================== 小工具 =====================

    private static VisionStepResult? LastResult(VisionReport rep, string type)
    {
        for (int i = rep.Results.Count - 1; i >= 0; i--)
            if (string.Equals(rep.Results[i].Type, type, StringComparison.Ordinal)) return rep.Results[i];
        return rep.Results.Count > 0 ? rep.Results[rep.Results.Count - 1] : null;
    }

    /// <summary>模板框文本 "x,y,w,h" → 步骤 ROI；解析失败按未框选处理。</summary>
    private static void ApplyRoi(VisualFlowStep s, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var parts = text.Replace('，', ',').Replace('　', ' ')
                        .Split(new[] { ',', ' ', ';', '×', 'x', 'X' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4) return;
        var v = new int[4];
        for (int i = 0; i < 4; i++)
        {
            if (!int.TryParse(parts[i].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out v[i])) return;
        }
        if (v[2] <= 0 || v[3] <= 0) return;
        s.TemplateRoiX = v[0];
        s.TemplateRoiY = v[1];
        s.TemplateRoiW = v[2];
        s.TemplateRoiH = v[3];
    }

    /// <summary>相机名 → OpenCV 相机索引：先按项目相机表顺序，找不到再取名字里的数字（1 基转 0 基）。</summary>
    private static int ResolveCameraIndex(string name)
    {
        string n = (name ?? "").Trim();
        var cams = ProjectStore.Data?.Cameras;
        if (cams != null && n.Length > 0)
        {
            for (int i = 0; i < cams.Count; i++)
                if (string.Equals((cams[i].Name ?? "").Trim(), n, StringComparison.OrdinalIgnoreCase))
                    return i;
        }
        var digits = new string(n.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, NumberStyles.Any, CultureInfo.InvariantCulture, out int k) && k > 0) return k - 1;
        return 0;
    }

    private static string Prop(NgNode n, string name, string def)
    {
        var p = n.Props.FirstOrDefault(x => x.Name == name);
        return p == null || string.IsNullOrWhiteSpace(p.Value) ? def : p.Value;
    }

    private static double Num(NgNode n, string name, double def)
    {
        var s = Prop(n, name, def.ToString(CultureInfo.InvariantCulture));
        return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : def;
    }

    private static bool IsYes(string s) => s.Trim() is "是" or "true" or "True" or "1";

    private static string Safe(string id)
    {
        var chars = id.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray();
        return chars.Length > 0 ? new string(chars) : "frame";
    }
}
