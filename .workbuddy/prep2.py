import subprocess, os, shutil
lines=[]
for n in ("dotnet.exe","VBCSCompiler.exe","MSBuild.exe","NoCodeMotion.exe"):
    r = subprocess.run(["taskkill","/IM",n,"/F"], capture_output=True, text=True)
    lines.append("kill %s rc=%s" % (n, r.returncode))
obj = r"D:\wqz\code\NoCodeMotionArtifacts\obj"
if os.path.exists(obj):
    try:
        shutil.rmtree(obj); lines.append("removed obj")
    except Exception as e:
        lines.append("rmtree err: %r" % e)
else:
    lines.append("no obj")
with open(r"D:\wqz\code\NoCodeMotion\.workbuddy\prep2.txt","w",encoding="utf-8") as f:
    f.write("\n".join(lines)+"\n")
print("OK")
