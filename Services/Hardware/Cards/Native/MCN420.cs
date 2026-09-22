﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/DLL/MCN420.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;      //使用C#导入dll必须的

namespace Samsun.Domain.MotionCard.Common.YKMCN42Series
{
    public class MCN420
    {
        //---------------------------------------------------------------------------------------------------
        public const double PI = 3.1415926535897931;
        public const int MaxAxisNumber = 8;
        public const int MaxInterpChannel = 2;
        //---------------------------------------------------------------------------------------------------
        //#pragma pack(1)   //设置结构体的边界对齐为1个字节，也就是所有数据在内存中是连续存储的。

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Init_Param                         //设置默认参数初始化
        {
            public uint CmdNum;
            public uint Init_CMD;                       //参数保留
            public uint Channel;                        // 0 or 1
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.R8)]
            public double[] iAxisSLMTP;//8                //正软件限位位置 
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.R8)]
            public double[] iAxisSLMTN;//8                //负软件限位位置 
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.R8)]
            public double[] iEncRatio;//8                 //编码器比率
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.R8)]
            public double[] iCommadRatio;//8              //轴指令比率 
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] iStopMotion;//8               //轴停止方式  0：减速停；1：立刻停
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] AxisPulseOutMode;//8          //轴脉冲输出模式  0 ：脉冲+方向；1：正交脉冲；2：正负脉冲；3：脉冲 + 方向； 
            //4 ：脉冲+方向；5：正交脉冲；6：正负脉冲；7：脉冲 + 方向；   取反了脉冲输出
            //8 ：脉冲+方向；9：正交脉冲；10：正负脉冲；11：脉冲 + 方向； 取反了方向输出
            //12 ：脉冲+方向；13：正交脉冲；14：正负脉冲；15：脉冲 + 方向； 全取反输出
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] AxisEncodeInMode;//8          //轴编码器输入模式  0:4倍频正交输入；1倍频正交输入；2：脉冲+方向输入
            public double InterpoVel;                   //默认插补目标速度
            public double InterpoStartVel;              //默认插补开始速度
            public double InterpoEndVel;                //默认插补结束速度
            public double InterpoAcc;                   //默认插补加速度 
            public double InterpoDec;                   //默认插补减速度 
            public double InterpJerkAcc;                //默认插补加加速度
            public double InterpJerkDec;                //默认插补减减速度 
            public double InterpoAccTime;               //默认插补加速时间 
            public double InterpoDecTime;               //默认插补减速时间  
            public uint InterpoType;                  //默认插补规划类型 0：T型；1：S型 
            public double InterpoMaxAng;                //默认插补最大角速度
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] AxisAlmInPutEnable;//8        //轴报警输入使能  0：不使能；1：使能   
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] AxisAlmInPutSelect;//8        //轴报警输入位选  0~127  分别选择0~127作为报警输入
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] AxisPLimitInPutEnable;//8     //轴正限位输入使能  0：不使能；1：使能
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] AxisPLimitInPutSelect;//8     //轴正限位输入位选  0~127  分别选择0~127作为正限位输入
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] AxisNLimitInPutEnable;//8     //轴负限位输入使能  0：不使能；1：使能
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] AxisNLimitInPutSelect;//8     //轴负限位输入位选  0~127  分别选择0~127作为负限位输入
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] AxisHomeInPutEnable;//8       //轴原点输入使能  0：不使能；1：使能
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] AxisHomeInPutSelect;//8       //轴原点输入位选  0~127  分别选择0~127作为原点输入
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] AxisSoftPLimitEnable;//8      //轴软件限位输入使能  0：不使能；1：使能
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] AxisSoftNLimitEnable;//8      //轴软件限位输入使能  0：不使能；1：使能 参数号 32~39
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] AxisHandSelectEnable;//8      //手轮轴选使能  0：不使能；1：使能 参数号 40~47
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] AxisHandInputSelect;//8       //手轮轴选输入位选 0~127  分别选择0~127作为手轮轴选输入
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U4)]
            public uint[] HandWheelRatioEnable;//4      //手轮倍率使能输入  0：不使能；1：使能
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U4)]
            public uint[] HandWheelRatioInputSelect;//4 //手轮倍率输入位选  0~127  分别选择0~127作为手轮倍率输入选择
            public uint EmgInPutEnable;//               //急停输入使能  0：不使能；1：使能
            public uint EmgInPutSelect;//               //轴急停输入位选  0~127  分别选择0~127作为急停输入
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] InputInvertBit; //32          //输入取反位  
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Set_Param                                  //参数设置指令
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                  //参数保留
            public uint Channel;                                     // 0 or 1
            public uint Group_ID;                                    //参数组号  0~9 参考 Init_Param结构体
            public uint Param_ID;                                    //参数号    参考 Init_Param结构体 
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] data;  //8                                   //参数值 实际是一个 double或两个 uint（使用memcpy拷贝进去即可）
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Home_Req                                   //回零指令
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                  //参数保留
            public uint Channel;                                     // 0 or 1
            public uint axis;                                       //轴号 0~7
            public double LowSpeed;                                    //回零低速
            public double HighSpeed;                                   //回零高速
            public double Acc;                                         //回零加速度
            public uint HomeMode;                                    //回零模式 0：三开关，1：只有零点开关，2：只有正限位开关，3：只有负限位开关
            public uint TDirTime;                                    //反向时间 单位毫秒
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Jog_Req_Param                              //连续速度指令
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                 //参数保留
            public uint Channel;                                     // 0 or 1
            public uint axis;                                       //轴号 0~7
            public uint STFlag;                                      //速度类型 0：T型，1：S型
            public uint ParaType;                                     //参数类型 0：无速度参数,1：有加速度参数；
            public Int32 Dir;                                         //方向 1：正方向，-1：负方向
            public double InterpoVel;                                  //目标速度
            public double InterpoStartVel;                             //开始速度
            public double InterpoEndVel;                               //结束速度
            public double InterpoAcc;                                  //加速度或加速时间
            public double InterpoDec;                                  //减速度或减速时间
            public double InterpJerkAcc;                               //加加速度
            public double InterpJerkDec;                               //减减速度
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Jog_Req_NParam                             //连续速度指令（不带速度参数，使用默认参数）
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                  //参数保留
            public uint Channel;                                     // 0 or 1
            public uint axis;                                       //轴号 0~7
            public uint STFlag;                                      //速度类型 0：T型，1：S型
            public uint ParaType;                                    //参数类型 0：无速度参数
            public Int32 Dir;                                         //方向 1：正方向，-1：负方向
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Single_Req_Param                           //单轴定长指令（不带速度参数，使用默认参数）
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                  //参数保留
            public uint Channel;                                     // 0 or 1
            public uint axis;                                       //轴号 0~7
            public uint STFlag;                                      //速度类型 0：T型，1：S型
            public uint ParaType;                                    //参数类型 1：有加速度参数，2：有加减时间参数
            public uint PosType;                                     //位置类型：0相对，1：绝对
            public double AxisPos;                                     //位置
            public double InterpoVel;                                  //目标速度
            public double InterpoStartVel;                             //开始速度
            public double InterpoEndVel;                               //结束速度
            public double InterpoAcc;                                  //加速度或加速时间
            public double InterpoDec;                                  //减速度或减速时间
            public double InterpJerkAcc;                               //加加速度
            public double InterpJerkDec;                               //减减速度
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Single_NReq_Param                          //单轴定长指令（不带速度参数，使用默认参数）
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                  //参数保留
            public uint Channel;                                     // 0 or 1
            public uint axis;                                       //轴号 0~7
            public uint STFlag;                                      //速度类型 0：T型，1：S型
            public uint ParaType;                                    //参数类型 0：无速度参数
            public uint PosType;                                     //位置类型：0相对，1：绝对
            public double AxisPos;                                     //位置
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Line_Req_Param                             //多轴直线插补指令
        {
            public uint CmdNum;                                    //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                  //参数保留
            public uint Channel;                                   // 0 or 1
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] axis; //8                                 //轴号列表 0~7 ,使用前瞻时只能0~3 ，其中通道1的4~7就是4~7轴  //连续
            public uint STFlag;                                    //速度类型 0：T型，1：S型
            public uint ParaType;                                  //参数类型 1：有加速度参数，2：有加减时间参数
            public uint PosType;                                   //位置类型：0相对，1：绝对
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.R4)]
            public double[] AxisPos; //8                               //位置列表
            public uint nAxisNum;                                  //轴数量
            public double InterpoVel;                                //目标速度
            public double InterpoStartVel;                           //开始速度
            public double InterpoEndVel;                             //结束速度
            public double InterpoAcc;                                //加速度或加速时间
            public double InterpoDec;                                //减速度或减速时间
            public double InterpJerkAcc;                             //加加速度
            public double InterpJerkDec;                             //减减速度
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Line_Req_NParam                             //多轴直线插补指令（不带速度参数，使用默认参数）
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                   //参数保留
            public uint Channel;                                     // 0 or 1
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] axis;//8                                    //轴号列表 0~7 ,使用前瞻时只能0~3 ，其中通道1的4~7就是4~7轴
            public uint STFlag;                                      //速度类型 0：T型，1：S型
            public uint ParaType;                                    //参数类型  0：无速度参数
            public uint PosType;                                     //位置类型：0相对，1：绝对
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.R8)]
            public double[] AxisPos;//8                                  //位置列表
            public uint nAxisNum;                                    //轴数量
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ARC_2D_Req_Param                            //2维圆弧插补指令
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                   //参数保留
            public uint Channel;                                     // 0 or 1
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U4)]
            public uint[] axis;//2                                    //轴号列表 0~7 ,使用前瞻时只能0~3 ，其中通道1的4~7就是4~7轴
            public uint STFlag;                                      //速度类型 0：T型，1：S型
            public uint ParaType;                                    //参数类型 1：有加速度参数，2：有加减时间参数
            public uint PosType;                                     //位置类型：0相对，1：绝对
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U4)]
            public double[] AxisPos;//2                                  //位置列表
            public double Radius;                                      //弧度 逆时针为负，顺时针为正
            public double InterpoVel;                                  //目标速度
            public double InterpoStartVel;                             //开始速度
            public double InterpoEndVel;                               //结束速度
            public double InterpoAcc;                                  //加速度或加速时间
            public double InterpoDec;                                  //减速度或减速时间
            public double InterpJerkAcc;                               //加加速度
            public double InterpJerkDec;                               //减减速度
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Arc_2D_3Point_Req_Param                      //2维3点圆弧
        {
            public uint CmdNum;                                //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                           //默认
            public uint channel;                               // 0 or 1
            public uint vel_mode;                              //速度模式：0：T型速度曲线；1：S型速度曲线
            public uint mode;                                  //坐标位置模式;0 相对位置；1：绝对位置；
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U4)]
            public uint[] axis_list;                             //轴号列表
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.R8)]
            public double[] start_pos;                             //起始位置点
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.R8)]
            public double[] mid_pos;                                //中间位置点
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.R8)]
            public double[] aim_pos;                                 //目标位置点
                                                                     //--------------------------------------------------------------------------
            public double synVel;                                  //目标速度
            public double velStart;                                //开始速度
            public double velEnd;                                  //结束速度
            public double synAcc;                                  //加速度或加速时间
            public double synDec;                                  //减速度或减速时间
            public double synJerkAcc;                              //加加速度
            public double synJerkDec;                              //减减速度
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ARC_2D_Req_NParam                           //2维圆弧插补指令（不带速度参数，使用默认参数）
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                   //参数保留
            public uint Channel;                                     //通道 0 or 1
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U4)]
            public uint[] axis;//2                                    //轴号列表 0~7 ,使用前瞻时只能0~3 ，其中通道1的4~7就是4~7轴
            public uint STFlag;                                      //速度类型 0：T型，1：S型
            public uint ParaType;                                    //参数类型 0：无速度参数
            public uint PosType;                                     //位置类型：0相对，1：绝对
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.R8)]
            public double[] AxisPos; //2                                 //位置列表
            public double Radius;                                      //弧度 逆时针为负，顺时针为正
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CMD_3DArc_Req_Param                         //3维圆弧插补指令（三点定圆弧）,不能使用在连续轨迹模式下
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                    //参数保留
            public uint Channel;                                     // 0 or 1
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.U4)]
            public uint[] axis;//3                                    //轴号列表 0~7 (分别是X,Y,Z的轴号)
            public uint STflag;                                      //速度类型 0：T型，1：S型
            public uint ParaType;                                    //参数类型 1：带速度参数
            public uint PosType;                                     //位置类型：0相对，1：绝对
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.R8)]
            public double[] AxisPos;  //6                                //位置列表（分别是圆弧上的点X,Y,Z的坐标和圆弧终点X,Y,Z的坐标）注：开始点坐标为当前点坐标
            public double InterpoVel;                                  //目标速度
            public double InterpoStartVel;                             //开始速度
            public double InterpoEndVel;                               //结束速度
            public double InterpoAcc;                                  //加速度或加速时间
            public double InterpoDec;                                  //减速度或减速时间
            public double InterpJerkAcc;                               //加加速度
            public double InterpJerkDec;                               //减减速度
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CMD_3DArc_Req_NParam                        //3维圆弧插补指令（三点定圆弧）,不能使用在连续轨迹模式下
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                   //参数保留
            public uint Channel;                                     // 0 or 1
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.U4)]
            public uint[] axis;//3                                    //轴号列表 0~7 (分别是X,Y,Z的轴号)
            public uint STflag;                                      //速度类型 0：T型，1：S型
            public uint ParaType;                                    //参数类型 0：无速度参数
            public uint PosType;                                     //位置类型：0相对，1：绝对
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.R8)]
            public double[] AxisPos;//6                                  //位置列表（分别是圆弧上的点X,Y,Z的坐标和圆弧终点X,Y,Z的坐标）注：开始点坐标为当前点坐标
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Helix_2D_Req_Param                          //2维渐变螺旋插补命令
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                   //参数保留
            public uint Channel;                                     // 0 or 1
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U4)]
            public uint[] axis;//2                                    //轴号列表 0~7 ,使用前瞻时只能0~3 ，其中通道1的4~7就是4~7轴
            public uint STFlag;                                      //速度类型 0：T型，1：S型
            public uint ParaType;                                    //参数类型 1：有加速度参数，2：有加减时间参数
            public uint PosType;                                     //位置类型：0相对，1：绝对
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.R8)]
            public double[] AxisPos; //2                                 //位置列表
            public double Radius;                                      //弧度 逆时针为负，顺时针为正
            public double EndRadius;                                   //目标半径
            public double InterpoVel;                                  //目标速度
            public double InterpoStartVel;                             //开始速度
            public double InterpoEndVel;                               //结束速度
            public double InterpoAcc;                                  //加速度或加速时间
            public double InterpoDec;                                  //减速度或减速时间
            public double InterpJerkAcc;                               //加加速度
            public double InterpJerkDec;                               //减减速度
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Helix_2D_Req_NParam                         //2维渐变螺旋插补命令（不带速度参数，使用默认参数）
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                   //参数保留
            public uint Channel;                                     //通道 0 or 1
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U4)]
            public uint[] axis; //2                                   //轴号列表 0~7 ,使用前瞻时只能0~3 ，其中通道1的4~7就是4~7轴
            public uint STFlag;                                      //速度类型 0：T型，1：S型
            public uint ParaType;                                    //参数类型 0：无速度参数
            public uint PosType;                                     //位置类型：0相对，1：绝对
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.R8)]
            public double[] AxisPos;  //2                                //位置列表
            public double Radius;                                      //弧度 逆时针为负，顺时针为正
            public double EndRadius;                                   //目标半径
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Helix_3D_Req_Param                          //3维螺旋插补命令
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                   //参数保留
            public uint Channel;                                     // 0 or 1
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.U4)]
            public uint[] axis; //3                                   //轴号列表 0~7 ,使用前瞻时只能0~3 ，其中通道1的4~7就是4~7轴  最后一个为直线轴
            public uint STFlag;                                      //速度类型 0：T型，1：S型
            public uint ParaType;                                    //参数类型 1：有加速度参数，2：有加减时间参数
            public uint PosType;                                     //位置类型：0相对，1：绝对
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.R8)]
            public double[] AxisPos;//3                                  //位置列表  
            public double Radius;                                      //弧度 逆时针为负，顺时针为正
            public double InterpoVel;                                  //目标速度
            public double InterpoStartVel;                             //开始速度
            public double InterpoEndVel;                               //结束速度
            public double InterpoAcc;                                  //加速度或加速时间
            public double InterpoDec;                                  //减速度或减速时间
            public double InterpJerkAcc;                               //加加速度
            public double InterpJerkDec;                               //减减速度
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Helix_3D_Req_NParam                         //3维螺旋插补命令（不带速度参数，使用默认参数）
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                   //参数保留
            public uint Channel;                                     //通道 0 or 1
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.U4)]
            public uint[] axis; //3                                   //轴号列表 0~7 ,使用前瞻时只能0~3 ，其中通道1的4~7就是4~7轴  最后一个为直线轴
            public uint STFlag;                                      //速度类型 0：T型，1：S型
            public uint ParaType;                                    //参数类型 0：无速度参数
            public uint PosType;                                     //位置类型：0相对，1：绝对
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.R8)]
            public double[] AxisPos;//3                                  //位置列表
            public double Radius;                                      //弧度 逆时针为负，顺时针为正
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Cone_3D_Req_Param                           //3维圆锥插补命令
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                   //参数保留
            public uint Channel;                                     // 0 or 1
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.U4)]
            public uint[] axis;//3                                    //轴号列表 0~7 ,使用前瞻时只能0~3 ，其中通道1的4~7就是4~7轴  最后一个为直线轴
            public uint STFlag;                                      //速度类型 0：T型，1：S型
            public uint ParaType;                                    //参数类型 1：有加速度参数，2：有加减时间参数
            public uint PosType;                                     //位置类型：0相对，1：绝对
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.R8)]
            public double[] AxisPos;//3                                  //位置列表
            public double Radius;                                      //弧度 逆时针为负，顺时针为正
            public double EndRadius;                                   //目标半径
            public double InterpoVel;                                  //目标速度
            public double InterpoStartVel;                             //开始速度
            public double InterpoEndVel;                               //结束速度
            public double InterpoAcc;                                  //加速度或加速时间
            public double InterpoDec;                                  //减速度或减速时间
            public double InterpJerkAcc;                               //加加速度
            public double InterpJerkDec;                               //减减速度
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Cone_3D_Req_NParam                          //3维圆锥插补命令（不带速度参数，使用默认参数）
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                   //参数保留
            public uint Channel;                                     //通道 0 or 1
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.U4)]
            public uint[] axis;//3                                    //轴号列表 0~7 ,使用前瞻时只能0~3 ，其中通道1的4~7就是4~7轴  最后一个为直线轴
            public uint STFlag;                                      //速度类型 0：T型，1：S型
            public uint ParaType;                                    //参数类型 0：无速度参数
            public uint PosType;                                     //位置类型：0相对，1：绝对
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.R8)]
            public double[] AxisPos;//3                                  //位置列表
            public double Radius;                                      //弧度 逆时针为负，顺时针为正
            public double EndRadius;                                   //目标半径
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Set_Ahead_Param                             //前瞻参数设置指令
        {
            public uint Channel;                                     //通道 0 or 1
            public double InterpoStartVel;                             //最低速度
            public double InterpoVel;                                  //目标速度
            public double InterpoAcc;                                  //加速度
            public double InterpJerkAcc;                               //加加速度
            public double InterpoDec;                                  //加速度
            public double InterpJerkDec;                               //加加速度
            public double MaxAngVel;                                   //最大角速度
            public uint STFlag;		                              //S型或T型的标志
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SetInputCounter_Req                         //设置输入16位计数器 计数器溢出后 65535 + 1 = 0   0-1 = 65535
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint CmdName;                                       //参数保留
            public uint Channel;                                     //通道 0 or 1
            public uint CounterNumber;                              //计数号，范围0~7 总共8个独立计数器号
            public uint Enable;                                      //1使能 0非使能
            public uint Mode;                                        //0~7 , 0:单触0发上升沿 + 1；1：单触发0下降沿+1；2：单触发0上升沿-1；3：单触发0下降沿-1; //4:双触发0上升沿 + 1 ，1上升-1；5：双触发0下降沿+1 ， 1下降-1；6：双触发0上升+1 ，1下降-1；7：双触发0下降+1， 1上升-1；                                                     
            public uint iInputSelect;                                //0~31，0输入选择
            public uint dInputSelect;                                //0~31，1输入选择
            public Int32 data;                                        // 设置计数器值 0~65535
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SetOutputDelayFlip_Req                      //设置输出延时翻转功能（只有独立的1路设置）
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint CmdName;                                       //参数保留
            public uint Channel;                                     //通道 0 or 1
            public uint Mode;                                        // 0:写入触发；1：IO输入上升沿触发；2：IO输入下降沿触发；3：位置触发
            public uint Enable;                                      //1使能 0非使能
            public uint iSelect;                                     //IO输入触发时0~31选择输入；位置触发时：0~7选择0~7轴逻辑位置触发，8~11选择0~3轴编码器位置触发
            public uint Pos;                                         //触发位置
            public uint Times;                                       //翻转除数0为一直翻转，1~65535为翻转次数
            public uint OutputSelect;                                //0~15,
            public double msDelay;                                     //延时时间ms
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SetOutputDelayFlip_ReqLogic                      //设置输出延时翻转功能（只有独立的1路设置）；
        {
            public uint CmdNum;                                     //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下；
            public uint CmdName;                                    //参数保留
            public uint Channel;                                    //通道 0 or 1
            public uint Mode;                                       //0:写入触发；1：IO输入上升沿触发；2：IO输入下降沿触发；3：位置触发；
            public uint Enable;                                     //1使能 0非使能
            public uint iSelect;                                    //IO输入触发时0~31选择输入；位置触发时：0~7选择0~7轴逻辑位置触发，8~11选择0~3轴编码器位置触发；
            public uint Pos;                                        //触发位置
            public uint Times;                                      //翻转除数0为一直翻转，1~65535为翻转次数；
            public uint OutputSelect;                               //0~15；
            public double msLowLogicDelay;                           //低电平延时时间ms；
            public double msHighLogicDelay;                          //高电平延时时间ms；                
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Delay_Time_Req                              //延时运行指令，相当于G04
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                   //参数保留
            public uint Channel;                                     // 0 or 1
            public uint Times;                                       //延时时间值ms
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct IO_InPut_Valid                              //等待输入有效指令
        {
            public uint CmdNum;                                      //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                                   //参数保留
            public uint Channel;                                     // 0 or 1
            public uint Bit;                                         //IO号 0~127
            public uint data;                                        //IO数据有效值 0或1 
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PWM_SET_Req                             ///PWM输出控制设置
        {
            public uint CmdNum;                                  //行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
            public uint ParamRemain;                             //参数保留
            public uint PwmID;                                   // 0:全部参数一次设置 1：使能 2：设置频率 3：设置比率 4：设置模式 5：设置速度跟随参数
            public uint PwmChannel;                              //0 or 1
            public uint Enable;                                  // 0:不使能，1：使能（设置1和0时更新） 
            public double PwmFreq;                                        //PWM频率 单位Hz   （设置2和0时更新）
            public double PwmRate;                                        //占空比   0~1      （设置3和0时更新） 
            public uint PwmMode;                                 //0：直接输出模式，1：速度跟随模式（跟随下面设置的轴的和速度） （设置4和0时更新）
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] axis;                                //轴列表  （设置5和0时更新）
            public uint AxisNum;                                 //轴数量  （设置5和0时更新）
            public double SpeedRate;                                      //速度为1输出占空比的量  （设置5和0时更新）
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct IP_Param                             ///PWM输出控制设置
        {
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U1)]
            public byte[] ip;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U1)]
            public byte[] mask;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U1)]
            public byte[] getway;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ReadAllAxisStatusData
        {
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U1)]
            public byte[] IoInput;                        //通用输入0-32,0表示低电平，1表示高电平；
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U1)]
            public byte[] IoOutput;                       //通用输出0-16,0表示低电平，1表示高电平；
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U1)]
            public byte[] AxisRunStatus;                  //轴运动状态，0在运动中，1停止；
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U1)]
            public byte[] AlmInput;                       //轴输入报警ALM信号状态，0：正常，1：报警。
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U1)]
            public byte[] PLimitInput;                    //轴输入正硬件限位信号状态，0表示无效；1表示有效。
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U1)]
            public byte[] NLimitInput;                    //轴输入负硬件限位信号状态，0表示无效；1表示有效。
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U1)]
            public byte[] OrgInput;                       //轴输入原点信号状态，0表示无效；1表示有效。

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.I4)]
            public int[] AxisMotionAlmStatus;                                  //轴运行状态信息，0表示正常；bit0为1是暂停；bit1为1是急停；bit2为1是减速停止；bit3为1是正硬件限位停止；bit4为1是负硬件限位停止；bit5为1正软件限位停止；bit6为1是负软件限位停止；bit7为1是驱动器报警。
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.R8)]
            public double[] AxisVel;                               //轴速度，单位是unit/ms；
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.R8)]
            public double[] AxisCmdPos;                            //轴位置，单位是unit；
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.R8)]
            public double[] AxisEncoderPos;                        //编码器位置，单位是unit；
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ReadAxisSyscnPos
        {
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.R8)]
            public double[] syncCmdPos;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.R8)]
            public double[] syncEncPos;
        }
        //主轴参数
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Master_Req_Param                           //单轴定长指令（不带速度参数，使用默认参数）
        {
            public uint axis;                                       //轴号 0~7
            public uint STFlag;                                      //速度类型 0：T型，1：S型
            public uint ParaType;                                    //参数类型 1：有加速度参数，2：有加减时间参数
            public uint PosType;                                     //位置类型：0相对，1：绝对
            public double AxisPos;                                     //位置
            public double InterpoVel;                                  //目标速度
            public double InterpoStartVel;                             //开始速度
            public double InterpoEndVel;                               //结束速度
            public double InterpoAcc;                                  //加速度或加速时间
            public double InterpoDec;                                  //减速度或减速时间
            public double InterpJerkAcc;                               //加加速度
            public double InterpJerkDec;                               //减减速度
        }

        //从轴参数
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Slave_Req_Param                           //单轴定长指令（不带速度参数，使用默认参数）
        {
            public uint axis;                                       //轴号 0~7
            public uint STFlag;                                      //速度类型 0：T型，1：S型
            public uint ParaType;                                    //参数类型 1：有加速度参数，2：有加减时间参数
            public uint PosType;                                     //位置类型：0相对，1：绝对
            public double AxisPos;                                     //位置
            public double InterpoVel;                                  //目标速度
            public double InterpoStartVel;                             //开始速度
            public double InterpoEndVel;                               //结束速度
            public double InterpoAcc;                                  //加速度或加速时间
            public double InterpoDec;                                  //减速度或减速时间
            public double InterpJerkAcc;                               //加加速度
            public double InterpJerkDec;                               //减减速度
        }

        //#pragma pack()
        //=================================================================================================================================================
        //板卡配置	
        /*
        功    能：为控制卡分配系统资源，并初始化控制卡。
        参    数：无。
        返回值：卡数：0 ~ 16，其中 0表示没有卡。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_board_init", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint YK_board_init();

        /*
        DWORD YK_board_initExp (char *szStartip,int szLocahostlPort,char* szLocalhostIp)
        功    能：为控制卡分配系统资源，并初始化控制卡。
        参    数：szStartip,控制卡内部IP起始地址；比如设置192.168.1.30；
        szLocahostlPort为为本地主机的网络端口
        szLocalhostIp为本地主机连接控制器的网卡的IP地址；
        返回值：卡数：0 ~ 16，其中 0表示没有卡。
        */

        [DllImport("MCN420.dll", EntryPoint = "YK_board_initExp", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint YK_board_initExp(String szDeviceStartip, Int32 szLocahostlPort, String szLocalhostIp);
        //*****************************************************************************
        /*
        功    能：关闭控制卡，释放系统资源。
        参    数：无。
        返回值：正确：返回 ERR_NoError；
        错误：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_board_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint YK_board_close();
        //******************************************************************************************************************************************
        /*
        功能：初始化运动控制卡运行参数
        参数：	CardNo 控制卡卡号
        param 运行参数,类型是Init_Param结构体。调用该函数前首先要申请一个Init_Param结
        构体变量，然后给对应的结构体成员赋值，最后将该结构体变量作为参数
        注意：使用运动控制前要先执行该函数
        结构体成员介绍：
        CmdNum;
        Init_CMD：参数保留；
        Channel：0 or 1；
        iAxisSLMTP[8]：正软件限位位置；
        iAxisSLMTN[8]：负软件限位位置；

        iEncRatio[8]：编码器比率 ；
        iCommadRatio[8]：轴指令比率；

        iStopMotion[8]：轴停止方式  0：减速停；1：立刻停；

        AxisPulseOutMode[8]：轴脉冲输出模式 ：
        0 ：脉冲+方向；1：正交脉冲；2：正负脉冲；3：脉冲 + 方向；
        4 ：脉冲+方向；5：正交脉冲；6：正负脉冲；7：脉冲 + 方向；取反了脉冲输出；
        8 ：脉冲+方向；9：正交脉冲；10：正负脉冲；11：脉冲 + 方向；取反了方向输出；
        12 ：脉冲+方向；13：正交脉冲；14：正负脉冲；15：脉冲 + 方向；全取反输出；
        AxisEncodeInMode[8]：轴编码器输入模式0:4倍频正交输入；1倍频正交输入；2：脉冲+方向输入；
        InterpoVel：默认插补目标速度, 单位是“unit/ms” ；
        InterpoStartVel：默认插补开始速度, 单位是unit/ms；
        InterpoEndVel：默认插补结束速度, 单位是unit/ms；
        InterpoAcc：默认插补加速度,单位是unit/ms²；
        InterpoDec：默认插补减速度,单位是unit/ms²；
        InterpJerkAcc：默认插补加加速度, 单位是unit/ms³；
        InterpJerkDec：默认插补减减速度, 单位是unit/ms³；
        InterpoAccTime：默认插补加速时间,单位是ms；
        InterpoDecTime：默认插补减速时间, 单位是ms；
        InterpoType：默认插补规划类型 0：T型；1：S型；
        InterpoMaxAng：默认插补最大角速度，单位是弧度/s；

        AxisAlmInPutEnable[8]：轴报警输入使能；0：不使能；1：使能；
        AxisAlmInPutSelect[8]：轴报警输入位选0~127分别选择0~127作为报警输入；
        AxisPLimitInPutEnable[8]：轴正限位输入使能；0：不使能；1：使能；
        AxisPLimitInPutSelect[8]：轴正限位输入位选0~127分别选择0~127作为正限位输入；
        AxisNLimitInPutEnable[8]：轴负限位输入使能；0：不使能；1：使能；
        AxisNLimitInPutSelect[8]：轴负限位输入位选0~127分别选择0~127作为负限位输入；
        AxisHomeInPutEnable[8]：轴原点输入使能；0：不使能；1：使能；
        AxisHomeInPutSelect[8]：轴原点输入位选0~127分别选择0~127作为原点输入；
        AxisSoftPLimitEnable[8]：轴软件限位输入使能；0：不使能；1：使能；
        AxisSoftNLimitEnable[8]：轴软件限位输入使能；0：不使能；1：使能；
        AxisHandSelectEnable[8]：手轮轴选使能；0：不使能；1：使能 ；
        AxisHandInputSelect[8]：手轮轴选输入位选0~127分别选择0~127作为手轮轴选输入；
        HandWheelRatioEnable[4]：手轮倍率使能输入0：不使能；1：使能；
        HandWheelRatioInputSelect[4]：手轮倍率输入位选0~127分别选择0~127作为手轮倍率输入选择；
        EmgInPutEnable：急停输入使能；0：不使能；1：使能；
        EmgInPutSelect：轴急停输入位选；0~127  分别选择0~127作为急停输入；
        InputInvertBit[32]：输入取反位；

        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_init_param", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_init_param(uint CardNo, ref Init_Param pParam);
        //下载参数
        [DllImport("MCN420.dll", EntryPoint = "YK_download_configfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_download_configfile(uint CardNo, string FileName);
        //*****************************************************************************************************************************************
        /*
        功   能：设置指定轴的脉冲输出模式；
        参   数：CardNo：表示卡号;
        Axis：表示轴号，范围0~7;
        out_mode：表示指定轴的输出脉冲模式，范围0-15，详细描述见《MCN420用户使用手册》第七章表7.1；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_axis_out_pulse_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_axis_out_pulse_mode(uint CardNo, uint axis, uint out_mode);
        //******************************************************************************************************************************************
        /*
        功能：读取指定轴的脉冲输出模式
        参数：CardNo 表示卡号v
        axis 表示轴号，范围0~7;
        out_mode 返回指定轴的脉冲输出方式
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_axis_out_pulse_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_axis_out_pulse_mode(uint CardNo, uint axis, ref uint out_mode);
        //******************************************************************************************************************************************
        /*
        功  能：设置指令位置比率；
        参  数：CardNo：表示卡号；
        axis：表示轴号，范围0~7;
        command_ratio：表示指定轴的比率（puls/unit）;
        返回值：错误代码。
        指令比率设置说明：控制卡一个单位为1mm，假设10000个脉冲电机转一圈，螺杆的螺距为1234.5mm，那么指令比率应该设置为10000/ 1234.5 =8.100445。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_command_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_command_ratio(uint CardNo, uint axis, double command_ratio);
        //*****************************************************************************************************************************************
        /*
        功  能：读取指定轴的指令位置比率；
        参  数：CardNo：表示卡号；
        axis：表示轴号，范围0~7;
        command_ratio：返回指定轴的比率（puls/unit）;
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_command_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_command_ratio(uint CardNo, uint axis, ref double command_ratio);
        //*****************************************************************************************************************************************
        /*
        功  能：设置编码器比率；
        参  数：CardNo：表示卡号；
        axis：表示轴号，范围0~3;
        encoder_ratio：表示指定轴的编码器比率（unit/puls）；
        返回值：错误代码
        编码器比率设置说明：控制卡一个单位为1mm，假设电机转一圈返回10000个脉冲，螺杆的螺距为1234.5mm，那么编码器比率应该设置为1234.5 / 10000 = 0.12345。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_encoder_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_encoder_ratio(uint CardNo, uint axis, double encoder_ratio);
        //*****************************************************************************************************************************************
        /*
        功  能：读取指定轴编码器比率
        参  数：CardNo：表示卡号；
        axis：表示轴号，范围0~3;
        encoder_ratio：返回指定轴的编码器比率（unit/puls）；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_encoder_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_encoder_ratio(uint CardNo, uint axis, ref double encoder_ratio);

        //*******************************************************************************************************************
        /*
        功  能：设置指定轴的运动停止模式
        参  数：CardNo： 控制卡卡号；
        axis： 指定轴号；取值范围为0~7；
        stop_mode：轴停止方式  0：减速停；1：立刻停
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_stop_motion_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_stop_motion_mode(uint CardNo, uint axis, uint stop_mode);

        //*******************************************************************************************************************************************
        /*
	    功  能：读取指定轴的运动停止模式
	    参  数：CardNo： 控制卡卡号；
	            axis： 指定轴号；取值范围为0~7；
	            stop_mode：轴停止方式  0：减速停；1：立刻停
	    返回值：错误代码。
	    */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_stop_motion_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_stop_motion_mode(uint CardNo, uint axis, ref uint stop_mode);
        //****************************************************************************************************************
        /*
        功  能：设置指定轴的编码器脉冲输入模式
        参  数：CardNo： 控制卡卡号；
        axis： 指定轴号；取值范围为0~3；
        encoder_puls_mode：编码器输入模式， 0:4倍频正交输入；1倍频正交输入；2：脉冲+方向输入；4:4倍频正交输入(反向)；5：1倍频正交输入(反向)；6：方向 + 脉冲输入；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_encoder_counter_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_encoder_counter_inmode(uint CardNo, uint axis, uint encoder_puls_mode);
        //*****************************************************************************************************************
        /*
        功  能：读取指定轴的编码器脉冲输入模式
        参  数：CardNo： 控制卡卡号；
        axis： 指定轴号；取值范围为0~3；
        encoder_puls_mode：返回编码器输入模式， 0:4倍频正交输入；1倍频正交输入；2：脉冲+方向输入；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_encoder_counter_inmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_encoder_counter_inmode(uint CardNo, uint axis, ref uint encoder_puls_mode);

        //**********************************************************************************************************************
        /*
        功  能：设置指定轴的报警输入使能以及报警输入的端口号；
        参  数：CardNo： 控制卡卡号；
        axis ：指定轴号；取值范围为0~7；
        alm_en： 轴报警输入使能；0不使能；1使能；
        注意：使能报警输入，需要根据输入取反函数设置好报警的电平关系，否则出现轴报警将影响轴运动；
        alm_map_inbit：映射(设定)alm信号的输入端口号，取值范围0~127；
        返回值：错误代码。

        备注：报警（Alarm）信号电平设置：
        MCN420卡的通用输入端口的电平默认为有效高电平，如果要单独设定，需要调用输入取反函数YK_set_input_bit_inverted 来设定。
        MCN420卡的指定轴的报警（Alarm）信号设置：根据配置的限位开关映射对应的输入信号编号(输入端口号)，使用YK_set_input_bit_inverted设置对应的输入信号有效电平，对应的位为0表示高电平有效，对应的为1表示低电平有效；
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_alarm_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_alarm_config(uint CardNo, uint axis, uint alm_en, uint alm_map_inbit);
        //*********************************************************************************************************************************
        /*
	    功  能：读取指定轴已设置的报警输入使能以及报警输入的端口号；
	    参  数：CardNo： 控制卡卡号；
	            axis ：指定轴号；取值范围为0~7；
	            alm_en： 返回轴报警输入使能；0不使能；1使能；
	    注  意：使能报警输入，需要根据输入取反函数设置好报警的电平关系，否则出现轴报警将影响轴运动；
	            alm_map_inbit：返回映射alm信号的输入端口号；
	    返回值：错误代码。
	    */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_alarm_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_alarm_config(uint CardNo, uint axis, ref uint alm_en, ref uint alm_map_inbit);
        //***********************************************************************************************************************************
        /*
	    功  能：设置指定轴的报警输入使能、报警输入的端口号以及电平；
	    参  数：CardNo： 控制卡卡号；
	            axis ：指定轴号；取值范围为0~7；
	            alm_en： 轴报警输入使能；0不使能；1使能；
	    注  意：使能报警输入，需要根据输入取反函数设置好报警的电平关系，否则出现轴报警将影响轴运动；
	            alm_map_inbit：映射(设定)alm信号的输入端口号，取值范围0~127；
	            logic_on_of: 端口号对应的有效电平，0低电平，1高电平；
	    返回值：错误代码。
	    */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_alarm_config_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_alarm_config_ex(uint CardNo, uint axis, uint alm_en, uint alm_map_inbit, uint logic_off_on);
        //*********************************************************************************************************************************
        /*
        功  能：设置指定轴的正限位输入使能以及正限位输入的端口号;
        参  数：CardNo :控制卡卡号；
        axis 指定轴号；取值范围为0~7；
        p_limit_en：轴正限位输入使能；0不使能；1使能；
        p_limit_map_inbit：映射(设定)正限位信号的输入端口号，取值范围0~127；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_positive_limit_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_positive_limit_config(uint CardNo, uint axis, uint p_limit_en, uint p_limit_map_inbit);
        //*********************************************************************************************************************************
        /*
	    功  能：读取指定轴的已设定的正限位输入使能以及正限位输入的端口号;
	    参  数：CardNo :控制卡卡号；
	            axis 指定轴号；取值范围为0~7；
	            p_limit_en：返回轴正限位输入使能；0不使能；1使能；
	            p_limit_map_inbit：返回映射(设定)正限位信号的输入端口号；
	    返回值：错误代码。
	    */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_positive_limit_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_positive_limit_config(uint CardNo, uint axis, ref uint p_limit_en, ref uint p_limit_map_inbit);
        //*********************************************************************************************************************************
        /*
        功  能：设置指定轴的负限位输入使能以及负限位输入的端口号;
        参  数：CardNo 控制卡卡号；
        axis 指定轴号；取值范围为0~7；
        n_limit_en ：轴负限位输入使能；0不使能；1使能；
        n_limit_map_inbit：映射(设定)负限位信号的输入端口号，取值范围0~127；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_negative_limit_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_negative_limit_config(uint CardNo, uint axis, uint n_limit_en, uint n_limit_map_inbit);
        //*********************************************************************************************************************************
        /*
	    功  能：读取指定轴已设定的负限位输入使能以及负限位输入的端口号;
	    参  数：CardNo 控制卡卡号；
	            axis 指定轴号；取值范围为0~7；
	            n_limit_en ：返回轴负限位输入使能；0不使能；1使能；
	            n_limit_map_inbit：返回映射(设定)负限位信号的输入端口号；
	    返回值：错误代码。
	    */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_negative_limit_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_negative_limit_config(uint CardNo, uint axis, ref uint n_limit_en, ref uint n_limit_map_inbit);
        //**********************************************************************************************************************************
        /*
        功  能：设置指定轴的软限位正向限位输入使能以及正向软件限位位置
        参  数：CardNo 控制卡卡号；
        axis 指定轴号；取值范围为0~7；
        soft_p_limit_en：轴软限位正向限位输入使能；0不使能；1使能；
        p_limit_pos：正向软件限位位置，单位是unit；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_soft_positive_limit_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_soft_positive_limit_config(uint CardNo, uint axis, uint soft_p_limit_en, double p_limit_pos);
        //*********************************************************************************************************************************
        /*
	    功  能：读取指定轴的软限位正向限位输入使能以及正向软件限位位置
	    参  数：CardNo 控制卡卡号；
	            axis 指定轴号；取值范围为0~7；
	            soft_p_limit_en：返回轴软限位正向限位输入使能；0不使能；1使能；
	            p_limit_pos：返回正向软件限位位置，单位是unit；
	    返回值：错误代码。
	    */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_soft_positive_limit_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_soft_positive_limit_config(uint CardNo, uint axis, ref uint soft_p_limit_en, ref double p_limit_pos);
        //**********************************************************************************************************************************
        /*
        功  能：设置指定轴的软限位负向限位输入使能以及负向软件限位位置
        参  数：CardNo 控制卡卡号；
        axis 指定轴号；取值范围为0~7；
        soft_n_limit_en：轴软限位负向限位输入使能；0不使能；1使能；
        n_limit_pos：负向软件限位位置，单位是unit；
        返回值：错误代码。
        */

        [DllImport("MCN420.dll", EntryPoint = "YK_set_soft_negative_limit_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_soft_negative_limit_config(uint CardNo, uint axis, uint soft_n_limit_en, double n_limit_pos);

        //**********************************************************************************************************************************
        /*
	    功  能：读取指定轴的软限位负向限位输入使能以及负向软件限位位置
	    参  数：CardNo 控制卡卡号；
	            axis 指定轴号；取值范围为0~7；
	            soft_n_limit_en：返回轴软限位负向限位输入使能；0不使能；1使能；
	            n_limit_pos：返回负向软件限位位置，单位是unit；
	    返回值：错误代码。
	    */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_soft_negative_limit_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_soft_negative_limit_config(uint CardNo, uint axis, ref uint soft_n_limit_en, ref double n_limit_pos);
        //***********************************************************************************************************************************
        /*
        功  能：设置指定轴的原点输入使能以及原点输入的端口号;
        参  数：CardNo: 控制卡卡号；
        axis: 指定轴号；取值范围为0~7；
        org_en: 轴原点输入使能；0不使能；1使能；
        org_map_inbit：映射(设定)原点信号的输入端口号，取值范围0~127；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_origin_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_origin_config(uint CardNo, uint axis, uint org_en, uint org_map_inbit);
        //****************************************************************************************************************************
        /*
	    功  能：读取指定轴的原点输入使能以及原点输入的端口号;
	    参  数：CardNo: 控制卡卡号；
	            axis: 指定轴号；取值范围为0~7；
	            org_en: 返回轴原点输入使能；0不使能；1使能；
	            org_map_inbit：返回映射(设定)原点信号的输入端口号；
	    返回值：错误代码。
	    */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_origin_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_origin_config(uint CardNo, uint axis, ref uint org_en, ref uint org_map_inbit);
        //***********************************************************************************************************************************
        /*
        功  能：设置指定轴的手轮输入使能以及轴手轮输入的端口号映射
        参  数：CardNo 控制卡卡号；
        axis 指定轴号；取值范围为0~7；
        axis_hwheel_en 手轮轴选使能；0不使能；1使能；
        axis_map_inbit：映射(设定)手轮轴选信号的输入端口号，取值范围0~127；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_handwheel_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_handwheel_config(uint CardNo, uint axis, uint axis_hwheel_en, uint axis_map_inbit);

        /*
	    功  能：读取指定轴的手轮输入使能以及轴手轮输入的端口号映射
	    参  数：CardNo: 控制卡卡号；
	            axis: 指定轴号；取值范围为0~7；
	            axis_hwheel_en 返回手轮轴选使能；0不使能；1使能；
	            axis_map_inbit：返回映射(设定)手轮轴选信号的输入端口号；
	    返回值：错误代码。
	    */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_handwheel_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_handwheel_config(uint CardNo, uint axis, ref uint axis_hwheel_en, ref uint axis_hwheel_map_inbit);
        //***********************************************************************************************************************************
        /*
        功  能：设置指定轴的手轮倍率输入以及轴手轮倍率输入的端口号
        参  数：CardNo 控制卡卡号；
        channel 手轮倍率输入使能通道；取值范围为0~4；
        channel_ratio_en 手轮倍率输入使能；0不使能；1使能；
        channel_map_inbit：映射(设定)手轮倍率输入信号的输入端口号，取值范围0~127；
        返回值：错误代码
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_handwheel_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_handwheel_ratio(uint CardNo, uint channel, uint channel_ratio_en, uint channel_map_inbit);

        //**********************************************************************************************************************************
        /*
        功  能：读取指定轴的手轮倍率输入以及轴手轮倍率输入的端口号
        参  数：CardNo 控制卡卡号；
        channel 手轮倍率输入使能通道；取值范围为0~4；
        channel_ratio_en 返回手轮倍率输入使能；0不使能；1使能；
        channel_map_inbit：返回映射(设定)手轮倍率输入信号的输入端口号；
        返回值：错误代码
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_handwheel_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_handwheel_ratio(uint CardNo, uint channel, ref uint channel_ratio_en, ref uint channel_map_inbit);
        //**********************************************************************************************************************
        /*
        功  能：设置软件手轮倍率；
        参  数：CardNo：控制卡卡号
                axis：0-3；
                mHwheelRatio：手轮倍率。
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_soft_handwheel_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_soft_handwheel_ratio(uint CardNo, uint axis, double mHwheelRatio);
        /*************************************************************************************************************************************
        /*
        功  能：读取软件手轮倍率；
        参  数：CardNo：控制卡卡号
                axis：0-3；
                mHwheelRatio：手轮倍率。
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_soft_handwheel_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_soft_handwheel_ratio(uint CardNo, uint axis, ref double mHwheelRatio);
        /**************************************************************************************************************************************
        /*
        功  能：修改手轮软件倍率；
        参  数：CardNo：控制卡卡号
                axis：0-8；
		        update_ratio_mode:修改手轮倍率模式，0:10倍；1:100倍；2:1000倍；

                mHwheelRatio：手轮倍率。
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_update_soft_handwheel_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_update_soft_handwheel_ratio(uint CardNo, uint axis, uint update_ratio_mode, double mHwheelRatio);
        //***********************************************************************************************************************************
        /*
        功  能：读取手轮软件倍率修改值；
        参  数：CardNo：控制卡卡号
                update_ratio_mode:修改手轮倍率模式，0:10倍；1:100倍；2:1000倍；
                axis：0-8；
                mHwheelRatio：返回手轮倍率修改值。
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_update_soft_handwheel_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_update_soft_handwheel_ratio(uint CardNo, uint axis, uint update_ratio_mode, ref double mHwheelRatio);
        /*************************************************************************************************************************************
        /*
        功  能：手轮设置；
        参  数：CardNo：控制卡卡号
                mHwheelEnable：使能手轮模式: 0不使能；1使能；
                mHwheelMode：手轮模式 0：控制轴选的轴按照手轮速度运行，1：运动命令按照手轮的速度运行（轴选不起作用，运动命令无法后退，只能前进；）
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_handwheel_control", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_handwheel_control(uint CardNo, uint mHwheelEnable, uint mHwheelMode);
        //**********************************************************************************************************************
        /*
        功  能：设置指定轴的外部急停输入使能以及外部急停输入的端口号
        参  数：CardNo 控制卡卡号；
        emg_stop_en：急停输入使能  0：不使能；1：使能；
        emg_stop_map_inbit：映射(设定)外部急停信号的输入端口号，取值范围0~127；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_emg_stop_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_emg_stop_config(uint CardNo, uint emg_stop_en, uint emg_stop_map_inbit);

        //*****************************************************************************************************************************************
        /*
        功   能：读取指定轴的外部急停输入使能以及外部急停输入的端口号
        参   数：CardNo 控制卡卡号；
                 emg_stop_en：返回急停输入使能  0：不使能；1：使能；
                 emg_stop_map_inbit：返回映射(设定)外部急停信号的输入端口号；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_emg_stop_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_emg_stop_config(uint CardNo, ref uint emg_stop_en, ref uint emg_stop_map_inbit);
        //****************************************************************************************************************
        /*
        功  能：设置输入16位计数器 计数器溢出后 65535 + 1 = 0   0-1 = 65535
        参  数： CardNo：控制卡卡号；
        pInCounter为设置输入计数器参数结构体（SetInputCounter_Req）指针，
        结构体成员介绍：
        CmdNum：行号；
        CmdName：函数内部设置命令；
        Channel：通道0~1，默认通道为0。
        CounterNumber：计数器号，范围0~7共8通道独立计数器；
        Enable：计数器时能 0不使能，1使能；
        Mode：计数器模式 （模式0~7）：
        0:单触0发上升沿 + 1；
        1:单触发0下降沿+1；
        2:单触发0上升沿-1；
        3:单触发0下降沿-1；
        4:双触发0上升沿 + 1,1上升-1；
        5:双触发0下降沿+1,1下降-1；
        6:双触发0上升+1 ,1下降-1；
        7:双触发0下降+1, 1上升-1；
        iInputSelect：单触发输入或双触发输出加选择设置，0~31分别选择输入0~31；
        dInputSelect：双触发输出减选择设置，0~31分别选择输入0~31（模式单触发时该设置不起作用，模式双触发时该选择不允许与上面选择一致）；
        data：计数器初始值；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_counter_input_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_counter_input_mode(uint CardNo, ref SetInputCounter_Req pInCounter);
        //*************************************************************************************************************
        /*
        功  能：读取IO计数值；
        参  数：CardNo :控制卡卡号；
        mCountNum：计数器号，范围0~7；
        mCountVal：返回指定的计数器的计数器值；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_counter_input_value", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_counter_input_value(uint CardNo, uint mCountNum, ref uint mCountVal);
        //******************************************************************************************************************************************
        /*
        功  能：停止所有轴的运动；
        参  数：CardNo:控制卡卡号;
        stop_mode：停止方式 0：减速停，1：立刻停;
        返回值：错误代码。
        备注：此指令可用于点位运动，也可用于连续轨迹运动中。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_stop_all_axis", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_stop_all_axis(uint CardNo, uint StopMode);
        //*******************************************************************************************************************************************
        /*
        功  能：停止指定轴的运动；
        参  数：CardNo 控制卡卡号;
        axis：轴号，0~7;
        stop_mode: 停止方式 0：减速停，1：立刻停;
        返回值：错误代码。
        备注：此指令可用于点位运动，也可用于连续轨迹运动中。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_stop_one_axis", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_stop_one_axis(uint CardNo, uint axis, uint StopMode);
        //********************************************************************************************************************************************
        /*
        功  能：暂停指定轴运动；
        参  数：CardNo：控制卡卡号；
        axis：轴号列表0~7；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_pause", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_pause(uint CardNo, uint axis);
        //*****************************************************************************************************************************************
        /*
        功  能：暂停轴的运动重新启动；
        参  数：CardNo：控制卡卡号；
        axis：轴号列表0~7；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_resume", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_resume(uint CardNo, uint axis);
        //***************************************************在线变速变位*************************************************************************************
        /*
        功  能：在线改变指定轴的速度命令；
        参  数：CardNo 控制卡卡号；
        axis：轴号列表 0~7；
        newVel：新的目标速度值, 单位是unit/ms；
        返回值：错误代码；
        备注：此指令只在点位模式下使用，不可用于连续轨迹中。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_change_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_change_speed(uint CardNo, uint axis, double newVel);
        //******************************************************************************************************************************************
        /*
        功 能： 在线改变轴速度倍率（只在连续轨迹下使用）;
        参 数： CardNo：控制卡卡号；
        axis：轴号0~7；
        SpeedRate：速度倍率值（建议0~1.2间调节）；
        返回值：错误代码。
        备注：连续轨迹下使用，速度倍率将保留；点位模式下只当前运行有效，不建议使用；
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_change_speed_rate", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_change_speed_rate(uint CardNo, uint axis, double speedRate);
        //******************************************************************************************************************************************
        /*
        功  能：在线改变位置命令;
        参  数：CardNo 控制卡卡号;
        axis：轴号列表 0~7；
        mPosType：位置类型：0相对，1：绝对；
        newPos：新的目标位置, 单位是unit；
        返回值：错误代码。
        备注：此指令只在点位模式下使用，不可用于连续轨迹中。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_change_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_change_position(uint CardNo, uint axis, uint mPosType, double newPos);
        [DllImport("MCN420.dll", EntryPoint = "YK_set_gear_ratio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_set_gear_ratio(uint CardNo, uint slave_axis, uint gear_enable, double speed_rate, double slope_distance);
        //*****************************************运动控制*************************************************************************************************
        /*
        功  能：单轴定长；
        参  数：CardNo 控制卡卡号；
        param 运行参数,类型是Single_Req_Param结构体。调用该函数前首先要申请一个Single_Req_Param结
        构体变量，然后给对应的结构体成员赋值，最后将该结构体变量作为参数
        结构体成员介绍：
        CmdNum：行号，在非连续轨迹中默认设置为0，在连续轨迹中设置为 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下；
        ParamRemain：参数保留；
        Channel：通道0 or 1；注意：点位运动下通道默认写0即可，连续轨迹需要按通道进行；
        axis：轴号 0~7；备注：在非缓冲区连续轨迹单轴独立运动中是0~7，在连续轨迹模式下，通道0中的0-3轴对应的是轴号是0~3；通道1中轴号是4~7；
        STFlag：速度类型 0：T型，1：S型；
        注意：连续轨迹下该参数不起作用，连续轨迹下速度类型只与前瞻参数中的速度类型相关；
        ParaType：参数类型参数类型 0:无速度参数， 1：有加速度参数；
        PosType：位置类型：0相对，1：绝对；
        AxisPos：位置, 单位是unit；
        InterpoVel：目标速度(参数类型为0无需填写),单位是unit/ms；
        InterpoStartVel：开始速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoEndVel：结束速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoAcc：加速度 (参数类型为0无需填写) 单位是unit/ms²；
        InterpoDec：减速度 (参数类型为0无需填写) 单位是unit/ms²；
        InterpJerkAcc：加加速度(参数类型为0无需填写) 单位是unit/ms³；
        InterpJerkDec：减减速度(参数类型为0无需填写) 单位是unit/ms³；
        返回值：错误代码。
        备注：此指令可用于点位运动，也可用于连续轨迹运动中。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_pmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_pmove(uint CardNo, ref Single_Req_Param pParam);
        //******************************************************************************************************************************************
        /*
        功  能：启动指定轴做Jog(连续)运动
        参  数：CardNo 控制卡卡号
        param 运行参数,类型是Jog_Req_Param结构体。调用该函数前首先要申请一个Jog_Req_Param结
        构体变量，然后给对应的结构体成员赋值，最后将该结构体变量作为参数
        结构体成员说明：
        CmdNum：行号，在非连续轨迹中默认设置为0，在连续轨迹中设置为 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下；
        ParamRemain：参数保留；
        Channel：通道0 or 1；
        axis：轴号 0~7；备注：通道默认写0即可；
        STFlag：速度类型 0：T型，1：S型；
        ParaType：参数类型  0：无速度参数,1：有加速度参数；
        Dir：方向 1：正方向，-1：负方向；
        InterpoVel：目标速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoStartVel: 开始速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoEndVel：结束速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoAcc：加速度（参数类型为0无需填写）单位是unit/ms²；
        InterpoDec：减速度（参数类型为0无需填写）单位是unit/ms²；
        InterpJerkAcc：加加速度（参数类型为0无需填写）单位是unit/ms³；
        InterpJerkDec：减减速度（参数类型为0无需填写）单位是unit/ms³；
        返回值：错误代码
        备注：此指令只在点位模式下使用，不可用于连续轨迹中。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_vmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_vmove(uint CardNo, ref Jog_Req_Param pParam);
        //******************************************************************************************************************************************
        /*
        功  能：启动指定轴做回零运动;
        参  数：CardNo 控制卡卡号；
        param 运行参数,类型是Home_Req_Param结构体。调用该函数前首先要申请一个Home_Req_Param结
        构体变量，然后给对应的结构体成员赋值，最后将该结构体变量作为参数
        结构体成员说明：
        CmdNum：行号，在非连续轨迹中默认设置为0，在连续轨迹中设置为 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下；
        ParamRemain：参数保留；
        Channel：通道 0 or 1；
        axis：轴号 0~7；
        LowSpeed：回零低速，单位unit/ms；
        HighSpeed：回零高速，正负代表回零方向;三开关和只有原点回零碰零点开关前是高速，其他全是低速，单位unit/ms；注意低速不能为0;
        double Acc：回零加速度,单位是“unit/ms²；
        HomeMode：回零模式,总共有8种回零模式 0~3 或 16~19，16种组合方式；(见第六章回零部分)；
        TDirTime：反向时间 ,单位毫秒(ms)；
        返回值：错误代码
        备注：MCN420卡的通用输入端口的电平默认为有效高电平，如果要单独设定，需要调用输入取反函数YK_set_input_bit_inverted 来设定。
        MCN420卡的指定轴的原点开关电平设置：根据配置的原点开关映射对应的输入信号编号(输入端口号)，使用YK_set_input_bit_inverted设置对应的输入信号有效电平，对应的位为0表示高电平有效，对应的为1表示低电平有效；
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_home_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_home_move(uint CardNo, ref Home_Req pParam);
        //******************************************************************************************************************************************
        /*
        功  能：直线插补命令；
        参  数：CardNo：控制卡卡号；
        param：运行参数,类型是Line_Req_Param结构体。调用该函数前首先要申请一个Line_Req_Param结
        构体变量，然后给对应的结构体成员赋值，最后将该结构体变量作为参数;
        结构体成员说明：
        CmdNum：行号，在非连续轨迹中默认设置为0，在连续轨迹中设置为 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下；
        ParamRemain：参数保留；
        Channel：通道 0 or 1；注意：点位运动下通道默认写0即可，连续轨迹需要按通道进行；
        axis[8]：轴号列表 0~7 ,在非连续轨迹中是0~7；使用连续轨迹时通道0对应的是0~3轴，通道1中的是4~7轴；
        STFlag：速度类型 0：T型，1：S型；
        注意：连续轨迹下该参数不起作用，连续轨迹下速度类型只与前瞻参数中的速度类型相关；
        ParaType：参数类型 0：无速度参数,1：有加速度参数；
        PosType：位置类型：0相对，1：绝对；
        AxisPos[8]：位置列表，单位是unit；
        nAxisNum：轴数量；
        InterpoVel：目标速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoStartVel：开始速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoEndVel：结束速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoAcc：加速度（参数类型为0无需填写）单位是unit/ms²；
        InterpoDec：减速度（参数类型为0无需填写）单位是unit/ms²；
        InterpJerkAcc：加加速度（参数类型为0无需填写）单位是unit/ms³；
        InterpJerkDec：减减速度（参数类型为0无需填写）单位是unit/ms³；
        返回值：错误代码；
        备注：此指令可用于点位运动，也可用于连续轨迹运动中。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_line_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_line_move(uint CardNo, ref Line_Req_Param pParam);
        //******************************************************************************************************************************************
        /*
        功  能：2维圆弧
        参  数：CardNo 控制卡卡号；
        param 运行参数,类型是Arc_2D_Req_Param结构体。调用该函数前首先要申请一个Arc_2D_Req_Param结
        构体变量，然后给对应的结构体成员赋值，最后将该结构体变量作为参数；
        结构体成员说明：
        CmdNum: 行号，在非连续轨迹中默认设置为0，在连续轨迹中设置为 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下；
        ParamRemain：参数保留；
        Channel：通道0 or 1；注意：点位运动下通道默认写0即可，连续轨迹需要按通道进行；
        axis[2]：轴号列表 0~7 ,在非连续轨迹中是0~7；使用连续轨迹时，通道0对应的是0~3轴，通道1中的是4~7轴；
        STFlag：速度类型 0：T型，1：S型；
        注意：连续轨迹下该参数不起作用，连续轨迹下速度类型只与前瞻参数中的速度类型相关；
        ParaType：参数类型0：无速度参数, 1：有加速度参数；
        PosType：位置类型：0相对，1：绝对；
        AxisPos[2]：位置列表，单位是unit；
        Radius：弧度 逆时针为正，顺时针为负；
        InterpoVel：目标速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoStartVel：开始速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoEndVel：结束速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoAcc：加速度（参数类型为0无需填写）单位是unit/ms²；
        InterpoDec：减速度（参数类型为0无需填写）单位是unit/ms²；
        InterpJerkAcc：加加速度（参数类型为0无需填写）单位是unit/ms³；
        InterpJerkDec：减减速度（参数类型为0无需填写）单位是unit/ms³；
        返回值：错误代码。
        备注：此指令可用于点位运动，也可用于连续轨迹运动中。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_arc_2d_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_arc_2d_move(uint CardNo, ref ARC_2D_Req_Param pParam);
        //******************************************************************************************************************************************
        /*
        功  能：2维圆弧 3点模式
        参  数：CardNo 控制卡卡号；
        param 运行参数,类型是ARC_3PPOINT_2D_MOVE_PARAM结构体。调用该函数前首先要申请一个ARC_3PPOINT_2D_MOVE_PARAM结
        构体变量，然后给对应的结构体成员赋值，最后将该结构体变量作为参数；
        结构体成员说明：
        CmdNum: 行号，在非连续轨迹中默认设置为0，在连续轨迹中设置为 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下；
        ParamRemain：参数保留；
        Channel：通道0 or 1；注意：点位运动下通道默认写0即可，连续轨迹需要按通道进行；
        axis[2]：轴号列表 0~7 ,在非连续轨迹中是0~7；使用连续轨迹时，通道0对应的是0~3轴，通道1中的是4~7轴；
        STFlag：速度类型 0：T型，1：S型；
        注意：连续轨迹下该参数不起作用，连续轨迹下速度类型只与前瞻参数中的速度类型相关；
        ParaType：参数类型0：无速度参数, 1：有加速度参数；
        PosType：位置类型：0相对，1：绝对；
        start_pos[2]：开始点位置列表，单位是unit；
        mid_pos[2]:中间点位置列表，单位是unit；
        aim_pos[2]:终点位置列表，单位是unit；
        InterpoVel：目标速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoStartVel：开始速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoEndVel：结束速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoAcc：加速度（参数类型为0无需填写）单位是unit/ms²；
        InterpoDec：减速度（参数类型为0无需填写）单位是unit/ms²；
        InterpJerkAcc：加加速度（参数类型为0无需填写）单位是unit/ms³；
        InterpJerkDec：减减速度（参数类型为0无需填写）单位是unit/ms³；
        返回值：错误代码。
        备注：此指令可用于点位运动，也可用于连续轨迹运动中。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_arc_3point_2d_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_arc_3point_2d_move(uint CardNo, ref Arc_2D_3Point_Req_Param pParam);
        //******************************************************************************************************************************************
        /*
        功  能：3维圆弧（三点定圆弧）,不能使用在连续轨迹模式下；
        参  数：CardNo 控制卡卡号；
        param 运行参数,类型是Arc_3D_Req_Param 结构体。调用该函数前首先要申请一个Arc_3D_Req_Param结
        构体变量，然后给对应的结构体成员赋值，最后将该结构体变量作为参数
        结构体成员说明：
        CmdNum:行号，默认设置为 0；
        ParamRemain：参数保留；
        Channel: 通道  0 or 1；注意：点位运动下通道默认写0即可；
        axis[3]：轴号列表 0~7 (可映射为X,Y,Z的的坐标系轴号)；
        STFlag：速度类型 0：T型，1：S型；
        ParaType：参数类型0：无速度参数,  1：有加速度参数；
        PosType：位置类型：0相对，1：绝对；
        AxisPos[6]：位置列表（分别是圆弧上的点X,Y,Z的坐标和圆弧终点X,Y,Z的坐标），单位是unit；注：开始点坐标为当前点坐标；
        InterpoVel：目标速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoStartVel：开始速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoEndVel：结束速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoAcc：加速度（参数类型为0无需填写）单位是unit/ms²；
        InterpoDec：减速度（参数类型为0无需填写）单位是unit/ms²；
        InterpJerkAcc：加加速度（参数类型为0无需填写）单位是unit/ms³；
        InterpJerkDec：减减速度（参数类型为0无需填写）单位是unit/ms³；
        返回值：错误代码。
        备注：此指令不能使用在连续轨迹模式下。
        */

        [DllImport("MCN420.dll", EntryPoint = "YK_arc_3d_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_arc_3d_move(uint CardNo, ref CMD_3DArc_Req_Param pParam);
        //*******************************************************************************************************************************************
        /*
        功  能：2维渐变螺旋插补命令
        参  数：CardNo 控制卡卡号
        param 运行参数,类型是Helix_2D_Req_Param结构体。调用该函数前首先要申请一个Helix_2D_Req_Param结
        构体变量，然后给对应的结构体成员赋值，最后将该结构体变量作为参数
        结构体成员说明：
        CmdNum：行号，在非连续轨迹中默认设置为0，在连续轨迹中设置为 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下;
        ParamRemain：参数保留；
        Channel：通道 0 or 1；	注意：点位运动下通道默认写0即可，连续轨迹需要按通道进行；
        axis[2]：轴号列表 0~7 ,在非连续轨迹中是0~7；使用连续轨迹时,其中通道0对应的是0~3轴，通道1是4~7轴；
        STFlag：速度类型 0：T型，1：S型；
        注意：连续轨迹下该参数不起作用，连续轨迹下速度类型只与前瞻参数中的速度类型相关；
        ParaType：参数类型0：无速度参数,  1：有加速度参数；
        PosType：位置类型：0相对，1：绝对；
        AxisPos[2]：位置列表，单位是unit；
        Radius：弧度 逆时针为负，顺时针为正；
        EndRadius：目标半径, 单位是unit；
        InterpoVel：目标速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoStartVel：开始速度(参数类型为0无需填写) 单位是unit/ms
        InterpoEndVel：结束速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoAcc：加速度（参数类型为0无需填写）单位是unit/ms²；
        InterpoDec：减速度（参数类型为0无需填写）单位是unit/ms²；
        InterpJerkAcc：加加速度（参数类型为0无需填写）单位是unit/ms³；
        InterpJerkDec：减减速度（参数类型为0无需填写）单位是unit/ms³；
        返回值：错误代码。
        备注：此指令可用于点位运动，也可用于连续轨迹运动中。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_helix_2d_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_helix_2d_move(uint CardNo, ref Helix_2D_Req_Param pParam);
        /*******************************************************************************************************************************************
        /*
        功  能：3维螺旋插补命令；
        参  数：CardNo 控制卡卡号；
        param 运行参数,类型是Helix_3D_Req_Param结构体。调用该函数前首先要申请一个Helix_3D_Req_Param结
        构体变量，然后给对应的结构体成员赋值，最后将该结构体变量作为参数
        结构体成员说明：
        CmdNum：行号，在非连续轨迹中默认设置为0，在连续轨迹中设置为 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下;
        ParamRemain： 参数保留；
        Channel：通道 0 or 1；	注意：点位运动下通道默认写0即可，连续轨迹需要按通道进行；
        axis[3]：轴号列表 0~7 ,在非连续轨迹中是0~7；使用连续轨迹时，其中通道0对应的是0~3轴，通道1是4~7轴；最后一个为直线轴；
        STFlag：速度类型 0：T型，1：S型；
        注意：连续轨迹下该参数不起作用，连续轨迹下速度类型只与前瞻参数中的速度类型相关；
        ParaType：参数类型 0：无速度参数,  1：有加速度参数；
        PosType：位置类型：0相对，1：绝对；
        AxisPos[3]：位置列表 ，单位是unit；
        Radius：弧度 逆时针为正，顺时针为负；
        InterpoVel：目标速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoStartVel：开始速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoEndVel：结束速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoAcc：加速度（参数类型为0无需填写）单位是unit/ms²；
        InterpoDec：减速度（参数类型为0无需填写）单位是unit/ms²；
        InterpJerkAcc：加加速度（参数类型为0无需填写）单位是unit/ms³；
        InterpJerkDec：减速度（参数类型为0无需填写）单位是unit/ms³；
        返回值：错误代码。
        备注：此指令可用于点位运动，也可用于连续轨迹运动中。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_helix_3d_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_helix_3d_move(uint CardNo, ref Helix_3D_Req_Param pParam);
        //*******************************************************************************************************************************************
        /*
        功  能：3维圆锥插补命令；
        参  数：CardNo 控制卡卡号；
        param 运行参数,类型是Cone_3D_Req_Param结构体。调用该函数前首先要申请一个Cone_3D_Req_Param结
        构体变量，然后给对应的结构体成员赋值，最后将该结构体变量作为参数
        结构体成员说明：
        CmdNum：行号，在非连续轨迹中默认设置为0，在连续轨迹中设置为 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下;
        ParamRemain：参数保留；
        Channel：通道 0 or 1；	注意：点位运动下通道默认写0即可，连续轨迹需要按通道进行；
        axis[3]：轴号列表 0~7 ,在非连续轨迹中是0~7；使用连续轨迹时，其中通道0对应的是0~3轴，通道1是4~7轴；最后一个为直线轴；
        STFlag：速度类型 0：T型，1：S型；
        注意：连续轨迹下该参数不起作用，连续轨迹下速度类型只与前瞻参数中的速度类型相关；
        ParaType：参数类型 0：无速度参数,  1：有加速度参数；
        PosType：位置类型：0相对，1：绝对；
        AxisPos[3]：位置列表，单位是unit；
        Radius：单位弧度, 逆时针为正，顺时针为负；
        EndRadius：目标半径, 单位是unit；
        InterpoVel：目标速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoStartVel：开始速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoEndVel：结束速度(参数类型为0无需填写) 单位是unit/ms；
        InterpoAcc：加速度（参数类型为0无需填写）单位是unit/ms²；
        InterpoDec：减速度（参数类型为0无需填写）单位是unit/ms²；
        InterpJerkAcc：加加速度（参数类型为0无需填写）单位是unit/ms³；
        InterpJerkDec：减速度（参数类型为0无需填写）单位是unit/ms³；
        返回值：错误代码。
        备注：此指令可用于点位运动，也可用于连续轨迹运动中。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_cone_3d_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_cone_3d_move(uint CardNo, ref Cone_3D_Req_Param pParam);
        //******************************************************************************************************************************************
        /*
            功  能：前瞻参数设置；
            参  数：CardNo 控制卡卡号；
            param 前瞻参数结构体（Set_Ahead_Param）指针,结构体（Set_Ahead_Param）定义参照函数头文中的相关定义：
            结构体成员说明：
            Channel：通道 0 or 1；
            InterpoStartVel：最低速度，单位是unit/ms；
            InterpoVel：目标速度, 单位是unit/ms；
            InterpoAcc：加速度, 单位是 unit/ms²；
            InterpJerkAcc：加加速度，单位是unit/ms³；
            InterpoDec:减速度 unit/ms²
            InterpJerkDec：减减速度 unit/ms³ ；
            MaxAngVel：最大角速度；
            STFlag：S型或T型的标志；
            返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_ahead", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_ahead(uint CardNo, ref Set_Ahead_Param pParam);
        //******************************************************************************************************************************************
        /*
        功  能：打开前瞻；
        参  数：CardNo：控制卡卡号；
        mChannel：通道 0 or 1；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_open_lookahead", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_open_lookahead(uint CardNo, uint mChannel);
        //******************************************************************************************************************************************
        /*
        功  能：关闭前瞻；
        参  数：CardNo 控制卡卡号；
        mChannel：通道0or1；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_close_lookahead", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_close_lookahead(uint CardNo, uint mChannel);
        //**********************************************运动状态以及位置速度读取***************************************************************************************
        /*
        功  能：读取指定轴的运行状态。
        参  数：CardNo:控制卡卡号
        axis:轴号0~7；
        返回值：返回值为0表示运动状态，返回值为1表示静止状态。
        注意：该函数使用需要在下发运动指令（如直线等，连续轨迹开始等）后延时3毫秒后再检查，防止运动未开始就误以为运动完成。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_check_done", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_check_done(uint CardNo, uint axis);
        //******************************************************************************************************************************************
        /*
        功  能：以unit为单位，设置指定轴的指令位置；
        参  数：CardNo 控制卡卡号；
        axis:轴号，范围0~7；
        mPos：设置位置值，单位是unit；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_command_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_command_position(uint CardNo, uint axis, double mPos);
        //******************************************************************************************************************************************
        /*
        功  能：以unit为单位，获取指定轴的指令位置；
        参  数：CardNo 控制卡卡号；
        axis：轴号0~7；
        返回值：返回指定轴的当前指令位置，单位是unit。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_command_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_get_command_position(uint CardNo, uint axis);
        //*******************************************************************************************************************************************
        /*
        功  能：以pluse为单位，读取指定轴的规划位置；
        参  数：CardNo：控制卡卡号；
        axis：轴号0~7；
        返回值：返回指定轴的规划位置，单位是pluse(脉冲)。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_current_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_get_current_position(uint CardNo, uint axis);
        //******************************************************************************************************************************************
        /*
        功  能：以pulse(脉冲)为单位设置编码器位置。;
        参  数：CardNo：控制卡卡号；
        axis：轴号，范围0~3；
        mEncoderPos：编码器数值, 单位是pulse；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_encoder_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_encoder_position(uint CardNo, uint axis, int mEncoderPos);
        //******************************************************************************************************************************************
        /*
        功  能：以pulse(脉冲)为单位读取编码器位置;
        参  数：CardNo 控制卡卡号;
        axis：轴号0~3;
        返回值：编码器的计数值 ，单位是pulse。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_encoder_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_encoder_position(uint CardNo, uint axis);
        //******************************************************************************************************************************************
        /*
        功  能：以unit 单位方式设定编码器位置；
        参  数：CardNo ：控制卡卡号；
        axis：轴号0~7；
        mRelPos：设置的实际位置，单位是unit；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_real_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_real_position(uint CardNo, uint axis, double mRelPos);
        //******************************************************************************************************************************************
        /*
        功  能：以unit 单位方式读取编码器位置；
        参  数：CardNo ：控制卡卡号；
        axis：轴号0~7;
        返回值：返回指定轴的实际位置，单位是unit。
        */

        [DllImport("MCN420.dll", EntryPoint = "YK_get_real_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_get_real_position(uint CardNo, uint axis);
        //*******************************************************************************************************************************************
        /*
        功  能：读取指定轴的当前速度；
        参  数：CardNo：控制卡卡号；
        axis：轴号0~7 ；
        返回值：返回指定轴的当前速度，单位是unit/ms。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_current_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_get_current_speed(uint CardNo, uint axis);
        //*******************************************************************************************************************************************
        /*
        功 能： 以unit为单位读取指定轴的规划位置;
        参 数： CardNo： 控制卡卡号;
        interpAxis：轴号0~7;
        返回值：返回指定轴的规划位置，单位是unit。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_interp_cmd_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_get_interp_cmd_position(uint CardNo, uint interpAxis);
        //*****************************************************通用输入输出控制*************************************************************************************
        /*
        功  能：设置指定控制卡的某个输出端口的电平(按位设置)(用于非缓冲区)
        参  数：CardNo：控制卡卡号；
        outBitNo：输出端口OUT号 0~127,按位输出；
        on_off：输出电平，0表示低电平，1表示高电平；
        返回值：错误代码
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_write_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_write_outbit(uint CardNo, uint outBitNo, uint on_off);
        /*
        功  能：设置指定控制卡的某个输出端口的电平(按位设置)(用于缓冲区)
        参  数：CardNo：控制卡卡号；
        channel:通道号，值为0和1;
        outBitNo：输出端口OUT号 0~127,按位输出；
        on_off：输出电平，0表示低电平，1表示高电平；
        返回值：错误代码
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_write_outbit_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_write_outbit_ex(uint CardNo, uint channel, uint outBitNo, uint on_off);
        //******************************************************************************************************************************************
        /*
        功  能：设置指定控制卡的某个输出端口的电平延时输出(按位设置)(可以用于缓冲区，也可以用于非缓冲区)
        参  数：CardNo：控制卡卡号；
        channel:通道号，值为0和1;
        outBitNo：输出端口OUT号 0~127,按位输出；
        on_off：输出电平，0表示低电平，1表示高电平；
        delaytime:延时时间，单位毫秒（ms）
        返回值：错误代码
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_write_outbit_delay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_write_outbit_delay(uint CardNo, uint channel, uint outBitNo, uint on_off, uint delaytime);
        //******************************************************************************************************************************************
        /*
        功  能：读取指定控制卡的某个输出端口的电平(按位读取)(用于非缓冲区)
        参  数：CardNo 控制卡卡号；
        outbitNo：输出端口位号0~127；
        返回值：返回指定端口的输出电平，0表示低电平，1表示高电平。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_read_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_read_outbit(uint CardNo, uint outbitNo);
        //******************************************************************************************************************************************
        /*
        功  能：读取指定控制卡的某个输入端口的电平（按字节读取)，(用于非缓冲区)；
        参  数：CardNo 控制卡卡号；
        outByteNum：输出IO端口字节编号：0-31；
        返回值：返回一个字节(8位)的输出电平(低8位有效)，字节中每位0表示低电平，1表示高电平。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_read_outbyte", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_read_outbyte(uint CardNo, uint outByteNum);
        //*******************************************************************************************************************************
        /*
        功  能：设置指定控制卡的输出端口的电平(按字节设置)，(用于非缓冲区)
        参  数：CardNo：控制卡卡号；
        outByteNum：输出IO端口字节编号：0-31；
        on_off：字节(8位)的输出电平，字节中每位0表示低电平，1表示高电平；
        返回值：错误代码.
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_write_outbyte", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_write_outbyte(uint CardNo, uint outByteNum, byte on_off);
        //******************************************************************************************************************************************
        /*
        功  能：读取指定控制卡的某个输入端口的电平(按位读取)，(用于非缓冲区)
        参  数：CardNo 控制卡卡号;
        inbitNo：输入端口位号0~127;
        返回值：指定的输入端口电平：0：低电平，1：高电平。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_read_inbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_read_inbit(uint CardNo, uint inbitNo);
        //*******************************************************************************************************************************************
        /*
        功  能：读取指定控制卡的输入端口的电平（按字节读取)；；
        参  数：CardNo 控制卡卡号；
        nByteNum：输入端口字节编号：0-31；
        返回值：返回一个字节(8位)的输入电平(低8位有效)，字节中每位0表示低电平，1表示高电平。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_read_inbyte", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_read_inbyte(uint CardNo, uint inByteNum);
        //*********************************************************************************************************************************
        /*
        功  能：设置输出延时翻转功能；
        参  数：CardNo：控制卡卡号；
        pOutDelayFlip：输出延时翻转指令参数的结构体(SetOutputDelayFlip_Req)变量指针
        结构体成员说明：
        CmdNum：行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下；
        CmdName：参数保留；
        Channel：通道0~1；
        Mode：触发模式，0:写入触发；1：IO输入上升沿触发；2：IO输入下降沿触发；3：位置触发；
        Enable：输出延时翻转 0不使能，1使能；
        iSelect:IO输入触发时0~31选择输入;位置触发时:0~7选择0~7轴逻辑位置触发,8~11选择0~3轴编码器位置触发；
        Pos：位置触发模式 触发位置；
        Times：翻转除数0为一直翻转，1~65535为翻转次数；
        OutputSelect：输出选择0~15；
        msDelay：延时时间值ms；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_output_delay_flip", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_output_delay_flip(uint CardNo, ref SetOutputDelayFlip_Req pOutDelayFlip);
        //*********************************************************************************************************************************
        /*
        功  能：设置输出延时翻转功能； 
        参  数：CardNo：控制卡卡号；
                pOutDelayFlip：输出延时翻转指令参数的结构体(SetOutputDelayFlip_Req)变量指针
                结构体成员说明：
	            CmdNum：行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下；
	            CmdName：参数保留；
	            Channel：通道0~1；
	            Mode：触发模式，0:写入触发；1：IO输入上升沿触发；2：IO输入下降沿触发；3：位置触发；
	            Enable：输出延时翻转 0不使能，1使能；
                iSelect:IO输入触发时0~31选择输入;位置触发时:0~7选择0~7轴逻辑位置触发,8~11选择0~3轴编码器位置触发；
	            Pos：位置触发模式 触发位置；
                Times：翻转除数0为一直翻转，1~65535为翻转次数；
	            OutputSelect：输出选择0~15；
	            msLowLogicDelay;                            //低电平延时时间ms；
	            msHighLogicDelay;                           //高电平延时时间ms； 
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_output_delay_flip_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_output_delay_flip_ex(uint CardNo, ref SetOutputDelayFlip_ReqLogic pOutDelayFlip);

        //*********************************************************************************************************************************
        /*
        功  能：通用输出超前或滞后输出(段内执行)；
        参  数：CardNo：控制卡卡号；
        LineNum:行号；
        channel：通道号0-1；
        bitno：输出端口位号0-127；
        on_off：输出端口电平值，0：低电平，1：高电平
        outputmode：超前或滞后控制模式，0：滞后；1:超前；
        launchmode：启动模式 0：相对于轨迹段起点 1：相对于轨迹段终点；
        para_mode:滞后或超前类型 0:时间；1:位置 ；
        para_value：滞后或超前值，单位：ms（滞后时间模式）或  unit（滞后距离模式）；
        revertime：输出端口电平输出后的翻转时间，单位：s ，当此参数为0表示不翻转。
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_output_delay_ahead_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_output_delay_ahead_pos(uint CardNo, uint LineNum, uint channel, uint bitno, uint on_off, uint outputmode, uint launchmode, uint para_mode, double para_value, double revertime);
        //*****************************************************专用输入限号控制*************************************************************************************
        /*
        功  能：读取指定轴的运行状态报警信息
        参  数：CardNo ：控制卡卡号；
        axis：轴号  0~7 ；
        返回值：返回指定轴的运行状态信息，0表示正常；1是暂停；2是急停；3是减速停止；4是正硬件限位停止；5是负硬件限位停止；6正软件限位停止；7是负软件限位停止；8是驱动器报警。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_axis_motion_status_alm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_axis_motion_status_alm(uint CardNo, uint axis);
        //******************************************************************************************************************************************
        /*
        功  能：复位轴异常报警命令；
        参  数：CardNo 控制卡卡号；
        axis: 轴号列表 0~7；
        返回值：错误代码。

        备注：报警（Alarm）信号电平设置：
        MCN420卡的通用输入端口的电平默认为有效高电平，如果要单独设定，需要调用输入取反函数YK_set_input_bit_inverted 来设定。
        MCN420卡的指定轴的报警（Alarm）信号设置：根据配置的限位开关映射对应的输入信号编号(输入端口号)，使用YK_set_input_bit_inverted设置对应的输入信号有效电平，对应的位为0表示高电平有效，对应的为1表示低电平有效；
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_remove_axis_alm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_remove_axis_alm(uint CardNo, uint axis);
        //*******************************************************************************************************************************************
        /*
        功  能：读取指定轴的输入报警状态；
        参  数：CardNo：控制卡卡号；
        axis：轴号0~7 ；
        返回值：返回输入的ALM信号状态，0：正常，1：报警。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_axis_alm_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_axis_alm_status(uint CardNo, uint axis);
        //*******************************************************************************************************************************************
        /*
        功  能：读取指定轴的正硬件限位输入状态；
        参  数：CardNo：控制卡卡号；
        axis：轴号0~7；
        返回值：返回输入的正硬件限位信号状态，0表示无效；1表示有效。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_axis_positive_limit_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_axis_positive_limit_status(uint CardNo, uint axis);
        //******************************************************************************************************************************************
        /*
        功  能：读取指定轴的负硬件限位输入状态；
        参  数：CardNo：控制卡卡号；
        axis：轴号0~7；
        返回值：返回输入的负硬件限位信号状态，0表示无效；1表示有效。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_axis_negative_limit_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_axis_negative_limit_status(uint CardNo, uint axis);
        //******************************************************************************************************************************************
        /*
        功  能：读取指定轴的原点输入状态；
        参  数：CardNo：控制卡卡号；
        axis：轴号0~7；
        返回值：返回输入的原点ORG的信号状态，0表示无效；1表示为有效。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_axis_home_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_axis_home_status(uint CardNo, uint axis);
        //**************************************************************************************************************************************
        /*
        功  能：读取指定通道的运行行号；
        参  数：CardNo：控制卡卡号；
        mChannel：通道号 0~1；
        返回值：返回指定通道的运行行号。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_channel_cmd_line_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_channel_cmd_line_num(uint CardNo, uint mChannel);
        //*************************************************位置锁存信号控制******************************************************************************************
        /*
        功  能：设置指定轴的位置锁存信号方式；
        参  数：CardNo: 控制卡卡号；
        axis:轴号，范围0~7；
        mLockEnable:锁存使能  0-不使能，1-使能；
        mLockInputBit:锁存输入位 0-127；
        mLockMode:锁存模式（8种模式）：
        //----------单次锁存---------------------------------------------
        0:单次锁存上升沿输入锁存逻辑位置；
        1:单次锁存下降沿输入锁存逻辑位置 ；
        2:单次锁存编码器Z脉冲上升沿锁存逻辑位置；
        3:单次锁存编码器 Z脉冲下降沿锁存逻辑位置；
        4:单次锁存上升沿输入锁存编码器位置；
        5:单次锁存下降沿输入锁存编码器位置 ；
        6:单次锁存编码器Z脉冲上升沿锁存编码器位置；
        7:单次锁存编码器Z脉冲下降沿锁存编码器位置；
        //---------多次锁存---------------------------------
        8:多次锁存上升沿输入锁存逻辑位置；
        9:多次锁存下降沿输入锁存逻辑位置 ；
        10:多次锁存编码器Z脉冲上升沿锁存逻辑位置；
        11:多次锁存编码器 Z脉冲下降沿锁存逻辑位置；
        12:多次锁存上升沿输入锁存编码器位置；
        13:多次锁存下降沿输入锁存编码器位置 ；
        14:多次锁存编码器Z脉冲上升沿锁存编码器位置；
        15:多次锁存编码器Z脉冲下降沿锁存编码器位置；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_latch_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_latch_position(uint CardNo, uint axis, uint mLockEnable, uint mLockInputBit, uint mLockMode);
        //******************************************************************************************************************************************
        /*
        功  能：读取指定轴锁存位置；
        参  数：CardNo：控制卡卡号；
        axis：轴号0~7；
        返回值：返回指定轴的锁存位置，单位是unit。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_latch_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_get_latch_position(uint CardNo, uint axis);
        //*****************************************************************************************************************************************
        /*
        功  能：读取指定轴锁存状态;
        参  数：CardNo 控制卡卡号;
        axis：轴号0~7 ;
        返回值：返回指定轴的锁存状态，0：未锁存，1：已经锁存。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_latch_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_latch_status(uint CardNo, uint axis);
        //******************************************************************************************************************************************
        /*
        功  能：延时运行指令
        参  数：CardNo：控制卡卡号；
        结构体成员介绍：
        CmdNum：行号0~n,用于连续轨迹中；
        SetParam_CMD : 参数保留；
        Channel：通道0~1；
        Times：延时时间值ms；
        返回值：错误代码；
        备注：此指令只适用与连续轨迹中；
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_delay_time", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_delay_time(uint CardNo, ref Delay_Time_Req pTimeDeley);
        //******************************************************一维高速位置比较************************************************************************************
        /*
        功  能：设置一维高速位置比较模式；
        参  数：CardNo:控制卡卡号；
        mHcmp：高速比较器(对应通道0~3),取值范围0~3；
        mMode：高速比较模式0：不使能；1：等于；2：大于；3：小于；4：队列中的等于；5：线性等于；6：队列中的等于+输出电平；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_1d_set_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_1d_set_mode(uint CardNo, uint mHcmp, uint mMode);
        //******************************************************************************************************************************************
        /*
        功  能：读取指定一维位置比较的模式；
        参  数：CardNo：控制卡卡号；
        mHcmp：高速比较器(对应通道0~3),取值范围0~3；
        cmpMode：返回比较模式设置；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_1d_get_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_1d_get_mode(uint CardNo, uint cmp, ref uint cmp_mode);
        //*****************************************************************************************************************************************
        /*
        功  能：设置一维高速比较参数；
        参  数：CardNo：控制卡卡号；
        mHcmp：高速比较器(对应通道0~3),取值范围0~3；
        axis：关联轴号0~7；
        cmpSource：比较位置源： 0：指令位置；1：编码器位置；
        cmpLogic：有效电平：0：低电平，1：高电平；
        time：比较输出宽度(单位us) ，取值范围为1~100000us；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_1d_set_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_1d_set_config(uint CardNo, uint mHcmp, uint axis, uint cmpSource, uint cmpLogic, uint time);
        //*****************************************************************************************************************************************
        /*
        功  能：读取一维位置比较参数;
        参  数：CardNo 控制卡卡号；
        mHcmp：高速比较器(对应通道0~3),取值范围0~3；
        axis:返回关联轴号设置；
        cmpSource:返回比较位置源设置；
        cmpLogic:返回有效电平设置；
        time:返回脉冲宽度设置,单位us；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_1d_get_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_1d_get_config(uint CardNo, uint mHcmp, ref uint axis, ref uint cmpSource, ref uint cmpLogic, ref uint time);
        //****************************************************************************************************************************************
        /*
        功  能：添加或更新一维高速比较位置;
        参  数：CardNo:控制卡卡号;
        mHcmp：高速比较器(对应通道0~3),取值范围0~3；
        cmpPos：比较位置，单位是pulse；
        备注：位置为脉冲计数器的值或编码器计数器的值，并非逻辑位置或实际位置，只有设置脉冲比率和编码器比率为1时两者才是相等的！！！
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_1d_add_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_1d_add_point(uint CardNo, uint mHcmp, Int32 cmpPos);
        //******************************************************************************************************************************************
        /*
        功  能：添加或更新一维高速比较位置和输出电平;
        参  数：CardNo:控制卡卡号;
        mHcmp：高速比较器(对应通道0~3),取值范围0~3；
        cmpPos：比较位置，单位是pulse；
        output_logic: 比较输出电平， 0：低电平，1：高电平；
        备注：位置为脉冲计数器的值或编码器计数器的值，并非逻辑位置或实际位置，只有设置脉冲比率和编码器比率为1时两者才是相等的！！！
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_1d_add_point_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_1d_add_point_ex(uint CardNo, uint mHcmp, Int32 cmpPos, uint output_logic);
        //************************************************************************************************************************************************************
        /*
        功  能：清除一维高速比较位置；
        参  数：CardNo：控制卡卡号；
        mHcmp：高速比较器(对应通道0~3),取值范围0~3；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_1d_clear_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_1d_clear_point(uint CardNo, uint mHcmp);
        //******************************************************************************************************************************************
        /*
        功  能：设置一维高速比较线性比较参数；
        参  数：CardNo：控制卡卡号；
        mHcmp：高速比较器(对应通道0~3),取值范围0~3；
        mIncrement：线性位置增量值，单位：pulse（正值表示位置递增，负值表示位置递减）；
        mTimes：比较次数 1~65535；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_1d_set_liner", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_1d_set_liner(uint CardNo, uint mHcmp, Int32 mIncrement, uint mTimes);
        //******************************************************************************************************************************************
        /*
        功  能：读取一维高速位置比较线性比较参数；
        参  数：CardNo 控制卡卡号；
        mHcmp：高速比较器(对应通道0~3),取值范围0~3；
        mIncrement:返回位置增量值设置, ，单位是pulse；
        mTimes:返回比较次数设置；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_1d_get_liner", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_1d_get_liner(uint CardNo, uint mHcmp, ref Int32 mIncrement, ref uint mTimes);
        //******************************************************************************************************************************************
        /*
        功  能：读取一维位置比较当前状态；
        参  数：CardNo：控制卡卡号；
        mHcmp：高速比较器(对应通道0~3),取值范围0~3；
        remainedPoints：回位置增量值设置；
        currentPoint：返回当前比较点位置，单位：pulse；
        runDonePoints：返回已运行完的比较点数；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_1d_get_current_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_1d_get_current_state(uint CardNo, uint mHcmp, ref uint remainedPoints, ref uint currentPoint, ref uint runDonePoints);
        //*************************************************1维低速位置比较*****************************************************************************************
        /*
        功  能：设置一维低速位置比较器
        参  数：CardNo：控制卡卡号；
        axis：轴号 0~7；
        mEnable：比较功能状态 0：不使能，1：使能
        cmpSource：位置比较源：0：指令位置；1：编码器位置；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_cmp_1d_set_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_cmp_1d_set_config(uint CardNo, uint axis, uint mEnable, uint cmpSource);
        //****************************************************************************************************************************************
        /*
        功  能：读取一维低速位置比较器配置；
        参  数：CardNo：控制卡卡号；
        axis：轴号0~7；
        mEnable:返回位置比较功能状态；
        mSource:返回位置比较源选择；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_cmp_1d_get_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_cmp_1d_get_config(uint CardNo, uint axis, ref uint mEnable, ref uint mSource);
        //******************************************************************************************************************************************
        /*
        功  能：清除一维低速位置比较位置；
        参  数：CardNo：控制卡卡号；
        axis：轴号0~7；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_cmp_1d_clear_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_cmp_1d_clear_point(uint CardNo, uint axis);

        //******************************************************************************************************************************************
        /*
        功  能：增加或更新一维低速位置比较位置；
        参  数：CardNo:控制卡卡号；
	            axis：轴号 0~7；
	            mPos：比较位置；
                      备注：位置为脉冲计数器的值或编码器计数器的值，并非逻辑位置或实际位置，只有设置脉冲比率和编码器比率为1时两者才是相等的！！！
	            mDir：比较方向 0：小于等于；1：大于等于；
	            mMode：比较模式； 功能定义见表7.1所示；
	            mCmpOutSet：比较结果输出选择；功能定义见《MCN420用户使用手册》第七章节中表7.1所示；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_cmp_1d_add_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_cmp_1d_add_point(uint CardNo, uint axis, int mPos, int mDir, uint mMode, uint mCmpOutSet);
        //*****************************************************************************************************************************************
        /*
		功  能：读取当前一维比较点位置；
		参  数：ardNo:控制卡卡号；
		       axis:指定轴号,取值范围:0-7；
		       mPos:返回当前比较点位置,单位:pulse；
		返回值：错误代码。
	    */
        [DllImport("MCN420.dll", EntryPoint = "YK_cmp_1d_get_current_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_cmp_1d_get_current_point(uint CardNo, uint axis, ref Int32 mPos);
        //******************************************************************************************************************************************
        /*
        功  能：查询已经比较过的一维比较点个数；
        参  数：CardNo：控制卡卡号；
        axis：指定轴号,取值范围：0-7；
        mPointNum：返回已经比较过的点数；
        返回值:错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_cmp_1d_get_points_runned", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_cmp_1d_get_points_runned(uint CardNo, uint axis, ref UInt32 mPointNum);

        //******************************************************************************************************************************************
        /*
        功  能：查询可以加入的一维比较点个数；
        参  数：CardNo：控制卡卡号；
        axis：指定轴号,取值范围: 0-7；
        mPointNum：返回可以加入的比较点数；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_cmp_1d_get_points_remained", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_cmp_1d_get_points_remained(uint CardNo, uint axis, ref uint mPointNum);

        //***********************************************低速二维比较*******************************************************************************************
        /*
        功  能：设置二维低速位置比较使能和比较源；
        参  数：CardNo：控制卡卡号；
        mEnable：二维位置比较使能，0：不使能；1：使能；
        cmpSource：二维位置比较源， 0：指令位置；1：编码器位置；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_cmp_2d_set_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_cmp_2d_set_config(uint CardNo, uint mEnable, uint cmpSource);

        //******************************************************************************************************************************************
        /*
        功  能：读取二维位置比较使能和比较源；
        参  数：CardNo： 控制卡卡号；
        mEnable：返回位置比较时能；
        mCmpSource：返回位置比较源选择；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_cmp_2d_get_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_cmp_2d_get_config(uint CardNo, ref uint mEnable, ref uint mCmpSource);

        //******************************************************************************************************************************************
        /*
        功  能：清除二维低速位置比较位置；
        参  数：CardNo：控制卡卡号；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_cmp_2d_clear_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_cmp_2d_clear_point(uint CardNo);

        //******************************************************************************************************************************************
        /*
        功  能：增加或更新二维低速位置比较位置；
        参  数：CardNo:控制卡卡号；
        mAxisList[2]： 轴号 0~7；
        mPosList[2]：比较位置；
        备注：位置为脉冲计数器的值或编码器计数器的值，并非逻辑位置或实际位置，只有设置脉冲比率和编码器比率为1时两者才是相等的！！！
        mDirList[2]：比较方向 0：小于等于；1：大于等于；
        mMode：比较模式；功能定义见《MCN420用户使用手册》第七章节中表7.2所示；
        mCmpOutSet：比较结果输出选择；功能定义见《MCN420用户使用手册》第七章节中表7.2所示；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_cmp_2d_add_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_cmp_2d_add_point(uint CardNo, uint[] mAxisList, int[] mPosList, uint[] mDirList, uint mMode, uint mCmpOutSet);

        //*****************************************************************************************************************************************
        /*
        功  能：读取当前二维比较点位置；
        参  数：CardNo:控制卡卡号；
        mPosList:返回当前比较点位置,单位:pulse；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_cmp_2d_get_current_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_cmp_2d_get_current_point(uint CardNo, Int32[] mPosList);

        //****************************************************************************************************************************************
        /*
        功 能：查询已经比较过的二维比较点个数；
        参 数：CardNo:控制卡卡号；
        mPointNum:返回已经比较过的点数；
        返回值:错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_cmp_2d_get_points_runned", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_cmp_2d_get_points_runned(uint CardNo, ref uint mPointNum);

        //******************************************************************************************************************************************
        /*
        功 能：查询可以加入的二维比较点个数；
        参 数：CardNo:控制卡卡号；
        mPointNum:返回可以加入的比较点数；
        返回值:错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_cmp_2d_get_points_remained", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_cmp_2d_get_points_remained(uint CardNo, ref uint mPointNum);

        //***************************************************AD/DA输出控制****************************************************************************************
        /*
        功  能：DAC输出设置
        参  数：CardNo 控制卡卡号
        mChannel：通道 0 or 1
        mEnable：使能, 0：不使能，1：使能；
        mVout：输出电压值,输出电压范围0V~10V；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_dac_control", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_dac_control(uint CardNo, uint mChannel, uint mEnable, double mVout);

        //***********************************************************************************************************************
        /*
        功  能：读取扩展模拟量输入；
        参  数：CardNo  控制卡卡号；
                channel: 模拟量通道号，范围0—1(对应于板子上的是0-1);
		        value:模拟量输入值；
        返回值：错误代码。
        */

        [DllImport("MCN420.dll", EntryPoint = "YK_get_adc_input", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 YK_get_adc_input(uint CardNo, uint channel, ref double value);

        //************************************************************************************************************************
        /*
        功  能：对输入取反；
        参  数：CardNo:控制卡卡号；
                pInputInvertBit:输入位数组指针；
	            arry_len：数组长度，固定值为32；；
        返回值:错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_input_bit_inverted", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_input_bit_inverted(uint CardNo, byte[] pInputInvertBitArr, uint arry_len);

        //**********************************************************************************************************************
        /*
        功  能：读取所有输入端口的逻辑电平；
        参  数：CardNo:控制卡卡号；
                pInputInvertBit:输入位数组指针；
	            arry_len：数组长度，固定值为32；；
        返回值:错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_all_input_bit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_all_input_bit(uint CardNo, byte[] pInputInvertBitArr);

        //*********************************************************************************************************************
        /*
        功  能：对输入位设置有效电平；
        参  数：CardNo:控制卡卡号；
                in_bit_no:输入位端口号；
		        logic_of_on:0:低电平，1高电平
        返回值:错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_input_bit_logic", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_input_bit_logic(uint CardNo, uint in_bit_no, uint logic_of_on);

        //*********************************************************************************************************************      
        /*
        功  能：读取输入位设置有效电平；
        参  数：CardNo:控制卡卡号；
                in_bit_no:输入位端口号；
		        logic_of_on: 返回端口有效电平；0:低电平，1高电平
        返回值:错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_input_bit_logic", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_input_bit_logic(uint CardNo, uint in_bit_no, ref uint logic_of_on);

        //*********************************************************************************************************************        
        /*
        功  能：读取指定通道运行时剩余的缓冲区大小；
        参  数：CardNo： 控制卡卡号；
        mChannel：通道号 0~1；
        返回值：返回指定通道的剩余缓冲区的大小。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_cmd_list_buf_num", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_cmd_list_buf_num(uint CardNo, uint mChannel);

        //**********************************************************************************************************************
        /*
        功  能：控制指定轴的伺服使能端口电平；
        参  数：CardNo 控制卡卡号；
        axis：轴号：0~3；注意：伺服使能只对0-3轴有效，其他四个轴4-7 不具备此功能；0~3轴对应的是通用输OUT32-OUT35;
        sevon_en: 设置伺服使能端口电平，0低电平；1高电平；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_sevon_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_sevon_config(uint CardNo, uint axis, uint sevon_en);

        //*********************************************************************************************************************
        /*
        功  能：控制指定轴的伺服使能端口电平；
        参  数：CardNo 控制卡卡号；
        channel:通道号，可设置为0,1,2；
        axis：轴号：0~3；注意：伺服使能只对0-3轴有效，其他四个轴4-7 不具备此功能；0~3轴对应的是通用输OUT32-OUT35;
        sevon_en: 设置伺服使能端口电平，0低电平；1高电平；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_sevon_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_sevon_config_ex(uint CardNo, uint channel, uint axis, uint sevon_en);

        //*********************************************************************************************************************
        /*
        功  能：读取指定轴的伺服使能端口的电平；
        参  数：CardNo：控制卡卡号；
        axis：指定轴号；注意：伺服使能只对0-3轴有效，其他四个轴4-7 不具备此功能；0~3轴对应的是通用输出口OUT32-OUT35；
        返回值：伺服使能端口电平，0：低电平，1：高电平。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_sevon_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_sevon_config(uint CardNo, uint axis);

        //***********************************************************************************************************************************
        /*
        功  能：读硬件版本号
        参  数：CardNo 控制卡卡号；
        hardWareVer;硬件版本号；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_hardware_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_hardware_version(uint CardNo, ref uint hardWareVer);

        //*******************************************************************************************
        /*
        功能：读固件版本号
        参数：CardNo 控制卡卡号；
        firmWareVer;固件版本号；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_firmware_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_firmware_version(uint CardNo, ref uint firmWareVer);

        //***************************************************************************************
        /*
        功能：读动态库版本号
        参数：CardNo 控制卡卡号；
        dllVer;动态链接库版本号；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_dll_lib_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_dll_lib_version(uint CardNo, ref uint dllVer);

        //****************************************************************************************************************
        /*
        功  能：读取指定卡的卡号类型；
        参  数：CardNo： 控制卡卡号；
        device_type: 设备类型；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_device_type", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_device_type(uint CardNo, ref uint device_type);

        //*********************************************************************************************************************
        /*
        功  能：设置指定卡的硬件ID号
        参  数：CardNo： 控制卡卡号；
                device_id: 设备id号；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_device_id", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_device_id(uint CardNo, uint device_id);

        //*********************************************************************************************************************
        /*
        功  能：读取指定卡的硬件ID号
        参  数：CardNo： 控制卡卡号；
        device_id: 设备id号；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_device_id", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_device_id(uint CardNo, ref uint device_id);

        //************************//高速二维位置比较**********************************************************************************************
        /*
        功  能：清除2维高速比较位置；
        参  数：CardNo：控制卡卡号；
        mHcmp：高速比较器(对应通道0),取值范围0；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_2d_clear_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_2d_clear_point(uint CardNo, uint mHcmp);

        //*********************************************************************************************************************
        /*
        功  能：添加或更新2维高速比较位置;
        参  数：CardNo:控制卡卡号;
        mHcmp：高速比较器(对应通道0),取值范围0；
        cmpPosX：比较位置；
        cmpPosY：比较位置；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_2d_add_point", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_2d_add_point(uint CardNo, uint mHcmp, Int32 cmpPosX, Int32 cmpPosY);

        //*****************************************************************************************************************************
        /*
        功  能：添加或更新2维高速比较位置;
        参  数：CardNo:控制卡卡号;
        mHcmp：高速比较器(对应通道0),取值范围0；
        cmpPosX：比较位置；
        cmpPosY：比较位置；
        logicLevel:输出逻辑电平，0：低电平，1：高电平；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_2d_add_point_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_2d_add_point_ex(uint CardNo, uint mHcmp, Int32 cmpPosX, Int32 cmpPosY, uint logicLevel);

        //*********************************************************************************************************************
        /*
        功  能：设置2维高速比较配置；
        参  数：CardNo：控制卡卡号；
        mHcmp：高速比较器(对应通道0),取值范围0；
        Level：初始化电平；
        mode：模式 0：脉冲，1：电平翻转；2.输出电平(调用YK_hcmp_2d_add_point_ex添加位置比较点)
        time：脉冲宽度时间 us；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_2d_set_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_2d_set_config(uint CardNo, uint mHcmp, uint Level, uint mode, uint time);

        //****************************************************************************************************************************
        /*
        功  能：设置2维高速比较参数；
        参  数：CardNo：控制卡卡号；
        mHcmp：高速比较器(对应通道0),取值范围0；
        mAxisX：关联轴号0~7；
        mAxisY：关联轴号0~7；
        cmpSource：比较位置源： 0：指令位置；1：编码器位置；
        MaxError：比较范围最大误差；
        threshold：最优算法阈值；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_2d_set_param", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_2d_set_param(uint CardNo, uint mHcmp, uint mAxisX, uint mAxisY, uint cmpSource, uint MaxError, uint threshold);
        //******************************************************************************************************************************************************************************

        /*
        功  能：设置2维高速比较开始；
        参  数：CardNo：控制卡卡号；
        mHcmp：高速比较器(对应通道0),取值范围0；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_2d_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_2d_start(uint CardNo, uint mHcmp);

        //******************************************************************************************************************************************************
        /*
        功  能：设置2维高速比较停止；
        参  数：CardNo：控制卡卡号；
        mHcmp：高速比较器(对应通道0),取值范围0；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_2d_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_2d_stop(uint CardNo, uint mHcmp);

        //************************************************************************************************************************************
        /*
        功  能：读取2维高速比较当前状态；
        参  数：CardNo：控制卡卡号；
        mHcmp：高速比较器(对应通道0),取值范围0；
        mStatus：当前状态  0 停止 ， 1正在比较
        remainedPoints：可添加点数；
        currentPointX：返回当前比较点位置，单位：pulse
        currentPointY：返回当前比较点位置，单位：pulse
        runDonePoints：返回已运行完的比较点数；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_2d_get_current_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_2d_get_current_state(uint CardNo, uint mHcmp, ref uint mStatus, ref uint remainedPoints, ref Int32 currentPointX, ref Int32 currentPointY, ref uint runDonePoints);

        //**********************************************************************************************************************************************************
        /*
        功  能：设置2维长度比较输出参数；
        参  数：CardNo：控制卡卡号；
        mAxisX：关联轴号0~7；
        mAxisY：关联轴号0~7；
        cmpSource：比较源：0指令位置，1编码器位置；
        Level：初始化电平：0低电平，1高电平
        mPulseWide：脉冲宽度us；
        mIncrement：长度（单位脉冲）；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_length_cmp_2d_set_param", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_length_cmp_2d_set_param(uint CardNo, uint Channel, uint mAxisX, uint mAxisY, uint cmpSource, uint Level, uint mPulseWide, uint mIncrement);

        //**************************************************************************************************************************************************************
        /*
        功  能：设置2维长度比较输出使能；
        参  数：CardNo：控制卡卡号；
        enable：1使能，0不使能
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_length_cmp_2d_set_en", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_length_cmp_2d_set_en(uint CardNo, uint Channel, uint enable);

        //*******************************************************************************************************
        /*
        功  能：读取2维高速长度比较当前状态；
        参  数：CardNo：控制卡卡号；
        mStatus：当前状态  0 停止 ， 1正在比较
        axisXSelect：x轴选择轴号
        axisYSelect：x轴选择轴号；
        mSource：比较源 0~1  0：指令位置；1：编码器位置
        mInitLevel:初始化电平
        mPulseWide：脉冲宽度，单位：us
        mIncrement：长度，单位：pulse
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_length_cmp_2d_get_current_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_length_cmp_2d_get_current_state(uint CardNo, ref uint mStatus, ref Int32 axisXSelect, ref Int32 axisYSelect, ref Int32 mSource, ref Int32 mInitLevel, ref Int32 mPulseWide, ref Int32 mIncrement);

        //*****************************************************************************************************************************************
        /*
        功  能：缓存区等待通用输入IO；
        参  数：CardNo：控制卡卡号；
        CmdNum://行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下
        channel：通道号，0 or 1；
        di_id：IO号，取值范围0-127；
        di_logic：需要等待的通用输入IO口电平；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_buf_wait_di", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_buf_wait_di(uint CardNo, uint CmdNum, uint channel, uint di_id, uint di_logic);//缓存区等待通用输入IO指令。

        //*********************************************************************************************************************
        /*
        功    能：设置控制器的IP。
        参    数：CardNo：表示卡号;
        IP_Param：坐标系参数。
        返回值  ：正确：返回 ERR_NoError；
        错误：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_ip", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_ip(uint CardNo, ref IP_Param IP_Param);

        //*********************************************************************************************************************
        /*
        功    能：获取控制器的IP地址。
        参    数：CardNo：表示卡号;
        IP_Param：返回IP参数。
        返回值  ：正确：返回 ERR_NoError；
        错误：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_ip", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_ip(uint CardNo, ref IP_Param IP_Param);

        //********************************************************************************************************************* 
        /*
        功能：PWM输出全部参数一次设置
        参数：CardNo：控制卡卡号；
        CmdNum: 行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下,默认为零
        pwm_channel：通道号，取值范围为0和1；
        pwm_enable： pwm使能，0:不使能，1：使能；
        pwm_freq：pwm频率，取值范围0-2MHz；
        pwm_duty：pwm占空比，取值范围0-1；
        pwm_mode：pwm跟随模式：0：直接输出模式，1：速度跟随模式；
        axis_list：pwm轴号列表：取值范围0—7；
        axis_num:pwm轴数量；
        speed_duyt：速度为1输出占空比的量；
        cut_offspeed: 截止速度，低于该速度PWM无输出；
        返回值：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_pwm_out_put", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_pwm_out_put(uint CardNo, uint CmdNum, uint pwm_channel, uint pwm_enable, double pwm_freq, double pwm_duty, uint pwm_mode, uint[] axis_list, uint axis_num, double speed_duyt, double cut_offspeed);

        //*********************************************************************************************************************
        /*
        功  能：一次读取PWM输出全部参数设置
        参  数：CardNo：控制卡卡号；
		        CmdNum: 行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下,默认为零
		        pwm_channel：通道号，取值范围为0和1；
		        pwm_enable： 返回pwm使能，0:不使能，1：使能；
		        pwm_freq：返回pwm频率，取值范围0-2MHz；
		        pwm_duty：返回pwm占空比，取值范围0-1；
		        pwm_mode：返回pwm跟随模式：0:PWM直接输出，1：PWM频率跟随和速度，脉宽0.5，2：PWM脉宽跟随和速度，频率为设置频率
		        axis_list：返回pwm轴号列表：取值范围0—7；
		        axis_num: 返回pwm轴数量；
		        speed_duyt：返回速度为1输出占空比的量；
                cut_offspeed: 返回截止速度，低于该速度PWM无输出；
        返回值：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_pwm_out_put", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_pwm_out_put(uint CardNo, uint CmdNum, uint pwm_channel, ref uint pwm_enable, ref double pwm_freq, ref double pwm_duty, ref uint pwm_mode, uint[] axis_list, ref uint axis_num, ref double speed_duyt, ref double cut_offspeed);

        //************************************************************************************************************************************************
        /*
        功能：PWM使能设置
        参数：CardNo：控制卡卡号；
        CmdNum: 行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下,默认为零
        pwm_channel：通道号，取值范围为0和1；
        pwm_enable： pwm使能，0:不使能，1：使能；

        返回值：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_pwm_enalbe", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_pwm_enalbe(uint CardNo, uint CmdNum, uint pwm_channel, uint pwm_enable);

        //*********************************************************************************************************************
        /*
        功能：读取PWM使能
        参数：CardNo：控制卡卡号；
        CmdNum: 行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下,默认为零
        pwm_channel：通道号，取值范围为0和1；
        pwm_enable： 返回pwm使能，0:不使能，1：使能；

        返回值：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_pwm_enalbe", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_pwm_enalbe(uint CardNo, uint CmdNum, uint pwm_channel, ref uint pwm_enable);

        //************************************************************************************************************************************************
        /*
        功能：PWM频率设置
        参数：CardNo：控制卡卡号；
        CmdNum: 行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下,默认为零
        pwm_channel：通道号，取值范围为0和1；
        pwm_freq：pwm频率，取值范围0-2MHz；

        返回值：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_pwm_frequency", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_pwm_frequency(uint CardNo, uint CmdNum, uint pwm_channel, double pwm_freq);

        //*************************************************************************************************************************************************
        /*
        功能：读取PWM频率
        参数：CardNo：控制卡卡号；
        CmdNum: 行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下,默认为零
        pwm_channel：通道号，取值范围为0和1；
        pwm_freq：返回pwm频率，取值范围0-2MHz；

        返回值：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_pwm_frequency", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_pwm_frequency(uint CardNo, uint CmdNum, uint pwm_channel, ref double pwm_freq);

        //*********************************************************************************************************************
        /*
        功能：PWM占空比设置
        参数：CardNo：控制卡卡号；
        CmdNum: 行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下,默认为零
        pwm_channel：通道号，取值范围为0和1；
        pwm_duty：pwm占空比，取值范围0-1；

        返回值：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_pwm_duty_cycle", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_pwm_duty_cycle(uint CardNo, uint CmdNum, uint pwm_channel, double pwm_duty);
        //*********************************************************************************************************************
        /*
        功能：读取PWM占空比
        参数：CardNo：控制卡卡号；
        CmdNum: 行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下,默认为零
        pwm_channel：通道号，取值范围为0和1；
        pwm_duty：返回pwm占空比，取值范围0-1；

        返回值：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_pwm_duty_cycle", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_pwm_duty_cycle(uint CardNo, uint CmdNum, uint pwm_channel, ref double pwm_duty);
        //****************************************************************************************************************************************************
        /*
        功能：PWM输出模式设置
        参数：CardNo：控制卡卡号；
        CmdNum: 行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下,默认为零
        pwm_channel：通道号，取值范围为0和1；
        pwm_mode：pwm输出模式：0：直接输出模式，1：速度跟随模式；

        返回值：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_pwm_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_pwm_mode(uint CardNo, uint CmdNum, uint pwm_channel, uint pwm_mode);
        //*********************************************************************************************************************
        /*
        功能：读取PWM输出模式
        参数：CardNo：控制卡卡号；
        CmdNum: 行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下,默认为零
        pwm_channel：通道号，取值范围为0和1；
        pwm_mode：返回pwm输出模式：0：直接输出模式，1：速度跟随模式；

        返回值：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_pwm_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_pwm_mode(uint CardNo, uint CmdNum, uint pwm_channel, ref uint pwm_mode);

        //****************************************************************************************************************************************************
        /*
        功能：PWM速度跟随设置
        参数：CardNo：控制卡卡号；
        CmdNum: 行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下,默认为零
        pwm_channel：通道号，取值范围为0和1；
        axis_list：pwm轴号列表：取值范围0—7；
        axis_num:pwm轴数量；
        speed_duyt：速度为1输出占空比的量；
        cut_offspeed: 返回截止速度，低于该速度PWM无输出；
        返回值：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_pwm_follow_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_pwm_follow_speed(uint CardNo, uint CmdNum, uint pwm_channel, uint[] axis_list, uint axis_num, double speed_duyt, double cut_offspeed);
        //*******************************************************************************************************************
        /*
            功能：读取PWM速度跟随设置
            参数：CardNo：控制卡卡号；
            CmdNum: 行号 0~n ，用于识别控制运行到哪里了，主要用在连续轨迹模式下,默认为零
            pwm_channel：通道号，取值范围为0和1；
            axis_list：返回pwm轴号列表：取值范围0—7；
            axis_num: 返回pwm轴数量；
            speed_duyt：返回速度为1输出占空比的量；
            cut_offspeed: 返回截止速度，低于该速度PWM无输出
            返回值：返回相关错误码。
            */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_pwm_follow_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_pwm_follow_speed(uint CardNo, uint CmdNum, uint pwm_channel, uint[] axis_list, ref uint axis_num, ref double speed_duyt, ref double cut_offspeed);
        //*********************************************************************************************************************************************************************************
        /*
        功    能：设置轴反向间隙补偿参数。
        参    数：CardNo：表示卡号;
        axis：轴号。
        CompValue：反向间隙补偿值，当为0时表示没有使能反向间隙补偿功能，取值范围：[0, 1073741824]，单位：脉冲
        CompSpeed：反向间隙补偿的变化量，取值范围：[0, 1073741824]，单位：脉冲/毫秒
        当该参数的值为0或者大于等于compValue时，则反向间隙的补偿量将瞬间叠加在规划位置上，没有渐变的过程
        CompDir：反向间隙补偿方向 0：只补偿负方向，当电机向负方向运动时，将施加补偿量，当电机向正方向运动时，不施加补偿量
        1：只补偿正方向，当电机向正方向运动时，将施加补偿量，当电机向负方向运动时，不施加补偿量
        返回值  ：正确：返回 ERR_NoError；
        错误：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_axis_reverse_clearance_compensation", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_axis_reverse_clearance_compensation(uint CardNo, uint axis, uint CompValue, double CompSpeed, uint CompDir);
        //*******************************************************************************************************************************************************
        /*
        功  能：读出轴反向间隙补偿参数。
        参    数：CardNo：表示卡号;
        axis：轴号。
        CompValue：反向间隙补偿值，当为0时表示没有使能反向间隙补偿功能，取值范围：[0, 1073741824]，单位：脉冲
        CompSpeed：反向间隙补偿的变化量，取值范围：[0, 1073741824]，单位：脉冲/毫秒
        当该参数的值为0或者大于等于compValue时，则反向间隙的补偿量将瞬间叠加在规划位置上，没有渐变的过程
        CompDir：反向间隙补偿方向 0：只补偿负方向，当电机向负方向运动时，将施加补偿量，当电机向正方向运动时，不施加补偿量
        1：只补偿正方向，当电机向正方向运动时，将施加补偿量，当电机向负方向运动时，不施加补偿量
        返回值  ：正确：返回 ERR_NoError；
        错误：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_axis_reverse_clearance_compensation", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_axis_reverse_clearance_compensation(uint CardNo, uint axis, ref uint CompValue, ref double CompSpeed, ref uint CompDir);
        //*******************************************************************************************************************************************
        /*
        功    能：设置编码器Z脉冲计数器。
        参    数：CardNo：表示卡号;
        axis：轴号。
        data：编码器Z脉冲设置计数值。
        返回值  ：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_ez_counter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_ez_counter(uint CardNo, uint axis, Int32 data);
        //******************************************************************************************************************************
        /*
        功  能：读出编码器Z脉冲计数器；
        参  数：CardNo：控制卡卡号；
        axis：轴号
        返回值：返回编码器Z脉冲设置计数值。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_ez_counter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_ez_counter(uint CardNo, uint axis);
        //******************************************************************************************************************************
        /*
        功  能：读can状态；
        参  数：CardNo：控制卡卡号；
        mCanIoLinkStatus：返回CAN-IO模块的连接状态；最多连接12个CAN模块。读出一个u32，Bit0为1代表地址1的CAN模块已经连接，0为未连接；Bit1为1代表地址2的CAN模块已经连接，0为未连接；Bit2为2代表地址3的CAN模块已经连接，0为未连接；Bit3为1代表地址3的CAN模块已经连接，0为未连接；....Bit11为1代表地址12的CAN模块已经连接，0为未连接；
        canIoNum:返回已连接CAN-IO模块数量，范围[0,12];
        返回值：返回相关错误码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_can_io_link_state", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_can_io_link_state(uint CardNo, ref uint mCanIoLinkStatus, ref uint canIoNum);
        //**********************************************************************************************************
        /*
        功能： 重新连接CAN-IO模块
        参数：CardNo：卡号；
        返回值：返回错误码；
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_can_io_relinking", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_can_io_relinking(uint CardNo);
        //******************************************************************************************************************************************************


        /*
            功 能:心跳使能设置； 
            参 数:CardNo:控制卡卡号；
            heartbeat_enable:设置心跳使能；其值 0：禁止；1：使能；
            hold_time：心跳最大维持时间 单位：ms；
            返回值:错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_heartbeat_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_heartbeat_enable(uint CardNo, uint heartbeat_enable, uint hold_time);
        //**********************************************************************************************************************
        /*
        功 能:心跳保持设置； 
        参 数:CardNo:控制卡卡号；
        返回值:错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_heartbeat_Keep", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_heartbeat_Keep(uint CardNo);
        //*********************************************************************************************************************
        /*
        功 能:设置心跳IO控制； 
        参 数:CardNo:控制卡卡号；
        channel:通道号，取值范围0-1；
           io_num：IO数量；
           outputList[32]:IO输出端口列表数组，长度为io_num，最大长度为32；
           outputLogicList[32]:IO输出端口电平列表，长度为io_num，最大长度为32；电平值：0-低电平，1-高电平；
        返回值:错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_heartbeat_io_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_heartbeat_io_output(uint CardNo, uint channel, uint io_num, ushort[] outputList, ushort[] outputLogicList);


        //******************************************************************************************************************************************************





        /*
        功  能：边沿触发停止轴
        参  数：CardNo：卡号；
		        aixs:  轴号0-7；
		        enalbe: 边沿触发使能；0为禁止；1为使能；
        返回值：返回错误码；
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_axis_lock_emg_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_axis_lock_emg_stop(uint CardNo, uint axis, uint enable);
        //******************************************************************************************************************************************************
        /*
        功  能：读取指定卡所有轴的状态
        参  数：CardNo：卡号；
                结构体成员说明：
	            unsigned char IoInput[4];                        //通用输入0-32,0表示低电平，1表示高电平；
	            unsigned char IoOutput[2];                       //通用输出0-16,0表示低电平，1表示高电平；
		        unsigned char AxisRunStatus[8];                  //轴运动状态，0在运动中，1停止；
		        unsigned char AlmInput[8];                       //轴输入报警ALM信号状态，0：正常，1：报警。
		        unsigned char PLimitInput[8];                    //轴输入正硬件限位信号状态，0表示无效；1表示有效。
		        unsigned char NLimitInput[8];                    //轴输入负硬件限位信号状态，0表示无效；1表示有效。
                unsigned char OrgInput[8];                       //轴输入原点信号状态，0表示无效；1表示有效。		
		        int AxisMotionAlmStatus[8];                      //轴运行状态信息，0表示正常；bit0为1是暂停；bit1为1是急停；bit2为1是减速停止；bit3为1是正硬件限位停止；bit4为1是负硬件限位停止；bit5为1正软件限位停止；bit6为1是负软件限位停止；bit7为1是驱动器报警。
		        double AxisVel[8];                               //轴速度，单位是unit/ms；
		        double AxisCmdPos[8];                            //轴位置，单位是unit；
		        double AxisEncoderPos[8];                        //编码器位置，单位是unit；
        返回值：返回错误码；
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_read_all_axis_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_read_all_axis_status(uint CardNo, ref ReadAllAxisStatusData pGetAllStatus);
        //******************************************************************************************************************************************************
        /*
        功  能：设置手轮限速功能
        参  数：CardNo：卡号；
		        aixs:  轴号0-7；
		        hwheel_max_speed:手轮最大速度；
		        hwheel_dec_stop：手轮减速停止加速度；
        返回值：返回错误码；
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_hand_wheel_limit_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_hand_wheel_limit_vel(uint CardNo, uint axis, double hwheel_max_speed, double hwheel_dec_stop);
        //******************************************************************************************************************************************************
        /*
        功  能：读取手轮限速功能
        参  数：CardNo：卡号；
		        aixs:  轴号0-7；
		        hwheel_max_speed:返回手轮最大速度；
		        hwheel_dec_stop：返回手轮减速停止加速度；
        返回值：返回错误码；
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_hand_wheel_limit_vel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_hand_wheel_limit_vel(uint CardNo, uint axis, ref double hwheel_max_speed, ref double hwheel_dec_stop);
        //******************************************************************************************************************************************************
        /*
        功  能：读取以同时读取任意两个轴同一个控制周期的指令位置和编码器位置
        参  数：CardNo：卡号；
                结构体成员说明：
	            axisList;轴号0-7；
	            syncCmdPos[2];//轴位置，单位是unit；
	            syncEncPos[2];//编码器位置，单位是unit；
        返回值：返回错误码；
        */

        [DllImport("MCN420.dll", EntryPoint = "YK_get_axis_sync_pos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_axis_sync_pos(uint CardNo, uint[] axisList, ref ReadAxisSyscnPos rdAxisSyncPos);
        //******************************************************************************************************************************************************
        /*
        功  能：读MCN420连网状态；
        参  数：CardNo：控制卡卡号；
        返回值：正确连接，返回true,断网或掉线连接，返回false。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_check_device_network_link", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool YK_check_device_network_link(uint CardNo);
        //******************************************************************************************************************************************************
        /*
        功  能：读取指定轴的外部急停状态；
        参  数：CardNo：控制卡卡号；
        返回值：返回输入的EMG信号状态，0：低电平，1：高电平；反回其他，错误码
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_emg_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_emg_status(uint CardNo);
        //*******************************************************************************************************************************************************
        /*
        功  能：读取指定轴的当前编码器速度；
        参  数：CardNo：控制卡卡号；
	            axis：轴号0~7 ；
        返回值：返回指定轴的当前速度，单位是unit/ms。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_current_encoder_speed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern double YK_get_current_encoder_speed(uint CardNo, uint axis);
        //*******************************************************************************************************************************************************
        /*
        功  能：添加或更新一维高速比较位置;
        参  数：CardNo:控制卡卡号;
	            mHcmp：高速比较器(对应通道0~3),取值范围0~3；
	            pCmpPosList：比较位置列表数组，单位是pulse，最大长度不限；
		        output_logic_list: 比较输出电平列表数组， 0：低电平，1：高电平；最大长度不限；
		        mListLength：数组长度，比较位置列表数组必须与比较输出电平列表数组一一对应，长度必须相等。
                备  注：
		        (1)位置为脉冲计数器的值或编码器计数器的值，并非逻辑位置或实际位置，只有设置脉冲比率和编码器比率为1时两者才是相等的！
		        (2)比较位置列表数组必须与比较输出电平列表数组一一对应，长度必须相等；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_hcmp_1d_add_point_list", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_hcmp_1d_add_point_list(uint CardNo, uint mHcmp, int[] pCmpPosList, uint[] output_logic_list, uint mListLength);
        //*******************************************************************************************************************************************************
        /*
        功  能：添加添加凸轮表;
        参  数：CardNo:控制卡卡号;
                Channel:高速比较通道0~3;
                CamMasterAxisNo:主轴轴号
                CamSlaveAxisNo:从轴轴号;
                CamCount:数据个数（主从轴位置数据长度）;
                CamSrcMode:主轴位置模式：0-指令位置，1-反馈位置;
                CamMasterPos:主轴位置数组列表，最大256个长度;
                CamSlavePos:从轴位置列表，最大256个长度;
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_ecam_table", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_ecam_table(uint CardNo, uint Channel, uint CamMasterAxisNo, uint CamSlaveAxisNo, uint CamCount, uint CamSrcMode, double[] CamMasterPos, double[] CamSlavePos);
        //*******************************************************************************************************************************************************
        /*
        功  能：添加添加凸轮表;
        参  数：CardNo:控制卡卡号;
                Channel:高速比较通道0~3;
                CamSlaveAxisEnable:从轴使能；
                SlaveIo：从轴IO；
                SlaveActive：从轴有效电平；
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_ecam_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_ecam_enable(uint CardNo, uint Channel, uint CamSlaveAxisEnable, uint SlaveIo, uint SlaveActive);
        //*******************************************************************************************************************************************************
        /*
        功  能：添加添加凸轮表;
        参  数：CardNo:控制卡卡号;
                Channel:高速比较通道0~3;
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_ecam_unenable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_ecam_unenable(uint CardNo, uint Channel);
        //*******************************************************************************************************************************************************
        /*
        功  能：单轴联动；
        参  数：CardNo 控制卡卡号；
                unsigned int Channel:通道号；
                Master_Req_Param: 主轴参数结构体，具体内容见其结构体；
                Slave_Req_Param：从轴参数结构体，具体内容见其结构体；
                enalbe：使能，0：不使能；1：使能；
                trigger_dir:触发方向；-1：负向；1：正向；
                trigger_pos：触发位置
                trigger_mode：触发模式： 0：指令位置；1：编码器位置；

        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_ecam_unenable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_pmove_union(uint CardNo, uint Channel, ref Master_Req_Param pMasterParam, ref Slave_Req_Param pSlaveParam, uint enalbe, int trigger_dir, int trigger_pos, uint trigger_mode);
        //*******************************************************************************************************************************************************
        /*
        功  能：读取单轴联动使能；
        参  数：CardNo 控制卡卡号；
                Channel:通道号；
                enalbe：返回使能，0：不使能；1：使能；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_pmove_union_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_pmove_union_enable(uint CardNo, uint Channel, ref uint enalbe);
        //*******************************************************************************************************************************************************
        /*
        功  能：设置单轴联动使能；
        参  数：CardNo 控制卡卡号；
                Channel:通道号；
                enalbe：使能，0：不使能；1：使能；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_pmove_union_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_pmove_union_enable(uint CardNo, uint Channel, uint enalbe);
        //*******************************************************************************************************************************************************
        //---------------------2023/04/04 add--------------------------------------------------------------
        /*
        功  能：设置指定轴的INP信号(伺服INP信号设置，包含INP信号使能，INP信号的有效电平); 
        参  数：CardNo:控制卡卡号;
                axis:指定轴号,取值范围MCN420:0-3;
                enable:INP信号使能,0:禁止,1:允许;
                inp_logic:INP信号的有效电平,0:常闭,1:常开;
        返回值：错误代码
        注  意：当使能INP信号功能后,只有在INP信号为有效状态时,对应的轴才能进行运动,否则此时检测轴的状态是正在运行(即对轴运动作限制)
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_inp_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_inp_config(uint CardNo, uint axis, uint enable, uint inp_logic);
        /*
        功 能：读取指定轴的INP信号设置(获取INP信号的设置值，包含INP信号的使能，INP信号的有效电平);
        参 数：CardNo:控制卡卡号;
               axis:指定轴号,取值范围:MCN420:0-3;
               enable:返回INP信号使能状态;
               inp_logic:返回设置的INP信号有效电平;
        返回值：错误代码	   
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_inp_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]

        public static extern Int32 YK_get_inp_config(uint CardNo, uint axis, ref uint enable, ref uint inp_logic);
        /*
        功  能：读取指定轴的到位信号状态；
        参  数：CardNo：控制卡卡号；
                axis：轴号0~3 ；
        返回值：返回输入的到位信号状态，0：无效，1：有效。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_axis_inp_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_axis_inp_status(uint CardNo, uint axis);
        /*
        功  能：读取指定轴的RDY端口的电平或RDY状态(获取伺服的rdy信号值); 
        参  数：CardNo：控制卡卡号；
                axis：轴号0~3 ；
        返回值：返回RDY:端口电平,0:低电平,1:高电平;
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_axis_rdy_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_axis_rdy_status(uint CardNo, uint axis);

        /*
        功  能：设置指定轴的ERC信号(伺服ERC信号设置，ERC信号的有效电平); 
        参  数：CardNo:控制卡卡号;
                axis:指定轴号,取值范围MCN420:0-3;
                erc_logic:ERC信号的有效电平,0:常闭,1:常开;
        返回值：错误代码;
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_erc_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_erc_config(uint CardNo, uint axis, uint erc_logic);
        /*
        功 能：读取指定轴的ERC信号设置(获取ERC信号的有效电平);
        参 数：CardNo:控制卡卡号;
               axis:指定轴号,取值范围:MCN420:0-3;
               erc_logic:返回设置的ERC信号有效电平;
        返回值：错误代码	   
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_erc_config", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_erc_config(uint CardNo, uint axis, ref uint erc_logic);
        //************************/检测轴到位状态***************************************************************************************
        /*
        功  能：设置位置误差带(设置编码器系数、误差带);       
        参  数：CardNo:控制卡卡号;
                axis:指定轴号,取值范围MCN420:0~3;
                checkDoneEn:编码器到位CheckDone使能,0:不启用; 1:启用;
                errPos:位置误差带,单位:unit;
        返回值：错误代码
        编码器系数的说明：当使用YK_check_success_encoder函数检测编码器是否到位时,其用于判断的编码器位置为:编码器计数值乘以编码器系数的值。	   
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_factor_error", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_factor_error(uint CardNo, uint axis, uint checkDoneEn, double errPos);
        /*
         功 能：读取位置误差带设置(获取编码器系数、误差带);   
         参 数：CardNo:控制卡卡号;
                axis:指定轴号,取值范围MCN420:0~3;
                checkDoneEn:编码器到位CheckDone使能,0:不启用; 1:启用;
                errPos:位置误差带,单位:unit;
        返回值：错误代码 
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_factor_error", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_factor_error(uint CardNo, uint axis, ref uint checkDoneEn, ref double errPos);

        /*
         功  能：检测指令到位(检测指令位置到位情况);    
         参  数：CardNo:控制卡卡号;
                 axis:指定轴号,取值范围MCN420:0~3;
         返回值：0:表示指令位置在设定的目标位置的误差带之外;
                 1:表示指令位置在设定的目标位置的误差带之内;
         注 意： 1)该函数只适用于单轴运动;
                 2)检测函数请在YK_check_done检测到轴停止后调用,函数调用后会等待轴到位后返回,如果调用函数100ms内未到位,函数超时返回认为不到位;  
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_check_success_pulse", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_check_success_pulse(uint CardNo, uint axis);
        /*
         功  能：检测编码器到位(检测编码器反馈位置到位情况);   
         参  数：CardNo:控制卡卡号;
                 axis:指定轴号,取值范围MCN420:0~3;
         返回值：0:表示编码器位置在设定的目标位置的误差带之外;
                 1:表示编码器位置在设定的目标位置的误差带之内;
         注  意：1)该函数只适用于单轴运动;
                 2)检测函数请在YK_check_done检测到轴停止后调用,函数调用后会等待轴到位后返回,如果
                 调用函数100ms内未到位,函数超时返回认为不到位;
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_check_success_encoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_check_success_encoder(uint CardNo, uint axis);
        //-----------------------add 20230606------------------------------------------------------------------------------------------
        /*
        功 能:设置跟随参数； 
        参 数:CardNo:控制卡卡号；
              slave_axis:跟随轴从轴轴号,取值范围:0-7；
              master_axis:跟随轴主轴轴号,取值范围:0-7； 
              followradio:跟随比例  
        返回值:错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_follow_parameter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_follow_parameter(uint CardNo, ushort slave_axis, ushort master_axis, double followradio);
        /*
        功 能:跟随参数读取； 
        参 数:CardNo:控制卡卡号；
              slave_axis:跟随轴从轴轴号,取值范围:0-7；
              master_axis:返回跟随轴主轴轴号,取值范围:0-7； 
              followradio:跟随比例  
        返回值:错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_fellow_parameter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_fellow_parameter(uint CardNo, ushort slave_axis, ref ushort master_axis, ref double followradio);
        /*
        功 能:设置跟随参数； 
        参 数:CardNo:控制卡卡号；
              slave_axis:跟随轴从轴轴号,取值范围:0-7； 
              enable:跟随使能：0不使能  1：使能； 
        返回值:错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_follow_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int YK_set_follow_enable(uint CardNo, ushort slave_axis, ushort enable);//跟随使能设置
        /*
        功 能:跟随使能读取； 
        参 数:CardNo:控制卡卡号；
            slave_axis:返回跟随轴从轴轴号,取值范围:0-7； 
            enable:返回跟随使能：0不使能  1：使能； 
        返回值:错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_follow_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_follow_enable(uint CardNo, ushort slave_axis, ref ushort enable); //
        /*
        功 能:设置编码器相脉宽测试极性； 
        参 数:CardNo:控制卡卡号；
                test_polarity:脉宽测试相极性，0-低电平；1-高电平；
                            其值 第0位：A相极性；第1位：B相极性；第2位：C相极性；
        返回值:错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_encoder_phase_pulse_width_test_polarity", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_set_encoder_phase_pulse_width_test_polarity(uint CardNo, ushort test_polarity);
        /*
        功 能:读取编码器相脉宽； 
        参 数:CardNo:控制卡卡号；
              encoder_phase:指定编码器相；其值 0：A相；1：B相；2：C相；
              phase_pulse_width：获取到的编码器相脉冲跨度；
        返回值:错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_get_encoder_phase_pulse_width", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_get_encoder_phase_pulse_width(uint CardNo, uint encoder_phase, ref uint phase_pulse_width);






        /*
        功  能：查询指令缓冲状态；
        参  数：CardNo:控制卡卡号；
        buf_id:缓冲区id号；
        run_staus:指令缓冲状态：0空闲状态,1写入状态,2激活状态,3结束状态,4错误状态;
        segment:读取当前已经完成的插补段数。
		
        返回值: 错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_static_buf_get_status", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_static_buf_get_status(uint CardNo, uint buf_id, ref uint run_staus, uint segment);//查询指令缓冲状态     0,//空闲状态 1,//写入状态 2,//激活状态 ,3//结束状态  4//错误状态



        /*
功  能：清除指令缓冲区,使用缓冲前建议清除；
参  数：CardNo:控制卡卡号；
        buf_id:缓冲区id号;
返回值: 错误代码。
*/
        [DllImport("MCN420.dll", EntryPoint = "YK_static_buf_clear", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]

        public static extern Int32 YK_static_buf_clear(uint CardNo, uint buf_id);//清除指令缓冲区,使用缓冲前建议清除


        /*
功  能：进入写入指令缓冲状态；
参  数：CardNo:控制卡卡号；
        buf_id:缓冲区id号;
返回值: 错误代码。
*/
        [DllImport("MCN420.dll", EntryPoint = "YK_static_buf_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]

        public static extern Int32 YK_static_buf_start(uint CardNo, uint buf_id);//进入写入指令缓冲状态  

        /*
功  能：进入激活指令缓冲状态；
参  数：CardNo:控制卡卡号；
        buf_id:缓冲区id号;
返回值: 错误代码。
*/
        [DllImport("MCN420.dll", EntryPoint = "YK_static_buf_active", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]

        public static extern Int32 YK_static_buf_active(uint CardNo, uint buf_id);//进入激活指令缓冲状态




        /*
功  能：进入停止指令缓冲状态；
参  数：CardNo:控制卡卡号；
        buf_id:缓冲区id号;
返回值: 错误代码。
*/
        [DllImport("MCN420.dll", EntryPoint = "YK_static_buf_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]

        public static extern Int32 YK_static_buf_stop(uint CardNo, uint buf_id);//进入停止指令缓冲状态

        /*
功  能：缓存区内延时设置指令，用于指令与指令之间的非阻塞延时；
参  数：CardNo:控制卡卡号；
        buf_id:缓冲区id号;
        delay_time:延时时间.单位：ms；
返回值: 错误代码。

*/
        [DllImport("MCN420.dll", EntryPoint = "YK_static_buf_delay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]

        public static extern Int32 YK_static_buf_delay(uint CardNo, uint buf_id, uint delay_time, uint segment);//缓存区内延时设置指令，用于指令与指令之间的非阻塞延时  delayTime 单位us 最大10s
        /*
功  能：指令缓冲阻塞检查轴运动指令; 
参  数：CardNo:控制卡卡号;
        buf_id:缓冲区id号;
        axis:指定轴号,取值范围:0~7;
返回值：缓冲区轴阻塞状态:0:正在使用中,1:正常停止;
*/
        [DllImport("MCN420.dll", EntryPoint = "YK_static_buf_check_done", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]

        public static extern Int32 YK_static_buf_check_done(uint CardNo, uint buf_id, uint axis, uint segment); //指令缓冲阻塞检查轴运动指令;

        /*
功  能：指令缓冲阻塞检查位置指令;
参  数：CardNo 控制卡卡号;
        axis:检测位置轴号,取值范围:0~7;
		mode:模式，0：大于 1或不等于0：小于；
		pulse_value:位置信息;
		segment：段号；
返回值：错误代码。
*/
        [DllImport("MCN420.dll", EntryPoint = "YK_static_buf_check_position", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]

        public static extern Int32 YK_static_buf_check_position(uint CardNo, uint buf_id, uint axis, uint mode, long pulse_value, uint segment); //指令缓冲阻塞检查位置指令





        /*
功  能：指令缓冲设置指定控制卡的某个输出端口的电平 ; 
参  数：CardNo:控制卡卡号;
		bitno:输出端口号,取值范围:0~31;
		on_off:输出电平,0:低电平,1:高电平;
		segment：段号；
返回值：错误代码。	   
*/
        [DllImport("MCN420.dll", EntryPoint = "YK_static_buf_write_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_static_buf_write_outbit(uint CardNo, uint buf_id, uint bitno, uint on_off, uint segment);	//指令缓冲设置指定控制卡的某个输出端口的电平 
        /*
        功  能：指令缓冲检测某个输出端口的电平 ; 
        参  数：CardNo:控制卡卡号;
                bitno:输出端口号,取值范围:0~31;
                on_off:输出电平,0:低电平,1:高电平;
                segment：段号；
        返回值：错误代码。	   
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_static_buf_wait_input", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]

        public static extern Int32 YK_static_buf_wait_input(uint CardNo, uint buf_id, uint bitno, uint on_off, uint segment);//指令缓冲检测某个输入	


        /*
功  能：指令缓冲指定轴点位运动 	；
参  数：CardNo :控制卡卡号；
		buf_id:缓冲区buf id号;
		axis:定长运动轴号；
		axis_dist:定长运动轴运行目标位置，单位是unit；
		start_vel：定长运动启动速度，单位是unit/ms；
		max_vel：定长运动目标速度，单位是unit/ms；
		end_vel：定长运动结束速度，单位是unit/ms；
		acc：定长运动加速度，单位是unit/ms²；
		dec：定长运动减速度，单位是unit/ms²；
		jerk_acc：定长运动加加速度，单位是unit/ms³；
		jerk_dec：定长运动减减速度，单位是unit/ms³；
		speed_mode：定长运动速度模式，0：T型，1：S型,;
		posi_mode:定长运动位置模式，0相对，1：绝对；
		segment：段号；
返回值：错误代码。
*/
        [DllImport("MCN420.dll", EntryPoint = "YK_static_buf_pmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]

        public static extern Int32 YK_static_buf_pmove(uint CardNo, uint buf_id, uint axis, double axis_dist, double start_vel, double max_vel, double end_vel, double acc, double dec, double jerk_acc, double jerk_dec, uint speed_mode, uint posi_mode, uint isegment);//指令缓冲指定轴点位运动 	
        /*
        功  能：指令缓冲指定轴插补运动；
        参  数：CardNo :控制卡卡号；
                buf_id:缓冲区buf id号;
                axis_num:线性运动轴数量;
                axis_list:线性运动轴号列表；
                axis_dist_list:线性运动轴运行目标位置列表，单位是unit；
                start_vel：线性运动启动速度，单位是unit/ms；
                max_vel：线性运动目标速度，单位是unit/ms；
                end_vel：线性运动结束速度，单位是unit/ms；
                acc：线性运动加速度，单位是unit/ms²；
                dec：线性运动减速度，单位是unit/ms²；
                jerk_acc：线性运动加加速度，单位是unit/ms³；
                jerk_dec：线性运动减减速度，单位是unit/ms³；
                speed_mode：线性运动速度模式，0：T型，1：S型,;
                posi_mode:线性运动位置模式，0相对，1：绝对；
                segment：段号；
        返回值：错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_static_buf_line_move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]

        public static extern Int32 YK_static_buf_line_move(uint CardNo, uint buf_id, uint axis_num, uint[] axis_list, double[] axis_dist_list, double start_vel, double max_vel, double end_vel, double acc, double dec, double jerk_acc, double jerk_dec, uint speed_mode, uint posi_mode, uint segment);//指令缓冲指定轴插补运动



        /*
    功  能：指令缓冲设置指定控制卡的某个输出端口的电平 ; 
    参  数：CardNo:控制卡卡号;
            reset_en:重启指令缓冲区使能，0-关闭，1-启动,;
            powerloss_bufid:掉电重启缓冲区号;
            reset_num：重启指令缓冲区启动次数，0表示一直循环跑，其他值表示启动次数；
    返回值：错误代码。	   
    */

        [DllImport("MCN420.dll", EntryPoint = "YK_static_buf_storage_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 YK_static_buf_storage_enable(uint CardNo, uint reset_en, uint powerloss_bufid, uint reset_num);

        /*
        功  能：指令缓冲检测输入急停；
        参  数：CardNo:控制卡卡号；
                buf_id:缓冲区id号;
                        bitno:输入端口号,取值范围:0~31;
                        on_off:输出电平,0:低电平,1:高电平;        
                        axid:停止轴号;
                segment：段号；
        返回值: 错误代码。
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_static_buf_axis_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]

        public static extern Int32 YK_static_buf_axis_stop(uint CardNo, uint buf_id, uint bitno, uint on_off, uint axis, uint segment);//进入停止指令缓冲状态
        /*
    功  能：设置轴传送带模式；
    参  数：CardNo:控制卡卡号；
            Channel:通道号，取值范围0-1;
            axis:轴号,取值范围:0~7;
            mode:模式使能,0:不使能,1:使能;	
    返回值: 错误代码。
    */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_axis_conveyor_mode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]

        public static extern Int32 YK_set_axis_conveyor_mode(uint CardNo, uint Channel, uint axis, uint mode);//设置轴传送带模式



        /* 
        功  能：同步锁存输出功能参数设置； 
        参  数：CardNo:控制卡卡号； 
                Channel:通道号，取值范围0-1; 
                axis:轴号,取值范围:0~7; 
                enable:同步锁存输出功能使能,0:不使能,1:使能;  
                intput_bit:同步锁存输入位 0-127; 
                lock_mode:锁存模式 0-7; 
                         0:单次锁存上升沿输入锁存逻辑位置； 
                         1:单次锁存下降沿输入锁存逻辑位置 ； 
                         2:单次锁存编码器Z脉冲上升沿锁存逻辑位置； 
                         3:单次锁存编码器 Z脉冲下降沿锁存逻辑位置； 
                         4:单次锁存上升沿输入锁存编码器位置； 
                         5:单次锁存下降沿输入锁存编码器位置 ； 
                         6:单次锁存编码器Z脉冲上升沿锁存编码器位置； 
                         7:单次锁存编码器Z脉冲下降沿锁存编码器位置； 
                cmp_num:同步锁存输出比较通道数; 
                cmp_channel[6]:同步锁存输出io,取值范围:0~15; 
                cmp_pos[6]:同步锁存输出比较通道脉冲间隔 
        返回值: 错误代码。 
        */
        [DllImport("MCN420.dll", EntryPoint = "YK_set_axis_syslock_output", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]

        public static extern Int32 YK_set_axis_syslock_output(uint CardNo, uint Channel, uint axis, uint enable, uint intput_bit, uint lock_mode, uint cmp_num, uint[] cmp_channel, double[] cmp_pos);

    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
