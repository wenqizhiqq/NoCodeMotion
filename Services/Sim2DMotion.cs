// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// =====================================================================
// 2D 运控仿真内核（纯 C#，无 WPF 依赖，可单元测试）。
//
// 模型：以 AxisRuntimeState 为唯一指令位置源——
//   · 流程仿真播放器（SimFlowPlayer）每帧低通插值写入 → 目标本身在滑动，内核追踪之；
//   · 脚本桩 / 点位流程瞬时写入 → 内核按梯形速度规划滑向目标（加减速真实可感）；
//   · 2D 视图点动（按住按钮）→ 内核按点动速度持续前推目标并回写 AxisRuntimeState，
//     与 3D 视图 / 自定义页面读回的位置天然一致。
//
// 规划：单轴梯形速度（Speed × 倍率，加速/减速时间取自 AxisItem.Accel/Decel），
//       刹停约束 v ≤ √(2·a·|剩余距离|) 保证无过冲；软限位使能时目标与实际位置钳制。
// S 型曲线（AxisItem.SpeedCurve）为预留项，当前统一按梯形处理。
// =====================================================================
using System;
using System.Collections.Generic;

namespace NoCodeMotion.Services
{
    /// <summary>2D 仿真单轴运行态（单位与 AxisItem.Unit 一致，通常 mm）。</summary>
    public sealed class Sim2DAxis
    {
        public string Name = "";
        public string Unit = "mm";

        public double Target;                       // 指令位置（每帧与 AxisRuntimeState 同步）
        public double Act;                          // 仿真实际位置（本内核规划产出）
        public double Vel;                          // 当前速度（单位/s）

        public double Speed = 100;                  // 额定速度（单位/s，取 AxisItem.Speed）
        public double AccelTime = 0.2;              // 加速时间 s（AxisItem.Accel）
        public double DecelTime = 0.2;              // 减速时间 s（AxisItem.Decel）
        public double LimitPlus = 100;              // 软正限位（AxisItem.PosLimitPlus）
        public double LimitMinus = -100;            // 软负限位（AxisItem.PosLimitMinus）
        public bool SoftLimitEnable = true;         // 软限位使能（AxisItem.SoftLimitEnable）
        public double InPos = 0.01;                 // 到位误差（AxisItem.InPosError）

        /// <summary>点动推速（已含仿真倍率，单位/s）。0 = 未点动；按住按钮时由视图设置。</summary>
        public double JogVel;

        public bool Moving => Math.Abs(Target - Act) > InPos || Math.Abs(Vel) > 1e-9;
    }

    /// <summary>
    /// 2D 运控仿真内核。UI 层每帧调用 <see cref="Tick"/> 推进规划并读取各轴
    /// <see cref="Sim2DAxis.Act"/> 渲染；轨迹采样在内核内完成（XY 平面）。
    /// 仅在 UI 线程调用（DispatcherTimer），无内部加锁。
    /// </summary>
    public sealed class Sim2DMotion
    {
        public List<Sim2DAxis> Axes { get; } = new();

        /// <summary>仿真倍率（0.01~1）：作用于额定速度与点动速度，便于慢速观察轨迹。</summary>
        public double SpeedScale { get; set; } = 0.1;

        /// <summary>XY 轨迹采样点（世界坐标）。容量上限 <see cref="TraceCapacity"/>，超出后丢弃最早的。</summary>
        public List<(double X, double Y)> Trace { get; } = new();
        public int TraceCapacity { get; set; } = 4000;

        private double _lastTraceX = double.NaN, _lastTraceY = double.NaN;
        private const double TraceMinStep = 0.25;   // 轨迹采样最小间距（世界单位）

        /// <summary>按轴名取轴（大小写不敏感）；不存在返回 null。</summary>
        public Sim2DAxis? Find(string name)
        {
            foreach (var a in Axes)
                if (string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)) return a;
            return null;
        }

        /// <summary>
        /// 推进一个时间片。dt 为秒（UI 层用真实时间差，建议钳制 ≤0.1s）。
        /// </summary>
        public void Tick(double dt)
        {
            if (dt <= 0 || dt > 0.5) return;
            bool anyMove = false;

            foreach (var a in Axes)
            {
                // ---- 1. 指令位置同步 ----
                if (a.JogVel != 0)
                {
                    a.Target += a.JogVel * dt;
                    if (a.SoftLimitEnable)
                    {
                        if (a.Target > a.LimitPlus) a.Target = a.LimitPlus;
                        if (a.Target < a.LimitMinus) a.Target = a.LimitMinus;
                    }
                    AxisRuntimeState.Set(a.Name, a.Target);   // 点动期间由内核持有目标并回写共享态
                }
                else
                {
                    double cmd = AxisRuntimeState.Get(a.Name);
                    if (!double.IsNaN(cmd) && !double.IsInfinity(cmd)) a.Target = cmd;
                }

                // ---- 2. 梯形速度规划一步 ----
                double dist = a.Target - a.Act;
                if (Math.Abs(dist) <= a.InPos && Math.Abs(a.Vel) < 1e-6)
                {
                    a.Act = a.Target; a.Vel = 0;
                    continue;
                }

                double speed = Math.Max(1e-4, a.Speed * SpeedScale);            // 单位/s
                double aT = a.AccelTime > 0.001 ? a.AccelTime : 0.001;          // 防除零
                double dT = a.DecelTime > 0.001 ? a.DecelTime : 0.001;
                double acc = speed / aT;
                double dec = speed / dT;

                // 刹停约束：当前距离下允许的最大速度（保证能在目标处减速到 0）
                double vBrake = Math.Sqrt(2 * dec * Math.Abs(dist));
                double vDes = Math.Sign(dist) * Math.Min(speed, vBrake);

                double dv = vDes - a.Vel;
                double maxDv = acc * dt;
                if (dv > maxDv) dv = maxDv; else if (dv < -maxDv) dv = -maxDv;
                a.Vel += dv;

                double step = a.Vel * dt;
                if (Math.Abs(step) >= Math.Abs(dist) - a.InPos)
                {
                    // 本步到达目标（刹停约束下 Vel 已接近小值，直接贴合）
                    a.Act = a.Target; a.Vel = 0;
                }
                else
                {
                    a.Act += step;
                    if (a.SoftLimitEnable)
                    {
                        if (a.Act > a.LimitPlus) { a.Act = a.LimitPlus; a.Vel = 0; }
                        if (a.Act < a.LimitMinus) { a.Act = a.LimitMinus; a.Vel = 0; }
                    }
                }

                if (a.Moving) anyMove = true;
            }

            // ---- 3. 轨迹采样（XY 平面）----
            var x = Axes.Count > 0 ? Axes[0] : null;
            var y = Axes.Count > 1 ? Axes[1] : null;
            if (anyMove && x != null && y != null)
            {
                if (double.IsNaN(_lastTraceX) ||
                    Math.Abs(x.Act - _lastTraceX) >= TraceMinStep ||
                    Math.Abs(y.Act - _lastTraceY) >= TraceMinStep)
                {
                    Trace.Add((x.Act, y.Act));
                    _lastTraceX = x.Act; _lastTraceY = y.Act;
                    if (Trace.Count > TraceCapacity) Trace.RemoveRange(0, Trace.Count - TraceCapacity);
                }
            }
        }

        /// <summary>清空轨迹。</summary>
        public void ClearTrace()
        {
            Trace.Clear();
            _lastTraceX = _lastTraceY = double.NaN;
        }

        /// <summary>急停：取消所有点动，目标贴合当前位置并回写共享态（外部流程随后写入会重新接管）。</summary>
        public void StopAll()
        {
            foreach (var a in Axes)
            {
                a.JogVel = 0;
                a.Target = a.Act;
                a.Vel = 0;
                AxisRuntimeState.Set(a.Name, a.Act);
            }
        }
    }
}
// ◇作者保留所有权利　请勿删除※
