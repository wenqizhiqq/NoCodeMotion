import subprocess, os, time, ctypes
from ctypes import wintypes

EXE = r"D:\wqz\code\NoCodeMotionArtifacts\bin\NoCodeMotion\debug\NoCodeMotion.exe"
ERR = r"D:\wqz\code\NoCodeMotion\.workbuddy\startup_error.txt"
LOG = r"D:\wqz\code\NoCodeMotion\.workbuddy\run_launch.txt"
lines = []

# kill any prior instance
for n in ("NoCodeMotion.exe",):
    subprocess.run(["taskkill","/IM",n,"/F"], capture_output=True, text=True)

try:
    os.remove(ERR)
except: pass

cwd = os.path.dirname(EXE)
p = subprocess.Popen([EXE], cwd=cwd)
pid = p.pid
lines.append("PID=%s" % pid)
time.sleep(5.0)
alive = p.poll() is None
lines.append("ALIVE after 5s=%s" % alive)

user32 = ctypes.windll.user32
EnumWindowsProc = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)
found = []
def cb(hwnd, lparam):
    if user32.IsWindowVisible(hwnd):
        pb = wintypes.DWORD(0)
        user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pb))
        if pb.value == pid:
            ln = user32.GetWindowTextLengthW(hwnd)
            t = ""
            if ln > 0:
                b = ctypes.create_unicode_buffer(ln+1)
                user32.GetWindowTextW(hwnd, b, ln+1)
                t = b.value
            cls = ctypes.create_unicode_buffer(256)
            user32.GetClassNameW(hwnd, cls, 256)
            found.append((t, cls.value))
    return True
user32.EnumWindows(EnumWindowsProc(cb), 0)
lines.append("OUR_WINDOWS=%d %s" % (len(found), found))

if os.path.exists(ERR):
    with open(ERR, "r", encoding="utf-8", errors="replace") as f:
        lines.append("=== STARTUP ERROR ===")
        lines.append(f.read()[:3000])
else:
    lines.append("(no startup_error.txt -> no captured exception)")

try:
    p.terminate()
except: pass
time.sleep(1)
lines.append("ALIVE after terminate=%s" % (p.poll() is None))

with open(LOG, "w", encoding="utf-8", errors="replace") as f:
    f.write("\n".join(lines))
print("WROTE", LOG)
