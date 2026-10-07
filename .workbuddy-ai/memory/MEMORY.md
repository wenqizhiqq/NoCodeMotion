# NoCodeMotion 长期项目笔记
> 精炼索引，只留最容易重复踩、一句话说不清的坑。完整推导在 `.workbuddy-ai/memory/` 逐日日志（2026-09-19 起）。

## 一、构建 / 验证
- **用 PowerShell 跑 dotnet，不要 Bash**：Bash 输出全空、`*> log` 是 **UTF-16LE**，解码 `iconv -f UTF-16LE -t UTF-8` 再 grep；`Get-Item` 核 DLL 大小/时间戳。**空日志 ≠ 成功**。
- `NuGet … null ('path1')`/`NETSDK1060` → shell `APPDATA`/`ProgramFiles` 空：补 `env "APPDATA=…" "ProgramFiles=C:\Program Files" "ProgramFiles(x86)=…" dotnet build`（bash 不能 export 带括号名）。
- **★ `%TEMP%` 进程读 `D:\` 拿沙箱替换（静默乱码）→ 源码文本守卫必须在仓库目录跑**（Python 可靠）。症状：`Contains` 在肉眼可见串返回 false。
- 冒烟工程 `%TEMP%\ncm_smoke\`（`ProjectReference` 指本工程、`net10.0-windows`+`UseWPF=true`、**须在工程目录外**）；守卫 **`tools\guard_sources.py`**（G1–G18，仓库目录跑，160 项全 PASS）。看 BOM 源码用 **`tools\_dump.py`**（内置 Read 会把 BOM+CRLF 文件误判成 binary）。
- **★ 主工程用 `<Reference>`+`HintPath` 引的 dll 不会进引用方的 `deps.json`**，而 `dotnet exec` **严格按 deps.json 解析** → 冒烟工程必须**照抄一份同样的 `<Reference>`**，否则运行期 `FileNotFoundException`（**产品自身 deps.json 有记录、不受影响**）。
- **★ `%TEMP%` 里的 exe 本机跑不起来**：bash 直接执行报 `exit 127`，`taskkill` 也「拒绝访问」留残进程锁住 `*.exe` → 一律 **`dotnet exec <dll>`** 运行；构建加 **`-p:UseAppHost=false`** 绕开被锁的 exe。
- **★ 补丁脚本一律写仓库内 `tools\_patch_*.py`，不要放 `%TEMP%\ncm_patch\`**——2026-10-07 实测 `%TEMP%` 被系统临时清理**整个删掉**，守卫 + `patch_lib.py` 一起没了（只剩最新一个文件）。范式：`load()` 断言纯 CRLF、`sub1(old,new,tag)` 逐块断言 `count==1`、**全部锚点通过后再 `flush()` 统一写盘**（任一失败则一个字都不写）。插方法的 `insert_after(anchor,text)` 看**锚点末尾字符**：可见字符则先换行再插（防方法粘同行），换行则直接插。
- 冒烟与 `--no-incremental` 构建不可并行（互删 `obj\` → `CS0103 InitializeComponent`/`CS5001 无 Main`/`CS2001 缺 *.g.cs`，不自愈）→ 见这三类错先怀疑 obj 污染，`rm -rf obj bin` 全量 build。`-t:Rebuild` 跳过 CoreCompile 报假「0 警告」。
- 与别的编辑器并发时构建会报真实感极强的假 `CS1061` → 先看 mtime 再重跑。临时目录带点前缀（`.bt/`）不编译，不带点（`scratch/` `tmp/`）会被 `**/*.cs` 收走。

## 二、代码约定
- 隐式 using 只有 System/Collections.Generic/Linq/Threading/Tasks（无 System.Windows、无 System.IO）。
- 行尾/BOM 逐文件判断，保持该文件风格；头尾作者水印注释（含零宽 U+200B/U+2063/U+200D）按字节保留。
- 多编辑同一文件先内存累积、最后一次写盘；写回不再做行尾转换（产 CRCRLF）。Python 多行字面量先归一化行尾再匹配，注入块里不写 `\uXXXX`。
- `core.autocrlf=true` → `git show HEAD:file` 行尾与工作区不一致，看工作区。
- **★ XAML 写 `{StaticResource KEY}` 前必须确认 KEY 在 `App.xaml` 的 `MergedDictionaries` 递归可达链里**：`App.xaml` **只合并了 `Resources\AppStyles.xaml`**；`Themes\AppleControls.xaml` 从未被合并 → 它里面的键（`AppleCheckBox` 等）写了就在 `InitializeComponent()` 抛 `XamlParseException「无法找到名为 X 的资源」`。**grep 到 `x:Key` ≠ 运行时找得到**，且 **`dotnet build` 抓不到**（StaticResource 是运行期解析，构建照样 0 错误）。守卫 **G17** 已静态锁死（沿 App.xaml 链递归求可达集 + 带理由的 allowlist）。
- `AppleToggle`（全局可用）是 46×26 的**无文字开关**，`ControlTemplate` 不渲染 `Content` → 要显示「折线模式」必须另配 `TextBlock` 标签（其它页面都这么用）。**用 grep 找到的样式，先打开它的 `ControlTemplate` 确认会渲染你要设的属性。**

## 三、架构要点
- `Catalog`（`Services/Catalog.cs`）是下拉框全局名称缓存，只在 `Load`/`LoadInto` 经 `SyncAllFromData` 重建，**不是数据源**。`Catalog.*Names` 是受限值域 → 写候选外的值渲染成空白；写值前断言 `Contains(值)`。
- 自动保存链：`Model.PropertyChanged → … → ListVM.OnItemPropertyChanged → ScheduleSave()`。**运行态/只读展示属性必须过滤**，否则每秒写盘。
- 点位的「名称」用同一个 0 基轴号（名里的数=轴号）；`XlsxProjectStore` 点位表列名固定 `轴1位置…轴1名`（4 轴槽 schema），改了读不了老工程。
- ★ 重复轴号是静默灾难：`AxisOf` 里 `slot.Axes` 是 `Dictionary<轴号,IAxis>`，卡族读位置/回零/状态不带卡号轴号 → 两轴同号共用物理通道、卡内缓冲互相抢占。已加一次性告警 `_warnedAxisDuplicate`。

## 四、流程 / UI 页
- `TableToolbar`「共 N 项」来自 `MatchInfo`；行数权威是 `TargetGrid.ItemsSource`。
- 流程「名称」列是受限下拉（`FunctionToNamesConverter`→`Catalog.*Names`），值不在库空白；**不要为显示未知值把当前值并进 ItemsSource**（`SelectedItem` TwoWay 会置空回写 null）。
- JSON 粘 AI 用 `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`。快照/回退 `RestoreFlows` 失败返 false 且不动 sink；空快照 `"[]"` 合法。
- `ExportSteps` 必须导出 `FlowStep.DurationMs`（键 `耗时`）：加字段时 `Export*` 与 `Fill*` 同改。
- Lua 真实 API 唯一权威是 `Services/HardwareApi.cs` 的 `Register()`；`Camera`/`Vision` 确定不注册。视觉流程契约是 `视觉步骤` 数组。
- FlowPage.xaml Row-0 是「复制JSON/粘贴生成/回退」落点、四种流程共用，在 `EditorPage.Detail` 里，**不能起 `x:Name`（MC3093）**。
- 「图像生成点位」（**工具栏按钮**，2026-10-07 由点位页右上角悬浮卡改成弹窗）：`GraphPointGenViewModel`（画布常量 `CanvasW=340`/`CanvasH=260`）+ `PointViewModel.Graph` → `Generated` 事件 → `AppendGraphPoints()` 追加到当前工位（轴1←X、轴2←Y）。映射 `mx=OriginX+cx/CanvasW*FrameW`、`my=OriginY+(CanvasH-cy)/CanvasH*FrameH`（画布 Y 向下→机器 Y 向上）。**画布与参数现在都在 `Views/GraphGenDialog.xaml`**（`DataContext` = `GraphPointGenViewModel`），由 `PointViewModel.ImageGenPointsCommand` 打开；点位页右上角只剩「轨迹仿真」。画布**最上层是透明 `Border` 收单击**（`MouseLeftButtonDown="GraphCanvas_MouseLeftButtonDown"`，处理函数已随画布搬进弹窗 code-behind），底下 Path/ItemsControl 全 `IsHitTestVisible=False`。
- ★ **新建 Apple 风格弹窗时别照抄 `Owner = Application.Current?.MainWindow;`**：若本窗口恰好是应用里第一个 `Window`，`Application.MainWindow` 的 getter 会**返回它自己**（未显式设置时返回 `Windows` 集合里第一个）→ 赋值直接抛 `ArgumentException「不能把 Owner 设为自己」`。必须 `if (owner is not null && !ReferenceEquals(owner, this)) Owner = owner;`（`ArrayGenDialog` 还是裸写法，真机上主窗口先建所以没暴露）。
- **工程师页 IO 列表（500 项流畅）**：输入/输出各自一个 `ListBox` 用 `IoVirtualListStyle`（`VirtualizingPanel.IsVirtualizing` + `Recycling` + `ScrollUnit=Pixel` + 双滚动条 `Auto` + `CanContentScroll=True`）。**`ItemsControl` + 外部 `ScrollViewer` 不会虚拟化**（WrapPanel 拿无限高度），要虚拟化必须用 `ListBox`。

## 五、暂停/停止守卫（Request B）
- 进程级静态 `HardwareBridge.WaitGuard`（`Action`）由所有阻塞等待（Delay、MoveAxis 等待、Lua wait、气缸脉冲）每 ≤50ms 轮询。
- Pause → 阻塞在 `ResumeEvent.Wait()`；Stop/E-Stop → **抛 `OperationCanceledException`（不是 `ScriptRuntimeException`，否则被 `ExecuteLeaf` 的 `catch(Exception)` 吞掉、流程继续跑）**。
- 每次运行用 `HardwareBridge.BindWaitGuard(object ctrl)`（**反射**，`FlowRunControl` 在 ViewModels，避 Services→ViewModels 环依赖）绑定；`HookWaitGuard`（RunAllAsync 起线程前）/`UnhookWaitGuard`（watchdog 收尾）。
- 雷赛/研控卡族 `WaitInterruptible` 必须读**静态** `HardwareBridge.WaitGuard`（实例 `WaitGuard` 无人赋值 → 永远不响应暂停/停止，是已踩过的静默 bug）。
- 气缸脉冲宽度安全：脉冲输出**不能**暂停在半途，只查 Stop，调用方在重抛前把输出复位到安全电平。
- 速度可改路径：`ExecAxis` `prop=="速度"`→`SetAxisSpeed`；`ExecPoint` `slot.Speed>0`→`SetAxisSpeed`；Lua `HardwareApi.SetAxisSpeed`；NgRunner `MoveAxis`。
- **`FlowLoopManager` 是 `NoCodeMotion.ViewModels` 的顶层静态类**（不是 `FlowRunnerService` 嵌套）；停所有循环流程用 `FlowLoopManager.StopAll()`。

## 六、硬件层通用
- 雷赛：`LtdmcNative.cs` 唯一 P/Invoke（原 `LtdmcSdk.cs` 774 个零引用、已删）。总线卡「运动」仍用 `dmc_*`（`profile_unit`/`pmove_unit`/`get_position_unit`/`check_done`(0=运动中,1=到位)/`stop`），只有伺服使能（`nmc_set_axis_enable`，CiA402）、回零、总线诊断分族。EtherCAT 端口固定 2；CiA402 只有状态 4 能动；`dmc_get_home_result` 的 `state==1` 才算成功。伺服使能脚低电平有效（写 0=使能）。
- 卡族层入口 `CardFamilyCatalog.cs` + `WenQiZhiCardBridge.cs`；`HardwareSetup.AutoDetectFromProject()` 只有雷赛工程走 `LeadshineHardwareBridge`，非雷赛卡族才切 `WenQiZhiCardBridge`。`Families/` 目录名≠命名空间→断言 `Create().GetType().Namespace`。`Aliases` 必须含「型号名」+「固件卡型码」（`$"0x{码:X}"`，48 码），否则混装掉兜底配错卡。
- 轴必须一轴一个 `IAxis` 实例；`GetCardAxisCurrentState()` 0=停止/1=运行中（反直觉）；`ListCardParam` 必须预置否则 `IndexOutOfRange`。模拟卡 `IsVitualCard=true` 跳过真实开卡。

## 七、研控 MCN420（当前真机）
- 使能极性：端口电平、低有效。`YK_set_sevon_config(card,axis,sevon_en)` 第三参=端口电平（写 0=使能/写 1=不使能）。四处同步：① `NmcSetCardAxisEnable` 写0使能/1不；② `GetAxisCurrentState()` bit8 在 `get==0` 置位；③ `NormalizeSevon(int)` 反转映射（0→1、1→0，**别改回恒等**）；④ `CardAxisWriteSevonPin` 入参契约不变（0→Disable、非0→Enable）。**规矩：参数语义看原生声明，极性看真机，不用错误码表反推。**
- 轴指令 unit 版，依赖卡内脉冲当量 `YK_set_command_ratio` = pulse/unit；桥接层**绝不能乘当量**（模拟卡由 `BuildSimParam` 自乘当量变 pps，1000pps 下限）；`EquivOf` 当量≤0 兜底 1。
- 错误码表 `Native/Mcn420ErrorCode.xml`（98 条，随程序发布，csproj 必须 `None+CopyToOutputDirectory`）：`8194`=普通缓冲满、`20480`=0通道轴未运动完成、`26`=使能出错、`60000`=轴被占用、`65535`=通讯异常。**8194/20480 与当量/使能无关，是卡内缓冲状态。**
- ★ **8194 真因 = `Channel=2`（非法通道）**：`Native/MCN420.cs` 所有请求结构体 `Channel` 注释都 `// 0 or 1`，SDK 硬编码写了 2（含 `DmcSetCardAxisProfileUnit` 重建的 Jog 参数，Jog 实际走这条）→ 卡无法解析直接回 8194。`MaxInterpChannel=2` 是插补通道数常量、零引用，是「把范围 0..1 误当取值 2」的来源。已全改 `Channel=0`。**错误码中文名会骗人 → 查不出先逐字段对原生 struct 值域，再 grep 整个文件那个字面量。**
- 8194 辅助处置（保留非主因）：`StartAxisJog` 非模拟卡在 `EnsureAxisRatio` 前先 `StopCardAxisMovement(cardNo,axis,1)`（立即停）清缓冲；`ClearBufferHint` 读 `GetCmdListBufNum` 并同时 `Log()`+`StatusBarService.ReportInfo()`（手动 Jog 不经 Lua，`HardwareLog.Sink` 为 null，只 Log 现场看不见）。
- **★ 轴慢真因 = 加减速语义错**：`Accel/Decel` 必须是**加速时间(秒)**，不是加速度值。`BuildMotionParam`/`BuildHomeParam` 直传（`>0?值:0.2`）；`AxisItem._accel/_decel` 默认 0.2（旧默认 50 经 `ProjectData.MigrateAxisDefaults()` 迁移）；雷赛 `LtdmcCard` 同口径。接口权威 `IAxis.cs`：MaxVel unit/s、TaccVel 单位 s。轴页标签改「加速时间/减速时间」。

## 八、多开 / 退出（Request C）
- `Services/SingleInstance.cs`：命名 Mutex `Local\NoCodeMotion_SingleInstance_<user>`；`EnsureSingleInstance()`→true 启动/false 取消；冲突弹 `InstanceConflictWindow`（关闭其它并打开 / 取消打开）；继续→`CloseOtherInstances`（`CloseMainWindow()` 再 `Kill()`）+`WaitOtherExit`+`WaitOne`。
- `Services/AppShutdown.cs`：`StopAndRelease()`→`FlowLoopManager.StopAll()` 然后遍历 `ProjectStore.Data.Controllers` 调 `HardwareSetup.CardFamilies?.Disconnect(c)`。
- `App.xaml.cs` OnStartup 调 `SingleInstance.EnsureSingleInstance()` 否则 `Shutdown()`；`MainWindow.xaml.cs` Closing 设 `e.Cancel=true`，后台 Task 跑释放、用 `Dispatcher.Invoke` 更新 `ClosingProgressWindow` 的 ProgressBar，最后 `Application.Current.Shutdown()`。

## 九、UI 验证硬约束（本机）
- 驱动 UI 用 `ctrl.GetInvokePattern().Invoke()`，不合成鼠标；导航项 `TextBlock` 沿 `GetParentControl()` 上溯到外层 `ButtonControl`。`uiautomation` 2.0.29 无 `GetSupportedPatterns()`。
- 「本机无交互桌面」旧结论已不成立 → 每次跑前先打印前景窗口/光标坐标。
- `PrintWindow` 可靠性不可假定（无桌面客户区全白）→ 以 UIA 文本为准，截图不当视觉证据。
- 读 ComboBox 候选取它自己的 `ListItemControl` 子孙；断言计数标签连粘两次不同条数。现成脚本 `%TEMP%\ncm_ui\verify_dropdowns2.py`（managed python 跑）。

## 十、相机（海康 MVS）
- 入口 `Services/Camera/MvsCameraService.cs`（`NoCodeMotion.Services.Camera`）+ 托管封装 `Native/MvCameraControl.Net.dll`（csproj `<Reference>`+`HintPath`+`Private`）。**原生 `MvCameraControl.dll` 本机没装（只装了托管封装）→ 必须优雅降级，不能假设真机可用**。
- **会话按设备 Key 复用**（`SN:` → `IP:` → `IDX:`）：一台 GigE 设备只有一个独占句柄，相机页与流程视觉（`VisionEngine.CaptureFrame`）**共用同一个已打开会话**，不要各开各的。
- 封装是**纯 IL**（.NET 10 可加载），`MyCamera` 无参公有构造、`MV_CC_EnumDevices_NET` 是**静态**、其余实例。取图只有 `MV_CC_GetImageForBGR_NET`（**没有**返回 `MV_FRAME_OUT` 的 API）→ 按 `nFrameLen` 反推像素布局（3 B/px=BGR、4=BGRA、1=Mono），避开像素类型常量。`MVCC_INTVALUE.nCurValue` 是 **`UInt32`**，转 int 前必须范围检查。
- `MV_CC_DEVICE_INFO_LIST` 在封装里是 `uint nDeviceNum; IntPtr[] pDeviceInfo;`（封送有坑）→ 用**手写镜像原生布局**（256 个内联 `IntPtr` = 2056 B）。`SpecialInfo` 的三个 `byte[]` 是**私有**定长缓冲 → `GCHandle.Alloc(Pinned)` + `PtrToStructure` 按 `nTLayerType` 解。
- 单位：曝光 **界面毫秒 → SDK 微秒（×1000）**；触发三态「连续/软触发/硬触发」→ `TriggerMode` Off / On+`TriggerSource` Software / On+Line0。
- ★ `CaptureFrame` 的**最后一层回退必须在 try 内**：`SyntheticCapture` 自己依赖 OpenCvSharp，OpenCV 原生库缺失时异常会直接冲出、把流程「相机」步骤整条打断 → 已加纯托管兜底 `ManagedPlaceholder`（不碰任何原生库），保证 `CaptureFrame` **永不抛**。
- 检测算法未接入：`CameraItem.LastScore` 仍沿用仿真分数，保持下游 `{CamResultN}` 语义不变。
