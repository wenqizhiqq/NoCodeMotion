# -*- coding: utf-8 -*-
import re
files = ['Views/%s.xaml'%n for n in ['AxisPage','IoPage','CylinderPage','VariablePage','CommPage','TrayPage','CameraPage']]
ctrl = re.compile(r'<(TextBox|CheckBox|ComboBox|ListBox)\b')
dock = re.compile(r'DockPanel\.Dock="Right"')
right = re.compile(r'DockPanel\.Dock="Right"[^>]*?/>|<(TextBox|CheckBox|ComboBox|ListBox)\b[^>]*?DockPanel\.Dock="Right"')
chip = re.compile(r'AppleChipLabel')
for f in files:
    raw=open(f,'rb').read()
    bom=raw[:3]==b'\xef\xbb\xbf'
    s=raw[3:].decode('utf-8') if bom else raw.decode('utf-8')
    n_right = len(re.findall(r'DockPanel\.Dock="Right"', s))
    n_ctrl = len(ctrl.findall(s))
    n_chip = len(re.findall(r'AppleChipLabel', s))
    # count input controls that are docked right
    print('%-22s dockRight=%d controls=%d appleChipLabel=%d'%(f, n_right, n_ctrl, n_chip))
