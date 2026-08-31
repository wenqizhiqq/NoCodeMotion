// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// =====================================================================
// 工程 JSON 注释注入器。
//
// 目的：把工程保存成【带中文注释的 JSON（JSONC）】，用户可以直接把
//   %LocalAppData%\NoCodeMotion\Projects\工程名.json 丢给 AI（豆包等），
//   AI 读到注释就知道每个字段的含义与合法取值，能更准确地帮用户改配置。
//
// 实现思路（避免手写几十个字段）：
//   1) 先用 JsonSerializer 正常序列化成【无注释】的纯 JSON
//   2) 用 JsonDocument 解析它
//   3) 用 Utf8JsonWriter 原样重写，遇到「已知属性名」时先 WriteCommentValue
//
// 这样注释是按属性名自动插入的，新增模型字段时只要在下面两个字典里
// 补一条即可，不必改动重写逻辑。
//
// 注意：写出的 JSON 含 /*...*/ 块注释，属于 JSONC。
//   读取时必须用 ReadCommentHandling = JsonCommentHandling.Skip，
//   ProjectManager / ProjectStore 的所有 Deserialize 都已加此选项。
// =====================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    public static class ProjectJsonAnnotator
    {
        /// <summary>文件头注释：告诉 AI 这个文件是什么、怎么改。</summary>
        private const string HeaderComment =
@"
 ============================================================
  NoCodeMotion 工程配置文件（JSONC，含中文注释）
 ============================================================
  本文件是一个「无代码运动控制工程」的完整配置，可安全手工编辑。

  【给 AI 的修改指引】
  1. 所有 name 字段是「引用键」：轴/IO/气缸/相机/通讯/流程的步骤都通过
     name 互相引用。改名字时必须同步改掉所有引用它的地方，否则运行时
     会报「找不到轴：XXX」。
  2. 枚举字段只能取注释里列出的值，写别的软件不认。
  3. 流程有三类（FlowItem.Kind）：
       0 = 运控   —— 用 Steps 数组，逐行表格步骤（轴/IO/气缸/延时/变量…）
       1 = 脚本   —— 用 LuaSource 字段，Lua 源码
       2 = 视觉   —— 用 LuaSource 字段，Lua 源码（调相机/视觉算子）
  4. 点位表(PointTables)的 AxisNames 是 4 个槽位，对应每个点位 Positions 的
     4 组坐标；槽位留空字符串表示该槽不使用。
  5. 修改后请保持 JSON 语法合法（可以用注释，但不能有多余逗号）。
 ============================================================
";

        /// <summary>顶层字段注释（属性名 → 注释）。改模型字段时同步这里。</summary>
        private static readonly Dictionary<string, string> TopLevelComments = new()
        {
            ["Controllers"] =
                "【控制器/控制卡】硬件入口。\n" +
                "  BusType 总线: 脉冲 | EtherCAT | Modbus | CAN | 虚拟\n" +
                "  Connection 连接: PCI | 网口 | 串口 | USB\n" +
                "  CardNo 卡号: 同类型控制器从 0 递增，不重复\n" +
                "  AxisCount 轴数: 该控制器带几个轴",
            ["Axes"] =
                "【轴】AxisNo 轴号在同一 Controller 内从 0 递增、不重复。\n" +
                "  AxisType 轴类型: 脉冲 | EtherCAT | Modbus | 虚拟\n" +
                "  Unit 单位: mm | deg | um | inch\n" +
                "  Speed/Accel/Decel: 速度/加速度/减速度，单位 Unit/秒",
            ["Inputs"] =
                "【数字量输入】传感器/按钮信号。\n" +
                "  Function 功能: 启动按钮 | 停止按钮 | 复位按钮 | 急停 | 手自动 | 原点 | 正限位 | 负限位 | 动点\n" +
                "  Level 电平: 取反 | 不取反（常开/常闭）\n" +
                "  CardNo/ModuleNo/Sequence: 卡号/模块号/位号，决定硬件地址",
            ["Outputs"] =
                "【数字量输出】指示灯/电磁阀/触发信号。\n" +
                "  Function 功能: 运行 | 就绪 | 报警 | 完成 | 暂停 | 动点\n" +
                "  Level 电平: 取反 | 不取反\n" +
                "  CardNo/ModuleNo/Sequence: 卡号/模块号/位号",
            ["Cylinders"] =
                "【气缸】InitialState 初始状态 / CurrentState 当前状态: 伸出 | 缩回。\n" +
                "  OutPoint 输出点: 控制电磁阀的输出点名（如 Y0）\n" +
                "  SensorExtend/SensorRetract: 伸出到位/缩回到位 感应输入点名（如 X0/X1）\n" +
                "  Type 类型: 单作用 | 双作用",
            ["Cameras"] =
                "【相机】IpAddress 是 GigE 相机的 IP；Port 默认 8000。\n" +
                "  Width/Height: 分辨率宽/高；ExposureMs 曝光毫秒；Gain 增益\n" +
                "  视觉流程的 Lua 里用 Camera.Grab(\"相机名\") 按 Name 取图",
            ["Comms"] =
                "【通讯】串口 / 网口 / Modbus。\n" +
                "  CommType 类型: 串口 | Modbus主站 | Modbus从站 | TCP | UDP\n" +
                "  PortOrIp: 串口写 COM1，网口写 IP；BaudOrPort: 串口写波特率，网口写端口",
            ["Trays"] =
                "【料盘】行列式排布（料盘取放料用）。Rows/Cols 行数/列数，PitchX/PitchY 行列间距",
            ["PointTables"] =
                "【工位/点位表】一个工位 = 一组轴 + 若干点位。\n" +
                "  AxisNames: 4 个槽位，依次填 轴名（留空字符串表示不用该槽）\n" +
                "  Points[].Positions: 也是 4 组，与 AxisNames 槽位一一对应（位置 + 速度）",
            ["Flows"] =
                "【流程】Kind 类型: 0=运控(表格步骤) | 1=脚本(Lua) | 2=视觉(Lua)\n" +
                "  Role 角色: 0=主流程 | 1=复位流程\n" +
                "  Kind=0 用 Steps 数组；Kind=1/2 用 LuaSource 字段\n" +
                "  Steps[].Function: 轴 | IO | 气缸 | modbus | 点位 | 变量 | 系统\n" +
                "  Steps[].Property: 位置 | 回零 | 速度 | 输出 | 复位 | 伸出 | 缩回 | 延时\n" +
                "  Steps[].Operation: 等于 | 加 | 减 | 乘 | 除 | 置位 | 复位 | 伸出 | 缩回 | 等待 | 回零\n" +
                "  Steps[].Logic: 如果 | 就 | 否则\n" +
                "  【注意】Function 不支持「调用子流程」，需要调用请改用 Kind=1 脚本流程",
            ["Variables"] =
                "【变量表】每行 5 组（Name1/Value1 ~ Name5/Value5），值一律是字符串。\n" +
                "  流程步骤 Function=变量 时按 Name 引用；Lua 里用 Variable.Get/Set(\"变量名\")",
            ["RequirementsText"] =
                "【需求文本】用户在项目管理页填写的原始需求（多行），AI 生成配置的输入依据",
            ["Remark"] = "【工程备注】简短说明，显示在工程列表",
            ["CreatedAt"] = "【创建时间】首次保存时写入",
            ["UpdatedAt"] = "【修改时间】每次保存自动更新（手工改此值无效）",
        };

        /// <summary>
        /// 序列化成带中文注释的 JSONC 文本。
        /// 先正常序列化 → JsonDocument 解析 → Utf8JsonWriter 重写并注入注释。
        /// </summary>
        public static string SerializeWithComments(ProjectData data)
        {
            // 用 UnsafeRelaxedJsonEscaping 让中文/特殊字符原样输出（不被转成 \uXXXX），
            // 方便人工与 AI 直接阅读、修改。
            var relaxed = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            var plain = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = relaxed
            });
            using var doc = JsonDocument.Parse(plain);

            using var ms = new MemoryStream();
            using (var w = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true, Encoder = relaxed }))
            {
                w.WriteCommentValue(HeaderComment);
                WriteElement(w, doc.RootElement, null);
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static void WriteElement(Utf8JsonWriter w, JsonElement el, string? propName)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    w.WriteStartObject();
                    foreach (var p in el.EnumerateObject())
                    {
                        w.WritePropertyName(p.Name);
                        // 顶层字段：属性名后、值之前插入注释
                        if (propName == null && TopLevelComments.TryGetValue(p.Name, out var c))
                            w.WriteCommentValue(c);
                        WriteElement(w, p.Value, p.Name);
                    }
                    w.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    w.WriteStartArray();
                    foreach (var item in el.EnumerateArray())
                        WriteElement(w, item, propName);
                    w.WriteEndArray();
                    break;

                case JsonValueKind.String:
                    w.WriteStringValue(el.GetString());
                    break;
                case JsonValueKind.Number:
                    // TryGetInt64 / TryGetDouble 覆盖了所有正常数值；
                    // 极端情况（超大数、科学计数法）用 WriteRawValue 原样写出，
                    // 避免精度丢失——WriteNumberValue 没有 string 重载。
                    if (el.TryGetInt64(out var l)) w.WriteNumberValue(l);
                    else if (el.TryGetDouble(out var d)) w.WriteNumberValue(d);
                    else w.WriteRawValue(el.GetRawText());
                    break;
                case JsonValueKind.True:
                    w.WriteBooleanValue(true);
                    break;
                case JsonValueKind.False:
                    w.WriteBooleanValue(false);
                    break;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    w.WriteNullValue();
                    break;
                default:
                    w.WriteNullValue();
                    break;
            }
        }

        /// <summary>读取带注释的工程 JSON（统一入口，各读取点都调它）。</summary>
        private static readonly JsonSerializerOptions ReadOptions = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public static ProjectData? Deserialize(string json)
            => JsonSerializer.Deserialize<ProjectData>(json, ReadOptions);
    }
}
// ◇作者保留所有权利　请勿删除※⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓⁣
