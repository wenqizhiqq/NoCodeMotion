// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
// ◆温​启‎志‌◆‌编‌写‍◇‍微‍信⁠﹕⁠1‌8​7⁠◆‌1‌9‌3‌6‌◇⁣1‌3‌9⁠9⁠　‌※‍保‍留‍所⁣有⁣权⁠利⁠请⁣勿‍删‌除‍◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.Win32;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>设计器中一个控件的包装：Model 是落盘数据；LiveText 只是显示框的运行时文本（不落盘）。</summary>
    public class DesignerWidgetVM : ViewModelBase
    {
        public DesignerWidget Model { get; }

        private bool _isSelected;
        /// <summary>是否被选中（画布上的高亮框、右侧属性面板跟着它）。</summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value);
        }

        private string _liveText = string.Empty;
        /// <summary>显示框的实时文本（250ms 定时刷新；不落盘）。</summary>
        public string LiveText
        {
            get => _liveText;
            set => SetField(ref _liveText, value);
        }

        private string _selectedPage = string.Empty;
        /// <summary>分页控件当前显示的页名（视图状态，不落盘；默认第一页）。</summary>
        public string SelectedPage
        {
            get => _selectedPage;
            set
            {
                if (!SetField(ref _selectedPage, value)) return;
                Owner?.UpdateTabVisibility();
            }
        }

        private bool _isTabActive = true;
        /// <summary>该控件当前是否可见：主画布控件恒 true；挂在分页下的控件只在对应页被选中时 true。</summary>
        public bool IsTabActive
        {
            get => _isTabActive;
            set => SetField(ref _isTabActive, value);
        }

        /// <summary>分页控件的页名列表（来自 Param，用「|」分隔；空则默认 页1|页2）。</summary>
        public List<string> TabPageList
        {
            get
            {
                var pages = (Model.Param ?? "")
                    .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(p => p.Length > 0).Distinct().ToList();
                if (pages.Count == 0) pages.AddRange(new[] { "页1", "页2" });
                return pages;
            }
        }

        private ICommand? _selectPageCommand;
        /// <summary>点分页页头切页（页头按钮 CommandParameter = 页名）。</summary>
        public ICommand SelectPageCommand => _selectPageCommand ??=
            new RelayCommand(o => { if (o is string p && TabPageList.Contains(p)) SelectedPage = p; });

        // ===== 分页的页面管理（属性面板：添加 / 删除 / 重命名页标签） =====

        private ICommand? _addPageCommand;
        /// <summary>添加一页（页名自动取 页N，不与现有页重名）。</summary>
        public ICommand AddPageCommand => _addPageCommand ??= new RelayCommand(_ =>
        {
            if (Model.WidgetType != "分页") return;
            var pages = TabPageList;
            int n = pages.Count + 1;
            while (pages.Contains($"页{n}")) n++;
            pages.Add($"页{n}");
            Model.Param = string.Join("|", pages);
            SelectedPage = TabPageList[^1];
        });

        private ICommand? _deletePageCommand;
        /// <summary>删除一页（CommandParameter = PageTabVM）；挂在被删页上的子控件改为每页都显示。</summary>
        public ICommand DeletePageCommand => _deletePageCommand ??= new RelayCommand(o =>
        {
            if (o is PageTabVM p) DeletePage(p.Name);
        });

        /// <summary>重命名一页：改 Param，并同步更新挂在旧页名上的子控件。</summary>
        public void RenamePage(string oldName, string newName)
        {
            var pages = TabPageList;
            int idx = pages.IndexOf(oldName);
            if (idx < 0) return;
            pages[idx] = newName;
            Model.Param = string.Join("|", pages);
            if (SelectedPage == oldName) SelectedPage = newName;
            if (Owner != null)
            {
                foreach (var vm in Owner.Widgets)
                    if (vm.Model.TabName == Model.Name && vm.Model.PageName == oldName)
                        vm.Model.PageName = newName;
                Owner.UpdateTabVisibility();
            }
        }

        /// <summary>删除一页：改 Param（至少保留一页），并清掉挂在该页上的子控件的页面归属。</summary>
        public void DeletePage(string name)
        {
            var pages = TabPageList;
            if (!pages.Remove(name)) return;
            if (pages.Count == 0) pages.Add("页1");
            Model.Param = string.Join("|", pages);
            if (SelectedPage == name || !TabPageList.Contains(SelectedPage))
                SelectedPage = TabPageList[0];
            if (Owner != null)
            {
                foreach (var vm in Owner.Widgets)
                    if (vm.Model.TabName == Model.Name && vm.Model.PageName == name)
                        vm.Model.PageName = "";
                Owner.UpdateTabVisibility();
            }
        }

        /// <summary>所属页面 VM（用于切页联动显隐）；ReloadFromStore / AddWidget 时回填。</summary>
        public CustomDesignerViewModel? Owner { get; set; }

        public DesignerWidgetVM(DesignerWidget model)
        {
            Model = model;
            // 分页控件默认停在第一页；Param（页名表）改动 → 刷新页列表并收敛当前页
            if (model.WidgetType == "分页")
            {
                _selectedPage = TabPageList[0];
                RebuildTabPages();
                _lastX = model.X; _lastY = model.Y; _lastW = model.Width; _lastH = model.Height;
            }
            model.PropertyChanged += (_, e) =>
            {
                // 分页容器几何变化 → 里面的子控件跟着动 / 跟着缩放（类似 VS2022 设计器容器联动）
                if (model.WidgetType == "分页" && Owner != null)
                {
                    switch (e.PropertyName)
                    {
                        case nameof(DesignerWidget.X):
                            MoveTabChildren(Model.X - _lastX, 0); _lastX = Model.X; break;
                        case nameof(DesignerWidget.Y):
                            MoveTabChildren(0, Model.Y - _lastY); _lastY = Model.Y; break;
                        case nameof(DesignerWidget.Width):
                            ScaleTabChildren(Model.Width / _lastW, 1); _lastW = Model.Width; break;
                        case nameof(DesignerWidget.Height):
                            ScaleTabChildren(1, Model.Height / _lastH); _lastH = Model.Height; break;
                    }
                }

                if (e.PropertyName != nameof(DesignerWidget.Param)) return;
                OnPropertyChanged(nameof(TabPageList));
                RebuildTabPages();
                var pages = TabPageList;
                if (!pages.Contains(_selectedPage)) SelectedPage = pages[0];
            };
        }

        // 分页容器的上次几何（用于算位移 / 缩放比例）
        private double _lastX, _lastY, _lastW, _lastH;

        /// <summary>分页被拖动：名下全部子控件（所有页）跟着平移同样的距离。</summary>
        private void MoveTabChildren(double dx, double dy)
        {
            if (Owner == null || (dx == 0 && dy == 0)) return;
            foreach (var vm in Owner.Widgets)
            {
                var m = vm.Model;
                if (m.WidgetType == "分页" || m.TabName != Model.Name) continue;
                if (dx != 0) m.X = System.Math.Max(0, System.Math.Round(m.X + dx));
                if (dy != 0) m.Y = System.Math.Max(0, System.Math.Round(m.Y + dy));
            }
        }

        /// <summary>分页被缩放：名下全部子控件按比例缩放位置和大小（锚点 = 分页左上角）。</summary>
        private void ScaleTabChildren(double sx, double sy)
        {
            if (Owner == null) return;
            if (sx <= 0 || !double.IsFinite(sx)) sx = 1;
            if (sy <= 0 || !double.IsFinite(sy)) sy = 1;
            if (sx == 1 && sy == 1) return;
            foreach (var vm in Owner.Widgets)
            {
                var m = vm.Model;
                if (m.WidgetType == "分页" || m.TabName != Model.Name) continue;
                if (sx != 1)
                {
                    m.X = System.Math.Max(0, System.Math.Round(Model.X + (m.X - Model.X) * sx));
                    m.Width = System.Math.Max(40, System.Math.Round(m.Width * sx));
                }
                if (sy != 1)
                {
                    m.Y = System.Math.Max(0, System.Math.Round(Model.Y + (m.Y - Model.Y) * sy));
                    m.Height = System.Math.Max(24, System.Math.Round(m.Height * sy));
                }
            }
        }

        /// <summary>分页控件的页列表（属性面板「页面管理」用，可改名/删除）。</summary>
        public ObservableCollection<PageTabVM> TabPages { get; } = new();

        private void RebuildTabPages()
        {
            TabPages.Clear();
            foreach (var p in TabPageList)
                TabPages.Add(new PageTabVM(this, p));
        }
    }

    /// <summary>分页控件属性面板里的一行页名（TextBox 改名 → 同步 Param 与子控件归属）。</summary>
    public class PageTabVM : ViewModelBase
    {
        private readonly DesignerWidgetVM _w;
        private string _name;

        public PageTabVM(DesignerWidgetVM w, string name) { _w = w; _name = name; }

        public string Name
        {
            get => _name;
            set
            {
                var n = (value ?? "").Trim();
                var old = _name;
                if (n.Length == 0 || n == old) { OnPropertyChanged(nameof(Name)); return; }
                if (_w.TabPageList.Contains(n)) { OnPropertyChanged(nameof(Name)); return; }  // 与其它页重名 → 忽略
                _name = n;
                _w.RenamePage(old, n);
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    /// <summary>
    /// 自定义页面（可视化设计器）ViewModel：左侧工具箱拖控件 → 中间画布摆位置/大小 →
    /// 右侧属性面板配动作（写 IO / 气缸 / 轴移动 / 变量…）。控件随工程存进 xlsx 的「自定义」表。
    /// <para>运行时动作全部经 <see cref="HardwareBridge.Current"/> 真实下发，显示框由页面定时器读回真实值。</para>
    /// </summary>
    public class CustomDesignerViewModel : ViewModelBase
    {
        /// <summary>画布上的控件（与 <see cref="ProjectStore.Data"/>.DesignerWidgets 同步维护）。</summary>
        public ObservableCollection<DesignerWidgetVM> Widgets { get; } = new();

        /// <summary>左侧工具箱可拖出的控件类型。</summary>
        public ObservableCollection<string> ToolboxTypes { get; } = new() { "按钮", "输入框", "显示框", "标签", "分页" };

        /// <summary>按钮可用的动作。</summary>
        public static readonly string[] ButtonActions =
            { "无", "写输出IO", "气缸", "轴移动(绝对)", "轴移动(相对)", "轴回原", "轴停止", "写变量" };

        /// <summary>输入框可用的动作。</summary>
        public static readonly string[] InputActions = { "无", "写变量" };

        /// <summary>显示框可显示的内容。</summary>
        public static readonly string[] DisplayActions =
            { "显示IO", "显示气缸", "显示变量", "显示轴位置", "显示轴参数", "无" };

        /// <summary>「显示轴参数」的参数取值。</summary>
        public static readonly string[] AxisParamNames =
            { "速度", "加速时间", "减速时间", "点动距离", "手动速度" };

        private DesignerWidgetVM? _selected;
        /// <summary>当前选中的控件（画布高亮 + 右侧属性面板）。</summary>
        public DesignerWidgetVM? SelectedWidget
        {
            get => _selected;
            set
            {
                var old = _selected;
                if (!SetField(ref _selected, value)) return;
                if (old?.Model != null) old.Model.PropertyChanged -= OnSelectedModelChanged;
                if (value?.Model != null) value.Model.PropertyChanged += OnSelectedModelChanged;
                foreach (var w in Widgets)
                    w.IsSelected = ReferenceEquals(w, value);
                OnPropertyChanged(nameof(ActionOptions));
                OnPropertyChanged(nameof(TargetOptions));
                OnPropertyChanged(nameof(ParamOptions));
                OnPropertyChanged(nameof(ShowTabPageProps));
                OnPropertyChanged(nameof(ShowPageEditor));
                OnPropertyChanged(nameof(ShowParamInput));
                OnPropertyChanged(nameof(TabOptions));
                OnPropertyChanged(nameof(TabPageOptions));
            }
        }

        /// <summary>选中控件的属性变化 → 联动刷新参数候选 / 目标候选 / 分页显隐，不合候选的旧参数收敛成默认值。</summary>
        private void OnSelectedModelChanged(object? sender, PropertyChangedEventArgs e)
        {
            var m = SelectedWidget?.Model;
            if (e.PropertyName == nameof(DesignerWidget.Action))
            {
                var opts = ParamOptions;
                if (m != null && opts != null && !opts.Contains(m.Param))
                    m.Param = opts[0];
                OnPropertyChanged(nameof(ParamOptions));
                OnPropertyChanged(nameof(TargetOptions));
            }
            else if (e.PropertyName is nameof(DesignerWidget.TabName) or nameof(DesignerWidget.Name)
                     or nameof(DesignerWidget.Param))
            {
                OnPropertyChanged(nameof(TabPageOptions));
                UpdateTabVisibility();
            }
        }

        /// <summary>重算所有控件的分页显隐：挂在分页下的控件只在其对应页被选中时可见（分页控件本身与主画布控件恒可见）。</summary>
        public void UpdateTabVisibility()
        {
            foreach (var vm in Widgets)
            {
                var m = vm.Model;
                if (m.WidgetType == "分页") { vm.IsTabActive = true; continue; }
                if (string.IsNullOrEmpty(m.TabName)) { vm.IsTabActive = true; continue; }
                var tab = Widgets.FirstOrDefault(w => w.Model.WidgetType == "分页" && w.Model.Name == m.TabName);
                // 分页控件不存在（已删/改名）→ 回落为可见，避免控件凭空消失
                vm.IsTabActive = tab == null || string.IsNullOrEmpty(m.PageName) || tab.SelectedPage == m.PageName;
            }
        }

        private bool _isDesign = true;
        /// <summary>true = 设计模式（拖拽摆布局，控件本身不可点）；false = 运行模式（按钮可点、输入框可输）。</summary>
        public bool IsDesign
        {
            get => _isDesign;
            set { if (SetField(ref _isDesign, value)) OnPropertyChanged(nameof(IsRunMode)); }
        }

        /// <summary>运行模式（给 XAML 绑 IsHitTestVisible 用）。</summary>
        public bool IsRunMode => !_isDesign;

        /// <summary>属性面板「动作」下拉的候选项（按选中控件的类型切换）。</summary>
        public IReadOnlyList<string> ActionOptions
        {
            get
            {
                var t = SelectedWidget?.Model.WidgetType;
                if (t == "输入框") return InputActions;
                if (t == "显示框") return DisplayActions;
                if (t == "标签" || t == "分页") return new[] { "无" };   // 标签纯显示、分页是容器，均无动作
                return ButtonActions;
            }
        }

        /// <summary>属性面板「目标」下拉的候选名（按动作切换：IO 名 / 气缸名 / 变量名 / 轴名）。</summary>
        public IReadOnlyList<string> TargetOptions
        {
            get
            {
                var data = ProjectStore.Data;
                var m = SelectedWidget?.Model;
                if (data == null || m == null) return Array.Empty<string>();
                switch (m.Action)
                {
                    case "写输出IO":
                        return data.Outputs.Select(i => i.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();
                    case "显示IO":
                        return data.Outputs.Concat(data.Inputs).Select(i => i.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();
                    case "气缸":
                    case "显示气缸":
                        return data.Cylinders.Select(i => i.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();
                    case "写变量":
                    case "显示变量":
                        return data.Variables.SelectMany(r => r.Names()).Distinct().ToList();
                    case "轴移动(绝对)":
                    case "轴移动(相对)":
                    case "轴回原":
                    case "轴停止":
                    case "显示轴位置":
                    case "显示轴参数":
                        return data.Axes.Select(i => i.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();
                    default:
                        return Array.Empty<string>();
                }
            }
        }

        /// <summary>
        /// 属性面板「参数」的固定候选（按动作）；<c>null</c> = 该动作参数自由输入（显示输入框）。
        /// 写输出IO → 开/关/切换；气缸 → 伸出/缩回/切换；显示轴参数 → 速度/加速时间/…；分页 → 页名用「|」分隔。
        /// </summary>
        public IReadOnlyList<string>? ParamOptions
        {
            get
            {
                return SelectedWidget?.Model.WidgetType == "分页"
                    ? null
                    : SelectedWidget?.Model.Action switch
                    {
                        "写输出IO" => new[] { "开", "关", "切换" },
                        "气缸" => new[] { "伸出", "缩回", "切换" },
                        "显示轴参数" => AxisParamNames,
                        _ => null,
                    };
            }
        }

        /// <summary>非分页控件才有「所属分页/页面」属性（分页本身直接放主画布）。</summary>
        public bool ShowTabPageProps => SelectedWidget?.Model.WidgetType != "分页";

        /// <summary>选中分页控件 → 显示「页面管理」（添加/删除/重命名页标签）。</summary>
        public bool ShowPageEditor => SelectedWidget?.Model.WidgetType == "分页";

        /// <summary>非分页控件才显示「参数」行（分页的页名表由页面管理维护）。</summary>
        public bool ShowParamInput => SelectedWidget?.Model.WidgetType != "分页";

        /// <summary>「所属分页」候选：空串（主画布）+ 画布上全部分页控件名。</summary>
        public IReadOnlyList<string> TabOptions
        {
            get
            {
                var names = Widgets.Where(w => w.Model.WidgetType == "分页")
                    .Select(w => w.Model.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();
                names.Insert(0, "");
                return names;
            }
        }

        /// <summary>「所属页面」候选：所属分页控件的页名列表。</summary>
        public IReadOnlyList<string> TabPageOptions
        {
            get
            {
                var m = SelectedWidget?.Model;
                if (m == null || string.IsNullOrEmpty(m.TabName)) return Array.Empty<string>();
                var tab = Widgets.FirstOrDefault(w => w.Model.WidgetType == "分页" && w.Model.Name == m.TabName);
                return tab?.TabPageList.ToArray() ?? Array.Empty<string>();
            }
        }

        public ICommand DeleteSelectedCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ImportCommand { get; }

        public CustomDesignerViewModel()
        {
            ReloadFromStore();
            DeleteSelectedCommand = new RelayCommand(_ => DeleteSelected());
            ClearAllCommand = new RelayCommand(_ => ClearAll());
            ExportCommand = new RelayCommand(_ => ExportWidgets());
            ImportCommand = new RelayCommand(_ => ImportWidgets());
        }

        /// <summary>从工程数据重建包装列表（页面构造 / 清空 / 导入后重载时调用）。</summary>
        private void ReloadFromStore()
        {
            Widgets.Clear();
            foreach (var m in ProjectStore.Data.DesignerWidgets)
                if (m != null) Widgets.Add(new DesignerWidgetVM(m) { Owner = this });
            UpdateTabVisibility();
        }

        // ===== 增删 =====

        /// <summary>在指定位置添加一个控件（工具箱拖入画布时调用），返回新控件。</summary>
        public DesignerWidgetVM AddWidget(string type, double x, double y)
        {
            var data = ProjectStore.Data;
            int n = data.DesignerWidgets.Count(w => w != null && w.WidgetType == type) + 1;
            var m = new DesignerWidget
            {
                Name = $"{type}{n}",
                WidgetType = type,
                X = Math.Max(0, x),
                Y = Math.Max(0, y),
                Text = $"{type}{n}",
            };
            switch (type)
            {
                case "输入框":
                    m.Action = "写变量"; m.Width = 150; m.Height = 34; break;
                case "显示框":
                    m.Action = "显示变量"; m.Width = 180; m.Height = 40; break;
                case "标签":
                    m.Action = "无"; m.Text = $"标签{n}"; m.Width = 120; m.Height = 30; break;
                case "分页":
                    m.Action = "无"; m.Param = "页1|页2"; m.Width = 420; m.Height = 320; break;
                default:
                    m.Action = "写输出IO"; m.Width = 130; m.Height = 40; break;
            }
            data.DesignerWidgets.Add(m);
            var vm = new DesignerWidgetVM(m) { Owner = this };
            if (type == "分页")
            {
                // 分页是容器：插到最底层（先加入先画在下层），避免盖住已拖入的控件
                var idx = data.DesignerWidgets.IndexOf(m);
                data.DesignerWidgets.Move(idx, 0);
                Widgets.Insert(0, vm);
            }
            else
            {
                Widgets.Add(vm);
            }
            SelectedWidget = vm;
            UpdateTabVisibility();
            return vm;
        }

        /// <summary>删除当前选中控件（画布 Delete 键 / 属性面板「删除」按钮）。</summary>
        public void DeleteSelected()
        {
            var vm = SelectedWidget;
            if (vm == null) return;
            ProjectStore.Data.DesignerWidgets.Remove(vm.Model);
            Widgets.Remove(vm);
            SelectedWidget = null;
        }

        /// <summary>清空画布。</summary>
        public void ClearAll()
        {
            ProjectStore.Data.DesignerWidgets.Clear();
            Widgets.Clear();
            SelectedWidget = null;
        }

        // ===== 导入 / 导出（控件布局存 JSON 文件，便于跨工程复用） =====

        private static readonly JsonSerializerOptions _jsonOpt = new() { WriteIndented = true };

        /// <summary>导出当前画布全部控件为 JSON 文件（SaveFileDialog 选择保存位置）。</summary>
        private void ExportWidgets()
        {
            try
            {
                var items = ProjectStore.Data.DesignerWidgets.Where(w => w != null).ToList();
                if (items.Count == 0) { Warn("画布上没有控件，先拖几个再导出。"); return; }

                var dlg = new SaveFileDialog
                {
                    Title = "导出自定义页面",
                    Filter = "自定义页面 (*.json)|*.json|所有文件 (*.*)|*.*",
                    FileName = "自定义页面.json",
                    OverwritePrompt = true,
                    AddExtension = true
                };
                if (dlg.ShowDialog() != true) return;

                File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(items, _jsonOpt));
                StatusBarService.ReportInfo($"[自定义] 已导出 {items.Count} 个控件到 {Path.GetFileName(dlg.FileName)}。");
            }
            catch (Exception ex)
            {
                StatusBarService.ReportException($"[自定义] 导出失败：{ex.Message}");
            }
        }

        /// <summary>从 JSON 文件导入控件布局（替换当前画布全部控件）。</summary>
        private void ImportWidgets()
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Title = "导入自定义页面",
                    Filter = "自定义页面 (*.json)|*.json|所有文件 (*.*)|*.*"
                };
                if (dlg.ShowDialog() != true) return;

                var imported = JsonSerializer.Deserialize<List<DesignerWidget>>(File.ReadAllText(dlg.FileName));
                if (imported == null || imported.Count == 0) { Warn($"[自定义] 文件「{Path.GetFileName(dlg.FileName)}」里没有控件。"); return; }

                var store = ProjectStore.Data.DesignerWidgets;
                store.Clear();
                foreach (var w in imported)
                    if (w != null) store.Add(w);

                ReloadFromStore();
                StatusBarService.ReportInfo($"[自定义] 已从 {Path.GetFileName(dlg.FileName)} 导入 {store.Count} 个控件（原画布内容已被替换）。");
            }
            catch (Exception ex)
            {
                StatusBarService.ReportException($"[自定义] 导入失败：{ex.Message}（请确认选择的是「导出」生成的 JSON 文件）");
            }
        }

        // ===== 运行时：按钮 / 输入框动作（真实下发硬件） =====

        /// <summary>执行按钮动作（按钮 Click 时调用）。</summary>
        public void Execute(DesignerWidget? m)
        {
            if (m == null || m.Action == "无") return;
            var bridge = HardwareBridge.Current;
            try
            {
                switch (m.Action)
                {
                    case "写输出IO":
                    {
                        var io = FindIo(m.Target);
                        if (io == null) { Warn($"输出「{m.Target}」不存在，请检查属性面板的目标名。"); return; }
                        int value = m.Param switch
                        {
                            "关" => 0,
                            "切换" => ((int)bridge.ReadOutput(io) != 0) ? 0 : 1,
                            _ => 1,
                        };
                        bridge.WriteOutput(io, value);
                        SimRuntime.SetOutput(io.Name, value);
                        StatusBarService.ReportInfo($"[自定义] 输出「{io.Name}」→ {(value != 0 ? "开" : "关")}");
                        break;
                    }
                    case "气缸":
                    {
                        var cyl = ProjectStore.Data.Cylinders.FirstOrDefault(c => c.Name == m.Target);
                        if (cyl == null) { Warn($"气缸「{m.Target}」不存在，请检查属性面板的目标名。"); return; }
                        int state = m.Param == "缩回" ? 0
                                  : m.Param == "切换" ? (SimRuntime.GetCylinder(cyl.Name) != 0 ? 0 : 1)
                                  : 1;
                        bridge.CylinderMove(cyl, state);
                        SimRuntime.SetCylinder(cyl.Name, state);
                        cyl.CurrentState = state != 0 ? "伸出" : "缩回";
                        StatusBarService.ReportInfo($"[自定义] 气缸「{cyl.Name}」→ {(state != 0 ? "伸出" : "缩回")}");
                        break;
                    }
                    case "轴移动(绝对)":
                    {
                        var axis = FindAxis(m.Target);
                        if (axis == null) { Warn($"轴「{m.Target}」不存在。"); return; }
                        if (!TryNum(m.Param, out double pos)) { Warn($"轴「{axis.Name}」目标位置「{m.Param}」不是数字。"); return; }
                        bridge.MoveAxisAbs(axis, pos);
                        StatusBarService.ReportInfo($"[自定义] 轴「{axis.Name}」绝对移动 → {pos:0.###}");
                        break;
                    }
                    case "轴移动(相对)":
                    {
                        var axis = FindAxis(m.Target);
                        if (axis == null) { Warn($"轴「{m.Target}」不存在。"); return; }
                        if (!TryNum(m.Param, out double dist)) { Warn($"轴「{axis.Name}」移动距离「{m.Param}」不是数字。"); return; }
                        bridge.MoveAxisRel(axis, dist);
                        StatusBarService.ReportInfo($"[自定义] 轴「{axis.Name}」相对移动 {dist:0.###}");
                        break;
                    }
                    case "轴回原":
                    {
                        var axis = FindAxis(m.Target);
                        if (axis == null) { Warn($"轴「{m.Target}」不存在。"); return; }
                        bridge.HomeAxis(axis);
                        StatusBarService.ReportInfo($"[自定义] 轴「{axis.Name}」回原已下发");
                        break;
                    }
                    case "轴停止":
                    {
                        var axis = FindAxis(m.Target);
                        if (axis == null) { Warn($"轴「{m.Target}」不存在。"); return; }
                        bridge.StopAxis(axis);
                        StatusBarService.ReportInfo($"[自定义] 轴「{axis.Name}」已停止");
                        break;
                    }
                    case "写变量":
                    {
                        if (string.IsNullOrWhiteSpace(m.Target)) { Warn("请先在属性面板选择要写的变量。"); return; }
                        if (!TryNum(m.Param, out double v)) { Warn($"变量「{m.Target}」的写入值「{m.Param}」不是数字。"); return; }
                        SimRuntime.SetVariable(m.Target, v);
                        StatusBarService.ReportInfo($"[自定义] 变量「{m.Target}」= {v:0.###}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                StatusBarService.ReportException($"[自定义] 「{m.Name}」动作失败：{ex.Message}");
            }
        }

        /// <summary>输入框提交（回车 / 失焦）：把输入值写进绑定的变量。</summary>
        public void CommitInput(DesignerWidget? m, string text)
        {
            if (m == null || m.Action != "写变量") return;
            if (string.IsNullOrWhiteSpace(m.Target)) { Warn("请先在属性面板选择要写的变量。"); return; }
            if (!TryNum(text, out double v)) { Warn($"变量「{m.Target}」的输入值「{text}」不是数字。"); return; }
            try
            {
                SimRuntime.SetVariable(m.Target, v);
                StatusBarService.ReportInfo($"[自定义] 变量「{m.Target}」= {v:0.###}");
            }
            catch (Exception ex)
            {
                StatusBarService.ReportException($"[自定义] 写变量失败：{ex.Message}");
            }
        }

        // ===== 运行时：显示框读回（页面 250ms 定时器调用） =====

        /// <summary>刷新所有显示框的实时文本（只动 LiveText，不碰布局数据）。</summary>
        public void RefreshLive()
        {
            foreach (var vm in Widgets)
            {
                if (vm.Model.WidgetType != "显示框") continue;
                vm.LiveText = ComputeLive(vm.Model);
            }
        }

        private string ComputeLive(DesignerWidget m)
        {
            if (string.IsNullOrWhiteSpace(m.Target)) return m.Action == "无" ? (m.Text ?? "") : "未选目标";
            try
            {
                switch (m.Action)
                {
                    case "显示IO":
                    {
                        var io = FindIo(m.Target);
                        if (io == null) return "未找到";
                        double v = ProjectStore.Data.Outputs.Contains(io)
                            ? HardwareBridge.Current.ReadOutput(io)
                            : HardwareBridge.Current.ReadInput(io);
                        return v != 0 ? "高" : "低";
                    }
                    case "显示气缸":
                    {
                        var cyl = ProjectStore.Data.Cylinders.FirstOrDefault(c => c.Name == m.Target);
                        if (cyl == null) return "未找到";
                        // 优先读真实传感器（配置了伸/缩感应），没有就回放仿真状态
                        var ext = FindIo(cyl.SensorExtend);
                        if (ext != null)
                        {
                            if ((int)HardwareBridge.Current.ReadInput(ext) == 1) return "伸出";
                            var ret = FindIo(cyl.SensorRetract);
                            if (ret != null && (int)HardwareBridge.Current.ReadInput(ret) == 1) return "缩回";
                            return "运动中";
                        }
                        return SimRuntime.GetCylinder(cyl.Name) != 0 ? "伸出" : "缩回";
                    }
                    case "显示变量":
                        return SimRuntime.GetVariableResolved(m.Target).ToString("0.###", CultureInfo.InvariantCulture);
                    case "显示轴位置":
                    {
                        var axis = FindAxis(m.Target);
                        if (axis == null) return "未找到";
                        return HardwareBridge.Current.ReadAxisPosition(axis).ToString("0.###");
                    }
                    case "显示轴参数":
                    {
                        var axis = FindAxis(m.Target);
                        if (axis == null) return "未找到";
                        return m.Param switch
                        {
                            "加速时间" => $"{axis.Accel:0.###} s",
                            "减速时间" => $"{axis.Decel:0.###} s",
                            "点动距离" => $"{axis.JogStep:0.###}",
                            "手动速度" => $"{axis.ManualSpeed:0.###}",
                            _ => $"{axis.Speed:0.###}",   // 默认显示运行速度
                        };
                    }
                    default:
                        return m.Text ?? "";
                }
            }
            catch
            {
                return "读取失败";
            }
        }

        // ===== 查找辅助 =====

        private static IoItem? FindIo(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var data = ProjectStore.Data;
            return data.Outputs.FirstOrDefault(i => i.Name == name)
                ?? data.Inputs.FirstOrDefault(i => i.Name == name);
        }

        private static AxisItem? FindAxis(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return ProjectStore.Data.Axes.FirstOrDefault(a => a.Name == name);
        }

        private static bool TryNum(string? s, out double v)
            => double.TryParse(s?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out v);

        private static void Warn(string msg) => StatusBarService.ReportException("[自定义] " + msg);
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
