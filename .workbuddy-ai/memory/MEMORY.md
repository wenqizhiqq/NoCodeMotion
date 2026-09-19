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
必须放在工程目录**之外**——主工程默认 `**/*.cs` glob 不排除 `.bt/` 之类目录，写进去会被编进产品。
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
