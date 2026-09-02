import subprocess, os, shutil

for n in ("dotnet.exe","VBCSCompiler.exe","MSBuild.exe","NoCodeMotion.exe"):
    r = subprocess.run(["taskkill","/IM",n,"/F"], capture_output=True, text=True)
    print("kill", n, r.returncode)

obj = r"D:\wqz\code\NoCodeMotionArtifacts\obj"
if os.path.exists(obj):
    try:
        shutil.rmtree(obj)
        print("removed", obj)
    except Exception as e:
        print("rmtree err", e)
else:
    print("no obj dir")

with open(r"D:\wqz\code\NoCodeMotion\.workbuddy\prep.txt","w",encoding="utf-8") as f:
    f.write("prep done\n")
print("OK")
