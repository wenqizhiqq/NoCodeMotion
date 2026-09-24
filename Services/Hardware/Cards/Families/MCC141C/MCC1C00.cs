﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/MCC141C/MCC1C00.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace csMCC1C00
{
    public class MCC1C00
    {
        [DllImport("MCC1C00.dll", EntryPoint = "YK_board_init", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_board_init();

        [DllImport("MCC1C00.dll", EntryPoint = "YK_board_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_board_close();

        [DllImport("MCC1C00.dll",EntryPoint = "YK_get_base_addr", CharSet = CharSet.Ansi,CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_get_base_addr(short CardNo);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_get_irq_channel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_get_irq_channel(short CardNo);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_set_pls_outmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_set_pls_outmode(short CardNo,short  axis, short pls_outmode);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_get_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_get_speed(short CardNo,short  axis);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_change_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_change_speed(short CardNo,short  axis, int NewVel);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_decel_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_decel_stop(short CardNo,short  axis);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_immediate_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_immediate_stop(short CardNo,short  axis);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_start_t_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_start_t_move(short CardNo,short  axis, int Dist, int StrVel, int MaxVel, double Tacc);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_start_ta_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_start_ta_move(short CardNo,short  axis, int Pos, int StrVel, int MaxVel, double Tacc);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_start_s_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_start_s_move(short CardNo,short  axis, int Dist, int StrVel, int MaxVel, double Tacc);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_start_sa_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_start_sa_move(short CardNo,short  axis, int Pos, int StrVel, int MaxVel, double Tacc);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_start_tv_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_start_tv_move(short CardNo,short  axis, int StrVel, int MaxVel, double Tacc);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_start_sv_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_start_sv_move(short CardNo,short  axis, int StrVel, int MaxVel, double Tacc);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_start_t_line", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_start_t_line(short CardNo,short  TotalAxis, ref short[] AxisArray, ref int[] DistArray, int StrVel, int MaxVel, double Tacc);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_start_ta_line", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_start_ta_line(short CardNo,short  TotalAxis, ref short[] AxisArray, ref int[] DistArray, int StrVel, int MaxVel, double Tacc);

        [DllImport("MCC1C00.dll",EntryPoint = "YK_start_t_arc",CharSet = CharSet.Ansi,CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_start_t_arc(ref short AxisArray, int OffsetC1, int OffsetC2, double Angle, int StrVel, int MaxVel, double Tacc);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_home_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_home_move(short CardNo,short  axis, int StrVel, int MaxVel, double Tacc);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_check_done", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_check_done(short CardNo,short  axis);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_get_command_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_get_command_pos(short CardNo,short  axis);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_set_command_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_set_command_pos(short CardNo,short  axis, double mPos);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_out_bit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_out_bit(short CardNo,short  BitNo, short BitData);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_in_bit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_in_bit(short CardNo,short  BitNo);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_get_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_get_outbit(short CardNo,short  BitNo);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_set_sd", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_set_sd(short CardNo,short  axis, short SdMode);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_get_axis_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_get_axis_status(short CardNo,short  axis);

        //新增函数
        //新增函数


        [DllImport("MCC1C00.dll", EntryPoint = "YK_set_compare_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_set_compare_pos(short CardNo,short  axis, double cmpPos);
        [DllImport("MCC1C00.dll", EntryPoint = "YK_get_compare_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_get_compare_pos(short CardNo,short  axis,ref double cmpPos);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_set_compare_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_set_compare_config(short CardNo,short  axis,short cmpLogic,short outBit);
        [DllImport("MCC1C00.dll", EntryPoint = "YK_get_compare_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_get_compare_config(short CardNo,short  axis,ref short cmpLogic,ref short outBit);
        [DllImport("MCC1C00.dll", EntryPoint = "YK_set_compare_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_set_compare_enable(short CardNo,short  axis, short enable);
        [DllImport("MCC1C00.dll", EntryPoint = "YK_get_compare_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_get_compare_enable(short CardNo,short  axis,ref short enable);
        [DllImport("MCC1C00.dll", EntryPoint = "YK_clear_compare_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_clear_compare_pos(short CardNo,short  axis);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_write_sevon_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_write_sevon_pin(short CardNo,short  axis, short sevon_en);//输出SEVON信号
        [DllImport("MCC1C00.dll", EntryPoint = "YK_read_sevon_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_sevon_pin(short CardNo,short  axis);
        [DllImport("MCC1C00.dll", EntryPoint = "YK_set_alm_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_set_alm_mode(short CardNo,short  axis, short enable, short alm_logic, short alm_action);
        [DllImport("MCC1C00.dll", EntryPoint = "YK_get_alm_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_get_alm_mode(short CardNo, short axis, ref ushort enable, ref ushort alm_logic, ref ushort alm_action);
        [DllImport("MCC1C00.dll", EntryPoint = "YK_write_erc_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_write_erc_pin(short CardNo,short  axis, short erc_logic);//输出ERC信号
        [DllImport("MCC1C00.dll", EntryPoint = "YK_read_erc_pin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_read_erc_pin(short CardNo,short  axis);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_set_encoder_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_set_encoder_inmode(short CardNo,short  axis, short mode);
        [DllImport("MCC1C00.dll", EntryPoint = "YK_get_encoder_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_get_encoder_inmode(short CardNo,short  axis, ref short mode);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_set_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_set_encoder(short CardNo,short  axis, double encoder_value);
        [DllImport("MCC1C00.dll", EntryPoint = "YK_get_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_get_encoder(short CardNo,short  axis);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_get_alm_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_get_alm_status(short CardNo,short  axis);

        [DllImport("MCC1C00.dll", EntryPoint = "YK_download_configfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_download_configfile(UInt16 CardNo, String FileName);
       
        [DllImport("MCC1C00.dll", EntryPoint = "YK_get_card_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_card_version(UInt16 CardNo, ref uint CardVersion);
        [DllImport("MCC1C00.dll", EntryPoint = "YK_get_card_lib_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_card_lib_version(UInt16 CardNo, ref uint LibVer);
        [DllImport("MCC1C00.dll", EntryPoint = "YK_get_card_type", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern short YK_get_card_type(UInt16 CardNo, ref uint CardType);

        //[DllImport("MCC1C00.dll", EntryPoint = "YK_get_pls_outmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern int YK_get_pls_outmode(short CardNo, short axis, ref short pls_outmode);
        //[DllImport("MCC1C00.dll", EntryPoint = "YK_get_sd", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern int YK_get_sd(short CardNo, short axis,ref short SdMode);
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
