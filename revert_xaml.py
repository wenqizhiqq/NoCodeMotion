import os, glob
ROOT = r"D:\wqz\code\NoCodeMotion"
SENT = "\u200b\u2063\u200b"
cnt = 0
for f in glob.glob(os.path.join(ROOT, "**", "*.xaml"), recursive=True):
    rel = os.path.relpath(f, ROOT)
    if any(p in rel for p in ("bin", "obj", ".workbuddy")):
        continue
    raw = open(f, "rb").read()
    if raw[:1] == b"\x88":
        continue
    codec = "utf-8-sig" if raw[:3] == b"\xef\xbb\xbf" else ("utf-8" if b"\x00" not in raw[:min(len(raw),4096)] else "gbk")
    try:
        text = raw.decode(codec)
    except UnicodeDecodeError:
        text = raw.decode("gbk")
    if SENT not in text:
        continue
    lines = text.split("\n")
    kept = [ln for ln in lines if SENT not in ln]
    out = "\n".join(kept)
    open(f, "wb").write(out.encode("utf-8-sig"))
    cnt += 1
print("reverted xaml:", cnt)
