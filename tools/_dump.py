# -*- coding: utf-8 -*-
"""按行号看某个源码文件的真实内容（BOM / 行尾一并显示）。

为什么需要它：本仓库大量文件是 UTF-8+BOM（多数还带 CRLF），内置 Read 工具会把它们
误判成「binary file」而拒绝显示 → 用这个脚本从仓库目录跑，读到的才是真内容。

用法（必须在仓库根目录跑）：
  python -u tools/_dump.py "Services/Vision/VisionEngine.cs:850-900"
  python -u tools/_dump.py Views/CameraPage.xaml
"""
import sys

def dump(p, a=None, b=None):
    try:
        raw = open(p, 'rb').read()
    except Exception as e:
        print('!! %s: %s' % (p, e)); return
    t = raw.decode('utf-8-sig')
    crlf = t.count('\r\n')
    lines = t.replace('\r\n', '\n').split('\n')
    print('########## %s  (bom=%s crlf=%d lines=%d) ##########'
          % (p, raw[:3] == b'\xef\xbb\xbf', crlf, len(lines)))
    lo = (a or 1) - 1
    hi = (b or len(lines))
    for i in range(lo, min(hi, len(lines))):
        print('%4d|%s' % (i + 1, lines[i]))
    print()


for arg in sys.argv[1:]:
    if ':' in arg and not arg[1:2] == ':':
        p, rng = arg.rsplit(':', 1)
        a, b = rng.split('-') if '-' in rng else (rng, rng)
        dump(p, int(a), int(b))
    else:
        dump(arg)
