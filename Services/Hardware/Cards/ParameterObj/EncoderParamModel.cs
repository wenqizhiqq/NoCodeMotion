﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionParameterObj/EncoderParamModel.cs
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
    /// 编码器参数
    /// </summary>
    public class EncoderParamModel
    {
        /// <summary>
        /// 卡号
        /// </summary>
        public int CardNo { get; set; }

        /// <summary>
        /// 轴号数组
        /// </summary>
        public AxisParamModel[] Axis { get; set; }

        /// <summary>
        /// 编码器系数
        /// </summary>
        public int Factor { get; set; }


        /// <summary>
        /// 位置误差带，单位：pulse
        /// </summary>
        public int Error { get; set; }

        /// <summary>
        /// 编码器计数
        /// </summary>
        public int EncodeData { get; set; }

        /// <summary>
        /// 辅助编码器通道，0，通道 0，1，通道 1
        /// </summary>
        public int Channel { get; set; }

        /// <summary>
        /// 辅助编码器输入方式，0：脉冲+方向信号；1：A、B 相位正交信号
        /// </summary>
        public int InMode { get; set; }

        /// <summary>
        /// 辅助编码器计数模式，固定 1：4 倍频计数
        /// </summary>
        public int Multi { get; set; }

         

  

   
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
