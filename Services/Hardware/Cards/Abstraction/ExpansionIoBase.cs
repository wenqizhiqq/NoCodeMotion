namespace NoCodeMotion.Hardware.Motion
{
    /// <summary>扩展 IO 模块（挂在校验总线/现场总线上的从站）输入的抽象基类。</summary>
    public abstract class ExpansionInputBase
    {
        /// <summary>所属卡号。</summary>
        public abstract int CardIndex { get; set; }

        /// <summary>输入点总数。</summary>
        public abstract ushort PointCount { get; set; }

        /// <summary>模块名称。</summary>
        public abstract string ChannelName { get; set; }

        /// <summary>默认有效电平是否高。</summary>
        public abstract bool ActiveHighByDefault { get; set; }

        /// <summary>读某个从站节点的输入位。</summary>
        public abstract int ReadBit(int cardIndex, int nodeIndex, int bitIndex, ref ushort value);
    }

    /// <summary>扩展 IO 模块输出的抽象基类。</summary>
    public abstract class ExpansionOutputBase
    {
        /// <summary>所属卡号。</summary>
        public abstract int CardIndex { get; set; }

        /// <summary>输出点总数。</summary>
        public abstract ushort PointCount { get; set; }

        /// <summary>模块名称。</summary>
        public abstract string ChannelName { get; set; }

        /// <summary>默认有效电平是否高。</summary>
        public abstract bool ActiveHighByDefault { get; set; }

        /// <summary>写某个从站节点的输出位。</summary>
        public abstract int WriteBit(int cardIndex, int nodeIndex, int bitIndex, int level);

        /// <summary>读某个从站节点的输出回读位。</summary>
        public abstract int ReadBit(int cardIndex, int nodeIndex, int bitIndex, ref ushort value);
    }
}
