# NoCodeMotion 长期项目笔记

## 构建 / 验证（最重要）

**本机 `dotnet build` 是可以跑的。** 若报
`NuGet ... Value cannot be null. (Parameter 'path1')`（或 `NETSDK1060 加载 obj/project.assets.json 失败`），
根因是当前 shell 里 `APPDATA` / `ProgramFiles` 为空字符串，**不是 SDK 坏、不是代码错、不是 obj 损坏**。
用 `env` 补上即可（bash 无法 export 带括号的变量名，必须用 `env`）：

    env "APPDATA=C:\Users\admin\AppData\Roaming" "ProgramFiles=C:\Program Files" \
        "ProgramFiles(x86)=C:\Program Files (x86)" dotnet build -v q --no-restore --nologo

基线：**0 错误**，约 590 条 `CA1416` 警告全部是既有的（AvalonEdit 跨平台分析），与本项目改动无关。

**运行时冒烟测试**：`C:\Users\admin\AppData\Local\Temp\ncm_smoke\`（csproj + Program.cs），
`ProjectReference` 指向本工程，`net10.0-windows` + `UseWPF=true`（控制台 Exe 也必须开 WPF）。
必须放在工程目录**之外**（为了不污染产品源码树）。**修正（2026-09-21 实测）**：以前这里写的是
「默认 `**/*.cs` glob 不排除 `.bt/` 之类目录」——**这个说法是错的**。实测：往 `.bt/` 放一个内容为
`this is definitely not valid C# at all !!!` 的 `.cs`，再 `dotnet build -t:Rebuild`，结果仍是
**0 错误 / 590 警告** → **点开头的目录（`.bt/`）默认就不参与编译**。
真正会被编进去的是**不带点**的临时目录（`NoCodeMotion.csproj` 里只显式 `Remove` 了 `artifacts\**`，
所以 `scratch/`、`tmp/` 这种名字会被 `**/*.cs` 收进去）。
用 `env "APPDATA=..." ... dotnet run -v q --nologo` 跑。适合验证 `Models/` + `Services/` 的纯逻辑，
不用起 GUI。

## 代码约定

- **隐式 using 不含 `System.Windows`**：`GlobalUsings.g.cs` 只有 `System / System.Collections.Generic /
  System.Linq / System.Threading / System.Threading.Tasks`。用 `Application.Current` 等必须显式
  `using System.Windows;`。
- **行尾 / BOM 逐文件判断，不要假设全仓统一**：仓库里 LF 与 CRLF 文件混存（如 `Models/ProjectTemplate.cs`
  是 LF，`Models/PointItem.cs` 是 CRLF），且多为 UTF-8 **带 BOM**。改文件时保持该文件自己的风格，
  写入前归一化，改完复查 `bareLF` 是否为 0。
- **文件头尾有作者水印注释**，内含零宽字符（U+200B/U+2063/U+200D 等），必须按字节原样保留，不可重打。
- `core.autocrlf=true`，所以 `git show HEAD:file` 看到的行尾与工作区不一致，判断行尾要看工作区文件本身。

## 架构要点

- `Catalog`（`Services/Catalog.cs`）是**下拉框用的全局名称缓存**，只在 `ProjectStore.Load()` 与
  `ProjectManager.LoadInto()` 里通过 `SyncAllFromData` 重建；各配置页靠
  `ListEditorViewModel.SyncCatalog()` + `CatalogCategory` 增量刷新。
  **它不是数据源**——需要判断「某设备是否存在」时应读 `ProjectStore.Data`。
- 自动保存链路：`Model.PropertyChanged → Parent.OnChildChanged → Parent.OnPropertyChanged(nameof(Children))
  → ListVM.OnItemPropertyChanged → ProjectStore.ScheduleSave()`。
  **任何运行态/只读展示属性都必须在这条链上被过滤掉**，否则会形成每秒几十次的写盘风暴。
- `PointItem.ConditionRowCount = 6`，`EnsureConditionRows()` 会把条件行规整为固定 6 行并裁掉尾部空行。
  **往条件集合里塞数据只能按下标覆写，不能 Append。**

## 表格工具栏 / 流程页（2026-09-21 新增）

- **`TableToolbar` 的「共 N 项/步」文案来自 `MatchInfo`，不是 `Count`。**
  `Count` / `CountLabel` 两个 DP 只参与文案拼接，从不直接显示。
  `MatchInfo` 曾在 `OnLoaded` / `SearchText` 变化时才重算，导致行数变化（粘贴生成换掉整个步骤集合、
  点「添加」）后标签一直停在打开页面时的数字。现已在 `TargetGrid.ItemContainerGenerator.ItemsChanged`
  上订阅刷新，并以 `TargetGrid.ItemsSource` 为行数权威来源（没挂上 grid 时才退回 `Count`）。
  **给表格页加「计数」类展示时，先确认自己绑的是不是真正被显示的那个属性。**

- **流程「名称」列是受限下拉**：`ComboBox ItemsSource` 走 `FunctionToNamesConverter` → `Catalog.*Names`，
  值不在库里就**渲染成空白**（作者既定设计）。`Catalog` 只是下拉缓存，判断对象是否存在要读 `ProjectStore.Data`。
  **不要为了让它显示未知值而把当前值并进 `ItemsSource`（MultiBinding）**：
  `SelectedItem` 默认 TwoWay，ItemsSource 换实例会被 WPF 置空并回写 `null`，把用户刚选的名称清掉。
  现在的做法是导入时提示「哪些对象在工程里不存在」。

- `AiProjectExchange` 输出的 JSON 是给人看、要粘进 AI 对话的，
  **必须用 `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`**，否则中文全变成 `\uXXXX`。

## 验证 UI 的硬约束（本机）

- **驱动 UI 一律用 `ctrl.GetInvokePattern().Invoke()`**，不要合成鼠标。
  导航项是 `TextBlock`，要沿 `GetParentControl()` 上溯到外层 `ButtonControl` 再 Invoke。
  `uiautomation` 2.0.29 **没有** `GetSupportedPatterns()`（那是裸 UIA COM API）。
  ~~（2026-09-22 更正）~~ 早先记的「本机没有交互式桌面：`GetForegroundWindow()==NULL`、
  `GetCursorPos()==(0,0)`」**已经不成立了**：实测 `GetForegroundWindow()` 返回非 0、
  光标在 (1203,453)。会话状态会变，**每次跑之前先打印这三个值再决定**，别照搬旧结论。
- **`PrintWindow` 的可靠性不能假定**（无交互桌面时客户区全白，标题栏正常）。
  截图不能直接当视觉证据 —— 以 UIA 读到的文本为准。
- **读 ComboBox 候选要取它自己的 `ListItemControl` 子孙**，别用「标签之后一路读到下一个有名字的
  TextControl」这种取法：ComboBox 内部那个可编辑文本框本身就是个有名字的 TextControl
  （显示当前选中值），会让你刚读到第一项就以为结束了（v1 脚本就栽在这，只报出「雷赛」一项）。
  ComboBox 自己 `Name` 通常是空的，靠标签的 y 坐标就近配对即可。
- 断言「计数标签是活的」不能只看一次结果：要**连粘两次不同条数**（如 7 → 3），
  否则工程里本来就是这个数，看不出标签是死的还是活的。
- 现成脚本：`%TEMP%\ncm_ui\verify_dropdowns2.py`（启动 exe → UIA Invoke 导航到「控制器」页
  → 断言品牌 / 总线类型 / 连接方式三个下拉的候选）。用
  `%TEMP%\ncm_patch\..` 旁边的 managed python 跑（`C:\Users\admin\.workbuddy-ai\binaries\python\versions\3.13.12\python.exe -u`），
  它同时装了 `PIL` 与 `uiautomation`；系统 Python 3.12 **没有** uiautomation。

## 流程页 AI 往返：提示词里的 Lua API（2026-09-21 新增）

- **Lua 沙箱真实可用的 API 只有一个权威来源：`Services/HardwareApi.cs` 的 `Register()`。**
  `Editing/LuaApi.HardwareList`（24 个硬件函数 + 中文说明）是编辑器的智能提示与 `Register` 的共同来源，
  `Services/LuaTemplates.cs` 的类注释是第三份（手写）摘要。
  **`Log` / `Camera` / `Vision` 曾经三处文档都在写、沙箱里却一个都没有** ——
  `Log` 已补注册（`Info`/`Output`/`Warn`/`Error`/`Debug`），`Camera`/`Vision` **确定不注册**
  （要真相机 SDK + 图像对象，是独立 feature）。
- `AiProjectExchange.BuildLuaApiText()` 是提示词里那段【Lua 可用的 API】的生成器，
  **轴/IO/气缸/通讯/料盘/硬件 六组直接从 `LuaApi.HardwareList` 生成**（签名 + 中文描述），
  子表 / 日志 / 标准库 / 「沙箱里没有的 API」四节手写。
  **往 `LuaApi` 加函数不用改提示词**；改手写那四节时记得同步冒烟 O2 的「24 个签名一个不漏」断言。
- 视觉流程的契约是 **`视觉步骤` 数组**（`FillVisualSteps` 读 `视觉步骤`，类型取
  图像采集 / 模板匹配 / 图像预处理 / 缺陷检测 / 测量 / 通讯），**不是 Lua**。
  工程级 `SchemaText` 里的视觉样例曾经给的是 `"脚本": "... Camera.Grab ..."` —— 会被导入器整段丢掉。

## 流程页 AI 往返：快照 / 回退（2026-09-21 新增）

- `AiProjectExchange.SnapshotFlows(flows)` → JSON 数组；`RestoreFlows(sink, snapshot)` → bool。
  **`RestoreFlows` 解析失败时返回 false 且绝不动 sink**；空快照 `"[]"` 是合法的「清空」目标。
- **`ExportSteps` 必须导出 `FlowStep.DurationMs`（键名 `耗时`）**：`FillFlow` 会读它，
  导出漏了就会在每次往返（粘贴生成 / 回退）把每步耗时清零。
  **加 `FlowItem` / `FlowStep` 字段时的规矩：`Export*` 与 `Fill*` 两边必须同时改，
  否则快照与 AI 往返都会静默丢字段。**
- `FlowViewModel._pasteUndo` 是 `Stack<PasteUndo>`，栈顶 = 最近一次粘贴。
  每项存 `Snapshot` / `FlowNames` / `SelectedIndex` / `Owner`（拍快照时的 `Items` 引用，
  用来在工程重载后失效化，防止跨工程串数据）。
  **只有快照真的变了才压栈**（`SnapshotFlows(Items) != undoSnapshot`）。
- `Views.ConfirmDialog(title, message, confirmText)` 是通用二次确认框（Apple 风格透明窗）。
  入口按钮叫「回退」、弹窗里的确认按钮叫「确定回退」——**刻意不同名**，既给用户区分，也让 UIA 测试能唯一定位。
- `Views/FlowPage.xaml` 的 Row-0 `StackPanel` 是「复制JSON / 粘贴生成 / 回退」三个按钮的落点，
  四种流程（运控/脚本/视觉/节点图）共用同一套。**该区域在 `EditorPage.Detail` 里，不能给元素起 `x:Name`（MC3093）。**

## 雷赛硬件层：脉冲卡 vs 总线卡（2026-09-22 新增，务必先读）

- **`LtdmcNative.cs` 是全项目唯一的 P/Invoke 声明处**（63 个声明）。`LtdmcSdk.cs`（774 个声明）
  **零外部引用**，已整体被取代；里面 3 个声明在 DLL 里根本不存在，已加警示注释。
- **LTDMC.dll 有两套互不相通的函数族，这是「函数返回 0 但轴不动」的总根源**：
  - `dmc_*` = 板卡本地资源 → **脉冲卡**。轴 = 卡上第几路，IO = 卡上第几位，伺服使能 = 物理引脚。
  - `nmc_*` = 总线主站资源 → **EtherCAT / CANopen 卡（DMC-E 3000/5000）**。
- **关键反直觉点：总线卡上「运动」仍然用 `dmc_*`**（官方例 1 / 例 7 实测）——
  `dmc_set_profile_unit`(7 参) / `dmc_pmove_unit` / `dmc_get_position_unit(card, axis, ref double)` /
  `dmc_check_done`(**0=运动中, 1=到位**) / `dmc_set_position_unit` / `dmc_stop`。
  **轴号就是整数索引，不用换算成从站地址。** 只有三件事分族：
  **伺服使能**（总线必须 `nmc_set_axis_enable`，CiA402）、**回零参数**（`nmc_set_home_profile` +
  `nmc_home_move`）、**总线诊断**（状态机 / 错误码）。
- **EtherCAT 端口号固定为 2**（`nmc_get_errcode(card, 2, ref …)`）。
  **CiA402 状态机只有 4（操作使能）能动**；0–7 的中文文案见 `LtdmcCard.DescribeAxisState`。
  回零完成读 `dmc_get_home_result(…, ref state)`，**`state == 1` 才算成功**。
- **IO 寻址默认「整卡位号」= 模块 × `Options.BitsPerModule` + 序号**（与官方例 8 一致，总线卡上也这么用）。
  官方 19 个例程**没有一个**用 `nmc_read_inbit`。需要「从站节点号 + 站内位号」时把
  `LeadshineHardwareBridge.Options.BusIoAddressing = true`。**这是全链路唯一没有例程依据的地方。**
- `LtdmcCard.Home` / `SetServoEnable` 已按卡型自动分派；桥接层加 `WarnIfAxisCardMismatch`
  （轴类型与卡型不一致会明确报警）与 `DiagnoseAxis`（超时异常里直接带上状态机 / 错误码读数）。
- **验收靠 `ncm_smoke` 的 R/S/T 三节，不需要插卡**：`PeExports(path)` 用纯 C# 解析 PE 导出表，
  断言每个声明都在 DLL 里、`nmc_*` 齐全、全部 `StdCall`、签名形状正确；
  并断言无卡时 `dmc_board_init()` 返回 0 且轴 / IO 动作只记日志不抛异常。

## 构建陷阱（2026-09-22 新增）

- **不带点前缀的临时目录会被 MSBuild 默认 glob 收进产品编译**。`_out/`（`.gitignore` 里忽略的
  临时 / 发布目录）里的残留 XAML 曾把构建打挂（12 条 `MC3000`/`MC3089`）。
  已在 csproj 里按 `artifacts\**` 的写法补了 `_out\**` 的 5 条 `Remove`。
  **新建 `scratch/`、`tmp/`、`test/` 之类目录前先想清楚 —— 它们会被 `**/*.cs`、`**/*.xaml` 收走。**
  （`.bt/` 这种带点的不会。）

- **冒烟测试与 `--no-incremental` 构建绝对不能并行**：两者都写工程根下的 `obj\`，
  并行会互删中间产物，报出成片的**假错**（`CS0103 InitializeComponent` / `CS5001 没有 Main` /
  `CS2001 缺 *.g.cs`）。而且**不会自愈** —— 之后单独跑，增量构建认为 obj 是最新的、
  不重新生成 XAML 的 `*.g.cs`，于是继续报同样的错。
  **看到这三类错误先怀疑 obj 被污染，不要改代码**；修法 `rm -rf obj bin` 再全量 build（约 37 秒）。
- **`-t:Rebuild` 会跳过 CoreCompile 并报假的「0 警告」**（日志里有
  `正在跳过目标"CoreCompile"`）。要真实警告清单必须用 `--no-incremental -v n`；
  且 WPF 的 `_wpftmp` 那一遍会把分析器警告**重复计一次**，原始 `grep -c` 约为真实值的 2 倍，比对前要归一化去重。

## 受限下拉字段：写入值必须落在 Catalog 候选集里（2026-09-22 新增）

- **`Catalog.BusTypeNames` 是从 `CardVendorRegistry.Vendors[].BusTypes` 生成的**
  （`Catalog.RefreshControllerStandards()`，由 `SyncAllFromData` 调用），
  下拉框只认这些值 —— **写出候选之外的值会被 WPF 渲染成空白，看起来像「功能坏了」**。
- 已知踩坑：`CardBusType.Other`（→「其它」）**曾经没有任何厂商声明**，
  所以「其它」不在候选里；而 `AxisControllerItem.BusType` 的文档取值集却写着它合法。已给雷赛条目补上。
  **给受限下拉字段写值时，先断言 `Catalog.*Names.Contains(值)`。**
- `AxisControllerViewModel.AutoDetect()` 曾经把 `BusType` **写死成「脉冲」** ——
  插 DMC-E 总线卡也会登记成脉冲卡，用户会照着错的分支接线。
  现在读 `LtdmcCard.FirstCard` 照实登记（`LtdmcCard.DescribeBusType` 负责卡型 → 总线类型）。
- `HardwareSetup.StatusMessage` 同时被 Lua 的 `HardwareStatus()` 读走，
  所以卡型 / 总线状态要写在这个串里。

## 已移植运动控制卡族层（2026-09-22 新增，动硬件前先读）

代码在 `Services/Hardware/Cards/`：`Interfaces/`（`ICard` 22 项 / `IAxis` 201 项 / `IIOInPut` /
`IIOOutPut` / `IEIOInPut` / `IEIOOutPut`）、`Native/`（共享 P/Invoke 包装类）、
`Families/<26 个卡族>/`（每族固定 5~7 个文件：`CardRealization` / `AxisRealization` /
`InioRealization` / `OutioRealization` / 可选 `Expand*Realizetion` / `XXXSDK.cs`）、`Compat/`（参考实现
自带的应用层服务的替身）。命名空间**保持参考工程原样** `Samsun.Domain.MotionCard.Common.*`。

- **入口**：`Services/Hardware/Cards/CardFamilyCatalog.cs`（26 个卡族注册表 + 卡族匹配 + dll 清单）
  与 `Services/Hardware/Cards/SamsunCardBridge.cs`（`IHardwareBridge` 实现）。
  `HardwareSetup.AutoDetectFromProject()` 决定用哪套：**只有雷赛的工程仍走
  `LeadshineHardwareBridge`（既有行为不变），出现非雷赛卡族才切 `SamsunCardBridge`。**
- **`Families` 目录名 ≠ 真实命名空间**，别按目录名猜。已知不同名：`E64IOSeries`→`.SLDIOE64`、
  `MCN42Series`→`.YKMCN42Series`、`SoftServo`→`.SoftServo_EtherCAT`、
  `YKMCCE3032`→`.YKMCC_E3032_EtherCAT`、`YMCC1200P`→`.YKMCC1200P`、`EC600`→`.EC600_EtherCAT`、
  `DMC1000S`→`.DMC1000S`（只有同目录的 `dmc1000SConfig.cs` 还在 `MotionCardRes.DMC1000S`）。
- **`NativeDlls` 清单必须「剥注释后扫 `DllImport` + 扫调用了哪个 Native 包装类」得出，不能按目录名猜。**
  曾经猜错 11 个：**`HY7X00`（恒昱）的真实驱动是 `PCI400.dll` 不是 LTDMC.dll**；
  `DMC1000S`/`E64IOSeries`/`EC600`/`HYMC608`/`PLTEI400H`/`SLD1230`/`SLD1232`/`VirtualMotionCard`
  都额外 P/Invoke `LTDMC.dll`；`SoftServo` 的 EtherCAT 复位还调 `MCCE135.dll`。
  不剥注释会出事：`//return LTDMC.xxx(...)` 这种注释调用满仓都是。
- **`Aliases` 必须同时收录「型号名」和「固件卡型码」**：自动识别对雷赛卡写进「卡型号」的是
  `$"0x{固件卡型码:X}"`（见 `AxisControllerViewModel.AutoDetect`），不是型号名。
  26 个卡族共登记了 48 个固件码（如 DMC1000S=`0x2711`、EC600=`0xEC600`、虚拟卡=`0x1B198`），
  码值抄自参考实现 `SystemHardwareData.cs` 的「卡型码 → 卡族」switch。
  缺了固件码，混装工程里这张卡会掉到「品牌 + 总线」兜底分支、**被配上一张不相干的同品牌卡**。
  别名是**子串**匹配，所以短数字码（如 MCN42 的 `420`）只登记十六进制形式，避免串味。
  注意参考实现自己把 `MCC400S` 与 `MCC800S` 都上报成 `0x8076`，该码归 MCC800S。
- **品牌归属照参考实现的分组规则算**（`SelectControllerDialog.xaml.cs` `BuildCardTree`）：
  含 DMC+E→雷赛总线；DMC 不含 E→雷赛脉冲；前缀 PCI/E/F→升立德；前缀 HY→恒昱；
  前缀 MCC 含 E→研控总线；MCC 不含 E→研控脉冲；含 虚拟/仿真→模拟卡。
  按此规则 `MCC141C`→研控、`EC600`→升立德（后者的枚举位置也紧挨升立德 E64xx 区块）；
  真正覆盖不到的只剩 `PLTEI400H`（前缀 PLT）与 `SoftServo`（前缀 S），挂「未分类」。
- **轴必须一轴一个 `IAxis` 实例**：`AxisWhichCardNo` / `AxisID` 是实例字段，而
  `GetCardAxisCurrentPosition(cntr_no)` / `CardAxisHomeMove()` / `GetCardAxisCurrentState()`
  都不带卡号/轴号参数 → `CardFamilyRuntime.NewAxis` 是**工厂**，不是单例。
- **`GetCardAxisCurrentState()` 返回 0 = 停止 / 1 = 运行中**（看着像反的，但全族一致）。
- **`ListCardParam` 必须预置**：部分卡族在 `OpenCard` 里按下标访问它，空表直接
  `IndexOutOfRange` → `SamsunCardBridge.SeedCardParams` 兜底。
- **模拟卡族**（`VirtualMotionCard` / `DigitalTwinCard`）把 `IsVitualCard = true` 后跳过真实开卡，
  直接返回成功 —— 脱机调试用这个，也是冒烟 Z 段的做法。
- **`SamsunCardBridge.Guard` 捕获所有异常**（不只是 `HardwareOperationException`）并包成
  中文 `ScriptRuntimeException`：移植过来的族里 `throw new NotImplementedException()` 很常见，
  不能让脚本因此中断。
- 参考实现自带的 `GTS800PG` 族**没有移植**（它不在参考工程 csproj 的编译集里）。
  缺的底层库：`MCC.dll`（研控 4 个脉冲族）、`Dmc2410.dll`、`SLD9014PTP.dll`；`Dmc2210.dll` 是 x86。

## 冒烟测试工程（2026-09-22 补全）

`C:/Users/admin/AppData/Local/Temp/ncm_smoke/`（`ProjectReference` 指向本工程，
`NoWarn=NU1701;CA1416;CS8632`，`GenerateAssemblyInfo=true`；`net10.0-windows` + `UseWPF=true`）。
**必须放在工程目录之外**，否则会被 `**/*.cs` glob 收进产品源码树。
跑法：`env "APPDATA=..." "ProgramFiles=..." "ProgramFiles(x86)=..." dotnet run -v q --nologo`。

现有段落：`A~D` 移动条件、`E~N` 流程 JSON 往返、`O~Q` 节点图模板、`R~U` 雷赛硬件层
（含「PE 导出表 vs P/Invoke 声明」比对）、`V~AB` 卡族层（注册表完整性 / dll 清单双向对齐 /
缺库清单 / 卡族匹配规则 / 虚拟卡端到端往返 / 模式自动选择 / Lua `UseCardFamilies`）。
**当前 708 PASS / 0 FAIL。加断言时别把 `_fail` 计数弄乱。**

两个容易踩的断言坑：
- C# 委托注册进 `script.Globals` 后 `Type` 是 `DataType.ClrFunction`，**不是** `DataType.Function`。
- `HardwareApi` 的构造函数是 `(IHardwareBridge, Action<string>)`，注册方法是
  `static HardwareApi.Register(Script, HardwareApi)`。
