import re
src = r"D:\wqz\code\NoCodeMotion\bout.txt"
dst = r"D:\wqz\code\NoCodeMotion\clean_build.txt"
with open(src, "rb") as f:
    raw = f.read()
# strip ANSI escape sequences
raw = re.sub(rb"\x1b\[[0-9;]*m", b"", raw)
raw = raw.replace(b"\x00", b"")
txt = raw.decode("utf-8", "ignore")
with open(dst, "w", encoding="utf-8") as f:
    f.write(txt)
# also pick key lines
keys = ["错误", "已成功生成", "error CS", "Build succeeded", "生成失败", "Warning(s)"]
lines = txt.splitlines()
out = [l for l in lines if any(k in l for k in keys)]
with open(r"D:\wqz\code\NoCodeMotion\clean_build_keys.txt", "w", encoding="utf-8") as f:
    f.write("\n".join(out[-40:]) if out else "(no key lines)")
