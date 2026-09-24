﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/DMC2210/dmc2210api.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
 

namespace WenQiZhi.Domain.MotionCard.Common.DMC2210
{
    public class Dmc2210
    {
       
        /// <summary>
        /// 取板卡函数的返回值的释义
        /// </summary>
        /// <param name="ErrNum"></param>
        /// <returns></returns>
        public string GetErrorInfo(int ErrNum)
        {
            return "";
        }

        internal static object objLock = new object();
        //---------------------   板卡初始和配置函数  ----------------------
        /********************************************************************************
        ** 函数名称: d2410_board_init
        ** 功能描述: 控制板初始化，设置初始化和速度等设置
        ** 输　  入: 无
        ** 返 回 值: 0：无卡； 1-8：成功(实际卡数) 
        **           1001 + j: j号卡初始化出错 从1001开始。
        ** 修    改:  
        ** 修改日期: 
        *********************************************************************************/
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_board_init", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt16 d2210_board_init();

        /********************************************************************************
        ** 函数名称: d2410_board_close
        ** 功能描述: 关闭所有卡
        ** 输　  入: 无
        ** 返 回 值: 无
        ** 日    期: 
        *********************************************************************************/
        public static void d2210_board_close()
        {
            lock (objLock)
            {
                board_close();
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_board_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void board_close();

        /********************************************************************************
        ** 函数名称: 控制卡复位
        ** 功能描述: 复位所有卡，只能在初始化完成之后调用．
        ** 输　  入: 无
        ** 返 回 值: 无
        ** 日    期: 
        *********************************************************************************/
        public static void d2210_board_rest()
        {
            lock (objLock)
            {
                board_rest();
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_board_rest", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void board_rest();




        //脉冲输入输出配置
        /********************************************************************************
        ** 函数名称: d2410_set_pulse_outmode
        ** 功能描述: 脉冲输出方式的设置
        ** 输　  入: axis - (0 - 3), outmode: 0 - 7
        **           6:正交脉冲，A相超前; 7:正交脉冲，B相超前
        ** 返 回 值: 无 
        ** 修改日期：2007.1.27
        *********************************************************************************/
        public static void d2210_set_pulse_outmode(ushort axis, ushort outmode)
        {
            lock (objLock)
            {
                set_pulse_outmode(axis,outmode);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_set_pulse_outmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void set_pulse_outmode(UInt16 axis, UInt16 outmode);

        //专用信号设置函数
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_config_SD_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_config_SD_PIN(UInt16 axis, UInt16 enable, UInt16 sd_logic, UInt16 sd_mode);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_config_PCS_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_config_PCS_PIN(UInt16 axis, UInt16 enable, UInt16 pcs_logic);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_config_INP_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_config_INP_PIN(UInt16 axis, UInt16 enable, UInt16 inp_logic);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_config_ERC_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_config_ERC_PIN(UInt16 axis, UInt16 enable, UInt16 erc_logic, UInt16 erc_width, UInt16 erc_off_time);

        [DllImport("Dmc2210.dll", EntryPoint = "d2210_config_ALM_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_config_ALM_PIN(UInt16 axis, UInt16 alm_logic, UInt16 alm_action);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_config_EL_MODE", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_config_EL_MODE(UInt16 axis, UInt16 el_mode);

        public static void d2210_set_HOME_pin_logic(UInt16 axis, UInt16 org_logic, UInt16 filter)
        {
            lock (objLock)
            {
                set_HOME_pin_logic(axis, org_logic, filter);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_set_HOME_pin_logic", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void set_HOME_pin_logic(UInt16 axis, UInt16 org_logic, UInt16 filter);

        public static void d2210_write_SEVON_PIN(ushort axis, ushort on_off)
        {
            lock (objLock)
            {
                write_SEVON_PIN(axis, on_off);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_write_SEVON_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void write_SEVON_PIN(UInt16 axis, UInt16 on_off);


        public static Int32 d2210_read_SEVON_PIN(UInt16 axis)
        {
            lock (objLock)
            {
                return read_SEVON_PIN(axis);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_read_SEVON_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern Int32 read_SEVON_PIN(UInt16 axis);

        [DllImport("Dmc2210.dll", EntryPoint = "d2210_write_ERC_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_write_ERC_PIN(UInt16 axis, UInt16 sel);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_read_RDY_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 d2210_read_RDY_PIN(UInt16 axis);

        //通用输入/输出控制函数

        public static Int32 d2210_read_inbit(UInt16 cardno, UInt16 bitno)
        {
            lock (objLock)
            {
                return read_inbit(cardno, bitno);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_read_inbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern Int32 read_inbit(UInt16 cardno, UInt16 bitno);


        public static void d2210_write_outbit(UInt16 cardno, UInt16 bitno, UInt16 on_off)
        {
            lock (objLock)
            {
                write_outbit(cardno, bitno, on_off);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_write_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void write_outbit(UInt16 cardno, UInt16 bitno, UInt16 on_off);


        public static Int32 d2210_read_outbit(UInt16 cardno, UInt16 bitno)
        {
            lock (objLock)
            {
                return read_outbit(cardno, bitno);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_read_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern Int32 read_outbit(UInt16 cardno, UInt16 bitno);

        public static Int32 d2210_read_inport(UInt16 cardno)
        {
            lock (objLock)
            {
                return read_inport(cardno);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_read_inport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern Int32 read_inport(UInt16 cardno);


        public static Int32 d2210_read_outport(UInt16 cardno)
        {
            lock (objLock)
            {
                return read_outport(cardno);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_read_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern Int32 read_outport(UInt16 cardno);

        public static void d2210_write_outport(UInt16 cardno, UInt32 port_value)
        {
            lock (objLock)
            {
                write_outport(cardno, port_value);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_write_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void write_outport(UInt16 cardno, UInt32 port_value);

        //制动函数
        public static void d2210_decel_stop(UInt16 axis, double Tdec)
        {
            lock (objLock)
            {
                decel_stop(axis, Tdec);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_decel_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void decel_stop(UInt16 axis, double Tdec);

        public static void d2210_imd_stop(UInt16 axis)
        {
            lock (objLock)
            {
                imd_stop(axis);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_imd_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void imd_stop(UInt16 axis);


        public static void d2210_emg_stop()
        {
            lock (objLock)
            {
                emg_stop();
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_emg_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void emg_stop();

        public static void d2210_simultaneous_stop(UInt16 axis)
        {
            lock (objLock)
            {
                simultaneous_stop(axis);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_simultaneous_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void simultaneous_stop(UInt16 axis);

        //位置设置和读取函数
        public static Int32 d2210_get_position(UInt16 axis)
        {
            lock (objLock)
            {
                return get_position(axis);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_get_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern Int32 get_position(UInt16 axis);

        public static void d2210_set_position(UInt16 axis, Int32 current_position)
        {
            lock (objLock)
            {
                set_position(axis, current_position);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_set_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void set_position(UInt16 axis, Int32 current_position);

        //状态检测函数
        public static UInt16 d2210_check_done(UInt16 axis)
        {
            lock (objLock)
            {
                return check_done(axis);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_check_done", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern UInt16 check_done(UInt16 axis);

        public static UInt16 d2210_prebuff_status(UInt16 axis)
        {
            lock (objLock)
            {
                return prebuff_status(axis);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_prebuff_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern UInt16 prebuff_status(UInt16 axis);

        public static UInt16 d2210_axis_io_status(UInt16 axis)
        {
            lock (objLock)
            {
                return axis_io_status(axis);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_axis_io_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern UInt16 axis_io_status(UInt16 axis);

        public static UInt16 d2210_axis_status(UInt16 axis)
        {
            lock (objLock)
            {
                return axis_status(axis);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_axis_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern UInt16 axis_status(UInt16 axis);


        [DllImport("Dmc2210.dll", EntryPoint = "d2210_get_rsts", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 d2210_get_rsts(UInt16 axis);

        //速度设置
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_variety_speed_range", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_variety_speed_range(UInt16 axis, UInt16 chg_enable, double Max_Vel);

        public static double d2210_read_current_speed(UInt16 axis)
        {
            lock (objLock)
            {
                return read_current_speed(axis);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_read_current_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern double read_current_speed(UInt16 axis);

        public static void d2210_change_speed(UInt16 axis, double Curr_Vel)
        {
            lock (objLock)
            {
                change_speed(axis, Curr_Vel);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_change_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void change_speed(UInt16 axis, double Curr_Vel);

        [DllImport("Dmc2210.dll", EntryPoint = "d2210_set_vector_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_set_vector_profile(double Min_Vel, double Max_Vel, double Tacc, double Tdec);

        public static void d2210_set_profile(UInt16 axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec)
        {
            lock (objLock)
            {
                set_profile(axis, Min_Vel, Max_Vel, Tacc, Tdec);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_set_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void set_profile(UInt16 axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec);

        public static void d2210_set_s_profile(UInt16 axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec, Int32 Sacc, Int32 Sdec)
        {
            lock (objLock)
            {
                set_s_profile(axis, Min_Vel, Max_Vel, Tacc, Tdec, Sacc, Sdec);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_set_s_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void set_s_profile(UInt16 axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec, Int32 Sacc, Int32 Sdec);

        [DllImport("Dmc2210.dll", EntryPoint = "d2210_set_st_profile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_set_st_profile(UInt16 axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Tsacc, double Tsdec);

        [DllImport("Dmc2210.dll", EntryPoint = "d2210_reset_target_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_reset_target_position(UInt16 axis, Int32 dist);

        //单轴定长运动
        public static void d2210_t_pmove(UInt16 axis, Int32 Dist, UInt16 posi_mode)
        {
            lock (objLock)
            {
                t_pmove(axis, Dist, posi_mode);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_t_pmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void t_pmove(UInt16 axis, Int32 Dist, UInt16 posi_mode);

        public static void d2210_ex_t_pmove(UInt16 axis, Int32 Dist, UInt16 posi_mode)
        {
            lock (objLock)
            {
                ex_t_pmove(axis, Dist, posi_mode);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_ex_t_pmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void ex_t_pmove(UInt16 axis, Int32 Dist, UInt16 posi_mode);

        public static void d2210_s_pmove(UInt16 axis, Int32 Dist, UInt16 posi_mode)
        {
            lock (objLock)
            {
                s_pmove(axis, Dist, posi_mode);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_s_pmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void s_pmove(UInt16 axis, Int32 Dist, UInt16 posi_mode);

        public static void d2210_ex_s_pmove(UInt16 axis, Int32 Dist, UInt16 posi_mode)
        {
            lock (objLock)
            {
                ex_s_pmove(axis, Dist, posi_mode);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_ex_s_pmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void ex_s_pmove(UInt16 axis, Int32 Dist, UInt16 posi_mode);

        //单轴连续运动
        public static void d2210_s_vmove(UInt16 axis, UInt16 dir)
        {
            lock (objLock)
            {
                s_vmove(axis, dir);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_s_vmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void s_vmove(UInt16 axis, UInt16 dir);

        public static void d2210_t_vmove(UInt16 axis, UInt16 dir)
        {
            lock (objLock)
            {
                t_vmove(axis, dir);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_t_vmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void t_vmove(UInt16 axis, UInt16 dir);

        //线性插补
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_t_line2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_t_line2(UInt16 axis1, Int32 Dist1, UInt16 axis2, Int32 Dist2, UInt16 posi_mode);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_t_line3", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_t_line3(ref UInt16 axis, Int32 Dist1, Int32 Dist2, Int32 Dist3, UInt16 posi_mode);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_t_line4", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_t_line4(UInt16 cardno, Int32 Dist1, Int32 Dist2, Int32 Dist3, Int32 Dist4, UInt16 posi_mode);


        //手轮运动
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_set_handwheel_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_set_handwheel_inmode(UInt16 axis, UInt16 inmode, UInt16 count_dir);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_handwheel_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_handwheel_move(UInt16 axis, double vh);

        //找原点
        public static void d2210_config_home_mode(UInt16 axis, UInt16 mode, UInt16 EZ_count)
        {
            lock (objLock)
            {
                config_home_mode(axis, mode, EZ_count);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_config_home_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void config_home_mode(UInt16 axis, UInt16 mode, UInt16 EZ_count);

        public static void d2210_home_move(UInt16 axis, UInt16 home_mode, UInt16 vel_mode)
        {
            lock (objLock)
            {
                home_move(axis, home_mode, vel_mode);
            }
        }
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_home_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void home_move(UInt16 axis, UInt16 home_mode, UInt16 vel_mode);

        //圆弧插补
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_arc_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_arc_move(ref UInt16 axis, ref Int32 target_pos, ref Int32 cen_pos, UInt16 arc_dir);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_rel_arc_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_rel_arc_move(ref UInt16 axis, ref Int32 rel_pos, ref Int32 rel_cen, UInt16 arc_dir);

        //脉冲当量设置和椭圆插补, 脉冲闭环
        //[DllImport("Dmc2410.dll", EntryPoint = "d2410_get_equiv", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern Int32 d2410_get_equiv(UInt16 axis, ref double equiv);
        //[DllImport("Dmc2410.dll", EntryPoint = "d2410_set_equiv", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern Int32 d2410_set_equiv(UInt16 axis, double new_equiv);
        //[DllImport("Dmc2410.dll", EntryPoint = "d2410_get_position_unitmm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern Int32 d2410_get_position_unitmm(UInt16 axis, ref double pos_by_mm);
        //[DllImport("Dmc2410.dll", EntryPoint = "d2410_set_position_unitmm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern Int32 d2410_set_position_unitmm(UInt16 axis, double pos_by_mm);
        //[DllImport("Dmc2410.dll", EntryPoint = "d2410_read_current_speed_unitmm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern Int32 d2410_read_current_speed_unitmm(UInt16 axis, ref double current_speed);
        //[DllImport("Dmc2410.dll", EntryPoint = "d2410_get_encoder_unitmm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern Int32 d2410_get_encoder_unitmm(UInt16 axis, ref double encoder_pos_by_mm);
        //[DllImport("Dmc2410.dll", EntryPoint = "d2410_set_encoder_unitmm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern Int32 d2410_set_encoder_unitmm(UInt16 axis, double encoder_pos_by_mm);
        //[DllImport("Dmc2410.dll", EntryPoint = "d2410_arc_move_unitmm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern void d2410_arc_move_unitmm(ref UInt16 axis, ref double target_pos, ref double cen_pos, UInt16 arc_dir);
        //[DllImport("Dmc2410.dll", EntryPoint = "d2410_rel_arc_move_unitmm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern void d2410_rel_arc_move_unitmm(ref UInt16 axis, ref double rel_pos, ref double rel_cen, UInt16 arc_dir);

        //设置和读取位置比较信号
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_config_CMP_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_config_CMP_PIN(UInt16 axis, UInt16 cmp1_enable, UInt16 cmp2_enable, UInt16 CMP_logic);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_read_CMP_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 d2210_read_CMP_PIN(UInt16 axis);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_write_CMP_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_write_CMP_PIN(UInt16 axis, UInt16 on_off);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_config_comparator", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_config_comparator(UInt16 axis, UInt16 cmp1_condition, UInt16 cmp2_condition, UInt16 source_sel, UInt16 SL_action);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_set_comparator_data", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_set_comparator_data(UInt16 axis, UInt32 cmp1_data, UInt32 cmp2_data);



        //---------------------   编码器计数功能PLD  ----------------------//
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_get_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 d2210_get_encoder(UInt16 axis);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_set_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_set_encoder(UInt16 axis, UInt32 encoder_value);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_config_EZ_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_config_EZ_PIN(UInt16 axis, UInt16 ez_logic, UInt16 ez_mode);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_config_LTC_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_config_LTC_PIN(UInt16 axis, UInt16 ltc_logic, UInt16 ltc_mode);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_config_latch_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_config_latch_mode(UInt16 cardno, UInt16 all_enable);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_counter_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_counter_config(UInt16 axis, UInt16 mode);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_get_latch_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 d2210_get_latch_value(UInt16 axis);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_get_latch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 d2210_get_latch_flag(UInt16 cardno);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_reset_latch_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_reset_latch_flag(UInt16 cardno);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_get_counter_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 d2210_get_counter_flag(UInt16 cardno);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_reset_counter_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_reset_counter_flag(UInt16 cardno);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_reset_clear_flag", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_reset_clear_flag(UInt16 cardno);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_triger_chunnel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_triger_chunnel(UInt16 cardno, UInt16 num);
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_set_speaker_logic", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_set_speaker_logic(UInt16 cardno, UInt16 logic);

        //other
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_config_EMG_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void d2210_config_EMG_PIN(UInt16 cardno, UInt16 enable, UInt16 emg_logic);


        //增加同时起停操作
        /********************************************************************************
        ** 函数名称: d2410_set_t_move_all
        ** 功能描述: 多轴同步运动设定
        ** 输　  入: TotalAxes: 轴数,  pAxis:轴列表, pDist:位移列表
                     posi_mode: 0-相对, 1-绝对
        ** 返 回 值: 1:正确 , -1:参数错
        ** 
        ** 全局变量: 无
        ** 修改内容: 
        ** 修改日期:   
        *********************************************************************************/
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_set_t_move_all", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 d2210_set_t_move_all(UInt16 TotalAxes, ref UInt16 pAxis, ref UInt32 pDist, UInt16 posi_mode);

        /********************************************************************************
        ** 函数名称: d2410_start_move_all
        ** 功能描述: 多轴同步运动
        ** 输　  入: TotalAxes: 第一轴轴号
        ** 返 回 值: 1:正确 , -1:参数错
        ** 
        ** 全局变量: 无
        ** 修改内容: 
        ** 修改日期:      
        *********************************************************************************/
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_start_move_all", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 d2210_start_move_all(UInt16 FirstAxis);

        /********************************************************************************
        ** 函数名称: d2410_set_sync_option
        ** 功能描述: 多轴同步选项设定, 注意: 使用后必须关闭此功能, 将sync_option1清0.
        ** 输　  入: axis:轴号
                     sync_stop_on: 1:当CSTOP 信号来时,轴停止; 
                     cstop_output_on: 当异常停止时输出 CSTOP信号
                     sync_option1: 0:立即启动, 1: 等待CSTA信号 或是启动命令 
                     sync_option2: 无用
        ** 返 回 值: 1:正确 , -1:参数错
        ** 
        ** 全局变量: 无
        ** 修改内容: 
        ** 修改日期:     
        *********************************************************************************/
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_set_sync_option", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 d2210_set_sync_option(UInt16 axis, UInt16 sync_stop_on, UInt16 cstop_output_on, UInt16 sync_option1, UInt16 sync_option2);

        /********************************************************************************
        ** 函数名称: d2410_set_sync_stop_mode
        ** 功能描述: 设置同步停止的减速方式
        ** 输　  入: axis: 轴号
                     stop_mode:  0- 立即停止, 1-减速停止
        ** 返 回 值: 1:正确 , -1:参数错
        ** 
        ** 全局变量: 无
        ** 修改内容: 
        ** 修改日期:      
        *********************************************************************************/
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_set_sync_stop_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 d2210_set_sync_stop_mode(UInt16 axis, UInt16 stop_mode);

        /********************************************************************************
        ** 函数名称: d2410_config_CSTA_PIN
        ** 功能描述: 设置同步启动信号, 只能为低有效, 配置为电平或是边沿信号触发,默认为电平触发
        ** 输　  入: axis: 轴号
                     edge_mode:  0- 电平, 1-边沿
        ** 返 回 值: 1
        ** 
        ** 全局变量: 无
        ** 修改内容: 
        ** 修改日期: 
        *********************************************************************************/
        [DllImport("Dmc2210.dll", EntryPoint = "d2210_config_CSTA_PIN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 d2210_config_CSTA_PIN(UInt16 axis, UInt16 edge_mode);


        //脉冲闭环操作
        //[DllImport("Dmc2210.dll", EntryPoint = "d2210_pulse_loop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        //public static extern Int32 d2210_pulse_loop(UInt16 axis); 
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
