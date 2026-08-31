"""从 EditorPage.xaml.cs 取 3 行作者签名，前置到 CylinderPage.xaml"""
import os
P = r"D:\wqz\code\NoCodeMotion\Views\CylinderPage.xaml"
SRC = r"D:\wqz\code\NoCodeMotion\Views\EditorPage.xaml.cs"

# 读 EditorPage 的前 3 行（签名）
with open(SRC, "rb") as f:
    src_lines = f.read().split(b"\n")[:3]
sig = b"\n".join(src_lines) + b"\n"
print("SIG len:", len(sig))
print("SIG repr:", repr(sig[:80]))

# 读当前 CylinderPage 内容，去掉开头的 BOM / 空行
with open(P, "rb") as f:
    cur = f.read()
# 去 BOM
if cur.startswith(b"\xef\xbb\xbf"):
    cur = cur[3:]
# 去开头空行
while cur.startswith(b"\n"):
    cur = cur[1:]

# 拼装：签名 + 主体
new = sig + cur
with open(P, "wb") as f:
    f.write(new)
print("WROTE", os.path.getsize(P), "bytes")
