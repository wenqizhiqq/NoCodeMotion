﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/DMC2210/CardRealization.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WenQiZhi.Domain.MotionCard.Common.DMC2210
{
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
        /// 虚拟卡，方便脱机调试
        /// </summary>
        public bool IsVitualCard { get; set; } = false;

        [DisplayName("多少张卡"), Description("注意指的是同型号多张卡的数量"), Category("卡参数设置")]
        /// <summary>
        /// 同型号多卡的数量
        /// </summary>
        public ushort CardSum { get; set; } = 1;

        [DisplayName("运动卡输出IO的数量"), Description("运动卡输出IO的数量"), Category("卡参数设置")]
        /// <summary>
        /// 运动卡输出IO的数量
        /// </summary>
        public int CardOutputIOSum { get; set; } = 16;

        [DisplayName("运动卡输入IO的数量"), Description("运动卡输入IO的数量"), Category("卡参数设置")]
        /// <summary>
        /// 运动卡输入IO的数量
        /// </summary>
        public int CardInputIOSum { get; set; } = 16;


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
        public SystemSupportMotionCardEnum HardwareName { get; set; } = SystemSupportMotionCardEnum.DMC2210;


        public CardRealization()
        {
            for (int i = 0; i < 16; i++)
            {
                ListCardParam.Add(new CardParamModel
                {
                    CardNo = 0,
                    CardType = 0,
                    CardName = SystemSupportMotionCardEnum.DMC2210.ToString(),

                    CardRemarks = "雷塞DMC2210二轴卡，16IO输出，16IO输出",
                    CardSuportAxisNum = 2,

                    OutportNum = 16,
                    InportNum = 16,
                    ControllerMode = 1,
                    CyclFieldbusTypeeTime = 500,
                    LibVersion = 2101,
                    EtherCATState = 0,
                    FileName = "",
                    Mode = 0,
                    Axes = default(AxisParamModel[])

                });
            }
        }


        /// <summary>
        /// 卡参数列表
        /// </summary>
        //public List<CardParamModel> ListCardParam { get { return _ListCardParam; } set { _ListCardParam = value; } }
        //private List<CardParamModel> _ListCardParam = new List<CardParamModel>();

        public List<CardParamModel> ListCardParam { get; set; } = new List<CardParamModel>();



        private int cardCount = 0;
        /// <summary>
        /// 控制卡初始化函数，返回卡的数量
        /// </summary>
        /// <returns>卡的数量</returns>
        public int InitCard()
        {
            if (IsVitualCard)
            {
                isInit = true;
                return 1;
            }
            int[] card_id = null;
            int card_count = 0;
            var res = DMC2210SDK.Instance.InitCard(ref card_id, ref card_count);
            if (res != 0)
            {
                isInit = false;
                return -1;
            }
            cardCount = card_count;
            for (int i = 0; i < card_count; i++)
            {
                ListCardParam[i].CardNo = card_id[i];
                //_ListCardParam.Add(new CardParamModel
                //{
                //    CardNo = card_id[i],
                //    CardType =0,
                //    CardName = SystemSupportMotionCardEnum.PCI9014.ToString(),

                //    CardRemarks = "PCI9014总线卡，16IO输出，16IO输出",
                //    CardSuportAxisNum = 8,

                //    OutportNum = 4,
                //    InportNum = 4,
                //    ControllerMode = 1,
                //    CyclFieldbusTypeeTime = 500,
                //    LibVersion = 2101,
                //    EtherCATState = 0,
                //    FileName = "",
                //    Mode = 0,
                //    Axes = default(AxisParamModel[])
                //});
            }
            isInit = true;
            return card_count;
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
            if (IsVitualCard)
            {
                isOpen = true;
                return 0;
            }
            //dmc2210不支持OpenCard,所以这里直接赋值缓存的信息
            cardnum = (ushort)cardCount;
            List<ushort> list1 = new List<ushort>();
            for (int i = 0; i < cardnum; i++)
            {
                list1.Add((ushort)i);
            }
            CardID = list1.ToArray();
            isOpen = true;
            return 0;
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
                uint pdriverVer = 0;
                uint plogicver = 0;
                int result = DMC2210SDK.Instance.GetCardVersion(CardID, ref libVer, ref pdriverVer, ref plogicver);
                LibVer = (int)libVer; pDriver_ver = (int)pdriverVer; pLogic_ver = (int)plogicver;
                return result;
            }
            catch (Exception e)
            {
                throw e;
            }

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
        public int SetCardMasterParam(int CardNo, int PortNum, int Baudrate, int NodeCnt, int MasterId)
        {
            return 0;
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
            return 0;
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
            return 0;
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
            return 0;
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
            return 0;
        }

        /// <summary>
        /// 控制卡初始复位
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <returns>错误代码</returns>
        public int CardReset(int CardNo)
        {
            return 0;
        }

        /// <summary>
        ///硬件复位
        /// </summary>
        /// <returns>错误代码</returns>
        public int CardBoardReset()
        {
            return 0;
        }

        /// <summary>
        /// 控制卡热复位
        /// </summary>
        /// <returns>错误代码</returns>
        public int CardSoftReset(ushort CardNo)
        {
            return 0;
        }

        /// <summary>
        /// 控制卡冷复位
        /// </summary>
        /// <returns>错误代码</returns>
        public int CardCoolReset(ushort CardNo)
        {
            return 0;
        }



        /// <summary>
        /// 关闭控制卡
        /// </summary>
        /// <returns>错误代码</returns>
        public int CloseCard()
        {
            int result = DMC2210SDK.Instance.CloseCard();
            return result;
        }

        public int SetCardCANIOConnectState(int CardNo, int NodeNum, int state, int baud)
        {
            throw new NotImplementedException();
        }

        public int GetCardCANIOConnectState(int CardNo, ref ushort NodeNum, ref ushort state)
        {
            //throw new NotImplementedException();
            state = 1;
            return 0;
        }

        public int GetCardTotalAxisNum(int CardNo, ref uint TotalAxis)
        {
            if (ListCardParam.Count < 1) return 0;
            TotalAxis = ListCardParam[0].CardSuportAxisNum;
            return 0;
        }

        public string GetEtherCATErrorInfo(int ErrNum)
        {
            return "";
        }

        public string GetErrorInfo(int ErrNum)
        {
            return DMC2210SDK.Instance.GetErrorInfo(ErrNum);
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

        ///// <summary>
        ///// 初始化卡 
        ///// </summary>
        //public short InitCard()
        //{
        //    return 0;
        //}

        ///// <summary>
        ///// 打开卡 
        ///// </summary>
        //public int OpenCard()
        //{
        //    return 0;
        //}

        ///// <summary>
        ///// 获取卡的动态库版本 
        ///// </summary>
        //public string GetCardLibVersion()
        //{
        //    return "v1.0";
        //}

        ///// <summary>
        ///// 设置卡参数 
        ///// </summary>
        //public short SetCardMasterPara()
        //{
        //    return 0;
        //}

        ///// <summary>
        ///// 获取卡参数
        ///// </summary>
        //public short GetCardMasterPara()
        //{
        //    return 0;
        //}

        ///// <summary>
        ///// 急停
        ///// </summary>
        //public short CardEmgStop()
        //{
        //    return 0;
        //}

        ///// <summary>
        ///// 复位
        ///// </summary>
        //public short CardReset()
        //{
        //    return 0;
        //}

        ///// <summary>
        ///// 卡关闭
        ///// </summary>
        //public short CloseCard()
        //{
        //    return 0;
        //}
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
