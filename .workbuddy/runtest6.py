# -*- coding: utf-8 -*-
import subprocess, os, time, ctypes
from ctypes import wintypes

EXE = r"D:\wqz\code\NoCodeMotion\bin\Debug\net10.0-windows\NoCodeMotion.exe"
OUT = r"D:\wqz\code\NoCodeMotion\.workbuddy\build5_launch2.txt"

for n in ("NoCodeMotion.exe",):
    try: subprocess.run(["taskkill","/IM",n,"/F"], capture_output=True, text=True)
    except: pass

user32 = ctypes.windll.user32
EnumWindowsProc = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)

p = subprocess.Popen([EXE], cwd=os.path.dirname(EXE))
pid = p.pid
target = None
# poll up to 7s
for t in range(1, 15):
    time.sleep(0.5)
    if p.poll() is not None: break
    def cb(hwnd, lparam, _pid=pid):
        global target
        if user32.IsWindowVisible(hwnd):
            pb = wintypes.DWORD(0)
            user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pb))
            if pb.value == _pid: target = hwnd
        return True
    user32.EnumWindows(EnumWindowsProc(cb), 0)
    if target: break

alive = p.poll() is None
lines = ["PID=%s ALIVE_AFTER_~%.1fs=%s TARGET=%s WAITED=%.1fs" %
         (pid, t*0.5, alive, target, t*0.5)]
try: p.terminate()
except: pass
with open(OUT,"w",encoding="ascii",errors="replace") as f: f.write("\n".join(lines))
print("\n".join(lines))
