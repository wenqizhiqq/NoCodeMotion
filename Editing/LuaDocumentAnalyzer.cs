#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NoCodeMotion.Editing
{
    /// <summary>
    /// 轻量级源码扫描：从当前文档里提取用户定义的函数、局部变量、全局变量和表字段，
    /// 供智能提示与变量悬停使用。
    /// </summary>
    public static class LuaDocumentAnalyzer
    {
        private static readonly Regex CommentBlock = new Regex(@"--\[\[.*?\]\]", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex CommentLine = new Regex(@"--[^\n]*", RegexOptions.Compiled);
        private static readonly Regex StringLiteral = new Regex("\"(\\\\.|[^\"\\\\])*\"|'(\\\\.|[^'\\\\])*'", RegexOptions.Compiled);

        private static readonly Regex NamedFunction = new Regex(@"\bfunction\s+([A-Za-z_][A-Za-z0-9_\.\:]*)\s*\(([^)]*)\)", RegexOptions.Compiled);
        private static readonly Regex LocalFunction = new Regex(@"\blocal\s+function\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(([^)]*)\)", RegexOptions.Compiled);
        private static readonly Regex AnonFunctionAssign = new Regex(@"\b(?:local\s+)?([A-Za-z_][A-Za-z0-9_]*)\s*=\s*function\s*\(([^)]*)\)", RegexOptions.Compiled);
        private static readonly Regex LocalDecl = new Regex(@"\blocal\s+([A-Za-z_][A-Za-z0-9_]*(?:\s*,\s*[A-Za-z_][A-Za-z0-9_]*)*)", RegexOptions.Compiled);
        private static readonly Regex NumericFor = new Regex(@"\bfor\s+([A-Za-z_][A-Za-z0-9_]*)\s*=", RegexOptions.Compiled);
        private static readonly Regex GenericFor = new Regex(@"\bfor\s+([A-Za-z_][A-Za-z0-9_]*(?:\s*,\s*[A-Za-z_][A-Za-z0-9_]*)*)\s+in\b", RegexOptions.Compiled);
        private static readonly Regex GlobalAssign = new Regex(@"^[ \t]*([A-Za-z_][A-Za-z0-9_]*)\s*=[^=]", RegexOptions.Multiline | RegexOptions.Compiled);

        private static string _cacheKey;
        private static List<LuaSymbol> _cache;

        /// <summary>提取文档中的用户符号（带缓存）。</summary>
        public static List<LuaSymbol> Analyze(string text)
        {
            if (text == null) return new List<LuaSymbol>();
            if (_cacheKey == text && _cache != null) return _cache;

            string clean = Strip(text);
            var map = new Dictionary<string, LuaSymbol>(StringComparer.Ordinal);

            void Add(string name, SymbolKind kind, string signature, string desc)
            {
                if (string.IsNullOrEmpty(name)) return;
                if (LuaApi.Keywords.Contains(name)) return;
                if (!map.ContainsKey(name)) map[name] = new LuaSymbol(name, kind, signature, desc);
            }

            foreach (Match m in LocalFunction.Matches(clean))
            {
                string args = Normalize(m.Groups[2].Value);
                Add(m.Groups[1].Value, SymbolKind.Function, $"local function {m.Groups[1].Value}({args})", "本文件定义的局部函数");
                foreach (string p in SplitNames(m.Groups[2].Value))
                    Add(p, SymbolKind.Variable, p, "函数参数");
            }

            foreach (Match m in NamedFunction.Matches(clean))
            {
                string full = m.Groups[1].Value;
                string args = Normalize(m.Groups[2].Value);
                string simple = full.Split('.', ':').Last();
                Add(simple, SymbolKind.Function, $"function {full}({args})", "本文件定义的函数");
                foreach (string p in SplitNames(m.Groups[2].Value))
                    Add(p, SymbolKind.Variable, p, "函数参数");
            }

            foreach (Match m in AnonFunctionAssign.Matches(clean))
            {
                string args = Normalize(m.Groups[2].Value);
                Add(m.Groups[1].Value, SymbolKind.Function, $"{m.Groups[1].Value}({args})", "本文件定义的函数");
                foreach (string p in SplitNames(m.Groups[2].Value))
                    Add(p, SymbolKind.Variable, p, "函数参数");
            }

            foreach (Match m in LocalDecl.Matches(clean))
                foreach (string n in SplitNames(m.Groups[1].Value))
                    Add(n, SymbolKind.Variable, "local " + n, "本文件的局部变量");

            foreach (Match m in NumericFor.Matches(clean))
                Add(m.Groups[1].Value, SymbolKind.Variable, m.Groups[1].Value, "循环变量");

            foreach (Match m in GenericFor.Matches(clean))
                foreach (string n in SplitNames(m.Groups[1].Value))
                    Add(n, SymbolKind.Variable, n, "循环变量");

            foreach (Match m in GlobalAssign.Matches(clean))
                Add(m.Groups[1].Value, SymbolKind.Variable, m.Groups[1].Value, "本文件的全局变量");

            _cache = map.Values.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();
            _cacheKey = text;
            return _cache;
        }

        /// <summary>取某个表变量在文档里出现过的字段名，用于 "t." 之后的成员提示。</summary>
        public static List<LuaSymbol> GetTableFields(string text, string root)
        {
            var result = new List<LuaSymbol>();
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(root)) return result;

            string clean = Strip(text);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            var usage = new Regex(Regex.Escape(root) + @"[\.\:]([A-Za-z_][A-Za-z0-9_]*)");
            foreach (Match m in usage.Matches(clean))
            {
                string field = m.Groups[1].Value;
                if (seen.Add(field))
                    result.Add(new LuaSymbol(field, SymbolKind.Field, root + "." + field, "本文件中出现的字段"));
            }

            // local t = { a = 1, b = 2 }
            var literal = new Regex(@"\b" + Regex.Escape(root) + @"\s*=\s*\{(?<body>[^{}]*)\}", RegexOptions.Singleline);
            Match lit = literal.Match(clean);
            if (lit.Success)
            {
                foreach (Match f in Regex.Matches(lit.Groups["body"].Value, @"([A-Za-z_][A-Za-z0-9_]*)\s*="))
                {
                    string field = f.Groups[1].Value;
                    if (seen.Add(field))
                        result.Add(new LuaSymbol(field, SymbolKind.Field, root + "." + field, "表字段"));
                }
            }

            return result;
        }

        /// <summary>去掉注释与字符串，避免误扫描。</summary>
        private static string Strip(string text)
        {
            string s = CommentBlock.Replace(text, m => Blank(m.Value));
            s = StringLiteral.Replace(s, m => Blank(m.Value));
            s = CommentLine.Replace(s, m => Blank(m.Value));
            return s;
        }

        private static string Blank(string original) =>
            new string(original.Select(c => c == '\n' ? '\n' : ' ').ToArray());

        private static IEnumerable<string> SplitNames(string list) =>
            list.Split(',')
                .Select(p => p.Trim())
                .Where(p => Regex.IsMatch(p, @"^[A-Za-z_][A-Za-z0-9_]*$"));

        private static string Normalize(string args) =>
            string.Join(", ", args.Split(',').Select(a => a.Trim()).Where(a => a.Length > 0));
    }
}
