using System.Collections.Generic;
using WenQiZhi.Domain.MotionCard.Common;

namespace NoCodeMotion.Hardware.Motion
{
    // =====================================================================
    //  运动控制卡抽象层（抽象类，不使用接口）
    //
    //  设计要点：
    //  · 一张物理卡 = MotionCardBase 的一个子类；一根轴 = MotionAxisBase 的一个子类；
    //    开关量与扩展 IO 各有自己的抽象基类。
    //  · 抽象成员一律「英文动宾式」命名（Open / Setup / MoveToPoint / ReadPosition …），
    //    与卡族内部的厂家函数命名彻底分离：卡族只需把厂商 SDK 包进这些动词里，
    //    上层（桥接 / 流程 / 页面）只认这套动词。
    //  · 只有「每张卡都必须做到」的能力才声明为 abstract；
    //    可选能力（状态字、报警文本、指令缓冲深度…）声明为 virtual 并给出安全默认值，
    //    缺这个能力的卡族不必再写空实现。
    // =====================================================================

    /// <summary>一张运动控制卡的抽象基类：生命周期 + 卡级信息查询。</summary>
    public abstract class MotionCardBase
    {
        // ---------- 卡身份与状态 ----------
        /// <summary>句柄已打开。</summary>
        public abstract bool IsOpen { get; set; }

        /// <summary>初始化已完成。</summary>
        public abstract bool IsReady { get; set; }

        /// <summary>本卡是否为虚拟/数字孪生卡（无真实硬件，纯仿真）。</summary>
        public abstract bool IsVirtualCard { get; set; }

        /// <summary>总线上找到的卡数量。</summary>
        public abstract ushort CardCount { get; set; }

        /// <summary>输出点总数。</summary>
        public abstract int OutputPointCount { get; set; }

        /// <summary>输入点总数。</summary>
        public abstract int InputPointCount { get; set; }

        /// <summary>离线模式（不真正下发，只走本地逻辑）。</summary>
        public abstract bool IsOffline { get; set; }

        /// <summary>厂商标识。</summary>
        public abstract SystemSupportMotionCardEnum VendorKind { get; set; }

        /// <summary>卡参数表（各卡的参数行）。</summary>
        public abstract List<CardParamModel> ParameterRows { get; set; }

        // ---------- 生命周期 ----------
        /// <summary>打开卡：回填实际卡数量、卡型号与卡号。</summary>
        public abstract int Open(ref ushort cardCount, ref uint[] cardTypes, ref ushort[] cardIds);

        /// <summary>初始化卡（下载参数、建立通讯）。</summary>
        public abstract int Setup();

        /// <summary>释放卡（关闭句柄）。</summary>
        public abstract int Release();

        /// <summary>读取某张卡的轴总数。</summary>
        public abstract int ReadAxisCount(int cardIndex, ref uint axisCount);

        /// <summary>复位整张卡。</summary>
        public abstract int ResetCard(int cardIndex);

        /// <summary>把厂商错误码翻成人话；卡族未提供时返回空串。</summary>
        public virtual string ReadAlarmText(int code) => string.Empty;
    }
}
