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
                return d.RootElement.GetRawText();
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
                sb.AppendLine("【我的需求】（尚未填写，请按当前流程的用途合理推断并补全成一套完整流程）");
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
                sb.AppendLine("9. 脚本流程的 Lua 源码至少 10 行，含 Log.Info / Variable.Get / Variable.Set / if / return，并且必须能直接运行。");
            else if (kind == FlowKind.Vision)
                sb.AppendLine("9. 视觉流程至少 3 个步骤，覆盖 图像采集 + 模板匹配（或缺陷检测）。");
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

        /// <summary>把工程里已配置的各类名称列成文本，喂给 AI 以免它编造设备名。</summary>
        private static string BuildNameContextText(ProjectData? d)
        {
            if (d == null) return string.Empty;
            var sb = new StringBuilder();

            void Line(string label, IEnumerable<string> names)
            {
                var list = names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
                if (list.Count == 0) return;
                sb.AppendLine("- " + label + "：" + string.Join("、", list));
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
        /// 解析 AI 返回的流程 JSON 并写入。
        /// · AI 只给 1 个流程且当前有选中流程 → **覆盖当前流程**（保留原名称，避免悄悄改名）；
        /// · 其它情况（给了多个流程 / 没有选中流程）→ 追加为新流程。
        /// 返回人类可读的结果摘要。
        /// </summary>
        public static string ApplyFlowGenerated(FlowItem? target, IList<FlowItem>? sink, string json)
        {
            if (sink == null) return "目标流程列表为空。";
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

                // 单个流程 + 当前有选中流程 → 覆盖当前流程（保留原名称）
                if (items.Count == 1 && target != null)
                {
                    FillFlow(target, items[0]);
                    return $"已覆盖流程「{target.Name}」：" + DescribeFlow(target);
                }

                var added = new List<string>();
                foreach (var e in items)
                {
                    var f = new FlowItem();
                    FillFlow(f, e);
                    if (string.IsNullOrWhiteSpace(f.Name)) f.Name = "AI流程";
                    f.Name = UniqueFlowName(sink, f.Name);
                    sink.Add(f);
                    added.Add(f.Name + "（" + DescribeFlow(f) + "）");
                }
                return $"已新增 {added.Count} 个流程：" + string.Join("；", added);
            }
        }

        /// <summary>把 AI 返回的一个流程对象写进 FlowItem（集合一律原地清空重填，保证界面绑定不断）。</summary>
        private static void FillFlow(FlowItem f, JsonElement e)
        {
            var name = Str(e, "名称", "name");
            if (!string.IsNullOrWhiteSpace(name)) f.Name = name!;

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

            // 交给 NgDoc 反序列化再序列化：顺带补齐缺省 Id、过滤非法连线
            var doc = NgDoc.FromJson(g.GetRawText());
            doc.Normalize();
            return doc.ToJson();
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
                    return prop == "缩回到位" || prop == "伸出到位" ? "CylinderMove" : "CylinderMove";
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

        /// <summary>JSON 字符串转义（借 JsonSerializer 拿标准转义，省得手写 \" \n）。</summary>
        private static string J(string? s) => JsonSerializer.Serialize(s ?? string.Empty);

        /// <summary>数值转 JSON（InvariantCulture，避免区域设置把小数点写成逗号）。</summary>
        private static string JN(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);

        private static string JN(int v) => v.ToString(CultureInfo.InvariantCulture);
