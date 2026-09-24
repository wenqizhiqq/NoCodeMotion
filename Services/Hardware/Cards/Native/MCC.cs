﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/DLL/MCC.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace csMCC
{
    public class MCC
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
        //=
        //========================================================================================================================================================================
        //板卡配置
        [DllImport("MCC.dll", EntryPoint = "YK_board_init_EX", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_board_init_EX(uint findCard);

        [DllImport("MCC.dll", EntryPoint = "YK_board_init", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_board_init();
        [DllImport("MCC.dll", EntryPoint = "YK_board_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_board_close();
        [DllImport("MCC.dll", EntryPoint = "YK_board_reset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_board_reset();
        [DllImport("MCC.dll", EntryPoint = "YK_get_CardInfList", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_CardInfList(ref UInt16 CardNum, UInt32[] CardTypeList, UInt16[] CardIdList);
        [DllImport("MCC.dll", EntryPoint = "YK_get_card_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_card_version(UInt16 CardNo, ref UInt32 CardVersion);
        [DllImport("MCC.dll", EntryPoint = "YK_get_card_soft_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_card_soft_version(UInt16 CardNo, ref UInt32 FirmID, ref UInt32 SubFirmID);
        [DllImport("MCC.dll", EntryPoint = "YK_get_card_lib_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_card_lib_version(ref UInt32 LibVer);
        [DllImport("MCC.dll", EntryPoint = "YK_get_total_axes", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_total_axes(UInt16 CardNo, ref UInt32 TotalAxis);
        [DllImport("MCC.dll", EntryPoint = "YK_get_hard_ware_info", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_hard_ware_info(UInt16 CardNo, byte[] hardWareInfo);


        //下载参数文件------------------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_download_configfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_download_configfile(UInt16 CardNo, String FileName);
        //下载固件文件
        [DllImport("MCC.dll", EntryPoint = "YK_download_firmware", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_download_firmware(UInt16 CardNo, String FileName);
        //=======================================================================================================================================================================
        //MCC800P专用 轴IO映射配置
        [DllImport("MCC.dll", EntryPoint = "YK_set_AxisIoMap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_AxisIoMap(UInt16 CardNo, UInt16 Axis, UInt16 IoType, UInt16 MapIoType, UInt16 MapIoIndex, UInt32 Filter);
        [DllImport("MCC.dll", EntryPoint = "YK_get_AxisIoMap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_AxisIoMap(UInt16 CardNo, UInt16 Axis, UInt16 IoType, ref UInt16 MapIoType, ref UInt16 MapIoIndex, ref UInt32 Filter);
        //以上函数以毫秒为单位可继续使用，新函数将时间统一到秒为单位
        [DllImport("MCC.dll", EntryPoint = "YK_set_axis_io_map", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_axis_io_map(UInt16 CardNo, UInt16 Axis, UInt16 IoType, UInt16 MapIoType, UInt16 MapIoIndex, double Filter);
        [DllImport("MCC.dll", EntryPoint = "YK_get_axis_io_map", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_axis_io_map(UInt16 CardNo, UInt16 Axis, UInt16 IoType, ref UInt16 MapIoType, ref UInt16 MapIoIndex, ref double Filter);
        [DllImport("MCC.dll", EntryPoint = "YK_set_special_input_filter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_special_input_filter(UInt16 CardNo, double Filter);      //设置所有专用IO滤波时间
        //3800专用 虚拟IO映射  用于读取滤波后的IO口电平状态
        [DllImport("MCC.dll", EntryPoint = "YK_set_io_map_virtual", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_io_map_virtual(UInt16 CardNo, UInt16 bitno, UInt16 MapIoType, UInt16 MapIoIndex, double Filter);
        [DllImport("MCC.dll", EntryPoint = "YK_get_io_map_virtual", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_io_map_virtual(UInt16 CardNo, UInt16 bitno, ref UInt16 MapIoType, ref UInt16 MapIoIndex, ref double Filter);
        [DllImport("MCC.dll", EntryPoint = "YK_read_inbit_virtual", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_inbit_virtual(UInt16 CardNo, UInt16 bitno);
        //==============================================================================================================================================================================
        //限位异常设置
        [DllImport("MCC.dll", EntryPoint = "YK_set_softlimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_softlimit(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 source_sel, UInt16 SL_action, Int32 N_limit, Int32 P_limit);
        [DllImport("MCC.dll", EntryPoint = "YK_get_softlimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_softlimit(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 source_sel, ref UInt16 SL_action, ref Int32 N_limit, ref Int32 P_limit);
        [DllImport("MCC.dll", EntryPoint = "YK_set_el_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_el_mode(UInt16 CardNo, UInt16 axis, UInt16 el_enable, UInt16 el_logic, UInt16 el_mode);
        [DllImport("MCC.dll", EntryPoint = "YK_get_el_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_el_mode(UInt16 CardNo, UInt16 axis, ref UInt16 el_enable, ref UInt16 el_logic, ref UInt16 el_mode);
        [DllImport("MCC.dll", EntryPoint = "YK_set_emg_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_emg_mode(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 emg_logic);
        [DllImport("MCC.dll", EntryPoint = "YK_get_emg_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_emg_mode(UInt16 CardNo, UInt16 axis, ref UInt16 enbale, ref UInt16 emg_logic);
        //MCC800P专用 外部减速停止信号及减速停止时间设置-----------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_set_dstp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_dstp_mode(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 logic, UInt32 time);
        [DllImport("MCC.dll", EntryPoint = "YK_get_dstp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_dstp_mode(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 logic, ref UInt32 time);
        [DllImport("MCC.dll", EntryPoint = "YK_set_dstp_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_dstp_time(UInt16 CardNo, UInt16 axis, UInt32 time);
        [DllImport("MCC.dll", EntryPoint = "YK_get_dstp_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_dstp_time(UInt16 CardNo, UInt16 axis, ref UInt32 time);
        //以上函数以毫秒为单位可继续使用，新函数将时间统一到秒为单位
        [DllImport("MCC.dll", EntryPoint = "YK_set_io_dstp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_io_dstp_mode(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 logic);
        [DllImport("MCC.dll", EntryPoint = "YK_get_io_dstp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_io_dstp_mode(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 logic);
        [DllImport("MCC.dll", EntryPoint = "YK_set_dec_stop_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_dec_stop_time(UInt16 CardNo, UInt16 axis, double stop_time);
        [DllImport("MCC.dll", EntryPoint = "YK_get_dec_stop_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_dec_stop_time(UInt16 CardNo, UInt16 axis, ref double stop_time);
        //==========================================================================================================================================================================================
        //速度设置
        [DllImport("MCC.dll", EntryPoint = "YK_set_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_profile(UInt16 CardNo, UInt16 axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double stop_vel);
        [DllImport("MCC.dll", EntryPoint = "YK_get_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_profile(UInt16 CardNo, UInt16 axis, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double stop_vel);
        [DllImport("MCC.dll", EntryPoint = "YK_set_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_s_profile(UInt16 CardNo, UInt16 axis, UInt16 s_mode, double s_para);
        [DllImport("MCC.dll", EntryPoint = "YK_get_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_s_profile(UInt16 CardNo, UInt16 axis, UInt16 s_mode, ref double s_para);
        [DllImport("MCC.dll", EntryPoint = "YK_set_vector_profile_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_vector_profile_multicoor(UInt16 CardNo, UInt16 Crd, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Stop_Vel);
        [DllImport("MCC.dll", EntryPoint = "YK_get_vector_profile_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_vector_profile_multicoor(UInt16 CardNo, UInt16 Crd, ref double Min_Vel, ref double Max_Vel, ref double Taccdec, ref double Tdec, ref double Stop_Vel);


    
        [DllImport("MCC.dll", EntryPoint = "YK_set_vector_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_vector_s_profile(UInt16 CardNo, UInt16 Crd, UInt16 axis, UInt16 s_mode, double s_para);
        [DllImport("MCC.dll", EntryPoint = "YK_get_vector_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_vector_s_profile(UInt16 CardNo, UInt16 Crd, UInt16 axis, UInt16 s_mode, ref double s_para);

        //===========================================================================================================================================================================================
        //运动模块脉冲模式
        [DllImport("MCC.dll", EntryPoint = "YK_set_pulse_outmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_pulse_outmode(UInt16 CardNo, UInt16 axis, UInt16 outmode);
        [DllImport("MCC.dll", EntryPoint = "YK_get_pulse_outmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_pulse_outmode(UInt16 CardNo, UInt16 axis, ref UInt16 outmode);
        //点位运动-------------------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_pmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_pmove(UInt16 CardNo, UInt16 axis, Int32 Dist, UInt16 posi_mode);
        //JOG运动--------------------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_vmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_vmove(UInt16 CardNo, UInt16 axis, UInt16 dir);
        //PVT运动------------------------------------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_PvtTable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_PvtTable(UInt16 CardNo, UInt16 iaxis, UInt32 count, double[] pTime, Int32[] pPos, double[] pVel);
        [DllImport("MCC.dll", EntryPoint = "YK_PtsTable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_PtsTable(UInt16 CardNo, UInt16 iaxis, UInt32 count, double[] pTime, Int32[] pPos, double[] pPercent);
        [DllImport("MCC.dll", EntryPoint = "YK_PvtsTable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_PvtsTable(UInt16 CardNo, UInt16 iaxis, UInt32 count, double[] pTime, Int32[] pPos, double velBegin, double velEnd);
        [DllImport("MCC.dll", EntryPoint = "YK_PttTable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_PttTable(UInt16 CardNo, UInt16 iaxis, UInt32 count, double[] pTime, int[] pPos);
        [DllImport("MCC.dll", EntryPoint = "YK_PvtMove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_PvtMove(UInt16 CardNo, UInt16 AxisNum, UInt16[] AxisList);
        //==============================================================================================================================================================
        //在线变位/变速运动
        [DllImport("MCC.dll", EntryPoint = "YK_reset_target_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_reset_target_position(UInt16 CardNo, UInt16 axis, Int32 dist, UInt16 posi_mode);
        [DllImport("MCC.dll", EntryPoint = "YK_change_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_change_speed(UInt16 CardNo, UInt16 axis, double Curr_Vel, double Taccdec);
        [DllImport("MCC.dll", EntryPoint = "YK_update_target_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_update_target_position(UInt16 CardNo, UInt16 axis, Int32 dist, UInt16 posi_mode);
        //插补===================================================================================================================================================================
        //直线插补		
        [DllImport("MCC.dll", EntryPoint = "YK_line_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_line_multicoor(UInt16 CardNo, UInt16 crd, UInt16 axisNum, UInt16[] axisList, double[] DistList, UInt16 posi_mode);
        //平面圆弧
        [DllImport("MCC.dll", EntryPoint = "YK_arc_move_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_arc_move_multicoor(UInt16 CardNo, UInt16 crd, UInt16[] AxisList, double[] Target_Pos, double[] Cen_Pos, UInt16 Arc_Dir, UInt16 posi_mode);
        //=======================================================================================================================================================================
        //回零运动
        [DllImport("MCC.dll", EntryPoint = "YK_set_home_pin_logic", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_home_pin_logic(UInt16 CardNo, UInt16 axis, UInt16 org_logic, double filter);
        [DllImport("MCC.dll", EntryPoint = "YK_get_home_pin_logic", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_home_pin_logic(UInt16 CardNo, UInt16 axis, ref UInt16 org_logic, ref double filter);
        [DllImport("MCC.dll", EntryPoint = "YK_set_homemode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_homemode(UInt16 CardNo, UInt16 axis, UInt16 home_dir, double vel, UInt16 mode, UInt16 EZ_count);
        [DllImport("MCC.dll", EntryPoint = "YK_get_homemode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_homemode(UInt16 CardNo, UInt16 axis, ref UInt16 home_dir, ref double vel, ref UInt16 home_mode, ref UInt16 EZ_count);
        [DllImport("MCC.dll", EntryPoint = "YK_home_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_home_move(UInt16 CardNo, UInt16 axis);
        //MCC800P专用 原点锁存--------------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_set_homelatch_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_homelatch_mode(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 logic, UInt16 source);
        [DllImport("MCC.dll", EntryPoint = "YK_get_homelatch_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_homelatch_mode(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 logic, ref UInt16 source);
        [DllImport("MCC.dll", EntryPoint = "YK_get_homelatch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_homelatch_flag(UInt16 CardNo, UInt16 axis);
        [DllImport("MCC.dll", EntryPoint = "YK_reset_homelatch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_reset_homelatch_flag(UInt16 CardNo, UInt16 axis);
        [DllImport("MCC.dll", EntryPoint = "YK_get_homelatch_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_homelatch_value(UInt16 CardNo, UInt16 axis);
        //==========================================================================================================================================================
        //MCC800P专用 手轮运动 
        //手轮通道选择
        [DllImport("MCC.dll", EntryPoint = "YK_set_handwheel_channel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_handwheel_channel(UInt16 CardNo, UInt16 index);
        [DllImport("MCC.dll", EntryPoint = "YK_get_handwheel_channel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_handwheel_channel(UInt16 CardNo, ref UInt16 index);
        //一个手轮信号控制单个轴运动	
        [DllImport("MCC.dll", EntryPoint = "YK_set_handwheel_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_handwheel_inmode(UInt16 CardNo, UInt16 axis, UInt16 inmode, Int32 multi, double vh);
        [DllImport("MCC.dll", EntryPoint = "YK_get_handwheel_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_handwheel_inmode(UInt16 CardNo, UInt16 axis, ref UInt16 inmode, ref Int32 multi, ref double vh);
        //MCC800P专用 一个手轮信号控制多个轴运动
        [DllImport("MCC.dll", EntryPoint = "YK_set_handwheel_inmode_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_handwheel_inmode_extern(UInt16 CardNo, UInt16 inmode, UInt16 AxisNum, UInt16[] AxisList, Int32[] multi);
        [DllImport("MCC.dll", EntryPoint = "YK_get_handwheel_inmode_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_handwheel_inmode_extern(UInt16 CardNo, ref UInt16 inmode, ref UInt16 AxisNum, UInt16[] AxisList, Int32[] multi);
        //启动手轮运动
        [DllImport("MCC.dll", EntryPoint = "YK_handwheel_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_handwheel_move(UInt16 CardNo, UInt16 axis);
        //==========================================================================================================================================================
        //编码器
        [DllImport("MCC.dll", EntryPoint = "YK_set_counter_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_counter_inmode(UInt16 CardNo, UInt16 axis, UInt16 mode);
        [DllImport("MCC.dll", EntryPoint = "YK_get_counter_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_counter_inmode(UInt16 CardNo, UInt16 axis, ref UInt16 mode);
        [DllImport("MCC.dll", EntryPoint = "YK_set_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_encoder(UInt16 CardNo, UInt16 axis, Int32 encoder_value);
        [DllImport("MCC.dll", EntryPoint = "YK_get_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_encoder(UInt16 CardNo, UInt16 axis);
        [DllImport("MCC.dll", EntryPoint = "YK_set_ez_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_ez_mode(UInt16 CardNo, UInt16 axis, UInt16 ez_logic, UInt16 ez_mode, double filter);
        [DllImport("MCC.dll", EntryPoint = "YK_get_ez_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_ez_mode(UInt16 CardNo, UInt16 axis, ref UInt16 ez_logic, ref UInt16 ez_mode, ref double filter);
        //==========================================================================================================================================================
        //高速锁存-------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_set_ltc_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_ltc_mode(UInt16 CardNo, UInt16 axis, UInt16 ltc_logic, UInt16 ltc_mode, Double filter);
        [DllImport("MCC.dll", EntryPoint = "YK_get_ltc_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_ltc_mode(UInt16 CardNo, UInt16 axis, ref UInt16 ltc_logic, ref UInt16 ltc_mode, ref Double filter);
        [DllImport("MCC.dll", EntryPoint = "YK_set_latch_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_latch_mode(UInt16 CardNo, UInt16 axis, UInt16 all_enable, UInt16 latch_source, UInt16 latch_channel);
        [DllImport("MCC.dll", EntryPoint = "YK_get_latch_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_latch_mode(UInt16 CardNo, UInt16 axis, ref UInt16 all_enable, ref UInt16 latch_source, ref UInt16 latch_channel);
        [DllImport("MCC.dll", EntryPoint = "YK_reset_latch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_reset_latch_flag(UInt16 CardNo, UInt16 axis);
        [DllImport("MCC.dll", EntryPoint = "YK_get_latch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_latch_flag(UInt16 CardNo, UInt16 axis);
        [DllImport("MCC.dll", EntryPoint = "YK_get_latch_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_latch_value(UInt16 CardNo, UInt16 axis);
        [DllImport("MCC.dll", EntryPoint = "YK_get_latch_flag_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_latch_flag_extern(UInt16 CardNo, UInt16 axis);
        [DllImport("MCC.dll", EntryPoint = "YK_get_latch_value_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_latch_value_extern(UInt16 CardNo, UInt16 axis, UInt16 Index);
        //LTC端口触发延时急停时间 单位us
        [DllImport("MCC.dll", EntryPoint = "YK_set_latch_stop_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_latch_stop_time(UInt16 CardNo, UInt16 axis, Int32 time);
        [DllImport("MCC.dll", EntryPoint = "YK_get_latch_stop_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_latch_stop_time(UInt16 CardNo, UInt16 axis, ref Int32 time);
        //MCC800P专用 LTC反相输出------------------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_SetLtcOutMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_SetLtcOutMode(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 bitno);
        [DllImport("MCC.dll", EntryPoint = "YK_GetLtcOutMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_GetLtcOutMode(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 bitno);
        //=============================================================================================================================================================
        //单轴低速位置比较
        [DllImport("MCC.dll", EntryPoint = "YK_compare_set_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_set_config(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 cmp_source);
        [DllImport("MCC.dll", EntryPoint = "YK_compare_get_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_get_config(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 cmp_source);
        [DllImport("MCC.dll", EntryPoint = "YK_compare_clear_points", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_clear_points(UInt16 CardNo, UInt16 axis);
        [DllImport("MCC.dll", EntryPoint = "YK_compare_add_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_add_point(UInt16 CardNo, UInt16 axis, Int32 pos, UInt16 dir, UInt16 action, UInt32 actpara);
        [DllImport("MCC.dll", EntryPoint = "YK_compare_get_current_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_get_current_point(UInt16 CardNo, UInt16 axis, ref Int32 pos);
        [DllImport("MCC.dll", EntryPoint = "YK_compare_get_points_runned", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_get_points_runned(UInt16 CardNo, UInt16 axis, ref Int32 pointNum);
        [DllImport("MCC.dll", EntryPoint = "YK_compare_get_points_remained", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_get_points_remained(UInt16 CardNo, UInt16 axis, ref Int32 pointNum);
        //二维低速位置比较
        [DllImport("MCC.dll", EntryPoint = "YK_compare_set_config_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_set_config_extern(UInt16 CardNo, UInt16 enable, UInt16 cmp_source);
        [DllImport("MCC.dll", EntryPoint = "YK_compare_get_config_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_get_config_extern(UInt16 CardNo, ref UInt16 enable, ref UInt16 cmp_source);
        [DllImport("MCC.dll", EntryPoint = "YK_compare_clear_points_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_clear_points_extern(UInt16 CardNo);
        [DllImport("MCC.dll", EntryPoint = "YK_compare_add_point_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_add_point_extern(UInt16 CardNo, UInt16[] axis, Int32[] pos, UInt16[] dir, UInt16 action, UInt32 actpara);
        [DllImport("MCC.dll", EntryPoint = "YK_compare_get_current_point_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_compare_get_current_point_extern(UInt16 CardNo, ref Int32 pos);
        public static extern short YK_compare_get_current_point_extern(UInt16 CardNo,  Int32[] pos);
        [DllImport("MCC.dll", EntryPoint = "YK_compare_get_points_runned_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_get_points_runned_extern(UInt16 CardNo, ref Int32 pointNum);
        [DllImport("MCC.dll", EntryPoint = "YK_compare_get_points_remained_extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_compare_get_points_remained_extern(UInt16 CardNo, ref Int32 pointNum);
        //单轴高速位置比较       
        [DllImport("MCC.dll", EntryPoint = "YK_hcmp_set_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_set_mode(UInt16 CardNo, UInt16 hcmp, UInt16 cmp_enable);
        [DllImport("MCC.dll", EntryPoint = "YK_hcmp_get_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_get_mode(UInt16 CardNo, UInt16 hcmp, ref UInt16 cmp_enable);
        [DllImport("MCC.dll", EntryPoint = "YK_hcmp_set_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_set_config(UInt16 CardNo, UInt16 hcmp, UInt16 axis, UInt16 cmp_source, UInt16 cmp_logic, Int32 time);
        [DllImport("MCC.dll", EntryPoint = "YK_hcmp_get_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_get_config(UInt16 CardNo, UInt16 hcmp, ref UInt16 axis, ref UInt16 cmp_source, ref UInt16 cmp_logic, ref Int32 time);
        [DllImport("MCC.dll", EntryPoint = "YK_hcmp_add_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_add_point(UInt16 CardNo, UInt16 hcmp, Int32 cmp_pos);
        [DllImport("MCC.dll", EntryPoint = "YK_hcmp_set_liner", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_set_liner(UInt16 CardNo, UInt16 hcmp, Int32 Increment, Int32 Count);
        [DllImport("MCC.dll", EntryPoint = "YK_hcmp_get_liner", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_get_liner(UInt16 CardNo, UInt16 hcmp, ref Int32 Increment, ref Int32 Count);
        [DllImport("MCC.dll", EntryPoint = "YK_hcmp_get_current_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_get_current_state(UInt16 CardNo, UInt16 hcmp, ref Int32 remained_points, ref Int32 current_point, ref Int32 runned_points);
        [DllImport("MCC.dll", EntryPoint = "YK_hcmp_clear_points", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_hcmp_clear_points(UInt16 CardNo, UInt16 hcmp);
        [DllImport("MCC.dll", EntryPoint = "YK_read_cmp_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_cmp_pin(UInt16 CardNo, UInt16 hcmp);
        [DllImport("MCC.dll", EntryPoint = "YK_write_cmp_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_cmp_pin(UInt16 CardNo, UInt16 hcmp, UInt16 on_off);
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        //二维高速位置比较
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_2DCompareClear", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DCompareClear(UInt16 CardNo, UInt16 chn);
        [DllImport("MCC.dll", EntryPoint = "YK_2DCompareData", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DCompareData(UInt16 CardNo, UInt16 chn, Int32 pX, Int32 pY);
        [DllImport("MCC.dll", EntryPoint = "YK_2DComparePulse", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DComparePulse(UInt16 CardNo, UInt16 chn, UInt16 level, UInt16 outputType, Int32 time, Int32 pwm_duty, Int32 pwm_freq, Int32 pwm_num);
        [DllImport("MCC.dll", EntryPoint = "YK_2DCompareSetPrm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DCompareSetPrm(UInt16 CardNo, UInt16 chn, UInt16 encx, UInt16 ency, UInt16 source, UInt16 maxerr, UInt16 threhold);
        [DllImport("MCC.dll", EntryPoint = "YK_2DCompareStart", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DCompareStart(UInt16 CardNo, UInt16 chn, UInt16 cmp_en);
        [DllImport("MCC.dll", EntryPoint = "YK_2DCompareStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DCompareStatus(UInt16 CardNo, UInt16 chn, ref UInt16 pStatus, ref UInt16 pCount, ref UInt16 pFifoCount, ref int x, ref int y);
        [DllImport("MCC.dll", EntryPoint = "YK_2DCompareStop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DCompareStop(UInt16 CardNo, UInt16 chn, UInt16 cmp_en);
        [DllImport("MCC.dll", EntryPoint = "YK_SetComparePort", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_SetComparePort(UInt16 CardNo, UInt16 chn, UInt16 compare_port);
        //================================================================================================================================================================
        //通用IO
        [DllImport("MCC.dll", EntryPoint = "YK_read_inbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_inbit(UInt16 CardNo, UInt16 bitno);
        [DllImport("MCC.dll", EntryPoint = "YK_write_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_outbit(UInt16 CardNo, UInt16 bitno, UInt16 on_off);
        [DllImport("MCC.dll", EntryPoint = "YK_write_outbitEx", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_outbitEx(UInt16 CardNo, UInt16 bitno, UInt16 on_off);
        [DllImport("MCC.dll", EntryPoint = "YK_read_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_outbit(UInt16 CardNo, UInt16 bitno);
        [DllImport("MCC.dll", EntryPoint = "YK_read_outbitEx", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_outbitEx(UInt16 CardNo, UInt16 bitno);
        [DllImport("MCC.dll", EntryPoint = "YK_read_inport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 YK_read_inport(UInt16 CardNo, UInt16 portno);
        [DllImport("MCC.dll", EntryPoint = "YK_read_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 YK_read_outport(UInt16 CardNo, UInt16 portno);
        [DllImport("MCC.dll", EntryPoint = "YK_write_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_outport(UInt16 CardNo, UInt16 portno, UInt32 outport_val);
        //--------IO输出延时翻转--------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_IO_TurnOutDelay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_IO_TurnOutDelay(UInt16 CardNo, UInt16 bitno, UInt32 DelayTime);
        //以上函数以毫秒为单位可继续使用，新函数将时间统一到秒为单位
        [DllImport("MCC.dll", EntryPoint = "YK_reverse_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_reverse_outbit(UInt16 CardNo, UInt16 bitno, double reverse_time);
        //================================================================================================================================================================
        //MCC800专用 IO辅助功能(IO计数功能)
        [DllImport("MCC.dll", EntryPoint = "YK_SetIoCountMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_SetIoCountMode(UInt16 CardNo, UInt16 bitno, UInt16 mode, UInt32 filter);
        [DllImport("MCC.dll", EntryPoint = "YK_GetIoCountMode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_GetIoCountMode(UInt16 CardNo, UInt16 bitno, ref UInt16 mode, ref UInt32 filter);
        [DllImport("MCC.dll", EntryPoint = "YK_SetIoCountValue", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_SetIoCountValue(UInt16 CardNo, UInt16 bitno, UInt32 CountValue);
        [DllImport("MCC.dll", EntryPoint = "YK_GetIoCountValue", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_GetIoCountValue(UInt16 CardNo, UInt16 bitno, ref UInt32 CountValue);
        //以上函数以毫秒为单位可继续使用，新函数将时间统一到秒为单位
        [DllImport("MCC.dll", EntryPoint = "YK_set_io_count_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_io_count_mode(UInt16 CardNo, UInt16 bitno, UInt16 mode, double filter_time);
        [DllImport("MCC.dll", EntryPoint = "YK_get_io_count_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_io_count_mode(UInt16 CardNo, UInt16 bitno, ref UInt16 mode, ref double filter_time);
        [DllImport("MCC.dll", EntryPoint = "YK_set_io_count_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_io_count_value(UInt16 CardNo, UInt16 bitno, UInt32 CountValue);
        [DllImport("MCC.dll", EntryPoint = "YK_get_io_count_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_io_count_value(UInt16 CardNo, UInt16 bitno, ref UInt32 CountValue);
        //================================================================================================================================================================
        //伺服专用IO
        [DllImport("MCC.dll", EntryPoint = "YK_set_alm_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_alm_mode(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 alm_logic, UInt16 alm_action);
        [DllImport("MCC.dll", EntryPoint = "YK_get_alm_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_alm_mode(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 alm_logic, ref UInt16 alm_action);
        [DllImport("MCC.dll", EntryPoint = "YK_set_inp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_inp_mode(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 inp_logic);
        [DllImport("MCC.dll", EntryPoint = "YK_get_inp_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_inp_mode(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 inp_logic);
        [DllImport("MCC.dll", EntryPoint = "YK_read_rdy_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_rdy_pin(UInt16 CardNo, UInt16 axis);
        [DllImport("MCC.dll", EntryPoint = "YK_write_erc_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_erc_pin(UInt16 CardNo, UInt16 axis, UInt16 sel);
        [DllImport("MCC.dll", EntryPoint = "YK_read_erc_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_erc_pin(UInt16 CardNo, UInt16 axis);
        [DllImport("MCC.dll", EntryPoint = "YK_write_sevon_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_sevon_pin(UInt16 CardNo, UInt16 axis, UInt16 on_off);
        [DllImport("MCC.dll", EntryPoint = "YK_read_sevon_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_sevon_pin(UInt16 CardNo, UInt16 axis);
        //==============================================================================================================================================================
        //运动状态检测
        [DllImport("MCC.dll", EntryPoint = "YK_read_current_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_read_current_speed(UInt16 CardNo, UInt16 axis);
        [DllImport("MCC.dll", EntryPoint = "YK_read_vector_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_read_vector_speed(UInt16 CardNo, UInt16 Crd);
        [DllImport("MCC.dll", EntryPoint = "YK_get_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_position(UInt16 CardNo, UInt16 axis);
        [DllImport("MCC.dll", EntryPoint = "YK_set_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_position(UInt16 CardNo, UInt16 axis, Int32 current_position);
        [DllImport("MCC.dll", EntryPoint = "YK_get_target_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_target_position(UInt16 CardNo, UInt16 axis);
        [DllImport("MCC.dll", EntryPoint = "YK_check_done", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_check_done(UInt16 CardNo, UInt16 axis);
        [DllImport("MCC.dll", EntryPoint = "YK_axis_io_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 YK_axis_io_status(UInt16 CardNo, UInt16 axis);
        [DllImport("MCC.dll", EntryPoint = "YK_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_stop(UInt16 CardNo, UInt16 axis, UInt16 stop_mode);
        [DllImport("MCC.dll", EntryPoint = "YK_check_done_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_check_done_multicoor(UInt16 CardNo, UInt16 crd);
        [DllImport("MCC.dll", EntryPoint = "YK_stop_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_stop_multicoor(UInt16 CardNo, UInt16 crd, UInt16 stop_mode);
        [DllImport("MCC.dll", EntryPoint = "YK_emg_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_emg_stop(UInt16 CardNo);
        //检测轴到位状态------------------------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_set_factor_error", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_factor_error(UInt16 CardNo, UInt16 axis, double factor, Int32 error_pos);
        [DllImport("MCC.dll", EntryPoint = "YK_get_factor_error", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_factor_error(UInt16 CardNo, UInt16 axis, ref double factor, ref Int32 error_pos);
        [DllImport("MCC.dll", EntryPoint = "YK_check_success_pulse", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_check_success_pulse(UInt16 CardNo, UInt16 axis);
        [DllImport("MCC.dll", EntryPoint = "YK_check_success_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_check_success_encoder(UInt16 CardNo, UInt16 axis);
        //==========================================================================================================================================================
        //MCC800,MCC600,MCC800S专用, CAN-IO扩展
        [DllImport("MCC.dll", EntryPoint = "YK_set_can_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_can_state(UInt16 CardNo, UInt16 NodeNum, UInt16 state, UInt16 Baud);//0-断开；1-连接；2-复位后自动连接
        [DllImport("MCC.dll", EntryPoint = "YK_get_can_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_can_state(UInt16 CardNo, ref UInt16 NodeNum, ref UInt16 state);//0-断开；1-连接；2-异常
        [DllImport("MCC.dll", EntryPoint = "YK_get_can_errcode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_can_errcode(UInt16 CardNo, ref UInt16 Errcode);//读取CanIo通讯错误码
        [DllImport("MCC.dll", EntryPoint = "YK_write_can_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_can_outbit(UInt16 CardNo, UInt16 Node, UInt16 bitno, UInt16 on_off);
        [DllImport("MCC.dll", EntryPoint = "YK_read_can_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_can_outbit(UInt16 CardNo, UInt16 Node, UInt16 bitno);
        [DllImport("MCC.dll", EntryPoint = "YK_read_can_inbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_can_inbit(UInt16 CardNo, UInt16 Node, UInt16 bitno);
        [DllImport("MCC.dll", EntryPoint = "YK_write_can_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_can_outport(UInt16 CardNo, UInt16 Node, UInt16 PortNo, UInt32 outport_val);//写CanIo输出口
        [DllImport("MCC.dll", EntryPoint = "YK_read_can_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 YK_read_can_outport(UInt16 CardNo, UInt16 Node, UInt16 PortNo);//读取CanIo输出端口
        [DllImport("MCC.dll", EntryPoint = "YK_read_can_inport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 YK_read_can_inport(UInt16 CardNo, UInt16 Node, UInt16 PortNo);//读取CanIo输入端口
        //==========================================================================================================================================================
        //PWM输出 MCC800P MCC800S专用
        //[DllImport("MCC.dll", EntryPoint = "YK_set_pwm_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_set_pwm_enable(UInt16 CardNo, UInt16 enable);//7号轴切换为PWM输出
        //[DllImport("MCC.dll", EntryPoint = "YK_get_pwm_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_get_pwm_enable(UInt16 CardNo, ref UInt16 enable);
        //[DllImport("MCC.dll", EntryPoint = "YK_set_pwm_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_set_pwm_output(UInt16 CardNo, UInt16 pwm_no, double fDuty, double fFre);//设置PWM输出
        //[DllImport("MCC.dll", EntryPoint = "YK_get_pwm_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_get_pwm_output(UInt16 CardNo, UInt16 pwm_no, ref double fDuty, ref double fFre);//获取PWM输出设置
        //MCC800专用 主卡与接线盒通讯状态-------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_LinkState", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_LinkState(UInt16 CardNo, ref UInt16 State);
        //密码管理-------------------------------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_check_sn", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_check_sn(UInt16 CardNo, string check_sn);
        [DllImport("MCC.dll", EntryPoint = "YK_write_sn", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_sn(UInt16 CardNo, string new_sn);
        //函数库打印输出----------------------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_set_debug_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_debug_mode(UInt16 mode, string FileName);
        [DllImport("MCC.dll", EntryPoint = "YK_get_debug_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_debug_mode(ref UInt16 mode, IntPtr FileName);
        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        //读取停止原因
        [DllImport("MCC.dll", EntryPoint = "YK_get_stop_reason", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_stop_reason(UInt16 CardNo, UInt16 axis, ref Int32 StopReason);     //读取轴停止原因

        //读取AD输入------------------------------------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_get_ad_input", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_ad_input(UInt16 CardNo, UInt16 channel, ref double Vout);

        ///读取输出端口的值------------------------------------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_read_outport_spd", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint YK_read_outport_spd(UInt16 CardNo, UInt16 portno);

        [DllImport("MCC.dll", EntryPoint = "YK_pos_delay_trig_line", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_pos_delay_trig_line(UInt16 CardNo, UInt16 Crd, UInt16 trigId, UInt16 axisNum, UInt16[] axisList, UInt16 trigAxis, double trigPos, UInt16 trigPosType, UInt16 trigMode, double[] TargetPosList, UInt16 posi_mode, UInt16 delayTime);
        [DllImport("MCC.dll", EntryPoint = "YK_get_trig_run_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_trig_run_state(UInt16 CardNo, UInt16 Crd, ref UInt16 state, ref UInt16 trig_phase);
        [DllImport("MCC.dll", EntryPoint = "YK_pmotion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_pmotion(ushort CardNo, ushort axis, double min_vel, double max_vel, double tacc, double tdec, double stop_vel, double s_tacc, double s_tdec, long dist, ushort posi_mode); //设置定长运动

        //----------------------//二维位置补偿-----------------------------------------------------------------------------------
        /*
        功  能：设置二维补偿参数
        参  数：CardNo:控制卡卡号;
                        axis:指定轴号,取值范围MCC800:0~7,MCC1200:0~12;
                        axisTable[2]:二维补偿轴号表。
                        count[2]:补偿表的数据点数量;
                        posBegin[2]:补偿区域起始点。
                        step[2]:补偿区域的步长;
                        pData[16*16]:二维数组表； 
        返回值：错误代码
        */
        [DllImport("MCC.dll", EntryPoint = "YK_set_2d_compensate_para_table", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_2d_compensate_para_table(ushort CardNo, ushort axis, ushort[] axisTable, ushort[] count, double[] posBegin, double[] step, double[,] pData);

        /*
功  能：获取二维补偿参数
参  数：CardNo:控制卡卡号;
                axis:指定轴号,取值范围MCC800:0~7,MCC1200:0~12;
                axisTable[2]:返回二维补偿轴号表。
                count[2]:返回补偿表的数据点数量;
                posBegin[2]:返回补偿区域起始点。
                step[2]:返回补偿区域的步长;
                pData[16,16]:返回二维数组表； 
返回值：错误代码
*/
        [DllImport("MCC.dll", EntryPoint = "YK_get_2d_compensate_para_table", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_2d_compensate_para_table(ushort CardNo, ushort axis, ushort[] axisTable, ushort[] count, double[] posBegin, double[] step, double[,] pData);
        /*
功  能：设置二维补偿使能
参  数：CardNo:控制卡卡号;
                axis:指定轴号,取值范围MCC800:0~7,MCC1200:0~12;
                mode:模式,0边沿扩展一个布局，其余不扩展。
                enable:使能，0不使能，1使能; 
返回值：错误代码
*/
        [DllImport("MCC.dll", EntryPoint = "YK_set_2d_compensate_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_2d_compensate_enable(ushort CardNo, ushort axis, ushort mode, ushort enable);
        /*
功  能：获取二维补偿使能
参  数：CardNo:控制卡卡号;
                axis:指定轴号,取值范围MCC800:0~7,MCC1200:0~12;
                mode:返回模式,0边沿扩展一个布局，其余不扩展。
                enable:返回使能，0不使能，1使能; 
返回值：错误代码
*/
        [DllImport("MCC.dll", EntryPoint = "YK_get_2d_compensate_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_2d_compensate_enable(ushort CardNo, ushort axis, ref ushort mode, ref ushort enable);

        /*
功  能：指定轴点位运动使能 
参  数：CardNo:控制卡卡号;
                axis:指定轴号,取值范围MCC800:0~7,MCC1200:0~12;
                min_vel:起始速度,单位:pulse/s(最大值为2M)
                max_vel:最大速度,单位:pulse/s(最大值为2M);
                tacc:加速时间,单位:s(最小值为0.001s);
                tdec:减速时间,单位:s(最小值为0.001s);
                stop_vel:停止速度,单位:pulse/s(最大值为2M) 
                s_tacc:S加速平滑时间,[s]
                s_tdec:S减速平滑时间,[s]
                enable:使能,0:不使能,1:使能； 
返回值：错误代码
*/
        [DllImport("MCC.dll", EntryPoint = "YK_pmotion_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_pmotion_enable(ushort CardNo, ushort axis, double min_vel, double max_vel, double tacc, double tdec, double stop_vel, double s_tacc, double s_tdec, ushort enable); //设置定长运动

        //------------螺距补偿功能--------------------------------------------------
        /*
        功  能：设置螺距误差补偿功能使能
        参  数：CardNo:控制卡卡号;
                        axis:指定轴号,取值范围MCC800:0~7,MCC1200:0~12;
                        enable:使能 1：使能 0 不使能;

        返回值：错误代码
        */
        [DllImport("MCC.dll", EntryPoint = "YK_set_leadscrew_comp_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_leadscrew_comp_enable(ushort CardNo, ushort axis, ushort enable);

        /*
功  能：读取螺距误差补偿功能使能
参  数：CardNo:控制卡卡号;
                axis:指定轴号,取值范围MCC800:0~7,MCC1200:0~12;
                enable:返回使能 1：使能 0 不使能;

返回值：错误代码
*/
        [DllImport("MCC.dll", EntryPoint = "YK_get_leadscrew_comp_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_leadscrew_comp_enable(ushort CardNo, ushort axis, ref ushort enable);
        /*
功  能：设置螺距误差补偿的相关参数
参  数：CardNo:控制卡卡号;
                axis:指定轴号,取值范围MCC800:0~7,MCC1200:0~12;
                dataNum:补偿点个数，取值范围:[1-256]。
                startPos:为补偿起始点的规划位置，单位:puse，补偿起始点不能为负;
                lenPos:补偿区域总长，单位:pulse。补偿起始点和补偿总长一起构成了补偿区域，超出该规划位置范围，补偿无效。
                pCompPosList:为对应为正方向运动时，各点位置需要补偿的脉冲数;
                pCompNegList:为对应为负方向运动时，各点位置需要补偿的脉冲数； 
返回值：错误代码
*/
        [DllImport("MCC.dll", EntryPoint = "YK_set_leadscrew_comp_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_leadscrew_comp_para(ushort CardNo, ushort axis, ushort dataNum, int startPos, int lenPos, int[] pCompPosList, int[] pCompNegList);
        /*
功  能：读取螺距误差补偿的相关参数
参  数：CardNo:控制卡卡号;
                axis:指定轴号,取值范围MCC800:0~7,MCC1200:0~12;
                dataNum:返回补偿点个数，取值范围:[1.256]。
                startPos:返回为补偿起始点的规划位置，单位:puse，补偿起始点不能为负;
                lenPos:返回补偿区域总长，单位:pulse。补偿起始点和补偿总长一起构成了补偿区域，超出该规划位置范围，补偿无效。
                pCompPosList:返回为对应为正方向运动时，各点位置需要补偿的脉冲数;
                pCompNegList:返回为对应为负方向运动时，各点位置需要补偿的脉冲数； 
返回值：错误代码
*/
        [DllImport("MCC.dll", EntryPoint = "YK_get_leadscrew_comp_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_leadscrew_comp_para(ushort CardNo, ushort axis, ref ushort dataNum, ref int startPos, ref int lenPos, int[] pCompPosList, int[] pCompNegList);

        //小线段前瞻函数+++++++++++++++++2018.06.14 add+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //设置坐标系参数，确立坐标系映射，建立坐标系
        [DllImport("MCC.dll", EntryPoint = "YK_buf_set_crd_prm_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_crd_prm_ex(ushort CardNo, ushort Crd, ref TCrdPrm pCrdPrm);
        //查询坐标系参数1
        [DllImport("MCC.dll", EntryPoint = "YK_buf_get_crd_prm_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_crd_prm_ex(ushort CardNo, ushort Crd, ref TCrdPrm pCrdPrm);
        //向插补缓存区增加插补数据。 用于在使用前瞻时，调用该指令表示后续没有新的数据，将会一次性把前瞻缓存区的数据压入运动缓存区2
        [DllImport("MCC.dll", EntryPoint = "YK_buf_crd_data", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_data(ushort CardNo, ushort Crd);
        //直线插补
        [DllImport("MCC.dll", EntryPoint = "YK_buf_line_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_line_move(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, short posi_mode, double synVel, double synAcc, double velEnd, double velStart);//直线插补

        //圆心插补
        [DllImport("MCC.dll", EntryPoint = "YK_buf_arcn_center_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_arcn_center_move(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, double[] cen_pos, ushort circleDir, ushort circle_num, short posi_mode, double synVel, double synAcc, double velEnd, double velStart);

        ////半径圆弧插补
        [DllImport("MCC.dll", EntryPoint = "YK_buf_arcn_radius_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_arcn_radius_move(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, double radius, ushort circleDir, ushort circle_num, short posi_mode, double synVel, double synAcc, double velEnd, double velStart);//半径圆弧插补

        //三点圆弧插补
        [DllImport("MCC.dll", EntryPoint = "YK_buf_arcn_3point_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_arcn_3point_move(ushort CardNo, ushort Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, double[] mid_pos, ushort circle_num, short posi_mode, double synVel, double synAcc, double velEnd, double velStart);//三点圆弧插补

        //缓存区内数字量 IO输出设置指令
        [DllImport("MCC.dll", EntryPoint = "YK_buf_io", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_io(ushort CardNo, ushort Crd, ushort doType, ushort doMask, ushort doValue);
        //缓存区指令，缓存区内延时设置指令+
        [DllImport("MCC.dll", EntryPoint = "YK_buf_delay_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_delay_ex(ushort CardNo, ushort Crd, Int32 delayTime);
        //设置缓存区内有效限位开关
        [DllImport("MCC.dll", EntryPoint = "YK_buf_limit_on", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_limit_on(ushort CardNo, ushort Crd, ushort axis, short limitType);
        //缓存区内无效限位开关
        [DllImport("MCC.dll", EntryPoint = "YK_buf_limit_off", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_limit_off(ushort CardNo, ushort Crd, ushort axis, short limitType);
        //缓存区内设置 axis 的停止IO信息
        [DllImport("MCC.dll", EntryPoint = "YK_buf_set_stop_io", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_stop_io(ushort CardNo, ushort Crd, ushort axis, short stopType, short inputType, short inputIndex);
        //实现刀向跟随功能，启动某个轴点位运动
        [DllImport("MCC.dll", EntryPoint = "YK_buf_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_move(ushort CardNo, ushort Crd, ushort moveAxis, double pos, double vel, double acc_time);
        //查询插补缓存区剩余空间+
        [DllImport("MCC.dll", EntryPoint = "YK_buf_crd_remain_space", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_remain_space(ushort CardNo, ushort Crd, ref Int32 pSpace);
        //清除插补缓存区内的插补数据(只清除缓冲区数据，不关闭坐标系)
        [DllImport("MCC.dll", EntryPoint = "YK_buf_crd_clear_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_clear_ex(ushort CardNo, ushort Crd);
        //启动缓冲区插补运动  
        [DllImport("MCC.dll", EntryPoint = "YK_buf_crd_start_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_start_ex(ushort CardNo, ushort Crd);
        //暂停缓冲区插补运动
        [DllImport("MCC.dll", EntryPoint = "YK_buf_crd_active", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_active(ushort CardNo, ushort Crd);
        //停止缓冲区插补运动
        [DllImport("MCC.dll", EntryPoint = "YK_buf_crd_stop_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_stop_ex(ushort CardNo, ushort Crd, ushort mode);
        //查询插补运动坐标系状态
        [DllImport("MCC.dll", EntryPoint = "YK_buf_crd_status_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_status_ex(ushort CardNo, ushort Crd, ref short pRun, ref Int32 pSegment);
        //设置自定义插补段段号
        [DllImport("MCC.dll", EntryPoint = "YK_buf_set_user_seg_num_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_user_seg_num_ex(ushort CardNo, ushort Crd, Int32 segNum);
        //读取自定义插补段段号
        [DllImport("MCC.dll", EntryPoint = "YK_buf_get_user_seg_num_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_user_seg_num_ex(ushort CardNo, ushort Crd, ref Int32 segNum);
        //读取未完成的插补段段数+
        [DllImport("MCC.dll", EntryPoint = "YK_buf_get_remain_seg_num_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_remain_seg_num_ex(ushort CardNo, ushort Crd, ref Int32 pSegment);
        //设置插补运动目标合成速度倍率
        [DllImport("MCC.dll", EntryPoint = "YK_buf_set_override", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_override(ushort CardNo, ushort Crd, double synVelRatio);
        //设置插补运动平滑停止、急停合成加速度
        [DllImport("MCC.dll", EntryPoint = "YK_buf_set_crd_stop_dec", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_crd_stop_dec(ushort CardNo, ushort Crd, double decSmoothStop, double decAbruptStop);
        //查询插补运动平滑停止、急停合成加速度
        [DllImport("MCC.dll", EntryPoint = "YK_buf_get_crd_stop_dec", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_crd_stop_dec(ushort CardNo, ushort Crd, ref double decSmoothStop, ref double decAbruptStop);
        //查询该坐标系的当前坐标位置值
        [DllImport("MCC.dll", EntryPoint = "YK_buf_get_crd_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_crd_pos(ushort CardNo, ushort Crd, double[] pPos);
        //查询该坐标系的当前坐标速度值
        [DllImport("MCC.dll", EntryPoint = "YK_buf_get_crd_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_crd_vel(ushort CardNo, ushort Crd, ref double pSynVel);
        //初始化插补前瞻缓存区
        [DllImport("MCC.dll", EntryPoint = "YK_buf_init_lookahead", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_init_lookahead(ushort CardNo, ushort Crd, double T, double accMax, uint enable);
        //查询插补前瞻缓存区
        [DllImport("MCC.dll", EntryPoint = "YK_buf_get_init_lookahead", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_init_lookahead(ushort CardNo, ushort Crd, ref double T, ref double accMax, ref uint enable);
        //设置插补速度曲线平滑系数
        [DllImport("MCC.dll", EntryPoint = "YK_buf_set_crd_smooth", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_crd_smooth(ushort CardNo, ushort Crd, double Smooth);
        //查询插补速度曲线平滑系数
        [DllImport("MCC.dll", EntryPoint = "YK_buf_get_crd_smooth", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_crd_smooth(ushort CardNo, ushort Crd, ref double Smooth);
        //关闭坐标系（运动停止，清除缓冲区数据，再关闭坐标系）
        [DllImport("MCC.dll", EntryPoint = "YK_buf_crd_reset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_reset(ushort CardNo, ushort Crd);
        //缓存区等待通用输入IO指令。
        [DllImport("MCC.dll", EntryPoint = "YK_buf_wait_di", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_wait_di(ushort CardNo, ushort Crd, ushort dI_id, ushort di_logic, Int32 time_out);
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //渐开线整圆封闭,设置螺旋线是否封闭
        [DllImport("MCC.dll", EntryPoint = "YK_set_arc_involute_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_arc_involute_mode(ushort CardNo, ushort Crd, ushort mode);
        //读取螺旋线是否封闭设置
        [DllImport("MCC.dll", EntryPoint = "YK_get_arc_involute_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_arc_involute_mode(ushort CardNo, ushort Crd, ref ushort mode);
        //矩形插补
        [DllImport("MCC.dll", EntryPoint = "YK_rectangle_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_rectangle_move(ushort CardNo, ushort Crd, ushort AxisNum, ushort[] AxisList, double[] Target_Pos, double[] Mark_Pos, long num, ushort rect_mode, ushort posi_mode, Int32 mark);

        //连续插补PWM 输出 PWM------------------------------------------------------------------------------------------------------------------------------------------------
        //连续插补缓存区pwm输出
        [DllImport("MCC.dll", EntryPoint = "YK_buf_set_pwm_on", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_pwm_on(ushort CardNo, ushort Crd, ushort pwm_no);
        [DllImport("MCC.dll", EntryPoint = "YK_buf_set_pwm_off", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_pwm_off(ushort CardNo, ushort Crd, ushort pwm_no);

        [DllImport("MCC.dll", EntryPoint = "YK_buf_pwm_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_pwm_output(ushort CardNo, ushort Crd, ushort pwm_no, double pwm_duty, double pwm_freq);
        //连续插补缓冲区中PWM跟随
        [DllImport("MCC.dll", EntryPoint = "YK_buf_pwm_follow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_pwm_follow(ushort CardNo, ushort Crd, ushort pwm_no, ushort mode, double start_speed, double max_speed, double max_power, double min_power, double none_follow_value);


        [DllImport("MCC.dll", EntryPoint = "YK_buf_delay_outbit_to_start_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_delay_outbit_to_start_pos(UInt16 CardNo, UInt16 Crd, UInt16 bitno, UInt16 on_off, double delay_value, UInt16 delay_mode, double ReverseTime);  //连续插补中相对于轨迹段起点IO滞后输出(段内执行)
        [DllImport("MCC.dll", EntryPoint = "YK_buf_delay_outbit_to_stop_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_delay_outbit_to_stop_pos(UInt16 CardNo, UInt16 Crd, UInt16 bitno, UInt16 on_off, double delay_time, double ReverseTime);//连续插补中相对于轨迹段终点IO滞后输出
        [DllImport("MCC.dll", EntryPoint = "YK_buf_ahead_outbit_to_stop_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_ahead_outbit_to_stop_pos(UInt16 CardNo, UInt16 Crd, UInt16 bitno, UInt16 on_off, double ahead_value, UInt16 ahead_mode, double ReverseTime);//连续插补中现对于轨迹段终点IO提前输出；
        //指定卡号初始化------------------------------------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_board_init_one_card", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_board_init_one_card(UInt16 CardNo);
        [DllImport("MCC.dll", EntryPoint = "YK_board_close_one_card", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_board_close_one_card(UInt16 CardNo);






        //-------------------//add 20240521--------------------------------------------------------------------------------------------------------------------------
        /*
功   能:高速一维比较跟随设置； 
参   数:CardNo:控制卡卡号；
        main_axis:主轴轴号,取值范围:2-3；
        follow_axis:跟随轴轴号,取值范围:0-1； 
        follow_delay:跟随延时单位：[us];  
返回值:错误代码。
*/
        [DllImport("MCC.dll", EntryPoint = "YK_set_axis_high_compare_follow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_axis_high_compare_follow(ushort CardNo, ushort enable, ushort main_channel, ushort follow_channel, long follow_delay);//增加一维比较跟随输出
        /*
功  能：指令缓冲阻塞检查轴运动指令 
参  数：CardNo 控制卡卡号;
Crd指定控制卡上的坐标系号(取值范围:0~1);
返回值：坐标系状态,0:正在使用中,1:正常停止;
*/
        [DllImport("MCC.dll", EntryPoint = "YK_buf_check_done", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_check_done(ushort CardNo, ushort buf_id, ushort axis); //指令缓冲阻塞检查轴运动指令;
        /*
功  能：指令缓冲阻塞检查位置指令
参  数：CardNo 控制卡卡号;
axis:检测位置轴号,取值范围MC800:0~7,MCC1200:0~11;
        mode:模式，0：大于 1或不等于0：小于；
        pulse_value:位置信息;
返回值：错误代码。
*/
        [DllImport("MCC.dll", EntryPoint = "YK_buf_set_check_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_check_position(ushort CardNo, ushort buf_id, ushort axis, ushort mode, long pulse_value); //指令缓冲阻塞检查位置指令

        [DllImport("MCC.dll", EntryPoint = "YK_buf_pmotion_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_pmotion_enable(ushort CardNo, ushort buf_id, ushort axis, double min_vel, double max_vel, double tacc, double tdec, double stop_vel, double s_tacc, double s_tdec, ushort enable);//指令缓冲指定轴点位运动使能 

        [DllImport("MCC.dll", EntryPoint = "YK_buf_pos_delay_trig_line", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_pos_delay_trig_line(ushort CardNo, ushort buf_id, ushort Crd, ushort trigId, ushort axisNum, ushort[] AxisList, ushort trigAxis, double trigPos, ushort trigPosType, ushort trigMode, double[] TargetPosList, ushort posi_mode, ushort delayTime);//指令缓冲 位置、延时触发插补运动

        [DllImport("MCC.dll", EntryPoint = "YK_buf_pmotion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_pmotion(ushort CardNo, ushort buf_id, ushort axis, double min_vel, double max_vel, double tacc, double tdec, double stop_vel, double s_tacc, double s_tdec, long dist, ushort posi_mode);//指令缓冲指定轴点位运动 

        [DllImport("MCC.dll", EntryPoint = "YK_buf_write_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_write_outbit(ushort CardNo, ushort buf_id, ushort bitno, ushort on_off);        //指令缓冲设置指定控制卡的某个输出端口的电平 

        [DllImport("MCC.dll", EntryPoint = "YK_buf_pmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_pmove(ushort CardNo, ushort buf_id, ushort axis, long dist, ushort posi_mode);//指令缓冲指定轴点位运动 

        [DllImport("MCC.dll", EntryPoint = "YK_buf_set_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_profile(ushort CardNo, ushort buf_id, ushort axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Stop_Vel);//指令缓冲设置单轴运动速度曲线

        [DllImport("MCC.dll", EntryPoint = "YK_buf_set_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_s_profile(ushort CardNo, ushort buf_id, ushort axis, ushort s_mode, double s_para);//指令缓冲设置单轴速度曲线S段参数值(设置平滑速度曲线参数) 

        /*
功  能：设置模拟量比较模式参数;
参  数：CardNo:控制卡卡号;
                cmp_no:比较器号;
                enable:使能：0 禁止，1使能;
                channel1:ADC比较通道;
                channel2：ADC被比较通道；
                mode:模式 0 大于   1或其余 小于；
                io_num:0-31
                io_evel:有效电平 1：高电平  0：低电平
返回值：错误代码
*/
        [DllImport("MCC.dll", EntryPoint = "YK_set_adc_cmp_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_adc_cmp_para(ushort CardNo, ushort cmp_no, ushort enable, ushort channel1, ushort channel2, ushort mode, ushort io_num, ushort io_evel);//设置模拟量比较模式参数

        /*
功  能：获取模拟量比较模式参数;
参  数：CardNo:控制卡卡号;
                cmp_no:比较器号;
                enable:返回使能：0 禁止，1使能;
                channel1:返回ADC比较通道;
                channel2：返回ADC被比较通道；
                mode:返回模式 0 大于   1或其余 小于；
                io_num:返回0-31
                io_evel:有效电平 1：高电平  0：低电平
返回值：错误代码
*/
        [DllImport("MCC.dll", EntryPoint = "YK_get_adc_cmp_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_adc_cmp_para(ushort CardNo, ushort cmp_no, ref ushort enable, ref ushort channel1, ref ushort channel2, ref ushort mode, ref ushort io_num, ref ushort io_evel);//获取模拟量比较模式参数
        /*
功  能：缓冲区段数阻塞;
参  数：CardNo:控制卡卡号;
               buf_id:检测缓冲区号 0~2;
               checked_buf_id:被检测缓冲区号 0~2;
               stage_num：检测缓冲区段数;若Bebuf_id缓冲区没有执行值stagenum段，则缓冲区buf_id则阻塞等待；
返回值：错误代码
*/
        [DllImport("MCC.dll", EntryPoint = "YK_buf_stage_choke", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_stage_choke(ushort CardNo, ushort buf_id, ushort checked_buf_id, ushort stage_num);

        /*
功  能：区域软件限位功能参数设置;
参  数：CardNo:控制卡卡号;
                index:组号 0-3;
                enable:使能：0 禁止，1使能;
                axis0:轴号;
                axis1：轴号；
                if_encoder:是否使用编码器位置(0-指令位置,1-编码器位置)；
                center0:axis0 圆心位置(单位:脉冲);
                center1:axis1 圆心位置(单位:脉冲);
                limit_radius:圆的半径(单位:脉冲);
返回值：错误代码;
*/
        [DllImport("MCC.dll", EntryPoint = "YK_set_area_soft_limit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_area_soft_limit(ushort CardNo, ushort index, ushort enable, ushort axis0, ushort axis1, ushort if_encoder, int center0, int center1, int limit_radius);
        /*
功  能：区域软件限位功能参数读取;
参  数：CardNo:控制卡卡号;
                index:组号 0-3;
                enable:返回使能：0 禁止，1使能;
                axis0:返回轴号;
                axis1：返回轴号；
                if_encoder:返回是否使用编码器位置(0-指令位置,1-编码器位置)；
                center0:返回axis0 圆心位置(单位:脉冲);
                center1:返回axis1 圆心位置(单位:脉冲);
                limit_radius:返回圆的半径(单位:脉冲);
返回值：错误代码;
*/
        [DllImport("MCC.dll", EntryPoint = "YK_get_area_soft_limit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_area_soft_limit(ushort CardNo, ushort index, ref ushort enable, ref ushort axis0, ref ushort axis1, ref ushort if_encoder, ref int center0, ref int center1, ref int limit_radius);
        /*
功  能：同步运行单轴运动;
参  数：CardNo:控制卡卡号;
                axis_num:轴数;
                axis_list:轴号列表数组，最大长度为axis_num;
                aim_pos:轴位置列表数组，最大长度为axis_num;
                posi_mode：运动模式,0:相对坐标模式,1:绝对坐标模式 ；
返回值：错误代码;
*/
        [DllImport("MCC.dll", EntryPoint = "YK_pmotion_sync", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_pmotion_sync(ushort CardNo, ushort axis_num, ushort[] axis_list, int[] aim_pos, ushort posi_mode);

        /*
功  能：//指令缓冲区延时清零+计数(计数离上一次清零的时间)
参  数：CardNo:控制卡卡号;
返回值：错误代码
*/
        [DllImport("MCC.dll", EntryPoint = "YK_buf_counter_clear", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_counter_clear(ushort CardNo, ushort buf_id);

        /*
功  能：指令缓冲区延时时间获取(离上一次清零的时间)
参  数：CardNo:控制卡卡号;
                delay_time:延时时间;
返回值：错误代码
*/
        [DllImport("MCC.dll", EntryPoint = "YK_buf_get_counter_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_counter_time(ushort CardNo, ref int delay_time);
        /*
功能：设置PWM 使能状态
参数：CardNo 卡号
      enable： PWM 通道使能状态，第0位表示通道0使能禁止 0：禁止，1：使能
          第1位表示通道1使能禁止 0：禁止，1：使能
          第2位表示通道2使能禁止 0：禁止，1：使能
          第3位表示通道3使能禁止 0：禁止，1：使能
返回值：错误代码
        1）MCC800 中，当使能PWM 功能后，7 号轴的脉冲端口（PUL+与PUL-）作为PWM 输出通
        道0，7 号轴的方向端口（DIR+与DIR-）作为PWM 输出通道1
        2）当使用通道0 时，硬件接线可接PUL+与GND（PUL-与GND 则输出相反信号）；当使用
        通道1 时，硬件接线可接DIR+与GND（DIR-与GND 则输出相反信号）
        3）当使能PWM 功能后，运动控制卡的相应轴端口将不能输出电机控制信号
*/
        [DllImport("MCC.dll", EntryPoint = "YK_set_pwm_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_pwm_enable(ushort CardNo, ushort enable);//7号轴切换为PWM输出
        /*
功 能：读取PWM使能状态设置
参 数：CardNo:卡号
   enable:返回PWM 通道使能状态：
         第0位表示通道0使能禁止 0：禁止，1：使能
         第1位表示通道1使能禁止 0：禁止，1：使能
         第2位表示通道2使能禁止 0：禁止，1：使能
         第3位表示通道3使能禁止 0：禁止，1：使能
返回值：错误代码
*/
        [DllImport("MCC.dll", EntryPoint = "YK_get_pwm_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_pwm_enable(ushort CardNo, ref ushort enable);
        /*
功 能：设置PWM 立即输出
参 数：CardNo:卡号
       pwm_no:PWM 通道,取值范围：0~3
       fDuty:占空比,取值范围：0~1
       fFre:频率,取值范围:0~500KHz
返回值：错误代码
*/
        [DllImport("MCC.dll", EntryPoint = "YK_set_pwm_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_pwm_output(ushort CardNo, ushort pwm_no, double fDuty, double fFre);
        /*
功能：读取PWM 立即输出设置
参数：CardNo:卡号;
      pwm_no:PWM 通道，取值范围：0~1;
      fDuty:返回占空比设置值;
      fFre: 返回频率设置值;
返回值：错误代码
*/
        [DllImport("MCC.dll", EntryPoint = "YK_get_pwm_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_pwm_output(ushort CardNo, ushort pwm_no, ref double fDuty, ref double fFre);
        /*
功 能：设置PWM-IO输出;
参 数：CardNo:卡号;
       pwm_no:PWM 通道,取值范围：0~3;
       pwm_io:pwm输出io通道 [0,31];
返回值：错误代码
*/
        [DllImport("MCC.dll", EntryPoint = "YK_set_pwm_io", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_pwm_io(ushort CardNo, ushort pwm_no, ushort pwm_io);//设置PWM-IO输出
        /*
功 能：读取PWM-IO输出;
参 数：CardNo:卡号;
       pwm_no:PWM 通道,取值范围：0~3;
       pwm_io:返回pwm输出io通道 [0,31];
返回值：错误代码
*/
        [DllImport("MCC.dll", EntryPoint = "YK_get_pwm_io", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_pwm_io(ushort CardNo, ushort pwm_no, ref ushort pwm_io);
        /*
        功  能：软着陆 
        参  数：CardNo:控制卡卡号;
                axis:指定轴号,取值范围MCC800:0~7,MCC1200:0~12;
                aim_pos:第一段终点位置,单位:pulse; 
                target_pos：第二段终点位置，单位:pulse;
                acc_time:加速时间,单位:s(最小值为0.001s)
                dec_time:减速时间,单位:s(最小值为0.001s);
                start_vel:起始速度,单位:pulse/s(最大值为2M)
                max_vel:最大速度,单位:pulse/s(最大值为2M);
                mid_vel:中间速度，单位:pulse/s(最大值为2M);
                stop_vel:停止速度,单位:pulse/s(最大值为2M)        
                posi_mode:运动模式,0:相对坐标模式,1:绝对坐标模式 
        返回值：错误代码
        注  意：当运动模式为相对坐标模式时,目标位置大于0时正向运动,小于0时反向运动
        */
        [DllImport("MCC.dll", EntryPoint = "YK_pmotion_soft_landing", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_pmotion_soft_landing(ushort CardNo, ushort axis, double aim_pos, double target_pos, double acc_time, double dec_time, double start_vel, double max_vel, double mid_vel, double stop_vel, ushort pos_mode);
        /*
        功  能：设置PWM的方向 
        参  数：CardNo:控制卡卡号;
                io_dir:输出端口号,取值范围:0~31;
                dir:输出方向,0:负向,1:正向;
        返回值：错误代码；           
        */
        [DllImport("MCC.dll", EntryPoint = "YK_set_pwm_dir", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_pwm_dir(ushort CardNo, ushort io_dir, ushort dir);


        /*
      功  能：读取回零状态 
      参  数：CardNo:控制卡卡号;
              Axis：指定轴号，取值范围：MCC400:0~3，MCC1800:0~7，MCC1,200:0~11，MCC1600:0~15;
              homeStatus 轴回零状态：
                0：停止中；
                1：回零中；
                2：回零成功；
                3：回零失败；
      返回值：错误代码；           
      */
        [DllImport("MCC.dll", EntryPoint = "YK_get_home_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_home_status(ushort CardNo, ushort axis, ref ushort homeStatus);


    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
