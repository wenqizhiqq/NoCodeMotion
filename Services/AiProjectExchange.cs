// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// ◆温​启​志‌◆‌编​写​◇​微⁣信‏﹕​1‌8‏7‌◆‎1‏9‍3‍6⁣◇⁣1⁣3‏9‎9⁠　⁠※‍保‎留​所‍有‍权⁣利‌请⁠勿​删⁣除‌◇‌⁣
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
using NoCodeMotion.Models.NodeGraph;

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
            sb.AppendLine("    * 1 个「视觉流程」（类型：\"视觉\"，用 \"视觉步骤\" 数组描述 图像采集/模板匹配/缺陷检测，不要写 Lua）");
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
            sb.AppendLine("7. 脚本流程不要留 \"脚本\" 字段为空，至少 5 行完整可读的 Lua 源码（含 Log.Info/Variable.Get/Variable.Set/if/return），且只能调用下面【Lua 可用的 API】里列出的函数。");
            sb.AppendLine("8. 视觉流程不要写 \"脚本\"，改用 \"视觉步骤\" 数组：至少 1 个 \"类型\": \"图像采集\" + 1 个 \"类型\": \"模板匹配\"（或 \"缺陷检测\"）。");
            sb.AppendLine("9. 运控流程每个步骤都要写齐 4 个字段（功能/对象/动作/值），不要留空。");
            sb.AppendLine("10. 步骤的「功能」只能用这几种：轴 / 输入 / 输出 / 气缸 / 延时 / 变量 / 点位 / 通讯。");
            sb.AppendLine("    不要写 \"流程\"（本软件步骤层不支持调用子流程，写了也执行不了）。");
            sb.AppendLine("11. 同一个 JSON 里键名要保持一致：要么全用中文键名（名称/类型/步骤/值），要么全用英文（name/type/steps/value），不要混用。");
            sb.AppendLine();
            // 工程级提示词里也有「脚本流程」，同样要把真实 API 清单摊开，
            // 否则 AI 会写 Camera.Grab / Log.Error 之类沙箱里没有的调用。
            sb.AppendLine(BuildLuaApiText());
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
      "视觉步骤": [
        { "名称": "拍照1", "类型": "图像采集", "启用": true, "来源": "相机", "相机": "顶视相机", "曝光ms": 20, "宽": 1920, "高": 1080 },
        { "名称": "定位1", "类型": "模板匹配", "启用": true, "模板路径": "Templates/A.ncc", "匹配分": 0.8, "角度范围": 360, "匹配模式": "灰度匹配" },
        { "名称": "检测1", "类型": "缺陷检测", "启用": true, "算法": "NCC", "检测模式": "阈值面积", "最小面积": 100, "最大面积": 100000, "阈值": 128 }
      ]
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

                if (fk == FlowKind.NodeGraph)
                {
                    f.GraphJson = ReadGraphJson(e);
                }
                else if (fk == FlowKind.Vision)
                {
                    FillVisualSteps(f, e);
                }
                else if (fk != FlowKind.Table && !string.IsNullOrWhiteSpace(lua))
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

        /// <summary>把 AI 写的流程类型（中英文/口语化）归一为本软件 FlowKind（运控/脚本/视觉/节点图）。</summary>
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

            // 节点图类：节点图 / 节点 / NodeGraph / Graph
            // 必须排在「图像」之后判断，避免与视觉流程抢词。
            if (kind.Contains("节点")
             || kind.Contains("NodeGraph", StringComparison.OrdinalIgnoreCase)
             || kind.Contains("Graph", StringComparison.OrdinalIgnoreCase))
                return FlowKind.NodeGraph;

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

        // ==================== 3. 流程级交换（流程页「复制JSON / 粘贴生成」按钮用） ====================
        //
        // 与上面工程级交换（1/2 节）的区别：只针对**单个流程**，绝不碰工程里其它配置
        // （轴/IO/气缸/点位/变量 等一律不动），所以粘贴不需要清空工程。
        //
        // 用户流程：流程页点【复制JSON】→ 剪贴板拿到「说明 + 当前流程 JSON + 可用名称 + 契约」
        //          → 粘到豆包/WorkBuddy 等 AI，AI 按需求产出流程 JSON
        //          → 复制回来点【粘贴生成】→ 写入当前流程（或新增流程）。
        //
        // 关键设计：**导出必须无损、导入必须夹到合法取值**。
        //   DataGrid 的「逻辑/功能/属性/运算/超时」四列都是固定下拉框（ItemsSource 是 x:Array），
        //   值一旦不在列表里，单元格就显示空白——看起来像没导入成功。
        //   所以：① 导出时把 Property/Operation 原样带出，往返不改语义；
        //        ② 导入时凡是不合法的取值一律映射/回退到合法值（ClampProperty / ClampOperation）。
        //   合法取值以两处为准（本文件把它们抄成常量表，改 UI 下拉时记得同步）：
        //     · Views/FlowPage.xaml 的 LogicOptions / FunctionOptions / OperationOptions / TimeoutOptions
        //     · Views/FunctionToPropertiesConverter 的「功能 → 属性」映射
        //     · FlowViewModel.GetTemplateSteps 里 63 条内置模板步骤实际用的 9 组 (功能,属性,运算) 组合

        /// <summary>「逻辑」列合法取值（对应 FlowPage.xaml 的 LogicOptions）。</summary>
        private static readonly string[] LegalLogic =
            { "如果", "就", "并且", "或者", "否则", "否则如果", "循环开始", "循环结束", "等待", "延时", "结束", "注释" };

        /// <summary>「超时」列合法取值（对应 FlowPage.xaml 的 TimeoutOptions）。</summary>
        private static readonly string[] LegalTimeout =
            { "空", "超时1秒就报警", "超时3秒就报警", "超时10秒就报警", "超时1秒就停机", "超时3秒就停机", "超时10秒就停机" };

        /// <summary>「功能」列合法取值（对应 FlowPage.xaml 的 FunctionOptions）。</summary>
        private static readonly string[] LegalFunctions =
            { "轴", "IO", "气缸", "点位", "modbus", "变量", "系统", "相机", "延时" };

        /// <summary>「属性」列合法取值（对应 FunctionToPropertiesConverter 的映射；未列出的功能回退到「速度」）。</summary>
        private static readonly Dictionary<string, string[]> LegalProperties = new()
        {
            ["轴"] = new[] { "速度", "位置", "编码器位置", "扭矩", "电流", "加速度", "已回零" },
            ["IO"] = new[] { "输入状态", "输出状态", "脉冲状态", "报警状态" },
            ["气缸"] = new[] { "伸出到位", "缩回到位", "电磁阀", "压力", "动作中" },
            ["modbus"] = new[] { "寄存器值", "线圈状态", "保持寄存器", "输入寄存器" },
            ["变量"] = new[] { "数值", "字符串", "布尔" },
            ["点位"] = new[] { "速度", "加速度", "到位误差", "是否等待到位" },
            ["系统"] = new[] { "运行时间", "节拍", "报警数", "急停状态" },
        };

        /// <summary>
        /// 「运算」列合法取值。
        /// 注意：轴/IO/气缸/modbus 用的是内置模板里的 API 名（MoveAxisAbs / HomeAxis / WriteOutput …），
        /// 这几个**不在** FlowPage 的 OperationOptions 下拉里，但它们是软件自身模板一直在用的写法，
        /// 生成结果与内置模板保持一致才最安全；其余功能用 OperationOptions 里的中文运算。
        /// </summary>
        private static readonly Dictionary<string, string[]> LegalOperations = new()
        {
            ["轴"] = new[] { "MoveAxisAbs", "HomeAxis", "WaitAxisStop", "SetAxisSpeed" },
            ["IO"] = new[] { "WriteOutput", "ReadInput", "Wait" },
            ["气缸"] = new[] { "CylinderMove", "CylinderReset" },
            ["modbus"] = new[] { "CommSend" },
        };

        /// <summary>通用（变量/系统/点位等）的「运算」合法取值（对应 FlowPage.xaml 的 OperationOptions）。</summary>
        private static readonly string[] GeneralOperations =
        {
            "修改", "修改为", "加", "减", "乘", "除", "等于", "大于", "小于", "大于等于", "小于等于",
            "取模", "取反", "与", "或", "是否等于", "是否不等于", "是否大于", "是否小于", "是否大于等于", "是否小于等于"
        };

        // ---------------- 3.1 导出 ----------------

        /// <summary>把单个流程导出为交换 JSON（【复制JSON】里给 AI 看的上下文，也是 AI 输出的模板）。</summary>
        public static string ExportFlowJson(FlowItem? flow)
        {
            if (flow == null) return "{}";

            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"名称\": ").Append(J(flow.Name)).Append(", ");
            sb.Append("\"类型\": ").Append(J(KindText(flow.Kind))).Append(", ");
            sb.Append("\"角色\": ").Append(J(RoleText(flow.Role)));

            switch (flow.Kind)
            {
                case FlowKind.Lua:
                    sb.Append(", \"脚本\": ").Append(J(flow.LuaSource));
                    break;
                case FlowKind.Vision:
                    sb.Append(", \"视觉步骤\": ").Append(ExportVisualSteps(flow.VisualSteps));
                    break;
                case FlowKind.NodeGraph:
                    sb.Append(", \"节点图\": ").Append(ExportGraph(flow.GraphJson));
                    break;
                default:
                    sb.Append(", \"步骤\": ").Append(ExportSteps(flow.Steps));
                    break;
            }
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>步骤数组 → JSON。字段名与提示词里的契约一致；导出带「属性/运算」，保证往返无损。</summary>
        private static string ExportSteps(IEnumerable<FlowStep> steps)
        {
            var sb = new StringBuilder("[");
            bool first = true;
            foreach (var s in steps)
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append("{ ");
                sb.Append("\"逻辑\": ").Append(J(s.Logic)).Append(", ");
                sb.Append("\"功能\": ").Append(J(s.Function)).Append(", ");
                sb.Append("\"对象\": ").Append(J(s.Name)).Append(", ");
                sb.Append("\"属性\": ").Append(J(s.Property)).Append(", ");
                sb.Append("\"运算\": ").Append(J(s.Operation)).Append(", ");
                sb.Append("\"值\": ").Append(J(s.SetValue)).Append(", ");
                sb.Append("\"超时\": ").Append(J(s.Timeout));
                // 「耗时」必须一起导出：FillFlow 会读它（见 DurationMs = Int(s, "耗时", ...)），
                // 漏掉的话「粘贴生成 / 回退」往返一圈就把每步的耗时清零了。
                sb.Append(", \"耗时\": ").Append(JN(s.DurationMs));
                sb.Append(" }");
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>视觉步骤数组 → JSON（只带与当前步骤类型有关的字段，避免几十个无关字段刷屏）。</summary>
        private static string ExportVisualSteps(IEnumerable<VisualFlowStep> steps)
        {
            var sb = new StringBuilder("[");
            bool first = true;
            foreach (var s in steps)
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append("{ ");
                sb.Append("\"名称\": ").Append(J(s.Name)).Append(", ");
                sb.Append("\"类型\": ").Append(J(s.StepType)).Append(", ");
                sb.Append("\"启用\": ").Append(s.Enabled ? "true" : "false");

                switch (s.StepType)
                {
                    case "图像采集":
                        sb.Append(", \"来源\": ").Append(J(s.SourceType));
                        sb.Append(", \"相机\": ").Append(J(s.CameraId));
                        sb.Append(", \"曝光ms\": ").Append(JN(s.ExposureMs));
                        sb.Append(", \"宽\": ").Append(JN(s.Width));
                        sb.Append(", \"高\": ").Append(JN(s.Height));
                        if (!string.IsNullOrWhiteSpace(s.FolderPath))
                            sb.Append(", \"文件夹\": ").Append(J(s.FolderPath));
                        break;

                    case "模板匹配":
                        sb.Append(", \"模板路径\": ").Append(J(s.TemplatePath));
                        sb.Append(", \"匹配分\": ").Append(JN(s.ScoreThreshold));
                        sb.Append(", \"角度范围\": ").Append(JN(s.AngleRange));
                        sb.Append(", \"匹配模式\": ").Append(J(s.MatchMode));
                        break;

                    case "图像预处理":
                        sb.Append(", \"预处理\": ").Append(J(s.PreOp));
                        sb.Append(", \"参数1\": ").Append(JN(s.PreParam1));
                        sb.Append(", \"参数2\": ").Append(JN(s.PreParam2));
                        if (!string.IsNullOrWhiteSpace(s.PreRoi))
                            sb.Append(", \"预处理ROI\": ").Append(J(s.PreRoi));
                        if (!string.IsNullOrWhiteSpace(s.PreImage2Path))
                            sb.Append(", \"第二张图\": ").Append(J(s.PreImage2Path));
                        break;

                    case "缺陷检测":
                        sb.Append(", \"算法\": ").Append(J(s.Algorithm));
                        sb.Append(", \"检测模式\": ").Append(J(s.DetectMode));
                        sb.Append(", \"最小面积\": ").Append(JN(s.MinArea));
                        sb.Append(", \"最大面积\": ").Append(JN(s.MaxArea));
                        sb.Append(", \"阈值\": ").Append(JN(s.Threshold));
                        break;

                    case "测量":
                        sb.Append(", \"测量模式\": ").Append(J(s.MeasureMode));
                        sb.Append(", \"标定\": ").Append(JN(s.Calibration));
                        sb.Append(", \"单位\": ").Append(J(s.Unit));
                        break;

                    case "通讯":
                        sb.Append(", \"协议\": ").Append(J(s.Protocol));
                        sb.Append(", \"目标\": ").Append(J(s.Target));
                        sb.Append(", \"内容\": ").Append(J(s.Content));
                        break;
                }

                if (!string.IsNullOrWhiteSpace(s.SavePath))
                    sb.Append(", \"保存路径\": ").Append(J(s.SavePath));

                sb.Append(" }");
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// 节点图 → JSON。GraphJson 本身就是 NgDoc 的 JSON（{"Nodes":[...],"Connections":[...]}），
        /// 原样内嵌即可；解析失败时给个空图，避免把整段提示词搞坏。
        /// </summary>
        private static string ExportGraph(string? graphJson)
        {
            if (string.IsNullOrWhiteSpace(graphJson)) return "{\"Nodes\":[],\"Connections\":[]}";
            try
            {
                using var d = JsonDocument.Parse(graphJson);
                // 再序列化一遍而不是 GetRawText()：GraphJson 是 NgDoc 用默认编码器写的，
                // 中文被存成 \u76F8\u673A1，粘给 AI 看几乎不可读。这里统一转成可读中文。
                return JsonSerializer.Serialize(d.RootElement, JOpts);
            }
            catch { return "{\"Nodes\":[],\"Connections\":[]}"; }
        }

        // ---------------- 3.2 提示词 ----------------

        /// <summary>
        /// 生成流程级提示词：当前流程 JSON（上下文）+ 可用名称 + 输出契约 + 数量要求 + 我的需求。
        /// </summary>
        public static string BuildFlowPrompt(FlowItem? flow, string? requirements)
        {
            var reqs = (requirements ?? "")
                       .Replace("\r\n", "\n")
                       .Split('\n')
                       .Select(r => r.Trim())
                       .Where(r => r.Length > 0)
                       .ToList();

            // 没有当前流程时按「运控流程」给契约，这是最常用也最容易跑起来的一类。
            var kind = flow?.Kind ?? FlowKind.Table;

            var sb = new StringBuilder();
            sb.AppendLine("你是无代码运动控制软件的**流程**生成助手。请根据我的需求，生成一个可直接导入的**流程 JSON**。");
            sb.AppendLine();
            sb.AppendLine("【当前流程】" + (flow == null
                ? "（尚未选中流程，请新生成一个）"
                : $"{flow.Name}（类型：{KindText(kind)}，角色：{RoleText(flow.Role)}）"));
            sb.AppendLine();

            if (reqs.Count > 0)
            {
                sb.AppendLine("【我的需求】");
                for (int i = 0; i < reqs.Count; i++) sb.AppendLine((i + 1) + ". " + reqs[i]);
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("【我的需求】（请在下面补一句需求，例如「改成 3 工位循环、每件拍照一次、超时就报警」；");
                sb.AppendLine(" 不想改就把这两行删掉，AI 会按当前流程的用途补全成一套完整流程）");
                sb.AppendLine();
            }

            // 可用名称：这是让 AI 不瞎编设备名的关键
            var names = BuildNameContextText(ProjectStore.Data);
            if (names.Length > 0)
            {
                sb.AppendLine("【本工程已配置的名称 —— 只能使用这些，不要自己编】");
                sb.Append(names);
                sb.AppendLine();
            }

            if (flow != null)
            {
                sb.AppendLine("【当前流程的 JSON】（请在此基础上按需求改写；保持既有风格与命名）");
                sb.AppendLine(ExportFlowJson(flow));
                sb.AppendLine();
            }

            sb.AppendLine("【输出格式】严格按下面的结构输出，只输出 JSON，不要解释、不要 Markdown 代码块：");
            sb.AppendLine(FlowSchemaText(kind));
            sb.AppendLine();

            // 脚本流程必须把「真实存在的 API」摊开告诉 AI：早期提示词只在硬性要求里提了一句
            // Log.Info，结果 AI 顺手写 Camera.Grab / Vision.Match，沙箱里没这些函数，一跑就报 nil。
            if (kind == FlowKind.Lua)
            {
                sb.AppendLine(BuildLuaApiText());
            }

            sb.AppendLine("【硬性要求】");
            sb.AppendLine("1. 所有名称（轴名、IO 名、气缸名、点位表名、变量名、相机名）必须用上面【本工程已配置的名称】里的原文，不要用英文、不要造新名字。");
            sb.AppendLine("2. 「功能」只能用：" + string.Join(" / ", LegalFunctions) + "。");
            sb.AppendLine("3. 「逻辑」只能用：" + string.Join(" / ", LegalLogic) + "；普通步骤写 \"就\"。");
            sb.AppendLine("4. 「超时」只能用：" + string.Join(" / ", LegalTimeout) + "；不确定就写 \"空\"。");
            sb.AppendLine("5. 只输出 JSON，不要任何解释文字，不要 ```json 代码块标记。");
            sb.AppendLine("6. 用不到的分类填空数组 []，不要删除分类。");
            sb.AppendLine("7. 引用到的轴 / IO / 气缸名必须与【本工程已配置的名称】完全一致，大小写也要一致。");
            sb.AppendLine("8. 步骤要足够详细：回零 → 移动到点位 → 等待输入 → 输出动作 → 气缸伸出/缩回 → 延时 → 计数，按真实工艺流程编排。");
            if (kind == FlowKind.Table)
                sb.AppendLine("9. 运控流程请生成至少 10 个步骤，每个步骤写齐 功能/对象/动作/值 四项。");
            else if (kind == FlowKind.Lua)
                sb.AppendLine("9. 脚本流程的 Lua 源码至少 10 行，含 Log.Info / Variable.Get / Variable.Set / if / return，并且必须能直接运行：只能用上面【Lua 可用的 API】里的函数，不要写 Camera.Grab / Vision.Match / File.Read 这类沙箱里不存在的调用。");
            else if (kind == FlowKind.Vision)
                sb.AppendLine("9. 视觉流程不要写 \"脚本\"（沙箱里没有 Camera / Vision 的 Lua API），改用 \"视觉步骤\" 数组：至少 3 个步骤，覆盖 图像采集 + 模板匹配（或缺陷检测）。");
            else
                sb.AppendLine("9. 节点图必须从 \"开始\" 节点出发、以 \"结束\" 节点收尾，节点之间用 Connections 串起来，Id 用唯一字符串。");
            sb.AppendLine("10. 顶层直接给一个流程对象（含 名称/类型/角色 + 内容），或者用 {\"流程\":[ ... ]} 包一层，两种都认。");

            if (kind == FlowKind.NodeGraph)
            {
                sb.AppendLine();
                sb.AppendLine("【节点图可用节点类型】");
                sb.Append(BuildNodeCatalogText());
            }

            return sb.ToString();
        }

        /// <summary>按流程类型给出对应的输出契约样例。</summary>
        private static string FlowSchemaText(FlowKind kind) => kind switch
        {
            FlowKind.Lua => """
{
  "名称": "主流程",
  "类型": "脚本",
  "角色": "主流程",
  "脚本": "-- Lua 源码，整段写成一行 JSON 字符串（内部换行用 \\n）\nlocal n = Variable.Get(\"计数\") or 0\nn = n + 1\nVariable.Set(\"计数\", n)\nLog.Info(\"第 \" .. n .. \" 件完成\")\nif n >= 100 then\n  Log.Warn(\"已达批次上限\")\n  return false\nend\nreturn true"
}
""",

            FlowKind.Vision => """
{
  "名称": "视觉检测",
  "类型": "视觉",
  "角色": "主流程",
  "视觉步骤": [
    { "名称": "拍照1", "类型": "图像采集", "启用": true, "来源": "相机", "相机": "顶视相机", "曝光ms": 20, "宽": 1920, "高": 1080 },
    { "名称": "定位1", "类型": "模板匹配", "启用": true, "模板路径": "Templates/A.ncc", "匹配分": 0.8, "角度范围": 360, "匹配模式": "灰度匹配" },
    { "名称": "检测1", "类型": "缺陷检测", "启用": true, "算法": "NCC", "检测模式": "阈值面积", "最小面积": 100, "最大面积": 100000, "阈值": 128 }
  ]
}
""",

            FlowKind.NodeGraph => """
{
  "名称": "节点图流程",
  "类型": "节点图",
  "角色": "主流程",
  "节点图": {
    "Nodes": [
      { "Id": "n1", "Kind": "Start", "X": 80, "Y": 200, "Props": [] },
      { "Id": "n2", "Kind": "MoveAxis", "X": 320, "Y": 200, "Props": [ { "Name": "轴", "Value": "X轴" }, { "Name": "目标位置", "Value": "100" } ] },
      { "Id": "n3", "Kind": "End", "X": 560, "Y": 200, "Props": [] }
    ],
    "Connections": [
      { "SourceId": "n1", "SourcePort": "Out", "TargetId": "n2" },
      { "SourceId": "n2", "SourcePort": "Out", "TargetId": "n3" }
    ]
  }
}
""",

            _ => """
{
  "名称": "主流程",
  "类型": "运控",
  "角色": "主流程",
  "步骤": [
    { "逻辑": "就", "功能": "轴",   "对象": "X轴",      "动作": "回零",   "值": "" },
    { "逻辑": "就", "功能": "轴",   "对象": "X轴",      "动作": "移动",   "值": "100" },
    { "逻辑": "就", "功能": "输入", "对象": "来料检测", "动作": "等待",   "值": "1" },
    { "逻辑": "就", "功能": "气缸", "对象": "夹爪缸",   "动作": "伸出",   "值": "" },
    { "逻辑": "就", "功能": "延时", "对象": "",         "动作": "延时",   "值": "300" },
    { "逻辑": "就", "功能": "气缸", "对象": "夹爪缸",   "动作": "缩回",   "值": "" },
    { "逻辑": "就", "功能": "输出", "对象": "完成",     "动作": "置位",   "值": "1" },
    { "逻辑": "就", "功能": "变量", "对象": "计数",     "动作": "加",     "值": "1" },
    { "逻辑": "就", "功能": "输出", "对象": "完成",     "动作": "复位",   "值": "0" },
    { "逻辑": "就", "功能": "轴",   "对象": "X轴",      "动作": "移动",   "值": "0" }
  ]
}
"""
        };

        /// <summary>
        /// Lua 侧【真实存在】的 API 清单，直接写进提示词喂给 AI。
        ///
        /// 背景：早期提示词只在硬性要求里写「含 Log.Info / Variable.Get」，从没把可用清单摊开，
        /// AI 于是照惯例顺手写了 Camera.Grab / Vision.Match / File.Read —— 这些在 MoonSharp
        /// 沙箱里根本没注册，生成的脚本一运行就是 attempt to call a nil value。
        ///
        /// 轴/IO/气缸/通讯/料盘/硬件 的签名与说明直接取自 <see cref="NoCodeMotion.Editing.LuaApi.HardwareList"/>：
        /// 那份清单同时是编辑器智能提示与 HardwareApi.Register 的来源，这里不复制文本，
        /// 以后新增硬件函数只要改 LuaApi 一处，提示词自动跟上，不会漂移。
        /// </summary>
        private static string BuildLuaApiText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("【Lua 可用的 API —— 只能调用下面这些，其它函数一律不存在（写了会报 attempt to call a nil value）】");

            foreach (var g in LuaApiGroups)
            {
                sb.AppendLine();
                sb.AppendLine("■ " + g.Group + "（全局函数，直接写函数名调用）");
                foreach (var name in g.Names)
                {
                    var sym = NoCodeMotion.Editing.LuaApi.HardwareList.FirstOrDefault(s => s.Name == name);
                    sb.AppendLine(sym == null
                        ? "- " + name + "(...)"
                        : "- " + sym.Signature + "　—— " + sym.Description);
                }
            }

            sb.AppendLine();
            sb.AppendLine("■ 命名空间函数（写成「表.函数」）");
            sb.AppendLine("- Variable.Get(变量名)　—— 读工程变量：是数字就返回 number，否则返回 string，变量不存在返回 nil");
            sb.AppendLine("- Variable.Set(变量名, 值)　—— 写工程变量（变量不存在会自动新建，值一律按字符串存回变量表）");
            sb.AppendLine("- IO.Get(输入名)　—— 读输入点，返回字符串 \"0\" 或 \"1\"（注意是字符串，比较要写 == \"1\"）");
            sb.AppendLine("- IO.Set(输出名, 值)　—— 写输出点，值 0 / 1");
            sb.AppendLine("- Axis.MoveAbs(轴名, 位置, 速度)　—— 先设速度再绝对定位（三个参数都要写）");
            sb.AppendLine("- Axis.MoveRel(轴名, 距离)　—— 相对当前位置移动");
            sb.AppendLine("- Axis.Home(轴名) / Axis.Stop(轴名) / Axis.WaitDone(轴名)　—— 回零 / 停止 / 阻塞等待到位");
            sb.AppendLine("- Axis.SetSpeed(轴名, 速度) / Axis.Enable(轴名)　—— 设置速度 / 使能");
            sb.AppendLine("- Cylinder.Out(气缸名) / Cylinder.Back(气缸名) / Cylinder.Reset(气缸名)　—— 伸出 / 缩回 / 复位");

            sb.AppendLine();
            sb.AppendLine("■ 日志与流程控制");
            sb.AppendLine("- print(...) / Print(...) / Log.Info(...)　—— 三者等价，输出到软件下方的「输出」面板");
            sb.AppendLine("- Log.Warn(...) / Log.Error(...) / Log.Debug(...)　—— 带 [警告] / [错误] / [调试] 前缀的日志");
            sb.AppendLine("- Delay(毫秒) / WaitStep(毫秒)　—— 阻塞延时，自动夹到 0~60000 毫秒");
            sb.AppendLine("- EStop()　—— 返回 true 表示已按下急停；脚本要自己判断并 return false，否则后续动作照发");

            sb.AppendLine();
            sb.AppendLine("■ 标准库（MoonSharp 完整 Lua 5.2 标准库）");
            sb.AppendLine("- string / table / math / os / io / coroutine / json（json.parse、json.serialize）");

            sb.AppendLine();
            sb.AppendLine("■ 沙箱里【没有】的 API（写了就报错，绝对不要用）");
            sb.AppendLine("- 没有 Camera / Vision：视觉流程不要用 Lua 写，改用「视觉步骤」数组（类型取 图像采集 / 模板匹配 / 图像预处理 / 缺陷检测 / 测量 / 通讯）");
            sb.AppendLine("- 没有 File / File.Read / File.Write 之类的文件读写函数");
            sb.AppendLine("- 没有调用其它流程 / 子流程的函数（要复用请把逻辑抄进同一段 Lua）");
            sb.AppendLine("- Log 只有 Info / Output / Warn / Error / Debug 五个成员，其它（Log.Success / Log.Table …）不存在");
            return sb.ToString();
        }

        /// <summary>Lua API 清单的分组顺序（组名 → LuaApi.HardwareList 里的函数名）。</summary>
        private static readonly (string Group, string[] Names)[] LuaApiGroups =
        {
            ("轴", new[] { "AxisMove", "SetAxisSpeed", "AxisHome", "StopAxis", "WaitAxisDone", "EnableAxis", "MoveAxisRel", "MoveAxisAbs" }),
            ("输入 / 输出 IO", new[] { "ReadIO", "WaitIO", "SetIO", "ToggleIO" }),
            ("气缸", new[] { "CylinderMove", "WaitCylinder", "CylinderReset" }),
            ("通讯", new[] { "CommSend", "CommRecv" }),
            ("料盘", new[] { "TrayPick", "TrayPlace" }),
            ("硬件状态与模式", new[] { "HardwareStatus", "HardwareReady", "HardwareReconnect", "UseRealHardware", "UseSimulation" })
        };

        /// <summary>把工程里已配置的各类名称列成文本，喂给 AI 以免它编造设备名。</summary>
        private static string BuildNameContextText(ProjectData? d)
        {
            if (d == null) return string.Empty;
            var sb = new StringBuilder();

            // 空类别也要显式写出来：早期版本直接 return 省略，AI 看不到「气缸一个都没配」，
            // 就自己编了个「夹爪缸」；粘回来时「名称」下拉框里没有这个对象，单元格只能显示空白。
            void Line(string label, IEnumerable<string> names)
            {
                var list = names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
                sb.AppendLine(list.Count == 0
                    ? "- " + label + "：（工程里未配置，请不要引用这一类）"
                    : "- " + label + "：" + string.Join("、", list));
            }

            Line("轴", d.Axes.Select(a => a.Name));
            Line("输入", d.Inputs.Select(i => i.Name));
            Line("输出", d.Outputs.Select(i => i.Name));
            Line("气缸", d.Cylinders.Select(c => c.Name));
            Line("点位表", d.PointTables.Select(t => t.Name));
            Line("变量", d.Variables.SelectMany(v => v.Names()));
            Line("相机", d.Cameras.Select(c => c.Name));
            Line("通讯", d.Comms.Select(c => c.Name));
            return sb.ToString();
        }

        /// <summary>节点图节点清单（直接由 NgNodeDefinitions.All 生成，新增节点类型时提示词自动跟上）。</summary>
        private static string BuildNodeCatalogText()
        {
            var sb = new StringBuilder();
            foreach (var kv in NgNodeDefinitions.All)
            {
                var def = kv.Value;
                sb.Append("- ").Append(kv.Key).Append("（").Append(def.Title).Append("，")
                  .Append(def.Domain == NgDomain.Vision ? "视觉" : def.Domain == NgDomain.Comm ? "通讯" : "运控").Append("）");
                if (def.Props.Count > 0)
                {
                    sb.Append(" 属性：");
                    sb.Append(string.Join("、", def.Props.Select(p =>
                        p.Options != null ? $"{p.Name}({p.Options})" : $"{p.Name}（默认 {p.Default}）")));
                }
                if (!def.HasInput) sb.Append("  [入口节点，无输入]");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        // ---------------- 3.3 导入 ----------------

        /// <summary>
        /// 解析出「流程对象」列表并交给 body 处理：
        /// 兼容裸流程对象 / 流程数组 / {"流程":[...]} 外层包装 / ```json 代码围栏。
        /// 解析失败或没识别到流程时直接返回可读的错误文案（此时 body 不会被调用）。
        /// </summary>
        private static string ReadFlowItems(string? json, Func<List<JsonElement>, string> body)
        {
            if (string.IsNullOrWhiteSpace(json)) return "内容为空。请先复制 AI 返回的流程 JSON。";

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(StripCodeFence(json.Trim()), new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });
            }
            catch (JsonException ex)
            {
                return "JSON 解析失败：" + ex.Message + "（请复制 AI 返回的完整 JSON，不要只复制一部分）";
            }

            using (doc)
            {
                var root = doc.RootElement;
                var items = new List<JsonElement>();

                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var e in root.EnumerateArray())
                        if (e.ValueKind == JsonValueKind.Object) items.Add(e);
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    // 兼容 {"流程":[...]} 外层包装；没有包装时根对象本身就是一个流程
                    bool wrapped = false;
                    foreach (var alias in new[] { "流程", "flows", "flow", "流程列表" })
                    {
                        if (root.TryGetProperty(alias, out var arr) && arr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var e in arr.EnumerateArray())
                                if (e.ValueKind == JsonValueKind.Object) items.Add(e);
                            wrapped = true;
                            break;
                        }
                    }
                    if (!wrapped) items.Add(root);
                }
                else
                {
                    return "内容不是一个 JSON 对象或数组。请复制 AI 返回的完整 JSON（以 { 或 [ 开头）。";
                }

                if (items.Count == 0) return "没识别到流程内容。请确认复制的是 AI 返回的完整流程 JSON。";

                return body(items);
            }
        }

        /// <summary>
        /// 解析 AI 返回的流程 JSON 并写入。
        /// · AI 只给 1 个流程且当前有选中流程 → **覆盖当前流程**（保留原名称，避免悄悄改名）；
        /// · 其它情况（给了多个流程 / 没有选中流程）→ 追加为新流程。
        /// 返回人类可读的结果摘要。
        /// </summary>
        public static string ApplyFlowGenerated(FlowItem? target, IList<FlowItem>? sink, string json)
        {
            if (sink == null) return "目标流程列表为空。";

            return ReadFlowItems(json, items =>
            {
                // 单个流程 + 当前有选中流程 → 覆盖当前流程（保留原名称）
                if (items.Count == 1 && target != null)
                {
                    FillFlow(target, items[0], keepName: true);
                    var h = UnknownObjectsHint(target);
                    return $"已覆盖流程「{target.Name}」：" + DescribeFlow(target)
                         + (h.Length > 0 ? "；" + h : "");
                }

                var added = new List<string>();
                var hints = new List<string>();
                foreach (var e in items)
                {
                    var f = new FlowItem();
                    FillFlow(f, e);
                    if (string.IsNullOrWhiteSpace(f.Name)) f.Name = "AI流程";
                    f.Name = UniqueFlowName(sink, f.Name);
                    sink.Add(f);
                    added.Add(f.Name + "（" + DescribeFlow(f) + "）");

                    var h = UnknownObjectsHint(f);
                    if (h.Length > 0 && !hints.Contains(h)) hints.Add(h);
                }
                return $"已新增 {added.Count} 个流程：" + string.Join("；", added)
                     + (hints.Count > 0 ? "；" + string.Join("；", hints) : "");
            });
        }

        // ---------------- 3.3 粘贴生成的快照 / 回退 ----------------

        /// <summary>
        /// 把整个流程列表拍成一份可回退的快照（JSON 数组，每个元素就是一个流程对象）。
        ///
        /// 与「粘贴生成」共用同一套导出格式，所以「快照 → 导回」是无损的；
        /// 用导出/导入做快照而不是手写深拷贝：以后 FlowItem 加字段时，只要 ExportSteps /
        /// ExportVisualSteps 跟着更新，快照自动跟上，不会悄悄丢内容。
        /// </summary>
        public static string SnapshotFlows(IEnumerable<FlowItem>? flows)
        {
            if (flows == null) return "[]";
            var sb = new StringBuilder("[");
            bool first = true;
            foreach (var f in flows)
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(ExportFlowJson(f));
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// 把 <see cref="SnapshotFlows"/> 拍下的快照导回：先清空 sink，再按顺序重建每个流程。
        /// 返回是否恢复成功。
        ///
        /// 关键：**快照解析失败时返回 false 且完全不动 sink**，避免出现「原来的流程被清空了、
        /// 新的又没导回来」这种最糟的结果。空快照 "[]"（粘贴前工程里一个流程都没有）是合法目标，
        /// 会清空 sink 并返回 true。
        /// </summary>
        public static bool RestoreFlows(IList<FlowItem>? sink, string? snapshotJson)
        {
            if (sink == null || string.IsNullOrWhiteSpace(snapshotJson)) return false;

            var text = StripCodeFence(snapshotJson.Trim());

            // 空快照：粘贴前一个流程都没有 —— 合法的恢复目标就是「清空」
            if (text == "[]")
            {
                foreach (var item in sink.ToList()) sink.Remove(item);
                return true;
            }

            // 先把快照整段解析成流程对象，全部成功才动 sink
            var rebuilt = new List<FlowItem>();
            _ = ReadFlowItems(text, items =>
            {
                foreach (var e in items)
                {
                    var f = new FlowItem();
                    FillFlow(f, e);
                    if (string.IsNullOrWhiteSpace(f.Name)) f.Name = "流程";
                    rebuilt.Add(f);
                }
                return string.Empty;
            });
            if (rebuilt.Count == 0) return false;

            foreach (var item in sink.ToList()) sink.Remove(item);
            foreach (var f in rebuilt) sink.Add(f);
            return true;
        }

        /// <summary>
        /// 【粘贴生成弹窗】用：把待导入的 JSON 解析成一句人类可读的预览（识别到几个流程、各是什么类型多少内容）。
        /// 解析失败时 count = 0，返回值是给用户看的错误原因。**只读，不改动任何工程数据。**
        /// </summary>
        public static string PreviewFlowJson(string? json, out int count)
        {
            int n = 0;
            var text = ReadFlowItems(json, items =>
            {
                n = items.Count;
                var parts = new List<string>();
                foreach (var e in items)
                {
                    var f = new FlowItem();
                    FillFlow(f, e);
                    var nm = string.IsNullOrWhiteSpace(f.Name) ? "未命名" : f.Name;
                    var h = UnknownObjectsHint(f);
                    parts.Add(nm + "（" + DescribeFlowBrief(f) + "）"
                              + (h.Length > 0 ? "\n⚠ " + h : ""));
                }
                return parts.Count == 1
                    ? "识别到 1 个流程：" + parts[0]
                    : "识别到 " + parts.Count + " 个流程：" + string.Join("；", parts);
            });
            count = n;
            return text;
        }

        /// <summary>
        /// 预览用的一句话内容摘要（自带类型前缀，避免出现「脚本 / 脚本 2 行」这种重复）。
        /// 内容是空的就直说「空…」，免得用户在弹窗里看到「运控 0 个步骤」还以为是程序出错。
        /// </summary>
        private static string DescribeFlowBrief(FlowItem f)
        {
            switch (f.Kind)
            {
                case FlowKind.Lua:
                    return string.IsNullOrWhiteSpace(f.LuaSource) ? "空脚本" : $"脚本 {CountLines(f.LuaSource)} 行";
                case FlowKind.Vision:
                    return f.VisualSteps.Count == 0 ? "空视觉流程" : $"视觉 {f.VisualSteps.Count} 个步骤";
                case FlowKind.NodeGraph:
                    return string.IsNullOrWhiteSpace(f.GraphJson) ? "空节点图" : "节点图 " + DescribeGraph(f.GraphJson);
                default:
                    return f.Steps.Count == 0 ? "空运控流程" : $"运控 {f.Steps.Count} 个步骤";
            }
        }

        /// <summary>预览：这段文本里有几个流程对象（0 = 没识别到 / 解析失败）。走同一套 ReadFlowItems，判定口径不会漂。</summary>
        public static int CountFlowsInJson(string json)
        {
            int n = 0;
            ReadFlowItems(json, items => { n = items.Count; return string.Empty; });
            return n;
        }

        /// <summary>
        /// 找出运控步骤「对象」列里引用了工程中并不存在的设备名（轴 / IO / 气缸 / 通讯 / 变量 / 点位）。
        /// 这类值会被原样存进流程，但「名称」下拉框只列工程里真实存在的对象，
        /// 所以单元格会显示为空白——不是数据丢了，是这一项在工程里没有对应物。
        /// 判断依据取 ProjectStore.Data（真实工程数据），而不是 Catalog 那个只供下拉用的名称缓存。
        /// </summary>
        private static List<string> UnknownObjects(FlowItem f)
        {
            var bad = new List<string>();
            var d = ProjectStore.Data;
            if (d == null || f.Steps.Count == 0) return bad;

            static HashSet<string> Set(IEnumerable<string> ns) =>
                new HashSet<string>(ns.Where(n => !string.IsNullOrWhiteSpace(n)), StringComparer.Ordinal);

            var axes   = Set(d.Axes.Select(a => a.Name));
            var ios    = Set(d.Inputs.Select(i => i.Name).Concat(d.Outputs.Select(i => i.Name)));
            var cyls   = Set(d.Cylinders.Select(c => c.Name));
            var comms  = Set(d.Comms.Select(c => c.Name));
            var vars   = Set(d.Variables.SelectMany(v => v.Names()));
            var points = Set(d.PointTables.SelectMany(t => t.Points).Select(p => p.Name));

            foreach (var s in f.Steps)
            {
                // 空对象不用管；「注释」行的对象是自由文本，写什么都可以，不参与校验
                if (string.IsNullOrWhiteSpace(s.Name) || s.Logic == "注释") continue;

                HashSet<string>? legal = s.Function switch
                {
                    "轴" => axes,
                    "IO" => ios,
                    "气缸" => cyls,
                    "modbus" => comms,
                    "变量" => vars,
                    "点位" => points,
                    _ => null            // 系统 / 相机 等不需要引用具体对象
                };
                if (legal == null) continue;
                if (!legal.Contains(s.Name)) bad.Add(s.Function + "[" + s.Name + "]");
            }
            return bad.Distinct().ToList();
        }

        /// <summary>把「引用了不存在的对象」拼成一句提醒；没有问题就返回空串（调用方自行决定分隔符）。</summary>
        private static string UnknownObjectsHint(FlowItem f)
        {
            var bad = UnknownObjects(f);
            if (bad.Count == 0) return string.Empty;
            var head = string.Join("、", bad.Take(5));
            if (bad.Count > 5) head += " 等 " + bad.Count + " 处";
            return "注意：" + head + " 在工程里不存在，「名称」列会显示空白，请先配置该对象或在表格里改选";
        }

        /// <summary>把 AI 返回的一个流程对象写进 FlowItem（集合一律原地清空重填，保证界面绑定不断）。</summary>
        private static void FillFlow(FlowItem f, JsonElement e, bool keepName = false)
        {
            var name = Str(e, "名称", "name");
            // keepName：覆盖当前流程时保留原名称。名称是流程在左侧列表里的身份，
            // 被 AI 悄悄改掉用户就找不到自己刚改的流程了；新增流程时仍用 AI 给的名字。
            if (!keepName && !string.IsNullOrWhiteSpace(name)) f.Name = name!;

            var roleStr = Str(e, "角色", "role") ?? "主流程";
            f.Role = roleStr.Contains("复位") || roleStr.Equals("Reset", StringComparison.OrdinalIgnoreCase)
                   ? FlowRole.Reset : FlowRole.Main;

            var kindStr = Str(e, "类型", "kind", "type") ?? "";
            var lua = Str(e, "脚本", "源码", "luaSource", "script", "source");

            FlowKind fk;
            if (!string.IsNullOrWhiteSpace(kindStr)) fk = NormFlowKind(kindStr);
            else if (TryGet(e, out _, "节点图", "graph", "nodeGraph")) fk = FlowKind.NodeGraph;
            else if (TryGet(e, out _, "视觉步骤", "视觉", "visualSteps")) fk = FlowKind.Vision;
            else if (!string.IsNullOrWhiteSpace(lua)) fk = FlowKind.Lua;
            else fk = FlowKind.Table;

            // 声明是运控但「步骤」为空却给了「脚本」→ 当脚本流程存，别把内容丢了
            if (fk == FlowKind.Table && !TryGet(e, out _, "步骤", "steps", "step")
                && !string.IsNullOrWhiteSpace(lua))
                fk = FlowKind.Lua;

            f.Kind = fk;

            switch (fk)
            {
                case FlowKind.Lua:
                    f.LuaSource = lua ?? string.Empty;
                    break;
                case FlowKind.NodeGraph:
                    f.GraphJson = ReadGraphJson(e);
                    break;
                case FlowKind.Vision:
                    FillVisualSteps(f, e);
                    break;
                default:
                    FillSteps(f, e);
                    break;
            }
        }

        /// <summary>运控流程：把「步骤」写进 f.Steps。</summary>
        private static void FillSteps(FlowItem f, JsonElement e)
        {
            f.Steps.Clear();
            foreach (var s in Items(e, "步骤", "steps", "step"))
            {
                var func = NormFunction(Str(s, "功能", "function") ?? "轴");
                var target = Str(s, "对象", "name", "target", "名称") ?? "";
                var action = Str(s, "动作", "operation", "op") ?? "";
                var val = Str(s, "值", "value", "setValue", "参数") ?? "";

                string prop, op;
                var propIn = Str(s, "属性", "property");
                var opIn = Str(s, "运算", "calc", "calcOp");

                if (!string.IsNullOrWhiteSpace(propIn) || !string.IsNullOrWhiteSpace(opIn))
                {
                    // 显式给了「属性 / 运算」（导出的 JSON 就是这种）→ 保留原值，只把非法值夹回合法值
                    prop = ClampProperty(func, propIn, action, val);
                    op = ClampOperation(func, opIn, prop);
                }
                else
                {
                    // 只给了「动作」→ 按功能映射成软件模板里实际在用的 (属性, 运算, 值)
                    var m = MapAction(func, action, val);
                    prop = m.Property;
                    op = m.Operation;
                    val = m.Value;
                }

                f.Steps.Add(new FlowStep
                {
                    // 名称列必须用「对象」（轴名/IO名/气缸名/点位表名/变量名）
                    Name = target,
                    Logic = ClampTo(Str(s, "逻辑", "logic"), LegalLogic, "就"),
                    Function = func,
                    Property = prop,
                    Operation = op,
                    SetValue = val,
                    Timeout = ClampTo(Str(s, "超时", "timeout"), LegalTimeout, "空"),
                    DurationMs = Int(s, "耗时", "durationMs")
                });
            }
        }

        /// <summary>视觉流程：把「视觉步骤」写进 f.VisualSteps。</summary>
        private static void FillVisualSteps(FlowItem f, JsonElement e)
        {
            f.VisualSteps.Clear();
            foreach (var s in Items(e, "视觉步骤", "视觉", "visualSteps", "steps", "step"))
            {
                var st = new VisualFlowStep
                {
                    Name = Str(s, "名称", "name") ?? "",
                    StepType = NormalizeStepType(Str(s, "类型", "stepType", "type")),
                    Enabled = Str(s, "启用", "enabled") != "0",
                };

                st.CameraId = Str(s, "相机", "cameraId") ?? st.CameraId;
                st.SavePath = Str(s, "保存路径", "savePath") ?? st.SavePath;
                st.SourceType = Str(s, "来源", "sourceType") ?? st.SourceType;
                st.ExposureMs = DblDef(s, st.ExposureMs, "曝光ms", "曝光", "exposureMs");
                st.Width = IntDef(s, st.Width, "宽", "width");
                st.Height = IntDef(s, st.Height, "高", "height");
                st.FolderPath = Str(s, "文件夹", "folderPath") ?? st.FolderPath;

                st.TemplatePath = Str(s, "模板路径", "templatePath") ?? st.TemplatePath;
                st.ScoreThreshold = DblDef(s, st.ScoreThreshold, "匹配分", "阈值分", "scoreThreshold");
                st.AngleRange = DblDef(s, st.AngleRange, "角度范围", "angleRange");
                st.MatchMode = Str(s, "匹配模式", "matchMode") ?? st.MatchMode;
                st.TemplateRoiX = IntDef(s, st.TemplateRoiX, "模板框X", "templateRoiX");
                st.TemplateRoiY = IntDef(s, st.TemplateRoiY, "模板框Y", "templateRoiY");
                st.TemplateRoiW = IntDef(s, st.TemplateRoiW, "模板框宽", "templateRoiW");
                st.TemplateRoiH = IntDef(s, st.TemplateRoiH, "模板框高", "templateRoiH");

                st.Algorithm = Str(s, "算法", "algorithm") ?? st.Algorithm;
                st.MinArea = DblDef(s, st.MinArea, "最小面积", "minArea");
                st.MaxArea = DblDef(s, st.MaxArea, "最大面积", "maxArea");
                st.Threshold = DblDef(s, st.Threshold, "阈值", "threshold");
                st.DetectMode = Str(s, "检测模式", "detectMode") ?? st.DetectMode;

                st.MeasureMode = Str(s, "测量模式", "measureMode") ?? st.MeasureMode;
                st.Calibration = DblDef(s, st.Calibration, "标定", "calibration");
                st.Unit = Str(s, "单位", "unit") ?? st.Unit;

                st.Protocol = Str(s, "协议", "protocol") ?? st.Protocol;
                st.Target = Str(s, "目标", "target") ?? st.Target;
                st.Content = Str(s, "内容", "content") ?? st.Content;

                st.PreOp = Str(s, "预处理", "preOp") ?? st.PreOp;
                st.PreParam1 = DblDef(s, st.PreParam1, "参数1", "preParam1");
                st.PreParam2 = DblDef(s, st.PreParam2, "参数2", "preParam2");
                st.PreRoi = Str(s, "预处理ROI", "preRoi") ?? st.PreRoi;
                st.PreImage2Path = Str(s, "第二张图", "preImage2Path") ?? st.PreImage2Path;

                f.VisualSteps.Add(st);
            }
        }

        /// <summary>节点图流程：读「节点图」并序列化回 GraphJson（NgDoc 自己的格式）。</summary>
        private static string ReadGraphJson(JsonElement e)
        {
            if (!TryGet(e, out var g, "节点图", "graph", "nodeGraph")) return string.Empty;
            if (g.ValueKind == JsonValueKind.String) return g.GetString() ?? string.Empty;
            if (g.ValueKind != JsonValueKind.Object) return string.Empty;

            // 交给 NgDoc 反序列化再序列化：FromJson 内部会 Normalize（按节点定义补齐/剔除属性），
            // 所以 AI 写错属性名时会自动回落到默认值，而不是把脏数据存进工程。
            return NgDoc.FromJson(g.GetRawText()).ToJson();
        }

        /// <summary>视觉步骤类型归一（AI 可能写英文/近义词）。</summary>
        private static string NormalizeStepType(string? t)
        {
            if (string.IsNullOrWhiteSpace(t)) return "图像采集";
            if (t.Contains("采集") || t.Contains("拍照") || t.Contains("相机")) return "图像采集";
            if (t.Contains("匹配") || t.Contains("定位") || t.Contains("Match")) return "模板匹配";
            if (t.Contains("预处理") || t.Contains("滤波") || t.Contains("增强")) return "图像预处理";
            if (t.Contains("缺陷") || t.Contains("检测") || t.Contains("Defect")) return "缺陷检测";
            if (t.Contains("测量") || t.Contains("Measure")) return "测量";
            if (t.Contains("通讯") || t.Contains("通信") || t.Contains("Comm")) return "通讯";
            return "图像采集";
        }

        // ---------------- 3.4 取值归一化 / 辅助 ----------------

        /// <summary>把 AI 的自由文本夹到合法取值里；不在列表内就用默认值。</summary>
        private static string ClampTo(string? v, string[] legal, string def)
        {
            if (string.IsNullOrWhiteSpace(v)) return def;
            var s = v.Trim();
            foreach (var l in legal) if (l == s) return l;
            // 英文/近义词的粗略兜底
            foreach (var l in legal) if (s.Contains(l) || l.Contains(s)) return l;
            return def;
        }

        /// <summary>「属性」夹取：合法就用；不合法时把该值当动作词再映射一次，仍不行就取该功能的默认属性。</summary>
        private static string ClampProperty(string func, string? given, string action, string val)
        {
            var legal = LegalProperties.TryGetValue(func, out var p) ? p : new[] { "速度" };
            if (!string.IsNullOrWhiteSpace(given))
            {
                foreach (var l in legal) if (l == given.Trim()) return l;
                var mapped = MapAction(func, given, val).Property;
                foreach (var l in legal) if (l == mapped) return l;
            }
            var byAction = MapAction(func, action, val).Property;
            foreach (var l in legal) if (l == byAction) return l;
            return legal[0];
        }

        /// <summary>「运算」夹取：合法就用；否则按 (功能, 属性) 取默认运算。</summary>
        private static string ClampOperation(string func, string? given, string prop)
        {
            var legal = LegalOperations.TryGetValue(func, out var o) ? o : GeneralOperations;
            if (!string.IsNullOrWhiteSpace(given))
                foreach (var l in legal) if (l == given.Trim()) return l;
            return DefaultOperation(func, prop);
        }

        /// <summary>(功能, 属性) → 默认运算，取值与内置模板/执行器一致。</summary>
        private static string DefaultOperation(string func, string prop)
        {
            switch (func)
            {
                case "轴":
                    if (prop == "已回零") return "HomeAxis";
                    if (prop == "速度") return "SetAxisSpeed";
                    return "MoveAxisAbs";
                case "IO":
                    if (prop == "输入状态") return "ReadInput";
                    if (prop == "脉冲状态") return "Wait";
                    return "WriteOutput";
                case "气缸":
                    return "CylinderMove";
                case "modbus":
                    return "CommSend";
                default:
                    return "修改";
            }
        }

        /// <summary>
        /// 把 AI 写的「动作」映射成软件模板实际使用的 (属性, 运算, 值) 三元组。
        /// 映射目标严格取自 FlowViewModel.GetTemplateSteps 里内置模板用过的组合，
        /// 保证生成的流程与软件自带模板同构、能真正跑起来。
        /// </summary>
        private static (string Property, string Operation, string Value) MapAction(string func, string? action, string? val)
        {
            string a = (action ?? string.Empty).Trim();
            string v = val ?? string.Empty;

            bool Has(params string[] keys)
            {
                foreach (var k in keys)
                    if (a.Contains(k, StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }

            switch (func)
            {
                case "轴":
                    if (Has("回零", "原点", "Home")) return ("已回零", "HomeAxis", "");
                    if (Has("等待", "到位", "Wait")) return ("已回零", "WaitAxisStop", "");
                    if (Has("速度", "Speed")) return ("速度", "SetAxisSpeed", v);
                    return ("位置", "MoveAxisAbs", v);

                case "IO":
                    if (Has("等待", "Wait")) return ("脉冲状态", "Wait", v);
                    if (Has("读", "检测", "输入", "Read")) return ("输入状态", "ReadInput", v);
                    if (Has("复位", "关闭", "Reset", "OFF"))
                        return ("输出状态", "WriteOutput", string.IsNullOrWhiteSpace(v) ? "0" : v);
                    if (Has("置位", "打开", "Set", "ON"))
                        return ("输出状态", "WriteOutput", string.IsNullOrWhiteSpace(v) ? "1" : v);
                    return ("输出状态", "WriteOutput", v);

                case "气缸":
                    if (Has("复位", "Reset")) return ("电磁阀", "CylinderReset", "");
                    if (Has("缩回", "退回", "Retract")) return ("电磁阀", "CylinderMove", "0");
                    if (Has("伸出", "推出", "Extend")) return ("电磁阀", "CylinderMove", "1");
                    // 没写动作时按「值」判断：0 = 缩回，其余 = 伸出
                    return ("电磁阀", "CylinderMove", v == "0" ? "0" : "1");

                case "modbus":
                    return ("寄存器值", "CommSend", v);

                case "变量":
                    return ("数值", ClampTo(a, GeneralOperations, "修改"), v);

                case "点位":
                    return ("是否等待到位", "等于", string.IsNullOrWhiteSpace(v) ? "1" : v);

                case "系统":
                case "延时":
                    // 执行器对「系统」不驱动任何硬件，只是表格里的一行；用合法取值保证单元格可见。
                    return ("运行时间", "等于", v);

                case "相机":
                    return ("数值", "修改", v);

                default:
                    return ("位置", "MoveAxisAbs", v);
            }
        }

        /// <summary>流程类型 → 中文名（与工程级交换的「类型」字段同一套词）。</summary>
        private static string KindText(FlowKind k) => k switch
        {
            FlowKind.Lua => "脚本",
            FlowKind.Vision => "视觉",
            FlowKind.NodeGraph => "节点图",
            _ => "运控",
        };

        private static string RoleText(FlowRole r) => r == FlowRole.Reset ? "复位流程" : "主流程";

        /// <summary>一句话描述流程内容，用于结果摘要。</summary>
        private static string DescribeFlow(FlowItem f) => f.Kind switch
        {
            FlowKind.Lua => $"脚本 {CountLines(f.LuaSource)} 行",
            FlowKind.Vision => $"{f.VisualSteps.Count} 个视觉步骤",
            FlowKind.NodeGraph => DescribeGraph(f.GraphJson),
            _ => $"{f.Steps.Count} 个步骤",
        };

        private static string DescribeGraph(string? graphJson)
        {
            try
            {
                var d = NgDoc.FromJson(graphJson);
                return $"{d.Nodes.Count} 个节点 / {d.Connections.Count} 条连线";
            }
            catch { return "节点图"; }
        }

        private static int CountLines(string? s) =>
            string.IsNullOrEmpty(s) ? 0 : s.Replace("\r\n", "\n").Split('\n').Length;

        /// <summary>在已有流程里取一个不重名的名称（重名会干扰流程页的「修改/删除」定位）。</summary>
        private static string UniqueFlowName(IList<FlowItem> sink, string name)
        {
            bool Taken(string n)
            {
                foreach (var f in sink)
                    if (string.Equals(f.Name, n, StringComparison.Ordinal)) return true;
                return false;
            }

            if (!Taken(name)) return name;
            for (int i = 2; i < 1000; i++)
            {
                var cand = name + i;
                if (!Taken(cand)) return cand;
            }
            return name;
        }

        // 用 UnsafeRelaxedJsonEscaping：默认编码器会把中文写成 \u8FD0\u63A7，而这段 JSON 是要粘进 AI 对话给人看的。
        // 与 ProjectJsonAnnotator / UserStore 的既有约定保持一致。
        private static readonly JsonSerializerOptions JOpts = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>JSON 字符串转义（借 JsonSerializer 拿标准转义，省得手写 \" \n）。</summary>
        private static string J(string? s) => JsonSerializer.Serialize(s ?? string.Empty, JOpts);

        /// <summary>数值转 JSON（InvariantCulture，避免区域设置把小数点写成逗号）。</summary>
        private static string JN(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);

        private static string JN(int v) => v.ToString(CultureInfo.InvariantCulture);
    }
}
// ◇作者保留所有权利　请勿删除※⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓⁣
