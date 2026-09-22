﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionParameterObj/LatchParamModel.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samsun.Domain.MotionCard.Common
{
    /// <summary>
    /// 锁存参数
    /// </summary>
    public class LatchParamModel
    {

        /// <summary>
        /// 控制卡卡号
        /// </summary>
        public int CardNo { get; set; }

        /// <summary>
        /// 轴
        /// </summary>
        public AxisParamModel Axis{get; set;}

        /// <summary>
        /// 锁存器号，0-3
        /// </summary>
        public int Latch { get; set; }

        /// <summary>
        /// 锁存模式，0：单次锁存，1：连续锁存
        /// </summary>
        public int LtcMode { get; set; }

        /// <summary>
        /// 锁存信号的触发方式，0：下降沿锁存，1：上升沿锁存，2：双边沿锁存
        /// </summary>
        public int LtcLogic { get; set; }

        /// <summary>
        /// 滤波时间，单位：us
        /// </summary>
        public int Filter { get; set; }

        /// <summary>
        /// 锁存器辅助编码器通道号，0、1
        /// </summary>
        public int EncoderChannelNo { get; set; }

        /// <summary>
        /// 锁存源，0：指令位置，1：辅助编码器计数
        /// </summary>
        public int LtcSource { get; set; }


  

    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
