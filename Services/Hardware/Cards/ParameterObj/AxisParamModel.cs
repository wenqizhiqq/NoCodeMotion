﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionParameterObj/AxisParamModel.cs
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
    public class AxisParamModel
    {
        /// <summary>
        /// 轴ID
        /// </summary>
        public int AxisID { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public int Name { get; set; }

        /// <summary>
        /// 轴运行状态
        /// </summary>
        public int RunState { get; set; }

        /// <summary>
        /// 使能状态
        /// </summary>
        public int Enable { get; set; }

        /// <summary>
        /// 位置
        /// </summary>
        public int Postion { get; set; }

        /// <summary>
        /// 脉冲当量
        /// </summary>
        public int Equiv { get; set; }

        /// <summary>
        /// 高电平/低电平
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 报警
        /// </summary>
        public int Alarm { get; set; }

        /// <summary>
        /// EtherCAT 总线轴地址
        /// </summary>
        public int SlaveAddr { get; set; }

        /// <summary>
        /// EtherCAT 总线轴子地址
        /// </summary>
        public int SubSlaveAddr { get; set; }


    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
