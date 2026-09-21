p = 'Services/AiProjectExchange.cs'
b = open(p, 'rb').read()
assert b[:3] == b'\xef\xbb\xbf', 'BOM missing'
t = b.decode('utf-8-sig')
assert t.count('\r') == t.count('\r\n'), 'lone CR present'
lines = t.split('\r\n')

# --- 1) 补 using NoCodeMotion.Models.NodeGraph; ---
anchor = 'using NoCodeMotion.Models;'
i = lines.index(anchor)
assert 'using NoCodeMotion.Models.NodeGraph;' not in t, 'already added'
lines.insert(i + 1, 'using NoCodeMotion.Models.NodeGraph;')

# --- 2) 在 class 收尾前插入流程级交换代码块 ---
fs = next(i for i, l in enumerate(lines) if '作者保留所有权利' in l)
assert lines[fs - 1] == '}', repr(lines[fs - 1])
assert lines[fs - 2] == '    }', repr(lines[fs - 2])

block = open('.bt/flow_exchange_block.cs', 'rb').read().decode('utf-8')
block_lines = block.replace('\r\n', '\n').split('\n')
while block_lines and block_lines[-1] == '':
    block_lines.pop()

lines[fs - 2:fs - 2] = [''] + block_lines

out = '\r\n'.join(lines)
assert '\n\n\n\n' not in out.replace('\r\n', '\n').replace('\n\n\n', ''), 'sanity'
open(p, 'wb').write(b'\xef\xbb\xbf' + out.encode('utf-8'))

# --- 复核 ---
b2 = open(p, 'rb').read()
t2 = b2.decode('utf-8-sig')
print('bom', b2[:3] == b'\xef\xbb\xbf')
print('crlf', b2.count(b'\r\n'), 'bareLF', b2.count(b'\n') - b2.count(b'\r\n'))
print('braces %+d parens %+d' % (t2.count('{') - t2.count('}'), t2.count('(') - t2.count(')')))
print('nodegraph using:', 'using NoCodeMotion.Models.NodeGraph;' in t2)
for probe in ['public static string ExportFlowJson(FlowItem? flow)',
              'public static string BuildFlowPrompt(FlowItem? flow, string? requirements)',
              'public static string ApplyFlowGenerated(FlowItem? target, IList<FlowItem>? sink, string json)']:
    print('has:', probe in t2, '|', probe)
print('lines', t2.count('\n') + 1)
