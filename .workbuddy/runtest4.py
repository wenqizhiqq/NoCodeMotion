# -*- coding: utf-8 -*-
import subprocess, os, time, ctypes
from ctypes import wintypes

EXE = r"D:\wqz\code\NoCodeMotion\bin\Debug\net10.0-windows\NoCodeMotion.exe"
SUMMARY = r"D:\wqz\code\NoCodeMotion\.workbuddy\runtest4.txt"
SHOT = r"D:\wqz\code\NoCodeMotion\.workbuddy\app_shot.png"
lines = []
def log(s): lines.append(str(s))

for name in ("NoCodeMotion.exe",):
    try:
        subprocess.run(["taskkill","/IM",name,"/F"], capture_output=True, text=True)
    except Exception:
        pass

user32 = ctypes.windll.user32
gdi32 = ctypes.windll.gdi32
EnumWindowsProc = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)

p = subprocess.Popen([EXE], cwd=os.path.dirname(EXE))
pid = p.pid
log("PID=%s" % pid)
time.sleep(2.5)
log("ALIVE=%s" % (p.poll() is None))

target = None
def cb(hwnd, lparam):
    global target
    if user32.IsWindowVisible(hwnd):
        pid_buf = wintypes.DWORD(0)
        user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid_buf))
        if pid_buf.value == pid:
            target = hwnd
    return True
user32.EnumWindows(EnumWindowsProc(cb), 0)
log("TARGET_HWND=%s" % target)

class BITMAPINFOHEADER(ctypes.Structure):
    _fields_ = [
        ("biSize", wintypes.DWORD), ("biWidth", wintypes.LONG),
        ("biHeight", wintypes.LONG), ("biPlanes", wintypes.WORD),
        ("biBitCount", wintypes.WORD), ("biCompression", wintypes.DWORD),
        ("biSizeImage", wintypes.DWORD), ("biXPelsPerMeter", wintypes.LONG),
        ("biYPelsPerMeter", wintypes.LONG), ("biClrUsed", wintypes.DWORD),
        ("biClrImportant", wintypes.DWORD),
    ]

def capture(hwnd, path):
    rect = wintypes.RECT()
    user32.GetWindowRect(hwnd, ctypes.byref(rect))
    w = rect.right - rect.left; h = rect.bottom - rect.top
    if w <= 0 or h <= 0:
        return False
    hwnd_dc = user32.GetWindowDC(hwnd)
    mem_dc = gdi32.CreateCompatibleDC(hwnd_dc)
    bmp = gdi32.CreateCompatibleBitmap(hwnd_dc, w, h)
    gdi32.SelectObject(mem_dc, bmp)
    user32.PrintWindow(hwnd, mem_dc, 0x02)
    bmi = BITMAPINFOHEADER()
    bmi.biSize = ctypes.sizeof(BITMAPINFOHEADER)
    bmi.biWidth = w; bmi.biHeight = -h; bmi.biPlanes = 1
    bmi.biBitCount = 32; bmi.biCompression = 0
    buf = ctypes.create_string_buffer(w * h * 4)
    gdi32.GetDIBits(mem_dc, bmp, 0, h, buf, ctypes.byref(bmi), 0)
    from PIL import Image
    img = Image.frombuffer("RGBA", (w, h), buf, "raw", "BGRA", 0, 1)
    img.convert("RGB").save(path)
    gdi32.DeleteObject(bmp); gdi32.DeleteDC(mem_dc); user32.ReleaseDC(hwnd, hwnd_dc)
    return True

if target:
    ok = capture(target, SHOT)
    log("CAPTURE=%s SIZE=%s" % (ok, os.path.getsize(SHOT) if os.path.exists(SHOT) else 0))
else:
    log("CAPTURE=SKIP")

try:
    p.terminate()
except Exception:
    pass

with open(SUMMARY, "w", encoding="utf-8", errors="replace") as f:
    f.write("\n".join(lines))
print("\n".join(lines))
