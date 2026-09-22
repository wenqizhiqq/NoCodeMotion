# -*- coding: utf-8 -*-
import sys, re

F = sys.argv[1]

raw = open(F,'rb').read()
bom = raw[:3]==b'\xef\xbb\xbf'
body = raw[3:] if bom else raw
try:
    s = body.decode('utf-8')
except Exception:
    s = body.decode('gbk')
crlf = '\r\n' in s
s = s.replace('\r\n','\n')

# ---- hint dictionary keyed by label text ----
HINTS = {
    # 通用
    '名称': '该项的唯一名称，供流程/点位引用',
    '备注': '备注说明，仅作标注不影响运行',
    # AxisPage
    '轴类型': '驱动方式：脉冲/总线/EtherCAT 等',
    '轴号': '控制器中该轴的物理或逻辑编号',
    '单位': '位置显示与设定的计量单位',
    '控制器': '该轴所属的控制器设备',
    '使能': '是否允许该轴上电使能',
    '脉冲当量': '每单位位移对应的脉冲数',
    '运行速度': '正常运行速度，单位 单位/秒',
    '加速度': '加速过程的加速度参数',
    '减速度': '减速过程的减速度参数',
    '加加速度': '加加速度（Jerk），加减速平滑参数',
    '回零方式': '寻找机械零点的方式',
    '回零速度': '回零运动速度，单位 单位/秒',
    '爬行速度': '接近零点时的低速爬行速度',
    '原点偏移': '回零后相对机械零点的偏移量',
    '软正限位': '正向软件保护行程边界（同单位）',
    '软负限位': '负向软件保护行程边界（同单位）',
    '到位误差': '判定到达目标位的允许偏差',
    '使能电平': '使能信号的有效电平',
    '方向电平': '方向信号的有效电平（正向/负向）',
    '报警电平': '报警信号的有效电平',
    '编码器类型': '反馈编码器类型：增量/绝对',
    '编码器分辨率': '编码器每转的脉冲线数',
    '急停减速': '急停时的减速度',
    # CylinderPage
    '设备编号': '气缸在控制器中的设备/端口编号',
    '气缸类型': '气缸结构类型（单/双作用等）',
    '默认动作': '上电或复位时的默认动作状态',
    '初始状态': '流程启动前的初始位置状态',
    '动作延时': '动作触发后的总等待时间（ms）',
    '伸出延时': '从缩回到完全伸出的时间（ms）',
    '缩回延时': '从伸回到完全缩回的时间（ms）',
    '伸出速度': '活塞伸出速度（0~100%）',
    '缩回速度': '活塞缩回速度（0~100%）',
    '到位容差': '判定到位允许的偏差范围',
    '输出点': '驱动该气缸的输出 IO 点',
    '伸出感应': '检测伸到位所用的输入信号',
    '缩回感应': '检测缩到位所用的输入信号',
    '感应类型': '感应信号类型（NPN/PNP 等）',
    '备用感应': '备用到位感应输入点',
    '互锁使能': '是否启用气缸互锁保护',
    '双线圈': '是否使用双线圈驱动',
    '报警使能': '是否启用气缸动作报警',
    '手动使能': '是否允许手动模式操作',
    '动作超时': '动作未完成判超时的时间（ms）',
    '脉冲输出': '是否以脉冲方式驱动输出',
    '脉冲宽度': '输出脉冲的持续时间（ms）',
    '关联轴': '与该气缸联动的轴（可选）',
    # TrayPage
    '行数': '托盘的行数量',
    '列数': '托盘的列数量',
    '起始X': '第 1 个工位的 X 坐标',
    '起始Y': '第 1 个工位的 Y 坐标',
    '间距X': '相邻工位的 X 方向间距',
    '间距Y': '相邻工位的 Y 方向间距',
    # CameraPage
    '品牌': '相机品牌（如海康/大华等）',
    'IP 地址': '相机网口通讯的 IP 地址',
    '端口': '相机通讯端口号',
    '分辨率': '采集图像分辨率（宽 × 高）',
    '曝光 (ms)': '曝光时间，单位毫秒',
    '增益': '图像增益（亮度放大倍数）',
    '触发模式': '软触发/硬触发等取图方式',
    # CommPage (AppleChip)
    '串口号': '串口名（如 COM3）',
    '端口 / IP': '网口 IP 或串口名（按类型）',
    '波特率': '串口波特率（如 9600/115200）',
    '端口号': '网口通讯端口号（如 502）',
    '数据位': '串口数据位（5~8）',
    '校验位': '串口校验方式（无/奇/偶）',
    '停止位': '串口停止位（1/1.5/2）',
    '流控': '串口流控制方式',
    '从站地址': 'Modbus 从站站号',
    '字节序': '多字节数据的字节顺序',
    '机架 Rack': '西门子 S7 机架号',
    '槽位 Slot': '西门子 S7 CPU 槽位号',
    '网络号': '三菱 MC 网络编号',
    # CommPage (DockPanel)
    '超时': '收发等待超时时间，单位毫秒',
    '重试次数': '失败后的重发次数',
    '轮询间隔': '周期轮询间隔，单位毫秒',
    '自动重连': '连接断开后是否自动重连',
    '详细日志': '是否记录详细收发日志',
    '保活心跳': '是否发送心跳保活连接',
}

LABEL_RE = re.compile(r'(?:Style="\{StaticResource AppleLabel\}"[^>]*Text="([^"]+)"|Text="([^"]+)"[^>]*Style="\{StaticResource AppleChipLabel\}")')
INPUT_MARK = re.compile(r'NumSliderBox|<(TextBox|ComboBox|CheckBox|ListBox)\b')

# single regex: matches self-closing OR paired control element (TextBox/CheckBox/ComboBox/ListBox)
CONTROL = re.compile(r'<(TextBox|CheckBox|ComboBox|ListBox)\b[^>]*?(?:/>|>.*?</\1>)', re.DOTALL)
CONTAINER = re.compile(r'<(StackPanel|Grid|Border)\b[^>]*?DockPanel\.Dock="Right"[^>]*>(.*?)</\1>', re.DOTALL)

def find_label(text, pos):
    seg = text[:pos]
    ms = list(LABEL_RE.finditer(seg))
    if not ms: return (None, None)
    m = ms[-1]
    txt = m.group(1) or m.group(2)
    kind = 'AppleChipLabel' if m.group(2) else 'AppleLabel'
    return (txt, kind)

# collect replacements: list of (start, end, newtext)
repls = []

# leaf inputs
for m in CONTROL.finditer(s):
    el = m.group(0)
    start, end = m.start(), m.end()
    docked = 'DockPanel.Dock="Right"' in el
    label, kind = find_label(s, start)
    if label is None:
        continue
    if docked:
        pass  # Rule 1
    else:
        if kind != 'AppleChipLabel':
            continue  # Rule 2 only for AppleChipLabel
    hint = HINTS.get(label)
    if hint is None:
        print('  [WARN] no hint for label=%r (kind=%s)' % (label, kind))
        continue
    inner = el.replace('DockPanel.Dock="Right"', '', 1)
    if docked:
        new = '<local:Field DockPanel.Dock="Right" Hint="%s">%s</local:Field>' % (hint, inner)
    else:
        new = '<local:Field Hint="%s">%s</local:Field>' % (hint, inner)
    repls.append((start, end, new))

# containers (StackPanel/Grid/Border) docked right, containing an input
for m in CONTAINER.finditer(s):
    el = m.group(0)
    start, end = m.start(), m.end()
    if not INPUT_MARK.search(el):
        continue  # not an input container (e.g. status display)
    label, kind = find_label(s, start)
    if label is None:
        continue
    hint = HINTS.get(label)
    if hint is None:
        print('  [WARN] no hint for container label=%r' % label)
        continue
    inner = el.replace('DockPanel.Dock="Right"', '', 1)
    new = '<local:Field DockPanel.Dock="Right" Hint="%s">%s</local:Field>' % (hint, inner)
    repls.append((start, end, new))

# apply from end to start
repls.sort(key=lambda t: t[0], reverse=True)
for start, end, new in repls:
    s = s[:start] + new + s[end:]

# write back preserving bom + crlf
s = s.replace('\r\n','\n')
if crlf:
    s = s.replace('\n','\r\n')
out = (b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8')
open(F,'wb').write(out)
print('  wrapped %d controls in %s' % (len(repls), F))
