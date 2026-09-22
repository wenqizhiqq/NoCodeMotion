﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/YKMCC1600P/InioRealization.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samsun.Domain.MotionCard.Common.YKMCC1600P
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
        /// 是否为虚拟IO
        /// </summary>
        public bool IsVitualIO { get; set; } = false;


        /// <summary>
        /// IO端口号是从0开始还是从1开始（每种卡都可能有差别，注意看手册）
        /// </summary>
        public int IOIndexStart { get; set; } = 0;


        /// <summary>
        /// 设置主板上输入IO的端口的电平（虚拟运动卡专用）
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="bitno"></param>
        /// <param name="on_off"></param>
        /// <returns></returns>
        public int SetCardWriteInBit(int CardNo, int bitno, int on_off)
        {
            return YKMCC1600PSDK.Instance.SetCardWriteInBit(CardNo, bitno, on_off);
        }

        /// <summary>
        /// 查询 DI 端口中指定位的状态 DI0—3 可在硬件上连接 XYZU 轴的告警 ALM 信号。用此函数可查询轴告警状态
        /// </summary>
        /// <param name="card_no">卡号</param>
        /// <param name="bit_no">对应的位(bit)号， 范围 0~15,分别对应端子上的 DI0~DI15。</param>
        /// <param name="pData">DI 端口状态，其中的 bit 0 ~15 分别对应端子上的 DI0~DI15  <para> 当*pData 中的位为 0 时，对应端子输入低电平；</para> <para> 当*pData 中的位为 1 时，对应端子输入高电平；</para>    </param>
        /// <returns></returns>
        public int GetCardDIBitAlarmState(int card_no, int bit_no, ref int pData)
        {
            return -1;
        }

        /// <summary>
        /// 查询 DI 端口的状态。 DI0—3 可在硬件上连接 XYZU 轴的告警 ALM 信号。用此函数可查询轴告警状态
        /// </summary>
        /// <param name="card_no">卡号 </param>
        /// <param name="pData">DI 端口状态，其中的 bit 0 ~15 分别对应端子上的 DI0~DI15  <para> 当*pData 中的位为 0 时，对应端子输入低电平；</para> <para> 当*pData 中的位为 1 时，对应端子输入高电平；</para>    </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int GetCardDIState(int card_no, ref int pData)
        {
            return -1;
        }
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
        public int GetCardInPortNoValue(int CardNo, int portno)
        {
            //todo: 3000系列bit和port区别
            return (int)YKMCC1600PSDK.Instance.GetCardInBit(CardNo, portno);
            //return (int)DMC3400ASDK.Instance.GetCardInPort(CardNo, portno);
        }



        /// <summary>
        /// 读取指定 CAN-IO 扩展模块的输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">模块编号，(函数内部最终转为读取的起始字节数）</param>
        /// <param name="PortNo">传入0和1,（0表示16位模块，1表示32位模块)</param>
        /// <param name="IoValue">输出端口的值，bit0~bit31 的值分别代表第 0~31 号输出口的电平 </param>
        /// <returns>错误代码</returns>
        public uint GetCardCANIOInPort(int CardNo, int NodeID, int PortNo, ref UInt32 IoValue)
        {
            //int state = 3;
            //YKMCC1600PSDK.Instance.CardAxisGetLocalcatState(CardNo, 0,ref state);

            List<byte> temp = new List<byte>();
            int readbytes = 0;  //读2个字节，还是读4个字节
            int startbyte = 0;  //起始字节
            if (PortNo == 0)
            {
                temp.Add(0); temp.Add(0); temp.Add(0); temp.Add(0); readbytes = 2;
                startbyte = NodeID * 2 - 2;
            }
            else if (PortNo == 1)
            {
                temp.Add(0); temp.Add(0); temp.Add(0); temp.Add(0); readbytes = 4;
                startbyte = NodeID * 4 - 2;
            }
            byte[] buffer = temp.ToArray();
            var res1 = YKMCC1600PSDK.Instance.CardAxisLocalcatIoReadInbyte(CardNo, startbyte, readbytes, buffer);
            IoValue = BitConverter.ToUInt32(buffer, 0);
            return (uint)res1;

            uint IoValue1 = 0;
            // PortNo = 0;
            YKMCC1600PSDK.Instance.GetCard_CANIO_InPort(CardNo, NodeID, PortNo, ref IoValue1);
            //var s3 = Convert.ToString(IoValue1, 2).PadLeft(32, '0');
            //s3 = new string(s3.ToCharArray().Reverse().ToArray());
            return IoValue1;
        }




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
            return (int)YKMCC1600PSDK.Instance.GetCardInPort(CardNo, portno);
        }
        /// <summary>
        /// 设置卡Com口使能
        /// </summary>
        /// <param name="card_no"></param>
        /// <param name="enable"></param>
        /// <param name="active_level"></param>
        /// <param name="ref_source"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public int SetCardCompEnable(int card_no, int enable, int active_level, int ref_source, int length)
        {
            return -1;
        }

        public int SetCardWriteExtendInBit(int CardNo, int bitno, int nodeno, int on_off)
        {
            return 0;
        }

        public int EMGAction(string CardName, int cardNo, int EmgIO)
        {
            throw new NotImplementedException();
        }

        public int NmcReadInportExtern(int CardNo, int Channel, int NoteID, int PortNo, ref uint IoValue)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 读取指定扩展输出端口的状态
        /// </summary>
        public int NmcGetCardReadInBit(int CardNo, int Channel, int NoteID, int IoBit, ref ushort IoValue)
        {
            int iovalue1 = -1;
            var res1 = YKMCC1600PSDK.Instance.CardAxisLocalcatIoReadInbit(CardNo, (NoteID - 1) * 16 + IoBit, ref iovalue1);
            IoValue = (ushort)iovalue1;
            return IoValue;

            //int error = YKMCC1600PSDK.Instance.GetCard_CANIO_InBit(CardNo, NoteID, IoBit, ref IoValue);
            //return IoValue;
            //return -1;
        }

        #region 1600P的localcat io指令

        /// <summary>
        /// 读取拓展板某个输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号,取值范围:0~5 通用输出 6~1023 耦合器输出;</param>
        /// <param name="on_off">指定的输入端口电平:0:低电平,1:高电平;</param>
        /// <returns>错误代码</returns>
        public int CardAxisLocalcatIoReadInbit(int CardNo, int bitno, ref int on_off)
        {
            return YKMCC1600PSDK.Instance.CardAxisLocalcatIoReadInbit(CardNo, bitno, ref on_off);
        }

        /// <summary>
        /// 按byte读取拓展板输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号，取值范围0-7；</param>
        /// <param name="StartByte"> 总线Di起始字节数,取值范围:0~128；</param>
        /// <param name="ByteNum">需要读取的总线Di总字节数量，取值范围:0~128</param>
        /// <param name="ValueList">Di端口电平值数组，转换为2进制后对应每一位bit；</param>
        /// <returns></returns>
        public int CardAxisLocalcatIoReadInbyte(int CardNo, int StartByte, int ByteNum, byte[] ValueList)
        {
            return YKMCC1600PSDK.Instance.CardAxisLocalcatIoReadInbyte(CardNo, StartByte, ByteNum, ValueList);
        }


        #endregion

    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
