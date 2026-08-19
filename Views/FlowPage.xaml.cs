using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using NoCodeMotion.Editing;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.ViewModels;

namespace NoCodeMotion.Views
{
    public partial class FlowPage : UserControl
    {
        private FlowViewModel VM => (FlowViewModel)DataContext;

        private TextEditor? _editor;
        private CompletionWindow? _completionWindow;
        private LineHighlightRenderer? _currentLineRenderer;
        private LineHighlightRenderer? _errorLineRenderer;
        private FlowItem? _currentItem;
        private bool _settingText;
        private bool _wired;

        public FlowPage()
        {
            InitializeComponent();
            DataContext = new FlowViewModel();
            Loaded += OnLoaded;
            VM.RequestShowCompletion += () => ShowWordCompletion(true);
            VM.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_wired) return;
            _editor = FindVisualChild<TextEditor>(this);
            if (_editor == null) return;

            // 语法高亮（Lua.xshd 作为内嵌资源）
            try
            {
                using Stream? stream = typeof(FlowPage).Assembly
                    .GetManifestResourceStream("NoCodeMotion.Assets.Lua.xshd");
                if (stream != null)
                    using (XmlReader reader = XmlReader.Create(stream))
                        _editor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lua 语法高亮加载失败：" + ex.Message);
            }

            _editor.Options.ConvertTabsToSpaces = true;
            _editor.Options.IndentationSize = 4;
            _editor.Options.HighlightCurrentLine = true;
            _editor.TextArea.IndentationStrategy = new LuaIndentationStrategy();

            TextView view = _editor.TextArea.TextView;
            _currentLineRenderer = new LineHighlightRenderer(
                Color.FromRgb(0xBF, 0xDB, 0xFE), Color.FromRgb(0x1D, 0x4E, 0xD8));
            _errorLineRenderer = new LineHighlightRenderer(
                Color.FromRgb(0xF8, 0xD7, 0xDA), Color.FromRgb(0xC6, 0x28, 0x28));
            view.BackgroundRenderers.Add(_errorLineRenderer);
            view.BackgroundRenderers.Add(_currentLineRenderer);

            _editor.TextChanged += (s, ev) =>
            {
                if (_settingText) return;
                if (_currentItem != null) _currentItem.LuaSource = _editor!.Text;
            };
            _editor.TextArea.TextEntering += TextArea_TextEntering;
            _editor.TextArea.TextEntered += TextArea_TextEntered;

            // 选中项变化时同步到编辑器
            AttachToItem();
            _wired = true;
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FlowViewModel.SelectedItem))
            {
                AttachToItem();
            }
            else if (e.PropertyName == nameof(FlowViewModel.LuaCurrentLine))
            {
                if (_currentLineRenderer != null)
                    _currentLineRenderer.Line = VM.LuaIsDebugging ? VM.LuaCurrentLine : 0;
            }
            else if (e.PropertyName == nameof(FlowViewModel.LuaErrorLine))
            {
                if (_errorLineRenderer != null)
                    _errorLineRenderer.Line = VM.LuaHasError ? VM.LuaErrorLine : 0;
            }
            else if (e.PropertyName == nameof(FlowViewModel.LuaIsDebugging) && !VM.LuaIsDebugging)
            {
                if (_currentLineRenderer != null) _currentLineRenderer.Line = 0;
            }
        }

        private void AttachToItem()
        {
            if (_editor == null) _editor = FindVisualChild<TextEditor>(this);
            if (_currentItem != null)
                _currentItem.PropertyChanged -= OnItemPropertyChanged;
            _currentItem = VM.SelectedItem;
            if (_currentItem != null)
                _currentItem.PropertyChanged += OnItemPropertyChanged;
            LoadEditorText();
        }

        private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FlowItem.LuaSource))
                LoadEditorText();
        }

        private void LoadEditorText()
        {
            if (_editor == null || _currentItem == null) return;
            string src = _currentItem.LuaSource ?? "";
            if (_editor.Text != src)
            {
                _settingText = true;
                _editor.Text = src;
                _settingText = false;
            }
        }

        #region 智能提示

        private void TextArea_TextEntering(object? sender, TextCompositionEventArgs e)
        {
            if (_completionWindow == null || e.Text.Length == 0) return;
            char c = e.Text[0];
            if (!char.IsLetterOrDigit(c) && c != '_')
                _completionWindow.CompletionList.RequestInsertion(e);
        }

        private void TextArea_TextEntered(object? sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length == 0 || _editor == null) return;
            char c = e.Text[0];
            if (c == '.' || c == ':')
                ShowMemberCompletion();
            else if (char.IsLetter(c) || c == '_')
                ShowWordCompletion(false);
        }

        private void ShowWordCompletion(bool force)
        {
            if (_editor == null) return;
            int caret = _editor.CaretOffset;
            int start = FindWordStart(caret);
            string prefix = _editor.Document.GetText(start, caret - start);
            if (!force && prefix.Length == 0) return;

            var items = new List<LuaCompletionData>();
            var used = new HashSet<string>(StringComparer.Ordinal);

            void Add(LuaSymbol symbol, double priority)
            {
                if (symbol == null || !used.Add(symbol.Name)) return;
                items.Add(new LuaCompletionData(symbol, priority));
            }

            // 1. 运行时变量（暂停时最有用，排最前）
            foreach (var v in VM.LuaVariables)
                Add(new LuaSymbol(v.Name, SymbolKind.Variable, $"{v.Name} = {v.Value}", "运行时变量，当前值"), 100);

            // 2. 当前文档里的符号
            foreach (LuaSymbol s in LuaDocumentAnalyzer.Analyze(_editor.Text)) Add(s, 50);

            // 3. 标准库
            foreach (LuaSymbol s in LuaApi.Globals) Add(s, 20);

            // 4. 关键字与代码片段
            foreach (string k in LuaApi.Keywords)
                Add(new LuaSymbol(k, SymbolKind.Keyword, k, "Lua 关键字"), 10);
            foreach (LuaSymbol s in LuaApi.Snippets) Add(s, 5);

            ShowCompletionWindow(start, items);
        }

        private void ShowMemberCompletion()
        {
            if (_editor == null) return;
            int caret = _editor.CaretOffset;
            if (caret < 2) return;

            string root = GetIdentifierBefore(caret - 1);
            if (string.IsNullOrEmpty(root)) return;

            var items = new List<LuaCompletionData>();
            var used = new HashSet<string>(StringComparer.Ordinal);

            void Add(LuaSymbol symbol, double priority)
            {
                if (symbol == null || !used.Add(symbol.Name)) return;
                items.Add(new LuaCompletionData(symbol, priority));
            }

            if (LuaApi.TryGetMembers(root, out List<LuaSymbol>? members))
                foreach (LuaSymbol s in members) Add(s, 60);

            foreach (LuaSymbol s in LuaDocumentAnalyzer.GetTableFields(_editor.Text, root)) Add(s, 30);

            if (items.Count == 0) return;
            ShowCompletionWindow(caret, items);
        }

        private void ShowCompletionWindow(int startOffset, List<LuaCompletionData> items)
        {
            if (_editor == null || items.Count == 0) return;

            _completionWindow = new CompletionWindow(_editor.TextArea)
            {
                Width = 340,
                MaxHeight = 320,
                CloseAutomatically = true,
                StartOffset = startOffset,
                EndOffset = _editor.CaretOffset
            };

            foreach (LuaCompletionData item in items.OrderByDescending(i => i.Priority))
                _completionWindow.CompletionList.CompletionData.Add(item);

            _completionWindow.CompletionList.SelectItem(
                _editor.Document.GetText(startOffset, _editor.CaretOffset - startOffset));

            _completionWindow.Closed += (s, ev) => _completionWindow = null;
            _completionWindow.Show();
        }

        #endregion

        #region 光标辅助

        private int FindWordStart(int offset)
        {
            if (_editor == null) return offset;
            string text = _editor.Text;
            int i = offset - 1;
            while (i >= 0 && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                i--;
            return i + 1;
        }

        private string GetIdentifierBefore(int offset)
        {
            if (_editor == null) return "";
            string text = _editor.Text;
            int i = offset;
            while (i >= 0 && (char.IsLetterOrDigit(text[i]) || text[i] == '_' || text[i] == '.' || text[i] == ':'))
                i--;
            string token = text.Substring(i + 1, offset - (i + 1));
            int dot = token.LastIndexOfAny(new[] { '.', ':' });
            return dot >= 0 ? token.Substring(dot + 1) : token;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                T? inside = FindVisualChild<T>(child);
                if (inside != null) return inside;
            }
            return null;
        }

        #endregion
    }
}
