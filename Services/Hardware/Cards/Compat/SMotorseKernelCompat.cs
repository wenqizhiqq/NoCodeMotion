﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Generic;

namespace WenQiZhi.Domain.MotionCard.Common
{
    // ────────────────────────────────────────────────────────────────
    // 应用内核服务兼容层（本工程自行实现，非厂商源码）
    //
    // 卡层的 CardRealization / AxisRealization 会调用 SMotorse 应用框架的
    // CMessage（消息出口）、CConfigurationInfo（硬件状态表查询）与 Resource1。
    // 这三者是 SMotorse 自己的应用内核，不是卡驱动，且依赖它独有的配置/状态模型
    // （CConfigurationInfo 单文件 2586 行，还牵连 CVARreadwrite、CallContext 等），
    // NoCodeMotion 既没有也不应导入。故此处只按卡层实际用到的成员做兼容层。
    //
    // 语义说明（与参考实现「状态表里查不到该键」时的行为完全一致）：
    //   * GetParameter(...) -> isVisual = false
    //       参考实现里卡层这样用：isVisual 为 true 才走虚拟信号，否则读真实硬件。
    //       恒为 false 即「一律读真实硬件」，这正是 NoCodeMotion 应有的行为。
    //   * GetAxisStatus(...) -> -1           （参考实现查不到键时同样返回 -1）
    //   * ReadEstopSignalByHardwareTable() -> false
    //       （参考实现仅在 status==3 或 SoftEStopBtn 为真时返回 true，
    //         NoCodeMotion 本层没有软急停标志，故为 false）
    //   * CMessage 的两个方法转发到 NoCodeMotion 自己的 HardwareLog（真实对接）。
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 消息出口。原为 SMotorse 的报警/提示消息服务，这里转发到 NoCodeMotion 的硬件日志。
    /// </summary>
    public sealed class CMessage
    {
        private static readonly CMessage _instance = new CMessage();

        public static CMessage Instance { get { return _instance; } }

        private CMessage() { }

        /// <summary>提示消息（原：写入报警/提示窗口）。</summary>
        public void AddTipsMessage(DeviceMessageTypeEnum msgType, string message)
        {
            NoCodeMotion.Services.Hardware.HardwareLog.Write("[卡消息/" + msgType + "] " + message);
        }

        /// <summary>异常消息（原：写入报警窗口并记录来源与资源提示）。</summary>
        public void AddExceptionMessage(DeviceMessageTypeEnum msgType, string message,
                                        string methodName, string resourceMessage, bool needStop)
        {
            NoCodeMotion.Services.Hardware.HardwareLog.Write(
                "[卡异常/" + msgType + "] " + message +
                "（位置 " + methodName + "；提示 " + resourceMessage + "）");
        }
    }

    /// <summary>
    /// 硬件状态表 / 参数表查询。原实现维护一张由运动流运行时填充的状态字典；
    /// NoCodeMotion 没有这套表，故按「查不到」返回，使卡层始终走真实硬件路径。
    /// </summary>
    public sealed class CConfigurationInfo
    {
                public enum hardwareEnum
                {
                IO, 气缸, 轴, 运动流项目, 运动流, 软件bug, 前置条件, 中置条件, 后置条件, 前置动作, 中置动作, 后置动作, 轴动作错误
                }

                public enum hardwareObjStatusEnum
                {
                速度,
                轴状态,
                规划位置,
                编码器位置,
                正限位PEL,
                负限位MEL,
                原点,
                急停,
                报警,


                IO状态,

                运动感应到负限位, 运动感应到正限位, 运动感应到软负限位, 运动感应到软正限位,

                运动参数错误, 轴在运动中, 回原点失败, ptp运动失败, jog运动失败, 多轴联动失败, 插补运动失败,

                前置条件出错, 中置条件出错, 后置条件出错, 前置动作出错, 中置动作出错, 后置动作出错,
                运动功能内部错误,

                到动点超时, 到原点超时,


                运动流控制, 运动流项目状态,

                SoftBug,

                条件算子状态, 动作算子状态, 条件算子超时, 运动算子超时,
                直线模块复位失败, 直线模块取料失败, 直线模块放料失败,

                弹夹取放模块复位失败, 弹夹取放模块供料盒取料失败, 弹夹取放模块取料失败, 弹夹取放模块放空盘失败,
                弹夹头模块复位失败, 弹夹头模块取料失败, 弹夹头模块放料失败,
                弹夹物料测试模块复位失败, 弹夹物料测试模块接料失败, 弹夹物料测试模块飞拍失败,

                SAC运控模块复位失败, SAC运控模块动作失败, MQ模块复位失败, MQ模块动作失败,
                倍速链模块复位失败, 倍速链模块动作失败,
                旋转检测模块复位失败, 旋转检测模块动作失败,
                XYZ取放模块复位失败, XYZ取放模块取料失败, XYZ取放模块放料失败

                /*
                运动对象未执行, 运动对象运行中, 运动对象运行结束, 运动对象暂停, 运动对象执行异常,
                前置条件未执行, 中置条件未执行, 后置条件未执行, 前置动作未执行, 中置动作未执行, 后置动作未执行,
                前置条件执行中, 中置条件执行中, 后置条件执行中, 前置动作执行中, 中置动作执行中, 后置动作执行中,
                前置条件执行结束, 中置条件执行结束, 后置条件执行结束, 前置动作执行结束, 中置动作执行结束, 后置动作执行结束,
                前置条件执行超时, 中置条件执行超时, 后置条件执行超时, 前置动作执行超时, 中置动作执行超时, 后置动作执行超时,
                前置条件执行异常, 中置条件执行异常, 后置条件执行异常, 前置动作执行异常, 中置动作执行异常, 后置动作执行异常*/
                }

                public enum HardwareNodeControllMethodEnum
                {
                N = 0, 启动, 停止, 急停, 工位动作完毕后停止
                }

                public struct NodeParameterStruct
                {
                //例如： false, 停止,  轴0，点到点运动, 详细错误信息

                public bool isVisual;

                public HardwareNodeControllMethodEnum controlMethod;

                public string motionSonObject;

                public string motionWayName;

                public string errInfo;
                }

        private static readonly CConfigurationInfo _instance = new CConfigurationInfo();

        public static CConfigurationInfo Instance { get { return _instance; } }

        private CConfigurationInfo() { }

        /// <summary>取硬件节点参数。isVisual=false 表示该节点不使用虚拟信号，应按真实硬件读取。</summary>
        public NodeParameterStruct GetParameter(hardwareEnum name1, string objname, hardwareObjStatusEnum name2)
        {
            return new NodeParameterStruct
            {
                isVisual = false,
                controlMethod = HardwareNodeControllMethodEnum.N,
                motionSonObject = null,
                motionWayName = null,
                errInfo = null
            };
        }

        /// <summary>取硬件节点状态值；无状态表，返回 -1（与参考实现查不到键时一致）。</summary>
        public int GetAxisStatus(string objname, hardwareObjStatusEnum name2)
        {
            return -1;
        }

        /// <summary>读软急停信号；本层无软急停标志，返回 false。</summary>
        public bool ReadEstopSignalByHardwareTable()
        {
            return false;
        }
    }

    /// <summary>
    /// 资源字符串。参考工程用 .resx 生成，卡层只用到 softbugMsg1 一项，故直接内联。
    /// </summary>
    public static class Resource1
    {
        /// <summary>原文取自 ShareData/Resource1.resx。</summary>
        public static string softbugMsg1 { get { return "请联系系统管理员！"; } }
    }

    /// <summary>
    /// 数组转 List 的扩展方法（原 HelperClass/listHelper.cs 同名方法）。
    /// 卡层用 array.toList() 这种小写写法，故保留原名以便移植代码零改动。
    /// </summary>
    public static class listHelper
    {
        public static List<T> toList<T>(this T[] data)
        {
            return data == null ? new List<T>() : new List<T>(data);
        }
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
