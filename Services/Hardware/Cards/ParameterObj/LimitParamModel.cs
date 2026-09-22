﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionParameterObj/LimitParamModel.cs
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
    /// 限位参数
    /// </summary>
    public class LimitParamModel
    {
        /// <summary>
        /// 限位名称
        /// </summary>
        public string LimitName { get; set; }
        /// <summary>
        /// 控制卡卡号
        /// </summary>
        public int CardNo{ get; set; }

        /// <summary>
        /// 轴编号
        /// </summary>
        public int Axis { get; set; }

        /// <summary>
        /// 限位信号状态，0：禁止，1：允许
        /// </summary>
        public int Enable { get; set; }

        /// <summary>
        /// 计数器选择，0：指令位置计数器，1：编码器
        /// </summary>
        public int SourceSel { get; set; }

        /// <summary>
        /// 限位停止方式，0：立即停止 1：减速停止
        /// </summary>
        public int SLAction { get; set; }

        /// <summary>
        /// 限位位置(安全)，单位：pulse
        /// </summary>
        public int LimitPosition { get; set; }

        /// <summary>
        /// 返回负限位脉冲数
        /// </summary>
        public int Nlimit{ get; set; }
        /// <summary>
        /// 返回正限位脉冲数
        /// </summary>
        public int Plimit{ get; set; }


        /// <summary>
        ///<para> 限位信号电平选择</para> <para>0 低电平有效</para> <para>1 高电平有效</para>
        /// </summary>
        public int ActiveLevel { get; set; }
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
