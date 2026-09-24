﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionParameterObj/CardParamModel.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WenQiZhi.Domain.MotionCard.Common
{
    /// <summary>
    /// 使用卡参数
    /// </summary>
    public class CardParamModel
    {
        /// <summary>
        /// 卡名字
        /// </summary>
        public string CardName { get; set; }

        /// <summary>
        /// 卡号，同型号多张卡时的编号
        /// </summary>
        public int CardNo { get; set; }

        /// <summary>
        /// 卡的类型
        /// </summary>
        public uint CardType { get; set; }

        /// <summary>
        /// 卡的注释信息
        /// </summary>
        public string CardRemarks { get; set; } = "";


        /// <summary>
        /// 动态库版本号
        /// </summary>
        public UInt32 LibVersion { get; set; }

        /// <summary>
        /// 轴数组
        /// </summary>
        public AxisParamModel[] Axes { get; set; }


        /// <summary>
        /// 输入端口号数，取值范围：0~7，如果扩展 IO 模块，依次往后累加
        /// </summary>
        public int InportNum { get; set; }

        /// <summary>
        /// 输出端口号数，取值范围：0~7，如果扩展 IO 模块，依次往后累加
        /// </summary>
        public int OutportNum { get; set; }

        /// <summary>
        /// 打印输出模式，0：只打印报错函数，1：全部打印，2：全部不打印
        /// </summary>
        public int Mode { get; set; }

        /// <summary>
        /// 参数文件
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 控制器工作模式，0 表示仿真模式，1 表示在线模式
        /// </summary>
        public int ControllerMode { get; set; }

        /// <summary>
        /// 0：停止 EtherCAT 总线成功，1：停止 EtherCAT 总线失败
        /// </summary>
        public int EtherCATState { get; set; }

        /// <summary>
        /// EtherCAT 总线循环周期，单位：us，支持 250/500/1000/2000
        /// </summary>
        public int CyclFieldbusTypeeTime { get; set; }

        /// <summary>
        /// 卡支持轴的数量
        /// </summary>
        public ushort CardSuportAxisNum { get; set; } = 8;


        #region 扩展IO卡

        [DisplayName("拓展IO模块节点值"), Description("如果是CAN总线IO扩展卡，要指定节点号，如果有多张用逗号分隔"), Category("扩展IO参数设置")]
        /// <summary>
        /// 如果是CAN总线IO扩展卡，要指定节点号，如果有多张用逗号分隔
        /// </summary>
        public string CANIOCardNodes { get; set; } = "";

        [DisplayName("设置拓展IO模块"), Description("不为null则启用扩展IO模块"), Category("扩展IO参数设置")]
        /// <summary>
        /// 卡是否设置拓展模块
        /// </summary>
        public ExtendIoEnum ExtendMode { get; set; } = ExtendIoEnum.Null;


        [DisplayName("设置拓展IO模块的数量"), Description("如果装多张扩展IO模块，请设置对应的数量"), Category("扩展IO参数设置")]

        /// <summary>
        /// 扩展IO模块的数量
        /// </summary>
        public ushort ExtendIOSum { get; set; } = 1;
        #endregion
        #region Rotor卡参数
        [DisplayName("机械手的数量"), Description("读取拓扑机械手数量，仅读取"), Category("Rotor参数"), ReadOnly(true)]
        public int RotorNum { get; set; } = 2;
        #endregion
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
