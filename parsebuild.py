import io
p = r'D:\wqz\code\NoCodeMotion\bout.txt'
out = r'D:\wqz\code\NoCodeMotion\parseout.txt'
with open(p, 'rb') as f:
    data = f.read()
data = bytes(b for b in data if b >= 32 or b in (9, 10, 13))
txt = data.decode('utf-8', 'replace')
lines = txt.splitlines()
errs = [l for l in lines if ' error ' in l.lower() or l.strip().startswith('error CS')]
warns = [l for l in lines if ' warning ' in l.lower() or l.strip().startswith('warning CS')]
with open(out, 'w', encoding='utf-8') as o:
    o.write('error lines: %d\n' % len(errs))
    for l in errs[:60]:
        o.write(l + '\n')
    o.write('warning lines: %d\n' % len(warns))
    for l in warns[:10]:
        o.write(l + '\n')
