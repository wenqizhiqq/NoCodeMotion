# 项目长期记忆

## 源码状态：全部 .cs 为明文（已逐字节核实）
- **本仓库所有 .cs 文件均为明文 UTF-8，可直接 Read/Edit/Python 改写**。命令 `grep -rIl $'\x88\x7d\x1c' --include=*.cs .` 结果为 **0**（103 个 .cs 无一带该标记）；被旧记忆列为「加密」的样本文件（`ViewModels/FlowViewModel.cs`、`Models/FlowItem.cs`、`Services/HardwareBridge.cs`、`Services/HardwareResolver.cs`、`Views/LuaEditorView.xaml.cs`）首字节均为 `75 73 69`/`23 6e 75`（"usin"/"#nu"），即明文。
- ⚠️ 旧记忆曾两度写反（先称「全明文」，后又称「118 个 .cs 按文件加密」）——**一律以本地逐字节扫描为准：当前为全明文、无加密屏障**。不要因旧记忆对 .cs 走「补丁文本 / 解密」流程，也不要因此对任何 .cs 文件做逆向/破解。
- 注意：含 BOM(utf-8-sig)/GBK/CRLF 的文件，`Read`/`Edit` 可能报 "binary file" 而失败，此时用 Python 读写（读 utf-8/gbk，写 utf-8，`newline="\n"`）。

## 操作员「启动」= 并发跑全部流程循环（非工位序列）
- 用户最终选择（澄清）：点「启动」= 并发跑 `ProjectStore.Data.Flows` 里每个 Flow 的「循环开始/循环结束」区域，次数取 `SetValue`；不需要选工位。
- 落地方式：新增文件 `ViewModels/FlowRunnerService.cs`（独立并发流程运行器，复用同一套 `HardwareBridge`/`HardwareResolver` 硬件接口），并在 `OperatorViewModel.cs` 接入：
  - `CanRun/CanStart` 放宽到 `!IsRunning && !EStopped && (Flows.Count>0 || HasAnyRunnableTable())`。
  - `Start()` 自动模式优先 `StartFlows()`（每条 Flow 一个 `Task.Run` 并发），无流程时回退到旧工位序列 `RunLoop()`。
  - `StartFlows()` 用 `FlowRunControl`（停止/急停/暂停标志 + 变量表，双向同步 `ProjectStore.Data.Variables`）驱动 `FlowRunnerService.RunAllAsync`，支持暂停/停止/急停；进度/完成回调经 `Ui()` 封送回 UI。
  - `EStop/Stop/Pause/Resume` 同时给 `_flowCtrl` 发信号。
- 解释器覆盖：循环（循环开始/循环结束，次数取 SetValue，封顶 1e5 防死循环）、如果/否则如果/否则/结束 分支、等待/延时、注释/就/并且/或者（当前按 no-op 近似）、轴=MoveAxisAbs(+SetAxisSpeed/HomeAxis/StopAxis/MoveAxisRel)、IO=WriteOutput、气缸=CylinderMove/CylinderReset、点位=按 4 轴槽走点、modbus=CommSend、变量=按 Operation 算术写入（同步回 VariableRow）、系统=Log、相机=暂跳过。变量支持 `{name}` 引用。
- 此实现是对 FlowViewModel 执行核心的"最佳独立复刻"，硬件 API 由反射编译 DLL 核对；若与流程页单步行为有出入，需用户反馈后对齐（`FlowViewModel.cs` 现已确认是明文、可改，如需合并可后续处理，不必另走补丁）。
