# -*- coding: utf-8 -*-
import subprocess
files = ["AxisPage","IoPage","CylinderPage","VariablePage","CommPage","TrayPage","CameraPage"]
for f in files:
    p="D:/wqz/code/NoCodeMotion/Views/%s.xaml"%f
    s=open(p,'rb').read().decode('utf-8','ignore')
    print("%-14s open=%d close=%d"%(f, s.count('<local:Field'), s.count('</local:Field>')))
