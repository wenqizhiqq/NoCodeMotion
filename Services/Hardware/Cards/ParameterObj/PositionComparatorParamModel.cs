﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionParameterObj/PositionComparatorParamModel.cs
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
    /// 位置比较器参数
    /// </summary>
    public class PositionComparatorParamModel
    {
        /// <summary>
        /// 卡号
        /// </summary>
        public int CardNo { get; set; }

        /// <summary>
        /// 轴号数组
        /// </summary>
        public int[] Axis { get; set; }
        /// <summary>
        /// 比较功能状态，0：禁止，1：使能
        /// </summary>
        public int Enable { get; set; }

        /// <summary>
        /// 轴比较起始位置：0：指令位置，1：反馈位置
        /// </summary>
        public int[] CmpSource { get; set; }

        /// <summary>
        /// 比较器输出端口号，取值范围：0~5（对应硬件 OUT2~OUT7 端口） 
        /// </summary>
        public int Hcmp { get; set; }
        /// <summary>
        /// 比较模式：0：进入误差带后触发  |  1：进入误差带单轴等于后再触发
        /// </summary>
        public int[] CmpMode { get; set; }
        /// <summary>
        /// 关联轴号
        /// </summary>
        public int[] AxisNo { get; set; }

        /// <summary>
        /// 轴误差带设置，单位：unit 
        /// </summary>
        public int Error { get; set; }
        /// <summary>
        /// 有效电平：0：低电平，1：高电
        /// </summary>
        public int CmpLogic { get; set; }

        /// <summary>
        /// 脉冲宽度，单位：us，取值范围：1us-20s 
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        /// pwm 模式使能
        /// </summary>
        public int PwmEnable { get; set; }
        /// <summary>
        /// 占空比
        /// </summary>                                                                 
        public int Duty { get; set; }
        /// <summary>
        /// 频率
        /// </summary>
        public int Freq { get; set; }
        /// <summary>
        /// 端口
        /// </summary>
        public int PortSel { get; set; }
        /// <summary>
        /// 输出的 pwm 脉冲数
        /// </summary>
        public int PwmNumber { get; set; }
        /// <summary>
        /// 队列模式下：添加 x 比较位置，单位：unit 
        /// </summary>
        public int[] XCmpPos { get; set; }
        /// <summary>
        /// 队列模式下：添加 y 比较位置，单位：unit 
        /// </summary>
        public int  YCmpPos { get; set; }
        /// <summary>
        /// 位置增量值，单位：pulse（正值表示位置递增，负值表示位置递减）
        /// </summary>
        public int  Increment { get; set; }
        /// <summary>
        /// 比较次数，取值范围：1~65535
        /// </summary>
        public int Count { get; set; }

    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
