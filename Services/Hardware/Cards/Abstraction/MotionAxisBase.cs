using WenQiZhi.Domain.MotionCard.Common;

namespace NoCodeMotion.Hardware.Motion
{
    /// <summary>
    /// 一根轴的抽象基类：身份 + 运动 + 使能 + 回零 + 位置 + 状态。
    /// <para>卡族子类把厂商 SDK 包进这些动词即可；上层只依赖这套动词，不再依赖任何接口。</para>
    /// </summary>
    public abstract class MotionAxisBase
    {
        // ---------- 轴身份 ----------
        /// <summary>轴显示名（工程里用户起的名字）。</summary>
        public abstract string DisplayName { get; set; }

        /// <summary>所属卡的卡号。</summary>
        public abstract int CardIndex { get; set; }

        /// <summary>所属卡的卡族名。</summary>
        public abstract string CardName { get; set; }

        /// <summary>所属卡的型号字符串。</summary>
        public abstract string CardType { get; set; }

        /// <summary>卡内轴号。</summary>
        public abstract int AxisIndex { get; set; }

        /// <summary>轴已上线（已绑定到一根真实轴）。</summary>
        public abstract bool IsOnline { get; set; }

        /// <summary>所在卡为虚拟卡。</summary>
        public abstract bool IsVirtualCard { get; set; }

        /// <summary>本轴的参数块（电平/软限位/编码器…）。</summary>
        public abstract CAxisParameter ConfigBlock { get; set; }

        // ---------- 运动 ----------
        /// <summary>定位运动（点位式，参数块带目标位置与速度）。</summary>
        public abstract int MoveToPoint(MotionParamModel plan);

        /// <summary>连续运动（速度式，走指令队列）。</summary>
        public abstract int MoveSequential(MotionParamModel plan);

        /// <summary>下发 T 型速度曲线参数。</summary>
        public abstract int ConfigureProfile(MotionParamModel plan);

        /// <summary>下发运行速度。</summary>
        public abstract int ConfigureVelocity(MotionParamModel plan);

        /// <summary>停止运动（stopMode：0 减速停止 / 1 急停）。</summary>
        public abstract int Halt(int cardIndex, int axisIndex, int stopMode);

        /// <summary>设置原点/限位与减速点的空间间距。</summary>
        public abstract int SetSensorSpacing(int axisIndex, int orgPos, int pelPos, int melPos);

        // ---------- 使能 ----------
        /// <summary>接通伺服使能。</summary>
        public abstract int EngageServo(int cardIndex, int axisIndex);

        /// <summary>断开伺服使能。</summary>
        public abstract int ReleaseServo(int cardIndex, int axisIndex);

        /// <summary>写使能引脚电平（0/1）。</summary>
        public abstract int WriteEnablePin(int level);

        /// <summary>读使能引脚电平。</summary>
        public abstract int ReadEnablePin();

        // ---------- 回零 ----------
        /// <summary>启动回零动作。</summary>
        public abstract int RunHome();

        /// <summary>查询回零结果状态。</summary>
        public abstract int ReadHomeResult(int cardIndex, int axisIndex, ref ushort state);

        /// <summary>下发回零参数（速度段/偏置）。</summary>
        public abstract int ConfigureHome(HomeParameModel profile);

        /// <summary>下发回零方式（就近/限位的组合）。</summary>
        public abstract int ConfigureHomeMode(HomeParameModel profile);

        // ---------- 位置 ----------
        /// <summary>设置脉冲当量。</summary>
        public abstract int SetPulseEquivalent(int cardIndex, int axisIndex, int pulsesPerUnit);

        /// <summary>读当前位置（counterSelector：0 指令脉冲 / 1 编码器反馈）。</summary>
        public abstract double ReadPosition(int counterSelector);

        /// <summary>改写当前位置计数器（设零点）。</summary>
        public abstract int WritePosition(int cardIndex, int axisIndex, int pulses);

        // ---------- 状态 ----------
        /// <summary>读轴运动状态（0 停止 / 1 运行中）。</summary>
        public abstract int ReadState();

        /// <summary>读报警状态。</summary>
        public abstract int ReadAlarm(int cardIndex, int axisIndex);

        /// <summary>读停止原因。</summary>
        public abstract int ReadStopReason(int cardIndex, int axisIndex, ref int stopReason);

        /// <summary>读轴 IO 状态字（bit1 正限位 / bit2 负限位 / bit4 原点 …）；卡族未提供时返回 -1。</summary>
        public virtual int ReadStatusWord() => -1;

        /// <summary>把厂商错误码翻成人话；卡族未提供时返回空串。</summary>
        public virtual string ReadAlarmText(int code) => string.Empty;

        /// <summary>读指令缓冲区占用（用于 Jog 前诊断）；卡族未提供时返回 int.MinValue。</summary>
        public virtual int ReadBufferDepth(int cardIndex) => int.MinValue;
    }
}
