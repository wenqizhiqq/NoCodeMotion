﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/SoftServo/CardRealization.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Samsun.Domain.MotionCard.Common.SoftServo_EtherCAT
{

    /// <summary>
    /// DMC_E3032_EtherCAT 的ICard实现类
    /// </summary>
    public class CardRealization : ICard
    {

        //[DisplayName("运动控制卡的配置文件名字"), Description("运动控制卡的配置文件名字"), Category("卡参数设置")]
        //public string CardConfigFileName { get; set; } = "";


        [DisplayName("卡是否打开"), Description("true为打开状态"), Category("卡参数设置")]
        /// <summary>
        /// 卡已经打开
        /// </summary>
        public bool isOpen { get; set; } = false;

        [DisplayName("卡是否初始化"), Description("true为已经初始化状态"), Category("卡参数设置")]
        /// <summary>
        /// 卡已经初始化
        /// </summary>
        public bool isInit { get; set; } = false;

        [DisplayName("卡是否为虚拟卡"), Description("true则置卡为虚拟运动卡，以方便调试"), Category("卡参数设置")]
        /// <summary>
        /// 同型号多卡的数量
        /// </summary>
        public ushort CardSum { get; set; } = 1;

        [DisplayName("多少张卡"), Description("注意指的是同型号多张卡的数量"), Category("卡参数设置")]
        /// <summary>
        /// 虚拟卡，方便脱机调试
        /// </summary>
        public bool IsVitualCard { get; set; } = false;


        [DisplayName("运动卡输出IO的数量"), Description("运动卡输出IO的数量"), Category("卡参数设置")]
        /// <summary>
        /// 运动卡输出IO的数量
        /// </summary>
        public int CardOutputIOSum { get; set; } = 6;

        [DisplayName("运动卡输入IO的数量"), Description("运动卡输入IO的数量"), Category("卡参数设置")]
        /// <summary>
        /// 运动卡输入IO的数量
        /// </summary>
        public int CardInputIOSum { get; set; } = 4;


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


        /// <summary>
        /// 硬件名字,这里用枚举，以方便程序中轮询卡
        /// </summary>
        public SystemSupportMotionCardEnum HardwareName { get; set; } = SystemSupportMotionCardEnum.MCCE332;


        [DisplayName("卡状态"), Description("卡状态，反馈卡是否在线模式"), Category("Online设置"), ReadOnly(true)]
        /// <summary>
        /// 判断是否为离线模式
        /// </summary>
        public bool IsOffineMode { get; set; } = false;
        public CardRealization()
        {
            //因为在调用同OpenCard()之前，还不知道电脑里有多少张同型号卡
            //所以预定义16张轴参数表
            //for (int i = 0; i < 16; i++)
            //{
            //    ListCardParam.Add(new CardParamModel()
            //    {
            //        CardName = SystemSupportMotionCardEnum.DMC_E3032_EtherCAT.ToString(),
            //        CardRemarks = "PCI总线卡，16IO输出，16IO输出",
            //        CardSuportAxisNum = 8,
            //        CardType = 0,
            //        OutportNum = 4,
            //        InportNum = 4,
            //        ControllerMode = 1,
            //        CyclFieldbusTypeeTime = 500,
            //        LibVersion = 2101,
            //        EtherCATState = 0,
            //        FileName = "",
            //        Mode = 0,
            //        Axes = default(AxisParamModel[])
            //    });
            //}

        }




        /// <summary>
        /// 卡参数列表
        /// </summary>
        //public List<CardParamModel> ListCardParam { get { return _ListCardParam; } set { _ListCardParam = value; } }
        //private List<CardParamModel> _ListCardParam = new List<CardParamModel>();

        public List<CardParamModel> ListCardParam { get; set; } = new List<CardParamModel>();



        /// <summary>
        /// 控制卡初始化函数
        /// </summary>
        /// <returns>卡的数量</returns>
        public int InitCard()
        {
            if (IsVitualCard)
            {
                isInit = true;
                return 1;
            }
            var num = SoftVersoSDK.Instance.DmcInitCard();
            if (SoftVersoSDK.Instance.IsInit)
            {
                isInit = true;
                return num;
            }
            isInit = true;
            IsOffineMode = true;
            return 1;
            //isInit = false;
            //return -1;
        }



        /// <summary>
        /// 打开卡
        /// </summary>
        /// <param name="cardnum">返回初始化成功的卡数 </param>
        /// <param name="cardtypes">返回控制卡固件类型数组</param>
        /// <param name="CardID">返回控制卡硬件 ID 号数组，卡号按从小到大顺序排列  </param>
        /// <returns>错误代码，返回零为成功，大于0的数表示第几张卡打开出错</returns>
        public int OpenCard(ref ushort cardnum, ref uint[] cardtypes, ref ushort[] CardID)
        {
            //dmc_get_CardInfList(ref cardnum, cardtypes, CardIdList);
            int res = SoftVersoSDK.Instance.DmcGetCardInList(ref cardnum, ref cardtypes, ref CardID);
            if (res != 0)
            {
                isOpen = true;
                IsOffineMode = true;
                return 0;
                //isOpen = false;
                //return res;
            }

            var cardConfigPath = AppDomain.CurrentDomain.BaseDirectory + "MotionConfigFile";
            if (!Directory.Exists(cardConfigPath))
            {
                Directory.CreateDirectory(cardConfigPath);
            }
            var configFiles = Directory.GetFiles(cardConfigPath);

            //卡的配置文件数组（可能有多张卡要载入配置文件）
            //var cardConfigFileNameAry = CardConfigFileName.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            uint cardtype = 0;
            int res1 = -1;
            var cardOFAxisConfigFname = "";
            var cardOFStationsConfigFname = "";
            for (int i = 0; i < cardnum; i++)
            {
                //ListCardParam[i].CardType = null == cardtypes ? 0 : (int)cardtypes[i];
                //ListCardParam[i].CardNo = null == CardID ? 0 : CardID[i];

                if (HardwareName == SystemSupportMotionCardEnum.DMC3000系列)
                {
                    //卡的配置文件由卡参数指定，改为在磁盘目录下搜索指定关键字的文件
                    cardtype = cardtypes[i];
                    switch (cardtype)
                    {
                        case 13312:
                            cardOFAxisConfigFname = configFiles.ToList().Find(s => s.IndexOf("3400") >= 0);
                            break;
                        case 14336:
                            cardOFAxisConfigFname = configFiles.ToList().Find(s => s.IndexOf("3800") >= 0);
                            break;

                    }
                    res1 = SoftVersoSDK.Instance.GetCardDownloadConfigfile(CardID[i],
                       /*cardConfigPath + "\\" + */ cardOFAxisConfigFname /*20210811.ini"*/);
                    if (res1 != 0)
                    {
                        res = i + 1;
                        CMessage.Instance.AddTipsMessage(DeviceMessageTypeEnum.报警消息窗口, $"卡 [{HardwareName}] 载入配置文件 [{AppDomain.CurrentDomain.BaseDirectory + cardOFAxisConfigFname}] 出错！");
                        break;
                    }
                }
                else if (HardwareName == SystemSupportMotionCardEnum.MCCE332)//todo:2022.11.26
                {
                    //卡的配置文件由卡参数指定，改为在磁盘目录下搜索指定关键字的文件

                    cardtype = cardtypes[i];
                    cardtype = cardtype & 0xfffff;
                    //switch (cardtype)
                    //{
                    //    case 0x13032:
                    //cardConfigFname = configFiles.ToList().Find(s => s.ToUpper().IndexOf("E3032") >= 0);
                    //string iniName = $"E3032_{CardID[i]}";  //todo:获取对应卡号的站号配置文件
                    string iniName = $"MCCE3032INI_{CardID[i]}.XML";  //todo:获取对应卡号的站号配置文件 ----2024/07/31 轴载入进行 
                    string eniName = $"MCCE3032ENI_{CardID[i]}.XML";  //todo:获取对应卡号的站号配置文件 ----2024/07/31 轴载入进行 
                    cardOFAxisConfigFname = configFiles.ToList().Find(s => s.ToUpper().IndexOf(iniName) >= 0);
                    cardOFStationsConfigFname = configFiles.ToList().Find(s => s.ToUpper().IndexOf(eniName) >= 0);
                    //        break;
                    //}
                    bool isOK = false;
                    ushort errnum = 1;
                    var res2 = SoftVersoSDK.Instance.NmcGetCardErrcode(CardID[i], 2, ref errnum);
                    if (errnum != 0)//总线报错
                    {
                        res = MCCE135.ecc_reset_ecat(CardID[i], 0);
                        if (res != 0)//热复位失败
                        {
                            res = MCCE135.mcc_board_cold_reset();//冷复位失败
                            if (res != 0)
                            {
                                CMessage.Instance.AddTipsMessage(DeviceMessageTypeEnum.报警消息窗口, $"dmc_soft_reset(复位总线错误) == {res.ToString()}\r\n {SoftVersoSDK.Instance.GetEtherCATErrorInfo(res)}");
                                return i + 1;
                            }
                        }
                        CMessage.Instance.AddTipsMessage(DeviceMessageTypeEnum.报警消息窗口, $"总线复位中... ");
                        for (int j = 0; j < 15; j++)
                        {
                            ushort err = 1;
                            res = MCCE135.ecc_get_master_state_machine(CardID[i], ref err);
                            if (res == 0)
                            {
                                if (err == 0)//总线复位正常完成
                                {
                                    isOK = true;
                                    break;
                                }
                            }
                            else
                            {
                                CMessage.Instance.AddTipsMessage(DeviceMessageTypeEnum.报警消息窗口, $"复位总线错误::{SoftVersoSDK.Instance.GetErrorInfo(res)}");
                                return i + 1;
                            }
                            Thread.Sleep(1000);
                        }
                        if (!isOK)
                        {
                            CMessage.Instance.AddTipsMessage(DeviceMessageTypeEnum.报警消息窗口, $"总线复位失败！");
                            return i + 1;
                        }
                    }
                    isOK = false;
                    if (!isOK)
                    {
                        res = i + 1;
                        CMessage.Instance.AddTipsMessage(DeviceMessageTypeEnum.报警消息窗口, $"卡 [{HardwareName}] 载入配置文件 [{AppDomain.CurrentDomain.BaseDirectory + cardOFAxisConfigFname}] 出错！");
                        //todo:可进行eni文件导入
                        #region eni文件导入（目前屏蔽）

                        FileStream fs = File.Open(cardOFStationsConfigFname, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        StreamReader sr = new StreamReader(fs);
                        string str = sr.ReadToEnd();
                        byte[] buffer = Encoding.UTF8.GetBytes(str);
                        byte[] fileincontrol = Encoding.UTF8.GetBytes("");
                        ushort filetype = 201;
                        //res1 = SoftVersoSDK.Instance.GetCardDownloadMemfile(CardID[i], buffer, fileincontrol, filetype);     //下载配置文件
                        res1 = SoftVersoSDK.Instance.GetCardDownloadMemfile(CardID[i], cardOFStationsConfigFname);     //下载配置文件
                        if (res1 != 0)
                        {
                            CMessage.Instance.AddTipsMessage(DeviceMessageTypeEnum.报警消息窗口, $"卡 [{HardwareName}] 载入配置文件 [{AppDomain.CurrentDomain.BaseDirectory + cardOFStationsConfigFname}] 出错！");
                            isOK = false; break;
                        }
                        #endregion
                        /*break*/;
                    }
                    for (int j = 0; j < 15; j++)
                    {
                        res1 = SoftVersoSDK.Instance.GetCardDownloadConfigfile(CardID[i], cardOFAxisConfigFname);
                        if (res1 == 0)
                        {
                            isOK = true;
                            break;
                        }
                        Thread.Sleep(1000);
                    }



                    //LTDMC.dmc_download_configfile((ushort)CardNo, ConfigName);
                    //res1 = SoftVersoSDK.Instance.GetCardDownloadConfigfile(CardID[i], cardConfigFname);
                    //if (res1 != 0)
                    //{
                    //    res = i + 1;
                    //    CMessage.Instance.AddTipsMessage(DeviceMessageTypeEnum.AlarmMsg, $"卡 [{HardwareName}] 载入配置文件 [{AppDomain.CurrentDomain.BaseDirectory + cardConfigFname}] 出错！");
                    //    break;
                    //}

                }
            }
            isOpen = true;
            return res;

            /*
            if (IsVitualCard)
            {
                isOpen = true;
                return 0;
            }
            int res = SoftVersoSDK.Instance.DmcGetCardInList(ref cardnum, ref cardtypes, ref CardID);
            if (res != 0)
            {
                isOpen = false;
                return res;
            }

            for (int i = 0; i < cardnum; i++)
            {
                ListCardParam[i].CardType =null== cardtypes?0:(int)cardtypes[i];
                ListCardParam[i].CardNo = null==CardID?0:CardID[i];
               
                int res1= SoftVersoSDK.Instance.DmcGetDownloadConfigfile(ListCardParam[i].CardNo);
                if (res1 != 0)
                {
                    res = i+1; break;
                    //throw new ArgumentException("载入卡的配置文件时出错！");

                }
            }
            isOpen = true;
            return res;
            */
        }

        /// <summary>
        /// 获取控制卡动态库文件版本号
        /// </summary>
        /// <param name="LibVer">返回库版本号</param>
        /// <returns>错误代码</returns>
        public int GetCardLibVersion(int CardID, ref int LibVer, ref int pDriver_ver, ref int pLogic_ver)
        {
            try
            {
                uint libVer = 0;
                int res = SoftVersoSDK.Instance.DmcGetCardLibVersion(ref libVer);
                LibVer = (int)libVer;
                if (res != 0) { return res; }
                foreach (var item in ListCardParam)
                {
                    item.LibVersion = libVer;
                }
                return res;
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        /// <summary>
        /// 设置卡参数(此方法不能用)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="PortNum">EtherCAT 端口号，固定为 2</param>
        /// <param name="Baudrate">参数不明等待回应</param>
        /// <param name="NodeCnt">参数不明等待回应</param>
        /// <param name="MasterId">参数不明等待回应</param>
        /// <returns>错误代码</returns>
        public int SetCardMasterParam(int CardNo, int PortNum, int Baudrate, int NodeCnt, int MasterId)
        {
            return SoftVersoSDK.Instance.NmcSetCardMasterPara((ushort)CardNo, (ushort)PortNum, (ushort)Baudrate, (ushort)NodeCnt, (ushort)MasterId);
        }

        /// <summary>
        /// 获取卡参数(此方法不能用)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="PortNum">EtherCAT 端口号，固定为 2</param>
        /// <param name="Baudrate">参数不明等待回应</param>
        /// <param name="NodeCnt">参数不明等待回应</param>
        /// <param name="MasterId">参数不明等待回应</param>
        /// <returns>错误代码</returns>
        public int GetCardMasterParam(int CardNo, int PortNum, ref ushort Baudrate, ref uint NodeCnt, ref ushort MasterId)
        {
            return SoftVersoSDK.Instance.NmcGetCardMasterPara((ushort)CardNo, (ushort)PortNum, ref Baudrate, ref NodeCnt, ref MasterId);
        }

        ///// <summary>
        /////  无_设置从站配置信息
        ///// </summary>
        ///// <returns></returns>
        //public int SetCardSlaveStationConfig()
        //{
        //    return -1;// dMC_E3032.
        //}

        /// <summary>
        /// 获取从站配置信息
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴号</param>
        /// <param name="SlaveAddr">EtherCAT 总线轴地址</param>
        /// <param name="Sub_SlaveAddr">EtherCAT 总线轴子地址</param>
        /// <returns>错误代码</returns>
        public int GetCardSlaveStationConfig(int CardNo, int axis, int lenght, ref ushort SlaveAddr, ref ushort Sub_SlaveAddr, ref int value)
        {
            return SoftVersoSDK.Instance.NmcGetCardAxisNodeAddress((ushort)CardNo, (ushort)axis, ref SlaveAddr, ref Sub_SlaveAddr);
        }

        /// <summary>
        /// 设置EtherCAT总线循环周期 
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="Fieldbustype">  EtherCAT 端口号，固定为 2</param>
        /// <param name="CycleTime">CAT 总线循环周期，单位：us，支持 250/500/1000/</param>
        /// <returns>错误代码</returns>
        public int SetCardMasterScanCycleTime(int CardNo, int FieldbusType, int CycleTime)
        {
            return SoftVersoSDK.Instance.NmcSetCardCycleTime((ushort)CardNo, (ushort)FieldbusType, CycleTime);
        }

        /// <summary>
        /// 读取EtherCAT总线循环周期
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="FieldbusType">EtherCAT 端口号，固定为 2</param>
        /// <param name="CyclFieldbusTypeeTime">EtherCAT 总线循环周期，单位：us，支持 250/500/1000/2000</param>
        /// <returns>错误代码</returns>
        public int GetCardMasterScanCycleTime(int CardNo, int FieldbusType, ref int CyclFieldbusTypeeTime)
        {
            return SoftVersoSDK.Instance.NmcGetCardCydleTime((ushort)CardNo, (ushort)FieldbusType, ref CyclFieldbusTypeeTime);
        }

        /// <summary>
        /// 控制卡初始复位（适用于EtherCAT总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <returns>错误代码</returns>
        public int CardReset(int CardNo)
        {
            return SoftVersoSDK.Instance.DmcCardOriginalReset((ushort)CardNo);
        }

        /// <summary>
        ///硬件复位（适用于所有脉冲/总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int CardBoardReset()
        {
            return SoftVersoSDK.Instance.DmcCardBoardReset();
        }

        /// <summary>
        /// 控制卡热复位（适用于EtherCAT、RTEX总线卡）  
        /// </summary>
        /// <returns>错误代码</returns>
        public int CardSoftReset(ushort CardNo)
        {
            return SoftVersoSDK.Instance.DmcCardSoftReset(CardNo);
        }

        /// <summary>
        /// 控制卡冷复位（适用于所有脉冲/总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int CardCoolReset(ushort CardNo)//todo:2022.11.26
        {
            bool isOK = false;
            var cardConfigPath = AppDomain.CurrentDomain.BaseDirectory + "MotionConfigFile";
            if (!Directory.Exists(cardConfigPath))
            {
                Directory.CreateDirectory(cardConfigPath);
            }
            var configFiles = Directory.GetFiles(cardConfigPath);
            var cardConfigFname = configFiles.ToList().Find(s => s.ToUpper().IndexOf("E3032") >= 0);

            var res = SoftVersoSDK.Instance.DmcCardCoolReset(CardNo);//复位总线错误
            if (res != 0)
            {
                CMessage.Instance.AddTipsMessage(DeviceMessageTypeEnum.报警消息窗口, $"dmc_soft_reset(复位总线错误) == {res.ToString()}\r\n {SoftVersoSDK.Instance.GetEtherCATErrorInfo(res)}");
                return -1;
            }

            CMessage.Instance.AddTipsMessage(DeviceMessageTypeEnum.报警消息窗口, $"总线复位中... ");

            for (int i = 0; i < 15; i++)
            {
                ushort err = 1;
                res = LTDMC.nmc_get_errcode(CardNo, 2, ref err);
                if (res == 0)
                {
                    if (err == 0)//总线复位正常完成
                    {
                        isOK = true;
                        break;
                    }
                }
                else
                {
                    CMessage.Instance.AddTipsMessage(DeviceMessageTypeEnum.报警消息窗口, $"复位总线错误::{SoftVersoSDK.Instance.GetErrorInfo(res)}");
                    return -3;
                }
                Thread.Sleep(1000);
            }
            if (!isOK)
            {
                CMessage.Instance.AddTipsMessage(DeviceMessageTypeEnum.报警消息窗口, $"总线复位失败！");
                return -3;
            }
            isOK = false;
            for (int i = 0; i < 15; i++)
            {
                res = SoftVersoSDK.Instance.GetCardDownloadConfigfile(CardNo, cardConfigFname);
                if (res == 0)
                {
                    isOK = true;
                    break;
                }
                Thread.Sleep(1000);
            }
            if (!isOK)
            {
                CMessage.Instance.AddTipsMessage(DeviceMessageTypeEnum.报警消息窗口, $"卡 [{HardwareName}] 载入配置文件 [{AppDomain.CurrentDomain.BaseDirectory + cardConfigFname}] 出错！");
                return -4;
            }

            return 0;
        }

        /// <summary>
        /// 关闭控制卡（适用于所有脉冲/总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int CloseCard()
        {
            return SoftVersoSDK.Instance.DmcCardBoardClose();
        }

        public int SetCardCANIOConnectState(int CardNo, int NodeNum, int state, int baud)
        {
            if (IsOffineMode == true)
            {
                NodeNum = 8;
                state = 1;
                return 0;
            }
            throw new NotImplementedException();
        }

        public int GetCardCANIOConnectState(int CardNo, ref ushort NodeNum, ref ushort state)
        {
            if (IsOffineMode == true)
            {
                NodeNum = 8;
                state = 1;
                return 0;
            }
            state = 1;
            //throw new NotImplementedException();
            return 0;
        }


        public int GetCardTotalAxisNum(int CardNo, ref uint TotalAxis)
        {
            //throw new NotImplementedException();
            if (ListCardParam.Count < 1) return 0;
            TotalAxis = ListCardParam[0].CardSuportAxisNum;
            return 0;
        }

        /// <summary>
        /// SDK调用返回值的错误描述
        /// </summary>
        /// <param name="ErrNum"></param>
        /// <returns></returns>
        public string GetErrorInfo(int ErrNum)
        {
            var res = SoftVersoSDK.Instance.GetErrorInfo(ErrNum);
            return res;
        }

        /// <summary>
        /// 取总线错误信息
        /// </summary>
        /// <param name="ErrNum"></param>
        /// <returns></returns>
        public string GetEtherCATErrorInfo(int ErrNum)
        {
            return SoftVersoSDK.Instance.GetEtherCATErrorInfo(ErrNum);
        }

        /// <summary>
        /// 读取 EtherCAT 总线卡状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="channel">EtherCAT 总线端口号，固定为 2</param>
        /// <returns></returns>
        public int GetCardCurrentState(int CardNo, int channel)
        {
            ushort errnum = 1;
            var res = SoftVersoSDK.Instance.NmcGetCardErrcode(CardNo, channel, ref errnum);
            return errnum;
        }

        public int DMCEventData(DMC_CacheMemory_Struct m_Obj)
        {
            throw new NotImplementedException();
        }


        public int ConfigureEMGConfig(int CardNo, ushort[] io_Port, ushort enable, ushort emg_logic, ushort stop_mode, double filter_time)
        {
            return 0;
        }
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
