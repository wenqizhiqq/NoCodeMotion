#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace NoCodeMotion.Editing
{
    /// <summary>补全列表中的一项。</summary>
    public sealed class LuaCompletionData : ICompletionData
    {
        private static readonly Dictionary<SymbolKind, Tuple<string, Color>> Icons =
            new Dictionary<SymbolKind, Tuple<string, Color>>
            {
                [SymbolKind.Keyword] = Tuple.Create("K", Color.FromRgb(0x56, 0x5F, 0xC8)),
                [SymbolKind.Function] = Tuple.Create("ƒ", Color.FromRgb(0xB0, 0x7D, 0x2B)),
                [SymbolKind.Module] = Tuple.Create("M", Color.FromRgb(0x1F, 0x7A, 0x8C)),
                [SymbolKind.Field] = Tuple.Create("F", Color.FromRgb(0x2E, 0x7D, 0x32)),
                [SymbolKind.Variable] = Tuple.Create("V", Color.FromRgb(0x37, 0x6B, 0xB5)),
                [SymbolKind.Snippet] = Tuple.Create("S", Color.FromRgb(0x8E, 0x44, 0xAD))
            };

        private readonly LuaSymbol _symbol;

        public LuaCompletionData(LuaSymbol symbol, double priority = 0)
        {
            _symbol = symbol;
            Priority = priority;
        }

        public LuaSymbol Symbol => _symbol;

        public ImageSource Image => null;

        public string Text => _symbol.Name;

        public double Priority { get; }

        public object Content
        {
            get
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal };
                var icon = Icons.TryGetValue(_symbol.Kind, out var v) ? v : Icons[SymbolKind.Variable];

                panel.Children.Add(new Border
                {
                    Width = 16,
                    Height = 16,
                    CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush(icon.Item2),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = icon.Item1,
                        Foreground = Brushes.White,
                        FontSize = 10,
                        FontWeight = FontWeights.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });

                panel.Children.Add(new TextBlock
                {
                    Text = _symbol.Name,
                    Margin = new Thickness(7, 0, 4, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });

                return panel;
            }
        }

        public object Description
        {
            get
            {
                var panel = new StackPanel { MaxWidth = 420 };

                panel.Children.Add(new TextBlock
                {
                    Text = _symbol.Signature,
                    FontFamily = new FontFamily("Consolas, Cascadia Mono, Courier New"),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x3B, 0x63)),
                    TextWrapping = TextWrapping.Wrap
                });

                if (!string.IsNullOrWhiteSpace(_symbol.Description))
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = _symbol.Description,
                        Margin = new Thickness(0, 4, 0, 0),
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44))
                    });
                }

                return panel;
            }
        }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            TextDocument doc = textArea.Document;
            string insert = _symbol.InsertText ?? _symbol.Name;
            int offset = completionSegment.Offset;
            int length = completionSegment.Length;

            if (insert.IndexOf('\n') >= 0)
            {
                DocumentLine line = doc.GetLineByOffset(offset);
                string lineText = doc.GetText(line.Offset, line.Length);
                string indent = new string(lineText.TakeWhile(c => c == ' ' || c == '\t').ToArray());
                insert = insert.Replace("\n", Environment.NewLine + indent).Replace("\t", "    ");
            }

            int caretMarker = insert.IndexOf(LuaApi.CaretMarker);
            if (caretMarker >= 0) insert = insert.Remove(caretMarker, 1);

            bool autoParens = _symbol.Kind == SymbolKind.Function && caretMarker < 0;
            if (autoParens)
            {
                int after = offset + length;
                char next = after < doc.TextLength ? doc.GetCharAt(after) : '\0';
                if (next != '(')
                {
                    insert += "()";
                    caretMarker = insert.Length - 1;
                }
            }

            doc.Replace(offset, length, insert);

            if (caretMarker >= 0)
                textArea.Caret.Offset = Math.Min(offset + caretMarker, doc.TextLength);
        }
    }
}
