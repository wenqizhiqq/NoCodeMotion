# NoCodeMotion 长期项目笔记
> 陷阱索引。完整推导见 `.workbuddy-ai/memory/` 逐日日志（2026-09-19 起）。

## 一、构建 / 验证
- dotnet 用 PowerShell；先设 `[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)`，否则中文乱码。★ `dotnet … > log` 重定向出来的是 **GBK(cp936)** 不是 UTF-8 → 拿中文 pattern 去 `grep` 会**静默 0 匹配**（用 `iconv -f gbk` 或 Python `decode('gbk')`）。
- ★ **清理要复核**：`Remove-Item -Recurse -Force .\obj,.\bin` 会**静默不删**；bash `rm -rf obj bin` 会被沙箱**批量删除守卫**拦下（`SAFE_DELETE_BULK_CONFIRM_REQUIRED`，`&&` 短路致 `ls` 没跑）。两者都 → 构建 up-to-date 跳过 CoreCompile、只报 6 条架构告警，误判「全量成功」。**删完必须 `ls` 复核目录真的没了**。全量日志 ≈0.4 MB、结尾 `614 个警告 / 0 个错误`。**空或极短日志 = 没编译**。
- `NuGet … null ('path1')`/NETSDK1060 → shell `APPDATA`/`ProgramFiles` 空：`env "APPDATA=…" "ProgramFiles=C:\Program Files" … dotnet build`。
- ★ 源码文本守卫**必须在仓库目录跑**：`%TEMP%` 进程读 `D:\` 被沙箱替换（静默乱码，`Contains` 在肉眼可见串返 false）。
- ★ `%TEMP%` 会被系统临时清理整删（已两次）→ 自建工具全放仓库：守卫 `tools\guard_sources.py`（G1–G18，**173 PASS**）、看 BOM 源码 `tools\_dump.py`（内置 Read 会误判 binary）、冒烟 `.smoke\`。
- 冒烟：`dotnet build .smoke/ncm_smoke.csproj -p:UseAppHost=false` → `dotnet exec .smoke/bin/Debug/net10.0-windows/ncm_smoke.dll`（段 A–J）。`.smoke/bin|obj` 已被 .gitignore 覆盖。
- ★ 主工程 `<Reference>`+`HintPath` 的 dll **不进引用方 deps.json**，`dotnet exec` 严格按 deps.json → 冒烟要**照抄同样的 `<Reference>`**，否则 `FileNotFoundException`（产品自身不受影响）。
- ★ `%TEMP%` 里的 exe 跑不起来（`exit 127`、`taskkill` 拒绝访问留残进程锁 `*.exe`）→ 一律 `dotnet exec <dll>` + `-p:UseAppHost=false`。
- 补丁脚本范式：`load()` 断言纯 CRLF、`sub1` 断言 `count==1`、全部锚点过后 `flush()` 一次写盘；`insert_after` 看锚点末尾字符（可见字符先换行再插）。
- 冒烟与 `--no-incremental` 不可并行（互删 `obj\` → CS0103 / CS5001 / CS2001，不自愈）→ `rm -rf obj bin` 全量（删除成败见上「清理要复核」）。`-t:Rebuild` 跳过 CoreCompile 报假「0 警告」。
- 并发编辑器报假 CS1061 → 先看 mtime。临时目录带点前缀（`.smoke/`）不编译，不带点（`scratch/`）会被 `**/*.cs` 收走。

## 二、代码约定
- 隐式 using 只有 System/Collections.Generic/Linq/Threading/Tasks（无 System.Windows/System.IO）。
- 行尾/BOM 逐文件保持；头尾作者水印（零宽 U+200B/U+2063/U+200D）按字节保留；**XAML 无水印**；新 .cs 从既有文件**字节拼接**水印。
- 多编辑同一文件先内存累积、最后写盘；Python 多行字面量先归一化行尾。`core.autocrlf=true` → `git show HEAD:file` 行尾≠工作区，看工作区。
- ★ XAML `{StaticResource KEY}` 必须在 `App.xaml` 递归可达链里：`App.xaml` **只合并 `Resources\AppStyles.xaml`**，`Themes\AppleControls.xaml` 从未合并 → 写了它的键就在 `InitializeComponent()` 抛 `XamlParseException`。**grep 到 `x:Key` ≠ 运行时找得到，且 build 抓不到**。守卫 G17 已锁。
- `AppleToggle`（全局可用）46×26 **无文字开关**，模板不渲染 Content → 要标签须另配 TextBlock。**grep 到的样式先看它的 ControlTemplate 会不会渲染你要设的属性。**
- ★ **`TtBaseBtn` 的 `Padding` 硬编码在 ControlTemplate 里**（`Padding="14,0"`）→ 覆盖 `Padding` 无效，只能改 `MinWidth`/`Height`/`FontSize`。`TtPillBase` 系（`TtPill*`，圆角 15）的 `Padding` 是 Setter + `TemplateBinding` → **可覆盖**。次要操作（取消/清除）用浅灰 `TtPillGrayBtn`，别用饱和主色 `TtBaseBtn`。

## 三、架构要点
- `Catalog`（`Services/Catalog.cs`）是下拉框名称缓存，只在 `Load`/`LoadInto` 经 `SyncAllFromData` 重建，**不是数据源**；`Catalog.*Names` 是受限值域 → 写候选外的值渲染空白。
- 自动保存链 `Model.PropertyChanged → ListVM.OnItemPropertyChanged → ScheduleSave()`；**运行态/只读展示属性必须过滤**，否则每秒写盘。
- 点位「名称」用 0 基轴号（名里的数=轴号）；`XlsxProjectStore` 点位表列名固定 `轴1位置…轴1名`（4 轴槽），改了读不了老工程。
- ★ 重复轴号是静默灾难：`AxisOf` 的 `slot.Axes` 是 `Dictionary<轴号,IAxis>`，卡族读写不带卡号轴号 → 两轴同号共用物理通道。已有一次性告警 `_warnedAxisDuplicate`。
- ★ **过滤共享集合必须用私有 `CollectionViewSource`**：`GetDefaultView(coll)` 的 Filter **全局共享**；`IoPage` 与 `EngineerPage` 绑**同一** `ProjectStore.Data.Inputs/Outputs` → 工程师页只能用 `new CollectionViewSource { Source = … }`（守卫 G16.7）。

## 四、流程 / UI 页
- `TableToolbar`「共 N 项」来自 `MatchInfo`；行数权威是 `TargetGrid.ItemsSource`。
- 流程「名称」列是受限下拉（`FunctionToNamesConverter`→`Catalog.*Names`）；**不要为显示未知值把当前值并进 ItemsSource**（SelectedItem TwoWay 置空回写 null）。
- JSON 粘 AI 用 `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`。`RestoreFlows` 失败返 false 且不动 sink；空快照 `"[]"` 合法。
- `ExportSteps` 必须导出 `FlowStep.DurationMs`（键 `耗时`）：加字段时 `Export*` 与 `Fill*` 同改。
- Lua API 唯一权威是 `Services/HardwareApi.cs` 的 `Register()`；`Camera`/`Vision` 不注册。视觉流程契约是 `视觉步骤` 数组。
- FlowPage.xaml Row-0（复制JSON/粘贴生成/回退）四流程共用、在 `EditorPage.Detail` 里，**不能起 `x:Name`（MC3093）**。
- 「图像生成点位」= 工具栏按钮 + `Views/GraphGenDialog.xaml(.cs)`（`DataContext`=`GraphPointGenViewModel`，340×260）。`PointViewModel.ImageGenPointsCommand` 打开；`Graph.Generated → AppendGraphPoints()` 追加到当前工位（轴1←X、轴2←Y）。映射 `mx=OriginX+cx/CanvasW*FrameW`、`my=OriginY+(CanvasH-cy)/CanvasH*FrameH`。画布**最上层透明 `Border` 收单击**，底下 Path/ItemsControl 全 `IsHitTestVisible=False`。
- ★ **新建 Apple 弹窗别照抄 `Owner = Application.Current?.MainWindow;`**：本窗口若是应用里第一个 `Window`，getter 返回它自己 → `ArgumentException「不能把 Owner 设为自己」`。必须 `if (owner is not null && !ReferenceEquals(owner, this)) Owner = owner;`（`ArrayGenDialog` 仍是裸写法，未暴露）。
- 工程师页三列表（输入 IO / 输出 IO / 气缸）：各一个 `ListBox` + **`VirtualCardListStyle`**（虚拟化+回收+像素滚动+双滚动条 Auto）+ 顶部搜索行（`TtSearchBox` + `TtMatchInfo` 命中数 + 浅灰 `SearchClearBtn`「取消」，绑 `InputSearch`/`OutputSearch`/`CylinderSearch` 与 `Clear*SearchCommand`），`ItemsSource` 绑 `InputsView`/`OutputsView`/`CylindersView`；行**单行紧凑** `MiniCardStyle`+`Padding="10,5"`、**无固定 Width**（旧 IO 210 / 气缸 224）。**`ItemsControl`+外部 `ScrollViewer` 不虚拟化**，必须用 `ListBox`。
- 工程师页布局：外层 `Grid` 两列宽 `1.3*` / `*`（**左宽右窄**）——左列「IO 控制 + 气缸控制」（两 IO 表要放得下），右列「**轴控制 + 点位移动和设置**」（轴控紧贴点位表上方）。列宽一改，三列表实际行宽随之变（气缸行 224 → 619 px）。

## 五、暂停/停止守卫（Request B）
- 进程级静态 `HardwareBridge.WaitGuard`（`Action`）由所有阻塞等待（Delay、MoveAxis、Lua wait、气缸脉冲）每 ≤50ms 轮询。Pause → `ResumeEvent.Wait()`；Stop/E-Stop → **抛 `OperationCanceledException`**（不是 `ScriptRuntimeException`，否则被 `ExecuteLeaf` 的 `catch(Exception)` 吞掉、流程继续跑）。
- 每次运行 `HardwareBridge.BindWaitGuard(object ctrl)`（**反射**，避 Services→ViewModels 环依赖）；`HookWaitGuard`（起线程前）/`UnhookWaitGuard`（watchdog 收尾）。雷赛/研控 `WaitInterruptible` 必须读**静态** `HardwareBridge.WaitGuard`（实例字段无人赋值 → 永不响应暂停/停止，已踩过的静默 bug）。
- 气缸脉冲**不能**暂停在半途，只查 Stop，调用方重抛前把输出复位到安全电平。
- 速度路径：`ExecAxis` `prop=="速度"`→`SetAxisSpeed`；`ExecPoint` `slot.Speed>0`→`SetAxisSpeed`；Lua `HardwareApi.SetAxisSpeed`；NgRunner `MoveAxis`。
- **`FlowLoopManager` 是 `NoCodeMotion.ViewModels` 顶层静态类**；`FlowLoopManager.StopAll()` 停所有循环流程。

## 六、硬件层通用
- 雷赛：`LtdmcNative.cs` 唯一 P/Invoke。总线卡运动用 `dmc_*`（`profile_unit`/`pmove_unit`/`get_position_unit`/`check_done`(0=运动中,1=到位)/`stop`），伺服使能（`nmc_set_axis_enable`，CiA402）、回零、总线诊断分族。EtherCAT 端口固定 2；CiA402 只有状态 4 能动；`dmc_get_home_result` 的 `state==1` 才算成功；伺服使能脚低电平有效（写 0=使能）。
- 卡族入口 `CardFamilyCatalog.cs` + `WenQiZhiCardBridge.cs`；`AutoDetectFromProject()` 只有雷赛走 `LeadshineHardwareBridge`。`Families/` 目录名≠命名空间→断言 `Create().GetType().Namespace`。`Aliases` 必须含「型号名」+「固件卡型码」（`$"0x{码:X}"`，48 码）。
- 轴必须一轴一个 `IAxis` 实例；`GetCardAxisCurrentState()` 0=停止/1=运行中（反直觉）；`ListCardParam` 必须预置否则 `IndexOutOfRange`。模拟卡 `IsVitualCard=true` 跳过真实开卡。

## 七、研控 MCN420（当前真机）
- 使能极性：端口电平、低有效。`YK_set_sevon_config(card,axis,sevon_en)` 第三参=端口电平（写 0=使能）。四处同步：① `NmcSetCardAxisEnable` 写0使能/1不；② `GetAxisCurrentState()` bit8 在 `get==0` 置位；③ `NormalizeSevon(int)` 反转（0→1、1→0，**别改回恒等**）；④ `CardAxisWriteSevonPin` 契约不变。**规矩：参数语义看原生声明，极性看真机，不用错误码表反推。**
- 轴指令 unit 版，依赖卡内脉冲当量 `YK_set_command_ratio` = pulse/unit；桥接层**绝不能乘当量**（模拟卡由 `BuildSimParam` 自乘当量变 pps，1000pps 下限）；`EquivOf` 当量≤0 兜底 1。
- 错误码表 `Native/Mcn420ErrorCode.xml`（98 条，csproj 必须 `None+CopyToOutputDirectory`）：`8194`=普通缓冲满、`20480`=0通道轴未运动完成、`26`=使能出错、`60000`=轴被占用、`65535`=通讯异常。**8194/20480 与当量/使能无关，是卡内缓冲状态。**
- ★ **8194 真因 = `Channel=2`（非法通道）**：`Native/MCN420.cs` 请求结构体 `Channel` 注释都 `// 0 or 1`，SDK 硬编码写 2（含 Jog 走的 `DmcSetCardAxisProfileUnit`）→ 卡无法解析直接回 8194。`MaxInterpChannel=2` 是插补通道数常量、零引用。已全改 `Channel=0`。**错误码中文名会骗人 → 查不出先逐字段对原生 struct 值域，再 grep 那个字面量。**（辅助：`StartAxisJog` 非模拟卡在 `EnsureAxisRatio` 前先 `StopCardAxisMovement(cardNo,axis,1)` 清缓冲。）
- ★ **轴慢真因 = 加减速语义错**：`Accel/Decel` 是**加速时间(秒)**，不是加速度值。`BuildMotionParam`/`BuildHomeParam` 直传（`>0?值:0.2`）；`AxisItem._accel/_decel` 默认 0.2（旧默认 50 经 `ProjectData.MigrateAxisDefaults()` 迁移）。接口权威 `IAxis.cs`：MaxVel unit/s、TaccVel 单位 s。

## 八、多开 / 退出（Request C）
- `Services/SingleInstance.cs`：命名 Mutex `Local\NoCodeMotion_SingleInstance_<user>`；`EnsureSingleInstance()`→false 则 `App.OnStartup` 里 `Shutdown()`；冲突弹 `InstanceConflictWindow`；继续→`CloseOtherInstances`+`WaitOtherExit`+`WaitOne`。`Services/AppShutdown.cs` 的 `StopAndRelease()` → `FlowLoopManager.StopAll()` + 遍历 `ProjectStore.Data.Controllers` 调 `HardwareSetup.CardFamilies?.Disconnect(c)`。
- `MainWindow` Closing 设 `e.Cancel=true`，后台 Task 释放、`Dispatcher.Invoke` 更新 `ClosingProgressWindow`，最后 `Application.Current.Shutdown()`。

## 九、UI 验证硬约束（本机）
- 驱动 UI 用 `ctrl.GetInvokePattern().Invoke()`，不合成鼠标；导航项 `TextBlock` 沿 `GetParentControl()` 上溯到外层 `ButtonControl`。`uiautomation` 2.0.29 无 `GetSupportedPatterns()`。
- 每次跑前打印前景窗口/光标坐标。`PrintWindow` 不可靠（无桌面客户区全白）→ 以 UIA 文本为准。读 ComboBox 候选取它自己的 `ListItemControl` 子孙。
- ★ **`RenderTargetBitmap` 离屏渲染是可靠视觉证据**（真走可视化树），与「窗口截图不可信」是两回事；离屏 `Measure/Arrange`+`UpdateLayout()` 后读控件 `ActualWidth/ActualHeight` 可**量化**验证行高/行宽（冒烟 J 段；PNG 落 `.smoke\out\`）。**断言阈值别拍脑袋**（写 `>300` FAIL，实际 291 → 改判据为「比旧固定宽更宽」+「占列宽 ≥85%」）。

## 十、相机（海康 MVS）
- 入口 `Services/Camera/MvsCameraService.cs`（`NoCodeMotion.Services.Camera`）+ 托管封装 `Native/MvCameraControl.Net.dll`（csproj `<Reference>`+`HintPath`+`Private`）。**本机只装了托管封装、没装 MVS Runtime** → `DllNotFoundException MvCameraControl.dll 0x8007007E`，**必须优雅降级**。
- **会话按设备 Key 复用**（`SN:`→`IP:`→`IDX:`）：一台 GigE 只有一个独占句柄，相机页与流程视觉（`VisionEngine.CaptureFrame`）**共用同一已打开会话**。
- 封装**纯 IL**（.NET 10 可加载），`MyCamera` 无参公有构造、`MV_CC_EnumDevices_NET` 是**静态**。取图只有 `MV_CC_GetImageForBGR_NET`（**无**返回 `MV_FRAME_OUT` 的 API）→ 按 `nFrameLen` 反推布局（3=BGR、4=BGRA、1=Mono）。`MVCC_INTVALUE.nCurValue` 是 **`UInt32`**，转 int 前必须范围检查。
- `MV_CC_DEVICE_INFO_LIST` 在封装里是 `uint nDeviceNum; IntPtr[] pDeviceInfo;`（封送有坑）→ **手写镜像原生布局**（256 内联 `IntPtr` = 2056 B）。`SpecialInfo` 三个 `byte[]` 是**私有**定长缓冲 → `GCHandle.Alloc(Pinned)`+`PtrToStructure` 按 `nTLayerType` 解。
- 单位：曝光 **界面毫秒 → SDK 微秒（×1000）**；触发三态「连续/软触发/硬触发」→ `TriggerMode` Off / On+`TriggerSource` Software / On+Line0。
- ★ `CaptureFrame` 的**最后一层回退必须在 try 内**：`SyntheticCapture` 依赖 OpenCvSharp，OpenCV 原生库缺失时异常冲出会打断整条流程 → 已加纯托管兜底 `ManagedPlaceholder`，保证 **永不抛**。
- 检测算法未接入：`CameraItem.LastScore` 仍沿用仿真分数，保持 `{CamResultN}` 语义不变。
