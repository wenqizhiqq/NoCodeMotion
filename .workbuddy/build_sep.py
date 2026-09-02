import subprocess, os, time, traceback, sys

DOTNET = r"C:\Program Files\dotnet\dotnet.exe"
ROOT = r"D:\wqz\code\NoCodeMotion"
PROJ = os.path.join(ROOT, "NoCodeMotion.csproj")
LOG = r"D:\wqz\code\NoCodeMotion\.workbuddy\build_sep.txt"
buf = []
def log(s):
    buf.append(str(s))
    print(s)

try:
    r = subprocess.run(["tasklist","/FI","IMAGENAME eq devenv.exe"], capture_output=True, text=True)
    log("DEVENV:\n"+r.stdout.strip())
    for n in ("dotnet.exe","VBCSCompiler.exe","MSBuild.exe"):
        rr = subprocess.run(["taskkill","/IM",n,"/F"], capture_output=True, text=True)
        log("kill %s rc=%s" % (n, rr.returncode))
    time.sleep(2)

    # redirect ONLY BaseIntermediateOutputPath (the locked obj files live here)
    biop = r"D:\wqz\code\NoCodeMotion\.bt\obj\"
    if os.path.exists(biop):
        import shutil
        try: shutil.rmtree(biop); log("removed biop")
        except Exception as e: log("rm biop err "+str(e))

    os.chdir(ROOT)
    t0=time.time()
    p = subprocess.run([DOTNET,"build",PROJ,"-c","Debug",
                        "-p:BaseIntermediateOutputPath="+biop],
                       capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=900)
    dt=time.time()-t0
    out=p.stdout+"\n"+p.stderr
    errs=out.count(": error ")
    log("EXIT=%d time=%.1fs errs=%d" % (p.returncode,dt,errs))
    # last 40 lines of build output
    for line in out.splitlines()[-40:]:
        log(line)
    exe = os.path.join(ROOT,"bin","Debug","net10.0-windows","NoCodeMotion.exe")
    log("EXE exists=%s path=%s" % (os.path.exists(exe), exe))
except Exception:
    log("EXCEPTION:\n"+traceback.format_exc())

with open(LOG,"w",encoding="utf-8",errors="replace") as f:
    f.write("\n".join(buf))
print("WROTE", LOG)
