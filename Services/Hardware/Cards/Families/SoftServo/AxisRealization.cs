﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/SoftServo/AxisRealization.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samsun.Domain.MotionCard.Common.SoftServo_EtherCAT
{
    public class AxisRealization : IAxis
    {
        /// <summary>
        /// 轴名字
        /// </summary>
        public string AxisName { get; set; }

        /// <summary>
        /// 这个轴功能的注释
        /// </summary>
        public string AxisNotes { get; set; }

        /// <summary>
        /// 回原标志--上电默认标志为false，伺服必须进行一次复位
        /// </summary>
        public bool HomeFlag { get; set; } = false;


        /// <summary>
        /// 轴功能的注释
        /// </summary>
        public string AxisNotes2 { get; set; }

        /// <summary>
        /// 轴所属的卡号（在多卡的时候轴所属的卡号就不再是0了）
        /// </summary>
        public int AxisWhichCardNo { get; set; }

        /// <summary>
        /// 轴所属卡的名字。
        /// </summary>
        public string AxisWhichCardName { get; set; }

        /// <summary>
        /// 轴所属卡的类型（有些系列卡，可以用此属性区分具体是哪一种卡）
        /// </summary>
        public string AxisWhichcardType { get; set; }

        /// <summary>
        /// 轴的运动方式状态
        /// </summary>
        public MotionParamModel AxisMotionStatus { get; set; }


        /// <summary>
        /// 轴ID，用于控制卡SDK函数识别运动轴号
        /// </summary>
        public int AxisID { get; set; }

        /// <summary>
        /// 虚拟卡(当用户勾选虚拟卡后，轴对象这里需要知道当前是虚拟卡)
        /// </summary>
        public bool IsVitualCard { get; set; } = false;


        /// <summary>
        /// 是否允许编码器
        /// </summary>
        public bool EncodeEnable { get; set; } = false;


        /// <summary>
        /// 用户定义的运动目标位置
        /// </summary>
        public double TargetPos { get; set; } = 0.0;
        /// <summary>
        /// 轴是否在线
        /// </summary>
        public bool Online { get; set; }

        /// <summary>
        /// 用户定义的运动目标位置
        /// </summary>
        public double TargetVel { get; set; } = 0.0;


        /// <summary>
        /// 轴状态, 0轴停止,1轴运行中
        /// </summary>
        public int AxisStatus
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
        public int AxisPEL
        {
            get
            {

                if ((GetAxisCurrentState() & 16) == 16)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            set
            {

            }
        }

        /// <summary>
        /// 负极限信号，1表示负限位有效， 0表示无效
        /// </summary>
        public int AxisMEL
        {
            get
            {
                if ((GetAxisCurrentState() & 32) == 32)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            set
            {

            }
        }

        /// <summary>
        /// 原点信号，1表示原点有效， 0表示无效
        /// </summary>
        public int AxisORG
        {
            get
            {
                if ((GetAxisCurrentState() & 8) == 8)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            set
            {

            }
        }

        /// <summary>
        /// Index信号，1表示EZ为高电平， 0表示为低电平
        /// </summary>
        public int AxisEZ
        {
            get
            {
                if ((GetAxisCurrentState() & 8) == 8)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            set
            {

            }
        }

        /// <summary>
        /// EMG信号，1表示EMG输入为高， 0表示输入为低；该信号为低电平有效。
        /// </summary>
        public int AxisEMG
        {
            get
            {
                if ((GetAxisCurrentState() & 4) == 4)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
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
                if ((GetAxisCurrentState() & 32) == 32)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            set
            {

            }
        }

        /// <summary>
        /// 获取轴的报警信息（如果有硬件接入的情况下）
        /// </summary>
        public int AxisAlarm
        {
            get
            {
                var status = GetAxisCurrentState();
                if ((status & 1) == 1)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            set
            {

            }
        }

        public CAxisParameter AxisParameter { get; set; } = new CAxisParameter();


        /// <summary>
        /// 停止坐标系内所有轴的运动
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="crd"></param>
        /// <param name="stop_mode"></param>
        /// <returns></returns>
        public int CardAxisStopMulticoor(int CardNo, int crd, int stop_mode)
        {
            return SoftVersoSDK.Instance.DmcCardCrdStopMulticoor(CardNo, crd, stop_mode);//todo:2022.11.26
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
            return 0;
        }

        /// <summary>
        /// 读取轴的初速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisInitVel(/*int CardNo,int Axis, */ref int pSpeed)
        {
            if (IsVitualCard)
            {
                pSpeed = 0; return 0;
            }
            double pspeed = 0;
            int CarNo = AxisWhichCardNo;
            int result = SoftVersoSDK.Instance.DmcGetCardAxisCurrentSpeedUnit(CarNo, AxisID, ref pspeed);
            pSpeed = (int)pspeed;
            return result;
        }


        /// <summary>
        /// 获取与轴相关I/O（如限位信号、原点信号）的状态
        /// </summary>
        /// <returns></returns>
        public int GetAxisCurrentState()
        {
            /*
        位号 信号名称 描述
        0 ALM 1：表示伺服报警信号 ALM 为 ON； 0：OFF
        1 EL+ 1：表示正硬限位信号 +EL 为 ON； 0：OFF
        2 EL- 1：表示负硬限位信号–EL 为 ON； 0：OFF
        3 EMG 1：表示急停信号 EMG 为 ON； 0：OFF
        4 ORG 1：表示原点信号 ORG 为 ON； 0：OFF
        6 SL+ 1：表示正软限位信号+SL 为 ON； 0：OFF
        7 SL- 1：表示负软件限位信号-SL 为 ON； 0：OFF
        其他位 保
        */
            uint status = 0;
            var res = SoftVersoSDK.Instance.DmcGetCardAxisIOState(AxisWhichCardNo, AxisID); // 修改了这里PCI9014SDK.Instance.GetAxisCardIOStatus(AxisParameter.AxisID, ref status);
            if (res == 0)
                return (int)status;
            else
                return res;
        }

        /// <summary>
        /// SDK调用返回值的错误描述
        /// </summary>
        /// <param name="ErrNum"></param>
        /// <returns></returns>
        public string GetErrorInfo(int ErrNum)
        {
            var res = SoftVersoSDK.Instance.GetErrorInfo(ErrNum);
            return res;
        }

        public string GetEtherCATErrorInfo(int ErrNum)
        {
            var res = SoftVersoSDK.Instance.GetEtherCATErrorInfo(ErrNum);
            return res;
        }

        /// <summary>
        /// 设置轴回零参数
        /// </summary>
        public short SetCardHomeProfile()
        {
            return 0;
        }


        ArcInterpolateparamModel AIModel = new ArcInterpolateparamModel();

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
        /// <param name="arc">圆弧插补参数
        ///  <para>参数 </para>
        /// <para>"CardNo"卡号 </para>
        /// <para>"Crd"坐标系号，取值范围：0~5 </para>
        /// <para>"axisNum"轴数，取值范围： 2~16 </para>
        /// <para>"Axis"轴号数组 </para>
        /// <para>"TargetPos"目标位置数组，单位：unit</para>
        /// <para>"CenPos"圆心位置数组，单位：unit </para>
        /// <para>"ArcDir"圆弧方向，0：顺时针，1：逆时针</para>
        /// <para>"Circle"圈数：自然数：表示此时执行的为螺旋线插补,螺旋线的圈数</para>
        /// <para>"PosiMode"运动模式，0：相对坐标模式，1：绝对坐标模式</para></param>
        /// <returns>错误代码</returns>
        public int CardAxisArcMoveCenterUnit(ArcInterpolateparamModel arc)
        {
            ushort[] AxisNums = new ushort[arc.Axis.Length];
            double[] Target_Pos = new double[arc.TargetPos.Length];
            double[] Cen_Pos = new double[arc.CenPos.Length];

            for (int i = 0; i < arc.Axis.Length; i++)
            {
                AxisNums[i] = (ushort)arc.Axis[i];
                Target_Pos[i] = arc.TargetPos[i];
                Cen_Pos[i] = arc.CenPos[i];
            }
            return SoftVersoSDK.Instance.DmcCardAxisArcMoveCenterUnit((ushort)arc.CardNo, (ushort)arc.Crd, (ushort)arc.Axis.Length, AxisNums, Target_Pos, Cen_Pos, (ushort)arc.ArcDir, arc.CircleCounts, (ushort)arc.PosiMode);
        }


        /// <summary>
        ///设置插补运动速度曲线的平滑时间
        /// </summary>
        /// <param name="CardNo">卡号 </param>
        /// <param name="Crd">坐标系号，取值范围：0~5 </param>
        /// <param name="s_mode">保留参数，固定值为 0</param>
        /// <param name="s_para">平滑时间，单位：s，范围：0~1</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardVectorSProfile(int CardNo, int Crd, int s_mode, double s_para)
        {
            return SoftVersoSDK.Instance.DmcSetCardVectorSProfile((ushort)CardNo, (ushort)Crd, 0, s_para);
        }


        /// <summary>
        /// 未完成
        /// 启动连续圆弧插补
        /// <para> 基于半径圆弧扩展的圆柱螺旋线插补运动（可作两轴圆弧插补） </para>
        /// <para>注  意：</para> 
        /// <para> 1）当轴数为 2 时，轴列表前两轴进行平面圆弧插补</para>
        /// <para> 2）当轴数为 3 时，轴列表前两轴平面为基面，进行平面圆弧插补；同时，轴列表第三轴运动指定高度；该轴终点位置与该轴起点位置的差值为圆柱螺旋线段相对于基面的高度 </para> 
        /// </summary>
        /// <param name="arc">圆弧插补参数
        /// <para>"CardNo"卡号 </para>
        /// <para>"Crd"坐标系号，取值范围：0~5 </para>
        /// <para>"axisNum"轴数，取值范围： 2~16 </para>
        /// <para>"Axis"轴号数组 </para>
        /// <para>"TargetPos"目标位置数组，单位：unit</para>
        /// <para>"ArcRadius"圆弧半径值，单位：unit  </para>
        /// <para>"ArcDir"圆弧方向，0：顺时针，1：逆时针</para>
        /// <para>"Circle"圈数：自然数：表示此时执行的为螺旋线插补,螺旋线的圈数</para>
        /// <para>"PosiMode"运动模式，0：相对坐标模式，1：绝对坐标模式</para></param>
        /// <returns></returns>
        public int CardAxisContiArcMoveStart(ArcInterpolateparamModel arc)
        {
            ushort[] AxisNums = new ushort[arc.Axis.Length];
            double[] Target_Pos = new double[arc.TargetPos.Length];

            for (int i = 0; i < arc.Axis.Length; i++)
            {
                AxisNums[i] = (ushort)arc.Axis[i];
                Target_Pos[i] = arc.TargetPos[i];
            }
            SoftVersoSDK.Instance.DmcCardAxisArcMoveRadiusUnit((ushort)arc.CardNo, (ushort)arc.Crd, (ushort)arc.Axis.Length, AxisNums, Target_Pos, arc.ArcRadius, (ushort)arc.ArcDir, arc.CircleCounts, (ushort)arc.PosiMode);
            return SoftVersoSDK.Instance.DmcCardContiStartList(arc.CardNo, arc.Crd);//开启轴连续插补
        }

        /// <summary>
        /// 未完成
        /// 启动连续直线插补
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public int CardAxisContiLineUnitStart(LineInterpolateparamModel line)
        {
            return SoftVersoSDK.Instance.DmcCardContiStartList(line.CardNo, line.Crd);//开启轴连续插补
        }

        /// <summary>
        /// 启动轴回零
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">轴编号</param>
        /// <returns></returns>
        public int CardAxisHomeMove()
        {
            return SoftVersoSDK.Instance.DmcCardAxisHomeMove((ushort)AxisWhichCardNo, (ushort)AxisID);
        }

        /// <summary>
        /// 启动直线插补
        /// </summary>
        /// <param name="line">直线插补参数
        /// <para>"CardNo"</para>
        /// <para>"Crd"坐标系号，取值范围：0~5 </para>
        /// <para>"axisNum"轴数，取值范围：2~16 </para>
        /// <para>"Axis"轴号数组 </para>
        /// <para>"Target_Pos"目标位置列表，单位：unit </para>
        /// <para>"posi_mode"运动模式，0：相对坐标模式，1：绝对坐标模式</para></param>
        /// <returns>错误代码</returns>
        public int CardAxisLineUnit(LineInterpolateparamModel line)
        {
            ushort[] AxisNums = new ushort[line.Axis.Length];
            double[] Target_Pos = new double[line.TargetPos.Length];

            for (int i = 0; i < line.Axis.Length; i++)
            {
                AxisNums[i] = (ushort)line.Axis[i];
                Target_Pos[i] = line.TargetPos[i];
            }
            return SoftVersoSDK.Instance.DmcCardAxisLineUnit(line.CardNo, line.Crd, line.Axis.Length, AxisNums, Target_Pos, line.PosiMode);
        }

        /// <summary>
        /// 点位运动
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="dist">目标位置，单位：unit </param>
        /// <param name="posi_mode">运动模式，0：相对坐标模式，1：绝对坐标模式</param>
        /// <returns>错误代码</returns>
        public int CardAxisPointMovement(MotionParamModel mpm)
        {
            return SoftVersoSDK.Instance.DmcCardAxisPMoveUnit(mpm.CardNo, mpm.Axis, mpm.Dist, (ushort)mpm.PosiMode);
        }

        /// <summary>
        /// 多轴空间圆弧插补
        ///<para>功  能：基于三点圆弧扩展的圆柱螺旋线插补运动（可作两轴及三轴圆弧插补） </para>
        ///<para>注  意：</para><para>1）当轴数为 2 时，轴列表前两轴进行平面圆弧插补</para>
        ///<para>2）当轴数为 3、运动轨迹为圆柱螺旋插补时，轴列表前两轴平面为基面，进行平面圆弧插补；同时，轴列表第三轴运动指定高度；该轴终点位置与该轴起点位置的差值为圆柱螺旋线段相对于基面的高度</para>
        ///<para>3）当轴数大于 3、运动轨迹为螺旋插补时，列表前三轴进行螺旋插补的同时，后续轴做线性跟随运动，运动时间与前三轴的运动时间相等</para>
        /// </summary>
        /// <param name="arc">卡号
        /// <para>"CardNo"卡号</para>
        /// <para>"Crd"坐标系号，取值范围：0~5</para>
        /// <para>"axisNum"轴数，取值范围：2~16 </para>
        /// <para>"AxisNums"轴号数组</para>
        /// <para>"TargetPos"目标位置数组，单位：unit</para>
        /// <para>"MidPos"中间位置数组，单位：unit</para>
        /// <para>"CircleCounts"圈数：<para>负数：表示此时执行的为空间圆弧插补：该值的绝对值减 1 表示空间圆弧的圈数。如，-1 即表示 0圈空间圆弧，-2 即表示 1 圈空间圆弧… </para><para>自然数：表示此时执行的为圆柱螺旋线插补:该值表示螺旋线的圈数。如，0 即表示 0 圈螺旋线插补， 1 即表示 1 圈螺旋线插补… </para></para>
        /// <para>"PosiMode"运动模式，0：相对坐标模式，1：绝对坐标模式 </para></param>
        /// <returns>错误代码</returns>
        public int CardAxesArcMoveCenterUnit(ArcInterpolateparamModel arc)
        {
            ushort[] AxisNums = new ushort[arc.Axis.Length];
            double[] TargetPos = new double[arc.TargetPos.Length];
            double[] MidPos = new double[arc.MidPos.Length];
            for (int i = 0; i < arc.Axis.Length; i++)
            {
                AxisNums[i] = (ushort)arc.Axis[i];
                TargetPos[i] = arc.TargetPos[i];
                MidPos[i] = arc.MidPos[i];
            }
            return SoftVersoSDK.Instance.DmcCardArcMove3PointsUnit((ushort)arc.CardNo, (ushort)arc.Crd, (ushort)arc.Axis.Length, AxisNums, TargetPos, MidPos, arc.CircleCounts, (ushort)arc.PosiMode);
        }

        /// <summary>
        /// 指定轴连续运动
        /// </summary>
        /// <param name="mpm">参数
        /// <para>"CardNo"控制卡卡号</para>
        /// <para>"Axis"指定轴号</para>
        /// <para>"Dir"方向运动方向，0：负方向，1：正方向</para></param>
        /// <returns>错误代码</returns>
        public int CardAxisSerialMovement(MotionParamModel mpm)
        {
            return SoftVersoSDK.Instance.DmcCardAxisVMove(mpm.CardNo, mpm.Axis, mpm.Dir);
        }

        /// <summary>
        /// 多轴空间直线插补
        /// </summary>
        /// <param name="line">直线插补参数
        /// <para>"CardNo"</para>
        /// <para>"Crd"坐标系号，取值范围：0~5 </para>
        /// <para>"axisNum"轴数，取值范围：2~16 </para>
        /// <para>"Axis"轴号数组 </para>
        /// <para>"Target_Pos"目标位置列表，单位：unit </para>
        /// <para>"posi_mode"运动模式，0：相对坐标模式，1：绝对坐标模式</para></param>
        /// <returns>错误代码</returns>
        public int CardAxesLineUnit(LineInterpolateparamModel line)
        {

            ushort[] AxisNums = new ushort[line.Axis.Length];
            double[] Target_Pos = new double[line.TargetPos.Length];

            for (int i = 0; i < line.Axis.Length; i++)
            {
                AxisNums[i] = (ushort)line.Axis[i];
                Target_Pos[i] = line.TargetPos[i];
            }
            return SoftVersoSDK.Instance.DmcCardAxisLineUnit(line.CardNo, line.Crd, line.Axis.Length, AxisNums, Target_Pos, line.PosiMode);

        }
        /// <summary>
        ///  清除总线轴错误码
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴轴号</param>
        /// <returns>错误代码</returns>
        public int ClearCardAxisAlarmState(int CardNo, int axis)
        {
            return SoftVersoSDK.Instance.NmcClearCardAxisErrCode(CardNo, axis);
        }


        /// <summary>
        /// 让伺服使能。  0失能 1或者非0值使能
        /// </summary>
        /// <param name="on_off"></param>
        /// <returns></returns>
        public int CardAxisWriteSevonPin(int on_off)
        {
            /*
            short nmc_set_axis_enable(WORD CardNo,WORD axis)
            功 能：设置 EtherCAT 总线驱动器使能
            参 数：CardNo 控制卡卡号
            axis EtherCAT 总线轴的轴号，255 表示使能所有 EtherCAT 轴
            返回值：错误代码

            short nmc_set_axis_disable(WORD CardNo,WORD axis)
            功 能：设置 EtherCAT 总线驱动器失能 参 数：CardNo 控制卡卡号
            axis EtherCAT 总线轴的轴号，255 表示失能所有 EtherCAT 轴
            返回值：错误代码
            */
            var res = -1;
            if (on_off == 0)
            {
                res = SoftVersoSDK.Instance.NmcSetCardAxisDisable(AxisWhichCardNo, AxisID);
            }
            else
            {
                res = SoftVersoSDK.Instance.NmcSetCardAxisEnable(AxisWhichCardNo, AxisID);
            }
            return res;
        }



        /// <summary>
        /// （疑问）清除插补坐标系
        /// </summary>
        /// <returns></returns>
        public int ClearCardAxisInterpolateCoord()
        {
            return -1;
        }

        /// <summary>
        /// 设置 EtherCAT 总线驱动器失能
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴的轴号，255 表示使能所有 EtherCAT 轴</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisDisable(int CardNo, int axis)
        {
            return SoftVersoSDK.Instance.NmcSetCardAxisDisable(CardNo, axis);
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
            return SoftVersoSDK.Instance.DmcSetCardContiLookaheadMode(impm.CardNo, impm.Crd, impm.Enable, impm.LookaheadSegments, impm.PathError, impm.LookaheadAcc);
        }

        /// <summary>
        /// （疑问）坐标系无法创建
        /// </summary>
        /// <returns>错误代码</returns>
        public int CreatCardAxisInterpolateCoord(InterpolateMotetionParamModel imp)
        {
            return -1;
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
            double Min_Vel = 0, Max_Vel = 0, Tacc = 0, Tdec = 0, Stop_Vel = 0;
            SoftVersoSDK.Instance.DmcGetCardAxisProfileUnit(mpm.CardNo, mpm.Axis,
               ref Min_Vel, ref Max_Vel, ref Tacc, ref Tdec, ref Stop_Vel);
            mpm.MaxVel = (int)Max_Vel;
            mpm.MinVel = (int)Min_Vel;
            mpm.TaccVel = (int)Tacc;
            mpm.TdecVel = (int)Tdec;
            mpm.StopVel = (int)Stop_Vel;
            return mpm;
        }

        /// <summary>
        /// 获取轴状态
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="axis"></param>
        /// <returns>轴状态</returns>
        public int GetCardAxisAlarmState(int CardNo, int axis)
        {
            ushort errcode = 0;
            SoftVersoSDK.Instance.NmcGetCardAxisErrCode(CardNo, axis, ref errcode);
            return errcode;
        }

        /// <summary>
        /// 读取控制轴的位置计数器，该计数器可以为输出脉冲计数器(cntr_no = 0)或者编码器反馈脉冲位置计数器(cntr_no = 1)；其对应的位置分别为指令脉冲位置（逻辑位置），或者编码器反馈脉冲位置（实际位置）。  
        /// </summary>
        /// <param name="cntr_no">输出脉冲计数器/编码器反馈脉冲位置计数器选择:  0: 输出脉冲计数器 |  1: 编码器反馈脉冲位置计数器</param>
        /// <returns>位置计数器的值, 范围在-134217728 ~ 134217727</returns>
        public double GetCardAxisCurrentPosition(/*int CardNo, */int cntr_no)
        {
            //double pos = 0;
            //SoftVersoSDK.Instance.DmcGetCardAxisPositionUnit(AxisWhichCardNo, axis, ref pos);
            //return (int)pos;
            double pos = 0; //
            var res = 0;
            if (cntr_no == 0)
            {
                res = SoftVersoSDK.Instance.DmcGetCardAxisPositionUnit(AxisWhichCardNo, AxisID, ref pos);
            }
            else
            {
                res = SoftVersoSDK.Instance.DmcGetCardAxisEncoderUnit(AxisWhichCardNo, AxisID, ref pos);
            }
            if (res != 0)
            {
                return 0;
            }
            return pos;//todo:2022.11.26
        }

        /// <summary>
        /// 读取EtherCAT总线轴状态机
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴轴号</param>
        /// <returns>轴状态
        /// <para> 0：轴处于未启动状态  </para>
        /// <para>1：轴处于启动禁止状态 </para>
        /// <para>2：轴处于准备启动状态 </para>
        /// <para>3：轴处于启动状态     </para>
        /// <para>4：轴处于操作使能状态 </para>
        /// <para>5：轴处于停止状态     </para>
        /// <para>6：轴处于错误触发状态 </para>
        /// <para>7：轴处于错误状态     </para>
        /// </returns>
        public int GetCardAxisCurrentState(/*int CardNo, int axis*/)
        {
            //要改成dmc4300A的状态值 ： 轴状态,0 表示控制轴运动完成，处于空闲状态;1 表示控制轴正在运动，其它值为调用出错
            //ushort Axis_StateMachine = 0;
            var Axis_StateMachine = SoftVersoSDK.Instance.DmcGetCardAxisCheckDone(AxisWhichCardNo, AxisID);
            if (Axis_StateMachine == 0) return 1;
            return 0;
        }

        /// <summary>
        /// 设置单轴运动速度
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="Min_Vel">起始速度，单位：unit/s</param>
        /// <param name="Max_Vel">最大速度，单位：unit/s</param>
        /// <param name="Tacc">加速时间，单位：s</param>
        /// <param name="Tdec">减速时间，单位：s</param>
        /// <param name="Stop_Vel">停止速度，单位：unit/s</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisMotionalVel(MotionParamModel mpm)
        {
            return SoftVersoSDK.Instance.DmcSetCardAxisProfileUnit(mpm.CardNo, mpm.Axis,
                mpm.MinVel, mpm.MaxVel, mpm.TaccVel, mpm.TdecVel, mpm.StopVel);
        }

        /// <summary>
        /// （疑问）没有硬限位
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisELLimit()
        {
            return -1;
        }


        /// <summary>
        /// 获取轴回零参数
        /// </summary>
        /// <param name="hpm">回零参数模型 
        /// <para>"CardNo"控制卡卡号 </para>
        /// <para>"axis"EtherCAT 总线轴轴号</para>
        /// <para>"home_mode"返回EtherCAT 总线轴回零模式</para>
        /// <para>"Low_Vel"返回EtherCAT 总线轴回零低速</para>
        /// <para>"High_Vel"返回EtherCAT 总线轴回零高速</para>
        /// <para>"Tacc"返回EtherCAT 总线轴回零加速时间</para>
        /// <para>"Tdec"返回EtherCAT 总线轴回零减速时间</para>
        /// <para>"offsetpos"返回EtherCAT 总线轴回零偏移 </para></param>
        /// <returns>回零参数模型</returns>
        public HomeParameModel GetCardAxisHighHomeProfile(HomeParameModel hpm)
        {
            short home_mode = 0;
            double Low_Vel = 0;
            double High_Vel = 0;
            double Tacc = 0;
            double Tdec = 0;
            double offsetpos = 0;
            SoftVersoSDK.Instance.NmcGetCardAxisHomeProfile(hpm.CardNo, hpm.Axis, ref home_mode, ref Low_Vel, ref High_Vel, ref Tacc, ref Tdec, ref offsetpos);
            hpm.HomeMode = home_mode;
            hpm.LowVel = (int)Low_Vel;
            hpm.HighVel = (int)High_Vel;
            hpm.TaccTime = (int)Tacc;
            hpm.TdecTime = (int)Tdec;
            hpm.OffSetPos = (int)offsetpos;
            return hpm;
        }

        /// <summary>
        /// （疑问）读取轴高速回零减速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisHighHomeDecVel()
        {
            return -1;
        }

        /// <summary>
        ///（疑问）读取轴高速回零速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisHighHomeVel()
        {
            return -1;
        }

        /// <summary>
        /// （疑问）读取轴回零最大保护距离
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisHomeMaxProtectDis()
        {
            return -1;
        }

        /// <summary>
        /// （疑问）读取回零模式
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisHomeMode()
        {
            return -1;
        }

        /// <summary>
        /// （疑问）读取轴回零位置偏差
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisHomePositionErro()
        {
            return -1;
        }

        /// <summary>
        /// （疑问）读取轴回零参数
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisHomeProfile()
        {
            return -1;
        }

        /// <summary>
        /// （疑问）读取轴的初速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisInitVel(int CardNo, int Axis, ref int pSpeed)
        {
            return -1;
        }

        /// <summary>
        /// （疑问）读取轴的加加速
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisJerkVel()
        {
            return -1;
        }

        /// <summary>
        /// (疑问)获取软限位状态
        /// </summary>
        public int GetCardAxisLimitState()
        {
            return -1;
        }

        /// <summary>
        /// （疑问）读取轴的低速回零加速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisLowHomeAccVel()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// （疑问）读取轴的低速回零减速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisLowHomeDecVel()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// （疑问）读取轴的低速回零速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisLowHomeVel()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// （疑问）读取轴的最大速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisMaxVel()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取轴的脉冲当量
        /// </summary> 
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <returns>脉冲当量，单位：pulse/unit</returns>
        public int GetCardAxisPulseEquival(int CardNo, int axis)
        {
            double equiv = 0;
            SoftVersoSDK.Instance.DmcGetCardAxisEquiv(CardNo, axis, ref equiv);
            return (int)equiv;
        }

        /// <summary>
        /// (疑问)读取轴的反向回零距离
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisReverseHomeDis()
        {
            return -1;
        }

        /// <summary>
        /// 获取轴运动模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <returns>轴运动模式
        ///  1为pp模式，6为回零模式，8为csp模式
        /// </returns>
        public int GetCardAxisRunMode(int CardNo, int axis)
        {
            UInt16 run_mode = 0;
            SoftVersoSDK.Instance.DmcGetCardAxisRunMode(CardNo, axis, ref run_mode);
            return run_mode;
        }

        /// <summary>
        /// （疑问）获取轴的减减速
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisSlowVel()
        {
            return -1;
        }

        /// <summary>
        /// 获取软限位设置
        /// </summary>
        /// <param name ="lpm">控制卡卡号
        /// <para>"CardNo"控制卡卡号</para>
        /// <para>"axis"轴编号</para>
        /// <para>"enable"返回使能状态</para>
        /// <para>"source_sel"返回计数器选择</para>
        /// <para>"SL_action"返回限位停止方式</para>
        /// <para>"N_limit"返回负限位脉冲数</para>
        /// <para>"P_limit"返回正限位脉冲数</para></param>
        /// <returns>限位参数模型</returns>
        public LimitParamModel GetCardAxisSoftLimit(LimitParamModel lpm)
        {
            UInt16 enable = 0;
            UInt16 source_sel = 0;
            UInt16 SL_action = 0;
            int N_limit = 0;
            int P_limit = 0;
            SoftVersoSDK.Instance.DmcGetCardAxisSoftLimit(lpm.CardNo, lpm.Axis, ref enable, ref source_sel, ref SL_action, ref N_limit, ref P_limit);
            lpm.Enable = enable;
            lpm.SourceSel = source_sel;
            lpm.SLAction = SL_action;
            lpm.Nlimit = N_limit;
            lpm.Plimit = P_limit;
            return lpm;
        }

        /// <summary>
        /// 读取 EtherCAT 总线卡状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="channel">EtherCAT 总线端口号，固定为 2</param>
        /// <returns>总线状态，0 表示正常</returns>
        public int GetCardCurrentState(int CardNo, int channel)
        {
            ushort errcode = 0;
            SoftVersoSDK.Instance.NmcGetCardErrcode(CardNo, channel, ref errcode);
            return errcode;
        }


        /// <summary>
        ///设置 EtherCAT 总线驱动器使能
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴的轴号，255 表示使能所有 EtherCAT 轴</param>
        /// <returns>错误代码</returns>
        public int OpenCardAxisEnable(int CardNo, int axis)
        {
            return SoftVersoSDK.Instance.NmcSetCardAxisEnable(CardNo, axis);
        }

        /// <summary>
        ///  (疑问)打开位置比较输出
        /// </summary>
        /// <returns></returns>
        public int OpenCardCompareConfig(int CardNo, int axis, int enable, int cmp_source)
        {
            return SoftVersoSDK.Instance.DmcSetCardAxisCompareConfig(CardNo, axis, enable, cmp_source);
        }

        /// <summary>
        /// (疑问)设置轴的加速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisAccVel()
        {
            return -1;
        }


        /// <summary>
        /// 设置一维位置比较输出
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisCompareConfig()
        {
            return -1;
        }

        /// <summary>
        /// 设置一维位置比较器
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号 </param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能 </param>
        /// <param name="cmp_source">比较源，0：指令位置计数器，1：编码器计数器</param>
        public int SetCardAxisCompareConfigDate(int CardNo, int axis, int enable, int cmp_source)
        {
            return SoftVersoSDK.Instance.DmcSetCardAxisCompareConfig(CardNo, axis, enable, cmp_source);
        }

        /// <summary>
        /// 设置轴的当前位置
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="pos">位置值，单位：unit</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisCurrentPosition(int CardNo, int axis, int pos)
        {
            return SoftVersoSDK.Instance.DmcSetCardAxisPositionUnit(CardNo, axis, pos);
        }

        /// <summary>
        ///  (疑问)设置轴的减速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisDecVel()
        {
            return -1;
        }

        /// <summary>
        ///  (疑问)没有硬限位设置
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisELLimit(LimitParamModel lpm)
        {
            return -1;
        }

        /// <summary>
        /// (疑问)设置轴的高速回零加速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisHighHomeAccVel()
        {
            return -1;
        }

        /// <summary>
        /// (疑问)设置轴的高速回零减速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisHighHomeDecVel()
        {
            return -1;
        }

        /// <summary>
        /// (疑问)设置轴的回零速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisHighHomeVel()
        {
            return -1;
        }

        /// <summary>
        /// (疑问)设置轴回零最大保护距离
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisHomeMaxProtectDis()
        {
            return -1;
        }

        /// <summary>
        /// 设置指定轴的回原点模式（适用于所有脉冲卡）
        /// </summary>
        /// <param name = "hpm">回原点参数模型
        /// <para>"CardNo"卡号</para>
        /// <para>"axis"轴编号</para>
        /// <para>"homedir"回原距离</para>
        /// <para>"vel"速度</para>
        /// <para>"mode"回原模型</para>
        /// <para>"EZcount"计数</para></param>
        /// <returns></returns>
        public int SetCardAxisHomeMode(HomeParameModel hpm)
        {
            return SoftVersoSDK.Instance.DmcSetCardAxisHomeMode(hpm.CardNo, hpm.Axis, hpm.HomeDir, hpm.Vel, hpm.HomeMode, hpm.TaccTime, hpm.TdecTime);
        }

        /// <summary>
        /// (疑问)设置轴的会令位置偏差
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisHomePositionErro()
        {
            return -1;
        }

        /// <summary>
        /// 设置轴回零参数
        /// </summary>
        /// <param name="hpm">会令参数模型
        /// <para>"CardNo" 控制卡卡号 </para>
        /// <para>"axis" EtherCAT 总线轴轴号</para>
        /// <para>"home_mode" EtherCAT 总线轴回零模式</para>
        /// <para>"Low_Vel" EtherCAT 总线轴回零低速</para>
        /// <para>"High_Vel" EtherCAT 总线轴回零高速</para>
        /// <para>"Tacc" EtherCAT 总线轴回零加速时间</para>
        /// <para>"Tdec" EtherCAT 总线轴回零减速时间</para>
        /// <para>"offsetpos" EtherCAT 总线轴回零偏移 </para> </param>
        /// <returns>错误代码</returns>
        public int SetCardAxisHomeProfile(HomeParameModel hpm)
        {
            return SoftVersoSDK.Instance.NmcSetCardAxisHomeProfile(hpm.CardNo, hpm.Axis, hpm.HomeMode, hpm.LowVel, hpm.HighVel, hpm.TaccTime, hpm.TdecTime, hpm.EtherCATHomeOffset);
        }

        /// <summary>
        /// 设置轴的初速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInitVel()
        {
            return -1;
        }

        /// <summary>
        /// 设置插补运动速度曲线
        /// <para>注 意：</para>
        /// <para>1）DMC-E3032 卡支持 6 个坐标系（参数 Crd）。六个坐标系的速度可独立设置，执行插补时六个坐标系可独立进行插补运动（即可同时进行六组插补运动）</para>
        /// </summary>
        /// <param name="imp">插补参数模型
        /// <para>"CardNo"卡号 </para>
        /// <para>"Crd"坐标系号，取值范围：0~3</para>
        /// <para>"Min_Vel"最小速度，单位：unit/s</para>
        /// <para>"Max_Vel"最大速度，单位：unit/s</para>
        /// <para>"Tacc"加速时间，单位：s </para>
        /// <para>"Tdec"减速时间，单位：s </para>
        /// <para>"Stop_Vel"停止速度，单位：unit/s</para> </param>
        /// <returns>错误代码</returns>
        public int SetCardAxisInterpolateVectorProfile(InterpolateMotetionParamModel imp)
        {
            return SoftVersoSDK.Instance.DmcSetCardVectorProfileUnit(imp.CardNo, imp.Crd, imp.MinVel, imp.MaxVel, imp.TaccTime, imp.TdecTime, imp.StopVel);
        }

        /// <summary>
        /// （疑问）设置插补减速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInterpolateDecVel()
        {
            return -1;
        }

        /// <summary>
        /// （疑问）设置插补初始速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInterpolateInitVel()
        {
            return -1;
        }

        /// <summary>
        /// （疑问）设置插补加加速
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInterpolateJerkVel()
        {
            return -1;
        }

        /// <summary>
        /// （疑问）设置插补初始速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInterpolateMaxVel()
        {
            return -1;
        }

        /// <summary>
        /// （疑问）设置差补低速
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInterpolateSlowVel()
        {
            return -1;
        }

        /// <summary>
        /// （疑问）设置轴的加加速
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisJerkVel()
        {
            return -1;
        }

        /// <summary>
        /// （疑问）设置轴的低速回零加速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisLowHomeAccVel()
        {
            return -1;
        }

        /// <summary>
        /// （疑问）设置轴的低速回零减速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisLowHomeDecVel()
        {
            return -1;
        }

        /// <summary>
        /// （疑问）设置轴的低速回零速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisLowHomeVel()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// （疑问）设置轴的最大速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisMaxVel()
        {
            return -1;
        }

        /// <summary>
        /// 设置轴的脉冲当量
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="equiv">脉冲当量，单位：pulse/unit</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisPulseEquival(int CardNo, int axis, int equiv)
        {
            return SoftVersoSDK.Instance.DmcSetCardAxisEquiv(CardNo, axis, equiv);
        }

        /// <summary>
        /// (疑问)设置轴的反向回零距离
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisReverseHomeDis()
        {
            return -1;
        }

        /// <summary>
        /// 设置轴运动模式：
        /// 1为pp模式，6为回零模式，8为csp模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="runmode">运动模式</param>
        /// <returns>short类型值</returns>
        public int SetCardAxisRunMode(int CardNo, int axis, ushort runmode)
        {
            return SoftVersoSDK.Instance.NmcSetCardAxisRunMode(CardNo, axis, runmode);
        }

        /// <summary>
        /// （疑问）设置轴的减减速
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisSlowVel()
        {
            return -1;
        }

        /// <summary>
        /// 设置软限位
        /// <para>注 意：</para>
        /// <para>正、负限位位置可为正数也可为负数，但正限位位置应大于负限位位置</para>
        /// </summary>
        /// <param name="lpm">限位参数模型
        /// <para>"CardNo"控制卡卡号</para>
        /// <para>"axis"轴编号</para>
        /// <para>"enable"使能状态，0：禁止，1：允许 </para>
        /// <para>"source_sel"计数器选择，0：指令位置计数器，1：编码器</para>
        /// <para>"SL_action"限位停止方式，0：立即停止 1：减速停止</para>
        /// <para>"N_limit"负限位位置，单位：pulse </para>
        /// <para>"P_limit"正限位位置，单位：pulse</para></param>
        /// <returns></returns>
        public int SetCardAxisSoftLimit(LimitParamModel lpm)
        {
            return SoftVersoSDK.Instance.DmcSetCardAxisSoftLimit(lpm.CardNo, lpm.Axis, lpm.Enable, lpm.SourceSel, lpm.SLAction, lpm.Nlimit, lpm.Plimit);
        }

        /// <summary>
        /// 设置单轴速度曲线 S 段参数值
        /// int CardNo, int axis, int s_mode, double s_para
        /// </summary>
        /// <param name="mpm">参数模型
        /// <para>"CardNo"控制卡卡号</para>
        /// <para>"axis"指定轴号</para>
        /// <para>"s_mode"保留参数，固定值为 0</para>
        /// <para>"s_para"S 段时间，单位：s；范围：0~1</para></param>
        /// <returns>错误代码</returns>        /// <returns></returns>
        public int SetCardAxisSProfile(MotionParamModel mpm)
        {
            return SoftVersoSDK.Instance.DmcSetCardAxisSProfile(mpm.CardNo, mpm.Axis, mpm.SMode, mpm.SPara);
        }



        public int SetCardAxisTProfile(MotionParamModel mpm)
        {
            /*
            short dmc_set_profile_unit(WORD CardNo, WORD axis, double Min_Vel, double Max_Vel, 
            double Tacc, double Tdec, double Stop_Vel)
            功 能：设置单轴运动速度曲线
            参 数：CardNo 卡号
            axis 指定轴号
            Min_Vel 起始速度，单位：unit/s
            Max_Vel 最大速度，单位：unit/s
            Tacc 加速时间，单位：s
            Tdec 减速时间，单位：s
            Stop_Vel 停止速度，单位：unit/s
            返回值：错误代码
             */
            return SoftVersoSDK.Instance.DmcSetCardAxisProfileUnit(mpm.CardNo, mpm.Axis, mpm.MinVel, mpm.MaxVel, mpm.TaccVel, mpm.TdecVel, mpm.StopVel);

            //return -1;
        }


        /// <summary>
        /// 设置二维位置比较器 （二维低速）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能 </param>
        /// <param name="cmp_source">   比较源，0：指令位置计数器，1：编码器计数器</param>
        /// <returns>错误代码</returns>
        public int SetCardCompareConfigExtern(int CardNo, int enable, int cmp_source)
        {
            return SoftVersoSDK.Instance.DmcSetCardCompareConfigExtern(CardNo, enable, cmp_source);
        }


        /// <summary>
        /// 设置二维位置比较输出数据(添加比较点)
        /// </summary>
        ///  <param name="pcpm">位置比较参数模型
        /// <para>"CardNo"控制卡卡号</para>
        /// <para>"axis"指定轴号 </para>
        /// <para>"pos"设置比较位置为 X pulse</para>
        /// <para>"dir"设置比较模式</para>
        /// <para>"action"设置触发功能（为 IO 电平取反）</para>
        /// <para>"actpara"设置输出 IO 端口 (0 触发功能)</para></param>
        /// <returns></returns>
        public int SetCardCompareConfigExternDate(PositionComparatorParamModel pcpm)
        {
            ushort[] AxisNums = new ushort[pcpm.Axis.Length];
            ushort[] CmpMode = new ushort[pcpm.CmpMode.Length];

            for (int i = 0; i < pcpm.Axis.Length; i++)
            {
                AxisNums[i] = (ushort)pcpm.Axis[i];
                CmpMode[i] = (ushort)pcpm.CmpMode[i];
            }
            return SoftVersoSDK.Instance.DmcAddCardComparePointsExtern(pcpm.CardNo, AxisNums, pcpm.XCmpPos, CmpMode, pcpm.Enable, pcpm.PortSel);
        }

        /// <summary>
        /// 设置一维位置比较（开启/关闭）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号 </param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能 </param>
        /// <param name="cmp_source">比较源，0：指令位置计数器，1：编码器计数器</param>
        /// <returns></returns>
        public int SetCardHcmpEnable(int CardNo, int axis, int enable, int cmp_source)
        {
            //return SoftVersoSDK.Instance.DmcSetCardAxisCompareConfig(CardNo, axis, enable, cmp_source);
            return SoftVersoSDK.Instance.DmcSetCardHcmpMode1(CardNo, enable, cmp_source);
        }

        /// <summary>
        /// 设置螺距补偿参数
        /// </summary>
        /// <returns></returns>
        public int SetCardLeadScrewCompConfig(PitchCompensationparamModel pcm)
        {
            return SoftVersoSDK.Instance.DmcSetCardLeadScrewCompConfig(pcm.CardNo, pcm.Axis, pcm.n,
                pcm.StartPos, pcm.LenPos, pcm.PCompPos, pcm.PCompNeg);
        }

        /// <summary>
        /// 设置螺距补偿（开启、关闭）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴号 </param>
        /// <param name="enable">螺距补偿：开启\关闭</param>
        /// <returns></returns>
        public int SetCardLeadScrewCompEnable(int CardNo, int axis, int enable)
        {
            return SoftVersoSDK.Instance.DmcSetCardLeadScrewCompEnable(CardNo, axis, enable);
        }

        /// <summary>
        /// 设置连续前瞻模式
        /// </summary>
        /// <returns></returns>
        public int StartCardContiLookaheadMode(int CardNo, int Crd, int enable, int LookaheadSegments, int PathError, int LookaheadAcc)
        {
            return SoftVersoSDK.Instance.DmcSetCardContiLookaheadMode(CardNo, Crd, enable, LookaheadSegments, PathError, LookaheadAcc);
        }


        /// <summary>
        /// 指定轴停止运动
        /// <para>注 意：</para>
        /// <para>此函数适用于单轴、PVT 运动</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="stop_mode">制动方式，0：减速停止，1：紧急停止</param>
        /// <returns>错误代码</returns>
        public int StopCardAxisMovement(int CardNo, int axis, int stop_mode)
        {
            return SoftVersoSDK.Instance.DmcCardAxisStop(CardNo, axis, stop_mode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="stop_mode"></param>
        /// <returns></returns>
        public int SetCardAxisErrorStopMode(int axis, int stop_mode)
        {
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="enable"></param>
        /// <param name="active_level"></param>
        /// <returns></returns>
        public int SetCardAxisAlarm(int axis, int enable, int active_level)
        {
            return 0;
        }

        public int CardAxisHomeMove(int plus_dir)
        {
            return 0;
        }

        /// <summary>
        /// 设置插补运动速度曲线
        /// </summary>
        /// <param name="parameter">需要指定的参数有：坐标系号,最小速度,最大速度,加速时间,减速时间,停止速度</param>
        /// <returns></returns>
        public int CardAxisLineUnitSpeed(LineInterpolateparamModel parameter)
        {
            return SoftVersoSDK.Instance.DmcSetCardVectorProfileUnit(AxisWhichCardNo,
                parameter.Crd, parameter.minVel, parameter.maxVel,
                parameter.tacc, parameter.tdec, parameter.stopVel);
        }

        public int SetCardAxisHcmp2dsetEnable(int CardNo, int hcmp, int cmpEnable)
        {
            return 0;
        }

        public int ClearCardHcmpPoints(int CardNo, int hcmp)
        {
            //return 0;
            return SoftVersoSDK.Instance.DmcClearCardAxisComparePoints(CardNo, hcmp);
        }

        public int SetCardAxisHcmpConfig(int CardNo, int hcmp, int axis, int cmp_source, int cmp_logic, int time)
        {
            return 0;
        }

        public int AddCardHcmpPoint(int CardNo, int hcmp, int cmp_pos)
        {
            //return 0;
            return SoftVersoSDK.Instance.DmcAddCardHcmpPoint(CardNo, hcmp, cmp_pos);
        }

        public int SetCardAxisLatchMode(int CardNo, int axis, int all_enable, int latch_source, int triger_chunnel)
        {
            return 0;
        }

        public int GetCardAxisReserLatchFlag(int CardNo, int axis)
        {
            return 0;
        }

        public int GetCardAxisLatchValue(int CardNo, int axis)
        {
            return 0;
        }

        public int SetCardHcmpCmpPin(int CardNo, int hcmp, int on_off)
        {
            return 0;
        }

        public int SetCardAxisLtcMode(int CardNo, int axis, ushort ltc_logic, ushort ltc_mode, double filter)
        {
            return 0;
        }

        public int GetCardHcmpCmpPin(int CardNo, int hcmp)
        {
            return 0;
        }

        public int SetCardHcmpLiner(int CardNo, int hcmp, int Increment, int Count)
        {
            return 0;
        }

        public int GetCardAxisLatchValueExtern(int CardNo, int axis, int Index)
        {
            return 0;
        }

        public int GetCardAxisLatchFlag(int CardNo, int axis)
        {
            return 0;
        }

        public int GetCardAxisLatchFlagExtern(int CardNo, int axis)
        {
            //return SoftVersoSDK.Instance.DmcSoftltcGetNumber()
            return 0;
        }

        /// <summary>
        ///  函数调用打印输出设置
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public int SetCardDebugMode(int CardNo, string FileName)
        {
            return SoftVersoSDK.Instance.DmcSetCardDebugMode(CardNo, FileName);
        }

        /// <summary>
        /// 设置指定轴的当前编码器计数值
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="pos">编码器计数值，单位：unit</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisEncoder(int CardNo, int axis, int encoder_value)
        {
            return SoftVersoSDK.Instance.DmcSetCardAxisEncoderUnit(CardNo, axis, encoder_value);
        }

        /// <summary>
        /// 读取指定轴的伺服使能端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <returns>伺服使能端口电平，0：低电平(on)，1：高电平(off)</returns>
        public int GetCardAxisSevonPin()
        {
            ushort status = 0;
            SoftVersoSDK.Instance.NmcGetCardAxisStateMachine(AxisWhichCardNo, AxisID, ref status);

            switch (status)
            {
                case 4:
                    return 0;
                case 2:
                    return 1;
                default:
                    return 1;
            }
        }

        /// <summary>
        /// 检测坐标系的运动状态
        /// <para>注 意：</para>
        /// <para>此函数适用于插补运动</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">指定控制卡上的坐标系号（取值范围：0~5）</param>
        /// <returns>轴运动状态</returns>
        public int GetCardCheckDoneMulticoor(int CardNo, int crd)
        {
            return SoftVersoSDK.Instance.DmcGetCardCrdCheckDoneMulticoor(CardNo, crd);
        }

        public int SetSpacing(int axis, int ORGPlusPos, int PELAlarmPlusPos, int NELAlarmPlusPos)
        {
            return 0;
        }

        public int SetStatusParameter(int axis, bool f, AxisStatusParameterEnum kind)
        {
            return 0;
        }

        /// <summary>
        /// 在线改变指定轴的当前运动速度
        /// <para>注 意：</para>
        /// <para>1）该函数适用于单轴运动中的变速</para>
        /// <para>2）设置的变速时间是从当前速度变速到新速度的时间。此时控制卡会重新计算起始速度加速到最高速度所需的时间以及最高速度减速到停止速度所需的时间，即加减速时间会被重新计算</para>
        /// <para>3）变速一旦成立，该轴的默认运行速度将会被改写为 New_Vel，加减速时间也会被控制卡新计算的值所覆盖，也即当调用 dmc_get_profile_unit 回读速度参数时会发生与 dmc_set_profile_unit 所设置的值不一致的现象</para>
        /// <para>4）在连续运动中 New_Vel 负值表示往负向变速，正值表示往正向变速。在点位运动中 New_Vel 只允许正值</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="New_Vel">新的运行速度，单位：unit/s</param>
        /// <param name="Taccdec">变速时间，单位：s </param>
        /// <returns>错误代码</returns>
        public int CardAxisChangeSpeed(int CardNo, int axis, double Curr_Vel, double Taccdec)
        {
            return SoftVersoSDK.Instance.DmcCardAxisChangeSpeed(CardNo, axis, Curr_Vel, Taccdec);
        }

        /// <summary>
        /// 设置一维位置比较器 (单轴低速)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号 </param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能 </param>
        /// <param name="cmp_source">比较源，0：指令位置计数器，1：编码器计数器</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisCompareConfig(int CardNo, int axis, int enable, int cmp_source)
        {
            return SoftVersoSDK.Instance.DmcSetCardAxisCompareConfig(CardNo, axis, enable, cmp_source);
        }

        /// <summary>
        /// 配置高速比较器
        /// <para>注  意：1）该函数的 time 参数（脉冲宽度）只对队列和线性比较模式起作用  </para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口）</param>
        /// <param name="axis">辅助编码器通道号 </param>
        /// <param name="cmp_source">比较位置源，固定值 1：辅助编码器</param>
        /// <param name="cmp_logic">有效电平：0：低电平，1：</param>
        /// <param name="time">脉冲宽度，单位：us，取值范围：1us~20s </param>
        /// <returns>错误代码</returns>
        public int OpenCardHighCompareConfig(int CardNo, int hcmp, int axis, int cmp_source, int cmp_logic, long time)
        {
            return SoftVersoSDK.Instance.DmcSetCardAxisHcmpConfig(CardNo, hcmp, axis, cmp_source, cmp_logic, (int)time);
            //return SoftVersoSDK.Instance.DmcSetCardAxisCompareConfig(CardNo, axis, 1, cmp_source);
        }

        /// <summary>
        /// 获取回零执行状态（适用于所有脉冲/总线卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="Axis">轴号</param>
        /// <param name="state">状态结果</param>
        /// <returns></returns>
        public int CardAxisGetHomeResult(int CardNo, int axis, ref ushort status)
        {
            return SoftVersoSDK.Instance.DmcGetCardHomeResult(CardNo, axis, ref status);
        }

        public int SetCardAxisHomeElReturn(int CardNo, int axis, ushort enable)
        {
            return 0;
        }

        /// <summary>
        /// 读取单轴运动速度曲线
        /// <para>注 意：</para>
        /// <para>该函数不适用于连续插补</para>
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="Min_Vel">返回起始速度设置，单位：unit/s</param>
        /// <param name="Max_Vel">返回最大速度设置，单位：unit/s</param>
        /// <param name="Tacc">返回加速时间设置，单位：s</param>
        /// <param name="Tdec">返回减速时间设置，单位：s</param>
        /// <param name="Stop_Vel">返回停止速度设置，单位：unit/s</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisProfile(int CardNo, int axis, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double stop_vel)
        {
            return SoftVersoSDK.Instance.DmcGetCardAxisProfileUnit(CardNo, axis, ref Min_Vel, ref Max_Vel, ref Tacc, ref Tdec, ref stop_vel);
        }

        /// <summary>
        /// 读取单轴速度曲线 S 段参数值
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="s_mode">保留参数</param>
        /// <param name="s_para">返回设置的 S 段时间</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisSProfile(int CardNo, int axis, int s_mode, ref double s_para)
        {
            return SoftVersoSDK.Instance.DmcGetCardAxisSProfile(CardNo, axis, s_mode, ref s_para);
        }

        public List<int> GetLineInterpolationSupportAxis()
        {
            var list = new List<int>();
            for (int i = 0; i < 32; i++)
            {
                list.Add(i);
            }
            return list;
        }

        public int GetCardHcmpCurrentState(int CardNo, int hcmp, ref int remained_points, ref int current_point, ref int runned_points)
        {
            throw new NotImplementedException();
        }

        public int CardAxisStartSelectHcmp(ushort CardNo, int camera_count, ushort GroupNo = 1)
        {
            throw new NotImplementedException();
        }

        public int CardAxisStopSelectHcmp(ushort CardNo, ushort GroupNo = 1)
        {
            throw new NotImplementedException();
        }

        public int AddCCDTriIO(int CardNo, int hcmp, short ioIndex)
        {
            throw new NotImplementedException();
        }

        public int AddCardAxisComparePoint(int CardNo, int axis, int pos, int dir, ushort action, uint actpara)
        {
            return SoftVersoSDK.Instance.DmcAddCardAxisComparePointsCycle(CardNo, axis, pos, dir, action, (int)actpara, 0);

            //throw new NotImplementedException();
        }

        public int ClearCardAxisComparePoints(int CardNo, int axis)
        {
            return SoftVersoSDK.Instance.DmcClearCardAxisComparePoints((ushort)CardNo, (ushort)axis);

            //throw new NotImplementedException();
        }

        public double GetCardAxisCurrentSpeed(int CardNo, int axis)
        {
            double currentSpeed = 0;
            var res = SoftVersoSDK.Instance.DmcGetCardAxisCurrentSpeedUnit(CardNo, axis, ref currentSpeed);
            if (res == 0) return currentSpeed;
            return res;
            //throw new NotImplementedException();
        }

        public int SetCardAxisInpMode(int CardNo, int axis, int enable, int inp_logic)
        {
            return 0;
        }

        public int GetCardAxisComparePointsRunned(int CardNo, int axis, ref int pointNum)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisCompareConfig(int CardNo, int axis, ref ushort enable, ref ushort cmp_source)
        {
            throw new NotImplementedException();
        }

        public int SetCardDAEnable(int CardNo, int enable)
        {
            throw new NotImplementedException();
        }

        public int GetCardDAEnable(int CardNo, ref ushort enable)
        {
            throw new NotImplementedException();
        }

        public int SetCardDAOutPut(int CardNo, int channel, double Vout)
        {
            throw new NotImplementedException();
        }

        public int GetCardDAOutPut(int CardNo, int channel, ref double Vout)
        {
            throw new NotImplementedException();
        }

        public int GetCardADInPut(int CardNo, int channel, ref double Vout)
        {
            throw new NotImplementedException();
        }

        public int GetCardADInPutEx(int CardNo, int channel, ref double Vout)
        {
            throw new NotImplementedException();
        }

        public int CardAxisResetTargetPosition(int CardNo, int axis, int dist, int posi_mode)
        {
            throw new NotImplementedException();
        }

        public int CardAxisUpdateTargetPosition(int CardNo, int axis, int dist, int posi_mode)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisComparePointsRemained(int CardNo, int axis, ref int pointNum)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisCompareCurrentPoint(int CardNo, int axis, ref int pos)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisHcmp2dsetEnable(int CardNo, int hcmp, ref ushort cmpEnable)
        {
            throw new NotImplementedException();
        }

        public int SetCardAxisHcmp2dsetConfig(int CardNo, ushort hcmp, ushort cmp_mode, ushort x_axis, ushort cmp_source, ushort y_axis, ushort y_cmp_source, int error, ushort cmp_logic, int time, ushort pwm_enable, double duty, int freq, ushort port_sel, ushort pwm_number)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisHcmp2dsetConfig(int CardNo, ushort hcmp, ref ushort cmp_mode, ref ushort x_axis, ref ushort cmp_source, ref ushort y_axis, ref ushort y_cmp_source, ref int error, ref ushort cmp_logic, ref int time, ref ushort pwm_enable, ref double duty, ref int freq, ref ushort port_sel, ref ushort pwm_number)
        {
            throw new NotImplementedException();
        }

        public int SetCardAxisHcmp2dClearPoints(int CardNo, ushort hcmp)
        {
            throw new NotImplementedException();
        }

        public int SetCardAxisHcmp2dAddPoint(int CardNo, ushort hcmp, int x_cmp_pos, int y_cmp_pos)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisHcmp2dCurrentState(int CardNo, ushort hcmp, ref int remained_points, ref int x_current_point, ref int y_current_point, ref int runned_points, ref ushort current_state)
        {
            throw new NotImplementedException();
        }

        public int SetCardAxisHcmp2dForceOutput(int CardNo, ushort hcmp, ushort enable)
        {
            throw new NotImplementedException();
        }

        public int ClearCardComparePointsExtern(int CardNo)
        {
            throw new NotImplementedException();
        }

        public int AddCardComparePointExtern(int CardNo, ushort[] axis, int[] pos, ushort[] dir, ushort action, int actpara)
        {
            throw new NotImplementedException();
        }

        public int GetCardCompareCurrentPointExtern(int CardNo, ref int[] pos)
        {
            throw new NotImplementedException();
        }

        public int GetCardComparePointsRunnedExtern(int CardNo, ref int pointNum)
        {
            throw new NotImplementedException();
        }

        public int GetCardComparePointsRemainedExtern(int CardNo, ref int pointNum)
        {
            throw new NotImplementedException();
        }

        public int SetCardAxisLatchStopTime(int CardNo, int axis, int time)
        {
            throw new NotImplementedException();
        }

        public int SetCardAxisOutMode(int CardNo, int axis, int enable, int bitno)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisOutMode(int CardNo, int axis, ref ushort enable, ref ushort bitno)
        {
            throw new NotImplementedException();
        }

        public int SetCardIoCountMode(int CardNo, int bitno, int mode, double filter_time)
        {
            throw new NotImplementedException();
        }

        public int SetCardIoCountValue(int CardNo, int bitno, int CountValue)
        {
            throw new NotImplementedException();
        }

        public int GetCardIoCountValue(int CardNo, int bitno, ref uint CountValue)
        {
            throw new NotImplementedException();
        }

        public int SetCardReverseOutBit(int CardNo, int bitno, int reverse_time)
        {
            throw new NotImplementedException();
        }

        public int SetCardAxisHomelatchMode(int CardNo, int axis, ushort enable, ushort logic, ushort source)
        {
            throw new NotImplementedException();
        }

        public int ClearCardAxisHomelatchFlag(int CardNo, int axis)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisHomelatchFlag(int CardNo, int axis)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisHomelatchValue(int CardNo, int axis)
        {
            throw new NotImplementedException();
        }

        public int SetCardAxisSetBacklash(int CardNo, int axis, int backlash_value, int backlash_interval, int backlash_dir)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisSetBacklash(int CardNo, int axis, ref int backlash_value, ref int backlash_interval, ref int backlash_dir)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisStopReason(int CardNo, int axis, ref int StopReason)
        {
            throw new NotImplementedException();
        }

        public int SetCardAxisFollowParameter(int CardNo, int slave_axis, int master_axis, double followradio)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisFollowParameter(int CardNo, int slave_axis, ref int master_axis, ref double followradio)
        {
            throw new NotImplementedException();
        }

        public int SetCardAxisFollowEnable(int CardNo, int slave_axis, int enable)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisFollowEnable(int CardNo, int slave_axis, ref int enable)
        {
            throw new NotImplementedException();
        }

        public int CardAxisPmotionSync(int CardNo, int axis_num, ushort[] axis_list, int[] aim_pos, int posi_mode)
        {
            throw new NotImplementedException();
        }

        public int CardAxisPmoveSoftLanding(int CardNo, int axis, double midPos, double targetPos, double startVel, double maxVel, double endVel, double tAcc, double tDec, int posiMode)
        {
            throw new NotImplementedException();
        }

        public int CardAxisPMotionSoftLanding(int CardNo, int axis, double aim_pos, double target_pos, double acc_time, double dec_time, double start_vel, double max_vel, double mid_vel, double stop_vel, int pos_mode)
        {
            throw new NotImplementedException();
        }

        public int SetCardAxisCrdPrm(int CardNo, int crd, ushort dimension, ushort[] profile, double synVelMax, double synAccMax, short evenTime, short setOriginFlag, int[] originPos)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisCrdPrm(int CardNo, int crd, ref ushort dimension, ushort[] profile, ref double synVelMax, ref double synAccMax, ref short evenTime, ref short setOriginFlag, int[] originPos)
        {
            throw new NotImplementedException();
        }

        public int SetCardAxisBufCrdData(int CardNo, int crd)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufLineData(int CardNo, int Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, short posi_mode, double synVel, double synAcc, double velEnd, double velStart)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufArcnCenterMove(int CardNo, int Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, ref double cen_pos, ushort circleDir, ushort circle_num, short posi_mode, double synVel, double synAcc, double velEnd, double velStart)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufArcnRadiusMove(int CardNo, int Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, double radius, ushort circleDir, ushort circle_num, short posi_mode, double synVel, double synAcc, double velEnd, double velStart)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufArcn3pointMove(int CardNo, int Crd, ushort axisNum, ushort[] AxisList, double[] aim_pos, ref double mid_pos, ushort circle_num, short posi_mode, double synVel, double synAcc, double velEnd, double velStart)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufIO(int CardNo, int crd, ushort doType, ushort doMask, ushort doValue)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufDelay(int CardNo, int crd, ushort delayTime)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufLimitON(int CardNo, int crd, short axis, short limitType)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufLimitOff(int CardNo, int crd, short axis, short limitType)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufSetStopIO(int CardNo, int crd, short axis, short stopType, short inputType, short inputIndex)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufMove(int CardNo, int crd, short moveAxis, double pos, double vel, double acc_time)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufCrdRemainSpace(int CardNo, int crd, ref int pSpace)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufCrdClear(int CardNo, int crd)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufCrdStart(int CardNo, int crd)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufCrdPause(int CardNo, int crd)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufCrdStop(int CardNo, int crd, int mode)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufCrdStatus(int CardNo, int crd, ref short pRun, ref int pSegment)
        {
            throw new NotImplementedException();
        }

        public int SetCardAxisBufUserSegNum(int CardNo, int crd, long segNum)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisBufUserSegNum(int CardNo, int crd, ref long segNum)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisBufRemainSegNum(int CardNo, int crd, ref long pSegment)
        {
            throw new NotImplementedException();
        }

        public int SetCardAxisBufSetOverride(int CardNo, int crd, double synVelRatio)
        {
            throw new NotImplementedException();
        }

        public int SetCardAxisBufSetCrdStopDec(int CardNo, int crd, double decSmoothStop, double decAbruptStop)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisBufSetCrdStopDec(int CardNo, int crd, ref double decSmoothStop, ref double decAbruptStop)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisBufGetCrdPos(int CardNo, int crd, ref double pPos)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisBufGetCrdVel(int CardNo, int crd, ref double pSynVel)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufInitLookahead(int CardNo, int crd, double T, double accMax, int enable)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisBufInitLookahead(int CardNo, int crd, ref double T, ref double accMax, ref int enable)
        {
            throw new NotImplementedException();
        }

        public int SetCardAxisBufCrdSmooth(int CardNo, int crd, double Smooth)
        {
            throw new NotImplementedException();
        }

        public int GetCardAxisBufCrdSmooth(int CardNo, int crd, ref double Smooth)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufCrdReset(int CardNo, int crd)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufWaitDI(int CardNo, int crd, ushort dI_id, ushort di_logic, long time_out)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufPwmOutput(int CardNo, int crd, ushort pwm_no, double pwm_duty, double pwm_freq)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufPwmFollow(int CardNo, int crd, ushort pwm_no, ushort mode, double start_speed, double max_speed, double max_power, double min_power, double none_follow_value)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufDelayOutbitToStartPos(int CardNo, int crd, int bitno, int on_off, double delay_value, int delay_mode, double ReverseTime)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufDelayOutbitToStopPos(int CardNo, int crd, int bitno, int on_off, double delay_time, double ReverseTime)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufAheadOutbitToStopPos(int CardNo, int crd, int bitno, int on_off, double ahead_value, int ahead_mode, double ReverseTime)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufSetPwmON(int CardNo, int crd, int PwmNo)
        {
            throw new NotImplementedException();
        }

        public int CardAxisBufSetPwmOFF(int CardNo, int crd, int PwmNo)
        {
            throw new NotImplementedException();
        }

        public int CardAxisGetLocalcatTotalAdcnum(int CardNo, ref int TotalIn, ref int TotalOut)
        {
            throw new NotImplementedException();
        }

        public int CardAxisGetLocalcatTotalIonum(int CardNo, ref int TotalIn, ref int TotalOut)
        {
            throw new NotImplementedException();
        }

        public int CardAxisYKSetLocalcatDaOutput(int CardNo, int channel, short Value)
        {
            throw new NotImplementedException();
        }

        public int CardAxisGetLocalcatDaOutput(int CardNo, int channel, ref short Value)
        {
            throw new NotImplementedException();
        }

        public int CardAxisGetLocalcatAdInput(int CardNo, int channel, ref short Value)
        {
            throw new NotImplementedException();
        }

        public int CardAxisSetLocalcatAdMode(int CardNo, int channel, int mode)
        {
            throw new NotImplementedException();
        }

        public int CardAxisGetLocalcatAdMode(int CardNo, int channel, ref int mode)
        {
            throw new NotImplementedException();
        }

        public int CardAxisSetLocalcatDaMode(int CardNo, int channel, int mode)
        {
            throw new NotImplementedException();
        }

        public int CardAxisGetLocalcatDaMode(int CardNo, int channel, ref int mode)
        {
            throw new NotImplementedException();
        }

        public int CardAxisLocalcatGetErrCode(int CardNo, int LocalcatNo, ref int ErrCode)
        {
            throw new NotImplementedException();
        }

        public int SetAxisStopTime(int CardNo, int axis, double stopTime)
        {
            throw new NotImplementedException();
        }

        public int SetCardMulAxisCompareConfig(int CardNo, int cmper_No, int[] axis_List, int enable, int cmp_source)
        {
            throw new NotImplementedException();
        }

        public int AddCardMulAxisComparePoint(int CardNo, int cmper_No, int dir, ushort action, uint actpara, int bitNo, double[] cmp_pos)
        {
            throw new NotImplementedException();
        }

        public int ClearCardMulAxisComparePoints(int CardNo, int cmper_No)
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
        MultiplexedLatchParam IAxis.GetCradMultiplexedLatchStatus(MultiplexedLatchParam latchParam)
        {
            return new MultiplexedLatchParam();
        }

        /// <summary>
        /// 给对应的锁存器设置参数
        /// </summary>
        /// <param name="latchParam"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        int IAxis.SetMultiAxisMode(MultiplexedLatchParam latchParam)
        {
            return -1;
        }
        /// <summary>
        ///  打开锁存器
        /// </summary>
        /// <param name="latchParam"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        int IAxis.OpenCardMultiLtc(MultiplexedLatchParam latchParam)
        {
            return -1;
        }
        /// <summary>
        /// 复位锁存器;先执行清除锁存器,然后关闭锁存器操作
        /// </summary>
        /// <param name="latchParam"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        int IAxis.CardResetLatch(MultiplexedLatchParam latchParam)
        {
            return -1;
        }
        #endregion
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
