# -*- coding: utf-8 -*-
import subprocess, os, time, ctypes
from ctypes import wintypes

EXE = r"D:\wqz\code\NoCodeMotion\bin\Debug\net10.0-windows\NoCodeMotion.exe"
OUT = r"D:\wqz\code\NoCodeMotion\.workbuddy\build5_diag.txt"
SHOT = r"D:\wqz\code\NoCodeMotion\.workbuddy\app_screen.png"

for n in ("NoCodeMotion.exe",):
    try: subprocess.run(["taskkill","/IM",n,"/F"], capture_output=True, text=True)
    except: pass

user32 = ctypes.windll.user32
gdi32 = ctypes.windll.gdi32
EnumWindowsProc = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)

p = subprocess.Popen([EXE], cwd=os.path.dirname(EXE))
pid = p.pid
time.sleep(4.0)
alive = p.poll() is None

allw = []
ourw = []
def cb(hwnd, lparam):
    if user32.IsWindowVisible(hwnd):
        pb = wintypes.DWORD(0)
        user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pb))
        ln = user32.GetWindowTextLengthW(hwnd)
        title = ""
        if ln > 0:
            b = ctypes.create_unicode_buffer(ln+1)
            user32.GetWindowTextW(hwnd, b, ln+1)
            title = b.value
        cls = ctypes.create_unicode_buffer(256)
        user32.GetClassNameW(hwnd, cls, 256)
        rec = (pb.value, title, cls.value)
        allw.append(rec)
        if pb.value == pid:
            ourw.append(rec)
    return True
user32.EnumWindows(EnumWindowsProc(cb), 0)

# full screen grab
from PIL import ImageGrab
try:
    img = ImageGrab.grab()
    img.save(SHOT)
    screen_ok = True
    screen_size = img.size
except Exception as e:
    screen_ok = False
    screen_size = str(e)

# also try ImageGrab.grab(bbox) of our window if found
our_shot = None
if ourw:
    hwnd = None
    # find our hwnd
    def cb2(hwnd_, lparam):
        global hwnd
        if user32.IsWindowVisible(hwnd_):
            pb = wintypes.DWORD(0)
            user32.GetWindowThreadProcessId(hwnd_, ctypes.byref(pb))
            if pb.value == pid: hwnd = hwnd_
        return True
    EnumWindowsProc2 = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)
    hwnd = None
    user32.EnumWindows(EnumWindowsProc2(cb2), 0)
    if hwnd:
        rect = wintypes.RECT()
        user32.GetWindowRect(hwnd, ctypes.byref(rect))
        bbox = (rect.left, rect.top, rect.right, rect.bottom)
        try:
            img2 = ImageGrab.grab(bbox=bbox)
            our_shot = r"D:\wqz\code\NoCodeMotion\.workbuddy\app_window.png"
            img2.save(our_shot)
        except Exception as e:
            our_shot = "ERR " + str(e)

lines = []
lines.append("PID=%s ALIVE=%s" % (pid, alive))
lines.append("ALL_VISIBLE_WINDOWS=%d" % len(allw))
for r in allw: lines.append("  %s T=%r C=%r" % r)
lines.append("OUR_VISIBLE_WINDOWS=%d" % len(ourw))
for r in ourw: lines.append("  %s T=%r C=%r" % r)
lines.append("SCREEN_GRAB=%s SIZE=%s" % (screen_ok, screen_size))
lines.append("WINDOW_GRAB=%s" % our_shot)
try: p.terminate()
except: pass
with open(OUT,"w",encoding="ascii",errors="replace") as f: f.write("\n".join(lines))
print("\n".join(lines))
