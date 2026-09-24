﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionParameterObj/PitchCompensationparamModel.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WenQiZhi.Domain.MotionCard.Common
{
    /// <summary>
    /// 螺距补偿参数
    /// </summary>
    public class PitchCompensationparamModel
    {

        /// <summary>
        /// 卡号
        /// </summary>
        public int CardNo { get; set; }

        /// <summary>
        /// 轴号数组
        /// </summary>
        public int Axis { get; set; }

        /// <summary>
        /// 设置螺距补偿参数 未知参数
        /// </summary>
        public int n { get; set; }

        /// <summary>
        /// 设置螺距补偿参数 未知参数
        /// </summary>
        public int StartPos { get; set; }

        /// <summary>
        /// 设置螺距补偿参数 未知参数
        /// </summary>
        public int LenPos { get; set; }


        /// <summary>
        /// 设置螺距补偿参数 未知参数
        /// </summary>
        public int[] PCompPos { get; set; }

        /// <summary>
        /// 设置螺距补偿参数 未知参数
        /// </summary>
        public int[] PCompNeg { get; set; }

        /// <summary>
        /// 螺距补偿：开启\关闭
        /// </summary>
        public int Enable { get; set; }

            



    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
