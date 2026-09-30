# NoCodeMotion 长期项目笔记

> 精炼索引（2026-09-30 整理）。只留最容易重复踩、且一句话说不清的坑。
> **完整细节与推导过程在 `.workbuddy/memory/` 的逐日日志里（2026-09-19 ~ 2026-09-30），需要时去查。**

## 一、构建 / 验证（最重要）

**本机 `dotnet build` 能跑。** 报 `NuGet ... Value cannot be null. (Parameter 'path1')` 或
`NETSDK1060 加载 obj/project.assets.json 失败`，根因是 shell 里 `APPDATA` / `ProgramFiles` 为空字符串，
**不是 SDK 坏 / 代码错 / obj 损坏**。用 `env` 补（bash 无法 export 带括号的名字）：

    env "APPDATA=C:\Users\admin\AppData\Roaming" "ProgramFiles=C:\Program Files" \
        "ProgramFiles(x86)=C:\Program Files (x86)" dotnet build -v q --no-restore --nologo

基线 **0 错误**，约 590~618 条 `CA1416`（AvalonEdit 跨平台分析，既有；`_wpftmp` 那遍会重复计一次）。

**★ 用 PowerShell 跑 dotnet，不要用 Bash**：Bash 里 `dotnet --version` / `build` 都是
「**空输出 + 退出码 0**」，重定向到文件也是 0 字节。**空日志 ≠ 构建成功** ——
表现是「构建成功但 DLL 时间戳不动、新加的 CopyToOutputDirectory 文件没进 bin」。
设 `[Console]::OutputEncoding=[System.Text.Encoding]::UTF8` 否则中文诊断乱码；
**构建完用 `Get-Item` 核对产物大小 / 时间戳**，别只看退出码。

**★ 从 `%TEMP%` 启动的进程读 `D:\` 下的文件会拿到被沙箱替换的内容**（不是报错）：
实测 `File.ReadAllBytes` 返回 237036 字节乱码、无 BOM，真实文件是 232940 字节 UTF-8+BOM。
→ **源码文本守卫必须在仓库目录里跑**（Python 可靠），运行时断言才放 `%TEMP%` 冒烟工程。
踩坑表现：`Contains` 在一个你肉眼可见的字符串上返回 false。

**冒烟测试** `C:/Users/admin/AppData/Local/Temp/ncm_smoke/`（`ProjectReference` 指向本工程，
`net10.0-windows` + `UseWPF=true`，控制台 Exe 也必须开 WPF；`NoWarn=NU1701;CA1416;CS8632`；
**必须放在工程目录之外**）。`System.IO` 不在隐式 using 里，要显式 `using System.IO;`。
**2026-09-30 `%TEMP%` 被清理过，原先 708 条断言的工程已丢失**，现为聚焦版（`S1~S14` 错误码表 /
`T1~T4` `NormalizeSevon` / `U1~U14` `AxisFailHint` / `V1~V3` `IsBufferBusyCode`）→ **37 PASS / 0 FAIL**；
源码文本守卫另放 `%TEMP%\ncm_patch\guard_sources.py`（**在仓库根目录跑**）→ 54 PASS。

**冒烟与 `--no-incremental` 构建绝对不能并行**：都写工程根下的 `obj\`，并行会互删中间产物，
报成片**假错**（`CS0103 InitializeComponent` / `CS5001 没有 Main` / `CS2001 缺 *.g.cs`），
**不会自愈**（增量构建认为 obj 最新，不重新生成 XAML 的 `*.g.cs`）。
**看到这三类错误先怀疑 obj 被污染，不要改代码**；修法 `rm -rf obj bin` 再全量 build（约 37 秒）。
**`-t:Rebuild` 会跳过 CoreCompile 报假的「0 警告」**；要真实警告清单必须 `--no-incremental -v n`。

**临时目录**：带点前缀（`.bt/`）默认**不**参与编译；不带点的（`scratch/`、`tmp/`、`test/`、`_out/`）
会被 `**/*.cs`、`**/*.xaml` 收走（csproj 只 `Remove` 了 `artifacts\**` 与 `_out\**`）。

## 二、代码约定

- **隐式 using 只有 System / Collections.Generic / Linq / Threading / Tasks** ——
  **不含 `System.Windows`，也不含 `System.IO`**。
- **行尾 / BOM 逐文件判断，不要假设全仓统一**（`Models/ProjectTemplate.cs` 是 LF，`Models/PointItem.cs` 是
  CRLF，多为 UTF-8 带 BOM）。保持该文件自身风格，改完复查 bareLF / CRCRLF。
- **文件头尾有作者水印注释**（含零宽字符 U+200B/U+2063/U+200D），必须按字节原样保留。
- `core.autocrlf=true` → `git show HEAD:file` 的行尾与工作区不一致，要看工作区文件本身。
- **多编辑同一文件必须先内存累积、最后一次性写盘**（每次 `sub()` 重读磁盘会只剩最后一个编辑）；
  写回时不要对已归一化的文本再做一次行尾转换（产出 `CRCRLF`）；写盘后复查「每个新 token 恰好 1 次」。
  出事用 `git checkout -- <files>` 还原再重跑。

## 三、架构要点

- `Catalog`（`Services/Catalog.cs`）是**下拉框用的全局名称缓存**，只在 `ProjectStore.Load()` 与
  `ProjectManager.LoadInto()` 里经 `SyncAllFromData` 重建，配置页靠 `ListEditorViewModel.SyncCatalog()`
  + `CatalogCategory` 增量刷新。**它不是数据源** —— 判断「某设备是否存在」要读 `ProjectStore.Data`。
- 自动保存链路：`Model.PropertyChanged → Parent.OnChildChanged → OnPropertyChanged(nameof(Children))
  → ListVM.OnItemPropertyChanged → ProjectStore.ScheduleSave()`。
  **任何运行态 / 只读展示属性都必须在这条链上被过滤掉**，否则每秒几十次写盘。
- `PointItem.ConditionRowCount = 6`，`EnsureConditionRows()` 规整为固定 6 行并裁尾部空行。
  **往条件集合塞数据只能按下标覆写，不能 Append。**
- **「轴号 / IO 序号」一律从 0 开始，且同一 Controller 内不可重复**（和 IO 位号同一套约定）。
  显示名是「轴1..轴N」「输入1..」，**但底层号是 0..N-1** —— 名字比号大 1 是刻意的，别去「对齐」。
  自动生成见 `AxisControllerViewModel.GenerateAxisPoints`（`MakeAxis(tag, s, s - 1)`）与
  `GenerateIoPoints`（主板 `MakeIo(..., 0, s)`）；**两处以前都从 1 生成，是已修的错位 bug**，
  代码里有 ★ 注释，别再改回去。
  **★ 重复轴号是静默灾难**：`WenQiZhiCardBridge.AxisOf` 里 `slot.Axes` 是
  `Dictionary<轴号, IAxis>`，而卡族的读位置 / 回零 / 读状态**都不带卡号轴号**（读实例字段
  `AxisID`）→ 两根轴填同一轴号时第二根**复用第一根的 `IAxis` 实例**，两轴指向同一物理通道、
  卡内缓冲互相抢占 → **Jog 报 8194「普通缓冲满」**。现已加一次性告警
  `_warnedAxisDuplicate`（键 = 控制器名#轴号）。现场「手工建表按轴1=1 的直觉填成 0/0」就会中招。

## 四、表格 / 流程页

- **`TableToolbar` 的「共 N 项/步」文案来自 `MatchInfo`，不是 `Count`**（`Count`/`CountLabel` 只参与拼接）。
  行数权威来源是 `TargetGrid.ItemsSource`，靠订阅 `ItemContainerGenerator.ItemsChanged` 刷新。
  **加计数类展示前先确认自己绑的是不是真正被显示的那个属性。**
- **流程「名称」列是受限下拉**：`FunctionToNamesConverter` → `Catalog.*Names`，值不在库里渲染成**空白**
  （作者既定设计）。**不要为了显示未知值把当前值并进 `ItemsSource`（MultiBinding）**：
  `SelectedItem` 默认 TwoWay，ItemsSource 换实例会被 WPF 置空并回写 `null`。现做法是导入时提示。
- `AiProjectExchange` 输出的 JSON 要粘进 AI 对话，**必须用 `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`**。
- **快照 / 回退**：`RestoreFlows(sink, snapshot)` **解析失败返回 false 且绝不动 sink**；空快照 `"[]"` 合法。
  `_pasteUndo` 是 `Stack<PasteUndo>`，**只有快照真的变了才压栈**。
- **`ExportSteps` 必须导出 `FlowStep.DurationMs`（键名 `耗时`）**。规矩：**加字段时 `Export*` 与 `Fill*`
  必须同时改**，否则快照与 AI 往返静默丢字段。
- `Views.ConfirmDialog(title, message, confirmText)`；入口按钮「回退」、弹窗按钮「确定回退」**刻意不同名**。
- `Views/FlowPage.xaml` Row-0 是「复制JSON / 粘贴生成 / 回退」的落点，四种流程共用。
  **该区域在 `EditorPage.Detail` 里，不能起 `x:Name`（MC3093）。**
- **提示词里的【Lua 可用的 API】**由 `AiProjectExchange.BuildLuaApiText()` 生成，六组硬件 API
  **直接从 `LuaApi.HardwareList` 生成** → **往 `LuaApi` 加函数不用改提示词**。
  Lua 沙箱真实可用的 API **唯一权威来源是 `Services/HardwareApi.cs` 的 `Register()`**；
  `Camera`/`Vision` **确定不注册**（独立 feature）。
- 视觉流程契约是 **`视觉步骤` 数组**，**不是 Lua**。

## 五、雷赛硬件层（动硬件前必读）

- **`LtdmcNative.cs` 是全项目唯一的 P/Invoke 声明处**（63 个）。`LtdmcSdk.cs`（774 个声明）
  **零引用**、已被取代，其中 3 个声明在 DLL 里根本不存在。
- **LTDMC.dll 有两套互不相通的函数族，这是「函数返回 0 但轴不动」的总根源**：
  `dmc_*` = 板卡本地资源 → **脉冲卡**；`nmc_*` = 总线主站资源 → **EtherCAT/CANopen（DMC-E 3000/5000）**。
- **反直觉点：总线卡上「运动」仍用 `dmc_*`**（官方例 1/例 7 实测）：`dmc_set_profile_unit`(7 参) /
  `dmc_pmove_unit` / `dmc_get_position_unit(card, axis, ref double)` / `dmc_check_done`
  (**0=运动中, 1=到位**) / `dmc_set_position_unit` / `dmc_stop`。**轴号就是整数索引，不换算从站地址。**
  只有三件事分族：**伺服使能**（总线必须 `nmc_set_axis_enable`，CiA402）、**回零**、**总线诊断**。
- **EtherCAT 端口号固定为 2**；**CiA402 状态机只有 4（操作使能）能动**；回零完成读
  `dmc_get_home_result(…, ref state)`，**`state == 1` 才算成功**。
- **IO 寻址默认「整卡位号」= 模块 × `Options.BitsPerModule` + 序号**（同官方例 8）。官方 19 个例程
  **没有一个**用 `nmc_read_inbit`；要「从站节点号 + 站内位号」时置 `Options.BusIoAddressing = true`。
  **这是全链路唯一没有例程依据的地方。**
- 桥接层有 `WarnIfAxisCardMismatch`（轴类型与卡型不一致明确报警）与 `DiagnoseAxis`（超时异常带读数）。
- **雷赛脉冲卡的伺服使能脚低电平有效**（`dmc_write_sevon_pin` 写 0 = 使能）—— 与研控 MCN420 相反。

## 六、已移植运动控制卡族层（`Services/Hardware/Cards/`）

`Interfaces/`（`ICard` 22 / `IAxis` 201 / `IIOInPut` / `IIOOutPut` / `IEIO*`）、`Native/`（共享 P/Invoke 包装）、
`Families/<26 卡族>/`（每族 5~7 文件：`CardRealization` / `AxisRealization` / `InioRealization` /
`OutioRealization` / 可选 `Expand*Realizetion` / `XXXSDK.cs`）、`Compat/`。
命名空间 `WenQiZhi.Domain.MotionCard.Common.*`。

- **入口**：`CardFamilyCatalog.cs`（注册表 + 匹配 + dll 清单）与 **`WenQiZhiCardBridge.cs`**
  （**旧名 `SamsunCardBridge` 已不存在，全仓零引用**）。`HardwareSetup.AutoDetectFromProject()`：
  **只有雷赛工程走 `LeadshineHardwareBridge`，出现非雷赛卡族才切 `WenQiZhiCardBridge`。**
- **`Families` 目录名 ≠ 真实命名空间**，别按目录名猜（`E64IOSeries`→`.SLDIOE64`、
  `MCN42Series`→`.YKMCN42Series`、`SoftServo`→`.SoftServo_EtherCAT`、`YKMCCE3032`→`.YKMCC_E3032_EtherCAT`、
  `YMCC1200P`→`.YKMCC1200P`、`EC600`→`.EC600_EtherCAT`）。**断言 `Create().GetType().Namespace` 最省事。**
- **`NativeDlls` 必须「剥注释后扫 `DllImport` + 扫调用了哪个 Native 包装类」得出，不能按目录名猜**
  （曾猜错 11 个：`HY7X00` 真实驱动是 `PCI400.dll` 不是 LTDMC.dll；不剥注释则 `//return LTDMC.xxx(...)`
  满仓都是，会把 LTDMC 归给几乎每个模块）。
- **`Aliases` 必须同时收录「型号名」和「固件卡型码」**：自动识别写进「卡型号」的是
  `$"0x{固件卡型码:X}"`（`AxisControllerViewModel.AutoDetect`），共 48 个码。缺了固件码，混装工程里
  这张卡会掉到「品牌 + 总线」兜底分支、**被配上不相干的同品牌卡**。别名是**子串**匹配，
  短数字码（如 MCN42 的 `420`）只登记十六进制形式避免串味。
- **轴必须一轴一个 `IAxis` 实例**（`AxisWhichCardNo`/`AxisID` 是实例字段，而 `GetCardAxisCurrentPosition`
  / `CardAxisHomeMove()` / `GetCardAxisCurrentState()` 都不带卡号轴号 → `NewAxis` 是工厂不是单例）。
- **`GetCardAxisCurrentState()` 返回 0 = 停止 / 1 = 运行中**（看着反，全族一致）。
- **`ListCardParam` 必须预置**（部分卡族在 `OpenCard` 里按下标访问，空表直接 `IndexOutOfRange`）。
- **模拟卡族**把 `IsVitualCard = true` 后跳过真实开卡直接成功（脱机调试用）。
- **桥接层 `Guard` 捕获所有异常**并包成中文 `ScriptRuntimeException`（移植族里
  `throw new NotImplementedException()` 很常见，不能让脚本中断）。
- 参考实现的 `GTS800PG` 族**没有移植**；缺的底层库：`MCC.dll`、`Dmc2410.dll`、`SLD9014PTP.dll`；
  `Dmc2210.dll` 是 x86。

## 七、研控 MCN420 使能极性 —— 曾经反了，别再改回去

**症状**：操作员点了「使能」，Jog 仍失败，卡返回 **8194**（`轴「轴1」Jog 失败：Jog 下发失败（卡返回 8194）`）。

**根因**：`YK_set_sevon_config(card, axis, sevon_en)` 第三参是**逻辑使能**（1=使能, 0=不使能），
不是端口电平。2026-09-30 的提交按 Native 注释「0低电平；1高电平」把 `NmcSetCardAxisEnable` 改成写 `0`
→ 点「使能」实际**断电**，卡于是拒绝运动 / 置位指令。现已改回 **使能写 1 / 不使能写 0**。

- 两条独立证据：① 参考实现 `AxisRealization.CardAxisWriteSevonPin(on_off)`：`0 → Disable`，否则 Enable；
  ② `Mcn420ErrorCode.xml` 第 26 条原文 **`使能出错 ,正确应该是0不使能；1使能`**。
- **`dmc_write_sevon_pin`（雷赛 / 模拟卡）是相反的（低电平有效，0 = 使能）**，两套不可混用。
  参考实现的**模拟卡**读回 `v == 0 ? 0 : 1`（0 = 使能）→ 与研控真实卡**互相矛盾**，
  **真机必须实测确认 `1 = 使能`**（尚未实测）。
- 同步改动：状态字 **bit8** 在 `YK_get_sevon_config(...) == 1` 时置位；`NormalizeSevon(int)` 改为
  **恒等映射**（刻意恒等，不是笔误）。`YK_set_sevon_config` 的输出**只覆盖 OUT32–OUT35，即轴 0~3**。
- 研控全族轴指令都是 **unit 版**（`YK_vmove` / `YK_pmove` / `YK_set_command_position`），依赖卡内
  **脉冲当量 = `YK_set_command_ratio`(pulses/unit)**；桥接层 `EnsureAxisRatio`/`PushAxisRatios` 负责下发。

**错误码表** `Mcn420ErrorCode.xml`（随 MCN420.dll 发布，md5 `14cede6b742678ae93c50530ccb229d5`，98 条）：
`8192/8193`=0/1 通道缓冲满；**`8194`=普通缓冲满**；`12288/12289`=连续轨迹缓冲满；`16384/16385`=写入指令异常；
`20480/20481`=0/1 通道轴未运动完成；`24576/24577`=前瞻未完成写入 JOG 出错；**`26`=使能出错**。
编码规律：值 = (族 << 12) | 通道号。
**8194 / 20480 与「脉冲当量为 0」无关，也不是「没使能」**（那是 26）—— 它们是**卡内指令缓冲状态**。

**已落地（2026-09-30）**：
- XML 放到 `Native/Mcn420ErrorCode.xml`，csproj 加 `None + CopyToOutputDirectory=PreserveNewest`
  （照 `Native\MCN420.dll` 那行）。**缺了它 `GetErrorInfo` 会 NRE**（原实现直接
  `errorcodeDic.Items.FindIndex`，Items 为 null）。
- `GetErrorInfo` / `GetEtherCATErrorInfo` 走 `FormatErrorCode`（XML 缺失给明确兜底、绝不抛），
  表改**惰性 + 只试一次** `EnsureErrorCodeItems()`，探测 `BaseDirectory` / `Native/` / `Cards/` 三处。
- `NormalizeCodeText()`：表里每条 `<zh_cn>` 都带 XML 缩进与换行，不折成单行会毁掉状态栏排版。
- `AxisFailHint(AxisItem, IAxis, int)`：先译码（`CardCodeText` 反射卡族的 `GetErrorInfo`，按类型缓存），
  再用 `_lastRatioRes`（键 = 控制器名#轴号，哨兵 `RatioPushThrew`）说明**当量到底有没有写进卡**。
  **三个调用点都要传 `(axis, a, res)`** —— 漏一个就是 `CS7036`（Jog / 点动 / 设零点各一处）。
- **提示分两支**（2026-09-30 追加，因操作员连续排除 ①使能、③轴号）：
  `IsBufferBusyCode(code)` 判 **10 个缓冲占用类**码
  `8192/8193/8194/12288/12289/20480/20481/24576/24577/60000` →
  这一支**只给动作**「先到轴页点「停止」清空卡内缓冲，再 Jog」，**不再列 ①…⑤、不再提轴号/当量**
  （仅在「点停止后仍报同一码」时才提 使能/报警/限位，因为那才是缓冲不释放的真因）；
  其它码走原 5 项清单。当量结论只在**非缓冲满**时才当主因报（`if (!bufferBusy && hasRatio)`）。
  **教训：提示里的排查项若已被用户排除，重复列它只会让人不再看提示。**
- `CardFamilyCatalog.cs` 里 Note 的两处错误说法已清掉（曾写「写 0 = 使能」
  「当量为 0 会被卡拒绝返回 8194/20480」）。

## 八、受限下拉字段：写入值必须落在 Catalog 候选集里

- **`Catalog.BusTypeNames` 由 `CardVendorRegistry.Vendors[].BusTypes` 生成**（`RefreshControllerStandards()`，
  由 `SyncAllFromData` 调用）。下拉只认这些值 —— **写出候选之外的值会被 WPF 渲染成空白，像「功能坏了」**。
  踩坑：`CardBusType.Other`（→「其它」）曾无任何厂商声明。**写值前先断言 `Catalog.*Names.Contains(值)`。**
- `AxisControllerViewModel.AutoDetect()` 曾把 `BusType` **写死成「脉冲」** → 插 DMC-E 总线卡也登记成脉冲卡。
  现在读 `LtdmcCard.FirstCard` 照实登记（`DescribeBusType` 负责卡型 → 总线类型）。
- `HardwareSetup.StatusMessage` 同时被 Lua 的 `HardwareStatus()` 读走，卡型 / 总线状态要写在这个串里。

## 九、验证 UI 的硬约束（本机）

- **驱动 UI 一律用 `ctrl.GetInvokePattern().Invoke()`**，不要合成鼠标。导航项是 `TextBlock`，
  要沿 `GetParentControl()` 上溯到外层 `ButtonControl` 再 Invoke。
  `uiautomation` 2.0.29 **没有** `GetSupportedPatterns()`（那是裸 UIA COM API）。
- **「本机没有交互式桌面」的旧结论已不成立**（曾观测 `GetForegroundWindow()==NULL`、光标 (0,0)；
  后来实测返回非 0、光标在 (1203,453)）。**每次跑之前先打印这几个值再决定**。
- **`PrintWindow` 可靠性不能假定**（无交互桌面时客户区全白）。截图不能当视觉证据，**以 UIA 文本为准**。
- **读 ComboBox 候选要取它自己的 `ListItemControl` 子孙**：ComboBox 内部那个可编辑文本框本身就是
  有名字的 TextControl（显示当前选中值），会让你刚读到第一项就以为结束。靠标签 y 坐标就近配对。
- 断言「计数标签是活的」要**连粘两次不同条数**（如 7 → 3）。
- 现成脚本 `%TEMP%\ncm_ui\verify_dropdowns2.py`，用 managed python 跑
  （`C:\Users\admin\.workbuddy-ai\binaries\python\versions\3.13.12\python.exe -u`；系统 Python 3.12 没有
  uiautomation）。
