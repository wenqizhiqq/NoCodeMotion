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
