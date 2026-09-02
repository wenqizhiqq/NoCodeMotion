# -*- coding: utf-8 -*-
import subprocess, os, time, ctypes
from ctypes import wintypes

EXE = r"D:\wqz\code\NoCodeMotion\bin\Debug\net10.0-windows\NoCodeMotion.exe"
SUMMARY = r"D:\wqz\code\NoCodeMotion\.workbuddy\runtest3.txt"
lines = []
def log(s): lines.append(str(s))

for name in ("NoCodeMotion.exe",):
    try:
        subprocess.run(["taskkill","/IM",name,"/F"], capture_output=True, text=True)
    except Exception:
        pass

user32 = ctypes.windll.user32
EnumWindowsProc = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)

p = subprocess.Popen([EXE], cwd=os.path.dirname(EXE))
pid = p.pid
log("PID=%s" % pid)
for t in (2, 4, 6):
    time.sleep(t - (0 if t==2 else (t-2)))
    allw = []
    def cb(hwnd, lparam):
        if user32.IsWindowVisible(hwnd):
            pid_buf = wintypes.DWORD(0)
            user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid_buf))
            ln = user32.GetWindowTextLengthW(hwnd)
            title = ""
            if ln > 0:
                b = ctypes.create_unicode_buffer(ln + 1)
                user32.GetWindowTextW(hwnd, b, ln + 1)
                title = b.value
            cls = ctypes.create_unicode_buffer(256)
            user32.GetClassNameW(hwnd, cls, 256)
            if pid_buf.value == pid or "NoCodeMotion" in title or "无代码" in title or "运动控制" in title:
                allw.append((pid_buf.value, title, cls.value))
        return True
    user32.EnumWindows(EnumWindowsProc(cb), 0)
    log("--- after ~%ds, ALIVE=%s, matches=%d ---" % (t, p.poll() is None, len(allw)))
    for pidv, title, cls in allw:
        log("  PID=%s T=%r C=%r" % (pidv, title, cls))

try:
    p.terminate()
except Exception:
    pass

with open(SUMMARY, "w", encoding="utf-8", errors="replace") as f:
    f.write("\n".join(lines))
print("\n".join(lines))
