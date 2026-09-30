# NoCodeMotion 长期项目笔记

> 精炼索引，只留最容易重复踩、一句话说不清的坑。
> **完整细节与推导在 `.workbuddy-ai/memory/` 逐日日志（2026-09-19 起），需要时去查。**

## 一、构建 / 验证

- **`dotnet build` 报 `NuGet ... Value cannot be null ('path1')` / `NETSDK1060`** → 根因是 shell 里
  `APPDATA`/`ProgramFiles` 为空，**不是 SDK / 代码 / obj 坏**。补：
  `env "APPDATA=…\Roaming" "ProgramFiles=C:\Program Files" "ProgramFiles(x86)=…" dotnet build …`
  （bash 无法 export 带括号的名字）。基线 **0 错误 + 612 `CA1416`**（AvalonEdit 跨平台，既有）。
- **★ 用 PowerShell 跑 dotnet，不要用 Bash**：Bash 里输出全空、退出码 0、重定向 0 字节。
  **空日志 ≠ 成功**。设 `[Console]::OutputEncoding=[UTF8]`，构建完用 `Get-Item` 核对 DLL 大小/时间戳。
- **★ `%TEMP%` 启动的进程读 `D:\` 会拿到沙箱替换内容**（不报错，静默乱码）→
  **源码文本守卫必须在仓库目录里跑**（Python 可靠），运行时断言才放 `%TEMP%`。
  症状：`Contains` 在你肉眼可见的字符串上返回 false。
- **冒烟工程** `%TEMP%\ncm_smoke\`（`ProjectReference` 指本工程，`net10.0-windows`+`UseWPF=true`，
  **必须在工程目录之外**；`System.IO`/`ViewModels` 要显式 using）。段：S 错误码表 / T `NormalizeSevon` /
  U `AxisFailHint` / V `IsBufferBusyCode` / W 名称 0 基 / X Jog 防 8194。守卫 `%TEMP%\ncm_patch\guard_sources.py`。
- **冒烟与 `--no-incremental` 构建不可并行**（都写工程根 `obj\`，会互删中间产物报成片假错：
  `CS0103 InitializeComponent` / `CS5001 没有 Main` / `CS2001 缺 *.g.cs`，**不会自愈**）→
  **看到这三类错先怀疑 obj 污染，不要改代码**；修法 `rm -rf obj bin` 全量 build。
  `-t:Rebuild` 会跳过 CoreCompile 报假「0 警告」。
- **与别的编辑器并发时，构建会在你没碰的文件上报真实感极强的 `CS1061`** → 先看 mtime，再重跑。
- **临时目录**：带点前缀（`.bt/`）不参与编译；不带点（`scratch/` `tmp/` `_out/`）会被 `**/*.cs` 收走。

## 二、代码约定

- **隐式 using 只有 System / Collections.Generic / Linq / Threading / Tasks** —— **无 `System.Windows`、无 `System.IO`**。
- **行尾 / BOM 逐文件判断**（`ProjectTemplate.cs` LF、`PointItem.cs` CRLF，多为 UTF-8+BOM）；保持该文件自身风格。
- 文件头尾有**作者水印注释**（含零宽字符 U+200B/U+2063/U+200D），按字节原样保留。
- `core.autocrlf=true` → `git show HEAD:file` 行尾与工作区不一致，要看工作区文件。
- **多编辑同一文件先内存累积、最后一次写盘**（每次 `sub()` 重读磁盘会只剩最后一个编辑）；
  写回不要对已归一化文本再做行尾转换（产 `CRCRLF`）；写盘后复查「每个新 token 恰好 1 次」。
- **Python 补丁里多行字面量必须先归一化行尾再匹配**；注入的 `'''…'''` 块里**不要写 `\uXXXX`**。

## 三、架构要点

- `Catalog`（`Services/Catalog.cs`）是**下拉框全局名称缓存**，只在 `ProjectStore.Load()` /
  `ProjectManager.LoadInto()` 经 `SyncAllFromData` 重建。**它不是数据源**。
- **`Catalog.*Names` 是受限值域** → **写出候选外的值会被 WPF 渲染成空白**（像「功能坏了」）。
  `BusTypeNames` 来自 `CardVendorRegistry.Vendors[].BusTypes`。写值前断言 `Contains(值)`。
- 自动保存链：`Model.PropertyChanged → OnChildChanged → OnPropertyChanged(Children) →
  ListVM.OnItemPropertyChanged → ScheduleSave()`。**任何运行态/只读展示属性必须被过滤**，否则每秒写盘。
- `PointItem.ConditionRowCount = 6`；**条件集合只能按下标覆写，不能 Append**。
- **「轴号 / IO 序号」从 0 起、同 Controller 内不可重复；「名称」用同一个号**（轴0、输入0…**名里的数 = 轴号**）。
  生成器见 `AxisControllerViewModel.GenerateAxisPoints`（`MakeAxis(tag,s,s)`）/ `GenerateIoPoints`；
  **曾从 1 生成，是已修 bug，别改回去**。
  **刻意不动**：`XlsxProjectStore` 点位表**列名** `轴1位置…轴1名`（固定 4 轴槽 schema），改了读不了老工程。
  **★ 重复轴号是静默灾难**：`AxisOf` 里 `slot.Axes` 是 `Dictionary<轴号,IAxis>`，而卡族读位置/回零/读状态
  **不带卡号轴号**（读实例字段）→ 两轴同号则第二根复用第一根 `IAxis`，共用同一物理通道、**卡内缓冲互相抢占
  → Jog 报 8194**。已加一次性告警 `_warnedAxisDuplicate`（键 = 控制器名#轴号）。

## 四、表格 / 流程页

- **`TableToolbar` 的「共 N 项」来自 `MatchInfo`，不是 `Count`**；行数权威是 `TargetGrid.ItemsSource`。
- **流程「名称」列是受限下拉**（`FunctionToNamesConverter` → `Catalog.*Names`），值不在库里渲染**空白**
  （既定设计）。**不要为显示未知值把当前值并进 `ItemsSource`**（`SelectedItem` TwoWay 会被置空回写 `null`）。
- **JSON 粘进 AI 对话必须 `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`**。
- **快照/回退**：`RestoreFlows` 解析失败返回 false 且**绝不动 sink**；空快照 `"[]"` 合法；
  `_pasteUndo` **只有快照真变了才压栈**。
- **`ExportSteps` 必须导出 `FlowStep.DurationMs`（键 `耗时`）** —— 规矩：**加字段时 `Export*` 与 `Fill*` 同改**。
- `Views.ConfirmDialog(title, msg, confirmText)`；入口「回退」/弹窗「确定回退」**刻意不同名**。
- `Views/FlowPage.xaml` Row-0 是「复制JSON / 粘贴生成 / 回退」落点，四种流程共用；
  **该区在 `EditorPage.Detail` 里，不能起 `x:Name`（MC3093）**。
- 【Lua 可用 API】由 `AiProjectExchange.BuildLuaApiText()` 从 `LuaApi.HardwareList` 生成 →
  **加函数不用改提示词**。Lua 真实可用 API **唯一权威是 `Services/HardwareApi.cs` 的 `Register()`**；
  `Camera`/`Vision` **确定不注册**。视觉流程契约是 **`视觉步骤` 数组**，**不是 Lua**。

## 五、雷赛硬件层

- **`LtdmcNative.cs` 是全项目唯一 P/Invoke 声明处**（63 个）。`LtdmcSdk.cs`（774 个）**零引用、已被取代**
  （其中 3 个 DLL 里根本不存在）。
- **LTDMC.dll 两套互不相通的函数族 = 「返回 0 但轴不动」的总根源**：`dmc_*` = 脉冲卡（板卡本地），
  `nmc_*` = EtherCAT/CANopen 主站。
- **反直觉：总线卡上「运动」仍用 `dmc_*`**：`dmc_set_profile_unit`(7参) / `dmc_pmove_unit` /
  `dmc_get_position_unit` / `dmc_check_done`（**0=运动中, 1=到位**）/ `dmc_stop`。**轴号就是整数索引**。
  只有三件事分族：**伺服使能**（总线 `nmc_set_axis_enable`，CiA402）、**回零**、**总线诊断**。
- **EtherCAT 端口固定 2**；**CiA402 只有状态 4（操作使能）能动**；回零读 `dmc_get_home_result(ref state)`，
  **`state == 1` 才算成功**。
- **IO 默认「整卡位号」= 模块 × `BytesPerModule` + 序号**（官方 19 例程无一用 `nmc_read_inbit`）；
  要「从站节点 + 站内位号」置 `Options.BusIoAddressing = true`。**全链路唯一没有例程依据处。**
- 桥接层有 `WarnIfAxisCardMismatch` / `DiagnoseAxis`。
- **雷赛脉冲卡伺服使能脚低电平有效**（`dmc_write_sevon_pin` 写 0 = 使能）。

## 六、运动控制卡族层（`Services/Hardware/Cards/`）

`Interfaces/`（ICard 22 / IAxis 201 / IIO*）、`Native/`（共享 P/Invoke）、
`Families/<26 卡族>/`（每族 5~7 文件：`CardRealization`/`AxisRealization`/`In*Realization`/`Out*Realization`/`XXXSDK.cs`）。
命名空间 `WenQiZhi.Domain.MotionCard.Common.*`。

- **入口** `CardFamilyCatalog.cs`（注册表+匹配+dll 清单）与 **`WenQiZhiCardBridge.cs`**
  （旧名 `SamsunCardBridge` **已不存在**）。`HardwareSetup.AutoDetectFromProject()`：
  **只有雷赛工程走 `LeadshineHardwareBridge`，出现非雷赛卡族才切 `WenQiZhiCardBridge`。**
- **`Families` 目录名 ≠ 命名空间**（`MCN42Series`→`.YKMCN42Series`、`E64IOSeries`→`.SLDIOE64`、
  `YKMCCE3032`→`.YKMCC_E3032_EtherCAT`、`EC600`→`.EC600_EtherCAT`…）。**断言 `Create().GetType().Namespace`。**
- **`NativeDlls` 必须剥注释后扫 `DllImport` + 扫调用了哪个 Native 包装类**，别按目录名猜
  （曾错 11 个：`HY7X00` 真驱动是 `PCI400.dll`）。
- **`Aliases` 必须同时收录「型号名」和「固件卡型码」**：自动识别写的是 `$"0x{码:X}"`（48 个码）。
  缺固件码 → 混装工程掉到「品牌+总线」兜底、**配到不相干的同品牌卡**。别名是**子串**匹配。
- **轴必须一轴一个 `IAxis` 实例**（`AxisWhichCardNo`/`AxisID` 是实例字段，`NewAxis` 是工厂不是单例）。
- **`GetCardAxisCurrentState()` 0 = 停止 / 1 = 运行中**（看着反，全族一致）。
- **`ListCardParam` 必须预置**（部分族 `OpenCard` 按下标访问，空表 `IndexOutOfRange`）。
- **模拟卡族** `IsVitualCard = true` 跳过真实开卡（脱机调试）。
- **桥接层 `Guard` 捕获所有异常**包成中文 `ScriptRuntimeException`（移植族 `NotImplementedException` 很常见）。
- 参考实现 `GTS800PG` 族**未移植**；缺库 `MCC.dll`/`Dmc2410.dll`/`SLD9014PTP.dll`；`Dmc2210.dll` 是 x86。

## 七、研控 MCN420 使能极性 —— 端口电平、低有效；真机实测为准

**`YK_set_sevon_config(card, axis, sevon_en)` 第三参就是端口电平**（原生声明「0低电平；1高电平」），
使能脚**低电平有效**：**写 0 = 使能、写 1 = 不使能**；`YK_get_sevon_config` 读回 **0 = 已使能**。

**四处必须同步**：① `NmcSetCardAxisEnable` 写 `0` / `Disable` 写 `1`；
② `GetAxisCurrentState()` **bit8** 在 `YK_get_sevon_config(...) == 0` 时置位；
③ `NormalizeSevon(int)` 是**反转映射**（`0→1`、`1→0`），**不是恒等，别改回去**；
④ `CardAxisWriteSevonPin(on_off)` **入参契约不变**（`0→Disable`、非 0→Enable），翻转只在 SDK 内部
（模拟卡分支调它，模拟卡 active-low 恰合契约）。
**曾经的两次错误**（用「文档证据」推翻原生声明）：① 参考实现写 1；② `Mcn420ErrorCode.xml` 第 26 条
「正确应该是0不使能；1使能」。**那条只说「存在一层 1=使能 的语义」，但不在这个 API 上。**
**规矩：参数语义看原生声明（端口电平），极性有效性看真机；不用错误码表反推 API 参数。**
`YK_set_sevon_config` 输出**只覆盖 OUT32–OUT35（轴 0~3）**。
**`dmc_write_sevon_pin`（雷赛/模拟卡）也是端口电平、低有效、方向一致**（不要再写「相反」）。

- 研控全族轴指令是 **unit 版**（`YK_vmove`/`YK_pmove`/`YK_set_command_position`），依赖卡内
  **脉冲当量 = `YK_set_command_ratio`**；桥接层 `EnsureAxisRatio`/`PushAxisRatios` 负责下发。
- **错误码表** `Native/Mcn420ErrorCode.xml`（随程序发布，98 条）：
  `8194`=普通缓冲满、`20480`=0通道轴未运动完成、`26`=使能出错、`60000`=轴被占用、
  `65535`=通讯异常。**8194/20480 与「当量为 0」无关，也不是「没使能」（那是 26）—— 是卡内指令缓冲状态。**
  缺 XML 会让 `GetErrorInfo` NRE → csproj 里 `None + CopyToOutputDirectory=PreserveNewest` 必须留。
- `AxisFailHint(AxisItem, IAxis, int)`：先译码（`CardCodeText` 反射卡族 `GetErrorInfo`，按类型缓存），
  再用 `_lastRatioRes`（键 控制器名#轴号，哨兵 `RatioPushThrew`）说明当量是否写进卡。
  **三个调用点都要传 `(axis, a, res)`**，漏一个 `CS7036`。分两支：
  `IsBufferBusyCode(code)`（10 码 `8192/8193/8194/12288/12289/20480/20481/24576/24577/60000`）
  走**动作支**，其余走 5 项清单；当量结论只在**非缓冲满**时当主因报（`if (!bufferBusy && hasRatio)`）。
  **教训：提示里的排查项若已被用户排除，重复列只会让人不再看提示。**
- **★ 8194 已落地处置**（`StartAxisJog`）：非模拟卡分支在 `EnsureAxisRatio` 前
  **先 `StopCardAxisMovement(cardNo, axis, 1)`（`1`=立刻停）**清卡内缓冲 —— `YK_vmove` 是连续运动、
  不会自己结束，上一条没真停掉就把普通缓冲占死、下条被拒 8194。
  新增 `ClearBufferHint`（反射轴实现 `GetCmdListBufNum(int)` → 日志「指令缓冲占用」一行，
  **只诊断绝不抛**）+ `MCN420SeriesSDK.GetCmdListBufNum` / `AxisRealization.GetCmdListBufNum`（channel 0）。
  8194 提示重写为「①点停再 Jog ②查上一条为何结束不了（未使能/报警/限位）③断电重启」。
  顺带修 `BuildSimParam` 的 `OrgLevel` 误读 `EnableLevel` → 改 `OriginLevel`。

## 八、验证 UI 的硬约束（本机）

- **驱动 UI 用 `ctrl.GetInvokePattern().Invoke()`**，不合成鼠标。导航项是 `TextBlock`，
  要沿 `GetParentControl()` 上溯到外层 `ButtonControl`。`uiautomation` 2.0.29 **没有** `GetSupportedPatterns()`。
- **「本机没有交互式桌面」旧结论已不成立**（曾 `GetForegroundWindow()==NULL`，后实测非 0、光标 (1203,453)）→
  **每次跑之前先打印这几个值再决定**。
- **`PrintWindow` 可靠性不能假定**（无交互桌面时客户区全白）→ **以 UIA 文本为准**，截图不当视觉证据。
- **读 ComboBox 候选要取它自己的 `ListItemControl` 子孙**（内部可编辑文本框本身也有名字，会让你误判读到头）；
  靠标签 y 坐标就近配对。
- 断言「计数标签是活的」要**连粘两次不同条数**（如 7 → 3）。
- 现成脚本 `%TEMP%\ncm_ui\verify_dropdowns2.py`，用 managed python 跑（系统 3.12 没有 `uiautomation`）。
