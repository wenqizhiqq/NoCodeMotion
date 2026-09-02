import subprocess, os, time, ctypes
from ctypes import wintypes

EXE = r"D:\wqz\code\NoCodeMotionArtifacts\bin\NoCodeMotion\debug\NoCodeMotion.exe"
LOG = r"D:\wqz\code\NoCodeMotion\.workbuddy\run_enum.txt"
lines = []
for n in ("NoCodeMotion.exe",):
    subprocess.run(["taskkill","/IM",n,"/F"], capture_output=True, text=True)

cwd = os.path.dirname(EXE)
p = subprocess.Popen([EXE], cwd=cwd)
pid = p.pid
lines.append("PID=%s" % pid)
time.sleep(5.0)
lines.append("ALIVE=%s" % (p.poll() is None))

user32 = ctypes.windll.user32
EnumWindowsProc = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)
allw = []
def cb(hwnd, lparam):
    if user32.IsWindowVisible(hwnd):
        pb = wintypes.DWORD(0)
        user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pb))
        ln = user32.GetWindowTextLengthW(hwnd)
        t = ""
        if ln > 0:
            b = ctypes.create_unicode_buffer(ln+1)
            user32.GetWindowTextW(hwnd, b, ln+1)
            t = b.value
        cls = ctypes.create_unicode_buffer(256)
        user32.GetClassNameW(hwnd, cls, 256)
        allw.append((pb.value, t, cls.value))
    return True
user32.EnumWindows(EnumWindowsProc(cb), 0)

lines.append("TOTAL VISIBLE=%d" % len(allw))
# our pid windows
lines.append("--- windows for our PID %s ---" % pid)
for wpid, t, c in allw:
    if wpid == pid:
        lines.append("  PID=%s TITLE=%r CLS=%r" % (wpid, t, c))
# any window mentioning NoCodeMotion
lines.append("--- windows containing 'NoCodeMotion' in title ---")
for wpid, t, c in allw:
    if "NoCodeMotion" in t or "NoCode" in t:
        lines.append("  PID=%s TITLE=%r CLS=%r" % (wpid, t, c))
# any HwndWrapper (WPF) windows
lines.append("--- HwndWrapper (WPF) windows ---")
for wpid, t, c in allw:
    if "HwndWrapper" in c:
        lines.append("  PID=%s TITLE=%r CLS=%r" % (wpid, t, c))

try:
    p.terminate()
except: pass
with open(LOG, "w", encoding="utf-8", errors="replace") as f:
    f.write("\n".join(lines))
print("WROTE", LOG)
