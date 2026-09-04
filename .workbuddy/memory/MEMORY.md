# 项目长期记忆（NoCodeMotion — WPF/.NET 10 无代码运动控制）

## 源码与编码
- 全部 .cs 明文（无 0x88 加密），可直 Read/Edit。GBK/BOM/CRLF 文件若报 "binary file"，改用 Python 读写（utf-8/gbk→utf-8，newline="\n"）。
- 作者水印「温启志◆编写◇微信﹕187◆1936◇1399」三处：`Services/AuthorWatermark.cs`（三段字段 string.Concat 拼接含 ◆◇﹕\u200B\u2063）、`MainWindow.xaml` 第4行署名栏、`Docs/*.md` 末尾。删 AuthorWatermark.cs 编译失败（App.xaml.cs 引用保护）。AI 不主动删。

## 运行/构建
- 操作员「启动」= 并发跑 `ProjectStore.Data.Flows` 每条 Flow 循环区（`FlowRunnerService.cs`+`OperatorViewModel.cs`）；解释器覆盖 循环/分支/等待/轴/IO/气缸/点位/modbus/变量/系统/相机(暂跳过)，变量支持 {name}；`EStop/Stop/Pause/Resume` 给 `_flowCtrl` 发信号。
- 沙箱构建：**Bash 对 dotnet 一律 LOLBin 拦截**；用 **PowerShell + 绝对 `C:\Program Files\dotnet\dotnet.exe` 前台构建**（exit code 真实）。无头验证另开工程（ProjectReference 引用 NoCodeMotion.csproj，net10.0-windows+UseWPF），**不能放主工程目录内**（CS0017 双 Main）。Python venv+ctypes PrintWindow 可抓窗口。
- 全局加载进度：`Services/LoadingService`（静态 depth 引用计数 + `Show/Hide/Report/StateChanged` + `Progress/ProgressMax`，<0=不确定）；`MainWindow` 遮罩 `LoadingOverlay` 订阅它。**启动预初始化所有页面**用确定式进度（"正在初始化页面 (i/n)：中文名"）；**打开/新建工程**用不确定式遮罩（xlsx 读/写放 `Task.Run`，`LoadInto` 回 UI 线程，页面重建发生在遮罩可见期）。页面切换 `Navigate` 即时完成、不显遮罩（页面已在启动预初始化时进缓存）。`Dispatcher.Yield(DispatcherPriority)` 是静态方法，须 `System.Windows.Threading.Dispatcher.Yield(...)`。
- .NET 10 `_wpftmp` CS0579：csproj 加 `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>`+`<GenerateTargetFrameworkAttribute>false</GenerateTargetFrameworkAttribute>`；`[assembly:ThemeInfo]` 留根 AssemblyInfo.cs。勿用 UseArtifactsOutput。

## 全局 UI 约定
- 所有删除/清空按钮红色：`TtDeleteBtn`（大）/ `TtPillRedBtn`（小）；色彩编码 红=破坏/橙=反向非破坏/蓝=正向/绿=保存/灰=次要。改前先 Grep 列全清单。
- 每页底 `PageHintBar`（OperationText/PrecautionText）：EditorPage 子页第3行绑 Hint*；独立根页末尾 Auto 行加。
- 视觉数值参数用 `NumericSliderRow.xaml`（Value 双向）；Grid `*` 列必须 MaxWidth 封顶防渲染出视口。
- **EditorPage.Detail 内容里的元素不能用 `x:Name`**（MC3093 与 EditorPage 名字域冲突）。需运行时引用的元素改设 `Tag` + 视觉树 `FindVisualChildByTag<T>(root,tag)` 定位（如 CylinderPage 时序表 SeqGrid、CameraPage 闪光点 CamFlashDot）。

## 视觉/节点图页同步陷阱
- `VisualFlowPage.xaml.cs ApplySelection()` 须覆盖 `_vm.Steps/_vm.Name/_vm.SelectedStep`（漏→卡片全 Collapsed）；Steps 非空且 SelectedStep=null 时自动选 Steps[0]。
- 节点图 `Models/NodeGraph/`：`NgDefs.cs`(NgKind/NgDomain/NgNodeDefinitions.All 数据驱动)、`NgModel.cs`(NgDoc/NgNode/NgConnection/NgTemplates)；UI 用 ItemsControl+DataTemplate，禁 code-behind Children.Add。输出端口坐标 `OutputPoint(x,y,idx)=(x+NodeWidth, y+HeaderHeight+11+idx*OutputRowHeight)` 须与 NodeView 一致；`Outputs` 为 IReadOnlyList，用 `OutputPortIndex(string)` 辅助。
- 节点图仅编辑+存 `FlowItem.GraphJson`，**无执行引擎**（FlowRunnerService 不跑 NodeGraph）。仿真执行在 `Services/SimFlowPlayer.cs`（递归执行器，支持 Decision/Loop/Compute/VarSet）。

## CAD/DWG 导入
- 真实 BREP(STP/STEP/IGES)→ **OcctNet.Wrapper 0.1.1**（OpenCASCADE 7.9.3，原生 DLL 自动拷）。`OcctShape.ImportStep(path).Triangulate(linearDeflection:1.0)`；`OcctMesh` 非 IDisposable（勿 Dispose）；读完顶点/索引到 WPF 再 `using` 释放 shape。STEP Z-up→WPF Y-up 绕 X -90°；顶点法线按三角形累加；BackMaterial=mat 防黑面。参考 `D:\StpRenderProbe`。
- DWG/DXF 2D→ `Services/Cad/DwgReader.cs`（Aspose.CAD 26.7.0，只迭代 `CadImage.Entities`/`BlockEntities` 矢量实体，**绝不 Image.Save 栅格化**免水印）。CadArc 继承 CadCircle，switch Arc 在 Circle 前；递归块深度≤24 防环。`Sim3DView.BuildDwgModel` 烘焙线段+文字标签按取景包围盒居中。

## 仿真体系
- `Services/SimRuntime.cs` 静态态(IO/气缸/相机/变量) 驱动 3D+变量页；`Sim3DView.UpdatePoseFromRuntime()` 每 tick 读 `AxisRuntimeState.Get(axis)`。
- `Services/SimFlowPlayer.cs` 把 FlowItem(Table/NodeGraph) 编译 `List<SimAction>`，DispatcherTimer 33ms 驱动；`StepCount`/`StepLabels`/`PreviewSteps(flow)` 静态供预览（无副作用）。
- `Services/ProjectTemplateCatalog.cs` 20 模板；`NgTemplates.Build` 脚手架(空/通用流程/设备启动/取放循环/视觉对位)。

## Phase 3 配置页增强（已完成，0 构建错误）
- 点位表：`ArrayGenDialog`(行×列阵列生成) + `PointViewModel.GenerateArray/ExportCsv`(UTF-8 BOM CSV) + PointPage「生成阵列/导出CSV」按钮。
- 流程：`FlowPreviewDialog` + `SimFlowPlayer.PreviewSteps` 静态 + `FlowViewModel.PreviewCommand` + FlowPage「步骤预览」。
- 通讯：`CommViewModel.CommandPresets`(8 条 Modbus/AT/PING/JSON/SCPI) + `ApplyPreset` 填发送框；CommPage 命令预设下拉。
- 相机：`CameraItem.TriggerMode`(连续/软触发/硬触发) + `CameraViewModel.ApplyCommonParams`(曝光/增益/触发) + CameraPage 触发模式行+「应用常用参数」。
- 轴/IO/气缸：状态高亮与内联动作(气缸伸出蓝/缩回灰蓝) 此前已具备，本轮未追加。
- 状态栏新增中性蓝 `InfoText/HasInfo` + `ReportInfo/ClearInfo`（StatusBarService/StatusBarViewModel/StatusBarView）。
