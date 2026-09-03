import os, sys

targets = [
    "GrayModelNative.dll",
    "OpenCvSharpExtern.dll",
    "opencv_world480.dll",
    "opencv_videoio_ffmpeg4130_64.dll",
    "vcomp140.dll",
]

roots = [
    r"D:\wqz\code",
    r"E:\网络代码\Gitee",
    r"C:\Users\admin\.nuget\packages",
    r"C:\Program Files\Microsoft Visual Studio",
    r"C:\Windows\System32",
]

hits = {t: [] for t in targets}

def walk(root):
    try:
        for entry in os.scandir(root):
            try:
                if entry.is_dir(follow_symlinks=False):
                    # prune huge/irrelevant dirs
                    low = entry.name.lower()
                    if low in ("node_modules", "$recycle.bin", "system volume information"):
                        continue
                    yield from walk(entry.path)
                else:
                    nm = entry.name.lower()
                    if nm in hits:
                        hits[entry.name].append(entry.path)
            except (PermissionError, OSError):
                pass
    except (PermissionError, OSError):
        pass

for r in roots:
    if not os.path.exists(r):
        continue
    for p in walk(r):
        pass

# also direct nuget package probe for opencvsharp
with open(r"D:\wqz\code\NoCodeMotion\found_dlls.txt", "w", encoding="utf-8") as f:
    for t in targets:
        f.write(f"=== {t} ===\n")
        if hits[t]:
            for p in hits[t][:20]:
                f.write("  " + p + "\n")
        else:
            f.write("  (not found)\n")
        f.write("\n")
    f.write("DONE\n")
print("written")
