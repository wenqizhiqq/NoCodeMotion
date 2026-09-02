src = r"D:\wqz\code\NoCodeMotion\.workbuddy\ps_build7.txt"
dst = r"D:\wqz\code\NoCodeMotion\.workbuddy\ps_build7_clean.txt"
with open(src,"rb") as f:
    raw = f.read()
data=None
for enc in ("utf-16","utf-8","utf-8-sig","gbk"):
    try: data=raw.decode(enc); break
    except: continue
if data is None: data=raw.decode("utf-8",errors="replace")
data=data.replace("\x00","")
with open(dst,"w",encoding="utf-8") as f: f.write(data)
errs=[l for l in data.splitlines() if ": error " in l]
with open(r"D:\wqz\code\NoCodeMotion\.workbuddy\ps_build7_errs.txt","w",encoding="utf-8") as f:
    f.write("\n".join(errs[:20]) if errs else "(no errors)\n")
    f.write("\n--- output lines ---\n")
    for l in data.splitlines():
        if "-> " in l or "Build succeeded" in l or "error CS" in l or "warning" in l.lower() and "NU" not in l:
            f.write(l+"\n")
print("errs=%d len=%d" % (len(errs), len(data)))
