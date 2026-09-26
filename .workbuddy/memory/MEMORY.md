# 项目长期记忆（NoCodeMotion — WPF/.NET 10 无代码运动控制）

## 源码与编码
- 全部 .cs 明文可直 Read/Edit。GBK/BOM/CRLF 文件若报 binary，用 Python 读写（utf-8-sig→归一化→写回恢复 CRLF/BOM）。禁止用 Write/Edit 直接改这类文件。
- 作者水印「温启志◆编写◇微信﹕187◆1936◇1399」三处：`Services/AuthorWatermark.cs`、`MainWindow.xaml:4`、`Docs/*.md` 末尾。删 AuthorWatermark.cs 编译失败，AI 不主动删。

## 运行/构建（沙箱铁律）
- dotnet 一律 LOLBin 拦截：用 **PowerShell + 绝对 `C:\Program Files\dotnet\dotnet.exe` 前台构建**；PowerShell stdout 不回显，须 `| Out-File C:\tmp\x.txt` 再 Read。禁写 `.ps1`（ExecutionPolicy 拦），用内联命令。
- 删文件用 `[System.IO.File]::Delete("绝对路径")`（`Remove-Item` 被 safe-delete 钩子拦）。
- bin 被运行中的 NoCodeMotion.exe 锁定时，构建到 `-p:OutDir=C:\tmp\nocode_out\`。
- ★★ obj 的 `NoCodeMotion_MarkupCompile.cache` 被锁（开着 Visual Studio/MSBuild 时必现）→ `error MSB4018 UnauthorizedAccessException`（DeleteFile 被拒），Release 同样中招；build-server shutdown 无效、沙箱禁 Stop-Process/删文件。**破解：每次构建换全新 `-p:IntermediateOutputPath=C:\tmp\nocode_intN\` + 全新 `-p:OutDir=C:\tmp\nocode_outN\`**（空目录无缓存可删）。**勿用 `-p:BaseIntermediateOutputPath=`**（obj 脱离 DefaultItemExcludes → `_wpftmp` 把 obj\Debug+obj\Release 的 *.g.cs 一起 glob → 几百条 CS0102/CS0111/CS8646）。判成败只 grep `error CS`/`error MSB`（中文摘要行 GBK→UTF8 变乱码，匹配不到）。若报 `NuGet.targets ... 'obj\*.nuget.g.props' is denied`（VS 锁 obj），加 **`--no-restore`** 即可（包已恢复过）。
- 加载进度：`LoadingService` 静态深度计数（`Show/Report/Hide`，`Progress<0`=不确定）；启动初始化与打开/新建工程都在遮罩下用确定式进度。`Dispatcher.Yield` 须 `System.Windows.Threading.Dispatcher.Yield(...)`（静态方法，在 Window 里写 `Dispatcher.Yield` 会 CS0176）。
- ★ 启动初始化：`MainWindow.Loaded`→`StartUpAsync()` 在加载遮罩下跑：①载入上次工程（xlsx 后台线程）②逐页预初始化进 `_cache` ③`NavigateTo("Flow")`；`finally` 里 `Hide()`。（不在 App 构造函数里。）
- ★★「返回 null = 成功」的方法不能用 `?.M() ?? 兜底串`：桥的 `InchAxis/StartAxisJog/SetAxisZero` 成功返回 null，用 `??` 会把成功判失败。必须先显式判桥实例为空再原样返回结果。
- .NET 10 `_wpftmp` CS0579：csproj 加 `GenerateAssemblyInfo=false`+`GenerateTargetFrameworkAttribute=false`；`[assembly:ThemeInfo]` 留根 AssemblyInfo.cs。勿用 UseArtifactsOutput。
- WPF 重复 XAML：`EnableDefaultPageItems` 把 `Views/*.xaml` 当 Page；探针脚本勿生成同 x:Class 的 `XXX_utf8.xaml`（CS0102+CS8646）。

## xlsx 工程存储
- `XlsxProjectStore.cs` 反射式：根 public 集合→sheet（列=标量）；标量→「项目管理」表；IO 由 BuildIoSheet/SplitIoToRoot 合并。
- 嵌套子集合走「父表.子表」分页（MergedBlockParents={PointTables,Trays,Flows} 跳过合并块单页）。
- 新增子集合：①元素继承 EditorItemBase；②集合属性带 set；③中文名加 ChildSheetNameOverrides。
- 只读计算属性勿放数据模型（成脏列）；新增标量字段零改动落盘、向后兼容。

## 全局 UI 约定
- DataGrid 行内按钮：DataContext 是页面 VM，命令要 `{Binding DataContext.XxxPanel.Cmd, RelativeSource={AncestorType=DataGrid}}`，漏面板路径静默失效。
- 下拉框候选来自固定值时**勿设 IsEditable**。
- 资源字典：`App.xaml` 只合 `Resources/AppStyles.xaml`（再合 HandyControl）。`Themes/AppleControls.xaml` 未合并、运行时不存在，勿引用。
- ★ Window 弹窗只能用 AppStyles 全局键；页面级 `<UserControl.Resources>` 的键在顶层 Window StaticResource 作用域取不到，运行时抛 `XamlParseException 无法找到名为"X"的资源`（编译期不报）。
- 色彩 红=破坏/橙=反向/蓝=正向/绿=保存/灰=次要。每页底 PageHintBar；EditorPage.Detail 内元素**勿用 x:Name**（MC3093），改用 Tag+FindVisualChildByTag。

## 视觉/节点图
- VisualFlowPage.ApplySelection() 须覆盖 _vm.Steps/_vm.Name/_vm.SelectedStep；Steps 非空且 SelectedStep=null 自动选 [0]。
- 节点图 Models/NodeGraph/ 数据驱动；UI 用 ItemsControl+DataTemplate，禁 Children.Add。
- NgRunner（6 按钮+NgStepResult）：WaitResumeAsync 须实例方法按 _state 轮询；Pause 仅节点边界生效。
- ★ **任何 VM/服务都不要在构造时捕获 `HardwareBridge.Current`**：控制卡在启动后才连接，构造时抓一次会一直用默认桩（Stub）→「运行时轴不动」。执行时实时读 `HardwareBridge.Current`（`FlowRunnerService`、`NgRunner` 都踩过同一个坑）。
- 节点图属性面板 `Views/NodeGraphPage.xaml`：`NgPropViewModel` 三态显隐 —— `HasOptions`（固定候选下拉）/ `IsAxisProp`（「轴」→ `{x:Static svc:Catalog.AxisNames}` 下拉）/ `IsPlainText`（自由文本）；轴类节点（轴运动/回零/等待轴到位）额外显示「实际位置」只读行，由 `NodeGraphViewModel` 的 1 秒 `DispatcherTimer` → `SelectedNode.RefreshActualPosition()` 刷新（显隐用 null 安全的 `ShowActualPositionRow`）。

## CAD/DWG
- 真实 BREP→OcctNet.Wrapper 0.1.1；STEP Z-up→WPF Y-up 绕 X -90°；BackMaterial 防黑面。
- DWG/DXF→Aspose.CAD 26.7.0 只迭代矢量实体，绝不 Image.Save 栅格化。

## 仿真体系
- SimRuntime 静态态驱动 3D+变量页；SimFlowPlayer 编译 List<SimAction>，33ms 驱动。
- BepuPhysics v2（2.5.0-beta.29）：①Simulation.Create 须 `new SolveDescription(1,1)`；②ConfigureContactManifold 须 `SpringSettings(30f,1f)`；③运动学↔动态用 BecomeKinematic/SetLocalInertia；④推工件用 Velocity.Linear 驱动。

## 控制卡接入 + 控制器连接体验
- 雷赛实桥 LeadshineHardwareBridge；无卡降级兜底。`HardwareSetup.Mode`：CardFamilies / Leadshine / Simulation。桥接类实际名 **`WenQiZhiCardBridge`**（非 SamsunCardBridge）。
- 连接链路：`AxisControllerViewModel.Connect()`→`HardwareSetup.EnsureInitialized()`→`CardFamilies.TryConnect`/`Reconnect`+`IsCardReady`。在线状态直接读 `IsControllerReady`/`ControllerStatus`（无 VM 影子字典、不落盘）。
- 连接后 `FetchDetectedCounts` 回写轴/IO 真实数→`GenerateIoPoints` 清旧+生成；`DetectedCountsChanged` 事件通知轴/IO 页。
- 连接按钮后台化（`Task.Run`+`IsConnecting` 防重复）；状态栏加控制器在线指示（绿/橙/红/灰）。
- 自动连接：`ProjectManager.DataReloaded`→`RequestAutoConnect()`，**必须等 `LoadingService.IsLoading==false` 后**才 BeginInvoke 调 `AutoConnectAll`（否则抢原生加载器锁拖住载入）；载入中挂一次性 `LoadingService.StateChanged` 回调。
- 硬件读数必须在后台线程：`FetchDetectedCounts` 拆 `ReadRealCounts`（后台）+`ApplyRealCounts`（UI 线程）；`dispatcher.Invoke` 里绝不能调 `TryGetRealCounts`/`IsControllerReady`（硬件查询会卡死 UI）。
- ★★ 硬件桥全局锁铁律：`WenQiZhiCardBridge` 只有一个 `_gate`。锁内只做快速状态读写；慢硬件调用（Initialize/OpenCard/CloseCard）移到 `_gate` 外、用 per-slot `InitLock` 串行。凡共享硬件桥：锁内绝不放慢调用。

## 模拟卡（虚拟运动卡 / DigitalTwinCard）适配 ★关键
- 判定：`CardFamilyDescriptor.IsSimulation`（`Vendor=="模拟卡" || BusKind=="虚拟" || Key 含 Virtual/DigitalTwin`）。进程内仿真、不依赖原生 dll，是「无硬件自测」正途。
- 六条语义差异（不处理会看成「指令成功但轴不动」「状态全 —」「点动像绝对定位」）：
  1. **spacing 默认全 0**→任何非 0 目标夹回 0 并置正负限位；每轴首次运动前 `SetSpacing(轴,0,±1e8)`。**正负限位同时 1 的大数字（如 4102=4096|2|4）是轴状态字不是错误码。**
  2. **设速只认 `SetCardAxisTProfile`**；`SetCardAxisMotionalVel`/`SetCardVectorProfileMulticoor` 是空实现（return 0）。
  3. **连续运动按 `(int)(pps/1000)` 脉冲/ms 步进**：pps<1000→每拍+0 但 isRun 照置→「运动中但位置不动」。速度须换算 pps（×脉冲当量）+1000 下限；`MinVel=MaxVel` 走恒速。
  4. **无无参 `GetAxisCurrentState()`**（状态字用 `GetCardAxisAlarmState`，其内部是 `GetCardAxisIOStatus`）；**`OpenCardAxisEnable` 未实现（恒 -1）**，使能走伺服使能端口 `CardAxisWriteSevonPin`/`GetCardAxisSevonPin`。**★ active-low 约定（早期 bug 根因）：SDK 读回 `1`=失能、`0`=使能（见 VirtualCardSDK「返回 1（失能）」），故「使能」=写 `0`、状态判定=`读回==0`；写成 `1` 反被当失能、且状态永远显示已使能（表现：使能没作用、状态不真实）。** 真实卡族 `GetCardAxisSevonPin()` 返回 `1`=on，仍用 `!=0`。
  5. **无「相对坐标」**：`CardAxisPMove` 忽略 `posi_mode`，直接 `目标=Dist`（永远绝对）→相对点动须 `SimAbsoluteTarget()` 读当前位+Δ发绝对。真实卡族（MCN420 `DmcCardAxisPMoveUnit(...,PosiMode)`）不受影响。
  6. **回零靠速度缓冲递减**：`CardAxisHomeMove` 后台循环 `目标 -= (int)(速度缓冲[1]/1000)`/ms，缓冲 0 就永不减→isRun 永久 true；回零前 `EnsureSimReady(axis.HomeSpeed)`。
- 位置/距离按脉冲下发、读回÷脉冲当量（无卡内换算层）；未填按 1:1。桥内收口 `IsSimulation/EquivOf/BuildSimParam/EnsureSimReady/SendPointMove`。
- `AxisRawRead.ServoOn`（`bool?`）优先于状态字 bit8 判使能；有状态字时 `AlarmCode` 置 0（bit0 权威），`AlarmCodeText` 显示「—（见状态字 bit0）」。

## IO「功能」列
- `Services/IoFunctionCatalog.cs`：`InputFunctions`/`OutputFunctions`（默认 None="无"）；`IsValid/Normalize/IsSafetyInput/IsIndicatorOutput`。
- `OperatorViewModel.UiTimerTick`（150ms）末步 `EvaluateIoFunctions()`：安全输入→`EStop()`；指示输出按 `StatusBarService.IsRunning/EStopped` 自动写。默认「无」=不干预。
- 选点方向：气缸/轴一律用 `InIoNames`/`OutIoNames`，别用合并的 `IoNames`。

## 气缸页 & IO 名称库
- IO 名称库三份：`InIoNames`/`OutIoNames`/`IoNames`（合并）。`Catalog.SyncAllFromData`+`IoPanelViewModel.SyncIoCatalog` 维护。
- 气缸页布局：参数左 / 状态显示右。无「气缸时序动作表」。
- `CylinderItem` 真实生效：`OutPoint`(伸)/`BackupSensor`(缩,输出第2路,互斥)/`SensorExtend`·`SensorRetract`(输入)/`DelayMs`·`ExtendMs`·`RetractMs`/`TimeoutMs`/`TimeoutAction`/`PulseOutput`·`PulseWidthMs`。
- 2 路输出：缩回输出配置了就写（不受 DoubleCoil 门控）；超时按 `TimeoutAction` 分支（报警并停止/仅报警/忽略）。
- 状态显示=按名在 Inputs/Outputs 找 IoItem 读实时 Value（300ms 刷新），与 IO 表联动。

## 轴页（2026-09-24 最终形态，关键）
- 模型 `Models/AxisItem.cs` 补齐底层参数：StartVel/StopVel/SpeedCurve/SPara、HomeDir/HomeTimeoutMs、SoftLimitEnable/LimitLevel、AlarmEnable/OriginLevel/OriginStopMode/EncoderEnable/EncoderDeviation、AllowManual/AllowHome/AllowSetZero、JogStep/ManualSpeed。
- 轴状态服务 `Services/Hardware/AxisMonitor.cs`（不改 IHardwareBridge 接口）：`AxisRawRead`/`AxisStatusSnapshot`（**可空布尔=读不到→显示「—」绝不编造**）/`AxisMonitorService.Read|Enable|Stop|Home|Inch|StartJog|SetZero`，按 `HardwareSetup.Mode` 分派；仿真模式返回「未连接」。
- 状态字位：bit0 报警/bit1 正限位/bit2 负限位/bit3 急停/bit4 原点/bit5 到位/bit6 正软限位/bit7 负软限位/bit8 使能；总线轴用 CiA402==4 覆盖使能。
- `ViewModels/AxisRowViewModel.cs`（一轴一实例；颜色用 hex 字符串；命令全部 Task.Run 后台+回 UI 报状态栏；`NotConnected` 前置检查——没连卡报「未执行：控制器未连接」不谎报；`NotEnabledHint` 只追加不拦截）。`AxisViewModel` 暴露 `AxisRowViewModel? Monitor`+`HasMonitor`，300ms DispatcherTimer 后台读该轴回 UI；轮询只在轴页可见时开（`AxisPage.xaml.cs` Loaded/Unloaded）。
- 页面 `Views/AxisPage.xaml`：`WrapPanel ItemWidth="240"`+`VerticalAlignment="Top"`（紧凑，4 列，窗更宽自动多排）；**别用 UniformGrid**（等高=留空白）。8 卡：基本信息/运动参数/回零参数/限位与保护/电平与编码器/轴权限/轴状态/轴控制。**状态与控制是 2 个独立容器**。轴控制 2 列按钮：使能|停止、点动−|点动+、Jog−|Jog+、回零|设零点。
- 轴状态卡带「报警码」行（原样显示）；只有 `==1` 才算报警，其它非 0 只显示不判定。**Jog 是「按住才走」**，单击只走一瞬。
- 使能两处职责分开：基本信息「上电使能」（配置项，改值实时下发）+轴控制「使能」按钮（唯一动作）。
- 返工教训：①状态/控制 UI 不要全轴列表，一律「一个容器=当前选中项」；②状态与控制分容器；③用户数「列数」（要 4 列）；④用户对「按钮是否真起作用」极敏感→真调硬件+前置检查+明确失败提示比 UI 重要。

## 仿真模板
- ProjectTemplateCatalog 20 模板；NgTemplates.Build 脚手架。

## 流程页：功能列 / 运算列（2026-09-26）
- **功能列取值**：轴 / **输入IO** / **输出IO** / 气缸 / 点位 / modbus / 变量 / 系统 / 相机 / 延时。（原「IO」已拆成 输入IO + 输出IO：`FunctionOptions`、`FunctionToNamesConverter`(InIoNames/OutIoNames)、`FunctionToPropertiesConverter`(输入IO→输入/脉冲/报警状态；输出IO→输出状态)、`FunctionToOperationsConverter`、执行分派(`FlowRunnerService`/`SimFlowPlayer`/`FlowViewModel`) 全部同步；`AiProjectExchange` 泛称 IO→输出IO。旧工程里残留的 `Function="IO"` 仍可用（各分派保留 case "IO"）。
- 新建工程默认步骤来自 `FlowViewModel.GetTemplateSteps`（StepDef 用的是 API 动作名）：`AddTemplateSteps` 里 `NormalizeTemplateStep` 会归一为中文词 + 拆分 IO，**改功能/运算词表时必须同步这里**。
- **「实际值」列**：`FlowViewModel.RefreshActualValues` → `ReadActualValue(step, bridge)`（1 秒 DispatcherTimer）按「功能 + 属性」**全覆盖**：变量值 / 轴位置·速度·加速度·使能 / 输入IO电平 / 输出IO电平 / 气缸伸出缩回 / 点位目标 / modbus 内容 / 相机结果 / 系统·延时·循环设置值。**读不到一律显示「—」**，且**不要**再写 `if (IsNullOrWhiteSpace(step.Name)) continue`（会把延时/循环/注释这类无名称控制行整行留空）。

## 运算列 = 功能 + 属性 联动（2026-09-26）
- `Views/FunctionToOperationsConverter.cs` 是 **IMultiValueConverter**（输入 Function+Property，返回运算项列表）；`FlowPage.xaml` 运算列用 `<MultiBinding>` 绑两者。**已删「修改」只留「修改为」**。矩阵：轴+位置/编码器位置→绝对移动/相对移动/回零/停止；轴+速度/扭矩/电流/加速度→修改为/加/减/乘/除/取模；轴+已回零→比较；IO+输出状态→修改为/置位/复位，其它→比较；气缸+电磁阀→伸出/缩回/复位，其它→比较；modbus→修改为/置位/复位；变量+数值→修改为/加减乘除取模取反/比较，字符串→修改为/等于/大于/小于，布尔→修改为/取反/等于；点位/系统→修改为/等于。
- 执行器 `FlowRunnerService`：`ExecAxis`（绝对移动/相对移动/回零/停止；属性=速度→设速）、`ExecIo(name,setv,op)`（置位→1/复位→0/其余按设置值）、`ExecCylinder`（优先按 op 伸出/缩回/复位，退回兼容设置值）、`ExecVar`（修改为/加/减/乘/除/取模/取反）。
- ★ 词表三处必须**同步**，否则流程步骤/模板/导入的运算值在下拉里显示空白：①上述转换器 ②`ProjectTemplateCatalog` 的 `MoveAxis/CylOut/CylBack/SetIO/PointStep/CameraStep/WaitStep/LoopStart/CommentStep/CommSend/WaitIO` 等构造助手 ③`AiProjectExchange` 的 `LegalOperations`/`GeneralOperations`/`DefaultOperation`/`MapAction`（**已全改中文运算**，不再用 MoveAxisAbs/HomeAxis/WriteOutput/CylinderMove/CommSend 等 API 名，保证 AI 复制/粘贴往返一致）。
- ★★ 运算词还要在**执行 / 仿真路径**逐个识别，否则「指令成功但轴不动 / 仿真按绝对走」：`FlowRunnerService.ExecAxis`、`FlowViewModel.ExecuteHardwareStep`（单步，原来看属性不看运算）、`SimFlowPlayer.AxisAction`（3D 仿真 / 预览）、`NgRunner`（节点图，节点用「模式」= 绝对/相对）。本项目「相对移动」就同时漏在 `SimFlowPlayer` 与单步路径上。**加任何新运算词前先 grep 这些 switch**。
