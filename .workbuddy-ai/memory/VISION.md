# NoCodeMotion 视觉子系统笔记（MEMORY.md 的补充）
> 由 `MEMORY.md` 拆出。覆盖：视觉流程步骤、OCR 字符识别、图像采集「来源」、结果图缩放/平移、**标定（9 点 XY + 5 点旋转）**。
> 触发场景：改 `Views/VisualFlowPage.*` / `VisualFlowDetailViewModel` / `Models/VisualFlowStep.cs` / `Models/CameraCalibration.cs` / `Services/Vision/**`（含 `Calibration/`），或调相机取像。
> 逐日原始推导见同目录日志。

## 一、视觉流程步骤（四处同改）
- 加/改一个视觉步骤类型 = **四处同改**（少一处就静默失效）：① `Models/VisualFlowStep.cs` 参数字段；② `Views/VisualFlowDetailViewModel.cs` 的 `IsXxx` 标志（并加进 `RaiseTypeFlags()` 的 `nameof` 列表，否则切类型后参数卡不刷新）；③ `Views/VisualFlowPage.xaml` 左侧调色板 `VisualStepTypes` + 右侧参数卡（绑 `IsXxx`）；④ `Services/Vision/VisionEngine.cs` 的 `switch (StepType)` 分支 + 一个 `RunXxx`。
- 图像链：OpenCvSharp `Cv.Mat`（BGRA）；另有一块 `display` 注释画布用于画框/写字。`VisionStepResult.Text` 是给后续节点/显示用的结构化文本结果。
- AI 交换三处同改：`Services/AiProjectExchange.cs` 的 `NormalizeStepType`（别名归一）+ `ExportVisualSteps`/`FillVisualSteps`（含 `识别框` = `"x,y,w,h"`）+ prompt 类型清单。

## 二、OCR 字符识别（Windows 自带引擎）
- ★ **OCR = `Windows.Media.Ocr`（离线零三方依赖）**，步骤类型 `字符识别`。链路：`Cv.Mat` → BGRA `byte[]` → `SoftwareBitmap`（Bgra8 / Ignore）→ `OcrEngine.RecognizeAsync(sb).AsTask().GetAwaiter().GetResult()`。
- ★ **必须把 `TargetFramework` 抬到带 Windows SDK 的版本**（`net10.0-windows10.0.19041.0`）才拿得到 WinRT 投影，否则 `Windows.Media.Ocr` 编不过；同时 `NoWarn` 加 `CA1416`；`.smoke/ncm_smoke.csproj` 要同步改。
- ★ 语言映射：`自动`→`TryCreateFromUserProfileLanguages()`，`中文`→`zh-Hans-CN`、`繁体中文`→`zh-Hant-TW`、`英文`→`en-US`、`日文`→`ja-JP`、`韩文`→`ko-KR`。**中文识别要目标机装中文 OCR 语言包**，否则建引擎失败 → 必须优雅降级。
- ★ **Windows OCR 会在 CJK 之间插空格** → 比对前先 `NormalizeOcr` 去掉所有空白，否则「ABC 123」≠「ABC123」。匹配方式 包含/等于/正则（可选忽略大小写）→ 输出 OK/NG。
- ★ **ROI 复用现有拖拽手势**：`VisualFlowPage.xaml.cs` 的 `ApplyTemplateRoi` 里按 `StepType` 分流 —— 字符识别写 `OcrRoiX/Y/W/H` 后 `return`（**不**走模板裁剪/保存）。未框选时 `OcrRoiText` 回显「整图识别」。

## 三、图像采集「来源」（相机 / 文件 / 文件夹）
- ★ 默认 `SourceType = "相机"`（旧默认「文件」）。**旧行为是静默灾难**：模板建出的采集步没写 `SourceType` → 默认「文件」+ 无路径（或把文件夹写进 `SavePath`）→ `File.Exists` 失败 → 引擎悄悄生成合成测试图并报 `Ok=true`，看着「跑通了」其实没取图。
- ★ **只有「来源=相机」读不到相机时才回退测试图**；来源=文件/文件夹 路径无效一律 `AddFail` + `return null`。因此采集失败时 `cur == null` → 主循环 `display = cur?.Clone()` 必须容错，且每个算子分支都要先 `if (cur == null) { AddFail(report, s, "请先执行图像采集"); break; }`（`测量` 原来漏了，已补）。
- ★ **相机字段支持「名字」也支持「编号」**：`VisionEngine.ResolveCameraIndex(string?)` —— ① 工程相机名（`ProjectStore.Data.Cameras` 忽略大小写精确匹配；模板存的就是名字如「下视相机」）② 纯数字 0 基索引 ③ 名字带数字按 1 基（「相机2」→ 1）。旧实现只有 `int.TryParse` → 模板的「下视相机」直接判「相机编号无效」。
- ★ **真实相机取像只有一处实现**：`VisionEngine.TryGrabRealCamera(idx, out w, out h, out err)` = ① 海康 MVS（`MvsCameraService.TryGrabBgra`，与相机页共用已打开会话）② OpenCV `VideoCapture(idx)`。`RunAcquire`（视觉流程采集步）与 `CaptureFrame`（Lua 相机步骤 / 3D 抓拍）都走它。**旧 `RunAcquire` 只 `new Cv.VideoCapture(索引)` → 工业相机永远打不开**。
- 界面：`VisualFlowPage` 采集卡的「相机」是**可编辑下拉**（`ItemsSource={Binding CameraNames}` + `Text={Binding SelectedStep.CameraId}`，`IsEditable=True IsReadOnly=False` —— `CellComboStyle` 默认 `IsReadOnly=True` 必须显式覆盖）。`CameraNames` 由 VM 从 `ProjectStore.Data.Cameras` 现取，在 `RaiseSourceFlags()` 里一起 `OnPropertyChanged`。
- 模板：两个视觉模板的采集步都已 `SourceType = "相机"` + 相机名，并删掉被当文件路径用的 `SavePath = "Images/inspection/"`。
- 节点图 `NgVisionExecutor.Capture`：来源=文件/文件夹 路径无效时当帧为空，新增 `if (r is { Ok: false } && !rep.HasImage)` 明确上报（否则拿 0×0 帧继续喂算子，报更难懂的错）。

## 四、结果图缩放 / 平移（`Views/ZoomPanBehavior.cs`）
- 需求：视觉工具右侧结果图要**滚轮缩放 + 中键拖拽平移 + 双击复位**（左键留给框选 ROI），并有**屏上说明文字**。附加行为 `beh:ZoomPanBehavior.IsEnabled="True"` 挂在**容器 `ImageHost`**（不是 `Image`）：容器带变换，ROI 矩形 / 匹配框等子覆盖层才一起走；且 `e.GetPosition(container)` 不受容器自身 `RenderTransform` 影响 → 原有 ROI 像素换算不用改。
- ★ **变换顺序必须 Scale → Translate**，`RenderTransformOrigin=(0,0)`；屏幕映射 `screen = scale*p + offset`。
- ★ **光标锚定缩放**：`offset' = mouse*(1-s) + offset*s`（`s = newScale/oldScale`），否则缩放「跑偏」。已抽成纯函数 `ZoomOffset(...)` 便于断言。
- ★ **平移必须在父空间量鼠标位移**（`e.GetPosition(el.Parent)`）：`e.GetPosition(el)` 返回**变换前**的元素本地坐标 → 直接拿来算位移，图会以 `1/scale` 速度跟手。
- ★ 用 `AddHandler(UIElement.MouseDownEvent, DownHandler, handledEventsToo: true)` 注册，否则页面自己的 `ImageHost_MouseDown`（框选）标记 Handled 后行为收不到、双击复位失效。中键按下时 `Cursor = Cursors.SizeAll` + `CaptureMouse()`。
- ★ **外层 `Border` 必须 `ClipToBounds="True"`**（不能用 `UIElement.Clip`，它在渲染变换**之前**生效）→ 放大后不溢出。
- ★ 框选只认**左键单击**：`ImageHost_MouseDown` 开头 `if (e.ChangedButton != MouseButton.Left || e.ClickCount != 1) return;`；小框（<8×8）只提示不落盘（否则双击复位的第一次点击会留下 1px 框）。
- 冒烟段 O：纯函数断言（锚定/平移/钳制/复位）+ 真 `new VisualFlowPage()` 验 XAML 附加属性生效 + **合成 `MouseButtonEventArgs`** 真发一次中键按下，证明 `handledEventsToo` 接线生效。守卫 G25.1–G25.6 钉这几条。

## 五、运行结果标注颜色
- **结果要绿色显示在图像上**：`VisionEngine` 把标注烧进「注释画布」`display`（= 最终 `ResultImage`）。颜色约定：
  - ★ **缺陷检测**框 = 绿 `Rgb(30,170,80)`；**测量**线 + 十字标记 = 绿 `Rgb(30,170,80)`（原本是红 `Rgb(220,40,40)` / 蓝 `Rgb(40,120,240)`，已统一改绿）。
  - ★ **字符识别**：识别区域框 **通过=绿 / 不通过=红**（`pass ? Rgb(30,170,80) : Rgb(220,40,40)`）保留——通过绿、不通过红是约定，不要全改绿。
  - ★ **模板匹配**：不在 Mat 上烧框，改由 WPF 叠加层 `MatchOverlay` 画**旋转绿/红框 + 相似度/角度文字**（绿=通过、红=不通过），按 `Stretch=Uniform` 投影到屏幕坐标（`ProjectOverlayBoxes`）。
  - `Rgb(r,g,b)` 是 **RGB→OpenCV BGR** 的助手（`new Cv.Scalar(b,g,r)`），传 RGB 分量即可。
  - 守卫 **G25.7** 钉这条（缺陷=绿、测量无残留蓝、OCR 的 NG 仍红）。

## 六、字符识别结果叠加（每行文字绿框 + 框上方绿字标签）
- 需求：**「为什么字符识别没有显示图像的匹配情况」** → 要像模板匹配那样把识别结果画到图上。
- ★ **根因是「从来没实现」而不是坏了**：图上的「匹配情况」叠加层数据源是**单线**的 `VisionReport.Matches → VM.MatchResults → ProjectOverlayBoxes() → XAML MatchOverlay`，而 `report.Matches` **全工程只有模板匹配 `RunMatch` 一处写入**。`RunOcr` 原来只做两件事：① 在注释画布上画**一个** ROI 矩形（且**只有框选了 ROI 才画**）② 往 `report.Results` 加一条文字结果 → 图上自然什么都没有。
- ★ **OCR 的框数据源是 `OcrLine.Words[].BoundingRect`**（`Windows.Foundation.Rect`，**是「OCR 输入位图」的像素坐标**）。有 ROI 时输入是 ROI 裁剪图 → **必须加回 ROI 原点** `Left = bx + rx, Top = by + ry`，否则框整体偏左上。
- ★ **单独收集，不并入 `report.Matches`**：`Matches` 还被「共找到 N 个目标」文案和 `MatchResult`（best）消费，混入 OCR 框会污染匹配统计。新增 `VisionReport.TextBoxes`（`TextBoxItem`：Left/Top/W/H/Text/Pass）→ VM `OcrTextBoxes` → `TextOverlayBoxes`（`TextOverlayBox.Project` 按同一 scale/offset 投影）→ XAML 第二个 `Canvas x:Name="TextOverlay"`。
- ★ **两类叠加层共用一次投影**：`ProjectOverlayBoxes()` 里 `anyMatch || anyText` 都要投影，各自用**同一个** `scale/offset`，才能保证文字框、匹配框与源图三者相对位置严格一致。
- ★ **走向量叠加层而不是 OpenCV `PutText`**：`PutText` 只有 Hershey 字体、**中文会变 `???`**；WPF TextBlock 能正常渲染中文，且随 `ImageHost` 的缩放/平移一起走。
- 颜色：绿 `Lime`（不通过 → `#DC2626` 框 / `#FF6B6B` 字），与 `MatchOverlay` 保持一致的观感。文字标签贴在框上沿外侧 `Canvas.Top="-20"`。
- 守卫 G26.1–G26.6 钉这六环；冒烟段 Q 断言投影数学 + 真 `new VisualFlowPage()` 找 `TextOverlay` + 塞 2 个框后**渲染出 2 个 Rectangle 与 2 条文字** + 离屏截图 `.smoke/out/ocr_overlay.png`。

## 七、标定（9 点 XY 仿射 + 5 点旋转圆拟合）→ 模板匹配输出机台真实位置
- 需求：**「xy的9点+旋转的5点标定，确定旋转中心和xy方向和像素当量，方便模板匹配之后的计算真实位置」**。工具形式 = 与「图像采集」同级的视觉步骤类型 `标定`（调色板里本来就有外壳）。
- 新增文件（都在 `Services/Vision/Calibration/`）：`CalibSolver.cs`（纯 C# 数学）、`MarkerDetector.cs`（纯托管质心检测）、`CalibrationRunner.cs`（自动走点执行器）、`CalibrationStore.cs`（工程仓库）、`CalibVarExport.cs`（导出变量）；模型 `Models/CameraCalibration.cs`（工程级、一台相机一条）。
- ★ **数学约定**：`X = A1·u + A2·v + A3`、`Y = B1·u + B2·v + B3`；**像素当量 = √(A1²+B1²)** mm/px（+u 向），+v 向 = √(A2²+B2²)；**X 方向角 = atan2(B1,A1)**、Y 方向角 = atan2(B2,A2)（度）。**轴向对齐时 Y 方向角是 90° 不是 0°**（别按直觉写断言）。旋转中心 = 5 点 (θ,u,v) 的 **Kåsa 圆拟合**（图像 px），再经仿射换算成机台 mm。
- ★ **求解器/检测器刻意不依赖 OpenCV**：`dotnet exec` 下 `opencv_world480.dll` 不在搜索路径，标定是最需要「可离线断言」的一环 → 纯托管实现（亮度 `(r*299+g*587+b*114)/1000` + 4 连通域栈式标记 + 裂纹周长求圆度）。冒烟因此能端到端跑完整流程。
- ★ **执行器硬件/采集全走注入委托**（`ReadAxisPosition`/`MoveAxisAbs`/`IsAxisDone`/`SetAxisSpeed`/`GrabFrame`/`Guard`/`Log`）：既避开 `Services.Vision → Services` 耦合，也让冒烟用**合成圆斑 + 假轴**跑真流程（合成图里画圆 → 质心已知 → 断言像素当量/方向角/旋转中心都被还原）。
- ★ **标定是「真驱动轴」的危险动作，三条硬约束**：① `HardwareBridge.Current is StubHardwareBridge` → 直接 `AddFail` 中止（仿真桩下会假装成功）；② 轴名必须能在 `ProjectStore.Data.Axes` 里按名找到，**找不到就失败，绝不「拿轴 0 顶上」**（会撞机）；③ `GrabFrame` 走 `TryGrabRealCamera`，**取不到就返回空帧让执行器明确报错，不回退合成图**（合成图会悄悄产出垃圾标定参数）。
- ★ **停止必须冒泡**：`CalibrationRunner` 里所有 `catch (OperationCanceledException)` 一律 `throw`（3 处），`VisionEngine.Run` 也补了 `catch (OperationCanceledException) { throw; }`。吞掉的话按「停止」后轴会**继续走完 14 个点**。可中断等待 `Sleep` 每 ≤50ms 轮询 `Guard`。
- ★ **模板匹配输出真实位置**：`RunMatch` 新增 `acquireCameraName` 形参（由「图像采集」分支记下，避免匹配步自己再解析相机名），`CalibrationStore.FindUsable(cam)` 取标定 → 每个 `MatchBox` 与 best `MatchOutcome` 用 `CameraCalibration.ImageToMachineRotated(cx, cy, angle, out x, out y)`。**先在图像空间绕标定出的旋转中心旋转、再走仿射**（匹配出的模板自带角度，直接走仿射会丢旋转量；仿射线性部分是相似变换时与「先换算 mm 再绕机台旋转中心转」等价）。未标定时 `RealText = "未标定"`（`HasReal=false`，不假装有值）。
- ★ **落盘两处**：① `ProjectData.Calibrations` → `XlsxProjectStore` 的 `SheetNameOverrides["Calibrations"]="标定"` 工作表（`CameraCalibration` 字段**全是标量**，点集序列化成 `"u,v;u,v"` / `"θ,u,v;…"` 字符串，才能走反射导出）；② 「流程」表新增 **14 个标定列**（导出与回填一一对应）。
- ★ **顺手修了一个静默 bug**：OCR 字段（`OcrLanguage`/`OcrExpectedText`/`OcrMatchMode`/`OcrIgnoreCase`/`OcrRoi*`）此前只在 AI 交换 JSON 里往返，**xlsx 流程表里根本没有这些列** → 保存再打开会静默丢 OCR 配置。已补 8 列。
- ★ **变量导出名一律用 `_` 不用 `-`**（`标定_像素当量` / `标定_旋转中心U` …）：表达式求值器把 `-` 当减运算符，名字带减号一进表达式就被拆成减法。`CalibVarExport.EnsureCell` 走「同名复用 → 空槽复用 → 新开一行」，因为 `SimRuntime.WriteVarRow` **只改不建**。
- 界面：`VisualFlowPage.xaml` 标定参数卡（绑 `IsCalibration`）—— 标定方式（XY+旋转/仅XY/仅旋转）、相机（可编辑下拉，标定按相机名存）、X/Y/旋转轴（可编辑下拉 `CalibAxisNames`）、9 点间距 / 旋转步距 / 旋转点数 / 稳定延时 / 标记阈值 / 标记最小·最大面积 / 暗标记、`CalibCurrentText` 回显已有标定、`CalibResultText` 回显本次结果。
- 守卫 **G27.1–G27.29** 钉以上全部；冒烟**段 R**（45 条）：解析点集精确恢复（含 30° 旋转、Y 向 90°/60°）、点数不足/共线明确失败、圆拟合精确恢复、合成圆斑质心、假轴端到端 9 点与 5 点、一次跑完 9+5 → 工程记录 → 真实位置换算（0°/90°/180° 与不动点）、失败路径（没轴名/无图/缺回调）、停止冒泡、仓库往返 + 7 个变量 + 表达式里引用、真 `new VisualFlowPage()` 验卡片可见性（选中 vs 切走对照），离屏截图 `.smoke/out/calibration_card.png`。
- ★ 另一个易漂移点：`XlsxProjectStore` 的合并页图例字符串 `Add("流程（N列）", …)` 手写列数 → 加了守卫 **G27.26** 拿 `BuildFlowSheet` 的真实 `dt.Columns.Add` 数量与图例里的 N 对比（这次加 22 列就把图例从 51 改到 74）。
