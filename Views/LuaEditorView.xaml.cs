#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using MoonSharp.Interpreter.Debugging;
using NoCodeMotion.Editing;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.Views
{
    /// <summary>输出面板的一行。</summary>
    public sealed class LogEntry
    {
        public string Text { get; set; } = string.Empty;
        public Brush Brush { get; set; } = Brushes.Black;
    }

    public partial class LuaEditorView : UserControl
    {
        private static readonly Brush BrushInfo = new SolidColorBrush(Color.FromRgb(0x44, 0x4D, 0x56));
        private static readonly Brush BrushOutput = new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x11));
        private static readonly Brush BrushError = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));
        private static readonly Brush BrushSuccess = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));

        private readonly BreakpointMargin _bpMargin = new BreakpointMargin();
        private readonly LineHighlightRenderer _currentLineRenderer =
            new LineHighlightRenderer(Color.FromArgb(0x66, 0xFF, 0xE0, 0x82), Color.FromArgb(0xAA, 0xE0, 0xB4, 0x33));
        private readonly LineHighlightRenderer _errorLineRenderer =
            new LineHighlightRenderer(Color.FromArgb(0x38, 0xFF, 0x6B, 0x6B), Color.FromArgb(0x99, 0xE0, 0x6C, 0x6C));
        private readonly ObservableCollection<LogEntry> _log = new ObservableCollection<LogEntry>();
        private readonly Dictionary<string, VarInfo> _varIndex = new Dictionary<string, VarInfo>(StringComparer.Ordinal);
        private readonly ToolTip _hoverTip = new ToolTip { Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse };
        private readonly DispatcherTimer _syntaxTimer;

        private LuaDebugSession _session;
        private CompletionWindow _completionWindow;
        private InsightWindow _insightWindow;
        private bool _settingText;

        public LuaEditorView()
        {
            InitializeComponent();

            SetupEditor();

            OutputList.ItemsSource = _log;
            UpdateCaretStatus();
            SetSessionState(SessionState.Idle);
            AppendLog("就绪。F5 运行，F10 单步，F9 断点。", LogKind.Info);

            _syntaxTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _syntaxTimer.Tick += (s, e) => CheckSyntaxNow();
            _syntaxTimer.Start();

            Unloaded += (s, e) => _session?.Stop();
            Loaded += (s, e) => Editor.Focus();
        }

        #region 依赖属性：绑定的 Lua 流程项

        public static readonly DependencyProperty LuaItemProperty =
            DependencyProperty.Register(nameof(LuaItem), typeof(FlowItem), typeof(LuaEditorView),
                new PropertyMetadata(null, (d, e) => ((LuaEditorView)d).OnLuaItemChanged()));

        public FlowItem LuaItem
        {
            get => (FlowItem)GetValue(LuaItemProperty);
            set => SetValue(LuaItemProperty, value);
        }

        private void OnLuaItemChanged()
        {
            _session?.Stop();
            ClearRuntimeMarkers();
            _log.Clear();
            _varIndex.Clear();
            TxtInspect.Text = "—";
            VarTree.ItemsSource = null;
            CallStackList.ItemsSource = null;
            _bpMargin.ClearAll();
            UpdateBreakpointCount();
            SetSessionState(SessionState.Idle);

            string src = LuaItem?.LuaSource ?? "";
            _settingText = true;
            Editor.Text = src;
            _settingText = false;
            CheckSyntaxNow();
        }

        #endregion

        #region 编辑器初始化

        private void SetupEditor()
        {
            try
            {
                using (Stream stream = typeof(LuaEditorView).Assembly.GetManifestResourceStream("NoCodeMotion.Assets.Lua.xshd"))
                {
                    if (stream != null)
                    {
                        using (XmlReader reader = XmlReader.Create(stream))
                            Editor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog("语法高亮加载失败：" + ex.Message, LogKind.Error);
            }

            Editor.Options.ConvertTabsToSpaces = true;
            Editor.Options.IndentationSize = 4;
            Editor.Options.HighlightCurrentLine = true;
            Editor.Options.EnableRectangularSelection = true;
            Editor.Options.AllowScrollBelowDocument = true;
            Editor.TextArea.IndentationStrategy = new LuaIndentationStrategy();

            // 断点边栏放在行号左侧
            Editor.TextArea.LeftMargins.Insert(0, _bpMargin);
            _bpMargin.BreakpointsChanged += (s, e) =>
            {
                UpdateBreakpointCount();
                _session?.SetBreakpoints(_bpMargin.Breakpoints);
            };

            TextView view = Editor.TextArea.TextView;
            view.BackgroundRenderers.Add(_errorLineRenderer);
            view.BackgroundRenderers.Add(_currentLineRenderer);

            Editor.TextArea.TextEntering += TextArea_TextEntering;
            Editor.TextArea.TextEntered += TextArea_TextEntered;
            Editor.TextArea.Caret.PositionChanged += (s, e) =>
            {
                UpdateCaretStatus();
                InspectAtCaret(false);
            };
            Editor.TextChanged += (s, e) =>
            {
                if (!_settingText && LuaItem != null) LuaItem.LuaSource = Editor.Text;
            };
            Editor.PreviewMouseLeftButtonUp += (s, e) =>
                Dispatcher.BeginInvoke(new Action(() => InspectAtCaret(true)), DispatcherPriority.Background);

            view.MouseHover += TextView_MouseHover;
            view.MouseHoverStopped += (s, e) => { _hoverTip.IsOpen = false; e.Handled = true; };

            PreviewKeyDown += LuaEditorView_PreviewKeyDown;
        }

        #endregion

        #region 智能提示

        private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (_completionWindow == null || e.Text.Length == 0) return;

            char c = e.Text[0];
            if (!char.IsLetterOrDigit(c) && c != '_')
                _completionWindow.CompletionList.RequestInsertion(e);
        }

        private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length == 0) return;
            char c = e.Text[0];

            if (c == '.' || c == ':')
            {
                ShowMemberCompletion();
            }
            else if (char.IsLetter(c) || c == '_')
            {
                if (_completionWindow == null) ShowWordCompletion(false);
            }
            else if (c == '(')
            {
                ShowSignatureInsight();
            }
        }

        /// <summary>标识符 / 关键字 / 代码片段补全。</summary>
        private void ShowWordCompletion(bool force)
        {
            int caret = Editor.CaretOffset;
            int start = FindWordStart(caret);
            string prefix = Editor.Document.GetText(start, caret - start);

            if (!force && prefix.Length == 0) return;

            var items = new List<LuaCompletionData>();
            var used = new HashSet<string>(StringComparer.Ordinal);

            void Add(LuaSymbol symbol, double priority)
            {
                if (symbol == null || !used.Add(symbol.Name)) return;
                items.Add(new LuaCompletionData(symbol, priority));
            }

            // 1. 运行时变量（最有用，排最前）
            foreach (var kv in _varIndex.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                var v = kv.Value;
                Add(new LuaSymbol(v.Name, SymbolKind.Variable, $"{v.Name} : {v.TypeName}",
                    $"{v.Scope}变量，当前值：{v.Value}"), 100);
            }

            // 2. 当前文档里的符号
            foreach (LuaSymbol s in LuaDocumentAnalyzer.Analyze(Editor.Text)) Add(s, 50);

            // 3. 标准库
            foreach (LuaSymbol s in LuaApi.Globals) Add(s, 20);

            // 4. 关键字与代码片段
            foreach (string k in LuaApi.Keywords)
                Add(new LuaSymbol(k, SymbolKind.Keyword, k, "Lua 关键字"), 10);
            foreach (LuaSymbol s in LuaApi.Snippets) Add(s, 5);

            ShowCompletionWindow(start, items);
        }

        /// <summary>"." / ":" 之后的成员补全。</summary>
        private void ShowMemberCompletion()
        {
            int caret = Editor.CaretOffset;
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

            // 运行时表字段
            if (_varIndex.TryGetValue(root, out VarInfo info) && info.Children.Count > 0)
            {
                foreach (VarInfo child in info.Children)
                    Add(new LuaSymbol(child.Name, SymbolKind.Field, $"{root}.{child.Name} : {child.TypeName}",
                        "当前值：" + child.Value), 100);
            }

            // 标准库成员
            if (LuaApi.TryGetMembers(root, out List<LuaSymbol> members))
                foreach (LuaSymbol s in members) Add(s, 60);

            // 文档中出现过的字段
            foreach (LuaSymbol s in LuaDocumentAnalyzer.GetTableFields(Editor.Text, root)) Add(s, 30);

            if (items.Count == 0) return;
            ShowCompletionWindow(caret, items);
        }

        private void ShowCompletionWindow(int startOffset, List<LuaCompletionData> items)
        {
            if (items.Count == 0) return;

            _completionWindow = new CompletionWindow(Editor.TextArea)
            {
                Width = 340,
                MaxHeight = 320,
                CloseAutomatically = true,
                StartOffset = startOffset,
                EndOffset = Editor.CaretOffset
            };

            foreach (LuaCompletionData item in items.OrderByDescending(i => i.Priority))
                _completionWindow.CompletionList.CompletionData.Add(item);

            _completionWindow.CompletionList.SelectItem(
                Editor.Document.GetText(startOffset, Editor.CaretOffset - startOffset));

            _completionWindow.Closed += (s, e) => _completionWindow = null;
            _completionWindow.Show();
        }

        /// <summary>输入 "(" 时的函数签名提示。</summary>
        private void ShowSignatureInsight()
        {
            int caret = Editor.CaretOffset;
            string name = GetIdentifierBefore(caret - 1);
            if (string.IsNullOrEmpty(name)) return;

            string module = null;
            int idStart = caret - 1 - name.Length;
            if (idStart > 0 && (Editor.Document.GetCharAt(idStart - 1) == '.' || Editor.Document.GetCharAt(idStart - 1) == ':'))
                module = GetIdentifierBefore(idStart - 1);

            LuaSymbol symbol = module != null
                ? LuaApi.FindMember(module, name) ?? LuaApi.Find(name)
                : LuaApi.Find(name) ?? LuaDocumentAnalyzer.Analyze(Editor.Text).FirstOrDefault(s => s.Name == name);

            if (symbol == null || symbol.Kind == SymbolKind.Keyword) return;

            _insightWindow?.Close();
            var panel = new StackPanel { MaxWidth = 460 };
            panel.Children.Add(new TextBlock
            {
                Text = symbol.Signature,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });
            if (!string.IsNullOrWhiteSpace(symbol.Description))
                panel.Children.Add(new TextBlock
                {
                    Text = symbol.Description,
                    Margin = new Thickness(0, 4, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55))
                });

            _insightWindow = new InsightWindow(Editor.TextArea) { Content = panel };
            _insightWindow.Closed += (s, e) => _insightWindow = null;
            _insightWindow.Show();
        }

        #endregion

        #region 变量取值（单击 / 悬停）

        private void InspectAtCaret(bool showPopup)
        {
            if (Editor.Document == null) return;

            string path = GetQualifiedNameAt(Editor.CaretOffset);
            if (string.IsNullOrEmpty(path))
            {
                TxtInspect.Text = "—";
                if (showPopup) VarPopup.IsOpen = false;
                return;
            }

            VarInfo info = ResolvePath(path);
            LuaSymbol api = info == null ? LuaApi.Find(path.Split('.', ':').Last()) : null;

            if (info != null)
            {
                TxtInspect.Text = $"{path} = {info.Value}";
                if (showPopup) ShowVarPopup(path, info.Value, $"类型：{info.TypeName}", info.Scope);
            }
            else if (api != null)
            {
                TxtInspect.Text = $"{path}（{api.Signature}）";
                if (showPopup) ShowVarPopup(path, api.Signature, api.Description, "标准库");
            }
            else
            {
                LuaSymbol doc = LuaDocumentAnalyzer.Analyze(Editor.Text).FirstOrDefault(s => s.Name == path);
                string hint = _session != null && _session.IsBusy
                    ? "当前作用域内没有该变量"
                    : "尚无运行时值，运行或单步后再查看";
                TxtInspect.Text = $"{path} = ?  （{hint}）";
                if (showPopup)
                    ShowVarPopup(path, "?", doc != null ? doc.Signature + " · " + hint : hint,
                        doc != null ? "本文件" : "未知");
            }
        }

        private void ShowVarPopup(string name, string value, string type, string scope)
        {
            PopupName.Text = name;
            PopupValue.Text = value;
            PopupType.Text = type;
            PopupScope.Text = string.IsNullOrEmpty(scope) ? "变量" : scope;

            TextView view = Editor.TextArea.TextView;
            view.EnsureVisualLines();

            var location = Editor.Document.GetLocation(Editor.CaretOffset);
            Point pos = view.GetVisualPosition(new TextViewPosition(location), VisualYPosition.LineBottom) - view.ScrollOffset;
            Point origin = view.TransformToAncestor(Editor).Transform(new Point(0, 0));

            VarPopup.HorizontalOffset = Math.Max(0, origin.X + pos.X - 12);
            VarPopup.VerticalOffset = origin.Y + pos.Y + 6;
            VarPopup.IsOpen = true;
        }

        private void TextView_MouseHover(object sender, MouseEventArgs e)
        {
            TextView view = Editor.TextArea.TextView;
            var mousePos = e.GetPosition(view);
            TextViewPosition? pos = view.GetPosition(mousePos + view.ScrollOffset);
            if (pos == null)
            {
                _hoverTip.IsOpen = false;
                return;
            }

            int offset = Editor.Document.GetOffset(pos.Value.Location);
            string path = GetQualifiedNameAt(offset);
            if (string.IsNullOrEmpty(path))
            {
                _hoverTip.IsOpen = false;
                return;
            }

            var panel = new StackPanel { MaxWidth = 440 };
            VarInfo info = ResolvePath(path);
            string simple = path.Split('.', ':').Last();
            LuaSymbol api = LuaApi.Find(simple) ?? LuaDocumentAnalyzer.Analyze(Editor.Text).FirstOrDefault(s => s.Name == simple);

            if (info != null)
            {
                panel.Children.Add(Header($"{path} = {info.Value}"));
                panel.Children.Add(Sub($"{info.Scope}变量 · 类型 {info.TypeName}"));
                if (info.Children.Count > 0)
                {
                    var sb = new StringBuilder();
                    foreach (VarInfo child in info.Children.Take(8))
                        sb.AppendLine($"  {child.Name} = {child.Value}");
                    if (info.Children.Count > 8) sb.AppendLine("  …");
                    panel.Children.Add(Sub(sb.ToString().TrimEnd()));
                }
            }
            else if (api != null)
            {
                panel.Children.Add(Header(api.Signature));
                panel.Children.Add(Sub(api.Description));
            }
            else
            {
                _hoverTip.IsOpen = false;
                return;
            }

            _hoverTip.Content = panel;
            _hoverTip.IsOpen = true;
            e.Handled = true;
        }

        private static TextBlock Header(string text) => new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily("Consolas"),
            FontWeight = FontWeights.SemiBold,
            FontSize = 12.5,
            TextWrapping = TextWrapping.Wrap
        };

        private static TextBlock Sub(string text) => new TextBlock
        {
            Text = text,
            Margin = new Thickness(0, 3, 0, 0),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11.5,
            Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x5D, 0x66)),
            TextWrapping = TextWrapping.Wrap
        };

        /// <summary>解析 a.b.c 形式的变量路径。</summary>
        private VarInfo ResolvePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            string[] parts = path.Split('.', ':');
            if (!_varIndex.TryGetValue(parts[0], out VarInfo current)) return null;

            for (int i = 1; i < parts.Length; i++)
            {
                VarInfo next = current.Children.FirstOrDefault(c => c.Name == parts[i]);
                if (next == null) return null;
                current = next;
            }

            return current;
        }

        private int FindWordStart(int offset)
        {
            int start = offset;
            while (start > 0)
            {
                char c = Editor.Document.GetCharAt(start - 1);
                if (char.IsLetterOrDigit(c) || c == '_') start--;
                else break;
            }
            return start;
        }

        private string GetIdentifierBefore(int offset)
        {
            int end = offset;
            int start = end;
            while (start > 0)
            {
                char c = Editor.Document.GetCharAt(start - 1);
                if (char.IsLetterOrDigit(c) || c == '_') start--;
                else break;
            }
            return end > start ? Editor.Document.GetText(start, end - start) : string.Empty;
        }

        /// <summary>取光标 / 鼠标处的完整变量路径，如 player.name。</summary>
        private string GetQualifiedNameAt(int offset)
        {
            TextDocument doc = Editor.Document;
            if (doc == null || doc.TextLength == 0) return null;

            offset = Math.Max(0, Math.Min(offset, doc.TextLength));

            int start = offset, end = offset;
            while (start > 0 && IsIdentChar(doc.GetCharAt(start - 1))) start--;
            while (end < doc.TextLength && IsIdentChar(doc.GetCharAt(end))) end++;
            if (end <= start) return null;

            string word = doc.GetText(start, end - start);
            if (word.Length == 0 || char.IsDigit(word[0])) return null;
            if (LuaApi.Keywords.Contains(word)) return null;

            // 向前拼接 a.b.c
            int p = start;
            var prefix = new StringBuilder();
            while (p > 1 && (doc.GetCharAt(p - 1) == '.' || doc.GetCharAt(p - 1) == ':'))
            {
                char sep = doc.GetCharAt(p - 1);
                int q = p - 1;
                while (q > 0 && IsIdentChar(doc.GetCharAt(q - 1))) q--;
                if (q == p - 1) break;
                prefix.Insert(0, doc.GetText(q, p - 1 - q) + sep);
                p = q;
            }

            return prefix + word;
        }

        private static bool IsIdentChar(char c) => char.IsLetterOrDigit(c) || c == '_';

        #endregion

        #region 调试会话

        private void StartSession(bool breakAtEntry)
        {
            if (_session != null && _session.IsBusy) return;

            ClearRuntimeMarkers();
            _log.Clear();

            _session = new LuaDebugSession();
            _session.Log += OnSessionLog;
            _session.Paused += OnSessionPaused;
            _session.Ended += OnSessionEnded;
            _session.SetBreakpoints(_bpMargin.Breakpoints);

            AppendLog(breakAtEntry ? "▶ 开始调试（停在第一条语句）" : "▶ 开始运行", LogKind.Info);
            SetSessionState(SessionState.Running);
            _session.Start(Editor.Text, breakAtEntry);
        }

        private void OnSessionLog(string text, LogKind kind) =>
            Dispatcher.BeginInvoke(new Action(() => AppendLog(text, kind)));

        private void OnSessionPaused(PauseInfo info) => Dispatcher.BeginInvoke(new Action(() =>
        {
            _currentLineRenderer.Line = info.Line;
            _bpMargin.SetCurrentLine(info.Line);
            Editor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);

            if (info.Line > 0 && info.Line <= Editor.Document.LineCount)
            {
                Editor.ScrollToLine(info.Line);
                Editor.TextArea.Caret.Line = info.Line;
                Editor.TextArea.Caret.Column = 1;
            }

            RebuildVariables(info.Locals, info.Globals);
            CallStackList.ItemsSource = info.CallStack;

            if (info.IsError)
            {
                _errorLineRenderer.Line = info.Line;
                AppendLog("✖ " + info.Message, LogKind.Error);
            }

            SetSessionState(SessionState.Paused, info.Line);
            InspectAtCaret(false);
        }));

        private void OnSessionEnded(ExecutionEndedInfo info) => Dispatcher.BeginInvoke(new Action(() =>
        {
            _currentLineRenderer.Line = 0;
            _bpMargin.SetCurrentLine(0);
            Editor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);

            if (info.IsError)
            {
                AppendLog("✖ " + info.Message, LogKind.Error);
                if (info.ErrorLine > 0)
                {
                    _errorLineRenderer.Line = info.ErrorLine;
                    Editor.ScrollToLine(info.ErrorLine);
                }
            }
            else if (info.Terminated)
            {
                AppendLog("■ " + info.Message, LogKind.Info);
            }
            else
            {
                AppendLog($"✔ {info.Message}（耗时 {info.ElapsedMs} ms）", LogKind.Success);
            }

            RebuildVariables(new List<VarInfo>(), info.Globals);
            CallStackList.ItemsSource = null;
            SetSessionState(SessionState.Idle);
            InspectAtCaret(false);
        }));

        private void RebuildVariables(List<VarInfo> locals, List<VarInfo> globals)
        {
            _varIndex.Clear();
            foreach (VarInfo g in globals) _varIndex[g.Name] = g;
            foreach (VarInfo l in locals) _varIndex[l.Name] = l;   // 局部变量优先

            var roots = new List<VarInfo>();

            var localRoot = new VarInfo
            {
                Name = "局部变量",
                Value = locals.Count + " 项",
                TypeName = string.Empty,
                IsExpanded = true
            };
            foreach (VarInfo l in locals) localRoot.Children.Add(l);

            var globalRoot = new VarInfo
            {
                Name = "全局变量",
                Value = globals.Count + " 项",
                TypeName = string.Empty,
                IsExpanded = true
            };
            foreach (VarInfo g in globals) globalRoot.Children.Add(g);

            roots.Add(localRoot);
            roots.Add(globalRoot);
            VarTree.ItemsSource = roots;
        }

        private void ClearRuntimeMarkers()
        {
            _currentLineRenderer.Line = 0;
            _errorLineRenderer.Line = 0;
            _bpMargin.SetCurrentLine(0);
            Editor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        }

        private void SetSessionState(SessionState state, int line = 0)
        {
            bool idle = state == SessionState.Idle;
            bool paused = state == SessionState.Paused;
            bool running = state == SessionState.Running;

            BtnRun.IsEnabled = idle || paused;
            RunLabel.Text = paused ? "继续" : "运行";
            BtnPause.IsEnabled = running;
            BtnStop.IsEnabled = running || paused;
            BtnStepOver.IsEnabled = idle || paused;
            BtnStepIn.IsEnabled = idle || paused;
            BtnStepOut.IsEnabled = paused;
            Editor.IsReadOnly = !idle;

            if (idle)
            {
                StateDot.Fill = new SolidColorBrush(Color.FromRgb(0x9A, 0xA5, 0xB1));
                TxtState.Text = "就绪";
            }
            else if (running)
            {
                StateDot.Fill = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
                TxtState.Text = "运行中…";
            }
            else
            {
                StateDot.Fill = new SolidColorBrush(Color.FromRgb(0xE8, 0x9C, 0x0E));
                TxtState.Text = $"已暂停 · 第 {line} 行";
            }
        }

        private void Resume(DebuggerAction.ActionType action)
        {
            if (_session == null) return;

            if (_session.State == SessionState.Paused)
            {
                ClearRuntimeMarkers();
                SetSessionState(SessionState.Running);
                _session.Resume(action);
            }
        }

        #endregion

        #region 工具栏 / 快捷键

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            if (_session != null && _session.State == SessionState.Paused)
                Resume(DebuggerAction.ActionType.Run);
            else
                StartSession(false);
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            _session?.RequestPause();
            AppendLog("… 已请求中断，将在下一条语句处暂停", LogKind.Info);
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e) => _session?.Stop();

        private void BtnStepOver_Click(object sender, RoutedEventArgs e)
        {
            if (_session == null || !_session.IsBusy) StartSession(true);
            else Resume(DebuggerAction.ActionType.StepOver);
        }

        private void BtnStepIn_Click(object sender, RoutedEventArgs e)
        {
            if (_session == null || !_session.IsBusy) StartSession(true);
            else Resume(DebuggerAction.ActionType.StepIn);
        }

        private void BtnStepOut_Click(object sender, RoutedEventArgs e) =>
            Resume(DebuggerAction.ActionType.StepOut);

        private void BtnBreakpoint_Click(object sender, RoutedEventArgs e) => ToggleBreakpointAtCaret();

        private void BtnClearBreakpoints_Click(object sender, RoutedEventArgs e)
        {
            _bpMargin.ClearAll();
            AppendLog("已清除全部断点", LogKind.Info);
        }

        private void BtnHint_Click(object sender, RoutedEventArgs e)
        {
            Editor.Focus();
            ShowWordCompletion(true);
        }

        private void BtnExample_Click(object sender, RoutedEventArgs e)
        {
            if (LuaItem == null)
            {
                AppendLog("请先选择或新建一个脚本流程，再插入示例", LogKind.Info);
                return;
            }

            _settingText = true;
            Editor.Text = FlowItem.DefaultLuaTemplate;
            _settingText = false;
            LuaItem.LuaSource = FlowItem.DefaultLuaTemplate;
            CheckSyntaxNow();
            AppendLog("已插入示例脚本，可运行 / 单步查看效果", LogKind.Info);
        }

        private void BtnClearOutput_Click(object sender, RoutedEventArgs e) => _log.Clear();

        private void ToggleBreakpointAtCaret()
        {
            int line = Editor.TextArea.Caret.Line;
            bool added = _bpMargin.Toggle(line);
            AppendLog(added ? $"● 已在第 {line} 行设置断点" : $"○ 已取消第 {line} 行的断点", LogKind.Info);
        }

        private void LuaEditorView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            switch (e.Key)
            {
                case Key.F5:
                    if (shift) BtnStop_Click(null, null);
                    else BtnRun_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.F6:
                    BtnPause_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.F9:
                    ToggleBreakpointAtCaret();
                    e.Handled = true;
                    break;
                case Key.F10:
                    BtnStepOver_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.F11:
                    if (shift) BtnStepOut_Click(null, null);
                    else BtnStepIn_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.Space:
                    if (ctrl)
                    {
                        ShowWordCompletion(true);
                        e.Handled = true;
                    }
                    break;
                case Key.Escape:
                    VarPopup.IsOpen = false;
                    break;
            }
        }

        #endregion

        #region 状态与输出

        private void AppendLog(string text, LogKind kind)
        {
            Brush brush = kind switch
            {
                LogKind.Error => BrushError,
                LogKind.Success => BrushSuccess,
                LogKind.Info => BrushInfo,
                _ => BrushOutput
            };

            _log.Add(new LogEntry { Text = text ?? string.Empty, Brush = brush });
            if (_log.Count > 2000) _log.RemoveAt(0);
            OutputList.ScrollIntoView(_log[_log.Count - 1]);
        }

        private void UpdateCaretStatus() =>
            TxtCaret.Text = $"行 {Editor.TextArea.Caret.Line}，列 {Editor.TextArea.Caret.Column}";

        private void UpdateBreakpointCount()
        {
            int n = _bpMargin.Breakpoints.Count;
            TxtBreakCount.Text = "断点 " + n;
        }

        private void CheckSyntaxNow()
        {
            if (Editor.Document == null) return;
            if (_session != null && _session.IsBusy) return; // 调试中由暂停现场负责

            var (ok, line, msg) = LuaDebugSession.CheckSyntax(Editor.Text);
            if (ok)
            {
                _errorLineRenderer.Line = 0;
                TxtDiagnostics.Text = "语法正确";
                TxtDiagnostics.Foreground = BrushInfo;
            }
            else
            {
                _errorLineRenderer.Line = line;
                TxtDiagnostics.Text = msg;
                TxtDiagnostics.Foreground = BrushError;
            }
        }

        #endregion
    }
}
