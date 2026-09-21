# NoCodeMotion 长期项目笔记

## 构建 / 验证（最重要）

**本机 `dotnet build` 是可以跑的。** 若报
`NuGet ... Value cannot be null. (Parameter 'path1')`（或 `NETSDK1060 加载 obj/project.assets.json 失败`），
根因是当前 shell 里 `APPDATA` / `ProgramFiles` 为空字符串，**不是 SDK 坏、不是代码错、不是 obj 损坏**。
用 `env` 补上即可（bash 无法 export 带括号的变量名，必须用 `env`）：

    env "APPDATA=C:\Users\admin\AppData\Roaming" "ProgramFiles=C:\Program Files" \
        "ProgramFiles(x86)=C:\Program Files (x86)" dotnet build -v q --no-restore --nologo

基线：**0 错误**，约 590 条 `CA1416` 警告全部是既有的（AvalonEdit 跨平台分析），与本项目改动无关。

**运行时冒烟测试**：`C:\Users\admin\AppData\Local\Temp\ncm_smoke\`（csproj + Program.cs），
`ProjectReference` 指向本工程，`net10.0-windows` + `UseWPF=true`（控制台 Exe 也必须开 WPF）。
必须放在工程目录**之外**（为了不污染产品源码树）。**修正（2026-09-21 实测）**：以前这里写的是
「默认 `**/*.cs` glob 不排除 `.bt/` 之类目录」——**这个说法是错的**。实测：往 `.bt/` 放一个内容为
`this is definitely not valid C# at all !!!` 的 `.cs`，再 `dotnet build -t:Rebuild`，结果仍是
**0 错误 / 590 警告** → **点开头的目录（`.bt/`）默认就不参与编译**。
真正会被编进去的是**不带点**的临时目录（`NoCodeMotion.csproj` 里只显式 `Remove` 了 `artifacts\**`，
所以 `scratch/`、`tmp/` 这种名字会被 `**/*.cs` 收进去）。
用 `env "APPDATA=..." ... dotnet run -v q --nologo` 跑。适合验证 `Models/` + `Services/` 的纯逻辑，
不用起 GUI。

## 代码约定

- **隐式 using 不含 `System.Windows`**：`GlobalUsings.g.cs` 只有 `System / System.Collections.Generic /
  System.Linq / System.Threading / System.Threading.Tasks`。用 `Application.Current` 等必须显式
  `using System.Windows;`。
- **行尾 / BOM 逐文件判断，不要假设全仓统一**：仓库里 LF 与 CRLF 文件混存（如 `Models/ProjectTemplate.cs`
  是 LF，`Models/PointItem.cs` 是 CRLF），且多为 UTF-8 **带 BOM**。改文件时保持该文件自己的风格，
  写入前归一化，改完复查 `bareLF` 是否为 0。
- **文件头尾有作者水印注释**，内含零宽字符（U+200B/U+2063/U+200D 等），必须按字节原样保留，不可重打。
- `core.autocrlf=true`，所以 `git show HEAD:file` 看到的行尾与工作区不一致，判断行尾要看工作区文件本身。

## 架构要点

- `Catalog`（`Services/Catalog.cs`）是**下拉框用的全局名称缓存**，只在 `ProjectStore.Load()` 与
  `ProjectManager.LoadInto()` 里通过 `SyncAllFromData` 重建；各配置页靠
  `ListEditorViewModel.SyncCatalog()` + `CatalogCategory` 增量刷新。
  **它不是数据源**——需要判断「某设备是否存在」时应读 `ProjectStore.Data`。
- 自动保存链路：`Model.PropertyChanged → Parent.OnChildChanged → Parent.OnPropertyChanged(nameof(Children))
  → ListVM.OnItemPropertyChanged → ProjectStore.ScheduleSave()`。
  **任何运行态/只读展示属性都必须在这条链上被过滤掉**，否则会形成每秒几十次的写盘风暴。
- `PointItem.ConditionRowCount = 6`，`EnsureConditionRows()` 会把条件行规整为固定 6 行并裁掉尾部空行。
  **往条件集合里塞数据只能按下标覆写，不能 Append。**

## 表格工具栏 / 流程页（2026-09-21 新增）

- **`TableToolbar` 的「共 N 项/步」文案来自 `MatchInfo`，不是 `Count`。**
  `Count` / `CountLabel` 两个 DP 只参与文案拼接，从不直接显示。
  `MatchInfo` 曾在 `OnLoaded` / `SearchText` 变化时才重算，导致行数变化（粘贴生成换掉整个步骤集合、
  点「添加」）后标签一直停在打开页面时的数字。现已在 `TargetGrid.ItemContainerGenerator.ItemsChanged`
  上订阅刷新，并以 `TargetGrid.ItemsSource` 为行数权威来源（没挂上 grid 时才退回 `Count`）。
  **给表格页加「计数」类展示时，先确认自己绑的是不是真正被显示的那个属性。**

- **流程「名称」列是受限下拉**：`ComboBox ItemsSource` 走 `FunctionToNamesConverter` → `Catalog.*Names`，
  值不在库里就**渲染成空白**（作者既定设计）。`Catalog` 只是下拉缓存，判断对象是否存在要读 `ProjectStore.Data`。
  **不要为了让它显示未知值而把当前值并进 `ItemsSource`（MultiBinding）**：
  `SelectedItem` 默认 TwoWay，ItemsSource 换实例会被 WPF 置空并回写 `null`，把用户刚选的名称清掉。
  现在的做法是导入时提示「哪些对象在工程里不存在」。

- `AiProjectExchange` 输出的 JSON 是给人看、要粘进 AI 对话的，
  **必须用 `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`**，否则中文全变成 `\uXXXX`。

## 验证 UI 的硬约束（本机）

- **这台机器的桌面没有交互式输入**：`GetForegroundWindow() == NULL`、`GetCursorPos() == (0,0)`。
  合成鼠标（`mouse_event` / `uiautomation` 的 `.Click(simulateMove=True)`）**会打空**：
  UIA 找得到控件、`IsEnabled=True`、坐标正常，但点了毫无反应。
  → 一律改用 `ctrl.GetInvokePattern().Invoke()` 直接调 WPF 命令。
  导航项是 `TextBlock`，要沿 `GetParentControl()` 上溯到外层 `ButtonControl` 再 Invoke。
  `uiautomation` 2.0.29 **没有** `GetSupportedPatterns()`（那是裸 UIA COM API）。
- **`PrintWindow` 在这种状态下客户区全白**（标题栏正常），截图不能当视觉证据 —— 以 UIA 读到的文本为准。
- 断言「计数标签是活的」不能只看一次结果：要**连粘两次不同条数**（如 7 → 3），
  否则工程里本来就是这个数，看不出标签是死的还是活的。
