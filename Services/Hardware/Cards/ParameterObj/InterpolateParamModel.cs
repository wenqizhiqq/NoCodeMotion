﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionParameterObj/InterpolateParamModel.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System.Collections.Generic;

namespace Samsun.Domain.MotionCard.Common
{

    /// <summary>
    /// 插补运动参数
    /// </summary>
    public class InterpolateMotetionParamModel
    {
        /// <summary>
        /// 卡号
        /// </summary>
        public int CardNo { get; set; }

        /// <summary>
        /// 坐标系号，取值范围：0~5
        /// </summary>
        public int Crd { get; set; }

          /// <summary>
        /// 指令队列缓存区
        /// </summary>
        public int FIFO { get; set; }

        /// <summary>
        /// 轴号数组
        /// </summary>
        public AxisParamModel[] Axis { get; set; }

        /// <summary>
        /// 速度曲线S/T
        /// </summary>
        public int SMode { get; set; }

        /// <summary>
        /// S 段时间，单位：s；范围：0~1?
        /// </summary>
        public int SPara { get; set; }

        /// <summary>
        /// 合成起始速度，单位：unit/s
        /// </summary>
        public int MinVel { get; set; }

        /// <summary>
        /// 合成最大速度，单位：unit/s
        /// </summary>
        public int MaxVel { get; set; }

        /// <summary>
        /// 减速时间，单位：s
        /// </summary>
        public int TdecTime { get; set; }

        /// <summary>
        /// 加速时间，单位：s
        /// </summary>
        public int TaccTime { get; set; }

        /// <summary>
        /// 合成停止速度，单位：unit/s
        /// </summary>
        public int StopVel { get; set; }




        /// <summary>
        /// 比较功能状态，0：禁止，1：使能
        /// </summary>
        public int Enable { get; set; }

        /// <summary>
        /// 设置前瞻 未知参数
        /// </summary>
        public int LookaheadSegments { get; set; }

        /// <summary>
        /// 设置前瞻 未知参数
        /// </summary>
        public int PathError { get; set; }

        /// <summary>
        /// 设置前瞻 未知参数
        /// </summary>
        public int LookaheadAcc { get; set; }

        /// <summary>
        /// 前瞻比较次数
        /// </summary>
        public int Numbers { get; set; }

        /// <summary>
        /// 前瞻模型数组结构
        /// </summary>
        public CrdBlockData LookAheadBuf { get; set; }
    }
    #region 前瞻模型
    public struct CrdBlockData
    {
        public short iMotionType;                             // 运动类型,0为直线插补,1为2D圆弧插补,2为3D圆弧插补,6为IO,7为延时，8位DAC
        public short iCirclePlane;                            // 圆弧插补的平面;XY—1，YZ-2，ZX-3
        public short arcPrmType;                               // 1-圆心表示法；2-半径表示法
        [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I4)]
        public int[] lPos;            // 当前段各轴终点位置

        public double dRadius;                                // 圆弧插补的半径
        public short iCircleDir;                             // 圆弧旋转方向,0:顺时针;1:逆时针
        [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = System.Runtime.InteropServices.UnmanagedType.R8)]
        public double[] dCenter;                             // 2维圆弧插补的圆心相对坐标值，即圆心相对于起点位置的偏移量
                                                             // 3维圆弧插补的圆心在用户坐标系下的坐标值
        public int height;                                 // 螺旋线的高度
        public double pitch;    // 螺旋线的螺距
                                //double[3]
        [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = System.Runtime.InteropServices.UnmanagedType.R8)]
        public double[] beginPos;
        //double[3]
        [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = System.Runtime.InteropServices.UnmanagedType.R8)]
        public double[] midPos;
        //double[3]
        [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = System.Runtime.InteropServices.UnmanagedType.R8)]
        public double[] endPos;
        //double[3][3]
        [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 9, ArraySubType = System.Runtime.InteropServices.UnmanagedType.R8)]
        public double[] R_inv;
        //double[3][3]
        [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 9, ArraySubType = System.Runtime.InteropServices.UnmanagedType.R8)]
        public double[] R;

        public double dVel;                                   // 当前段合成目标速度
        public double dAcc;                                   // 当前段合成加速度
        public short loop;
        public short iVelEndZero;                             // 标志当前段的终点速度是否强制为0,值0——不强制为0;值1——强制为0
        public CrdBufOperation operation;
        public double dVelEnd;                                // 当前段合成终点速度
        public double dVelStart;                              // 当前段合成的起始速度
        public double dResPos;                                // 当前段合成位移量
    }
    public struct CrdBufOperation
    {
        public ushort delay;                         // 延时时间
        public short doType;                        // 缓存区IO的类型,0:不输出IO
        public ushort doAddress;                     // IO模块地址
        public ushort doMask;                        // 缓存区IO的输出控制掩码
        public ushort doValue;                       // 缓存区IO的输出值
        public short dacChannel;                     // DAC输出通道
        public short dacValue;                       // DAC输出值
        [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = System.Runtime.InteropServices.UnmanagedType.U2)]
        public ushort[] dataExt;               // 辅助操作扩展数据
    }
    #endregion

    /// <summary>
    /// 直线插补参数
    /// </summary>
    public class LineInterpolateparamModel
    {
        /// <summary>
        /// 卡号
        /// </summary>
        public int CardNo { get; set; }

        /// <summary>
        /// 坐标系号，取值范围：0~5
        /// </summary>
        public int Crd { get; set; }

        /// <summary>
        /// 指令队列缓存区
        /// </summary>
        public int FIFO { get; set; }

        /// <summary>
        /// 轴号数组
        /// </summary>
        public int[] Axis { get; set; }


        /// <summary>
        /// 目标位置列表，单位：unit 
        /// </summary>
        public double[] TargetPos { get; set; }


        /// <summary>
        /// 运动模式，0：相对运动，1：绝对运动
        /// </summary>
        public int PosiMode { get; set; }


        /// <summary>
        /// 保留参数，固定值为 0
        /// </summary>
        public double minVel { get; set; }
        /// <summary>
        /// 合成最大速度，单位：pulse/
        /// </summary>
        public double maxVel { get; set; }
        /// <summary>
        /// 加减速时间，单位：s（最小值为 0.001s）
        /// </summary>
        public double tacc { get; set; }
        /// <summary>
        /// 保留参数，固定值为 0
        /// </summary>
        public double tdec { get; set; }
        /// <summary>
        /// S段 
        /// </summary>
        public double s_Param { get; set; }
        /// <summary>
        /// 保留参数，固定值为 0
        /// </summary>
        public double stopVel { get; set; }

        /// <summary>
        /// 插补模式  0：单次模式插补直接完成  1：多种模式插补需要再次调用一个连续插补的start标志进行完成
        /// </summary>
        public int ContiMode { get; set; } = 0;

    }

    /// <summary>
    /// 圆弧插补参数
    /// </summary>
    public class ArcInterpolateparamModel
    {

        /// <summary>
        /// 卡号
        /// </summary>
        public int CardNo { get; set; }


        /// <summary>
        /// 坐标系号，取值范围：0~5
        /// </summary>
        public int Crd { get; set; }

        /// <summary>
        /// 指令队列缓存区
        /// </summary>
        public int FIFO { get; set; }


        /// <summary>
        /// 轴号数组
        /// </summary>
        public int[] Axis { get; set; }


        /// <summary>
        /// 目标位置列表，单位：unit 
        /// </summary>
        public int[] TargetPos { get; set; }

        /// <summary>
        /// 运动模式，0：相对运动，1：绝对运动
        /// </summary>
        public int PosiMode { get; set; }

        /// <summary>
        /// /圆心位置，单位：unit
        /// </summary>
        public int[] CenPos { get; set; }

        /// <summary>
        /// 圆弧方向，0：顺时针，1：逆时针
        /// </summary>
        public int ArcDir { get; set; }

        /// <summary>
        /// 圈数：自然数：表示此时执行的为螺旋线插补,螺旋线的圈数
        /// </summary>
        public int CircleCounts { get; set; }

        /// <summary>
        /// 圆弧半径值，单位：unit  
        /// </summary>
        public int ArcRadius { get; set; }

        /// <summary>
        /// 中间位置数组，单位：unit
        /// </summary>
        public int[] MidPos { get; set; }

        /// <summary>
        /// 末点位置数组，单位：unit
        /// </summary>
        public int EndPos { get; set; }

        /// <summary>
        /// 运行最大速度
        /// </summary>
        public double MaxSpeed { get; set; } = 200;

        /// <summary>
        /// 停止速度
        /// </summary>
        public double StopSpeed { get; set; } = 50;

        /// <summary>
        /// 加减速
        /// </summary>
        public double Tacc { get; set; } = 0.5;

        /// <summary>
        /// 插补模式
        /// </summary>
        public int ContiMode { get; set; } = 0;

    }


    #region  插补参数模型

    /*
    /// <summary>
    /// 插补参数
    /// </summary>
    public class InterpolateparamModel
    {
        /// <summary>
        /// 坐标系号，取值范围：0~5
        /// </summary>
        public int Crd { get; set; }

        /// <summary>
        /// 轴号数组
        /// </summary>
        public AxisParameModel[] Axis { get; set; }

        /// <summary>
        /// 目标位置列表，单位：unit 
        /// </summary>
        public int[] TargetPos { get; set; }

        /// <summary>
        /// 运动模式，0：相对运动，1：绝对运动
        /// </summary>
        public int PosiMode { get; set; }

        /// <summary>
        /// 速度曲线S/T
        /// </summary>
        public int SMode { get; set; }

        /// <summary>
        /// S 段时间，单位：s；范围：0~1?
        /// </summary>
        public int SPara { get; set; }

        /// <summary>
        /// 合成起始速度，单位：unit/s
        /// </summary>
        public int MinVel { get; set; }

        /// <summary>
        /// 合成最大速度，单位：unit/s
        /// </summary>
        public int MaxVel { get; set; }

        /// <summary>
        /// 减速时间，单位：s
        /// </summary>
        public int TdecTime { get; set; }

        /// <summary>
        /// 加速时间，单位：s
        /// </summary>
        public int TaccTime { get; set; }

        /// <summary>
        /// 合成停止速度，单位：unit/s
        /// </summary>
        public int StopVel { get; set; }

        /// <summary>
        /// /圆心位置，单位：unit
        /// </summary>
        public int[] Cen_Pos { get; set; }

        /// <summary>
        /// 圆弧方向，0：顺时针，1：逆时针
        /// </summary>
        public int Arc_Dir { get; set; }

        /// <summary>
        /// 圈数：自然数：表示此时执行的为螺旋线插补,螺旋线的圈数
        /// </summary>
        public int CircleCounts { get; set; }

        /// <summary>
        /// 圆弧半径值，单位：unit  
        /// </summary>
        public int Arc_Radius { get; set; }

        /// <summary>
        /// 中间位置数组，单位：unit
        /// </summary>
        public int Mid_Pos { get; set; }

        /// <summary>
        /// 末点位置数组，单位：unit
        /// </summary>
        public int End_Pos { get; set; }

        /// <summary>
        /// 比较功能状态，0：禁止，1：使能
        /// </summary>
        public int Enable { get; set; }

        /// <summary>
        /// 设置前瞻 未知参数
        /// </summary>
        public int LookaheadSegments { get; set; }

        /// <summary>
        /// 设置前瞻 未知参数
        /// </summary>
        public int PathError { get; set; }

        /// <summary>
        /// 设置前瞻 未知参数
        /// </summary>
        public int LookaheadAcc { get; set; }

    }
     */
    #endregion

}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
