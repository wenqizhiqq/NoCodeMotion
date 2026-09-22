# -*- coding: utf-8 -*-
import re
F='Views/AxisPage.xaml'
raw=open(F,'rb').read(); bom=raw[:3]==b'\xef\xbb\xbf'
s=(raw[3:] if bom else raw).decode('utf-8').replace('\r\n','\n')
LEAF_SELF = re.compile(r'<(TextBox|CheckBox|ComboBox|ListBox)\b[^>]*?/>')
LEAF_PAIR = re.compile(r'<(ComboBox|ListBox|TextBox)\b[^>]*?>(?:.*?</\1>)?', re.DOTALL)
print('LEAF_SELF count', len(LEAF_SELF.findall(s)))
print('LEAF_PAIR count', len(LEAF_PAIR.findall(s)))
# show first few LEAF_PAIR matches
for i,m in enumerate(LEAF_PAIR.finditer(s)):
    print('PAIR',i,m.group(0)[:80].replace('\n',' '))
    if i>4: break
for i,m in enumerate(LEAF_SELF.finditer(s)):
    print('SELF',i,m.group(0)[:80].replace('\n',' '))
    if i>4: break
