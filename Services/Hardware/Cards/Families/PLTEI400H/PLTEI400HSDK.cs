﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/PLTEI400H/PLTEI400HSDK.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WenQiZhi.Domain.MotionCard.Common.PLTEI400H
{
    public class PLTEI400HSDK
    {
        /// <summary>
        /// 单例
        /// </summary>
        private static readonly Lazy<PLTEI400HSDK> lazy = new Lazy<PLTEI400HSDK>(() => new PLTEI400HSDK());
        public static PLTEI400HSDK Instance { get { return lazy.Value; } }

        #region IAxis接口
        /// <summary>
        /// 读取高速比较参数
        /// </summary>
        /// <param name="cardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器，取值范围：0~3（对应硬件 CMP0~CMP3 端口）</param>
        /// <param name="remained_points">返回可添加比较点数</param>
        /// <param name="current_point">返回当前比较点位置，单位：pulse</param>
        /// <param name="runned_points">返回已比较点数</param>
        /// <returns>错误代码</returns>
        public int GetCardHcmpCurrentState(int cardNo, int hcmp, ref int remained_points, ref int current_point, ref int runned_points)
        {
            return PLT.GetCardHcmpCurrentState(cardNo, hcmp, ref remained_points, ref current_point, ref runned_points);
        }
        /// <summary>
        ///设置插补运动速度曲线的平滑时间
        /// </summary>
        /// <param name="cardNo">卡号 </param>
        /// <param name="Crd">坐标系号，取值范围：0~5 </param>
        /// <param name="s_mode">保留参数，固定值为 0</param>
        /// <param name="s_para">平滑时间，单位：s，范围：0~1</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardVectorSProfile(int cardNo, int Crd, int s_mode, double s_para)
        {
            return PLT.DmcSetCardVectorSProfile(cardNo, Crd, s_mode, s_para);
        }

        /// <summary>
        /// 获取单轴运动速度曲线  
        /// </summary>
        /// <param name="cardNo">控制卡卡号 </param>
        /// <param name="axisNo">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="Min_Vel">返回起始速度，单位：pulse/s (最大值为 2M)</param>
        /// <param name="Max_Vel">返回最大速度，单位：pulse/s (最大值为 2M)</param>
        /// <param name="Tacc">返回加速时间，单位：s (最小值为 0.001s)</param>
        /// <param name="Tdec">返回减速时间，单位：s (最小值为 0.001s)</param>
        /// <param name="stop_vel">返回停止速度，单位：pulse/s (最大值为 2M)</param>
        /// <returns>：错误代码</returns>
        public int GetCardAxisProfile(int cardNo, int axisNo, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double stop_vel)
        {
            return PLT.GetCardAxisProfile(cardNo, axisNo, ref Min_Vel, ref Max_Vel, ref Tacc, ref Tdec, ref stop_vel);
        }

        /// <summary>
        /// 设置单轴速度曲线 S 段参数值
        /// </summary>
        /// <param name="cardNo">控制卡卡号 </param>
        /// <param name="axisNo">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="s_mode">保留参数，固定值为 0</param>
        /// <param name="s_para">返回设置的 S 段时间，单位：s；范围：0~0.5 s</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisSProfile(int cardNo, int axisNo, int s_mode, ref double s_para)
        {
            return GetCardAxisSProfile(cardNo, axisNo, s_mode, ref s_para);
        }

        /// <summary>
        /// 设置一维位置比较器 (单轴高速)
        /// <para>注 意：</para><para>DMC3C00 后四轴不支持位置比较功能</para>
        /// </summary>
        /// <param name="cardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400ASDK：0~3</param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能 </param>
        /// <param name="cmp_source">   比较源，0：指令位置计数器，1：编码器计数器</param>
        /// <returns>错误代码</returns>
        public int OpenCardHighCompareConfig(int cardNo, int hcmp, int axis, int cmp_source, int cmp_logic, long time)
        {
            return OpenCardHighCompareConfig(cardNo, hcmp, axis, cmp_source, cmp_logic, time);
        }



        /// <summary>
        /// 打开位置比较输出
        /// </summary>
        /// <returns></returns>
        public int OpenCardCompareConfig(int cardNo, int axisNo, int enable, int cmp_source)
        {
            return OpenCardCompareConfig(cardNo, axisNo, enable, cmp_source);
        }


        /// <summary>
        /// 轴所属卡的名字。
        /// </summary>
        public string AxisWhichCardName { get; set; }

        /// <summary>
        /// 轴所属卡的类型（有些系列卡，可以用此属性区分具体是哪一种卡）
        /// </summary>
        public string AxisWhichcardType { get; set; }

        /// <summary>
        /// 轴所属的卡号（在多卡的时候轴所属的卡号就不再是0了）
        /// </summary>
        public int AxisWhichCardNo { get; set; }

        /// <summary>
        /// 轴名字
        /// </summary>
        public string AxisName { get; set; }

        /// <summary>
        /// 这个轴功能的注释
        /// </summary>
        public string AxisNotes { get; set; }

        /// <summary>
        /// 轴ID，用于控制卡SDK函数识别运动轴号
        /// </summary>
        public int AxisID { get; set; }


        /// <summary>
        /// 虚拟卡(当用户勾选虚拟卡后，轴对象这里需要知道当前是虚拟卡)
        /// </summary>
        public bool IsVitualCard { get; set; }

        /// <summary>
        /// 轴状态, 0轴停止,1轴运行中
        /// </summary>
        public int AxisStatus { get; set; }

        /// <summary>
        /// 正极限信号，1表示正限位有效， 0表示无效
        /// </summary>
        public int AxisPEL { get; set; }
        /// <summary>
        /// 负极限信号，1表示负限位有效， 0表示无效
        /// </summary>
        public int AxisMEL { get; set; }
        /// <summary>
        /// 原点信号，1表示原点有效， 0表示无效
        /// </summary>
        public int AxisORG { get; set; }
        /// <summary>
        /// Index信号，1表示EZ为高电平， 0表示为低电平
        /// </summary>
        public int AxisEZ { get; set; }
        /// <summary>
        /// EMG信号，1表示EMG输入为高， 0表示输入为低；该信号为低电平有效。
        /// </summary>
        public int AxisEMG { get; set; }

        /// <summary>
        /// 报警信号, 1为报警  
        /// </summary>
        public int AxisAlarm { get; set; }

        /// <summary>
        /// 是否允许编码器
        /// </summary>
        public bool EncodeEnable { get; set; }

        /// <summary>
        /// 用户定义的运动目标位置
        /// </summary>
        public double TargetPos { get; set; }

        /// <summary>
        /// 用户定义的运动目标位置
        /// </summary>
        public double TargetVel { get; set; }



        public CAxisParameter AxisParameter { get; set; }

        /// <summary>
        /// 设置回零遇限位是否反找。（DMC3C00，3400A 默认使能，DMC3800，3600 默认不使能）
        /// </summary>
        /// <param name="cardNo"></param>
        /// <param name="axisNo"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        public int SetCardAxisHomeElReturn(int cardNo, int axisNo, ushort enable)
        {
            return PLT.SetCardAxisHomeElReturn(cardNo, axisNo, enable);
        }


        /// <summary>
        /// 在线改变指定轴的当前运动速度
        /// </summary>
        /// <param name="cardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="curr_Vel">改变后的运动速度，单位：pulse/s</param>
        /// <param name="taccDec">保留参数，固定值为 0</param>
        /// <returns>错误代码</returns>
        public int CardAxisChangeSpeed(int cardNo, int axis, double curr_Vel, double taccDec)
        {
            return PLT.CardAxisChangeSpeed(cardNo, axis, curr_Vel, taccDec);
        }

        /// <summary>
        /// 获取坐标系的运动状态
        /// </summary>
        /// <param name="cardNo">控制卡卡号</param>
        /// <param name="crd">指定控制卡上的坐标系号（取值范围：0~1）</param>
        /// <returns>：坐标系状态，0：正在使用中，1：正常停止</returns>
        public int GetCardCheckDoneMulticoor(int cardNo, int crd)
        {
            return PLT.GetCardCheckDoneMulticoor(cardNo, crd);
        }

        /// <summary>
        /// 停止坐标系内所有轴的运动
        /// </summary>
        /// <param name="cardNo"></param>
        /// <param name="crd"></param>
        /// <param name="stop_mode"></param>
        /// <returns></returns>
        public int CardAxisStopMulticoor(int cardNo, int crd, int stop_mode)
        {
            return PLT.CardAxisStopMulticoor(cardNo, crd, stop_mode);
        }


        /// <summary>
        /// 函数调用打印输出设置
        /// <para>注 意：</para>
        /// <para>使能打印输出后，可监控运动函数库的调用情况。在用户调用函数时，将输出相关信息，并保存在指定文件路径中；函数设置模式 2 全部不打印需配合最新动态库（20181030 及以后动态库）使用。</para>
        /// </summary>
        /// <param name="cardNo">打印输出模式，0：只打印报错函数，1：全部打印，2：全部不打印</param>
        /// <param name="fileName">文件保存路径：参数文件名+后缀：相对路径 ；完整描述参数文件的路径+文件名后缀：绝对路径</param>
        /// <returns>错误代码</returns>
        public int SetCardDebugMode(int cardNo, string fileName)
        {
            return PLT.SetCardDebugMode(cardNo, fileName);
        }

        /// <summary>
        /// 读取指定轴的伺服使能端口的电平
        /// </summary>
        /// <returns>伺服使能端口电平，0：低电平(on)，1：高电平(off)</returns>
        public int GetCardAxisSevonPin(int cardNo, int axisNo)
        {
            return PLT.GetCardAxisSevonPin(cardNo, axisNo);
        }

        public int SetCardAxisHcmp2dsetEnable(int cardNo, int hcmp, int cmpEnable)
        {
            return PLT.SetCardAxisHcmp2dsetEnable(cardNo, hcmp, cmpEnable);
        }
        /// <summary>
        /// 控制指定轴的伺服使能端口的输出
        /// </summary>
        /// <param name="cardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="on_off">设置伺服使能端口电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int CardAxisWriteSevonPin(int cardNo, int axisNo, int on_off)
        {
            return PLT.CardAxisWriteSevonPin(cardNo, axisNo, on_off);
        }





        /// <summary>
        ///  设置编码器位置
        /// </summary>
        /// <param name="cardNo"></param>
        /// <param name="axisNo"></param>
        /// <param name="encoder_value"></param>
        public int SetCardAxisEncoder(int cardNo, int axisNo, int encoder_value)
        {
            return PLT.SetCardAxisEncoder(cardNo, axisNo, encoder_value);
        }

        /// <summary>
        /// 设置轴回零的参数
        /// </summary>
        public int SetCardAxisHomeProfile(HomeParameModel hpm)
        {
            return PLT.SetCardAxisHomeProfile(hpm.CardNo, hpm.Axis, hpm.HomeDir, hpm.HighVel, hpm.LowVel, hpm.TaccTime, hpm.HomeMode, hpm.OrgLevel);
        }

        /// <summary>
        /// 设置回零模式
        /// LTDMC.dmc_set_homemode((ushort)CardNo, (ushort)axis, (ushort)home_dir, vel, (ushort)mode, (ushort)EZ_count);
        /// </summary>
        public int SetCardAxisHomeMode(HomeParameModel hpm)
        {
            return PLT.SetCardAxisHomeMode(hpm.CardNo, hpm.Axis, hpm.HomeMode);
        }

        /// <summary>
        /// 设置软限位
        /// </summary>
        public int SetCardAxisSoftLimit(LimitParamModel lpm)
        {
            return PLT.SetCardAxisSoftLimit(lpm.CardNo, lpm.Axis, lpm.Nlimit, lpm.Plimit, lpm.Enable, lpm.SLAction);
        }

        /// <summary>
        /// 设置轴的硬限位
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisELLimit(LimitParamModel lpm)
        {
            //return PLT.SetCardAxisELLimit(lpm.CardNo, lpm.Axis, lpm.Nlimit, lpm.Plimit, lpm.Enable, lpm.SLAction);
            return 0;
        }

        /// <summary>
        /// 设置轴初始速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInitVel(int cardNo, int axisNo, double min_vel)
        {
            return PLT.SetCardAxisInitVel(cardNo, axisNo, min_vel);
        }

        /// <summary>
        /// 设置轴的最大速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisMaxVel(int cardNo, int axisNo, double Max_Vel)
        {
            return PLT.SetCardAxisMaxVel(cardNo, axisNo, Max_Vel);
        }

        /// <summary>
        /// 设置轴的加速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisAccVel(int cardNo, int axisNo, double tacc)
        {
            return PLT.SetCardAxisAccVel(cardNo, axisNo, tacc);
        }

        /// <summary>
        /// 设置单轴运动速度
        /// </summary>
        /// <param name="mpm">运动参数模型
        /// <para>"CardNo"卡号</para>
        /// <para>"axis"指定轴号</para>
        /// <para>"Min_Vel"起始速度，单位：unit/s</para>
        /// <para>"Max_Vel"最大速度，单位：unit/s</para>
        /// <para>"Tacc"加速时间，单位：s</para>
        /// <para>"Tdec"减速时间，单位：s</para>
        /// <para>"Stop_Vel">停止速度，单位：unit/s</para></param>
        /// <returns>错误代码</returns>
        public int SetCardAxisMotionalVel(MotionParamModel mpm)
        {
            return PLT.SetCardAxisMotionalVel(mpm.CardNo, mpm.Axis, mpm.MinVel, mpm.MaxVel, mpm.TaccVel, mpm.TdecVel, mpm.StopVel, mpm.SPara);
        }

        /// <summary>
        /// 设置轴的加加速
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisJerkVel()
        {
            return PLT.SetCardAxisJerkVel();
        }

        /// <summary>
        /// 设置轴的减减速
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisSlowVel()
        {
            return PLT.SetCardAxisSlowVel();
        }

        /// <summary>
        /// 设置轴的高速回零速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisHighHomeVel(int cardNo, int axisNo, double High_Vel)
        {
            return PLT.SetCardAxisHighHomeVel(cardNo, axisNo, High_Vel);
        }

        /// <summary>
        /// 设置轴的高速回零加速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisHighHomeAccVel(int cardNo, int axisNo, double Home_Acc)
        {
            return PLT.SetCardAxisHighHomeAccVel(cardNo, axisNo, Home_Acc);
        }

        /// <summary>
        /// 设置轴的高速回零减速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisHighHomeDecVel(int cardNo, int axisNo, double Home_Dec)
        {
            return PLT.SetCardAxisHighHomeDecVel(cardNo, axisNo, Home_Dec);
        }

        /// <summary>
        /// 设置轴的低速回零速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisLowHomeVel(int cardNo, int axisNo, double Low_Vel)
        {
            return PLT.SetCardAxisLowHomeVel(cardNo, axisNo, Low_Vel);
        }

        /// <summary>
        /// 设置轴的低速回零加速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisLowHomeAccVel(int cardNo, int axisNo, double Home_Acc)
        {
            return PLT.SetCardAxisLowHomeAccVel(cardNo, axisNo, Home_Acc);
        }

        /// <summary>
        /// 设置轴的低速回零减速度
        /// </summary>
        /// <returns></returns>
        //public int SetCardAxisLowHomeDecVel()
        //{
        //    return PLT.SetCardAxisLowHomeDecVel();
        //}

        /// <summary>
        /// 设置轴的低速回零减速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisLowHomeDecVel(int CardNo, int axisNo, double homeMode)
        {
            return PLT.SetCardAxisLowHomeDecVel(CardNo, axisNo, homeMode);
        }
        public int Plt_AxConfigHomeParms(int cardNo, int axisNo, PLT.struct_home_config_parms homeparms)
        {
            //homeparms.home_high_vel = highHomeSpeed;//回零高速，单位：【pulse/s】,取值范围（0,4000000】
            //homeparms.home_low_vel = homeparms.home_high_vel;//回零高速，单位：【pulse/s】,取值范围（0,4000000】
            //homeparms.home_acc = 100;//回零加减速度, 单位：【pulse/s2】,取值范围（0,4000000000】
            //homeparms.home_mode = Convert.ToUInt16(homeMode);//0:原点捕获回零;1：EZ锁存回零;2:原点+EZ锁存回零 3：反向找EZ锁存回零 4：一次回零 5：一次回零加反找回零 6：二次回零 7：原点加EZ回零 8：ez回零 9：反向找EZ回零
            //homeparms.org_level = 0;//原点有效电平，0：低电平有效；1：高电平有效
            //homeparms.org_ltc_source = 0;//原点锁存源，0：理论位置 1：编码器位置
            //homeparms.ez_level = 0;//ez有效电平，0：低电平有效；1：高电平有效
            //homeparms.ez_ltc_source = 0;//ez锁存源，0：理论位置 1：编码器位置
            //homeparms.org_ltc_level = 0;//0：上升沿锁存原点  1：下降沿锁存原点
            //homeparms.ez_ltc_level = 0;//0：上升沿锁存ez 1：下降沿锁存ez
            return PLT.Plt_AxConfigHomeParms((ushort)cardNo, (ushort)axisNo, homeparms);
        }



        /// <summary>
        /// 设置轴的回零位置偏差
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisHomePositionErro()
        {
            return PLT.SetCardAxisHomePositionErro();
        }

        /// <summary>
        /// 设置轴的反向回零距离
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisReverseHomeDis()
        {
            return PLT.SetCardAxisReverseHomeDis();
        }

        /// <summary>
        /// 设置轴的回零最大保护距离
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisHomeMaxProtectDis()
        {
            return PLT.SetCardAxisHomeMaxProtectDis();
        }

        /// <summary>
        /// 设置轴的当前位置
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisCurrentPosition(int cardNo, int axisNo, int pos)
        {
            return PLT.SetCardAxisCurrentPosition(cardNo, axisNo, pos);
        }

        /// <summary>
        /// 读取轴回零的参数
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="axis"></param>
        /// <param name="home_mode"></param>
        /// <param name="Low_Vel"></param>
        /// <param name="High_Vel"></param>
        /// <param name="Tacc"></param>
        /// <param name="Tdec"></param>
        /// <param name="offsetpos"></param>
        /// <returns></returns>
        public int GetCardAxisHomeProfile()
        {
            return GetCardAxisHomeProfile();
        }

        /// <summary>
        /// 读取轴回零的模式
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="axis"></param>
        /// <param name="home_dir"></param>
        /// <param name="vel"></param>
        /// <param name="mode"></param>
        /// <param name="EZ_count"></param>
        /// <returns></returns>
        public int GetCardAxisHomeMode(int cardNo, int axisNo, int Home_Mde)
        {
            return PLT.GetCardAxisHomeMode(cardNo, axisNo, ref Home_Mde);
        }

        /// <summary>
        /// 获取轴的硬限位
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisELLimit(int cardNo, int axisNo, ref int enable, ref int slAction, ref int activeLevel)
        {
            return PLT.GetCardAxisELLimit(cardNo, axisNo, ref enable, ref slAction, ref activeLevel);
        }

        /// <summary>
        /// 读取轴的软限位设置
        /// </summary>
        /// <returns>限位参数模型</returns>
        public LimitParamModel GetCardAxisSoftLimit(LimitParamModel lpm)
        {
            //return PLT.GetCardAxisSoftLimit();
            return new LimitParamModel();
        }

        /// <summary>
        /// 读取轴的初速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisInitVel(int cardNo, int AxisNo, ref double pSpeed)
        {
            return PLT.GetCardAxisInitVel(cardNo, AxisNo, ref pSpeed);
        }
        /// <summary>
        /// 读取轴的最大速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisMaxVel(int cardNo, int axisNo, ref double Max_Vel)
        {
            return PLT.GetCardAxisMaxVel(cardNo, axisNo, ref Max_Vel);
        }

        /// <summary>
        /// 读取单轴运动速度
        /// <para>注 意：</para>
        /// <para>该函数不适用于连续插补</para>
        /// </summary>
        /// <param name="mpm">运动参数
        /// <para>"CardNo"卡号</para>
        /// <para>"axis"指定轴号</para>
        /// <para>"MinVel"返回起始速度设置，单位：unit/s</para>
        /// <para>"MaxVel"返回最大速度设置，单位：unit/s</para>
        /// <para>"TaccVel"返回加速时间设置，单位：s</para>
        /// <para>"TdecVel"返回减速时间设置，单位：s</para>
        /// <para>"StopVel"返回停止速度设置，单位：unit/s</para></param>
        /// <returns>错误代码</returns>
        public MotionParamModel GetCardAxisAccVel(MotionParamModel mpm)
        {
            //return PLT.GetCardAxisAccVel();
            return new MotionParamModel();
        }

        /// <summary>
        /// 设置轴的减速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisDecVel(int cardNo, int axisNo, double Tec)
        {
            return PLT.GetCardAxisDecVel(cardNo, axisNo, Tec);
        }

        /// <summary>
        /// 设置轴的加加速
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisJerkVel()
        {
            //return PLT.GetCardAxisJerkVel();
            return 0;
        }

        /// <summary>
        /// 设置轴的减减速
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisSlowVel()
        {
            //return PLT.GetCardAxisSlowVel();
            return 0;
        }

        /// <summary>
        /// 读取轴的高速回零速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisHighHomeVel(int cardNo, int axisNo, ref double Home_High_Vel)
        {
            return PLT.GetCardAxisHighHomeVel(cardNo, axisNo, ref Home_High_Vel);
        }

        /// <summary>
        /// 读取轴的高速回零加速度
        /// </summary>
        /// <returns></returns>
        public HomeParameModel GetCardAxisHighHomeProfile(HomeParameModel hpm)
        {
            //double homeAcc = hpm.TaccTime;
            //return PLT.GetCardAxisHighHomeProfile(hpm.CardNo,hpm.Axis,ref homeAcc);
            return new HomeParameModel();
        }

        /// <summary>
        /// 读取轴的高速回零减速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisHighHomeDecVel(int cardNo, int axisNo, ref double Home_Acc)
        {
            return PLT.GetCardAxisHighHomeDecVel(cardNo, axisNo, ref Home_Acc);
        }

        /// <summary>
        /// 读取轴的低速回零速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisLowHomeVel(int cardNo, int axisNo, ref double Home_Low_Vel)
        {
            return PLT.GetCardAxisLowHomeVel(cardNo, axisNo, ref Home_Low_Vel);
        }

        /// <summary>
        /// 读取轴的低速回零加速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisLowHomeAccVel(int cardNo, int axisNo, ref double Home_Acc)
        {
            return PLT.GetCardAxisLowHomeAccVel(cardNo, axisNo, ref Home_Acc);
        }

        /// <summary>
        /// 读取轴的低速回零减速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisLowHomeDecVel(int cardNo, int axisNo, ref double Home_Acc)
        {
            return PLT.GetCardAxisLowHomeDecVel(cardNo, axisNo, ref Home_Acc);
        }

        /// <summary>
        /// 读取轴的回零位置偏差
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisHomePositionErro()
        {
            return PLT.GetCardAxisHomePositionErro();
        }

        /// <summary>
        /// 读取轴的反向回零距离
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisReverseHomeDis()
        {
            return PLT.GetCardAxisReverseHomeDis();
        }

        /// <summary>
        /// 读取轴的回零最大保护距离
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisHomeMaxProtectDis()
        {
            return PLT.GetCardAxisHomeMaxProtectDis();
        }

        /// <summary>
        /// 读取轴的当前位置
        /// </summary>
        /// <param name="cntr_no">出脉冲计数器/编码器反馈脉冲位置计数器选择:  0: 输出脉冲计数器 |  1: 编码器反馈脉冲位置计数器</param>
        /// <returns>-1返回错误，返回大于0的值为轴的当前位置</returns>
        public double GetCardAxisCurrentPosition(int cntr_no, int cardNo, int axisNo)
        {
            return PLT.GetCardAxisCurrentPosition(cntr_no, cardNo, axisNo);
        }

        /// <summary>
        /// 读取轴状态,停止还是运行中
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴号</param>
        /// <returns>轴状态
        /// </returns>
        public int GetCardAxisCurrentState(int cardNo, int axisNo)
        {
            return PLT.GetCardAxisCurrentState(cardNo, axisNo);
        }




        /// <summary>
        /// 读取轴相关的IO的状态。包括正负限位、原点、报警等轴相关IO信号
        /// </summary>
        public int GetCardAxisAlarmState(int cardNo, int axis)
        {
            return PLT.GetCardAxisAlarmState(cardNo, axis);
        }

        /// <summary>
        /// 读取轴的限位状态
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisLimitState()
        {
            //return PLT.GetCardAxisLimitState();
            return 0;
        }

        /// <summary>
        /// 清除轴的报警状态
        /// </summary>
        /// <returns></returns>
        public int ClearCardAxisAlarmState(int CardNo, int axis)
        {
            return PLT.ClearCardAxisAlarmState(CardNo, axis);
        }
        /// <summary>
        /// 清除所有已添加的高速位置比较器
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="hcmp"></param>
        /// <returns></returns>
        public int ClearCardHcmpPoints(int CardNo, int hcmp)
        {
            return PLT.ClearCardHcmpPoints(CardNo, hcmp);
        }
        /// <summary>
        /// 打开轴的使能
        /// </summary>
        /// <returns></returns>
        public int OpenCardAxisEnable(int CardNo, int axis)
        {
            return PLT.OpenCardAxisEnable(CardNo, axis);
        }


        /// <summary>
        /// 设置 EtherCAT 总线驱动器失能
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴的轴号，255 表示使能所有 EtherCAT 轴</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisDisable(int CardNo, int axis)
        {
            return PLT.SetCardAxisDisable(CardNo, axis);
        }

        /// <summary>
        /// 设置轴的运行模式
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisRunMode(int CardNo, int axis, ushort runmode)
        {
            return PLT.SetCardAxisRunMode(CardNo, axis, runmode);
        }

        /// <summary>
        /// 获取轴当前的运行模式
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisRunMode(int CardNo, int axis)
        {
            return PLT.GetCardAxisRunMode(CardNo, axis);
        }

        /// <summary>
        /// 设置轴的脉冲当量
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisPulseEquival(int CardNo, int axis, int equiv)
        {
            return PLT.SetCardAxisPulseEquival(CardNo, axis, equiv);
        }


        /// <summary>
        /// 获取轴的脉冲当量
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisPulseEquival(int CardNo, int axis)
        {
            return PLT.GetCardAxisPulseEquival(CardNo, axis);
        }

        /// <summary>
        /// 设置轴的S速度模式
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisSProfile(MotionParamModel mpm)
        {
            return PLT.SetCardAxisSProfile(mpm.CardNo, mpm.Axis, mpm.SMode, mpm.SPara);
        }

        /// <summary>
        /// 设置轴的T速度模式
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisTProfile(MotionParamModel mpm)
        {
            double MoveSpeed = new double();
            MoveSpeed = Convert.ToDouble(mpm.MaxVel);
            PLT.struct_vel_plan_parms speedVel = new PLT.struct_vel_plan_parms();
            speedVel.start_vel = mpm.MinVel / 1000;
            speedVel.max_vel = mpm.MaxVel / 1000;
            speedVel.end_vel = mpm.StopVel / 1000;
            speedVel.acc = mpm.TaccVel; // mpm.MaxVel / 100.0;
            speedVel.dec = mpm.TdecVel; // mpm.MaxVel / 100.0;

            return PLT.Plt_AxSetvelParms((ushort)mpm.CardNo, (ushort)mpm.Axis, speedVel);//设置速度
            //return PLT.SetCardAxisTProfile();
        }

        /// <summary>
        /// 驱动指定的轴回零，并立即返回。 回零是否完成，可以通过查询轴状态来完成。
        /// </summary>
        /// <returns></returns>
        public int CardAxisHomeMove(int cardNo, int axisNo)
        {
            PLT.struct_home_config_parms homeparms = new PLT.struct_home_config_parms();
            homeparms.home_high_vel = 20; //Convert.ToDouble(textBox19.Text);//回零高速，单位：【pulse/s】,取值范围（0,4000000】
            homeparms.home_low_vel = 5; //homeparms.home_high_vel;//回零高速，单位：【pulse/s】,取值范围（0,4000000】
            homeparms.home_acc = 1;//回零加减速度, 单位：【pulse/s2】,取值范围（0,4000000000】
            homeparms.home_mode = 5; // Convert.ToUInt16(home_mode.SelectedIndex);//0:原点捕获回零;1：EZ锁存回零;2:原点+EZ锁存回零 3：反向找EZ锁存回零 4：一次回零 5：一次回零加反找回零 6：二次回零 7：原点加EZ回零 8：ez回零 9：反向找EZ回零
            homeparms.org_level = 0;//原点有效电平，0：低电平有效；1：高电平有效
            homeparms.org_ltc_source = 0;//原点锁存源，0：理论位置 1：编码器位置
            homeparms.ez_level = 0;//ez有效电平，0：低电平有效；1：高电平有效
            homeparms.ez_ltc_source = 0;//ez锁存源，0：理论位置 1：编码器位置
            homeparms.org_ltc_level = 0;//0：上升沿锁存原点  1：下降沿锁存原点
            homeparms.ez_ltc_level = 0;//0：上升沿锁存ez 1：下降沿锁存ez
            short iret = PLT.Plt_AxConfigHomeParms((ushort)cardNo, (ushort)axisNo, homeparms);
            return PLT.CardAxisHomeMove(cardNo, axisNo);
        }


        /// <summary>
        /// 读取回零执行状态
        /// </summary>
        /// <param name="cardNo"></param>
        /// <param name="axisNo"></param>
        /// <param name="status">回零执行状态，1：回零完成，0：回零未完成</param>
        /// <returns></returns>
        public int CardAxisGetHomeResult(int cardNo, int axisNo, ref ushort status)
        {
            return PLT.CardAxisGetHomeResult(cardNo, axisNo, ref status);
        }

        /// <summary>
        /// 启动轴的点位运动
        /// </summary>
        /// <returns></returns>
        public int CardAxisPointMovement(MotionParamModel mpm)
        {
            if (mpm.Dist >= 0)
            {
                return PLT.CardAxisPointMovement(mpm.CardNo, mpm.Axis, (long)mpm.Dist, mpm.PosiMode);
            }
            else
            {
                return PLT.CardAxisPointMovement(mpm.CardNo, mpm.Axis, 0 - Math.Abs((long)mpm.Dist), mpm.PosiMode);
            }
        }

        /// <summary>
        /// 启动轴的连续运动
        /// </summary>
        /// <returns></returns>
        public int CardAxisSerialMovement(MotionParamModel mpm)
        {
            return PLT.CardAxisSerialMovement(mpm.CardNo, mpm.Axis, mpm.Dir);
        }

        /// <summary>
        /// 停止轴的运动
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">轴号</param>
        /// <param name="stop_mode">0减速停,1急停</param>
        /// <returns></returns>
        public int StopCardAxisMovement(int CardNo, int axis, int stop_mode)
        {
            return PLT.StopCardAxisMovement(CardNo, axis, stop_mode);
        }

        /// <summary>
        /// 建立插补坐标系
        /// </summary>
        /// <returns></returns>
        public int CreatCardAxisInterpolateCoord()
        {
            return PLT.CreatCardAxisInterpolateCoord();
        }

        /// <summary>
        /// 清除插补坐标系
        /// </summary>
        /// <returns></returns>
        public int ClearCardAxisInterpolateCoord()
        {
            return PLT.ClearCardAxisInterpolateCoord();
        }

        /// <summary>
        /// 设置插补参数 
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInterpolateVectorProfile(InterpolateMotetionParamModel imp)
        {
            // return PLT.SetCardAxisInterpolateVectorProfile()
            return 0;
        }

        /// <summary>
        /// 设置插补初始速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInterpolateMaxVel()
        {
            return PLT.SetCardAxisInterpolateMaxVel();
        }


        /// <summary>
        /// 设置插补减速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInterpolateDecVel()
        {
            return PLT.SetCardAxisInterpolateDecVel();
        }

        /// <summary>
        /// 设置插补加加速
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInterpolateJerkVel()
        {
            return PLT.SetCardAxisInterpolateJerkVel();
        }

        /// <summary>
        /// 设置插补低速
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInterpolateSlowVel()
        {
            return PLT.SetCardAxisInterpolateSlowVel();
        }

        /// <summary>
        /// 设置直线插补的速度
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public int CardAxisLineUnitSpeed(LineInterpolateparamModel parameter)
        {
            return 0;
        }

        /// <summary>
        /// 启动两轴平面直线插补
        /// </summary>
        /// <returns></returns>
        public int CardAxisLineUnit(LineInterpolateparamModel line)
        {
            return 0;
        }

        /// <summary>
        /// 启动圆弧插补
        /// <para>基于圆心圆弧扩展的螺旋线插补运动（圆心终点式圆弧/螺旋线/渐开线 ）（可作两轴圆弧插补） </para>
        /// <para>注  意：</para> 
        /// <para> 1） 当轴数为 2 时，轴列表前两轴进行平面螺旋</para>
        /// <para> 2） 当轴数为 3、运动轨迹为螺旋插补时，轴列表前两轴平面为基面，进行平面螺旋插补；同时，轴列表第三轴运动指定高度，该轴终点位置与该轴起点位置的差值为螺旋线段相对于基面的高度</para>
        /// <para> 3） 当轴数大于 3、运动轨迹为螺旋插补时，列表前三轴进行螺旋插补的同时，后续轴做线性跟随运动，运动时间与前三轴的运动时间相等</para>
        /// <para> 4） 当运动轨迹为螺旋插补时：</para>
        ///  <para>   轴列表前两轴组成的基面上，当起始点到圆心的距离小于终点到圆心的距离，为绽放螺旋线</para>
        ///  <para>   轴列表前两轴组成的基面上，当起始点到圆心的距离大于终点到圆心的距离，为收敛螺旋线</para>
        ///  <para>   轴列表前两轴组成的基面上，当起始点到圆心的距离等于终点到圆心的距离，为圆弧插补（插补轴数为 3 时则为圆柱螺旋线）</para>
        /// </summary>
        public int CardAxisArcMoveCenterUnit(ArcInterpolateparamModel arc)
        {
            return 0;
        }


        /// <summary>
        /// 启动多轴空间直线插补
        /// </summary>
        /// <returns></returns>
        public int CardAxesLineUnit(LineInterpolateparamModel line)
        {
            return 0;
        }

        /// <summary>
        /// 启动多轴空间圆弧插补
        /// </summary>
        /// <returns></returns>
        public int CardAxesArcMoveCenterUnit(ArcInterpolateparamModel arc)
        {
            return 0;
        }

        /// <summary>
        /// 启动两轴平面连续直线插补
        /// </summary>
        /// <returns></returns>
        public int CardAxisContiLineUnitStart(LineInterpolateparamModel line)
        {
            return 0;
        }

        /// <summary>
        /// 启动两轴平面连续圆弧插补
        /// </summary>
        /// <returns></returns>
        public int CardAxisContiArcMoveStart(ArcInterpolateparamModel arc)
        {
            return 0;
        }

        /// <summary>
        /// 设置连续插补前瞻模式
        /// </summary>
        /// <param name="impm">插补运动参数（打开关闭前瞻）
        /// <para>"CardNo"控制卡卡号</para>
        /// <para>"Crd"坐标系号，取值范围：0~5</para>
        /// <para>"enable"比较功能状态，0：禁止，1：使能</para>
        /// <para>"LookaheadSegments"</para>
        /// <para>"PathError"</para>
        /// <para>"LookaheadAcc"</para></param>
        /// <returns></returns>
        public int SetCardContiLookaheadMode(InterpolateMotetionParamModel impm)
        {
            return 0;
        }

        /// <summary>
        /// 设置一维位置比较：开启\关闭(单轴高速)
        /// </summary>
        /// <returns></returns>
        public int SetCardHcmpEnable(int CardNo, int axis, int hcmp, int cmp_mode = 0)
        {
            return PLT.SetCardHcmpEnable(CardNo, axis, hcmp, cmp_mode);
        }
        /// <summary>
        /// 按索引号读取 PC 缓冲区中已保存的锁存值
        /// <para>注 意：</para><para>当选择锁存方式为连续锁存时，用此函数读取锁存值。索引号按锁存顺序从 0 开始排列（即第一次锁存的位置值存在索引号为 0 处，第二次锁存的位置值存在索引号为 1处，以此类推）</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <param name="Index">索引号</param>
        /// <returns>锁存值</returns>
        public int GetCardAxisLatchValueExtern(int CardNo, int axis, int Index)
        {
            return PLT.GetCardAxisLatchValueExtern(CardNo, axis, Index);
        }
        /// <summary>
        /// 从 PC 缓存中读取锁存器已锁存个数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <returns></returns>
        public int GetCardAxisLatchFlagExtern(int CardNo, int axis)
        {
            return PLT.GetCardAxisLatchFlagExtern(CardNo, axis);
        }

        /// <summary>
        /// 设置一维位置比较器 (单轴低速)
        /// <para>注 意：</para><para>DMC3C00 后四轴不支持位置比较功能</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能 </param>
        /// <param name="cmp_source">   比较源，0：指令位置计数器，1：编码器计数器</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisCompareConfig(int CardNo, int axis, int enable, int cmp_source)
        {
            return PLT.SetCardAxisCompareConfig(CardNo, axis, enable, cmp_source);
        }

        /// <summary>
        /// 设置二维位置比较输出
        /// </summary>
        /// <returns></returns>
        public int SetCardCompareConfigExtern(int CardNo, int enable, int cmp_source)
        {
            return PLT.SetCardCompareConfigExtern(CardNo, enable, cmp_source);
        }
        /// <summary>
        /// 配置高速比较器关联
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="hcmp"></param>
        /// <param name="axis"></param>
        /// <param name="cmp_source"></param>
        /// <param name="cmp_logic"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public int SetCardAxisHcmpConfig(int CardNo, int hcmp, int axis, int cmp_source, int cmp_logic, int time)
        {
            return PLT.SetCardAxisHcmpConfig(CardNo, hcmp, axis, cmp_source, cmp_logic, time);
        }
        /// <summary>
        /// 添加/更新高速比较位置
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="hcmp"></param>
        /// <param name="cmp_pos"></param>
        /// <returns></returns>
        public int AddCardHcmpPoint(int CardNo, int hcmp, int cmp_pos)
        {
            return PLT.AddCardHcmpPoint(CardNo, hcmp, cmp_pos);
        }
        /// <summary>
        /// 设置高速比较线性模式参数    
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器，取值范围：0~3（对应硬件 CMP0~CMP3 端口）</param>
        /// <param name="Increment">位置增量值，单位：pulse（正值表示位置递增，负值表示位置递减）</param>
        /// <param name="Count">比较次数，取值范围：1~32767</param>
        /// <returns>错误代码</returns>
        public int SetCardHcmpLiner(int CardNo, int hcmp, int Increment, int Count)
        {
            return 0;
        }
        /// <summary>
        /// 设置锁存方式
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="axis"></param>
        /// <param name="all_enable"></param>
        /// <param name="latch_source"></param>
        /// <param name="triger_chunnel"></param>
        /// <returns></returns>
        public int SetCardAxisLatchMode(int CardNo, int axis, int all_enable, int latch_source, int triger_chunnel)
        {
            return PLT.SetCardAxisLatchMode(CardNo, axis, all_enable, latch_source, triger_chunnel);
        }
        /// <summary>
        /// 复位指定卡的锁存器的标志位
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        public int GetCardAxisReserLatchFlag(int CardNo, int axis)
        {
            return PLT.GetCardAxisReserLatchFlag(CardNo, axis);
        }
        /// <summary>
        /// 从控制卡内读取锁存器的值
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        public int GetCardAxisLatchValue(int CardNo, int axis)
        {
            return PLT.GetCardAxisLatchValue(CardNo, axis);
        }
        /// <summary>
        /// 从控制卡内读取指定卡内锁存器的标志位
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <returns>0：未触发锁存，1：已触发锁存</returns>
        public int GetCardAxisLatchFlag(int CardNo, int axis)
        {
            return PLT.GetCardAxisLatchFlag(CardNo, axis);
        }

        /// <summary>
        /// 读取指定 CMP 端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器，取值范围：0~3（对应硬件 CMP0~CMP3 端口）</param>
        /// <returns>CMP 端口电平</returns>
        public int GetCardHcmpCmpPin(int CardNo, int hcmp)
        {
            return PLT.GetCardHcmpCmpPin(CardNo, hcmp);
        }

        public int SetCardHcmpCmpPin(int CardNo, int hcmp, int on_off)
        {
            return PLT.SetCardHcmpCmpPin(CardNo, hcmp, on_off);
        }
        /// <summary>
        /// 设置指定轴的 LTC 信号
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="axis"></param>
        /// <param name="ltc_logic"></param>
        /// <param name="ltc_mode"></param>
        /// <param name="filter"></param>
        public int SetCardAxisLtcMode(int CardNo, int axis, UInt16 ltc_logic, UInt16 ltc_mode, double filter)
        {
            return PLT.SetCardAxisLtcMode(CardNo, axis, ltc_logic, ltc_mode, filter);
        }
        /// <summary>
        /// 设置一维位置比较输出数据
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisCompareConfigDate(int CardNo, int axis, int enable, int cmp_source)
        {
            return PLT.SetCardAxisCompareConfigDate(CardNo, axis, enable, cmp_source);
        }

        /// <summary>
        /// 设置二维位置比较输出数据
        /// </summary>
        /// <returns></returns>
        public int SetCardCompareConfigExternDate(PositionComparatorParamModel pcpm)
        {
            return 0;
        }

        /// <summary>
        /// 设置轴的螺距补偿
        /// </summary>
        /// <returns></returns>
        public int SetCardLeadScrewCompConfig(PitchCompensationparamModel pcm)
        {
            return 0;
        }

        /// <summary>
        /// 打开轴的螺距补尝
        /// </summary>
        /// <returns></returns>
        public int SetCardLeadScrewCompEnable(int CardNo, int axis, int enable)
        {
            return PLT.SetCardLeadScrewCompEnable(CardNo, axis, enable);
        }


        /// <summary>
        /// 错误时设置停止模式(减速停止、突然停止)
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="stop_mode">停止模式(0:减速停止、1:突然停止)</param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardAxisErrorStopMode(int axis, int stop_mode)
        {
            return PLT.SetCardAxisErrorStopMode(axis, stop_mode);
        }

        /// <summary>
        /// 设定告警信号有效电平，可以为高电平有效，也可以为低电平有效，默认为低电平有效。 DI0—3 可在硬件上连接 XYZU 轴的告警 ALM 信号。用此函数可查询轴告警状态电平
        /// </summary>
        /// <param name="axis">轴号 </param>
        /// <param name="enable">告警信号使能</param>
        /// <param name="active_level">限位信号电平选择  <para>0 低电平有效</para>  <para>1 高电平有效</para></param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardAxisAlarm(int cardNo, int axis, int enable, int active_level)
        {
            return PLT.SetCardAxisAlarm(cardNo, axis, enable, active_level);
        }

        /// <summary>
        /// 设置正负原点的限位
        /// </summary>
        /// <param name="axis">轴</param>
        /// <param name="ORGPlusPos">原点限位</param>
        /// <param name="PELAlarmPlusPos">正限位</param>
        /// <param name="NELAlarmPlusPos">负限位</param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetSpacing(int axis, int ORGPlusPos, int PELAlarmPlusPos, int NELAlarmPlusPos)
        {
            return PLT.SetSpacing(axis, ORGPlusPos, PELAlarmPlusPos, NELAlarmPlusPos);
        }

        /// <summary>
        /// 设置轴参数状态
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="f">状态 true:置为1，false:置为0</param>
        /// <param name="kind">轴包含的元素</param>
        /// <returns></returns>
        public int SetStatusParameter(int axis, bool f, AxisStatusParameterEnum kind)
        {
            return 0;
        }

        /// <summary>
        /// 返回支持直线插补的轴列表（轴索引）
        /// </summary>
        /// <returns></returns>
        public List<int> GetLineInterpolationSupportAxis()
        {
            return new List<int>();
        }

        #endregion




        #region  ICard接口

        /// <summary>
        /// 总线卡时，取总线错误
        /// </summary>
        /// <param name="ErrNum"></param>
        /// <returns></returns>
        public string GetEtherCATErrorInfo(int ErrNum)
        {
            return PLT.GetEtherCATErrorInfo(ErrNum);
        }

        /// <summary>
        /// 取SDK函数返回的字符串错误信息
        /// </summary>
        /// <param name="ErrNum"></param>
        /// <returns></returns>
        public string GetErrorInfo(int ErrNum)
        {
            return "";
        }

        /// <summary>
        /// 读取卡的当前状态(如果是总线卡，则读总线状态） 
        /// </summary>
        /// <param name="cardNo"></param>
        /// <param name="channel">对于总线卡，此值固定为2</param>
        /// <returns></returns>
        public int GetCardCurrentState(int cardNo, int channel)
        {
            return PLT.GetCardCurrentState(cardNo, channel);
        }


        /// <summary>
        /// 控制卡初始化函数
        /// </summary>
        /// <returns>卡的数量</returns>
        public int InitCard()
        {
            return 1;
            //return PLT.InitCard();
        }

        /// <summary>
        /// 获取当前卡的轴数 
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="TotalAxis">返回当前卡的轴数</param>
        /// <returns>错误代码</returns>
        public int GetCardTotalAxisNum(int CardNo, ref UInt32 TotalAxis)
        {
            return PLT.GetCardTotalAxisNum(CardNo, ref TotalAxis);
        }

        /// <summary>
        /// 设置 CAN 通讯状态
        /// <para>注 意：</para>
        /// <para>1）当关闭运动控制卡时，CAN 通讯不会被自动断开；当再次初始化运动控制卡时，  CAN-IO 通讯依然保持之前的状态；</para>
        /// <para>2）当连接 CAN 通讯时，必须使用 nmc_get_can_state 函数读取 CAN-IO 的通讯状态， 确认 CAN 通讯已正常连接。当连接出现异常时，可再次调用 nmc_set_can_state函数进行连接；</para>
        /// <para>3）设置波特率时需保证控制卡波特率与模块波特率相对应；</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeNum">CAN 节点数，取值范围：1~8</param>
        /// <param name="state">设置通讯状态，0：断开，1：连接</param>
        /// <param name="baud">设置控制卡波特率<para>波特率参数：0,1,2,3,4,5</para><para>对应波特率：1000Kbps，800Kbps,500Kbps,250Kbps,125Kbps,100Kbps</para></param>
        /// <returns></returns>
        public int SetCardCANIOConnectState(int CardNo, int NodeNum, int state, int baud)
        {
            return PLT.SetCardCANIOConnectState(CardNo, NodeNum, state, baud);
        }

        /// <summary>
        /// 读取 CAN 通讯状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeNum">返回 CAN 节点数</param>
        /// <param name="state">返回 CAN-IO 通讯状态，0：断开，1：连接，2：异常</param>
        /// <returns>错误代码</returns>
        public int GetCardCANIOConnectState(int CardNo, ref UInt16 NodeNum, ref UInt16 state)
        {
            return PLT.GetCardCANIOConnectState(CardNo, ref NodeNum, ref state);
        }

        /// <summary>
        /// 打开卡
        /// </summary>
        /// <param name="cardnum">返回初始化成功的卡数 </param>
        /// <param name="cardtypes">返回控制卡固件类型数组</param>
        /// <param name="CardID">返回控制卡硬件 ID 号数组，卡号按从小到大顺序排列  </param>
        /// <returns>错误代码，返回零为成功，大于0的数表示第几张卡打开出错</returns>
        public int OpenCard(ref ushort cardnum, ref uint[] cardtypes, ref ushort[] CardNos)
        {
            cardtypes[0] = (uint)54320;
            PLT.OpenCard(ref cardnum, cardtypes, CardNos);
            return 0;
        }

        /// <summary>
        /// 获取控制卡动态库文件版本号
        /// </summary>
        /// <param name="LibVer">返回库版本号</param>
        /// <returns>错误代码</returns>
        public int GetCardLibVersion(int CardID, ref int LibVer, ref int pDriver_ver, ref int pLogic_ver)
        {
            return PLT.GetCardLibVersion(CardID, ref LibVer, ref pDriver_ver, ref pLogic_ver);
        }

        /// <summary>
        /// 设置卡参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="PortNum">EtherCAT 端口号，固定为 2</param>
        /// <param name="Baudrate">参数不明等待回应</param>
        /// <param name="NodeCnt">参数不明等待回应</param>
        /// <param name="MasterId">参数不明等待回应</param>
        /// <returns>错误代码</returns>
        public int SetCardMasterParam(int CardID, int PortNum, int Baudrate, int NodeCnt, int MasterId)
        {
            return PLT.SetCardMasterParam(CardID, PortNum, Baudrate, NodeCnt, MasterId);
        }

        /// <summary>
        /// 获取卡参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="PortNum">EtherCAT 端口号，固定为 2</param>
        /// <param name="Baudrate">参数不明等待回应</param>
        /// <param name="NodeCnt">参数不明等待回应</param>
        /// <param name="MasterId">参数不明等待回应</param>
        /// <returns>错误代码</returns>
        public int GetCardMasterParam(int CardNo, int PortNum, ref ushort Baudrate, ref uint NodeCnt, ref ushort MasterId)
        {
            return PLT.GetCardMasterParam(CardNo, PortNum, ref Baudrate, ref NodeCnt, ref MasterId);
        }


        ///// <summary>
        ///// 设置从站的配置
        ///// </summary>
        ///// <returns></returns>
        //int SetCardSlaveStationConfig();


        /// <summary>
        /// 获取从站配置信息
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴号</param>
        /// <param name="SlaveAddr">EtherCAT 总线轴地址</param>
        /// <param name="Sub_SlaveAddr">EtherCAT 总线轴子地址</param>
        /// <returns>错误代码</returns>
        public int GetCardSlaveStationConfig(int CardNo, int axis, int lenght, ref ushort SlaveAddr, ref ushort Sub_SlaveAddr, ref int value)
        {
            return PLT.GetCardSlaveStationConfig(CardNo, axis, ref SlaveAddr, ref Sub_SlaveAddr);
        }

        /// <summary>
        /// 设置主站的扫描周期
        /// </summary>
        /// <returns></returns>
        public int SetCardMasterScanCycleTime(int CardNo, int FieldbusType, int CycleTime)
        {
            return PLT.SetCardMasterScanCycleTime(CardNo, FieldbusType, CycleTime);
        }

        /// <summary>
        /// 设置EtherCAT总线循环周期 
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="Fieldbustype">  EtherCAT 端口号，固定为 2</param>
        /// <param name="CycleTime">CAT 总线循环周期，单位：us，支持 250/500/1000/</param>
        /// <returns>错误代码</returns>
        public int GetCardMasterScanCycleTime(int CardNo, int FieldbusType, ref int CyclFieldbusTypeeTime)
        {
            return PLT.GetCardMasterScanCycleTime(CardNo, FieldbusType, ref CyclFieldbusTypeeTime);
        }

        /// <summary>
        /// 控制卡初始复位（适用于EtherCAT总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <returns>错误代码</returns>
        public int CardReset(int CardNo)
        {
            return PLT.CardReset(CardNo);
        }

        /// <summary>
        ///硬件复位（适用于所有脉冲/总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int CardBoardReset()
        {
            return PLT.CardBoardReset();
        }



        /// <summary>
        /// 总线卡总线复位
        /// </summary>
        /// <param name="CardNo"></param>
        /// <returns></returns>
        public int CardSoftReset(ushort CardNo)
        {
            return PLT.CardSoftReset(CardNo);
        }



        /// <summary>
        /// 控制卡冷复位（适用于所有脉冲/总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int CardCoolReset(ushort CardNo)
        {
            return PLT.CardCoolReset(CardNo);
        }

        /// <summary>
        /// 关闭控制卡（适用于所有脉冲/总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int CloseCard()
        {
            return PLT.CloseCard();
        }
        #endregion


        #region IIOIn 接口

        /// <summary>
        /// 获取所有输入端口的值dmc_read_inport
        /// </summary>
        /// <returns></returns>
        public int GetCardInPortValue(int CardNo, int portno)
        {
            return PLT.GetCardInPortValue(CardNo, portno);
        }

        /// <summary>
        /// 检测急停IO，如果触发则停止全部轴
        /// </summary>
        /// <param name="CardName"></param>
        /// <param name="EmgIO"></param>
        /// <returns></returns>
        public int EMGAction(string CardName, int cardNo, int EmgIO)
        {
            return PLT.EMGAction(CardName, cardNo, EmgIO);
        }


        /// <summary>
        /// 读取指定 EtherCAT 扩展模块的输入口组的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Channel">总线通道号，固定为 2</param>
        /// <param name="NoteID">EtherCAT 地址，从 1001 开始，按从站数往后累加</param>
        /// <param name="PortNo">输入组号（32 位一组，从 0 开始，如 0 表示 0-31 位，1 表示 32-63位）</param>
        /// <param name="IoValue">输入组各输入端口的值，bit0~bit31 的值分别代表第 0~31 号输入口的电平，1：低电平，0：高电平</param>
        /// <returns></returns>
        public int NmcReadInportExtern(int CardNo, int Channel, int NoteID, int PortNo, ref uint IoValue)
        {
            return PLT.NmcReadInportExtern(CardNo, Channel, NoteID, PortNo, ref IoValue);
        }

        /// <summary>
        /// 读取指定 CAN-IO 扩展模块的输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">端口号，0 表示 0~31 号口，1 表示 32~63 号口</param>
        /// <param name="IoValue">输出端口的值，bit0~bit31 的值分别代表第 0~31 号输出口的电平 </param>
        /// <returns>错误代码</returns>
        public uint GetCardCANIOInPort(int CardNo, int NodeID, int PortNo, ref UInt32 IoValue)
        {
            return (uint)PLT.GetCardCANIOInPort(CardNo, NodeID, PortNo, ref IoValue);
        }


        /// <summary>
        /// 设置主板上输入IO的端口的电平（虚拟运动卡专用）
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="bitno"></param>
        /// <param name="on_off"></param>
        /// <returns></returns>
        public int SetCardWriteInBit(int CardNo, int bitno, int on_off)
        {
            return PLT.SetCardWriteInBit(CardNo, bitno, on_off);
        }

        /// <summary>
        /// 设置主板上扩展模块输入IO的端口的电平（虚拟运动卡专用）
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="bitno"></param>
        /// <param name="nodeno"></param>
        /// <param name="on_off"></param>
        /// <returns></returns>
        public int SetCardWriteExtendInBit(int CardNo, int bitno, int nodeno, int on_off)
        {
            return PLT.SetCardWriteExtendInBit(CardNo, bitno, nodeno, on_off);
        }

        /// <summary>
        /// 读取指定输入端口的状态
        /// </summary>
        /// <returns></returns>
        public int GetCardInPortNoValue(int CardNo, int portno)
        {
            return PLT.GetCardInPortNoValue(CardNo, portno);
        }

        /// <summary>
        /// 查询 DI 端口的状态。 DI0—3 可在硬件上连接 XYZU 轴的告警 ALM 信号。用此函数可查询轴告警状态(Pci9014)
        /// </summary>
        /// <param name="card_no">卡号 </param>
        /// <param name="pData">DI 端口状态，其中的 bit 0 ~15 分别对应端子上的 DI0~DI15  <para> 当*pData 中的位为 0 时，对应端子输入低电平；</para> <para> 当*pData 中的位为 1 时，对应端子输入高电平；</para>    </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int GetCardDIState(int card_no, ref int pData)
        {
            return PLT.GetCardDIState(card_no, ref pData);
        }

        public int GetCardDIBitAlarmState(int card_no, int bit_no, ref int pData)
        {
            return PLT.GetCardDIBitAlarmState(card_no, bit_no, ref pData);
        }

        /// <summary>
        /// 设置卡Com口使能
        /// </summary>
        public int SetCardCompEnable(int card_no, int enable, int active_level, int ref_source, int length)
        {
            return PLT.SetCardCompEnable(card_no, enable, active_level, ref_source, length);
        }

        #endregion



        #region  IIOOut 接口

        /// <summary>
        /// 读取指定输出端口的状态
        /// </summary>
        /// <returns></returns>
        public int GetCardPortNoOutState(int CardNo, int bitno)
        {
            return PLT.GetCardPortNoOutState(CardNo, bitno);
        }
        /// <summary>
        /// 读取所有输出端口的状态
        /// </summary>
        /// <returns></returns>
        public int GetCardOutState(int CardNo, int portno)
        {
            return PLT.GetCardPortNoOutState(CardNo, portno);
        }


        /// <summary>
        /// 设置指定控制卡的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="bitno"></param>
        /// <param name="on_off"></param>
        /// <returns></returns>
        public int SetCardBitNoInBit(int CardNo, int bitno, int on_off)
        {
            return PLT.SetCardBitNoInBit(CardNo, bitno, on_off);
        }

        /// <summary>
        /// 读取指定 EtherCAT 扩展模块的输出口组的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Channel">总线通道号，固定为 2</param>
        /// <param name="NoteID">EtherCAT 地址，从 1001 开始，按从站数往后累加</param>
        /// <param name="PortNo">输出组号（32 位一组，从 0 开始，如 0 表示 0-31 位，1 表示 32-63位）</param>
        /// <param name="IoValue">输出组各输出端口的值，bit0 ~bit31 的值分别代表第 0~31 号输出口的电平，1：低电平，0：高电平</param>
        /// <returns>错误代码</returns>
        public int NmcReadOutportExtern(int CardNo, int Channel, int NoteID, int PortNo, ref uint IoValue)
        {
            return PLT.NmcReadOutportExtern(CardNo, Channel, NoteID, PortNo, ref IoValue);
        }


        /// <summary>
        /// 设置指定 EtherCAT 扩展模块的某个输出端口的电平(0：低电平，1：高电平)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Channel">总线通道号，固定为 2</param>
        /// <param name="NoteID">EtherCAT 地址，从 1001 开始，按从站数往后累加</param>
        /// <param name="IoBit">输出端口号</param>
        /// <param name="IoValue">指定输出端口的电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int NmcWriteOutbitExtern(int CardNo, int Channel, int NoteID, int IoBit, ushort IoValue)
        {
            return PLT.NmcWriteOutbitExtern(CardNo, Channel, NoteID, IoBit, IoValue);
        }

        /// <summary>
        /// 设置指定 CAN-IO 扩展模块的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="IoBit">输出口号</param>
        /// <param name="IoValue">输出电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int SetCardCANIOOutBit(int CardNo, int NodeID, int IoBit, int IoValue)
        {
            return PLT.SetCardCANIOOutBit(CardNo, NodeID, IoBit, IoValue);
        }


        /// <summary>
        /// 读取指定 CAN-IO 扩展模块的输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">端口号，0 表示 0~31 号口，1 表示 32~63 号口</param>
        /// <param name="IoValue">输出端口的值，bit0~bit31 的值分别代表第 0~31 号输出口的电平 </param>
        /// <returns></returns>
        public uint GetCardCANIOOutPort(int CardNo, int NodeID, int PortNo, ref UInt32 IoValue)
        {
            return (uint)PLT.GetCardCANIOOutPort(CardNo, NodeID, PortNo, ref IoValue);
        }

        /// <summary>
        /// 设置指定控制卡的全部输出口的电平
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="portno"></param>
        /// <param name="outport_val"></param>
        /// <returns></returns>
        public int SetCardPortNoOutPort(int CardNo, int portno, int outport_va)
        {
            return PLT.SetCardPortNoOutPort(CardNo, portno, outport_va);
        }

        /// <summary>
        /// 读取位置比较输出口的状态
        /// </summary>
        /// <returns></returns>
        public int GetLocationIsOutNoState()
        {
            return PLT.GetLocationIsOutNoState();
        }
        /// <summary>
        /// 设置位置比较输出口的状态
        /// </summary>
        /// <returns></returns>
        public int SetLocationIsOutNoState()
        {
            return PLT.SetLocationIsOutNoState();
        }

        /// <summary>
        /// 设置 DO 端口输出状态 
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public int SetCardDOOut(int CardNo, int data)
        {
            return PLT.SetCardDOOut(CardNo, data);
        }

        /// <summary>
        /// 设置 DO 端口中指定位的状态
        /// </summary>
        public int SetCardDOBitState(int card_no, int bit_no, int data)
        {
            return PLT.SetCardDOBitState(card_no, bit_no, data);
        }


        /// <summary>
        /// 读取 DO 端口的状态。 
        /// </summary>
        public int GetCardDOState(int card_no, ref int pData)
        {
            return PLT.GetCardDOState(card_no, ref pData);
        }

        #endregion
    }


}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
