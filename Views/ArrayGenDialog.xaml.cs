// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
using System.Globalization;
using System.Windows;
using NoCodeMotion.Services;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 生成阵列点位对话框：输入行数 / 列数 / 起始坐标 / 行间距 / 列间距 / 速度 / 命名前缀，
    /// 确认后由 PointViewModel 按「行优先」生成网格点位（X 随列递增、Y 随行递增）。
    /// </summary>
    public partial class ArrayGenDialog : Window
    {
        public int Rows { get; private set; } = 3;
        public int Cols { get; private set; } = 3;
        public double StartX { get; private set; }
        public double StartY { get; private set; }
        public double Dx { get; private set; } = 10;
        public double Dy { get; private set; } = 10;
        public double Speed { get; private set; } = 100;
        public string Prefix { get; private set; } = "P";

        public ArrayGenDialog()
        {
            InitializeComponent();
            Owner = Application.Current?.MainWindow;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseInt(RowsBox.Text, 1, 1000, out int rows) ||
                !TryParseInt(ColsBox.Text, 1, 1000, out int cols) ||
                !TryParseDouble(StartXBox.Text, out double sx) ||
                !TryParseDouble(StartYBox.Text, out double sy) ||
                !TryParseDouble(DxBox.Text, out double dx) ||
                !TryParseDouble(DyBox.Text, out double dy) ||
                !TryParseDouble(SpeedBox.Text, out double sp))
            {
                StatusBarService.ReportException("参数格式不正确：行数/列数为正整数，其余为数字。");
                return;
            }

            Rows = rows;
            Cols = cols;
            StartX = sx;
            StartY = sy;
            Dx = dx;
            Dy = dy;
            Speed = sp;
            Prefix = string.IsNullOrWhiteSpace(PrefixBox.Text) ? "P" : PrefixBox.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static bool TryParseInt(string? s, int min, int max, out int value)
        {
            value = 0;
            if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) return false;
            if (v < min || v > max) return false;
            value = v;
            return true;
        }

        private static bool TryParseDouble(string? s, out double value)
            => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
