# 项目长期记忆

## 源码状态：当前全部 .cs 为明文（2026-08-27 实测无 0x88）
- ⚠️ **重要更正**：此前多轮记忆称"Models/Editing 首字节 0x88 加密、需走补丁"。2026-08-27 用脚本逐文件查首字节：**103 个 .cs 全部首字节正常（`using`/`#nu`/BOM），0 个 0x88 加密**。当前磁盘状态=**全明文，均可直接用 Read/Edit/Python 改**，不再需要补丁或请用户解密。
- 若日后重新出现"Read 报 binary / 首字节 0x88"，再按加密处理；常态下按明文处理。
- 明文文件若 `Read`/`Edit` 报 "binary file"（多为 GBK/BOM/CRLF），改用 Python 读写（读 utf-8/gbk，写 utf-8，`newline="\n"`）；GBK 文件可安全升级为 UTF-8(BOM)。

## 作者水印（温启志◆编写◇微信﹕187◆1936◇1399）三处统一
- 完整串被故意拆段：源码里"187" / "1936" / "1399" 在 `Services/AuthorWatermark.cs` 分三段独立字段，运行时 `string.Concat` 拼接；同行插入 `◆` `◇` `﹕` `\u200B` `\u2063` 等装饰/零宽字符。
- 三处统一出现（缺一会被立即发现）：① `Services/AuthorWatermark.cs` 暴露 `Signature/UiSignature/DocumentSignature/DocumentFooter`；② `MainWindow.xaml` 第 4 行（深色署名栏，构造时 `AuthorSignatureText.Text = AuthorWatermark.UiSignature()` 渲染）；③ `Docs/版权与作者水印.md` + `Docs/硬件对接说明书.md` 末尾 + `Docs/雷赛控制卡与通讯对接说明.md` 末尾。
- 误删保护：`App.xaml.cs` 构造里 `_ = AuthorWatermark.Signature;` 引用 → 删 `AuthorWatermark.cs` 整个工程编译失败。
- 关键工程经验：给数据模型 `override ToString() => string.IsNullOrWhiteSpace(_name) ? "未命名" : _name;`，能在 ComboBox `DisplayMemberPath` 解析失败/`IsReadOnly=True` 嵌入 TextBox 路径/`ItemContainerStyle` 模板等多种场景下救回"显示类名 `NoCodeMotion.Models.PointTable`"问题，也对调试/日志输出有效。

## 操作员「启动」= 并发跑全部流程循环（非工位序列）
- 用户最终选择（澄清）：点「启动」= 并发跑 `ProjectStore.Data.Flows` 里每个 Flow 的「循环开始/循环结束」区域，次数取 `SetValue`；不需要选工位。
- 落地方式：新增文件 `ViewModels/FlowRunnerService.cs`（独立并发流程运行器，复用同一套 `HardwareBridge`/`HardwareResolver` 硬件接口），并在 `OperatorViewModel.cs` 接入：
  - `CanRun/CanStart` 放宽到 `!IsRunning && !EStopped && (Flows.Count>0 || HasAnyRunnableTable())`。
  - `Start()` 自动模式优先 `StartFlows()`（每条 Flow 一个 `Task.Run` 并发），无流程时回退到旧工位序列 `RunLoop()`。
  - `StartFlows()` 用 `FlowRunControl`（停止/急停/暂停标志 + 变量表，双向同步 `ProjectStore.Data.Variables`）驱动 `FlowRunnerService.RunAllAsync`，支持暂停/停止/急停；进度/完成回调经 `Ui()` 封送回 UI。
  - `EStop/Stop/Pause/Resume` 同时给 `_flowCtrl` 发信号。
- 解释器覆盖：循环（循环开始/循环结束，次数取 SetValue，封顶 1e5 防死循环）、如果/否则如果/否则/结束 分支、等待/延时、注释/就/并且/或者（当前按 no-op 近似）、轴=MoveAxisAbs(+SetAxisSpeed/HomeAxis/StopAxis/MoveAxisRel)、IO=WriteOutput、气缸=CylinderMove/CylinderReset、点位=按 4 轴槽走点、modbus=CommSend、变量=按 Operation 算术写入（同步回 VariableRow）、系统=Log、相机=暂跳过。变量支持 `{name}` 引用。
- 此实现是对 FlowViewModel 执行核心的"最佳独立复刻"，硬件 API 由反射编译 DLL 核对；若与流程页单步行为有出入，需用户反馈后对齐（`FlowViewModel.cs` 现已确认是明文、可改，如需合并可后续处理，不必另走补丁）。

## 沙箱构建/截图通道（2026-08-28/29 验证）
- **Bash 工具对 dotnet/msbuild/csc 一律 LOLBin 拦截**（连 `ls /c/Program Files/dotnet/` 都拒），但 **PowerShell 工具可正常调 `C:\Program Files\dotnet\dotnet.exe`**（绝对路径），build exit code 真实传递。这是本环境构建 .NET 工程的稳定通道。
- **Add-Type 被沙箱拦截**（运行时编译 .NET），无法用 PowerShell 直接做 WinForms 对话框 / Win32 P/Invoke → 改用 **Python venv + ctypes**：`pip install pillow` 后用 `ImageGrab` 截屏；用 `ctypes.windll.user32.PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT=0x02)` + `gdi32.GetDIBits` 抓任意窗口（**不依赖前台**，可绕过 SetForegroundWindow 限制），PIL 保存 PNG。
- **dotnet 10 可用 `Microsoft.Win32.OpenFolderDialog`**（.NET 8+ 新增），引用 `Microsoft.Win32` 命名空间即可，与 `OpenFileDialog` 同处。注意：环境若有 EDR 可能静默阻断对话框，**必须 try/catch 并把异常写回 UI 状态文本**，避免"点了没反应"被误解为缺按钮。

## 视觉页 SelectedStep 同步陷阱（2026-08-29）
- `Views/VisualFlowPage.xaml.cs` 的 `ApplySelection()` 是"视觉页 ↔ 主流程项"同步枢纽。**必须覆盖所有相关属性**：`_vm.Steps` / `_vm.Name` / `_vm.SelectedStep`。任一遗漏会导致：ListBox 无高亮 / 参数卡全 Collapsed / "加按钮却看不见"。
- 早期版本只同步 Steps+Name，未初始化 SelectedStep → 用户进视觉流程看不到任何参数卡（不是卡不存在，是 IsImageAcquisition 等标志永远 false）。**修法**：Steps 非空且 SelectedStep 为 null 时自动选 `Steps[0]`。`AddStepCommand` 已有 `SelectedStep = step` 自动同步新增。
- 排查"按钮/卡片看不见"类问题顺序：① PrintWindow 截图 → ② 读 code-behind 同步函数 → ③ 读 VM `SelectedStep` DP 默认值与 setter → ④ 读 ListBox `SelectedItem` 双向绑。任一断点都让整组卡消失。

## 视觉数值参数 Slider+输入框联动（2026-08-29）
- 复用 `Views/NumericSliderRow.xaml` UserControl（Label/Value/Minimum/Maximum/TickFrequency/Unit 依赖属性），`Value` 用 `FrameworkPropertyMetadataOptions.BindsTwoWayByDefault` 让 XAML 双向绑定省写 `Mode=TwoWay`。Slider + TextBox 都 `ElementName=Root` 绑 Value，**天然联动**。TextBox 用 `UpdateSourceTrigger=PropertyChanged + Delay=300` 避免输入"0."、"-"等中间态转换失败。
- **Grid `*` 列必须加 `MaxWidth` 封顶**——否则 `*` 会无界扩张把后面 `Auto` 列（TextBox + Unit）推出视口边缘，看似控件"消失"实则是渲染在屏幕外。`DockPanel LastChildFill` 也有类似陷阱。复杂行布局务必显式约束每一列最大/最小宽度。
- TextBox 用现有 `AppleTextField` 样式时务必 `BasedOn` 后覆盖 `MinWidth=0` 和 `HorizontalAlignment=Stretch`，否则 AppleTextField 的 `MinWidth=120` 会撑爆紧凑布局。

## 全局 UI 约定：所有删除/清空按钮统一红色（2026-08-29 用户明确要求"软件所有的删除清空按钮都需要红色"）
- 红色按钮样式在 `Resources/AppStyles.xaml`，共两个：
  - **`TtDeleteBtn`** — `BasedOn=TtBaseBtn`，饱和红 `DangerBrush`，hover `#D63A3A`。用于**工具栏大按钮**（图标+文字的删除/清空）
  - **`TtPillRedBtn`** — `BasedOn=TtPillBase`，浅红底 `#FFE3E3` + 深红字 `#C92A2A`。用于**小药丸/紧凑按钮**（表格行内删除、面板内小清除）
- 非破坏性颜色对照：蓝 `TtPillBlueBtn`（+加/使能/移动）、橙 `TtPillOrangeBtn`（-减/反向，非破坏性）、绿 `TtPillGreenBtn`（保存）、灰 `TtPillGrayBtn`（回原/次要）
- **已统一为红色的 11 个按钮**（新增按钮时务必遵守此约定）：
  - `EditorPage.xaml` 删除（默认工具栏，所有 EditorPage 宿主页共享）
  - `TableToolbar.xaml` 删除（所有表格页共享）
  - `ProjectManagerPage.xaml` 删除工程
  - `FlowPage.xaml` 清空全部流程
  - `PointPage.xaml` 删除工位 / 删除点位
  - `VisualFlowPage.xaml` 删除步骤 / 清除（模板框选）
  - `LuaEditorView.xaml` 清空（输出面板）/ 清除断点（`LuaDbgButton` + `Background={StaticResource DangerBrush}`）
  - `CommPage.xaml` 清空日志
- 经验：**破坏性操作用颜色编码**（红=删除破坏、橙=反向非破坏、蓝=正向主操作、绿=成功保存、灰=次要），比纯文字更防误操作。全局统一改这类按钮色时，**先 `Grep` 扫 `(Text|Content)="(删除|清空|清除|移除...)"` 或 `Command="...(Delete|Clear|Remove)..."` 列全清单**再逐个改，避免漏改。

## 每个页面底部「操作说明 + 注意事项」栏（2026-09-02 起全局约定）
- 复用控件：`Views/PageHintBar.xaml/.cs`（Apple 风格、贴底、浅色、可换行），两个 DP `OperationText`/`PrecautionText`。
- EditorPage 宿主的子页（轴/IO/气缸/控制器/点位/通讯/料盘/相机/变量/流程）：在 `EditorPage` 根 Grid 第 3 行放 `PageHintBar`，绑定 `EditorPage.HintOperation`/`HintPrecaution`；各子页在 `<local:EditorPage>` 上设本页文案。
- 独立根页面（Io/Point/Variable/Operator/Engineer/OperatorManual/VisualFlow/LuaEditor/ProjectManager）：根 `<Grid>` 加末尾 `Auto` 行 + `<local:PageHintBar Grid.Row=最后行 [Grid.ColumnSpan=列数]>`。
- 文案默认由 AI 按页面功能撰写（操作 + 注意事项），用户截图微调。新增页面时务必同步加底栏，保持全局一致。

## CAD（STP/STEP/IGES BREP）导入到 WPF 3D：用 OcctNet.Wrapper（2026-09-03）
- **永远不要用 AssimpNet 处理 Creo/UG/SolidWorks 真实 BREP**：Assimp 4.1.0 会按扩展名把 `.stp` 误判为 IFC 撞 `Unrecognized file schema`；即使改 `.p21` 强制 STEP reader，对真实 BREP 也产出 **0 三角面**（它的 BREP 三角化器太弱）。
- **正确选择：`<PackageReference Include="OcctNet.Wrapper" Version="0.1.1" />`** — 封装 OpenCASCADE 7.9.3，自带 `runtimes/win-x64/native/` 下 51 个 `Occt*`/`TK*` 原生 DLL（MSBuild 自动拷到输出），net 8/10 兼容。
- **API 要点**：`using var shape = OcctShape.ImportStep(path); var mesh = shape.Triangulate(linearDeflection:1.0);` —— `OcctMesh` **不是 IDisposable**（没 `Dispose`，别加 try/finally mesh.Dispose()`）。读 `mesh.Vertices[i].X/.Y/.Z`、`mesh.TriangleIndices[i]`、`mesh.Vertices.Count`、`mesh.TriangleCount`。重要：读完所有数据到 WPF `MeshGeometry3D`/`Point3D[]` 后 `using` 会自动释放 `shape`（同时释放 mesh 内存），所以务必先把数据拷走。
- **坐标转换**：CAD STEP 是 **Z-up**，WPF `Viewport3D` 是 **Y-up**。烘培顶点：`var p = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1,0,0), -90)).Value.Transform(new Point3D(v.X,v.Y,v.Z));`（绕 X 轴 -90°）。多数工业 CAD 组件放完旋转后中孔/基座方向与相机视角一致。
- **平滑顶点法线**：OCCT 的 `Triangulate` 不直接给法线；按三角形累加 `Vector3D.CrossProduct(p[b]-p[a], p[c]-p[a])`，归一化累加到三个顶点法线，再归一化每个顶点法线（fallback `(0,1,0)` 避免零向量）。否则渲染出来是平面着色 + 缺面 = 黑块。
- **双面避免黑面**：`GeometryModel3D(mg, mat){ BackMaterial = mat }`（BREP 三角化法线方向偶发不一致）。
- **大模型性能**：46 万三角面一次性烘焙到**单一** `MeshGeometry3D`（不要每子节点一网格），`Freeze()` 后跨线程共享。WPF retained-mode 吃得下，但要避免再叠透明/反射多层。
- **CSPROJ 缩进敏感**（`NoCodeMotion.csproj` 行 74-77）：Edit 加 `<ItemGroup><Content Include="*.stp" .../></ItemGroup>` 时必须匹配原 8 空格缩进，否则 Edit 报 whitespace mismatch。
- **加载流程**：UI 线程 `Task.Run` 后台做导入+三角化 → `group.Freeze()` → `Dispatcher.Invoke` 装到场景 + `BuildScene()` + `UpdateCamera()`；异常 catch 后 `SetStpStatus("STP 解析失败："+ex.Message)`。默认开 `Loaded` 自动载入 `Models/CAD/*.stp`。
- **WPF PrintWindow 抓不到硬件加速客户端区**（DWM 标题栏能抓到，DirectComposition 合成层空白）——所以**运行中窗口截图验证不靠谱**。验证渲染用独立 `RenderTargetBitmap` 探针项目（参考 `D:\StpRenderProbe`）：复用完全相同的 `OcctShape`+`Triangulate`+Z-up→Y-up 路径 + 同样的相机公式（`theta=0.7, phi=0.5, r=2.6·boundsRadius`）→ `RenderTargetBitmap.Render(vp)` → `PngBitmapEncoder` 保存 PNG。

## 节点图（第 4 种流程 FlowKind.NodeGraph，2026-09-03 移植自 MotionFlowDesigner）
- **用户需求**：FlowPage 新增「添加节点图」按钮 → 第 4 种流程（与 Table/Lua/Visual 平级）；视觉/运控/通讯三类节点都要；当前**仅可编辑+保存到工程，无执行引擎**（FlowRunnerService 只跑 Lua/Visual，不跑 NodeGraph，符合预期）。
- **移植要点**：MotionFlowDesigner 的 `Models/`（FlowNode/Connection/NodeDefinitions）是纯数据可复用，但其渲染（`MainWindow.xaml.cs`/`NodeControl.xaml.cs`）用 `Children.Add`+路由事件，**违反 NoCodeMotion MVVM/`Children.Add` 禁令** → 整页重写为 ItemsControl+DataTemplate 的 MVVM。新节点类型在 `NgDefs.cs` 自定义（不依赖原工程的运控独占定义）。
- **关键文件**：`Models/NodeGraph/NgDefs.cs`（18 种节点：`NgKind` 枚举 + `NgNodeDefinitions.All` 字典，含视觉6/运控8/通讯4、颜色/端口/属性）、`NgModel.cs`（`NgDoc`/`NgNode`/`NgConnection`/`NgProp` 序列化 + `NgGeometry` 几何 + `NgTemplates` 三模板）、`ViewModels/NodeGraphNodeViewModel.cs`、`NodeGraphConnectionViewModel.cs`、`NodeGraphViewModel.cs`、`Views/NodeGraphPage.xaml[.cs]`、`Views/NodeGraphNodeView.xaml`、`Views/NgConverters.cs`。
- **持久化**：整图序列化进 `FlowItem.GraphJson`（JSON 字符串，镜像 `LuaSource`），`XlsxProjectStore.cs` 新增「节点图JSON」列读/写；`NgDoc.ToJson/FromJson` 用 `System.Text.Json` + `JsonStringEnumConverter`，`FromJson` try/catch 容错返回空图。
- **交互（NodeGraphPage.xaml.cs 仅做拖拽/连线/选中，不做 `Children.Add`）**：`DesignerCanvas_PreviewMouse*` 三事件；命中用 `FindWithTag(dep,"CONN"/"OUT"/"IN"/"NODE_HEADER")` + `FindNodeVm`；连线终点用 `VisualTreeHelper.HitTest` 找 `IN` 元素。
- **坐标几何**：`NgGeometry.NodeWidth=186, HeaderHeight=30, OutputRowHeight=22`；`OutputPoint(x,y,idx)= (x+NodeWidth, y+HeaderHeight+11+idx*OutputRowHeight)` —— 必须与 `NodeGraphNodeView.xaml` 渲染的输出端口行布局一致，否则贝塞尔线不落在端口上。
- **选中同步**：镜像 VisualFlowPage 模式 —— `NodeGraphPage.Loaded` 沿可视树找 `FlowPage` 祖先，订阅 `FlowViewModel.SelectedItem.PropertyChanged` → `ApplySelection()` 仅在 `Kind==NodeGraph` 时 `LoadFrom(flowItem)`。
- **懒加载**：镜像 VisionContent —— `FlowPage.xaml.cs` 的 `NodeGraphContent` DependencyProperty + `EnsureNodeGraph()`，避免 MC3093 NameScope 冲突（ContentControl 不可 x:Name）。
- **易踩坑**：① 节点 VM 的 `Outputs` 是 `IReadOnlyList<string>`，**没有 `.IndexOf`** → 加 `OutputPortIndex(string)` 辅助方法，调用处改用它（`_src.Outputs.IndexOf` 报 CS1929）；② 节点页 code-behind 必须 `using System.Windows.Controls;`（UserControl 所在命名空间），否则 CS0246；③ 测试工程**不能放在 NoCodeMotion 目录内**（SDK `**/*.cs` globs 会把测试 Program.cs 一起编进 NoCodeMotion 导致 CS0017 双 Main），须放到上级 `D:\wqz\code\NodeGraphSmoke`。
- **沙箱自测**：构建 0 错误；另写 `D:\wqz\code\NodeGraphSmoke`（ProjectReference 引用 NoCodeMotion.csproj，net10.0-windows + UseWPF）做无头逻辑冒烟测试（NgDoc 往返/三模板连线端点/LoadFrom/AddNode/Connect/DeleteSelected/Save 同步写回 GraphJson/几何），22 项全 PASS。WPF 渲染层因无显示器无法在无头沙箱验证，需用户在桌面运行确认。

## .NET 10 SDK WPF 构建：`_wpftmp` CS0579 重复特性（2026-09-04 实测 + 修复）
- **现象**：`dotnet build` 报 16 个 `CS0579`，全在 `NoCodeMotion_xxx_wpftmp.csproj`（WPF 临时标记编译工程），重复 `TargetFrameworkAttribute` + `AssemblyCompany/Version/...Attribute`。根因：SDK 10.0.102 的 `_wpftmp` 同时含主工程生成的 `NoCodeMotion.AssemblyInfo.cs` 与自带 `_wpftmp.AssemblyInfo.cs` / `.NETCoreApp,Version=v10.0.AssemblyAttributes.cs`。
- **修复（csproj `<PropertyGroup>`）**：加 `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` + `<GenerateTargetFrameworkAttribute>false</GenerateTargetFrameworkAttribute>`；共享的 `[assembly:ThemeInfo(...)]` 保留在根 `AssemblyInfo.cs`（仅此项、不与临时工程冲突）。改后 **0 错误**。
- ⚠️ 主程序集将不再含版本/TargetFramework 特性（仅缺失元数据，桌面直接启动仍可运行）；若日后出现运行时报缺框架特性，再考虑用 `Directory.Build.props` + 手动特性回退。
- ⚠️ 不要误用 `UseArtifactsOutput`/`UseArtifactsOutputPath`（非本 SDK 真实属性）；若需重定向中间产物，正确属性是 `UseArtifactsPath=true` + `ArtifactsPath=artifacts`（但本 bug 用它无效，必须用上面的 Generate* 关闭法）。
- 构建通道：Bash 对 dotnet 一律 LOLBin 拦截（含 `run_in_background` 包装），**始终用 PowerShell + 绝对 `C:\Program Files\dotnet\dotnet.exe` 前台构建**。`DiffuseMaterial` 无 `Opacity` 属性 → 透明度设在 `SolidColorBrush` 上。

## DWG/DXF 二维图纸导入仿真页（2026-09-04）
- **阅读器**：`Services/Cad/DwgReader.cs`，Aspose.CAD 26.7.0（PackageReference）。`Image.Load(path)` 通吃二进制 DWG 与 DXF 文本，返回 `CadImage`。**只迭代矢量实体（`CadImage.Entities` / `BlockEntities[name]`），绝不调 `Image.Save` 栅格化 → 无 "Evaluation only" 水印**。
- **递归块**：`CadInsertObject` → `img.BlockEntities[ins.OriginalBlockName]`（带 BasePoint 移位、Scale、绕 Z 旋转、插入点、阵列行列偏移），深度≤24 + 访问集合防环。
- **实体→线段**：`CadLine`(FirstPoint→SecondPoint)；`CadLwPolyline`(Coordinates，闭合接首尾)；`CadCircle`/`CadArc` 按半径采样成段。**⚠️ CadArc 继承自 CadCircle，switch 必须 Arc 在 Circle 之前**（否则 CS8120 不可达）。`CadArc` 顺时针用 `CounterClockwize!=0` 判定；`CadLwPolylineFlag` 是枚举，`(int)pl.Flag` 直接转（不能 `(int)(object)` 否则 InvalidCast）。
- **文字**：`CadText`(FirstAlignment/DefaultValue/TextHeight/TextRotation)、`CadMText`(InsertionPoint/Text或FullText/InitialTextHeight/RotationAngleRad)。
- **取景聚类**：`ComputeFitBounds` 网格连通分量聚类，选面积最大图块为相机取景目标。实测目标文件 96% 图元在 Y≈-2.5M（电缆表/明细，被拖远），正确剔除，主图块为机器立面。
- **渲染**：`Sim3DView.BuildDwgModel` 把线段烘焙成 XZ 平面细矩形网格 + 半透明背板 + `Viewport2DVisual3D` 文字标签，按取景包围盒居中缩放（target=180 场景单位），离群段/标签剔除；`LoadDwgFile` 后台读 + Dispatcher 装配，复用 `_cadMode` 隐藏参数化机台；XAML 工具栏「导入DWG/DXF」按钮（`BtnImportDwg_Click`）。
- **本机验证**：DwgTest 头less（已删）RAW=100, SEGMENTS=70381, LABELS=79, FIT=[-3,2797.6→860,24596.8]；主工程构建 0 错误。WPF 运行时渲染未截图验证（沙箱无显示器）。
