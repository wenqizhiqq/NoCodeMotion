import os, glob

ROOT = r"D:\wqz\code\NoCodeMotion"
ZW   = "\u200b"          # 零宽：插在每个字符之间，使连续子串搜索失效
SENT = "\u200b\u2063\u200b"  # 不可见哨兵：用于幂等移除旧水印
PLAIN = "温启志编写，微信：18719361399"
OBF  = ZW.join(PLAIN)   # 混淆后的联系方式（渲染正常，搜索匹配不到）
NOTE = "※" + ZW.join("保留所有权利请勿删除")
SYMS = "◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖"

def symline(n=56):
    return "// " + "".join(SYMS[i % len(SYMS)] for i in range(n)) + SENT

def midline():
    # 中部水印行：前后塞满符号，进一步干扰搜索/正则
    return "// " + "◆" + OBF + "　" + NOTE + "◇" + SENT

HEADER_CS = [symline(), midline(), symline()]
FOOTER_CS = [
    "// " + "◇" + ZW.join("作者保留所有权利") + "　" + ZW.join("请勿删除") + "※" + SENT,
    symline(34),
]

def is_old_watermark(line: str) -> bool:
    s = line.strip()
    if SENT in line:
        return True
    if OBF in line:
        return True
    if s.startswith("//"):
        body = s[2:].replace("═", "").replace(" ", "")
        if body == "":
            return True
    return False

def codec_of(raw: bytes):
    if raw[:3] == b"\xef\xbb\xbf":
        return "utf-8-sig"
    try:
        raw.decode("utf-8"); return "utf-8"
    except UnicodeDecodeError:
        return "gbk"

def dom_nl(text: str):
    crlf = text.count("\r\n"); lf = text.count("\n") - crlf
    return "\r\n" if crlf >= lf else "\n"

def process_cs(path: str, rel: str):
    if rel == "Services/AuthorWatermark.cs":
        return False  # 单独生成
    raw = open(path, "rb").read()
    if raw[:1] == b"\x88":
        return False
    codec = codec_of(raw)
    text = raw.decode(codec)
    if text.startswith("\ufeff"):
        text = text[1:]
    lines = text.split("\n")
    kept = [ln for ln in lines if not is_old_watermark(ln)]
    body = "\n".join(kept)
    # 去首尾多余空行
    body = body.strip("\n")
    out = "\n".join(HEADER_CS) + "\n" + body + "\n" + "\n".join(FOOTER_CS) + "\n"
    enc = "utf-8-sig" if codec == "gbk" else codec
    open(path, "wb").write(out.encode(enc))
    return True

def process_xaml(path: str):
    raw = open(path, "rb").read()
    if raw[:1] == b"\x88":
        return False
    codec = codec_of(raw)
    text = raw.decode(codec)
    if text.startswith("\ufeff"):
        text = text[1:]
    lines = text.split("\n")
    kept = [ln for ln in lines if SENT not in ln]
    # 找第一个 '>' 所在行（xml 声明或根元素开始标签闭合处），在其后插入水印
    insert_at = 0
    for i, ln in enumerate(kept):
        if ">" in ln:
            insert_at = i + 1
            break
    block = [
        "<!-- " + "◆" + OBF + "　" + NOTE + "◇ -->" + SENT,
        "<!-- " + "".join(SYMS[i % len(SYMS)] for i in range(40)) + " -->" + SENT,
    ]
    kept[insert_at:insert_at] = block
    out = "\n".join(kept)
    enc = "utf-8-sig" if codec == "gbk" else codec
    open(path, "wb").write(out.encode(enc))
    return True

cnt_cs = cnt_xaml = 0
for f in glob.glob(os.path.join(ROOT, "**", "*.cs"), recursive=True):
    rel = os.path.relpath(f, ROOT)
    if any(p in rel for p in ("bin", "obj", ".workbuddy")):
        continue
    if process_cs(f, rel):
        cnt_cs += 1
for f in glob.glob(os.path.join(ROOT, "**", "*.xaml"), recursive=True):
    rel = os.path.relpath(f, ROOT)
    if any(p in rel for p in ("bin", "obj", ".workbuddy")):
        continue
    if process_xaml(f):
        cnt_xaml += 1

print(f"CS={cnt_cs} XAML={cnt_xaml}")
