# -*- coding: utf-8 -*-
import re

INPUT_TAGS = {'TextBox','ComboBox','CheckBox','ListBox','Grid','RadioButton',
              'ToggleButton','Slider','PasswordBox','DatePicker','ctl:NumSliderBox'}
INPUT_SUBSTR = ['<TextBox','<ComboBox','<CheckBox','<ListBox','<ctl:NumSliderBox',
                '<RadioButton','<Slider','<DatePicker','<PasswordBox','<ToggleButton']
NAME = r'[\w:.\-]+'

def read_text(f):
    raw=open(f,'rb').read()
    bom=raw[:3]==b'\xef\xbb\xbf'
    body=raw[3:] if bom else raw
    try: s=body.decode('utf-8')
    except Exception: s=body.decode('gbk')
    return s.replace('\r\n','\n'), ('utf-8-sig' if bom else 'utf-8'), ('\r\n' in s)

def write_text(f, s, enc, crlf):
    s=s.replace('\r\n','\n')
    if crlf: s=s.replace('\n','\r\n')
    raw=s.encode('utf-8' if enc!='gbk' else 'gbk')
    if enc=='utf-8-sig': raw=b'\xef\xbb\xbf'+raw
    open(f,'wb').write(raw)

def find_close(text, open_start):
    m=re.match(r'<\s*('+NAME+r')', text[open_start:])
    if not m: return -1
    gt=text.find('>', open_start)
    if gt==-1: return -1
    if text[gt-1]=='/': return gt+1
    i=gt+1; stack=[m.group(1)]
    while i<len(text):
        lt=text.find('<', i)
        if lt==-1: return -1
        if text.startswith('<!--', lt):
            end=text.find('-->', lt); i=(end+3) if end!=-1 else lt+4; continue
        if text.startswith('</', lt):
            cm=re.match(r'</\s*('+NAME+r')', text[lt:])
            name=cm.group(1) if cm else None
            if stack and stack[-1]==name:
                stack.pop()
            elif name in stack:
                while stack and stack[-1]!=name: stack.pop()
                if stack: stack.pop()
            i=text.find('>', lt)+1
            if not stack: return i
            continue
        om=re.match(r'<\s*('+NAME+r')', text[lt:])
        if om:
            egt=text.find('>', lt)
            if egt!=-1 and text[egt-1]=='/':
                i=egt+1; continue
            stack.append(om.group(1)); i=egt+1
        else:
            i=lt+1
    return -1

def is_input_tag(t): return t in INPUT_TAGS
def contains_input(elem): return any(sub in elem for sub in INPUT_SUBSTR)

def find_right_input(block):
    dp_gt=block.find('>')
    if dp_gt==-1: return None
    i=dp_gt+1; n=len(block); depth=0
    while i<n:
        lt=block.find('<', i)
        if lt==-1: break
        if block.startswith('<!--', lt):
            end=block.find('-->', lt); i=(end+3) if end!=-1 else lt+4; continue
        if block.startswith('</', lt):
            depth-=1; i=block.find('>', lt)+1; continue
        om=re.match(r'<\s*('+NAME+r')', block[lt:])
        if not om: i=lt+1; continue
        name=om.group(1); egt=block.find('>', lt); selfclose=block[egt-1]=='/'
        if depth==0:
            seg=block[lt:egt]
            if 'DockPanel.Dock="Right"' in seg:
                estart=lt
                eend=find_close(block, lt) if not selfclose else egt+1
                elem=block[estart:eend]
                if is_input_tag(name) or contains_input(elem):
                    return (estart, eend, name)
        if selfclose: i=egt+1; continue
        depth+=1; i=egt+1
    return None

def remove_dock_right(elem):
    return elem.replace(' DockPanel.Dock="Right"', '', 1)

def reindent(elem):
    extra='    '
    return '\n'.join((extra+ln) if ln.strip() else ln for ln in elem.split('\n'))

def transform_dockpanels(s, hints):
    out=[]; last=0; n=len(s); count=0; found=0
    while True:
        dp=s.find('<DockPanel', last)
        if dp==-1: break
        out.append(s[last:dp])
        close=find_close(s, dp)
        if close==-1:
            gt=s.find('>', dp); last=(gt+1) if gt!=-1 else dp+1; continue
        block=s[dp:close]; found+=1
        right=find_right_input(block)
        if right is None:
            gt=s.find('>', dp); last=(gt+1) if gt!=-1 else dp+1; continue
        rstart,rend,tag=right
        lm=re.search(r'TextBlock\b[^>]*?DockPanel\.Dock="Left"[^>]*?Text="([^"]*)"', block)
        label=lm.group(1) if lm else None
        elem=block[rstart:rend]; elem2=remove_dock_right(elem)
        ls=block.rfind('\n',0,rstart)+1; ci=block[ls:rstart]
        ei=reindent(elem2)
        hint=hints.get(label, label or '')
        fo=ci+'<local:Field DockPanel.Dock="Right" Hint="%s">\n'%(hint.replace('&','&amp;').replace('<','&lt;').replace('>','&gt;').replace('"','&quot;'))
        fc=ci+'</local:Field>'
        out.append(block[:rstart]+fo+ei+'\n'+fc+block[rend:])
        last=close; count+=1
    out.append(s[last:])
    return ''.join(out), count, found

def transform_chips(s, hints):
    out=[]; last=0; n=len(s); count=0; found=0
    needle='<Border Style="{StaticResource AppleChip}"'
    while True:
        bi=s.find(needle, last)
        if bi==-1: break
        out.append(s[last:bi])
        close=find_close(s, bi)
        if close==-1:
            gt=s.find('>', bi); last=(gt+1) if gt!=-1 else bi+1; continue
        block=s[bi:close]; found+=1
        lm=re.search(r'<TextBlock\b[^>]*?\bText="([^"]*)"', block)
        label=lm.group(1) if lm else None
        im=re.search(r'<(TextBox|ComboBox|ListBox|ctl:NumSliderBox|CheckBox|RadioButton|Slider|DatePicker|PasswordBox|ToggleButton)\b', block)
        if not im:
            last=close; continue
        estart=im.start()
        if '<local:Field' in block[:estart]:
            last=close; continue
        eend=find_close(block, estart)
        if eend==-1:
            last=close; continue
        elem=block[estart:eend]
        ls=block.rfind('\n',0,estart)+1; ci=block[ls:estart]
        ei=reindent(elem)
        hint=hints.get(label, label or '')
        fo=ci+'<local:Field Hint="%s">\n'%(hint.replace('&','&amp;').replace('<','&lt;').replace('>','&gt;').replace('"','&quot;'))
        fc=ci+'</local:Field>'
        out.append(block[:estart]+fo+ei+'\n'+fc+block[eend:])
        last=close; count+=1
    out.append(s[last:])
    return ''.join(out), count, found

HINTS = eval(open('D:/wqz/code/NoCodeMotion/_hints.py','r',encoding='utf-8').read())

FILES = [
 "D:/wqz/code/NoCodeMotion/Views/AxisPage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/IoPage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/CylinderPage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/VariablePage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/CommPage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/TrayPage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/CameraPage.xaml",
]

import xml.etree.ElementTree as ET
for f in FILES:
    s, enc, crlf = read_text(f)
    s2, dc, dfound = transform_dockpanels(s, HINTS)
    s3, cc, cfound = transform_chips(s2, HINTS)
    opens=s3.count('<local:Field'); closes=s3.count('</local:Field>')
    test=re.sub(r'<!--.*?-->','',s3,flags=re.S)
    wf='OK'
    try: ET.fromstring(test)
    except Exception as e: wf='ERR:'+str(e)[:60]
    print("%-15s dockWrap=%2d/%2d chipWrap=%2d/%2d Field(o=%d,c=%d) wf=%s"%(f.split('/')[-1],dc,dfound,cc,cfound,opens,closes,wf))
