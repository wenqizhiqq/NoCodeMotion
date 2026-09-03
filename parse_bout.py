import re

path = r"D:\wqz\code\NoCodeMotion\bout.txt"
out_path = r"D:\wqz\code\NoCodeMotion\build_errors.txt"
with open(path, "rb") as f:
    data = f.read()

# strip ANSI escape sequences and control bytes
data = re.sub(rb"\x1b\[[0-9;]*[A-Za-z]", b"", data)
data = re.sub(rb"[\x00-\x08\x0b\x0c\x0e-\x1f]", b"", data)

text = data.decode("ascii", "replace")
lines = text.splitlines()

errs = [l for l in lines if ("error cs" in l.lower() or l.lower().startswith("error") or "error:" in l.lower() or (".cs(" in l and "error" in l.lower()))]
warns = [l for l in lines if "warning cs" in l.lower() or "warning :" in l.lower()]

with open(out_path, "w", encoding="utf-8") as o:
    o.write("ERRORS ({}) found\n".format(len(errs)))
    for e in errs:
        o.write(e + "\n")
    o.write("\nWARNINGS ({}) found\n".format(len(warns)))
    for w in warns[:40]:
        o.write(w + "\n")
    o.write("\nLAST 25 LINES\n")
    for l in lines[-25:]:
        o.write(l + "\n")
print("done")
