﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/MCC141C/AxisRealization.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable

using System;
    using NoCodeMotion.Hardware.Motion;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NoCodeMotion.Services.Hardware.Cards.Families.MCC141C
{
    public class AxisRealization : MotionAxisBase
    {
        /// <summary>
        /// 轴名字
        /// </summary>
        public override string AxisName { get; set; }

        /// <summary>
        /// 轴功能的注释
        /// </summary>
        public override string AxisNotes { get; set; }

        /// <summary>
        /// 轴功能的注释
        /// </summary>
        public override string AxisNotes2 { get; set; }

        /// <summary>
        /// 轴所属的卡号（在多卡的时候轴所属的卡号就不再是0了）
        /// </summary>
        public override int AxisWhichCardNo { get; set; }

        /// <summary>
        /// 轴所属卡的名字。
        /// </summary>
        public override string AxisWhichCardName { get; set; }

        /// <summary>
        /// 轴的运动方式状态
        /// </summary>
        public override MotionParamModel AxisMotionStatus { get; set; }

        /// <summary>
        /// 轴所属卡的类型（有些系列卡，可以用此属性区分具体是哪一种卡）
        /// </summary>
        public override string AxisWhichcardType { get; set; }


        /// <summary>
        /// 轴ID，用于控制卡SDK函数识别运动轴号
        /// </summary>
        public override int AxisID { get; set; }
        /// <summary>
        /// 轴是否在线
        /// </summary>
        public override bool Online { get; set; }

        /// <summary>
        /// 虚拟卡(当用户勾选虚拟卡后，轴对象这里需要知道当前是虚拟卡)
        /// </summary>
        public override bool IsVirtualCard { get; set; } = false;

        /// <summary>
        /// 回原标志--上电默认标志为false，伺服必须进行一次复位
        /// </summary>
        public override bool HomeFlag { get; set; } = false;



        /// <summary>
        /// 是否允许编码器
        /// </summary>
        public override bool EncodeEnable { get; set; } = false;

        /// <summary>
        /// 用户定义的运动目标位置
        /// </summary>
        public override double TargetPos { get; set; } = 0.0;


        /// <summary>
        /// 用户定义的运动目标位置
        /// </summary>
        public override double TargetVel { get; set; } = 0.0;


        /// <summary>
        /// 取轴状态,0 表示控制轴运动完成，处于空闲状态;1 表示控制轴正在运动，其它值为调用出错
        /// </summary>
        public override int AxisStatus
        {
            get
            {

                return GetCardAxisCurrentState();
            }
            set
            {

            }
        }

        /// <summary>
        /// 正极限信号，1表示正限位有效， 0表示无效
        /// </summary>
        public override int AxisPEL
        {
            get
            {
                if (CConfigurationInfo.Instance.GetParameter(CConfigurationInfo.hardwareEnum.轴, AxisName,
                    CConfigurationInfo.hardwareObjStatusEnum.正限位PEL).isVisual)
                {
                    return CConfigurationInfo.Instance.GetAxisStatus(AxisName, CConfigurationInfo.hardwareObjStatusEnum.正限位PEL);
                }
                return (int)((GetAxisCurrentState() % 2));
            }
            set
            {

            }
        }

        /// <summary>
        /// 负极限信号，1表示负限位有效， 0表示无效
        /// </summary>
        public override int AxisMEL
        {
            get
            {
                if (CConfigurationInfo.Instance.GetParameter(CConfigurationInfo.hardwareEnum.轴, AxisName,
                    CConfigurationInfo.hardwareObjStatusEnum.负限位MEL).isVisual)
                {
                    return CConfigurationInfo.Instance.GetAxisStatus(AxisName, CConfigurationInfo.hardwareObjStatusEnum.负限位MEL);
                }
                return (int)((GetAxisCurrentState() >> 1) % 2);
            }
            set
            {

            }
        }

        /// <summary>
        /// 原点信号，1表示原点有效， 0表示无效
        /// </summary>
        public override int AxisORG
        {
            get
            {
                if (CConfigurationInfo.Instance.GetParameter(CConfigurationInfo.hardwareEnum.轴, AxisName,
                       CConfigurationInfo.hardwareObjStatusEnum.原点).isVisual)
                {
                    return CConfigurationInfo.Instance.GetAxisStatus(AxisName, CConfigurationInfo.hardwareObjStatusEnum.原点);
                }
                return (int)((GetAxisCurrentState() >> 2) % 2);
            }
            set
            {

            }
        }

        /// <summary>
        /// Index信号，1表示EZ为高电平， 0表示为低电平
        /// </summary>
        public override int AxisEZ
        {
            get
            {
                return (int)((GetAxisCurrentState() >> 3) % 2);
            }
            set
            {

            }
        }

        /// <summary>
        /// EMG信号，1表示EMG输入为高， 0表示输入为低；该信号为低电平有效。
        /// </summary>
        public override int AxisEMG
        {
            get
            {
                //todo: pci9014卡 急停信号总是为1，为了测试方便，这里暂时可以改0
                //return /*0; // */(int)((GetAxisCurrentState() >> 4) % 2);
                return (int)((GetAxisCurrentState() >> 4) % 2) == 1 ? 0 : 1;
            }
            set
            {

            }
        }

        /// <summary>
        /// 当前运动方向，0表示当前运动方向为正向， 1表示当前运动方向为负向。
        /// </summary>
        public int AxisDIR
        {
            get
            {
                return (int)((GetAxisCurrentState() >> 5) % 2);
            }
            set
            {

            }
        }

        /// <summary>
        /// 设置位置计数器 
        /// </summary>
        /// <param name="axis">轴号 </param>
        /// <param name="cntr_on">输出脉冲计数器/编码器反馈脉冲位置计数器选择:<para> 0 :输出脉冲计数器 </para><para>1: 编码器反馈脉冲位置计数器</para></param>
        /// <param name="Pos">要设置的值, 范围在-134217728 ~ 134217727。 </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值； </returns>
        public int SetCardAxisPos(int cntr_on, int Pos)
        {
            //return MCC141CSDK.Instance.SetCardAxisPos(AxisID, cntr_on, Pos);
            return 0;
        }

        /// <summary>
        /// 获取轴的报警信息（如果有硬件接入的情况下）
        /// </summary>
        public override int AxisAlarm
        {
            get
            {
                int level = 0;
                var res = MCC141CSDK.Instance.GetCardDIBit(AxisWhichCardNo, (uint)AxisID, ref level);
                return 0; // (int)level;
            }
            set
            {

            }
        }


        public override CAxisParameter AxisParameter { get; set; } = new CAxisParameter();

        /// <summary>
        /// 停止坐标系内所有轴的运动
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="crd"></param>
        /// <param name="stop_mode"></param>
        /// <returns></returns>
        public override int CardAxisStopMulticoor(int cardIndex, int crd, int stop_mode)
        {
            return 0;
        }


        /// <summary>
        /// 获取与轴相关I/O（如限位信号、原点信号）的状态
        /// </summary>
        /// <returns></returns>
        public int GetAxisCurrentState()
        {
            uint status = 0;
            var res = MCC141CSDK.Instance.GetAxisCardIOStatus(AxisWhichCardNo, AxisID, ref status);
            if (res == 0)
                return (int)status;
            else
                return res;
        }


        public override string GetErrorInfo(int errorNumber)
        {
            var res = MCC141CSDK.Instance.GetErrorInfo(errorNumber);
            return res;
        }





        /// <summary>
        /// 配置原点开关有效电平、编码器 index 信号有效电平、回零模式；这些参数在 p9014_home_move 函数中将使用到。 在模式 0（home_mode = 0）情况下，只需要 ORG 信号有效即可， 对 ez_level 可以给任意值； 而在 2 模式下，需要使用到编码器输入的 index 信号，因而需要指定 index 信号的有效电平; 
        /// </summary>
        /// <param name="hpm">参数
        /// <para>"axis"轴号 </para>
        /// <para>"mode"回零模式, 范围: 0~2;  <para>home_mode = 0 只有 ORG 有效，没有加速过程。 有效的 ORG 信号立即使控制轴立即停止运动，停止过程没有减速；在 ORG 有效的边沿，位置计数器被清零；  </para>  <para>   home_mode =2  ORG 和 index 信号同时有效，启动没有加速过程。 ORG 信号有效后，然后收到有效的 index 信号，控制轴停止运动，原点查找完成；原点查找结束后，位置计数器被清零；</para></para>
        /// <para>"org_level"原点信号的有效电平； 0 – 低有效； 1 – 高有效； </para>
        /// <para>"ez_level"编码器的 index 信号有效电平；0 – 低有效； 1 – 高有效；</para> </param>
        /// <returns> 正常返回 0；  出现错误时返回非 0 值；</returns>
        public override int SetCardAxisHomeProfile(HomeParameModel hpm)
        {
            return MCC141CSDK.Instance.SetCardAxisHomeConfig(hpm.Axis, hpm.HomeMode, hpm.OrgLevel, hpm.EZlevel);
        }


        ///// <summary>
        ///// 设置回零模式 SetCardAxisHomeProfile
        ///// </summary>
        ///// <returns></returns>
        //public int SetCardAxisHomeMode(HomeParameModel hpm)
        //{
        //    //SetCardAxisHomeProfile();已经设置过
        //    return 0;
        //}
        /// <summary>
        /// 设置指定轴的回零模式（适用于所有脉冲卡） (PCI9014板卡该功能 需要把数据放到对应的数据结构对象存储以便后续进行方向更改)
        /// <para>注意：</para>
        /// <para>1）当回零模式mode=4 时，回零速度模式将固定为低速回零</para>
        /// <para>2) DMC3C00后四轴只支持 0、1、2、10、11、12 六种回零模式</para>
        /// <para>3) 后三种回零模式最新固件（3XX201611 及以后固件）才支持。正向回零时进行正限位回零，负向回零时进行负限位回零； 若开始回零时处于限位信号中，会先向设置的 回零方向的反向运动，移出限位信号范围后，再变向，找相应的限位信号；一次限位回 零遇到限位信号后急停；一次限位回零加反找在反找阶段遇到限位信号后急停；二次限位回零在第二次遇到限位信号后急停； </para>
        /// </summary>
        /// <param name="CardNo">卡号（默认属性）</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400ASDK：0~3 </param>
        /// <param name="home_dir">回零方向，0：负向，1：正向 </param>
        /// <param name="vel"> 回零速度模式：0：低速回零，即以本指令前面的 dmc_set_profile 函数设置的起始速度运行 1：高速回零，即以本指令前面的 dmc_set_profile 函数设置的最大速度运行</param>
        /// <param name="mode">回零模式：0：一次回零  | 1：一次回零加回找  | 2：二次回零  | 3：一次回零后再记一个同向 EZ 脉冲进行回零  | 4：记一个 EZ 脉冲进行回零  | 5：原点加反向 EZ  | 6：原点锁存  | 7：原点锁存加同向 EZ 锁存  | 8：单独记一个 EZ 锁存  | 9：原点锁存加反向 EZ 锁存 | 10.一次限位回零 | 11.	一次限位回零加反找 | 12.	二次限位回零</param>
        /// <param name="EZ_count">保留参数，固定值为 0</param>
        /// <returns>错误代码</returns>
        public override int SetCardAxisHomeMode(HomeParameModel hpm)
        {
            //SetCardAxisHomeProfile();已经设置过
            if (MCC141CSDK.Instance.homeConfigArray == null || MCC141CSDK.Instance.homeConfigArray.Count() < (4 * (hpm.CardNo + 1))) MCC141CSDK.Instance.homeConfigArray = new HomeParameModel[4 * (hpm.CardNo + 1)];
            MCC141CSDK.Instance.homeConfigArray[hpm.Axis] = hpm;
            return 0;
        }


        /// <summary>
        /// 设置软限位（无此功能）
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisSoftLimit(LimitParamModel lpm)
        {
            return -1;
        }


        /// <summary>
        /// 设置轴的硬限位
        /// </summary>
        /// <param name="lpm">参数
        /// <para>"Axis"轴号 </para>
        ///<para>"ActiveLevel" 限位信号电平选择</para> <para>0 低电平有效</para> <para>1 高电平有效 </para> 
        /// </param>
        /// <returns> 正常返回 0；  出现错误时返回非 0 值；</returns>
        public override int SetCardAxisELLimit(LimitParamModel lpm)
        {
            return MCC141CSDK.Instance.SetCardAxisELLevel(lpm.Axis, lpm.ActiveLevel);
        }


        /// <summary>
        /// 错误时设置停止模式(减速停止、突然停止)
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="stop_mode">停止模式(0:减速停止、1:突然停止)</param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public override int SetCardAxisErrorStopMode(int axis, int stop_mode)
        {
            return MCC141CSDK.Instance.SetCardAxisErrorStopMode(axis, stop_mode);
        }


        /// <summary>
        /// 设置轴初始速度
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisInitVel()
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的最大速度
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisMaxVel()
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的加速度
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisAccVel()
        {
            return 0;
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
        public override int SetCardAxisMotionalVel(MotionParamModel mpm)
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的加加速
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisJerkVel()
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的减减速
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisSlowVel()
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的高速回零速度
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisHighHomeVel()
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的高速回零加速度
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisHighHomeAccVel()
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的高速回零减速度
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisHighHomeDecVel()
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的低速回零速度
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisLowHomeVel()
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的低速回零加速度
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisLowHomeAccVel()
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的低速回零减速度
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisLowHomeDecVel()
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的回零位置偏差
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisHomePositionErro()
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的反向回零距离
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisReverseHomeDis()
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的回零最大保护距离
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisHomeMaxProtectDis()
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的当前位置
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisCurrentPosition(int cardIndex, int axis, int pos)
        {
            return MCC141CSDK.Instance.SetCardAxisPos(cardIndex, axis, pos);
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
        public override int GetCardAxisHomeProfile()
        {
            return 0;
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
        public override int GetCardAxisHomeMode()
        {
            return 0;
        }


        /// <summary>
        /// 获取轴的硬限位
        /// </summary>
        /// <returns></returns>
        public override int GetCardAxisELLimit()
        {
            return 0;
        }


        /// <summary>
        /// 读取轴的软限位设置
        /// </summary>
        /// <returns>限位参数模型</returns>
        public override LimitParamModel GetCardAxisSoftLimit(LimitParamModel lpm)
        {
            return new LimitParamModel();
        }


        /// <summary>
        /// 读取轴的初速度
        /// </summary>
        /// <returns></returns>
        public override int GetCardAxisInitVel(/*int CardNo,int Axis, */ref int pSpeed)
        {
            if (IsVirtualCard)
            {
                pSpeed = 0; return 0;
            }
            double pspeed = 0;
            int result = MCC141CSDK.Instance.GetCardAxisCurrentSpeed(AxisID, ref pspeed);
            pSpeed = (int)pspeed;
            return result;
        }


        /// <summary>
        /// 读取轴的最大速度
        /// </summary>
        /// <returns></returns>
        public override int GetCardAxisMaxVel()
        {
            return 0;
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
        public override MotionParamModel GetCardAxisAccVel(MotionParamModel mpm)
        {
            return new MotionParamModel();
        }


        /// <summary>
        /// 设置轴的减速度
        /// </summary>
        /// <returns></returns>
        public override int GetCardAxisDecVel()
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的加加速
        /// </summary>
        /// <returns></returns>
        public override int GetCardAxisJerkVel()
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的减减速
        /// </summary>
        /// <returns></returns>
        public override int GetCardAxisSlowVel()
        {
            return 0;
        }


        /// <summary>
        /// 读取轴的高速回零速度
        /// </summary>
        /// <returns></returns>
        public override int GetCardAxisHighHomeVel()
        {
            return 0;
        }


        /// <summary>
        /// 读取轴的高速回零加速度
        /// </summary>
        /// <returns></returns>
        public override HomeParameModel GetCardAxisHighHomeProfile(HomeParameModel hpm)
        {
            return new HomeParameModel();
        }


        /// <summary>
        /// 读取轴的高速回零减速度
        /// </summary>
        /// <returns></returns>
        public override int GetCardAxisHighHomeDecVel()
        {
            return 0;
        }


        /// <summary>
        /// 读取轴的低速回零速度
        /// </summary>
        /// <returns></returns>
        public override int GetCardAxisLowHomeVel()
        {
            return 0;
        }


        /// <summary>
        /// 读取轴的低速回零加速度
        /// </summary>
        /// <returns></returns>
        public override int GetCardAxisLowHomeAccVel()
        {
            return 0;
        }


        /// <summary>
        /// 读取轴的低速回零减速度
        /// </summary>
        /// <returns></returns>
        public override int GetCardAxisLowHomeDecVel()
        {
            return 0;
        }


        /// <summary>
        /// 读取轴的回零位置偏差
        /// </summary>
        /// <returns></returns>
        public override int GetCardAxisHomePositionErro()
        {
            return 0;
        }


        /// <summary>
        /// 读取轴的反向回零距离
        /// </summary>
        /// <returns></returns>
        public override int GetCardAxisReverseHomeDis()
        {
            return 0;
        }


        /// <summary>
        /// 读取轴的回零最大保护距离
        /// </summary>
        /// <returns></returns>
        public override int GetCardAxisHomeMaxProtectDis()
        {
            return 0;
        }


        /// <summary>
        /// 读取控制轴的位置计数器，该计数器可以为输出脉冲计数器(cntr_no = 0)或者编码器反馈脉冲位置计数器(cntr_no = 1)；其对应的位置分别为指令脉冲位置（逻辑位置），或者编码器反馈脉冲位置（实际位置）。  
        /// </summary>
        /// <param name="cntr_no">输出脉冲计数器/编码器反馈脉冲位置计数器选择:  0: 输出脉冲计数器 |  1: 编码器反馈脉冲位置计数器</param>
        /// <returns>位置计数器的值, 范围在-134217728 ~ 134217727</returns>
        public override double GetCardAxisCurrentPosition(int cntr_no/*, int Axis*/)
        {
            if (IsVirtualCard)
            {
                return 0;
            }
            int pos = 0;
            //因为返回值代表计数器位置，而不是返回GetCardAxisPos的错误码
            //所以写成下面这样
            if (MCC141CSDK.Instance.GetCardAxisPos(AxisWhichCardNo, AxisID, cntr_no, ref pos) == 0)
            {
                return pos;
            }
            return -1;
        }


        /// <summary>
        /// 读取轴状态
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="pStatus">状态</param>
        /// <returns>轴状态,0 表示控制轴运动完成，处于空闲状态;1 表示控制轴正在运动，其它值为调用出错</returns>
        public override int GetCardAxisCurrentState()
        {
            if (IsVirtualCard)
            {
                return 0;
            }
            uint u = 0;
            int axis = AxisID;
            var res = MCC141CSDK.Instance.GetCardAxisMotionStatus(AxisWhichCardNo, axis, ref u);
            if (res == 0)
                return (int)u;
            else
                return -1;
        }


        /// <summary>
        /// 读取卡的当前状态
        /// </summary>
        /// <returns></returns>
        public override int GetCardCurrentState(int cardIndex, int channel)
        {
            return 0;
        }


        /// <summary>
        /// 读取轴的状态
        /// </summary>
        public override int GetCardAxisAlarmState(int cardIndex, int axis)
        {
            return 0;
        }


        /// <summary>
        /// 设定告警信号有效电平，可以为高电平有效，也可以为低电平有效，默认为低电平有效。 DI0—3 可在硬件上连接 XYZU 轴的告警 ALM 信号。用此函数可查询轴告警状态电平
        /// </summary>
        /// <param name="axis">轴号 </param>
        /// <param name="enable">告警信号使能</param>
        /// <param name="active_level">限位信号电平选择  <para>0 低电平有效</para>  <para>1 高电平有效</para></param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public override int SetCardAxisAlarm(int axis, int enable, int active_level)
        {
            return MCC141CSDK.Instance.SetCardAxisAlarm(axis, enable, active_level);
        }


        /// <summary>
        /// 读取轴的限位状态
        /// </summary>
        /// <returns></returns>
        public override int GetCardAxisLimitState()
        {
            return 0;
        }


        /// <summary>
        /// 清除轴的报警状态
        /// </summary>
        /// <returns></returns>
        public override int ClearCardAxisAlarmState(int cardIndex, int axis)
        {
            return 0;
        }


        /// <summary>
        /// 打开轴的使能
        /// </summary>
        /// <returns></returns>
        public override int OpenCardAxisEnable(int cardIndex, int axis)
        {
            return 0;
        }


        /// <summary>
        /// 设置 EtherCAT 总线驱动器失能
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴的轴号，255 表示使能所有 EtherCAT 轴</param>
        /// <returns>错误代码</returns>
        public override int SetCardAxisDisable(int cardIndex, int axis)
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的运行模式
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisRunMode(int cardIndex, int axis, ushort runmode)
        {
            return 0;
        }


        /// <summary>
        /// 获取轴当前的运行模式
        /// </summary>
        /// <returns></returns>
        public override int GetCardAxisRunMode(int cardIndex, int axis)
        {
            return 0;
        }


        /// <summary>
        /// 设置轴的脉冲当量
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisPulseEquival(int cardIndex, int axis, int equiv)
        {
            return 0;
        }


        /// <summary>
        /// 获取轴的脉冲当量
        /// </summary>
        /// <returns></returns>
        public override int GetCardAxisPulseEquival(int cardIndex, int axis)
        {
            return 0;
        }

        /// <summary>
        ///  设置单轴速度曲线 S 段参数值，并设置相应的起始速度、最大速度、加速度、减速度参数。 
        /// <para>调用该函数后，该轴的点位运动、连续运动、回零运动、减速停止将使用这些速度、加速度参数进行驱动。</para> 
        /// </summary>
        /// <param name="mpm">参数模型
        /// <para>"axis">轴号</para>
        /// <para>"start_vel">启动速度，单位: pps, 最大值为 1000000pps </para>
        /// <para>"max_vel">最大速度, 单位: pps, 最大值为 1000000pps </para>
        /// <para>"acc">加速度，单位： pps/s, 最大值为 1000000000pps/s </para>
        /// <para>"dec">减速度，单位: pps/s, 最大值为 1000000000pps/s </para>
        /// <para>"jerk_percent">过程使用 S 曲线的比例，范围为 0~1; 0 表示没有 S 曲线部分(也就是 T 型曲线加减速) | 1 表示全部 S 曲线加减速；</para> </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public override int SetCardAxisSProfile(MotionParamModel mpm)
        {
            return MCC141CSDK.Instance.SetCarsAxisSProfile(mpm.CardNo, mpm.Axis, mpm.MinVel, mpm.MaxVel, mpm.TaccVel, mpm.TdecVel, mpm.JerkPercent);
        }


        /// <summary>
        /// 配置控制轴使用 T 型速度曲线加减速，并设置相应的起始速度、最大速度、加速度、减速度参数。 
        /// <para>调用该函数后，该轴的点位运动、连续运动、回零运动、减速停止将使用这些速度、加速度参数进行驱动。</para> 
        /// </summary>
        /// <param name="mpm">运动参数模型
        /// <para>"Axis">轴号</para>
        /// <para>"MinVel">启动速度，单位: pps, 最大值为 1000000pps </para>
        /// <para>"MaxVel">最大速度, 单位: pps, 最大值为 1000000pps </para>
        /// <para>"TaccVel">加速度，单位： pps/s, 最大值为 1000000000pps/s </para>
        /// <para>"TdecVel">减速度，单位: pps/s, 最大值为 1000000000pps/s </para></param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public override int SetCardAxisTProfile(MotionParamModel mpm)
        {
            return MCC141CSDK.Instance.SetCarsAxisTProfile(mpm.CardNo, mpm.Axis, mpm.MinVel, mpm.MaxVel, mpm.TaccVel, mpm.TdecVel);
        }


        public override int CardAxisWriteSevonPin(int on_off)
        {
            return 0;
        }


        /// <summary>
        /// 驱动指定的轴回零，并立即返回。 回零是否完成，可以通过 p9014_motion_done 查询状态来完成。
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="plus_dir">原点查找的运动方向；<para> pulsDir = 0  控制轴反向运动启动原点查找</para><para>plusDir = 1  控制轴正向运动启动原点查找</para> </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public override int CardAxisHomeMove()
        {
            if (MCC141CSDK.Instance.homeConfigArray[AxisID].HomeMode == 41 || MCC141CSDK.Instance.homeConfigArray[AxisID].HomeMode == 2)
            {

                MCC141CSDK.Instance.CardAxisHomeMove((int)MCC141CSDK.Instance.homeConfigArray[AxisID].CardNo, AxisID, 0);
                MCC141CSDK.Instance.CardAxisPMove((int)MCC141CSDK.Instance.homeConfigArray[AxisID].CardNo, AxisID, (int)MCC141CSDK.Instance.homeConfigArray[AxisID].HomeTrigNegMoveDir, 0, 1);
                Thread.Sleep(1000); //二次回原，第一次回原后延时在执行一次回原
                SetCardAxisTProfile(new MotionParamModel
                {
                    Axis = MCC141CSDK.Instance.homeConfigArray[AxisID].Axis,
                    CardNo = MCC141CSDK.Instance.homeConfigArray[AxisID].CardNo,
                    MinVel = 10000,
                    MaxVel = 11000,
                    TaccVel = 0.1,
                    TdecVel = 0.1
                });
                return MCC141CSDK.Instance.CardAxisHomeMove((int)MCC141CSDK.Instance.homeConfigArray[AxisID].CardNo, AxisID, 0);
            }
            else
                return MCC141CSDK.Instance.CardAxisHomeMove((int)MCC141CSDK.Instance.homeConfigArray[AxisID].CardNo, AxisID, 0);

            //return MCC141CSDK.Instance.CardAxisHomeMove(AxisID,0);
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
        /// <param name="mpm">参数  
        /// <para>"Axis">轴号 </para>
        /// <para>"Dist">驱动的距离（相对于当前位置）；</para>
        /// <para>"PosiMode">dist 参数的坐标模式， 0 表示相对坐标； 1 表示绝对坐标；</para>
        /// <para>"RunMode">速度模式<para>0 :表示以 start_vel 为速度进行驱动，中间没有加速过程；</para><para>1: 表示以 max_vel 为速度进行驱动，中间没有加速过程；</para><para>2: 表示从 start_vel 加速到 max_vel,  中间有加速、减速过程；</para></para></param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public override int CardAxisPointMovement(MotionParamModel mpm)
        {
            return MCC141CSDK.Instance.CardAxisPMove(mpm.CardNo, mpm.Axis, (int)mpm.Dist, mpm.PosiMode, mpm.RunMode);
        }


        /// <summary>
        /// 启动控制轴进行连续驱动，运动方向由 plus_dir 决定
        /// </summary>
        /// <param name="mpm">参数
        /// <para>"Axis">轴号</para>
        /// <para>"Dir">驱动方向， 1 为正向驱动， 0 为反向驱动；</para>
        /// <para>"RunMode">速度模式<para>0 :表示以 start_vel 为速度进行驱动，中间没有加速过程；</para><para>1: 表示以 max_vel 为速度进行驱动，中间没有加速过程；</para><para>2: 表示从 start_vel 加速到 max_vel,  中间有加速、减速过程；</para></para></param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public override int CardAxisSerialMovement(MotionParamModel mpm)
        {
            return MCC141CSDK.Instance.CardAxisVMove(mpm.CardNo, mpm.Axis, mpm.Dir, mpm.RunMode);
        }


        /// <summary>
        /// 使控制轴减速停止或立即停止。
        /// </summary>
        /// <param name="CardNo">卡号，PCI9014中，不用设置</param>
        /// <param name="axis">轴号</param>
        /// <param name="emg_stop"><para>如果控制轴减速停止(EmgStop = 0)，将使用 p9014_set_t_profile 中的 dec 参数进行减速，减速到 start_vel 后，停止运动。</para><para>  如果控制轴立即停止(EmgStop = 1)，则没有减速过程，立即停止运动。</para> </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public override int StopCardAxisMovement(int cardIndex, int axis, int emg_stop)
        {
            return MCC141CSDK.Instance.CardAxisStop(cardIndex, axis, emg_stop);
        }

        /// <summary>
        /// 设置减速轴停止时间
        /// </summary>
        /// <param name="axis">卡号</param>
        /// <param name="CardNo">轴号</param>
        /// <param name="stopTime">停止时间</param>
        public override int SetAxisStopTime(int cardIndex, int axis, double stopTime)
        {
            //return DMC1000SSDK.Instance.DmcSetCardAxisDecStopTime(CardNo, axis, stopTime);
            return -1;
        }
        /// <summary>
        /// 建立插补坐标系
        /// </summary>
        /// <returns></returns>
        public override int CreatCardAxisInterpolateCoord(InterpolateMotetionParamModel imp)
        {
            return 0;
        }


        /// <summary>
        /// 清除插补坐标系
        /// </summary>
        /// <returns></returns>
        public override int ClearCardAxisInterpolateCoord()
        {
            return 0;
        }


        /// <summary>
        /// 设置插补参数 
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisInterpolateVectorProfile(InterpolateMotetionParamModel imp)
        {
            return 0;
        }


        /// <summary>
        /// 设置插补初始速度
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisInterpolateMaxVel()
        {
            return 0;
        }



        /// <summary>
        /// 设置插补减速度
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisInterpolateDecVel()
        {
            return 0;
        }


        /// <summary>
        /// 设置插补加加速
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisInterpolateJerkVel()
        {
            return 0;
        }


        /// <summary>
        /// 设置插补低速
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisInterpolateSlowVel()
        {
            return 0;
        }


        ///// <summary>
        ///// 启动两轴平面直线插补
        ///// </summary>
        ///// <returns></returns>
        //public int CardAxisLineUnit(LineInterpolateparamModel line)
        //{
        //    return 0;
        //}


        /// <summary>
        /// 直线插补运动
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">指定控制卡上的坐标系号；取值范围：0~1</param>
        /// <param name="axisNum">插补轴数，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5DMC3400ASDK：0~3</param>
        /// <param name="axisList">插补轴列表</param>
        /// <param name="DistList">插补轴目标位置列表，单位：pulse</param>
        /// <param name="posi_mode">运动模式，0：相对坐标模式，1：绝对坐标模式</param>
        /// <returns></returns>
        public override int CardAxisLineUnit(LineInterpolateparamModel line)
        {
            ushort[] axisNumbers = new ushort[line.Axis.Length];
            int[] targetPosition = new int[line.TargetPos.Length];

            for (int i = 0; i < line.Axis.Length; i++)
            {
                axisNumbers[i] = (ushort)line.Axis[i];
                targetPosition[i] = (int)line.TargetPos[i];
            }
            return MCC141CSDK.Instance.CardAxisLineMulticoor(line.CardNo, line.Crd, line.Axis.Length, axisNumbers, targetPosition, line.PosiMode, (int)line.minVel, (int)line.maxVel, line.tacc);

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
        public override int CardAxisArcMoveCenterUnit(ArcInterpolateparamModel arc)
        {
            return 0;
        }


        /// <summary>
        /// 启动多轴空间直线插补
        /// </summary>
        /// <returns></returns>
        public override int CardAxesLineUnit(LineInterpolateparamModel line)
        {
            return 0;
        }


        /// <summary>
        /// 启动多轴空间圆弧插补
        /// </summary>
        /// <returns></returns>
        public override int CardAxesArcMoveCenterUnit(ArcInterpolateparamModel arc)
        {
            return 0;
        }


        /// <summary>
        /// 启动两轴平面连续直线插补
        /// </summary>
        /// <returns></returns>
        public override int CardAxisContiLineUnitStart(LineInterpolateparamModel line)
        {
            return 0;
        }


        /// <summary>
        /// 启动两轴平面连续圆弧插补
        /// </summary>
        /// <returns></returns>
        public override int CardAxisContiArcMoveStart(ArcInterpolateparamModel arc)
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
        public override int SetCardContiLookaheadMode(InterpolateMotetionParamModel impm)
        {
            return 0;
        }


        /// <summary>
        /// 设置一维位置比较：开启\关闭(单轴高速)
        /// </summary>
        /// <returns></returns>
        public override int SetCardHcmpEnable(int cardIndex, int axis, int enable, int cmp_source)
        {
            return 0;
        }


        /// <summary>
        /// 设置一维位置比较输出
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisCompareConfig()
        {
            return 0;
        }


        /// <summary>
        /// 设置二维位置比较输出
        /// </summary>
        /// <returns></returns>
        public override int SetCardCompareConfigExtern(int cardIndex, int enable, int cmp_source)
        {
            return 0;
        }

        /// <summary>
        /// 打开位置比较输出
        /// </summary>
        /// <returns></returns>
        public override int OpenCardCompareConfig(int cardIndex, int axis, int enable, int cmp_source)
        {
            return 0;
        }


        /// <summary>
        /// 设置一维位置比较输出数据
        /// </summary>
        /// <returns></returns>
        public override int SetCardAxisCompareConfigDate(int cardIndex, int axis, int enable, int cmp_source)
        {
            return 0;
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
        public override int SetCardLeadScrewCompConfig(PitchCompensationparamModel pcm)
        {
            return 0;
        }

        /// <summary>
        /// 打开轴的螺距补尝
        /// </summary>
        /// <returns></returns>
        public override int SetCardLeadScrewCompEnable(int cardIndex, int axis, int enable)
        {
            return 0;
        }

        public override int CardAxisLineUnitSpeed(LineInterpolateparamModel parameter)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisHcmp2dsetEnable(int cardIndex, int hcmp, int cmpEnable)
        {
            throw new NotImplementedException();
        }

        public override int ClearCardHcmpPoints(int cardIndex, int hcmp)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisHcmpConfig(int cardIndex, int hcmp, int axis, int cmp_source, int cmp_logic, int time)
        {
            throw new NotImplementedException();
        }

        public override int AddCardHcmpPoint(int cardIndex, int hcmp, int cmp_pos)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisLatchMode(int cardIndex, int axis, int all_enable, int latch_source, int triger_chunnel)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisReserLatchFlag(int cardIndex, int axis)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisLatchValue(int cardIndex, int axis)
        {
            throw new NotImplementedException();
        }

        public override int SetCardHcmpCmpPin(int cardIndex, int hcmp, int on_off)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisLtcMode(int cardIndex, int axis, ushort ltc_logic, ushort ltc_mode, double filter)
        {
            throw new NotImplementedException();
        }

        public override int GetCardHcmpCmpPin(int cardIndex, int hcmp)
        {
            throw new NotImplementedException();
        }

        public override int SetCardHcmpLiner(int cardIndex, int hcmp, int increment, int Count)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisLatchValueExtern(int cardIndex, int axis, int Index)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisLatchFlag(int cardIndex, int axis)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisLatchFlagExtern(int cardIndex, int axis)
        {
            throw new NotImplementedException();
        }

        public override int SetCardDebugMode(int cardIndex, string FileName)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisEncoder(int cardIndex, int axis, int encoder_value)
        {
            return MCC141CSDK.Instance.SetCardAxisPos(cardIndex, axis, encoder_value);
            //throw new NotImplementedException();
        }

        public override int GetCardAxisSevonPin()
        {
            //throw new NotImplementedException();
            return 0;
        }

        public override int GetCardCheckDoneMulticoor(int cardIndex, int crd)
        {
            throw new NotImplementedException();
        }

        public override int SetSpacing(int axis, int ORGPlusPos, int PELAlarmPlusPos, int NELAlarmPlusPos)
        {
            throw new NotImplementedException();
        }

        public override int SetStatusParameter(int axis, bool f, AxisStatusParameterEnum kind)
        {
            //todo :  lqc 202305161157 写9014的api 目的为了在轴控制面板可以用
            int res = 0;
            if (kind.ToString() == "正限位" || kind.ToString() == "负限位")
            {
                if (f == true)
                {
                    res = MCC141CSDK.Instance.SetCardAxisELLevel(axis, 1);
                }
                else
                {
                    res = MCC141CSDK.Instance.SetCardAxisELLevel(axis, 0);
                }
            }
            if (kind.ToString() == "原点位")
            {
                if (f == true)
                {
                    res = MCC141CSDK.Instance.SetCardAxisHomeConfig(axis, 0, 1, 0);
                }
                else
                {
                    res = MCC141CSDK.Instance.SetCardAxisHomeConfig(axis, 0, 0, 0);
                }
            }


            return res;
            // throw new NotImplementedException();
        }

        public override int CardAxisChangeSpeed(int cardIndex, int axis, double currentVelocity, double accelerationAndDeceleration)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisCompareConfig(int cardIndex, int axis, int enable, int cmp_source)
        {
            throw new NotImplementedException();
        }

        public override int OpenCardHighCompareConfig(int cardIndex, int hcmp, int axis, int cmp_source, int cmp_logic, long time)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisGetHomeResult(int cardIndex, int axis, ref ushort status)
        {
            //throw new NotImplementedException();
            uint pstatus = 1;
            var res = MCC141CSDK.Instance.GetCardAxisMotionStatus(AxisWhichCardNo, axis, ref pstatus);
            if (pstatus == 1)
                status = 0;
            else
                status = 1;
            return res;
        }

        public override int SetCardAxisHomeElReturn(int cardIndex, int axis, ushort enable)
        {
            return 0;
        }

        public override string GetEtherCATErrorInfo(int errorNumber)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisProfile(int cardIndex, int axis, ref double minVelocity, ref double maxVelocity, ref double acceleration, ref double deceleration, ref double stop_vel)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisSProfile(int cardIndex, int axis, int s_mode, ref double s_para)
        {
            throw new NotImplementedException();
        }

        public override List<int> GetLineInterpolationSupportAxis()
        {
            var list = new List<int>();
            return list;
        }

        public override int DmcSetCardVectorSProfile(int cardIndex, int crdIndex, int s_mode, double s_para)
        {
            return 0;
        }

        public override int GetCardHcmpCurrentState(int cardIndex, int hcmp, ref int remained_points, ref int current_point, ref int runned_points)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisStartSelectHcmp(ushort cardIndex, int camera_count, ushort GroupNo = 1)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisStopSelectHcmp(ushort cardIndex, ushort GroupNo = 1)
        {
            throw new NotImplementedException();
        }

        public override int AddCCDTriIO(int cardIndex, int hcmp, short ioIndex)
        {
            throw new NotImplementedException();
        }

        public override int AddCardAxisComparePoint(int cardIndex, int axis, int pos, int dir, ushort action, uint actpara)
        {
            throw new NotImplementedException();
        }

        public override int ClearCardAxisComparePoints(int cardIndex, int axis)
        {
            throw new NotImplementedException();
        }

        public override double GetCardAxisCurrentSpeed(int cardIndex, int axis)
        {
            double currentSpeed = 0;
            var res = MCC141CSDK.Instance.GetCardAxisCurrentSpeed(axis, ref currentSpeed);
            if (res == 0) return currentSpeed;
            return res;
        }

        public override int SetCardAxisInpMode(int cardIndex, int axis, int enable, int inp_logic)
        {
            return 0;
        }

        public override int GetCardAxisComparePointsRunned(int cardIndex, int axis, ref int pointNum)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisCompareConfig(int cardIndex, int axis, ref ushort enable, ref ushort cmp_source)
        {
            throw new NotImplementedException();
        }

        public override int SetCardDAEnable(int cardIndex, int enable)
        {
            throw new NotImplementedException();
        }

        public override int GetCardDAEnable(int cardIndex, ref ushort enable)
        {
            throw new NotImplementedException();
        }

        public override int SetCardDAOutput(int cardIndex, int channel, double voltageOut)
        {
            throw new NotImplementedException();
        }

        public override int GetCardDAOutput(int cardIndex, int channel, ref double voltageOut)
        {
            throw new NotImplementedException();
        }

        public override int GetCardADInput(int cardIndex, int channel, ref double voltageOut)
        {
            throw new NotImplementedException();
        }

        public override int GetCardADInputEx(int cardIndex, int channel, ref double voltageOut)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisResetTargetPosition(int cardIndex, int axis, int dist, int posi_mode)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisUpdateTargetPosition(int cardIndex, int axis, int dist, int posi_mode)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisComparePointsRemained(int cardIndex, int axis, ref int pointNum)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisCompareCurrentPoint(int cardIndex, int axis, ref int pos)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisHcmp2dsetEnable(int cardIndex, int hcmp, ref ushort cmpEnable)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisHcmp2dsetConfig(int cardIndex, ushort hcmp, ushort cmp_mode, ushort x_axis, ushort cmp_source, ushort y_axis, ushort y_cmp_source, int error, ushort cmp_logic, int time, ushort pwm_enable, double duty, int freq, ushort port_sel, ushort pwm_number)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisHcmp2dsetConfig(int cardIndex, ushort hcmp, ref ushort cmp_mode, ref ushort x_axis, ref ushort cmp_source, ref ushort y_axis, ref ushort y_cmp_source, ref int error, ref ushort cmp_logic, ref int time, ref ushort pwm_enable, ref double duty, ref int freq, ref ushort port_sel, ref ushort pwm_number)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisHcmp2dClearPoints(int cardIndex, ushort hcmp)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisHcmp2dAddPoint(int cardIndex, ushort hcmp, int x_cmp_pos, int y_cmp_pos)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisHcmp2dCurrentState(int cardIndex, ushort hcmp, ref int remained_points, ref int x_current_point, ref int y_current_point, ref int runned_points, ref ushort current_state)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisHcmp2dForceOutput(int cardIndex, ushort hcmp, ushort enable)
        {
            throw new NotImplementedException();
        }

        public override int ClearCardComparePointsExtern(int cardIndex)
        {
            throw new NotImplementedException();
        }

        public override int AddCardComparePointExtern(int cardIndex, ushort[] axis, int[] pos, ushort[] dir, ushort action, int actpara)
        {
            throw new NotImplementedException();
        }

        public override int GetCardCompareCurrentPointExtern(int cardIndex, ref int[] pos)
        {
            throw new NotImplementedException();
        }

        public override int GetCardComparePointsRunnedExtern(int cardIndex, ref int pointNum)
        {
            throw new NotImplementedException();
        }

        public override int GetCardComparePointsRemainedExtern(int cardIndex, ref int pointNum)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisLatchStopTime(int cardIndex, int axis, int time)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisOutMode(int cardIndex, int axis, int enable, int bitno)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisOutMode(int cardIndex, int axis, ref ushort enable, ref ushort bitno)
        {
            throw new NotImplementedException();
        }

        public override int SetCardIoCountMode(int cardIndex, int bitno, int mode, double filter_time)
        {
            throw new NotImplementedException();
        }

        public override int SetCardIoCountValue(int cardIndex, int bitno, int countValue)
        {
            throw new NotImplementedException();
        }

        public override int GetCardIoCountValue(int cardIndex, int bitno, ref uint countValue)
        {
            throw new NotImplementedException();
        }

        public override int SetCardReverseOutBit(int cardIndex, int bitno, int reverse_time)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisHomelatchMode(int cardIndex, int axis, ushort enable, ushort logic, ushort source)
        {
            throw new NotImplementedException();
        }

        public override int ClearCardAxisHomelatchFlag(int cardIndex, int axis)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisHomelatchFlag(int cardIndex, int axis)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisHomelatchValue(int cardIndex, int axis)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisSetBacklash(int cardIndex, int axis, int backlash_value, int backlash_interval, int backlash_dir)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisSetBacklash(int cardIndex, int axis, ref int backlash_value, ref int backlash_interval, ref int backlash_dir)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisStopReason(int cardIndex, int axis, ref int StopReason)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisFollowParameter(int cardIndex, int slave_axis, int master_axis, double followradio)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisFollowParameter(int cardIndex, int slave_axis, ref int master_axis, ref double followradio)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisFollowEnable(int cardIndex, int slave_axis, int enable)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisFollowEnable(int cardIndex, int slave_axis, ref int enable)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisPmotionSync(int cardIndex, int axis_num, ushort[] axis_list, int[] aim_pos, int posi_mode)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisPmoveSoftLanding(int cardIndex, int axis, double midPos, double targetPos, double startVel, double maxVel, double endVel, double tAcc, double tDec, int posiMode)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisPMotionSoftLanding(int cardIndex, int axis, double aim_pos, double target_pos, double acc_time, double dec_time, double start_vel, double max_vel, double mid_vel, double stop_vel, int pos_mode)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisCrdPrm(int cardIndex, int crd, ushort dimension, ushort[] profile, double synVelMax, double synAccMax, short evenTime, short setOriginFlag, int[] originPos)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisCrdPrm(int cardIndex, int crd, ref ushort dimension, ushort[] profile, ref double synVelMax, ref double synAccMax, ref short evenTime, ref short setOriginFlag, int[] originPos)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisBufCrdData(int cardIndex, int crd)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufLineData(int cardIndex, int crdIndex, ushort axisNum, ushort[] axisNumbers, double[] aim_pos, short posi_mode, double synVel, double synAcc, double velEnd, double velStart)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufArcnCenterMove(int cardIndex, int crdIndex, ushort axisNum, ushort[] axisNumbers, double[] aim_pos, ref double cen_pos, ushort circleDir, ushort circle_num, short posi_mode, double synVel, double synAcc, double velEnd, double velStart)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufArcnRadiusMove(int cardIndex, int crdIndex, ushort axisNum, ushort[] axisNumbers, double[] aim_pos, double radius, ushort circleDir, ushort circle_num, short posi_mode, double synVel, double synAcc, double velEnd, double velStart)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufArcn3pointMove(int cardIndex, int crdIndex, ushort axisNum, ushort[] axisNumbers, double[] aim_pos, ref double mid_pos, ushort circle_num, short posi_mode, double synVel, double synAcc, double velEnd, double velStart)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufIO(int cardIndex, int crd, ushort doType, ushort doMask, ushort doValue)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufDelay(int cardIndex, int crd, ushort delayTime)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufLimitON(int cardIndex, int crd, short axis, short limitType)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufLimitOff(int cardIndex, int crd, short axis, short limitType)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufSetStopIO(int cardIndex, int crd, short axis, short stopType, short inputType, short inputIndex)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufMove(int cardIndex, int crd, short moveAxis, double pos, double vel, double acc_time)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufCrdRemainSpace(int cardIndex, int crd, ref int pSpace)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufCrdClear(int cardIndex, int crd)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufCrdStart(int cardIndex, int crd)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufCrdPause(int cardIndex, int crd)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufCrdStop(int cardIndex, int crd, int mode)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufCrdStatus(int cardIndex, int crd, ref short pRun, ref int pSegment)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisBufUserSegNum(int cardIndex, int crd, long segNum)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisBufUserSegNum(int cardIndex, int crd, ref long segNum)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisBufRemainSegNum(int cardIndex, int crd, ref long pSegment)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisBufSetOverride(int cardIndex, int crd, double synVelRatio)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisBufSetCrdStopDec(int cardIndex, int crd, double decSmoothStop, double decAbruptStop)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisBufSetCrdStopDec(int cardIndex, int crd, ref double decSmoothStop, ref double decAbruptStop)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisBufGetCrdPos(int cardIndex, int crd, ref double pPos)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisBufGetCrdVel(int cardIndex, int crd, ref double pSynVel)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufInitLookahead(int cardIndex, int crd, double T, double accMax, int enable)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisBufInitLookahead(int cardIndex, int crd, ref double T, ref double accMax, ref int enable)
        {
            throw new NotImplementedException();
        }

        public override int SetCardAxisBufCrdSmooth(int cardIndex, int crd, double Smooth)
        {
            throw new NotImplementedException();
        }

        public override int GetCardAxisBufCrdSmooth(int cardIndex, int crd, ref double Smooth)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufCrdReset(int cardIndex, int crd)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufWaitDI(int cardIndex, int crd, ushort dI_id, ushort di_logic, long time_out)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufPwmOutput(int cardIndex, int crd, ushort pwm_no, double pwm_duty, double pwm_freq)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufPwmFollow(int cardIndex, int crd, ushort pwm_no, ushort mode, double start_speed, double max_speed, double max_power, double min_power, double none_follow_value)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufDelayOutbitToStartPos(int cardIndex, int crd, int bitno, int on_off, double delay_value, int delay_mode, double reverseTime)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufDelayOutbitToStopPos(int cardIndex, int crd, int bitno, int on_off, double delay_time, double reverseTime)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufAheadOutbitToStopPos(int cardIndex, int crd, int bitno, int on_off, double ahead_value, int ahead_mode, double reverseTime)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufSetPwmON(int cardIndex, int crd, int pwmIndex)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisBufSetPwmOFF(int cardIndex, int crd, int pwmIndex)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisGetLocalcatTotalAdcnum(int cardIndex, ref int TotalIn, ref int TotalOut)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisGetLocalcatTotalIonum(int cardIndex, ref int TotalIn, ref int TotalOut)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisYKSetLocalcatDaOutput(int cardIndex, int channel, short Value)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisGetLocalcatDaOutput(int cardIndex, int channel, ref short Value)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisGetLocalcatAdInput(int cardIndex, int channel, ref short Value)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisSetLocalcatAdMode(int cardIndex, int channel, int mode)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisGetLocalcatAdMode(int cardIndex, int channel, ref int mode)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisSetLocalcatDaMode(int cardIndex, int channel, int mode)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisGetLocalcatDaMode(int cardIndex, int channel, ref int mode)
        {
            throw new NotImplementedException();
        }

        public override int CardAxisLocalcatGetErrCode(int cardIndex, int localcatIndex, ref int ErrCode)
        {
            throw new NotImplementedException();
        }

        public override int SetCardMulAxisCompareConfig(int cardIndex, int cmper_No, int[] axis_List, int enable, int cmp_source)
        {
            throw new NotImplementedException();
        }

        public override int AddCardMulAxisComparePoint(int cardIndex, int cmper_No, int dir, ushort action, uint actpara, int bitNo, double[] cmp_pos)
        {
            throw new NotImplementedException();
        }

        public override int ClearCardMulAxisComparePoints(int cardIndex, int cmper_No)
        {
            throw new NotImplementedException();
        }
        #region 多路锁存
        /// <summary>
        /// 根据锁存器的锁存号获取锁存状态和锁存值;锁存器状态 ,1正在锁存钟,0未启用,2锁存完成,3缓存区已满
        /// </summary>
        /// <param name="latchParam"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override MultiplexedLatchParam GetCardMultiplexedLatchStatus(MultiplexedLatchParam latchParam)
        {
            return new MultiplexedLatchParam();
        }

        /// <summary>
        /// 给对应的锁存器设置参数
        /// </summary>
        /// <param name="latchParam"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override int SetMultiAxisMode(MultiplexedLatchParam latchParam)
        {
            return -1;
        }
        /// <summary>
        ///  打开锁存器
        /// </summary>
        /// <param name="latchParam"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override int OpenCardMultiLtc(MultiplexedLatchParam latchParam)
        {
            return -1;
        }
        /// <summary>
        /// 复位锁存器;先执行清除锁存器,然后关闭锁存器操作
        /// </summary>
        /// <param name="latchParam"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override int CardResetLatch(MultiplexedLatchParam latchParam)
        {
            return -1;
        }
        #endregion
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
