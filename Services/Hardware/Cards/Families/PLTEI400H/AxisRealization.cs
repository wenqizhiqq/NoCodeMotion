﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/PLTEI400H/AxisRealization.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using Samsun.Domain.MotionCard.Common;
namespace Samsun.Domain.MotionCard.Common.PLTEI400H
{
    public class AxisRealization : IAxis
    {
        /// <summary>
        /// 轴名字
        /// </summary>
        public string AxisName { get; set; }

        /// <summary>
        /// 轴功能的注释
        /// </summary>
        public string AxisNotes { get; set; }

        /// <summary>
        /// 轴功能的注释
        /// </summary>
        public string AxisNotes2 { get; set; }

        /// <summary>
        /// 回原标志--上电默认标志为false，伺服必须进行一次复位
        /// </summary>
        public bool HomeFlag { get; set; } = false;


        /// <summary>
        /// 轴所属的卡号（在多卡的时候轴所属的卡号就不再是0了）
        /// </summary>
        public int AxisWhichCardNo { get; set; }

        /// <summary>
        /// 轴所属卡的名字。
        /// </summary>
        public string AxisWhichCardName { get; set; }

        /// <summary>
        /// 轴的运动方式状态
        /// </summary>
        public MotionParamModel AxisMotionStatus { get; set; }


        /// <summary>
        /// 轴所属卡的类型（有些系列卡，可以用此属性区分具体是哪一种卡）
        /// </summary>
        public string AxisWhichcardType { get; set; }

        /// <summary>
        /// 轴ID，用于控制卡SDK函数识别运动轴号
        /// </summary>
        public int AxisID { get; set; }


        /// <summary>
        /// 虚拟卡(当用户勾选虚拟卡后，轴对象这里需要知道当前是虚拟卡)
        /// </summary>
        public bool IsVitualCard { get; set; } = false;
        /// <summary>
        /// 轴是否在线
        /// </summary>
        public bool Online { get; set; }

        /// <summary>
        /// 是否允许编码器
        /// </summary>
        public bool EncodeEnable { get; set; } = false;

        /// <summary>
        /// 用户定义的运动目标位置
        /// </summary>
        public double TargetPos { get; set; } = 0.0;


        /// <summary>
        /// 用户定义的运动速度
        /// </summary>
        public double TargetVel { get; set; } = 0.0;


        /// <summary>
        /// 转盘分拣比较输出第A组相机数量
        /// </summary>
        public int SelectHcmpGroupACCDSum { get; set; } = 15;

        /// <summary>
        /// 转盘分拣比较输出第B组相机数量
        /// </summary>
        public int SelectHcmpGroupBCCDSum { get; set; } = 5;

        /// <summary>
        /// 转盘分拣比较输出 相机时间，单位500us
        /// </summary>
        public int CameraTime { get; set; } = 2;


        /// <summary>
        /// 取轴状态,0 表示控制轴运动完成，处于空闲状态;1 表示控制轴正在运动，其它值为调用出错
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
        /// 获取轴的报警信息（如果有硬件接入的情况下）
        /// </summary>
        public int AxisAlarm
        {
            get
            {
                return (int)((GetCardAxisAlarmState(AxisWhichCardNo, AxisID)) % 2);
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
                return (int)((GetCardAxisAlarmState(AxisWhichCardNo, AxisID) >> 1) % 2);
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
                return (int)((GetCardAxisAlarmState(AxisWhichCardNo, AxisID) >> 2) % 2);
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
                return (int)((GetCardAxisAlarmState(AxisWhichCardNo, AxisID) >> 3) % 2);
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
                return (int)((GetCardAxisAlarmState(AxisWhichCardNo, AxisID) >> 4) % 2);
            }
            set
            {

            }
        }

        /// <summary>
        /// 软件正限位信号，1有效，0无效
        /// </summary>
        public int SoftPEL
        {
            get
            {
                return (int)((GetCardAxisAlarmState(AxisWhichCardNo, AxisID) >> 6) % 2);
            }
            set
            {

            }
        }


        /// <summary>
        /// 软件负限位信号，1有效，0无效
        /// </summary>
        public int SoftMEL
        {
            get
            {
                return (int)((GetCardAxisAlarmState(AxisWhichCardNo, AxisID) >> 7) % 2);
            }
            set
            {

            }
        }


        /// <summary>
        /// INP 1：表示伺服到位信号 INP 为 ON； 0：OFF
        /// </summary>
        public int INP
        {
            get
            {
                return (int)((GetCardAxisAlarmState(AxisWhichCardNo, AxisID) >> 8) % 2);
            }
            set
            {

            }
        }



        /// <summary>
        /// EZ 1：表示 EZ 信号为 ON； 0：OFF
        /// </summary>
        public int AxisEZ
        {
            get
            {
                return (int)((GetCardAxisAlarmState(AxisWhichCardNo, AxisID) >> 9) % 2);
            }
            set
            {

            }
        }



        /// <summary>
        /// 1：表示伺服准备信号 RDY 为 ON（DMC3800 卡专用）； 0：OFF 
        /// </summary>
        public int AxisRDY
        {
            get
            {
                return (int)((GetCardAxisAlarmState(AxisWhichCardNo, AxisID) >> 10) % 2);
            }
            set
            {

            }
        }


        /// <summary>
        /// 1：表示减速停止信号 DSTP 为 ON（DMC3800 卡专用）； 0：OFF 
        /// </summary>
        public int AxisDSTP
        {
            get
            {
                return (int)((GetCardAxisAlarmState(AxisWhichCardNo, AxisID) >> 11) % 2);
            }
            set
            {

            }
        }


        public CAxisParameter AxisParameter { get; set; } = new CAxisParameter();






        /// <summary>
        /// 设置高速比较模式
        ///<para>注  意：</para>
        ///<para>1）当选择模式 1 时，只有当前位置等于比较位置时，CMP 端口才输出有效电平</para>
        ///<para>2）当选择模式 2 时，只要当前位置小于比较位置时，CMP 端口就一直保持有效电平</para>
        ///<para>3）当选择模式 3 时，只要当前位置大于比较位置时，CMP 端口就一直保持有效电平</para>
        ///<para>4）当选择模式 4 或 5 时，CMP 端口输出有效电平的时间通过 dmc_hcmp_set_config函数的 time 参数（脉冲宽度）设置</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器，取值范围：0~3（对应硬件 CMP0~CMP3 端口）</param>
        /// <param name="cmp_mode">比较模式：0：禁止（默认值）| 1：等于 | 2：小于 | 3：大于 | 4：队列 | ;提供 127 个点比较空间，采用先添加先比较，比较完可追加比较点，也可一次性添加多个比较点 | 5：线性，提供起始比较点，位置增量，比较次数  </param>
        /// <returns>错误代码</returns>
        public int SetCardHcmpEnable(int CardNo, int axis, int hcmp, int cmp_mode = 0)
        {
            if (hcmp == 1)
                cmp_enableA = cmp_mode == 0 ? false : true;
            else
                cmp_enableB = cmp_mode == 0 ? false : true;

            return 0;
            //(int CardNo, int hcmp, int cmp_enable
            //return PLTEI400HSDK.Instance.SetCardHcmpEnable(CardNo, hcmp, cmp_mode);
        }

        private bool cmp_enableA = false;
        private bool cmp_enableB = false;

        /// <summary>
        /// 设置一维位置比较器 (单轴高速)
        /// <para>注 意：</para><para>DMC3C00 后四轴不支持位置比较功能</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400ASDK：0~3</param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能 </param>
        /// <param name="cmp_source">   比较源，0：指令位置计数器，1：编码器计数器</param>
        /// <returns>错误代码</returns>
        public int OpenCardHighCompareConfig(int CardNo, int hcmp, int axis, int cmp_source, int cmp_logic, long time)//接口参数更改
        {
            if (hcmp == 1)
            {
                CardAxisSetSelectHcmpParam((ushort)CardNo, (ushort)axis, (uint)CameraTime, (ushort)SelectHcmpGroupACCDSum, (ushort)hcmp);
            }
            else
            {
                CardAxisSetSelectHcmpParam((ushort)CardNo, (ushort)axis, (uint)CameraTime, (ushort)SelectHcmpGroupBCCDSum, (ushort)hcmp);
            }
            return 0;
            //return PLTEI400HSDK.Instance.SetCardAxisHcmpConfig(CardNo, hcmp, axis, cmp_source, cmp_logic, (int)time);
        }



        /// <summary>
        /// 清除已添加的所有高速位置比较点
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="hcmp"></param>
        /// <returns></returns>
        public int ClearCardHcmpPoints(int CardNo, int hcmp)
        {
            var GroupNo = hcmp;
            if (GroupNo != 1 && GroupNo != 2) GroupNo = 1;
            if (GroupNo == 1)
            {
                selectHcmpCmmPosBufferA.Clear();
                selectHcmpCCDTriIoIndexBufferA.Clear();
            }
            else
            {
                selectHcmpCmmPosBufferB.Clear();
                selectHcmpCCDTriIoIndexBufferB.Clear();
            }

            return 0;
            //return PLTEI400HSDK.Instance.ClearCardHcmpPoints((ushort)CardNo, (ushort)hcmp);
        }


        PLT.struct_sort_config_parms sort_config_params;
        PLT.struct_sort_config_parms_e sort_config_params_e;

        /// <summary>
        /// 设置转盘分拣比较输出的控制参数. 注意调用此方法前请选置脉冲位置和编码器位置为0
        /// </summary>
        /// <returns></returns>

        public int CardAxisSetSelectHcmpParam(ushort CardNo,ushort Axisid,uint camera_time,ushort camera_count, ushort GroupNo = 1)
        {
            if (GroupNo != 1 && GroupNo != 2) GroupNo = 1;
            //var ret = PLT.Plt_AxSetCmmandPosition(CardNo, Axisid, 0);
            //ret = PLT.Plt_AxSetEncoderPosition(CardNo, Axisid, 0);

            sort_config_params.check_logic = 1; // (Byte)(comboBox3.SelectedIndex); //检测装置以上降沿作为触发沿（0 表示上升沿）
            sort_config_params.camera_count = camera_count;
            sort_config_params.camera_time = camera_time;
            sort_config_params.camera_reverse = 0; //相机触发时输出低电平
            sort_config_params.blow_count = 0; //设置使用 4 个吹气口
            sort_config_params.blow_time = 0; //设置吹气口触发时间（单位 12.5us），400 表示5ms;
            sort_config_params.blow_reverse = 0; //吹气口触发时输出高电平
            sort_config_params.width_max = 7000; //设置元件的最大宽度（单位：脉冲），超过这个宽度的过滤掉，不予处理
            sort_config_params.width_min = 1; //设置元件的最小宽度（单位：脉冲），小于这个宽度的过滤掉，不予处理
                                                //当两个元件之间的脉冲距离或者时间距离小于设置最小间距，后面的这个元件都不予处理。
            sort_config_params.distance_min = 1600; // Convert.ToUInt32(textBox22.Text); ; //设置两元件之间的最小间距（单位：脉冲），
            sort_config_params.time_min = 4; // Convert.ToUInt32(textBox23.Text); ; //设置两元件之间的最小间距（单位：250us），10表示 2.5ms;


            sort_config_params_e.check_logic = 1; // (Byte)(comboBox3.SelectedIndex); //检测装置以上降沿作为触发沿（0 表示上升沿）
            sort_config_params_e.camera_count = camera_count;
            sort_config_params_e.camera_time = camera_time;
            sort_config_params_e.camera_reverse = 0; //相机触发时输出低电平
            sort_config_params_e.blow_count = 0; //设置使用 4 个吹气口
            sort_config_params_e.blow_time = 0; //设置吹气口触发时间（单位 12.5us），400 表示5ms;
            sort_config_params_e.blow_reverse = 0; //吹气口触发时输出高电平
            sort_config_params_e.width_max = 7000; //设置元件的最大宽度（单位：脉冲），超过这个宽度的过滤掉，不予处理
            sort_config_params_e.width_min = 1; //设置元件的最小宽度（单位：脉冲），小于这个宽度的过滤掉，不予处理
                                                //当两个元件之间的脉冲距离或者时间距离小于设置最小间距，后面的这个元件都不予处理。
            sort_config_params_e.distance_min = 1600; // Convert.ToUInt32(textBox22.Text); ; //设置两元件之间的最小间距（单位：脉冲），
            sort_config_params_e.time_min = 4; // Convert.ToUInt32(textBox23.Text); ; //设置两元件之间的最小间距（单位：250us），10表示 2.5ms;
            
            return 0;
        }



        /// <summary>
        /// 启动转盘分拣比较输出
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="GroupNo">组号，只能取值1或者2</param>
        /// <returns></returns>
        public int CardAxisStartSelectHcmp(ushort CardNo,int camera_count,ushort GroupNo=1)
        {
            if (GroupNo != 1 && GroupNo != 2) GroupNo = 1;

            if (selectHcmpCCDTriIoIndexBufferA.Count != camera_count) return -1;
            if (selectHcmpCmmPosBufferA.Count != camera_count) return -2;

            short retval = 0;
            if (GroupNo == 1)
            {
                PLT.Plt_SetCameraOutMap(CardNo, sort_config_params.camera_count, selectHcmpCCDTriIoIndexBufferA.ToArray());
            }
            else
            {
                PLT.Plt_SetCameraOutMap_e(CardNo, sort_config_params_e.camera_count, selectHcmpCCDTriIoIndexBufferB.ToArray());
            }
            if (retval != 0)
            {
                return -3;
            }

            Int32[] blow_pos = new Int32[32];
            //调用分选配置参数设置函数
            if (GroupNo == 1)
            {
                retval = PLT.Plt_SortCameraBlowConfig(CardNo, sort_config_params, selectHcmpCmmPosBufferA.ToArray(), blow_pos);
            }
            else
            {
                retval = PLT.Plt_SortCameraBlowConfig_e(CardNo, sort_config_params_e, selectHcmpCmmPosBufferB.ToArray(), blow_pos);
            }

            if (retval != 0)
            {
                return -4;
            }


            short ret = 0;
            if (GroupNo == 1)
            {
                ret = PLT.Plt_StartSort(CardNo);
            }
            else
            {
                ret= PLT.Plt_StartSort_e(CardNo);
            }
            return ret;
        }

        /// <summary>
        /// 停止转盘分拣比较输出
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="GroupNo">组号，只能取值1或者2</param>
        /// <returns></returns>
        public int CardAxisStopSelectHcmp(ushort CardNo,ushort GroupNo = 1)
        {
            if (GroupNo != 1 && GroupNo != 2) GroupNo = 1;
            short ret = 0;
            if (GroupNo == 1)
            {
                ret = PLT.Plt_ClearSort(CardNo);
            }
            else
            {
                ret = PLT.Plt_ClearSort_e(CardNo);
            }
            return 0;
        }

        /// <summary>
        /// 转盘分拣比较输出的 比较位置数据对应的相机索引缓存。分拣第一组用
        /// </summary>
        private List<short> selectHcmpCCDTriIoIndexBufferA = new List<short>();
        /// <summary>
        /// 转盘分拣比较输出的 比较位置数据对应的相机索引缓存。分拣第二组用
        /// </summary>
        private List<short> selectHcmpCCDTriIoIndexBufferB = new List<short>();

        /// <summary>
        ///  转盘分拣比较输出:添加相机触发io索引
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="hcmp"></param>
        /// <param name="ioIndex"></param>
        /// <param name="GroupNo"></param>
        /// <returns></returns>
        public int AddCCDTriIO(int CardNo, int hcmp, short ioIndex)
        {
            var GroupNo = hcmp;
            if (GroupNo != 1 && GroupNo != 2) GroupNo = 1;
            if (GroupNo == 1)
            {
                selectHcmpCCDTriIoIndexBufferA.Add(ioIndex);
            }
            else
            {
                selectHcmpCCDTriIoIndexBufferB.Add(ioIndex);
            }
            return 0;
        }

        /// <summary>
        /// 转盘分拣比较输出的 比较位置数据缓存。分拣第一组用
        /// </summary>
        private List<int> selectHcmpCmmPosBufferA = new List<int>();
        /// <summary>
        /// 转盘分拣比较输出的 比较位置数据缓存。分拣第一组用
        /// </summary>
        private List<int> selectHcmpCmmPosBufferB = new List<int>();


        /// <summary>
        /// 添加/更新高速比较位置
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="hcmp"></param>
        /// <param name="cmp_pos"></param>
        /// <param name="GroupNo"></param>
        /// <returns></returns>
        public int CardAxisAddCardHcmpPoint(int CardNo, int hcmp, int cmp_pos,ushort GroupNo = 1)
        {
            if (GroupNo != 1 && GroupNo != 2) GroupNo = 1;
            if (GroupNo == 1)
            {
                selectHcmpCmmPosBufferA.Add(cmp_pos);
            }
            else
            {
                selectHcmpCmmPosBufferB.Add(cmp_pos);
            }
            return 0;
            //return PLTEI400HSDK.Instance.AddCardHcmpPoint((ushort)CardNo, (ushort)hcmp, cmp_pos);
        }

        

        public int AddCardHcmpPoint(int CardNo, int hcmp, int cmp_pos)
        {
            var GroupNo = hcmp;
            if (GroupNo != 1 && GroupNo != 2) GroupNo = 1;

            return CardAxisAddCardHcmpPoint(CardNo, GroupNo, cmp_pos);
        }


        /// <summary>
        /// 停止坐标系内所有轴的运动
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="crd"></param>
        /// <param name="stop_mode"></param>
        /// <returns></returns>
        public int CardAxisStopMulticoor(int CardNo, int crd, int stop_mode)
        {
            return PLTEI400HSDK.Instance.CardAxisStopMulticoor(CardNo, crd, stop_mode);
        }


        /// <summary>
        /// 在线改变指定轴的当前运动速度
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="Curr_Vel">改变后的运动速度，单位：pulse/s</param>
        /// <param name="Taccdec">保留参数，固定值为 0</param>
        /// <returns>错误代码</returns>
        public int CardAxisChangeSpeed(int CardNo, int axis, double Curr_Vel, double Taccdec)
        {
            return PLTEI400HSDK.Instance.CardAxisChangeSpeed((ushort)CardNo, (ushort)axis, Curr_Vel, Taccdec);
        }


        /// <summary>
        /// 设置高速比较使能
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="hcmp"></param>
        /// <param name="cmpEnable"></param>
        /// <returns></returns>
        public int SetCardAxisHcmp2dsetEnable(int CardNo, int hcmp, int cmpEnable)
        {
            return PLTEI400HSDK.Instance.SetCardAxisHcmp2dsetEnable(CardNo, hcmp, cmpEnable);
        }

        /// <summary>
        /// 启动多轴空间圆弧插补
        /// </summary>
        /// <returns></returns>
        public int CardAxesArcMoveCenterUnit(ArcInterpolateparamModel arc)
        {
            return PLTEI400HSDK.Instance.CardAxesArcMoveCenterUnit(arc);
        }

        /// <summary>
        /// 启动多轴空间直线插补 
        /// </summary>
        /// <returns></returns>
        public int CardAxesLineUnit(LineInterpolateparamModel line)
        {
            return PLTEI400HSDK.Instance.CardAxesLineUnit(line);

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
        /// <para>注 意：</para>
        /// <para>1）检测圆弧插补状态应使用坐标系状态检测函数 dmc_check_done_multicoor；停止正在执行的圆弧插补运动应使用坐标系停止函数 dmc_stop_multicoor</para>
        /// <para>2）圆弧插补设置的终点位置与理论终点位置的允许误差在+/-100 个脉冲以内。以相对坐标模式为例，当圆心位置为（0,1000），终点理论位置为（0，2000）时，而终点位置被设置为（0,2100），该圆弧插补仍可正常运行</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">指定控制卡上的坐标系号；取值范围：0~1</param>
        /// <param name="AxisList">轴列表数组</param>
        /// <param name="Target_Pos">终点坐标，单位：pulse</param>
        /// <param name="Cen_Pos">圆心坐标，单位：pulse</param>
        /// <param name="Arc_Dir">圆弧方向，0：顺时针，1：逆时针</param>
        /// <param name="posi_mode">运动模式，0：相对坐标模式，1：绝对坐标模式</param>
        /// <returns>错误代码</returns>
        public int CardAxisArcMoveCenterUnit(ArcInterpolateparamModel arc)
        {
            //ushort[] AxisNums = new ushort[arc.Axis.Length];
            //int[] Target_Pos = new int[arc.TargetPos.Length];
            //int[] Cen_Pos = new int[arc.CenPos.Length];

            //for (int i = 0; i < arc.Axis.Length; i++)
            //{
            //    AxisNums[i] = (ushort)arc.Axis[i];
            //    Target_Pos[i] = arc.TargetPos[i];
            //    Cen_Pos[i] = arc.CenPos[i];
            //}
            //return PLTEI400HSDK.Instance.CardAxisArcMoveMulticoor((ushort)arc.CardNo, (ushort)arc.Crd, AxisNums, Target_Pos, Cen_Pos, (ushort)arc.ArcDir, (ushort)arc.PosiMode);
            return PLTEI400HSDK.Instance.CardAxisArcMoveCenterUnit(arc);
        }

        /// <summary>
        /// 获取坐标系的运动状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">指定控制卡上的坐标系号（取值范围：0~1）</param>
        /// <returns>：坐标系状态，0：正在使用中，1：正常停止</returns>
        public int GetCardCheckDoneMulticoor(int CardNo, int crd)
        {
            return PLTEI400HSDK.Instance.GetCardCheckDoneMulticoor(CardNo, crd);
        }

        /// <summary>
        /// 启动两轴平面连续圆弧插补
        /// </summary>
        /// <returns></returns
        public int CardAxisContiArcMoveStart(ArcInterpolateparamModel arc)
        {
            return PLTEI400HSDK.Instance.CardAxisContiArcMoveStart(arc);
        }

        /// <summary>
        /// 启动两轴平面连续直线插补
        /// </summary>
        /// <returns></returns>
        public int CardAxisContiLineUnitStart(LineInterpolateparamModel line)
        {
            return PLTEI400HSDK.Instance.CardAxisContiLineUnitStart(line);
        }

        /// <summary>
        /// 驱动指定的轴回零，并立即返回。
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <para> pulsDir = 0  控制轴反向运动启动原点查找</para>
        /// <para>plusDir = 1  控制轴正向运动启动原点查找</para> </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int CardAxisHomeMove()
        {
            return PLTEI400HSDK.Instance.CardAxisHomeMove(AxisWhichCardNo, AxisID);
        }

        /// <summary>
        /// 读取回零执行状态
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="axis"></param>
        /// <param name="status">回零执行状态，1：回零完成，0：回零未完成</param>
        /// <returns></returns>
        public int CardAxisGetHomeResult(int CardNo, int axis, ref ushort status)
        {
            return PLTEI400HSDK.Instance.CardAxisGetHomeResult(CardNo, axis, ref status);
        }

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
        public int CardAxisLineUnit(LineInterpolateparamModel line)
        {
            //ushort[] AxisNums = new ushort[line.Axis.Length];
            //int[] Target_Pos = new int[line.TargetPos.Length];

            //for (int i = 0; i < line.Axis.Length; i++)
            //{
            //    AxisNums[i] = (ushort)line.Axis[i];
            //    Target_Pos[i] = (int)line.TargetPos[i];
            //}
            //return PLTEI400HSDK.Instance.CardAxisLineMulticoor(line.CardNo, line.Crd, line.Axis.Length, AxisNums, Target_Pos, line.PosiMode);
            return PLTEI400HSDK.Instance.CardAxisLineUnit(line);
        }

        /// <summary>
        /// 设置直线插补速度
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public int CardAxisLineUnitSpeed(LineInterpolateparamModel parameter)
        {
            //return PLTEI400HSDK.Instance.SetMultiVectorSpeed((ushort)parameter.CardNo, (ushort)parameter.Crd, parameter.minVel,
            //    parameter.maxVel, parameter.tacc);
            return PLTEI400HSDK.Instance.CardAxisLineUnitSpeed(parameter);
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
        /// <para>"RunMode">速度模式<para>0 :表示以 start_vel 为速度进行驱动，中间没有加速过程；</para>
        /// <para>1: 表示以 max_vel 为速度进行驱动，中间没有加速过程；</para>
        /// <para>2: 表示从 start_vel 加速到 max_vel,  中间有加速、减速过程；</para></para></param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int CardAxisPointMovement(MotionParamModel mpm)
        {
            return PLTEI400HSDK.Instance.CardAxisPointMovement(mpm); // mpm.CardNo, mpm.Axis, mpm.Dist, (ushort)mpm.PosiMode);
        }

        /// <summary>
        /// 启动控制轴进行连续驱动，运动方向由 plus_dir 决定
        /// </summary>
        /// <param name="mpm">参数
        /// <para>"Axis">轴号</para>
        /// <para>"Dir">驱动方向， 1 为正向驱动， 0 为反向驱动；</para>
        /// <para>"RunMode">速度模式<para>0 :表示以 start_vel 为速度进行驱动，中间没有加速过程；</para><para>1: 表示以 max_vel 为速度进行驱动，中间没有加速过程；</para><para>2: 表示从 start_vel 加速到 max_vel,  中间有加速、减速过程；</para></para></param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int CardAxisSerialMovement(MotionParamModel mpm)
        {
            return PLTEI400HSDK.Instance.CardAxisSerialMovement(mpm); // (mpm.CardNo, mpm.Axis, mpm.Dir);
        }
      
        /// <summary>
        /// 清除轴的报警状态
        /// </summary>
        /// <returns></returns>
        public int ClearCardAxisAlarmState(int CardNo, int axis)
        {
            return PLTEI400HSDK.Instance.ClearCardAxisAlarmState(CardNo, axis);
        }

        /// <summary>
        /// 清除插补坐标系
        /// </summary>
        /// <returns></returns>
        public int ClearCardAxisInterpolateCoord()
        {
            return PLTEI400HSDK.Instance.ClearCardAxisInterpolateCoord();
        }

        /// <summary>
        /// 建立插补坐标系
        /// </summary>
        /// <returns></returns>
        public int CreatCardAxisInterpolateCoord(InterpolateMotetionParamModel imp)
        {
            return PLTEI400HSDK.Instance.CreatCardAxisInterpolateCoord();
        }

        /// <summary>
        /// 读取单轴运动速度
        /// </summary>
        /// <param name="mpm">运动参数模型 
        /// <para>"CardNo"卡号</para>
        /// <para>"axis"指定轴号</para>
        /// <para>"MinVel"返回起始速度设置，单位：unit/s</para>
        /// <para>"MaxVel"返回最大速度设置，单位：unit/s</para>
        /// <para>"TaccVel"返回加速时间设置，单位：s</para>
        /// <para>"TdecVel"返回减速时间设置，单位：s</para>
        /// <para>"StopVel"返回停止速度设置，单位：unit/s</para></param>
        /// <returns>运动参数模型</returns>
        public MotionParamModel GetCardAxisAccVel(MotionParamModel mpm)
        {
            //double Min_Vel = 0, Max_Vel = 0, Tacc = 0, Tdec = 0, Stop_Vel = 0;
            //PLTEI400HSDK.Instance.GetCardVectorProfileMulticoor(mpm.CardNo, mpm.Axis,
            //   ref Min_Vel, ref Max_Vel, ref Tacc, ref Tdec, ref Stop_Vel);
            //mpm.MaxVel = (int)Max_Vel;
            //mpm.MinVel = (int)Min_Vel;
            //mpm.TaccVel = (int)Tacc;
            //mpm.TdecVel = (int)Tdec;
            //mpm.StopVel = (int)Stop_Vel;
            //return mpm;

            return PLTEI400HSDK.Instance.GetCardAxisAccVel(mpm);
        }

        /// <summary>
        /// 读取轴的状态
        /// </summary>
        public int GetCardAxisAlarmState(int CardNo, int axis)
        {
            /* 必须按dmc3400a的值来安排，下面是位号及取值含义
          /// <para> 0  、ALM 1：表示伺服报警信号 ALM 为 ON； 0：OFF</para>
          /// <para> 1  、EL+ 1：表示正硬限位信号 +EL 为 ON； 0：OFF</para>
          /// <para> 2  、EL- 1：表示负硬限位信号–EL 为 ON； 0：OFF</para>
          /// <para> 3  、EMG 1：表示急停信号 EMG 为 ON； 0：OFF</para>
          /// <para> 4  、ORG 1：表示原点信号 ORG 为 ON； 0：OFF</para>
          /// <para> 6  、SL+ 1：表示正软限位信号+SL 为 ON； 0：OFF</para>
          /// <para> 7  、SL- 1：表示负软件限位信号-SL 为 ON； 0：OFF</para>
          /// <para> 8  、INP 1：表示伺服到位信号 INP 为 ON； 0：OFF</para>
          /// <para> 9  、EZ 1：表示 EZ 信号为 ON； 0：OFF</para>
          /// <para> 10 、 RDY 1：表示伺服准备信号 RDY 为 ON（DMC3800 卡专用）；0：OFF</para>
          /// <para> 11 、 DSTP1：表示减速停止信号 DSTP 为 ON（DMC3800 卡专用）；0：OFF</para>

            /* plt的定义如下：
            typedef struct
            {
              uint8 alm_status;//alm状态；0:无效；1：有效
              uint8 el_pos_status;//正限位状态；0:无效；1：有效
              uint8 el_neg_status;//负限位状态；0:无效；1：有效
              uint8 swel_pos_status;//正软限位状态；0:无效；1：有效
              uint8 swel_neg_status;//负软限位状态；0:无效；1：有效
              uint8 emg_status;//emg状态；0:无效；1：有效
              uint8 home_status;//原点状态；0:无效；1：有效
              uint8 inp_status;//inp状态；0:无效；1：有效
              uint8 ez_status;//ez状态；0:无效；1：有效
              uint8 rdy_status;//rdy状态；0:无效；1：有效
            }struct_special_input_status;//专用输入状态
          */


            PLT.struct_special_input_status param = new PLT.struct_special_input_status();
            PLT.Plt_AxReadSpecialInputStatus((ushort)CardNo, (ushort)axis, ref param);
            var data = $"{param.alm_status}{param.el_pos_status}{param.el_neg_status}{param.emg_status}{param.home_status}{param.swel_pos_status}{param.swel_neg_status}{param.inp_status}{param.ez_status}{param.rdy_status}00000";

            var list1 = data.ToCharArray().toList();
            list1.Reverse();
            var res = new string(list1.ToArray());

            int errcode= Convert.ToInt32(res, 2);

            return errcode;
        }

        /// <summary>
        /// 读取控制轴的位置计数器，该计数器可以为输出脉冲计数器(cntr_no = 0)或者编码器反馈脉冲位置计数器(cntr_no = 1)；其对应的位置分别为指令脉冲位置（逻辑位置），或者编码器反馈脉冲位置（实际位置）。  
        /// </summary>
        /// <param name="cntr_no">输出脉冲计数器/编码器反馈脉冲位置计数器选择:  0: 输出脉冲计数器 |  1: 编码器反馈脉冲位置计数器</param>
        /// <returns>位置计数器的值, 范围在-134217728 ~ 134217727</returns>
        public double GetCardAxisCurrentPosition(int cntr_no)
        {

            double pos = 0; //
            if (cntr_no == 0)
            {
                pos = PLTEI400HSDK.Instance.GetCardAxisCurrentPosition(0,AxisWhichCardNo, AxisID);
            }
            else
            {
                pos = PLTEI400HSDK.Instance.GetCardAxisCurrentPosition(1,AxisWhichCardNo, AxisID);
            }
            return pos;
        }

        /// <summary>
        /// 读取轴状态
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="pStatus">状态</param>
        /// <returns>轴状态,0 表示控制轴运动完成，处于空闲状态;1 表示控制轴正在运动，其它值为调用出错</returns>
        public int GetCardAxisCurrentState()
        {
            int Axis_StateMachine = PLTEI400HSDK.Instance.GetCardAxisCurrentState(AxisWhichCardNo, AxisID);
            if (Axis_StateMachine == 0) return 1;
            return 0;
        }

        /// <summary>
        /// 获取轴的减速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisDecVel()
        {
            //return PLTEI400HSDK.Instance.GetCardAxisDecVel();
            return 0;
        }

        /// <summary>
        /// 获取轴的硬限位
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisELLimit()
        {
            int el_enable = 0, el_logic = 0, el_mode = 0;

            return PLTEI400HSDK.Instance.GetCardAxisELLimit(AxisWhichCardNo, AxisID, ref el_enable, ref el_logic, ref el_mode);
        }
        /// <summary>
        /// 获取轴的高速回零减速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisHighHomeDecVel()
        {
            //return PLTEI400HSDK.Instance.GetCardAxisHighHomeDecVel();
            return 0;
        }
        /// <summary>
        /// 读取轴的高速回零加速度（无）
        /// </summary>
        /// <returns></returns>
        public HomeParameModel GetCardAxisHighHomeProfile(HomeParameModel hpm)
        {
            return null;
        }
        /// <summary>
        /// 读取轴的高速回零速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisHighHomeVel()
        {
            return 0;
        }
        /// <summary>
        /// 读取轴的回零最大保护距离
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisHomeMaxProtectDis()
        {
            return 0;
        }
        /// <summary>
        /// 读取轴回零的模式
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisHomeMode()
        {
            UInt16 home_dir = 0, mode = 0, EZ_count = 0;
            double vel = 0;

            if (PLTEI400HSDK.Instance.GetCardAxisHomeMode(AxisWhichCardNo, AxisID, mode) == 1)
            {
                return mode;
            }
            return -1;
        }
        /// <summary>
        /// 读取轴的回零位置偏差
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisHomePositionErro()
        {
            return -1;
        }

        /// <summary>
        /// 读取轴回零的参数
        /// </summary>
        public int GetCardAxisHomeProfile()
        {
            return -1;
        }
        /// <summary>
        /// 读取轴的初速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisInitVel(ref int pSpeed)
        {
            if (IsVitualCard)
            {
                pSpeed = 0; return 0;
            }
            double pspeed = 0;
            int result = 0;
            pspeed = PLTEI400HSDK.Instance.GetCardAxisInitVel(AxisWhichCardNo, AxisID, ref pspeed);
            result = (int)pspeed;
            return result;
        }
        /// <summary>
        /// 设置轴的加加速
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisJerkVel()
        {
            return -1;
        }
        /// <summary>
        /// 读取轴的限位状态
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisLimitState()
        {
            return -1;
        }
        /// <summary>
        /// 读取轴的低速回零加速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisLowHomeAccVel()
        {
            return -1;
        }
        /// <summary>
        /// 读取轴的低速回零减速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisLowHomeDecVel()
        {
            return -1;
        }
        /// <summary>
        /// 读取轴的低速回零速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisLowHomeVel()
        {
            return -1;
        }

        /// <summary>
        /// 读取轴的最大速度
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisMaxVel()
        {
            return -1;
        }
        /// <summary>
        /// 获取轴的脉冲当量
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisPulseEquival(int card, int axis)
        {
            return PLTEI400HSDK.Instance.GetCardAxisPulseEquival(card, axis);
        }
        /// <summary>
        /// 读取轴的反向回零距离
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisReverseHomeDis()
        {
            return -1;
        }
        /// <summary>
        /// 获取轴当前的运行模式
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisRunMode(int CardNo, int axis)
        {

            return -1;
        }

        /// <summary>
        /// 设置轴的减减速
        /// </summary>
        /// <returns></returns>
        public int GetCardAxisSlowVel()
        {
            return 0;
        }

        /// <summary>
        /// 读取轴的软限位设置
        /// </summary>
        /// <returns>限位参数模型</returns>
        public LimitParamModel GetCardAxisSoftLimit(LimitParamModel lpm)
        {
            UInt16 enable = 0;
            UInt16 source_sel = 0;
            UInt16 SL_action = 0;
            int N_limit = 0;
            int P_limit = 0;
            PLTEI400HSDK.Instance.GetCardAxisSoftLimit(lpm);
            lpm.Enable = enable;
            lpm.SourceSel = source_sel;
            lpm.SLAction = SL_action;
            lpm.Nlimit = N_limit;
            lpm.Plimit = P_limit;
            return lpm;
        }

        /// <summary>
        /// 读取卡的当前状态
        /// </summary>
        /// <returns></returns>
        public int GetCardCurrentState(int CardNo, int channel)
        {
            return -1;
        }

        /// <summary>
        /// 获取错误信息
        /// </summary>
        /// <param name="ErrNum"></param>
        /// <returns></returns>
        public string GetErrorInfo(int ErrNum)
        {
            var res = PLTEI400HSDK.Instance.GetErrorInfo(ErrNum);
            return res;
        }

        /// <summary>
        /// 打开轴的使能
        /// </summary>
        /// <returns></returns>
        public int OpenCardAxisEnable(int CardNo, int axis)
        {
            return -1;
        }

        /// <summary>
        /// 设置一维位置比较器 (单轴低速)
        /// <para>注 意：</para><para>DMC3C00 后四轴不支持位置比较功能</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400ASDK：0~3</param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能 </param>
        /// <param name="cmp_source">   比较源，0：指令位置计数器，1：编码器计数器</param>
        /// <returns>错误代码</returns>
        public int OpenCardCompareConfig(int CardNo, int axis, int enable, int cmp_source)//接口参数更改
        {
            return PLTEI400HSDK.Instance.SetCardAxisCompareConfig(CardNo, axis, enable, cmp_source);
        }

        //WORD CardNo,WORD hcmp,WORD axis, WORD cmp_source, WORD cmp_logic,long time
       

        /// <summary>
        /// 设置轴的加速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisAccVel()
        {
            return -1;
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
            return 0;
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
        /// 设置一维位置比较输出数据
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisCompareConfigDate(int CardNo, int axis, int enable, int cmp_source)
        {
            return PLTEI400HSDK.Instance.SetCardAxisCompareConfig(CardNo, axis, enable, cmp_source);
        }
        /// <summary>
        /// 设置轴的当前位置
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisCurrentPosition(int CardNo, int axis, int pos)
        {
            return PLTEI400HSDK.Instance.SetCardAxisCurrentPosition(CardNo, axis, pos);
        }


        /// <summary>
        ///  设置编码器位置
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="axis"></param>
        /// <param name="encoder_value"></param>
        /// <returns></returns>
        public int SetCardAxisEncoder(int CardNo, int axis, int encoder_value)
        {
            return PLTEI400HSDK.Instance.SetCardAxisEncoder(CardNo, axis, encoder_value);
        }

        /// <summary>
        /// 设置 EtherCAT 总线驱动器失能
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴的轴号，255 表示使能所有 EtherCAT 轴</param>
        /// <returns>错误代码</returns>
        public int SetCardAxisDisable(int CardNo, int axis)
        {
            return 0;
        }

        /// <summary>
        /// 设置EL 限位信号
        /// <para> name=" CardNo "：控制卡卡号 </para>
        /// <para> name=" axis "：指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400ASDK：0~3 </para>
        /// <para> name=" lpm.Enable "：EL 信号的使能状态：0：正负限位禁止 1：正负限位允许 2：正限位禁止、负限位允许 3：正限位允许、负限位禁止</para>
        /// <para> name=" lpm.Plimit "：EL 信号的有效电平：0：正负限位低电平有效 1：正负限位高电平有效 2：正限位低有效，负限位高有效 3：正限位高有效，负限位低有效</para>
        /// <para> name=" lpm.SLAction "：EL 制动方式：0：正负限位立即停止  1：正负限位减速停止  2：正限位立即停止，负限位减速停止</para>
        /// </summary>
        /// <returns>错误代码 </returns>
        public int SetCardAxisELLimit(LimitParamModel lpm)
        {
            return PLTEI400HSDK.Instance.SetCardAxisELLimit(lpm);
        }

        /// <summary>
        /// 错误时设置停止模式(减速停止、突然停止)
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="stop_mode">停止模式(0:减速停止、1:突然停止)</param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardAxisErrorStopMode(int axis, int stop_mode)
        {
            return PLTEI400HSDK.Instance.SetCardAxisErrorStopMode( axis, stop_mode);
        }

        /// <summary>
        /// 设置轴的高速回零加速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisHighHomeAccVel()
        {
            return -1;
        }

        /// <summary>
        /// 设置轴的高速回零减速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisHighHomeDecVel()
        {
            return -1;
        }

        /// <summary>
        /// 设置轴的高速回零速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisHighHomeVel()
        {
            return -1;
        }

        /// <summary>
        /// 设置轴的回零最大保护距离
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisHomeMaxProtectDis()
        {
            return -1;
        }

        /// <summary>
        /// 设置指定轴的回零模式（适用于所有脉冲卡）
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
        public int SetCardAxisHomeMode(HomeParameModel hpm)
        {
            return PLTEI400HSDK.Instance.SetCardAxisHomeMode(hpm);
        }

        /// <summary>
        /// 设置回零遇限位是否反找。（DMC3C00，3400A 默认使能，DMC3800，3600 默认不使能）
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="axis"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        public int SetCardAxisHomeElReturn(int CardNo, int axis, ushort enable)
        {
            /*
             short dmc_set_home_el_return(WORD CardNo,WORD axis,WORD enable)
            功 能：设置回零遇限位是否反找
            参 数：CardNo 控制卡卡号
            axis 指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5
             DMC3400A：0~3
            Enable 使能是否遇限位反找
            返回值：错误代码
            说 明：设置回零过程当中遇限位是否反找，需要在配置回零函数时配置，一次即可，以后均
            不用配置。DMC3C00，3400A 默认使能，DMC3800，3600 默认不使能。限位反找功能只针对非限
            位回零方式起作用。
             */
            return PLTEI400HSDK.Instance.SetCardAxisHomeElReturn((ushort)CardNo, (ushort)axis, enable);
        }

        /// <summary>
        /// 设置轴的回零位置偏差
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisHomePositionErro()
        {
            return -1;
        }

        /// <summary>
        ///设置回零参数
        /// </summary>
        /// <returns> 正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardAxisHomeProfile(HomeParameModel hpm)
        {
            return -1;
        }

        /// <summary>
        /// 设置轴初始速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInitVel()
        {
            return -1;
        }

        /// <summary>
        /// 设置插补减速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInterpolateDecVel()
        {
            return -1;
        }

        /// <summary>
        /// 设置插补加加速
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInterpolateJerkVel()
        {
            return -1;
        }

        /// <summary>
        /// 设置插补初始速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInterpolateMaxVel()
        {
            return -1;
        }

        /// <summary>
        /// 设置插补低速
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInterpolateSlowVel()
        {
            return -1;
        }

        /// <summary>
        /// 设置插补参数 
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisInterpolateVectorProfile(InterpolateMotetionParamModel imp)
        {
            return -1;
        }

        /// <summary>
        /// 设置轴的加加速
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisJerkVel()
        {
            return -1;
        }

        /// <summary>
        /// 设置轴的低速回零加速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisLowHomeAccVel()
        {
            return -1;
        }

        /// <summary>
        /// 设置轴的低速回零减速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisLowHomeDecVel()
        {
            return -1;
        }

        /// <summary>
        /// 设置轴的低速回零速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisLowHomeVel()
        {
            return -1;
        }

        /// <summary>
        /// 设置轴的最大速度
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisMaxVel()
        {
            return -1;
        }

        /// <summary>
        /// 设置单轴运动速度
        /// </summary>
        public int SetCardAxisMotionalVel(MotionParamModel mpm)
        {
            return PLTEI400HSDK.Instance.SetCardAxisMotionalVel(mpm);
        }

        /// <summary>
        /// 设置位置计数器 
        /// </summary>
        /// <returns>正常返回 0；  出现错误时返回非 0 值； </returns>
        public int SetCardAxisPos(int cntr_on, int Pos)
        {
            return -1;
        }
        /// <summary>
        /// 设置轴的脉冲当量
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisPulseEquival(int CardNo, int axis, int equiv)
        {
            return PLTEI400HSDK.Instance.SetCardAxisPulseEquival(CardNo, axis, equiv); ;
        }

        /// <summary>
        /// 设置轴的反向回零距离
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisReverseHomeDis()
        {
            return -1;
        }
        /// <summary>
        /// 设置轴的运行模式
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisRunMode(int CardNo, int axis, ushort runmode)
        {
            return -1;
        }

        /// <summary>
        /// 设置轴的减减速
        /// </summary>
        /// <returns></returns>
        public int SetCardAxisSlowVel()
        {

            return 0;
        }

        /// <summary>
        /// 设置软限位 
        ///  <para>注  意：</para>
        /// <para>正、负限位位置可为正数也可为负数，但正限位位置应大于负限位位置 </para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400ASDK：0~3 </param>
        /// <param name="enable">使能状态，0：禁止，1：允许</param>
        /// <param name="source_sel">计数器选择，0：指令位置计数器，1：编码器计数器</param>
        /// <param name="SL_action">限位停止方式，0：减速停止，1：立即停止 </param>
        /// <param name="N_limit">负限位位置，单位：pulse </param>
        /// <param name="P_limit">正限位位置，单位：pulse </param>
        /// <returns>错误代码 </returns>
        public int SetCardAxisSoftLimit(LimitParamModel lpm)
        {
            return PLTEI400HSDK.Instance.SetCardAxisSoftLimit(lpm);
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
        /// <para>"jerk_percent"过程使用 S 曲线的比例，范围为 0~1; 0 表示没有 S 曲线部分(也就是 T 型曲线加减速) | 1 表示全部 S 曲线加减速；</para> </param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardAxisSProfile(MotionParamModel mpm)
        {
            return PLTEI400HSDK.Instance.SetCardAxisSProfile(mpm);
        }

        /// <summary>
        /// 获取单轴运动速度曲线  
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="Min_Vel">返回起始速度，单位：pulse/s (最大值为 2M)</param>
        /// <param name="Max_Vel">返回最大速度，单位：pulse/s (最大值为 2M)</param>
        /// <param name="Tacc">返回加速时间，单位：s (最小值为 0.001s)</param>
        /// <param name="Tdec">返回减速时间，单位：s (最小值为 0.001s)</param>
        /// <param name="stop_vel">返回停止速度，单位：pulse/s (最大值为 2M)</param>
        /// <returns>：错误代码</returns>
        public int GetCardAxisProfile(int CardNo, int axis, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double stop_vel)
        {
            return PLTEI400HSDK.Instance.GetCardAxisProfile(CardNo, axis,ref Min_Vel,ref Max_Vel,ref Tacc,ref Tdec,ref stop_vel);
        }


        /// <summary>
        /// 设置单轴速度曲线 S 段参数值
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5 DMC3400A：0~3 </param>
        /// <param name="s_mode">保留参数，固定值为 0</param>
        /// <param name="s_para">返回设置的 S 段时间，单位：s；范围：0~0.5 s</param>
        /// <returns>错误代码</returns>
        public int GetCardAxisSProfile(int CardNo, int axis, int s_mode, ref double s_para)
        {
            return PLTEI400HSDK.Instance.GetCardAxisSProfile(CardNo, axis, s_mode, ref s_para);
        }

        /// <summary>
        /// 读取指定轴的伺服使能端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <returns>伺服使能端口电平，0：低电平(on)，1：高电平(off)</returns>
        public int GetCardAxisSevonPin()
        {
            return PLTEI400HSDK.Instance.GetCardAxisSevonPin(AxisWhichCardNo, AxisID);
        }


        /// <summary>
        /// 控制指定轴的伺服使能端口的输出
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400A：0~3</param>
        /// <param name="on_off">设置伺服使能端口电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int CardAxisWriteSevonPin(int on_off)
        {
            return PLTEI400HSDK.Instance.CardAxisWriteSevonPin(AxisWhichCardNo, AxisID, on_off);
        }


        private static readonly object dmc3000apilock = new object();

        /// <summary>
        /// 配置控制轴使用 T 型速度曲线加减速，并设置相应的起始速度、最大速度、加速度、减速度参数。（LTDMC.dmc_set_profile） 
        /// <para>调用该函数后，该轴的点位运动、连续运动、回零运动、减速停止将使用这些速度、加速度参数进行驱动。</para> 
        /// </summary>
        /// <param name="mpm">运动参数模型
        /// <para>"Axis">轴号</para>
        /// <para>"MinVel">启动速度，单位: pps, 最大值为 1000000pps </para>
        /// <para>"MaxVel">最大速度, 单位: pps, 最大值为 1000000pps </para>
        /// <para>"TaccVel">加速度，单位： pps/s, 最大值为 1000000000pps/s </para>
        /// <para>"TdecVel">减速度，单位: pps/s, 最大值为 1000000000pps/s </para></param>
        /// <returns>正常返回 0；  出现错误时返回非 0 值；</returns>
        public int SetCardAxisTProfile(MotionParamModel mpm)
        {
            lock (dmc3000apilock)
            {
                //return public int SetCardAxisProfile(int CardNo, int axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double stop_vel)
                return PLTEI400HSDK.Instance.SetCardAxisTProfile(mpm);
            }
        }
        
        /// <summary>
        /// 函数调用打印输出设置
        /// <para>注 意：</para>
        /// <para>使能打印输出后，可监控运动函数库的调用情况。在用户调用函数时，将输出相关信息，并保存在指定文件路径中；函数设置模式 2 全部不打印需配合最新动态库（20181030 及以后动态库）使用。</para>
        /// </summary>
        /// <param name="CardNo">打印输出模式，0：只打印报错函数，1：全部打印，2：全部不打印</param>
        /// <param name="FileName">文件保存路径：参数文件名+后缀：相对路径 ；完整描述参数文件的路径+文件名后缀：绝对路径</param>
        /// <returns>错误代码</returns>
        public int SetCardDebugMode(int CardNo, string FileName)
        {
            return PLTEI400HSDK.Instance.SetCardDebugMode(CardNo, FileName);
        }

        /// <summary>
        /// 设置二维位置比较器
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="enable">二维位置比较功能状态，0：禁止，1：使能 </param>
        /// <param name="cmp_source">二维位置比较源，0：指令位置计数器，1：编码器计数器</param>
        /// <returns>错误代码</returns>
        public int SetCardCompareConfigExtern(int CardNo, int enable, int cmp_source)
        {
            return PLTEI400HSDK.Instance.SetCardCompareConfigExtern(CardNo, enable, cmp_source);
        }

        /// <summary>
        /// 设置二维位置比较输出数据
        /// </summary>
        /// <returns></returns>
        public int SetCardCompareConfigExternDate(PositionComparatorParamModel pcpm)
        {
            return -1;
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
            return -1;
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
            return PLTEI400HSDK.Instance.GetCardAxisLatchValueExtern(CardNo, axis,Index);
        }


        /// <summary>
        /// 从 PC 缓存中读取锁存器已锁存个数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <returns></returns>
        public int GetCardAxisLatchFlagExtern(int CardNo, int axis)
        {
            return PLTEI400HSDK.Instance.GetCardAxisLatchFlagExtern((ushort)CardNo, (ushort)axis);
        }

        /// <summary>
        /// 配置高速比较器
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
            return PLTEI400HSDK.Instance.SetCardAxisHcmpConfig((ushort)CardNo, (ushort)hcmp, (ushort)axis, (ushort)cmp_source, (ushort)cmp_logic, time);
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
            return PLTEI400HSDK.Instance.SetCardHcmpLiner((ushort)CardNo, (ushort)hcmp, Increment, Count);
        }
     

        public int SetCardAxisLatchMode(int CardNo, int axis, int all_enable, int latch_source, int triger_chunnel)
        {
            return PLTEI400HSDK.Instance.SetCardAxisLatchMode((ushort)CardNo, (ushort)axis, (ushort)all_enable, (ushort)latch_source, (ushort)triger_chunnel);
        }

        /// <summary>
        /// 设置指定轴的 LTC 信号
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="axis"></param>
        /// <param name="ltc_logic"></param>
        /// <param name="ltc_mode"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public int SetCardAxisLtcMode(int CardNo, int axis, UInt16 ltc_logic, UInt16 ltc_mode, double filter)
        {
            return PLTEI400HSDK.Instance.SetCardAxisLtcMode((ushort)CardNo, (ushort)axis,ltc_logic, ltc_mode, filter);
        }

        /// <summary>
        /// 从控制卡内读取锁存器的值
        /// <para>注 意：</para><para>：当选择锁存方式为单次锁存时，用此函数读取锁存值</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <returns>锁存值</returns>
        public int GetCardAxisLatchValue(int CardNo, int axis)
        {
            return PLTEI400HSDK.Instance.GetCardAxisLatchValue((ushort)CardNo, (ushort)axis);
        }
        /// <summary>
        /// 从控制卡内读取指定卡内锁存器的标志位
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00/3800：0~7，DMC3600：0~5,DMC3400A：0~3</param>
        /// <returns>0：未触发锁存，1：已触发锁存</returns>
        public int GetCardAxisLatchFlag(int CardNo, int axis)
        {
            return PLTEI400HSDK.Instance.GetCardAxisLatchFlag((ushort)CardNo, (ushort)axis);
        }
        public int SetCardHcmpCmpPin(int CardNo, int hcmp, int on_off)
        {
            return PLTEI400HSDK.Instance.SetCardHcmpCmpPin((ushort)CardNo, (ushort)hcmp, (ushort)on_off);
        }
        /// <summary>
        /// 读取指定 CMP 端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器，取值范围：0~3（对应硬件 CMP0~CMP3 端口）</param>
        /// <returns>CMP 端口电平</returns>
        public int GetCardHcmpCmpPin(int CardNo, int hcmp)
        {
            return PLTEI400HSDK.Instance.GetCardHcmpCmpPin((ushort)CardNo, (ushort)hcmp);
        }
        /// <summary>
        /// 复位指定卡的锁存器的标志位
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        public int GetCardAxisReserLatchFlag(int CardNo, int axis)
        {
            return PLTEI400HSDK.Instance.GetCardAxisReserLatchFlag((ushort)CardNo, (ushort)axis);
        }
        /// <summary>
        /// 设置轴的螺距补偿
        /// </summary>
        /// <returns></returns>
        public int SetCardLeadScrewCompConfig(PitchCompensationparamModel pcm)
        {
            return -1;
        }
        /// <summary>
        /// 打开轴的螺距补尝
        /// </summary>
        /// <returns></returns>
        public int SetCardLeadScrewCompEnable(int CardNo, int axis, int enable)
        {
            return -1;
        }



        /// <summary>
        /// 读取高速比较参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器，取值范围：0~3（对应硬件 CMP0~CMP3 端口）</param>
        /// <param name="remained_points">返回可添加比较点数</param>
        /// <param name="current_point">返回当前比较点位置，单位：pulse</param>
        /// <param name="runned_points">返回已比较点数</param>
        /// <returns>错误代码</returns>
        public int GetCardHcmpCurrentState(int CardNo, int hcmp, ref int remained_points, ref int current_point, ref int runned_points)
        {
            return PLTEI400HSDK.Instance.GetCardHcmpCurrentState((ushort)CardNo, (ushort)hcmp, ref remained_points, ref current_point, ref runned_points);
            //return LTDMC.dmc_hcmp_get_current_state((ushort)CardNo, (ushort)hcmp, ref remained_points, ref current_point, ref runned_points);
        }


        /// <summary>
        /// 指定轴停止运动
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">指定轴号，取值范围：DMC3C00：0~11，DMC3800：0~7，DMC3600：0~5 DMC3400ASDK：0~3</param>
        /// <param name="stop_mode">制动方式，0：减速停止，1：紧急停止</param>
        /// <returns>错误代码</returns>
        public int StopCardAxisMovement(int CardNo, int axis, int stop_mode)
        {
            return PLTEI400HSDK.Instance.StopCardAxisMovement(CardNo, axis, stop_mode);
        }
        /// <summary>
        /// 设置减速轴停止时间
        /// </summary>
        /// <param name="axis">卡号</param>
        /// <param name="CardNo">轴号</param>
        /// <param name="stopTime">停止时间</param>
        public int SetAxisStopTime(int CardNo, int axis, double stopTime)
        {
            //return DMC1000SSDK.Instance.DmcSetCardAxisDecStopTime(CardNo, axis, stopTime);
            return -1;
        }
        public int SetSpacing(int axis, int ORGPlusPos, int PELAlarmPlusPos, int NELAlarmPlusPos)
        {
            throw new NotImplementedException();
        }

        public int SetStatusParameter(int axis, bool f, AxisStatusParameterEnum kind)
        {
            return 0;
        }

        public int SetCardAxisCompareConfig(int CardNo, int axis, int enable, int cmp_source)
        {
            return LTDMC.dmc_compare_set_config((ushort)CardNo, (ushort)axis, (ushort)enable, (ushort)cmp_source);
        }

        public string GetEtherCATErrorInfo(int ErrNum)
        {
            throw new NotImplementedException();
        }

        public List<int> GetLineInterpolationSupportAxis()
        {
            var list = new List<int>();
            switch (AxisWhichcardType)
            {

                case "13312":
                    //res = AllMotionCardEnum.DMC3400A.ToString();
                    for(int i=0;i<4;i++)
                    {
                        list.Add(i);
                    }
                    break;
                case "14336":
                    //res = AllMotionCardEnum.DMC3800.ToString();
                    for (int i = 0; i < 8; i++)
                    {
                        list.Add(i);
                    }
                    break;
                case "15360":
                    for (int i = 0; i < 12; i++)
                    {
                        list.Add(i);
                    }
                    break;
                case "54320":
                      for (int i = 0; i <2; i++)
                    {
                        list.Add(i);
                    }
                    break;
            }
            return list;
        }

        public int DmcSetCardVectorSProfile(int CardNo, int Crd, int s_mode, double s_para)
        {
            throw new NotImplementedException();
        }

        public int AddCardAxisComparePoint(int CardNo, int axis, int pos, int dir, ushort action, uint actpara)
        {
            throw new NotImplementedException();
        }

        public int ClearCardAxisComparePoints(int CardNo, int axis)
        {
            throw new NotImplementedException();
        }

        public double GetCardAxisCurrentSpeed(int CardNo, int axis)
        {
            double currentSpeed = 0;
            var res = PLTEI400HSDK.Instance.GetCardAxisInitVel(CardNo, axis, ref currentSpeed);
            if (res == 0) return currentSpeed;
            return res;
        }

        public int SetCardAxisInpMode(int CardNo, int axis, int enable, int inp_logic)
        {
            throw new NotImplementedException();
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
