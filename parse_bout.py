import re
path = r"D:\wqz\code\NoCodeMotion\bout.txt"
out_path = r"D:\wqz\code\NoCodeMotion\build_errors.txt"
with open(path, "rb") as f:
    data = f.read()
data = re.sub(rb"\x1b\[[0-9;]*[A-Za-z]", b"", data)
data = re.sub(rb"[\x00-\x08\x0b\x0c\x0e-\x1f]", b"", data)
text = data.decode("ascii", "replace")
lines = text.splitlines()
errs = [l for l in lines if "error cs" in l.lower() or l.lower().startswith("error")]
with open(out_path, "w", encoding="utf-8") as o:
    o.write("ERRORS ({})\n".format(len(errs)))
    for e in errs:
        o.write(e + "\n")
    o.write("\nLAST 6\n")
    for l in lines[-6:]:
        o.write(l + "\n")
print("done")
