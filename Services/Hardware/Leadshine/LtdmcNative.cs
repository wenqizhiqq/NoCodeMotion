// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志⁣◆‍编​写​◇‍微​信‌﹕‌1⁠8​7⁠◆‌1‍9‏3⁠6​◇⁣1‌3⁣9⁠9‏　⁣※⁣保‏留​所‎有‍权⁠利⁠请‎勿⁠删‍除⁣◇​⁣​
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
    ///
    /// 【两套函数族，千万别混用】
    ///   - <c>dmc_*</c>：板卡本地资源。**脉冲型卡**（DMC2410 / DMC1000 等）的轴与 IO 都走这一族，
    ///     轴号是「卡上第几路」，IO 是「卡上第几位」，伺服使能是 dmc_write_sevon_pin 这根使能脚。
    ///   - <c>nmc_*</c>：总线主站资源。**EtherCAT / CANopen 总线卡**（DMC-E 3000/5000 系列）的轴与
    ///     从站 IO 必须走这一族：IO 按「从站节点号 + 站内位号」寻址，伺服使能必须用
    ///     nmc_set_axis_enable（总线伺服没有 dmc_write_sevon_pin 这种使能脚，走的是 CiA402 状态机）。
    ///
    ///   拿 dmc_* 去驱动总线卡，典型现象就是**函数返回 0（成功）但轴不动、IO 不动作** ——
    ///   卡根本不去看那些本地寄存器。所以上层要先判断卡属于哪一族（见 LtdmcCard.IsBusCard）。
    ///
    /// 【本文件与官方例程 LTDMC.cs 的差异（都是按例程改正过的）】
    ///   1. <c>dmc_get_position_unit</c>：真签名是 (卡号, 轴号, ref double pos)，**不是** double 返回值。
    ///      声明成返回值会让原生函数往一个野指针写 8 字节 —— 轻则位置读出垃圾，重则崩溃。
    ///   2. <c>dmc_home_move</c>：真签名只有 (卡号, 轴号) 两个参数。
    ///   3. <c>dmc_get_CardInfList</c>：第 3 个参数是 <c>ushort[]</c>（卡号列表），不是 uint[]。
    ///   4. <c>dmc_get_encpos_unit</c> / <c>dmc_check_home_done</c> / <c>dmc_read_alarm_pin</c>
    ///      在 LTDMC.dll 里**没有导出**，调用必抛 EntryPointNotFoundException，已删除；
    ///      回零完成状态改用 <c>dmc_get_home_result</c>。
    /// </summary>
    internal static class LtdmcNative
    {
        /// <summary>原生库文件名。</summary>
        public const string Dll = "LTDMC.dll";

        /// <summary>总线端口号：EtherCAT 固定用 2（官方例程里 nmc_get_errcode(card, 2, …) 就是这么传的）。</summary>
        public const ushort EtherCatPort = 2;

        // ===================== 板卡 =====================

        /// <summary>初始化控制卡。返回卡数量（&gt;0 成功），0 表示没找到卡。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_board_init();

        /// <summary>关闭控制卡，释放资源。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_board_close();

        /// <summary>硬件复位（重新枚举卡）。复位后需要重新调用 dmc_board_init。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_board_reset();

        /// <summary>软件复位指定卡。复位后需要重新调用 dmc_board_init。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_soft_reset(ushort CardNo);

        /// <summary>
        /// 获取卡信息列表：卡数量、卡类型、卡 ID。
        /// ★ 第 3 个参数是 <c>ushort[]</c>（卡号列表）。声明成 uint[] 会让原生写入的
        /// 两个相邻 ushort 被当成一个 uint 读回来，卡号全是垃圾值。
        /// </summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_CardInfList(ref ushort CardNum, uint[] CardTypeList, ushort[] CardIdList);

        /// <summary>卡上总轴数。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_total_axes(ushort CardNo, ref uint TotalAxis);

        /// <summary>卡上总输入 / 输出点数。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_total_ionum(ushort CardNo, ref ushort TotalIn, ref ushort TotalOut);

        /// <summary>卡上总 AD / DA 通道数。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_total_adcnum(ushort CardNo, ref ushort TotalIn, ref ushort TotalOut);

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

        /// <summary>在线变速：新的运行速度与变速时间。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_change_speed_unit(ushort CardNo, ushort axis, double New_Vel, double Taccdec);

        /// <summary>在线变位：运动过程中改目标位置。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_reset_target_position_unit(ushort CardNo, ushort axis, double New_Pos);

        /// <summary>读取当前速度（单位模式）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_read_current_speed_unit(ushort CardNo, ushort axis, ref double current_speed);

        /// <summary>设置减速停止时间（秒）。影响限位触发 / 减速停止的减速过程，防撞机。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_dec_stop_time(ushort CardNo, ushort axis, double stop_time);

        /// <summary>设置脉冲输出模式（0=脉冲+方向，1=双脉冲，2=CW/CCW …，以手册为准）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_pulse_outmode(ushort CardNo, ushort axis, ushort outmode);

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

        /// <summary>查询轴运动状态：0 运动中，1 已停止（到位）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_check_done(ushort CardNo, ushort axis);

        /// <summary>
        /// 读取指令位置（单位模式）。
        /// ★ 必须带 <c>ref double</c> 出参 —— 官方例程里就是
        /// <c>LTDMC.dmc_get_position_unit(card, axis, ref pos)</c>。
        /// </summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_position_unit(ushort CardNo, ushort axis, ref double pos);

        /// <summary>
        /// 读取编码器反馈位置（单位模式）。官方例 1 用它对指令位置做闭环对比：
        /// 指令位置在走、编码器不动 → 电机没使能 / 动力线没接 / 编码器线松。
        /// </summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_encoder_unit(ushort CardNo, ushort axis, ref double pos);

        /// <summary>设置（清零 / 重定义）指令位置（单位模式）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_position_unit(ushort CardNo, ushort axis, double Pos);

        /// <summary>读取轴 IO 状态位（bit0 负限位 / bit1 正限位 / bit2 原点 / bit3 EZ / bit4 伺服报警 / bit5 急停，以手册为准）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern uint dmc_axis_io_status(ushort CardNo, ushort axis);

        /// <summary>读取轴停止原因（配合 dmc_clear_stop_reason 用）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_stop_reason(ushort CardNo, ushort axis, ref int StopReason);

        /// <summary>清除轴停止原因。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_clear_stop_reason(ushort CardNo, ushort axis);

        // ===================== 回零 =====================

        /// <summary>
        /// 启动回零。★ 只有 (卡号, 轴号) 两个参数 —— 回零模式 / 速度要先通过
        /// <see cref="dmc_set_home_profile_unit"/> 下发。
        /// </summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_home_move(ushort CardNo, ushort axis);

        /// <summary>设置回零速度曲线（单位模式）：低速、高速、加速时间、减速时间。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_set_home_profile_unit(ushort CardNo, ushort axis,
            double Low_Vel, double High_Vel, double Tacc, double Tdec);

        /// <summary>
        /// 查询回零结果：state = 1 表示回零成功。
        /// （LTDMC.dll 里没有 dmc_check_home_done，回零完成状态就靠这个函数。）
        /// </summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_get_home_result(ushort CardNo, ushort axis, ref ushort state);

        // ===================== 伺服使能 / 报警（脉冲卡） =====================

        /// <summary>伺服使能输出。on_off：0 使能（低电平有效），1 断开。仅脉冲卡用。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_write_sevon_pin(ushort CardNo, ushort axis, ushort on_off);

        /// <summary>读取伺服使能状态。仅脉冲卡用。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_read_sevon_pin(ushort CardNo, ushort axis);

        /// <summary>紧急停止（整卡）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short dmc_emg_stop(ushort CardNo);

        // ===================== 通用 IO（脉冲卡：按位号寻址） =====================

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

        // =================================================================
        // 总线卡（EtherCAT / CANopen 主站，nmc_ 族）
        //   端口号固定用 EtherCatPort(=2)；IO 寻址是「从站节点号 + 站内位号」，
        //   不是脉冲卡那种「整卡第几位」。
        // =================================================================

        // —— 卡级状态 ——

        /// <summary>总线卡上的总轴数（总线伺服轴，按从站顺序编号）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_total_axes(ushort CardNo, ref uint TotalAxis);

        /// <summary>指定端口上的从站总数。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_total_slaves(ushort CardNo, ushort PortNum, ref ushort TotalSlaves);

        /// <summary>读取总线错误码。errcode == 0 表示总线正常。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_errcode(ushort CardNo, ushort channel, ref ushort errcode);

        /// <summary>清除总线错误码。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_clear_errcode(ushort CardNo, ushort channel);

        /// <summary>总线卡上的总输入 / 输出点数。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_total_ionum(ushort CardNo, ref ushort TotalIn, ref ushort TotalOut);

        /// <summary>总线卡上的总 AD / DA 通道数。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_total_adcnum(ushort CardNo, ref ushort TotalADIn, ref ushort TotalDAOut);

        /// <summary>设置总线通讯周期（微秒）。例程里用 nmc_set_cycletime(card, 2, 1000)。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_set_cycletime(ushort CardNo, ushort FieldbusType, int CycleTime);

        /// <summary>
        /// 读取总线轴状态机（CiA402）：
        /// 0 未启动 / 1 启动禁止 / 2 准备启动 / 3 启动 / 4 操作使能 / 5 停止 / 6 错误触发 / 7 错误。
        /// 轴不动时先看这个值：不是 4 就说明伺服没真正使能。
        /// </summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_axis_state_machine(ushort CardNo, ushort axis, ref ushort Axis_StateMachine);

        /// <summary>读取总线轴错误码（伺服自身报警码）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_axis_errcode(ushort CardNo, ushort axis, ref ushort Errcode);

        /// <summary>清除总线轴错误码。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_clear_axis_errcode(ushort CardNo, ushort axis);

        /// <summary>读取总线轴的输入 IO 位图。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_axis_io_in(ushort CardNo, ushort axis);

        /// <summary>读取总线轴的输出 IO 位图。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_axis_io_out(ushort CardNo, ushort axis);

        /// <summary>
        /// 读取总线轴所在的从站节点地址（SlaveAddr = 从站号，Sub_SlaveAddr = 子站号）。
        /// 官方例 18 用它核对「轴 ↔ 从站」的对应关系；配总线 IO 的节点号时也按这个值填。
        /// </summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_get_axis_node_address(ushort CardNo, ushort axis, ref ushort SlaveAddr, ref ushort Sub_SlaveAddr);

        // —— 轴使能 / 回零 ——

        /// <summary>
        /// 使能总线伺服轴（CiA402 走 Operation Enabled）。
        /// ★ 总线卡必须用这个，不能用 dmc_write_sevon_pin —— 总线伺服没有本地使能脚。
        /// </summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_set_axis_enable(ushort CardNo, ushort axis);

        /// <summary>失能总线伺服轴。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_set_axis_disable(ushort CardNo, ushort axis);

        /// <summary>设置总线轴运行模式（1 位置 / 3 速度 / 4 力矩 / 6 回零 …，以手册为准）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_set_axis_run_mode(ushort CardNo, ushort axis, ushort run_mode);

        /// <summary>设置总线轴回零参数：模式、低速、高速、加速时间、减速时间、零点偏移。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_set_home_profile(ushort CardNo, ushort axis, ushort home_mode,
            double Low_Vel, double High_Vel, double Tacc, double Tdec, double offsetpos);

        /// <summary>启动总线轴回零。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_home_move(ushort CardNo, ushort axis);

        /// <summary>设置总线轴零点偏移。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_set_offset_pos(ushort CardNo, ushort axis, double offset_pos);

        // —— 从站 IO（按「节点号 + 站内位号」寻址） ——

        /// <summary>读从站输入位。NodeID = 从站节点号，IoBit = 该从站内的位号。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_read_inbit(ushort CardNo, ushort NodeID, ushort IoBit, ref ushort IoValue);

        /// <summary>读从站输出位。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_read_outbit(ushort CardNo, ushort NodeID, ushort IoBit, ref ushort IoValue);

        /// <summary>写从站输出位。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_write_outbit(ushort CardNo, ushort NodeID, ushort IoBit, ushort IoValue);

        /// <summary>按端口读从站输入（32 位一组）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_read_inport(ushort CardNo, ushort NodeID, ushort PortNo, ref uint IoValue);

        /// <summary>按端口读从站输出（32 位一组）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_read_outport(ushort CardNo, ushort NodeID, ushort PortNo, ref uint IoValue);

        /// <summary>按端口写从站输出（32 位一组）。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern short nmc_write_outport(ushort CardNo, ushort NodeID, ushort PortNo, uint IoValue);
    }
}

// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
