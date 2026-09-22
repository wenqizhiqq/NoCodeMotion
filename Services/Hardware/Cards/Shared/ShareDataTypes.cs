﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: ShareData/ShareDatastruct.cs
//   说明: 原文件是 5194 行的「大杂烩」（314 个类型，含 System.Drawing /
//         BinaryFormatter 等 .NET 10 无法使用的依赖），故按依赖闭包只摘出
//         卡层实际引用到的类型，其余一律不移植。
//   本文件包含 16 个类型: AllMotionCardEnum, AxisStatusEnum, AxisStatusInfoEnum, AxisStatusParameterEnum, DMC3000CardEnum, DMC_CacheMemory_Struct, DeviceMessageTypeEnum, Dmc1000SConfigStruct, ExtendIoEnum, HY7X00系列MotionCardSwitchEnum, HomeModeEnum, MultiplexedLatchParam, PlusOutModeEnum, SACMotionMethodEnum, SystemSupportMotionCardEnum, UnitDenomEnumName
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;

namespace Samsun.Domain.MotionCard.Common
{

        /// <summary>
        /// 全部运动控制卡的型号枚举
        /// </summary>
        public enum AllMotionCardEnum
        {
            DMC3400A, DMC3800, DMC3600, DMC3C00, DMC2210, DMC1000S, PCI9014,
            虚拟运动卡, DMC_E3032_EtherCAT, PLTEI400H,
            HYMC608,
            HY7400, HY7600, HY7800, HY7C00,
            SLD1230, SLD1230系列,
            E6432, E6416, E6364, E6316, E6564, E6516,
            PCI9016,
            仿真运动卡,
            MCC400P, MCC400S, MCC800P, MCC800S, MCC1200P, MCC1200S, MCC1600P, MCC1600S, MCCE332, MCC42Series, SLD1232系列
        }

        /// <summary>
        /// 轴状态
        /// </summary>
        public enum AxisStatusEnum
        {
            轴停止 = 0, 轴运行中, 轴回原标识
        }

        public enum AxisStatusInfoEnum
        {
            报警, 正限位, 负限位, 急停, 原点位, 保留1, SLAdd, SLSub, INP, EZ, RDY, DSTP, jog前进,  //12
            jog后退, 回原, 轴停止, 起始速度, 最大速度, 加速度, ptp目标位置, 调试运动速度, ptp运动开始, ptp运动轴状态, ptp运动到达位置,  //23
            ptp位置, jog位置, 保留24, 保留25, 保留26, 保留27, 保留28, 保留29, 保留30, 轴名字, 是否为jog模式
        }

        /***  ============  ***/


        public enum AxisStatusParameterEnum
        {
            目标位置 = 0, 编码器位置, 正限位, 负限位, 原点位, 急停, 报警
        }


        /// <summary>
        /// dmc3000系列卡的枚举
        /// </summary>
        public enum DMC3000CardEnum
        {
            DMC3400A, DMC3800, DMC3600, DMC3C00
        }

        public class DMC_CacheMemory_Struct
        {
            /// <summary>
            /// 1:open Cache buffer   2:Start Cache buffer    3:Close Cache buffer  4: SetProfileUnit_Axis   5:PMove_OnceAxis   6:WaitEvent    7:SetProfileUnit_MulAxis   8:PMove_MulAxis   9:Clear Cache buffer
            /// 10: SetCrdLineProfileUnit_Axis     11:LineStart_Crd  12: Set RegisterFlag   13：Delay Time(us)   14：Add trigger（IO/DA/）  15：Set RegisterFlag SoftOperation   16：Read RegisterFlag SoftOperation
            /// 17: Stop Cache buffer
            /// SIW（Coil/Word--bit/byte）   100: YAMAHA Rotor Operation  Write Coil    101: YAMAHA Rotor Operation Read Coil    102: YAMAHA Rotor Operation Write Word  103: YAMAHA Rotor Operation Read Word
            /// SOW（Coil/Word--bit/byte）   104: YAMAHA Rotor Operation Read Coil   105: YAMAHA Rotor Operation Read Word
            /// </summary>
            public int type { get; set; }
            public int CardNo { get; set; }

            public ushort Group { get; set; }

            public ushort Event1 { get; set; }
            public ushort AxisID { get; set; }

            public ushort num { get; set; }

            public ushort CompareOperator { get; set; }

            public double target_Pos { get; set; }
            public ushort Mark { get; set; }
            public ushort[] Axis_List { get; set; }


            public double StartSpeeed { get; set; }

            public double EndSpeeed { get; set; }
            public double StopSpeed { get; set; }

            public double tacc { get; set; }
            public double tdec { get; set; }
            public double smooth_time { get; set; }

            public ushort Position_Mode { get; set; }
            #region Array
            public double[] target_PosArray { get; set; }
            public ushort[] AxisArray_MarkMove { get; set; }
            public double[] StartSpeeedArray { get; set; }

            public double[] EndSpeeedArray { get; set; }
            public double[] StopSpeedArray { get; set; }

            public double[] taccArray { get; set; }
            public double[] tdecArray { get; set; }
            public double[] smooth_timeArray { get; set; }

            #endregion

            #region Register
            public ushort RegisterNo { get; set; }
            public ushort Wait_Flag_sts { get; set; }

            #endregion
            public double DelayTime { get; set; }


            #region YAMAHA
            public ushort SW_NO { get; set; }
            public ushort SIW { get; set; }
            public ushort SOW { get; set; }

            #endregion
        }



        /// <summary>
        /// 设备消息枚举
        /// </summary>
        public enum DeviceMessageTypeEnum
        {

            /// <summary>
            /// 操作确认消息(报警-显示报警信息，由操作用户确定是否要停机处理)
            /// </summary>
            //OperationConfirmMsg = 0,
            操作确认消息窗口 = 0,
            /// <summary>
            /// 警告消息(警告-显示警告信息，但不影响运行)
            /// </summary>
            //WarningMsg,
            警告消息窗口,
            /// <summary>
            /// 生产消息(提示-显示异常信息，但不影响运行)
            /// </summary>
            //WorkMsg,
            生产消息窗口,
            /// <summary>
            /// 报警消息(错误-显示错误信息，必须立即停机处理)
            /// </summary>
            //AlarmMsg,
            报警消息窗口,
            /// <summary>
            /// 用户操作记录消息(提示-显示异常信息，但不影响运行)
            /// </summary>
            //UserOperationRecMsg,
            用户操作记录消息窗口,
            /// <summary>
            /// 调试消息(提示-显示异常信息，但不影响运行)
            /// </summary>
            //DebugMsg,
            调试消息窗口,
            /// <summary>
            /// DownTime消息(提示-显示异常信息，但不影响运行)
            /// </summary>
            //DownTimeMsg,
            DownTime消息窗口,
            /// <summary>
            /// 流动报警消息(错误-显示错误信息，必须立即停机处理)
            /// </summary>
            //WalkingAlarmMsg,
            流动报警消息窗口,
            /// <summary>
            /// 软件异常消息(错误-显示错误信息，必须立即停机处理)
            /// </summary>
            //SoftBugMsg,
            软件异常消息窗口,

            /// <summary>
            /// 硬件节点异常消息(此消息往硬件节点表写属性)
            /// </summary>
            //HardwareNodeMsg,
            硬件节点异常消息窗口,

            /// <summary>
            /// 硬件IO交互消息记录
            /// </summary>
            //IOMsg,
            硬件IO交互消息记录窗口,

            /// <summary>
            /// 状态条消息
            /// </summary>
            //StatusMsg,
            状态条消息窗口,

            /// <summary>
            /// 交互信号
            /// </summary>
            //SignalMsg,
            交互信号窗口,

            /// <summary>
            ///运动流输出信息 
            /// </summary>
            //MotionFlowMsg,
            运动流输出信息窗口,

            /// <summary>
            /// mes交互消息信息
            /// </summary>
            //MESCommMsg
            mes交互消息信息窗口,
            /// <summary>
            /// 模块运动输出的调试信息, SAC模块,XYZ模块 等等
            /// </summary>
            模块调试信息输出,
            /// <summary>
            /// 消息发送接收日志
            /// </summary>
            消息发送接收日志,
            IO置位状态日志,
            无,
        }


        public struct Dmc1000SConfigStruct
        {
            public int SetSDStatusP;
            public int SetHomeLogicP;
            public int SetCounterConfigP;
            public int SetCardPlusOutModeP;
            public int SetELModeP;
            public int SetELEnableP;
            public int SetALMStatusP;
            public int SetALMLogicP;
            public int SetALMAllP;
        }


        /// <summary>
        /// 扩展IO枚举
        /// </summary>
        public enum ExtendIoEnum
        {
            Null = 0,
            //用于pci卡的扩展模块
            DMC_CAN_In16Out16,
            DMC_CAN_In32Out32,
            DMC_CAN_In48Out48,
            DMC_CAN_In64Out64,
            //用于总线卡的IO扩展模块
            DMC_EtherCAT_In16Out16,
            DMC_EtherCAT_In32Out32,
            DMC_EtherCAT_In48Out48,
            DMC_EtherCAT_In64Out64,

            //用于恆昱扩展IO模块
            HY_CAN_In20Out20,

            //用io耦合器
            IO_Coupter,
        }





        public enum HY7X00系列MotionCardSwitchEnum
        {
            HY7400 = 0, HY7600, HY7800, HY7C00
        }




        public enum HomeModeEnum
        {
            /// <summary>
            /// 只根据原点（原点传感器）回Home。轴将一直沿着指定的方向运动，直到碰到原点。
            /// </summary>
            MODE1_Abs,
            /// <summary>
            /// 只根据负限位信号回Home。轴将一直沿着负限位方向运动，直至碰到负限位。
            /// </summary>
            MODE2_NegLimitSignal,

            /// <summary>
            /// 只根据正限位信号回Home。轴将一直沿着正限位方向运动，直至碰到正限位。
            /// </summary>
            MODE2_PosLimitSignal,
            /// <summary>
            /// 先找到限位信号，然后再回原。（如果是负方向回原，则先找到负限位，再找到回点信号，冲出原点后再正常回原。）
            /// （如果是正方向回原，先找到正限位，然后执行正常回原）
            /// </summary>
            MODE3_LimitSignalAndORG,
            /// <summary>
            /// Ether总线回原的方式，共计37种
            /// </summary>
            ///     /// <summary>
            /// 脉冲卡两次回原
            /// </summary>
            TWOHOME,
            EtherCATHome1,
            EtherCATHome2,
            EtherCATHome3,
            EtherCATHome4,
            EtherCATHome5,
            EtherCATHome6,
            EtherCATHome7,
            EtherCATHome8,
            EtherCATHome9,
            EtherCATHome10,
            EtherCATHome11,
            EtherCATHome12,
            EtherCATHome13,
            EtherCATHome14,
            EtherCATHome15,
            EtherCATHome16,
            EtherCATHome17,
            EtherCATHome18,
            EtherCATHome19,
            EtherCATHome20,
            EtherCATHome21,
            EtherCATHome22,
            EtherCATHome23,
            EtherCATHome24,
            EtherCATHome25,
            EtherCATHome26,
            EtherCATHome27,
            EtherCATHome28,
            EtherCATHome29,
            EtherCATHome30,
            EtherCATHome31,
            EtherCATHome32,
            EtherCATHome33,
            EtherCATHome34,
            EtherCATHome35,
            EtherCATHome36,
            EtherCATHome37,
            EtherCATHome101,
            EtherCATHome102,
            EtherCATHome103,

        }


        /// <summary>
        /// 锁存器的数据结构
        /// </summary>
        public class MultiplexedLatchParam
        {
            /// <summary>
            /// 卡号
            /// </summary>
            public int CardNo { get; set; } = 0;
            /// <summary>
            /// 锁存器号
            /// </summary>
            public int LtcNo { get; set; } = 0;
            /// <summary>
            /// 锁存方式
            /// </summary>
            //[DisplayName("锁存方式")]
            //[Description("0：单次锁存；2：连续锁存;3:触发延时急停")]
            public int LtcMode { get; set; } = 0;

            /// <summary>
            /// 锁存源
            /// </summary>
            //[DisplayName("锁存源")]
            //[Description("0：轴指令位置，2：辅助编码器")]
            public int LtcSource { get; set; } = 0;
            /// <summary>
            /// 触发信号 0:下降沿锁存;1：上升沿锁存； 2：双边沿锁存"
            /// </summary>
            //[DisplayName("触发信号")]
            //[Description("0:下降沿锁存;1：上升沿锁存； 2：双边沿锁存")]
            public int LtcLogic { get; set; } = 1;
            /// <summary>
            /// 锁存源选择轴数组列表;轴指令位置锁存源为0-31，辅助编码器锁存源为0-3,最多支持3个轴,多个轴,可以使用英文逗号隔开
            /// </summary>
            //[DisplayName("锁存源选择轴数组列表")]
            //[Description("轴指令位置锁存源为0-31，辅助编码器锁存源为0-3,最多支持3个轴,多个轴,可以使用英文逗号隔开")]
            public List<int> LtcOrderList { get; set; } = new List<int>();
            /// <summary>
            /// 锁存器的锁存值的数组
            /// </summary>
            public double[] LtcPos { get; set; }
            /// <summary>
            /// 锁存器锁存的总数据个数
            /// </summary>
            public int LtcNum { get; set; } = 0;
            /// <summary>
            /// 锁存器状态 ,1正在锁存钟,0未启用,2锁存完成,3缓存区已满
            /// </summary>
            public int LtcStatus { get; set; } = -1;
            /// <summary>
            /// 锁存器缓存区剩余的数据个数
            /// </summary>
            public int LtcRemanin { get; set; } = 0;
        }




        /// <summary>
        /// 轴的脉冲输出模式
        /// </summary>
        public enum PlusOutModeEnum
        {
            OUT_DIR = 0,
            OUT_DIR_OUT_NEG,
            OUT_DIR_DIR_NEG,
            OUT_DIR_ALL_NEG,
            O_CW_CCW,
            CW_CCW_ALL_NEG
        }


        //0 联动  1顺序  2 插补  3顺序回原 4并行回原
        public enum SACMotionMethodEnum
        {
            联动 = 0, 顺序, 直线插补, 顺序回原, 并行回原, Jog, 穴位运动, 相对联动, 料盘取放运动,
            NoAgainJog
        }


        /// <summary>
        /// 系统支持的板卡列表
        /// </summary>
        public enum SystemSupportMotionCardEnum
        {
            DMC_E3032_EtherCAT = 0,/*DMC3000,GTS800PG,*/PCI9014, DMC2210, DMC3000系列, 虚拟运动卡, HY7X00系列,
            DMC1000S,
            PLTEI400H,
            HYMC608,
            SLD1230系列, SLD1232系列,
            PCI9016,
            仿真运动卡,
            MCC400P, MCC400S, MCC800P, MCC800S, MCC1200P, MCC1200S, MCC1600P, MCC1600S, MCCE332,
            EC600,
            E6432, E6416, E6364, E6316, E6564, E6516,
            MCC42Series,
        }


        /// <summary>
        /// 单位名称枚举
        /// </summary>
        public enum UnitDenomEnumName
        {
            pulse = 0, mm, C
        }

}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
