import re
path = r"D:\wqz\code\NoCodeMotion\smoke_build.txt"
out_path = r"D:\wqz\code\NoCodeMotion\smoke_errors.txt"
with open(path, "rb") as f:
    data = f.read()
data = re.sub(rb"\x1b\[[0-9;]*[A-Za-z]", b"", data)
data = re.sub(rb"[\x00-\x08\x0b\x0c\x0e-\x1f]", b"", data)
text = data.decode("ascii", "replace")
lines = text.splitlines()
errs = [l for l in lines if ("error cs" in l.lower() or l.lower().startswith("error") or ("NodeGraphSmoke" in l and "error" in l.lower()))]
with open(out_path, "w", encoding="utf-8") as o:
    o.write("ERRORS ({})\n".format(len(errs)))
    for e in errs:
        o.write(e + "\n")
    o.write("\nLAST 30\n")
    for l in lines[-30:]:
        o.write(l + "\n")
print("done")
