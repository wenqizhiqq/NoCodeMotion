﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/HYMC608/CardRealization.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samsun.Domain.MotionCard.Common.HYMC608
{
    public class CardRealization : ICard
    {

        //[DisplayName("运动控制卡的配置文件名字"), Description("运动控制卡的配置文件名字"), Category("卡参数设置")]
        //public string CardConfigFileName { get; set; } = "";


        [DisplayName("运动卡输出IO的数量"), Description("运动卡输出IO的数量"), Category("卡参数设置")]
        /// <summary>
        /// 运动卡输出IO的数量
        /// </summary>
        public int CardOutputIOSum { get; set; } = 16;

        [DisplayName("运动卡输入IO的数量"), Description("运动卡输入IO的数量"), Category("卡参数设置")]
        /// <summary>
        /// 运动卡输入IO的数量
        /// </summary>
        public int CardInputIOSum { get; set; } = 36;

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
        /// 虚拟卡，方便脱机调试
        /// </summary>
        public bool IsVitualCard { get; set; } = false;

        [DisplayName("多少张卡"), Description("注意指的是同型号多张卡的数量"), Category("卡参数设置")]
        /// <summary>
        /// 同型号多卡的数量
        /// </summary>
        public ushort CardSum { get; set; } = 1;


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





        [DisplayName("卡状态"), Description("卡状态，反馈卡是否在线模式"), Category("Online设置"), ReadOnly(true)]
        /// <summary>
        /// 判断是否为离线模式
        /// </summary>
        public bool IsOffineMode { get; set; } = false;

        /// <summary>
        /// 硬件名字,这里用枚举，以方便程序中轮询卡
        /// </summary>
        public SystemSupportMotionCardEnum HardwareName { get; set; } = SystemSupportMotionCardEnum.HYMC608;


        /// <summary>
        /// 卡参数列表
        /// </summary>
        public List<CardParamModel> ListCardParam { get ; set ; } = new List<CardParamModel>();


     
        ~CardRealization()
        {
            //var num2 = MC608SDK.Instance.CloseBoardCard();
        }

        public CardRealization()
        {
            //因为在调用同OpenCard()之前，还不知道电脑里有多少张同型号卡
            //所以预定义16张轴参数表
            //for (int i = 0; i < 16; i++)
            //{
            //    ListCardParam.Add(new CardParamModel()
            //    {
            //        CardName = SystemSupportMotionCardEnum.DMC3000系列.ToString(),
            //        CardRemarks = "Dmc3400A卡，16IO输入，16IO输出",
            //        CardSuportAxisNum = 4,
            //        CardType = 0,
            //        OutportNum = 16,
            //        InportNum = 16,
            //        ControllerMode = 1,
            //        CyclFieldbusTypeeTime = 500,
            //        LibVersion = 2101,
            //        EtherCATState = 0,
            //        FileName = "",
            //        Mode = 0,
            //        Axes = default(AxisParamModel[])
            //    });
            //}
            //CardInputIOSum = 16;
            //CardOutputIOSum = 16;

        }



        /// <summary>
        ///硬件复位
        /// </summary>
        /// <returns>错误代码</returns>
        public int CardBoardReset()
        {
            return MC608SDK.Instance.CardBoardReset();
        }

        /// <summary>
        /// 控制卡冷复位
        /// </summary>
        /// <returns>错误代码</returns>
        public int CardCoolReset(ushort CardNo)
        {
            return -1;
        }

        /// <summary>
        /// 控制卡初始复位
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <returns>错误代码</returns>
        public int CardReset(int CardNo)
        {
            return -1 ;
        }

        /// <summary>
        /// 控制卡热复位
        /// </summary>
        /// <returns>错误代码</returns>
        public int CardSoftReset(ushort CardNo)
        {
            return MC608SDK.Instance.CardSoftReset(CardNo);
        }

        /// <summary>
        /// 获取当前卡的轴数 
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="TotalAxis">返回当前卡的轴数</param>
        /// <returns>错误代码</returns>
        public int GetCardTotalAxisNum(int CardNo, ref UInt32 TotalAxis)
        {
            return MC608SDK.Instance.GetCardTotalAxisNum(CardNo, ref TotalAxis);
        }

        /// <summary>
        /// 关闭控制卡
        /// </summary>
        /// <returns>错误代码</returns>
        public int CloseCard()
        {
            return MC608SDK.Instance.CloseBoardCard();
        }

        /// <summary>
        /// 版本号信息查询，可以查询 API, 驱动程序和逻辑的版本。
        /// </summary>
        /// <param name="axis">卡号 </param>
        /// <param name="pApi_ver">API 版本，其中的 bit0~7 为副版本号， bit8~15 为主版本号； </param>
        /// <param name="pDriver_ver">驱动程序版本，其中的 bit0~7 为副版本号， bit8~15 为主版本号；</param>
        /// <param name="pLogic_ver">逻辑软件版本，其中的 bit0~7 为副版本号， bit8~15 为主版本号； </param>
        /// <returns>   正常返回 0；  出现错误时返回非 0 值；</returns>
        public int GetCardLibVersion(int CardID, ref int LibVer, ref int pDriver_ver, ref int pLogic_ver)
        {
            try
            {
                uint libVer = 0;
                int res = MC608SDK.Instance.GetCardLibVersion(ref libVer);
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
        /// 获取卡参数 
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="PortNum">EtherCAT 端口号，固定为 2</param>
        /// <param name="Baudrate">参数不明等待回应</param>
        /// <param name="NodeCnt">参数不明等待回应</param>
        /// <param name="MasterId">参数不明等待回应</param>
        /// <returns>错误代码</returns>
        public int GetCardMasterParam(int CardNo, int PortNum, ref ushort Baudrate, ref uint NodeCnt, ref ushort MasterId)
        {
            return -1;
        }

        /// <summary>
        /// 读取循环周期
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="FieldbusType">EtherCAT 端口号，固定为 2</param>
        /// <param name="CyclFieldbusTypeeTime">EtherCAT 总线循环周期，单位：us，支持 250/500/1000/2000</param>
        /// <returns>错误代码</returns>
        public int GetCardMasterScanCycleTime(int CardNo, int FieldbusType, ref int CyclFieldbusTypeeTime)
        {
            return -1;
        }

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
            return -1;
        }

        /// <summary>
        /// 设置 CAN 通讯状态
        /// <para>注 意：</para>
        /// <para>1）当关闭运动控制卡时，CAN 通讯不会被自动断开；当再次初始化运动控制卡时，  CAN-IO 通讯依然保持之前的状态；</para>
        /// <para>2）当连接 CAN 通讯时，必须使用 nmc_get_can_state 函数读取 CAN-IO 的通讯状态， 确认 CAN 通讯已正常连接。当连接出现异常时，可再次调用 nmc_set_can_state函数进行连接；</para>
        /// <para>3）设置波特率时需保证控制卡波特率与模块波特率相对应；</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeNum">CAN 节点数，取值范围：1~8</param>
        /// <param name="state">设置通讯状态，0：断开，1：连接</param>
        /// <param name="baud">设置控制卡波特率<para>波特率参数：0,1,2,3,4,5</para><para>对应波特率：1000Kbps，800Kbps,500Kbps,250Kbps,125Kbps,100Kbps</para></param>
        /// <returns></returns>
        public int SetCardCANIOConnectState(int CardNo, int NodeNum, int state, int baud)
        {
            return MC608SDK.Instance.SetExpandIOConnectState/*SetCard_CANIO_ConnectState*/((ushort)CardNo, (ushort)NodeNum, (ushort)state, (ushort)baud);
        }

        /// <summary>
        /// 读取 CAN 通讯状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeNum">返回 CAN 节点数</param>
        /// <param name="state">返回 CAN-IO 通讯状态，0：断开，1：连接，2：异常</param>
        /// <returns>错误代码</returns>
        public int GetCardCANIOConnectState(int CardNo, ref UInt16 NodeNum, ref UInt16 state)
        {
           return  MC608SDK.Instance.GetCard_CANIO_ConnectState(CardNo,ref NodeNum, ref state);
        }

        /// <summary>
        /// 控制卡初始化函数，返回卡的数量
        /// </summary>
        /// <returns>卡的数量</returns>
        public int InitCard()
        {
            //if (IsVitualCard)
            //{
            //    isInit = true;
            //    return 1;
            //}
            //var num2=MC608SDK.Instance.CloseBoardCard();
            var num = MC608SDK.Instance.InitBoardCard();
            if (num>0)
            {
                isInit = true;
                return num;
            }
            isInit = false;
            return -1;
        }

        /// <summary>
        /// 打开卡
        /// </summary>
        /// <param name="cardnum">返回初始化成功的卡数 </param>
        /// <param name="cardtypes">返回控制卡固件类型数组</param>
        /// <param name="CardID">返回控制卡硬件 ID 号数组，卡号按从小到大顺序排列  </param>
        /// <returns>错误代码，返回0表示成功</returns>
        public int OpenCard(ref ushort cardnum, ref uint[] cardtypes, ref ushort[] CardID)
        {
            //if (IsVitualCard)
            //{
            //    isOpen = true;
            //    return 0;
            //}
            //取卡的硬件ID号
            int res = MC608SDK.Instance.GetCardInfList(ref cardnum, ref cardtypes, ref CardID);
            if (res != 0)
            {
                isOpen = false;
                return res;
            }

            var cardConfigPath = AppDomain.CurrentDomain.BaseDirectory + "MotionConfigFile";
            if (!Directory.Exists(cardConfigPath))
            {
                Directory.CreateDirectory(cardConfigPath);
            }
            var configFiles = Directory.GetFiles(cardConfigPath);

            //卡的配置文件数组（可能有多张卡要载入配置文件）
            //var cardConfigFileNameAry = CardConfigFileName.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);


            var cardConfigFname = "";
            for (int i = 0; i < cardnum; i++)
            {
                //ListCardParam[i].CardType = null == cardtypes ? 0 : (int)cardtypes[i];
                //ListCardParam[i].CardNo = null == CardID ? 0 : CardID[i];

                switch (HardwareName)
                {
                    case SystemSupportMotionCardEnum.DMC3000系列:
                        //卡的配置文件由卡参数指定，改为在磁盘目录下搜索指定关键字的文件
                        var cardtype = cardtypes[i];
                        switch (cardtype)
                        {
                            case 13312:
                                cardConfigFname = configFiles.ToList().Find(s => s.IndexOf("3400") >= 0);
                                break;
                            case 14336:
                                cardConfigFname = configFiles.ToList().Find(s => s.IndexOf("3800") >= 0);
                                break;
                            case 15360:
                                cardConfigFname = configFiles.ToList().Find(s => s.IndexOf("3C00") >= 0);
                                break;
                        }


                        int res1 = MC608SDK.Instance.GetCardDownloadConfigfile(CardID[i],
                           /*cardConfigPath + "\\" + */ cardConfigFname /*20210811.ini"*/);
                        if (res1 != 0)
                        {
                            res = i + 1;
                            CMessage.Instance.AddTipsMessage(DeviceMessageTypeEnum.报警消息窗口, $"卡 [{HardwareName}] 载入配置文件 [{AppDomain.CurrentDomain.BaseDirectory + cardConfigFname}] 出错！");
                            break;
                        }
                        break;
                }
            }
            isOpen = true;
            return res;
        }

        /// <summary>
        /// 设置卡参数 
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="PortNum">EtherCAT 端口号，固定为 2</param>
        /// <param name="Baudrate">参数不明等待回应</param>
        /// <param name="NodeCnt">参数不明等待回应</param>
        /// <param name="MasterId">参数不明等待回应</param>
        /// <returns>错误代码</returns>
        public int SetCardMasterParam(int CardID, int PortNum, int Baudrate, int NodeCnt, int MasterId)
        {
            return -1;
        }

        /// <summary>
        /// 设置循环周期 
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="Fieldbustype">  EtherCAT 端口号，固定为 2</param>
        /// <param name="CycleTime">CAT 总线循环周期，单位：us，支持 250/500/1000/</param>
        /// <returns>错误代码</returns>
        public int SetCardMasterScanCycleTime(int CardNo, int FieldbusType, int CycleTime)
        {
            return -1;
        }

        public string GetEtherCATErrorInfo(int ErrNum)
        {
            return "";
        }

        public string GetErrorInfo(int ErrNum)
        {
            return MC608SDK.Instance.GetErrorInfo(ErrNum);
        }

        public int GetCardCurrentState(int CardNo, int channel)
        {
            return 0;
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
