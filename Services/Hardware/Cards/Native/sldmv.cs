﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/DLL/sldmv.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
/****************************************************************************************************
*  SolidTech Motion Library									    									*
*  Copyright (C) 2023 Shenzhen Solid Technology Co.,Ltd.			           						*
*																									*
*																									*
*  @file     sldmv.cs																				*
*  @brief    defs of struct and function															*
*																									*
*																									*
*  @version  20240220(x64版本号)																		*
*  @date     2024.02.20																				*
*																									*
*																									*
****************************************************************************************************/
#define WIN64
//#define WIN64
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;




namespace WenQiZhi.Domain.MotionCard.Common
{
	public class sldmv
	{
#if WIN64
		/***************************************************************************************************
		*			函数返回值及其含义
		***************************************************************************************************/
		/*数据结构*/
		public static int SLDM_ERR_OK = 0;                 /*无错误*/
		public static int SLDM_ERR_PMVAL = -1;             /*参数值错误*/
		public static int SLDM_ERR_PULSEOVERFLOW = -2;             /*脉冲发生器溢出*/
		public static int SLDM_ERR_PMID = -3;              /*参数ID不存在，无此参数*/
		public static int SLDM_ERR_MCMAX = -4;             /*控制器不存在，超出系统支持的最大控制器索引号*/
		public static int SLDM_ERR_CHMAX = -5;             /*通道号不存在，超出系统最大通道数*/
		public static int SLDM_ERR_AXISMAX = -6;               /*轴号不存在，超出系统最大轴数*/
		public static int SLDM_ERR_NOAUTH = -7;                /*控制器无授权*/
		public static int SLDM_ERR_ADDROVERFLOW = -8;              /*参数地址溢出*/
		public static int SLDM_ERR_NOFLAG = -9;                /*没有此状态或标志*/
		public static int SLDM_ERR_NONSTOPPED = -10;               /*运动没有停止*/
		public static int SLDM_ERR_MCNOFILE = -11;             /*文件号打开失败。文件号不存在，或SD卡不存在。*/
		public static int SLDM_ERR_HOSTNOFILE = -12;               /*HOST打开文件失败*/
		public static int SLDM_ERR_AXISALM = -13;              /*轴伺服报警*/
		public static int SLDM_ERR_AXISPOT = -14;              /*轴正向硬限位*/
		public static int SLDM_ERR_AXISNOT = -15;              /*轴负向硬限位*/
		public static int SLDM_ERR_AXISPSL = -16;              /*轴正向软限位*/
		public static int SLDM_ERR_AXISNSL = -17;              /*轴负向软限位*/
		public static int SLDM_ERR_AXISESTOP = -18;                /*硬急停*/
		public static int SLDM_ERR_AXISOT = -19;               /*轴硬限位*/
		public static int SLDM_ERR_AXISSL = -20;               /*轴软限位*/
		public static int SLDM_ERR_PARANUM = -25;              /*函数参数个数错误*/

		public static int SLDM_ERR_CURRENTPOS = -30;               /*脉冲位置与插补位置误差过大*/
		public static int SLDM_ERR_ARCRADIUS = -32;                /*圆弧半径误差过大*/
		public static int SLDM_ERR_ENDPOS = -33;               /*终点位置错误*/
		public static int SLDM_ERR_INDEXMAX = -34;             /*参数索引号错误，超出最大索引号*/
		public static int SLDM_ERR_ADDRMAX = -35;              /*参数地址号错误，超出最大地址号*/
		public static int SLDM_ERR_FUNCTIONEN = -36;               /*模式功能没有使能或初始化*/
		public static int SLDM_ERR_BUFFENABLE = -37;               /*缓冲区禁止操作*/
		public static int SLDM_ERR_BUFFMAX = -39;              /*超过最大缓冲区容量*/
		public static int SLDM_ERR_SPITIMEOVER = -40;              /*spi通讯超时*/
		public static int SLDM_ERR_SPISYSTICKS = -41;              /*spi心跳错误*/

		public static int PULSE_NUM_LESS_1 = -57;              /*脉冲个数小于1*/
		public static int PULSE_PERIOD_LESS_1 = -58;               /*脉冲周期小于1*/
		public static int OVERPPOS = -59;              /*正向位置溢出*/
		public static int OVERNPOS = -60;              /*负向位置溢出*/
		public static int PT_PLAN_OUT = -61;               /*PT运动数字输出错误*/
		public static int HOMEING_MOVE = -62;              /*回零中，反向限位报警*/
		public static int BLOCK_EDGE_OUT = -63;                /*缓冲区运动数字输出错误*/
		public static int BLOCK_EDGE_PTP1 = -64;               /*缓冲区运动直线插补错误*/
		public static int BLOCK_EDGE_ARC = -65;                /*缓冲区运动圆弧插补错误*/
		public static int BLOCK_EDGE_RESTARTMOVE = -66;                /*缓冲区运动启动函数错误*/
		public static int BLOCK_ADD_POS = -67;             /*缓冲区运动合成距离为0错误*/

		public static int SLDM_ERR_OPEN = -90;             /*控制卡打开失败*/
		public static int SLDM_ERR_OPENED = -91;               /*控制卡已经打开*/
		public static int SLDM_ERR_FREE = -92;             /*函数直接退出*/
		public static int SLDM_ERR_IO_OVERTIME = -93;              /*控制器IO通讯超时*/
		public static int SLDM_ERR_PC_IPADDR = -99;                /*PC IP地址错误,或未连接*/

		public static int SLDM_ERR_FPGA_CMDBUFFFULL = -100;                /*FPGA指令缓冲区满*/
		public static int SLDM_ERR_COM_ADDR = -101;                /*与HOST通讯的设备地址错误*/
		public static int SLDM_ERR_COM_CHECKSUM = -102;                /*与HOST通讯的校验和错误*/
		public static int SLDM_ERR_COM_INVCMD = -103;              /*与HOST通讯的命令ID错误*/
		public static int SLDM_ERR_SOCKET = -104;              /*HOST库中，SOCKET初始化失败*/
		public static int SLDM_ERR_SHM = -105;             /*HOST库中，SHM初始化失败*/
		public static int SLDM_ERR_PIPE = -106;                /*HOST库中，PIPE初始化失败*/
		public static int SLDM_ERR_NOLIBINITD = -107;              /*HOST库没有初始化*/
		public static int SLDM_ERR_NOMCOPEN = -108;                /*控制器未打开,或控制器关闭失败*/
		public static int SLDM_ERR_MCOPEND = -109;             /*HOST库中，控制器已打开*/
		public static int SLDM_ERR_MC_NONCONNECT = -110;               /*HOST与控制器通讯超时，未连接*/
		public static int SLDM_ERR_MC_CONNECTING = -111;               /*HOST与控制器正在连接*/
		public static int SLDM_ERR_THREAD = -112;              /*HOST库中，线程初始化失败*/
		public static int SLDM_ERR_BUFFIDX = -113;             /*命令缓冲区索引号错误*/
		public static int SLDM_ERR_REFMESSAGE = -114;              /*收到错误报文*/
		public static int SLDM_ERR_PREBUFFFULL = -115;             /*标准命令块的预缓冲区满*/
		public static int SLDM_ERR_BUFFFULL = -116;         /*标准命令块缓冲区满*/

		public static int SLDM_ERR_FB_TIMEOUT = -121;               /*伺服现场总线超时*/
		public static int SLDM_ERR_FB_NCYCWNG = -122;               /*伺服总线中非周期命令执行报警*/
		public static int SLDM_ERR_FB_CCYCERR = -123;               /*伺服总线中非周期命令执行错误*/

		public static int SLDM_ERR_SERVICE_STOP = -230;                /*控制器服务程序停止*/
		public static int SLDM_ERR_UPDATE = -255;              /*内部使用，更新命令*/

		//@Begin 返回错误ID的函数名称 编码
		public static int SLDM_OPEN_CODE = 0X01;   //1
		public static int SLDM_CLOSE_CODE = 0X02;  //2
		public static int SLDM_E2PROM_WRITE_CODE = 0X06;   //6
		public static int SLDM_E2PROM_READ_CODE = 0X07;    //7
		public static int SLDM_WRITE_REGCODE_CODE = 0X08;  //8
		public static int SLDM_READ_REGCODE_CODE = 0X09;   //9
		public static int SLDM_EXE2PROM_WRITE_CODE = 0X0A; //10
		public static int SLDM_EXE2PROM_READ_CODE = 0X0B;  //11
		public static int SLDM_FLASH_WRITE_CODE = 0X0C; //12
		public static int SLDM_ERASE_NEWAPP = 0X0D; //13
		public static int SLDM_SET_COMM_PERIOD = 0X0E; //14
		public static int SLDM_SET_DEBUG_MODE_CODE = 0X0F; //15

		public static int SLDM_SET_DIR_POL_CODE = 0X10;    //16
		public static int SLDM_SET_ENC_POL_CODE = 0X11;    //17
		public static int SLDM_SET_CPOS_CODE = 0X12;   //18
		public static int SLDM_SET_ENC_ENABLE = 0X13;   //19
		public static int SLDM_SET_ENC_CODE = 0X14;    //20
		public static int SLDM_SET_SERVOON_CODE = 0X1B;    //27
		public static int SLDM_SET_RSTALM_CODE = 0X1D; //29

		public static int SLDM_SET_PLIMIT_CONFIG_CODE = 0X20;  //32
		public static int SLDM_SET_NLIMIT_CONFIG_CODE = 0X21;  //33
		public static int SLDM_SET_ORG_CONFIG_CODE = 0X22; //34
		public static int SLDM_SET_SERVOALM_CONFIG_CODE = 0X23;    //35
		public static int SLDM_SET_SOFT_LIMIT_CODE = 0X24; //36
		public static int SLDM_RESET_CODE = 0X2E;  //46
		public static int SLDM_CLEAR_ALARM_HISTORY_CODE = 0X30;    //48
		public static int SLDM_SET_ENCZ_POL_CODE = 0X32;   //50
		public static int SLDM_SET_EMG_CONFIG_CODE = 0X33; //51
		public static int SLDM_SET_PULSE_MODE_CODE = 0X35; //53
		public static int SLDM_SET_PULSE_POL_CODE = 0X36;  //54
		public static int SLDM_SET_ENC_FILTER_CODE = 0X37; //55

		public static int SLDM_SET_SPEC_INPUT_FILTER_CODE = 0X39;  //57
		public static int SLDM_SET_AXIS_INPUT_FILTER_CODE = 0X3A;  //58

		public static int SLDM_PTPN_GROUP = 0x3E;   //62
		public static int SLDM_PTPN_STOP_GROUP = 0x3F;  //63

		public static int SLDM_SET_HOME_PARA_CODE = 0X50;  //80
		public static int SLDM_JOGA_CODE = 0X51;   //81
		public static int SLDM_PTP1_CODE = 0X52;   //82
		public static int SLDM_PTP1R_CODE = 0X53;  //83
		public static int SLDM_JOGP_CODE = 0X54;   //84
		public static int SLDM_JOGM_CODE = 0X55;   //85
		public static int SLDM_SET_HOME_OFT_CODE = 0X56;   //86
		public static int SLDM_STOP_CODE = 0X58;   //88
		public static int SLDM_ESTOP_CODE = 0X59;  //89
		public static int SLDM_CHANGE_POS_CODE = 0X5A; //90
		public static int SLDM_CHANGE_VEL_CODE = 0X5B; //91
		public static int SLDM_PTPN_CODE = 0X5C;   //92
		public static int SLDM_PTPN_STOP_CODE = 0X5D;  //93
		public static int SLDM_PMOVE_EXTERN_CODE = 0X5E;   //94
		public static int SLDM_HOME_MOVE_CODE = 0X5F;  //95

		public static int SLDM_SET_BUFF_ENABLE_CODE = 0X60;    //96
		public static int SLDM_START_BUFF_MOVE_CODE = 0X61;    //97
		public static int SLDM_ADD_BUFF_SEGMENT_CODE = 0X62;   //98
		public static int SLDM_RESET_BUFF_SEGMENT_P_CODE = 0X67;   //103
		public static int SLDM_SET_BUFF_MOVE_CURENT_P_CODE = 0X68; //104
		public static int SLDM_STOP_BUFF_MOVE_CODE = 0X69; //105
		public static int SLDM_SET_BUFF_MOVE_SINGLE_STEP_CODE = 0X6A;  //106
		public static int SLDM_RESET_BUFF_MOVE_CODE = 0X6B;    //107
		public static int SLDM_SETBUFFLOOP_CODE = 0X6D;    //109
		public static int SLDM_BUFF_WAIT_INPUT_CODE = 0X6E;    //110
		public static int SLDM_BUFF_DELAY = 0X6F;  //111

		public static int SLDM_TOGGLE_OUTBIT = 0X70;    //112
		public static int SLDM_SETOUTBIT_CODE = 0X73;  //115
		public static int SLDM_SETOUTBYTE_CODE = 0X76; //118
		public static int SLDM_SET_DI_FILTER_CODE = 0x7B;  //123
		public static int SLDM_SETOUTWORD = 0X7C;  //124
		public static int SLDM_SETOUTDWORD = 0X7D; //125
		public static int SLDM_IO_TRIGGER = 0x7E;   //126
		public static int SLDM_REVERSE_OUTBIT = 0X7F;  //127

		public static int SLDM_ARC_2D_CODE = 0X80; //128
		public static int SLDM_ARC_RADIUS_CODE = 0X81; //129
		public static int SLDM_ARC_STOP_CODE = 0X82;   //130
		public static int SLDM_ADD_MIX_SEGMENT_CODE = 0X83;    //131
		public static int SLDM_ADD_MIX_SEG_CODE = 0X84;    //132
		public static int SLDM_BUFF_DELAY_OUTBIT_TO_START_CODE = 0X85; //133
		public static int SLDM_BUFF_DELAY_OUTBIT_TO_STOP_CODE = 0X86;  //134
		public static int SLDM_BUFF_AHEAD_OUTBIT_TO_STOP_CODE = 0X87;  //135
		public static int SLDM_BUFF_CLEAR_IO_ACTION_CODE = 0X88;   //136
		public static int SLDM_ARC_3POlongS_CODE = 0X8A;    //138
		public static int SLDM_SET_GEAR_FOLLOW_CODE = 0X8B;    //139
		public static int SLDMPI_ARC_2DEX_CODE = 0X8C; //140
		public static int SLDM_PTPN_PLUS_CODE = 0X8E;//142
		public static int SLDM_PTPN_STOP_PLUS_CODE = 0X8F;  //143

		public static int SLDM_CMPR_ENABLE_CODE = 0X90;    //144
		public static int SLDM_CMPR_ADD_POlong_CODE = 0X91; //145
		public static int SLDM_CLEAR_CMPR_CODE = 0X93; //147
		public static int SLDM_START_CMPR_CODE = 0X94; //148
		public static int SLDM_COMPARELINEAR_CODE = 0X96;  //150
		public static int SLDM_CMPR_MOD_CODE = 0X97;   //151
		public static int SLDM_CMPR_2D_CODE = 0X98;    //152
		public static int SLDM_START_LATCH_CODE = 0X99;    //153
		public static int SLDM_SET_LATCH_PARA_CODE = 0X9A; //154
		public static int SLDM_CLEAR_LATCH_FIFO_CODE = 0X9C;   //156
		public static int SLDM_CMPR_3D_CODE = 0X9F; //159

		public static int SLDM_PTINIT_CODE = 0XA0; //160
		public static int SLDM_PRFPT_CODE = 0XA1;  //161
		public static int SLDM_PTSTART_CODE = 0XA3;    //163
		public static int SLDM_PTDATA_CODE = 0XA4; //164
		public static int SLDM_PTCLEAR_CODE = 0XA5;    //165
		public static int SLDM_SETPTLOOP_CODE = 0XA6;  //166
		public static int SLDM_SETPTMEMORY_CODE = 0XAB;    //171

		public static int SLDM_SET_PWM_ENABLE_CODE = 0XB0; //176
		public static int SLDM_PWM_START_CODE = 0XB1;  //177
		public static int SLDM_SET_PWM_CONFIG_CODE = 0XB2; //178
														   //public static int	SLDM_SET_GEAR_FOLLOW_CODE		=			0XB6;	//182(命中断点问题)
		public static int SLDM_BUFF_SET_PWM_OUTPUT = 0XB4; //180
		public static int SLDM_BUFF_SET_PWM_ONOFF_DUTY = 0XB5; //181
		public static int SLDM_BUFF_SET_PWM_FOLLOW_SPEED = 0XB6;   //182
		public static int SLDM_BUFF_DELAY_PWM_TO_START = 0XB7; //183
		public static int SLDM_BUFF_AHEAD_PWM_TO_STOP = 0XB8;  //184

		public static int SLDM_SET_HANDWHEEL_CONFIG = 0XBD;//189
		public static int SLDM_SET_HANDWHEEL_MAXVEL = 0XBE; //190
		public static int SLDM_SET_HANDWHEEL_ENABLE = 0XBF; //191

		public static int SLDM_SET_ECAT_AXIS = 0XC0;    //192
		public static int SLDM_ECAT_RESET = 0XC1;   //193

		public static int SLDM_TEST_FUNCTION_CODE = 0XFF;  //255
														   //@End 返回错误ID的函数名称 编码=

		//@Begin PT运动模式
		public static int PT_MODE_STATIC = 0;//（该宏定义为 0）静态模式。默认为该模式。
		public static int PT_MODE_DYNAMIC = 1;//（该宏定义为 1）动态模式
		public static int PT_SEGMENT_NORMAL = 0;//（该宏定义为 0）普通段。默认为该类型。
		public static int PT_SEGMENT_EVEN = 1;//（该宏定义为 1）匀速段。
		public static int PT_SEGMENT_STOP = 2;//（该宏定义为 2）减速到 0 段。
		public static int PT_SEGMENT_END = 3;// 自定义结束标志
											 //@End PT运动模式

		public static int MAX_ALM_BUFFER_NUM = 256;



		[StructLayout(LayoutKind.Sequential)]
		public struct CARD_INFO
		{
			public UInt32 Card_ID;
			public UInt32 Lib_SoftVer;
			public UInt32 Arm_SoftVer;
			public UInt32 Arm_GitCommit;
			public UInt32 FPGA_SoftVer;
			public UInt32 FPGA_GitCommit;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public UInt32[] FPGA_UID;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public UInt32[] Arm_UID;
			public UInt32 PCB_Ver;
			public UInt32 PCB_GitCommit;
			public UInt32 BOM_Ver;
			public UInt32 BOM_GitCommit;
			public UInt32 Slave_Type;

		}


		[StructLayout(LayoutKind.Sequential)]
		public struct Jump_Data
		{
			public UInt32 axis_count;               //3,固定3轴
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public uint[] axis_list;     //当前指令最多可以对3个轴进行操作;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public int[] target_pos;         //最终目标位置，绝对坐标
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public int[] start_pos;          //起点位置，绝对坐标
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public short[] soft_en;               //是否启用软启动和软着陆标志，soft_en[0]-软启动,soft_en[0]-软着陆。
			public int soft_upZPos;               //抬起过程中，软启动Z轴位置数据
			public int soft_downZPos;             //下降过程中，软着陆Z轴位置数据
			public float soft_upVel;               //抬起过程中，软启动终点速度
			public float soft_downVel;             //下降过程中，软着陆终点速度
			public int z_sofePos;                 //Z轴在下降和抬起过程中，安全位置
			public int z_steadyPos;               //Z轴横移高度
			public float start_vel;                //整个门运动的起始速度
			public float end_vel;                  //整个门运动的终点速度
			public float steady_vel;               //整个门运动的最大速度
			public float move_acc;                 //运动过程中的加速度，pulse/ms^2
			public float move_dec;                 //运动过程中的加速度，pulse/ms^2
		}


		public struct Axis_Para
		{
			public bool servo_alarm_en;
			public bool Plimit_en;
			public bool Nlimit_en;
			public bool origin_en;

			public bool servo_alarm_pol;
			public bool Plimit_pol;
			public bool Nlimit_pol;
			public bool origin_pol;

			public bool Dir_pol;
			public bool Enc_pol;
			public bool Encz_pol;

			public bool soft_Plimit_en;
			public bool soft_Nlimit_en;
			public bool pulse_Pol;
			public byte Pulse_Mode;//2 bit

			public int soft_Plimit_pos;
			public int soft_Nlimit_pos;
			public int home_oft_pos;
			public int i_zcap_pulse;//E516 @
		}

		public struct Axis_Status
		{
			public byte servo_alarm_vol;
			public byte Plimit_vol;
			public byte Nlimit_vol;
			public byte origin_vol;
			public byte Encz_vol;
			public byte reached_vol;
			public byte servon;
			public byte resalm;

			public byte servo_alarm_valid;
			public byte Plimit_valid;
			public byte Nlimit_valid;
			public byte origin_valid;
			public byte Encz_valid;
			public byte reached_valid;
			public byte soft_Plimit_valid;
			public byte soft_Nlimit_valid;

			public byte homing;
			public byte homed;
			public byte running;

			public byte alarm_status;//0x01正限位，0x02负限位，0x04 soft_plimit,0x08 soft_nlimit,0x10伺服报警
			public byte home_flag;

		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Alarm_History
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
			public int[] alarm_number;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
			public int[] alarm_para;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
			public int[] time_tick;

		}

		public struct Home_Para
		{
			public uint h_dir;
			public uint mode;
			public float low_vel;
			public float high_vel;
			public float acc;
			public uint enc_en;
		}

		public delegate void fn();



		/**********************************************************************************
		* @Begin E450
		***********************************************************************************/
		//********************************************************************************************************************************************************************
		/**
        * @brief 设置控制器级联数量及型号列表
        * @param IP_4：      主机IP地址第4位，主机IP地址前3位必须是192.168.100.
         *       card_num：   级联控制器数量.
        *        type：		  控制器级联型号列表数组
        *						450-E450
        *						6364-E6364
        *						6564-E6564
        *						......
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Card_List(ulong IP_4, ulong card_num, UInt32[] type);
		//********************************************************************************************************************************************************************
		/**
        * @brief 读取控制器级联数量及型号列表
        * @param IP_4：      主机IP地址第4位，主机IP地址前3位必须是192.168.100.
         *       card_num：   返回级联控制器数量.
        *        type：		  返回控制器级联型号列表数组
        *						450-E450
        *						6364-E6364
        *						6564-E6564
        *						......
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Card_List(UInt32 IP_4, ref UInt32 card_num, [Out] UInt32[] type);

		/*
		         * @brief 读取控制器级联数量及型号列表
		         * SLDM_Get_Card_List_IP 此函数暂时无法引用
		         * 
        * @param 
        *		 IP_1：      主机IP地址第1位        
        *		 IP_2：      主机IP地址第2位        
        *		 IP_3：      主机IP地址第3位
        *		 IP_4：      主机IP地址第4位
        *		 ip_4：      卡ip地址第四位      		 
        *        cardlist_num：   返回级联控制器数量.
        *        type：		  返回控制器级联型号列表数组
        *						450-E450
        *						6364-E6364
        *						6564-E6564
        *						......
        * @return 成功返回0.
        * @remark
		 */
		//sldmv.SLDM_Get_Card_List_IP(PC_IP_1, PC_IP_2, PC_IP_3, PC_IP_4, ip_4, &cardlist_num, type);
		//public static extern long SLDM_Get_Card_List(ref ulong card_num, ref UInt32[] type);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Card_List_IP(UInt32 IP_1, UInt32 IP_2, UInt32 IP_3, UInt32 IP_4, UInt32 ip_4, ref UInt32 cardlist_num, [Out] UInt32[] type);

		/**********************************************************************************
		* @End E450
		***********************************************************************************/


		/***************************************************************************************************
		*		函数接口
		***************************************************************************************************/

		/**********************************************************************************
		* @Begin 控制器操作
		***********************************************************************************/

		//********************************************************************************************************************************************************************
		/**
        * @brief 打开控制器
        * @param IP_4：      主机IP地址第4位，主机IP地址前3位必须是192.168.100.
        *        type：      以太网传输方式,取值范围：
        *						0-UDP (默认)
        *						1-wincap
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Open(ulong IP_4, ulong type = 0);
		/// <summary>
		/// 打开控制卡
		/// </summary>
		/// <param name="PC_IP_1"></param>主机地址 1
		/// <param name="PC_IP_2"></param>主机地址 2
		/// <param name="PC_IP_3"></param>主机地址 3
		/// <param name="PC_IP_4"></param>主机地址 4
		/// <param name="ip_4"></param>从机/卡 ip地址 4
		/// <returns> 成功返回 0
		/// </returns>
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Open_IP(UInt32 PC_IP_1, UInt32 PC_IP_2, UInt32 PC_IP_3, UInt32 PC_IP_4, UInt32 ip_4);
		//       public static extern int SLDM_Open(ulong IP_4, ulong card_no, ulong type = 0);
		//********************************************************************************************************************************************************************
		/**
		* @brief 关闭控制器
		* @param card_no：   0
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Close(UInt32 card_no);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置控制器最小通讯周期
		* @param card_no：	0
		*		 period：	最小通讯周期模式：单位us，取值范围：0-1000.默认值800.
		* @return 成功返回0.
		* @remark 调试模式时，控制器不会因通讯周期过长而报警。
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Comm_Period(UInt32 card_no, UInt32 period);//us
																					 //********************************************************************************************************************************************************************
		/**
		* @brief 设置控制器调试模式
		* @param card_no：	0
		*		 model：	模式：0-非调试模式，1-调试模式
		* @return 成功返回0.
		* @remark 调试模式时，控制器不会因通讯周期过长而报警。
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Debug_Mode(UInt32 card_no, UInt32 mode);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器心跳
		* @param card_no：	0
		*		 ticks：	返回控制器心跳值
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_SysTicks(UInt32 card_no, ref UInt32 ticks);

		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器的连接状态
		* @param card_no：	级联的卡号
		*		 status：	返回控制器的连接状态：
		*						0-已连接
		*						-1-未连接
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_ConnStatus(UInt32 card_no, ref int status);

		/**********************************************************************************
		* @End 控制器操作
		***********************************************************************************/

		/**********************************************************************************
		* @Begin 轴参数、状态读写
		***********************************************************************************/

		//********************************************************************************************************************************************************************
		/**
		* @brief 设置控制器IP地址（默认地址为192.168.100.1）
		* @param card_no：	0
		*		 IP_1：	IP地址第一位，对应默认地址为192
		*		 IP_2：	IP地址第二位，对应默认地址为168
		*		 IP_3：	IP地址第三位，对应默认地址为100
		*		 IP_4：	IP地址第四位，对应默认地址为1
		* @return 成功返回0.
		* @remark 调试模式时，控制器不会因通讯周期过长而报警。
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_IP(UInt32 card_no, UInt32 IP_1, UInt32 IP_2, UInt32 IP_3, UInt32 IP_4);
		//********************************************************************************************************************************************************************

		//********************************************************************************************************************************************************************
		/**
		* @brief 设置脉冲输出模式
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 mode：		模式值：
		*						0-脉冲+方向
		*						1-AB项
		*						2-双脉冲
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Pulse_Mode(UInt32 card_no, UInt32 axis, UInt32 mode);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴脉冲极性
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pol：		极性值：
		*						0-正极性 (默认)
		*						1-负极性
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Pulse_Pol(UInt32 card_no, UInt32 axis, UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴脉冲方向极性
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pol：		极性值：
		*						0-正方向 (默认)
		*						1-负方向
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Dir_Pol(UInt32 card_no, UInt32 axis, UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴编码器方向极性
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pol：		极性值：
		*						0-正极性 (默认)
		*						1-负极性
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Enc_Pol(UInt32 card_no, UInt32 axis, UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
        * @brief 设置轴编码器使能
        * @param card_no：	级联的卡号
        *		 axis：		轴选择(0-3)
        *		 en：		使能值：
        *						0-不使能
        *						1-使能 (默认)
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Enc_Enable(UInt32 card_no, UInt32 axis, UInt32 en);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置读取编码器计数倍率
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 mult：		计数倍率值，默认值4（4倍频）.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Enc_Mult(UInt32 card_no, UInt32 axis, UInt32 mult);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴当前位置
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		位置 ，单位：脉冲.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Cpos(UInt32 card_no, UInt32 axis, int pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴当前位置
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		返回当前位置 ，单位：脉冲.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Pos(UInt32 card_no, UInt32 axis, ref Int32 pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴当前编码器位置
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		位置 ，单位：脉冲.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Enc(UInt32 card_no, UInt32 axis, int pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴当前编码器位置
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		返回当前编码器位置 ，单位：脉冲.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Enc(UInt32 card_no, UInt32 axis, ref Int32 pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置编码器Z信号参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pol：		有效电平，0-低电平，1-高电平
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_EncZ_Pol(UInt32 card_no, UInt32 axis, UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取编码器Z信号电平
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 valid：	返回编码器z信号有效状态，0-无效，1-有效.
		*		 el：		返回编码器z信号电平，0-低电平，1-高电平.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Enc_Z(UInt32 card_no, UInt32 axis, ref UInt32 valid, ref UInt32 el);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置急停参数
		* @param card_no：	级联的卡号
		*		 en：		使能 0-不使能，1-使能 (默认不使能)
		*		 pol：		急停有效值，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_EMG_Config(UInt32 card_no, UInt32 en, UInt32 pol);

		//********************************************************************************************************************************************************************
		/**
		* @brief 读取急停参数
		* @param card_no：	级联的卡号
		*		 en：		使能 0-不使能，1-使能 
		*		 pol：		急停有效值，0-输入不导通，1-输入导通 
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_EMG_Config(UInt32 card_no, ref UInt32 en, ref UInt32 pol);

		//********************************************************************************************************************************************************************
		/**
		* @brief 读取急停信号状态
		* @param card_no：	级联的卡号
		*		 valid：	返回编码器z信号有效状态，0-无效，1-有效.
		*		 el：		返回编码器z信号电平，0-低电平，1-高电平.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_EMG(UInt32 card_no, ref UInt32 valid, ref UInt32 el);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置各轴伺服使能状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 on：		使能端口导通状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_ServoOn(UInt32 card_no, UInt32 axis, UInt32 on);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴伺服使能状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 on：		返回使能端口导通状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_ServoOn(UInt32 card_no, UInt32 axis, ref UInt32 on);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置各轴伺服报警清除状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 on：		报警清除端口导通状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_RstAlm(UInt32 card_no, UInt32 axis, UInt32 on);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴伺服报警清除状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 on：		报警清除端口导通状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_RstAlm(UInt32 card_no, UInt32 axis, ref UInt32 on);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴正限位参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		使能 0-不使能，1-使能 (默认不使能)
		*		 pol：		限位有效值，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Plimit_Config(UInt32 card_no, UInt32 axis, UInt32 en, UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴正限位参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		返回使能状态 0-不使能，1-使能 (默认不使能)
		*		 pol：		返回限位有效值状态，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Plimit_Config(UInt32 card_no, UInt32 axis, ref UInt32 en, ref UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴正向限位开关状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 valid：	返回限位有效状态值 0-无效，1-有效
		*		 el：		返回限位接口导通状态值0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_POT(UInt32 card_no, UInt32 axis, ref UInt32 valid, ref UInt32 el);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴负限位参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		使能 0-不使能，1-使能 (默认不使能)
		*		 pol：		限位有效值，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Nlimit_Config(UInt32 card_no, UInt32 axis, UInt32 en, UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴负限位参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		返回使能状态 0-不使能，1-使能 (默认不使能)
		*		 pol：		返回限位有效值状态，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Nlimit_Config(UInt32 card_no, UInt32 axis, ref UInt32 en, ref UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴负向限位开关状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 valid：	返回限位有效状态值 0-无效，1-有效
		*		 el：		返回限位接口导通状态值0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_NOT(UInt32 card_no, UInt32 axis, ref UInt32 valid, ref UInt32 el);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴原点参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		使能 0-不使能，1-使能 (默认不使能)
		*		 pol：		原点有效值，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Org_Config(UInt32 card_no, UInt32 axis, UInt32 en, UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴原点参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		返回使能状态 0-不使能，1-使能 (默认不使能)
		*		 pol：		返回原点有效值状态，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Org_Config(UInt32 card_no, UInt32 axis, ref UInt32 en, ref UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴原点开关状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 valid：	返回原点开关有效状态值 0-无效，1-有效
		*		 el：		返回原点开关接口导通状态值0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Org(UInt32 card_no, UInt32 axis, ref UInt32 valid, ref UInt32 el);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴伺服报警参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		使能 0-不使能，1-使能 (默认不使能)
		*		 pol：		轴伺服报警有效值，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_ServoAlm_Config(UInt32 card_no, UInt32 axis, UInt32 en, UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴伺服报警参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		返回使能状态 0-不使能，1-使能 (默认不使能)
		*		 pol：		返回报警有效值状态，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_ServoAlm_Config(UInt32 card_no, UInt32 axis, ref UInt32 en, ref UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴伺服报警开关状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 valid：	返回伺服报警关有效状态值 0-无效，1-有效
		*		 el：		返回伺服报警接口导通状态值0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Servo_Alarm(UInt32 card_no, UInt32 axis, ref UInt32 valid, ref UInt32 el);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴方向极性参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 Dir_pol：	返回脉冲方向设置状态 0-正方向，1-反方向
		*		 Pulse_Pol：返回脉冲极性设置状态 0-正极性，1-负极性
		*		 Enc_pol：	返回编码器极性设置状态 0-正极性，1-负极性
		*		 Encz_pol：	返回编码器Z信号极性设置状态 0-正极性，1-负极性
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_AxisPol_Config(UInt32 card_no, UInt32 axis, ref UInt32 Dir_pol, ref UInt32 Pulse_Pol, ref UInt32 Enc_pol, ref UInt32 Encz_pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴参数结构
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 p_axispara：返回轴参数结构体
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Axis_Para(UInt32 card_no, UInt32 axis, ref Axis_Para p_axispara);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置编码器滤波时间
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 filter_time：滤波时间，单位us，默认值0.2-20ns.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Enc_Filter(UInt32 card_no, UInt32 axis, float filter_time);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置数字输入口滤波时间
		* @param card_no：	级联的卡号
		*		 input_bit：DI位选择(0-15)
		*		 filter_time：滤波时间，单位us，默认值1-1us.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_DI_Filter(UInt32 card_no, UInt32 input_bit, float filter_time);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置限位、原点、到位输入信号滤波时间
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 filter_time：滤波时间，单位us，默认值1000-1ms.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Spec_Input_Filter(UInt32 card_no, UInt32 axis, float filter_time);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴报警信号滤波时间
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 filter_time：滤波时间，单位us，默认值1000-1ms.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Axis_Input_Filter(UInt32 card_no, UInt32 axis, float filter_time);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴运动的目标位置
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		返回当前运动目标位置 ，单位：脉冲.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_PosT(UInt32 card_no, UInt32 axis, ref int pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴运动的轨迹规划位置
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		返回当前运动轨迹规划位置 ，单位：脉冲.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_PosA(UInt32 card_no, UInt32 axis, [Out] int pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴运动速度
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 vel：		返回轴当前速度，单位：p/ms
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Vel(UInt32 card_no, UInt32 axis, ref float vel);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取直线插补、圆弧插补运动速度
		* @param card_no：	级联的卡号
		*		 line_vel：	返回直线插补当前速度，单位：p/ms
		*		 arc_vel：	返回圆弧插补当前速度，单位：p/ms
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_ExVel(UInt32 card_no, ref float line_vel, ref float arc_vel);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴限位原点等状态结构体
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 p_axissatus：返回轴状态结构体
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Axis_Status(UInt32 card_no, UInt32 axis, ref Axis_Status p_axissatus);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴到位开关状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 valid：	返回到位开关有效状态值 0-无效，1-有效
		*		 el：		返回到位开关接口导通状态值0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Reached(UInt32 card_no, UInt32 axis, ref UInt32 valid, ref UInt32 el);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴正在运动状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 running：	返回轴运动状态，0-停止，1-运动
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Running(UInt32 card_no, UInt32 axis, ref UInt32 running);
		//********************************************************************************************************************************************************************
		/**
		* @brief 轴复位
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Reset(UInt32 card_no, UInt32 axis);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴综合报警信息
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 p_almstatus：返回各轴报警信息状态
		* @return 成功返回0.
		* @remark 0x01正限位，0x02负限位，0x04 正软限位,0x08 负软限位,0x10伺服报警
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Axis_AlmStatus(UInt32 card_no, UInt32 axis, ref UInt32 p_almstatus);
		//********************************************************************************************************************************************************************
		/**
        * @brief 读取轴历史报警信息
        * @param card_no：	级联的卡号
        *		 p_alarmhistory：返回轴历史报警结构
        * @return 返回历史报警个数.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Alarm_History(UInt32 card_no, ref Alarm_History p_alarmhistory);
		//********************************************************************************************************************************************************************
		/**
		* @brief 清除轴历史报警信息
		* @param card_no：	级联的卡号
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Clear_Alarm_History(UInt32 card_no);
		//********************************************************************************************************************************************************************
		/**
        * @brief 读取控制器最后一次的历史报警信息
        * @param card_no：	级联的卡号
        *		 p_Alarm_number：返回报警标志
        *		 p_Alarm_para：返回报警参数
        *		 p_time_tick：返回报警次数
        * @return 返回历史报警个数.
        * @remark 0x01正限位，0x02负限位，0x04 正软限位,0x08 负软限位,0x10伺服报警
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_AlarmHistory(UInt32 card_no, [Out] int[] p_Alarm_number, [Out] int[] p_Alarm_para, [Out] int[] p_time_tick);

		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴错误信息
		* @param card_no：	级联的卡号
		*		 err：		返回轴错误代码，正常返回0.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Move_Error(UInt32 card_no, ref int err);

		/**********************************************************************************
		* @End 轴参数、状态读写
		***********************************************************************************/

		/**********************************************************************************
		* @Begin 回零
		***********************************************************************************/


		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴回零偏置参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		回零偏置位置，单位：脉冲.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Home_Oft(UInt32 card_no, UInt32 axis, int pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置软限位参数
		* @param card_no：	级联的卡号
		*		 axis_mask：按位掩码表示的轴选择
		*		 en：		回零偏置位置，单位：脉冲.
		*		 p_pos：	正限位位置，单位：脉冲
		*		 n_pos：	负限位位置，单位：脉冲
		* @return 成功返回0.
		* @remark 软限位在回零完成后生效
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Soft_Limit(UInt32 card_no, UInt32 axis, UInt32 en, int p_pos, int n_pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴软限位参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		返回软限位使能设置状态 0-不使能，1-使能
		*		 p_pos：	返回正软限位位置
		*		 n_pos：	返回负软限位位置
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Soft_Limit(UInt32 card_no, UInt32 axis, ref UInt32 en, ref int p_pos, ref int n_pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置回零参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 h_dir：	回零方向，0-正向，1-反向.
		*		 low_vel：	低速速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 high_vel：	高速速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000.
		*		 mode：		回零模式：
		*                       0- 原地回零
		*                       1- 一次原点回零
		*                       2- 一次限位回零
		*                       3- 一次原点回零加回找
		*                       4- 一次限位回零加回找
		*                       5- 两次原点回零
		*                       6- 两次限位回零
		*                       7- EZ 信号回零
		*                       8- 原点回零后再回找 EZ 信号
		*                       9- 限位回零后再回找 EZ 信号
		*                       10- 原点锁存
		*                       11- 限位锁存
		*                       12- EZ信号 锁存
		*                       13- 原点回零加反向 EZ 锁存
		*                       14- 限位回零加反向 EZ 锁存
		*		 enc_en：	设置编码器有效，0-无效，1-有效
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Home_Para(UInt32 card_no, UInt32 axis, UInt32 h_dir, float low_vel, float high_vel, float acc, UInt32 mode, UInt32 enc_en);

		//********************************************************************************************************************************************************************
		/**
		* @brief 启动回零运动
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Home_Move(UInt32 card_no, UInt32 axis);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴回零状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 homing：	返回轴回零中状态，0-不在回零过程中，1-在回零过程中
		*		 homed：	返回轴回零完成状态，0-回零未完成，1-回零完成
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Homing(UInt32 card_no, UInt32 axis, ref UInt32 homing, ref UInt32 homed);

		/**********************************************************************************
		* @End  回零
		***********************************************************************************/


		/**********************************************************************************
		* @Begin 单轴运动
		***********************************************************************************/

		//********************************************************************************************************************************************************************
		/**
		* @brief 启动绝对式 P2P 运动
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		目标位置，单位 pulse，范围-2 31 ~2 31
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 sine：		加速曲线，0-T型，1-S型
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_JogA(UInt32 card_no, UInt32 axis, int pos, float start_vel, float steady_vel, float acc, UInt32 sine);
		//********************************************************************************************************************************************************************
		/**
		* @brief 启动绝对式 P2P 运动（非对称点位运动）
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		目标位置，单位 pulse，范围-2 31 ~2 31
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 sine：		加速曲线，0-T型，1-S型
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTP1(UInt32 card_no, UInt32 axis, Int32 pos, float start_vel, float steady_vel, float end_vel, float acc, float dec, UInt32 sine);

		//********************************************************************************************************************************************************************
		/**
		* @brief 启动相对式 P2P 运动（非对称点位运动）
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		相对目标位置，单位 pulse，范围-2 31 ~2 31
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 sine：		加速曲线，0-T型，1-S型
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTP1R(UInt32 card_no, UInt32 axis, int pos, float start_vel, float steady_vel, float end_vel, float acc, float dec, UInt32 sine);
		//********************************************************************************************************************************************************************
		/**
		* @brief 启动正向连续运动
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加(减)速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_JogP(UInt32 card_no, UInt32 axis, float start_vel, float steady_vel, float acc);
		//********************************************************************************************************************************************************************
		/**
		* @brief 启动负向连续运动
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加(减)速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_JogM(UInt32 card_no, UInt32 axis, float start_vel, float steady_vel, float acc);
		//********************************************************************************************************************************************************************
		/**
		* @brief 轴运动减速停止，针对 P2P、Jog、Home 等运动
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 stop_dec：	减速停止减速度，如果为零则按照默认减速度停止.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Stop(UInt32 card_no, UInt32 axis, float stop_dec);
		//********************************************************************************************************************************************************************
		/**
		* @brief 轴运动立即停止，针对 P2P、Jog、Home 等运动
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Estop(UInt32 card_no, UInt32 axis);
		//********************************************************************************************************************************************************************
		/**
		* @brief 改变轴运动终点位置
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		目标位置，单位 pulse，范围-2 31 ~2 31
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Change_Pos(UInt32 card_no, UInt32 axis, int pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 改变轴运动速度
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 vel：		目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Change_Vel(UInt32 card_no, UInt32 axis, float vel);
		/**********************************************************************************
		* @End 单轴运动
		***********************************************************************************/


		/**********************************************************************************
		* @Begin 多轴运动
		***********************************************************************************/
		//********************************************************************************************************************************************************************
		/**
		* @brief 多轴直线点位运动
		* @param card_no：	级联的卡号
		*		 n_axis：	运动轴个数1-7
		*		 p_axis：	轴号数组
		*		 pos：		各轴目标位置数组，单位 pulse，范围-2 31 ~2 31
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000		
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000		
		*		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000	
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 sine：		加速曲线，0-T型，1-S型		
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTPn(UInt32 card_no, UInt32 n_axis, UInt32[] axislist, Int32[] pos, float start_vel, float steady_vel, float end_vel, float acc, float dec, UInt32 sine);
		//********************************************************************************************************************************************************************
		/**
		* @brief 多轴直线点位运动
		* @param card_no：	级联的卡号
		*		 stop_dec：	减速停止减速度，如果为零则按照默认减速度停止.	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTPn_Stop(UInt32 card_no, float stop_dec);
		//********************************************************************************************************************************************************************
		/**
        * @brief 带组号的多轴直线点位运动
        * @param card_no：	级联的卡号
        *		 group_id：	组号，区别于其他组号，每组独立运动，停止.范围：0-最大轴数/2
        *		 n_axis：	运动轴个数1-7
        *		 p_axis：	轴号数组
        *		 pos：		各轴目标位置数组，单位 pulse，范围-2 31 ~2 31
        *		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000		
        *		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000		
        *		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000	
        *		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
        *		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
        *		 sine：		加速曲线，0-T型，1-S型		
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTPn_Group(UInt32 card_no, UInt32 group_id, UInt32 n_axis, UInt32[] axislist, Int32[] pos, float start_vel, float steady_vel, float end_vel, float acc, float dec, UInt32 sine);
		//********************************************************************************************************************************************************************
		/**
        * @brief 停止多轴直线点位运动
        * @param card_no：	级联的卡号
        *		 group_id：	组号，区别于其他组号，每组独立运动，停止.范围：0-最大轴数/2
        *		 stop_dec：	减速停止减速度，如果为零则按照默认减速度停止.	
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTPn_Stop_Group(UInt32 card_no, UInt32 group_id, float stop_dec);
		//********************************************************************************************************************************************************************
		/**
        * @brief 多轴直线点位运动（扩展升级，虚拟轴规划方式）
        * @param card_no：	级联的卡号
        *		 n_axis：	运动轴个数1-7
        *		 p_axis：	轴号数组
        *		 pos：		各轴目标位置数组，单位 pulse，范围-2 31 ~2 31
        *		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000		
        *		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000		
        *		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000	
        *		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
        *		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
        *		 sine：		加速曲线，0-T型，1-S型		
        * @return 成功返回0.
        * @remark 与SLDM_PTPn_Stop_Plus 配合使用
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTPn_Plus(UInt32 card_no, UInt32 n_axis, UInt32[] axislist, Int32[] pos, float start_vel, float steady_vel, float end_vel, float acc, float dec, UInt32 sine);
		//********************************************************************************************************************************************************************
		/**
        * @brief 停止多轴直线点位运动（扩展升级，虚拟轴规划方式）
        * @param card_no：	级联的卡号
        *		 stop_dec：	减速停止减速度，如果为零则按照默认减速度停止.	
        * @return 成功返回0.
        * @remark 与SLDM_PTPn_Plus 配合使用
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTPn_Stop_Plus(UInt32 card_no, float stop_dec);
		/**********************************************************************************
		* @End 多轴运动
		***********************************************************************************/


		/**********************************************************************************
		* @Begin 圆弧运动
		***********************************************************************************/

		//********************************************************************************************************************************************************************
		/**
		* @brief 二维圆弧运动（圆心）
		* @param card_no：	级联的卡号
		*		 axislist：	二维轴号数组	
		*		 center_pos：二维圆心位置数组，单位：脉冲个数		
		*		 end_pos：	二维终点位置，单位：脉冲个数
		*		 ccw_dir：	圆弧方向：0-逆时针，1-顺时针
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Arc_2D(UInt32 card_no, UInt32[] axislist, float[] center_pos, Int32[] end_pos, UInt32 ccw_dir, float start_vel, float steady_vel, float end_vel, float acc, float dec);
		//********************************************************************************************************************************************************************
		/**
		* @brief 扩展二维圆弧运动（圆心）
		* @param card_no：	级联的卡号
		*		 axislist：	二维轴号数组	
		*		 center_pos：二维圆心位置数组，单位：脉冲个数		
		*		 end_pos：	二维终点位置，单位：脉冲个数
		*		 circle：	圈数，单位：圈，0：一段圆弧		
		*		 oft_pos：	弧长补偿增量长度，单位：脉冲个数
		*		 ccw_dir：	圆弧方向：0-逆时针，1-顺时针
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDMpi_Arc_2DEx(uint card_no, UInt32[] axislist, float[] center_pos, Int32[] end_pos, int circle, int oft_pos, uint ccw_dir, float start_vel, float steady_vel, float end_vel, float acc, float dec);
		//********************************************************************************************************************************************************************
		/**
		* @brief 二维圆弧运动（半径）
		* @param card_no：	级联的卡号
		*		 axislist：	二维轴号数组			
		*		 end_pos：	二维终点位置，单位：脉冲个数
		*		 arc_Radius：圆弧半径，单位：脉冲个数
		*		 ccw_dir：	圆弧方向：0-逆时针，1-顺时针
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Arc_Radius(UInt32 card_no, UInt32[] axislist, Int32[] end_pos, float arc_Radius, uint ccw_dir, float start_vel, float steady_vel, float end_vel, float acc, float dec);
		//********************************************************************************************************************************************************************
		/**
		* @brief 基于三点二维圆弧插补运动
		* @param card_no：	级联的卡号	
		*		 axislist：	轴号列表
		*		 mid_pos：	中间位置数组，单位：脉冲
		*		 target_pos：目标位置数组，单位：脉冲
		*		 start_vel：起始速度
		*		 steady_vel：最大速度
		*		 end_vel：	终点速度
		*		 acc：		加速度
		*		 dec：		减速度
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Arc_3Points(UInt32 card_no, UInt32[] axislist, float[] mid_pos, Int32[] target_pos, float start_vel, float steady_vel, float end_vel, float acc, float dec);


		//********************************************************************************************************************************************************************
		/**
		* @brief 二维圆弧运动停止
		* @param card_no：	级联的卡号
		*		 stop_dec：	减速停止减速度，如果为零则按照默认减速度停止.			
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Arc_Stop(UInt32 card_no, float stop_dec);


		/**********************************************************************************
		* @End 圆弧运动
		***********************************************************************************/


		/**********************************************************************************
		* @Begin 高级运动功能
		***********************************************************************************/

		//********************************************************************************************************************************************************************
		/**
		* @brief 软启停动功能
		* @param card_no：	级联的卡号	
		*		 axis：		轴号（0-3）
		*		 mid_pos：	中间点位置，第一段 move 终点位置,单位：pulse
		*		 target_pos：第二段 move 终点位置,单位：pulse
		*		 start_vel：第一段 move 起始速度 
		*		 steady_vel：第一段 move 最大速度
		*		 stop_vel：	第一段 move 停止速度
		*		 delay_ms：	第一阶段完成后延迟时间（单位：毫秒）
		*		 steady_vel2：第二段 move 最大速度
		*		 end_vel：	第二段 move 停止速度
		*		 acc_ms：	加速时间(单位：ms)
		*		 dec_ms：	减速时间(单位：ms)
		*		 posi_mode：运动模式，0：相对模式，1：绝对模式
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Pmove_Extern(UInt32 card_no, UInt32 axis, int mid_pos, int target_pos, float start_vel, float steady_vel, float stop_vel,
								   uint delay_ms, float steady_vel2, float end_vel, uint acc_ms, uint dec_ms, UInt32 posi_mode);



		//********************************************************************************************************************************************************************
		/**
        * @brief 龙门跟随功能
        * @param card_no：	级联的卡号	
        *		 axis：		跟随轴轴号，取值范围：0-3
        *		 enable：	使能状态，0：禁止，1：使能
        *		 master_axis：跟随主轴轴号，取值范围：0-3
        *		 ratio：	跟随倍率
        *		 src：		位置源 0-脉冲位置 1-编码器位置
		*       delay:		跟随轴延时跟随运动延时时间，单位：ms 0-不延时 取值范围：0-1000
        * @return 成功返回0.
        * @remark 
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Gear_Follow(UInt32 card_no, UInt32 axis, UInt32 enable, UInt32 master_axis, float ratio, UInt32 src, UInt32 delay);
		/**********************************************************************************
		* @End 高级运动功能
		***********************************************************************************/

		/**********************************************************************************
		* @Begin 缓冲区运动
		***********************************************************************************/



		//********************************************************************************************************************************************************************
		/**
		* @brief 多轴直线连续运动使能
		* @param card_no：	级联的卡号
		*		 enable：	0-禁能，1-使能	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Buff_Enable(UInt32 card_no, UInt32 enable);
		//********************************************************************************************************************************************************************
		/**
		* @brief 启动多轴直线连续运动
		* @param card_no：	级联的卡号
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Start_Buff_Move(UInt32 card_no);
		//********************************************************************************************************************************************************************
		/**
		* @brief 写入缓冲区直线运动数据
		* @param card_no：	级联的卡号
		*		 n_axis：	运动轴个数（最大4轴）	
		*		 p_axis：	轴号数组	
		*		 pos：		各轴目标位置数组，单位 pulse，范围-2 31 ~2 31
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000		
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000	
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 sine：		加速曲线，0-T型，1-S型
			
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Add_Buff_Segment(UInt32 card_no, UInt32 n_axis, UInt32[] axislist, Int32[] pos, float start_vel, float steady_vel, float end_vel, float acc, float dec, UInt32 sine);

		//********************************************************************************************************************************************************************
		/**
		* @brief 得到连续运动当前缓冲区运动段数
		* @param card_no：	级联的卡号
		*		 p：		返回连续运动当前运动段数值	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Buff_Move_Curent_P(UInt32 card_no, ref UInt32 p);
		//********************************************************************************************************************************************************************
		/**
		* @brief 得到连续运动当前总运动段数
		* @param card_no：	级联的卡号
		*		 p：		返回连续运动当前运动总段数值	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Buff_Move_Total_P(UInt32 card_no, ref UInt32 p);
		//********************************************************************************************************************************************************************
		/**
		* @brief 得到连续运动当前缓冲区写入段数
		* @param card_no：	级联的卡号
		*		 p：		返回当前缓冲区写入段数值	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Buff_Segment_P(UInt32 card_no, ref UInt32 p);
		//********************************************************************************************************************************************************************
		/**
		* @brief 得到连续运动当前block缓冲区剩余段数
		* @param card_no：	级联的卡号
		*		 space：		返回当前block缓冲区段剩余段数	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Buff_Block_Space(UInt32 card_no, ref UInt32 space);
		//********************************************************************************************************************************************************************
		/**
		* @brief 复位连续运动当前block缓冲区指针
		* @param card_no：	级联的卡号	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_ReSet_Buff_Segment_P(UInt32 card_no);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置当前运动缓冲区指针位置
		* @param card_no：	级联的卡号
		*		 run_buffer：缓冲区号：0-1	
		*		 curent_p：	运动缓冲区指针位置	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Buff_Move_Curent_P(UInt32 card_no, UInt32 run_buffer, UInt32 curent_p);
		//********************************************************************************************************************************************************************
		/**
		* @brief 缓冲区连续运动减速停止
		* @param card_no：	级联的卡号
		*		 stop_dec：	减速停止减速度，如果为零则按照默认减速度停止.	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Stop_Buff_Move(UInt32 card_no, float stop_dec);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置缓冲区连续单步运动
		* @param card_no：	级联的卡号
		*		 one_step：	0-连续运动，1-单步运动	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Buff_Move_Single_Step(UInt32 card_no, UInt32 one_step);
		//********************************************************************************************************************************************************************
		/**
		* @brief 复位连续运动参数
		* @param card_no：	级联的卡号
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Reset_Buff_Move(UInt32 card_no);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取连续运动正在运行状态
		* @param card_no：	级联的卡号
		*		 running：	返回轴运动状态 0-停止，1-正在运行。	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Running_BuffMove(UInt32 card_no, ref UInt32 running);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置连续运动循环次数
		* @param card_no：	级联的卡号
		*		 loop：		循环次数，0-无限循环。	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Buff_Loop(UInt32 card_no, UInt32 loop);
		//********************************************************************************************************************************************************************
		/**
		* @brief 写入缓冲区混合运动数据
		* @param card_no：	级联的卡号
		*		 n_axis：	运动轴个数（最大4轴）	
		*		 p_axis：	轴号数组	
		*		 pos：		各轴目标位置数组，单位 pulse，范围-2 31 ~2 31
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000		
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000	
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 sine：		加速曲线，0-T型，1-S型
		*		 out_bit：	运动段终点操作DO位, 取值范围：0-最大DO位，-1为无DO操作	
		*		 out_data：	运动段终点操作DO值，取值范围：0-1
		*	     type：		运动类型 0-直线，1-圆弧
		*	     center_pos：二维圆心位置
		*	     ccw_dir：	圆弧方向：0-逆时针，1-顺时针
		*		 delaytime：终点暂停时间，单位：ms，0为无延时		
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Add_Mix_Segment(UInt32 card_no, UInt32 n_axis, UInt32[] axislist, Int32[] pos, float start_vel, float steady_vel, float end_vel, float acc, float dec, UInt32 sine, UInt32 type, float[] center_pos, UInt32 ccw_dir);
		//********************************************************************************************************************************************************************
		/**
		* @brief 写入缓冲区混合运动数据（自动加减速）
		* @param card_no：	级联的卡号
		*		 n_axis：	运动轴个数（最大4轴）	
		*		 p_axis：	轴号数组	
		*		 pos：		各轴目标位置数组，单位 pulse，范围-2 31 ~2 31	
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加(减)速度，单位：pulse /(ms*ms) ，取值范围：0.001~8000.000
		*		 sine：		加速曲线，0-T型，1-S型
		*		 out_bit：	运动段终点操作DO位, 取值范围：0-最大DO位，-1为无DO操作	
		*		 out_data：	运动段终点操作DO值，取值范围：0-1
		*	     type：		运动类型 0-直线，1-圆弧
		*	     center_pos：二维圆心位置
		*	     ccw_dir：	圆弧方向：0-逆时针，1-顺时针
		*		 delaytime：终点暂停时间，单位：ms，0为无延时		
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Add_Mix_Seg(UInt32 card_no, UInt32 n_axis, UInt32[] axislist, Int32[] pos, float steady_vel, float acc, UInt32 sine, UInt32 type, float[] center_pos, UInt32 ccw_dir);
		//********************************************************************************************************************************************************************
		/**
		* @brief 连续插补等待 IO 输入
		* @param card_no：	级联的卡号	
		*		 in_bit：	输入口号，取值范围：0~15
		*		 on_off：	导通状态，0：不导通，1：导通
		*		 delaytime：超时时间，单位：ms
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Wait_Input(UInt32 card_no, UInt32 in_bit, UInt32 on_off, UInt32 delaytime);
		//********************************************************************************************************************************************************************
		/**
		* @brief 连续插补中相对于轨迹段起点 IO 滞后输出（段内执行）
		* @param card_no：	级联的卡号	
		*		 out_bit：	输出口号，取值范围：0~15
		*		 on_off：	导通状态，0：不导通，1：导通
		*		 delay_value：滞后值，单位：ms（滞后时间模式）或 pulse（滞后距离模式）
		*		 delay_mode：滞后模式，0：滞后时间，1：滞后距离
		*		 reverse_time：电平输出后的延时翻转时间，单位：ms
		* @return 成功返回0.
		* @remark 设置的 IO 操作，将在该指令的下一条轨迹中起作用
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Delay_Outbit_To_Start(UInt32 card_no, UInt32 out_bit, UInt32 on_off, UInt32 delay_value, UInt32 delay_mode, UInt32 reverse_time);
		//********************************************************************************************************************************************************************
		/**
		* @brief 连续插补中相对于轨迹段终点 IO 滞后输出
		* @param card_no：	级联的卡号	
		*		 out_bit：	输出口号，取值范围：0~15
		*		 on_off：	导通状态，0：不导通，1：导通
		*		 delaytime：滞后时间值，单位：ms
		*		 reverse_time：电平输出后的延时翻转时间，单位：ms
		* @return 成功返回0.
		* @remark 设置的 IO 操作，将在该指令的下一条轨迹中起作用
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Delay_Outbit_To_Stop(UInt32 card_no, UInt32 out_bit, UInt32 on_off, UInt32 delay_time, UInt32 reverse_time);
		//********************************************************************************************************************************************************************
		/**
		* @brief 连续插补中相对于轨迹段终点 IO 提前输出（段内执行）
		* @param card_no：	级联的卡号	
		*		 out_bit：	输出口号，取值范围：0~15
		*		 on_off：	导通状态，0：不导通，1：导通
		*		 ahead_value：提前值，单位：ms（提前时间模式）或 pulse（提前距离模式）
		*		 ahead_mode：提前模式，0：提前时间，1：提前距离
		*		 reverse_time：电平输出后的延时翻转时间，单位：ms
		* @return 成功返回0.
		* @remark 设置的 IO 操作，将在该指令的下一条轨迹中起作用
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Ahead_Outbit_To_Stop(UInt32 card_no, UInt32 out_bit, UInt32 on_off, UInt32 ahead_value, UInt32 ahead_mode, UInt32 reverse_time);
		//********************************************************************************************************************************************************************
		/**
		* @brief 清除段内未执行完的 IO 动作
		* @param card_no：	级联的卡号	
		*		 io_bit：	IO口号
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Clear_Io_Action(UInt32 card_no, UInt32 io_bit);
		//********************************************************************************************************************************************************************
		/**
		* @brief 连续插补中暂停延时指令
		* @param card_no：	级联的卡号	
		*		 delaytime: 终点暂停时间，单位：ms，0为无限延时	
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_delay(UInt32 card_no, int delaytime);

		//********************************************************************************************************************************************************************
		/**
		* @brief 设置缓冲区运动上位机缓存使能
		* @param card_no：	级联的卡号
		*		 enable：	0-禁能，1-使能	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Move_ListEn(UInt32 card_no, UInt32 en);

		//********************************************************************************************************************************************************************
		/**
		* @brief 清空缓冲区运动上位机缓存
		* @param 
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Move_ListClear(UInt32 card_no);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取缓冲区运动上位机缓存指令数量
		* @param space：	返回PC缓冲区缓存指令（等待下载）数量

		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Buff_Move_ListSpace(UInt32 card_no, ref UInt32 space);

		/**********************************************************************************
		* @End 缓冲区运动
		***********************************************************************************/

		/**********************************************************************************
		* @Begin IO功能
		***********************************************************************************/

		//********************************************************************************************************************************************************************
		/**
		* @brief 按位设置输出DO位的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~63
		*		 data：		IO输出的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_OutBit(UInt32 card_no, UInt32 addr, UInt32 index, UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按位读取IO输出的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~63
		*		 data：		IO输出的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_OutBit(UInt32 card_no, UInt32 addr, UInt32 index, ref UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按位读取IO输入的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输入位索引号，范围0~63
		*		 data：		IO输入的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_InBit(UInt32 card_no, UInt32 addr, UInt32 index, ref UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按字节设置输出DO位的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~7
		*		 data：		IO按字节输出的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_OutByte(UInt32 card_no, UInt32 addr, UInt32 index, UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按字节读取各站的IO输入状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	DI索引号，范围：0~7
		*		 data：		IO输入的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_InByte(UInt32 card_no, UInt32 addr, UInt32 index, ref UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按字节读取IO输出的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~7
		*		 data：		IO输出的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_OutByte(UInt32 card_no, UInt32 addr, UInt32 index, ref UInt32 data);

		//********************************************************************************************************************************************************************
		/**
		* @brief 按字设置输出DO位的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~3
		*		 data：		IO按字输出的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_OutWord(UInt32 card_no, UInt32 addr, UInt32 index, UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按双字设置输出DO位的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~1
		*		 data：		IO按双字输出的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_OutDWord(UInt32 card_no, UInt32 addr, UInt32 index, UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按字读取IO输出的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~3
		*		 data：		IO输出的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_OutWord(UInt32 card_no, UInt32 addr, UInt32 index, ref UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按双字读取IO输出的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~1
		*		 data：		IO输出的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_OutDWord(UInt32 card_no, UInt32 addr, UInt32 index, ref UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按字读取各站的IO输入状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	DI索引号，范围：0~3
		*		 data：		IO输入的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_InWord(UInt32 card_no, UInt32 addr, UInt32 index, ref UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按双字读取各站的IO输入状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	DI索引号，范围：0~1
		*		 data：		IO输入的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_InDWord(UInt32 card_no, UInt32 addr, UInt32 index, ref UInt32 data);

		//********************************************************************************************************************************************************************
		/**
		* @brief 注册响应输入信号的回调函数(IO中断功能)
		* @param card_no：	级联的卡号
		*		 fn：		回调函数指针
		*		 addr：		从卡号，主卡为0
		*		 in_bit：	DI输入字节的位索引
		*		 edge：		信号边沿，0-下降沿，1-上升沿
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Register_Callback(UInt32 card_no, sldmv.fn cbEvent, UInt32 addr, UInt32 in_bit, UInt32 edge);
		//********************************************************************************************************************************************************************
		/**
        * @brief 取消响应输入信号的回调函数
        * @param card_no：	级联的卡号
        *		 addr：		从卡号，主卡为0
        *		 in_bit：	DI输入字节的位索引
        *		 edge：		信号边沿，0-下降沿，1-上升沿
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Unregister_Callback(UInt32 card_no, UInt32 addr, UInt32 in_bit, UInt32 edge);

		//********************************************************************************************************************************************************************
		/**
		* @brief 按位设置IO 输出延时翻转
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~63
		*		 data：		IO输出的状态，0-不导通，1-导通
		*		 reverse_time：翻转延时时间(单位：ms)，0-不翻转,取值范围(0-42949672)ms
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Reverse_OutBit(UInt32 card_no, UInt32 addr, UInt32 index, UInt32 data, UInt32 reverse_time);
		//********************************************************************************************************************************************************************
		/**
        * @brief 按位设置IO 延时输出延时翻转
        * @param card_no：	级联的卡号
        *		 addr：		从卡号，主卡为0	
        *		 index：	输出位索引号，范围0~63
        *		 data：		IO输出的状态，0-不导通，1-导通
        *		 delay_time：延时输出时间(单位：ms)，0-不延时
        *		 reverse_time：翻转延时时间(单位：ms)，0-不翻转
        * @return 成功返回0.//
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Toggle_OutBit(UInt32 card_no, UInt32 addr, UInt32 index, UInt32 data, UInt32 delay_time, UInt32 reverse_time);


		/**********************************************************************************
		* @End IO功能
		***********************************************************************************/






		/********************************************************************************
		* @Begin 位置比较
		*******************************************************************************/

		//********************************************************************************************************************************************************************
		/**
		* @brief 位置比较使能
		* @param card_no：	级联的卡号
		*		 fifo：		位置比较器选择，(0-1)		
		*		 en：		使能,0-不使能，1-使能	
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Cmpr_Enable(UInt32 card_no, UInt32 fifo, UInt32 en);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器位置比较使能状态
		* @param card_no：	级联的卡号
		*		 fifo：		位置比较器选择，(0-1)	
		*		 en：		位置比较使能状态，0-不使能，1-使能
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Cmpr_Enable(UInt32 card_no, UInt32 fifo, ref UInt32 en);
		//********************************************************************************************************************************************************************
		/**
		* @brief 添加位置比较数据
		* @param card_no：	级联的卡号
		*		 axis：		轴选择	
		*		 cmpr_pos：	比较位置
		*		 fifo：		位置比较器号，对应输出DO点位 （0-1）
		*		 width：	位置比较输出脉冲宽度，单位10us
		*		 out_level：比较成功时DO口输出状态，0-不导通，1-导通
		*		 src：		位置比较位置源，0-脉冲位置，1-编码器位置。
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Cmpr_Add_Point(UInt32 card_no, UInt32 axis, int cmpr_pos, UInt32 fifo, UInt32 width, UInt32 out_level, UInt32 src);
		//********************************************************************************************************************************************************************
		/**
		* @brief 得到位置比较位置缓冲区状态
		* @param card_no：	级联的卡号
		*		 fifo：		位置比较器选择，(0-1)		
		*		 space：	缓冲区剩余数据数，最大256位
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Cmpr_Fifo_Space(UInt32 card_no, UInt32 fifo, ref UInt32 space);
		//********************************************************************************************************************************************************************
		/**
		* @brief 清除位置比较位置缓冲区，及比较完成个数
		* @param card_no：	级联的卡号
		*		 fifo：		位置比较器选择，(0-1)	
		*		 fifo_data_en：	位置比较缓冲区清除，0-不清除，1-清除	
		*		 complete_num_en：	位置比较完成个数清除，0-不清除，1-清除
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Clear_Cmpr(UInt32 card_no, UInt32 fifo, UInt32 fifo_data_en, UInt32 complete_num_en);
		//********************************************************************************************************************************************************************
		/**
		* @brief 开始位置比较
		* @param card_no：	级联的卡号
		*		 fifo：		位置比较器选择，(0-1)	
		*		 start：	开始，0-停止，1-开始	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Start_Cmpr(UInt32 card_no, UInt32 fifo, UInt32 start);
		//********************************************************************************************************************************************************************
		/**
		* @brief 得到位置比较完成个数
		* @param card_no：	级联的卡号
		*		 fifo：		位置比较器选择，(0-1)		
		*		 num：		返回位置比较完成个数
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Cmpr_Num(UInt32 card_no, UInt32 fifo, ref UInt32 num);
		//********************************************************************************************************************************************************************
		/**
		* @brief 启动等间距位置比较
		* @param card_no：	级联的卡号
		*		 axis：		轴选择0-4	
		*		 startPos：	开始位置比较位置
		*		 interval：	比较位置间距
		*		 fifo：	位置比较器号，对应输出DO点位 （0-1）
		*		 width：	位置比较输出脉冲宽度，单位10us
		*		 out_level：比较成功时DO口输出状态，0-不导通，1-导通
		*		 src：		位置比较位置源，0-脉冲位置，1-编码器位置。
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Compare_Linear(UInt32 card_no, UInt32 axis, int startPos, int interval, UInt32 fifo, UInt32 width, UInt32 out_level, UInt32 src);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置位置比较模式
		* @param card_no：	级联的卡号
		*		 fifo：		位置比较器选择，(0-1)		
		*		 mod：		位置比较模式，0：一维位置比较，1：等间距位置比较；2：二维位置比较
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Cmpr_Mod(UInt32 card_no, UInt32 fifo, UInt32 mod);
		//********************************************************************************************************************************************************************
		/**
        * @brief 二维位置比较添加位置比较数据
        * @param card_no：	级联的卡号
        *		 axislist：	二维轴选择列表	
        *		 cmpr_pos：	二维比较位置列表
        *		 max_err：	比较位置到位的最大允许误差，取值范围[0,512)，单位：pulse
        *		 threshould：最优算法阈值
        *		 fifo：		位置比较器号，对应输出DO点位 （0-1）
        *		 width：	位置比较输出脉冲宽度，单位10us
        *		 out_level：比较成功时DO口输出状态，0-不导通，1-导通
        *		 src：		位置比较位置源，0-脉冲位置，1-编码器位置。
        * @return 成功返回0.
        * @remark 
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Cmpr_2D(UInt32 card_no, UInt32[] axislist, Int32[] cmpr_pos, UInt32 max_err, UInt32 threshould, UInt32 fifo, UInt32 width, UInt32 out_level, UInt32 src);
		//********************************************************************************************************************************************************************
		/**
        * @brief 三维位置比较添加位置比较数据
        * @param card_no：	级联的卡号
        *		 axislist：	三维轴选择列表	
        *		 cmpr_pos：	三维比较位置列表
        *		 max_err：	比较位置到位的最大允许误差，取值范围[0,512)，单位：pulse
        *		 threshould：最优算法阈值
        *		 fifo：		位置比较器号，对应输出DO点位 （0-1）
        *		 width：	位置比较输出脉冲宽度，单位10us
        *		 out_level：比较成功时DO口输出状态，0-不导通，1-导通
        *		 src：		位置比较位置源，0-脉冲位置，1-编码器位置。
        * @return 成功返回0.
        * @remark 
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Cmpr_3D(UInt32 card_no, UInt32[] axislist, Int32[] cmpr_pos, UInt32 max_err, UInt32 threshould, UInt32 fifo, UInt32 width, UInt32 out_level, UInt32 src);

		/**********************************************************************************
		* @End 位置比较
		***********************************************************************************/


		/**********************************************************************************
		* @Begin 位置锁存
		***********************************************************************************/


		//********************************************************************************************************************************************************************
		/**
		* @brief 开始位置锁存
		* @param card_no：	级联的卡号
		*		 fifo：		位置锁存器选择，(0-1)
		*		 start：	1-启动锁存,0-停止锁存
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Start_Latch(UInt32 card_no, UInt32 fifo, UInt32 start);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置位置锁存参数
		* @param card_no：	级联的卡号
		*		 fifo：		位置锁存缓冲区选择，（位置锁存缓冲区= DI口号）0-1	
		*		 axis：	轴选择 0-7
		*		 src：		位置锁存位置源选择，0-指令位置，1-编码器位置
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Latch_Para(UInt32 card_no, UInt32 fifo, UInt32 axis, UInt32 src);//,ushort trigger);
																										   //********************************************************************************************************************************************************************
		/**
		* @brief 读取位置锁存数据
		* @param card_no：	级联的卡号
		*		 fifo：		位置锁存缓冲区选择，（位置锁存缓冲区= DI口号）0-1	
		*		 pos：		所存位置
		*		 pos_delay：所存输入口电平有效时间
		*		 numofcapt：位置锁存完成个数
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Latch_Pos(UInt32 card_no, UInt32 fifo, [Out] int[] pos, [Out] UInt32[] pos_delay, ref UInt32 numofcapt);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取位置锁存数据
		* @param card_no：	级联的卡号
		*		 fifo：		位置锁存器选择，(0-1)	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Clear_Latch_Fifo(UInt32 card_no, UInt32 fifo);

		/**
        * @brief 注册位置锁存回调函数
        * @param card_no：	级联的卡号
        *		 fn：		回调函数	
        *		 fifo：		位置锁存器选择，(0-1)
        *		 hold_time：所存输入口电平有效时间
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Register_Latch_Callback(UInt32 card_no, sldmv.fn cbEvent, UInt32 fifo, UInt32 hold_time);
		/**
        * @brief 注销位置锁存回调函数
        * @param card_no：	级联的卡号
        *		 fifo：		位置锁存器选择，(0-1)
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Unregister_Latch_Callback(UInt32 card_no, UInt32 fifo);

		/**********************************************************************************
		* @End 位置锁存
		***********************************************************************************/

		/**********************************************************************************
		* @Begin PT运动
		***********************************************************************************/

		//********************************************************************************************************************************************************************
		/**
		* @brief 复位PT运动，PT模式开始及退出时调用
		* @param card_no：	级联的卡号	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PtInit(UInt32 card_no);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置指定轴为 PT 运动模式
		* @param card_no：	级联的卡号	
		*		 axis：		轴选择(0-3)	
		*		 mode：		PT_MODE_STATIC静态模式，PT_MODE_DYNAMIC 动态模式	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PrfPt(UInt32 card_no, UInt32 axis, UInt32 mode = 0);
		//********************************************************************************************************************************************************************
		/**
		* @brief 查询 PT 运动模式指定 FIFO 的剩余空间
		* @param card_no：	级联的卡号	
		*		 axis：		轴选择(0-3)	
		*		 pspace：	返回Fifo号对应剩余空间，动态模式时返回总剩余空间
		*		 fifo：		Fifo号(0-1)	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PtSpace(UInt32 card_no, UInt32 axis, ref UInt32 pspace, UInt32 fifo = 0);
		//********************************************************************************************************************************************************************
		/**
		* @brief 启动 PT 运动
		* @param card_no：	级联的卡号	
		*		 axis：		轴选择(0-3)	
		*		 start：	1-启动对应的轴，0-停止对应的轴	
		*		 option：	位掩码表示的所使用的 FIFO,默认为 0,动态模式下该参数无效.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PtStart(UInt32 card_no, UInt32 axis, UInt32 start, UInt32 option);
		//********************************************************************************************************************************************************************
		/**
		* @brief 向 PT 运动模式指定 FIFO 增加数据
		* @param card_no：	级联的卡号	
		*		 axis：		轴号
		*		 pos：		位置
		*		 time：		时间，单位毫秒
		*		 type：		运动类型，0-普通段，1-匀速段，2-减速段
		*		 fifo：		Fifo号
		*		 move_delay：运动结束后停止延时时间
		*		 out_bit：	运动结束后输出DO位，-1无输出
		*		 out_data：	运动结束后输出DO位状态，0-不导通，1-导通
		*		 out_delay：输出DO位延时时间
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PtData(UInt32 card_no, UInt32 axis, int pos, UInt32 time, UInt32 type, UInt32 fifo = 0, UInt32 move_delay = 0, int out_bit = -1, UInt32 out_data = 0, UInt32 out_delay = 0);
		//********************************************************************************************************************************************************************
		/**
		* @brief 清除 PT 运动模式指定 FIFO 中的数据
		* @param card_no：	级联的卡号	
		*		 axis：		轴选择(0-3)		
		*		 fifo：		Fifo号
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PtClear(UInt32 card_no, UInt32 axis, UInt32 fifo);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置 PT 运动模式循环执行的次数。动态模式下该指令无效
		* @param card_no：	级联的卡号	
		*		 axis：		轴选择(0-3)		
		*		 loop：		循环次数，0-无限循环
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_SetPtLoop(UInt32 card_no, UInt32 axis, UInt32 loop);
		//********************************************************************************************************************************************************************
		/**
		* @brief 查询 PT 运动模式循环执行的次数。动态模式下该指令无效
		* @param card_no：	级联的卡号	
		*		 axis：		轴选择(0-3)		
		*		 ploop：	返回循环次数
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_GetPtLoop(UInt32 card_no, UInt32 axis, ref UInt32 ploop);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置 PT 运动模式的缓存区(FIFO)大小
		* @param card_no：	级联的卡号	
		*		 memory：	最大100	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_SetPtMemory(UInt32 card_no, UInt32 memory);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取 PT 运动模式的缓存区大小
		* @param card_no：	级联的卡号	
		*		 pmemory：	返回缓冲区大小
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_GetPtMemory(UInt32 card_no, ref UInt32 pmemory);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取 PT 运动模式的缓存区运动指针
		* @param card_no：	级联的卡号	
		*		 axis：		轴选择(0-3)	
		*		 move_p：	返回缓冲区运动指针
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_GetPtMoveP(UInt32 card_no, UInt32 axis, ref UInt32 move_p);

		//********************************************************************************************************************************************************************
		/**
		* @brief 设置PT运动上位机缓存使能
		* @param card_no：	级联的卡号
		*		 enable：	0-禁能，1-使能	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Pt_ListEn(UInt32 card_no, UInt32 en);

		//********************************************************************************************************************************************************************
		/**
		* @brief 清空PT运动上位机缓存
		* @param card_no：	级联的卡号
				 axis：		轴号
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Pt_ListClear(UInt32 card_no, UInt32 axis);

		//********************************************************************************************************************************************************************
		/**
		* @brief 读取PT运动上位机缓存指令数量
		* @param card_no：	级联的卡号
				 space：	返回PC缓冲区缓存指令（等待下载）数量
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Pt_ListSpace(UInt32 card_no, ref UInt32 space);



		/**********************************************************************************
		* @End PT运动
		***********************************************************************************/

		/**********************************************************************************
		* @Begin PWM功能
		***********************************************************************************/


		//********************************************************************************************************************************************************************
		/**
		* @brief 设置PWM功能使能
		* @param card_no：	级联的卡号	
		*		 pwm：		PWM模块号(0-1)
		*		 en：		PWM模块使能，1-使能，0-禁止。
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_PWM_Enable(UInt32 card_no, UInt32 pwm, UInt32 en);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置PWM功能开始
		* @param card_no：	级联的卡号	
		*		 pwm：		PWM模块号(0-1)
		*		 start：	1-开始，0-停止
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PWM_Start(UInt32 card_no, UInt32 pwm, UInt32 start);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置PWM功能参数
		* @param card_no：	级联的卡号	
		*		 pwm：		PWM模块号
		*		 duty：		PWM占空比 0-1
		*		 fre：		PWM频率
		*		 pol：		PWM极性
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_PWM_Config(UInt32 card_no, UInt32 pwm, float duty, float fre, UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置PWM功能使能
		* @param card_no：	级联的卡号	
		*		 pwm：		PWM模块号
		*		 duty：		返回PWM占空比
		*		 fre：		返回PWM频率
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_PWM(UInt32 card_no, UInt32 pwm, ref float duty, ref float fre);

		//********************************************************************************************************************************************************************
		/**
		* @brief 不使用跟随模式，连续插补中 PWM 输出设置，如果PWM已经打开，PWM 输出频率及占空比立即改变
		* @param card_no：	级联的卡号	
		*		 pwm：		PWM模块号 0-1
		*		 fDuty：	PWM占空比 0-1
		*		 fFre：		PWM频率
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Set_PWM_Output(UInt32 card_no, UInt32 pwm, float fDuty, float fFre);
		//功 能：设置 PWM 开关状态对应的占空比,以下4个函数成套使用，跟随模式0-4
		//********************************************************************************************************************************************************************
		/**
		* @brief 使用跟随模式，设置 PWM 开关状态对应的占空比
		* @param card_no：	级联的卡号	
		*		 pwm：		PWM模块号
		*		 fOnDuty：	PWM 打开状态的占空比，取值范围：0~1
		*		 fOffDuty：	PWM 关闭状态的占空比，取值范围：0~1
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Set_PWM_Onoff_Duty(UInt32 card_no, UInt32 pwm, float fOnDuty, float fOffDuty);

		//********************************************************************************************************************************************************************
		/**
		* @brief 使用跟随模式，连续插补中 PWM 速度跟随设置
		* @param card_no：	级联的卡号	
		*		 pwm：		PWM模块号，取值范围：0~1
		*		 mode：		跟随模式：
							0：不跟随，保持状态
							1：不跟随，输出低电平
							2：不跟随，输出高电平
							3：跟随，占空比自动调整
							4：跟随，频率自动调整
		*		 maxVel：	最大运行速度，单位：pulse /ms
		*		 maxValue：	最大输出值：
							跟随模式为 3 时：占空比，取值范围：0~1
							跟随模式为 4 时：频率，取值范围：0~2MHz
		*		 outValue：	固定输出值：
							跟随模式为 3 时：频率，取值范围：0~2MHz
							跟随模式为 4 时：占空比，取值范围：0~1
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Set_PWM_Follow_Speed(UInt32 card_no, UInt32 pwm, UInt32 mode, float maxVel, float maxValue, float outValue);

		//********************************************************************************************************************************************************************
		/**
		* @brief 连续插补中相对于轨迹段起点 PWM 滞后输出（段内执行）
		* @param card_no：		级联的卡号	
		*		 pwm：			PWM模块号，取值范围：0~1
		*		 on_off：		输出状态，0：关闭，1：打开
		*		 delay_value：	滞后值，单位：ms（滞后时间模式）或 pulse（滞后距离模式）
		*		 delay_mode：	滞后模式，0：滞后时间，1：滞后距离
		*		 reverse_time：	保留参数，固定值为 0
		* @return 成功返回0.
		* @remark 设置的 IO 操作，将在该指令的下一条轨迹中起作用
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Delay_PWM_To_Start(UInt32 card_no, UInt32 pwm, UInt32 on_off, UInt32 delay_value, UInt32 delay_mode, float reverse_time);

		//********************************************************************************************************************************************************************
		/**
		* @brief 连续插补中相对于轨迹段终点 PWM 提前输出（段内执行）
		* @param card_no：		级联的卡号	
		*		 pwm：			PWM模块号，取值范围：0~1
		*		 on_off：		输出状态，0：关闭，1：打开
		*		 ahead_value：	提前值，单位：ms（提前时间模式）或 pulse（提前距离模式）
		*		 ahead_mode：	提前模式，0：提前时间，1：提前距离
		*		 reverse_time：	保留参数，固定值为 0
		* @return 成功返回0.
		* @remark 设置的 IO 操作，将在该指令的下一条轨迹中起作用
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Ahead_PWM_To_Stop(UInt32 card_no, UInt32 pwm, UInt32 on_off, UInt32 ahead_value, UInt32 ahead_mode, float reverse_time);



		/**********************************************************************************
		* @End PWM功能
		***********************************************************************************/


		/**********************************************************************************
		* @Begin 控制器辅助功能
		***********************************************************************************/
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器的产品版本号
		* @param card_no：	级联的卡号
		*		 softver：	返回软件库版本号
		*		 fpgaver：	返回硬件版本号
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_SysVersion(UInt32 card_no, ref UInt32 softver, ref UInt32 fpgaver);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器的ARM序列号
		* @param card_no：	级联的卡号
		*		 id：		返回控制器的ARM序列号(12byte) 
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_ArmID(UInt32 card_no, ref UInt32 id);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器的产品序列号
		* @param card_no：	级联的卡号
		*		 id：		返回控制器的产品序列号(4byte)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_CardID(UInt32 card_no, ref UInt32 id);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器的产品信息
		* @param card_no：	级联的卡号
		*		 card_info：返回控制器的产品信息结构体
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_CardInfo(UInt32 card_no, ref CARD_INFO card_info);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器IO数量参数
		* @param card_no：	级联的卡号
		*		 axis_num：	返回控制轴数(默认4)
		*		 di_num：	返回数字输入端口数量(默认16)
		*		 do_num：	返回数字输出端口数量(默认16)
		*		 ad_num：	返回模拟量输入端口数量(默认0)
		*		 da_num：	返回模拟量输出端口数量(默认0)
		*		 pwm_num：	返回PWM端口数量(默认2)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Card_IOResource(UInt32 card_no, ref UInt32 axis_num, ref UInt32 di_num, ref UInt32 do_num, ref UInt32 ad_num, ref UInt32 da_num, ref UInt32 pwm_num);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器位置比较锁存功能模块数量参数
		* @param card_no：	级联的卡号
		*		 cmpr1_num：	返回一维位置比较器数量(默认2)
		*		 cmprline_num：	返回线性位置比较器数量(默认2)
		*		 cmpr2_num：	返回二维位置比较器数量(默认0)
		*		 latch_num：	返回位置锁存器数量(默认2)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Card_EXResource(UInt32 card_no, ref UInt32 cmpr1_num, ref UInt32 cmprline_num, ref UInt32 cmpr2_num, ref UInt32 latch_num);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器模式参数
		* @param card_no：	级联的卡号
		*		 model：	返回控制器模式
		*		 type：		返回控制器类型
		*		 period：	返回控制器伺服周期
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Card_Model(UInt32 card_no, ref UInt32 model, ref UInt32 type, ref UInt32 period);


		//********************************************************************************************************************************************************************
		/**
		* @brief 用户产品注册码写入
		* @param card_no：	级联的卡号
		*		 reg_code：	客户注册码
		*		 reg_code_md5：返回写入控制器的MD5码 （16byte）
		* @return 成功返回0.
		* @remark (EEPROM大小：20*32bit)
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Write_Regcode(UInt32 card_no, byte[] reg_code, ref byte reg_code_md5);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读出写入控制器的MD5码
		* @param card_no：	级联的卡号
		*		 reg_code：	读出写入控制器的MD5码（16byte）
		* @return 成功返回0.
		* @remark (EEPROM大小：20*32bit)
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Read_Regcode(UInt32 card_no, ref byte reg_code);
		//********************************************************************************************************************************************************************
		/**
		* @brief EEPROM数据写入 
		* @param card_no：	级联的卡号
		*		 addr：		写入地址,范围：0-20
		*		 num：		写入个数0-20
		*		 pData：	写入数据,范围：32bit
		* @return 成功返回0.
		* @remark (EEPROM大小：20*32bit)
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_E2prom_Write(UInt32 card_no, UInt32 addr, UInt32 num, UInt32[] pData);
		//********************************************************************************************************************************************************************
		/**
		* @brief EEPROM数据读出 
		* @param card_no：	级联的卡号
		*		 addr：		读出地址,范围：0-20
		*		 num：		读出个数0-20
		*		 pData：	读出数据,范围：32bit
		* @return 成功返回0.
		* @remark (EEPROM大小：20*32bit)
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_E2prom_Read(UInt32 card_no, UInt32 addr, UInt32 num, ref int pData);
		//********************************************************************************************************************************************************************
		/**
		* @brief 扩展EEPROM数据写入 
		* @param card_no：	级联的卡号
		*		 addr：		写入地址,范围：0-20
		*		 num：		写入个数0-20
		*		 pData：	写入数据,范围：32bit
		* @return 成功返回0.
		* @remark (EEPROM大小：20*32bit)
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_ExE2prom_Write(UInt32 card_no, UInt32 addr, UInt32 num, int[] pData);
		//********************************************************************************************************************************************************************
		/**
		* @brief 扩展EEPROM数据读出 
		* @param card_no：	级联的卡号
		*		 addr：		读出地址,范围：0-20
		*		 num：		读出个数0-20
		*		 pData：	读出数据,范围：32bit
		* @return 成功返回0.
		* @remark (EEPROM大小：20*32bit)
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_ExE2prom_Read(UInt32 card_no, UInt32 addr, UInt32 num, ref int pData);

		//********************************************************************************************************************************************************************
		/**
		* @brief 装载系统参数文件，下载到控制器 
		* @param card_no：	级联的卡号
		* @return 成功返回0.
		* @remark 参数文件：E450config.ini
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Load_ParaFile(UInt32 card_no);

		//********************************************************************************************************************************************************************
		/**
		* @brief 保存系统参数文件
		* @param card_no：	级联的卡号
		* @return 成功返回0.
		* @remark 参数文件：E450config.ini
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Save_ParaFile(UInt32 card_no);


		/**********************************************************************************
		* @End 控制器辅助功能
		***********************************************************************************/

		/**********************************************************************************
        * @Begin ETHERCAT功能
        ***********************************************************************************/


		//********************************************************************************************************************************************************************
		/**
        * @brief 设置ethercat控制器控制轴数及型号
        * @param card_no：	级联的卡号
        *		 axisnum：	控制器连接轴数
        *		 type：		控制器连接轴型号列表数组
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Ecat_Axis(UInt32 card_no, UInt32 axis_num, UInt32[] type);

		//********************************************************************************************************************************************************************
		/**
        * @brief ethercat控制器复位
        * @param card_no：	级联的卡号
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Ecat_Reset(UInt32 card_no);


		//********************************************************************************************************************************************************************
		/**
        * @brief 读取ethercat控制器编码器状态
        * @param card_no：	级联的卡号
        *		 cur_axisnum：		当前在线轴个数
        *		 online_flag：		位掩码表示对轴在线标志 0-离线 1-在线
        *		 link_ok：			轴初始化配置完成标志 0-未完成 1-完成
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Ecat_Status(UInt32 card_no, ref UInt32 cur_axisnum, ref UInt32 online_flag, ref UInt32 link_ok);

		//********************************************************************************************************************************************************************
		/**
        * @brief 读取ethercat控制器编码器Z信号锁存位置
        * @param card_no：	级联的卡号
        *		 axis：		轴号
        *		 pos：		返回的对应编码器Z信号锁存位置计数值
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Ecat_ZCap_pos(UInt32 card_no, UInt32 axis, ref int pos);

		//********************************************************************************************************************************************************************
		/**
        * @brief 读取ethercat控制器授权控制轴数
        * @param card_no：	级联的卡号
        *		 axisnum：	返回控制器授权控制轴数
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Ecat_Release_AxisNum(UInt32 card_no, ref UInt32 axisnum);

		//********************************************************************************************************************************************************************
		/**
        * @brief 读取ethercat控制器扩展编码器位置
        * @param card_no：	级联的卡号
        *		 exenc_num：扩展编码器号，在ethercat控制器最大控制轴号基础上递增
        *		 pos：		返回的对应编码器计数值
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_EcatEx_Enc(UInt32 card_no, UInt32 exenc_num, ref int pos);


		/**********************************************************************************
        * @End ETHERCAT功能
        ***********************************************************************************/

		/**********************************************************************************
        * @Begin 手轮功能
        ***********************************************************************************/
		//********************************************************************************************************************************************************************
		/**
        * @brief 设置手轮运动参数
        * @param card_no：	级联的卡号
        *		 axis_num：	手轮控制轴数
        *		 axis_list：手轮轴选择挡位对应控制轴列表数组
        *		 mult_list：手轮倍率选择挡位对应控制倍率列表数组
        *		 dir_pol：	方向极性·，0-正向，1-反向	
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_HandWheel_Config(UInt32 card_no, UInt32 axis_num, UInt32[] axis_list, UInt32[] mult_list, UInt32 dir_pol);
		//********************************************************************************************************************************************************************
		/**
        * @brief 设置手轮运动最大速度和加速度
        * @param card_no：	级联的卡号
        *		 max_vel：	最大速度，单位：p/ms
        *		 max_acc：	最大加速度，单位：p/(ms*ms)	
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_HandWheel_MaxVel(UInt32 card_no, UInt32 max_vel, UInt32 max_acc);
		//********************************************************************************************************************************************************************
		/**
        * @brief 设置手轮运动使能
        * @param card_no：	级联的卡号
        *		 enable：	使能，0-不使能，1-使能	
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_HandWheel_Enable(UInt32 card_no, UInt32 enable);
		//********************************************************************************************************************************************************************
		/**
        * @brief 读取手轮输入端口状态
        * @param card_no：	级联的卡号
        *		 di_status：XYZ45及X10X100端口电平状态
        *					bit:
        *					0：X
        *					1：Y
        *					2：Z
        *					3：4
        *					4：5
        *					5：X10
        *					6：X100
        *					7：0
        *		 enc_data：	手轮编码器计数值

        * @return 成功返回0.
        * @remark 参数文件：E450config.ini
        */
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_HandWheel_Status(UInt32 card_no, ref UInt32 di_status, ref int enc_data);
		/**********************************************************************************
        * @End 手轮功能
        ***********************************************************************************/


		/**********************************************************************************
		* @Begin 开发调试函数
		***********************************************************************************/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Recbuf(UInt32 card_no, ref string recbuf, ref string recbuf_ID, ref string recbuf_EEP, ref string recbuf_SELF, ref string recbuf_ERR);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Sendbuf(UInt32 card_no, ref string sendbuf);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_commthread_debug(UInt32 card_no, short debug_mode, short debug_key);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Test_function(UInt32 card_no, UInt32 para_num);//测试UDP函数
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_CommTime(ref float cur_time, ref float max_time);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_CommTime_10000(ref float time);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_udp_complete_num(ref int write_num, ref int read_num);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Home_flag(UInt32 card_no, UInt32 axis, ref int flag);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Cmpr_2D(UInt32 card_no, ref UInt32 axislist, ref int cmpr_pos, UInt32 max_err, UInt32 threshould, UInt32 fifo, UInt32 width, UInt32 out_level, UInt32 src);

		/**********************************************************************************
		* @End 开发调试函数
		***********************************************************************************/

		//********************************************************************************************************************************************************************
		/**
		* @brief 单位距离转脉冲数
		* @param axis：		轴号.
		*		 PPR：		每转脉冲数
		*		 pitch：	螺距，单位ms
		*		 unit_value：	待转换单位距离，单位ms
		*		 pulse_value：	转换脉冲数结果，单位：脉冲
		* @return 成功返回0.
		* @remark 参数文件：E450config.ini
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int Unit_TO_Pulse(int axis, float PPR, float pitch, float unit_value, ref int pulse_value);

		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTPn_Ex_Register(UInt32 id, UInt32 n_axis, UInt32[] cardlist, UInt32[] axislist);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTPn_Ex(UInt32 id, int[] pos, float start_vel, float steady_vel, float end_vel, float acc, float dec, UInt32 sine, int dist_mode);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTPn_Ex_GetRunning(UInt32 id, ref UInt32 running);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTPn_Ex_Stop(UInt32 id, float stop_dec);

		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Add_Jump_Down(UInt32 card_no, Jump_Data jump_data);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Add_Jump_Up(UInt32 card_no, Jump_Data jump_data);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Add_Jump_Steady(UInt32 card_no, Jump_Data jump_data);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Write_ToUart(UInt32 card_no, UInt32 uart_no, UInt32 num, byte[] data);

		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Read_FromUart(UInt32 card_no, UInt32 uart_no, ref UInt32 num, byte[] data);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Uart_Trans_Enable(UInt32 card_no, UInt32 uart_no, UInt32 en);
#else
		/***************************************************************************************************
		*			函数返回值及其含义
		***************************************************************************************************/
		/*数据结构*/
		public static int SLDM_ERR_OK = 0;                 /*无错误*/
		public static int SLDM_ERR_PMVAL = -1;             /*参数值错误*/
		public static int SLDM_ERR_PULSEOVERFLOW = -2;             /*脉冲发生器溢出*/
		public static int SLDM_ERR_PMID = -3;              /*参数ID不存在，无此参数*/
		public static int SLDM_ERR_MCMAX = -4;             /*控制器不存在，超出系统支持的最大控制器索引号*/
		public static int SLDM_ERR_CHMAX = -5;             /*通道号不存在，超出系统最大通道数*/
		public static int SLDM_ERR_AXISMAX = -6;               /*轴号不存在，超出系统最大轴数*/
		public static int SLDM_ERR_NOAUTH = -7;                /*控制器无授权*/
		public static int SLDM_ERR_ADDROVERFLOW = -8;              /*参数地址溢出*/
		public static int SLDM_ERR_NOFLAG = -9;                /*没有此状态或标志*/
		public static int SLDM_ERR_NONSTOPPED = -10;               /*运动没有停止*/
		public static int SLDM_ERR_MCNOFILE = -11;             /*文件号打开失败。文件号不存在，或SD卡不存在。*/
		public static int SLDM_ERR_HOSTNOFILE = -12;               /*HOST打开文件失败*/
		public static int SLDM_ERR_AXISALM = -13;              /*轴伺服报警*/
		public static int SLDM_ERR_AXISPOT = -14;              /*轴正向硬限位*/
		public static int SLDM_ERR_AXISNOT = -15;              /*轴负向硬限位*/
		public static int SLDM_ERR_AXISPSL = -16;              /*轴正向软限位*/
		public static int SLDM_ERR_AXISNSL = -17;              /*轴负向软限位*/
		public static int SLDM_ERR_AXISESTOP = -18;                /*硬急停*/
		public static int SLDM_ERR_AXISOT = -19;               /*轴硬限位*/
		public static int SLDM_ERR_AXISSL = -20;               /*轴软限位*/
		public static int SLDM_ERR_PARANUM = -25;              /*函数参数个数错误*/

		public static int SLDM_ERR_CURRENTPOS = -30;               /*脉冲位置与插补位置误差过大*/
		public static int SLDM_ERR_ARCRADIUS = -32;                /*圆弧半径误差过大*/
		public static int SLDM_ERR_ENDPOS = -33;               /*终点位置错误*/
		public static int SLDM_ERR_INDEXMAX = -34;             /*参数索引号错误，超出最大索引号*/
		public static int SLDM_ERR_ADDRMAX = -35;              /*参数地址号错误，超出最大地址号*/
		public static int SLDM_ERR_FUNCTIONEN = -36;               /*模式功能没有使能或初始化*/
		public static int SLDM_ERR_BUFFENABLE = -37;               /*缓冲区禁止操作*/
		public static int SLDM_ERR_BUFFMAX = -39;              /*超过最大缓冲区容量*/
		public static int SLDM_ERR_SPITIMEOVER = -40;              /*spi通讯超时*/
		public static int SLDM_ERR_SPISYSTICKS = -41;              /*spi心跳错误*/

		public static int PULSE_NUM_LESS_1 = -57;              /*脉冲个数小于1*/
		public static int PULSE_PERIOD_LESS_1 = -58;               /*脉冲周期小于1*/
		public static int OVERPPOS = -59;              /*正向位置溢出*/
		public static int OVERNPOS = -60;              /*负向位置溢出*/
		public static int PT_PLAN_OUT = -61;               /*PT运动数字输出错误*/
		public static int HOMEING_MOVE = -62;              /*回零中，反向限位报警*/
		public static int BLOCK_EDGE_OUT = -63;                /*缓冲区运动数字输出错误*/
		public static int BLOCK_EDGE_PTP1 = -64;               /*缓冲区运动直线插补错误*/
		public static int BLOCK_EDGE_ARC = -65;                /*缓冲区运动圆弧插补错误*/
		public static int BLOCK_EDGE_RESTARTMOVE = -66;                /*缓冲区运动启动函数错误*/
		public static int BLOCK_ADD_POS = -67;             /*缓冲区运动合成距离为0错误*/

		public static int SLDM_ERR_OPEN = -90;             /*控制卡打开失败*/
		public static int SLDM_ERR_OPENED = -91;               /*控制卡已经打开*/
		public static int SLDM_ERR_FREE = -92;             /*函数直接退出*/
		public static int SLDM_ERR_IO_OVERTIME = -93;              /*控制器IO通讯超时*/
		public static int SLDM_ERR_PC_IPADDR = -99;                /*PC IP地址错误,或未连接*/

		public static int SLDM_ERR_FPGA_CMDBUFFFULL = -100;                /*FPGA指令缓冲区满*/
		public static int SLDM_ERR_COM_ADDR = -101;                /*与HOST通讯的设备地址错误*/
		public static int SLDM_ERR_COM_CHECKSUM = -102;                /*与HOST通讯的校验和错误*/
		public static int SLDM_ERR_COM_INVCMD = -103;              /*与HOST通讯的命令ID错误*/
		public static int SLDM_ERR_SOCKET = -104;              /*HOST库中，SOCKET初始化失败*/
		public static int SLDM_ERR_SHM = -105;             /*HOST库中，SHM初始化失败*/
		public static int SLDM_ERR_PIPE = -106;                /*HOST库中，PIPE初始化失败*/
		public static int SLDM_ERR_NOLIBINITD = -107;              /*HOST库没有初始化*/
		public static int SLDM_ERR_NOMCOPEN = -108;                /*控制器未打开,或控制器关闭失败*/
		public static int SLDM_ERR_MCOPEND = -109;             /*HOST库中，控制器已打开*/
		public static int SLDM_ERR_MC_NONCONNECT = -110;               /*HOST与控制器通讯超时，未连接*/
		public static int SLDM_ERR_MC_CONNECTING = -111;               /*HOST与控制器正在连接*/
		public static int SLDM_ERR_THREAD = -112;              /*HOST库中，线程初始化失败*/
		public static int SLDM_ERR_BUFFIDX = -113;             /*命令缓冲区索引号错误*/
		public static int SLDM_ERR_REFMESSAGE = -114;              /*收到错误报文*/
		public static int SLDM_ERR_PREBUFFFULL = -115;             /*标准命令块的预缓冲区满*/
        public static int SLDM_ERR_BUFFFULL = -116;			/*标准命令块缓冲区满*/

        public static int SLDM_ERR_FB_TIMEOUT = -121;				/*伺服现场总线超时*/
        public static int SLDM_ERR_FB_NCYCWNG = -122;				/*伺服总线中非周期命令执行报警*/
        public static int SLDM_ERR_FB_CCYCERR = -123;				/*伺服总线中非周期命令执行错误*/

		public static int SLDM_ERR_SERVICE_STOP = -230;                /*控制器服务程序停止*/
		public static int SLDM_ERR_UPDATE = -255;              /*内部使用，更新命令*/

		//@Begin 返回错误ID的函数名称 编码
		public static int SLDM_OPEN_CODE = 0X01;   //1
		public static int SLDM_CLOSE_CODE = 0X02;  //2
		public static int SLDM_E2PROM_WRITE_CODE = 0X06;   //6
		public static int SLDM_E2PROM_READ_CODE = 0X07;    //7
		public static int SLDM_WRITE_REGCODE_CODE = 0X08;  //8
		public static int SLDM_READ_REGCODE_CODE = 0X09;   //9
		public static int SLDM_EXE2PROM_WRITE_CODE = 0X0A; //10
		public static int SLDM_EXE2PROM_READ_CODE = 0X0B;  //11
        public static int SLDM_FLASH_WRITE_CODE   =	0X0C;	//12
        public static int SLDM_ERASE_NEWAPP = 0X0D;	//13
		public static int SLDM_SET_COMM_PERIOD = 0X0E; //14
		public static int SLDM_SET_DEBUG_MODE_CODE = 0X0F; //15

		public static int SLDM_SET_DIR_POL_CODE = 0X10;    //16
		public static int SLDM_SET_ENC_POL_CODE = 0X11;    //17
		public static int SLDM_SET_CPOS_CODE = 0X12;   //18
        public static int SLDM_SET_ENC_ENABLE = 0X13;	//19
		public static int SLDM_SET_ENC_CODE = 0X14;    //20
		public static int SLDM_SET_SERVOON_CODE = 0X1B;    //27
		public static int SLDM_SET_RSTALM_CODE = 0X1D; //29

		public static int SLDM_SET_PLIMIT_CONFIG_CODE = 0X20;  //32
		public static int SLDM_SET_NLIMIT_CONFIG_CODE = 0X21;  //33
		public static int SLDM_SET_ORG_CONFIG_CODE = 0X22; //34
		public static int SLDM_SET_SERVOALM_CONFIG_CODE = 0X23;    //35
		public static int SLDM_SET_SOFT_LIMIT_CODE = 0X24; //36
		public static int SLDM_RESET_CODE = 0X2E;  //46
		public static int SLDM_CLEAR_ALARM_HISTORY_CODE = 0X30;    //48
		public static int SLDM_SET_ENCZ_POL_CODE = 0X32;   //50
		public static int SLDM_SET_EMG_CONFIG_CODE = 0X33; //51
		public static int SLDM_SET_PULSE_MODE_CODE = 0X35; //53
		public static int SLDM_SET_PULSE_POL_CODE = 0X36;  //54
		public static int SLDM_SET_ENC_FILTER_CODE = 0X37; //55
		
		public static int SLDM_SET_SPEC_INPUT_FILTER_CODE = 0X39;  //57
		public static int SLDM_SET_AXIS_INPUT_FILTER_CODE = 0X3A;  //58

        public static int SLDM_PTPN_GROUP	=	0x3E;	//62
        public static int SLDM_PTPN_STOP_GROUP = 0x3F;	//63

		public static int SLDM_SET_HOME_PARA_CODE = 0X50;  //80
		public static int SLDM_JOGA_CODE = 0X51;   //81
		public static int SLDM_PTP1_CODE = 0X52;   //82
		public static int SLDM_PTP1R_CODE = 0X53;  //83
		public static int SLDM_JOGP_CODE = 0X54;   //84
		public static int SLDM_JOGM_CODE = 0X55;   //85
		public static int SLDM_SET_HOME_OFT_CODE = 0X56;   //86
		public static int SLDM_STOP_CODE = 0X58;   //88
		public static int SLDM_ESTOP_CODE = 0X59;  //89
		public static int SLDM_CHANGE_POS_CODE = 0X5A; //90
		public static int SLDM_CHANGE_VEL_CODE = 0X5B; //91
		public static int SLDM_PTPN_CODE = 0X5C;   //92
		public static int SLDM_PTPN_STOP_CODE = 0X5D;  //93
		public static int SLDM_PMOVE_EXTERN_CODE = 0X5E;   //94
		public static int SLDM_HOME_MOVE_CODE = 0X5F;  //95

		public static int SLDM_SET_BUFF_ENABLE_CODE = 0X60;    //96
		public static int SLDM_START_BUFF_MOVE_CODE = 0X61;    //97
		public static int SLDM_ADD_BUFF_SEGMENT_CODE = 0X62;   //98
		public static int SLDM_RESET_BUFF_SEGMENT_P_CODE = 0X67;   //103
		public static int SLDM_SET_BUFF_MOVE_CURENT_P_CODE = 0X68; //104
		public static int SLDM_STOP_BUFF_MOVE_CODE = 0X69; //105
		public static int SLDM_SET_BUFF_MOVE_SINGLE_STEP_CODE = 0X6A;  //106
		public static int SLDM_RESET_BUFF_MOVE_CODE = 0X6B;    //107
		public static int SLDM_SETBUFFLOOP_CODE = 0X6D;    //109
		public static int SLDM_BUFF_WAIT_INPUT_CODE = 0X6E;    //110
		public static int SLDM_BUFF_DELAY = 0X6F;  //111

        public static int SLDM_TOGGLE_OUTBIT = 0X70;	//112
		public static int SLDM_SETOUTBIT_CODE = 0X73;  //115
		public static int SLDM_SETOUTBYTE_CODE = 0X76; //118
        public static int SLDM_SET_DI_FILTER_CODE = 0x7B;  //123
		public static int SLDM_SETOUTWORD = 0X7C;  //124
		public static int SLDM_SETOUTDWORD = 0X7D; //125
        public static int SLDM_IO_TRIGGER = 0x7E;	//126
		public static int SLDM_REVERSE_OUTBIT = 0X7F;  //127

		public static int SLDM_ARC_2D_CODE = 0X80; //128
		public static int SLDM_ARC_RADIUS_CODE = 0X81; //129
		public static int SLDM_ARC_STOP_CODE = 0X82;   //130
		public static int SLDM_ADD_MIX_SEGMENT_CODE = 0X83;    //131
		public static int SLDM_ADD_MIX_SEG_CODE = 0X84;    //132
		public static int SLDM_BUFF_DELAY_OUTBIT_TO_START_CODE = 0X85; //133
		public static int SLDM_BUFF_DELAY_OUTBIT_TO_STOP_CODE = 0X86;  //134
		public static int SLDM_BUFF_AHEAD_OUTBIT_TO_STOP_CODE = 0X87;  //135
		public static int SLDM_BUFF_CLEAR_IO_ACTION_CODE = 0X88;   //136
		public static int SLDM_ARC_3POINTS_CODE = 0X8A;    //138
		public static int SLDM_SET_GEAR_FOLLOW_CODE = 0X8B;    //139
		public static int SLDMPI_ARC_2DEX_CODE = 0X8C; //140
        public static int SLDM_PTPN_PLUS_CODE = 0X8E;//142
        public static int SLDM_PTPN_STOP_PLUS_CODE = 0X8F;	//143

		public static int SLDM_CMPR_ENABLE_CODE = 0X90;    //144
		public static int SLDM_CMPR_ADD_POINT_CODE = 0X91; //145
		public static int SLDM_CLEAR_CMPR_CODE = 0X93; //147
		public static int SLDM_START_CMPR_CODE = 0X94; //148
		public static int SLDM_COMPARELINEAR_CODE = 0X96;  //150
		public static int SLDM_CMPR_MOD_CODE = 0X97;   //151
		public static int SLDM_CMPR_2D_CODE = 0X98;    //152
		public static int SLDM_START_LATCH_CODE = 0X99;    //153
		public static int SLDM_SET_LATCH_PARA_CODE = 0X9A; //154
		public static int SLDM_CLEAR_LATCH_FIFO_CODE = 0X9C;   //156
        public static int SLDM_CMPR_3D_CODE = 0X9F;	//159

		public static int SLDM_PTINIT_CODE = 0XA0; //160
		public static int SLDM_PRFPT_CODE = 0XA1;  //161
		public static int SLDM_PTSTART_CODE = 0XA3;    //163
		public static int SLDM_PTDATA_CODE = 0XA4; //164
		public static int SLDM_PTCLEAR_CODE = 0XA5;    //165
		public static int SLDM_SETPTLOOP_CODE = 0XA6;  //166
		public static int SLDM_SETPTMEMORY_CODE = 0XAB;    //171

		public static int SLDM_SET_PWM_ENABLE_CODE = 0XB0; //176
		public static int SLDM_PWM_START_CODE = 0XB1;  //177
		public static int SLDM_SET_PWM_CONFIG_CODE = 0XB2; //178
															//public static int	SLDM_SET_GEAR_FOLLOW_CODE		=			0XB6;	//182(命中断点问题)
		public static int SLDM_BUFF_SET_PWM_OUTPUT = 0XB4; //180
		public static int SLDM_BUFF_SET_PWM_ONOFF_DUTY = 0XB5; //181
		public static int SLDM_BUFF_SET_PWM_FOLLOW_SPEED = 0XB6;   //182
		public static int SLDM_BUFF_DELAY_PWM_TO_START = 0XB7; //183
		public static int SLDM_BUFF_AHEAD_PWM_TO_STOP = 0XB8;  //184

        public static int SLDM_SET_HANDWHEEL_CONFIG	= 0XBD;//189
        public static int SLDM_SET_HANDWHEEL_MAXVEL	= 0XBE;	//190
        public static int SLDM_SET_HANDWHEEL_ENABLE	= 0XBF;	//191

        public static int SLDM_SET_ECAT_AXIS = 0XC0;	//192
        public static int SLDM_ECAT_RESET = 0XC1;	//193


		public static int SLDM_TEST_FUNCTION_CODE = 0XFF;  //255
															//@End 返回错误ID的函数名称 编码=

		//@Begin PT运动模式
		public static int PT_MODE_STATIC = 0;//（该宏定义为 0）静态模式。默认为该模式。
		public static int PT_MODE_DYNAMIC = 1;//（该宏定义为 1）动态模式
		public static int PT_SEGMENT_NORMAL = 0;//（该宏定义为 0）普通段。默认为该类型。
		public static int PT_SEGMENT_EVEN = 1;//（该宏定义为 1）匀速段。
		public static int PT_SEGMENT_STOP = 2;//（该宏定义为 2）减速到 0 段。
		public static int PT_SEGMENT_END = 3;// 自定义结束标志
											  //@End PT运动模式

		public static int MAX_ALM_BUFFER_NUM = 256;

		//卡类型

        public static int E450A = 450;
        public static int E850 = 850;
        public static int E1250 = 1250;
        public static int E1650 = 1650;
        public static int E5080 = 5080;
        public static int E5160 = 5160;

        public static int E6308 = 6308;
        public static int E6508 = 6508;
        public static int E6408 = 6408;
        public static int E6316 = 6316;
        public static int E6516 = 6516;
        public static int E6416 = 6416;
        public static int E6332 = 6332;
        public static int E6532 = 6532;
        public static int E6432 = 6432;
        public static int E6364 = 6364;
        public static int E6564 = 6564;
        public static int E6464 = 6464;

        [StructLayout(LayoutKind.Sequential)]
        public struct CARD_INFO
        {
            public uint Card_ID;
            public uint Lib_SoftVer;
            public uint Arm_SoftVer;
            public uint Arm_GitCommit;
            public uint FPGA_SoftVer;
            public uint FPGA_GitCommit;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public uint[] FPGA_UID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public uint[] Arm_UID;
            public uint PCB_Ver;
            public uint PCB_GitCommit;
            public uint BOM_Ver;
            public uint BOM_GitCommit;
            public uint Slave_Type;

        }


		public struct Axis_Para
		{
            public bool servo_alarm_en;
            public bool Plimit_en;
            public bool Nlimit_en;
            public bool origin_en;

            public bool servo_alarm_pol;
            public bool Plimit_pol;
            public bool Nlimit_pol;
            public bool origin_pol;

            public bool Dir_pol;
            public bool Enc_pol;
            public bool Encz_pol;

            public bool soft_Plimit_en;
            public bool soft_Nlimit_en;
            public bool pulse_Pol;
			public byte Pulse_Mode;//2 bit

            public int soft_Plimit_pos;
            public int soft_Nlimit_pos;
            public int home_oft_pos;
            public int i_zcap_pulse;//E516 @
		}


		public struct Axis_Status
		{
			public byte servo_alarm_vol;
			public byte Plimit_vol;
			public byte Nlimit_vol;
			public byte origin_vol;
			public byte Encz_vol;
			public byte reached_vol;
			public byte servon;
			public byte resalm;

			public byte servo_alarm_valid;
			public byte Plimit_valid;
			public byte Nlimit_valid;
			public byte origin_valid;
			public byte Encz_valid;
			public byte reached_valid;
			public byte soft_Plimit_valid;
			public byte soft_Nlimit_valid;

			public byte homing;
			public byte homed;
			public byte running;

			public byte alarm_status;//0x01正限位，0x02负限位，0x04 soft_plimit,0x08 soft_nlimit,0x10伺服报警
			public byte home_flag;

		}


        [StructLayout(LayoutKind.Sequential)]
        public struct Alarm_History
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public int[] alarm_number;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public int[] alarm_para;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public int[] time_tick;

        }

        public struct Home_Para
        {
            public UInt32 h_dir;
            public UInt32 mode;
            public float low_vel;
            public float high_vel;
            public float acc;
            public UInt32 enc_en;
        }

		public delegate void fn();

		[StructLayout(LayoutKind.Sequential)]
		public struct Jump_Data
		{
			public UInt32 axis_count;               //3,固定3轴
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public uint[] axis_list;     //当前指令最多可以对3个轴进行操作;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public int[] target_pos;         //最终目标位置，绝对坐标
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public int[] start_pos;          //起点位置，绝对坐标
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public short[] soft_en;               //是否启用软启动和软着陆标志，soft_en[0]-软启动,soft_en[0]-软着陆。
			public int soft_upZPos;               //抬起过程中，软启动Z轴位置数据
			public int soft_downZPos;             //下降过程中，软着陆Z轴位置数据
			public float soft_upVel;               //抬起过程中，软启动终点速度
			public float soft_downVel;             //下降过程中，软着陆终点速度
			public int z_sofePos;                 //Z轴在下降和抬起过程中，安全位置
			public int z_steadyPos;               //Z轴横移高度
			public float start_vel;                //整个门运动的起始速度
			public float end_vel;                  //整个门运动的终点速度
			public float steady_vel;               //整个门运动的最大速度
			public float move_acc;                 //运动过程中的加速度，pulse/ms^2
			public float move_dec;                 //运动过程中的加速度，pulse/ms^2
		}


		/**********************************************************************************
		* @Begin E450
		***********************************************************************************/
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置控制器级联数量及型号列表
		* @param card_num：   级联控制器数量.
		*        type：		  控制器级联型号列表数组
		*						450-E450
		*						6364-E6364
		*						6564-E6564
		*						......
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Card_List(UInt32 IP_4,UInt32 card_num,  UInt32[] type);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器级联数量及型号列表
		* @param card_num：   返回级联控制器数量.
		*        type：		  返回控制器级联型号列表数组
		*						450-E450
		*						6364-E6364
		*						6564-E6564
		*						......
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Get_Card_List(UInt32 IP_4, ref UInt32 card_num, [Out] UInt32[] type);
        //public static extern int SLDM_Get_Card_List(ref UInt32 card_num, ref UInt32[] type);
		/**********************************************************************************
		* @End E450
		***********************************************************************************/


		/***************************************************************************************************
		*		函数接口
		***************************************************************************************************/

		/**********************************************************************************
		* @Begin 控制器操作
		***********************************************************************************/

		//********************************************************************************************************************************************************************
		/**
		* @brief 打开控制器
		* @param card_no：   级联的卡号
		*        type：      以太网传输方式,取值范围：
		*						0-UDP (默认)
		*						1-wincap
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Open(UInt32 IP_4, UInt32 card_no, UInt32 type = 0);

		//********************************************************************************************************************************************************************
		/**
		* @brief 关闭控制器
		* @param card_no：   0
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Close(UInt32 card_no);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置控制器最小通讯周期
		* @param card_no：	0
		*		 period：	最小通讯周期模式：单位us，取值范围：0-1000.默认值800.
		* @return 成功返回0.
		* @remark 调试模式时，控制器不会因通讯周期过长而报警。
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Comm_Period(UInt32 card_no, UInt32 period);//us
																					//********************************************************************************************************************************************************************
		/**
		* @brief 设置控制器调试模式
		* @param card_no：	0
		*		 model：	模式：0-非调试模式，1-调试模式
		* @return 成功返回0.
		* @remark 调试模式时，控制器不会因通讯周期过长而报警。
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Debug_Mode(UInt32 card_no, UInt32 mode);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器心跳
		* @param card_no：	0
		*		 ticks：	返回控制器心跳值
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_SysTicks(UInt32 card_no, ref UInt32 ticks);

		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器的连接状态
		* @param card_no：	级联的卡号
		*		 status：	返回控制器的连接状态：
		*						0-已连接
		*						-1-未连接
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_ConnStatus(UInt32 card_no, ref int status);

		/**********************************************************************************
		* @End 控制器操作
		***********************************************************************************/

		/**********************************************************************************
		* @Begin 轴参数、状态读写
		***********************************************************************************/

		//********************************************************************************************************************************************************************
		/**
		* @brief 设置脉冲输出模式
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 mode：		模式值：
		*						0-脉冲+方向
		*						1-AB项
		*						2-双脉冲
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Pulse_Mode(UInt32 card_no, UInt32 axis, UInt32 mode);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴脉冲极性
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pol：		极性值：
		*						0-正极性 (默认)
		*						1-负极性
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Pulse_Pol(UInt32 card_no, UInt32 axis, UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴脉冲方向极性
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pol：		极性值：
		*						0-正方向 (默认)
		*						1-负方向
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Dir_Pol(UInt32 card_no, UInt32 axis, UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴编码器方向极性
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pol：		极性值：
		*						0-正极性 (默认)
		*						1-负极性
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Enc_Pol(UInt32 card_no, UInt32 axis, UInt32 pol);
        //********************************************************************************************************************************************************************
        /**
        * @brief 设置轴编码器使能
        * @param card_no：	级联的卡号
        *		 axis：		轴选择(0-3)
        *		 en：		使能值：
        *						0-不使能
        *						1-使能 (默认)
        * @return 成功返回0.
        * @remark
        */
        [DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Set_Enc_Enable(UInt32 card_no, UInt32 axis, UInt32 en);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置读取编码器计数倍率
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 mult：		计数倍率值，默认值4（4倍频）.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Enc_Mult(UInt32 card_no, UInt32 axis, UInt32 mult);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴当前位置
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		位置 ，单位：脉冲.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Cpos(UInt32 card_no, UInt32 axis, int pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴当前位置
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		返回当前位置 ，单位：脉冲.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Pos(UInt32 card_no, UInt32 axis, ref Int32 pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴当前编码器位置
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		位置 ，单位：脉冲.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Enc(UInt32 card_no, UInt32 axis, int pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴当前编码器位置
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		返回当前编码器位置 ，单位：脉冲.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Get_Enc(UInt32 card_no, UInt32 axis, ref Int32 pos);


		/*
		         * @brief 读取控制器级联数量及型号列表
		         * SLDM_Get_Card_List_IP 此函数暂时无法引用
		         * 
        * @param 
        *		 IP_1：      主机IP地址第1位        
        *		 IP_2：      主机IP地址第2位        
        *		 IP_3：      主机IP地址第3位
        *		 IP_4：      主机IP地址第4位
        *		 ip_4：      卡ip地址第四位      		 
        *        cardlist_num：   返回级联控制器数量.
        *        type：		  返回控制器级联型号列表数组
        *						450-E450
        *						6364-E6364
        *						6564-E6564
        *						......
        * @return 成功返回0.
        * @remark
		 */
		//sldmv.SLDM_Get_Card_List_IP(PC_IP_1, PC_IP_2, PC_IP_3, PC_IP_4, ip_4, &cardlist_num, type);
		//public static extern long SLDM_Get_Card_List(ref ulong card_num, ref UInt32[] type);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Card_List_IP(UInt32 IP_1, UInt32 IP_2, UInt32 IP_3, UInt32 IP_4, UInt32 ip_4, ref UInt32 cardlist_num, [Out] UInt32[] type);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置编码器Z信号参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pol：		有效电平，0-低电平，1-高电平
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_EncZ_Pol(UInt32 card_no, UInt32 axis, UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取编码器Z信号电平
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 valid：	返回编码器z信号有效状态，0-无效，1-有效.
		*		 el：		返回编码器z信号电平，0-低电平，1-高电平.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Enc_Z(UInt32 card_no, UInt32 axis, ref UInt32 valid, ref UInt32 el);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置急停参数
		* @param card_no：	级联的卡号
		*		 en：		使能 0-不使能，1-使能 (默认不使能)
		*		 pol：		急停有效值，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_EMG_Config(UInt32 card_no, UInt32 en, UInt32 pol);

		//********************************************************************************************************************************************************************
		/**
		* @brief 读取急停参数
		* @param card_no：	级联的卡号
		*		 en：		使能 0-不使能，1-使能 
		*		 pol：		急停有效值，0-输入不导通，1-输入导通 
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_EMG_Config(UInt32 card_no, ref UInt32 en, ref UInt32 pol);

		//********************************************************************************************************************************************************************
		/**
		* @brief 读取急停信号状态
		* @param card_no：	级联的卡号
		*		 valid：	返回编码器z信号有效状态，0-无效，1-有效.
		*		 el：		返回编码器z信号电平，0-低电平，1-高电平.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_EMG(UInt32 card_no, ref UInt32 valid, ref UInt32 el);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置各轴伺服使能状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 on：		使能端口导通状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_ServoOn(UInt32 card_no, UInt32 axis, UInt32 on);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴伺服使能状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 on：		返回使能端口导通状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_ServoOn(UInt32 card_no, UInt32 axis, ref UInt32 on);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置各轴伺服报警清除状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 on：		报警清除端口导通状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_RstAlm(UInt32 card_no, UInt32 axis, UInt32 on);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴伺服报警清除状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 on：		报警清除端口导通状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_RstAlm(UInt32 card_no, UInt32 axis, ref UInt32 on);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴正限位参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		使能 0-不使能，1-使能 (默认不使能)
		*		 pol：		限位有效值，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Plimit_Config(UInt32 card_no, UInt32 axis, UInt32 en, UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴正限位参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		返回使能状态 0-不使能，1-使能 (默认不使能)
		*		 pol：		返回限位有效值状态，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Plimit_Config(UInt32 card_no, UInt32 axis, ref UInt32 en, ref UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴正向限位开关状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 valid：	返回限位有效状态值 0-无效，1-有效
		*		 el：		返回限位接口导通状态值0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_POT(UInt32 card_no, UInt32 axis, ref UInt32 valid, ref UInt32 el);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴负限位参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		使能 0-不使能，1-使能 (默认不使能)
		*		 pol：		限位有效值，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Nlimit_Config(UInt32 card_no, UInt32 axis, UInt32 en, UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴负限位参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		返回使能状态 0-不使能，1-使能 (默认不使能)
		*		 pol：		返回限位有效值状态，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Nlimit_Config(UInt32 card_no, UInt32 axis, ref UInt32 en, ref UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴负向限位开关状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 valid：	返回限位有效状态值 0-无效，1-有效
		*		 el：		返回限位接口导通状态值0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_NOT(UInt32 card_no, UInt32 axis, ref UInt32 valid, ref UInt32 el);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴原点参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		使能 0-不使能，1-使能 (默认不使能)
		*		 pol：		原点有效值，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Org_Config(UInt32 card_no, UInt32 axis, UInt32 en, UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴原点参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		返回使能状态 0-不使能，1-使能 (默认不使能)
		*		 pol：		返回原点有效值状态，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Org_Config(UInt32 card_no, UInt32 axis, ref UInt32 en, ref UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴原点开关状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 valid：	返回原点开关有效状态值 0-无效，1-有效
		*		 el：		返回原点开关接口导通状态值0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Org(UInt32 card_no, UInt32 axis, ref UInt32 valid, ref UInt32 el);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴伺服报警参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		使能 0-不使能，1-使能 (默认不使能)
		*		 pol：		轴伺服报警有效值，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_ServoAlm_Config(UInt32 card_no, UInt32 axis, UInt32 en, UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴伺服报警参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		返回使能状态 0-不使能，1-使能 (默认不使能)
		*		 pol：		返回报警有效值状态，0-输入不导通，1-输入导通 (默认1-输入导通)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_ServoAlm_Config(UInt32 card_no, UInt32 axis, ref UInt32 en, ref UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴伺服报警开关状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 valid：	返回伺服报警关有效状态值 0-无效，1-有效
		*		 el：		返回伺服报警接口导通状态值0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Servo_Alarm(UInt32 card_no, UInt32 axis, ref UInt32 valid, ref UInt32 el);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴方向极性参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 Dir_pol：	返回脉冲方向设置状态 0-正方向，1-反方向
		*		 Pulse_Pol：返回脉冲极性设置状态 0-正极性，1-负极性
		*		 Enc_pol：	返回编码器极性设置状态 0-正极性，1-负极性
		*		 Encz_pol：	返回编码器Z信号极性设置状态 0-正极性，1-负极性
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_AxisPol_Config(UInt32 card_no, UInt32 axis, ref UInt32 Dir_pol, ref UInt32 Pulse_Pol, ref UInt32 Enc_pol, ref UInt32 Encz_pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴参数结构
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 p_axispara：返回轴参数结构体
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Axis_Para(UInt32 card_no, UInt32 axis, ref Axis_Para p_axispara);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置编码器滤波时间
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 filter_time：滤波时间，单位us，默认值0.2-20ns.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Enc_Filter(UInt32 card_no, UInt32 axis, float filter_time);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置数字输入口滤波时间
		* @param card_no：	级联的卡号
		*		 input_bit：DI位选择(0-15)
		*		 filter_time：滤波时间，单位us，默认值1-1us.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_DI_Filter(UInt32 card_no, UInt32 input_bit, float filter_time);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置限位、原点、到位输入信号滤波时间
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 filter_time：滤波时间，单位us，默认值1000-1ms.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Spec_Input_Filter(UInt32 card_no, UInt32 axis, float filter_time);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴报警信号滤波时间
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 filter_time：滤波时间，单位us，默认值1000-1ms.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Axis_Input_Filter(UInt32 card_no, UInt32 axis, float filter_time);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴运动的目标位置
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		返回当前运动目标位置 ，单位：脉冲.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_PosT(UInt32 card_no, UInt32 axis, ref int pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴运动的轨迹规划位置
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		返回当前运动轨迹规划位置 ，单位：脉冲.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_PosA(UInt32 card_no, UInt32 axis, ref int pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴运动速度
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 vel：		返回轴当前速度，单位：p/ms
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Vel(UInt32 card_no, UInt32 axis, ref float vel);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取直线插补、圆弧插补运动速度
		* @param card_no：	级联的卡号
		*		 line_vel：	返回直线插补当前速度，单位：p/ms
		*		 arc_vel：	返回圆弧插补当前速度，单位：p/ms
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_ExVel(UInt32 card_no, ref float line_vel, ref float arc_vel);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴限位原点等状态结构体
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 p_axissatus：返回轴状态结构体
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Axis_Status(UInt32 card_no, UInt32 axis, ref Axis_Status p_axissatus);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴到位开关状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 valid：	返回到位开关有效状态值 0-无效，1-有效
		*		 el：		返回到位开关接口导通状态值0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Reached(UInt32 card_no, UInt32 axis, ref UInt32 valid, ref UInt32 el);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴正在运动状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 running：	返回轴运动状态，0-停止，1-运动
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Running(UInt32 card_no, UInt32 axis, ref UInt32 running);
		//********************************************************************************************************************************************************************
		/**
		* @brief 轴复位
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Reset(UInt32 card_no, UInt32 axis);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴综合报警信息
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 p_almstatus：返回各轴报警信息状态
		* @return 成功返回0.
		* @remark 0x01正限位，0x02负限位，0x04 正软限位,0x08 负软限位,0x10伺服报警
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Axis_AlmStatus(UInt32 card_no, UInt32 axis, ref UInt32 p_almstatus);
		//********************************************************************************************************************************************************************
        /**
        * @brief 读取轴历史报警信息
        * @param card_no：	级联的卡号
        *		 p_alarmhistory：返回轴历史报警结构
        * @return 返回历史报警个数.
        * @remark
        */
        [DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Alarm_History(UInt32 card_no, ref Alarm_History p_alarmhistory);
		//********************************************************************************************************************************************************************
		/**
		* @brief 清除轴历史报警信息
		* @param card_no：	级联的卡号
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Clear_Alarm_History(UInt32 card_no);
		//********************************************************************************************************************************************************************
        /**
        * @brief 读取控制器最后一次的历史报警信息
        * @param card_no：	级联的卡号
        *		 p_Alarm_number：返回报警标志
        *		 p_Alarm_para：返回报警参数
        *		 p_time_tick：返回报警次数
        * @return 返回历史报警个数.
        * @remark 0x01正限位，0x02负限位，0x04 正软限位,0x08 负软限位,0x10伺服报警
        */
        [DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_AlarmHistory(UInt32 card_no, [Out] int[] p_Alarm_number, [Out]  int[] p_Alarm_para, [Out]  int[] p_time_tick);

		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴错误信息
		* @param card_no：	级联的卡号
		*		 err：		返回轴错误代码，正常返回0.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Move_Error(UInt32 card_no, ref int err);

		/**********************************************************************************
		* @End 轴参数、状态读写
		***********************************************************************************/

		/**********************************************************************************
		* @Begin 回零
		***********************************************************************************/


		//********************************************************************************************************************************************************************
		/**
		* @brief 设置轴回零偏置参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		回零偏置位置，单位：脉冲.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Home_Oft(UInt32 card_no, UInt32 axis, int pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置软限位参数
		* @param card_no：	级联的卡号
		*		 axis_mask：按位掩码表示的轴选择
		*		 en：		回零偏置位置，单位：脉冲.
		*		 p_pos：	正限位位置，单位：脉冲
		*		 n_pos：	负限位位置，单位：脉冲
		* @return 成功返回0.
		* @remark 软限位在回零完成后生效
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Soft_Limit(UInt32 card_no, UInt32 axis, UInt32 en, int p_pos, int n_pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取各轴软限位参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 en：		返回软限位使能设置状态 0-不使能，1-使能
		*		 p_pos：	返回正软限位位置
		*		 n_pos：	返回负软限位位置
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Soft_Limit(UInt32 card_no, UInt32 axis, ref UInt32 en, ref int p_pos, ref int n_pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置回零参数
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 h_dir：	回零方向，0-正向，1-反向.
		*		 low_vel：	低速速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 high_vel：	高速速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000.
		*		 mode：		回零模式：
		*                       0- 原地回零
		*                       1- 一次原点回零
		*                       2- 一次限位回零
		*                       3- 一次原点回零加回找
		*                       4- 一次限位回零加回找
		*                       5- 两次原点回零
		*                       6- 两次限位回零
		*                       7- EZ 信号回零
		*                       8- 原点回零后再回找 EZ 信号
		*                       9- 限位回零后再回找 EZ 信号
		*                       10- 原点锁存
		*                       11- 限位锁存
		*                       12- EZ信号 锁存
		*                       13- 原点回零加反向 EZ 锁存
		*                       14- 限位回零加反向 EZ 锁存
		*		 enc_en：	设置编码器有效，0-无效，1-有效
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Home_Para(UInt32 card_no, UInt32 axis, UInt32 h_dir, float low_vel, float high_vel, float acc, UInt32 mode, UInt32 enc_en);

		//********************************************************************************************************************************************************************
		/**
		* @brief 启动回零运动
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Home_Move(UInt32 card_no, UInt32 axis);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取轴回零状态
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 homing：	返回轴回零中状态，0-不在回零过程中，1-在回零过程中
		*		 homed：	返回轴回零完成状态，0-回零未完成，1-回零完成
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Homing(UInt32 card_no, UInt32 axis, ref UInt32 homing, ref UInt32 homed);

		/**********************************************************************************
		* @End  回零
		***********************************************************************************/


		/**********************************************************************************
		* @Begin 单轴运动
		***********************************************************************************/

		//********************************************************************************************************************************************************************
		/**
		* @brief 启动绝对式 P2P 运动
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		目标位置，单位 pulse，范围-2 31 ~2 31
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 sine：		加速曲线，0-T型，1-S型
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_JogA(UInt32 card_no, UInt32 axis, int pos, float start_vel, float steady_vel, float acc, UInt32 sine);
		//********************************************************************************************************************************************************************
		/**
		* @brief 启动绝对式 P2P 运动（非对称点位运动）
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		目标位置，单位 pulse，范围-2 31 ~2 31
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 sine：		加速曲线，0-T型，1-S型
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTP1(UInt32 card_no, UInt32 axis, Int32 pos, float start_vel, float steady_vel, float end_vel, float acc, float dec, UInt32 sine);

		//********************************************************************************************************************************************************************
		/**
		* @brief 启动相对式 P2P 运动（非对称点位运动）
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		相对目标位置，单位 pulse，范围-2 31 ~2 31
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 sine：		加速曲线，0-T型，1-S型
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTP1R(UInt32 card_no, UInt32 axis, int pos, float start_vel, float steady_vel, float end_vel, float acc, float dec, UInt32 sine);
		//********************************************************************************************************************************************************************
		/**
		* @brief 启动正向连续运动
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加(减)速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_JogP(UInt32 card_no, UInt32 axis, float start_vel, float steady_vel, float acc);
		//********************************************************************************************************************************************************************
		/**
		* @brief 启动负向连续运动
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加(减)速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_JogM(UInt32 card_no, UInt32 axis, float start_vel, float steady_vel, float acc);
		//********************************************************************************************************************************************************************
		/**
		* @brief 轴运动减速停止，针对 P2P、Jog、Home 等运动
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 stop_dec：	减速停止减速度，如果为零则按照默认减速度停止.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Stop(UInt32 card_no, UInt32 axis, float stop_dec);
		//********************************************************************************************************************************************************************
		/**
		* @brief 轴运动立即停止，针对 P2P、Jog、Home 等运动
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Estop(UInt32 card_no, UInt32 axis);
		//********************************************************************************************************************************************************************
		/**
		* @brief 改变轴运动终点位置
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 pos：		目标位置，单位 pulse，范围-2 31 ~2 31
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Change_Pos(UInt32 card_no, UInt32 axis, int pos);
		//********************************************************************************************************************************************************************
		/**
		* @brief 改变轴运动速度
		* @param card_no：	级联的卡号
		*		 axis：		轴选择(0-3)
		*		 vel：		目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Change_Vel(UInt32 card_no, UInt32 axis, float vel);
		/**********************************************************************************
		* @End 单轴运动
		***********************************************************************************/


		/**********************************************************************************
		* @Begin 多轴运动
		***********************************************************************************/
		//********************************************************************************************************************************************************************
		/**
		* @brief 多轴直线点位运动
		* @param card_no：	级联的卡号
		*		 n_axis：	运动轴个数1-7
		*		 p_axis：	轴号数组
		*		 pos：		各轴目标位置数组，单位 pulse，范围-2 31 ~2 31
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000		
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000		
		*		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000	
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 sine：		加速曲线，0-T型，1-S型		
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_PTPn(UInt32 card_no, UInt32 n_axis, UInt32[] axislist, Int32[] pos, float start_vel, float steady_vel, float end_vel, float acc, float dec, UInt32 sine);
		//********************************************************************************************************************************************************************
		/**
		* @brief 多轴直线点位运动
		* @param card_no：	级联的卡号
		*		 stop_dec：	减速停止减速度，如果为零则按照默认减速度停止.	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTPn_Stop(UInt32 card_no, float stop_dec);
        //********************************************************************************************************************************************************************
        /**
        * @brief 带组号的多轴直线点位运动
        * @param card_no：	级联的卡号
        *		 group_id：	组号，区别于其他组号，每组独立运动，停止.范围：0-最大轴数/2
        *		 n_axis：	运动轴个数1-7
        *		 p_axis：	轴号数组
        *		 pos：		各轴目标位置数组，单位 pulse，范围-2 31 ~2 31
        *		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000		
        *		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000		
        *		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000	
        *		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
        *		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
        *		 sine：		加速曲线，0-T型，1-S型		
        * @return 成功返回0.
        * @remark
        */
        [DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_PTPn_Group(UInt32 card_no, UInt32 group_id, UInt32 n_axis, UInt32[] axislist, Int32[] pos, float start_vel, float steady_vel, float end_vel, float acc, float dec, UInt32 sine);

		/// <summary>
		/// 打开控制卡
		/// </summary>
		/// <param name="PC_IP_1"></param>主机地址 1
		/// <param name="PC_IP_2"></param>主机地址 2
		/// <param name="PC_IP_3"></param>主机地址 3
		/// <param name="PC_IP_4"></param>主机地址 4
		/// <param name="ip_4"></param>从机/卡 ip地址 4
		/// <returns> 成功返回 0
		/// </returns>
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Open_IP(UInt32 PC_IP_1, UInt32 PC_IP_2, UInt32 PC_IP_3, UInt32 PC_IP_4, UInt32 ip_4);
		//********************************************************************************************************************************************************************
		/**
        * @brief 停止多轴直线点位运动
        * @param card_no：	级联的卡号
        *		 group_id：	组号，区别于其他组号，每组独立运动，停止.范围：0-最大轴数/2
        *		 stop_dec：	减速停止减速度，如果为零则按照默认减速度停止.	
        * @return 成功返回0.
        * @remark
        */
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_PTPn_Stop_Group(UInt32 card_no,UInt32 group_id,float stop_dec);
        //********************************************************************************************************************************************************************
        /**
        * @brief 多轴直线点位运动（扩展升级，虚拟轴规划方式）
        * @param card_no：	级联的卡号
        *		 n_axis：	运动轴个数1-7
        *		 p_axis：	轴号数组
        *		 pos：		各轴目标位置数组，单位 pulse，范围-2 31 ~2 31
        *		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000		
        *		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000		
        *		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000	
        *		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
        *		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
        *		 sine：		加速曲线，0-T型，1-S型		
        * @return 成功返回0.
        * @remark 与SLDM_PTPn_Stop_Plus 配合使用
        */
        [DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_PTPn_Plus(UInt32 card_no, UInt32 n_axis, UInt32[] axislist, Int32[] pos, float start_vel, float steady_vel, float end_vel, float acc, float dec, UInt32 sine);
        //********************************************************************************************************************************************************************
        /**
        * @brief 停止多轴直线点位运动（扩展升级，虚拟轴规划方式）
        * @param card_no：	级联的卡号
        *		 stop_dec：	减速停止减速度，如果为零则按照默认减速度停止.	
        * @return 成功返回0.
        * @remark 与SLDM_PTPn_Plus 配合使用
        */
        [DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_PTPn_Stop_Plus(UInt32 card_no,float stop_dec);
        
		/**********************************************************************************
		* @End 多轴运动
		***********************************************************************************/


		/**********************************************************************************
		* @Begin 圆弧运动
		***********************************************************************************/

		//********************************************************************************************************************************************************************
		/**
		* @brief 二维圆弧运动（圆心）
		* @param card_no：	级联的卡号
		*		 axislist：	二维轴号数组	
		*		 center_pos：二维圆心位置数组，单位：脉冲个数		
		*		 end_pos：	二维终点位置，单位：脉冲个数
		*		 ccw_dir：	圆弧方向：0-逆时针，1-顺时针
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Arc_2D(UInt32 card_no,  UInt32[] axislist,  float[] center_pos,  int[] end_pos, UInt32 ccw_dir, float start_vel, float steady_vel, float end_vel, float acc, float dec);
		//********************************************************************************************************************************************************************
		/**
		* @brief 扩展二维圆弧运动（圆心）
		* @param card_no：	级联的卡号
		*		 axislist：	二维轴号数组	
		*		 center_pos：二维圆心位置数组，单位：脉冲个数		
		*		 end_pos：	二维终点位置，单位：脉冲个数
		*		 circle：	圈数，单位：圈，0：一段圆弧		
		*		 oft_pos：	弧长补偿增量长度，单位：脉冲个数
		*		 ccw_dir：	圆弧方向：0-逆时针，1-顺时针
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDMpi_Arc_2DEx(UInt32 card_no,  UInt32[] axislist,  float[] center_pos,  int[] end_pos, int circle, int oft_pos, UInt32 ccw_dir, float start_vel, float steady_vel, float end_vel, float acc, float dec);
		//********************************************************************************************************************************************************************
		/**
		* @brief 二维圆弧运动（半径）
		* @param card_no：	级联的卡号
		*		 axislist：	二维轴号数组			
		*		 end_pos：	二维终点位置，单位：脉冲个数
		*		 arc_Radius：圆弧半径，单位：脉冲个数
		*		 ccw_dir：	圆弧方向：0-逆时针，1-顺时针
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Arc_Radius(UInt32 card_no,  UInt32[] axislist,  int[] end_pos, float arc_Radius, UInt32 ccw_dir, float start_vel, float steady_vel, float end_vel, float acc, float dec);
		//********************************************************************************************************************************************************************
		/**
		* @brief 基于三点二维圆弧插补运动
		* @param card_no：	级联的卡号	
		*		 axislist：	轴号列表
		*		 mid_pos：	中间位置数组，单位：脉冲
		*		 target_pos：目标位置数组，单位：脉冲
		*		 start_vel：起始速度
		*		 steady_vel：最大速度
		*		 end_vel：	终点速度
		*		 acc：		加速度
		*		 dec：		减速度
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Arc_3Points(UInt32 card_no,  UInt32[] axislist,  float[] mid_pos,  int[] target_pos, float start_vel, float steady_vel, float end_vel, float acc, float dec);


		//********************************************************************************************************************************************************************
		/**
		* @brief 二维圆弧运动停止
		* @param card_no：	级联的卡号
		*		 stop_dec：	减速停止减速度，如果为零则按照默认减速度停止.			
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Arc_Stop(UInt32 card_no, float stop_dec);


		/**********************************************************************************
		* @End 圆弧运动
		***********************************************************************************/


		/**********************************************************************************
		* @Begin 高级运动功能
		***********************************************************************************/

		//********************************************************************************************************************************************************************
		/**
		* @brief 软启停动功能
		* @param card_no：	级联的卡号	
		*		 axis：		轴号（0-3）
		*		 mid_pos：	中间点位置，第一段 move 终点位置,单位：pulse
		*		 target_pos：第二段 move 终点位置,单位：pulse
		*		 start_vel：第一段 move 起始速度 
		*		 steady_vel：第一段 move 最大速度
		*		 stop_vel：	第一段 move 停止速度
		*		 delay_ms：	第一阶段完成后延迟时间（单位：毫秒）
		*		 steady_vel2：第二段 move 最大速度
		*		 end_vel：	第二段 move 停止速度
		*		 acc_ms：	加速时间(单位：ms)
		*		 dec_ms：	减速时间(单位：ms)
		*		 posi_mode：运动模式，0：相对模式，1：绝对模式
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Pmove_Extern(UInt32 card_no, UInt32 axis, int mid_pos, int target_pos, float start_vel, float steady_vel, float stop_vel,
								   UInt32 delay_ms, float steady_vel2, float end_vel, UInt32 acc_ms, UInt32 dec_ms, UInt32 posi_mode);



		//********************************************************************************************************************************************************************
        /**
        * @brief 龙门跟随功能
        * @param card_no：	级联的卡号	
        *		 axis：		跟随轴轴号，取值范围：0-3
        *		 enable：	使能状态，0：禁止，1：使能
        *		 master_axis：跟随主轴轴号，取值范围：0-3
        *		 ratio：	跟随倍率
        *		 src：		位置源 0-脉冲位置 1-编码器位置
        *       delay:		跟随轴延时跟随运动延时时间，单位：ms 0-不延时 取值范围：0-1000
        * @return 成功返回0.
        * @remark 
        */
        [DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Set_Gear_Follow(UInt32 card_no, UInt32 axis, UInt32 enable, UInt32 master_axis, float ratio, UInt32 src, UInt32 delay);
		/**********************************************************************************
		* @End 高级运动功能
		***********************************************************************************/

		/**********************************************************************************
		* @Begin 缓冲区运动
		***********************************************************************************/



		//********************************************************************************************************************************************************************
		/**
		* @brief 多轴直线连续运动使能
		* @param card_no：	级联的卡号
		*		 enable：	0-禁能，1-使能	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Buff_Enable(UInt32 card_no, UInt32 enable);
		//********************************************************************************************************************************************************************
		/**
		* @brief 启动多轴直线连续运动
		* @param card_no：	级联的卡号
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Start_Buff_Move(UInt32 card_no);
		//********************************************************************************************************************************************************************
		/**
		* @brief 写入缓冲区直线运动数据
		* @param card_no：	级联的卡号
		*		 n_axis：	运动轴个数（最大4轴）	
		*		 p_axis：	轴号数组	
		*		 pos：		各轴目标位置数组，单位 pulse，范围-2 31 ~2 31
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000		
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000	
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 sine：		加速曲线，0-T型，1-S型
			
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Add_Buff_Segment(UInt32 card_no, UInt32 n_axis,  UInt32[] axislist,  int[] pos, float start_vel, float steady_vel, float end_vel, float acc, float dec, UInt32 sine);
		
		//********************************************************************************************************************************************************************
		/**
		* @brief 得到连续运动当前缓冲区运动段数
		* @param card_no：	级联的卡号
		*		 p：		返回连续运动当前运动段数值	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Buff_Move_Curent_P(UInt32 card_no, ref UInt32 p);
		//********************************************************************************************************************************************************************
		/**
		* @brief 得到连续运动当前总运动段数
		* @param card_no：	级联的卡号
		*		 p：		返回连续运动当前运动总段数值	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Buff_Move_Total_P(UInt32 card_no, ref UInt32 p);
		//********************************************************************************************************************************************************************
		/**
		* @brief 得到连续运动当前缓冲区写入段数
		* @param card_no：	级联的卡号
		*		 p：		返回当前缓冲区写入段数值	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Buff_Segment_P(UInt32 card_no, ref UInt32 p);
		//********************************************************************************************************************************************************************
		/**
		* @brief 得到连续运动当前block缓冲区剩余段数
		* @param card_no：	级联的卡号
		*		 space：		返回当前block缓冲区段剩余段数	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Buff_Block_Space(UInt32 card_no, ref UInt32 space);
		//********************************************************************************************************************************************************************
		/**
		* @brief 复位连续运动当前block缓冲区指针
		* @param card_no：	级联的卡号	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_ReSet_Buff_Segment_P(UInt32 card_no);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置当前运动缓冲区指针位置
		* @param card_no：	级联的卡号
		*		 run_buffer：缓冲区号：0-1	
		*		 curent_p：	运动缓冲区指针位置	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Buff_Move_Curent_P(UInt32 card_no, UInt32 run_buffer, UInt32 curent_p);
		//********************************************************************************************************************************************************************
		/**
		* @brief 缓冲区连续运动减速停止
		* @param card_no：	级联的卡号
		*		 stop_dec：	减速停止减速度，如果为零则按照默认减速度停止.	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Stop_Buff_Move(UInt32 card_no, float stop_dec);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置缓冲区连续单步运动
		* @param card_no：	级联的卡号
		*		 one_step：	0-连续运动，1-单步运动	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Buff_Move_Single_Step(UInt32 card_no, UInt32 one_step);
		//********************************************************************************************************************************************************************
		/**
		* @brief 复位连续运动参数
		* @param card_no：	级联的卡号
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Reset_Buff_Move(UInt32 card_no);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取连续运动正在运行状态
		* @param card_no：	级联的卡号
		*		 running：	返回轴运动状态 0-停止，1-正在运行。	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Running_BuffMove(UInt32 card_no, ref UInt32 running);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置连续运动循环次数
		* @param card_no：	级联的卡号
		*		 loop：		循环次数，0-无限循环。	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Buff_Loop(UInt32 card_no, UInt32 loop);
		//********************************************************************************************************************************************************************
		/**
		* @brief 写入缓冲区混合运动数据
		* @param card_no：	级联的卡号
		*		 n_axis：	运动轴个数（最大4轴）	
		*		 p_axis：	轴号数组	
		*		 pos：		各轴目标位置数组，单位 pulse，范围-2 31 ~2 31
		*		 start_vel：起始速度，单位：pulse /ms，取值范围：0.001~8000.000		
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 end_vel：	终点速度，单位：pulse /ms，取值范围：0.001~8000.000	
		*		 acc：		目标加速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 dec：		目标减速度，单位：pulse /(ms*s) ，取值范围：0.001~8000.000
		*		 sine：		加速曲线，0-T型，1-S型
		*		 out_bit：	运动段终点操作DO位, 取值范围：0-最大DO位，-1为无DO操作	
		*		 out_data：	运动段终点操作DO值，取值范围：0-1
		*	     type：		运动类型 0-直线，1-圆弧
		*	     center_pos：二维圆心位置
		*	     ccw_dir：	圆弧方向：0-逆时针，1-顺时针
		*		 delaytime：终点暂停时间，单位：ms，0为无延时		
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Add_Mix_Segment(UInt32 card_no, UInt32 n_axis,  UInt32[] axislist,  int[] pos, float start_vel, float steady_vel, float end_vel, float acc, float dec, UInt32 sine, UInt32 type,  float[] center_pos, UInt32 ccw_dir);
		//********************************************************************************************************************************************************************
		/**
		* @brief 写入缓冲区混合运动数据（自动加减速）
		* @param card_no：	级联的卡号
		*		 n_axis：	运动轴个数（最大4轴）	
		*		 p_axis：	轴号数组	
		*		 pos：		各轴目标位置数组，单位 pulse，范围-2 31 ~2 31	
		*		 steady_vel：目标速度，单位：pulse /ms，取值范围：0.001~8000.000
		*		 acc：		目标加(减)速度，单位：pulse /(ms*ms) ，取值范围：0.001~8000.000
		*		 sine：		加速曲线，0-T型，1-S型
		*		 out_bit：	运动段终点操作DO位, 取值范围：0-最大DO位，-1为无DO操作	
		*		 out_data：	运动段终点操作DO值，取值范围：0-1
		*	     type：		运动类型 0-直线，1-圆弧
		*	     center_pos：二维圆心位置
		*	     ccw_dir：	圆弧方向：0-逆时针，1-顺时针
		*		 delaytime：终点暂停时间，单位：ms，0为无延时		
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Add_Mix_Seg(UInt32 card_no, UInt32 n_axis,  UInt32[] axislist,  int[] pos, float steady_vel, float acc, UInt32 sine, UInt32 type,  float[] center_pos, UInt32 ccw_dir);
		
		//********************************************************************************************************************************************************************
		/**
		* @brief 连续插补等待 IO 输入
		* @param card_no：	级联的卡号	
		*		 in_bit：	输入口号，取值范围：0~15
		*		 on_off：	导通状态，0：不导通，1：导通
		*		 delaytime：超时时间，单位：ms
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Wait_Input(UInt32 card_no, UInt32 in_bit, UInt32 on_off, UInt32 delaytime);
		//********************************************************************************************************************************************************************
		/**
		* @brief 连续插补中相对于轨迹段起点 IO 滞后输出（段内执行）
		* @param card_no：	级联的卡号	
		*		 out_bit：	输出口号，取值范围：0~15
		*		 on_off：	导通状态，0：不导通，1：导通
		*		 delay_value：滞后值，单位：ms（滞后时间模式）或 pulse（滞后距离模式）
		*		 delay_mode：滞后模式，0：滞后时间，1：滞后距离
		*		 reverse_time：电平输出后的延时翻转时间，单位：ms
		* @return 成功返回0.
		* @remark 设置的 IO 操作，将在该指令的下一条轨迹中起作用
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Delay_Outbit_To_Start(UInt32 card_no, UInt32 out_bit, UInt32 on_off, UInt32 delay_value, UInt32 delay_mode, UInt32 reverse_time);
		//********************************************************************************************************************************************************************
		/**
		* @brief 连续插补中相对于轨迹段终点 IO 滞后输出
		* @param card_no：	级联的卡号	
		*		 out_bit：	输出口号，取值范围：0~15
		*		 on_off：	导通状态，0：不导通，1：导通
		*		 delaytime：滞后时间值，单位：ms
		*		 reverse_time：电平输出后的延时翻转时间，单位：ms
		* @return 成功返回0.
		* @remark 设置的 IO 操作，将在该指令的下一条轨迹中起作用
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Delay_Outbit_To_Stop(UInt32 card_no, UInt32 out_bit, UInt32 on_off, UInt32 delay_time, UInt32 reverse_time);
		//********************************************************************************************************************************************************************
		/**
		* @brief 连续插补中相对于轨迹段终点 IO 提前输出（段内执行）
		* @param card_no：	级联的卡号	
		*		 out_bit：	输出口号，取值范围：0~15
		*		 on_off：	导通状态，0：不导通，1：导通
		*		 ahead_value：提前值，单位：ms（提前时间模式）或 pulse（提前距离模式）
		*		 ahead_mode：提前模式，0：提前时间，1：提前距离
		*		 reverse_time：电平输出后的延时翻转时间，单位：ms
		* @return 成功返回0.
		* @remark 设置的 IO 操作，将在该指令的下一条轨迹中起作用
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Ahead_Outbit_To_Stop(UInt32 card_no, UInt32 out_bit, UInt32 on_off, UInt32 ahead_value, UInt32 ahead_mode, UInt32 reverse_time);
		//********************************************************************************************************************************************************************
		/**
		* @brief 清除段内未执行完的 IO 动作
		* @param card_no：	级联的卡号	
		*		 io_bit：	IO口号
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Clear_Io_Action(UInt32 card_no, UInt32 io_bit);
		//********************************************************************************************************************************************************************
		/**
		* @brief 连续插补中暂停延时指令
		* @param card_no：	级联的卡号	
		*		 delaytime: 终点暂停时间，单位：ms，0为无限延时	
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_delay(UInt32 card_no, int delaytime);

		//********************************************************************************************************************************************************************
		/**
		* @brief 设置缓冲区运动上位机缓存使能
		* @param card_no：	级联的卡号
		*		 enable：	0-禁能，1-使能	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Move_ListEn(UInt32 card_no, UInt32 en);

		//********************************************************************************************************************************************************************
		/**
		* @brief 清空缓冲区运动上位机缓存
		* @param 
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Move_ListClear(UInt32 card_no);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取缓冲区运动上位机缓存指令数量
		* @param space：	返回PC缓冲区缓存指令（等待下载）数量

		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Buff_Move_ListSpace(UInt32 card_no, ref UInt32 space);

		/**********************************************************************************
		* @End 缓冲区运动
		***********************************************************************************/
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置控制器IP地址（默认地址为192.168.100.1）
		* @param card_no：	0
		*		 IP_1：	IP地址第一位，对应默认地址为192
		*		 IP_2：	IP地址第二位，对应默认地址为168
		*		 IP_3：	IP地址第三位，对应默认地址为100
		*		 IP_4：	IP地址第四位，对应默认地址为1
		* @return 成功返回0.
		* @remark 调试模式时，控制器不会因通讯周期过长而报警。
		*/
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_IP(UInt32 card_no, UInt32 IP_1 , UInt32 IP_2 , UInt32 IP_3 , UInt32 IP_4 );
		//********************************************************************************************************************************************************************
		/**********************************************************************************
		* @Begin IO功能
		***********************************************************************************/

		//********************************************************************************************************************************************************************
		/**
		* @brief 按位设置输出DO位的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~63
		*		 data：		IO输出的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_OutBit(UInt32 card_no, UInt32 addr, UInt32 index, UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按位读取IO输出的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~63
		*		 data：		IO输出的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_OutBit(UInt32 card_no, UInt32 addr, UInt32 index, ref UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按位读取IO输入的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输入位索引号，范围0~63
		*		 data：		IO输入的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_InBit(UInt32 card_no, UInt32 addr, UInt32 index, ref UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按字节设置输出DO位的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~7
		*		 data：		IO按字节输出的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_OutByte(UInt32 card_no, UInt32 addr, UInt32 index, UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按字节读取各站的IO输入状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	DI索引号，范围：0~7
		*		 data：		IO输入的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_InByte(UInt32 card_no, UInt32 addr, UInt32 index, ref UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按字节读取IO输出的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~7
		*		 data：		IO输出的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_OutByte(UInt32 card_no, UInt32 addr, UInt32 index, ref UInt32 data);

		//********************************************************************************************************************************************************************
		/**
		* @brief 按字设置输出DO位的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~3
		*		 data：		IO按字输出的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_OutWord(UInt32 card_no, UInt32 addr, UInt32 index, UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按双字设置输出DO位的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~1
		*		 data：		IO按双字输出的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_OutDWord(UInt32 card_no, UInt32 addr, UInt32 index, UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按字读取IO输出的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~3
		*		 data：		IO输出的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Get_OutWord(UInt32 card_no, UInt32 addr, UInt32 index, ref UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按双字读取IO输出的状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~1
		*		 data：		IO输出的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_OutDWord(UInt32 card_no, UInt32 addr, UInt32 index, ref UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按字读取各站的IO输入状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	DI索引号，范围：0~3
		*		 data：		IO输入的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_InWord(UInt32 card_no, UInt32 addr, UInt32 index, ref UInt32 data);
		//********************************************************************************************************************************************************************
		/**
		* @brief 按双字读取各站的IO输入状态
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	DI索引号，范围：0~1
		*		 data：		IO输入的状态，0-不导通，1-导通
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_InDWord(UInt32 card_no, UInt32 addr, UInt32 index, ref UInt32 data);

		//********************************************************************************************************************************************************************
		/**
		* @brief 注册响应输入信号的回调函数(IO中断功能)
		* @param card_no：	级联的卡号
		*		 fn：		回调函数指针
		*		 addr：		从卡号，主卡为0
		*		 in_bit：	DI输入字节的位索引
		*		 edge：		信号边沿，0-下降沿，1-上升沿
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Register_Callback(UInt32 card_no, sldmv.fn cbEvent, UInt32 addr, UInt32 in_bit, UInt32 edge);
		//********************************************************************************************************************************************************************
        /**
        * @brief 取消响应输入信号的回调函数
        * @param card_no：	级联的卡号
        *		 addr：		从卡号，主卡为0
        *		 in_bit：	DI输入字节的位索引
        *		 edge：		信号边沿，0-下降沿，1-上升沿
        * @return 成功返回0.
        * @remark
        */
        [DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Unregister_Callback(UInt32 card_no, UInt32 addr, UInt32 in_bit, UInt32 edge);

		//********************************************************************************************************************************************************************
		/**
		* @brief 按位设置IO 输出延时翻转
		* @param card_no：	级联的卡号
		*		 addr：		从卡号，主卡为0	
		*		 index：	输出位索引号，范围0~63
		*		 data：		IO输出的状态，0-不导通，1-导通
		*		 reverse_time：翻转延时时间(单位：ms)，0-不翻转,取值范围(0-42949672)ms
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Reverse_OutBit(UInt32 card_no, UInt32 addr, UInt32 index, UInt32 data, UInt32 reverse_time);
        //********************************************************************************************************************************************************************
        /**
        * @brief 按位设置IO 延时输出延时翻转
        * @param card_no：	级联的卡号
        *		 addr：		从卡号，主卡为0	
        *		 index：	输出位索引号，范围0~63
        *		 data：		IO输出的状态，0-不导通，1-导通
        *		 delay_time：延时输出时间(单位：ms)，0-不延时
        *		 reverse_time：翻转延时时间(单位：ms)，0-不翻转
        * @return 成功返回0.//
        * @remark
        */
        [DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Toggle_OutBit(UInt32 card_no, UInt32 addr, UInt32 index, UInt32 data, UInt32 delay_time, UInt32 reverse_time);
		/**********************************************************************************
		* @End IO功能
		***********************************************************************************/






		/********************************************************************************
		* @Begin 位置比较
		*******************************************************************************/

		//********************************************************************************************************************************************************************
		/**
		* @brief 位置比较使能
		* @param card_no：	级联的卡号
		*		 fifo：		位置比较器选择，(0-1)		
		*		 en：		使能,0-不使能，1-使能	
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Cmpr_Enable(UInt32 card_no, UInt32 fifo, UInt32 en);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器位置比较使能状态
		* @param card_no：	级联的卡号
		*		 fifo：		位置比较器选择，(0-1)	
		*		 en：		位置比较使能状态，0-不使能，1-使能
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Cmpr_Enable(UInt32 card_no, UInt32 fifo, ref UInt32 en);
		//********************************************************************************************************************************************************************
		/**
		* @brief 添加位置比较数据
		* @param card_no：	级联的卡号
		*		 axis：		轴选择	
		*		 cmpr_pos：	比较位置
		*		 fifo：		位置比较器号，对应输出DO点位 （0-1）
		*		 width：	位置比较输出脉冲宽度，单位10us
		*		 out_level：比较成功时DO口输出状态，0-不导通，1-导通
		*		 src：		位置比较位置源，0-脉冲位置，1-编码器位置。
		* @return 成功返回0.
		* @remark 
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Cmpr_Add_Point(UInt32 card_no, UInt32 axis, int cmpr_pos, UInt32 fifo, UInt32 width, UInt32 out_level, UInt32 src);
		//********************************************************************************************************************************************************************
		/**
		* @brief 得到位置比较位置缓冲区状态
		* @param card_no：	级联的卡号
		*		 fifo：		位置比较器选择，(0-1)		
		*		 space：	缓冲区剩余数据数，最大256位
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Cmpr_Fifo_Space(UInt32 card_no, UInt32 fifo, ref UInt32 space);
		//********************************************************************************************************************************************************************
		/**
		* @brief 清除位置比较位置缓冲区，及比较完成个数
		* @param card_no：	级联的卡号
		*		 fifo：		位置比较器选择，(0-1)	
		*		 fifo_data_en：	位置比较缓冲区清除，0-不清除，1-清除	
		*		 complete_num_en：	位置比较完成个数清除，0-不清除，1-清除
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Clear_Cmpr(UInt32 card_no, UInt32 fifo, UInt32 fifo_data_en, UInt32 complete_num_en);
		//********************************************************************************************************************************************************************
		/**
		* @brief 开始位置比较
		* @param card_no：	级联的卡号
		*		 fifo：		位置比较器选择，(0-1)	
		*		 start：	开始，0-停止，1-开始	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Start_Cmpr(UInt32 card_no, UInt32 fifo, UInt32 start);
		//********************************************************************************************************************************************************************
		/**
		* @brief 得到位置比较完成个数
		* @param card_no：	级联的卡号
		*		 fifo：		位置比较器选择，(0-1)		
		*		 num：		返回位置比较完成个数
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Cmpr_Num(UInt32 card_no, UInt32 fifo, ref UInt32 num);
		//********************************************************************************************************************************************************************
		/**
		* @brief 启动等间距位置比较
		* @param card_no：	级联的卡号
		*		 axis：		轴选择0-4	
		*		 startPos：	开始位置比较位置
		*		 interval：	比较位置间距
		*		 fifo：	位置比较器号，对应输出DO点位 （0-1）
		*		 width：	位置比较输出脉冲宽度，单位10us
		*		 out_level：比较成功时DO口输出状态，0-不导通，1-导通
		*		 src：		位置比较位置源，0-脉冲位置，1-编码器位置。
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Compare_Linear(UInt32 card_no, UInt32 axis, int startPos, int interval, UInt32 fifo, UInt32 width, UInt32 out_level, UInt32 src);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置位置比较模式
		* @param card_no：	级联的卡号
		*		 fifo：		位置比较器选择，(0-1)		
		*		 mod：		位置比较模式，0：一维位置比较，1：等间距位置比较；2：二维位置比较
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Cmpr_Mod(UInt32 card_no, UInt32 fifo, UInt32 mod);
        //********************************************************************************************************************************************************************
        /**
        * @brief 二维位置比较添加位置比较数据
        * @param card_no：	级联的卡号
        *		 axislist：	二维轴选择列表	
        *		 cmpr_pos：	二维比较位置列表
        *		 max_err：	比较位置到位的最大允许误差，取值范围[0,512)，单位：pulse
        *		 threshould：最优算法阈值
        *		 fifo：		位置比较器号，对应输出DO点位 （0-1）
        *		 width：	位置比较输出脉冲宽度，单位10us
        *		 out_level：比较成功时DO口输出状态，0-不导通，1-导通
        *		 src：		位置比较位置源，0-脉冲位置，1-编码器位置。
        * @return 成功返回0.
        * @remark 
        */
        [DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Cmpr_2D(UInt32 card_no, UInt32[] axislist, Int32[] cmpr_pos, UInt32 max_err, UInt32 threshould, UInt32 fifo, UInt32 width, UInt32 out_level, UInt32 src);
        //********************************************************************************************************************************************************************
        /**
        * @brief 三维位置比较添加位置比较数据
        * @param card_no：	级联的卡号
        *		 axislist：	三维轴选择列表	
        *		 cmpr_pos：	三维比较位置列表
        *		 max_err：	比较位置到位的最大允许误差，取值范围[0,512)，单位：pulse
        *		 threshould：最优算法阈值
        *		 fifo：		位置比较器号，对应输出DO点位 （0-1）
        *		 width：	位置比较输出脉冲宽度，单位10us
        *		 out_level：比较成功时DO口输出状态，0-不导通，1-导通
        *		 src：		位置比较位置源，0-脉冲位置，1-编码器位置。
        * @return 成功返回0.
        * @remark 
        */
        [DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Cmpr_3D(UInt32 card_no, UInt32[] axislist, Int32[] cmpr_pos, UInt32 max_err, UInt32 threshould, UInt32 fifo, UInt32 width, UInt32 out_level, UInt32 src);

		/**********************************************************************************
		* @End 位置比较
		***********************************************************************************/


		/**********************************************************************************
		* @Begin 位置锁存
		***********************************************************************************/


		//********************************************************************************************************************************************************************
		/**
		* @brief 开始位置锁存
		* @param card_no：	级联的卡号
		*		 fifo：		位置锁存器选择，(0-1)
		*		 start：	1-启动锁存,0-停止锁存
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Start_Latch(UInt32 card_no, UInt32 fifo, UInt32 start);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置位置锁存参数
		* @param card_no：	级联的卡号
		*		 fifo：		位置锁存缓冲区选择，（位置锁存缓冲区= DI口号）0-1	
		*		 axis：	轴选择 0-7
		*		 src：		位置锁存位置源选择，0-指令位置，1-编码器位置
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_Latch_Para(UInt32 card_no, UInt32 fifo, UInt32 axis, UInt32 src);//,ushort trigger);
																										//********************************************************************************************************************************************************************
		/**
		* @brief 读取位置锁存数据
		* @param card_no：	级联的卡号
		*		 fifo：		位置锁存缓冲区选择，（位置锁存缓冲区= DI口号）0-1	
		*		 pos：		所存位置
		*		 pos_delay：所存输入口电平有效时间
		*		 numofcapt：位置锁存完成个数
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Latch_Pos(UInt32 card_no, UInt32 fifo, int[] pos, [Out] UInt32[] pos_delay, ref UInt32 numofcapt);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取位置锁存数据
		* @param card_no：	级联的卡号
		*		 fifo：		位置锁存器选择，(0-1)	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Clear_Latch_Fifo(UInt32 card_no, UInt32 fifo);
        /**
        * @brief 注册位置锁存回调函数
        * @param card_no：	级联的卡号
        *		 fn：		回调函数	
        *		 fifo：		位置锁存器选择，(0-1)
        *		 hold_time：所存输入口电平有效时间
        * @return 成功返回0.
        * @remark
        */
        [DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern long SLDM_Register_Latch_Callback(UInt32 card_no, sldmv.fn cbEvent, UInt32 fifo, UInt32 hold_time);
        /**
        * @brief 注销位置锁存回调函数
        * @param card_no：	级联的卡号
        *		 fifo：		位置锁存器选择，(0-1)
        * @return 成功返回0.
        * @remark
        */
        [DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern long SLDM_Unregister_Latch_Callback(UInt32 card_no, UInt32 fifo);

		/**********************************************************************************
		* @End 位置锁存
		***********************************************************************************/

		/**********************************************************************************
		* @Begin PT运动
		***********************************************************************************/

		//********************************************************************************************************************************************************************
		/**
		* @brief 复位PT运动，PT模式开始及退出时调用
		* @param card_no：	级联的卡号	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PtInit(UInt32 card_no);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置指定轴为 PT 运动模式
		* @param card_no：	级联的卡号	
		*		 axis：		轴选择(0-3)	
		*		 mode：		PT_MODE_STATIC静态模式，PT_MODE_DYNAMIC 动态模式	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PrfPt(UInt32 card_no, UInt32 axis, UInt32 mode = 0);
		//********************************************************************************************************************************************************************
		/**
		* @brief 查询 PT 运动模式指定 FIFO 的剩余空间
		* @param card_no：	级联的卡号	
		*		 axis：		轴选择(0-3)	
		*		 pspace：	返回Fifo号对应剩余空间，动态模式时返回总剩余空间
		*		 fifo：		Fifo号(0-1)	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PtSpace(UInt32 card_no, UInt32 axis, ref UInt32 pspace, UInt32 fifo = 0);
		//********************************************************************************************************************************************************************
		/**
		* @brief 启动 PT 运动
		* @param card_no：	级联的卡号	
		*		 axis：		轴选择(0-3)	
		*		 start：	1-启动对应的轴，0-停止对应的轴	
		*		 option：	位掩码表示的所使用的 FIFO,默认为 0,动态模式下该参数无效.
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PtStart(UInt32 card_no, UInt32 axis, UInt32 start, UInt32 option);
		//********************************************************************************************************************************************************************
		/**
		* @brief 向 PT 运动模式指定 FIFO 增加数据
		* @param card_no：	级联的卡号	
		*		 axis：		轴号
		*		 pos：		位置
		*		 time：		时间，单位毫秒
		*		 type：		运动类型，0-普通段，1-匀速段，2-减速段
		*		 fifo：		Fifo号
		*		 move_delay：运动结束后停止延时时间
		*		 out_bit：	运动结束后输出DO位，-1无输出
		*		 out_data：	运动结束后输出DO位状态，0-不导通，1-导通
		*		 out_delay：输出DO位延时时间
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PtData(UInt32 card_no, UInt32 axis, int pos, UInt32 time, UInt32 type, UInt32 fifo = 0, UInt32 move_delay = 0, int out_bit = -1, UInt32 out_data = 0, UInt32 out_delay = 0);
		//********************************************************************************************************************************************************************
		/**
		* @brief 清除 PT 运动模式指定 FIFO 中的数据
		* @param card_no：	级联的卡号	
		*		 axis：		轴选择(0-3)		
		*		 fifo：		Fifo号
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PtClear(UInt32 card_no, UInt32 axis, UInt32 fifo);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置 PT 运动模式循环执行的次数。动态模式下该指令无效
		* @param card_no：	级联的卡号	
		*		 axis：		轴选择(0-3)		
		*		 loop：		循环次数，0-无限循环
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_SetPtLoop(UInt32 card_no, UInt32 axis, UInt32 loop);
		//********************************************************************************************************************************************************************
		/**
		* @brief 查询 PT 运动模式循环执行的次数。动态模式下该指令无效
		* @param card_no：	级联的卡号	
		*		 axis：		轴选择(0-3)		
		*		 ploop：	返回循环次数
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_GetPtLoop(UInt32 card_no, UInt32 axis, ref UInt32 ploop);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置 PT 运动模式的缓存区(FIFO)大小
		* @param card_no：	级联的卡号	
		*		 memory：	最大100	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_SetPtMemory(UInt32 card_no, UInt32 memory);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取 PT 运动模式的缓存区大小
		* @param card_no：	级联的卡号	
		*		 pmemory：	返回缓冲区大小
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_GetPtMemory(UInt32 card_no, ref UInt32 pmemory);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取 PT 运动模式的缓存区运动指针
		* @param card_no：	级联的卡号	
		*		 axis：		轴选择(0-3)	
		*		 move_p：	返回缓冲区运动指针
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_GetPtMoveP(UInt32 card_no, UInt32 axis, ref UInt32 move_p);

		//********************************************************************************************************************************************************************
		/**
		* @brief 设置PT运动上位机缓存使能
		* @param card_no：	级联的卡号
		*		 enable：	0-禁能，1-使能	
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Pt_ListEn(UInt32 card_no, UInt32 en);

		//********************************************************************************************************************************************************************
		/**
		* @brief 清空PT运动上位机缓存
		* @param card_no：	级联的卡号
				 axis：		轴号
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Pt_ListClear(UInt32 card_no, UInt32 axis);

		//********************************************************************************************************************************************************************
		/**
		* @brief 读取PT运动上位机缓存指令数量
		* @param card_no：	级联的卡号
				 space：	返回PC缓冲区缓存指令（等待下载）数量
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Pt_ListSpace(UInt32 card_no, ref UInt32 space);



		/**********************************************************************************
		* @End PT运动
		***********************************************************************************/

		/**********************************************************************************
		* @Begin PWM功能
		***********************************************************************************/


		//********************************************************************************************************************************************************************
		/**
		* @brief 设置PWM功能使能
		* @param card_no：	级联的卡号	
		*		 pwm：		PWM模块号(0-1)
		*		 en：		PWM模块使能，1-使能，0-禁止。
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_PWM_Enable(UInt32 card_no, UInt32 pwm, UInt32 en);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置PWM功能开始
		* @param card_no：	级联的卡号	
		*		 pwm：		PWM模块号(0-1)
		*		 start：	1-开始，0-停止
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PWM_Start(UInt32 card_no, UInt32 pwm, UInt32 start);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置PWM功能参数
		* @param card_no：	级联的卡号	
		*		 pwm：		PWM模块号
		*		 duty：		PWM占空比 0-1
		*		 fre：		PWM频率
		*		 pol：		PWM极性
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_PWM_Config(UInt32 card_no, UInt32 pwm, float duty, float fre, UInt32 pol);
		//********************************************************************************************************************************************************************
		/**
		* @brief 设置PWM功能使能
		* @param card_no：	级联的卡号	
		*		 pwm：		PWM模块号
		*		 duty：		返回PWM占空比
		*		 fre：		返回PWM频率
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_PWM(UInt32 card_no, UInt32 pwm, ref float duty, ref float fre);

		//********************************************************************************************************************************************************************
		/**
		* @brief 不使用跟随模式，连续插补中 PWM 输出设置，如果PWM已经打开，PWM 输出频率及占空比立即改变
		* @param card_no：	级联的卡号	
		*		 pwm：		PWM模块号 0-1
		*		 fDuty：	PWM占空比 0-1
		*		 fFre：		PWM频率
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Set_PWM_Output(UInt32 card_no, UInt32 pwm, float fDuty, float fFre);
		//功 能：设置 PWM 开关状态对应的占空比,以下4个函数成套使用，跟随模式0-4
		//********************************************************************************************************************************************************************
		/**
		* @brief 使用跟随模式，设置 PWM 开关状态对应的占空比
		* @param card_no：	级联的卡号	
		*		 pwm：		PWM模块号
		*		 fOnDuty：	PWM 打开状态的占空比，取值范围：0~1
		*		 fOffDuty：	PWM 关闭状态的占空比，取值范围：0~1
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Set_PWM_Onoff_Duty(UInt32 card_no, UInt32 pwm, float fOnDuty, float fOffDuty);

		//********************************************************************************************************************************************************************
		/**
		* @brief 使用跟随模式，连续插补中 PWM 速度跟随设置
		* @param card_no：	级联的卡号	
		*		 pwm：		PWM模块号，取值范围：0~1
		*		 mode：		跟随模式：
							0：不跟随，保持状态
							1：不跟随，输出低电平
							2：不跟随，输出高电平
							3：跟随，占空比自动调整
							4：跟随，频率自动调整
		*		 maxVel：	最大运行速度，单位：pulse /ms
		*		 maxValue：	最大输出值：
							跟随模式为 3 时：占空比，取值范围：0~1
							跟随模式为 4 时：频率，取值范围：0~2MHz
		*		 outValue：	固定输出值：
							跟随模式为 3 时：频率，取值范围：0~2MHz
							跟随模式为 4 时：占空比，取值范围：0~1
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Set_PWM_Follow_Speed(UInt32 card_no, UInt32 pwm, UInt32 mode, float maxVel, float maxValue, float outValue);

		//********************************************************************************************************************************************************************
		/**
		* @brief 连续插补中相对于轨迹段起点 PWM 滞后输出（段内执行）
		* @param card_no：		级联的卡号	
		*		 pwm：			PWM模块号，取值范围：0~1
		*		 on_off：		输出状态，0：关闭，1：打开
		*		 delay_value：	滞后值，单位：ms（滞后时间模式）或 pulse（滞后距离模式）
		*		 delay_mode：	滞后模式，0：滞后时间，1：滞后距离
		*		 reverse_time：	保留参数，固定值为 0
		* @return 成功返回0.
		* @remark 设置的 IO 操作，将在该指令的下一条轨迹中起作用
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Delay_PWM_To_Start(UInt32 card_no, UInt32 pwm, UInt32 on_off, UInt32 delay_value, UInt32 delay_mode, float reverse_time);

		//********************************************************************************************************************************************************************
		/**
		* @brief 连续插补中相对于轨迹段终点 PWM 提前输出（段内执行）
		* @param card_no：		级联的卡号	
		*		 pwm：			PWM模块号，取值范围：0~1
		*		 on_off：		输出状态，0：关闭，1：打开
		*		 ahead_value：	提前值，单位：ms（提前时间模式）或 pulse（提前距离模式）
		*		 ahead_mode：	提前模式，0：提前时间，1：提前距离
		*		 reverse_time：	保留参数，固定值为 0
		* @return 成功返回0.
		* @remark 设置的 IO 操作，将在该指令的下一条轨迹中起作用
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Buff_Ahead_PWM_To_Stop(UInt32 card_no, UInt32 pwm, UInt32 on_off, UInt32 ahead_value, UInt32 ahead_mode, float reverse_time);



		/**********************************************************************************
		* @End PWM功能
		***********************************************************************************/


		/**********************************************************************************
		* @Begin 控制器辅助功能
		***********************************************************************************/
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器的产品版本号
		* @param card_no：	级联的卡号
		*		 softver：	返回软件库版本号
		*		 fpgaver：	返回硬件版本号
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_SysVersion(UInt32 card_no, ref UInt32 softver, ref UInt32 fpgaver);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器的ARM序列号
		* @param card_no：	级联的卡号
		*		 id：		返回控制器的ARM序列号(12byte) 
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_ArmID(UInt32 card_no, ref UInt32 id);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器的产品序列号
		* @param card_no：	级联的卡号
		*		 id：		返回控制器的产品序列号(4byte)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_CardID(UInt32 card_no, ref UInt32 id);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器的产品信息
		* @param card_no：	级联的卡号
		*		 card_info：返回控制器的产品信息结构体
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_CardInfo(UInt32 card_no, ref CARD_INFO card_info);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器IO数量参数
		* @param card_no：	级联的卡号
		*		 axis_num：	返回控制轴数(默认4)
		*		 di_num：	返回数字输入端口数量(默认16)
		*		 do_num：	返回数字输出端口数量(默认16)
		*		 ad_num：	返回模拟量输入端口数量(默认0)
		*		 da_num：	返回模拟量输出端口数量(默认0)
		*		 pwm_num：	返回PWM端口数量(默认2)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Card_IOResource(UInt32 card_no, ref UInt32 axis_num, ref UInt32 di_num, ref UInt32 do_num, ref UInt32 ad_num, ref UInt32 da_num, ref UInt32 pwm_num);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器位置比较锁存功能模块数量参数
		* @param card_no：	级联的卡号
		*		 cmpr1_num：	返回一维位置比较器数量(默认2)
		*		 cmprline_num：	返回线性位置比较器数量(默认2)
		*		 cmpr2_num：	返回二维位置比较器数量(默认0)
		*		 latch_num：	返回位置锁存器数量(默认2)
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Card_EXResource(UInt32 card_no, ref UInt32 cmpr1_num, ref UInt32 cmprline_num, ref UInt32 cmpr2_num, ref UInt32 latch_num);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读取控制器模式参数
		* @param card_no：	级联的卡号
		*		 model：	返回控制器模式
		*		 type：		返回控制器类型
		*		 period：	返回控制器伺服周期
		* @return 成功返回0.
		* @remark
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Card_Model(UInt32 card_no, ref UInt32 model, ref UInt32 type, ref UInt32 period);


		//********************************************************************************************************************************************************************
		/**
		* @brief 用户产品注册码写入
		* @param card_no：	级联的卡号
		*		 reg_code：	客户注册码
		*		 reg_code_md5：返回写入控制器的MD5码 （16byte）
		* @return 成功返回0.
		* @remark (EEPROM大小：20*32bit)
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Write_Regcode(UInt32 card_no,  byte[] reg_code, ref byte reg_code_md5);
		//********************************************************************************************************************************************************************
		/**
		* @brief 读出写入控制器的MD5码
		* @param card_no：	级联的卡号
		*		 reg_code：	读出写入控制器的MD5码（16byte）
		* @return 成功返回0.
		* @remark (EEPROM大小：20*32bit)
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Read_Regcode(UInt32 card_no, ref byte reg_code);
		//********************************************************************************************************************************************************************
		/**
		* @brief EEPROM数据写入 
		* @param card_no：	级联的卡号
		*		 addr：		写入地址,范围：0-20
		*		 num：		写入个数0-20
		*		 pData：	写入数据,范围：32bit
		* @return 成功返回0.
		* @remark (EEPROM大小：20*32bit)
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_E2prom_Write(UInt32 card_no, UInt32 addr, UInt32 num,  uint[] pData);
		//********************************************************************************************************************************************************************
		/**
		* @brief EEPROM数据读出 
		* @param card_no：	级联的卡号
		*		 addr：		读出地址,范围：0-20
		*		 num：		读出个数0-20
		*		 pData：	读出数据,范围：32bit
		* @return 成功返回0.
		* @remark (EEPROM大小：20*32bit)
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_E2prom_Read(UInt32 card_no, UInt32 addr, UInt32 num, ref int pData);
		//********************************************************************************************************************************************************************
		/**
		* @brief 扩展EEPROM数据写入 
		* @param card_no：	级联的卡号
		*		 addr：		写入地址,范围：0-20
		*		 num：		写入个数0-20
		*		 pData：	写入数据,范围：32bit
		* @return 成功返回0.
		* @remark (EEPROM大小：20*32bit)
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_ExE2prom_Write(UInt32 card_no, UInt32 addr, UInt32 num,  int[] pData);
		//********************************************************************************************************************************************************************
		/**
		* @brief 扩展EEPROM数据读出 
		* @param card_no：	级联的卡号
		*		 addr：		读出地址,范围：0-20
		*		 num：		读出个数0-20
		*		 pData：	读出数据,范围：32bit
		* @return 成功返回0.
		* @remark (EEPROM大小：20*32bit)
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_ExE2prom_Read(UInt32 card_no, UInt32 addr, UInt32 num, ref int pData);

		//********************************************************************************************************************************************************************
		/**
		* @brief 装载系统参数文件，下载到控制器 
		* @param card_no：	级联的卡号
		* @return 成功返回0.
		* @remark 参数文件：E450config.ini
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Load_ParaFile(UInt32 card_no);
		//public static extern int SLDM_LoadParaFile(UInt32 card_no,Home_Para *homepara);
		//********************************************************************************************************************************************************************
		/**
		* @brief 保存系统参数文件
		* @param card_no：	级联的卡号
		* @return 成功返回0.
		* @remark 参数文件：E450config.ini
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Save_ParaFile(UInt32 card_no);


		/**********************************************************************************
		* @End 控制器辅助功能
		***********************************************************************************/

        /**********************************************************************************
         * @Begin ETHERCAT功能
         ***********************************************************************************/


        //********************************************************************************************************************************************************************
        /**
        * @brief 设置ethercat控制器控制轴数及型号
        * @param card_no：	级联的卡号
        *		 axisnum：	控制器连接轴数
        *		 type：		控制器连接轴型号列表数组
        * @return 成功返回0.
        * @remark
        */
        [DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Set_Ecat_Axis(UInt32 card_no, UInt32 axis_num, UInt32[] type);

        //********************************************************************************************************************************************************************
        /**
        * @brief ethercat控制器复位
        * @param card_no：	级联的卡号
        * @return 成功返回0.
        * @remark
        */
        [DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Ecat_Reset(UInt32 card_no);


        //********************************************************************************************************************************************************************
        /**
        * @brief 读取ethercat控制器编码器状态
        * @param card_no：	级联的卡号
        *		 cur_axisnum：		当前在线轴个数
        *		 online_flag：		位掩码表示对轴在线标志 0-离线 1-在线
        *		 link_ok：			轴初始化配置完成标志 0-未完成 1-完成
        * @return 成功返回0.
        * @remark
        */
        [DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Get_Ecat_Status(UInt32 card_no, ref UInt32 cur_axisnum, ref UInt32 online_flag, ref UInt32 link_ok);

        //********************************************************************************************************************************************************************
        /**
        * @brief 读取ethercat控制器编码器Z信号锁存位置
        * @param card_no：	级联的卡号
        *		 axis：		轴号
        *		 pos：		返回的对应编码器Z信号锁存位置计数值
        * @return 成功返回0.
        * @remark
        */
        [DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Get_Ecat_ZCap_pos(UInt32 card_no, UInt32 axis, ref int pos);

        //********************************************************************************************************************************************************************
        /**
        * @brief 读取ethercat控制器授权控制轴数
        * @param card_no：	级联的卡号
        *		 axisnum：	返回控制器授权控制轴数
        * @return 成功返回0.
        * @remark
        */
        [DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Get_Ecat_Release_AxisNum(UInt32 card_no, ref UInt32 axisnum);

        //********************************************************************************************************************************************************************
        /**
        * @brief 读取ethercat控制器扩展编码器位置
        * @param card_no：	级联的卡号
        *		 exenc_num：扩展编码器号，在ethercat控制器最大控制轴号基础上递增
        *		 pos：		返回的对应编码器计数值
        * @return 成功返回0.
        * @remark
        */
        [DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Get_EcatEx_Enc(UInt32 card_no, UInt32 exenc_num, ref int pos);


        /**********************************************************************************
        * @End ETHERCAT功能
        ***********************************************************************************/

        /**********************************************************************************
        * @Begin 手轮功能
        ***********************************************************************************/
        //********************************************************************************************************************************************************************
        /**
        * @brief 设置手轮运动参数
        * @param card_no：	级联的卡号
        *		 axis_num：	手轮控制轴数
        *		 axis_list：手轮轴选择挡位对应控制轴列表数组
        *		 mult_list：手轮倍率选择挡位对应控制倍率列表数组
        *		 dir_pol：	方向极性·，0-正向，1-反向	
        * @return 成功返回0.
        * @remark
        */
        [DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Set_HandWheel_Config(UInt32 card_no, UInt32 axis_num, UInt32[] axis_list, UInt32[] mult_list, UInt32 dir_pol);
        //********************************************************************************************************************************************************************
        /**
        * @brief 设置手轮运动最大速度和加速度
        * @param card_no：	级联的卡号
        *		 max_vel：	最大速度，单位：p/ms
        *		 max_acc：	最大加速度，单位：p/(ms*ms)	
        * @return 成功返回0.
        * @remark
        */
        [DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Set_HandWheel_MaxVel(UInt32 card_no, UInt32 max_vel, UInt32 max_acc);
        //********************************************************************************************************************************************************************
        /**
        * @brief 设置手轮运动使能
        * @param card_no：	级联的卡号
        *		 enable：	使能，0-不使能，1-使能	
        * @return 成功返回0.
        * @remark
        */
        [DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Set_HandWheel_Enable(UInt32 card_no, UInt32 enable);
        //********************************************************************************************************************************************************************
        /**
        * @brief 读取手轮输入端口状态
        * @param card_no：	级联的卡号
        *		 di_status：XYZ45及X10X100端口电平状态
        *					bit:
        *					0：X
        *					1：Y
        *					2：Z
        *					3：4
        *					4：5
        *					5：X10
        *					6：X100
        *					7：0
        *		 enc_data：	手轮编码器计数值

        * @return 成功返回0.
        * @remark 参数文件：E450config.ini
        */
        [DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SLDM_Get_HandWheel_Status(UInt32 card_no, ref UInt32 di_status, ref int enc_data);
        /**********************************************************************************
        * @End 手轮功能
        ***********************************************************************************/



		/**********************************************************************************
		* @Begin 开发调试函数
		***********************************************************************************/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Recbuf(UInt32 card_no, ref string recbuf, ref string recbuf_ID, ref string recbuf_EEP, ref string recbuf_SELF, ref string recbuf_ERR);
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Sendbuf(UInt32 card_no, ref string sendbuf);
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Set_commthread_debug(UInt32 card_no, short debug_mode, short debug_key);
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Test_function(UInt32 card_no, UInt32 para_num);//测试UDP函数
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_CommTime(ref float cur_time, ref float max_time);
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_CommTime_10000(ref float time);
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_udp_complete_num(ref int write_num, ref int read_num);
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Get_Home_flag(UInt32 card_no, UInt32 axis, ref int flag);
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Cmpr_2D(UInt32 card_no, ref UInt32 axislist, ref int cmpr_pos, UInt32 max_err, UInt32 threshould, UInt32 fifo, UInt32 width, UInt32 out_level, UInt32 src);

		/**********************************************************************************
		* @End 开发调试函数
		***********************************************************************************/

		//********************************************************************************************************************************************************************
		/**
		* @brief 单位距离转脉冲数
		* @param axis：		轴号.
		*		 PPR：		每转脉冲数
		*		 pitch：	螺距，单位ms
		*		 unit_value：	待转换单位距离，单位ms
		*		 pulse_value：	转换脉冲数结果，单位：脉冲
		* @return 成功返回0.
		* @remark 参数文件：E450config.ini
		*/
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int Unit_TO_Pulse(int axis, float PPR, float pitch, float unit_value, ref int pulse_value);

		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTPn_Ex_Register(UInt32 id, UInt32 n_axis,  UInt32[] cardlist,  UInt32[] axislist);
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTPn_Ex(UInt32 id,  int[] pos, float start_vel, float steady_vel, float end_vel, float acc, float dec, UInt32 sine, int dist_mode);
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTPn_Ex_GetRunning(UInt32 id, ref UInt32 running);
		[DllImport("sldmv.dll", CharSet =  CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_PTPn_Ex_Stop(UInt32 id, float stop_dec);

		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Write_ToUart(UInt32 card_no, UInt32 uart_no, UInt32 num, byte[] data);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Uart_Trans_Enable(UInt32 card_no, UInt32 uart_no, UInt32 en);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Read_FromUart(UInt32 card_no, UInt32 uart_no, ref UInt32 num, byte[] data);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Add_Jump_Up(UInt32 card_no, Jump_Data jump_data);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Add_Jump_Steady(UInt32 card_no, Jump_Data jump_data);
		[DllImport("sldmv.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SLDM_Add_Jump_Down(UInt32 card_no, Jump_Data jump_data);
#endif
	}

}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
