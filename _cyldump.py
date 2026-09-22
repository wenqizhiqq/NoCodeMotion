# -*- coding: utf-8 -*-
def read_text(f):
    raw = open(f,'rb').read()
    bom = raw[:3]==b'\xef\xbb\xbf'
    body = raw[3:] if bom else raw
    try: s=body.decode('utf-8')
    except Exception: s=body.decode('gbk')
    return s
s = read_text("D:/wqz/code/NoCodeMotion/Views/CylinderPage.xaml")
open("D:/wqz/code/NoCodeMotion/_cyldump.txt","w",encoding="utf-8").write(s)
print("lines", s.count(chr(10)))
