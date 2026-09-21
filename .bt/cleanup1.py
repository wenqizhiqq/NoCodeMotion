p = 'Services/AiProjectExchange.cs'
b = open(p, 'rb').read()
assert b[:3] == b'\xef\xbb\xbf'
t = b.decode('utf-8-sig')
assert t.count('\r') == t.count('\r\n')

pairs = [
    # 气缸默认运算：两个分支返回值相同，合并掉
    ('                case "气缸":\r\n'
     '                    return prop == "缩回到位" || prop == "伸出到位" ? "CylinderMove" : "CylinderMove";\r\n',
     '                case "气缸":\r\n'
     '                    return "CylinderMove";\r\n'),

    # NgDoc.FromJson 内部已经 Normalize 过，这里不必再来一次
    ('            // 交给 NgDoc 反序列化再序列化：顺带补齐缺省 Id、过滤非法连线\r\n'
     '            var doc = NgDoc.FromJson(g.GetRawText());\r\n'
     '            doc.Normalize();\r\n'
     '            return doc.ToJson();\r\n',
     '            // 交给 NgDoc 反序列化再序列化：FromJson 内部会 Normalize（按节点定义补齐/剔除属性），\r\n'
     '            // 所以 AI 写错属性名时会自动回落到默认值，而不是把脏数据存进工程。\r\n'
     '            return NgDoc.FromJson(g.GetRawText()).ToJson();\r\n'),
]

for old, new in pairs:
    n = t.count(old)
    assert n == 1, f'anchor count {n}: {old[:60]!r}'
    t = t.replace(old, new)

open(p, 'wb').write(b'\xef\xbb\xbf' + t.encode('utf-8'))
b2 = open(p, 'rb').read()
t2 = b2.decode('utf-8-sig')
print('crlf', b2.count(b'\r\n'), 'bareLF', b2.count(b'\n') - b2.count(b'\r\n'))
print('braces %+d parens %+d' % (t2.count('{') - t2.count('}'), t2.count('(') - t2.count(')')))
print('ok')
