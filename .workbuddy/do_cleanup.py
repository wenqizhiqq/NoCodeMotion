import os, glob, shutil, sys

base = r"D:\wqz\code\NoCodeMotion\.workbuddy"
# remove temp diagnostic files at .workbuddy root only (keep memory/ subdir)
removed = 0
failed = []
for name in os.listdir(base):
    p = os.path.join(base, name)
    if os.path.isfile(p):
        try:
            os.remove(p)
            removed += 1
        except Exception as e:
            failed.append((name, str(e)))
    # skip directories (memory/ preserved)

# external build dirs
dirs = [r"D:\wqz\code\NoCodeMotionArtifacts"] + [f"D:\\wqz\\code\\NCMBuild{c}" for c in "ABCDEFGH"]
dremoved = 0
dfailed = []
for d in dirs:
    if os.path.isdir(d):
        try:
            shutil.rmtree(d, ignore_errors=False)
            dremoved += 1
        except Exception as e:
            dfailed.append((d, str(e)))

report = f"workbuddy_root_files_removed={removed} failed={len(failed)}\n"
report += "FAIL: " + "; ".join(f"{n}:{e}" for n, e in failed[:20]) + "\n"
report += f"build_dirs_removed={dremoved} failed={len(dfailed)}\n"
report += "DFAIL: " + "; ".join(f"{d}:{e}" for d, e in dfailed[:20])
print(report)
with open(r"D:\wqz\code\NoCodeMotion\.workbuddy\cleanup_report.txt", "w", encoding="utf-8") as f:
    f.write(report)
