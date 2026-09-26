// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// ◆温​启‍志‌◆​编‍写⁣◇⁣微⁣信⁠﹕‍1​8‌7⁣◆‎1‏9⁠3‎6‎◇‌1​3‎9‍9‏　​※‏保⁣留‍所‌有‍权‎利​请‎勿‎删‎除⁠◇​⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// =====================================================================
// 新建工程弹窗所用的「项目模板」目录。
// 每个模板是一个 ProjectTemplate，Factory() 每次返回全新的 ProjectData。
//
    // 模板总数：23 个（含 1 个空白）。
    // 分类：空白 / 轴运动(6) / 气缸(2) / IO(2) / 综合(9) / 半导体(3)。
    // 综合(7) 中「仿真演示」为专为 3D 仿真设计的示例：运行即可看到轴/气缸/相机动起来。
    // 半导体(3)：固晶机 / 探针台 / 平移式分选机。
    //   平移式分选机 共用 AddSemiEquipment（3自动盘+3手动盘+上料/下空盘+2高温中转盘+16吸嘴+搬运手臂+2 Z压力测+GPIB）；
    //   固晶机 / 探针台 各有专属配置（固晶头/顶针/点胶；真空吸盘/精密载台/探针Z + GPIB）。
// 覆盖：控制器 / 轴 / IO(入+出) / 气缸 / 点位表 / 通讯 / 料盘 / 相机 / 变量 / 流程（主流程多个 + 复位流程）。
// 流程：除空白模板外，每个模板都会经 ProjectTemplate.Build() → EnsureNodeGraphFlow() 自动补一条
//       「示例(节点图)」节点图流程（本来就自带节点图的 3 个模板不重复加），
//       保证新建出来的工程在流程页里一定能看到「节点图」这类流程长什么样。
// =====================================================================
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NoCodeMotion.Models;
using NoCodeMotion.Models.NodeGraph;

namespace NoCodeMotion.Services
{
    public static class ProjectTemplateCatalog
    {
        /// <summary>全部模板（顺序即弹窗中显示顺序，按 Category 分组渲染）。</summary>
        public static IReadOnlyList<ProjectTemplate> All { get; } = new List<ProjectTemplate>
        {
            // ---------- 空白 ----------
            Empty(),

            // ---------- 轴运动 (6) ----------
            SingleAxis(),
            TwoAxis(),
            ThreeAxisXyz(),
            FourAxisXyzr(),
            Scara(),
            SixAxis(),

            // ---------- 气缸 (2) ----------
            SimpleCylinder(),
            MultiCylinder(),

            // ---------- IO (2) ----------
            Io8x8(),
            Io16x16(),

            // ---------- 综合 (9) ----------
            PointPick(),
            DualStation(),
            AssemblyLine(),
            VisionGuided(),
            MultiProduct(),
            FullFeatured(),
            SimDemo(),
            VisionSort(),
            Dispensing(),

            // ---------- 半导体 (3) ----------
            DieBoner(),
            ProbeStation(),
            TransferHandler(),
        };

        // ====================================================================
        // 模板定义：每个方法返回一个 ProjectTemplate。
        // 工厂方法体内只构造 ProjectData，不持有任何共享可变状态——同一模板
        // 被多次 Build() 出来是互不污染的独立实例。
        // ====================================================================

        private static ProjectTemplate Empty() => new()
        {
            Id = "empty",
            Name = "空白工程",
            Category = "空白",
            Description = "从零开始，所有页面均为空。",
            Summary = "无任何预设数据",
            Highlights = new[] { "0 个控制器", "0 个轴", "0 个 IO", "0 个气缸", "0 个流程" },
            Factory = () => new ProjectData(),
        };

        // =================== 轴运动 ===================

        private static ProjectTemplate SingleAxis() => new()
        {
            Id = "single-axis",
            Name = "单轴点动",
            Category = "轴运动",
            Description = "1 个控制卡 + 1 个 X 轴 + 4 入 4 出 + Modbus 主站，含点动 / 复位两条流程。",
            Summary = "1 控制 · 1 轴 · 4 入 4 出 · 1 通讯 · 2 变量 · 1 主流程 · 1 复位",
            Highlights = new[]
            {
                "控制器：雷赛 DMC5400 (脉冲)",
                "轴：X 脉冲轴，单位 mm",
                "输入：启动 / 停止 / 复位 / 急停",
                "输出：运行 / 就绪 / 报警 / 完成",
                "通讯：Modbus 主站 COM1 (9600)",
                "变量：计数 / 总数",
                "主流程：等待启动 → 移动到 100mm",
                "复位流程：X 回零 + 清报警"
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("控制卡1", "雷赛", "DMC5400", 0, 4, "脉冲", "PCI"));
                d.Axes.Add(Ax("X", "控制卡1", "脉冲", 0, "mm", 100, 50, 50));
                d.Inputs.Add(In("启动", "启动按钮", "控制卡1", 0, 0, 0));
                d.Inputs.Add(In("停止", "停止按钮", "控制卡1", 0, 0, 1));
                d.Inputs.Add(In("复位", "复位按钮", "控制卡1", 0, 0, 2));
                d.Inputs.Add(In("急停", "安全门", "控制卡1", 0, 0, 3));
                d.Outputs.Add(Out("运行", "动点", "控制卡1", 0, 0, 0));
                d.Outputs.Add(Out("就绪", "动点", "控制卡1", 0, 0, 1));
                d.Outputs.Add(Out("报警", "动点", "控制卡1", 0, 0, 2));
                d.Outputs.Add(Out("完成", "动点", "控制卡1", 0, 0, 3));
                d.Comms.Add(Comm("Modbus主站", "ModbusRTU", "COM1", 9600));
                AddVars(d, ("计数", "0"), ("总数", "0"));
                d.Flows.Add(TblFlow("主流程", FlowRole.Main,

                    CommentStep("主流程" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("就绪", "1"),
                    SetIO("运行", "1"),
                    MoveAxis("X", 100, 1000),
                    WaitStep(200),
                    MoveAxis("X", 0, 1000),
                    SetIO("运行", "0"),
                    ElseStep(),
                    CommentStep("未收到启动信号，本周期跳过"),
                    EndIfStep(),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,

                    CommentStep("复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    HomeAxis("X"),
                    SetIO("就绪", "1")));
                return d;
            },
        };

        private static ProjectTemplate TwoAxis() => new()
        {
            Id = "two-axis",
            Name = "两轴同步",
            Category = "轴运动",
            Description = "1 个控制卡 + X/Y 两轴 + 6 入 6 出 + Modbus 主站 / 串口，含 XY 同步走位。",
            Summary = "1 控制 · 2 轴 · 6 入 6 出 · 1 通讯 · 2 变量 · 2 主流程 · 1 复位",
            Highlights = new[]
            {
                "控制器：固高 GHN_FB (PCI)",
                "轴：X/Y 同步直线插补",
                "输入：启动 / 停止 / 复位 / 急停 / 暂停 / 手自动",
                "输出：运行 / 就绪 / 报警 / 完成 / 暂停 / 同步",
                "通讯：Modbus 主站 + 串口扫描枪",
                "变量：计数 / 总数",
                "主流程1：XY 同步到 (100, 50)",
                "主流程2：XY 同步到 (200, 150)",
                "复位流程：X/Y 依次回零"
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("控制卡1", "固高", "GHN_FB", 0, 4, "脉冲", "PCI"));
                d.Axes.Add(Ax("X", "控制卡1", "脉冲", 0, "mm", 200, 100, 100));
                d.Axes.Add(Ax("Y", "控制卡1", "脉冲", 1, "mm", 200, 100, 100));
                d.Inputs.Add(In("启动", "启动按钮", "控制卡1", 0, 0, 0));
                d.Inputs.Add(In("停止", "停止按钮", "控制卡1", 0, 0, 1));
                d.Inputs.Add(In("复位", "复位按钮", "控制卡1", 0, 0, 2));
                d.Inputs.Add(In("急停", "安全门", "控制卡1", 0, 0, 3));
                d.Inputs.Add(In("暂停", "动点", "控制卡1", 0, 0, 4));
                d.Inputs.Add(In("手自动", "动点", "控制卡1", 0, 0, 5));
                d.Outputs.Add(Out("运行", "动点", "控制卡1", 0, 0, 0));
                d.Outputs.Add(Out("就绪", "动点", "控制卡1", 0, 0, 1));
                d.Outputs.Add(Out("报警", "动点", "控制卡1", 0, 0, 2));
                d.Outputs.Add(Out("完成", "动点", "控制卡1", 0, 0, 3));
                d.Outputs.Add(Out("暂停", "动点", "控制卡1", 0, 0, 4));
                d.Outputs.Add(Out("同步", "动点", "控制卡1", 0, 0, 5));
                d.Comms.Add(Comm("Modbus主站", "ModbusRTU", "COM1", 9600));
                AddVars(d, ("计数", "0"), ("总数", "0"));
                d.Flows.Add(TblFlow("主流程1", FlowRole.Main,

                    CommentStep("主流程1" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 100, 800),
                    MoveAxis("Y", 50, 800),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("主流程2", FlowRole.Main,

                    CommentStep("主流程2" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 200, 1200),
                    MoveAxis("Y", 150, 1200),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,

                    CommentStep("复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    HomeAxis("X"),
                    HomeAxis("Y"),
                    SetIO("就绪", "1")));
                return d;
            },
        };

        private static ProjectTemplate ThreeAxisXyz() => new()
        {
            Id = "three-axis-xyz",
            Name = "三轴 XYZ 直角",
            Category = "轴运动",
            Description = "经典 XYZ 三轴直角机器人（上下料 / 移载 / 点胶等），含 Modbus 主站 + 变量。",
            Summary = "1 控制 · 3 轴 · 8 入 8 出 · 1 通讯 · 2 变量 · 2 主流程 · 1 复位",
            Highlights = new[]
            {
                "控制器：雷赛 DMC5800 (PCI)",
                "轴：X/Y/Z，单位 mm，Z 轴带抱闸逻辑",
                "输入：8 路（启动/停止/复位/急停/暂停/手自动/原点到位/Z 抱闸确认）",
                "输出：8 路（运行/就绪/报警/完成/暂停/Z 抱闸/真空/下料）",
                "通讯：Modbus 主站",
                "变量：计数 / 总数",
                "主流程1：XY 走位 + Z 下降",
                "主流程2：XY 走位 + Z 上升",
                "复位流程：Z 先抬升 → X/Y 归零"
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("控制卡1", "雷赛", "DMC5800", 0, 6, "脉冲", "PCI"));
                d.Axes.Add(Ax("X", "控制卡1", "脉冲", 0, "mm", 300, 150, 150));
                d.Axes.Add(Ax("Y", "控制卡1", "脉冲", 1, "mm", 300, 150, 150));
                d.Axes.Add(Ax("Z", "控制卡1", "脉冲", 2, "mm", 100, 80, 80));
                string[] inFns = { "启动按钮", "停止按钮", "复位按钮", "安全门", "动点", "动点", "原点", "动点" };
                string[] inNames = { "启动", "停止", "复位", "急停", "暂停", "手自动", "原点到位", "Z上限" };
                for (int i = 0; i < 8; i++) d.Inputs.Add(In(inNames[i], inFns[i], "控制卡1", 0, 0, i));
                string[] outNames = { "运行", "就绪", "报警", "完成", "暂停", "Z抱闸", "真空", "下料" };
                for (int i = 0; i < 8; i++) d.Outputs.Add(Out(outNames[i], "动点", "控制卡1", 0, 0, i));
                d.Comms.Add(Comm("Modbus主站", "ModbusRTU", "COM1", 9600));
                AddVars(d, ("计数", "0"), ("总数", "0"));
                d.Flows.Add(TblFlow("取料", FlowRole.Main,

                    CommentStep("取料" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("Z", 0, 600),
                    MoveAxis("X", 200, 800),
                    MoveAxis("Y", 100, 800),
                    SetIO("真空", "1"),
                    WaitStep(300),
                    MoveAxis("Z", 50, 600),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("放料", FlowRole.Main,

                    CommentStep("放料" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    MoveAxis("X", 400, 800),
                    MoveAxis("Y", 300, 800),
                    SetIO("下料", "1"),
                    WaitStep(300),
                    SetIO("真空", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,

                    CommentStep("复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    SetIO("Z抱闸", "0"),
                    MoveAxis("Z", 0, 800),
                    HomeAxis("X"),
                    HomeAxis("Y"),
                    HomeAxis("Z"),
                    SetIO("就绪", "1")));
                return d;
            },
        };

        private static ProjectTemplate FourAxisXyzr() => new()
        {
            Id = "four-axis-xyzr",
            Name = "四轴 XYZR 龙门",
            Category = "轴运动",
            Description = "XYZ + R 旋转四轴龙门（点胶 / 螺丝机 / 贴标），含 2 通讯 + 1 料盘 + 变量。",
            Summary = "1 控制 · 4 轴 · 8 入 8 出 · 1 料盘 · 2 通讯 · 2 变量 · 3 主流程 · 1 复位",
            Highlights = new[]
            {
                "控制器：EtherCAT 主站",
                "轴：X/Y/Z 直线 + R 旋转轴",
                "输入：8 路（启动/停止/复位/急停/暂停/手自动/原点到位/R 编码器Z）",
                "输出：8 路（运行/就绪/报警/完成/暂停/真空/R 锁紧/点胶阀）",
                "料盘：原料盘 8×6 (16mm 间距)",
                "通讯：Modbus 主站 + 串口扫描枪",
                "变量：计数 / 总数",
                "主流程1：点胶动作（XY 移动 + R 旋转 + 阀开）",
                "主流程2：贴标动作（XY 移动 + R 旋转）",
                "主流程3：自动循环",
                "复位流程：R 锁紧 + Z 抬升 + X/Y/R 归零"
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("控制卡1", "雷赛", "EtherCAT主站", 0, 8, "EtherCAT", "网口"));
                d.Axes.Add(Ax("X", "控制卡1", "EtherCAT", 0, "mm", 500, 250, 250));
                d.Axes.Add(Ax("Y", "控制卡1", "EtherCAT", 1, "mm", 500, 250, 250));
                d.Axes.Add(Ax("Z", "控制卡1", "EtherCAT", 2, "mm", 200, 100, 100));
                d.Axes.Add(Ax("R", "控制卡1", "EtherCAT", 3, "°", 360, 180, 180));
                string[] inNames = { "启动", "停止", "复位", "急停", "暂停", "手自动", "原点到位", "RZ信号" };
                for (int i = 0; i < 8; i++) d.Inputs.Add(In(inNames[i], "动点", "控制卡1", 0, 0, i));
                string[] outNames = { "运行", "就绪", "报警", "完成", "暂停", "真空", "R锁紧", "点胶阀" };
                for (int i = 0; i < 8; i++) d.Outputs.Add(Out(outNames[i], "动点", "控制卡1", 0, 0, i));
                d.Trays.Add(Tray("原料盘", 8, 6, 0, 0, 16, 16));
                d.Comms.Add(Comm("Modbus主站", "ModbusTCP", "192.168.1.10", 502));
                d.Comms.Add(Comm("扫码枪", "串口", "COM2", 9600));
                AddVars(d, ("计数", "0"), ("总数", "0"));
                d.Flows.Add(TblFlow("点胶", FlowRole.Main,

                    CommentStep("点胶" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("R", 45, 600),
                    MoveAxis("X", 100, 800),
                    MoveAxis("Y", 200, 800),
                    MoveAxis("Z", -10, 400),
                    SetIO("点胶阀", "1"),
                    WaitStep(500),
                    SetIO("点胶阀", "0"),
                    MoveAxis("Z", 0, 400),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("贴标", FlowRole.Main,

                    CommentStep("贴标" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("R", 90, 600),
                    MoveAxis("X", 200, 800),
                    MoveAxis("Y", 100, 800),
                    MoveAxis("Z", -5, 400),
                    WaitStep(300),
                    MoveAxis("Z", 0, 400),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("自动循环", FlowRole.Main,

                    CommentStep("自动循环" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    LoopStart(3),
                    MoveAxis("X", 50, 600),
                    MoveAxis("Y", 50, 600),
                    WaitStep(200),
                    MoveAxis("X", 250, 600),
                    MoveAxis("Y", 250, 600),
                    WaitStep(200),
                    LoopEnd(),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,

                    CommentStep("复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    SetIO("R锁紧", "1"),
                    MoveAxis("Z", 0, 800),
                    HomeAxis("R"),
                    HomeAxis("X"),
                    HomeAxis("Y"),
                    HomeAxis("Z"),
                    SetIO("R锁紧", "0"),
                    SetIO("就绪", "1")));
                return d;
            },
        };

        private static ProjectTemplate Scara() => new()
        {
            Id = "scara",
            Name = "SCARA 水平多关节",
            Category = "轴运动",
            Description = "J1/J2/Z/R 四轴 SCARA 水平多关节机器人，含示教/运行/暂停/复位 + 2 通讯 + 1 料盘 + 变量。",
            Summary = "1 控制 · 4 轴 · 8 入 8 出 · 1 料盘 · 2 通讯 · 2 变量 · 4 主流程 · 2 复位",
            Highlights = new[]
            {
                "控制器：EtherCAT 主站",
                "轴：J1 大臂 + J2 小臂（°） + Z 升降（mm） + R 旋转（°）",
                "输入：8 路（启动/停止/复位/急停/暂停/手自动/示教/R编码器Z）",
                "输出：8 路（运行/就绪/报警/完成/暂停/真空/夹爪/R锁紧）",
                "料盘：原料盘 10×8 (15mm 间距)",
                "通讯：Modbus 主站 + 串口扫描枪",
                "变量：计数 / 总数 / 当前产品",
                "主流程1：自动取料",
                "主流程2：自动放料",
                "主流程3：示教记录",
                "主流程4：自动循环",
                "复位1：所有轴归零",
                "复位2：仅 Z 抬升 + 真空关"
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("控制卡1", "雷赛", "EtherCAT主站", 0, 8, "EtherCAT", "网口"));
                d.Axes.Add(Ax("J1", "控制卡1", "EtherCAT", 0, "°", 360, 200, 200));
                d.Axes.Add(Ax("J2", "控制卡1", "EtherCAT", 1, "°", 360, 200, 200));
                d.Axes.Add(Ax("Z", "控制卡1", "EtherCAT", 2, "mm", 200, 100, 100));
                d.Axes.Add(Ax("R", "控制卡1", "EtherCAT", 3, "°", 720, 360, 360));
                string[] inNames = { "启动", "停止", "复位", "急停", "暂停", "手自动", "示教", "RZ" };
                for (int i = 0; i < 8; i++) d.Inputs.Add(In(inNames[i], "动点", "控制卡1", 0, 0, i));
                string[] outNames = { "运行", "就绪", "报警", "完成", "暂停", "真空", "夹爪", "R锁紧" };
                for (int i = 0; i < 8; i++) d.Outputs.Add(Out(outNames[i], "动点", "控制卡1", 0, 0, i));
                d.Trays.Add(Tray("原料盘", 10, 8, 0, 0, 15, 15));
                d.Comms.Add(Comm("Modbus主站", "ModbusTCP", "192.168.1.10", 502));
                d.Comms.Add(Comm("扫码枪", "串口", "COM2", 9600));
                AddVars(d, ("计数", "0"), ("总数", "0"), ("当前产品", "A"));
                d.Flows.Add(TblFlow("自动取料", FlowRole.Main,

                    CommentStep("自动取料" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("J1", 30, 800),
                    MoveAxis("J2", -20, 800),
                    MoveAxis("Z", -30, 400),
                    SetIO("真空", "1"),
                    WaitStep(300),
                    MoveAxis("Z", 0, 400),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("自动放料", FlowRole.Main,

                    CommentStep("自动放料" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("J1", -45, 800),
                    MoveAxis("J2", 45, 800),
                    MoveAxis("R", 180, 600),
                    MoveAxis("Z", -25, 400),
                    SetIO("真空", "0"),
                    SetIO("夹爪", "1"),
                    WaitStep(300),
                    MoveAxis("Z", 0, 400),
                    SetIO("夹爪", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("示教记录", FlowRole.Main,

                    CommentStep("示教记录" + " 流程示例"),
                    WaitIO("示教", "1", 60000),
                    SetIO("暂停", "1"),
                    WaitStep(200),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("自动循环", FlowRole.Main,

                    CommentStep("自动循环" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    LoopStart(3),
                    MoveAxis("J1", 0, 600),
                    MoveAxis("J2", 0, 600),
                    MoveAxis("R", 0, 400),
                    WaitStep(500),
                    LoopEnd(),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("完整复位", FlowRole.Reset,

                    CommentStep("完整复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    SetIO("真空", "0"),
                    SetIO("夹爪", "0"),
                    MoveAxis("Z", 0, 600),
                    HomeAxis("J1"),
                    HomeAxis("J2"),
                    HomeAxis("R"),
                    HomeAxis("Z"),
                    SetIO("就绪", "1")));
                d.Flows.Add(TblFlow("快速归位", FlowRole.Reset,

                    CommentStep("快速归位" + " 流程示例"),
                    SetIO("真空", "0"),
                    MoveAxis("Z", 0, 600),
                    SetIO("就绪", "1")));
                return d;
            },
        };

        private static ProjectTemplate SixAxis() => new()
        {
            Id = "six-axis",
            Name = "六轴串联机械手",
            Category = "轴运动",
            Description = "2 块控制卡 + 6 轴（J1-J6）+ 16 IO + 2 通讯 + 1 料盘 + 变量，全功能串联机械手工程。",
            Summary = "2 控制 · 6 轴 · 16 入 16 出 · 1 料盘 · 2 通讯 · 3 变量 · 5 主流程 · 2 复位",
            Highlights = new[]
            {
                "控制器1：EtherCAT 主站（控制 J1/J2/J3）",
                "控制器2：EtherCAT 扩展（控制 J4/J5/J6）",
                "轴：J1 基座 + J2 肩 + J3 肘 + J4 腕旋 + J5 腕俯 + J6 腕摆",
                "输入：16 路（启动/停止/复位/急停/暂停/手自动/示教/原点 等）",
                "输出：16 路（运行/就绪/报警/完成/真空/夹爪/工装 等）",
                "料盘：原料盘 12×8 (12mm 间距)",
                "通讯：Modbus 主站 + Modbus 扩展IO",
                "变量：计数 / 总数 / 当前产品",
                "5 个主流程：取料/放料/示教/点焊/搬运",
                "2 个复位：完整复位 / 快速归位"
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("控制卡1", "雷赛", "EtherCAT主站", 0, 8, "EtherCAT", "网口"));
                d.Controllers.Add(Ctl("控制卡2", "雷赛", "EtherCAT扩展", 1, 8, "EtherCAT", "网口"));
                d.Axes.Add(Ax("J1", "控制卡1", "EtherCAT", 0, "°", 180, 90, 90));
                d.Axes.Add(Ax("J2", "控制卡1", "EtherCAT", 1, "°", 180, 90, 90));
                d.Axes.Add(Ax("J3", "控制卡1", "EtherCAT", 2, "°", 180, 90, 90));
                d.Axes.Add(Ax("J4", "控制卡2", "EtherCAT", 0, "°", 360, 180, 180));
                d.Axes.Add(Ax("J5", "控制卡2", "EtherCAT", 1, "°", 360, 180, 180));
                d.Axes.Add(Ax("J6", "控制卡2", "EtherCAT", 2, "°", 720, 360, 360));
                string[] inNames = { "启动", "停止", "复位", "急停", "暂停", "手自动", "示教", "原点",
                                     "J1限位", "J2限位", "J3限位", "J4限位", "J5限位", "J6限位", "安全门", "允许启动" };
                string[] inFns = { "启动按钮", "停止按钮", "复位按钮", "安全门", "动点", "动点", "动点", "原点",
                                   "动点", "动点", "动点", "动点", "动点", "动点", "安全门", "启动按钮" };
                for (int i = 0; i < 16; i++) d.Inputs.Add(In(inNames[i], inFns[i], i < 8 ? "控制卡1" : "控制卡2", i < 8 ? 0 : 1, 0, i % 8));
                string[] outNames = { "运行", "就绪", "报警", "完成", "暂停", "真空", "夹爪", "工装1",
                                      "工装2", "焊接", "冷却", "润滑", "绿灯", "红灯", "黄灯", "蜂鸣" };
                for (int i = 0; i < 16; i++) d.Outputs.Add(Out(outNames[i], "动点", i < 8 ? "控制卡1" : "控制卡2", i < 8 ? 0 : 1, 0, i % 8));
                d.Trays.Add(Tray("原料盘", 12, 8, 0, 0, 12, 12));
                d.Comms.Add(Comm("Modbus主站", "ModbusTCP", "192.168.1.10", 502));
                d.Comms.Add(Comm("ModbusIO", "ModbusTCP", "192.168.1.20", 502));
                AddVars(d, ("计数", "0"), ("总数", "0"), ("当前产品", "A"));
                d.Flows.Add(TblFlow("取料", FlowRole.Main,

                    CommentStep("取料" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("J1", 0, 600),
                    MoveAxis("J2", 0, 600),
                    MoveAxis("J3", 0, 600),
                    MoveAxis("J4", 0, 600),
                    MoveAxis("J5", 0, 600),
                    MoveAxis("J6", 0, 600),
                    SetIO("真空", "1"),
                    WaitStep(500),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("放料", FlowRole.Main,

                    CommentStep("放料" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("J1", 90, 800),
                    MoveAxis("J2", -45, 800),
                    MoveAxis("J3", 30, 800),
                    MoveAxis("J4", 0, 600),
                    MoveAxis("J5", 45, 600),
                    MoveAxis("J6", 90, 600),
                    SetIO("真空", "0"),
                    WaitStep(300),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("示教", FlowRole.Main,

                    CommentStep("示教" + " 流程示例"),
                    WaitIO("示教", "1", 60000),
                    SetIO("暂停", "1"),
                    WaitStep(200),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("点焊", FlowRole.Main,

                    CommentStep("点焊" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("J1", 45, 600),
                    MoveAxis("J2", 30, 600),
                    MoveAxis("J3", -30, 600),
                    SetIO("焊接", "1"),
                    WaitStep(800),
                    SetIO("焊接", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("搬运循环", FlowRole.Main,

                    CommentStep("搬运循环" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    LoopStart(3),
                    MoveAxis("J1", 0, 600),
                    MoveAxis("J2", 0, 600),
                    MoveAxis("J3", 0, 600),
                    SetIO("工装1", "1"),
                    WaitStep(500),
                    SetIO("工装1", "0"),
                    LoopEnd(),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("完整复位", FlowRole.Reset,

                    CommentStep("完整复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    SetIO("真空", "0"),
                    SetIO("夹爪", "0"),
                    SetIO("焊接", "0"),
                    HomeAxis("J1"),
                    HomeAxis("J2"),
                    HomeAxis("J3"),
                    HomeAxis("J4"),
                    HomeAxis("J5"),
                    HomeAxis("J6"),
                    SetIO("就绪", "1")));
                d.Flows.Add(TblFlow("快速归位", FlowRole.Reset,

                    CommentStep("快速归位" + " 流程示例"),
                    SetIO("报警", "0"),
                    SetIO("真空", "0"),
                    SetIO("完成", "1")));
                return d;
            },
        };

        // =================== 气缸 ===================

        private static ProjectTemplate SimpleCylinder() => new()
        {
            Id = "simple-cylinder",
            Name = "简单气缸",
            Category = "气缸",
            Description = "2 个双作用气缸 + 4 入 4 出 + Modbus 主站 + 变量，最小气缸演示工程。",
            Summary = "4 入 4 出 · 2 气缸 · 1 通讯 · 2 变量 · 1 主流程 · 1 复位",
            Highlights = new[]
            {
                "气缸 1：推料气缸（伸出/缩回）",
                "气缸 2：挡料气缸（伸出/缩回）",
                "输入：启动 / 停止 / 复位 / 急停",
                "输出：运行 / 就绪 / 报警 / 完成",
                "通讯：Modbus 主站",
                "变量：计数 / 总数",
                "主流程：推料 → 延时 → 挡料",
                "复位流程：所有气缸缩回"
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Inputs.Add(In("启动", "启动按钮"));
                d.Inputs.Add(In("停止", "停止按钮"));
                d.Inputs.Add(In("复位", "复位按钮"));
                d.Inputs.Add(In("急停", "安全门"));
                d.Outputs.Add(Out("运行", "动点"));
                d.Outputs.Add(Out("就绪", "动点"));
                d.Outputs.Add(Out("报警", "动点"));
                d.Outputs.Add(Out("完成", "动点"));
                d.Cylinders.Add(Cyl("推料", "Y0", "X0", "X1"));
                d.Cylinders.Add(Cyl("挡料", "Y1", "X2", "X3"));
                d.Comms.Add(Comm("Modbus主站", "ModbusRTU", "COM1", 9600));
                AddVars(d, ("计数", "0"), ("总数", "0"));
                d.Flows.Add(TblFlow("主流程", FlowRole.Main,

                    CommentStep("主流程" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    CylOut("推料"),
                    WaitStep(500),
                    CylOut("挡料"),
                    WaitStep(500),
                    CylBack("推料"),
                    CylBack("挡料"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,

                    CommentStep("复位" + " 流程示例"),
                    CylBack("推料"),
                    CylBack("挡料"),
                    SetIO("报警", "0"),
                    SetIO("就绪", "1")));
                return d;
            },
        };

        private static ProjectTemplate MultiCylinder() => new()
        {
            Id = "multi-cylinder",
            Name = "多气缸装配",
            Category = "气缸",
            Description = "4 个气缸（送料 / 夹紧 / 打螺丝 / 顶升）+ 8 IO + 2 通讯 + 变量，典型装配工程。",
            Summary = "8 入 8 出 · 4 气缸 · 2 通讯 · 2 变量 · 2 主流程 · 1 复位",
            Highlights = new[]
            {
                "气缸 1：送料（Y0/X0/X1）",
                "气缸 2：夹紧（Y1/X2/X3）",
                "气缸 3：打螺丝（Y2/X4/X5）",
                "气缸 4：顶升（Y3/X6/X7）",
                "输入：启动/停止/复位/急停/手自动/暂停/送料完成/装配完成",
                "输出：运行/就绪/报警/完成/送料/夹紧/打螺丝/顶升",
                "通讯：Modbus 主站 + Modbus 压力传感器",
                "变量：计数 / 总数",
                "主流程1：装配动作链",
                "主流程2：循环装配",
                "复位：所有气缸缩回 + 报警清"
            },
            Factory = () =>
            {
                var d = new ProjectData();
                string[] inNames = { "启动", "停止", "复位", "急停", "手自动", "暂停", "送料完成", "装配完成" };
                for (int i = 0; i < 8; i++) d.Inputs.Add(In(inNames[i], "动点", "", 0, 0, i));
                string[] outNames = { "运行", "就绪", "报警", "完成", "送料", "夹紧", "打螺丝", "顶升" };
                for (int i = 0; i < 8; i++) d.Outputs.Add(Out(outNames[i], "动点", "", 0, 0, i));
                d.Cylinders.Add(Cyl("送料", "Y0", "X0", "X1"));
                d.Cylinders.Add(Cyl("夹紧", "Y1", "X2", "X3"));
                d.Cylinders.Add(Cyl("打螺丝", "Y2", "X4", "X5"));
                d.Cylinders.Add(Cyl("顶升", "Y3", "X6", "X7"));
                // 提供 X/Y 两轴，使内置「脚本流程」Lua 示例（演示 Axis.MoveAbs 轴联动）可直接跑通
                d.Axes.Add(Ax("X", "控制卡1", "脉冲", 0, "mm", 200, 100, 100));
                d.Axes.Add(Ax("Y", "控制卡1", "脉冲", 1, "mm", 200, 100, 100));
                d.Comms.Add(Comm("Modbus主站", "ModbusRTU", "COM1", 9600));
                d.Comms.Add(Comm("压力传感器", "ModbusTCP", "192.168.1.30", 502));
                AddVars(d, ("计数", "0"), ("总数", "0"));
                d.Flows.Add(TblFlow("装配", FlowRole.Main,

                    CommentStep("装配" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    CylOut("送料"),
                    WaitIO("送料完成", "1", 5000),
                    CylBack("送料"),
                    CylOut("夹紧"),
                    WaitStep(300),
                    CylOut("顶升"),
                    WaitStep(300),
                    CylOut("打螺丝"),
                    WaitStep(800),
                    CylBack("打螺丝"),
                    CylBack("顶升"),
                    WaitStep(200),
                    CylBack("夹紧"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("循环装配", FlowRole.Main,

                    CommentStep("循环装配" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    LoopStart(3),
                    CylOut("送料"),
                    WaitStep(1000),
                    CylBack("送料"),
                    CylOut("夹紧"),
                    WaitStep(500),
                    CylBack("夹紧"),
                    LoopEnd(),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,

                    CommentStep("复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    CylBack("送料"),
                    CylBack("夹紧"),
                    CylBack("打螺丝"),
                    CylBack("顶升"),
                    SetIO("就绪", "1")));
                return d;
            },
        };

        // =================== IO ===================

        private static ProjectTemplate Io8x8() => new()
        {
            Id = "io-8x8",
            Name = "IO 扩展 8 入 8 出",
            Category = "IO",
            Description = "1 块扩展 IO 模块 + 8 入 8 出 + Modbus 主/从站 + 变量，最小 IO 演示工程。",
            Summary = "1 控制 · 8 入 8 出 · 2 通讯 · 2 变量 · 1 主流程 · 1 复位",
            Highlights = new[]
            {
                "控制器：扩展IO 模块（雷赛）",
                "输入：8 路（启动/停止/复位/急停/手自动/暂停/允许/完成）",
                "输出：8 路（运行/就绪/报警/完成/暂停/允许/送料/装配）",
                "通讯：Modbus 主站 + Modbus 从站",
                "变量：计数 / 总数",
                "主流程：输入条件 → 输出响应",
                "复位：清报警 + 就绪"
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("扩展IO1", "雷赛", "IO扩展", 0, 0, "Modbus", "网口"));
                string[] inNames = { "启动", "停止", "复位", "急停", "手自动", "暂停", "允许", "完成" };
                for (int i = 0; i < 8; i++) d.Inputs.Add(In(inNames[i], "动点", "扩展IO1", 0, 0, i));
                string[] outNames = { "运行", "就绪", "报警", "完成", "暂停", "允许", "送料", "装配" };
                for (int i = 0; i < 8; i++) d.Outputs.Add(Out(outNames[i], "动点", "扩展IO1", 0, 0, i));
                d.Comms.Add(Comm("Modbus主站", "ModbusRTU", "COM1", 9600));
                d.Comms.Add(Comm("ModbusIO从站", "ModbusRTU", "COM2", 9600));
                AddVars(d, ("计数", "0"), ("总数", "0"));
                d.Flows.Add(TblFlow("主流程", FlowRole.Main,

                    CommentStep("主流程" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    SetIO("送料", "1"),
                    WaitIO("完成", "1", 5000),
                    SetIO("送料", "0"),
                    SetIO("装配", "1"),
                    WaitStep(1000),
                    SetIO("装配", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,

                    CommentStep("复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    SetIO("送料", "0"),
                    SetIO("装配", "0"),
                    SetIO("就绪", "1")));
                return d;
            },
        };

        private static ProjectTemplate Io16x16() => new()
        {
            Id = "io-16x16",
            Name = "IO 扩展 16 入 16 出",
            Category = "IO",
            Description = "2 块扩展 IO 模块 + 16 入 16 出 + Modbus 多通道通讯 + 变量，适合多工位 IO 联动。",
            Summary = "2 控制 · 16 入 16 出 · 2 通讯 · 3 变量 · 2 主流程 · 1 复位",
            Highlights = new[]
            {
                "控制器1：扩展IO1（8 入 8 出）",
                "控制器2：扩展IO2（8 入 8 出）",
                "输入：16 路（启动/停止/复位/急停/手自动/暂停 + 8 工位感应）",
                "输出：16 路（运行/就绪/报警/完成/暂停 + 4 工位控制 + 4 指示灯）",
                "通讯：Modbus 主站 + Modbus TCP 网桥",
                "变量：计数 / 总数 / 当前工位",
                "主流程1：单工位动作",
                "主流程2：多工位并行",
                "复位：所有输出清零 + 就绪"
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("扩展IO1", "雷赛", "IO扩展", 0, 0, "Modbus", "网口"));
                d.Controllers.Add(Ctl("扩展IO2", "雷赛", "IO扩展", 1, 0, "Modbus", "网口"));
                string[] inNamesA = { "启动", "停止", "复位", "急停", "手自动", "暂停", "工位1完成", "工位2完成" };
                string[] inNamesB = { "工位3完成", "工位4完成", "工位5完成", "工位6完成", "工位7完成", "工位8完成", "允许", "急停2" };
                for (int i = 0; i < 8; i++) d.Inputs.Add(In(inNamesA[i], "动点", "扩展IO1", 0, 0, i));
                for (int i = 0; i < 8; i++) d.Inputs.Add(In(inNamesB[i], "动点", "扩展IO2", 1, 0, i));
                string[] outNamesA = { "运行", "就绪", "报警", "完成", "暂停", "工位1控制", "工位2控制", "工位3控制" };
                string[] outNamesB = { "工位4控制", "工位5控制", "工位6控制", "工位7控制", "工位8控制", "绿灯", "红灯", "蜂鸣" };
                for (int i = 0; i < 8; i++) d.Outputs.Add(Out(outNamesA[i], "动点", "扩展IO1", 0, 0, i));
                for (int i = 0; i < 8; i++) d.Outputs.Add(Out(outNamesB[i], "动点", "扩展IO2", 1, 0, i));
                d.Comms.Add(Comm("Modbus主站", "ModbusTCP", "192.168.1.10", 502));
                d.Comms.Add(Comm("ModbusTCP桥", "ModbusTCP", "192.168.1.20", 502));
                AddVars(d, ("计数", "0"), ("总数", "0"), ("当前工位", "1"));
                d.Flows.Add(TblFlow("单工位", FlowRole.Main,

                    CommentStep("单工位" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    SetIO("工位1控制", "1"),
                    WaitIO("工位1完成", "1", 5000),
                    SetIO("工位1控制", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("多工位并行", FlowRole.Main,

                    CommentStep("多工位并行" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    SetIO("工位1控制", "1"),
                    SetIO("工位2控制", "1"),
                    SetIO("工位3控制", "1"),
                    SetIO("工位4控制", "1"),
                    WaitStep(3000),
                    SetIO("工位1控制", "0"),
                    SetIO("工位2控制", "0"),
                    SetIO("工位3控制", "0"),
                    SetIO("工位4控制", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,

                    CommentStep("复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    SetIO("工位1控制", "0"),
                    SetIO("工位2控制", "0"),
                    SetIO("工位3控制", "0"),
                    SetIO("工位4控制", "0"),
                    SetIO("工位5控制", "0"),
                    SetIO("工位6控制", "0"),
                    SetIO("工位7控制", "0"),
                    SetIO("工位8控制", "0"),
                    SetIO("蜂鸣", "0"),
                    SetIO("就绪", "1")));
                return d;
            },
        };

        // =================== 综合 ===================

        private static ProjectTemplate PointPick() => new()
        {
            Id = "point-pick",
            Name = "点位抓取（XY+Z）",
            Category = "综合",
            Description = "3 轴 + 1 工位（含 6 个点位）+ 1 气缸抓取 + 1 料盘 + Modbus + 变量。",
            Summary = "1 控制 · 3 轴 · 4 入 4 出 · 1 气缸 · 6 点位 · 1 料盘 · 1 通讯 · 2 变量 · 2 主流程 · 2 复位",
            Highlights = new[]
            {
                "控制器：雷赛 DMC5400",
                "轴：X/Y/Z 三轴",
                "气缸：抓取气缸（Y0/X0/X1）",
                "工位1：6 个点位（取料/放料/安全/中转/备用1/备用2）",
                "料盘：原料盘 6×5 (20mm 间距)",
                "通讯：Modbus 主站",
                "变量：计数 / 总数",
                "主流程1：取料动作（点1→点2）",
                "主流程2：放料动作（点2→点3）",
                "复位1：完整回零",
                "复位2：气缸缩回 + Z 抬升"
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("控制卡1", "雷赛", "DMC5400", 0, 4, "脉冲", "PCI"));
                d.Axes.Add(Ax("X", "控制卡1", "脉冲", 0, "mm", 300, 150, 150));
                d.Axes.Add(Ax("Y", "控制卡1", "脉冲", 1, "mm", 300, 150, 150));
                d.Axes.Add(Ax("Z", "控制卡1", "脉冲", 2, "mm", 100, 80, 80));
                d.Inputs.Add(In("启动", "启动按钮", "控制卡1", 0, 0, 0));
                d.Inputs.Add(In("停止", "停止按钮", "控制卡1", 0, 0, 1));
                d.Inputs.Add(In("复位", "复位按钮", "控制卡1", 0, 0, 2));
                d.Inputs.Add(In("急停", "安全门", "控制卡1", 0, 0, 3));
                d.Outputs.Add(Out("运行", "动点", "控制卡1", 0, 0, 0));
                d.Outputs.Add(Out("就绪", "动点", "控制卡1", 0, 0, 1));
                d.Outputs.Add(Out("报警", "动点", "控制卡1", 0, 0, 2));
                d.Outputs.Add(Out("完成", "动点", "控制卡1", 0, 0, 3));
                d.Cylinders.Add(Cyl("抓取", "Y0", "X0", "X1"));
                var t = new PointTable { Name = "工位1" };
                t.AxisNames[0] = "X"; t.AxisNames[1] = "Y"; t.AxisNames[2] = "Z"; t.AxisNames[3] = "";
                t.Points.Add(MakePoint("取料位", 100, 50, 0));
                t.Points.Add(MakePoint("放料位", 200, 150, 0));
                t.Points.Add(MakePoint("安全位", 50, 50, 50));
                t.Points.Add(MakePoint("中转位", 150, 100, 30));
                t.Points.Add(MakePoint("备用1", 0, 0, 0));
                t.Points.Add(MakePoint("备用2", 0, 0, 0));
                d.PointTables.Add(t);
                d.Trays.Add(Tray("原料盘", 6, 5, 0, 0, 20, 20));
                d.Comms.Add(Comm("Modbus主站", "ModbusRTU", "COM1", 9600));
                AddVars(d, ("计数", "0"), ("总数", "0"));
                d.Flows.Add(TblFlow("取料", FlowRole.Main,

                    CommentStep("取料" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 100, 800),
                    MoveAxis("Y", 50, 800),
                    MoveAxis("Z", -30, 400),
                    CylOut("抓取"),
                    WaitStep(300),
                    MoveAxis("Z", 0, 400),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("放料", FlowRole.Main,

                    CommentStep("放料" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 200, 800),
                    MoveAxis("Y", 150, 800),
                    MoveAxis("Z", -30, 400),
                    CylBack("抓取"),
                    WaitStep(300),
                    MoveAxis("Z", 0, 400),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("完整复位", FlowRole.Reset,

                    CommentStep("完整复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    CylBack("抓取"),
                    MoveAxis("Z", 0, 600),
                    HomeAxis("X"),
                    HomeAxis("Y"),
                    HomeAxis("Z"),
                    SetIO("就绪", "1")));
                d.Flows.Add(TblFlow("快速归位", FlowRole.Reset,

                    CommentStep("快速归位" + " 流程示例"),
                    CylBack("抓取"),
                    MoveAxis("Z", 0, 600),
                    SetIO("就绪", "1")));
                return d;
            },
        };

        private static ProjectTemplate DualStation() => new()
        {
            Id = "dual-station",
            Name = "双工位分拣",
            Category = "综合",
            Description = "2 个工位（分拣 + 包装），每工位 6 个点位 + 2 气缸 + 2 料盘 + 2 通讯 + 变量。",
            Summary = "1 控制 · 4 轴 · 12 入 12 出 · 2 气缸 · 12 点位 · 2 料盘 · 2 通讯 · 3 变量 · 3 主流程 · 2 复位",
            Highlights = new[]
            {
                "控制器：雷赛 DMC5800",
                "轴：X/Y 共享 + Z1/Z2 两工位独立升降",
                "气缸：分拣气缸 + 包装气缸",
                "工位1：6 个点位（入料/分拣A/分拣B/不良/检测/等待）",
                "工位2：6 个点位（上料/定位/包装/封口/出料/等待）",
                "料盘1：分拣料盘 6×4 (25mm 间距)",
                "料盘2：包装料盘 8×3 (30mm 间距)",
                "通讯：Modbus 主站 + 串口扫描枪",
                "变量：计数 / 总数 / 当前工位",
                "3 个主流程：分拣 / 包装 / 联动循环",
                "2 个复位：完整复位 / 快速归位"
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("控制卡1", "雷赛", "DMC5800", 0, 6, "脉冲", "PCI"));
                d.Axes.Add(Ax("X", "控制卡1", "脉冲", 0, "mm", 400, 200, 200));
                d.Axes.Add(Ax("Y", "控制卡1", "脉冲", 1, "mm", 400, 200, 200));
                d.Axes.Add(Ax("Z1", "控制卡1", "脉冲", 2, "mm", 100, 80, 80));
                d.Axes.Add(Ax("Z2", "控制卡1", "脉冲", 3, "mm", 100, 80, 80));
                string[] inNames = { "启动", "停止", "复位", "急停", "手自动", "暂停", "来料", "检测完成",
                                     "包装完成", "出料允许", "封口完成", "安全门" };
                for (int i = 0; i < 12; i++) d.Inputs.Add(In(inNames[i], "动点", "控制卡1", 0, 0, i));
                string[] outNames = { "运行", "就绪", "报警", "完成", "暂停", "分拣A", "分拣B", "不良",
                                      "上料", "包装", "封口", "出料" };
                for (int i = 0; i < 12; i++) d.Outputs.Add(Out(outNames[i], "动点", "控制卡1", 0, 0, i));
                d.Cylinders.Add(Cyl("分拣", "Y0", "X0", "X1"));
                d.Cylinders.Add(Cyl("包装", "Y1", "X2", "X3"));
                var t1 = new PointTable { Name = "分拣工位" };
                t1.AxisNames[0] = "X"; t1.AxisNames[1] = "Y"; t1.AxisNames[2] = "Z1"; t1.AxisNames[3] = "";
                t1.Points.Add(MakePoint("入料", 0, 0, 0));
                t1.Points.Add(MakePoint("分拣A", 100, 50, -30));
                t1.Points.Add(MakePoint("分拣B", 100, 150, -30));
                t1.Points.Add(MakePoint("不良", 200, 200, -30));
                t1.Points.Add(MakePoint("检测", 50, 100, 0));
                t1.Points.Add(MakePoint("等待", 0, 0, 50));
                d.PointTables.Add(t1);
                var t2 = new PointTable { Name = "包装工位" };
                t2.AxisNames[0] = "X"; t2.AxisNames[1] = "Y"; t2.AxisNames[2] = "Z2"; t2.AxisNames[3] = "";
                t2.Points.Add(MakePoint("上料", 300, 0, 0));
                t2.Points.Add(MakePoint("定位", 300, 100, -20));
                t2.Points.Add(MakePoint("包装", 300, 200, -20));
                t2.Points.Add(MakePoint("封口", 350, 200, -20));
                t2.Points.Add(MakePoint("出料", 400, 200, 0));
                t2.Points.Add(MakePoint("等待", 0, 0, 50));
                d.PointTables.Add(t2);
                d.Trays.Add(Tray("分拣料盘", 6, 4, 0, 0, 25, 25));
                d.Trays.Add(Tray("包装料盘", 8, 3, 300, 0, 30, 30));
                d.Comms.Add(Comm("Modbus主站", "ModbusRTU", "COM1", 9600));
                d.Comms.Add(Comm("扫码枪", "串口", "COM2", 9600));
                AddVars(d, ("计数", "0"), ("总数", "0"), ("当前工位", "1"));
                d.Flows.Add(TblFlow("分拣", FlowRole.Main,

                    CommentStep("分拣" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 0, 600),
                    MoveAxis("Y", 0, 600),
                    MoveAxis("Z1", 0, 400),
                    WaitIO("来料", "1", 5000),
                    CylOut("分拣"),
                    WaitStep(300),
                    MoveAxis("X", 100, 600),
                    MoveAxis("Y", 50, 600),
                    SetIO("分拣A", "1"),
                    WaitStep(500),
                    CylBack("分拣"),
                    SetIO("分拣A", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("包装", FlowRole.Main,

                    CommentStep("包装" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 300, 800),
                    MoveAxis("Y", 100, 800),
                    MoveAxis("Z2", -20, 400),
                    SetIO("上料", "1"),
                    WaitStep(500),
                    SetIO("上料", "0"),
                    SetIO("包装", "1"),
                    WaitStep(800),
                    SetIO("包装", "0"),
                    SetIO("封口", "1"),
                    WaitStep(500),
                    SetIO("封口", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("联动循环", FlowRole.Main,

                    CommentStep("联动循环" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    LoopStart(3),
                    MoveAxis("X", 100, 600),
                    MoveAxis("Y", 100, 600),
                    WaitStep(200),
                    MoveAxis("X", 300, 600),
                    MoveAxis("Y", 200, 600),
                    LoopEnd(),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("完整复位", FlowRole.Reset,

                    CommentStep("完整复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    CylBack("分拣"),
                    CylBack("包装"),
                    MoveAxis("Z1", 0, 600),
                    MoveAxis("Z2", 0, 600),
                    HomeAxis("X"),
                    HomeAxis("Y"),
                    HomeAxis("Z1"),
                    HomeAxis("Z2"),
                    SetIO("就绪", "1")));
                d.Flows.Add(TblFlow("快速归位", FlowRole.Reset,

                    CommentStep("快速归位" + " 流程示例"),
                    CylBack("分拣"),
                    CylBack("包装"),
                    SetIO("就绪", "1")));
                return d;
            },
        };

        private static ProjectTemplate AssemblyLine() => new()
        {
            Id = "assembly-line",
            Name = "流水线装配",
            Category = "综合",
            Description = "4 轴 + 4 气缸 + 16 IO + 1 工位 10 点 + 2 料盘 + 2 通讯 + 变量，完整流水线装配工程。",
            Summary = "1 控制 · 4 轴 · 16 入 16 出 · 4 气缸 · 10 点位 · 2 料盘 · 2 通讯 · 3 变量 · 4 主流程 · 2 复位",
            Highlights = new[]
            {
                "控制器：雷赛 DMC5800",
                "轴：X 输送 + Y 横移 + Z1/Z2 升降",
                "气缸：送料/夹紧/打螺丝/顶升",
                "工位1：10 个点位（上料/装配1/装配2/装配3/装配4/检测/打螺丝/出料/等待/安全）",
                "料盘1：上料盘 12×6 (15mm 间距)",
                "料盘2：装配盘 8×8 (20mm 间距)",
                "通讯：Modbus 主站 + 串口扫描枪",
                "变量：计数 / 总数 / 当前工序",
                "4 个主流程：送料/装配/打螺丝/检测",
                "2 个复位：完整复位 / 快速归位"
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("控制卡1", "雷赛", "DMC5800", 0, 6, "脉冲", "PCI"));
                d.Axes.Add(Ax("X", "控制卡1", "脉冲", 0, "mm", 500, 250, 250));
                d.Axes.Add(Ax("Y", "控制卡1", "脉冲", 1, "mm", 300, 150, 150));
                d.Axes.Add(Ax("Z1", "控制卡1", "脉冲", 2, "mm", 100, 80, 80));
                d.Axes.Add(Ax("Z2", "控制卡1", "脉冲", 3, "mm", 100, 80, 80));
                string[] inNames = { "启动", "停止", "复位", "急停", "手自动", "暂停",
                                     "来料", "装配1完成", "装配2完成", "装配3完成",
                                     "打螺丝完成", "检测完成", "出料允许", "安全门", "夹紧确认", "顶升确认" };
                for (int i = 0; i < 16; i++) d.Inputs.Add(In(inNames[i], "动点", "控制卡1", 0, 0, i));
                string[] outNames = { "运行", "就绪", "报警", "完成", "暂停",
                                      "送料", "夹紧", "装配1", "装配2", "装配3",
                                      "打螺丝", "顶升", "检测", "出料", "绿灯", "红灯" };
                for (int i = 0; i < 16; i++) d.Outputs.Add(Out(outNames[i], "动点", "控制卡1", 0, 0, i));
                d.Cylinders.Add(Cyl("送料", "Y0", "X0", "X1"));
                d.Cylinders.Add(Cyl("夹紧", "Y1", "X2", "X3"));
                d.Cylinders.Add(Cyl("打螺丝", "Y2", "X4", "X5"));
                d.Cylinders.Add(Cyl("顶升", "Y3", "X6", "X7"));
                var t = new PointTable { Name = "装配工位" };
                t.AxisNames[0] = "X"; t.AxisNames[1] = "Y"; t.AxisNames[2] = "Z1"; t.AxisNames[3] = "Z2";
                t.Points.Add(MakePoint("上料", 0, 0, 0, 0));
                t.Points.Add(MakePoint("装配1", 100, 0, -30, 0));
                t.Points.Add(MakePoint("装配2", 200, 0, 0, -30));
                t.Points.Add(MakePoint("装配3", 300, 0, -30, 0));
                t.Points.Add(MakePoint("装配4", 400, 0, 0, -30));
                t.Points.Add(MakePoint("打螺丝", 500, 0, -30, 0));
                t.Points.Add(MakePoint("检测", 600, 0, 0, 0));
                t.Points.Add(MakePoint("出料", 700, 0, 0, 0));
                t.Points.Add(MakePoint("等待", 0, 0, 50, 50));
                t.Points.Add(MakePoint("安全", 50, 50, 50, 50));
                d.PointTables.Add(t);
                d.Trays.Add(Tray("上料盘", 12, 6, -50, 0, 15, 15));
                d.Trays.Add(Tray("装配盘", 8, 8, 750, 0, 20, 20));
                d.Comms.Add(Comm("Modbus主站", "ModbusRTU", "COM1", 9600));
                d.Comms.Add(Comm("扫码枪", "串口", "COM2", 9600));
                AddVars(d, ("计数", "0"), ("总数", "0"), ("当前工序", "1"));
                d.Flows.Add(TblFlow("送料", FlowRole.Main,

                    CommentStep("送料" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    CylOut("送料"),
                    WaitIO("来料", "1", 5000),
                    CylBack("送料"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("装配", FlowRole.Main,

                    CommentStep("装配" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    CylOut("夹紧"),
                    WaitStep(300),
                    MoveAxis("X", 100, 800),
                    SetIO("装配1", "1"),
                    WaitStep(500),
                    SetIO("装配1", "0"),
                    MoveAxis("X", 200, 800),
                    SetIO("装配2", "1"),
                    WaitStep(500),
                    SetIO("装配2", "0"),
                    CylBack("夹紧"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("打螺丝", FlowRole.Main,

                    CommentStep("打螺丝" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 500, 800),
                    CylOut("打螺丝"),
                    WaitStep(1000),
                    CylBack("打螺丝"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("检测", FlowRole.Main,

                    CommentStep("检测" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 600, 800),
                    SetIO("检测", "1"),
                    WaitStep(800),
                    SetIO("检测", "0"),
                    MoveAxis("X", 700, 800),
                    SetIO("出料", "1"),
                    WaitStep(500),
                    SetIO("出料", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("完整复位", FlowRole.Reset,

                    CommentStep("完整复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    CylBack("送料"),
                    CylBack("夹紧"),
                    CylBack("打螺丝"),
                    CylBack("顶升"),
                    SetIO("装配1", "0"),
                    SetIO("装配2", "0"),
                    SetIO("检测", "0"),
                    SetIO("出料", "0"),
                    HomeAxis("X"),
                    HomeAxis("Y"),
                    HomeAxis("Z1"),
                    HomeAxis("Z2"),
                    SetIO("就绪", "1")));
                d.Flows.Add(TblFlow("快速归位", FlowRole.Reset,

                    CommentStep("快速归位" + " 流程示例"),
                    SetIO("报警", "0"),
                    CylBack("送料"),
                    CylBack("夹紧"),
                    CylBack("打螺丝"),
                    CylBack("顶升"),
                    SetIO("就绪", "1")));
                return d;
            },
        };

        private static ProjectTemplate VisionGuided() => new()
        {
            Id = "vision-guided",
            Name = "视觉引导抓取",
            Category = "综合",
            Description = "4 轴 + 8 IO + 2 气缸 + 2 相机 + 1 料盘 + 1 视觉流程（图像采集/模板匹配/缺陷检测）+ 通讯 + 变量。",
            Summary = "1 控制 · 4 轴 · 8 入 8 出 · 2 气缸 · 1 料盘 · 2 相机 · 2 通讯 · 3 变量 · 1 视觉流 · 3 主流程 · 2 复位",
            Highlights = new[]
            {
                "控制器：雷赛 EtherCAT 主站",
                "轴：X/Y/Z/R 4 轴",
                "气缸：夹爪 + 真空",
                "料盘：原料盘 8×6 (15mm 间距)",
                "相机1：海康工业相机（1920×1080）",
                "相机2：巴斯勒 GigE（2448×2048）",
                "通讯：Modbus 主站 + 串口光源控制器",
                "变量：计数 / 总数 / 匹配分数",
                "视觉流程：图像采集 → 模板匹配 → 缺陷检测 → 输出位姿",
                "主流程1：自动取料（视觉引导）",
                "主流程2：自动放料",
                "主流程3：手动示教",
                "2 个复位：完整复位 / 视觉归位"
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("控制卡1", "雷赛", "EtherCAT主站", 0, 8, "EtherCAT", "网口"));
                d.Axes.Add(Ax("X", "控制卡1", "EtherCAT", 0, "mm", 400, 200, 200));
                d.Axes.Add(Ax("Y", "控制卡1", "EtherCAT", 1, "mm", 400, 200, 200));
                d.Axes.Add(Ax("Z", "控制卡1", "EtherCAT", 2, "mm", 150, 100, 100));
                d.Axes.Add(Ax("R", "控制卡1", "EtherCAT", 3, "°", 360, 180, 180));
                string[] inNames = { "启动", "停止", "复位", "急停", "手自动", "暂停", "示教", "拍照允许" };
                for (int i = 0; i < 8; i++) d.Inputs.Add(In(inNames[i], "动点", "控制卡1", 0, 0, i));
                string[] outNames = { "运行", "就绪", "报警", "完成", "暂停", "夹爪", "真空", "光源" };
                for (int i = 0; i < 8; i++) d.Outputs.Add(Out(outNames[i], "动点", "控制卡1", 0, 0, i));
                d.Cylinders.Add(Cyl("夹爪", "Y0", "X0", "X1"));
                d.Cylinders.Add(Cyl("真空", "Y1", "X2", "X3"));
                d.Trays.Add(Tray("原料盘", 8, 6, 0, 0, 15, 15));
                d.Cameras.Add(Cam("上视相机", "海康威视", "192.168.1.100", 8000, 1920, 1080, 10.0, 1.0, "装配检测"));
                d.Cameras.Add(Cam("下视相机", "巴斯勒", "192.168.1.101", 8000, 2448, 2048, 8.0, 1.5, "位置识别"));
                d.Comms.Add(Comm("Modbus主站", "ModbusTCP", "192.168.1.10", 502));
                d.Comms.Add(Comm("光源控制器", "串口", "COM3", 9600));
                AddVars(d, ("计数", "0"), ("总数", "0"), ("匹配分数", "0"));

                // 点位表：视觉工位（4 轴槽 X/Y/Z/R），供「点位」步骤联动走位
                var vtbl = new PointTable { Name = "视觉工位" };
                vtbl.AxisNames[0] = "X"; vtbl.AxisNames[1] = "Y"; vtbl.AxisNames[2] = "Z"; vtbl.AxisNames[3] = "R";
                vtbl.Points.Add(MakePoint("取料位", 100, 100, -30, 0));
                vtbl.Points.Add(MakePoint("放料位", 200, 200, -30, 180));
                d.PointTables.Add(vtbl);

                d.Flows.Add(TblFlow("自动取料", FlowRole.Main,

                    CommentStep("自动取料" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    SetIO("光源", "1"),
                    SetIO("真空", "1"),
                    WaitIO("拍照允许", "1", 5000),
                    CameraStep("0"),
                    MoveAxis("X", 100, 800),
                    MoveAxis("Y", 100, 800),
                    MoveAxis("Z", -30, 400),
                    PointStep("视觉工位", "取料位"),
                    CylOut("夹爪"),
                    WaitStep(300),
                    MoveAxis("Z", 0, 400),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("自动放料", FlowRole.Main,

                    CommentStep("自动放料" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("R", 180, 600),
                    MoveAxis("X", 200, 800),
                    MoveAxis("Y", 200, 800),
                    MoveAxis("Z", -30, 400),
                    CylBack("夹爪"),
                    SetIO("真空", "0"),
                    WaitStep(300),
                    MoveAxis("Z", 0, 400),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("手动示教", FlowRole.Main,

                    CommentStep("手动示教" + " 流程示例"),
                    WaitIO("示教", "1", 60000),
                    SetIO("暂停", "1"),
                    WaitStep(200),
                    SetIO("完成", "1")));
                var vis = new FlowItem { Name = "视觉引导", Kind = FlowKind.Vision, Role = FlowRole.Main };
                vis.VisualSteps.Add(new VisualFlowStep { Name = "图像采集", StepType = "图像采集", CameraId = "0", ExposureMs = 10, Width = 1920, Height = 1080 });
                vis.VisualSteps.Add(new VisualFlowStep { Name = "模板匹配", StepType = "模板匹配", TemplatePath = "", ScoreThreshold = 0.85, AngleRange = 360, MatchMode = "灰度匹配" });
                vis.VisualSteps.Add(new VisualFlowStep { Name = "缺陷检测", StepType = "缺陷检测", Algorithm = "NCC", MinArea = 100, MaxArea = 100000, Threshold = 128, DetectMode = "阈值面积" });
                vis.VisualSteps.Add(new VisualFlowStep { Name = "输出位姿", StepType = "通讯", Protocol = "Modbus", Target = "控制卡1", Content = "X,Y,R" });
                d.Flows.Add(vis);
                d.Flows.Add(TblFlow("完整复位", FlowRole.Reset,

                    CommentStep("完整复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    CylBack("夹爪"),
                    CylBack("真空"),
                    SetIO("光源", "0"),
                    MoveAxis("Z", 0, 600),
                    HomeAxis("X"),
                    HomeAxis("Y"),
                    HomeAxis("R"),
                    HomeAxis("Z"),
                    SetIO("就绪", "1")));
                d.Flows.Add(TblFlow("视觉归位", FlowRole.Reset,

                    CommentStep("视觉归位" + " 流程示例"),
                    SetIO("光源", "0"),
                    CylBack("夹爪"),
                    CylBack("真空"),
                    SetIO("就绪", "1")));
                return d;
            },
        };

        private static ProjectTemplate MultiProduct() => new()
        {
            Id = "multi-product",
            Name = "多产品切换",
            Category = "综合",
            Description = "4 轴 + 8 IO + 1 工位 8 点（产品 A/B/C 共线）+ 2 料盘 + 2 通讯 + 变量。",
            Summary = "1 控制 · 4 轴 · 8 入 8 出 · 8 点位 · 2 料盘 · 2 通讯 · 3 变量 · 3 主流程 · 1 复位",
            Highlights = new[]
            {
                "控制器：雷赛 DMC5800",
                "轴：X/Y/Z/R 4 轴",
                "工位1：8 个点位（产品A 4 个 + 产品B 2 个 + 产品C 2 个）",
                "料盘1：产品A料盘 4×3 (30mm 间距)",
                "料盘2：产品B料盘 4×3 (30mm 间距)",
                "通讯：Modbus 主站 + 串口扫描枪",
                "变量：当前产品 / 计数 / 总数",
                "主流程1：产品 A 完整工艺",
                "主流程2：产品 B 完整工艺",
                "主流程3：产品 C 完整工艺",
                "复位：所有轴归零"
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("控制卡1", "雷赛", "DMC5800", 0, 6, "脉冲", "PCI"));
                d.Axes.Add(Ax("X", "控制卡1", "脉冲", 0, "mm", 400, 200, 200));
                d.Axes.Add(Ax("Y", "控制卡1", "脉冲", 1, "mm", 400, 200, 200));
                d.Axes.Add(Ax("Z", "控制卡1", "脉冲", 2, "mm", 150, 100, 100));
                d.Axes.Add(Ax("R", "控制卡1", "脉冲", 3, "°", 360, 180, 180));
                string[] inNames = { "产品A", "产品B", "产品C", "急停", "复位", "停止", "手自动", "暂停" };
                for (int i = 0; i < 8; i++) d.Inputs.Add(In(inNames[i], "动点", "控制卡1", 0, 0, i));
                string[] outNames = { "产品A运行", "产品B运行", "产品C运行", "就绪", "报警", "完成", "绿灯", "红灯" };
                for (int i = 0; i < 8; i++) d.Outputs.Add(Out(outNames[i], "动点", "控制卡1", 0, 0, i));
                var t = new PointTable { Name = "共线工位" };
                t.AxisNames[0] = "X"; t.AxisNames[1] = "Y"; t.AxisNames[2] = "Z"; t.AxisNames[3] = "R";
                t.Points.Add(MakePoint("产品A取料", 0, 0, -30, 0));
                t.Points.Add(MakePoint("产品A装配", 100, 100, -30, 0));
                t.Points.Add(MakePoint("产品A检测", 200, 100, 0, 90));
                t.Points.Add(MakePoint("产品A出料", 300, 100, 0, 0));
                t.Points.Add(MakePoint("产品B取料", 0, 200, -20, 0));
                t.Points.Add(MakePoint("产品B出料", 300, 200, 0, 0));
                t.Points.Add(MakePoint("产品C取料", 0, 300, -40, 0));
                t.Points.Add(MakePoint("产品C出料", 300, 300, 0, 0));
                d.PointTables.Add(t);
                d.Trays.Add(Tray("产品A料盘", 4, 3, -50, 0, 30, 30));
                d.Trays.Add(Tray("产品B料盘", 4, 3, -50, 100, 30, 30));
                d.Comms.Add(Comm("Modbus主站", "ModbusRTU", "COM1", 9600));
                d.Comms.Add(Comm("扫码枪", "串口", "COM2", 9600));
                AddVars(d, ("当前产品", "A"), ("计数", "0"), ("总数", "0"));
                d.Flows.Add(TblFlow("产品A", FlowRole.Main,

                    CommentStep("产品A" + " 流程示例"),
                    WaitIO("产品A", "1", 3000),
                    SetIO("产品A运行", "1"),
                    MoveAxis("X", 0, 600),
                    MoveAxis("Y", 0, 600),
                    MoveAxis("Z", -30, 400),
                    WaitStep(300),
                    MoveAxis("X", 100, 600),
                    MoveAxis("Y", 100, 600),
                    MoveAxis("Z", -30, 400),
                    WaitStep(500),
                    MoveAxis("Z", 0, 400),
                    MoveAxis("R", 90, 400),
                    MoveAxis("X", 200, 600),
                    WaitStep(500),
                    MoveAxis("X", 300, 600),
                    SetIO("产品A运行", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("产品B", FlowRole.Main,

                    CommentStep("产品B" + " 流程示例"),
                    WaitIO("产品B", "1", 3000),
                    SetIO("产品B运行", "1"),
                    MoveAxis("X", 0, 600),
                    MoveAxis("Y", 200, 600),
                    MoveAxis("Z", -20, 400),
                    WaitStep(300),
                    MoveAxis("X", 300, 600),
                    MoveAxis("Y", 200, 600),
                    SetIO("产品B运行", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("产品C", FlowRole.Main,

                    CommentStep("产品C" + " 流程示例"),
                    WaitIO("产品C", "1", 3000),
                    SetIO("产品C运行", "1"),
                    MoveAxis("X", 0, 600),
                    MoveAxis("Y", 300, 600),
                    MoveAxis("Z", -40, 400),
                    WaitStep(300),
                    MoveAxis("X", 300, 600),
                    MoveAxis("Y", 300, 600),
                    SetIO("产品C运行", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,

                    CommentStep("复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    SetIO("产品A运行", "0"),
                    SetIO("产品B运行", "0"),
                    SetIO("产品C运行", "0"),
                    HomeAxis("X"),
                    HomeAxis("Y"),
                    HomeAxis("Z"),
                    HomeAxis("R"),
                    SetIO("就绪", "1")));
                return d;
            },
        };

        private static ProjectTemplate FullFeatured() => new()
        {
            Id = "full-featured",
            Name = "全功能完整工程",
            Category = "综合",
            Description = "最大演示工程：2 控制卡 + 6 轴 + 16 IO + 4 气缸 + 2 工位 24 点 + 2 料盘 + 2 相机 + 3 通讯 + 变量 + 视觉流程 + Lua 脚本流程。",
            Summary = "2 控制 · 6 轴 · 16 入 16 出 · 4 气缸 · 24 点位 · 2 料盘 · 2 相机 · 3 通讯 · 5 变量 · 5 主流程 · 1 视觉流 · 1 脚本流 · 3 复位",
            Highlights = new[]
            {
                "控制器1：EtherCAT 主站（轴 0-3）",
                "控制器2：EtherCAT 扩展（轴 4-5）",
                "轴：J1/J2/J3 + X/Y/Z 共 6 轴",
                "气缸：送料 / 夹紧 / 打螺丝 / 顶升",
                "工位1：12 个点位（X/Y/Z 3 轴）",
                "工位2：12 个点位（X/Y/Z 3 轴）",
                "料盘1：上料盘 10×6 (15mm 间距)",
                "料盘2：装配盘 8×8 (20mm 间距)",
                "相机1：海康工业相机（装配检测）",
                "相机2：巴斯勒 GigE（位置识别）",
                "通讯：Modbus 主站 + Modbus 扩展IO + 串口扫描枪",
                "变量：计数 / 总数 / 当前产品 / 工位选择 / 循环数",
                "视觉流程：相机2 拍照 → 模板匹配 → 缺陷检测 → 输出位姿到变量",
                "脚本流程：Lua 脚本（轴联动 + IO 读写 + 延时）",
                "5 个主流程 + 1 视觉流程 + 1 脚本流程 + 3 个复位流程",
                "含变量、IO、气缸、轴、点位、流程、料盘、相机、通讯、视觉、脚本全套"
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("控制卡1", "雷赛", "EtherCAT主站", 0, 8, "EtherCAT", "网口"));
                d.Controllers.Add(Ctl("控制卡2", "雷赛", "EtherCAT扩展", 1, 8, "EtherCAT", "网口"));
                d.Axes.Add(Ax("J1", "控制卡1", "EtherCAT", 0, "°", 180, 90, 90));
                d.Axes.Add(Ax("J2", "控制卡1", "EtherCAT", 1, "°", 180, 90, 90));
                d.Axes.Add(Ax("J3", "控制卡1", "EtherCAT", 2, "°", 180, 90, 90));
                d.Axes.Add(Ax("X", "控制卡2", "EtherCAT", 0, "mm", 400, 200, 200));
                d.Axes.Add(Ax("Y", "控制卡2", "EtherCAT", 1, "mm", 400, 200, 200));
                d.Axes.Add(Ax("Z", "控制卡2", "EtherCAT", 2, "mm", 200, 100, 100));
                string[] inNames = { "启动", "停止", "复位", "急停", "手自动", "暂停", "示教", "安全门",
                                     "来料", "装配1完成", "装配2完成", "打螺丝完成", "检测完成", "出料", "拍照允许", "允许启动" };
                for (int i = 0; i < 16; i++) d.Inputs.Add(In(inNames[i], "动点", i < 8 ? "控制卡1" : "控制卡2", i < 8 ? 0 : 1, 0, i % 8));
                string[] outNames = { "运行", "就绪", "报警", "完成", "暂停", "送料", "夹紧", "打螺丝",
                                      "顶升", "检测", "出料", "真空", "光源", "绿灯", "红灯", "蜂鸣" };
                for (int i = 0; i < 16; i++) d.Outputs.Add(Out(outNames[i], "动点", i < 8 ? "控制卡1" : "控制卡2", i < 8 ? 0 : 1, 0, i % 8));
                d.Cylinders.Add(Cyl("送料", "Y0", "X0", "X1"));
                d.Cylinders.Add(Cyl("夹紧", "Y1", "X2", "X3"));
                d.Cylinders.Add(Cyl("打螺丝", "Y2", "X4", "X5"));
                d.Cylinders.Add(Cyl("顶升", "Y3", "X6", "X7"));
                var t1 = new PointTable { Name = "工位1" };
                t1.AxisNames[0] = "X"; t1.AxisNames[1] = "Y"; t1.AxisNames[2] = "Z"; t1.AxisNames[3] = "";
                t1.Points.Add(MakePoint("上料", 0, 0, 0));
                t1.Points.Add(MakePoint("装配1", 50, 0, -30));
                t1.Points.Add(MakePoint("装配2", 100, 0, -30));
                t1.Points.Add(MakePoint("装配3", 150, 0, -30));
                t1.Points.Add(MakePoint("装配4", 200, 0, -30));
                t1.Points.Add(MakePoint("打螺丝", 250, 0, -30));
                t1.Points.Add(MakePoint("检测", 300, 0, 0));
                t1.Points.Add(MakePoint("出料", 350, 0, 0));
                t1.Points.Add(MakePoint("等待", 0, 0, 50));
                t1.Points.Add(MakePoint("安全", 50, 50, 50));
                t1.Points.Add(MakePoint("备用1", 0, 0, 0));
                t1.Points.Add(MakePoint("备用2", 0, 0, 0));
                d.PointTables.Add(t1);
                var t2 = new PointTable { Name = "工位2" };
                t2.AxisNames[0] = "X"; t2.AxisNames[1] = "Y"; t2.AxisNames[2] = "Z"; t2.AxisNames[3] = "";
                t2.Points.Add(MakePoint("入料", 400, 0, 0));
                t2.Points.Add(MakePoint("定位", 400, 50, -20));
                t2.Points.Add(MakePoint("包装", 400, 100, -20));
                t2.Points.Add(MakePoint("封口", 450, 100, -20));
                t2.Points.Add(MakePoint("出料", 500, 100, 0));
                t2.Points.Add(MakePoint("打码", 450, 150, 0));
                t2.Points.Add(MakePoint("贴标", 450, 200, 0));
                t2.Points.Add(MakePoint("检测", 400, 250, 0));
                t2.Points.Add(MakePoint("等待", 400, 0, 50));
                t2.Points.Add(MakePoint("安全", 450, 50, 50));
                t2.Points.Add(MakePoint("备用1", 0, 0, 0));
                t2.Points.Add(MakePoint("备用2", 0, 0, 0));
                d.PointTables.Add(t2);
                d.Trays.Add(Tray("上料盘", 10, 6, -50, 0, 15, 15));
                d.Trays.Add(Tray("装配盘", 8, 8, 750, 0, 20, 20));
                d.Cameras.Add(Cam("上视相机", "海康威视", "192.168.1.100", 8000, 1920, 1080, 10.0, 1.0, "装配检测"));
                d.Cameras.Add(Cam("下视相机", "巴斯勒", "192.168.1.101", 8000, 2448, 2048, 8.0, 1.5, "位置识别"));
                d.Comms.Add(Comm("Modbus主站", "ModbusTCP", "192.168.1.10", 502));
                d.Comms.Add(Comm("ModbusIO扩展", "ModbusTCP", "192.168.1.20", 502));
                d.Comms.Add(Comm("扫码枪", "串口", "COM2", 9600));
                AddVars(d, ("计数", "0"), ("总数", "0"), ("当前产品", "A"), ("工位选择", "1"), ("循环数", "0"));
                d.Flows.Add(TblFlow("自动取料", FlowRole.Main,

                    CommentStep("自动取料" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    SetIO("真空", "1"),
                    MoveAxis("J1", 0, 600),
                    MoveAxis("J2", 0, 600),
                    MoveAxis("J3", -45, 600),
                    MoveAxis("X", 0, 600),
                    MoveAxis("Y", 0, 600),
                    MoveAxis("Z", -30, 400),
                    WaitStep(500),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("装配循环", FlowRole.Main,

                    CommentStep("装配循环" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    LoopStart(3),
                    CylOut("夹紧"),
                    WaitStep(300),
                    CylOut("打螺丝"),
                    WaitStep(1000),
                    CylBack("打螺丝"),
                    CylBack("夹紧"),
                    LoopEnd(),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("工位1出料", FlowRole.Main,

                    CommentStep("工位1出料" + " 流程示例"),
                    WaitIO("检测完成", "1", 5000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 350, 800),
                    MoveAxis("Y", 0, 800),
                    SetIO("出料", "1"),
                    WaitStep(500),
                    SetIO("出料", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("工位2包装", FlowRole.Main,

                    CommentStep("工位2包装" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 400, 800),
                    MoveAxis("Y", 100, 800),
                    MoveAxis("Z", -20, 400),
                    WaitStep(500),
                    MoveAxis("Z", 0, 400),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("全流程", FlowRole.Main,

                    CommentStep("全流程" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    CylOut("送料"),
                    WaitIO("来料", "1", 5000),
                    CylBack("送料"),
                    CylOut("顶升"),
                    WaitStep(300),
                    CylOut("夹紧"),
                    WaitStep(500),
                    CylOut("打螺丝"),
                    WaitStep(1000),
                    CylBack("打螺丝"),
                    CylBack("夹紧"),
                    CylBack("顶升"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("完整复位", FlowRole.Reset,

                    CommentStep("完整复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    CylBack("送料"),
                    CylBack("夹紧"),
                    CylBack("打螺丝"),
                    CylBack("顶升"),
                    SetIO("真空", "0"),
                    SetIO("出料", "0"),
                    SetIO("光源", "0"),
                    MoveAxis("Z", 0, 800),
                    HomeAxis("J1"),
                    HomeAxis("J2"),
                    HomeAxis("J3"),
                    HomeAxis("X"),
                    HomeAxis("Y"),
                    HomeAxis("Z"),
                    SetIO("就绪", "1")));
                d.Flows.Add(TblFlow("快速归位", FlowRole.Reset,

                    CommentStep("快速归位" + " 流程示例"),
                    SetIO("报警", "0"),
                    CylBack("送料"),
                    CylBack("夹紧"),
                    CylBack("打螺丝"),
                    CylBack("顶升"),
                    SetIO("真空", "0"),
                    SetIO("就绪", "1")));
                d.Flows.Add(TblFlow("气缸复位", FlowRole.Reset,

                    CommentStep("气缸复位" + " 流程示例"),
                    CylBack("送料"),
                    CylBack("夹紧"),
                    CylBack("打螺丝"),
                    CylBack("顶升"),
                    SetIO("就绪", "1")));

                // 视觉流程：用相机2 拍照 → 模板匹配定位 → 缺陷检测 → 把结果写到变量
                var vis = new FlowItem { Name = "视觉检测", Kind = FlowKind.Vision, Role = FlowRole.Main };
                vis.VisualSteps.Add(new VisualFlowStep
                {
                    Name = "图像采集",
                    StepType = "图像采集",
                    CameraId = "下视相机",
                    ExposureMs = 8,
                    Width = 2448,
                    Height = 2048,
                    SavePath = "Images/inspection/"
                });
                vis.VisualSteps.Add(new VisualFlowStep
                {
                    Name = "模板匹配",
                    StepType = "模板匹配",
                    CameraId = "下视相机",
                    TemplatePath = "Templates/part_a.bmp",
                    ScoreThreshold = 0.85,
                    AngleRange = 360,
                    MatchMode = "灰度匹配",
                    TemplateRoiX = 1100, TemplateRoiY = 900, TemplateRoiW = 200, TemplateRoiH = 200
                });
                vis.VisualSteps.Add(new VisualFlowStep
                {
                    Name = "缺陷检测",
                    StepType = "缺陷检测",
                    Algorithm = "NCC",
                    MinArea = 100,
                    MaxArea = 100000,
                    Threshold = 128,
                    DetectMode = "阈值面积"
                });
                vis.VisualSteps.Add(new VisualFlowStep
                {
                    Name = "输出位姿",
                    StepType = "通讯",
                    Protocol = "变量",
                    Target = "匹配分数",
                    Content = "{匹配分数},{X},{Y},{R}"
                });
                d.Flows.Add(vis);

                // Lua 脚本流程：演示 Lua 语法（变量赋值 / 轴移动 / IO 读写 / 延时 / 打印）
                var lua = new FlowItem
                {
                    Name = "脚本流程",
                    Kind = FlowKind.Lua,
                    Role = FlowRole.Main,
                    LuaSource = @"-- 脚本流程示例：Lua 控制流程
-- 通过 LuaSource 编写逻辑，运行时会逐行执行
local cycle = Variable.Get('循环数') or 0
cycle = cycle + 1
Variable.Set('循环数', tostring(cycle))

-- 等待启动信号
while IO.Get('启动') ~= '1' do
    WaitStep(50)
    if EStop() then return end
end

IO.Set('运行', '1')

-- 演示轴联动：X/Y 走一个矩形
Axis.MoveAbs('X', 0, 800)
Axis.MoveAbs('Y', 0, 800)
WaitStep(200)
Axis.MoveAbs('X', 100, 800)
Axis.MoveAbs('Y', 0, 800)
WaitStep(200)
Axis.MoveAbs('X', 100, 800)
Axis.MoveAbs('Y', 100, 800)
WaitStep(200)
Axis.MoveAbs('X', 0, 800)
Axis.MoveAbs('Y', 100, 800)
WaitStep(200)

-- 气缸动作
Cylinder.Out('夹紧')
WaitStep(300)
Cylinder.Back('夹紧')

IO.Set('完成', '1')
Print(string.format('脚本流程 第 %d 次循环完成', cycle))
"
                };
                d.Flows.Add(lua);

                return d;
            },
        };

        // =================== 仿真演示（专为 3D 仿真设计） ===================
        // 打开后「运行」即可看到：四轴联动走位 + 点位表运动 + 气缸伸缩 + 相机取帧预览。
        // 轴/气缸/相机数量会被 3D 仿真页自动识别并建模；流程里的 相机/点位 步骤
        // 直接驱动仿真预览与相机画面。
        private static ProjectTemplate SimDemo() => new()
        {
            Id = "sim-demo",
            Name = "仿真演示",
            Category = "综合",
            Description = "4 轴 + 8 IO + 2 气缸 + 2 相机 + 1 点位表，专为 3D 仿真设计的示例：运行即见轴/气缸/相机动起来。",
            Summary = "1 控制 · 4 轴 · 8 入 8 出 · 2 气缸 · 2 相机 · 1 点位表 · 2 通讯 · 3 变量 · 2 主流程 · 1 复位",
            Highlights = new[]
            {
                "控制器：雷赛 EtherCAT 主站",
                "轴：X/Y/Z 直线 + R 旋转（单位 mm / °）",
                "气缸：夹爪 + 真空，仿真中可见活塞伸缩",
                "相机：上视 / 下视各一台，运行到「相机」步骤即刷新仿真取帧预览",
                "点位表：取放工位（安全点 / 取料点 / 放料点），运行到「点位」步骤即联动走位",
                "主流程1：安全点 → 取料点 → 夹取+拍照 → 放料点 → 释放",
                "主流程2：视觉定位（连续拍照 + 走位循环）",
                "复位：气缸缩回 + 各轴回零",
                "适合直接点「运行」观察 3D 仿真动画"
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("控制卡1", "雷赛", "EtherCAT主站", 0, 4, "EtherCAT", "网口"));
                d.Axes.Add(Ax("X", "控制卡1", "EtherCAT", 0, "mm", 400, 200, 200));
                d.Axes.Add(Ax("Y", "控制卡1", "EtherCAT", 1, "mm", 400, 200, 200));
                d.Axes.Add(Ax("Z", "控制卡1", "EtherCAT", 2, "mm", 150, 100, 100));
                d.Axes.Add(Ax("R", "控制卡1", "EtherCAT", 3, "°", 360, 180, 180));
                string[] inNames = { "启动", "停止", "复位", "急停", "手自动", "暂停", "示教", "拍照允许" };
                for (int i = 0; i < 8; i++) d.Inputs.Add(In(inNames[i], "动点", "控制卡1", 0, 0, i));
                string[] outNames = { "运行", "就绪", "报警", "完成", "夹爪", "真空", "光源", "下料" };
                for (int i = 0; i < 8; i++) d.Outputs.Add(Out(outNames[i], "动点", "控制卡1", 0, 0, i));
                d.Cylinders.Add(Cyl("夹爪", "Y0", "X0", "X1"));
                d.Cylinders.Add(Cyl("真空", "Y1", "X2", "X3"));
                d.Cameras.Add(Cam("上视相机", "海康威视", "192.168.1.100", 8000, 1920, 1080, 10.0, 1.0, "装配检测"));
                d.Cameras.Add(Cam("下视相机", "巴斯勒", "192.168.1.101", 8000, 2448, 2048, 8.0, 1.5, "位置识别"));
                d.Comms.Add(Comm("Modbus主站", "ModbusTCP", "192.168.1.10", 502));
                d.Comms.Add(Comm("光源控制器", "串口", "COM3", 9600));
                AddVars(d, ("计数", "0"), ("总数", "0"), ("匹配分数", "0"));

                // 点位表：取放工位（4 轴槽 X/Y/Z/R）
                var tbl = new PointTable { Name = "取放工位" };
                tbl.AxisNames[0] = "X";
                tbl.AxisNames[1] = "Y";
                tbl.AxisNames[2] = "Z";
                tbl.AxisNames[3] = "R";
                tbl.Points.Add(MakePoint("安全点", 0, 0, 50, 0));
                tbl.Points.Add(MakePoint("取料点", 120, 80, -30, 0));
                tbl.Points.Add(MakePoint("放料点", 260, 200, -30, 180));
                d.PointTables.Add(tbl);

                // 主流程1：完整取放（点位表走位 + 气缸 + 相机取帧）
                d.Flows.Add(TblFlow("取放演示", FlowRole.Main,

                    CommentStep("取放演示" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    SetIO("光源", "1"),
                    // 先回安全点（显式轴移动，仿真可见）
                    MoveAxis("X", 0, 800),
                    MoveAxis("Y", 0, 800),
                    MoveAxis("Z", 50, 600),
                    MoveAxis("R", 0, 600),
                    // 点位表走位：仿真按 4 轴槽目标位置联动
                    PointStep("取放工位", "取料点"),
                    CylOut("夹爪"),
                    WaitStep(300),
                    // 相机取帧：仿真相机预览刷新（返回合成帧）
                    CameraStep("0"),
                    SetIO("真空", "1"),
                    WaitStep(300),
                    PointStep("取放工位", "放料点"),
                    CylBack("夹爪"),
                    SetIO("真空", "0"),
                    WaitStep(300),
                    MoveAxis("Z", 0, 600),
                    SetIO("完成", "1")));

                // 主流程2：视觉定位（连续拍照 + 走位循环，便于观察相机预览刷新）
                d.Flows.Add(TblFlow("视觉定位", FlowRole.Main,

                    CommentStep("视觉定位" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    SetIO("光源", "1"),
                    MoveAxis("X", 60, 600),
                    MoveAxis("Y", 60, 600),
                    CameraStep("0"),
                    MoveAxis("X", 200, 600),
                    MoveAxis("Y", 160, 600),
                    CameraStep("1"),
                    MoveAxis("X", 320, 600),
                    MoveAxis("Y", 280, 600),
                    CameraStep("0"),
                    SetIO("完成", "1")));

                // 复位：气缸缩回 + 各轴回零
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,

                    CommentStep("复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    CylBack("夹爪"),
                    CylBack("真空"),
                    SetIO("光源", "0"),
                    MoveAxis("Z", 0, 600),
                    HomeAxis("X"),
                    HomeAxis("Y"),
                    HomeAxis("Z"),
                    HomeAxis("R"),
                    SetIO("就绪", "1")));

                // —— 节点图流程 1：视觉定位（相机取帧 + 气缸夹持，3D 仿真直接驱动）——
                var ng = new NgDoc();
                NgNode N(NgKind k, double x, double y, params (string name, string value)[] props)
                {
                    var def = NgNodeDefinitions.All[k];
                    var n = new NgNode { Kind = k, X = x, Y = y };
                    foreach (var pd in def.Props) n.Props.Add(new NgProp { Name = pd.Name, Value = pd.Default, Options = pd.Options });
                    foreach (var (pn, pv) in props) { var p = n.Props.FirstOrDefault(z => z.Name == pn); if (p != null) p.Value = pv; }
                    ng.Nodes.Add(n); return n;
                }
                void L(NgNode s, NgNode t) => ng.Connections.Add(new NgConnection { SourceId = s.Id, SourcePort = "Out", TargetId = t.Id });

                var nStart = N(NgKind.Start, 60, 80);
                var nX = N(NgKind.MoveAxis, 320, 40, ("轴", "X"), ("目标位置", "120"), ("速度", "100"));
                var nY = N(NgKind.MoveAxis, 320, 180, ("轴", "Y"), ("目标位置", "80"), ("速度", "100"));
                var nCam = N(NgKind.CamCapture, 600, 110, ("相机", "上视相机"));
                var nCylO = N(NgKind.Cylinder, 600, 250, ("气缸", "夹爪"), ("动作", "伸出"));
                var nZ = N(NgKind.MoveAxis, 880, 180, ("轴", "Z"), ("目标位置", "-30"), ("速度", "100"));
                var nCylI = N(NgKind.Cylinder, 880, 320, ("气缸", "夹爪"), ("动作", "缩回"));
                var nD = N(NgKind.Delay, 1120, 250, ("时间ms", "300"));
                var nEnd = N(NgKind.End, 1360, 180);
                L(nStart, nX); L(nX, nY); L(nY, nCam); L(nCam, nCylO); L(nCylO, nZ); L(nZ, nCylI); L(nCylI, nD); L(nD, nEnd);

                d.Flows.Add(new FlowItem
                {
                    Name = "视觉定位(节点图)",
                    Kind = FlowKind.NodeGraph,
                    Role = FlowRole.Main,
                    GraphJson = ng.ToJson()
                });

                // —— 节点图流程 2：阵列循环（条件分支 + 循环 + 变量运算，验证图执行器）——
                var ng2 = new NgDoc();
                NgNode N2(NgKind k, double x, double y, params (string name, string value)[] props)
                {
                    var def = NgNodeDefinitions.All[k];
                    var n = new NgNode { Kind = k, X = x, Y = y };
                    foreach (var pd in def.Props) n.Props.Add(new NgProp { Name = pd.Name, Value = pd.Default, Options = pd.Options });
                    foreach (var (pn, pv) in props) { var p = n.Props.FirstOrDefault(z => z.Name == pn); if (p != null) p.Value = pv; }
                    ng2.Nodes.Add(n); return n;
                }
                void L2(NgNode s, NgNode t, string port = "Out") => ng2.Connections.Add(new NgConnection { SourceId = s.Id, SourcePort = port, TargetId = t.Id });

                var s2 = N2(NgKind.Start, 60, 80);
                var init = N2(NgKind.VarSet, 320, 20, ("变量", "计数"), ("值", "0"));
                var x0 = N2(NgKind.MoveAxis, 320, 160, ("轴", "X"), ("目标位置", "0"), ("速度", "100"));
                var y0 = N2(NgKind.MoveAxis, 320, 300, ("轴", "Y"), ("目标位置", "0"), ("速度", "100"));
                var dec = N2(NgKind.Decision, 600, 230,
                    ("分支数", "2"),
                    ("条件1类型", "轴位置"), ("条件1名称", "X"), ("条件1比较", "大于等于"), ("条件1值", "0"),
                    ("条件2类型", "轴位置"), ("条件2名称", "X"), ("条件2比较", "小于"), ("条件2值", "0"));
                var loop = N2(NgKind.Loop, 860, 230, ("次数", "3"));
                var inc = N2(NgKind.Compute, 1120, 160, ("变量", "计数"), ("表达式", "计数 + 1"));
                var co = N2(NgKind.Cylinder, 1120, 300, ("气缸", "夹爪"), ("动作", "伸出"));
                var d2 = N2(NgKind.Delay, 1380, 300, ("时间ms", "200"));
                var ci = N2(NgKind.Cylinder, 1380, 440, ("气缸", "夹爪"), ("动作", "缩回"));
                var xr = N2(NgKind.MoveAxis, 1640, 300, ("轴", "X"), ("模式", "相对"), ("目标位置", "50"), ("速度", "100"));
                var yr = N2(NgKind.MoveAxis, 1640, 440, ("轴", "Y"), ("模式", "相对"), ("目标位置", "30"), ("速度", "100"));
                var end2 = N2(NgKind.End, 1900, 230);
                L2(s2, init); L2(init, x0); L2(x0, y0); L2(y0, dec);
                L2(dec, loop, "条件1"); L2(dec, end2, "条件2");
                L2(loop, inc, "Body"); L2(loop, end2, "Exit");
                L2(inc, co); L2(co, d2); L2(d2, ci); L2(ci, xr); L2(xr, yr); L2(yr, loop);

                d.Flows.Add(new FlowItem
                {
                    Name = "阵列循环(节点图)",
                    Kind = FlowKind.NodeGraph,
                    Role = FlowRole.Main,
                    GraphJson = ng2.ToJson()
                });
                return d;
            },
        };

        // =================== 综合：视觉分拣线 ===================
        private static ProjectTemplate VisionSort() => new()
        {
            Id = "vision-sort",
            Name = "视觉分拣线",
            Category = "综合",
            Description = "4 轴 + 双相机 + 双气缸，按视觉判定把工件分到良品/不良品料道；含表格主流程与节点图循环流程。",
            Summary = "1 控制 · 4 轴 · 8 入 8 出 · 2 气缸 · 2 相机 · 1 点位表 · 3 变量 · 3 流程",
            Highlights = new[]
            {
                "控制器：雷赛 DMC5400 (脉冲)",
                "轴：X/Y/Z 脉冲轴 mm，R 旋转轴 °",
                "相机：上视定位 / 下视检测",
                "气缸：夹爪(真空吸) / 分拣挡杆",
                "变量：计数 / 良品数 / 不良品数",
                "主流程：上料→视觉→判定→分拣→计数→循环",
                "节点图：视觉分拣循环（分支+循环+变量）",
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("控制卡1", "雷赛", "DMC5400", 0, 4, "脉冲", "PCI"));
                d.Axes.Add(Ax("X", "控制卡1", "脉冲", 0, "mm", 100, 200, 200));
                d.Axes.Add(Ax("Y", "控制卡1", "脉冲", 1, "mm", 100, 200, 200));
                d.Axes.Add(Ax("Z", "控制卡1", "脉冲", 2, "mm", 80, 200, 200));
                d.Axes.Add(Ax("R", "控制卡1", "脉冲", 3, "°", 60, 200, 200));
                d.Inputs.Add(In("启动", "启动")); d.Inputs.Add(In("停止", "停止"));
                d.Inputs.Add(In("复位", "复位")); d.Inputs.Add(In("急停", "急停"));
                d.Inputs.Add(In("来料", "来料检测")); d.Inputs.Add(In("良品", "良品检测"));
                d.Inputs.Add(In("不良", "不良品检测")); d.Inputs.Add(In("料满", "料满"));
                d.Outputs.Add(Out("运行", "运行")); d.Outputs.Add(Out("就绪", "就绪"));
                d.Outputs.Add(Out("报警", "报警")); d.Outputs.Add(Out("完成", "完成"));
                d.Outputs.Add(Out("上料阀", "上料")); d.Outputs.Add(Out("分拣气缸", "分拣"));
                d.Outputs.Add(Out("光源", "光源")); d.Outputs.Add(Out("蜂鸣器", "报警音"));
                d.Cylinders.Add(Cyl("夹爪", "OUT5", "IN5", "IN6"));
                d.Cylinders.Add(Cyl("分拣挡杆", "OUT6", "IN7", "IN8"));
                d.Cameras.Add(Cam("上视相机")); d.Cameras.Add(Cam("下视相机"));
                var tbl = new PointTable { Name = "分拣工位" };
                tbl.AxisNames[0] = "X"; tbl.AxisNames[1] = "Y"; tbl.AxisNames[2] = "Z"; tbl.AxisNames[3] = "R";
                tbl.Points.Add(MakePoint("上料点", 0, 0, 20, 0));
                tbl.Points.Add(MakePoint("良品点", 100, 0, 20, 0));
                tbl.Points.Add(MakePoint("不良品点", 200, 0, 20, 0));
                d.PointTables.Add(tbl);
                AddVars(d, ("计数", "0"), ("良品数", "0"), ("不良品数", "0"));

                // 主流程（表格）：上料 → 视觉 → 判定（良品/不良）→ 计数 → 循环
                d.Flows.Add(TblFlow("主流程", FlowRole.Main,

                    CommentStep("主流程" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    SetIO("光源", "1"),
                    CylOut("夹爪"),
                    PointStep("分拣工位", "上料点"),
                    CameraStep("0"),
                    WaitStep(200),
                    PointStep("分拣工位", "良品点"),
                    PointStep("分拣工位", "不良品点"),
                    CylBack("夹爪"),
                    SetIO("完成", "1"),
                    WaitStep(200)));

                // 复位
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,

                    CommentStep("复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    CylBack("夹爪"),
                    CylBack("分拣挡杆"),
                    SetIO("光源", "0"),
                    MoveAxis("Z", 0, 600),
                    HomeAxis("X"), HomeAxis("Y"), HomeAxis("Z"), HomeAxis("R"),
                    SetIO("就绪", "1")));

                // 节点图：视觉分拣循环（分支 + 循环 + 变量运算）
                var ng = new NgDoc();
                NgNode N(NgKind k, double x, double y, params (string name, string value)[] props)
                {
                    var def = NgNodeDefinitions.All[k];
                    var n = new NgNode { Kind = k, X = x, Y = y };
                    foreach (var pd in def.Props) n.Props.Add(new NgProp { Name = pd.Name, Value = pd.Default, Options = pd.Options });
                    foreach (var (pn, pv) in props) { var p = n.Props.FirstOrDefault(z => z.Name == pn); if (p != null) p.Value = pv; }
                    ng.Nodes.Add(n); return n;
                }
                void L(NgNode s, NgNode t, string p = "Out") => ng.Connections.Add(new NgConnection { SourceId = s.Id, SourcePort = p, TargetId = t.Id });

                var a = N(NgKind.Start, 60, 80);
                var v0 = N(NgKind.VarSet, 320, 20, ("变量", "计数"), ("值", "0"));
                var sx = N(NgKind.MoveAxis, 320, 160, ("轴", "X"), ("目标位置", "0"), ("速度", "100"));
                var sy = N(NgKind.MoveAxis, 320, 300, ("轴", "Y"), ("目标位置", "0"), ("速度", "100"));
                var cam = N(NgKind.CamCapture, 600, 230, ("相机", "上视相机"));
                var dec = N(NgKind.Decision, 860, 230,
                    ("分支数", "2"),
                    ("条件1类型", "轴位置"), ("条件1名称", "X"), ("条件1比较", "大于等于"), ("条件1值", "0"),
                    ("条件2类型", "轴位置"), ("条件2名称", "X"), ("条件2比较", "小于"), ("条件2值", "0"));
                var loop = N(NgKind.Loop, 1120, 230, ("次数", "4"));
                var inc = N(NgKind.Compute, 1380, 160, ("变量", "计数"), ("表达式", "计数 + 1"));
                var cyl = N(NgKind.Cylinder, 1380, 300, ("气缸", "夹爪"), ("动作", "伸出"));
                var dl = N(NgKind.Delay, 1640, 300, ("时间ms", "200"));
                var cylb = N(NgKind.Cylinder, 1640, 440, ("气缸", "夹爪"), ("动作", "缩回"));
                var xr = N(NgKind.MoveAxis, 1900, 300, ("轴", "X"), ("模式", "相对"), ("目标位置", "60"), ("速度", "100"));
                var yr = N(NgKind.MoveAxis, 1900, 440, ("轴", "Y"), ("模式", "相对"), ("目标位置", "40"), ("速度", "100"));
                var e = N(NgKind.End, 2160, 230);
                L(a, v0); L(v0, sx); L(sx, sy); L(sy, cam); L(cam, dec);
                L(dec, loop, "条件1"); L(dec, e, "条件2");
                L(loop, inc, "Body"); L(loop, e, "Exit");
                L(inc, cyl); L(cyl, dl); L(dl, cylb); L(cylb, xr); L(xr, yr); L(yr, loop);
                d.Flows.Add(new FlowItem { Name = "视觉分拣(节点图)", Kind = FlowKind.NodeGraph, Role = FlowRole.Main, GraphJson = ng.ToJson() });
                return d;
            },
        };

        // =================== 综合：点胶阵列 ===================
        private static ProjectTemplate Dispensing() => new()
        {
            Id = "dispensing",
            Name = "点胶阵列",
            Category = "综合",
            Description = "3 轴 + 点胶阀 + 定位相机，按阵列逐点出胶；含表格主流程与节点图阵列循环流程。",
            Summary = "1 控制 · 3 轴 · 4 入 4 出 · 1 气缸 · 1 相机 · 2 变量 · 3 流程",
            Highlights = new[]
            {
                "控制器：雷赛 DMC5400 (脉冲)",
                "轴：X/Y 脉冲轴 mm，Z 升降轴 mm",
                "相机：定位相机",
                "气缸：点胶阀（伸出=出胶，缩回=断胶）",
                "变量：计数 / 胶量",
                "主流程：回零→阵列点胶→收尾",
                "节点图：阵列点胶循环（循环+气缸+变量）",
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("控制卡1", "雷赛", "DMC5400", 0, 4, "脉冲", "PCI"));
                d.Axes.Add(Ax("X", "控制卡1", "脉冲", 0, "mm", 100, 200, 200));
                d.Axes.Add(Ax("Y", "控制卡1", "脉冲", 1, "mm", 100, 200, 200));
                d.Axes.Add(Ax("Z", "控制卡1", "脉冲", 2, "mm", 60, 200, 200));
                d.Inputs.Add(In("启动", "启动")); d.Inputs.Add(In("停止", "停止"));
                d.Inputs.Add(In("复位", "复位")); d.Inputs.Add(In("急停", "急停"));
                d.Outputs.Add(Out("运行", "运行")); d.Outputs.Add(Out("就绪", "就绪"));
                d.Outputs.Add(Out("报警", "报警")); d.Outputs.Add(Out("完成", "完成"));
                d.Cylinders.Add(Cyl("点胶阀", "OUT1", "IN1", "IN2"));
                d.Cameras.Add(Cam("定位相机"));
                AddVars(d, ("计数", "0"), ("胶量", "5"));

                // 主流程（表格）
                d.Flows.Add(TblFlow("主流程", FlowRole.Main,

                    CommentStep("主流程" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("Z", 10, 400),
                    CameraStep("0"),
                    CylOut("点胶阀"),
                    WaitStep(300),
                    CylBack("点胶阀"),
                    MoveAxis("X", 100, 600),
                    MoveAxis("Z", 0, 400),
                    SetIO("完成", "1")));

                // 复位
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,

                    CommentStep("复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    CylBack("点胶阀"),
                    MoveAxis("Z", 0, 600),
                    HomeAxis("X"), HomeAxis("Y"), HomeAxis("Z"),
                    SetIO("就绪", "1")));

                // 节点图：阵列点胶循环（循环 + 气缸出胶 + 变量计数）
                var ng = new NgDoc();
                NgNode N(NgKind k, double x, double y, params (string name, string value)[] props)
                {
                    var def = NgNodeDefinitions.All[k];
                    var n = new NgNode { Kind = k, X = x, Y = y };
                    foreach (var pd in def.Props) n.Props.Add(new NgProp { Name = pd.Name, Value = pd.Default, Options = pd.Options });
                    foreach (var (pn, pv) in props) { var p = n.Props.FirstOrDefault(z => z.Name == pn); if (p != null) p.Value = pv; }
                    ng.Nodes.Add(n); return n;
                }
                void L(NgNode s, NgNode t, string p = "Out") => ng.Connections.Add(new NgConnection { SourceId = s.Id, SourcePort = p, TargetId = t.Id });

                var a = N(NgKind.Start, 60, 80);
                var v0 = N(NgKind.VarSet, 320, 20, ("变量", "计数"), ("值", "0"));
                var sx = N(NgKind.MoveAxis, 320, 160, ("轴", "X"), ("目标位置", "0"), ("速度", "120"));
                var sy = N(NgKind.MoveAxis, 320, 300, ("轴", "Y"), ("目标位置", "0"), ("速度", "120"));
                var loop = N(NgKind.Loop, 600, 230, ("次数", "6"));
                var inc = N(NgKind.Compute, 860, 160, ("变量", "计数"), ("表达式", "计数 + 1"));
                var cylo = N(NgKind.Cylinder, 860, 300, ("气缸", "点胶阀"), ("动作", "伸出"));
                var dl = N(NgKind.Delay, 1120, 300, ("时间ms", "150"));
                var cylb = N(NgKind.Cylinder, 1120, 440, ("气缸", "点胶阀"), ("动作", "缩回"));
                var xr = N(NgKind.MoveAxis, 1380, 300, ("轴", "X"), ("模式", "相对"), ("目标位置", "30"), ("速度", "120"));
                var e = N(NgKind.End, 1640, 230);
                L(a, v0); L(v0, sx); L(sx, sy); L(sy, loop);
                L(loop, inc, "Body"); L(loop, e, "Exit");
                L(inc, cylo); L(cylo, dl); L(dl, cylb); L(cylb, xr); L(xr, loop);
                d.Flows.Add(new FlowItem { Name = "阵列点胶(节点图)", Kind = FlowKind.NodeGraph, Role = FlowRole.Main, GraphJson = ng.ToJson() });
                return d;
            },
        };

        // =================== 半导体：共用设备清单 ===================
        // 固晶机 / 探针台 / 平移式分选机 共享的硬件配置：
        //   3 自动盘 + 3 手动盘 + 上料盘 + 下空盘 + 2 高温中转盘（共 10 盘）
        //   2×4 上料吸嘴 + 2×4 下料吸嘴（共 16 真空吸嘴，伸出=吸/缩回=放）
        //   搬运料盘手臂 3 轴 + 2 个 Z 轴压力测
        //   GPIB 测试机通讯（获取 Bin）
        private static void AddSemiEquipment(ProjectData d)
        {
            d.Controllers.Add(Ctl("运动控制卡", "雷赛", "DMC5800", 0, 12, "脉冲", "PCI"));
            d.Controllers.Add(Ctl("IO扩展卡", "雷赛", "IO扩展", 1, 0, "Modbus", "网口"));
            d.Controllers.Add(Ctl("视觉控制卡", "雷赛", "EtherCAT主站", 2, 4, "EtherCAT", "网口"));

            // 搬运料盘手臂 3 轴
            d.Axes.Add(Ax("搬运X", "运动控制卡", "脉冲", 0, "mm", 600, 300, 300));
            d.Axes.Add(Ax("搬运Y", "运动控制卡", "脉冲", 1, "mm", 400, 200, 200));
            d.Axes.Add(Ax("搬运Z", "运动控制卡", "脉冲", 2, "mm", 200, 150, 150));
            // 2 个 Z 轴压力测
            d.Axes.Add(Ax("Z压力测1", "运动控制卡", "脉冲", 3, "mm", 100, 80, 80));
            d.Axes.Add(Ax("Z压力测2", "运动控制卡", "脉冲", 4, "mm", 100, 80, 80));

            // 盘 / 料盘（共 10 个）
            d.Trays.Add(Tray("自动盘1", 25, 25, 0, 0, 4, 4));
            d.Trays.Add(Tray("自动盘2", 25, 25, 0, 200, 4, 4));
            d.Trays.Add(Tray("自动盘3", 25, 25, 0, 400, 4, 4));
            d.Trays.Add(Tray("手动盘1", 12, 12, 200, 0, 8, 8));
            d.Trays.Add(Tray("手动盘2", 12, 12, 200, 200, 8, 8));
            d.Trays.Add(Tray("手动盘3", 12, 12, 200, 400, 8, 8));
            d.Trays.Add(Tray("上料盘", 20, 20, 400, 0, 6, 6));
            d.Trays.Add(Tray("下空盘", 20, 20, 400, 200, 6, 6));
            d.Trays.Add(Tray("高温中转盘1", 10, 10, 600, 0, 10, 10));
            d.Trays.Add(Tray("高温中转盘2", 10, 10, 600, 200, 10, 10));

            // 16 真空吸嘴：2×4 上料吸嘴 + 2×4 下料吸嘴
            for (int i = 0; i < 8; i++)
                d.Cylinders.Add(Cyl($"上料吸嘴{i + 1}", $"Y{i}", "真空检测", "真空检测", "真空吸嘴"));
            for (int i = 0; i < 8; i++)
                d.Cylinders.Add(Cyl($"下料吸嘴{i + 1}", $"Y{8 + i}", "真空检测", "真空检测", "真空吸嘴"));

            // 输入 IO（16）
            string[] inNames = { "启动", "停止", "复位", "急停", "手自动", "暂停",
                "上料盘到位", "下空盘到位", "高温盘1到位", "高温盘2到位",
                "搬运原点", "真空检测", "压力报警1", "压力报警2", "安全门", "测试完成" };
            for (int i = 0; i < inNames.Length; i++)
                d.Inputs.Add(In(inNames[i], "动点", "IO扩展卡", 1, 0, i));

            // 输出 IO（12）
            string[] outNames = { "运行", "就绪", "报警", "完成", "光源", "真空总阀",
                "蜂鸣器", "加热1", "加热2", "搬运使能", "上料阀", "下料阀" };
            for (int i = 0; i < outNames.Length; i++)
                d.Outputs.Add(Out(outNames[i], "动点", "IO扩展卡", 1, 0, i));

            // GPIB 测试机（获取 Bin）
            d.Comms.Add(Comm("测试机GPIB", "GPIB", "GPIB0::7::INSTR", 0, "无", 8, 1.0, 1000));

            // 变量
            AddVars(d, ("计数", "0"), ("总数", "0"), ("良品数", "0"), ("不良品数", "0"),
                        ("当前Bin", "0"), ("压力1", "0"), ("压力2", "0"), ("当前工序", "1"));
        }

        // =================== 半导体：固晶机 ===================
        private static ProjectTemplate DieBoner() => new()
        {
            Id = "die-bonder",
            Name = "固晶机（Die Bonder）",
            Category = "半导体",
            Description = "顶针顶晶 → 固晶吸嘴取晶 → 视觉对位 → 点胶 → 固晶头 Z 下压邦定到基板。",
            Summary = "3 控制 · 5 轴 · 8 入 8 出 · 3 吸嘴/气缸 · 3 盘 · 1 相机 · 1 Modbus · 5 变量 · 1 主流程 · 1 复位",
            Highlights = new[]
            {
                "控制器：运动控制卡(DMC5400) + IO扩展卡 + 视觉控制卡(EtherCAT)",
                "轴：载台 X/Y/θ（对位平台）+ 固晶头Z + 顶针Z",
                "气缸/吸嘴：固晶吸嘴(真空) + 顶针(双作用) + 点胶阀(双作用)",
                "盘：晶圆环(供晶) / 基板条(邦定) / 收料盘（共 3 盘）",
                "相机：固晶对位相机（上视晶片 + 下视基板，模板匹配）",
                "通讯：点胶控制器 ModbusRTU",
                "变量：计数 / 总数 / 良品数 / 不良品数 / 当前工序",
                "主流程：顶针顶晶 → 吸嘴取晶 → 视觉对位 → 点胶 → 固晶Z压合 → 放晶",
                "复位：顶针/吸嘴/点胶缩回 + 轴归零 + 就绪",
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("运动控制卡", "雷赛", "DMC5400", 0, 5, "脉冲", "PCI"));
                d.Controllers.Add(Ctl("IO扩展卡", "雷赛", "IO扩展", 1, 0, "Modbus", "网口"));
                d.Controllers.Add(Ctl("视觉控制卡", "雷赛", "EtherCAT主站", 2, 2, "EtherCAT", "网口"));

                // 固晶机轴：对位平台 X/Y/θ + 固晶头Z + 顶针Z
                d.Axes.Add(Ax("载台X", "运动控制卡", "脉冲", 0, "mm", 200, 100, 100));
                d.Axes.Add(Ax("载台Y", "运动控制卡", "脉冲", 1, "mm", 200, 100, 100));
                d.Axes.Add(Ax("载台θ", "运动控制卡", "脉冲", 2, "°", 360, 180, 180));
                d.Axes.Add(Ax("固晶头Z", "运动控制卡", "脉冲", 3, "mm", 50, 40, 40));
                d.Axes.Add(Ax("顶针Z", "运动控制卡", "脉冲", 4, "mm", 20, 20, 20));

                // 吸嘴 / 气缸
                d.Cylinders.Add(Cyl("固晶吸嘴", "Y0", "真空检测", "真空检测", "真空吸嘴"));
                d.Cylinders.Add(Cyl("顶针", "Y1", "X0", "X1", "双作用"));
                d.Cylinders.Add(Cyl("点胶阀", "Y2", "X2", "X3", "双作用"));

                // 盘（3）
                d.Trays.Add(Tray("晶圆环", 25, 25, 0, 0, 4, 4));
                d.Trays.Add(Tray("基板条", 8, 4, 300, 0, 15, 15));
                d.Trays.Add(Tray("收料盘", 8, 4, 300, 200, 15, 15));

                // IO
                string[] inNames = { "启动", "停止", "复位", "急停", "手自动", "暂停",
                    "真空检测", "顶针原点", "固晶原点", "点胶原点", "安全门", "来料", "基板到位", "晶片检测", "点胶报警", "完成" };
                for (int i = 0; i < inNames.Length; i++) d.Inputs.Add(In(inNames[i], "动点", "IO扩展卡", 1, 0, i));
                string[] outNames = { "运行", "就绪", "报警", "完成", "光源", "真空总阀",
                    "顶针阀", "点胶使能", "蜂鸣器", "加热", "上料阀", "下料阀" };
                for (int i = 0; i < outNames.Length; i++) d.Outputs.Add(Out(outNames[i], "动点", "IO扩展卡", 1, 0, i));

                // 通讯：点胶控制器
                d.Comms.Add(Comm("点胶控制器", "ModbusRTU", "COM4", 9600));

                // 变量
                AddVars(d, ("计数", "0"), ("总数", "0"), ("良品数", "0"), ("不良品数", "0"), ("当前工序", "1"));

                d.Cameras.Add(Cam("固晶对位相机", "海康威视", "192.168.1.110", 8000, 1920, 1080, 10.0, 1.0, "晶片/基板对位"));

                d.Flows.Add(TblFlow("固晶循环", FlowRole.Main,

                    CommentStep("固晶循环" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    SetIO("光源", "1"),
                    SetIO("真空总阀", "1"),
                    CylOut("顶针"),
                    WaitStep(150),
                    CylOut("固晶吸嘴"),
                    WaitStep(200),
                    CameraStep("0"),
                    CylBack("顶针"),
                    CylOut("点胶阀"),
                    WaitStep(120),
                    MoveAxis("固晶头Z", -3, 200),
                    WaitStep(200),
                    MoveAxis("固晶头Z", 0, 200),
                    CylBack("点胶阀"),
                    CylBack("固晶吸嘴"),
                    SetIO("完成", "1"),
                    WaitStep(200)));

                d.Flows.Add(TblFlow("固晶复位", FlowRole.Reset,

                    CommentStep("固晶复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    SetIO("真空总阀", "0"),
                    CylBack("顶针"),
                    CylBack("固晶吸嘴"),
                    CylBack("点胶阀"),
                    SetIO("光源", "0"),
                    MoveAxis("固晶头Z", 0, 600),
                    HomeAxis("载台X"), HomeAxis("载台Y"), HomeAxis("载台θ"),
                    SetIO("就绪", "1")));
                return d;
            },
        };

        // =================== 半导体：探针台 ===================
        private static ProjectTemplate ProbeStation() => new()
        {
            Id = "probe-station",
            Name = "探针台（Probe Station）",
            Category = "半导体",
            Description = "晶圆吸附在真空吸盘，载台 X/Y/θ 精移使 Die 对准探针卡，探针 Z 下压接触 Pad，GPIB 测试机读取 Bin。",
            Summary = "3 控制 · 4 轴 · 8 入 8 出 · 2 吸嘴/气缸 · 1 盘 · 1 相机 · 1 GPIB · 5 变量 · 1 主流程 · 1 复位",
            Highlights = new[]
            {
                "控制器：运动控制卡(精密 EtherCAT) + IO扩展卡 + 视觉控制卡",
                "轴：载台 X/Y/θ（晶圆精密平台）+ 探针Z（接触下压）",
                "气缸/吸嘴：真空吸盘(吸晶) + 探针卡夹紧(双作用)",
                "盘：晶圆盘（吸盘上 25×25 Die，共 1 盘）",
                "相机：显微镜对位相机（Die 与探针卡对位）",
                "通讯：测试机 GPIB（MEAS:BIN? 读取 Bin）",
                "变量：当前Bin / 计数 / 良品数 / 不良品数 / 当前Die",
                "主流程：载台移到Die → 探针Z下压 → GPIB读Bin → 抬起 → 下一颗",
                "复位：探针Z抬起 + 真空关 + 载台归零 + 就绪",
            },
            Factory = () =>
            {
                var d = new ProjectData();
                d.Controllers.Add(Ctl("运动控制卡", "雷赛", "EtherCAT主站", 0, 4, "EtherCAT", "网口"));
                d.Controllers.Add(Ctl("IO扩展卡", "雷赛", "IO扩展", 1, 0, "Modbus", "网口"));
                d.Controllers.Add(Ctl("视觉控制卡", "雷赛", "EtherCAT主站", 2, 2, "EtherCAT", "网口"));

                // 探针台轴：载台 X/Y/θ + 探针Z
                d.Axes.Add(Ax("载台X", "运动控制卡", "EtherCAT", 0, "mm", 300, 100, 100));
                d.Axes.Add(Ax("载台Y", "运动控制卡", "EtherCAT", 1, "mm", 300, 100, 100));
                d.Axes.Add(Ax("载台θ", "运动控制卡", "EtherCAT", 2, "°", 360, 180, 180));
                d.Axes.Add(Ax("探针Z", "运动控制卡", "EtherCAT", 3, "mm", 20, 15, 15));

                // 吸盘 / 探针卡夹紧
                d.Cylinders.Add(Cyl("真空吸盘", "Y0", "真空检测", "真空检测", "真空吸嘴"));
                d.Cylinders.Add(Cyl("探针卡夹紧", "Y1", "X0", "X1", "双作用"));

                // 盘（1）：晶圆盘
                d.Trays.Add(Tray("晶圆盘", 25, 25, 0, 0, 4, 4));

                // IO
                string[] inNames = { "启动", "停止", "复位", "急停", "手自动", "暂停",
                    "真空检测", "载台原点", "探针原点", "安全门", "测试完成", "对位完成",
                    "卡盘到位", "Die到位", "压力报警", "完成" };
                for (int i = 0; i < inNames.Length; i++) d.Inputs.Add(In(inNames[i], "动点", "IO扩展卡", 1, 0, i));
                string[] outNames = { "运行", "就绪", "报警", "完成", "光源", "真空总阀",
                    "探针下压", "卡盘夹紧", "蜂鸣器", "加热", "上料阀", "下料阀" };
                for (int i = 0; i < outNames.Length; i++) d.Outputs.Add(Out(outNames[i], "动点", "IO扩展卡", 1, 0, i));

                // 通讯：测试机 GPIB（获取 Bin）
                d.Comms.Add(Comm("测试机GPIB", "GPIB", "GPIB0::7::INSTR", 0, "无", 8, 1.0, 1000));

                // 变量
                AddVars(d, ("当前Bin", "0"), ("计数", "0"), ("良品数", "0"), ("不良品数", "0"), ("当前Die", "1"));

                d.Cameras.Add(Cam("显微镜对位相机", "巴斯勒", "192.168.1.120", 8000, 2448, 2048, 8.0, 1.5, "Die/探针卡对位"));

                d.Flows.Add(TblFlow("探针测试", FlowRole.Main,

                    CommentStep("探针测试" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    SetIO("真空总阀", "1"),
                    WaitIO("卡盘到位", "1", 5000),
                    CameraStep("0"),
                    MoveAxis("载台X", 100, 400),
                    MoveAxis("载台Y", 100, 400),
                    SetIO("探针下压", "1"),
                    MoveAxis("探针Z", -1, 150),
                    WaitIO("压力报警", "0", 3000),
                    CommSend("测试机GPIB", "MEAS:BIN?"),
                    MoveAxis("探针Z", 0, 150),
                    SetIO("探针下压", "0"),
                    SetIO("完成", "1"),
                    WaitStep(200)));

                d.Flows.Add(TblFlow("探针复位", FlowRole.Reset,

                    CommentStep("探针复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    SetIO("探针下压", "0"),
                    MoveAxis("探针Z", 0, 600),
                    SetIO("真空总阀", "0"),
                    HomeAxis("载台X"), HomeAxis("载台Y"), HomeAxis("载台θ"),
                    SetIO("就绪", "1")));
                return d;
            },
        };

        // =================== 半导体：平移式分选机 ===================
        private static ProjectTemplate TransferHandler() => new()
        {
            Id = "transfer-handler",
            Name = "平移式分选机（Transfer Handler）",
            Category = "半导体",
            Description = "2×4 上料吸嘴取已测件、GPIB 读取 Bin、2×4 下料吸嘴按 Bin 分选到自动盘/手动盘/下空盘/高温中转盘。",
            Summary = "3 控制 · 5 轴 · 16 入 12 出 · 16 吸嘴 · 10 盘 · 1 GPIB · 8 变量 · 1 主流程 · 1 复位",
            Highlights = new[]
            {
                "控制器：运动控制卡(DMC5800) + IO扩展卡 + 视觉控制卡(EtherCAT)",
                "轴：搬运X/Y/Z 手臂 + Z压力测1/Z压力测2（辅助压合/定位）",
                "盘：自动盘×3 / 手动盘×3 / 上料盘 / 下空盘 / 高温中转盘×2（共 10 盘）",
                "吸嘴：2×4 上料吸嘴取料 + 2×4 下料吸嘴分选（共 16 真空吸嘴）",
                "通讯：测试机 GPIB（获取 Bin，决定分选落点）",
                "变量：当前Bin / 良品数 / 不良品数 / 计数 / 总数 等",
                "主流程：上料吸嘴取料 → GPIB读Bin → 下料吸嘴按Bin分选 → 计数",
                "复位：吸嘴缩回 + 轴归零 + 就绪",
            },
            Factory = () =>
            {
                var d = new ProjectData();
                AddSemiEquipment(d);

                d.Flows.Add(TblFlow("分选循环", FlowRole.Main,

                    CommentStep("分选循环" + " 流程示例"),
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    SetIO("真空总阀", "1"),
                    CylOut("上料吸嘴1"),
                    WaitStep(200),
                    CommSend("测试机GPIB", "MEAS:BIN?"),
                    WaitIO("测试完成", "1", 5000),
                    CylBack("上料吸嘴1"),
                    CylOut("下料吸嘴1"),
                    WaitStep(200),
                    MoveAxis("搬运X", 200, 600),
                    CylBack("下料吸嘴1"),
                    MoveAxis("搬运Z", 0, 400),
                    SetIO("完成", "1"),
                    WaitStep(200)));

                d.Flows.Add(TblFlow("分选复位", FlowRole.Reset,

                    CommentStep("分选复位" + " 流程示例"),
                    SetIO("报警", "0"),
                    SetIO("真空总阀", "0"),
                    CylBack("上料吸嘴1"),
                    CylBack("下料吸嘴1"),
                    HomeAxis("搬运X"), HomeAxis("搬运Y"), HomeAxis("搬运Z"),
                    SetIO("就绪", "1")));
                return d;
            },
        };

        // ====================================================================
        // 构建助手
        // ====================================================================

        private static AxisControllerItem Ctl(string name, string vendor, string cardType, int cardNo, int axisCount, string busType, string connection)
            => new()
            {
                Name = name,
                Vendor = vendor,
                CardType = cardType,
                CardNo = cardNo,
                AxisCount = axisCount,
                BusType = busType,
                Connection = connection,
            };

        private static AxisItem Ax(string name, string controller, string axisType, int axisNo, string unit, double speed, double accel, double decel)
            => new()
            {
                Name = name,
                Controller = controller,
                AxisType = axisType,
                AxisNo = axisNo,
                Unit = unit,
                Speed = speed,
                Accel = accel,
                Decel = decel,
                HomeMode = "原点开关+限位",
                Enabled = true,
            };

        private static IoItem In(string name, string function, string controller = "", int cardNo = 0, int moduleNo = 0, int sequence = 0, string level = "取反")
            => new()
            {
                Name = name,
                Function = function,
                Controller = controller,
                CardNo = cardNo,
                ModuleNo = moduleNo,
                Sequence = sequence,
                Level = level,
            };

        private static IoItem Out(string name, string function, string controller = "", int cardNo = 0, int moduleNo = 0, int sequence = 0, string level = "取反")
            => new()
            {
                Name = name,
                Function = function,
                Controller = controller,
                CardNo = cardNo,
                ModuleNo = moduleNo,
                Sequence = sequence,
                Level = level,
            };

        private static CylinderItem Cyl(string deviceId, string outPoint, string sensorExtend, string sensorRetract, string type = "双作用", int delayMs = 200)
            => new()
            {
                Name = deviceId,        // 同时设 Name，否则左侧列表（绑 Name）显示空白
                DeviceId = deviceId,
                Type = type,
                OutPoint = outPoint,
                SensorExtend = sensorExtend,
                SensorRetract = sensorRetract,
                DelayMs = delayMs,
                InitialState = "缩回",
            };

        private static FlowItem TblFlow(string name, FlowRole role, params FlowStep[] steps)
        {
            var f = new FlowItem { Name = name, Kind = FlowKind.Table, Role = role };
            foreach (var s in steps) f.Steps.Add(s);
            return f;
        }

        // 流程步骤构造助手（Name 取自首个参数：模板调用时已传入与所建对象一致的名字，
        // 这样示例流程的「名称」列直接指向真实存在的轴/IO/气缸/通讯，落库后下拉即可选中、运行器能解析）。
        private static FlowStep WaitIO(string ioName, string value, int timeoutMs)
            => new() { Logic = "如果", Function = "输入IO", Property = "输入状态", Operation = "等于", SetValue = value, Timeout = "等待3秒就统计", DurationMs = timeoutMs, Name = ioName };
        private static FlowStep MoveAxis(string axisName, double position, int durationMs)
            => new() { Logic = "就", Function = "轴", Property = "位置", Operation = "绝对移动", SetValue = position.ToString("0.##"), DurationMs = durationMs, Name = axisName };
        private static FlowStep HomeAxis(string axisName)
            => new() { Logic = "就", Function = "轴", Property = "速度", Operation = "修改为", SetValue = "0", Timeout = "空", Name = axisName };
        private static FlowStep CylOut(string cylId)
            => new() { Logic = "就", Function = "气缸", Property = "电磁阀", Operation = "伸出", SetValue = "伸出", Name = cylId };
        private static FlowStep CylBack(string cylId)
            => new() { Logic = "就", Function = "气缸", Property = "电磁阀", Operation = "缩回", SetValue = "缩回", Name = cylId };
        // 等待：真实时间停顿（Logic=等待，引擎按 SetValue 毫秒 Sleep）。替代原 Delay 占位
        // （原 Delay 用 轴+空名+SetValue=0 仅把速度设为 0，并非时间延迟，列里也只显示「就」）。
        private static FlowStep WaitStep(int ms)
            => new() { Logic = "等待", Function = "系统", Property = "延时", Operation = "修改为", SetValue = ms.ToString(), DurationMs = ms };

        // 循环结构（循环开始 / 循环结束 成对出现；引擎按 循环开始 的 SetValue 次数重复执行循环体）。
        private static FlowStep LoopStart(int count = 3)
            => new() { Logic = "循环开始", Function = "系统", Property = "循环", Operation = "修改为", SetValue = count.ToString() };
        private static FlowStep LoopEnd()
            => new() { Logic = "循环结束", Function = "系统" };

        // 注释：仅作流程说明，不执行任何动作。
        private static FlowStep CommentStep(string text)
            => new() { Logic = "注释", Function = "系统", Property = "注释", Operation = "修改为", SetValue = text };

        // 分支结构（如果 / 否则如果 / 否则 / 结束 成对；否则 之前可接 如果 或 否则如果）。
        private static FlowStep ElseStep()
            => new() { Logic = "否则", Function = "系统" };
        private static FlowStep EndIfStep()
            => new() { Logic = "结束", Function = "系统" };

        private static FlowStep SetIO(string ioName, string value)
            => new() { Logic = "就", Function = "输出IO", Property = "输出状态", Operation = "修改为", SetValue = value, Name = ioName };

        // 相机采集步骤：Name 即相机序号（与 ProjectData.Cameras 的下标对应，0 起）。
        // 运行到该步骤时 FlowRunnerService 会调用 VisionEngine.CaptureFrame 取一帧
        // （仿真桩下返回合成帧），并触发 OnCameraCapture 让 3D 仿真相机预览刷新。
        private static FlowStep CameraStep(string camIdx = "0")
            => new() { Logic = "就", Function = "相机", Property = "采集", Operation = "修改为", SetValue = camIdx, Name = camIdx };

        // 点位步骤：Name = 点位表名，SetValue = 该表下的点位名（留空则取第一个点位）。
        // 运行到该步骤时 FlowRunnerService 会按点位表里的 4 轴槽目标位置驱动各轴，
        // 3D 仿真即随之同步运动。
        private static FlowStep PointStep(string tableName, string pointName = "")
            => new() { Logic = "就", Function = "点位", Property = "移动到", Operation = "修改为", SetValue = pointName, Name = tableName };

        // ====================================================================
        // 示例条件：让「新建工程」出来的示例点位自带一份可看的移动条件配置。
        // 只挑「默认仿真态下就一定成立」的期望值，保证示例工程开箱即可正常移动，
        // 同时让「移动条件」卡片和「实际值」列一打开就有内容可看。
        // ====================================================================

        /// <summary>
        /// 给模板产出的示例工程挂上示范用的移动条件（IsUsed = true，
        /// 与用户自己新增的行「默认不勾选」形成对照）：
        ///   ① 气缸 —— 取工程里第一个气缸，要求「缩回」（枚举型 + == ）；
        ///   ② 输出 IO —— 取第一个输出，要求「0」（枚举型 + == ）。
        /// 两条在默认仿真态下都成立（SimRuntime 的气缸/输出初值就是 0），所以示例工程开箱即可正常移动，
        /// 不会因为示范条件把机台拦住。
        /// 刻意只用枚举型：数值型（变量 / 轴位置）的期望值随运行变化，做成示范条件会时灵时不灵。
        /// 工程里没有气缸/输出时对应那条自动跳过——示例条件指向不存在的设备，
        /// 界面上会显示红色「找不到」，反而像 bug。
        /// 只挂在每张点位表的第一个点位上，保持示例清爽。
        /// 由 ProjectTemplate.Build() 在模板实例化后调用。
        /// </summary>
        /// <summary>
        /// 给模板补一条「节点图」示例流程；模板里已经有一条节点图流程时直接跳过。
        ///
        /// 为什么放在这里统一补：节点图是四类流程（运控 / 脚本 / 视觉 / 节点图）里最不容易被
        /// 用户自己拼出来的一类，但只有「仿真演示 / 视觉分拣线 / 点胶机」3 个模板自带；
        /// 其余模板新建出来后在流程页里根本看不到节点图的例子。
        /// 挂在 ProjectTemplate.Build() 这条公共出口上，一次覆盖全部模板，
        /// 以后新增模板也不用记得单独补。
        ///
        /// 节点只引用模板里**真实存在**的对象（第一根轴 / 第二根轴 / 第一个输入 / 第一个输出 /
        /// 第一台相机 / 第一个气缸 / 第一张点位表的第一个点位），缺哪类就少放哪个节点，
        /// 不会造出属性指向不存在设备的图（那种图在节点图编辑器里会显示空属性）。
        /// 空白模板没有任何可引用的对象，自然空转，保持「0 个流程」。
        /// </summary>
        public static void EnsureNodeGraphFlow(ProjectData d)
        {
            if (d == null) return;

            // 已经自带节点图示例的模板不重复加
            if (d.Flows.Any(f => f.Kind == FlowKind.NodeGraph)) return;

            var axis = d.Axes.FirstOrDefault()?.Name;
            var axis2 = d.Axes.Skip(1).FirstOrDefault()?.Name;
            var waitIn = d.Inputs.FirstOrDefault()?.Name;
            var outName = d.Outputs.FirstOrDefault()?.Name;
            var cyl = d.Cylinders.FirstOrDefault()?.Name;
            var cam = d.Cameras.FirstOrDefault()?.Name;
            var tbl = d.PointTables.FirstOrDefault();

            // 空白模板：一根轴 / 一个 IO / 一台相机 / 一个气缸 / 一张有点位的点位表都没有，保持空工程
            // （气缸和点位表也算「可引用的对象」，只有气缸没轴的模板同样应该有节点图示例）
            if (axis == null && axis2 == null && waitIn == null && outName == null
                && cam == null && cyl == null && (tbl == null || tbl.Points.Count == 0)) return;

            var ng = new NgDoc();
            NgNode N(NgKind k, double x, double y, params (string name, string value)[] props)
            {
                var def = NgNodeDefinitions.All[k];
                var n = new NgNode { Kind = k, X = x, Y = y };
                foreach (var pd in def.Props)
                    n.Props.Add(new NgProp { Name = pd.Name, Value = pd.Default, Options = pd.Options });
                foreach (var (pn, pv) in props)
                {
                    var p = n.Props.FirstOrDefault(z => z.Name == pn);
                    if (p != null) p.Value = pv;
                }
                ng.Nodes.Add(n);
                return n;
            }
            void L(NgNode s, NgNode t) =>
                ng.Connections.Add(new NgConnection { SourceId = s.Id, SourcePort = "Out", TargetId = t.Id });

            // 横向排布：每加一个节点往右推一列，纵向按分支错开，打开节点图就是一条清楚的主线
            const double Col = 260;
            double cx = 60;
            var prev = N(NgKind.Start, cx, 80);

            if (axis != null)
            {
                cx += Col;
                var home = N(NgKind.Home, cx, 20, ("轴", axis));
                L(prev, home);
                var move = N(NgKind.MoveAxis, cx, 150, ("轴", axis), ("模式", "绝对"), ("目标位置", "100"), ("速度", "100"));
                L(home, move);
                var wait = N(NgKind.WaitAxis, cx, 280, ("轴", axis));
                L(move, wait);
                prev = wait;
            }

            if (axis2 != null)
            {
                cx += Col;
                var move2 = N(NgKind.MoveAxis, cx, 20, ("轴", axis2), ("模式", "绝对"), ("目标位置", "50"), ("速度", "100"));
                L(prev, move2);
                prev = move2;
            }

            if (tbl != null && tbl.Points.Count > 0)
            {
                cx += Col;
                var pg = N(NgKind.PointGo, cx, 20, ("点位表", tbl.Name), ("点位", tbl.Points[0].Name));
                L(prev, pg);
                prev = pg;
            }

            if (cam != null)
            {
                cx += Col;
                var shot = N(NgKind.CamCapture, cx, 20, ("图像源", "相机"), ("相机", cam), ("曝光ms", "10"));
                L(prev, shot);
                var match = N(NgKind.TemplateMatch, cx, 170, ("匹配模式", "灰度匹配"), ("分数阈值", "0.8"), ("角度范围", "360"));
                L(shot, match);
                prev = match;
            }

            if (waitIn != null)
            {
                cx += Col;
                var w = N(NgKind.WaitInput, cx, 20, ("信号", waitIn), ("状态", "高电平"), ("超时ms", "3000"));
                L(prev, w);
                prev = w;
            }

            if (cyl != null)
            {
                cx += Col;
                var cylOut = N(NgKind.Cylinder, cx, 20, ("气缸", cyl), ("动作", "伸出"));
                L(prev, cylOut);
                var delay = N(NgKind.Delay, cx, 160, ("时间ms", "300"));
                L(cylOut, delay);
                var cylIn = N(NgKind.Cylinder, cx, 300, ("气缸", cyl), ("动作", "缩回"));
                L(delay, cylIn);
                prev = cylIn;
            }

            if (outName != null)
            {
                cx += Col;
                var io = N(NgKind.IoWrite, cx, 20, ("输出", outName), ("值", "1"));
                L(prev, io);
                prev = io;
            }

            cx += Col;
            var end = N(NgKind.End, cx, 20);
            L(prev, end);

            d.Flows.Add(new FlowItem
            {
                Name = "示例(节点图)",
                Kind = FlowKind.NodeGraph,
                Role = FlowRole.Main,
                GraphJson = ng.ToJson()
            });
        }

        public static void SeedSampleConditions(ProjectData d)
        {
            if (d == null) return;

            var conds = new List<PointMoveCondition>();

            var cylinder = d.Cylinders.FirstOrDefault();
            if (cylinder != null)
                conds.Add(new PointMoveCondition
                {
                    IsUsed = true,
                    Kind = ConditionKinds.Cylinder,
                    TargetName = cylinder.Name,
                    Comparison = "==",
                    ExpectedState = "缩回",
                });

            var output = d.Outputs.FirstOrDefault();
            if (output != null)
                conds.Add(new PointMoveCondition
                {
                    IsUsed = true,
                    Kind = ConditionKinds.Io,
                    TargetName = output.Name,
                    Comparison = "==",
                    ExpectedState = "0",
                });

            if (conds.Count == 0) return;

            foreach (var table in d.PointTables)
            {
                var point = table.Points.FirstOrDefault();
                if (point == null) continue;
                // 覆写最前面几行空条件（PointItem 构造时已补齐 6 行空行）。
                // 不能 Append：追加到末尾会变成 7~8 行，且会被 EnsureConditionRows 的裁剪逻辑挡住。
                for (int i = 0; i < conds.Count && i < point.Conditions.Count; i++)
                    point.Conditions[i] = conds[i];
            }
        }

        // 点位构造助手
        private static PointItem MakePoint(string name, double x, double y, double z, double r = 0)
        {
            var p = new PointItem { Name = name };
            p.Positions[0] = new PointAxis { Position = x, Speed = 100 };
            p.Positions[1] = new PointAxis { Position = y, Speed = 100 };
            p.Positions[2] = new PointAxis { Position = z, Speed = 100 };
            p.Positions[3] = new PointAxis { Position = r, Speed = 100 };
            return p;
        }

        // 变量行构造助手
        private static VariableRow MakeVar(params (string name, string value)[] vars)
        {
            var v = new VariableRow();
            if (vars.Length > 0) { v.Name1 = vars[0].name; v.Value1 = vars[0].value; }
            if (vars.Length > 1) { v.Name2 = vars[1].name; v.Value2 = vars[1].value; }
            if (vars.Length > 2) { v.Name3 = vars[2].name; v.Value3 = vars[2].value; }
            if (vars.Length > 3) { v.Name4 = vars[3].name; v.Value4 = vars[3].value; }
            if (vars.Length > 4) { v.Name5 = vars[4].name; v.Value5 = vars[4].value; }
            return v;
        }

        /// <summary>变量批量加入：每行最多 5 个 (名称,值)，超过自动多行。</summary>
        private static void AddVars(ProjectData d, params (string name, string value)[] vars)
        {
            for (int i = 0; i < vars.Length; i += 5)
            {
                var batch = new (string, string)[Math.Min(5, vars.Length - i)];
                for (int j = 0; j < batch.Length; j++) batch[j] = vars[i + j];
                d.Variables.Add(MakeVar(batch));
            }
        }

        // 通讯构造助手
        private static CommItem Comm(string name, string commType, string portOrIp,
            int baudOrPort = 9600, string parity = "无", int dataBits = 8, double stopBits = 1.0, int timeoutMs = 1000)
            => new()
            {
                Name = name,
                CommType = commType,
                PortOrIp = portOrIp,
                BaudOrPort = baudOrPort,
                Parity = parity,
                DataBits = dataBits,
                StopBits = stopBits,
                TimeoutMs = timeoutMs,
            };

        // 料盘构造助手
        private static TrayItem Tray(string name, int rows, int cols,
            double startX = 0, double startY = 0, double pitchX = 20, double pitchY = 20)
            => new()
            {
                Name = name,
                Rows = rows,
                Cols = cols,
                StartX = startX,
                StartY = startY,
                PitchX = pitchX,
                PitchY = pitchY,
            };

        // 相机构造助手
        private static CameraItem Cam(string name, string vendor = "海康威视",
            string ip = "192.168.1.100", int port = 8000,
            int width = 1920, int height = 1080, double exposureMs = 10.0,
            double gain = 1.0, string description = "")
            => new()
            {
                Name = name,
                Vendor = vendor,
                IpAddress = ip,
                Port = port,
                Width = width,
                Height = height,
                ExposureMs = exposureMs,
                Gain = gain,
                Description = description,
            };

        // 流程步骤：Modbus/串口发送
        private static FlowStep CommSend(string commName, string content)
            => new()
            {
                Logic = "就",
                Function = "modbus",
                Property = "发送",
                Operation = "修改为",
                SetValue = content,
                Name = commName,
            };
    }
}
// ◇作者保留所有权利　请勿删除※⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓⁣