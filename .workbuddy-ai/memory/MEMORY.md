# NoCodeMotion 长期项目笔记
> 陷阱索引。完整推导见 `.workbuddy-ai/memory/` 逐日日志（2026-09-19 起）。
> ★ 硬件层细节（雷赛 / 研控 MCN420 / 卡族 / 多开退出 / 海康相机）已拆到同目录 **`HARDWARE.md`**，动硬件前先读它。

## 一、构建 / 验证
- dotnet 走独立 shell 工具（非 bash）；中文乱码先设 `[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false)`。★ `dotnet … > log` 是 **GBK(cp936)** → 中文 pattern `grep` **静默 0 匹配**（`iconv -f gbk`；或加 `DOTNET_CLI_UI_LANGUAGE=en-US`）。
- ★ **清理要复核**：`Remove-Item -Recurse -Force` 静默不删；bash `rm -rf obj bin` 被沙箱**批量删除守卫**拦下（`SAFE_DELETE_BULK_CONFIRM_REQUIRED`，`&&` 短路）。up-to-date 跳过 CoreCompile → 误判「全量成功」，**删完必须 `ls` 复核**。全量 ≈0.4 MB / `626 警告 0 错误`；**空或极短日志 = 没编译**。
- ★ **build 非 0 时冒烟仍会跑旧 dll**（`exit=0`、输出是上一次的，看着「全部通过」）→ **先确认 build 的 `0 个错误` 再看冒烟**。build 偶发 `exit=-1073741571`（0xC00000FD 栈溢出），重跑即可。
- ★ `dotnet exec` 严格按 deps.json → 主工程 `<Reference>`+`HintPath` 的 dll **不进引用方 deps.json**，冒烟要**照抄同样的 `<Reference>`**。`NETSDK1060` = shell `APPDATA`/`ProgramFiles` 空 → 显式 `env` 传入。
- ★ 源码守卫**必须在仓库目录跑**（`%TEMP%` 进程读 `D:\` 被沙箱替换成乱码；`%TEMP%` 还会被系统清理整删）→ 工具全放仓库：`tools\guard_sources.py`（G1–G22，**208 PASS**）、`tools\_dump.py`（看 BOM）、`.smoke\`。
- 冒烟：`dotnet build .smoke/ncm_smoke.csproj -p:UseAppHost=false` → `dotnet exec .smoke/bin/Debug/net10.0-windows/ncm_smoke.dll`（段 A–L，**150 PASS**）。★ `%TEMP%` 里的 exe 跑不起来（`exit 127`）→ 一律 `dotnet exec <dll>`。冒烟与 `--no-incremental` 不可并行（互删 `obj\`）；`-t:Rebuild` 跳过 CoreCompile 报假「0 警告」。
- 补丁脚本：优先按行号区间替换，或 `sub1` 断言 `count==1`、锚点全过后一次写盘。★ **别把 BOM 写成硬断言**（`MEMORY.md` 无 BOM → abort）。并发编辑器报假 CS1061 → 先看 mtime。临时目录带点前缀（`.smoke/`）不编译，不带点（`scratch/`）会被 `**/*.cs` 收走。

## 二、代码约定
- 隐式 using 只有 System/Collections.Generic/Linq/Threading/Tasks（无 System.Windows/System.IO）。
- 行尾/BOM 逐文件保持；头尾作者水印（零宽 U+200B/U+2063/U+200D）按字节保留；**XAML 无水印**；新 .cs 从既有文件**字节拼接**水印。**BOM 逐文件不同** → 先探再写。多编辑同一文件先内存累积、最后写盘。
- ★ XAML `{StaticResource KEY}` 必须在 `App.xaml` 递归可达链里：**只合并 `Resources\AppStyles.xaml`**，`Themes\AppleControls.xaml` 从未合并 → 写了它的键就在 `InitializeComponent()` 抛 `XamlParseException`。**grep 到 `x:Key` ≠ 运行时找得到，build 也抓不到**（守卫 G17）。
- `AppleToggle` 46×26 **无文字开关**，模板不渲染 Content → 要标签另配 TextBlock。★ **`TtBaseBtn` 的 `Padding` 硬编码在 ControlTemplate 里** → 覆盖无效，只能改 `MinWidth`/`Height`/`FontSize`；`TtPill*` 的 `Padding` 是 Setter + `TemplateBinding` → **可覆盖**；次要操作用浅灰 `TtPillGrayBtn`。**grep 到的样式先看它模板会不会渲染你要设的属性。**
- ★ **`ItemsControl.GroupStyle` 是只读 CLR 集合、不是依赖属性** → Style `<Setter>` 写不了，只能回调里 `Add`。`CollectionViewGroup` **不实现 `IEnumerable`**（用 `.Items`）；`GroupNameFromItem` 只有 3 参重载。★ **全局钩子用 `[ModuleInitializer]`，别用静态构造**（要等首次访问本类，漏一处引用就静默失效）。

## 三、架构要点
- `Catalog`（`Services/Catalog.cs`）是下拉框名称缓存，只在 `Load`/`LoadInto` 经 `SyncAllFromData` 重建，**不是数据源**；`Catalog.*Names` 是受限值域 → 写候选外的值渲染空白。
- 自动保存链 `Model.PropertyChanged → ListVM.OnItemPropertyChanged → ScheduleSave()`；**运行态/只读展示属性必须过滤**，否则每秒写盘。
- 点位「名称」用 0 基轴号（名里的数=轴号）；`XlsxProjectStore` 点位表列名固定 `轴1位置…轴1名`（4 轴槽），改了读不了老工程。
- ★ 重复轴号是静默灾难：`AxisOf` 的 `slot.Axes` 是 `Dictionary<轴号,IAxis>`，卡族读写不带轴号 → 两轴同号共用物理通道。
- ★ **过滤共享集合必须用私有 `CollectionViewSource`**：`GetDefaultView(coll)` 的 Filter **全局共享**；`IoPage`/`EngineerPage` 绑**同一** `ProjectData.Inputs/Outputs` → 工程师页只能 `new CollectionViewSource { Source = … }`（G16.7）。★ 反向：**分组**故意打在**默认视图**上（在 `Catalog` 一处改即全体生效）。

## 四、流程 / UI 页
- `TableToolbar`「共 N 项」来自 `MatchInfo`；行数权威是 `TargetGrid.ItemsSource`。
- 流程「名称」列是受限下拉（`FunctionToNamesConverter`→`Catalog.*Names`）；**别为显示未知值把当前值并进 ItemsSource**（SelectedItem TwoWay 置空回写 null）。
- JSON 粘 AI 用 `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`。`RestoreFlows` 失败返 false 且不动 sink；空快照 `"[]"` 合法。
- `ExportSteps` 必须导出 `FlowStep.DurationMs`（键 `耗时`）：加字段时 `Export*` 与 `Fill*` 同改。
- Lua API 唯一权威是 `Services/HardwareApi.cs` 的 `Register()`；`Camera`/`Vision` 不注册。视觉流程契约是 `视觉步骤` 数组。
- FlowPage.xaml Row-0（复制JSON/粘贴生成/回退）四流程共用、在 `EditorPage.Detail` 里，**不能起 `x:Name`（MC3093）**。
- 「图像生成点位」= 工具栏按钮 + `Views/GraphGenDialog.xaml(.cs)`（VM=`GraphPointGenViewModel`）；`Graph.Generated` → 追加到当前工位。画布**最上层透明 `Border` 收单击**，底下全 `IsHitTestVisible=False`。
- ★ **新建 Apple 弹窗别照抄 `Owner = Application.Current?.MainWindow;`**：① 本窗口若是应用里第一个 `Window`，getter 返回它自己 → 「不能把 Owner 设为自己」；② Owner 必须是**已显示过**的窗口，否则「无法将 Owner 设置为之前未显示的 Window」（离屏/冒烟）。`NewProjectDialog` 已改 `if (owner is not null && !ReferenceEquals(owner,this) && owner.IsVisible) Owner = owner;` —— 另有 10 个弹窗（`ArrayGenDialog`/`ConfirmDialog`/`RenameDialog`…）仍是裸写法。
- 工程师页三列表（输入/输出 IO、气缸）：各一个 `ListBox` + **`VirtualCardListStyle`**（虚拟化+回收+像素滚动）+ 顶部搜索行（`TtSearchBox`、命中数、「取消」）+ `*Search` 过滤属性；行 `MiniCardStyle`+`Padding="10,5"`。**`ItemsControl`+外部 `ScrollViewer` 不虚拟化** → 必须 `ListBox`。
- 工程师页布局：外层 `Grid` 两列 star 宽 —— 左「IO 控制 + 气缸控制」、右「**轴控制 + 点位移动和设置**」（轴控紧贴点位表正上方）。★ **列宽比例是布局口味，别锁死**。
- 名称库下拉「级联二级菜单」（Request N）：**>20** 且至少一个名字能截前缀 → 按**第一个 `-`（半/全角）前**文本分组，截不出归「其他」；≤20 个保持平铺。数据三件套：`NamePrefixGroupDescription.cs` + `Catalog.ApplyGrouping`（改**默认视图**，含并集 `AllNames`）+ `NameGroupHeaderBehavior.cs`（`[ModuleInitializer]` 注册 ComboBox 类处理器 → 显式/内联/无 Style 三种下拉一律生效）。**条目仍是 `string`**。
- ★ **呈现**：一级只列分类行（`下料 (15) ›`），悬停/点击 → **右侧飞出**二级菜单列真实名称。= `GroupStyle.ContainerStyle`（`NameGroupCascadeContainerStyle`，TargetType=GroupItem）+ `NameGroupFlyout` 行为（按名找 `row`/`flyout`；轮询判定「一级行 + 二级菜单」都没鼠标才收起；点收起后临时抑制悬停否则立刻弹开；`Unloaded` 收掉，否则 Popup 作为独立顶层窗口留在屏幕上）。
- ★ **条目宿主必须是 `<ItemsPresenter/>`**：`CellComboStyle` 下拉原本是裸 `<StackPanel IsItemsHost="True"/>`，**裸面板宿主只实体化最外层 GroupItem（= 一级分类行），组内条目一个都不生成** → 下拉只剩分类标题、真实名称点不到（XAML 合法、build / 守卫 /「标题能渲染」全绿，只有真打开下拉才暴露）。守卫 G20/G21。
- 新建工程示例模板（Request N-samples）：`ProjectTemplate.Build()` 是唯一出口 → 三道后处理 `SeedSampleConditions` + `EnsureNodeGraphFlow` + **`ExpandNameLists`**（由 `ExpandSampleNames` 开关，仅「空白工程」= false）。它给每个模板追加 3 工位 × 8 = **24 个**轴/输入/输出/气缸/变量，全「工位-对象」命名（`上料-轴1`…）→ 一建工程下拉就是级联（轴 25 / 入 28 / 出 28 / 缸 24 / 变量 26 / 并集 134）。
- ★ 扩展轴卡厂商**必须写「雷赛」**：`CanServeProject()` 只 `continue` 掉 `fam.Vendor=="雷赛"`，写成「模拟卡」会把**所有模板**的默认硬件通道整体改成卡族层（不报错，行为全变）。卡型号用 `DMC-E3000`（命中雷赛族 → 仍跳过），`CardNo=9` 避开模板自带 0/1/2，卡内 `AxisNo` 0..23 唯一。
- ★ 「工位-对象」约定要**提示用户**，两处：`NewProjectDialog` 名称框下方一条；轴/IO/气缸/变量四页底部 `Views/PageHintBar`（**定义了但一直没人用**，正好复用，两个 DP `OperationText`/`PrecautionText`）。IO/变量页根 Grid 末尾本就留了空 `Auto` 行；轴页用 `DockPanel.Dock="Bottom"`；气缸页 Detail 原本直接是 `ScrollViewer`，需包 DockPanel。

## 五、暂停/停止守卫（Request B）
- 进程级静态 `HardwareBridge.WaitGuard`（`Action`）所有阻塞等待（Delay/MoveAxis/Lua/气缸脉冲）每 ≤50ms 轮询。Pause → `ResumeEvent.Wait()`；Stop/E-Stop → **抛 `OperationCanceledException`**（不是 `ScriptRuntimeException`，否则被 `ExecuteLeaf` 的 `catch(Exception)` 吞掉）。
- 每次运行 `HardwareBridge.BindWaitGuard(object ctrl)`（**反射**，避 Services→ViewModels 环依赖）。雷赛/研控 `WaitInterruptible` 必须读**静态** `HardwareBridge.WaitGuard`（读实例字段 → 永不响应暂停/停止）。气缸脉冲**不能**暂停在半途，只查 Stop，重抛前把输出复位到安全电平。
- 速度路径：`ExecAxis` `prop=="速度"`→`SetAxisSpeed`；`ExecPoint` `slot.Speed>0`→`SetAxisSpeed`；Lua `HardwareApi.SetAxisSpeed`；NgRunner `MoveAxis`。**`FlowLoopManager` 是 `NoCodeMotion.ViewModels` 顶层静态类**；`StopAll()` 停所有循环流程。

## 六、UI 验证硬约束（本机）
- 驱动 UI 用 `ctrl.GetInvokePattern().Invoke()`，不合成鼠标；导航项 `TextBlock` 沿 `GetParentControl()` 上溯到外层 `ButtonControl`。`uiautomation` 2.0.29 无 `GetSupportedPatterns()`。`PrintWindow` 不可靠（无桌面客户区全白）→ 以 UIA 文本为准。读 ComboBox 候选取它自己的 `ListItemControl` 子孙。
- ★ **`RenderTargetBitmap` 离屏渲染是可靠视觉证据**（真走可视化树）；`Measure/Arrange`+`UpdateLayout()` 后读 `ActualWidth/ActualHeight` 可**量化**验证。★ 元素是**活对象**：重排后 `ActualWidth` **就地更新** → 测拉伸要在改尺寸**前**取值。★ **阈值/比例都别锁死** → 断言**关系**（≥列宽 85%、随列宽单调增、两列铺满）。
- ★ 离屏搭件：**裸控件取不到主题模板**（`GetChildrenCount==0`）→ 自给最小 `ControlTemplate`；框架 `Loaded` 不自动触发 → 手动 `RaiseEvent(FrameworkElement.LoadedEvent)`。★ 计数按自定义 `Tag`/`Name`，别数全部 `Border`。★ **关着的 `Popup` 内部树不实例化** → 走**逻辑属性**（`Popup.Child`→`Border.Child`→`ScrollViewer.Content`）。★ 条目模板只给裸 `TextBlock` 不绑 `Text` → 占位却渲染空白。★ 别数整棵树的 `Path`/`Shape`（滚动条箭头也是 `Path`）→ 只在目标控件**内部**数。
- ★ **`RenderTargetBitmap` 渲染「还挂在别的父级里」的子元素会画出空白**（偏移把它推出位图范围）→ 只渲染**脱离窗口的独立控件**（整页 / 新建的搭件）；内容挂在 `Window` 上时先摘下来（`npd.Content = null`）再渲染。**别拿「在树里找得到」当渲染证据。**
- ★ `EditorPage` 在 `Items.Count==0` **且**宿主页设了非空 `EmptyHint` 时把右侧详情整块 `Collapsed` → 离屏空态量到的详情/提示栏是 **0×0**（假象，不是布局问题）。要量版面先给 `ProjectStore.Data` 灌数据：各页 VM 的 `Items` 就是 `ProjectStore.Data.Axes/Cylinders` 那个实例，`CopyFrom` 原地改内容即可（`Data` 是 private set）。

## 七、硬件层 / 相机（详见 `HARDWARE.md`）
- 雷赛 `LtdmcNative.cs` 是唯一 P/Invoke；卡族入口 `CardFamilyCatalog.cs` + `WenQiZhiCardBridge.cs`。★ 轴必须一轴一个 `IAxis`；`GetCardAxisCurrentState()` 0=停止/1=运行中（反直觉）；模拟卡 `IsVitualCard=true` 跳过真实开卡。
- ★ 研控 MCN420：`Accel/Decel` 是**加速时间(秒)**（旧默认 50 会爬行）；`Channel` 只能 0/1（SDK 曾硬编码 2 → 回 8194）；使能低电平有效、`NormalizeSevon(int)` 反转**别改回恒等**。
- ★ **模板轴加减速默认 `0.2` 秒**（`DefaultAxisAccel`）：`Accel/Decel` 新语义是**加速时间(秒)**，旧模板 `Ax(...)` 传的整数（50/100/150/250/200/90/180/360/40/20/15/300）会被读成几十~几百秒斜坡 → 轴爬行。所有模板 `Ax(...)` 已统一传 `0.2`（N-samples 收尾）；`MigrateAxisDefaults()` 只在**载入**时把恰好 `==50` 的迁成 0.2 且**不动用户值**，新工程不经过它。
- ★ 海康 MVS：本机只有托管封装、无 Runtime → 必须优雅降级；相机会话按设备 Key 复用（一台 GigE 一个独占句柄）。`CaptureFrame` 最后一层回退必须在 try 内，保证**永不抛**。

## 八、变量与表达式
- `ExpressionEvaluator.Tokenize` 用 `char.IsLetter` → **中文也算标识符**，`-` 是减运算符：变量名里的 `-` 一旦进表达式就被当减号（`下料-计数` → `下料 - 计数` = 0）。流程步骤按**名称精确匹配**取变量（`GetVariableValue`/`SetVariableValue`），`{变量名}` 走 `FlowRunnerService.Sub()` 正则 `\{([^}]+)\}` 字面替换 → 声明与引用都安全。
- ★ **已修（N-samples 收尾）**：`SimFlowPlayer` 的「变量」步骤**不再**把变量名拼进表达式。改为存 `VarOp`（加/减/乘/除/取模/取反）+ `VarExpr`（操作数），Apply 时 `GetVariableResolved(VarName)` **按名精确取值**后再运算 → `-` 命名的变量在仿真里也算对（取反保持 `1-cur` 语义）。真机 `FlowRunnerService.ExecVar` 本来就走 `GetVarNum(name)` 精确匹配，无此问题。
