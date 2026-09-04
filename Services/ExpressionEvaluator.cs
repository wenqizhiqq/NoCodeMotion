// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// =====================================================================
// 共享表达式求值器：供 变量实时计算 / 流程"变量"步骤 / 条件分支 复用。
// 支持：小数、一元负号、+ - * / %、括号、标识符（变量名，由 lookup 提供）；
// 比较运算符（> >= < <= == !=）用于条件分支，返回 bool。
// =====================================================================
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NoCodeMotion.Services
{
    /// <summary>极简算术/比较表达式求值（递归下降）。</summary>
    public static class ExpressionEvaluator
    {
        /// <summary>判断字符串是否为需要求值的表达式（不是纯数字字面量）。</summary>
        public static bool IsExpression(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) return false;
            return Regex.IsMatch(s, @"[+\-*/%()]|[A-Za-z_]");
        }

        /// <summary>求值算术表达式。标识符经 lookup 取得数值（未知变量记 0）。失败时返回 false 且 result=0。</summary>
        public static bool Evaluate(string expr, Func<string, double> lookup, out double result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(expr)) return false;
            try
            {
                var toks = Tokenize(expr);
                var p = new Parser(toks, lookup);
                result = p.ParseExpr();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>求值带比较符的条件表达式，返回 bool。无比较符时按数值真值。</summary>
        public static bool EvaluateCondition(string expr, Func<string, double> lookup)
        {
            if (string.IsNullOrWhiteSpace(expr)) return false;
            int opLen = 0; string? op = null;
            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];
                if (c == '>' || c == '<' || c == '=' || c == '!')
                {
                    if (i + 1 < expr.Length && expr[i + 1] == '=') { op = expr.Substring(i, 2); opLen = 2; break; }
                    if (c == '=' || c == '!') continue;
                    op = expr.Substring(i, 1); opLen = 1; break;
                }
            }
            if (op == null)
                return Math.Abs(Evaluate(expr, lookup, out var v) ? v : 0) > 1e-9;
            int idx = expr.IndexOf(op, StringComparison.Ordinal);
            string left = expr.Substring(0, idx).Trim();
            string right = expr.Substring(idx + opLen).Trim();
            double lv = Evaluate(left, lookup, out var l) ? l : 0;
            double rv = Evaluate(right, lookup, out var r) ? r : 0;
            return op switch
            {
                ">" => lv > rv, ">=" => lv >= rv, "<" => lv < rv, "<=" => lv <= rv,
                "==" => Math.Abs(lv - rv) < 1e-9, "!=" => Math.Abs(lv - rv) >= 1e-9,
                _ => false
            };
        }

        private static System.Collections.Generic.List<string> Tokenize(string s)
        {
            var toks = new System.Collections.Generic.List<string>();
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }
                if (char.IsDigit(c) || (c == '.' && i + 1 < s.Length && char.IsDigit(s[i + 1])))
                {
                    int j = i; while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.')) i++;
                    toks.Add(s.Substring(j, i - j)); continue;
                }
                if (char.IsLetter(c) || c == '_')
                {
                    int j = i; while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                    toks.Add(s.Substring(j, i - j)); continue;
                }
                toks.Add(c.ToString()); i++;
            }
            return toks;
        }

        private sealed class Parser
        {
            private readonly System.Collections.Generic.List<string> _t;
            private readonly Func<string, double> _lookup;
            private int _p;
            public Parser(System.Collections.Generic.List<string> t, Func<string, double> lookup) { _t = t; _lookup = lookup; }

            private string Peek() => _p < _t.Count ? _t[_p] : "";
            private string Next() => _p < _t.Count ? _t[_p++] : "";

            public double ParseExpr() => ParseAdd();

            private double ParseAdd()
            {
                double v = ParseMul();
                while (Peek() == "+" || Peek() == "-")
                {
                    string op = Next();
                    double r = ParseMul();
                    v = op == "+" ? v + r : v - r;
                }
                return v;
            }
            private double ParseMul()
            {
                double v = ParseUnary();
                while (Peek() == "*" || Peek() == "/" || Peek() == "%")
                {
                    string op = Next();
                    double r = ParseUnary();
                    v = op == "*" ? v * r : op == "/" ? (r == 0 ? 0 : v / r) : (r == 0 ? 0 : v % r);
                }
                return v;
            }
            private double ParseUnary()
            {
                if (Peek() == "-") { Next(); return -ParseUnary(); }
                if (Peek() == "+") { Next(); return ParseUnary(); }
                return ParsePrimary();
            }
            private double ParsePrimary()
            {
                string tok = Next();
                if (tok == "(") { double v = ParseAdd(); if (Peek() == ")") Next(); return v; }
                if (double.TryParse(tok, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
                if (!string.IsNullOrEmpty(tok)) return _lookup(tok);
                return 0;
            }
        }
    }
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥
