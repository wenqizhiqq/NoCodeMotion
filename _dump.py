# -*- coding: utf-8 -*-
files = [
 "D:/wqz/code/NoCodeMotion/Views/AxisPage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/IoPage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/CylinderPage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/VariablePage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/CommPage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/TrayPage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/CameraPage.xaml",
]
def read_text(f):
    raw = open(f,'rb').read()
    bom = raw[:3]==b'\xef\xbb\xbf'
    body = raw[3:] if bom else raw
    try: s=body.decode('utf-8')
    except Exception: s=body.decode('gbk')
    return s, ('utf-8-sig' if bom else 'utf-8')
out = []
for f in files:
    s,enc = read_text(f)
    out.append("="*80)
    out.append("FILE: %s  ENC: %s  LINES: %d" % (f, enc, s.count(chr(10))))
    out.append("="*80)
    out.append(s)
open("D:/wqz/code/NoCodeMotion/_dump.txt","w",encoding="utf-8").write("\n".join(out))
print("OK")
