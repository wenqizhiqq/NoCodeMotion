// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤ۦ▧▨۩░▒▓✦
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// =====================================================================
// 仿真运行时状态：与真实硬件完全解耦。3D 仿真播放器把流程步骤写到这里，
// Sim3DView 每帧读取并反映在气缸活塞伸缩、相机闪光等可视化上。
// 轴位置走 AxisRuntimeState（被参数化机台每帧读取刷新）。
// =====================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 静态仿真状态仓。键为 IO 输出名 / 气缸名 / 相机名 / 变量名（大小写不敏感）。
    /// 状态变化触发 Changed，供 3D 视图刷新可视化。
    /// </summary>
    public static class SimRuntime
    {
        private static readonly Dictionary<string, int> _outputs = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> _cylinders = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, DateTime> _camFlash = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, double> _variables = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>任意状态变化（IO 输出 / 气缸 / 相机闪光 / 变量）时触发，供 3D 视图订阅刷新。</summary>
        public static event Action? Changed;

        public static void Reset()
        {
            _outputs.Clear();
            _cylinders.Clear();
            _camFlash.Clear();
            _variables.Clear();
            // 变量页实时值复位为 0（仿真结束回到初始态）
            if (ProjectStore.Data?.Variables != null)
                foreach (var r in ProjectStore.Data.Variables)
                    for (int c = 1; c <= 5; c++) SetVarCell(r, c, "0");
            Changed?.Invoke();
        }

        // —— IO 输出 ——
        public static int GetOutput(string name)
            => _outputs.TryGetValue(name, out var v) ? v : 0;
        public static void SetOutput(string name, int value)
        {
            if (string.IsNullOrEmpty(name)) return;
            int v = value != 0 ? 1 : 0;
            if (!_outputs.TryGetValue(name, out var cur) || cur != v)
            {
                _outputs[name] = v;
                Changed?.Invoke();
            }
        }

        // —— 气缸 ——
        public static int GetCylinder(string name)
            => _cylinders.TryGetValue(name, out var v) ? v : 0;
        public static void SetCylinder(string name, int state)
        {
            if (string.IsNullOrEmpty(name)) return;
            int v = state != 0 ? 1 : 0;
            if (!_cylinders.TryGetValue(name, out var cur) || cur != v)
            {
                _cylinders[name] = v;
                Changed?.Invoke();
            }
        }

        // —— 相机闪光（记录最近一次触发时刻，由视图按时间衰减还原）——
        public static DateTime GetCamFlash(string name)
            => _camFlash.TryGetValue(name, out var t) ? t : DateTime.MinValue;
        public static void FlashCamera(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            _camFlash[name] = DateTime.Now;
            Changed?.Invoke();
        }

        // —— 变量（与变量页 VariableRow 实时双向：仿真写回，页面显示）——
        public static double GetVariable(string name)
            => _variables.TryGetValue(name, out var v) ? v : 0;

        /// <summary>解析变量值：若 VariableRow 的 Value 是表达式（含其他变量名或运算符），
        /// 按当前其它变量的解析值递归求值；纯数字则直接返回；否则回退到数值仓。</summary>
        public static double GetVariableResolved(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            var vars = ProjectStore.Data?.Variables;
            if (vars != null)
            {
                foreach (var r in vars)
                    for (int c = 1; c <= 5; c++)
                    {
                        if (string.Equals(VarName(r, c), name, StringComparison.OrdinalIgnoreCase))
                        {
                            string raw = VarValue(r, c);
                            if (string.IsNullOrWhiteSpace(raw)) return GetVariable(name);
                            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var num)) return num;
                            if (ExpressionEvaluator.Evaluate(raw, n => GetVariableResolved(n), out var r2)) return r2;
                            return GetVariable(name);
                        }
                    }
            }
            return GetVariable(name);
        }

        public static void SetVariable(string name, double value)
        {
            if (string.IsNullOrEmpty(name)) return;
            _variables[name] = value;
            WriteVarRow(name, value);
            Changed?.Invoke();
        }

        private static void WriteVarRow(string name, double value)
        {
            var vars = ProjectStore.Data?.Variables;
            if (vars == null) return;
            foreach (var r in vars)
            {
                for (int c = 1; c <= 5; c++)
                {
                    if (string.Equals(VarName(r, c), name, StringComparison.OrdinalIgnoreCase))
                    {
                        SetVarCell(r, c, value.ToString("0.###"));
                        return;
                    }
                }
            }
        }

        private static string VarName(VariableRow r, int c) => c switch
        {
            1 => r.Name1, 2 => r.Name2, 3 => r.Name3, 4 => r.Name4, 5 => r.Name5, _ => string.Empty
        };
        private static string VarValue(VariableRow r, int c) => c switch
        {
            1 => r.Value1, 2 => r.Value2, 3 => r.Value3, 4 => r.Value4, 5 => r.Value5, _ => string.Empty
        };
        private static void SetVarCell(VariableRow r, int c, string v)
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
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤ۦ▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣ۤۦ▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣ۤ
