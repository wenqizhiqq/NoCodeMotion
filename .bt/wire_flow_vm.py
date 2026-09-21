# -*- coding: utf-8 -*-
def load(p):
    b = open(p, 'rb').read()
    assert b[:3] == b'\xef\xbb\xbf', p + ' BOM missing'
    t = b.decode('utf-8-sig')
    assert t.count('\r') == t.count('\r\n'), p + ' lone CR'
    return t

def save(p, t):
    open(p, 'wb').write(b'\xef\xbb\xbf' + t.encode('utf-8'))
    b = open(p, 'rb').read()
    t2 = b.decode('utf-8-sig')
    print('  -> crlf %d bareLF %d braces %+d parens %+d' % (
        b.count(b'\r\n'), b.count(b'\n') - b.count(b'\r\n'),
        t2.count('{') - t2.count('}'), t2.count('(') - t2.count(')')))

def patch(p, pairs):
    t = load(p)
    print('==', p)
    for old, new in pairs:
        old = old.replace('\n', '\r\n')
        new = new.replace('\n', '\r\n')
        n = t.count(old)
        if n == 0 and t.count(new) == 1:
            print('   skip (already applied)')
            continue
        assert n == 1, 'anchor count %d for %r' % (n, old[:70])
        t = t.replace(old, new)
    save(p, t)

# ============ 1) AiProjectExchange: 补 CountFlowsInJson ============
patch('Services/AiProjectExchange.cs', [(
'''        /// <summary>把 AI 返回的一个流程对象写进 FlowItem（集合一律原地清空重填，保证界面绑定不断）。</summary>''',
'''        /// <summary>预览：剪贴板里有多少个流程对象（0 = 没识别到 / 解析失败）。供粘贴前的确认弹窗选文案用。</summary>
        public static int CountFlowsInJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return 0;
            try
            {
                using var doc = JsonDocument.Parse(StripCodeFence(json.Trim()), new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array) return root.GetArrayLength();
                if (root.ValueKind != JsonValueKind.Object) return 0;
                foreach (var alias in new[] { "流程", "flows", "flow", "流程列表" })
                    if (root.TryGetProperty(alias, out var arr) && arr.ValueKind == JsonValueKind.Array)
                        return arr.GetArrayLength();
                return 1;   // 根对象本身就是一个流程
            }
            catch { return 0; }
        }

        /// <summary>把 AI 返回的一个流程对象写进 FlowItem（集合一律原地清空重填，保证界面绑定不断）。</summary>''')])

# ============ 2) 提示词里「我的需求」占位说明更可操作 ============
patch('Services/AiProjectExchange.cs', [(
'''                sb.AppendLine("【我的需求】（尚未填写，请按当前流程的用途合理推断并补全成一套完整流程）");''',
'''                sb.AppendLine("【我的需求】（请在下面补一句需求，例如「改成 3 工位循环、每件拍照一次、超时就报警」；");
                sb.AppendLine(" 不想改就把这两行删掉，AI 会按当前流程的用途补全成一套完整流程）");''')])

# ============ 3) FlowViewModel: 命令声明 ============
patch('ViewModels/FlowViewModel.cs', [(
'''        /// <summary>「清空」：删除全部流程，弹窗 ConfirmDialog 二次确认（避免误删）。</summary>
        public ICommand DeleteAllCommand { get; }
''',
'''        /// <summary>「清空」：删除全部流程，弹窗 ConfirmDialog 二次确认（避免误删）。</summary>
        public ICommand DeleteAllCommand { get; }

        /// <summary>
        /// 【复制JSON】把「当前流程 JSON + 本工程可用名称 + 输出契约 + 待填需求」整段提示词写进剪贴板，
        /// 粘到豆包 / WorkBuddy 等 AI 对话软件即可让它生成流程。未选中流程时也能用（AI 会新生成一个）。
        /// </summary>
        public ICommand CopyFlowJsonCommand { get; }

        /// <summary>【粘贴生成】读取剪贴板里 AI 返回的流程 JSON 并写入当前流程（或新增流程）。</summary>
        public ICommand PasteFlowJsonCommand { get; }

        /// <summary>粘贴生成覆盖了当前流程内容后触发：供 FlowPage 让节点图编辑器按新的 GraphJson 重新加载。</summary>
        public event Action? FlowContentReplaced;
''')])

# ============ 4) FlowViewModel: 构造函数注册命令 ============
patch('ViewModels/FlowViewModel.cs', [(
'''            DeleteAllCommand = new RelayCommand(_ => DeleteAll(), _ => Items != null && Items.Count > 0);
''',
'''            DeleteAllCommand = new RelayCommand(_ => DeleteAll(), _ => Items != null && Items.Count > 0);

            // 复制JSON / 粘贴生成：两个按钮都不依赖「已选中流程」——未选中时复制的是「新生成一个流程」的提示词，
            // 粘贴则把 AI 返回的流程追加进工程，这样空工程里也能直接用。
            CopyFlowJsonCommand = new RelayCommand(_ => CopyFlowJson());
            PasteFlowJsonCommand = new RelayCommand(_ => PasteFlowJson());
''')])

# ============ 5) FlowViewModel: 两个方法实现（插在 DeleteAll 之后）============
patch('ViewModels/FlowViewModel.cs', [(
'''            // 快照列表逐个 Remove（直接 Items.Clear() 会绕过单项 OnItemsChanged 的取消订阅逻辑）
            foreach (var item in Items.ToList()) Items.Remove(item);
            SelectedItem = null;
        }
''',
'''            // 快照列表逐个 Remove（直接 Items.Clear() 会绕过单项 OnItemsChanged 的取消订阅逻辑）
            foreach (var item in Items.ToList()) Items.Remove(item);
            SelectedItem = null;
        }

        // ==================== 复制JSON / 粘贴生成（与 AI 往返） ====================

        /// <summary>
        /// 【复制JSON】把「当前流程 JSON + 本工程已配置名称 + 输出契约 + 待填需求」整段写进剪贴板。
        /// 粘到 AI 对话软件里补一句需求，AI 就会回一个流程 JSON。
        /// 未选中流程也能用——此时提示词会让 AI 新生成一个流程。
        /// </summary>
        private void CopyFlowJson()
        {
            try
            {
                var prompt = AiProjectExchange.BuildFlowPrompt(SelectedItem, null);
                // 用全限定名：本工程隐式 using 不含 System.Windows，Clipboard 必须显式定位
                System.Windows.Clipboard.SetText(prompt);
                StatusBarService.ReportInfo(SelectedItem == null
                    ? "流程 JSON 提示词已复制（当前未选中流程，AI 会新生成一个）。粘贴到 AI 对话软件即可。"
                    : $"流程「{SelectedItem.Name}」的 JSON 提示词已复制。粘贴到 AI 对话软件并补一句需求即可生成流程。");
            }
            catch (Exception ex)
            {
                StatusBarService.ReportException("复制流程 JSON 失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 【粘贴生成】读取剪贴板里 AI 返回的流程 JSON 并写入：
        ///   · AI 只回 1 个流程且当前选中了流程 → 覆盖当前流程（保留原名称，工程里其它流程不受影响）；
        ///   · 回了多个流程 / 当前没选中流程 → 追加为新流程。
        /// 覆盖是破坏性操作，先弹窗二次确认。
        /// </summary>
        private void PasteFlowJson()
        {
            string text;
            try
            {
                if (!System.Windows.Clipboard.ContainsText())
                {
                    StatusBarService.ReportException("剪贴板没有文本内容。请先复制 AI 返回的流程 JSON。");
                    return;
                }
                text = System.Windows.Clipboard.GetText();
            }
            catch (Exception ex)
            {
                StatusBarService.ReportException("读取剪贴板失败：" + ex.Message);
                return;
            }

            int count = AiProjectExchange.CountFlowsInJson(text);
            if (count <= 0)
            {
                StatusBarService.ReportException("剪贴板里没识别到流程 JSON。请确认复制的是 AI 返回的完整内容（以 { 或 [ 开头）。");
                return;
            }

            bool overwrite = count == 1 && SelectedItem != null;
            string msg = overwrite
                ? $"将用 AI 返回的内容【覆盖】当前流程「{SelectedItem!.Name}」的\\n全部步骤 / 脚本 / 节点图。\\n\\n" +
                  "（流程名称保持不变，工程里其它流程不受影响）\\n\\n是否继续？"
                : $"将把 AI 返回的 {count} 个流程【新增】到当前工程。\\n\\n是否继续？";

            var dlg = new Views.ConfirmDialog("粘贴生成流程", msg, "粘贴生成")
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };
            if (dlg.ShowDialog() != true)
            {
                StatusBarService.ReportInfo("已取消粘贴生成。");
                return;
            }

            int before = Items.Count;
            var result = AiProjectExchange.ApplyFlowGenerated(SelectedItem, Items, text);

            if (overwrite)
            {
                // 表格步骤 / 视觉步骤是原地清空重填，绑定会自动跟上；
                // 节点图的 GraphJson 是字符串，需要显式通知宿主页重新加载。
                FlowContentReplaced?.Invoke();
            }
            else if (Items.Count > before)
            {
                // 选中第一个新增的流程，让用户一眼看到生成结果
                SelectedItem = Items[before];
            }

            StatusBarService.ReportInfo(result);
        }
''')])

print()
print('done')
