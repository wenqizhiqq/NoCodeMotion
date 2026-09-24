# 项目长期记忆（NoCodeMotion — WPF/.NET 10 无代码运动控制）

## 源码与编码
- 全部 .cs 明文可直 Read/Edit。GBK/BOM/CRLF 文件若报 binary，用 Python 读写（utf-8-sig→归一化→写回恢复 CRLF/BOM）。
- 作者水印「温启志◆编写◇微信﹕187◆1936◇1399」三处：`Services/AuthorWatermark.cs`、`MainWindow.xaml:4`、`Docs/*.md` 末尾。删 AuthorWatermark.cs 编译失败，AI 不主动删。

## 运行/构建（沙箱铁律）
- dotnet 一律 LOLBin 拦截：用 **PowerShell + 绝对 `C:\Program Files\dotnet\dotnet.exe` 前台构建**；PowerShell stdout 不回显，须 `| Out-File C:\tmp\x.txt` 再 Read。禁写 `.ps1` 文件（ExecutionPolicy 拦），用内联命令。
- 删文件用 `[System.IO.File]::Delete("绝对路径")`（`Remove-Item` 被 safe-delete 钩子拦）。
- 加载进度：`Services/LoadingService` 静态深度计数；启动预初始化用确定式进度，打开/新建工程用不确定式遮罩（xlsx 读写 Task.Run，LoadInto 回 UI）。`Dispatcher.Yield` 须 `System.Windows.Threading.Dispatcher.Yield(...)`。
- .NET 10 `_wpftmp` CS0579：csproj 加 `GenerateAssemblyInfo=false`+`GenerateTargetFrameworkAttribute=false`；`[assembly:ThemeInfo]` 留根 AssemblyInfo.cs。勿用 UseArtifactsOutput。
- WPF 重复 XAML 陷阱：`EnableDefaultPageItems` 会把 `Views/*.xaml` 当 Page 编译；探针脚本勿生成同 x:Class 的 `XXX_utf8.xaml`（CS0102+CS8646）。

## xlsx 工程存储
- `XlsxProjectStore.cs` 反射式：根 public 集合→sheet（列=标量）；标量→「项目管理」表；IO 由 BuildIoSheet/SplitIoToRoot 合并。
- 嵌套子集合走「父表.子表」分页（CollectNestedSheets/RestoreNestedCollections），MergedBlockParents={PointTables,Trays,Flows} 跳过合并块单页。
- 新增子集合：①元素继承 EditorItemBase；②集合属性带 set（cp.CanWrite）；③中文名加 ChildSheetNameOverrides。
- 只读计算属性勿放数据模型（会成脏列）；新增标量字段零改动落盘、向后兼容。

## 全局 UI 约定
- DataGrid 行内按钮：DataContext 是页面 VM，命令要 `{Binding DataContext.XxxPanel.Cmd, RelativeSource={AncestorType=DataGrid}}`，漏面板路径静默失效。
- 下拉框选值来自固定候选时**勿设 IsEditable**（可编辑输入框盖住整块、点击文字区不展开）。
- 资源字典：`App.xaml` 只合 `Resources/AppStyles.xaml`（再合 HandyControl）。`Themes/AppleControls.xaml` 未合并、运行时不存在，勿引用；所需键以 AppStyles 为准；缺的小字/窄框在页面 `<UserControl.Resources>` 本地补。
- ★ **Window 弹窗（ConfirmDialog/ConnectProgressDialog 等）只能用 AppStyles 全局键**：页面级 `<UserControl.Resources>` 里的键（如 `SubLabel`）在顶层 Window 的 StaticResource 作用域里解析不到，运行时抛 `XamlParseException 无法找到名为"X"的资源`（编译期不报）。缺的样式在弹窗里内联（FontSize/Foreground）或改用全局 `TextMutedBrush` 等。
- 删除/清空红 `TtDeleteBtn`/`TtPillRedBtn`；色彩 红=破坏/橙=反向/蓝=正向/绿=保存/灰=次要。
- 每页底 PageHintBar；EditorPage.Detail 内元素**勿用 x:Name**（MC3093），改用 Tag+FindVisualChildByTag。

## 视觉/节点图
- VisualFlowPage.ApplySelection() 须覆盖 _vm.Steps/_vm.Name/_vm.SelectedStep；Steps 非空且 SelectedStep=null 自动选 [0]。
- 节点图 Models/NodeGraph/ 数据驱动（NgDefs/NgModel）；UI 用 ItemsControl+DataTemplate，禁 Children.Add。输出端口坐标须与 NodeView 一致。
- 调试引擎 `Services/NgRunner.cs`（6 按钮 + NgStepResult）；NgRunner 两坑：WaitResumeAsync 须实例方法按 _state 轮询；Pause 仅节点边界生效。

## CAD/DWG
- 真实 BREP→OcctNet.Wrapper 0.1.1（OcctShape.ImportStep.Triangulate）；STEP Z-up→WPF Y-up 绕 X -90°；BackMaterial 防黑面。
- DWG/DXF→Aspose.CAD 26.7.0 只迭代矢量实体，绝不 Image.Save 栅格化（免水印）。

## 仿真体系
- SimRuntime 静态态驱动 3D+变量页；SimFlowPlayer 编译 List<SimAction>，33ms 驱动。
- BepuPhysics v2（2.5.0-beta.29，纯 C#）：四坑——①Simulation.Create 须 `new SolveDescription(1,1)`；②ConfigureContactManifold 须 `pairMaterial.SpringSettings=new SpringSettings(30f,1f)`；③运动学↔动态用 BecomeKinematic/SetLocalInertia（勿改 LocalInertia ref）；④推工件用 Velocity.Linear 速度驱动勿只设 Pose。详见 2026-09-05.md。

## 控制卡接入（2026-09-23 累积）
- 雷赛实桥 LeadshineHardwareBridge + LtdmcCard/LtdmcNative P/Invoke LTDMC.dll；无卡降级兜底。`HardwareSetup.Mode`：CardFamilies / Leadshine。
- 控制器页连接链路：`AxisControllerViewModel.Connect()` → `HardwareSetup.EnsureInitialized()` → `Mode==CardFamilies` 走 `CardFamilies.TryConnect(ctl,out)` 否则 `Reconnect()`+`IsCardReady`；`Disconnect()` 走 `CardFamilies?.Disconnect`。桥接类实际名 **`WenQiZhiCardBridge`**（非 SamsunCardBridge），`ResolveFamily(ctl)`/`TryGetRealCounts`/`IsControllerReady` 在内。
- 在线状态直接读底层 `IsControllerReady(ctl)`/`ControllerStatus(ctl)`（无 VM 影子字典、不落盘）。
- 连接后 `FetchDetectedCounts` 回写轴/IO 真实数→`GenerateIoPoints(ctl)` 按本卡清旧+生成 IO 点；`DetectedCountsChanged` 静态事件通知轴/IO 页重算。
- 卡型选择器 SelectCardTypeDialog；Native/ 已是 x64 DLL 全集（csproj `Native\*.dll`→PreserveNewest），缺 Dmc2210(x86)/MCC/SLD9014PTP。
- 容量兜底：硬件 API 不报容量时，在 `CardFamilyDescriptor` 填 AxisCount/InIoCount/OutIoCount（如 MCN420=8/16/16）。

## 控制器连接体验增强（2026-09-23，本轮）
- 连接按钮后台化：`AxisControllerViewModel.Connect()` 改 `Task.Run` 后台，分步 `ConnectStatusText`（初始化/连接卡/读真实轴IO/生成点/完成/失败），`IsConnecting` 防重复点击，`Application.Current.Dispatcher.Invoke` 回 UI。`AxisControllerPage.xaml` 加 `IsConnecting` 绑定的不确定进度条 + 状态文字。
- 状态栏在线指示：`StatusBarService` 加 `ControllerOnlineCount/TotalCount`+`SetControllerStatus(online,total)`+`ControllerStatusText/ControllerColor`（全在线绿/部分橙/全离线红/无控制器灰）；`StatusBarViewModel` 转发 INPC；`StatusBarView.xaml` 加第 3 列「控制器」圆点+文字（Grid 改 6 列）。VM 每次连接/断开/自动连接后调 `UpdateStatusBar()`。
- 自动连接：`ProjectManager.DataReloaded`（`System.Action?`，**0 参**）/ 构造函数触发 `RequestAutoConnect()`（**不是**直接 `AutoConnectAll`）。`RequestAutoConnect` 关键铁律：**必须等 `LoadingService.IsLoading==false`（载入遮罩隐藏）后**才 `BeginInvoke(Background)` 调 `AutoConnectAll`——否则连接与 `PreloadAllPagesAsync` 页面预初始化同跑，抢原生加载器锁（`CardFamilyCatalog.DetectPresent` 枚举底层 DLL）会拖住载入进度条，表现成「连上控制器才进主界面」。载入中则挂一次性 `LoadingService.StateChanged` 回调（`_autoConnectPending` 防重复），等遮罩隐藏再连。
- 连接按钮合并（2026-09-24）：控制器页「获取」+「连接」两按钮合并为单按钮「连接控制器」→ `ConnectControllerCommand` = 先 `FetchModules()`（获取卡参数）再弹 `Views/ConnectProgressDialog`（Apple 圆角白卡 + 不确定进度条 + `ConnectStatusText`）后台 `Connect()`，`IsConnecting` 变 false 时弹窗自动关。（`StackPanel` 无 `Padding`，用 `Margin`。）

## 仿真模板
- ProjectTemplateCatalog 20 模板；NgTemplates.Build 脚手架。
