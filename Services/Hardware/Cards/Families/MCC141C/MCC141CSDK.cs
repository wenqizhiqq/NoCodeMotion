﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/MCC141C/MCC141CSDK.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using csMCC1C00;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Samsun.Domain.MotionCard.Common.MCC141C
{
    public class MCC141CSDK
    {
        /// <summary>
        /// 用户定义轴回零参数
        /// </summary>
        public HomeParameModel[] homeConfigArray/* = new HomeParameModel[4]*/;
        private static MCC141CSDK _instance;
        public static MCC141CSDK Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MCC141CSDK();
                return MCC141CSDK._instance;
            }
            set { MCC141CSDK._instance = value; }
        }

        /// <summary>
        /// 取板卡函数的返回值的释义
        /// </summary>
        /// <param name="ErrNum"></param>
        /// <returns></returns>
        public string GetErrorInfo(int ErrNum)
        {
            var msg = string.Empty;
            Dictionary<int, string> dic = new Dictionary<int, string>()
            {
                {0,"函数正确执行" },
                {1,"invalid parameter(参数无效)，代表卡号参数设置错误" },
                {2,"代表轴号参数设置错误" },
                {3,"除卡号和轴号以上参数外的其他参数设置错误" },
                {4,"PCI数据读写错误" },
                {8,"插补轴正在运动" },
                {400,"缺少初始化或初始化错误" },
                {401,"指定轴正在运行中" },
                {402,"RC_MEMORY_TOOL_SMALL" },
                {404,"RC_INVALID_DRIVER" },
                {405,"insufficient system resource, create system object fail" },
                {406,"RC_INVALID_HARDWARE" },
                {407,"RC_DEVICE_ALREADY_OPEN(设备已经打开)" },

                {408,"RC_PCI_ACCESS_ERROR(无法打开 PCI 设备)" },
                {409,"RC_PCI_CONFIG_ERROR" },
                {410,"RC_PCI_ACCESS_TIMEOUT" },

                {411,"unknow motion error(未知运动错误)" },
                {412,"the axis is busy(轴处于运行状态)" },
                {413,"the axis is busy interplating" },
                {414,"fail to search home" },
                {415,"limit switch is active while driving(限位有效或告警有效)" },
                {416,"continuous interpolation mode is disable" },
                {417,"continuous interplation mode is enabled" },
                {418,"distance too large for positioning drive or linear " },
                {-300,"回原过程中轴被异常停止" }
            };

            if (dic.ContainsKey(ErrNum))
            {
                return dic[ErrNum];
            }
            return "unkown errNum";
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
        class SpeedType
        {
            /// <summary>
            /// 1:运动为T型曲线    2:运动为S型曲线
            /// </summary>
            public int Type { get; set; } = 1;
            public int CardNo { get; set; } = 1;
            public int AxisNO { get; set; } = 1;

            public double StartVel { get; set; } = 1;
            public double MaxVel { get; set; } = 1;
            public double Tacc { get; set; } = 1;
            public double Tdec { get; set; } = 1;
            public double STcc { get; set; } = 1;

        }
        List<SpeedType> AxisSpeedList = new List<SpeedType>();
        /// <summary>
        /// 初始化 MCC141C卡控制卡
        /// </summary>
        /// <param name="card_id">有*existCards 个元素的数组，包含查找到的 PCI-9014 的 ID 号，建议使用含 有 16 个元素的 I32 类型数组作为该参数 </param>
        /// <param name="card_count">查到的 PCI-9014 数目</param>
        /// <returns>正常返回 0； 出现错误时返回非 0 值;</returns>
        public int InitCard(ref int[] card_id, ref int card_count)
        {
            card_id = new int[16];
            card_count = MCC1C00.YK_board_init();
            if (card_count > 0 && card_count <= 16)
            {
                uint cardType = 0; AxisSpeedList.Clear();
                for (int i = 0; i < card_count; i++)
                {
                    card_id[i] = i;
                    MCC1C00.YK_get_card_type((ushort)i, ref cardType);
                    if (cardType == 0X1C00)
                    {
                        for (int j = 0; j < 12; j++)
                        {
                            AxisSpeedList.Add(new SpeedType
                            {
                                AxisNO = j,
                                CardNo = i,
                            });
                        }
                    }
                    else
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            AxisSpeedList.Add(new SpeedType
                            {
                                AxisNO = j,
                                CardNo = i,
                            });
                        }
                    }
                }
                return 0;
            }
            else
                return -200;

        }

        /// <summary>
        /// 设置位置计数器 
        /// </summary>
        /// <param name="axis">轴号 </param>
        /// <param name="cntr_on">输出脉冲计数器/编码器反馈脉冲位置计数器选择:<para> 0 :输出脉冲计数器 </para><para>1: 编码器反馈脉冲位置计数器</para></param>
        /// <param name="Pos">要设置的值, 范围在-134217728 ~ 134217727。 </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值； </returns>
        public int SetCardAxisPos(int cardNo, int axis, int Pos)
        {
            //for (int j = 0; j < 4; j++)
            //{
            //    axis = card_id[i] * 4 + j;
            //    MCC1C00.p9014_set_pos(axis, 0, 0);
            //}
            return MCC1C00.YK_set_encoder((short)cardNo, (short)axis, Pos);
        }

        /// <summary>
        /// 直线插补运动
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">指定控制卡上的坐标系号；取值范围：0~1</param>
        /// <param name="axisNum">插补轴数，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5DMC3400A：0~3</param>
        /// <param name="axisList">插补轴列表</param>
        /// <param name="DistList">插补轴目标位置列表，单位：pulse</param>
        /// <param name="posi_mode">运动模式，0：相对坐标模式，1：绝对坐标模式</param>
        /// <returns></returns>
        public int CardAxisLineMulticoor(int CardNo, int Crd, int axisNum, UInt16[] axisList, int[] distList, int posi_mode, int startVel, int MaxVel, double Tacc)
        {
            //return LTDMC.dmc_line_multicoor((ushort)CardNo, (ushort)Crd, (ushort)axisNum, axisList, DistList, (ushort)posi_mode);
            //MCC1C00.P9014_PTP_move(axisList,DistList,)

            if (posi_mode == 0)
            {
                var AxisList = Array.ConvertAll(axisList, s => (short)s);
                var DistList = Array.ConvertAll(distList, s => (int)s);
                MCC1C00.YK_start_t_line((short)CardNo, (short)axisNum, ref AxisList, ref DistList, startVel, MaxVel, Tacc);

            }
            else if (posi_mode == 1)
            {
                var AxisList = Array.ConvertAll(axisList, s => (short)s);
                var DistList = Array.ConvertAll(distList, s => (int)s);
                MCC1C00.YK_start_ta_line((short)CardNo, (short)axisNum, ref AxisList, ref DistList, startVel, MaxVel, Tacc);
            }
            return 0;
        }


        /// <summary>
        /// 关闭卡
        /// </summary>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int CloseCard()
        {
            return MCC1C00.YK_board_close();
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
        /// <para> Vel_mode 参数表示该命令使用的速度模式： </para>
        /// <para>如果 vel_mode 为 0，则该轴以 p9014_set_t_profile 中的 start_vel 为速度进行驱动； </para>
        /// <para>如果 vel_mode 为 1，则该轴以 p9014_set_t_profile 中的 max_vel 为速度进行驱动；</para>
        /// <para>如果 vel_mode 为 2，则该轴从 start_vel 加速到 max_vel 后，以 max_vel 为速度进行驱动，运行到减速点后，控制轴开始减速。 在运行过程中，可以使用 p9014_stop 函数使控制轴停止运动。</para>
        /// </summary>
        /// <param name="axis">轴号 </param>
        /// <param name="dist">驱动的距离（相对于当前位置）；</param>
        /// <param name="dist_mode">dist 参数的坐标模式， 0 表示相对坐标； 1 表示绝对坐标；</param>
        /// <param name="vel_mode">速度模式<para>0 :表示以 start_vel 为速度进行驱动，中间没有加速过程；</para><para>1: 表示以 max_vel 为速度进行驱动，中间没有加速过程；</para><para>2: 表示从 start_vel 加速到 max_vel,  中间有加速、减速过程；</para></param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int CardAxisPMove(int card, int axis, int dist, int dist_mode, int vel_mode)
        {
            var index = AxisSpeedList.FindIndex(s => s.AxisNO == axis && s.CardNo == card);
            if (index < 0) throw new ArgumentException($"index错误：【{index}】");
            if (dist_mode == 0)
            {
                if (AxisSpeedList[index].Type == 1)
                    return MCC1C00.YK_start_s_move((short)card, (short)axis, dist, (int)AxisSpeedList[index].StartVel, (int)AxisSpeedList[index].MaxVel, AxisSpeedList[index].Tacc);
                else
                    return MCC1C00.YK_start_s_move((short)card, (short)axis, dist, (int)AxisSpeedList[index].StartVel, (int)AxisSpeedList[index].MaxVel, AxisSpeedList[index].Tacc);
            }
            else
            {
                if (AxisSpeedList[index].Type == 1)
                    return MCC1C00.YK_start_sa_move((short)card, (short)axis, dist, (int)AxisSpeedList[index].StartVel, (int)AxisSpeedList[index].MaxVel, AxisSpeedList[index].Tacc);
                else
                    return MCC1C00.YK_start_sa_move((short)card, (short)axis, dist, (int)AxisSpeedList[index].StartVel, (int)AxisSpeedList[index].MaxVel, AxisSpeedList[index].Tacc);
            }

        }

        /// <summary>
        /// 启动控制轴进行连续驱动，运动方向由 plus_dir 决定
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="plus_dir">驱动方向， 1 为正向驱动， 0 为反向驱动；</param>
        /// <param name="vel_mode">速度模式<para>0 :表示以 start_vel 为速度进行驱动，中间没有加速过程；</para><para>1: 表示以 max_vel 为速度进行驱动，中间没有加速过程；</para><para>2: 表示从 start_vel 加速到 max_vel,  中间有加速、减速过程；</para></param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int CardAxisVMove(int card, int axis, int plus_dir, int vel_mode)
        {
            var index = AxisSpeedList.FindIndex(s => s.AxisNO == axis && s.CardNo == card);
            if (index < 0) throw new ArgumentException($"index错误：【{index}】");
            if (vel_mode == 1)
                return MCC1C00.YK_start_tv_move((short)card, (short)axis, (int)AxisSpeedList[index].StartVel, (int)(plus_dir == 1 ? Math.Abs(AxisSpeedList[index].MaxVel) : -Math.Abs(AxisSpeedList[index].MaxVel)), AxisSpeedList[index].Tacc);
            else
                return MCC1C00.YK_start_sv_move((short)card, (short)axis, (int)AxisSpeedList[index].StartVel, (int)(plus_dir == 1 ? Math.Abs(AxisSpeedList[index].MaxVel) : -Math.Abs(AxisSpeedList[index].MaxVel)), AxisSpeedList[index].Tacc);
        }

        /// <summary>
        /// 使控制轴减速停止或立即停止。
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="emg_stop"><para>如果控制轴减速停止(EmgStop = 0)，将使用 p9014_set_t_profile 中的 dec 参数进行减速，减速到 start_vel 后，停止运动。</para><para>  如果控制轴立即停止(EmgStop = 1)，则没有减速过程，立即停止运动。</para> </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int CardAxisStop(int card, int axis, int emg_stop)
        {
            if (emg_stop == 0)
                return MCC1C00.YK_decel_stop((short)card, (short)axis);
            else
                return MCC1C00.YK_immediate_stop((short)card, (short)axis);

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
        public int SetCarsAxisTProfile(int card, int axis, double start_vel, double max_vel, double acc, double dec)
        {
            //加减速同步雷赛单位：s
            try
            {
                var index = AxisSpeedList.FindIndex(s => s.AxisNO == axis && s.CardNo == card);
                if (index < 0) throw new ArgumentException($"index错误：【{index}】");
                AxisSpeedList[index].StartVel = start_vel;
                AxisSpeedList[index].MaxVel = max_vel;
                AxisSpeedList[index].Tacc = acc;
                AxisSpeedList[index].Tdec = dec;
                AxisSpeedList[index].Type = 1;
            }
            catch
            {
                return -111;
            }
            return 0;
            //return MCC1C00.p9014_set_t_profile(axis, start_vel, max_vel, acc, dec);
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
        public int SetCarsAxisSProfile(int card, int axis, double start_vel, double max_vel, double acc, double dec, double jerk_percent)
        {
            //加减速同步雷赛单位：s
            try
            {
                var index = AxisSpeedList.FindIndex(s => s.AxisNO == axis && s.CardNo == card);
                if (index < 0) throw new ArgumentException($"index错误：【{index}】");
                AxisSpeedList[index].StartVel = start_vel;
                AxisSpeedList[index].MaxVel = max_vel;
                AxisSpeedList[index].Tacc = acc;
                AxisSpeedList[index].Tdec = dec;
                AxisSpeedList[index].Type = 2;
            }
            catch
            {
                return -111;
            }
            return 0;
        }


        /// <summary>
        /// 配置原点开关有效电平、编码器 index 信号有效电平、回零模式；这些参数在 p9014_home_move 函数中将使用到。 在模式 0（home_mode = 0）情况下，只需要 ORG 信号有效即可， 对 ez_level 可以给任意值； 而在 2 模式下，需要使用到编码器输入的 index 信号，因而需要指定 index 信号的有效电平; 
        /// </summary>
        /// <param name="axis">轴号 </param>
        /// <param name="mode">回零模式, 范围: 0~2;  <para>home_mode = 0 只有 ORG 有效，没有加速过程。 有效的 ORG 信号立即使控制轴立即停止运动，停止过程没有减速；在 ORG 有效的边沿，位置计数器被清零；  </para>  <para>   home_mode =2  ORG 和 index 信号同时有效，启动没有加速过程。 ORG 信号有效后，然后收到有效的 index 信号，控制轴停止运动，原点查找完成；原点查找结束后，位置计数器被清零；</para></param>
        /// <param name="org_level">原点信号的有效电平； 0 – 低有效； 1 – 高有效； </param>
        /// <param name="ez_level">编码器的 index 信号有效电平；0 – 低有效； 1 – 高有效；</param>
        /// <returns> 正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardAxisHomeConfig(int axis, int mode, int org_level, int ez_level)
        {
            //return MCC1C00.p9014_home_config(axis, mode, org_level, ez_level);
            return 0;
        }



        ///// <summary>
        ///// 驱动指定的轴回零，并立即返回。 回零是否完成，可以通过 p9014_motion_done 查询状态来完成。
        ///// </summary>
        ///// <param name="axis">轴号</param>
        ///// <param name="plus_dir">原点查找的运动方向；<para> pulsDir = 0  控制轴反向运动启动原点查找</para><para>plusDir = 1  控制轴正向运动启动原点查找</para> </param>
        ///// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        //public int CardAxisHomeMove(int axis, int plus_dir)
        //{
        //    return MCC1C00.p9014_home_move(axis, plus_dir);
        //}

        /// <summary>
        /// 驱动指定的轴回零，并立即返回。 回零是否完成，可以通过 p9014_motion_done 查询状态来完成。
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="plus_dir">原点查找的运动方向；<para> pulsDir = 0  控制轴反向运动启动原点查找</para><para>plusDir = 1  控制轴正向运动启动原点查找</para> </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int CardAxisHomeMove(int card, int axis, int plus_dirint/*, int positiveDir, int negativeDir*/)
        {
            //return MCC1C00.p9014_home_move(axis, plus_dir);
            return CustomizeHome(card, axis, homeConfigArray[axis].HomeDir, (int)homeConfigArray[axis].HomeTrigPosMoveDir, (int)homeConfigArray[axis].HomeTrigNegMoveDir);    //todo:位偏脉冲需要在轴参数设置上设定  xcr_20230518
        }


        /// <summary>
        /// 设置 DO 端口输出状态 
        /// </summary>
        /// <param name="card_no">卡号</param>
        /// <param name="data">状态输出值，其中的 bit 0 ~15 分别对应端子上的 DO0~DO15 <para>当 data 中的位为 0 时，对应端子输出导通；</para> <para>当 data 中的位为 1 时，对应端子输出截止；</para></param>
        /// <returns> 正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardDO(int card_no, uint data)
        {
            throw new ArgumentException("");
            //return MCC1C00.YK_out_bit(card_no, data);
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
            return MCC1C00.YK_out_bit((short)card_no, (short)bit_no, (short)data);
        }

        /// <summary>
        /// 读取 DO 端口的状态。 
        /// </summary>
        /// <param name="card_no">卡号</param>
        /// <param name="data">状态输出值，其中的 bit 0 ~15 分别对应端子上的 DO0~DO15 <para>当 data 中的位为 0 时，对应端子输出导通；</para> <para>当 data 中的位为 1 时，对应端子输出截止；</para></param>
        /// <returns> 正常返回 0；  出现错误时返回非 0 值；</returns>
        public int GetCardDO(int card_no, ref uint pData)
        {
            throw new ArgumentException("");

            //return MCC1C00.p9014_get_do(card_no, ref pData);
        }

        /// <summary>
        /// 查询 DI 端口的状态。 DI0—3 可在硬件上连接 XYZU 轴的告警 ALM 信号。用此函数可查询轴告警状态
        /// </summary>
        /// <param name="card_no">卡号 </param>
        /// <param name="pData">DI 端口状态，其中的 bit 0 ~15 分别对应端子上的 DI0~DI15  <para> 当*pData 中的位为 0 时，对应端子输入低电平；</para> <para> 当*pData 中的位为 1 时，对应端子输入高电平；</para>    </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int GetCardDI(int card_no, ref uint pData)
        {
            throw new ArgumentException("");

            //return MCC1C00.p9014_get_di(card_no, ref pData);
        }

        /// <summary>
        /// 查询 DI 端口中指定位的状态 DI0—3 可在硬件上连接 XYZU 轴的告警 ALM 信号。用此函数可查询轴告警状态
        /// </summary>
        /// <param name="card_no">卡号</param>
        /// <param name="bit_no">对应的位(bit)号， 范围 0~15,分别对应端子上的 DI0~DI15。</param>
        /// <param name="pData">DI 端口状态，其中的 bit 0 ~15 分别对应端子上的 DI0~DI15  <para> 当*pData 中的位为 0 时，对应端子输入低电平；</para> <para> 当*pData 中的位为 1 时，对应端子输入高电平；</para>    </param>
        /// <returns></returns>
        public int GetCardDIBit(int card_no, uint bit_no, ref int pData)
        {
            pData = MCC1C00.YK_in_bit((short)card_no, (short)bit_no);
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
            var res = MCC1C00.YK_get_card_version((ushort)card_no, ref pApi_ver);

            return res;
        }

        /// <summary>
        /// 逻辑版本信息查询
        /// </summary>
        /// <param name="card_no">卡号</param>
        /// <param name="pLogic_revision"> 运动控制功能的版本号； </param>
        /// <returns>   正常返回 0；  出现错误时返回非 0 值；</returns>
        public int GetCardReVersion(int card_no, ref int pLogic_revision)
        {
            uint pLogic_revision1 = 0;
            return MCC1C00.YK_get_card_lib_version((ushort)card_no, ref pLogic_revision1);
        }

        /// <summary>
        /// 设定限位信号有效电平，可以为高电平有效，也可以为低电平有效，默认为低电平有效。
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="active_level">限位信号电平选择 <para>0 低电平有效</para> <para>1 高电平有效 </para></param>
        /// <returns> 正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardAxisELLevel(int axis, int active_level)
        {
            //return MCC1C00.p9014_set_el_level(axis, active_level);
            return 0;
        }


        /// <summary>
        /// 错误时设置停止模式(减速停止、突然停止)
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="stop_mode">停止模式(0:减速停止、1:突然停止)</param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardAxisErrorStopMode(int axis, int stop_mode)
        {
            //return MCC1C00.p9014_set_error_stop_mode(axis, stop_mode);
            throw new ArgumentException("");

        }

        /// <summary>
        /// 设定告警信号有效电平，可以为高电平有效，也可以为低电平有效，默认为低电平有效。 DI0—3 可在硬件上连接 XYZU 轴的告警 ALM 信号。用此函数可查询轴告警状态电平
        /// </summary>
        /// <param name="axis">轴号 </param>
        /// <param name="enable">告警信号使能</param>
        /// <param name="active_level">限位信号电平选择  <para>0 低电平有效</para>  <para>1 高电平有效</para></param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardAxisAlarm(int axis, int enable, int active_level)
        {
            return MCC1C00.YK_set_alm_mode(0, (short)axis, (short)enable, (short)active_level, 0);
        }


        /// <summary>
        /// 设置卡Com口使能
        /// </summary>
        /// <param name="card_no"></param>
        /// <param name="enable"></param>
        /// <param name="active_level"></param>
        /// <param name="ref_source"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public int SetCardCompEnable(int card_no, int enable, int active_level, int ref_source, int length)
        {
            //return MCC1C00.p9014_comp_enable(card_no, enable, active_level, ref_source, length);
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
            return 0;
            //return MCC1C00.p9014_set_pls_outmode(axis, pls_outmode);
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
                case PlusOutModeEnum.O_CW_CCW:
                    plsoutmode = 1;
                    break;
            }

            return 0;
            //return MCC1C00.p9014_set_pls_outmode(axis, plsoutmode);
        }

        /// <summary>
        /// 设置外部脉冲编码器输入信号类型； 共有两种类型可选： PULSE/DIR , 4X AB。初始化后， API 默认的输入信号类型为 4X AB。 
        /// </summary>
        /// <param name="axis">轴号 </param>
        /// <param name="pls_iptmode">0 ：PULSE/DIR  <para> 1：4X AB</para> </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardAxisInputMode(int axis, int pls_iptmode)
        {
            //return MCC1C00.p9014_set_pls_iptmode(axis, pls_iptmode);
            return 0;

        }

        /// <summary>
        /// 查询指定轴的状态， 是正在运动，或者运动停止，处于空闲状态。
        /// <para>注意:</para>
        /// <para>停止运动的原因可能为到达目标位置，停止命令或者遇到错误，如 PEL, MEL 信号有效， 因此用户程序需要查询 PEL, MEL 的状态才能获取控制轴运动停止的原因。</para>
        /// </summary>
        /// <param name="axis">轴号 </param>
        /// <param name="pStatus">表示运动状态； 0: 表示控制轴运动完成，处于空闲状态；| 1: 表示控制轴正在运动</param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int GetCardAxisMotionStatus(int card, int axis, ref uint pStatus)
        {
            pStatus = (uint)MCC1C00.YK_check_done((short)card, (short)axis);
            if (pStatus == 1) pStatus = 0;
            //var err = GetErrorInfo(pStatus);
            return 0;
        }

        /// <summary>
        /// 获取与驱动轴相关 I/O（如限位信号、原点信号）的状态
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="io_status">I/O 状态</param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int GetAxisCardIOStatus(int card, int axis, ref uint io_status)
        {
            io_status = (uint)MCC1C00.YK_get_axis_status((short)card, (short)axis);

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
            return 0;
            //return MCC1C00.p9014_get_current_speed(axis, ref pSpeed);
        }

        /// <summary>
        /// 读取控制轴的位置计数器，该计数器可以为输出脉冲计数器(cntr_no = 0)或者编码器反馈脉冲位置计数器(cntr_no = 1)；其对应的位置分别为指令脉冲位置（逻辑位置），或者编码器反馈脉冲位置（实际位置）。  
        /// </summary>
        /// <param name="axis">轴号</param>
        ///  /// <param name="cntr_no">输出脉冲计数器/编码器反馈脉冲位置计数器选择:  0: 输出脉冲计数器 |  1: 编码器反馈脉冲位置计数器</param>
        /// <param name="pPos">位置计数器的值, 范围在-134217728 ~ 134217727；</param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int GetCardAxisPos(int card, int axis, int cntr_no, ref int pPos)
        {
            pPos = MCC1C00.YK_get_command_pos((short)card, (short)axis);
            return 0;
        }


        #region MCC1C00卡 开发文档 未 包含函数
        /// <summary>
        /// 驱动指定的轴回零。修改回原并不会直接返回会等待回原完成
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="plus_dir">原点查找的运动方向；<para> pulsDir = 0  控制轴反向运动启动原点查找</para><para>plusDir = 1  控制轴正向运动启动原点查找</para> </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int CustomizeHome(int card, int axis, int plus_dir, int positiveDir, int negativeDir)
        {
            var index = AxisSpeedList.FindIndex(s => s.AxisNO == axis && s.CardNo == card);
            if (index < 0) throw new ArgumentException($"index错误：【{index}】");
            int error = MCC1C00.YK_home_move((short)card, (short)axis, (int)AxisSpeedList[index].StartVel, (int)(plus_dir == 1 ? Math.Abs(AxisSpeedList[index].MaxVel) : -Math.Abs(AxisSpeedList[index].MaxVel)), AxisSpeedList[index].Tacc);
            if (error == 0)
            {
                int UState = MCC1C00.YK_get_axis_status((short)card, (short)axis);
                if (error != 0 && ReadEMGStatus(card))
                {//急停
                    if (ReadEMGStatus(card)) return 205;
                    else
                        return error;
                }
                int check_done = 0;
                while (true)
                {
                    UState = MCC1C00.YK_get_axis_status((short)card, (short)axis);
                    check_done = MCC1C00.YK_check_done((short)card, (short)axis);
                    if ((UState >> 1) % 2 == 1/* && error2 == 0*/)
                    {//轴到达正限位
                        error = LimitHome(card, axis, plus_dir, UState, 0, -positiveDir);
                    }
                    else if ((UState) % 2 == 1 /*&& error2 == 0*/)
                    {//轴到达负限位
                        error = LimitHome(card, axis, plus_dir, UState, 1, negativeDir);
                    }
                    else if (/*(UState >> 4) % 2 == 0*/ReadEMGStatus(card) || CConfigurationInfo.Instance.ReadEstopSignalByHardwareTable())
                    {
                        MCC1C00.YK_immediate_stop((short)card, (short)axis); return 205;
                    }
                    else if ((UState >> 2) % 2 == 1 /*&& error2 == 0*/)//①：在正限位和原点直接负方向直接回原完成    ②：在负限位和原点直接正方向直接回原完成
                        return error;
                    else if ((UState >> 2) % 2 != 1 && check_done == 1)
                    {//不等于原点且轴停止运动
                        return -300;
                    }

                    Thread.Sleep(20);
                }
                //});
                // axisHomeThread.Start();
                return error;
            }
            else
                return error;
        }
        /// <summary>
        /// 驱动指定的轴限位回零。修改回原并不会直接返回会等待回原完成
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="plus_dir">原点查找的运动方向；<para> pulsDir = 0  控制轴反向运动启动原点查找</para><para>plusDir = 1  控制轴正向运动启动原点查找</para></param>
        /// <param name="UState">轴IO状态</param>
        /// <param name="reverseBack">反向回原</param>
        /// <param name="dist_mode">位偏脉冲</param>
        /// <returns></returns>
        public int LimitHome(int card, int axis, int plus_dir, int UState, int reverseBack, int dist_mode)
        {//p9014_get_motion_status 添加check_done
            var index = AxisSpeedList.FindIndex(s => s.AxisNO == axis && s.CardNo == card);
            if (index < 0) throw new ArgumentException($"index错误：【{index}】");
            int error = -1;
            int error3 = MCC1C00.YK_home_move((short)card, (short)axis, (int)AxisSpeedList[index].StartVel, (int)(reverseBack == 1 ? Math.Abs(AxisSpeedList[index].MaxVel) : -Math.Abs(AxisSpeedList[index].MaxVel)), AxisSpeedList[index].Tacc);
            int check_done = 0; uint UState1 = 0;
            if (error3 == 0)
            {
                while (true)
                {
                    UState = MCC1C00.YK_get_axis_status((short)card, (short)axis);
                    check_done = MCC1C00.YK_check_done((short)card, (short)axis);
                    if ((UState >> 2) % 2 == 1)
                    {
                        CardAxisPMove(card, axis, dist_mode, 0, 1);
                        while (true)
                        {
                            check_done = MCC1C00.YK_check_done((short)card, (short)axis);
                            UState = MCC1C00.YK_get_axis_status((short)card, (short)axis);
                            if (check_done == 1)
                                break;
                            else if (ReadEMGStatus(card) || CConfigurationInfo.Instance.ReadEstopSignalByHardwareTable())
                            {
                                MCC1C00.YK_immediate_stop((short)card, (short)axis); return 205;
                            }
                            Thread.Sleep(30);
                        }
                        error = MCC1C00.YK_home_move((short)card, (short)axis, (int)AxisSpeedList[index].StartVel, (int)(plus_dir == 1 ? Math.Abs(AxisSpeedList[index].MaxVel) : -Math.Abs(AxisSpeedList[index].MaxVel)), AxisSpeedList[index].Tacc);
                        if (error != 0)
                            return error;
                    }
                    if (error == 0)
                        break;
                    else if (ReadEMGStatus(card) || CConfigurationInfo.Instance.ReadEstopSignalByHardwareTable())
                    {
                        MCC1C00.YK_immediate_stop((short)card, (short)axis); return 205;
                    }
                    else if ((UState >> 2) % 2 != 1 && check_done == 1)
                    {//不等于原点且轴停止运动
                        return -300;
                    }
                    Thread.Sleep(20);
                }
                return error;
            }
            else
                return error3;
        }

        public bool ReadEMGStatus(int card)
        {
            return MCC1C00.YK_in_bit((short)card, 15) == 0 ? true : false;

        }

        /*
        /// <summary>
        /// 预留（不能使用，功能不明）
        /// </summary>
        /// <param name="card_no"></param>
        /// <param name="PosRef"></param>
        /// <returns></returns>
        public int A(int card_no, int PosRef)
        {
            return MCC1C00.p9014_comp_add_ref(card_no, PosRef);
        }
        /// <summary>
        /// 预留（不能使用，功能不明）
        /// </summary>
        /// <param name="card_no"></param>
        /// <param name="PosRef"></param>
        /// <returns></returns>
        public int B(int card_no, ref int PosRef)
        {

            return MCC1C00.p9014_comp_get_curRef(card_no,ref PosRef);
        }
        /// <summary>
        /// 预留（不能使用，功能不明）
        /// </summary>
        /// <param name="card_no"></param>
        /// <returns></returns>
        public int C(int card_no)
        {

            return MCC1C00.p9014_comp_clr_fifo(card_no);
        }
        /// <summary>
        /// 预留（不能使用，功能不明）
        /// </summary>
        /// <param name="card_no"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public int D(int card_no, ref uint Count)
        {

            return MCC1C00.p9014_comp_get_matchCount(card_no,ref Count); 
        }
        /// <summary>
        /// 预留（不能使用，功能不明）
        /// </summary>
        /// <param name="card_no"></param>
        /// <returns></returns>
        public int E(int card_no)
        {

            return MCC1C00.p9014_comp_clr_matchCount(card_no);
        }

        /// <summary>
        /// 预留（不能使用，功能不明）
        /// </summary>
        /// <param name="card_no"></param>
        /// <param name="stat"></param>
        /// <returns></returns>
        public int H(int card_no, ref uint stat)
        {
            return MCC1C00.p9014_comp_get_fifoStatus(card_no, ref stat);
        } 
         */
        #endregion
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
