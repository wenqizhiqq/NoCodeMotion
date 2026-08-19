#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace NoCodeMotion.Editing
{
    /// <summary>
    /// 语义着色：在语法高亮（关键字 / 字符串 / 数字 / 注释）之上，对“用户定义的变量 / 函数 / 表字段”
    /// 与“标准库全局名”额外着以区别色，使变量在编辑器中一眼可辨。
    /// 仅在标识符不在注释 / 字符串内时才着色，避免误染。
    /// </summary>
    public sealed class LuaSemanticColorizer : DocumentColorizingTransformer
    {
        // 变量 / 表字段：蓝（明显，区别于标准库青绿）；函数：棕（与语法高亮 FunctionCall 一致）；标准库：蓝绿（与 StdLib 一致）
        private static readonly Brush VarBrush = new SolidColorBrush(Color.FromRgb(0x0F, 0x6C, 0xBD));
        private static readonly Brush FuncBrush = new SolidColorBrush(Color.FromRgb(0x79, 0x5E, 0x26));
        private static readonly Brush StdBrush = new SolidColorBrush(Color.FromRgb(0x1F, 0x7A, 0x8C));

        private static readonly Regex Identifier = new Regex(@"\b[A-Za-z_][A-Za-z0-9_]*\b", RegexOptions.Compiled);
        private static readonly Regex CommentLine = new Regex(@"--[^\n]*", RegexOptions.Compiled);
        private static readonly Regex StringLiteral = new Regex("\"(\\\\.|[^\"\\\\])*\"|'(\\\\.|[^'\\\\])*'", RegexOptions.Compiled);

        // 按文档内容缓存符号集合，避免每行重复分析
        private string _cacheText;
        private HashSet<string> _funcSet;
        private HashSet<string> _varSet;
        private HashSet<string> _stdSet;

        protected override void ColorizeLine(DocumentLine line)
        {
            string full = CurrentContext.Document.Text;
            if (full != _cacheText)
            {
                RebuildCache(full);
                _cacheText = full;
            }

            string text = CurrentContext.Document.GetText(line);
            string masked = CommentLine.Replace(text, m => Blank(m.Value));
            masked = StringLiteral.Replace(masked, m => Blank(m.Value));

            foreach (Match m in Identifier.Matches(masked))
            {
                string id = m.Value;
                if (LuaApi.Keywords.Contains(id)) continue;          // 关键字交给语法高亮

                Brush brush = null;
                if (_funcSet.Contains(id)) brush = FuncBrush;
                else if (_varSet.Contains(id)) brush = VarBrush;
                else if (_stdSet.Contains(id)) brush = StdBrush;

                if (brush != null)
                    ChangeLinePart(line.Offset + m.Index, line.Offset + m.Index + m.Length,
                        v => v.TextRunProperties.SetForegroundBrush(brush));
            }
        }

        private void RebuildCache(string full)
        {
            var locals = LuaDocumentAnalyzer.Analyze(full);
            _funcSet = new HashSet<string>(StringComparer.Ordinal);
            _varSet = new HashSet<string>(StringComparer.Ordinal);
            foreach (LuaSymbol s in locals)
            {
                if (s.Kind == SymbolKind.Function) _funcSet.Add(s.Name);
                else if (s.Kind != SymbolKind.Keyword) _varSet.Add(s.Name);
            }

            _stdSet = new HashSet<string>(StringComparer.Ordinal);
            foreach (LuaSymbol s in LuaApi.Globals) _stdSet.Add(s.Name);
        }

        private static string Blank(string original) =>
            new string(original.Select(c => c == '\n' ? '\n' : ' ').ToArray());
    }
}
