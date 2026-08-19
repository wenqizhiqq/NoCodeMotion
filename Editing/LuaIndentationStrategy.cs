#nullable disable
using System;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Indentation;

namespace NoCodeMotion.Editing
{
    /// <summary>Lua 自动缩进：then / do / function / { 之后加一级，end / else / until / } 回退一级。</summary>
    public sealed class LuaIndentationStrategy : IIndentationStrategy
    {
        private static readonly Regex OpenBlock = new Regex(
            @"(\b(then|do|else|repeat)\s*$)|(\bfunction\b[^)]*\)\s*$)|(\{\s*$)", RegexOptions.Compiled);

        private static readonly Regex CloseBlock = new Regex(
            @"^\s*(end|else|elseif|until|\})", RegexOptions.Compiled);

        public string IndentationString { get; set; } = "    ";

        public void IndentLine(TextDocument document, DocumentLine line)
        {
            if (document == null || line == null) return;

            DocumentLine previous = line.PreviousLine;
            if (previous == null) return;

            string prevText = document.GetText(previous.Offset, previous.Length);
            string prevCode = StripComment(prevText);
            string indent = new string(prevText.TakeWhile(c => c == ' ' || c == '\t').ToArray());

            if (OpenBlock.IsMatch(prevCode.TrimEnd()))
                indent += IndentationString;

            string currentText = document.GetText(line.Offset, line.Length);
            if (CloseBlock.IsMatch(currentText) && indent.Length >= IndentationString.Length)
                indent = indent.Substring(0, indent.Length - IndentationString.Length);

            int whitespaceLength = currentText.TakeWhile(c => c == ' ' || c == '\t').Count();
            document.Replace(line.Offset, whitespaceLength, indent);
        }

        public void IndentLines(TextDocument document, int beginLine, int endLine)
        {
            for (int i = beginLine; i <= endLine; i++)
                IndentLine(document, document.GetLineByNumber(i));
        }

        private static string StripComment(string text)
        {
            int idx = text.IndexOf("--", StringComparison.Ordinal);
            return idx >= 0 ? text.Substring(0, idx) : text;
        }
    }
}
