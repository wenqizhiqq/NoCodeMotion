import subprocess, os, time, ctypes, tempfile
from ctypes import wintypes

EXE = r"D:\wqz\code\NCMBuildC\bin\NoCodeMotion\debug\NoCodeMotion.exe"
MARK = os.path.join(tempfile.gettempdir(), "ncm_markers.txt")
ERR = os.path.join(tempfile.gettempdir(), "ncm_startup_error.txt")
LOG = r"D:\wqz\code\NoCodeMotion\.workbuddy\run_markers.txt"
lines = []

for f in (MARK, ERR):
    try: os.remove(f)
    except: pass

for n in ("NoCodeMotion.exe",):
    subprocess.run(["taskkill","/IM",n,"/F"], capture_output=True, text=True)

cwd = os.path.dirname(EXE)
p = subprocess.Popen([EXE], cwd=cwd)
pid = p.pid
lines.append("PID=%s" % pid)
time.sleep(12.0)
lines.append("ALIVE=%s" % (p.poll() is None))

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

for name,label in ((ERR,"STARTUP_ERROR"),(MARK,"MARKERS")):
    lines.append("=== %s ===" % label)
    if os.path.exists(name):
        with open(name,"r",encoding="utf-8",errors="replace") as f:
            lines.append(f.read()[:3000])
    else:
        lines.append("(missing)")

try: p.terminate()
except: pass
time.sleep(1)
lines.append("ALIVE after term=%s" % (p.poll() is None))

with open(LOG,"w",encoding="utf-8",errors="replace") as f:
    f.write("\n".join(lines))
print("WROTE", LOG)
