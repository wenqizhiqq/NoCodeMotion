# -*- coding: utf-8 -*-
import re
def find_close(text, open_start):
    m = re.match(r'<\s*([\w:]+)', text[open_start:])
    if not m: return -1
    tag = m.group(1)
    gt = text.find('>', open_start)
    if gt==-1: return -1
    if text[gt-1]=='/':
        return gt+1
    i = gt+1; depth=1
    while i < len(text):
        lt = text.find('<', i)
        if lt==-1: return -1
        if text.startswith('</', lt):
            cm = re.match(r'</\s*([\w:]+)', text[lt:])
            if cm and cm.group(1)==tag:
                depth-=1
                if depth==0:
                    return text.find('>', lt)+1
                i = text.find('>', lt)+1
            else:
                i = text.find('>', lt)+1
        elif text.startswith('<!--', lt):
            end = text.find('-->', lt)
            i = (end+3) if end!=-1 else i+1
        else:
            om = re.match(r'<\s*([\w:]+)', text[lt:])
            if om:
                egt = text.find('>', lt)
                if egt!=-1 and text[egt-1]=='/':
                    i = egt+1; continue
                depth+=1; i = egt+1
            else:
                i = lt+1
    return -1

def read_text(f):
    raw=open(f,'rb').read()
    bom=raw[:3]==b'\xef\xbb\xbf'
    body=raw[3:] if bom else raw
    try: s=body.decode('utf-8')
    except Exception: s=body.decode('gbk')
    return s.replace('\r\n','\n')

for f in ["CylinderPage","CameraPage","CommPage","AxisPage","TrayPage"]:
    s=read_text("D:/wqz/code/NoCodeMotion/Views/%s.xaml"%f)
    pos=0; idx=0; fails=[]
    while True:
        dp=s.find('<DockPanel',pos)
        if dp==-1: break
        c=find_close(s,dp)
        if c==-1:
            # show context
            seg=s[dp:dp+80].replace('\n',' ')
            fails.append((idx, seg))
        idx+=1; pos=dp+1
    print("%-12s totalDock=%d fails=%d"%(f, idx, len(fails)))
    for fi,seg in fails:
        print("   fail#%d: %s"%(fi, seg))
