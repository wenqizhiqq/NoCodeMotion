// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using NoCodeMotion.Models;
using NoCodeMotion.Services.Hardware.Cards;
using NoCodeMotion.Services.Hardware.Leadshine;

namespace NoCodeMotion.Services.Hardware
{
    /// <summary>底层读到的「原始」轴状态（尚未按位定义解释）。</summary>
    public struct AxisRawRead
    {
        /// <summary>是否读成功（false 时看 <see cref="Message"/>）。</summary>
        public bool Ok;
        /// <summary>失败原因（可直接显示）。</summary>
        public string Message;
        /// <summary>指令位置（单位同 axis.Unit）。</summary>
        public double Position;
        /// <summary>编码器反馈位置（单位同 axis.Unit）。</summary>
        public double Encoder;
        /// <summary>是否在运动中。</summary>
        public bool Moving;
        /// <summary>报警 / 错误码（0 = 无）。</summary>
        public int AlarmCode;
        /// <summary>是否有有效的「轴状态字」。</summary>
        public bool HasIoWord;
        /// <summary>轴状态字（dmc_axis_io_status 布局）。</summary>
        public uint IoWord;
        /// <summary>总线轴 CiA402 状态机；-1 = 不适用 / 读不到。</summary>
        public int StateMachine;
    }

    /// <summary>
    /// 解释后的轴状态，供「轴状态与控制」表绑定。
    /// 可空布尔表示「该项读不到」，界面显示为「—」，绝不编造状态。
    /// </summary>
    public sealed class AxisStatusSnapshot
    {
        public bool Connected;
        public string Message = "未连接";
        public double Position;
        public double Encoder;
        public bool Moving;
        public bool? Alarm;
        public bool? Pel;
        public bool? Mel;
        public bool? Org;
        public bool? Emg;
        public bool? Enabled;
        public uint IoWord;
        public bool HasIoWord;
        public int StateMachine = -1;

        public string PosText => Connected ? Position.ToString("0.###") : "—";
        public string EncText => Connected ? Encoder.ToString("0.###") : "—";
        public string MovingText => !Connected ? "—" : (Moving ? "运动中" : "停止");
        public string AlarmText => Fmt(Alarm);
        public string PelText => Fmt(Pel);
        public string MelText => Fmt(Mel);
        public string OrgText => Fmt(Org);
        public string EmgText => Fmt(Emg);
        public string EnabledText => Fmt(Enabled);

        private static string Fmt(bool? v) => v == null ? "—" : (v.Value ? "● 有" : "○ 无");
    }

    /// <summary>
    /// 轴监控服务：统一「读底层轴状态」与「下发手动指令」。
    ///
    ///   卡族模式（<see cref="HardwareMode.CardFamilies"/>）：走 <see cref="WenQiZhiCardBridge.TryReadAxisRaw"/>；
    ///   雷赛模式（<see cref="HardwareMode.Leadshine"/>）：走 <see cref="LeadshineHardwareBridge.TryReadAxisRaw"/>；
    ///   仿真模式：返回「未连接」，不造任何假状态。
    ///
    /// ★ 所有方法是「真实硬件调用」，**必须在后台线程调用**（UI 线程调用会卡界面）。
    /// </summary>
    public static class AxisMonitorService
    {
        /// <summary>
        /// 轴状态字位定义（dmc_axis_io_status 标准布局，与卡族 AxisRealization 里的注释一致）：
        /// bit0 报警 / bit1 正限位 / bit2 负限位 / bit3 急停 / bit4 原点 /
        /// bit5 到位 / bit6 正软限位 / bit7 负软限位 / bit8 使能。
        /// </summary>
        public const string IoWordHint =
            "状态字 bit0 报警 / bit1 正限位 / bit2 负限位 / bit3 急停 / bit4 原点 / " +
            "bit5 到位 / bit6 正软限位 / bit7 负软限位 / bit8 使能（以你的卡手册为准）";

        /// <summary>读一个轴的实时状态（后台线程调用）。</summary>
        public static AxisStatusSnapshot Read(AxisItem axis)
        {
            if (axis == null) return new AxisStatusSnapshot { Message = "无轴" };

            if (HardwareSetup.Mode == HardwareMode.CardFamilies)
            {
                var b = HardwareSetup.CardFamilies;
                if (b == null) return new AxisStatusSnapshot { Message = "卡族层未装配" };
                if (!b.TryReadAxisRaw(axis, out var raw))
                    return new AxisStatusSnapshot { Message = string.IsNullOrEmpty(raw.Message) ? "未连接" : raw.Message };
                return Decode(raw);
            }

            if (HardwareSetup.Mode == HardwareMode.Leadshine)
            {
                var b = HardwareBridge.Current as LeadshineHardwareBridge;
                if (b == null) return new AxisStatusSnapshot { Message = "雷赛层未装配" };
                if (!b.TryReadAxisRaw(axis, out var raw))
                    return new AxisStatusSnapshot { Message = string.IsNullOrEmpty(raw.Message) ? "控制卡未就绪" : raw.Message };
                return Decode(raw);
            }

            return new AxisStatusSnapshot { Message = "仿真模式（未接硬件）" };
        }

        private static AxisStatusSnapshot Decode(AxisRawRead raw)
        {
            var s = new AxisStatusSnapshot
            {
                Connected = true,
                Message = "已连接",
                Position = raw.Position,
                Encoder = raw.Encoder,
                Moving = raw.Moving,
                IoWord = raw.IoWord,
                HasIoWord = raw.HasIoWord,
                StateMachine = raw.StateMachine,
            };

            if (raw.HasIoWord)
            {
                uint w = raw.IoWord;
                s.Alarm = (w & (1u << 0)) != 0;
                s.Pel = (w & (1u << 1)) != 0;
                s.Mel = (w & (1u << 2)) != 0;
                s.Emg = (w & (1u << 3)) != 0;
                s.Org = (w & (1u << 4)) != 0;
                s.Enabled = (w & (1u << 8)) != 0;
            }
            else if (raw.AlarmCode != 0)
            {
                s.Alarm = true;   // 没有状态字时，报警退化成「错误码是否非 0」
            }

            // 总线轴：CiA402 状态机 = 4（操作使能）才算真正使能，优先用它覆盖位读数。
            if (raw.StateMachine >= 0) s.Enabled = raw.StateMachine == 4;

            return s;
        }

        // ===================== 手动指令（全部为真实硬件调用，后台线程执行） =====================

        /// <summary>使能轴（真实下发）。</summary>
        public static void Enable(AxisItem axis) => HardwareBridge.Current?.EnableAxis(axis);

        /// <summary>停止轴（减速停止）。</summary>
        public static void Stop(AxisItem axis) => HardwareBridge.Current?.StopAxis(axis);

        /// <summary>回零（阻塞至回零完成或超时，务必在后台线程调用）。</summary>
        public static void Home(AxisItem axis) => HardwareBridge.Current?.HomeAxis(axis);

        /// <summary>
        /// 点动一段距离（增量移动，正负决定方向）。
        /// <paramref name="speed"/> &gt; 0 时按它作为本次点动速度（手动速度），否则退回轴配置的「运行速度」。
        /// 返回 null 表示成功，否则是失败原因。
        /// </summary>
        public static string Inch(AxisItem axis, double delta, double speed = 0)
        {
            if (axis == null) return "无轴";
            if (delta == 0) return "点动距离为 0";

            if (HardwareSetup.Mode == HardwareMode.CardFamilies)
                return HardwareSetup.CardFamilies?.InchAxis(axis, delta, speed) ?? "卡族层未装配";
            if (HardwareSetup.Mode == HardwareMode.Leadshine)
                return (HardwareBridge.Current as LeadshineHardwareBridge)?.InchAxis(axis, delta, speed) ?? "雷赛层未装配";

            HardwareBridge.Current?.MoveAxisRel(axis, delta);   // 仿真：只打日志
            return null;
        }

        /// <summary>
        /// 启动连续点动（Jog）：按住持续走，松开调 <see cref="Stop"/>。
        /// 卡族走 CardAxisSerialMovement，雷赛走 dmc_vmove。
        /// <paramref name="speed"/> &gt; 0 时按它作为本次 Jog 的速度（手动速度），否则退回轴配置的「运行速度」。
        /// 返回 null 表示成功，否则是失败原因。
        /// </summary>
        public static string StartJog(AxisItem axis, bool positive, double speed = 0)
        {
            if (axis == null) return "无轴";
            if (HardwareSetup.Mode == HardwareMode.CardFamilies)
                return HardwareSetup.CardFamilies?.StartAxisJog(axis, positive, speed) ?? "卡族层未装配";
            if (HardwareSetup.Mode == HardwareMode.Leadshine)
                return (HardwareBridge.Current as LeadshineHardwareBridge)?.StartAxisJog(axis, positive, speed) ?? "雷赛层未装配";
            return "仿真模式：未接硬件，Jog 只记录日志";
        }

        /// <summary>把当前指令位置置零（设零点）。返回 null 表示成功，否则是失败原因。</summary>
        public static string SetZero(AxisItem axis)
        {
            if (axis == null) return "无轴";
            if (HardwareSetup.Mode == HardwareMode.CardFamilies)
                return HardwareSetup.CardFamilies?.SetAxisZero(axis) ?? "卡族层未装配";
            if (HardwareSetup.Mode == HardwareMode.Leadshine)
                return (HardwareBridge.Current as LeadshineHardwareBridge)?.SetAxisZero(axis) ?? "雷赛层未装配";
            return "仿真模式：未接硬件，设零点只记录日志";
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
