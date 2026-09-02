import subprocess, os

DOTNET = r"C:\Program Files\dotnet\dotnet.exe"
PROJ = r"D:\wqz\code\NoCodeMotion\NoCodeMotion.csproj"
LOG = r"D:\wqz\code\NoCodeMotion\.workbuddy\build_full.txt"

for n in ("NoCodeMotion.exe","dotnet.exe","VBCSCompiler.exe"):
    subprocess.run(["taskkill","/IM",n,"/F"], capture_output=True, text=True)

os.chdir(r"D:\wqz\code\NoCodeMotion")
p = subprocess.run([DOTNET,"build",PROJ,"-c","Debug"],
                   capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=900)
out = p.stdout + "\n" + p.stderr
with open(LOG,"w",encoding="utf-8",errors="replace") as f:
    f.write("EXIT=%d\n\n" % p.returncode)
    f.write(out)
# also write just the error lines for quick grep
f2 = LOG + ".errs"
with open(f2,"w",encoding="utf-8",errors="replace") as f:
    for line in out.splitlines():
        if ": error " in line:
            f.write(line + "\n")
print("DONE exit=%d errs=%d" % (p.returncode, out.count(": error ")))
