import re
p = r"D:\wqz\code\NoCodeMotion\_build_main.txt"
lines = open(p, encoding="utf-8", errors="replace").read().split("\n")
errs = [l for l in lines if re.search(r"error CS|error :|\.cs\(\d+,\d+\)", l) and "warning" not in l.lower()]
# also grab the final summary line
summary = [l for l in lines if re.search(r"生成成功|失败|个错误|个警告|Build succeeded|Build FAILED", l)]
out = []
out.append("===== ERRORS (if any) =====")
out.append("\n".join(errs) if errs else "(none)")
out.append("")
out.append("===== SUMMARY =====")
out.append("\n".join(summary[-12:]) if summary else "(no summary line found)")
open(r"D:\wqz\code\NoCodeMotion\_build_main_err.txt", "w", encoding="utf-8").write("\n".join(out))
