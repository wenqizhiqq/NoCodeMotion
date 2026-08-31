// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// =====================================================================
// AI 工程交换服务（项目管理页「复制需求 / 粘贴生成」按钮的后端）。
//
// 工作流：
//   1) 用户在右侧详情填「备注」+「需求」
//   2) 点【复制需求】→ 生成中文提示词（含中文 JSON 契约 + 数量要求）进剪贴板
//   3) 粘贴到豆包 / WorkBuddy / 任意 AI，AI 返回工程配置 JSON
//   4) 复制 AI 返回的 JSON，回本页点【粘贴生成】
//   5) 本服务容错解析并写入 ProjectStore.Data
//
// 关键设计一：**中英文键名全兼容**。
//   中文 AI（豆包等）习惯输出中文键名（"轴"/"名称"/"速度"），英文模型输出
//   英文键名（"axes"/"name"/"speed"）。所有读取都走别名匹配，两套都认。
//
// 关键设计二：**动词归一化**（NormFunction/NormProperty/NormOperation）。
//   AI 写的可能是中文动词（"移动"/"置位"/"伸出"）也可能是英文（"IO"/"Axis"/"Output"），
//   统一映射到本软件 FlowStep.Function / Property / Operation 的合法取值。
//
// 解析原则：**逐条容错**。任何一条数据字段缺失/类型不对只跳过该条，
//   不抛异常中断整体导入，保证 AI 输出不完美时仍能拿到可用配置。
// =====================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    public static class AiProjectExchange
    {
        // ==================== 1. 生成提示词（复制按钮用） ====================

        /// <summary>按工程名 / 备注 / 需求文本生成给 AI 的中文提示词（含中文 JSON 契约 + 数量要求 + 三类流程）。</summary>
        public static string BuildPrompt(string projectName, string? remark, string? requirementsText)
        {
            // 把需求文本逐行化展示（保留空行作为段落分隔）
            var reqs = (requirementsText ?? "")
                       .Replace("\r\n", "\n")
                       .Split('\n')
                       .Select(r => r.Trim())
                       .Where(r => r.Length > 0)
                       .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("你是无代码运动控制软件的工程配置助手。请根据我的需求，生成一个**完整、可运行**的工程配置 JSON。");
            sb.AppendLine();
            sb.AppendLine("【工程名称】" + projectName);
            if (!string.IsNullOrWhiteSpace(remark))
                sb.AppendLine("【工程备注】" + remark.Trim());
            sb.AppendLine();

            if (reqs.Count > 0)
            {
                sb.AppendLine("【我的需求】");
                for (int i = 0; i < reqs.Count; i++)
                    sb.AppendLine((i + 1) + ". " + reqs[i]);
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("【我的需求】（尚未填写，请根据工程名称与备注合理推断一套典型配置）");
                sb.AppendLine();
            }

            // ===== 数量要求：这是关键，防止 AI 只生成最少内容 =====
            sb.AppendLine("【数量要求】请生成足够丰富的内容，不要只给最小示例。按下面下限生成：");
            sb.AppendLine("- 控制器：1~2 个");
            sb.AppendLine("- 轴：2~4 个（按需求涉及的自由度给足）");
            sb.AppendLine("- 输入：至少 8 个（启动、停止、复位、急停、手自动、暂停、原点、正限位、负限位、来料、完成 等）");
            sb.AppendLine("- 输出：至少 8 个（运行、就绪、报警、完成、暂停、各工位控制输出、指示灯 等）");
            sb.AppendLine("- 气缸：0~4 个（需求涉及抓取/推料/压紧/分拣等动作时必须给）");
            sb.AppendLine("- 相机：1~2 个（需求涉及视觉检测/视觉引导/拍照定位时必须给）");
            sb.AppendLine("- 流程：至少 3 个，覆盖三种类型，必须齐全：");
            sb.AppendLine("    * 1 个「运控流程」（类型：\"运控\"，即表格步骤 - 轴/IO/气缸/变量/延时 步骤），至少 10 个步骤");
            sb.AppendLine("    * 1 个「脚本流程」（类型：\"脚本\"，用 Lua 写一些工艺逻辑，例如配方切换/统计/报警分支），源码完整可运行");
            sb.AppendLine("    * 1 个「视觉流程」（类型：\"视觉\"，用 Lua 调用相机/图像处理，对应视觉步骤）");
            sb.AppendLine("    * 复位流程（角色：\"复位\"）作为运控流程的补充，至少 6 步");
            sb.AppendLine("- 工位：1~2 个，每个工位至少 4 个点位（取料位、放料位、安全位、等待位 等）");
            sb.AppendLine("- 通讯：0~2 个（Modbus 主站 / 串口扫码枪 等）");
            sb.AppendLine("- 变量：2~5 个（计数、总数、当前工位、当前工序 等）");
            sb.AppendLine();

            sb.AppendLine("【重要要求】");
            sb.AppendLine("1. 所有名称（轴名、IO 名、气缸名、流程名、点位名、变量名）必须用中文，不要用英文。");
            sb.AppendLine("2. 只输出 JSON，不要任何解释、不要 Markdown 代码块标记。");
            sb.AppendLine("3. 用不到的分类填空数组 []，不要删除分类。");
            sb.AppendLine("4. 流程步骤里引用的轴名、IO 名、气缸名，必须和上面定义过的名称完全一致。");
            sb.AppendLine("5. 步骤要足够详细：回零→移动到点位→等待输入→输出动作→气缸伸出/缩回→延时→计数，按真实工艺流程编排。");
            sb.AppendLine("6. 流程必须写明「类型」字段：运控流程写 \"运控\"，脚本流程写 \"脚本\"，视觉流程写 \"视觉\"。");
            sb.AppendLine("7. 脚本流程不要留 \"脚本\" 字段为空，至少 5 行完整可读的 Lua 源码（含 Log.Info/Variable.Get/Variable.Set/if/return）。");
            sb.AppendLine("8. 视觉流程的步骤里要包含 至少 1 个 \"功能\": \"相机\"（拍照）+ 1 个 \"功能\": \"视觉\"（匹配/检测）。");
            sb.AppendLine("9. 运控流程每个步骤都要写齐 4 个字段（功能/对象/动作/值），不要留空。");
            sb.AppendLine("10. 步骤的「功能」只能用这几种：轴 / 输入 / 输出 / 气缸 / 延时 / 变量 / 点位 / 通讯。");
            sb.AppendLine("    不要写 \"流程\"（本软件步骤层不支持调用子流程，写了也执行不了）。");
            sb.AppendLine("11. 同一个 JSON 里键名要保持一致：要么全用中文键名（名称/类型/步骤/值），要么全用英文（name/type/steps/value），不要混用。");
            sb.AppendLine();
            sb.AppendLine("【输出格式】严格按下面这个结构输出：");
            sb.AppendLine(SchemaText);
            return sb.ToString();
        }

        /// <summary>中文 JSON 契约模板（提示词用；解析器同时兼容英文键名）。</summary>
        private const string SchemaText = """
{
  "控制器": [
    { "名称": "控制卡1", "型号": "DMC5400", "卡号": 0, "轴数": 4, "总线": "脉冲" }
  ],
  "轴": [
    { "名称": "X轴", "类型": "脉冲", "轴号": 0, "单位": "mm", "速度": 100, "加速度": 50, "减速度": 50 },
    { "名称": "Y轴", "类型": "脉冲", "轴号": 1, "单位": "mm", "速度": 100, "加速度": 50, "减速度": 50 },
    { "名称": "Z轴", "类型": "脉冲", "轴号": 2, "单位": "mm", "速度": 80, "加速度": 40, "减速度": 40 }
  ],
  "输入": [
    { "名称": "启动", "功能": "启动按钮", "卡号": 0, "位号": 0 },
    { "名称": "停止", "功能": "停止按钮", "卡号": 0, "位号": 1 },
    { "名称": "复位", "功能": "复位按钮", "卡号": 0, "位号": 2 },
    { "名称": "急停", "功能": "安全门", "卡号": 0, "位号": 3 },
    { "名称": "手自动", "功能": "动点", "卡号": 0, "位号": 4 },
    { "名称": "暂停", "功能": "动点", "卡号": 0, "位号": 5 },
    { "名称": "原点到位", "功能": "原点", "卡号": 0, "位号": 6 },
    { "名称": "来料检测", "功能": "动点", "卡号": 0, "位号": 7 }
  ],
  "输出": [
    { "名称": "运行", "功能": "动点", "卡号": 0, "位号": 0 },
    { "名称": "就绪", "功能": "动点", "卡号": 0, "位号": 1 },
    { "名称": "报警", "功能": "动点", "卡号": 0, "位号": 2 },
    { "名称": "完成", "功能": "动点", "卡号": 0, "位号": 3 },
    { "名称": "暂停指示", "功能": "动点", "卡号": 0, "位号": 4 },
    { "名称": "取料阀", "功能": "动点", "卡号": 0, "位号": 5 },
    { "名称": "放料阀", "功能": "动点", "卡号": 0, "位号": 6 },
    { "名称": "绿灯", "功能": "动点", "卡号": 0, "位号": 7 }
  ],
  "气缸": [
    { "名称": "推料缸", "输出点": "Y0", "伸出感应": "X0", "缩回感应": "X1", "初始状态": "缩回" },
    { "名称": "夹爪缸", "输出点": "Y1", "伸出感应": "X2", "缩回感应": "X3", "初始状态": "缩回" }
  ],
  "流程": [
    {
      "名称": "主流程",
      "类型": "运控",
      "角色": "主流程",
      "步骤": [
        { "功能": "轴", "对象": "X轴", "动作": "回零", "值": "" },
        { "功能": "轴", "对象": "Y轴", "动作": "回零", "值": "" },
        { "功能": "轴", "对象": "Z轴", "动作": "回零", "值": "" },
        { "功能": "输入", "对象": "来料检测", "动作": "等待", "值": "1" },
        { "功能": "轴", "对象": "X轴", "动作": "移动", "值": "100" },
        { "功能": "轴", "对象": "Y轴", "动作": "移动", "值": "50" },
        { "功能": "轴", "对象": "Z轴", "动作": "移动", "值": "0" },
        { "功能": "气缸", "对象": "夹爪缸", "动作": "伸出", "值": "" },
        { "功能": "延时", "对象": "", "动作": "", "值": "300" },
        { "功能": "气缸", "对象": "夹爪缸", "动作": "缩回", "值": "" },
        { "功能": "输出", "对象": "完成", "动作": "置位", "值": "1" },
        { "功能": "变量", "对象": "计数", "动作": "加", "值": "1" },
        { "功能": "输出", "对象": "完成", "动作": "复位", "值": "0" }
      ]
    },
    {
      "名称": "复位流程",
      "角色": "复位流程",
      "步骤": [
        { "功能": "输出", "对象": "报警", "动作": "复位", "值": "0" },
        { "功能": "气缸", "对象": "推料缸", "动作": "缩回", "值": "" },
        { "功能": "气缸", "对象": "夹爪缸", "动作": "缩回", "值": "" },
        { "功能": "轴", "对象": "Z轴", "动作": "移动", "值": "0" },
        { "功能": "轴", "对象": "X轴", "动作": "回零", "值": "" },
        { "功能": "轴", "对象": "Y轴", "动作": "回零", "值": "" },
        { "功能": "轴", "对象": "Z轴", "动作": "回零", "值": "" },
        { "功能": "输出", "对象": "就绪", "动作": "置位", "值": "1" }
      ]
    },
    {
      "名称": "配方脚本",
      "类型": "脚本",
      "脚本": "-- 切换当前加工配方（依据变量「当前工序」决定工件号）\nlocal recipe = Variable.Get(\"当前工序\") or \"A\"\nif recipe == \"A\" then\n  Log.Info(\"运行 A 配方\")\n  Variable.Set(\"缺陷阈值\", 0.95)\nelseif recipe == \"B\" then\n  Log.Info(\"运行 B 配方\")\n  Variable.Set(\"缺陷阈值\", 0.90)\nelse\n  Log.Warn(\"未知配方：\" .. recipe)\nend\nreturn true"
    },
    {
      "名称": "缺陷检测",
      "类型": "视觉",
      "脚本": "-- 顶视相机拍照 + 模板匹配 + 缺陷检测\nlocal img = Camera.Grab(\"顶视相机\")          -- 拍照取图\nif img == nil then\n  Log.Error(\"相机取图失败\")\n  return false\nend\n\nlocal score = Vision.Match(img, \"模板A\")     -- 模板匹配，返回 0~1\nLog.Info(\"匹配得分：\" .. score)\nif score < 0.8 then\n  Log.Warn(\"匹配失败，跳过\")\n  return false\nend\n\nlocal defect = Vision.Defect(img, 0.9)       -- 缺陷检测\nif defect then\n  Variable.Set(\"缺陷计数\", (Variable.Get(\"缺陷计数\") or 0) + 1)\n  Log.Warn(\"检出缺陷，累计：\" .. Variable.Get(\"缺陷计数\"))\nend\nreturn true"
    }
  ],
  "工位": [
    {
      "名称": "工位1",
      "轴": ["X轴", "Y轴", "Z轴"],
      "点位": [
        { "名称": "取料位", "X": 100, "Y": 50, "Z": 0 },
        { "名称": "放料位", "X": 200, "Y": 150, "Z": 0 },
        { "名称": "安全位", "X": 0, "Y": 0, "Z": 50 },
        { "名称": "等待位", "X": 50, "Y": 50, "Z": 30 }
      ]
    }
  ],
  "通讯": [
    { "名称": "主站", "类型": "串口", "端口": "COM1", "波特率": 9600 }
  ],
  "相机": [
    { "名称": "顶视相机", "类型": "海康面阵", "接口": "GigE", "编号": 0, "触发模式": "软件触发", "曝光ms": 20, "增益": 1.0 },
    { "名称": "底视相机", "类型": "海康面阵", "接口": "GigE", "编号": 1, "触发模式": "硬件触发", "曝光ms": 30, "增益": 1.2 }
  ],
  "变量": [
    { "名称": "计数", "值": "0" },
    { "名称": "总数", "值": "0" },
    { "名称": "当前工位", "值": "1" }
  ]
}
""";

        // cameras 已加到 schema 上半段；流程示例加在下面（运控 / 脚本 / 视觉 / 复位 4 个示例）

        // ==================== 2. 解析并应用（粘贴按钮用） ====================

        /// <summary>
        /// 容错解析 AI 返回的 JSON 并写入目标 ProjectData（中英文键名都兼容）。
        /// 返回人类可读的结果摘要。
        /// </summary>
        public static string ApplyGenerated(ProjectData data, string json)
        {
            if (data == null) return "目标工程数据为空。";
            if (string.IsNullOrWhiteSpace(json)) return "剪贴板内容为空。";

            var text = StripCodeFence(json.Trim());

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(text, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });
            }
            catch (JsonException ex)
            {
                return "JSON 解析失败：" + ex.Message + "（请复制 AI 返回的完整 JSON，不要只复制一部分）";
            }

            // ===== 先清空所有集合：粘贴生成是「完全替换」语义（弹窗已确认） =====
            data.Controllers.Clear();
            data.Axes.Clear();
            data.Inputs.Clear();
            data.Outputs.Clear();
            data.Cylinders.Clear();
            data.Cameras.Clear();
            data.Comms.Clear();
            data.Trays.Clear();
            data.PointTables.Clear();
            data.Flows.Clear();
            data.Variables.Clear();

            using (doc)
            {
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return "内容不是一个 JSON 对象。请复制 AI 返回的完整 JSON（以 { 开头）。";

                var log = new List<string>();
                int n;

                n = ApplyControllers(data, root); if (n > 0) log.Add($"控制器 {n}");
                n = ApplyAxes(data, root); if (n > 0) log.Add($"轴 {n}");
                n = ApplyIo(data, root, isInput: true); if (n > 0) log.Add($"输入 {n}");
                n = ApplyIo(data, root, isInput: false); if (n > 0) log.Add($"输出 {n}");
                n = ApplyCylinders(data, root); if (n > 0) log.Add($"气缸 {n}");
                n = ApplyCameras(data, root); if (n > 0) log.Add($"相机 {n}");
                n = ApplyComms(data, root); if (n > 0) log.Add($"通讯 {n}");
                n = ApplyPointTables(data, root); if (n > 0) log.Add($"工位 {n}");
                n = ApplyFlows(data, root); if (n > 0) log.Add($"流程 {n}");
                n = ApplyVariables(data, root); if (n > 0) log.Add($"变量 {n}");

                data.EnsurePointTables();
                Catalog.SyncAllFromData(data);

                return log.Count == 0
                    ? "没识别到可导入的数据。请确认复制的是 AI 返回的完整 JSON（应包含「轴」「输入」「输出」「气缸」「流程」等分类）。"
                    : "已生成：" + string.Join("、", log);
            }
        }

        /// <summary>去掉 AI 常用的 ```json 代码围栏。</summary>
        private static string StripCodeFence(string text)
        {
            if (!text.StartsWith("```")) return text;
            var firstNl = text.IndexOf('\n');
            if (firstNl < 0) return text;
            var body = text.Substring(firstNl + 1);
            var end = body.LastIndexOf("```", StringComparison.Ordinal);
            if (end >= 0) body = body.Substring(0, end);
            return body.Trim();
        }

        // ---------------- 各分类导入（键名走别名匹配） ----------------

        private static int ApplyControllers(ProjectData d, JsonElement root)
        {
            int n = 0;
            foreach (var e in Items(root, "控制器", "controllers", "controller"))
            {
                var name = Str(e, "名称", "name", "名字");
                if (string.IsNullOrWhiteSpace(name)) continue;
                d.Controllers.Add(new AxisControllerItem
                {
                    Name = name,
                    Vendor = Str(e, "厂商", "vendor", "品牌") ?? "雷赛",
                    CardType = Str(e, "型号", "cardType", "卡型号") ?? "",
                    CardNo = Int(e, "卡号", "cardNo"),
                    AxisCount = IntDef(e, 4, "轴数", "axisCount"),
                    BusType = Str(e, "总线", "busType", "总线类型") ?? "脉冲",
                    Connection = Str(e, "连接", "connection", "连接方式") ?? "PCI"
                });
                n++;
            }
            return n;
        }

        private static int ApplyAxes(ProjectData d, JsonElement root)
        {
            int n = 0;
            foreach (var e in Items(root, "轴", "axes", "axis"))
            {
                var name = Str(e, "名称", "name", "轴名", "名字");
                if (string.IsNullOrWhiteSpace(name)) continue;
                d.Axes.Add(new AxisItem
                {
                    Name = name,
                    Controller = Str(e, "控制器", "controller", "归属控制器") ?? "",
                    AxisType = Str(e, "类型", "axisType", "轴类型") ?? "脉冲",
                    AxisNo = Int(e, "轴号", "axisNo", "序号"),
                    Unit = Str(e, "单位", "unit") ?? "mm",
                    Speed = DblDef(e, 100, "速度", "speed"),
                    Accel = DblDef(e, 50, "加速度", "accel"),
                    Decel = DblDef(e, 50, "减速度", "decel")
                });
                n++;
            }
            return n;
        }

        private static int ApplyIo(ProjectData d, JsonElement root, bool isInput)
        {
            int n = 0;
            var keys = isInput
                ? new[] { "输入", "输入点", "inputs", "input" }
                : new[] { "输出", "输出点", "outputs", "output" };

            foreach (var e in Items(root, keys))
            {
                var name = Str(e, "名称", "name", "名字");
                if (string.IsNullOrWhiteSpace(name)) continue;
                var io = new IoItem
                {
                    Name = name,
                    Function = Str(e, "功能", "function") ?? "动点",
                    Controller = Str(e, "控制器", "controller") ?? "",
                    CardNo = Int(e, "卡号", "cardNo"),
                    ModuleNo = Int(e, "模块", "moduleNo"),
                    Sequence = Int(e, "位号", "序号", "sequence"),
                    Level = Str(e, "电平", "level") ?? "取反"
                };
                if (isInput) d.Inputs.Add(io); else d.Outputs.Add(io);
                n++;
            }
            return n;
        }

        private static int ApplyCylinders(ProjectData d, JsonElement root)
        {
            int n = 0;
            foreach (var e in Items(root, "气缸", "cylinders", "cylinder"))
            {
                var name = Str(e, "名称", "name", "名字");
                if (string.IsNullOrWhiteSpace(name)) continue;
                var init = Str(e, "初始状态", "initialState") ?? "缩回";
                if (init != "伸出" && init != "缩回") init = "缩回";
                d.Cylinders.Add(new CylinderItem
                {
                    Name = name,
                    DeviceId = Str(e, "设备编号", "deviceId") ?? name,
                    OutPoint = Str(e, "输出点", "outPoint") ?? "",
                    SensorExtend = Str(e, "伸出感应", "sensorExtend") ?? "",
                    SensorRetract = Str(e, "缩回感应", "sensorRetract") ?? "",
                    Type = Str(e, "类型", "type") ?? "双作用",
                    InitialState = init,
                    CurrentState = init
                });
                n++;
            }
            return n;
        }

        private static int ApplyCameras(ProjectData d, JsonElement root)
        {
            int n = 0;
            foreach (var e in Items(root, "相机", "摄像头", "cameras", "camera"))
            {
                var name = Str(e, "名称", "name");
                if (string.IsNullOrWhiteSpace(name)) continue;

                // 相机字段名兼容：编号/slotNo → Port；IP/接口 → IpAddress（去掉 GigE 字面只留 IP）；
                // 触发模式：仅存到 Description
                var vendor = Str(e, "厂商", "vendor", "品牌", "类型", "type") ?? "海康威视";
                var ip = Str(e, "IP", "ip", "ipAddress")
                      ?? Str(e, "接口", "interface", "connection")
                      ?? "192.168.1.100";
                if (ip.Equals("GigE", StringComparison.OrdinalIgnoreCase)) ip = "192.168.1.100";

                // 端口：优先用显式「端口」；否则用「编号」推导（8000+编号），避免编号 1 变成 Port=1。
                // 注意不能把「编号」直接当 Port——编号 0/1 是相机序号，不是网络端口。
                var explicitPort = Str(e, "端口", "port");
                var slotNo = IntDef(e, 0, "编号", "slotNo", "index");
                var port = explicitPort != null
                    ? IntDef(e, 8000, "端口", "port")
                    : 8000 + slotNo;

                var width = IntDef(e, 1920, "宽度", "width", "分辨率宽");
                var height = IntDef(e, 1080, "高度", "height", "分辨率高");
                var exposure = DblDef(e, 10.0, "曝光ms", "曝光", "exposureMs", "exposure");
                var gain = DblDef(e, 1.0, "增益", "gain");

                // 触发模式追加到 Description
                var desc = Str(e, "备注", "description", "remark");
                var trig = Str(e, "触发模式", "trigger", "triggerMode");
                if (!string.IsNullOrEmpty(trig))
                    desc = "[触发:" + trig + "] " + (desc ?? "");

                d.Cameras.Add(new CameraItem
                {
                    Name = name,
                    Vendor = vendor,
                    IpAddress = ip,
                    Port = port,
                    Width = width == 0 ? 1920 : width,
                    Height = height == 0 ? 1080 : height,
                    ExposureMs = exposure,
                    Gain = gain,
                    Description = (desc ?? "").Trim()
                });
                n++;
            }
            return n;
        }

        private static int ApplyComms(ProjectData d, JsonElement root)
        {
            int n = 0;
            foreach (var e in Items(root, "通讯", "通信", "comms", "comm"))
            {
                var name = Str(e, "名称", "name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                d.Comms.Add(new CommItem
                {
                    Name = name,
                    CommType = Str(e, "类型", "commType", "通讯类型") ?? "串口",
                    PortOrIp = Str(e, "端口", "portOrIp", "串口号", "IP") ?? "COM1",
                    BaudOrPort = IntDef(e, 9600, "波特率", "baudOrPort", "波特"),
                    Parity = Str(e, "校验", "parity") ?? "无",
                    DataBits = IntDef(e, 8, "数据位", "dataBits"),
                    StopBits = DblDef(e, 1, "停止位", "stopBits"),
                    TimeoutMs = IntDef(e, 1000, "超时", "timeoutMs")
                });
                n++;
            }
            return n;
        }

        private static int ApplyPointTables(ProjectData d, JsonElement root)
        {
            int n = 0;
            foreach (var e in Items(root, "工位", "点位表", "pointTables", "points"))
            {
                var tname = Str(e, "名称", "name");
                if (string.IsNullOrWhiteSpace(tname)) continue;

                var t = new PointTable { Name = tname };

                if (TryGet(e, out var ax, "轴", "axes") && ax.ValueKind == JsonValueKind.Array)
                {
                    int i = 0;
                    foreach (var a in ax.EnumerateArray())
                    {
                        if (i >= PointTable.SlotCount) break;
                        t.AxisNames[i++] = a.ValueKind == JsonValueKind.String ? (a.GetString() ?? "") : "";
                    }
                }

                foreach (var p in Items(e, "点位", "points"))
                {
                    var pname = Str(p, "名称", "name");
                    if (string.IsNullOrWhiteSpace(pname)) continue;
                    var item = new PointItem { Name = pname };
                    string n0 = t.AxisNames.Count > 0 ? t.AxisNames[0] : "";
                    string n1 = t.AxisNames.Count > 1 ? t.AxisNames[1] : "";
                    string n2 = t.AxisNames.Count > 2 ? t.AxisNames[2] : "";
                    string n3 = t.AxisNames.Count > 3 ? t.AxisNames[3] : "";
                    // 坐标键名兜底：AI 可能用轴名（"X搬运轴"）、单字母（"X"）、或语义名（"旋转"/"角度"）。
                    // 第 4 槽常见写法「旋转」「旋转轴」「R」，都要覆盖，否则旋转坐标会丢。
                    item.Positions[0] = new PointAxis { Position = DblDef(p, 0, n0, "x", "X"), Speed = 100 };
                    item.Positions[1] = new PointAxis { Position = DblDef(p, 0, n1, "y", "Y"), Speed = 100 };
                    item.Positions[2] = new PointAxis { Position = DblDef(p, 0, n2, "z", "Z"), Speed = 100 };
                    item.Positions[3] = new PointAxis { Position = DblDef(p, 0, n3, "r", "R", "旋转", "旋转轴", "角度"), Speed = 100 };
                    t.Points.Add(item);
                }

                d.PointTables.Add(t);
                n++;
            }
            return n;
        }

        private static int ApplyFlows(ProjectData d, JsonElement root)
        {
            int n = 0;
            foreach (var e in Items(root, "流程", "flows", "flow"))
            {
                var name = Str(e, "名称", "name");
                if (string.IsNullOrWhiteSpace(name)) continue;

                var roleStr = Str(e, "角色", "role") ?? "主流程";
                var fr = roleStr.Contains("复位") || roleStr.Equals("Reset", StringComparison.OrdinalIgnoreCase)
                       ? FlowRole.Reset
                       : FlowRole.Main;

                // 「类型」别名必须含 type：AI 常中英混用（第一条写「类型」，后几条写 type）
                var kindStr = Str(e, "类型", "kind", "type") ?? "";
                var fk = NormFlowKind(kindStr);

                var f = new FlowItem { Name = name, Kind = fk, Role = fr };

                // 脚本 / 视觉流程都走 Lua 源码；运控流程走表格步骤。
                // 容差：若流程声明是 Lua/Vision 但没给「脚本」字段，退回去读「步骤」。
                // 运控流程也兼容「脚本」字段（AI 偶尔给运控流程塞脚本，此时按 Lua 存，别丢内容）。
                var lua = Str(e, "脚本", "源码", "luaSource", "script", "source") ?? "";

                if (fk != FlowKind.Table && !string.IsNullOrWhiteSpace(lua))
                {
                    f.LuaSource = lua;
                }
                else
                {
                    foreach (var s in Items(e, "步骤", "steps", "step"))
                    {
                        var func = Str(s, "功能", "function") ?? "轴";
                        var target = Str(s, "对象", "name", "target", "名称") ?? "";
                        var op = Str(s, "动作", "operation") ?? "移动";
                        // 「值」别名必须含 value：AI 常中英混用（主流程写「值」，复位流程写 value）
                        var val = Str(s, "值", "value", "setValue", "参数") ?? "";

                        var step = new FlowStep
                        {
                            // 名称列必须用「对象」（轴名/IO名/气缸名/点位名/变量名）——空就回退到值。
                            Name = !string.IsNullOrWhiteSpace(target) ? target
                                 : (!string.IsNullOrWhiteSpace(val) ? val : ""),
                            Logic = Str(s, "条件", "logic") ?? "就",
                            Function = NormFunction(func),
                            Property = NormProperty(func, op),
                            Operation = NormOperation(func, op),
                            // 回零/置位/复位 等动作的「值」本来就空，此时保留 val 空，不要塞 target 占位
                            // （避免截图里"X回退轴"出现在设置值列）
                            SetValue = val,
                            Timeout = Str(s, "超时", "timeout") ?? "空",
                            DurationMs = Int(s, "时长", "延时", "durationMs")
                        };
                        f.Steps.Add(step);
                    }
                }

                d.Flows.Add(f);
                n++;
            }
            return n;
        }

        /// <summary>把 AI 写的流程类型（中英文/口语化）归一为本软件 FlowKind（运控/脚本/视觉）。</summary>
        private static FlowKind NormFlowKind(string kind)
        {
            if (string.IsNullOrWhiteSpace(kind)) return FlowKind.Table;

            // 脚本类：脚本 / Lua / LuaScript / 脚本流程
            if (kind.Contains("脚本")
             || kind.Equals("Lua", StringComparison.OrdinalIgnoreCase)
             || kind.Equals("LuaScript", StringComparison.OrdinalIgnoreCase)
             || kind.Equals("Script", StringComparison.OrdinalIgnoreCase))
                return FlowKind.Lua;

            // 视觉类：视觉 / Vision / 相机 / 图像
            if (kind.Contains("视觉")
             || kind.Contains("相机")
             || kind.Contains("图像")
             || kind.Equals("Vision", StringComparison.OrdinalIgnoreCase)
             || kind.Equals("Camera", StringComparison.OrdinalIgnoreCase))
                return FlowKind.Vision;

            // 运控类：运控 / 运动控制 / 表格 / Table / Motion（含默认兜底）
            return FlowKind.Table;
        }

        /// <summary>把 AI 写的中文/英文功能名归一为本软件 Function 取值。</summary>
        private static string NormFunction(string func)
        {
            if (string.IsNullOrWhiteSpace(func)) return "轴";

            // 英文/缩写精确匹配优先（豆包常直接写 "IO"/"Axis"/"Output" 等）
            if (func.Equals("IO", StringComparison.OrdinalIgnoreCase)
             || func.Equals("Input", StringComparison.OrdinalIgnoreCase)
             || func.Equals("Output", StringComparison.OrdinalIgnoreCase)) return "IO";
            if (func.Equals("Axis", StringComparison.OrdinalIgnoreCase)) return "轴";
            if (func.Equals("Cylinder", StringComparison.OrdinalIgnoreCase)) return "气缸";
            if (func.Equals("Point", StringComparison.OrdinalIgnoreCase)) return "点位";
            if (func.Equals("Variable", StringComparison.OrdinalIgnoreCase)) return "变量";
            if (func.Equals("Delay", StringComparison.OrdinalIgnoreCase)) return "系统";
            if (func.Equals("Comm", StringComparison.OrdinalIgnoreCase)) return "modbus";

            // 中文包含匹配
            if (func.Contains("轴") || func.Contains("移动")) return "轴";
            if (func.Contains("输出") || func.Contains("输出点")) return "IO";
            if (func.Contains("输入") || func.Contains("等待输入")) return "IO";
            if (func.Contains("气缸") || func.Contains("电磁阀")) return "气缸";
            if (func.Contains("延时") || func.Contains("等待")) return "系统";
            if (func.Contains("点位")) return "点位";
            if (func.Contains("变量")) return "变量";
            if (func.Contains("通讯") || func.Contains("通信") || func.Contains("modbus")) return "modbus";
            // 「流程/调用子流程」本软件步骤层不支持（ExecuteHardwareStep 只认 轴/IO/气缸/modbus/点位），
            // 归一到「系统」（只记日志），避免这类步骤被误判成「轴」而真的去动轴。
            if (func.Contains("流程") || func.Contains("调用")) return "系统";
            return "轴";
        }

        /// <summary>Property 槽位：中文/英文动作 → 本软件的属性取值。</summary>
        private static string NormProperty(string func, string op)
        {
            var f = NormFunction(func);
            if (f == "轴")
            {
                if (op.Contains("回零") || op.Contains("原点") || op.Equals("Home", StringComparison.OrdinalIgnoreCase)) return "回零";
                return "位置";
            }
            if (f == "气缸")
                return op.Contains("缩回") || op.Equals("Retract", StringComparison.OrdinalIgnoreCase) ? "缩回" : "伸出";
            if (f == "系统") return "延时";
            if (f == "IO")
                return op.Contains("复位") || op.Equals("Reset", StringComparison.OrdinalIgnoreCase) ? "复位" : "输出";
            return "位置";
        }

        /// <summary>Operation 槽位：中文/英文动作 → 本软件的运算取值。</summary>
        private static string NormOperation(string func, string op)
        {
            if (string.IsNullOrWhiteSpace(op)) return "等于";
            if (op.Contains("回零") || op.Equals("Home", StringComparison.OrdinalIgnoreCase)) return "回零";
            if (op.Contains("置位") || op.Contains("打开") || op.Equals("Set", StringComparison.OrdinalIgnoreCase)) return "置位";
            if (op.Contains("复位") || op.Contains("关闭") || op.Equals("Reset", StringComparison.OrdinalIgnoreCase)) return "复位";
            if (op.Contains("伸出") || op.Equals("Extend", StringComparison.OrdinalIgnoreCase)) return "伸出";
            if (op.Contains("缩回") || op.Equals("Retract", StringComparison.OrdinalIgnoreCase)) return "缩回";
            if (op.Contains("等待") || op.Equals("Wait", StringComparison.OrdinalIgnoreCase)) return "等待";
            if (op.Contains("加") || op.Equals("Add", StringComparison.OrdinalIgnoreCase)) return "加";
            if (op.Contains("减") || op.Equals("Sub", StringComparison.OrdinalIgnoreCase)) return "减";
            return "等于";
        }

        private static int ApplyVariables(ProjectData d, JsonElement root)
        {
            var pairs = new List<(string name, string value)>();
            foreach (var e in Items(root, "变量", "variables", "variable"))
            {
                var name = Str(e, "名称", "name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                pairs.Add((name, Str(e, "值", "value", "初始值") ?? "0"));
            }
            if (pairs.Count == 0) return 0;

            for (int i = 0; i < pairs.Count; i += 5)
            {
                var v = new VariableRow();
                var batch = pairs.Skip(i).Take(5).ToArray();
                if (batch.Length > 0) { v.Name1 = batch[0].name; v.Value1 = batch[0].value; }
                if (batch.Length > 1) { v.Name2 = batch[1].name; v.Value2 = batch[1].value; }
                if (batch.Length > 2) { v.Name3 = batch[2].name; v.Value3 = batch[2].value; }
                if (batch.Length > 3) { v.Name4 = batch[3].name; v.Value4 = batch[3].value; }
                if (batch.Length > 4) { v.Name5 = batch[4].name; v.Value5 = batch[4].value; }
                d.Variables.Add(v);
            }
            return pairs.Count;
        }

        // ---------------- 容错读取辅助（全部支持中英文别名） ----------------

        private static IEnumerable<JsonElement> Items(JsonElement parent, params string[] aliases)
        {
            if (parent.ValueKind != JsonValueKind.Object) yield break;
            if (!TryGet(parent, out var arr, aliases)) yield break;
            if (arr.ValueKind != JsonValueKind.Array) yield break;
            foreach (var e in arr.EnumerateArray())
                if (e.ValueKind == JsonValueKind.Object) yield return e;
        }

        private static bool TryGet(JsonElement parent, out JsonElement value, params string[] aliases)
        {
            value = default;
            if (parent.ValueKind != JsonValueKind.Object) return false;
            foreach (var a in aliases)
            {
                if (string.IsNullOrEmpty(a)) continue;
                if (parent.TryGetProperty(a, out var v)) { value = v; return true; }
            }
            return false;
        }

        private static string? Str(JsonElement e, params string[] aliases)
        {
            if (!TryGet(e, out var v, aliases)) return null;
            return v.ValueKind switch
            {
                JsonValueKind.String => v.GetString(),
                JsonValueKind.Number => v.GetRawText(),
                JsonValueKind.True => "1",
                JsonValueKind.False => "0",
                _ => null
            };
        }

        private static int Int(JsonElement e, params string[] aliases) => IntDef(e, 0, aliases);

        private static int IntDef(JsonElement e, int def, params string[] aliases)
        {
            if (!TryGet(e, out var v, aliases)) return def;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)) return i;
            if (v.ValueKind == JsonValueKind.String &&
                int.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s)) return s;
            return def;
        }

        private static double DblDef(JsonElement e, double def, params string[] aliases)
        {
            if (!TryGet(e, out var v, aliases)) return def;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d)) return d;
            if (v.ValueKind == JsonValueKind.String &&
                double.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s)) return s;
            return def;
        }
    }
}
// ◇作者保留所有权利　请勿删除※⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓⁣
