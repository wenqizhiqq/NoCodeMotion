﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionParameterObj/MotionParamModel.cs
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
    public class MotionParamModel
    {
        /// <summary>
        /// 卡号
        /// </summary>
        public int CardNo { get; set; }

        /// <summary>
        /// 轴号
        /// </summary>
        public int Axis { get; set; }

        /// <summary>
        /// 运动模式
        /// </summary>
        public int RunMode { get; set; }

        /// <summary>
        /// 脉冲当量，单位：pulse/unit
        /// </summary>
        public int Equiv { get; set; }

        /// <summary>
        /// 位置值，单位：unit
        /// </summary>
        public int Pos { get; set; }

        /// <summary>
        /// 起始速度，单位：unit/s
        /// </summary>
        public double MinVel { get; set; }

        /// <summary>
        /// 最大速度，单位：unit
        /// </summary>
        public double MaxVel { get; set; }

        /// <summary>
        /// 加速时间，单位：s
        /// </summary>
        public double TaccVel { get; set; }

        /// <summary>
        /// 减速时间，单位：s
        /// </summary>
        public double TdecVel { get; set; }

        /// <summary>
        /// 停止速度，单位：unit/s
        /// </summary>
        public int StopVel { get; set; }

        /// <summary>
        /// 保留参数，固定值为 0
        /// </summary>
        public int SMode { get; set; }

        /// <summary>
        /// S 段时间，单位：s；范围：0~1
        /// </summary>
        public double SPara { get; set; }

        /// <summary>
        /// 目标位置，单位：unit
        /// </summary>
        public double Dist { get; set; }

        /// <summary>
        /// 运动模式，0：相对坐标模式，1：绝对坐标模式
        /// </summary>
        public int PosiMode { get; set; }

        /// <summary>
        /// 方向运动方向，0：负方向，1：正方向
        /// </summary>
        public int Dir { get; set; }

        /// <summary>
        /// 新的运行速度，单位：unit/s
        /// </summary>
        public int NewVel { get; set; }

        /// <summary>
        /// 变速时间，单位：s 
        /// </summary>
        public int TaccDecTime { get; set; }

        /// <summary>
        /// 新目标位置，单位：unit
        /// </summary>
        public int NewPos { get; set; }

        /// <summary>
        /// 手轮输入方式，0：脉冲+方向信号；1：A、B 相位正交信号
        /// </summary>
        public int InMode { get; set; }

        /// <summary>
        /// 手轮倍率，正数表示默认方向，负数表示与默认方向反向
        /// </summary>
        public int Multi { get; set; }

        /// <summary>
        /// 保留参数，固定值为 0
        /// </summary>
        public int vh { get; set; }

        /// <summary>
        /// 参与手轮运动的轴数
        /// </summary>
        public int AxisNum { get; set; }

        /// <summary>
        /// 参与手轮运动的轴号数组
        /// </summary>
        public int[] AxisList { get; set; }

        /// <summary>
        /// 能状态，0：禁止，1：允许
        /// </summary>
        public int Enable { get; set; }

        /// <summary>
        /// 有效电平：0：低电平，1：高电
        /// </summary>
        public int EmgLogic { get; set; }


        /// <summary>
        /// 过程使用 S 曲线的比例，范围为 0~1; 0 表示没有 S 曲线部分(也就是 T 型曲线加减速) | 1 表示全部 S 曲线加减速；
        /// </summary>
        public double JerkPercent { get; set; }

        /// <summary>
        /// 模块运动方式
        /// </summary>
        public SACMotionMethodEnum SACMotionMethod { get; set; }
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
