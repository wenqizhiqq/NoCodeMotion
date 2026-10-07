# NoCodeMotion 长期项目笔记
> 精炼索引，只留最容易重复踩、一句话说不清的坑。完整推导在 `.workbuddy-ai/memory/` 逐日日志（2026-09-19 起）。

## 一、构建 / 验证
- **用 PowerShell 跑 dotnet，不要 Bash**：Bash 输出全空、`*> log` 是 **UTF-16LE**，解码 `iconv -f UTF-16LE -t UTF-8` 再 grep；`Get-Item` 核 DLL 大小/时间戳。**空日志 ≠ 成功**。
- `NuGet … null ('path1')`/`NETSDK1060` → shell `APPDATA`/`ProgramFiles` 空：补 `env "APPDATA=…" "ProgramFiles=C:\Program Files" "ProgramFiles(x86)=…" dotnet build`（bash 不能 export 带括号名）。
- **★ `%TEMP%` 进程读 `D:\` 拿沙箱替换（静默乱码）→ 源码文本守卫必须在仓库目录跑**（Python 可靠）。症状：`Contains` 在肉眼可见串返回 false。
- 冒烟工程 `%TEMP%\ncm_smoke\`（`ProjectReference` 指本工程、`net10.0-windows`+`UseWPF=true`、**须在工程目录外**）；守卫 `%TEMP%\ncm_patch\guard_sources.py`（G1–G14，仓库目录跑，129 项全 PASS）。
- 补丁助手 `%TEMP%\ncm_patch\patch_lib.py`（`Patcher`：按文件自身 BOM/行尾精确替换，`sub` 多编辑内存累积最后写盘；`insert_after(anchor,text)` 看**锚点末尾字符**——可见字符则先换行再插（防方法粘同行）、换行则直接插，`text` 前导换行被剥掉由本函数统一决定）。
- 冒烟与 `--no-incremental` 构建不可并行（互删 `obj\` → `CS0103 InitializeComponent`/`CS5001 无 Main`/`CS2001 缺 *.g.cs`，不自愈）→ 见这三类错先怀疑 obj 污染，`rm -rf obj bin` 全量 build。`-t:Rebuild` 跳过 CoreCompile 报假「0 警告」。
- 与别的编辑器并发时构建会报真实感极强的假 `CS1061` → 先看 mtime 再重跑。临时目录带点前缀（`.bt/`）不编译，不带点（`scratch/` `tmp/`）会被 `**/*.cs` 收走。

## 二、代码约定
- 隐式 using 只有 System/Collections.Generic/Linq/Threading/Tasks（无 System.Windows、无 System.IO）。
- 行尾/BOM 逐文件判断，保持该文件风格；头尾作者水印注释（含零宽 U+200B/U+2063/U+200D）按字节保留。
- 多编辑同一文件先内存累积、最后一次写盘；写回不再做行尾转换（产 CRCRLF）。Python 多行字面量先归一化行尾再匹配，注入块里不写 `\uXXXX`。
- `core.autocrlf=true` → `git show HEAD:file` 行尾与工作区不一致，看工作区。

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
