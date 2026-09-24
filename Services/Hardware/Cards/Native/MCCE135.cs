﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/DLL/MCCE135.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
/////////////////////////////////////////////////////////////////////////////////////////////////
/***********************************************************************************************
 *               MCCE135系列总线运动控制卡函数指令库C#接口                                     *
 *               TIME：202603051043                                                            *
 *               Author：shenweixin                                                            *
 *               Version:3.0.0.4                                                               *
 *               适用产品：MCCE135总线E1、E3、E5系列6\8\16\32\64轴卡                           *
************************************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;               //使用C#导入dll必须的

namespace WenQiZhi.Domain.MotionCard.Common
{
    public class MCCE135
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TCrdPrm                        //设置坐标系的相关参数
        {
            public ushort dimension;                 //坐标系的维数。取值范围：[1, 32]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32, ArraySubType = UnmanagedType.U2)]
            public ushort[] profile;                 //坐标系【x,y,z,u,v,w,c,c1】与物理轴0~7的映射关系。
            public double synVelMax;                 //该坐标系的最大合成速度。如果用户在输入插补段的时候所设置的目标速度大于了该速度，则将会被限制为该速度。单位：脉冲当量/s。
            public double synAccMax;                 //该坐标系的最大合成加速度。如果用户在输入插补段的时候所设置的加速度大于了该加速度，则将会被限制为该加速度。单位：脉冲当量/s2。
            public short evenTime;                   //保留值 
            public short setOriginFlag;              //保留值 
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32, ArraySubType = UnmanagedType.I4)]
            public Int32[] originPos;                 //保留值 
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TCrdSysEniPrm                  //总线卡系统参数
        {
            public ushort numberType;                //0:驱动器   1:IO模块  2：耦合器 
            public uint man;                         /** Manufacturer code of slave */
            public uint id;                          /** ID of slave */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 42, ArraySubType = UnmanagedType.I1)]
            public char[] name;                      /** Readable name */
            public ushort Ibits;                     /** Input bits */
            public ushort Obits;                     /** Output bits */
            public byte Dtype;                       /** Data type */
            public ushort SM2a;                      /** SyncManager 2 address */
            public uint SM2f;                        /** SyncManager 2 flags */
            public ushort SM3a;                      /** SyncManager 3 address */
            public uint SM3f;                        /** SyncManager 3 flags */
            public byte FM0ac;                       /** FMMU 0 activation */
            public byte FM1ac;                       /** FMMU 1 activation */
            public byte RxPdo_Len;                   //RxPdo长度 最长16
            public byte TxPdo_Len;                   //TxPdo长度 最长16
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = UnmanagedType.U4)]
            public uint[] RxPdo_Indexex;             //RxPdo索引
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = UnmanagedType.U4)]
            public uint[] TxPdo_Indexex;            //TxPdo索引
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = UnmanagedType.U1)]
            public byte[] RxPdo_Index;              //RxPdo子索引
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = UnmanagedType.U1)]
            public byte[] TxPdo_Index;              //RxPdo子索引
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = UnmanagedType.U1)]
            public byte[] RxPdo_InLen;              //RxPdo索引值长度  8 16 32
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] TxPdo_InLen;              //RxPdo索引值长度  8 16 32
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TCrdSysIoLinkingPrm
        {
            public uint slave;                          //IO从站号
            public uint eep_man;                        //manid
            public uint eep_id;                         //设备id
            public uint slotIndex;                      //累加量
            public uint slave_num;                      //IO从站个数
            public uint iolink_num;                     //IO从站子模块个数
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 32, ArraySubType = UnmanagedType.U4)]
            public uint[] Iolink_Index;                  //io链表  0：输入  1：输出  2：输入输出
        }
        //--------------------------------------------------------------------------------
        //设置多段速-高速变低速运动参数
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TMultiStageVelHL_Set
        {
            public double high_dist;                         //高速段，运行距离(单位：脉冲)
            public double low_dist;                          //低速段，运行距离(单位：脉冲)
            public double start_vel;                         //起始速度:单位:[ppu/s];
            public double high_vel;                          //高速段最大速度:单位:[ppu/s];
            public double high_tacc;                         //高速段加速时间,单位秒[s],最小1ms;
            public double high_tdec;                         //高速段减速时间,单位秒[s],最小1ms;
            public double low_vel;                           //低速段最大速度:单位:[ppu/s];
            public double low_tdec;                          //低速段加速时间,单位秒[s],最小1ms;
            public double end_vel;                           //停止速度:单位:[ppu/s];
            public double smooth_time;                       //平滑段时间,单位秒[s],最小1ms;
            public ushort pos_mode;                         //位置模式 0：相对坐标  1：绝对坐标
        }

        //读取多段速-高速变低速运动参数
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TMultiStageVelHL_Get
        {
            public double high_dist;                          //高速段，运行距离(单位：脉冲)
            public double low_dist;                           //低速段，运行距离(单位：脉冲)
            public double start_vel;                          //起始速度:单位:[ppu/s];
            public double high_vel;                           //高速段最大速度:单位:[ppu/s];
            public double high_tacc;                          //高速段加速时间,单位秒[s],最小1ms;
            public double high_tdec;                          //高速段减速时间,单位秒,[s],最小1ms;
            public double low_vel;                            //低速段最大速度:单位:[ppu/s];
            public double low_tdec;                           //低速段加速时间,单位秒,[s],最小1ms;
            public double end_vel;                            //停止速度:单位:[ppu/s];
            public double smooth_time;                        //平滑段时间,单位秒[s],最小1ms;
            public ushort pos_mode;                           //位置模式 0：相对坐标  1：绝对坐标;
        }
        //------------------------------------------------------------------------------------
        //设置多段速-低速变高速运动参数
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TMultiStageVelLH_Set
        {
            public double low_dist;                           //低速段，运行距离(单位：脉冲)
            public double high_dist;                          //高速段，运行距离(单位：脉冲)
            public double start_vel;                          //起始速度:单位:[ppu/s];
            public double low_vel;                            //低速段最大速度:单位:[ppu/s];
            public double low_tacc;                           //低速段加速时间,单位秒,[s],最小1ms;
            public double high_vel;                           //高速段最大速度:单位:[ppu/s];
            public double high_tacc;                          //高速段加速时间,单位秒,[s],最小1ms;
            public double high_tdec;                          //高速段减速时间,单位秒,[s],最小1ms;
            public double end_vel;                            //停止速度:单位:[ppu/s];
            public double smooth_time;                        //S段时间,单位秒,[s],最小1ms;
            public ushort pos_mode;                           //位置模式 0：相对坐标  1：绝对坐标
        }

        //读取多段速-低速变高速运动参数
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TMultiStageVelLH_Get
        {
            public double low_dist;                           //低速段，运行距离(单位：脉冲)
            public double high_dist;                          //高速段，运行距离(单位：脉冲)
            public double start_vel;                          //起始速度:单位:[ppu/s];
            public double low_vel;                            //低速段最大速度:单位:[ppu/s];
            public double low_tacc;                           //低速段加速时间,单位秒,[s],最小1ms;
            public double high_vel;                           //高速段最大速度:单位:[ppu/s];
            public double high_tacc;                          //高速段加速时间,单位秒,[s],最小1ms;
            public double high_tdec;                          //高速段减速时间,单位秒,[s],最小1ms;
            public double end_vel;                            //停止速度:单位:[ppu/s];
            public double smooth_time;                        //平滑段时间,单位秒,[s],最小1ms;
            public ushort pos_mode;                           //位置模式 0：相对坐标  1：绝对坐标
        }
        //--------------------------------------------------------------------------------------------
        //三段速低高低
        //设置多段速-低高低三段速运动参数
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TMultiStageVelLHL_Set
        {
            public double low_dist1;                          //第一段低速段，运行距离(单位：脉冲)
            public double high_dist;                          //高速段，运行距离(单位：脉冲)
            public double low_dist2;                          //第二段低速段，运行距离
            public double start_vel;                          //起始速度:单位:[ppu/s];
            public double low_vel1;                           //第一段低速段最大速度:单位:[ppu/s];
            public double low_tacc1;                          //第一段低速段加速时间,单位秒,[s],最小1ms;
            public double high_vel;                           //高速段最大速度单位:[ppu/s];
            public double high_tacc;                          //高速段加速时间,单位秒,[s],最小1ms;
            public double high_tdec;                          //高速段减速时间,单位秒,[s],最小1ms;
            public double low_vel2;                           //第二段低速段低速最大速度:单位:[ppu/s];
            public double low_tdec2;                          //第二段低速段减速时间,单位秒,[s],最小1ms;
            public double end_vel;                            //停止速度:单位:[ppu/s];
            public double smooth_time;                        //S段时间,单位秒,[s],最小1ms;
            public ushort pos_mode;                           //位置模式 0：相对坐标  1：绝对坐标
        }

        //读取多段速 - 低高低三段速运动参数
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TMultiStageVelLHL_Get
        {
            public double low_dist1;                          //第一段低速段，运行距离(单位：脉冲)
            public double high_dist;                          //高速段，运行距离(单位：脉冲)
            public double low_dist2;                          //第二段低速段，运行距离(单位：脉冲)
            public double start_vel;                          //起始速度:单位:[ppu/s];
            public double low_vel1;                           //第一段低速段最大速度:单位:[ppu/s];
            public double low_tacc1;                          //第一段低速段加速时间,单位秒,[s],最小1ms;
            public double high_vel;                           //高速段最大速度:单位:[ppu/s];
            public double high_tacc;                          //高速段加速时间,单位秒,[s],最小1ms;
            public double high_tdec;                          //高速段减速时间,单位秒,[s],最小1ms;
            public double low_vel2;                           //第二段低速段低速最大速度:单位:[ppu/s];
            public double low_tdec2;                          //第二段低速段减速时间,单位秒,[s],最小1ms;
            public double end_vel;                            //停止速度:单位:[ppu/s];
            public double smooth_time;                        //S段时间,单位秒,[s],最小1ms;
            public ushort pos_mode;                           //位置模式 0：相对坐标  1：绝对坐标
        }
        //----------------------------------------------------------------------------------------------------
        //三段速高低高
        //设置多段速 - 高低高三段速运动参数
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TMultiStageVelHLH_Set
        {
            public double high_dist1;                         //第一段高速段，运行距离(单位：脉冲)
            public double low_dist;                           //低速段，运行距离(单位：脉冲)
            public double high_dist2;                         //第二段高速段，运行距离(单位：脉冲)
            public double start_vel;                          //起始速度:单位:[ppu/s];
            public double high_vel1;                          //第一段高速段最大速度:单位:[ppu/s];
            public double high_tacc1;                         //第一段高速段加速时间,单位秒,[s],最小1ms;
            public double high_tdec1;                         //第一段高速段减速时间,单位秒,[s],最小1ms;
            public double low_vel;                            //低速段最大速度:单位:[ppu/s];
            public double high_vel2;                          //第二段高速段最大速度:单位:[ppu/s];
            public double high_tacc2;                         //第二段高速段加速时间,单位秒,[s],最小1ms;
            public double high_tdec2;                         //第二段高速段减速时间,单位秒,[s],最小1ms;
            public double end_vel;                            //停止速度:单位:[ppu/s];
            public double smooth_time;                        //S段时间,单位秒,[s],最小1ms;
            public ushort pos_mode;                           //位置模式 0：相对坐标  1：绝对坐标
        }

        //读取多段速-高低高三段速运动参数
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TMultiStageVelHLH_Get
        {
            public double high_dist1;                        //第一段高速段，运行距离(单位：脉冲)
            public double low_dist;                          //低速段，运行距离(单位：脉冲)
            public double high_dist2;                        //第二段高速段，运行距离(单位：脉冲)
            public double start_vel;                         //起始速度:单位:[ppu/s];
            public double high_vel1;                         //第一段高速段最大速度:单位:[ppu/s];
            public double high_tacc1;                        //第一段高速段加速时间,单位秒,[s],最小1ms;
            public double high_tdec1;                        //第一段高速段减速时间,单位秒,[s],最小1ms;
            public double low_vel;                           //低速段最大速度:单位:[ppu/s];
            public double high_vel2;                         //第二段高速段最大速度:单位:[ppu/s];
            public double high_tacc2;                        //第二段高速段加速时间,单位秒,[s],最小1ms;
            public double high_tdec2;                        //第二段高速段减速时间,单位秒,[s],最小1ms;
            public double end_vel;                           //停止速度:单位:[ppu/s];
            public double smooth_time;                       //S段时间,单位秒,[s],最小1ms;
            public ushort pos_mode;                          //位置模式 0：相对坐标  1：绝对坐标
        }

        //电子齿轮参数设置
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TGearPara_Set
        {
            public ushort MasterAxis;           //主轴轴号   
            public uint MasterScale;            //主轴齿数  
            public uint SlaveScale;             //从轴齿数
            public short FollowDirMode;         //跟随方向模式  0：双向跟随 1：正向跟随 -1：负向跟随
            public ushort FollowSource;         //跟随位置类型 0:规划位置1:反馈位置
            public double MasterSlopeDis;       //主轴离合区位移量   
        }

        //电子齿轮参数读取
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TGearPara_Get
        {
            public ushort MasterAxis;          //主轴轴号
            public uint MasterScale;           //主轴齿数  
            public uint SlaveScale;            //从轴齿数
            public short FollowDirMode;        //跟随方向模式  0：双向跟随 1：正向跟随 -1：负向跟随 
            public ushort FollowSource;        //跟随位置类型0:规划位置1:反馈位置
            public double MasterSlopeDis;      //主轴离合区位移量   
        }

        //所有轴的参数
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TaxisParaPackGet
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.U2)]
            public ushort[] alm_status;         //报警状态
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.U2)]
            public ushort[] pos_hwlimit_status; //硬件正限位状态
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.U2)]
            public ushort[] neg_hwlimit_status; //硬件负限位状态
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.U2)]
            public ushort[] pos_swlimit_status; //软件正限位状态
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.U2)]
            public ushort[] neg_swlimit_status; //软件负限位状态
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.U2)]
            public ushort[] emg_status;         //emg状态
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.U2)]
            public ushort[] home_status;        //home状态
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.U2)]
            public ushort[] inp_status;         //inp状态
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.U2)]
            public ushort[] svon_status;        //svon状态
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.U2)]
            public ushort[] busy_status;        //busy状态
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.U2)]
            public ushort[] warning_status;     //warning状态
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.R8)]
            public double[] encoder_pos;        //轴编码器位置
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.R8)]
            public double[] plan_pos;           //轴规划位置
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.R8)]
            public double[] vel;                //轴速度
        }

        //板卡配置相关函数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_init", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_init();//初始化控制卡
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_close();//关闭控制卡
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_reset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_reset();//硬件复位
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_cold_reset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_cold_reset();//冷复位
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_get_CardInfList", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_get_CardInfList(ref ushort CardNum, uint[] CardTypeList, ushort[] CardIdList);//读取初始化完成后的获取所有卡信息列表
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_get_card_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_get_card_version(ushort CardNo, ref uint CardVersion);//读取控制卡硬件版本
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_get_card_soft_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_get_card_soft_version(ushort CardNo, ref uint FirmID, ref uint SubFirmID);//读取控制卡硬件的固件版本
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_get_card_lib_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_get_card_lib_version(ref uint LibVer);//读取控制卡动态库版本    
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_get_axis_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_get_axis_num(ushort CardNo, ref uint TotalAxis);//读取指定卡轴数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_get_di_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_get_di_num(ushort CardNo, ref ushort TotalDi);//获取当前控制卡的Di数；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_get_do_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_get_do_num(ushort CardNo, ref ushort TotalDo);//获取当前控制卡的Do数  
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_get_hard_ware_info", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_get_hard_ware_info(ushort CardNo, byte[] hardWareInfo);//读取卡的硬件信息;
        //轴输出绑定
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_set_bondcfg", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_set_bondcfg(ushort CardNo, ushort axis, ushort axistype, ushort outputChn, ushort loaclEncSrc);//轴输出绑定设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_get_bondcfg", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_get_bondcfg(ushort CardNo, ushort axis, ref ushort axistype, ref ushort outputChn, ref ushort loaclEncSrc);//获取轴绑定状态
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_reset_axbondcfg", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_reset_axbondcfg(ushort CardNo);//复位轴绑定状态，让所有轴恢复到虚轴状态
        //ENI,INI参数文件下载与固件升级
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_download_eni_file", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_download_eni_file(ushort CardNo, string xml_file_name); //下载设备参数文件        
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_download_ini_file", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_download_ini_file(ushort CardNo, string xml_file_name);//下载系统参数文件 
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_download_firmware", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_download_firmware(ushort CardNo, string FileName);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_fpga_update_firmware", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_fpga_update_firmware(ushort CardNo, string FileName);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_board_download_iolinking_file", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_board_download_iolinking_file(ushort CardNo, ref TCrdSysIoLinkingPrm pCrdSysIoLinkPrm);//下载耦合器链路信息文件
        //脉冲当量设置     
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_set_equiv", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_set_equiv(ushort CardNo, ushort axis, double equiv);//设置指定轴的脉冲当量
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_get_equiv", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_get_equiv(ushort CardNo, ushort axis, ref double equiv);//读取指定轴的脉冲当量     
                                                                                                    //电子齿轮比设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_calculate_egear_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_calculate_egear_ratio(int increments, int motor_turns0, int motor_turns1, int gear_output_turns0, int gear_output_turns1, double units_in_app, ref double egear_ratio);//设置指定轴的电子齿轮比率        
        //轴使能设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_set_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_set_enable(ushort CardNo, ushort enable);//设置所有轴使能；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_get_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_get_enable(ushort CardNo, ref ushort enable);//读取所有轴使能设置    
        //软限位设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_set_softlimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_set_softlimit(ushort CardNo, ushort axis, ushort enable, ushort source_sel, ushort SL_action, double N_limit, double P_limit);//设置软限位参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_get_softlimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_get_softlimit(ushort CardNo, ushort axis, ref ushort enable, ref ushort source_sel, ref ushort SL_action, ref double N_limit, ref double P_limit);//读取软限位参数
        //主站回零运动	
        [DllImport("MCCE135.dll", EntryPoint = "mcc_home_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_home_set_para(ushort CardNo, ushort axis, ushort home_dir, ushort home_vel, ushort home_mode);    //设置本地回零参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_home_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_home_get_para(ushort CardNo, ushort axis, ref ushort home_dir, ref ushort home_vel, ref ushort home_mode); //读取本地回零参数；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_home_set_offset_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_home_set_offset_para(ushort CardNo, ushort axis, ushort offsetmode, double offset_pos);//主站回零偏移量参数设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_home_get_offset_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_home_get_offset_para(ushort CardNo, ushort axis, ref ushort offsetmode, ref double offset_pos);//主站回零偏移量参数读取
        [DllImport("MCCE135.dll", EntryPoint = "mcc_home_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_home_start(ushort CardNo, ushort axis);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_home_get_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_home_get_status(ushort CardNo, ushort axis, ref ushort home_state);//读取主站回零状态	    
        //单轴运动速度曲线设置函数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_set_prf_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_set_prf_vel(ushort CardNo, ushort axis, double MinVel, double MaxVel, double Tacc, double Tdec, double StopVel);//设置单轴运动速度曲线    
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_get_prf_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_get_prf_vel(ushort CardNo, ushort axis, ref double MinVel, ref double MaxVel, ref double Tacc, ref double Tdec, ref double StopVel);//读取单轴运动速度曲线
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_set_s_prf_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_set_s_prf_vel(ushort CardNo, ushort axis, ushort s_mode, double s_para);//设置平滑速度曲线参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_get_s_prf_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_get_s_prf_vel(ushort CardNo, ushort axis, ushort s_mode, ref double s_para);//读取平滑速度曲线参数
                                                                                                                        //单轴点位运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_pmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_pmove(ushort CardNo, ushort axis, double dist, ushort posi_mode);//指定轴做定长位移运动
                                                                                                             //单轴JOG(连续)运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_vmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_vmove(ushort CardNo, ushort axis, ushort dir);//指定轴做连续运动
        //单轴在线变位/变速
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_reset_target_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_reset_target_position(ushort CardNo, ushort axis, double dist, ushort posi_mode);//运动中改变目标位置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_change_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_change_speed(ushort CardNo, ushort axis, double curr_vel, double acc_dec_t);//在线改变指定轴的当前运动速度及加减速时间
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_update_target_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_update_target_position(ushort CardNo, ushort axis, double dist, ushort posi_mode);// 强行改变在线/非在线目标位置 
        //软着陆与软启动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pmove_soft_landing", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pmove_soft_landing(ushort CardNo, ushort axis, double midPos, double targetPos, double startVel, double maxVel, double endVel, double tAcc, double tDec, ushort posiMode);
        //轴停止、软急停、硬急停参数设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_stop(ushort CardNo, ushort axis, ushort stop_mode);	//(轴停止)单轴减速停止/立即停止  
        [DllImport("MCCE135.dll", EntryPoint = "mcc_soft_emg_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_soft_emg_stop(ushort CardNo);//软急停(紧急停止所有轴)  
        //硬急停参数设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_emg_set_stop_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_emg_set_stop_para(ushort CardNo, ushort io_port, ushort enable, ushort emg_logic, ushort stop_mode, double filter_time);      //设置硬急停参数  
        [DllImport("MCCE135.dll", EntryPoint = "mcc_emg_get_stop_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_emg_get_stop_para(ushort CardNo, ref ushort io_port, ref ushort enable, ref ushort emg_logic, ref ushort stop_mode, ref double filter_time);  //读硬急停参数设置   
        //----------------------add 20250422--------------------------------------------------------------------------------------
        [DllImport("MCCE135.dll", EntryPoint = "mcc_emg_set_stop_para_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_emg_set_stop_para_ex(ushort CardNo, ushort io_num, ushort enable, ushort emg_logic, ushort stop_mode, double filter_time, ushort[] io_port_list);// 硬件紧急停止参数设置  有效电平+停止模式+滤波时间
        [DllImport("MCCE135.dll", EntryPoint = "mcc_emg_get_stop_para_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_emg_get_stop_para_ex(ushort CardNo, ref ushort io_num, ref ushort enable, ref ushort emg_logic, ref ushort stop_mode, ref double filter_time, ushort[] io_port_list); //硬件紧急停止参数读取  有效电平+停止模式+滤波时间    
        //通用IO读写		    
        [DllImport("MCCE135.dll", EntryPoint = "mcc_io_read_inbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_io_read_inbit(ushort CardNo, ushort bitno, ref ushort on_off);//读取输入口的状态      
        [DllImport("MCCE135.dll", EntryPoint = "mcc_io_write_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_io_write_outbit(ushort CardNo, ushort bitno, ushort on_off);//设置输出口的状态  
        [DllImport("MCCE135.dll", EntryPoint = "mcc_io_read_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_io_read_outbit(ushort CardNo, ushort bitno, ref ushort on_off);//读取输出口的状态    
        [DllImport("MCCE135.dll", EntryPoint = "mcc_io_read_inbyte", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_io_read_inbyte(ushort CardNo, ushort StartByte, ushort ByteNum, byte[] ValueList);//读取输入端口的值   
        [DllImport("MCCE135.dll", EntryPoint = "mcc_io_read_outbyte", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_io_read_outbyte(ushort CardNo, ushort StartByte, ushort ByteNum, byte[] ValueList);//读取输出端口的值  
        [DllImport("MCCE135.dll", EntryPoint = "mcc_io_write_outbyte", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_io_write_outbyte(ushort CardNo, ushort StartByte, ushort ByteNum, byte[] ValueList);//设置输出端口的值  
        //通用IO拓展操作(IO计数)
        [DllImport("MCCE135.dll", EntryPoint = "mcc_io_delay_turn_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_io_delay_turn_outbit(ushort CardNo, ushort bitno, double reverse_time);//IO输出延时翻转  
        [DllImport("MCCE135.dll", EntryPoint = "mcc_io_set_count_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_io_set_count_mode(ushort CardNo, ushort bitno, ushort mode, double filter_time); //设置IO计数模式 
        [DllImport("MCCE135.dll", EntryPoint = "mcc_io_get_count_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_io_get_count_mode(ushort CardNo, ushort bitno, ref ushort mode, ref double filter_time);//读取 IO 计数模式设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_io_set_count_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_io_set_count_value(ushort CardNo, ushort bitno, uint CountValue);//设置IO计数值  
        [DllImport("MCCE135.dll", EntryPoint = "mcc_io_get_count_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_io_get_count_value(ushort CardNo, ushort bitno, ref uint CountValue);//读取IO计数值   
        //本地IO映射
        [DllImport("MCCE135.dll", EntryPoint = "mcc_io_set_axis_map", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_io_set_axis_map(ushort CardNo, ushort axis, ushort io_map_type, ushort io_logic, short io_map_num, double filter_time);//IO映射设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_io_get_axis_map", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_io_get_axis_map(ushort CardNo, ushort axis, ushort io_map_type, ref ushort io_logic, ref short io_map_num, ref double filter_time);//读取IO映射设置     
        //通用输出DO映射为EMG硬件口—特殊IO映射	
        [DllImport("MCCE135.dll", EntryPoint = "mcc_io_function_convert", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_io_function_convert(ushort CardNo, ushort bitno, ushort io_logic, double filter_time);//通用IO当做EMG硬件口,后面根据需求增加选项
        //手轮功能
        //一个手轮信号控制单个轴运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_handwheel_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_handwheel_set_para(ushort CardNo, ushort ChnNo, ushort enable, ushort CfgAxis, ushort inmode, double ratio);//设置输入手轮脉冲信号的工作方式 控制单轴
        [DllImport("MCCE135.dll", EntryPoint = "mcc_handwheel_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_handwheel_get_para(ushort CardNo, ushort ChnNo, ref ushort enable, ref ushort CfgAxis, ref ushort inmode, ref double ratio);//读取输入手轮脉冲信号的工作方式 控制单轴
        //一个手轮信号控制多个轴运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_handwheel_multi_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_handwheel_multi_set_para(ushort CardNo, ushort ChnNo, ushort enable, ushort AxisNum, ushort[] CfgAxisList, ushort inmode, double ratio);//设置输入手轮脉冲信号的工作方式 控制多轴
        [DllImport("MCCE135.dll", EntryPoint = "mcc_handwheel_multi_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_handwheel_multi_get_para(ushort CardNo, ushort ChnNo, ref ushort enable, ref ushort AxisNum, ushort[] CfgAxisList, ref ushort inmode, ref double ratio);
        //启动指定轴的手轮脉冲运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_handwheel_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_handwheel_move(ushort CardNo, ushort ChnNo);
        //位置计数器相关函数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_set_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_set_position(ushort CardNo, ushort axis, double current_position);//设定指定轴的当前位置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_get_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double mcc_axis_get_position(ushort CardNo, ushort axis);//读取指定轴的当前位置  
        //编码器相关函数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_encoder_get_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double mcc_encoder_get_value(ushort CardNo, ushort axis);  //读取编码器计数值
        [DllImport("MCCE135.dll", EntryPoint = "mcc_encoder_get_value_actual", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_encoder_get_value_actual(ushort CardNo, ushort axis, ref double encoder_actval);//读取指定轴编码器实际计数值；
        //辅助编码器	
        [DllImport("MCCE135.dll", EntryPoint = "mcc_assist_encoder_set_counter_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_assist_encoder_set_counter_inmode(ushort CardNo, ushort encoder_no, ushort mode);//设置辅助编码器计数方式
        [DllImport("MCCE135.dll", EntryPoint = "mcc_assist_encoder_get_counter_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_assist_encoder_get_counter_inmode(ushort CardNo, ushort encoder_no, ref ushort mode);//读取辅助编码器计数方式
        [DllImport("MCCE135.dll", EntryPoint = "mcc_assist_encoder_set_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_assist_encoder_set_value(ushort CardNo, ushort encoder_no, int encoder_value); //设置辅助编码器计数值
        [DllImport("MCCE135.dll", EntryPoint = "mcc_assist_encoder_get_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_assist_encoder_get_value(ushort CardNo, ushort encoder_no, ref int encoder_value); //读取辅助编码器计数值
        [DllImport("MCCE135.dll", EntryPoint = "mcc_assist_encoder_set_reverse", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_assist_encoder_set_reverse(ushort CardNo, ushort encoder_no, ushort intreverse_enable); //设置辅助编码器计数值取反使能;
        [DllImport("MCCE135.dll", EntryPoint = "mcc_assist_encoder_get_reverse", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_assist_encoder_get_reverse(ushort CardNo, ushort encoder_no, ref ushort intreverse_enable); //读取辅助编码器计数值取反使能;
        //轴到位参数          
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_set_arrive_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_set_arrive_para(ushort CardNo, ushort axis, double arrive_offset, double hold_time); //读取辅助编码器计数值
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_get_arrive_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_get_arrive_para(ushort CardNo, ushort axis, ref double arrive_offset, ref double hold_time); //读取辅助编码器计数值	
        //轴状态清除
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_clear_stop_reason", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_clear_stop_reason(ushort CardNo, ushort axis);
        //单轴减速停止时间设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_set_dstp_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_set_dstp_time(ushort CardNo, ushort axis, double time);// 
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_get_dstp_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_get_dstp_time(ushort CardNo, ushort axis, ref double time);// 
        //插补系减速停止时间设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_set_dstp_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_set_dstp_time(ushort CardNo, ushort crd, double time);// 
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_get_dstp_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_get_dstp_time(ushort CardNo, ushort crd, ref double time);// 
        //轴状态检测函数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_read_current_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double mcc_status_read_current_speed(ushort CardNo, ushort axis);//读取指定轴的当前速度
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_get_axis_prf_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double mcc_status_get_axis_prf_vel(ushort CardNo, ushort axis);  //读取单轴的规划速度         
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_get_axis_prf_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double mcc_status_get_axis_prf_pos(ushort CardNo, ushort axis);  //读取单轴的规划位置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_check_axis_busy", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_status_check_axis_busy(ushort CardNo, ushort axis);	//检测指定轴规划位置是否发送完毕
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_check_axis_arrive", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_status_check_axis_arrive(ushort CardNo, ushort axis);	//判断指定轴编码器是否到位  
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_get_axis_io", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint mcc_status_get_axis_io(ushort CardNo, ushort axis);	    //读取指定轴运动信号的状态:ALM、SVON、Busy、INP、Home、EL+、EL-、SL+、SL-、EMG、Warnig等  
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_get_axis_run_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_status_get_axis_run_mode(ushort CardNo, ushort axis);  //读取轴运动模式
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_get_stop_reason", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_get_stop_reason(ushort CardNo, ushort axis, ref int StopReason);//读取轴停止原因
                                                                                                            //==============================================================================================================================================================
                                                                                                            //电子齿轮 
        [DllImport("MCCE135.dll", EntryPoint = "mcc_gear_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_gear_set_para(ushort CardNo, ushort SlaveAxis, ref TGearPara_Set pGearPara_Set);//电子齿轮参数设置；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_gear_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_gear_get_para(ushort CardNo, ushort SlaveAxis, ref TGearPara_Get pGearPara_Get); //电子齿轮参数读取；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_gear_update_scale_online", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_gear_update_scale_online(ushort CardNo, ushort SlaveAxis, double MasterScale, double SlaveScale, double MasterSlopeDis); //在线更新主从轴跟随比例；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_gear_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_gear_start(ushort CardNo, ushort SlaveAxis); //电子齿轮运动启动；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_gear_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_gear_stop(ushort CardNo, ushort SlaveAxis, ushort StopMode); //电子齿轮运动停止；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_gear_reset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_gear_reset(ushort CardNo, ushort SlaveAxis);// 电子齿轮关系销毁；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_get_gear_sts", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_status_get_gear_sts(ushort CardNo, ushort SlaveAxis, ref ushort pGearSts); //电子齿轮状态读取；
                                                                                                                  //==============================================================================================================================================================       

        //PWM功能、PWM输出 
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pwm_set_on_off", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pwm_set_on_off(ushort CardNo, ushort pwm_no, ushort on_off); //设置PWM输出口开关
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pwm_get_on_off", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pwm_get_on_off(ushort CardNo, ushort pwm_no, ref ushort on_off); //读取PWM输出口开关
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pwm_set_output_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pwm_set_output_para(ushort CardNo, ushort pwm_no, double fDuty, double fFre);//设置PWM 立即输出
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pwm_get_output_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pwm_get_output_para(ushort CardNo, ushort pwm_no, ref double fDuty, ref double fFre);//读取PWM 立即输出设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pwm_set_on_delay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pwm_set_on_delay(ushort CardNo, ushort pwm_no, ushort on_delay);//设置PWM延时输出时间
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pwm_get_on_delay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pwm_get_on_delay(ushort CardNo, ushort pwm_no, ref ushort on_delay);//读取PWM延时输出时间；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pwm_set_off_delay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pwm_set_off_delay(ushort CardNo, ushort pwm_no, ushort off_delay);//设置PWM延时关闭时间
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pwm_get_off_delay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pwm_get_off_delay(ushort CardNo, ushort pwm_no, ref ushort off_delay);//读取PWM延时关闭时间

        //本地高速位置锁存设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ltc_local_latch_open", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ltc_local_latch_open(ushort CardNo, ushort LtcNo);//本地锁存关闭
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ltc_local_latch_set_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ltc_local_latch_set_mode(ushort CardNo, ushort LtcNo, ushort ltc_mode, ushort ltc_source, ushort ltc_logic, ushort ltc_order);//本地位置锁存设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ltc_local_latch_get_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ltc_local_latch_get_mode(ushort CardNo, ushort LtcNo, ref ushort ltc_mode, ref ushort ltc_source, ref ushort ltc_logic, ref ushort ltc_order);//本地位置锁存读取
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ltc_local_latch_get_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ltc_local_latch_get_status(ushort CardNo, ushort LtcNo, ref ushort ltc_num, ref ushort ltc_remain, ref ushort ltc_sts);//本地锁存标志读取
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ltc_local_latch_get_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ltc_local_latch_get_pos(ushort CardNo, ushort LtcNo, ushort ltc_count, double[] ltc_poslist);//本地锁存位置读取
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ltc_local_latch_clear", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ltc_local_latch_clear(ushort CardNo, ushort LtcNo);//本地位置锁存清除
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ltc_local_latch_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ltc_local_latch_close(ushort CardNo, ushort LtcNo);//本地锁存关闭
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ltc_local_latch_set_multi_axis_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ltc_local_latch_set_multi_axis_mode(ushort CardNo, ushort LtcNo, ushort ltc_mode, ushort ltc_source, ushort ltc_logic, ushort ltc_order_num, ushort[] ltc_order, ushort logic_num);//本地位置多轴锁存设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ltc_local_latch_get_multi_axis_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ltc_local_latch_get_multi_axis_mode(ushort CardNo, ushort LtcNo, ref ushort ltc_mode, ref ushort ltc_source, ref ushort ltc_logic, ref ushort ltc_order_num, ushort[] ltc_order, ref ushort logic_num);//读取本地位置多轴锁存
        //---------------------------------------------------------------------------
        //驱动器位置锁存设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ltc_servo_latch_open", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ltc_servo_latch_open(ushort CardNo, ushort Axis, ushort LtcNo);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ltc_servo_latch_set_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ltc_servo_latch_set_mode(ushort CardNo, ushort Axis, ushort LtcNo, ushort ltc_mode, ushort ltc_trigtype, ushort ltc_logic);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ltc_servo_latch_get_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ltc_servo_latch_get_mode(ushort CardNo, ushort Axis, ushort LtcNo, ref ushort ltc_mode, ref ushort ltc_trigtype, ref ushort ltc_logic);//驱动器位置锁存读取
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ltc_servo_latch_get_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ltc_servo_latch_get_status(ushort CardNo, ushort Axis, ushort LtcNo, ref ushort ltc_num, ref ushort ltc_remain, ref ushort ltc_sts);//驱动器锁存标志读取
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ltc_servo_latch_get_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ltc_servo_latch_get_pos(ushort CardNo, ushort Axis, ushort LtcNo, ushort ltc_count, double[] ltc_poslist); //驱动器锁存位置读取
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ltc_servo_latch_clear", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ltc_servo_latch_clear(ushort CardNo, ushort axis, ushort LtcNo);//驱动器位置锁存清除
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ltc_servo_latch_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ltc_servo_latch_close(ushort CardNo, ushort Axis, ushort LtcNo); //关闭伺服锁存器；
        //PSO(同步位置比较输出)功能 1D、2D、3D的PSO比较输出
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pso_1d_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pso_1d_set_para(ushort CardNo, ushort psoIO, short psoSource, ushort axis, double psoPos, ushort activeLevel, double psoTime);//设置一维PSO配置参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pso_1d_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pso_1d_get_para(ushort CardNo, ushort psoIO, ref short psoSource, ref ushort axis, ref double psoPos, ref ushort activeLevel, ref double psoTime);//读取一维PSO配置参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pso_2d_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pso_2d_set_para(ushort CardNo, ushort psoIO, short psoSource, ushort[] axisList, double psoPos, ushort activeLevel, double psoTime);//设置二维PSO配置参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pso_2d_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pso_2d_get_para(ushort CardNo, ushort psoIO, ref short psoSource, ushort[] axisList, ref double psoPos, ref ushort activeLevel, ref double psoTime);//读取二维PSO配置参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pso_3d_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pso_3d_set_para(ushort CardNo, ushort psoIO, short psoSource, ushort[] axisList, double psoPos, ushort activeLevel, double psoTime);//设置三维PSO配置参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pso_3d_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pso_3d_get_para(ushort CardNo, ushort psoIO, ref short psoSource, ushort[] axisList, ref double psoPos, ref ushort activeLevel, ref double psoTime);//读取三维PSO配置参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pso_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pso_start(ushort CardNo, ushort psoIO);//启动PSO
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pso_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pso_stop(ushort CardNo, ushort psoIO);//停止PSO；
  	         
        //一维位置比较---低速位置比较 (日志打印模式设置为1D_LCMP_CNTL_DEBUG)
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_1d_low_set_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_1d_low_set_config(ushort CardNo, ushort axis, ushort enable, ushort cmp_source);//配置比较器
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_1d_low_get_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_1d_low_get_config(ushort CardNo, ushort axis, ref ushort enable, ref ushort cmp_source);//读取配置比较器
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_1d_low_clear_points", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_1d_low_clear_points(ushort CardNo, ushort axis);//清除所有比较点
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_1d_low_add_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_1d_low_add_point(ushort CardNo, ushort axis, double pos, ushort dir, ushort action,int pulse_width,int actpara);//添加比较点
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_1d_low_get_compare_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_1d_low_get_compare_status(ushort CardNo, ushort axis, ref double pos, ref int runNum, ref int remainNum);//读取当前比较状态
	//-----------------------------------------------------------------------------------------------------------------
        //多维位置比较---低速位置比较	
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_xd_low_set_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_xd_low_set_config(ushort CardNo, ushort cmper_no, ushort cmp_axis_num, ushort[] axis_list, ushort enable, ushort cmp_source);//配置比较器
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_xd_low_get_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_xd_low_get_config(ushort CardNo, ushort cmper_no, ref ushort cmp_axis_num, ushort[] axis_list, ref ushort enable, ref ushort cmp_source);//读取配置比较器
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_xd_low_clear_points", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_xd_low_clear_points(ushort CardNo, ushort cmper_no);//清除所有比较点
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_xd_low_add_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_xd_low_add_point(ushort CardNo, ushort cmper_no, ushort cmp_point_num, ushort cmp_method, ushort cmp_action, int pulse_width, int actpara, double[] cmp_pos);//添加比较点
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_xd_low_get_compare_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_xd_low_get_compare_status(ushort CardNo, ushort cmper_no, ref ushort cmp_point_num, double[] cmp_pos, ref int runNum, ref int remainNum);//读取当前比较状态            
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_xd_low_add_points_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_xd_low_add_points_ex(ushort CardNo, ushort cmper_no, ushort cmp_point_num, ushort cmp_method, ushort cmp_action, int pulse_width, ushort out_port_num, ushort[] output_port_list, double[] cmp_pos_list); 
        //一维高速位置比较 (1D位置比较输出)
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_1d_open", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_1d_open(ushort CardNo, ushort CmpNum, ushort[] CmpOpenList);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_1d_set_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_1d_set_mode(ushort CardNo, ushort CmpNum, ushort[] CmpList, ushort CmpMode, ushort CmpSource, ushort CmpAxis, ushort CmpLogic, ushort CmpPulseNum, int CmpPulseTime, ushort pulseSpace);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_1d_get_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_1d_get_mode(ushort CardNo, ushort CmpNum, ref ushort CmpMode, ref ushort CmpSource, ref ushort CmpAxis, ref ushort CmpLogic, ref ushort CmpPulseNum, ref int CmpPulseTime, ref ushort pulseSpace);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_1d_set_offset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_1d_set_offset(ushort CardNo, ushort CmpNo, double offset); //一维位置比较器偏差设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_1d_get_offset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_1d_get_offset(ushort CardNo, ushort CmpNo, ref double offset);//一维位置比较器偏差读取
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_1d_add_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_1d_add_point(ushort CardNo, ushort CmpNo, ushort CmpPosNum, double[] CmpPosList);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_1d_set_liner_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_1d_set_liner_para(ushort CardNo, ushort CmpNo, double CmpStepLength, int CmpStepNum);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_1d_get_liner_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_1d_get_liner_para(ushort CardNo, ushort CmpNo, ref double CmpStepLength, ref int CmpStepNum);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_1d_clear_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_1d_clear_point(ushort CardNo, ushort CmpNo);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_1d_get_current_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_1d_get_current_status(ushort CardNo, ushort CmpNo, ref ushort CmpSts, ref double CmpNxetPoint, ref int CmpOutputPoint, ref int CmpRemainSpace); //设置线性比较模式的单位增量和比较次数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_1d_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_1d_close(ushort CardNo, ushort CmpNum, ushort[] CmpOpenList);
        //二维高速位置比较 
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_2d_open", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_2d_open(ushort CardNo, ushort CmpNum, ushort[] CmpOpenList);//打开二维位置比较器
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_2d_set_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_2d_set_mode(ushort CardNo, ushort CmpNum, ushort[] CmpList, ushort CmpMode, ushort CmpSource, ushort[] CmpAxisList, ushort CmpLogic, ushort CmpPulseNum, int CmpPulseTime, ushort CmpPulseDis);//二维位置比较器配置(可同时配置多个比较器)；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_2d_get_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_2d_get_mode(ushort CardNo, ushort CmpNo, ref ushort CmpMode, ref ushort CmpSource, ushort[] CmpAxis, ref ushort CmpLogic, ref ushort CmpPulseNum, ref int CmpPulseTime, ref ushort CmpPulseDis);//读取位置比较器配置(只能一个个读取)；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_2d_set_offset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_2d_set_offset(ushort CardNo, ushort CmpNo, double offset);//设置二维位置比较误差值；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_2d_get_offset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_2d_get_offset(ushort CardNo, ushort CmpNo, ref double offset);//读取二维位置比较误差值；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_2d_add_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_2d_add_point(ushort CardNo, ushort CmpNo, ushort CmpPosNum, double[] CmpPosList_X, double[] CmpPosList_Y);//向需要进行位置比较的比较器缓存区内压入位置数据；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_2d_clear_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_2d_clear_point(ushort CardNo, ushort CmpNo);//清除比较器缓存区所有位置数据；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_2d_get_current_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_2d_get_current_status(ushort CardNo, ushort CmpNo, ref ushort CmpSts, double[] CmpNxetPoint, ref int CmpOutputPoint, ref int CmpRemainSpace);//读取高速二维位置比较输出状态；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_cmp_2d_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_cmp_2d_close(ushort CardNo, ushort CmpNum, ushort[] CmpOpenList);//关闭位置比较器；
        //螺距补偿功能
        [DllImport("MCCE135.dll", EntryPoint = "mcc_unit_offset_set_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_unit_offset_set_enable(ushort CardNo, ushort axis, ushort enable);//设置螺距误差补偿功能使能
        [DllImport("MCCE135.dll", EntryPoint = "mcc_unit_offset_get_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_unit_offset_get_enable(ushort CardNo, ushort axis, ref ushort enable);//读取螺距误差补偿功能使能
        [DllImport("MCCE135.dll", EntryPoint = "mcc_unit_offset_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_unit_offset_set_para(ushort CardNo, ushort axis, ushort DataNum, int StartPos, int lenPos, int[] PosList_P, int[] PosList_N);//设置螺距误差补偿的相关参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_unit_offset_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_unit_offset_get_para(ushort CardNo, ushort axis, ref ushort DataNum, ref int StartPos, ref int lenPos, int[] PosList_P, int[] PosList_N);//读取螺距误差补偿的相关参数                             
        //反向间隙补偿
        [DllImport("MCCE135.dll", EntryPoint = "mcc_backlash_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_backlash_set_para(ushort CardNo, ushort axis, uint lash_value, uint delta_value, int lash_dir);//设定指定轴的反向间隙值
        [DllImport("MCCE135.dll", EntryPoint = "mcc_backlash_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_backlash_get_para(ushort CardNo, ushort axis, ref uint lash_value, ref uint backlash_interval, ref int lash_dir);
        //===========================================================================================================================  
        //AD/DA输出
        [DllImport("MCCE135.dll", EntryPoint = "mcc_da_set_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_da_set_enable(ushort CardNo, ushort enable);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_da_get_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_da_get_enable(ushort CardNo, ref ushort enable);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_da_get_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_da_get_output(ushort CardNo, ushort channel, ref double Vout); //设置DA输出
        [DllImport("MCCE135.dll", EntryPoint = "mcc_da_set_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_da_set_output(ushort CardNo, ushort channel, double Vout); //设置DA输出
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ad_get_input", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ad_get_input(ushort CardNo, ushort channel, ref double Vout); ////读取AD输入(电压)
        //非缓冲区插补速度设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_set_prf_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_set_prf_vel(ushort CardNo, ushort Crd, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Stop_Vel);//非缓冲区设置插补运动速度曲线
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_get_prf_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_get_prf_vel(ushort CardNo, ushort Crd, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double Stop_Vel);//非缓冲获取区读取插补运动速度曲线
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_set_prf_s_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_set_prf_s_vel(ushort CardNo, ushort Crd, ushort s_mode, double s_para);  //非缓冲区设置插补运动速度曲线的平滑时间   
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_get_prf_s_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_get_prf_s_vel(ushort CardNo, ushort Crd, ushort s_mode, ref double s_para);  //非缓冲区读取插补运动速度曲线的平滑时间
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_set_prf_axix_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_set_prf_axix_vel(ushort CardNo, ushort Crd, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Stop_Vel);//非缓冲区设置插补运动速度曲线
        //非缓冲区插补运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_line", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_line(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] DistList, ushort posi_mode); //非缓冲区指定轴直线插补运动，2-16轴直线插补   
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_circle_center", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_circle_center(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, double[] cen_pos, ushort arc_dir, ushort circle_num, ushort posi_mode);//非缓冲区圆心圆弧插补，螺旋线插补  圆心+终点      
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_circle_radius", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_circle_radius(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, double radius, ushort arc_dir, ushort circle_num, ushort posi_mode);//非缓冲区半径圆弧插补，螺旋线插补 半径+终点    
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_circle_3points", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_circle_3points(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, double[] mid_pos, ushort circle_num, ushort posi_mode);//非缓冲区三点圆弧插补，螺旋线插补 三点圆弧
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_stop(ushort CardNo, ushort Crd, ushort stop_mode);//插补系停止操作(适用于单段插补)
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_axis_line", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_axis_line(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] DistList, ushort posi_mode); //非缓冲区指定轴直线插补运动，2-16轴直线插补           
        //非缓冲区单段插补状态操作
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_get_crd_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_status_get_crd_vel(ushort CardNo, ushort Crd, ref double pSynVel);//读取插补运动的矢量速度
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_check_crd_sts", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_status_check_crd_sts(ushort CardNo, ushort Crd);//检测坐标系状态 :0-运行，1-暂停，2-正常停止，3-未启动，4-空闲
        //缓冲区连续插补小线段前瞻===================================================================================================================================================================
        //建立缓冲区与前瞻设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_open", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_open(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList);//设置坐标系参数，确立坐标系映射，建立坐标系  unsigned short axisNum,unsigned short *AxisList
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_set_lookahead_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_set_lookahead_para(ushort CardNo, ushort Crd, uint enable, double T, double accMax);//初始化插补前瞻缓存区
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_get_lookahead_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_get_lookahead_para(ushort CardNo, ushort Crd, ref uint enable, ref double T, ref double accMax);//查询插补前瞻缓存区
        //插补速度设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_set_prf_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_set_prf_vel(ushort CardNo, ushort Crd, double MinVel, double MaxVel, double Tacc, double Tdec, double Stop_Vel);//非缓冲区插补速度设置//设置插补运动速度曲线
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_get_prf_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_get_prf_vel(ushort CardNo, ushort Crd, ref double MinVel, ref double MaxVel, ref double Tacc, ref double Tdec, ref double StopVel);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_set_prf_s_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_set_prf_s_vel(ushort CardNo, ushort Crd, ushort s_mode, double s_para); //非缓冲区设置S型速度曲线参数//设置插补运动速度曲线的平滑时间
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_get_prf_s_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_get_prf_s_vel(ushort CardNo, ushort Crd, ushort s_mode, ref double s_para);
        //缓冲区连续插补
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_line", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_line(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, short posi_mode);//直线插补
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_circle_center", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_circle_center(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, double[] cen_pos, ushort circleDir, ushort circle_num, short posi_mode);//圆心插补
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_circle_radius", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_circle_radius(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, double radius, ushort circleDir, ushort circle_num, short posi_mode);//半径圆弧插补
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_circle_3points", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_circle_3points(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, double[] mid_pos, ushort circle_num, short posi_mode);//三点圆弧插补
        //缓冲区输出IO设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_write_block_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_write_block_outbit(ushort CardNo, ushort Crd, ushort biton, ushort on_off, double ReverseTime);//缓存区阻塞式单个Do操作指令
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_write_nblock_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_write_nblock_outbit(ushort CardNo, ushort Crd, ushort biton, ushort on_off, double ReverseTime);// 缓存区非阻塞式单个Do操作指令
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_write_nblock_outbyte", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_write_nblock_outbyte(ushort CardNo, ushort Crd, ushort IO_num, ushort[] ByteNo, ushort[] ByteValue, double[] ReverseTime);//缓存区非阻塞式多个Do操作指令
        //缓冲区启动、停止与暂停
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_move_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_move_start(ushort CardNo, ushort Crd);//启动缓冲区插补运动   
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_stop(ushort CardNo, ushort Crd, ushort mode);//停止缓冲区插补运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_move_pause", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_move_pause(ushort CardNo, ushort Crd);//暂停缓冲区插补运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_move_full_pause", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_move_full_pause(ushort CardNo, ushort Crd);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_move_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_move_stop(ushort CardNo, ushort Crd, ushort mode);//没有加实体函数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_add_data_finish", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_add_data_finish(ushort CardNo, ushort Crd);//缓冲区插补轨迹添加结束指令
        //关闭缓冲区   
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_close(ushort CardNo, ushort Crd); //关闭坐标系（运动停止，清除缓冲区数据，再关闭坐标系）
        //状态操作
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_get_crd_buf_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_status_get_crd_buf_vel(ushort CardNo, ushort Crd, ref double pSynVel);//查询连续插补坐标系的当前坐标速度值
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_check_crd_buf_sts", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_status_check_crd_buf_sts(ushort CardNo, ushort Crd);//检测连续坐标系状态 :0-运行，1-暂停，2-正常停止，3-未启动，4-空闲
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_get_crd_buf_remain_space", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 mcc_status_get_crd_buf_remain_space(ushort CardNo, ushort Crd);//查询连续插补缓冲区剩余插补空间10000
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_get_crd_buf_current_mark", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 mcc_status_get_crd_buf_current_mark(ushort CardNo, ushort Crd);//读取连续插补缓冲区当前插补段号
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_get_crd_buf_finish_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_status_get_crd_buf_finish_num(ushort CardNo, ushort Crd, ref Int32 pSegment);//查询插补运动坐标系状态
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_get_crd_buf_pause_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_status_get_crd_buf_pause_pos(ushort CardNo, ushort Crd, double[] pausePosList);//读取连续插补暂停时的位置 
        //等待延时与延时输出
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_delay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_delay(ushort CardNo, ushort Crd, ushort delayTime);//缓存区指令，缓存区内延时设置指令
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_wait_di", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_wait_di(ushort CardNo, ushort Crd, ushort dI_id, ushort di_logic, Int32 time_out);//缓存区等待通用输入IO指令。
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_set_pause_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_set_pause_output(ushort CardNo, ushort Crd, ushort action, Int32 mask, Int32 state);//设置连续插补暂停及异常停止时 IO 输出状态
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_get_pause_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_get_pause_output(ushort CardNo, ushort Crd, ref ushort action, ref Int32 mask, ref Int32 state);//读取连续插补暂停及异常停止时 IO 输出状态设置
        //超前与滞后IO控制
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_delay_outbit_to_start_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_delay_outbit_to_start_pos(ushort CardNo, ushort Crd, ushort bitno, ushort on_off, double delay_value, ushort delay_mode, double ReverseTime);//连续插补中相对于轨迹段起点IO滞后输出(段内执行)
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_delay_outbit_to_stop_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_delay_outbit_to_stop_pos(ushort CardNo, ushort Crd, ushort bitno, ushort on_off, double delay_time, double ReverseTime);//连续插补中相对于轨迹段终点IO滞后输出
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_ahead_outbit_to_stop_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_ahead_outbit_to_stop_pos(ushort CardNo, ushort Crd, ushort bitno, ushort on_off, double ahead_value, ushort ahead_mode, double ReverseTime);//连续插补中现对于轨迹段终点IO提前输出；
        //清除段内未执行完的 IO 动作
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_clear_io_action", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_clear_io_action(ushort CardNo, ushort Crd, uint Io_Mask);//清除段内未执行完的 IO 动作
        //刀向跟随
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_blade_following_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_blade_following_move(ushort CardNo, ushort Crd, ushort moveAxis, double pos, double vel, double acc_time);//实现刀向跟随功能，启动某个轴点位运动
        //自定义插补段号
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_set_user_segment_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_set_user_segment_num(ushort CardNo, ushort Crd, Int32 segNum);//设置自定义插补段段号
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_get_user_segment_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_get_user_segment_num(ushort CardNo, ushort Crd, ref Int32 segNum);//读取自定义插补段段号
        //清除缓冲区的错误
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_clear_error", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_clear_error(ushort CardNo, ushort Crd);//清除插补系错误状态
        //设置缓冲区标志阻塞参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_set_control_flag_block", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_set_control_flag_block(ushort CardNo, ushort Crd, ushort group, ushort flag_num, ushort type, double paravalue);//设置缓冲区标志阻塞参数
        //PT-PVT运动 单轴PT运动-多轴PT运动-单轴PVT运动-多轴PVT运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ptt_table", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ptt_table(ushort CardNo, ushort iaxis, uint count, double[] pTime, Int32[] pPos);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pts_table", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pts_table(ushort CardNo, ushort iaxis, uint count, double[] pTime, Int32[] pPos, double[] pPercent);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pt_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pt_move(ushort CardNo, ushort Axis); //启动单轴PT运动；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pvt_table", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pvt_table(ushort CardNo, ushort iaxis, uint count, double[] pTime, Int32[] pPos, double[] pVel);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pvts_table", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pvts_table(ushort CardNo, ushort iaxis, uint count, double[] pTime, Int32[] pPos, double velBegin, double velEnd);
        [DllImport("MCCE135.dll", EntryPoint = "mcc_pvt_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_pvt_move(ushort CardNo, ushort AxisNum, ushort[] AxisList);
        //---------------------add 20240709-----------------------------------------------------------------------
        //ptp参数配置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_set_pmove_extra", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_set_pmove_extra(ushort CardNo, ushort axis, double MinVel, double MaxVel, double Tacc, double Tdec, double StopVel, ushort s_mode, double s_para, double dist, ushort posi_mode); //设置指定轴PTP运动参数；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_axis_get_pmove_extra", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_axis_get_pmove_extra(ushort CardNo, ushort axis, ref double MinVel, ref double MaxVel, ref double Tacc, ref double Tdec, ref double StopVel, ref ushort s_mode, ref double s_para);//读取指定轴PTP运动参数
                                                                                                                                                                                                                          //---------------------add 20240716--------------------------------------------------------------------------
                                                                                                                                                                                                                          //龙门专用
        [DllImport("MCCE135.dll", EntryPoint = "ecc_set_gantry_axis_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_gantry_axis_enable(ushort CardNo, ushort[] axislist);//EtherCAT总线龙门轴上使能(高创龙门专用);
        [DllImport("MCCE135.dll", EntryPoint = "ecc_set_gantry_axis_disable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_gantry_axis_disable(ushort CardNo, ushort[] axislist);//EtherCAT总线龙门轴下使能(高创龙门专用); 
                                                                                                 //---------------------add 20240827----------------------------------------------------------------------------------------------
                                                                                                 //总线多轴同步运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_sync_pmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_sync_pmove(ushort CardNo, ushort AxisNum, ushort[] AxisList, double[] Dist, ushort[] PosiMode);//多轴同步PTP运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_sync_vmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_sync_vmove(ushort CardNo, ushort AxisNum, ushort[] AxisList, ushort[] Dir);//多轴同步Jog运动
                                                                                                                  //圆弧区域限位
        [DllImport("MCCE135.dll", EntryPoint = "mcc_circle_area_limit_open", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_circle_area_limit_open(ushort CardNo, ushort ChnNo); //打开圆形区域限位通道；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_circle_area_limit_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_circle_area_limit_set_para(ushort CardNo, ushort ChnNo, ushort AxisNum, ushort[] AxisList, double[] CenPos, double Radius, ushort Source, ushort StopMode);//设置圆形区域限位参数；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_circle_area_limit_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_circle_area_limit_get_para(ushort CardNo, ushort ChnNo, ref ushort AxisNum, ushort[] AxisList, double[] CenPos, ref double Radius, ref ushort Source, ref ushort StopMode);//读取圆形区域限位参数；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_circle_area_limit_get_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_circle_area_limit_get_status(ushort CardNo, ushort ChnNo, ref ushort Status);//获取圆形区域限位通道状态；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_circle_area_limit_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_circle_area_limit_close(ushort CardNo, ushort ChnNo);//关闭圆形区域限位通道；
                                                                                            //--------------------add 20240902-------------------------------------------------------------------------
                                                                                            //多段速-低速变高速
        [DllImport("MCCE135.dll", EntryPoint = "mcc_multistage_vel_low_high_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_multistage_vel_low_high_set_para(ushort CardNo, ushort axis, ref TMultiStageVelLH_Set pMultiStageVelLH_Set);//设置多段速-低速变高速运动参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_multistage_vel_low_high_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_multistage_vel_low_high_get_para(ushort CardNo, ushort axis, ref TMultiStageVelLH_Get pMultiStageVelLH_Get);//读取多段速-低速变高速运动参数；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_multistage_vel_low_high_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_multistage_vel_low_high_start(ushort CardNo, ushort axis);//启动多段速-低速变高速运动
                                                                                                 //多段速-高速变低速
        [DllImport("MCCE135.dll", EntryPoint = "mcc_multistage_vel_high_low_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_multistage_vel_high_low_set_para(ushort CardNo, ushort axis, ref TMultiStageVelHL_Set pMultiStageVelHL_Set);//设置多段速-高速变低速运动参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_multistage_vel_high_low_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_multistage_vel_high_low_get_para(ushort CardNo, ushort axis, ref TMultiStageVelHL_Get pMultiStageVelHL_Get);//读取多段速-高速变低速运动参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_multistage_vel_high_low_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_multistage_vel_high_low_start(ushort CardNo, ushort axis);// 启动多段速 - 高速变低速运动；
                                                                                                 //低高低三段速
        [DllImport("MCCE135.dll", EntryPoint = "mcc_multistage_vel_low_high_Low_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_multistage_vel_low_high_Low_set_para(ushort CardNo, ushort axis, ref TMultiStageVelLHL_Set pMultiStageVelLHL_Set);//设置多段速-低高低三段速运动参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_multistage_vel_low_high_Low_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_multistage_vel_low_high_Low_get_para(ushort CardNo, ushort axis, ref TMultiStageVelLHL_Get pMultiStageVelLHL_Get);//读取多段速-低高低三段速运动参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_multistage_vel_low_high_Low_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_multistage_vel_low_high_Low_start(ushort CardNo, ushort axis); //启动多段速 - 低高低三段速运动
                                                                                                      //高低高三段速
        [DllImport("MCCE135.dll", EntryPoint = "mcc_multistage_vel_high_Low_high_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_multistage_vel_high_Low_high_set_para(ushort CardNo, ushort axis, ref TMultiStageVelHLH_Set pMultiStageVelHLH_Set);//设置多段速-高低高三段速运动参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_multistage_vel_high_Low_high_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_multistage_vel_high_Low_high_get_para(ushort CardNo, ushort axis, ref TMultiStageVelHLH_Get pMultiStageVelHLH_Get);//读取多段速-高低高三段速运动参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_multistage_vel_high_Low_high_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_multistage_vel_high_Low_high_start(ushort CardNo, ushort axis); //启动多段速 - 高低高三段速运动
        //===================以下为EthCat总线配置==========================================================
        //总线回零功能
        //设置 EtherCAT 总线轴回零参数
        [DllImport("MCCE135.dll", EntryPoint = "ecc_home_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_home_set_para(ushort CardNo, ushort axis, short home_mode, double low_vel, double high_vel, double tacc, double tdec);//设置 EtherCAT 总线轴回零参数;
        [DllImport("MCCE135.dll", EntryPoint = "ecc_home_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_home_get_para(ushort CardNo, ushort axis, ref short home_mode, ref double low_vel, ref double high_vel, ref double tacc, ref double tdec);//读取EtherCAT 总线轴回零参数 
        [DllImport("MCCE135.dll", EntryPoint = "ecc_home_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_home_start(ushort CardNo, ushort axis);//启动 EtherCAT 总线轴回零
        [DllImport("MCCE135.dll", EntryPoint = "ecc_home_get_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_home_get_status(ushort CardNo, ushort axis, ref ushort status);//读取总线回零状态 
        [DllImport("MCCE135.dll", EntryPoint = "ecc_home_set_offset_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_home_set_offset_para(ushort CardNo, ushort axis, ushort control_mode, ushort offsetmode, double offset_pos);//设置EtherCAT总线轴回零偏移位置参数
        [DllImport("MCCE135.dll", EntryPoint = "ecc_home_get_offset_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_home_get_offset_para(ushort CardNo, ushort axis, ref ushort control_mode, ref ushort offsetmode, ref double offset_pos);//读取EtherCAT 总线轴回零偏移位置参数
        [DllImport("MCCE135.dll", EntryPoint = "ecc_home_set_mode_switch_delay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_home_set_mode_switch_delay(ushort CardNo, ushort axis, ushort enable, int delay_time);//设置回零完成切换模式
        [DllImport("MCCE135.dll", EntryPoint = "ecc_home_get_mode_switch_delay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_home_get_mode_switch_delay(ushort CardNo, ushort axis, ref ushort enable, ref int delay_time);//读取回零完成切换模式   
        //总线操作相关函数     
        [DllImport("MCCE135.dll", EntryPoint = "ecc_set_node_od", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_node_od(ushort CardNo, ushort PortNum, ushort NodeNum, ushort Index, ushort SubIndex, ushort ValLength, int Value);//设置从站对象字典参数值  
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_node_od", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_node_od(ushort CardNo, ushort PortNum, ushort NodeNum, ushort Index, ushort SubIndex, ushort ValLength, ref int Value);//读取从站对象字典参数值设置   
        [DllImport("MCCE135.dll", EntryPoint = "ecc_set_axis_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_axis_enable(ushort CardNo, ushort axis);//使能 EtherCAT 总线驱动器      
        [DllImport("MCCE135.dll", EntryPoint = "ecc_set_axis_disable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_axis_disable(ushort CardNo, ushort axis);//失能 EtherCAT 总线驱动器
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_total_axisnum", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_total_axisnum(ushort CardNo, ref uint TotalAxis);//读取 EtherCAT 总线轴和虚拟轴轴数-获取总线轴数
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_total_adcnum", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_total_adcnum(ushort CardNo, ref ushort TotalIn, ref ushort TotalOut);//获取EtherCAT总线AD/DA输入输出口数  
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_total_ionum", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_total_ionum(ushort CardNo, ref ushort TotalIn, ref ushort TotalOut);//获取EtherCAT总线IO输入输出口数    
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_total_slaves", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_total_slaves(ushort CardNo, ushort PortNum, ref ushort TotalSlaves);//设置EtherCAT总线循环周期(us)
        [DllImport("MCCE135.dll", EntryPoint = "ecc_set_cycletime", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_cycletime(ushort CardNo, ushort PortNum, uint CycleTime);//设置ethercat总线循环周期(us)   
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_cycletime", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_cycletime(ushort CardNo, ushort PortNum, ref uint CycleTime);//获取EtherCAT总线循环周期(us)      
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_axis_state_machine", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_axis_state_machine(ushort CardNo, ushort axis, ref ushort axis_StateMachine);//读取 EtherCAT 总线轴状态机-轴状态机
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_master_state_machine", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_master_state_machine(ushort CardNo, ref ushort masterStateMachine);//读取 EtherCAT 主站状态机
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_errcode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_errcode(ushort CardNo, ushort NodeNum, ref ushort Errcode);// 获取EtherCAT总线端口错误码
        [DllImport("MCCE135.dll", EntryPoint = "ecc_clear_errcode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_clear_errcode(ushort CardNo, ushort PortNum);// 清除EtherCAT总线端口错误码
        [DllImport("MCCE135.dll", EntryPoint = "ecc_set_fieldbus_error_switch", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_fieldbus_error_switch(ushort CardNo, ushort PortNum, short enable);  //设置总线掉线后是否能操作从站
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_fieldbus_error_switch", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_fieldbus_error_switch(ushort CardNo, ushort PortNum, ref short enable);  //读取设置的总线掉线后是否能操作从站
        [DllImport("MCCE135.dll", EntryPoint = "ecc_break_ecat", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_break_ecat(ushort CardNo, ushort PortNum);//断开 EtherCAT 总线
        [DllImport("MCCE135.dll", EntryPoint = "ecc_reset_ecat", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_reset_ecat(ushort CardNo, ushort PortNum);//复位 EtherCAT 总线
        [DllImport("MCCE135.dll", EntryPoint = "ecc_alias_set_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_alias_set_enable(ushort CardNo, ushort enable); //轴别名绑定使能设置
        [DllImport("MCCE135.dll", EntryPoint = "ecc_alias_get_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_alias_get_enable(ushort CardNo, ref ushort enable, ref ushort slave_num, ushort[] alias_list);//别名从站使能读取;   
        //总线补充相关函数
        [DllImport("MCCE135.dll", EntryPoint = "ecc_set_master_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_master_para(ushort CardNo, ushort PortNum, ushort Baudrate, ushort NodeCnt, ushort MasterId);//设置主站参数
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_master_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_master_para(ushort CardNo, ushort PortNum, ref ushort Baudrate, ref uint NodeCnt, ref ushort MasterId);//读取主站参数
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_errcode_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_errcode_ex(ushort CardNo, ushort channel, ref ushort Errcode);// 获取总线端口错误码
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_card_errcode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_card_errcode(ushort CardNo, ref ushort Errcode);// 获取控制卡错误码
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_axis_errcode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_axis_errcode(ushort CardNo, ushort axis, ref ushort Errcode);// 获取总线轴错误码
        [DllImport("MCCE135.dll", EntryPoint = "ecc_clear_errcode_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_clear_errcode_ex(ushort CardNo, ushort channel);// 清除总线端口错误码
        [DllImport("MCCE135.dll", EntryPoint = "ecc_clear_card_errcode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_clear_card_errcode(ushort CardNo);// 清除控制卡错误码
        [DllImport("MCCE135.dll", EntryPoint = "ecc_clear_axis_errcode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_clear_axis_errcode(ushort CardNo, ushort axis);// 清除总线轴错误码
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_axis_statusword", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_axis_statusword(ushort CardNo, ushort axis, ref Int32 statusushort);//获取轴状态字
        [DllImport("MCCE135.dll", EntryPoint = "ecc_set_axis_contrlmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_axis_contrlmode(ushort CardNo, ushort axis, Int32 Contrlmode);//设置总线字控制模式
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_axis_contrlmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_axis_contrlmode(ushort CardNo, ushort axis, ref Int32 Contrlmode);//获取总线字控制模式
        [DllImport("MCCE135.dll", EntryPoint = "ecc_set_axis_contrlword", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_axis_contrlword(ushort CardNo, ushort axis, Int32 Contrlushort);//设置总线轴控制字
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_axis_contrlword", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_axis_contrlword(ushort CardNo, ushort axis, ref Int32 Contrlushort);//获取总线轴控制字
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_axis_setting_contrlmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_axis_setting_contrlmode(ushort CardNo, ushort axis, ref Int32 contrlmode);   //获取轴配置控制模式，返回值
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_node_address", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_node_address(ushort CardNo, ushort port_num, ushort node_num, ref uint vendor_id, ref uint device_id, ref uint revision_id);// 获取轴的从站信息
        [DllImport("MCCE135.dll", EntryPoint = "ecc_stop_etc", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_stop_etc(ushort CardNo, ushort PortNum, ref ushort ETCState);//停止ethercat总线运行,返回0表示成功，其他参数表示不成功
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_axis_node_address", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_axis_node_address(ushort CardNo, ushort PortNum, ref ushort TotalSlaves);// 获取EtherCAT总线所有从站总数	
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_axis_node_address", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_axis_node_address(ushort CardNo, ushort axis, ref ushort SlaveAddr, ref ushort Sub_SlaveAddr);//获取EtherCAT轴的节点地址信息(获取从站信息)
        //总线链接
        [DllImport("MCCE135.dll", EntryPoint = "ecc_set_connect_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_connect_state(ushort CardNo, ushort NodeNum, ushort state, ushort baud);//总线链接，0-断开；1-连接；2-复位后自动连接
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_connect_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_connect_state(ushort CardNo, ref ushort NodeNum, ref ushort state);//0-断开；1-连接；2-异常
                                                                                                              //获取总线通用输入、输出IO口数
        [DllImport("MCCE135.dll", EntryPoint = "ecc_io_write_outslot", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_io_write_outslot(ushort CardNo, ushort SlaveID, ushort SlotID, ushort Value);//设置io输出16位
        [DllImport("MCCE135.dll", EntryPoint = "ecc_io_read_outslot", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_io_read_outslot(ushort CardNo, ushort SlaveID, ushort SlotID, ref ushort Value);//读取io输出16位
        [DllImport("MCCE135.dll", EntryPoint = "ecc_io_read_inslot", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_io_read_inslot(ushort CardNo, ushort SlaveID, ushort SlotID, ref ushort Value);//读取io输入16位
        [DllImport("MCCE135.dll", EntryPoint = "ecc_io_write_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_io_write_outbit(ushort CardNo, ushort SlaveID, ushort bitno, ushort on_off);//设置io输出
        [DllImport("MCCE135.dll", EntryPoint = "ecc_io_read_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_io_read_outbit(ushort CardNo, ushort SlaveID, ushort bitno, ref ushort on_off);//读取io输出
        [DllImport("MCCE135.dll", EntryPoint = "ecc_io_read_inbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_io_read_inbit(ushort CardNo, ushort SlaveID, ushort bitno, ref ushort on_off);//读取io输入
                                                                                                                     //---------------------------add 20240903-----------------------------------------------------------------------------
                                                                                                                     //耦合器IO
        [DllImport("MCCE135.dll", EntryPoint = "ecc_pure_mode_set", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pure_mode_set(ushort CardNo, ushort on_off);// 纯净模式设置(不使用驱动器锁存、力矩)
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_total_slot", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_total_slot(ushort CardNo, ushort SlaveID, ref ushort TotalInSlot, ref ushort TotalOutSlot);//读取耦合器上DI和DO给自节点总数量
        [DllImport("MCCE135.dll", EntryPoint = "ecc_io_read_inbit_extra", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_io_read_inbit_extra(ushort CardNo, ushort SlaveID, ushort SlotID, ushort BitNo, ref ushort on_off);//读取耦合器上指定节点的输入端口电平
        [DllImport("MCCE135.dll", EntryPoint = "ecc_io_write_outbit_extra", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_io_write_outbit_extra(ushort CardNo, ushort SlaveID, ushort SlotID, ushort BitNo, ushort on_off);//设置耦合器上指定节点输出端口电平
        [DllImport("MCCE135.dll", EntryPoint = "ecc_io_read_outbit_extra", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_io_read_outbit_extra(ushort CardNo, ushort SlaveID, ushort SlotID, ushort BitNo, ref ushort on_off);//读取耦合器上指定节点输出端口电平
        //DA/AD
        [DllImport("MCCE135.dll", EntryPoint = "ecc_set_da_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_da_output(ushort CardNo, ushort channel, short Value);//设置DA参数	
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_da_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_da_output(ushort CardNo, ushort channel, ref short Value);//读取DA参数
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_ad_input", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_ad_input(ushort CardNo, ushort channel, ref short Value);//读取AD参数
        [DllImport("MCCE135.dll", EntryPoint = "ecc_set_ad_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_ad_mode(ushort CardNo, ushort channel, ushort mode);//配置AD模式
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_ad_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_ad_mode(ushort CardNo, ushort channel, ref ushort mode);
        [DllImport("MCCE135.dll", EntryPoint = "ecc_set_da_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_da_mode(ushort CardNo, ushort channel, ushort mode);//配置DA模式
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_da_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_da_mode(ushort CardNo, ushort channel, ref ushort mode);
        //温度控制
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_get_tc_total_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_tc_total_num(ushort CardNo, ref short value);//读取 EtherCAT TC通道数
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_set_tc_offset_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_tc_offset_para(ushort CardNo, ushort channel, short value); //设置温度偏移参数
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_get_tc_offset_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_tc_offset_para(ushort CardNo, ushort channel, ref short value); //读取温度偏移参数
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_get_tc_input", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_tc_input(ushort CardNo, ushort channel, ref short in_value);// 读取温度输入值
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_set_tc_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_tc_mode(ushort CardNo, ushort channel, ushort mode); //配置温度模式
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_get_tc_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_tc_mode(ushort CardNo, ushort channel, ref ushort mode); //配置温度模式
     
        //-------------------------------------------------------------------------------------------------
        //返回错误信息查询
        [DllImport("MCCE135.dll", EntryPoint = "mcc_get_error_description", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_get_error_description(int error_code, byte[] description); //错误码描述读取功能
        //高速轴状态查询
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_axis_check_done", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_status_axis_check_done(ushort CardNo, ushort axis, ref ushort axis_status);//高速轴状态查询
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_axis_check_org", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_status_axis_check_org(ushort CardNo, ushort axis, ref ushort org_status);//高速轴原点状态查询
        //断线重新连接
        //--------------------add 20240924-----------------------------------------------------------------------------------------------------------------------
        [DllImport("MCCE135.dll", EntryPoint = "mcc_set_relinking_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_set_relinking_para(ushort CardNo, ushort relink_en, ushort relink_interval, ushort relink_num, ushort relink_axis_en, ushort relink_jog_axis_num, ushort[] relink_jog_axis_list);//断线重连参数设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_get_relinking_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_get_relinking_para(ushort CardNo, ref ushort relink_en, ref ushort relink_interval, ref ushort relink_num, ref ushort relink_axis_en, ref ushort relink_jog_axis_num, ushort[] relink_jog_axis_list);//断线重连参数获取
        [DllImport("MCCE135.dll", EntryPoint = "mcc_get_relinking_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_get_relinking_status(ushort CardNo, ref ushort relink_status);// 断线重连状态获取
        //二维位置补偿------add 20241112------------------------------------------------------------------------------
        [DllImport("MCCE135.dll", EntryPoint = "mcc_2d_unit_offset_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_2d_unit_offset_set_para(ushort CardNo, ushort axis, ushort[] axisTable, ushort[] count, double[] posBegin, double[] step, double[,] pData);//设置二维补偿参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_2d_unit_offset_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_2d_unit_offset_get_para(ushort CardNo, ushort axis, ushort[] axisTable, ushort[] count, double[] posBegin, double[] step, double[,] pData);//获取二维补偿参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_2d_unit_offset_set_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_2d_unit_offset_set_enable(ushort CardNo, ushort axis, ushort mode, ushort enable);//设置二维补偿使能
        [DllImport("MCCE135.dll", EntryPoint = "mcc_2d_unit_offset_get_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_2d_unit_offset_get_enable(ushort CardNo, ushort axis, ref ushort mode, ref ushort enable);//获取二维补偿使能

        //------------------------20241118----------------------------------------------------------------------------------------------------------
        //龙门安全参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_gantry_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_gantry_set_para(ushort CardNo, ushort SlaveAxis, ushort enable, ushort MasterAxis, ushort FollowRatio);//龙门参数设置；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_gantry_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_gantry_get_para(ushort CardNo, ushort SlaveAxis, ref ushort enable, ref ushort MasterAxis, ref ushort FollowRatio); //龙门参数读取；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_gantry_set_safe_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_gantry_set_safe_para(ushort CardNo, ushort SlaveAxis, ushort enable, double MaxOffset, ushort StopMode);// 龙门安全参数设置；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_gantry_get_safe_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_gantry_get_safe_para(ushort CardNo, ushort SlaveAxis, ref ushort enable, ref double MaxOffset, ref ushort StopMode);// 龙门安全参数读取；                                                                                                                                                         //缓冲区连续插补小线段前瞻====-=========================================================================================================
        //======================20241126=======================================  
        //缓冲区连续插补小线段前瞻
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_set_coordinate_prm_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_set_coordinate_prm_gt(ushort CardNo, ushort Crd, ref TCrdPrm pCrdPrm);//设置坐标系参数，确立坐标系映射，建立坐标系  
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_get_coordinate_prm_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_get_coordinate_prm_gt(ushort CardNo, ushort Crd, ref TCrdPrm pCrdPrm);//查询坐标系参数。
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_add_data", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_add_data_gt(ushort CardNo, ushort Crd);//向插补缓存区增加插补数据。 用于在使用前瞻时，调用该指令表示后续没有新的数据，将会一次性把前瞻缓存区的数据压入运动缓存区。
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_line_move_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_line_move_gt(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, short posi_mode, double synVel, double synAcc, double velEnd, double velStart);//直线插补
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_arcn_center_move_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_arcn_center_move_gt(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, double[] cen_pos, ushort circleDir, ushort circle_num, short posi_mode, double synVel, double synAcc, double velEnd, double velStart);//圆心插补
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_arcn_radius_move_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_arcn_radius_move_gt(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, double radius, ushort circleDir, ushort circle_num, short posi_mode, double synVel, double synAcc, double velEnd, double velStart);//半径圆弧插补
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_arcn_3point_move_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_arcn_3point_move_gt(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, double[] mid_pos, ushort circle_num, short posi_mode, double synVel, double synAcc, double velEnd, double velStart);//三点圆弧插补
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_io_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_io_gt(ushort CardNo, ushort Crd, ushort doType, ushort doMask, ushort doValue);//缓存区内数字量 IO输出设置指令。
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_delay_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_delay_gt(ushort CardNo, ushort Crd, ushort delayTime);//缓存区指令，缓存区内延时设置指令
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_limit_on_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_limit_on_gt(ushort CardNo, ushort Crd, ushort axis, short limitType);//设置缓存区内有效限位开关
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_limit_off_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_limit_off_gt(ushort CardNo, ushort Crd, ushort axis, short limitType);//缓存区内无效限位开关
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_set_stop_io_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_set_stop_io_gt(ushort CardNo, ushort Crd, ushort axis, short stopType, short inputType, short inputIndex);//缓存区内设置 axis 的停止IO信息
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_move_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_move_gt(ushort CardNo, ushort Crd, ushort moveAxis, double pos, double vel, double acc_time);//实现刀向跟随功能，启动某个轴点位运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_segment_remain_space_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_segment_remain_space_gt(ushort CardNo, ushort Crd, ref int pSpace);//查询插补缓存区剩余空间
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_data_clear_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_data_clear_gt(ushort CardNo, ushort Crd);//清除插补缓存区内的插补数据(只清除缓冲区数据，不关闭坐标系)
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_motion_start_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_motion_start_gt(ushort CardNo, ushort Crd);//启动缓冲区插补运动 
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_motion_pause_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_motion_pause_gt(ushort CardNo, ushort Crd);//暂停缓冲区插补运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_motion_stop_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_motion_stop_gt(ushort CardNo, ushort Crd, ushort mode);//停止缓冲区插补运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_running_status_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_running_status_gt(ushort CardNo, ushort Crd, ref short pRun, ref int pSegment);//查询插补运动坐标系状态
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_set_user_segment_num_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_set_user_segment_num_gt(ushort CardNo, ushort Crd, int segNum);//设置自定义插补段段号
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_get_user_segment_num_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_get_user_segment_num_gt(ushort CardNo, ushort Crd, ref int segNum);//读取自定义插补段段号
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_get_remain_data_segment_num_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_get_remain_data_segment_num_gt(ushort CardNo, ushort Crd, ref int pSegment);//读取未完成的插补段段数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_set_override_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_set_override_gt(ushort CardNo, ushort Crd, double synVelRatio);//设置插补运动目标合成速度倍率
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_set_interp_stop_dec_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_set_interp_stop_dec_gt(ushort CardNo, ushort Crd, double decSmoothStop, double decAbruptStop);//设置插补运动平滑停止、急停合成加速度
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_get_interp_stop_dec_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_get_interp_stop_dec_gt(ushort CardNo, ushort Crd, ref double decSmoothStop, ref double decAbruptStop);//查询插补运动平滑停止、急停合成加速度
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_get_coordinate_pos_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_get_coordinate_pos_gt(ushort CardNo, ushort Crd, double[] pPos);//查询该坐标系的当前坐标位置值
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_get_coordinate_vel_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_get_coordinate_vel_gt(ushort CardNo, ushort Crd, ref double pSynVel);//查询该坐标系的当前坐标速度值
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_set_lookahead_para_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_set_lookahead_para_gt(ushort CardNo, ushort Crd, double T, double accMax, uint enable);//初始化插补前瞻缓存区
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_get_lookahead_para_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_get_lookahead_para_gt(ushort CardNo, ushort Crd, ref double T, ref double accMax, ref uint enable);//查询插补前瞻缓存区
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_set_vel_smooth_factor_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_set_vel_smooth_factor_gt(ushort CardNo, ushort Crd, double Smooth);//设置插补速度曲线平滑系数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_get_vel_smooth_factor_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_get_vel_smooth_factor_gt(ushort CardNo, ushort Crd, ref double Smooth);//查询插补速度曲线平滑系数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_coordinate_reset_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_coordinate_reset_gt(ushort CardNo, ushort Crd);   //关闭坐标系（运动停止，清除缓冲区数据，再关闭坐标系）
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_wait_di_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_wait_di_gt(ushort CardNo, ushort Crd, ushort di_id, ushort di_logic, int time_out);//缓存区等待通用输入IO指令。
        //连续插补IO控制
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_set_pause_output_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_set_pause_output_gt(ushort CardNo, ushort Crd, ushort action, int mask, int state);//设置连续插补暂停及异常停止时 IO 输出状态
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_get_pause_output_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_get_pause_output_gt(ushort CardNo, ushort Crd, ref ushort action, ref int mask, ref int state);//读取连续插补暂停及异常停止时 IO 输出状态设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_delay_outbit_to_start_pos_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_delay_outbit_to_start_pos_gt(ushort CardNo, ushort Crd, ushort bitno, ushort on_off, double delay_value, ushort delay_mode, double ReverseTime);  //连续插补中相对于轨迹段起点IO滞后输出(段内执行)
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_delay_outbit_to_stop_pos_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_delay_outbit_to_stop_pos_gt(ushort CardNo, ushort Crd, ushort bitno, ushort on_off, double delay_time, double ReverseTime);//连续插补中相对于轨迹段终点IO滞后输出
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_ahead_outbit_to_stop_pos_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_ahead_outbit_to_stop_pos_gt(ushort CardNo, ushort Crd, ushort bitno, ushort on_off, double ahead_value, ushort ahead_mode, double ReverseTime);//连续插补中现对于轨迹段终点IO提前输出；
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_clear_io_action_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_clear_io_action_gt(ushort CardNo, ushort Crd, uint Io_Mask);//清除段内未执行完的 IO 动作 
        //------------------------20241115-----------------------------------------------------------------------------------------------
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_write_block_outbit_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_write_block_outbit_gt(ushort CardNo, ushort Crd, ushort biton, ushort on_off, double ReverseTime);//缓存区阻塞式单个Do操作指令
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_write_nblock_outbit_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_write_nblock_outbit_gt(ushort CardNo, ushort Crd, ushort biton, ushort on_off, double ReverseTime);// 缓存区非阻塞式单个Do操作指令
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_write_nblock_outbyte_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_write_nblock_outbyte_gt(ushort CardNo, ushort Crd, ushort IO_num, ushort[] ByteNo, ushort[] ByteValue, double[] ReverseTime);//缓存区非阻塞式多个Do操作指令
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_move_full_pause_gt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_move_full_pause_gt(ushort CardNo, ushort Crd);
        //-------------------- add 20250120-----------------------------------------------------------------------------
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ecam_set_table", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ecam_set_table(ushort CardNo, ushort TableId, ushort NodeNum, ushort[] DataSegmentTypeList, double[] MasterPosList, double[] SlavePosList, double[] SlaveVelList, double[] SlaveAccList);//设置电子凸轮比表
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ecam_in", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ecam_in(ushort CardNo, ushort MasterAxisNo, ushort SlaveAxisNo, ushort TableId, ushort Perodic, ushort StartMode, double MasterStartPos, double MasterSynPos);//启动电子凸轮运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ecam_out", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ecam_out(ushort CardNo, ushort SlaveAxisNo, ushort StopMode);//解除电子凸轮运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ecam_set_aux_param", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ecam_set_aux_param(ushort CardNo, ushort SlaveAxisNo, ushort reftype, ushort dirmode, double masterscale, double slavescale, double masteroffset, double slaveoffset, ushort buffermode);//设置凸轮运动的其他辅助参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_ecam_get_in_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_ecam_get_in_status(ushort CardNo, ushort SlaveAxisNo, ref ushort Status);//获取凸轮运动从轴状态  
        [DllImport("MCCE135.dll", EntryPoint = "mcc_set_phase_shift", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_set_phase_shift(ushort CardNo, ushort SlaveAxisNo, ushort Enable, double ShiftPhase, double max_vel, double acc_time, double dec_time, ushort mode);//设置相位偏移量
                                                                                                                                                                                           //--------------------------------------------------------------------------------------------------------
                                                                                                                                                                                           //设置伺服轴类型
        [DllImport("MCCE135.dll", EntryPoint = "mcc_set_servo_axis_type", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_set_servo_axis_type(ushort CardNo, ushort AxisNo, ushort AxisType, double RotationPeriod);//设置伺服轴类型（默认为线性轴）
        [DllImport("MCCE135.dll", EntryPoint = "mcc_get_servo_axis_type", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_get_servo_axis_type(ushort CardNo, ushort AxisNo, ref ushort AxisType, ref double RotationPeriod);//获取伺服轴类型（默认为线性轴）
        //-----------------add 20250312-------------------------------------------------------------------------------------------------------
        //断线自动重连
        [DllImport("MCCE135.dll", EntryPoint = "ecc_relink_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_relink_set_para(ushort CardNo, ushort enable, ushort time, ushort delta);//断线自动重连参数设置；
        [DllImport("MCCE135.dll", EntryPoint = "ecc_relink_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_relink_get_para(ushort CardNo, ref ushort enable, ref ushort time, ref ushort delta);//断线自动重连参数读取；
        [DllImport("MCCE135.dll", EntryPoint = "ecc_relink_set_axis_enable_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_relink_set_axis_enable_state(ushort CardNo, ushort num, ushort[] axis_list);//断线自动重连后，轴保持使能状态设置；
        [DllImport("MCCE135.dll", EntryPoint = "ecc_relink_get_axis_enable_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_relink_get_axis_enable_state(ushort CardNo, ref ushort num, ushort[] axis_list);//断线自动重连后，轴保持使能状态读取；
        [DllImport("MCCE135.dll", EntryPoint = "ecc_relink_set_output_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_relink_set_output_state(ushort CardNo, ushort num, ushort[] do_list);//断线自动重连后，Do口保持输出设置；
        [DllImport("MCCE135.dll", EntryPoint = "ecc_relink_get_output_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_relink_get_output_state(ushort CardNo, ref ushort num, ushort[] do_list);//断线自动重连后，Do口保持输出设置；
        //-----------------add 20250328--------------------------------------------------------------------------------------------------------------------
        //轴扭矩参数
        [DllImport("MCCE135.dll", EntryPoint = "ecc_torque_set_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_torque_set_para(ushort CardNo, ushort axis, short troque_val, double acc_time);//设置轴扭矩参数
        [DllImport("MCCE135.dll", EntryPoint = "ecc_torque_get_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_torque_get_para(ushort CardNo, ushort axis, ref short troque_val, ref double acc_time);//获取轴扭矩参数
        [DllImport("MCCE135.dll", EntryPoint = "ecc_torque_set_max_limit_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_torque_set_max_limit_vel(ushort CardNo, ushort axis, uint max_troque, ushort posit_max_troque, ushort negat_max_troque);//设置最大扭矩限速
        [DllImport("MCCE135.dll", EntryPoint = "ecc_torque_get_max_limit_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_torque_get_max_limit_vel(ushort CardNo, ushort axis, ref uint max_troque, ref ushort posit_max_troque, ref ushort negat_max_troque);//获取最大扭矩限速
        [DllImport("MCCE135.dll", EntryPoint = "ecc_torque_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_torque_start(ushort CardNo, ushort axis);//启动扭矩运动
        [DllImport("MCCE135.dll", EntryPoint = "ecc_torque_change_para_online", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_torque_change_para_online(ushort CardNo, ushort axis, short update_torque);//在线修改转矩值
        [DllImport("MCCE135.dll", EntryPoint = "ecc_torque_get_aim_val", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_torque_get_aim_val(ushort CardNo, ushort axis, ref short aim_torque);//获取目标扭矩
        //------------------add 20250401------------------------------------------------------------------------------------------
        //CSV 规划速度
        [DllImport("MCCE135.dll", EntryPoint = "ecc_csv_prf_vel_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_csv_prf_vel_start(ushort CardNo, ushort axis, Int32 target_vel, Int32 acc, ushort prf_type);//启动CSV速度规划
        [DllImport("MCCE135.dll", EntryPoint = "ecc_csv_prf_vel_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_csv_prf_vel_stop(ushort CardNo, ushort axis, Int32 dec);//停止CSV速度规划
        [DllImport("MCCE135.dll", EntryPoint = "ecc_csv_prf_vel_emg_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_csv_prf_vel_emg_stop(ushort CardNo, ushort axis);//急停CSV速度规划
        [DllImport("MCCE135.dll", EntryPoint = "ecc_csv_prf_vel_update", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_csv_prf_vel_update(ushort CardNo, ushort axis, Int32 target_vel);//更新CSV速度规划的
        [DllImport("MCCE135.dll", EntryPoint = "ecc_csv_prf_vel_get_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_csv_prf_vel_get_status(ushort CardNo, ushort axis, ref ushort prf_status);//获取CSV速度规划的状态

        //----------------------add 20250408--------------------------------------------------------------------------------
        //读取指定所有轴的参数;
        [DllImport("MCCE135.dll", EntryPoint = "mcc_status_get_all_axis_para_pack", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_status_get_all_axis_para_pack(ushort CardNo, ushort axis_num, ushort[] axis_list, ref TaxisParaPackGet pTaxisParPack); //抓取轴参数数据包
        //----------------------add 20250408--------------------------------------------------------------------------
        [DllImport("MCCE135.dll", EntryPoint = "mcc_set_coupler_file", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_set_coupler_file(ushort CardNo, uint slave, uint eep_man, uint eep_id, uint slot_index, uint slave_num, uint iolink_num, uint[] iolink_index_list, uint out_freebit, uint in_freebit, int link_flag); // 下载耦合器链路信息
                                                                                                                                                                                                                                             //----------------add 20250527------------------------------------------------------------------------------
        [DllImport("MCCE135.dll", EntryPoint = "mcc_headwheel_set_limit_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_headwheel_set_limit_para(ushort CardNo, ushort ChnNo, ushort limit_flag, double max_vel, double max_acc);//手轮限速、限加速度设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_headwheel_get_limit_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_headwheel_get_limit_para(ushort CardNo, ushort ChnNo, ref ushort limit_flag, ref double max_vel, ref double max_acc);//手轮限速、限加速度读取
        [DllImport("MCCE135.dll", EntryPoint = "mcc_headwheel_set_ratio_io_convert", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_headwheel_set_ratio_io_convert(ushort CardNo, ushort ChnNo, ushort enable, ushort axis_num, ushort ratio_num, ushort inmode, ushort[] axis_list, ushort[] axis_io_list, ushort[] ratio_list);//手轮倍率IO映射设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_headwheel_get_ratio_io_convert", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_headwheel_get_ratio_io_convert(ushort CardNo, ushort ChnNo, ref ushort enable, ref ushort axis_num, ref ushort ratio_num, ref ushort inmode, ushort[] axis_list, ushort[] axis_io_list, ushort[] ratio_list);//手轮倍率IO映射读取
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_set_interp_limit_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_set_interp_limit_vel(ushort CardNo, ushort Crd, ushort axis, ushort enable, double synVelMax, double synAccMax);//设置插补系轴速度,加速度限制
        [DllImport("MCCE135.dll", EntryPoint = "mcc_crd_buf_get_interp_limit_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_crd_buf_get_interp_limit_vel(ushort CardNo, ushort Crd, ushort axis, ref ushort enable, ref double synVelMax, ref double synAccMax);//读取插补系轴速度,加速度限制
                                                                                                                                                                           //----------------------add 20250604--------------------------------------------------------------------------------------------------------
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_open_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_open_list(ushort CardNo, ushort group, ushort axis_num, ushort[] axis_list);//打开缓存区
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_start_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_start_list(ushort CardNo, ushort group);//启动缓冲存区运动指令
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_close_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_close_list(ushort CardNo, ushort group);//关闭缓存区
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_stop_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_stop_list(ushort CardNo, ushort group, ushort stop_mode);//停止缓存区运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_pause_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_pause_list(ushort CardNo, ushort group, ushort stop_mode);//暂停缓存区运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_set_axis_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_set_axis_profile(ushort CardNo, ushort group, ushort axis, double start_vel, double max_vel, double tacc, double tdec, double smooth_time, double stop_vel, uint mark);//设置缓存区单轴运动速度参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_set_muti_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_set_muti_profile(ushort CardNo, ushort group, ushort axis_num, ushort[] axis_list, double[] start_vel, double[] max_vel, double[] tacc, double[] tdec, double[] stop_vel, double[] smooth_time, uint mark);//设置缓存区多轴速度模式
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_add_sigaxis_pmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_add_sigaxis_pmove(ushort CardNo, ushort group, ushort axis, double target_pos, ushort posi_mode, uint mark);//缓存区单轴运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_add_mutiaxis_pmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_add_mutiaxis_pmove(ushort CardNo, ushort group, ushort axis_num, ushort[] axis_list, double[] target_pos, ushort posi_mode, uint mark);//缓存区同时添加多组单轴运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_add_wait_event_data", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_add_wait_event_data(ushort CardNo, ushort group, ushort wait_events, ushort num, ushort compare_operator, double target_value, uint mark);//缓存区等待事件配置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_add_trigger_data", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_add_trigger_data(ushort CardNo, ushort group, ushort mode, ushort num, double target_value, uint mark);//缓存区添加触发动作
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_add_time_delay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_add_time_delay(ushort CardNo, ushort group, double time_delay, uint mark);//缓存区添加延时配置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_set_wait_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_set_wait_flag(ushort CardNo, ushort group, ushort flag_no, ushort wait_flag_sts);//设置指令缓存区运动标志位状态
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_get_wait_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_get_wait_flag(ushort CardNo, ushort group, ushort flag_no, ref ushort wait_flag_sts);//读取指令缓存区运动标志位状态
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_get_run_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_get_run_state(ushort CardNo, ushort group, ref ushort state, ref ushort enable, ref uint stop_reason, ref ushort trig_phase, ref uint mark);//读取缓存区运动状态
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_get_remain_space", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_get_remain_space(ushort CardNo, ushort group, ref uint data);//读取缓存区剩余空间
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_clear_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_clear_list(ushort CardNo, ushort group); //清除缓存区数据
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_set_buf_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_set_buf_mode(ushort CardNo, ushort group, ushort mode);//指令缓存区模式设置 0-动态 1-静态
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_get_buf_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_get_buf_mode(ushort CardNo, ushort group, ref ushort mode);//指令缓存区模式获取0-动态 1-静态
   	    //指令缓存直线插补
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_set_line_prf_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_set_line_prf_vel(ushort CardNo, ushort Crd, ushort group, double min_vel, double max_vel, double stop_vel, double tacc, double tdec, double s_para, uint mark);//设置缓冲区插补速度参数
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_line_motion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_line_motion(ushort CardNo, ushort Crd,ushort group, ushort axis_num, ushort[] axis_list, double[] aim_pos_list, ushort posi_mode, uint mark); //缓冲区插补指令     
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_set_buf_choke_stage", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_set_buf_choke_stage(ushort CardNo, ushort group_id, ushort checked_group_id, ushort stage_num, uint mark);//缓冲区段数阻塞
        [DllImport("MCCE135.dll", EntryPoint = "mcc_m_set_control_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_m_set_control_flag(ushort CardNo, ushort group, ushort flag_no, ushort flag_flag_sts, uint mark);
        //-------------------------add 20250630---------------------------------------------------------------------------------------------------------
        [DllImport("MCCE135.dll", EntryPoint = "mcc_write_data", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_write_data(ushort CardNo, ushort data_len, byte[] data_list);//写内存
        [DllImport("MCCE135.dll", EntryPoint = "mcc_read_data", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_read_data(ushort CardNo, ushort data_len, byte[] data_list);//读内存
	    //-------------------------add 20250827---------------------------------------------------------------------------------
        //机器人输入输出控制
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_robot_sow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_robot_sow(ushort CardNo, ushort sow_no, ref ushort value);//读取机器人输出   //对卡来说  是IN 信号
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_set_robot_siw", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_robot_siw(ushort CardNo, ushort siw_no, ushort value);//设置机器人输入    //对卡来说  是OUT 信号
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_robot_siw", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_robot_siw(ushort CardNo, ushort siw_no, ref ushort value); //读取机器人输入 双字节     //对卡来说  是OUT 信号
        [DllImport("MCCE135.dll", EntryPoint = "ecc_set_robot_siw_bit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_robot_siw_bit(ushort CardNo, ushort siw_bit, ushort value);//设置机器人输入 位      //对卡来说  是OUT 信号
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_robot_siw_bit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_robot_siw_bit(ushort CardNo, ushort siw_bit, ref ushort value);//读取机器人输入 位     //对卡来说  是OUT 信号
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_robot_sow_bit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_robot_sow_bit(ushort CardNo, ushort sow_bit, ref ushort value);//读取机器人输出 位     //对卡来说  是IN 信号
        [DllImport("MCCE135.dll", EntryPoint = "ecc_get_robot_rebit_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_robot_rebit_num(ushort CardNo, ref ushort tcnum); //读取 EtherCAT 机器人通道数
        //蛙跳运动
        [DllImport("MCCE135.dll", EntryPoint = "mcc_w_set_up_parameter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_w_set_up_parameter(ushort CardNo, ushort Crd, ushort axisNum, ushort[] axisList, double[] middlePosList, double[] targetPosList, ushort posi_mode);//蛙跳上升直线插补参数设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_w_set_shift_parameter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_w_set_shift_parameter(ushort CardNo, ushort Crd, ushort axisNum, ushort[] axisList, double[] middlePosList, double[] targetPosList, ushort posi_mode);//蛙跳横移直线插补参数设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_w_set_down_parameter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_w_set_down_parameter(ushort CardNo, ushort Crd, ushort axisNum, ushort[] axisList, double[] middlePosList, double[] targetPosList, ushort posi_mode);//蛙跳下降直线插补参数设置
        [DllImport("MCCE135.dll", EntryPoint = "mcc_w_line_motion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_w_line_motion(ushort CardNo, ushort Crd, uint delayTime); //蛙跳开始运动 将3段直线插补压入缓冲
        [DllImport("MCCE135.dll", EntryPoint = "mcc_w_get_run_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_w_get_run_time(ushort CardNo, ushort Crd, ref uint delayTime, ref uint wStatus); //蛙跳计算时间返回，蛙跳运动状态返回  
        [DllImport("MCCE135.dll", EntryPoint = "mcc_w_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_w_stop(ushort CardNo, ushort Crd, ushort mode);//蛙跳停止
        [DllImport("MCCE135.dll", EntryPoint = "mcc_w_get_status_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short mcc_w_get_status_time(ushort CardNo, ushort Crd, ref uint delayTime, ref uint wStatus, ref uint upAimTime, ref uint upMiddleTime, ref uint shiftAimTime, ref uint shiftMiddleTime, ref uint downAimTime, ref uint downMiddleTime); //蛙跳计算时间返回，蛙跳运动状态返回(新)
    	//----------------20251124------------------------------------------------------------------------------------------
	    //脉冲模块
        [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_get_channel_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_get_channel_num(ushort CardNo, ref ushort channel_num);
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_set_encoder_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_set_encoder_para(ushort CardNo,ushort channel,ushort enable,ushort enc_rev,ushort enc_z_clear,ushort enc_mode,ushort enc_filt_grade );//脉冲模块编码器参数设置
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_get_encoder_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_get_encoder_para(ushort CardNo, ushort channel, ref ushort enable, ref ushort enc_rev, ref ushort enc_z_clear, ref ushort enc_mode, ref ushort enc_filt_grade);//脉冲模块编码器参数获取
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_set_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_set_value(ushort CardNo, ushort channel, int value);//脉冲模块编码器值设置
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_get_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_get_value(ushort CardNo, ushort channel, ref int value);//脉冲模块编码器值获取
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_set_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_set_mode(ushort CardNo, ushort channel, ushort mode);//脉冲模块脉冲输出模式设置
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_get_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_get_mode(ushort CardNo, ushort channel, ref ushort mode);//脉冲模块脉冲输出模式获取
        [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_set_orglmt_active", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_set_orglmt_active(ushort CardNo, ushort channel, ushort org_actv, ushort el_actv); //脉冲模块原点限位有效电平设置
        [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_get_orglmt_active", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_get_orglmt_active(ushort CardNo, ushort channel, ref ushort org_actv, ref ushort el_actv); //脉冲模块原点限位有效电平获取
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_motion_emg_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_motion_emg_stop(ushort CardNo, ushort channel); //脉冲模块运动急停
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_motion_dec_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_motion_dec_stop(ushort CardNo, ushort channel); //脉冲模块运动减速停止
        [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_set_axis_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_set_axis_enable(ushort CardNo, ushort channel, ushort svon_en); //脉冲模块伺服使能设置
        [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_get_axis_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_get_axis_enable(ushort CardNo, ushort channel, ref ushort svon_en); //脉冲模块伺服使能读取
        [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_set_erc_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_set_erc_pin(ushort CardNo, ushort channel,ushort on_off); //脉冲模块伺服报警清除
        [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_get_erc_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_get_erc_pin(ushort CardNo, ushort channel, ref ushort on_off); //读取脉冲模块伺服报警清除	       
        [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_set_prf_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_set_prf_vel(ushort CardNo, ushort channel, ushort vel_mode, double start_vel, double max_vel, double acc_time, double dec_time); //脉冲模块运动参数设置
        [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_get_prf_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_get_prf_vel(ushort CardNo, ushort channel, ref ushort vel_mode, ref double start_vel, ref double max_vel, ref double acc_time, ref double dec_time); //脉冲模块运动参数读取
        [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_pmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_pmove(ushort CardNo, ushort channel, double pos, ushort posi_mode);//脉冲模块位置模式运动
        [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_vmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_vmove(ushort CardNo, ushort channel,ushort dir);//脉冲模块速度模式运动
        [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_line_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_line_move(ushort CardNo, ushort channel, double[] pos, ushort posi_mode);//脉冲模块插补模式运动
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_set_home_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_set_home_para(ushort CardNo, ushort channel,uint home_mode, double home_vel,double low_speed);//脉冲模块回零参数设置
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_get_home_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_get_home_para(ushort CardNo, ushort channel, ref uint home_mode,ref double home_vel,ref double low_speed);//脉冲模块回零参数读取
        [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_home_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_home_move(ushort CardNo, ushort channel);//脉冲模块回零模式运动
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_get_io_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_get_io_status(ushort CardNo, ushort channel,ref uint p_el_status,ref uint n_el_status,ref uint org_status,ref uint alm_status,ref uint home_status,ref uint point_status,ref uint jog_status ); //脉冲模块io状态获取
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_get_status_error", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_get_status_error(ushort CardNo, ushort channel, ref ushort err_code); //获取脉冲模块轴状态错误码
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_get_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_get_speed(ushort CardNo, ushort channel, ref double rcur_speed);//脉冲模块轴指令速度获取
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_get_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_get_pos(ushort CardNo, ushort channel, ref double pos);//脉冲模块轴指令位置获取
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_get_encoder_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_get_encoder_speed(ushort CardNo, ushort channel, ref double enc_speed);//脉冲模块轴编码器速度获取
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_pulse_module_get_encoder_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_pulse_module_get_encoder_pos(ushort CardNo, ushort channel, ref double enc_pos);//脉冲模块轴编码器位置获取

	    //----------------20260304------------------------------------------------------------------------------------------
	    //总线周期脉冲滤波参数设置
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_set_axis_pulse_cycle_filter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_set_axis_pulse_cycle_filter(ushort CardNo,ushort axis,double max_vel_filter);//设置总线周期脉冲滤波参数
	    [DllImport("MCCE135.dll", EntryPoint = "ecc_get_axis_pulse_cycle_filter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short ecc_get_axis_pulse_cycle_filter(ushort CardNo, ushort axis, ref uint cycle_num,ref double max_vel_filter);//获取总线周期脉冲滤波参数

    }

}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
