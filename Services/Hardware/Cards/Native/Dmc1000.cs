﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/DLL/Dmc1000.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Samsun.Domain.MotionCard.Common
{
    public class Dmc1000
    {
        //////////////////初始化函数////////////////////
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_board_init", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_board_init();
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_board_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_board_close();

        //////////////////脉冲输出设置函数//////////////
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_set_pls_outmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_set_pls_outmode(int axis,int pls_outmode);

        //////////////////速度设置函数//////////////
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_get_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_get_speed(int axis);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_change_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_change_speed(int axis, int NewVel);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_decel_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_decel_stop(int axis);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_immediate_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_immediate_stop(int axis);

        //////////////////单轴位置模式函数/////////////
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_start_t_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_start_t_move(int axis, int Dist, int StrVel, int MaxVel, double Tacc);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_start_ta_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_start_ta_move(int axis, int Pos, int StrVel, int MaxVel, double Tacc);

        [DllImport("Dmc1000.dll", EntryPoint = "d1000_start_s_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_start_s_move(int axis, int Dist, int StrVel, int MaxVel, double Tacc);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_start_sa_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_start_sa_move(int axis, int Pos, int StrVel, int MaxVel, double Tacc);

        [DllImport("Dmc1000.dll", EntryPoint = "d1000_start_tv_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_start_tv_move(int axis, int StrVel, int MaxVel, double Tacc);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_start_sv_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_start_sv_move(int axis, int StrVel, int MaxVel, double Tacc);

        [DllImport("Dmc1000.dll", EntryPoint = "d1000_set_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_set_s_profile(int axis, double s_para);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_get_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int  d1000_get_s_profile(int axis,ref double s_para);

        //////////////////线性插补函数//////////////////
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_start_t_line", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_start_t_line(int TotalAxis, UInt16[] AxisArray, int[] DistArray, int StrVel, int MaxVel, double Tacc);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_start_ta_line", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_start_ta_line(int TotalAxis, UInt16[] AxisArray, int[] DistArray, int StrVel, int MaxVel, double Tacc);

        //////////////////回原点函数////////////////////
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_home_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_home_move(int axis, int StrVel, int MaxVel, double Tacc);

        //////////////////运动状态检测函数//////////////
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_check_done", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_check_done(int axis);

        //////////////////位置设定和读取函数////////////
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_get_command_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_get_command_pos(int axis);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_set_command_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_set_command_pos(int axis,double Pos);

        //////////////////通用I/O函数///////////////////
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_out_bit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_out_bit(int BitNo, int BitData);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_in_bit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_in_bit(int BitNo);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_get_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_get_outbit(int BitNo);
        // [DllImport("Dmc1000.dll", EntryPoint = "d1000_in_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        // public static extern int d1000_in_enable(int CardNo, int InputEn);

        [DllImport("Dmc1000.dll", EntryPoint = "d1000_in_port", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_in_port(int cardno);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_get_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_get_outport(int cardno);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_out_port", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_out_port(int cardno, int PortData);

        //////////////////专用I/O接口函数///////////////
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_set_sd", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_set_sd(int axis, int SdMode);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_get_axis_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_get_axis_status(int axis);     

       // [DllImport("Dmc1000.dll", EntryPoint = "d1000_WriteDWord", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
       // public static extern int d1000_WriteDWord(int addr, int data);
       // [DllImport("Dmc1000.dll", EntryPoint = "d1000_ReadDWord", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
       // public static extern int d1000_ReadDWord(int addr);


        [DllImport("Dmc1000.dll", EntryPoint = "d1000_set_HOME_pin_logic", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_set_HOME_pin_logic(int axis,int org_logic,int filter);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_config_home_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_config_home_mode(int axis,int mode,int EZ_count);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_set_home_el_return", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_set_home_el_return(int axis,int enable);

        [DllImport("Dmc1000.dll", EntryPoint = "d1000_get_target_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_get_target_position(int axis);

        [DllImport("Dmc1000.dll", EntryPoint = "d1000_get_stop_reason", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_get_stop_reason(int axis,ref long stopreason);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_clear_stop_reason", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_clear_stop_reason(int axis);

        [DllImport("Dmc1000.dll", EntryPoint = "d1000_check_done_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_check_done_multicoor(int cardno,int crd);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_stop_multicoor", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_stop_multicoor(int CardNo,int Crd,int stop_mode);

        [DllImport("Dmc1000.dll", EntryPoint = "d1000_reset_target_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_reset_target_position(int axis,long dist);

        [DllImport("Dmc1000.dll", EntryPoint = "d1000_set_ALM_PIN_Extern", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_set_ALM_PIN_Extern(int axis,int alm_enable,int alm_logic,int alm_all,int alm_action);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_read_ALM_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_read_ALM_PIN(int axis);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_config_ALM_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_config_ALM_PIN(int axis, int alm_logic, int alm_action);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_set_ALM_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_set_ALM_PIN(int axis, int alm_logic, int alm_action);

        [DllImport("Dmc1000.dll", EntryPoint = "d1000_config_EL_MODE", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_config_EL_MODE(int axis,int el_mode);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_Enable_EL_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_Enable_EL_PIN(int axis, int enable);

        [DllImport("Dmc1000.dll", EntryPoint = "d1000_in_bit_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_in_bit_ex(int CardNo,int BitNo);

        [DllImport("Dmc1000.dll", EntryPoint = "d1000_config_softlimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_config_softlimit(int axis, int enable, int source_sel, int SL_action, long N_limit, long P_limit);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_get_config_softlimit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_get_config_softlimit(int axis,ref int enable,ref  int source_sel,ref int SL_action,ref  long N_limit,ref long P_limit);

        [DllImport("Dmc1000.dll", EntryPoint = "d1000_counter_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_counter_config(int axis,int mode);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_get_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_get_encoder(int axis);
        [DllImport("Dmc1000.dll", EntryPoint = "d1000_set_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int d1000_set_encoder(int axis,long encoder_value);
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
