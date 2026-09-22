﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: CIO/Extend/IEIOInPut.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
namespace Samsun.Domain.MotionCard.Common
{
    /// <summary>
    /// 外部输入IO接口
    /// </summary>
    public interface IEIOInPut
    {

        /// <summary>
        /// IO的初始电平为高电平
        /// </summary>
        bool EDefaultLevelIsHigh { get; set; }
        /// <summary>
        /// 硬件卡拥有的输入IO的数量
        /// </summary>
        ushort EInIONums { get; set; }
        /// <summary>
        /// 输入IO所属卡的名字
        /// </summary>
        string EInIOName { get; set; }

        /// <summary>
        /// 卡号（即这个输入IO对象是属于第几张卡的）
        /// </summary>
        int ECardNo { get; set; }


        /// <summary>
        /// DMC_3000_设置 扩展IO 通讯状态
        /// <para>注 意：</para>
        /// <para>1）当关闭运动控制卡时，CAN 通讯不会被自动断开；当再次初始化运动控制卡时， CAN-IO 通讯依然保持之前的状态；</para>
        /// <para>2）当连接 CAN 通讯时，必须使用 nmc_get_can_state 函数读取 CAN-IO 的通讯状态，确认 CAN 通讯已正常连接。当连接出现异常时，可再次调用 nmc_set_can_state函数进行连接；</para>
        /// <para>3）设置波特率时需保证控制卡波特率与模块波特率相对应；</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeNum">CAN 节点数，取值范围：1~8</param>
        /// <param name="state">设置通讯状态，0：断开，1：连接</param>
        /// <param name="baud">设置控制卡波特率：波特率参数：0,1,2,3,4,5；对特率：1000Kbps，800Kbps,500Kbps,250Kbps,125Kbps,100Kbps</param>
        /// <returns>返回值：错误代码</returns>
        int ESetExpandIOConnectState(int CardNo, int NodeNum, int state, int baud);

        /// <summary>
        /// DMC_3000_读取 扩展IO 通讯状态
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="NodeNum"></param>
        /// <param name="state"></param>
        /// <param name="baud"></param>
        /// <returns>返回值：错误代码</returns>
        int EGetExpandIOConnectState(int CardNo, ref ushort NodeNum, ref ushort state);

        /// <summary>
        /// DMC_3000_读取指定 CAN-IO 扩展模块的某个输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="IoBit">输出口号</param>
        /// <param name="IoValue">输出电平，0：低电平，1：高电平</param>
        /// <returns>返回值：错误代码</returns>
        int EGetExpandIOInBit(int CardNo, int NodeID, int IoBit, ref ushort IoValue);

        /// <summary>
        /// DMC_3000_读取指定 CAN-IO 扩展模块的输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">端口号，0 表示 0~31 号口，1 表示 32~63 号口</param>
        /// <param name="IoValue">输出值，bit0~bit31 的值分别代表第 0~31 号输出口的电平</param>
        /// <returns>返回值：错误代码</returns>
        int EGetExpandIOPortNoInBit(int CardNo, int NodeID, int PortNo, ref uint IoValue);

        /// <summary>
        /// DMC_3000_读取指定 CAN-ADDA 扩展模块的某个输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">输出端口号</param>
        /// <param name="IoValue">输出电平，单位：mV/mA</param>
        /// <returns>返回值：错误代码</returns>
        int EGetExpandADDAIOPortNoInBit(int CardNo, int NodeID, int PortNo, ref double IoValue);

        /// <summary>
        ///  DMC_3000_设置指定 CAN-ADDA 扩展模块的某个输入端口的模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">输入端口号</param>
        /// <param name="mode">输入模式，0：电压模式，1：电流模式</param>
        /// <param name="buffer_nums">保留参数</param>
        /// <returns>返回值：错误代码</returns>
        int ESetExpandADDAIOInPortNoMode(int CardNo, int NodeID, int PortNo, int mode, uint buffer_nums);

        /// <summary>
        ///  DMC_3000_读取指定 CAN-ADDA 扩展模块的某个输入端口的模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="NodeID">CAN 节点号，取值范围：1~8</param>
        /// <param name="PortNo">输入端口号</param>
        /// <param name="mode">输入模式，0：电压模式，1：电流模式</param>
        /// <param name="buffer_nums">保留参数</param>
        /// <returns>返回值：错误代码</returns>
        int EGetExpandADDAIOInPortNoMode(int CardNo, int NodeID, int PortNo, ref ushort mode, uint buffer_nums);

        /// <summary>
        /// DMC_3000_保存模式设置到模块 FLASH
        /// <para>注 意：</para>
        /// <para>1）保存后模块会断开连接，需要重新连接才能进行正常控制。</para>
        /// <para>2）设置的模式会断电保存，上电后电压或电流模式为最后一次断电前设置的模式。</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="PortNum">保留参数，设为 0</param>
        /// <param name="NodeNum">节点号，1-8</param>
        /// <returns>返回值：错误代码</returns>
        int ESaveExpandIoToFlash(int CardNo, int PortNum, int NodeNum);
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
