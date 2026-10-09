# NoCodeMotion UI / 视觉验证细节（MEMORY.md 的补充）
> MEMORY.md 只留索引，本文件放完整推导。触发场景：改流程页/工程师页/名称库下拉、或要「证明 UI 真的对了」。
> 逐日原始推导见同目录 `YYYY-MM-DD.md`。

## 一、名称库下拉「级联二级菜单」（Request N）
- 触发条件：候选 **>20** 且至少一个名字能截出前缀 → 分组；**≤20 个保持平铺**。
- 分组规则：按**第一个 `-`（半角或全角）前**的文本分组；截不出前缀的归「其他」。
- 数据三件套：
  - `Services/NamePrefixGroupDescription.cs`（`GroupNameFromItem` **只有 3 参重载**）
  - `Catalog.ApplyGrouping` —— 打在**默认视图**上（故意全局，含并集 `AllNames`），一处改即全体生效
  - `Views/NameGroupHeaderBehavior.cs` —— `[ModuleInitializer]` 注册 ComboBox **类处理器** → 显式 Style / 内联 Style / 无 Style 三种下拉一律生效
- **条目仍是 `string`**（没有包成对象），所以 `SelectedItem` 绑定不用改。
- ★ 呈现：一级只列分类行（`下料 (15) ›`），悬停/点击 → **右侧飞出**二级菜单列真实名称。
  - `GroupStyle.ContainerStyle` = `NameGroupCascadeContainerStyle`（`TargetType=GroupItem`）
  - `NameGroupFlyout` 行为：按名找 `row` / `flyout`；**轮询**判定「一级行 + 二级菜单」都没鼠标才收起；点收起后要**临时抑制悬停**，否则立刻弹开；`Unloaded` 必须收掉，否则 `Popup` 作为独立顶层窗口留在屏幕上。
- ★★ **条目宿主必须是 `<ItemsPresenter/>`**：`CellComboStyle` 的下拉原本是裸 `<StackPanel IsItemsHost="True"/>`。
  **裸面板宿主只实体化最外层 GroupItem（= 一级分类行），组内条目一个都不生成** → 下拉只剩分类标题、真实名称点不到。
  此时 XAML 合法、build 绿、守卫绿、「分类标题能渲染」也绿 —— **只有真打开下拉才暴露**。守卫 G20/G21 钉这条。

## 二、工程师页
- 三列表（输入 IO / 输出 IO / 气缸）：各一个 `ListBox` + **`VirtualCardListStyle`**（虚拟化 + 回收 + 像素滚动）+ 顶部搜索行（`TtSearchBox`、命中数、「取消」）+ `*Search` 过滤属性；行样式 `MiniCardStyle` + `Padding="10,5"`。
  ★ **`ItemsControl` + 外部 `ScrollViewer` 不虚拟化** → 必须用 `ListBox`。
- 布局：外层 `Grid` 两列 star 宽 —— 左「IO 控制 + 气缸控制」、右「**轴控制 + 点位移动和设置**」（轴控制紧贴点位表正上方）。★ **列宽比例是布局口味，别锁死**。

## 三、UI 验证硬约束（本机）
- 驱动 UI 用 `ctrl.GetInvokePattern().Invoke()`，**不合成鼠标**；导航项 `TextBlock` 沿 `GetParentControl()` 上溯到外层 `ButtonControl`。`uiautomation` 2.0.29 **无** `GetSupportedPatterns()`。`PrintWindow` 不可靠（无桌面客户区全白）→ 以 UIA 文本为准。读 ComboBox 候选取它自己的 `ListItemControl` 子孙。
- ★ **`RenderTargetBitmap` 离屏渲染是可靠视觉证据**（真走可视化树）；`Measure/Arrange` + `UpdateLayout()` 后读 `ActualWidth/ActualHeight` 可**量化**验证。
  - ★ 元素是**活对象**：重排后 `ActualWidth` **就地更新** → 测拉伸要在改尺寸**前**取值。
  - ★ **阈值/比例都别锁死** → 断言**关系**（≥列宽 85%、随列宽单调增、两列铺满）。
- ★ 离屏搭件的坑：
  - **裸控件取不到主题模板**（`GetChildrenCount==0`）→ 自给最小 `ControlTemplate`。
  - 框架 `Loaded` **不自动触发** → 手动 `RaiseEvent(FrameworkElement.LoadedEvent)`。
  - 计数按自定义 `Tag`/`Name`，别数全部 `Border`。
  - **关着的 `Popup` 内部树不实例化** → 走**逻辑属性**（`Popup.Child` → `Border.Child` → `ScrollViewer.Content`）。
  - 条目模板只给裸 `TextBlock` 不绑 `Text` → 占位却渲染空白。
  - 别数整棵树的 `Path`/`Shape`（滚动条箭头也是 `Path`）→ 只在目标控件**内部**数。
- ★ **`RenderTargetBitmap` 渲染「还挂在别的父级里」的子元素会画出空白**（偏移把它推出位图范围）→ 只渲染**脱离窗口的独立控件**（整页 / 新建的搭件）；内容挂在 `Window` 上时先摘下来（`npd.Content = null`）再渲染。**别拿「在树里找得到」当渲染证据。**
- ★ `EditorPage` 在 `Items.Count==0` **且**宿主页设了非空 `EmptyHint` 时把右侧详情整块 `Collapsed` → 离屏空态量到的详情/提示栏是 **0×0**（假象，不是布局问题）。
  要量版面先给 `ProjectStore.Data` 灌数据：各页 VM 的 `Items` 就是 `ProjectStore.Data.Axes/Cylinders` 那个实例，`CopyFrom` 原地改内容即可（`Data` 是 private set）。

## 四、XAML 实例化回归（比 build 更靠得住）
- 构建**通不过**这一关：`{StaticResource}` 找不到是**运行期** `XamlParseException`。
- 所以冒烟段 F 会真的 `new` 一次页面/弹窗（`PointPage` / `EngineerPage` / `CameraPage` / `VisualFlowPage` / `GraphGenDialog`），走 `InitializeComponent()` 解析 XAML。
- ★ 给某个页面加了新控件/新绑定后，**把它加进段 F** —— 这是唯一能抓到「键写错 / 绑错」的自动手段。
