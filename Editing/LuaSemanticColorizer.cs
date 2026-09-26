// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‎启‏志​◆‍编‌写⁣◇‏微‌信‌﹕‎1⁠8‎7‍◆‎1‍9‎3‎6​◇‍1‎3‍9‌9⁣　‌※‌保⁣留​所‎有​权‌利‌请‏勿⁠删‌除‏◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace NoCodeMotion.Editing
{
    /// <summary>
    /// 语义着色：在语法高亮（关键字 / 字符串 / 数字 / 注释）之上，对“用户定义的变量 / 函数 / 表字段”
    /// 与“标准库全局名”额外着以区别色，使变量在编辑器中一眼可辨。
    /// 仅在标识符不在注释 / 字符串内时才着色，避免误染。
    /// 同时对控制流关键字（if / for / return 等）也独立地刷一层醒目橙色（加粗），
    /// 作为对 xshd Keywords 规则的双保险，避免因 xshd 未生效或 App 未重启时看不到区别色。
    /// </summary>
    public sealed class LuaSemanticColorizer : DocumentColorizingTransformer
    {
        // 用户自定义变量 / 表字段：蓝；用户自定义函数：棕（与 xshd 的 Function 色一致）
        private static readonly Brush VarBrush = new SolidColorBrush(Color.FromRgb(0x0F, 0x6C, 0xBD));
        private static readonly Brush FuncBrush = new SolidColorBrush(Color.FromRgb(0x79, 0x5E, 0x26));

        private static readonly Regex Identifier = new Regex(@"\b[A-Za-z_][A-Za-z0-9_]*\b", RegexOptions.Compiled);
        private static readonly Regex CommentLine = new Regex(@"--[^\n]*", RegexOptions.Compiled);
        private static readonly Regex StringLiteral = new Regex("\"(\\\\.|[^\"\\\\])*\"|'(\\\\.|[^'\\\\])*'", RegexOptions.Compiled);

        /// <summary>
        /// 已由 xshd 语法高亮负责的名字（注册硬件函数 + 标准库表名），语义着色器不再覆盖，
        /// 避免出现「同色冲突 / 颜色被刷掉」：注册函数保持棕色、标准库表保持青绿、关键字保持紫色。
        /// </summary>
        private static readonly HashSet<string> XshdCovered = BuildXshdCovered();

        private static HashSet<string> BuildXshdCovered()
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (LuaSymbol s in LuaApi.HardwareList) set.Add(s.Name);
            foreach (string m in LuaApi.ModuleNames) set.Add(m);
            return set;
        }

        // 按文档内容缓存符号集合，避免每行重复分析
        private string _cacheText;
        private HashSet<string> _funcSet;
        private HashSet<string> _varSet;

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
                if (LuaApi.Keywords.Contains(id)) continue;   // 关键字交给 xshd 着色
                if (XshdCovered.Contains(id)) continue;       // 注册函数 / 标准库表交给 xshd 着色

                Brush brush = null;
                if (_funcSet.Contains(id)) brush = FuncBrush;
                else if (_varSet.Contains(id)) brush = VarBrush;

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
        }

        private static string Blank(string original) =>
            new string(original.Select(c => c == '\n' ? '\n' : ' ').ToArray());
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
