import os, glob

wb = r"D:\wqz\code\NoCodeMotion\.workbuddy"
lines = []
lines.append("=== .workbuddy dir listing ===")
try:
    for f in sorted(os.listdir(wb)):
        p = os.path.join(wb, f)
        if os.path.isfile(p):
            lines.append(f"{f}  ({os.path.getsize(p)} bytes)")
        else:
            lines.append(f"{f}/  (dir)")
except Exception as e:
    lines.append(f"listdir error: {e}")

for name in ["diag2.txt", "startup_error.txt", "diag2.py", "runtest.txt"]:
    p = os.path.join(wb, name)
    lines.append(f"\n=== {name} ===")
    if os.path.exists(p):
        try:
            with open(p, "r", encoding="utf-8", errors="replace") as fh:
                lines.append(fh.read()[:4000])
        except Exception as e:
            lines.append(f"read error: {e}")
    else:
        lines.append("(missing)")

out = r"D:\wqz\code\NoCodeMotion\.workbuddy\scan_out.txt"
with open(out, "w", encoding="utf-8") as fh:
    fh.write("\n".join(lines))
print("WROTE", out)
