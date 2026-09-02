import os, subprocess

ROOT = r"D:\wqz\code\NoCodeMotion"
lines = []
bt_bin = os.path.join(ROOT, ".bt", "bin")
lines.append("=== .bt/bin tree (top) ===")
if os.path.exists(bt_bin):
    for dp, dn, fn in os.walk(bt_bin):
        depth = dp[len(bt_bin):].count(os.sep)
        if depth <= 2:
            for f in sorted(fn)[:60]:
                lines.append(os.path.join(dp, f))
        if depth > 2:
            break
else:
    lines.append("(no .bt/bin)")

exe = os.path.join(bt_bin, "NoCodeMotion.exe")
lines.append("\nEXE exists: %s" % os.path.exists(exe))

lines.append("\n=== recent .workbuddy files ===")
wb = os.path.join(ROOT, ".workbuddy")
for f in sorted(os.listdir(wb)):
    p = os.path.join(wb, f)
    if os.path.isfile(p):
        lines.append("%s (%d)" % (f, os.path.getsize(p)))

out = os.path.join(wb, "scan2_out.txt")
with open(out, "w", encoding="utf-8", errors="replace") as fh:
    fh.write("\n".join(lines))
print("WROTE", out)
