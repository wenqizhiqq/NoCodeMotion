# 项目长期记忆（NoCodeMotion — WPF/.NET 10 无代码运动控制）

## 源码与编码
- 全部 .cs 明文可直 Read/Edit。GBK/BOM/CRLF 文件若报 binary，用 Python 读写（utf-8-sig→归一化→写回恢复 CRLF/BOM）。
- 作者水印「温启志◆编写◇微信﹕187◆1936◇1399」三处：`Services/AuthorWatermark.cs`、`MainWindow.xaml:4`、`Docs/*.md` 末尾。删 AuthorWatermark.cs 编译失败，AI 不主动删。

## 运行/构建（沙箱铁律）
- dotnet 一律 LOLBin 拦截：用 **PowerShell + 绝对 `C:\Program Files\dotnet\dotnet.exe` 前台构建**；PowerShell stdout 不回显，须 `| Out-File C:\tmp\x.txt` 再 Read。禁写 `.ps1` 文件（ExecutionPolicy 拦），用内联命令。
- 删文件用 `[System.IO.File]::Delete("绝对路径")`（`Remove-Item` 被 safe-delete 钩子拦）。
- 加载进度：`Services/LoadingService` 静态深度计数（`Show/Report/Hide`，`Progress<0` = 不确定）；启动初始化与打开/新建工程都在遮罩下用确定式进度（xlsx 读写 Task.Run，LoadInto 回 UI）。`Dispatcher.Yield` 须 `System.Windows.Threading.Dispatcher.Yield(...)`（**静态方法**，在 `Window` 里写 `Dispatcher.Yield` 会 CS0176）。
- ★ **启动初始化流程（2026-09-24 改造）**：工程载入**不在 `App` 构造函数里**（那时主窗口不存在，读 xlsx 期间只看到白屏零提示）。现在 `MainWindow` 的 `Loaded` → **`StartUpAsync()`** 全程在加载遮罩下用确定式进度条 + 步骤文字跑：①「正在载入上次工程…」`ProjectManager.OpenProjectAsync(last)`（`LoadLastProject()` + `Exists()` 判存在，xlsx 读取走后台线程）→ ②「正在初始化页面 (i/n)：页签名…」逐页预初始化进 `_cache` → ③「正在进入主界面…」`NavigateTo("Flow")`；`finally` 里 `Hide()`。
- ★★ **「返回 null = 成功」的方法不能用 `?.M() ?? 兜底串`**：本工程桥的 `InchAxis/StartAxisJog/SetAxisZero` 成功时返回 `null`，用 `??` 会把成功判成失败（踩过：点动/Jog/设零点永远报「卡族层未装配」）。必须先显式判桥实例为空，再原样返回方法结果。
- .NET 10 `_wpftmp` CS0579：csproj 加 `GenerateAssemblyInfo=false`+`GenerateTargetFrameworkAttribute=false`；`[assembly:ThemeInfo]` 留根 AssemblyInfo.cs。勿用 UseArtifactsOutput。
- WPF 重复 XAML 陷阱：`EnableDefaultPageItems` 会把 `Views/*.xaml` 当 Page 编译；探针脚本勿生成同 x:Class 的 `XXX_utf8.xaml`（CS0102+CS8646）。

## xlsx 工程存储
- `XlsxProjectStore.cs` 反射式：根 public 集合→sheet（列=标量）；标量→「项目管理」表；IO 由 BuildIoSheet/SplitIoToRoot 合并。
- 嵌套子集合走「父表.子表」分页（CollectNestedSheets/RestoreNestedCollections），MergedBlockParents={PointTables,Trays,Flows} 跳过合并块单页。
- 新增子集合：①元素继承 EditorItemBase；②集合属性带 set（cp.CanWrite）；③中文名加 ChildSheetNameOverrides。
- 只读计算属性勿放数据模型（会成脏列）；新增标量字段零改动落盘、向后兼容。

## 全局 UI 约定
- DataGrid 行内按钮：DataContext 是页面 VM，命令要 `{Binding DataContext.XxxPanel.Cmd, RelativeSource={AncestorType=DataGrid}}`，漏面板路径静默失效。
- 下拉框选值来自固定候选时**勿设 IsEditable**（可编辑输入框盖住整块、点击文字区不展开）。
- 资源字典：`App.xaml` 只合 `Resources/AppStyles.xaml`（再合 HandyControl）。`Themes/AppleControls.xaml` 未合并、运行时不存在，勿引用；所需键以 AppStyles 为准；缺的小字/窄框在页面 `<UserControl.Resources>` 本地补。
- ★ **Window 弹窗（ConfirmDialog/ConnectProgressDialog 等）只能用 AppStyles 全局键**：页面级 `<UserControl.Resources>` 里的键（如 `SubLabel`）在顶层 Window 的 StaticResource 作用域里解析不到，运行时抛 `XamlParseException 无法找到名为"X"的资源`（编译期不报）。缺的样式在弹窗里内联（FontSize/Foreground）或改用全局 `TextMutedBrush` 等。
- 删除/清空红 `TtDeleteBtn`/`TtPillRedBtn`；色彩 红=破坏/橙=反向/蓝=正向/绿=保存/灰=次要。
- 每页底 PageHintBar；EditorPage.Detail 内元素**勿用 x:Name**（MC3093），改用 Tag+FindVisualChildByTag。

## 视觉/节点图
- VisualFlowPage.ApplySelection() 须覆盖 _vm.Steps/_vm.Name/_vm.SelectedStep；Steps 非空且 SelectedStep=null 自动选 [0]。
- 节点图 Models/NodeGraph/ 数据驱动（NgDefs/NgModel）；UI 用 ItemsControl+DataTemplate，禁 Children.Add。输出端口坐标须与 NodeView 一致。
- 调试引擎 `Services/NgRunner.cs`（6 按钮 + NgStepResult）；NgRunner 两坑：WaitResumeAsync 须实例方法按 _state 轮询；Pause 仅节点边界生效。

## CAD/DWG
- 真实 BREP→OcctNet.Wrapper 0.1.1（OcctShape.ImportStep.Triangulate）；STEP Z-up→WPF Y-up 绕 X -90°；BackMaterial 防黑面。
- DWG/DXF→Aspose.CAD 26.7.0 只迭代矢量实体，绝不 Image.Save 栅格化（免水印）。

## 仿真体系
- SimRuntime 静态态驱动 3D+变量页；SimFlowPlayer 编译 List<SimAction>，33ms 驱动。
- BepuPhysics v2（2.5.0-beta.29，纯 C#）：四坑——①Simulation.Create 须 `new SolveDescription(1,1)`；②ConfigureContactManifold 须 `pairMaterial.SpringSettings=new SpringSettings(30f,1f)`；③运动学↔动态用 BecomeKinematic/SetLocalInertia（勿改 LocalInertia ref）；④推工件用 Velocity.Linear 速度驱动勿只设 Pose。详见 2026-09-05.md。

## 控制卡接入（2026-09-23 累积）
- 雷赛实桥 LeadshineHardwareBridge + LtdmcCard/LtdmcNative P/Invoke LTDMC.dll；无卡降级兜底。`HardwareSetup.Mode`：CardFamilies / Leadshine。
- 控制器页连接链路：`AxisControllerViewModel.Connect()` → `HardwareSetup.EnsureInitialized()` → `Mode==CardFamilies` 走 `CardFamilies.TryConnect(ctl,out)` 否则 `Reconnect()`+`IsCardReady`；`Disconnect()` 走 `CardFamilies?.Disconnect`。桥接类实际名 **`WenQiZhiCardBridge`**（非 SamsunCardBridge），`ResolveFamily(ctl)`/`TryGetRealCounts`/`IsControllerReady` 在内。
- 在线状态直接读底层 `IsControllerReady(ctl)`/`ControllerStatus(ctl)`（无 VM 影子字典、不落盘）。
- 连接后 `FetchDetectedCounts` 回写轴/IO 真实数→`GenerateIoPoints(ctl)` 按本卡清旧+生成 IO 点；`DetectedCountsChanged` 静态事件通知轴/IO 页重算。
- 卡型选择器 SelectCardTypeDialog；Native/ 已是 x64 DLL 全集（csproj `Native\*.dll`→PreserveNewest），缺 Dmc2210(x86)/MCC/SLD9014PTP。
- 容量兜底：硬件 API 不报容量时，在 `CardFamilyDescriptor` 填 AxisCount/InIoCount/OutIoCount（如 MCN420=8/16/16）。

## 控制器连接体验增强（2026-09-23，本轮）
- 连接按钮后台化：`AxisControllerViewModel.Connect()` 改 `Task.Run` 后台，分步 `ConnectStatusText`（初始化/连接卡/读真实轴IO/生成点/完成/失败），`IsConnecting` 防重复点击，`Application.Current.Dispatcher.Invoke` 回 UI。`AxisControllerPage.xaml` 加 `IsConnecting` 绑定的不确定进度条 + 状态文字。
- 状态栏在线指示：`StatusBarService` 加 `ControllerOnlineCount/TotalCount`+`SetControllerStatus(online,total)`+`ControllerStatusText/ControllerColor`（全在线绿/部分橙/全离线红/无控制器灰）；`StatusBarViewModel` 转发 INPC；`StatusBarView.xaml` 加第 3 列「控制器」圆点+文字（Grid 改 6 列）。VM 每次连接/断开/自动连接后调 `UpdateStatusBar()`。
- 自动连接：`ProjectManager.DataReloaded`（`System.Action?`，**0 参**）/ 构造函数触发 `RequestAutoConnect()`（**不是**直接 `AutoConnectAll`）。`RequestAutoConnect` 关键铁律：**必须等 `LoadingService.IsLoading==false`（载入遮罩隐藏）后**才 `BeginInvoke(Background)` 调 `AutoConnectAll`——否则连接与 `PreloadAllPagesAsync` 页面预初始化同跑，抢原生加载器锁（`CardFamilyCatalog.DetectPresent` 枚举底层 DLL）会拖住载入进度条，表现成「连上控制器才进主界面」。载入中则挂一次性 `LoadingService.StateChanged` 回调（`_autoConnectPending` 防重复），等遮罩隐藏再连。
- 连接按钮合并（2026-09-24）：控制器页「获取」+「连接」两按钮合并为单按钮「连接控制器」→ `ConnectControllerCommand` = 先 `FetchModules()`（获取卡参数）再弹 `Views/ConnectProgressDialog`（Apple 圆角白卡 + 不确定进度条 + `ConnectStatusText`）后台 `Connect()`，`IsConnecting` 变 false 时弹窗自动关。（`StackPanel` 无 `Padding`，用 `Margin`。）
- 硬件读数必须在后台线程（2026-09-24，铁律）：`FetchDetectedCounts` 拆成 **`ReadRealCounts`（纯硬件读，后台可调）+ `ApplyRealCounts`（写回绑定属性，必须 UI 线程）**。`Connect()`/`AutoConnectAll()` 的 `dispatcher.Invoke` 里**绝不能**再直接调 `TryGetRealCounts`/`UpdateStatusBar()`/`IsControllerReady`（都是硬件查询）——否则连接时 UI 线程卡死、页面点不动。后台算好数量与在线数，UI 线程只赋值 + INPC + `StatusBarService.SetControllerStatus(online,total)`。
- 控制器页布局与门控（2026-09-24）：detail 由横向 `WrapPanel` 改**纵向 `StackPanel`（从上到下，卡片全宽）**；流程门控「**卡型号 → 扩展IO → 连接**」：VM `HasCardType`/`CanConfigureExpansion`/`CanConnect`（都由 `SelectedItem.CardType` 决定，在 `WireSelected`+`OnTrackedCardChanged` 刷新），扩展IO 配置块 `IsEnabled="{Binding CanConfigureExpansion}"`，未选型号显示橙色提示，「连接控制器」按钮 `IsEnabled="{Binding CanConnect}"`。
- ★★ 硬件桥全局锁的作用域铁律（2026-09-24，连接卡界面的真根因）：`WenQiZhiCardBridge`（`Services/Hardware/Cards/SamsunCardBridge.cs`）只有一个全局 `_gate`。原来 `TryConnect`/`SlotOf` 是 `lock(_gate){ Initialize(slot) }`，而 `Initialize` 含 `InitCard()`/`OpenCard()`（真硬件，秒级）；**UI 线程的 `IsOnline`→`IsControllerReady`、`ConnectionStatusLines`→`ControllerStatus`、`AnyReady` 都要 `lock(_gate)`** → 连接期间界面整个冻结（VM 侧后台化无效，因为 UI 绑定自己会查桥）。修法：`_gate` 内只做快速状态读写；慢硬件调用（Initialize/CloseCard）移到 `_gate` **之外**，用 `CardSlot.InitLock`（per-slot）串行。`Disconnect`/`Reset` 同理；VM `Disconnect()` 也改 `Task.Run` 后台。**凡共享硬件桥：锁内绝不放 InitCard/OpenCard/CloseCard 这类慢调用。**

## IO「功能」列（2026-09-24，真实生效）
- 目录：`Services/IoFunctionCatalog.cs` — `InputFunctions` / `OutputFunctions` 两套（默认 `None="无"`）；`IsValid/Normalize/IsSafetyInput/IsIndicatorOutput/DesiredIndicator`。`IoItem` 默认功能 = 无。
- 运行时：`OperatorViewModel.UiTimerTick`（150ms UI 定时器）末步 `EvaluateIoFunctions()`：**安全输入**（急停按钮/安全门/光栅，逻辑 Value≠0）→ `EStop()`；**指示输出**（运行/就绪/报警/三色灯/蜂鸣器）按 `StatusBarService.IsRunning/EStopped` 自动写 `WriteOutput`。默认「无」= 不干预（opt-in）。
- `IoPanelViewModel.IsInput => Title != "输出"`；构造 + `OnAfterExcelReplace` 里 `NormalizeFunctions()` 把旧值（如「动点」）归一为「无」。
- `Views/IoPage.xaml`：输入表 ComboBox 绑 `InputFunctionOptions`、输出表绑 `OutputFunctionOptions`。
- 注：`IoItem.Level`（取反/不取反）本就真实生效（`ApplyLevel` 决定常开常闭），勿与「功能」混淆。

## 气缸页 & IO 名称库（2026-09-24）
- IO 名称库分三份（`Services/Catalog.cs`）：`InIoNames`（输入）/ `OutIoNames`（输出）/ `IoNames`（合并，给流程页通用下拉）。`Catalog.SyncAllFromData` 与 `IoPanelViewModel.SyncIoCatalog` 同时维护三份。**需要方向的选点（气缸/轴等）一律用 `InIoNames`/`OutIoNames`，别用合并的 `IoNames`**（旧 bug：气缸「输出点」列出了输入点）。
- 气缸页布局：**参数左 / 状态显示右**（左 `1.25*` 放 基本信息/IO配置/动作参数/安全与逻辑/高级；右 `1*` 放 状态显示（只读）+ 手动测试）。**无**「气缸时序动作表」（已删）。
- `CylinderItem` 运行时真实生效字段：`OutPoint`(**伸出输出**第1路，输出IO) / `BackupSensor`(**缩回输出**第2路，输出IO；与第1路互斥) / `SensorExtend`·`SensorRetract`(输入IO) / `DelayMs`·`ExtendMs`·`RetractMs` / `TimeoutMs`(超时时间) / `TimeoutAction`(超时报警方式：报警并停止/仅报警/忽略) / `PulseOutput`·`PulseWidthMs`。装饰性（运行时不用）：`SensorType`/`ToleranceMs`/`ExtendSpeed`/`RetractSpeed`/`LinkedAxis`/`Interlock`/`DoubleCoil`/`AlarmEnable`/`ManualEnable`。
- 气缸 2 路输出：两桥 `CylinderMove` 只要「缩回输出」配置了就写（不再受 `DoubleCoil` 门控）；超时按 `TimeoutAction` 分支：默认「报警并停止」= `AlarmService.Raise` + `throw ScriptRuntimeException`，「仅报警」= Raise 后继续，「忽略」= 只 Log。`AlarmService` 在 `NoCodeMotion.Services.Hardware.*` 命名空间下可直接引用（父命名空间查找）。
- 气缸「状态显示」= 按名称在 `ProjectStore.Data.Inputs/Outputs` 找 IoItem 读实时 `Value`（300ms DispatcherTimer 刷新），即与 IO 表真实点位联动。
- 页面标题栏所在 Grid 的 detail 内容若需滚动：在 `<local:EditorPage.Detail>` 内自己包 `ScrollViewer`（见控制器页/气缸页）。

## 轴页：参数对齐底层 + 轴状态 / 轴控制（2026-09-24，最终形态）
- **模型**（`Models/AxisItem.cs`）补齐底层关键参数（全是标量 → xlsx 零改动落盘、向后兼容）：`StartVel/StopVel/SpeedCurve/SPara`、`HomeDir/HomeTimeoutMs`、`SoftLimitEnable/LimitLevel`、`AlarmEnable/OriginLevel/OriginStopMode/EncoderEnable/EncoderDeviation`、轴权限 `AllowManual/AllowHome/AllowSetZero`（`AllowEnable` 保留但已不用）、手动 `JogStep/ManualSpeed`。
- **轴状态服务**（`Services/Hardware/AxisMonitor.cs`，**不改 `IHardwareBridge` 接口**）：`AxisRawRead`(原始) / `AxisStatusSnapshot`(解释后；**可空布尔 = 读不到 → 界面显示「—」，绝不编造状态**) / `AxisMonitorService.Read|Enable|Stop|Home|Inch|StartJog|SetZero`，按 `HardwareSetup.Mode` 分派到具体桥；仿真模式返回「未连接」。
- 轴状态字位定义（`dmc_axis_io_status` 标准，与卡族 `AxisRealization` 注释一致）：**bit0 报警 / bit1 正限位 / bit2 负限位 / bit3 急停 / bit4 原点 / bit5 到位 / bit6 正软限位 / bit7 负软限位 / bit8 使能**；总线轴用 CiA402 状态机==4 覆盖「使能」。
- 桥新增（两桥同名）：`TryReadAxisRaw` / **`InchAxis(axis, distance, speed)`** / `StartAxisJog(axis, positive, speed)` / `SetAxisZero`。雷赛走 `dmc_axis_io_status`+`dmc_check_done`+`dmc_get_axis_state_machine`，Jog=`dmc_vmove`，设零=`dmc_set_position_unit`；卡族走 `GetCardAxisCurrentPosition(0/1)`/`GetCardAxisCurrentState()`/`GetCardAxisAlarmState()`，**状态字用反射调卡族无参 `GetAxisCurrentState()`**（不在 IAxis 上，按 Type 缓存 MethodInfo；取不到就只给位置/编码器/运动中），Jog=`CardAxisSerialMovement(Dir)`，设零=`SetCardAxisCurrentPosition`。
- ★★ **Jog / 点动 必须「先下发速度曲线再发运动指令」**（2026-09-24 真 bug）：正常定位 `MoveAxisRel/MoveAxisAbs` 都是先 `SetCardAxisMotionalVel`（卡族）/ `EnsureProfile`（雷赛）再动；`StartAxisJog`/`InchAxis` 少了这步 → 卡「收下指令但按无效 profile 跑」= 现场「点了 Jog 轴不动」。且 **`IHardwareBridge.MoveAxisRel` 把速度写死成 `axis.Speed`，「手动速度」对它无效**，所以点动必须走桥上的 `InchAxis`（带 speed 参数），不能复用 `MoveAxisRel`。
- ★ **CS1628 铁律**：`out` 参数不能出现在 lambda / 本地函数里 —— 读硬件时先写本地变量，最后再组装 `out` 结构体。
- **VM**：`ViewModels/AxisRowViewModel.cs`（**一个轴一个实例**；状态只读属性 + **颜色用 hex 字符串**，与状态栏 `RunColor/ControllerColor` 同套路由 WPF 自动转 Brush；点动距离/手动速度；使能·点动·Jog·回零·停止·设零点命令，全部 `Task.Run` 后台 + 回 UI 报状态栏；`NotConnected(action)` 前置检查——没连卡直接报「未执行：控制器未连接」，**不谎报「已下发」**；`NotEnabledHint()` 只在明确读到「未使能」时**追加提醒但不拦截**）。`AxisViewModel` 暴露 **`AxisRowViewModel? Monitor` + `HasMonitor`**，用 `override OnPropertyChanged` 捕捉 `nameof(SelectedItem)` 重建（选中/新增/删除轴都覆盖），**只服务当前选中的那一个轴**；`SetStatusRefreshEnabled(bool)` + **300ms `DispatcherTimer`** 后台读该轴 → `BeginInvoke` 回 UI 赋值；`Interlocked` 防重入；`!HardwareSetup.IsCardReady` 直接置「未连接」不读卡；`ReferenceEquals(Monitor, m)` 防期间换轴回填。**轮询只在轴页可见时开**（`AxisPage.xaml.cs` 的 `Loaded/Unloaded` 开关）。
- **页面**（`Views/AxisPage.xaml`）：**`WrapPanel ItemWidth="250"` + 每张卡 `VerticalAlignment="Top"`**（紧凑：每张卡只有自身内容那么高；该宽度下正好 4 列，窗口更宽自动多排）。★ **别用 `UniformGrid Columns="4"` 追求「正好 4 列」** —— UniformGrid 所有单元格等高 = 全局最高那张卡的高度，矮卡下方会留大片空白（用户明确抱怨过）。8 张卡 = 基本信息 / 运动参数 / 回零参数 / 限位与保护 / 电平与编码器 / 轴权限 / **轴状态** / **轴控制**。**状态与控制是 2 个独立容器**（用户要求），样式/颜色/字号全部复用 `AppleGroupCard` + `AppleSectionHeader` + `AppleRow` + 全局药丸按钮。轴控制里 2 列按钮组：使能|停止、点动−|点动+、Jog−|Jog+（`beh:JogHoldBehavior`）、回零|设零点。卡片里的长说明一律放 `AppleSectionHeader.ToolTip`，不要占一行高度。**页内不定义任何本地样式**。
- **轴状态卡必须带「报警码」行**（原始错误码）：很多卡族没有无参 `GetAxisCurrentState()`，报警只能靠 `GetCardAxisAlarmState` 的错误码非 0 推出；现场「指令下发成功但轴不动」九成是**有报警未清 / 未使能**，`MoveHint()` 会把这两条追加到点动/Jog 的成功提示里（**只提醒不拦截**）。位置/编码器显示精度 `0.####`。
- **Jog 是「按住才走」**：单击只走一瞬（用户会误报「点 Jog 没反应」）。要按设定距离走一步必须用「点动 −/+」。UI 文案与 ToolTip 都要写清这一点。
- **使能只有两处，职责分开**：基本信息里的「**上电使能**」（工程配置项，改值会实时下发使能）+ 轴控制容器里的「使能」按钮（唯一的动作）。轴权限里的「允许使能」已删（`AllowEnable` 字段保留不用）。
- 改完页面 XAML 后**务必脚本校验所有 `StaticResource` 键都存在**（AppStyles 全局 + 页内局部）；页面级键在 Window 弹窗里取不到会运行时炸。
- **本页返工教训（用户两次纠正）**：① 状态/控制类 UI **不要全轴列表**，一律「**一个容器 = 当前选中项**」；② 状态与控制要**分容器**（不要挤在一张卡里左右两列）；③ 用户会数「**列数**」（要 4 列）；④ 用户对「按钮是不是真起作用」极敏感 → 真调硬件 + 前置检查 + 明确失败/未使能提示，比 UI 更重要。

## 仿真模板
- ProjectTemplateCatalog 20 模板；NgTemplates.Build 脚手架。
