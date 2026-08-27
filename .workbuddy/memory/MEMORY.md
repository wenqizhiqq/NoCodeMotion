# 项目长期记忆

## 源码状态：明文与加密混存（首字节 0x88 = 加密）
- **仓库 .cs 并非全部明文，而是混存**：部分文件首字节为 `0x88`（加密）。已抽样确认加密的含 `Models/*`、`Editing/*`（如 `Models/FlowItem.cs`、`Editing/LineHighlightRenderer.cs` 首字节均为 `0x88`）；`Read`/`Edit`/Python 读到的是乱码/二进制，且沙箱的 Git Bash 在 `dotnet build` 读取这些密文时会把字节注入 shell 导致命令失败（PowerShell 不受影响）。
- **明文可直接改**的已确认：`ViewModels/*`（`FlowViewModel`/`FlowRunnerService`/`ListEditorViewModel`/`OperatorViewModel` 等，首字节 `#nu` 或 `usin`）、`Services/LuaDebugSession.cs`、`Views/LuaEditorView.xaml.cs`、`Views/FlowPage.xaml.cs`。
- ⚠️ 早期记忆两度写反（"全明文" / "118 个按文件加密"），本会话又一度误判为"全明文"——**真正判定法看首字节**：`0x88`=加密（不能直接改），否则=明文。之前 grep 的 3 字节标记 `88 7d 1c` 在本仓库返回 0，并非实际加密签名，不可用。
- 加密文件处理：**走补丁文本 / 请用户在 VS 解密后再改**，绝不做逆向/破解。明文文件若 `Read`/`Edit` 报 "binary file"（多为 GBK/BOM/CRLF），改用 Python 读写（读 utf-8/gbk，写 utf-8，`newline="\n"`）。

## 操作员「启动」= 并发跑全部流程循环（非工位序列）
- 用户最终选择（澄清）：点「启动」= 并发跑 `ProjectStore.Data.Flows` 里每个 Flow 的「循环开始/循环结束」区域，次数取 `SetValue`；不需要选工位。
- 落地方式：新增文件 `ViewModels/FlowRunnerService.cs`（独立并发流程运行器，复用同一套 `HardwareBridge`/`HardwareResolver` 硬件接口），并在 `OperatorViewModel.cs` 接入：
  - `CanRun/CanStart` 放宽到 `!IsRunning && !EStopped && (Flows.Count>0 || HasAnyRunnableTable())`。
  - `Start()` 自动模式优先 `StartFlows()`（每条 Flow 一个 `Task.Run` 并发），无流程时回退到旧工位序列 `RunLoop()`。
  - `StartFlows()` 用 `FlowRunControl`（停止/急停/暂停标志 + 变量表，双向同步 `ProjectStore.Data.Variables`）驱动 `FlowRunnerService.RunAllAsync`，支持暂停/停止/急停；进度/完成回调经 `Ui()` 封送回 UI。
  - `EStop/Stop/Pause/Resume` 同时给 `_flowCtrl` 发信号。
- 解释器覆盖：循环（循环开始/循环结束，次数取 SetValue，封顶 1e5 防死循环）、如果/否则如果/否则/结束 分支、等待/延时、注释/就/并且/或者（当前按 no-op 近似）、轴=MoveAxisAbs(+SetAxisSpeed/HomeAxis/StopAxis/MoveAxisRel)、IO=WriteOutput、气缸=CylinderMove/CylinderReset、点位=按 4 轴槽走点、modbus=CommSend、变量=按 Operation 算术写入（同步回 VariableRow）、系统=Log、相机=暂跳过。变量支持 `{name}` 引用。
- 此实现是对 FlowViewModel 执行核心的"最佳独立复刻"，硬件 API 由反射编译 DLL 核对；若与流程页单步行为有出入，需用户反馈后对齐（`FlowViewModel.cs` 现已确认是明文、可改，如需合并可后续处理，不必另走补丁）。
