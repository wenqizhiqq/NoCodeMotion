# VisionPlus 视觉算子逆向清单与移植规划

> 生成日期：2026-11-12 ｜ 逆向对象：`C:\Program Files\VisionPlus\bin`
> 目标工程：`d:\wqz\code\NoCodeMotion`（net10.0-windows，WPF）
> **当前状态（2026-11-12 同日更新：已全量落地）** —— 缺口算子 **32 个全部实现**，每个都有可设置参数、都是真实算法，无占位。
> 实现路线：OpenCV 复刻 + OpenCV DNN 跑 ONNX + ZXing.Net 纯托管解码；**不调用 VisionPlus 二进制、不复制其源码**。
> 验证：`dotnet build` 0 警告 0 错误 ｜ `guard_sources.py` 553 PASS / 0 FAIL ｜ 冒烟 111 个算子全跑、32 个新增 0 异常 0 未实现。
> 下面第 1~5 节是原始逆向调研记录，第 6 节是落地清单，第 7 节是约束，第 8 节是交付与已知限制。

---

## 1. 结论摘要

| 项 | 结论 |
|---|---|
| VisionPlus 是什么 | HALCON 兼容内核 + Cognex 风格算子名的商业视觉平台，版本 `4.2.0.8001`，`MvpAlgorithmsApi` 版本 `4.1.3.0` |
| 算子规模 | 传统 2D 约 40 类工具、3D 5 类、深度学习 6 类、EL 传统学习 1 类；另有 50+ 个单算子原生模块 |
| NoCodeMotion 现状 | `VisionOperators.cs` 已登记 **79 个真能跑的算子**（OpenCV / Halcon 同款 / WenVision 移植三族），执行器 `VisionOpRunner.cs` 单个静态 switch |
| 覆盖度 | 找边拟合、分割形态学、几何计算、基础标定、对位、读码、图像颜色 **基本齐**；**位置跟随、参数化卡尺、颜色模板匹配、轮廓金字塔匹配、缺陷比对、手眼/N 点标定、3D、DL 是缺口** |
| 最大障碍 | 有 Wibu 加密狗 + `VP.Services.License.dll`；C API 结构体签名未公开 → 直接 P/Invoke 不可取，且带授权风险 |
| 接入成本 | 低。目录 + 执行器两个明文文件即可，参数走 `ParamJson`，添加菜单 / 参数行 / 节点图工具箱 / xlsx 算子手册全自动带上 |

---

## 2. 逆向取证方式（可复现）

沙箱安全策略拦截了 `Add-Type` 与 `Reflection.Assembly.LoadFrom`，因此改用纯字节取证：

1. **托管 / 原生判定**：`[Reflection.AssemblyName]::GetAssemblyName()` —— `VP.*.dll` 与 `MvpAlgorithmsApi.dll` 均为 .NET Framework 程序集；`*Operator.dll` 与 `Vision2D_Interface_C*.dll` 为原生 C++。
2. **算子类清单**：读 `VP.Algorithm.dll` 的 CLI `#Strings` 堆，提取 `Atom*` 公开类（272 条，含算子类 / `Setting` / `ParameterPageViewModel` / `ResultPageViewModel`）。
3. **算法门面清单**：读 `MvpAlgorithmsApi.dll` 的字符串堆，提取 `MvpXxxApi` / `XxxTool` / `MVP_*` 枚举与参数字段名。
4. **C API 导出**：读 5 个 `*_Interface_C*.dll` 的 PE 导出表明文，提取 `Dcck2D_Function_*` 全量函数名。
5. **佐证**：`procedures/general/*.hdpl` / `*.hdvp` 是 HALCON 风格过程模板（`dev_display_ncc_matching`、`dev_display_shape_matching` 等），印证内核算子命名体系。

### 模块分层

| 层 | 文件 | 作用 |
|---|---|---|
| UI / 流程 | `VP.App.exe`、`VP.Presentation.*.dll`、`VP.Models.Base.dll`、`VP.Data.Model.dll` | 流程编辑、参数页、结果页 |
| 算子定义 | `VP.Algorithm.dll`（`Atom*`）、`VP.Algorithm.Wrapper.dll`、`VP.AlgorithmCntr.dll` | 每个算子的 Setting / 参数模型 / 结果 VM |
| 算法门面 | `MvpAlgorithmsApi.dll` | `MvpXxxApi` / `XxxTool`，托管封装 |
| C API | `Vision2D_Interface_C.dll`、`Vision2D_Advanced_Interface_C.dll`、`Vision2D_Interface_C_Decoder.dll`、`VisionDL_Advanced_Interface_C.dll`、`VisionEL_Advanced_Interface_C.dll` | `Dcck2D_Function_*` 导出 |
| 原生内核 | `MvsFilters64.dll`、`TotalMeasure64.dll`、`blob64.dll`、`ncc64.dll`、`align64.dll`、`Barcode64.dll`、`Datacode64.dll`、`CameraCalibration64.dll`、`MvpHandEyeCalib64.dll`、`MvpOCRTool64.dll`、`MvpPatInspect64.dll`、`MvpStitchApi` | HALCON 兼容算子实现 |
| 单算子模块 | `BlobOperator.dll`、`CaliperOperator.dll`、`RigidFixtureOperator.dll`、`PixelMapOperator.dll`、`EdgePatternMatchingNew.dll`、`ConventionalOperator3D.dll` 等 50+ | 每类工具一个原生 DLL |
| DL 运行时 | `paddle_inference.dll`、`onnxruntime.dll`、`openvino.dll`、`mkldnn.dll`、`dnnl.dll`、`classifier_plugin_v0_*.dll`、`vanilla_det_plugin_v2_*.dll` | 分类 / 检测 / OCR / 分割 |
| 授权 | `WibuCmNET.dll`、`x64/slm_runtime.dll`、`VP.Services.License.dll`、`hasp_windows_x64_*.dll` | 加密狗 + License |

---

## 3. VisionPlus 算子全量清单

### 3.1 传统 2D 工具（`MvpAlgorithmsApi.dll` 的 Api/Tool 类）

| 族 | 类 / 原生入口 |
|---|---|
| 区域与 Blob | `BlobApi` / `BlobTool` / `BlobToolEx`、`RegionApi` / `RegionTool`、`ImageRoiApi` / `ImageRoiTool(Ex)`、`blob64`、`MvsFilters64` |
| 轮廓与 XLD | `MvpContourApi` / `MvpContourTool`、`MvpXldContourApi` / `MvpXldContourTool(Ex)` |
| 找边测量 | `MvpMeasureApi` / `MvpMeasureTool`、`CaliperToolApi` / `CaliperTool(Ex)`、`FindLineApi` / `FindLineTool(Ex)`、`FindCircleApi` / `FindCircleTool(Ex)`、`FindTipApi`、`MvpFitLineApi` / `MvpFitLineTool`、`MetrologyApi`、`TotalMeasure64`、`ToolLineOperator`、`FitLineOperator` / `FitCircleOperator` / `FitEllipseOperator` |
| 匹配 | `MvpNccMatchingApi` / `MvpNccMatchingTool(Ex/XL)`（PMAlign）、`ShapeMatchingApi` / `ShapeMatchingTool(Ex)`、`MvpPatInspectaApi` / `MvpPatInspectTool` / `MvpPatInspect64`、`EdgePatternMatchingNew.dll`、`align64`、`ncc64` |
| 颜色 | `ColorExtractOperator.dll`、`ColorMatchOperator.dll`、`ColorSegmenterOperator.dll`、`ConvertImageOperator.dll`、`CreateFeature2D.dll` |
| 读码 / OCR | `BarCodeApi` / `BarCodeTool(Ex)` / `Barcode64`、`DataCodeApi` / `DataCodeTool(Ex)` / `Datacode64`、`DecoderOperator.dll`、`Vision2D_Interface_C_Decoder.dll`、`OCRApi` / `OCRTool(Ex)` / `MvpOCRTool64`、`bar_code.dll`、`DMCode-Locate.bin` |
| 标定 | `CalibrationApi`、`CheckerBoardCalibrationTool(Ex)` / `CameraCalibration64`、`NPointToNPointCalibrationOperator.dll`、`HandEyeCalibApi` / `HandEyeCalibTool` / `MvpHandEyeCalib64`、`MvpLcCalibApi` / `MvpLcCalibTool`、`PixelMapOperator.dll`、`ThicknessCalibration.dll`、`MvpStitchApi` / `MvpStitchTool` |
| 几何与变换 | `MvpArithmeticApi` / `MvpArithmeticTool`、`MvpTupleApi` / `MvpTupleTool`、`MvpHomMat2dApi` / `MvpHomMat2dTool`、`FeaturesDistance.dll`、`FeatureIntersectionOperator.dll`、`MvpRobotPosition64` |
| 位置跟随 | `RigidFixtureOperator.dll`（`AtomFixture` / `AtomSettingFixture`） |
| 图像 | `ImageFilterOperator.dll`、`ImageRotationOperator.dll`、`HistogramOperator.dll`、`Convert16ToRGB.dll`、`CircleLocationOperator.dll` / `LineLocationOperator.dll`、`EasyCircleTool.dll` / `EasyLineTool.dll` |

### 3.2 `VP.Algorithm.dll` 的 Atom 算子（按族去重）

| 族 | 算子 |
|---|---|
| 找边 / 拟合 | `AtomFindLine`、`AtomFindCircle`、`AtomFitLine`、`AtomFitCircle`、`AtomCaliper`、`AtomPMAlign` |
| Blob / 颜色 | `AtomBlob`、`AtomBlobEx`、`AtomColorExtract`、`AtomColorMatch`、`AtomConvertImage` |
| 读码 | `AtomDecoder`、`AtomMVPDecoder`、`AtomMVPBarcode` |
| 图像 | `AtomImageFilter`、`AtomImageRotation`、`AtomImageCropping`、`AtomHistogram`、`AtomPixelMap`、`AtomSaveImage`、`AtomSnap` / `AtomSnapVP` |
| 几何 | `AtomGeometryCreation`、`AtomGeometryDistance`、`AtomGeometryIntersection`、`AtomGeometryGuideLine`、`AtomAnglePlanePlane`、`AtomInvertSpace` |
| 位置 / 标定 | `AtomFixture`、`AtomCheckerboard`、`AtomNPoint`、`AtomHandEye`、`AtomPyramidCalibration`、`AtomUseCalibration`、`AtomUsePyramidCalibration`、`AtomMergePyramid` |
| 3D | `AtomHeightCalculator`、`AtomPlaneEstimator`、`AtomVolumeCalculator`、`AtomZmapToImage`、`AtomAlgoTool3DBase` |
| 深度学习 | `AtomDLClassify`、`AtomDLDetection`、`AtomDLOCR`、`AtomDLSegment`、`AtomDLSetPin`、`AtomELClassify` |
| 其他 | `AtomLabelGraphicsVP`、`AtomCreateRecord`、`AtomStatus`、`AtomCanInit` |

### 3.3 C API 导出（`Dcck2D_Function_*`）

| 模块 | 关键导出 |
|---|---|
| `Vision2D_Interface_C.dll` | `InitializeTool` / `SetInputImage` / `SetToolParameter` / `Compute` / `GetToolResults` / `ReleaseTool(Results/Images)`；`LoadImage` / `SaveImage` / `ReleaseImage`；`ComputeSharpness`、`ComputeSimilarity`、`MeanGreyInRegion`、`CalculateHistogram`；`CalibrationAndCreateNode`、`GetCalibedImage` / `GetCalibedTransform`、`GetTransform_SrcImg2DstImg`；`BlobTool_AddMorphOperator` / `ClearMorphOperator` / `SetTransform_Outer2ROI`；`ColorExtractTool_*` / `ColorMatchTool_*`；`EdgePatternNew_SetTrainParams` / `TrainATemplate` / `GetModel` / `SetModel`；`PixelMapTool_*`（`AutoMapParams`、`SetLinearPoints`、`SetQuantization`、`StandardPoints`、`UseImage16`、`Histogram`）；`ConvertImage_16To8`、`ConfigString`、`StopToolManually` |
| `Vision2D_Advanced_Interface_C.dll` | `InitializeTool_Advanced` / `Compute_Advanced` / `GetToolResults_Advanced`；`EasyLineTool_TrainModel` / `SetTrainModelParameters` / `EasyLine_Model(Line/Parameters)` / `SetModel` / `SetRunRoi`；同套 `EasyCircle*` |
| `Vision2D_Interface_C_Decoder.dll` | `InitializeTool_Decoder` / `SetToolParameter_Decoder` / `Compute_Decoder` / `GetToolResults_Decoder` / `DecoderTool_Train` / `EnableDLLocation` / `EnableDLEnhancement` |
| `VisionDL_Advanced_Interface_C.dll` | `InitializeDLTool_Advanced` / `GetDLToolResults_Advanced`；`ClassifyCpu/GpuTool_Infer`、`DetectionCpu/GpuTool_Infer`、`SegmentCpu/GpuTool_Infer`、`OCRCpu/GpuTool_Infer(_SingleRec)` + 各自 `SetInferParameters` |
| `VisionEL_Advanced_Interface_C.dll` | `InitializeELTool_Advanced` / `LoadModelELTool_Advanced` / `AddDatasELTool_Advanced` / `TrainELTool_Advanced` / `UpdateTrainELTool_Advanced` / `GetTrainProgressELTool_Advanced` / `ELToolInfer_Advanced` / `GetELToolROIImg_Advanced` / `RemoveModelLabelELTool_Advanced` |

### 3.4 关键参数语义（从 `MvpAlgorithmsApi` 字符串堆还原，供 spec 设计）

| 工具 | 参数 / 枚举线索 |
|---|---|
| Blob | `blobDetectMode`、`blobDetectNum`、`blobFilter`、`eliminateBorderBlob`、`MVP_BLOB_THRESHOLD_AUTO/HARD`、区域特征 `MVP_BLOB_AREA / CIRCULARITY / RECTANGLULARITY / BOUND_WIDTH / MASSCENTER_X/Y`、极性 `MVP_BLOB_POLAR_DARK_ON_LIGHT` |
| 卡尺 / 测量 | `caliperGap/Width/Height`、`nCaliper`、`edgeThresh`、`edgeAmplitude`、`edgeContrast`、`edgeDirection`、`edgeSingle/Double`、`CP_EDGE_SINGLE/DOUBLE`、`MeasureSigma` / `MeasureThreshold` / `MeasureLength1/2`、`MVP_MEASURE_SELECT_FIRST/LAST/ALL`、`TRANSITION_POSITIVE/NEGATIVE/ALL` |
| 拟合 | `EFitLineType`：`REGRESSION / HUBER / TUKEY / DROP / GAUSS`（稳健拟合，项目 `VisionCaliper` 目前是最小二乘 + 按比例截尾） |
| NCC / PMAlign | `MvpNccConfigParam` / `MvpNccProcessParam(Ex)` / `MvpNccChannelParam` / `MvpNccProcessResult(Ex)`、`MvpNccMatchingInfo`；错误枚举含 `WRONG_ANGLE_RANGE`、`WRONG_SCALE_RANGE`、`TEMPL_SIZE_TOO_LARGE` → 支持角度范围 + 缩放范围 + 金字塔 |
| 标定 | `calibBoardType`、`calibFilePath`、`MVP_CALIB_PLATE_CHECKERBOARD / DOT_GRID`、`MVP_CALIB_DISTORT_LINEAR`、`MVP_CALIB_AREA_SCAN_POLYNOMIAL/DIVISION`、`MVP_CALIB_LINE_SCAN_DIVISION`、`MVP_CALIB_ADAPT_SIZE_TRUE/FALSE`、`MVP_CALIB_TRANS_IMAGE2UNDISTORT`；手眼 `MvpHECRotateNumParam`（0/3/5 次旋转） |
| 颜色 | `EColorSpace`：`RGB/RGBX/BGR/BGRX/HSV/HSI/YUV`；`colorTransLUTHandle`、`mvpCreateColorTransLut` / `mvpApplyColorTransLut` |
| 读码 | `codeType`、`codeNum`、`codeMinWidth/MaxWidth`、`codeStepX/Y`、`decodeMode`、`ECoderType`、`ECoderCapacityType`、`EMvpDataCodeDPM/ISO/SEMI`、`MVP_RESULT_DECODER_1DCODE/2DCODE` |
| 变换 | `homMat2DCompose/Invert/Rotate/Scale/Slant/Translate/Transpose`、`mvpAffineTransImage/Region/ContourXld`、`rotatePoints` |
| DL | `acceptDetThreshold`、`acceptRecThreshold`、`acceptThreshold`、`detNmsThreshold`、`adaptImageSize` |
| 图像 | `cropMinWidth/Height`、`highThreshold/lowThreshold`、`measureSigma`、`imageAbs/Add/Mult/Invert/EquHisto/Emphasize` |

---

## 4. 与 NoCodeMotion 现有 79 算子逐条对照

图例：**✔ 已有** ｜ **◐ 部分覆盖** ｜ **✘ 缺口**

| VisionPlus 算子族 | NoCodeMotion 对应 Key | 状态 | 说明 |
|---|---|---|---|
| FindLine / FindCircle / Fit | `line`、`lines`、`circle`、`arc`、`fitline`、`fitcircle`、`straight`、`edgepoints`、`edgepair`、`edgept` | ✔ | `VisionCaliper.cs` 自研卡尺内核已覆盖 |
| EasyLine / EasyCircle 训练式 | `line` / `circle` | ◐ | 缺「先训练模型再跑」的模式与稳健拟合 Huber/Tukey |
| Caliper 参数化卡尺工具 | `edgepair`、`edgepoints` | ◐ | 缺单边/双边极性、边缘对选择（首/末/全部）、卡尺间距/数量/评分 |
| Blob / Region / XLD | `blob`、`regions`、`connect`、`selectshape`、`shapefeat`、`convexhull`、`contours` | ✔ | 缺 BlobEx 的掩膜编辑与形态算子链 |
| 阈值 / 形态学 | `thresh`、`autothresh`、`dynthresh`、`erode`、`dilate`、`open`、`close`、`tophat`、`bothat` | ✔ | |
| NCC / PMAlign 轮廓匹配 | `shape`、`feat`、`aligntpl` | ◐ | 缺角度范围 + 缩放范围 + 金字塔 + 多目标分数排序 |
| ShapeMatching / PatInspect | `shape` | ✘ | **缺缺陷比对**（模板 vs 当前图差分 + 容差） |
| ColorExtract / ColorMatch | `hueselect`、`colorchk`、`colorspace`、`chan` | ◐ | 缺多区域颜色模板学习与逐区域色差评分 |
| ImageFilter / Rotation / Crop | `gaussblur`、`equhisto`、`invert`、`geom`、`crop`、`enhance`、`denoise` | ✔ | 缺 16→8 位转换与清晰度评分 |
| Histogram / 灰度投影 | `histo`、`pxstat`、`grayproj`、`minmaxgray` | ✔ | |
| 读码 1D / QR | `bar1d`、`qr`、`codecheck` | ✔ | |
| **DataMatrix / 点阵 DPM / 码质量评级** | — | ✘ | OpenCvSharp 无 DM 解码器，需新依赖 |
| Checkerboard / 镜头畸变 | `calboard`、`lenscal`、`lensfix` | ✔ | |
| **N 点 / 手眼 / 金字塔 / 厚度 / 拼接标定** | `caln`、`calone`、`rotcenter` | ◐ | 缺手眼（Eye-in-Hand / Eye-to-Hand）、线扫/多项式畸变、图像拼接 |
| PixelMap 像素映射 | `lensfix` | ◐ | 缺自动标准点、量化、16 位图支持 |
| 几何计算 / 创建 / 交点 / 引导线 | `pp`、`pl`、`pc`、`ll`、`lc`、`cc`、`gap`、`rigid`、`pxstat` | ◐ | 数学工具齐，缺「几何创建 / 引导线 / 显式交点」算子形态 |
| **RigidFixture 位置跟随** | — | ✘ | **完全缺口**，现场高频（先定位 Mark，再带着 ROI 与坐标系走） |
| 图像相似度 / 清晰度评分 | `twoimg` | ◐ | 缺 `ComputeSimilarity` / `ComputeSharpness` 评分输出 |
| **3D：高度 / 平面 / 体积 / Zmap / 面面夹角** | — | ✘ | 需先扩展深度图（ZMap）图像源 |
| **DL：分类 / 检测 / OCR / 分割** | — | ✘ | 需 ONNX Runtime + 模型管理 |
| **EL 传统学习分类** | — | ✘ | 需训练器与样本管理 |

### 现有 79 个 Key 一览（避免重复造）

`line, lines, circle, arc, edgepoints, quad, corner, straight, fitline, fitcircle, edgept, ellipse, fitellipse, edges, contours, edgepair, thresh, autothresh, dynthresh, erode, dilate, open, close, tophat, bothat, connect, selectshape, shapefeat, convexhull, minmaxgray, grayproj, rigid, affineimg, hueselect, invert, equhisto, gaussblur, pp, pl, pc, ll, lc, cc, gap, pxstat, histo, mscale, mcaltwo, px2mm, mm2px, scalar, caln, calone, rotcenter, calboard, lenscal, lensfix, alignpm, aligntpl, alignrel, alignrigid, alignout, shape, feat, blob, regions, bar1d, qr, codecheck, colorspace, chan, colorchk, enhance, denoise, geom, save, load, twoimg, crop`

---

## 5. 缺口算子可行性分级

| 级 | 含义 | 算子 |
|---|---|---|
| **A** | 纯 OpenCvSharp / 纯数学可复刻，无新依赖 | `fixture`、`caliper`、`colormatch`、`sharpness`、`similarity`、`pminspect`、`geomcreate`、`geomintersect`、`geomguide`、`handeye`、`npointcal`、`pixmap16`、`pmalign`（工作量大）、稳健拟合 Huber/Tukey |
| **B** | 需新增第三方解码库 | `dmcode`、`codegrade`（DataMatrix / DPM / ISO 质量评级） |
| **C** | 需新增图像源 / 数据模型（深度图 ZMap） | `zmap2img`、`planefit`、`heightcalc`、`volumecalc`、`anglepp`、`thickness`、`stitch` |
| **D** | 需推理引擎 + 模型文件 + 训练/样本管理 | `dlclassify`、`dldetect`、`dlocr`、`dlsegment`、`elclassify` |
| **E** | 依赖 VisionPlus 原生 DLL + 加密狗，**不建议** | 直接 P/Invoke `Dcck2D_Function_*` |

---

## 6. 落地清单（四个批次已一次性全部完成）

原计划按 2D → 标定 → 3D → DL 分四批，本轮直接一次做完，共 **32 个新算子 / 111 个算子总量**。

| 批次 | 新增 Key | 实现文件 |
|---|---|---|
| 1 · 2D / 几何 / 统计 | `fixture` `caliper` `sharpness` `similarity` `geomcreate` `geomintersect` `geomguide` `hommat` `morphchain` | `Services/Vision/VisionPlus/VpOps2D.cs` |
| 2 · 匹配 / 颜色 / 训练式 / 标定 | `colormatch` `pminspect` `pmalign` `easyline` `easycircle` `colorlut` `pixmap16` `handeye` `npointcal` | `VpOpsMatch.cs` |
| 3 · 3D / 深度图 | `zmapload` `zmap2img` `planefit` `heightcalc` `volumecalc` `anglepp` `thickness` | `VpOps3D.cs` |
| 4 · 深度学习 / 传统学习 | `dlclassify` `dldetect` `dlsegment` `dlocr` `elclassify` | `VpOpsAI.cs` |
| 4 · 读码 | `dmcode` `codegrade` | `VpOpsCode.cs` |

**接线方式（只动了 3 处既有文件）**
- `VisionOperators.cs`：新增 `SourceVisionPlus = "VisionPlus 移植"`、32 条 `VisionOpSpec`（参数走 `ParamJson`，UI 自动生成参数行）、`Sources` 加第四族。
- `VisionOpRunner.cs`：`VisionOpContext` 加一个 `Vp` 状态字典（跨步共享深度图 / 模板 / 样本）；`default:` 分支转给 `VpDispatch`（原来 79 个 case 一个没动）。
- `VisualFlowPage.xaml.cs`：添加步骤菜单加 `【VisionPlus 移植】` 来源前缀。

**新增的支撑文件**：`VpUtil.cs`（参数/ROI/记账工具 + `VpState`）、`VpDispatch.cs`（Key→实现文件分派表）、4 个 `VpOps*.cs`。
**新增依赖**：`ZXing.Net 0.16.11`（纯托管，只有 DataMatrix/PDF417/Aztec 解码需要；ONNX 推理直接用已打包的 `opencv_world480.dll` 的 `Cv.Dnn`，无新原生依赖）。
**守卫**：`tools/guard_sources.py` 的 G33.1 / G45.1 扩展为同时扫描 `VpOps*.cs`（否则「实现拆到独立文件」会被误判成没实现 / 假参数），并让参数读取正则认识 `Int/Bool/Choice`。

### 6.1 已落地算子明细（32 个，全部可设参数、真跑真出数）

**2D / 几何 / 统计（`VpOps2D.cs`）**

| Key | 名称 | 主要参数 | 算法要点 |
|---|---|---|---|
| `fixture` | 位置跟随 | mode / rx,ry,ra / maxShift | 存基准位姿，跟随当前位姿算 Dx/Dy/Dr，偏移写入 `VpState` 供后续 ROI 修正 |
| `caliper` | 参数化卡尺 | roi / cx,cy,angle,length,width / polarity / minGrad / calipers / sigma / edgeMode / select | 垂直探测线双线性采样灰度剖面 → 梯度 → 极性筛选 → 最小二乘拟合边线；双边模式报宽 |
| `sharpness` | 清晰度评分 | roi / method / minScore | 拉普拉斯方差 / Tenengrad / 梯度均值，三种可选 |
| `similarity` | 两图相似度 | mode / roi / method / minScore | 灰度 NCC / 直方图相关 / 结构相似度，输出 0~1 |
| `geomcreate` | 造点/线/圆 | kind / x1,y1,x2,y2 / cx,cy,radius | 直接写 `ctx.Points` 并画在图上 |
| `geomintersect` | 求交点 | kind / fallback / 8 个坐标 | 线线、线圆、圆圆解析解，报交点数与夹角 |
| `geomguide` | 几何引导线 | cx,cy,angle,length,offset | 点+角生成参考线，可垂直偏移 |
| `hommat` | 仿射矩阵运算 | op / dx,dy,angle,scale / px,py | 平移/旋转/缩放/求逆/合成，3×3 实数矩阵，报 a11..a23 与行列式 |
| `morphchain` | 形态学链 | roi / thresh / chain / minArea | 解析 `erode:3,dilate:3,open:5` 链式执行，连通域统计并画框 |

**匹配 / 颜色 / 训练式 / 标定（`VpOpsMatch.cs`）**

| Key | 名称 | 主要参数 | 算法要点 |
|---|---|---|---|
| `colormatch` | 多区域颜色模板匹配 | mode / regions / roi / bins / tol / minScore | HSV 的 H、S 直方图逐区学习，比对用 L1 距离折算分数，总分取均值 |
| `pminspect` | 标准图缺陷比对 | mode / roi / thresh / blur / minArea / align | 相位相关对齐 → 差分 → 二值 → 连通域，按面积筛缺陷 |
| `pmalign` | 金字塔轮廓匹配 | template / roi / a0,a1 / scaleMin,scaleMax / levels / minScore / maxHits | 图像金字塔 + 角度×缩放双扫描 + `MatchTemplate` + NMS 去重 |
| `easyline` | 训练式找线 | mode / roi / cx,cy,angle,length,width / polarity / minGrad / samples / minScore | 训练存几何与模板；运行做真实探针扫边 + 最小二乘拟合，分数 = 命中率×梯度置信 |
| `easycircle` | 训练式找圆 | mode / roi / cx,cy,radius,tol / polarity / minGrad / samples / minScore | 径向探针 + Kasa 线性化最小二乘拟合圆 |
| `colorlut` | 颜色查找表 | mode / gain,bias,gamma / lo,hi | 256 项 LUT（亮度对比度/伽马/反色/通道交换/分段拉伸），出图算子 |
| `pixmap16` | 16 位像素映射 | mode / lo,hi / levels / gamma | 16U/8U→32F→自动拉伸/手填区间/量化/伽马压缩→8U 出图 |
| `handeye` | 手眼标定 | mode / ux,uy,mx,my / type / rotNums / minPoints | 点对累积 → 正规方程高斯消元解仿射 → RMS 残差、像素当量；轴装相机时对机台点拟合转动中心 |
| `npointcal` | N 点标定 | mode / ux,uy,mx,my / kind / minPoints | 仿射 6 参最小二乘 / 相似变换 / 透视 `FindHomography`，写 `ctx.Affine` |

**3D / 深度图（`VpOps3D.cs`）**

| Key | 名称 | 主要参数 | 算法要点 |
|---|---|---|---|
| `zmapload` | 载入深度图 | path / unit / invalid / cols | 支持 16 位 PNG/TIFF 与 CSV/TXT 文本深度图，转 32F、乘单位、无效值置 NaN |
| `zmap2img` | 深度图转可视 | mode / zmin,zmax / steps | 线性拉伸 + Jet 伪彩 / 灰度 / 等高线分带，出图算子 |
| `planefit` | 平面拟合 | roi / slot / step / maxRms | 正规方程解 z=ax+by+c，报系数、倾角、RMS，结果存平面A/平面B |
| `heightcalc` | 高度测量 | roi / ref / baseZ / step / maxH,minH | 相对拟合平面或固定基准，报最大/最小/平均高度与高度差 |
| `volumecalc` | 体积计算 | roi / baseZ / pixelMm / step | Σ max(0,z−baseZ)×像元面积，报立方毫米体积 |
| `anglepp` | 面面夹角 | useManual / 6 个法向量 / maxAngle | 法向量归一化点积反余弦，可手填法向量 |
| `thickness` | 厚度测量 | roi / pixelMm / target / tol / step | 双平面间距 / 单平面相对深度，按目标公差判定 |

**读码（`VpOpsCode.cs`）**

| Key | 名称 | 主要参数 | 算法要点 |
|---|---|---|---|
| `dmcode` | DataMatrix 读码 | roi / expect / polarity / tryHarder | ZXing.Net 纯托管解码（含旋转重试），报内容与码制 |
| `codegrade` | 码质量评级 | roi / format / minScore / minCells | 定位图形对比度、模块数、静区与清晰度综合打分（ISO 风格近似） |

**深度学习 / 传统学习（`VpOpsAI.cs`）**

| Key | 名称 | 主要参数 | 算法要点 |
|---|---|---|---|
| `dlclassify` | ONNX 图像分类 | model / labels / roi / size / topk / minScore | `Cv.Dnn` 真推理 + 标签文件，报 Top-K 与置信度 |
| `dldetect` | ONNX 目标检测 | model / labels / roi / size / conf / nms / maxHits | 兼容 SSD `[1,1,N,7]` 与 YOLO `[1,N,5+C]` 两种输出，IoU NMS |
| `dlsegment` | ONNX 图像分割 | model / roi / size / classes / conf | 逐像素 argmax + 阈值过滤 + 固定调色板着色，出图算子 |
| `dlocr` | 文字检测识别 | model(可选) / roi / lang / expected / match / ignoreCase | 检测框 + Windows.Media.Ocr 识别 + 四种匹配模式判定 |
| `elclassify` | 样本学习分类 | mode / folder / label / roi / bins / k / maxDist | 自研特征（RGB 直方图 + 均值方差 + 边缘密度）+ kNN，无需模型 |

### 6.2 验证结果

| 项 | 结果 |
|---|---|
| `dotnet build NoCodeMotion.csproj -c Debug` | **0 警告 0 错误** |
| `python tools/guard_sources.py` | **553 PASS / 0 FAIL**（G33.1 确认 32 个新算子均有实现，G45.1 确认每个参数都被真实读取） |
| 冒烟（32 个算子逐个真跑） | 111 个算子总量，32 个新算子全部产出结果：异常 0 / 无结果 0 / 未实现 0 / FAIL 0 |

冒烟中少数「诚实失败」（无 ONNX 模型、无码、参数没对准 ROI）均由算子按中文原因明确报出，属于真实能力边界而非占位。

### 固定落点（仅明文文件）

| 文件 | 改动 |
|---|---|
| `Services/Vision/VisionOperators.cs` | 追加 `VisionOpSpec`；新增常量 `SourceVisionPlus = "VisionPlus 移植"`（**不要动现有三族标签计数**） |
| `Services/Vision/VisionOpRunner.cs` | `switch` 加 `case`，实现 `DoXxx(...)`；必要时给 `VisionOpContext` 加字段 |
| `Models/VisualFlowStep.cs` | 仅在需要新持久化字段时动；参数一律走 `GetOpParam/SetOpParam` → `ParamJson` |
| 自动生效，无需改 | 添加步骤菜单、参数行、节点图工具箱、`OperatorManualSheet` 算子手册、AI 交换词汇 |

---

## 7. 约束与风险

1. **商业授权**：本报告只记录接口名称与功能语义，用于自研复刻。**不得**把 VisionPlus 的 DLL / 源码 / 模型文件复制进工程，也不得在工程内直接调用其 C API —— 该软件带 Wibu 加密狗与 License 校验，且 EULA 风险由使用方承担。
2. **加密源文件**：仓库 432/451 个 `.cs` 带 `88 7D 1C` 磁盘加密包装，含 `FlowItem.cs`、`FlowViewModel.cs`、`FlowRunnerService.cs`、`VisionCaliper.cs`、`MvsCameraService.cs` 等；**禁止用普通文本编辑器回写**，只能通过外围新增明文文件扩展。可安全编辑的明文清单见附录调查（核心是 `VisionOperators.cs`、`VisionOpRunner.cs`、`VisionEngine.cs`、`NgDefs.cs`、`VisualFlowStep.cs`、`VisualFlowDetailViewModel.cs`）。
3. **守卫断言**：`tools/guard_sources.py` 的 `G55.3` 要求 `SourceWenVision` **恰好 11 个**、`G56` 要求 `SourceHalcon` **≥27 个**、`G55.1` 要求三族常量齐全。新增第四来源常量不破坏这些断言，但**绝不能改动现有三族标签的数量**。
4. **两条铁律**：`Key` 是落盘值，上线永不改名；只登记「真能跑」的算子 —— 目录里有、跑起来报「未实现」比没有更坑现场。
5. **构建环境**：net10.0-windows10.0.19041.0，本机 SDK `10.0.102` / `9.0.317`；`OcctNet.Wrapper` 只支持 net10，本项目不可降级 net8。

---

## 8. 交付状态

- 已全量落地：**32 个新算子**（合计 111 个），全部带可设置参数、全部为真实算法实现，构建 / 守卫 / 冒烟三项全绿。
- 新增文件（全部明文，可安全编辑）：
  - `Services/Vision/VisionPlus/VpUtil.cs` —— 参数读取、ROI、结果记账、跨步状态 `VpState`
  - `Services/Vision/VisionPlus/VpDispatch.cs` —— Key → 实现文件分派表
  - `Services/Vision/VisionPlus/VpOps2D.cs` / `VpOpsMatch.cs` / `VpOps3D.cs` / `VpOpsAI.cs` / `VpOpsCode.cs`
- 改动既有文件仅 3 处：`VisionOperators.cs`（来源常量 + 32 条 spec）、`VisionOpRunner.cs`（ctx 状态字典 + `default` 转分派）、`VisualFlowPage.xaml.cs`（来源前缀）。原 79 个算子实现一行未动。
- 新增依赖：`ZXing.Net 0.16.11`（纯托管，仅 DataMatrix 解码用）；ONNX 推理走已打包的 `opencv_world480.dll`，无新原生依赖。
- 后续可继续做（当前未做）：模型文件分发型部署（ONNX 模型管理页）、3D 传感器的实时 ZMap 图像源、DL 结果的坐标反投影到机械手坐标系。

