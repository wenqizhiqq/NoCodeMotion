# 项目长期记忆

## 源码加密屏障（关键，已核实）
- 仓库 .cs 文件**按文件加密**：绝大多数 .cs 首字节是 `88 7d 1c` 标记（加密），由定制版 Roslyn 编译器在 `dotnet build` 时透明解密并编译（所以构建 0 错误），但 AI 的 Read/Edit 工具读到的是密文，无法直接读写。
- **明文可编辑**的例外（首字节 `75 73 69 6e` = "usin"）：`ViewModels/OperatorViewModel.cs`、`Services/ProjectStore.cs`、`Services/ProjectManager.cs`、`Services/XlsxProjectStore.cs`，以及 `obj/` 下生成的 `.g.cs`。
- 已核实加密（不可直接改）：`ViewModels/FlowViewModel.cs`、`Models/*`、`Services/Hardware*`、`Services/Catalog.cs`、`Services/Timing*`、`Views/*`、`Editing/*` 等。
- 规则：AI 只编辑明文 .cs 或新建明文 .cs；任何加密 .cs 的改动必须以「补丁文本」交用户在 VS 粘贴，或请用户运行其解密工具使文件转明文后再编辑。**绝不对加密文件做逆向/破解尝试**。
- 之前有一版记忆误写成"全部 .cs 明文无加密"，已纠正——那是错的。

## 操作员「启动」= 并发跑全部流程循环（非工位序列）
- 用户最终选择（澄清）：点「启动」= 并发跑 `ProjectStore.Data.Flows` 里每个 Flow 的「循环开始/循环结束」区域，次数取 `SetValue`；不需要选工位。
- 落地方式（因 `FlowViewModel.cs` 加密、不能改）：新增**明文**文件 `ViewModels/FlowRunnerService.cs`（独立并发流程运行器，复用同一套 `HardwareBridge`/`HardwareResolver` 硬件接口），并在明文 `OperatorViewModel.cs` 接入：
  - `CanRun/CanStart` 放宽到 `!IsRunning && !EStopped && (Flows.Count>0 || HasAnyRunnableTable())`。
  - `Start()` 自动模式优先 `StartFlows()`（每条 Flow 一个 `Task.Run` 并发），无流程时回退到旧工位序列 `RunLoop()`。
  - `StartFlows()` 用 `FlowRunControl`（停止/急停/暂停标志 + 变量表，双向同步 `ProjectStore.Data.Variables`）驱动 `FlowRunnerService.RunAllAsync`，支持暂停/停止/急停；进度/完成回调经 `Ui()` 封送回 UI。
  - `EStop/Stop/Pause/Resume` 同时给 `_flowCtrl` 发信号。
- 解释器覆盖：循环（循环开始/循环结束，次数取 SetValue，封顶 1e5 防死循环）、如果/否则如果/否则/结束 分支、等待/延时、注释/就/并且/或者（当前按 no-op 近似）、轴=MoveAxisAbs(+SetAxisSpeed/HomeAxis/StopAxis/MoveAxisRel)、IO=WriteOutput、气缸=CylinderMove/CylinderReset、点位=按 4 轴槽走点、modbus=CommSend、变量=按 Operation 算术写入（同步回 VariableRow）、系统=Log、相机=暂跳过。变量支持 `{name}` 引用。
- 此实现是对 FlowViewModel 执行核心的"最佳独立复刻"，硬件 API 由反射编译 DLL 核对；若与流程页单步行为有出入，需用户反馈后对齐（FlowViewModel.cs 加密，不擅自改）。
