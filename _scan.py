# -*- coding: utf-8 -*-
import re
files = ['Views/%s.xaml'%n for n in ['AxisPage','IoPage','CylinderPage','VariablePage','CommPage','TrayPage','CameraPage']]
for f in files:
    raw=open(f,'rb').read()
    bom=raw[:3]==b'\xef\xbb\xbf'
    body=raw[3:] if bom else raw
    s=body.decode('utf-8')
    labels=re.findall(r'AppleLabel\}"\s*Text="([^"]+)"', s)
    print('==== '+f)
    print(' | '.join(labels))
    print('count=%d'%len(labels))
