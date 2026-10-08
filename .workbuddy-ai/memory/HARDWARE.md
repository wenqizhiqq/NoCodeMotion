# NoCodeMotion 硬件层笔记
> 由 `MEMORY.md` 拆出（它是索引，这里是细节）。逐日推导见同目录日志。

## 一、硬件层通用
- 雷赛：`LtdmcNative.cs` 唯一 P/Invoke。总线卡运动 `dmc_*`（profile_unit/pmove_unit/get_position_unit/check_done/stop），使能 `nmc_set_axis_enable`（CiA402）。EtherCAT 端口固定 2；CiA402 只有状态 4 能动；`dmc_get_home_result` `state==1` 才算成功。
- 卡族入口 `CardFamilyCatalog.cs` + `WenQiZhiCardBridge.cs`；`AutoDetectFromProject()` 只有雷赛走 `LeadshineHardwareBridge`。`Families/` 目录名≠命名空间 → 断言 `Create().GetType().Namespace`。`Aliases` 必须含型号名 + 固件卡型码（`$"0x{码:X}"`）。轴必须一轴一个 `IAxis`；`GetCardAxisCurrentState()` 0=停止/1=运行中（反直觉）；`ListCardParam` 必须预置否则 `IndexOutOfRange`；模拟卡 `IsVitualCard=true` 跳过真实开卡。

## 二、研控 MCN420（当前真机）
- 使能极性：端口电平、低有效。`YK_set_sevon_config(card,axis,sevon_en)` 第三参=端口电平（写 0=使能）。四处同步：① `NmcSetCardAxisEnable` 写0使能/1不；② `GetAxisCurrentState()` bit8 在 `get==0` 置位；③ `NormalizeSevon(int)` 反转（**别改回恒等**）；④ `CardAxisWriteSevonPin` 契约不变。**规矩：参数语义看原生声明，极性看真机，不用错误码表反推。**
- 轴指令 unit 版，依赖卡内脉冲当量 `YK_set_command_ratio` = pulse/unit；桥接层**绝不能乘当量**（模拟卡由 `BuildSimParam` 自乘当量变 pps，1000pps 下限）；`EquivOf` 当量≤0 兜底 1。错误码表 `Native/Mcn420ErrorCode.xml`（98 条，csproj 必须 `None+CopyToOutputDirectory`）——**字面别当真**。
- ★ **8194 真因 = `Channel=2`（非法通道）**：请求结构体 `Channel` 注释 `// 0 or 1`，SDK 硬编码写 2（含 Jog 的 `DmcSetCardAxisProfileUnit`）→ 卡回 8194。已全改 `Channel=0`。**查不出先逐字段对原生 struct 值域，再 grep 那个字面量。**
- ★ **轴慢真因 = 加减速语义错**：`Accel/Decel` 是**加速时间(秒)**，不是加速度值。`BuildMotionParam`/`BuildHomeParam` 直传（`>0?值:0.2`）；`AxisItem._accel/_decel` 默认 0.2（旧默认 50 经 `MigrateAxisDefaults()` 迁移）。接口权威 `IAxis.cs`：MaxVel unit/s、TaccVel 单位 s。★ 模板里 `Ax(...)` 仍传 50（旧语义）→ 新建工程加速度是 50 秒；新加的对象一律传 0.2。

## 三、多开 / 退出（Request C，详见 2026-10-07 日志）
- `Services/SingleInstance.cs` 命名 Mutex；`EnsureSingleInstance()`→false 则 `App.OnStartup` `Shutdown()`；冲突弹 `InstanceConflictWindow`。`AppShutdown.StopAndRelease()` → `FlowLoopManager.StopAll()` + 遍历 `Controllers` 调 `CardFamilies?.Disconnect(c)`。★ `MainWindow` Closing 必须 `e.Cancel=true` 再异步释放，否则进程退不干净。

## 四、相机（海康 MVS）
- 入口 `Services/Camera/MvsCameraService.cs` + 托管封装 `Native/MvCameraControl.Net.dll`（csproj `<Reference>`+`HintPath`+`Private`）。**本机只有托管封装、没装 MVS Runtime** → `DllNotFoundException MvCameraControl.dll 0x8007007E`，**必须优雅降级**。**会话按设备 Key 复用**（`SN:`→`IP:`→`IDX:`）：一台 GigE 只有一个独占句柄，相机页与流程视觉共用同一会话。
- 封装**纯 IL**（.NET 10 可加载），`MyCamera` 无参公有构造、`MV_CC_EnumDevices_NET` 是**静态**。取图只有 `MV_CC_GetImageForBGR_NET` → 按 `nFrameLen` 反推布局（3=BGR、4=BGRA、1=Mono）；`nCurValue` 是 `UInt32`。曝光 **界面毫秒 → SDK 微秒（×1000）**；触发三态「连续/软/硬」→ `TriggerMode` Off / On+`TriggerSource` Software / On+Line0。
- `MV_CC_DEVICE_INFO_LIST` 封送有坑 → **手写镜像原生布局**（256 内联 `IntPtr`=2056 B）；`SpecialInfo` 是私有定长缓冲 → 必须 `GCHandle.Alloc(Pinned)` 再 `PtrToStructure`。★ `CaptureFrame` 的**最后一层回退必须在 try 内**：`SyntheticCapture` 依赖 OpenCvSharp，缺原生库时异常冲出会打断整条流程 → 已加纯托管兜底 `ManagedPlaceholder`，**永不抛**。
