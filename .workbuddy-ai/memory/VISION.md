# NoCodeMotion 视觉子系统笔记（MEMORY.md 的补充）
> 由 `MEMORY.md` 拆出。覆盖：视觉流程步骤、OCR 字符识别、图像采集「来源」、结果图缩放/平移。
> 触发场景：改 `Views/VisualFlowPage.*` / `VisualFlowDetailViewModel` / `Models/VisualFlowStep.cs` / `Services/Vision/**`，或调相机取像。
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
