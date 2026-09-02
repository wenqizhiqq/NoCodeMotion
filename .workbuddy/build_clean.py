import subprocess, os, shutil, time

DOTNET = r"C:\Program Files\dotnet\dotnet.exe"
ROOT = r"D:\wqz\code\NoCodeMotion"
PROJ = os.path.join(ROOT, "NoCodeMotion.csproj")
LOG = r"D:\wqz\code\NoCodeMotion\.workbuddy\build_clean.txt"

# 1) kill all build/runtime processes that may hold locks on obj/bin
for n in ("NoCodeMotion.exe","dotnet.exe","VBCSCompiler.exe","MSBuild.exe"):
    r = subprocess.run(["taskkill","/IM",n,"/F"], capture_output=True, text=True)
    print("kill", n, r.returncode)

time.sleep(2)

# 2) wipe obj and bin to clear any locked/generated files
for d in ("obj","bin"):
    p = os.path.join(ROOT, d)
    if os.path.exists(p):
        try:
            shutil.rmtree(p)
            print("removed", p)
        except Exception as e:
            print("rmtree", d, "err:", e)

os.chdir(ROOT)
t0 = time.time()
p = subprocess.run([DOTNET,"build",PROJ,"-c","Debug"],
                   capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=900)
dt = time.time()-t0
out = p.stdout + "\n" + p.stderr
errs = out.count(": error ")
with open(LOG,"w",encoding="utf-8",errors="replace") as f:
    f.write("EXIT=%d time=%.1fs errs=%d\n\n" % (p.returncode, dt, errs))
    f.write(out)
print("EXIT=%d time=%.1fs errs=%d" % (p.returncode, dt, errs))
