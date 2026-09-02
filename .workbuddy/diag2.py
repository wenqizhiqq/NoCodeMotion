# -*- coding: utf-8 -*-
import subprocess, os, time, ctypes
from ctypes import wintypes

DOTNET = r"C:\Program Files\dotnet\dotnet.exe"
PROJ = r"D:\wqz\code\NoCodeMotion\NoCodeMotion.csproj"
EXE = r"D:\wqz\code\NoCodeMotion\bin\Debug\net10.0-windows\NoCodeMotion.exe"
ERR = r"D:\wqz\code\NoCodeMotion\.workbuddy\startup_error.txt"
LOG = r"D:\wqz\code\NoCodeMotion\.workbuddy\diag2.txt"

for n in ("NoCodeMotion.exe","dotnet.exe","VBCSCompiler.exe"):
    subprocess.run(["taskkill","/IM",n,"/F"], capture_output=True, text=True)
# clear previous error
try: os.remove(ERR)
except: pass

os.chdir(r"D:\wqz\code\NoCodeMotion")
p1 = subprocess.run([DOTNET,"build",PROJ,"-c","Debug"],
                    capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=600)
out = p1.stdout + "\n" + p1.stderr
errs = out.count(": error ")
lines = ["BUILD exit=%d errs=%d" % (p1.returncode, errs)]

if p1.returncode == 0 and os.path.exists(EXE):
    p = subprocess.Popen([EXE], cwd=os.path.dirname(EXE))
    pid = p.pid
    time.sleep(4.0)
    alive = p.poll() is None
    lines.append("PID=%s ALIVE=%s" % (pid, alive))
    # find our window
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
                if ln>0:
                    b=ctypes.create_unicode_buffer(ln+1); user32.GetWindowTextW(hwnd,b,ln+1); t=b.value
                cls = ctypes.create_unicode_buffer(256); user32.GetClassNameW(hwnd,cls,256)
                found.append((t,cls.value))
        return True
    user32.EnumWindows(EnumWindowsProc(cb), 0)
    lines.append("WINDOWS=%d %s" % (len(found), found))
    try: p.terminate()
    except: pass

# read error file
if os.path.exists(ERR):
    with open(ERR,"r",encoding="utf-8",errors="replace") as f:
        lines.append("=== STARTUP ERROR ===")
        lines.append(f.read())
else:
    lines.append("(no startup_error.txt)")

with open(LOG,"w",encoding="utf-8",errors="replace") as f: f.write("\n".join(lines))
print("\n".join(lines))
