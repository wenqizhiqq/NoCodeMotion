""""""
import os, shutil
P = r"D:\wqz\code\NoCodeMotion\Views\CylinderPage.xaml"
BAK = r"D:\wqz\code\_projtrash\CylinderPage.xaml_20260831"

# 读备份的前 3 行（作者签名）
with open(BAK, "rb") as f:
    bak_lines = f.read().split(b"\n")[:3]
sig = b"\n".join(bak_lines) + b"\n"
print("SIG bytes len:", len(sig))
print("SIG preview:", sig[:120])

# 读当前文件（去掉开头的空行）
with open(P, "rb") as f:
    cur = f.read()
# 去掉开头的换行
while cur.startswith(b"\n"):
    cur = cur[1:]

# 拼装：签名 + 主体
new = sig + cur
with open(P, "wb") as f:
    f.write(new)
print("WROTE", os.path.getsize(P), "bytes")
