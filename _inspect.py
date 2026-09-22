# -*- coding: utf-8 -*-
import os
files = ['Views/Controls/Field.cs'] + ['Views/%s.xaml'%n for n in ['AxisPage','IoPage','CylinderPage','VariablePage','CommPage','TrayPage','CameraPage']]
for f in files:
    raw=open(f,'rb').read()
    bom=raw[:3]==b'\xef\xbb\xbf'
    body=raw[3:] if bom else raw
    try:
        body.decode('utf-8'); enc='utf-8'
    except Exception:
        enc='gbk'
    crlf=b'\r\n' in raw
    print(f, 'BOM' if bom else 'noBOM', enc, 'CRLF' if crlf else 'LF', 'size', len(raw))
