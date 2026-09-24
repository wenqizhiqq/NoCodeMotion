﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionParameterObj/InPutParamModel.cs
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
    /// 输入参数
    /// </summary>
    public class InPutParamModel
    {
        /// <summary>
        /// 控制卡卡号
        /// </summary>
        public int CardNo { get; set; }


        /// <summary>
        /// 名称
        /// </summary>
        public int Name { get; set; }

        /// <summary>
        /// 输出端口号
        /// </summary>
        public int BitNo { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// IO 计数模式，0：禁用，1：上升沿计数，2：下降沿计数
        /// </summary>
        public int Mode { get; set; }

        /// <summary>
        /// 滤波时间，单位：s，保留参数
        /// </summary>
        public int FilterTime { get; set; }

        /// <summary>
        /// IO 计数值
        /// </summary>
        public int CountValue { get; set; }


    

    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
