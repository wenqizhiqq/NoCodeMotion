# NoCodeMotion 长期项目笔记
> 陷阱索引。完整推导见 `.workbuddy-ai/memory/` 逐日日志（2026-09-19 起）。

## 一、构建 / 验证
- dotnet 走独立 shell 工具（非 bash）；中文乱码先设 `[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false)`。★ `dotnet … > log` 出来是 **GBK(cp936)** → 中文 pattern `grep` **静默 0 匹配**（`iconv -f gbk`）。
- ★ **清理要复核**：`Remove-Item -Recurse -Force .\obj,.\bin` 静默不删；bash `rm -rf obj bin` 被沙箱**批量删除守卫**拦下（`SAFE_DELETE_BULK_CONFIRM_REQUIRED`，`&&` 短路）。构建 up-to-date 会跳过 CoreCompile → 误判「全量成功」，**删完必须 `ls` 复核**。全量 ≈0.4 MB / `626 个警告 0 个错误`；**空或极短日志 = 没编译**。
- ★ **build 非 0 时冒烟仍会跑旧 dll**（`exit=0`、输出是上一次的结果，看着「全部通过」）→ **先确认 build 的 `0 个错误`，再看冒烟**。build 偶发崩成 `exit=-1073741571`（0xC00000FD 栈溢出），重跑即可。
- ★ `dotnet exec` 严格按 deps.json → 主工程 `<Reference>`+`HintPath` 的 dll **不进引用方 deps.json**，冒烟要**照抄同样的 `<Reference>`**，否则 `FileNotFoundException`。`NETSDK1060` = shell `APPDATA`/`ProgramFiles` 空 → 显式 `env` 传入再 build。
- ★ 源码文本守卫**必须在仓库目录跑**（`%TEMP%` 进程读 `D:\` 被沙箱替换、静默乱码）；`%TEMP%` 还会被系统清理整删 → 自建工具全放仓库：守卫 `tools\guard_sources.py`（G1–G20，**187 PASS**）、看 BOM 源码 `tools\_dump.py`、冒烟 `.smoke\`。
- 冒烟：`dotnet build .smoke/ncm_smoke.csproj -p:UseAppHost=false` → `dotnet exec .smoke/bin/Debug/net10.0-windows/ncm_smoke.dll`（段 A–K）。★ `%TEMP%` 里的 exe 跑不起来（`exit 127`）→ 一律 `dotnet exec <dll>`。冒烟与 `--no-incremental` 不可并行（互删 `obj\` → CS0103/CS5001/CS2001）；`-t:Rebuild` 跳过 CoreCompile 报假「0 警告」。
- 补丁脚本：优先**按行号区间**替换（避开长文本精确匹配），或 `sub1` 断言 `count==1`、锚点全过后一次写盘。★ **别把 BOM 写成硬断言**（`MEMORY.md` 无 BOM → abort）。并发编辑器报假 CS1061 → 先看 mtime。临时目录带点前缀（`.smoke/`）不编译，不带点（`scratch/`）会被 `**/*.cs` 收走。

## 二、代码约定
- 隐式 using 只有 System/Collections.Generic/Linq/Threading/Tasks（无 System.Windows/System.IO）。
- 行尾/BOM 逐文件保持；头尾作者水印（零宽 U+200B/U+2063/U+200D）按字节保留；**XAML 无水印**；新 .cs 从既有文件**字节拼接**水印。**BOM 逐文件不同**（`Catalog.cs` 有、`MEMORY.md` 无）→ 先探再写。多编辑同一文件先内存累积、最后写盘。
- ★ XAML `{StaticResource KEY}` 必须在 `App.xaml` 递归可达链里：**只合并 `Resources\AppStyles.xaml`**，`Themes\AppleControls.xaml` 从未合并 → 写了它的键就在 `InitializeComponent()` 抛 `XamlParseException`。**grep 到 `x:Key` ≠ 运行时找得到，build 也抓不到**（守卫 G17）。
- `AppleToggle` 46×26 **无文字开关**，模板不渲染 Content → 要标签须另配 TextBlock。★ **`TtBaseBtn` 的 `Padding` 硬编码在 ControlTemplate 里** → 覆盖无效，只能改 `MinWidth`/`Height`/`FontSize`；`TtPill*` 的 `Padding` 是 Setter + `TemplateBinding` → **可覆盖**；次要操作（取消/清除）用浅灰 `TtPillGrayBtn`。**grep 到的样式先看它的 ControlTemplate 会不会渲染你要设的属性。**
- ★ **`ItemsControl.GroupStyle` 是只读 CLR 集合、不是依赖属性** → Style `<Setter>` 写不了，只能回调里 `Add`。`CollectionViewGroup` **不实现 `IEnumerable`**（用 `.Items`）；`GroupNameFromItem` 只有 3 参重载。★ **全局钩子用 `[ModuleInitializer]`，别用静态构造**（静态构造要等首次访问本类，漏一处引用就静默失效）。

## 三、架构要点
- `Catalog`（`Services/Catalog.cs`）是下拉框名称缓存，只在 `Load`/`LoadInto` 经 `SyncAllFromData` 重建，**不是数据源**；`Catalog.*Names` 是受限值域 → 写候选外的值渲染空白。
- 自动保存链 `Model.PropertyChanged → ListVM.OnItemPropertyChanged → ScheduleSave()`；**运行态/只读展示属性必须过滤**，否则每秒写盘。
- 点位「名称」用 0 基轴号（名里的数=轴号）；`XlsxProjectStore` 点位表列名固定 `轴1位置…轴1名`（4 轴槽），改了读不了老工程。
- ★ 重复轴号是静默灾难：`AxisOf` 的 `slot.Axes` 是 `Dictionary<轴号,IAxis>`，卡族读写不带轴号 → 两轴同号共用物理通道。
- ★ **过滤共享集合必须用私有 `CollectionViewSource`**：`GetDefaultView(coll)` 的 Filter **全局共享**；`IoPage` 与 `EngineerPage` 绑**同一** `ProjectData.Inputs/Outputs` → 工程师页只能 `new CollectionViewSource { Source = … }`（守卫 G16.7）。★ 反向：**分组**故意打在**默认视图**上（在 `Catalog` 一处改即全体生效）。

## 四、流程 / UI 页
- `TableToolbar`「共 N 项」来自 `MatchInfo`；行数权威是 `TargetGrid.ItemsSource`。
- 流程「名称」列是受限下拉（`FunctionToNamesConverter`→`Catalog.*Names`）；**别为显示未知值把当前值并进 ItemsSource**（SelectedItem TwoWay 置空回写 null）。
- JSON 粘 AI 用 `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`。`RestoreFlows` 失败返 false 且不动 sink；空快照 `"[]"` 合法。
- `ExportSteps` 必须导出 `FlowStep.DurationMs`（键 `耗时`）：加字段时 `Export*` 与 `Fill*` 同改。
- Lua API 唯一权威是 `Services/HardwareApi.cs` 的 `Register()`；`Camera`/`Vision` 不注册。视觉流程契约是 `视觉步骤` 数组。
- FlowPage.xaml Row-0（复制JSON/粘贴生成/回退）四流程共用、在 `EditorPage.Detail` 里，**不能起 `x:Name`（MC3093）**。
- 「图像生成点位」= 工具栏按钮 + `Views/GraphGenDialog.xaml(.cs)`（VM=`GraphPointGenViewModel`）；`Graph.Generated` → 追加到当前工位。画布**最上层透明 `Border` 收单击**，底下全 `IsHitTestVisible=False`。
- ★ **新建 Apple 弹窗别照抄 `Owner = Application.Current?.MainWindow;`**：本窗口若是应用里第一个 `Window`，getter 返回它自己 → `ArgumentException「不能把 Owner 设为自己」`。必须 `if (owner is not null && !ReferenceEquals(owner, this)) Owner = owner;`（`ArrayGenDialog` 未修）。
- 工程师页三列表（输入/输出 IO、气缸）：各一个 `ListBox` + **`VirtualCardListStyle`**（虚拟化+回收+像素滚动）+ 顶部搜索行（`TtSearchBox`、命中数、「取消」）+ `*Search` 过滤属性；行 `MiniCardStyle`+`Padding="10,5"`。**`ItemsControl`+外部 `ScrollViewer` 不虚拟化** → 必须 `ListBox`。
- 工程师页布局：外层 `Grid` 两列 star 宽（当前 `*` / `2*`）——左列「IO 控制 + 气缸控制」、右列「**轴控制 + 点位移动和设置**」（轴控紧贴点位表正上方）。★ **列宽比例是布局口味，别锁死**。
- 名称库下拉「级联二级菜单」（Request N）：**>20** 且至少一个名字能截前缀 → 按**第一个 `-`（半/全角）前**文本分组，截不出归「其他」；≤20 个保持普通平铺。数据三件套：`NamePrefixGroupDescription.cs` + `Catalog.ApplyGrouping`（改**默认视图**，含并集 `AllNames`）+ `NameGroupHeaderBehavior.cs`（`[ModuleInitializer]` 注册 ComboBox 类处理器 → 显式/内联/无 Style 三种下拉一律生效）。**条目仍是 `string`**。
- ★ **呈现**：一级只列分类行（`下料 (15) ›`），悬停/点击 → **右侧飞出**二级菜单列真实名称。= `GroupStyle.ContainerStyle`（`NameGroupCascadeContainerStyle`，TargetType=GroupItem）+ `NameGroupFlyout` 行为（在模板里按名找 `row`/`flyout` 部件；轮询判定「一级行 + 二级菜单」都没鼠标才收起；点收起后临时抑制悬停，否则立刻被弹开；`Unloaded` 时收掉，否则 Popup 会作为独立顶层窗口留在屏幕上）。
- ★ **条目宿主必须是 `<ItemsPresenter/>`**：`CellComboStyle` 下拉原本是裸 `<StackPanel IsItemsHost="True"/>`，**裸面板宿主只实体化最外层 GroupItem（= 一级分类行），组内条目一个都不生成** → 下拉里只剩几行分类标题、真实名称点不到（XAML 合法、build / 守卫 /「标题能渲染」测试全绿，只有真打开下拉才暴露）。已改 `ItemsPresenter`。守卫 G20/G21。

## 五、暂停/停止守卫（Request B）
- 进程级静态 `HardwareBridge.WaitGuard`（`Action`）所有阻塞等待（Delay/MoveAxis/Lua/气缸脉冲）每 ≤50ms 轮询。Pause → `ResumeEvent.Wait()`；Stop/E-Stop → **抛 `OperationCanceledException`**（不是 `ScriptRuntimeException`，否则被 `ExecuteLeaf` 的 `catch(Exception)` 吞掉）。
- 每次运行 `HardwareBridge.BindWaitGuard(object ctrl)`（**反射**，避 Services→ViewModels 环依赖）；`HookWaitGuard`/`UnhookWaitGuard`。雷赛/研控 `WaitInterruptible` 必须读**静态** `HardwareBridge.WaitGuard`（读实例字段 → 永不响应暂停/停止）。
- 气缸脉冲**不能**暂停在半途，只查 Stop，调用方重抛前把输出复位到安全电平。速度路径：`ExecAxis` `prop=="速度"`→`SetAxisSpeed`；`ExecPoint` `slot.Speed>0`→`SetAxisSpeed`；Lua `HardwareApi.SetAxisSpeed`；NgRunner `MoveAxis`。
- **`FlowLoopManager` 是 `NoCodeMotion.ViewModels` 顶层静态类**；`StopAll()` 停所有循环流程。

## 六、硬件层通用
- 雷赛：`LtdmcNative.cs` 唯一 P/Invoke。总线卡运动 `dmc_*`（profile_unit/pmove_unit/get_position_unit/check_done/stop），伺服使能 `nmc_set_axis_enable`（CiA402）。EtherCAT 端口固定 2；CiA402 只有状态 4 能动；`dmc_get_home_result` `state==1` 才算成功。
- 卡族入口 `CardFamilyCatalog.cs` + `WenQiZhiCardBridge.cs`；`AutoDetectFromProject()` 只有雷赛走 `LeadshineHardwareBridge`。`Families/` 目录名≠命名空间→断言 `Create().GetType().Namespace`。`Aliases` 必须含型号名 + 固件卡型码（`$"0x{码:X}"`）。轴必须一轴一个 `IAxis` 实例；`GetCardAxisCurrentState()` 0=停止/1=运行中（反直觉）；`ListCardParam` 必须预置否则 `IndexOutOfRange`；模拟卡 `IsVitualCard=true` 跳过真实开卡。

## 七、研控 MCN420（当前真机）
- 使能极性：端口电平、低有效。`YK_set_sevon_config(card,axis,sevon_en)` 第三参=端口电平（写 0=使能）。四处同步：① `NmcSetCardAxisEnable` 写0使能/1不；② `GetAxisCurrentState()` bit8 在 `get==0` 置位；③ `NormalizeSevon(int)` 反转（**别改回恒等**）；④ `CardAxisWriteSevonPin` 契约不变。**规矩：参数语义看原生声明，极性看真机，不用错误码表反推。**
- 轴指令 unit 版，依赖卡内脉冲当量 `YK_set_command_ratio` = pulse/unit；桥接层**绝不能乘当量**（模拟卡由 `BuildSimParam` 自乘当量变 pps，1000pps 下限）；`EquivOf` 当量≤0 兜底 1。错误码表 `Native/Mcn420ErrorCode.xml`（98 条，csproj 必须 `None+CopyToOutputDirectory`）——**字面含义别当真**。
- ★ **8194 真因 = `Channel=2`（非法通道）**：请求结构体 `Channel` 注释 `// 0 or 1`，SDK 硬编码写 2（含 Jog 走的 `DmcSetCardAxisProfileUnit`）→ 卡回 8194。已全改 `Channel=0`。**查不出先逐字段对原生 struct 值域，再 grep 那个字面量。**
- ★ **轴慢真因 = 加减速语义错**：`Accel/Decel` 是**加速时间(秒)**，不是加速度值。`BuildMotionParam`/`BuildHomeParam` 直传（`>0?值:0.2`）；`AxisItem._accel/_decel` 默认 0.2（旧默认 50 经 `MigrateAxisDefaults()` 迁移）。接口权威 `IAxis.cs`：MaxVel unit/s、TaccVel 单位 s。

## 八、多开 / 退出（Request C）
- `Services/SingleInstance.cs`：命名 Mutex `Local\NoCodeMotion_SingleInstance_<user>`；`EnsureSingleInstance()`→false 则 `App.OnStartup` `Shutdown()`；冲突弹 `InstanceConflictWindow`；继续→`CloseOtherInstances`+`WaitOtherExit`+`WaitOne`。`AppShutdown.StopAndRelease()` → `FlowLoopManager.StopAll()` + 遍历 `Controllers` 调 `CardFamilies?.Disconnect(c)`。`MainWindow` Closing 设 `e.Cancel=true`，后台 Task 释放、`Dispatcher.Invoke` 更新进度窗，最后 `Application.Current.Shutdown()`。

## 九、UI 验证硬约束（本机）
- 驱动 UI 用 `ctrl.GetInvokePattern().Invoke()`，不合成鼠标；导航项 `TextBlock` 沿 `GetParentControl()` 上溯到外层 `ButtonControl`。`uiautomation` 2.0.29 无 `GetSupportedPatterns()`。`PrintWindow` 不可靠（无桌面客户区全白）→ 以 UIA 文本为准。读 ComboBox 候选取它自己的 `ListItemControl` 子孙。
- ★ **`RenderTargetBitmap` 离屏渲染是可靠视觉证据**（真走可视化树）；`Measure/Arrange`+`UpdateLayout()` 后读 `ActualWidth/ActualHeight` 可**量化**验证。★ 元素是**活对象**：重排后 `ActualWidth` **就地更新** → 测拉伸要在改尺寸**前**取值。★ **阈值/比例都别锁死** → 断言**关系**（≥列宽 85%、随列宽单调增、两列铺满）。
- ★ 离屏搭件：**裸控件取不到主题模板**（`GetChildrenCount==0`）→ 自己给最小 `ControlTemplate`；框架 `Loaded` 不会自动触发 → 手动 `RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent))`。★ 计数**按自定义 `Tag`/`Name`**，别数全部 `Border`（分组头模板本身也是 `Border`）。★ **关着的 `Popup` 内部可视化树不实例化** → 查它要走**逻辑属性**（`Popup.Child` → `Border.Child` → `ScrollViewer.Content`），`VisualTreeHelper` 找不到。★ 离屏条目模板只给裸 `TextBlock` 而不绑 `Text` → 条目占位却渲染空白，看着像「有高度没内容」。★ 也别数整棵树里的 `Path`/`Shape`：`ScrollViewer` 自带滚动条的箭头也是 `Path`（会多算）→ 只在目标控件**内部**数。

## 十、相机（海康 MVS）
- 入口 `Services/Camera/MvsCameraService.cs` + 托管封装 `Native/MvCameraControl.Net.dll`（csproj `<Reference>`+`HintPath`+`Private`）。**本机只有托管封装、没装 MVS Runtime** → `DllNotFoundException MvCameraControl.dll 0x8007007E`，**必须优雅降级**。**会话按设备 Key 复用**（`SN:`→`IP:`→`IDX:`）：一台 GigE 只有一个独占句柄，相机页与流程视觉**共用同一会话**。
- 封装**纯 IL**（.NET 10 可加载），`MyCamera` 无参公有构造、`MV_CC_EnumDevices_NET` 是**静态**。取图只有 `MV_CC_GetImageForBGR_NET` → 按 `nFrameLen` 反推布局（3=BGR、4=BGRA、1=Mono）；`nCurValue` 是 `UInt32`。曝光 **界面毫秒 → SDK 微秒（×1000）**；触发三态「连续/软/硬」→ `TriggerMode` Off / On+`TriggerSource` Software / On+Line0。
- `MV_CC_DEVICE_INFO_LIST` 封送有坑 → **手写镜像原生布局**（256 内联 `IntPtr`=2056 B）；`SpecialInfo` 是私有定长缓冲 → 必须 `GCHandle.Alloc(Pinned)` 再 `PtrToStructure`。★ `CaptureFrame` 的**最后一层回退必须在 try 内**：`SyntheticCapture` 依赖 OpenCvSharp，原生库缺失时异常冲出会打断整条流程 → 已加纯托管兜底 `ManagedPlaceholder`，保证 **永不抛**。
