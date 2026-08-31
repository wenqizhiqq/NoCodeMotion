// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// =====================================================================
// AI 工程交换服务（项目管理页「复制 / 粘贴」按钮的后端）。
//
// 工作流：
//   1) 用户在右侧详情填「备注」+「需求」
//   2) 点【复制需求】→ 本服务生成一段提示词（含 JSON 契约）进剪贴板
//   3) 用户粘贴到 WorkBuddy / 任意 AI，AI 返回工程配置 JSON
//   4) 用户复制 AI 返回的 JSON，回本页点【粘贴生成】
//   5) 本服务容错解析并写入 ProjectStore.Data（轴/IO/气缸/流程/点位/通讯/变量）
//
// 解析原则：**逐条容错**。任何一条数据字段缺失/类型不对只跳过该条，
// 不抛异常中断整体导入，保证 AI 输出不完美时仍能拿到可用配置。
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

        /// <summary>按工程名 / 备注 / 需求列表生成给 AI 的提示词（含严格 JSON 契约）。</summary>
        public static string BuildPrompt(string projectName, string? remark, IEnumerable<string>? requirements)
        {
            var reqs = (requirements ?? Enumerable.Empty<string>())
                       .Where(r => !string.IsNullOrWhiteSpace(r))
                       .Select(r => r.Trim())
                       .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("# 任务：为无代码运动控制软件生成工程配置");
            sb.AppendLine();
            sb.AppendLine($"工程名称：{projectName}");
            if (!string.IsNullOrWhiteSpace(remark))
                sb.AppendLine($"工程备注：{remark.Trim()}");
            sb.AppendLine();

            if (reqs.Count > 0)
            {
                sb.AppendLine("## 用户需求");
                for (int i = 0; i < reqs.Count; i++)
                    sb.AppendLine($"{i + 1}. {reqs[i]}");
                sb.AppendLine();
            }

            sb.AppendLine("## 输出要求");
            sb.AppendLine("只输出一个 JSON 对象，不要任何解释文字、不要 Markdown 代码块围栏。");
            sb.AppendLine("JSON 结构如下（数组都可为空数组，字段缺失则用默认值）：");
            sb.AppendLine();
            sb.AppendLine(SchemaText);
            sb.AppendLine();
            sb.AppendLine("## 字段说明与取值");
            sb.AppendLine("- controllers：运动控制卡。busType 取 脉冲/EtherCAT/Modbus；connection 取 PCI/网口/串口。");
            sb.AppendLine("- axes：轴。axisType 取 脉冲/EtherCAT；unit 取 mm/°；axisNo 从 0 开始、同一控制器内不重复。");
            sb.AppendLine("- inputs/outputs：IO 点。level 固定 取反。sequence 为位号，从 0 开始。");
            sb.AppendLine("- cylinders：气缸。type 取 双作用/单作用；initialState 取 伸出/缩回。");
            sb.AppendLine("- flows：流程。kind 取 Table/Lua/Vision；role 取 Main/Reset。");
            sb.AppendLine("- flows[].steps：步骤。function 取 轴/IO/气缸/点位/变量/modbus/系统；");
            sb.AppendLine("  operation 取 等于/修改/加/减/复位/等待；timeout 取 空/等待3秒就统计/不停机。");
            sb.AppendLine("- pointTables：工位（点位表）。axes 为 4 个轴名（不足补空串）。");
            sb.AppendLine("- comms：通讯。commType 取 串口/网口TCP/网口UDP/ModbusTCP/ModbusRTU。");
            sb.AppendLine();
            sb.AppendLine("请确保流程步骤引用的轴名 / IO 名 / 气缸名 与上面 axes、inputs、outputs、cylinders 中的 name 完全一致。");
            return sb.ToString();
        }

        /// <summary>JSON 契约模板（同时用于提示词与文档）。</summary>
        private const string SchemaText = """
{
  "controllers": [
    { "name": "控制卡1", "vendor": "雷赛", "cardType": "DMC5400", "cardNo": 0, "axisCount": 4, "busType": "脉冲", "connection": "PCI" }
  ],
  "axes": [
    { "name": "X", "controller": "控制卡1", "axisType": "脉冲", "axisNo": 0, "unit": "mm", "speed": 100, "accel": 50, "decel": 50 }
  ],
  "inputs": [
    { "name": "启动", "function": "启动按钮", "controller": "控制卡1", "cardNo": 0, "moduleNo": 0, "sequence": 0 }
  ],
  "outputs": [
    { "name": "运行", "function": "动点", "controller": "控制卡1", "cardNo": 0, "moduleNo": 0, "sequence": 0 }
  ],
  "cylinders": [
    { "name": "推料", "deviceId": "推料", "outPoint": "Y0", "sensorExtend": "X0", "sensorRetract": "X1", "type": "双作用", "initialState": "缩回" }
  ],
  "flows": [
    {
      "name": "主流程",
      "kind": "Table",
      "role": "Main",
      "steps": [
        { "logic": "就", "function": "轴", "name": "X", "property": "位置", "operation": "等于", "setValue": "100", "timeout": "空", "durationMs": 500 }
      ]
    }
  ],
  "pointTables": [
    {
      "name": "工位1",
      "axes": ["X", "Y", "Z", ""],
      "points": [
        { "name": "取料位", "x": 100, "y": 50, "z": 0, "r": 0 }
      ]
    }
  ],
  "comms": [
    { "name": "Modbus主站", "commType": "ModbusRTU", "portOrIp": "COM1", "baudOrPort": 9600, "parity": "无", "dataBits": 8, "stopBits": 1, "timeoutMs": 1000 }
  ],
  "variables": [
    { "name": "计数", "value": "0" }
  ]
}
""";

        // ==================== 2. 解析并应用（粘贴按钮用） ====================

        /// <summary>
        /// 容错解析 AI 返回的 JSON 并写入目标 ProjectData。
        /// 返回人类可读的结果摘要（各分类成功导入条数 + 跳过原因）。
        /// </summary>
        public static string ApplyGenerated(ProjectData data, string json)
        {
            if (data == null) return "目标工程数据为空。";
            if (string.IsNullOrWhiteSpace(json)) return "剪贴板内容为空。";

            // 容忍 AI 用 ```json ... ``` 包裹
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
                return "JSON 解析失败：" + ex.Message;
            }

            using (doc)
            {
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return "内容不是一个 JSON 对象，请复制 AI 返回的完整 JSON。";

                var log = new List<string>();
                int n;

                n = ApplyControllers(data, root); if (n > 0) log.Add($"控制器 {n}");
                n = ApplyAxes(data, root); if (n > 0) log.Add($"轴 {n}");
                n = ApplyIo(data, root, "inputs", isInput: true); if (n > 0) log.Add($"输入 {n}");
                n = ApplyIo(data, root, "outputs", isInput: false); if (n > 0) log.Add($"输出 {n}");
                n = ApplyCylinders(data, root); if (n > 0) log.Add($"气缸 {n}");
                n = ApplyComms(data, root); if (n > 0) log.Add($"通讯 {n}");
                n = ApplyPointTables(data, root); if (n > 0) log.Add($"点位表 {n}");
                n = ApplyFlows(data, root); if (n > 0) log.Add($"流程 {n}");
                n = ApplyVariables(data, root); if (n > 0) log.Add($"变量 {n}");

                data.EnsurePointTables();
                Catalog.SyncAllFromData(data);

                return log.Count == 0
                    ? "未识别到任何可导入的配置，请检查 JSON 是否包含 controllers/axes/inputs/outputs/cylinders/flows 等字段。"
                    : "已生成：" + string.Join("、", log);
            }
        }

        /// <summary>去掉 AI 常用的 ```json 代码围栏。</summary>
        private static string StripCodeFence(string text)
        {
            if (!text.StartsWith("```")) return text;
            var firstNl = text.IndexOf('\n');
            if (firstNl < 0) return text;
            // 去掉首行（```json 之类）
            var body = text.Substring(firstNl + 1);
            var end = body.LastIndexOf("```", StringComparison.Ordinal);
            if (end >= 0) body = body.Substring(0, end);
            return body.Trim();
        }

        // ---------------- 各分类导入 ----------------

        private static int ApplyControllers(ProjectData d, JsonElement root)
        {
            int n = 0;
            foreach (var e in Items(root, "controllers"))
            {
                var name = Str(e, "name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                d.Controllers.Add(new AxisControllerItem
                {
                    Name = name,
                    Vendor = Str(e, "vendor") ?? "雷赛",
                    CardType = Str(e, "cardType") ?? "",
                    CardNo = Int(e, "cardNo"),
                    AxisCount = Int(e, "axisCount", 4),
                    BusType = Str(e, "busType") ?? "脉冲",
                    Connection = Str(e, "connection") ?? "PCI"
                });
                n++;
            }
            return n;
        }

        private static int ApplyAxes(ProjectData d, JsonElement root)
        {
            int n = 0;
            foreach (var e in Items(root, "axes"))
            {
                var name = Str(e, "name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                d.Axes.Add(new AxisItem
                {
                    Name = name,
                    Controller = Str(e, "controller") ?? "",
                    AxisType = Str(e, "axisType") ?? "脉冲",
                    AxisNo = Int(e, "axisNo"),
                    Unit = Str(e, "unit") ?? "mm",
                    Speed = Dbl(e, "speed", 100),
                    Accel = Dbl(e, "accel", 50),
                    Decel = Dbl(e, "decel", 50)
                });
                n++;
            }
            return n;
        }

        private static int ApplyIo(ProjectData d, JsonElement root, string key, bool isInput)
        {
            int n = 0;
            foreach (var e in Items(root, key))
            {
                var name = Str(e, "name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                var io = new IoItem
                {
                    Name = name,
                    Function = Str(e, "function") ?? (isInput ? "动点" : "动点"),
                    Controller = Str(e, "controller") ?? "",
                    CardNo = Int(e, "cardNo"),
                    ModuleNo = Int(e, "moduleNo"),
                    Sequence = Int(e, "sequence"),
                    Level = Str(e, "level") ?? "取反"
                };
                if (isInput) d.Inputs.Add(io); else d.Outputs.Add(io);
                n++;
            }
            return n;
        }

        private static int ApplyCylinders(ProjectData d, JsonElement root)
        {
            int n = 0;
            foreach (var e in Items(root, "cylinders"))
            {
                var name = Str(e, "name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                var init = Str(e, "initialState") ?? "缩回";
                if (init != "伸出" && init != "缩回") init = "缩回";
                d.Cylinders.Add(new CylinderItem
                {
                    Name = name,
                    DeviceId = Str(e, "deviceId") ?? name,
                    OutPoint = Str(e, "outPoint") ?? "",
                    SensorExtend = Str(e, "sensorExtend") ?? "",
                    SensorRetract = Str(e, "sensorRetract") ?? "",
                    Type = Str(e, "type") ?? "双作用",
                    InitialState = init,
                    CurrentState = init
                });
                n++;
            }
            return n;
        }

        private static int ApplyComms(ProjectData d, JsonElement root)
        {
            int n = 0;
            foreach (var e in Items(root, "comms"))
            {
                var name = Str(e, "name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                d.Comms.Add(new CommItem
                {
                    Name = name,
                    CommType = Str(e, "commType") ?? "串口",
                    PortOrIp = Str(e, "portOrIp") ?? "COM1",
                    BaudOrPort = Int(e, "baudOrPort", 9600),
                    Parity = Str(e, "parity") ?? "无",
                    DataBits = Int(e, "dataBits", 8),
                    StopBits = Dbl(e, "stopBits", 1),
                    TimeoutMs = Int(e, "timeoutMs", 1000)
                });
                n++;
            }
            return n;
        }

        private static int ApplyPointTables(ProjectData d, JsonElement root)
        {
            int n = 0;
            foreach (var e in Items(root, "pointTables"))
            {
                var tname = Str(e, "name");
                if (string.IsNullOrWhiteSpace(tname)) continue;

                var t = new PointTable { Name = tname };
                // axes: ["X","Y","Z",""]
                if (e.TryGetProperty("axes", out var ax) && ax.ValueKind == JsonValueKind.Array)
                {
                    int i = 0;
                    foreach (var a in ax.EnumerateArray())
                    {
                        if (i >= PointTable.SlotCount) break;
                        t.AxisNames[i++] = a.ValueKind == JsonValueKind.String ? (a.GetString() ?? "") : "";
                    }
                }
                foreach (var p in Items(e, "points"))
                {
                    var pname = Str(p, "name");
                    if (string.IsNullOrWhiteSpace(pname)) continue;
                    var item = new PointItem { Name = pname };
                    item.Positions[0] = new PointAxis { Position = Dbl(p, "x"), Speed = 100 };
                    item.Positions[1] = new PointAxis { Position = Dbl(p, "y"), Speed = 100 };
                    item.Positions[2] = new PointAxis { Position = Dbl(p, "z"), Speed = 100 };
                    item.Positions[3] = new PointAxis { Position = Dbl(p, "r"), Speed = 100 };
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
            foreach (var e in Items(root, "flows"))
            {
                var name = Str(e, "name");
                if (string.IsNullOrWhiteSpace(name)) continue;

                var kind = Str(e, "kind") ?? "Table";
                var fk = kind.Equals("Lua", StringComparison.OrdinalIgnoreCase) ? FlowKind.Lua
                       : kind.Equals("Vision", StringComparison.OrdinalIgnoreCase) ? FlowKind.Vision
                       : FlowKind.Table;

                var role = Str(e, "role") ?? "Main";
                var fr = role.Equals("Reset", StringComparison.OrdinalIgnoreCase) ? FlowRole.Reset
                       : FlowRole.Main;

                var f = new FlowItem { Name = name, Kind = fk, Role = fr };

                if (fk == FlowKind.Lua)
                {
                    f.LuaSource = Str(e, "luaSource") ?? "";
                }
                else
                {
                    foreach (var s in Items(e, "steps"))
                    {
                        // 名称字段：AI 常写 name / target 都可接受
                        var target = Str(s, "name") ?? Str(s, "target") ?? "";
                        f.Steps.Add(new FlowStep
                        {
                            Logic = Str(s, "logic") ?? "就",
                            Function = Str(s, "function") ?? "轴",
                            // Property 槽位承载"对谁操作"（轴名/IO名/气缸名）
                            Property = Str(s, "property") ?? "位置",
                            Operation = Str(s, "operation") ?? "等于",
                            SetValue = Str(s, "setValue") ?? target,
                            Timeout = Str(s, "timeout") ?? "空",
                            DurationMs = Int(s, "durationMs")
                        });
                    }
                }

                d.Flows.Add(f);
                n++;
            }
            return n;
        }

        private static int ApplyVariables(ProjectData d, JsonElement root)
        {
            var pairs = new List<(string name, string value)>();
            foreach (var e in Items(root, "variables"))
            {
                var name = Str(e, "name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                pairs.Add((name, Str(e, "value") ?? "0"));
            }
            if (pairs.Count == 0) return 0;

            // 每行 5 槽位，超出自动换行
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

        // ---------------- 容错读取辅助 ----------------

        /// <summary>取 root[key] 数组元素；缺失或不是数组时返回空序列。</summary>
        private static IEnumerable<JsonElement> Items(JsonElement parent, string key)
        {
            if (parent.ValueKind != JsonValueKind.Object) yield break;
            if (!parent.TryGetProperty(key, out var arr)) yield break;
            if (arr.ValueKind != JsonValueKind.Array) yield break;
            foreach (var e in arr.EnumerateArray())
            {
                if (e.ValueKind == JsonValueKind.Object)
                    yield return e;
            }
        }

        private static string? Str(JsonElement e, string key)
        {
            if (e.ValueKind != JsonValueKind.Object) return null;
            if (!e.TryGetProperty(key, out var v)) return null;
            return v.ValueKind switch
            {
                JsonValueKind.String => v.GetString(),
                JsonValueKind.Number => v.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        private static int Int(JsonElement e, string key, int def = 0)
        {
            if (e.ValueKind != JsonValueKind.Object) return def;
            if (!e.TryGetProperty(key, out var v)) return def;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)) return i;
            if (v.ValueKind == JsonValueKind.String &&
                int.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s)) return s;
            return def;
        }

        private static double Dbl(JsonElement e, string key, double def = 0)
        {
            if (e.ValueKind != JsonValueKind.Object) return def;
            if (!e.TryGetProperty(key, out var v)) return def;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d)) return d;
            if (v.ValueKind == JsonValueKind.String &&
                double.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s)) return s;
            return def;
        }
    }
}
// ◇作者保留所有权利　请勿删除※⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓⁣
