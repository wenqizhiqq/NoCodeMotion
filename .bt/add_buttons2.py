# -*- coding: utf-8 -*-
# 逐文件探测行尾/BOM —— 仓库里 CRLF 与 LF 混存，绝不能假设统一。
def load(p):
    b = open(p, 'rb').read()
    bom = b[:3] == b'\xef\xbb\xbf'
    t = b.decode('utf-8-sig')
    assert t.count('\r') == t.count('\r\n'), p + ' has lone CR'
    crlf = t.count('\r\n') > 0
    return t, bom, crlf

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
    t, bom, crlf = load(p)
    print('==', p, '(CRLF)' if crlf else '(LF)')
    for old, new in pairs:
        # 让 pattern 跟文件实际换行对齐（脚本里统一用 \n 写）
        if crlf:
            old = old.replace('\r\n', '\n').replace('\n', '\r\n')
            new = new.replace('\r\n', '\n').replace('\n', '\r\n')
        else:
            old = old.replace('\r\n', '\n')
            new = new.replace('\r\n', '\n')

        n = t.count(old)
        if n == 0 and t.count(new) == 1:
            print('   skip (already applied)')
            continue
        assert n == 1, 'anchor count %d for %r' % (n, old[:80])
        t = t.replace(old, new)
    save(p, t, bom)

# ============ 3) NodeGraphPage：公开 Reload（LF 文件）============
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

# ============ 4) VisualFlowPage：公开 Reload（CRLF 文件）============
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
