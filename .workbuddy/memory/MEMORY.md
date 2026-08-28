# 项目长期记忆

## 源码状态：当前全部 .cs 为明文（2026-08-27 实测无 0x88）
- ⚠️ **重要更正**：此前多轮记忆称"Models/Editing 首字节 0x88 加密、需走补丁"。2026-08-27 用脚本逐文件查首字节：**103 个 .cs 全部首字节正常（`using`/`#nu`/BOM），0 个 0x88 加密**。当前磁盘状态=**全明文，均可直接用 Read/Edit/Python 改**，不再需要补丁或请用户解密。
- 若日后重新出现"Read 报 binary / 首字节 0x88"，再按加密处理；常态下按明文处理。
- 明文文件若 `Read`/`Edit` 报 "binary file"（多为 GBK/BOM/CRLF），改用 Python 读写（读 utf-8/gbk，写 utf-8，`newline="\n"`）；GBK 文件可安全升级为 UTF-8(BOM)。

## 操作员「启动」= 并发跑全部流程循环（非工位序列）
- 用户最终选择（澄清）：点「启动」= 并发跑 `ProjectStore.Data.Flows` 里每个 Flow 的「循环开始/循环结束」区域，次数取 `SetValue`；不需要选工位。
- 落地方式：新增文件 `ViewModels/FlowRunnerService.cs`（独立并发流程运行器，复用同一套 `HardwareBridge`/`HardwareResolver` 硬件接口），并在 `OperatorViewModel.cs` 接入：
  - `CanRun/CanStart` 放宽到 `!IsRunning && !EStopped && (Flows.Count>0 || HasAnyRunnableTable())`。
  - `Start()` 自动模式优先 `StartFlows()`（每条 Flow 一个 `Task.Run` 并发），无流程时回退到旧工位序列 `RunLoop()`。
  - `StartFlows()` 用 `FlowRunControl`（停止/急停/暂停标志 + 变量表，双向同步 `ProjectStore.Data.Variables`）驱动 `FlowRunnerService.RunAllAsync`，支持暂停/停止/急停；进度/完成回调经 `Ui()` 封送回 UI。
  - `EStop/Stop/Pause/Resume` 同时给 `_flowCtrl` 发信号。
- 解释器覆盖：循环（循环开始/循环结束，次数取 SetValue，封顶 1e5 防死循环）、如果/否则如果/否则/结束 分支、等待/延时、注释/就/并且/或者（当前按 no-op 近似）、轴=MoveAxisAbs(+SetAxisSpeed/HomeAxis/StopAxis/MoveAxisRel)、IO=WriteOutput、气缸=CylinderMove/CylinderReset、点位=按 4 轴槽走点、modbus=CommSend、变量=按 Operation 算术写入（同步回 VariableRow）、系统=Log、相机=暂跳过。变量支持 `{name}` 引用。
- 此实现是对 FlowViewModel 执行核心的"最佳独立复刻"，硬件 API 由反射编译 DLL 核对；若与流程页单步行为有出入，需用户反馈后对齐（`FlowViewModel.cs` 现已确认是明文、可改，如需合并可后续处理，不必另走补丁）。
