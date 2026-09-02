# -*- coding: utf-8 -*-
import subprocess, os, time, sys, ctypes
from ctypes import wintypes

DOTNET = r"C:\Program Files\dotnet\dotnet.exe"
PROJ = r"D:\wqz\code\NoCodeMotion\NoCodeMotion.csproj"
EXE = r"D:\wqz\code\NoCodeMotion\bin\Debug\net10.0-windows\NoCodeMotion.exe"
SUMMARY = r"D:\wqz\code\NoCodeMotion\.workbuddy\runtest.txt"

lines = []
def log(s):
    lines.append(str(s))

# ---- 1) kill zombies ----
for name in ("NoCodeMotion.exe", "dotnet.exe", "VBCSCompiler.exe"):
    try:
        subprocess.run(["taskkill", "/IM", name, "/F"], capture_output=True, text=True)
    except Exception:
        pass

# ---- 2) build ----
os.chdir(r"D:\wqz\code\NoCodeMotion")
log("=== BUILD ===")
proc = subprocess.run([DOTNET, "build", PROJ, "-c", "Debug"],
                      capture_output=True, text=True, encoding="utf-8", errors="replace",
                      timeout=600)
out = proc.stdout + "\n" + proc.stderr
errs = out.count(": error ")
warns = out.count(": warning ")
log("BUILD_EXIT=%d ERRORS=%d WARNINGS=%d" % (proc.returncode, errs, warns))
if errs:
    for l in out.splitlines():
        if ": error " in l:
            log("  ERR " + l.strip())

# ---- 3) ensure pillow ----
try:
    from PIL import Image
    log("PIL ok")
except ImportError:
    log("installing pillow...")
    subprocess.run([sys.executable, "-m", "pip", "install", "pillow"], capture_output=True, text=True)
    from PIL import Image
    log("pillow installed")

# ---- 4) launch + capture ----
if proc.returncode == 0 and os.path.exists(EXE):
    log("=== LAUNCH ===")
    p = subprocess.Popen([EXE], cwd=os.path.dirname(EXE))
    time.sleep(1.3)  # wait for window; EDR may kill ~2s
    alive = p.poll() is None
    log("PROCESS_ALIVE_AFTER_1.3s=%s PID=%s" % (alive, p.pid))

    user32 = ctypes.windll.user32
    gdi32 = ctypes.windll.gdi32
    EnumWindowsProc = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)

    found = []
    def cb(hwnd, lparam):
        if user32.IsWindowVisible(hwnd):
            ln = user32.GetWindowTextLengthW(hwnd)
            if ln > 0:
                buf = ctypes.create_unicode_buffer(ln + 1)
                user32.GetWindowTextW(hwnd, buf, ln + 1)
                cls = ctypes.create_unicode_buffer(256)
                user32.GetClassNameW(hwnd, cls, 256)
                if "NoCodeMotion" in buf.value or "NoCodeMotion" in cls.value:
                    found.append((hwnd, buf.value, cls.value))
        return True
    user32.EnumWindows(EnumWindowsProc(cb), 0)
    log("WINDOWS_FOUND=%d" % len(found))
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
        img = Image.frombuffer("RGBA", (w, h), buf, "raw", "BGRA", 0, 1)
        img.save(path)
        gdi32.DeleteObject(bmp); gdi32.DeleteDC(mem_dc); user32.ReleaseDC(hwnd, hwnd_dc)
        return True

    shot_path = r"D:\wqz\code\NoCodeMotion\.workbuddy\runtest_shot.png"
    ok = False
    if found:
        ok = capture(found[0][0], shot_path)
        log("CAPTURE=%s PATH=%s" % (ok, shot_path if ok else ""))
    # try to also interact: click nothing, just report
    try:
        p.terminate()
    except Exception:
        pass
    log("TERMINATED")
else:
    log("SKIP launch (build failed or exe missing)")

with open(SUMMARY, "w", encoding="utf-8", errors="replace") as f:
    f.write("\n".join(lines))
print("\n".join(lines))
