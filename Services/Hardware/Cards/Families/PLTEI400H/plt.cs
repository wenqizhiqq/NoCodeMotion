﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/PLTEI400H/plt.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace WenQiZhi.Domain.MotionCard.Common.PLTEI400H
{
    public class PLT
    {   	
        [DllImport("PLT.dll", EntryPoint = "Plt_CardOpen", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_CardOpen(UInt16 TotalCards, UInt16[] CardNameArray, UInt16[] Section, UInt16[] Host_id);
        /*指令功能：初始化控制卡
        输入参数：  TotalCards	卡的数量（打开卡的数量）
                    CardIdArray	卡号数组参数（根据IP地址第四位进行设置），各卡卡号不能重复，数组元素取值范围[0,11]。
                    Section	    IP地址第三段的号码（需与主机PC的地址一致）数组参数，控制卡出厂默认Section为167，数组元素取值范围[1,254]。
                    Host_id	    IP地址第四段的号码（由串口或指令设置）数组参数,控制卡出厂默认Host_id为120。数组元素取值范围[1,254]。
        输出参数：	无
        返回：      0或错误码*/
        [DllImport("PLT.dll", EntryPoint = "Plt_CardClose", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_CardClose();	
        /*指令功能：关闭控制卡
        输入参数：  无
        输出参数：	无
        返回：      0或错误码*/
        [DllImport("PLT.dll", EntryPoint = "Plt_CardReset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_CardReset();
        /*指令功能：复位控制卡
        输入参数：  无
        输出参数：	无
        返回：      0或错误码*/
        [DllImport("PLT.dll", EntryPoint = "Plt_CardGetVerson", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_CardGetVerson(UInt16 cardid,ref UInt32 verson1,ref UInt32 verson2,ref UInt32 verson3);
        /*指令功能：读取相关版本号
        输入参数：  cardid	卡号，取值范围[0,11]。
        输出参数：	*verson1	FPGA版本号
                    *verson2	固件版本号
                    *verson3	动态链接库版本号
        返回：      0或错误码*/
        [DllImport("PLT.dll", EntryPoint = "Plt_CardReadCommincationState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_CardReadCommincationState(UInt16 cardid, ref UInt16 state);	
        /*指令功能：读取PC机和运动控制卡连接状态
        输入参数：  cardid	卡号，取值范围[0,11]。
        输出参数：	*state	PC机和运动控制卡连接状态 1：连接  0：断开
        返回：      0或错误码*/
        [DllImport("PLT.dll", EntryPoint = "Plt_CardGetCardAxisNum", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_CardGetCardAxisNum(UInt16 cardid, ref UInt16 axisnum);
        /*指令功能：读取卡的轴数
        输入参数：  cardid	卡号，取值范围[0,11]。
        输出参数：  *axisnum	控制卡的轴数
        返回：      0或错误码*/
        [DllImport("PLT.dll", EntryPoint = "Plt_CardUpdataFirmare", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_CardUpdataFirmare(UInt16 cardid, String filename);
        /*指令功能：控制卡固件更新
        输入参数：  cardid	卡号，取值范围[0,11]。
	                *filename	新固件绝对路径。 注意：路径中不能有中文字符
        输出参数：	无
        返回：      0或错误码*/
        [DllImport("PLT.dll", EntryPoint = "Plt_GetCardSerialNum", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_GetCardSerialNum(UInt16 cardid, ref UInt32 SerialNum_0, ref UInt32 SerialNum_1, ref UInt32 SerialNum_2); 
        /*指令功能：读取控制卡96位序列号
        输入参数：  cardid	卡号，取值范围[0,11]。
        输出参数：	SerialNum_0	序列号0-31位
	                SerialNum_1	序列号32-63位
			        SerialNum_2	序列号64-95位

        返回：      0或错误码*/

        //轴相关函数
        //脉冲模式	
        [DllImport("PLT.dll", EntryPoint = "Plt_AxSetPulseOutMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_AxSetPulseOutMode(UInt16 cardid,UInt16 axis,UInt16 mode);	
        /*指令功能：设定脉冲输出模式
        输入参数：  cardid	    卡号，取值范围[0,11]。
	                axis	    轴号，EI400\EI400S取值范围:[0,3];
                                EI800取值范围:[0,7];
                                EIC00取值范围:[0,11];
			        mode	    脉冲输出模式，取值范围：0、2、4、6。各种脉冲模式的具体波形参考编程手册“3.4脉冲模式”一节
        输出参数：	无
        返回：      0或错误码*/
        [DllImport("PLT.dll", EntryPoint = "Plt_AxGetPulseOutMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_AxGetPulseOutMode(UInt16 cardid,UInt16 axis,ref UInt16 mode);	
        /*指令功能：读取脉冲输出模式
        输入参数：  cardid	    卡号，取值范围[0,11]。
	                axis	    轴号，EI400\EI400S取值范围:[0,3];
                                EI800取值范围:[0,7];
                                EIC00取值范围:[0,11];
        输出参数：	*mode	    脉冲输出模式，取值范围：0、2、4、6。各种脉冲模式的具体波形参考编程手册“3.4脉冲模式”一节
        返回：      0或错误码*/
        [DllImport("PLT.dll", EntryPoint = "Plt_AxSetEncoderInMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_AxSetEncoderInMode(UInt16 cardid,UInt16 axis,UInt16 mode);  
        /*指令功能：设定编码器输入计数模式
        输入参数：  cardid	    卡号，取值范围[0,11]。
	                axis	    轴号，EI400\EI400S取值范围:[0,3];
                                EI800取值范围:[0,7];
                                EIC00取值范围:[0,11];
			        mode	    编码器输入计数模式  取值范围:[0,3]  0：脉冲+方向   1：1倍频 2:2倍频  3:4倍频
        输出参数：	无
        返回：      0或错误码*/
        [DllImport("PLT.dll", EntryPoint = "Plt_AxGetEncoderInMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_AxGetEncoderInMode(UInt16 cardid,UInt16 axis,ref UInt16 mode); 
        /*指令功能：读取编码器输入计数模式
        输入参数：  cardid	    卡号，取值范围[0,11]。
	                axis	    轴号，EI400\EI400S取值范围:[0,3];
                                EI800取值范围:[0,7];
                                EIC00取值范围:[0,11];
        输出参数：	*mode	    编码器输入计数模式
        返回：      0或错误码*/
        [DllImport("PLT.dll", EntryPoint = "Plt_AxSetPPU", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_AxSetPPU(UInt16 cardid,UInt16 axis, double PPU);
        /*指令功能：设定脉冲当量
        输入参数：  cardid	    卡号，取值范围[0,11]。
	                axis	    轴号，EI400\EI400S取值范围:[0,3];
                                EI800取值范围:[0,7];
                                EIC00取值范围:[0,11];
			        PPU	脉冲当量值（用户单位对应的脉冲数）
        输出参数：	无
        返回：      0或错误码*/
        [DllImport("PLT.dll", EntryPoint = "Plt_AxGetPPU", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_AxGetPPU(UInt16 cardid,UInt16 axis, ref double PPU);
        /*指令功能：读取脉冲当量
        输入参数：  cardid	    卡号，取值范围[0,11]。
	                axis	    轴号，EI400\EI400S取值范围:[0,3];
                                EI800取值范围:[0,7];
                                EIC00取值范围:[0,11];
        输出参数：	*PPU	    脉冲当量值（用户单位对应的脉冲数）
        返回：      0或错误码*/
        //当前位置
        [DllImport("PLT.dll", EntryPoint = "Plt_AxSetCmmandPosition", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_AxSetCmmandPosition(UInt16 cardid, UInt16 axis, double pos);
        /*指令功能：设置轴的指令位置
        输入参数：  cardid	    卡号，取值范围[0,11]。
	                axis	    轴号，EI400\EI400S取值范围:[0,3];
                                EI800取值范围:[0,7];
                                EIC00取值范围:[0,11];
			        pos	        指令位置值
        输出参数：	无
        返回：      0或错误码*/
        [DllImport("PLT.dll", EntryPoint = "Plt_AxGetCmmandPosition", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_AxGetCmmandPosition(UInt16 cardid,UInt16 axis, ref double pos);
        /*指令功能：查询轴的指令位置
        输入参数：  cardid	    卡号，取值范围[0,11]。
	                axis	    轴号，EI400\EI400S取值范围:[0,3];
                                EI800取值范围:[0,7];
                                EIC00取值范围:[0,11];
        输出参数：	*pos	    指令位置值
        返回：      0或错误码*/
        [DllImport("PLT.dll", EntryPoint = "Plt_AxSetEncoderPosition", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_AxSetEncoderPosition(UInt16 cardid,UInt16 axis, double pos);
        /*指令功能：设置编码器位置
        输入参数：  cardid	    卡号，取值范围[0,11]。
	                axis	    轴号，EI400\EI400S取值范围:[0,3];
                                EI800取值范围:[0,7];
                                EIC00取值范围:[0,11];
			        pos	        编码器设定值
        输出参数：	无
        返回：      0或错误码*/
        [DllImport("PLT.dll", EntryPoint = "Plt_AxGetEncoderPosition", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short Plt_AxGetEncoderPosition(UInt16 cardid,UInt16 axis, ref double pos);
        /*指令功能：读取编码器位置
        输入参数：  cardid	    卡号，取值范围[0,11]。
	                axis	    轴号，EI400\EI400S取值范围:[0,3];
                                EI800取值范围:[0,7];
                                EIC00取值范围:[0,11];
        输出参数：	*pos	    编码器位置
        返回：      0或错误码*/
        /*************************************************************************安全保护***************************************************************/
         public struct struct_el_parms
        {
            public UInt16 pel_enable;//正限位使能；0：正限位禁止；1：正限位使能
            public UInt16 mel_enable;//负限位使能；0：负限位禁止；1：负限位使能
            public UInt16 pel_active_level;//正限位有效电平；0：低电平有效；1：高电平有效
            public UInt16 mel_active_level;//负限位有效电平；0：低电平有效；1：高电平有效
            public UInt16 pel_react;//正限位有效停止方式；0：立即停止；1：减速停止
            public UInt16 mel_react;//正限位有效停止方式；0：立即停止；1：减速停止
        }; //硬限位参数结构体

        public struct struct_sw_el_parms
        {
            public double melpos;//负软限位位置，单位:[ppu]
            public double pelpos;//正软限位位置，单位:[ppu]
            public UInt16 enable;//软限位使能；0：软限位禁止；1：负限位使能
            public UInt16 react;//软限位有效停止方式；0：立即停止；1：减速停止
        };//软限位参数结构体
     [DllImport("PLT.dll", EntryPoint = "Plt_AxSetEl", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
     public static extern short Plt_AxSetEl(UInt16 cardid,UInt16 axis,struct_el_parms elparms); 
     /*指令功能：设置硬件限位
     输入参数：  cardid	    卡号，取值范围[0,11]。
                 axis	    轴号，EI400\EI400S取值范围:[0,3];
                             EI800取值范围:[0,7];
                             EIC00取值范围:[0,11];
                 elparms	    硬限位参数
     输出参数：	无
     返回：      0或错误码*/
     [DllImport("PLT.dll", EntryPoint = "Plt_AxGetEl", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
     public static extern short Plt_AxGetEl(UInt16 cardid,UInt16 axis,ref struct_el_parms elparms);
     /*指令功能：读取硬件限位的设置
     输入参数：  cardid	    卡号，取值范围[0,11]。
                 axis	    轴号，EI400\EI400S取值范围:[0,3];
                             EI800取值范围:[0,7];
                             EIC00取值范围:[0,11];
     输出参数：	*elparms	硬限位参数
     返回：      0或错误码*/
     [DllImport("PLT.dll", EntryPoint = "Plt_AxSetSWEL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
     public static extern short Plt_AxSetSWEL(UInt16 cardid,UInt16 axis,struct_sw_el_parms swelparms);
     /*指令功能：设置软件限位
    输入参数：  cardid	    卡号，取值范围[0,11]。
             axis	    轴号，EI400\EI400S取值范围:[0,3];
                         EI800取值范围:[0,7];
                         EIC00取值范围:[0,11];
             swelparms	软件限位参数
    输出参数：	无
    返回：      0或错误码*/
     [DllImport("PLT.dll", EntryPoint = "Plt_AxGetSWEL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
     public static extern short Plt_AxGetSWEL(UInt16 cardid,UInt16 axis,ref struct_sw_el_parms swelparms);
     /*指令功能：读取软件限位的设置
    输入参数：  cardid	    卡号，取值范围[0,11]。
             axis	    轴号，EI400\EI400S取值范围:[0,3];
                         EI800取值范围:[0,7];
                         EIC00取值范围:[0,11];
    输出参数：	*swelparms	软件限位参数
    返回：      0或错误码*/
/*************************************************************************安全保护***************************************************************/

/*************************************************************************专用接口***************************************************************/
    public struct struct_special_input_parms
    {
        public UInt16 emg_enable;//emg使能开关；0：使能禁止 1：使能有效
        public UInt16 emg_level;//emg有效电平；0：低电平有效；1：高电平有效
        public UInt16 emg_port;//映射为EMG的输入IO口   取值范围：EI400\EI400S:[0,31]，EI800\EIC00:[0,15];
        public UInt16 alm_enable;//alm使能开关；0：使能禁止 1：使能有效
        public UInt16 alm_level;//alm有效电平；0：低电平有效；1：高电平有效
        public double filter_time;//输入信号滤波时间；单位[ms]
        public UInt16 inp_enable;//inp使能开关；0：使能禁止 1：使能有效
        public UInt16 inp_level;//inp有效电平；0：低电平有效；1：高电平有效
    };//

    public struct struct_special_input_status
    {
        public byte alm_status;//alm状态；0:无效；1：有效
        public byte el_pos_status;//正限位状态；0:无效；1：有效
        public byte el_neg_status;//负限位状态；0:无效；1：有效
        public byte swel_pos_status;//正软限位状态；0:无效；1：有效
        public byte swel_neg_status;//负软限位状态；0:无效；1：有效
        public byte emg_status;//emg状态；0:无效；1：有效
        public byte home_status;//原点状态；0:无效；1：有效
        public byte inp_status;//inp状态；0:无效；1：有效
        public byte ez_status;//ez状态；0:无效；1：有效
        public byte rdy_status;//rdy状态；0:无效；1：有效
    };//专用输入状态
    [DllImport("PLT.dll", EntryPoint = "Plt_AxConfigSpecialInputParms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxConfigSpecialInputParms(UInt16 cardid,UInt16 axis,struct_special_input_parms specinput);
    /*指令功能：配置EMG\ERC\INP\ALM等信号和滤波时间
    输入参数：  cardid	    卡号，取值范围[0,11]。
	            axis	    轴号，EI400\EI400S取值范围:[0,3];
                            EI800取值范围:[0,7];
                            EIC00取值范围:[0,11];
			    specinput	专用输入配置
    输出参数：	无
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxReadSpecialInputParms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxReadSpecialInputParms(UInt16 cardid,UInt16 axis,ref struct_special_input_parms specinput);
    /*指令功能：读取EMG\ERC\INP\ALM等信号和滤波时间的配置
    输入参数：  cardid	    卡号，取值范围[0,11]。
	            axis	    轴号，EI400\EI400S取值范围:[0,3];
                            EI800取值范围:[0,7];
                            EIC00取值范围:[0,11];
    输出参数：	*specinput	专用输入配置
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxReadSpecialInputStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxReadSpecialInputStatus(UInt16 cardid,UInt16 axis,ref struct_special_input_status inputstatus);
    /*指令功能：读取轴专用输入信号状态
    输入参数：  cardid	    卡号，取值范围[0,11]。
	            axis	    轴号，EI400\EI400S取值范围:[0,3];
                            EI800取值范围:[0,7];
                            EIC00取值范围:[0,11];
    输出参数：	*inputstatus	专用输入状态
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxSetsvonPort", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxSetsvonPort(UInt16 cardid,UInt16 axis,UInt16 active_level);
    /*指令功能：输出SEVON信号
    输入参数：  cardid	    卡号，取值范围[0,11]。
                axis	    轴号，EI400\EI400S取值范围:[0,3];
                            EI800取值范围:[0,7];
                            EIC00取值范围:[0,11];
                active_level	SERV-ON输出电平值 0:低电平 1：高电平
    输出参数：	无
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxGetsvonPort", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxGetsvonPort(UInt16 cardid,UInt16 axis,ref UInt16 active_level);
    /*指令功能：获取SEVON信号
    输入参数：  cardid	    卡号，取值范围[0,11]。
	            axis	    轴号，EI400\EI400S取值范围:[0,3];
                            EI800取值范围:[0,7];
                            EIC00取值范围:[0,11];
    输出参数：	*active_level	SERV-ON输出电平值 0:低电平 1：高电平
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxGetRdyPort", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxGetRdyPort(UInt16 cardid,UInt16 axis,ref UInt16 active_level);
    /*指令功能：获取RDY信号（此函数保留）
    输入参数：  cardid	    卡号，取值范围[0,11]。
	            axis	    轴号，EI400\EI400S取值范围:[0,3];
                            EI800取值范围:[0,7];
                            EIC00取值范围:[0,11];
    输出参数：	*active_level	RDY信号输入值 0:低电平 1：高电平
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxSetErcPort", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxSetErcPort(UInt16 cardid,UInt16 axis,UInt16 active_level);
    /*指令功能：控制ERC信号输出电平
    输入参数：  cardid	    卡号，取值范围[0,11]。
	            axis	    轴号，EI400\EI400S取值范围:[0,3];
                            EI800取值范围:[0,7];
                            EIC00取值范围:[0,11];
			    active_level	ERC信号输出值	0:低电平 1：高电平
    输出参数：	无
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxGetErcPort", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxGetErcPort(UInt16 cardid,UInt16 axis,ref UInt16 active_level);
    /*指令功能：获取控制ERC信号输出电平
    输入参数：  cardid	    卡号，取值范围[0,11]。
	            axis	    轴号，EI400\EI400S取值范围:[0,3];
                            EI800取值范围:[0,7];
                            EIC00取值范围:[0,11];
    输出参数：	*active_level	ERC信号输出值 0:低电平 1：高电平
    返回：      0或错误码*/
/*************************************************************************专用接口***************************************************************/
/*************************************************************************通用输入输出IO**********************************************************/
    [DllImport("PLT.dll", EntryPoint = "Plt_IoReadInputByBit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_IoReadInputByBit(UInt16 cardid,UInt16 bitno,ref UInt16 active_level); 	
    /*指令功能：读取输入口的状态
    输入参数：  cardid	    卡号，取值范围[0,11]。
                bitno       输入口编号,取值范围：EI400\EI400S:[0,31]，EI800\EIC00:[0,15];
    输出参数：  *active_level 输入口状态 0：低电平  1：高电平
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_IoWriteOutputByBit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_IoWriteOutputByBit(UInt16 cardid,UInt16 bitno,UInt16 active_level); 	
    /*指令功能：设置输出口的状态
    输入参数：  cardid	    卡号，取值范围[0,11]。
                bitno       输出口编号,取值范围：EI400\EI400S:[0,31]，EI800\EIC00:[0,15];
			    active_level 输出口输出状态 0：低电平  1：高电平
    输出参数：  无
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_IoReadOutputByBit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_IoReadOutputByBit(UInt16 cardid,UInt16 bitno,ref UInt16 active_level);  
    /*指令功能：读取输出口的状态
    输入参数：  cardid	    卡号，取值范围[0,11]。
                bitno       输出口编号,取值范围：EI400\EI400S:[0,31]，EI800\EIC00:[0,15];
    输出参数：  *active_level 输出口状态 0：低电平  1：高电平
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_IoReadAllInput", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_IoReadAllInput(UInt16 cardid,ref UInt32 active_level_1,ref UInt32 active_level_2);
    /*指令功能：读取输入端口的值
    输入参数：  cardid	    卡号，取值范围[0,11]。
    输出参数：  *active_level_1	输入口0--31的状态, bit0对应IN0，bit31对应IN31
                                EI400\EI400S: bit0-bit31位都有效；
                                EI800\EIC00: bit0-bit15位有效；
                *active_level_2	输入口32--63的状态, bit0对应IN32，bit31对应IN63（保留）
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_IoReadAllOutput", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_IoReadAllOutput(UInt16 cardid,ref UInt32 active_level);
    /*指令功能：读取输出端口的值
    输入参数：  cardid	                卡号，取值范围[0,11]。
    输出参数：  *active_level           所有输出口0--31的状态, bit0对应OUT0，bit31对应OUT31
                                        EI400\EI400S: bit0-bit31位都有效；
                                       EI800\EIC00: bit0-bit15位有效；
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_IoWriteAllOutput", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_IoWriteAllOutput(UInt16 cardid,UInt32 active_level);
    /*指令功能：设置输出端口的值
    输入参数：  cardid	                卡号，取值范围[0,11]。
               active_level	            所有输出口的输出状态, bit0对应OUT0，bit31对应OUT31
                                        EI400\EI400S: bit0-bit31位都有效；
                                        EI800\EIC00: bit0-bit15位有效；
    输出参数：  无
    返回：      0或错误码*/
/*************************************************************************通用输入输出IO**********************************************************/

/*************************************************************************一维位置比较功能*********************************************************/
     public struct struct_axis_compare_parms
    {
      public UInt16 queueno;//比较器号  范围：0-15
      public UInt16 cmp_axis;//比较轴号 范围：0-7
      public UInt16 enable;//比较使能开关；0：比较禁止；1：比较允许
      public UInt16 cmp_source;//比较源 0：理论位置 1：编码器位置
    };//单轴位置比较配置参数结构体

     public struct struct_axis_get_compare_parms
    {
      public UInt16 cmp_axis;//比较轴号 范围：0-7
      public UInt16 enable;//比较使能开关；0：比较禁止；1：比较允许
      public UInt16 cmp_source;//比较源 0：理论位置 1：编码器位置
    };//单轴位置比较回读配置参数结构体


     public struct struct_axis_compare_datas
    {
      public UInt16 queueno;//比较器号， 范围：0-15
      public double cmpposition; //比较位置，单位：[pulse]
      public UInt16 cmpmethod; //比较方法：0：大于等于；1：小于等于
      public UInt16 reaction;//比较动作;0：设定io号电平取反；1：设定io号输出低电平；2：设定IO号输出高电平；3：设定io号输出设定时间宽度的脉冲；4：设定轴减速停止；5：设定轴立即停止
      public double pulsewidth;//脉冲宽度；单位：[s]
      public UInt16 react_object;//设定io号或者设定轴
    };//单轴比较缓冲区数据
    [DllImport("PLT.dll", EntryPoint = "Plt_CrdcCompareSetParms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
     public static extern short Plt_AxCompareSetParms(UInt16 cardid, struct_axis_compare_parms axcmpparms); 	//配置比较器
    [DllImport("PLT.dll", EntryPoint = "Plt_CrdcCompareSetParms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxCompareGetParms(UInt16 cardid, UInt16 cmpno, ref struct_axis_get_compare_parms axcmpparms);	//读取配置比较器
    [DllImport("PLT.dll", EntryPoint = "Plt_CrdcCompareSetParms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxCompareClearBuf(UInt16 cardid, UInt16 cmpno); 	//清除比较器所有比较点
    [DllImport("PLT.dll", EntryPoint = "Plt_CrdcCompareSetParms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxCompareSetData(UInt16 cardid, struct_axis_compare_datas axcmpdata);//添加比较点
    [DllImport("PLT.dll", EntryPoint = "Plt_CrdcCompareSetParms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxCompareGetData(UInt16 cardid, UInt16 cmpno, ref double cmpposition); 	//读取当前比较点
    [DllImport("PLT.dll", EntryPoint = "Plt_CrdcCompareSetParms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxCompareGetComparedDataNum(UInt16 cardid, UInt16 cmpno, ref long num); 	//查询已经比较过的点
    [DllImport("PLT.dll", EntryPoint = "Plt_CrdcCompareSetParms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxCompareGetBufRemain(UInt16 cardid, UInt16 cmpno, ref long space); 	//查询可以加入的比较点数量
/*************************************************************************一维位置比较功能*********************************************************/
/*************************************************************************一维精确位置比较功能*********************************************************/
    public struct struct_axis_accurate_compare_parms
    {
      public UInt16 queueno; //比较器号 范围：0-3
      public UInt16 cmp_axis;//比较轴号 范围：0-3
      public UInt16 enable;//比较使能开关；0;比较禁止；1：比较允许；
      public UInt16 cmpmethod;//比较方法：0：小于；1：大于;2:缓冲区比较方法
      public UInt16 cmp_source;//比较源 0：理论位置 1：编码器位置
      public UInt16 active_level;//有效电平；0：低电平有效；1：高电平有效
      public double pulsewidth;//脉冲宽度；单位：【s】;范围：100us-10s;此参数只有在cmpmethod为2情况下有效
    };//精确位置比较

    public struct struct_axis_get_accurate_compare_parms
    {
      public UInt16 cmp_axis;//比较轴号 范围：0-7
      public UInt16 enable;//比较使能开关；0;比较禁止；1：比较允许；
      public UInt16 cmpmethod;//比较方法：0：小于；1：大于;2:缓冲区比较方法
      public UInt16 cmp_source;//比较源 0：理论位置 1：编码器位置
      public UInt16 active_level;//有效电平；0：低电平有效；1：高电平有效
      public double pulsewidth;//脉冲宽度；单位：【s】;范围：1us-10s;此参数只有在cmpmethod为2情况下有效
    };//精确位置比较
    [DllImport("PLT.dll", EntryPoint = "Plt_AxAccurateCompareSetParms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxAccurateCompareSetParms(UInt16 cardid, struct_axis_accurate_compare_parms acccmpparms);
    [DllImport("PLT.dll", EntryPoint = "Plt_AxAccurateCompareGetParms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxAccurateCompareGetParms(UInt16 cardid, UInt16 queueno, ref struct_axis_get_accurate_compare_parms acccmpparms);
    [DllImport("PLT.dll", EntryPoint = "Plt_AxAccurateCompareClearBuf", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxAccurateCompareClearBuf(UInt16 cardid, UInt16 cmpno);
    [DllImport("PLT.dll", EntryPoint = "Plt_AxAccurateCompareSetData", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxAccurateCompareSetData(UInt16 cardid, UInt16 cmpno, UInt16 axis, double cmpposition);
    [DllImport("PLT.dll", EntryPoint = "Plt_AxAccurateCompareGetData", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxAccurateCompareGetData(UInt16 cardid, UInt16 cmpno, UInt16 axis, ref double cmpposition);
    [DllImport("PLT.dll", EntryPoint = "Plt_AxAccurateCompareGetComparedDataNum", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxAccurateCompareGetComparedDataNum(UInt16 cardid, UInt16 cmpno, ref long num); 	//查询已经比较过的点
    [DllImport("PLT.dll", EntryPoint = "Plt_AxAccurateCompareGetBufRemain", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxAccurateCompareGetBufRemain(UInt16 cardid, UInt16 cmpno, ref long space); 	//查询可以加入的比较点数量
/*************************************************************************高速位置比较功能*********************************************************/



/*************************************************************************位置锁存功能*********************************************************/
    public struct struct_axis_latch_parms
    {
        public UInt16 active_level;//有效沿；0；上升沿锁存1：下降沿锁存
        public UInt16 latch_method;//锁存方式；0：单次锁存；1：连续锁存
        public UInt16 latch_source;//锁存源；0：锁存理论位置；1：锁存编码器位置；
    };//位置锁存配置
    [DllImport("PLT.dll", EntryPoint = "Plt_AxConfigLatchParms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxConfigLatchParms(UInt16 cardid,  UInt16 axis, struct_axis_latch_parms ltcparms);
    /*指令功能：配置高速锁存参数
    输入参数：  cardid 卡号，取值范围:[0,11]。
                ltc_num	锁存器通道号，取值范围[0,1]。
                axis	轴号，EI400\EI400S取值范围:[0,3];
                        EI800取值范围:[0,7];
                        EIC00取值范围:[0,11];
                ltcparms	锁存参数
    输出参数：  无
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxReadLatchParms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxReadLatchParms(UInt16 cardid, UInt16 axis, ref struct_axis_latch_parms ltcparms);
    /*指令功能：读取高速锁存配置
    输入参数：  cardid 卡号，取值范围:[0,11]。
                ltc_num	锁存器通道号，取值范围[0,1]。
                axis	轴号，EI400\EI400S取值范围:[0,3];
                        EI800取值范围:[0,7];
                        EIC00取值范围:[0,11];	
    输出参数：  *ltcparms	锁存参数
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxGetLatchFlagStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxGetLatchFlagStatus(UInt16 cardid,  UInt16 axis, ref UInt16 latchstatus);
    /*指令功能：读取锁存器标志
    输入参数：  cardid 卡号，取值范围:[0,11]。
                ltc_num	锁存器通道号，取值范围[0,1]。
                axis	轴号，EI400\EI400S取值范围:[0,3];
                        EI800取值范围:[0,7];
                        EIC00取值范围:[0,11];	
    输出参数：  *latchstatus	锁存标志（1:有锁存数据 0：没有锁存数据）
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxClearLatchStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxClearLatchStatus(UInt16 cardid,UInt16 axis);
    /*指令功能：复位锁存器标志
    输入参数：  cardid 卡号，取值范围:[0,11]。
                ltc_num	锁存器通道号，取值范围[0,1]。
                axis	轴号，EI400\EI400S取值范围:[0,3];
                        EI800取值范围:[0,7];
                        EIC00取值范围:[0,11];	
    输出参数：  无
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxGetLacthPosition", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxGetLacthPosition(UInt16 cardid, UInt16 axis, long latch_num, ref double position);
    /*指令功能：复位锁存器标志
    输入参数：  cardid 卡号，取值范围:[0,11]。
                ltc_num	锁存器通道号，取值范围[0,1]。
                axis	轴号，EI400\EI400S取值范围:[0,3];
                        EI800取值范围:[0,7];
                        EIC00取值范围:[0,11];	
    输出参数：  无
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxGetLatchNum", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxGetLatchNum(UInt16 cardid,UInt16 axis,ref Int32 num);
    /*指令功能：查询已经锁存的数据个数
    输入参数：  cardid 卡号，取值范围:[0,11]。
                ltc_num	锁存器通道号，取值范围[0,1]。
                axis	轴号，EI400\EI400S取值范围:[0,3];
                        EI800取值范围:[0,7];
                        EIC00取值范围:[0,11];	
    输出参数：  *num	锁存的个数
    返回：      0或错误码*/
/*************************************************************************位置锁存功能*********************************************************/



/*************************************************************************回零功能*********************************************************/
    //回零模块
    public struct struct_home_config_parms
    {
        public double home_high_vel;//回零高速，单位：【pulse/s】,取值范围（0,4000000】
        public double home_low_vel;//回零高速，单位：【pulse/s】,取值范围（0,4000000】
        public double home_acc;//回零加减速度, 单位：【pulse/s2】,取值范围（0,4000000000】
        public UInt16 home_mode;//0:原点捕获回零;1：EZ锁存回零;2:原点+EZ锁存回零 3：反向找EZ锁存回零 4：一次回零 5：一次回零加反找回零 6：二次回零 7：原点加EZ回零 8：ez回零 9：反向找EZ回零
        public UInt16 org_level;//原点有效电平，0：低电平有效；1：高电平有效
        public UInt16 org_ltc_source;//原点锁存源，0：理论位置 1：编码器位置
        public UInt16 ez_level;//ez有效电平，0：低电平有效；1：高电平有效
        public UInt16 ez_ltc_source;//ez锁存源，0：理论位置 1：编码器位置
        public UInt16 org_ltc_level;//0：上升沿锁存原点  1：下降沿锁存原点
        public UInt16 ez_ltc_level;//0：上升沿锁存ez 1：下降沿锁存ez
    };//回零模块配置参数
    [DllImport("PLT.dll", EntryPoint = "Plt_AxConfigHomeParms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxConfigHomeParms(UInt16 cardid,UInt16 axis,struct_home_config_parms homeparms);
    /*指令功能：设置回零参数
    输入参数：  cardid 卡号，取值范围:[0,11]。
			    axis	轴号，EI400\EI400S取值范围:[0,3];
                        EI800取值范围:[0,7];
                        EIC00取值范围:[0,11];	
			    homeparms	回零参数
    输出参数：  无
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxReadHomeParms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxReadHomeParms(UInt16 cardid,UInt16 axis,ref struct_home_config_parms homeparms);
    /*指令功能：读取回零参数
    输入参数：  cardid 卡号，取值范围:[0,11]。
			    axis	轴号，EI400\EI400S取值范围:[0,3];
                        EI800取值范围:[0,7];
                        EIC00取值范围:[0,11];			
    输出参数：  *homeparms	回零参数
    返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxHomeMove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxHomeMove(UInt16 cardid,UInt16 axis,UInt16 homedir);
    /*指令功能：启动回零运动
    输入参数：  cardid 卡号，取值范围:[0,11]。
			    axis	轴号，EI400\EI400S取值范围:[0,3];
                        EI800取值范围:[0,7];
                        EIC00取值范围:[0,11];	
			    homedir	回零方向（0：负方向，1：正方向）
    输出参数：  无
    返回：      0或错误码*/

/*************************************************************************回零功能*********************************************************/

/*************************************************************************JOG和点位运动功能*********************************************************/
    public struct struct_vel_plan_parms
    {
        public double start_vel;     //起始速度，单位：【ppu/s】 ;取值范围【0,4000000/PPU】
        public double max_vel;       //最大速度，单位：【ppu/s】;取值范围(0,4000000/PPU】
        public double end_vel;       //停止速度，单位：【ppu/s】;取值范围【0,4000000/PPU】
        public double acc;           //加速度,单位：[ppu/s2];取值范围(0,4000000000/ppu]
        public double dec;           //减速度,单位：[ppus/s2];取值范围(0,4000000000/ppu]
        public double smooth_factor; //S段比例因子,范围：[0-1]
    };//速度规划参数
    //速度参数设置：
    [DllImport("PLT.dll", EntryPoint = "Plt_AxSetvelParms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxSetvelParms(UInt16 cardid,UInt16 axis,struct_vel_plan_parms axisplanparms);
    /*指令功能：设置速度曲线的规划参数
      输入参数：cardid 卡号，取值范围:[0,11]。
			    axis	轴号，EI400\EI400S取值范围:[0,3];
                        EI800取值范围:[0,7];
                        EIC00取值范围:[0,11];
		        axisplanparms	速度规划参数
      输出参数：无
      返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxGetvelParms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxGetvelParms(UInt16 cardid,UInt16 axis,ref struct_vel_plan_parms axisplanparms);
    /*指令功能：设置速度曲线规划参数
      输入参数：cardid 卡号，取值范围:[0,11]。
			    axis	轴号，EI400\EI400S取值范围:[0,3];
                        EI800取值范围:[0,7];
                        EIC00取值范围:[0,11];	    
      输出参数：*axisplanparms	速度规划参数
      返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxMoveRel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxMoveRel(UInt16 cardid,UInt16 axis,double dist);
    /*指令功能：开始点位运动（运动一段dist距离）
      输入参数：cardid 卡号，取值范围:[0,11]。
                axis	轴号，EI400\EI400S取值范围:[0,3];
                        EI800取值范围:[0,7];
                        EIC00取值范围:[0,11];
                dist	点位运动的距离，单位：PPU dist取值范围：【2147483640/ppu,-2147483640/ppu】 注意：当dist加上当前寄存器值超出范围【2147483640/ppu,-2147483640/ppu】，会报错
      输出参数：*axisplanparms	速度规划参数
      返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxMoveAbs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxMoveAbs(UInt16 cardid,UInt16 axis,double position);
    /*指令功能：开始点位运动（运动到position位置）
      输入参数：cardid 卡号，取值范围:[0,11]。
                axis	轴号，EI400\EI400S取值范围:[0,3];
                        EI800取值范围:[0,7];
                        EIC00取值范围:[0,11];
                position	点位运动的目标位置，单位：PPU position取值范围：【2147483640/ppu,-2147483640/ppu】
      输出参数：*axisplanparms	速度规划参数
      返回：      0或错误码*/	
	[DllImport("PLT.dll", EntryPoint = "Plt_AxMoveVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxMoveVel(UInt16 cardid,UInt16 axis,UInt16 direction);	//指定轴做连续运动
    /*指令功能：启动JOG运动
  输入参数：cardid 卡号，取值范围:[0,11]。
			axis	轴号，EI400\EI400S取值范围:[0,3];
                    EI800取值范围:[0,7];
                    EIC00取值范围:[0,11];
			direction	JOG运动的方向，0：负方向，1：正方向.指令位置增加的方向为正方向，相反的为负方向。
  输出参数：无
  返回：      0或错误码*/
  //在线变位/变速
[DllImport("PLT.dll", EntryPoint = "Plt_AxChangeTartetPos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxChangeTartetPos(UInt16 cardid, UInt16 axis, double position);//position取值范围：【2147483640/ppu,-2147483640/ppu】
[DllImport("PLT.dll", EntryPoint = "Plt_AxChangeVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short Plt_AxChangeVel(UInt16 cardid, UInt16 axis, double maxvel); //maxvel取值范围：[-4000000/PPU,4000000/PPU]
/*************************************************************************JOG和点位运动功能*********************************************************/
/*************************************************************************停止函数*********************************************************/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxMotionStop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxMotionStop(UInt16 cardid, UInt16 axis, UInt16 stop_mode);
    /*指令功能：停止指定轴
      输入参数：cardid	卡号，取值范围:[0,11]。
                axis	轴号，EI400\EI400S取值范围:[0,3];EI800取值范围:[0,7];EIC00取值范围:[0,11];
                stop_mode	停止模式  0：减速停止 1：立即停止
      输出参数：无
      返回：      0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_CardMotionEmgStop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_CardMotionEmgStop(UInt16 cardid);
    /*指令功能：停止所有轴
      输入参数：cardid	卡号，取值范围:[0,11]。
      输出参数：无
      返回：      0或错误码*/
/*************************************************************************停止函数*********************************************************/
/*************************************************************************状态监测*********************************************************/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxGetMotionStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxGetMotionStatus(UInt16 cardid, UInt16 axis, ref UInt16 motionstatus);	//读取指定轴的运动状态
    /*指令功能：读取指定轴的运动状态
输入参数：cardid	卡号，取值范围:[0,11]。
            axis	轴号，EI400\EI400S取值范围:[0,3];
                        EI800取值范围:[0,7];
                        EIC00取值范围:[0,11];
输出参数：*motionstatus	轴运动状态（0：运动中，1：停止）
返回值：0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxGetStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxGetStatus(UInt16 cardid, UInt16 axis, ref UInt16 mode, ref UInt16 stopreason);//1：点位 2:JOG 3：回零 4：手轮 5：连续插补
    /*指令功能：获取轴运动模式及停止原因
    输入参数：cardid	卡号，取值范围:[0,11]。
            axis	轴号，EI400\EI400S取值范围:[0,3];
                        EI800取值范围:[0,7];
                        EIC00取值范围:[0,11];
    输出参数：*status	轴运动模式（0：空闲，1：点位运动，2：JOG运动，3：回零运动，4：手轮运动，5：连续插补运动）
            *stopreason 停止原因
    返回值：0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxGetMotionSpeed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxGetMotionSpeed(UInt16 cardid, UInt16 axis, ref double motionspeed);//读取各轴速度
    /*指令功能：读取指定轴的运动速度
    输入参数：cardid	卡号，取值范围:[0,11]。
            axis	轴号，EI400\EI400S取值范围:[0,3];
                        EI800取值范围:[0,7];
                        EIC00取值范围:[0,11];
    输出参数：*motionspeed	速度值，单位:PPU/s
    返回值：0或错误码*/
    [DllImport("PLT.dll", EntryPoint = "Plt_AxClearStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern short Plt_AxClearStatus(UInt16 cardid, UInt16 axis);//清除停止原因
    /*指令功能：清除停止原因
    输入参数：cardid	卡号，取值范围:[0,11]。
            axis	轴号，EI400\EI400S取值范围:[0,3];
                        EI800取值范围:[0,7];
                        EIC00取值范围:[0,11];
    输出参数：无
    返回值：0或错误码*/
/*************************************************************************物料分选功能相关函数*********************************************************/
public struct struct_sort_config_parms
{
    public UInt16 camera_count;  //相机数量，有效范围：[1,6]
    public UInt32 camera_time;   //相机触发保持时间，单位为250us
    public byte camera_reverse;//相机触发电平 0：输出低电平  1：输出高电平
    public UInt16 blow_count;   //吹气装置数量，有效范围：[1,4]
    public  UInt32 blow_time;    //吹气装置触发保持时间，单位为250us
    public byte blow_reverse;  //吹气装置触发电平 0：输出低电平  1：输出高电平
    public byte check_logic;   //检测装置有效沿   0：下降沿有效  1：上升沿有效
    public UInt32 width_max;    //工件最大宽度，单位：【pulse】  
    public UInt32 width_min;    //工件最小宽度，单位：【pulse】
    public UInt32 distance_min;  //工件最小脉冲间距 ，单位：【pulse】  
    public UInt32 time_min;      //工件最小时间间距 ，单位：【250us】
};//分选配置参数

public struct struct_sort_config_parms_1
{
	public UInt16 camera_count;       //相机数量
	public UInt32 camera_time;        //相机触发保持时间，单位为250us
	public Byte camera_reverse;      //相机触发电平 0：输出低电平  1：输出高电平
	public UInt16 blow_count;         //吹气装置数量
    public Byte blow_reverse;        //吹气装置触发电平 0：输出低电平  1：输出高电平
    public Byte check_logic;         //检测装置有效沿   1：下降沿有效  0：上升沿有效
	public UInt32 width_max;         //工件最大宽度，单位：【pulse】
	public UInt32 width_min;         //工件最小宽度，单位：【pulse】
	public UInt32 distance_min;      //工件最小脉冲间距 ，单位：【pulse】
	public UInt32 time_min;          //工件最小时间间距 ，单位：【500us】
	public UInt16 buffer_pos_mode;   //0:缓冲区保存元件中心位置  1：缓冲区保存锁存位置
}//分选配置参数

public struct struct_sort_config_parms_2
{
	public UInt16 camera_count;       //相机数量
	public UInt32 camera_time;        //相机触发保持时间，单位为250us
    public Byte camera_reverse;      //相机触发电平 0：输出低电平  1：输出高电平
	public UInt16 blow_count;         //吹气装置数量
    public Byte blow_reverse;        //吹气装置触发电平 0：输出低电平  1：输出高电平
    public Byte check_logic;         //检测装置有效沿   1：下降沿有效  0：上升沿有效
	public UInt32 width_max;         //工件最大宽度，单位：【pulse】  //工件之间的间距
	public UInt32 width_min;         //工件最小宽度，单位：【pulse】  // 第一颗料
	public UInt32 distance_min;      //工件最小脉冲间距 ，单位：【pulse】
	public UInt32 time_min;          //工件最小时间间距 ，单位：【500us】
	public UInt16 buffer_pos_mode;   //0:缓冲区保存元件中心位置  1：缓冲区保存锁存位置
	public UInt16 latch_source;      //0：脉冲计数  1：编码器计数
}//分选配置参数  ----废弃不用的结构体




public struct struct_sort_status
{
    public UInt32 command_blow_count;        //发送吹气指令的数量
    public UInt32 piece_cross_camera_count;  //穿过所有相机的工件数量
    public UInt32 piece_find_count;          //已过检测点的工件数量
};//分选状态
 [DllImport("PLT.dll", EntryPoint = "Plt_SortCameraBlowConfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SortCameraBlowConfig(UInt16 cardid,struct_sort_config_parms sort_config_params, Int32[] camera_pos, Int32[] blow_pos);
//camera_pos;  //相机相对于检测装置的偏移位置,单位：脉冲// 数组最大维数为6
//blow_pos     吹气装置相对于检测装置的偏移位置,单位：脉冲 数组最大维数为6
 [DllImport("PLT.dll", EntryPoint = "Plt_get_SortCameraBlowConfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_get_SortCameraBlowConfig(UInt16 cardid,ref struct_sort_config_parms sort_config_params, Int32[] camera_pos, Int32[] blow_pos);

 [DllImport("PLT.dll", EntryPoint = "Plt_SortCameraBlowConfig_1", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SortCameraBlowConfig_1(UInt16 cardid,struct_sort_config_parms_1 sort_config_params, Int32[] camera_pos, Int32[] blow_pos,UInt32[] blow_time);

 [DllImport("PLT.dll", EntryPoint = "Plt_get_SortCameraBlowConfig_1", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_get_SortCameraBlowConfig_1(UInt16 cardid,ref struct_sort_config_parms_1 sort_config_params, Int32[] camera_pos, Int32[] blow_pos,UInt32[] blow_time);



 [DllImport("PLT.dll", EntryPoint = "Plt_SetBlow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetBlow(UInt16 cardid,UInt16 blow_num);
 [DllImport("PLT.dll", EntryPoint = "Plt_StartSort", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_StartSort(UInt16 cardid);
 [DllImport("PLT.dll", EntryPoint = "Plt_ClearSort", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_ClearSort(UInt16 cardid);
 [DllImport("PLT.dll", EntryPoint = "Plt_SetCameraTriggerCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetCameraTriggerCount(UInt16 cardid,UInt16 camera_num,UInt32 camera_trigger_count);
 [DllImport("PLT.dll", EntryPoint = "Plt_GetCameraTriggerCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetCameraTriggerCount(UInt16 cardid,UInt16 camera_num, UInt32[] camera_trigger_count,UInt16 count);
 [DllImport("PLT.dll", EntryPoint = "Plt_SetBlowTriggerCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetBlowTriggerCount(UInt16 cardid,UInt16 blow_num,UInt32 blow_trigger_count);
 [DllImport("PLT.dll", EntryPoint = "Plt_GetBlowTriggerCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetBlowTriggerCount(UInt16 cardid,UInt16 blow_num, UInt32[] blow_trigger_count,UInt16 count);
/**************************************************************增加OK误吹和NG误吹功能20191202.1******************************************************************/
 [DllImport("PLT.dll", EntryPoint = "Plt_SetWrongOKTriggerCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetWrongOKTriggerCount(UInt16 cardid,UInt32 wrong_trigger_count);
/*功能描述：设置OK误吹比较器已经比较个数初始值*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetWrongOKTriggerCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetWrongOKTriggerCount(UInt16 cardid,ref UInt32 wrong_trigger_count);
/*功能描述：返回OK误吹比较器已经比较个数初始值*/
 [DllImport("PLT.dll", EntryPoint = "Plt_SetWrongNGTriggerCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetWrongNGTriggerCount(UInt16 cardid,UInt32 wrong_trigger_count);
/*功能描述：设置NG误吹比较器已经比较个数初始值*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetWrongNGTriggerCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetWrongNGTriggerCount(UInt16 cardid,ref UInt32 wrong_trigger_count);
/*功能描述：返回NG误吹比较器已经比较个数初始值*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetWrongOKCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetWrongOKCount(UInt16 cardid,ref UInt32 wrong_count);
/*功能描述：返回OK误吹个数统计值*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetWrongNGCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetWrongNGCount(UInt16 cardid,ref UInt32 wrong_count);
/*功能描述：返回NG误吹个数统计值*/
 [DllImport("PLT.dll", EntryPoint = "Plt_SetWrongOKPos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetWrongOKPos(UInt16 cardid,UInt32 pos);
/*功能描述：设置传感器位置相对于检测装置的偏移位置,单位：脉冲*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetWrongOKPos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetWrongOKPos(UInt16 cardid,ref UInt32 pos);
/*功能描述：返回传感器位置相对于检测装置的偏移位置,单位：脉冲*/
/**************************************************************增加OK误吹和NG误吹功能20191202.1******************************************************************/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetSortStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetSortStatus(UInt16 cardid,ref struct_sort_status sort_status);
 [DllImport("PLT.dll", EntryPoint = "Plt_GetSortDistance", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetSortDistance(UInt16 cardid,ref Int32 distance);
 [DllImport("PLT.dll", EntryPoint = "Plt_GetWorkpieceLength", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetWorkpieceLength(UInt16 cardid,ref Int32 length);
 [DllImport("PLT.dll", EntryPoint = "Plt_SetSortOutPut", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetSortOutPut(UInt16 cardid,UInt16 io_num,byte logic,UInt32 hold_time);
 [DllImport("PLT.dll", EntryPoint = "Plt_SetBlowProtectPos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetBlowProtectPos(UInt16 cardid,Int32 protect_pos);
/*
功能描述：设置吹气保护距离，此距离为相对于分选位置锁存的距离
输入参数：cardid 卡号
          protect_pos 距离
输出参数：无*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetBlowProtectPos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetBlowProtectPos(UInt16 cardid, ref Int32 protect_pos);
/*
功能描述：读取吹气保护距离，此距离为相对于分选位置锁存的距离
输入参数：cardid 卡号
          *protect_pos 距离
输出参数：无*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetIntPos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetIntPos(UInt16 cardid, ref Int32 enc_pos, ref Int32 cmp_pos);
/*
功能描述：读取中断产生时编码器位置和比较缓冲区位置
输入参数：cardid 卡号
          *enc_pos 编码器位置  
		  *cmp_pos 比较缓冲区位置
输出参数：无*/
 [DllImport("PLT.dll", EntryPoint = "Plt_SetBufferDelayTime", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetBufferDelayTime(UInt16 cardid, double delay_time);
/*功能描述：设置检测到元件后压缓冲区的延时时间
  输入参数：cardid 卡号
            delay_time 延时时间  单位：s  
  输出参数：无
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetBufferDelayTime", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetBufferDelayTime(UInt16 cardid, ref double delay_time);
/*功能描述：读取检测到元件后压缓冲区的延时时间
  输入参数：cardid 卡号
  输出参数：delay_time
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetAbnormalWorkpieceCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetAbnormalWorkpieceCount(UInt16 cardid, ref UInt32 count);
/*功能描述：读取由于距离太近等导致的异常元件计数值
  输入参数：cardid 卡号
  输出参数：count
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetWorkPieceLatchPosition", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetWorkPieceLatchPosition(UInt16 cardid, ref Int32 pos);
/*功能描述：读取元件锁存位置值
  输入参数：cardid 卡号
  输出参数：pos
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetWorkPiecCentralPosition", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetWorkPiecCentralPosition(UInt16 cardid, ref Int32 pos);
/*功能描述：读取元件中心点位置值(锁存边沿+元件长度的一半)
  输入参数：cardid 卡号
  输出参数：pos
*/

 [DllImport("PLT.dll", EntryPoint = "Plt_SetCameraOutMap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetCameraOutMap(UInt16 cardid, UInt16 cameracount, Int16[] camera_out_map);
/*
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetCameraOutMap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetCameraOutMap(UInt16 cardid, ref UInt16 cameracount, Int16[] camera_out_map);
/*
*/

 [DllImport("PLT.dll", EntryPoint = "Plt_SetBlowOutMap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetBlowOutMap(UInt16 cardid, UInt16 blowcount, Int16[] blow_out_map);
/*
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetBlowOutMap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetBlowOutMap(UInt16 cardid, ref UInt16 blowcount, Int16[] blow_out_map);
/*
*/

 [DllImport("PLT.dll", EntryPoint = "Plt_SetCameraOutPos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetCameraOutPos(UInt16 cardid, ref UInt16 camera_index, Int32 camera_pos);
/*
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetCameraOutPos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetCameraOutPos(UInt16 cardid, UInt16 camera_index,ref Int32 camera_pos);
/*
*/

 [DllImport("PLT.dll", EntryPoint = "Plt_SetBlowOutPos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetBlowOutPos(UInt16 cardid, UInt16 blow_index, Int32 blow_pos);
/*
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetBlowOutMPos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetBlowOutMPos(UInt16 cardid, UInt16 blow_index,ref Int32 blow_pos);
/*
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetBlowStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetBlowStatus(UInt16 cardid, ref UInt32 blow_more, ref UInt32 blow_little, ref UInt32 blow_cmd);
/*
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_SetBlowOutTime", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetBlowOutTime(UInt16 cardid,UInt16 blow_index,UInt32 blow_time);
 [DllImport("PLT.dll", EntryPoint = "Plt_GetBlowOutTime", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetBlowOutTime(UInt16 cardid, UInt16 blow_index, ref UInt32 blow_time);
 [DllImport("PLT.dll", EntryPoint = "Plt_GeLatchRepeatPos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern  short Plt_GeLatchRepeatPos(UInt16 cardid,ref UInt16 ltc_state,ref UInt16 ltc_cnt,Int32[] ltc_pos);

 [DllImport("PLT.dll", EntryPoint = "Plt_SetBlowOutTime_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetBlowOutTime_e(UInt16 cardid, UInt16 blow_index, UInt32 blow_time);
 [DllImport("PLT.dll", EntryPoint = "Plt_GetBlowOutTime_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetBlowOutTime_e(UInt16 cardid, UInt16 blow_index, ref UInt32 blow_time);
 [DllImport("PLT.dll", EntryPoint = "Plt_GeLatchRepeatPos_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GeLatchRepeatPos_e(UInt16 cardid, ref UInt16 ltc_state, ref UInt16 ltc_cnt, Int32[] ltc_pos);


/*功能描述：设置吹气保持模式 blow_mode=0：吹气保持设定时间  blow_mode=1 吹气保持设定编码器计数个数*/
 [DllImport("PLT.dll", EntryPoint = "Plt_SetBlowMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetBlowMode(UInt16 cardid, UInt16 blow_num,UInt16 blow_mode);

/*功能描述：读取吹气保持模式 blow_mode=0：吹气保持设定时间  blow_mode=1 吹气保持设定编码器计数个数*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetBlowMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetBlowMode(UInt16 cardid, UInt16 blow_num, ref UInt16 blow_mode);

/*功能描述：设置OK料统计模式  mode=0：一个OK料盒计数没限制，mode=1：两个OK料盒，当其中一个OK料盒计数满后，切换到另外一个料盒 mode=2：一个OK料盒 计数满之后暂停*/
 [DllImport("PLT.dll", EntryPoint = "Plt_SetOKPiecesCountMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetOKPiecesCountMode(UInt16 cardid, UInt16 mode, UInt32 max_count);

/*功能描述：读取OK料统计模式  mode=0：一个OK料盒计数没限制，mode=1：两个OK料盒，当其中一个OK料盒计数满后，切换到另外一个料盒 mode=2：一个OK料盒 计数满之后暂停*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetOKPiecesCountMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetOKPiecesCountMode(UInt16 cardid, ref UInt16 mode, ref UInt32 max_count);

/*功能描述：转盘转一圈编码器计数*/
 [DllImport("PLT.dll", EntryPoint = "Plt_SetOneCycleMaxCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetOneCycleMaxCount(UInt16 cardid,  UInt32 max_count);

/*功能描述：转盘转一圈编码器计数*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetOneCycleMaxCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetOneCycleMaxCount(UInt16 cardid,   ref UInt32 max_count);

/*功能描述：读取转盘当前角度*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetCurAngle", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetCurAngle(UInt16 cardid,   ref float max_count);
 
 /*功能描述：读取缓冲区元件中心点位置值(锁存边沿+元件长度的一半)*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetWorkPiecCentralBufferPosition", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetWorkPiecCentralBufferPosition(UInt16 cardid, UInt16 pos_num, ref UInt16 act_pos_num, Int32[] pos);

 [DllImport("PLT.dll", EntryPoint = "Plt_set_pre_sort_blow_parms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_set_pre_sort_blow_parms(UInt16 cardid, UInt32 pre_blow_pos, UInt32 pre_blow_index, UInt32 pre_blow_time);
 /*功能描述：配置预分选吹气参数
   输入参数：cardid 卡号
             pre_blow_pos 吹气口到第一个锁存位置的距离
             pre_blow_index 吹气口号
             pre_blow_time  吹气时间  单位：500us
 */
 [DllImport("PLT.dll", EntryPoint = "Plt_set_pre_sort_blow_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_set_pre_sort_blow_enable(UInt16 cardid, UInt16 pre_sort_enable);
 /*功能描述：启动关闭-预分选或二次定位功能
   输入参数：cardid 卡号
             pre_sort_enable  0：关闭预分选和二次定位功能   1：开启预分选功能   2：开启二次定位功能
 */
 [DllImport("PLT.dll", EntryPoint = "Plt_get_pre_sort_blow_parms", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_get_pre_sort_blow_parms(UInt16 cardid, ref UInt32 pre_blow_pos, ref UInt32 pre_blow_index, ref UInt32 pre_blow_time);
 [DllImport("PLT.dll", EntryPoint = "Plt_get_pre_sort_blow_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_get_pre_sort_blow_enable(UInt16 cardid, ref UInt16 pre_sort_enable);
 [DllImport("PLT.dll", EntryPoint = "Plt_set_first_group_camera_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_set_first_group_camera_num(UInt16 cardid, UInt16 camera_num);
 /*功能描述：设置二次定位分配到第一组锁存的相机个数
   输入参数：cardid 卡号
            camera_num  相机个数*/
 [DllImport("PLT.dll", EntryPoint = "Plt_get_first_group_camera_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_get_first_group_camera_num(UInt16 cardid, ref UInt16 camera_num);
 [DllImport("PLT.dll", EntryPoint = "Plt_get_pre_sort_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_get_pre_sort_state(UInt16 cardid, ref UInt32 latch_num, ref UInt32 blow_num);
 /*功能描述：读取预分选状态
   输入参数：cardid 卡号
  输出参数   latch_num  已经锁存元件个数
             blow_num   已经吹气元件个数
 */
 [DllImport("PLT.dll", EntryPoint = "Plt_get_first_group_latch_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_get_first_group_latch_num(UInt16 cardid, ref UInt32 latch_num);
 /*功能描述：读取二次定位第一组分选已锁存元件个数
   输入参数：cardid 卡号
  输出参数   latch_num  已经锁存元件个数
 */
[DllImport("PLT.dll", EntryPoint = "Plt_SetBlowTriggerGrandTotalCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetBlowTriggerGrandTotalCount(UInt16 cardid, UInt16 blow_id, UInt32 blow_trigger_count);
/*功能描述：设置累计吹气计数初值
  输入参数：cardid 卡号
            blow_id 吹气口序号 范围[0,31] 
			blow_trigger_count 初值  当前版本只支持初值为0
  输出参数：无
*/
[DllImport("PLT.dll", EntryPoint = "Plt_GetBlowTriggerGrandTotalCount", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short Plt_GetBlowTriggerGrandTotalCount(UInt16 cardid, UInt16 blow_id, UInt32[] blow_trigger_count, UInt16 count);
/*功能描述：设置累计吹气计数初值
  输入参数：cardid 卡号
            blow_id 吹气口序号 范围[0,31] 
			count 一次连续获得的吹气口个数
  输出参数：blow_trigger_count 吹气口计数
*/
/*************************************************************************物料分选功能相关函数*********************************************************/

/*************************************************************************第二组物料分选功能相关函数*0********************************************************/

public struct struct_sort_config_parms_e
{
    public UInt16 camera_count;  //相机数量，有效范围：[1,6]
    public UInt32 camera_time;   //相机触发保持时间，单位为250us
    public byte camera_reverse;//相机触发电平 0：输出低电平  1：输出高电平
    public UInt16 blow_count;   //吹气装置数量，有效范围：[1,4]
    public  UInt32 blow_time;    //吹气装置触发保持时间，单位为250us
    public byte blow_reverse;  //吹气装置触发电平 0：输出低电平  1：输出高电平
    public byte check_logic;   //检测装置有效沿   1：下降沿有效  0：上升沿有效
    public UInt32 width_max;    //工件最大宽度，单位：【pulse】
    public UInt32 width_min;    //工件最小宽度，单位：【pulse】
    public UInt32 distance_min;  //工件最小脉冲间距 ，单位：【pulse】
    public UInt32 time_min;      //工件最小时间间距 ，单位：【250us】
};//分选配置参数



public struct struct_sort_status_e
{
    public UInt32 command_blow_count;        //发送吹气指令的数量
    public UInt32 piece_cross_camera_count;  //穿过所有相机的工件数量
    public UInt32 piece_find_count;          //已过检测点的工件数量
};//分选状态


//camera_pos;  //相机相对于检测装置的偏移位置,单位：脉冲// 数组最大维数为6
//blow_pos     吹气装置相对于检测装置的偏移位置,单位：脉冲 数组最大维数为6
[DllImport("PLT.dll", EntryPoint = "Plt_SortCameraBlowConfig_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SortCameraBlowConfig_e(UInt16 cardid,struct_sort_config_parms_e sort_config_params, Int32[] camera_pos, Int32[] blow_pos);

[DllImport("PLT.dll", EntryPoint = "Plt_get_SortCameraBlowConfig_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_get_SortCameraBlowConfig_e(UInt16 cardid,ref struct_sort_config_parms_e sort_config_params, Int32[] camera_pos, Int32[] blow_pos);

/*功能描述：设置吹气保持模式 blow_mode=0：吹气保持设定时间  blow_mode=1 吹气保持设定编码器计数个数*/
[DllImport("PLT.dll", EntryPoint = "Plt_SetBlowMode_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetBlowMode_e(UInt16 cardid, UInt16 blow_num, UInt16 blow_mode);

/*功能描述：读取吹气保持模式 blow_mode=0：吹气保持设定时间  blow_mode=1 吹气保持设定编码器计数个数*/
[DllImport("PLT.dll", EntryPoint = "Plt_GetBlowMode_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetBlowMode_e(UInt16 cardid, UInt16 blow_num, ref  UInt16 blow_mode);

/*功能描述：设置OK料统计模式  mode=0：一个OK料盒计数没限制，mode=1：两个OK料盒，当其中一个OK料盒计数满后，切换到另外一个料盒 mode=2：一个OK料盒 计数满之后暂停*/
[DllImport("PLT.dll", EntryPoint = "Plt_SetOKPiecesCountMode_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetOKPiecesCountMode_e(UInt16 cardid, UInt16 mode, UInt32 max_count);

/*功能描述：读取OK料统计模式  mode=0：一个OK料盒计数没限制，mode=1：两个OK料盒，当其中一个OK料盒计数满后，切换到另外一个料盒 mode=2：一个OK料盒 计数满之后暂停*/
[DllImport("PLT.dll", EntryPoint = "Plt_GetOKPiecesCountMode_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetOKPiecesCountMode_e(UInt16 cardid,  ref UInt16 mode,  ref UInt32 max_count);

/*功能描述：转盘转一圈编码器计数*/
[DllImport("PLT.dll", EntryPoint = "Plt_SetOneCycleMaxCount_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetOneCycleMaxCount_e(UInt16 cardid,  UInt32 max_count);

/*功能描述：转盘转一圈编码器计数*/
[DllImport("PLT.dll", EntryPoint = "Plt_GetOneCycleMaxCount_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetOneCycleMaxCount_e(UInt16 cardid,  ref UInt32 max_count);

/*功能描述：读取转盘当前角度*/
[DllImport("PLT.dll", EntryPoint = "Plt_GetCurAngle_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetCurAngle_e(UInt16 cardid,  ref float max_count);



[DllImport("PLT.dll", EntryPoint = "Plt_SetBlow_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetBlow_e(UInt16 cardid,UInt16 blow_num);

[DllImport("PLT.dll", EntryPoint = "Plt_StartSort_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_StartSort_e(UInt16 cardid);

[DllImport("PLT.dll", EntryPoint = "Plt_ClearSort_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_ClearSort_e(UInt16 cardid);

[DllImport("PLT.dll", EntryPoint = "Plt_SetCameraTriggerCount_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetCameraTriggerCount_e(UInt16 cardid,  UInt16 camera_num,  UInt32 camera_trigger_count);

[DllImport("PLT.dll", EntryPoint = "Plt_GetCameraTriggerCount_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetCameraTriggerCount_e(UInt16 cardid,UInt16 camera_num, UInt32[] camera_trigger_count,UInt16 count);

[DllImport("PLT.dll", EntryPoint = "Plt_SetBlowTriggerCount_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetBlowTriggerCount_e(UInt16 cardid,UInt16 blow_num,UInt32 blow_trigger_count);
[DllImport("PLT.dll", EntryPoint = "Plt_GetBlowTriggerCount_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetBlowTriggerCount_e(UInt16 cardid,UInt16 blow_num, UInt32[] blow_trigger_count,UInt16 count);



[DllImport("PLT.dll", EntryPoint = "Plt_GetSortStatus_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetSortStatus_e(UInt16 cardid, ref struct_sort_status_e  sort_status);

[DllImport("PLT.dll", EntryPoint = "Plt_GetSortDistance_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetSortDistance_e(UInt16 cardid, ref Int32  distance);

[DllImport("PLT.dll", EntryPoint = "Plt_GetWorkpieceLength_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetWorkpieceLength_e(UInt16 cardid, ref Int32  length);

[DllImport("PLT.dll", EntryPoint = "Plt_SetSortOutPut_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetSortOutPut_e(UInt16 cardid, UInt16 io_num, byte logic, UInt32 hold_time);

//[DllImport("PLT.dll", EntryPoint = "Plt_SetBlowProtectPos_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
//public static extern short  Plt_SetBlowProtectPos_e(UInt16 cardid, Int32 protect_pos);

/*
功能描述：设置吹气保护距离，此距离为相对于分选位置锁存的距离
输入参数：cardid 卡号
          protect_pos 距离
输出参数：无*/
[DllImport("PLT.dll", EntryPoint = "Plt_SetBlowProtectPos_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetBlowProtectPos_e(UInt16 cardid, Int32 protect_pos);


/*
功能描述：读取吹气保护距离，此距离为相对于分选位置锁存的距离
输入参数：cardid 卡号
          protect_pos 距离
输出参数：无*/
[DllImport("PLT.dll", EntryPoint = "Plt_GetBlowProtectPos_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetBlowProtectPos_e(UInt16 cardid, ref Int32 protect_pos);

/*
功能描述：读取中断产生时编码器位置和比较缓冲区位置
输入参数：cardid 卡号
          *enc_pos 编码器位置  
		  *cmp_pos 比较缓冲区位置
输出参数：无*/
[DllImport("PLT.dll", EntryPoint = "Plt_GetIntPos_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetIntPos_e(UInt16 cardid, ref Int32 enc_pos, ref Int32 cmp_pos);


/*功能描述：设置检测到元件后压缓冲区的延时时间
  输入参数：cardid 卡号
            delay_time 延时时间  单位：s  
  输出参数：无
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_SetBufferDelayTime_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetBufferDelayTime_e(UInt16 cardid, double delay_time);

/*功能描述：读取检测到元件后压缓冲区的延时时间
  输入参数：cardid 卡号
  输出参数：delay_time
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetBufferDelayTime_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetBufferDelayTime_e(UInt16 cardid, ref double delay_time);

/*功能描述：读取由于距离太近等导致的异常元件计数值
  输入参数：cardid 卡号
  输出参数：count
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetAbnormalWorkpieceCount_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetAbnormalWorkpieceCount_e(UInt16 cardid, ref UInt32 count);


/*功能描述：读取元件锁存位置值
  输入参数：cardid 卡号
  输出参数：pos
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetWorkPieceLatchPosition_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetWorkPieceLatchPosition_e(UInt16 cardid, ref Int32 pos);

/*功能描述：读取元件中心点位置值(锁存边沿+元件长度的一半)
  输入参数：cardid 卡号
  输出参数：pos
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetWorkPiecCentralPosition_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetWorkPiecCentralPosition_e(UInt16 cardid, ref Int32 pos);


 /*功能描述：读取缓冲区元件中心点位置值(锁存边沿+元件长度的一半)*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetWorkPiecCentralBufferPosition_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetWorkPiecCentralBufferPosition_e(UInt16 cardid, UInt16 pos_num, ref UInt16 act_pos_num, Int32[] pos);


 [DllImport("PLT.dll", EntryPoint = "Plt_SetCameraOutMap_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetCameraOutMap_e(UInt16 cardid, UInt16 cameracount, Int16[] camera_out_map);
/*
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetCameraOutMap_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetCameraOutMap_e(UInt16 cardid, UInt16 cameracount, Int16[] camera_out_map);
/*
*/

 [DllImport("PLT.dll", EntryPoint = "Plt_SetBlowOutMap_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetBlowOutMap_e(UInt16 cardid, UInt16 blowcount, Int16[] blow_out_map);
/*
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetBlowOutMap_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetBlowOutMap_e(UInt16 cardid, UInt16 blowcount, Int16[] blow_out_map);
/*
*/

 [DllImport("PLT.dll", EntryPoint = "Plt_SetCameraOutPos_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetCameraOutPos_e(UInt16 cardid, UInt16 camera_index, Int32 camera_pos);
/*
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetCameraOutPos_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetCameraOutPos_e(UInt16 cardid, UInt16 camera_index,ref Int32 camera_pos);
/*
*/

 [DllImport("PLT.dll", EntryPoint = "Plt_SetBlowOutPos_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetBlowOutPos_e(UInt16 cardid, UInt16 blow_index, Int32 blow_pos);
/*
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetBlowOutMPos_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetBlowOutMPos_e(UInt16 cardid, UInt16 blow_index,ref Int32 blow_pos);
/*
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetBlowStatus_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetBlowStatus_e(UInt16 cardid, ref UInt32 blow_more, ref UInt32 blow_little, ref UInt32 blow_cmd);
/*
*/




/*功能描述：设置OK误吹比较器已经比较个数初始值*/
[DllImport("PLT.dll", EntryPoint = "Plt_SetWrongOKTriggerCount_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetWrongOKTriggerCount_e(UInt16 cardid,UInt32 wrong_trigger_count);

/*功能描述：返回OK误吹比较器已经比较个数初始值*/
[DllImport("PLT.dll", EntryPoint = "Plt_GetWrongOKTriggerCount_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetWrongOKTriggerCount_e(UInt16 cardid,ref UInt32 wrong_trigger_count);

/*功能描述：设置NG误吹比较器已经比较个数初始值*/
[DllImport("PLT.dll", EntryPoint = "Plt_SetWrongNGTriggerCount_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetWrongNGTriggerCount_e(UInt16 cardid,UInt32 wrong_trigger_count);

/*功能描述：返回NG误吹比较器已经比较个数初始值*/
[DllImport("PLT.dll", EntryPoint = "Plt_GetWrongNGTriggerCount_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetWrongNGTriggerCount_e(UInt16 cardid,ref UInt32 wrong_trigger_count);

/*功能描述：返回OK误吹个数统计值*/
[DllImport("PLT.dll", EntryPoint = "Plt_GetWrongOKCount_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetWrongOKCount_e(UInt16 cardid,ref UInt32 wrong_count);

/*功能描述：返回NG误吹个数统计值*/
[DllImport("PLT.dll", EntryPoint = "Plt_GetWrongNGCount_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetWrongNGCount_e(UInt16 cardid,ref UInt32 wrong_count);

/*功能描述：设置传感器位置相对于检测装置的偏移位置,单位：脉冲*/
[DllImport("PLT.dll", EntryPoint = "Plt_SetWrongOKPos_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_SetWrongOKPos_e(UInt16 cardid,UInt32 pos);

/*功能描述：返回传感器位置相对于检测装置的偏移位置,单位：脉冲*/
[DllImport("PLT.dll", EntryPoint = "Plt_GetWrongOKPos_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short  Plt_GetWrongOKPos_e(UInt16 cardid,ref UInt32 pos);



[DllImport("PLT.dll", EntryPoint = "Plt_set_pre_sort_blow_parms_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short Plt_set_pre_sort_blow_parms_e(UInt16 cardid, UInt32 pre_blow_pos, UInt32 pre_blow_index, UInt32 pre_blow_time);
/*功能描述：配置预分选吹气参数
  输入参数：cardid 卡号
            pre_blow_pos 吹气口到第一个锁存位置的距离
			pre_blow_index 吹气口号
			pre_blow_time  吹气时间  单位：500us
*/
[DllImport("PLT.dll", EntryPoint = "Plt_set_pre_sort_blow_enable_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short Plt_set_pre_sort_blow_enable_e(UInt16 cardid, UInt16 pre_sort_enable);
/*功能描述：启动关闭-预分选或二次定位功能
  输入参数：cardid 卡号
            pre_sort_enable  0：关闭预分选和二次定位功能   1：开启预分选功能   2：开启二次定位功能
*/
[DllImport("PLT.dll", EntryPoint = "Plt_get_pre_sort_blow_parms_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short Plt_get_pre_sort_blow_parms_e(UInt16 cardid, ref UInt32 pre_blow_pos, ref UInt32 pre_blow_index, ref UInt32 pre_blow_time);
[DllImport("PLT.dll", EntryPoint = "Plt_get_pre_sort_blow_enable_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short Plt_get_pre_sort_blow_enable_e(UInt16 cardid, ref UInt16 pre_sort_enable);
[DllImport("PLT.dll", EntryPoint = "Plt_set_first_group_camera_num_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short Plt_set_first_group_camera_num_e(UInt16 cardid, UInt16 camera_num);
/*功能描述：设置二次定位分配到第一组锁存的相机个数
  输入参数：cardid 卡号
           camera_num  相机个数*/
[DllImport("PLT.dll", EntryPoint = "Plt_get_first_group_camera_num_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short Plt_get_first_group_camera_num_e(UInt16 cardid, ref UInt16 camera_num);
[DllImport("PLT.dll", EntryPoint = "Plt_get_pre_sort_state_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short Plt_get_pre_sort_state_e(UInt16 cardid, ref UInt32 latch_num, ref UInt32 blow_num);
/*功能描述：读取预分选状态
  输入参数：cardid 卡号
 输出参数   latch_num  已经锁存元件个数
            blow_num   已经吹气元件个数
*/
[DllImport("PLT.dll", EntryPoint = "Plt_get_first_group_latch_num_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern short Plt_get_first_group_latch_num_e(UInt16 cardid, ref UInt32 latch_num);
/*功能描述：读取二次定位第一组分选已锁存元件个数
  输入参数：cardid 卡号
 输出参数   latch_num  已经锁存元件个数
*/
/*************************************************************************第二组物料分选功能相关函数*1********************************************************/







/*************************************************************************看门狗功能相关函数*********************************************************/
 [DllImport("PLT.dll", EntryPoint = "Plt_SetHostWatchDog", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetHostWatchDog(UInt16 cardid, UInt16 enable, UInt16 watchtime, UInt16 do_total_num, ref UInt16 do_num, ref UInt16 do_logic);
/*
功能描述：设置看门狗参数
输入参数：cardid 卡号
          enable 看门狗使能标志 1：看门狗使能  0：看门狗禁止  默认是禁止
          watchtime 超时报警时间，单位[ms]
          do_total_num 超时报警时需要设置的输出IO口总数
          *do_num     超时报警时需要设置的输出IO口序号
          *do_logic    超时报警时需要设置的输出各个IO口电平
输出参数：无*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetHostWatchDog", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetHostWatchDog(UInt16 cardid, ref UInt16 enable, ref UInt16 watchtime);
/*功能描述：读取看门狗参数*/
 [DllImport("PLT.dll", EntryPoint = "Plt_FeedHostWatchDog", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_FeedHostWatchDog(UInt16 cardid);
/*功能描述：在超时报警时间范围内不断调用此函数，以防止超时报警*/
 [DllImport("PLT.dll", EntryPoint = "Plt_InitHostWatchDog", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_InitHostWatchDog(UInt16 cardid);
/*功能描述：看门狗功能复位，复位后如果需要再次启动看门狗功能，需要重新配置看门狗函数*/
/*************************************************************************看门狗功能相关函数*********************************************************/
 /*************************************************************************输入IO计数功能相关函数*********************************************************/
 [DllImport("PLT.dll", EntryPoint = "Plt_IoConfigInputCountMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_IoConfigInputCountMode(UInt16 cardid, UInt16 bitno, UInt16 count_mode, double filter_time);
 /*指令功能：设置输入IO计数模式
   输入参数：cardid	卡号，取值范围:[0,11]。
             bitno 输入io号，取值范围：EI400\EI400S:[0,31]，EI800\EIC00:[0,15];
             count_mode 计数模式  0：禁止输入IO计数  1：上升沿计数 2：下降沿计数
             filter_time 输入IO滤波时间 单位：ms 
   输出参数：无
 返回值：0或错误码*/
 [DllImport("PLT.dll", EntryPoint = "Plt_IoReadInputCountMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_IoReadInputCountMode(UInt16 cardid, UInt16 bitno, ref UInt16 count_mode, ref double filter_time);
 /*指令功能：读取IO计数模式设置
   输入参数：cardid	卡号，取值范围:[0,11]。
             bitno 输入io号，取值范围：EI400\EI400S:[0,31]，EI800\EIC00:[0,15];
        			
   输出参数：*count_mode 计数模式  0：禁止输入IO计数  1：上升沿计数 2：下降沿计数
             *filter_time 输入IO滤波时间 单位：ms 
 返回值：0或错误码*/
 [DllImport("PLT.dll", EntryPoint = "Plt_IoSetInputCountValue", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_IoSetInputCountValue(UInt16 cardid, UInt16 bitno, UInt32 init_value);
 /*指令功能：设置输入IO计数初始值
    输入参数：cardid	卡号，取值范围:[0,11]。
              bitno 输入io号，取值范围：EI400\EI400S:[0,31]，EI800\EIC00:[0,15];
              init_value 输入io计数初始值
    输出参数：无
  返回值：0或错误码*/
 [DllImport("PLT.dll", EntryPoint = "Plt_IoGetInputCountValue", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_IoGetInputCountValue(UInt16 cardid, UInt16 bitno, ref UInt32 count_value);
/*指令功能：读取输入IO计数值
    输入参数：cardid	卡号，取值范围:[0,11]。
            bitno   输入io号，取值范围：EI400\EI400S:[0,31]，EI800\EIC00:[0,15];
    输出参数：*count_value  当前输入io计数值
返回值：0或错误码*/
 [DllImport("PLT.dll", EntryPoint = "Plt_SetBufferDelayDistance_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetBufferDelayDistance_e(UInt16 cardid, UInt32 delay_distance);
/*功能描述：设置检测到元件后压缓冲区的延时脉冲
  输入参数：cardid 卡号
            delay_distance 延时时间 
  输出参数：无
*/
 [DllImport("PLT.dll", EntryPoint = "Plt_GetBufferDelayDistance_e", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetBufferDelayDistance_e(UInt16 cardid, ref UInt32 delay_distance);
/*功能描述：读取检测到元件后压缓冲区的延时脉冲
  输入参数：cardid 卡号
  输出参数：delay_distance
*/

 [DllImport("PLT.dll", EntryPoint = "Plt_SetBufferDelayDistance", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_SetBufferDelayDistance(UInt16 cardid, UInt32 delay_distance);
 /*功能描述：设置检测到元件后压缓冲区的延时脉冲
   输入参数：cardid 卡号
             delay_distance 延时时间 
   输出参数：无
 */
 [DllImport("PLT.dll", EntryPoint = "Plt_GetBufferDelayDistance", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern short Plt_GetBufferDelayDistance(UInt16 cardid, ref UInt32 delay_distance);
/*功能描述：读取检测到元件后压缓冲区的延时脉冲
    输入参数：cardid 卡号
    输出参数：delay_distance
*/
/*************************************************************************输入IO计数功能相关函数*********************************************************/

[DllImport("PLT.dll", EntryPoint = "GetEtherCATErrorInfo", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern string GetEtherCATErrorInfo(int ErrNum);
[DllImport("PLT.dll", EntryPoint = "GetCardCurrentState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardCurrentState(int CardNo, int channel);
 [DllImport("PLT.dll", EntryPoint = "InitCard", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int InitCard();
 [DllImport("PLT.dll", EntryPoint = "GetCardTotalAxisNum", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardTotalAxisNum(int CardNo, ref UInt32 TotalAxis);
 [DllImport("PLT.dll", EntryPoint = "SetCardCANIOConnectState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardCANIOConnectState(int CardNo, int NodeNum, int state, int baud);
 [DllImport("PLT.dll", EntryPoint = "GetCardCANIOConnectState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int GetCardCANIOConnectState(int CardNo, ref UInt16 NodeNum, ref UInt16 state);
 [DllImport("PLT.dll", EntryPoint = "OpenCard", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int OpenCard(ref UInt16 cardnum, UInt32[] cardtypes, UInt16[] CardNos);
 [DllImport("PLT.dll", EntryPoint = "GetCardLibVersion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int GetCardLibVersion(int CardID, ref Int32 LibVer, ref Int32 pDriver_ver, ref Int32 pLogic_ver);
 [DllImport("PLT.dll", EntryPoint = "SetCardMasterParam", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardMasterParam(int CardID, int PortNum, int Baudrate, int NodeCnt, int MasterId);
 [DllImport("PLT.dll", EntryPoint = "GetCardMasterParam", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int GetCardMasterParam(int CardNo, int PortNum, ref UInt16 Baudrate, ref UInt32 NodeCnt, ref UInt16 MasterId);
 [DllImport("PLT.dll", EntryPoint = "GetCardSlaveStationConfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int GetCardSlaveStationConfig(int CardNo, int axis, ref UInt16 SlaveAddr, ref UInt16 Sub_SlaveAddr);
 [DllImport("PLT.dll", EntryPoint = "SetCardMasterScanCycleTime", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardMasterScanCycleTime(int CardNo, int FieldbusType, int CycleTime);
 [DllImport("PLT.dll", EntryPoint = "GetCardMasterScanCycleTime", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int GetCardMasterScanCycleTime(int CardNo, int FieldbusType, ref Int32 CyclFieldbusTypeeTime);
 [DllImport("PLT.dll", EntryPoint = "CardReset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int CardReset(int CardNo);
 [DllImport("PLT.dll", EntryPoint = "CardBoardReset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int CardBoardReset();
 [DllImport("PLT.dll", EntryPoint = "CardSoftReset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int CardSoftReset(UInt16 CardNo);
 [DllImport("PLT.dll", EntryPoint = "CardCoolReset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int CardCoolReset(UInt16 CardNo);
 [DllImport("PLT.dll", EntryPoint = "CloseCard", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int CloseCard();
 [DllImport("PLT.dll", EntryPoint = "GetCardInPortValue", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardInPortValue(int CardNo, int portno);
 [DllImport("PLT.dll", EntryPoint = "EMGAction", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int EMGAction(string CardName, int cardNo, int EmgIO);

[DllImport("PLT.dll", EntryPoint = "NmcReadInportExtern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int NmcReadInportExtern(int CardNo, int Channel, int NoteID, int PortNo, ref UInt32 IoValue);
 [DllImport("PLT.dll", EntryPoint = "GetCardCANIOInPort", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardCANIOInPort(int CardNo, int NodeID, int PortNo, ref UInt32 IoValue);
 [DllImport("PLT.dll", EntryPoint = "SetCardWriteInBit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardWriteInBit(int CardNo, int bitno, int on_off);

 [DllImport("PLT.dll", EntryPoint = "SetCardWriteExtendInBit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardWriteExtendInBit(int CardNo, int bitno, int nodeno, int on_off);
 [DllImport("PLT.dll", EntryPoint = "GetCardInPortNoValue", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int GetCardInPortNoValue(int CardNo, int portno);
 [DllImport("PLT.dll", EntryPoint = "GetCardDIState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardDIState(int card_no, ref Int32 pData);
 [DllImport("PLT.dll", EntryPoint = "GetCardDIBitAlarmState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int GetCardDIBitAlarmState(int card_no, int bit_no, ref Int32 pData);
 [DllImport("PLT.dll", EntryPoint = "SetCardCompEnable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardCompEnable(int card_no, int enable, int active_level, int ref_source, int length);
 [DllImport("PLT.dll", EntryPoint = "GetCardPortNoOutState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardPortNoOutState(int CardNo, int bitno);
 [DllImport("PLT.dll", EntryPoint = "GetCardOutState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardOutState(int CardNo, int portno);
 [DllImport("PLT.dll", EntryPoint = "SetCardBitNoInBit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardBitNoInBit(int CardNo, int bitno, int on_off);
 [DllImport("PLT.dll", EntryPoint = "NmcReadOutportExtern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int NmcReadOutportExtern(int CardNo, int Channel, int NoteID, int PortNo, ref UInt32 IoValue);
 [DllImport("PLT.dll", EntryPoint = "NmcWriteOutbitExtern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int NmcWriteOutbitExtern(int CardNo, int Channel, int NoteID, int IoBit, UInt16 IoValue);
 [DllImport("PLT.dll", EntryPoint = "SetCardCANIOOutBit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardCANIOOutBit(int CardNo, int NodeID, int IoBit, int IoValue);
 [DllImport("PLT.dll", EntryPoint = "GetCardCANIOOutPort", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int GetCardCANIOOutPort(int CardNo, int NodeID, int PortNo, ref UInt32 IoValue);
 [DllImport("PLT.dll", EntryPoint = "SetCardPortNoOutPort", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardPortNoOutPort(int CardNo, int portno, int outport_va);
 [DllImport("PLT.dll", EntryPoint = "GetLocationIsOutNoState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetLocationIsOutNoState();
 [DllImport("PLT.dll", EntryPoint = "SetLocationIsOutNoState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetLocationIsOutNoState();
 [DllImport("PLT.dll", EntryPoint = "SetCardDOOut", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardDOOut(int CardNo, int data);
 [DllImport("PLT.dll", EntryPoint = "SetCardDOBitState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardDOBitState(int card_no, int bit_no, int data);
 [DllImport("PLT.dll", EntryPoint = "GetCardDOState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardDOState(int card_no, ref Int32 pData);
 [DllImport("PLT.dll", EntryPoint = "SetCardHcmpEnable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardHcmpEnable(int CardNo, int axis, int hcmp, int cmp_mode);
 [DllImport("PLT.dll", EntryPoint = "OpenCardHighCompareConfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int OpenCardHighCompareConfig(int CardNo, int hcmp, int axis, int cmp_source, int cmp_logic, long time);
 [DllImport("PLT.dll", EntryPoint = "GetCardHcmpCurrentState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardHcmpCurrentState(int CardNo, int hcmp, ref int remained_points, ref int current_point, ref int runned_points);
 [DllImport("PLT.dll", EntryPoint = "DmcSetCardVectorSProfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int DmcSetCardVectorSProfile(int CardNo, int Crd, int s_mode, double s_para);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisProfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int GetCardAxisProfile(int CardNo, int axis, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double stop_vel);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisSProfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int GetCardAxisSProfile(int CardNo, int axis, int s_mode, ref double s_para);
 [DllImport("PLT.dll", EntryPoint = "OpenCardCompareConfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int OpenCardCompareConfig(int CardNo, int axis, int enable, int cmp_source);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisHomeElReturn", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int SetCardAxisHomeElReturn(int CardNo, int axis, UInt16 enable);
 [DllImport("PLT.dll", EntryPoint = "CardAxisChangeSpeed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int CardAxisChangeSpeed(int CardNo, int axis, double Curr_Vel, double Taccdec);
 [DllImport("PLT.dll", EntryPoint = "GetCardCheckDoneMulticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardCheckDoneMulticoor(int CardNo, int crd);
 [DllImport("PLT.dll", EntryPoint = "CardAxisStopMulticoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int CardAxisStopMulticoor(int CardNo, int crd, int stop_mode);
 [DllImport("PLT.dll", EntryPoint = "SetCardDebugMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardDebugMode(int CardNo, string FileName);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisSevonPin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisSevonPin(int CardNo,int axis);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisHcmp2dsetEnable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisHcmp2dsetEnable(int CardNo, int hcmp, int cmpEnable);
 [DllImport("PLT.dll", EntryPoint = "CardAxisWriteSevonPin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int CardAxisWriteSevonPin(int CardNo,int axis,int on_off);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisEncoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisEncoder(int CardNo, int axis, int encoder_value);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisHomeProfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int SetCardAxisHomeProfile(int CardNo, int axis, int HomeDir, double home_high_vel, double home_low_vel, double home_acc, int home_mode, int org_level);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisHomeMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisHomeMode(int CardNo,int axis,int home_mode);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisSoftLimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisSoftLimit(int CardNo,int axis,double melpos,double pelpos,int enable,int react);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisELLimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisELLimit(int CardNo,int axis,int Enable,int SLAction,int ActiveLevel);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisInitVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisInitVel(int CardNo,int axis,double Min_vel);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisMaxVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisMaxVel(int CardNo,int axis,double Max_Vel);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisAccVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisAccVel(int CardNo,int axis,double Tacc);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisMotionalVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisMotionalVel(int CardNo,int axis,double min_Vel, double Max_Vel, double Tacc, double Tdec,double Stop_Vel,double SPara);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisJerkVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisJerkVel();
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisSlowVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisSlowVel();
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisHighHomeVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisHighHomeVel(int CardNo,int axis,double home_high_vel);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisHighHomeAccVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisHighHomeAccVel(int CardNo,int axis,double home_acc);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisHighHomeDecVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisHighHomeDecVel(int CardNo,int axis,double home_acc);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisLowHomeVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisLowHomeVel(int CardNo,int axis,double low_vel);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisLowHomeAccVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisLowHomeAccVel(int CardNo,int axis,double home_acc);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisLowHomeDecVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisLowHomeDecVel(int CardNo,int axis,double home_acc);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisHomePositionErro", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisHomePositionErro();
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisReverseHomeDis", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisReverseHomeDis();
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisHomeMaxProtectDis", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisHomeMaxProtectDis();
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisCurrentPosition", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisCurrentPosition(int CardNo, int axis, int pos);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisHomeProfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisHomeProfile(int CardNo, int axis,ref int home_mde,ref double Low_Vel,ref double High_Vel,ref double Tacc,ref double Tdec,ref double offsetpos);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisHomeMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisHomeMode(int CardNo, int axis,ref int home_mde);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisELLimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int GetCardAxisELLimit(int CardNo, int axis, ref int Enable, ref int SLAction, ref int ActiveLevel);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisSoftLimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisSoftLimit(int CardNo,int axis,ref double melpos,ref double pelpos,ref int enable,ref int react);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisInitVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int GetCardAxisInitVel(int CardNo, int Axis, ref double pSpeed);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisMaxVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int GetCardAxisMaxVel(int CardNo, int axis, ref double Max_Vel);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisAccVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int GetCardAxisAccVel(int CardNo, int axis, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double stop_vel);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisDecVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisDecVel(int CardNo, int axis,double Tec);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisHighHomeVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisHighHomeVel(int CardNo, int axis,ref double home_high_vel);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisHighHomeProfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisHighHomeProfile(int CardNo, int axis,ref double home_acc);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisHighHomeDecVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisHighHomeDecVel(int CardNo, int axis,ref double home_acc);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisLowHomeVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisLowHomeVel(int CardNo, int axis,ref double home_low_vel);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisLowHomeAccVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisLowHomeAccVel(int CardNo, int axis,ref double home_acc);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisLowHomeDecVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisLowHomeDecVel(int CardNo, int axis,ref double home_acc);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisHomePositionErro", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisHomePositionErro();
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisReverseHomeDis", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisReverseHomeDis();
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisHomeMaxProtectDis", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisHomeMaxProtectDis();
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisCurrentPosition", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern double GetCardAxisCurrentPosition(int cntr_no ,int CardNo,int axis);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisCurrentState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisCurrentState(int CardNo,int axis);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisAlarmState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisAlarmState(int CardNo, int axis);
 [DllImport("PLT.dll", EntryPoint = "ClearCardAxisAlarmState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int ClearCardAxisAlarmState(int CardNo, int axis);
 [DllImport("PLT.dll", EntryPoint = "ClearCardHcmpPoints", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int ClearCardHcmpPoints(int CardNo, int hcmp);
 [DllImport("PLT.dll", EntryPoint = "OpenCardAxisEnable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int OpenCardAxisEnable(int CardNo, int axis);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisDisable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisDisable(int CardNo, int axis);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisRunMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int SetCardAxisRunMode(int CardNo, int axis, UInt16 runmode);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisRunMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisRunMode(int CardNo, int axis);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisPulseEquival", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisPulseEquival(int CardNo, int axis, int equiv);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisPulseEquival", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisPulseEquival(int CardNo, int axis);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisSProfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisSProfile(int CardNo, int axis,int s_mode,double s_para);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisTProfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisTProfile();
 [DllImport("PLT.dll", EntryPoint = "CardAxisHomeMove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int CardAxisHomeMove(int CardNo, int axis);
 [DllImport("PLT.dll", EntryPoint = "CardAxisGetHomeResult", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int CardAxisGetHomeResult(int CardNo, int axis, ref UInt16 status);
 [DllImport("PLT.dll", EntryPoint = "CardAxisPointMovement", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int CardAxisPointMovement(int CardNo, int axis,long dist,int posi_mode);
 [DllImport("PLT.dll", EntryPoint = "CardAxisSerialMovement", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int CardAxisSerialMovement(int CardNo, int axis,int dir);
 [DllImport("PLT.dll", EntryPoint = "StopCardAxisMovement", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int StopCardAxisMovement(int CardNo, int axis, int stop_mode);
 [DllImport("PLT.dll", EntryPoint = "CreatCardAxisInterpolateCoord", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int CreatCardAxisInterpolateCoord();
 [DllImport("PLT.dll", EntryPoint = "ClearCardAxisInterpolateCoord", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int ClearCardAxisInterpolateCoord();
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisInterpolateMaxVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisInterpolateMaxVel();
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisInterpolateDecVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisInterpolateDecVel();
 [DllImport("PLT.dll", EntryPoint = "EMGAction", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisInterpolateJerkVel();
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisInterpolateSlowVel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisInterpolateSlowVel();
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisLatchValueExtern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisLatchValueExtern(int CardNo, int axis, int Index);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisLatchFlagExtern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisLatchFlagExtern(int CardNo, int axis);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisCompareConfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisCompareConfig(int CardNo, int axis, int enable, int cmp_source);
 [DllImport("PLT.dll", EntryPoint = "SetCardCompareConfigExtern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardCompareConfigExtern(int CardNo, int enable, int cmp_source);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisHcmpConfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisHcmpConfig(int CardNo, int hcmp, int axis, int cmp_source, int cmp_logic, int time);
 [DllImport("PLT.dll", EntryPoint = "AddCardHcmpPoint", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int AddCardHcmpPoint(int CardNo, int hcmp, int cmp_pos);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisLatchMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisLatchMode(int CardNo, int axis, int all_enable, int latch_source, int triger_chunnel);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisReserLatchFlag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisReserLatchFlag(int CardNo, int axis);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisLatchValue", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisLatchValue(int CardNo, int axis);
 [DllImport("PLT.dll", EntryPoint = "GetCardAxisLatchFlag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisLatchFlag(int CardNo, int axis);
 [DllImport("PLT.dll", EntryPoint = "GetCardHcmpCmpPin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardHcmpCmpPin(int CardNo, int hcmp);
 [DllImport("PLT.dll", EntryPoint = "SetCardHcmpCmpPin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardHcmpCmpPin(int CardNo, int hcmp, int on_off);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisLtcMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 public static extern int SetCardAxisLtcMode(int CardNo, int axis, UInt16 ltc_logic, UInt16 ltc_mode, double filter);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisCompareConfigDate", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisCompareConfigDate(int CardNo, int axis, int enable, int cmp_source);
 [DllImport("PLT.dll", EntryPoint = "SetCardLeadScrewCompEnable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardLeadScrewCompEnable(int CardNo, int axis, int enable);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisErrorStopMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisErrorStopMode(int axis, int stop_mode);
 [DllImport("PLT.dll", EntryPoint = "SetCardAxisAlarm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisAlarm(int CardNo,int axis, int enable, int active_level);
 [DllImport("PLT.dll", EntryPoint = "SetSpacing", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetSpacing(int axis, int ORGPlusPos, int PELAlarmPlusPos, int NELAlarmPlusPos);
 [DllImport("PLT.dll", EntryPoint = "OpenCardHighCompareConfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int OpenCardHighCompareConfig(int CardNo, int hcmp, ref int remained_points, ref int current_point, ref int runned_points);

[DllImport("PLT.dll", EntryPoint = "SetCardAxisPulseOutMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisPulseOutMode(int CardNo, int axis, int outmode);
[DllImport("PLT.dll", EntryPoint = "GetCardAxisPulseOutMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisPulseOutMode(int CardNo, int axis, ref UInt16 outmode);
[DllImport("PLT.dll", EntryPoint = "SetCardAxisCounterInMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int SetCardAxisCounterInMode(int CardNo, int axis, int mode);
[DllImport("PLT.dll", EntryPoint = "GetCardAxisCounterInMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
public static extern int GetCardAxisCounterInMode(int CardNo, int axis, ref UInt16 mode);






























































































    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
