// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// =====================================================================
// 新建工程弹窗所用的「项目模板」目录。
// 每个模板是一个 ProjectTemplate，Factory() 每次返回全新的 ProjectData。
//
// 模板总数：17 个（含 1 个空白）。
// 分类：空白 / 轴运动(6) / 气缸(2) / IO(2) / 综合(6)。
// 覆盖：控制器 / 轴 / IO(入+出) / 气缸 / 点位表 / 通讯 / 料盘 / 相机 / 变量 / 流程（主流程多个 + 复位流程）。
// =====================================================================
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NoCodeMotion.Models;

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

            // ---------- 综合 (6) ----------
            PointPick(),
            DualStation(),
            AssemblyLine(),
            VisionGuided(),
            MultiProduct(),
            FullFeatured(),
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
                    WaitIO("启动", "1", 3000),
                    SetIO("就绪", "1"),
                    SetIO("运行", "1"),
                    MoveAxis("X", 100, 1000),
                    Delay(200),
                    MoveAxis("X", 0, 1000),
                    SetIO("运行", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,
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
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 100, 800),
                    MoveAxis("Y", 50, 800),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("主流程2", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 200, 1200),
                    MoveAxis("Y", 150, 1200),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,
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
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("Z", 0, 600),
                    MoveAxis("X", 200, 800),
                    MoveAxis("Y", 100, 800),
                    SetIO("真空", "1"),
                    Delay(300),
                    MoveAxis("Z", 50, 600),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("放料", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    MoveAxis("X", 400, 800),
                    MoveAxis("Y", 300, 800),
                    SetIO("下料", "1"),
                    Delay(300),
                    SetIO("真空", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,
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
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("R", 45, 600),
                    MoveAxis("X", 100, 800),
                    MoveAxis("Y", 200, 800),
                    MoveAxis("Z", -10, 400),
                    SetIO("点胶阀", "1"),
                    Delay(500),
                    SetIO("点胶阀", "0"),
                    MoveAxis("Z", 0, 400),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("贴标", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("R", 90, 600),
                    MoveAxis("X", 200, 800),
                    MoveAxis("Y", 100, 800),
                    MoveAxis("Z", -5, 400),
                    Delay(300),
                    MoveAxis("Z", 0, 400),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("自动循环", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 50, 600),
                    MoveAxis("Y", 50, 600),
                    Delay(200),
                    MoveAxis("X", 250, 600),
                    MoveAxis("Y", 250, 600),
                    Delay(200),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,
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
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("J1", 30, 800),
                    MoveAxis("J2", -20, 800),
                    MoveAxis("Z", -30, 400),
                    SetIO("真空", "1"),
                    Delay(300),
                    MoveAxis("Z", 0, 400),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("自动放料", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("J1", -45, 800),
                    MoveAxis("J2", 45, 800),
                    MoveAxis("R", 180, 600),
                    MoveAxis("Z", -25, 400),
                    SetIO("真空", "0"),
                    SetIO("夹爪", "1"),
                    Delay(300),
                    MoveAxis("Z", 0, 400),
                    SetIO("夹爪", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("示教记录", FlowRole.Main,
                    WaitIO("示教", "1", 60000),
                    SetIO("暂停", "1"),
                    Delay(200),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("自动循环", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("J1", 0, 600),
                    MoveAxis("J2", 0, 600),
                    MoveAxis("R", 0, 400),
                    Delay(500),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("完整复位", FlowRole.Reset,
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
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("J1", 0, 600),
                    MoveAxis("J2", 0, 600),
                    MoveAxis("J3", 0, 600),
                    MoveAxis("J4", 0, 600),
                    MoveAxis("J5", 0, 600),
                    MoveAxis("J6", 0, 600),
                    SetIO("真空", "1"),
                    Delay(500),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("放料", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("J1", 90, 800),
                    MoveAxis("J2", -45, 800),
                    MoveAxis("J3", 30, 800),
                    MoveAxis("J4", 0, 600),
                    MoveAxis("J5", 45, 600),
                    MoveAxis("J6", 90, 600),
                    SetIO("真空", "0"),
                    Delay(300),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("示教", FlowRole.Main,
                    WaitIO("示教", "1", 60000),
                    SetIO("暂停", "1"),
                    Delay(200),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("点焊", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("J1", 45, 600),
                    MoveAxis("J2", 30, 600),
                    MoveAxis("J3", -30, 600),
                    SetIO("焊接", "1"),
                    Delay(800),
                    SetIO("焊接", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("搬运循环", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("J1", 0, 600),
                    MoveAxis("J2", 0, 600),
                    MoveAxis("J3", 0, 600),
                    SetIO("工装1", "1"),
                    Delay(500),
                    SetIO("工装1", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("完整复位", FlowRole.Reset,
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
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    CylOut("推料"),
                    Delay(500),
                    CylOut("挡料"),
                    Delay(500),
                    CylBack("推料"),
                    CylBack("挡料"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,
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
                d.Comms.Add(Comm("Modbus主站", "ModbusRTU", "COM1", 9600));
                d.Comms.Add(Comm("压力传感器", "ModbusTCP", "192.168.1.30", 502));
                AddVars(d, ("计数", "0"), ("总数", "0"));
                d.Flows.Add(TblFlow("装配", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    CylOut("送料"),
                    WaitIO("送料完成", "1", 5000),
                    CylBack("送料"),
                    CylOut("夹紧"),
                    Delay(300),
                    CylOut("顶升"),
                    Delay(300),
                    CylOut("打螺丝"),
                    Delay(800),
                    CylBack("打螺丝"),
                    CylBack("顶升"),
                    Delay(200),
                    CylBack("夹紧"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("循环装配", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    CylOut("送料"),
                    Delay(1000),
                    CylBack("送料"),
                    CylOut("夹紧"),
                    Delay(500),
                    CylBack("夹紧"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,
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
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    SetIO("送料", "1"),
                    WaitIO("完成", "1", 5000),
                    SetIO("送料", "0"),
                    SetIO("装配", "1"),
                    Delay(1000),
                    SetIO("装配", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,
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
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    SetIO("工位1控制", "1"),
                    WaitIO("工位1完成", "1", 5000),
                    SetIO("工位1控制", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("多工位并行", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    SetIO("工位1控制", "1"),
                    SetIO("工位2控制", "1"),
                    SetIO("工位3控制", "1"),
                    SetIO("工位4控制", "1"),
                    Delay(3000),
                    SetIO("工位1控制", "0"),
                    SetIO("工位2控制", "0"),
                    SetIO("工位3控制", "0"),
                    SetIO("工位4控制", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,
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
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 100, 800),
                    MoveAxis("Y", 50, 800),
                    MoveAxis("Z", -30, 400),
                    CylOut("抓取"),
                    Delay(300),
                    MoveAxis("Z", 0, 400),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("放料", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 200, 800),
                    MoveAxis("Y", 150, 800),
                    MoveAxis("Z", -30, 400),
                    CylBack("抓取"),
                    Delay(300),
                    MoveAxis("Z", 0, 400),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("完整复位", FlowRole.Reset,
                    SetIO("报警", "0"),
                    CylBack("抓取"),
                    MoveAxis("Z", 0, 600),
                    HomeAxis("X"),
                    HomeAxis("Y"),
                    HomeAxis("Z"),
                    SetIO("就绪", "1")));
                d.Flows.Add(TblFlow("快速归位", FlowRole.Reset,
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
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 0, 600),
                    MoveAxis("Y", 0, 600),
                    MoveAxis("Z1", 0, 400),
                    WaitIO("来料", "1", 5000),
                    CylOut("分拣"),
                    Delay(300),
                    MoveAxis("X", 100, 600),
                    MoveAxis("Y", 50, 600),
                    SetIO("分拣A", "1"),
                    Delay(500),
                    CylBack("分拣"),
                    SetIO("分拣A", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("包装", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 300, 800),
                    MoveAxis("Y", 100, 800),
                    MoveAxis("Z2", -20, 400),
                    SetIO("上料", "1"),
                    Delay(500),
                    SetIO("上料", "0"),
                    SetIO("包装", "1"),
                    Delay(800),
                    SetIO("包装", "0"),
                    SetIO("封口", "1"),
                    Delay(500),
                    SetIO("封口", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("联动循环", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 100, 600),
                    MoveAxis("Y", 100, 600),
                    Delay(200),
                    MoveAxis("X", 300, 600),
                    MoveAxis("Y", 200, 600),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("完整复位", FlowRole.Reset,
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
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    CylOut("送料"),
                    WaitIO("来料", "1", 5000),
                    CylBack("送料"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("装配", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    CylOut("夹紧"),
                    Delay(300),
                    MoveAxis("X", 100, 800),
                    SetIO("装配1", "1"),
                    Delay(500),
                    SetIO("装配1", "0"),
                    MoveAxis("X", 200, 800),
                    SetIO("装配2", "1"),
                    Delay(500),
                    SetIO("装配2", "0"),
                    CylBack("夹紧"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("打螺丝", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 500, 800),
                    CylOut("打螺丝"),
                    Delay(1000),
                    CylBack("打螺丝"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("检测", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 600, 800),
                    SetIO("检测", "1"),
                    Delay(800),
                    SetIO("检测", "0"),
                    MoveAxis("X", 700, 800),
                    SetIO("出料", "1"),
                    Delay(500),
                    SetIO("出料", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("完整复位", FlowRole.Reset,
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
                d.Flows.Add(TblFlow("自动取料", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    SetIO("光源", "1"),
                    SetIO("真空", "1"),
                    WaitIO("拍照允许", "1", 5000),
                    MoveAxis("X", 100, 800),
                    MoveAxis("Y", 100, 800),
                    MoveAxis("Z", -30, 400),
                    CylOut("夹爪"),
                    Delay(300),
                    MoveAxis("Z", 0, 400),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("自动放料", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("R", 180, 600),
                    MoveAxis("X", 200, 800),
                    MoveAxis("Y", 200, 800),
                    MoveAxis("Z", -30, 400),
                    CylBack("夹爪"),
                    SetIO("真空", "0"),
                    Delay(300),
                    MoveAxis("Z", 0, 400),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("手动示教", FlowRole.Main,
                    WaitIO("示教", "1", 60000),
                    SetIO("暂停", "1"),
                    Delay(200),
                    SetIO("完成", "1")));
                var vis = new FlowItem { Name = "视觉引导", Kind = FlowKind.Vision, Role = FlowRole.Main };
                vis.VisualSteps.Add(new VisualFlowStep { Name = "图像采集", StepType = "图像采集", CameraId = "0", ExposureMs = 10, Width = 1920, Height = 1080 });
                vis.VisualSteps.Add(new VisualFlowStep { Name = "模板匹配", StepType = "模板匹配", TemplatePath = "", ScoreThreshold = 0.85, AngleRange = 360, MatchMode = "灰度匹配" });
                vis.VisualSteps.Add(new VisualFlowStep { Name = "缺陷检测", StepType = "缺陷检测", Algorithm = "NCC", MinArea = 100, MaxArea = 100000, Threshold = 128, DetectMode = "阈值面积" });
                vis.VisualSteps.Add(new VisualFlowStep { Name = "输出位姿", StepType = "通讯", Protocol = "Modbus", Target = "控制卡1", Content = "X,Y,R" });
                d.Flows.Add(vis);
                d.Flows.Add(TblFlow("完整复位", FlowRole.Reset,
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
                    WaitIO("产品A", "1", 3000),
                    SetIO("产品A运行", "1"),
                    MoveAxis("X", 0, 600),
                    MoveAxis("Y", 0, 600),
                    MoveAxis("Z", -30, 400),
                    Delay(300),
                    MoveAxis("X", 100, 600),
                    MoveAxis("Y", 100, 600),
                    MoveAxis("Z", -30, 400),
                    Delay(500),
                    MoveAxis("Z", 0, 400),
                    MoveAxis("R", 90, 400),
                    MoveAxis("X", 200, 600),
                    Delay(500),
                    MoveAxis("X", 300, 600),
                    SetIO("产品A运行", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("产品B", FlowRole.Main,
                    WaitIO("产品B", "1", 3000),
                    SetIO("产品B运行", "1"),
                    MoveAxis("X", 0, 600),
                    MoveAxis("Y", 200, 600),
                    MoveAxis("Z", -20, 400),
                    Delay(300),
                    MoveAxis("X", 300, 600),
                    MoveAxis("Y", 200, 600),
                    SetIO("产品B运行", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("产品C", FlowRole.Main,
                    WaitIO("产品C", "1", 3000),
                    SetIO("产品C运行", "1"),
                    MoveAxis("X", 0, 600),
                    MoveAxis("Y", 300, 600),
                    MoveAxis("Z", -40, 400),
                    Delay(300),
                    MoveAxis("X", 300, 600),
                    MoveAxis("Y", 300, 600),
                    SetIO("产品C运行", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("复位", FlowRole.Reset,
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
            Description = "最大演示工程：2 控制卡 + 6 轴 + 16 IO + 4 气缸 + 2 工位 24 点 + 2 料盘 + 2 相机 + 3 通讯 + 变量。",
            Summary = "2 控制 · 6 轴 · 16 入 16 出 · 4 气缸 · 24 点位 · 2 料盘 · 2 相机 · 3 通讯 · 5 变量 · 5 主流程 · 3 复位",
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
                "5 个主流程 + 3 个复位流程",
                "含变量、IO、气缸、轴、点位、流程、料盘、相机、通讯全套"
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
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    SetIO("真空", "1"),
                    MoveAxis("J1", 0, 600),
                    MoveAxis("J2", 0, 600),
                    MoveAxis("J3", -45, 600),
                    MoveAxis("X", 0, 600),
                    MoveAxis("Y", 0, 600),
                    MoveAxis("Z", -30, 400),
                    Delay(500),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("装配循环", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    CylOut("夹紧"),
                    Delay(300),
                    CylOut("打螺丝"),
                    Delay(1000),
                    CylBack("打螺丝"),
                    CylBack("夹紧"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("工位1出料", FlowRole.Main,
                    WaitIO("检测完成", "1", 5000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 350, 800),
                    MoveAxis("Y", 0, 800),
                    SetIO("出料", "1"),
                    Delay(500),
                    SetIO("出料", "0"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("工位2包装", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    MoveAxis("X", 400, 800),
                    MoveAxis("Y", 100, 800),
                    MoveAxis("Z", -20, 400),
                    Delay(500),
                    MoveAxis("Z", 0, 400),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("全流程", FlowRole.Main,
                    WaitIO("启动", "1", 3000),
                    SetIO("运行", "1"),
                    CylOut("送料"),
                    WaitIO("来料", "1", 5000),
                    CylBack("送料"),
                    CylOut("顶升"),
                    Delay(300),
                    CylOut("夹紧"),
                    Delay(500),
                    CylOut("打螺丝"),
                    Delay(1000),
                    CylBack("打螺丝"),
                    CylBack("夹紧"),
                    CylBack("顶升"),
                    SetIO("完成", "1")));
                d.Flows.Add(TblFlow("完整复位", FlowRole.Reset,
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
                    SetIO("报警", "0"),
                    CylBack("送料"),
                    CylBack("夹紧"),
                    CylBack("打螺丝"),
                    CylBack("顶升"),
                    SetIO("真空", "0"),
                    SetIO("就绪", "1")));
                d.Flows.Add(TblFlow("气缸复位", FlowRole.Reset,
                    CylBack("送料"),
                    CylBack("夹紧"),
                    CylBack("打螺丝"),
                    CylBack("顶升"),
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

        // 流程步骤构造助手
        private static FlowStep WaitIO(string ioName, string value, int timeoutMs)
            => new() { Logic = "如果", Function = "IO", Property = "输入状态", Operation = "是否等于", SetValue = value, Timeout = "等待3秒就统计", DurationMs = timeoutMs };
        private static FlowStep MoveAxis(string axisName, double position, int durationMs)
            => new() { Logic = "就", Function = "轴", Property = "位置", Operation = "修改", SetValue = position.ToString("0.##"), DurationMs = durationMs };
        private static FlowStep HomeAxis(string axisName)
            => new() { Logic = "就", Function = "轴", Property = "速度", Operation = "等于", SetValue = "0", Timeout = "空" };
        private static FlowStep CylOut(string cylId)
            => new() { Logic = "就", Function = "气缸", Property = "伸出", Operation = "修改", SetValue = "伸出" };
        private static FlowStep CylBack(string cylId)
            => new() { Logic = "就", Function = "气缸", Property = "缩回", Operation = "修改", SetValue = "缩回" };
        private static FlowStep Delay(int ms)
            => new() { Logic = "就", Function = "轴", Property = "速度", Operation = "等于", SetValue = "0", DurationMs = ms };
        private static FlowStep SetIO(string ioName, string value)
            => new() { Logic = "就", Function = "IO", Property = "输出状态", Operation = "修改", SetValue = value };

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
                Operation = "修改",
                SetValue = content,
            };
    }
}
// ◇作者保留所有权利　请勿删除※⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓⁣