﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/MCN42Series/MCN420SeriesSDK.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;
using static Samsun.Domain.MotionCard.Common.ecat_motion;
using static Samsun.Domain.MotionCard.Common.YKMCN42Series.MCN420;

namespace Samsun.Domain.MotionCard.Common.YKMCN42Series
{
    public class YK_MCN42SeriesSDK
    {
        /// <summary>
        /// 日志记录
        /// </summary>
        public Action<string, string> CMessageAddTipsMessage;
        //int Hcmp = 0; int Cmp_Mode = 0;
        Dictionary<ushort, int> HcmpDic = new Dictionary<ushort, int>();
        /// <summary>
        /// Key: Item1 cardNo   Item2 AxisID
        /// Value: Home Struct
        /// </summary>
        ConcurrentDictionary<Tuple<int, int>, Home_Req> AxisHomeParaDic = new ConcurrentDictionary<Tuple<int, int>, Home_Req>();
        /// <summary>
        /// Key: Item1 cardNo   Item2 AxisID
        /// Value: Pmove Struct
        /// </summary>
        ConcurrentDictionary<Tuple<int, int>, Single_Req_Param> AxisPMoveParaDic = new ConcurrentDictionary<Tuple<int, int>, Single_Req_Param>();
        #region 
        /// <summary>
        /// Key: Item1 cardNo   Item2 AxisID
        /// Value: vmove Struct
        /// </summary>
        ConcurrentDictionary<Tuple<int, int>, Jog_Req_Param> AxisVMoveParaDic = new ConcurrentDictionary<Tuple<int, int>, Jog_Req_Param>();
        ConcurrentDictionary<Tuple<int, int>, Line_Req_Param> AxisLineParaDic = new ConcurrentDictionary<Tuple<int, int>, Line_Req_Param>();
        ConcurrentDictionary<Tuple<int, int>, ARC_2D_Req_Param> AxisARC2DParaDic = new ConcurrentDictionary<Tuple<int, int>, ARC_2D_Req_Param>();
        ConcurrentDictionary<Tuple<int, int>, Arc_2D_3Point_Req_Param> AxisARC2D3PointParaDic = new ConcurrentDictionary<Tuple<int, int>, Arc_2D_3Point_Req_Param>();
        ConcurrentDictionary<Tuple<int, int>, CMD_3DArc_Req_Param> AxisARC3DParaDic = new ConcurrentDictionary<Tuple<int, int>, CMD_3DArc_Req_Param>();
        ConcurrentDictionary<Tuple<int, int>, Helix_2D_Req_Param> AxisHelix2DParaDic = new ConcurrentDictionary<Tuple<int, int>, Helix_2D_Req_Param>();
        ConcurrentDictionary<Tuple<int, int>, Helix_3D_Req_Param> AxisHelix3DParaDic = new ConcurrentDictionary<Tuple<int, int>, Helix_3D_Req_Param>();
        ConcurrentDictionary<Tuple<int, int>, Cone_3D_Req_Param> AxisCone3DParaDic = new ConcurrentDictionary<Tuple<int, int>, Cone_3D_Req_Param>();
        #endregion

        public bool IsInit { get; set; }

        private static YK_MCN42SeriesSDK _instance;
        public static YK_MCN42SeriesSDK Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new YK_MCN42SeriesSDK();
                return YK_MCN42SeriesSDK._instance;
            }
            set { YK_MCN42SeriesSDK._instance = value; }
        }
        YK_MCN42SeriesSDK()
        {
            for (int i = 0; i < 8; i++)
            {
                HcmpDic.Add((ushort)i, 4);
            }
            //Load ErrorCodeXML
            ConvertErrorCodeXML();
        }

        /// <summary>
        /// 设置主板上输入IO的端口的电平（虚拟运动卡专用）
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="bitno"></param>
        /// <param name="on_off">输入电平，0：低电平，1：高电平</param>
        /// <returns></returns>
        public int SetCardWriteInBit(int CardNo, int bitno, int on_off)
        {
            return 0;
        }

        /// <summary>
        /// 取总线错误信息
        /// </summary>
        /// <param name="ErrNum"></param>
        /// <returns></returns>
        public string GetEtherCATErrorInfo(int ErrNum)
        {
            if (ErrNum == 0) { return ""; }
            var msg = string.Empty;
            //MCN420.mcc_get_error_description(ErrNum, des);
            //msg = System.Text.Encoding.Default.GetString(des);
            int index = errorcodeDic.Items.FindIndex(s => s.Code.Trim() == ErrNum.ToString().Trim());
            if (index > -1)
                msg = $"错误码：{ErrNum}，错误信息：{errorcodeDic.Items[index].ZhCn}";
            else
                msg = $"错误码：{ErrNum}，请查看手册！";
            return msg;
        }

        /// <summary>
        /// SDK调用返回值的错误描述
        /// </summary>
        /// <param name="ErrNum"></param>
        /// <returns></returns>
        public string GetErrorInfo(int ErrNum)
        {
            if (ErrNum == 0) { return ""; }
            var msg = string.Empty;
            //MCN420.mcc_get_error_description(ErrNum, des);
            //msg = System.Text.Encoding.Default.GetString(des);
            int index = errorcodeDic.Items.FindIndex(s => s.Code.Trim() == ErrNum.ToString().Trim());
            if (index > -1)
                msg = $"错误码：{ErrNum}，错误信息：{errorcodeDic.Items[index].ZhCn}";
            else
                msg = $"错误码：{ErrNum}，请查看手册！";
            return msg;
        }

        #region 读取ErrorCode XML
        string xmlPath = "Mcn420ErrorCode.xml";
        ErrorCodeList errorcodeDic = new ErrorCodeList();
        void ConvertErrorCodeXML()
        {
            try
            {
                var cardConfigPath = AppDomain.CurrentDomain.BaseDirectory + xmlPath;
                if (!File.Exists(cardConfigPath)) { return; }
                XmlSerializer serializer = new XmlSerializer(typeof(ErrorCodeList));
                using (FileStream fs = new FileStream(cardConfigPath, FileMode.Open))
                {
                    errorcodeDic = (ErrorCodeList)serializer.Deserialize(fs);
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"读取错误代码XML文件失败，错误信息：{ex.Message}__{ex.Source}__{ex.StackTrace}");
            }
        }
        [XmlRoot("errorcodes")]
        public class ErrorCodeList
        {
            [XmlElement("errorcode")]
            public List<ErrorCode> Items { get; set; }
        }

        public class ErrorCode
        {
            [XmlAttribute("code")]
            public string Code { get; set; }

            [XmlElement("zh_cn")]
            public string ZhCn { get; set; }

            [XmlElement("en_us")]
            public string EnUs { get; set; }
        }
        #endregion


        /*
        short ecc_read_inport_extern(WORD CardNo, WORD Channel ， WORD NoteID, WORD
            PortNo,WORD* IoValue)
            功 能：读取指定 EtherCAT 扩展模块的输入口组的电平
            参 数：CardNo 控制卡卡号
            Channel 总线通道号，固定为 2；
            NoteID EtherCAT 地址，从 1001 开始，按从站数往后累加
            PortNo 输入组号（32 位一组，从 0 开始，如 0 表示 0-31 位，1 表示 32-63
            位）
            IoValue 输入组各输入端口的值，bit0~bit31 的值分别代表第 0~31 号输入口
            的电平，1：低电平，0：高电平
            返回值：错误代码
            */
        /// <summary>
        /// 读取指定 EtherCAT 扩展模块的输入口组的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Channel">总线通道号，固定为 2</param>
        /// <param name="NoteID">EtherCAT 地址，从 1001 开始，按从站数往后累加</param>
        /// <param name="PortNo">输入组号（32 位一组，从 0 开始，如 0 表示 0-31 位，1 表示 32-63位）</param>
        /// <param name="IoValue">输入组各输入端口的值，bit0~bit31 的值分别代表第 0~31 号输入口的电平，1：低电平，0：高电平</param>
        /// <returns></returns>
        public int NmcReadInportExtern(int CardNo, int Channel, int NoteID, int PortNo, ref uint IoValue)
        {
            //byte[] value = new byte[4];
            byte[] value = new byte[2];
            value[0] = (byte)(MCN420.YK_read_inbyte((ushort)CardNo, (ushort)((NoteID - 1) * 2 + 8)) & 0xFF);
            value[1] = (byte)(MCN420.YK_read_inbyte((ushort)CardNo, (ushort)((NoteID - 1) * 2 + 1 + 8)) & 0xFF);
            //value[2] = (byte)(MCN420.YK_read_inbyte((ushort)CardNo, (ushort)((NoteID - 1) * 4 + 2 + 8)) & 0xFF);
            //value[3] = (byte)(MCN420.YK_read_inbyte((ushort)CardNo, (ushort)((NoteID - 1) * 4 + 6 + 8)) & 0xFF);
            //result = MCN420.YK_read_inbyte((ushort)CardNo, (ushort)NoteID, (ushort)(PortNo * 2 + 1), ref value[1]);
            IoValue = BytesToUInt16(value);
            return 0;
        }

        uint BytesToUInt32(byte[] bytes)
        {
            if (bytes.Length < 4)
                throw new ArgumentException("需要4字节数组");

            // 小端序：低位在前
            return (uint)(bytes[0] | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24);
        }
        uint BytesToUInt16(byte[] bytes)
        {
            if (bytes.Length != 2)
                throw new ArgumentException("需要4字节数组");

            // 小端序：低位在前
            return (uint)(bytes[0] | bytes[1] << 8);
        }
        uint BytesToUInt32(UInt16[] uint16)
        {
            if (uint16.Length != 2)
                throw new ArgumentException("需要2个int16数组");

            // 小端序：低位在前
            return (uint)(uint16[1] << 16 | uint16[0]);
        }

        /*
        short ecc_read_outport_extern(WORD CardNo, WORD Channel ， WORD NoteID, WORD
        PortNo, WORD* IoValue)
        功 能：读取指定 EtherCAT 扩展模块的输出口组的电平
        参 数：CardNo 控制卡卡号
        Channel 总线通道号，固定为 2；
        NoteID EtherCAT 地址，从 1001 开始，按从站数往后累加
        PortNo 输出组号（32 位一组，从 0 开始，如 0 表示 0-31 位，1 表示 32-63
        位）
        IoValue 输出组各输出端口的值，bit0 ~bit31 的值分别代表第 0~31 号输出
            口的电平，1：低电平，0：高电平
            返回值：错误代码
        */
        /// <summary>
        /// 读取指定 EtherCAT 扩展模块的输出口组的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Channel">总线通道号，固定为 2</param>
        /// <param name="NoteID">EtherCAT 地址，从 1001 开始，按从站数往后累加</param>
        /// <param name="PortNo">输出组号（32 位一组，从 0 开始，如 0 表示 0-31 位，1 表示 32-63位）</param>
        /// <param name="IoValue">输出组各输出端口的值，bit0 ~bit31 的值分别代表第 0~31 号输出口的电平，1：低电平，0：高电平</param>
        /// <returns>错误代码</returns>
        public int NmcReadOutportExtern(int CardNo, int Channel, int NoteID, int PortNo, ref uint IoValue)
        {
            //byte[] value = new byte[4];
            byte[] value = new byte[2];
            value[0] = (byte)(MCN420.YK_read_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 2 + 8)) & 0xFF);
            value[1] = (byte)(MCN420.YK_read_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 2 + 8 + 1)) & 0xFF);
            //value[2] = (byte)(MCN420.YK_read_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 4 + 8 + 2)) & 0xFF);
            //value[3] = (byte)(MCN420.YK_read_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 4 + 8 + 3)) & 0xFF);
            //result = MCN420.YK_read_inbyte((ushort)CardNo, (ushort)NoteID, (ushort)(PortNo * 2 + 1), ref value[1]);
            IoValue = BytesToUInt16(value);
            return 0;
        }


        /*
        short ecc_write_outbit_extern(WORD CardNo, WORD Channel，WORD NoteID, WORD IoBit,WORD IoValue)
        功 能：设置指定 EtherCAT 扩展模块的某个输出端口的电平
        参 数：CardNo 控制卡卡号
        Channel 总线通道号，固定为 2；
        NoteID EtherCAT 地址，从 1001 开始，按从站数往后累加
        IoBit 输出端口号
        IoValue 指定输出端口的电平，0：低电平，1：高电平
        返回值：错误代码
        */
        /// <summary>
        /// 设置指定 EtherCAT 扩展模块的某个输出端口的电平(0：低电平，1：高电平)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Channel">总线通道号，固定为 2</param>
        /// <param name="NoteID">EtherCAT 地址，从 1001 开始，按从站数往后累加</param>
        /// <param name="IoBit">输出端口号</param>
        /// <param name="IoValue">指定输出端口的电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int NmcWriteOutbitExtern(int CardNo, int Channel, int NoteID, int IoBit, ushort IoValue)
        {
            return MCN420.YK_write_outbit((ushort)CardNo, (ushort)(64 + (NoteID - 1) * 16 + IoBit), IoValue);
        }

        /*
        short ecc_write_outport_extern(UInt16 CardNo, UInt16 Channel, UInt16 NoteID, UInt16 portno, UInt32 outport_val)
        功 能：设置指定 EtherCAT 扩展模块的某个输出端口的电平
        参 数：CardNo 控制卡卡号
        Channel 总线通道号，固定为 2；
        NoteID EtherCAT 地址，从 1001 开始，按从站数往后累加
        portno 输出组号（32 位一组，从 0 开始，如 0 表示 0-31 位，1 表示 32-63位）
        IoValue 输出组各输出端口的值，bit0~bit31 的值分别代表第 0~31 号输出口的电平，1：低电平，0：高电平
        返回值：错误代码
        */
        /// <summary>
        /// 设置指定 EtherCAT 扩展模块的某个输出端口的电平(0：低电平，1：高电平)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Channel">总线通道号，固定为 2</param>
        /// <param name="NoteID">EtherCAT 地址，从 1001 开始，按从站数往后累加</param>
        /// <param name="portno">输出端口号</param>
        /// <param name="IoValue">指定输出端口的电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int NmcWriteOutportExtern(int CardNo, int Channel, int NoteID, ushort portno, uint IoValue)
        {
            //return MCN420.ecc_write_outport_extern((ushort)CardNo, (ushort)Channel, (ushort)NoteID, portno, IoValue);
            var Value = SplitUInt32Little_byte_two(IoValue);
            //var result = MCN420.ecc_io_write_outslot((ushort)CardNo, (ushort)NoteID, (ushort)(portno * 2), Value[1]);
            //result = MCN420.ecc_io_write_outslot((ushort)CardNo, (ushort)NoteID, (ushort)(portno * 2 + 1), Value[0]);
            //var result = MCN420.YK_write_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 4 + 2), Value[3]);
            //if (result != 0) goto end;
            //result = MCN420.YK_write_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 4 + 3), Value[2]);
            //if (result != 0) goto end;
            //result = MCN420.YK_write_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 4 + 4), Value[1]);
            //if (result != 0) goto end;
            //result = MCN420.YK_write_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 4 + 5), Value[0]);
            var result = MCN420.YK_write_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 2 + 8), Value[1]);
            if (result != 0) goto end;
            result = MCN420.YK_write_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 2 + 8 + 1), Value[0]);
            if (result != 0) goto end;
            end:
            return result;
        }

        UInt16[] SplitUInt32Little(uint value)
        {
            UInt16[] v = new ushort[2];
            v[0] = (ushort)(value >> 16);
            v[1] = (ushort)(value & 0xFFFF);
            return v;
        }

        byte[] SplitUInt32Little_byte(uint value)
        {
            byte[] v = new byte[4];
            v[0] = (byte)(value >> 24);
            v[1] = (byte)((value >> 16) & 0xFF);
            v[2] = (byte)((value >> 8) & 0xFF);
            v[3] = (byte)(value & 0xFF);
            return v;
        }
        byte[] SplitUInt32Little_byte_two(uint value)
        {
            byte[] v = new byte[2];
            //v[0] = (byte)(value >> 24);
            //v[1] = (byte)((value >> 16) & 0xFF);
            v[0] = (byte)((value >> 8) & 0xFF);
            v[1] = (byte)(value & 0xFF);
            return v;
        }
        /*
                short ecc_read_outbit_extern(WORD CardNo, WORD Channel，WORD NoteID, WORD IoBit,WORD* 
                IoValue)
                    功 能：读取指定 EtherCAT 扩展模块的某个输出端口的电平
                    参 数：CardNo 控制卡卡号
                    Channel 总线通道号，固定为 2；
                    NoteID EtherCAT 地址，从 1001 开始，按从站数往后累加
                    IoBit 输出端口号
                    IoValue 指定输出端口的电平，0：低电平，1：高电平
                返回值：错误代码

                */
        /// <summary>
        /// 读取指定 EtherCAT 扩展模块的某个输出端口的电平(0：低电平，1：高电平)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Channel">总线通道号，固定为 2</param>
        /// <param name="NoteID">EtherCAT 地址，从 1001 开始，按从站数往后累加</param>
        /// <param name="IoBit">输出端口号</param>
        /// <param name="IoValue">指定输出端口的电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int NmcReadOutbitExtern(int CardNo, int Channel, int NoteID, int IoBit, ref ushort IoValue)
        {
            IoValue = (ushort)MCN420.YK_read_outbit((ushort)CardNo, (ushort)((64 + (NoteID - 1) * 16 + IoBit)));
            return 0;
        }
        /// <summary>
        /// 读取指定 EtherCAT 扩展模块的某个输出端口的电平(0：低电平，1：高电平)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Channel">总线通道号，固定为 2</param>
        /// <param name="NoteID">EtherCAT 地址，从 1001 开始，按从站数往后累加</param>
        /// <param name="IoBit">输入端口号</param>
        /// <param name="IoValue">指定输出端口的电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int NmcReadInbitExtern(int CardNo, int Channel, int NoteID, int IoBit, ref ushort IoValue)
        {
            IoValue = (ushort)MCN420.YK_read_inbit((ushort)CardNo, (ushort)((64 + (NoteID - 1) * 16 + IoBit)));
            return 0;
            //return MCN420.ecc_io_read_inbit((ushort)CardNo, (ushort)NoteID, (ushort)IoBit, ref IoValue);
        }

        /// <summary>
        /// 设置主站参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="PortNum">EtherCAT 端口号，固定为 2</param>
        /// <param name="Baudrate"></param>
        /// <param name="NodeCnt"></param>
        /// <param name="MasterId"></param>
        /// <returns>错误代码</returns>
        public int NmcSetCardMasterPara(int CardNo, int PortNum, int Baudrate, int NodeCnt, int MasterId)
        {
            //return MCN420.ecc_set_master_para((ushort)CardNo, (ushort)PortNum, (ushort)Baudrate, (ushort)NodeCnt, (ushort)MasterId);
            return 0;
        }

        /// <summary>
        /// 获取主站参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="PortNum">EtherCAT 端口号，固定为 2</param>
        /// <param name="Baudrate"></param>
        /// <param name="NodeCnt"></param>
        /// <param name="MasterId"></param>
        /// <returns>错误代码</returns>
        public int NmcGetCardMasterPara(int CardNo, int PortNum, ref ushort Baudrate, ref uint NodeCnt, ref ushort MasterId)
        {
            return 0;
            //return MCN420.ecc_get_master_para((ushort)CardNo, (ushort)PortNum, ref Baudrate, ref NodeCnt, ref MasterId);
        }

        /// <summary>
        /// 获取发布版本号（适用于DMC3000/DMC5X10系列脉冲卡、EtherCAT总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int DmcGetCardReleaseVersion(ushort CardNo, ref string ReleaseVersion)
        {
            //,MCC_E1_016PCIe_250902_16:16:49
            uint Version = 0;
            var res = MCN420.YK_get_hardware_version(CardNo, ref Version);
            if (Version == 0)
            {
                ReleaseVersion = string.Empty;
            }
            else
            {
                ReleaseVersion = Version.ToString();
            }
            return res;
            //return MCN420.mcc_board_get_card_version(CardNo, ref ReleaseVersion);
        }

        /// <summary>
        ///硬件复位（适用于所有脉冲/总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int DmcCardBoardReset()
        {
            //return MCN420.mcc_board_reset();
            return 0;
        }

        /// <summary>
        /// 控制卡热复位（适用于EtherCAT、RTEX总线卡）  
        /// </summary>
        /// <returns>错误代码</returns>
        public int DmcCardSoftReset(ushort CardNo)
        {
            //return MCN420.ecc_reset_ecat(CardNo, 0);
            return 0;
        }

        /// <summary>
        /// 控制卡冷复位（适用于所有脉冲/总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int DmcCardCoolReset(ushort CardNo)
        {
            return 0;
            //return MCN420.mcc_board_cold_reset();
        }

        /// <summary>
        /// 控制卡初始复位（适用于EtherCAT总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int DmcCardOriginalReset(ushort CardNo)
        {
            return 0;
            //return MCN420.mcc_board_reset();
        }

        /// <summary>
        /// 设置EtherCAT总线循环周期 
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="Fieldbustype">  EtherCAT 端口号，固定为 2</param>
        /// <param name="CycleTime">CAT 总线循环周期，单位：us，支持 250/500/1000/</param>
        /// <returns></returns>
        public int NmcSetCardCycleTime(int CardNo, int FieldbusType, int CycleTime)
        {
            //return MCN420.ecc_set_cycletime((ushort)CardNo, (ushort)FieldbusType, (uint)CycleTime);
            return 0;
        }

        /// <summary>
        ///  清除总线错误
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <returns></returns>
        public int DmcClearCardErrCode(int CardNo)
        {
            return 0;
            //return MCN420.ecc_clear_errcode((ushort)CardNo, 2);
        }

        /// <summary>
        /// 设置HOME信号（适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="org_logic"></param>
        /// <param name="filter">0~127</param>
        /// <returns></returns>
        public int NmcSetCardAxisHomeMove(int CardNo, int axis, int org_logic, double filter)
        {
            //return MCN420.mcc_set_home_pin_logic((ushort)CardNo, (ushort)axis, (ushort)org_logic, filter);
            return MCN420.YK_set_origin_config((ushort)CardNo, (ushort)axis, (ushort)org_logic, (uint)filter);
            return 0;
        }

        /// <summary>
        /// 获取HOME信号（适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="org_logic"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public int NmcGetCardAxisHomeMove(int CardNo, int axis, ref UInt16 org_logic, ref double filter)
        {
            uint value0 = 0;
            uint value1 = 0;
            var res = MCN420.YK_get_origin_config((ushort)CardNo, (ushort)axis, ref value0, ref value1);
            org_logic = (ushort)value0; filter = value1;
            return res;
            //return MCN420.mcc_get_home_pin_logic((ushort)CardNo, (ushort)axis, ref org_logic, ref filter);
            return 0;

        }
        void SleepWaitMove()
        {
            Thread.Sleep(10);
        }

        /// <summary>
        /// 启动轴回零
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">轴编号</param>
        /// <returns></returns>
        public int DmcCardAxisHomeMove(int CardNo, int axis)
        {
            var param = AxisHomeParaDic[Tuple.Create(CardNo, axis)];
            string info = $"获取回原参数：卡：{CardNo}，轴{axis}，回原加速度：{param.Acc},回零模式：{param.HomeMode}，回零高速：{param.HighSpeed}，回零低速：{param.LowSpeed}，实际轴号：{param.axis}";
            CMessageAddTipsMessage?.Invoke("DT1000",info);
            MCN420.YK_remove_axis_alm((ushort)CardNo, (ushort)axis);
            var res = MCN420.YK_home_move((ushort)CardNo, ref param);
            info = $"执行YK_home_move后，实际回原参数：卡：{CardNo}，轴{axis}，回原加速度：{param.Acc},回零模式：{param.HomeMode}，回零高速：{param.HighSpeed}，回零低速：{param.LowSpeed}，实际轴号：{param.axis}";
            CMessageAddTipsMessage?.Invoke("DT1000", info);
            SleepWaitMove();
            return res;
        }

        /// <summary>
        /// 设置指定轴的回原点模式（适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="home_dir"></param>
        /// <param name="vel"></param>
        /// <param name="mode"></param>
        /// <param name="EZ_count"></param>
        /// <returns></returns>
        public int DmcSetCardAxisHomeMode(int CardNo, int axis, int home_dir, double startvel, double vel, int mode, double tacc, double tdec)
        {
            if (AxisHomeParaDic.ContainsKey(Tuple.Create(CardNo, axis)))
            {
                //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                if (tacc <= 0)
                {
                    tacc = 0.2;
                }
                AxisHomeParaDic[Tuple.Create(CardNo, axis)] = new Home_Req
                {
                    CmdNum = 0,
                    ParamRemain = 0,
                    Channel = 0,
                    axis = (uint)axis,
                    LowSpeed = startvel / 1000,
                    HighSpeed = (home_dir == 0 ? -vel : vel) / 1000,
                    Acc = ((vel - startvel) / 1000 - 0) / (tacc * 1000),
                    HomeMode = 0,
                    TDirTime = 50,
                };
                return 0;
            }
            throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{axis}");
        }

        /// <summary>
        /// 读取指定轴的回原点模式（适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">轴编号</param>
        /// <returns></returns>
        public int DmcGetCardAxisHomeMode(int CardNo, int axis)
        {
            UInt16 home_dir = 0;
            double vel = 0;
            double maxvel = 0;
            UInt16 home_mod = 0;
            UInt16 EZ_count = 0;
            double tacc = 0;
            double tdec = 0;
            if (AxisHomeParaDic.ContainsKey(Tuple.Create(CardNo, axis)))
            {

            }
            return 0;
            //return MCN420.ecc_home_get_para((ushort)CardNo, (ushort)axis, ref home_mod, ref vel, ref maxvel, ref tacc, ref tdec);
        }

        /// <summary>
        /// 设置回零遇限位是否反找（适用于DMC3000/5X10系列脉冲卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="Axis">轴编号</param>
        /// <param name="Enable">是否反找</param>
        /// <returns></returns>
        public int DmcSetCardHomeELReturn(int CardNo, int Axis, int Enable)
        {
            //return MCN420.mcc_set_home_el_return((ushort)CardNo, (ushort)Axis, (ushort)Enable);
            return 0;
        }

        /// <summary>
        /// 获取遇限位反找使能（适用于DMC3000/5X10系列脉冲卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="Axis">轴编号</param>
        /// <param name="Enable">是否反找</param>
        /// <returns></returns>
        public int DmcGetCardHomeELReturn(int CardNo, int Axis, ref UInt16 Enable)
        {
            return 0;
            //return MCN420.mcc_get_home_el_return((ushort)CardNo, (ushort)Axis, ref Enable);
        }

        /// <summary>
        /// 设置回零速度参数（适用于Rtex总线卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="Axis">轴号</param>
        /// <param name="Low_Vel">低速</param>
        /// <param name="High_Vel">高速</param>
        /// <param name="Tacc">总加速时间</param>
        /// <param name="Tdec">总减速时间</param>
        /// <returns></returns>
        public int DmcSetCardHomeProfileUnit(int CardNo, int Axis, double Low_Vel, double High_Vel, double Tacc, double Tdec)
        {
            return 0;
            //return MCN420.mcc_set_home_profile_unit((ushort)CardNo, (ushort)Axis, Low_Vel, High_Vel, Tacc, Tdec);
        }

        /// <summary>
        /// 获取回零速度参数（适用于Rtex总线卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="Axis">轴号</param>
        /// <param name="Low_Vel">低速</param>
        /// <param name="High_Vel">高速</param>
        /// <param name="Tacc">总加速时间</param>
        /// <param name="Tdec">总减速时间</param>
        /// <returns></returns>
        public int DmcGetCardHomeProfileUnit(int CardNo, int Axis, ref double Low_Vel, ref double High_Vel, ref double Tacc, ref double Tdec)
        {
            return 0;
            //return MCN420.mcc_get_home_profile_unit((ushort)CardNo, (ushort)Axis, ref Low_Vel, ref High_Vel, ref Tacc, ref Tdec);
        }

        /// <summary>
        /// 获取回零执行状态（适用于所有脉冲/总线卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="Axis">轴号</param>
        /// <param name="state">状态结果</param>
        /// <returns></returns>
        public int DmcGetCardHomeResult(int CardNo, int Axis, ref UInt16 state)
        {
            var res = MCN420.YK_get_axis_home_status((ushort)CardNo, (ushort)Axis);
            state = (ushort)res;
            return 0;
        }
        #region 前瞻
        /// <summary>
        /// 获取插补坐标系
        /// </summary>
        /// <param name="CardNo"></param>
        /// <returns></returns>
        public int DmcGetTotalLiners(int CardNo, uint totleLiners)
        {
            //return MCN420.mcc_get_total_liners((ushort)CardNo, ref totleLiners);
            return 0;
        }

        /// <summary>
        /// 设置连续插补前瞻模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">坐标系号，取值范围：0~5</param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能</param>
        /// <param name="LookaheadSegments"></param>
        /// <param name="PathError"></param>
        /// <param name="LookaheadAcc"></param>
        /// <returns></returns>
        public int DmcSetCardContiLookaheadMode(int CardNo, int Crd, int enable, int LookaheadSegments, double PathError, double LookaheadAcc)
        {
            //return MCN420.mcc_conti_set_lookahead_mode((ushort)CardNo, (ushort)Crd, (ushort)enable, (ushort)LookaheadSegments, PathError, LookaheadAcc);
            return 0;
        }

        /// <summary>
        /// 获取连续插补前瞻模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">坐标系号，取值范围：0~5</param>
        /// <returns></returns>
        public int DmcGetCardContiLookaheadMode(int CardNo, int Crd, ref ushort enable, ref int LookaheadSegments, ref double PathError, ref double LookaheadAcc)
        {
            return 0;
            //return MCN420.mcc_conti_get_lookahead_mode((ushort)CardNo, (ushort)Crd, ref enable, ref LookaheadSegments, ref PathError, ref LookaheadAcc);
        }

        /// <summary>
        /// 打开连续插补指令表
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">坐标系号，取值范围：0~5</param>
        /// <param name="axis">轴编号</param>
        /// <param name="AxisNums">轴号数组</param>
        /// <returns></returns>
        public int DmcOpenCardContiList(int CardNo, int Crd, int axis, UInt16[] AxisNums)
        {
            return 0;
            //return MCN420.mcc_conti_open_list((ushort)CardNo, (ushort)Crd, (ushort)axis, AxisNums);
        }

        /// <summary>
        /// 开始连续插补
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">坐标系号，取值范围：0~5</param>
        /// <returns></returns>
        public int DmcCardContiStartList(int CardNo, int Crd)
        {
            return 0;
            //return MCN420.mcc_conti_start_list((ushort)CardNo, (ushort)Crd);
        }

        #endregion
        /// <summary>
        /// 设置螺距补偿（开启、关闭）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴号 </param>
        /// <param name="enable">螺距补偿：开启\关闭</param>
        /// <returns></returns>
        public int DmcSetCardLeadScrewCompEnable(int CardNo, int axis, int enable)
        {
            return 0;
            //return MCN420.mcc_enable_leadscrew_comp((ushort)CardNo, (ushort)axis, (ushort)enable);
        }

        /// <summary>
        /// 设置螺距补偿参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴号 </param>
        /// <param name="n"></param>
        /// <param name="startpos"></param>
        /// <param name="lenpos"></param>
        /// <param name="pCompPos"></param>
        /// <param name="pCompNeg"></param>
        /// <returns></returns>
        public int DmcSetCardLeadScrewCompConfig(int CardNo, int axis, int n, int startpos, int lenpos, int[] pCompPos, int[] pCompNeg)
        {
            return 0;
            //return MCN420.mcc_set_leadscrew_comp_config((ushort)CardNo, (ushort)axis, (ushort)n, startpos, lenpos, pCompPos, pCompNeg);
        }

        /// <summary>
        /// 读取螺距补偿参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴号 </param>
        /// <param name="n"></param>
        /// <param name="startpos"></param>
        /// <param name="lenpos"></param>
        /// <param name="pCompPos"></param>
        /// <param name="pCompNeg"></param>
        /// <returns></returns>
        public int DmcGetCardLeadScrewCompConfig(int CardNo, int axis, ref ushort n, ref int startpos, ref int lenpos, int[] pCompPos, int[] pCompNeg)
        {
            return 0;
            //return MCN420.mcc_get_leadscrew_comp_config((ushort)CardNo, (ushort)axis, ref n, ref startpos, ref lenpos, pCompPos, pCompNeg);
        }

        /// <summary>
        /// 设置轴运动模式：
        /// 1为pp模式，6为回零模式，8为csp模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="runmode">运动模式</param>
        /// <returns>short类型值</returns>
        public int NmcSetCardAxisRunMode(int CardNo, int axis, ushort runmode)
        {
            //return MCN420.mcc_set_axis_run_mode((ushort)CardNo, (ushort)axis, runmode);
            return 0;
        }

        /// <summary>
        /// 获取轴状态
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="axis"></param>
        /// <returns>ushort错误编号</returns>
        public int NmcGetCardAxisErrCode(int CardNo, int axis, ref ushort errcode)
        {
            errcode = (ushort)MCN420.YK_get_axis_alm_status((ushort)CardNo, (ushort)axis);
            return 0;
        }

        /// <summary>
        ///  清除总线轴错误码
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴轴号</param>
        /// <returns></returns>
        public int NmcClearCardAxisErrCode(int CardNo, int axis)
        {
            return MCN420.YK_remove_axis_alm((ushort)CardNo, (ushort)axis);
        }

        /// <summary>
        ///  清除端口报警
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="PortNum">端口号</param>
        /// <returns></returns>
        public int NmcClearCardAlarmFieldbus(int CardNo, int PortNum)
        {
            //return MCN420.ecc_clear_alarm_fieldbus((ushort)CardNo, (ushort)PortNum);
            return 0;
        }

        /// <summary>
        /// 添加比较点（适用于DMC5X10系列脉冲卡、E5032总线卡） (单轴高速)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口） </param>
        /// <param name="pos">1：队列模式下：添加比较位置，单位：pulse | 2：线性模式下：更新起始比较位置，单位：pulse | 3：其他模式下：更新比较位置，单位：pulse</param>
        /// <returns></returns>
        public int DmcAddCardHcmpPoints(int CardNo, int hcmp, double cmp_pos)
        {
            //return MCN420.mcc_hcmp_add_point_unit((ushort)CardNo, (ushort)hcmp, cmp_pos);
            return 0;
        }
        #region 二维比较（低速）待实现
        /// <summary>
        /// 设置二维位置比较器 （二维低速）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能 </param>
        /// <param name="cmp_source">   比较源，0：指令位置计数器，1：编码器计数器</param>
        /// <returns></returns>
        public int DmcSetCardCompareConfigExtern(int CardNo, int enable, int cmp_source)
        {
            //ushort[] CmpList = new ushort[2] { 0, 1 };
            //ushort[] CmpAxisList = new ushort[2] { 0, 1 };
            //ushort CmpPulseNum = 10;
            //int CmpPulseTime = 1000; //单位：1us-20s
            //ushort CmpPulseDis = 20;
            return MCN420.YK_cmp_2d_set_config((ushort)CardNo, (ushort)enable, (ushort)cmp_source);
        }

        /// <summary>
        /// 获取二维位置比较器 （二维低速）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能 </param>
        /// <param name="cmp_source">   比较源，0：指令位置计数器，1：编码器计数器</param>
        /// <returns></returns>
        public int DmcGetCardCompareConfigExtern(int CardNo, ref ushort enable, ref ushort cmp_source)
        {
            uint value0 = 0;
            uint value1 = 0;
            var res = MCN420.YK_cmp_2d_get_config((ushort)CardNo, ref value0, ref value1);
            enable = (ushort)value0;
            cmp_source = (ushort)value1;
            return res;
            //return MCN420.mcc_cmp_2d_get_mode((ushort)CardNo, ref enable, ref cmp_source);
            return 0;
        }

        /// <summary>
        /// 添加比较点（适用于所有脉冲卡）（二维低速）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号 </param>
        /// <param name="pos">设置比较位置为 X pulse</param>
        /// <param name="dir">设置比较模式</param>
        /// <param name="action">设置触发功能（为 IO 电平取反）</param>
        /// <param name="actpara">设置输出 IO 端口 (0 触发功能)</param>
        /// <returns></returns>
        public int DmcAddCardComparePointsExtern(int CardNo, UInt16[] axis, int[] pos, UInt16[] dir, int action, int actpara)
        {
            return MCN420.YK_cmp_2d_add_point((ushort)CardNo, Array.ConvertAll(axis, s => (uint)s), pos, Array.ConvertAll(dir, s => (uint)s), (ushort)action, (uint)actpara);
            return 0;
        }

        /// <summary>
        /// 获取当前比较点（适用于所有脉冲/总线卡）（二维低速）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public int DmcGetCardCompareCurrentPointsExtern(int CardNo, int[] pos = null)
        {
            return MCN420.YK_cmp_2d_get_current_point((ushort)CardNo, pos);
            return 0;
        }

        /// <summary>
        /// 查询已经比较过的点（适用于所有脉冲卡、EtherCAT总线卡）（二维低速）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <returns></returns>
        public int DmcGetCardCompareRunnedPointsExtern(int CardNo, ref int pointNum)
        {
            uint num = 0;
            var res = MCN420.YK_cmp_2d_get_points_runned((ushort)CardNo, ref num);
            pointNum = (int)num;
            return res;
            return 0;
        }

        /// <summary>
        /// 清除所有比较点（适用于所有脉冲/总线卡）（二维低速）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <returns></returns>
        public int DmcClearCardComparePoints(int CardNo, ushort cmpNo)
        {
            return MCN420.YK_cmp_2d_clear_point((ushort)CardNo);
        }
        #endregion
        #region 二维比较 （高速）待实现

        /// <summary>
        /// 设置二维高速比较使能 （二维高速）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口） </param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能</param>
        /// <returns></returns>
        public int DmcSetCardHcmp2DEnable(int CardNo, int hcmp, int enable)
        {
            //return MCN420.YK_hcmp_2d_set_config((ushort)CardNo, (ushort)hcmp, (ushort)enable);
            return 0;
        }

        /// <summary>
        /// 获取二维高速比较使能 （二维高速）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口） </param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能 </param>
        /// <returns></returns>
        public int DmcGetCardHcmp2DEnable(int CardNo, int hcmp, ref ushort enable)
        {
            return 0;
            //return MCN420.mcc_hcmp_2d_get_enable((ushort)CardNo, (ushort)hcmp, ref enable);
        }

        /// <summary>
        /// 配置二维位置高速比较器（二维高速   适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号  </param>
        /// <param name="hcmp"> 高速比较号</param>
        /// <param name="cmp_mode"> 比较模式：0：进入误差带后触发  |  1：进入误差带单轴等于后再触发</param>
        /// <param name="x_axis">x轴关联轴号</param>
        /// <param name="x_cmp_source">x 轴比较位置源：0：指令位置，1：反馈位置</param>
        /// <param name="y_axis">y轴关联轴号</param>
        /// <param name="y_cmp_source">y 轴比较位置源：0：指令位置，1：反馈位置</param>
        /// <param name="error">轴误差带设置，单位：unit </param>
        /// <param name="cmp_logic">有效电平：0：低电平，1：高电</param>
        /// <param name="time">脉冲宽度，单位：us，取值范围：1us-20s </param>
        /// <param name="pwm_enable">pwm 模式使能</param>
        /// <param name="duty">占空比 </param>
        /// <param name="freq">频率 </param>
        /// <param name="port_sel">端口</param>
        /// <param name="pwm_number"> 输出的 pwm 脉冲数</param>
        /// <returns></returns>
        public int DmcSetCardHcmp2DConfig(int CardNo, int hcmp, int cmp_mode, int x_axis, int x_cmp_source, int y_axis, int y_cmp_source, int error, int cmp_logic, int time, int pwm_enable, double duty, int freq, int port_sel, int pwm_number)
        {
            //return MCN420.YK_hcmp_2d_set_param((ushort)CardNo, (ushort)hcmp, (ushort)cmp_mode, (ushort)x_axis, (ushort)x_cmp_source, (ushort)y_axis, (ushort)y_cmp_source, error, (ushort)cmp_logic, time, (ushort)pwm_enable, duty, freq, (ushort)port_sel, (ushort)pwm_number);
            return 0;
        }

        /// <summary>
        /// 获取二维位置高速比较器（二维高速   适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号  </param>
        /// <param name="hcmp"> 高速比较号</param>
        /// <param name="cmp_mode"></param>
        /// <param name="x_axis"></param>
        /// <param name="x_cmp_source"></param>
        /// <param name="y_axis"></param>
        /// <param name="y_cmp_source"></param>
        /// <param name="error"></param>
        /// <param name="cmp_logic"></param>
        /// <param name="time"></param>
        /// <param name="pwm_enable"></param>
        /// <param name="duty"></param>
        /// <param name="freq"></param>
        /// <param name="port_sel"></param>
        /// <param name="pwm_number"></param>
        /// <returns></returns>
        public int DmcGetCardHcmp2DConfig(int CardNo, int hcmp, ref ushort cmp_mode, ref ushort x_axis, ref ushort x_cmp_source, ref ushort y_axis, ref ushort y_cmp_source, ref int error, ref ushort cmp_logic, ref int time, ref ushort pwm_enable, ref double duty, ref int freq, ref ushort port_sel, ref ushort pwm_number)
        {
            return 0;
            //return MCN420.mcc_hcmp_2d_get_config((ushort)CardNo, (ushort)hcmp, ref cmp_mode, ref x_axis, ref x_cmp_source, ref y_axis, ref y_cmp_source, ref error, ref cmp_logic, ref time, ref pwm_enable, ref duty, ref freq, ref port_sel, ref pwm_number);
        }

        /// <summary>
        /// 配置二维位置高速比较器（二维高速  适用于DMC5X10系列脉冲卡、E5032总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号  </param>
        /// <param name="hcmp"> 高速比较号</param>
        /// <param name="cmp_mode"> 比较模式：0：进入误差带后触发  |  1：进入误差带单轴等于后再触发</param>
        /// <param name="x_axis">x轴关联轴号</param>
        /// <param name="x_cmp_source">x 轴比较位置源：0：指令位置，1：反馈位置</param>
        /// <param name="x_cmp_error">x 轴误差带设置，单位：unit</param>
        /// <param name="y_axis">y 轴关联轴号</param>
        /// <param name="y_cmp_source">y 轴比较位置源：0：指令位置，1：反馈位置</param>
        /// <param name="y_cmp_error">y 轴误差带设置，单位：unit </param>
        /// <param name="cmp_logic">    有效电平：0：低电平，1：高电</param>
        /// <param name="time">脉冲宽度，单位：us，取值范围：1us-20s </param>
        /// <returns></returns>
        public int DmcSetCardHcmp2DConfig(int CardNo, int hcmp, int cmp_mode, int x_axis, int x_cmp_source, double x_cmp_error, int y_axis, int y_cmp_source, double y_cmp_error, int cmp_logic, int time)
        {
            return 0;
            //return MCN420.mcc_hcmp_2d_set_config_unit((ushort)CardNo, (ushort)hcmp, (ushort)cmp_mode, (ushort)x_axis, (ushort)x_cmp_source, (ushort)x_cmp_error, (ushort)y_axis, (ushort)y_cmp_source, y_cmp_error, (ushort)cmp_logic, time);
        }

        /// <summary>
        /// 获取二维位置高速比较器（二维高速  适用于DMC5X10系列脉冲卡、E5032总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号  </param>
        /// <param name="hcmp"> 高速比较号</param>
        /// <param name="cmp_mode"> 比较模式：0：进入误差带后触发  |  1：进入误差带单轴等于后再触发</param>
        /// <param name="x_axis">x轴关联轴号</param>
        /// <param name="x_cmp_source">x 轴比较位置源：0：指令位置，1：反馈位置</param>
        /// <param name="x_cmp_error">x 轴误差带设置，单位：unit</param>
        /// <param name="y_axis">y 轴关联轴号</param>
        /// <param name="y_cmp_source">y 轴比较位置源：0：指令位置，1：反馈位置</param>
        /// <param name="y_cmp_error">y 轴误差带设置，单位：unit </param>
        /// <param name="cmp_logic">    有效电平：0：低电平，1：高电</param>
        /// <param name="time">脉冲宽度，单位：us，取值范围：1us-20s </param>
        /// <returns></returns>
        public int DmcGetCardHcmp2DConfig(int CardNo, int hcmp, ref ushort cmp_mode, ref ushort x_axis, ref ushort x_cmp_source, ref double x_cmp_error, ref ushort y_axis, ref ushort y_cmp_source, ref double y_cmp_error, ref ushort cmp_logic, ref int time)
        {
            return 0;
            //return MCN420.mcc_hcmp_2d_get_config_unit((ushort)CardNo, (ushort)hcmp, ref cmp_mode, ref x_axis, ref x_cmp_source, ref x_cmp_error, ref y_axis, ref y_cmp_source, ref y_cmp_error, ref cmp_logic, ref time);
        }

        /// <summary>
        /// 添加二维高速位置比较点（适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="hcmp">高速比较器号 </param>
        /// <param name="x_cmp_pos"> 队列模式下：添加 x 比较位置，单位：unit </param>
        /// <param name="y_cmp_pos">队列模式下：添加 x 比较位置，单位：unit  </param>
        /// <returns></returns>
        public int DmcAddCardHcmp2DPoint(int CardNo, int hcmp, int x_cmp_pos, int y_cmp_pos)
        {
            return 0;
            //return MCN420.mcc_hcmp_2d_add_point((ushort)CardNo, (ushort)hcmp, (ushort)x_cmp_pos, (ushort)y_cmp_pos);
        }

        /// <summary>
        /// 添加二维高速位置比较点（适用于DMC5X10系列脉冲卡、E5032总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="hcmp">高速比较器号 </param>
        /// <param name="x_cmp_pos"> 队列模式下：添加 x 比较位置，单位：unit </param>
        /// <param name="y_cmp_pos">队列模式下：添加 x 比较位置，单位：unit  </param>
        /// <param name="cmp_outbit">输出口号 </param>
        /// <returns></returns>
        public int DmcAddCardHcmp2DPointUnit(int CardNo, int hcmp, double x_cmp_pos, double y_cmp_pos, int cmp_outbit)
        {
            return 0;
            //return MCN420.mcc_hcmp_2d_add_point_unit((ushort)CardNo, (ushort)hcmp, x_cmp_pos, y_cmp_pos, (ushort)cmp_outbit);
        }

        /// <summary>
        /// 读取高速比较参数（适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="hcmp">高速比较器号 </param>
        /// <param name="remained_points">返回可添加比较点数 </param>
        /// <param name="x_current_point">返回当前 x 比较点位置</param>
        /// <param name="y_current_point">返回当前 y 比较点位置 </param>
        /// <param name="runned_points">返回已比较点数</param>
        /// <param name="current_state">比较器状态 1 正在输出 0 输出完成</param>
        /// <returns></returns>
        public int DmcGetCardHcmp2DCurrentState(int CardNo, int hcmp, ref int remained_points, ref int x_current_point, ref int y_current_point, ref int runned_points, ref UInt16 current_state)
        {
            return 0;
            //return MCN420.mcc_hcmp_2d_get_current_state((ushort)CardNo, (ushort)hcmp, ref remained_points, ref x_current_point, ref y_current_point, ref runned_points, ref current_state);
        }

        /// <summary>
        /// 读取高速比较参数（适用于DMC5X10系列脉冲卡、EtherCAT总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="hcmp">高速比较器号 </param>
        /// <param name="remained_points">返回可添加比较点数 </param>
        /// <param name="x_current_point">返回当前 x 比较点位置</param>
        /// <param name="y_current_point">返回当前 y 比较点位置 </param>
        /// <param name="runned_points">返回已比较点数</param>
        /// <param name="current_state">比较器状态 1 正在输出 0 输出完成</param>
        /// <param name="current_outbit">返回当前输出口 </param>
        /// <returns></returns>
        public int DmcGetCardHcmp2DCurrentStateUnit(int CardNo, int hcmp, ref int remained_points, ref double x_current_point, ref double y_current_point, ref Int32 runned_points, ref UInt16 current_state, ref UInt16 current_outbit)
        {
            return 0;
            //return MCN420.mcc_hcmp_2d_get_current_state_unit((ushort)CardNo, (ushort)hcmp, ref remained_points, ref x_current_point, ref y_current_point, ref runned_points, ref current_state, ref current_outbit);
        }

        /// <summary>
        /// 强制二维高速比较输出（适用于所有脉冲卡、EtherCAT总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">保留，参数值为 0 </param>
        /// <param name="cmp_outbit">强制二维比较输出，设置为1使能</param>
        /// <returns></returns>
        public int DmcCardHcmp2DForceOutPut(int CardNo, int hcmp, int cmp_outbit)
        {
            return 0;
            //return MCN420.mcc_hcmp_2d_force_output((ushort)CardNo, (ushort)hcmp, (ushort)cmp_outbit);
        }

        /// <summary>
        /// 配置二维比较PWM输出模式（适用于DMC5X10系列脉冲卡、EtherCAT总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="hcmp">高速比较器号</param>
        /// <param name="pwm_enable">读取 pwm 模式使能状态 </param>
        /// <param name="duty">读取占空比</param>
        /// <param name="freq">读取频率</param>
        /// <param name="pwm_number">读取输出的 pwm 脉冲数 </param>
        /// <returns>二维比较PWM输出模式</returns>
        public int DmcSetCardHcmp2DPWMOutPut(int CardNo, int hcmp, int pwm_enable, double duty, double freq, int pwm_number)
        {
            return 0;
            //return MCN420.mcc_hcmp_2d_set_pwmoutput((ushort)CardNo, (ushort)hcmp, (ushort)pwm_enable, duty, freq, (ushort)pwm_number);
        }

        /// <summary>
        /// 获取二维比较PWM输出模式（适用于DMC5X10系列脉冲卡、EtherCAT总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="hcmp">高速比较器号</param>
        /// <param name="pwm_enable">读取 pwm 模式使能状态 </param>
        /// <param name="duty">读取占空比</param>
        /// <param name="freq">读取频率</param>
        /// <param name="pwm_number">读取输出的 pwm 脉冲数 </param>
        /// <returns></returns>
        public int DmcGetCardHcmp2DPWMOutPut(int CardNo, int hcmp, ref UInt16 pwm_enable, ref double duty, ref double freq, ref UInt16 pwm_number)
        {
            return 0;
            //return MCN420.mcc_hcmp_2d_get_pwmoutput((ushort)CardNo, (ushort)hcmp, ref pwm_enable, ref duty, ref freq, ref pwm_number);
        }


        /// <summary>
        /// 清除二维高速位置比较点（适用于所有脉冲卡、EtherCAT总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器号</param>
        /// <returns></returns>
        public int DmcClearCardHcmp2DPoints(int CardNo, int hcmp)
        {
            return MCN420.YK_hcmp_2d_clear_point((ushort)CardNo, (ushort)hcmp);
            return 0;
        }

        #endregion







        // DMCE3032卡
        #region 板卡设置函数
        int num = 0;
        /// <summary>
        /// 控制卡初始化函数
        /// </summary>
        /// <returns>卡的数量</returns>
        public int DmcInitCard()
        {
            Stopwatch sw = new Stopwatch();
        recall:
            var f1 = MCN420.YK_board_init();
            if (f1 <= 0 || f1 > 16)//todo:2022.11.26
            {
                if (!sw.IsRunning) sw.Restart();
                Thread.Sleep(2000);
                if (sw.ElapsedMilliseconds < 8400)
                {
                    MCN420.YK_board_close();
                    goto recall;
                }
                IsInit = false;
            }
            else
            {
                num = (int)f1;
                IsInit = true;
                for (int i = 0; i < f1; i++)
                {
                    //Heart Keep
                    if (false)
                    {// heart break to Stop All Axis
                        MCN420.YK_set_heartbeat_enable((ushort)i, 1, 2000);
                        MCN420.YK_set_heartbeat_Keep((ushort)i);
                    }
                    for (int j = 0; j < 8; j++)
                    {
                        AxisHomeParaDic.TryAdd(Tuple.Create(i, j), new Home_Req());
                        AxisPMoveParaDic.TryAdd(Tuple.Create(i, j), new Single_Req_Param()
                        {
                            ParamRemain = 0,
                            CmdNum = 0,
                            Channel = 2,
                        });
                        AxisVMoveParaDic.TryAdd(Tuple.Create(i, j), new Jog_Req_Param()
                        {
                            ParamRemain = 0,
                            CmdNum = 0,
                            Channel = 2,
                        });
                        MCN420.YK_set_gear_ratio((uint)i, (uint)j, 0, 0, 0);
                    }
                    for (int j = 0; j < 4; j++)
                    {
                        AxisLineParaDic.TryAdd(Tuple.Create(i, j), new Line_Req_Param());
                        AxisARC2DParaDic.TryAdd(Tuple.Create(i, j), new ARC_2D_Req_Param());
                        AxisARC2D3PointParaDic.TryAdd(Tuple.Create(i, j), new Arc_2D_3Point_Req_Param());
                        AxisARC3DParaDic.TryAdd(Tuple.Create(i, j), new CMD_3DArc_Req_Param());
                        AxisHelix2DParaDic.TryAdd(Tuple.Create(i, j), new Helix_2D_Req_Param());
                        AxisHelix3DParaDic.TryAdd(Tuple.Create(i, j), new Helix_3D_Req_Param());
                        AxisCone3DParaDic.TryAdd(Tuple.Create(i, j), new Cone_3D_Req_Param());
                    }
                }
            }
            return (int)f1;
        }

        /// <summary>
        /// 关闭控制卡（适用于所有脉冲/总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int DmcCardBoardClose()
        {
            return (int)MCN420.YK_board_close();
        }

        /// <summary>
        /// 获取控制卡硬件 ID 号
        /// </summary>
        /// <param name="cardnum">返回初始化成功的卡数 </param>
        /// <param name="cardtypes">返回控制卡固件类型数组</param>
        /// <param name="CardNos">返回控制卡硬件 ID 号数组，卡号按从小到大顺序排列  </param>
        /// <returns>错误代码</returns>
        public int DmcGetCardInList(ref ushort cardnum, ref uint[] cardtypes, ref ushort[] CardIdList)
        {
            //var typeVar = new List<u>
            for (int i = 0; i < num; i++)
            {
                uint device_type = 0;
                MCN420.YK_get_device_type((ushort)i, ref device_type);
                cardtypes[i] = (ushort)device_type;
                CardIdList[i] = (ushort)i;
            }
            cardnum = (ushort)num;
            return /*MCN420.mcc_board_get_CardInfList(ref cardnum, cardtypes, CardIdList)*/0;
        }

        /// <summary>
        /// 获取控制卡硬件版本号（适用于DMC3000/DMC5X10系列脉冲卡、EtherCAT总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <returns></returns>
        public int DmcGetCardVersion(ushort CardNo, ref UInt32 CardVersion)
        {
            return MCN420.YK_get_hardware_version(CardNo, ref CardVersion);

        }
        /// <summary>
        /// 获取控制卡固件版本号（适用于所有脉冲/总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int DmcGetCardSoftVersion(ushort CardNo, ref uint FirmID, ref uint SubFirmID)
        {
            return MCN420.YK_get_firmware_version(CardNo, ref FirmID);
        }

        /// <summary>
        /// 获取控制卡动态库文件版本号
        /// </summary>
        /// <param name="LibVer">返回库版本号</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardLibVersion(int CardID, ref UInt32 LibVer)
        {
            return MCN420.YK_get_dll_lib_version((ushort)CardID, ref LibVer);
        }

        /// <summary>
        /// 获取指定卡轴数（适用于所有脉冲卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int DmcGetCardTotalAxes(ushort CardNo, ref UInt32 TotalAxis)
        {
            var res = MCN420.YK_get_device_type(CardNo, ref TotalAxis);
            if (TotalAxis == 420)
            {
                TotalAxis = 8;
            }
            else TotalAxis = 4;
            return res;
        }

        /// <summary>
        /// 下载从站配置参数文件
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="ConfigName">参数文件名，默认"AxisPara.ini"</param>
        /// <returns>错误代码</returns>
        public int GetCardDownloadConfigfile(int CardNo, string ConfigName = "AxisPara.ini")
        {
            return MCN420.YK_download_configfile((ushort)CardNo, ConfigName);
        }

        /// <summary>
        /// 下载总线配置文件
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="buffer">配置文件字符串缓存，UTF8 编码</param>
        /// <param name="fileincontrol">文件名（保留参数）</param>
        /// <param name="filetype">文件类型</param>
        /// <returns>错误代码</returns>
        public int GetCardDownloadMemfile(int CardNo, string name)
        {
            //return MCN420.mcc_board_download_eni_file((ushort)CardNo, buffer, (uint)buffer.Length, fileincontrol, (ushort)filetype);
            //return MCN420.mcc_board_download_eni_file((ushort)CardNo, name.Trim());
            return 0;
        }

        /// <summary>
        /// 下载固件文件
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="FileName">文件路径：参数文件名+后缀：相对路径；完整描述参数文件的路径+文件名后缀：绝对路径</param>
        /// <returns></returns>
        public int DmcGetDownloadFirmware(int CardNo, String FileName)
        {
            //return MCN420.mcc_download_firmware((ushort)CardNo, FileName);
            return 0;
        }
        #endregion

        #region 脉冲当量设置函数

        /// <summary>
        /// 设置轴的脉冲当量
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="equiv">脉冲当量，单位：pulse/unit</param>
        /// <returns>short类型值</returns>
        public int DmcSetCardAxisEquiv(int CardNo, int axis, double equiv)
        {
            return MCN420.YK_set_axis_out_pulse_mode((ushort)CardNo, (ushort)axis, (uint)equiv);
        }
        /// <summary>
        /// 获取轴的脉冲当量
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="equiv">脉冲当量，单位：pulse/unit</param>
        /// <returns>脉冲当量</returns>
        public int DmcGetCardAxisEquiv(int CardNo, int axis, ref double equiv)
        {
            uint mode = 0;
            var res = MCN420.YK_get_axis_out_pulse_mode((ushort)CardNo, (ushort)axis, ref mode);
            equiv = mode;
            return res;

        }
        #endregion

        #region 回原点运动函数

        /// <summary>
        /// 启动 EtherCAT 总线轴回零
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">轴编号</param>
        /// <returns></returns>
        public int CardNmcAxisNmcHomeMove(int CardNo, int axis)
        {
            //return MCN420.mcc_home_start((ushort)CardNo, (ushort)axis);
            return 0;
        }
        #endregion

        #region 限位开关设置函数
        /// <summary>
        /// 设置软限位
        /// <para>注 意：</para>
        /// <para>正、负限位位置可为正数也可为负数，但正限位位置应大于负限位位置</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="enable">使能状态，0：禁止，1：允许 </param>
        /// <param name="source_sel">计数器选择，0：指令位置计数器，1：编码器</param>
        /// <param name="SL_action">限位停止方式，0：立即停止 1：减速停止</param>
        /// <param name="N_limit">负限位位置，单位：pulse </param>
        /// <param name="P_limit">正限位位置，单位：pulse</param>
        /// <returns></returns>
        public int DmcSetCardAxisSoftLimit(int CardNo, int axis, int enable, int source_sel, int SL_action, int N_limit, int P_limit)
        {
            MCN420.YK_set_soft_positive_limit_config((ushort)CardNo, (ushort)axis, (ushort)enable, P_limit);
            MCN420.YK_set_soft_negative_limit_config((ushort)CardNo, (ushort)axis, (ushort)enable, N_limit);
            return 0;
        }

        /// <summary>
        /// 获取软限位设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="enable">返回使能状态</param>
        /// <param name="source_sel">返回计数器选择</param>
        /// <param name="SL_action">返回限位停止方式</param>
        /// <param name="N_limit">返回负限位脉冲数</param>
        /// <param name="P_limit">返回正限位脉冲数</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisSoftLimit(int CardNo, int axis, ref UInt16 enable, ref UInt16 source_sel, ref UInt16 SL_action, ref int N_limit, ref int P_limit)
        {
            double n_limit = 0;
            double p_limit = 0;
            uint enable0 = 0;
            uint enable1 = 0;
            var res = MCN420.YK_get_soft_positive_limit_config((ushort)CardNo, (ushort)axis, ref enable0, ref p_limit);
            res = MCN420.YK_get_soft_negative_limit_config((ushort)CardNo, (ushort)axis, ref enable1, ref n_limit);
            N_limit = (int)n_limit;
            P_limit = (int)p_limit;
            enable = (ushort)(((enable0 == 0) || (enable1 == 0)) ? 0 : 1);
            return res;
        }

        /// <summary>
        ///  设置指定轴的当前指令位置计数器值
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="pos">位置值，单位：unit</param>
        /// <returns></returns>
        public int DmcSetCardAxisPositionUnit(int CardNo, int axis, double pos)
        {
            return MCN420.YK_set_command_position((ushort)CardNo, (ushort)axis, pos);
            //return 0;
        }

        /// <summary>
        ///  获取指定轴当前位置
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="pos">返回当前位置值，单位：unit</param>
        /// <returns></returns>
        public int DmcGetCardAxisPositionUnit(int CardNo, int axis, ref double pos)
        {
            pos = MCN420.YK_get_current_position((ushort)CardNo, (ushort)axis/*, ref pos*/);
            return 0;
        }

        #endregion

        #region 运动状态检测及控制函数

        /// <summary>
        /// 读取指定轴的当前单轴速度
        /// <para>注 意：</para>
        /// <para>当执行基于脉冲当量的插补运动时，使用该函数读取的为各轴的单轴速度</para>
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="current_speed">返回速度值，单位：unit/s</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisCurrentSpeedUnit(int CardNo, int axis, ref double current_speed)
        {
            current_speed = MCN420.YK_get_current_speed((ushort)CardNo, (ushort)axis) * 1000;
            return 0;
        }

        /// <summary>
        /// 检测指定轴的运动状态
        /// <para>注 意：</para>
        /// <para>此函数适用于单轴、PVT 运动</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <returns>0：指定轴正在运行，1：指定轴已停止</returns>
        public int DmcGetCardAxisCheckDone(int CardNo, int axis)
        {
            return MCN420.YK_check_done((ushort)CardNo, (ushort)axis);
        }

        /// <summary>
        /// 检测坐标系的运动状态, 0运动中 1空闲
        /// <para>注 意：</para>
        /// <para>此函数适用于插补运动</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">指定控制卡上的坐标系号（取值范围：0~5）</param>
        /// <returns>轴运动状态，0运动中 1空闲</returns>
        public int DmcGetCardCrdCheckDoneMulticoor(int CardNo, int crd)
        {
            return MCN420.YK_check_done((ushort)CardNo, (ushort)crd);//crd改为AxisID，如果当前轴停止代表插补完成
            //return MCN420.mcc_status_check_crd_sts((ushort)CardNo, (ushort)crd);
        }

        /*
        short mcc_softltc_get_number(WORD CardNo,WORD latch,WORD axis,int *number)
            功 能：读取锁存个数
            参 数：CardNo 控制卡卡号
            latch 锁存器号
            axis 锁存对应轴号
            number 锁存个数
            返回值：错误代码
        */

        public int DmcSoftltcGetNumber(int CardNo, int latch, int axis, ref ushort number)
        {
            return 0;
            ushort ltc_remain = 0;
            ushort ltc_sts = 0;
            //return (int)MCN420.mcc_ltc_local_latch_get_status((ushort)CardNo, (ushort)latch, ref number, ref ltc_remain, ref ltc_sts);
        }

        /// <summary>
        /// 读取指定轴有关运动信号的状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <returns>
        /// <para>* 1    EL+ 1：表示正硬限位信号 +EL 为 ON； 0：OFF</para>  
        /// <para>* 2    EL- 1：表示负硬限位信号–EL为 ON； 0：OFF</para> 
        /// <para>* 3    EMG 1：表示急停信号 EMG 为 ON； 0：OFF</para> 
        /// <para>* 4    ORG 1：表示原点信号 ORG 为 ON； 0：OFF</para> 
        /// <para>* 5    SL+ 1：表示正软限位信号+SL为ON； 0：OFF</para> 
        /// <para>* 6    SL- 1：表示负软限位信号-SL为ON； 0：OFF</para> 
        /// </returns>
        public int DmcGetCardAxisIOState(int CardNo, int axis)
        {
            //return (int)MCN420.mcc_status_get_axis_io((ushort)CardNo, (ushort)axis);
            throw new ArgumentNullException($"DmcGetCardAxisIOState函数未添加！");
        }

        /// <summary>
        /// 指定轴停止运动
        /// <para>注 意：</para>
        /// <para>此函数适用于单轴、PVT 运动</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="stop_mode">制动方式，0：减速停止，1：紧急停止</param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisStop(int CardNo, int axis, int stop_mode)
        {
            var res = MCN420.YK_stop_one_axis((ushort)CardNo, (ushort)axis, (ushort)stop_mode);
            MCN420.YK_remove_axis_alm((ushort)CardNo, (ushort)axis);
            return res;
        }

        /// <summary>
        /// 停止坐标系内所有轴的运动
        /// <para>注 意：</para>
        /// <para>此函数适用于插补运动</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">指定控制卡上的坐标系号（取值范围：0~5）</param>
        /// <param name="stop_mode">制动方式，0：减速停止，1：立即停止</param>
        /// <returns>错误代码</returns>
        public int DmcCardCrdStopMulticoor(int CardNo, int crd, int stop_mode)
        {
            return MCN420.YK_stop_all_axis((ushort)CardNo, (ushort)stop_mode);
        }

        /// <summary>
        /// 紧急停止（所有轴）
        /// <para>注 意：</para>
        /// <para>此函数适用于所有运动模式</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <returns>错误代码</returns>
        public int CardDmcEmgStop(int CardNo)
        {
            return MCN420.YK_stop_all_axis((ushort)CardNo, 1);
        }

        #endregion

        #region 单轴运动速度曲线设置函数

        /// <summary>
        /// 设置单轴运动速度曲线
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="Min_Vel">起始速度，单位：unit/s</param>
        /// <param name="Max_Vel">最大速度，单位：unit/s</param>
        /// <param name="Tacc">加速时间，单位：s</param>
        /// <param name="Tdec">减速时间，单位：s</param>
        /// <param name="Stop_Vel">停止速度，单位：unit/s</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisProfileUnit(int CardNo, int axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Stop_Vel)
        {
            //AxisVMoveParaDic[Tuple.Create(CardNo, axis)] = new Jog_Req_Param();
            //AxisHomeParaDic[Tuple.Create(CardNo, axis)] = new Home_Req();
            //AxisPMoveParaDic[Tuple.Create(CardNo, axis)] = new Single_Req_Param();
            if (AxisVMoveParaDic.ContainsKey(Tuple.Create(CardNo, axis)))
            {
                //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                var obj = AxisVMoveParaDic[Tuple.Create(CardNo, axis)];
                AxisVMoveParaDic[Tuple.Create(CardNo, axis)] = new Jog_Req_Param
                {
                    CmdNum = 0,
                    ParamRemain = 0,
                    Channel = 2,
                    axis = (uint)axis,
                    STFlag = 1,
                    ParaType = 1,
                    Dir = obj.Dir,
                    InterpoStartVel = Min_Vel / 1000,
                    InterpoEndVel = Min_Vel / 1000,
                    InterpoVel = Max_Vel / 1000,
                    InterpoAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000),
                    InterpoDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000),
                    InterpJerkAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000) / 10,
                    InterpJerkDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000) / 10,
                };
            }
            else
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{axis}");
            if (AxisPMoveParaDic.ContainsKey(Tuple.Create(CardNo, axis)))
            {
                //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                var obj = AxisPMoveParaDic[Tuple.Create(CardNo, axis)];
                AxisPMoveParaDic[Tuple.Create(CardNo, axis)] = new Single_Req_Param
                {
                    CmdNum = obj.CmdNum,
                    ParamRemain = obj.ParamRemain,
                    Channel = obj.Channel,
                    axis = (uint)axis,
                    STFlag = obj.STFlag,
                    ParaType = obj.ParaType,
                    //PosType = obj.PosType,
                    InterpoStartVel = Min_Vel / 1000,
                    InterpoEndVel = Min_Vel / 1000,
                    InterpoVel = Max_Vel / 1000,
                    InterpoAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000),
                    InterpoDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000),
                    InterpJerkAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000) / 10,
                    InterpJerkDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000) / 10,
                };
                return 0;
            }
            throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{axis}");
            //return MCN420.mcc_axis_set_prf_vel((ushort)CardNo, (ushort)axis, Min_Vel, Max_Vel, Tacc, Tdec, Stop_Vel);
        }

        /// <summary>
        /// 读取单轴运动速度曲线
        /// <para>注 意：</para>
        /// <para>该函数不适用于连续插补</para>
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="Min_Vel">返回起始速度设置，单位：unit/s</param>
        /// <param name="Max_Vel">返回最大速度设置，单位：unit/s</param>
        /// <param name="Tacc">返回加速时间设置，单位：s</param>
        /// <param name="Tdec">返回减速时间设置，单位：s</param>
        /// <param name="Stop_Vel">返回停止速度设置，单位：unit/s</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisProfileUnit(int CardNo, int axis, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double Stop_Vel)
        {
            if (AxisPMoveParaDic.ContainsKey(Tuple.Create(CardNo, axis)))
            {
                //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                var obj = AxisPMoveParaDic[Tuple.Create(CardNo, axis)];
                Min_Vel = obj.InterpoStartVel * 1000;
                Max_Vel = obj.InterpoVel * 1000;
                Stop_Vel = obj.InterpoEndVel * 1000;
                Tacc = ((Max_Vel) - (Min_Vel)) / obj.InterpoAcc / 1000;
                Tdec = ((Max_Vel) - (Min_Vel)) / obj.InterpoDec / 1000;
                return 0;
            }
            throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{axis}");
            //return MCN420.mcc_axis_get_prf_vel((ushort)CardNo, (ushort)axis, ref Min_Vel, ref Max_Vel, ref Tacc, ref Tdec, ref Stop_Vel);
        }

        /// <summary>
        /// 设置单轴速度曲线 S 段参数值
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="s_mode">保留参数，固定值为 0</param>
        /// <param name="s_para">S 段时间，单位：s；范围：0~1</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisSProfile(int CardNo, int axis, int s_mode, double s_para)
        {
            if (AxisPMoveParaDic.ContainsKey(Tuple.Create(CardNo, axis)))
            {
                //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                var obj = AxisPMoveParaDic[Tuple.Create(CardNo, axis)];
                AxisPMoveParaDic[Tuple.Create(CardNo, axis)] = new Single_Req_Param
                {
                    CmdNum = obj.CmdNum,
                    ParamRemain = obj.ParamRemain,
                    Channel = obj.Channel,
                    axis = (uint)axis,
                    STFlag = 1,
                    ParaType = 1,
                    //PosType = obj.PosType,
                    InterpoStartVel = obj.InterpoStartVel,
                    InterpoEndVel = obj.InterpoEndVel,
                    InterpoVel = obj.InterpoVel,
                    InterpoAcc = obj.InterpoAcc,
                    InterpoDec = obj.InterpoDec,
                    InterpJerkAcc = ((s_para) * 1000 >= (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoAcc)) / 2 ? obj.InterpoAcc / 2 : ((s_para) * 1000 / (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoAcc))) * obj.InterpoAcc),
                    InterpJerkDec = ((s_para) * 1000 >= (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoDec)) / 2 ? obj.InterpoDec / 2 : ((s_para) * 1000 / (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoDec))) * obj.InterpoDec),
                    //InterpJerkAcc = Math.Pow(s_para * 1000, 3),
                    //InterpJerkDec = Math.Pow(s_para * 1000, 3),
                };
                return 0;
            }
            throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{axis}");
        }

        /// <summary>
        /// 读取单轴速度曲线 S 段参数值
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="s_mode">保留参数</param>
        /// <param name="s_para">返回设置的 S 段时间</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisSProfile(int CardNo, int axis, int s_mode, ref double s_para)
        {
            if (AxisPMoveParaDic.ContainsKey(Tuple.Create(CardNo, axis)))
            {
                //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                s_para = (Math.Pow(AxisPMoveParaDic[Tuple.Create(CardNo, axis)].InterpJerkAcc, 1 / 3)) / 1000;
                return 0;
            }
            throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{axis}");
            //return MCN420.mcc_axis_get_s_prf_vel((ushort)CardNo, (ushort)axis, (ushort)s_mode, ref s_para);
        }

        #endregion

        #region 单轴运动函数
        /// <summary>
        /// 点位运动
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="dist">目标位置，单位：unit </param>
        /// <param name="posi_mode">运动模式，0：相对坐标模式，1：绝对坐标模式</param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisPMoveUnit(int CardNo, int axis, double dist, ushort posi_mode)
        {
            if (AxisPMoveParaDic.ContainsKey(Tuple.Create(CardNo, axis)))
            {
                //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                var obj = AxisPMoveParaDic[Tuple.Create(CardNo, axis)];
                var moveObj = AxisPMoveParaDic[Tuple.Create(CardNo, axis)] = new Single_Req_Param
                {
                    CmdNum = obj.CmdNum,
                    ParamRemain = obj.ParamRemain,
                    Channel = obj.Channel,
                    axis = (uint)axis,
                    STFlag = obj.STFlag,
                    ParaType = obj.ParaType,
                    PosType = posi_mode,
                    AxisPos = dist,
                    InterpoStartVel = obj.InterpoStartVel,
                    InterpoEndVel = obj.InterpoEndVel,
                    InterpoVel = obj.InterpoVel,
                    InterpoAcc = obj.InterpoAcc,
                    InterpoDec = obj.InterpoDec,
                    InterpJerkAcc = obj.InterpJerkAcc,
                    InterpJerkDec = obj.InterpJerkDec,
                };
                MCN420.YK_remove_axis_alm((ushort)CardNo, (ushort)axis);
                SleepWaitMove();
                return MCN420.YK_pmove((ushort)CardNo, ref moveObj);
            }
            throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{axis}");
            //return MCN420.mcc_axis_pmove((ushort)CardNo, (ushort)axis, dist, posi_mode);
        }

        /// <summary>
        /// 指定轴连续运动
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="dir">方向运动方向，0：负方向，1：正方向</param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisVMove(int CardNo, int axis, int dir)
        {
            /*
            short mcc_vmove(WORD CardNo, WORD axis, WORD dir)
            功 能：指定轴连续运动
            参 数：CardNo 控制卡卡号
            axis 指定轴号
             dir 运动方向，0：负方向，1：正方向
            返回值：错误代码
            */
            if (AxisVMoveParaDic.ContainsKey(Tuple.Create(CardNo, axis)))
            {
                //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                var obj = AxisVMoveParaDic[Tuple.Create(CardNo, axis)];
                obj.Dir = dir == 0 ? -1 : 1;
                MCN420.YK_remove_axis_alm((ushort)CardNo, (ushort)axis);
                return MCN420.YK_vmove((ushort)CardNo, ref obj);
            }
            else
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{axis}");
            //return MCN420.mcc_axis_vmove((ushort)CardNo, (ushort)axis, (ushort)dir);
        }

        /// <summary>
        /// 在线改变指定轴的当前运动速度
        /// <para>注 意：</para>
        /// <para>1）该函数适用于单轴运动中的变速</para>
        /// <para>2）设置的变速时间是从当前速度变速到新速度的时间。此时控制卡会重新计算起始速度加速到最高速度所需的时间以及最高速度减速到停止速度所需的时间，即加减速时间会被重新计算</para>
        /// <para>3）变速一旦成立，该轴的默认运行速度将会被改写为 New_Vel，加减速时间也会被控制卡新计算的值所覆盖，也即当调用 mcc_get_profile_unit 回读速度参数时会发生与 mcc_set_profile_unit 所设置的值不一致的现象</para>
        /// <para>4）在连续运动中 New_Vel 负值表示往负向变速，正值表示往正向变速。在点位运动中 New_Vel 只允许正值</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="New_Vel">新的运行速度，单位：unit/s</param>
        /// <param name="Taccdec">变速时间，单位：s </param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisChangeSpeed(int CardNo, int axis, double New_Vel, double Taccdec)
        {
            return MCN420.YK_change_speed((ushort)CardNo, (uint)axis, New_Vel);
        }

        /// <summary>
        /// 在线改变指定轴的当前运动位置
        /// <para> 注 意：</para>
        /// <para>1）该函数只适用于点位运动中的变位</para>
        ///  <para>2）参数 New_Pos 为绝对位置值，无论当前的运动模式为绝对坐标还是相对坐标模式 </para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="New_Pos">新目标位置，单位：unit</param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisResetTargetPositionUnit(int CardNo, int axis, double New_Pos, int pos_mode)
        {
            return MCN420.YK_change_position((ushort)CardNo, (uint)axis, (uint)pos_mode, New_Pos);
        }

        /// <summary>
        /// 强行变位(强行改变指定轴的当前目标位置（在线/非在线）)
        /// <para> 注 意：</para>
        /// <para>1）该函数适用于指定轴停止状态或点位运动中的变位</para>
        ///  <para>2）参数 New_Pos 为绝对位置值，无论当前的运动模式为绝对坐标还是相对坐标模式 </para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="New_Pos">新的点位</param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisUpdateTargetPosition(int CardNo, int axis, double New_Pos)
        {
            //return MCN420.mcc_axis_update_target_position((ushort)CardNo, (ushort)axis, New_Pos, 1);
            return 0;
        }
        #endregion

        #region 通用输入输出 IO 函数

        /// <summary>
        /// 读取指定控制卡的某个输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号，取值范围： 0~7，如果扩展 IO 模块，依次往后累加 </param>
        /// <returns>指定的输入端口电平：0：低电平，1：高电平</returns>
        public int DmcGetCardInPortNoBit(int CardNo, int bitno)
        {
            return MCN420.YK_read_inbit((ushort)CardNo, (ushort)(bitno));
            //var res = MCN420.mcc_io_read_inbit((ushort)CardNo, (ushort)bitno, ref on_off);
        }
        /// <summary>
        /// 设置指定控制卡的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输出端口号，取值范围： 0~7，如果扩展 IO 模块，依次往后累加</param>
        /// <param name="on_off">输出电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardBitNoInBit(int CardNo, int bitno, int on_off)
        {
            return MCN420.YK_write_outbit((ushort)CardNo, (ushort)(bitno), (uint)on_off);
            //return MCN420.mcc_io_write_outbit((ushort)CardNo, (ushort)bitno, (ushort)on_off);

        }

        /// <summary>
        /// 读取指定控制卡的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号，取值范围： 0~7，如果扩展 IO 模块，依次往后累加 </param>
        /// <returns>指定输出端口的电平，0：低电平，1：高电平</returns>
        public int DmcGetCardBitNoOutBit(int CardNo, int bitno)
        {
            return MCN420.YK_read_outbit((ushort)CardNo, (ushort)(bitno));
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
        public int DmcGetCardPortNoInPort(int CardNo, int portno)
        {
            //byte[] value = new byte[4];
            byte[] value = new byte[2];
            value[0] = (byte)(MCN420.YK_read_inbyte((ushort)CardNo, (ushort)((portno) * 2)) & 0xFF);
            value[1] = (byte)(MCN420.YK_read_inbyte((ushort)CardNo, (ushort)((portno) * 2 + 1)) & 0xFF);
            //value[2] = (byte)(MCN420.YK_read_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 4 + 8 + 2)) & 0xFF);
            //value[3] = (byte)(MCN420.YK_read_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 4 + 8 + 3)) & 0xFF);
            //result = MCN420.YK_read_inbyte((ushort)CardNo, (ushort)NoteID, (ushort)(PortNo * 2 + 1), ref value[1]);
            return (int)BytesToUInt16(value);
        }

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
        public uint DmcGetCardPortNoOutPort(int CardNo, int portno)
        {
            //byte[] value = new byte[4];
            byte[] value = new byte[2];
            value[0] = (byte)(MCN420.YK_read_outbyte((ushort)CardNo, (ushort)((portno) * 2)) & 0xFF);
            value[1] = (byte)(MCN420.YK_read_outbyte((ushort)CardNo, (ushort)((portno) * 2 + 1)) & 0xFF);
            //value[2] = (byte)(MCN420.YK_read_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 4 + 8 + 2)) & 0xFF);
            //value[3] = (byte)(MCN420.YK_read_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 4 + 8 + 3)) & 0xFF);
            //result = MCN420.YK_read_inbyte((ushort)CardNo, (ushort)NoteID, (ushort)(PortNo * 2 + 1), ref value[1]);
            return BytesToUInt16(value);
        }

        /// <summary>
        /// 设置指定控制卡的全部输出口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="portno">保留参数，固定值为 0；所有输出口按顺序排列，每 32bit 为一个 port口号，例如 portno = 0 表示 out0-out31，portno=1 表示 out32-out63，以此类推；</param>
        /// <param name="outport_val">
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
        /// </param>
        /// <returns>错误代码</returns>
        public int DmcSetCardPortNoOutPort(int CardNo, int portno, uint outport_val)
        {

            var Value = SplitUInt32Little_byte_two(outport_val);
            var result = MCN420.YK_write_outbyte((ushort)CardNo, (ushort)((portno) * 2), Value[1]);
            if (result != 0) goto end;
            result = MCN420.YK_write_outbyte((ushort)CardNo, (ushort)((portno) * 2 + 1), Value[0]);
            if (result != 0) goto end;
            end:
            return result;
        }

        /// <summary>
        /// IO 输出延时翻转
        /// <para>注 意：</para>
        /// <para>延时翻转时间参数设置为 0 时，此时延时翻转时间将为无限大</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输出端口号，取值范围：0~7，如果扩展 IO 模块，依次往后累加</param>
        /// <param name="reverse_time">延时翻转时间，单位：s</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardIOReverseOutbit(int CardNo, int bitno, int reverse_time)
        {
            //var res = new SetOutputDelayFlip_Req
            //{
            //    CmdNum = 0,
            //    CmdName = 0,
            //    Channel = 2,
            //    Mode = 0,
            //    iSelect = 1,
            //    Pos = 0,
            //    Times = 1,
            //    OutputSelect = (uint)bitno,
            //    msDelay = reverse_time,
            //};
            //return MCN420.YK_set_output_delay_flip((ushort)CardNo, ref res);
            return MCN420.YK_write_outbit_delay((ushort)CardNo, 0, (uint)bitno, 0, (uint)reverse_time);
        }

        /// <summary>
        /// 设置 IO 计数模式；
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号，取值范围：0~7，如果扩展 IO 模块，依次往后累加</param>
        /// <param name="mode">IO 计数模式，0：禁用，1：上升沿计数，2：下降沿计数</param>
        /// <param name="filter_time">滤波时间，单位：s，保留参数</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardIOCountMode(int CardNo, int bitno, int mode, double filter_time)
        {
            var res = new SetInputCounter_Req
            {
                CmdNum = 0,
                CmdName = 0,
                Channel = 2,
                Mode = 0,
                CounterNumber = 0,
                Enable = 1,

                iInputSelect = 1,
                dInputSelect = (uint)bitno,
                data = 0,
            };
            return MCN420.YK_set_counter_input_mode((ushort)CardNo, ref res);
        }

        /// <summary>
        /// 读取 IO 计数模式设置；
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号，取值范围：0~7，如果扩展 IO 模块，依次往后累加</param>
        /// <param name="mode">返回 IO 计数模式</param>
        /// <param name="filter_time">返回滤波时间，单位：s，保留参数</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardIOCountMode(int CardNo, int bitno, ref UInt16 mode, ref uint filter_time)
        {
            //return MCN420.mcc_io_get_count_value((ushort)CardNo, (ushort)bitno, /*ref mode,*/ ref filter_time);
            throw new AggregateException("未添加！");
        }

        /// <summary>
        /// 设置 IO 计数值
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号，取值范围：0~7，如果扩展 IO 模块，依次往后累加</param>
        /// <param name="CountValue">IO 计数值</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardIOCountValue(int CardNo, int bitno, int CountValue)
        {
            //return MCN420.mcc_io_set_count_value((ushort)CardNo, (ushort)bitno, (UInt32)CountValue);
            throw new AggregateException("未添加！");
        }

        /// <summary>
        /// 读取 IO 计数值；
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号，取值范围：0~7，如果扩展 IO 模块，依次往后累加</param>
        /// <param name="CountValue">返回 IO 计数值</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardIOCountValue(int CardNo, int bitno, ref UInt32 CountValue)
        {
            throw new AggregateException("未添加！");
        }
        #endregion

        #region 手轮功能函数
        /// <summary>
        /// 设置单轴手轮运动控制输入方式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// 
        /// <param name="inmode">手轮输入方式，0：脉冲+方向信号；1：A、B 相位正交信号</param>
        /// <param name="multi">手轮倍率，正数表示默认方向，负数表示与默认方向反向</param>
        /// <param name="vh">保留参数，固定值为 1</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisHandwheelInMode(int CardNo, int axis, ushort ChnNo, int inmode, ushort multi, ushort vh)
        {
            return MCN420.YK_set_handwheel_config((ushort)CardNo, (ushort)axis, (ushort)inmode, multi);
            //YK_set_handwheel_ratio
            //YK_set_handwheel_control
        }

        /// <summary>
        /// 读取单轴手轮运动控制输入方式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="inmode">返回手轮输入方式</param>
        /// <param name="multi">返回手轮倍率</param>
        /// <param name="vh">保留参数</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisHandwheelInMode(int CardNo, ushort axis, ref ushort ChnNo, ref UInt16 inmode, ref double multi, ref ushort vh)
        {

            //return MCN420.YK_get_handwheel_config((ushort)CardNo, ChnNo, ref vh, ref axis, ref inmode, ref multi);
            return 0;
        }

        /// <summary>
        /// 启动手轮运动
        /// <para>注 意：</para>
        /// <para>当启动手轮运动后，只有发送 mcc_stop 或 mcc_emg_stop 命令后才会退出手轮模式</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisHandwheelMove(int CardNo, int mode, int enable)
        {
            return MCN420.YK_set_handwheel_control((ushort)CardNo, (uint)enable, (ushort)mode);
        }

        /// <summary>
        /// 设置多轴手轮运动控制输入方式
        /// <para>注 意：</para>
        /// <para>通过该函数设置可以使一个手轮通道控制多个轴同时运动，运动倍率都以第一个轴的倍率。</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="inmode">手轮输入方式：0：脉冲+方向信号；1：A、B 相位正交信号</param>
        /// <param name="AxisNum">参与手轮运动的轴数</param>
        /// <param name="AxisNums">参与手轮运动的轴号数组</param>
        /// <param name="multi">手轮倍率数组:正数表示默认方向，负数表示与默认方向反向</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardHandwheelInModeExtern(int CardNo, int inmode, ushort ChnNo, int AxisNum, UInt16[] AxisNums, double multi, ushort enable)
        {
            //return MCN420.mcc_handwheel_multi_set_para((ushort)CardNo, ChnNo, enable, (ushort)AxisNum, AxisNums, (ushort)inmode, multi);
            return 0;
        }

        /// <summary>
        /// 读取多轴手轮运动控制输入方式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="inmode">返回手轮输入方式</param>
        /// <param name="AxisNum">返回参与手轮运动的轴数</param>
        /// <param name="AxisNums">返回参与手轮运动的轴号数组</param>
        /// <param name="multi">返回手轮倍率数组</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardHandwheelInModeExtern(int CardNo, ushort ChnNo, ref UInt16 inmode, ref UInt16 AxisNum, ref UInt16[] AxisNums, ref double multi, ref ushort enable)
        {
            return 0;
            //return MCN420.mcc_handwheel_multi_get_para((ushort)CardNo, ChnNo, ref enable, ref AxisNum, AxisNums, ref inmode, ref multi);
        }
        #endregion

        #region 编码器函数
        /// <summary>
        /// 设置指定轴的当前编码器计数值
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="pos">编码器计数值，单位：unit</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisEncoderUnit(int CardNo, int axis, double pos)
        {
            return MCN420.YK_set_encoder_position((ushort)CardNo, (ushort)axis, (int)pos);
        }

        /// <summary>
        /// 读取指定轴的当前编码器计数值
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="pos">返回当前编码器计数值，单位：unit</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisEncoderUnit(int CardNo, int axis, ref double pos)
        {
            var res = MCN420.YK_get_encoder_position((ushort)CardNo, (ushort)axis);
            pos = res;
            return 0;
        }

        /// <summary>
        /// 设置辅助编码器输入方式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="channel">辅助编码器通道，0，通道 0，1，通道 1</param>
        /// <param name="inmode">辅助编码器输入方式，0：脉冲+方向信号；1：A、B 相位正交信号0:非 A/B 相(脉冲/方向)； 1: A/B 相 1 倍频；2: A/B 相 2 倍频；3: A/B 相 4 倍频；</param>
        /// <param name="multi">辅助编码器计数模式，固定 1：4 倍频计数</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardExtraEncoderMode(int CardNo, int channel, int inmode, int multi)
        {
            //return MCN420.YK_set_encoder_counter_inmode((ushort)CardNo, (ushort)channel, (ushort)inmode);
            return 0;
        }

        /// <summary>
        /// 读取辅助编码器输入方式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="channel">辅助编码器通道</param>
        /// <param name="inmode">返回辅助编码器输入方式</param>
        /// <param name="multi">返回辅助编码器计数模式</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardExtraEncoderMode(int CardNo, int channel, ref ushort inmode)
        {
            //return MCN420.YK_get_encoder_counter_inmode((ushort)CardNo, (ushort)channel, ref inmode);
            return 0;
        }

        #endregion

        #region 高速位置锁存函数
        /// <summary>
        /// 配置锁存器（触发型记录位置功能）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴号：0~7</param>
        /// <param name="enable">使能锁存</param>
        /// <param name="ltc_mode">16种锁存模式</param>
        /// <param name="mLockInputBit"> 锁存源，0：轴指令位置，1：轴编码器位置，2：辅助编码器；</param>
        /// 当锁存源选择：轴指令位置时，取值 0-31；当锁存源选择：轴编码器位置时，取值 0-31；当锁存源选择：辅助编码器位置时，取值 0-3；</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardLtcMode(int CardNo, int axis, int enable, ushort mLockInputBit, int ltc_mode)
        {
            return MCN420.YK_set_latch_position((ushort)CardNo, (ushort)axis, (ushort)enable, mLockInputBit, (ushort)ltc_mode);
        }
        /// <summary>
        /// 配置锁存器(同步输出功能)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴号：0~7</param>
        /// <param name="enable">使能锁存</param>
        /// <param name="latch">锁存器号，0-127  输入IO</param>
        /// <param name="ltc_mode">7种锁存模式</param>
        /// <param name="out_num"> 同步输出IO数量</param>
        /// <param name="cmp_channel">多个通道对应的out IO</param>
        /// <param name="cmp_pos">相对偏移脉冲Array，多个通道，单位：us
        /// 当锁存源选择：轴指令位置时，取值 0-31；当锁存源选择：轴编码器位置时，取值 0-31；当锁存源选择：辅助编码器位置时，取值 0-3；</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardLtcMode(int CardNo, int axis, int Channel, int enable, int latch, int ltc_mode, ushort out_num, int[] cmp_channel, double[] cmp_pos)
        {
            return MCN420.YK_set_axis_syslock_output((ushort)CardNo, (uint)Channel, (ushort)axis, (ushort)enable, (ushort)latch, (ushort)ltc_mode, out_num, Array.ConvertAll(cmp_channel, s => (uint)s), cmp_pos);
        }

        /// <summary>
        /// 读取锁存器配置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="latch">锁存器号,0-3</param>
        /// <param name="ltc_mode">读取锁存模式，0：单次锁存，1：连续锁存</param>
        /// <param name="ltc_logic">读取锁存信号的触发方式，0：下降沿锁存，1：上升沿锁存，2：双边沿锁存</param>
        /// <param name="filter">读取滤波时间，单位：us</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardLtcMode(int CardNo, int latch, ref ushort ltc_mode, ref ushort ltc_source, ref ushort ltc_logic, ref ushort filter)
        {
            //return MCN420.mcc_ltc_local_latch_get_mode((ushort)CardNo, (ushort)latch, ref ltc_mode, ref ltc_source, ref ltc_logic, ref filter);
            return 0;
        }

        /// <summary>
        /// 配置锁存源
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="latch">锁存器号,0-3</param>
        /// <param name="axis">锁存器辅助编码器通道号，0、1</param>
        /// <param name="ltc_source">锁存源，0：指令位置，1：辅助编码器计数</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardLtcSource(int CardNo, int latch, int axis, int ltc_source)
        {
            //return MCN420.mcc_ltc_set_source((ushort)CardNo, (ushort)latch, (ushort)axis, (ushort)ltc_source);
            return 0;
        }

        /// <summary>
        /// 读取锁存源配置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="latch">锁存器号,0-3</param>
        /// <param name="axis">锁存器辅助编码器通道号</param>
        /// <param name="ltc_source">读取锁存源，0：指令位置，1：辅助编码器计数</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardLtcSource(int CardNo, int latch, int axis, ushort ltc_source)
        {
            return 0;
            //return MCN420.mcc_ltc_get_source((ushort)CardNo, (ushort)latch, (ushort)axis, ref ltc_source);
        }

        /// <summary>
        /// 读取锁存值
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">锁存器辅助编码器通道号</param>
        /// <param name="latch">锁存器号,0-3</param>
        /// <param name="value">锁存值</param>
        /// <returns>锁存值
        /// <para>注 意：</para>
        /// <para>1）该函数只可以读辅助编码器锁存值；</para>
        /// <para>2）当选择锁存方式为连续锁存时，该函数第一次执行的时候，读取的是锁存器的第一个锁存值，第二次执行的时候，读取的是锁存器的第二个锁存值，以此类推；</para>
        /// <para>3）单次锁存时，调用 mcc_get_latch_value 不会自动清除已锁存个数，须调用 mcc_reset_latch_flag；</para>
        /// </returns>
        public double DmcGetCardLtcValueUnit(int CardNo, int axis, int latch = 0, double[] value = null)
        {
            return MCN420.YK_get_latch_position((ushort)CardNo, (ushort)axis);
        }
        /// <summary>
        /// 读取锁存状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">锁存器辅助编码器通道号</param>
        /// <returns>锁存值
        /// <para>注 意：</para>
        /// <para>1）该函数只可以锁存是否触发；</para>
        /// </returns>
        public double DmcGetCardLtcStatus(int CardNo, int axis)
        {
            return MCN420.YK_get_latch_status((ushort)CardNo, (ushort)axis);
        }
        /// <summary>
        /// 读取锁存器锁存个数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="latch">锁存器号,0-3</param>
        /// <param name="axis">锁存器辅助编码器通道号</param>
        /// <param name="number">读取锁存器个数</param>
        /// <returns>已锁存个数，0 表示无锁存值</returns>
        public int DmcGetCardLtcNumber(int CardNo, int latch, int axis, ref int number)
        {
            //double[] value = new double[1024];
            //var res = MCN420.mcc_ltc_local_latch_get_pos((ushort)CardNo, (ushort)latch, (ushort)axis, value);
            //number = value.Length;
            //return res;
            throw new AggregateException("未添加！");
        }

        /// <summary>
        /// 复位锁存器
        /// <para> 注 意：</para>
        /// <para>当使用锁存功能前，必须先调用此函数复位锁存器的标志位</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="latch">锁存器号,0-3</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardLtcReset(int CardNo, int latch)
        {
            throw new AggregateException("未添加！");
            //return MCN420.mcc_ltc_local_latch_clear((ushort)CardNo, (ushort)latch);
        }

        #endregion

        #region 位置比较函数  

        /// <summary>
        /// 设置一维位置比较器 (单轴低速)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="channel">指定轴号 </param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能 </param>
        /// <param name="cmp_source">比较源，0：指令位置计数器，1：编码器计数器</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisCompareConfig(int CardNo, int channel, int enable, int cmp_source)
        {
            DmcClearCardAxisComparePoints(CardNo, channel);
            return MCN420.YK_cmp_1d_set_config((ushort)CardNo, (ushort)channel, (ushort)enable, (ushort)cmp_source);
            return 0;
            throw new ArgumentNullException("待添加");
        }

        /// <summary>
        /// 读取一维位置比较器设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="enable">返回比较功能状态</param>
        /// <param name="cmp_source">返回比较源</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisCompareConfig(int CardNo, int axis, ref UInt16 enable, ref UInt16 cmp_source)
        {
            uint value0 = 0;
            uint value1 = 0;
            var res = MCN420.YK_cmp_1d_get_config((ushort)CardNo, (ushort)axis, ref value0, ref value1);
            enable = (ushort)value0;
            cmp_source = (ushort)value1;
            return res;
            throw new ArgumentNullException("待添加");
        }

        /// <summary>
        /// 清除已添加的所有一维位置比较点
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号 </param>
        /// <returns>错误代码</returns>
        public int DmcClearCardAxisComparePoints(int CardNo, int axis)
        {
            return MCN420.YK_cmp_1d_clear_point((ushort)CardNo, (ushort)axis);
        }

        /// <summary>
        /// 添加一维位置比较点 (非改)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="channel">通道号：0~23 </param>
        /// <param name="pos">设置比较位置为 X pulse</param>
        /// <param name="dir">设置比较模式</param>
        /// <param name="action">设置触发功能（为 IO 电平取反）模式</param>
        /// <param name="actpara">设置输出 IO 端口 (0 触发功能)</param>
        /// <returns>错误代码</returns>
        public int DmcAddCardAxisComparePoints(int CardNo, int channel, int pos, int dir, int action, int actpara)
        {
            if (actpara>13|| actpara<1)
            {
                actpara = 9;
            }
            return MCN420.YK_cmp_1d_add_point((ushort)CardNo, (ushort)channel, pos, (ushort)dir, (uint)actpara, (ushort)action);
            throw new ArgumentNullException("待添加");
        }

        /// <summary>
        /// 添加一维位置比较点
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="pos">比较位置</param>
        /// <param name="dir">比较模式，0：小于等于，1：大于等于</param>
        /// <param name="bitno">比较输出口</param>
        /// <param name="cycle">周期个数</param>
        /// <param name="level">比较输出点电平（level 只有当 cycle 为 0 的时候起作用，cycle 为 0 的时候触发时输出 level 电平）</param>
        /// <returns>错误代码</returns>
        public int DmcAddCardAxisComparePointsCycle(int CardNo, int axis, int pos, int dir, int bitno, int cycle, int level)
        {
            //return MCN420.YK_cmp_1d_add_point((ushort)CardNo, (ushort)axis, pos, (ushort)dir, (ushort)bitno, (ushort)cycle, (ushort)level);
            //return MCN420.mcc_cmp_1d_add_point((ushort)CardNo, (ushort)axis, (double)pos, (ushort)dir, (ushort)bitno, (ushort)cycle, (ushort)level);
            throw new ArgumentNullException("待添加");

        }

        /// <summary>
        /// 读取当前一维比较点位置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号 </param>
        /// <param name="pos">返回当前比较点位置 </param>       
        /// <returns></returns>
        public int DmcGetCardAxisCompareCurrentPoint(int CardNo, int axis, ref int pos)
        {
            //return MCN420.mcc_compare_get_current_point((ushort)CardNo, (ushort)axis, ref pos);
            throw new ArgumentNullException("待添加");
        }
        /// <summary>
        /// 查询已经比较过的一维比较点个数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="pointNum">返回已经比较过的点数</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisComparPointsRunned(int CardNo, int axis, ref int pointNum)
        {
            throw new ArgumentNullException("待添加");
            //return MCN420.mcc_compare_get_points_runned((ushort)CardNo, (ushort)axis, ref pointNum);
        }

        /// <summary>
        /// 查询可以加入的一维比较点个数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="pointNum">返回可以加入的比较点数</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisComparPointsRemained(int CardNo, int axis, ref int pointNum)
        {
            throw new ArgumentNullException("待添加");
            //return MCN420.mcc_compare_get_points_remained((ushort)CardNo, (ushort)axis, ref pointNum);
        }
        #endregion

        #region 高速位置比较函数
        /// <summary>
        ///设置高速比较模式
        ///<para>注  意：</para>
        ///<para>0）当选择模式 0 时，不使能</para>
        ///<para>1）当选择模式 1 时，只有当前位置等于比较位置时，CMP 端口才输出有效电平</para>
        ///<para>2）当选择模式 2 时，只要当前位置大于比较位置时，CMP 端口就一直保持有效电平</para>
        ///<para>3）当选择模式 3 时，只要当前位置小于比较位置时，CMP 端口就一直保持有效电平</para>
        ///<para>4）当选择模式 4 时，队列中的等于</para>
        ///<para>4）当选择模式 5 时，线性等于</para>
        ///<para>4）当选择模式 6 时，队列中的等于+输出电平保持</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口） </param>
        /// <param name="cmp_mode">比较模式：0：禁止（默认值）| 1：等于 | 2：小于 | 3：大于 | 4：队列 | ;提供 127 个点比较空间，采用先添加先比较，比较完可追加比较点，也可一次性添加多个比较点 | 5：线性，提供起始比较点，位置增量，比较次数  </param>
        /// <returns></returns>
        public int DmcSetCardHcmpMode1(int CardNo, int hcmp, int cmp_mode)
        {
            //return MCN420.mcc_hcmp_set_mode((ushort)CardNo, (ushort)hcmp, (ushort)cmp_mode);
            //Hcmp = hcmp;
            //Cmp_Mode = cmp_mode;
            //HcmpDic[(ushort)hcmp] = cmp_mode;
            return MCN420.YK_hcmp_1d_set_mode((ushort)CardNo, (ushort)hcmp, (uint)cmp_mode);
        }

        /// <summary>
        /// 读取高速比较模式设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口） </param>
        /// <param name="cmp_mode">返回比较模式设置</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardHcmpMode1(int CardNo, int hcmp, ref UInt16 cmp_mode)
        {
            //return MCN420.YK_hcmp_1d_get_mode((ushort)CardNo, (ushort)hcmp, ref cmp_mode);
            return 0;
        }

        /// <summary>
        /// 配置高速比较器
        /// <para>注  意：1）该函数的 time 参数（脉冲宽度）只对队列和线性比较模式起作用  </para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口）</param>
        /// <param name="axis">辅助编码器通道号 </param>
        /// <param name="cmp_source">比较位置源，固定值 1：辅助编码器</param>
        /// <param name="cmp_logic">有效电平：0：低电平，1：</param>
        /// <param name="time">脉冲宽度，单位：us，取值范围：1us~20s </param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisHcmpConfig(int CardNo, int hcmp, int axis, int cmp_source, int cmp_logic, int time)
        {
            //ushort[] CmpList = new ushort[1] { (ushort)hcmp };
            DmcClearCardHcmpPoints(CardNo, hcmp);
            return MCN420.YK_hcmp_1d_set_config((ushort)CardNo, (ushort)hcmp, (ushort)axis, (ushort)cmp_source, (ushort)cmp_logic, (uint)time);
        }
        /// <summary>
        /// 读取高速比较器配置 (单轴高速)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口）</param>
        /// <param name="axis">辅助编码器通道号 </param>
        /// <param name="cmp_source">比较位置源，固定值 1：辅助编码器</param>
        /// <param name="cmp_logic">有效电平：0：低电平，1：</param>
        /// <param name="time">脉冲宽度，单位：us，取值范围：1us~20s </param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisHcmpConfig(int CardNo, int hcmp, ref ushort axis, ref ushort cmp_source, ref ushort cmp_logic, ref int time)
        {
            //return MCN420.YK_hcmp_1d_get_config((ushort)CardNo, (ushort)hcmp, ref axis, ref cmp_source, ref cmp_logic, ref time);
            return 0;
        }

        /// <summary>
        /// 添加/更新高速比较位置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口） </param>
        /// <param name="pos">1：队列模式下：添加比较位置，单位：pulse | 2：线性模式下：更新起始比较位置，单位：pulse | 3：其他模式下：更新比较位置，单位：pulse</param>
        /// <returns>错误代码</returns>
        public int DmcAddCardHcmpPoint(int CardNo, int hcmp, int pos)
        {
            return MCN420.YK_hcmp_1d_add_point((ushort)CardNo, (ushort)hcmp, pos);
        }
        /// <summary>
        /// 添加/更新高速比较位置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口） </param>
        /// <param name="pos">1：队列模式下：添加比较位置，单位：pulse | 2：线性模式下：更新起始比较位置，单位：pulse | 3：其他模式下：更新比较位置，单位：pulse</param>
        /// <returns>错误代码</returns>
        public int DmcAddCardHcmpPointEx(int CardNo, int hcmp, int pos, int output_logic)
        {
            return MCN420.YK_hcmp_1d_add_point_ex((ushort)CardNo, (ushort)hcmp, pos, (uint)output_logic);
        }

        /// <summary>
        /// 设置高速比较线性模式参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口）</param>
        /// <param name="Increment">位置增量值，单位：pulse（正值表示位置递增，负值表示位置递减）</param>
        /// <param name="Count">比较次数，取值范围：1~65535</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardHcmpLiner(int CardNo, int hcmp, int Increment, int Count)
        {
            return MCN420.YK_hcmp_1d_set_liner((ushort)CardNo, (ushort)hcmp, Increment, (uint)Count);
        }

        /// <summary>
        /// 读取高速比较线性模式参数设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口）</param>
        /// <param name="Increment">返回位置增量值设置</param>
        /// <param name="Count">返回比较次数设置</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardHcmpLiner(int CardNo, int hcmp, ref double Increment, ref int Count)
        {
            int inc = 0;
            uint count = 0;
            var res = MCN420.YK_hcmp_1d_get_liner((ushort)CardNo, (ushort)hcmp, ref inc, ref count);
            Increment = inc; Count = (int)count;
            return res;
        }

        /// <summary>
        /// 清除已添加的所有高速位置比较点
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口）</param>
        /// <returns>错误代码</returns>
        public int DmcClearCardHcmpPoints(int CardNo, int hcmp)
        {
            return MCN420.YK_hcmp_1d_clear_point((ushort)CardNo, (ushort)hcmp);
        }

        /// <summary>
        /// 读取高速比较参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口）</param>
        /// <param name="remained_points">返回可添加比较点数</param>
        /// <param name="current_point">返回当前比较点位置，单位：pulse</param>
        /// <param name="runned_points">返回已比较点数</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardHcmpCurrentState(int CardNo, int hcmp, ref int remained_points, ref int current_point, ref int runned_points)
        {
            //return MCN420.mcc_cmp_1d_get_current_status((ushort)CardNo, (ushort)hcmp, ref remained_points, ref current_point, ref runned_points);
            return 0;
        }
        /// <summary>
        /// 清除一维位置比较器关闭
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <returns>错误代码</returns>
        public int DmcClearCardHcmpPoints(int CardNo)
        {
            return 0;
            //return MCN420.YK_hcmp_1d_clear_point((ushort)CardNo, 6, new ushort[6] { 0, 1, 2, 3, 4, 5 });
        }

        #endregion

        #region 异常信号接口函数
        /// <summary>
        /// 设置EMG信号
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="enable">使能状态，0：禁止，1：允许</param>
        /// <param name="io_port">io索引</param>
        /// <param name="emg_logic">有效电平：0：低电平，1：高电</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisEmgMode(int CardNo, int enable, int io_port, int emg_logic)
        {
            return MCN420.YK_set_emg_stop_config((ushort)CardNo, (ushort)enable, (ushort)io_port);
        }

        /// <summary>
        /// 获取EMG信号
        /// </summary>
        /// <param name="CardNo">返回控制卡卡号</param>
        /// <param name="axis">返回指定轴号</param>
        /// <param name="enable">返回使能状态，0：禁止，1：允许</param>
        /// <param name="emg_logic">返回有效电平：0：低电平，1：高电</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisEmgMode(int CardNo, int axis, ref UInt16 enable, ref UInt16 emg_logic)
        {
            uint Enable = 0;
            uint IO_Index = 10;
            var res = MCN420.YK_get_emg_stop_config((ushort)CardNo, ref Enable, ref IO_Index);
            enable = (ushort)Enable;
            emg_logic = (ushort)IO_Index;
            return res;
            return 0;
        }

        /// <summary>
        /// 外部减速停止信号及减速停止时间设置，秒为单位（适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="enable">使能状态，0：禁止，1：允许</param>
        /// <param name="logic">有效电平：0：低电平，1：高电</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisIODstpMode(int CardNo, int axis, int enable, int logic)
        {
            return MCN420.YK_set_stop_motion_mode((ushort)CardNo, (ushort)axis, (ushort)enable);
            return 0;
        }

        /// <summary>
        /// 外部减速停止信号及减速停止时间获取，秒为单位（适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">返回控制卡卡号</param>
        /// <param name="axis">返回指定轴号</param>
        /// <param name="enable">返回使能状态，0：禁止，1：允许</param>
        /// <param name="logic">返回有效电平：0：低电平，1：高电</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisIODstpMode(int CardNo, int axis, ref UInt16 enable, ref UInt16 logic)
        {
            //return MCN420.mcc_get_io_dstp_mode((ushort)CardNo, (ushort)axis, ref enable, ref logic);
            return 0;
        }

        /// <summary>
        /// 设置减速停止时间
        /// <para>注 意：</para>
        /// <para>1. 当发生异常停止时，如：限位信号（软硬件）被触发、减速停止信号(DSTP)被触发等进行减速停止时，减速停止时间都为 mcc_set_dec_stop_time 函数里设置的减速时间；</para>
        /// <para>2. T 型速度规划，电机实际停止时间等于设置的异常减速停止时间；S 型速度规划，电机实际停止时间等于设置的异常减速停止时间与 S 段时间之和；</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="stop_time"> 减速时间，单位：s</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisDecStopTime(int CardNo, int axis, double stop_time)
        {
            //return MCN420.YK_set_stop_motion_mode((ushort)CardNo, (ushort)axis, stop_time);
            return 0;
        }

        /// <summary>
        /// 读取减速停止时间设置；
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="stop_time">返回设置的减速时间，单位：s</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisDecStopTime(int CardNo, int axis, ref double stop_time)
        {
            //return MCN420.mcc_get_dec_stop_time((ushort)CardNo, (ushort)axis, ref stop_time);
            return 0;
        }
        #endregion

        #region 检测轴到位状态函数
        /// <summary>
        /// 设置位置误差带
        /// <para>编码器系数的说明：</para>
        /// <para>当使用 mcc_check_success_encoder 函数检测编码器是否到位时，其用于判断的编码器位置为：编码器计数值乘以编码器系数的值。</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="factor">编码器系数</param>
        /// <param name="error">位置误差带，单位：pulse</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisFactorError(int CardNo, int axis, double factor, int error)
        {
            //return MCN420.mcc_set_factor_error((ushort)CardNo, (ushort)axis, factor, error);
            return 0;
        }

        /// <summary>
        /// 读取位置误差带设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="factor">返回编码器系数设置</param>
        /// <param name="error">返回位置误差带设置</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisFactorError(int CardNo, int axis, ref double factor, ref int error)
        {
            return 0;
            //return MCN420.mcc_get_factor_error((ushort)CardNo, (ushort)axis, ref factor, ref error);
        }

        /// <summary>
        /// 检测指令到位  
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <returns></returns>
        public int DmcGetCardAxisCheckSuccessPulse(int CardNo, int axis)
        {
            return 0;
            //return MCN420.mcc_check_success_pulse((ushort)CardNo, (ushort)axis);
        }

        /// <summary>
        /// 检测编码器到位
        /// <para>注 意：</para>
        /// <para>1）该函数只适用于单轴运动 </para>
        /// <para>2）检测函数请在 mcc_check_done 检测到轴停止后调用，函数调用后会等待轴到位后，如果调用函数 100ms 内未到位，函数超时返回认为不到位</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <returns>0：表示编码器位置在设定的目标位置的误差带之外；1：表示编码器位置在设定的目标位置的误差带之内</returns>
        public int DmcGetCardAxisCheckSuccessEncoder(int CardNo, int axis)
        {
            return 0;
            //return MCN420.mcc_check_success_encoder((ushort)CardNo, (ushort)axis);
        }
        #endregion

        #region 密码管理函数
        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="new_sn">新密码，密码长度不大于 255 个字符</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardWriteSN(int CardNo, string new_sn)
        {
            //return MCN420.mcc_write_sn((ushort)CardNo, new_sn);
            return 0;
        }

        /// <summary>
        /// 密码校验
        /// <para>注 意：</para>
        /// <para>1）用户可以在系统软件开启时加入密码校验动作，以此对系统软件进行加密</para>
        /// <para>2）密码校验失败 3 次后，无法进行校验；若需再次校验，需将电脑关机，断电重启</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="check_sn">旧密码</param>
        /// <returns>校验状态，0：失败，1：成功</returns>
        public int DmcGetCardCheckSN(int CardNo, string check_sn)
        {
            return 0;
            //return MCN420.mcc_check_sn((ushort)CardNo, check_sn);
        }
        #endregion

        #region 打印输出函数

        /// <summary>
        /// 函数调用打印输出设置
        /// </summary>
        /// <param name="mode">打印输出模式，0：只打印报错函数，1：全部打印，2：全部不打印</param>
        /// <param name="FileName">文件保存路径：参数文件名+后缀：相对路径；完整描述参数文件的路径+文件名后缀：绝对路径</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardDebugMode(int mode, string FileName)
        {
            //return MCN420.mcc_set_debug_mode((ushort)mode, FileName);
            return 0;
        }

        /// <summary>
        /// 读取函数调用打印输出设置
        /// </summary>
        /// <param name="mode">返回打印输出使能状态</param>
        /// <param name="FileName">返回文件保存路径</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardDebugMode(ref UInt16 mode, IntPtr FileName)
        {
            //return MCN420.mcc_get_debug_mode(ref mode, FileName);
            return 0;
        }
        #endregion
        #region 扩展IO √

        /// <summary>
        /// DMC_3000_设置 扩展IO 通讯状态
        /// <para>注 意：</para>
        /// <para>1）当关闭运动控制卡时，CAN 通讯不会被自动断开；当再次初始化运动控制卡时， CAN-IO 通讯依然保持之前的状态；</para>
        /// <para>2）当连接 CAN 通讯时，必须使用 nmc_get_can_state 函数读取 CAN-IO 的通讯状态，确认 CAN 通讯已正常连接。当连接出现异常时，可再次调用 nmc_set_can_state函数进行连接；</para>
        /// <para>3）设置波特率时需保证控制卡波特率与模块波特率相对应；</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeNum">CAN 节点数，取值范围：1~8</param>
        /// <param name="state">设置通讯状态，0：断开，1：连接</param>
        /// <param name="baud">设置控制卡波特率：波特率参数：0,1,2,3,4,5；对特率：1000Kbps，800Kbps,500Kbps,250Kbps,125Kbps,100Kbps</param>
        /// <returns>返回值：错误代码</returns>
        public int SetExpandIOConnectState(int CardNo, int NodeNum, int state, int baud)
        {
            return MCN420.YK_can_io_relinking((ushort)CardNo);
        }

        /// <summary>
        /// DMC_3000_读取 扩展IO 通讯状态
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="NodeNum"></param>
        /// <param name="state"></param>
        /// <param name="baud"></param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandIOConnectState(int CardNo, ref ushort NodeNum, ref ushort state)
        {
            uint num = 0;
            uint Statu = 0;
            var res = MCN420.YK_can_io_link_state((ushort)CardNo, ref Statu, ref num);
            NodeNum = (ushort)num;
            state = (ushort)Statu;
            return res;
        }

        //*********************************************************************************************************************





        /// <summary>
        /// DMC_3000_设置指定 CAN-IO 扩展模块的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="IoBit">输出口号</param>
        /// <param name="IoValue">输出电平，0：低电平，1：高电平</param>
        /// <returns>返回值：错误代码</returns>
        public int SetExpandIOOutBit(int CardNo, int NodeID, int IoBit, int IoValue)
        {
            return MCN420.YK_write_outbit((ushort)CardNo, (ushort)(64 + (NodeID - 1) * 16 + IoBit), (uint)IoValue);
            //return LTDMC.nmc_write_outbit((ushort)CardNo, (ushort)NodeID, (ushort)IoBit, (ushort)IoValue);
        }

        /// <summary>
        /// DMC_3000_读取指定 CAN-IO 扩展模块的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="IoBit">输出口号</param>
        /// <param name="IoValue">输出电平，0：低电平，1：高电平</param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandIOOutBit(int CardNo, int NodeID, int IoBit, ref ushort IoValue)
        {
            IoValue = (ushort)MCN420.YK_read_outbit((ushort)CardNo, (ushort)((64 + (NodeID - 1) * 16 + IoBit)));
            //return LTDMC.nmc_read_outbit((ushort)CardNo, (ushort)NodeID, (ushort)IoBit, ref IoValue);
            return 0;
        }

        /// <summary>
        /// DMC_3000_设置指定 CAN-IO 扩展模块的输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">端口号，0 表示 0~31 号口，1 表示 32~63 号口</param>
        /// <param name="IoValue">输出值，bit0~bit32 的值分别代表第 0~32 号输出口的电平</param>
        /// <returns>返回值：错误代码</returns>
        public int SetExpandIOPortNoOutBit(int CardNo, int NodeID, int PortNo, int IoValue)
        {
            //return MCN420.ecc_write_outport_extern((ushort)CardNo, (ushort)Channel, (ushort)NoteID, portno, IoValue);
            var Value = SplitUInt32Little_byte_two((uint)IoValue);
            //var result = MCN420.ecc_io_write_outslot((ushort)CardNo, (ushort)NoteID, (ushort)(portno * 2), Value[1]);
            //result = MCN420.ecc_io_write_outslot((ushort)CardNo, (ushort)NoteID, (ushort)(portno * 2 + 1), Value[0]);
            //var result = MCN420.YK_write_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 4 + 2), Value[3]);
            //if (result != 0) goto end;
            //result = MCN420.YK_write_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 4 + 3), Value[2]);
            //if (result != 0) goto end;
            //result = MCN420.YK_write_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 4 + 4), Value[1]);
            //if (result != 0) goto end;
            //result = MCN420.YK_write_outbyte((ushort)CardNo, (ushort)((NoteID - 1) * 4 + 5), Value[0]);
            var result = MCN420.YK_write_outbyte((ushort)CardNo, (ushort)((NodeID - 1) * 2 + 8), Value[1]);
            if (result != 0) goto end;
            result = MCN420.YK_write_outbyte((ushort)CardNo, (ushort)((NodeID - 1) * 2 + 8 + 1), Value[0]);
            if (result != 0) goto end;
            end:
            return result;
            //return LTDMC.nmc_write_outport((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, (ushort)IoValue);
        }

        /// <summary>
        /// DMC_3000_读取指定 CAN-IO 扩展模块的输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">端口号，0 表示 0~31 号口，1 表示 32~63 号口</param>
        /// <param name="IoValue">输出值，bit0~bit31 的值分别代表第 0~31 号输出口的电平</param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandIOPortNoOutBit(int CardNo, int NodeID, int PortNo, ref uint IoValue)
        {
            return LTDMC.nmc_read_outport((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, ref IoValue);
        }


        /// <summary>
        /// DMC_3000_设置指定 CAN-ADDA 扩展模块的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">输出端口号</param>
        /// <param name="IoValue">输出电平，单位：mV/mA</param>
        /// <returns>返回值：错误代码</returns>
        public int SetExpandADDAIOPortNoOutBit(int CardNo, int NodeID, int PortNo, double IoValue)
        {
            return LTDMC.nmc_set_da_output((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, IoValue);
        }

        /// <summary>
        /// DMC_3000_读取指定 CAN-ADDA 扩展模块的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">输出端口号</param>
        /// <param name="IoValue">输出电平，单位：mV/mA</param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandADDAIOPortNoOutBit(int CardNo, int NodeID, int PortNo, ref double IoValue)
        {
            return LTDMC.nmc_get_da_output((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, ref IoValue);
        }

        /// <summary>
        ///  DMC_3000_设置指定 CAN-ADDA 扩展模块的某个输出端口的模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">输入端口号</param>
        /// <param name="mode">输入模式，0：电压模式，1：电流模式</param>
        /// <param name="buffer_nums">保留参数</param>
        /// <returns>返回值：错误代码</returns>
        public int SetExpandADDAIOOutPortNoMode(int CardNo, int NodeID, int PortNo, int mode, uint buffer_nums)
        {
            return LTDMC.nmc_set_da_mode((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, (ushort)mode, buffer_nums);
        }



        /// <summary>
        ///  DMC_3000_读取指定 CAN-ADDA 扩展模块的某个输出端口的模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">输入端口号</param>
        /// <param name="mode">输入模式，0：电压模式，1：电流模式</param>
        /// <param name="buffer_nums">保留参数</param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandADDAIOOutPortNoMode(int CardNo, int NodeID, int PortNo, ref ushort mode, uint buffer_nums)
        {
            return LTDMC.nmc_get_da_mode((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, ref mode, buffer_nums);
        }



        //*********************************************************************************************************************

        /// <summary>
        /// DMC_3000_读取指定 CAN-IO 扩展模块的某个输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="IoBit">输出口号</param>
        /// <param name="IoValue">输出电平，0：低电平，1：高电平</param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandIOInBit(int CardNo, int NodeID, int IoBit, ref ushort IoValue)
        {
            return LTDMC.nmc_read_inbit((ushort)CardNo, (ushort)NodeID, (ushort)IoBit, ref IoValue);
        }



        /// <summary>
        /// DMC_3000_读取指定 CAN-IO 扩展模块的输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">端口号，0 表示 0~31 号口，1 表示 32~63 号口</param>
        /// <param name="IoValue">输出值，bit0~bit31 的值分别代表第 0~31 号输出口的电平</param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandIOPortNoInBit(int CardNo, int NodeID, int PortNo, ref uint IoValue)
        {
            return LTDMC.nmc_read_inport((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, ref IoValue);
        }

        /// <summary>
        /// DMC_3000_读取指定 CAN-ADDA 扩展模块的某个输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">输出端口号</param>
        /// <param name="IoValue">输出电平，单位：mV/mA</param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandADDAIOPortNoInBit(int CardNo, int NodeID, int PortNo, ref double IoValue)
        {
            return LTDMC.nmc_get_ad_input((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, ref IoValue);
        }

        /// <summary>
        ///  DMC_3000_设置指定 CAN-ADDA 扩展模块的某个输入端口的模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">输入端口号</param>
        /// <param name="mode">输入模式，0：电压模式，1：电流模式</param>
        /// <param name="buffer_nums">保留参数</param>
        /// <returns>返回值：错误代码</returns>
        public int SetExpandADDAIOInPortNoMode(int CardNo, int NodeID, int PortNo, int mode, uint buffer_nums = 999)
        {
            return LTDMC.nmc_set_ad_mode((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, (ushort)mode, buffer_nums);

        }

        /// <summary>
        ///  DMC_3000_读取指定 CAN-ADDA 扩展模块的某个输入端口的模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">输入端口号</param>
        /// <param name="mode">输入模式，0：电压模式，1：电流模式</param>
        /// <param name="buffer_nums">保留参数</param>
        /// <returns>返回值：错误代码</returns>
        public int GetExpandADDAIOInPortNoMode(int CardNo, int NodeID, int PortNo, ref ushort mode, uint buffer_nums = 999)
        {
            return LTDMC.nmc_get_ad_mode((ushort)CardNo, (ushort)NodeID, (ushort)PortNo, ref mode, buffer_nums);

        }



        /// <summary>
        /// DMC_3000_保存模式设置到模块 FLASH
        /// <para>注 意：</para>
        /// <para>1）保存后模块会断开连接，需要重新连接才能进行正常控制。</para>
        /// <para>2）设置的模式会断电保存，上电后电压或电流模式为最后一次断电前设置的模式。</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="PortNum">保留参数，设为 0</param>
        /// <param name="NodeNum">节点号，1-8</param>
        /// <returns>返回值：错误代码</returns>
        public int SaveExpandIoToFlash(int CardNo, int PortNum, int NodeNum)
        {
            return LTDMC.nmc_write_to_flash((ushort)CardNo, (ushort)PortNum, (ushort)NodeNum);
        }

        #endregion
        #region 状态检测

        /// <summary>
        /// 获取轴运动模式：
        /// 1为pp模式，6为回零模式，8为csp模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <returns>轴运动模式</returns>
        public int DmcGetCardAxisRunMode(int CardNo, int axis, ref UInt16 run_mode)
        {
            //int mode = 0;
            //var result = MCN420.ecc_set_axis_contrlmode((ushort)CardNo, (ushort)axis, /*ref*/ mode);
            //run_mode = (ushort)mode;
            //return result;
            throw new ArgumentNullException("待添加");
            return 0;
        }
        /// <summary>
        /// 读取指定轴的停止原因
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="StopReason">停止原因：
        ///<para> 0：正常停止</para>
        ///<para>1：保留</para>
        ///<para>2：保留</para>
        ///<para>3：LTC 外部触发立即停止，IMD_STOP_AT_LTC</para>
        ///<para>4：EMG 立即停止，IMD_STOP_AT_EMG</para>
        ///<para>5：正硬限位立即停止，IMD_STOP_AT_ELP</para>
        ///<para>6：负硬限位立即停止，IMD_STOP_AT_ELN</para>
        ///<para>7：正硬限位减速停止，DEC_STOP_AT_ELP</para>
        ///<para>8：负硬限位减速停止，DEC_STOP_AT_ELN</para>
        ///<para>9：正软限位立即停止，IMD_STOP_AT_SOFT_ELP</para>
        ///<para>10：负软限位立即停止，IMD_STOP_AT_SOFT_ELN</para>
        ///<para>11：正软限位减速停止，DEC_STOP_AT_SOFT_ELP</para>
        ///<para>12：负软限位减速停止，DEC_STOP_AT_SOFT_ELN</para>
        ///<para>13：命令立即停止，IMD_STOP_AT_CMD</para>
        ///<para>14：命令减速停止，DEC_STOP_AT_CMD</para>
        ///<para>15：其它原因立即停止，IMD_STOP_AT_OTHER</para>
        ///<para>16：其它原因减速停止，DEC_STOP_AT_OTHER</para>
        ///<para>17：未知原因立即停止，IMD_STOP_AT_UNKOWN</para>
        ///<para>18：未知原因减速停止，DEC_STOP_AT_UNKOWN</para>
        ///<para>19：保留，DEC_STOP_AT_DEC</para>
        ///</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisStopReason(int CardNo, int axis, ref int StopReason)
        {
            var alarmValue = MCN420.YK_get_axis_motion_status_alm((ushort)CardNo, (ushort)axis);
            StopReason = Enumerable.Range(0, 32).Where(s => (alarmValue & (1 << s)) != 0).Select(s => s + 1).FirstOrDefault();
            return 0;
        }
        /// <summary>
        /// 清除指定轴的停止原因
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <returns>错误代码</returns>
        public int DmcClearCardAxisStopReason(int CardNo, int axis)
        {
            return MCN420.YK_remove_axis_alm((ushort)CardNo, (ushort)axis);
        }
        #endregion

        #region 插补速度设置函数

        /// <summary>
        /// 设置插补运动速度曲线
        /// <para>注 意：</para>
        /// <para>1）DMC-E3032 卡支持 6 个坐标系（参数 Crd）。六个坐标系的速度可独立设置，执行插补时六个坐标系可独立进行插补运动（即可同时进行六组插补运动）</para>
        /// </summary>
        /// <param name="CardNo">卡号 </param>
        /// <param name="Crd">坐标系号，取值范围：0~3</param>
        /// <param name="Min_Vel">最小速度，单位：unit/s</param>
        /// <param name="Max_Vel">最大速度，单位：unit/s</param>
        /// <param name="Tacc">加速时间，单位：s </param>
        /// <param name="Tdec">减速时间，单位：s </param>
        /// <param name="Stop_Vel">停止速度，单位：unit/s</param>
        /// <param name="type">插补模式：
        ///                             1：直线插补运动
        ///                             2：2维圆弧插补运动 
        ///                             3：3维圆弧插补运动
        ///                             4：2维渐变螺旋插补运动
        ///                             5：3维渐变螺旋插补运动
        ///                             6：3维圆锥插补运动
        ///                             7：三点圆弧插补
        /// </param>
        /// <returns>错误代码</returns>
        public int DmcSetCardVectorProfileUnit(int CardNo, int Crd, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Stop_Vel, int type)
        {//后续补充完成插补模式
            if (type == 1)
            {
                if (AxisLineParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
                {
                    //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                    var obj = AxisLineParaDic[Tuple.Create(CardNo, Crd)];
                    var moveObj = AxisLineParaDic[Tuple.Create(CardNo, Crd)] = new Line_Req_Param
                    {
                        CmdNum = obj.CmdNum,
                        ParamRemain = obj.ParamRemain,
                        Channel = obj.Channel,

                        STFlag = obj.STFlag,
                        ParaType = obj.ParaType,
                        PosType = obj.PosType,
                        axis = obj.axis,
                        AxisPos = obj.AxisPos,
                        nAxisNum = obj.nAxisNum,

                        InterpoStartVel = Min_Vel / 1000,
                        InterpoEndVel = Stop_Vel / 1000,
                        InterpoVel = Max_Vel / 1000,
                        InterpoAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000),
                        InterpoDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000),
                        InterpJerkAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000) / 10,
                        InterpJerkDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000) / 10,
                    };
                    return 0;
                }
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
            }
            else if (type == 2)
            {
                if (AxisARC2DParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
                {
                    //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                    var obj = AxisARC2DParaDic[Tuple.Create(CardNo, Crd)];
                    var moveObj = AxisARC2DParaDic[Tuple.Create(CardNo, Crd)] = new ARC_2D_Req_Param
                    {
                        CmdNum = obj.CmdNum,
                        ParamRemain = obj.ParamRemain,
                        Channel = obj.Channel,

                        STFlag = obj.STFlag,
                        ParaType = obj.ParaType,
                        PosType = obj.PosType,
                        axis = obj.axis,
                        AxisPos = obj.AxisPos,
                        Radius = obj.Radius,

                        InterpoStartVel = Min_Vel / 1000,
                        InterpoEndVel = Stop_Vel / 1000,
                        InterpoVel = Max_Vel / 1000,
                        InterpoAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000),
                        InterpoDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000),
                        InterpJerkAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000) / 10,
                        InterpJerkDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000) / 10,
                    };
                    return 0;
                }
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
            }
            else if (type == 3)
            {
                if (AxisARC3DParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
                {
                    //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                    var obj = AxisARC3DParaDic[Tuple.Create(CardNo, Crd)];
                    var moveObj = AxisARC3DParaDic[Tuple.Create(CardNo, Crd)] = new CMD_3DArc_Req_Param
                    {
                        CmdNum = obj.CmdNum,
                        ParamRemain = obj.ParamRemain,
                        Channel = obj.Channel,

                        STflag = obj.STflag,
                        ParaType = obj.ParaType,
                        PosType = obj.PosType,
                        axis = obj.axis,
                        AxisPos = obj.AxisPos,

                        InterpoStartVel = Min_Vel / 1000,
                        InterpoEndVel = Stop_Vel / 1000,
                        InterpoVel = Max_Vel / 1000,
                        InterpoAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000),
                        InterpoDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000),
                        InterpJerkAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000) / 10,
                        InterpJerkDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000) / 10,
                    };
                    return 0;
                }
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
            }
            else if (type == 4)
            {
                if (AxisHelix2DParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
                {
                    //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                    var obj = AxisHelix2DParaDic[Tuple.Create(CardNo, Crd)];
                    var moveObj = AxisHelix2DParaDic[Tuple.Create(CardNo, Crd)] = new Helix_2D_Req_Param
                    {
                        CmdNum = obj.CmdNum,
                        ParamRemain = obj.ParamRemain,
                        Channel = obj.Channel,

                        STFlag = obj.STFlag,
                        ParaType = obj.ParaType,
                        PosType = obj.PosType,
                        axis = obj.axis,
                        AxisPos = obj.AxisPos,
                        Radius = obj.Radius,
                        EndRadius = obj.EndRadius,

                        InterpoStartVel = Min_Vel / 1000,
                        InterpoEndVel = Stop_Vel / 1000,
                        InterpoVel = Max_Vel / 1000,
                        InterpoAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000),
                        InterpoDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000),
                        InterpJerkAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000) / 10,
                        InterpJerkDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000) / 10,
                    };
                    return 0;
                }
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
            }
            else if (type == 5)
            {
                if (AxisHelix3DParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
                {
                    //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                    var obj = AxisHelix3DParaDic[Tuple.Create(CardNo, Crd)];
                    var moveObj = AxisHelix3DParaDic[Tuple.Create(CardNo, Crd)] = new Helix_3D_Req_Param
                    {
                        CmdNum = obj.CmdNum,
                        ParamRemain = obj.ParamRemain,
                        Channel = obj.Channel,

                        STFlag = obj.STFlag,
                        ParaType = obj.ParaType,
                        PosType = obj.PosType,
                        axis = obj.axis,
                        AxisPos = obj.AxisPos,
                        Radius = obj.Radius,

                        InterpoStartVel = Min_Vel / 1000,
                        InterpoEndVel = Stop_Vel / 1000,
                        InterpoVel = Max_Vel / 1000,
                        InterpoAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000),
                        InterpoDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000),
                        InterpJerkAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000) / 10,
                        InterpJerkDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000) / 10,
                    };
                    return 0;
                }
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
            }
            else if (type == 6)
            {
                if (AxisCone3DParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
                {
                    //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                    var obj = AxisCone3DParaDic[Tuple.Create(CardNo, Crd)];
                    var moveObj = AxisCone3DParaDic[Tuple.Create(CardNo, Crd)] = new Cone_3D_Req_Param
                    {
                        CmdNum = obj.CmdNum,
                        ParamRemain = obj.ParamRemain,
                        Channel = obj.Channel,

                        STFlag = obj.STFlag,
                        ParaType = obj.ParaType,
                        PosType = obj.PosType,
                        axis = obj.axis,
                        AxisPos = obj.AxisPos,
                        Radius = obj.Radius,
                        EndRadius = obj.EndRadius,

                        InterpoStartVel = Min_Vel / 1000,
                        InterpoEndVel = Stop_Vel / 1000,
                        InterpoVel = Max_Vel / 1000,
                        InterpoAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000),
                        InterpoDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000),
                        InterpJerkAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000) / 10,
                        InterpJerkDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000) / 10,
                    };
                    return 0;
                }
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
            }
            else if (type == 7)
            {
                if (AxisARC2D3PointParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
                {
                    //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                    var obj = AxisARC2D3PointParaDic[Tuple.Create(CardNo, Crd)];
                    var moveObj = AxisARC2D3PointParaDic[Tuple.Create(CardNo, Crd)] = new Arc_2D_3Point_Req_Param
                    {
                        CmdNum = obj.CmdNum,
                        ParamRemain = obj.ParamRemain,
                        channel = obj.channel,

                        vel_mode = obj.vel_mode,
                        mode = obj.mode,
                        axis_list = obj.axis_list,
                        start_pos = obj.start_pos,
                        mid_pos = obj.mid_pos,
                        aim_pos = obj.aim_pos,

                        velStart = Min_Vel / 1000,
                        velEnd = Stop_Vel / 1000,
                        synVel = Max_Vel / 1000,
                        synAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000),
                        synDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000),
                        synJerkAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000) / 10,
                        synJerkDec = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tdec * 1000) / 10,
                    };
                    return 0;
                }
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
            }
            throw new ArgumentNullException($"插补模式传入错误，模式；【{type}】");
            //return MCN420.mcc_crd_set_prf_vel((ushort)CardNo, (ushort)Crd, Min_Vel, Max_Vel, Tacc, Tdec, Stop_Vel);
        }

        /// <summary>
        /// 读取插补运动速度曲线
        /// </summary>
        /// <param name="CardNo">卡号 </param>
        /// <param name="Crd">坐标系号，取值范围：0~3</param>
        /// <param name="Min_Vel">返回最小速度设置</param>
        /// <param name="Max_Vel">返回最大速度设置</param>
        /// <param name="Tacc">返回加速时间设置</param>
        /// <param name="Tdec">返回减速时间设置</param>
        /// <param name="Stop_Vel">返回停止速度</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardVectorProfileUnit(int CardNo, int Crd, int type, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double Stop_Vel)
        {
            if (AxisLineParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
            {
                //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                var obj = AxisLineParaDic[Tuple.Create(CardNo, Crd)];
                Min_Vel = obj.InterpoStartVel * 1000;
                Max_Vel = obj.InterpoVel * 1000;
                Stop_Vel = obj.InterpoEndVel * 1000;
                Tacc = ((Max_Vel) - (Min_Vel)) / obj.InterpoAcc / 1000;
                Tdec = ((Max_Vel) - (Min_Vel)) / obj.InterpoDec / 1000;
                return 0;
            }
            throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
        }

        /// <summary>
        ///设置插补运动速度曲线的平滑时间
        /// </summary>
        /// <param name="CardNo">卡号 </param>
        /// <param name="Crd">坐标系号，取值范围：0~5 </param>
        /// <param name="s_mode">保留参数，固定值为 0</param>
        /// <param name="s_para">平滑时间，单位：s，范围：0~1</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardVectorSProfile(int CardNo, int Crd, int s_mode, double s_para, int type)
        {
            if (type == 1)
            {
                if (AxisLineParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
                {
                    //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                    var obj = AxisLineParaDic[Tuple.Create(CardNo, Crd)];
                    var moveObj = AxisLineParaDic[Tuple.Create(CardNo, Crd)] = new Line_Req_Param
                    {
                        CmdNum = obj.CmdNum,
                        ParamRemain = obj.ParamRemain,
                        Channel = obj.Channel,

                        STFlag = 1,
                        ParaType = 1,
                        PosType = obj.PosType,
                        axis = obj.axis,
                        AxisPos = obj.AxisPos,
                        nAxisNum = obj.nAxisNum,

                        InterpoStartVel = obj.InterpoStartVel,
                        InterpoEndVel = obj.InterpoEndVel,
                        InterpoVel = obj.InterpoVel,
                        InterpoAcc = obj.InterpoAcc,
                        InterpoDec = obj.InterpoDec,
                        InterpJerkAcc = ((s_para) * 1000 >= (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoAcc)) / 2 ? obj.InterpoAcc / 2 : ((s_para) * 1000 / (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoAcc))) * obj.InterpoAcc),
                        InterpJerkDec = ((s_para) * 1000 >= (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoDec)) / 2 ? obj.InterpoDec / 2 : ((s_para) * 1000 / (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoDec))) * obj.InterpoDec),
                        //InterpJerkAcc = Math.Pow(s_para * 1000, 3),
                        //InterpJerkDec = Math.Pow(s_para * 1000, 3),
                    };
                    return 0;
                }
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
            }
            else if (type == 2)
            {
                if (AxisARC2DParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
                {
                    //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                    var obj = AxisARC2DParaDic[Tuple.Create(CardNo, Crd)];
                    var moveObj = AxisARC2DParaDic[Tuple.Create(CardNo, Crd)] = new ARC_2D_Req_Param
                    {
                        CmdNum = obj.CmdNum,
                        ParamRemain = obj.ParamRemain,
                        Channel = obj.Channel,

                        STFlag = 1,
                        ParaType = 1,
                        PosType = obj.PosType,
                        axis = obj.axis,
                        AxisPos = obj.AxisPos,
                        Radius = obj.Radius,

                        InterpoStartVel = obj.InterpoStartVel,
                        InterpoEndVel = obj.InterpoEndVel,
                        InterpoVel = obj.InterpoVel,
                        InterpoAcc = obj.InterpoAcc,
                        InterpoDec = obj.InterpoDec,
                        InterpJerkAcc = ((s_para) * 1000 >= (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoAcc)) / 2 ? obj.InterpoAcc / 2 : ((s_para) * 1000 / (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoAcc))) * obj.InterpoAcc),
                        InterpJerkDec = ((s_para) * 1000 >= (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoDec)) / 2 ? obj.InterpoDec / 2 : ((s_para) * 1000 / (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoDec))) * obj.InterpoDec),
                    };
                    return 0;
                }
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
            }
            else if (type == 3)
            {
                if (AxisARC3DParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
                {
                    //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                    var obj = AxisARC3DParaDic[Tuple.Create(CardNo, Crd)];
                    var moveObj = AxisARC3DParaDic[Tuple.Create(CardNo, Crd)] = new CMD_3DArc_Req_Param
                    {
                        CmdNum = obj.CmdNum,
                        ParamRemain = obj.ParamRemain,
                        Channel = obj.Channel,

                        STflag = 1,
                        ParaType = 1,
                        PosType = obj.PosType,
                        axis = obj.axis,
                        AxisPos = obj.AxisPos,

                        InterpoStartVel = obj.InterpoStartVel,
                        InterpoEndVel = obj.InterpoEndVel,
                        InterpoVel = obj.InterpoVel,
                        InterpoAcc = obj.InterpoAcc,
                        InterpoDec = obj.InterpoDec,
                        InterpJerkAcc = ((s_para) * 1000 >= (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoAcc)) / 2 ? obj.InterpoAcc / 2 : ((s_para) * 1000 / (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoAcc))) * obj.InterpoAcc),
                        InterpJerkDec = ((s_para) * 1000 >= (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoDec)) / 2 ? obj.InterpoDec / 2 : ((s_para) * 1000 / (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoDec))) * obj.InterpoDec),
                    };
                    return 0;
                }
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
            }
            else if (type == 4)
            {
                if (AxisHelix2DParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
                {
                    //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                    var obj = AxisHelix2DParaDic[Tuple.Create(CardNo, Crd)];
                    var moveObj = AxisHelix2DParaDic[Tuple.Create(CardNo, Crd)] = new Helix_2D_Req_Param
                    {
                        CmdNum = obj.CmdNum,
                        ParamRemain = obj.ParamRemain,
                        Channel = obj.Channel,

                        STFlag = 1,
                        ParaType = 1,
                        PosType = obj.PosType,
                        axis = obj.axis,
                        AxisPos = obj.AxisPos,
                        Radius = obj.Radius,
                        EndRadius = obj.EndRadius,

                        InterpoStartVel = obj.InterpoStartVel,
                        InterpoEndVel = obj.InterpoEndVel,
                        InterpoVel = obj.InterpoVel,
                        InterpoAcc = obj.InterpoAcc,
                        InterpoDec = obj.InterpoDec,
                        InterpJerkAcc = ((s_para) * 1000 >= (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoAcc)) / 2 ? obj.InterpoAcc / 2 : ((s_para) * 1000 / (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoAcc))) * obj.InterpoAcc),
                        InterpJerkDec = ((s_para) * 1000 >= (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoDec)) / 2 ? obj.InterpoDec / 2 : ((s_para) * 1000 / (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoDec))) * obj.InterpoDec),
                    };
                    return 0;
                }
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
            }
            else if (type == 5)
            {
                if (AxisHelix3DParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
                {
                    //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                    var obj = AxisHelix3DParaDic[Tuple.Create(CardNo, Crd)];
                    var moveObj = AxisHelix3DParaDic[Tuple.Create(CardNo, Crd)] = new Helix_3D_Req_Param
                    {
                        CmdNum = obj.CmdNum,
                        ParamRemain = obj.ParamRemain,
                        Channel = obj.Channel,

                        STFlag = 1,
                        ParaType = 1,
                        PosType = obj.PosType,
                        axis = obj.axis,
                        AxisPos = obj.AxisPos,
                        Radius = obj.Radius,

                        InterpoStartVel = obj.InterpoStartVel,
                        InterpoEndVel = obj.InterpoEndVel,
                        InterpoVel = obj.InterpoVel,
                        InterpoAcc = obj.InterpoAcc,
                        InterpoDec = obj.InterpoDec,
                        InterpJerkAcc = ((s_para) * 1000 >= (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoAcc)) / 2 ? obj.InterpoAcc / 2 : ((s_para) * 1000 / (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoAcc))) * obj.InterpoAcc),
                        InterpJerkDec = ((s_para) * 1000 >= (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoDec)) / 2 ? obj.InterpoDec / 2 : ((s_para) * 1000 / (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoDec))) * obj.InterpoDec),
                    };
                    return 0;
                }
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
            }
            else if (type == 6)
            {
                if (AxisCone3DParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
                {
                    //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                    var obj = AxisCone3DParaDic[Tuple.Create(CardNo, Crd)];
                    var moveObj = AxisCone3DParaDic[Tuple.Create(CardNo, Crd)] = new Cone_3D_Req_Param
                    {
                        CmdNum = obj.CmdNum,
                        ParamRemain = obj.ParamRemain,
                        Channel = obj.Channel,

                        STFlag = 1,
                        ParaType = 1,
                        PosType = obj.PosType,
                        axis = obj.axis,
                        AxisPos = obj.AxisPos,
                        Radius = obj.Radius,
                        EndRadius = obj.EndRadius,

                        InterpoStartVel = obj.InterpoStartVel,
                        InterpoEndVel = obj.InterpoEndVel,
                        InterpoVel = obj.InterpoVel,
                        InterpoAcc = obj.InterpoAcc,
                        InterpoDec = obj.InterpoDec,
                        InterpJerkAcc = ((s_para) * 1000 >= (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoAcc)) / 2 ? obj.InterpoAcc / 2 : ((s_para) * 1000 / (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoAcc))) * obj.InterpoAcc),
                        InterpJerkDec = ((s_para) * 1000 >= (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoDec)) / 2 ? obj.InterpoDec / 2 : ((s_para) * 1000 / (((obj.InterpoVel) - (obj.InterpoStartVel)) / (obj.InterpoDec))) * obj.InterpoDec),
                    };
                    return 0;
                }
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
            }
            else if (type == 7)
            {
                if (AxisARC2D3PointParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
                {
                    //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                    var obj = AxisARC2D3PointParaDic[Tuple.Create(CardNo, Crd)];
                    var moveObj = AxisARC2D3PointParaDic[Tuple.Create(CardNo, Crd)] = new Arc_2D_3Point_Req_Param
                    {
                        CmdNum = obj.CmdNum,
                        ParamRemain = obj.ParamRemain,
                        channel = obj.channel,

                        vel_mode = 1,
                        mode = obj.mode,
                        axis_list = obj.axis_list,
                        start_pos = obj.start_pos,
                        mid_pos = obj.mid_pos,
                        aim_pos = obj.aim_pos,

                        velStart = obj.velStart,
                        velEnd = obj.velEnd,
                        synVel = obj.synVel,
                        synAcc = obj.synVel,
                        synDec = obj.synDec,
                        synJerkAcc = ((s_para) * 1000 >= (((obj.synVel) - (obj.velStart)) / (obj.synAcc)) / 2 ? obj.synAcc / 2 : ((s_para) * 1000 / (((obj.synVel) - (obj.velStart)) / (obj.synAcc))) * obj.synAcc),
                        synJerkDec = ((s_para) * 1000 >= (((obj.synVel) - (obj.velStart)) / (obj.synDec)) / 2 ? obj.synDec / 2 : ((s_para) * 1000 / (((obj.synVel) - (obj.velStart)) / (obj.synDec))) * obj.synDec),
                    };
                    return 0;
                }
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
            }
            throw new ArgumentNullException($"插补模式传入错误，模式；【{type}】");
        }

        /// <summary>
        /// 读取设置的插补运动速度曲线平滑时间
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="Crd">坐标系号，取值范围：0~</param>
        /// <param name="s_mode">保留参数，固定值为 0</param>
        /// <param name="s_para">返回平滑时间设置</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardVectorSProfile(int CardNo, int Crd, int s_mode, ref double s_para)
        {
            if (AxisLineParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
            {
                //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                s_para = (Math.Pow(AxisLineParaDic[Tuple.Create(CardNo, Crd)].InterpJerkAcc, 1 / 3)) / 1000;
                return 0;
            }
            throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
        }

        #endregion

        #region 插补运动函数
        /// <summary>
        /// 启动直线插补
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="Crd">坐标系号，取值范围：0~5 </param>
        /// <param name="axisNum">轴数，取值范围：2~16 </param>
        /// <param name="AxisNums">轴号数组 </param>
        /// <param name="Target_Pos">目标位置列表，单位：unit </param>
        /// <param name="posi_mode">运动模式，0：相对坐标模式，1：绝对坐标模式</param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisLineUnit(int CardNo, int Crd, int axisNum, ushort[] AxisNums, double[] Target_Pos, int posi_mode)    //
        {
            if (AxisLineParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
            {
                //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                var obj = AxisLineParaDic[Tuple.Create(CardNo, Crd)];
                var moveObj = AxisLineParaDic[Tuple.Create(CardNo, Crd)] = new Line_Req_Param
                {
                    CmdNum = obj.CmdNum,
                    ParamRemain = obj.ParamRemain,
                    Channel = obj.Channel,

                    STFlag = obj.STFlag,
                    ParaType = obj.ParaType,
                    PosType = (uint)posi_mode,
                    axis = Array.ConvertAll(AxisNums, s => (uint)s),
                    AxisPos = Target_Pos,
                    nAxisNum = (uint)axisNum,

                    InterpoStartVel = obj.InterpoStartVel,
                    InterpoEndVel = obj.InterpoEndVel,
                    InterpoVel = obj.InterpoVel,
                    InterpoAcc = obj.InterpoAcc,
                    InterpoDec = obj.InterpoDec,
                    InterpJerkAcc = obj.InterpJerkAcc,
                    InterpJerkDec = obj.InterpJerkDec,
                };
                for (int i = 0; i < axisNum; i++)
                {
                    MCN420.YK_remove_axis_alm((ushort)CardNo, AxisNums[i]);
                }
                SleepWaitMove();
                var res = MCN420.YK_line_move((ushort)CardNo, ref moveObj);
                return res;
            }
            throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
        }

        /// <summary>
        /// 圆弧插补
        /// <para>基于圆心圆弧扩展的螺旋线插补运动（圆心终点式圆弧/螺旋线/渐开线 ）（可作两轴圆弧插补） </para>
        /// <para>注  意：</para> 
        /// <para> 1） 当轴数为 2 时，轴列表前两轴进行平面螺旋</para>
        /// <para> 2） 当轴数为 3、运动轨迹为螺旋插补时，轴列表前两轴平面为基面，进行平面螺旋插补；同时，轴列表第三轴运动指定高度，该轴终点位置与该轴起点位置的差值为螺旋线段相对于基面的高度</para>
        /// <para> 3） 当轴数大于 3、运动轨迹为螺旋插补时，列表前三轴进行螺旋插补的同时，后续轴做线性跟随运动，运动时间与前三轴的运动时间相等</para>
        /// <para> 4） 当运动轨迹为螺旋插补时：</para>
        ///  <para>   轴列表前两轴组成的基面上，当起始点到圆心的距离小于终点到圆心的距离，为绽放螺旋线</para>
        ///  <para>   轴列表前两轴组成的基面上，当起始点到圆心的距离大于终点到圆心的距离，为收敛螺旋线</para>
        ///  <para>   轴列表前两轴组成的基面上，当起始点到圆心的距离等于终点到圆心的距离，为圆弧插补（插补轴数为 3 时则为圆柱螺旋线）</para>
        /// </summary>
        /// <param name="CardNo">卡号 </param>
        /// <param name="Crd">坐标系号，取值范围：0~5 </param>
        /// <param name="axisNum">轴数，取值范围： 2~16 </param>
        /// <param name="AxisNums">轴号数组</param>
        /// <param name="Target_Pos">目标位置数组，单位：unit</param>
        /// <param name="Cen_Pos">圆心位置数组，单位：unit </param>
        /// <param name="Arc_Dir">圆弧方向，0：顺时针，1：逆时针</param>
        /// <param name="Circle">圈数：自然数：表示此时执行的为螺旋线插补,螺旋线的圈数</param>
        /// <param name="posi_mode">运动模式，0：相对坐标模式，1：绝对坐标模式</param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisArcMoveCenterUnit(int CardNo, int Crd, int axisNum, UInt16[] AxisNums, double[] Target_Pos, double[] Cen_Pos, int Arc_Dir, int Circle, int posi_mode)
        {
            if (axisNum == 3)
            {
                if (AxisHelix3DParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
                {
                    //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                    var obj = AxisHelix3DParaDic[Tuple.Create(CardNo, Crd)];
                    var moveObj = AxisHelix3DParaDic[Tuple.Create(CardNo, Crd)] = new Helix_3D_Req_Param
                    {
                        CmdNum = obj.CmdNum,
                        ParamRemain = obj.ParamRemain,
                        Channel = obj.Channel,

                        STFlag = obj.STFlag,
                        ParaType = obj.ParaType,
                        PosType = (uint)posi_mode,
                        axis = obj.axis,
                        AxisPos = new double[] { Cen_Pos[0], Cen_Pos[1], Target_Pos[2] },
                        Radius = ((1 * Circle) / 180) * PI,

                        InterpoStartVel = obj.InterpoStartVel,
                        InterpoEndVel = obj.InterpoEndVel,
                        InterpoVel = obj.InterpoVel,
                        InterpoAcc = obj.InterpoAcc,
                        InterpoDec = obj.InterpoDec,
                        InterpJerkAcc = obj.InterpJerkAcc,
                        InterpJerkDec = obj.InterpJerkDec,
                    };
                    for (int i = 0; i < axisNum; i++)
                    {
                        MCN420.YK_remove_axis_alm((ushort)CardNo, AxisNums[i]);
                    }
                    var res = MCN420.YK_helix_3d_move((ushort)CardNo, ref moveObj);
                    SleepWaitMove();
                    return res;
                }
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
            }
            else
            {
                if (AxisHelix2DParaDic.ContainsKey(Tuple.Create(CardNo, Crd)))
                {
                    //(int)MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)mode, (ushort)home_dir, vel, tacc, tdec);
                    var obj = AxisHelix2DParaDic[Tuple.Create(CardNo, Crd)];
                    var moveObj = AxisHelix2DParaDic[Tuple.Create(CardNo, Crd)] = new Helix_2D_Req_Param
                    {
                        CmdNum = obj.CmdNum,
                        ParamRemain = obj.ParamRemain,
                        Channel = obj.Channel,

                        STFlag = obj.STFlag,
                        ParaType = obj.ParaType,
                        PosType = (uint)posi_mode,
                        axis = obj.axis,
                        AxisPos = Cen_Pos,
                        //Radius = Circle,
                        Radius = ((1 * Circle) / 180) * PI,
                        EndRadius = obj.EndRadius,  //后续通过 Target_Pos 和 Cen_Pos进行算出

                        InterpoStartVel = obj.InterpoStartVel,
                        InterpoEndVel = obj.InterpoEndVel,
                        InterpoVel = obj.InterpoVel,
                        InterpoAcc = obj.InterpoAcc,
                        InterpoDec = obj.InterpoDec,
                        InterpJerkAcc = obj.InterpJerkAcc,
                        InterpJerkDec = obj.InterpJerkDec,
                    };
                    for (int i = 0; i < axisNum; i++)
                    {
                        MCN420.YK_remove_axis_alm((ushort)CardNo, AxisNums[i]);
                    }
                    var res = MCN420.YK_helix_2d_move((ushort)CardNo, ref moveObj);
                    SleepWaitMove();
                    return res;
                }
                throw new ArgumentNullException($"为查找到对应的卡和轴  CardNo:{CardNo}  AxisId:{Crd}");
            }
        }
        /// <summary>
        /// 圆弧插补
        /// <para> 基于半径圆弧扩展的圆柱螺旋线插补运动（可作两轴圆弧插补） </para>
        /// <para>注  意：</para> 
        /// <para> 1）当轴数为 2 时，轴列表前两轴进行平面圆弧插补</para>
        /// <para> 2）当轴数为 3 时，轴列表前两轴平面为基面，进行平面圆弧插补；同时，轴列表第三轴运动指定高度；该轴终点位置与该轴起点位置的差值为圆柱螺旋线段相对于基面的高度 </para> 
        /// </summary>
        /// <param name="CardNo">卡号 </param>
        /// <param name="Crd">坐标系号，取值范围：0~5 </param>
        /// <param name="axisNum">轴数，取值范围： 2~16 </param>
        /// <param name="AxisNums">轴号数组</param>
        /// <param name="Target_Pos">目标位置数组，单位：unit</param>
        /// <param name="Arc_Radius">圆弧半径值，单位：unit  </param>
        /// <param name="Arc_Dir">圆弧方向，0：顺时针，1：逆时针</param>
        /// <param name="Circle">圈数：自然数：表示此时执行的为螺旋线插补,螺旋线的圈数。如:0 即表示 0 圈螺旋线插补，1 即表示 1 圈螺旋线插补 </param>
        /// <param name="posi_mode">运动模式，0：相对坐标模式，1：绝对坐标模式</param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisArcMoveRadiusUnit(int CardNo, int Crd, int axisNum, UInt16[] AxisNums, double[] Target_Pos, double Arc_Radius, int Arc_Dir, int Circle, int posi_mode)
        {
            //return MCN420.mcc_crd_circle_radius((ushort)CardNo, (ushort)Crd, (ushort)axisNum, AxisNums, Target_Pos, Arc_Radius, (ushort)Arc_Dir, (ushort)Circle, (ushort)posi_mode);
            throw new AggregateException("未添加！");
        }

        /// <summary>
        /// 圆弧插补
        ///<para>功  能：基于三点圆弧扩展的圆柱螺旋线插补运动（可作两轴及三轴圆弧插补） </para>
        ///<para>注  意：</para><para>1）当轴数为 2 时，轴列表前两轴进行平面圆弧插补</para>
        ///<para>2）当轴数为 3、运动轨迹为圆柱螺旋插补时，轴列表前两轴平面为基面，进行平面圆弧插补；同时，轴列表第三轴运动指定高度；该轴终点位置与该轴起点位置的差值为圆柱螺旋线段相对于基面的高度</para>
        ///<para>3）当轴数大于 3、运动轨迹为螺旋插补时，列表前三轴进行螺旋插补的同时，后续轴做线性跟随运动，运动时间与前三轴的运动时间相等</para>
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="Crd">坐标系号，取值范围：0~5</param>
        /// <param name="axisNum">轴数，取值范围：2~16 </param>
        /// <param name="AxisNums">轴号数组</param>
        /// <param name="Target_Pos">目标位置数组，单位：unit</param>
        /// <param name="Mid_Pos">中间位置数组，单位：unit</param>
        /// <param name="Circle">圈数：<para>负数：表示此时执行的为空间圆弧插补：该值的绝对值减 1 表示空间圆弧的圈数。如，-1 即表示 0圈空间圆弧，-2 即表示 1 圈空间圆弧… </para><para>自然数：表示此时执行的为圆柱螺旋线插补:该值表示螺旋线的圈数。如，0 即表示 0 圈螺旋线插补， 1 即表示 1 圈螺旋线插补… </para></param>
        /// <param name="posi_mode">运动模式，0：相对坐标模式，1：绝对坐标模式 </param>
        /// <returns><错误代码/returns>
        public int DmcCardArcMove3PointsUnit(int CardNo, int Crd, int axisNum, UInt16[] AxisNums, double[] Target_Pos, double[] Mid_Pos, int Circle, int posi_mode)
        {
            throw new AggregateException("未添加！");
            //return MCN420.mcc_crd_circle_3points((ushort)CardNo, (ushort)Crd, (ushort)axisNum, AxisNums, Target_Pos, Mid_Pos, (ushort)Circle, (ushort)posi_mode);
        }
        #endregion

        #region 总线操作函数

        /// <summary>
        /// 读取从站对象字典参数值
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="PortNum">EtherCAT 端口号，固定为 2</param>
        /// <param name="nodenum">从站 EtherCAT 地址，第 i 个 EtherCAT 从站为 1000+i</param>
        /// <param name="index">对象字典索引</param>
        /// <param name="subindex">对象字典子索引</param>
        /// <param name="valuelength">对象字典索引长度(单位：bit)</param>
        /// <param name="value">对象字典索引参数值</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardNodeOD(int CardNo, int PortNum, int nodenum, int index, int subindex, int valuelength, ref int value)
        {
            throw new AggregateException("未添加！");
            //return MCN420.ecc_get_node_od((ushort)CardNo, (ushort)PortNum, (ushort)nodenum, (ushort)index, (ushort)subindex, (ushort)valuelength, ref value);
        }

        /// <summary>
        ///设置 EtherCAT 总线驱动器使能
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴的轴号，255 表示使能所有 EtherCAT 轴</param>
        /// <returns>错误代码</returns>
        public int NmcSetCardAxisEnable(int CardNo, int axis)
        {
            return MCN420.YK_set_sevon_config((ushort)CardNo, (ushort)axis, 1);
        }

        /// <summary>
        /// 设置 EtherCAT 总线驱动器失能
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴的轴号，255 表示使能所有 EtherCAT 轴</param>
        /// <returns>错误代码</returns>
        public int NmcSetCardAxisDisable(int CardNo, int axis)
        {
            return MCN420.YK_set_sevon_config((ushort)CardNo, (ushort)axis, 0);
        }

        /// <summary>
        /// 读取 EtherCAT 总线轴和虚拟轴轴数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="TotalAxis">EtherCAT 总线轴和虚拟轴轴数</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardTotalAxes(int CardNo, ref uint TotalAxis)
        {
            //return MCN420.mcc_board_get_axis_num((ushort)CardNo, ref TotalAxis);
            throw new AggregateException("未添加！");
        }
        /// <summary>
        /// 读取 EtherCAT 总线 AD/DA 输入输出口数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="TotalIn">EtherCAT 总线 AD 输入数</param>
        /// <param name="TotalOut">EtherCAT 总线 DA 输出数</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardTotalAdcnum(int CardNo, ref ushort TotalIn, ref ushort TotalOut)
        {
            throw new AggregateException("未添加！");
            //return MCN420.ecc_get_total_adcnum((ushort)CardNo, ref TotalIn, ref TotalOut);
        }

        /// <summary>
        /// 读取 EtherCAT 总线 IO 输入输出口数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="TotalIn">EtherCAT 总线 IO 输入数，包括板卡自带以及 IO 模块上的输入</param>
        /// <param name="TotalOut">EtherCAT 总线 IO 输出数，包括板卡自带以及 IO 模块上的输出</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardTotalIONum(int CardNo, ref UInt16 TotalIn, ref UInt16 TotalOut)
        {
            throw new AggregateException("未添加！");
            //return MCN420.ecc_get_total_ionum((ushort)CardNo, ref TotalIn, ref TotalOut);
        }

        /// <summary>
        /// 设置控制卡工作模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="controller_mode">控制器工作模式，0 表示仿真模式，1 表示 EtherCAT总线模式</param>
        /// <returns>错误代码</returns>
        public int NmcSetCardControllerWorkMode(int CardNo, int controller_mode)
        {
            //return MCN420.ecc_set_controller_workmode((ushort)CardNo, (ushort)controller_mode);
            return 0;
        }

        /// <summary>
        /// 读取控制卡工作模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="controller_mode">控制卡的工作模式，0 表示仿真模式，1 表示 EtherCAT 总线模式</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardControllerWorkMode(int CardNo, ref ushort controller_mode)
        {
            return 0;
            //return MCN420.ecc_get_controller_workmode((ushort)CardNo, ref controller_mode);
        }

        /// <summary>
        /// 读取EtherCAT总线循环周期
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="FieldbusType">EtherCAT 端口号，固定为 2</param>
        /// <param name="CyclFieldbusTypeeTime">EtherCAT 总线循环周期，单位：us，支持 250/500/1000/2000</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardCydleTime(int CardNo, int FieldbusType, ref int CyclFieldbusTypeeTime)
        {
            //uint time = 0;
            //var res = MCN420.ecc_get_cycletime((ushort)CardNo, (ushort)FieldbusType, ref time);
            //CyclFieldbusTypeeTime = (int)time;
            //return res;
            return 0;
        }

        /// <summary>
        /// 读取轴类型
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴号</param>
        /// <param name="Axis_Type">轴的类型，0：虚拟轴，1：EtherCAT 轴，2：CANopen轴，3：脉冲轴，4：未知类型轴</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardAxisType(int CardNo, int axis, ref ushort Axis_Type)
        {
            //return MCN420.ecc_get_axis_type((ushort)CardNo, (ushort)axis, ref Axis_Type);
            return 0;
        }
        /// <summary>
        /// 停止EtherCAT总线运行
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="ETCState">0：停止 EtherCAT 总线成功，1：停止 EtherCAT 总线失败</param>
        /// <returns>错误代码</returns>
        public int NmcStopCardEtc(int CardNo, ref ushort ETCState)
        {
            //return MCN420.ecc_break_ecat((ushort)CardNo, ETCState);
            return 0;
        }

        /// <summary>
        /// 读取EtherCAT总线平均周期时间、最大周期时间、执行周期数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Fieldbustype">EtherCAT 端口号，固定为 2</param>
        /// <param name="Average_time">EtherCAT 总线周期时间，单位：us</param>
        /// <param name="Max_time">EtherCAT 总线最大周期时间，单位：us</param>
        /// <param name="Cycles">EtherCAT 总线执行周期数</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardConsumeTimeFieldbus(int CardNo, int Fieldbustype, ref uint Average_time, ref uint Max_time, ref UInt64 Cycles)
        {
            //return MCN420.ecc_get_cycletime((ushort)CardNo, (ushort)Fieldbustype, ref Average_time/*, ref Max_time, ref Cycles*/);
            return 0;
        }

        /// <summary>
        /// 清除EtherCAT总线平均周期时间、最大周期时间、执行周期数等记录
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Fieldbustype">EtherCAT 端口号，固定为 2</param>
        /// <returns>错误代码</returns>
        public int NmcClearCardConsumeTimeFieldbus(int CardNo, int Fieldbustype)
        {
            //return MCN420.ecc_clear_consume_time_fieldbus((ushort)CardNo, (ushort)Fieldbustype);
            return 0;
        }

        /// <summary>
        /// 读取EtherCAT总线轴状态机
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴轴号</param>
        /// <param name="Axis_StateMachine">EtherCAT 总线轴状态机
        /// <para> 0：NOT_READY_SWITCH_ON  轴处于未启动状态  </para>
        /// <para>1：SWITCH_ON_DISABLE  轴处于启动禁止状态 </para>
        /// <para>2：READY_TO_DISABLE  轴处于准备启动状态(轴失能) </para>
        /// <para>3：SWOTCH_ON  轴处于启动状态     </para>
        /// <para>4：OP_ENABLE  轴处于操作使能状态（轴已经使能） </para>
        /// <para>5：QUICK_STOP  轴处于停止状态     </para>
        /// <para>6：FAULT_ACTIVE  轴处于错误触发状态 </para>
        /// <para>7：FAULT  轴处于错误状态     </para>
        /// </param>
        /// <returns>错误代码</returns>
        public int NmcGetCardAxisStateMachine(int CardNo, int axis, ref ushort Axis_StateMachine)
        {
            //var res = MCN420.mcc_status_get_axis_io((ushort)CardNo, (ushort)axis);
            //if ((res & 1024) == 1024) Axis_StateMachine = 4;
            //return 0;
            Axis_StateMachine = (ushort)MCN420.YK_get_sevon_config((ushort)CardNo, (ushort)axis);
            return 0;
        }

        /// <summary>
        /// 获取EtherCAT总线轴配置控制模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴轴号</param>
        /// <param name="contrlmode">EtherCAT 总线轴配置控制模式（6 回零模式、8 CSP 模式）</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardAxisSettingControlMode(int CardNo, int axis, ref int contrlmode)
        {
            //return MCN420.ecc_get_axis_setting_contrlmode((ushort)CardNo, (ushort)axis, ref contrlmode);
            return 0;
        }

        /// <summary>
        /// 设置 EtherCAT 总线轴回零参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">EtherCAT 总线轴轴号</param>
        /// <param name="home_mode">EtherCAT 总线轴回零模式</param>
        /// <param name="Low_Vel">EtherCAT 总线轴回零低速</param>
        /// <param name="High_Vel">EtherCAT 总线轴回零高速</param>
        /// <param name="Tacc">EtherCAT 总线轴回零加速时间</param>
        /// <param name="Tdec">EtherCAT 总线轴回零减速时间</param>
        /// <param name="offsetpos">EtherCAT 总线轴回零偏移 </param>
        /// <returns>错误代码</returns>
        public int NmcSetCardAxisHomeProfile(int CardNo, int axis, int home_mode, double Low_Vel, double High_Vel, double Tacc, double Tdec, double offsetpos)
        {
            throw new ArgumentException("未添加！");
            //return MCN420.ecc_home_set_para((ushort)CardNo, (ushort)axis, (ushort)home_mode, Low_Vel, High_Vel, Tacc, Tdec);
        }

        /// <summary>
        /// 获取 EtherCAT 总线轴回零参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">EtherCAT 总线轴轴号</param>
        /// <param name="home_mode">返回EtherCAT 总线轴回零模式</param>
        /// <param name="Low_Vel">返回EtherCAT 总线轴回零低速</param>
        /// <param name="High_Vel">返回EtherCAT 总线轴回零高速</param>
        /// <param name="Tacc">返回EtherCAT 总线轴回零加速时间</param>
        /// <param name="Tdec">返回EtherCAT 总线轴回零减速时间</param>
        /// <param name="offsetpos">返回EtherCAT 总线轴回零偏移 </param>
        /// <returns>错误代码</returns>
        public int NmcGetCardAxisHomeProfile(int CardNo, int axis, ref ushort home_mode, ref double Low_Vel, ref double High_Vel, ref double Tacc, ref double Tdec, ref double offsetpos)
        {
            throw new ArgumentException("未添加！");
            //return MCN420.ecc_home_get_para((ushort)CardNo, (ushort)axis, ref home_mode, ref Low_Vel, ref High_Vel, ref Tacc, ref Tdec);
        }

        /// <summary>
        /// 启动 EtherCAT 总线轴回零
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴轴号</param>
        /// <returns>错误代码</returns>
        public int NmcCardAxisHomeMove(int CardNo, int axis)
        {
            return 0;
        }

        /// <summary>
        /// 读取 EtherCAT 总线卡状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="channel">EtherCAT 总线端口号，固定为 2</param>
        /// <param name="errcode">EtherCAT 总线状态，0 表示正常</param>
        /// <returns></returns>
        public int NmcGetCardErrcode(int CardNo, int channel, ref ushort errcode)
        {
            //var res = MCN420.ecc_get_master_state_machine((ushort)CardNo, /*(ushort)channel,*/ ref errcode);
            //if (errcode == 8) errcode = 0;
            //else errcode = 125;
            //return res;
            errcode = (ushort)(MCN420.YK_check_device_network_link((ushort)CardNo) ? 0 : 128);
            return 0;
        }

        /// <summary>
        /// 获读取 EtherCAT 轴节点地址信息
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴号</param>
        /// <param name="SlaveAddr">EtherCAT 总线轴地址</param>
        /// <param name="Sub_SlaveAddr">EtherCAT 总线轴子地址</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardAxisNodeAddress(int CardNo, int axis, int lenght, ref ushort SlaveAddr, ref ushort Sub_SlaveAddr, ref int value)
        {
            return 0;
            //return MCN420.ecc_get_node_od((ushort)CardNo, 0, (ushort)axis, SlaveAddr, Sub_SlaveAddr, (ushort)lenght, ref value);
        }

        /// <summary>
        /// 获取 EtherCAT 从站总数
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="PortNum">EtherCAT 端口号，固定为 2</param>
        /// <param name="TotalSlaves">EtherCAT 从站总数</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardTotalSlaves(int CardNo, int PortNum, ref ushort TotalSlaves)
        {
            //return MCN420.ecc_get_total_slaves((ushort)CardNo, (ushort)PortNum, ref TotalSlaves);
            return 0;
        }

        #endregion

        #region 指令缓存
        public int DmcMOpenListBuff(int CardNo, ushort Group, int num, ushort[] axis_List)
        {
            return MCN420.YK_open_lookahead((ushort)CardNo, (ushort)Group);
        }
        public int DmcMStartListBuff(int CardNo, ushort Group)
        {
            return 0;
            //return MCN420.mcc_m_start_list((ushort)CardNo, (ushort)Group);
        }

        public int DmcMAddSigaxisMovesegDataExBuff(int CardNo, ushort Group, ushort axis, double target_Pos, ushort mark)
        {
            return 0;
            //默认位置模式，0:相对位置，1：绝对位置;
            ushort mode = 1;
            //return MCN420.mcc_m_add_sigaxis_pmove((ushort)CardNo, Group, axis, target_Pos, mode, mark);
        }

        public int DmcMAddMutiaxisMovesegDataExBuff(int CardNo, ushort Group, ushort[] axis, double[] target_Pos, ushort mark)
        {
            return 0;
            //默认位置模式，0:相对位置，1：绝对位置;
            ushort mode = 1;
            //return MCN420.mcc_m_add_mutiaxis_pmove((ushort)CardNo, Group, (ushort)axis.Length, axis, target_Pos, mode, mark);
        }

        public int DmcMAddWaitEventDataBuff(int CardNo, ushort Group, ushort event1, ushort num, ushort CompareOperate, double target_Pos, ushort mark)
        {
            return 0;
            //return MCN420.mcc_m_add_wait_event_data((ushort)CardNo, (ushort)Group, event1, num, CompareOperate, target_Pos, mark);

        }

        public int DmcMSetProfileUnitBuff(int CardNo, ushort Group, ushort axis, double start_vel, double max_vel, double tacc, double tdec, double smooth_time, double stop_vel, ushort mark)
        {
            return 0;
            //return MCN420.mcc_m_set_axis_profile((ushort)CardNo, (ushort)Group, axis, start_vel, max_vel, tacc, tdec, smooth_time, stop_vel, mark);
        }
        public int DmcMSetMutiProfileUnitBuff(int CardNo, ushort Group, ushort[] axis, double[] start_vel, double[] max_vel, double[] tacc, double[] tdec, double[] smooth_time, double[] stop_vel, ushort mark)
        {
            return 0;
            //return MCN420.mcc_m_set_muti_profile((ushort)CardNo, (ushort)Group, (ushort)axis.Length, axis, start_vel, max_vel, tacc, tdec, smooth_time, stop_vel, mark);
        }

        public int DmcMCloseListBuff(int CardNo, ushort Group)
        {
            return 0;
            //return MCN420.YK_close_lookahead((ushort)CardNo, (ushort)Group);
        }
        public int DmcMClearListBuff(int CardNo, ushort Group)
        {
            return 0;
            //return MCN420.mcc_m_clear_list((ushort)CardNo, (ushort)Group);
        }
        public int DmcMStopListBuff(int CardNo, ushort Group)
        {
            return 0;
            //return MCN420.mcc_m_stop_list((ushort)CardNo, (ushort)Group, 1);
        }
        public int DmcMSetProfileUnitBuff_Line(int CardNo, ushort Group, ushort crd, double start_vel, double max_vel, double tacc, double tdec, double smooth_time, double stop_vel, ushort mark)
        {
            return 0;
            //return MCN420.mcc_m_set_line_prf_vel((ushort)CardNo, crd, (ushort)Group, start_vel, max_vel, stop_vel, tacc, tdec, smooth_time, mark);
        }
        public int DmcMAddMutiaxisMovesegDataExBuff_Line(int CardNo, ushort Group, ushort crd, ushort[] axis, double[] target_Value, ushort mark)
        {
            return 0;
            //默认位置模式，0:相对位置，1：绝对位置;
            ushort mode = 1;
            //return MCN420.mcc_m_line_motion((ushort)CardNo, crd, Group, (ushort)axis.Length, axis, target_Value, mode, mark);
        }
        public int DmcMSetRegisterFlagStsBuff(int CardNo, ushort Group, ushort Flag_No, ushort Wait_Flag_sts, ushort mark)
        {
            return 0;
            //return MCN420.mcc_m_set_control_flag((ushort)CardNo, (ushort)Group, Flag_No, Wait_Flag_sts, mark);
        }

        public int DmcAddDelayTimeBuff(int CardNo, ushort Group, double Time_delay, ushort mark)
        {
            var delay = new Delay_Time_Req
            {
                CmdNum = mark,
                Channel = Group,
                Times = (uint)Time_delay,
                ParamRemain = 0
            };
            return MCN420.YK_delay_time((ushort)CardNo, ref delay);
        }
        public int DmcAddTriggerDataBuff(int CardNo, ushort Group, ushort Mode, ushort Num, double target_Value, ushort mark)
        {
            return 0;
            //return MCN420.mcc_m_add_trigger_data((ushort)CardNo, (ushort)Group, Mode, Num, target_Value, mark);
        }

        public int DmcMSetRegisterFlagSts(int CardNo, ushort Group, ushort Flag_No, ushort Wait_Flag_sts)
        {
            return 0;
            //return MCN420.mcc_m_set_wait_flag((ushort)CardNo, (ushort)Group, Flag_No, Wait_Flag_sts);
        }

        public int DmcMReadRegisterFlagSts(int CardNo, ushort Group, ushort Flag_No, ref ushort Wait_Flag_sts)
        {
            return 0;
            //return MCN420.mcc_m_get_wait_flag((ushort)CardNo, (ushort)Group, Flag_No, ref Wait_Flag_sts);
        }
        #endregion

        #region EMG Config
        /// <summary>
        /// 急停配置
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="io_Port"></param>
        /// <param name="enable"></param>
        /// <param name="emg_logic"></param>
        /// <param name="stop_mode"></param>
        /// <param name="filter_time"></param>
        /// <returns></returns>
        public int ConfigureEMGMapping(int CardNo, ushort[] io_Port, ushort enable, ushort emg_logic = 1, ushort stop_mode = 1, double filter_time = 5)
        {
            return 0;
            //return MCN420.mcc_emg_set_stop_para_ex((ushort)CardNo, (ushort)io_Port.Length, enable, emg_logic, stop_mode, filter_time, io_Port);
        }
        #endregion
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
