import os
src = r"D:\wqz\code\NoCodeMotion\.workbuddy\ps_build.txt"
dst = r"D:\wqz\code\NoCodeMotion\.workbuddy\ps_build_clean.txt"
data = None
with open(src, "rb") as f:
    raw = f.read()
for enc in ("utf-16", "utf-8", "utf-8-sig", "gbk"):
    try:
        data = raw.decode(enc)
        break
    except Exception:
        continue
if data is None:
    data = raw.decode("utf-8", errors="replace")
# strip nulls
data = data.replace("\x00", "")
with open(dst, "w", encoding="utf-8") as f:
    f.write(data)
# print error lines
lines = [l for l in data.splitlines() if ": error " in l]
with open(r"D:\wqz\code\NoCodeMotion\.workbuddy\ps_build_errs.txt", "w", encoding="utf-8") as f:
    f.write("\n".join(lines) if lines else "(no ': error ' lines)\n")
    f.write("\n--- tail ---\n")
    f.write("\n".join(data.splitlines()[-25:]))
print("decoded len=%d errs=%d" % (len(data), len(lines)))
