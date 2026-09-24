﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/DMC2210/DMC2210SDK.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
///
///	Description:
///			C# class for PCI-9014
///	Author:
///			yuanxiaowei
///	History:
///			2012-9-12  Create, part of APIs implemented
///			2014-4-11  update, all APIs implemented
///
using System;
using System.Runtime.InteropServices;

namespace WenQiZhi.Domain.MotionCard.Common.DMC2210 //命名空间根据应用程序修改
{
    public class DMC2210SDK
    {
        private static DMC2210SDK _instance;
        public static DMC2210SDK Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new DMC2210SDK();
                return DMC2210SDK._instance;
            }
            set { DMC2210SDK._instance = value; }
        }


        /// <summary>
        /// 设置主板上输入IO的端口的电平（虚拟运动卡专用）
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="bitno"></param>
        /// <param name="on_off">输入电平，0：低电平，1：高电平</param>
        /// <returns></returns>
        public int SetCardWriteInBit(int CardNo, int bitno, int on_off)
        {
            return 0;
        }


        /// <summary>
        /// 设置 OUT, DIR 引脚输出脉冲方式；初始化后，API 默认的脉冲输出为 PULSE/DIR 模式
        /// </summary>
        /// <param name="axis">轴号 </param>
        /// <param name="pls_outmode"> pls_outmode=0, PULSE/DIR 输出. <para>pls_outmode=1, CW/CCW 输出</para>   </param>
        /// <returns></returns>
        public int SetCardAxisOutMode(int axis, int pls_outmode)
        {
            Dmc2210.d2210_set_pulse_outmode((ushort)axis, (ushort)pls_outmode);
            return 0;
        }

        /// <summary>
        /// 设置脉冲输出模式。
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="outmode"></param>
        /// <returns></returns>
        public int SetCardAxisOutMode(int axis, PlusOutModeEnum outmode)
        {
            int plsoutmode = -1;
            switch (outmode)
            {
                case PlusOutModeEnum.OUT_DIR:
                    plsoutmode = 0;
                    break;
                case PlusOutModeEnum.OUT_DIR_OUT_NEG:
                    plsoutmode = 1;
                    break;
                case PlusOutModeEnum.OUT_DIR_DIR_NEG:
                    plsoutmode = 2;
                    break;
                case PlusOutModeEnum.OUT_DIR_ALL_NEG:
                    plsoutmode = 3;
                    break;
                case PlusOutModeEnum.O_CW_CCW:
                    plsoutmode = 4;
                    break;
                case PlusOutModeEnum.CW_CCW_ALL_NEG:
                    plsoutmode = 5;
                    break;
            }
            if(plsoutmode<0 || plsoutmode>5)
            {
                throw new ArgumentException("dmc2210:SetCardAxisOutMode()失败，传入的outmode不正确。");
            }
            Dmc2210.d2210_set_pulse_outmode((ushort)axis, (ushort)plsoutmode);
            return 0;
        }

        /// <summary>
        /// 设置轴的硬限位
        /// </summary>
        /// <param name="lpm">参数
        /// <para>"Axis"轴号 </para>
        ///<para>"ActiveLevel" 限位信号电平选择</para><para> 0－立即停、低有效, 1－减速停、低有效 2－立即停、高有效  3－减速停、高有效 </para> 
        /// </param>
        /// <returns> 正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardAxisELLimit(LimitParamModel lpm)
        {
            Dmc2210.d2210_config_EL_MODE((ushort)lpm.Axis, (ushort)lpm.ActiveLevel);
            return 0;
        }


        /// <summary>
        /// 使指定轴立即停止，没有任何减速的过程。
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="emg_stop"></param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int CardAxisStop(int axis, int emg_stop = 0)
        {
            Dmc2210.d2210_imd_stop((ushort)axis);
            return 0;
        }

        /// <summary>
        /// 使指定轴立即停止，没有任何减速的过程。
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="emg_stop"></param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int CardAxisStop(int axis, double emg_stop = 0)
        {
            Dmc2210.d2210_imd_stop((ushort)axis);
            return 0;
        }


        /// <summary>
        /// 让指定轴以梯形速度曲线加速到指定的运行速度后，连续运行。运动方向由 plus_dir 决定
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="plus_dir">指定运动的方向，其中 0 表示负方向，1 表示正方向</param>
        /// <param name="vel_mode">dmc2210不支持这个参数</param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int CardAxisVMove(int axis, int plus_dir, int vel_mode=0)
        {
             Dmc2210.d2210_t_vmove((ushort)axis, (ushort)plus_dir);
            return 0;
        }

        /// <summary>
        /// 让指定轴以 S 形速度曲线加速到指定的运行速度后，连续运行。运动方向由 plus_dir 决定
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="plus_dir">指定运动的方向，其中 0 表示负方向，1 表示正方向</param>
        /// <param name="vel_mode">dmc2210不支持这个参数</param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int CardAxisVMoveS(int axis, int plus_dir, int vel_mode = 0)
        {
            Dmc2210.d2210_s_vmove((ushort)axis, (ushort)plus_dir);
            return 0;
        }

        /// <summary>
        /// 启动控制轴进行定脉冲驱动，并立即返回；
        /// <para>------------------------------------------------------------------------------------------------------------------------------------------</para>
        /// <para>驱动的方向由 dist 决定；</para>
        /// <para>dist 大于 0 时，正向驱动；dist 小于 0 时，反向驱动； </para>
        /// <para>在绝对坐标方式下：</para>
        /// <para>dist 是表示目标位置, 运动方向由目标位置和当前位置差值的符号决定。 </para>
        /// <para>在运动过程中：限位信号从无效变为有效时，控制轴停止运动，</para>
        /// <para> 控制卡硬件有锁存机制：将状态锁存，需要控制轴反向驱动后才能清除锁存的状态；在无清除锁存状态前，控制轴无法继续向同方向运动</para>
        /// <para>------------------------------------------------------------------------------------------------------------------------------------------</para>
        /// </summary>
        /// <param name="axis">轴号 </param>
        /// <param name="dist">驱动的距离（相对于当前位置）；</param>
        /// <param name="dist_mode">dist 参数的坐标模式， 0 表示相对坐标； 1 表示绝对坐标；</param>
        /// <param name="vel_mode">dmc2210不支持这个参数</param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int CardAxisPMove(int axis, int dist, int dist_mode, int vel_mode)
        {
             Dmc2210.d2210_t_pmove((ushort)axis, dist, (ushort)dist_mode);
            return 0;
        }

        /// <summary>
        /// 慢速度回零，并立即返回。
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="plus_dir">原点查找的运动方向；1—正方向回原点 2—负方向回原点 </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int CardAxisHomeMove(int axis, int plus_dir)
        {
            Dmc2210.d2210_home_move((ushort)axis, (ushort)plus_dir, 0);
            return 0;
        }

        /// <summary>
        /// 高速度回零，并立即返回。
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="plus_dir"></param>
        /// <returns></returns>
        public int CardAxisHomeMoveHighSpeed(int axis, int plus_dir)
        {
            Dmc2210.d2210_home_move((ushort)axis, (ushort)plus_dir, 1);
            return 0;
        }



        /// <summary>
        /// 配置控制轴使用 T 型速度曲线加减速，并设置相应的起始速度、最大速度、加速度、减速度参数。 
        /// <para>调用该函数后，该轴的点位运动、连续运动、回零运动、减速停止将使用这些速度、加速度参数进行驱动。</para> 
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="start_vel">启动速度，单位: pps, 最大值为 1000000pps </param>
        /// <param name="max_vel">最大速度, 单位: pps, 最大值为 1000000pps </param>
        /// <param name="acc">加速度，单位： pps/s, 最大值为 1000000000pps/s </param>
        /// <param name="dec">减速度，单位: pps/s, 最大值为 1000000000pps/s </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCarsAxisTProfile(int axis, double start_vel, double max_vel, double acc, double dec)
        {
             Dmc2210.d2210_set_profile((ushort)axis, start_vel, max_vel, acc, dec);
            return 0;
        }

        /// <summary>
        /// 配置控制轴使用 T 型速度曲线加减速，并设置相应的起始速度、最大速度、加速度、减速度参数。 
        /// <para>调用该函数后，该轴的点位运动、连续运动、回零运动、减速停止将使用这些速度、加速度参数进行驱动。</para> 
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="start_vel">启动速度，单位: pps, 最大值为 1000000pps </param>
        /// <param name="max_vel">最大速度, 单位: pps, 最大值为 1000000pps </param>
        /// <param name="acc">加速度，单位： pps/s, 最大值为 1000000000pps/s </param>
        /// <param name="dec">减速度，单位: pps/s, 最大值为 1000000000pps/s </param>
        /// <param name="jerk_percent">过程使用 S 曲线的比例，范围为 0~1; 0 表示没有 S 曲线部分(也就是 T 型曲线加减速) | 1 表示全部 S 曲线加减速； </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCarsAxisSProfile(int axis, double start_vel, double max_vel, double acc, double dec, double jerk_percent=0.5)
        {
             Dmc2210.d2210_set_s_profile((ushort)axis, start_vel, max_vel, acc, dec, (int)(acc*jerk_percent), (int)(dec* jerk_percent));
            return 0;
        }

        /// <summary>
        /// 设定告警信号有效电平，可以为高电平有效，也可以为低电平有效，默认为低电平有效。 DI0—3 可在硬件上连接 XYZU 轴的告警 ALM 信号。用此函数可查询轴告警状态电平
        /// </summary>
        /// <param name="axis">轴号 </param>
        /// <param name="alm_logic">ALM 信号的输入电平：0－低电平有效，1－高电平有效</param>
        /// <param name="alm_action">ALM 信号的制动方式：0－立即停止，1－减速停止</para> </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardAxisAlarm(int axis, int alm_logic, int alm_action)
        {
            Dmc2210.d2210_config_ALM_PIN((ushort)axis, (ushort)alm_logic, (ushort)alm_action);
            return 0;
        }


        /// <summary>
        /// 查询指定轴的状态， 是正在运动，或者运动停止，处于空闲状态。
        /// <para>注意:</para>
        /// <para>停止运动的原因可能为到达目标位置，停止命令或者遇到错误，如 PEL, MEL 信号有效， 因此用户程序需要查询 PEL, MEL 的状态才能获取控制轴运动停止的原因。</para>
        /// </summary>
        /// <param name="axis">轴号 </param>
        /// <param name="pStatus">表示运动状态； 0: 表示控制轴正在运动；| 1: 表示控制轴停止</param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int GetCardAxisMotionStatus(int axis, ref uint pStatus)
        {
            pStatus = Dmc2210.d2210_check_done((ushort)axis);
            return 0;
        }

        /// <summary>
        /// 读取控制轴的位置计数器，该计数器可以为输出脉冲计数器(cntr_no = 0)或者编码器反馈脉冲位置计数器(cntr_no = 1)；其对应的位置分别为指令脉冲位置（逻辑位置），或者编码器反馈脉冲位置（实际位置）。  
        /// </summary>
        /// <param name="axis">轴号</param>
        ///  /// <param name="cntr_no">输出脉冲计数器/编码器反馈脉冲位置计数器选择:  0: 输出脉冲计数器 |  1: 编码器反馈脉冲位置计数器</param>
        /// <param name="pPos">位置计数器的值, 范围在-134217728 ~ 134217727；</param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int GetCardAxisPos(int axis, int cntr_no, ref int pPos)
        {
            pPos= Dmc2210.d2210_get_position((ushort)axis);
            return 0;
        }

        /// <summary>
        /// 读取 DO 端口的状态。 
        /// </summary>
        /// <param name="card_no">卡号</param>
        /// <param name="data">状态输出值，其中的 bit 0 ~15 分别对应端子上的 DO0~DO15 <para>当 data 中的位为 0 时，对应端子输出导通；</para> <para>当 data 中的位为 1 时，对应端子输出截止；</para></param>
        /// <returns> 正常返回 0；  出现错误时返回非 0 值；</returns>
        public int GetCardDO(int card_no, ref uint pData)
        {
            pData=(uint)Dmc2210.d2210_read_outport((ushort)card_no);
            return 0;
        }

        /// <summary>
        /// 读取指定控制卡的全部通用输出口的电平状态
        /// </summary>
        /// <param name="card_no">卡号</param>
        /// <param name="data">状态输出值，其中的 bit 0 ~15 分别对应端子上的 DO0~DO15 <para>当 data 中的位为 0 时，对应端子输出导通；</para> <para>当 data 中的位为 1 时，对应端子输出截止；</para></param>
        /// <returns> 正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardDO(int card_no, ref uint data)
        {
            data=(uint) Dmc2210.d2210_read_outport((ushort)card_no);
            return 0;
        }

        /// <summary>
        /// 设置 DO 端口中指定位的状态
        /// </summary>
        /// <param name="card_no">卡号 </param>
        /// <param name="bit_no">对应的位(bit)号， 范围 0~15,分别对应端子上的 DO0~DO15。</param>
        /// <param name="data">对应 DO 端子输出值:  <para>1  对应端子输出截止；</para> <para>0  对应端子输出导通；</para></param>
        /// <returns>  正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardDOBit(int card_no, uint bit_no, uint data)
        {
            Dmc2210.d2210_write_outbit((ushort)card_no, (ushort)bit_no, (ushort)data);
            return 0;
        }

        /// <summary>
        /// 读取指定控制卡的某一位输入口的电平状态
        /// </summary>
        /// <param name="card_no">卡号</param>
        /// <param name="bit_no">指定输入口位号（取值范围：1～20）</param>
        /// <param name="pData"> 表示低电平；1 表示高电平</param>
        /// <returns></returns>
        public int GetCardDIBit(int card_no, uint bit_no, ref uint pData)
        {
            pData=(uint) Dmc2210.d2210_read_inbit((ushort)card_no,(ushort)bit_no);
            return 0;
        }

        /// <summary>
        /// 读取指定控制卡的全部通用输入口的电平状态
        /// </summary>
        /// <param name="card_no">卡号 </param>
        /// <param name="pData">bit0～bit19 位值分别代表第1～20 号输入端口值</param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int GetCardDI(int card_no, ref uint pData)
        {
            pData=(uint) Dmc2210.d2210_read_inport((ushort)card_no);
            return 0;
        }

        /// <summary>
        /// 获取指定轴的当前速度 
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="pSpeed">读到的当前速度，单位： pps</param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int GetCardAxisCurrentSpeed(int axis, ref double pSpeed)
        {
            pSpeed= Dmc2210.d2210_read_current_speed((ushort)axis);
            return 0;
        }

        /// <summary>
        /// 配置原点开关有效电平、编码器 index 信号有效电平、回零模式；这些参数在 p9014_home_move 函数中将使用到。 在模式 0（home_mode = 0）情况下，只需要 ORG 信号有效即可， 对 ez_level 可以给任意值； 而在 2 模式下，需要使用到编码器输入的 index 信号，因而需要指定 index 信号的有效电平; 
        /// </summary>
        /// <param name="axis">轴号 </param>
        /// <param name="mode">dmc2210不支持这个选项。回零模式, 范围: 0~2;  <para>home_mode = 0 只有 ORG 有效，没有加速过程。 有效的 ORG 信号立即使控制轴立即停止运动，停止过程没有减速；在 ORG 有效的边沿，位置计数器被清零；  </para>  <para>   home_mode =2  ORG 和 index 信号同时有效，启动没有加速过程。 ORG 信号有效后，然后收到有效的 index 信号，控制轴停止运动，原点查找完成；原点查找结束后，位置计数器被清零；</para></param>
        /// <param name="org_level">原点信号的有效电平； 0 – 低有效； 1 – 高有效； </param>
        /// <param name="ez_level">编码器的 index 信号有效电平；0 – 低有效； 1 – 高有效；</param>
        /// <returns> 正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardAxisHomeConfig(int axis, int mode=0, int org_level=0, int filter=1)
        {
            Dmc2210.d2210_set_HOME_pin_logic((ushort)axis, (ushort)org_level, (ushort)filter);
            return 0;
        }


        /// <summary>
        /// 取板卡函数的返回值的释义
        /// </summary>
        /// <param name="ErrNum"></param>
        /// <returns></returns>
        public string GetErrorInfo(int ErrNum)
        {
            return "";
        }

        /// <summary>
        /// 获取与驱动轴相关 I/O（如限位信号、原点信号）的状态
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="io_status">I/O 状态</param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int GetAxisCardIOStatus(int axis, ref uint io_status)
        {
            io_status = Dmc2210.d2210_axis_io_status((ushort)axis);
            return 0;
        }
        /*
            位号           信号名称 描述
            0～3 保留
            4 CSD 1：表示同时减速信号（CSD）为 ON; 0 : 为OFF 
            5 STA 1：表示同时启动信号（STA）为 ON 
            6 STP 1：表示同时停止信号（STP）为 ON 
            7 EMG 1：表示紧急停止信号（EMG）为 ON 
            8 PCS 1：表示 PCS 信号为 ON 
            9 ERC 1：表示误差清除信号（ERC）为 ON 
            10 EZ 1：表示索引信号（EZ）为 ON 
            11 +DR(PA) 1：表示 +DR(PA) 信号为 ON 
            12 -DR(PB) 1：表示 -DR(PB) 信号为 ON 
            13 保留
            14 SD 1：表示 SD 信号为 ON 
            15 INP 1：表示到位信号 INP 为 ON 
            16 DIR 脉冲输出方向(0：表示正方向；1：表示负方向）
            17～31 保留
             */
        public int GetAxisRsts(int axis, ref int status)
        {
            status = (int)Dmc2210.d2210_get_rsts((ushort)axis);
            return 0;
        }

        /// <summary>
        /// 设置位置计数器 
        /// </summary>
        /// <param name="axis">轴号 </param>
        /// <param name="cntr_on">输出脉冲计数器/编码器反馈脉冲位置计数器选择:<para> 0 :输出脉冲计数器 </para><para>1: 编码器反馈脉冲位置计数器</para></param>
        /// <param name="Pos">要设置的值, 范围在-134217728 ~ 134217727。 </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值； </returns>
        public int SetCardAxisPos(int axis, int cntr_on, int Pos)
        {
            //dmc2210不支持编码器反馈脉冲位置计数器，只支持输出脉冲计数器
            Dmc2210.d2210_set_position((ushort)axis, Pos);
            return 0;
        }

        /// <summary>
        /// 关闭控制卡
        /// </summary>
        /// <returns></returns>
        public int CloseCard()
        {
            Dmc2210.d2210_board_close();
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
        public int GetCardVersion(int card_no, ref uint pApi_ver, ref uint pDriver_ver, ref uint pLogic_ver)
        {
            //dmc2210貌似不支持这个指令
            pApi_ver = 0;
            pDriver_ver = 0;
            pLogic_ver = 0;

            return 0;
        }


        /// <summary>
        /// 初始化 DMC2210卡控制卡
        /// </summary>
        /// <param name="card_id">有*existCards 个元素的数组，包含查找到的 DMC2210卡 的 ID 号，建议使用含 有 16 个元素的 I32 类型数组作为该参数 </param>
        /// <param name="card_count">查到的 DMC2210卡的数目，最多能有8张卡</param>
        /// <returns>正常返回 0； 出现错误时返回非 0 值;</returns>
        public int InitCard(ref int[] card_id, ref int card_count)
        {
            card_id = new int[16];
            card_count= Dmc2210.d2210_board_init();
            if(card_count==0)
            {
                return -1;
            }
            for(int i=1;i<=card_count;i++)
            {
                card_id[i] = i;
            }
            return 0;
        }

    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
