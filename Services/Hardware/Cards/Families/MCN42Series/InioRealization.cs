﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/MCN42Series/InioRealization.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WenQiZhi.Domain.MotionCard.Common.YKMCN42Series
{
    public class InioRealization : IIOInPut
    {
        /// <summary>
        /// IO的初始电平为高电平
        /// </summary>
        public bool DefaultLevelIsHigh { get; set; } = false;

        /// <summary>
        /// 硬件卡拥有的输入IO的数量
        /// </summary>
        public ushort InIONums { get; set; } = 24;
        /// <summary>
        /// 输入IO的名字
        /// </summary>
        public string InIOName { get; set; }


        /// <summary>
        /// 卡号（即这个输入IO对象是属于第几张卡的）
        /// </summary>
        public int CardNo { get; set; }


        /// <summary>
        /// 是否为虚拟IO
        /// </summary>
        public bool IsVitualIO { get; set; } = false;

        /// <summary>
        /// IO端口号是从0开始还是从1开始（每种卡都可能有差别，注意看手册）
        /// </summary>
        public int IOIndexStart { get; set; } = 0;

        /// <summary>
        /// 读取指定控制卡的全部输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="portno">保留参数，固定值为 0；所有输入口按顺序排列，每 32bit 为一个 port口号，例如 portno = 0 表示 in0-in31，portno=1 表示 in32-in63，以此类推；</param>
        /// <returns>
        /// <para>portno参数</para>
        /// <para>函数返回值的 bit：0 ；输入口号：0 ；输入口名称：IN0</para>
        /// <para>函数返回值的 bit：1 ；输入口号：1 ；输入口名称：IN1</para>
        /// <para>函数返回值的 bit：2 ；输入口号： 2；输入口名称： IN2</para>
        /// <para>函数返回值的 bit：3 ；输入口号：3 ；输入口名称：IN3</para>
        /// <para> 函数返回值的 bit：4 ；输入口号： 4 ；输入口名称：IN4</para>
        /// <para>函数返回值的 bit：5 ；输入口号：5 ；输入口名称：IN5</para>
        /// <para>函数返回值的 bit：6 ；输入口号：6 ；输入口名称：IN6</para>
        /// <para>函数返回值的 bit：7 ；输入口号： 7 ；输入口名称：IN7</para>
        /// <para>函数返回值的 bit：8-31 8-31 ；输入口名称：扩展输入口</para>
        /// </returns>
        public int GetCardInPortValue(int CardNo, int portno)
        {
            return YK_MCN42SeriesSDK.Instance.DmcGetCardPortNoInPort(CardNo, portno);
        }


        /// <summary>
        /// 设置主板上输入IO的端口的电平（虚拟运动卡专用）
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="bitno"></param>
        /// <param name="on_off"></param>
        /// <returns></returns>
        public int SetCardWriteInBit(int CardNo, int bitno, int on_off)
        {
            return YK_MCN42SeriesSDK.Instance.SetCardWriteInBit(CardNo, bitno, on_off);
        }


        /// <summary>
        /// 读取指定 CAN-IO 扩展模块的输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">端口号，0 表示 0~31 号口，1 表示 32~63 号口</param>
        /// <param name="IoValue">输出端口的值，bit0~bit31 的值分别代表第 0~31 号输出口的电平 </param>
        /// <returns>错误代码</returns>
        public uint GetCardCANIOInPort(int CardNo, int NodeID, int PortNo, ref UInt32 IoValue)
        {
            YK_MCN42SeriesSDK.Instance.NmcReadInportExtern(CardNo, 2, NodeID, PortNo, ref IoValue);
            return 0;
        }


        /// <summary>
        /// 读取指定控制卡的某个输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号，取值范围： 0~7，如果扩展 IO 模块，依次往后累加 </param>
        /// <returns>指定的输入端口电平：0：低电平，1：高电平</returns>
        public int GetCardInPortNoValue(int CardNo, int bitno)
        {
            return YK_MCN42SeriesSDK.Instance.DmcGetCardInPortNoBit(CardNo, bitno);
        }

        public int GetCardDIState(int card_no, ref int pData)
        {
            return -1;
        }
        public int GetCardDIBitAlarmState(int card_no, int bit_no, ref int pData)
        {
            return -1;
        }

        public int SetCardCompEnable(int card_no, int enable, int active_level, int ref_source, int length)
        {
            throw new NotImplementedException();
        }

        public int SetCardWriteExtendInBit(int CardNo, int bitno, int nodeno, int on_off)
        {
            throw new NotImplementedException();
        }

        public int EMGAction(string CardName, int cardNo, int EmgIO)
        {
            throw new NotImplementedException();
        }

        public int NmcReadInportExtern(int CardNo, int Channel, int NoteID, int PortNo, ref uint IoValue)
        {
            return YK_MCN42SeriesSDK.Instance.NmcReadInportExtern(CardNo, Channel, NoteID, PortNo, ref IoValue);
        }

        /// <summary>
        /// 读取指定扩展输出端口的状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Channel">总线通道号，固定为 2</param>
        /// <param name="NoteID">站号</param>
        /// <param name="IoBit">输入端口号</param>
        /// <param name="IoValue">指定输出端口的电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardReadInBit(int CardNo, int Channel, int NoteID, int IoBit, ref ushort IoValue)
        {
            int error = YK_MCN42SeriesSDK.Instance.NmcReadInbitExtern(CardNo, Channel, NoteID, IoBit, ref IoValue);
            return IoValue;
        }
        ///// <summary>
        ///// 获取输入端口的值 
        ///// </summary>
        //public short GetCardInPort(int CardNo, int portno)
        //{
        //    return 32767;
        //}


        ///// <summary>
        ///// 读取指定输入端口的状态  
        ///// </summary>
        //public short GetCardReadInBit()
        //{
        //    return 32767;
        //}
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
