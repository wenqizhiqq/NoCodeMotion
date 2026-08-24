using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 通用表格工具栏（所有表格页都用它）：Excel编辑 / 添加 / 删除 / 上移 / 下移 / 复制 / 粘贴 / 回撤 / 重做 + 搜索(定位/导航)/上一条/下一条/关闭按钮。
    /// 命令（ExcelEditCommand / AddCommand / ... / RedoCommand）通过继承的 DataContext 自动绑定，
    /// 复用到任意表格面板（IO输入/输出、变量、流程步骤……）：把 TableToolbar 放进 DataContext 是正确 ViewModel 的区域即可。
    /// 搜索为“定位/导航”语义（不隐藏任何行）：输入关键字后算出所有匹配项，用上一条/下一条在匹配项间跳转并滚动到可视区；
    /// TargetGrid 缺省时尝试沿可视化树向上查找最近 DataGrid。
    /// </summary>
    public partial class TableToolbar : UserControl
    {
        public TableToolbar() { InitializeComponent(); Loaded += OnLoaded; }

        // ===== 原有依赖项属性 =====
        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register(nameof(Count), typeof(int), typeof(TableToolbar), new PropertyMetadata(0));
        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        public static readonly DependencyProperty CountLabelProperty =
            DependencyProperty.Register(nameof(CountLabel), typeof(string), typeof(TableToolbar), new PropertyMetadata("项"));
        public string CountLabel
        {
            get => (string)GetValue(CountLabelProperty);
            set => SetValue(CountLabelProperty, value);
        }

        // ===== 搜索相关依赖项属性 =====
        public static readonly DependencyProperty TargetGridProperty =
            DependencyProperty.Register(nameof(TargetGrid), typeof(DataGrid), typeof(TableToolbar));
        /// <summary>搜索/导航目标的 DataGrid；缺省时自动沿可视化树查找最近一个。</summary>
        public DataGrid TargetGrid
        {
            get => (DataGrid)GetValue(TargetGridProperty);
            set => SetValue(TargetGridProperty, value);
        }

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(TableToolbar),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSearchTextChanged));
        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public static readonly DependencyProperty SearchPlaceholderProperty =
            DependencyProperty.Register(nameof(SearchPlaceholder), typeof(string), typeof(TableToolbar), new PropertyMetadata("搜索…"));
        public string SearchPlaceholder
        {
            get => (string)GetValue(SearchPlaceholderProperty);
            set => SetValue(SearchPlaceholderProperty, value);
        }

        public static readonly DependencyProperty MatchInfoProperty =
            DependencyProperty.Register(nameof(MatchInfo), typeof(string), typeof(TableToolbar), new PropertyMetadata(string.Empty));
        public string? MatchInfo
        {
            get => (string?)GetValue(MatchInfoProperty);
            set => SetValue(MatchInfoProperty, value ?? string.Empty);
        }

        // 当前匹配项（原始集合引用），不隐藏任何行
        private readonly List<object> _matches = new();
        private int _matchPos = -1;
        private string _lastKeyword = "";

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TableToolbar tb) tb.ApplySearch();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var nearest = FindNearestDataGrid(this);
            if (nearest != null) TargetGrid = nearest;
            UpdateMatchInfo();
        }

        private void ApplySearch()
        {
            _lastKeyword = SearchText ?? "";
            _matches.Clear();
            _matchPos = -1;

            if (TargetGrid != null && !string.IsNullOrEmpty(_lastKeyword))
            {
                if (TargetGrid.ItemsSource is IEnumerable src)
                {
                    foreach (var item in src)
                    {
                        if (ItemMatches(item, _lastKeyword)) _matches.Add(item!);
                    }
                }
                if (_matches.Count > 0)
                {
                    _matchPos = 0;
                    SelectMatch(_matchPos);
                }
            }
            UpdateMatchInfo();
        }

        private static bool ItemMatches(object o, string kw)
        {
            if (o == null) return false;
            // 模型未重写 ToString()，故改为拼接所有可读属性的文本进行匹配，
            // 使按任意列（名称/值/类型…）搜索都能命中。
            return GetSearchableText(o).IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetSearchableText(object o)
        {
            if (o == null) return "";
            var sb = new StringBuilder();
            try
            {
                var t = o.GetType();
                foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!prop.CanRead) continue;
                    if (prop.GetIndexParameters().Length > 0) continue;
                    object? v;
                    try { v = prop.GetValue(o); }
                    catch { continue; }
                    if (v == null) continue;
                    sb.Append(v.ToString()).Append(' ');
                }
            }
            catch { }
            return sb.ToString();
        }

        private void SelectMatch(int pos)
        {
            if (TargetGrid == null || pos < 0 || pos >= _matches.Count) return;
            var item = _matches[pos];
            TargetGrid.SelectedItem = item;
            TargetGrid.ScrollIntoView(item);
        }

        private void UpdateMatchInfo()
        {
            if (TargetGrid == null) { MatchInfo = ""; return; }
            int total = -1;
            if (TargetGrid.ItemsSource is IEnumerable src)
            {
                if (src is ICollection col) total = col.Count;
                else { int n = 0; foreach (var _ in src) n++; total = n; }
            }

            if (string.IsNullOrEmpty(_lastKeyword))
            {
                MatchInfo = total < 0 ? "" : $"共 {total} {CountLabel}";
                return;
            }
            if (_matches.Count == 0) MatchInfo = "无匹配";
            else MatchInfo = $"第 {_matchPos + 1}/{_matches.Count} 个匹配";
        }

        private void MoveSelection(int delta)
        {
            if (_matches.Count == 0) return;
            _matchPos = (_matchPos + delta) % _matches.Count;
            if (_matchPos < 0) _matchPos += _matches.Count;
            SelectMatch(_matchPos);
            UpdateMatchInfo();
        }

        private void BtnPrevSearch_Click(object sender, RoutedEventArgs e) => MoveSelection(-1);
        private void BtnNextSearch_Click(object sender, RoutedEventArgs e) => MoveSelection(+1);
        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchText = "";
            if (TargetGrid != null) TargetGrid.SelectedItem = null;
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) MoveSelection(-1);
                else MoveSelection(+1);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                SearchText = "";
                e.Handled = true;
            }
        }

        private static DataGrid? FindNearestDataGrid(DependencyObject start)
        {
            DependencyObject? node = start;
            while (node != null)
            {
                var child = FindFirstDataGrid(node);
                if (child != null) return child;
                node = VisualTreeHelper.GetParent(node);
            }
            return null;
        }

        private static DataGrid? FindFirstDataGrid(DependencyObject root)
        {
            int n = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < n; i++)
            {
                var c = VisualTreeHelper.GetChild(root, i);
                if (c is DataGrid g) return g;
                var sub = FindFirstDataGrid(c);
                if (sub != null) return sub;
            }
            return null;
        }
    }
}
