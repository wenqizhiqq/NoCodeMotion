# -*- coding: utf-8 -*-
def load(p):
    b = open(p, 'rb').read()
    bom = b[:3] == b'\xef\xbb\xbf'
    t = b.decode('utf-8-sig')
    assert t.count('\r') == t.count('\r\n'), p + ' lone CR'
    return t, bom

def save(p, t, bom):
    data = t.encode('utf-8')
    if bom:
        data = b'\xef\xbb\xbf' + data
    open(p, 'wb').write(data)
    b = open(p, 'rb').read()
    t2 = b.decode('utf-8-sig')
    print('  -> crlf %d bareLF %d braces %+d parens %+d' % (
        b.count(b'\r\n'), b.count(b'\n') - b.count(b'\r\n'),
        t2.count('{') - t2.count('}'), t2.count('(') - t2.count(')')))

def patch(p, pairs):
    t, bom = load(p)
    print('==', p)
    for old, new in pairs:
        old = old.replace('\n', '\r\n')
        new = new.replace('\n', '\r\n')
        n = t.count(old)
        if n == 0 and t.count(new) == 1:
            print('   skip (already applied)')
            continue
        assert n == 1, 'anchor count %d for %r' % (n, old[:80])
        t = t.replace(old, new)
    save(p, t, bom)

# ============ 1) FlowPage.xaml：标题行加「复制JSON / 粘贴生成」两个按钮 ============
patch('Views/FlowPage.xaml', [(
'''                    <!-- 右上角「当前类型」chip（SelectedItem.Kind：表格 / Lua 文本由 enum.ToString 直出） -->
                    <Border DockPanel.Dock="Right" Padding="10,3" CornerRadius="10" Background="{StaticResource AccentSoftBrush}" VerticalAlignment="Center">
                        <TextBlock Text="{Binding SelectedItem.Kind}" FontSize="12" FontWeight="SemiBold" Foreground="{StaticResource AccentDarkBrush}"/>
                    </Border>
''',
'''                    <!-- 右上角「当前类型」chip（SelectedItem.Kind：表格 / Lua 文本由 enum.ToString 直出） -->
                    <Border DockPanel.Dock="Right" Padding="10,3" CornerRadius="10" Background="{StaticResource AccentSoftBrush}" VerticalAlignment="Center">
                        <TextBlock Text="{Binding SelectedItem.Kind}" FontSize="12" FontWeight="SemiBold" Foreground="{StaticResource AccentDarkBrush}"/>
                    </Border>

                    <!-- 复制JSON / 粘贴生成：与 AI 对话软件往返生成流程。
                         放在 Detail 标题行而不是左侧工具栏，这样「运控 / 脚本 / 视觉 / 节点图」四种流程页
                         看到的是同一对按钮，不必每类各加一遍。
                         DockPanel 按声明顺序分配空间，所以这两个按钮声明在类型 chip 之后 → 显示在 chip 左侧。 -->
                    <StackPanel DockPanel.Dock="Right" Orientation="Horizontal"
                                VerticalAlignment="Center" Margin="0,0,10,0">
                        <Button Content="复制JSON" Style="{StaticResource TtBaseBtn}"
                                Command="{Binding CopyFlowJsonCommand}"
                                ToolTip="把当前流程的 JSON（含本工程可用名称、输出格式契约、需求待填行）复制到剪贴板。粘贴到豆包 / WorkBuddy 等 AI 并补一句需求，它就会返回流程 JSON。"/>
                        <Button Content="粘贴生成" Style="{StaticResource TtBaseBtn}" Margin="8,0,0,0"
                                Command="{Binding PasteFlowJsonCommand}"
                                ToolTip="读取剪贴板里 AI 返回的流程 JSON 并生成流程：只回 1 个流程时覆盖当前流程（名称保留），回多个时追加为新流程。"/>
                    </StackPanel>
''')])

# ============ 2) FlowPage.xaml.cs：订阅 FlowContentReplaced ============
patch('Views/FlowPage.xaml.cs', [(
'''            var vm = new FlowViewModel();
            DataContext = vm;
            vm.PropertyChanged += OnVmPropertyChanged;
''',
'''            var vm = new FlowViewModel();
            DataContext = vm;
            vm.PropertyChanged += OnVmPropertyChanged;
            // 「粘贴生成」覆盖当前流程内容后，节点图 / 视觉流程页需要显式重新加载（表格 / 脚本靠绑定自动跟上）
            vm.FlowContentReplaced += OnFlowContentReplaced;
'''), (
'''        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
''',
'''        /// <summary>
        /// 「粘贴生成」覆盖了当前流程内容后触发。
        /// 表格步骤与视觉步骤都是原地清空重填，绑定会自动跟上；但
        /// 节点图的 GraphJson 是字符串、视觉页的 SelectedStep 会指向已被清掉的旧对象，
        /// 这两处必须显式让宿主页重新加载，否则画布/参数面板会停在旧内容。
        /// </summary>
        private void OnFlowContentReplaced()
        {
            if (NodeGraphContent is NodeGraphPage ng) ng.Reload();
            if (VisionContent is VisualFlowPage vf) vf.Reload();
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
''')])

# ============ 3) NodeGraphPage：公开 Reload ============
patch('Views/NodeGraphPage.xaml.cs', [(
'''    private void ApplySelection()
''',
'''    /// <summary>
    /// 外部替换了当前流程的 GraphJson（流程页「粘贴生成」）后调用，按新的图重建画布。
    /// GraphJson 是普通字符串属性，没有「集合变了」这种信号可订阅，只能由宿主显式通知。
    /// </summary>
    public void Reload() => ApplySelection();

    private void ApplySelection()
''')])

# ============ 4) VisualFlowPage：公开 Reload ============
patch('Views/VisualFlowPage.xaml.cs', [(
'''        private void ApplySelection()
''',
'''        /// <summary>
        /// 外部替换了当前流程的视觉步骤（流程页「粘贴生成」）后调用。
        /// 必须先把 SelectedStep 清空再重新同步：粘贴会把 VisualSteps 原地清空重填，
        /// 旧 SelectedStep 指向的对象已不在集合里，参数面板会显示一条并不存在的步骤。
        /// </summary>
        public void Reload()
        {
            _vm.SelectedStep = null;
            ApplySelection();
        }

        private void ApplySelection()
''')])

print()
print('done')
