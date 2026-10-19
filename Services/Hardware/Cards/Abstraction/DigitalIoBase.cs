namespace NoCodeMotion.Hardware.Motion
{
    /// <summary>卡载开关量输入端口的抽象基类。</summary>
    public abstract class DigitalInputBase
    {
        /// <summary>所属卡号。</summary>
        public abstract int CardIndex { get; set; }

        /// <summary>输入点总数。</summary>
        public abstract ushort PointCount { get; set; }

        /// <summary>端口组名称。</summary>
        public abstract string ChannelName { get; set; }

        /// <summary>点位起始索引。</summary>
        public abstract int IndexStart { get; set; }

        /// <summary>默认有效电平是否高。</summary>
        public abstract bool ActiveHighByDefault { get; set; }

        /// <summary>虚拟 IO（仿真）。</summary>
        public abstract bool IsVirtual { get; set; }

        /// <summary>读一个输入端口/点的状态。</summary>
        public abstract int ReadBit(int cardIndex, int portIndex);
    }

    /// <summary>卡载开关量输出端口的抽象基类。</summary>
    public abstract class DigitalOutputBase
    {
        /// <summary>所属卡号。</summary>
        public abstract int CardIndex { get; set; }

        /// <summary>输出点总数。</summary>
        public abstract ushort PointCount { get; set; }

        /// <summary>端口组名称。</summary>
        public abstract string ChannelName { get; set; }

        /// <summary>点位起始索引。</summary>
        public abstract int IndexStart { get; set; }

        /// <summary>默认有效电平是否高。</summary>
        public abstract bool ActiveHighByDefault { get; set; }

        /// <summary>虚拟 IO（仿真）。</summary>
        public abstract bool IsVirtual { get; set; }

        /// <summary>读一个输出点的回读状态。</summary>
        public abstract int ReadBit(int cardIndex, int bitIndex);

        /// <summary>写一个输出点（level：0 断开 / 1 接通）。</summary>
        public abstract int WriteBit(int cardIndex, int bitIndex, int level);
    }
}
