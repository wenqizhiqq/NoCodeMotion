# -*- coding: utf-8 -*-
import subprocess, os, time, ctypes
from ctypes import wintypes

EXE = r"D:\wqz\code\NoCodeMotion\bin\Debug\net10.0-windows\NoCodeMotion.exe"
OUT = r"D:\wqz\code\NoCodeMotion\.workbuddy\build5_launch.txt"
SHOT = r"D:\wqz\code\NoCodeMotion\.workbuddy\app_shot2.png"

for n in ("NoCodeMotion.exe",):
    try: subprocess.run(["taskkill","/IM",n,"/F"], capture_output=True, text=True)
    except: pass

user32 = ctypes.windll.user32
gdi32 = ctypes.windll.gdi32
EnumWindowsProc = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)

p = subprocess.Popen([EXE], cwd=os.path.dirname(EXE))
pid = p.pid
time.sleep(2.5)
alive = p.poll() is None
target = None
def cb(hwnd, lparam):
    global target
    if user32.IsWindowVisible(hwnd):
        pb = wintypes.DWORD(0)
        user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pb))
        if pb.value == pid: target = hwnd
    return True
user32.EnumWindows(EnumWindowsProc(cb), 0)

class BITMAPINFOHEADER(ctypes.Structure):
    _fields_ = [("biSize",wintypes.DWORD),("biWidth",wintypes.LONG),("biHeight",wintypes.LONG),
                ("biPlanes",wintypes.WORD),("biBitCount",wintypes.WORD),("biCompression",wintypes.DWORD),
                ("biSizeImage",wintypes.DWORD),("biXPelsPerMeter",wintypes.LONG),("biYPelsPerMeter",wintypes.LONG),
                ("biClrUsed",wintypes.DWORD),("biClrImportant",wintypes.DWORD)]

def capture(hwnd, path):
    rect = wintypes.RECT()
    user32.GetWindowRect(hwnd, ctypes.byref(rect))
    w,h = rect.right-rect.left, rect.bottom-rect.top
    if w<=0 or h<=0: return False
    hd = user32.GetWindowDC(hwnd); md = gdi32.CreateCompatibleDC(hd)
    bmp = gdi32.CreateCompatibleBitmap(hd, w, h); gdi32.SelectObject(md, bmp)
    user32.PrintWindow(hwnd, md, 0x02)
    bmi = BITMAPINFOHEADER(); bmi.biSize = ctypes.sizeof(BITMAPINFOHEADER)
    bmi.biWidth=w; bmi.biHeight=-h; bmi.biPlanes=1; bmi.biBitCount=32; bmi.biCompression=0
    buf = ctypes.create_string_buffer(w*h*4)
    gdi32.GetDIBits(md, bmp, 0, h, buf, ctypes.byref(bmi), 0)
    from PIL import Image
    Image.frombuffer("RGBA",(w,h),buf,"raw","BGRA",0,1).convert("RGB").save(path)
    gdi32.DeleteObject(bmp); gdi32.DeleteDC(md); user32.ReleaseDC(hwnd, hd)
    return True

ok = capture(target, SHOT) if target else False
lines = ["PID=%s ALIVE=%s TARGET=%s CAPTURE=%s SIZE=%s" %
         (pid, alive, target, ok, os.path.getsize(SHOT) if ok and os.path.exists(SHOT) else 0)]
try: p.terminate()
except: pass
with open(OUT,"w",encoding="ascii",errors="replace") as f: f.write("\n".join(lines))
print("\n".join(lines))
