import subprocess
exe = r"C:\Program Files\dotnet\dotnet.exe"
proj = r"D:\wqz\code\NoCodeMotion\NoCodeMotion.csproj"
p = subprocess.run([exe, "build", proj, "-c", "Debug"], capture_output=True, text=True, encoding="utf-8", errors="replace")
out = p.stdout + p.stderr
lines = out.splitlines()
errs = [l for l in lines if "error CS" in l or "error MC" in l or ": error " in l]
warns = [l for l in lines if ": warning " in l]
with open(r"D:\wqz\code\NoCodeMotion\build_result.txt", "w", encoding="ascii", errors="replace") as f:
    f.write("RETURNCODE=%d\n" % p.returncode)
    f.write("TOTAL_LINES=%d\n" % len(lines))
    f.write("ERRORS=%d\n" % len(errs))
    for e in errs[:40]:
        f.write("ERR> " + e.encode("ascii", "replace").decode() + "\n")
    f.write("WARNINGS=%d\n" % len(warns))
    for l in lines[-8:]:
        f.write("TAIL> " + l.encode("ascii", "replace").decode() + "\n")
