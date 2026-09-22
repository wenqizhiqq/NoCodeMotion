﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/YKMCC1600P/OutioRealization.cs
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
    public class OutioRealization : IIOOutPut
    {
        /// <summary>
        /// IO的初始电平为高电平
        /// </summary>
        public bool DefaultLevelIsHigh { get; set; } = false;
        /// <summary>
        /// 硬件卡拥有的输出IO的数量
        /// </summary>
        public ushort OutIONums { get; set; } = 64;

        /// <summary>
        /// 输入IO的名字
        /// </summary>
        public string OutIOName { get; set; }

        /// <summary>
        /// 卡号（即这个输出IO对象是属于第几张卡的）
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
        /// 读取指定控制卡的全部输出口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="portno">保留参数，固定值为 0；所有输出口按顺序排列，每 32bit 为一个 port口号，例如 portno=0 表示 out0-out31，portno=1 表示 out32-out63，以此类推；</param>
        /// <returns>
        /// <para>portno 参数</para>
        /// <para>函数返回值的 bit：  0 ；输入口号：0 ；输入口名称：OUT0</para>
        /// <para>函数返回值的 bit：1 ；输入口号：1 ；输入口名称：OUT1</para>
        /// <para>函数返回值的 bit：2 ；输入口号： 2；输入口名称： OUT2</para>
        /// <para>函数返回值的 bit：3 ；输入口号：3 ；输入口名称：OUT3</para>
        /// <para> 函数返回值的 bit：4 ；输入口号： 4 ；输入口名称：OUT1</para>
        /// <para>函数返回值的 bit：5 ；输入口号：5 ；输入口名称：OUT2</para>
        /// <para>函数返回值的 bit：6 ；输入口号：6 ；输入口名称：OUT6</para>
        /// <para>函数返回值的 bit：7 ；输入口号： 7 ；输入口名称：OUT7</para>
        /// <para>函数返回值的 bit：8-31 8-31 ；输入口名称：扩展输入口</para>
        /// </returns>
        public uint GetCardOutState(int CardNo, int portno)
        {
            return YKMCC1600PSDK.Instance.GetCardOutPort(CardNo, portno);
        }

        /// <summary>
        /// 读取指定 CAN-IO 扩展模块的输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">端口号，0 表示 0~31 号口，1 表示 32~63 号口</param>
        /// <param name="IoValue">输出端口的值，bit0~bit31 的值分别代表第 0~31 号输出口的电平 </param>
        /// <returns></returns>
        public uint GetCardCANIOOutPort(int CardNo, int NodeID, int PortNo, ref UInt32 IoValue)
        {
            //uint IoValue1 = 0;
            //YKMCC1600PSDK.Instance.GetCard_CANIO_OutPort(CardNo, NodeID, PortNo, ref IoValue1);
            //return IoValue1;


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
            var res1 = YKMCC1600PSDK.Instance.CardAxisLocalcatIoReadOutbyte(CardNo, startbyte, readbytes, buffer);
            IoValue = BitConverter.ToUInt32(buffer, 0);
            return (uint)res1;
        }

        /// <summary>
        /// 读取指定控制卡的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号，取值范围： 0~7，如果扩展 IO 模块，依次往后累加 </param>
        /// <returns>指定输出端口的电平，0：低电平，1：高电平</returns>
        public int GetCardPortNoOutState(int CardNo, int bitno)
        {
            return YKMCC1600PSDK.Instance.GetCardOutBit(CardNo, bitno);
        }

        /// <summary>
        /// 获取位置比较输出口状态
        /// </summary>
        /// <returns></returns>
        public int GetLocationIsOutNoState()
        {
            return -1;
        }

        /// <summary>
        /// 设置指定控制卡的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="bitno"></param>
        /// <param name="on_off"></param>
        /// <returns></returns>
        public int SetCardBitNoInBit(int CardNo, int bitno, int on_off)
        {
            return YKMCC1600PSDK.Instance.SetCardWriteOutBit(CardNo, bitno, on_off);
        }

        /// <summary>
        /// 设置指定 CAN-IO 扩展模块的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="IoBit">输出口号</param>
        /// <param name="IoValue">输出电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int SetCardCANIOOutBit(int CardNo, int NodeID, int IoBit, int IoValue)
        {
            //IoBit = 0;
            //IoValue = 1;
            return YKMCC1600PSDK.Instance.SetCard_CANIO_OutBit((ushort)CardNo, (ushort)NodeID, (ushort)IoBit, (ushort)IoValue);
        }

        /// <summary>
        /// 设置 DO 端口中指定位的状态
        /// </summary>
        /// <param name="card_no">卡号 </param>
        /// <param name="bit_no">对应的位(bit)号， 范围 0~15,分别对应端子上的 DO0~DO15。</param>
        /// <param name="data">对应 DO 端子输出值:  <para>1  对应端子输出截止；</para> <para>0  对应端子输出导通；</para></param>
        /// <returns>  正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardDOBitState(int card_no, int bit_no, int data)
        {
            return -1;
        }

        /// <summary>
        /// 读取 DO 端口的状态。 
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="Data">状态输出值，其中的 bit 0 ~15 分别对应端子上的 DO0~DO15 <para>当 data 中的位为 0 时，对应端子输出导通；</para> <para>当 data 中的位为 1 时，对应端子输出截止；</para></param>
        /// <returns> 正常返回 0；  出现错误时返回非 0 值；</returns>
        public int GetCardDOState(int card_no, ref int pData)
        {
            return -1;
        }

        /// <summary>
        /// 读取指定输出端口的状态
        /// </summary>
        public uint NmcGetCardReadOutBit(int CardNo, int Channel, int NoteID, int IoBit, ref ushort IoValue)
        {
            int iovalue1 = 3;
            var res1 = YKMCC1600PSDK.Instance.CardAxisLocalcatIoReadOutbit(CardNo, (NoteID - 1) * 16 + IoBit, ref iovalue1);
            IoValue = (ushort)iovalue1;
            return (uint)IoValue;

            //var error = (uint)YKMCC1600PSDK.Instance.GetCard_CANIO_OutBit(CardNo, NoteID, IoBit, ref IoValue);
            //return IoValue;
            //return 0;
        }

        /// <summary>
        /// 设置 DO 端口输出状态 
        /// </summary>
        /// <param name="card_no">卡号</param>
        /// <param name="data">状态输出值，其中的 bit 0 ~15 分别对应端子上的 DO0~DO15 <para>当 data 中的位为 0 时，对应端子输出导通；</para> <para>当 data 中的位为 1 时，对应端子输出截止；</para></param>
        /// <returns> 正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardDOOut(int CardNo, int data)
        {
            return -1;
        }

        /// <summary>
        /// 设置指定控制卡的全部输出口的电平
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="portno"></param>
        /// <param name="outport_val"></param>
        /// <returns></returns>
        public int SetCardPortNoOutPort(int CardNo, int portno, uint outport_val)
        {
            return YKMCC1600PSDK.Instance.SetCardOutPort(CardNo, portno, outport_val);
        }

        /// <summary>
        /// 设置位置比较输出口状态
        /// </summary>
        /// <returns></returns>
        public int SetLocationIsOutNoState()
        {
            return -1;
        }


        public int NmcReadOutportExtern(int CardNo, int Channel, int NoteID, int PortNo, ref uint IoValue)
        {
            //throw new NotImplementedException();
            List<byte> temp = new List<byte>();
            int readbytes = 0;  //读2个字节，还是读4个字节
            int startbyte = 0;  //起始字节
            if (PortNo == 0)
            {
                temp.Add(0); temp.Add(0); readbytes = 2;
                startbyte = NoteID * 2 - 1;
            }
            else if (PortNo == 1)
            {
                temp.Add(0); temp.Add(0); temp.Add(0); temp.Add(0); readbytes = 4;
                startbyte = NoteID * 4 - 1;
            }
            byte[] buffer = temp.ToArray();
            var res1 = YKMCC1600PSDK.Instance.CardAxisLocalcatIoReadOutbyte(CardNo, startbyte, readbytes, buffer);
            IoValue = BitConverter.ToUInt32(buffer, 0);
            return res1;
        }

        public int NmcWriteOutbitExtern(int CardNo, int Channel, int NoteID, int IoBit, ushort IoValue)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 设置指定CAN 扩展模块的输出口组的电平(0：低电平，1：高电平)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Channel">总线通道号，固定为 2</param>
        /// <param name="NodeID">EtherCAT 地址，从 1001 开始，按从站数往后累加</param>
        /// <param name="portno">输出组号（32 位一组，从 0 开始，如 0 表示 0-31 位，1 表示 32-63位）</param>
        /// <param name="Iovalue">输出组各输出端口的值，bit0~bit31 的值分别代表第 0~31 号输出口的电平，1：低电平，0：高电平</param>
        /// <returns></returns>
        public int NmcWriteOutPortExtern(int CardNo, int Channel, int NodeID, ushort portno, uint Iovalue)
        {

            //throw new NotImplementedException();
            //List<byte> temp = new List<byte>();
            //int readbytes = 0;  //读2个字节，还是读4个字节
            //int startbyte = 0;  //起始字节
            //if (portno == 0)
            //{
            //    temp.Add(0); temp.Add(0); readbytes = 2;
            //    startbyte = NodeID * 2 - 1;
            //}
            //else if (portno == 1)
            //{
            //    temp.Add(0); temp.Add(0); temp.Add(0); temp.Add(0); readbytes = 4;
            //    startbyte = NodeID * 4 - 1;
            //}
            //byte[] buffer = temp.ToArray();


            //if (readbytes == 2)
            //{
            //    buffer = BitConverter.GetBytes((ushort)(Iovalue & 0xFFFF));

            //    // 如果需要大端序，反转字节数组
            //    if (BitConverter.IsLittleEndian)
            //    {
            //        Array.Reverse(buffer);
            //    }
            //}
            //else if(readbytes==3)
            //{
            //    buffer = BitConverter.GetBytes(Iovalue);

            //    // 如果需要大端序，反转字节数组
            //    if (BitConverter.IsLittleEndian)
            //    {
            //        Array.Reverse(buffer);
            //    }
            //}

            byte high = Extract8Bits(Iovalue, 0);
            byte low = Extract8Bits(Iovalue, 8);
            byte[] buffer = new byte[2] { high, low };
            //return YKMCC1600PSDK.Instance.CardAxisLocalcatIoWriteOutbyte(CardNo, startbyte-1, 1, buffer);
            return YKMCC1600PSDK.Instance.CardAxisLocalcatIoWriteOutbyte(CardNo, (int)(2 * (NodeID - 1)), 2, buffer);
        }

        byte Extract8Bits(uint value, int startBit)
        {
            if (startBit < 0 || startBit > 24)
                throw new ArgumentOutOfRangeException(nameof(startBit), "起始位必须是在0~24之间！");
            uint mask = 0xFFU << startBit;
            return (byte)((value & mask) >> startBit);
        }

        #region 1600P的localcat io指令

        /// <summary>
        /// 设置拓展板某个输出端口的电平;
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输出端口号,取值范围:0~5 通用输出 6~1023 耦合器输出;</param>
        /// <param name="on_off">输出电平,0:低电平,1:高电平 ;</param>
        /// <returns></returns>
        public int CardAxisLocalcatIoWriteOutbit(int CardNo, int bitno, int on_off)
        {
            return YKMCC1600PSDK.Instance.CardAxisLocalcatIoWriteOutbit(CardNo, bitno, on_off);
        }

        /// <summary>
        /// 读取拓展板的某个输出端口的电平;
        /// </summary>
        /// <param name="CardNo">:控制卡卡号</param>
        /// <param name="bitno">输入端口号,取值范围:0~5 通用输出 6~1023 耦合器输出;</param>
        /// <param name="on_off">指定输出端口的电平,0:低电平,1:高电平</param>
        /// <returns></returns>
        public int CardAxisLocalcatIoReadOutbit(int CardNo, int bitno, ref int on_off)
        {
            return YKMCC1600PSDK.Instance.CardAxisLocalcatIoReadOutbit(CardNo, bitno, ref on_off);
        }


        /// <summary>
        /// 按byte读取指拓展板输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号，取值范围0-7；</param>
        /// <param name="StartByte">输出端口起始字节数,取值范围:0~128；</param>
        /// <param name="ByteNum">需要读取的输出端口总字节数量，取值范围:0~128；</param>
        /// <param name="ValueList">Do端口电平值数组，转换为2进制后对应每一位bit；</param>
        /// <returns></returns>
        public int CardAxisLocalcatIoReadOutbyte(int CardNo, int StartByte, int ByteNum, byte[] ValueList)
        {
            return YKMCC1600PSDK.Instance.CardAxisLocalcatIoReadOutbyte(CardNo, StartByte, ByteNum, ValueList);
        }

        /// <summary>
        /// 按byte设置拓展板的输出端口的电平；
        /// </summary>
        /// <param name="CardNo">控制卡卡号，取值范围0-7；</param>
        /// <param name="StartByte">输出端口起始字节数,取值范围:0~128；</param>
        /// <param name="ByteNum">需要设置的输出端口总字节数量，取值范围:0~128；</param>
        /// <param name="ValueList">Do端口电平值数组，转换为2进制后对应每一位bit；</param>
        /// <returns></returns>
        public int CardAxisLocalcatIoWriteOutbyte(int CardNo, int StartByte, int ByteNum, byte[] ValueList)
        {
            return YKMCC1600PSDK.Instance.CardAxisLocalcatIoWriteOutbyte(CardNo, StartByte, ByteNum, ValueList);
        }

        #endregion


    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
