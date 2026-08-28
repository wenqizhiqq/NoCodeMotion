// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System.Runtime.InteropServices;

namespace NoCodeMotion.Services.Hardware.Leadshine
{
    /// <summary>
    /// 雷赛（Leadshine）DMC 系列运动控制卡原生库 LTDMC.dll 的函数声明。
    ///
    /// ★ 这是唯一声明原生函数签名的地方。若你手上的《LTDMC 函数库说明书》版本与此处不一致，
    ///   只需修改本文件对应的一行声明，上层 <see cref="LtdmcCard"/> 与
    ///   <see cref="LeadshineHardwareBridge"/> 都不用改。
    ///
    /// 约定：
    ///   - 原生 WORD  → ushort，DWORD → uint，返回值 short（0 = 成功，非 0 = 错误码）
    ///   - 调用约定 StdCall（雷赛库为 __stdcall）
    ///   - 位数必须匹配：程序编译成 x86 就放 32 位 LTDMC.dll，编译成 x64 就放 64 位 LTDMC.dll，
    ///     否则会报 BadImageFormatException（上层已翻译成中文提示）
    ///   - LTDMC.dll 与其依赖（如 usb 驱动 dll）放在 exe 同目录即可
    /// </summary>
    internal static class LtdmcNative
    {
        /// <summary>原生库文件名。</summary>
        public const string Dll = "LTDMC.dll";

        // ===================== 板卡 =====================

        /// <summary>初始化控制卡。返回卡数量（&gt;0 成功），0 表示没找到卡。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_board_init();

        /// <summary>关闭控制卡，释放资源。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_board_close();

        /// <summary>获取卡信息列表：卡数量、卡类型、卡 ID。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_CardInfList(ref ushort CardNum, uint[] CardTypeList, uint[] CardIdInBitList);

        // ===================== 脉冲当量 / 速度曲线 =====================

        /// <summary>设置脉冲当量（每个单位对应多少脉冲）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_equiv(ushort CardNo, ushort axis, double new_equiv);

        /// <summary>读取脉冲当量。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_equiv(ushort CardNo, ushort axis, ref double equiv);

        /// <summary>设置梯形速度曲线（单位模式）：起始速度、最高速度、加速时间、减速时间、停止速度。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_profile_unit(ushort CardNo, ushort axis,
            double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Stop_Vel);

        /// <summary>设置 S 形曲线平滑时间（s_mode 一般填 0，s_para 为平滑时间秒）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_s_profile(ushort CardNo, ushort axis, ushort s_mode, double s_para);

        // ===================== 单轴运动 =====================

        /// <summary>定长运动（单位模式）。posi_mode：0 相对坐标，1 绝对坐标。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_pmove_unit(ushort CardNo, ushort axis, double Dist, ushort posi_mode);

        /// <summary>连续（Jog）运动。dir：0 负向，1 正向。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_vmove(ushort CardNo, ushort axis, ushort dir);

        /// <summary>停止轴。stop_mode：0 减速停止，1 立即停止。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_stop(ushort CardNo, ushort axis, ushort stop_mode);

        /// <summary>查询轴运动状态：0 运动中，1 已停止。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_check_done(ushort CardNo, ushort axis);

        /// <summary>读取指令位置（单位模式）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern double dmc_get_position_unit(ushort CardNo, ushort axis);

        /// <summary>设置（清零 / 重定义）指令位置（单位模式）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_position_unit(ushort CardNo, ushort axis, double Pos);

        /// <summary>读取编码器位置（单位模式）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern double dmc_get_encpos_unit(ushort CardNo, ushort axis);

        // ===================== 回零 =====================

        /// <summary>
        /// 回零运动（单位模式）。home_mode 取自雷赛手册（常见 0：限位回零，1：原点回零，
        /// 2：原点 + EZ 等），vel_mode 一般 0，EZ_count 一般 0。
        /// </summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_home_move(ushort CardNo, ushort axis,
            ushort home_mode, ushort vel_mode, ushort EZ_count);

        /// <summary>设置回零速度曲线（单位模式）：低速、高速、加速时间、减速时间。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_home_profile_unit(ushort CardNo, ushort axis,
            double Low_Vel, double High_Vel, double Tacc, double Tdec);

        /// <summary>查询回零是否完成：0 未完成，1 完成。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_check_home_done(ushort CardNo, ushort axis);

        // ===================== 伺服使能 / 报警 =====================

        /// <summary>伺服使能输出。on_off：0 使能（低电平有效），1 断开。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_write_sevon_pin(ushort CardNo, ushort axis, ushort on_off);

        /// <summary>读取伺服使能状态。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_read_sevon_pin(ushort CardNo, ushort axis);

        /// <summary>读取伺服报警信号。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_read_alarm_pin(ushort CardNo, ushort axis);

        /// <summary>紧急停止（整卡）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_emg_stop(ushort CardNo);

        // ===================== 通用 IO =====================

        /// <summary>写单个输出位。on_off：0 / 1。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_write_outbit(ushort CardNo, ushort bitno, ushort on_off);

        /// <summary>读单个输出位状态，返回 0 / 1。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_read_outbit(ushort CardNo, ushort bitno);

        /// <summary>读单个输入位，返回 0 / 1。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_read_inbit(ushort CardNo, ushort bitno);

        /// <summary>按端口（32 位一组）读输入。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern uint dmc_read_inport(ushort CardNo, ushort portno);

        /// <summary>按端口（32 位一组）写输出。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_write_outport(ushort CardNo, ushort portno, uint outport_value);
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
