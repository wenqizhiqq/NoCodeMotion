import subprocess, os

DOTNET = r"C:\Program Files\dotnet\dotnet.exe"
PROJ = r"D:\wqz\code\NoCodeMotion\NoCodeMotion.csproj"
LOG = r"D:\wqz\code\NoCodeMotion\.workbuddy\build_sep2.txt"

for n in ("NoCodeMotion.exe","dotnet.exe","VBCSCompiler.exe","MSBuild.exe"):
    subprocess.run(["taskkill","/IM",n,"/F"], capture_output=True, text=True)

biop = r"D:\wqz\code\NoCodeMotion\.bt\obj\"
if os.path.exists(biop):
    import shutil
    try: shutil.rmtree(biop)
    except Exception: pass

os.chdir(r"D:\wqz\code\NoCodeMotion")
p = subprocess.run([DOTNET,"build",PROJ,"-c","Debug",
                    "-p:BaseIntermediateOutputPath="+biop],
                   capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=900)
out = p.stdout + "\n" + p.stderr
errs = out.count(": error ")
lines = ["EXIT=%d errs=%d" % (p.returncode, errs)]
for line in out.splitlines()[-50:]:
    lines.append(line)
exe = r"D:\wqz\code\NoCodeMotion\bin\Debug\net10.0-windows\NoCodeMotion.exe"
lines.append("EXE exists=%s" % os.path.exists(exe))
with open(LOG,"w",encoding="utf-8",errors="replace") as f:
    f.write("\n".join(lines))
print("DONE exit=%d errs=%d" % (p.returncode, errs))
