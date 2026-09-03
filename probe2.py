import os

def w(s):
    with open(r"D:\wqz\code\NoCodeMotion\probe2.txt", "a", encoding="utf-8") as f:
        f.write(s + "\n")

# 1) OpenCvSharp4 package native DLLs
w("=== OpenCvSharp4 package ===")
pkg = r"C:\Users\admin\.nuget\packages\opencvsharp4"
if os.path.isdir(pkg):
    for root, dirs, files in os.walk(pkg):
        for fn in files:
            if fn.lower().endswith(".dll") or fn.lower() == "opencvsharp4.nuspec" or fn.lower().endswith(".props") or fn.lower().endswith(".targets"):
                w("  " + os.path.join(root, fn))
else:
    w("  pkg NOT FOUND: " + pkg)

# 2) Any opencvextern anywhere on C: (restricted)
w("=== OpenCvSharpExtern search on C: (top dirs only) ===")
for d in [r"C:\Users\admin\.nuget\packages", r"C:\Program Files", r"C:\Windows\System32"]:
    if not os.path.isdir(d):
        continue
    for root, dirs, files in os.walk(d):
        # limit depth
        if root[len(d):].count(os.sep) > 5:
            dirs[:] = []
            continue
        for fn in files:
            if fn.lower() == "opencvextern.dll":
                w("  " + os.path.join(root, fn))

# 3) GrayMatch source / built dll
w("=== GrayMatch\\GrayModelNative ===")
gmn = r"D:\wqz\code\GrayMatch\GrayModelNative"
w("  exists=" + str(os.path.isdir(gmn)))
if os.path.isdir(gmn):
    for entry in sorted(os.scandir(gmn)):
        w("  " + ("[D] " if entry.is_dir() else "") + entry.name)
    # look for built dll in common places
    for cand in [
        os.path.join(gmn, "build-ninja9", "GrayModelNative.dll"),
        os.path.join(gmn, "build", "GrayModelNative.dll"),
        os.path.join(gmn, "bin", "GrayModelNative.dll"),
    ]:
        w("  built? " + cand + " = " + str(os.path.exists(cand)))
    # look for CMakeLists anywhere under gmn (depth 3)
    for root, dirs, files in os.walk(gmn):
        if root[len(gmn):].count(os.sep) > 3:
            dirs[:] = []
            continue
        for fn in files:
            if fn.lower() in ("cmakelists.txt",) or fn.lower().endswith(".cpp") or fn.lower().endswith(".h"):
                w("  SRC " + os.path.join(root, fn))
w("DONE")
