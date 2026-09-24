﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionParameterObj/ProjectHardwareStruct.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using WenQiZhi.Domain.MotionCard.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WenQiZhi.Domain.MotionCard.Common
{
    internal class ProjectHardwareStruct
    {
    }
    #region 方案硬件配置
    public class InputIODataStruct
    {
        /// <summary>
        /// IO的初始电平为高电平
        /// </summary>
        public bool DefaultLevelIsHigh { get; set; }
        /// <summary>
        /// 硬件卡拥有的输入IO的数量
        /// </summary>
        public ushort InIONums { get; set; } = 64;
        /// <summary>
        /// 输入IO的名字
        /// </summary>
        public string InIOName { get; set; }


        /// <summary>
        /// 卡号（即这个输入IO对象是属于第几张卡的）
        /// </summary>
        public int CardNo { get; set; }

        /// <summary>
        /// 此io所属卡的名字
        /// </summary>
        public string CardName { get; set; }

        /// <summary>
        /// 是否为虚拟IO
        /// </summary>
        public bool IsVitualIO { get; set; }


        /// <summary>
        /// IO端口号是从0开始还是从1开始（每种卡都可能有差别，注意看手册）
        /// </summary>
        public int IOIndexStart { get; set; }

    }


    public class OutputIODataStruct
    {
        /// <summary>
        /// IO的初始电平为高电平
        /// </summary>
        public bool DefaultLevelIsHigh { get; set; }
        /// <summary>
        /// 硬件卡拥有的输出IO的数量
        /// </summary>
        public ushort OutIONums { get; set; }

        /// <summary>
        /// 输入IO的名字
        /// </summary>
        public string OutIOName { get; set; }

        /// <summary>
        /// 卡号（即这个输出IO对象是属于第几张卡的）
        /// </summary>
        public int CardNo { get; set; }

        /// <summary>
        /// 此io所属卡的名字
        /// </summary>
        public string CardName { get; set; }

        /// <summary>
        /// 是否为虚拟IO
        /// </summary>
        public bool IsVitualIO { get; set; }

        /// <summary>
        /// IO端口号是从0开始还是从1开始（每种卡都可能有差别，注意看手册）
        /// </summary>
        public int IOIndexStart { get; set; }
    }

    public class AxisConfigDataStruct
    {
        /// <summary>
        /// 逗号分隔的轴信息串
        /// </summary>
        public string AxisInfoStr { get; set; }
        /// <summary>
        /// 轴名字
        /// </summary>
        public string AxisName { get; set; }
        /// <summary>
        /// 轴号
        /// </summary>
        public string AxisID { get; set; }
        /// <summary>
        /// 轴所属卡的卡号
        /// </summary>
        public string AxisWhichCardID { get; set; }

        /// <summary>
        /// 轴所属卡的名字
        /// </summary>
        public string AxisWhichCardName { get; set; }

        /// <summary>
        /// 轴标签
        /// </summary>
        public string AxisNotes { get; set; }

        /// <summary>
        /// 轴注释
        /// </summary>
        public string AxisNotes2 { get; set; }
    }

    public class CardConfigDataStruct
    {
        /// <summary>
        /// 卡已经打开
        /// </summary>
        public bool isOpen { get; set; } = false;

        /// <summary>
        /// 卡已经初始化
        /// </summary>
        public bool isInit { get; set; } = false;

        /// <summary>
        /// 同型号多卡的数量
        /// </summary>
        public ushort CardSum { get; set; } = 1;

        /// <summary>
        /// 虚拟卡，方便脱机调试
        /// </summary>
        public bool IsVitualCard { get; set; } = false;


        /// <summary>
        /// 运动卡输出IO的数量
        /// </summary>
        public int CardOutputIOSum { get; set; } = 16;

        /// <summary>
        /// 运动卡输入IO的数量
        /// </summary>
        public int CardInputIOSum { get; set; } = 16;


        /// <summary>
        /// 如果是CAN总线IO扩展卡，要指定节点号，如果有多张用逗号分隔
        /// </summary>
        public string CANIOCardNodes { get; set; } = "";


        /// <summary>
        /// 卡是否设置拓展模块
        /// </summary>
        public ExtendIoEnum ExtendMode { get; set; } = ExtendIoEnum.Null;


        /// <summary>
        /// 扩展IO模块的数量
        /// </summary>
        public ushort ExtendIOSum { get; set; } = 1;


        public List<CardParamModel> ListCardParam { get; set; }
    }
    #endregion
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
