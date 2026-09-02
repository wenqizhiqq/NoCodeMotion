src = r"D:\wqz\code\NoCodeMotion\.workbuddy\ps_build3.txt"
dst = r"D:\wqz\code\NoCodeMotion\.workbuddy\ps_build3_clean.txt"
with open(src, "rb") as f:
    raw = f.read()
data = None
for enc in ("utf-16","utf-8","utf-8-sig","gbk"):
    try:
        data = raw.decode(enc); break
    except Exception: continue
if data is None:
    data = raw.decode("utf-8", errors="replace")
data = data.replace("\x00","")
with open(dst,"w",encoding="utf-8") as f:
    f.write(data)
errs = [l for l in data.splitlines() if ": error " in l]
with open(r"D:\wqz\code\NoCodeMotion\.workbuddy\ps_build3_errs.txt","w",encoding="utf-8") as f:
    f.write("\n".join(errs[:40]) if errs else "(no ': error ')\n")
    f.write("\n--- tail 30 ---\n")
    f.write("\n".join(data.splitlines()[-30:]))
print("errs=%d" % len(errs))
