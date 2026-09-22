﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/DLL/MCC1600p.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace csMCC1600
{
    public class MCC1600
    {


        //========================================================================================================================================================================
        //板卡配置
        [DllImport("MCC.dll", EntryPoint = "YK_board_init_EX", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_board_init_EX(uint findCard);
        [DllImport("MCC.dll", EntryPoint = "YK_board_init_one_card", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_board_init_one_card(ushort Card);

        [DllImport("MCC.dll", EntryPoint = "YK_board_init", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_board_init();
        [DllImport("MCC.dll", EntryPoint = "YK_board_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_board_close();
        [DllImport("MCC.dll", EntryPoint = "YK_board_close_one_card", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_board_close_one_card(ushort Card);

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
        //下载总线固件文件
        [DllImport("MCC.dll", EntryPoint = "nmc_download_memfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_download_memfile(UInt16 CardNo, String FileName, UInt16 filetype);
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
        public static extern short YK_set_softlimit(UInt16 CardNo, UInt16 axis, UInt16 enable, UInt16 source_sel, UInt16 SL_action, int N_limit, int P_limit);
        [DllImport("MCC.dll", EntryPoint = "YK_get_softlimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_softlimit(UInt16 CardNo, UInt16 axis, ref UInt16 enable, ref UInt16 source_sel, ref UInt16 SL_action, ref int N_limit, ref int P_limit);
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

        [DllImport("MCC.dll", EntryPoint = "YK_arcn_center_move_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_arcn_center_move_multicoor(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Target_Pos, double[] Cen_Pos, UInt16 Arc_Dir, Int32 Circle, UInt16 posi_mode);     //圆心终点式圆弧/螺旋线/渐开线
        [DllImport("MCC.dll", EntryPoint = "YK_arcn_radius_move_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_arcn_radius_move_multicoor(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Target_Pos, double Arc_Radius, UInt16 Arc_Dir, Int32 Circle, UInt16 posi_mode);    //半径终点式圆弧/螺旋线
        [DllImport("MCC.dll", EntryPoint = "YK_arcn_3point_move_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_arcn_3point_move_multicoor(UInt16 CardNo, UInt16 Crd, UInt16 AxisNum, UInt16[] AxisList, double[] Target_Pos, double[] Mid_Pos, Int32 Circle, UInt16 posi_mode);     //三点式圆弧/螺旋线

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
        public static extern short YK_compare_get_current_point_extern(UInt16 CardNo, Int32[] pos);
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
        //[DllImport("MCC.dll", EntryPoint = "YK_hcmp_2d_set_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_hcmp_2d_set_config(UInt16 CardNo, UInt16 cmp_en, UInt16 cmp_mode, UInt16 x_axis, UInt16 x_cmp_source, UInt16 y_axis, UInt16 y_cmp_source, Int32 m_error, UInt16 out_mode, Int32 time, UInt16 pwm_enable, double duty, Int32 freq, UInt16 port_sel, UInt16 pwm_counter, UInt16 pwm_num);
        //[DllImport("MCC.dll", EntryPoint = "YK_hcmp_2d_get_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_hcmp_2d_get_config(UInt16 CardNo, ref UInt16 cmp_en, ref UInt16 cmp_mode, ref UInt16 x_axis, ref UInt16 x_cmp_source, ref UInt16 y_axis, ref UInt16 y_cmp_source, ref Int32 m_error, ref UInt16 out_mode, ref Int32 time, ref UInt16 pwm_enable, ref double duty, ref Int32 freq, ref UInt16 port_sel, ref UInt16 pwm_counter, ref UInt16 pwm_num);
        //[DllImport("MCC.dll", EntryPoint = "YK_hcmp_2d_clear_points", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_hcmp_2d_clear_points(UInt16 CardNo);
        //[DllImport("MCC.dll", EntryPoint = "YK_hcmp_2d_add_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_hcmp_2d_add_point(UInt16 CardNo, Int32 x_cmp_pos, Int32 y_cmp_pos);
        //[DllImport("MCC.dll", EntryPoint = "YK_hcmp_2d_get_current_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_hcmp_2d_get_current_state(UInt16 CardNo, ref Int32 remained_points, ref Int32 x_current_point, ref Int32 y_current_point, ref Int32 runned_points, ref UInt16 current_state);
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_2DCompareClear", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DCompareClear(UInt16 CardNo, UInt16 chn);
        [DllImport("MCC.dll", EntryPoint = "YK_2DCompareData", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DCompareData(UInt16 CardNo, UInt16 chn, Int32 pX, Int32 pY);
        [DllImport("MCC.dll", EntryPoint = "YK_2DComparePulse", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DComparePulse(UInt16 CardNo, UInt16 chn, UInt16 level, UInt16 outputType, Int32 time, float pwm_duty, Int32 pwm_freq, Int32 pwm_num, UInt16 pwm_Channel);
        [DllImport("MCC.dll", EntryPoint = "YK_2DCompareSetPrm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DCompareSetPrm(UInt16 CardNo, UInt16 chn, UInt16 encx, UInt16 ency, UInt16 source, UInt16 maxerr, UInt16 threhold);
        [DllImport("MCC.dll", EntryPoint = "YK_2DCompareStart", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DCompareStart(UInt16 CardNo, UInt16 chn, UInt16 cmp_en);
        [DllImport("MCC.dll", EntryPoint = "YK_2DCompareStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_2DCompareStatus(UInt16 CardNo, UInt16 chn, ref UInt16 pStatus, ref UInt16 pCount, ref UInt16 pFifoCount, ref int pPosX, ref int pPosY);
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
        [DllImport("MCC.dll", EntryPoint = "YK_read_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_outbit(UInt16 CardNo, UInt16 bitno);
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
        [DllImport("MCC.dll", EntryPoint = "YK_set_pwm_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_pwm_enable(UInt16 CardNo, UInt16 enable);//7号轴切换为PWM输出
        [DllImport("MCC.dll", EntryPoint = "YK_get_pwm_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_pwm_enable(UInt16 CardNo, ref UInt16 enable);
        [DllImport("MCC.dll", EntryPoint = "YK_set_pwm_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_pwm_output(UInt16 CardNo, UInt16 pwm_no, double fDuty, double fFre);//设置PWM输出
        [DllImport("MCC.dll", EntryPoint = "YK_get_pwm_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_pwm_output(UInt16 CardNo, UInt16 pwm_no, ref double fDuty, ref double fFre);//获取PWM输出设置


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
        [DllImport("MCC.dll", EntryPoint = "YK_clear_stop_reason", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_clear_stop_reason(UInt16 CardNo, UInt16 axis);     //清除轴停止原因
        //基于脉冲当量的函数 MCC800P,MCC800S专用
        //1.1	脉冲当量设置
        [DllImport("MCC.dll", EntryPoint = "YK_set_equiv", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_equiv(UInt16 CardNo, UInt16 axis, double equiv);
        [DllImport("MCC.dll", EntryPoint = "YK_get_equiv", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_equiv(UInt16 CardNo, UInt16 axis, ref double equiv);   //脉冲当量
             //**************************************************************************************************************************************************************************
        //MCC800S专用 ,连续插补等功能---------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_get_axis_run_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_axis_run_mode(UInt16 CardNo, UInt16 axis, ref UInt16 run_mode);    //轴运动模式
        [DllImport("MCC.dll", EntryPoint = "YK_set_backlash", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_backlash(UInt16 CardNo, UInt16 axis, uint backlash, uint backlash_interval, int backlash_dir);    //反向间隙   

        [DllImport("MCC.dll", EntryPoint = "YK_get_backlash", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_backlash(UInt16 CardNo, UInt16 axis, ref uint backlash, ref uint backlash_interval, ref int backlash_dir);

        [DllImport("MCC.dll", EntryPoint = "YK_set_vector_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_vector_s_profile(UInt16 CardNo, UInt16 Crd, UInt16 s_mode, double s_para);   //设置S型速度曲线参数
        [DllImport("MCC.dll", EntryPoint = "YK_get_vector_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_vector_s_profile(UInt16 CardNo, UInt16 Crd, UInt16 s_mode, ref double s_para);
          //MCC800S专用 连续插补PWM输出-------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //设置PWM开关的占空比
        [DllImport("MCC.dll", EntryPoint = "YK_set_pwm_onoff_duty", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_pwm_onoff_duty(UInt16 CardNo, UInt16 PwmNo, double fOnDuty, double fOffDuty);
        [DllImport("MCC.dll", EntryPoint = "YK_get_pwm_onoff_duty", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_pwm_onoff_duty(UInt16 CardNo, UInt16 PwmNo, ref double fOnDuty, ref double fOffDuty);
        [DllImport("MCC.dll", EntryPoint = "YK_conti_set_pwm_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_conti_set_pwm_output(UInt16 CardNo, UInt16 Crd, UInt16 pwm_no, double fDuty, double fFre);//连续插补中设置PWM输出
        [DllImport("MCC.dll", EntryPoint = "YK_conti_set_pwm_follow_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_conti_set_pwm_follow_speed(UInt16 CardNo, UInt16 Crd, UInt16 pwm_no, UInt16 mode, double MaxVel, double MaxValue, double OutValue);//PWM速度跟随
        [DllImport("MCC.dll", EntryPoint = "YK_conti_get_pwm_follow_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_conti_get_pwm_follow_speed(UInt16 CardNo, UInt16 Crd, UInt16 pwm_no, ref UInt16 mode, ref double MaxVel, ref double MaxValue, ref double OutValue);
        [DllImport("MCC.dll", EntryPoint = "YK_conti_delay_pwm_to_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_conti_delay_pwm_to_start(UInt16 UInt16, UInt16 Crd, UInt16 pwmno, UInt16 on_off, double delay_value, UInt16 delay_mode, double ReverseTime);//相对于轨迹段起点PWM滞后输出
        [DllImport("MCC.dll", EntryPoint = "YK_conti_ahead_pwm_to_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_conti_ahead_pwm_to_stop(UInt16 CardNo, UInt16 Crd, UInt16 pwmno, UInt16 on_off, double ahead_value, UInt16 ahead_mode, double ReverseTime);//相对轨迹段终点PWM提前输出
        [DllImport("MCC.dll", EntryPoint = "YK_conti_write_pwm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_conti_write_pwm(UInt16 CardNo, UInt16 Crd, UInt16 pwmno, UInt16 on_off, double ReverseTime);    //缓冲区立即PWM输出
        //圆弧限速-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_set_arc_limit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_arc_limit(UInt16 CardNo, UInt16 Crd, UInt16 Enable, double MaxCenAcc, double MaxArcError);
        [DllImport("MCC.dll", EntryPoint = "YK_get_arc_limit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_arc_limit(UInt16 CardNo, UInt16 Crd, ref UInt16 Enable, ref double MaxCenAcc, ref double MaxArcError);
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        [DllImport("MCC.dll", EntryPoint = "YK_set_dec_stop_dist", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_dec_stop_dist(UInt16 CardNo, UInt16 axis, Int32 dist);//设置减速停止距离
        [DllImport("MCC.dll", EntryPoint = "YK_get_dec_stop_dist", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_dec_stop_dist(UInt16 CardNo, UInt16 axis, ref Int32 dist);
        //MCC600S定制接线盒----------------------------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_set_da_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_da_enable(UInt16 CardNo, UInt16 enable);//开启DA输出
        [DllImport("MCC.dll", EntryPoint = "YK_get_da_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_da_enable(UInt16 CardNo, ref UInt16 enable);
        [DllImport("MCC.dll", EntryPoint = "YK_set_da_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_da_output(UInt16 CardNo, UInt16 channel, double Vout);//设置DA输出
        [DllImport("MCC.dll", EntryPoint = "YK_get_da_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_da_output(UInt16 CardNo, UInt16 channel, ref double Vout);
        //读取AD输入------------------------------------------------------------------------------------------------------------------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_get_ad_input", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_ad_input(UInt16 CardNo, UInt16 channel, ref double Vout);
        [DllImport("MCC.dll", EntryPoint = "YK_get_ad_inputEx", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_ad_inputEx(UInt16 CardNo, UInt16 channel, ref double Vout); //读电流

        //增加预览
        [DllImport("MCC.dll", EntryPoint = "YK_line_multicoor_track_preview", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_line_multicoor_track_preview(UInt16 CardNo, UInt16 Crd, UInt16 interp_aixs_num, UInt16[] axis_list, double[] trace_aim_pos_list, double[] trace_mid_pos_list, UInt16 pos_mode);
        [DllImport("MCC.dll", EntryPoint = "YK_line_multicoor_track_capture", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_line_multicoor_track_capture(UInt16 CardNo, UInt16 Crd, UInt16 interp_aixs_num, UInt16[] axis_list, int[] trace_aim_pos_time, int[] trace_mid_pos_time);


        [DllImport("MCC.dll", EntryPoint = "YK_pmove_track_preview", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_pmove_track_preview(UInt16 CardNo, UInt16 axis, Int32 tarce_aim_pos, Int32 tarce_mid_pos, UInt16 pos_mode);
        [DllImport("MCC.dll", EntryPoint = "YK_pmove_track_capture", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_pmove_track_capture(UInt16 CardNo, UInt16 axis, ref int tarce_aim_time, ref int tarce_mid_time);

        //增加输出端口-范围放大


        [DllImport("MCC.dll", EntryPoint = "YK_fpga_update_firmware", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_fpga_update_firmware(ushort CardNo, String FileName);

        [DllImport("MCC.dll", EntryPoint = "YK_pmove_soft_landing", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_pmove_soft_landing(UInt16 CardNo, UInt16 axis, double midPos, double targetPos, double startVel, double maxVel, double endVel, double tAcc, double tDec, UInt16 posiMode);

        [DllImport("MCC.dll", EntryPoint = "YK_pso_motion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_pso_motion(UInt16 CardNo, UInt16 psoEnable, UInt16 psoIO, double psoPos, UInt16 activeLevel, double psoTime);
        [DllImport("MCC.dll", EntryPoint = "YK_get_home_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_home_status(UInt16 CardNo, UInt16 axis, ref UInt16 homeStatus);

        [DllImport("MCC.dll", EntryPoint = "YK_set_emg_io_handle_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_emg_io_handle_mode(UInt16 CardNo, UInt16 axis, UInt16 bitno, UInt16 io_on_off, UInt16 io_map_emg_ctl);
        [DllImport("MCC.dll", EntryPoint = "YK_get_emg_io_handle_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_emg_io_handle_mode(UInt16 CardNo, UInt16 axis, ref UInt16 bitno, ref UInt16 io_on_off, ref UInt16 io_map_emg_ctl);
        [DllImport("MCC.dll", EntryPoint = "YK_set_emg_io_handle_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_emg_io_handle_enable(UInt16 CardNo, UInt16 axis, UInt16 enable);
        [DllImport("MCC.dll", EntryPoint = "YK_get_emg_io_handle_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_emg_io_handle_enable(UInt16 CardNo, UInt16 axis, ref UInt16 enable);

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
        //小线段前瞻函数+++++++++++++++++2018.06.14 add+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //设置坐标系参数，确立坐标系映射，建立坐标系
        [DllImport("MCC.dll", EntryPoint = "YK_buf_set_crd_prm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_crd_prm(ushort CardNo, ushort Crd, ref TCrdPrm pCrdPrm);
        //查询坐标系参数
        [DllImport("MCC.dll", EntryPoint = "YK_buf_get_crd_prm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_crd_prm(ushort CardNo, ushort Crd, ref TCrdPrm pCrdPrm);
        //向插补缓存区增加插补数据。 用于在使用前瞻时，调用该指令表示后续没有新的数据，将会一次性把前瞻缓存区的数据压入运动缓存区
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
        //缓存区指令，缓存区内延时设置指令
        [DllImport("MCC.dll", EntryPoint = "YK_buf_delay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_delay(ushort CardNo, ushort Crd, ushort delayTime);
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
        //查询插补缓存区剩余空间
        [DllImport("MCC.dll", EntryPoint = "YK_buf_crd_remain_space", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_remain_space(ushort CardNo, ushort Crd, ref Int32 pSpace);
        //清除插补缓存区内的插补数据(只清除缓冲区数据，不关闭坐标系)
        [DllImport("MCC.dll", EntryPoint = "YK_buf_crd_clear", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_clear(ushort CardNo, ushort Crd);
        //启动缓冲区插补运动  
        [DllImport("MCC.dll", EntryPoint = "YK_buf_crd_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_start(ushort CardNo, ushort Crd);
        //暂停缓冲区插补运动
        [DllImport("MCC.dll", EntryPoint = "YK_buf_crd_pause", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_pause(ushort CardNo, ushort Crd);
        //停止缓冲区插补运动
        [DllImport("MCC.dll", EntryPoint = "YK_buf_crd_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_stop(ushort CardNo, ushort Crd, ushort mode);
        //查询插补运动坐标系状态
        [DllImport("MCC.dll", EntryPoint = "YK_buf_crd_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_crd_status(ushort CardNo, ushort Crd, ref short pRun, ref Int32 pSegment);
        //设置自定义插补段段号
        [DllImport("MCC.dll", EntryPoint = "YK_buf_set_user_seg_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_set_user_seg_num(ushort CardNo, ushort Crd, Int32 segNum);
        //读取自定义插补段段号
        [DllImport("MCC.dll", EntryPoint = "YK_buf_get_user_seg_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_user_seg_num(ushort CardNo, ushort Crd, ref Int32 segNum);
        //读取未完成的插补段段数
        [DllImport("MCC.dll", EntryPoint = "YK_buf_get_remain_seg_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_remain_seg_num(ushort CardNo, ushort Crd, ref Int32 pSegment);
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


        //螺距补偿
        [DllImport("MCC.dll", EntryPoint = "YK_set_leadscrew_comp_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_leadscrew_comp_enable(ushort CardNo, ushort axis, ushort enable);
        [DllImport("MCC.dll", EntryPoint = "YK_get_leadscrew_comp_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_leadscrew_comp_enable(ushort CardNo, ushort axis, ref ushort enable);
        [DllImport("MCC.dll", EntryPoint = "YK_set_leadscrew_comp_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_leadscrew_comp_para(ushort CardNo, ushort axis, ushort Num, int StartPos, int LenPos, int[] PosList_P, int[] PosList_N);
        [DllImport("MCC.dll", EntryPoint = "YK_get_leadscrew_comp_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_leadscrew_comp_para(ushort CardNo, ushort axis, ref ushort Num, ref int StartPos, ref int LenPos, int[] PosList_P, int[] PosList_N);

        //mcc1600专用
        [DllImport("MCC.dll", EntryPoint = "YK_write_erc_pinEx", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_erc_pinEx(UInt16 CardNo, UInt16 axis, UInt16 sel);
        [DllImport("MCC.dll", EntryPoint = "YK_read_erc_pinEx", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_erc_pinEx(UInt16 CardNo, UInt16 axis);
        [DllImport("MCC.dll", EntryPoint = "YK_write_sevon_pinEx", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_sevon_pinEx(UInt16 CardNo, UInt16 axis, UInt16 on_off);
        [DllImport("MCC.dll", EntryPoint = "YK_read_sevon_pinEx", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_sevon_pinEx(UInt16 CardNo, UInt16 axis);

        [DllImport("MCC.dll", EntryPoint = "YK_board_reset_one_card", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_board_reset_one_card(UInt16 CardNo);
        [DllImport("MCC.dll", EntryPoint = "YK_write_outbitEx", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_write_outbitEx(UInt16 CardNo, UInt16 bitno, UInt16 on_off);
        [DllImport("MCC.dll", EntryPoint = "YK_read_outbitEx", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_outbitEx(UInt16 CardNo, UInt16 bitno);




        [DllImport("MCC.dll", EntryPoint = "YK_set_pwm_io", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_pwm_io(UInt16 CardNo, UInt16 pwm_no, UInt16 pwm_io);
        [DllImport("MCC.dll", EntryPoint = "YK_get_pwm_io", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_pwm_io(UInt16 CardNo, UInt16 pwm_no, ref UInt16 pwm_io);



        [DllImport("MCC.dll", EntryPoint = "YK_set_assist_encoder_reverse", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_assist_encoder_reverse(UInt16 CardNo, UInt16 axis, UInt16 enable);
        [DllImport("MCC.dll", EntryPoint = "YK_get_assist_encoder_reverse", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_assist_encoder_reverse(UInt16 CardNo, UInt16 axis, ref UInt16 enable);



        [DllImport("MCC.dll", EntryPoint = "YK_set_follow_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_follow_enable(UInt16 CardNo, UInt16 slave_axis, UInt16 enable);//跟随使能设置
        [DllImport("MCC.dll", EntryPoint = "YK_get_follow_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_follow_enable(UInt16 CardNo, UInt16 slave_axis, ref UInt16 enable);//跟随使能读取
        [DllImport("MCC.dll", EntryPoint = "YK_set_follow_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_follow_move(UInt16 CardNo, UInt16 master_axis, UInt16 slave_axis, double maseter_pos_start, double master_pos_end, double followradio, UInt16 if_revers, UInt16 if_encoder);
        [DllImport("MCC.dll", EntryPoint = "YK_get_follow_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_follow_move(UInt16 CardNo, UInt16 master_axis, UInt16 slave_axis, ref double maseter_pos_start, ref double master_pos_end, ref double followradio, ref UInt16 if_revers, ref UInt16 if_encoder);

        //扩展 

        [DllImport("MCC.dll", EntryPoint = "YK_set_localcat_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_localcat_state(UInt16 CardNo, UInt16 LocalcatNo, UInt16 state);
        [DllImport("MCC.dll", EntryPoint = "YK_get_localcat_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_localcat_state(UInt16 CardNo, UInt16 LocalcatNo, ref UInt16 state);
        [DllImport("MCC.dll", EntryPoint = "YK_get_localcat_module_msg", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_get_localcat_module_msg(UInt16 CardNo, UInt16 LocalcatNo, ref UInt16 ModuleNum, byte[,] ModuleNameList, ushort[] ModuleTypeList, ushort[] ModulePortNumList);
        public static extern short YK_get_localcat_module_msg(UInt16 CardNo, UInt16 LocalcatNo, ref UInt16 ModuleNum, IntPtr[] ModuleNameList, ushort[] ModuleTypeList, ushort[] ModulePortNumList);//获取localcat模块信息
        [DllImport("MCC.dll", EntryPoint = "YK_get_localcat_total_adcnum", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_localcat_total_adcnum(UInt16 CardNo, ref UInt16 TotalIn, ref UInt16 TotalOut);//读取 拓展的AD/DA输入输出通道数
        [DllImport("MCC.dll", EntryPoint = "YK_get_localcat_total_ionum", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_localcat_total_ionum(UInt16 CardNo, ref UInt16 TotalIn, ref UInt16 TotalOut); //读取  拓展的IO 输入输出口数
        //扩展AD/DA
        [DllImport("MCC.dll", EntryPoint = "YK_set_localcat_da_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_localcat_da_output(UInt16 CardNo, UInt16 channel, Int16 Value);//设置拓展的DA参数	
        [DllImport("MCC.dll", EntryPoint = "YK_get_localcat_da_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_localcat_da_output(UInt16 CardNo, UInt16 channel, ref Int16 Value);//读取拓展的DA参数	
        [DllImport("MCC.dll", EntryPoint = "YK_get_localcat_ad_input", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_localcat_ad_input(UInt16 CardNo, UInt16 channel, ref Int16 Value);
        [DllImport("MCC.dll", EntryPoint = "YK_set_localcat_ad_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_localcat_ad_mode(UInt16 CardNo, UInt16 channel, UInt16 mode);//配置拓展的AD模式
        [DllImport("MCC.dll", EntryPoint = "YK_get_localcat_ad_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_localcat_ad_mode(UInt16 CardNo, UInt16 channel, ref UInt16 mode);//配置拓展的AD模式
        [DllImport("MCC.dll", EntryPoint = "YK_set_localcat_da_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_localcat_da_mode(UInt16 CardNo, UInt16 channel, UInt16 mode);//配置拓展的DA模式
        [DllImport("MCC.dll", EntryPoint = "YK_get_localcat_da_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_localcat_da_mode(UInt16 CardNo, UInt16 channel, ref UInt16 mode);

        //扩展通用IO读写	    
        [DllImport("MCC.dll", EntryPoint = "YK_localcat_io_read_inbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_localcat_io_read_inbit(UInt16 CardNo, UInt16 bitno, ref UInt16 on_off);
        [DllImport("MCC.dll", EntryPoint = "YK_localcat_io_write_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_localcat_io_write_outbit(UInt16 CardNo, UInt16 bitno, UInt16 on_off);//设置拓展的输出口的状态  
        [DllImport("MCC.dll", EntryPoint = "YK_localcat_io_read_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_localcat_io_read_outbit(UInt16 CardNo, UInt16 bitno, ref UInt16 on_off);//读取拓展的输出口的状态    

        [DllImport("MCC.dll", EntryPoint = "YK_localcat_io_read_inbyte", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_localcat_io_read_inbyte(UInt16 CardNo, UInt16 StartByte, UInt16 ByteNum, byte[] ValueList);
        [DllImport("MCC.dll", EntryPoint = "YK_localcat_io_read_outbyte", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_localcat_io_read_outbyte(UInt16 CardNo, UInt16 StartByte, UInt16 ByteNum, byte[] ValueList);//设置拓展的输出端口的值  
        [DllImport("MCC.dll", EntryPoint = "YK_localcat_io_write_outbyte", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_localcat_io_write_outbyte(UInt16 CardNo, UInt16 StartByte, UInt16 ByteNum, byte[] ValueList);//设置拓展的输出端口的值  

        //[DllImport("MCC.dll", EntryPoint = "YK_set_area_soft_limit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_set_area_soft_limit(UInt16 CardNo, UInt16 index, UInt16 enable, UInt16 axis0, UInt16 axis1, UInt16 if_encoder, int center0, int center1, int limit_radius);//区域软件限位功能参数设置;
        //[DllImport("MCC.dll", EntryPoint = "YK_get_area_soft_limit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_get_area_soft_limit(UInt16 CardNo, UInt16 index, ref UInt16 enable, ref UInt16 axis0, ref UInt16 axis1, ref UInt16 if_encoder, ref int center0, ref int center1, ref int limit_radius);//区域软件限位功能参数读取;
        //[DllImport("MCC.dll", EntryPoint = "YK_pmotion_sync", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_pmotion_sync(UInt16 CardNo, UInt16 axis_num, UInt16[] axis_list, int[] aim_pos, UInt16 posi_mode);//同步运行单轴运动;
        [DllImport("MCC.dll", EntryPoint = "YK_pmotion_soft_landing", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_pmotion_soft_landing(UInt16 CardNo, UInt16 axis, double aim_pos, double target_pos, double acc_time, double dec_time, double start_vel, double max_vel, double mid_vel, double stop_vel, UInt16 pos_mode);//PMotion软着陆

        [DllImport("MCC.dll", EntryPoint = "YK_pmotion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_pmotion(UInt16 CardNo, UInt16 axis, double min_vel, double max_vel, double tacc, double tdec, double stop_vel, double s_tacc, double s_tdec, int dist, UInt16 posi_mode);//PMotion软着陆
        //add 20240521
        [DllImport("MCC.dll", EntryPoint = "YK_set_axis_high_compare_follow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_axis_high_compare_follow(ushort CardNo, ushort enable, ushort main_channel, ushort follow_channel, long follow_delay);//增加一维比较跟随输出
        [DllImport("MCC.dll", EntryPoint = "YK_buf_check_done", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_check_done(ushort CardNo, ushort buf_id, ushort axis); //指令缓冲阻塞检查轴运动指令;
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
        //
        [DllImport("MCC.dll", EntryPoint = "YK_set_adc_cmp_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_adc_cmp_para(ushort CardNo, ushort cmp_no, ushort enable, ushort channel1, ushort channel2, ushort mode, ushort io_num, ushort io_evel);//设置模拟量比较模式参数
        [DllImport("MCC.dll", EntryPoint = "YK_get_adc_cmp_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_adc_cmp_para(ushort CardNo, ushort cmp_no, ref ushort enable, ref ushort channel1, ref ushort channel2, ref ushort mode, ref ushort io_num, ref ushort io_evel);//获取模拟量比较模式参数
        [DllImport("MCC.dll", EntryPoint = "YK_buf_stage_choke", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_stage_choke(ushort CardNo, ushort buf_id, ushort checked_buf_id, ushort stage_num);
        [DllImport("MCC.dll", EntryPoint = "YK_set_area_soft_limit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_area_soft_limit(ushort CardNo, ushort index, ushort enable, ushort axis0, ushort axis1, ushort if_encoder, int center0, int center1, int limit_radius);
        [DllImport("MCC.dll", EntryPoint = "YK_get_area_soft_limit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_area_soft_limit(ushort CardNo, ushort index, ref ushort enable, ref ushort axis0, ref ushort axis1, ref ushort if_encoder, ref int center0, ref int center1, ref int limit_radius);
        [DllImport("MCC.dll", EntryPoint = "YK_pmotion_sync", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_pmotion_sync(ushort CardNo, ushort axis_num, ushort[] axis_list, int[] aim_pos, ushort posi_mode);
        [DllImport("MCC.dll", EntryPoint = "YK_buf_counter_clear", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_counter_clear(ushort CardNo, ushort buf_id);
        [DllImport("MCC.dll", EntryPoint = "YK_buf_get_counter_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_get_counter_time(ushort CardNo, ref int delay_time);


        //-------------------------add 20240531 -------
        //数据采集
        [DllImport("MCC.dll", EntryPoint = "YK_start_posvel_data_capture", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_start_posvel_data_capture(UInt16 CardNo, UInt16 axis_num, UInt16[] axis_List);
        [DllImport("MCC.dll", EntryPoint = "YK_stop_posvel_data_capture", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_stop_posvel_data_capture(UInt16 CardNo);
        [DllImport("MCC.dll", EntryPoint = "YK_upload_posvel_data", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_upload_posvel_data(UInt16 CardNo, UInt16 axis, UInt16 data_len, int[] rd_data, ref int eff_data, ref UInt16 upload_flag);
        //--------------------------add 20240602---------
        [DllImport("MCC.dll", EntryPoint = "YK_set_pwm_dir", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_pwm_dir(ushort CardNo, ushort io_dir, ushort dir);

        [DllImport("MCC.dll", EntryPoint = "YK_buf_pmove_paramer_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_pmove_paramer_ex(ushort CardNo, ushort crd_no, ushort axis, double start_vel, double max_vel, double end_vel, double tacc, double tdec, double s_time, double targpos, ushort posi_mode);
        [DllImport("MCC.dll", EntryPoint = "YK_buf_axis_check_done", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_buf_axis_check_done(ushort CardNo, ushort crd_no, ushort axis_num, ushort[] check_axis_list);//指令缓冲阻塞检查轴运动指令


        //--------------------------add 20240722---------------------------------------------
        [DllImport("MCC.dll", EntryPoint = "YK_set_continuous_linear_interpolation_motion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_continuous_linear_interpolation_motion(ushort CardNo, ushort crd_no, ushort axis_num, ushort[] axis_list, double start_vel, double max_vel, double end_vel, double tacc, double tdec, double smooth_time, double[] targpos, ushort posi_mode);//设置插补连续运动
        [DllImport("MCC.dll", EntryPoint = "YK_get_continuous_linear_interpolation_motion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_continuous_linear_interpolation_motion(ushort CardNo, ushort crd_no, ref ushort axis_num, ref double start_vel, ref double max_vel, ref  double end_vel, ref double tacc, ref double tdec, ref double smooth_time, ref ushort posi_mode);//读取插补连续运动
        [DllImport("MCC.dll", EntryPoint = "YK_pmove_soft_starting", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_pmove_soft_starting(ushort CardNo, ushort axis, double aim_pos, double mid_pos, double acc_time, double dec_time, double start_vel, double start_low_vel, double max_vel, double stop_vel, ushort pos_mode);//PMove软启动

        [DllImport("MCC.dll", EntryPoint = "YK_set_encoder_dir", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_encoder_dir(UInt16 CardNo, UInt16 axis, UInt16 dir);
        [DllImport("MCC.dll", EntryPoint = "YK_get_encoder_dir", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_encoder_dir(UInt16 CardNo, UInt16 axis, ref UInt16 dir);
        //--------------------------add 20240912---
        [DllImport("MCC.dll", EntryPoint = "YK_set_pwm_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_pwm_para(UInt16 CardNo, UInt16 pwm_no, double fDuty, double fFre, int pwm_time);//设置PWM输出
        [DllImport("MCC.dll", EntryPoint = "YK_get_pwm_para", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_pwm_para(UInt16 CardNo, UInt16 pwm_no, ref double fDuty, ref double fFre, ref int pwm_time);//
        //[DllImport("MCC.dll", EntryPoint = "YK_set_pwm_io_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_set_pwm_io_enable(UInt16 CardNo, UInt16 pwm_no, Int32 pwm_io);//
        //[DllImport("MCC.dll", EntryPoint = "YK_get_pwm_io_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern short YK_get_pwm_io_enable(UInt16 CardNo, UInt16 pwm_no, ref Int32 pwm_io);//
        [DllImport("MCC.dll", EntryPoint = "YK_set_pwm_io_onoff", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_pwm_io_onoff(UInt16 CardNo, UInt16 pwm_no, Int32 pwm_io);//
        [DllImport("MCC.dll", EntryPoint = "YK_get_pwm_io_onoff", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_pwm_io_onoff(UInt16 CardNo, UInt16 pwm_no, ref Int32 pwm_io);//
        //---------------------------add 20240918----
        [DllImport("MCC.dll", EntryPoint = "YK_set_pwm_one_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_set_pwm_one_enable(UInt16 CardNo, UInt16 pwm_channel, UInt16 enable); //设置PWM输出单个使能
        [DllImport("MCC.dll", EntryPoint = "YK_get_pwm_one_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_pwm_one_enable(UInt16 CardNo, UInt16 pwm_channel, ref UInt16 enable); //设置PWM输出单个使能

  
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
