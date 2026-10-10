// === NoCodeMotion 视觉标定结果导出为工程变量 | 作者：温​启‎志‍ ‍|⁣ ⁣微‏信‎：‏1‏8‌7‍1‌9‎3⁠6‏1​3​9‌9‌ ⁠|​ ​保‎留‍所⁠有‍权⁣利‏，⁣请‌勿‍删‎除 ===
using System.Globalization;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Vision.Calibration
{
    /// <summary>
    /// 把标定结果导出成工程变量（「变量」页可见，流程里可用 <c>{标定_像素当量}</c> 引用）。
    ///
    /// 名字统一以「标定_」开头，且**不带减号** —— 表达式求值器把 '-' 当减运算符，
    /// 名字里带减号的变量一旦进表达式就会被拆成减法（见项目笔记「变量与表达式」）。
    ///
    /// 说明：变量名会写进工程的变量表（空槽优先复用，不够才新增行），所以标定结果随工程落盘；
    /// 变量名下拉（Catalog）在工程载入时重建，新导出的名字下一次打开工程 / 载入后即可选到。
    /// </summary>
    public static class CalibVarExport
    {
        /// <summary>像素当量 mm/px（图像 +u 方向）。</summary>
        public const string VarPixelEquivalent = "标定_像素当量";

        /// <summary>图像 +u 方向在机台坐标下的方向角（度）。</summary>
        public const string VarAngleX = "标定_X方向角";

        /// <summary>图像 +v 方向在机台坐标下的方向角（度）。</summary>
        public const string VarAngleY = "标定_Y方向角";

        /// <summary>旋转中心机台 X（mm）。</summary>
        public const string VarRotCenterX = "标定_旋转中心X";

        /// <summary>旋转中心机台 Y（mm）。</summary>
        public const string VarRotCenterY = "标定_旋转中心Y";

        /// <summary>旋转中心图像 u（像素）。</summary>
        public const string VarRotCenterU = "标定_旋转中心U";

        /// <summary>旋转中心图像 v（像素）。</summary>
        public const string VarRotCenterV = "标定_旋转中心V";

        /// <summary>导出全部标定量；返回实际写入的变量个数（标定无效时返回 0）。</summary>
        public static int Export(CameraCalibration? rec)
        {
            if (rec == null || !rec.IsUsable) return 0;
            int n = 0;
            n += Put(VarPixelEquivalent, rec.PixelEquivalent);
            n += Put(VarAngleX, rec.AngleX);
            n += Put(VarAngleY, rec.AngleY);
            n += Put(VarRotCenterX, rec.RotCenterX);
            n += Put(VarRotCenterY, rec.RotCenterY);
            n += Put(VarRotCenterU, rec.RotCenterU);
            n += Put(VarRotCenterV, rec.RotCenterV);
            return n;
        }

        private static int Put(string name, double v)
        {
            if (!EnsureCell(name, out var row, out int col)) return 0;
            SetCellValue(row, col, v.ToString("0.#####", CultureInfo.InvariantCulture));
            // 同时写数值仓：SimRuntime 会回写同名的变量行，并通知界面刷新
            SimRuntime.SetVariable(name, v);
            return 1;
        }

        /// <summary>确保变量表里有名为 name 的单元（同名复用 → 空槽复用 → 新开一行），返回行与列号。</summary>
        private static bool EnsureCell(string name, out VariableRow row, out int col)
        {
            row = null!;
            col = 0;
            var vars = ProjectStore.Data?.Variables;
            if (vars == null) return false;

            foreach (var r in vars)
                for (int c = 1; c <= 5; c++)
                    if (string.Equals(CellName(r, c), name, StringComparison.OrdinalIgnoreCase))
                    {
                        row = r; col = c; return true;
                    }

            foreach (var r in vars)
                for (int c = 1; c <= 5; c++)
                    if (string.IsNullOrWhiteSpace(CellName(r, c)))
                    {
                        SetCellName(r, c, name);
                        row = r; col = c; return true;
                    }

            var nr = new VariableRow();
            SetCellName(nr, 1, name);
            vars.Add(nr);
            row = nr; col = 1;
            return true;
        }

        private static string CellName(VariableRow r, int c) => c switch
        {
            1 => r.Name1, 2 => r.Name2, 3 => r.Name3, 4 => r.Name4, 5 => r.Name5, _ => string.Empty
        };

        private static void SetCellName(VariableRow r, int c, string v)
        {
            switch (c)
            {
                case 1: r.Name1 = v; break;
                case 2: r.Name2 = v; break;
                case 3: r.Name3 = v; break;
                case 4: r.Name4 = v; break;
                case 5: r.Name5 = v; break;
            }
        }

        private static void SetCellValue(VariableRow r, int c, string v)
        {
            switch (c)
            {
                case 1: r.Value1 = v; break;
                case 2: r.Value2 = v; break;
                case 3: r.Value3 = v; break;
                case 4: r.Value4 = v; break;
                case 5: r.Value5 = v; break;
            }
        }
    }
}
// === NoCodeMotion 视觉标定结果导出为工程变量 | 作者：温⁣启⁠志‌ ‌|​ ​微⁠信​：‎1‎8‎7‎1⁠9‎3​6‏1‎3‎9⁠9⁠ ​|⁣ ‌保⁣留​所⁠有​权‍利‏，⁠请‎勿‏删‍除 ===
