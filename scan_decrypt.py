import os

marker = bytes([0x88, 0x7d, 0x1c])
name_kw = ('decrypt', 'encrypt', 'codec', 'crypt', 'obfus', 'cipher', '\u52a0\u5bc6', '\u89e3\u5bc6')

roots = [
    r'D:\wqz',
    r'D:\\u4e09\u59c6\u68ee',
    r'C:\Users\admin\Desktop',
    r'C:\Users\admin\Documents',
    r'C:\Users\admin\source',
    r'C:\Users\admin\AppData\Local\Microsoft\VisualStudio',
]

skip_dirs = {'.git', 'node_modules', 'bin', 'obj', 'artifacts', 'packages', '.vs', 'Release', 'Debug'}

name_hits = []
marker_hits = []
plaintext_cs = []

for r in roots:
    if not os.path.isdir(r):
        continue
    for rd, dirs, files in os.walk(r):
        dirs[:] = [d for d in dirs if d.lower() not in skip_dirs]
        for f in files:
            low = f.lower()
            full = os.path.join(rd, f)
            if any(k in low for k in name_kw):
                name_hits.append(full)
            if low.endswith('.exe') or low.endswith('.dll') or low.endswith('.py') or low.endswith('.ps1') or low.endswith('.cs') or low.endswith('.json') or low.endswith('.xml'):
                try:
                    raw = open(full, 'rb').read(200000)
                except Exception:
                    continue
                if marker in raw and not (rd.replace('\\', '/').startswith(r'D:\wqz\code\NoCodeMotion'.replace('\\', '/')) and low.endswith('.cs')):
                    marker_hits.append(full)
            if low.endswith('.cs') and rd.replace('\\', '/').startswith(r'D:\wqz'.replace('\\', '/')) and r != r'D:\wqz\code\NoCodeMotion':
                try:
                    raw = open(full, 'rb').read()
                    if raw[:3] != marker:
                        plaintext_cs.append(full)
                except Exception:
                    pass

print("NAME HITS:")
for h in name_hits[:40]:
    print("  ", h)
print("MARKER-EMBEDDING (non-project .cs) HITS:")
for h in marker_hits[:40]:
    print("  ", h)
print("PLAINTEXT .cs OUTSIDE THIS PROJECT:")
for h in plaintext_cs[:40]:
    print("  ", h)
if not (name_hits or marker_hits or plaintext_cs):
    print("  (none)")
