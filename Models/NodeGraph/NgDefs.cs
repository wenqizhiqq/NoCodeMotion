// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦
// ◆温⁣启⁠志⁣◆​编⁣写‍◇​微‌信​﹕​1‏8⁠7‍◆‏1⁣9‏3⁣6‏◇⁠1‏3‎9⁣9‏　‌※‍保⁣留⁣所‏有‎权‍利⁠请‏勿​删‏除‏◇
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨ۤ
using System.Linq;
using NoCodeMotion.Services.Vision;

namespace NoCodeMotion.Models.NodeGraph;

/// <summary>节点领域：视觉 / 视觉算子 / 运控 / 通讯 / 逻辑。工具箱按此分组。</summary>
public enum NgDomain
{
    Vision,
    VisionOp,
    Motion,
    Comm,
    Logic
}

/// <summary>单个属性定义（节点类型元数据，用于右侧属性面板）。</summary>
public sealed class NgPropDef
{
    public string Name { get; init; } = "";
    public string Default { get; init; } = "";
    /// <summary>若为枚举，提供 "A|B|C" 形式的可选项，属性面板渲染为下拉框。</summary>
    public string? Options { get; init; }
}

/// <summary>节点类型元数据（数据驱动：NodeGraphNodeView 据此渲染端口与属性面板）。</summary>
public sealed class NgNodeDef
{
    public NgKind Kind { get; init; }
    public string Title { get; init; } = "";
    public NgDomain Domain { get; init; }
    /// <summary>工具箱芯片对应的算子键（只有「视觉算子」分组里的条目非空）。</summary>
    public string OpKey { get; init; } = "";
    /// <summary>算子分类（工具箱按它再分小组）。</summary>
    public string Category { get; init; } = "";
    /// <summary>标准名（手册 / 行业叫法），白话名不好懂时在提示里补上。</summary>
    public string StdName { get; init; } = "";
    /// <summary>详细说明（工具箱悬停显示）。</summary>
    public string Summary { get; init; } = "";

    /// <summary>
    /// 工具箱悬停提示：第一行「白话名（标准名）」，第二行详细说明，第三行参数清单。
    /// 只写标题的话，悬停等于没提示 —— 每个节点都必须有说明（守卫 G36.4 会拦）。
    /// </summary>
    public string Tip
    {
        get
        {
            var sb = new System.Text.StringBuilder(Title);
            if (StdName.Length > 0 && StdName != Title) sb.Append("（").Append(StdName).Append('）');
            if (Summary.Length > 0) sb.Append('\n').Append(Summary);
            if (Props.Count > 0)
                sb.Append('\n').Append("参数：").Append(string.Join(" / ", System.Linq.Enumerable.Select(Props, p => p.Name)));
            return sb.ToString();
        }
    }
    public string Color { get; init; } = "#4A89DC";
    public bool HasInput { get; init; } = true;
    public IReadOnlyList<string> Outputs { get; init; } = new[] { "Out" };
    public IReadOnlyList<NgPropDef> Props { get; init; } = System.Array.Empty<NgPropDef>();
}

/// <summary>节点图节点类型。视觉(图像采集/模板匹配/缺陷检测/测量/对位/标定)、
/// 运控(开始/轴运动/回零/延时/等待输入/条件分支/循环/结束)、
/// 通讯(Modbus发送/接收/TCP发送/下位机写)。</summary>
public enum NgKind
{
    // —— 视觉 ——
    CamCapture,      // 图像采集
    TemplateMatch,   // 模板匹配
    DefectDetect,    // 缺陷检测
    Measure,         // 测量
    Align,           // 对位
    Calib,           // 标定
    VisionOp,        // 视觉算子（参数表由 VisionOperators 目录生成）

    // —— 运控 ——
    Start,           // 开始
    MoveAxis,        // 轴运动
    AxisJog,         // 点动（一直走，直到停）
    AxisStop,        // 停下这根轴
    AxisServo,       // 伺服使能 / 断开
    LineInterp,      // 直线插补
    ArcInterp,       // 三点圆弧插补
    Home,            // 回零
    Delay,           // 延时
    WaitInput,       // 等待输入
    WaitAxis,        // 等待轴到位
    Cylinder,        // 气缸动作
    PointGo,         // 点位移动
    IoWrite,         // 写输出
    Decision,        // 条件分支
    Loop,            // 循环
    End,             // 结束

    // —— 通讯 ——
    ModbusSend,      // Modbus 发送
    ModbusRecv,      // Modbus 接收
    TcpSend,         // TCP 发送
    McuWrite,        // 下位机写

    // —— 逻辑 / 变量 ——
    VarSet,          // 设置变量
    Compute,         // 运算

    // —— 并行控制 ——
    ParallelFork,    // 并行分支：同时扇出多条分支
    ParallelJoin,    // 并行汇聚：等待全部分支到达后再继续
}

/// <summary>节点类型静态字典（数据驱动渲染）。</summary>
public static class NgNodeDefinitions
{
    public static IReadOnlyDictionary<NgKind, NgNodeDef> All { get; } = new Dictionary<NgKind, NgNodeDef>
    {
        // ============ 视觉 ============
        [NgKind.CamCapture] = new()
        {
            Kind = NgKind.CamCapture, Title = "拍一张照", Domain = NgDomain.Vision, Color = "#10B981",
            StdName = "图像采集", Summary = "从相机或图片文件取一帧，后面所有视觉节点都复用这一张图；可设曝光与分辨率。没接相机时会用测试图跑通流程。",
            Props = new[]
            {
                new NgPropDef { Name = "图像源", Default = "相机", Options = "相机|文件|文件夹" },
                new NgPropDef { Name = "相机", Default = "相机1" },
                new NgPropDef { Name = "图像路径", Default = "" },
                new NgPropDef { Name = "曝光ms", Default = "10" },
                new NgPropDef { Name = "宽度", Default = "1280" },
                new NgPropDef { Name = "高度", Default = "960" },
            }
        },
        [NgKind.TemplateMatch] = new()
        {
            Kind = NgKind.TemplateMatch, Title = "图上找图案", Domain = NgDomain.Vision, Color = "#34C759",
            StdName = "模板匹配", Summary = "用框选的模板在整幅图里找最像的位置，给出中心坐标、角度和相似度；结果按「变量前缀」写成变量供后续判断。",
            Props = new[]
            {
                new NgPropDef { Name = "模板路径", Default = "" },
                new NgPropDef { Name = "模板框", Default = "" },
                new NgPropDef { Name = "匹配模式", Default = "灰度匹配", Options = "灰度匹配|轮廓匹配" },
                new NgPropDef { Name = "分数阈值", Default = "0.8" },
                new NgPropDef { Name = "角度范围", Default = "360" },
                new NgPropDef { Name = "变量前缀", Default = "匹配" },
            }
        },
        [NgKind.DefectDetect] = new()
        {
            Kind = NgKind.DefectDetect, Title = "找缺陷", Domain = NgDomain.Vision, Color = "#22C55E",
            StdName = "缺陷检测", Summary = "按灰度阈值 + 面积（或轮廓）找暗斑 / 亮斑，统计个数和总面积，再和「允许缺陷数」比较得出合格与否。",
            Props = new[]
            {
                new NgPropDef { Name = "检测模式", Default = "阈值面积", Options = "阈值面积|边缘轮廓" },
                new NgPropDef { Name = "缺陷类型", Default = "暗斑", Options = "暗斑|亮斑" },
                new NgPropDef { Name = "阈值", Default = "128" },
                new NgPropDef { Name = "最小面积", Default = "50" },
                new NgPropDef { Name = "最大面积", Default = "100000" },
                new NgPropDef { Name = "允许缺陷数", Default = "0" },
                new NgPropDef { Name = "变量前缀", Default = "缺陷" },
            }
        },
        [NgKind.Measure] = new()
        {
            Kind = NgKind.Measure, Title = "量尺寸", Domain = NgDomain.Vision, Color = "#16A34A",
            StdName = "测量", Summary = "用上游找到的特征（匹配中心 / 斑点重心）算距离或直径，按标定系数换算成毫米，可设上下限判合格。",
            Props = new[]
            {
                new NgPropDef { Name = "测量项", Default = "距离", Options = "距离|直径" },
                new NgPropDef { Name = "标定系数", Default = "1" },
                new NgPropDef { Name = "单位", Default = "mm" },
                new NgPropDef { Name = "下限", Default = "0" },
                new NgPropDef { Name = "上限", Default = "0" },
                new NgPropDef { Name = "变量前缀", Default = "测量" },
            }
        },
        [NgKind.Align] = new()
        {
            Kind = NgKind.Align, Title = "对准位置", Domain = NgDomain.Vision, Color = "#059669",
            StdName = "对位", Summary = "把匹配到的中心与基准点比较，算出 X / Y 偏差（像素或毫米），供下游补偿或判断是否超差。",
            Props = new[]
            {
                new NgPropDef { Name = "基准X", Default = "0" },
                new NgPropDef { Name = "基准Y", Default = "0" },
                new NgPropDef { Name = "像素当量", Default = "0" },
                new NgPropDef { Name = "容差", Default = "0.5" },
                new NgPropDef { Name = "变量前缀", Default = "偏差" },
            }
        },
        [NgKind.Calib] = new()
        {
            Kind = NgKind.Calib, Title = "标定对应关系", Domain = NgDomain.Vision, Color = "#047857",
            StdName = "标定", Summary = "用棋盘格 / 圆点算出「多少像素 = 1 毫米」，后续测量和对位都用这个比例换算。",
            Props = new[]
            {
                new NgPropDef { Name = "标定板", Default = "棋盘格", Options = "棋盘格|圆点" },
                new NgPropDef { Name = "行数", Default = "9" },
                new NgPropDef { Name = "列数", Default = "9" },
                new NgPropDef { Name = "格子尺寸mm", Default = "10" },
            }
        },

        // ============ 运控 ============
        [NgKind.Start] = new()
        {
            Kind = NgKind.Start, Title = "开始", Domain = NgDomain.Motion, Color = "#37BC9B",
            StdName = "起始", Summary = "流程的入口：运行从这里开始往下走，本身不动作。", HasInput = false,
        },
        [NgKind.MoveAxis] = new()
        {
            Kind = NgKind.MoveAxis, Title = "电机走位", Domain = NgDomain.Motion, Color = "#4A89DC",
            StdName = "单轴运动", Summary = "把一根轴按速度走到目标位置（绝对 = 走到坐标，相对 = 走一段距离）；勾「等待到位」会等轴停稳再往下走。加速 / 减速时间在「轴」页配置。",
            Props = new[]
            {
                new NgPropDef { Name = "轴", Default = "X" },
                new NgPropDef { Name = "模式", Default = "绝对", Options = "绝对|相对" },
                new NgPropDef { Name = "目标位置", Default = "0" },
                new NgPropDef { Name = "速度", Default = "10" },
                new NgPropDef { Name = "等待到位", Default = "否", Options = "是|否" },
                new NgPropDef { Name = "到位延时ms", Default = "0" },
            }
        },
        [NgKind.AxisJog] = new()
        {
            Kind = NgKind.AxisJog, Title = "点动（一直走，直到停）", Domain = NgDomain.Motion, Color = "#4A89DC",
            StdName = "Jog 运动", Summary = "让轴朝指定方向一直走，直到执行「停下这根轴」或有人拍急停；适合手动找位置，确认周边安全再用。",
            Props = new[]
            {
                new NgPropDef { Name = "轴", Default = "X" },
                new NgPropDef { Name = "方向", Default = "正", Options = "正|负" },
                new NgPropDef { Name = "速度", Default = "10" },
            }
        },
        [NgKind.AxisStop] = new()
        {
            Kind = NgKind.AxisStop, Title = "停下这根轴", Domain = NgDomain.Motion, Color = "#ED5565",
            StdName = "轴停止", Summary = "让正在动的轴停下来：减速停止按轴的减速时间收尾，立即停止会马上断输出。",
            Props = new[]
            {
                new NgPropDef { Name = "轴", Default = "X" },
                new NgPropDef { Name = "停止方式", Default = "减速停止", Options = "减速停止|立即停止" },
            }
        },
        [NgKind.AxisServo] = new()
        {
            Kind = NgKind.AxisServo, Title = "伺服使能 / 断开", Domain = NgDomain.Motion, Color = "#967ADC",
            StdName = "伺服使能", Summary = "给轴上伺服（使能）或断使能。未使能的轴收到运动指令也不会动。",
            Props = new[]
            {
                new NgPropDef { Name = "轴", Default = "X" },
                new NgPropDef { Name = "动作", Default = "使能", Options = "使能|断开" },
            }
        },
        [NgKind.LineInterp] = new()
        {
            Kind = NgKind.LineInterp, Title = "直线插补（两轴一起走直线）", Domain = NgDomain.Motion, Color = "#4A89DC",
            StdName = "直线插补", Summary = "两根轴联动走一条直线，按合成速度一起到目标点。两轴必须在同一张卡上；速度 / 加减速是否生效取决于控制卡。",
            Props = new[]
            {
                new NgPropDef { Name = "轴1", Default = "X" },
                new NgPropDef { Name = "目标1", Default = "0" },
                new NgPropDef { Name = "轴2", Default = "Y" },
                new NgPropDef { Name = "目标2", Default = "0" },
                new NgPropDef { Name = "模式", Default = "绝对", Options = "绝对|相对" },
                new NgPropDef { Name = "速度", Default = "20" },
                new NgPropDef { Name = "加减速时间", Default = "0.2" },
            }
        },
        [NgKind.ArcInterp] = new()
        {
            Kind = NgKind.ArcInterp, Title = "圆弧插补（过中间点到终点）", Domain = NgDomain.Motion, Color = "#4A89DC",
            StdName = "三点圆弧插补", Summary = "两根轴联动走圆弧：经过「中间点」到「终点」。两轴必须在同一张卡上，坐标按整数单位下发。",
            Props = new[]
            {
                new NgPropDef { Name = "轴1", Default = "X" },
                new NgPropDef { Name = "轴2", Default = "Y" },
                new NgPropDef { Name = "中间点1", Default = "0" },
                new NgPropDef { Name = "中间点2", Default = "0" },
                new NgPropDef { Name = "终点1", Default = "0" },
                new NgPropDef { Name = "终点2", Default = "0" },
                new NgPropDef { Name = "模式", Default = "绝对", Options = "绝对|相对" },
                new NgPropDef { Name = "圈数", Default = "0" },
            }
        },
        [NgKind.Home] = new()
        {
            Kind = NgKind.Home, Title = "回原点", Domain = NgDomain.Motion, Color = "#5D9CEC",
            StdName = "回零运动", Summary = "按「轴」页配置的回零模式 / 方向 / 速度回原点，并等它到位；上电后建议先回零。",
            // 回零方向 / 回零模式 / 高低速都在「轴」页配（按轴区分），节点这里只管走一次回零
            Props = new[] { new NgPropDef { Name = "轴", Default = "X" } }
        },
        [NgKind.Delay] = new()
        {
            Kind = NgKind.Delay, Title = "按时间等待", Domain = NgDomain.Motion, Color = "#F6BB42",
            StdName = "延时", Summary = "等待指定毫秒再往下走；等待期间按「暂停」会挂起、按「停止」会立刻结束。",
            Props = new[] { new NgPropDef { Name = "时间ms", Default = "500" } }
        },
        [NgKind.WaitInput] = new()
        {
            Kind = NgKind.WaitInput, Title = "等输入信号", Domain = NgDomain.Motion, Color = "#8CC152",
            StdName = "条件等待", Summary = "等某个输入点变成指定电平再继续；超过「超时ms」（0 = 用全局超时）仍未满足会报错并停下。",
            Props = new[]
            {
                new NgPropDef { Name = "信号", Default = "IN0" },
                new NgPropDef { Name = "状态", Default = "高电平", Options = "高电平|低电平" },
                new NgPropDef { Name = "超时ms", Default = "0" },
            }
        },
        [NgKind.Decision] = new()
        {
            Kind = NgKind.Decision, Title = "按条件分岔", Domain = NgDomain.Motion, Color = "#E9573F",
            StdName = "条件分支", Summary = "按配置的条件（变量 / 轴位置 / IO 等）决定走哪个出口，满足哪条就走哪条。",
            // 多分支：条件1..条件8 + 否则（旧图的 True/False 端口在 NgRunner 里做了兼容回退）
            Outputs = new[] { "条件1", "条件2", "条件3", "条件4", "条件5", "条件6", "条件7", "条件8", "否则" },
            Props = BuildDecisionProps()
        },
        [NgKind.Loop] = new()
        {
            Kind = NgKind.Loop, Title = "重复几次", Domain = NgDomain.Motion, Color = "#967ADC",
            StdName = "循环", Summary = "循环体的起点：执行到这里会按设置的次数回头重复，循环结束的节点负责跳回。",
            Outputs = new[] { "Body", "Exit" },
            Props = new[] { new NgPropDef { Name = "次数", Default = "3" } }
        },
        [NgKind.End] = new()
        {
            Kind = NgKind.End, Title = "结束", Domain = NgDomain.Motion, Color = "#DA4453",
            StdName = "结束", Summary = "流程的终点：走到这里本次运行结束（循环运行时再开下一轮）。",
            Outputs = System.Array.Empty<string>(),
        },

        // —— 运控扩展（物理动作 / 信号）——
        [NgKind.WaitAxis] = new()
        {
            Kind = NgKind.WaitAxis, Title = "等电机停稳", Domain = NgDomain.Motion, Color = "#48CFAD",
            StdName = "等待到位", Summary = "阻塞等待该轴停止并到位；超时会按轴的配置报错。",
            Props = new[] { new NgPropDef { Name = "轴", Default = "X" } }
        },
        [NgKind.Cylinder] = new()
        {
            Kind = NgKind.Cylinder, Title = "气缸动作", Domain = NgDomain.Motion, Color = "#EC87C0",
            StdName = "气缸控制", Summary = "让气缸伸出 / 缩回，并等磁性开关到位再往下走。",
            Props = new[]
            {
                new NgPropDef { Name = "气缸", Default = "夹爪" },
                new NgPropDef { Name = "动作", Default = "伸出", Options = "伸出|缩回" },
            }
        },
        [NgKind.PointGo] = new()
        {
            Kind = NgKind.PointGo, Title = "走到点位", Domain = NgDomain.Motion, Color = "#4FC1E9",
            StdName = "点表运动", Summary = "按点位表里配好的点位一次走多轴，流程里只需选表名 + 点位名。",
            Props = new[]
            {
                new NgPropDef { Name = "点位表", Default = "取放工位" },
                new NgPropDef { Name = "点位", Default = "取料点" },
            }
        },
        [NgKind.IoWrite] = new()
        {
            Kind = NgKind.IoWrite, Title = "打开/关闭输出", Domain = NgDomain.Motion, Color = "#A0D468",
            StdName = "IO 控制（写）", Summary = "把某个输出点设成 1（开）或 0（关），可接气缸、光源、蜂鸣器。",
            Props = new[]
            {
                new NgPropDef { Name = "输出", Default = "光源" },
                new NgPropDef { Name = "值", Default = "1", Options = "1|0" },
            }
        },

        // ============ 通讯 ============
        [NgKind.ModbusSend] = new()
        {
            Kind = NgKind.ModbusSend, Title = "发消息（外部通讯）", Domain = NgDomain.Comm, Color = "#AF52DE",
            StdName = "Modbus 写入", Summary = "向通讯页配好的 Modbus 通道写入指令（指令 / 地址 / 值）。",
            Props = new[]
            {
                new NgPropDef { Name = "通讯", Default = "通讯1" },
                new NgPropDef { Name = "指令", Default = "write" },
            }
        },
        [NgKind.ModbusRecv] = new()
        {
            Kind = NgKind.ModbusRecv, Title = "收消息（外部通讯）", Domain = NgDomain.Comm, Color = "#B06BE0",
            StdName = "Modbus 读取", Summary = "从 Modbus 通道读一次数据，按关键字取字段写进变量。",
            Props = new[]
            {
                new NgPropDef { Name = "通讯", Default = "通讯1" },
                new NgPropDef { Name = "关键字", Default = "OK" },
            }
        },
        [NgKind.TcpSend] = new()
        {
            Kind = NgKind.TcpSend, Title = "发消息（网口）", Domain = NgDomain.Comm, Color = "#9B51E0",
            StdName = "TCP 发送", Summary = "向指定 host:port 发一段文本，3 秒连不上会报错。",
            Props = new[]
            {
                new NgPropDef { Name = "端点", Default = "192.168.1.10:8000" },
                new NgPropDef { Name = "报文", Default = "hello" },
            }
        },
        [NgKind.McuWrite] = new()
        {
            Kind = NgKind.McuWrite, Title = "写下位机寄存器", Domain = NgDomain.Comm, Color = "#8E44AD",
            StdName = "下位机写", Summary = "把数据写到通讯页配好的设备（或 host:port），适合下位机寄存器 / 指令。",
            Props = new[]
            {
                new NgPropDef { Name = "设备", Default = "MCU1" },
                new NgPropDef { Name = "数据", Default = "0x01" },
            }
        },

        // ============ 逻辑 / 变量 ============
        [NgKind.VarSet] = new()
        {
            Kind = NgKind.VarSet, Title = "把值存进变量", Domain = NgDomain.Logic, Color = "#ED5565",
            StdName = "变量赋值", Summary = "把值（数字 / 文本 / 其它变量）存进指定变量，供后面节点和条件判断用。",
            Props = new[]
            {
                new NgPropDef { Name = "变量", Default = "计数" },
                new NgPropDef { Name = "值", Default = "0" },
            }
        },
        [NgKind.Compute] = new()
        {
            Kind = NgKind.Compute, Title = "算一下", Domain = NgDomain.Logic, Color = "#DA4453",
            StdName = "变量计算", Summary = "对变量做加 / 减 / 乘 / 除 / 求余 / 真假互换，右值可以是数字、变量名或表达式。",
            // 结构化运算：下拉选「运算」(加/减/乘/除/取模/取反) + 自由文本「值」(可为数字或其它变量名/表达式)。
            // 例：变量=计数，运算=加，值=1 → 计数 当前值 + 1。彻底避免「+1 被当成设置为 1」的歧义。
            Props = new[]
            {
                new NgPropDef { Name = "变量", Default = "结果" },
                new NgPropDef { Name = "运算", Default = "加上", Options = "加上|减去|乘以|除以|求余数|真假互换" },
                new NgPropDef { Name = "值", Default = "1" },
            }
        },

        // —— 并行控制 ——
        [NgKind.ParallelFork] = new()
        {
            Kind = NgKind.ParallelFork, Title = "同时做几件事", Domain = NgDomain.Logic, Color = "#FF9500",
            StdName = "并行分支", Summary = "同时扇出多条分支一起执行（例如多轴联动）。",
            Outputs = new[] { "Branch1", "Branch2", "Branch3", "Branch4", "Branch5", "Branch6", "Branch7", "Branch8" },
            Props = new[]
            {
                new NgPropDef { Name = "分支数", Default = "4", Options = "2|3|4|5|6|7|8" }
            }
        },
        [NgKind.ParallelJoin] = new()
        {
            Kind = NgKind.ParallelJoin, Title = "等几件事都做完", Domain = NgDomain.Logic, Color = "#FF9500",
            StdName = "并行汇聚", Summary = "等所有分支都执行到这里再继续往下走。",
            Props = new[]
            {
                new NgPropDef { Name = "分支数", Default = "4", Options = "2|3|4|5|6|7|8" }
            }
        },
        [NgKind.VisionOp] = new()
        {
            Kind = NgKind.VisionOp, Title = "视觉算子", Domain = NgDomain.VisionOp, Color = "#0EA5E9",
            StdName = "视觉算子", Summary = "从算子目录里挑一个视觉算子，参数按目录自动生成；结果按「变量前缀」写成变量。",
            Props = new[]
            {
                new NgPropDef { Name = "算子", Default = "" },
                new NgPropDef { Name = "变量前缀", Default = "算子" },
            }
        },
    };

    /// <summary>
    /// 「条件分支」节点的属性表：分支数 + 8 组（类型 / 名称 / 比较 / 值）+ 旧版「条件」表达式。
    /// 类型（轴位置 / 轴速度 / 输入IO / 输出IO / 变量）决定「名称」下拉的候选；
    /// 超出「分支数」的组在属性面板里自动隐藏（NgPropViewModel.IsVisible）。
    /// </summary>
    private static NgPropDef[] BuildDecisionProps()
    {
        var list = new System.Collections.Generic.List<NgPropDef>
        {
            new NgPropDef { Name = "分支数", Default = "2", Options = "2|3|4|5|6|7|8" }
        };
        for (int i = 1; i <= 8; i++)
        {
            list.Add(new NgPropDef { Name = $"条件{i}类型", Default = "变量", Options = "轴位置|轴速度|输入信号|输出信号|变量" });
            list.Add(new NgPropDef { Name = $"条件{i}名称", Default = "" });
            list.Add(new NgPropDef { Name = $"条件{i}比较", Default = "等于", Options = "等于|不等于|大于|大于等于|小于|小于等于" });
            list.Add(new NgPropDef { Name = $"条件{i}值", Default = "0" });
        }
        // 旧图 / 内置模板用的表达式条件（结构化分支都没配时作为兜底）
        list.Add(new NgPropDef { Name = "条件", Default = "" });
        return list.ToArray();
    }

    /// <summary>工具箱分组顺序（视觉 / 运控 / 通讯 / 逻辑），算子组固定排在最后 —— 现场先摆流程骨架，再往下挑算子。</summary>
    public static readonly IReadOnlyList<NgDomain> DomainOrder = new[] { NgDomain.Vision, NgDomain.Motion, NgDomain.Comm, NgDomain.Logic, NgDomain.VisionOp };

    static readonly System.Collections.Generic.Dictionary<string, string> CategoryColor = new()
    {
        ["找边与测量"] = "#0EA5E9",
        ["几何计算"] = "#6366F1",
        ["标定与换算"] = "#14B8A6",
        ["匹配与识别"] = "#F59E0B",
        ["图像与颜色"] = "#EC4899",
    };

    /// <summary>
    /// 工具箱「视觉算子」分组里的芯片：一个算子一个，全部来自 VisionOperators 目录
    /// ——目录加了算子，节点图这边自动就有，不需要再手写节点。
    /// </summary>
    public static IReadOnlyList<NgNodeDef> OpTools { get; } = BuildOpTools();

    static NgNodeDef[] BuildOpTools()
    {
        var list = new System.Collections.Generic.List<NgNodeDef>();
        foreach (var op in VisionOperators.All2)
            list.Add(new NgNodeDef
            {
                Kind = NgKind.VisionOp,
                Title = op.Name,
                Domain = NgDomain.VisionOp,
                OpKey = op.Key,
                Category = op.Category,
                StdName = "",
                Summary = op.Summary,
                Color = CategoryColor.TryGetValue(op.Category, out var c) ? c : "#0EA5E9",
                Props = OpProps(op.Name),
            });
        return list.ToArray();
    }

    /// <summary>某个算子做成节点时的属性表：算子 + 变量前缀 + 该算子目录里的全部参数。</summary>
    public static NgPropDef[] OpProps(string? opName)
    {
        var list = new System.Collections.Generic.List<NgPropDef>
        {
            new() { Name = "算子", Default = opName ?? "", Options = string.Join("|", VisionOperators.Names) },
            new() { Name = "变量前缀", Default = string.IsNullOrWhiteSpace(opName) ? "算子" : opName },
        };
        var spec = VisionOperators.All2.FirstOrDefault(a => a.Name == opName);
        if (spec == null) return list.ToArray();
        foreach (var pd in spec.Params)
            list.Add(new NgPropDef
            {
                Name = pd.Name,
                Default = pd.Kind == "bool" ? (pd.Default == "false" ? "否" : "是") : pd.Default,
                Options = pd.Kind == "bool" ? "是|否" : (pd.Options ?? ""),
            });
        return list.ToArray();
    }

    /// <summary>节点卡片标题：算子节点显示它选的算子名，其它节点显示类型名。</summary>
    public static string TitleOf(NgNode node)
    {
        if (node == null) return "";
        if (node.Kind != NgKind.VisionOp) return All.TryGetValue(node.Kind, out var d) ? d.Title : node.Kind.ToString();
        string v = OpNameOf(node);
        return v.Length == 0 ? "视觉算子" : v;
    }

    /// <summary>节点卡片颜色：算子节点跟着分类走，一眼看出它是哪族算子。</summary>
    public static string ColorOf(NgNode node)
    {
        if (node == null) return "#0EA5E9";
        if (node.Kind != NgKind.VisionOp) return All.TryGetValue(node.Kind, out var d) ? d.Color : "#0EA5E9";
        var spec = VisionOperators.All2.FirstOrDefault(a => a.Name == OpNameOf(node));
        return spec != null && CategoryColor.TryGetValue(spec.Category, out var c) ? c : "#0EA5E9";
    }

    /// <summary>算子节点当前选的算子白话名（没选返回空串）。</summary>
    public static string OpNameOf(NgNode node)
        => node?.Props?.FirstOrDefault(x => x != null && x.Name == "算子")?.Value?.Trim() ?? "";

    public static readonly IReadOnlyDictionary<NgDomain, string> DomainTitle = new Dictionary<NgDomain, string>
    {
        [NgDomain.Vision] = "视觉",
        [NgDomain.VisionOp] = "视觉算子",
        [NgDomain.Motion] = "运控",
        [NgDomain.Comm] = "通讯",
        [NgDomain.Logic] = "逻辑/变量",
    };
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░�▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥
