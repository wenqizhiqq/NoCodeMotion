import os, sys

OUT = open(r"D:\wqz\code\NoCodeMotion\probe_pkgs.txt", "w", encoding="utf-8")

def list_dir(p, depth=0, maxdepth=2):
    if depth > maxdepth:
        return
    try:
        for e in sorted(os.scandir(p), key=lambda x: x.name):
            if e.is_dir():
                OUT.write("  "*depth + "[D] " + e.name + "\n")
                list_dir(e.path, depth+1, maxdepth)
            else:
                OUT.write("  "*depth + e.name + "\n")
    except Exception as ex:
        OUT.write("  "*depth + "ERR " + str(ex) + "\n")

OUT.write("########## NuGet opencvsharp* packages ##########\n")
npkg = r"C:\Users\admin\.nuget\packages"
for e in sorted(os.scandir(npkg)):
    if e.name.lower().startswith("opencvsharp") or e.name.lower().startswith("opencv"):
        OUT.write("[PKG] " + e.name + "\n")
        list_dir(e.path, 1, 3)

OUT.write("\n########## D:\\wqz\\code\\GrayMatch\\GrayModelNative ##########\n")
gmn = r"D:\wqz\code\GrayMatch\GrayModelNative"
if os.path.exists(gmn):
    list_dir(gmn, 0, 2)
else:
    OUT.write("NOT EXISTS: " + gmn + "\n")

OUT.write("\n########## search OpenCvSharpExtern.dll in nuget ##########\n")
found = []
for root, dirs, files in os.walk(npkg):
    for f in files:
        if f.lower() == "opencvextern.dll":
            found.append(os.path.join(root, f))
for p in found[:10]:
    OUT.write("  " + p + "\n")
if not found:
    OUT.write("  (none in nuget)\n")

OUT.write("\nDONE\n")
OUT.close()
