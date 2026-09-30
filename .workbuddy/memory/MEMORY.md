# 项目长期记忆（NoCodeMotion — WPF/.NET 10 无代码运动控制）

## 源码与编码
- ★★ 工程目录所有源文件（.cs/.xaml/.csproj/.xshd）磁盘上带加密包装（前 3 字节 `88 7D 1C`）。Read/Edit 工具与 MSBuild 编译透明解密 → 可直接编辑；但**运行时** `XmlReader.Create(路径)`/`File.ReadAllText` 读到密文报 `Invalid character in the given encoding` → 运行时需要的配置（xshd/json/ini）**一律内联进 .cs 常量**。
- GBK/BOM/CRLF 报 binary → 用 Python 读写（utf-8-sig→归一化→恢复 CRLF/BOM）。
- 作者水印「温启志◆编写◇微信﹕187◆1936◇1399」三处：`Services/AuthorWatermark.cs`、`MainWindow.xaml:4`、`Docs/*.md` 末尾。删 AuthorWatermark.cs 编译失败，AI 不主动删。

## 构建（沙箱铁律）
- PowerShell + 绝对 `C:\Program Files\dotnet\dotnet.exe` 前台构建；**stdout 不回显**，须 `| Out-File C:\tmp\x.txt` 再 Read/Grep。禁写 .ps1。
- 删文件用 `[System.IO.File]::Delete(绝对路径)`（Remove-Item 被拦）。
- obj MarkupCompile.cache 被锁（MSB4018）→ 每次构建换全新 `-p:IntermediateOutputPath=C:\tmp\nocode_intN\ -p:OutDir=C:\tmp\nocode_outN\`；勿用 BaseIntermediateOutputPath（_wpftmp glob 出 CS0102/CS0111）。NuGet targets denied → 加 `--no-restore`。
- 判成败只 grep `error CS`/`error MSB`（中文摘要乱码）。csproj 已设 GenerateAssemblyInfo=false；[assembly:ThemeInfo] 在根 AssemblyInfo.cs。
- EnableDefaultPageItems 把 Views/*.xaml 当 Page → 勿造同 x:Class 探针 XAML。

## xlsx 存储 / 全局 UI
- `XlsxProjectStore` 反射式：根 public 集合→sheet；标量→「项目管理」表；新增子集合要继承 EditorItemBase+set+`ChildSheetNameOverrides`；只读计算属性勿放数据模型。
- DataGrid 行内按钮命令：`{Binding DataContext.XxxPanel.Cmd, RelativeSource={AncestorType=DataGrid}}`；固定候选下拉勿设 IsEditable。
- `App.xaml` 只合 `Resources/AppStyles.xaml`；弹窗只能用 AppStyles 全局键（页面级 Resources 键顶层取不到）。色彩：红=破坏/橙=反向/蓝=正向/绿=保存/灰=次要。
- **后台线程绝不能改 ObservableCollection**：`Catalog.Set` 已加 Dispatcher 检查；任何名称库刷新/集合重建同样处理。

## 节点图（NgRunner / NodeGraphPage）
- 数据驱动：ItemsControl+DataTemplate，禁 Children.Add；端口 Tag="OUT"/"IN"（名取 `Label`）；连线命中 Tag="CONN"；拓扑变更后必须 `SyncRunnerTopology()`。
- 条件分支：端口 条件1..8+否则，`NgPortStateViewModel` 定时刷「符合/不符合」。
- 断点（2026-09-27）：**唯一真源 = 页面 VM `_runner.Breakpoints`，并同步到 `FlowRunStore.SetBreakpoints`**；后台运行线程（RunOneFlowNodeGraph）新建的独立 NgRunner 从共享态取断点 + **`BreakOnHit=false`**（后台无「继续」入口，遇断点只写 `Report.TriggeredBreakpointId`→`FlowRunStore.SetTriggeredBreakpoint`→卡片「触发断点」，不暂停防卡死）；页面自己「运行一次」BreakOnHit=true 可暂停。卡片角标：左上「运行到此节点」(IsCurrent)、右上 Tag="NODE_BP" 可点击切换断点（画布 PreviewMouseLeftButtonDown 处理，存在断点/触发断点两态）。
- VM/服务不要构造时捕获 `HardwareBridge.Current`，执行时实时读。

## 控制卡 + 仿真
- 桥接 `WenQiZhiCardBridge`；模式 CardFamilies/Leadshine/Simulation。硬件读数必须在后台线程。模拟卡：IsSimulation、设速只认 SetCardAxisTProfile、使能 active-low、无相对坐标、回零前 EnsureSimReady。BepuPhysics v2：SolveDescription(1,1)、SpringSettings(30f,1f)。
- ★★ **寻址一律 0 基**：`AxisItem.AxisNo` = 卡轴号（→ `IAxis.AxisID`）；`IoItem.Sequence` = 卡上位号（`MainBoardBit = ModuleNo × BitsPerModule + Sequence`，雷赛 `BitNo` 同式）。自动生成轴 / IO（`AxisControllerViewModel.GenerateAxisPoints/GenerateIoPoints`）与手工新增（`IoViewModel.MakeNew`）都必须从 0 起 —— 曾按 1..N 生成导致「轴使能设置不了（错位/越界）+ IO 位号整体偏移一位」。
- ★ 卡族层轴 / IO 动作**返回非 0 只记日志不抛**，界面若要报「成功」必须自己**读回真实状态**（见 `AxisRowViewModel.InvokeEnable`），否则现场看到「点了没反应也没报错」。
- ★ 移植卡族的 `*SDK.cs` 里不少函数是「未添加」（直接 throw，如 MCN420SeriesSDK.DmcGetCardAxisIOState）。要读轴 IO 状态字时，先确认该函数实现了没有；没实现就用该卡原生 `YK_*` 单点读接口按 dmc_axis_io_status 位布局自己拼（MCN42Series 已这么做）。
- ★★ **使能是「端口电平」不是逻辑值**：研控 Sevon **低电平有效** —— `YK_set_sevon_config(card, axis, 0)` = 使能、`1` = 不使能（原生声明原文「0低电平；1高电平」）。电平翻转**只在卡族 SDK 包装层做一次**（`MCN420SeriesSDK.NmcSetCardAxisEnable/Disable`），状态字 bit8 判据要同步（读回 0 才是使能，见 `AxisRealization.NormalizeSevon`）。写反的现场症状 = 点使能反而不使能。
- ★★ **脉冲当量必须真正下发到卡**：卡族指令全是 **unit 版**（`YK_vmove`/`YK_pmove`/`YK_set_command_position`），卡内靠「指令位置比率」换算 → 研控叫 `YK_set_command_ratio(card, axis, pulses/unit)`（**不是** `YK_set_axis_out_pulse_mode`，那是 0~15 的脉冲输出模式；DLL 里没有别的 equiv 接口）。当量为 0 时这些指令被卡**直接拒绝、只回一个数字**（如 Jog 8194 / 设零点 20480），而纯脉冲接口照旧「成功」，极难定位。推当量的位置：`WenQiZhiCardBridge.EnsureAxisRatio` + 连接后 `PushAxisRatios`（雷赛层本来就在 `ApplyAxisProfile` 里带当量，卡族层原本漏了）。
- ★ 看到裸错误码（8194 / 20480 之类）先按这个顺序查：**使能 → 脉冲当量 → 轴号(0 基) → 报警/限位 → 卡型号**（`AxisFailHint` 已把五条写进报错文案）。
- ★★ **凡是界面能「写」的，都要能「读」回来并定时刷新**（只有写没有读 = 界面在骗人）。IO 原本只有 `ReadInput`/`WriteOutput`/`ToggleOutput` → 输出表只能显示**界面期望值**，外部改过看不出。已补 `IHardwareBridge.ReadOutput(IoItem)`（三实现齐：卡族 `OutIo.GetCardPortNoOutState` / 扩展 `ExtOut.EGetExpandIOOutBit`；雷赛 `ReadOutBit`/`ReadOutBitBus`；仿真回放 `_ioState`），且**读失败一律 `return io.Value`（不清零不抛）**。`IoPage` 的 250ms 定时器现在 `RefreshLevels()` 同时刷输入+输出；**仿真/卡未就绪时输出读 `SimRuntime.GetOutput`**。
  ⚠️ 刷新采样必须跳过**正在编辑的行**，但 WPF 没有 `DataGrid.GetEditingRow()`（CS1061）→ 用 `grid.IsKeyboardFocusWithin && grid.CurrentCell.IsValid && ReferenceEquals(cell.Item, item)`。
  ⚠️ `IHardwareBridge` 默认**不加**接口（要同步 3 个实现类）；**例外**仅限「三边都能真实提供、且实现简单」的通用能力（如 `ReadOutput`），卡族特有知识（Jog/回零点/当量）一律走静态分派服务。

## Lua 编辑器
- xshd 内联在 `Views/LuaEditorView.xaml.cs` 的 `LuaXshdXml` 常量（外部文件被磁盘加密读不了）；xshd 注释不能含 `--`。
- `LuaSemanticColorizer` 只着色 xshd 认不出的自定义变量/函数。智能插入面板：LuaInsertFunc+LuaPickItem。
- ★★ **运行路径零 throw**：配置/执行错误走 `NodeFail`/`Warn`（橙提示+跳过+日志，不 throw）——VS 调试器"引发时中断"会在 throw 处弹窗。新增 Lua 函数同步三处：`HardwareApi.Register`、`Editing/LuaApi.cs` HardwareList、`Docs/lua-manual/index.html`。

## 流程运行架构（铁律，2026-09-27 定版）
- ★★ **运行全在后台 thread+while(true)+1ms；页面一律定时器轮询共享态刷新**。运行器只写 `FlowRunStore`（SetStatus/SetStep/SetCycle/SetProgress/SetNodeResults/SetTriggeredBreakpoint），绝不碰 UI；页面 200~300ms 定时器读共享态刷高亮/状态。
- ★ **循环统一托管 `FlowLoopManager`（静态）**：StartLoop/StopLoop/PauseLoop/ResumeLoop/StopAll/PauseAll/ResumeAll/IsLooping。所有入口（表格页/Lua页/节点图页/操作员页）必须走它，勿自建循环。`StopLoop` 置 ctrl.StopRequested+ResumeEvent.Set，轮内 WaitRound 轮询到即 `runner.Stop()`。
- ★★ **暂停/断点铁律**：① 置 `PauseRequested=true` 必须同时 `ResumeEvent.Reset()`（暂停门靠它阻塞，不 Reset 等于没暂停）；② 断点闸感知恢复用 **`ctrl.ResumeTick`（恢复代数）**——所有「继续」入口（ResumeLoop/ResumeAll/页面 Run 恢复/操作员 Resume）都自增它，等待方记录进入时 tick、变化即恢复；勿用 ResumeEvent.Wait 等"下一次继续"（初始有信号会立即通过）。
- ★ **断点（2026-09-27 全类型落地）**：表格 `FlowStep.Breakpoint`（FlowPage 断点列）→ `FlowExecutor.BreakpointGate`；Lua 编辑器断点 → `_breakpointsByItem` 注册表 → RunOneFlowLua 注入 session + watcher 等 tick → `session.Resume(Run)`；节点图 `runner.BreakOnHit=loop`（循环真暂停/后台单次 flash 防卡死）+ WaitRound Paused 处理；视觉 `VisualFlowStep.Breakpoint` + `VisionEngine.Run(..., breakpointGate)`。挂起状态=FlowStatus.Breakpoint（页面显示「触发断点」）；恢复入口=流程页「运行」（IsLoopPaused 含 Paused/Breakpoint）/单次由 300ms 定时器映射 IsPaused/操作员「继续」。
- ★ 四类流程统一 `FlowRunnerService.RunOneFlow` 按 Kind 分发；`FlowStatus` 7 种含 Looping/Breakpoint；判定在跑必须 `Running or Looping`（部分场景含 Paused/Breakpoint）；`PushStatuses()` 是共享态→FlowItem.Status 唯一推送口。
- ⚠ 共享 `FlowRunControl` 是整批的：单条流程失败用 `FlowAbortException`，绝不置共享 StopRequested（防"一运行全停"）。
- ⚠ 脚本/后台线程→UI 的回调：第一行封送 Dispatcher 且 try/catch（含判 HasShutdownStarted）；`SimRuntime.Changed` 逐订阅者 try/catch——宿主异常冒回脚本线程会被判脚本报错而停流程。静态监视器（LuaRunMonitor/NodeGraphRunMonitor）订阅/退订必须 Loaded/Unloaded 成对。
- ⚠ `FlowViewModel.Stop()` 会被切流程/选中触发——停循环只能走显式「停止」。
- 循环日志：第 1 轮+每 100 轮一条；轮间 1ms；Lua 脚本循环单独提速（10/100/50ms）已被用户定版，勿再引入统一 FlowRunPace。
- 表格流程：运算列全中文，改词表同步转换器/ProjectTemplateCatalog/AiProjectExchange/执行路径；`TickIntervalMs=30` 只是刷新粒度，`RunTick` 连续推进（上限 500 步），只有延时/等待步骤真等待。

## Git（2026-09-27）
- `.gitignore` 白名单式：`*` + `!*/` + `!*.cs/.xaml/.csproj/...`；目录忽略必须前导 `/` 锚定（`/Native/`、`/_out/`），否则误杀深层源码。staged 改动未提交。
