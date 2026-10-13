using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 轴IO标定对话框：为每个启用的轴选择 正限位 / 负限位 / 原点开关（输入IO）与 轴使能（输出IO），
    /// 结果写入 AxisItem 的 Io* 字段（随工程 xlsx 持久化），供 3D 仿真生成传感器显示。
    /// </summary>
    public partial class AxisIoCalibDialog : Window
    {
        private const string Unbound = "（未绑定）";

        private readonly List<(AxisItem Axis, ComboBox Plus, ComboBox Minus, ComboBox Origin, ComboBox Enable)> _rows = new();

        public AxisIoCalibDialog()
        {
            InitializeComponent();
            BuildRows();
        }

        private void BuildRows()
        {
            var data = ProjectStore.Data;
            var inputs = data?.Inputs ?? new System.Collections.ObjectModel.ObservableCollection<IoItem>();
            var outputs = data?.Outputs ?? new System.Collections.ObjectModel.ObservableCollection<IoItem>();

            string[] headers = { "轴名", "正限位", "负限位", "原点开关", "轴使能" };
            for (int c = 0; c < headers.Length; c++)
                AddCell(MakeHeader(headers[c]), c, 0);

            int r = 1;
            foreach (var axis in data?.Axes?.Where(a => a != null && a.Enabled) ?? Enumerable.Empty<AxisItem>())
            {
                RowsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                AddCell(MakeText(axis.Name, FontWeights.SemiBold), 0, r);

                var cbPlus = MakeCombo(inputs);
                Select(cbPlus, inputs, axis.IoLimitPlus);
                var cbMinus = MakeCombo(inputs);
                Select(cbMinus, inputs, axis.IoLimitMinus);
                var cbOrigin = MakeCombo(inputs);
                Select(cbOrigin, inputs, axis.IoOrigin);
                var cbEnable = MakeCombo(outputs);
                Select(cbEnable, outputs, axis.IoEnable);

                AddCell(cbPlus, 1, r);
                AddCell(cbMinus, 2, r);
                AddCell(cbOrigin, 3, r);
                AddCell(cbEnable, 4, r);

                _rows.Add((axis, cbPlus, cbMinus, cbOrigin, cbEnable));
                r++;
            }

            if (r == 1)
                AddCell(MakeText("工程中没有启用的轴，请先在轴页面添加。", FontWeights.Normal, (Brush)Application.Current.FindResource("TextMutedBrush")), 0, 1);
        }

        private void AddCell(FrameworkElement el, int col, int row)
        {
            Grid.SetColumn(el, col);
            Grid.SetRow(el, row);
            if (el is ComboBox cb) cb.Margin = new Thickness(0, 3, 8, 3);
            else el.Margin = new Thickness(0, 6, 8, 6);
            RowsGrid.Children.Add(el);
        }

        private static TextBlock MakeHeader(string text) =>
            new()
            {
                Text = text,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.FindResource("TextSecondaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };

        private static TextBlock MakeText(string text, FontWeight weight, Brush? fg = null) =>
            new()
            {
                Text = text,
                FontSize = 12.5,
                FontWeight = weight,
                Foreground = fg ?? (Brush)Application.Current.FindResource("TextPrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

        private static ComboBox MakeCombo(System.Collections.ObjectModel.ObservableCollection<IoItem> items)
        {
            var names = new List<string> { Unbound };
            foreach (var io in items)
                if (io != null && !string.IsNullOrWhiteSpace(io.Name))
                    names.Add(io.Name);
            return new ComboBox
            {
                ItemsSource = names,
                Style = (Style)Application.Current.FindResource("CellComboStyle"),
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private static void Select(ComboBox cb, System.Collections.ObjectModel.ObservableCollection<IoItem> items, string bound)
        {
            if (!string.IsNullOrWhiteSpace(bound) && items.Any(io => io != null && io.Name == bound))
                cb.SelectedItem = bound;
            else
                cb.SelectedItem = Unbound;
        }

        private static string Pick(ComboBox cb, System.Collections.ObjectModel.ObservableCollection<IoItem>? items)
        {
            var sel = cb.SelectedItem as string;
            if (string.IsNullOrEmpty(sel) || sel == Unbound || items == null) return string.Empty;
            // 名称不存在（IO 被删）→ 保存为未绑定
            return items.Any(io => io != null && io.Name == sel) ? sel : string.Empty;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var data = ProjectStore.Data;
            var inputs = data?.Inputs;
            var outputs = data?.Outputs;
            foreach (var row in _rows)
            {
                row.Axis.IoLimitPlus = Pick(row.Plus, inputs);
                row.Axis.IoLimitMinus = Pick(row.Minus, inputs);
                row.Axis.IoOrigin = Pick(row.Origin, inputs);
                row.Axis.IoEnable = Pick(row.Enable, outputs);
            }
            DialogResult = true;
            Close();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var row in _rows)
            {
                row.Plus.SelectedItem = Unbound;
                row.Minus.SelectedItem = Unbound;
                row.Origin.SelectedItem = Unbound;
                row.Enable.SelectedItem = Unbound;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
