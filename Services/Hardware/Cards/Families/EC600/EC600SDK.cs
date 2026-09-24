﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 WenQiZhiMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/EC600/EC600SDK.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WenQiZhi.Domain.MotionCard.Common.EC600_EtherCAT
{
    public class EC600SDK
    {

        public bool IsInit { get; set; }

        private static EC600SDK _instance;
        public static EC600SDK Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new EC600SDK();
                return EC600SDK._instance;
            }
            set { EC600SDK._instance = value; }
        }


        /// <summary>
        /// 设置主板上输入IO的端口的电平（虚拟运动卡专用）
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="bitno"></param>
        /// <param name="on_off">输入电平，0：低电平，1：高电平</param>
        /// <returns></returns>
        public int SetCardWriteInBit(int CardNo, int bitno, int on_off)
        {
            return 0;
        }


        /*
        错误代码 可能错误原因
        0x0000 没有错误
        0x0009 同步时钟丢失，第一个从站丢失
        0x000E 总线初始化不成功，总线未连接
        0x0010 总线初始化连接超时
        0x0013 SDO 操作失败
        0x0014 无效的 SDO 指令
        0x001E 从站丢失
        0x001F 读取配置文件错误，xml 文件有问题
        0x0020 总线无法切换到 op 状态，总线连接有问
        题,可能的原因是总线没有下载配置文件
        0x0022 从站的状态切换寄存器与其配置的xml不
        匹配，导致读写从站寄存器失败
        0x0024
        从站错误，可能的原因是从站无法切换状
        态机，查找从站错误码信息,即读取
        0x603F 地址
        0x0026
        总线网络连接有问题，可能网线有干扰，
        导致丢数据包
        0x0027
        同步时钟丢失，网络时钟不稳定，1.该现
        象可能在总线连接过程中出现，可能运动
        中程序负载太大导致，可增加总线周期调
        试；2.网络干扰大导致
        0x002A 从站无法切换到 OP 状态
        0x002D 网络连接线已连接
        0x0031 从站不支持 SDO 操作
        0x0041 读写 SDO 超时
        0x0047 该地址不支持 SDO 操作
        0x0048 该地址不可读，只能写操作
        0x0049 该地址不可写，只能读操作
        0x004A 对象在对象字典中不存在
        0x004B 不能映射为 PDO 对象
        0x017B 网线断开,可能网线插错，入口与出口不匹
        配
        0x01C2 总线的时钟发生较大的跳动，可能总线周
        期太小导致
        0x0206 SDO 写操作失败
         DMC-E3032 V2.0 运动控制卡用户使用手册
        — 148 —
        0x0207 SDO 读操作失败
        0x020C 从站有报警出现，需求查找具体的从站错
        误码地址 0x603F
        0x020D 从站地址无法收到数据，可能掉线等
        0x0224 网络掉线，或者新加入了从站，该从站没
        有进入 OP 状态
        0x0225 网络接线重新连上
        0x0226 网络掉线，或者没有连接网线
        0x0227 从站掉线
        0x0229 从站状态状态错误，从站 AL-Status 寄存
        器错误，可能网络中网线被拔出后又插上
        0x022B 无法连接从站地址
        其它的为错误码位乱码，是总线没有建立
        （即主站没有连接从站）；尤其是当控制
        卡中存有配置文件，而没有连接任何从站
        时常出现。







        ERR_NOERR 0 成功
        ERR_UNKNOWN 1 总线初始化不成功
        ERR_PARAERR 2 参数错误
        ERR_TIMEOUT 3 通讯超时
        ERR_CONTROLLERBUSY 4 控制器忙、控制器相应轴处于运动中
        ERR_CONNECT_TOOMANY 5 链接太频繁
        ERR_CANNOT_CONNECTETH
        8 网络链接失败，请查看连线及通讯参
        数
        ERR_HANDLEERR 9 句柄错误，网络未链接或链接已断开
        ERR_SENDERR 10 发送失败，网络未链接或链接已断开
        ERR_FIRMWAREERR 12 固件文件错误
        ERR_FIRMWAR_MISMATCH 14 固件不匹配
        ERR_CARD_NOT_SUPPORT 17 不支持的功能
        ERR_FIRMWARE_INVALID_PAR
        A
        20 固件参数错误
        ERR_FIRMWARE_STATE_ERR 22 固件当前状态不允许操作
        ERR_FIRMWARE_CARD_NOT_S
        UPPORT
        24 固件不支持的功能
        ERR_PASSWORD_ERR 25 密码错误
        ERR_PASSWORD_TIMES_OUT 26 密码错误输入次数受限
        ERR_AXIS_SEL_ERR 30 手轮脉冲的轴档位选择超出范围（软
         DMC-E3032 V2.0 运动控制卡用户使用手册
        — 149 —
        件控制模式）
        ERR_HAND_AXIS_NUM_ERR 31 手轮脉冲的轴映射数量超出范围
        ERR_AXIS_RATIO_ERR
        32 手轮脉冲的倍率档位选择超出范围
        （软件控制模式）
        ERR_HANDWH_START
        33 已进入手轮脉冲模式，不能切换软硬
        件控制模式
        ERR_AXIS_BUSY_STATE 34 轴已在运动，不能切换到手轮模式
        ERR_LIST_NUM_ERR 50 LIST 号超出范围
        ERR_LIST_NOT_OEPN 51 LIST 没有初始化
        ERR_PARA_NOT_VALID 52 参数不在有效范围
        ERR_LIST_HAS_OPEN 53 LIST 已经打开
        ERR_MAIN_LIST_NOT_OPEN 54 LIST 没有初始化
        ERR_AXIS_NUM_ERR 55 轴数不在有效范围
        ERR_AXIS_MAP_ARRAY_ERR 56 轴映射表为空
        ERR_MAP_AXIS_ERR 57 映射轴错误
        ERR_MAP_AXIS_BUSY 58 映射轴忙
        ERR_PARA_SET_FORBIT 59 运动中不允许更改参数
        ERR_FIFO_FULL 60 缓冲区已满
        ERR_RADIUS_ERR 61 半径为 0 或小于两点的距离的一半
        ERR_MAINLIST_HAS_START 62 LIST 已经启动
        ERR_ACC_TIME_ZERO 63 加减速时间为 0
        ERR_MAINLIST_NOT_START 64 主要 LIST 没有启动
        ERR_POINT_SAME_ON_RADIUS
        67 圆弧插补在半径模式下起点和终点
        不能重合
        MCVP_SMOOTH_TIME_SET_ER
        ROR
        70 S 曲线加减速模式，平滑时间为零出
        错
        MCVP_START_VEL_SET_ERROR 71 起跳速度小于零
        MCVP_STEADY_VEL_SET_ERR
        OR
        72 最大速度小于等于零
        MCVP_END_VEL_SET_ERROR 73 终点速度小于零
        MCVP_TOTAL_LENGTH_SET_E
        RROR
        74 规划长度小于等于零
        MCVP_EVEN_TIME_ERROR 75 最小匀速时间小于零
        MCVP_PLAN_MODE_SET_ERRO
        R
        76 规划模式非 T 型非 S 型
        MCVP_ACC_SET_ERROR 77 加速时间等于零
        MCVP_DEC_SET_ERROR 78 减速时间等于零
        MCVP_PLAN_ERROR
        79 规划长度小于等于零（速度规划函数
        返回，74 为长度计算时返回）
        MCVP_SMOOTH_TIME_SET_ER
        ROR
        80
        s 时间设置错误(小于等于 0)
        MCVP_START_VEL_SET_ERROR 81 起始速度绝对值设置错误(小于 0)
        MCVP_STEADY_VEL_SET_ERR
        OR
        82 最大速度绝对值设置错误(小于等于
        0)
         DMC-E3032 V2.0 运动控制卡用户使用手册
        — 150 —
        MCVP_END_VEL_SET_ERROR 83 停止速度绝对值设置错误(小于 0)
        MCVP_TOTAL_LENGTH_SET_E
        RROR
        84 运动距离为 0，无法运动
        ERR_AXIS_INDEX 101 所选轴超出最大值
        ERR_SET_WHILE_MOVING 102 轴正在运动，不能设置参数
        ERR_ENTER_WHILE_MOVING 103 轴正在运动，不能进入该模式
        ERR_PEL_STATE 104 轴处于正限位，不能正向运动
        ERR_NEL_STATE 105 轴处于负限位，不能负向运动
        ERR_SOFT_PEL_STATE 106 轴处于软正限位，不能正向运动
        ERR_SOFT_NEL_STATE 107 轴处于软负限位，不能负向运动
        ERR_FORCE_IN_OTHER_MODE 108 轴处于非点位模式，不能强制变位
        ERR_MAX_VEL_ZERO 109 设置最大速度错误，不能为 0
        ERR_EQU_ZERO 110 设置轴当量错误，不能为 0
        ERR_BACKLASH_NEG 111 设置反向间隙错误，不能为负值
        ERR_MAX_PULSE 112 设置位置错误，已超出允许范围
        ERR_AXIS_BUS_CONFIGURING 117 设置轴处于未使能状态
        ERR_AXIS_NOT_ETC_BUS
        118 设置轴非总线轴，不能进行该总线操
        作
        ERR_CMP_EXCEED_MAX_AXIS
        ES
        121 所选比较轴超出范围
        ERR_CMP_EXCEED_MAX_INDE
        X
        122 比较点数已满，不能继续添加
        ERR_CMP_EXCEED_MAX_IO 123 进行比较的 IO 超出范围
        ERR_CMP_EXCEED_MAX_CHA
        N
        124 选择的高速比较 IO 超出范围
        ERR_MAP_AXISIO_MAX_AXISE
        S
        130 映射的轴超出范围
        PVT_ERROR_AXISES_OVER_RA
        NGE
        140 所选轴超出范围
        PVT_ERROR_INDEX_OVER_RA
        NGE
        141 控制点已满，不能继续添加
        PVT_ERROR_INDEX_EXCEED 142 控制点已满，不能继续添加
        PVT_ERROR_TIME_EROOR 143 插入段时间为 0 或者负数
        HOME_ERROR_AXISES_OVER_
        RANGE
        200 所选轴超出最大值
        HOME_ERROR_MAX_VEL 202 设置的最大速度为 0
        HOME_ERROR_MAX_ACC 203 设置的加速度小于等于 0
        HOME_ERROR_BOTH_LIMIT
        207 同时处于正、负限位,无法启动回零运
        动
        ERR_TRACE_HAS_STARTED
        210 TRACE 功能已经启动，请停止后再
        次启动
        ERR_TIMEOUT 258 通讯超时，检查接线及通讯参数配置
        ERR_OVER_MAX_SLAVES 10000+1 超过轴个数限制
        ERR_NO_ADDRESS 10000+3 地址不存在
        ERR_NO_EXIST_AXIS 10000+4 轴不存在
         DMC-E3032 V2.0 运动控制卡用户使用手册
        — 151 —
        ERR_AXIS_EXIST 10000+5 设置的轴号不存在
        ERR_NO_EXIST_IO 10000+6 IO 点不存在
        ERR_NO_SLAVE_TYPE 10000+8 设置的从站类型不存在
        ERR_INVALID_PARA_SLAVE_EC
        T
        10000+9 无效 EtherCAT 参数
        ERR_INVALID_PARA_SDO 10000+11 无效 SDO 参数
        ERR_INVALID_PARA_FIELDBUS
        _PORT
        10000+12 无效主站
        ERR_NO_MAPPING 10000+13 配置文件不存在
        ERR_OVER_MAX_SLAVES_ECT 10000+15 超过 EtherCAT 从站个数限制
        ERR_OVER_MASTERNUM 10000+16 设置的主站类型错误
        ERR_FILEMANNAGE_ECT_NOE
        XIST
        10000+17 映射文件不存在
        ERR_FILESIZE_ECT_EMPTY 10000+18 配置文件为空
        ERR_FILEFORMAT_ECT 10000+19 配置文件格式错误
        ERR_MAPPINGINFO_ECT 10000+20 轴及 IO 映射错误
        ERROR_ENI_INVALID 10000+21 下载的配置文件错误
        ERROR_AXISRUNMODE 10000+22 轴运行模式设置错误
        ERROR_FIELDBUS_TYPE 10000+23 总线类型错误
        ERROR_PP_AXIS_MOVEING 10000+ 24 PP 模式下轴在运动中
        ERR_PARAR 10000+ 25 参数错误
            */

        /// <summary>
        /// 取总线错误信息
        /// </summary>
        /// <param name="ErrNum"></param>
        /// <returns></returns>
        public string GetEtherCATErrorInfo(int ErrNum)
        {
            var msg = string.Empty;
            Dictionary<int, string> dic = new Dictionary<int, string>()
            {
                {0x0000,"" },
                {0x0001, "CMD_EXECUTE_ERROR，执行错误"},
                {0x0002, "CMD_BAD_LICENSE，固件未授权"},
                {0x0003, "CMD_PARAM_ERROR，接口参数错误"},
                {0x0004, "CMD_DEVICE_NOT_OPEN，设备未打开"},
                {0x0005, "CMD_ECAT_DISCONNECTED，从站设备未连接"},
                {0x0006, "CMD_DEVICE_OFFLINE 设备掉线"},
                {0x0007, "CMD_FPGA_ACT_TIMEOUT，写入FPGA的指令没有及时返回正确值"},
                {0x0008, "CMD_SDO_RETURN_TIMEOUT，SDO操作返回超时"},
                {0x0009, "CMD_DRIVER_ERROR，设备驱动故障"},
                {0x0010, "CMD_FILE_OPEN_FAIL，文件打开失败"},
                {0x0011, "CMD_FILE_OPERATE_FAIL，文件操作失败"},
                {0x0012, "CMD_INSUFFICIENT_RESOURCE，系统资源不足"},
                {0x0013, "CMD_ENI_UNLOAD OP 未加载ENI文件"},
                {0x0014, "CMD_NOT_DEFINED 指令未定义"},
                {0x0015, "CMD_DATA_CHECK_ERROR 数据校验错误"},
                {0x0016, "CMD_WRITE_TIMEOUT 指令数据写入超时"},
                {0x0017, "CMD_READ_TIMEOUT 指令数据读取超时"},
                {0x0019, "CMD_AXIS_NOT_ON 伺服未使能"},
                {0x0020, "CMD_USE_SAME_ALIAS  从站别名冲突"},
                {0x0021, "CMD_ENI_NO_SUCH_SLAVE  ENI文件找不到对应的从站"},
                {0x0022, "CMD_WATCHDOG_TIMEOUT  看门狗超时"},
                {0x0023, "CMD_EMG_TRIGGERED  急停信号触发"},
                {0x0030, "CMD_ECAT_NETWORK_CHANGED  网络拓扑结构发生变化"} };

            if (dic.ContainsKey(ErrNum))
            {
                return dic[ErrNum];
            }
            return "unkown errNum";
        }

        /// <summary>
        /// SDK调用返回值的错误描述
        /// </summary>
        /// <param name="ErrNum"></param>
        /// <returns></returns>
        public string GetErrorInfo(int ErrNum)
        {
            var msg = string.Empty;
            Dictionary<int, string> dic = new Dictionary<int, string>()
            {
                {0,"" },
                {1,"总线初始化不成功"},
                {2, "参数错误"},
                {3, "通讯超时"},
                {4, "控制器忙、控制器相应轴处于运动中"},
                {5, "链接太频繁"},
                {8, "网络链接失败，请查看连线及通讯参数"},
                {9, "句柄错误，网络未链接或链接已断开"},
                {10, "发送失败，网络未链接或链接已断开"},
                {12, "固件文件错误"},
                {14, "固件不匹配"},
                {17, "不支持的功能"},
                {20, "固件参数错误"},
                {22, "固件当前状态不允许操作"},
                {24, "固件不支持的功能"},
                {25, "密码错误"},
                {26, "密码错误输入次数受限"},
                {30, "手轮脉冲的轴档位选择超出范围（软件控制模式）"},
                {31,"手轮脉冲的轴映射数量超出范围"},
                {32,"手轮脉冲的倍率档位选择超出范围（软件控制模式）"},
                {33,"已进入手轮脉冲模式，不能切换软硬件控制模式"},
                {34,"轴已在运动，不能切换到手轮模式"},
                {50,"LIST 号超出范围"},
                {51,"LIST 没有初始化"},
                {52 ,"参数不在有效范围"},
                {53 ,"LIST 已经打开"},
                {54 ,"LIST 没有初始化"},
                {55 ,"轴数不在有效范围"},
                {56 ,"轴映射表为空"},
                {57 ,"映射轴错误"},
                {58 ,"映射轴忙"},
                {59 ,"运动中不允许更改参数"},
                {60 ,"缓冲区已满"},
                {61 ,"半径为 0 或小于两点的距离的一半"},
                {62 ,"LIST 已经启动"},
                {63 ,"加减速时间为 0"},
                {64 ,"主要 LIST 没有启动"},
                {67 ,"圆弧插补在半径模式下起点和终点不能重合"},
                {70 ,"S 曲线加减速模式，平滑时间为零出错"},
                {71 ,"起跳速度小于零"},
                {72 ,"最大速度小于等于零"},
                {73 ,"终点速度小于零"},
                {74 ,"规划长度小于等于零"},
                {75 ,"最小匀速时间小于零"},
                {76 ,"规划模式非 T 型非 S 型"},
                {77 ,"加速时间等于零"},
                {78 ,"减速时间等于零"},
                {79 ,"规划长度小于等于零（速度规划函数返回，74 为长度计算时返回）"},
                {80 ,"s 时间设置错误(小于等于 0)"},
                {81 ,"起始速度绝对值设置错误(小于 0)"},
                {82 ,"最大速度绝对值设置错误(小于等于0)"},
                {83 ,"停止速度绝对值设置错误(小于 0)"},
                {84 ,"运动距离为 0，无法运动"},
                {101 ,"所选轴超出最大值"},
                {102 ,"轴正在运动，不能设置参数"},
                {103 ,"轴正在运动，不能进入该模式"},
                {104 ,"轴处于正限位，不能正向运动"},
                {105 ,"轴处于负限位，不能负向运动"},
                {106 ,"轴处于软正限位，不能正向运动"},
                {107 ,"轴处于软负限位，不能负向运动"},
                {108 ,"轴处于非点位模式，不能强制变位"},
                {109 ,"设置最大速度错误，不能为 0"},
                {110 ,"设置轴当量错误，不能为 0"},
                {111 ,"设置反向间隙错误，不能为负值"},
                {112 ,"设置位置错误，已超出允许范围"},
                {117 ,"设置轴处于未使能状态"},
                {118 ,"设置轴非总线轴，不能进行该总线操作"},
                {119,"轴可能没有使能"},
                {121 ,"所选比较轴超出范围"},
                {122 ,"比较点数已满，不能继续添加"},
                {123 ,"进行比较的 IO 超出范围"},
                {124 ,"选择的高速比较 IO 超出范围"},
                {130 ,"映射的轴超出范围"},
                {140 ,"所选轴超出范围"},
                {141 ,"控制点已满，不能继续添加"},
                {142 ,"控制点已满，不能继续添加"},
                {143 ,"插入段时间为 0 或者负数"},
                {200 ,"所选轴超出最大值"},
                {202 ,"设置的最大速度为 0"},
                {203 ,"设置的加速度小于等于 0"},
                {207 ,"同时处于正、负限位,无法启动回零运动"},
                {210 ,"TRACE 功能已经启动，请停止后再次启动"},
                {258 ,"通讯超时，检查接线及通讯参数配置"},
                {10000+1 ,"超过轴个数限制"},
                {10000+3 ,"地址不存在"},
                {10000+4 ,"轴不存在"},
                {10000+5 ,"设置的轴号不存在"},
                {10000+6 ,"IO 点不存在"},
                {10000+8 ,"设置的从站类型不存在"},
                {10000+9 ,"无效 EtherCAT 参数"},
                {10000+11 ,"无效 SDO 参数"},
                {10000+12 ,"无效主站"},
                {10000+13 ,"配置文件不存在"},
                {10000+15 ,"超过 EtherCAT 从站个数限制"},
                {10000+16 ,"设置的主站类型错误"},
                {10000+17 ,"映射文件不存在"},
                {10000+18 ,"配置文件为空"},
                {10000+19 ,"配置文件格式错误"},
                {10000+20 ,"轴及 IO 映射错误"},
                {10000+21 ,"下载的配置文件错误"},
                {10000+22 ,"轴运行模式设置错误"},
                {10000+23 ,"总线类型错误"},
                {10000+ 24 ,"PP 模式下轴在运动中"},
                {10000+ 25 ,"参数错误"}
            };

            if (dic.ContainsKey(ErrNum))
            {
                return dic[ErrNum];
            }
            return "unkown errNum";
        }

        /*
        short nmc_read_inport_extern(WORD CardNo, WORD Channel ， WORD NoteID, WORD
            PortNo,WORD* IoValue)
            功 能：读取指定 EtherCAT 扩展模块的输入口组的电平
            参 数：CardNo 控制卡卡号
            Channel 总线通道号，固定为 2；
            NoteID EtherCAT 地址，从 1001 开始，按从站数往后累加
            PortNo 输入组号（32 位一组，从 0 开始，如 0 表示 0-31 位，1 表示 32-63
            位）
            IoValue 输入组各输入端口的值，bit0~bit31 的值分别代表第 0~31 号输入口
            的电平，1：低电平，0：高电平
            返回值：错误代码
            */
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
            return ecat_motion.M_Get_Slave_Digital_Port_Input((short)NoteID, (short)(Channel - 1), ref IoValue, (short)CardNo);
        }


        /*
        short nmc_read_outport_extern(WORD CardNo, WORD Channel ， WORD NoteID, WORD
        PortNo, WORD* IoValue)
        功 能：读取指定 EtherCAT 扩展模块的输出口组的电平
        参 数：CardNo 控制卡卡号
        Channel 总线通道号，固定为 2；
        NoteID EtherCAT 地址，从 1001 开始，按从站数往后累加
        PortNo 输出组号（32 位一组，从 0 开始，如 0 表示 0-31 位，1 表示 32-63
        位）
        IoValue 输出组各输出端口的值，bit0 ~bit31 的值分别代表第 0~31 号输出
            口的电平，1：低电平，0：高电平
            返回值：错误代码
        */
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
            return ecat_motion.M_Get_Slave_Digital_Port_Output((short)NoteID, (short)(Channel - 1), ref IoValue, (short)CardNo);
        }


        /*
        short nmc_write_outbit_extern(WORD CardNo, WORD Channel，WORD NoteID, WORD IoBit,WORD IoValue)
        功 能：设置指定 EtherCAT 扩展模块的某个输出端口的电平
        参 数：CardNo 控制卡卡号
        Channel 总线通道号，固定为 2；
        NoteID EtherCAT 地址，从 1001 开始，按从站数往后累加
        IoBit 输出端口号
        IoValue 指定输出端口的电平，0：低电平，1：高电平
        返回值：错误代码
        */
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
            return ecat_motion.M_Set_Slave_Digital_Chn_Output((short)NoteID, (short)(IoBit + 1), (short)IoValue, (short)CardNo);
        }

        /*
        short nmc_write_outport_extern(UInt16 CardNo, UInt16 Channel, UInt16 NoteID, UInt16 portno, UInt32 outport_val)
        功 能：设置指定 EtherCAT 扩展模块的某个输出端口的电平
        参 数：CardNo 控制卡卡号
        Channel 总线通道号，固定为 2；
        NoteID EtherCAT 地址，从 1001 开始，按从站数往后累加
        portno 输出组号（32 位一组，从 0 开始，如 0 表示 0-31 位，1 表示 32-63位）
        IoValue 输出组各输出端口的值，bit0~bit31 的值分别代表第 0~31 号输出口的电平，1：低电平，0：高电平
        返回值：错误代码
        */
        /// <summary>
        /// 设置指定 EtherCAT 扩展模块的某个输出端口的电平(0：低电平，1：高电平)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Channel">总线通道号，固定为 2</param>
        /// <param name="NoteID">EtherCAT 地址，从 1001 开始，按从站数往后累加</param>
        /// <param name="portno">输出端口号</param>
        /// <param name="IoValue">指定输出端口的电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int NmcWriteOutportExtern(int CardNo, int Channel, int NoteID, ushort portno, uint IoValue)
        {
            return ecat_motion.M_Set_Slave_Digital_Port_Output((short)NoteID, (short)(Channel - 1), IoValue, 0xFFFFFFFF, (short)CardNo);
        }

        /*
                short nmc_read_outbit_extern(WORD CardNo, WORD Channel，WORD NoteID, WORD IoBit,WORD* 
                IoValue)
                    功 能：读取指定 EtherCAT 扩展模块的某个输出端口的电平
                    参 数：CardNo 控制卡卡号
                    Channel 总线通道号，固定为 2；
                    NoteID EtherCAT 地址，从 1001 开始，按从站数往后累加
                    IoBit 输出端口号
                    IoValue 指定输出端口的电平，0：低电平，1：高电平
                返回值：错误代码

                */
        /// <summary>
        /// 读取指定 EtherCAT 扩展模块的某个输出端口的电平(0：低电平，1：高电平)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Channel">总线通道号，固定为 2</param>
        /// <param name="NoteID">EtherCAT 地址，从 1001 开始，按从站数往后累加</param>
        /// <param name="IoBit">输出端口号</param>
        /// <param name="IoValue">指定输出端口的电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int NmcReadOutbitExtern(int CardNo, int Channel, int NoteID, int IoBit, ref short IoValue)
        {
            return ecat_motion.M_Get_Slave_Digital_Chn_Output((short)NoteID, (short)(IoBit + 1), ref IoValue, (short)CardNo);
        }
        /// <summary>
        /// 读取指定 EtherCAT 扩展模块的某个输出端口的电平(0：低电平，1：高电平)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Channel">总线通道号，固定为 2</param>
        /// <param name="NoteID">EtherCAT 地址，从 1001 开始，按从站数往后累加</param>
        /// <param name="IoBit">输入端口号</param>
        /// <param name="IoValue">指定输出端口的电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int NmcReadInbitExtern(int CardNo, int Channel, int NoteID, int IoBit, ref short IoValue)
        {
            return ecat_motion.M_Get_Slave_Digital_Chn_Input((short)NoteID, (short)(IoBit + 1), ref IoValue, (short)CardNo);
        }

        /// <summary>
        /// 设置主站参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="PortNum">EtherCAT 端口号，固定为 2</param>
        /// <param name="Baudrate"></param>
        /// <param name="NodeCnt"></param>
        /// <param name="MasterId"></param>
        /// <returns>错误代码</returns>
        public int NmcSetCardMasterPara(int CardNo, int PortNum, int Baudrate, int NodeCnt, int MasterId)
        {
            //return ecat_motion.M_SetGearMaster((ushort)CardNo, (ushort)PortNum, (ushort)Baudrate, (uint)NodeCnt, (ushort)MasterId);
            return 0;
        }

        /// <summary>
        /// 获取主站参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="PortNum">EtherCAT 端口号，固定为 2</param>
        /// <param name="Baudrate"></param>
        /// <param name="NodeCnt"></param>
        /// <param name="MasterId"></param>
        /// <returns>错误代码</returns>
        public int NmcGetCardMasterPara(int CardNo, int PortNum, ref ushort Baudrate, ref uint NodeCnt, ref ushort MasterId)
        {
            //return ecat_motion.nmc_get_master_para((ushort)CardNo, (ushort)PortNum, ref Baudrate, ref NodeCnt, ref MasterId);
            return 0;
        }

        /// <summary>
        /// 获取发布版本号（适用于DMC3000/DMC5X10系列脉冲卡、EtherCAT总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int DmcGetCardReleaseVersion(ushort CardNo, byte[] ReleaseVersion)
        {
            return ecat_motion.M_GetVersion(out ReleaseVersion[0], 1, (short)CardNo);
        }

        /// <summary>
        ///硬件复位（适用于所有脉冲/总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int DmcCardBoardReset()
        {
            return ecat_motion.M_Reset(0);
        }

        /// <summary>
        /// 控制卡热复位（适用于EtherCAT、RTEX总线卡）  
        /// </summary>
        /// <returns>错误代码</returns>
        public int DmcCardSoftReset(ushort CardNo)
        {
            return ecat_motion.M_Reset((short)CardNo);
        }

        /// <summary>
        /// 控制卡冷复位（适用于所有脉冲/总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int DmcCardCoolReset(ushort CardNo)
        {
            return ecat_motion.M_Reset((short)CardNo);
        }

        /// <summary>
        /// 控制卡初始复位（适用于EtherCAT总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int DmcCardOriginalReset(ushort CardNo)
        {
            return ecat_motion.M_Reset((short)CardNo);
        }

        /// <summary>
        /// 设置EtherCAT总线循环周期 
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="Fieldbustype">  EtherCAT 端口号，固定为 2</param>
        /// <param name="CycleTime">CAT 总线循环周期，单位：us，支持 250/500/1000/</param>
        /// <returns></returns>
        public int NmcSetCardCycleTime(int CardNo, int FieldbusType, int CycleTime)
        {
            //return ecat_motion.nmc_set_cycletime((ushort)CardNo, (ushort)FieldbusType, CycleTime);
            return 0;
        }

        /// <summary>
        ///  清除总线错误
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <returns></returns>
        public int DmcClearCardErrCode(int CardNo)
        {
            //return ecat_motion.nmc_clear_errcode((ushort)CardNo, 2);
            return 0;
        }

        /// <summary>
        /// 设置HOME信号（适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="org_logic"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public int NmcSetCardAxisHomeMove(int CardNo, int axis, int org_logic, double filter)
        {
            //return ecat_motion.dmc_set_home_pin_logic((ushort)CardNo, (ushort)axis, (ushort)org_logic, filter);
            return 0;
        }

        /// <summary>
        /// 获取HOME信号（适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="org_logic"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public int NmcGetCardAxisHomeMove(int CardNo, int axis, ref UInt16 org_logic, ref double filter)
        {
            //return ecat_motion.dmc_get_home_pin_logic((ushort)CardNo, (ushort)axis, ref org_logic, ref filter);
            return 0;
        }


        /// <summary>
        /// 启动轴回零
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">轴编号</param>
        /// <returns></returns>
        public int DmcCardAxisHomeMove(int CardNo, int axis)
        {
            return ecat_motion.M_HomingStart((short)axis, (short)CardNo);
        }

        /// <summary>
        /// 设置指定轴的回原点模式（适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="home_dir"></param>
        /// <param name="vel"></param>
        /// <param name="mode"></param>
        /// <param name="EZ_count"></param>
        /// <returns></returns>
        public int DmcSetCardAxisHomeMode(int CardNo, int axis, int home_dir, double vel, int acc, int mode, int EZ_count)
        {
            //return ecat_motion.M_SetHomingMode((short)CardNo, (ushort)axis, (ushort)home_dir, vel, (ushort)mode, (ushort)EZ_count);
            ecat_motion.M_SetHomingMode((short)(axis + 1), 6, (short)CardNo);
            uint tacc = (uint)((home_dir - vel) / acc);
            return ecat_motion.M_SetHomingPrm((short)(axis + 1), (short)mode, EZ_count, (uint)home_dir, (uint)vel, (uint)tacc, 0, (short)CardNo);
        }

        /// <summary>
        /// 读取指定轴的回原点模式（适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">轴编号</param>
        /// <returns></returns>
        public int DmcGetCardAxisHomeMode(int CardNo, int axis)
        {
            UInt16 home_dir = 0;
            double vel = 0;
            UInt16 home_mod = 0;
            UInt16 EZ_count = 0;
            //return ecat_motion.M_GetHomingPrm((ushort)CardNo, (ushort)axis, ref home_dir, ref vel, ref home_mod, ref EZ_count);
            return 0;
        }

        /// <summary>
        /// 设置回零遇限位是否反找（适用于DMC3000/5X10系列脉冲卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="Axis">轴编号</param>
        /// <param name="Enable">是否反找</param>
        /// <returns></returns>
        public int DmcSetCardHomeELReturn(int CardNo, int Axis, int Enable)
        {
            //return ecat_motion.dmc_set_home_el_return((ushort)CardNo, (ushort)Axis, (ushort)Enable);
            return 0;
        }

        /// <summary>
        /// 获取遇限位反找使能（适用于DMC3000/5X10系列脉冲卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="Axis">轴编号</param>
        /// <param name="Enable">是否反找</param>
        /// <returns></returns>
        public int DmcGetCardHomeELReturn(int CardNo, int Axis, ref UInt16 Enable)
        {
            //return ecat_motion.dmc_get_home_el_return((ushort)CardNo, (ushort)Axis, ref Enable);
            return 0;
        }

        /// <summary>
        /// 设置回零速度参数（适用于Rtex总线卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="Axis">轴号</param>
        /// <param name="Low_Vel">低速</param>
        /// <param name="High_Vel">高速</param>
        /// <param name="Tacc">总加速时间</param>
        /// <param name="Tdec">总减速时间</param>
        /// <returns></returns>
        public int DmcSetCardHomeProfileUnit(int CardNo, int Axis, double Low_Vel, double High_Vel, double Tacc, double Tdec)
        {
            //return ecat_motion.dmc_set_home_profile_unit((ushort)CardNo, (ushort)Axis, Low_Vel, High_Vel, Tacc, Tdec);
            return 0;
        }

        /// <summary>
        /// 获取回零速度参数（适用于Rtex总线卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="Axis">轴号</param>
        /// <param name="Low_Vel">低速</param>
        /// <param name="High_Vel">高速</param>
        /// <param name="Tacc">总加速时间</param>
        /// <param name="Tdec">总减速时间</param>
        /// <returns></returns>
        public int DmcGetCardHomeProfileUnit(int CardNo, int Axis, ref double Low_Vel, ref double High_Vel, ref double Tacc, ref double Tdec)
        {
            //return ecat_motion.dmc_get_home_profile_unit((ushort)CardNo, (ushort)Axis, ref Low_Vel, ref High_Vel, ref Tacc, ref Tdec);
            return 0;
        }

        /// <summary>
        /// 获取回零执行状态（适用于所有脉冲/总线卡）
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="Axis">轴号</param>
        /// <param name="state">状态结果</param>
        /// <returns></returns>
        public int DmcGetCardHomeResult(int CardNo, int Axis, out short state)
        {
            return ecat_motion.M_GetEcatHomingStatus((short)(Axis + 1), out state, (short)CardNo);
        }

        /// <summary>
        /// 获取插补坐标系
        /// </summary>
        /// <param name="CardNo"></param>
        /// <returns></returns>
        public int DmcGetTotalLiners(int CardNo, ecat_motion.CrdCfg pCrdPrm, short crd)
        {
            return ecat_motion.M_GetCrd(crd, out pCrdPrm, (short)CardNo);
        }

        /// <summary>
        /// 设置连续插补前瞻模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">坐标系号，取值范围：1~2</param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能</param>
        /// <param name="LookaheadSegments"></param>
        /// <param name="PathError"></param>
        /// <param name="LookaheadAcc"></param>
        /// <returns></returns>
        public int DmcSetCardContiLookaheadMode(int CardNo, int fifo, int Crd, double T, double accMax, short nums, ecat_motion.CrdBlockData pLookAheadBuf)
        {
            //return ecat_motion.M_SetVelPlanning((ushort)CardNo, (ushort)Crd, (ushort)enable, (ushort)LookaheadSegments, PathError, LookaheadAcc);
            return ecat_motion.M_SetVelPlanning((short)Crd, (short)fifo, T, accMax, nums, ref pLookAheadBuf, (short)CardNo);
        }

        /// <summary>
        /// 获取连续插补前瞻模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">坐标系号，取值范围：0~5</param>
        /// <returns></returns>
        public int DmcGetCardContiLookaheadMode(int CardNo, int Crd, ref ushort enable, ref int LookaheadSegments, ref double PathError, ref double LookaheadAcc)
        {
            //return ecat_motion.dmc_conti_get_lookahead_mode((ushort)CardNo, (ushort)Crd, ref enable, ref LookaheadSegments, ref PathError, ref LookaheadAcc);
            return 0;
        }

        /// <summary>
        /// 打开连续插补指令表
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">坐标系号，取值范围：1~2</param>
        /// <param name="pCrdPrm">插补结构体</param>
        /// <returns></returns>
        public int DmcOpenCardContiList(int CardNo, int Crd, ecat_motion.CrdCfg pCrdPrm)
        {
            return ecat_motion.M_SetCrd((short)Crd, ref pCrdPrm, (short)CardNo);
        }

        /// <summary>
        /// 开始连续插补
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Crd">坐标系号，取值范围：1~2</param>
        /// <param name="FIFO">缓存区1~2</param>
        /// <returns></returns>
        public int DmcCardContiStartList(int CardNo, int Crd, int FIFO)
        {
            return ecat_motion.M_CrdStart((short)Crd, (short)FIFO, (short)CardNo);
        }

        /// <summary>
        /// 设置螺距补偿（开启、关闭）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴号 </param>
        /// <param name="enable">螺距补偿：开启\关闭</param>
        /// <returns></returns>
        public int DmcSetCardLeadScrewCompEnable(int CardNo, int axis, int enable)
        {
            //return ecat_motion.dmc_enable_leadscrew_comp((ushort)CardNo, (ushort)axis, (ushort)enable);
            return 0;
        }

        /// <summary>
        /// 设置螺距补偿参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴号 </param>
        /// <param name="n"></param>
        /// <param name="startpos"></param>
        /// <param name="lenpos"></param>
        /// <param name="pCompPos"></param>
        /// <param name="pCompNeg"></param>
        /// <returns></returns>
        public int DmcSetCardLeadScrewCompConfig(int CardNo, int axis, int n, int startpos, int lenpos, int[] pCompPos, int[] pCompNeg)
        {
            //return ecat_motion.dmc_set_leadscrew_comp_config((ushort)CardNo, (ushort)axis, (ushort)n, startpos, lenpos, pCompPos, pCompNeg);
            return 0;
        }

        /// <summary>
        /// 读取螺距补偿参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴号 </param>
        /// <param name="n"></param>
        /// <param name="startpos"></param>
        /// <param name="lenpos"></param>
        /// <param name="pCompPos"></param>
        /// <param name="pCompNeg"></param>
        /// <returns></returns>
        public int DmcGetCardLeadScrewCompConfig(int CardNo, int axis, ref ushort n, ref int startpos, ref int lenpos, int[] pCompPos, int[] pCompNeg)
        {
            //return ecat_motion.dmc_get_leadscrew_comp_config((ushort)CardNo, (ushort)axis, ref n, ref startpos, ref lenpos, pCompPos, pCompNeg);
            return 0;
        }

        /// <summary>
        /// 设置轴运动模式：
        /// 1为pp模式，6为回零模式，8为csp模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="runmode">运动模式</param>
        /// <returns>short类型值</returns>
        public int NmcSetCardAxisRunMode(int CardNo, int axis, ushort runmode)
        {
            //return ecat_motion.nmc_set_axis_run_mode((ushort)CardNo, (ushort)axis, runmode);
            return 0;
        }

        /// <summary>
        /// 获取轴状态
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="axis"></param>
        /// <returns>ushort错误编号</returns>
        public int NmcGetCardAxisErrCode(int CardNo, int axis, ref int errcode)
        {
            //return ecat_motion.M_GetSts((short)axis, out errcode, 1, (short)CardNo);
            int[] code = new int[1];
            var res = ecat_motion.M_GetSts((short)axis, out code, 1, (short)CardNo);
            errcode = code[0];
            return res;
        }

        /// <summary>
        /// 一次性获取所有轴状态
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="axis"></param>
        /// <returns>ushort错误编号</returns>
        public int NmcGetCardAxisErrCode(int CardNo, int axis, int nums, ref int[] axis_IO)
        {
            return ecat_motion.M_GetSts((short)(axis + 1), out axis_IO, (short)nums, (short)CardNo);
        }

        /// <summary>
        ///  清除总线轴错误码
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axisStartAddress">EtherCAT 总线轴轴号</param>
        /// <returns></returns>
        public int NmcClearCardAxisErrCode(int CardNo, int axisStartAddress)
        {
            return ecat_motion.M_ClrSts((short)(axisStartAddress + 1), 1, (short)CardNo);
        }

        /// <summary>
        ///  一次清除全部总线轴错误码
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axisStartAddress">EtherCAT 总线轴轴号</param>
        /// <returns></returns>
        public int NmcClearCardAxisErrCode(int CardNo, int nums, int axisStartAddress)
        {
            return ecat_motion.M_ClrSts((short)(axisStartAddress + 1), (short)nums, (short)CardNo);
        }

        /// <summary>
        ///  清除端口报警
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="PortNum">端口号</param>
        /// <returns></returns>
        public int NmcClearCardAlarmFieldbus(int CardNo, int PortNum)
        {
            //return ecat_motion.nmc_clear_alarm_fieldbus((ushort)CardNo, (ushort)PortNum);
            return 0;
        }

        /// <summary>
        /// 添加比较点（适用于DMC5X10系列脉冲卡、E5032总线卡） (单轴高速)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口） </param>
        /// <param name="pos">1：队列模式下：添加比较位置，单位：pulse | 2：线性模式下：更新起始比较位置，单位：pulse | 3：其他模式下：更新比较位置，单位：pulse</param>
        /// <returns></returns>
        public int DmcAddCardHcmpPoints(int CardNo, int hcmp, double cmp_pos)
        {
            //return ecat_motion.dmc_hcmp_add_point_unit((ushort)CardNo, (ushort)hcmp, cmp_pos);
            return -6;
        }

        /// <summary>
        /// 设置二维位置比较器 （二维低速）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能 </param>
        /// <param name="cmp_source">   比较源，0：指令位置计数器，1：编码器计数器</param>
        /// <returns></returns>
        public int DmcSetCardCompareConfigExtern(int CardNo, int enable, int cmp_source)
        {
            //return ecat_motion.dmc_compare_set_config_extern((ushort)CardNo, (ushort)enable, (ushort)cmp_source);
            return -6;

        }

        /// <summary>
        /// 获取二维位置比较器 （二维低速）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能 </param>
        /// <param name="cmp_source">   比较源，0：指令位置计数器，1：编码器计数器</param>
        /// <returns></returns>
        public int DmcGetCardCompareConfigExtern(int CardNo, ref ushort enable, ref ushort cmp_source)
        {
            return -6;
        }

        /// <summary>
        /// 添加比较点（适用于所有脉冲卡）（二维低速）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号 </param>
        /// <param name="pos">设置比较位置为 X pulse</param>
        /// <param name="dir">设置比较模式</param>
        /// <param name="action">设置触发功能（为 IO 电平取反）</param>
        /// <param name="actpara">设置输出 IO 端口 (0 触发功能)</param>
        /// <returns></returns>
        public int DmcAddCardComparePointsExtern(int CardNo, UInt16[] axis, int[] pos, UInt16[] dir, int action, int actpara)
        {
            return -6;
            //return ecat_motion.dmc_compare_add_point_extern((ushort)CardNo, axis, pos, dir, (ushort)action, (uint)actpara);
        }

        /// <summary>
        /// 获取当前比较点（适用于所有脉冲/总线卡）（二维低速）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public int DmcGetCardCompareCurrentPointsExtern(int CardNo, int[] pos = null)
        {
            //return ecat_motion.dmc_compare_get_current_point_extern((ushort)CardNo, pos);
            return -6;

        }

        /// <summary>
        /// 查询已经比较过的点（适用于所有脉冲卡、EtherCAT总线卡）（二维低速）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <returns></returns>
        public int DmcGetCardCompareRunnedPointsExtern(int CardNo, ref int pointNum)
        {
            //return ecat_motion.dmc_compare_get_points_runned_extern((ushort)CardNo, ref pointNum);
            return -6;

        }

        /// <summary>
        /// 清除所有比较点（适用于所有脉冲/总线卡）（二维低速）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <returns></returns>
        public int DmcClearCardComparePoints(int CardNo)
        {
            //return ecat_motion.dmc_compare_clear_points_extern((ushort)CardNo);
            return -6;
        }

        /// <summary>
        /// 设置二维高速比较使能 （二维高速）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口） </param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能</param>
        /// <returns></returns>
        public int DmcSetCardHcmp2DEnable(int CardNo, int hcmp, int enable)
        {
            return -6;
        }

        /// <summary>
        /// 获取二维高速比较使能 （二维高速）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口） </param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能 </param>
        /// <returns></returns>
        public int DmcGetCardHcmp2DEnable(int CardNo, int hcmp, ref ushort enable)
        {
            return -6;
            return -6;
        }

        /// <summary>
        /// 配置二维位置高速比较器（二维高速   适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号  </param>
        /// <param name="hcmp"> 高速比较号</param>
        /// <param name="cmp_mode"> 比较模式：0：进入误差带后触发  |  1：进入误差带单轴等于后再触发</param>
        /// <param name="x_axis">x轴关联轴号</param>
        /// <param name="x_cmp_source">x 轴比较位置源：0：指令位置，1：反馈位置</param>
        /// <param name="y_axis">y轴关联轴号</param>
        /// <param name="y_cmp_source">y 轴比较位置源：0：指令位置，1：反馈位置</param>
        /// <param name="error">轴误差带设置，单位：unit </param>
        /// <param name="cmp_logic">有效电平：0：低电平，1：高电</param>
        /// <param name="time">脉冲宽度，单位：us，取值范围：1us-20s </param>
        /// <param name="pwm_enable">pwm 模式使能</param>
        /// <param name="duty">占空比 </param>
        /// <param name="freq">频率 </param>
        /// <param name="port_sel">端口</param>
        /// <param name="pwm_number"> 输出的 pwm 脉冲数</param>
        /// <returns></returns>
        public int DmcSetCardHcmp2DConfig(int CardNo, int hcmp, int cmp_mode, int x_axis, int x_cmp_source, int y_axis, int y_cmp_source, int error, int cmp_logic, int time, int pwm_enable, double duty, int freq, int port_sel, int pwm_number)
        {
            return -6;
        }

        /// <summary>
        /// 获取二维位置高速比较器（二维高速   适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号  </param>
        /// <param name="hcmp"> 高速比较号</param>
        /// <param name="cmp_mode"></param>
        /// <param name="x_axis"></param>
        /// <param name="x_cmp_source"></param>
        /// <param name="y_axis"></param>
        /// <param name="y_cmp_source"></param>
        /// <param name="error"></param>
        /// <param name="cmp_logic"></param>
        /// <param name="time"></param>
        /// <param name="pwm_enable"></param>
        /// <param name="duty"></param>
        /// <param name="freq"></param>
        /// <param name="port_sel"></param>
        /// <param name="pwm_number"></param>
        /// <returns></returns>
        public int DmcGetCardHcmp2DConfig(int CardNo, int hcmp, ref ushort cmp_mode, ref ushort x_axis, ref ushort x_cmp_source, ref ushort y_axis, ref ushort y_cmp_source, ref int error, ref ushort cmp_logic, ref int time, ref ushort pwm_enable, ref double duty, ref int freq, ref ushort port_sel, ref ushort pwm_number)
        {
            return -6;
        }

        /// <summary>
        /// 配置二维位置高速比较器（二维高速  适用于DMC5X10系列脉冲卡、E5032总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号  </param>
        /// <param name="hcmp"> 高速比较号</param>
        /// <param name="cmp_mode"> 比较模式：0：进入误差带后触发  |  1：进入误差带单轴等于后再触发</param>
        /// <param name="x_axis">x轴关联轴号</param>
        /// <param name="x_cmp_source">x 轴比较位置源：0：指令位置，1：反馈位置</param>
        /// <param name="x_cmp_error">x 轴误差带设置，单位：unit</param>
        /// <param name="y_axis">y 轴关联轴号</param>
        /// <param name="y_cmp_source">y 轴比较位置源：0：指令位置，1：反馈位置</param>
        /// <param name="y_cmp_error">y 轴误差带设置，单位：unit </param>
        /// <param name="cmp_logic">    有效电平：0：低电平，1：高电</param>
        /// <param name="time">脉冲宽度，单位：us，取值范围：1us-20s </param>
        /// <returns></returns>
        public int DmcSetCardHcmp2DConfig(int CardNo, int hcmp, int cmp_mode, int x_axis, int x_cmp_source, double x_cmp_error, int y_axis, int y_cmp_source, double y_cmp_error, int cmp_logic, int time)
        {
            return -6;
        }

        /// <summary>
        /// 获取二维位置高速比较器（二维高速  适用于DMC5X10系列脉冲卡、E5032总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号  </param>
        /// <param name="hcmp"> 高速比较号</param>
        /// <param name="cmp_mode"> 比较模式：0：进入误差带后触发  |  1：进入误差带单轴等于后再触发</param>
        /// <param name="x_axis">x轴关联轴号</param>
        /// <param name="x_cmp_source">x 轴比较位置源：0：指令位置，1：反馈位置</param>
        /// <param name="x_cmp_error">x 轴误差带设置，单位：unit</param>
        /// <param name="y_axis">y 轴关联轴号</param>
        /// <param name="y_cmp_source">y 轴比较位置源：0：指令位置，1：反馈位置</param>
        /// <param name="y_cmp_error">y 轴误差带设置，单位：unit </param>
        /// <param name="cmp_logic">    有效电平：0：低电平，1：高电</param>
        /// <param name="time">脉冲宽度，单位：us，取值范围：1us-20s </param>
        /// <returns></returns>
        public int DmcGetCardHcmp2DConfig(int CardNo, int hcmp, ref ushort cmp_mode, ref ushort x_axis, ref ushort x_cmp_source, ref double x_cmp_error, ref ushort y_axis, ref ushort y_cmp_source, ref double y_cmp_error, ref ushort cmp_logic, ref int time)
        {
            return -6;
        }

        /// <summary>
        /// 添加二维高速位置比较点（适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="hcmp">高速比较器号 </param>
        /// <param name="x_cmp_pos"> 队列模式下：添加 x 比较位置，单位：unit </param>
        /// <param name="y_cmp_pos">队列模式下：添加 x 比较位置，单位：unit  </param>
        /// <returns></returns>
        public int DmcAddCardHcmp2DPoint(int CardNo, int hcmp, int x_cmp_pos, int y_cmp_pos)
        {
            return -6;
        }

        /// <summary>
        /// 添加二维高速位置比较点（适用于DMC5X10系列脉冲卡、E5032总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="hcmp">高速比较器号 </param>
        /// <param name="x_cmp_pos"> 队列模式下：添加 x 比较位置，单位：unit </param>
        /// <param name="y_cmp_pos">队列模式下：添加 x 比较位置，单位：unit  </param>
        /// <param name="cmp_outbit">输出口号 </param>
        /// <returns></returns>
        public int DmcAddCardHcmp2DPointUnit(int CardNo, int hcmp, double x_cmp_pos, double y_cmp_pos, int cmp_outbit)
        {
            return -6;
        }

        /// <summary>
        /// 读取高速比较参数（适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="hcmp">高速比较器号 </param>
        /// <param name="remained_points">返回可添加比较点数 </param>
        /// <param name="x_current_point">返回当前 x 比较点位置</param>
        /// <param name="y_current_point">返回当前 y 比较点位置 </param>
        /// <param name="runned_points">返回已比较点数</param>
        /// <param name="current_state">比较器状态 1 正在输出 0 输出完成</param>
        /// <returns></returns>
        public int DmcGetCardHcmp2DCurrentState(int CardNo, int hcmp, ref int remained_points, ref int x_current_point, ref int y_current_point, ref int runned_points, ref UInt16 current_state)
        {
            return -6;
        }

        /// <summary>
        /// 读取高速比较参数（适用于DMC5X10系列脉冲卡、EtherCAT总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="hcmp">高速比较器号 </param>
        /// <param name="remained_points">返回可添加比较点数 </param>
        /// <param name="x_current_point">返回当前 x 比较点位置</param>
        /// <param name="y_current_point">返回当前 y 比较点位置 </param>
        /// <param name="runned_points">返回已比较点数</param>
        /// <param name="current_state">比较器状态 1 正在输出 0 输出完成</param>
        /// <param name="current_outbit">返回当前输出口 </param>
        /// <returns></returns>
        public int DmcGetCardHcmp2DCurrentStateUnit(int CardNo, int hcmp, ref int remained_points, ref double x_current_point, ref double y_current_point, ref Int32 runned_points, ref UInt16 current_state, ref UInt16 current_outbit)
        {
            return -6;
        }

        /// <summary>
        /// 强制二维高速比较输出（适用于所有脉冲卡、EtherCAT总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">保留，参数值为 0 </param>
        /// <param name="cmp_outbit">强制二维比较输出，设置为1使能</param>
        /// <returns></returns>
        public int DmcCardHcmp2DForceOutPut(int CardNo, int hcmp, int cmp_outbit)
        {
            return -6;
        }

        /// <summary>
        /// 配置二维比较PWM输出模式（适用于DMC5X10系列脉冲卡、EtherCAT总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="hcmp">高速比较器号</param>
        /// <param name="pwm_enable">读取 pwm 模式使能状态 </param>
        /// <param name="duty">读取占空比</param>
        /// <param name="freq">读取频率</param>
        /// <param name="pwm_number">读取输出的 pwm 脉冲数 </param>
        /// <returns>二维比较PWM输出模式</returns>
        public int DmcSetCardHcmp2DPWMOutPut(int CardNo, int hcmp, int pwm_enable, double duty, double freq, int pwm_number)
        {
            return -6;
        }

        /// <summary>
        /// 获取二维比较PWM输出模式（适用于DMC5X10系列脉冲卡、EtherCAT总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="hcmp">高速比较器号</param>
        /// <param name="pwm_enable">读取 pwm 模式使能状态 </param>
        /// <param name="duty">读取占空比</param>
        /// <param name="freq">读取频率</param>
        /// <param name="pwm_number">读取输出的 pwm 脉冲数 </param>
        /// <returns></returns>
        public int DmcGetCardHcmp2DPWMOutPut(int CardNo, int hcmp, ref UInt16 pwm_enable, ref double duty, ref double freq, ref UInt16 pwm_number)
        {
            return -6;
        }


        /// <summary>
        /// 清除二维高速位置比较点（适用于所有脉冲卡、EtherCAT总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">高速比较器号</param>
        /// <returns></returns>
        public int DmcClearCardHcmp2DPoints(int CardNo, int hcmp)
        {
            return -6;
        }






        // DMCE3032卡
        #region 板卡设置函数

        /// <summary>
        /// 控制卡初始化函数
        /// </summary>
        /// <returns>卡的数量</returns>
        public int DmcInitCard(ref int[] card_id, ref int card_count)
        {
            //public int InitCard(ref int[] card_id, ref int card_count)
            //{
            //    card_id = new int[16];
            //    return CPci9014.p9014_initial(ref card_count, card_id);
            //}
            var f1 = ecat_motion.M_Open(0, 0);
            if (f1 != 0)//todo:2022.11.26
            {
                IsInit = false;
            }
            else
            {
                card_id = new int[16];
                card_count = 1;
                IsInit = true;
            }
            return f1;
        }

        /// <summary>
        /// 关闭控制卡（适用于所有脉冲/总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int DmcCardBoardClose()
        {
            return ecat_motion.M_Close(0);
        }

        /// <summary>
        /// 获取控制卡硬件 ID 号
        /// </summary>
        /// <param name="cardnum">返回初始化成功的卡数 </param>
        /// <param name="cardtypes">返回控制卡固件类型数组</param>
        /// <param name="CardNos">返回控制卡硬件 ID 号数组，卡号按从小到大顺序排列  </param>
        /// <returns>错误代码</returns>
        public int DmcGetCardInList(ref ushort cardnum, ref uint[] cardtypes, ref ushort[] CardIdList)
        {
            //return ecat_motion.dmc_get_CardInfList(ref cardnum, cardtypes, CardIdList);
            var ret = DmcCardBoardReset();
            Thread.Sleep(500);
            ret = ecat_motion.M_ConnectECAT(0, 0); //连接总线
            return ret;
        }

        /// <summary>
        /// 获取控制卡硬件版本号（适用于DMC3000/DMC5X10系列脉冲卡、EtherCAT总线卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <returns></returns>
        public int DmcGetCardVersion(ushort CardNo, ref UInt32 CardVersion)
        {
            byte ReleaseVersion = 0;
            return (int)ecat_motion.M_GetVersion(out ReleaseVersion, 1, (short)CardNo);

        }
        /// <summary>
        /// 获取控制卡固件版本号（适用于所有脉冲/总线卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int DmcGetCardSoftVersion(ushort CardNo, ref uint FirmID, ref uint SubFirmID)
        {
            //return ecat_motion.dmc_get_card_soft_version(CardNo, ref FirmID, ref SubFirmID);
            return 0;
        }

        /// <summary>
        /// 获取控制卡动态库文件版本号
        /// </summary>
        /// <param name="LibVer">返回库版本号</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardLibVersion(ref UInt32 LibVer)
        {
            //return ecat_motion.dmc_get_card_lib_version(ref LibVer);
            return 0;
        }

        /// <summary>
        /// 获取指定卡轴数（适用于所有脉冲卡）
        /// </summary>
        /// <returns>错误代码</returns>
        public int DmcGetCardTotalAxes(ushort CardNo, ref UInt32 TotalAxis)
        {
            ecat_motion.SL_RES source = GetCurrentCardSalveSource((short)CardNo);
            TotalAxis = (uint)source.AxisNum;
            return 0;
        }

        ecat_motion.SL_RES GetCurrentCardSalveSource(short cardNo)
        {
            ecat_motion.SL_RES source = new ecat_motion.SL_RES();
            var res = ecat_motion.M_GetSlaveResource(out source, (short)cardNo);
            return source;
        }

        /// <summary>
        /// 下载从站配置参数文件
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="ConfigName">参数文件名，默认"AxisPara.ini"</param>
        /// <returns>错误代码</returns>
        public int GetCardDownloadConfigfile(int CardNo, string ConfigName = "AxisPara.ini")
        {
            return ecat_motion.M_LoadParamFromFile(ConfigName, (short)CardNo);
        }

        /// <summary>
        /// 下载总线配置文件
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="buffer">配置文件字符串缓存，UTF8 编码</param>
        /// <param name="fileincontrol">文件名（保留参数）</param>
        /// <param name="filetype">文件类型</param>
        /// <returns>错误代码</returns>
        public int GetCardDownloadMemfile(int CardNo, string fileincontrol)
        {
            return ecat_motion.M_LoadEni(fileincontrol, (short)CardNo);
        }

        /// <summary>
        /// 下载固件文件
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="FileName">文件路径：参数文件名+后缀：相对路径；完整描述参数文件的路径+文件名后缀：绝对路径</param>
        /// <returns></returns>
        public int DmcGetDownloadFirmware(int CardNo, String FileName)
        {
            //return ecat_motion.dmc_download_firmware((ushort)CardNo, FileName);
            return 0;
        }
        #endregion

        #region 脉冲当量设置函数

        /// <summary>
        /// 设置轴的脉冲当量
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="equiv">脉冲当量，单位：pulse/unit</param>
        /// <returns>short类型值</returns>
        public int DmcSetCardAxisEquiv(int CardNo, int axis, double equiv)
        {
            //return ecat_motion.dmc_set_equiv((ushort)CardNo, (ushort)axis, equiv);
            return -4;
        }
        /// <summary>
        /// 获取轴的脉冲当量
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="equiv">脉冲当量，单位：pulse/unit</param>
        /// <returns>脉冲当量</returns>
        public int DmcGetCardAxisEquiv(int CardNo, int axis, ref double equiv)
        {
            //return ecat_motion.dmc_get_equiv((ushort)CardNo, (ushort)axis, ref equiv);
            return -4;
        }
        #endregion

        #region 回原点运动函数

        /// <summary>
        /// 启动 EtherCAT 总线轴回零
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">轴编号</param>
        /// <returns></returns>
        public int CardNmcAxisNmcHomeMove(int CardNo, int axis)
        {
            //return ecat_motion.nmc_home_move((ushort)CardNo, (ushort)axis);
            return 0;
        }
        #endregion

        #region 限位开关设置函数
        /// <summary>
        /// 设置软限位
        /// <para>注 意：</para>
        /// <para>正、负限位位置可为正数也可为负数，但正限位位置应大于负限位位置</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="enable">使能状态，0：禁止，1：允许 </param>
        /// <param name="source_sel">计数器选择，0：指令位置计数器，1：编码器</param>
        /// <param name="SL_action">限位停止方式，0：立即停止 1：减速停止</param>
        /// <param name="N_limit">负限位位置，单位：pulse </param>
        /// <param name="P_limit">正限位位置，单位：pulse</param>
        /// <returns></returns>
        public int DmcSetCardAxisSoftLimit(int CardNo, int axis, int enable, int source_sel, int SL_action, int N_limit, int P_limit)
        {
            //return ecat_motion.dmc_set_softlimit((ushort)CardNo, (ushort)axis, (ushort)enable, (ushort)source_sel, (ushort)SL_action, N_limit, P_limit);
            return 0;
        }

        /// <summary>
        /// 获取软限位设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="enable">返回使能状态</param>
        /// <param name="source_sel">返回计数器选择</param>
        /// <param name="SL_action">返回限位停止方式</param>
        /// <param name="N_limit">返回负限位脉冲数</param>
        /// <param name="P_limit">返回正限位脉冲数</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisSoftLimit(int CardNo, int axis, ref UInt16 enable, ref UInt16 source_sel, ref UInt16 SL_action, ref int N_limit, ref int P_limit)
        {
            //return ecat_motion.dmc_get_softlimit((ushort)CardNo, (ushort)axis, ref enable, ref source_sel, ref SL_action, ref N_limit, ref P_limit);
            return 0;
        }

        /// <summary>
        ///  设置指定轴的当前指令位置计数器值
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="pos">位置值，单位：unit</param>
        /// <returns></returns>
        public int DmcSetCardAxisPositionUnit(int CardNo, int axis, double pos)
        {
            return ecat_motion.M_SetCurrentPos((short)(axis + 1), (int)pos, (short)CardNo);
        }

        /// <summary>
        ///  获取指定轴当前位置
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="pos">返回当前位置值，单位：unit</param>
        /// <returns></returns>
        public int DmcGetCardAxisPositionUnit(int CardNo, int axis, ref double pos)
        {
            double[] PosArray = new double[1];
            var res = ecat_motion.M_GetCmd((short)(axis + 1), out PosArray, 1, (short)CardNo);
            pos = PosArray[0];
            return res;
        }

        //double GetAxisCmd(int Axis)

        #endregion

        #region 运动状态检测及控制函数

        /// <summary>
        /// 读取指定轴的当前单轴速度
        /// <para>注 意：</para>
        /// <para>当执行基于脉冲当量的插补运动时，使用该函数读取的为各轴的单轴速度</para>
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="current_speed">返回速度值，单位：unit/s</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisCurrentSpeedUnit(int CardNo, int axis, ref double current_speed)
        {
            double[] VelArray = new double[1];
            var res = ecat_motion.M_GetCmdVel((short)(axis + 1), out VelArray, 1, (short)CardNo);
            current_speed = VelArray[0];
            return res;
        }

        /// <summary>
        /// 检测指定轴的运动状态
        /// <para>注 意：</para>
        /// <para>此函数适用于单轴、PVT 运动</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <returns>0：指定轴正在运行，1：指定轴已停止</returns>
        public int DmcGetCardAxisCheckDone(int CardNo, int axis)
        {
            int[] code = new int[1];
            var res = ecat_motion.M_GetSts((short)(axis + 1), out code, 1, (short)CardNo);
            return ((code[0] >> 10) & 1);
        }

        /// <summary>
        /// 检测坐标系的运动状态, 0运动中 1空闲
        /// <para>注 意：</para>
        /// <para>此函数适用于插补运动</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">指定控制卡上的坐标系号（取值范围：1~2）</param>
        /// <returns>轴运动状态，0运动中 1空闲</returns>
        public int DmcGetCardCrdCheckDoneMulticoor(int CardNo, int crd, int Fifo)
        {
            short pSts = 0; short pCmdNum = 0; int pSpace = 0;
            var tes = ecat_motion.M_CrdStatus((short)(crd), out pSts, out pCmdNum, out pSpace, (short)Fifo, (short)CardNo);
            if (pSts == 0) pSts = 1;
            else if (pSts == 1) pSts = 0;
            else if (pSts == 2) pSts = 1;
            else if (pSts == 3) pSts = 1;
            return pSts;
        }

        /*
        short dmc_softltc_get_number(WORD CardNo,WORD latch,WORD axis,int *number)
            功 能：读取锁存个数
            参 数：CardNo 控制卡卡号
            latch 锁存器号
            axis 锁存对应轴号
            number 锁存个数
            返回值：错误代码
        */

        public int DmcSoftltcGetNumber(int CardNo, int latch, int axis, ref int number)
        {
            //return (int)ecat_motion.dmc_softltc_get_number((ushort)CardNo, (ushort)latch, (ushort)axis, ref number);
            return 0;
        }

        /// <summary>
        /// 读取指定轴有关运动信号的状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <returns>
        /// <para>* 1    EL+ 1：表示正硬限位信号 +EL 为 ON； 0：OFF</para>  
        /// <para>* 2    EL- 1：表示负硬限位信号–EL为 ON； 0：OFF</para> 
        /// <para>* 3    EMG 1：表示急停信号 EMG 为 ON； 0：OFF</para> 
        /// <para>* 4    ORG 1：表示原点信号 ORG 为 ON； 0：OFF</para> 
        /// <para>* 5    SL+ 1：表示正软限位信号+SL为ON； 0：OFF</para> 
        /// <para>* 6    SL- 1：表示负软限位信号-SL为ON； 0：OFF</para> 
        /// </returns>
        public int DmcGetCardAxisIOState(int CardNo, int axis)
        {
            int[] code = new int[1];
            var res = ecat_motion.M_GetSts((short)(axis + 1), out code, 1, (short)CardNo);
            return code[0];
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
        public int DmcCardAxisStop(int CardNo, int axis, int stop_mode)
        {
            return ecat_motion.M_StopSingleAxis((short)(axis + 1), (short)stop_mode, (short)CardNo);
        }

        /// <summary>
        /// 停止坐标系内所有轴的运动
        /// <para>注 意：</para>
        /// <para>此函数适用于插补运动</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="crd">指定控制卡上的坐标系号（取值范围：0~5）</param>
        /// <param name="stop_mode">制动方式，0：减速停止，1：立即停止</param>
        /// <returns>错误代码</returns>
        public int DmcCardCrdStopMulticoor(int CardNo, int crd, int stop_mode)
        {
            return ecat_motion.M_CrdStop((short)crd, (short)stop_mode, (short)CardNo);
        }

        /// <summary>
        /// 紧急停止（所有轴）
        /// <para>注 意：</para>
        /// <para>此函数适用于所有运动模式</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <returns>错误代码</returns>
        public int CardDmcEmgStop(int CardNo)
        {

            return ecat_motion.M_Stop(0xFFFFFFFF, 1, (short)CardNo);
        }

        #endregion

        #region 单轴运动速度曲线设置函数

        /// <summary>
        /// 设置单轴运动速度曲线
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="Min_Vel">起始速度，单位：unit/s</param>
        /// <param name="Max_Vel">最大速度，单位：unit/s</param>
        /// <param name="Tacc">加速时间，单位：s</param>
        /// <param name="Tdec">减速时间，单位：s</param>
        /// <param name="Stop_Vel">停止速度，单位：unit/s</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisProfileUnit(int CardNo, int axis, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Stop_Vel)
        {
            //return ecat_motion.dmc_set_profile_unit((ushort)CardNo, (ushort)axis, Min_Vel, Max_Vel, Tacc, Tdec, Stop_Vel);
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
        public int DmcGetCardAxisProfileUnit(int CardNo, int axis, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double Stop_Vel)
        {
            //return ecat_motion.dmc_get_profile_unit((ushort)CardNo, (ushort)axis, ref Min_Vel, ref Max_Vel, ref Tacc, ref Tdec, ref Stop_Vel);
            return 0;
        }

        /// <summary>
        /// 设置单轴速度曲线 S 段参数值
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="s_mode">保留参数，固定值为 0</param>
        /// <param name="s_para">S 段时间，单位：s；范围：0~1</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisSProfile(int CardNo, int axis, int s_mode, double s_para)
        {
            //return ecat_motion.dmc_set_s_profile((ushort)CardNo, (ushort)axis, (ushort)s_mode, s_para);
            return 0;

        }

        /// <summary>
        /// 读取单轴速度曲线 S 段参数值
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="s_mode">保留参数</param>
        /// <param name="s_para">返回设置的 S 段时间</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisSProfile(int CardNo, int axis, int s_mode, ref double s_para)
        {
            return 0;
            //return ecat_motion.dmc_get_s_profile((ushort)CardNo, (ushort)axis, (ushort)s_mode, ref s_para);
        }

        #endregion

        #region 单轴运动函数
        /// <summary>
        /// 点位运动
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="dist">目标位置，单位：unit </param>
        /// <param name="posi_mode">运动模式，0：相对坐标模式，1：绝对坐标模式</param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisPMoveUnit(int CardNo, int axis, double dist, ushort posi_mode, double start_vel, double max_vel, double acc, double dec)
        {
            //加减速同步雷赛单位：s
            short accs = (short)(acc * 1000);
            try
            {
                var speedDifferencevalue = max_vel - start_vel;
                acc = speedDifferencevalue / acc;
                dec = max_vel / dec;
                if (acc > 1000000000) acc = 1000000000;
                if (dec > 1000000000) dec = 1000000000;
            }
            catch
            {
                acc = 10; dec = 10;
            }
            ecat_motion.CmdPrm cmdPrm = new ecat_motion.CmdPrm
            {
                acc = acc,
                dec = dec,
                sTime = accs
            };
            ecat_motion.M_SetMove((short)(axis + 1), ref cmdPrm, (short)CardNo);
            if (posi_mode == 0)
                return ecat_motion.M_RelMove((short)(axis + 1), (int)dist, max_vel, (short)CardNo);
            else
                return ecat_motion.M_AbsMove((short)(axis + 1), (int)dist, max_vel, (short)CardNo);

        }

        /// <summary>
        /// 指定轴连续运动
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="dir">方向运动方向，0：负方向，1：正方向</param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisVMove(int CardNo, int axis, int dir, double start_vel, double max_vel, double acc, double dec)
        {
            /*
            short dmc_vmove(WORD CardNo, WORD axis, WORD dir)
            功 能：指定轴连续运动
            参 数：CardNo 控制卡卡号
            axis 指定轴号
             dir 运动方向，0：负方向，1：正方向
            返回值：错误代码
            */

            //加减速同步雷赛单位：s
            short accs = (short)(acc * 1000);
            try
            {
                var speedDifferencevalue = max_vel - start_vel;
                acc = speedDifferencevalue / acc;
                dec = max_vel / dec;
                if (acc > 1000000000) acc = 1000000000;
                if (dec > 1000000000) dec = 1000000000;
            }
            catch
            {
                acc = 10; dec = 10;
            }
            ecat_motion.CmdPrm cmdPrm = new ecat_motion.CmdPrm
            {
                acc = acc,
                dec = dec,
                sTime = accs
            };
            ecat_motion.M_SetMove((short)(axis + 1), ref cmdPrm, (short)CardNo);

            return ecat_motion.M_Jog((short)(axis + 1), max_vel, (short)CardNo);
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
        public int DmcCardAxisChangeSpeed(int CardNo, int axis, double New_Vel, double Taccdec)
        {
            //return ecat_motion.dmc_change_speed_unit((ushort)CardNo, (ushort)axis, New_Vel, Taccdec);
            return 0;
        }

        /// <summary>
        /// 在线改变指定轴的当前运动位置
        /// <para> 注 意：</para>
        /// <para>1）该函数只适用于点位运动中的变位</para>
        ///  <para>2）参数 New_Pos 为绝对位置值，无论当前的运动模式为绝对坐标还是相对坐标模式 </para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="New_Pos">新目标位置，单位：unit</param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisResetTargetPositionUnit(int CardNo, int axis, double New_Pos)
        {
            //return ecat_motion.dmc_reset_target_position_unit((ushort)CardNo, (ushort)axis, New_Pos);
            return 0;
        }

        /// <summary>
        /// 强行变位(强行改变指定轴的当前目标位置（在线/非在线）)
        /// <para> 注 意：</para>
        /// <para>1）该函数适用于指定轴停止状态或点位运动中的变位</para>
        ///  <para>2）参数 New_Pos 为绝对位置值，无论当前的运动模式为绝对坐标还是相对坐标模式 </para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <param name="New_Pos">新的点位</param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisUpdateTargetPosition(int CardNo, int axis, double New_Pos)
        {
            //return ecat_motion.dmc_update_target_position_unit((ushort)CardNo, (ushort)axis, New_Pos);
            return 0;
        }
        #endregion

        #region 通用输入输出 IO 函数

        /// <summary>
        /// 读取指定控制卡的某个输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号，取值范围： 0~7，如果扩展 IO 模块，依次往后累加 </param>
        /// <returns>指定的输入端口电平：0：低电平，1：高电平</returns>
        public int DmcGetCardInPortNoBit(int CardNo, int bitno)
        {
            //return ecat_motion.dmc_read_inbit((ushort)CardNo, (ushort)bitno);
            return 0;

        }
        /// <summary>
        /// 设置指定控制卡的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输出端口号，取值范围： 0~7，如果扩展 IO 模块，依次往后累加</param>
        /// <param name="on_off">输出电平，0：低电平，1：高电平</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardBitNoInBit(int CardNo, int bitno, int on_off)
        {
            //return ecat_motion.dmc_write_outbit((ushort)CardNo, (ushort)bitno, (ushort)on_off);
            return 0;

        }

        /// <summary>
        /// 读取指定控制卡的某个输出端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号，取值范围： 0~7，如果扩展 IO 模块，依次往后累加 </param>
        /// <returns>指定输出端口的电平，0：低电平，1：高电平</returns>
        public int DmcGetCardBitNoOutBit(int CardNo, int bitno)
        {
            //return ecat_motion.dmc_read_outbit((ushort)CardNo, (ushort)bitno);
            return 0;
        }

        /// <summary>
        /// 读取指定控制卡的全部输入端口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="portno">保留参数，固定值为 0；所有输入口按顺序排列，每 32bit 为一个 port口号，例如 portno = 0 表示 in0-in31，portno=1 表示 in32-in63，以此类推；</param>
        /// <returns>
        /// <para>portno参数</para>
        /// <para>函数返回值的 bit：0 ；输入口号：0 ；输入口名称：IN0</para>
        /// <para>函数返回值的 bit：1 ；输入口号：1 ；输入口名称：IN1</para>
        /// <para>函数返回值的 bit：2 ；输入口号： 2；输入口名称： IN2</para>
        /// <para>函数返回值的 bit：3 ；输入口号：3 ；输入口名称：IN3</para>
        /// <para> 函数返回值的 bit：4 ；输入口号： 4 ；输入口名称：IN4</para>
        /// <para>函数返回值的 bit：5 ；输入口号：5 ；输入口名称：IN5</para>
        /// <para>函数返回值的 bit：6 ；输入口号：6 ；输入口名称：IN6</para>
        /// <para>函数返回值的 bit：7 ；输入口号： 7 ；输入口名称：IN7</para>
        /// <para>函数返回值的 bit：8-31 8-31 ；输入口名称：扩展输入口</para>
        /// </returns>
        public int DmcGetCardPortNoInPort(int CardNo, int portno)
        {
            //return (int)ecat_motion.dmc_read_inport((ushort)CardNo, (ushort)portno);
            return 0;
        }

        /// <summary>
        /// 读取指定控制卡的全部输出口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="portno">保留参数，固定值为 0；所有输出口按顺序排列，每 32bit 为一个 port口号，例如 portno=0 表示 out0-out31，portno=1 表示 out32-out63，以此类推；</param>
        /// <returns>
        /// <para>portno 参数</para>
        /// <para>函数返回值的 bit：  0 ；输入口号：0 ；输入口名称：OUT0</para>
        /// <para>函数返回值的 bit：1 ；输入口号：1 ；输入口名称：OUT1</para>
        /// <para>函数返回值的 bit：2 ；输入口号： 2；输入口名称： OUT2</para>
        /// <para>函数返回值的 bit：3 ；输入口号：3 ；输入口名称：OUT3</para>
        /// <para> 函数返回值的 bit：4 ；输入口号： 4 ；输入口名称：OUT1</para>
        /// <para>函数返回值的 bit：5 ；输入口号：5 ；输入口名称：OUT2</para>
        /// <para>函数返回值的 bit：6 ；输入口号：6 ；输入口名称：OUT6</para>
        /// <para>函数返回值的 bit：7 ；输入口号： 7 ；输入口名称：OUT7</para>
        /// <para>函数返回值的 bit：8-31 8-31 ；输入口名称：扩展输入口</para>
        /// </returns>
        public uint DmcGetCardPortNoOutPort(int CardNo, int portno)
        {
            //return ecat_motion.dmc_read_outport((ushort)CardNo, (ushort)portno);
            return 0;
        }

        /// <summary>
        /// 设置指定控制卡的全部输出口的电平
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="portno">保留参数，固定值为 0；所有输出口按顺序排列，每 32bit 为一个 port口号，例如 portno = 0 表示 out0-out31，portno=1 表示 out32-out63，以此类推；</param>
        /// <param name="outport_val">
        /// <para>portno 参数</para>
        /// <para>函数返回值的 bit：  0 ；输入口号：0 ；输入口名称：OUT0</para>
        /// <para>函数返回值的 bit：1 ；输入口号：1 ；输入口名称：OUT1</para>
        /// <para>函数返回值的 bit：2 ；输入口号： 2；输入口名称： OUT2</para>
        /// <para>函数返回值的 bit：3 ；输入口号：3 ；输入口名称：OUT3</para>
        /// <para> 函数返回值的 bit：4 ；输入口号： 4 ；输入口名称：OUT1</para>
        /// <para>函数返回值的 bit：5 ；输入口号：5 ；输入口名称：OUT2</para>
        /// <para>函数返回值的 bit：6 ；输入口号：6 ；输入口名称：OUT6</para>
        /// <para>函数返回值的 bit：7 ；输入口号： 7 ；输入口名称：OUT7</para>
        /// <para>函数返回值的 bit：8-31 8-31 ；输入口名称：扩展输入口</para>
        /// </param>
        /// <returns>错误代码</returns>
        public int DmcSetCardPortNoOutPort(int CardNo, int portno, int outport_val)
        {
            //return ecat_motion.dmc_write_outport((ushort)CardNo, (ushort)portno, (uint)outport_val);
            return 0;
        }

        /// <summary>
        /// IO 输出延时翻转
        /// <para>注 意：</para>
        /// <para>延时翻转时间参数设置为 0 时，此时延时翻转时间将为无限大</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输出端口号，取值范围：0~7，如果扩展 IO 模块，依次往后累加</param>
        /// <param name="reverse_time">延时翻转时间，单位：s</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardIOReverseOutbit(int CardNo, int bitno, int reverse_time)
        {
            //return ecat_motion.dmc_reverse_outbit((ushort)CardNo, (ushort)bitno, (ushort)reverse_time);
            return 0;

        }

        /// <summary>
        /// 设置 IO 计数模式；
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号，取值范围：0~7，如果扩展 IO 模块，依次往后累加</param>
        /// <param name="mode">IO 计数模式，0：禁用，1：上升沿计数，2：下降沿计数</param>
        /// <param name="filter_time">滤波时间，单位：s，保留参数</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardIOCountMode(int CardNo, int bitno, int mode, double filter_time)
        {
            //return ecat_motion.dmc_set_io_count_mode((ushort)CardNo, (ushort)bitno, (ushort)mode, filter_time);
            return 0;

        }

        /// <summary>
        /// 读取 IO 计数模式设置；
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号，取值范围：0~7，如果扩展 IO 模块，依次往后累加</param>
        /// <param name="mode">返回 IO 计数模式</param>
        /// <param name="filter_time">返回滤波时间，单位：s，保留参数</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardIOCountMode(int CardNo, int bitno, ref UInt16 mode, ref double filter_time)
        {
            //return ecat_motion.dmc_get_io_count_mode((ushort)CardNo, (ushort)bitno, ref mode, ref filter_time);
            return 0;

        }

        /// <summary>
        /// 设置 IO 计数值
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号，取值范围：0~7，如果扩展 IO 模块，依次往后累加</param>
        /// <param name="CountValue">IO 计数值</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardIOCountValue(int CardNo, int bitno, int CountValue)
        {
            //return ecat_motion.dmc_set_io_count_value((ushort)CardNo, (ushort)bitno, (UInt32)CountValue);
            return 0;

        }

        /// <summary>
        /// 读取 IO 计数值；
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="bitno">输入端口号，取值范围：0~7，如果扩展 IO 模块，依次往后累加</param>
        /// <param name="CountValue">返回 IO 计数值</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardIOCountValue(int CardNo, int bitno, ref UInt32 CountValue)
        {
            //return ecat_motion.dmc_get_io_count_value((ushort)CardNo, (ushort)bitno, ref CountValue);
            return 0;

        }
        #endregion

        #region 手轮功能函数
        /// <summary>
        /// 设置单轴手轮运动控制输入方式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="inmode">手轮输入方式，0：脉冲+方向信号；1：A、B 相位正交信号</param>
        /// <param name="multi">跟随方向，0：双向跟随，1：正向跟随，-1：负向跟随</param>
        /// <param name="vh">保留参数，固定值为 0</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisHandwheelInMode(int CardNo, int axis, int inmode, int multi, double vh)
        {
            return ecat_motion.M_Gear((short)(axis + 1), (short)multi, (short)CardNo);
        }

        /// <summary>
        /// 读取单轴手轮运动控制输入方式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="inmode">返回手轮输入方式</param>
        /// <param name="multi">返回手轮倍率</param>
        /// <param name="vh">保留参数</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisHandwheelInMode(int CardNo, int axis, ref UInt16 inmode, ref int multi, ref double vh)
        {
            //return ecat_motion.dmc_get_handwheel_inmode((ushort)CardNo, (ushort)axis, ref inmode, ref multi, ref vh);
            return 0;
        }

        /// <summary>
        /// 启动手轮运动
        /// <para>注 意：</para>
        /// <para>当启动手轮运动后，只有发送 dmc_stop 或 dmc_emg_stop 命令后才会退出手轮模式</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisHandwheelMove(int CardNo, int axis)
        {
            //return ecat_motion.dmc_handwheel_move((ushort)CardNo, (ushort)axis);
            return 0;
        }

        /// <summary>
        /// 设置多轴手轮运动控制输入方式
        /// <para>注 意：</para>
        /// <para>通过该函数设置可以使一个手轮通道控制多个轴同时运动，运动倍率都以第一个轴的倍率。</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="inmode">手轮输入方式：0：脉冲+方向信号；1：A、B 相位正交信号</param>
        /// <param name="AxisNum">参与手轮运动的轴数</param>
        /// <param name="AxisNums">参与手轮运动的轴号数组</param>
        /// <param name="multi">手轮倍率数组:正数表示默认方向，负数表示与默认方向反向</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardHandwheelInModeExtern(int CardNo, int inmode, int AxisNum, UInt16[] AxisNums, int[] multi)
        {
            //return ecat_motion.dmc_set_handwheel_inmode_extern((ushort)CardNo, (ushort)inmode, (ushort)AxisNum, AxisNums, multi);
            return 0;
        }

        /// <summary>
        /// 读取多轴手轮运动控制输入方式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="inmode">返回手轮输入方式</param>
        /// <param name="AxisNum">返回参与手轮运动的轴数</param>
        /// <param name="AxisNums">返回参与手轮运动的轴号数组</param>
        /// <param name="multi">返回手轮倍率数组</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardHandwheelInModeExtern(int CardNo, ref UInt16 inmode, ref UInt16 AxisNum, ref UInt16[] AxisNums, ref int[] multi)
        {
            return 0;
            //return ecat_motion.dmc_get_handwheel_inmode_extern((ushort)CardNo, ref inmode, ref AxisNum, AxisNums, multi);
        }
        #endregion

        #region 编码器函数
        /// <summary>
        /// 设置指定轴的当前编码器计数值
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="pos">编码器计数值，单位：unit</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisEncoderUnit(int CardNo, int axis, double pos)
        {
            //return ecat_motion.dmc_set_encoder_unit((ushort)CardNo, (ushort)axis, pos);
            return 0;
        }

        /// <summary>
        /// 读取指定轴的当前编码器计数值
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="pos">返回当前编码器计数值，单位：unit</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisEncoderUnit(int CardNo, int axis, ref double pos)
        {
            int[] poss = new int[1];
            var res = ecat_motion.M_ReadActualPosition((short)(axis + 1), out poss, 1, (short)CardNo);
            pos = poss[0];
            return res;
        }

        /// <summary>
        /// 设置辅助编码器输入方式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="channel">辅助编码器通道，0，通道 0，1，通道 1</param>
        /// <param name="inmode">辅助编码器输入方式，0：脉冲+方向信号；1：A、B 相位正交信号</param>
        /// <param name="multi">辅助编码器计数模式，固定 1：4 倍频计数</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardExtraEncoderMode(int CardNo, int channel, int inmode, int multi)
        {
            //return ecat_motion.dmc_set_extra_encoder_mode((ushort)CardNo, (ushort)channel, (ushort)inmode, (ushort)multi);
            return 0;
        }

        /// <summary>
        /// 读取辅助编码器输入方式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="channel">辅助编码器通道</param>
        /// <param name="inmode">返回辅助编码器输入方式</param>
        /// <param name="multi">返回辅助编码器计数模式</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardExtraEncoderMode(int CardNo, int channel, ushort inmode, ushort multi)
        {
            //return ecat_motion.dmc_get_extra_encoder_mode((ushort)CardNo, (ushort)channel, ref inmode, ref multi);
            return 0;
        }

        #endregion

        #region 高速位置锁存函数
        /// <summary>
        /// 配置锁存器
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="latch">锁存器号，0-3</param>
        /// <param name="ltc_mode">锁存模式，0：单次锁存，1：连续锁存</param>
        /// <param name="ltc_logic">锁存信号的触发方式，0：下降沿锁存，1：上升沿锁存，2：双边沿锁存</param>
        /// <param name="filter">滤波时间，单位：us</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardLtcMode(int CardNo, int latch, int ltc_mode, int ltc_logic, double filter)
        {
            return 0;
            //return ecat_motion.dmc_ltc_set_mode((ushort)CardNo, (ushort)latch, (ushort)ltc_mode, (ushort)ltc_logic, filter);
        }

        /// <summary>
        /// 读取锁存器配置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="latch">锁存器号,0-3</param>
        /// <param name="ltc_mode">读取锁存模式，0：单次锁存，1：连续锁存</param>
        /// <param name="ltc_logic">读取锁存信号的触发方式，0：下降沿锁存，1：上升沿锁存，2：双边沿锁存</param>
        /// <param name="filter">读取滤波时间，单位：us</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardLtcMode(int CardNo, int latch, ushort ltc_mode, ushort ltc_logic, double filter)
        {
            return 0;
            //return ecat_motion.dmc_ltc_get_mode((ushort)CardNo, (ushort)latch, ref ltc_mode, ref ltc_logic, ref filter);
        }

        /// <summary>
        /// 配置锁存源
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="latch">锁存器号,0-3</param>
        /// <param name="axis">锁存器辅助编码器通道号，0、1</param>
        /// <param name="ltc_source">锁存源，0：指令位置，1：辅助编码器计数</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardLtcSource(int CardNo, int latch, int axis, int ltc_source)
        {
            return 0;
            //return ecat_motion.dmc_ltc_set_source((ushort)CardNo, (ushort)latch, (ushort)axis, (ushort)ltc_source);
        }

        /// <summary>
        /// 读取锁存源配置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="latch">锁存器号,0-3</param>
        /// <param name="axis">锁存器辅助编码器通道号</param>
        /// <param name="ltc_source">读取锁存源，0：指令位置，1：辅助编码器计数</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardLtcSource(int CardNo, int latch, int axis, ushort ltc_source)
        {
            return 0;
            //return ecat_motion.dmc_ltc_get_source((ushort)CardNo, (ushort)latch, (ushort)axis, ref ltc_source);
        }

        /// <summary>
        /// 读取锁存值
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="latch">锁存器号,0-3</param>
        /// <param name="axis">锁存器辅助编码器通道号</param>
        /// <param name="value">锁存值</param>
        /// <returns>锁存值
        /// <para>注 意：</para>
        /// <para>1）该函数只可以读辅助编码器锁存值；</para>
        /// <para>2）当选择锁存方式为连续锁存时，该函数第一次执行的时候，读取的是锁存器的第一个锁存值，第二次执行的时候，读取的是锁存器的第二个锁存值，以此类推；</para>
        /// <para>3）单次锁存时，调用 dmc_get_latch_value 不会自动清除已锁存个数，须调用 dmc_reset_latch_flag；</para>
        /// </returns>
        public int DmcGetCardLtcValueUnit(int CardNo, int latch, int axis, ref double value)
        {
            return 0;
            //return ecat_motion.dmc_ltc_get_value_unit((ushort)CardNo, (ushort)latch, (ushort)axis, ref value);
        }

        /// <summary>
        /// 读取锁存器锁存个数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="latch">锁存器号,0-3</param>
        /// <param name="axis">锁存器辅助编码器通道号</param>
        /// <param name="number">读取锁存器个数</param>
        /// <returns>已锁存个数，0 表示无锁存值</returns>
        public int DmcGetCardLtcNumber(int CardNo, int latch, int axis, ref int number)
        {
            return 0;
            //return ecat_motion.dmc_ltc_get_number((ushort)CardNo, (ushort)latch, (ushort)axis, ref number);
        }

        /// <summary>
        /// 复位锁存器
        /// <para> 注 意：</para>
        /// <para>当使用锁存功能前，必须先调用此函数复位锁存器的标志位</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="latch">锁存器号,0-3</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardLtcReset(int CardNo, int latch)
        {
            return 0;
            //return ecat_motion.dmc_ltc_reset((ushort)CardNo, (ushort)latch);
        }

        #endregion

        #region 位置比较函数

        /// <summary>
        /// 设置一维位置比较器 (单轴低速)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号 </param>
        /// <param name="enable">比较功能状态，0：禁止，1：使能 </param>
        /// <param name="cmp_source">比较源，0：指令位置计数器，1：编码器计数器</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisCompareConfig(int CardNo, int axis, int enable, int cmp_source)
        {
            return 0;
            //return ecat_motion.dmc_compare_set_config((ushort)CardNo, (ushort)axis, (ushort)enable, (ushort)cmp_source);
        }

        /// <summary>
        /// 读取一维位置比较器设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="enable">返回比较功能状态</param>
        /// <param name="cmp_source">返回比较源</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisCompareConfig(int CardNo, int axis, ref UInt16 enable, ref UInt16 cmp_source)
        {
            return 0;
            //return ecat_motion.dmc_compare_get_config((ushort)CardNo, (ushort)axis, ref enable, ref cmp_source);
        }

        /// <summary>
        /// 清除已添加的所有一维位置比较点
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号 </param>
        /// <returns>错误代码</returns>
        public int DmcClearCardAxisComparePoints(int CardNo, int axis)
        {
            return 0;
            //return ecat_motion.dmc_compare_clear_points((ushort)CardNo, (ushort)axis);
        }

        /// <summary>
        /// 添加一维位置比较点
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号 </param>
        /// <param name="pos">设置比较位置为 X pulse</param>
        /// <param name="dir">设置比较模式</param>
        /// <param name="action">设置触发功能（为 IO 电平取反）</param>
        /// <param name="actpara">设置输出 IO 端口 (0 触发功能)</param>
        /// <returns>错误代码</returns>
        public int DmcAddCardAxisComparePoints(int CardNo, int axis, int pos, int dir, int action, int actpara)
        {
            return 0;
            //return ecat_motion.dmc_compare_add_point((ushort)CardNo, (ushort)axis, pos, (ushort)dir, (ushort)action, (uint)actpara);
        }

        /// <summary>
        /// 添加一维位置比较点
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="pos">比较位置</param>
        /// <param name="dir">比较模式，0：小于等于，1：大于等于</param>
        /// <param name="bitno">比较输出口</param>
        /// <param name="cycle">周期个数</param>
        /// <param name="level">比较输出点电平（level 只有当 cycle 为 0 的时候起作用，cycle 为 0 的时候触发时输出 level 电平）</param>
        /// <returns>错误代码</returns>
        public int DmcAddCardAxisComparePointsCycle(int CardNo, int axis, int pos, int dir, int bitno, int cycle, int level)
        {
            //return ecat_motion.dmc_compare_add_point_cycle((ushort)CardNo, (ushort)axis, pos, (ushort)dir, (ushort)bitno, (ushort)cycle, (ushort)level);
            return 0;
            //return ecat_motion.dmc_compare_add_point_cycle_unit((ushort)CardNo, (ushort)axis, (double)pos, (ushort)dir, (ushort)bitno, (ushort)cycle, (ushort)level);
        }

        /// <summary>
        /// 读取当前一维比较点位置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号 </param>
        /// <param name="pos">返回当前比较点位置 </param>       
        /// <returns></returns>
        public int DmcGetCardAxisCompareCurrentPoint(int CardNo, int axis, ref int pos)
        {
            return 0;
            //return ecat_motion.dmc_compare_get_current_point((ushort)CardNo, (ushort)axis, ref pos);
        }
        /// <summary>
        /// 查询已经比较过的一维比较点个数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="pointNum">返回已经比较过的点数</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisComparPointsRunned(int CardNo, int axis, ref int pointNum)
        {
            return 0;
            //return ecat_motion.dmc_compare_get_points_runned((ushort)CardNo, (ushort)axis, ref pointNum);
        }

        /// <summary>
        /// 查询可以加入的一维比较点个数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="pointNum">返回可以加入的比较点数</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisComparPointsRemained(int CardNo, int axis, ref int pointNum)
        {
            return 0;
            //return ecat_motion.dmc_compare_get_points_remained((ushort)CardNo, (ushort)axis, ref pointNum);
        }
        #endregion

        #region 高速位置比较函数
        /// <summary>
        ///设置高速比较模式
        ///<para>注  意：</para>
        ///<para>1）当选择模式 1 时，只有当前位置等于比较位置时，CMP 端口才输出有效电平</para>
        ///<para>2）当选择模式 2 时，只要当前位置小于比较位置时，CMP 端口就一直保持有效电平</para>
        ///<para>3）当选择模式 3 时，只要当前位置大于比较位置时，CMP 端口就一直保持有效电平</para>
        ///<para>4）当选择模式 4 或 5 时，CMP 端口输出有效电平的时间通过 dmc_hcmp_set_config函数的 time 参数（脉冲宽度）设置</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口） </param>
        /// <param name="cmp_mode">比较模式：0：禁止（默认值）| 1：等于 | 2：小于 | 3：大于 | 4：队列 | ;提供 127 个点比较空间，采用先添加先比较，比较完可追加比较点，也可一次性添加多个比较点 | 5：线性，提供起始比较点，位置增量，比较次数  </param>
        /// <returns></returns>
        public int DmcSetCardHcmpMode1(int CardNo, int hcmp, int cmp_mode)
        {
            return 0;
            //return ecat_motion.dmc_hcmp_set_mode((ushort)CardNo, (ushort)hcmp, (ushort)cmp_mode);
        }

        /// <summary>
        /// 读取高速比较模式设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口） </param>
        /// <param name="cmp_mode">返回比较模式设置</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardHcmpMode1(int CardNo, int hcmp, ref UInt16 cmp_mode)
        {
            return 0;
            //return ecat_motion.dmc_hcmp_get_mode((ushort)CardNo, (ushort)hcmp, ref cmp_mode);
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
        public int DmcSetCardAxisHcmpConfig(int CardNo, int hcmp, int axis, int cmp_source, int cmp_logic, int time)
        {
            return 0;
            //return ecat_motion.dmc_hcmp_set_config((ushort)CardNo, (ushort)hcmp, (ushort)axis, (ushort)cmp_source, (ushort)cmp_logic, time);
        }
        /// <summary>
        /// 读取高速比较器配置 (单轴高速)
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口）</param>
        /// <param name="axis">辅助编码器通道号 </param>
        /// <param name="cmp_source">比较位置源，固定值 1：辅助编码器</param>
        /// <param name="cmp_logic">有效电平：0：低电平，1：</param>
        /// <param name="time">脉冲宽度，单位：us，取值范围：1us~20s </param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisHcmpConfig(int CardNo, int hcmp, ref ushort axis, ref ushort cmp_source, ref ushort cmp_logic, ref int time)
        {
            return 0;
            //return ecat_motion.dmc_hcmp_get_config((ushort)CardNo, (ushort)hcmp, ref axis, ref cmp_source, ref cmp_logic, ref time);
        }

        /// <summary>
        /// 添加/更新高速比较位置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口） </param>
        /// <param name="pos">1：队列模式下：添加比较位置，单位：pulse | 2：线性模式下：更新起始比较位置，单位：pulse | 3：其他模式下：更新比较位置，单位：pulse</param>
        /// <returns>错误代码</returns>
        public int DmcAddCardHcmpPoint(int CardNo, int hcmp, int pos)
        {
            return 0;
            //return ecat_motion.dmc_hcmp_add_point((ushort)CardNo, (ushort)hcmp, pos);
        }

        /// <summary>
        /// 设置高速比较线性模式参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口）</param>
        /// <param name="Increment">位置增量值，单位：pulse（正值表示位置递增，负值表示位置递减）</param>
        /// <param name="Count">比较次数，取值范围：1~65535</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardHcmpLiner(int CardNo, int hcmp, int Increment, int Count)
        {
            return 0;
            //return ecat_motion.dmc_hcmp_set_liner((ushort)CardNo, (ushort)hcmp, Increment, Count);
        }

        /// <summary>
        /// 读取高速比较线性模式参数设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口）</param>
        /// <param name="Increment">返回位置增量值设置</param>
        /// <param name="Count">返回比较次数设置</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardHcmpLiner(int CardNo, int hcmp, ref int Increment, ref int Count)
        {
            return 0;
            //return ecat_motion.dmc_hcmp_get_liner((ushort)CardNo, (ushort)hcmp, ref Increment, ref Count);
        }

        /// <summary>
        /// 清除已添加的所有高速位置比较点
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口）</param>
        /// <returns>错误代码</returns>
        public int DmcClearCardHcmpPoints(int CardNo, int hcmp)
        {
            return 0;
            //return ecat_motion.dmc_hcmp_clear_points((ushort)CardNo, (ushort)hcmp);
        }

        /// <summary>
        /// 读取高速比较参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="hcmp">比较器号，取值范围：0~5（对应硬件 OUT2~OUT7 端口）</param>
        /// <param name="remained_points">返回可添加比较点数</param>
        /// <param name="current_point">返回当前比较点位置，单位：pulse</param>
        /// <param name="runned_points">返回已比较点数</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardHcmpCurrentState(int CardNo, int hcmp, ref int remained_points, ref int current_point, ref int runned_points)
        {
            return 0;
            //return ecat_motion.dmc_hcmp_get_current_state((ushort)CardNo, (ushort)hcmp, ref remained_points, ref current_point, ref runned_points);
        }
        #endregion

        #region 异常信号接口函数
        /// <summary>
        /// 设置EMG信号
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="enable">使能状态，0：禁止，1：允许</param>
        /// <param name="emg_logic">有效电平：0：低电平，1：高电</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisEmgMode(int CardNo, int axis, int enable, int emg_logic)
        {
            return ecat_motion.M_SetEmgInv((short)(emg_logic == 1 ? 0 : 1), (short)CardNo);
        }

        /// <summary>
        /// 获取EMG信号
        /// </summary>
        /// <param name="CardNo">返回控制卡卡号</param>
        /// <param name="axis">返回指定轴号</param>
        /// <param name="enable">返回使能状态，0：禁止，1：允许</param>
        /// <param name="emg_logic">返回有效电平：0：低电平，1：高电</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisEmgMode(int CardNo, int axis, ref UInt16 enable, ref UInt16 emg_logic)
        {
            short emg = 0;
            var res = ecat_motion.M_GetEmgInv(ref emg, (short)CardNo);
            emg_logic = (ushort)emg;
            return res;
        }

        /// <summary>
        /// 外部减速停止信号及减速停止时间设置，秒为单位（适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="enable">使能状态，0：禁止，1：允许</param>
        /// <param name="logic">有效电平：0：低电平，1：高电</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisIODstpMode(int CardNo, int axis, int enable, int logic)
        {
            return 0;
            //return ecat_motion.dmc_set_io_dstp_mode((ushort)CardNo, (ushort)axis, (ushort)enable, (ushort)logic);
        }

        /// <summary>
        /// 外部减速停止信号及减速停止时间获取，秒为单位（适用于所有脉冲卡）
        /// </summary>
        /// <param name="CardNo">返回控制卡卡号</param>
        /// <param name="axis">返回指定轴号</param>
        /// <param name="enable">返回使能状态，0：禁止，1：允许</param>
        /// <param name="logic">返回有效电平：0：低电平，1：高电</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisIODstpMode(int CardNo, int axis, ref UInt16 enable, ref UInt16 logic)
        {
            return 0;
            //return ecat_motion.dmc_get_io_dstp_mode((ushort)CardNo, (ushort)axis, ref enable, ref logic);
        }

        /// <summary>
        /// 设置减速停止时间
        /// <para>注 意：</para>
        /// <para>1. 当发生异常停止时，如：限位信号（软硬件）被触发、减速停止信号(DSTP)被触发等进行减速停止时，减速停止时间都为 dmc_set_dec_stop_time 函数里设置的减速时间；</para>
        /// <para>2. T 型速度规划，电机实际停止时间等于设置的异常减速停止时间；S 型速度规划，电机实际停止时间等于设置的异常减速停止时间与 S 段时间之和；</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="stop_time"> 减速时间，单位：s</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisDecStopTime(int CardNo, int axis, double stop_time)
        {
            return 0;
            //return ecat_motion.dmc_set_dec_stop_time((ushort)CardNo, (ushort)axis, stop_time);
        }

        /// <summary>
        /// 读取减速停止时间设置；
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="stop_time">返回设置的减速时间，单位：s</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisDecStopTime(int CardNo, int axis, ref double stop_time)
        {
            return 0;
            //return ecat_motion.dmc_get_dec_stop_time((ushort)CardNo, (ushort)axis, ref stop_time);
        }
        #endregion

        #region 检测轴到位状态函数
        /// <summary>
        /// 设置位置误差带
        /// <para>编码器系数的说明：</para>
        /// <para>当使用 dmc_check_success_encoder 函数检测编码器是否到位时，其用于判断的编码器位置为：编码器计数值乘以编码器系数的值。</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="factor">编码器系数</param>
        /// <param name="error">位置误差带，单位：pulse</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardAxisFactorError(int CardNo, int axis, double factor, int error)
        {
            return ecat_motion.M_SetAxisBand((short)(axis + 1), (uint)error, (uint)factor, (short)CardNo);
        }

        /// <summary>
        /// 读取位置误差带设置
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="factor">返回编码器系数设置</param>
        /// <param name="error">返回位置误差带设置</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisFactorError(int CardNo, int axis, ref double factor, ref int error)
        {
            uint band = 0; uint time;
            var res = ecat_motion.M_GetAxisBand((short)(axis + 1), out band, out time, (short)CardNo);
            error = (int)band;
            factor = (double)time;
            return res;
        }

        /// <summary>
        /// 检测指令到位  
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <returns></returns>
        public int DmcGetCardAxisCheckSuccessPulse(int CardNo, int axis)
        {
            //return ecat_motion.dmc_check_success_pulse((ushort)CardNo, (ushort)axis);
            return 0;
        }

        /// <summary>
        /// 检测编码器到位
        /// <para>注 意：</para>
        /// <para>1）该函数只适用于单轴运动 </para>
        /// <para>2）检测函数请在 dmc_check_done 检测到轴停止后调用，函数调用后会等待轴到位后，如果调用函数 100ms 内未到位，函数超时返回认为不到位</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <returns>0：表示编码器位置在设定的目标位置的误差带之外；1：表示编码器位置在设定的目标位置的误差带之内</returns>
        public int DmcGetCardAxisCheckSuccessEncoder(int CardNo, int axis)
        {
            //return ecat_motion.dmc_check_success_encoder((ushort)CardNo, (ushort)axis);
            return 0;
        }
        #endregion

        #region 密码管理函数
        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="new_sn">新密码，密码长度不大于 255 个字符</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardWriteSN(int CardNo, string new_sn)
        {
            //return ecat_motion.dmc_write_sn((ushort)CardNo, new_sn);
            return 0;
        }

        /// <summary>
        /// 密码校验
        /// <para>注 意：</para>
        /// <para>1）用户可以在系统软件开启时加入密码校验动作，以此对系统软件进行加密</para>
        /// <para>2）密码校验失败 3 次后，无法进行校验；若需再次校验，需将电脑关机，断电重启</para>
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="check_sn">旧密码</param>
        /// <returns>校验状态，0：失败，1：成功</returns>
        public int DmcGetCardCheckSN(int CardNo, string check_sn)
        {
            return 0;
            //return ecat_motion.dmc_check_sn((ushort)CardNo, check_sn);
        }
        #endregion

        #region 打印输出函数

        /// <summary>
        /// 函数调用打印输出设置
        /// </summary>
        /// <param name="mode">打印输出模式，0：只打印报错函数，1：全部打印，2：全部不打印</param>
        /// <param name="FileName">文件保存路径：参数文件名+后缀：相对路径；完整描述参数文件的路径+文件名后缀：绝对路径</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardDebugMode(int mode, string FileName)
        {
            //return ecat_motion.dmc_set_debug_mode((ushort)mode, FileName);
            return 0;
        }

        /// <summary>
        /// 读取函数调用打印输出设置
        /// </summary>
        /// <param name="mode">返回打印输出使能状态</param>
        /// <param name="FileName">返回文件保存路径</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardDebugMode(ref UInt16 mode, IntPtr FileName)
        {
            return 0;
            //return ecat_motion.dmc_get_debug_mode(ref mode, FileName);
        }
        #endregion

        #region 状态检测

        /// <summary>
        /// 获取轴运动模式：
        /// 1为pp模式，6为回零模式，8为csp模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴编号</param>
        /// <returns>轴运动模式</returns>
        public int DmcGetCardAxisRunMode(int CardNo, int axis, ref UInt16 run_mode)
        {
            //int[] code = new int[1];
            //var res = ecat_motion.M_GetSts((short)(axis + 1), out code, 1, (short)CardNo);
            //run_mode = (code[0]>>)
            //return res;
            return 0;
        }
        /// <summary>
        /// 读取指定轴的停止原因
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <param name="StopReason">停止原因：
        ///<para> 0：正常停止</para>
        ///<para>1：保留</para>
        ///<para>2：保留</para>
        ///<para>3：LTC 外部触发立即停止，IMD_STOP_AT_LTC</para>
        ///<para>4：EMG 立即停止，IMD_STOP_AT_EMG</para>
        ///<para>5：正硬限位立即停止，IMD_STOP_AT_ELP</para>
        ///<para>6：负硬限位立即停止，IMD_STOP_AT_ELN</para>
        ///<para>7：正硬限位减速停止，DEC_STOP_AT_ELP</para>
        ///<para>8：负硬限位减速停止，DEC_STOP_AT_ELN</para>
        ///<para>9：正软限位立即停止，IMD_STOP_AT_SOFT_ELP</para>
        ///<para>10：负软限位立即停止，IMD_STOP_AT_SOFT_ELN</para>
        ///<para>11：正软限位减速停止，DEC_STOP_AT_SOFT_ELP</para>
        ///<para>12：负软限位减速停止，DEC_STOP_AT_SOFT_ELN</para>
        ///<para>13：命令立即停止，IMD_STOP_AT_CMD</para>
        ///<para>14：命令减速停止，DEC_STOP_AT_CMD</para>
        ///<para>15：其它原因立即停止，IMD_STOP_AT_OTHER</para>
        ///<para>16：其它原因减速停止，DEC_STOP_AT_OTHER</para>
        ///<para>17：未知原因立即停止，IMD_STOP_AT_UNKOWN</para>
        ///<para>18：未知原因减速停止，DEC_STOP_AT_UNKOWN</para>
        ///<para>19：保留，DEC_STOP_AT_DEC</para>
        ///</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardAxisStopReason(int CardNo, int axis, ref int StopReason)
        {
            //return ecat_motion.dmc_get_stop_reason((ushort)CardNo, (ushort)axis, ref StopReason);
            return 0;
        }
        /// <summary>
        /// 清除指定轴的停止原因
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="axis">指定轴号</param>
        /// <returns>错误代码</returns>
        public int DmcClearCardAxisStopReason(int CardNo, int axis)
        {
            return 0;
            //return ecat_motion.dmc_clear_stop_reason((ushort)CardNo, (ushort)axis);
        }
        #endregion

        #region 插补速度设置函数

        /// <summary>
        /// 设置插补运动速度曲线
        /// <para>注 意：</para>
        /// <para>1）DMC-E3032 卡支持 6 个坐标系（参数 Crd）。六个坐标系的速度可独立设置，执行插补时六个坐标系可独立进行插补运动（即可同时进行六组插补运动）</para>
        /// </summary>
        /// <param name="CardNo">卡号 </param>
        /// <param name="Crd">坐标系号，取值范围：0~3</param>
        /// <param name="Min_Vel">最小速度，单位：unit/s</param>
        /// <param name="Max_Vel">最大速度，单位：unit/s</param>
        /// <param name="Tacc">加速时间，单位：s </param>
        /// <param name="Tdec">减速时间，单位：s </param>
        /// <param name="Stop_Vel">停止速度，单位：unit/s</param>
        /// <returns>错误代码</returns>
        public int DmcSetCardVectorProfileUnit(int CardNo, int Crd, double Min_Vel, double Max_Vel, double Tacc, double Tdec, double Stop_Vel)
        {
            return 0;
            return 0;
            return 0;

        }

        /// <summary>
        /// 读取插补运动速度曲线
        /// </summary>
        /// <param name="CardNo">卡号 </param>
        /// <param name="Crd">坐标系号，取值范围：0~3</param>
        /// <param name="Min_Vel">返回最小速度设置</param>
        /// <param name="Max_Vel">返回最大速度设置</param>
        /// <param name="Tacc">返回加速时间设置</param>
        /// <param name="Tdec">返回减速时间设置</param>
        /// <param name="Stop_Vel">返回停止速度</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardVectorProfileUnit(int CardNo, int Crd, ref double Min_Vel, ref double Max_Vel, ref double Tacc, ref double Tdec, ref double Stop_Vel)
        {
            return 0;
            //return ecat_motion.dmc_get_vector_profile_unit((ushort)CardNo, (ushort)Crd, ref Min_Vel, ref Max_Vel, ref Tacc, ref Tdec, ref Stop_Vel);
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
            return 0;
            //return ecat_motion.dmc_set_vector_s_profile((ushort)CardNo, (ushort)Crd, (ushort)s_mode, s_para);
        }

        /// <summary>
        /// 读取设置的插补运动速度曲线平滑时间
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="Crd">坐标系号，取值范围：0~</param>
        /// <param name="s_mode">保留参数，固定值为 0</param>
        /// <param name="s_para">返回平滑时间设置</param>
        /// <returns>错误代码</returns>
        public int DmcGetCardVectorSProfile(int CardNo, int Crd, int s_mode, ref double s_para)
        {
            return 0;
            //return ecat_motion.dmc_get_vector_s_profile((ushort)CardNo, (ushort)Crd, (ushort)s_mode, ref s_para);
        }

        #endregion

        #region 插补运动函数
        /// <summary>
        /// 启动直线插补
        /// </summary>
        /// <param name="CardNo"></param>
        /// <param name="Crd">坐标系号，取值范围：0~5 </param>
        /// <param name="axisNum">轴数，取值范围：2~16 </param>
        /// <param name="AxisNums">轴号数组 </param>
        /// <param name="Target_Pos">目标位置列表，单位：unit </param>
        /// <param name="posi_mode">运动模式，0：相对坐标模式，1：绝对坐标模式</param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisLineUnit(int CardNo, int Crd, int axisNum, ushort[] AxisNums, double[] Target_Pos, int posi_mode, double MaxVel, double acc, short Fifo = 0, double velEnd = 0)    //
        {
            if (axisNum < 5)
            {
                short[] AxisArray = Array.ConvertAll(AxisNums, s => (short)s);
                int[] PosArray = Array.ConvertAll(Target_Pos, s => (int)s);
                double Acc = MaxVel / acc;
                var res = ecat_motion.M_Line((short)Crd, (short)axisNum, ref AxisArray, ref PosArray, MaxVel, Acc, velEnd, Fifo, (short)CardNo);
                return res;
            }
            else
            {
                short[] AxisArray = Array.ConvertAll(AxisNums, s => (short)s);
                int[] PosArray = Array.ConvertAll(Target_Pos, s => (int)s);
                double Acc = MaxVel / acc;
                var res = ecat_motion.M_Line_All(/*(short)Crd,*/ (short)axisNum, ref AxisArray, ref PosArray, Acc, MaxVel, (short)CardNo);
                return res;
            }
        }

        /// <summary>
        /// 圆弧插补
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
        /// <param name="CardNo">卡号 </param>
        /// <param name="Crd">坐标系号，取值范围：0~5 </param>
        /// <param name="axisNum">轴数，取值范围： 2~16 </param>
        /// <param name="AxisNums">轴号数组</param>
        /// <param name="Target_Pos">目标位置数组，单位：unit</param>
        /// <param name="Cen_Pos">圆心位置数组，单位：unit </param>
        /// <param name="Arc_Dir">圆弧方向，0：顺时针，1：逆时针</param>
        /// <param name="Circle">圈数：自然数：表示此时执行的为螺旋线插补,螺旋线的圈数</param>
        /// <param name="posi_mode">运动模式，0：相对坐标模式，1：绝对坐标模式</param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisArcMoveCenterUnit(int CardNo, int Crd, int axisNum, UInt16[] AxisNums, double[] Target_Pos, double[] Cen_Pos, int Arc_Dir, int Circle, int posi_mode, double MaxVel, double acc, short Fifo = 0)
        {
            short[] AxisArray = Array.ConvertAll(AxisNums, s => (short)s);
            int[] PosArray = Array.ConvertAll(Target_Pos, s => (int)s);
            double Acc = MaxVel / acc;
            return ecat_motion.M_Arc2R((short)Crd, /*(short)axisNum,*/ ref AxisArray, ref PosArray, 1, (short)Arc_Dir, MaxVel, Acc, Fifo, (short)CardNo);
        }
        /// <summary>
        /// 圆弧插补
        /// <para> 基于半径圆弧扩展的圆柱螺旋线插补运动（可作两轴圆弧插补） </para>
        /// <para>注  意：</para> 
        /// <para> 1）当轴数为 2 时，轴列表前两轴进行平面圆弧插补</para>
        /// <para> 2）当轴数为 3 时，轴列表前两轴平面为基面，进行平面圆弧插补；同时，轴列表第三轴运动指定高度；该轴终点位置与该轴起点位置的差值为圆柱螺旋线段相对于基面的高度 </para> 
        /// </summary>
        /// <param name="CardNo">卡号 </param>
        /// <param name="Crd">坐标系号，取值范围：0~5 </param>
        /// <param name="axisNum">轴数，取值范围： 2~16 </param>
        /// <param name="AxisNums">轴号数组</param>
        /// <param name="Target_Pos">目标位置数组，单位：unit</param>
        /// <param name="Arc_Radius">圆弧半径值，单位：unit  </param>
        /// <param name="Arc_Dir">圆弧方向，0：顺时针，1：逆时针</param>
        /// <param name="Circle">圈数：自然数：表示此时执行的为螺旋线插补,螺旋线的圈数。如:0 即表示 0 圈螺旋线插补，1 即表示 1 圈螺旋线插补 </param>
        /// <param name="posi_mode">运动模式，0：相对坐标模式，1：绝对坐标模式</param>
        /// <returns>错误代码</returns>
        public int DmcCardAxisArcMoveRadiusUnit(int CardNo, int Crd, int axisNum, UInt16[] AxisNums, double[] Target_Pos, double Arc_Radius, int Arc_Dir, int Circle, int posi_mode)
        {
            //return ecat_motion.M_Arc2C((ushort)CardNo, (ushort)Crd, (ushort)axisNum, AxisNums, Target_Pos, Arc_Radius, (ushort)Arc_Dir, Circle, (ushort)posi_mode);
            return 0;
        }

        /// <summary>
        /// 圆弧插补
        ///<para>功  能：基于三点圆弧扩展的圆柱螺旋线插补运动（可作两轴及三轴圆弧插补） </para>
        ///<para>注  意：</para><para>1）当轴数为 2 时，轴列表前两轴进行平面圆弧插补</para>
        ///<para>2）当轴数为 3、运动轨迹为圆柱螺旋插补时，轴列表前两轴平面为基面，进行平面圆弧插补；同时，轴列表第三轴运动指定高度；该轴终点位置与该轴起点位置的差值为圆柱螺旋线段相对于基面的高度</para>
        ///<para>3）当轴数大于 3、运动轨迹为螺旋插补时，列表前三轴进行螺旋插补的同时，后续轴做线性跟随运动，运动时间与前三轴的运动时间相等</para>
        /// </summary>
        /// <param name="CardNo">卡号</param>
        /// <param name="Crd">坐标系号，取值范围：0~5</param>
        /// <param name="axisNum">轴数，取值范围：2~16 </param>
        /// <param name="AxisNums">轴号数组</param>
        /// <param name="Target_Pos">目标位置数组，单位：unit</param>
        /// <param name="Mid_Pos">中间位置数组，单位：unit</param>
        /// <param name="Circle">圈数：<para>负数：表示此时执行的为空间圆弧插补：该值的绝对值减 1 表示空间圆弧的圈数。如，-1 即表示 0圈空间圆弧，-2 即表示 1 圈空间圆弧… </para><para>自然数：表示此时执行的为圆柱螺旋线插补:该值表示螺旋线的圈数。如，0 即表示 0 圈螺旋线插补， 1 即表示 1 圈螺旋线插补… </para></param>
        /// <param name="posi_mode">运动模式，0：相对坐标模式，1：绝对坐标模式 </param>
        /// <returns><错误代码/returns>
        public int DmcCardArcMove3PointsUnit(int CardNo, int Crd, int axisNum, UInt16[] AxisNums, double[] Target_Pos, double[] Mid_Pos, int Circle, int posi_mode)
        {
            //return ecat_motion.M_Arc3D((ushort)CardNo, (ushort)Crd, (ushort)axisNum, AxisNums, Target_Pos, Mid_Pos, Circle, (ushort)posi_mode);
            return 0;
        }

        public int CardBufIO(int CardNo, int Crd, int Bit, int status, int fifo)
        {
            return ecat_motion.M_BufIO((short)Crd, (short)(Bit + 1), (short)status, (short)fifo, (short)CardNo);

        }
        public int CardBufMultiDO(int CardNo, int Crd, int StationID, int status, int fifo)
        {
            return ecat_motion.M_BufMultiDO((short)Crd, (short)(StationID), 1, (short)status, 0x7FFF, (short)fifo, (short)CardNo);

        }
        public int CardBufWaitDI(int CardNo, int Crd, int Bit, int status, int fifo, int waittime)
        {
            return ecat_motion.M_BufWaitDI((short)Crd, (short)(Bit + 1), (short)status, (ushort)waittime, (short)fifo, (short)CardNo);
        }
        public int CardBufWaitDO(int CardNo, int Crd, int Bit, int status, int fifo, int waittime)
        {
            return ecat_motion.M_BufWaitDO((short)Crd, (short)(Bit + 1), (short)status, (ushort)waittime, (short)fifo, (short)CardNo);
        }

        public int CardBufDelay(int CardNo, int Crd, int fifo, int waittime)
        {
            return ecat_motion.M_BufDelay((short)Crd, (ushort)waittime, (short)fifo, (short)CardNo);

        }
        public int CardGetLastCrdPos(int CardNo, int Crd, int fifo, int waittime, out int[] position)
        {
            var res = ecat_motion.M_GetLastCrdPos((short)Crd, out position, (short)fifo, (short)CardNo);
            return res;
        }

        #endregion

        #region 总线操作函数

        /// <summary>
        /// 读取从站对象字典参数值
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="PortNum">EtherCAT 端口号，固定为 2</param>
        /// <param name="nodenum">从站 EtherCAT 地址，第 i 个 EtherCAT 从站为 1000+i</param>
        /// <param name="index">对象字典索引</param>
        /// <param name="subindex">对象字典子索引</param>
        /// <param name="valuelength">对象字典索引长度(单位：bit)</param>
        /// <param name="value">对象字典索引参数值</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardNodeOD(int CardNo, int PortNum, int nodenum, int index, int subindex, int valuelength, ref int value)
        {
            //return ecat_motion.nmc_get_node_od((ushort)CardNo, (ushort)PortNum, (ushort)nodenum, (ushort)index, (ushort)subindex, (ushort)valuelength, ref value);
            return 0;
        }

        /// <summary>
        ///设置 EtherCAT 总线驱动器使能
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴的轴号，255 表示使能所有 EtherCAT 轴</param>
        /// <returns>错误代码</returns>
        public int NmcSetCardAxisEnable(int CardNo, int axis)
        {
            return ecat_motion.M_Servo_On((short)(axis + 1), (short)CardNo);
        }

        /// <summary>
        /// 设置 EtherCAT 总线驱动器失能
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴的轴号，255 表示使能所有 EtherCAT 轴</param>
        /// <returns>错误代码</returns>
        public int NmcSetCardAxisDisable(int CardNo, int axis)
        {
            return ecat_motion.M_Servo_Off((short)(axis + 1), (short)CardNo);
        }

        /// <summary>
        /// 读取 EtherCAT 总线轴和虚拟轴轴数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="TotalAxis">EtherCAT 总线轴和虚拟轴轴数</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardTotalAxes(int CardNo, ref uint TotalAxis)
        {
            short[] pAlias = new short[16]; short pSlaveNum = 0;
            var res = ecat_motion.M_ReadServoAlias(ref pAlias, ref pSlaveNum, (short)CardNo);
            //TotalAxis = Array.ConvertAll(pAlias, s => Convert.ToUInt32(s));
            TotalAxis = (uint)pSlaveNum;
            return res;

        }
        /// <summary>
        /// 读取 EtherCAT 总线 AD/DA 输入输出口数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="TotalIn">EtherCAT 总线 AD 输入数</param>
        /// <param name="TotalOut">EtherCAT 总线 DA 输出数</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardTotalAdcnum(int CardNo, ref ushort TotalIn, ref ushort TotalOut)
        {
            return 0;
            //return ecat_motion.nmc_get_total_adcnum((ushort)CardNo, ref TotalIn, ref TotalOut);
        }

        /// <summary>
        /// 读取 EtherCAT 总线 IO 输入输出口数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="TotalIn">EtherCAT 总线 IO 输入数，包括板卡自带以及 IO 模块上的输入</param>
        /// <param name="TotalOut">EtherCAT 总线 IO 输出数，包括板卡自带以及 IO 模块上的输出</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardTotalIONum(int CardNo, ref UInt16 TotalIn, ref UInt16 TotalOut)
        {
            return 0;
            //return ecat_motion.nmc_get_total_ionum((ushort)CardNo, ref TotalIn, ref TotalOut);
        }

        /// <summary>
        /// 设置控制卡工作模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="controller_mode">控制器工作模式，0 表示仿真模式，1 表示 EtherCAT总线模式</param>
        /// <returns>错误代码</returns>
        public int NmcSetCardControllerWorkMode(int CardNo, int controller_mode)
        {
            return 0;
            //return ecat_motion.nmc_set_controller_workmode((ushort)CardNo, (ushort)controller_mode);
        }

        /// <summary>
        /// 读取控制卡工作模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="controller_mode">控制卡的工作模式，0 表示仿真模式，1 表示 EtherCAT 总线模式</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardControllerWorkMode(int CardNo, ref ushort controller_mode)
        {
            return 0;
            //return ecat_motion.nmc_get_controller_workmode((ushort)CardNo, ref controller_mode);
        }

        /// <summary>
        /// 读取EtherCAT总线循环周期
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="FieldbusType">EtherCAT 端口号，固定为 2</param>
        /// <param name="CyclFieldbusTypeeTime">EtherCAT 总线循环周期，单位：us，支持 250/500/1000/2000</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardCydleTime(int CardNo, int FieldbusType, ref int CyclFieldbusTypeeTime)
        {
            return 0;
            //return ecat_motion.nmc_get_cycletime((ushort)CardNo, (ushort)FieldbusType, ref CyclFieldbusTypeeTime);
        }

        /// <summary>
        /// 读取轴类型
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">轴号</param>
        /// <param name="Axis_Type">轴的类型，0：虚拟轴，1：EtherCAT 轴，2：CANopen轴，3：脉冲轴，4：未知类型轴</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardAxisType(int CardNo, int axis, ref ushort Axis_Type)
        {
            return 0;
            //return ecat_motion.nmc_get_axis_type((ushort)CardNo, (ushort)axis, ref Axis_Type);
        }
        /// <summary>
        /// 停止EtherCAT总线运行
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="ETCState">0：停止 EtherCAT 总线成功，1：停止 EtherCAT 总线失败</param>
        /// <returns>错误代码</returns>
        public int NmcStopCardEtc(int CardNo, ref ushort ETCState)
        {
            return 0;
            //return ecat_motion.nmc_stop_etc((ushort)CardNo, ref ETCState);
        }

        /// <summary>
        /// 读取EtherCAT总线平均周期时间、最大周期时间、执行周期数
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Fieldbustype">EtherCAT 端口号，固定为 2</param>
        /// <param name="Average_time">EtherCAT 总线周期时间，单位：us</param>
        /// <param name="Max_time">EtherCAT 总线最大周期时间，单位：us</param>
        /// <param name="Cycles">EtherCAT 总线执行周期数</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardConsumeTimeFieldbus(int CardNo, int Fieldbustype, ref uint Average_time, ref uint Max_time, ref UInt64 Cycles)
        {
            return 0;
            //return ecat_motion.nmc_get_consume_time_fieldbus((ushort)CardNo, (ushort)Fieldbustype, ref Average_time, ref Max_time, ref Cycles);
        }

        /// <summary>
        /// 清除EtherCAT总线平均周期时间、最大周期时间、执行周期数等记录
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="Fieldbustype">EtherCAT 端口号，固定为 2</param>
        /// <returns>错误代码</returns>
        public int NmcClearCardConsumeTimeFieldbus(int CardNo, int Fieldbustype)
        {
            return 0;
            //return ecat_motion.nmc_clear_consume_time_fieldbus((ushort)CardNo, (ushort)Fieldbustype);
        }

        /// <summary>
        /// 读取EtherCAT总线轴状态机
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴轴号</param>
        /// <param name="Axis_StateMachine">EtherCAT 总线轴状态机
        /// <para> 0：NOT_READY_SWITCH_ON  轴处于未启动状态  </para>
        /// <para>1：SWITCH_ON_DISABLE  轴处于启动禁止状态 </para>
        /// <para>2：READY_TO_DISABLE  轴处于准备启动状态(轴失能) </para>
        /// <para>3：SWOTCH_ON  轴处于启动状态     </para>
        /// <para>4：OP_ENABLE  轴处于操作使能状态（轴已经使能） </para>
        /// <para>5：QUICK_STOP  轴处于停止状态     </para>
        /// <para>6：FAULT_ACTIVE  轴处于错误触发状态 </para>
        /// <para>7：FAULT  轴处于错误状态     </para>
        /// </param>
        /// <returns>错误代码</returns>
        public int NmcGetCardAxisStateMachine(int CardNo, int axis, ref ushort Axis_StateMachine)
        {
            int[] code = new int[1];
            var res = ecat_motion.M_GetSts((short)(axis + 1), out code, 1, (short)CardNo);
            return code[0];
        }

        /// <summary>
        /// 获取EtherCAT总线轴配置控制模式
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴轴号</param>
        /// <param name="contrlmode">EtherCAT 总线轴配置控制模式（6 回零模式、8 CSP 模式）</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardAxisSettingControlMode(int CardNo, int axis, ref int contrlmode)
        {
            short mode = 0;
            var res = ecat_motion.M_EcatGetOperationMode((short)(axis + 1), ref mode, (short)CardNo);
            contrlmode = (int)mode;
            return res;
        }

        /// <summary>
        /// 设置 EtherCAT 总线轴回零参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">EtherCAT 总线轴轴号</param>
        /// <param name="home_mode">EtherCAT 总线轴回零模式</param>
        /// <param name="Low_Vel">EtherCAT 总线轴回零低速</param>
        /// <param name="High_Vel">EtherCAT 总线轴回零高速</param>
        /// <param name="Tacc">EtherCAT 总线轴回零加速时间</param>
        /// <param name="Tdec">EtherCAT 总线轴回零减速时间</param>
        /// <param name="offsetpos">EtherCAT 总线轴回零偏移 </param>
        /// <returns>错误代码</returns>
        public int NmcSetCardAxisHomeProfile(int CardNo, int axis, int home_mode, double Low_Vel, double High_Vel, double Tacc, double Tdec, double offsetpos)
        {
            ecat_motion.M_SetHomingMode((short)(axis + 1), 6, (short)CardNo);
            uint tacc = (uint)((High_Vel - Low_Vel) / Tacc);
            return ecat_motion.M_SetHomingPrm((short)(axis + 1), (short)home_mode, (int)offsetpos, (uint)Low_Vel, (uint)High_Vel, (uint)tacc, 0, (short)CardNo);
            //return ecat_motion.M_SetHomingPrm((ushort)CardNo, (ushort)axis, (ushort)home_mode, Low_Vel, High_Vel, Tacc, Tdec, offsetpos);
        }

        /// <summary>
        /// 获取 EtherCAT 总线轴回零参数
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="axis">EtherCAT 总线轴轴号</param>
        /// <param name="home_mode">返回EtherCAT 总线轴回零模式</param>
        /// <param name="Low_Vel">返回EtherCAT 总线轴回零低速</param>
        /// <param name="High_Vel">返回EtherCAT 总线轴回零高速</param>
        /// <param name="Tacc">返回EtherCAT 总线轴回零加速时间</param>
        /// <param name="Tdec">返回EtherCAT 总线轴回零减速时间</param>
        /// <param name="offsetpos">返回EtherCAT 总线轴回零偏移 </param>
        /// <returns>错误代码</returns>
        public int NmcGetCardAxisHomeProfile(int CardNo, int axis, ref ushort home_mode, ref double Low_Vel, ref double High_Vel, ref double Tacc, ref double Tdec, ref double offsetpos)
        {
            short method = 0; int offset = 0; uint speed1 = 0; uint speed2 = 0; uint acc = 0; ushort probeFunction = 0;
            var res = ecat_motion.M_GetHomingPrm((short)(axis + 1), ref method, ref offset, ref speed1, ref speed2, ref acc, ref probeFunction, (short)CardNo);
            uint tacc = (uint)((High_Vel - Low_Vel) / Tacc);
            home_mode = (ushort)method; offsetpos = (double)offset; High_Vel = (double)speed1; Low_Vel = (double)speed2;
            Tacc = (ushort)acc;
            return res;
        }

        /// <summary>
        /// 启动 EtherCAT 总线轴回零
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴轴号</param>
        /// <returns>错误代码</returns>
        public int NmcCardAxisHomeMove(int CardNo, int axis)
        {
            //return ecat_motion.nmc_home_move((ushort)CardNo, (ushort)axis);
            return 0;
        }

        /// <summary>
        /// 读取 EtherCAT 总线卡状态
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="channel">EtherCAT 总线端口号，固定为 2</param>
        /// <param name="errcode">EtherCAT 总线状态，0 表示正常</param>
        /// <returns></returns>
        public int NmcGetCardErrcode(int CardNo, int channel, ref ushort errcode)
        {
            short errinfo = 0;
            var res = ecat_motion.M_GetEmg(ref errinfo, (short)CardNo);
            //errcode = errinfo;
            //return res;
            return res;
        }

        byte Extract8Bits(uint value, int startBit)
        {
            if (startBit < 0 || startBit > 24)
                throw new ArgumentOutOfRangeException(nameof(startBit), "起始位必须是在0~24之间！");
            uint mask = 0xFFU << startBit;
            return (byte)((value & mask) >> startBit);
        }

        /// <summary>
        /// 获读取 EtherCAT 轴节点地址信息
        /// </summary>
        /// <param name="CardNo">控制卡卡号</param>
        /// <param name="axis">EtherCAT 总线轴号</param>
        /// <param name="SlaveAddr">EtherCAT 总线轴地址</param>
        /// <param name="Sub_SlaveAddr">EtherCAT 总线轴子地址</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardAxisNodeAddress(int CardNo, int axis, ref ushort SlaveAddr, ref ushort Sub_SlaveAddr)
        {
            //return ecat_motion.nmc_get_axis_node_address((ushort)CardNo, (ushort)axis, ref SlaveAddr, ref Sub_SlaveAddr);
            return 0;

        }

        /// <summary>
        /// 获取 EtherCAT 从站总数
        /// </summary>
        /// <param name="CardNo">控制卡卡号 </param>
        /// <param name="PortNum">EtherCAT 端口号，固定为 2</param>
        /// <param name="TotalSlaves">EtherCAT 从站总数</param>
        /// <returns>错误代码</returns>
        public int NmcGetCardTotalSlaves(int CardNo, int PortNum, ref ushort TotalSlaves)
        {
            short[] pAlias = new short[32]; short pSlaveNum = 0;
            var res = ecat_motion.M_ReadSlaveAlias(ref pAlias, ref pSlaveNum, (short)CardNo);
            TotalSlaves = (ushort)pSlaveNum;
            return res;
        }

        #endregion

    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
