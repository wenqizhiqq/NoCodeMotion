# -*- coding: utf-8 -*-
import re, sys

INPUT_TAGS = {'TextBox','ComboBox','CheckBox','ListBox','Grid','RadioButton',
              'ToggleButton','Slider','PasswordBox','DatePicker','ctl:NumSliderBox'}
INPUT_SUBSTR = ['<TextBox','<ComboBox','<CheckBox','<ListBox','<ctl:NumSliderBox',
                '<RadioButton','<Slider','<DatePicker','<PasswordBox','<ToggleButton']

def read_text(f):
    raw = open(f,'rb').read()
    bom = raw[:3]==b'\xef\xbb\xbf'
    body = raw[3:] if bom else raw
    try: s = body.decode('utf-8')
    except Exception: s = body.decode('gbk')
    crlf = '\r\n' in s
    return s.replace('\r\n','\n'), ('utf-8-sig' if bom else 'utf-8'), crlf

def write_text(f, s, enc, crlf):
    s = s.replace('\r\n','\n')
    if crlf: s = s.replace('\n','\r\n')
    raw = s.encode('utf-8' if enc!='gbk' else 'gbk')
    if enc=='utf-8-sig': raw = b'\xef\xbb\xbf'+raw
    open(f,'wb').write(raw)

def escape_attr(t):
    return t.replace('&','&amp;').replace('<','&lt;').replace('>','&gt;').replace('"','&quot;')

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

def is_input_tag(t): return t in INPUT_TAGS
def contains_input(elem): return any(sub in elem for sub in INPUT_SUBSTR)

def find_right_input(block):
    pos=0
    while True:
        idx = block.find('DockPanel.Dock="Right"', pos)
        if idx==-1: return None
        j = block.rfind('<', 0, idx)
        if j==-1:
            pos=idx+1; continue
        m = re.match(r'<\s*([\w:]+)', block[j:])
        if not m:
            pos=idx+1; continue
        tag = m.group(1)
        eend = find_close(block, j)
        if eend==-1:
            pos=idx+1; continue
        elem = block[j:eend]
        if is_input_tag(tag) or contains_input(elem):
            return (j, eend, tag)
        pos = idx+1
    return None

def remove_dock_right(elem):
    return elem.replace(' DockPanel.Dock="Right"', '', 1)

def reindent(elem):
    extra='    '
    lines = elem.split('\n')
    return '\n'.join((extra+ln) if ln.strip() else ln for ln in lines)

def transform_dockpanels(s, hints):
    out=[]; pos=0; n=len(s); count=0; found=0
    while True:
        dp = s.find('<DockPanel', pos)
        if dp==-1: break
        close = find_close(s, dp)
        if close==-1:
            out.append(s[pos:]); pos=n; break
        block = s[dp:close]
        found+=1
        right = find_right_input(block)
        if right is None:
            pos=close; continue
        rstart, rend, tag = right
        label_m = re.search(r'TextBlock\b[^>]*?DockPanel\.Dock="Left"[^>]*?Text="([^"]*)"', block)
        label = label_m.group(1) if label_m else None
        elem = block[rstart:rend]
        elem2 = remove_dock_right(elem)
        line_start = block.rfind('\n',0,rstart)+1
        child_indent = block[line_start:rstart]
        elem_ind = reindent(elem2)
        hint = escape_attr(hints.get(label, label or ''))
        field_open = child_indent + '<local:Field DockPanel.Dock="Right" Hint="%s">\n' % hint
        field_close = child_indent + '</local:Field>'
        new_block = block[:rstart] + field_open + elem_ind + '\n' + field_close + block[rend:]
        out.append(s[pos:dp]); out.append(new_block)
        pos = close; count+=1
    out.append(s[pos:])
    return ''.join(out), count, found

def transform_chips(s, hints):
    out=[]; pos=0; n=len(s); count=0; found=0
    needle = '<Border Style="{StaticResource AppleChip}"'
    while True:
        bi = s.find(needle, pos)
        if bi==-1: break
        close = find_close(s, bi)
        if close==-1:
            out.append(s[pos:]); pos=n; break
        block = s[bi:close]
        found+=1
        lm = re.search(r'<TextBlock\b[^>]*?\bText="([^"]*)"', block)
        label = lm.group(1) if lm else None
        im = re.search(r'<(TextBox|ComboBox|ListBox|ctl:NumSliderBox|CheckBox|RadioButton|Slider|DatePicker|PasswordBox|ToggleButton)\b', block)
        if not im:
            pos=close; continue
        estart = im.start()
        # skip if already wrapped (inside a local:Field) -> handled by DockPanel pass
        if '<local:Field' in block[:estart]:
            pos=close; continue
        eend = find_close(block, estart)
        if eend==-1:
            pos=close; continue
        elem = block[estart:eend]
        line_start = block.rfind('\n',0,estart)+1
        child_indent = block[line_start:estart]
        elem_ind = reindent(elem)
        hint = escape_attr(hints.get(label, label or ''))
        field_open = child_indent + '<local:Field Hint="%s">\n' % hint
        field_close = child_indent + '</local:Field>'
        new_block = block[:estart] + field_open + elem_ind + '\n' + field_close + block[eend:]
        out.append(s[pos:bi]); out.append(new_block)
        pos = close; count+=1
    out.append(s[pos:])
    return ''.join(out), count, found

HINTS = {
 # AxisPage
 '名称':'唯一标识名称，供点位与流程引用',
 '轴类型':'脉冲/总线/EtherCAT 等驱动方式',
 '轴号':'控制器中该轴的物理或逻辑编号',
 '单位':'位置显示与设定的计量单位',
 '控制器':'该轴所属的控制器设备',
 '使能':'是否允许该轴上电使能',
 '脉冲当量':'每单位位移对应的脉冲数',
 '运行速度':'正常运行的目标速度（单位/秒）',
 '加速度':'运动加速时的加速度参数',
 '减速度':'运动减速时的减速度参数',
 '加加速度':'加加速度 jerk，用于平滑加减速',
 '回零方式':'寻找机械零点的方式',
 '回零速度':'回零运动速度（单位/秒）',
 '爬行速度':'接近零点时的低速（单位/秒）',
 '原点偏移':'回零后相对机械零点的偏移量',
 '软正限位':'软件正方向行程保护边界',
 '软负限位':'软件负方向行程保护边界',
 '到位误差':'判定到达目标的允许位置偏差',
 '使能电平':'使能信号的有效电平',
 '方向电平':'方向信号的有效电平',
 '报警电平':'报警信号的有效电平',
 '编码器类型':'反馈编码器类型（无/增量/绝对）',
 '编码器分辨率':'编码器线数（每转脉冲数）',
 '急停减速':'急停时的减速度',
 # CylinderPage
 '设备编号':'气缸在控制器中的设备编号',
 '气缸类型':'单作用/双作用等气缸类型',
 '默认动作':'上电或复位后的默认动作',
 '初始状态':'气缸初始所处状态（伸/缩）',
 '备注':'备注说明（可选）',
 '动作延时':'触发后到执行的延时（毫秒）',
 '伸出延时':'伸出动作持续时间（毫秒）',
 '缩回延时':'缩回动作持续时间（毫秒）',
 '伸出速度':'伸出速度（占最大速度百分比）',
 '缩回速度':'缩回速度（占最大速度百分比）',
 '到位容差':'判定到位的允许时间容差（毫秒）',
 '输出点':'驱动该气缸的输出 IO 点',
 '伸出感应':'检测伸到位的输入 IO 点',
 '缩回感应':'检测缩到位的输入 IO 点',
 '感应类型':'到位感应方式（常开/常闭等）',
 '备用感应':'备用到位感应输入点',
 '互锁使能':'是否启用互锁保护',
 '双线圈':'是否为双线圈电磁阀',
 '报警使能':'异常时是否触发报警',
 '手动使能':'是否允许手动操作该气缸',
 '动作超时':'动作未完成时的报警超时（毫秒）',
 '脉冲输出':'是否以脉冲方式输出',
 '脉冲宽度':'脉冲输出宽度（毫秒）',
 '关联轴':'与该气缸联动的轴',
 # TrayPage
 '行数':'料盘的行数',
 '列数':'料盘的列数',
 '起始X':'第一个点位的 X 坐标',
 '起始Y':'第一个点位的 Y 坐标',
 '间距X':'相邻点位 X 方向间距',
 '间距Y':'相邻点位 Y 方向间距',
 # CameraPage
 '品牌':'相机品牌/厂商',
 'IP 地址':'相机网口 IP 地址',
 '端口':'相机通讯端口号',
 '分辨率':'采集图像宽 × 高（像素）',
 '曝光 (ms)':'曝光时间（毫秒）',
 '增益':'图像增益（亮度放大）',
 '触发模式':'触发方式（连续/软触发/硬触发）',
 '备注':'备注说明（可选）',
 # CommPage DockPanel
 '超时':'通讯超时时间（毫秒）',
 '重试次数':'发送失败后的重试次数',
 '轮询间隔':'轮询设备的间隔（毫秒）',
 '自动重连':'断开后是否自动重连',
 '详细日志':'是否记录详细收发日志',
 '保活心跳':'是否发送心跳保活连接',
 # CommPage chips
 '串口号':'串口名称（如 COM3）',
 '端口 / IP':'网口 IP 或端口地址（串口时填串口号）',
 '波特率':'串口波特率（bps）',
 '端口号':'网口通讯端口号',
 '数据位':'串口数据位（5~8）',
 '校验位':'串口校验方式（无/奇/偶）',
 '停止位':'串口停止位（1/1.5/2）',
 '流控':'串口流控制方式',
 '从站地址':'Modbus 从站地址（1~247）',
 '字节序':'多字节数据的字节顺序',
 '机架 Rack':'西门子 PLC 的机架号',
 '槽位 Slot':'西门子 PLC 的槽号',
 '网络号':'三菱 MC 协议网络号',
}

FILES = [
 "D:/wqz/code/NoCodeMotion/Views/AxisPage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/IoPage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/CylinderPage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/VariablePage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/CommPage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/TrayPage.xaml",
 "D:/wqz/code/NoCodeMotion/Views/CameraPage.xaml",
]

summary=[]
for f in FILES:
    s, enc, crlf = read_text(f)
    s2, dc, dfound = transform_dockpanels(s, HINTS)
    s3, cc, cfound = transform_chips(s2, HINTS)
    # balance check
    opens = s3.count('<local:Field')
    closes = s3.count('</local:Field>')
    # well-formedness (strip comments)
    import xml.etree.ElementTree as ET
    test = re.sub(r'<!--.*?-->', '', s3, flags=re.S)
    wf = 'OK'
    try:
        ET.fromstring(test)
    except Exception as e:
        wf = 'XML-ERR: '+str(e)
    write_text(f, s3, enc, crlf)
    summary.append((f.split('/')[-1], dc, dfound, cc, cfound, opens, closes, wf, enc))

for name,dc,dfound,cc,cfound,op,cl,wf,enc in summary:
    print("%-16s dockWrap=%2d/%2d  chipWrap=%2d/%2d  Field(open=%d,close=%d)  wf=%s  enc=%s" % (name,dc,dfound,cc,cfound,op,cl,wf,enc))
