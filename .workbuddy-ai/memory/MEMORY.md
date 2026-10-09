# NoCodeMotion 长期项目笔记
> 陷阱索引。完整推导见 `.workbuddy-ai/memory/` 逐日日志（2026-09-19 起）。
> ★ 硬件层细节（雷赛 / 研控 MCN420 / 卡族 / 多开退出 / 海康相机）已拆到同目录 **`HARDWARE.md`**，动硬件前先读它。
> ★ UI 细节（名称库级联下拉 / 工程师页布局 / 离屏渲染验证硬约束）已拆到同目录 **`UI.md`**，改 UI 或要「证明 UI 对了」先读它。

## 一、构建 / 验证
- dotnet 走独立 shell 工具（非 bash）。★ `dotnet … > log` 是 **GBK(cp936)** → 中文 pattern `grep` **静默 0 匹配**（`iconv -f gbk`；或 `DOTNET_CLI_UI_LANGUAGE=en-US`）。
- ★ **清理要复核**：`Remove-Item -Recurse -Force` 静默不删；bash `rm -rf obj bin` 被沙箱**批量删除守卫**拦下。up-to-date 会跳过 CoreCompile → 误判「全量成功」，**删完必须 `ls` 复核**；**空或极短日志 = 没编译**。
- ★ **build 非 0 时冒烟仍会跑旧 dll**（`exit=0`、输出是上一次的，看着「全部通过」）→ **先确认 `0 个错误` 再看冒烟**。build 偶发 `exit=-1073741571`（栈溢出），重跑即可。
- ★ `dotnet exec` 严格按 deps.json → 主工程 `<Reference>`+`HintPath` 的 dll **不进引用方 deps.json**，冒烟要**照抄同样的 `<Reference>`**。`NETSDK1060` = shell `APPDATA`/`ProgramFiles` 空 → 显式 `env` 传入。
- ★ 源码守卫**必须在仓库目录跑**（`%TEMP%` 读 `D:\` 被沙箱替换成乱码，还会被清理整删）→ 工具全放仓库：`tools\guard_sources.py`（G1–G24，**232 PASS**）、`tools\_dump.py`（看 BOM）、`.smoke\`。
- 冒烟：`dotnet build .smoke/ncm_smoke.csproj -p:UseAppHost=false` → `dotnet exec .smoke/bin/Debug/net10.0-windows10.0.19041.0/ncm_smoke.dll`（段 A–N，**172 PASS**）。★ `%TEMP%` 里的 exe 跑不起来（`exit 127`）→ 一律 `dotnet exec <dll>`。冒烟与 `--no-incremental` 不可并行（互删 `obj\`）；`-t:Rebuild` 跳过 CoreCompile 报假「0 警告」。
- ★ **改 `TargetFramework` 会挪输出目录** → 旧路径 dll 还在 → `dotnet exec` 旧路径**静默跑旧代码**（新加的段「消失」）。改完先删旧 bin 或确认新路径。
- ★ **`dotnet exec` 下 OpenCvSharp 原生库不可用**：`opencv_world480.dll` 不在 DLL 搜索路径 → `DllNotFoundException … 0x8007007E`。修：`PATH="<outdir>:$PATH" dotnet exec <dll>`。冒烟里走 OpenCV 的段都要**先探测、探测失败就 `SKIP`**。
- 补丁脚本：按行号区间替换，或 `sub1` 断言 `count==1`、锚点全过后一次写盘。★ **别把 BOM 写成硬断言**。临时目录带点前缀（`.smoke/`）不编译，不带点（`scratch/`）会被 `**/*.cs` 收走。

## 二、代码约定
- 隐式 using 只有 System/Collections.Generic/Linq/Threading/Tasks（无 System.Windows/System.IO）。
- 行尾/BOM 逐文件保持；头尾作者水印（零宽 U+200B/U+2063/U+200D）按字节保留；**XAML 无水印**。**BOM 逐文件不同** → 先探再写；多编辑同一文件先内存累积、最后写盘。
- ★ XAML `{StaticResource KEY}` 必须在 `App.xaml` 递归可达链里：**只合并 `Resources\AppStyles.xaml`**，`Themes\AppleControls.xaml` 从未合并 → 写了它的键就在 `InitializeComponent()` 抛 `XamlParseException`。**grep 到 `x:Key` ≠ 运行时找得到，build 也抓不到**（G17）。
- `AppleToggle` 46×26 **无文字开关**，模板不渲染 Content → 要标签另配 TextBlock。★ **`TtBaseBtn` 的 `Padding` 硬编码在 ControlTemplate 里** → 覆盖无效，只能改 `MinWidth`/`Height`/`FontSize`；`TtPill*` 的 `Padding` 是 Setter+`TemplateBinding` → **可覆盖**。**先看样式模板会不会渲染你要设的属性。**
- ★ **`ItemsControl.GroupStyle` 是只读 CLR 集合、不是依赖属性** → 只能回调里 `Add`。`CollectionViewGroup` **不实现 `IEnumerable`**（用 `.Items`）。★ **全局钩子用 `[ModuleInitializer]`，别用静态构造**（要等首次访问本类，漏一处引用就静默失效）。

## 三、架构要点
- `Catalog`（`Services/Catalog.cs`）是下拉框名称缓存，只在 `Load`/`LoadInto` 经 `SyncAllFromData` 重建，**不是数据源**；`Catalog.*Names` 是受限值域 → 写候选外的值渲染空白。
- 自动保存链 `Model.PropertyChanged → ListVM.OnItemPropertyChanged → ScheduleSave()`；**运行态/只读展示属性必须过滤**，否则每秒写盘。
- 点位「名称」用 0 基轴号；`XlsxProjectStore` 点位表列名固定 `轴1位置…轴1名`（4 轴槽），改了读不了老工程。
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
- ★ 名称库下拉「级联二级菜单」（Request N）与 工程师页三列表/布局：**细节与坑见 `UI.md`**（分组阈值 >20、`NamePrefixGroupDescription`+`Catalog.ApplyGrouping`+`NameGroupHeaderBehavior` 三件套、一级分类行右侧飞出、**条目宿主必须 `<ItemsPresenter/>`** G20/G21、`VirtualCardListStyle` 必须用 `ListBox`）。
- 新建工程示例模板（Request N-samples）：`ProjectTemplate.Build()` 是唯一出口 → 三道后处理 `SeedSampleConditions` + `EnsureNodeGraphFlow` + **`ExpandNameLists`**（由 `ExpandSampleNames` 开关，仅「空白工程」= false）。给每个模板追加 3 工位 × 8 = **24 个**轴/输入/输出/气缸/变量，全「工位-对象」命名（`上料-轴1`…）→ 一建工程下拉就是级联（轴 25 / 入 28 / 出 28 / 缸 24 / 变量 26 / 并集 134）。
- ★ 扩展轴卡厂商**必须写「雷赛」**：`CanServeProject()` 只 `continue` 掉 `fam.Vendor=="雷赛"`，写成「模拟卡」会把**所有模板**的默认硬件通道整体改成卡族层（不报错，行为全变）。卡型号 `DMC-E3000`，`CardNo=9` 避开模板自带 0/1/2，卡内 `AxisNo` 0..23 唯一。
- ★ 「工位-对象」约定要**提示用户**：`NewProjectDialog` 名称框下方一条；轴/IO/气缸/变量四页底部 `Views/PageHintBar`（**定义了但一直没人用**，两个 DP `OperationText`/`PrecautionText`）。IO/变量页根 Grid 末尾本就留了空 `Auto` 行；轴页用 `DockPanel.Dock="Bottom"`；气缸页 Detail 原本直接是 `ScrollViewer`，需包 DockPanel。

## 五、暂停/停止守卫（Request B）
- 进程级静态 `HardwareBridge.WaitGuard`（`Action`）所有阻塞等待（Delay/MoveAxis/Lua/气缸脉冲）每 ≤50ms 轮询。Pause → `ResumeEvent.Wait()`；Stop/E-Stop → **抛 `OperationCanceledException`**（不是 `ScriptRuntimeException`，否则被 `ExecuteLeaf` 的 `catch(Exception)` 吞掉）。
- 每次运行 `HardwareBridge.BindWaitGuard(object ctrl)`（**反射**，避 Services→ViewModels 环依赖）。雷赛/研控 `WaitInterruptible` 必须读**静态** `HardwareBridge.WaitGuard`（读实例字段 → 永不响应暂停/停止）。气缸脉冲**不能**暂停在半途，只查 Stop，重抛前把输出复位到安全电平。
- 速度路径：`ExecAxis` `prop=="速度"`→`SetAxisSpeed`；`ExecPoint` `slot.Speed>0`→`SetAxisSpeed`；Lua `HardwareApi.SetAxisSpeed`；NgRunner `MoveAxis`。**`FlowLoopManager` 是 `NoCodeMotion.ViewModels` 顶层静态类**；`StopAll()` 停所有循环流程。

## 六、UI 验证（详见 `UI.md`）
- ★ 驱动 UI 用 UIA `InvokePattern`（不合成鼠标）；`PrintWindow` 不可靠 → 以 UIA 文本为准。
- ★ **`RenderTargetBitmap` 离屏渲染 + `ActualWidth/ActualHeight` 量化**是可靠证据，但**别锁死阈值** → 断言**关系**；元素是**活对象**（重排后尺寸就地更新，测拉伸要在改尺寸前取值）。
- ★ **给页面加了新控件/新绑定，就把它加进冒烟段 F**（真 `new` 一次走 `InitializeComponent()`）—— 这是唯一能抓到「`StaticResource` 键写错 / 绑定写错」的自动手段（build 抓不到）。
- ★ `EditorPage` 空态会把详情整块 `Collapsed` → 离屏量到 0×0 是假象，先 `ProjectStore.Data.CopyFrom` 灌数据。

## 七、硬件层 / 相机（详见 `HARDWARE.md`）
- 雷赛 `LtdmcNative.cs` 是唯一 P/Invoke；卡族入口 `CardFamilyCatalog.cs` + `WenQiZhiCardBridge.cs`。★ 轴必须一轴一个 `IAxis`；`GetCardAxisCurrentState()` 0=停止/1=运行中（反直觉）；模拟卡 `IsVitualCard=true` 跳过真实开卡。
- ★ 研控 MCN420：`Accel/Decel` 是**加速时间(秒)**（旧默认 50 会爬行）；`Channel` 只能 0/1（SDK 曾硬编码 2 → 回 8194）；使能低电平有效、`NormalizeSevon(int)` 反转**别改回恒等**。
- ★ **模板轴加减速统一 `0.2` 秒**（`DefaultAxisAccel`）：旧模板 `Ax(...)` 传的整数（50/100/150/250/200/90/180/360/40/20/15/300）会被读成几十~几百秒斜坡 → 轴爬行。`MigrateAxisDefaults()` 只在**载入**时迁恰好 `==50` 的值且**不动用户值**，新工程不经过它。
- ★ 海康 MVS：本机只有托管封装、无 Runtime → 必须优雅降级；相机会话按设备 Key 复用（一台 GigE 一个独占句柄）。`CaptureFrame` 最后一层回退必须在 try 内，保证**永不抛**。

## 八、变量与表达式
- `ExpressionEvaluator.Tokenize` 用 `char.IsLetter` → **中文也算标识符**，`-` 是减运算符：变量名里的 `-` 一旦进表达式就被当减号（`下料-计数` → `下料 - 计数` = 0）。流程步骤按**名称精确匹配**取变量，`{变量名}` 走 `FlowRunnerService.Sub()` 正则 `\{([^}]+)\}` 字面替换 → 声明与引用都安全。
- ★ **已修（N-samples 收尾）**：`SimFlowPlayer` 的「变量」步骤**不再**把变量名拼进表达式。改为存 `VarOp`（加/减/乘/除/取模/取反）+ `VarExpr`（操作数），Apply 时 `GetVariableResolved(VarName)` **按名精确取值**后再运算 → `-` 命名的变量在仿真里也算对（取反保持 `1-cur`）。真机 `FlowRunnerService.ExecVar` 本来就走 `GetVarNum(name)` 精确匹配。

## 九、视觉流程（图像工具）
- 一个视觉步骤 = 四处同改（**少一处就静默失效**）：① `Models/VisualFlowStep.cs` 参数字段；② `Views/VisualFlowDetailViewModel.cs` 的 `IsXxx` 标志（并加进 `RaiseTypeFlags()` 的 `nameof` 列表，否则切类型后参数卡不刷新）；③ `Views/VisualFlowPage.xaml` 左侧调色板 `VisualStepTypes` + 右侧参数卡（绑 `IsXxx`）；④ `Services/Vision/VisionEngine.cs` 的 `switch (StepType)` 分支 + 一个 `RunXxx`。
- 图像链：OpenCvSharp `Cv.Mat`（BGRA）；另有一块 `display` 注释画布用于画框/写字。`VisionStepResult.Text` 是给后续节点/显示用的结构化文本结果。
- ★ **OCR = Windows 自带引擎（`Windows.Media.Ocr`，离线零三方依赖）**，步骤类型 `字符识别`。链路：`Cv.Mat` → BGRA `byte[]` → `SoftwareBitmap`（Bgra8 / Ignore）→ `OcrEngine.RecognizeAsync(sb).AsTask().GetAwaiter().GetResult()`（逐行细节见当日日志）。
- ★ **必须把 `TargetFramework` 抬到带 Windows SDK 版本**（`net10.0-windows10.0.19041.0`）才拿得到 WinRT 投影；否则 `Windows.Media.Ocr` 编不过。同时 `NoWarn` 加 `CA1416`。`.smoke/ncm_smoke.csproj` 要同步改。
- ★ 语言映射：`自动`→`TryCreateFromUserProfileLanguages()`，`中文`→`zh-Hans-CN`、`繁体中文`→`zh-Hant-TW`、`英文`→`en-US`、`日文`→`ja-JP`、`韩文`→`ko-KR`。**中文识别要目标机装中文 OCR 语言包**，否则建引擎失败 → 必须优雅降级。
- ★ **Windows OCR 会在 CJK 之间插空格** → 比对前先 `NormalizeOcr` 去掉所有空白，否则「ABC 123」≠「ABC123」。匹配方式 包含/等于/正则（可选忽略大小写）→ 输出 OK/NG。
- ★ **ROI 复用现有拖拽手势**：`VisualFlowPage.xaml.cs` 的 `ApplyTemplateRoi` 里按 `StepType` 分流 —— 字符识别写 `OcrRoiX/Y/W/H` 后 `return`（**不**走模板裁剪/保存）。未框选时 `OcrRoiText` 回显「整图识别」。
- AI 交换：`Services/AiProjectExchange.cs` 的 `NormalizeStepType`（别名归一）+ `ExportVisualSteps`/`FillVisualSteps`（含 `识别框` = `"x,y,w,h"`）+ prompt 类型清单，三处同改。

## 十、图像采集「来源」（相机 / 文件 / 文件夹）
- ★ 默认 `SourceType = "相机"`（旧默认「文件」）。**旧行为是静默灾难**：模板建出的采集步没写 `SourceType` → 默认「文件」+ 无路径（或把文件夹写进 `SavePath`）→ `File.Exists` 失败 → 引擎悄悄生成合成测试图并报 `Ok=true`，看着「跑通了」其实没取图。
- ★ **只有「来源=相机」读不到相机时才回退测试图**；来源=文件/文件夹 路径无效一律 `AddFail` + `return null`。因此采集失败时 `cur == null` → 主循环 `display = cur?.Clone()` 必须容错，且每个算子分支都要先 `if (cur == null) { AddFail(report, s, "请先执行图像采集"); break; }`（`测量` 原来漏了，已补）。
- ★ **相机字段支持「名字」也支持「编号」**：`VisionEngine.ResolveCameraIndex(string?)` —— ① 工程相机名（`ProjectStore.Data.Cameras` 忽略大小写精确匹配；模板存的就是名字如「下视相机」）② 纯数字 0 基索引 ③ 名字带数字按 1 基（「相机2」→ 1）。旧实现只有 `int.TryParse` → 模板的「下视相机」直接判「相机编号无效」。
- ★ **真实相机取像只有一处实现**：`VisionEngine.TryGrabRealCamera(idx, out w, out h, out err)` = ① 海康 MVS（`MvsCameraService.TryGrabBgra`，与相机页共用已打开会话）② OpenCV `VideoCapture(idx)`。`RunAcquire`（视觉流程采集步）与 `CaptureFrame`（Lua 相机步骤 / 3D 抓拍）都走它。**旧 `RunAcquire` 只 `new Cv.VideoCapture(索引)` → 工业相机永远打不开**。
- 界面：`VisualFlowPage` 采集卡的「相机」是**可编辑下拉**（`ItemsSource={Binding CameraNames}` + `Text={Binding SelectedStep.CameraId}`，`IsEditable=True IsReadOnly=False` —— `CellComboStyle` 默认 `IsReadOnly=True` 必须显式覆盖）。`CameraNames` 由 VM 从 `ProjectStore.Data.Cameras` 现取，在 `RaiseSourceFlags()` 里一起 `OnPropertyChanged`。
- 模板：两个视觉模板的采集步都已 `SourceType = "相机"` + 相机名，并删掉被当文件路径用的 `SavePath = "Images/inspection/"`。
- 节点图 `NgVisionExecutor.Capture`：来源=文件/文件夹 路径无效时当帧为空，新增 `if (r is { Ok: false } && !rep.HasImage)` 明确上报（否则拿 0×0 帧继续喂算子，报更难懂的错）。
