import re
path = r"D:\wqz\code\NodeGraphSmoke\smoke_out.txt"
out_path = r"D:\wqz\code\NodeGraphSmoke\smoke_result.txt"
with open(path, "rb") as f:
    data = f.read()
data = re.sub(rb"\x1b\[[0-9;]*[A-Za-z]", b"", data)
data = re.sub(rb"[\x00-\x08\x0b\x0c\x0e-\x1f]", b"", data)
text = data.decode("ascii", "replace")
with open(out_path, "w", encoding="utf-8") as o:
    o.write(text)
print("done")
