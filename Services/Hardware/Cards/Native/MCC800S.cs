﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/DLL/MCC800S.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;      

namespace csMCC800S
{
   public class MCC800S
    {
      
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TCrdPrm                   //设置坐标系的相关参数
        {
            public UInt16 dimension;            //坐标系的维数。取值范围：[1, 8]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U2)]
            public UInt16[] profile;//8         //坐标系【x,y,z,u,v,w,c,c1】与物理轴0~7的映射关系。
            public double synVelMax;           //该坐标系的最大合成速度。如果用户在输入插补段的时候所设置的目标速度大于了该速度，则将会被限制为该速度。单位：脉冲当量/s。
            public double synAccMax;           //该坐标系的最大合成加速度。如果用户在输入插补段的时候所设置的加速度大于了该加速度，则将会被限制为该加速度。单位：脉冲当量/s2。
            public Int16 evenTime;             //保留值 
            public Int16 setOriginFlag;        //保留值 
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.I4)]
            public Int32[] originPos;  //8     //保留值 
        }
        //========================================================================================================================================================================
        //板卡配置
        [DllImport("MCC800S.dll", EntryPoint = "YK_board_init", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_board_init();
        //关闭控制卡
        [DllImport("MCC800S.dll", EntryPoint = "YK_board_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_board_close();
        //硬件复位
        [DllImport("MCC800S.dll", EntryPoint = "YK_board_reset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_board_reset();
        //读取初始化完成后的获取所有卡信息列表
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_CardInfList", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_CardInfList(ref ushort CardNum, UInt32[] CardTypeList, ushort[] CardIdList);
        //读取控制卡硬件版本
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_card_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_card_version(ushort CardNo, ref UInt32 CardVersion);
        //读取控制卡硬件的固件版本
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_card_soft_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_card_soft_version(ushort CardNo, ref UInt32 FirmID, ref UInt32 SubFirmID);
        //读取控制卡动态库版本
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_card_lib_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_card_lib_version(ref UInt32 LibVer);
        //读取控制卡动态库版本信息
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_hard_ware_info", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_hard_ware_info(ushort CardNo,  byte[] hardWareInfo);
        //读取指定卡轴数
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_total_axes", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_total_axes(ushort CardNo, ref UInt32 TotalAxis);

        //下载参数文件 ===================================================================================
        [DllImport("MCC800S.dll", EntryPoint = "YK_download_configfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_download_configfile(ushort CardNo, String FileName);
        //下载固件文件
        [DllImport("MCC800S.dll", EntryPoint = "YK_download_firmware", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_download_firmware(ushort CardNo, String FileName);

        //=======================================================================================================================================================================
        //轴IO映射配置
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_AxisIoMap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_AxisIoMap(ushort CardNo, ushort Axis, ushort IoType, ushort MapIoType, ushort MapIoIndex, UInt32 Filter);
        //读取轴IO映射关系设置
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_AxisIoMap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_AxisIoMap(ushort CardNo, ushort Axis, ushort IoType, ref ushort MapIoType, ref ushort MapIoIndex, ref UInt32 Filter);
        //以上函数以毫秒为单位可继续使用，新函数将时间统一到秒为单位
        //设置轴IO映射关系
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_axis_io_map", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_axis_io_map(ushort CardNo, ushort Axis, ushort IoType, ushort MapIoType, ushort MapIoIndex, double Filter);
        //读取轴IO映射关系设置
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_axis_io_map", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_axis_io_map(ushort CardNo, ushort Axis, ushort IoType, ref ushort MapIoType, ref ushort MapIoIndex, ref double Filter);
        //统一设置所有专用IO的滤波时间
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_special_input_filter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_special_input_filter(ushort CardNo, double Filter);
        //虚拟IO映射  用于读取滤波后的IO口电平状态
        //设置虚拟IO映射关系
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_io_map_virtual", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_io_map_virtual(ushort CardNo, ushort bitno, ushort MapIoType, ushort MapIoIndex, double Filter);
        //读取虚拟IO映射关系设置
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_io_map_virtual", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_io_map_virtual(ushort CardNo, ushort bitno, ref ushort MapIoType, ref ushort MapIoIndex, ref double Filter);
        //读取滤波后的虚拟 IO 口电平状态
        [DllImport("MCC800S.dll", EntryPoint = "YK_read_inbit_virtual", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_inbit_virtual(ushort CardNo, ushort bitno);
        
        //限位/异常配置 ==================================================================================================================================================
        //设置硬件限位EL信号
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_el_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_el_mode(ushort CardNo, ushort axis, ushort el_enable, ushort el_logic, ushort el_mode);
        //读取硬件限位EL信号设置
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_el_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_el_mode(ushort CardNo, ushort axis, ref ushort el_enable, ref ushort el_logic, ref ushort el_mode);
        //设置软限位参数
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_softlimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_softlimit(ushort CardNo, ushort axis, ushort enable, ushort source_sel, ushort SL_action, double N_limit, double P_limit);
        //读取软限位参数
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_softlimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_softlimit(ushort CardNo, ushort axis, ref ushort enable, ref ushort source_sel, ref ushort SL_action, ref double N_limit, ref double P_limit);
        //设置EMG信号
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_emg_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_emg_mode(ushort CardNo, ushort axis, ushort enable, ushort emg_logic);
        //读取设置EMG信号
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_emg_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_emg_mode(ushort CardNo, ushort axis, ref ushort enbale, ref ushort emg_logic);

        //外部减速停止信号及减速停止时间设置==================================================================================================================================================
        //设置减速停止信号
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_dstp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_dstp_mode(ushort CardNo, ushort axis, ushort enable, ushort logic, UInt32 time);
        //读取减速停止信号设置	
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_dstp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_dstp_mode(ushort CardNo, ushort axis, ref ushort enable, ref ushort logic, ref UInt32 time);
        //设置减速停止时间
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_dstp_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_dstp_time(ushort CardNo, ushort axis, UInt32 time);
        //读取减速停止时间设置
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_dstp_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_dstp_time(ushort CardNo, ushort axis, ref UInt32 time);
        //以上函数以毫秒为单位可继续使用，新函数将时间统一到秒为单位
        //设置外部IO触发减速停止模式
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_io_dstp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_io_dstp_mode(ushort CardNo, ushort axis, ushort enable, ushort logic);
        //读取减速停止信号设置
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_io_dstp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_io_dstp_mode(ushort CardNo, ushort axis, ref ushort enable, ref ushort logic);
        //设置全局减速停止时间
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_dec_stop_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_dec_stop_time(ushort CardNo, ushort axis, double stop_time);
        //读取减速停止时间设置
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_dec_stop_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_dec_stop_time(ushort CardNo, ushort axis, ref double stop_time);

        //速度设置==============================================================================================================	
        //设定速度曲线参数
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_profile(ushort CardNo, ushort axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double stop_vel);
        //读取速度曲线参数
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_profile(ushort CardNo, ushort axis, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double stop_vel);
        //设置平滑速度曲线参数
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_s_profile(ushort CardNo, ushort axis, ushort s_mode, double s_para);
        //读取平滑速度曲线参数
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_s_profile(ushort CardNo, ushort axis, ushort s_mode, ref double s_para);

        //运动模块脉冲模式===================================================================================================================
        //设定脉冲输出模式
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_pulse_outmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_pulse_outmode(ushort CardNo, ushort axis, ushort outmode);
        //读取脉冲输出模式
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_pulse_outmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_pulse_outmode(ushort CardNo, ushort axis, ref ushort outmode);
        //点位运动
        [DllImport("MCC800S.dll", EntryPoint = "YK_pmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_pmove(ushort CardNo, ushort axis, double dist, ushort posi_mode);
        //JOG(连续)运动
        [DllImport("MCC800S.dll", EntryPoint = "YK_vmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_vmove(ushort CardNo, ushort axis, ushort dir);
        //在线变位/变速===========================================================================================================================================
        //运动中改变目标位置
        [DllImport("MCC800S.dll", EntryPoint = "YK_reset_target_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_reset_target_position(ushort CardNo, ushort axis, double dist, ushort posi_mode);
        //在线改变指定轴的当前运动速度及加减速时间
        [DllImport("MCC800S.dll", EntryPoint = "YK_change_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_change_speed(ushort CardNo, ushort axis, double curr_vel, double acc_dec_t);
        //强行改变目标位置
        [DllImport("MCC800S.dll", EntryPoint = "YK_update_target_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_update_target_position(ushort CardNo, ushort axis, double dist, ushort posi_mode);

        //回零运动============================================================================================================================================
        //设置HOME信号
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_home_pin_logic", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_home_pin_logic(ushort CardNo, ushort axis, ushort org_logic, double filter);
        //读取设置HOME信号
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_home_pin_logic", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_home_pin_logic(ushort CardNo, ushort axis, ref ushort org_logic, ref double filter);
        //设定指定轴的回原点模式
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_homemode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_homemode(ushort CardNo, ushort axis, ushort home_dir, double vel_mode, ushort mode, ushort EZ_count);
        //读取指定轴的回原点模式
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_homemode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_homemode(ushort CardNo, ushort axis, ref ushort home_dir, ref double vel_move, ref ushort home_mode, ref ushort EZ_count);
        //回零运动启动
        [DllImport("MCC800S.dll", EntryPoint = "YK_home_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_home_move(ushort CardNo, ushort axis); 
        //原点锁存-----------------------------------------------------------------------------------------------------------
        //设置原点锁存模式
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_homelatch_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_homelatch_mode(ushort CardNo, ushort axis, ushort enable, ushort logic, ushort source);
        //读取原点锁存模式
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_homelatch_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_homelatch_mode(ushort CardNo, ushort axis, ref ushort enable, ref ushort logic, ref ushort source);
        //读取锁存标志  
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_homelatch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_get_homelatch_flag(ushort CardNo, ushort axis);
        //复位原点锁存标志 
        [DllImport("MCC800S.dll", EntryPoint = "YK_reset_homelatch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_reset_homelatch_flag(ushort CardNo, ushort axis);
        //读取锁存值
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_homelatch_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_get_homelatch_value(ushort CardNo, ushort axis);
        //=============================================================================================================================================================
        //手轮运动	
        //手轮通道选择	     
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_handwheel_channel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_handwheel_channel(ushort CardNo, ushort index);
        //手轮通道读取
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_handwheel_channel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_handwheel_channel(ushort CardNo, ref ushort index);
        //一个手轮信号控制单个轴运动
        //设置输入手轮脉冲信号的工作方式 控制单轴
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_handwheel_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_handwheel_inmode(ushort CardNo, ushort axis, ushort inmode, Int32 multi, double vh);
        //读取输入手轮脉冲信号的工作方式 控制单轴
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_handwheel_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_handwheel_inmode(ushort CardNo, ushort axis, ref ushort inmode, ref Int32 multi, ref double vh);
        //一个手轮信号控制多个轴运动
        //设置输入手轮脉冲信号的工作方式 控制多轴
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_handwheel_inmode_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_handwheel_inmode_extern(ushort CardNo, ushort inmode, ushort AxisNum, ushort[] AxisList, Int32[] multi);
        //读取输入手轮脉冲信号的工作方式 控制多轴
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_handwheel_inmode_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_handwheel_inmode_extern(ushort CardNo, ref ushort inmode, ref ushort AxisNum, ushort[] AxisList, Int32[] multi);
        //启动指定轴的手轮脉冲运动
        [DllImport("MCC800S.dll", EntryPoint = "YK_handwheel_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_handwheel_move(ushort CardNo, ushort axis);

        //MCC1200增加一个手轮通道
        //设置输入手轮脉冲信号的工作方式 控制单轴
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_handwheel_inmode_esp", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_handwheel_inmode_esp(ushort CardNo, ushort axis,ushort channel, ushort inmode, Int32 multi, double vh);
        //读取输入手轮脉冲信号的工作方式 控制单轴
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_handwheel_inmode_esp", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_handwheel_inmode_esp(ushort CardNo, ushort axis, ref ushort channel, ref ushort inmode, ref Int32 multi, ref double vh);
        //一个手轮信号控制多个轴运动
        //设置输入手轮脉冲信号的工作方式 控制多轴
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_handwheel_inmode_extern_esp", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_handwheel_inmode_extern_esp(ushort CardNo, ushort inmode,  ushort AxisNum, ushort[] AxisList, Int32[] multi,ushort[] channel);
        //读取输入手轮脉冲信号的工作方式 控制多轴
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_handwheel_inmode_extern_esp", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_handwheel_inmode_extern_esp(ushort CardNo, ref ushort inmode, ref ushort AxisNum, ushort[] AxisList, Int32[] multi, ushort[] channel);
   
      
        //编码器=============================================================================================================================================================	
        //设定编码器的计数方式 
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_counter_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_counter_inmode(ushort CardNo, ushort axis, ushort mode);
        //读取编码器的计数方式
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_counter_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_counter_inmode(ushort CardNo, ushort axis, ref ushort mode);
        //设置指定轴编码器脉冲计数值
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_encoder(ushort CardNo, ushort axis, double encoder_value);
        //读取指定轴编码器脉冲计数值；
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_get_encoder(ushort CardNo, ushort axis);
        //设置指定轴的EZ信号
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_ez_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_ez_mode(ushort CardNo, ushort axis, ushort ez_logic, ushort ez_mode, double filter);
        //读取指定轴的EZ信号设置
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_ez_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_ez_mode(ushort CardNo, ushort axis, ref ushort ez_logic, ref ushort ez_mode, ref double filter);
        //高速位置锁存==============================================================================================================================================
        //设置LTC信号
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_ltc_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_ltc_mode(ushort CardNo, ushort axis, ushort ltc_logic, ushort ltc_mode, double filter);
        //读取设置LTC信号
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_ltc_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_ltc_mode(ushort CardNo, ushort axis, ref ushort ltc_logic, ref ushort ltc_mode, ref double filter);
        //设置锁存方式
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_latch_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_latch_mode(ushort CardNo, ushort axis, ushort all_enable, ushort latch_source, ushort latch_channel);
        //读取锁存方式
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_latch_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_latch_mode(ushort CardNo, ushort axis, ref ushort all_enable, ref ushort latch_source, ref ushort latch_channel);
        //复位锁存器标志
        [DllImport("MCC800S.dll", EntryPoint = "YK_reset_latch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_reset_latch_flag(ushort CardNo, ushort axis);
        //读取锁存器标志
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_latch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_latch_flag(ushort CardNo, ushort axis);
        //读取锁存器的值
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_latch_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_get_latch_value(ushort CardNo, ushort axis);
        //读取有效锁存次数
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_latch_flag_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_latch_flag_extern(ushort CardNo, ushort axis);
        //按索引读取锁存器的值
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_latch_value_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_get_latch_value_extern(ushort CardNo, ushort axis, ushort Index);
        //LTC端口触发延时急停时间 单位：微秒-------------------------------------------------------------------------------------------------------------------------
        //设置LTC端口触发延时急停时间
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_latch_stop_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_latch_stop_time(ushort CardNo, ushort axis, Int32 time);
        //读取LTC端口触发延时急停时间
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_latch_stop_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_latch_stop_time(ushort CardNo, ushort axis, ref Int32 time);
        //LTC反相输出-----------------------------------------------------------------------------------------------------------------------------------------------
        //设置LTC反向输出模式
        [DllImport("MCC800S.dll", EntryPoint = "YK_SetLtcOutMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_SetLtcOutMode(ushort CardNo, ushort axis, ushort enable, ushort bitno);
        //读取LTC反相输出设置(位置锁存反相输出读取)
        [DllImport("MCC800S.dll", EntryPoint = "YK_GetLtcOutMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_GetLtcOutMode(ushort CardNo, ushort axis, ref ushort enable, ref ushort bitno);
        
        //一维低速位置比较=============================================================================================================================================
        //配置比较器	
        [DllImport("MCC800S.dll", EntryPoint = "YK_compare_set_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_set_config(ushort CardNo, ushort axis, ushort enable, ushort cmp_source);
        //读取配置比较器
        [DllImport("MCC800S.dll", EntryPoint = "YK_compare_get_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_get_config(ushort CardNo, ushort axis, ref ushort enable, ref ushort cmp_source);
        //清除所有比较点
        [DllImport("MCC800S.dll", EntryPoint = "YK_compare_clear_points", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_clear_points(ushort CardNo, ushort axis);
        //添加比较点
        [DllImport("MCC800S.dll", EntryPoint = "YK_compare_add_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_add_point(ushort CardNo, ushort axis, double pos, ushort dir, ushort action, UInt32 actpara);
        //读取当前比较点
        [DllImport("MCC800S.dll", EntryPoint = "YK_compare_get_current_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_get_current_point(ushort CardNo, ushort axis, ref double pos);
        //查询已经比较过的点
        [DllImport("MCC800S.dll", EntryPoint = "YK_compare_get_points_runned", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_get_points_runned(ushort CardNo, ushort axis, ref Int32 pointNum);
        //查询可以加入的比较点数量
        [DllImport("MCC800S.dll", EntryPoint = "YK_compare_get_points_remained", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_get_points_remained(ushort CardNo, ushort axis, ref Int32 pointNum);
        
        //二维低速位置比较-------------------------------------------------------------------------------------------------------------------------------------------
        //配置比较器
        [DllImport("MCC800S.dll", EntryPoint = "YK_compare_set_config_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_set_config_extern(ushort CardNo, ushort enable, ushort cmp_source);
        //读取配置比较器
        [DllImport("MCC800S.dll", EntryPoint = "YK_compare_get_config_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_get_config_extern(ushort CardNo, ref ushort enable, ref ushort cmp_source);
        //清除所有比较点
        [DllImport("MCC800S.dll", EntryPoint = "YK_compare_clear_points_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_clear_points_extern(ushort CardNo);
        //添加两轴位置比较点
        [DllImport("MCC800S.dll", EntryPoint = "YK_compare_add_point_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_add_point_extern(ushort CardNo, ushort[] axis, double[] pos, ushort[] dir, ushort action, UInt32 actpara);
        //读取当前比较点
        [DllImport("MCC800S.dll", EntryPoint = "YK_compare_get_current_point_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_get_current_point_extern(ushort CardNo, double[] pos);
        //查询已经比较过的点
        [DllImport("MCC800S.dll", EntryPoint = "YK_compare_get_points_runned_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_get_points_runned_extern(ushort CardNo, ref Int32 pointNum);
        //查询可以加入的二维比较点数量
        [DllImport("MCC800S.dll", EntryPoint = "YK_compare_get_points_remained_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_get_points_remained_extern(ushort CardNo, ref Int32 pointNum);

        //一维高速位置比较 ------------------------------------------------------------------------------------------------------------------------------------------
        //设置高速一维位置比较模式
        [DllImport("MCC800S.dll", EntryPoint = "YK_hcmp_set_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_set_mode(ushort CardNo, ushort hcmp, ushort cmp_enable);
        //读取高速一维位置比较模式设置
        [DllImport("MCC800S.dll", EntryPoint = "YK_hcmp_get_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_get_mode(ushort CardNo, ushort hcmp, ref ushort cmp_enable);
        //配置高速一维位置比较相关参数
        [DllImport("MCC800S.dll", EntryPoint = "YK_hcmp_set_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_set_config(ushort CardNo, ushort hcmp, ushort axis, ushort cmp_source, ushort cmp_logic, Int32 time);
        //读取高速一维位置比较器配置
        [DllImport("MCC800S.dll", EntryPoint = "YK_hcmp_get_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_get_config(ushort CardNo, ushort hcmp, ref ushort axis, ref ushort cmp_source, ref ushort cmp_logic, ref Int32 time);
        //添加/更新高速一维比较位置
        [DllImport("MCC800S.dll", EntryPoint = "YK_hcmp_add_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_add_point(ushort CardNo, ushort hcmp, double cmp_pos);
        //设置高速一维线性比较模式相关参数 
        [DllImport("MCC800S.dll", EntryPoint = "YK_hcmp_set_liner", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_set_liner(ushort CardNo, ushort hcmp, double Increment, Int32 Count);
        //读取高速一维比较线性模式参数设置
        [DllImport("MCC800S.dll", EntryPoint = "YK_hcmp_get_liner", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_get_liner(ushort CardNo, ushort hcmp, ref double Increment, ref Int32 Count);
        //读取高速一维位置比较状态
        [DllImport("MCC800S.dll", EntryPoint = "YK_hcmp_get_current_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_get_current_state(ushort CardNo, ushort hcmp, ref Int32 remained_points, ref double current_point, ref Int32 runned_points);
        //清除已添加的所有高速一维位置比较点 
        [DllImport("MCC800S.dll", EntryPoint = "YK_hcmp_clear_points", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_clear_points(ushort CardNo, ushort hcmp);
        //读取指定高速比较输出口CMP端口的电平
        [DllImport("MCC800S.dll", EntryPoint = "YK_read_cmp_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_cmp_pin(ushort CardNo, ushort hcmp);
        //控制指定高速比较输出口CMP 端口的输出
        [DllImport("MCC800S.dll", EntryPoint = "YK_write_cmp_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_cmp_pin(ushort CardNo, ushort hcmp, ushort on_off);

        //二维高速位置比较------------------------------------------------------------------------------------------------------------------------------------------------
        //设置高速二维位置比较输出参数。
        [DllImport("MCC800S.dll", EntryPoint = "YK_2DCompareSetPrm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DCompareSetPrm(ushort CardNo, ushort chn, ushort encx, ushort ency, ushort source, ushort maxerr, ushort threhold);
        //设置高速二维位置比较输出电平、脉冲和PWM。
        [DllImport("MCC800S.dll", EntryPoint = "YK_2DComparePulse", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DComparePulse(ushort CardNo, ushort chn, ushort level, ushort outputType, Int32 time, float pwm_duty, Int32 pwm_freq, Int32 pwm_num, ushort pwm_channal);
        //设置高速二维位置比较位置
        [DllImport("MCC800S.dll", EntryPoint = "YK_2DCompareData", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DCompareData(ushort CardNo, ushort chn, double pX, double pY);
        //设置高速二维位置比较输出接口
        [DllImport("MCC800S.dll", EntryPoint = "YK_SetComparePort", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_SetComparePort(ushort CardNo, ushort chn, ushort compare_port);
        //清空高速二维位置比较位置数据  
        [DllImport("MCC800S.dll", EntryPoint = "YK_2DCompareClear", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DCompareClear(ushort CardNo, ushort chn);
        //启动高速二维位置比较输出
        [DllImport("MCC800S.dll", EntryPoint = "YK_2DCompareStart", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DCompareStart(ushort CardNo, ushort chn, ushort cmp_en);
        //停止高速二维位置比较输出 
        [DllImport("MCC800S.dll", EntryPoint = "YK_2DCompareStop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DCompareStop(ushort CardNo, ushort chn, ushort cmp_en);
        //读取二维位置比较输出状态
        [DllImport("MCC800S.dll", EntryPoint = "YK_2DCompareStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DCompareStatus(ushort CardNo, ushort chn, ref ushort pStatus, ref ushort pCount, ref ushort pFifoCount,ref double pPosx, ref double pPosy);

        //通用IO===============================================================================================================================================
        //读取输入口的状态   
        [DllImport("MCC800S.dll", EntryPoint = "YK_read_inbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_inbit(ushort CardNo, ushort bitno);
        //设置输出口的状态 
        [DllImport("MCC800S.dll", EntryPoint = "YK_write_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_outbit(ushort CardNo, ushort bitno, ushort on_off);
        //读取输出口的状态 
        [DllImport("MCC800S.dll", EntryPoint = "YK_read_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_outbit(ushort CardNo, ushort bitno);
        //读取输入端口的值  
        [DllImport("MCC800S.dll", EntryPoint = "YK_read_inport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 YK_read_inport(ushort CardNo, ushort portno);
        //读取输出端口的值 
        [DllImport("MCC800S.dll", EntryPoint = "YK_read_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 YK_read_outport(ushort CardNo, ushort portno);
        //设置输出端口的值  
        [DllImport("MCC800S.dll", EntryPoint = "YK_write_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_outport(ushort CardNo, ushort portno, UInt32 outport_val);
        //IO输出延时翻转
        [DllImport("MCC800S.dll", EntryPoint = "YK_IO_TurnOutDelay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_IO_TurnOutDelay(ushort CardNo, ushort bitno, UInt32 DelayTime);
        //以上函数以毫秒为单位可继续使用，新函数将时间统一到秒为单位
        [DllImport("MCC800S.dll", EntryPoint = "YK_reverse_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_reverse_outbit(ushort CardNo, ushort bitno, double reverse_time);

        //=============================================================================================================================================
        //IO辅助功能-IO计数功能 
        [DllImport("MCC800S.dll", EntryPoint = "YK_SetIoCountMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_SetIoCountMode(ushort CardNo, ushort bitno, ushort mode, UInt32 filter);
        
        [DllImport("MCC800S.dll", EntryPoint = "YK_GetIoCountMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_GetIoCountMode(ushort CardNo, ushort bitno, ref ushort mode, ref UInt32 filter);

        [DllImport("MCC800S.dll", EntryPoint = "YK_SetIoCountValue", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_SetIoCountValue(ushort CardNo, ushort bitno, UInt32 CountValue);

        [DllImport("MCC800S.dll", EntryPoint = "YK_GetIoCountValue", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_GetIoCountValue(ushort CardNo, ushort bitno, ref UInt32 CountValue);

        //以上函数以毫秒为单位可继续使用，新函数将时间统一到秒为单位
        //设置IO计数模式 
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_io_count_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_io_count_mode(ushort CardNo, ushort bitno, ushort mode, double filter_time);

        [DllImport("MCC800S.dll", EntryPoint = "YK_get_io_count_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_io_count_mode(ushort CardNo, ushort bitno, ref ushort mode, ref double filter_time);
        //设置IO计数值  
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_io_count_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_io_count_value(ushort CardNo, ushort bitno, UInt32 CountValue);
        //读取IO计数值  
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_io_count_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_io_count_value(ushort CardNo, ushort bitno, ref UInt32 CountValue);

        //伺服专用IO----------------------------------------------------------------------------------------------------------------------------
        //设置ALM信号
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_alm_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_alm_mode(ushort CardNo, ushort axis, ushort enable, ushort alm_logic, ushort alm_action);
        //读取设置ALM信号
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_alm_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_alm_mode(ushort CardNo, ushort axis, ref ushort enable, ref ushort alm_logic, ref ushort alm_action);
        //设置INP信号   
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_inp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_inp_mode(ushort CardNo, ushort axis, ushort enable, ushort inp_logic);
        //读取设置INP信号 
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_inp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_inp_mode(ushort CardNo, ushort axis, ref ushort enable, ref ushort inp_logic);
        //读取RDY状态
        [DllImport("MCC800S.dll", EntryPoint = "YK_read_rdy_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_rdy_pin(ushort CardNo, ushort axis);
        //控制ERC信号输出 
        [DllImport("MCC800S.dll", EntryPoint = "YK_write_erc_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_erc_pin(ushort CardNo, ushort axis, ushort sel);
        [DllImport("MCC800S.dll", EntryPoint = "YK_read_erc_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_erc_pin(ushort CardNo, ushort axis);
        //输出SEVON信号
        [DllImport("MCC800S.dll", EntryPoint = "YK_write_sevon_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_sevon_pin(ushort CardNo, ushort axis, ushort on_off);
        //读取SEVON信号
        [DllImport("MCC800S.dll", EntryPoint = "YK_read_sevon_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_sevon_pin(ushort CardNo, ushort axis);

        //运动状态检测	========================================================================================================================================
        //读取指定轴的当前速度
        [DllImport("MCC800S.dll", EntryPoint = "YK_read_current_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_read_current_speed(ushort CardNo, ushort axis);
        [DllImport("MCC800S.dll", EntryPoint = "YK_read_current_encoder_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_read_current_encoder_speed(ushort CardNo, ushort axis);
        //设定指定轴的当前位置
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_position(ushort CardNo, ushort axis, double current_position);
        //读取指定轴的当前位置   
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_get_position(ushort CardNo, ushort axis);
        //读取指定轴的目标位置
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_target_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_get_target_position(ushort CardNo, ushort axis);
        //读取指定轴的运动状态 
        [DllImport("MCC800S.dll", EntryPoint = "YK_check_done", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_check_done(ushort CardNo, ushort axis);
        //读取指定轴有关运动信号的状态 
        [DllImport("MCC800S.dll", EntryPoint = "YK_axis_io_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 YK_axis_io_status(ushort CardNo, ushort axis);
        //单轴减速停止/立即停止  
        [DllImport("MCC800S.dll", EntryPoint = "YK_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_stop(ushort CardNo, ushort axis, ushort stop_mode);
        //紧急停止所有轴
        [DllImport("MCC800S.dll", EntryPoint = "YK_emg_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_emg_stop(ushort CardNo);

        //检测轴到位状态---------------------------------------------------------------------------------------------------------------------------------
        //设置编码器系数、误差带
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_factor_error", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_factor_error(ushort CardNo, ushort axis, double factor, double error_pos);
        //读取编码器系数、误差带
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_factor_error", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_factor_error(ushort CardNo, ushort axis, ref double factor, ref double error_pos);
        //检测指令位置到位情况
        [DllImport("MCC800S.dll", EntryPoint = "YK_check_success_pulse", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_check_success_pulse(ushort CardNo, ushort axis);
        //检测编码器反馈位置到位情况
        [DllImport("MCC800S.dll", EntryPoint = "YK_check_success_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_check_success_encoder(ushort CardNo, ushort axis);

        //================================================================================================================================================
        //CAN—IO扩展
        //0-断开；1-连接；2-复位后自动连接
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_can_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_can_state(ushort CardNo, ushort NodeNum, ushort state, ushort Baud);//0-断开；1-连接；2-复位后自动连接

        [DllImport("MCC800S.dll", EntryPoint = "YK_get_can_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_can_state(ushort CardNo, ref ushort NodeNum, ref ushort state);//0-断开；1-连接；2-异常
        //读取CanIo通讯错误码
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_can_errcode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_can_errcode(ushort CardNo, ref ushort Errcode);//读取CanIo通讯错误码

        [DllImport("MCC800S.dll", EntryPoint = "YK_write_can_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_can_outbit(ushort CardNo, ushort Node, ushort bitno, ushort on_off);

        [DllImport("MCC800S.dll", EntryPoint = "YK_read_can_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_can_outbit(ushort CardNo, ushort Node, ushort bitno);

        [DllImport("MCC800S.dll", EntryPoint = "YK_read_can_inbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_can_inbit(ushort CardNo, ushort Node, ushort bitno);
        //写CanIo输出口
        [DllImport("MCC800S.dll", EntryPoint = "YK_write_can_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_can_outport(ushort CardNo, ushort Node, ushort PortNo, UInt32 outport_val);
        //读取CanIo输出端口
        [DllImport("MCC800S.dll", EntryPoint = "YK_read_can_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 YK_read_can_outport(ushort CardNo, ushort Node, ushort PortNo);
        //读取CanIo输入端口
        [DllImport("MCC800S.dll", EntryPoint = "YK_read_can_inport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 YK_read_can_inport(ushort CardNo, ushort Node, ushort PortNo);

        //PWM输出 ====================================================================================================================================================
        //设置PWM 使能状态 7号轴切换为PWM输出
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_pwm_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_pwm_enable(ushort CardNo, ushort enable);
        //读取PWM 使能状态设置
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_pwm_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_pwm_enable(ushort CardNo, ref ushort enable);
        //设置PWM 立即输出
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_pwm_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_pwm_output(ushort CardNo, ushort pwm_no, double fDuty, double fFre);
        //读取PWM 立即输出设置
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_pwm_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_pwm_output(ushort CardNo, ushort pwm_no, ref double fDuty, ref double fFre);
        //主卡与接线盒通讯状态
        [DllImport("MCC800S.dll", EntryPoint = "YK_LinkState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_LinkState(ushort CardNo, ref ushort State);
        //密码管理
        //验证密码，校验3次失败之后再次校验将返回校验失败
        [DllImport("MCC800S.dll", EntryPoint = "YK_check_sn", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_check_sn(ushort CardNo, string check_sn);
        //改写密码
        [DllImport("MCC800S.dll", EntryPoint = "YK_write_sn", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_sn(ushort CardNo, string new_sn);
        //函数库打印输出功能 mode：0-不输出，1-输出
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_debug_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_debug_mode(ushort mode, string FileName);

        [DllImport("MCC800S.dll", EntryPoint = "YK_get_debug_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_debug_mode(ref ushort mode, IntPtr FileName);

        //读取停止原因---------------------------------------------------------------------------------------------------------------------------------------
        //读取停止原因
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_stop_reason", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_stop_reason(ushort CardNo, ushort axis, ref Int32 StopReason);
        //脉冲当量设置
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_equiv", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_equiv(ushort CardNo, ushort axis, double equiv);

        [DllImport("MCC800S.dll", EntryPoint = "YK_get_equiv", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_equiv(ushort CardNo, ushort axis, ref double equiv);

        //反向间隙补偿==========================================================================================================================================
        //设定指定轴的反向间隙值
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_backlash", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_backlash(ushort CardNo, ushort axis, double backlash);
        //读取指定轴的反向间隙值
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_backlash", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_backlash(ushort CardNo, ushort axis, ref double backlash);
       
       //反向间隙补偿==========================================================================================================================================
        //设定指定轴的反向间隙值
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_flow_line_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_flow_line_speed(ushort CardNo, ushort axis, double flow_line_speed);
        //读取指定轴的反向间隙值
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_flow_line_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_flow_line_speed(ushort CardNo, ushort axis, ref double flow_line_speed);
        //AD/DA输出---------------------------------------------------------------------------------------------------------------------------
        //开启DA输出
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_da_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_da_enable(ushort CardNo, ushort enable);

        [DllImport("MCC800S.dll", EntryPoint = "YK_get_da_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_da_enable(ushort CardNo,ref ushort enable);
        //设置DA输出
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_da_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_da_output(ushort CardNo, ushort channel, double Vout);

        [DllImport("MCC800S.dll", EntryPoint = "YK_get_da_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_da_output(ushort CardNo, ushort channel,ref double Vout);
        //读取AD输入
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_ad_input", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_ad_input(ushort CardNo, ushort channel,ref double Vout);

        //------------------------------------------------------------------------------------------------------------------
        //小线段前瞻函数+++++++++++++++++2018.06.14 add+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //设置坐标系参数，确立坐标系映射，建立坐标系
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_set_crd_prm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_crd_prm(ushort CardNo, ushort Crd,ref TCrdPrm pCrdPrm);
        //查询坐标系参数
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_get_crd_prm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_crd_prm(ushort CardNo, ushort Crd, ref TCrdPrm pCrdPrm);
        //向插补缓存区增加插补数据。 用于在使用前瞻时，调用该指令表示后续没有新的数据，将会一次性把前瞻缓存区的数据压入运动缓存区
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_crd_data", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_data(ushort CardNo, ushort Crd);
        //直线插补
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_line_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
       	 public static extern short YK_buf_line_move(ushort CardNo,ushort Crd, ushort axisNum , ushort[] AxisList,double[] aim_pos,short posi_mode,double synVel, double synAcc, double velEnd,double velStart);//直线插补
       
       //圆心插补
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_arcn_center_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_arcn_center_move(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, double[] cen_pos, ushort circleDir, ushort circle_num, short posi_mode, double synVel, double synAcc, double velEnd, double velStart);
        
       ////半径圆弧插补
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_arcn_radius_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
       	public static extern short  YK_buf_arcn_radius_move(ushort CardNo,ushort Crd, ushort axisNum ,ushort[] AxisList,double[] aim_pos, double radius, ushort circleDir,ushort circle_num,short posi_mode,double synVel, double synAcc, double velEnd,double velStart);//半径圆弧插补

        //三点圆弧插补
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_arcn_3point_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_arcn_3point_move(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, double[] mid_pos, ushort circle_num, short posi_mode, double synVel, double synAcc, double velEnd, double velStart);//三点圆弧插补

       //缓存区内数字量 IO输出设置指令
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_io", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_io(ushort CardNo, ushort Crd, ushort doType, ushort doMask, ushort doValue);
        //缓存区指令，缓存区内延时设置指令
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_delay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_delay(ushort CardNo, ushort Crd, ushort delayTime);
        //设置缓存区内有效限位开关
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_limit_on", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_limit_on(ushort CardNo, ushort Crd, ushort axis, short limitType);
        //缓存区内无效限位开关
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_limit_off", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_limit_off(ushort CardNo, ushort Crd, ushort axis, short limitType);
        //缓存区内设置 axis 的停止IO信息
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_set_stop_io", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_stop_io(ushort CardNo, ushort Crd, ushort axis, short stopType, short inputType, short inputIndex);
        //实现刀向跟随功能，启动某个轴点位运动
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_move(ushort CardNo, ushort Crd, ushort moveAxis, double pos, double vel, double acc_time);
        //查询插补缓存区剩余空间
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_crd_remain_space", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_remain_space(ushort CardNo, ushort Crd, ref Int32 pSpace);
        //清除插补缓存区内的插补数据(只清除缓冲区数据，不关闭坐标系)
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_crd_clear", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_clear(ushort CardNo, ushort Crd);
        //启动缓冲区插补运动  
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_crd_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_start(ushort CardNo, ushort Crd);
        //暂停缓冲区插补运动
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_crd_pause", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_pause(ushort CardNo, ushort Crd);
        //停止缓冲区插补运动
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_crd_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_stop(ushort CardNo, ushort Crd, ushort mode);
        //查询插补运动坐标系状态
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_crd_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_status(ushort CardNo, ushort Crd, ref short pRun, ref Int32 pSegment);
        //设置自定义插补段段号
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_set_user_seg_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_user_seg_num(ushort CardNo, ushort Crd, Int32 segNum);
        //读取自定义插补段段号
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_get_user_seg_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_user_seg_num(ushort CardNo, ushort Crd, ref Int32 segNum);
        //读取未完成的插补段段数
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_get_remain_seg_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_remain_seg_num(ushort CardNo, ushort Crd, ref Int32 pSegment);
        //设置插补运动目标合成速度倍率
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_set_override", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_override(ushort CardNo, ushort Crd, double synVelRatio);
        //设置插补运动平滑停止、急停合成加速度
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_set_crd_stop_dec", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_crd_stop_dec(ushort CardNo, ushort Crd, double decSmoothStop, double decAbruptStop);
        //查询插补运动平滑停止、急停合成加速度
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_get_crd_stop_dec", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_crd_stop_dec(ushort CardNo, ushort Crd, ref double decSmoothStop, ref double decAbruptStop);
        //查询该坐标系的当前坐标位置值
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_get_crd_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_crd_pos(ushort CardNo, ushort Crd, double[] pPos);
        //查询该坐标系的当前坐标速度值
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_get_crd_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_crd_vel(ushort CardNo, ushort Crd, ref double pSynVel);
        //初始化插补前瞻缓存区
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_init_lookahead", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_init_lookahead(ushort CardNo, ushort Crd, double T, double accMax, uint enable);
        //查询插补前瞻缓存区
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_get_init_lookahead", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_init_lookahead(ushort CardNo, ushort Crd, ref double T, ref double accMax, ref uint enable);
        //设置插补速度曲线平滑系数
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_set_crd_smooth", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_crd_smooth(ushort CardNo, ushort Crd, double Smooth);
        //查询插补速度曲线平滑系数
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_get_crd_smooth", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_crd_smooth(ushort CardNo, ushort Crd, ref double Smooth);
        //关闭坐标系（运动停止，清除缓冲区数据，再关闭坐标系）
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_crd_reset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_reset(ushort CardNo, ushort Crd);
        //缓存区等待通用输入IO指令。
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_wait_di", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_wait_di(ushort CardNo, ushort Crd, ushort dI_id, ushort di_logic, Int32 time_out);
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //渐开线整圆封闭,设置螺旋线是否封闭
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_arc_involute_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_arc_involute_mode(ushort CardNo, ushort Crd, ushort mode);
        //读取螺旋线是否封闭设置
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_arc_involute_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_arc_involute_mode(ushort CardNo, ushort Crd,ref ushort mode);
        //矩形插补
        [DllImport("MCC800S.dll", EntryPoint = "YK_rectangle_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_rectangle_move(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Target_Pos, double[] Mark_Pos, long num, ushort rect_mode, ushort posi_mode, Int32 mark);

        //连续插补PWM 输出 PWM------------------------------------------------------------------------------------------------------------------------------------------------
        //连续插补缓存区pwm输出
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_set_pwm_on", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_pwm_on(ushort CardNo, ushort Crd, ushort pwm_no);
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_set_pwm_off", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_pwm_off(ushort CardNo, ushort Crd, ushort pwm_no);
       
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_pwm_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_pwm_output(ushort CardNo, ushort Crd, ushort pwm_no, double pwm_duty, double pwm_freq);
        //连续插补缓冲区中PWM跟随
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_pwm_follow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_pwm_follow(ushort CardNo, ushort Crd, ushort pwm_no, ushort mode, double start_speed, double max_speed, double max_power, double min_power, double none_follow_value);


        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_delay_outbit_to_start_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_delay_outbit_to_start_pos(UInt16 CardNo, UInt16 Crd, UInt16 bitno, UInt16 on_off, double delay_value, UInt16 delay_mode, double ReverseTime);  //连续插补中相对于轨迹段起点IO滞后输出(段内执行)
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_delay_outbit_to_stop_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_delay_outbit_to_stop_pos(UInt16 CardNo, UInt16 Crd, UInt16 bitno, UInt16 on_off, double delay_time, double ReverseTime);//连续插补中相对于轨迹段终点IO滞后输出
        [DllImport("MCC800S.dll", EntryPoint = "YK_buf_ahead_outbit_to_stop_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_ahead_outbit_to_stop_pos(UInt16 CardNo, UInt16 Crd, UInt16 bitno, UInt16 on_off, double ahead_value, UInt16 ahead_mode, double ReverseTime);//连续插补中现对于轨迹段终点IO提前输出；


       //添加非缓冲区函数，新增加20200901
        [DllImport("MCC800S.dll", EntryPoint = "YK_check_done_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_check_done_multicoor(UInt16 CardNo, UInt16 crd);
        [DllImport("MCC800S.dll", EntryPoint = "YK_stop_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_stop_multicoor(UInt16 CardNo, UInt16 crd, UInt16 stop_mode);
        //[DllImport("MCC800S.dll", EntryPoint = "YK_set_vector_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_set_vector_s_profile(UInt16 CardNo, UInt16 Crd, UInt16 s_mode, double s_para);   //设置S型速度曲线参数
        //[DllImport("MCC800S.dll", EntryPoint = "YK_get_vector_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_get_vector_s_profile(UInt16 CardNo, UInt16 Crd, UInt16 s_mode, ref double s_para);
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_vector_profile_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_vector_profile_multicoor(UInt16 CardNo, UInt16 Crd, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Stop_Vel);
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_vector_profile_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_vector_profile_multicoor(UInt16 CardNo, UInt16 Crd, ref double Min_Vel, ref double Max_Vel, ref double Taccdec, ref double Tdec, ref double Stop_Vel);

       [DllImport("MCC800S.dll", EntryPoint = "YK_read_vector_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_read_vector_speed(UInt16 CardNo, UInt16 Crd);
        //[DllImport("MCC800S.dll", EntryPoint = "YK_line_move_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_line_move_multicoor(UInt16 CardNo, UInt16 Crd, UInt16 axisNum, UInt16[] AxisList, double[] DistList, UInt16 posi_mode);


        //**************************************************************************************************************************************************************************
        //MCC800S专用 ,连续插补等功能---------------------------------------------------------------------------------------------------
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_axis_run_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_axis_run_mode(UInt16 CardNo, UInt16 axis, ref UInt16 run_mode);    //轴运动模式
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_backlash_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_backlash_unit(UInt16 CardNo, UInt16 axis, double backlash);    //反向间隙
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_backlash_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_backlash_unit(UInt16 CardNo, UInt16 axis, ref double backlash);
        [DllImport("MCC800S.dll", EntryPoint = "YK_t_pmove_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_t_pmove_unit(UInt16 CardNo, UInt16 axis, double Dist, UInt16 posi_mode);   //对称T型定长
        [DllImport("MCC800S.dll", EntryPoint = "YK_ex_t_pmove_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_ex_t_pmove_unit(UInt16 CardNo, UInt16 axis, double Dist, UInt16 posi_mode);    //非对称T型定长
        [DllImport("MCC800S.dll", EntryPoint = "YK_s_pmove_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_s_pmove_unit(UInt16 CardNo, UInt16 axis, double Dist, UInt16 posi_mode);   //对称S型定长
        [DllImport("MCC800S.dll", EntryPoint = "YK_ex_s_pmove_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_ex_s_pmove_unit(UInt16 CardNo, UInt16 axis, double Dist, UInt16 posi_mode);    //非对称S型定长


       [DllImport("MCC800S.dll", EntryPoint = "YK_rectangle_move_unit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_rectangle_move_unit(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] TargetPos, double[] MaskPos, Int32 Count, UInt16 rect_mode, UInt16 posi_mode);     //矩形区域插补，单段插补指令        
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_vector_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_vector_s_profile(UInt16 CardNo, UInt16 Crd, UInt16 s_mode, double s_para);   //设置S型速度曲线参数
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_vector_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_vector_s_profile(UInt16 CardNo, UInt16 Crd, UInt16 s_mode, ref double s_para);

       //非缓存区插补指令
        [DllImport("MCC800S.dll", EntryPoint = "YK_line_move_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_line_move_multicoor(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Target_Pos, UInt16 posi_mode);    //单段直线
        [DllImport("MCC800S.dll", EntryPoint = "YK_arcn_center_move_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_arcn_center_move_multicoor(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Target_Pos, double[] Cen_Pos, UInt16 Arc_Dir, Int32 Circle, UInt16 posi_mode);     //圆心终点式圆弧/螺旋线/渐开线
        [DllImport("MCC800S.dll", EntryPoint = "YK_arcn_radius_move_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_arcn_radius_move_multicoor(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Target_Pos, double Arc_Radius, UInt16 Arc_Dir, Int32 Circle, UInt16 posi_mode);    //半径终点式圆弧/螺旋线
        [DllImport("MCC800S.dll", EntryPoint = "YK_arcn_3point_move_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_arcn_3point_move_multicoor(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Target_Pos, double[] Mid_Pos, Int32 Circle, UInt16 posi_mode);     //三点式圆弧/螺旋线
    

        //以下为新增加总线API函数，20200410       

        //*******************以下为总线轴******************************
        //获取总线轴错误码
        [DllImport("MCC800S.dll", EntryPoint = "nmc_get_axis_errcode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_axis_errcode(UInt16 CardNo, UInt16 axis, ref UInt16 Errcode);
        //清除总线轴错误码
        [DllImport("MCC800S.dll", EntryPoint = "nmc_clear_axis_errcode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_clear_axis_errcode(UInt16 CardNo, UInt16 axis);
        //获取轴的从站节点信息
        [DllImport("MCC800S.dll", EntryPoint = "nmc_get_axis_node_address", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_axis_node_address(UInt16 CardNo, UInt16 axis, ref UInt16 SlaveAddr, ref UInt16 Sub_SlaveAddr);

        //设置总线轴状态字
        //[DllImport("MCC800S.dll", EntryPoint = "nmc_clear_axis_errcode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short nmc_clear_axis_errcode(UInt16 CardNo, UInt16 axis);
        //总线轴状态机
        [DllImport("MCC800S.dll", EntryPoint = "nmc_get_axis_state_machine", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_axis_state_machine(UInt16 CardNo, UInt16 axis, ref UInt16 Axis_StateMachine);
        //设置回零配置参数
        [DllImport("MCC800S.dll", EntryPoint = "nmc_set_home_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_set_home_profile(UInt16 CardNo, UInt16 axis, UInt16 home_mode, double Low_Vel, double High_Vel, double Tacc, double Tdec, double offsetpos);
        //回读回零配置参数
        [DllImport("MCC800S.dll", EntryPoint = "nmc_get_home_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_home_profile(UInt16 CardNo, UInt16 axis, ref UInt16 home_mode, ref double Low_Vel, ref double High_Vel, ref double Tacc, ref double Tdec, ref double offsetpos);
        //启动回零
        [DllImport("MCC800S.dll", EntryPoint = "nmc_home_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_home_move(UInt16 CardNo, UInt16 axis);

        //下载总线固件文件
        [DllImport("MCC800S.dll", EntryPoint = "nmc_download_memfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_download_memfile(UInt16 CardNo, String FileName, UInt16 filetype);

       //新增初始化参数

           [DllImport("MCC800S.dll", EntryPoint = "nmc_set_reset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
           public static extern short nmc_set_reset(UInt16 CardNo, UInt16 PortNum);	//重置EtherCAT主站状态并将总线上的从站切换到OP状态(FPGA)
           [DllImport("MCC800S.dll", EntryPoint = "nmc_set_initial", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public static extern short nmc_set_initial(UInt16 CardNo, UInt16 PortNum);	//执行系统初始化并将总线上的从站切换至OP状态
           [DllImport("MCC800S.dll", EntryPoint = "nmc_get_master_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public static extern short nmc_get_master_status(UInt16 CardNo, UInt16 PortNum);	//获取网络当前状态，在CMD_ECT_MASTER_INITIAL后调用
           [DllImport("MCC800S.dll", EntryPoint = "nmc_get_slavenum", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public static extern short nmc_get_slavenum(UInt16 CardNo, UInt16 PortNum);	//获取当前从站个数,掉线从站

           [DllImport("MCC800S.dll", EntryPoint = "nmc_set_cycletime", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public static extern short nmc_set_cycletime(UInt16 CardNo, UInt16 PortNum, int CycleTime);
          //回读总线周期
           [DllImport("MCC800S.dll", EntryPoint = "nmc_get_cycletime", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
           public static extern short nmc_get_cycletime(UInt16 CardNo, UInt16 PortNum, ref int CycleTime);
          //3.设置对象字典参数
           [DllImport("MCC800S.dll", EntryPoint = "nmc_set_node_od", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern short nmc_set_node_od(UInt16 CardNo, UInt16 PortNum, UInt16 NodeNum, UInt16 Index, UInt16 SubIndex, UInt16 ValLength, int Value);
          //回读对象字典参数
           [DllImport("MCC800S.dll", EntryPoint = "nmc_get_node_od", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern short nmc_get_node_od(UInt16 CardNo, UInt16 PortNum, UInt16 NodeNum, UInt16 Index, UInt16 SubIndex, UInt16 ValLength, ref int Value);
          //回读总线错误
           [DllImport("MCC800S.dll", EntryPoint = "nmc_get_errcode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern short nmc_get_errcode(UInt16 CardNo, UInt16 PortNum, ref UInt16 Errcode);
          //停止总线，返回0表示成功，其他参数表示不成功
           [DllImport("MCC800S.dll", EntryPoint = "nmc_stop_etc", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern short nmc_stop_etc(UInt16 CardNo, UInt16 PortNum, ref UInt16 ETCState);
          //返回总线从站个数
           [DllImport("MCC800S.dll", EntryPoint = "nmc_get_total_slaves", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern short nmc_get_total_slaves(UInt16 CardNo, UInt16 PortNum, ref UInt16 TotalSlaves);
          //总线软件复位
           [DllImport("MCC800S.dll", EntryPoint = "nmc_soft_reset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern short nmc_soft_reset(UInt16 CardNo);

           [DllImport("MCC800S.dll", EntryPoint = "nmc_clear_errcode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
          public static extern short nmc_clear_errcode(UInt16 CardNo, UInt16 PortNum);

        //回读从站信息
        [DllImport("MCC800S.dll", EntryPoint = "nmc_get_node_address", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_node_address(UInt16 CardNo, UInt16 PortNum, UInt16 NodeNum, ref UInt32 vendor_id, ref UInt32 device_id, ref UInt32 revision_id);
        //设置回读轴使能
        //输出SEVON信号
        [DllImport("MCC800S.dll", EntryPoint = "nmc_write_sevon_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_write_sevon_pin(ushort CardNo, ushort axis, ushort on_off);
        //读取SEVON信号
        [DllImport("MCC800S.dll", EntryPoint = "nmc_read_sevon_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_read_sevon_pin(ushort CardNo, ushort axis);

       //总线轴IO功能
        [DllImport("MCC800S.dll", EntryPoint = "nmc_axis_write_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_axis_write_outport(ushort CardNo, ushort axis,ushort portno,uint  outport_val);
        [DllImport("MCC800S.dll", EntryPoint = "nmc_axis_read_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint nmc_axis_read_outport(ushort CardNo, ushort axis, ushort portno);
        [DllImport("MCC800S.dll", EntryPoint = "nmc_axis_read_inbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_axis_read_inbit(ushort CardNo, ushort axis, ushort IoBit);
        [DllImport("MCC800S.dll", EntryPoint = "nmc_axis_read_inport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint nmc_axis_read_inport(ushort CardNo, ushort axis, ushort portno);  
        [DllImport("MCC800S.dll", EntryPoint = "nmc_axis_write_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_axis_write_outbit(ushort CardNo, ushort axis, ushort IoBit, ushort IoValue);
        [DllImport("MCC800S.dll", EntryPoint = "nmc_axis_read_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_axis_read_outbit(ushort CardNo, ushort axis, ushort IoBit);

        //总线IO模块功能
        [DllImport("MCC800S.dll", EntryPoint = "nmc_write_outbyte", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_write_outbyte(ushort CardNo, ushort NoteID, ushort portno, uint outport_val);
        [DllImport("MCC800S.dll", EntryPoint = "nmc_read_outbyte", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint nmc_read_outbyte(ushort CardNo, ushort NoteID, ushort portno);
        [DllImport("MCC800S.dll", EntryPoint = "nmc_read_inbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_read_inbit(ushort CardNo, ushort NoteID, ushort IoBit);
        [DllImport("MCC800S.dll", EntryPoint = "nmc_read_inbyte", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint nmc_read_inbyte(ushort CardNo, ushort NoteID, ushort portno);
        [DllImport("MCC800S.dll", EntryPoint = "nmc_write_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_write_outbit(ushort CardNo, ushort NoteID, ushort IoBit, ushort IoValue);
        [DllImport("MCC800S.dll", EntryPoint = "nmc_read_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_read_outbit(ushort CardNo, ushort NoteID, ushort IoBit);

        [DllImport("MCC800S.dll", EntryPoint = "nmc_download_configfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_download_configfile(ushort CardNo, String FileName);

       //高速IO翻转功能（定制）
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_high_speed_io_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_high_speed_io_para(ushort CardNo, ushort high_speed_io, ushort init_level, ushort mode_enable);//使能高速输出口，输出翻转IO电平；
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_high_speed_io_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_high_speed_io_para(ushort CardNo, ref ushort high_speed_io, ref ushort init_level, ref ushort mode_enable);//使能高速输出口，输出翻转IO电平；

        [DllImport("MCC800S.dll", EntryPoint = "YK_set_high_speed_io_strike", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_high_speed_io_strike(ushort CardNo, ushort high_speed_io, double pulse_width);//使能高速输出口，输出翻转IO电平；
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_high_speed_io_strike", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_high_speed_io_strike(ushort CardNo, ushort high_speed_io, ref double pulse_width);//使能高速输出口，输出翻转IO电平；

        [DllImport("MCC800S.dll", EntryPoint = "YK_set_latch_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_latch_stop(UInt16 CardNo, UInt16 ltc_Channel, UInt16 axis_Num, UInt16[] stop_axis_list, int[] time_Num);
        [DllImport("MCC800S.dll", EntryPoint = "YK_get_latch_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_latch_stop(UInt16 CardNo, UInt16 ltc_Channel, ref UInt16 axis_Num, ref UInt16[] stop_axis_list, ref int[] time_Num);

        [DllImport("MCC800S.dll", EntryPoint = "YK_fpga_update_firmware", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_fpga_update_firmware(ushort CardNo, String FileName);

        /*
功  能：单轴运动软着陆功能
参  数：CardNo:控制卡卡号;
axis:制定轴号;
  midPos:第一段pmove 终点位置，单位unit;
  targetPos:第二段pmove 终点位置,单位unit;
  startVel:起始速度，单位 unit/s;
  maxVel:最大速度，单位 unit/s;
  endVel:停止速度，单位 unit/s;
  tAcc:加速时间,单位秒（S）;
  tDec:减速时间，单位秒（S）;
  posiMode:位置坐标模式：0-相对位置坐标；1-绝对位置坐标；
返回值：错误代码；
*/
        [DllImport("MCC800S.dll", EntryPoint = "YK_pmove_soft_landing", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_pmove_soft_landing(UInt16 CardNo, UInt16 axis, double midPos, double targetPos, double startVel, double maxVel, double endVel, double tAcc, double tDec, UInt16 posiMode);
        /*
功  能：PSO (位置同步输出)运动控制
参  数：CardNo:控制卡卡号;
        psoEnable:位置同步输出使能，0-不使能；1-使能；;
                psoIO:位置同步输出Io端口;
                psoPos:位置同步输出脉冲，单位unit；
                activeLevel：位置同步输出有效电平；
                psoTime：位置同步输出IO持续时间；
返回值：错误代码；
*/
        [DllImport("MCC800S.dll", EntryPoint = "YK_pso_motion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_pso_motion(UInt16 CardNo, UInt16 psoEnable, UInt16 psoIO, double psoPos, UInt16 activeLevel, double psoTime);

         [DllImport("MCC800S.dll", EntryPoint = "YK_get_home_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_home_status(UInt16 CardNo, UInt16 axis,ref UInt16 homeStatus);

       //门型运动
         [DllImport("MCC800S.dll", EntryPoint = "YK_set_m_speed_parameter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern short YK_set_m_speed_parameter(UInt16 CardNo,UInt16 Crd,double sysStartVel,double sysMaxVel,double sysEndVel,double synAcc,double synDec);//设置缓冲区速度参数
        [DllImport("MCC800S.dll", EntryPoint = "YK_m_line_motion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern short YK_m_line_motion(UInt16 CardNo, UInt16 Crd, UInt16 axisNum, UInt16[] axisList, UInt16 processMode, double[] TargetPosList, UInt16 posi_mode, int mark);//设置门运动直线插补
         [DllImport("MCC800S.dll", EntryPoint = "YK_m_io_trig_line", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_m_io_trig_line(UInt16 CardNo, UInt16 Crd, UInt16 axisNum, UInt16[] axisList, UInt16 processMode, UInt16 trigInbitno, UInt16 trigInState, double[] TargetPosList, UInt16 posi_mode, int mark);//IO触发直线插补运动
         [DllImport("MCC800S.dll", EntryPoint = "YK_m_mutipos_trig_line", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern short YK_m_mutipos_trig_line(UInt16 CardNo, UInt16 Crd, UInt16 axisNum, UInt16[] axisList, UInt16 processMode, UInt16 trigAxisNum, UInt16[] trigAxisList, double[] trigPosList, UInt16[] trigPosTypeList, UInt16[] trigModeList, double[] TargetPosList, UInt16 posi_mode, int mark); //多轴位置触发插补运动
         [DllImport("MCC800S.dll", EntryPoint = "YK_m_io_mutipos_trig_line", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern short YK_m_io_mutipos_trig_line(UInt16 CardNo, UInt16 Crd, UInt16 axisNum, UInt16[] axisList, UInt16 processMode, UInt16 trigInbitno, UInt16 trigInState, UInt16 trigAxisNum, UInt16[] trigAxisList, double[] trigPosList, UInt16[] trigPosTypeList, UInt16[] trigModeList, double[] TargetPosList, UInt16 posi_mode, int mark); //IO、多轴位置触发插补运动
         [DllImport("MCC800S.dll", EntryPoint = "YK_m_pos_trig_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern short YK_m_pos_trig_outbit(UInt16 CardNo, UInt16 Crd, UInt16 bitno, UInt16 on_off, UInt16 ahead_axis, double ahead_value, UInt16 ahead_posType, UInt16 ahead_mode, int mark); //位置触发 IO 输出
         [DllImport("MCC800S.dll", EntryPoint = "YK_m_wait_input", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern short YK_m_wait_input(UInt16 CardNo, UInt16 Crd, UInt16 bitno, UInt16 on_off, UInt16 time_out, int mark);//缓冲区等待输入指令
         [DllImport("MCC800S.dll", EntryPoint = "YK_get_m_run_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern short YK_get_m_run_state(UInt16 CardNo, UInt16 Crd, ref UInt16 state, ref UInt16 enable, ref UInt16 stop_reason, ref UInt16 trig_phase, ref int mark);//读取运动状态

         //---------------20231014------蛙跳运动-----------------------------------------------
        [DllImport("MCC800S.dll", EntryPoint = "YK_set_w_speed_parameter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern short YK_set_w_speed_parameter(UInt16 CardNo, UInt16 Crd, double sysStartVel, double sysMaxVel, double sysEndVel, double synAcc, double synDec);//蛙跳速度参数设置
         [DllImport("MCC800S.dll", EntryPoint = "YK_set_w_up_parameter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_w_up_parameter(UInt16 CardNo, UInt16 Crd, UInt16 axisNum, UInt16[] axisList, double[] MidPosList, double[] TargetPosList, UInt16 posi_mode, int mark);//蛙跳上升直线插补参数设置
         [DllImport("MCC800S.dll", EntryPoint = "YK_set_w_shift_parameter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern short YK_set_w_shift_parameter(UInt16 CardNo, UInt16 Crd, UInt16 axisNum, UInt16[] axisList, double[] MidPosList, double[] TargetPosList, UInt16 posi_mode, int mark);//蛙跳横移直线插补参数设置
         [DllImport("MCC800S.dll", EntryPoint = "YK_set_w_down_parameter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern short YK_set_w_down_parameter(UInt16 CardNo, UInt16 Crd, UInt16 axisNum, UInt16[] axisList, double[] MidPosList, double[] TargetPosList, UInt16 posi_mode, int mark);//蛙跳下降直线插补参数设置
         [DllImport("MCC800S.dll", EntryPoint = "YK_w_line_motion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern short YK_w_line_motion(UInt16 CardNo, UInt16 Crd, UInt32 delayTime); //蛙跳开始运动 将3段直线插补压入缓冲
         [DllImport("MCC800S.dll", EntryPoint = "YK_get_w_run_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
         public static extern short YK_get_w_run_time(UInt16 CardNo, UInt16 Crd, ref UInt32 delayTime, ref UInt32 upAimTime, ref UInt32 upMiddlemTime, ref UInt32 shiftAimTime, ref UInt32 shiftMiddlemTime, ref UInt32 downAimTime, ref UInt32 downMiddlemTime); //蛙跳计算时间返回，用于调试

       
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
