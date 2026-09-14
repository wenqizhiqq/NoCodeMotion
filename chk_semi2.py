data = open(r'D:\wqz\code\NoCodeMotion\build_semi2.log', 'rb').read().decode('gbk', 'replace')
lines = data.splitlines()
keep = []
keep.append('LINES=' + str(len(lines)))
for l in lines:
    low = l.lower()
    if 'error' in low or 'succeeded' in low or 'warning' in low or ('生成' in l) or ('成功' in l):
        # 仅保留 ASCII 安全的字符，避免 Read 判定为二进制
        safe = ''.join(ch if 32 <= ord(ch) < 127 else '?' for ch in l)
        keep.append(safe)
keep.append('---TAIL---')
for l in lines[-8:]:
    safe = ''.join(ch if 32 <= ord(ch) < 127 else '?' for ch in l)
    keep.append(safe)
with open(r'D:\wqz\code\NoCodeMotion\chk_semi2.txt', 'w', encoding='ascii') as f:
    f.write('\n'.join(keep))
