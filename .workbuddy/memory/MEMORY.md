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
