import os, glob

ROOT = r"D:\wqz\code\NoCodeMotion"
ZW   = "\u200b"
SENT = "\u200b\u2063\u200b"
# 新方案：号码拆开，混入 ◆/◇/﹕，源码不出现连续完整号码
NOTE = "※保留所有权利请勿删除"
SYMS = "◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖"
MID  = "◆温启志◆编写◇微信﹕187◆1936◇1399　" + NOTE + "◇"

def symline(n=56):
    return "// " + "".join(SYMS[i % len(SYMS)] for i in range(n)) + SENT

def midline():
    return "// " + MID + SENT

HEADER_CS = [symline(), midline(), symline()]
FOOTER_CS = [
    "// ◇作者保留所有权利　请勿删除※" + SENT,
    symline(34),
]

def is_old(line: str) -> bool:
    if SENT in line:
        return True
    if ZW in line:                      # 旧零宽水印
        return True
    s = line.strip()
    if s.startswith("//"):
        body = s[2:].replace("═", "").replace(" ", "")
        if body == "":
            return True
    if "18719361399" in line:           # 任何连续完整号码都清掉
        return True
    return False

def codec_of(raw: bytes):
    if raw[:3] == b"\xef\xbb\xbf":
        return "utf-8-sig"
    try:
        raw.decode("utf-8"); return "utf-8"
    except UnicodeDecodeError:
        return "gbk"

cnt = 0
for f in glob.glob(os.path.join(ROOT, "**", "*.cs"), recursive=True):
    rel = os.path.relpath(f, ROOT)
    if any(p in rel for p in ("bin", "obj", ".workbuddy")):
        continue
    if rel == "Services/AuthorWatermark.cs":
        continue  # 已单独生成
    raw = open(f, "rb").read()
    if raw[:1] == b"\x88":
        continue
    codec = codec_of(raw)
    text = raw.decode(codec)
    if text.startswith("\ufeff"):
        text = text[1:]
    lines = text.split("\n")
    kept = [ln for ln in lines if not is_old(ln)]
    body = "\n".join(kept).strip("\n")
    out = "\n".join(HEADER_CS) + "\n" + body + "\n" + "\n".join(FOOTER_CS) + "\n"
    enc = "utf-8-sig" if codec == "gbk" else codec
    open(f, "wb").write(out.encode(enc))
    cnt += 1

print("updated cs:", cnt)
