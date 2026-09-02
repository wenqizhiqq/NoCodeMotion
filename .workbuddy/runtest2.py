# -*- coding: utf-8 -*-
import subprocess, os, time, sys, ctypes
from ctypes import wintypes

EXE = r"D:\wqz\code\NoCodeMotion\bin\Debug\net10.0-windows\NoCodeMotion.exe"
SUMMARY = r"D:\wqz\code\NoCodeMotion\.workbuddy\runtest2.txt"

lines = []
def log(s): lines.append(str(s))

# kill lingering
for name in ("NoCodeMotion.exe",):
    try:
        subprocess.run(["taskkill","/IM",name,"/F"], capture_output=True, text=True)
    except Exception:
        pass

user32 = ctypes.windll.user32
gdi32 = ctypes.windll.gdi32
EnumWindowsProc = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)

log("=== LAUNCH (existing build) ===")
p = subprocess.Popen([EXE], cwd=os.path.dirname(EXE))
pid = p.pid
log("PID=%s" % pid)
time.sleep(1.5)
log("ALIVE=%s" % (p.poll() is None))

found = []
def cb(hwnd, lparam):
    if user32.IsWindowVisible(hwnd):
        pid_buf = wintypes.DWORD(0)
        user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid_buf))
        if pid_buf.value == pid:
            ln = user32.GetWindowTextLengthW(hwnd)
            title = ""
            if ln > 0:
                b = ctypes.create_unicode_buffer(ln + 1)
                user32.GetWindowTextW(hwnd, b, ln + 1)
                title = b.value
            cls = ctypes.create_unicode_buffer(256)
            user32.GetClassNameW(hwnd, cls, 256)
            found.append((hwnd, title, cls.value))
    return True
user32.EnumWindows(EnumWindowsProc(cb), 0)
log("WINDOWS_FOR_PID=%d" % len(found))
for hwnd, title, cls in found:
    log("  HWND=%s TITLE=%r CLS=%r" % (hwnd, title, cls))

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

shot = r"D:\wqz\code\NoCodeMotion\.workbuddy\runtest_shot.png"
if found:
    ok = capture(found[0][0], shot)
    log("CAPTURE=%s SIZE=%s" % (ok, (os.path.getsize(shot) if os.path.exists(shot) else 0)))
else:
    log("CAPTURE=SKIP no window")

try:
    p.terminate()
except Exception:
    pass

with open(SUMMARY, "w", encoding="utf-8", errors="replace") as f:
    f.write("\n".join(lines))
print("\n".join(lines))
