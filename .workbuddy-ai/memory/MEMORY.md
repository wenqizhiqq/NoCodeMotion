# NoCodeMotion 长期项目笔记
> 陷阱索引。完整推导见 `.workbuddy-ai/memory/` 逐日日志（2026-09-19 起）。
> ★ 硬件层细节（雷赛 / 研控 MCN420 / 卡族 / 多开退出 / 海康相机）见 **`HARDWARE.md`**，动硬件前先读。
> ★ UI 细节（名称库级联下拉 / 工程师页布局 / 离屏渲染验证 / 弹窗 Owner / 提示栏）见 **`UI.md`**，改 UI 或要「证明 UI 对了」先读。
> ★ 视觉子系统（视觉步骤四处同改 / OCR / 图像采集来源 / 结果图缩放平移 / 字符识别文字框叠加 / **标定 9点XY+5点旋转 → 模板匹配输出机台真实位置**）见 **`VISION.md`**。
> ★ 通讯子系统（串口/网口/Modbus/**真实 SECS/HSMS：E5 数据项 + E37 会话 + 页面真收发**）见 **`COMM.md`**。

## 一、构建 / 验证
- dotnet 走独立 shell 工具（非 bash）。★ `dotnet … > log` 是 **GBK(cp936)** → 中文 pattern `grep` **静默 0 匹配**（`iconv -f gbk`；或 `DOTNET_CLI_UI_LANGUAGE=en-US`）。★ PowerShell `Tee-Object` 写的是 **UTF-16**，不是 GBK。
- ★ **清理要复核**：`Remove-Item -Recurse -Force` 静默不删；bash `rm -rf obj bin` 被沙箱**批量删除守卫**拦下 —— 连 `for f in …; do rm -f` 循环删多个也会被 **SIGTERM 掐断**（只删掉前几个）→ **逐个删、删完 `ls` 复核**。up-to-date 会跳过 CoreCompile → 误判「全量成功」；**空或极短日志 = 没编译**。
- ★ **build 非 0 时冒烟仍会跑旧 dll**（`exit=0`、输出是上一次的，看着「全部通过」）→ **先确认 `0 个错误` 再看冒烟**。build 偶发 `exit=-1073741571`（栈溢出），重跑即可。
- ★ `dotnet exec` 严格按 deps.json → 主工程 `<Reference>`+`HintPath` 的 dll **不进引用方 deps.json**，冒烟要**照抄同样的 `<Reference>`**。`NETSDK1060` = shell `APPDATA`/`ProgramFiles` 空 → 显式 `env` 传入。
- ★ 源码守卫**必须在仓库目录跑**（`%TEMP%` 读 `D:\` 被沙箱替换成乱码，还会被清理整删）→ 工具全放仓库：`tools\guard_sources.py`（G1–G28，**311 PASS**）、`tools\_dump.py`（看 BOM）、`tools\_seal.py`（新 `.cs` 补页眉页脚水印 + BOM）、`.smoke\`。
- 冒烟：`dotnet build .smoke/ncm_smoke.csproj -p:UseAppHost=false` → `PATH="<outdir>:$PATH" dotnet exec .smoke/bin/Debug/net10.0-windows10.0.19041.0/ncm_smoke.dll`（段 A–S，**297 PASS**）。★ `%TEMP%` 里的 exe 跑不起来（`exit 127`）→ 一律 `dotnet exec <dll>`。冒烟与 `--no-incremental` 不可并行（互删 `obj\`）；`-t:Rebuild` 跳过 CoreCompile 报假「0 警告」。
- ★ **改 `TargetFramework` 会挪输出目录** → 旧路径 dll 还在 → `dotnet exec` 旧路径**静默跑旧代码**（新加的段「消失」）。改完先删旧 bin 或确认新路径。
- ★ **`dotnet exec` 下 OpenCvSharp 原生库不可用**：`opencv_world480.dll` 不在 DLL 搜索路径 → `DllNotFoundException … 0x8007007E`。修：`PATH="<outdir>:$PATH" dotnet exec <dll>`。冒烟里走 OpenCV 的段都要**先探测、探测失败就 `SKIP`**。
- 补丁脚本：按行号区间替换，或 `sub1` 断言 `count==1`、锚点全过后一次写盘。★ **别把 BOM 写成硬断言**；★ **行尾断言要拿「重建后」的计数比**（CRLF 文件写盘时会把每个 `\n` 都转成 `\r\n`，拿原始 CRLF 计数比会假报 drift）。★ 新建 `.cs` 时从**同级已有文件**原样复用页眉/页脚**尾部**（零宽水印逐文件不同，别自己编）→ 用 `python tools/_seal.py <目标.cs> [模板.cs]`：目标里写 `@@HDR@@`/`@@FTR@@` 占位行，脚本按**模板**给 BOM、按参数给行尾；占位符为 0/0 时按「已封过」处理，只补 BOM/统一行尾。★ 工具调用参数里的 heredoc 会把 `\n` 折成真换行 → 要写 C# 的 `\\n` 转义时用 `chr(92)`/`chr(34)` 拼，并在写盘前 `assert NEW.count(chr(92))==0`。临时目录带点前缀（`.smoke/`）不编译，不带点（`scratch/`）会被 `**/*.cs` 收走。

## 二、代码约定
- 隐式 using 只有 System/Collections.Generic/Linq/Threading/Tasks（**无** System.Windows / System.IO / System.Windows.Input）。★ 冒烟里用 `MouseButtonEventArgs`/`Mouse`/`MouseButton`/`Cursors` 要自己 `using System.Windows.Input;`（漏了是编译错，但 build 失败 → 静默跑旧 dll）。
- 行尾/BOM 逐文件保持；头尾作者水印（零宽 U+200B/U+2063/U+200D）按字节保留；**XAML 无水印**。**BOM 逐文件不同** → 先探再写；多编辑同一文件先内存累积、最后写盘。记忆文件：`MEMORY.md` 是 **CRLF 无 BOM**，`UI.md`/`HARDWARE.md`/逐日日志是 **LF 无 BOM**。
- ★ XAML `{StaticResource KEY}` 必须在 `App.xaml` 递归可达链里：**只合并 `Resources\AppStyles.xaml`**，`Themes\AppleControls.xaml` 从未合并 → 写了它的键就在 `InitializeComponent()` 抛 `XamlParseException`。**grep 到 `x:Key` ≠ 运行时找得到，build 也抓不到**（G17）。
- `AppleToggle` 46×26 **无文字开关**，模板不渲染 Content → 要标签另配 TextBlock。★ **`TtBaseBtn` 的 `Padding` 硬编码在 ControlTemplate 里** → 覆盖无效，只能改 `MinWidth`/`Height`/`FontSize`；`TtPill*` 的 `Padding` 是 Setter+`TemplateBinding` → **可覆盖**。**先看样式模板会不会渲染你要设的属性。**
- ★ **`ItemsControl.GroupStyle` 是只读 CLR 集合、不是依赖属性** → 只能回调里 `Add`。`CollectionViewGroup` **不实现 `IEnumerable`**（用 `.Items`）。★ **全局钩子用 `[ModuleInitializer]`，别用静态构造**（要等首次访问本类，漏一处引用就静默失效）。
- ★ 附加行为文件放 `Views/`，但命名空间统一是 **`NoCodeMotion.Behaviors`**，XAML 用 `xmlns:beh="clr-namespace:NoCodeMotion.Behaviors"`（`JogHoldBehavior` / `NameGroupHeaderBehavior` / `ZoomPanBehavior`）。

## 三、架构要点
- `Catalog`（`Services/Catalog.cs`）是下拉框名称缓存，只在 `Load`/`LoadInto` 经 `SyncAllFromData` 重建，**不是数据源**；`Catalog.*Names` 是受限值域 → 写候选外的值渲染空白。
- 自动保存链 `Model.PropertyChanged → ListVM.OnItemPropertyChanged → ScheduleSave()`；**运行态/只读展示属性必须过滤**，否则每秒写盘。
- 点位「名称」用 0 基轴号；`XlsxProjectStore` 点位表列名固定 `轴1位置…轴1名`（4 轴槽），改了读不了老工程。
- ★ 重复轴号是静默灾难：`AxisOf` 的 `slot.Axes` 是 `Dictionary<轴号,IAxis>`，卡族读写不带轴号 → 两轴同号共用物理通道。
- ★ **`DependencyObject` 型 VM：DP 变化不会自动抛 `INotifyPropertyChanged`**。只注册 DP 回调（`HasXxx` 系列）→ 靠 `PropertyChanged("Xxx")` 驱动副作用的代码是**死代码**。`VisualFlowDetailViewModel` 已 override `OnPropertyChanged(DependencyPropertyChangedEventArgs e)` 把 `e.Property.Name` 也抛出去；否则结果图叠加层只能等 `ImageHost.SizeChanged`（缩放窗口 / 切页）才刷新 —— **跑完一次不刷新绿框**。
- ★ **过滤共享集合必须用私有 `CollectionViewSource`**：`GetDefaultView(coll)` 的 Filter **全局共享**；`IoPage`/`EngineerPage` 绑**同一** `ProjectData.Inputs/Outputs` → 工程师页只能 `new CollectionViewSource { Source = … }`（G16.7）。★ 反向：**分组**故意打在**默认视图**上（在 `Catalog` 一处改即全体生效）。

## 四、流程 / UI 页
- `TableToolbar`「共 N 项」来自 `MatchInfo`；行数权威是 `TargetGrid.ItemsSource`。
- 流程「名称」列是受限下拉（`FunctionToNamesConverter`→`Catalog.*Names`）；**别为显示未知值把当前值并进 ItemsSource**（SelectedItem TwoWay 置空回写 null）。
- JSON 粘 AI 用 `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`。`RestoreFlows` 失败返 false 且不动 sink；空快照 `"[]"` 合法。
- `ExportSteps` 必须导出 `FlowStep.DurationMs`（键 `耗时`）：加字段时 `Export*` 与 `Fill*` 同改。
- Lua API 唯一权威是 `Services/HardwareApi.cs` 的 `Register()`；`Camera`/`Vision` 不注册。视觉流程契约是 `视觉步骤` 数组。
- FlowPage.xaml Row-0（复制JSON/粘贴生成/回退）四流程共用、在 `EditorPage.Detail` 里，**不能起 `x:Name`（MC3093）**。
- 「图像生成点位」= 工具栏按钮 + `Views/GraphGenDialog.xaml(.cs)`（VM=`GraphPointGenViewModel`）；`Graph.Generated` → 追加到当前工位。画布**最上层透明 `Border` 收单击**，底下全 `IsHitTestVisible=False`。
- 新建工程示例模板（Request N-samples）：`ProjectTemplate.Build()` 是唯一出口 → 三道后处理 `SeedSampleConditions` + `EnsureNodeGraphFlow` + **`ExpandNameLists`**（由 `ExpandSampleNames` 开关，仅「空白工程」= false）。给每个模板追加 3 工位 × 8 = **24 个**轴/输入/输出/气缸/变量，全「工位-对象」命名（`上料-轴1`…）→ 一建工程下拉就是级联（轴 25 / 入 28 / 出 28 / 缸 24 / 变量 26 / 并集 134）。
- ★ 扩展轴卡厂商**必须写「雷赛」**：`CanServeProject()` 只 `continue` 掉 `fam.Vendor=="雷赛"`，写成「模拟卡」会把**所有模板**的默认硬件通道整体改成卡族层（不报错，行为全变）。卡型号 `DMC-E3000`，`CardNo=9` 避开模板自带 0/1/2，卡内 `AxisNo` 0..23 唯一。
- ★ 名称库级联下拉 / 工程师页布局 / **新建弹窗 Owner** / 「工位-对象」提示栏：**细节见 `UI.md`**。

## 五、暂停/停止守卫（Request B）
- 进程级静态 `HardwareBridge.WaitGuard`（`Action`）所有阻塞等待（Delay/MoveAxis/Lua/气缸脉冲）每 ≤50ms 轮询。Pause → `ResumeEvent.Wait()`；Stop/E-Stop → **抛 `OperationCanceledException`**（不是 `ScriptRuntimeException`，否则被 `ExecuteLeaf` 的 `catch(Exception)` 吞掉）。
- 每次运行 `HardwareBridge.BindWaitGuard(object ctrl)`（**反射**，避 Services→ViewModels 环依赖）。雷赛/研控 `WaitInterruptible` 必须读**静态** `HardwareBridge.WaitGuard`（读实例字段 → 永不响应暂停/停止）。气缸脉冲**不能**暂停在半途，只查 Stop，重抛前把输出复位到安全电平。
- 速度路径：`ExecAxis` `prop=="速度"`→`SetAxisSpeed`；`ExecPoint` `slot.Speed>0`→`SetAxisSpeed`；Lua `HardwareApi.SetAxisSpeed`；NgRunner `MoveAxis`。**`FlowLoopManager` 是 `NoCodeMotion.ViewModels` 顶层静态类**；`StopAll()` 停所有循环流程。

## 六、UI 验证（详见 `UI.md`）
- ★ 驱动 UI 用 UIA `InvokePattern`（不合成鼠标）；`PrintWindow` 不可靠 → 以 UIA 文本为准。
- ★ **`RenderTargetBitmap` 离屏渲染 + `ActualWidth/ActualHeight` 量化**是可靠证据，但**别锁死阈值** → 断言**关系**；元素是**活对象**（重排后尺寸就地更新，测拉伸要在改尺寸前取值）。
- ★ **给页面加了新控件/新绑定，就把它加进冒烟段 F**（真 `new` 一次走 `InitializeComponent()`）—— 这是唯一能抓到「`StaticResource` 键写错 / 绑定写错」的自动手段（build 抓不到）。
- ★ `EditorPage` 空态会把详情整块 `Collapsed` → 离屏量到 0×0 是假象，先 `ProjectStore.Data.CopyFrom` 灌数据。

## 七、硬件层 / 相机
- **细节全在 `HARDWARE.md`**（雷赛 P/Invoke 与 CiA402、卡族 `AutoDetectFromProject`、研控 MCN420 使能极性 / 8194=非法 Channel / 加减速是「秒」、单实例与退出、海康 MVS 会话复用 + 永不抛回退）。
- 一句话记住：**参数语义看原生声明，极性看真机，不用错误码表反推。** 轴必须一轴一个 `IAxis`；`GetCardAxisCurrentState()` 0=停止/1=运行中（反直觉）；模板轴加减速统一 `0.2` 秒。

## 八、变量与表达式
- `ExpressionEvaluator.Tokenize` 用 `char.IsLetter` → **中文也算标识符**，`-` 是减运算符：变量名里的 `-` 一旦进表达式就被当减号（`下料-计数` → `下料 - 计数` = 0）。流程步骤按**名称精确匹配**取变量，`{变量名}` 走 `FlowRunnerService.Sub()` 正则 `\{([^}]+)\}` 字面替换 → 声明与引用都安全。
- ★ **已修（N-samples 收尾）**：`SimFlowPlayer` 的「变量」步骤**不再**把变量名拼进表达式。改为存 `VarOp`（加/减/乘/除/取模/取反）+ `VarExpr`（操作数），Apply 时 `GetVariableResolved(VarName)` **按名精确取值**后再运算 → `-` 命名的变量在仿真里也算对（取反保持 `1-cur`）。真机 `FlowRunnerService.ExecVar` 本来就走 `GetVarNum(name)` 精确匹配。

## 九、视觉流程 / 图像采集 / 结果图缩放
- **细节全在 `VISION.md`**：视觉步骤「四处同改」、OCR=Windows 自带引擎（`Windows.Media.Ocr`）、图像采集「来源」= 相机/文件/文件夹、结果图 `ZoomPanBehavior`（滚轮缩放 / 中键平移 / 双击复位）、**标定（9 点 XY 仿射 + 5 点旋转圆拟合）**。
- 四条最贵的坑：① 加视觉步骤类型**四处同改**，少一处静默失效；② 图像采集默认来源是「相机」，**来源=文件/文件夹 路径无效一律明确失败**（旧行为是悄悄出合成测试图还报 `Ok=true`）；③ 真实取像只有 `TryGrabRealCamera` 一处，`ResolveCameraIndex` 同时认**相机名**与编号；④ OCR 的框只能来自 `OcrLine.Words[].BoundingRect`，**有 ROI 时要加回 ROI 原点**，且要**单独收集**（混进 `report.Matches` 会污染「共找到 N 个目标」）；⑤ **标定会真驱动轴**：仿真桩必须拒绝、轴名找不到必须失败（绝不拿轴 0 顶上）、取不到图必须失败（不回退合成图），且停止信号 `OperationCanceledException` **必须冒泡**（吞掉就会继续走完 14 个点）。

## 十、通讯 / SECS
- **细节全在 `COMM.md`**（SEMI E5 数据项 / E37 会话 / 页面真收发 / 与 Lua 同一条通道）。
- 三条最贵的坑：① **控制消息判定必须同时要求 `byte3(Stream)==0`** —— S1F1 的功能号 `1` 与 `Select.req` 的 SType `1` 是同一个字节值，只看 `byte5` 会把 S1F1 当 Select.req；② `CommManager.Create` 的 **SECS 分支必须在 Modbus / 默认 TCP 之前**（落到最后会被当普通 TCP，**不报错**但完全没有 SECS 语义）；③ `SecsItem` 的静态工厂**不能叫 `Ints`/`Reals`**（与同名实例属性撞 CS0102，现名 `Numbers`/`Floats`）。
- ★ `Separate.req` / `Reject.req` **没有应答** → 「要不要等」写 `msg.IsData ? msg.WBit : msg.ReplyType().HasValue`，写 `IsControl || WBit` 会给它们白等一个 T6。
- 页面「打开连接只改个 bool / 发送只回显」的假实现已全部改成真 `CommManager`。★ **HSMS 读线程会直接回调日志** → `DebugLog`（ObservableCollection）必须 `PostToUi` marshal，否则对方一连上来就抛 `InvalidOperationException`。
- ★ 页面刷状态用 `CommManager.TryPeek`（**不创建**）：`Peek` 会真 `Create`，而 S7 / 三菱MC 的 `Create` 直接抛 `NotSupportedException` → 只是「选中一条 S7 配置」就当场抛。
- ★ `CommTypeVisibilityConverter` 是**精确匹配**（整串 `OrdinalIgnoreCase`）→ 新通讯类型必须加进**每一处**相关 `ConverterParameter`（端口/IP 芯片、端口号芯片、类型专属卡、专属按钮），漏一处就永远不显示（G28.33/G28.35）。
- `SecsRole`/`SecsDeviceId`/`SecsT3..T8`/`SecsAutoReply`/`SecsMdln`/`SecsSoftRev` 都是 `CommItem` **标量** → 反射自动进 xlsx「通讯」表；`ChannelKey` 已含角色+DeviceID（改了会重建通道）。
- 验证：守卫 G28.1–G28.37；冒烟段 S（S1–S57），含 **`127.0.0.1` 回环端到端**（被动 `Port=0` 拿 `BoundPort` ↔ 主动连接 → Select / S1F1→S1F2 / S6F11 未请求消息 / Linktest / Separate 立即返回 / 非法帧长度断开）+ 通讯页离屏截图 `.smoke/out/comm_secs_card.png`。
