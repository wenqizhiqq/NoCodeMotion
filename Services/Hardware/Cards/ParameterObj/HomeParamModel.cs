﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionParameterObj/HomeParamModel.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable



namespace Samsun.Domain.MotionCard.Common
{ 
    /// <summary>
    /// 回原参数
    /// </summary>
    public class HomeParameModel
    {
        /// <summary>
        /// 总线卡回零偏移
        /// </summary>
        public double EtherCATHomeOffset { get; set; } = 0;

        /// <summary>
        /// 卡号
        /// </summary>
        public int CardNo { get; set; }

        /// <summary>
        /// 轴号
        /// </summary>
        public int Axis { get; set; }

        ///// <summary>
        ///// 正负方向
        ///// </summary>
        //public int SignDirection { get; set; }

        /// <summary>
        /// 回零模式
        /// </summary>
        public int HomeMode { get; set; }


        /// <summary>
        /// 回原速度 0低速  1高速
        /// </summary>
        public int HomeSpeed { get; set; }

        /// <summary>
        /// 运行速度
        /// </summary>
        public double Vel { get; set; }

        /// <summary>
        /// 起始速度
        /// </summary>
        public double StartVel { get; set; }

        /// <summary>
        /// 低速
        /// </summary>
        public double LowVel { get; set; }

        /// <summary>
        /// 高速
        /// </summary>
        public double HighVel { get; set; }

        /// <summary>
        /// 减速时间
        /// </summary>
        public double TdecTime { get; set; }

        /// <summary>
        /// 加速时间
        /// </summary>
        public double TaccTime { get; set; }

        /// <summary>
        /// 偏移
        /// </summary>
        public double OffSetPos { get; set; }

        /// <summary>
        /// 遇限位是否反找
        /// </summary>
        public bool ELEnable { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public bool State { get; set; }

        /// <summary>
        /// 设置Home信号 未知参数
        /// </summary>
        public int OrgLogic { get; set; }

        /// <summary>
        /// 使用回零信号
        /// </summary>
        public LimitTypeEnum UseHomeSignal { get; set; }
        /// <summary>
        ///设置Home信号 未知参数
        /// </summary>
        public int Filter { get; set; }

        /// <summary>
        ///  设置回原方向
        /// </summary>
        public int HomeDir { get; set; }

        /// <summary>
        /// 设置回原 未知参数
        /// </summary>
        public int EZcount { get; set; }

        ///// <summary>
        ///// 设置回原 未知参数
        ///// </summary>
        //public int mode { get; set; }



        /// <summary>
        /// 原点信号的有效电平； 0 – 低有效； 1 – 高有效
        /// </summary>
        public int OrgLevel { get; set; }

        /// <summary>
        /// 编码器的 index 信号有效电平；0 – 低有效； 1 – 高有效
        /// </summary>
        public int EZlevel { get; set; }

        /// <summary>
        /// 感应负限后的移动距离
        /// </summary>
        public double HomeTrigNegMoveDir { get; set; } = 1000;


        /// <summary>
        /// 感应正限后的移动距离
        /// </summary>
        public double HomeTrigPosMoveDir { get; set; } = 1000;

    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
