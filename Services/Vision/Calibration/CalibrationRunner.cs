// === NoCodeMotion 视觉标定自动执行器（程序自动走 9 点 + 5 点旋转） | 作者：温​启‎志‍ ‍|⁣ ⁣微‏信‎：‏1‏8‌7‍1‌9‎3⁠6‏1​3​9‌9‌ ⁠|​ ​保‎留‍所⁠有‍权⁣利‏，⁣请‌勿‍删‎除 ===
using System.Diagnostics;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Vision.Calibration
{
    /// <summary>一个 9 点样本：图像像素 (U,V) ↔ 机台坐标 (X,Y) mm。</summary>
    public sealed class NinePointSample
    {
        /// <summary>序号（1 起）。</summary>
        public int Index { get; set; }
        /// <summary>标记质心 u（像素）。</summary>
        public double U { get; set; }
        /// <summary>标记质心 v（像素）。</summary>
        public double V { get; set; }
        /// <summary>到位后读回的 X 轴位置（mm）。</summary>
        public double X { get; set; }
        /// <summary>到位后读回的 Y 轴位置（mm）。</summary>
        public double Y { get; set; }
        /// <summary>标记像素面积（用于判断检测稳定性）。</summary>
        public int Area { get; set; }
    }

    /// <summary>一个旋转样本：旋转轴角度（度）↔ 标记质心（像素）。</summary>
    public sealed class RotationSample
    {
        /// <summary>到位后读回的旋转轴角度（度）。</summary>
        public double Angle { get; set; }
        /// <summary>标记质心 u（像素）。</summary>
        public double U { get; set; }
        /// <summary>标记质心 v（像素）。</summary>
        public double V { get; set; }
        /// <summary>标记像素面积。</summary>
        public int Area { get; set; }
    }

    /// <summary>标定运行参数。</summary>
    public sealed class CalibrationRunOptions
    {
        /// <summary>X 轴名称（工程轴名）。</summary>
        public string XAxisName { get; set; } = "";
        /// <summary>Y 轴名称（工程轴名）。</summary>
        public string YAxisName { get; set; } = "";
        /// <summary>旋转轴名称（工程轴名）。</summary>
        public string RotationAxisName { get; set; } = "";

        /// <summary>9 点网格间距（mm），3×3 铺开。</summary>
        public double PitchMm { get; set; } = 10.0;

        /// <summary>旋转步距（度）。</summary>
        public double RotationStepDeg { get; set; } = 20.0;

        /// <summary>旋转采样点数（默认 5 点）。</summary>
        public int RotationCount { get; set; } = 5;

        /// <summary>定位速度（脉冲/单位，0 = 不改速度，沿用工程设定）。</summary>
        public double Speed { get; set; }

        /// <summary>每点到位后的稳定延时（ms），等图像稳定再取图。</summary>
        public int SettleMs { get; set; } = 300;

        /// <summary>单轴到位等待超时（ms）。</summary>
        public int AxisTimeoutMs { get; set; } = 20000;

        /// <summary>是否执行 9 点 XY 标定。</summary>
        public bool DoNinePoint { get; set; } = true;

        /// <summary>是否执行 5 点旋转标定。</summary>
        public bool DoRotation { get; set; } = true;

        /// <summary>旋转拟合半径下限（像素）——太小说明标记离旋转中心太近，结果不可信。</summary>
        public double MinRotationRadiusPx { get; set; } = 5.0;

        /// <summary>9 点仿射残差 RMS 上限（mm）。</summary>
        public double MaxResidualMm { get; set; } = 2.0;

        /// <summary>旋转圆拟合残差 RMS 上限（像素）。</summary>
        public double MaxRotationResidualPx { get; set; } = 3.0;

        /// <summary>标记检测参数。</summary>
        public MarkerDetectOptions Detect { get; set; } = new();
    }

    /// <summary>标定运行结果（含原始点集，便于结果回显与落盘）。</summary>
    public sealed class CalibrationRunResult
    {
        /// <summary>整体是否成功。</summary>
        public bool Ok { get; set; }

        /// <summary>结果说明 / 失败原因。</summary>
        public string Message { get; set; } = "";

        /// <summary>9 点仿射拟合结果。</summary>
        public AffineFit? Affine { get; set; }

        /// <summary>5 点旋转圆拟合结果。</summary>
        public CircleFit? Circle { get; set; }

        /// <summary>9 点原始样本。</summary>
        public List<NinePointSample> NinePoints { get; } = new();

        /// <summary>旋转原始样本。</summary>
        public List<RotationSample> RotationPoints { get; } = new();

        /// <summary>过程日志（逐步追加）。</summary>
        public List<string> Log { get; } = new();

        /// <summary>拼一行给「结果」列用的摘要。</summary>
        public string Summary()
        {
            var parts = new List<string>();
            if (Affine != null && Affine.Ok)
            {
                parts.Add("像素当量 " + CalibSolver.Fmt(Affine.PixelEquivalentU) + " mm/px");
                parts.Add("X 向 " + CalibSolver.Fmt(Affine.AngleXDeg) + "°");
                parts.Add("Y 向 " + CalibSolver.Fmt(Affine.AngleYDeg) + "°");
                parts.Add("残差 " + CalibSolver.Fmt(Affine.RmsMm) + " mm");
            }
            if (Circle != null && Circle.Ok)
            {
                parts.Add("旋转中心 (" + CalibSolver.Fmt(Circle.CenterU) + ", " + CalibSolver.Fmt(Circle.CenterV) + ") px");
            }
            if (parts.Count == 0) return Message;
            return string.Join("，", parts);
        }

        /// <summary>把运行结果转成工程级标定记录（可直接落 xlsx）。</summary>
        public CameraCalibration ToRecord(string cameraName)
        {
            var rec = new CameraCalibration
            {
                CameraName = cameraName ?? "",
                CalibratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                IsValid = Ok,
                Message = Message,
                Points9 = CalibSolver.SerializePoints2(NinePoints.Select(p => (p.U, p.V))),
                Machine9 = CalibSolver.SerializePoints2(NinePoints.Select(p => (p.X, p.Y))),
                Points5 = CalibSolver.SerializePoints3(RotationPoints.Select(p => (p.Angle, p.U, p.V)))
            };
            if (Affine != null)
            {
                rec.A1 = Affine.A1; rec.A2 = Affine.A2; rec.A3 = Affine.A3;
                rec.B1 = Affine.B1; rec.B2 = Affine.B2; rec.B3 = Affine.B3;
                rec.PixelEquivalent = Affine.PixelEquivalentU;
                rec.PixelEquivalentV = Affine.PixelEquivalentV;
                rec.AngleX = Affine.AngleXDeg;
                rec.AngleY = Affine.AngleYDeg;
                rec.ResidualRms = Affine.RmsMm;
            }
            if (Circle != null)
            {
                rec.RotCenterU = Circle.CenterU;
                rec.RotCenterV = Circle.CenterV;
                rec.RotRadiusPx = Circle.RadiusPx;
                rec.RotResidualRms = Circle.RmsPx;
                if (Affine != null && Affine.Ok)
                {
                    Affine.Map(Circle.CenterU, Circle.CenterV, out double mx, out double my);
                    rec.RotCenterX = mx;
                    rec.RotCenterY = my;
                }
            }
            return rec;
        }
    }

    /// <summary>
    /// 标定执行器：程序自动走 9 点（3×3）与 5 点旋转，每点取图 → 找标记质心 →
    /// 与轴读回值配对 → 最小二乘求解。
    ///
    /// 硬件与采集全部走**注入委托**，不直接引用 HardwareBridge / VisionEngine：
    /// 一来避免 Services.Vision → Services 的耦合，二来冒烟可以用合成数据端到端跑完整流程
    /// （合成图里画一个圆 → 质心已知 → 断言仿射系数与旋转中心都被精确还原）。
    /// </summary>
    public sealed class CalibrationRunner
    {
        /// <summary>读轴当前位置（轴名 → mm 或度）。</summary>
        public Func<string, double>? ReadAxisPosition { get; set; }

        /// <summary>绝对定位（轴名，目标值）。</summary>
        public Action<string, double>? MoveAxisAbs { get; set; }

        /// <summary>查询轴是否到位（轴名 → true 表示已停稳）。</summary>
        public Func<string, bool>? IsAxisDone { get; set; }

        /// <summary>设置轴速度（轴名，速度）；为 null 时沿用工程设定。</summary>
        public Action<string, double>? SetAxisSpeed { get; set; }

        /// <summary>采集一帧 BGRA（字节序 B,G,R,A）。</summary>
        public Func<(byte[]? Bgra, int Width, int Height)>? GrabFrame { get; set; }

        /// <summary>
        /// 暂停/停止轮询（每 ≤50ms 调一次）。运行器注入的是 HardwareBridge.WaitGuard：
        /// 暂停时阻塞、停止时抛 OperationCanceledException，本类一律让它冒泡，绝不吞掉。
        /// </summary>
        public Action? Guard { get; set; }

        /// <summary>过程日志回调（可为 null）。</summary>
        public Action<string>? Log { get; set; }

        /// <summary>执行标定（按选项决定跑 9 点 / 5 点 / 两者）。</summary>
        public CalibrationRunResult Run(CalibrationRunOptions o)
        {
            var res = new CalibrationRunResult();
            if (o == null) { res.Message = "标定参数为空"; return res; }
            void Say(string m) { res.Log.Add(m); Log?.Invoke(m); }

            try
            {
                if (ReadAxisPosition == null || MoveAxisAbs == null || GrabFrame == null)
                {
                    res.Ok = false;
                    res.Message = "标定执行器缺少硬件回调（读取轴位置 / 移动轴 / 采集图像）";
                    return res;
                }

                if (o.DoNinePoint && !RunNinePoint(o, res, Say)) return res;
                if (o.DoRotation && !RunRotation(o, res, Say)) return res;

                if (!o.DoNinePoint && !o.DoRotation)
                {
                    res.Ok = false;
                    res.Message = "未选择任何标定内容（9 点 XY 与 5 点旋转至少要选一项）";
                    return res;
                }

                res.Ok = true;
                res.Message = res.Summary();
                Say("标定完成：" + res.Message);
            }
            catch (OperationCanceledException)
            {
                // 暂停/停止：必须冒泡给上层运行器，不能当成「标定失败」吞掉
                throw;
            }
            catch (Exception ex)
            {
                res.Ok = false;
                res.Message = "标定异常：" + ex.Message;
                Say(res.Message);
            }
            return res;
        }

        // ---------- 9 点 XY ----------
        private bool RunNinePoint(CalibrationRunOptions o, CalibrationRunResult res, Action<string> say)
        {
            if (string.IsNullOrWhiteSpace(o.XAxisName) || string.IsNullOrWhiteSpace(o.YAxisName))
            {
                res.Ok = false;
                res.Message = "请先指定 9 点标定的 X 轴与 Y 轴";
                return false;
            }

            double baseX = ReadAxisPosition!(o.XAxisName);
            double baseY = ReadAxisPosition!(o.YAxisName);
            say("9 点起点：X=" + CalibSolver.Fmt(baseX) + "，Y=" + CalibSolver.Fmt(baseY)
                + "，间距 " + CalibSolver.Fmt(o.PitchMm) + " mm");

            int idx = 0;
            for (int iy = -1; iy <= 1; iy++)
            {
                for (int ix = -1; ix <= 1; ix++)
                {
                    double tx = baseX + ix * o.PitchMm;
                    double ty = baseY + iy * o.PitchMm;

                    string? err = MoveAndSettle(o, o.XAxisName, tx);
                    if (err != null) { res.Ok = false; res.Message = err; return false; }
                    err = MoveAndSettle(o, o.YAxisName, ty);
                    if (err != null) { res.Ok = false; res.Message = err; return false; }

                    var (blob, derr) = GrabMarker(o);
                    if (blob == null)
                    {
                        res.Ok = false;
                        res.Message = "9 点标定失败：第 " + (idx + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)
                            + " 点（目标 X=" + CalibSolver.Fmt(tx) + ", Y=" + CalibSolver.Fmt(ty) + "）" + derr;
                        say(res.Message);
                        return false;
                    }

                    // 轴读回值才是真值（可能因限位/误差没走到目标）
                    double ax = ReadAxisPosition!(o.XAxisName);
                    double ay = ReadAxisPosition!(o.YAxisName);
                    idx++;
                    res.NinePoints.Add(new NinePointSample { Index = idx, U = blob.CenterU, V = blob.CenterV, X = ax, Y = ay, Area = blob.Area });
                    say("9 点 [" + idx.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/9] 轴("
                        + CalibSolver.Fmt(ax) + ", " + CalibSolver.Fmt(ay) + ") ↔ " + blob.Describe());
                }
            }

            var fit = CalibSolver.FitAffine(res.NinePoints.Select(p => (p.U, p.V, p.X, p.Y)).ToList());
            res.Affine = fit;
            if (!fit.Ok)
            {
                res.Ok = false;
                res.Message = "9 点仿射拟合失败：" + fit.Message;
                say(res.Message);
                return false;
            }
            if (fit.RmsMm > o.MaxResidualMm)
            {
                res.Ok = false;
                res.Message = "9 点残差过大（RMS " + CalibSolver.Fmt(fit.RmsMm) + " mm > 上限 "
                    + CalibSolver.Fmt(o.MaxResidualMm) + " mm），请检查标记检测是否稳定";
                say(res.Message);
                return false;
            }
            say("9 点标定完成：" + fit.Message);
            return true;
        }

        // ---------- 5 点旋转 ----------
        private bool RunRotation(CalibrationRunOptions o, CalibrationRunResult res, Action<string> say)
        {
            if (string.IsNullOrWhiteSpace(o.RotationAxisName))
            {
                res.Ok = false;
                res.Message = "请先指定旋转轴";
                return false;
            }

            int count = Math.Max(3, o.RotationCount);
            double baseAngle = ReadAxisPosition!(o.RotationAxisName);
            double start = baseAngle - (count - 1) / 2.0 * o.RotationStepDeg;
            say("旋转标定：起点 " + CalibSolver.Fmt(baseAngle) + "°，步距 " + CalibSolver.Fmt(o.RotationStepDeg)
                + "°，共 " + count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " 点");

            for (int k = 0; k < count; k++)
            {
                double target = start + k * o.RotationStepDeg;
                string? err = MoveAndSettle(o, o.RotationAxisName, target);
                if (err != null) { res.Ok = false; res.Message = err; return false; }

                var (blob, derr) = GrabMarker(o);
                if (blob == null)
                {
                    res.Ok = false;
                    res.Message = "旋转标定失败：第 " + (k + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)
                        + " 点（目标角度 " + CalibSolver.Fmt(target) + "°）" + derr;
                    say(res.Message);
                    return false;
                }

                double a = ReadAxisPosition!(o.RotationAxisName);
                res.RotationPoints.Add(new RotationSample { Angle = a, U = blob.CenterU, V = blob.CenterV, Area = blob.Area });
                say("旋转 [" + (k + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + "/"
                    + count.ToString(System.Globalization.CultureInfo.InvariantCulture) + "] "
                    + CalibSolver.Fmt(a) + "° ↔ " + blob.Describe());
            }

            var circ = CalibSolver.FitCircle(res.RotationPoints.Select(p => (p.U, p.V)).ToList());
            res.Circle = circ;
            if (!circ.Ok)
            {
                res.Ok = false;
                res.Message = "旋转圆拟合失败：" + circ.Message;
                say(res.Message);
                return false;
            }
            if (circ.RadiusPx < o.MinRotationRadiusPx)
            {
                res.Ok = false;
                res.Message = "旋转半径过小（" + CalibSolver.Fmt(circ.RadiusPx) + " px），标记应远离旋转中心";
                say(res.Message);
                return false;
            }
            if (circ.RmsPx > o.MaxRotationResidualPx)
            {
                res.Ok = false;
                res.Message = "旋转残差过大（RMS " + CalibSolver.Fmt(circ.RmsPx) + " px > 上限 "
                    + CalibSolver.Fmt(o.MaxRotationResidualPx) + " px）";
                say(res.Message);
                return false;
            }
            say("旋转标定完成：" + circ.Message);
            return true;
        }

        // ---------- 通用 ----------
        private string? MoveAndSettle(CalibrationRunOptions o, string axis, double target)
        {
            try
            {
                if (o.Speed > 0) SetAxisSpeed?.Invoke(axis, o.Speed);
                MoveAxisAbs!(axis, target);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { return "移动轴「" + axis + "」失败：" + ex.Message; }

            var sw = Stopwatch.StartNew();
            while (true)
            {
                Guard?.Invoke();
                bool done;
                try { done = IsAxisDone?.Invoke(axis) ?? true; }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { return "等待轴「" + axis + "」到位失败：" + ex.Message; }
                if (done) break;
                if (sw.ElapsedMilliseconds > o.AxisTimeoutMs)
                    return "等待轴「" + axis + "」到位超时（" + o.AxisTimeoutMs.ToString(System.Globalization.CultureInfo.InvariantCulture) + " ms）";
                Sleep(20);
            }
            Sleep(o.SettleMs);
            return null;
        }

        private (MarkerBlob? Blob, string Error) GrabMarker(CalibrationRunOptions o)
        {
            Guard?.Invoke();
            var f = GrabFrame!.Invoke();
            if (f.Bgra == null || f.Width <= 0 || f.Height <= 0)
                return (null, "采集图像失败（相机不可用或返回空帧）");
            var blob = MarkerDetector.DetectLargest(f.Bgra, f.Width, f.Height, o.Detect, out string err);
            return (blob, err);
        }

        /// <summary>可中断的等待：每 ≤50ms 轮询一次 Guard（暂停阻塞 / 停止抛异常）。</summary>
        private void Sleep(int ms)
        {
            if (ms <= 0) return;
            int elapsed = 0;
            while (elapsed < ms)
            {
                Guard?.Invoke();
                int step = Math.Min(50, ms - elapsed);
                System.Threading.Thread.Sleep(step);
                elapsed += step;
            }
        }
    }
}
// === NoCodeMotion 视觉标定自动执行器（程序自动走 9 点 + 5 点旋转） | 作者：温⁣启⁠志‌ ‌|​ ​微⁠信​：‎1‎8‎7‎1⁠9‎3​6‏1‎3‎9⁠9⁠ ​|⁣ ‌保⁣留​所⁠有​权‍利‏，⁠请‎勿‏删‍除 ===
