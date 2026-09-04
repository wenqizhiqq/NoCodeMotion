// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨ۤ
namespace NoCodeMotion.Models.NodeGraph;

/// <summary>节点三大领域：视觉 / 运控 / 通讯 / 逻辑。工具箱按此分组。</summary>
public enum NgDomain
{
    Vision,
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

    // —— 运控 ——
    Start,           // 开始
    MoveAxis,        // 轴运动
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
    Compute          // 运算
}

/// <summary>节点类型静态字典（数据驱动渲染）。</summary>
public static class NgNodeDefinitions
{
    public static IReadOnlyDictionary<NgKind, NgNodeDef> All { get; } = new Dictionary<NgKind, NgNodeDef>
    {
        // ============ 视觉 ============
        [NgKind.CamCapture] = new()
        {
            Kind = NgKind.CamCapture, Title = "图像采集", Domain = NgDomain.Vision, Color = "#10B981",
            Props = new[]
            {
                new NgPropDef { Name = "相机", Default = "相机1" },
                new NgPropDef { Name = "曝光ms", Default = "10" },
                new NgPropDef { Name = "宽度", Default = "1280" },
                new NgPropDef { Name = "高度", Default = "960" },
            }
        },
        [NgKind.TemplateMatch] = new()
        {
            Kind = NgKind.TemplateMatch, Title = "模板匹配", Domain = NgDomain.Vision, Color = "#34C759",
            Props = new[]
            {
                new NgPropDef { Name = "模板", Default = "模板1" },
                new NgPropDef { Name = "分数阈值", Default = "0.8" },
                new NgPropDef { Name = "角度范围", Default = "360" },
            }
        },
        [NgKind.DefectDetect] = new()
        {
            Kind = NgKind.DefectDetect, Title = "缺陷检测", Domain = NgDomain.Vision, Color = "#22C55E",
            Props = new[]
            {
                new NgPropDef { Name = "算法", Default = "阈值面积" },
                new NgPropDef { Name = "最小面积", Default = "50" },
                new NgPropDef { Name = "阈值", Default = "128" },
            }
        },
        [NgKind.Measure] = new()
        {
            Kind = NgKind.Measure, Title = "测量", Domain = NgDomain.Vision, Color = "#16A34A",
            Props = new[]
            {
                new NgPropDef { Name = "测量项", Default = "直径" },
                new NgPropDef { Name = "标定系数", Default = "1" },
            }
        },
        [NgKind.Align] = new()
        {
            Kind = NgKind.Align, Title = "对位", Domain = NgDomain.Vision, Color = "#059669",
            Props = new[]
            {
                new NgPropDef { Name = "基准点", Default = "P1" },
                new NgPropDef { Name = "容差", Default = "0.5" },
            }
        },
        [NgKind.Calib] = new()
        {
            Kind = NgKind.Calib, Title = "标定", Domain = NgDomain.Vision, Color = "#047857",
            Props = new[]
            {
                new NgPropDef { Name = "标定板", Default = "圆点9x9" },
                new NgPropDef { Name = "格子尺寸", Default = "10" },
            }
        },

        // ============ 运控 ============
        [NgKind.Start] = new()
        {
            Kind = NgKind.Start, Title = "开始", Domain = NgDomain.Motion, Color = "#37BC9B", HasInput = false,
        },
        [NgKind.MoveAxis] = new()
        {
            Kind = NgKind.MoveAxis, Title = "轴运动", Domain = NgDomain.Motion, Color = "#4A89DC",
            Props = new[]
            {
                new NgPropDef { Name = "轴", Default = "X" },
                new NgPropDef { Name = "模式", Default = "绝对", Options = "绝对|相对" },
                new NgPropDef { Name = "目标位置", Default = "0" },
                new NgPropDef { Name = "速度", Default = "10" },
                new NgPropDef { Name = "加速度", Default = "50" },
            }
        },
        [NgKind.Home] = new()
        {
            Kind = NgKind.Home, Title = "回零", Domain = NgDomain.Motion, Color = "#5D9CEC",
            Props = new[]
            {
                new NgPropDef { Name = "轴", Default = "X" },
                new NgPropDef { Name = "方向", Default = "负向", Options = "负向|正向" },
                new NgPropDef { Name = "模式", Default = "ORG", Options = "ORG|限位反向|Index" },
            }
        },
        [NgKind.Delay] = new()
        {
            Kind = NgKind.Delay, Title = "延时", Domain = NgDomain.Motion, Color = "#F6BB42",
            Props = new[] { new NgPropDef { Name = "时间ms", Default = "500" } }
        },
        [NgKind.WaitInput] = new()
        {
            Kind = NgKind.WaitInput, Title = "等待输入", Domain = NgDomain.Motion, Color = "#8CC152",
            Props = new[]
            {
                new NgPropDef { Name = "信号", Default = "IN0" },
                new NgPropDef { Name = "状态", Default = "高电平", Options = "高电平|低电平" },
                new NgPropDef { Name = "超时ms", Default = "0" },
            }
        },
        [NgKind.Decision] = new()
        {
            Kind = NgKind.Decision, Title = "条件分支", Domain = NgDomain.Motion, Color = "#E9573F",
            Outputs = new[] { "True", "False" },
            Props = new[] { new NgPropDef { Name = "条件", Default = "PosX>=10" } }
        },
        [NgKind.Loop] = new()
        {
            Kind = NgKind.Loop, Title = "循环", Domain = NgDomain.Motion, Color = "#967ADC",
            Outputs = new[] { "Body", "Exit" },
            Props = new[] { new NgPropDef { Name = "次数", Default = "3" } }
        },
        [NgKind.End] = new()
        {
            Kind = NgKind.End, Title = "结束", Domain = NgDomain.Motion, Color = "#DA4453",
            Outputs = System.Array.Empty<string>(),
        },

        // —— 运控扩展（物理动作 / 信号）——
        [NgKind.WaitAxis] = new()
        {
            Kind = NgKind.WaitAxis, Title = "等待轴到位", Domain = NgDomain.Motion, Color = "#48CFAD",
            Props = new[] { new NgPropDef { Name = "轴", Default = "X" } }
        },
        [NgKind.Cylinder] = new()
        {
            Kind = NgKind.Cylinder, Title = "气缸动作", Domain = NgDomain.Motion, Color = "#EC87C0",
            Props = new[]
            {
                new NgPropDef { Name = "气缸", Default = "夹爪" },
                new NgPropDef { Name = "动作", Default = "伸出", Options = "伸出|缩回" },
            }
        },
        [NgKind.PointGo] = new()
        {
            Kind = NgKind.PointGo, Title = "点位移动", Domain = NgDomain.Motion, Color = "#4FC1E9",
            Props = new[]
            {
                new NgPropDef { Name = "点位表", Default = "取放工位" },
                new NgPropDef { Name = "点位", Default = "取料点" },
            }
        },
        [NgKind.IoWrite] = new()
        {
            Kind = NgKind.IoWrite, Title = "写输出", Domain = NgDomain.Motion, Color = "#A0D468",
            Props = new[]
            {
                new NgPropDef { Name = "输出", Default = "光源" },
                new NgPropDef { Name = "值", Default = "1", Options = "1|0" },
            }
        },

        // ============ 通讯 ============
        [NgKind.ModbusSend] = new()
        {
            Kind = NgKind.ModbusSend, Title = "Modbus发送", Domain = NgDomain.Comm, Color = "#AF52DE",
            Props = new[]
            {
                new NgPropDef { Name = "通讯", Default = "通讯1" },
                new NgPropDef { Name = "指令", Default = "write" },
            }
        },
        [NgKind.ModbusRecv] = new()
        {
            Kind = NgKind.ModbusRecv, Title = "Modbus接收", Domain = NgDomain.Comm, Color = "#B06BE0",
            Props = new[]
            {
                new NgPropDef { Name = "通讯", Default = "通讯1" },
                new NgPropDef { Name = "关键字", Default = "OK" },
            }
        },
        [NgKind.TcpSend] = new()
        {
            Kind = NgKind.TcpSend, Title = "TCP发送", Domain = NgDomain.Comm, Color = "#9B51E0",
            Props = new[]
            {
                new NgPropDef { Name = "端点", Default = "192.168.1.10:8000" },
                new NgPropDef { Name = "报文", Default = "hello" },
            }
        },
        [NgKind.McuWrite] = new()
        {
            Kind = NgKind.McuWrite, Title = "下位机写", Domain = NgDomain.Comm, Color = "#8E44AD",
            Props = new[]
            {
                new NgPropDef { Name = "设备", Default = "MCU1" },
                new NgPropDef { Name = "数据", Default = "0x01" },
            }
        },

        // ============ 逻辑 / 变量 ============
        [NgKind.VarSet] = new()
        {
            Kind = NgKind.VarSet, Title = "设置变量", Domain = NgDomain.Logic, Color = "#ED5565",
            Props = new[]
            {
                new NgPropDef { Name = "变量", Default = "计数" },
                new NgPropDef { Name = "值", Default = "0" },
            }
        },
        [NgKind.Compute] = new()
        {
            Kind = NgKind.Compute, Title = "运算", Domain = NgDomain.Logic, Color = "#DA4453",
            Props = new[]
            {
                new NgPropDef { Name = "变量", Default = "结果" },
                new NgPropDef { Name = "表达式", Default = "计数 + 1" },
            }
        },
    };

    /// <summary>工具箱分组顺序（视觉 / 运控 / 通讯 / 逻辑）。</summary>
    public static readonly IReadOnlyList<NgDomain> DomainOrder = new[] { NgDomain.Vision, NgDomain.Motion, NgDomain.Comm, NgDomain.Logic };

    public static readonly IReadOnlyDictionary<NgDomain, string> DomainTitle = new Dictionary<NgDomain, string>
    {
        [NgDomain.Vision] = "视觉",
        [NgDomain.Motion] = "运控",
        [NgDomain.Comm] = "通讯",
        [NgDomain.Logic] = "逻辑/变量",
    };
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░�▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥
